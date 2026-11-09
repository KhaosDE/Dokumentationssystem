using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Bedarfsgaben (0.33.0): eintragen mit Bericht in einem Zug, lesen je Verordnung. Nichts wird überschrieben.
public sealed partial class Speicher
{
    public List<Bedarfsgabe> Bedarfsgaben(long verordnungId) => BedarfsgabenLesen("WHERE verordnung_id = $v", ("$v", verordnungId));

    public List<Bedarfsgabe> BedarfsgabenBewohner(long bewohnerId) => BedarfsgabenLesen("WHERE bewohner_id = $b", ("$b", bewohnerId));

    // Alle Gaben desselben Medikaments an diesen Bewohner, auch aus anderen Bedarfsverordnungen (0.33.2): Für die
    // Anzeige „in den letzten 24 Stunden“ und die Höchstmenge zählt, was er davon bekommen hat.
    public List<Bedarfsgabe> BedarfsgabenMittel(Verordnung v) => BedarfsgabenLesen(
        "WHERE bewohner_id = $b AND verordnung_id IN (SELECT id FROM verordnungen WHERE bewohner_id = $b AND medikament_id = $m)",
        ("$b", v.BewohnerId), ("$m", v.MedikamentId));

    private List<Bedarfsgabe> BedarfsgabenLesen(string bedingung, params (string, object?)[] parameter)
    {
        var liste = new List<Bedarfsgabe>();
        using var befehl = Befehl($"""
            SELECT id, verordnung_id, bewohner_id, zeit, menge, anlass, kuerzel, absprache, erstellt, bericht_id
            FROM bedarfsgaben {bedingung} ORDER BY zeit, id
            """, null, parameter);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Bedarfsgabe
            {
                Id = leser.GetInt64(0), VerordnungId = leser.GetInt64(1), BewohnerId = leser.GetInt64(2), Zeit = AusZeit(leser.GetString(3)),
                Menge = leser.GetString(4), Anlass = leser.GetString(5), Kuerzel = leser.GetString(6),
                Absprache = leser.IsDBNull(7) ? null : leser.GetString(7), Erstellt = AusZeit(leser.GetString(8)),
                BerichtId = leser.IsDBNull(9) ? null : leser.GetInt64(9),
            });
        return liste;
    }

    // „Bedarf gegeben …“: prüft (Recht, Absprache bei Assistenzkräften), trägt die Gabe und den Bericht ein.
    public Bedarfsgabe BedarfGeben(BedarfEingabe e, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.BedarfGeben) is string grund) throw new Eingabefehler([grund]);
        var v = Verordnung(e.VerordnungId) ?? throw new Eingabefehler(["Diese Verordnung gibt es nicht mehr."]);
        var mitarbeiter = AlleMitarbeiter();
        var ich = mitarbeiter.FirstOrDefault(m => m.Kuerzel.Equals(kuerzel, StringComparison.OrdinalIgnoreCase));
        var gabe = BedarfRegeln.Pruefen(e, v, ich, mitarbeiter.Where(m => m.Aktiv && m.Fachkraft).ToList(), jetzt);
        string? name = gabe.Absprache is string a ? mitarbeiter.FirstOrDefault(m => m.Kuerzel == a)?.Name : null;
        using var transaktion = db.BeginTransaction();
        long id = BedarfEinfuegen(v, gabe, name, transaktion);
        transaktion.Commit();
        MedikationGeaendert?.Invoke();
        return Bedarfsgaben(v.Id).Single(g => g.Id == id);
    }

    private long BedarfEinfuegen(Verordnung v, Bedarfsgabe g, string? abspracheName, SqliteTransaction transaktion)
    {
        // Der Bericht: Lebensbereich 5 (Umgang mit den Auswirkungen der Behinderung), wie bei Visiten.
        long berichtId = BerichtEinfuegen(new Bericht
        {
            BewohnerId = v.BewohnerId, Zeitpunkt = g.Zeit, Schicht = BerichtRegeln.SchichtZu(g.Zeit), Verfasser = g.Kuerzel,
            Bereiche = [5], Text = BedarfRegeln.BerichtText(v, g, abspracheName), Erfasst = g.Erstellt,
        }, transaktion);
        Ausfuehren("""
            INSERT INTO bedarfsgaben (verordnung_id, bewohner_id, zeit, menge, anlass, kuerzel, absprache, erstellt, bericht_id)
            VALUES ($v, $b, $z, $m, $a, $k, $f, $e, $r)
            """, transaktion,
            ("$v", v.Id), ("$b", v.BewohnerId), ("$z", Zeit(g.Zeit)), ("$m", g.Menge), ("$a", g.Anlass), ("$k", g.Kuerzel),
            ("$f", g.Absprache), ("$e", Zeit(g.Erstellt)), ("$r", berichtId));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // Demo: ausgedachte Gaben an den Bedarfsverordnungen (daten/demo/bedarf.json), mit Bericht.
    private void DemoBedarfEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM berichte WHERE id IN (SELECT bericht_id FROM bedarfsgaben WHERE bericht_id IS NOT NULL); DELETE FROM bedarfsgaben", transaktion);
        var namen = new Dictionary<string, string>();
        using (var befehl = Befehl("SELECT kuerzel, vorname || ' ' || nachname FROM kollegen", transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read()) namen[leser.GetString(0)] = leser.GetString(1);
        var medikamente = MedikamenteLesen(transaktion, "").ToDictionary(m => m.Id, m => m.Anzeige);
        var jetzt = Uhr();
        foreach (var d in Demodaten.Bedarf())
        {
            // Nur, was der Bericht braucht (innerhalb der Transaktion gelesen).
            Verordnung? v = null;
            using (var befehl = Befehl("""
                SELECT v.id, v.bewohner_id, v.medikament_id, v.staerke, v.einheit, v.bedarf_anlass FROM verordnungen v
                JOIN bewohner b ON b.id = v.bewohner_id WHERE v.bedarf = 1 AND b.nachname = $n ORDER BY v.id LIMIT 1
                """, transaktion, [("$n", d.Bewohner)]))
            using (var leser = befehl.ExecuteReader())
                if (leser.Read())
                    v = new Verordnung
                    {
                        Id = leser.GetInt64(0), BewohnerId = leser.GetInt64(1), Medikament = medikamente.GetValueOrDefault(leser.GetInt64(2), "?"),
                        Staerke = leser.GetString(3), Einheit = leser.GetString(4), Bedarf = true, BedarfAnlass = leser.GetString(5),
                    };
            if (v is null) continue;
            var zeit = jetzt.AddHours(-d.VorStunden);
            var gabe = new Bedarfsgabe
            {
                VerordnungId = v.Id, BewohnerId = v.BewohnerId, Zeit = new DateTime(zeit.Year, zeit.Month, zeit.Day, zeit.Hour, zeit.Minute, 0),
                Menge = d.Menge, Anlass = d.Anlass.Length > 0 ? d.Anlass : v.BedarfAnlass, Kuerzel = d.Kuerzel, Absprache = d.Absprache,
                Erstellt = zeit.AddMinutes(2),
            };
            BedarfEinfuegen(v, gabe, d.Absprache is string a ? namen.GetValueOrDefault(a) : null, transaktion);
        }
        SetzeMeta(DemoBedarf, "1", transaktion);
    }
}
