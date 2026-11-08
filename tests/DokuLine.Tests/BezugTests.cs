using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Bezugspflege und Einzeltrainer (Bauplan Schritt 20).
[Collection("Datenordner")]
public sealed class BezugTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 8, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public BezugTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoBeimErstenStart();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long Id(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;

    [Fact]
    public void Demo_Zuordnungen_und_Verlauf()
    {
        var alle = s.Bezuege();
        Assert.Equal("AB", alle[Id("Ahorn")].Bezugspflege!.Kuerzel);
        Assert.Null(alle[Id("Ahorn")].Einzeltrainer);
        Assert.Equal("DBT · PR", BezugRegeln.TrainerText(alle[Id("Birke")].Einzeltrainer!));
        Assert.Equal(5, s.Bezugsbewohner("AB"));   // ab 5 nur ein Hinweis
        Assert.Contains("5 Bewohner", BezugRegeln.Hinweis("AB", s.Bezugsbewohner("AB")));
        Assert.Equal("", BezugRegeln.Hinweis("CD", s.Bezugsbewohner("CD")));
        Assert.Equal(["CD", "AB"], s.Bezugspflegen(Id("Ahorn")).Select(z => z.Kuerzel));   // Wechsel bleibt nachvollziehbar
        Assert.Equal(new DateOnly(2026, 8, 10), s.Bezugspflegen(Id("Ahorn"))[^1].Ab);      // vor 90 Tagen
        var namen = s.AlleMitarbeiter().ToDictionary(m => m.Kuerzel, m => m.Name);
        var verlauf = BezugRegeln.Verlauf(s.Bezugspflegen(Id("Ahorn")), s.Einzeltrainer(Id("Ahorn")), namen);
        Assert.StartsWith("ab 10.08.2026 · Bezugspflege: Anna Bachstelze (AB)", verlauf[0]);
        Assert.Equal(["DBT", "Suchtgruppe"], s.EinzeltrainerArten());
    }

    [Fact]
    public void Wechsel_als_neuer_Eintrag()
    {
        long kiefer = Id("Kiefer");
        s.BezugspflegeSetzen(kiefer, "cd", new DateOnly(2026, 11, 8), "EF");
        Assert.Equal("CD", s.Bezug(kiefer).Bezugspflege!.Kuerzel);
        s.BezugspflegeSetzen(kiefer, "CD", new DateOnly(2026, 11, 8), "EF");   // unverändert: kein Eintrag
        Assert.Equal(2, s.Bezugspflegen(kiefer).Count);
        s.EinzeltrainerSetzen(kiefer, "PR", "Achtsamkeit", new DateOnly(2026, 11, 8), "EF");   // neue Art kommt in die Liste
        Assert.Equal("Achtsamkeit · PR", BezugRegeln.TrainerText(s.Bezug(kiefer).Einzeltrainer!));
        Assert.Contains("Achtsamkeit", s.EinzeltrainerArten());
        s.EinzeltrainerSetzen(kiefer, "", "", new DateOnly(2026, 11, 9), "EF");
        Assert.Null(s.Bezug(kiefer).Einzeltrainer);
        Assert.Equal(2, s.Einzeltrainer(kiefer).Count);

        var fehler = Assert.Throws<Eingabefehler>(() => s.EinzeltrainerSetzen(kiefer, "HI", "", new DateOnly(2026, 11, 8), "")).Fehler;
        Assert.Contains("Hugo Ibis ist ausgeschieden.", fehler);
        Assert.Contains("Art des Einzeltrainings fehlt.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("Kürzel fehlt"));
        Assert.Throws<Eingabefehler>(() => s.BezugspflegeSetzen(kiefer, "ZZ", new DateOnly(2026, 11, 8), "EF"));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE bezugspflege SET kuerzel = 'AB'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Farbe_gehoert_zum_Mitarbeiter()
    {
        Assert.Equal("#2F6FB7", s.Farbe("AB"));                                   // aus der Demo
        Assert.Equal(BezugRegeln.Vorgabefarbe("EF"), s.Farbe("EF"));              // ohne eigene: Vorgabe, immer gleich
        Assert.Equal(BezugRegeln.Vorgabefarbe("EF"), BezugRegeln.Vorgabefarbe("ef"));
        s.FarbeSetzen("EF", "#123abc");
        Assert.Equal("#123ABC", s.Farbe("EF"));
        Assert.Equal("#123ABC", s.MitarbeiterLesen("EF")!.Farbe);
        Assert.Throws<Eingabefehler>(() => s.FarbeSetzen("EF", "rot"));
        s.FarbeSetzen("EF", null);
        Assert.Equal(BezugRegeln.Vorgabefarbe("EF"), s.Farbe("EF"));
    }
}
