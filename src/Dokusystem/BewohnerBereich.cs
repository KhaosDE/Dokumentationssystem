using Dokusystem.Kern;

namespace Dokusystem;

// Bereich „Bewohner“: Übersicht als Kartenraster mit Filterknöpfen, Klick öffnet die Akte.
// Der Kopf (KopfBereich) zeigt je Ansicht passende Texte und Knöpfe; pro Ansicht genau eine
// Hauptaktion in Akzentfarbe.
public sealed class BewohnerBereich : Panel
{
    private readonly Speicher speicher;
    private readonly KopfBereich kopf;
    private readonly Button neuKnopf = Stil.HauptKnopf("Neuer Bewohner …");
    private readonly Button zurueckKnopf = Stil.NebenKnopf("← Alle Bewohner");
    private readonly Button bearbeitenKnopf = Stil.HauptKnopf("Bearbeiten …");

    public KartenRaster Raster { get; } = new() { Dock = DockStyle.Fill };
    public FlowLayoutPanel FilterLeiste { get; } = new()
    {
        Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = Stil.Seite,
        Padding = new Padding(Stil.AbstandGross, Stil.AbstandMini, Stil.AbstandGross, 0),
    };
    public Dictionary<BewohnerFilter, FilterKnopf> FilterKnoepfe { get; } = [];
    // Offene Aufgaben über den Karten (Wohnbereich-ToDo), mit Knopf zu allen.
    public AufgabenKurzliste Aufgaben { get; } = new();
    public NachrichtenHinweis Nachrichten { get; } = new() { Visible = false };
    public event Action<long?>? NachrichtenAnzeigen;
    private readonly Button nachrichtenKnopf = Stil.NebenKnopf("Nachrichten …");
    private readonly Panel aufgabenRahmen = new() { Dock = DockStyle.Top, BackColor = Stil.Seite };
    public event Action? AufgabenAnzeigen;
    public BewohnerFilter AktiverFilter { get; private set; } = BewohnerFilter.Alle;

    // Der heutige Tag (für die Aufenthaltsdauer); zum Testen ersetzbar.
    internal Func<DateOnly> Heute { get; set; } = () => DateOnly.FromDateTime(DateTime.Today);
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public AkteAnsicht Akte { get; }
    // Eigenes Feld statt Akte.Visible: Visible meldet „unsichtbar“, solange das Fenster selbst
    // noch nicht angezeigt ist.
    public bool AkteSichtbar { get; private set; }

    // Zum Testen ersetzbar: öffnet das Formular als Dialog und gibt den gespeicherten Bewohner zurück.
    internal Func<BewohnerFormular, Bewohner?> FormularZeigen { get; set; } =
        f => f.ShowDialog() == DialogResult.OK ? f.Gespeichert : null;

