using DokuLine.Kern;

namespace DokuLine;

// Die schmale Zielleiste (Bauplan 43): „Leitziel: … · Nahziel 1: … · Nahziel 2: …“ über der Berichtsliste und im Kopf
// der Akte, einklappbar. Fehlende Ziele stehen als Wort in Achtung-Farbe da (Regel 5), nie als leere Zeile.
public sealed class ZielLeiste : Panel
{
    public Label Zeile { get; } = new() { AutoSize = true, UseMnemonic = false, Font = Stil.Standard, BackColor = Stil.Seite, Cursor = Cursors.Hand };
    public Button Umschalter { get; } = Stil.KleinerKnopf("Ziele ▾");
    public bool Eingeklappt { get; private set; }
    public bool Achtung { get; private set; }
    public event Action? Geklickt;

    public ZielLeiste()
    {
        BackColor = Stil.Seite;
        Umschalter.AccessibleName = "Zielleiste ein- oder ausklappen";
        Umschalter.Click += (_, _) => Umschalten();
        Zeile.Click += (_, _) => Geklickt?.Invoke();
        Controls.AddRange([Umschalter, Zeile]);
    }

    // Text und Marken der Ziele eines Bewohners.
    public void Zeigen(Speicher speicher, long bewohnerId, DateOnly heute)
    {
        var ziele = speicher.Ziele(bewohnerId);
        var verzicht = speicher.Verzicht(bewohnerId);
        var marken = speicher.ZielMarken(bewohnerId, heute);
        string text = ZielRegeln.Leiste(ziele, verzicht);
        if (marken.Count > 0 && ZielRegeln.Zustand(ziele, verzicht) == ZieleZustand.Eingetragen)
            text += $"  ({string.Join(", ", marken.Select(m => m.Text))})";
        Setzen(text, marken.Count > 0);
    }

    public void Setzen(string text, bool achtung)
    {
        Achtung = achtung;
        Zeile.Text = text;
        Zeile.ForeColor = achtung ? Stil.Achtung.Text : Stil.TextNormal;
        Zeile.Font = achtung ? Stil.FettKursiv : Stil.Standard;
        Zeile.AccessibleName = $"Ziele: {text}";
        PerformLayout();
    }

    public void Umschalten()
    {
        Eingeklappt = !Eingeklappt;
        Zeile.Visible = !Eingeklappt;
        Umschalter.Text = Eingeklappt ? "Ziele ▸" : "Ziele ▾";
        Parent?.PerformLayout();
    }

    public int HoeheFuer(int breite)
    {
        var knopf = Umschalter.GetPreferredSize(Size.Empty);
        int luecke = Stil.Px(this, Stil.AbstandKlein);
        int text = Eingeklappt ? 0 : Zeile.GetPreferredSize(new Size(Math.Max(50, breite - knopf.Width - luecke), 0)).Height;
        return Math.Max(knopf.Height, text) + Stil.Px(this, Stil.AbstandMini);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var knopf = Umschalter.GetPreferredSize(Size.Empty);
        int luecke = Stil.Px(this, Stil.AbstandKlein);
        Umschalter.Bounds = new Rectangle(0, 0, knopf.Width, knopf.Height);
        Zeile.MaximumSize = new Size(Math.Max(50, Width - knopf.Width - luecke), 0);
        var text = Zeile.GetPreferredSize(Zeile.MaximumSize with { Height = 0 });
        Zeile.Location = new Point(knopf.Width + luecke, Math.Max(0, (knopf.Height - Math.Min(text.Height, knopf.Height)) / 2));
    }
}
