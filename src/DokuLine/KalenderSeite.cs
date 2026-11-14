using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Bereich „Kalender“: Monat, Woche, Tag; Filter „alle“ oder ein Bewohner. Termine von Bewohnern,
// des Wohnbereichs, Heimfahrten und Tagesausflüge. Ändern und Absagen sind neue Fassungen.
// Ausdruck der Monats- oder Wochenansicht und der Teilnehmerliste eines Ausflugs.
public sealed class KalenderSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Kalender";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [NeuKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Zeigen();

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
    // Kalender „Bereich“ und „Haus“ – beide, nur Bereich oder nur Haus.
    public ComboBox Kalenderwahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    public KalenderWahl Wahl => (KalenderWahl)Math.Max(0, Kalenderwahl.SelectedIndex);
    public FahrzeugLeiste Fahrzeuge { get; }
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    // Austausch über .ics-Dateien (z. B. mit Outlook); keine Verbindung zu einem Server.
    public Button ExportKnopf { get; } = Stil.NebenKnopf("Als .ics speichern …");
    public Button ImportKnopf { get; } = Stil.NebenKnopf(".ics übernehmen …");
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
        foreach (var c in new Control[] { ZurueckKnopf, ZeitraumText, VorKnopf, HeuteKnopf, Filter, Kalenderwahl, DruckKnopf, ExportKnopf, ImportKnopf })
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
        ExportKnopf.Click += (_, _) => Exportieren();
        ImportKnopf.Click += (_, _) => Importieren();
        // .ics-Datei auf die Seite ziehen (z. B. eine aus dem E-Mail-Programm gespeicherte Einladung).
        foreach (Control ziel in new Control[] { this, Monat, Woche })
        {
            ziel.AllowDrop = true;
            ziel.DragEnter += (_, e) => e.Effect = IcsDatei(e.Data) is not null ? DragDropEffects.Copy : DragDropEffects.None;
            // Erst nach dem Ziehen öffnen: Ein Fenster mitten im Ziehen hält das Programm fest, aus dem gezogen
            // wurde (z. B. den Explorer), bis es geschlossen ist.
            ziel.DragDrop += (_, e) => { if (IcsDatei(e.Data) is string pfad) BeginInvoke(() => Importieren(pfad)); };
        }
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
        bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt);
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
        // Termine von Bewohnern nur aus dem gewählten Bereich; Haus- und Bereichstermine ohne Bewohner stehen immer da.
        var ids = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt)
            .Select(b => b.Id!.Value).ToHashSet();
        bool ImBereich(KalenderTermin k) => k.Aktuell.Bewohner.Count == 0 || k.Aktuell.Bewohner.Any(ids.Contains);
        // Haus-Termine eines anderen Hauses nur, wenn man dessen Bereiche wählen darf (ohne Haus: für alle).
        var hausVon = speicher.HausVonBereich();
        var sichtbar = speicher.SichtbareWohnbereiche(Recht.Kuerzel).Select(w => hausVon.GetValueOrDefault(w)).OfType<string>().ToHashSet();
        var terminHaus = speicher.KalenderHaeuser();
        bool ImHaus(KalenderTermin k) => !k.Aktuell.Haus || !terminHaus.TryGetValue(k.Id, out var h) || sichtbar.Count == 0 || sichtbar.Contains(h);
        var termine = speicher.Kalender(von, bis, BewohnerFilter).Where(k => KalenderRegeln.Passt(k.Aktuell, Wahl) && ImBereich(k) && ImHaus(k)).ToList();
        // HEB-Fristen als Ganztagseintrag im Bereichskalender (nur gezeigt, nicht gespeichert; nur Name und „HEB fällig“).
        if (Wahl != KalenderWahl.Haus)
            termine.AddRange(speicher.HebFristKalender(von, bis).Where(k => ImBereich(k) && (BewohnerFilter is null || k.Aktuell.Bewohner.Contains(BewohnerFilter.Value))));
        return termine;
    }

    private Dictionary<long, string> Namen() => speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);

    public void Aktualisieren()
    {
        foreach (var (a, knopf) in Ansichten) knopf.Aktiv = a == Ansicht;
        Recht.NurAngemeldet(NeuKnopf);   // ohne Anmeldung nur ansehen
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

    private static string Kuerzel => Recht.Kuerzel;

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

    private static string? IcsDatei(IDataObject? daten) =>
        daten?.GetData(DataFormats.FileDrop) is string[] dateien && dateien.Length > 0 && dateien[0].EndsWith(".ics", StringComparison.OrdinalIgnoreCase) ? dateien[0] : null;

    // Die gezeigten Termine (Zeitraum, Bewohner, Bereich/Haus) als .ics-Datei.
    public string? Exportieren()
    {
        var termine = Termine().Where(k => k.Id > 0).ToList();   // ohne HEB-Fristen (nur gezeigt)
        if (termine.Count == 0)
        {
            MessageBox.Show(FindForm(), "In diesem Zeitraum stehen keine Termine.", "Als .ics speichern", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        string vorschlag = $"Kalender {ZeitraumText.Text}.ics".Replace(',', ' ').Replace(':', ' ');
        if (KalenderAustausch.SpeichernUnter(FindForm(), vorschlag) is not string pfad) return null;
        try { KalenderAustausch.Schreiben(pfad, Ics.Schreiben(termine, Namen(), Jetzt(), speicher.IcsKennung())); }
        catch (Exception x) when (x is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(FindForm(), $"Die Datei ließ sich nicht speichern: {x.Message}", "Als .ics speichern", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        return pfad;
    }

    // .ics-Datei wählen (oder hereingezogen), lesen, Vorschau mit Zuordnung, übernehmen.
    public int Importieren(string? pfad = null)
    {
        pfad ??= KalenderAustausch.Oeffnen(FindForm());
        if (pfad is null) return 0;
        List<IcsTermin> gelesen;
        try { gelesen = Ics.Lesen(File.ReadAllText(pfad)); }
        catch (Exception x) when (x is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(FindForm(), $"Die Datei ließ sich nicht lesen: {x.Message}", ".ics übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 0;
        }
        if (gelesen.Count == 0)
        {
            MessageBox.Show(FindForm(), "In der Datei steht kein Termin (VEVENT).", ".ics übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        using var fenster = new IcsImportFenster(speicher, gelesen, Path.GetFileName(pfad));
        if (BewohnerFilter is long id) { fenster.Arten[(int)KalenderArt.Bewohner].Checked = true; fenster.BewohnerAnkreuzen(id); }
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return 0;
        if (fenster.Angelegt.Count > 0) TagWaehlen(DateOnly.FromDateTime(fenster.Angelegt[0].Aktuell.Beginn));
        Geaendert?.Invoke();
        return fenster.Angelegt.Count;
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
