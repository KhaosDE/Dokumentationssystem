using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Personenverzeichnis im Speicher: anlegen, ändern, inaktiv setzen (nie löschen), Zuordnungen zu Bewohnern,
// einmalige Übernahme der Betreuer aus der Akte, Demo-Daten.
public sealed partial class Speicher
{
    public event Action? AerzteGeaendert;   // jede Änderung am Personenverzeichnis (Name von 26b geblieben)

    private const string Personenspalten = "id, gruppe, name, art, fachrichtung, praxis, telefon, mobil, email, anschrift, erreichbarkeit, hinweis, aktiv";

    public List<Person> Personen(PersonGruppe? gruppe = null, bool nurAktive = false)
    {
        var liste = new List<Person>();
        using var befehl = Befehl($"SELECT {Personenspalten} FROM aerzte" + (gruppe is null ? "" : " WHERE gruppe = $g") + " ORDER BY name COLLATE NOCASE",
            null, gruppe is PersonGruppe g ? [("$g", PersonRegeln.Schluessel(g))] : []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(PersonLesen(leser));
        return nurAktive ? liste.Where(a => a.Aktiv).ToList() : liste;
    }

    private static Person PersonLesen(SqliteDataReader l) => new()
    {
        Id = l.GetInt64(0), Gruppe = PersonRegeln.AusSchluessel(l.GetString(1)), Name = l.GetString(2),
        Art = l.GetString(3), Fachrichtung = l.GetString(4),
        Praxis = l.GetString(5), Telefon = l.GetString(6), Mobil = l.GetString(7), Email = l.GetString(8),
        Anschrift = l.GetString(9),
        Erreichbarkeit = l.GetString(10), Hinweis = l.GetString(11), Aktiv = l.GetInt64(12) == 1,
    };

    public List<Person> Aerzte(bool nurAktive = false) => Personen(PersonGruppe.Arzt, nurAktive);

    public Person? PersonNach(long id) => Personen().FirstOrDefault(p => p.Id == id);

    public Person? ArztNach(string name) =>
        Aerzte().FirstOrDefault(a => a.Name.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase));

    // Warum der Angemeldete diese Gruppe nicht ändern darf (null = darf).
    public string? PersonGrund(string? kuerzel, PersonGruppe gruppe) => Grund(kuerzel, PersonRegeln.RechtFuer(gruppe));

    // Anlegen (Id 0) oder ändern. Namen sind je Verzeichnis eindeutig, damit die Auswahl eindeutig bleibt.
    public Person PersonSpeichern(Person a, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        if (PersonGrund(kuerzel, a.Gruppe) is string grund) throw new Eingabefehler([grund]);
        if (Personen(a.Gruppe).Any(x => x.Id != a.Id && x.Name.Equals(a.Name,
            StringComparison.CurrentCultureIgnoreCase)))
            throw new Eingabefehler([$"„{a.Name}“ steht schon im Verzeichnis {PersonRegeln.GruppeText(a.Gruppe)}."]);
        if (a.Id != 0 && PersonNach(a.Id) is Person alt && alt.Gruppe != a.Gruppe)
            throw new Eingabefehler(["Das Verzeichnis einer Person lässt sich nicht ändern – bitte neu anlegen."]);
        long id = a.Id;
        using (var transaktion = db.BeginTransaction())
        {
            if (id == 0) id = PersonEinfuegen(a, kuerzel, jetzt, transaktion);
            else
                Ausfuehren("""
                    UPDATE aerzte SET name = $n, art = $art, fachrichtung = $f, praxis = $p, telefon = $t, mobil = $m,
                        email = $e,
                        anschrift = $an, erreichbarkeit = $er, hinweis = $h, geaendert = $g, kuerzel = $k WHERE id = $id
                    """, transaktion, ("$n", a.Name), ("$art", a.Art), ("$f", a.Fachrichtung), ("$p", a.Praxis), ("$t", a.Telefon), ("$m", a.Mobil),
                    ("$e", a.Email), ("$an", a.Anschrift), ("$er", a.Erreichbarkeit), ("$h", a.Hinweis), ("$g",
                        Zeit(jetzt)), ("$k", kuerzel), ("$id", id));
            transaktion.Commit();
        }
        AerzteGeaendert?.Invoke();
        return PersonNach(id)!;
    }

