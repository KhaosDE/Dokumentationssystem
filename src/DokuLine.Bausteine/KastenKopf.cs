using System.Drawing.Drawing2D;

namespace DokuLine;

// Kopfleiste eines Kastens: helle Akzentfläche, oben abgerundet wie der Kasten, unten eine Trennlinie; links die
// Überschrift, rechts der Zähler. Klick öffnet den Bereich; beim Darüberfahren etwas kräftiger.
public sealed class KastenKopf : Control
{
    private bool maus;

    public string Zaehler { get; private set; } = "";

    public KastenKopf(string titel)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw, true);
        Text = titel;
        Font = Stil.KastenKopfSchrift;
        BackColor = Stil.Flaeche;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = titel;
    }

    public int Hoehe => TextRenderer.MeasureText("Ag", Font).Height + 2 * Stil.Px(this, Stil.AbstandKlein);

    public void ZaehlerSetzen(string zaehler)
    {
        Zaehler = zaehler;
        AccessibleName = zaehler.Length > 0 ? $"{Text}: {zaehler}" : Text;
        Invalidate();
    }

    public void Klicken() => OnClick(EventArgs.Empty);

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); maus = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); maus = false; Invalidate(); }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float d = Stil.Px(this, Stil.RadiusKarte) * 2f;
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1f);
        using (var pfad = new GraphicsPath())
        {
            pfad.AddArc(r.Left, r.Top, d, d, 180, 90);
            pfad.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            pfad.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
            pfad.CloseFigure();
            using var flaeche = new SolidBrush(maus ? Stil.KastenKopfMaus : Stil.KastenKopfFlaeche);
            g.FillPath(flaeche, pfad);
        }
        using (var linie = new Pen(Stil.KastenKopfLinie, 1f)) g.DrawLine(linie, 0, Height - 1, Width, Height - 1);
        int innen = Stil.Px(this, Stil.AbstandMittel);
        var platz = new Rectangle(innen, 0, Math.Max(0, Width - 2 * innen), Height);
        int zaehlerBreite = Zaehler.Length > 0 ? TextRenderer.MeasureText(g, Zaehler, Font).Width + innen : 0;
        TextRenderer.DrawText(g, Text, Font, platz with { Width = Math.Max(0, platz.Width - zaehlerBreite) }, Stil.TextNormal,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        if (Zaehler.Length > 0)
            TextRenderer.DrawText(g, Zaehler, Font, platz, Stil.TextMittel,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
