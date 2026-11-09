using System.Drawing.Drawing2D;

namespace DokuLine;

// Kuvert in der Kopfzeile: leise ohne ungelesene Nachrichten, sonst mit rotem Kreis, Ausrufezeichen und Zahl.
// Die Zahl steht immer als Text daneben, damit die Farbe nie allein die Information trägt.
public sealed class KuvertKnopf : Control
{
    private static readonly ToolTip Hinweis = new();
    private bool maus;

    public int Ungelesen { get; private set; }
    public string HinweisText { get; private set; } = "Keine ungelesenen Nachrichten";

    public KuvertKnopf()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        Setzen(0);
    }

    public void Setzen(int ungelesen)
    {
        Ungelesen = Math.Max(0, ungelesen);
        HinweisText = Ungelesen switch
        {
            0 => "Keine ungelesenen Nachrichten",
            1 => "1 ungelesene Nachricht",
            _ => $"{Ungelesen} ungelesene Nachrichten",
        };
        AccessibleName = $"Nachrichten: {HinweisText}";
        Hinweis.SetToolTip(this, HinweisText + " – Klick öffnet „Nachrichten“.");
        Size = GetPreferredSize(Size.Empty);
        Invalidate();
    }

    private string ZahlText => Ungelesen > 99 ? "99+" : $"{Ungelesen}";

    public override Size GetPreferredSize(Size proposedSize)
    {
        int seite = Stil.Px(this, 30);
        if (Ungelesen == 0) return new Size(seite, seite);
        int zahl = TextRenderer.MeasureText("! " + ZahlText, Stil.Fett).Width;
        return new Size(seite + zahl, seite);
    }

    public void Klicken() => OnClick(EventArgs.Empty);

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); maus = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); maus = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space) { e.Handled = true; Klicken(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int seite = Height;
        var kuvert = new Rectangle(0, 0, seite, seite);
        if (maus || Focused)
        {
            using var rand = new Pen(Stil.Fokus, Stil.Px(this, 1));
            using var pfad = Stil.Abgerundet(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Stil.Px(this, 6));
            g.DrawPath(rand, pfad);
        }
        Color symbol = Ungelesen > 0 ? Stil.TextNormal : Stil.TextLeise;
        TextRenderer.DrawText(g, Symbole.Nachrichten, Stil.Symbolschrift, kuvert, symbol,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        if (Ungelesen == 0) return;
        int punkt = Stil.Px(this, 14);
        var kreis = new Rectangle(seite - punkt - Stil.Px(this, 2), Stil.Px(this, 2), punkt, punkt);
        using (var rot = new SolidBrush(Stil.Gefahr)) g.FillEllipse(rot, kreis);
        TextRenderer.DrawText(g, "!", Stil.Abschnitt, kreis, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        var zahl = new Rectangle(seite, 0, Width - seite, seite);
        TextRenderer.DrawText(g, ZahlText, Stil.Fett, zahl, Stil.Gefahr,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
