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
        var fehler = Assert.Throws<Eingabefehler>(() => ArztRegeln.AusEingabe(new ArztEingabe { Email = "x" })).Fehler;
        Assert.Contains(fehler, f => f.StartsWith("Name fehlt"));
        Assert.Contains(fehler, f => f.StartsWith("E-Mail-Adresse"));
        var neu = s.ArztSpeichern(ArztRegeln.AusEingabe(new ArztEingabe { Name = "Dr. Test (ausgedacht)", Fachrichtung = "Orthopädie" }), "EF", Jetzt);
        Assert.Equal("Dr. Test (ausgedacht) · Orthopädie", neu.Anzeige);
        Assert.Throws<Eingabefehler>(() => s.ArztSpeichern(ArztRegeln.AusEingabe(new ArztEingabe { Name = "dr. test (ausgedacht)" }), "EF", Jetzt));   // doppelt
        var geaendert = s.ArztSpeichern(ArztRegeln.AusEingabe(new ArztEingabe { Name = "Dr. Test (ausgedacht)", Praxis = "Praxis (ausgedacht)", Telefon = "0000 1" }, neu.Id), "EF", Jetzt);
        Assert.Equal("Dr. Test (ausgedacht), Praxis (ausgedacht), Tel. 0000 1", ArztRegeln.Kontakt(geaendert));
        s.ArztAktiv(neu.Id, false, "EF", Jetzt);
        Assert.DoesNotContain(s.Aerzte(nurAktive: true), a => a.Id == neu.Id);
        Assert.Contains(s.Aerzte(), a => a.Id == neu.Id);   // nie gelöscht
        Assert.Throws<Eingabefehler>(() => s.ArztAktiv(neu.Id, true, "", Jetzt));
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
