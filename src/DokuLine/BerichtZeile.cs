using System.Drawing.Drawing2D;
using DokuLine.Kern;

namespace DokuLine;

// Ein Bericht in der Dokumentation: Rand in der Farbe der Schicht, links Uhrzeit und Schichtwort, dann die Marken,
// der Text (kompakt höchstens zwei Zeilen, Klick klappt auf) und rechts das Kürzel; Nachträge eingerückt darunter.
public sealed class BerichtZeile : Control
{
    private const int ZeitBreite = 64, KuerzelBreite = 44, Innen = 8;
    private readonly bool kompakt;
    private readonly List<Label> nachtraege = [];
    private int textHoehe;

    public Bericht Bericht { get; }
    public RichTextBox TextFeld { get; } = new()
    {
        BorderStyle = BorderStyle.None, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.None, WordWrap = true,
        DetectUrls = false, TabStop = false, Font = Stil.Standard, Cursor = Cursors.Hand,
    };
    public Label Zeit { get; } = new() { AutoSize = true, Font = Stil.Fett, UseMnemonic = false };
    public Label SchichtWort { get; } = new() { AutoSize = true, Font = Stil.Klein, UseMnemonic = false };
    public Label Kuerzel { get; } = new()
    {
        AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel,
        UseMnemonic = false
    };
    public Label UebergabeZeichen { get; } = new()
    {
        AutoSize = true, Font = Stil.MarkeSchrift, Text = "Übergabe",
        Visible = false
    };
    public LinkLabel Mehr { get; } = new()
    {
        AutoSize = true, Text = "… mehr", LinkColor = Stil.Akzent,
        Visible = false
    };
    public List<MarkenFeld> Marken { get; } = [];
    public List<(int Start, int Laenge)> Fundstellen { get; }
    public bool Aufgeklappt { get; private set; }
    public bool Gekuerzt { get; private set; }
    public event Action<BerichtZeile>? Umgeschaltet;

