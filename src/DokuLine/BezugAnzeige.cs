using DokuLine.Kern;

namespace DokuLine;

// Farben der Mitarbeiter für Bezugspflege und Einzeltrainer.
public static class BezugAnzeige
{
    public static Dictionary<string, string> Farben(Speicher speicher) =>
        speicher.AlleMitarbeiter().ToDictionary(m => m.Kuerzel, m => m.Farbe ?? BezugRegeln.Vorgabefarbe(m.Kuerzel), StringComparer.OrdinalIgnoreCase);

    public static Color Farbe(IReadOnlyDictionary<string, string> farben, string kuerzel) =>
        ColorTranslator.FromHtml(farben.GetValueOrDefault(kuerzel) ?? BezugRegeln.Vorgabefarbe(kuerzel));

    public static void Setzen(BewohnerKarte karte, Bezug? bezug, IReadOnlyDictionary<string, string> farben) =>
        karte.BezugSetzen(
            bezug?.Bezugspflege is { } b ? (b.Kuerzel, Farbe(farben, b.Kuerzel)) : null,
            bezug?.Einzeltrainer is { } t ? (BezugRegeln.TrainerText(t), Farbe(farben, t.Kuerzel)) : null);
}

// Ein kleiner Farbpunkt (z. B. vor der Bezugspflege in der Akte). Farbe null = unsichtbar.
public sealed class FarbPunkt : Control
{
    private Color? farbe;

    public FarbPunkt()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(12, 12);
        BackColor = Stil.Flaeche;
    }

    public Color? Farbe
    {
        get => farbe;
        set { farbe = value; Visible = value is not null; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (farbe is not Color f) return;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pinsel = new SolidBrush(f);
        e.Graphics.FillEllipse(pinsel, 0, 0, Width - 1, Height - 1);
    }
}
