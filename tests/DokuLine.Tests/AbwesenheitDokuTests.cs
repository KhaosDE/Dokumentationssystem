using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;

namespace DokuLine.Tests;

// Bauplan 50: Abwesenheit in der Dokumentation – Zeiträume aus den Heimfahrten, Texte, laufende Abwesenheit aus dem Kern,
// Beginn und Ende als je ein Eintrag beim Status der Ausgangsliste.
[Collection("Datenordner")]
public sealed class AbwesenheitDokuTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public AbwesenheitDokuTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.ModulAnmelden(new AusgangslisteDaten());
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    [Fact]
    public void Zeitraeume_Texte_und_laufende_Heimfahrt()
    {
        var b = s.AlleBewohner().First();
        long id = b.Id!.Value;
        var heute = DateOnly.FromDateTime(Jetzt);
        s.KalenderAnlegen(new KalenderFassung
        {
            Art = KalenderArt.Heimfahrt, Titel = "Heimfahrt (Test)", Beginn = Jetzt.AddDays(-3), Ende = Jetzt.AddHours(5), Bewohner = [id], Erstellt = Jetzt.AddDays(-4), Kuerzel = "EF",
        });
        var z = Assert.Single(s.Abwesenheiten(id, heute.AddDays(-10), heute));
        Assert.Equal((heute.AddDays(-3), heute, "Heimfahrt"), (z.Von, z.Bis, z.Grund));
        Assert.Equal("06.11.–09.11. abwesend: Heimfahrt", AbwesenheitRegeln.BalkenText(z));
        Assert.StartsWith("Heimfahrt bis 09.11. 14:00", s.AbwesenheitJetzt(id, Jetzt));
        Assert.Null(s.AbwesenheitJetzt(id, Jetzt.AddDays(1)));
        Assert.Equal("abwesend: Klinik seit 03.11.", AbwesenheitRegeln.ZeileText("Klinik seit 03.11."));
        Assert.Equal("abwesend ab 09.11. 09:00 (Klinik)", AbwesenheitRegeln.BeginnText(Jetzt, "Klinik"));
        Assert.Equal("zurück am 09.11. 09:00", AbwesenheitRegeln.EndeText(Jetzt));
    }

    [Fact]
    public void Beginn_und_Ende_als_Eintrag_einmal()
    {
        var db = new Datenbank(s);
        var p = db.Laden().First(x => x.Status == AnwesenheitsStatus.Keiner);
        int vorher = s.Berichte(p.BewohnerId).Count;
        db.StatusSetzen(p.BewohnerId, AnwesenheitsStatus.Abwesenheit, "Klinik (Test)", "AB");
        db.StatusSetzen(p.BewohnerId, AnwesenheitsStatus.Keiner, "", "AB");
        var neu = s.Berichte(p.BewohnerId).Where(x => x.Art == DokuRegeln.ArtAbgaengigkeit).OrderBy(x => x.Id).TakeLast(2).Select(x => x.Text).ToList();
        Assert.Equal(vorher + 2, s.Berichte(p.BewohnerId).Count);
        Assert.Equal(["abwesend ab 09.11. 09:00 (Klinik (Test))", "zurück am 09.11. 09:00"], neu);
    }
}
