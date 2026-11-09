using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26b: Ärzteverzeichnis – Auswahl statt Freitext, nie löschen, nur inaktiv; Recht
// „Ärzteverzeichnis verwalten“; Demo mit ausgedachten Ärztinnen und Ärzten (nur @example.org).
[Collection("Datenordner")]
public sealed class AerzteTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public AerzteTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    [Fact]
    public void Demo_Verzeichnis()
    {
        var alle = s.Aerzte();
        Assert.True(alle.Count >= 6);
        Assert.All(alle, a => Assert.EndsWith("(ausgedacht)", a.Name));
        Assert.All(alle.Where(a => a.Email.Length > 0), a => Assert.EndsWith("@example.org", a.Email));
        Assert.Contains(alle, a => !a.Aktiv);
        Assert.DoesNotContain(s.Aerzte(nurAktive: true), a => !a.Aktiv);
        // Die Ärzte der übrigen Demo-Daten stehen im Verzeichnis.
        foreach (var name in s.Visiten().Select(v => v.Arzt).Concat(s.AlleBewohner().SelectMany(b => s.Verordnungen(b.Id!.Value)).Select(v => v.Anordnung.Arzt)).Distinct())
            Assert.NotNull(s.ArztNach(name));
        string datei = File.ReadAllText(Path.Combine(Repo.Wurzel, "daten", "demo", "aerzte.json"));
        Assert.DoesNotMatch(@"@(?!example\.org)", datei);
    }

    [Fact]
    public void Anlegen_aendern_inaktiv_und_Pruefung()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => PersonRegeln.AusEingabe(new PersonEingabe { Email = "x" })).Fehler;
        Assert.Contains(fehler, f => f.StartsWith("Name fehlt"));
        Assert.Contains(fehler, f => f.StartsWith("E-Mail-Adresse"));
        var neu = s.PersonSpeichern(PersonRegeln.AusEingabe(new PersonEingabe { Name = "Dr. Test (ausgedacht)", Fachrichtung = "Orthopädie" }), "EF", Jetzt);
        Assert.Equal("Dr. Test (ausgedacht) · Orthopädie", neu.Anzeige);
        Assert.Throws<Eingabefehler>(() => s.PersonSpeichern(PersonRegeln.AusEingabe(new PersonEingabe { Name = "dr. test (ausgedacht)" }), "EF", Jetzt));   // doppelt
        var geaendert = s.PersonSpeichern(PersonRegeln.AusEingabe(new PersonEingabe { Name = "Dr. Test (ausgedacht)", Praxis = "Praxis (ausgedacht)", Telefon = "0000 1" }, neu.Id), "EF", Jetzt);
        Assert.Equal("Dr. Test (ausgedacht), Praxis (ausgedacht), Tel. 0000 1", PersonRegeln.Kontakt(geaendert));
        s.PersonAktiv(neu.Id, false, "EF", Jetzt);
        Assert.DoesNotContain(s.Aerzte(nurAktive: true), a => a.Id == neu.Id);
        Assert.Contains(s.Aerzte(), a => a.Id == neu.Id);   // nie gelöscht
        Assert.Throws<Eingabefehler>(() => s.PersonAktiv(neu.Id, true, "", Jetzt));
    }

    [Fact]
    public void Recht_Aerzteverzeichnis_verwalten()
    {
        Assert.Null(s.Grund("EF", Funktionen.Aerzte));   // Bereichsleitung
        Assert.Null(s.Grund("PR", Funktionen.Aerzte));   // Psychologischer Fachdienst
        Assert.Null(s.Grund("UT", Funktionen.Aerzte));   // Geschäftsführung
        Assert.NotNull(s.Grund("CD", Funktionen.Aerzte));   // Mitarbeiter Wohnbereich: in der Tabelle änderbar
        Assert.NotNull(s.Grund("OP", Funktionen.Aerzte));   // Therapie
    }
}