    public void PersonAktiv(long id, bool aktiv, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        var p = PersonNach(id) ?? throw new Eingabefehler(["Diese Person gibt es nicht mehr."]);
        if (PersonGrund(kuerzel, p.Gruppe) is string grund) throw new Eingabefehler([grund]);
        Ausfuehren("UPDATE aerzte SET aktiv = $a, geaendert = $g, kuerzel = $k WHERE id = $id", null,
            ("$a", aktiv ? 1 : 0), ("$g", Zeit(jetzt)), ("$k", kuerzel), ("$id", id));
        AerzteGeaendert?.Invoke();
    }

    private long PersonEinfuegen(Person a, string kuerzel, DateTime jetzt, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO aerzte (gruppe, name, art, fachrichtung, praxis, telefon, mobil, email, anschrift,
                erreichbarkeit, hinweis, aktiv, geaendert, kuerzel)
            VALUES ($gr, $n, $art, $f, $p, $t, $m, $e, $an, $er, $h, $a, $g, $k)
            """, transaktion, ("$gr", PersonRegeln.Schluessel(a.Gruppe)), ("$n", a.Name), ("$art", a.Art), ("$f", a.Fachrichtung), ("$p", a.Praxis),
            ("$t", a.Telefon), ("$m", a.Mobil), ("$e", a.Email), ("$an", a.Anschrift), ("$er", a.Erreichbarkeit),
                ("$h", a.Hinweis), ("$a", a.Aktiv ? 1 : 0),
            ("$g", Zeit(jetzt)), ("$k", kuerzel));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // ----- Zuordnungen zu Bewohnern -----

    public List<PersonZuordnung> Zuordnungen(long? personId = null, long? bewohnerId = null)
    {
        var liste = new List<PersonZuordnung>();
        using var befehl = Befehl("""
            SELECT id, person_id, bewohner_id, rolle, geaendert, kuerzel FROM person_zuordnungen
            WHERE ($p IS NULL OR person_id = $p) AND ($b IS NULL OR bewohner_id = $b) ORDER BY id
            """, null, [("$p", personId), ("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new PersonZuordnung(leser.GetInt64(0), leser.GetInt64(1), leser.GetInt64(2), leser.GetString(3),
                AusZeit(leser.GetString(4)), leser.GetString(5)));
        return liste;
    }

    // Person einem Bewohner zuordnen (gleiche Rolle nur einmal).
    public PersonZuordnung ZuordnungSpeichern(long personId, long bewohnerId, string rolle, string kuerzel,
        DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        var p = PersonNach(personId) ?? throw new Eingabefehler(["Diese Person gibt es nicht mehr."]);
        if (PersonGrund(kuerzel, p.Gruppe) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        using (var transaktion = db.BeginTransaction())
        {
            ZuordnungEinfuegen(personId, bewohnerId, rolle.Trim(), kuerzel, jetzt, transaktion);
            transaktion.Commit();
        }
        AerzteGeaendert?.Invoke();
        return Zuordnungen(personId, bewohnerId).Last(z => z.Rolle == rolle.Trim());
    }

    public void ZuordnungEntfernen(long id, string kuerzel)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        var z = Zuordnungen().FirstOrDefault(x => x.Id == id)
            ?? throw new Eingabefehler(["Diese PersonZuordnung gibt es nicht mehr."]);
        if (PersonNach(z.PersonId) is Person p && PersonGrund(kuerzel,
            p.Gruppe) is string grund) throw new Eingabefehler([grund]);
        Ausfuehren("DELETE FROM person_zuordnungen WHERE id = $id", null, ("$id", id));
        AerzteGeaendert?.Invoke();
    }

    private void ZuordnungEinfuegen(long personId, long bewohnerId, string rolle, string kuerzel, DateTime jetzt,
        SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT OR IGNORE INTO person_zuordnungen (person_id, bewohner_id, rolle, geaendert, kuerzel)
            SELECT $p, $b, $r, $g,
                $k WHERE EXISTS (SELECT 1 FROM aerzte WHERE id = $p) AND EXISTS (SELECT 1 FROM bewohner WHERE id = $b)
            """, transaktion, ("$p", personId), ("$b", bewohnerId), ("$r", rolle), ("$g", Zeit(jetzt)), ("$k", kuerzel));

    // ----- Übernahme und Demo -----

