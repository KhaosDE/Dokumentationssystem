using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26a3: Entwürfe beim Abmelden – gehören dem Kürzel, je Bewohner einer, mit dem Bewohner weg.
[Collection("Datenordner")]
public sealed class EntwurfTests : IDisposable
{
    private readonly Datenordner daten = new();
    private readonly Speicher s = new();

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    [Fact]
    public void Entwurf_je_Kuerzel_und_Bewohner()
    {
        s.DemoBeimErstenStart();
        long ahorn = s.AlleBewohner().First(b => b.Nachname == "Ahorn").Id!.Value;
        var eingabe = new BerichtEingabe { BewohnerId = ahorn, Text = "Angefangen …", Bereiche = [2, 4], Wichtig = true, Schicht = Schicht.Spaet };
        int meldungen = 0;
        s.EntwuerfeGeaendert += () => meldungen++;
        s.EntwurfSpeichern("CD", EntwurfRegeln.Bericht, ahorn, EntwurfRegeln.AlsJson(eingabe), new DateTime(2026, 11, 8, 14, 5, 0));
        Assert.Empty(s.Entwuerfe("AB"));   // gehört nur CD
        var e = Assert.Single(s.Entwuerfe("cd"));
        Assert.Equal(new DateTime(2026, 11, 8, 14, 5, 0), e.Geaendert);
        var gelesen = EntwurfRegeln.BerichtLesen(e.Inhalt)!;
        Assert.Equal("Angefangen …", gelesen.Text);
        Assert.Equal([2, 4], gelesen.Bereiche.Order());
        Assert.Equal(Schicht.Spaet, gelesen.Schicht);
        Assert.True(gelesen.Wichtig);
        // Noch einmal für denselben Bewohner: überschreibt, kein zweiter Entwurf.
        s.EntwurfSpeichern("CD", EntwurfRegeln.Bericht, ahorn, EntwurfRegeln.AlsJson(new BerichtEingabe { BewohnerId = ahorn, Text = "weiter" }), new DateTime(2026, 11, 8, 15, 0, 0));
        Assert.Equal("weiter", EntwurfRegeln.BerichtLesen(Assert.Single(s.Entwuerfe("CD")).Inhalt)!.Text);
        Assert.NotNull(s.EntwurfFuer("CD", EntwurfRegeln.Bericht, ahorn));
        Assert.Null(EntwurfRegeln.BerichtLesen("{kaputt"));
        Assert.Throws<Eingabefehler>(() => s.EntwurfSpeichern("", EntwurfRegeln.Bericht, ahorn, "{}", DateTime.Now));
        s.EntwurfLoeschen(e.Id);
        Assert.Empty(s.Entwuerfe("CD"));
        Assert.Equal(3, meldungen);
        // Demo-Daten zurücksetzen: Bewohner neu, Entwürfe weg.
        s.EntwurfSpeichern("CD", EntwurfRegeln.Bericht, ahorn, "{}", DateTime.Now);
        s.DemoZuruecksetzen();
        Assert.Empty(s.Entwuerfe("CD"));
    }
}
