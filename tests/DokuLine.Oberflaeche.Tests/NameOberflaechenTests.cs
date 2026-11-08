using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26a1: Das Programm heißt DokuLine, darunter der Leittext „Betreuung auf Augenhöhe“.
// Jeder Ausdruck trägt unten Name und Demo-Hinweis.
[Collection("Oberflaeche")]
public class NameOberflaechenTests
{
    private static IEnumerable<Control> Alle(Control c) => c.Controls.Cast<Control>().SelectMany(k => Alle(k).Prepend(k));

    [Fact]
    public void Name_und_Leittext() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using (var anmelden = new AnmeldenFenster(u.Speicher, ""))
        {
            var texte = Alle(anmelden).OfType<Label>().Select(l => l.Text).ToList();
            Assert.Contains("DokuLine", texte);
            Assert.Contains("Betreuung auf Augenhöhe", texte);
            Assert.DoesNotContain(texte, t => t.Contains("Dokumentationssystem"));
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.Equal("DokuLine – Demo — ausgedachte Daten", f.Text);
        Assert.Equal("Betreuung auf Augenhöhe", f.LeittextAnzeige.Text);
        Assert.True(f.LeittextAnzeige.Visible);
        Hilfe.Foto(f, "97_dokuline");
        using var fortschritt = new Fortschrittsfenster();
        Assert.Equal("DokuLine – Update", fortschritt.Text);
    });

    private sealed class Seite : PrintDocument
    {
        public void Drucken(PrintPageEventArgs e) => OnPrintPage(e);
    }

    [Fact]
    public void Fusszeile_in_jedem_Ausdruck() => Hilfe.Sta(() =>
    {
        int inhalt = 0;
        var dokument = (Seite)Druckvorschau.MitFusszeile(() =>
        {
            var d = new Seite();
            d.PrintPage += (_, _) => inhalt++;
            return d;
        })();
        using var bild = new Bitmap(850, 1100);
        using var g = Graphics.FromImage(bild);
        g.Clear(Color.White);
        dokument.Drucken(new PrintPageEventArgs(g, new Rectangle(100, 100, 650, 900), new Rectangle(0, 0, 850, 1100), new PageSettings()));
        Assert.Equal(1, inhalt);   // der eigentliche Inhalt wird weiter gezeichnet
        bool gezeichnet = false;
        for (int x = 100; x < 400 && !gezeichnet; x++)
            for (int y = 1000; y < 1070 && !gezeichnet; y++)
                gezeichnet = bild.GetPixel(x, y).R < 200;
        Assert.True(gezeichnet);
        Assert.Equal("DokuLine · Demo — ausgedachte Daten", Druckvorschau.FusszeileText);
    });
}
