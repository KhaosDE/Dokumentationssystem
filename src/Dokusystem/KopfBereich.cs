using System.Drawing.Drawing2D;

namespace Dokusystem;

// Der Kopf über jedem Bereich (übernommen und angepasst aus der Ausgangsliste): eine weiße,
// abgerundete Fläche mit feinem Rand und weichem Farbverlauf – Orange von oben links, Blau von
// rechts unten –, der nach rechts in reines Weiß ausläuft.
// Links: kleine Zeile und Titel. Rechts oben der dauerhaft sichtbare Hinweis „Demo — ausgedachte
// Daten“ (Regel 4); ein Bild der Einrichtung gibt es hier nicht (Regel 7). Darunter rechts die Knöpfe.
public sealed class KopfBereich : Panel
{
    public const string DemoHinweis = "Demo — ausgedachte Daten";

    // Logische Maße (100 % Skalierung).
    private const int Rand = 16, RandOben = 16, RandUnten = 8;   // Abstand zum Fensterrand
    private const int Innen = 16, InnenSeite = 24;
    private const int TitelHoehe = 64, Zeilenabstand = 8;
    public const int GesamtHoehe = RandOben + Innen + TitelHoehe + Zeilenabstand + Stil.KnopfHoehe + Innen + RandUnten;

    private readonly Label klein = new();
    private readonly Label titel = new();
    private readonly List<Button> knoepfe = [];

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
        float von = f.Left + f.Width * 0.70f, bis = f.Left + f.Width * 0.84f;
        using (var auslauf = new LinearGradientBrush(new PointF(von - 1, 0), new PointF(bis + 1, 0),
                   Color.FromArgb(0, Stil.Flaeche), Stil.Flaeche))
            g.FillRectangle(auslauf, von, f.Top, bis - von, f.Height);
        using (var voll = new SolidBrush(Stil.Flaeche))
            g.FillRectangle(voll, bis, f.Top, f.Right - bis, f.Height);
        g.Clip = vorher;

        using (var rand = new Pen(Stil.KarteRand, 1f)) g.DrawPath(rand, pfad);

        // Demo-Hinweis oben rechts (Regel 4), als Marke „Achtung“ – mit Wort, nicht nur Farbe.
        int breite = Stil.MarkenBreite(g, DemoHinweis, Skala);
        Stil.MarkeZeichnen(g, DemoHinweis, (int)f.Right - S(InnenSeite) - breite, (int)f.Top + S(Innen), Stil.Achtung, Skala);
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
