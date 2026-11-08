using System.Drawing.Drawing2D;

namespace Dokusystem;

// Zentrale Gestaltungswerte: ein Wert, ein Ort. Übernommen und angepasst aus Stil.cs der
// Ausgangsliste (helle Darstellung „A3 Markenfarben“), ergänzt um die Verfeinerungen aus
// docs/gestaltung.md. Zunächst nur die helle Darstellung.
//
// Grundsätze (docs/gestaltung.md): Farbe sparsam – Farbe nie allein, immer mit Wort –
// Abstände 4/8/12/16 – Knopfhöhe 36 – Segoe UI – Radien 4 (Knöpfe, Eingaben) und 8 (Karten).
public static class Stil
{
    // Dauerhaft sichtbarer Hinweis (Regel 4 in CLAUDE.md): im Kopf, in Fenstertiteln und auf Ausdrucken.
    public const string DemoHinweis = "Demo — ausgedachte Daten";

    // ----- Flächen und Text -----
    public static readonly Color Seite = Color.FromArgb(238, 241, 247);              // hinter den Karten
    public static readonly Color Fensterhintergrund = Color.FromArgb(243, 243, 243);
    public static readonly Color Flaeche = Color.White;                              // Karten, Felder
    public static readonly Color KarteRand = Color.FromArgb(217, 224, 236);
    public static readonly Color TextNormal = Color.FromArgb(23, 32, 51);
    public static readonly Color TextMittel = Color.FromArgb(59, 70, 96);
    public static readonly Color TextLeise = Color.FromArgb(91, 100, 120);
    public static readonly Color Trennlinie = Color.FromArgb(229, 229, 229);
    public static readonly Color Schatten = Color.FromArgb(16, 32, 64);

    // ----- Akzente -----
    public static readonly Color Akzent = Color.FromArgb(45, 84, 156);             // Hauptaktion, Text auf Hell
    public static readonly Color AkzentHell = Color.FromArgb(222, 232, 245);       // markierte Zeile
    public static readonly Color AkzentOrange = Color.FromArgb(238, 166, 60);
    public static readonly Color VerlaufOrange = Color.FromArgb(238, 166, 60);     // Kopf
    public static readonly Color VerlaufBlau = Color.FromArgb(84, 128, 198);       // Kopf
    public static readonly Color Erfolg = Color.FromArgb(16, 124, 16);
    public static readonly Color Gefahr = Color.FromArgb(196, 43, 28);
    public static readonly Color Fokus = Color.FromArgb(47, 91, 211);              // „Zahl“: Fokus- und Hover-Rand

    // ----- Marken (Fläche / Text) -----
    public readonly record struct Marke(Color Flaeche, Color Text, Color? Rand = null);

    public static readonly Marke Beschluss = new(Color.FromArgb(253, 231, 228), Color.FromArgb(163, 34, 26));
    public static readonly Marke Hinweis = new(Color.FromArgb(240, 234, 251), Color.FromArgb(91, 58, 153));
    public static readonly Marke Zimmer = new(Color.FromArgb(238, 242, 251), Color.FromArgb(42, 63, 107));
    public static readonly Marke Achtung = new(Color.FromArgb(255, 248, 230), Color.FromArgb(122, 74, 0), Color.FromArgb(243, 213, 140));
    public static readonly Marke Offen = new(Color.FromArgb(227, 244, 230), Color.FromArgb(30, 107, 46));
    public static readonly Marke Unterwegs = new(Color.FromArgb(221, 244, 241), Color.FromArgb(14, 102, 94));   // hier: Übergabe
    public static readonly Marke Abwesend = new(Color.FromArgb(236, 237, 240), Color.FromArgb(70, 76, 90), Color.FromArgb(200, 204, 212));

    // ----- Abstandsraster: jeder Abstand ist eines dieser Maße -----
    public const int AbstandMini = 4;     // innerhalb zusammengehöriger Texte
    public const int AbstandKlein = 8;    // zwischen Knöpfen, Feld und Beschriftung
    public const int AbstandMittel = 12;  // zwischen Inhaltsblöcken
    public const int AbstandGross = 16;   // Rand von Fenstern und Flächen
    public const int KnopfHoehe = 36;
    public const int RadiusKnopf = 4;
    public const int RadiusKarte = 8;

