using DokuLine.Kern;

namespace DokuLine;

// Bereich „Bewohner“ (seit Schritt 26j): links eine Liste der Bewohner des Bereichs (Zimmer/Bett, Name,
// Bezugspflege, Hinweise; sortierbar, Suche, Bereichswahl, Filterknöpfe), rechts die Berichte des
// gewählten Bewohners. Pfeiltasten blättern, Doppelklick oder „Akte öffnen“ öffnet die Akte.
// Der Kopf (KopfBereich) zeigt je Ansicht passende Texte und Knöpfe; pro Ansicht genau eine
// Hauptaktion in Akzentfarbe.
public sealed class BewohnerBereich : Panel
{
    private readonly Speicher speicher;
    private readonly KopfBereich kopf;
    private readonly Button neuKnopf = Stil.NebenKnopf("Neuer Bewohner …");
    public Button BerichtNeuKnopf { get; } = Stil.HauptKnopf("Neuer Bericht …");   // für den gewählten Bewohner
    public Button AkteKnopf { get; } = Stil.NebenKnopf("Akte öffnen");
    private readonly Button zurueckKnopf = Stil.NebenKnopf("← Alle Bewohner");
    private readonly Button bearbeitenKnopf = Stil.HauptKnopf("Bearbeiten …");

    // Schritt 26j: Liste links, Berichte rechts.
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Dock = DockStyle.Fill,
        Font = Stil.Standard, BorderStyle = BorderStyle.None, ShowItemToolTips = true, AccessibleName = "Bewohnerliste",
    };
    public List<BewohnerZeile> Zeilen { get; private set; } = [];
    public BerichteAnsicht Berichte { get; }
    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = Stil.Standard, AccessibleName = "Bereich" };
    public const string AlleBereiche = "alle Bereiche";
    public TextBox Suche { get; } = new() { Width = 200, PlaceholderText = "Name oder Zimmer suchen", Font = Stil.Standard, AccessibleName = "Bewohner suchen" };
    private readonly Panel listenFlaeche = new() { Dock = DockStyle.Fill, BackColor = Stil.Seite };
    // Die Berichte wachsen mit ihrem Inhalt; der Rahmen rollt (0.33.2: vorher abgeschnitten, kein Bildlauf).
    public Panel BerichteRahmen { get; } = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Stil.Seite };
    private readonly Panel links = new() { Dock = DockStyle.Left, Width = 640, BackColor = Stil.Flaeche };   // Spalten 600 px + Bildlauf; mit der Trennlinie verschiebbar
    private int sortSpalte;
    private bool absteigend;
    public BewohnerZeile? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as BewohnerZeile : null;
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
    private readonly Button statusKnopf = Stil.NebenKnopf("Status …");
    private readonly Button berichtKnopf = Stil.NebenKnopf("Neuer Bericht …");
    public Button AerztlichKnopf { get; } = Stil.NebenKnopf("Ärztliche Informationen …");   // Schritt 26f

    // „Neuer Bericht …“ für einen Bewohner (Akte oder Rechtsklick auf eine Karte); das Hauptfenster öffnet ihn.
    public event Action<long>? BerichtGewuenscht;

    // „Bezug ändern …“ in der Akte (Schritt 20).
    public bool BezugOeffnen(long bewohnerId)
    {
        if (speicher.Bewohner(bewohnerId) is not Bewohner b) return false;
        using var fenster = new BezugFenster(speicher, b);
        bool gespeichert = Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK;
        Akte.NeuLaden();
        return gespeichert;
    }
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
        Akte.BezugGewuenscht += id => BezugOeffnen(id);
        BackColor = Stil.Seite;
        foreach (var filter in Enum.GetValues<BewohnerFilter>())
        {
            var knopf = new FilterKnopf(Kennzeichen.FilterText(filter));
            knopf.Click += (_, _) => FilterSetzen(filter);
            FilterKnoepfe[filter] = knopf;
            FilterLeiste.Controls.Add(knopf);
        }
        // Bereichswahl und Suche (Schritt 26j).
        BereichWahl.Items.Add(UebersichtSeite.EigeneBereiche);
        BereichWahl.Items.Add(AlleBereiche);
        foreach (var w in speicher.Wohnbereiche()) BereichWahl.Items.Add(w.Name);
        BereichWahl.SelectedIndex = 0;
        BereichWahl.SelectedIndexChanged += (_, _) => { if (!abgleichen) UebersichtZeigen(); };
        Suche.TextChanged += (_, _) => UebersichtZeigen();
        foreach (var c in new Control[] { BereichWahl, Suche }) c.Margin = new Padding(Stil.AbstandMittel, Stil.AbstandKlein, 0, 0);
        FilterLeiste.Controls.AddRange([BereichWahl, Suche]);

        foreach (var (titel, breite) in new[] { ("Zimmer", 100), ("Name", 180), ("Bezug", 70), ("Hinweise", 250) }) Liste.Columns.Add(titel, breite);
        Liste.ColumnClick += (_, e) => Sortieren(e.Column);
        Liste.SelectedIndexChanged += (_, _) => BerichteZeigen();
        Liste.DoubleClick += (_, _) => AkteOeffnen();
        Liste.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; AkteOeffnen(); } };
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        menue.Items.Add("Akte öffnen", null, (_, _) => AkteOeffnen());
        menue.Items.Add("Neuer Bericht …", null, (_, _) => NeuerBericht());
        Liste.ContextMenuStrip = menue;
        Berichte = new BerichteAnsicht(speicher);
        Berichte.NeuKnopf.Visible = false;   // „Neuer Bericht …“ steht in der Aktionsleiste
        BerichteRahmen.Padding = new Padding(Stil.AbstandGross, 0, Stil.AbstandGross, 0);
        BerichteRahmen.Controls.Add(Berichte);
        BerichteRahmen.Layout += (_, _) =>
        {
            var p = BerichteRahmen.Padding;
            int breite = Math.Max(100, BerichteRahmen.ClientSize.Width - p.Horizontal);
            Berichte.SetBounds(p.Left + BerichteRahmen.AutoScrollPosition.X, p.Top + BerichteRahmen.AutoScrollPosition.Y, breite, Berichte.Height);
        };
        links.Controls.Add(Liste);
        listenFlaeche.Controls.Add(BerichteRahmen);
        listenFlaeche.Controls.Add(new Splitter { Dock = DockStyle.Left, Width = 6, BackColor = Stil.Seite });
        listenFlaeche.Controls.Add(links);
        listenFlaeche.Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, 0, 0);
        BerichtNeuKnopf.Click += (_, _) => NeuerBericht();
        AkteKnopf.Click += (_, _) => AkteOeffnen();
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
        statusKnopf.Click += (_, _) => StatusOeffnen();
        AerztlichKnopf.Click += (_, _) => AerztlichOeffnen();
        berichtKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) BerichtGewuenscht?.Invoke(id); };
        Controls.Add(listenFlaeche);
        Controls.Add(Akte);
        Controls.Add(FilterLeiste);
        Controls.Add(aufgabenRahmen);
        listenFlaeche.BringToFront();
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

    // Kopf und Inhalt der Liste: Filterknöpfe mit Zählern (im gewählten Bereich), darunter die Bewohner;
    // die Auswahl bleibt, sonst ist der erste gewählt und rechts stehen seine Berichte.
    private bool abgleichen;

    public void UebersichtZeigen()
    {
        // Zurück aus der Akte: deren Bewohner bleibt gewählt (0.33.2: vorher der zuletzt in der Liste gewählte).
        long? vorher = (AkteSichtbar ? Akte.Bewohner?.Id : null) ?? Gewaehlt?.Bewohner.Id;
        abgleichen = true;
        try { Auswahl.Abgleichen(BereichWahl, [UebersichtSeite.EigeneBereiche, AlleBereiche, .. speicher.Wohnbereiche().Select(w => w.Name)]); }
        finally { abgleichen = false; }
        var werte = Einstellungen.Laden();
        var alle = speicher.AlleBewohner();
        var imBereich = BereichFiltern(alle, werte, out string bereichText);
        foreach (var (filter, knopf) in FilterKnoepfe)
        {
            knopf.Anzahl = imBereich.Count(b => Kennzeichen.Passt(b, filter));
            knopf.Aktiv = filter == AktiverFilter;
        }
        string suche = Suche.Text.Trim();
        var sichtbar = imBereich.Where(b => Kennzeichen.Passt(b, AktiverFilter) &&
            (suche.Length == 0 || b.Name.Contains(suche, StringComparison.CurrentCultureIgnoreCase) || b.Zimmer.StartsWith(suche, StringComparison.Ordinal))).ToList();
        DateOnly heute = Heute();
        DateTime jetzt = Jetzt();
        string kuerzel = werte.Kuerzel;
        var status = speicher.GeltendeStatusmeldungen(jetzt);
        var inTherapie = speicher.InTherapie(jetzt);   // Tagesplan (Schritt 23): „in Therapie: … bis …“
        var mitfahrende = speicher.Mitfahrende(jetzt);  // Fahrten (Schritt 24): „abwesend: unterwegs mit …“
        var bezuege = speicher.Bezuege();
        var farben = BezugAnzeige.Farben(speicher);
        var zeilen = sichtbar.Select(b =>
        {
            long id = b.Id!.Value;
            // Wie auf den Karten: Statusmeldung vorne; Bewohner eines anderen Wohnbereichs hinten gekennzeichnet.
            IReadOnlyList<Marke> marken =
            [
                .. mitfahrende.TryGetValue(id, out var fahrt) ? [fahrt] : Array.Empty<Marke>(),
                .. inTherapie.TryGetValue(id, out var wo) ? [wo] : Array.Empty<Marke>(),
                .. status.TryGetValue(id, out var s) ? [StatusmeldungRegeln.Marke(s, jetzt)] : Array.Empty<Marke>(),
                .. Kennzeichen.Marken(b, speicher.UngeleseneUebergaben(id, kuerzel, jetzt), speicher.WichtigeBerichteSeit(id, jetzt.AddDays(-7))),
                .. Modulverwaltung.Marken(speicher, b, jetzt),
                .. b.Wohnbereich != werte.Bereich ? [new Marke(MarkenArt.Angabe, b.Wohnbereich)] : Array.Empty<Marke>(),
            ];
            var bezug = bezuege.GetValueOrDefault(id);
            var zeile = new BewohnerZeile(b, heute, marken,
                bezug?.Bezugspflege is { } bp ? (bp.Kuerzel, BezugAnzeige.Farbe(farben, bp.Kuerzel)) : null,
                bezug?.Einzeltrainer is { } tr ? (BezugRegeln.TrainerText(tr), BezugAnzeige.Farbe(farben, tr.Kuerzel)) : null);
            zeile.Gewaehlt += gewaehlt => AkteZeigen(gewaehlt.Id!.Value);
            return zeile;
        });
        Zeilen = Sortiert(zeilen);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var z in Zeilen) Liste.Items.Add(z.Eintrag);
        Liste.EndUpdate();
        int index = Zeilen.FindIndex(z => z.Bewohner.Id == vorher);
        if (index < 0 && Zeilen.Count > 0) index = 0;
        if (index >= 0) { Liste.Items[index].Selected = true; Liste.Items[index].Focused = true; Liste.EnsureVisible(index); }
        BerichteZeigen();
        BerichteRahmen.AutoScrollPosition = Point.Empty;
        Akte.Visible = false;
        listenFlaeche.Visible = FilterLeiste.Visible = aufgabenRahmen.Visible = true;
        AkteSichtbar = false;
        string anzahl = imBereich.Count == 1 ? "1 Bewohner" : $"{imBereich.Count} Bewohner";
        kopf.TexteSetzen($"{bereichText} · " + (sichtbar.Count == imBereich.Count ? anzahl : $"{sichtbar.Count} von {anzahl}"), "Bewohner");
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, neuKnopf);
        kopf.KnoepfeSetzen(BerichtNeuKnopf, AkteKnopf, neuKnopf);
    }

    // Eigene Bereiche wie in der Übersicht (Wohnbereiche des Angemeldeten, sonst der des Rechners; ohne
    // eigene Bewohner alle), „alle Bereiche“ oder ein gewählter Wohnbereich.
    private List<Bewohner> BereichFiltern(List<Bewohner> alle, EinstellungsWerte werte, out string text)
    {
        string? wahl = BereichWahl.SelectedItem as string;
        if (wahl == AlleBereiche) { text = AlleBereiche; return alle; }
        if (wahl is not null && wahl != UebersichtSeite.EigeneBereiche) { text = wahl; return alle.Where(b => b.Wohnbereich == wahl).ToList(); }
        var meine = speicher.MitarbeiterLesen(werte.Kuerzel)?.Bereiche.Where(b => alle.Any(x => x.Wohnbereich == b)).ToList()
            is { Count: > 0 } liste ? liste : [werte.Bereich];
        var eigene = alle.Where(b => meine.Contains(b.Wohnbereich)).ToList();
        if (eigene.Count == 0) { text = AlleBereiche; return alle; }
        text = string.Join(", ", meine);
        return eigene;
    }

    // Sortieren per Klick auf den Spaltenkopf; noch einmal klicken dreht die Reihenfolge um.
    public void Sortieren(int spalte)
    {
        absteigend = spalte == sortSpalte && !absteigend;
        sortSpalte = spalte;
        UebersichtZeigen();
    }

    private List<BewohnerZeile> Sortiert(IEnumerable<BewohnerZeile> zeilen)
    {
        var sortiert = sortSpalte switch
        {
            1 => zeilen.OrderBy(z => z.Bewohner.Nachname, StringComparer.CurrentCulture).ThenBy(z => z.Bewohner.Vorname, StringComparer.CurrentCulture),
            2 => zeilen.OrderBy(z => z.Bezugspflege?.Kuerzel ?? "\uffff", StringComparer.Ordinal).ThenBy(z => z.Bewohner.Nachname, StringComparer.CurrentCulture),
            3 => zeilen.OrderByDescending(z => z.Marken.Count).ThenBy(z => z.Bewohner.Nachname, StringComparer.CurrentCulture),
            _ => zeilen.OrderBy(z => z.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal).ThenBy(z => z.Bewohner.Bett == ZimmerRegeln.Tuer ? 1 : 0),
        };
        var liste = sortiert.ToList();
        if (absteigend) liste.Reverse();
        return liste;
    }

    public bool Waehlen(long bewohnerId)
    {
        int index = Zeilen.FindIndex(z => z.Bewohner.Id == bewohnerId);
        if (index < 0) return false;
        Liste.SelectedIndices.Clear();
        Liste.Items[index].Selected = true;
        Liste.EnsureVisible(index);
        return true;
    }

    // Rechts die Berichte des gewählten Bewohners; ohne Auswahl sind „Neuer Bericht …“ und „Akte öffnen“ grau.
    private void BerichteZeigen()
    {
        var z = Gewaehlt;
        Berichte.Visible = z is not null;
        if (z is not null) Berichte.Zeigen(z.Bewohner);
        string? ohne = z is null ? "Bitte zuerst einen Bewohner wählen." : null;
        Stil.Sperren(AkteKnopf, ohne);
        Stil.Sperren(BerichtNeuKnopf, ohne ?? Recht.Grund(speicher, Funktionen.BerichteSchreiben));
        if (Liste.ContextMenuStrip is { } menue)
        {
            menue.Items[1].Enabled = Stil.Sperrgrund(BerichtNeuKnopf) is null;
            menue.Items[1].Text = Stil.Sperrgrund(BerichtNeuKnopf) is string grund ? $"Neuer Bericht … – {grund}" : "Neuer Bericht …";
        }
    }

    public void AkteOeffnen() { if (Gewaehlt is { } z) z.Waehlen(); }

    // „Neuer Bericht …“ für den gewählten Bewohner: bleibt in der Liste, rechts erscheint er.
    public bool NeuerBericht()
    {
        if (Gewaehlt is null || Stil.Sperrgrund(BerichtNeuKnopf) is not null) return false;
        Berichte.NeuerBericht();
        UebersichtZeigen();   // Marken (z. B. „Wichtig“, „Übergabe“) neu
        return true;
    }

    // Rechte des Angemeldeten auf die Knöpfe im Kopf der Akte (Schritt 19).
    private void AkteKnoepfeSperren()
    {
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, bearbeitenKnopf);
        Recht.Anwenden(speicher, Funktionen.BerichteSchreiben, berichtKnopf);
        Recht.Anwenden(speicher, Funktionen.AerztlicheAnsehen, AerztlichKnopf);
    }

    // Mehrplatz: die gezeigte Ansicht neu lesen (Karten oder Kopf und Stammdaten der Akte).
    public void NeuLesen()
    {
        if (!AkteSichtbar) { UebersichtZeigen(); return; }
        if (Akte.Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) { UebersichtZeigen(); return; }
        Akte.NeuLaden();
        kopf.TexteSetzen("Bewohner", "Bewohnerakte");   // Schritt 26d: der Name steht nur einmal, auf der Karte darunter
        AkteKnoepfeSperren();
    }

    public void AkteZeigen(long id)
    {
        if (speicher.Bewohner(id) is not Bewohner b)
        {
            UebersichtZeigen();
            return;
        }
        Akte.Zeigen(b);
        listenFlaeche.Visible = FilterLeiste.Visible = aufgabenRahmen.Visible = false;
        Akte.Visible = true;
        AkteSichtbar = true;
        kopf.TexteSetzen("Bewohner", "Bewohnerakte");   // Schritt 26d: der Name steht nur einmal, auf der Karte darunter
        AkteKnoepfeSperren();
        kopf.KnoepfeSetzen(zurueckKnopf, nachrichtenKnopf, statusKnopf, AerztlichKnopf, berichtKnopf, bearbeitenKnopf);
    }

    // Statusmeldung setzen oder aufheben (Mehrplatzbetrieb); danach die Akte neu lesen.
    public bool StatusOeffnen()
    {
        if (Akte.Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) return false;
        using var fenster = new StatusmeldungFenster(speicher, b);
        bool gespeichert = Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK;
        Akte.NeuLaden();
        return gespeichert;
    }

    // Schritt 26f: Ärztliche Informationen (Recht „ansehen“); danach die Akte neu (Allergiehinweis).
    public bool AerztlichOeffnen()
    {
        if (Recht.Grund(speicher, Funktionen.AerztlicheAnsehen) is not null) return false;
        if (Akte.Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) return false;
        using var fenster = new AerztlichFenster(speicher, b);
        Dialog.Zeigen(fenster, FindForm());
        Akte.NeuLaden();
        return fenster.Geaendert;
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

    // „Neuer Bewohner …“ aus der Übersicht (Schritt 26l): Formular; der gespeicherte Bewohner oder null.
    public Bewohner? NeuerBewohner()
    {
        if (Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return null;
        using var formular = FormularErstellen(null);
        return FormularZeigen(formular);
    }

    private void Formular(Bewohner? b)
    {
        using var formular = FormularErstellen(b);
        if (FormularZeigen(formular) is Bewohner gespeichert) AkteZeigen(gespeichert.Id!.Value);
        else if (b?.Id is long id) AkteZeigen(id);   // neu lesen: ein anderer Arbeitsplatz kann inzwischen gespeichert haben
    }
}

// Die Akte eines Bewohners: Kopfkarte (Bild, Name, Zimmer) und Allergiehinweis immer gut sichtbar
// oben, darunter Reiter für die Teile der Akte. Noch nicht gebaute Reiter zeigen „Kommt in Version …“.
public sealed class AkteAnsicht : Panel
{
    private const int BildGroesse = 96;

    private readonly KartenFlaeche kopfKarte = new();
    private readonly BildFeld bild = new();
    private readonly Label name = new();
    private readonly MarkenFeld zimmer = new("Zimmer", Stil.Zimmer) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };
    private readonly FlowLayoutPanel kopfMarken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
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
    public HebAnsicht Heb { get; }
    public List<ModulReiter> ModulAnsichten { get; } = [];
    public Label Geburtsdatum { get; } = new();
    public Label Aufnahmedatum { get; } = new();
    public Label Beschlussende { get; } = new();
    public Label OffenGefuehrt { get; } = new();
    public Label Wohnbereich { get; } = new();
    public Label Alter { get; } = new();
    // Bezugspflege und Einzeltrainer (Schritt 20): Farbpunkt der Person, Name und Kürzel, seit wann.
    public Label Bezugspflege { get; } = new();
    public Label Einzeltrainer { get; } = new();
    private readonly FarbPunkt bezugPunkt = new(), trainerPunkt = new();
    public Button BezugKnopf { get; } = Stil.NebenKnopf("Bezug ändern …");
    public Button BezugVerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    private readonly FlowLayoutPanel bezugKnoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    public event Action<long>? BezugGewuenscht;
    // Betreuer (Schritt 28): Name, Stelle, Telefon, E-Mail; „E-Mail an Betreuer …“ öffnet das E-Mail-Programm.
    public Label BetreuerText { get; } = new();
    public Button BetreuerKnopf { get; } = Stil.NebenKnopf("Betreuer …");
    public Button EmailKnopf { get; } = Stil.NebenKnopf("E-Mail an Betreuer …");
    // Zum Testen ersetzbar: Hinweis vor dem Öffnen des E-Mail-Programms; true = weiter.
    internal Func<string, bool> EmailBestaetigen { get; set; }

    // Marken im Kopf der Akte: Statusmeldung (lang, mit Bereich, Kürzel und Zeit), Zimmer, „offen geführt“,
    // Hinweise der Module (z. B. Widersprüche).
    public IEnumerable<string> KopfMarken => kopfMarken.Controls.OfType<MarkenFeld>().Select(m => m.Text);
    public Bewohner? Bewohner { get; private set; }
    public string? StatusText { get; private set; }   // die geltende Statusmeldung, wie sie im Kopf steht
    public string? WoText { get; private set; }       // „in Therapie: … bis …“ (Tagesplan) oder „abwesend: unterwegs mit …“ (Fahrt)

    private readonly Speicher speicher;

    public AkteAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        Berichte = new BerichteAnsicht(speicher) { Visible = false };
        Medikation = new MedikationAnsicht(speicher) { Visible = false };
        Wochenplan = new WochenplanAnsicht(speicher) { Visible = false };
        Biografie = new BiografieAnsicht(speicher) { Visible = false };
        Reflexion = new ReflexionAnsicht(speicher) { Visible = false };
        Heb = new HebAnsicht(speicher) { Visible = false };
        AutoScroll = true;
        BackColor = Stil.Seite;
        DoubleBuffered = true;

        name.AutoSize = true;
        name.Font = Stil.Ueberschrift;
        name.ForeColor = Stil.TextNormal;
        name.BackColor = Stil.Flaeche;
        name.UseMnemonic = false;
        kopfMarken.Controls.Add(zimmer);
        kopfKarte.Controls.AddRange([bild, name, kopfMarken]);

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
        foreach (var (beschriftung, wert) in new[]
                 {
                     ("Geburtsdatum", Geburtsdatum), ("Aufnahmedatum", Aufnahmedatum), ("Beschlussende", Beschlussende),
                     ("Offen geführt", OffenGefuehrt), ("Wohnbereich", Wohnbereich),
                     ("Bezugspflege", Bezugspflege), ("Einzeltrainer", Einzeltrainer), ("Betreuer", BetreuerText),
                 })
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
            if (wert == Bezugspflege || wert == Einzeltrainer)
            {
                var punkt = wert == Bezugspflege ? bezugPunkt : trainerPunkt;
                punkt.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0);
                var mitPunkt = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
                mitPunkt.Controls.AddRange([punkt, wert]);
                tabelle.Controls.Add(mitPunkt);
                continue;
            }
            if (wert != Geburtsdatum)
            {
                tabelle.Controls.Add(wert);
                continue;
            }
            // Das Alter steht neben dem Geburtsdatum.
            Alter.AutoSize = true;
            Alter.Font = Stil.Standard;
            Alter.ForeColor = Stil.TextMittel;
            Alter.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandMini, 0, Stil.AbstandMini);
            var zelle = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
            zelle.Controls.AddRange([wert, Alter]);
            tabelle.Controls.Add(zelle);
        }
        stammdaten.Controls.Add(tabelle);
        foreach (var k in new[] { BezugKnopf, BezugVerlaufKnopf, BetreuerKnopf, EmailKnopf }) { k.AutoSize = true; k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini); }
        bezugKnoepfe.Controls.AddRange([BezugKnopf, BezugVerlaufKnopf, BetreuerKnopf, EmailKnopf]);
        BetreuerKnopf.Click += (_, _) => BetreuerBearbeiten();
        EmailKnopf.Click += (_, _) => EmailSchreiben();
        EmailBestaetigen = text => MessageBox.Show(FindForm(), text, "E-Mail an Betreuer", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        stammdaten.Controls.Add(bezugKnoepfe);
        BezugKnopf.Click += (_, _) => { if (Bewohner?.Id is long id) BezugGewuenscht?.Invoke(id); };
        BezugVerlaufKnopf.Click += (_, _) => BezugVerlaufZeigen();

        Reiter.Gewechselt += _ => ReiterZeigen();
        Controls.AddRange([kopfKarte, Allergie, Reiter, abschnitt, stammdaten, ReiterPlatzhalter, Berichte, Medikation, Wochenplan, Biografie, Reflexion, Heb]);
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

    // Nach einem Benutzerwechsel: den offenen Reiter neu zeigen (gesperrte Knöpfe nach den neuen Rechten).
    public void AnsichtNeu() => ReiterZeigen();

    private void ReiterZeigen()
    {
        bool stamm = Reiter.Aktiv == "Stammdaten";
        bool berichte = Reiter.Aktiv == "Berichte";
        bool medikation = Reiter.Aktiv == "Medikamentenplan";
        bool wochenplan = Reiter.Aktiv == "Wochenplan";
        bool biografie = Reiter.Aktiv == "Biografie";
        bool reflexion = Reiter.Aktiv == "Reflexion";
        bool heb = Reiter.Aktiv == "HEB";
        var modul = ModulAnsichten.FirstOrDefault(r => r.Titel == Reiter.Aktiv);
        foreach (var r in ModulAnsichten) r.Visible = r == modul;
        if (modul is not null && Bewohner is not null) modul.Zeigen(Bewohner);
        abschnitt.Visible = stammdaten.Visible = stamm;
        Berichte.Visible = berichte;
        Medikation.Visible = medikation;
        Wochenplan.Visible = wochenplan;
        Biografie.Visible = biografie;
        Reflexion.Visible = reflexion;
        Heb.Visible = heb;
        if (berichte && Bewohner is not null) Berichte.Zeigen(Bewohner);
        if (medikation && Bewohner is not null) Medikation.Zeigen(Bewohner);
        if (wochenplan && Bewohner is not null) Wochenplan.Zeigen(Bewohner);
        if (biografie && Bewohner is not null) Biografie.Zeigen(Bewohner);
        if (reflexion && Bewohner is not null) Reflexion.Zeigen(Bewohner);
        if (heb && Bewohner is not null) Heb.Zeigen(Bewohner);
        ReiterPlatzhalter.Visible = !stamm && !berichte && !medikation && !wochenplan && !biografie && !reflexion && !heb && modul is null;
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
        Fuellen(b);
    }

    // Mehrplatz: Kopf und Stammdaten neu lesen, ohne den Reiter zu wechseln (z. B. nach 30 Sekunden,
    // wenn ein anderer Arbeitsplatz etwas gespeichert hat). Die anderen Reiter lesen beim Wechsel neu.
    public void NeuLaden()
    {
        if (Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) return;
        Bewohner = b;
        var scroll = AutoScrollPosition;
        Fuellen(b);
        AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
    }

    // Bezugspflege und Einzeltrainer mit Farbe der Person; „keine“ grau.
    private void BezugZeigen(Bewohner b)
    {
        var bezug = speicher.Bezug(b.Id!.Value);
        var farben = BezugAnzeige.Farben(speicher);
        var namen = speicher.AlleMitarbeiter().ToDictionary(m => m.Kuerzel, m => m.Anzeige, StringComparer.OrdinalIgnoreCase);
        void Zeile(Label text, FarbPunkt punkt, Zuordnung? z, string keine, string vorne)
        {
            text.Text = z is null ? keine : $"{vorne}{namen.GetValueOrDefault(z.Kuerzel, z.Kuerzel)} · seit {Datum.Text(z.Ab)}";
            text.ForeColor = z is null ? Stil.TextLeise : Stil.TextNormal;
            punkt.Farbe = z is null ? null : BezugAnzeige.Farbe(farben, z.Kuerzel);
        }
        Zeile(Bezugspflege, bezugPunkt, bezug.Bezugspflege, "keine", "");
        Zeile(Einzeltrainer, trainerPunkt, bezug.Einzeltrainer, "keiner", bezug.Einzeltrainer is { } t ? $"{t.Art} · " : "");
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, BezugKnopf);
    }

    // Betreuer: „nicht erfasst“ in Achtung-Farbe (leer heißt nicht „keiner“, Regel 5).
    private void BetreuerZeigen(Bewohner b)
    {
        var betreuer = speicher.Betreuer(b.Id!.Value);
        BetreuerText.Text = BetreuerRegeln.Text(betreuer);
        BetreuerText.ForeColor = betreuer is null ? Stil.Achtung.Text : Stil.TextNormal;
        BetreuerText.Font = betreuer is null ? Stil.FettKursiv : Stil.Fett;
        BetreuerText.MaximumSize = new Size(S(560), 0);
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, BetreuerKnopf);
        Stil.Sperren(EmailKnopf, betreuer is null ? "Kein Betreuer eingetragen."
            : !BetreuerRegeln.EmailGueltig(betreuer.Email) ? "Für den Betreuer ist keine E-Mail-Adresse eingetragen." : null);
    }

    public void BetreuerBearbeiten()
    {
        if (Bewohner is null || Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return;
        using var fenster = new BetreuerFenster(speicher, Bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) BetreuerZeigen(Bewohner);
        PerformLayout();
    }

    // Erst der Hinweis, dann das E-Mail-Programm mit Empfänger und Betreff (mailto:). Das Programm
    // selbst sendet nichts; Text und Versand macht der Mensch im E-Mail-Programm.
    public bool EmailSchreiben()
    {
        if (Bewohner?.Id is not long id || speicher.Betreuer(id) is not Betreuer b || !BetreuerRegeln.EmailGueltig(b.Email)) return false;
        if (!EmailBestaetigen($"{BetreuerRegeln.EmailHinweis}\n\nDas E-Mail-Programm öffnet sich mit Empfänger und Betreff. Text und Versand machen Sie dort."))
            return false;
        Extern.Oeffnen(BetreuerRegeln.Mailto(b, BetreuerRegeln.Betreff(Bewohner)));
        return true;
    }

    public void BezugVerlaufZeigen()
    {
        if (Bewohner?.Id is not long id) return;
        var namen = speicher.AlleMitarbeiter().ToDictionary(m => m.Kuerzel, m => m.Name, StringComparer.OrdinalIgnoreCase);
        var verlauf = BezugRegeln.Verlauf(speicher.Bezugspflegen(id), speicher.Einzeltrainer(id), namen);
        using var fenster = new TextFenster($"Bezugspflege und Einzeltrainer – {Bewohner.Name}",
            verlauf.Count == 0 ? "Noch keine Einträge." : string.Join(Environment.NewLine, verlauf));
        Dialog.Zeigen(fenster, FindForm());
    }

    private void Fuellen(Bewohner b)
    {
        bild.Setzen(Bilder.Pfad(b.Bild), BewohnerRegeln.Initialen(b), b.Nachname);
        name.Text = b.Name;
        zimmer.Text = $"Zimmer {b.Zimmer}" + (b.Bett.Length > 0 && b.Bett != ZimmerRegeln.Einzel ? $" · {ZimmerRegeln.BettText(b.Bett)}" : "");   // Schritt 26e
        foreach (var alt in kopfMarken.Controls.OfType<MarkenFeld>().Where(m => m != zimmer).ToList())
        {
            kopfMarken.Controls.Remove(alt);
            alt.Dispose();
        }
        var jetzt = DateTime.Now;
        var marken = new List<Marke>();
        if (Kennzeichen.OffenGefuehrtMarke(b) is Marke offenMarke) marken.Add(offenMarke);
        marken.AddRange(Modulverwaltung.Marken(speicher, b, jetzt));
        foreach (var m in marken)
            kopfMarken.Controls.Add(new MarkenFeld(m.Text, Stil.MarkeFuer(m.Art)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });
        StatusText = speicher.Statusmeldung(b.Id!.Value, jetzt) is Statusmeldung status ? "Status: " + StatusmeldungRegeln.Langtext(status, jetzt) : null;
        if (StatusText is not null)
        {
            var feld = new MarkenFeld(StatusText, Stil.MarkeFuer(MarkenArt.Status)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };
            kopfMarken.Controls.Add(feld);
            kopfMarken.Controls.SetChildIndex(feld, 0);   // vorne, vor dem Zimmer
        }
        // Wo müsste er sein? (Tagesplan, Schritt 23): nach „gegangen“ bis zum Ende der Therapie.
        WoText = (speicher.Mitfahrende(jetzt).GetValueOrDefault(b.Id!.Value) ?? speicher.InTherapie(jetzt).GetValueOrDefault(b.Id!.Value))?.Text;
        if (WoText is not null)
        {
            var feld = new MarkenFeld(WoText, Stil.MarkeFuer(WoText.StartsWith("abwesend") ? MarkenArt.Abwesend : MarkenArt.Status)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };
            kopfMarken.Controls.Add(feld);
            kopfMarken.Controls.SetChildIndex(feld, 0);
        }
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
        Alter.Text = $"({BewohnerRegeln.AlterText(b.Geburtsdatum, DateOnly.FromDateTime(DateTime.Now))})";
        OffenGefuehrt.Text = BewohnerRegeln.OffenGefuehrtText(b);
        bool offenUnklar = b.OffenGefuehrt == OffenGefuehrtZustand.NichtErfasst;
        OffenGefuehrt.ForeColor = offenUnklar ? Stil.Achtung.Text : Stil.TextNormal;
        OffenGefuehrt.Font = offenUnklar ? Stil.FettKursiv : Stil.Fett;
        Wohnbereich.Text = b.Wohnbereich;
        BezugZeigen(b);
        BetreuerZeigen(b);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Invalidate(true);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = S(Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int x = rand, y = rand + AutoScrollPosition.Y;
        int innen = S(Stil.AbstandGross);

        int bildSeite = S(BildGroesse);
        int kopfHoehe = bildSeite + 2 * innen;
        kopfKarte.Bounds = new Rectangle(x, y, breite, kopfHoehe);
        bild.Bounds = new Rectangle(innen, innen, bildSeite, bildSeite);
        int textX = innen + bildSeite + S(Stil.AbstandGross);
        var markenGroesse = kopfMarken.GetPreferredSize(new Size(Math.Max(100, breite - textX - innen), 0));
        int blockHoehe = name.PreferredHeight + S(Stil.AbstandKlein) + markenGroesse.Height;
        int textY = Math.Max(innen, (kopfHoehe - blockHoehe) / 2);
        name.Location = new Point(textX, textY);
        kopfMarken.MaximumSize = new Size(Math.Max(100, breite - textX - innen), 0);
        kopfMarken.Bounds = new Rectangle(new Point(textX, textY + name.PreferredHeight + S(Stil.AbstandKlein)), markenGroesse);
        y += kopfHoehe + S(Stil.AbstandMittel);

        Allergie.Bounds = new Rectangle(x, y, breite, S(52));
        y += Allergie.Height + S(Stil.AbstandMittel);

        Reiter.Bounds = new Rectangle(x, y, breite, S(40));
        y += Reiter.Height + S(Stil.AbstandGross);

        foreach (Control ansicht in new Control[] { Berichte, Medikation, Wochenplan, Biografie, Reflexion, Heb }.Concat(ModulAnsichten))
        {
            if (!ansicht.Visible) continue;
            // Der Wochenplan füllt die Höhe bis zum unteren Rand (Schritt 18).
            if (ansicht == Wochenplan) Wochenplan.VerfuegbareHoehe = ClientSize.Height - (y - AutoScrollPosition.Y) - rand;
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
        var knoepfe = bezugKnoepfe.GetPreferredSize(new Size(Math.Max(100, breite - 2 * innen), 0));
        stammdaten.Bounds = new Rectangle(x, y, breite, groesse.Height + S(Stil.AbstandKlein) + knoepfe.Height + 2 * innen);
        tabelle.Location = new Point(innen, innen);
        tabelle.Size = groesse;
        bezugKnoepfe.MaximumSize = new Size(Math.Max(100, breite - 2 * innen), 0);
        bezugKnoepfe.Bounds = new Rectangle(new Point(innen, tabelle.Bottom + S(Stil.AbstandKlein)), knoepfe);
        y += stammdaten.Height + rand;
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
    }
}
