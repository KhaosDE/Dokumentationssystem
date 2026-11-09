using System.Drawing.Drawing2D;

namespace DokuLine;

// Der Kopf über jedem Bereich (übernommen und angepasst aus der Ausgangsliste): eine weiße,
// abgerundete Fläche mit feinem Rand und weichem Farbverlauf – Orange von oben links, Blau von
// rechts unten –, der nach rechts in reines Weiß ausläuft.
// Links: kleine Zeile und Titel. Rechts oben das Logofeld: ein selbst gewähltes Bild aus den
// Einstellungen (liegt im Datenordner, nie im Repository – Regel 7), sonst ein Platzhalter
// „Logo der Einrichtung“ (Klick öffnet die Einstellungen). Daneben der
// dauerhaft sichtbare Hinweis „Demo — ausgedachte Daten“ (Regel 4). Darunter rechts die Knöpfe.
public sealed class KopfBereich : Panel
{
    public const string DemoHinweis = Stil.DemoHinweis;

    // Logische Maße (100 % Skalierung).
    private const int Rand = 16, RandOben = 16, RandUnten = 8;   // Abstand zum Fensterrand
    private const int Innen = 16, InnenSeite = 24;
    private const int TitelHoehe = 64;
    private const int LogoHoehe = 56, LogoMaxBreite = 220, PlatzhalterBreite = 150;
    // Schritt 26i: ohne die Knopfzeile – die Knöpfe stehen in der Aktionsleiste darunter.
    public const int GesamtHoehe = RandOben + Innen + TitelHoehe + Innen + RandUnten;

    private readonly Label klein = new();
    private readonly Label titel = new();
    private readonly List<Button> knoepfe = [];
    private Image? logo;
    private static readonly ToolTip Hinweis = new();

    // Klick auf den Platzhalter (kein Logo gewählt): das Hauptfenster öffnet die Einstellungen.
    public event Action? LogoGewuenscht;
    public const string PlatzhalterText = "Logo der Einrichtung";

