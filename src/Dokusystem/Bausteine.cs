using System.Drawing.Drawing2D;

namespace Dokusystem;

// Eine weiße Fläche mit 1-px-Rand und abgerundeten Ecken (Radius 8) auf der Seitenfarbe.
// Darin liegende Steuerelemente bekommen die Kartenfarbe als Hintergrund.
public class KartenFlaeche : Panel
{
    public KartenFlaeche()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Stil.Flaeche;
        Padding = new Padding(Stil.AbstandGross);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Stil.Seite);
        Stil.FlaecheZeichnen(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f),
            Stil.RadiusKarte * Stil.Skala(this), Stil.Flaeche, Stil.KarteRand);
    }
}

// Eine kleine Marke (z. B. „Zimmer 101“ oder „Demo — ausgedachte Daten“).
public sealed class MarkenFeld : Control
{
    private Stil.Marke marke;

    public MarkenFeld(string text, Stil.Marke marke)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        this.marke = marke;
        Text = text;
        AccessibleRole = AccessibleRole.StaticText;
        Size = GetPreferredSize(Size.Empty);
    }

    public Stil.Marke Marke
    {
        get => marke;
        set { marke = value; Invalidate(); }
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        AccessibleName = Text;
        Size = GetPreferredSize(Size.Empty);
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize) => new(
        TextRenderer.MeasureText(Text, Stil.MarkeSchrift, Size.Empty, TextFormatFlags.NoPadding).Width + Stil.Px(this, 16) + 1,
        Stil.Px(this, 22) + 1);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Stil.Flaeche);
        Stil.MarkeZeichnen(g, Text, 0, 0, marke, Stil.Skala(this));
    }
}

// Ein deutlicher Hinweis über die volle Breite, z. B. der Allergiehinweis in der Akte.
// Farbe nie allein: Der Text sagt immer, was gilt; links ein farbiger Balken.
public sealed class HinweisFeld : Control
{
    private Stil.Marke marke = Stil.Achtung;
    private bool gestrichelt;

    public HinweisFeld()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Font = new Font("Segoe UI Semibold", 11F);
        AccessibleRole = AccessibleRole.StaticText;
        Height = Stil.Px(this, 48);
    }

    public Stil.Marke Marke => marke;
    public bool Gestrichelt => gestrichelt;

    public void Setzen(string text, Stil.Marke neueMarke, bool mitStrichelrand)
    {
        Text = text;
        AccessibleName = text;
        marke = neueMarke;
        gestrichelt = mitStrichelrand;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Stil.Seite);
        float s = Stil.Skala(this);
        var feld = new RectangleF(0.5f + s, 0.5f + s, Width - 1.5f - 2 * s, Height - 1.5f - 2 * s);
        Color rand = marke.Rand ?? Stil.Mischen(marke.Flaeche, marke.Text, 0.25);
        Stil.FlaecheZeichnen(g, feld, Stil.RadiusKarte * s, marke.Flaeche, rand, gestrichelt ? 2f * s : 1f, gestrichelt);
        using (var balken = new SolidBrush(marke.Text))
        using (var pfad = Stil.Abgerundet(new RectangleF(feld.Left, feld.Top, 4 * s, feld.Height), 2 * s))
            g.FillPath(balken, pfad);
        var text = Rectangle.Round(new RectangleF(feld.Left + Stil.Px(this, Stil.AbstandGross), feld.Top,
            feld.Width - 2 * Stil.Px(this, Stil.AbstandGross), feld.Height));
        TextRenderer.DrawText(g, Text, Font, text, marke.Text,
            TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
    }
}

// Ein Eintrag in der Seitenleiste. Aktiv: heller Akzent als Fläche, Akzent als Textfarbe,
// links ein schmaler Balken (docs/gestaltung.md).
public sealed class SeitenleistenEintrag : Control
{
    private bool aktiv;
    private bool maus;