    public BewohnerBereich(Speicher speicher, KopfBereich kopf)
    {
        this.speicher = speicher;
        this.kopf = kopf;
        Akte = new AkteAnsicht(speicher) { Dock = DockStyle.Fill, Visible = false };
        BackColor = Stil.Seite;
        foreach (var filter in Enum.GetValues<BewohnerFilter>())
        {
            var knopf = new FilterKnopf(Kennzeichen.FilterText(filter));
            knopf.Click += (_, _) => FilterSetzen(filter);
            FilterKnoepfe[filter] = knopf;
            FilterLeiste.Controls.Add(knopf);
        }
        aufgabenRahmen.Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein);
        aufgabenRahmen.Controls.Add(Aufgaben);
        aufgabenRahmen.Controls.Add(Nachrichten);
        aufgabenRahmen.Layout += (_, _) =>
        {
            var p = aufgabenRahmen.Padding;
            int breite = Math.Max(100, aufgabenRahmen.ClientSize.Width - p.Horizontal), y = p.Top;
            if (nachrichtenSichtbar)
            {
                Nachrichten.SetBounds(p.Left, y, breite, Nachrichten.Height);
                Nachrichten.PerformLayout();
                y = Nachrichten.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
            Aufgaben.SetBounds(p.Left, y, breite, Aufgaben.Height);
            Aufgaben.PerformLayout();
            int hoehe = Aufgaben.Bottom + p.Bottom;
            if (aufgabenRahmen.Height != hoehe) aufgabenRahmen.Height = hoehe;
        };
        Aufgaben.AlleKnopf.Click += (_, _) => AufgabenAnzeigen?.Invoke();
        Nachrichten.OeffnenKnopf.Click += (_, _) => NachrichtenAnzeigen?.Invoke(null);
        nachrichtenKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) NachrichtenAnzeigen?.Invoke(id); };
        Controls.Add(Raster);
        Controls.Add(Akte);
        Controls.Add(FilterLeiste);
        Controls.Add(aufgabenRahmen);
        Raster.BringToFront();
        Akte.BringToFront();
        neuKnopf.Click += (_, _) => Formular(null);
        zurueckKnopf.Click += (_, _) => UebersichtZeigen();
        bearbeitenKnopf.Click += (_, _) =>
        {
            if (Akte.Bewohner?.Id is long id) Formular(speicher.Bewohner(id));
        };
    }

    public void FilterSetzen(BewohnerFilter filter)
    {
        AktiverFilter = filter;
        UebersichtZeigen();
    }

    // Kopf und Inhalt für die Übersicht: Filterknöpfe mit Zählern, darunter die passenden Karten.
    public void UebersichtZeigen()
    {
        var alle = speicher.AlleBewohner();
        foreach (var (filter, knopf) in FilterKnoepfe)
        {
            knopf.Anzahl = alle.Count(b => Kennzeichen.Passt(b, filter));
            knopf.Aktiv = filter == AktiverFilter;
        }
        var sichtbar = alle.Where(b => Kennzeichen.Passt(b, AktiverFilter)).ToList();
        DateOnly heute = Heute();
        DateTime jetzt = Jetzt();
        string kuerzel = Einstellungen.Laden().Kuerzel;
        Raster.KartenSetzen(sichtbar.Select(b =>
        {
            var karte = new BewohnerKarte(b, heute,
                speicher.UngeleseneUebergaben(b.Id!.Value, kuerzel, jetzt),
                speicher.WichtigeBerichteSeit(b.Id!.Value, jetzt.AddDays(-7)),
                Modulverwaltung.Marken(speicher, b, jetzt));
            karte.Gewaehlt += gewaehlt => AkteZeigen(gewaehlt.Id!.Value);
            return karte;
        }));
        Akte.Visible = false;
        Raster.Visible = FilterLeiste.Visible = aufgabenRahmen.Visible = true;
        AkteSichtbar = false;
        string anzahl = alle.Count == 1 ? "1 Bewohner" : $"{alle.Count} Bewohner";
        kopf.TexteSetzen(AktiverFilter == BewohnerFilter.Alle ? anzahl : $"{sichtbar.Count} von {anzahl}", "Bewohner");
        kopf.KnoepfeSetzen(neuKnopf);
    }

    public void AkteZeigen(long id)
    {
        if (speicher.Bewohner(id) is not Bewohner b)
        {
            UebersichtZeigen();
            return;
        }
        Akte.Zeigen(b);
        Raster.Visible = FilterLeiste.Visible = aufgabenRahmen.Visible = false;
        Akte.Visible = true;
        AkteSichtbar = true;
        kopf.TexteSetzen("Bewohnerakte", b.Name);
        kopf.KnoepfeSetzen(zurueckKnopf, nachrichtenKnopf, bearbeitenKnopf);
    }

    public BewohnerFormular FormularErstellen(Bewohner? b) => new(speicher, b);

    // Hinweis auf ungelesene Nachrichten über den Karten (nur wenn es welche gibt).
    private bool nachrichtenSichtbar;
    public bool NachrichtenHinweisSichtbar => nachrichtenSichtbar;

    public void NachrichtenSetzen(int ungelesen, string? name)
    {
        nachrichtenSichtbar = ungelesen > 0 && name is not null;
        Nachrichten.Visible = nachrichtenSichtbar;
        if (nachrichtenSichtbar) Nachrichten.Setzen(ungelesen, name);
        aufgabenRahmen.PerformLayout();
    }

    public void AufgabenSetzen(IReadOnlyList<Aufgabe> offen, DateTime jetzt)
    {
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        Aufgaben.Setzen(offen, namen, jetzt);
        aufgabenRahmen.PerformLayout();
    }

    private void Formular(Bewohner? b)
    {
        using var formular = FormularErstellen(b);
        if (FormularZeigen(formular) is Bewohner gespeichert) AkteZeigen(gespeichert.Id!.Value);
    }
}

// Die Akte eines Bewohners: Kopfkarte (Bild, Name, Zimmer) und Allergiehinweis immer gut sichtbar
// oben, darunter Reiter für die Teile der Akte. Noch nicht gebaute Reiter zeigen „Kommt in Version …“.
public sealed class AkteAnsicht : Panel
{
    private const int MaxBreite = 880;
    private const int BildGroesse = 96;

    private readonly KartenFlaeche kopfKarte = new();
    private readonly BildFeld bild = new();
    private readonly Label name = new();
    private readonly MarkenFeld zimmer = new("Zimmer", Stil.Zimmer);
    private readonly Label abschnitt = new();
    private readonly KartenFlaeche stammdaten = new();
    private readonly TableLayoutPanel tabelle = new();

