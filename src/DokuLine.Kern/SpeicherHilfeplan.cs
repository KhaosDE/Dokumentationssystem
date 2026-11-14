using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Hilfeplangespräche im Speicher (Bauplan 47): Ereignisse „faellig“, „termin“, „gefuehrt“ (nie geändert). Ein Termin ist ein
// Kalendereintrag des Bewohners; das Ergebnis ein Bericht der Art „Hilfeplangespräch“.
public sealed partial class Speicher
{
    private const string DemoHilfeplan = "demo_hilfeplan";

    public event Action? HilfeplanGeaendert;

    private List<HilfeplanEreignis> HilfeplanEreignisse(SqliteTransaction? transaktion = null)
    {
        var liste = new List<HilfeplanEreignis>();
        using var befehl = Befehl("SELECT id, bewohner_id, art, datum, kalender_id, teilnehmer, bericht_id, kuerzel, erfasst FROM hilfeplan ORDER BY erfasst, id", transaktion, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new HilfeplanEreignis(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), AusIso(leser.GetString(3)), leser.IsDBNull(4) ? null : leser.GetInt64(4),
                leser.GetString(5), leser.IsDBNull(6) ? null : leser.GetInt64(6), leser.GetString(7), AusZeit(leser.GetString(8))));
        return liste;
    }

    public List<HilfeplanStand> Hilfeplaene(IEnumerable<Bewohner> bewohner, DateOnly heute)
    {
        var ereignisse = HilfeplanEreignisse().ToLookup(e => e.BewohnerId);
        var kalender = KalenderLesen(null).ToDictionary(k => k.Id);
        return [.. bewohner.Select(b => HilfeplanRegeln.Stand(b, [.. ereignisse[b.Id!.Value]],
            Angaben(b.Id!.Value, heute).GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>(), kalender))];
    }

    public HilfeplanStand Hilfeplan(long bewohnerId, DateOnly heute) =>
        Hilfeplaene([Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."])], heute)[0];

    private void HilfeplanEreignisEinfuegen(long bewohnerId, string art, DateOnly datum, long? kalenderId, string teilnehmer, long? berichtId, string kuerzel,
        DateTime jetzt, SqliteTransaction? transaktion) =>
        Ausfuehren("INSERT INTO hilfeplan (bewohner_id, art, datum, kalender_id, teilnehmer, bericht_id, kuerzel, erfasst) VALUES ($b, $a, $d, $k, $t, $r, $u, $e)",
            transaktion, ("$b", bewohnerId), ("$a", art), ("$d", Iso(datum)), ("$k", kalenderId), ("$t", teilnehmer), ("$r", berichtId), ("$u", kuerzel.Trim()),
            ("$e", Zeit(jetzt)));

    private void HilfeplanRecht(string kuerzel, long bewohnerId)
    {
        if (Grund(kuerzel, Funktionen.BewohnerBearbeiten) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (Archiviert(bewohnerId)) throw new Eingabefehler(["Dieser Bewohner ist entlassen."]);
    }

    public void HilfeplanFaelligSetzen(long bewohnerId, DateOnly bis, string kuerzel, DateTime jetzt)
    {
        HilfeplanRecht(kuerzel, bewohnerId);
        HilfeplanEreignisEinfuegen(bewohnerId, "faellig", bis, null, "", null, kuerzel, jetzt, null);
        HilfeplanGeaendert?.Invoke();
    }

    // Teilnehmer aus der Akte (Vorschlag für „Termin vergeben …“).
    public List<string> HilfeplanTeilnehmer(long bewohnerId, DateOnly heute)
    {
        var b = Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var angaben = Angaben(bewohnerId, heute);
        var namen = Personen().ToDictionary(p => p.Id, p => p);
        var angehoerige = (angaben.GetValueOrDefault(Angabe.Schweigepflicht)?.Lesen<Schweigepflicht>()?.Eintraege ?? [])
            .Where(e => SchweigepflichtRegeln.Wirksam(e, heute) == Entbindungsstand.Entbunden && e.PersonId is long p && namen.TryGetValue(p, out var person) && person.Gruppe == PersonGruppe.Angehoerige)
            .Select(e => namen[e.PersonId!.Value].Name);
        var bezug = Bezug(bewohnerId).Bezugspflege?.Kuerzel;
        string? bezugName = bezug is null ? null : MitarbeiterLesen(bezug)?.Name ?? bezug;
        return HilfeplanRegeln.TeilnehmerVorschlag(b, bezugName, Betreuer(bewohnerId), angaben.GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>(),
            angaben.GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>(), angehoerige);
    }

    // Andere Termine des Bewohners zur selben Zeit (Therapie, Kalender, Regelwoche).
    public List<string> HilfeplanUeberschneidungen(long bewohnerId, DateTime beginn, DateTime ende) =>
        HilfeplanRegeln.Ueberschneidungen(Woche(bewohnerId, WochenplanRegeln.Montag(DateOnly.FromDateTime(beginn))), beginn, ende);

    // Termin vergeben: ein Kalendereintrag des Bewohners (Wochenplan, Tagesplan) und das Ereignis mit den Teilnehmern.
    public KalenderTermin HilfeplanTerminVergeben(long bewohnerId, DateTime beginn, int minuten, string ort, IReadOnlyList<string> teilnehmer, string kuerzel, DateTime jetzt)
    {
        HilfeplanRecht(kuerzel, bewohnerId);
        if (minuten is < 15 or > 480) throw new Eingabefehler(["Dauer bitte zwischen 15 Minuten und 8 Stunden."]);
        if (teilnehmer.Count == 0) throw new Eingabefehler(["Bitte mindestens einen Teilnehmer wählen."]);
        var termin = KalenderAnlegen(new KalenderFassung
        {
            Art = KalenderArt.Bewohner, Titel = HilfeplanRegeln.Titel, Beginn = beginn, Ende = beginn.AddMinutes(minuten), Ort = ort.Trim(),
            Hinweis = "Teilnehmer: " + string.Join(", ", teilnehmer), Bewohner = [bewohnerId], Erstellt = jetzt, Kuerzel = kuerzel.Trim(),
        });
        HilfeplanEreignisEinfuegen(bewohnerId, "termin", DateOnly.FromDateTime(beginn), termin.Id, string.Join("; ", teilnehmer), null, kuerzel, jetzt, null);
        HilfeplanGeaendert?.Invoke();
        return termin;
    }

    // Vorbereitung als Aufgaben des Bereichs, fällig am Tag vor dem Gespräch.
    public List<Aufgabe> HilfeplanVorbereiten(long bewohnerId, IEnumerable<string> punkte, string kuerzel, DateTime jetzt)
    {
        HilfeplanRecht(kuerzel, bewohnerId);
        var stand = Hilfeplan(bewohnerId, DateOnly.FromDateTime(jetzt));
        if (stand.Termin is not KalenderTermin t) throw new Eingabefehler(["Zuerst einen Termin vergeben."]);
        var b = stand.Bewohner;
        return [.. punkte.Select(p => AufgabeSpeichern(new Aufgabe
        {
            BewohnerId = bewohnerId, Bereich = b.Wohnbereich, Text = HilfeplanRegeln.VorbereitungText(p, b, t.Aktuell.Beginn), Fuer = AufgabeFuer.WerZeitHat,
            FaelligBis = t.Aktuell.Beginn.Date.AddDays(-1).AddHours(18), Erstellt = jetzt, Verfasser = kuerzel.Trim(),
        }))];
    }

    // Nach dem Gespräch: „geführt am …“, Ergebnis als Bericht, nächste Fälligkeit.
    public Bericht HilfeplanGefuehrt(long bewohnerId, DateOnly am, string ergebnis, DateOnly? naechstes, string kuerzel, DateTime jetzt)
    {
        HilfeplanRecht(kuerzel, bewohnerId);
        if (ergebnis.Trim().Length == 0) throw new Eingabefehler(["Bitte das Ergebnis kurz festhalten."]);
        if (am > DateOnly.FromDateTime(jetzt)) throw new Eingabefehler(["Das Gespräch liegt in der Zukunft."]);
        if (naechstes is DateOnly n && n <= am) throw new Eingabefehler(["Die nächste Fälligkeit muss nach dem Gespräch liegen."]);
        var bericht = BerichtSpeichern(new Bericht
        {
            BewohnerId = bewohnerId, Zeitpunkt = am.ToDateTime(TimeOnly.FromDateTime(jetzt)), Schicht = BerichtRegeln.SchichtZu(am.ToDateTime(TimeOnly.FromDateTime(jetzt))),
            Verfasser = kuerzel.Trim(), Text = ergebnis.Trim(), Bereiche = [], Erfasst = jetzt, Art = HilfeplanRegeln.ArtBericht,
        });
        using var transaktion = db.BeginTransaction();
        HilfeplanEreignisEinfuegen(bewohnerId, "gefuehrt", am, null, "", bericht.Id, kuerzel, jetzt, transaktion);
        if (naechstes is DateOnly bis) HilfeplanEreignisEinfuegen(bewohnerId, "faellig", bis, null, "", null, kuerzel, jetzt, transaktion);   // Reihenfolge über die Nummer
        transaktion.Commit();
        HilfeplanGeaendert?.Invoke();
        return bericht;
    }

    // Demo: letzte Gespräche vor einigen Monaten; einige überfällig, einige bald fällig, einer mit Termin.
    private void DemoHilfeplanEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM hilfeplan", transaktion);
        var jetzt = Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        var bewohner = LesenIn(transaktion, AktiveBewohnerSql);
        bewohner.Sort(BewohnerRegeln.NachName);
        for (int i = 0; i < bewohner.Count; i++)
        {
            long id = bewohner[i].Id!.Value;
            if (i % 4 == 3) continue;   // nur der Vorschlag aus der Bewilligung
            var letztes = heute.AddDays(-300 - i * 7 % 120);
            HilfeplanEreignisEinfuegen(id, "gefuehrt", letztes, null, "", null, "EF", letztes.ToDateTime(new TimeOnly(15, 0)), transaktion);
            var faellig = i % 6 == 1 ? heute.AddDays(-10) : heute.AddDays(14 + i % 9 * 14);
            HilfeplanEreignisEinfuegen(id, "faellig", faellig, null, "", null, "EF", letztes.ToDateTime(new TimeOnly(15, 5)), transaktion);
        }
        SetzeMeta(DemoHilfeplan, "1", transaktion);
    }
}
