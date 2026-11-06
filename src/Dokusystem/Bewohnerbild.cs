using System.Drawing.Drawing2D;
using Dokusystem.Kern;

namespace Dokusystem;

// Bewohnerbild oder Platzhalter mit Initialen (Regel 2: in den Demo-Daten nur Platzhalter).
public static class Bewohnerbild
{
    // Ruhige, gut lesbare Grundfarben für die Initialen (weiße Schrift).
    private static readonly Color[] Farben =
    [
        Color.FromArgb(45, 84, 156), Color.FromArgb(14, 102, 94), Color.FromArgb(91, 58, 153),
        Color.FromArgb(122, 74, 0), Color.FromArgb(42, 63, 107), Color.FromArgb(30, 107, 46),
        Color.FromArgb(163, 34, 26), Color.FromArgb(84, 128, 198),
    ];

    public static Color FarbeFuer(string schluessel) =>
        Farben[Math.Abs(schluessel.Sum(c => (int)c)) % Farben.Length];

    // Zeichnet Bild oder Platzhalter als Kreis in das Feld.
    public static void Zeichnen(Graphics g, Rectangle feld, string? bildPfad, string initialen, string farbschluessel)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var kreis = new GraphicsPath();
        kreis.AddEllipse(feld);
        using Image? bild = Laden(bildPfad);
        if (bild is not null)
        {
            var vorher = g.Clip;
            g.SetClip(kreis);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bild, Zuschnitt(bild.Size, feld));
            g.Clip = vorher;
            return;
        }
        using (var pinsel = new SolidBrush(FarbeFuer(farbschluessel))) g.FillPath(pinsel, kreis);
        using var schrift = new Font("Segoe UI Semibold", Math.Max(6f, feld.Height * 0.36f), GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, initialen, schrift, feld, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
    }

    public static void Zeichnen(Graphics g, Rectangle feld, Bewohner b) =>
        Zeichnen(g, feld, Bilder.Pfad(b.Bild), BewohnerRegeln.Initialen(b), b.Nachname);

    // Lädt ein Bild ohne die Datei zu sperren; null, wenn es fehlt oder kein Bild ist.
    public static Image? Laden(string? pfad)
    {
        if (pfad is null || !File.Exists(pfad)) return null;
        try
        {
            using var strom = new MemoryStream(File.ReadAllBytes(pfad));
            using var bild = Image.FromStream(strom);
            return new Bitmap(bild);
        }
        catch (Exception e) when (e is ArgumentException or IOException or OutOfMemoryException)
        {
            return null;
        }
    }

    // Bild so skalieren, dass es das Feld ganz füllt (mittig zugeschnitten).
    private static RectangleF Zuschnitt(Size bild, Rectangle feld)
    {
        float faktor = Math.Max((float)feld.Width / bild.Width, (float)feld.Height / bild.Height);
        float w = bild.Width * faktor, h = bild.Height * faktor;
        return new RectangleF(feld.X + (feld.Width - w) / 2, feld.Y + (feld.Height - h) / 2, w, h);
    }
}

// Ein Steuerelement, das nur ein rundes Bewohnerbild zeigt (Akte, Formular).
public sealed class BildFeld : Control
{
    private string? pfad;
    private string initialen = "?";
    private string farbschluessel = "";

    public BildFeld()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        AccessibleRole = AccessibleRole.Graphic;
    }

    public void Setzen(string? bildPfad, string initialen, string farbschluessel)
    {
        pfad = bildPfad;
        this.initialen = initialen;
        this.farbschluessel = farbschluessel;
        AccessibleName = bildPfad is null ? $"Platzhalter mit Initialen {initialen}" : "Bewohnerbild";
        Invalidate();
    }

    public bool HatBild => pfad is not null;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int seite = Math.Min(Width, Height) - 1;
        Bewohnerbild.Zeichnen(e.Graphics, new Rectangle(0, 0, seite, seite), pfad, initialen, farbschluessel);
    }
}
