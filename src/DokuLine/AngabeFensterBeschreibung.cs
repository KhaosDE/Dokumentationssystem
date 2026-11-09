using DokuLine.Kern;

namespace DokuLine;

// Personenbeschreibung (Bauplan 34): für das Meldeblatt bei einer Abgängigkeit.
public sealed partial class AngabeFenster
{
    public TextBox Groesse { get; private set; } = null!;
    public TextBox Merkmale { get; private set; } = null!;

    private void BeschreibungBauen(Action<string, Control> feld, Personenbeschreibung? p)
    {
        Groesse = Feld(p?.Groesse ?? "", 120);
        Groesse.PlaceholderText = "z. B. 1,78 m";
        var statur = Feld(p?.Statur ?? "");
        statur.PlaceholderText = "z. B. schlank";
        var haare = Feld(p?.Haarfarbe ?? "");
        var augen = Feld(p?.Augenfarbe ?? "");
        var brille = Feld(p?.Brille ?? "");
        brille.PlaceholderText = "z. B. keine, Lesebrille, immer";
        Merkmale = new TextBox
        {
            Text = p?.Merkmale ?? "", Width = 320, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "z. B. Gangbild, Narben, Tätowierungen"
        };
        feld("Größe", Groesse);
        feld("Statur", statur);
        feld("Haarfarbe", haare);
        feld("Augenfarbe", augen);
        feld("Brille", brille);
        feld("Besondere Merkmale", Merkmale);
        lesen = () => new Personenbeschreibung
        {
            Groesse = Groesse.Text.Trim(), Statur = statur.Text.Trim(), Haarfarbe = haare.Text.Trim(), Augenfarbe = augen.Text.Trim(),
            Brille = brille.Text.Trim(), Merkmale = Merkmale.Text.Trim(),
        };
    }
}
