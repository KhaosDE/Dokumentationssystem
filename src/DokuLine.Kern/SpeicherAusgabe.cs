using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Medikamentenausgabe speichern (Bauplan 42): je Medikament, Tag und Einnahmezeit ein Eintrag mit Status, Kürzel und Zeit;
// nie geändert (ein späterer Eintrag gilt). „Abgelehnt“ und „nicht abgeholt“ schreiben einen kurzen Bericht der Art
// „Medikation“ in die Dokumentation.
public sealed partial class Speicher
{
    private const string DemoAusgabe = "demo_ausgabe";

    public event Action? AusgabeGeaendert;

    public List<AusgabeEintrag> AusgabeEintraege(DateOnly von, DateOnly bis, long? bewohnerId = null)
    {
        var liste = new List<AusgabeEintrag>();
        using var befehl = Befehl("""
            SELECT id, bewohner_id, tag, zeit, verordnung_id, status, kuerzel, erfasst, bericht_id FROM medikamentenausgaben
            WHERE tag >= $von AND tag <= $bis AND ($b IS NULL OR bewohner_id = $b) ORDER BY id
            """, null, [("$von", Iso(von)), ("$bis", Iso(bis)), ("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new AusgabeEintrag(leser.GetInt64(0), leser.GetInt64(1), AusIso(leser.GetString(2)), Enum.Parse<Einnahmezeit>(leser.GetString(3), true),
                leser.GetInt64(4), Enum.Parse<AusgabeStatus>(leser.GetString(5), true), leser.GetString(6), AusZeit(leser.GetString(7)),
                leser.IsDBNull(8) ? null : leser.GetInt64(8)));
        return liste;
    }

    // Die Liste einer Einnahmezeit für die Bewohner; abwesend = laut Ausgangsliste (für die Vorbelegung „abwesend“).
    public List<AusgabeZeile> Ausgabeliste(IEnumerable<Bewohner> bewohner, DateOnly tag, Einnahmezeit zeit, IReadOnlySet<long>? abwesend = null)
    {
        var liste = bewohner.ToList();
        var plaene = liste.ToDictionary(b => b.Id!.Value, b => Verordnungen(b.Id!.Value));
        return AusgabeRegeln.Zeilen(liste, plaene, tag, zeit, AusgabeEintraege(tag, tag), abwesend ?? new HashSet<long>());
    }

    // Status setzen – für alle Medikamente der Zeit (verordnungen = null) oder nur für die gewählten.
    public void AusgabeSetzen(long bewohnerId, DateOnly tag, Einnahmezeit zeit, AusgabeStatus status, string kuerzel, DateTime jetzt,
        IReadOnlyCollection<long>? verordnungen = null)
    {
        Recht(kuerzel, Funktionen.MedikamenteAusgeben);
        var b = Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var zeile = Ausgabeliste([b], tag, zeit).SingleOrDefault() ?? throw new Eingabefehler([$"{b.Name} bekommt {AusgabeRegeln.ZeitText(zeit)} nichts laut Plan."]);
        var gewaehlt = zeile.Medikamente.Where(m => verordnungen is null || verordnungen.Contains(m.Verordnung.Id)).ToList();
        if (gewaehlt.Count == 0) throw new Eingabefehler(["Bitte mindestens ein Medikament wählen."]);
        if (tag > DateOnly.FromDateTime(jetzt)) throw new Eingabefehler(["Eine Ausgabe in der Zukunft lässt sich nicht bestätigen."]);
        using var transaktion = db.BeginTransaction();
        long? bericht = AusgabeRegeln.MitBericht(status)
            ? BerichtEinfuegen(new Bericht
            {
                BewohnerId = bewohnerId, Zeitpunkt = jetzt, Schicht = BerichtRegeln.SchichtZu(jetzt), Verfasser = kuerzel.Trim(), Bereiche = [5],
                Text = AusgabeRegeln.BerichtText(zeit, status, gewaehlt), Erfasst = jetzt, Art = DokuRegeln.ArtMedikation,
            }, transaktion)
            : null;
        foreach (var m in gewaehlt) AusgabeEinfuegen(bewohnerId, tag, zeit, m.Verordnung.Id, status, kuerzel.Trim(), jetzt, bericht, transaktion);
        transaktion.Commit();
        AusgabeGeaendert?.Invoke();
    }

    private void AusgabeEinfuegen(long bewohnerId, DateOnly tag, Einnahmezeit zeit, long verordnung, AusgabeStatus status, string kuerzel, DateTime erfasst,
        long? bericht, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO medikamentenausgaben (bewohner_id, tag, zeit, verordnung_id, status, kuerzel, erfasst, bericht_id)
            VALUES ($b, $t, $z, $v, $s, $k, $e, $r)
            """, transaktion, ("$b", bewohnerId), ("$t", Iso(tag)), ("$z", zeit.ToString().ToLowerInvariant()), ("$v", verordnung),
            ("$s", status.ToString().ToLowerInvariant()), ("$k", kuerzel), ("$e", Zeit(erfasst)), ("$r", bericht));

    // Nach dem Dienstwechsel einmal: Wer im vorigen Dienst seine Medikamente nicht bekommen hat (nicht abgeholt oder ohne
    // Eintrag), steht als Sammeleintrag je Wohnbereich in der Übergabe.
    public int AusgabeUebergabeErzeugen(DateTime jetzt)
    {
        var vorher = SchichtTermin.Zu(SchichtTermin.Zu(jetzt).Beginn.AddMinutes(-1));
        string schluessel = $"ausgabe_uebergabe:{Iso(vorher.Tag)}:{BerichtRegeln.Kuerzel(vorher.Schicht)}";
        if (Meta(schluessel) is not null) return 0;
        var bewohner = AlleBewohner();
        var offen = new List<(string Bereich, Einnahmezeit Zeit, string Name, string Stand)>();
        foreach (var zeit in AusgabeRegeln.ZeitenDesDienstes(vorher.Schicht))
            foreach (var z in Ausgabeliste(bewohner, vorher.Tag, zeit).Where(z => !z.Erledigt))
                offen.Add((z.Bewohner.Wohnbereich, zeit, z.Bewohner.Name, z.NochOffen ? "ohne Eintrag" : z.NichtAbgeholt ? "nicht abgeholt" : "teilweise"));
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT OR IGNORE INTO meta (schluessel, wert) VALUES ($s, '1')", transaktion, ("$s", schluessel));
        if (Convert.ToInt32(WertIn(transaktion, "SELECT changes()"), CultureInfo.InvariantCulture) == 0) return 0;
        int neu = 0;
        foreach (var gruppe in offen.GroupBy(o => o.Bereich).OrderBy(g => g.Key))
        {
            UebergabeEinfuegen(null, null, AusgabeRegeln.UebergabeText(gruppe.Key, vorher, [.. gruppe.Select(o => (o.Zeit, o.Name, o.Stand))]), jetzt,
                MassnahmenRegeln.UebergabeVerfasser, UebergabeRegeln.GiltBis(UebergabeDauer.NaechsteSchicht, jetzt, null), transaktion);
            neu++;
        }
        transaktion.Commit();
        return neu;
    }

    // Demo: die Ausgaben der letzten 14 Tage (meist ausgegeben, einige nicht abgeholt oder abgelehnt, mit Bericht); heute ist
    // die laufende Einnahmezeit noch offen.
    private void DemoAusgabeEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("""
            DELETE FROM berichte WHERE id IN (SELECT bericht_id FROM medikamentenausgaben WHERE bericht_id IS NOT NULL);
            DELETE FROM medikamentenausgaben;
            """, transaktion);
        var jetzt = Uhr();
        var (heute, laufend) = AusgabeRegeln.Jetzt(jetzt);
        var bewohner = LesenIn(transaktion, AktiveBewohnerSql);
        foreach (var b in bewohner)
        {
            var plan = VerordnungenIn(b.Id!.Value, transaktion);
            for (var tag = heute.AddDays(-14); tag <= heute; tag = tag.AddDays(1))
                foreach (var zeit in AusgabeRegeln.Zeiten)
                {
                    if (tag == heute && zeit >= laufend) continue;
                    var faellig = plan.Where(v => AusgabeRegeln.Faellig(v, tag, zeit) is not null).ToList();
                    if (faellig.Count == 0) continue;
                    var (status, kuerzel) = Demodaten.Ausgabe(b.Id!.Value, tag, zeit);
                    var zeitpunkt = tag.ToDateTime(zeit switch { Einnahmezeit.Morgens => new(8, 5), Einnahmezeit.Mittags => new(12, 10), Einnahmezeit.Abends => new(18, 5), _ => new(21, 40) });
                    long? bericht = AusgabeRegeln.MitBericht(status)
                        ? BerichtEinfuegen(new Bericht
                        {
                            BewohnerId = b.Id!.Value, Zeitpunkt = zeitpunkt, Schicht = BerichtRegeln.SchichtZu(zeitpunkt), Verfasser = kuerzel, Bereiche = [5],
                            Text = AusgabeRegeln.BerichtText(zeit, status, faellig.Select(v => new AusgabeMedikament(v, AusgabeRegeln.Faellig(v, tag, zeit)!, null))),
                            Erfasst = zeitpunkt, Art = DokuRegeln.ArtMedikation,
                        }, transaktion)
                        : null;
                    foreach (var v in faellig) AusgabeEinfuegen(b.Id!.Value, tag, zeit, v.Id, status, kuerzel, zeitpunkt, bericht, transaktion);
                }
        }
        SetzeMeta(DemoAusgabe, "1", transaktion);
    }
}