    public HinweisFeld Allergie { get; } = new();
    public Reiterleiste Reiter { get; } = new(Bauplan.AktenReiter);
    public PlatzhalterKarte ReiterPlatzhalter { get; } = new() { Visible = false };
    public BerichteAnsicht Berichte { get; }
    public MedikationAnsicht Medikation { get; }
    public WochenplanAnsicht Wochenplan { get; }
    public BiografieAnsicht Biografie { get; }
    public ReflexionAnsicht Reflexion { get; }
    public List<ModulReiter> ModulAnsichten { get; } = [];
    public Label Geburtsdatum { get; } = new();
    public Label Aufnahmedatum { get; } = new();
    public Label Beschlussende { get; } = new();
    public Bewohner? Bewohner { get; private set; }

    private readonly Speicher speicher;

    public AkteAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        Berichte = new BerichteAnsicht(speicher) { Visible = false };
        Medikation = new MedikationAnsicht(speicher) { Visible = false };
        Wochenplan = new WochenplanAnsicht(speicher) { Visible = false };
        Biografie = new BiografieAnsicht(speicher) { Visible = false };
        Reflexion = new ReflexionAnsicht(speicher) { Visible = false };
        AutoScroll = true;
        BackColor = Stil.Seite;
        DoubleBuffered = true;

        name.AutoSize = true;
        name.Font = Stil.Ueberschrift;
        name.ForeColor = Stil.TextNormal;
        name.BackColor = Stil.Flaeche;
        name.UseMnemonic = false;
        kopfKarte.Controls.AddRange([bild, name, zimmer]);

        abschnitt.Text = "STAMMDATEN";
        abschnitt.AutoSize = true;
        abschnitt.Font = Stil.Abschnitt;
        abschnitt.ForeColor = Stil.TextLeise;
        abschnitt.BackColor = Stil.Seite;

