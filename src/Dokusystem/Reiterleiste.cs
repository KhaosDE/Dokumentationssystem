using System.Drawing.Drawing2D;

namespace Dokusystem;

// Reiter nebeneinander (z. B. in der Bewohnerakte). Aktiv: Akzentfarbe und ein Balken darunter.
// Bedienbar mit Maus und Tastatur (Pfeiltasten links/rechts).
public sealed class Reiterleiste : Control
{
    private readonly List<string> namen;
    private readonly List<Rectangle> felder = [];
    private int aktiv;
    private int maus = -1;

    public event Action<string>? Gewechselt;

    public Reiterleiste(IEnumerable<string> namen)
    {
        this.namen = namen.ToList();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Stil.Seite;
        Font = Stil.Standard;
        TabStop = true;
        AccessibleRole = AccessibleRole.PageTabList;
        Height = Stil.Px(this, 40);
    }

    public IReadOnlyList<string> Namen => namen;

    // Neue Reihe von Reitern (z. B. wenn ein Modul ein- oder ausgeschaltet wird). Der aktive Reiter
    // bleibt, wenn es ihn noch gibt; sonst gilt der erste.
    public void NamenSetzen(IEnumerable<string> neue)
    {
        string vorher = Aktiv;
        namen.Clear();
        namen.AddRange(neue);
        aktiv = Math.Max(0, namen.IndexOf(vorher));
        Invalidate();
    }
    public string Aktiv => namen[aktiv];

    public void Waehlen(string name)
    {
        int i = namen.IndexOf(name);
        if (i < 0 || i == aktiv) return;
        aktiv = i;
        AccessibleName = $"Reiter {name}";
        Invalidate();
        Gewechselt?.Invoke(name);
    }

    private void Berechnen(Graphics g)
    {
        felder.Clear();
        int x = 0, innen = Stil.Px(this, Stil.AbstandMittel);
        foreach (string name in namen)
        {
            int breite = TextRenderer.MeasureText(g, name, Stil.Fett, Size.Empty, TextFormatFlags.NoPadding).Width + 2 * innen;
            felder.Add(new Rectangle(x, 0, breite, Height));
            x += breite + Stil.Px(this, Stil.AbstandMini);
        }
    }

    private int Treffer(Point p)
    {
        using var g = CreateGraphics();
        Berechnen(g);
        return felder.FindIndex(f => f.Contains(p));
    }

    protected override void OnMouseMove(MouseEventArgs e) { int m = Treffer(e.Location); if (m != maus) { maus = m; Invalidate(); } base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { maus = -1; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int i = Treffer(e.Location);
        if (i >= 0) Waehlen(namen[i]);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left && aktiv > 0) { Waehlen(namen[aktiv - 1]); e.Handled = true; }
        if (e.KeyCode == Keys.Right && aktiv < namen.Count - 1) { Waehlen(namen[aktiv + 1]); e.Handled = true; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        Berechnen(g);
        float s = Stil.Skala(this);
        using (var linie = new Pen(Stil.KarteRand)) g.DrawLine(linie, 0, Height - 1, Width, Height - 1);
        for (int i = 0; i < namen.Count; i++)
        {
            var feld = felder[i];
            bool istAktiv = i == aktiv;
            if (i == maus && !istAktiv)
                Stil.FlaecheZeichnen(g, new RectangleF(feld.X, feld.Y + 4 * s, feld.Width, feld.Height - 8 * s),
                    Stil.RadiusKnopf * s, Stil.Mischen(Stil.Seite, Stil.Akzent, 0.06), Stil.Mischen(Stil.Seite, Stil.Akzent, 0.06));
            TextRenderer.DrawText(g, namen[i], istAktiv ? Stil.Fett : Stil.Standard, feld,
                istAktiv ? Stil.Akzent : Stil.TextMittel,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            if (istAktiv)
            {
                using var balken = new SolidBrush(Stil.Akzent);
                using var pfad = Stil.Abgerundet(new RectangleF(feld.X + 4 * s, Height - 3 * s, feld.Width - 8 * s, 3 * s), 1.5f * s);
                g.FillPath(balken, pfad);
            }
            if (Focused && istAktiv)
            {
                using var fokus = new Pen(Stil.Fokus) { DashStyle = DashStyle.Dot };
                g.DrawRectangle(fokus, feld.X + 2, feld.Y + 4, feld.Width - 4, feld.Height - 10);
            }
        }
    }
}
