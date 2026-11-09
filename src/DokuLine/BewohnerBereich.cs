using DokuLine.Kern;

namespace DokuLine;

// Bereich „Dokumentation“: links die Bewohner als schmale, einklappbare Liste („Nachname, Vorname“, Bereichswahl und
// Suche darüber), rechts ihre Berichte mit Filter. Pfeiltasten blättern; Doppelklick oder „Akte öffnen“ öffnet die
// Akte. Pro Ansicht genau eine Hauptaktion in Akzentfarbe.
public sealed class BewohnerBereich : Panel
{
    public const int LinksBreite = 240, LinksEingeklappt = 36;
    private readonly Speicher speicher;
    private readonly KopfBereich kopf;
    public Button BerichtNeuKnopf { get; } = Stil.HauptKnopf("Neuer Bericht …");   // für den gewählten Bewohner
    public Button BedarfKnopf { get; } = Stil.NebenKnopf("Bedarf gegeben …");
    public Button AkteKnopf { get; } = Stil.NebenKnopf("Akte öffnen");
    public Button AnsichtKnopf { get; } = Stil.NebenKnopf("Ansicht: kompakt");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    private readonly Button zurueckKnopf = Stil.NebenKnopf("← Dokumentation");
    private readonly Button berichteKnopf = Stil.NebenKnopf("Berichte ansehen");
    private readonly Button bearbeitenKnopf = Stil.HauptKnopf("Bearbeiten …");

