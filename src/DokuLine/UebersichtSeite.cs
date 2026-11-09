using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Startseite „Übersicht“ (seit Bauplan Schritt 26l): links „Heute wichtig“ mit vier Kästen (Übergaben
// offen, Termine heute, laufender Dienst, Nachrichten ungelesen), rechts alle Bewohner des Bereichs als
// Liste (Zimmer/Bett, Name, Bezugspflege, Wo, Hinweise). Doppelklick öffnet die Akte; Berichte zeigt
// erst der Bereich „Bewohner“. Unter 1100 px Breite steht „Heute wichtig“ über der Liste.
// Baut sich nach Änderungen und um Mitternacht neu auf. Mehrplatz: gezeigt wird der eigene Bereich
// (Einstellungen), umschaltbar auf „alle Bereiche“ oder einen Wohnbereich.
public sealed class UebersichtSeite : Panel
{
    public const int LinksBreite = 380, SchmalAb = 1100;
    private readonly Speicher speicher;
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 60_000 };
    private DateOnly gezeigtAm;
    private SchichtTermin? gezeigtSchicht;
    private bool abgleichen;
    public Doppelklickschutz Schutz { get; } = new();
    private readonly Panel links = new() { AutoScroll = true, BackColor = Stil.Seite };
    private readonly KartenFlaeche rechts = new();
    private readonly Label listenKopf = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };

    // „Heute wichtig“
    public HeuteKasten Uebergaben { get; } = new("ÜBERGABEN OFFEN");
    public HeuteKasten Termine { get; } = new("TERMINE HEUTE");
    public HeuteKasten Dienst { get; } = new("LAUFENDER DIENST · NOCH OFFEN");
    public HeuteKasten Nachrichten { get; } = new("NACHRICHTEN UNGELESEN");
    public ProgressBar DienstBalken { get; } = new() { Height = 10, Minimum = 0, Style = ProgressBarStyle.Continuous, AccessibleName = "Erledigte Aufgaben" };
    public Label DienstText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public LinkLabel AlleAufgaben { get; } = new() { Text = "Alle Aufgaben …", AutoSize = true, BackColor = Stil.Flaeche, LinkColor = Stil.Akzent };
    public DienstStand? DienstStand { get; private set; }

    // Bewohnerliste
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Font = Stil.Standard,
        BorderStyle = BorderStyle.None, ShowItemToolTips = true, AccessibleName = "Bewohnerliste",
    };
    public List<BewohnerZeile> Zeilen { get; private set; } = [];
    public BewohnerZeile? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as BewohnerZeile : null;
    public string ListenKopfText => listenKopf.Text;
    public TextBox Suche { get; } = new() { Width = 220, PlaceholderText = "Name oder Zimmer suchen", Font = Stil.Standard, AccessibleName = "Bewohner suchen" };
    // Bereichswahl (steht rechts in der Aktionsleiste): eigene Bereiche, alle Bereiche, R1, R2, R3 …
    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = Stil.Standard, AccessibleName = "Bereich" };
    public const string EigeneBereiche = "eigene Bereiche";
    public const string AlleBereiche = "alle Bereiche";
    // Schritt 20: nur die eigenen Bezugsbewohner bzw. Einzeltrainings (aus allen Bereichen).
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
        BereichWahl.Items.Add(EigeneBereiche);
        BereichWahl.Items.Add(AlleBereiche);
        BereichWahl.Items.AddRange([.. speicher.Wohnbereiche().Select(w => (object)w.Name)]);
        BereichWahl.SelectedIndex = 0;
        BereichWahl.SelectedIndexChanged += (_, _) => { if (!abgleichen) Aktualisieren(); };
        foreach (var c in new[] { MeineBezugsbewohner, MeineEinzeltrainings }) c.CheckedChanged += (_, _) => Aktualisieren();
        Suche.TextChanged += (_, _) => ListeFuellen();

        Dienst.Oben.AddRange([DienstText, DienstBalken]);
        Dienst.Unten.Add(AlleAufgaben);
        AlleAufgaben.LinkClicked += (_, _) => StelleGewuenscht?.Invoke("Aufgaben");
        Uebergaben.KopfGeklickt += () => StelleGewuenscht?.Invoke("Übergabe");
        Termine.KopfGeklickt += () => StelleGewuenscht?.Invoke("Tagesplan");
        Dienst.KopfGeklickt += () => StelleGewuenscht?.Invoke("Aufgaben");
        Nachrichten.KopfGeklickt += () => StelleGewuenscht?.Invoke("Nachrichten");
        links.Controls.AddRange([Uebergaben, Termine, Dienst, Nachrichten]);

        foreach (var (titel, breite) in new[] { ("Zimmer", 100), ("Name", 170), ("Bezug", 60), ("Wo", 230), ("Hinweise", 300) }) Liste.Columns.Add(titel, breite);
        Liste.DoubleClick += (_, _) => AkteOeffnen();
        Liste.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; AkteOeffnen(); } };
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSperren();
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        menue.Items.Add("Akte öffnen", null, (_, _) => AkteOeffnen());
        menue.Items.Add("Neuer Bericht …", null, (_, _) => NeuerBericht());
        Liste.ContextMenuStrip = menue;
        rechts.Controls.AddRange([listenKopf, Suche, MeineBezugsbewohner, MeineEinzeltrainings, BereichHinweis, Liste]);

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

    // Neuer Tag oder neue Schicht seit dem letzten Aufbau (0.33.2: vorher nur der Tag – der Kasten „Laufender
    // Dienst“ zeigte nach dem Schichtwechsel noch die alte Schicht).
    public bool Veraltet => DateOnly.FromDateTime(Jetzt()) != gezeigtAm || SchichtTermin.Zu(Jetzt()) != gezeigtSchicht;

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        gezeigtAm = heute;
        gezeigtSchicht = SchichtTermin.Zu(jetzt);
        abgleichen = true;
        try { Auswahl.Abgleichen(BereichWahl, [EigeneBereiche, AlleBereiche, .. speicher.Wohnbereiche().Select(w => w.Name)]); }
        finally { abgleichen = false; }
        var werte = Einstellungen.Laden();
        var jeder = speicher.Heute(jetzt, werte.HebFristMonate);
        // Eigene Bereiche: die Wohnbereiche des angemeldeten Mitarbeiters (Schritt 19); hat er keinen
        // mit Bewohnern, der Bereich dieses Rechners. Hat auch der keine Bewohner (z. B. „Therapie“),
        // alle mit Hinweis.
        var meine = speicher.MitarbeiterLesen(werte.Kuerzel)?.Bereiche.Where(b => jeder.Any(h => h.Bewohner.Wohnbereich == b)).ToList()
            is { Count: > 0 } liste ? liste : [werte.Bereich];
        var eigene = jeder.Where(h => meine.Contains(h.Bewohner.Wohnbereich)).ToList();
        string? wahl = BereichWahl.SelectedItem as string;
        bool allesZeigen = wahl == AlleBereiche || eigene.Count == 0;
        var alle = allesZeigen ? jeder : eigene;
        if (wahl is not null && wahl != EigeneBereiche && wahl != AlleBereiche)
        {
            meine = [wahl];
            alle = jeder.Where(h => h.Bewohner.Wohnbereich == wahl).ToList();
            allesZeigen = false;
        }
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
        BereichHinweis.Visible = wahl == EigeneBereiche && eigene.Count == 0 && jeder.Count > 0;
        BereichHinweis.Text = $"Für „{string.Join(", ", meine)}“ gibt es keine eigenen Bewohner – gezeigt werden alle Bereiche.";
        string bereichText = meineBp || meineEt
            ? string.Join(" und ", new[] { meineBp ? "meine Bezugsbewohner" : null, meineEt ? "meine Einzeltrainings" : null }.OfType<string>())
            : allesZeigen ? AlleBereiche : string.Join(", ", meine);
        gezeigt = alle;
        var ids = alle.Select(h => h.Bewohner.Id!.Value).ToHashSet();
        var offeneUebergaben = speicher.Uebergaben(jetzt).Where(u => u.BewohnerId is null || ids.Contains(u.BewohnerId.Value)).ToList();

        // Bewohnerliste: Marken wie bisher auf den Karten, dazu Übergabe und Geburtstag; „Wo“ eigene Spalte.
        var status = speicher.GeltendeStatusmeldungen(jetzt);
        var inTherapie = speicher.InTherapie(jetzt);   // Tagesplan (Schritt 23)
        var mitfahrende = speicher.Mitfahrende(jetzt);  // Fahrten (Schritt 24): abwesend während der Fahrt
        var farben = BezugAnzeige.Farben(speicher);
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
            int uebergaben = offeneUebergaben.Count(u => u.BewohnerId == id);
            if (uebergaben > 0) marken.Add(new Marke(MarkenArt.Uebergabe, uebergaben == 1 ? "Übergabe" : $"{uebergaben} Übergaben"));
            if (allesZeigen && !meine.Contains(h.Bewohner.Wohnbereich)) marken.Add(new Marke(MarkenArt.Angabe, h.Bewohner.Wohnbereich));
            var bezug = bezuege.GetValueOrDefault(id);
            var zeile = new BewohnerZeile(h.Bewohner, heute, marken,
                bezug?.Bezugspflege is { } bp ? (bp.Kuerzel, BezugAnzeige.Farbe(farben, bp.Kuerzel)) : null,
                bezug?.Einzeltrainer is { } tr ? (BezugRegeln.TrainerText(tr), BezugAnzeige.Farbe(farben, tr.Kuerzel)) : null,
                HeuteWichtigRegeln.Wo(marken, h.Termine, jetzt));
            zeile.Gewaehlt += b => AkteGewuenscht?.Invoke(b.Id!.Value);
            alleZeilen.Add(zeile);
        }
        alleZeilen = [.. alleZeilen.OrderBy(z => z.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal).ThenBy(z => z.Bewohner.Bett == ZimmerRegeln.Tuer ? 1 : 0)];
        string kurz = meineBp || meineEt ? bereichText : allesZeigen ? "alle Bereiche" : string.Join(", ", meine);
        listenKopf.Text = $"BEWOHNER {kurz.ToUpperInvariant()} · {alleZeilen.Count} · {alleZeilen.Count(z => z.ImHaus)} IM HAUS";
        ListeFuellen();

        HeuteFuellen(jetzt, alle, offeneUebergaben, werte.Kuerzel);

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
    private void HeuteFuellen(DateTime jetzt, List<HeuteFuer> alle, List<Uebergabe> offen, string kuerzel)
    {
        var bewohner = alle.ToDictionary(h => h.Bewohner.Id!.Value, h => h.Bewohner);
        var ids = bewohner.Keys.ToHashSet();

        // 1. Übergaben offen: bleiben stehen, bis sie erledigt sind.
        Uebergaben.Setzen(offen.Count, offen.Select(u =>
        {
            var b = u.BewohnerId is long id ? bewohner.GetValueOrDefault(id) : null;
            return Eintrag(HeuteWichtigRegeln.UebergabeText(u, b), HeuteWichtigRegeln.SeitText(u), null, () => StelleGewuenscht?.Invoke("Übergabe"));
        }));

        // 2. Termine heute aus dem Tagesplan, mit dem Status der Tafel.
        var termine = HeuteWichtigRegeln.Termine(speicher.Tagesplan(DateOnly.FromDateTime(jetzt), alle.Select(h => h.Bewohner)), jetzt);
        Termine.Setzen(termine.Count, termine.Select(t =>
            Eintrag($"{t.Zeit}  {t.Text}", null, (t.StatusText, StatusMarke(t.Status)), () => StelleGewuenscht?.Invoke("Tagesplan"))));

        // 3. Laufender Dienst: „6 von 9 erledigt“, Balken, offene Aufgaben zum Abhaken. Die Standardaufgaben der
        // Schicht zuerst anlegen (0.33.2: vorher erst beim Öffnen von „Aufgaben“).
        speicher.StandardaufgabenErzeugen(jetzt);
        var aufgaben = speicher.Aufgaben(jetzt, auchErledigte: true).Where(a => a.BewohnerId is null || ids.Contains(a.BewohnerId.Value));
        var dienst = HeuteWichtigRegeln.Dienst(aufgaben, jetzt);
        DienstStand = dienst;
        DienstText.Text = $"{BerichtRegeln.SchichtText(dienst.Schicht.Schicht)}: {dienst.Text}";
        DienstBalken.Maximum = Math.Max(1, dienst.Gesamt);
        DienstBalken.Value = Math.Min(DienstBalken.Maximum, dienst.Erledigt);
        string? sperre = kuerzel.Length == 0 ? RechteRegeln.NichtAngemeldetText : null;
        Dienst.Setzen(dienst.Offen.Count, dienst.Offen.Select(a =>
        {
            string name = a.BewohnerId is long id && bewohner.TryGetValue(id, out var b) ? $" ({b.Name})" : "";
            string faellig = AufgabenRegeln.Ueberfaellig(a, jetzt) ? " · überfällig" : "";
            var haken = new CheckBox
            {
                Text = a.Text + name + faellig, AutoSize = false, BackColor = Stil.Flaeche, Font = Stil.Standard, UseMnemonic = false, Tag = a,
                ForeColor = faellig.Length > 0 ? Stil.Beschluss.Text : Stil.TextNormal, Enabled = sperre is null, AccessibleName = $"Erledigt: {a.Text}{name}",
            };
            // Nach dem Abhaken baut sich der Kasten neu auf; der zweite Klick eines Doppelklicks träfe sonst die
            // nächste Aufgabe (0.33.2).
            haken.CheckedChanged += (_, _) =>
            {
                if (!haken.Checked) return;
                if (Schutz.Frei()) BeginInvoke(() => Erledigen(a));
                else haken.Checked = false;
            };
            return (Control)haken;
        }), leer: dienst.Gesamt > 0 ? "Alles erledigt" : HeuteWichtigRegeln.NichtsOffen);

        // 4. Nachrichten ungelesen (an den Angemeldeten).
        if (kuerzel.Length == 0) Nachrichten.Setzen(0, [], leer: "Nicht angemeldet");
        else
        {
            var verlaeufe = speicher.Verlaeufe(kuerzel, NachrichtenOrdner.Posteingang);
            var ungelesen = HeuteWichtigRegeln.Ungelesen(verlaeufe, kuerzel);
            // Zähler wie in der Seitenleiste: alle ungelesenen Nachrichten; je Verlauf steht die neueste da (0.33.3:
            // vorher zählte der Kasten die Verläufe).
            Nachrichten.Setzen(verlaeufe.Sum(v => v.Ungelesen), ungelesen.Select(n =>
            {
                var v = verlaeufe.First(x => x.Id == n.Verlauf);
                return Eintrag(HeuteWichtigRegeln.NachrichtText(n), BerichtRegeln.ZeitText(n.Gesendet), null, () => VerlaufGewuenscht?.Invoke(v));
            }));
        }
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

    private static Stil.Marke StatusMarke(TafelStatus s) => s switch
    {
        TafelStatus.Gegangen or TafelStatus.Zurueck => Stil.Offen,
        TafelStatus.NochOffen => Stil.Achtung,
        TafelStatus.Abgesagt or TafelStatus.Ausgefallen => Stil.Beschluss,
        TafelStatus.Jetzt or TafelStatus.Gleich => Stil.Unterwegs,
        _ => Stil.Zimmer,
    };

    private static HeuteEintrag Eintrag(string text, string? unten, (string Text, Stil.Marke Marke)? status, Action klick)
    {
        var e = new HeuteEintrag(text, unten, status);
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
            menue.Items[1].Text = grund is null ? "Neuer Bericht …" : $"Neuer Bericht … – {grund}";
        }
    }

    public void Drucken()
    {
        var druck = new UebersichtDruck(ListenKopfText, KopfText, [Uebergaben, Termine, Dienst, Nachrichten], Zeilen, Jetzt());
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, "Übersicht");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), abstand = Stil.Px(this, Stil.AbstandMittel);
        int breite = Math.Max(300, ClientSize.Width - 2 * rand), hoehe = Math.Max(200, ClientSize.Height - 2 * rand);
        Schmal = ClientSize.Width < Stil.Px(this, SchmalAb);
        int linksBreite = Schmal ? breite : Stil.Px(this, LinksBreite);
        // Die Kästen untereinander; im schmalen Fenster über der Liste (höchstens 40 % der Höhe, dann Bildlauf).
        int kastenBreite = linksBreite - SystemInformation.VerticalScrollBarWidth;   // Platz für den Bildlauf
        int y = links.AutoScrollPosition.Y;
        foreach (var k in new[] { Uebergaben, Termine, Dienst, Nachrichten })
        {
            k.SetBounds(0, y, kastenBreite, k.Height);
            k.PerformLayout();
            y = k.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        int inhalt = y - links.AutoScrollPosition.Y;
        if (Schmal)
        {
            int obenHoehe = Math.Min(inhalt, hoehe * 2 / 5);
            links.SetBounds(rand, rand, breite, obenHoehe);
            rechts.SetBounds(rand, links.Bottom + abstand, breite, Math.Max(150, hoehe - obenHoehe - abstand));
        }
        else
        {
            links.SetBounds(rand, rand, linksBreite, hoehe);
            rechts.SetBounds(links.Right + abstand, rand, Math.Max(200, breite - linksBreite - abstand), hoehe);
        }
        links.AutoScrollMinSize = new Size(0, inhalt);

        // Rechts: Kopf mit Zählern, Suche und Filtern, darunter die Liste.
        int innen = Stil.Px(this, Stil.AbstandMittel);
        int rb = rechts.ClientSize.Width - 2 * innen;
        listenKopf.Location = new Point(innen, innen + Stil.Px(this, 4));
        int x = rechts.ClientSize.Width - innen;
        foreach (var c in new Control[] { MeineEinzeltrainings, MeineBezugsbewohner, Suche })
        {
            int w = c is TextBox ? c.Width : c.PreferredSize.Width;
            x -= w;
            c.Location = new Point(x, innen + (c is TextBox ? 0 : Stil.Px(this, 3)));
            x -= Stil.Px(this, Stil.AbstandMittel);
        }
        int unter = Math.Max(listenKopf.Bottom, Suche.Bottom) + Stil.Px(this, Stil.AbstandKlein);
        if (x < listenKopf.Right)   // zu schmal: Suche und Filter in eine eigene Zeile
        {
            int fx = innen;
            foreach (var c in new Control[] { Suche, MeineBezugsbewohner, MeineEinzeltrainings })
            {
                c.Location = new Point(fx, listenKopf.Bottom + Stil.Px(this, Stil.AbstandKlein));
                fx += (c is TextBox ? c.Width : c.PreferredSize.Width) + Stil.Px(this, Stil.AbstandMittel);
            }
            unter = Suche.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
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
            // Bereichswahl und Knöpfe stehen in der Aktionsleiste; sind sie gerade nicht dort, hier freigeben.
            foreach (var c in Knoepfe.Cast<Control>().Append(BereichWahl).Where(c => c.Parent is null)) c.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Ein Kasten in „Heute wichtig“ (Schritt 26l): Überschrift in Großbuchstaben mit Zähler, darunter die
// Einträge. Ohne Einträge steht „Nichts offen“ da; der Kasten verschwindet nicht.
public sealed class HeuteKasten : KartenFlaeche
{
    private readonly Label kopf = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false, Cursor = Cursors.Hand };
    public Label Leer { get; } = new() { Text = HeuteWichtigRegeln.NichtsOffen, AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    public List<Control> Zeilen { get; } = [];
    public List<Control> Oben { get; } = [];    // feste Teile über den Einträgen (z. B. Fortschritt)
    public List<Control> Unten { get; } = [];   // feste Teile darunter (z. B. „Alle Aufgaben …“)
    public string Titel { get; }
    public int Anzahl { get; private set; }
    public string KopfText => kopf.Text;
    public event Action? KopfGeklickt;

    public HeuteKasten(string titel)
    {
        Titel = titel;
        kopf.Text = titel;
        kopf.Click += (_, _) => KopfGeklickt?.Invoke();
        Controls.Add(kopf);
        Controls.Add(Leer);
        Padding = new Padding(Stil.AbstandMittel);
    }

    public void Setzen(int anzahl, IEnumerable<Control> zeilen, string leer = HeuteWichtigRegeln.NichtsOffen)
    {
        foreach (var c in Oben.Concat(Unten)) if (c.Parent != this) Controls.Add(c);
        foreach (var z in Zeilen) { Controls.Remove(z); z.Dispose(); }
        Zeilen.Clear();
        Zeilen.AddRange(zeilen);
        foreach (var z in Zeilen) Controls.Add(z);
        Anzahl = anzahl;
        kopf.Text = $"{Titel} · {anzahl}";
        Leer.Text = leer;
        Leer.Visible = Zeilen.Count == 0;
        AccessibleName = $"{Titel}: {anzahl}";
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandMittel), mini = Stil.Px(this, Stil.AbstandMini);
        int breite = Math.Max(100, Width - 2 * innen), y = innen;
        kopf.Location = new Point(innen, y);
        y = kopf.Bottom + Stil.Px(this, Stil.AbstandKlein);
        void Setzen(Control c)
        {
            switch (c)
            {
                case Label l when l.AutoSize: l.MaximumSize = new Size(breite, 0); l.Location = new Point(innen, y); break;
                case CheckBox k:
                    int hoch = TextRenderer.MeasureText(k.Text, k.Font, new Size(breite - Stil.Px(this, 22), 0), TextFormatFlags.WordBreak).Height + Stil.Px(this, 6);
                    k.SetBounds(innen, y, breite, Math.Max(Stil.Px(this, 24), hoch));
                    break;
                case HeuteEintrag e: e.Width = breite; e.Location = new Point(innen, y); e.PerformLayout(); break;
                default: c.SetBounds(innen, y, c is ProgressBar ? breite : c.Width, c.Height); break;
            }
            y = c.Bottom + mini;
        }
        foreach (var c in Oben) Setzen(c);
        if (Leer.Visible) Setzen(Leer);
        foreach (var z in Zeilen) Setzen(z);
        foreach (var c in Unten) Setzen(c);
        int hoehe = y - mini + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Ein Eintrag in einem Kasten: Text (umbrechend), darunter klein z. B. „seit 07.11. · AB“, rechts
// ein Status als Wort mit Farbe. Klick öffnet die Stelle, an die er gehört.
public sealed class HeuteEintrag : Panel
{
    public Label Text1 { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label? Text2 { get; }
    public MarkenFeld? Status { get; }
    public event Action? Geklickt;

    public HeuteEintrag(string text, string? unten, (string Text, Stil.Marke Marke)? status)
    {
        BackColor = Stil.Flaeche;
        Cursor = Cursors.Hand;
        Text1.Text = text;
        Controls.Add(Text1);
        if (unten is not null)
        {
            Text2 = new Label { Text = unten, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
            Controls.Add(Text2);
        }
        if (status is { } s)
        {
            Status = new MarkenFeld(s.Text, s.Marke);
            Controls.Add(Status);
        }
        AccessibleRole = AccessibleRole.Link;
        AccessibleName = text + (unten is null ? "" : $", {unten}") + (status is { } st ? $", {st.Text}" : "");
        foreach (Control c in Controls) { c.Cursor = Cursors.Hand; c.Click += (_, _) => Klicken(); }
        Click += (_, _) => Klicken();
    }

    public void Klicken() => Geklickt?.Invoke();

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int statusBreite = Status is null ? 0 : Status.PreferredSize.Width + Stil.Px(this, Stil.AbstandKlein);
        Text1.MaximumSize = new Size(Math.Max(60, Width - statusBreite), 0);
        Text1.Location = Point.Empty;
        int y = Text1.Bottom;
        if (Text2 is not null) { Text2.Location = new Point(0, y); y = Text2.Bottom; }
        if (Status is not null) Status.Bounds = new Rectangle(Width - Status.PreferredSize.Width, 0, Status.PreferredSize.Width, Status.PreferredSize.Height);
        int hoehe = Math.Max(y, Status?.Bottom ?? 0) + Stil.Px(this, 2);
        if (Height != hoehe) Height = hoehe;
    }
}

// „Drucken …“ in der Übersicht: Kopf, die vier Kästen als Text und die Bewohnerliste als Tabelle.
public sealed class UebersichtDruck(string listenKopf, string kopf, IReadOnlyList<HeuteKasten> kaesten, IReadOnlyList<BewohnerZeile> zeilen, DateTime jetzt)
{
    private int naechster;

    // Alle Zeilen des Ausdrucks: (Abschnitt?, Text, Spalten?) – Spalten nur für die Bewohnerliste.
    public List<(string? Abschnitt, string Text)> Zeilen()
    {
        var liste = new List<(string?, string)>();
        foreach (var k in kaesten)
        {
            if (k.Zeilen.Count == 0) liste.Add((k.KopfText, k.Leer.Text));
            foreach (var z in k.Zeilen)
                liste.Add((k.KopfText, z switch
                {
                    HeuteEintrag e => e.AccessibleName ?? e.Text1.Text,
                    CheckBox c => "☐ " + c.Text,
                    _ => z.Text,
                }));
        }
        foreach (var z in zeilen)
            liste.Add((listenKopf, $"{z.ZimmerText}\t{z.Bewohner.Name}\t{z.Bezugspflege?.Kuerzel ?? ""}\t{z.Wo}\t{z.HinweisText}"));
        return liste;
    }

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Übersicht" };
        dokument.DefaultPageSettings.Landscape = true;
        dokument.BeginPrint += (_, _) => naechster = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechster = SeiteZeichnen(e.Graphics!, naechster);
            e.HasMorePages = naechster < Zeilen().Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 15f, rechts = 282f, unten = 195f;
        float y = 12f;
        using var titel = new Font("Segoe UI Semibold", 13F);
        using var fett = new Font("Segoe UI Semibold", 9.5F);
        using var normal = new Font("Segoe UI", 9F);
        using var klein = new Font("Segoe UI", 7.5F);
        using var linie = new Pen(Color.Gray, 0.2f);
        var alle = Zeilen();
        g.DrawString("Übersicht – " + kopf, titel, Brushes.Black, links, y);
        y += 7f;
        g.DrawString($"Gedruckt {BerichtRegeln.ZeitText(jetzt)} · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y);
        y += 5f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 2f;
        float[] spalten = [0f, 22f, 70f, 88f, 150f];   // Zimmer, Name, Bezug, Wo, Hinweise (mm ab links)
        string? letzter = null;
        int i = start;
        for (; i < alle.Count; i++)
        {
            var (abschnitt, text) = alle[i];
            bool tabelle = abschnitt == listenKopf;
            float hoehe = (tabelle ? 4.5f : g.MeasureString(text, normal, new SizeF(rechts - links - 4f, 1000f)).Height + 0.5f) + (abschnitt != letzter ? 7f : 0f);
            if (y + hoehe > unten && i > start) break;
            if (abschnitt != letzter)
            {
                y += 1.5f;
                g.DrawString(abschnitt, fett, Brushes.Black, links, y);
                y += 5.5f;
                letzter = abschnitt;
            }
            if (tabelle)
            {
                var teile = text.Split('\t');
                for (int s = 0; s < teile.Length && s < spalten.Length; s++)
                {
                    float x = links + spalten[s], bis = s + 1 < spalten.Length ? links + spalten[s + 1] - 1f : rechts;
                    g.DrawString(teile[s], normal, Brushes.Black, new RectangleF(x, y, bis - x, 4.5f), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                }
                y += 4.5f;
            }
            else
            {
                g.DrawString(text, normal, Brushes.Black, new RectangleF(links + 4f, y, rechts - links - 4f, 1000f));
                y += g.MeasureString(text, normal, new SizeF(rechts - links - 4f, 1000f)).Height + 0.5f;
            }
        }
        return i;
    }
}
