using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 41: Zeitauswertung je Bewohner – Quellen (Maßnahmen, Therapie, Berichte mit Dauer), geplant gegenüber
// tatsächlich, Summen, CSV, Recht; nie je Mitarbeiter.
[Collection("Datenordner")]
public sealed class ZeitauswertungTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);   // Montag, Frühdienst
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public ZeitauswertungTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static Bericht B(int dauer, params int[] bereiche) => new()
    {
        BewohnerId = 1, Zeitpunkt = Jetzt, Schicht = Schicht.Frueh, Verfasser = "AB", Text = "Gespräch (Test)", Bereiche = bereiche, Dauer = dauer,
    };

    [Fact]
    public void Dauer_Bericht_Geplant_Zeilen_und_Csv()
    {
        var fehler = new List<string>();
        Assert.Null(ZeitauswertungRegeln.DauerLesen(" ", fehler));
        Assert.Equal(30, ZeitauswertungRegeln.DauerLesen("30", fehler));
        Assert.Empty(fehler);
        foreach (string falsch in new[] { "0", "abc", "700", "-5", "1,5" }) ZeitauswertungRegeln.DauerLesen(falsch, fehler);
        Assert.Equal(5, fehler.Count);

        Assert.Equal(new ZeitPosten(1, Heute, ZeitArt.Sonstiges, 2, 20), ZeitauswertungRegeln.AusBericht(B(20, 4, 2)));   // erster Lebensbereich
        Assert.Null(ZeitauswertungRegeln.AusBericht(B(20, 2) with { Dauer = null }));
        Assert.Null(ZeitauswertungRegeln.AusBericht(B(20, 2) with { Art = MassnahmenRegeln.ArtBericht }));   // zählt als Maßnahme
        Assert.Null(ZeitauswertungRegeln.AusBericht(B(20))!.Lebensbereich);   // „allgemein“

        var taeglich = new MassnahmeFassung { Titel = "A", Lebensbereich = 2, Art = "Anleitung", Rhythmus = MassnahmeRhythmus.Taeglich, Minuten = 30, GiltAb = Heute.AddDays(-30) };
        var wochentage = taeglich with { Rhythmus = MassnahmeRhythmus.Wochentage, Wochentage = [DayOfWeek.Monday, DayOfWeek.Thursday], Minuten = 10 };
        var bedarf = taeglich with { Rhythmus = MassnahmeRhythmus.BeiBedarf };
        Assert.Equal(7 * 30 + 2 * 10, ZeitauswertungRegeln.Geplant([new Massnahme(1, 1, [taeglich]), new Massnahme(2, 1, [wochentage]), new Massnahme(3, 1, [bedarf])],
            Heute, Heute.AddDays(6)));

        var bewohner = new Bewohner { Id = 1, Vorname = "Anton", Nachname = "Ahorn (Test)", Zimmer = "101", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };
        var zeilen = ZeitauswertungRegeln.Zeilen([bewohner],
            [new(1, Heute, ZeitArt.Massnahmen, 2, 30), new(1, Heute, ZeitArt.Therapie, null, 90), new(1, Heute, ZeitArt.Sonstiges, 1, 15), new(2, Heute, ZeitArt.Massnahmen, 2, 99)],
            new Dictionary<long, int> { [1] = 60 });
        var z = Assert.Single(zeilen);
        Assert.Equal([15, 30, 0, 0, 0], z.JeLebensbereich);
        Assert.Equal((90, 30, 90, 15, 135, 60), (z.OhneLebensbereich, z.Massnahmen, z.Therapie, z.Sonstiges, z.Summe, z.MassnahmenGeplant));
        string csv = ZeitauswertungRegeln.Csv(zeilen, Heute, Heute);
        var reihen = csv.TrimEnd().Split(Environment.NewLine);
        Assert.Equal(4, reihen.Length);
        Assert.StartsWith("Zeitauswertung (Minuten);09.11.2026", reihen[0]);
        Assert.Equal(string.Join(';', ZeitauswertungRegeln.Spalten), reihen[2]);
        Assert.Equal("101;Anton Ahorn (Test);15;30;0;0;0;90;30;90;15;135;60", reihen[3]);
        Assert.DoesNotContain(ZeitauswertungRegeln.Spalten, sp => sp.Contains("Mitarbeiter") || sp.Contains("Kürzel"));
    }

    [Fact]
    public void Speicher_sammelt_Massnahmen_Therapie_und_Berichte_mit_Recht()
    {
        var r1 = s.AlleBewohner().Where(b => b.Wohnbereich == "R1").ToList();
        Assert.Throws<Eingabefehler>(() => s.Zeitauswertung(r1, Heute.AddDays(-30), Heute, "KL"));   // Assistenzkraft: kein Recht
        Assert.Throws<Eingabefehler>(() => s.Zeitauswertung(r1, Heute, Heute.AddDays(-1), "EF"));
        var zeilen = s.Zeitauswertung(r1, Heute.AddDays(-30), Heute, "EF");   // Bereichsleitung
        Assert.Equal(r1.Count, zeilen.Count);
        Assert.Contains(zeilen, z => z.Massnahmen > 0);
        Assert.Contains(zeilen, z => z.Therapie > 0);
        Assert.Contains(zeilen, z => z.Sonstiges > 0);   // Demo: einige Berichte mit Dauer
        Assert.All(zeilen, z => Assert.True(z.MassnahmenGeplant >= z.Massnahmen - 60));   // grob: tatsächlich ≈ geplant

        // Ein Bericht mit Dauer zählt; die Dauer bleibt, wie sie ist.
        long id = r1[0].Id!.Value;
        int vorher = zeilen.Single(z => z.Bewohner.Id == id).Sonstiges;
        var bericht = s.BerichtSpeichern(B(25, 3) with { BewohnerId = id, Zeitpunkt = Jetzt.AddMinutes(-5), Erfasst = Jetzt });
        Assert.Equal(25, s.Berichte(id).Single(x => x.Id == bericht.Id).Dauer);
        Assert.Equal(vorher + 25, s.Zeitauswertung(r1, Heute.AddDays(-30), Heute, "EF").Single(z => z.Bewohner.Id == id).Sonstiges);
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE bericht_dauer SET minuten = 1";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());
        Assert.True(RechteRegeln.Vorbelegt("Bereichsleitung", Funktionen.Zeitauswertung));
        Assert.False(RechteRegeln.Vorbelegt("Mitarbeiter Wohnbereich", Funktionen.Zeitauswertung));
        Assert.False(RechteRegeln.Vorbelegt("Therapie", Funktionen.Zeitauswertung));
    }
}
