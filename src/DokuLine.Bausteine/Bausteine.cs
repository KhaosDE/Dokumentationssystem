using System.Drawing.Drawing2D;

namespace DokuLine;

// Eine weiße Fläche mit 1-px-Rand und abgerundeten Ecken (Radius 8) auf der Seitenfarbe.
// Darin liegende Steuerelemente bekommen die Kartenfarbe als Hintergrund.
public class KartenFlaeche : Panel
{
    public KartenFlaeche()
    {
        // Kein Layout im Konstruktor: Abgeleitete Kästen haben ihre Teile hier noch nicht angelegt.
        SuspendLayout();
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Stil.Flaeche;
        Padding = new Padding(Stil.AbstandGross);
        ResumeLayout(false);
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
// links ein schmaler Balken (docs/gestaltung.md). Mit Symbol vor dem Text; eingeklappt nur das
// Symbol, der Zähler als kleine Zahl daran.
public sealed class SeitenleistenEintrag : Control
{
    private bool aktiv;
    private bool maus;
    private bool eingeklappt;

    public string Symbol { get; set; } = "";

    public bool Eingeklappt
    {
        get => eingeklappt;
        set { eingeklappt = value; Invalidate(); }
    }

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
        var textFarbe = aktiv ? Stil.Akzent : Stil.TextNormal;
        int links = Stil.Px(this, Stil.AbstandGross);
        if (Symbol.Length > 0)
        {
            int symbolBreite = Stil.Px(this, 24);
            var symbolFeld = eingeklappt
                ? new Rectangle((Width - symbolBreite) / 2, 0, symbolBreite, Height)
                : new Rectangle(links - Stil.Px(this, 4), 0, symbolBreite, Height);
            TextRenderer.DrawText(g, Symbol, Stil.Symbolschrift, symbolFeld, textFarbe,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            links = symbolFeld.Right + Stil.Px(this, Stil.AbstandKlein);
            if (eingeklappt)
            {
                if (Anzahl is int klein) ZahlZeichnen(g, klein, new Rectangle(symbolFeld.Right - Stil.Px(this, 8), Stil.Px(this, 3), 0, Stil.Px(this, 16)), s);
                return;
            }
        }
        TextRenderer.DrawText(g, Text, aktiv ? Stil.Fett : Stil.Standard,
            new Rectangle(links, 0, Width - links, Height),
            textFarbe, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
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

    // Eingeklappt: die Zahl klein oben rechts am Symbol (feld.Width 0 = nach dem Text bemessen).
    private void ZahlZeichnen(Graphics g, int anzahl, Rectangle feld, float s)
    {
        var marke = Dringend ? Stil.Beschluss : Stil.Zimmer;
        string zahl = anzahl.ToString();
        feld.Width = Math.Max(feld.Height, TextRenderer.MeasureText(zahl, Stil.Klein).Width + Stil.Px(this, 4));
        Stil.FlaecheZeichnen(g, feld, 8 * s, marke.Flaeche, marke.Text);
        TextRenderer.DrawText(g, zahl, Stil.Klein, feld, marke.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
    }
}

// Drei Zustände (Regel 5): eingetragen (mit Eingabefeld) / zweite Möglichkeit / nicht erfasst.
// Vorgabe ist immer „nicht erfasst“.
public sealed class Dreiwahl : TableLayoutPanel
{
    private readonly string[] kuerzel;
    public RadioButton[] Knoepfe { get; }
    public TextBox Feld { get; }

    // platzhalter = null: ohne Textfeld, die drei Knöpfe nebeneinander (z. B. „offen geführt“).
    public Dreiwahl(string[] kuerzel, string[] beschriftungen, string? platzhalter)
    {
        this.kuerzel = kuerzel;
        bool mitFeld = platzhalter is not null;
        Feld = platzhalter == "TT.MM.JJJJ" ? new DatumFeld() : new TextBox();   // Schritt 26d
        ColumnCount = mitFeld ? 2 : 3;
        RowCount = mitFeld ? 3 : 1;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Margin = new Padding(0);
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ColumnStyles.Add(new ColumnStyle(mitFeld ? SizeType.Percent : SizeType.AutoSize, mitFeld ? 100 : 0));
        if (!mitFeld) ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Knoepfe = beschriftungen.Select(t => new RadioButton
        {
            Text = t, AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextNormal,
            Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandMini),
        }).ToArray();
        for (int i = 0; i < 3; i++) Controls.Add(Knoepfe[i], mitFeld ? 0 : i, mitFeld ? i : 0);
        Feld.PlaceholderText = platzhalter ?? "";
        Feld.Font = Stil.Standard;
        Feld.Dock = DockStyle.Fill;
        Feld.Margin = new Padding(0, 0, 0, Stil.AbstandMini);
        if (mitFeld) Controls.Add(Feld, 1, 0);
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
    private readonly bool mitZahl;

    // mitZahl: false für reine Umschalter (z. B. Monat/Woche/Tag), dann ohne Zähler.
    public FilterKnopf(string text, bool mitZahl = true)
    {
        this.mitZahl = mitZahl;
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
        AccessibleName = mitZahl ? $"{Text}: {anzahl}" : Text;
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Stil.Standard, Size.Empty, TextFormatFlags.NoPadding);
        if (!mitZahl) return new Size(text.Width + Stil.Px(this, 2 * Stil.AbstandMittel), Stil.Px(this, 32));
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
        if (mitZahl)
            TextRenderer.DrawText(g, anzahl.ToString(), Stil.Fett,
                new Rectangle(x + text.Width + Stil.Px(this, Stil.AbstandKlein), 0, Width, Height), schrift,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

// Statusleiste unten mit dem Farbschlüssel der Marken (Farbe nie allein, docs/gestaltung.md).
public sealed class Statusleiste : Control
{
    // Bezugspflege und Einzeltrainer haben die Farbe der Person (Schritt 20) – das Kürzel steht immer dabei.
    public const string BezugText = "Streifen: Bezugspflege, Punkt: Einzeltrainer";
    private static readonly string Schluessel = "Farbschlüssel: " + string.Join("; ", Stil.Farbschluessel.Select(f => f.Text)) + "; " + BezugText;
    private string statusText = "";

    // Links vor dem Farbschlüssel (Schritt 26l): „DEMO — ausgedachte Daten · Datum · Dienst · Stand“.
    public string StatusText
    {
        get => statusText;
        set
        {
            if (statusText == value) return;
            statusText = value;
            AccessibleName = (value.Length > 0 ? value + "; " : "") + Schluessel;
            Invalidate();
        }
    }

    public Statusleiste()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Stil.Fensterhintergrund;
        Height = Stil.Px(this, 30);
        AccessibleRole = AccessibleRole.StatusBar;
        AccessibleName = Schluessel;
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
        if (statusText.Length > 0)
        {
            TextRenderer.DrawText(g, statusText, Stil.Klein, new Rectangle(x, 0, Width, Height), Stil.TextMittel, flags);
            x += TextRenderer.MeasureText(g, statusText, Stil.Klein, Size.Empty, TextFormatFlags.NoPadding).Width + Stil.Px(this, Stil.AbstandGross);
            using (var trenner = new Pen(Stil.Trennlinie)) g.DrawLine(trenner, x, Stil.Px(this, 7), x, Height - Stil.Px(this, 7));
            x += Stil.Px(this, Stil.AbstandGross);
        }
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
        // Streifen und Punkt in neutralem Grau (die echte Farbe ist die der Person), nur wenn Platz ist.
        int bezugBreite = Stil.Px(this, 20) + TextRenderer.MeasureText(g, BezugText, Stil.Klein, Size.Empty, TextFormatFlags.NoPadding).Width;
        if (x + bezugBreite > Width) return;
        using (var grau = new SolidBrush(Stil.TextLeise))
        {
            int hoehe = Stil.Px(this, 12);
            g.FillRectangle(grau, x, (Height - hoehe) / 2f, Stil.Px(this, 4), hoehe);
            g.FillEllipse(grau, x + Stil.Px(this, 7), (Height - Stil.Px(this, 8)) / 2f, Stil.Px(this, 8), Stil.Px(this, 8));
        }
        TextRenderer.DrawText(g, BezugText, Stil.Klein, new Rectangle(x + Stil.Px(this, 20), 0, Width, Height), Stil.TextMittel, flags);
    }
}

// Schutz vor dem zweiten Klick eines Doppelklicks (0.33.2): Baut sich eine Liste nach dem ersten Klick neu
// auf, liegt unter dem Mauszeiger schon der nächste Eintrag – der zweite Klick träfe ihn. Klicks innerhalb
// der Doppelklickzeit von Windows nach einem angenommenen Klick zählen deshalb nicht.
public sealed class Doppelklickschutz
{
    private long? letzter;
    public Func<long> Uhr { get; set; } = () => Environment.TickCount64;   // Millisekunden; im Test ersetzbar

    public bool Frei()
    {
        long jetzt = Uhr();
        if (letzter is long l && jetzt - l < SystemInformation.DoubleClickTime) return false;
        letzter = jetzt;
        return true;
    }
}

public static class Auswahl
{
    // Hält die Einträge einer Auswahl aktuell, z. B. nach dem Umbenennen oder Anlegen eines Bereichs (0.33.2:
    // vorher blieb die Liste vom Programmstart). Die Wahl bleibt, sonst der erste Eintrag. Wer auf
    // SelectedIndexChanged reagiert, sperrt das während des Abgleichs selbst. true = Einträge geändert.
    public static bool Abgleichen(ComboBox box, IReadOnlyList<string> eintraege)
    {
        if (box.Items.Cast<object>().Select(o => o.ToString()).SequenceEqual(eintraege)) return false;
        string? wahl = box.SelectedItem as string;
        box.BeginUpdate();
        box.Items.Clear();
        foreach (var e in eintraege) box.Items.Add(e);
        box.EndUpdate();
        int index = wahl is null ? -1 : eintraege.ToList().IndexOf(wahl);
        box.SelectedIndex = index >= 0 ? index : eintraege.Count > 0 ? 0 : -1;
        return true;
    }
}
