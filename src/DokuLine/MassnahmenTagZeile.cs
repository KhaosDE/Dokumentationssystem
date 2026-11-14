namespace DokuLine;

// In der Dokumentation je Tag: „▸ Maßnahmen: 3 durchgeführt, 1 abgelehnt“ – ein Klick klappt die einzelnen Einträge auf.
public sealed class MassnahmenTagZeile : LinkLabel
{
    private readonly string text;
    public bool Offen { get; private set; }
    public event Action<bool>? Umgeschaltet;

    public MassnahmenTagZeile(string zusammenfassung)
    {
        text = zusammenfassung;
        AutoSize = false;
        Font = Stil.Standard;
        BackColor = Stil.Seite;
        LinkColor = Stil.Akzent;
        ActiveLinkColor = Stil.Akzent;
        LinkBehavior = LinkBehavior.HoverUnderline;
        UseMnemonic = false;
        AccessibleName = zusammenfassung;
        TextSetzen();
        LinkClicked += (_, _) => Umschalten();
    }

    public void Umschalten()
    {
        Offen = !Offen;
        TextSetzen();
        Umgeschaltet?.Invoke(Offen);
    }

    private void TextSetzen() => Text = $"{(Offen ? "▾" : "▸")} {text}";
}
