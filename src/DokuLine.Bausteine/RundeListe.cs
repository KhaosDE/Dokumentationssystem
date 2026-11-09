using System.Drawing.Drawing2D;

namespace DokuLine;

// Kleine Überschrift in einer RundeListe (z. B. „R1“ über den Bewohnern des Wohnbereichs); nicht wählbar.
public sealed record ListenUeberschrift(string Text)
{
    public override string ToString() => Text;
}

// Schlichte Liste auf einer Fläche: die gewählte Zeile abgerundet in hellem Akzent, mit Fokus zusätzlich umrandet.
// Überschriften werden übersprungen (mit Pfeiltasten in Laufrichtung, mit der Maus zur Zeile darunter).
public sealed class RundeListe : ListBox
{
    private int zuletzt = -1;

    public RundeListe()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        IntegralHeight = false;
        BackColor = Stil.Flaeche;
        Font = Stil.Standard;
        ItemHeight = Font.Height + Stil.Px(this, Stil.AbstandKlein + Stil.AbstandMini);
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        int i = SelectedIndex;
        if (i >= 0 && i < Items.Count && Items[i] is ListenUeberschrift)
        {
            int schritt = i < zuletzt ? -1 : 1;
            foreach (int ziel in new[] { i + schritt, i - schritt })
                if (ziel >= 0 && ziel < Items.Count && Items[ziel] is not ListenUeberschrift) { SelectedIndex = ziel; return; }
        }
        zuletzt = i;
        base.OnSelectedIndexChanged(e);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var g = e.Graphics;
        using (var grund = new SolidBrush(BackColor)) g.FillRectangle(grund, e.Bounds);
        if (e.Index < 0 || e.Index >= Items.Count) return;
        int innen = Stil.Px(this, Stil.AbstandKlein);
        if (Items[e.Index] is ListenUeberschrift u)
        {
            TextRenderer.DrawText(g, u.Text, Stil.Abschnitt, Rectangle.FromLTRB(e.Bounds.Left + innen / 2, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom),
                Stil.TextLeise, TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.NoPrefix);
            return;
        }
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
        var text = Rectangle.FromLTRB(e.Bounds.Left + innen, e.Bounds.Top, e.Bounds.Right - innen, e.Bounds.Bottom);
        TextRenderer.DrawText(g, GetItemText(Items[e.Index]), gewaehlt ? Stil.Fett : Font, text,
            gewaehlt ? Stil.Akzent : Stil.TextNormal,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
