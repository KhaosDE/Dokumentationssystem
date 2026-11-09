using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Monatsansicht: sechs Wochen à sieben Tage; je Tag bis zu drei Termine, sonst „+ n weitere“.
// Klick auf einen Tag öffnet die Tagesansicht.
public sealed class MonatsRaster : Control
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private const int KopfHoehe = 28, ZeileHoehe = 18;
    private DateOnly tag, heute, erster;
    private IReadOnlyList<KalenderTermin> termine = [];
    private IReadOnlyDictionary<long, string> namen = new Dictionary<long, string>();
    public event Action<DateOnly>? TagGeklickt;

    public MonatsRaster()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Font = Stil.Klein;
        AccessibleRole = AccessibleRole.Table;
    }

    private int S(int w) => Stil.Px(this, w);

    public void Setzen(DateOnly tag, IReadOnlyList<KalenderTermin> termine, DateOnly heute, IReadOnlyDictionary<long, string> namen)
    {
        this.tag = tag;
        this.heute = heute;
        this.termine = termine;
        this.namen = namen;
        erster = WochenplanRegeln.Montag(new DateOnly(tag.Year, tag.Month, 1));
        AccessibleName = $"Monat {tag.ToString("MMMM yyyy", De)}: " + string.Join("; ",
            termine.Select(k => $"{KalenderRegeln.ZeitText(k.Aktuell)} {k.Aktuell.Titel}"));
        Invalidate();
    }

    public List<string> ZeilenAm(DateOnly d) =>
        termine.Where(k => k.Am(d)).Select(k =>
        {
            var f = k.Aktuell;
            string zeit = f.Ganztags || DateOnly.FromDateTime(f.Beginn) != d ? "" : $"{f.Beginn:HH\\:mm} ";
            string wer = f.Bewohner.Count == 1 ? $" ({namen.GetValueOrDefault(f.Bewohner[0], "?").Split(' ')[0]})" : f.Bewohner.Count > 1 ? $" ({f.Bewohner.Count})" : "";
            return zeit + (f.Art == KalenderArt.Heimfahrt ? "Heimfahrt" : f.Art == KalenderArt.Ausflug ? $"Ausflug {f.Titel}" : f.Titel) + wer;
        }).ToList();

    public int BenoetigteHoehe(int breite) => S(KopfHoehe) + 6 * (S(ZeileHoehe) * 4 + S(10)) + 1;

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int kopf = S(KopfHoehe);
        if (e.Y < kopf) return;
        float spalte = Width / 7f, zeile = (Height - kopf) / 6f;
        int i = (int)(e.X / spalte), w = (int)((e.Y - kopf) / zeile);
        if (i is >= 0 and < 7 && w is >= 0 and < 6) TagGeklickt?.Invoke(erster.AddDays(w * 7 + i));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Stil.Seite);
        Stil.FlaecheZeichnen(g, new Rectangle(0, 0, Width - 1, Height - 1), Stil.RadiusKarte * Stil.Skala(this), Stil.Flaeche, Stil.KarteRand);
        int kopf = S(KopfHoehe);
        float spalte = Width / 7f, zeile = (Height - kopf) / 6f;
        using var linie = new Pen(Stil.Trennlinie);
        for (int i = 0; i < 7; i++)
            TextRenderer.DrawText(g, De.DateTimeFormat.GetAbbreviatedDayName(TherapieRegeln.Woche[i]), Stil.Fett,
                new Rectangle((int)(i * spalte), 0, (int)spalte, kopf), Stil.TextMittel, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        for (int w = 0; w < 6; w++)
            for (int i = 0; i < 7; i++)
            {
                var d = erster.AddDays(w * 7 + i);
                var feld = new Rectangle((int)(i * spalte), (int)(kopf + w * zeile), (int)spalte, (int)zeile);
                if (d == heute) using (var b = new SolidBrush(Stil.AkzentHell)) g.FillRectangle(b, Rectangle.Inflate(feld, -1, -1));
                g.DrawRectangle(linie, feld);
                bool imMonat = d.Month == tag.Month;
                TextRenderer.DrawText(g, d.Day.ToString(), d == heute ? Stil.Fett : Stil.Standard, new Rectangle(feld.X + S(4), feld.Y + S(2), feld.Width - S(8), S(ZeileHoehe)),
                    imMonat ? (d == heute ? Stil.Akzent : Stil.TextNormal) : Stil.TextLeise, TextFormatFlags.NoPadding);
                var zeilen = ZeilenAm(d);
                int y = feld.Y + S(ZeileHoehe) + S(2);
                for (int z = 0; z < Math.Min(3, zeilen.Count); z++)
                {
                    var k = termine.Where(x => x.Am(d)).ElementAt(z);
                    var marke = k.Aktuell.Haus ? Stil.Abwesend : k.Aktuell.Art switch { KalenderArt.Heimfahrt => Stil.Achtung, KalenderArt.Ausflug => Stil.Hinweis, KalenderArt.Wohnbereich => Stil.Zimmer, _ => Stil.Unterwegs };
                    var r = new Rectangle(feld.X + S(3), y, feld.Width - S(6), S(ZeileHoehe) - S(2));
                    using (var b = new SolidBrush(marke.Flaeche)) g.FillRectangle(b, r);
                    TextRenderer.DrawText(g, (z == 2 && zeilen.Count > 3) ? $"+ {zeilen.Count - 2} weitere" : zeilen[z], Stil.Klein, r, marke.Text,
                        TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
                    y += S(ZeileHoehe);
                }
            }
    }
}

