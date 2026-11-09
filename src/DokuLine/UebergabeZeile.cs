using System.Drawing.Drawing2D;
using DokuLine.Kern;

namespace DokuLine;

// Ein Übergabe-Eintrag als kompakte Zeile, umrandet in der Farbe seiner Schicht: links der Bewohner (nur beim ersten
// Eintrag eines Bewohners) und „neu“, dann der Text (höchstens zwei Zeilen; Klick klappt den ganzen Bericht auf), gültig
// bis, Kürzel und die Knöpfe „Gelesen“ und „Erledigt“. Aufgeklappt dazu Herkunft, wer gelesen hat und „Verlängern …“.
public sealed class UebergabeZeile : Control
{
    private const int NameBreite = 180, GueltigBreite = 130, KuerzelBreite = 40;
    private readonly bool offen;
    private readonly string kurz, voll;

    public Uebergabe Uebergabe { get; }
    public Label NameText { get; } = Beschriftung(Stil.Fett, Stil.TextNormal);
    public Label TextFeld { get; } = Beschriftung(Stil.Standard, Stil.TextNormal);
    public Label Gueltig { get; } = Beschriftung(Stil.Klein, Stil.TextMittel);
    public Label Kuerzel { get; } = Beschriftung(Stil.Klein, Stil.TextMittel);
    public Label Herkunft { get; } = Beschriftung(Stil.Klein, Stil.TextLeise);
    public Label GelesenText { get; } = Beschriftung(Stil.Klein, Stil.TextMittel);
    public Button Gelesen { get; } = KleinKnopf("Gelesen");
    public Button Erledigt { get; } = KleinKnopf("Erledigt");
    public Button Verlaengern { get; } = KleinKnopf("Verlängern …");
    public List<MarkenFeld> Marken { get; } = [];
    public IReadOnlyList<string> MarkenTexte { get; }
    public bool Aufgeklappt { get; private set; }
    public Color Farbe { get; }

    public event Action<UebergabeZeile>? Umgeschaltet;
    public event Action<Uebergabe>? GelesenGeklickt;
    public event Action<Uebergabe>? ErledigtGeklickt;
    public event Action<Uebergabe>? VerlaengernGeklickt;

