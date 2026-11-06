using System.Drawing.Drawing2D;
using Dokusystem.Kern;

namespace Dokusystem;

// Eine Karte im Kartenraster: Bild (sonst Initialen), Name und Zimmer. Die Karte zeichnet sich
// selbst (Aufbau nach Vorbild der Bewohnerkarte der Ausgangsliste). Klick, Eingabe oder
// Leertaste öffnet die Akte.
public sealed class BewohnerKarte : Control
{
    // Logische Maße bei 100 % Skalierung.
    public const int Breite = 240;   // Mindestbreite; das Raster verteilt den Rest
    public const int Hoehe = 96;
    private const int BildGroesse = 56;

    public Bewohner Bewohner { get; }
    private bool maus;

    public event Action<Bewohner>? Gewaehlt;

    public BewohnerKarte(Bewohner bewohner)
    {
        Bewohner = bewohner;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Stil.Seite;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = $"{bewohner.Name}, Zimmer {bewohner.Zimmer}. Akte öffnen";
        Text = bewohner.Name;
    }

    private int S(int wert) => Stil.Px(this, wert);
    private float Skala => Stil.Skala(this);

    public void Waehlen() => Gewaehlt?.Invoke(Bewohner);

    protected override void OnMouseEnter(EventArgs e) { maus = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { maus = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left) Waehlen();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space) { Waehlen(); e.Handled = true; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Stil.Seite);

        // Fläche mit 1-px-Rand und Hauch von Schatten; unter der Maus oder mit Fokus kräftiger.
        var karte = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f - 2 * Skala);
        float radius = Stil.RadiusKarte * Skala;
        Stil.HauchSchatten(g, karte, radius, Skala);
        Color rand = Focused ? Stil.Fokus : maus ? Stil.Mischen(Stil.KarteRand, Stil.Fokus, 0.55) : Stil.KarteRand;
        Stil.FlaecheZeichnen(g, karte, radius, Stil.Flaeche, rand, Focused ? 2f * Skala : 1f);

        int innen = S(Stil.AbstandGross);
        int bild = S(BildGroesse);
        int bildY = (int)(karte.Top + (karte.Height - bild) / 2);
        Bewohnerbild.Zeichnen(g, new Rectangle(innen, bildY, bild, bild), Bewohner);

        int x = innen + bild + S(Stil.AbstandMittel);
        int rechts = (int)karte.Right - innen;
        int nameHoehe = S(24), markeHoehe = S(22);
        int blockY = (int)(karte.Top + (karte.Height - nameHoehe - S(Stil.AbstandMini) - markeHoehe) / 2);
        TextRenderer.DrawText(g, $"{Bewohner.Nachname}, {Bewohner.Vorname}", Stil.KarteName,
            new Rectangle(x, blockY, Math.Max(0, rechts - x), nameHoehe), Stil.TextNormal,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        Stil.MarkeZeichnen(g, $"Zimmer {Bewohner.Zimmer}", x, blockY + nameHoehe + S(Stil.AbstandMini), Stil.Zimmer, Skala);
    }
}