// Ein Termin in der Tagesansicht mit allen Angaben und den Knöpfen.
public sealed class KalenderKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label titel = new();
    private readonly Label angaben = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public KalenderTermin Termin { get; }
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern …");
    public Button AbsagenKnopf { get; } = Stil.NebenKnopf("Absagen …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public Button ListeKnopf { get; } = Stil.NebenKnopf("Teilnehmerliste drucken …");
    public IReadOnlyList<string> MarkenTexte { get; }
    public string AngabenText => angaben.Text;

    public KalenderKarte(KalenderTermin k, IReadOnlyDictionary<long, string> namen)
    {
        Termin = k;
        var f = k.Aktuell;
        var marke = f.Art switch { KalenderArt.Heimfahrt => Stil.Achtung, KalenderArt.Ausflug => Stil.Hinweis, KalenderArt.Wohnbereich => Stil.Zimmer, _ => Stil.Unterwegs };
        var liste = new List<(string, Stil.Marke)>
        {
            ($"Kalender {KalenderRegeln.KalenderName(f.Haus)}", f.Haus ? Stil.Abwesend : Stil.Zimmer),
            (KalenderRegeln.ArtText(f.Art), marke), (KalenderRegeln.ZeitText(f), Stil.Zimmer),
        };
        if (k.Fassungen.Count > 1) liste.Add(("geändert", Stil.Hinweis));
        MarkenTexte = liste.Select(x => x.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });
        titel.Text = f.Art == KalenderArt.Ausflug ? $"Ausflug: {f.Titel}" : f.Titel;
        titel.Font = Stil.KarteName;
        titel.ForeColor = Stil.TextNormal;
        var zeilen = new List<string>();
        if (f.Bewohner.Count > 0) zeilen.Add((f.Art == KalenderArt.Ausflug ? "Teilnehmer: " : "Bewohner: ") + string.Join(", ", f.Bewohner.Select(b => namen.GetValueOrDefault(b, "?"))));
        if (f.Ort.Length > 0) zeilen.Add($"Ort: {f.Ort}");
        if (f.Treffpunkt.Length > 0) zeilen.Add($"Treffpunkt: {f.Treffpunkt}");
        if (f.Begleitung.Length > 0) zeilen.Add($"Begleitung: {f.Begleitung}");
        if (f.Hinweis.Length > 0) zeilen.Add($"Hinweis: {f.Hinweis}");
        zeilen.Add($"eingetragen von {f.Kuerzel}");
        angaben.Text = string.Join("\n", zeilen);
        angaben.Font = Stil.Standard;
        angaben.ForeColor = Stil.TextMittel;
        foreach (var l in new[] { titel, angaben })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        ListeKnopf.Visible = f.Art == KalenderArt.Ausflug;
        foreach (var b in new[] { AendernKnopf, AbsagenKnopf, VerlaufKnopf, ListeKnopf })
        {
            b.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            knoepfe.Controls.Add(b);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{titel.Text}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Termin is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var l in new[] { titel, angaben })
        {
            l.MaximumSize = new Size(breite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, 3);
        }
        knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int hoehe = knoepfe.Bottom + innen - Stil.Px(this, Stil.AbstandMini);
        if (Height != hoehe) Height = hoehe;
    }
}