    // Schritt 26a3: unter dem Demo-Hinweis „Angemeldet: TST – Name“ und daneben ein eigener Knopf
    // „Abmelden (F12)“ (ohne Anmeldung: „Anmelden …“) – kein verstecktes Klappmenü. Hat die angemeldete
    // Person Entwürfe, steht davor „Entwurf fortsetzen“.
    public Label BenutzerAnzeige { get; } = new() { AutoSize = true, BackColor = Color.Transparent, Font = Stil.Standard, UseMnemonic = false };
    public Button AbmeldenKnopf { get; } = Stil.NebenKnopf("Abmelden (F12)");
    public Button EntwurfKnopf { get; } = Stil.NebenKnopf("Entwurf fortsetzen");
    private const int BenutzerHoehe = 30;
    // Schritt 26i: Die Aktionen des Bereichs stehen in einer Leiste direkt unter der Kopfzeile (das
    // Hauptfenster legt sie dorthin); „Entwurf fortsetzen“ steht dort rechts außen.
    public AktionsLeiste Leiste { get; } = new();

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
        EntwurfKnopf.Visible = false;
        Controls.AddRange([BenutzerAnzeige, AbmeldenKnopf]);
        Leiste.EntwurfSetzen(EntwurfKnopf);
        BenutzerSetzen(null, null);
    }

    public string BenutzerText => BenutzerAnzeige.Text;

    // kuerzel == null: niemand angemeldet (Hinweis in der Farbe „bitte klären“).
    public void BenutzerSetzen(string? kuerzel, string? name)
    {
        BenutzerAnzeige.Text = kuerzel is null ? "Nicht angemeldet" : $"Angemeldet: {kuerzel}" + (name is null ? "" : $" – {name}");
        BenutzerAnzeige.ForeColor = kuerzel is null ? Stil.Achtung.Text : Stil.TextNormal;
        AbmeldenKnopf.Text = kuerzel is null ? "Anmelden …" : "Abmelden (F12)";
        AbmeldenKnopf.AccessibleName = AbmeldenKnopf.Text;
        Hinweis.SetToolTip(AbmeldenKnopf, kuerzel is null
            ? "Mit dem Kürzel anmelden."
            : "Abmelden (Taste F12, aus jedem Fenster). Danach ist nichts mehr zu sehen, bis sich jemand anmeldet.");
        PerformLayout();
    }

    public void EntwuerfeSetzen(int anzahl)
    {
        EntwurfKnopf.Text = anzahl == 1 ? "Entwurf fortsetzen" : $"Entwürfe fortsetzen ({anzahl})";
        Leiste.EntwurfZeigen(anzahl > 0);
    }

    // Rechter Rand für Demo-Hinweis und Benutzer: links neben dem Logo, falls eines gewählt ist.
    private int RechtsNebenLogo()
    {
        var f = Flaeche;
        int rechts = f.Right - S(InnenSeite);
        if (logo is null) return rechts - S(PlatzhalterBreite) - S(Stil.AbstandMittel);
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

    // Die Aktionen des Bereichs (Schritt 26i: in der Aktionsleiste unter der Kopfzeile). Knoepfe behält
    // die übergebene Reihenfolge; wie sie in der Leiste stehen, entscheidet AktionsLeiste.Reihenfolge.
    public void KnoepfeSetzen(params Button[] neue)
    {
        knoepfe.Clear();
        knoepfe.AddRange(neue);
        Leiste.KnoepfeSetzen(neue);
    }

    public IReadOnlyList<Button> Knoepfe => knoepfe;

    // Ein Bedienelement rechts in der Aktionsleiste, z. B. die Bereichswahl der Übersicht (Schritt 26l).
    // Nach KnoepfeSetzen aufrufen; der nächste KnoepfeSetzen-Aufruf entfernt es wieder.
    public void ZusatzSetzen(Control? zusatz) => Leiste.ZusatzSetzen(zusatz);

    // Eigenes Bild für das Logofeld (null = leer).
    public void LogoSetzen(string? pfad)
    {
        logo?.Dispose();
        logo = Bewohnerbild.Laden(pfad);
        PerformLayout();
        Invalidate();
    }

    public bool HatLogo => logo is not null;

    // Wo der Platzhalter steht (leer, wenn ein Logo gewählt ist).
    public Rectangle PlatzhalterFeld
    {
        get
        {
            if (logo is not null) return Rectangle.Empty;
            var f = Flaeche;
            int rechts = f.Right - S(InnenSeite);
            return new Rectangle(rechts - S(PlatzhalterBreite), f.Top + S(Innen), S(PlatzhalterBreite), S(LogoHoehe));
        }
    }

    public void PlatzhalterKlicken() { if (logo is null) LogoGewuenscht?.Invoke(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool drauf = PlatzhalterFeld.Contains(e.Location);
        Cursor = drauf ? Cursors.Hand : Cursors.Default;
        string? text = drauf ? "Eigenes Logo wählen: Einstellungen → „Bild im Kopf“" : null;
        if (Hinweis.GetToolTip(this) != (text ?? "")) Hinweis.SetToolTip(this, text);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && PlatzhalterFeld.Contains(e.Location)) PlatzhalterKlicken();
    }

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
        int links = f.Left + S(InnenSeite), oben = f.Top + S(Innen);
        int textHoehe = klein.PreferredHeight + S(Stil.AbstandMini) + titel.PreferredHeight;
        int textOben = oben + Math.Max(0, (S(TitelHoehe) - textHoehe) / 2);
        klein.Location = new Point(links, textOben);
        titel.Location = new Point(links - S(2), textOben + klein.PreferredHeight + S(Stil.AbstandMini));

        // Benutzer unter dem Demo-Hinweis, rechtsbündig mit ihm: [Entwurf fortsetzen] Angemeldet: … [Abmelden (F12)]
        int zeile = oben + S(26), hoehe = S(BenutzerHoehe);
        int b = Math.Max(AbmeldenKnopf.PreferredSize.Width, TextRenderer.MeasureText(AbmeldenKnopf.Text, AbmeldenKnopf.Font).Width + S(24));
        AbmeldenKnopf.Bounds = new Rectangle(RechtsNebenLogo() - b, zeile, b, hoehe);
        BenutzerAnzeige.Location = new Point(AbmeldenKnopf.Left - S(Stil.AbstandKlein) - BenutzerAnzeige.PreferredWidth, zeile + (hoehe - BenutzerAnzeige.PreferredHeight) / 2);
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
        else
        {
            // Platzhalter: gestrichelter Rahmen mit Wort – das echte Logo kommt nie ins Repository (Regel 7).
            var feld = PlatzhalterFeld;
            using (var flaeche = Stil.Abgerundet(feld, Stil.RadiusKnopf * Skala))
            using (var grund = new SolidBrush(Stil.Fensterhintergrund))
            using (var strich = new Pen(Stil.KarteRand, 1.2f * Skala) { DashStyle = DashStyle.Dash })
            {
                g.FillPath(grund, flaeche);
                g.DrawPath(strich, flaeche);
            }
            TextRenderer.DrawText(g, $"{PlatzhalterText}\n(Einstellungen)", Stil.Klein, feld, Stil.TextLeise,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            rechts = feld.Left - S(Stil.AbstandMittel);
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

// Aktionsleiste direkt unter der Kopfzeile: gleiche Breite und gleiche Seitenränder
// wie die Kopfzeile, Knöpfe im bestehenden Stil. In allen Bereichen gleich angeordnet: links ein
// Rücksprung („← Dokumentation“), dann die Hauptaktion (farbig), dann die übrigen Aktionen; rechts
// „… drucken …“ und ganz außen „Entwurf fortsetzen“. Ohne Aktion entfällt die Leiste.
public sealed class AktionsLeiste : Panel
{
    private const int Rand = 16, RandUnten = 8;   // wie KopfBereich
    private readonly List<Button> knoepfe = [];
    private Button? entwurf;
    public Control? Zusatz { get; private set; }

    public AktionsLeiste()
    {
        Dock = DockStyle.Top;
        BackColor = Stil.Seite;
        Visible = false;
        Height = S(Stil.KnopfHoehe + RandUnten);
    }

    private int S(int wert) => Stil.Px(this, wert);

    internal void EntwurfSetzen(Button knopf)
    {
        entwurf = knopf;
        Controls.Add(knopf);
    }

    public void KnoepfeSetzen(IEnumerable<Button> neue)
    {
        foreach (var alt in knoepfe) Controls.Remove(alt);
        knoepfe.Clear();
        if (Zusatz is not null)
        {
            if (zusatzBreite is int breite) Zusatz.Width = breite;
            Zusatz.Visible = true;
            Controls.Remove(Zusatz);
        }
        Zusatz = null;
        zusatzBreite = null;
        knoepfe.AddRange(neue);
        foreach (var knopf in knoepfe) Controls.Add(knopf);
        Anordnen();
    }

    private int? zusatzBreite;   // Breite, wie der Zusatz gesetzt wurde (vor dem Schmälern)
    public const int ZusatzMindestens = 90;

    public void ZusatzSetzen(Control? zusatz)
    {
        if (Zusatz is not null)
        {
            if (zusatzBreite is int alt) Zusatz.Width = alt;   // für den nächsten Gebrauch wie vorher
            Zusatz.Visible = true;
        }
        zusatzBreite = null;
        if (Zusatz is not null) Controls.Remove(Zusatz);
        Zusatz = zusatz;
        if (zusatz is not null) Controls.Add(zusatz);
        Anordnen();
    }

    // Ohne Recht ausgeblendet: nimmt keinen Platz (Visible meldet false, solange die Leiste verborgen ist).
    private static bool Sichtbar(Button k) => k is not Knopf { OhneRecht: true };

    // Gruppe in der Leiste: 0 Rücksprung, 1 Hauptaktion, 2 übrige Aktionen, 3 rechts (Drucken).
    public static int Gruppe(Button knopf) =>
        knopf.Text.StartsWith('←') ? 0
        : knopf.Text.Contains("drucken", StringComparison.OrdinalIgnoreCase) ? 3
        : knopf is Knopf { Hauptaktion: true } ? 1
        : 2;

    // Links, wie sie stehen (stabil innerhalb der Gruppe), und rechts.
    public static (List<Button> Links, List<Button> Rechts) Reihenfolge(IEnumerable<Button> knoepfe)
    {
        var sortiert = knoepfe.Select((k, i) => (k, i)).OrderBy(x => Gruppe(x.k)).ThenBy(x => x.i).Select(x => x.k).ToList();
        return (sortiert.Where(k => Gruppe(k) < 3).ToList(), sortiert.Where(k => Gruppe(k) == 3).ToList());
    }

    // „Entwurf fortsetzen“ zeigen oder nicht. (Visible eines Knopfs gilt erst, wenn die Leiste zu sehen
    // ist; deshalb merkt sich die Leiste den Wunsch selbst.)
    public void EntwurfZeigen(bool zeigen)
    {
        entwurfSichtbar = zeigen;
        if (entwurf is not null) entwurf.Visible = zeigen;
        Anordnen();
    }

    private bool entwurfSichtbar;

    public void Anordnen()
    {
        Visible = knoepfe.Any(Sichtbar) || entwurfSichtbar || Zusatz is not null;
        Height = S(Stil.KnopfHoehe + RandUnten);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int hoehe = S(Stil.KnopfHoehe);
        int Breite(Button k) => Math.Max(k.PreferredSize.Width, TextRenderer.MeasureText(k.Text, k.Font).Width + S(32));
        var (links, rechts) = Reihenfolge(knoepfe.Where(Sichtbar));
        int x = S(Rand);
        foreach (var k in links)
        {
            int b = Breite(k);
            k.Bounds = new Rectangle(x, 0, b, hoehe);
            x += b + S(Stil.AbstandKlein);
        }
        int r = Width - S(Rand);
        if (entwurf is not null && entwurfSichtbar)
        {
            int b = Breite(entwurf);
            r -= b;
            entwurf.Bounds = new Rectangle(r, 0, b, hoehe);
            r -= S(Stil.AbstandMittel);
        }
        for (int i = rechts.Count - 1; i >= 0; i--)
        {
            int b = Breite(rechts[i]);
            r -= b;
            rechts[i].Bounds = new Rectangle(r, 0, b, hoehe);
            r -= S(Stil.AbstandKlein);
        }
        if (Zusatz is not null)
        {
            // Reicht der Platz nicht, wird die Auswahl schmaler, notfalls ausgeblendet – sie verdeckt nie „Drucken …“.
            r -= S(Stil.AbstandKlein);
            zusatzBreite ??= Zusatz.Width;
            int platz = r - x;
            Zusatz.Width = Math.Min(zusatzBreite.Value, Math.Max(0, platz));
            Zusatz.Visible = platz >= S(ZusatzMindestens);
            Zusatz.Location = new Point(r - Zusatz.Width, Math.Max(0, (hoehe - Zusatz.Height) / 2));
        }
    }
}