    // ----- Schriften (Segoe UI ist die Systemschrift von Windows) -----
    public static readonly Font KopfTitel = new("Segoe UI Semibold", 22F);
    public static readonly Font Ueberschrift = new("Segoe UI Semibold", 18F);
    public static readonly Font KarteName = new("Segoe UI Semibold", 12.75F);
    public static readonly Font Standard = new("Segoe UI", 10F);
    public static readonly Font Fett = new("Segoe UI Semibold", 10F);
    public static readonly Font FettKursiv = new("Segoe UI Semibold", 10F, FontStyle.Italic);   // „nicht erfasst“
    public static readonly Font Klein = new("Segoe UI", 9F);
    public static readonly Font MarkeSchrift = new("Segoe UI Semibold", 9F);
    public static readonly Font Abschnitt = new("Segoe UI Semibold", 8.5F);   // „STAMMDATEN“

    // Logische Pixel (bei 100 %) in Bildschirmpixel umrechnen (125 %, 150 % …).
    public static int Px(Control bezug, int wert) => (int)Math.Round(wert * bezug.DeviceDpi / 96f);
    public static float Skala(Control bezug) => bezug.DeviceDpi / 96f;

    // Ein Rechteck mit abgerundeten Ecken als Zeichenpfad.
    public static GraphicsPath Abgerundet(RectangleF r, float radius)
    {
        var pfad = new GraphicsPath();
        float d = Math.Max(1f, radius * 2);
        pfad.AddArc(r.Left, r.Top, d, d, 180, 90);
        pfad.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        pfad.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        pfad.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        pfad.CloseFigure();
        return pfad;
    }

    // Haarlinien statt Schatten: höchstens ein Hauch (2 px nach unten, etwa 4 % Deckkraft).
    public static void HauchSchatten(Graphics g, RectangleF flaeche, float radius, float skala)
    {
        var r = flaeche;
        r.Offset(0, 2 * skala);
        using var pinsel = new SolidBrush(Color.FromArgb(10, Schatten));
        using var pfad = Abgerundet(r, radius);
        g.FillPath(pinsel, pfad);
    }

    // Weicher Schatten unter einer Karte (aus der Ausgangsliste, Gestaltung Stufe 2): mehrere
    // Lagen, nach außen immer durchsichtiger – die Karte wirkt leicht angehoben.
    public static void SchattenZeichnen(Graphics g, RectangleF flaeche, float radius, float skala)
    {
        const int lagen = 6, grundAlpha = 12;
        float versatz = 2f * skala;
        for (int i = lagen; i >= 1; i--)
        {
            float w = i * skala;
            var r = RectangleF.Inflate(flaeche, w * 0.6f, w * 0.6f);
            r.Offset(0, versatz + w * 0.35f);
            int alpha = Math.Max(1, grundAlpha * (lagen - i + 1) / lagen / 2);
            using var pinsel = new SolidBrush(Color.FromArgb(alpha, Schatten));
            using var pfad = Abgerundet(r, radius + w * 0.6f);
            g.FillPath(pinsel, pfad);
        }
    }

    // Farbe der Marken auf den Karten. Farbe nie allein: Jede Marke trägt ein Wort, und die
    // Statusleiste erklärt die Farben.
    public static Marke MarkeFuer(Kern.MarkenArt art) => art switch
    {
        Kern.MarkenArt.Beschluss => Beschluss,
        Kern.MarkenArt.Allergie => Beschluss,
        Kern.MarkenArt.Uebergabe => Unterwegs,
        Kern.MarkenArt.Wichtig => Hinweis,
        Kern.MarkenArt.Abwesend => Abwesend,
        Kern.MarkenArt.Ausgang => Zimmer,
        Kern.MarkenArt.Termin => Zimmer,
        Kern.MarkenArt.Angabe => Zimmer,
        _ => Achtung,
    };

    // Der Farbschlüssel der Statusleiste: Farbe und was sie bedeutet.
    public static readonly (Marke Marke, string Text)[] Farbschluessel =
    [
        (Beschluss, "Beschluss, Allergie"),
        (Achtung, "nicht erfasst, Frist"),
        (Unterwegs, "Übergabe"),
        (Hinweis, "wichtig"),
        (Abwesend, "abwesend"),
        (Zimmer, "Zimmer, Lebensbereich"),
    ];

