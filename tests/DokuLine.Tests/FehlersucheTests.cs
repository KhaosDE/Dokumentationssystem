using DokuLine.Kern;

namespace DokuLine.Tests;

// Abschließende Fehlersuche nach Bauplan 52: Regeln, die vorher falsch waren (je ein Fall).
[Collection("Datenordner")]
public sealed class FehlersucheTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 14, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public FehlersucheTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static readonly Bewohner B = new() { Id = 1, Vorname = "A", Nachname = "Ahorn (Test)", Zimmer = "101", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };

    [Fact]
    public void Alle_n_Wochen_verschiebt_sich_bei_einer_Aenderung_nicht()
    {
        var montag = new DateOnly(2026, 11, 2);
        var erste = new MassnahmeFassung { Titel = "Wäsche (Test)", GiltAb = montag, Rhythmus = MassnahmeRhythmus.AlleNWochen, AlleWochen = 2, Wochentage = [DayOfWeek.Wednesday] };
        var zweite = erste with { GiltAb = new DateOnly(2026, 11, 13), Minuten = 30 };   // nur der Zeitwert geändert
        var m = new Massnahme(1, 1, [erste, zweite]);
        Assert.True(MassnahmenRegeln.AmTag(m, zweite, new DateOnly(2026, 11, 18)));
        Assert.False(MassnahmenRegeln.AmTag(m, zweite, new DateOnly(2026, 11, 25)));
        var andererTag = zweite with { Wochentage = [DayOfWeek.Thursday] };   // neuer Rhythmus: zählt ab der neuen Fassung
        Assert.True(MassnahmenRegeln.AmTag(new Massnahme(1, 1, [erste, andererTag]), andererTag, new DateOnly(2026, 11, 26)));   // Woche des 13.11. = 0
    }

    [Fact]
    public void Hilfeplan_vergangener_Termin_zaehlt_als_offen_und_Wochen_runden_auf()
    {
        var termin = new KalenderTermin { Id = 5, Fassungen = [new KalenderFassung { Titel = HilfeplanRegeln.Titel, Beginn = Jetzt.AddDays(-20), Ende = Jetzt.AddDays(-20).AddHours(1) }] };
        var vorbei = new HilfeplanStand(B, null, Heute.AddDays(-10), false, HilfeplanStandArt.TerminVergeben, termin);
        Assert.False(HilfeplanRegeln.TerminSteht(vorbei, Heute));
        Assert.Equal("Hilfeplangespräch überfällig", HilfeplanRegeln.Marke(vorbei, Heute));
        var bald = new HilfeplanStand(B, null, Heute.AddDays(3), false, HilfeplanStandArt.Offen, null);
        Assert.Equal("Hilfeplangespräch fällig in 1 Woche", HilfeplanRegeln.Marke(bald, Heute));
        var einladung = HilfeplanRegeln.FuerEinladung(termin with { Fassungen = [termin.Aktuell with { Hinweis = "Teilnehmer: FOSA (Test)" }] });
        Assert.Equal("", einladung.Aktuell.Hinweis);
    }

    [Fact]
    public void Schluesselwoerter_Wortgruppe_und_Datum_ohne_Jahr()
    {
        Assert.True(SchluesselwortRegeln.Trifft("ist heute nicht zurückgekommen", "nicht zurück", Treffer.Wortgruppe));
        Assert.True(SchluesselwortRegeln.Trifft("Termin beim Hausarzt", "Termin bei", Treffer.Wortgruppe));
        Assert.False(SchluesselwortRegeln.Trifft("Blutabnahmen", "Blutabnahme", Treffer.Wort));
        var gestern = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var (datum, _) = SchluesselwortRegeln.DatumImText($"Arzttermin am {gestern.Day}.{gestern.Month}.");
        Assert.Equal(gestern.AddYears(1), datum);
    }

    [Fact]
    public void Entlassene_bekommen_keine_Berichte_und_keine_erzeugten_Aufgaben()
    {
        var b = s.AlleBewohner().First(x => s.Vorlagen().Any(v => v.BewohnerId == x.Id) || s.Messauftraege().Any(m => m.BewohnerId == x.Id));
        long id = b.Id!.Value;
        s.Entlassen(id, Heute, EntlassGrund.Auszug, "", "EF", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.BerichtSpeichern(new Bericht
        {
            BewohnerId = id, Zeitpunkt = Jetzt, Schicht = Schicht.Frueh, Verfasser = "EF", Text = "nach der Entlassung (Test)", Erfasst = Jetzt,
        }));
        s.StandardaufgabenErzeugen(Jetzt.AddDays(1));
        Assert.DoesNotContain(s.Aufgaben(Jetzt.AddDays(1), auchErledigte: true), a => a.BewohnerId == id && a.Erstellt > Jetzt);
    }
}
