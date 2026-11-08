using DokuLine.Kern;

namespace DokuLine;

// Eine Zeile der Bewohnerliste (Bauplan Schritt 26j): dieselben Angaben wie eine Bewohnerkarte –
// Marken, Bezugspflege, Einzeltrainer, Aufenthalt –, aber als eine Zeile in der Tabelle.
public sealed class BewohnerZeile
{
    public Bewohner Bewohner { get; }
    public IReadOnlyList<Marke> Marken { get; }
    public string AufenthaltText { get; }
    public (string Kuerzel, Color Farbe)? Bezugspflege { get; }
    public (string Text, Color Farbe)? Trainer { get; }
    public ListViewItem Eintrag { get; }

    public BewohnerZeile(Bewohner bewohner, DateOnly heute, IReadOnlyList<Marke> marken,
        (string Kuerzel, Color Farbe)? bezugspflege, (string Text, Color Farbe)? trainer)
    {
        Bewohner = bewohner;
        Marken = marken;
        AufenthaltText = Kennzeichen.Aufenthalt(bewohner.Aufnahmedatum, heute);
        Bezugspflege = bezugspflege;
        Trainer = trainer;
        Eintrag = new ListViewItem([ZimmerText, bewohner.Name, bezugspflege is { } b ? $"● {b.Kuerzel}" : "", HinweisText])
        {
            Tag = this, UseItemStyleForSubItems = false, ToolTipText = Beschreibung,
        };
        if (bezugspflege is { } bp) Eintrag.SubItems[2].ForeColor = bp.Farbe;   // Farbe nie allein: das Kürzel steht dabei
        if (marken.Any(m => m.Art is MarkenArt.Beschluss or MarkenArt.Allergie)) Eintrag.SubItems[3].ForeColor = Stil.Beschluss.Text;
        else if (marken.Any(m => m.Art == MarkenArt.NichtErfasst)) Eintrag.SubItems[3].ForeColor = Stil.Achtung.Text;
    }

    // „101 · Fenster“, „102“ (Einzelzimmer).
    public string ZimmerText => Bewohner.Bett is ZimmerRegeln.Fenster or ZimmerRegeln.Tuer
        ? $"{Bewohner.Zimmer} · {(Bewohner.Bett == ZimmerRegeln.Fenster ? "Fenster" : "Tür")}"
        : Bewohner.Zimmer;

    public string HinweisText => string.Join(" · ", Marken.Select(m => m.Text));

    // Für Bildschirmleser und den Hinweis beim Darüberfahren: alles, was zur Zeile gehört (wie auf der Karte).
    public string Beschreibung
    {
        get
        {
            string marken = Marken.Count > 0 ? ", " + string.Join(", ", Marken.Select(m => m.Text)) : "";
            string bezug = (Bezugspflege is { } b ? $", Bezugspflege {b.Kuerzel}" : "") + (Trainer is { } t ? $", Einzeltrainer {t.Text}" : "");
            return $"{Bewohner.Name}, Zimmer {Bewohner.Zimmer}{marken}{bezug}. Akte öffnen";
        }
    }

    public event Action<Bewohner>? Gewaehlt;
    public void Waehlen() => Gewaehlt?.Invoke(Bewohner);
}