    public UebergabeZeile(Uebergabe u, string? name, DateTime jetzt, string kuerzel, DateTime? letzterBesuch)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw, true);
        Uebergabe = u;
        offen = UebergabeRegeln.Offen(u, jetzt);
        Farbe = offen ? BerichtZeile.Schichtfarbe(UebergabeRegeln.SchichtVon(u)) : Stil.KarteRand;
        BackColor = Stil.Flaeche;
        NameText.Text = name ?? "";
        if (offen && UebergabeRegeln.Neu(u, kuerzel, letzterBesuch)) Marken.Add(new MarkenFeld("neu", Stil.Unterwegs));
        MarkenTexte = Marken.Select(m => m.Text).ToList();
        kurz = u.Anzeige;
        voll = u.Text.Length > 0 && u.Bericht is Bericht b ? $"{u.Text}\n\nBericht: {b.Text}" : u.Anzeige;
        TextFeld.Text = kurz;
        TextFeld.ForeColor = offen ? Stil.TextNormal : Stil.TextLeise;
        bool heute = UebergabeRegeln.LaeuftHeuteAb(u, jetzt);
        Gueltig.Text = (heute ? "läuft heute ab, " : "")
                       + (UebergabeRegeln.SeitText(u) is string seit && offen ? seit : UebergabeRegeln.GueltigText(u, jetzt))
                       + (offen && u.Verlaengert > 0 ? " (verlängert)" : "");
        Gueltig.ForeColor = heute ? Stil.Achtung.Text : Stil.TextMittel;
        Gueltig.AutoEllipsis = TextFeld.AutoEllipsis = NameText.AutoEllipsis = true;
        Kuerzel.Text = u.Bericht?.Verfasser ?? u.Verfasser;
        Herkunft.Text = u.Bericht is Bericht h
            ? $"aus dem Bericht vom {BerichtRegeln.ZeitText(h.Zeitpunkt)} · {BerichtRegeln.SchichtText(h.Schicht)} · {h.Verfasser}"
            : $"Notiz von {u.Verfasser}, {BerichtRegeln.ZeitText(u.Erstellt)}";
        GelesenText.Text = u.Gelesen.Count == 0 ? "noch von niemandem als gelesen bestätigt"
            : "gelesen: " + string.Join(", ", u.Gelesen.Select(g => $"{g.Kuerzel} ({g.Zeit:dd.MM., HH:mm})"));
        bool schonGelesen = UebergabeRegeln.GelesenVon(u, kuerzel);
        Gelesen.Text = schonGelesen ? "Gelesen ✓" : "Gelesen";
        Gelesen.Enabled = offen && kuerzel.Length > 0 && !schonGelesen;
        Erledigt.Enabled = Verlaengern.Enabled = offen && kuerzel.Length > 0;
        Gelesen.Visible = Erledigt.Visible = offen;
        Gelesen.Click += (_, _) => GelesenGeklickt?.Invoke(u);
        Erledigt.Click += (_, _) => ErledigtGeklickt?.Invoke(u);
        Verlaengern.Click += (_, _) => VerlaengernGeklickt?.Invoke(u);
        Controls.AddRange([NameText, .. Marken, TextFeld, Gueltig, Kuerzel, Herkunft, GelesenText, Gelesen, Erledigt, Verlaengern]);
        foreach (var c in new Control[] { this, TextFeld, NameText }) { c.Cursor = Cursors.Hand; c.Click += (_, _) => Umschalten(); }
        AccessibleName = $"Übergabe: {(name is null ? "" : name + ", ")}{u.Anzeige}, {Gueltig.Text}";
        Aufklappen(false);
    }

    private static Label Beschriftung(Font schrift, Color farbe) =>
        new() { AutoSize = false, Font = schrift, ForeColor = farbe, BackColor = Stil.Flaeche, UseMnemonic = false };

    private static Button KleinKnopf(string text)
    {
        var k = Stil.NebenKnopf(text);
        k.Font = Stil.Klein;
        return k;
    }

    public void Umschalten()
    {
        Aufklappen(!Aufgeklappt);
        Umgeschaltet?.Invoke(this);
    }

    private void Aufklappen(bool auf)
    {
        Aufgeklappt = auf;
        TextFeld.Text = auf ? voll : kurz;
        Herkunft.Visible = GelesenText.Visible = auf;
        Verlaengern.Visible = auf && offen;
    }

    // Höhe für eine Breite; legt die Teile dabei an ihren Platz. Eingeklappt eine Zeile (Ziel: 12 Einträge bei 1366 × 768).
    public int Anordnen(int breite)
    {
        int P(int wert) => Stil.Px(this, wert);
        int innen = P(Stil.AbstandKlein), mini = P(Stil.AbstandMini), knopfHoehe = P(22), rand = P(3);
        int zeile = TextRenderer.MeasureText("Ag", TextFeld.Font).Height, klein = TextRenderer.MeasureText("Ag", Gueltig.Font).Height;
        int hoehe = Math.Max(Math.Max(zeile, knopfHoehe), Marken.Count > 0 ? Marken[0].Height : 0) + 2 * rand;
        int Mitte(int h) => (hoehe - h) / 2;
        int rechts = breite - innen;
        foreach (var k in offen ? new[] { Erledigt, Gelesen } : [])
        {
            int w = TextRenderer.MeasureText(k.Text, k.Font).Width + P(16);
            k.SetBounds(rechts - w, Mitte(knopfHoehe), w, knopfHoehe);
            rechts = k.Left - mini;
        }
        Kuerzel.SetBounds(rechts - P(KuerzelBreite), Mitte(klein), P(KuerzelBreite), klein);
        int gueltigX = Kuerzel.Left - P(GueltigBreite) - mini;
        Gueltig.SetBounds(gueltigX, Mitte(klein), P(GueltigBreite), klein);
        // Name und „neu“ in der linken Spalte; der Name wird gekürzt, wenn die Marke sonst nicht passt.
        int marke = Marken.Count > 0 ? Marken[0].Width + mini : 0;
        int nameBreite = NameText.Text.Length == 0 ? 0
            : Math.Min(P(NameBreite) - marke, TextRenderer.MeasureText(NameText.Text, NameText.Font).Width + P(2));
        NameText.SetBounds(innen, Mitte(zeile), nameBreite, zeile);
        if (Marken.Count > 0) Marken[0].Location = new Point(innen + nameBreite + (nameBreite > 0 ? mini : 0), Mitte(Marken[0].Height));
        int textX = innen + P(NameBreite) + innen, textBreite = Math.Max(P(120), gueltigX - innen - textX);
        if (!Aufgeklappt)
        {
            TextFeld.SetBounds(textX, Mitte(zeile), textBreite, zeile);
            return hoehe;
        }
        int unten = rand + P(2);
        foreach (var l in new[] { TextFeld, Herkunft, GelesenText })
        {
            l.SetBounds(textX, unten, textBreite, TextRenderer.MeasureText(l.Text, l.Font, new Size(textBreite, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height);
            unten = l.Bottom + P(2);
        }
        if (offen)
        {
            int w = TextRenderer.MeasureText(Verlaengern.Text, Verlaengern.Font).Width + P(16);
            Verlaengern.SetBounds(textX, unten + P(2), w, knopfHoehe);
            unten = Verlaengern.Bottom;
        }
        return Math.Max(hoehe, unten + rand);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float stark = 1.5f * DeviceDpi / 96f;
        var rechteck = new RectangleF(stark / 2, stark / 2, Width - stark - 1, Height - stark - 1);
        using var pfad = Stil.Abgerundet(rechteck, Stil.Px(this, Stil.RadiusKarte));
        using var stift = new Pen(Farbe, stark);
        g.DrawPath(stift, pfad);
    }
}

// Ein Abschnitt der Übergabe: Fläche mit 2 px Rand in der Farbe der Schicht, darüber das Schichtwort in derselben Farbe,
// der Zähler und „Alle in diesem Abschnitt gelesen“. Der Abschnitt „Wohnbereich allgemein“ ist einklappbar.
public sealed class UebergabeAbschnitt : Control
{
    private readonly Label kopf = new() { AutoSize = false, AutoEllipsis = true, Font = Stil.Fett, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label zaehler = new() { AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly Label leer = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, Text = "Keine Einträge." };
    private readonly bool einklappbar, mitAlleGelesen;

    public string Titel { get; }
    public Color Farbe { get; }
    public List<UebergabeZeile> Zeilen { get; }
    public Button AlleGelesen { get; } = Stil.NebenKnopf("Alle in diesem Abschnitt gelesen");
    public bool Eingeklappt { get; private set; }
    public string KopfText => $"{Titel} · {Zeilen.Count}";

    public UebergabeAbschnitt(string titel, Color farbe, List<UebergabeZeile> zeilen, bool einklappbar, bool alleGelesen = true)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw, true);
        Titel = titel;
        Farbe = farbe;
        Zeilen = zeilen;
        this.einklappbar = einklappbar;
        mitAlleGelesen = alleGelesen && !einklappbar;
        BackColor = Stil.Flaeche;
        kopf.ForeColor = farbe == Stil.KarteRand ? Stil.TextMittel : farbe;
        zaehler.Text = $"· {zeilen.Count}";
        AlleGelesen.Font = Stil.Klein;
        AlleGelesen.Visible = AlleGelesen.Enabled = mitAlleGelesen;
        leer.Visible = zeilen.Count == 0;
        Controls.AddRange([kopf, zaehler, AlleGelesen, leer, .. zeilen]);
        AccessibleName = KopfText;
        Eingeklappt = einklappbar;
        KopfSetzen();
        if (!einklappbar) return;
        foreach (var c in new Control[] { kopf, zaehler }) { c.Cursor = Cursors.Hand; c.Click += (_, _) => Umschalten(); }
    }

    // Eingeklappt steht die erste Zeile des ersten Eintrags im Kopf.
    private void KopfSetzen()
    {
        kopf.Text = !einklappbar ? Titel
            : Eingeklappt && Zeilen.Count > 0 ? $"▸ {Titel}: {HeuteWichtigRegeln.ErsteZeile(Zeilen[0].Uebergabe.Anzeige)}"
            : $"{(Eingeklappt ? "▸" : "▾")} {Titel}";
        foreach (var z in Zeilen) z.Visible = !Eingeklappt;
    }

    public event Action? Umgeschaltet;

    public void Umschalten()
    {
        if (!einklappbar) return;
        Eingeklappt = !Eingeklappt;
        KopfSetzen();
        Umgeschaltet?.Invoke();
    }

    public int Anordnen(int breite)
    {
        int P(int wert) => Stil.Px(this, wert);
        int innen = P(Stil.AbstandKlein), rand = P(2), oben = P(6), knopfHoehe = P(22);
        int kopfHoehe = Math.Max(kopf.PreferredHeight, knopfHoehe);
        int knopf = mitAlleGelesen ? TextRenderer.MeasureText(AlleGelesen.Text, AlleGelesen.Font).Width + P(16) : 0;
        if (mitAlleGelesen) AlleGelesen.SetBounds(breite - innen - knopf, oben + (kopfHoehe - knopfHoehe) / 2, knopf, knopfHoehe);
        int kopfBreite = Math.Min(TextRenderer.MeasureText(kopf.Text, kopf.Font).Width + P(4),
            Math.Max(P(80), breite - 4 * innen - knopf - zaehler.PreferredWidth));
        kopf.SetBounds(innen + rand, oben + (kopfHoehe - kopf.PreferredHeight) / 2, kopfBreite, kopf.PreferredHeight);
        zaehler.Location = new Point(kopf.Right + P(Stil.AbstandMini), kopf.Top);
        int y = oben + kopfHoehe + P(Stil.AbstandMini);
        if (Zeilen.Count == 0) { leer.Location = new Point(innen + rand, y); y = leer.Bottom + P(Stil.AbstandMini); }
        foreach (var z in Eingeklappt ? [] : Zeilen)
        {
            int hoehe = z.Anordnen(breite - 2 * innen);
            z.SetBounds(innen, y, breite - 2 * innen, hoehe);
            y += hoehe + P(Stil.AbstandMini);
        }
        return y + oben - P(Stil.AbstandMini);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Stil.Seite);
        float stark = 2f * DeviceDpi / 96f;
        var rechteck = new RectangleF(stark / 2, stark / 2, Width - stark - 1, Height - stark - 1);
        Stil.FlaecheZeichnen(g, rechteck, Stil.Px(this, Stil.RadiusKarte), Stil.Flaeche, Farbe, stark);
    }
}
