using System.Drawing.Drawing2D;

namespace Dokusystem;

// Der Kopf über jedem Bereich (übernommen und angepasst aus der Ausgangsliste): eine weiße,
// abgerundete Fläche mit feinem Rand und weichem Farbverlauf – Orange von oben links, Blau von
// rechts unten –, der nach rechts in reines Weiß ausläuft.
// Links: kleine Zeile und Titel. Rechts oben das Logofeld: ein selbst gewähltes Bild aus den
// Einstellungen (liegt im Datenordner, nie im Repository – Regel 7), sonst leer. Daneben der
// dauerhaft sichtbare Hinweis „Demo — ausgedachte Daten“ (Regel 4). Darunter rechts die Knöpfe.
public sealed class KopfBereich : Panel
{
    public const string DemoHinweis = Stil.DemoHinweis;

    // Logische Maße (100 % Skalierung).
    private const int Rand = 16, RandOben = 16, RandUnten = 8;   // Abstand zum Fensterrand
    private const int Innen = 16, InnenSeite = 24;
    private const int TitelHoehe = 64, Zeilenabstand = 8;
    private const int LogoHoehe = 56, LogoMaxBreite = 220;
    public const int GesamtHoehe = RandOben + Innen + TitelHoehe + Zeilenabstand + Stil.KnopfHoehe + Innen + RandUnten;

    private readonly Label klein = new();
    private readonly Label titel = new();
    private readonly List<Button> knoepfe = [];
    private Image? logo;

    // „Angemeldet als …“ unter dem Demo-Hinweis: Klick wechselt den Benutzer (ohne Passwort,
    // nur für die Vorführung – IDEEN „Nachrichten“).
    public Button BenutzerKnopf { get; } = new()
    {
        FlatStyle = FlatStyle.Flat, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Cursor = Cursors.Hand,
        BackColor = Stil.Flaeche, ForeColor = Stil.Akzent, Font = Stil.Standard, TextAlign = ContentAlignment.MiddleRight,
        UseMnemonic = false, TabStop = true,
    };

