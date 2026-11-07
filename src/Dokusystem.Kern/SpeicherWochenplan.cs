using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Wochenplan im Speicher: freie Einträge der Regelwoche (ändern = „ab Datum“ neu, die alte
// Fassung endet), „entfernt ab“ als eigener Eintrag, Teilnahme an Therapien je Tag (der neueste
// Eintrag gilt, frühere bleiben). Nichts wird überschrieben.
public sealed partial class Speicher
{
    public event Action? WochenplanGeaendert;

    public List<PlanEintrag> PlanEintraege(long bewohnerId) =>
        PlanEintraegeLesen(null, "WHERE p.bewohner_id = $b", ("$b", bewohnerId));

    public PlanEintrag PlanEintragAnlegen(PlanEintrag e)
    {
        if (Bewohner(e.BewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        using var transaktion = db.BeginTransaction();
        long id = PlanEintragEinfuegen(e, transaktion);
        transaktion.Commit();
        WochenplanGeaendert?.Invoke();
        return PlanEintraege(e.BewohnerId).Single(x => x.Id == id);
    }

    // Ändern ab Datum: der bisherige Eintrag endet an diesem Tag, der neue beginnt.
    public PlanEintrag PlanEintragAendern(long id, PlanEintrag neu)
    {
        var alt = PlanEintraegeLesen(null, "WHERE p.id = $id", ("$id", id)).FirstOrDefault()
                  ?? throw new Eingabefehler(["Diesen Eintrag gibt es nicht mehr."]);
        if (alt.EndetAb is not null) throw new Eingabefehler(["Dieser Eintrag ist schon beendet."]);
        if (neu.Ab < alt.Ab) throw new Eingabefehler([$"„Gilt ab“ darf nicht vor dem Beginn des Eintrags liegen ({Datum.Text(alt.Ab)})."]);
        using var transaktion = db.BeginTransaction();
        PlanEndeEinfuegen(id, neu.Ab, neu.Erstellt, neu.Kuerzel, transaktion);
        long neueId = PlanEintragEinfuegen(neu with { BewohnerId = alt.BewohnerId, Ersetzt = id }, transaktion);
        transaktion.Commit();
        WochenplanGeaendert?.Invoke();
        return PlanEintraege(alt.BewohnerId).Single(x => x.Id == neueId);
    }

    public void PlanEintragEntfernen(long id, DateOnly ab, string kuerzel, DateTime jetzt)
    {
        var alt = PlanEintraegeLesen(null, "WHERE p.id = $id", ("$id", id)).FirstOrDefault()
                  ?? throw new Eingabefehler(["Diesen Eintrag gibt es nicht mehr."]);
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (alt.EndetAb is not null) fehler.Add("Dieser Eintrag ist schon beendet.");
        if (ab < alt.Ab) fehler.Add($"„Ab“ darf nicht vor dem Beginn des Eintrags liegen ({Datum.Text(alt.Ab)}).");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        using var transaktion = db.BeginTransaction();
        PlanEndeEinfuegen(id, ab, jetzt, kuerzel.Trim(), transaktion);
        transaktion.Commit();
        WochenplanGeaendert?.Invoke();
    }

    private long PlanEintragEinfuegen(PlanEintrag e, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO plan_eintraege (bewohner_id, tag, von, bis, titel, ort, hinweis, ab, ersetzt, erstellt, kuerzel)
            VALUES ($b, $t, $v, $bi, $ti, $o, $h, $ab, $er, $e, $k)
            """, transaktion,
            ("$b", e.BewohnerId), ("$t", (int)e.Termin.Tag), ("$v", e.Termin.Von.ToString("HH:mm", CultureInfo.InvariantCulture)),
            ("$bi", e.Termin.Bis.ToString("HH:mm", CultureInfo.InvariantCulture)), ("$ti", e.Titel), ("$o", e.Ort), ("$h", e.Hinweis),
            ("$ab", Iso(e.Ab)), ("$er", e.Ersetzt), ("$e", Zeit(e.Erstellt)), ("$k", e.Kuerzel));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    private void PlanEndeEinfuegen(long id, DateOnly ab, DateTime jetzt, string kuerzel, SqliteTransaction transaktion) =>
        Ausfuehren("INSERT INTO plan_ende (eintrag_id, ab, erstellt, kuerzel) VALUES ($i, $ab, $e, $k)", transaktion,
            ("$i", id), ("$ab", Iso(ab)), ("$e", Zeit(jetzt)), ("$k", kuerzel));

    private List<PlanEintrag> PlanEintraegeLesen(SqliteTransaction? transaktion, string bedingung, params (string, object?)[] parameter)
    {
        var liste = new List<PlanEintrag>();
        using var befehl = Befehl($"""
            SELECT p.id, p.bewohner_id, p.tag, p.von, p.bis, p.titel, p.ort, p.hinweis, p.ab, p.ersetzt, p.erstellt, p.kuerzel,
                   e.ab, e.kuerzel, e.erstellt
            FROM plan_eintraege p LEFT JOIN plan_ende e ON e.eintrag_id = p.id {bedingung} ORDER BY p.tag, p.von, p.id
            """, transaktion, parameter);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new PlanEintrag
            {
                Id = leser.GetInt64(0), BewohnerId = leser.GetInt64(1),
                Termin = new Wochentermin((DayOfWeek)leser.GetInt32(2), TimeOnly.ParseExact(leser.GetString(3), "HH:mm", CultureInfo.InvariantCulture),
                    TimeOnly.ParseExact(leser.GetString(4), "HH:mm", CultureInfo.InvariantCulture)),
                Titel = leser.GetString(5), Ort = leser.GetString(6), Hinweis = leser.GetString(7), Ab = AusIso(leser.GetString(8)),
                Ersetzt = leser.IsDBNull(9) ? null : leser.GetInt64(9), Erstellt = AusZeit(leser.GetString(10)), Kuerzel = leser.GetString(11),
                EndetAb = leser.IsDBNull(12) ? null : AusIso(leser.GetString(12)), EndetVon = leser.IsDBNull(13) ? null : leser.GetString(13),
                EndetErstellt = leser.IsDBNull(14) ? null : AusZeit(leser.GetString(14)),
            });
        return liste;
    }

    // ----- Teilnahme an Therapien -----

    public void TeilnahmeSetzen(long zuteilungId, DateOnly tag, Teilnahme status, string kuerzel, DateTime jetzt)
    {
        var treffer = Angebote().SelectMany(a => a.Zuteilungen.Select(z => (a, z))).FirstOrDefault(x => x.z.Id == zuteilungId);
        if (treffer.z is null) throw new Eingabefehler(["Diese Zuteilung gibt es nicht mehr."]);
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (!treffer.z.GiltAm(tag) || !treffer.a.Am(tag).Termine.Any(t => t.Tag == tag.DayOfWeek))
            fehler.Add("An diesem Tag ist kein Termin dieser Therapie.");
        if (tag > DateOnly.FromDateTime(jetzt) && status == Teilnahme.Teilgenommen) fehler.Add("„Teilgenommen“ geht erst am Tag selbst.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        Ausfuehren("INSERT INTO teilnahmen (zuteilung_id, tag, status, erstellt, kuerzel) VALUES ($z, $t, $s, $e, $k)", null,
            ("$z", zuteilungId), ("$t", Iso(tag)), ("$s", WochenplanRegeln.TeilnahmeKuerzel(status)), ("$e", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        WochenplanGeaendert?.Invoke();
    }

    // Die zuletzt eingetragene Teilnahme je (Zuteilung, Tag).
    public Dictionary<(long ZuteilungId, DateOnly Tag), Teilnahme> Teilnahmen()
    {
        var ergebnis = new Dictionary<(long, DateOnly), Teilnahme>();
        using var befehl = Befehl("SELECT zuteilung_id, tag, status FROM teilnahmen ORDER BY erstellt, id", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) ergebnis[(leser.GetInt64(0), AusIso(leser.GetString(1)))] = WochenplanRegeln.TeilnahmeAus(leser.GetString(2));
        return ergebnis;
    }

    // Die Termine einer Woche (Montag bis Sonntag) für einen Bewohner.
    public List<Termin> Woche(long bewohnerId, DateOnly montag)
    {
        var therapien = Angebote().SelectMany(a => a.Zuteilungen.Where(z => z.BewohnerId == bewohnerId).Select(z => (a, z))).ToList();
        return WochenplanRegeln.Woche(montag, PlanEintraege(bewohnerId), therapien, Teilnahmen());
    }

    public List<string> PlanUeberschneidungen(PlanEintrag neu)
    {
        var therapien = Angebote().SelectMany(a => a.Zuteilungen.Where(z => z.BewohnerId == neu.BewohnerId).Select(z => (a, z))).ToList();
        return WochenplanRegeln.Ueberschneidungen(neu, PlanEintraege(neu.BewohnerId), therapien);
    }

    // Demo: freie Einträge der Regelwoche (eine Änderung, ein entfernter) und Teilnahmen der
    // letzten vier Wochen.
    private void DemoWochenplanEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM plan_eintraege; DELETE FROM teilnahmen;", transaktion);
        foreach (var d in Demodaten.Wochenplan(bewohner, Uhr()))
        {
            long id = PlanEintragEinfuegen(d.Eintrag, transaktion);
            if (d.Ersatz is PlanEintrag ersatz)
            {
                PlanEndeEinfuegen(id, ersatz.Ab, ersatz.Erstellt, ersatz.Kuerzel, transaktion);
                PlanEintragEinfuegen(ersatz with { Ersetzt = id }, transaktion);
            }
            else if (d.EntferntAb is DateOnly ab)
                PlanEndeEinfuegen(id, ab, Uhr().AddDays(-4), "AB", transaktion);
        }
        var heute = DateOnly.FromDateTime(Uhr());
        foreach (var a in AngeboteLesen(transaktion, ""))
            foreach (var z in a.Zuteilungen)
                for (var tag = heute.AddDays(-28); tag < heute; tag = tag.AddDays(1))
                {
                    if (!z.GiltAm(tag) || !a.Am(tag).Termine.Any(t => t.Tag == tag.DayOfWeek)) continue;
                    // Fest verteilt, nicht zufällig: meist teilgenommen, ab und zu abgesagt oder ausgefallen.
                    int wert = (int)((z.Id * 7 + tag.DayNumber) % 10);
                    var status = wert switch { 0 => Teilnahme.Abgesagt, 1 => Teilnahme.Ausgefallen, _ => Teilnahme.Teilgenommen };
                    Ausfuehren("INSERT INTO teilnahmen (zuteilung_id, tag, status, erstellt, kuerzel) VALUES ($z, $t, $s, $e, $k)", transaktion,
                        ("$z", z.Id), ("$t", Iso(tag)), ("$s", WochenplanRegeln.TeilnahmeKuerzel(status)),
                        ("$e", Zeit(tag.ToDateTime(new TimeOnly(18, 0)))), ("$k", "OP"));
                }
        SetzeMeta(DemoWochenplan, "1", transaktion);
    }
}
