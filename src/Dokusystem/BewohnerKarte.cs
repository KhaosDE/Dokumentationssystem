using System.Drawing.Drawing2D;
using Dokusystem.Kern;

namespace Dokusystem;

// Eine Karte im Kartenraster (Aufbau nach BewohnerKarte.cs der Ausgangsliste, Gestaltung Stufe 2):
// Bild oder Initialen, Name, Zimmer-Marke; darunter Marken für das, was Aufmerksamkeit braucht
// (Beschluss mit Enddatum, Allergie, „nicht erfasst“); unten die Aufenthaltsdauer.
// Weicher Schatten, Rand unter der Maus kräftiger. Die Karte zeichnet sich selbst.
// Klick, Eingabe oder Leertaste öffnet die Akte.
public sealed class BewohnerKarte : Control
{
    // Logische Maße bei 100 % Skalierung.
    public const int Breite = 236;     // Mindestbreite; das Raster verteilt den Rest (3 Spalten bei 1180 px)
    public const int Hoehe = 206;      // mit Schattenrand; drei Markenzeilen passen
    public const int RandSeite = 6, RandOben = 4, RandUnten = 10;   // Platz für den Schatten
    private const int BildGroesse = 52, Innen = 14, MarkeHoehe = 22;

    public Bewohner Bewohner { get; }
    public IReadOnlyList<Marke> Marken { get; }
    public string AufenthaltText { get; }
    private bool maus;

    public event Action<Bewohner>? Gewaehlt;

    // ungeleseneUebergaben: offene Übergabe-Einträge, die das eigene Kürzel noch nicht gelesen hat;
    // wichtigeBerichte: als „Wichtig“ gekennzeichnete Berichte der letzten 7 Tage.
    public BewohnerKarte(Bewohner bewohner, DateOnly heute, int ungeleseneUebergaben = 0, int wichtigeBerichte = 0)
    {
        Bewohner = bewohner;
        Marken = Kennzeichen.Marken(bewohner, ungeleseneUebergaben, wichtigeBerichte);
        AufenthaltText = Kennzeichen.Aufenthalt(bewohner.Aufnahmedatum, heute);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Stil.Seite;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        // Für Bildschirmleser: alles, was auf der Karte steht, als Text.
        string marken = Marken.Count > 0 ? ", " + string.Join(", ", Marken.Select(m => m.Text)) : "";
        AccessibleName = $"{bewohner.Name}, Zimmer {bewohner.Zimmer}{marken}. Akte öffnen";
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
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Stil.Seite);

        // ---- Fläche mit weichem Schatten ----
        var karte = new RectangleF(S(RandSeite), S(RandOben), Width - S(RandSeite) * 2, Height - S(RandOben) - S(RandUnten));
        float radius = Stil.RadiusKarte * Skala;
        Stil.SchattenZeichnen(g, karte, radius, Skala);
        Color rand = Focused ? Stil.Fokus : maus ? Stil.Mischen(Stil.KarteRand, Stil.Fokus, 0.55) : Stil.KarteRand;
        Stil.FlaecheZeichnen(g, karte, radius, Stil.Flaeche, rand, Focused ? 2f * Skala : 1f);

        int innen = S(Innen);
        int x = (int)karte.Left + innen, rechts = (int)karte.Right - innen, y = (int)karte.Top + innen;

        // ---- Kopf: Bild, Name, Zimmer ----
        int bild = S(BildGroesse);
        Bewohnerbild.Zeichnen(g, new Rectangle(x, y, bild, bild), Bewohner);
        int textX = x + bild + S(Stil.AbstandMittel);
        int nameHoehe = S(24);
        int blockY = y + (bild - nameHoehe - S(Stil.AbstandMini) - S(MarkeHoehe)) / 2;
        TextRenderer.DrawText(g, $"{Bewohner.Nachname}, {Bewohner.Vorname}", Stil.KarteName,
            new Rectangle(textX, blockY, Math.Max(0, rechts - textX), nameHoehe), Stil.TextNormal,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        Stil.MarkeZeichnen(g, $"Zi. {Bewohner.Zimmer}", textX, blockY + nameHoehe + S(Stil.AbstandMini), Stil.Zimmer, Skala);
        y += bild + S(Stil.AbstandMittel);

        // ---- Marken: höchstens drei Zeilen; zu lange Texte werden gekürzt ----
        int markeX = x, zeile = 0;
        foreach (var marke in Marken)
        {
            int breite = Math.Min(Stil.MarkenBreite(g, marke.Text, Skala), rechts - x);
            if (markeX > x && markeX + breite > rechts)
            {
                if (++zeile > 2) break;
                markeX = x;
                y += S(MarkeHoehe) + S(Stil.AbstandMini);
            }
            MarkeGekuerzt(g, marke.Text, markeX, y, breite, Stil.MarkeFuer(marke.Art));
            markeX += breite + S(Stil.AbstandMini);
        }

        // ---- Fußzeile: Aufenthaltsdauer ----
        int fussHoehe = S(18);
        int fussY = (int)karte.Bottom - innen + S(2) - fussHoehe;
        using (var linie = new Pen(Stil.Mischen(Stil.Flaeche, Stil.TextNormal, 0.08), 1f))
            g.DrawLine(linie, x, fussY - S(Stil.AbstandKlein), rechts, fussY - S(Stil.AbstandKlein));
        TextRenderer.DrawText(g, AufenthaltText, Stil.Klein, new Rectangle(x, fussY, rechts - x, fussHoehe), Stil.TextLeise,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
    }

    // Marke mit fester Breite; passt der Text nicht, endet er mit „…“.
    private void MarkeGekuerzt(Graphics g, string text, int x, int y, int breite, Stil.Marke marke)
    {
        var feld = new Rectangle(x, y, breite, S(MarkeHoehe));
        Stil.FlaecheZeichnen(g, feld, Stil.RadiusKnopf * Skala, marke.Flaeche, marke.Rand ?? marke.Flaeche);
        TextRenderer.DrawText(g, text, Stil.MarkeSchrift, Rectangle.Inflate(feld, -S(Stil.AbstandKlein), 0), marke.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
