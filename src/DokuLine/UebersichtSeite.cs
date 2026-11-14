using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Startseite: links „Heute wichtig“ mit vier Kästen, rechts die Bewohner des Wohnbereichs (gewählt in der Seitenleiste)
// als Liste; Doppelklick öffnet die Akte. Unter 1100 px steht „Heute wichtig“ über der Liste.
public sealed class UebersichtSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Übersicht";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => Knoepfe;
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    public const int LinksBreite = 380, SchmalAb = 1100;
    private readonly Speicher speicher;
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 60_000 };
    private DateOnly gezeigtAm;
    private SchichtTermin? gezeigtSchicht;
    public Doppelklickschutz Schutz { get; } = new();
    private readonly Panel links = new() { AutoScroll = true, BackColor = Stil.Seite };
    private readonly KartenFlaeche rechts = new();
    public KastenKopf ListenKopf { get; } = new("BEWOHNER") { Cursor = Cursors.Default };

    // „Heute wichtig“; „Medikamentenänderungen“ nur, wenn es welche gibt (dann ganz oben).
    public HeuteKasten Medikation { get; } = new("MEDIKAMENTENÄNDERUNGEN");
    public bool MedikationZeigen { get; private set; }
    // „Fristen“ (Betreuung, Befreiung, Kostenzusage) nur, wenn eine bald endet.
    public HeuteKasten Fristen { get; } = new("FRISTEN");
    public bool FristenZeigen { get; private set; }
    private HeuteKasten[] Kaesten =>
        [.. MedikationZeigen ? [Medikation] : Array.Empty<HeuteKasten>(), .. FristenZeigen ? [Fristen] : Array.Empty<HeuteKasten>(),
         Uebergaben, Termine, Dienst, Nachrichten];
    public HeuteKasten Uebergaben { get; } = new("ÜBERGABEN OFFEN");
    public HeuteKasten Termine { get; } = new("TERMINE HEUTE");
    public HeuteKasten Dienst { get; } = new("LAUFENDER DIENST · NOCH OFFEN");
    public HeuteKasten Nachrichten { get; } = new("NACHRICHTEN UNGELESEN");
    public ProgressBar DienstBalken { get; } = new() { Height = 10, Minimum = 0, Style = ProgressBarStyle.Continuous, AccessibleName = "Erledigte Aufgaben" };
    public Label DienstText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public LinkLabel AlleAufgaben { get; } = new() { Text = "Alle Aufgaben …", AutoSize = true, BackColor = Stil.Flaeche, LinkColor = Stil.Akzent };
    public DienstStand? DienstStand { get; private set; }
    // Maßnahmen im Kasten „Laufender Dienst“ (Bauplan 40): offene zum Abhaken, erledigte einklappbar, „alle“ oder nur die
    // eigenen Bezugsbewohner.
    public List<MassnahmeTermin> MassnahmenTermine { get; private set; } = [];
    public List<MassnahmeKastenZeile> MassnahmenZeilen { get; } = [];
    public bool NurMeineMassnahmen { get; private set; }
    public bool ErledigteMassnahmenZeigen { get; private set; }
    public int NichtAbgeholt { get; private set; }   // Medikamente der laufenden Einnahmezeit (Bauplan 42)

    // Bewohnerliste
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Font = Stil.Standard,
        BorderStyle = BorderStyle.None, ShowItemToolTips = true, AccessibleName = "Bewohnerliste",
    };
    public List<BewohnerZeile> Zeilen { get; private set; } = [];
    public BewohnerZeile? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as BewohnerZeile : null;
    public string ListenKopfText => ListenKopf.Text;
    public TextBox Suche { get; } = new() { Width = 220, PlaceholderText = "Name oder Zimmer suchen", Font = Stil.Standard, AccessibleName = "Bewohner suchen" };
    // Nur die eigenen Bezugsbewohner bzw. Einzeltrainings (aus allen Bereichen).
    public CheckBox MeineBezugsbewohner { get; } = new() { Text = "Meine Bezugsbewohner", AutoSize = true, BackColor = Stil.Flaeche, Font = Stil.Standard };
    public CheckBox MeineEinzeltrainings { get; } = new() { Text = "Meine Einzeltrainings", AutoSize = true, BackColor = Stil.Flaeche, Font = Stil.Standard };
    public Label BereichHinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, Visible = false, UseMnemonic = false };

    // Aktionsleiste
    public Button BerichtNeuKnopf { get; } = Stil.HauptKnopf("Neuer Bericht …");
    public Button AufgabeNeuKnopf { get; } = Stil.NebenKnopf("Neue Aufgabe …");
    public Button NachrichtNeuKnopf { get; } = Stil.NebenKnopf("Neue Nachricht …");
    public Button BewohnerNeuKnopf { get; } = Stil.AufnahmeKnopf("Neuer Bewohner …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public Button[] Knoepfe => [BerichtNeuKnopf, AufgabeNeuKnopf, NachrichtNeuKnopf, BewohnerNeuKnopf, DruckKnopf];

    public event Action<long>? AkteGewuenscht;
    public event Action<long>? BerichtGewuenscht;
    public event Action<Bewohner>? UeberleitungGewuenscht;
    public event Action<Bewohner>? AbgaengigkeitGewuenscht;
    public event Action<Bewohner>? AuskunftGewuenscht;
    public event Action<string>? StelleGewuenscht;          // „Übergabe“, „Tagesplan“, „Aufgaben“
    public event Action<Verlauf>? VerlaufGewuenscht;        // Nachricht öffnen
    public event Action<Aufgabe>? VisitenAufgabeGewuenscht;
    public event Action? AufgabeNeuGewuenscht;
    public event Action? NachrichtNeuGewuenscht;
    public event Action? BewohnerNeuGewuenscht;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public string KopfText { get; private set; } = "";
    public string StatusText { get; private set; } = "";
    public bool Schmal { get; private set; }

    public UebersichtSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var c in new[] { MeineBezugsbewohner, MeineEinzeltrainings }) c.CheckedChanged += (_, _) => Aktualisieren();
        Suche.TextChanged += (_, _) => ListeFuellen();

        Dienst.Oben.AddRange([DienstText, DienstBalken]);
        Dienst.Unten.Add(AlleAufgaben);
        AlleAufgaben.LinkClicked += (_, _) => StelleGewuenscht?.Invoke("Aufgaben");
        Uebergaben.KopfGeklickt += () => StelleGewuenscht?.Invoke("Übergabe");
        Termine.KopfGeklickt += () => StelleGewuenscht?.Invoke("Tagesplan");
        Dienst.KopfGeklickt += () => StelleGewuenscht?.Invoke("Aufgaben");
        Nachrichten.KopfGeklickt += () => StelleGewuenscht?.Invoke("Nachrichten");
        Medikation.KopfGeklickt += () => StelleGewuenscht?.Invoke("Übergabe");
        Medikation.Visible = false;
        Fristen.Visible = false;
        links.Controls.AddRange([Medikation, Fristen, Uebergaben, Termine, Dienst, Nachrichten]);

        foreach (var (titel, breite) in new[] { ("Zimmer", 100), ("Name", 170), ("Bezug", 60), ("Wo", 230), ("Auskunft", 170), ("Maßnahmen", 90), ("Hinweise", 300) })
            Liste.Columns.Add(titel, breite);
        Liste.DoubleClick += (_, _) => AkteOeffnen();
        Liste.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; AkteOeffnen(); } };
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSperren();
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        menue.Items.Add("Akte öffnen", null, (_, _) => AkteOeffnen());
        menue.Items.Add("Neuer Bericht …", null, (_, _) => NeuerBericht());
        menue.Items.Add("Überleitungsbogen …", null, (_, _) => { if (Gewaehlt is BewohnerZeile z) UeberleitungGewuenscht?.Invoke(z.Bewohner); });
        menue.Items.Add("Abgängigkeit melden …", null, (_, _) => { if (Gewaehlt is BewohnerZeile z) AbgaengigkeitGewuenscht?.Invoke(z.Bewohner); });
        menue.Items.Add("Auskunft prüfen …", null, (_, _) => { if (Gewaehlt is BewohnerZeile z) AuskunftGewuenscht?.Invoke(z.Bewohner); });
        Liste.ContextMenuStrip = menue;
        rechts.Controls.AddRange([ListenKopf, Suche, MeineBezugsbewohner, MeineEinzeltrainings, BereichHinweis, Liste]);

        BerichtNeuKnopf.Click += (_, _) => NeuerBericht();
        AufgabeNeuKnopf.Click += (_, _) => AufgabeNeuGewuenscht?.Invoke();
        NachrichtNeuKnopf.Click += (_, _) => NachrichtNeuGewuenscht?.Invoke();
        BewohnerNeuKnopf.Click += (_, _) => BewohnerNeuGewuenscht?.Invoke();
        DruckKnopf.Click += (_, _) => Drucken();

        Controls.Add(links);
        Controls.Add(rechts);
        // Immer aktuell: Datums- und Schichtwechsel (jede Minute prüfen, nur wenn sichtbar neu bauen).
        uhr.Tick += (_, _) => { if (Visible && Veraltet) Aktualisieren(); };
        uhr.Start();
    }

    private List<HeuteFuer> gezeigt = [];
    private List<BewohnerZeile> alleZeilen = [];

    // Neuer Tag oder neue Schicht seit dem letzten Aufbau – der Kasten „Laufender Dienst“ folgt dem Schichtwechsel.
    public bool Veraltet => DateOnly.FromDateTime(Jetzt()) != gezeigtAm || SchichtTermin.Zu(Jetzt()) != gezeigtSchicht;

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        gezeigtAm = heute;
        gezeigtSchicht = SchichtTermin.Zu(jetzt);
        var werte = Einstellungen.Laden();
        string gewaehlt = Arbeitsbereich.Gewaehlt.Length > 0 ? Arbeitsbereich.Gewaehlt : werte.Bereich;
        var jeder = speicher.Heute(jetzt, werte.HebFristMonate);
        // Der gewählte Bereich; „Alle“ und Bereiche ohne eigene Bewohner (z. B. „Therapie“) zeigen alle, mit Hinweis.
        var eigene = jeder.Where(h => h.Bewohner.Wohnbereich == gewaehlt).ToList();
        bool allesZeigen = gewaehlt == BereichswahlRegeln.Alle || eigene.Count == 0;
        var alle = allesZeigen ? jeder : eigene;
        List<string> meine = allesZeigen ? [.. speicher.Wohnbereiche().Select(w => w.Name)] : [gewaehlt];
        var bezuege = speicher.Bezuege();
        bool angemeldet = speicher.MitarbeiterLesen(werte.Kuerzel) is not null;
        MeineBezugsbewohner.Enabled = MeineEinzeltrainings.Enabled = angemeldet;
        bool meineBp = angemeldet && MeineBezugsbewohner.Checked, meineEt = angemeldet && MeineEinzeltrainings.Checked;
        if (meineBp || meineEt)
        {
            bool Meins(Zuordnung? z) => z?.Kuerzel.Equals(werte.Kuerzel, StringComparison.OrdinalIgnoreCase) == true;
            alle = jeder.Where(h => bezuege.GetValueOrDefault(h.Bewohner.Id!.Value) is Bezug b &&
                                    ((meineBp && Meins(b.Bezugspflege)) || (meineEt && Meins(b.Einzeltrainer)))).ToList();
            allesZeigen = true;
        }
        BereichHinweis.Visible = gewaehlt != BereichswahlRegeln.Alle && eigene.Count == 0 && jeder.Count > 0;
        BereichHinweis.Text = $"Für „{gewaehlt}“ gibt es keine eigenen Bewohner – gezeigt werden alle Bereiche.";
        string bereichText = meineBp || meineEt
            ? string.Join(" und ", new[] { meineBp ? "meine Bezugsbewohner" : null, meineEt ? "meine Einzeltrainings" : null }.OfType<string>())
            : gewaehlt;
        gezeigt = alle;
        var ids = alle.Select(h => h.Bewohner.Id!.Value).ToHashSet();
        var offeneUebergaben = speicher.Uebergaben(jetzt).Where(u => u.BewohnerId is null || ids.Contains(u.BewohnerId.Value)).ToList();
        var medikation = speicher.Medikamentenaenderungen(heute).Where(a => ids.Contains(a.BewohnerId)).ToList();
        var medikationGeaendert = medikation.Select(a => a.BewohnerId).ToHashSet();

        // Maßnahmen des laufenden Dienstes für die gezeigten Bewohner (Kasten „Laufender Dienst“ und Spalte der Liste).
        MassnahmenTermine = speicher.MassnahmenImDienst(SchichtTermin.Zu(jetzt), ids);
        var massnahmenJe = MassnahmenTermine.ToLookup(t => t.Massnahme.BewohnerId);

        // Bewohnerliste: Marken wie bisher auf den Karten, dazu Übergabe und Geburtstag; „Wo“ eigene Spalte.
        var status = speicher.GeltendeStatusmeldungen(jetzt);
        var inTherapie = speicher.InTherapie(jetzt);   // Tagesplan
        var mitfahrende = speicher.Mitfahrende(jetzt);  // Fahrten: abwesend während der Fahrt
        var farben = BezugAnzeige.Farben(speicher);
        var abgaengig = speicher.OffeneAbgaengigkeiten();
        int frist = werte.BeschlussFristTage;
        alleZeilen = [];
        foreach (var h in alle)
        {
            long id = h.Bewohner.Id!.Value;
            var modul = Modulverwaltung.Marken(speicher, h.Bewohner, jetzt);
            if (mitfahrende.TryGetValue(id, out var fahrt)) modul = [fahrt, .. modul];
            var marken = UebersichtRegeln.Marken(h, heute, frist);
            // Abwesend nur einmal: Meldet ein Modul schon „abwesend“, entfällt die Heimfahrt-Marke.
            if (modul.Any(m => m.Art == MarkenArt.Abwesend)) marken.RemoveAll(m => m.Art == MarkenArt.Abwesend);
            marken.InsertRange(0, modul.Where(m => m.Art == MarkenArt.Abwesend));
            marken.AddRange(modul.Where(m => m.Art != MarkenArt.Abwesend));
            if (status.TryGetValue(id, out var s)) marken.Insert(0, StatusmeldungRegeln.Marke(s, jetzt));
            if (inTherapie.TryGetValue(id, out var wo)) marken.Insert(0, wo);
            if (medikationGeaendert.Contains(id)) marken.Insert(0, new Marke(MarkenArt.Medikation, MedikationsAenderungen.MarkeText));
            if (abgaengig.TryGetValue(id, out var ab) && !marken.Any(m => m.Art == MarkenArt.Abgaengig)) marken.Insert(0, AbgaengigkeitRegeln.Marke(ab));
            int uebergaben = offeneUebergaben.Count(u => u.BewohnerId == id);
            if (uebergaben > 0) marken.Add(new Marke(MarkenArt.Uebergabe, uebergaben == 1 ? "Übergabe" : $"{uebergaben} Übergaben"));
            if (allesZeigen) marken.Add(new Marke(MarkenArt.Angabe, h.Bewohner.Wohnbereich));
            var bezug = bezuege.GetValueOrDefault(id);
            var zeile = new BewohnerZeile(h.Bewohner, heute, marken,
                bezug?.Bezugspflege is { } bp ? (bp.Kuerzel, BezugAnzeige.Farbe(farben, bp.Kuerzel)) : null,
                bezug?.Einzeltrainer is { } tr ? (BezugRegeln.TrainerText(tr), BezugAnzeige.Farbe(farben, tr.Kuerzel)) : null,
                HeuteWichtigRegeln.Wo(marken, h.Termine, jetzt), h.Auskunft, MassnahmenRegeln.ListenText(massnahmenJe[id]));
            zeile.Gewaehlt += b => AkteGewuenscht?.Invoke(b.Id!.Value);
            alleZeilen.Add(zeile);
        }
        alleZeilen = [.. alleZeilen.OrderBy(z => z.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal).ThenBy(z => z.Bewohner.Bett == ZimmerRegeln.Tuer ? 1 : 0)];
        string kurz = meineBp || meineEt ? bereichText : allesZeigen ? "alle Bereiche" : string.Join(", ", meine);
        ListenKopf.Text = $"BEWOHNER {kurz.ToUpperInvariant()} · {alleZeilen.Count} · {alleZeilen.Count(z => z.ImHaus)} IM HAUS";
        ListeFuellen();

        HeuteFuellen(jetzt, alle, offeneUebergaben, werte.Kuerzel, bezuege);
        MedikationFuellen(medikation, alle);
        FristenFuellen(alle);

        // Kopf: „R2 · 23 Bewohner · 3 Betten frei · Stand 14:32 Uhr“ (freie Betten der gezeigten Wohnbereiche).
        var bereiche = meineBp || meineEt ? [] : allesZeigen ? speicher.Wohnbereiche().Select(w => w.Name).ToList() : meine;
        int frei = bereiche.Sum(b => speicher.FreieBetten(b).Count);
        string betten = bereiche.Count == 0 || !speicher.Wohnbereiche().Any(w => bereiche.Contains(w.Name)) ? ""
            : frei == 1 ? " · 1 Bett frei" : $" · {frei} Betten frei";
        KopfText = $"{bereichText} · {alle.Count} Bewohner{betten} · Stand {jetzt:HH:mm} Uhr";
        StatusText = HeuteWichtigRegeln.StatusText(jetzt, jetzt);
        KnoepfeSperren();
        PerformLayout();
        Geaendert?.Invoke();
    }

    // Die Liste nach der Suche (Name oder Zimmer); die Auswahl bleibt.
    private void ListeFuellen()
    {
        long? vorher = Gewaehlt?.Bewohner.Id;
        string suche = Suche.Text.Trim();
        Zeilen = [.. alleZeilen.Where(z => suche.Length == 0 || z.Bewohner.Name.Contains(suche, StringComparison.CurrentCultureIgnoreCase) ||
                                           z.Bewohner.Zimmer.StartsWith(suche, StringComparison.Ordinal))];
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var z in Zeilen) Liste.Items.Add(z.Eintrag);
        Liste.EndUpdate();
        int index = Zeilen.FindIndex(z => z.Bewohner.Id == vorher);
        if (index >= 0) { Liste.Items[index].Selected = true; Liste.Items[index].Focused = true; Liste.EnsureVisible(index); }
        KnoepfeSperren();
    }

    // Die vier Kästen links.
    // Kasten „Medikamentenänderungen“: je Änderung eine Zeile, rot umrandet; Klick öffnet die Akte.
    private void MedikationFuellen(List<Medikamentenaenderung> liste, List<HeuteFuer> alle)
    {
        var bewohner = alle.ToDictionary(h => h.Bewohner.Id!.Value, h => h.Bewohner);
        Medikation.Setzen(liste.Count, liste.Select(a =>
        {
            string name = bewohner.TryGetValue(a.BewohnerId, out var b) ? $"{b.Name}, Zi. {b.Zimmer}" : "?";
            return Eintrag($"{name}: {a.Medikament}: {a.Was}", $"ab {Datum.Text(a.Ab)} · {a.Arzt}", null,
                () => AkteGewuenscht?.Invoke(a.BewohnerId), Stil.Gefahr);
        }));
        MedikationZeigen = Medikation.Visible = liste.Count > 0;
    }

    // Kasten „Fristen“: je Frist eine Zeile; Klick öffnet die Akte.
    private void FristenFuellen(List<HeuteFuer> alle)
    {
        var zeilen = alle.SelectMany(h => h.Fristen.Select(m => (h.Bewohner, m))).ToList();
        Fristen.Setzen(zeilen.Count, zeilen.Select(x =>
            Eintrag($"{x.Bewohner.Name}, Zi. {x.Bewohner.Zimmer}: {x.m.Text}", null, null, () => AkteGewuenscht?.Invoke(x.Bewohner.Id!.Value))));
        FristenZeigen = Fristen.Visible = zeilen.Count > 0;
    }

    private void HeuteFuellen(DateTime jetzt, List<HeuteFuer> alle, List<Uebergabe> offen, string kuerzel, IReadOnlyDictionary<long, Bezug> bezuege)
    {
        var bewohner = alle.ToDictionary(h => h.Bewohner.Id!.Value, h => h.Bewohner);
        var ids = bewohner.Keys.ToHashSet();

        // 1. Übergaben offen: bleiben stehen, bis sie erledigt sind.
        Uebergaben.Setzen(offen.Count, offen.Select(u =>
        {
            var b = u.BewohnerId is long id ? bewohner.GetValueOrDefault(id) : null;
            return Eintrag(HeuteWichtigRegeln.UebergabeText(u, b), HeuteWichtigRegeln.SeitText(u), null, () => StelleGewuenscht?.Invoke("Übergabe"),
                BerichtZeile.Schichtfarbe(UebergabeRegeln.SchichtVon(u)));
        }));

        // 2. Termine heute aus dem Tagesplan, mit dem Status der Tafel.
        var termine = HeuteWichtigRegeln.Termine(speicher.Tagesplan(DateOnly.FromDateTime(jetzt), alle.Select(h => h.Bewohner)), jetzt);
        Termine.Setzen(termine.Count, termine.Select(t =>
            Eintrag($"{t.Zeit}  {t.Text}", null, (t.StatusText, TafelStil.Status(t.Status)), () => StelleGewuenscht?.Invoke("Tagesplan"))));

        // 3. Laufender Dienst: „6 von 9 erledigt“, Balken, offene Aufgaben zum Abhaken. Die Standardaufgaben der
        // Schicht zuerst anlegen.
        speicher.StandardaufgabenErzeugen(jetzt);
        var aufgaben = speicher.Aufgaben(jetzt, auchErledigte: true, Arbeitsbereich.Gewaehlt);
        var dienst = HeuteWichtigRegeln.Dienst(aufgaben, jetzt);
        DienstStand = dienst;
        DienstText.Text = $"{BerichtRegeln.SchichtText(dienst.Schicht.Schicht)}: {dienst.Text}";
        DienstBalken.Maximum = Math.Max(1, dienst.Gesamt);
        DienstBalken.Value = Math.Min(DienstBalken.Maximum, dienst.Erledigt);
        string? sperre = kuerzel.Length == 0 ? RechteRegeln.NichtAngemeldetText : null;
        var massnahmen = NurMeineMassnahmen
            ? MassnahmenTermine.Where(t => bezuege.GetValueOrDefault(t.Massnahme.BewohnerId)?.Bezugspflege?.Kuerzel.Equals(kuerzel, StringComparison.OrdinalIgnoreCase) == true).ToList()
            : MassnahmenTermine;
        var massnahmenZeilen = MassnahmenKasten(massnahmen, bewohner, jetzt);
        // Medikamentenausgabe (Bauplan 42): wer seine Medikamente der laufenden Einnahmezeit noch nicht abgeholt hat.
        var (ausgabeTag, ausgabeZeit) = AusgabeRegeln.Jetzt(jetzt);
        var nichtAbgeholt = speicher.Ausgabeliste(bewohner.Values, ausgabeTag, ausgabeZeit).Where(z => z.NichtAbgeholt).ToList();
        NichtAbgeholt = nichtAbgeholt.Count;
        if (nichtAbgeholt.Count > 0)
            massnahmenZeilen.Insert(0, Eintrag($"Medikamente {AusgabeRegeln.ZeitText(ausgabeZeit)}: {nichtAbgeholt.Count} noch nicht abgeholt",
                string.Join(", ", nichtAbgeholt.Select(z => z.Bewohner.Name)), ("nicht abgeholt", Stil.Achtung), () => StelleGewuenscht?.Invoke("Medikamentenausgabe")));
        int massnahmenOffen = massnahmen.Count(t => t.Offen);
        Dienst.Setzen(dienst.Offen.Count + massnahmenOffen + NichtAbgeholt, dienst.Offen.Select(a =>
        {
            string name = a.BewohnerId is long id && bewohner.TryGetValue(id, out var b) ? $" ({b.Name})" : "";
            string faellig = AufgabenRegeln.Ueberfaellig(a, jetzt) ? " · überfällig" : "";
            var haken = new CheckBox
            {
                Text = a.Text + name + faellig, AutoSize = false, BackColor = Stil.Flaeche, Font = Stil.Standard, UseMnemonic = false, Tag = a,
                ForeColor = faellig.Length > 0 ? Stil.Beschluss.Text : Stil.TextNormal, Enabled = sperre is null, AccessibleName = $"Erledigt: {a.Text}{name}",
            };
            // Nach dem Abhaken baut sich der Kasten neu auf; der zweite Klick eines Doppelklicks träfe sonst die
            // nächste Aufgabe.
            haken.CheckedChanged += (_, _) =>
            {
                if (!haken.Checked) return;
                if (Schutz.Frei()) BeginInvoke(() => Erledigen(a));
                else haken.Checked = false;
            };
            return (Control)haken;
        }).Concat(massnahmenZeilen), leer: dienst.Gesamt > 0 ? "Alles erledigt" : HeuteWichtigRegeln.NichtsOffen);

        // 4. Nachrichten ungelesen (an den Angemeldeten).
        if (kuerzel.Length == 0) Nachrichten.Setzen(0, [], leer: "Nicht angemeldet");
        else
        {
            var verlaeufe = speicher.Verlaeufe(kuerzel, NachrichtenOrdner.Posteingang);
            var ungelesen = HeuteWichtigRegeln.Ungelesen(verlaeufe, kuerzel);
            // Zähler wie in der Seitenleiste: alle ungelesenen Nachrichten (nicht die Verläufe); je Verlauf steht die
            // neueste da.
            Nachrichten.Setzen(verlaeufe.Sum(v => v.Ungelesen), ungelesen.Select(n =>
            {
                var v = verlaeufe.First(x => x.Id == n.Verlauf);
                return Eintrag(HeuteWichtigRegeln.NachrichtText(n), BerichtRegeln.ZeitText(n.Gesendet), null, () => VerlaufGewuenscht?.Invoke(v));
            }));
        }
    }

    // Der zweite Abschnitt des Kastens: Überschrift mit Stand und Balken, Umschalter, offene Maßnahmen zum Abhaken, darunter
    // die bestätigten (einklappbar).
    private List<Control> MassnahmenKasten(List<MassnahmeTermin> termine, Dictionary<long, Bewohner> bewohner, DateTime jetzt)
    {
        MassnahmenZeilen.Clear();
        if (MassnahmenTermine.Count == 0) return [];
        var teile = new List<Control>
        {
            new Label { Text = MassnahmenRegeln.StandText(termine), AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false },
            new ProgressBar { Height = 10, Minimum = 0, Maximum = Math.Max(1, termine.Count), Value = Math.Min(Math.Max(1, termine.Count), termine.Count(t => !t.Offen)),
                Style = ProgressBarStyle.Continuous, AccessibleName = "Bestätigte Maßnahmen" },
        };
        // Die Knöpfe entstehen bei jedem Aufbau neu (der Kasten entsorgt seine Zeilen); gemerkt wird nur die Wahl.
        var umschalter = new FlowLayoutPanel { AutoSize = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
        foreach (var (text, meine) in new[] { ("alle", false), ("meine Bezugsbewohner", true) })
        {
            var k = new FilterKnopf(text, mitZahl: false) { Aktiv = NurMeineMassnahmen == meine, Margin = new Padding(0, 0, Stil.AbstandKlein, 0) };
            k.Click += (_, _) => MassnahmenFiltern(meine);
            umschalter.Controls.Add(k);
        }
        teile.Add(umschalter);
        string? sperre = Recht.Grund(speicher, Funktionen.MassnahmenBestaetigen);
        string Name(MassnahmeTermin t) => bewohner.GetValueOrDefault(t.Massnahme.BewohnerId)?.Name ?? "";
        foreach (var t in termine.Where(t => t.Offen))
        {
            var zeile = new MassnahmeKastenZeile(t, Name(t), jetzt, sperre);
            zeile.Gewaehlt += MassnahmeBestaetigen;
            MassnahmenZeilen.Add(zeile);
            teile.Add(zeile);
        }
        var erledigt = termine.Where(t => !t.Offen).ToList();
        if (erledigt.Count > 0)
        {
            var link = new LinkLabel
            {
                Text = $"{(ErledigteMassnahmenZeigen ? "▾" : "▸")} Bestätigt ({erledigt.Count})", AutoSize = true, BackColor = Stil.Flaeche, LinkColor = Stil.Akzent,
                UseMnemonic = false,
            };
            link.LinkClicked += (_, _) => ErledigteUmschalten();
            teile.Add(link);
            if (ErledigteMassnahmenZeigen)
                foreach (var t in erledigt)
                {
                    var zeile = new MassnahmeKastenZeile(t, Name(t), jetzt, sperre);
                    MassnahmenZeilen.Add(zeile);
                    teile.Add(zeile);
                }
        }
        return teile;
    }

    public void MassnahmenFiltern(bool nurMeine)
    {
        NurMeineMassnahmen = nurMeine;
        if (IsHandleCreated) BeginInvoke(Aktualisieren);
        else Aktualisieren();
    }

    public void ErledigteUmschalten()
    {
        ErledigteMassnahmenZeigen = !ErledigteMassnahmenZeigen;
        if (IsHandleCreated) BeginInvoke(Aktualisieren);   // der Klick kam aus einem Teil des Kastens, der dabei neu entsteht
        else Aktualisieren();
    }

    // Abhaken einer Maßnahme: „durchgeführt“ sofort (Zeit = Zeitwert), sonst das kleine Fenster mit Grund.
    public void MassnahmeBestaetigen(MassnahmeTermin t, MassnahmeErgebnis ergebnis, bool mitFenster)
    {
        if (!Schutz.Frei()) return;
        if (mitFenster)
        {
            string name = speicher.Bewohner(t.Massnahme.BewohnerId)?.Name ?? "";
            using var fenster = new MassnahmeBestaetigenFenster(speicher, t.Massnahme, t.Dienst, name, ergebnis) { Jetzt = Jetzt };
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
        }
        else
            try { speicher.MassnahmeBestaetigen(t.Massnahme.Id, t.Dienst, ergebnis, "", t.Fassung.Minuten, "", Recht.Kuerzel, Jetzt()); }
            catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Maßnahme bestätigen", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        if (IsHandleCreated) BeginInvoke(Aktualisieren);
        else Aktualisieren();
    }

    // Abhaken im Kasten „Laufender Dienst“: Kürzel und Uhrzeit wie in „Aufgaben“; Visiten-Aufgaben
    // (Facharzttermin) fragen dort nach Datum, Uhrzeit und Ort.
    public void Erledigen(Aufgabe a)
    {
        if (a.VisiteEintragId is not null) { VisitenAufgabeGewuenscht?.Invoke(a); Aktualisieren(); return; }
        try { speicher.AufgabeErledigt(a.Id, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Aufgaben", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    private static HeuteEintrag Eintrag(string text, string? unten, (string Text, Stil.Marke Marke)? status, Action klick, Color? rahmen = null)
    {
        var e = new HeuteEintrag(text, unten, status) { Rahmen = rahmen };
        e.Geklickt += klick;
        return e;
    }

    public void AkteOeffnen() { if (Gewaehlt is { } z) z.Waehlen(); }

    public bool NeuerBericht()
    {
        if (Gewaehlt is not { } z || Stil.Sperrgrund(BerichtNeuKnopf) is not null) return false;
        BerichtGewuenscht?.Invoke(z.Bewohner.Id!.Value);
        return true;
    }

    public bool Waehlen(long bewohnerId)
    {
        int index = Zeilen.FindIndex(z => z.Bewohner.Id == bewohnerId);
        if (index < 0) return false;
        Liste.SelectedIndices.Clear();
        Liste.Items[index].Selected = true;
        Liste.EnsureVisible(index);
        KnoepfeSperren();   // auch ohne Fenstergriff (Ereignis der Liste kommt dann nicht)
        return true;
    }

    // „Neuer Bericht …“ braucht einen gewählten Bewohner; die übrigen Knöpfe die Rechte wie in ihren Bereichen.
    private void KnoepfeSperren()
    {
        string? ohne = Gewaehlt is null ? "Bitte zuerst einen Bewohner in der Liste wählen." : null;
        Stil.Sperren(BerichtNeuKnopf, Recht.Grund(speicher, Funktionen.BerichteSchreiben) ?? ohne);
        Recht.NurAngemeldet(AufgabeNeuKnopf, NachrichtNeuKnopf);
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, BewohnerNeuKnopf);
        if (Liste.ContextMenuStrip is { } menue)
        {
            string? grund = Stil.Sperrgrund(BerichtNeuKnopf);
            menue.Items[1].Enabled = grund is null;
            menue.Items[1].Visible = !RechteRegeln.FehltRecht(grund);
            menue.Items[1].Text = grund is null ? "Neuer Bericht …" : $"Neuer Bericht … – {grund}";
        }
    }

    public void Drucken()
    {
        var druck = new UebersichtDruck(ListenKopfText, KopfText, Kaesten, Zeilen, Jetzt());
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, "Übersicht");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), abstand = Stil.Px(this, Stil.AbstandMittel);
        int breite = Math.Max(300, ClientSize.Width - 2 * rand);
        int oben = rand;
        int hoehe = Math.Max(200, ClientSize.Height - oben - rand);
        Schmal = ClientSize.Width < Stil.Px(this, SchmalAb);
        int linksBreite = Schmal ? breite : Stil.Px(this, LinksBreite);
        // Die Kästen untereinander; im schmalen Fenster über der Liste (höchstens 40 % der Höhe, dann Bildlauf).
        int kastenBreite = linksBreite - SystemInformation.VerticalScrollBarWidth;   // Platz für den Bildlauf
        int y = links.AutoScrollPosition.Y;
        foreach (var k in Kaesten)
        {
            k.SetBounds(0, y, kastenBreite, k.Height);
            k.PerformLayout();
            y = k.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        int inhalt = y - links.AutoScrollPosition.Y;
        if (Schmal)
        {
            int obenHoehe = Math.Min(inhalt, hoehe * 2 / 5);
            links.SetBounds(rand, oben, breite, obenHoehe);
            rechts.SetBounds(rand, links.Bottom + abstand, breite, Math.Max(150, hoehe - obenHoehe - abstand));
        }
        else
        {
            links.SetBounds(rand, oben, linksBreite, hoehe);
            rechts.SetBounds(links.Right + abstand, oben, Math.Max(200, breite - linksBreite - abstand), hoehe);
        }
        links.AutoScrollMinSize = new Size(0, inhalt);

        // Rechts: Kopf mit Zählern, Suche und Filtern, darunter die Liste.
        int innen = Stil.Px(this, Stil.AbstandMittel);
        int rb = rechts.ClientSize.Width - 2 * innen;
        ListenKopf.SetBounds(1, 1, Math.Max(10, rechts.ClientSize.Width - 2), ListenKopf.Hoehe);
        int fx = innen, zeile = ListenKopf.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in new Control[] { Suche, MeineBezugsbewohner, MeineEinzeltrainings })
        {
            c.Location = new Point(fx, zeile + (c is TextBox ? 0 : Stil.Px(this, 3)));
            fx += (c is TextBox ? c.Width : c.PreferredSize.Width) + Stil.Px(this, Stil.AbstandMittel);
        }
        int unter = Suche.Bottom + Stil.Px(this, Stil.AbstandKlein);
        if (BereichHinweis.Visible)
        {
            BereichHinweis.Location = new Point(innen, unter);
            unter = BereichHinweis.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        Liste.SetBounds(innen, unter, Math.Max(100, rb), Math.Max(80, rechts.ClientSize.Height - unter - innen));
        // Die Spalte „Hinweise“ nimmt den Rest der Breite.
        int feste = 0;
        for (int i = 0; i < Liste.Columns.Count - 1; i++) feste += Liste.Columns[i].Width;
        Liste.Columns[^1].Width = Math.Max(Stil.Px(this, 200), Liste.ClientSize.Width - feste);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            uhr.Dispose();
            // Die Knöpfe stehen in der Aktionsleiste; sind sie gerade nicht dort, hier freigeben.
            foreach (var c in Knoepfe.Cast<Control>().Where(c => c.Parent is null)) c.Dispose();
        }
        base.Dispose(disposing);
    }
}