    // Weiße Fläche mit 1-px-Rand und abgerundeten Ecken (Karten, Kopf, Hinweise).
    public static void FlaecheZeichnen(Graphics g, RectangleF flaeche, float radius, Color grund, Color rand,
        float randDicke = 1f, bool gestrichelt = false)
    {
        using var pfad = Abgerundet(flaeche, radius);
        using (var pinsel = new SolidBrush(grund)) g.FillPath(pinsel, pfad);
        using var stift = new Pen(rand, randDicke);
        if (gestrichelt) stift.DashStyle = DashStyle.Dash;
        g.DrawPath(stift, pfad);
    }

    // Mischt zwei Farben; anteil = wie viel von „zu“ hinein soll (0 bis 1).
    public static Color Mischen(Color von, Color zu, double anteil) => Color.FromArgb(
        (int)(von.R + (zu.R - von.R) * anteil),
        (int)(von.G + (zu.G - von.G) * anteil),
        (int)(von.B + (zu.B - von.B) * anteil));

    // Eine kleine Marke mit abgerundeter Fläche; gibt ihre Breite zurück.
    public static int MarkeZeichnen(Graphics g, string text, int x, int y, Marke marke, float skala)
    {
        int breite = MarkenBreite(g, text, skala);
        var feld = new Rectangle(x, y, breite, (int)Math.Round(22 * skala));
        FlaecheZeichnen(g, feld, RadiusKnopf * skala, marke.Flaeche, marke.Rand ?? marke.Flaeche);
        TextRenderer.DrawText(g, text, MarkeSchrift, feld, marke.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        return breite;
    }

    public static int MarkenBreite(Graphics g, string text, float skala) =>
        TextRenderer.MeasureText(g, text, MarkeSchrift, Size.Empty, TextFormatFlags.NoPadding).Width + (int)Math.Round(16 * skala);

    // ----- Knöpfe -----

    // Die eine Hauptaktion einer Ansicht: Akzentfarbe (docs/gestaltung.md).
    public static Button HauptKnopf(string text)
    {
        var knopf = Grundknopf(text);
        knopf.BackColor = Akzent;
        knopf.ForeColor = Color.White;
        knopf.Font = Fett;
        knopf.FlatAppearance.BorderColor = Akzent;
        knopf.FlatAppearance.MouseOverBackColor = Mischen(Akzent, Color.White, 0.15);
        knopf.FlatAppearance.MouseDownBackColor = Mischen(Akzent, Color.Black, 0.15);
        return knopf;
    }

    // Alle anderen Knöpfe: neutral.
    public static Button NebenKnopf(string text)
    {
        var knopf = Grundknopf(text);
        knopf.BackColor = Flaeche;
        knopf.ForeColor = TextNormal;
        knopf.Font = Standard;
        knopf.FlatAppearance.BorderColor = KarteRand;
        knopf.FlatAppearance.MouseOverBackColor = Mischen(Flaeche, Akzent, 0.08);
        knopf.FlatAppearance.MouseDownBackColor = Mischen(Flaeche, Akzent, 0.16);
        return knopf;
    }

    private static Button Grundknopf(string text)
    {
        var knopf = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Height = KnopfHoehe,
            Padding = new Padding(AbstandMittel, 0, AbstandMittel, 0),
            UseVisualStyleBackColor = false,
            // Knöpfe wachsen mit ihrer Beschriftung (wichtig bei 125 % / 150 % Skalierung).
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            MinimumSize = new Size(0, KnopfHoehe),
        };
        knopf.FlatAppearance.BorderSize = 1;
        return knopf;
    }

    // Alle Maße im Code sind für 100 % geschrieben; Windows Forms rechnet sie bei höherer
    // Skalierung selbst hoch. Aufruf als letzte Anweisung im Konstruktor eines Dialogs.
    public static void DpiSkalierungEinschalten(Form fenster)
    {
        fenster.AutoScaleDimensions = new SizeF(96F, 96F);
        fenster.AutoScaleMode = AutoScaleMode.Dpi;
        fenster.PerformLayout();
    }
}
