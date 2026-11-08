using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Bereich „Kalender“: Monat, Woche, Tag; Filter „alle“ oder ein Bewohner. Termine von Bewohnern,
// des Wohnbereichs, Heimfahrten und Tagesausflüge. Ändern und Absagen sind neue Fassungen.
// Ausdruck der Monats- oder Wochenansicht und der Teilnehmerliste eines Ausflugs.
public sealed class KalenderSeite : Panel
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];
    private List<Bewohner> bewohner = [];

    public Dictionary<KalenderAnsicht, FilterKnopf> Ansichten { get; } = [];
    public KalenderAnsicht Ansicht { get; private set; } = KalenderAnsicht.Monat;
    public DateOnly Stichtag { get; private set; }
    public Label ZeitraumText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite };
    public Button ZurueckKnopf { get; } = Stil.NebenKnopf("◀");
    public Button VorKnopf { get; } = Stil.NebenKnopf("▶");
    public Button HeuteKnopf { get; } = Stil.NebenKnopf("Heute");
    public ComboBox Filter { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    // Schritt 24: Kalender „Bereich“ und „Haus“ – beide, nur Bereich oder nur Haus.
    public ComboBox Kalenderwahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    public KalenderWahl Wahl => (KalenderWahl)Math.Max(0, Kalenderwahl.SelectedIndex);
    public FahrzeugLeiste Fahrzeuge { get; }
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neuer Termin …");
    public MonatsRaster Monat { get; } = new();
    public WochenRaster Woche { get; } = new();
    public List<KalenderKarte> Karten { get; } = [];
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public KalenderSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        Stichtag = DateOnly.FromDateTime(Jetzt());
        foreach (var a in Enum.GetValues<KalenderAnsicht>())
        {
            var knopf = new FilterKnopf(a switch { KalenderAnsicht.Monat => "Monat", KalenderAnsicht.Woche => "Woche", _ => "Tag" }, mitZahl: false)
                { Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein) };
            knopf.Click += (_, _) => AnsichtWaehlen(a);
            Ansichten[a] = knopf;
            leiste.Controls.Add(knopf);
        }
        Kalenderwahl.Items.AddRange(["Bereich und Haus", "nur Bereich", "nur Haus"]);
        Kalenderwahl.SelectedIndex = 0;
        Kalenderwahl.AccessibleName = "Welche Kalender";
        Kalenderwahl.SelectedIndexChanged += (_, _) => Aktualisieren();
        Fahrzeuge = new FahrzeugLeiste(speicher) { BackColor = Stil.Seite };
        Fahrzeuge.Geaendert += () => Geaendert?.Invoke();
        foreach (var c in new Control[] { ZurueckKnopf, ZeitraumText, VorKnopf, HeuteKnopf, Filter, Kalenderwahl, DruckKnopf })
        {
            c.Margin = new Padding(c == ZurueckKnopf ? Stil.AbstandMittel : 0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        ZurueckKnopf.Width = VorKnopf.Width = 44;
        ZurueckKnopf.AccessibleName = "Zurück";
        VorKnopf.AccessibleName = "Weiter";
        ZeitraumText.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein + 2, Stil.AbstandKlein, 0);
        Filter.Margin = new Padding(Stil.AbstandMittel, Stil.AbstandMini + 2, Stil.AbstandKlein, Stil.AbstandKlein);
        Kalenderwahl.Margin = new Padding(0, Stil.AbstandMini + 2, Stil.AbstandKlein, Stil.AbstandKlein);
        ZurueckKnopf.Click += (_, _) => TagWaehlen(KalenderRegeln.Blaettern(Ansicht, Stichtag, -1));
        VorKnopf.Click += (_, _) => TagWaehlen(KalenderRegeln.Blaettern(Ansicht, Stichtag, 1));
        HeuteKnopf.Click += (_, _) => TagWaehlen(DateOnly.FromDateTime(Jetzt()));
        Filter.SelectedIndexChanged += (_, _) => Aktualisieren();
        DruckKnopf.Click += (_, _) => Drucken();
        NeuKnopf.Click += (_, _) => Bearbeiten(null);
        Monat.TagGeklickt += t => { Stichtag = t; AnsichtWaehlen(KalenderAnsicht.Tag); };
        Woche.TerminGeklickt += (t, _) => { if (t.KalenderId is long id) DetailZeigen(id); };
        Rueckfrage = text => MessageBox.Show(FindForm(), text, "Termin absagen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        Controls.Add(leiste);
        Controls.Add(Fahrzeuge);
    }

    public long? BewohnerFilter => Filter.SelectedIndex > 0 ? bewohner[Filter.SelectedIndex - 1].Id : null;

    public string KopfText
    {
        get
        {
            var heute = DateOnly.FromDateTime(Jetzt());
            int n = speicher.Kalender(heute, heute).Count;
            return n == 0 ? "heute keine Termine" : n == 1 ? "heute 1 Termin" : $"heute {n} Termine";
        }
    }

    public void Zeigen()
    {
        bewohner = speicher.AlleBewohner();
        long? vorher = BewohnerFilter;
        Filter.Items.Clear();
        Filter.Items.Add("alle Bewohner");
        Filter.Items.AddRange(bewohner.Select(b => (object)b.Name).ToArray());
        Filter.SelectedIndex = vorher is long v ? bewohner.FindIndex(b => b.Id == v) + 1 : 0;   // löst Aktualisieren aus
        Aktualisieren();
    }

    public void FilterSetzen(long? bewohnerId) => Filter.SelectedIndex = bewohnerId is long id ? bewohner.FindIndex(b => b.Id == id) + 1 : 0;

    public void AnsichtWaehlen(KalenderAnsicht a)
    {
        Ansicht = a;
        Aktualisieren();
    }

    public void TagWaehlen(DateOnly tag)
    {
        Stichtag = tag;
        Aktualisieren();
    }

    public List<KalenderTermin> Termine()
    {
        var (von, bis) = Ansicht == KalenderAnsicht.Monat
            ? (WochenplanRegeln.Montag(new DateOnly(Stichtag.Year, Stichtag.Month, 1)), WochenplanRegeln.Montag(new DateOnly(Stichtag.Year, Stichtag.Month, 1)).AddDays(41))
            : KalenderRegeln.Zeitraum(Ansicht, Stichtag);
        return speicher.Kalender(von, bis, BewohnerFilter).Where(k => KalenderRegeln.Passt(k.Aktuell, Wahl)).ToList();
    }

    private Dictionary<long, string> Namen() => speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);

    public void Aktualisieren()
    {
        foreach (var (a, knopf) in Ansichten) knopf.Aktiv = a == Ansicht;
        Recht.NurAngemeldet(NeuKnopf);   // Schritt 26a: ohne Anmeldung nur ansehen
        ZeitraumText.Text = KalenderRegeln.ZeitraumText(Ansicht, Stichtag);
        Fahrzeuge.Aktualisieren();
        string? hausGrund = Recht.Grund(speicher, Funktionen.KalenderHaus);   // Haus-Termine: Recht „Kalender Haus bearbeiten“
        var termine = Termine();
        var namen = Namen();
        var heute = DateOnly.FromDateTime(Jetzt());
        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); if (c is not MonatsRaster and not WochenRaster) c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        switch (Ansicht)
        {
            case KalenderAnsicht.Monat:
                Monat.Setzen(Stichtag, termine, heute, namen);
                inhalt.Add(Monat);
                break;
            case KalenderAnsicht.Woche:
                var montag = WochenplanRegeln.Montag(Stichtag);
                var liste = new List<Termin>();
                for (int i = 0; i < 7; i++)
                    foreach (var k in termine.Where(k => k.Am(montag.AddDays(i))))
                    {
                        var t = KalenderRegeln.AlsTermin(k, montag.AddDays(i));
                        string wer = k.Aktuell.Bewohner.Count == 0 ? "" : " · " + string.Join(", ", k.Aktuell.Bewohner.Select(b => namen.GetValueOrDefault(b, "?").Split(' ')[0]));
                        liste.Add(t with { Titel = t.Titel + wer });
                    }
                Woche.Setzen(montag, liste.OrderBy(t => t.Tag).ThenBy(t => t.Von).ToList(), heute);
                inhalt.Add(Woche);
                break;
            default:
                foreach (var k in termine.Where(k => k.Am(Stichtag)))
                {
                    var karte = new KalenderKarte(k, namen);
                    if (k.Aktuell.Haus) foreach (var knopf in new[] { karte.AendernKnopf, karte.AbsagenKnopf }) Stil.Sperren(knopf, hausGrund);
                    karte.AendernKnopf.Click += (_, _) => Bearbeiten(k);
                    karte.AbsagenKnopf.Click += (_, _) => Absagen(k);
                    karte.VerlaufKnopf.Click += (_, _) => VerlaufZeigen(k);
                    karte.ListeKnopf.Click += (_, _) => TeilnehmerlisteDrucken(k);
                    Karten.Add(karte);
                    inhalt.Add(karte);
                }
                if (Karten.Count == 0) inhalt.Add(new Label { Text = "Keine Termine an diesem Tag.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
                break;
        }
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    private static string Kuerzel => Einstellungen.Laden().Kuerzel;

    // Klick in der Wochenansicht: zum Tag springen, dort stehen Knöpfe und Einzelheiten.
    public void DetailZeigen(long id)
    {
        if (speicher.KalenderTermin(id) is not KalenderTermin k) return;
        Stichtag = DateOnly.FromDateTime(k.Aktuell.Beginn) < KalenderRegeln.Zeitraum(KalenderAnsicht.Woche, Stichtag).Von
            ? KalenderRegeln.Zeitraum(KalenderAnsicht.Woche, Stichtag).Von : DateOnly.FromDateTime(k.Aktuell.Beginn);
        AnsichtWaehlen(KalenderAnsicht.Tag);
    }

    public void Bearbeiten(KalenderTermin? k)
    {
        using var fenster = new KalenderFenster(speicher, k, Stichtag, BewohnerFilter);
        Dialog.Zeigen(fenster, FindForm());
        Aktualisieren();   // immer neu lesen: auch nach „Andere übernehmen“ im Konflikt-Hinweis
    }

    public void Absagen(KalenderTermin k)
    {
        if (Rueckfrage($"„{k.Aktuell.Titel}“ ({KalenderRegeln.ZeitText(k.Aktuell)}) absagen?\n\nDer Termin bleibt im Verlauf sichtbar.") != DialogResult.Yes) return;
        try { speicher.KalenderAbsagen(k.Id, Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Kalender", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    // Zum Testen ersetzbar: Rückfrage vor dem Absagen.
    internal Func<string, DialogResult> Rueckfrage { get; set; }

    public void VerlaufZeigen(KalenderTermin k)
    {
        using var fenster = new TextFenster($"Verlauf: {k.Aktuell.Titel}", string.Join(Environment.NewLine, KalenderRegeln.Verlauf(k, Namen())));
        Dialog.Zeigen(fenster, FindForm());
    }

    public void Drucken()
    {
        var namen = Namen();
        var druck = new KalenderDruck(Ansicht == KalenderAnsicht.Woche ? KalenderAnsicht.Woche : KalenderAnsicht.Monat, Stichtag, Termine(), namen,
            Filter.SelectedIndex > 0 ? (string)Filter.SelectedItem! : null);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Kalender {ZeitraumText.Text}");
    }

    public void TeilnehmerlisteDrucken(KalenderTermin k)
    {
        var druck = new AusflugDruck(k.Aktuell, speicher.AlleBewohner().Where(b => k.Aktuell.Bewohner.Contains(b.Id!.Value)).ToList());
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Teilnehmerliste {k.Aktuell.Titel}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(Math.Max(100, breite), 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        if (Fahrzeuge is FahrzeugLeiste fahrzeuge)   // im Konstruktor (AutoScroll) noch nicht angelegt
        {
            fahrzeuge.MaximumSize = new Size(breite, 0);
            fahrzeuge.Bounds = new Rectangle(new Point(rand, y), fahrzeuge.GetPreferredSize(new Size(breite, 0)));
            y = fahrzeuge.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        foreach (var c in inhalt)
        {
            switch (c)
            {
                case MonatsRaster m:
                    m.SetBounds(rand, y, breite, m.BenoetigteHoehe(breite));
                    y = m.Bottom + rand;
                    break;
                case WochenRaster w:
                    w.SetBounds(rand, y, breite, w.BenoetigteHoehe());
                    y = w.Bottom + rand;
                    break;
                case KalenderKarte k:
                    k.SetBounds(rand, y, breite, k.Height);
                    k.PerformLayout();
                    y += k.Height + Stil.Px(this, Stil.AbstandKlein);
                    break;
                default:
                    c.Location = new Point(rand, y + Stil.Px(this, Stil.AbstandKlein));
                    y = c.Bottom;
                    break;
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }

}

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
            ($"Kalender {KalenderRegeln.KalenderName(f.Haus)}", f.Haus ? Stil.Abwesend : Stil.Zimmer),   // Schritt 24
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

// Termin anlegen oder ändern (neue Fassung). Je nach Art passende Felder.
public sealed class KalenderFenster : Form
{
    private readonly Speicher speicher;
    private readonly KalenderTermin? bisher;
    private readonly List<Bewohner> bewohner;

    public RadioButton[] Arten { get; } = Enum.GetValues<KalenderArt>()
        .Select(a => new RadioButton { Text = KalenderRegeln.ArtText(a) == "Termin" ? "Termin eines Bewohners" : KalenderRegeln.ArtText(a), AutoSize = true }).ToArray();
    public RadioButton BereichWahl { get; } = new() { Text = "Bereich", AutoSize = true, Checked = true };
    public RadioButton HausWahl { get; } = new() { Text = "Haus", AutoSize = true };
    public TextBox Titel { get; } = new() { Width = 320 };
    public TextBox BeginnDatum { get; } = new() { Width = 100 };
    public TextBox BeginnUhrzeit { get; } = new() { Width = 60 };
    public TextBox EndeDatum { get; } = new() { Width = 100, PlaceholderText = "gleicher Tag" };
    public TextBox EndeUhrzeit { get; } = new() { Width = 60 };
    public CheckBox Ganztags { get; } = new() { Text = "ganztags", AutoSize = true };
    public CheckedListBox BewohnerListe { get; } = new() { Width = 320, Height = 150, CheckOnClick = true, IntegralHeight = false };
    public TextBox Ort { get; } = new() { Width = 320 };
    public TextBox Treffpunkt { get; } = new() { Width = 320 };
    public PersonenWahl Begleitung { get; }   // Schritt 26a: Auswahl aus den Mitarbeitern
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public KalenderTermin? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public KalenderFenster(Speicher speicher, KalenderTermin? bisher, DateOnly vorschlag, long? bewohnerId)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        bewohner = speicher.AlleBewohner();
        Begleitung = new PersonenWahl(speicher) { AccessibleName = "Begleitung" };
        Text = (bisher is null ? "Neuer Termin" : "Termin ändern") + $" ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        var arten = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(460, 0), Margin = new Padding(0) };
        arten.Controls.AddRange(Arten);
        zeile("Art", arten);
        // Schritt 24: Kalender „Haus“ nur mit dem Recht „Kalender Haus bearbeiten“.
        zeile("Kalender", Formular.Reihe(BereichWahl, HausWahl));
        if (Recht.Grund(speicher, Funktionen.KalenderHaus) is string hausGrund)
        {
            HausWahl.Enabled = false;
            HausWahl.Text = $"Haus ({hausGrund.TrimEnd('.')})";
        }
        zeile("Titel / Ziel", Titel);
        zeile("Beginn", Formular.Reihe(BeginnDatum, BeginnUhrzeit, Ganztags));
        zeile("Ende", Formular.Reihe(EndeDatum, EndeUhrzeit));
        BewohnerListe.Items.AddRange(bewohner.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
        zeile("Bewohner", BewohnerListe);
        zeile("Ort", Ort);
        zeile("Treffpunkt (Ausflug)", Treffpunkt);
        zeile("Begleitung (Ausflug)", Begleitung);
        zeile("Hinweis", Hinweis);
        zeile(null, new Label { Text = "Mitgegebene Medikation bei Heimfahrten bitte im Hinweis vermerken; sie wird nicht berechnet.", AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise });
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Ganztags.CheckedChanged += (_, _) => BeginnUhrzeit.Enabled = EndeUhrzeit.Enabled = !Ganztags.Checked;
        foreach (var r in Arten) r.CheckedChanged += (_, _) => FelderAnpassen();

        if (bisher is not null)
        {
            var f = bisher.Aktuell;
            Arten[(int)f.Art].Checked = true;
            (f.Haus ? HausWahl : BereichWahl).Checked = true;
            Titel.Text = f.Titel;
            BeginnDatum.Text = Datum.Text(DateOnly.FromDateTime(f.Beginn));
            EndeDatum.Text = Datum.Text(KalenderRegeln.LetzterTag(f));
            Ganztags.Checked = f.Ganztags;
            if (!f.Ganztags)
            {
                BeginnUhrzeit.Text = f.Beginn.ToString("HH:mm");
                EndeUhrzeit.Text = f.Ende.ToString("HH:mm");
                EndeDatum.Text = Datum.Text(DateOnly.FromDateTime(f.Ende));
            }
            for (int i = 0; i < bewohner.Count; i++) BewohnerListe.SetItemChecked(i, f.Bewohner.Contains(bewohner[i].Id!.Value));
            Ort.Text = f.Ort;
            Treffpunkt.Text = f.Treffpunkt;
            Begleitung.Setzen(PersonenWahl.Lesen(f.Begleitung));
            Hinweis.Text = f.Hinweis;
        }
        else
        {
            Arten[0].Checked = true;
            BeginnDatum.Text = Datum.Text(vorschlag);
            BeginnUhrzeit.Text = "10:00";
            EndeUhrzeit.Text = "11:00";
            if (bewohnerId is long id && bewohner.FindIndex(b => b.Id == id) is int i and >= 0) BewohnerListe.SetItemChecked(i, true);
        }
        FelderAnpassen();
        Stil.DpiSkalierungEinschalten(this);
    }

    public KalenderArt Art => (KalenderArt)Math.Max(0, Array.FindIndex(Arten, r => r.Checked));

    private void FelderAnpassen()
    {
        bool ausflug = Art == KalenderArt.Ausflug;
        Treffpunkt.Enabled = Begleitung.Enabled = ausflug;
        BewohnerListe.Enabled = Art != KalenderArt.Wohnbereich;
    }

    public void BewohnerAnkreuzen(long id) => BewohnerListe.SetItemChecked(bewohner.FindIndex(b => b.Id == id), true);

    private bool ohnePruefung;   // Mehrplatz: nach „Meine Fassung speichern“ im Konflikt-Hinweis

    public bool Speichern()
    {
        try
        {
            var f = KalenderRegeln.AusEingabe(new KalenderEingabe
            {
                Art = Art, Titel = Titel.Text, BeginnDatum = BeginnDatum.Text, BeginnUhrzeit = BeginnUhrzeit.Text,
                EndeDatum = EndeDatum.Text, EndeUhrzeit = EndeUhrzeit.Text, Ganztags = Ganztags.Checked, Ort = Ort.Text, Hinweis = Hinweis.Text,
                Bewohner = Art == KalenderArt.Wohnbereich ? [] : BewohnerListe.CheckedIndices.Cast<int>().Select(i => bewohner[i].Id!.Value).ToList(),
                Treffpunkt = Art == KalenderArt.Ausflug ? Treffpunkt.Text : "", Begleitung = Art == KalenderArt.Ausflug ? Begleitung.AlsText : "",
                Haus = HausWahl.Checked, Kuerzel = Einstellungen.Laden().Kuerzel,
            }, Jetzt());
            Gespeichert = bisher is null ? speicher.KalenderAnlegen(f) : speicher.KalenderAendern(bisher.Id, f, ohnePruefung ? null : bisher.Fassungen.Count);
        }
        catch (Konflikt k)
        {
            switch (KonfliktFenster.Fragen(this, k))
            {
                case KonfliktWahl.MeineSpeichern:
                    ohnePruefung = true;
                    return Speichern();
                case KonfliktWahl.AndereUebernehmen:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return false;
                default:
                    return false;
            }
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Ausdruck Monat (A4 quer, Raster) oder Woche (A4 hoch, Liste je Tag).
public sealed class KalenderDruck(KalenderAnsicht ansicht, DateOnly tag, IReadOnlyList<KalenderTermin> termine, IReadOnlyDictionary<long, string> namen, string? nurFuer)
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Kalender" };
        dokument.DefaultPageSettings.Landscape = ansicht == KalenderAnsicht.Monat;
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    private string Zeile(KalenderTermin k, DateOnly d)
    {
        var f = k.Aktuell;
        string zeit = f.Ganztags || DateOnly.FromDateTime(f.Beginn) != d ? "" : $"{f.Beginn:HH\\:mm} ";
        string wer = f.Bewohner.Count > 0 ? " – " + string.Join(", ", f.Bewohner.Select(b => namen.GetValueOrDefault(b, "?"))) : "";
        return zeit + (f.Art == KalenderArt.Heimfahrt ? "Heimfahrt" : f.Art == KalenderArt.Ausflug ? $"Ausflug {f.Titel}" : f.Titel) + wer;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 9F);
        using var normal = new Font("Segoe UI", 9F);
        using var klein = new Font("Segoe UI", 7.5F);
        using var linie = new Pen(Color.Gray, 0.2f);
        string ueber = $"Kalender – {KalenderRegeln.ZeitraumText(ansicht, tag)}" + (nurFuer is null ? "" : $" – {nurFuer}");
        if (ansicht == KalenderAnsicht.Monat)
        {
            const float links = 10f, oben = 10f, breite = 277f, hoehe = 190f;
            g.DrawString(ueber, titel, Brushes.Black, links, oben);
            g.DrawString(KopfBereich.DemoHinweis, klein, Brushes.DimGray, links, oben + 8f);
            var erster = WochenplanRegeln.Montag(new DateOnly(tag.Year, tag.Month, 1));
            float y0 = oben + 14f, spalte = breite / 7f, zeile = (hoehe - 14f - 6f) / 6f;
            for (int i = 0; i < 7; i++) g.DrawString(De.DateTimeFormat.GetDayName(TherapieRegeln.Woche[i]), fett, Brushes.Black, links + i * spalte + 1f, y0);
            for (int w = 0; w < 6; w++)
                for (int i = 0; i < 7; i++)
                {
                    var d = erster.AddDays(w * 7 + i);
                    float x = links + i * spalte, y = y0 + 6f + w * zeile;
                    g.DrawRectangle(linie, x, y, spalte, zeile);
                    g.DrawString(d.Day.ToString(), d.Month == tag.Month ? fett : normal, d.Month == tag.Month ? Brushes.Black : Brushes.Gray, x + 1f, y + 0.5f);
                    float zy = y + 4.5f;
                    foreach (var k in termine.Where(k => k.Am(d)))
                    {
                        if (zy > y + zeile - 3.5f) { g.DrawString("…", klein, Brushes.Black, x + 1f, zy - 1f); break; }
                        g.DrawString(Zeile(k, d), klein, Brushes.Black, new RectangleF(x + 1f, zy, spalte - 2f, 3.6f));
                        zy += 3.4f;
                    }
                }
        }
        else
        {
            const float links = 20f, oben = 15f;
            g.DrawString(ueber, titel, Brushes.Black, links, oben);
            g.DrawString(KopfBereich.DemoHinweis, klein, Brushes.DimGray, links, oben + 8f);
            float y = oben + 15f;
            var montag = WochenplanRegeln.Montag(tag);
            for (int i = 0; i < 7; i++)
            {
                var d = montag.AddDays(i);
                g.DrawString(d.ToString("dddd, dd.MM.", De), fett, Brushes.Black, links, y);
                y += 5.5f;
                var heute = termine.Where(k => k.Am(d)).ToList();
                if (heute.Count == 0) { g.DrawString("–", normal, Brushes.Gray, links + 4f, y); y += 5f; }
                foreach (var k in heute)
                {
                    var r = new RectangleF(links + 4f, y, 166f, 20f);
                    var groesse = g.MeasureString(Zeile(k, d), normal, (int)r.Width);
                    g.DrawString(Zeile(k, d), normal, Brushes.Black, r);
                    y += groesse.Height + 1f;
                }
                g.DrawLine(linie, links, y + 1f, 190f, y + 1f);
                y += 4f;
            }
        }
    }
}

// Teilnehmerliste eines Ausflugs (A4 hoch) zum Abhaken.
public sealed class AusflugDruck(KalenderFassung f, IReadOnlyList<Bewohner> teilnehmer)
{
    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = $"Teilnehmerliste {f.Titel}" };
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f;
        float y = 15f;
        using var titel = new Font("Segoe UI Semibold", 16F);
        using var normal = new Font("Segoe UI", 12F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        g.DrawString($"Ausflug: {f.Titel}", titel, Brushes.Black, links, y);
        y += 10f;
        foreach (var z in new[] { KalenderRegeln.ZeitText(f), f.Treffpunkt.Length > 0 ? $"Treffpunkt: {f.Treffpunkt}" : "", f.Begleitung.Length > 0 ? $"Begleitung: {f.Begleitung}" : "", f.Hinweis })
        {
            if (z.Length == 0) continue;
            g.DrawString(z, normal, Brushes.Black, links, y);
            y += 7f;
        }
        y += 4f;
        g.DrawString("Teilnehmer", normal, Brushes.Black, links, y);
        y += 8f;
        foreach (var b in teilnehmer)
        {
            g.DrawString("☐", normal, Brushes.Black, links, y);
            g.DrawString($"{b.Name}, Zi. {b.Zimmer}", normal, Brushes.Black, links + 8f, y);
            y += 8f;
            g.DrawLine(linie, links, y - 1f, 190f, y - 1f);
        }
        g.DrawString($"{teilnehmer.Count} Teilnehmer · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y + 4f);
    }
}