    // Die Betreuer aus der Akte werden Personen „Betreuer“ mit Zuordnung. Gleicher
    // Name = dieselbe Person; „keine gesetzliche Betreuung“ bleibt ein Zustand in der Akte, keine Person.
    // Mehrfach aufrufbar: Vorhandenes bleibt.
    private void BetreuerUebernehmen(SqliteTransaction transaktion)
    {
        var aktuelle = new List<(long BewohnerId, string Name, string Stelle, string Email, string Telefon,
            string Kuerzel)>();
        using (var befehl = Befehl("""
            SELECT b.bewohner_id, b.name, b.stelle, b.email, b.telefon, b.kuerzel FROM betreuer b
            WHERE b.id = (SELECT MAX(id) FROM betreuer x WHERE x.bewohner_id = b.bewohner_id) AND b.keine = 0 AND TRIM(b.name) <> ''
            ORDER BY b.bewohner_id
            """, transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                aktuelle.Add((leser.GetInt64(0), leser.GetString(1).Trim(), leser.GetString(2), leser.GetString(3),
                    leser.GetString(4), leser.GetString(5)));
        var jetzt = Uhr();
        foreach (var b in aktuelle)
        {
            long id = WertIn(transaktion,
                "SELECT id FROM aerzte WHERE gruppe = 'betreuer' AND name = $n COLLATE NOCASE", ("$n",
                    b.Name)) is long vorhanden
                ? vorhanden
                : PersonEinfuegen(new Person
                {
                    Gruppe = PersonGruppe.Betreuer, Name = b.Name, Praxis = b.Stelle, Email = b.Email,
                    Telefon = b.Telefon,
                    Art = b.Stelle.Contains("Berufsbetreuer", StringComparison.OrdinalIgnoreCase) ? "Berufsbetreuer"
                        : b.Stelle.Contains("Verein", StringComparison.OrdinalIgnoreCase) ? "Betreuungsverein"
                        : b.Stelle.Contains("ehrenamtlich", StringComparison.OrdinalIgnoreCase) ? "ehrenamtlich" : "",
                }, b.Kuerzel.Length > 0 ? b.Kuerzel : "EF", jetzt, transaktion);
            ZuordnungEinfuegen(id, b.BewohnerId, "Betreuer", b.Kuerzel.Length > 0 ? b.Kuerzel : "EF", jetzt,
                transaktion);
        }
        SetzeMeta(BetreuerUebernommen, "1", transaktion);
    }

    private const string BetreuerUebernommen = "personen_betreuer_uebernommen";
    private const string DemoPersonen = "demo_personen";

    // Einmal je Datenbank (auch ohne Demo): die bisherigen Betreuer ins Verzeichnis.
    private void PersonenPruefen()
    {
        if (Meta(BetreuerUebernommen) is not null) return;
        using var transaktion = db.BeginTransaction();
        if (WertIn(transaktion, "SELECT wert FROM meta WHERE schluessel = $s", ("$s",
            BetreuerUebernommen)) is not null) return;
        BetreuerUebernehmen(transaktion);
        transaktion.Commit();
    }

    private void DemoAerzteEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM aerzte WHERE gruppe = 'arzt'", transaktion);
        foreach (var a in Demodaten.Aerzte()) PersonEinfuegen(a, "EF", Uhr(), transaktion);
        SetzeMeta(DemoAerzte, "1", transaktion);
    }

    // Demo: die übrigen Verzeichnisse (daten/demo/personen.json) mit Zuordnungen, dazu die Betreuer aus den Stammdaten.
    private void DemoPersonenEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM aerzte WHERE gruppe <> 'arzt'", transaktion);   // Zuordnungen folgen (CASCADE)
        var bewohner = LesenIn(transaktion,
            AktiveBewohnerSql).GroupBy(b => b.Nachname).ToDictionary(g => g.Key,
                g => g.First().Id!.Value);
        var jetzt = Uhr();
        foreach (var (p, zuordnungen) in Demodaten.Personen())
        {
            long id = PersonEinfuegen(p, "EF", jetzt, transaktion);
            foreach (var (nachname, rolle) in zuordnungen)
                if (bewohner.TryGetValue(nachname, out long b)) ZuordnungEinfuegen(id, b, rolle, "EF", jetzt,
                    transaktion);
        }
        BetreuerUebernehmen(transaktion);
        SetzeMeta(DemoPersonen, "1", transaktion);
    }
}
