using DokuLine.Kern;

namespace DokuLine;

// Eine Zeile der Bewohnerliste: dieselben Angaben wie eine Bewohnerkarte –
// Marken, Bezugspflege, Einzeltrainer, Aufenthalt –, aber als eine Zeile in der Tabelle.
// In der Übersicht mit der Spalte „Wo“ zwischen Bezug und Hinweisen.
public sealed class BewohnerZeile
{
    public Bewohner Bewohner { get; }
    public IReadOnlyList<Marke> Marken { get; }
    public string AufenthaltText { get; }
    public (string Kuerzel, Color Farbe)? Bezugspflege { get; }
    public (string Text, Color Farbe)? Trainer { get; }
    public ListViewItem Eintrag { get; }
    // Nur in der Übersicht: wo der Bewohner gerade ist; WoMarke ist die Marke, aus der das stammt.
    public string? Wo { get; }
    // Spalte „Auskunft“ (Bauplan 35): Kurzform; die Namen stehen im Hinweis.
    public (string Kurz, string Namen, Entbindungsstand Art)? Auskunft { get; }
    public Marke? WoMarke { get; }
    public bool ImHaus => Wo == HeuteWichtigRegeln.ImHaus;

    public BewohnerZeile(Bewohner bewohner, DateOnly heute, IReadOnlyList<Marke> marken,
        (string Kuerzel, Color Farbe)? bezugspflege, (string Text, Color Farbe)? trainer,
        (string Text, MarkenArt Art, Marke? Quelle)? wo = null, (string Kurz, string Namen, Entbindungsstand Art)? auskunft = null)
    {
        Auskunft = auskunft;
        Bewohner = bewohner;
        Marken = marken;
        AufenthaltText = Kennzeichen.Aufenthalt(bewohner.Aufnahmedatum, heute);
        Bezugspflege = bezugspflege;
        Trainer = trainer;
        Wo = wo?.Text;
        WoMarke = wo?.Quelle;
        string bezug = bezugspflege is { } b ? $"● {b.Kuerzel}" : "";
        string[] spalten = wo is null ? [ZimmerText, bewohner.Name, bezug] : [ZimmerText, bewohner.Name, bezug, Wo!];
        if (wo is not null && auskunft is { } a) spalten = [.. spalten, a.Kurz];
        Eintrag = new ListViewItem([.. spalten, HinweisText])
        {
            Tag = this, UseItemStyleForSubItems = false, ToolTipText = Beschreibung,
        };
        if (bezugspflege is { } bp) Eintrag.SubItems[2].ForeColor = bp.Farbe;   // Farbe nie allein: das Kürzel steht dabei
        if (wo is { } w && !ImHaus) Eintrag.SubItems[3].ForeColor = Stil.MarkeFuer(w.Art).Text;
        if (wo is not null && auskunft is { } au) Eintrag.SubItems[4].ForeColor = Stil.AuskunftFarbe(au.Art).Text;
        var hinweise = Eintrag.SubItems[Eintrag.SubItems.Count - 1];
        if (Hinweise.Any(m => m.Art is MarkenArt.Beschluss or MarkenArt.Allergie)) hinweise.ForeColor = Stil.Beschluss.Text;
        else if (Hinweise.Any(m => m.Art == MarkenArt.NichtErfasst)) hinweise.ForeColor = Stil.Achtung.Text;
    }

    // Alle Marken außer der, die schon in der Spalte „Wo“ steht.
    public IEnumerable<Marke> Hinweise => Marken.Where(m => !ReferenceEquals(m, WoMarke));

    // „101 · Fenster“, „102“ (Einzelzimmer).
    public string ZimmerText => Bewohner.Bett is ZimmerRegeln.Fenster or ZimmerRegeln.Tuer
        ? $"{Bewohner.Zimmer} · {(Bewohner.Bett == ZimmerRegeln.Fenster ? "Fenster" : "Tür")}"
        : Bewohner.Zimmer;

    public string HinweisText => string.Join(" · ", Hinweise.Select(m => m.Text));

    // Für Bildschirmleser und den Hinweis beim Darüberfahren: alles, was zur Zeile gehört (wie auf der Karte).
    public string Beschreibung
    {
        get
        {
            string marken = Hinweise.Any() ? ", " + string.Join(", ", Hinweise.Select(m => m.Text)) : "";
            string bezug = (Bezugspflege is { } b ? $", Bezugspflege {b.Kuerzel}" : "") + (Trainer is { } t ? $", Einzeltrainer {t.Text}" : "");
            string wo = Wo is null ? "" : $", {Wo}";
            string auskunft = Auskunft is { } a ? $", Auskunft: {a.Namen}" : "";
            return $"{Bewohner.Name}, Zimmer {Bewohner.Zimmer}{wo}{marken}{bezug}{auskunft}. Akte öffnen";
        }
    }

    public event Action<Bewohner>? Gewaehlt;
    public void Waehlen() => Gewaehlt?.Invoke(Bewohner);
}
