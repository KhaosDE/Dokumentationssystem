using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 49: die Anbindung an die Rechtschreibprüfung von Windows (ISpellChecker) – mit Englisch, das auf den
// Testrechnern vorhanden ist – und der leise Hinweis im „Neuer Bericht“ mit einem ausgedachten Prüfer.
[Collection("Oberflaeche")]
public class RechtschreibungOberflaechenTests
{
    private sealed class Testpruefer : IRechtschreibung
    {
        public IReadOnlyList<(int Start, int Laenge)> Fehler(string text) =>
            [.. System.Text.RegularExpressions.Regex.Matches(text, @"\bwra\b|\bhatt\b|\bAhorn\b").Select(m => (m.Index, m.Length))];
        public IReadOnlyList<string> Vorschlaege(string wort) => ["war"];
        public void Ignorieren(string wort) { }
    }

    [Fact]
    public void Windows_Pruefer_findet_Tippfehler_wenn_vorhanden() => Hilfe.Sta(() =>
    {
        // Ohne englisches Sprachpaket (selten auf Windows) gibt es nichts zu prüfen.
        if (WindowsRechtschreibung.Erstellen("en-US") is not WindowsRechtschreibung p) return;
        var fehler = p.Fehler("This is a tset of the speling.");
        Assert.Contains(fehler, f => f == (10, 4));
        Assert.Contains(fehler, f => f == (22, 7));
        Assert.Contains("test", p.Vorschlaege("tset"));
    });

    [Fact]
    public void Hinweis_im_Bericht_ohne_eigene_Woerter() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        var vorher = Rechtschreibpruefung.Erstellen;
        try
        {
            Rechtschreibpruefung.Erstellen = () => new Testpruefer();
            Rechtschreibpruefung.Zuruecksetzen();
            using var formular = new BerichtFormular(u.Speicher, u.Demo("Ahorn"));
            formular.Inhalt.Text = "Herr Ahorn wra ruhig und hatt geschlafen.";
            formular.InhaltPruefung.Pruefen();
            Assert.Equal(["wra", "hatt"], formular.InhaltPruefung.Fehler.Select(f => f.Wort));   // „Ahorn“ ist ein Bewohnername
            Assert.Equal("2 Wörter unbekannt", formular.RechtschreibHinweis.Text);
        }
        finally
        {
            Rechtschreibpruefung.Erstellen = vorher;
            Rechtschreibpruefung.Zuruecksetzen();
        }
    });
}