// Bauplan Schritt 29a: Personenverzeichnis – Verzeichnisse je Gruppe, Zuordnung zu Bewohnern mit Rolle,
// Übernahme der Betreuer aus den Stammdaten, Rechte (Angehörige mit „Bewohner bearbeiten“).
[Collection("Datenordner")]
public sealed class PersonenverzeichnisTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private Speicher s;

    public PersonenverzeichnisTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;

    [Fact]
    public void Demo_alle_Verzeichnisse_und_Betreuer_uebernommen()
    {
        Assert.All(PersonRegeln.Gruppen, g => Assert.NotEmpty(s.Personen(g)));
        Assert.Equal(35, s.Personen(PersonGruppe.Angehoerige).Count);   // etwa die Hälfte der 69 Bewohner
        Assert.All(s.Personen().Where(p => p.Email.Length > 0), p => Assert.EndsWith("@example.org", p.Email));
        string datei = File.ReadAllText(Path.Combine(Repo.Wurzel, "daten", "demo", "personen.json"));
        Assert.DoesNotMatch(@"@(?!example\.org)", datei);
        // Betreuer aus den Stammdaten: eine Person für mehrere Bewohner (Heike Hecht: Ahorn und Eiche).
        var hecht = Assert.Single(s.Personen(PersonGruppe.Betreuer), p => p.Name == "Heike Hecht");
        Assert.Equal("Betreuungsverein", hecht.Art);
        var z = s.Zuordnungen(personId: hecht.Id);
        Assert.Equal([B("Ahorn"), B("Eiche")], z.Select(x => x.BewohnerId).Order());
        Assert.All(z, x => Assert.Equal("Betreuer", x.Rolle));
        // „keine gesetzliche Betreuung“ wird keine Person (Ginster).
        Assert.DoesNotContain(s.Zuordnungen(bewohnerId: B("Ginster")), x => s.PersonNach(x.PersonId)!.Gruppe == PersonGruppe.Betreuer);
        // Spalten und Zeile: Angehörige mit „zugeordnet zu“.
        var namen = s.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        var hanna = s.Personen(PersonGruppe.Angehoerige).Single(p => p.Name == "Hanna Ahorn");
        Assert.Equal(["Hanna Ahorn", hanna.Telefon, hanna.Mobil, "Anton Ahorn (Mutter)", "aktiv"], PersonRegeln.Zeile(hanna, s.Zuordnungen(), namen));
    }

    [Fact]
    public void Bestehende_Datenbank_uebernimmt_Betreuer_einmal()
    {
        void Sql(string text)
        {
            using var befehl = s.Verbindung.CreateCommand();
            befehl.CommandText = text;
            befehl.ExecuteNonQuery();
        }
        // Wie vor 0.33.4: keine Betreuer im Verzeichnis, Merker fehlt.
        Sql("DELETE FROM aerzte WHERE gruppe = 'betreuer'; DELETE FROM meta WHERE schluessel = 'personen_betreuer_uebernommen'");
        string pfad = s.Pfad;
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Equal(5, s.Personen(PersonGruppe.Betreuer).Count);   // betreuer.json: 7 Bewohner, 5 verschiedene Betreuer
        Assert.Equal(7, s.Zuordnungen().Count(z => z.Rolle == "Betreuer"));
        // Noch einmal: nichts doppelt.
        Sql("DELETE FROM meta WHERE schluessel = 'personen_betreuer_uebernommen'");
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Equal(5, s.Personen(PersonGruppe.Betreuer).Count);
        Assert.Equal(7, s.Zuordnungen().Count(z => z.Rolle == "Betreuer"));
    }

    [Fact]
    public void Anlegen_zuordnen_Rechte_und_Namen_je_Verzeichnis()
    {
        // Gleicher Name in zwei Verzeichnissen ist erlaubt, im selben nicht.
        var p = s.PersonSpeichern(PersonRegeln.AusEingabe(new PersonEingabe { Gruppe = PersonGruppe.Sonstige, Name = "Dr. Ulme (ausgedacht)", Praxis = "Gutachten" }), "EF", Jetzt);
        Assert.Equal(PersonGruppe.Sonstige, p.Gruppe);
        Assert.Contains("steht schon im Verzeichnis Sonstige Stellen", Assert.Throws<Eingabefehler>(() =>
            s.PersonSpeichern(PersonRegeln.AusEingabe(new PersonEingabe { Gruppe = PersonGruppe.Sonstige, Name = "dr. ulme (ausgedacht)" }), "EF", Jetzt)).Message);
        Assert.Throws<Eingabefehler>(() => s.PersonSpeichern(p with { Gruppe = PersonGruppe.Arzt, Name = "anders" }, "EF", Jetzt));   // Verzeichnis bleibt
        // Felder je Verzeichnis: Angehörige haben keine Stelle und keine Funktion.
        var e = PersonRegeln.AusEingabe(new PersonEingabe { Gruppe = PersonGruppe.Angehoerige, Name = "Test Tanne", Praxis = "x", Fachrichtung = "y", Art = "z" });
        Assert.Equal(("", "", ""), (e.Praxis, e.Fachrichtung, e.Art));
        // Rechte: CD (Mitarbeiter Wohnbereich) darf Angehörige, aber keine Ärzte anlegen.
        Assert.Null(s.PersonGrund("CD", PersonGruppe.Angehoerige));
        Assert.Equal(RechteRegeln.FehltText, s.PersonGrund("CD", PersonGruppe.Arzt));
        var tanne = s.PersonSpeichern(e, "CD", Jetzt);
        Assert.Equal([RechteRegeln.FehltText], Assert.Throws<Eingabefehler>(() =>
            s.PersonSpeichern(PersonRegeln.AusEingabe(new PersonEingabe { Name = "Dr. Neu (ausgedacht)" }), "CD", Jetzt)).Fehler);
        // Zuordnen mit Rolle; gleiche Rolle nur einmal; entfernen.
        var z = s.ZuordnungSpeichern(tanne.Id, B("Birke"), "Bruder", "CD", Jetzt);
        s.ZuordnungSpeichern(tanne.Id, B("Birke"), "Bruder", "CD", Jetzt);
        Assert.Single(s.Zuordnungen(personId: tanne.Id));
        s.ZuordnungSpeichern(tanne.Id, B("Clematis"), "Freund", "CD", Jetzt);
        Assert.Equal(2, s.Zuordnungen(personId: tanne.Id).Count);
        s.ZuordnungEntfernen(z.Id, "CD");
        Assert.Equal(["Freund"], s.Zuordnungen(personId: tanne.Id).Select(x => x.Rolle));
        Assert.Throws<Eingabefehler>(() => s.ZuordnungSpeichern(tanne.Id, 999_999, "x", "CD", Jetzt));
        // Inaktiv: nie gelöscht; ohne Recht nicht.
        var arzt = s.Aerzte()[0];
        Assert.Equal([RechteRegeln.FehltText], Assert.Throws<Eingabefehler>(() => s.PersonAktiv(arzt.Id, false, "CD", Jetzt)).Fehler);
        Assert.Equal("Personenverzeichnis verwalten", Funktionen.Alle.Single(f => f.Schluessel == Funktionen.Aerzte).Name);
        // Vorschläge für die Rolle.
        Assert.Equal("Arzt/Ärztin Forensik", PersonRegeln.RolleVorschlag(s.Personen(PersonGruppe.Forensik).Single(x => x.Name.StartsWith("Dr. Stint"))));
        Assert.True(PersonRegeln.Passt(tanne, "tan"));
        Assert.False(PersonRegeln.Passt(tanne, "Ulme"));
    }
}