        tabelle.ColumnCount = 2;
        tabelle.AutoSize = true;
        tabelle.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        tabelle.BackColor = Stil.Flaeche;
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        foreach (var (beschriftung, wert) in new[] { ("Geburtsdatum", Geburtsdatum), ("Aufnahmedatum", Aufnahmedatum), ("Beschlussende", Beschlussende) })
        {
            tabelle.Controls.Add(new Label
            {
                Text = beschriftung, AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextMittel,
                Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandGross * 2, Stil.AbstandMini),
            });
            wert.AutoSize = true;
            wert.Font = Stil.Fett;
            wert.ForeColor = Stil.TextNormal;
            wert.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            wert.UseMnemonic = false;
            tabelle.Controls.Add(wert);
        }
        stammdaten.Controls.Add(tabelle);

        Reiter.Gewechselt += _ => ReiterZeigen();
        Controls.AddRange([kopfKarte, Allergie, Reiter, abschnitt, stammdaten, ReiterPlatzhalter, Berichte, Medikation, Wochenplan, Biografie, Reflexion]);
        ModuleAktualisieren();
    }

    // Reiter der eingeschalteten Module nach „Wochenplan“; ausgeschaltete verschwinden ganz.
    public void ModuleAktualisieren()
    {
        foreach (var r in ModulAnsichten) { Controls.Remove(r); r.Dispose(); }
        ModulAnsichten.Clear();
        foreach (var m in Modulverwaltung.Eingeschaltet(speicher))
            if (m.ReiterBauen(speicher) is ModulReiter r)
            {
                r.Visible = false;
                ModulAnsichten.Add(r);
                Controls.Add(r);
            }
        var namen = Bauplan.AktenReiter.ToList();
        namen.InsertRange(namen.IndexOf("Wochenplan") + 1, ModulAnsichten.Select(r => r.Titel));
        Reiter.NamenSetzen(namen);
        ReiterZeigen();
    }

    private void ReiterZeigen()
    {
        bool stamm = Reiter.Aktiv == "Stammdaten";
        bool berichte = Reiter.Aktiv == "Berichte";
        bool medikation = Reiter.Aktiv == "Medikation";
        bool wochenplan = Reiter.Aktiv == "Wochenplan";
        bool biografie = Reiter.Aktiv == "Biografie";
        bool reflexion = Reiter.Aktiv == "Reflexion";
        var modul = ModulAnsichten.FirstOrDefault(r => r.Titel == Reiter.Aktiv);
        foreach (var r in ModulAnsichten) r.Visible = r == modul;
        if (modul is not null && Bewohner is not null) modul.Zeigen(Bewohner);
        abschnitt.Visible = stammdaten.Visible = stamm;
        Berichte.Visible = berichte;
        Medikation.Visible = medikation;
        Wochenplan.Visible = wochenplan;
        Biografie.Visible = biografie;
        Reflexion.Visible = reflexion;
        if (berichte && Bewohner is not null) Berichte.Zeigen(Bewohner);
        if (medikation && Bewohner is not null) Medikation.Zeigen(Bewohner);
        if (wochenplan && Bewohner is not null) Wochenplan.Zeigen(Bewohner);
        if (biografie && Bewohner is not null) Biografie.Zeigen(Bewohner);
        if (reflexion && Bewohner is not null) Reflexion.Zeigen(Bewohner);
        ReiterPlatzhalter.Visible = !stamm && !berichte && !medikation && !wochenplan && !biografie && !reflexion && modul is null;
        if (!stamm && Bauplan.Reiter.TryGetValue(Reiter.Aktiv, out var vorhaben))
            ReiterPlatzhalter.Setzen(vorhaben.Version, vorhaben.Beschreibung);
        PerformLayout();
        Invalidate(true);
    }

    private int S(int wert) => Stil.Px(this, wert);

    public void Zeigen(Bewohner b)
    {
        Bewohner = b;
        Reiter.Waehlen("Stammdaten");
        bild.Setzen(Bilder.Pfad(b.Bild), BewohnerRegeln.Initialen(b), b.Nachname);
        name.Text = b.Name;
        zimmer.Text = $"Zimmer {b.Zimmer}";
        AccessibleName = $"Akte von {b.Name}";

        // Allergiehinweis: drei klar verschiedene Darstellungen (Regel 5), immer mit Wort.
        var (marke, gestrichelt) = b.Allergie switch
        {
            AllergieZustand.Eingetragen => (Stil.Beschluss, false),
            AllergieZustand.KeineBekannt => (Stil.Offen, false),
            _ => (Stil.Achtung, true),
        };
        Allergie.Setzen(BewohnerRegeln.AllergieText(b), marke, gestrichelt);

        Geburtsdatum.Text = Datum.Text(b.Geburtsdatum);
        Aufnahmedatum.Text = Datum.Text(b.Aufnahmedatum);
        Beschlussende.Text = BewohnerRegeln.BeschlussText(b);
        bool offen = b.Beschluss == BeschlussZustand.NichtErfasst;
        Beschlussende.ForeColor = offen ? Stil.Achtung.Text : Stil.TextNormal;
        Beschlussende.Font = offen ? Stil.FettKursiv : Stil.Fett;
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Invalidate(true);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = S(Stil.AbstandGross);
        int breite = Math.Min(ClientSize.Width - 2 * rand, S(MaxBreite));
        int x = rand, y = rand + AutoScrollPosition.Y;
        int innen = S(Stil.AbstandGross);

        int bildSeite = S(BildGroesse);
        int kopfHoehe = bildSeite + 2 * innen;
        kopfKarte.Bounds = new Rectangle(x, y, breite, kopfHoehe);
        bild.Bounds = new Rectangle(innen, innen, bildSeite, bildSeite);
        int textX = innen + bildSeite + S(Stil.AbstandGross);
        int blockHoehe = name.PreferredHeight + S(Stil.AbstandKlein) + zimmer.Height;
        int textY = (kopfHoehe - blockHoehe) / 2;
        name.Location = new Point(textX, textY);
        zimmer.Location = new Point(textX, textY + name.PreferredHeight + S(Stil.AbstandKlein));
        y += kopfHoehe + S(Stil.AbstandMittel);

        Allergie.Bounds = new Rectangle(x, y, breite, S(52));
        y += Allergie.Height + S(Stil.AbstandMittel);

        Reiter.Bounds = new Rectangle(x, y, breite, S(40));
        y += Reiter.Height + S(Stil.AbstandGross);

        foreach (Control ansicht in new Control[] { Berichte, Medikation, Wochenplan, Biografie, Reflexion }.Concat(ModulAnsichten))
        {
            if (!ansicht.Visible) continue;
            ansicht.SetBounds(x, y, breite, ansicht.Height);
            ansicht.PerformLayout();
            y += ansicht.Height + rand;
            AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
            return;
        }
        if (ReiterPlatzhalter.Visible)
        {
            ReiterPlatzhalter.Bounds = new Rectangle(x, y, breite, ReiterPlatzhalter.Height);
            y += ReiterPlatzhalter.Height + rand;
            AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
            return;
        }

        abschnitt.Location = new Point(x + S(Stil.AbstandMini), y);
        y += abschnitt.PreferredHeight + S(Stil.AbstandMini);

        var groesse = tabelle.GetPreferredSize(Size.Empty);
        stammdaten.Bounds = new Rectangle(x, y, breite, groesse.Height + 2 * innen);
        tabelle.Location = new Point(innen, innen);
        tabelle.Size = groesse;
        y += stammdaten.Height + rand;
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
    }
}