    public BerichtZeile(Bericht b, string suche, bool kompakt)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw, true);
        Bericht = b;
        this.kompakt = kompakt;
        bool medikation = DokuRegeln.Marken(b).Contains(DokuMarke.MedikationGeaendert);
        Color flaeche = medikation ? Stil.MedikationZeile : Stil.Flaeche;
        BackColor = flaeche;
        Zeit.Text = b.Zeitpunkt.ToString("HH:mm");
        SchichtWort.Text = DokuRegeln.SchichtWort(b.Schicht);
        SchichtWort.ForeColor = Schichtfarbe(b.Schicht);
        Kuerzel.Text = b.Verfasser;
        UebergabeZeichen.Visible = b.Uebergabe;
        UebergabeZeichen.ForeColor = Stil.Hinweis.Text;
        foreach (var m in DokuRegeln.Marken(b))
            Marken.Add(new MarkenFeld(DokuRegeln.MarkeText(m), m switch
            {
                DokuMarke.Wichtig => Stil.Beschluss,
                DokuMarke.MedikationGeaendert => Stil.MedikationGeaendert,
                _ => Stil.Hinweis,
            }));
        TextFeld.BackColor = flaeche;
        TextFeld.Text = b.Text;
        Fundstellen = DokuRegeln.Fundstellen(b.Text, suche);
        foreach (var n in b.Nachtraege)
            nachtraege.Add(new Label
            {
                AutoSize = false, Font = Stil.Klein, ForeColor = Stil.TextMittel, UseMnemonic = false,
                Text = $"Nachtrag {BerichtRegeln.ZeitText(n.Erfasst)} · {n.Verfasser}: {n.Text}",
            });
        // Was aus dem Bericht entstand (Bauplan 48), z. B. „→ Termin 14.11. 10:30“.
        if (b.Verweis.Length > 0)
            nachtraege.Add(new Label { AutoSize = false, Font = Stil.Klein, ForeColor = Stil.Akzent, UseMnemonic = false, Text = b.Verweis });
        foreach (var c in new Control[] { Zeit, SchichtWort, Kuerzel, UebergabeZeichen, Mehr }) c.BackColor = flaeche;
        foreach (var l in nachtraege) l.BackColor = flaeche;
        Controls.AddRange([Zeit, SchichtWort, UebergabeZeichen, .. Marken, TextFeld, Mehr, Kuerzel, .. nachtraege]);
        Cursor = Cursors.Hand;
        Click += (_, _) => Umschalten();
        TextFeld.Click += (_, _) => Umschalten();
        Mehr.LinkClicked += (_, _) => Umschalten();
        TextFeld.HandleCreated += (_, _) => Formatieren();
        AccessibleName = $"Bericht {Zeit.Text} {SchichtWort.Text}, {Kuerzel.Text}";
    }

    public static Color Schichtfarbe(Schicht s) => s switch
    {
        Schicht.Frueh => Stil.SchichtFrueh,
        Schicht.Spaet => Stil.SchichtSpaet,
        _ => Stil.SchichtNacht,
    };

    public void Umschalten()
    {
        if (!kompakt || !Gekuerzt && !Aufgeklappt) return;
        Aufgeklappt = !Aufgeklappt;
        Umgeschaltet?.Invoke(this);
    }

    // Fetter Anfang (Visite) und Fundstellen der Suche hervorheben.
    private void Formatieren()
    {
        if (DokuRegeln.FettAnfang(Bericht) is string fett)
        {
            TextFeld.Select(0, fett.Length);
            TextFeld.SelectionFont = Stil.Fett;
        }
        foreach (var (start, laenge) in Fundstellen)
        {
            TextFeld.Select(start, laenge);
            TextFeld.SelectionBackColor = Stil.Achtung.Rand ?? Stil.AkzentOrange;
        }
        TextFeld.Select(0, 0);
    }

    // Höhe für eine Breite; legt die Teile dabei an ihren Platz.
    public int Anordnen(int breite)
    {
        int innen = Stil.Px(this, Innen), x = innen + Stil.Px(this, ZeitBreite);
        Zeit.Location = new Point(innen, innen);
        SchichtWort.Location = new Point(innen, Zeit.Bottom);
        int marken = x, y = innen;
        foreach (var m in Marken)
        {
            m.Location = new Point(marken, y);
            y = m.Bottom + Stil.Px(this, 2);
        }
        if (UebergabeZeichen.Visible) { UebergabeZeichen.Location = new Point(marken, y); y = UebergabeZeichen.Bottom; }
        int zeichen = UebergabeZeichen.Visible ? UebergabeZeichen.PreferredWidth : 0;
        int markenBreite = Marken.Count > 0 || UebergabeZeichen.Visible
            ? Marken.Select(m => m.Width).Append(zeichen).Max() + innen
            : 0;
        int textX = x + markenBreite, kuerzelX = breite - innen - Stil.Px(this, KuerzelBreite);
        int textBreite = Math.Max(Stil.Px(this, 120), kuerzelX - textX - innen);
        int zeile = TextRenderer.MeasureText("Ag", TextFeld.Font).Height;
        int voll = TextRenderer.MeasureText(Bericht.Text, TextFeld.Font, new Size(textBreite, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + 2;
        Gekuerzt = kompakt && voll > 2 * zeile + 2;
        textHoehe = Gekuerzt && !Aufgeklappt ? 2 * zeile : voll;
        TextFeld.SetBounds(textX, innen, textBreite, textHoehe);
        Mehr.Visible = Gekuerzt;
        Mehr.Text = Aufgeklappt ? "weniger" : "… mehr";
        int unten = TextFeld.Bottom;
        if (Mehr.Visible) { Mehr.Location = new Point(textX, unten); unten = Mehr.Bottom; }
        foreach (var n in nachtraege)
        {
            int nb = Math.Max(Stil.Px(this, 80), textBreite - Stil.Px(this, Stil.AbstandGross));
            n.Size = new Size(nb, TextRenderer.MeasureText(n.Text, n.Font, new Size(nb, int.MaxValue),
                TextFormatFlags.WordBreak).Height);
            n.Location = new Point(textX + Stil.Px(this, Stil.AbstandGross), unten + Stil.Px(this, 2));
            unten = n.Bottom;
        }
        Kuerzel.Location = new Point(kuerzelX, innen);
        return Math.Max(Math.Max(unten, y), SchichtWort.Bottom) + innen;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float stark = 1.5f * DeviceDpi / 96f, radius = Stil.Px(this, Stil.RadiusKarte);
        var rechteck = new RectangleF(stark / 2, stark / 2, Width - stark - 1, Height - stark - 1);
        using var pfad = new GraphicsPath();
        float d = radius * 2;
        pfad.AddArc(rechteck.Left, rechteck.Top, d, d, 180, 90);
        pfad.AddArc(rechteck.Right - d, rechteck.Top, d, d, 270, 90);
        pfad.AddArc(rechteck.Right - d, rechteck.Bottom - d, d, d, 0, 90);
        pfad.AddArc(rechteck.Left, rechteck.Bottom - d, d, d, 90, 90);
        pfad.CloseFigure();
        using var stift = new Pen(Schichtfarbe(Bericht.Schicht), stark);
        g.DrawPath(stift, pfad);
    }
}

// Überschrift eines Tages über die ganze Breite: „Montag, 09.11.2026 · 3 Berichte“.
public sealed class TagKopf : Control
{
    public DateOnly Datum { get; }

    public TagKopf(DateOnly tag, int anzahl)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw, true);
        Datum = tag;
        Text = DokuRegeln.TagText(tag, anzahl);
        Font = Stil.TagKopf;
        BackColor = Stil.Seite;
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = Text;
    }

    public int Hoehe => TextRenderer.MeasureText("Ag", Font).Height + Stil.Px(this, Stil.AbstandKlein);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float r = Stil.Px(this, 6) * 2f;
        using var pfad = new GraphicsPath();
        var rect = new RectangleF(0, 0, Width - 1, Height - 1);
        pfad.AddArc(rect.Left, rect.Top, r, r, 180, 90);
        pfad.AddArc(rect.Right - r, rect.Top, r, r, 270, 90);
        pfad.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        pfad.AddArc(rect.Left, rect.Bottom - r, r, r, 90, 90);
        pfad.CloseFigure();
        using var pinsel = new SolidBrush(Stil.TagKopfFlaeche);
        g.FillPath(pinsel, pfad);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(Stil.Px(this, Stil.AbstandKlein), 0, Width, Height),
            Stil.TextNormal,
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
