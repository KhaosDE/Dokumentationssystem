using DokuLine.Kern;

namespace DokuLine.Tests;

// Startseite: Marken des Tages (Beschluss endet bald, Medikation heute, Termine heute, Heimfahrt),
// Bereich „Heute“, Frist aus den Einstellungen.
[Collection("Datenordner")]
public sealed class UebersichtTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);   // Samstag
    private static readonly DateOnly Heute = new(2026, 11, 7);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public UebersichtTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private HeuteFuer H(string nachname) => s.Heute(Jetzt).Single(h => h.Bewohner.Nachname == nachname);
    private List<string> M(string nachname, int frist = 90) => UebersichtRegeln.Marken(H(nachname), Heute, frist).Select(m => m.Text).ToList();

    [Fact]
    public void Medikation_am_Tag()
    {
        var fichte = H("Fichte");
        var (v, aenderung) = Assert.Single(fichte.Medikation);
        Assert.Contains("Metformin", v.Medikament);
        Assert.Equal("Pause beginnt", aenderung);
        Assert.Contains("Medikation: Pause beginnt", M("Fichte"));
        // Pause endet, neue Stufe und neu verordnet am jeweiligen Tag.
        var clematis = s.Verordnungen(H("Clematis").Bewohner.Id!.Value).Single(x => x.Aenderungen.Count > 0);
        Assert.Equal("Pause endet", UebersichtRegeln.MedikationAm(clematis, Heute.AddDays(4)));
        var birke = s.Verordnungen(H("Birke").Bewohner.Id!.Value).Single();
        Assert.Equal("neue Dosis", UebersichtRegeln.MedikationAm(birke, Heute.AddDays(4)));
        Assert.Equal("neu verordnet", UebersichtRegeln.MedikationAm(birke, birke.Beginn));
        Assert.Null(UebersichtRegeln.MedikationAm(birke, Heute));
    }

    [Fact]
    public void Beschluss_endet_bald_nach_Frist()
    {
        // Clematis: Beschluss bis 15.01.2027 = in 69 Tagen.
        Assert.Contains("Beschluss endet in 69 Tagen", M("Clematis"));
        Assert.DoesNotContain(M("Clematis", frist: 30), t => t.StartsWith("Beschluss endet"));
        Assert.DoesNotContain(M("Ahorn"), t => t.StartsWith("Beschluss"));   // erst 2027 – keine Marke
        Assert.Contains("Beschluss nicht erfasst", M("Distel"));
        var b = H("Ahorn").Bewohner with { Beschlussende = Heute.AddDays(-2) };
        Assert.Contains(UebersichtRegeln.Marken(H("Ahorn") with { Bewohner = b }, Heute, 90), m => m.Text == "Beschluss abgelaufen seit 05.11.2026");
    }

    [Fact]
    public void Termine_und_Heimfahrt_heute()
    {
        Assert.Contains(M("Holunder"), t => t.StartsWith("abwesend: Heimfahrt bis Mo 09.11."));
        var alle = s.Heute(Jetzt);
        Assert.StartsWith("69 Bewohner · 1 abwesend · ", UebersichtRegeln.HeuteText(alle, 1));   // 23 je Wohnbereich (Schritt 26h)
        Assert.All(alle.SelectMany(h => h.Termine), t => Assert.Equal(Heute, t.Tag));
        var mitTermin = alle.First(h => h.Termine.Count > 0);
        Assert.Contains(UebersichtRegeln.Marken(mitTermin, Heute, 90), m => m.Art == MarkenArt.Termin && m.Text.StartsWith("heute "));
        Assert.Equal(UebersichtRegeln.MedikationZeilen(alle).Count, alle.Sum(h => h.Medikation.Count));
    }

    [Fact]
    public void Frist_in_den_Einstellungen()
    {
        Assert.Equal(90, Einstellungen.Laden().BeschlussFristTage);
        Einstellungen.Aendern(w => w with { BeschlussFristTage = 30 });
        Assert.Equal(30, Einstellungen.Laden().BeschlussFristTage);
        Assert.Throws<Eingabefehler>(() => Einstellungen.Aendern(w => w with { BeschlussFristTage = 0 }));
    }
}
