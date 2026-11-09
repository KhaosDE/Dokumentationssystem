using System.Drawing.Drawing2D;

namespace DokuLine;

// Schlichte Liste auf einer Fläche: die gewählte Zeile abgerundet in hellem Akzent, mit Fokus zusätzlich umrandet.
public sealed class RundeListe : ListBox
{
    public RundeListe()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        IntegralHeight = false;
        BackColor = Stil.Flaeche;
        Font = Stil.Standard;
        ItemHeight = Font.Height + Stil.Px(this, Stil.AbstandKlein + Stil.AbstandMini);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var g = e.Graphics;
        using (var grund = new SolidBrush(BackColor)) g.FillRectangle(grund, e.Bounds);
        if (e.Index < 0 || e.Index >= Items.Count) return;
        bool gewaehlt = (e.State & DrawItemState.Selected) != 0;
        if (gewaehlt)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(e.Bounds.X + 0.5f, e.Bounds.Y + 1.5f, e.Bounds.Width - 1.5f, e.Bounds.Height - 3f);
            using var pfad = Stil.Abgerundet(r, Stil.RadiusKnopf * Stil.Skala(this));
            using (var flaeche = new SolidBrush(Stil.AkzentHell)) g.FillPath(flaeche, pfad);
            if (Focused)
            {
                using var rand = new Pen(Stil.Fokus, 1f);
                g.DrawPath(rand, pfad);
            }
        }
        int innen = Stil.Px(this, Stil.AbstandKlein);
        var text = Rectangle.FromLTRB(e.Bounds.Left + innen, e.Bounds.Top, e.Bounds.Right - innen, e.Bounds.Bottom);
        TextRenderer.DrawText(g, GetItemText(Items[e.Index]), gewaehlt ? Stil.Fett : Font, text,
            gewaehlt ? Stil.Akzent : Stil.TextNormal,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