    public ListBox Namen { get; } = new()
    {
        IntegralHeight = false, BorderStyle = BorderStyle.None, Font = Stil.Standard, AccessibleName = "Bewohner",
    };
    public List<Bewohner> Zeilen { get; private set; } = [];
    public Bewohner? Gewaehlt => Namen.SelectedIndex >= 0
        && Namen.SelectedIndex < Zeilen.Count ? Zeilen[Namen.SelectedIndex] : null;
    public DokumentationAnsicht Dokumentation { get; }
    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Standard,
        AccessibleName = "Bereich" };
    public const string AlleBereiche = "alle Bereiche";
    public TextBox Suche { get; } = new() { PlaceholderText = "Name suchen", Font = Stil.Standard,
        AccessibleName = "Bewohner suchen" };
    public Button EinklappenKnopf { get; } = Stil.NebenKnopf("◀");
    public bool Eingeklappt { get; private set; }
    private readonly Panel listenFlaeche = new() { Dock = DockStyle.Fill, BackColor = Stil.Seite };
    private readonly Panel links = new() { Dock = DockStyle.Left, Width = LinksBreite, BackColor = Stil.Flaeche };
    private long? zuletzt;
    // Offene Aufgaben über der Liste (Wohnbereich-ToDo), mit Knopf zu allen.
    public AufgabenKurzliste Aufgaben { get; } = new();
    public NachrichtenHinweis Nachrichten { get; } = new() { Visible = false };
    public event Action<long?>? NachrichtenAnzeigen;
    private readonly Button nachrichtenKnopf = Stil.NebenKnopf("Nachrichten …");
    private readonly Button statusKnopf = Stil.NebenKnopf("Status …");
    private readonly Button berichtKnopf = Stil.NebenKnopf("Neuer Bericht …");
    public Button AerztlichKnopf { get; } = Stil.NebenKnopf("Ärztliche Informationen …");

    // „Neuer Bericht …“ aus der Akte; das Hauptfenster öffnet ihn.
    public event Action<long>? BerichtGewuenscht;

    // Wahl einer Bedarfsverordnung, wenn es mehrere gibt (Index oder null); zum Testen ersetzbar.
    public Func<IReadOnlyList<string>, int?> BedarfWaehlen { get; set; }

    // „Bezug ändern …“ in der Akte.
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

    // Der heutige Tag (für die Aufenthaltsdauer); zum Testen ersetzbar.
    internal Func<DateOnly> Heute { get; set; } = () => DateOnly.FromDateTime(DateTime.Today);
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public AkteAnsicht Akte { get; }
    // Eigenes Feld statt Akte.Visible: Visible meldet „unsichtbar“, solange das Fenster selbst
    // noch nicht angezeigt ist.
    public bool AkteSichtbar { get; private set; }

    // Zum Testen ersetzbar: öffnet das Formular als Dialog und gibt den gespeicherten Bewohner zurück.
    internal Func<BewohnerFormular, Bewohner?> FormularZeigen { get; set; } =
        f => { Stil.SymbolSetzen(f); return f.ShowDialog() == DialogResult.OK ? f.Gespeichert : null; };

    public BewohnerBereich(Speicher speicher, KopfBereich kopf)
    {
        this.speicher = speicher;
        this.kopf = kopf;
        BedarfWaehlen = texte => Eingabe.Waehlen(FindForm(), "Bedarf gegeben", "Welche Bedarfsmedikation?", texte);
        Akte = new AkteAnsicht(speicher) { Dock = DockStyle.Fill, Visible = false };
        Akte.BezugGewuenscht += id => BezugOeffnen(id);
        BackColor = Stil.Seite;
        BereichWahl.Items.AddRange([UebersichtSeite.EigeneBereiche, AlleBereiche,
            .. speicher.Wohnbereiche().Select(w => (object)w.Name)]);
        BereichWahl.SelectedIndex = 0;
        BereichWahl.SelectedIndexChanged += (_, _) => { if (!abgleichen) UebersichtZeigen(); };
        Suche.TextChanged += (_, _) => UebersichtZeigen();
        Namen.SelectedIndexChanged += (_, _) => { if (!abgleichen) BerichteZeigen(); };
        Namen.DoubleClick += (_, _) => AkteOeffnen();
        Namen.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; AkteOeffnen(); } };
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        menue.Items.Add("Akte öffnen", null, (_, _) => AkteOeffnen());
        menue.Items.Add("Neuer Bericht …", null, (_, _) => NeuerBericht());
        Namen.ContextMenuStrip = menue;
        EinklappenKnopf.AccessibleName = "Bewohnerliste ein- oder ausklappen";
        EinklappenKnopf.Click += (_, _) => Einklappen(!Eingeklappt);
        links.Controls.AddRange([EinklappenKnopf, BereichWahl, Suche, Namen]);
        links.Layout += (_, _) => LinksAnordnen();
        Dokumentation = new DokumentationAnsicht(speicher) { Dock = DockStyle.Fill,
            Padding = new Padding(Stil.AbstandGross, 0, Stil.AbstandGross, 0) };
        listenFlaeche.Controls.Add(Dokumentation);
        listenFlaeche.Controls.Add(new Panel { Dock = DockStyle.Left, Width = Stil.AbstandGross,
            BackColor = Stil.Seite });
        listenFlaeche.Controls.Add(links);
        listenFlaeche.Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, 0, 0);
        BerichtNeuKnopf.Click += (_, _) => NeuerBericht();
        BedarfKnopf.Click += (_, _) => BedarfGeben();
        AkteKnopf.Click += (_, _) => AkteOeffnen();
        AnsichtKnopf.Click += (_, _) => AnsichtUmschalten();
        DruckKnopf.Click += (_, _) => Dokumentation.Drucken();
        aufgabenRahmen.Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross,
            Stil.AbstandKlein);
        aufgabenRahmen.Controls.Add(Aufgaben);
        aufgabenRahmen.Controls.Add(Nachrichten);
        aufgabenRahmen.Layout += (_, _) => AufgabenAnordnen();
        Aufgaben.AlleKnopf.Click += (_, _) => AufgabenAnzeigen?.Invoke();
        Nachrichten.OeffnenKnopf.Click += (_, _) => NachrichtenAnzeigen?.Invoke(null);
        nachrichtenKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) NachrichtenAnzeigen?.Invoke(id); };
        statusKnopf.Click += (_, _) => StatusOeffnen();
        AerztlichKnopf.Click += (_, _) => AerztlichOeffnen();
        berichtKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) BerichtGewuenscht?.Invoke(id); };
        Controls.Add(listenFlaeche);
        Controls.Add(Akte);
        Controls.Add(aufgabenRahmen);
        listenFlaeche.BringToFront();
        Akte.BringToFront();
        zurueckKnopf.Click += (_, _) => UebersichtZeigen();
        berichteKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) DokumentationZeigen(id); };
        bearbeitenKnopf.Click += (_, _) =>
        {
            if (Akte.Bewohner?.Id is long id) Formular(speicher.Bewohner(id));
        };
    }

    private void AufgabenAnordnen()
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
    }

    private void LinksAnordnen()
    {
        int rand = Stil.Px(this, Stil.AbstandKlein), breite = links.ClientSize.Width - 2 * rand;
        EinklappenKnopf.SetBounds(links.ClientSize.Width - rand - Stil.Px(this, 28), rand, Stil.Px(this, 28),
            Stil.Px(this, 28));
        foreach (var c in new Control[] { BereichWahl, Suche, Namen }) c.Visible = !Eingeklappt;
        if (Eingeklappt) return;
        BereichWahl.SetBounds(rand, rand, breite - Stil.Px(this, 28) - rand, BereichWahl.Height);
        Suche.SetBounds(rand, BereichWahl.Bottom + rand, breite, Suche.Height);
        Namen.SetBounds(rand, Suche.Bottom + rand, breite, Math.Max(40,
            links.ClientSize.Height - Suche.Bottom - 2 * rand));
    }

    public void Einklappen(bool ein)
    {
        Eingeklappt = ein;
        links.Width = Stil.Px(this, ein ? LinksEingeklappt : LinksBreite);
        EinklappenKnopf.Text = ein ? "▶" : "◀";
        links.PerformLayout();
    }

    private bool abgleichen;

    // Die Liste links (Bereich, Suche) und rechts die Berichte des gewählten Bewohners; die Wahl bleibt.
    public void UebersichtZeigen()
    {
        long? vorher = (AkteSichtbar ? Akte.Bewohner?.Id : null) ?? Gewaehlt?.Id ?? zuletzt;
        if (Namen.SelectedIndex < 0 && !AkteSichtbar) vorher = zuletzt;
        var werte = Einstellungen.Laden();
        var alle = speicher.AlleBewohner();
        abgleichen = true;
        try
        {
            Auswahl.Abgleichen(BereichWahl, [UebersichtSeite.EigeneBereiche, AlleBereiche,
                .. speicher.Wohnbereiche().Select(w => w.Name)]);
            var imBereich = BereichFiltern(alle, werte, out string bereichText);
            string suche = Suche.Text.Trim();
            Zeilen = [.. imBereich.Where(b => suche.Length == 0 || b.Name.Contains(suche,
                StringComparison.CurrentCultureIgnoreCase)
                                             || $"{b.Nachname}, {b.Vorname}".Contains(suche,
                                                 StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(b => b.Nachname, StringComparer.CurrentCulture).ThenBy(b => b.Vorname,
                    StringComparer.CurrentCulture)];
            Namen.BeginUpdate();
            Namen.Items.Clear();
            foreach (var b in Zeilen) Namen.Items.Add($"{b.Nachname}, {b.Vorname}");
            Namen.EndUpdate();
            int index = Zeilen.FindIndex(b => b.Id == vorher);
            if (index < 0 && Zeilen.Count > 0) index = 0;
            Namen.SelectedIndex = index;
            string anzahl = imBereich.Count == 1 ? "1 Bewohner" : $"{imBereich.Count} Bewohner";
            string zahl = Zeilen.Count == imBereich.Count ? anzahl : $"{Zeilen.Count} von {anzahl}";
            kopf.TexteSetzen($"{bereichText} · {zahl}", "Dokumentation");
        }
        finally { abgleichen = false; }
        BerichteZeigen();
        Akte.Visible = false;
        listenFlaeche.Visible = aufgabenRahmen.Visible = true;
        AkteSichtbar = false;
        kopf.KnoepfeSetzen(BerichtNeuKnopf, BedarfKnopf, AkteKnopf, AnsichtKnopf, DruckKnopf);
    }

    // Eigene Bereiche wie in der Übersicht (Wohnbereiche des Angemeldeten, sonst der des Rechners; ohne
    // eigene Bewohner alle), „alle Bereiche“ oder ein gewählter Wohnbereich.
    private List<Bewohner> BereichFiltern(List<Bewohner> alle, EinstellungsWerte werte, out string text)
    {
        string? wahl = BereichWahl.SelectedItem as string;
        if (wahl == AlleBereiche) { text = AlleBereiche; return alle; }
        if (wahl is not null && wahl != UebersichtSeite.EigeneBereiche)
        {
            text = wahl;
            return alle.Where(b => b.Wohnbereich == wahl).ToList();
        }
        var meine = speicher.MitarbeiterLesen(werte.Kuerzel)?.Bereiche
            .Where(b => alle.Any(x => x.Wohnbereich == b)).ToList() is { Count: > 0 } liste ? liste : [werte.Bereich];
        var eigene = alle.Where(b => meine.Contains(b.Wohnbereich)).ToList();
        if (eigene.Count == 0) { text = AlleBereiche; return alle; }
        text = string.Join(", ", meine);
        return eigene;
    }

    public bool Waehlen(long bewohnerId)
    {
        int index = Zeilen.FindIndex(b => b.Id == bewohnerId);
        if (index < 0) return false;
        Namen.SelectedIndex = index;
        return true;
    }

    // Die Berichte eines Bewohners zeigen (nach „Neuer Bericht …“, „Berichte ansehen“); wohnt er in einem anderen
    // Bereich oder passt nicht zur Suche, werden alle Bereiche gezeigt.
    public void DokumentationZeigen(long bewohnerId)
    {
        AkteSichtbar = false;
        zuletzt = bewohnerId;
        Namen.SelectedIndex = -1;
        UebersichtZeigen();
        if (Waehlen(bewohnerId)) return;
        Suche.Text = "";
        BereichWahl.SelectedItem = AlleBereiche;
        Waehlen(bewohnerId);
    }

    // Rechts die Berichte des gewählten Bewohners; ohne Auswahl sind die Knöpfe grau.
    private void BerichteZeigen()
    {
        var b = Gewaehlt;
        if (b is not null) { zuletzt = b.Id; Dokumentation.Zeigen(b); }
        Dokumentation.Visible = b is not null;
        string? ohne = b is null ? "Bitte zuerst einen Bewohner wählen." : null;
        Stil.Sperren(AkteKnopf, ohne);
        Stil.Sperren(DruckKnopf, ohne);
        Stil.Sperren(BerichtNeuKnopf, ohne ?? Recht.Grund(speicher, Funktionen.BerichteSchreiben));
        Stil.Sperren(BedarfKnopf, ohne ?? Recht.Grund(speicher, Funktionen.BedarfGeben)
            ?? (BedarfVerordnungen(b!).Count == 0 ? "Keine Bedarfsmedikation, die heute gilt." : null));
        if (Namen.ContextMenuStrip is { } menue)
        {
            menue.Items[1].Enabled = Stil.Sperrgrund(BerichtNeuKnopf) is null;
            menue.Items[1].Text = Stil.Sperrgrund(BerichtNeuKnopf) is string grund
                ? $"Neuer Bericht … – {grund}"
                : "Neuer Bericht …";
        }
    }

    private List<Verordnung> BedarfVerordnungen(Bewohner b)
    {
        var heute = Heute();
        return [.. speicher.Verordnungen(b.Id!.Value).Where(v => v.Bedarf && MedikationRegeln.Stand(v,
            heute).Zustand == Zustand.Gilt)];
    }

    public void AkteOeffnen() { if (Gewaehlt?.Id is long id) AkteZeigen(id); }

    // „Neuer Bericht …“ für den gewählten Bewohner: bleibt in der Liste, rechts erscheint er.
    public bool NeuerBericht()
    {
        if (Gewaehlt is null || Stil.Sperrgrund(BerichtNeuKnopf) is not null) return false;
        return Dokumentation.NeuerBericht();
    }

    // „Bedarf gegeben …“ wie im Medikamentenplan; bei mehreren Bedarfsverordnungen erst wählen.
    public bool BedarfGeben()
    {
        if (Gewaehlt is not Bewohner b || Stil.Sperrgrund(BedarfKnopf) is not null) return false;
        var liste = BedarfVerordnungen(b);
        var texte = liste.Select(v => $"{v.Medikament} {v.Staerke} – bei Bedarf: {v.BedarfAnlass}".Trim()).ToList();
        int? wahl = liste.Count == 1 ? 0 : BedarfWaehlen(texte);
        if (wahl is not int i || i < 0 || i >= liste.Count) return false;
        using var fenster = new BedarfFenster(speicher, liste[i], b);
        bool ok = Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK;
        Dokumentation.Aktualisieren();
        return ok;
    }

    public void AnsichtUmschalten()
    {
        Dokumentation.KompaktSetzen(!Dokumentation.Kompakt);
        AnsichtKnopf.Text = Dokumentation.Kompakt ? "Ansicht: kompakt" : "Ansicht: vollständig";
    }

    // Rechte des Angemeldeten auf die Knöpfe im Kopf der Akte.
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
        kopf.TexteSetzen("Dokumentation", "Bewohnerakte");   // der Name steht nur einmal, auf der Karte darunter
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
        listenFlaeche.Visible = aufgabenRahmen.Visible = false;
        Akte.Visible = true;
        AkteSichtbar = true;
        kopf.TexteSetzen("Dokumentation", "Bewohnerakte");   // der Name steht nur einmal, auf der Karte darunter
        AkteKnoepfeSperren();
        kopf.KnoepfeSetzen(zurueckKnopf, berichteKnopf, nachrichtenKnopf, statusKnopf, AerztlichKnopf, berichtKnopf,
            bearbeitenKnopf);
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

    // Ärztliche Informationen (Recht „ansehen“); danach die Akte neu (Allergiehinweis).
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

    // „Neuer Bewohner …“ aus der Übersicht: Formular; der gespeicherte Bewohner oder null.
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
        // Neu lesen: Ein anderer Arbeitsplatz kann inzwischen gespeichert haben.
        else if (b?.Id is long id) AkteZeigen(id);
    }
}
