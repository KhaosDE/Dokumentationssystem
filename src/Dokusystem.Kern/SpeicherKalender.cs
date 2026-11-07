using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Kalender im Speicher: je Termin Fassungen (ändern und absagen sind neue Fassungen).
public sealed partial class Speicher
{
    public event Action? KalenderGeaendert;

    public KalenderTermin KalenderAnlegen(KalenderFassung f)
    {
        BewohnerPruefen(f);
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT INTO kalender DEFAULT VALUES", transaktion);
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        KalenderFassungEinfuegen(id, f, transaktion);
        transaktion.Commit();
        KalenderGeaendert?.Invoke();
        return KalenderTermin(id)!;
    }

    public KalenderTermin KalenderAendern(long id, KalenderFassung f)
    {
        var bisher = KalenderTermin(id) ?? throw new Eingabefehler(["Diesen Termin gibt es nicht mehr."]);
        if (bisher.Aktuell.Abgesagt) throw new Eingabefehler(["Dieser Termin ist abgesagt."]);
        BewohnerPruefen(f);
        using var transaktion = db.BeginTransaction();
        KalenderFassungEinfuegen(id, f, transaktion);
        transaktion.Commit();
        KalenderGeaendert?.Invoke();
        return KalenderTermin(id)!;
    }

    // Absagen statt löschen: eine neue Fassung „abgesagt“; der Termin bleibt im Verlauf.
    public void KalenderAbsagen(long id, string kuerzel, DateTime jetzt)
    {
        var bisher = KalenderTermin(id) ?? throw new Eingabefehler(["Diesen Termin gibt es nicht mehr."]);
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Kürzel fehlt (oben rechts „Angemeldet als …“)."]);
        if (bisher.Aktuell.Abgesagt) return;
        using var transaktion = db.BeginTransaction();
        KalenderFassungEinfuegen(id, bisher.Aktuell with { Abgesagt = true, Erstellt = jetzt, Kuerzel = kuerzel.Trim() }, transaktion);
        transaktion.Commit();
        KalenderGeaendert?.Invoke();
    }

    private void BewohnerPruefen(KalenderFassung f)
    {
        var vorhanden = AlleBewohner().Select(b => b.Id!.Value).ToHashSet();
        if (f.Bewohner.Any(b => !vorhanden.Contains(b))) throw new Eingabefehler(["Einen der gewählten Bewohner gibt es nicht mehr."]);
    }

    private void KalenderFassungEinfuegen(long id, KalenderFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO kalender_fassungen (termin_id, art, titel, beginn, ende, ganztags, ort, hinweis, bewohner, treffpunkt, begleitung, abgesagt, erstellt, kuerzel)
            VALUES ($t, $a, $ti, $b, $e, $g, $o, $h, $bw, $tr, $be, $ab, $er, $k)
            """, transaktion,
            ("$t", id), ("$a", KalenderRegeln.ArtKuerzel(f.Art)), ("$ti", f.Titel), ("$b", Zeit(f.Beginn)), ("$e", Zeit(f.Ende)),
            ("$g", f.Ganztags ? 1 : 0), ("$o", f.Ort), ("$h", f.Hinweis), ("$bw", JsonSerializer.Serialize(f.Bewohner)),
            ("$tr", f.Treffpunkt), ("$be", f.Begleitung), ("$ab", f.Abgesagt ? 1 : 0), ("$er", Zeit(f.Erstellt)), ("$k", f.Kuerzel));

    public KalenderTermin? KalenderTermin(long id) => KalenderLesen(null).FirstOrDefault(k => k.Id == id);

    // Termine (nicht abgesagt), die den Zeitraum berühren; optional nur die eines Bewohners.
    public List<KalenderTermin> Kalender(DateOnly von, DateOnly bis, long? bewohnerId = null) =>
        KalenderLesen(null)
            .Where(k => !k.Aktuell.Abgesagt && DateOnly.FromDateTime(k.Aktuell.Beginn) <= bis && KalenderRegeln.LetzterTag(k.Aktuell) >= von)
            .Where(k => bewohnerId is not long b || k.Aktuell.Bewohner.Contains(b))
            .OrderBy(k => k.Aktuell.Beginn).ThenBy(k => k.Id).ToList();

    // Heimfahrt, die gerade läuft (für „abwesend: Heimfahrt bis …“ auf der Startseite).
    public KalenderTermin? HeimfahrtJetzt(long bewohnerId, DateTime jetzt) =>
        KalenderLesen(null).FirstOrDefault(k => !k.Aktuell.Abgesagt && k.Aktuell.Art == KalenderArt.Heimfahrt &&
                                                k.Aktuell.Bewohner.Contains(bewohnerId) && k.Aktuell.Beginn <= jetzt && jetzt < k.Aktuell.Ende);

    private List<KalenderTermin> KalenderLesen(SqliteTransaction? transaktion)
    {
        var fassungen = new Dictionary<long, List<KalenderFassung>>();
        using (var befehl = Befehl("""
            SELECT termin_id, art, titel, beginn, ende, ganztags, ort, hinweis, bewohner, treffpunkt, begleitung, abgesagt, erstellt, kuerzel
            FROM kalender_fassungen ORDER BY id
            """, transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!fassungen.TryGetValue(leser.GetInt64(0), out var l)) fassungen[leser.GetInt64(0)] = l = [];
                l.Add(new KalenderFassung
                {
                    Art = KalenderRegeln.ArtAus(leser.GetString(1)), Titel = leser.GetString(2), Beginn = AusZeit(leser.GetString(3)),
                    Ende = AusZeit(leser.GetString(4)), Ganztags = leser.GetInt64(5) == 1, Ort = leser.GetString(6), Hinweis = leser.GetString(7),
                    Bewohner = JsonSerializer.Deserialize<List<long>>(leser.GetString(8)) ?? [], Treffpunkt = leser.GetString(9),
                    Begleitung = leser.GetString(10), Abgesagt = leser.GetInt64(11) == 1, Erstellt = AusZeit(leser.GetString(12)), Kuerzel = leser.GetString(13),
                });
            }
        return fassungen.Select(x => new KalenderTermin { Id = x.Key, Fassungen = x.Value }).ToList();
    }

    // Demo: Termine relativ zu heute (Arzttermine, Wohnbereich, eine laufende Heimfahrt, Ausflug).
    private void DemoKalenderEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM kalender", transaktion);
        foreach (var d in Demodaten.Kalender(bewohner, Uhr()))
        {
            Ausfuehren("INSERT INTO kalender DEFAULT VALUES", transaktion);
            long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            foreach (var f in d) KalenderFassungEinfuegen(id, f, transaktion);
        }
        SetzeMeta(DemoKalender, "1", transaktion);
    }
}
