using DokuLine.Kern;

namespace DokuLine.Tests;

// Übergabe nach Schichten: Schicht des Eintrags, Reihenfolge der Abschnitte und der Einträge.
public class UebergabeSchichtTests
{
    private static readonly DateTime Tag = new(2026, 11, 9);

    private static Uebergabe Eintrag(long id, long? bewohner, DateTime erstellt, bool wichtig = false, string art = DokuRegeln.ArtAlltag) => new()
    {
        Id = id, BewohnerId = bewohner, Erstellt = erstellt, Verfasser = "AB",
        Bericht = bewohner is null ? null : new Bericht
        {
            BewohnerId = bewohner.Value, Zeitpunkt = erstellt, Schicht = BerichtRegeln.SchichtZu(erstellt), Verfasser = "AB",
            Text = "Ausgedachter Text.", Erfasst = erstellt, Wichtig = wichtig, Art = art,
        },
    };

    [Fact]
    public void Laufende_Schicht_oben_dann_die_vorherigen()
    {
        Assert.Equal([Schicht.Frueh, Schicht.Nacht, Schicht.Spaet], UebergabeRegeln.SchichtFolge(Tag.AddHours(9)));
        Assert.Equal([Schicht.Spaet, Schicht.Frueh, Schicht.Nacht], UebergabeRegeln.SchichtFolge(Tag.AddHours(15)));
        Assert.Equal([Schicht.Nacht, Schicht.Spaet, Schicht.Frueh], UebergabeRegeln.SchichtFolge(Tag.AddHours(23)));
        Assert.Equal([Schicht.Nacht, Schicht.Spaet, Schicht.Frueh], UebergabeRegeln.SchichtFolge(Tag.AddHours(3)));
    }

    [Fact]
    public void Schicht_aus_dem_Bericht_sonst_aus_der_Notiz()
    {
        Assert.Equal(Schicht.Spaet, UebergabeRegeln.SchichtVon(Eintrag(1, 5, Tag.AddHours(16))));
        Assert.Equal(Schicht.Nacht, UebergabeRegeln.SchichtVon(Eintrag(2, null, Tag.AddHours(2))));
    }

    [Fact]
    public void Medikation_dann_wichtig_dann_nach_Zimmer_ein_Bewohner_zusammen()
    {
        var zimmer = new Dictionary<long, string> { [1] = "101", [2] = "102", [3] = "103" };
        var eintraege = new[]
        {
            Eintrag(10, 1, Tag.AddHours(7)),
            Eintrag(11, 2, Tag.AddHours(8)),
            Eintrag(12, null, Tag.AddHours(8)),
            Eintrag(13, 3, Tag.AddHours(9), wichtig: true),
            Eintrag(14, 1, Tag.AddHours(10), art: DokuRegeln.ArtMedikation),
        };
        var geordnet = UebergabeRegeln.Ordnen(eintraege, id => zimmer[id]).Select(u => u.Id);
        Assert.Equal([14, 10, 13, 11, 12], geordnet);
    }

    [Fact]
    public void Bis_erledigt_traegt_seit()
    {
        var offen = Eintrag(1, 1, Tag.AddHours(9)) with { GiltBis = null };
        Assert.Equal("seit Mo 09.11.", UebergabeRegeln.SeitText(offen));
        Assert.Null(UebergabeRegeln.SeitText(offen with { GiltBis = Tag.AddDays(1) }));
        Assert.Null(UebergabeRegeln.SeitText(offen with { Erledigt = Tag.AddHours(12) }));
    }
}
