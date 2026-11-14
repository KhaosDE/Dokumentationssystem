using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 47: Hilfeplangespräche – Stand aus Ereignissen, Vorschlag aus der Bewilligung, Marken, Teilnehmer aus der Akte,
// Termin im Kalender (Wochen- und Tagesplan), Überschneidung, Vorbereitung als Aufgaben, „geführt“ mit Bericht, Prüfpunkt QMB.
[Collection("Datenordner")]
public sealed class HilfeplanTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public HilfeplanTests()
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

    private static HilfeplanEreignis E(string art, DateOnly datum, int minuten) => new(minuten, 1, art, datum, null, "", null, "EF", Jetzt.AddMinutes(minuten));

    [Fact]
    public void Stand_Vorschlag_Marke_und_Texte()
    {
        var k = new Kostentraeger
        {
            Name = "Bezirk (Test)", Sachbearbeiter = "Frau Sachlich (Test)", Email = "kt@example.org",
            Bewilligungen = [new Bewilligung("Wohnen", null, Heute.AddYears(-1), Heute.AddDays(40))]
        };
        var ohne = HilfeplanRegeln.Stand(B, [], k, new Dictionary<long, KalenderTermin>());
        Assert.Equal((null, Heute.AddDays(40), true, HilfeplanStandArt.Offen), (ohne.LetztesAm, ohne.FaelligBis, ohne.Vorschlag, ohne.Stand));
        Assert.Equal("Hilfeplangespräch fällig in 5 Wochen", HilfeplanRegeln.Marke(ohne, Heute));
        Assert.Contains("(Vorschlag aus der Bewilligung)", HilfeplanRegeln.KurzText(ohne));

        var gefuehrt = HilfeplanRegeln.Stand(B, [E("gefuehrt", Heute.AddDays(-5), 1)], null, new Dictionary<long, KalenderTermin>());
        Assert.Equal((Heute.AddDays(-5), (DateOnly?)null, HilfeplanStandArt.Gefuehrt), (gefuehrt.LetztesAm, gefuehrt.FaelligBis, gefuehrt.Stand));
        Assert.Null(HilfeplanRegeln.Marke(gefuehrt, Heute));
        var faellig = HilfeplanRegeln.Stand(B, [E("gefuehrt", Heute.AddYears(-1), 1), E("faellig", Heute.AddDays(-3), 2)], k, new Dictionary<long, KalenderTermin>());
        Assert.Equal((Heute.AddDays(-3), false), (faellig.FaelligBis, faellig.Vorschlag));
        Assert.Equal("Hilfeplangespräch überfällig", HilfeplanRegeln.Marke(faellig, Heute));
        Assert.Null(HilfeplanRegeln.Marke(faellig with { Stand = HilfeplanStandArt.TerminVergeben }, Heute));
        Assert.Null(HilfeplanRegeln.Marke(faellig with { FaelligBis = Heute.AddDays(200) }, Heute));

        var teilnehmer = HilfeplanRegeln.TeilnehmerVorschlag(B, "Emma Fink", new Betreuer { Name = "Herr Treu (Test)" },
            new Zustaendigkeit { Fosa = JaNein.Ja, Ambulanz = "Ambulanz (Test)" }, k, ["Erika Ahornblatt (Test)"]);
        Assert.Equal(["A Ahorn (Test) (Bewohner)", "Emma Fink (Bezugspflege)", "Herr Treu (Test) (Betreuer)", "FOSA (Ambulanz (Test))",
            "Frau Sachlich (Test) (Bezirk (Test))", "Erika Ahornblatt (Test) (Angehörige)"], teilnehmer);

        var f = new KalenderFassung { Beginn = Heute.ToDateTime(new TimeOnly(10, 0)), Ende = Heute.ToDateTime(new TimeOnly(11, 0)), Ort = "Besprechungsraum" };
        var text = HilfeplanRegeln.Einladung(B, f);
        Assert.Contains("09.11.2026, 10:00–11:00 Uhr", text);
        Assert.DoesNotContain("Diagnose", text);
        var mail = HilfeplanRegeln.Mailto(["kt@example.org", "keine adresse"], HilfeplanRegeln.Betreff(B, f), text);
        Assert.StartsWith("mailto:kt%40example.org?subject=Einladung", mail);

        var woche = new[] { new Termin { Tag = Heute, Von = new TimeOnly(10, 30), Bis = new TimeOnly(11, 30), Titel = "Ergotherapie" },
            new Termin { Tag = Heute, Von = new TimeOnly(14, 0), Bis = new TimeOnly(15, 0), Titel = "Malen" } };
        Assert.Equal(["10:30–11:30 Ergotherapie"], HilfeplanRegeln.Ueberschneidungen(woche, f.Beginn, f.Ende));
    }

    [Fact]
    public void Termin_Vorbereitung_Gefuehrt_und_Pruefpunkt()
    {
        var b = s.AlleBewohner().First();
        long id = b.Id!.Value;
        Assert.NotEmpty(s.HilfeplanTeilnehmer(id, Heute));
        var beginn = Heute.AddDays(10).ToDateTime(new TimeOnly(10, 0));
        Assert.Throws<Eingabefehler>(() => s.HilfeplanTerminVergeben(id, beginn, 60, "Raum 1", ["x"], "QS", Jetzt));   // QMB
        var termin = s.HilfeplanTerminVergeben(id, beginn, 60, "Besprechungsraum", ["A", "B"], "EF", Jetzt);
        Assert.Equal((KalenderArt.Bewohner, HilfeplanRegeln.Titel, beginn.AddHours(1)), (termin.Aktuell.Art, termin.Aktuell.Titel, termin.Aktuell.Ende));
        Assert.Contains(s.Woche(id, WochenplanRegeln.Montag(DateOnly.FromDateTime(beginn))), t => t.Titel == HilfeplanRegeln.Titel);
        var stand = s.Hilfeplan(id, Heute);
        Assert.Equal(HilfeplanStandArt.TerminVergeben, stand.Stand);
        Assert.Equal(termin.Id, stand.Termin!.Id);

        var aufgaben = s.HilfeplanVorbereiten(id, HilfeplanRegeln.Vorbereitung.Take(2), "EF", Jetzt);
        Assert.Equal(2, aufgaben.Count);
        Assert.All(aufgaben, a => Assert.Equal((id, beginn.Date.AddDays(-1).AddHours(18)), (a.BewohnerId!.Value, a.FaelligBis!.Value)));

        Assert.Throws<Eingabefehler>(() => s.HilfeplanGefuehrt(id, Heute, " ", null, "EF", Jetzt));
        var bericht = s.HilfeplanGefuehrt(id, Heute, "Ziele besprochen, Bewilligung wird verlängert (Test).", Heute.AddMonths(12), "EF", Jetzt);
        Assert.Equal(HilfeplanRegeln.ArtBericht, bericht.Art);
        Assert.Contains(HilfeplanRegeln.ArtBericht, s.Berichtsarten());
        stand = s.Hilfeplan(id, Heute);
        Assert.Equal((Heute, Heute.AddMonths(12), HilfeplanStandArt.Offen), (stand.LetztesAm, stand.FaelligBis, stand.Stand));

        // Demo: einige überfällig – der Prüfpunkt des QMB meldet sie.
        var plaene = s.Hilfeplaene(s.AlleBewohner(), Heute);
        Assert.Contains(plaene, p => HilfeplanRegeln.Marke(p, Heute) == "Hilfeplangespräch überfällig");
        Assert.Contains(plaene, p => p.Vorschlag);
        var pruef = s.Pruefliste(s.AlleBewohner(), Heute);
        Assert.Contains(pruef, z => z.Ergebnisse.Any(e => e.Punkt == Pruefpunkt.Hilfeplan && !e.Ok && e.Text.StartsWith("überfällig seit ", StringComparison.Ordinal)));
    }
}