    public KopfBereich()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Stil.Seite;
        foreach (var label in new[] { klein, titel })
        {
            label.AutoSize = true;
            label.BackColor = Color.Transparent;
            label.UseMnemonic = false;
            Controls.Add(label);
        }
        klein.ForeColor = Stil.TextLeise;
        klein.Font = Stil.Standard;
        titel.ForeColor = Stil.TextNormal;
        titel.Font = Stil.KopfTitel;
        Height = S(GesamtHoehe);
        AccessibleName = DemoHinweis;
        BenutzerKnopf.FlatAppearance.BorderSize = 0;
        BenutzerKnopf.FlatAppearance.MouseOverBackColor = Stil.AkzentHell;
        BenutzerKnopf.FlatAppearance.MouseDownBackColor = Stil.AkzentHell;
        BenutzerKnopf.Padding = new Padding(Stil.AbstandMini, 0, Stil.AbstandMini, 0);
        Controls.Add(BenutzerKnopf);
        BenutzerSetzen(null);
    }

    public string BenutzerText => BenutzerKnopf.Text;

    // name == null: niemand angemeldet (Hinweis in der Farbe „bitte klären“).
    public void BenutzerSetzen(string? name)
    {
        BenutzerKnopf.Text = name is null ? "Nicht angemeldet – Anmelden ▾" : $"Angemeldet als {name} ▾";
        BenutzerKnopf.ForeColor = name is null ? Stil.Achtung.Text : Stil.Akzent;
        BenutzerKnopf.AccessibleName = (name is null ? "Nicht angemeldet. Anmelden" : $"Angemeldet als {name}. Abmelden / Benutzer wechseln");
        PerformLayout();
    }

    // Rechter Rand für Demo-Hinweis und Benutzer: links neben dem Logo, falls eines gewählt ist.
    private int RechtsNebenLogo()
    {
        var f = Flaeche;
        int rechts = f.Right - S(InnenSeite);
        if (logo is null) return rechts;
        var feld = new Rectangle(rechts - S(LogoMaxBreite), f.Top + S(Innen), S(LogoMaxBreite), S(LogoHoehe));
        return (int)(rechts - Einpassen(logo.Size, feld).Width) - S(Stil.AbstandMittel);
    }

    public string KleinText => klein.Text;
    public string TitelText => titel.Text;

    private int S(int wert) => Stil.Px(this, wert);
    private float Skala => Stil.Skala(this);

    public void TexteSetzen(string kleinText, string titelText)
    {
        klein.Text = kleinText;
        titel.Text = titelText;
        PerformLayout();
        Invalidate();
    }

    // Die Knöpfe rechts unten, von links nach rechts (die Hauptaktion zuletzt, ganz rechts).
    public void KnoepfeSetzen(params Button[] neue)
    {
        foreach (var alt in knoepfe) Controls.Remove(alt);
        knoepfe.Clear();
        knoepfe.AddRange(neue);
        foreach (var knopf in knoepfe) Controls.Add(knopf);
        PerformLayout();
    }

    public IReadOnlyList<Button> Knoepfe => knoepfe;

    // Eigenes Bild für das Logofeld (null = leer).
    public void LogoSetzen(string? pfad)
    {
        logo?.Dispose();
        logo = Bewohnerbild.Laden(pfad);
        PerformLayout();
        Invalidate();
    }

    public bool HatLogo => logo is not null;

    // Bild im Seitenverhältnis in ein Feld einpassen (mittig).
    public static RectangleF Einpassen(Size bild, Rectangle feld)
    {
        float faktor = Math.Min((float)feld.Width / Math.Max(1, bild.Width), (float)feld.Height / Math.Max(1, bild.Height));
        float w = bild.Width * faktor, h = bild.Height * faktor;
        return new RectangleF(feld.X + (feld.Width - w) / 2, feld.Y + (feld.Height - h) / 2, w, h);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) logo?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Height = S(GesamtHoehe);
    }

    private Rectangle Flaeche => new(S(Rand), S(RandOben), Width - 2 * S(Rand), Height - S(RandOben) - S(RandUnten));

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var f = Flaeche;
        int links = f.Left + S(InnenSeite), rechts = f.Right - S(InnenSeite), oben = f.Top + S(Innen);
        int textHoehe = klein.PreferredHeight + S(Stil.AbstandMini) + titel.PreferredHeight;
        int textOben = oben + Math.Max(0, (S(TitelHoehe) - textHoehe) / 2);
        klein.Location = new Point(links, textOben);
        titel.Location = new Point(links - S(2), textOben + klein.PreferredHeight + S(Stil.AbstandMini));

        // Benutzer unter dem Demo-Hinweis, rechtsbündig mit ihm.
        var benutzer = BenutzerKnopf.PreferredSize;
        BenutzerKnopf.Bounds = new Rectangle(RechtsNebenLogo() - benutzer.Width + S(Stil.AbstandMini), oben + S(26), benutzer.Width, benutzer.Height);

        int y = oben + S(TitelHoehe) + S(Zeilenabstand);
        int x = rechts;
        for (int i = knoepfe.Count - 1; i >= 0; i--)
        {
            var knopf = knoepfe[i];
            int breite = Math.Max(knopf.PreferredSize.Width, TextRenderer.MeasureText(knopf.Text, knopf.Font).Width + S(32));
            x -= breite;
            knopf.Bounds = new Rectangle(x, y, breite, S(Stil.KnopfHoehe));
            x -= S(Stil.AbstandKlein);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Stil.Seite);
        RectangleF f = Flaeche;
        float radius = Stil.RadiusKarte * Skala;
        Stil.HauchSchatten(g, f, radius, Skala);

        using var pfad = Stil.Abgerundet(f, radius);
        using (var grund = new SolidBrush(Stil.Flaeche)) g.FillPath(grund, pfad);

        // Verlauf nur innerhalb der abgerundeten Fläche.
        var vorher = g.Clip;
        g.SetClip(pfad);
        FleckMalen(g, new PointF(f.Left, f.Top), f.Width * 0.56f, f.Height * 1.75f,
            Stil.VerlaufOrange, Stil.VerlaufOrange, (int)(0.42 * 255), (int)(0.15 * 255), 0.55f, 0.20f);
        FleckMalen(g, new PointF(f.Left + f.Width * 0.80f, f.Bottom), f.Width * 0.62f, f.Height * 1.9f,
            Stil.VerlaufBlau, Color.FromArgb(140, 174, 224), (int)(0.40 * 255), (int)(0.18 * 255), 0.50f, 0.15f);
        // Rechts in reine Fläche auslaufen (dort steht der Demo-Hinweis).
        // TileFlipX: Am letzten Pixel springt der Verlauf sonst auf „durchsichtig“ zurück – ein feiner
        // senkrechter Strich. Die volle Fläche beginnt deshalb auch einen Pixel früher.
        float von = f.Left + f.Width * 0.70f, bis = f.Left + f.Width * 0.84f;
        using (var auslauf = new LinearGradientBrush(new PointF(von, 0), new PointF(bis, 0),
                   Color.FromArgb(0, Stil.Flaeche), Stil.Flaeche) { WrapMode = WrapMode.TileFlipX })
            g.FillRectangle(auslauf, von, f.Top, bis - von, f.Height);
        using (var voll = new SolidBrush(Stil.Flaeche))
            g.FillRectangle(voll, bis - 1, f.Top, f.Right - bis + 1, f.Height);
        g.Clip = vorher;

        using (var rand = new Pen(Stil.KarteRand, 1f)) g.DrawPath(rand, pfad);

        // Logofeld oben rechts, im Seitenverhältnis, rechtsbündig.
        int rechts = (int)f.Right - S(InnenSeite);
        if (logo is not null)
        {
            var feld = new Rectangle(rechts - S(LogoMaxBreite), (int)f.Top + S(Innen), S(LogoMaxBreite), S(LogoHoehe));
            var ziel = Einpassen(logo.Size, feld);
            ziel.X = rechts - ziel.Width;
            ziel.Y = feld.Y;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(logo, ziel);
            rechts = (int)ziel.X - S(Stil.AbstandMittel);
        }

        // Demo-Hinweis (Regel 4), als Marke „Achtung“ – mit Wort, nicht nur Farbe.
        int breite = Stil.MarkenBreite(g, DemoHinweis, Skala);
        Stil.MarkeZeichnen(g, DemoHinweis, rechts - breite, (int)f.Top + S(Innen), Stil.Achtung, Skala);
    }

    // Ein weicher Farbfleck: innen kräftig, außen durchsichtig – wie ein Aquarell-Klecks.
    private static void FleckMalen(Graphics g, PointF mitte, float rx, float ry,
        Color innen, Color zwischen, int alphaInnen, int alphaZwischen, float mitteBei, float aussenBei)
    {
        if (rx < 1 || ry < 1) return;
        using var ellipse = new GraphicsPath();
        ellipse.AddEllipse(mitte.X - rx, mitte.Y - ry, rx * 2, ry * 2);
        using var pinsel = new PathGradientBrush(ellipse)
        {
            CenterPoint = mitte,
            InterpolationColors = new ColorBlend
            {
                Colors = [Color.FromArgb(0, zwischen), Color.FromArgb(0, zwischen), Color.FromArgb(alphaZwischen, zwischen), Color.FromArgb(alphaInnen, innen)],
                Positions = [0f, aussenBei, mitteBei, 1f],
            },
        };
        g.FillPath(pinsel, ellipse);
    }
}