    public SeitenleistenEintrag(string text)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Text = text;
        Font = Stil.Standard;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PageTab;
        AccessibleName = text;
        Height = Stil.Px(this, 40);
    }

    public bool Aktiv
    {
        get => aktiv;
        set { aktiv = value; Invalidate(); }
    }

    // Zähler rechts im Eintrag (z. B. offene Aufgaben); null = keiner. Dringend = rot (es ist
    // etwas überfällig); der Bildschirmleser bekommt beides als Text.
    public int? Anzahl { get; private set; }
    public bool Dringend { get; private set; }

    public void ZaehlerSetzen(int? anzahl, bool dringend = false, string? beschreibung = null)
    {
        Anzahl = anzahl;
        Dringend = dringend;
        AccessibleName = anzahl is int n ? $"{Text}, {beschreibung ?? n.ToString()}" : Text;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { maus = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { maus = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    // Wie ein Klick (auch für Tastatur: Eingabe oder Leertaste).
    public void Ausloesen() => OnClick(EventArgs.Empty);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space) { Ausloesen(); e.Handled = true; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Stil.Fensterhintergrund);
        float s = Stil.Skala(this);
        var feld = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Color? flaeche = aktiv ? Stil.AkzentHell : maus ? Stil.Mischen(Stil.Fensterhintergrund, Stil.Akzent, 0.06) : null;
        if (flaeche is Color f)
            Stil.FlaecheZeichnen(g, feld, Stil.RadiusKnopf * s, f, Focused ? Stil.Fokus : f);
        else if (Focused)
            Stil.FlaecheZeichnen(g, feld, Stil.RadiusKnopf * s, Stil.Fensterhintergrund, Stil.Fokus);
        if (aktiv)
        {
            using var balken = new SolidBrush(Stil.Akzent);
            using var pfad = Stil.Abgerundet(new RectangleF(feld.Left + 2 * s, feld.Top + Height * 0.25f, 3 * s, Height * 0.5f), 1.5f * s);
            g.FillPath(balken, pfad);
        }
        TextRenderer.DrawText(g, Text, aktiv ? Stil.Fett : Stil.Standard,
            new Rectangle(Stil.Px(this, Stil.AbstandGross), 0, Width - Stil.Px(this, Stil.AbstandGross), Height),
            aktiv ? Stil.Akzent : Stil.TextNormal, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        if (Anzahl is int anzahl)
        {
            var farbe = Dringend ? Stil.Beschluss : Stil.Zimmer;
            string zahl = anzahl.ToString();
            int breite = Math.Max(Stil.Px(this, 24), TextRenderer.MeasureText(zahl, Stil.MarkeSchrift).Width + Stil.Px(this, 8));
            var zaehler = new Rectangle(Width - breite - Stil.Px(this, Stil.AbstandKlein), (Height - Stil.Px(this, 20)) / 2, breite, Stil.Px(this, 20));
            Stil.FlaecheZeichnen(g, zaehler, 10 * s, farbe.Flaeche, farbe.Flaeche);
            TextRenderer.DrawText(g, zahl, Stil.MarkeSchrift, zaehler, farbe.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }
}

// Drei Zustände (Regel 5): eingetragen (mit Eingabefeld) / zweite Möglichkeit / nicht erfasst.
// Vorgabe ist immer „nicht erfasst“.
public sealed class Dreiwahl : TableLayoutPanel
{
    private readonly string[] kuerzel;
    public RadioButton[] Knoepfe { get; }
    public TextBox Feld { get; } = new();

    public Dreiwahl(string[] kuerzel, string[] beschriftungen, string platzhalter)
    {
        this.kuerzel = kuerzel;
        ColumnCount = 2;
        RowCount = 3;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Margin = new Padding(0);
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Knoepfe = beschriftungen.Select(t => new RadioButton
        {
            Text = t, AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextNormal,
            Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandMini),
        }).ToArray();
        for (int i = 0; i < 3; i++) Controls.Add(Knoepfe[i], 0, i);
        Feld.PlaceholderText = platzhalter;
        Feld.Font = Stil.Standard;
        Feld.Dock = DockStyle.Fill;
        Feld.Margin = new Padding(0, 0, 0, Stil.AbstandMini);
        Controls.Add(Feld, 1, 0);
        foreach (var knopf in Knoepfe) knopf.CheckedChanged += (_, _) => Feld.Enabled = Knoepfe[0].Checked;
        Setzen(kuerzel[2], "");
    }

    // Unbekannter Zustand oder nichts gewählt: immer „nicht erfasst“ (Index 2), nie „eingetragen“.
    public void Setzen(string zustand, string text)
    {
        int i = Array.IndexOf(kuerzel, zustand);
        if (i < 0) i = 2;
        Knoepfe[i].Checked = true;
        Feld.Text = text;
        Feld.Enabled = i == 0;
    }

    public string Zustand
    {
        get
        {
            int i = Array.FindIndex(Knoepfe, k => k.Checked);
            return kuerzel[i < 0 ? 2 : i];
        }
    }
}

// Ein Filterknopf mit Zähler („Beschluss 5“), nach Vorbild der Ausgangsliste; Ecken mit Radius 4
// statt Pillenform (docs/gestaltung.md). Aktiv: Akzent als Fläche, weiße Schrift.
public sealed class FilterKnopf : Control
{
    private bool aktiv;
    private bool maus;
    private int anzahl;

    public FilterKnopf(string text)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Text = text;
        Font = Stil.Standard;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        Aktualisieren();
    }

    public bool Aktiv
    {
        get => aktiv;
        set { aktiv = value; Invalidate(); }
    }

    public int Anzahl
    {
        get => anzahl;
        set { anzahl = value; Aktualisieren(); }
    }

    public void Ausloesen() => OnClick(EventArgs.Empty);

    private void Aktualisieren()
    {
        Size = GetPreferredSize(Size.Empty);
        AccessibleName = $"{Text}: {anzahl}";
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Stil.Standard, Size.Empty, TextFormatFlags.NoPadding);
        Size zahl = TextRenderer.MeasureText(anzahl.ToString(), Stil.Fett, Size.Empty, TextFormatFlags.NoPadding);
        return new Size(text.Width + zahl.Width + Stil.Px(this, Stil.AbstandKlein + 2 * Stil.AbstandMittel), Stil.Px(this, 32));
    }

    protected override void OnMouseEnter(EventArgs e) { maus = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { maus = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space) { Ausloesen(); e.Handled = true; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Stil.Seite);
        float s = Stil.Skala(this);
        var feld = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        Color flaeche = aktiv ? Stil.Akzent : maus ? Stil.Mischen(Stil.Flaeche, Stil.Akzent, 0.08) : Stil.Flaeche;
        Color rand = aktiv ? Stil.Akzent : (maus || Focused) ? Stil.Mischen(Stil.KarteRand, Stil.Fokus, 0.5) : Stil.KarteRand;
        Color schrift = aktiv ? Color.White : Stil.TextNormal;
        Stil.FlaecheZeichnen(g, feld, Stil.RadiusKnopf * s, flaeche, rand, Focused ? 2f : 1f);
        Size text = TextRenderer.MeasureText(g, Text, Stil.Standard, Size.Empty, TextFormatFlags.NoPadding);
        int x = Stil.Px(this, Stil.AbstandMittel);
        TextRenderer.DrawText(g, Text, Stil.Standard, new Rectangle(x, 0, text.Width, Height), schrift,
            TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        TextRenderer.DrawText(g, anzahl.ToString(), Stil.Fett,
            new Rectangle(x + text.Width + Stil.Px(this, Stil.AbstandKlein), 0, Width, Height), schrift,
            TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

// Statusleiste unten mit dem Farbschlüssel der Marken (Farbe nie allein, docs/gestaltung.md).
public sealed class Statusleiste : Control
{
    public Statusleiste()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Stil.Fensterhintergrund;
        Height = Stil.Px(this, 30);
        AccessibleRole = AccessibleRole.StatusBar;
        AccessibleName = "Farbschlüssel: " + string.Join("; ", Stil.Farbschluessel.Select(f => f.Text));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        float s = Stil.Skala(this);
        using (var linie = new Pen(Stil.Trennlinie)) g.DrawLine(linie, 0, 0, Width, 0);
        int x = Stil.Px(this, Stil.AbstandGross);
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
        const string titel = "Farbschlüssel:";
        TextRenderer.DrawText(g, titel, Stil.Klein, new Rectangle(x, 0, Width, Height), Stil.TextLeise, flags);
        x += TextRenderer.MeasureText(g, titel, Stil.Klein, Size.Empty, TextFormatFlags.NoPadding).Width + Stil.Px(this, Stil.AbstandMittel);
        foreach (var (marke, text) in Stil.Farbschluessel)
        {
            int seite = Stil.Px(this, 12);
            var feld = new RectangleF(x, (Height - seite) / 2f, seite, seite);
            Stil.FlaecheZeichnen(g, feld, 2.5f * s, marke.Flaeche, marke.Rand ?? marke.Text);
            x += seite + Stil.Px(this, Stil.AbstandMini + 2);
            TextRenderer.DrawText(g, text, Stil.Klein, new Rectangle(x, 0, Width, Height), Stil.TextMittel, flags);
            x += TextRenderer.MeasureText(g, text, Stil.Klein, Size.Empty, TextFormatFlags.NoPadding).Width + Stil.Px(this, Stil.AbstandGross);
        }
    }
}
