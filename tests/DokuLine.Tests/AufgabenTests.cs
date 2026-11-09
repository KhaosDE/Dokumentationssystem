using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Wohnbereich-ToDo: Für wen, fällig, überfällig, weitergeben, erledigt, nichts wird überschrieben, Demo.
[Collection("Datenordner")]
public sealed class AufgabenTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);   // Samstag, Spätdienst
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private readonly Bewohner b;

    public AufgabenTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        b = s.BewohnerSpeichern(BewohnerRegeln.AusEingabe(new BewohnerEingabe
        {
            Vorname = "Test", Nachname = "Tanne", Zimmer = "201", Geburtsdatum = "01.01.1970", Aufnahmedatum = "01.01.2020",
        }));
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Aufgabe Neu(Action<AufgabeEingabe>? aendern = null, DateTime? jetzt = null)
    {
        var e = new AufgabeEingabe { Text = "Rezept in der Praxis abholen", BewohnerId = b.Id, Verfasser = "AB" };
        aendern?.Invoke(e);
        return s.AufgabeSpeichern(AufgabenRegeln.AusEingabe(e, jetzt ?? Jetzt));
    }

    [Fact]
    public void Pflichtangaben_und_Pruefungen()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => AufgabenRegeln.AusEingabe(new AufgabeEingabe(), Jetzt)).Fehler;
        Assert.Contains("Was ist zu tun? Text fehlt.", fehler);
        Assert.Contains("Verfasser (Kürzel) fehlt.", fehler);
        fehler = Assert.Throws<Eingabefehler>(() => AufgabenRegeln.AusEingabe(new AufgabeEingabe
        {
            Text = "x", Verfasser = "AB", Fuer = AufgabeFuer.Schicht, SchichtTag = "07.11.2026", SchichtArt = Schicht.Frueh,
            FaelligDatum = "06.11.2026",
        }, Jetzt)).Fehler;
        Assert.Contains("Diese Schicht ist schon vorbei.", fehler);
        Assert.Contains("„Fällig bis“ liegt in der Vergangenheit.", fehler);
        fehler = Assert.Throws<Eingabefehler>(() => AufgabenRegeln.AusEingabe(new AufgabeEingabe
        {
            Text = "x", Verfasser = "AB", FaelligDatum = "08.11.2026", FaelligUhrzeit = "25:00",
        }, Jetzt)).Fehler;
        Assert.Contains(fehler, f => f.StartsWith("Fällig bis, Uhrzeit"));
    }

    [Fact]
    public void Faellig_ohne_Uhrzeit_gilt_der_ganze_Tag()
    {
        var a = Neu(e => e.FaelligDatum = "09.11.2026");
        Assert.Equal(new DateTime(2026, 11, 9, 23, 59, 0), a.FaelligBis);
        Assert.Equal("fällig bis Mo 09.11.", AufgabenRegeln.FaelligText(a, Jetzt));
        Assert.Equal("überfällig seit Mo 09.11.", AufgabenRegeln.FaelligText(a, new DateTime(2026, 11, 10, 8, 0, 0)));
        Assert.True(AufgabenRegeln.Ueberfaellig(a, new DateTime(2026, 11, 10, 8, 0, 0)));
    }

    [Fact]
    public void Naechste_Schicht_bestimmte_Schicht_und_wer_Zeit_hat()
    {
        var naechste = Neu();
        Assert.Equal(new SchichtTermin(new DateOnly(2026, 11, 7), Schicht.Nacht), AufgabenRegeln.Zielschicht(naechste));
        Assert.Equal("für Nachtdienst Sa 07.11.", AufgabenRegeln.FuerText(naechste));
        Assert.False(AufgabenRegeln.JetztDran(naechste, Jetzt));
        Assert.True(AufgabenRegeln.JetztDran(naechste, new DateTime(2026, 11, 7, 22, 30, 0)));

        var bestimmt = Neu(e => { e.Fuer = AufgabeFuer.Schicht; e.SchichtTag = "9.11.2026"; e.SchichtArt = Schicht.Frueh; });
        Assert.Equal("für Frühdienst Mo 09.11.", AufgabenRegeln.FuerText(bestimmt));

        var frei = Neu(e => e.Fuer = AufgabeFuer.WerZeitHat);
        Assert.Null(AufgabenRegeln.Zielschicht(frei));
        Assert.Equal("wer Zeit hat", AufgabenRegeln.FuerText(frei));
        Assert.False(AufgabenRegeln.JetztDran(frei, Jetzt));

        // Nachtdienst um 2 Uhr gehört zum Vortag; die nächste Schicht ist der Frühdienst.
        var nachts = Neu(jetzt: new DateTime(2026, 11, 8, 2, 0, 0));
        Assert.Equal(new SchichtTermin(new DateOnly(2026, 11, 8), Schicht.Frueh), AufgabenRegeln.Zielschicht(nachts));
    }

    [Fact]
    public void Weitergeben_und_erledigen_sind_Ereignisse()
    {
        var a = Neu();
        Assert.Throws<Eingabefehler>(() => s.AufgabeWeitergeben(a.Id, "", "x", Jetzt));
        s.AufgabeWeitergeben(a.Id, "CD", "Praxis war schon zu.", new DateTime(2026, 11, 7, 23, 0, 0));
        var weiter = s.AufgabeLesen(a.Id)!;
        var w = Assert.Single(weiter.Weitergaben);
        Assert.Equal(("CD", "Praxis war schon zu."), (w.Kuerzel, w.Notiz));
        Assert.Equal(new SchichtTermin(new DateOnly(2026, 11, 8), Schicht.Frueh), AufgabenRegeln.Zielschicht(weiter));
        Assert.Equal(a.Text, weiter.Text);

        s.AufgabeErledigt(a.Id, "EF", new DateTime(2026, 11, 8, 9, 0, 0));
        var erledigt = s.AufgabeLesen(a.Id)!;
        Assert.False(erledigt.Offen);
        Assert.Equal("EF", erledigt.ErledigtVon);
        Assert.Throws<Eingabefehler>(() => s.AufgabeErledigt(a.Id, "EF", Jetzt));
        Assert.DoesNotContain(s.Aufgaben(Jetzt), x => x.Id == a.Id);
        Assert.Contains(s.Aufgaben(Jetzt, auchErledigte: true), x => x.Id == a.Id);
    }

    [Fact]
    public void Aufgaben_lassen_sich_nicht_ueberschreiben()
    {
        var a = Neu();
        s.AufgabeErledigt(a.Id, "EF", Jetzt);
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE aufgaben SET text = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
        befehl.CommandText = "UPDATE aufgabe_ereignisse SET kuerzel = 'XY'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
        Assert.Throws<InvalidOperationException>(() => s.AufgabeSpeichern(s.AufgabeLesen(a.Id)!));
    }

    [Fact]
    public void Reihenfolge_ueberfaellig_zuerst()
    {
        var spaeter = Neu(e => { e.Text = "später"; e.FaelligDatum = "20.11.2026"; });
        var frei = Neu(e => { e.Text = "ohne Frist"; e.Fuer = AufgabeFuer.WerZeitHat; });
        var bald = Neu(e => { e.Text = "bald"; e.FaelligDatum = "08.11.2026"; e.FaelligUhrzeit = "10:00"; });
        var liste = s.Aufgaben(new DateTime(2026, 11, 8, 12, 0, 0)).Select(a => a.Text).ToList();
        Assert.Equal(["bald", "später", "ohne Frist"], liste);   // „bald“ ist überfällig, „später“ jetzt dran
        Assert.Equal(3, s.OffeneAufgaben());
        _ = (spaeter, frei, bald);
    }

    [Fact]
    public void Demo_Aufgaben_relativ_zu_heute()
    {
        s.DemoZuruecksetzen();
        var mitVisiten = s.Aufgaben(Jetzt, auchErledigte: true);
        Assert.Equal(3, mitVisiten.Count(a => a.VisiteAnordnungId is not null || a.VisiteEintragId is not null && !a.Offen));   // Demo-Visiten (Schritt 26): erledigt
        var alle = mitVisiten.Where(a => a.VisiteAnordnungId is null && a.VisiteEintragId is null).ToList();
        Assert.Equal(12, alle.Count);   // mit zwei Aufgaben des Bereichs Therapie
        Assert.Single(alle, a => !a.Offen);
        // Jede Aufgabe gehört zu genau einem Bereich: die Bereiche zusammen ergeben alle.
        Assert.All(alle.Where(a => a.BewohnerId is null), a => Assert.Contains(a.Bereich, new[] { "R1", "R2", "R3", "Therapie" }));
        var ids = alle.Select(a => a.Id).ToHashSet();
        Assert.Equal(alle.Count, new[] { "R1", "R2", "R3", "Therapie" }.Sum(b => s.Aufgaben(Jetzt, true, b).Count(a => ids.Contains(a.Id))));
        Assert.Equal(2, s.Aufgaben(Jetzt, true, "R2").Count(a => a.BewohnerId is null));
        var ueberfaellig = Assert.Single(alle, a => AufgabenRegeln.Ueberfaellig(a, Jetzt));
        Assert.Single(ueberfaellig.Weitergaben);
        Assert.Equal(ueberfaellig.Id, s.Aufgaben(Jetzt)[0].Id);   // steht oben
        Assert.Contains(alle, a => a.BewohnerId is null);
        Assert.Contains(alle, a => a.Fuer == AufgabeFuer.Schicht && a.FuerSchicht!.Value.Beginn > Jetzt);
        Assert.DoesNotContain(alle, a => a.Text.Contains('{'));
        Assert.All(alle, a => Assert.True(a.Erstellt <= Jetzt));
        Assert.Equal(11, s.OffeneAufgaben());

        s.DemoZuruecksetzen();   // zweimal laden verdoppelt nichts
        Assert.Equal(15, s.Aufgaben(Jetzt, auchErledigte: true).Count);   // 12 + 3 aus den Demo-Visiten
    }

    [Fact]
    public void Nach_dem_Update_kommen_Demo_Aufgaben_dazu()
    {
        s.DemoZuruecksetzen();
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM aufgaben; DELETE FROM meta WHERE schluessel = 'demo_aufgaben'";
            befehl.ExecuteNonQuery();
        }
        int berichte = s.Berichte(s.AlleBewohner()[0].Id!.Value).Count;
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(12, s.Aufgaben(Jetzt, auchErledigte: true).Count);
        Assert.Equal(berichte, s.Berichte(s.AlleBewohner()[0].Id!.Value).Count);   // Berichte nicht doppelt
        Assert.False(s.DemoBeimErstenStart());
    }
}
