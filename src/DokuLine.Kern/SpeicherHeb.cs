using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// HEB-Bögen im Speicher: Kopf (Art, Zeitraum, Nummer) und Fassungen (UPDATE verboten).
public sealed partial class Speicher
{
    public List<HebBogen> HebBoegen(long bewohnerId) => HebLesen("WHERE bewohner_id = $b", null, ("$b", bewohnerId));

    public HebBogen? HebBogen(long id) => HebLesen("WHERE id = $id", null, ("$id", id)).FirstOrDefault();

    // Legt einen Bogen an. Bei HEB B werden Rahmenziele, Leitziel und Nahziele des letzten Bogens
    // als erste Fassung (Entwurf) vorgelegt.
    public HebBogen HebAnlegen(long bewohnerId, Bogenart art, DateOnly von, DateOnly bis, string kuerzel)
    {
        var b = Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (bis < von) fehler.Add("Das Ende des Zeitraums liegt vor dem Beginn.");
        if (von < b.Aufnahmedatum) fehler.Add("Der Zeitraum beginnt vor der Aufnahme.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        var fruehere = HebBoegen(bewohnerId);
        var jetzt = Uhr();
        using var transaktion = db.BeginTransaction();
        long id = HebKopfEinfuegen(bewohnerId, art, von, bis, HebRegeln.Nummer(art, fruehere), DateOnly.FromDateTime(jetzt), transaktion);
        var vorlage = HebRegeln.Fortschreiben(art, fruehere.Where(x => x.Fertig).OrderBy(x => x.Bis).LastOrDefault());
        if (vorlage.Count > 0)
            HebFassungEinfuegen(new HebFassung { BogenId = id, Texte = vorlage, Erstellt = jetzt, Kuerzel = kuerzel.Trim() }, transaktion);
        transaktion.Commit();
        return HebBogen(id)!;
    }

    // Neue Fassung; ist nichts geändert, entsteht keine.
    public HebBogen HebSpeichern(HebFassung f, int? erwartet = null)
    {
        var bisher = HebBogen(f.BogenId) ?? throw new Eingabefehler(["Diesen HEB-Bogen gibt es nicht mehr."]);
        if (bisher.Aktuell is HebFassung alt && HebRegeln.Gleich(alt, f)) return bisher;
        using var transaktion = db.BeginTransaction();
        FassungenPruefen(transaktion, "SELECT COUNT(*) FROM heb_fassungen WHERE bogen_id = $b", erwartet,
            () => HebBogen(f.BogenId)?.Aktuell is HebFassung a ? FassungText(a.Kuerzel, a.Erstellt, HebRegeln.AlsText(bisher.Art, a)) : "",
            FassungText(f.Kuerzel, f.Erstellt, HebRegeln.AlsText(bisher.Art, f)), ("$b", f.BogenId));
        HebFassungEinfuegen(f, transaktion);
        transaktion.Commit();
        return HebBogen(f.BogenId)!;
    }

    // Quellen neben einem Lebensbereich: Berichte des Zeitraums, fertige Reflexionen des Zeitraums.
    public List<Bericht> HebBerichte(long bewohnerId, DateOnly von, DateOnly bis, int bereich) =>
        Berichte(bewohnerId, new BerichtFilter(von, bis, bereich));

    public List<(DateOnly Monat, string Text)> HebReflexionen(long bewohnerId, DateOnly von, DateOnly bis, int bereich) =>
        Reflexionen(bewohnerId)
            .Where(r => r.Fertig && r.Monat >= ReflexionRegeln.MonatVon(von) && r.Monat <= bis && r.Text(bereich).Length > 0)
            .Select(r => (r.Monat, r.Text(bereich))).ToList();

    // Wochenplan, der am Ende des Zeitraums galt (Regelwoche und Therapien).
    public List<Termin> HebWochenplan(long bewohnerId, DateOnly bis) =>
        Woche(bewohnerId, WochenplanRegeln.Montag(bis)).Where(t => t.Quelle != TerminQuelle.Kalender).ToList();

    private long HebKopfEinfuegen(long bewohnerId, Bogenart art, DateOnly von, DateOnly bis, int? nummer, DateOnly erstellt, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO heb_boegen (bewohner_id, art, von, bis, nummer, erstellt) VALUES ($b, $a, $v, $bis, $n, $e)", transaktion,
            ("$b", bewohnerId), ("$a", art.ToString()), ("$v", Iso(von)), ("$bis", Iso(bis)), ("$n", nummer), ("$e", Iso(erstellt)));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    private void HebFassungEinfuegen(HebFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("INSERT INTO heb_fassungen (bogen_id, texte, hilfebedarf, fertig, erstellt, kuerzel) VALUES ($b, $t, $h, $f, $e, $k)", transaktion,
            ("$b", f.BogenId), ("$t", JsonSerializer.Serialize(f.Texte)),
            ("$h", JsonSerializer.Serialize(f.Hilfebedarf.ToDictionary(x => x.Key.ToString(CultureInfo.InvariantCulture), x => x.Value))),
            ("$f", f.Fertig ? 1 : 0), ("$e", Zeit(f.Erstellt)), ("$k", f.Kuerzel));

    private List<HebBogen> HebLesen(string bedingung, SqliteTransaction? transaktion, params (string, object?)[] parameter)
    {
        var koepfe = new List<(long Id, long Bewohner, Bogenart Art, DateOnly Von, DateOnly Bis, int? Nummer, DateOnly Erstellt)>();
        using (var befehl = Befehl($"SELECT id, bewohner_id, art, von, bis, nummer, erstellt FROM heb_boegen {bedingung} ORDER BY bis, id", transaktion, parameter))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                koepfe.Add((leser.GetInt64(0), leser.GetInt64(1), Enum.Parse<Bogenart>(leser.GetString(2)), AusIso(leser.GetString(3)),
                    AusIso(leser.GetString(4)), leser.IsDBNull(5) ? null : leser.GetInt32(5), AusIso(leser.GetString(6))));
        var liste = new List<HebBogen>();
        foreach (var k in koepfe)
        {
            var fassungen = new List<HebFassung>();
            using var befehl = Befehl("SELECT texte, hilfebedarf, fertig, erstellt, kuerzel FROM heb_fassungen WHERE bogen_id = $b ORDER BY id", transaktion, [("$b", k.Id)]);
            using var leser = befehl.ExecuteReader();
            while (leser.Read())
                fassungen.Add(new HebFassung
                {
                    BogenId = k.Id,
                    Texte = JsonSerializer.Deserialize<Dictionary<string, string>>(leser.GetString(0)) ?? [],
                    Hilfebedarf = (JsonSerializer.Deserialize<Dictionary<string, int?>>(leser.GetString(1)) ?? [])
                        .ToDictionary(x => int.Parse(x.Key, CultureInfo.InvariantCulture), x => x.Value),
                    Fertig = leser.GetInt64(2) == 1, Erstellt = AusZeit(leser.GetString(3)), Kuerzel = leser.GetString(4),
                });
            liste.Add(new HebBogen(k.Id, k.Bewohner, k.Art, k.Von, k.Bis, k.Nummer, k.Erstellt, fassungen));
        }
        return liste;
    }

    // Demo: frühere Bögen (die meisten aktuell, zwei fällig) und ein Entwurf.
    private void DemoHebEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, AktiveBewohnerSql);
        Ausfuehren("DELETE FROM heb_boegen", transaktion);
        foreach (var d in Demodaten.HebBoegen(bewohner, Uhr()))
        {
            long id = HebKopfEinfuegen(d.BewohnerId, d.Art, d.Von, d.Bis, d.Nummer, d.Bis, transaktion);
            foreach (var f in d.Fassungen) HebFassungEinfuegen(f with { BogenId = id }, transaktion);
        }
        SetzeMeta(DemoHeb, "1", transaktion);
    }
}
