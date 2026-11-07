using Dokusystem.Kern;

namespace Dokusystem.Tests;

// Marken, Filter und Aufenthaltsdauer der Bewohnerkarten (Gestaltung Stufe 2).
public class KennzeichenTests
{
    private static Bewohner Neuer(Action<BewohnerEingabe>? aendern = null)
    {
        var e = new BewohnerEingabe { Vorname = "A", Nachname = "B", Zimmer = "1", Geburtsdatum = "01.01.1970", Aufnahmedatum = "01.01.2020" };
        aendern?.Invoke(e);
        return BewohnerRegeln.AusEingabe(e);
    }

    [Fact]
    public void Normalfall_ohne_Marke()
    {
        Assert.Empty(Kennzeichen.Marken(Neuer(e => { e.Allergie = "keine_bekannt"; e.Beschluss = "kein_beschluss"; })));
    }

    [Fact]
    public void Marken_mit_Wort()
    {
        var b = Neuer(e => { e.Allergie = "eingetragen"; e.Allergien = "Erdbeeren"; e.Beschluss = "ende"; e.Beschlussende = "30.06.2027"; });
        Assert.Equal([new Marke(MarkenArt.Beschluss, "Beschluss bis 30.06.2027"), new Marke(MarkenArt.Allergie, "Allergie: Erdbeeren")],
            Kennzeichen.Marken(b));
    }

    [Fact]
    public void Uebergabe_und_Wichtig_stehen_vorn()
    {
        var b = Neuer(e => { e.Allergie = "keine_bekannt"; e.Beschluss = "kein_beschluss"; });
        Assert.Equal([new Marke(MarkenArt.Uebergabe, "Übergabe 2"), new Marke(MarkenArt.Wichtig, "Wichtig")],
            Kennzeichen.Marken(b, ungeleseneUebergaben: 2, wichtigeBerichte: 1));
    }

    [Fact]
    public void Nicht_erfasst_ist_sichtbar_Regel_5()
    {
        var marken = Kennzeichen.Marken(Neuer());
        Assert.Equal(["Allergien nicht erfasst", "Beschluss nicht erfasst"], marken.Select(m => m.Text));
        Assert.All(marken, m => Assert.Equal(MarkenArt.NichtErfasst, m.Art));
    }

    [Fact]
    public void Filter_zaehlen_die_Demo_Daten()
    {
        var alle = Demodaten.Bewohner();
        int Zahl(BewohnerFilter f) => alle.Count(b => Kennzeichen.Passt(b, f));
        Assert.Equal(alle.Count, Zahl(BewohnerFilter.Alle));
        Assert.Equal(5, Zahl(BewohnerFilter.Beschluss));
        Assert.Equal(4, Zahl(BewohnerFilter.Allergie));
        Assert.Equal(4, Zahl(BewohnerFilter.NichtErfasst));   // Clematis, Distel, Ginster, Kiefer
        Assert.Equal("Nicht erfasst", Kennzeichen.FilterText(BewohnerFilter.NichtErfasst));
    }

    [Theory]
    [InlineData("01.09.2019", "07.11.2026", "seit 01.09.2019 · 7 Jahre, 2 Monate")]
    [InlineData("07.11.2025", "07.11.2026", "seit 07.11.2025 · 1 Jahr")]
    [InlineData("08.11.2025", "07.11.2026", "seit 08.11.2025 · 11 Monate")]
    [InlineData("07.10.2026", "07.11.2026", "seit 07.10.2026 · 1 Monat")]
    [InlineData("03.11.2026", "07.11.2026", "seit 03.11.2026 · 4 Tage")]
    [InlineData("06.11.2026", "07.11.2026", "seit 06.11.2026 · 1 Tag")]
    [InlineData("10.11.2026", "07.11.2026", "Aufnahme am 10.11.2026")]
    public void Aufenthaltsdauer(string aufnahme, string heute, string erwartet)
    {
        Assert.Equal(erwartet, Kennzeichen.Aufenthalt(Datum.Lesen(aufnahme, "a"), Datum.Lesen(heute, "h")));
    }
}
