using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Visite mit allen Bewohnern: Man geht mit dem Arzt (Psychiater oder Hausarzt) die Bewohner des Bereichs
// der Reihe nach durch; „Weitere Bewohner …“ nimmt jemanden aus einem anderen Bereich dazu. Links die Liste (Zimmer, Name, Bezugspflege, Häkchen „besprochen“), rechts
// zum gewählten Bewohner Allergiehinweis, Diagnosen (nur mit Recht), die aktuelle Medikation (wie im Medikamentenplan), die
// letzte Visite, offene Aufgaben zur Medikation und darunter die Eingabe. Beim Wechsel steht alles
// sofort als Entwurf in der Datenbank. Während der Visite gibt es keine Einblendungen; erst „Visite
// abschließen“ legt Berichte, Übergaben und Aufgaben an und zeigt die Aufgaben gesammelt.
public sealed class VisitenRundgang : Panel
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> alle;
    private readonly Panel rechts = new() { AutoScroll = true, BackColor = Stil.Seite };
    private readonly FlowLayoutPanel kopfleiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly Label titel = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
    private readonly Label medikationTitel = Abschnitt("AKTUELLE MEDIKATION");
    private readonly Label bedarfTitel = Abschnitt("BEI BEDARF");
    private readonly Label eingabeTitel = Abschnitt("VISITE FÜR DIESEN BEWOHNER");
    private bool fuellt;
    // Zwischenspeichern kurz nach dem Tippen: Auch bei einem Absturz geht höchstens die letzte Sekunde verloren.
    private readonly System.Windows.Forms.Timer merkenUhr = new() { Interval = 1500 };

    public VisitenEntwurf Entwurf { get; }
    public List<Bewohner> Bewohner { get; private set; }
    public int Aktuell { get; private set; } = -1;
    public ListView Liste { get; } = new()
    {
        View = View.Details, CheckBoxes = true, FullRowSelect = true, MultiSelect = false, HideSelection = false, Font = Stil.Standard, BorderStyle = BorderStyle.None,
    };
    public ComboBox Sortierung { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    public Label OffenText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public Label VorgemerktText { get; } = new() { AutoSize = true, ForeColor = Stil.Hinweis.Text, BackColor = Stil.Hinweis.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public Button VorherigerKnopf { get; } = Stil.NebenKnopf("◀ Vorheriger");
    public Button NaechsterKnopf { get; } = Stil.NebenKnopf("Nächster Bewohner ▶");
    // „◀ Vorheriger“ und „Nächster Bewohner ▶“ direkt unter der Eingabe; „Nächster“ erst, wenn
    // zu diesem Bewohner etwas eingetragen ist.
    public const string ErstEintragenText = "Erst den Bericht zu diesem Bewohner schreiben.";
    private readonly FlowLayoutPanel weiter = new() { AutoSize = true, WrapContents = false, BackColor = Stil.Seite, Margin = new Padding(0) };
    public Button UnterbrechenKnopf { get; } = Stil.NebenKnopf("Unterbrechen");
    public Button WeitereKnopf { get; } = Stil.NebenKnopf("Weitere Bewohner …");
    // Auswahl für „Weitere Bewohner …“ (Index oder null); zum Testen ersetzbar.
    public Func<IReadOnlyList<string>, int?> WeitereWaehlen { get; set; }
    public Button AbschliessenKnopf { get; } = Stil.HauptKnopf("Visite abschließen …");
    public Label Name_ { get; } = new() { AutoSize = true, Font = Stil.Ueberschrift, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
    public Label Allergie { get; } = new() { AutoSize = true, Font = Stil.Fett, BackColor = Stil.Seite, UseMnemonic = false };
    // „Medikation geändert: …“ (sieben Tage), leer, wenn nichts geändert wurde.
    public Label MedikationGeaendert { get; } = new()
    {
        AutoSize = true, Font = Stil.Fett, BackColor = Stil.MedikationGeaendert.Flaeche, ForeColor = Stil.MedikationGeaendert.Text,
        Padding = new Padding(Stil.AbstandMini), UseMnemonic = false,
    };
    private bool diagnosenZeigen;
    public Label Diagnosen { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, UseMnemonic = false };
    public DataGridView Tabelle { get; } = MedikationAnsicht.RegelTabelleBauen();
    public DataGridView BedarfTabelle { get; } = MedikationAnsicht.BedarfTabelleBauen();
    public Label KeineMedikation { get; } = new() { AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, Text = "Keine Medikation." };
    public Label LetzteVisite { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, UseMnemonic = false };
    public Label OffeneAufgaben { get; } = new() { AutoSize = true, ForeColor = Stil.Achtung.Text, BackColor = Stil.Seite, UseMnemonic = false };
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public VisiteEintragFeld? Feld { get; private set; }

    public event Action? Unterbrochen;
    public event Action<Speicher.VisiteGespeichert>? Abgeschlossen;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VisitenRundgang(Speicher speicher, VisitenEntwurf entwurf)
    {
        this.speicher = speicher;
        Entwurf = entwurf;
        BackColor = Stil.Seite;
        WeitereWaehlen = texte => Eingabe.Waehlen(FindForm(), "Weitere Bewohner", "Wer kommt dazu?", texte);
        // Dazu, wer schon im Entwurf steht (auch aus anderen Bereichen, über „Weitere Bewohner …“).
        alle = speicher.AlleBewohner().Where(b => b.Wohnbereich == entwurf.Bereich || entwurf.Eintraege.Any(e => e.BewohnerId == b.Id)).ToList();
        Bewohner = NachZimmer(alle);
        titel.Text = $"Visite {entwurf.Art} {Datum.Text(entwurf.Datum)} · {entwurf.Arzt}" + (entwurf.Teilnehmer.Length > 0 ? $" · dabei: {entwurf.Teilnehmer}" : "");
        Sortierung.Items.AddRange(["nach Zimmer", "nach Name"]);
        Sortierung.SelectedIndex = 0;
        Sortierung.AccessibleName = "Sortierung";
        Sortierung.SelectedIndexChanged += (_, _) => Sortieren();
        foreach (var c in new Control[] { titel, OffenText, VorgemerktText, Sortierung, WeitereKnopf, UnterbrechenKnopf, AbschliessenKnopf })
        {
            c.Margin = new Padding(0, 0, Stil.AbstandMittel, Stil.AbstandKlein);
            if (c is Button b) b.AutoSize = true;
            kopfleiste.Controls.Add(c);
        }
        titel.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein);
        VorherigerKnopf.Click += (_, _) => Vorheriger();
        NaechsterKnopf.Click += (_, _) => Naechster();
        foreach (var k in new[] { VorherigerKnopf, NaechsterKnopf }) { k.AutoSize = true; k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0); }
        weiter.Controls.AddRange([VorherigerKnopf, NaechsterKnopf]);
        UnterbrechenKnopf.Click += (_, _) => Unterbrechen();
        WeitereKnopf.Click += (_, _) => WeitereHinzufuegen();
        AbschliessenKnopf.Click += (_, _) => Abschliessen();
        merkenUhr.Tick += (_, _) => { merkenUhr.Stop(); Merken(); };
        Liste.Columns.Add("Zi.", 50);
        Liste.Columns.Add("Bewohner", 170);
        Liste.Columns.Add("Bezug", 60);
        Liste.AccessibleName = "Bewohner der Visite";
        Liste.ItemChecked += (_, e) => { if (!fuellt && e.Item.Tag is long id) HakenSetzen(id, e.Item.Checked); };
        Liste.SelectedIndexChanged += (_, _) => { if (!fuellt && Liste.SelectedIndices.Count == 1 && Liste.SelectedIndices[0] != Aktuell) Waehlen(Liste.SelectedIndices[0]); };
        Tabelle.AccessibleName = "Aktuelle Medikation";
        BedarfTabelle.AccessibleName = "Medikation bei Bedarf";
        rechts.Controls.AddRange([Name_, Allergie, MedikationGeaendert, Diagnosen, Fehler, medikationTitel, Tabelle, KeineMedikation, bedarfTitel, BedarfTabelle, LetzteVisite, OffeneAufgaben, eingabeTitel, weiter]);
        rechts.Layout += (_, _) => RechtsAnordnen();
        Controls.AddRange([kopfleiste, Liste, rechts]);
        ListeFuellen();
        if (Bewohner.Count > 0) Waehlen(Math.Max(0, Bewohner.FindIndex(b => !Entwurf.Eintrag(b.Id!.Value).Erledigt)));
        ZaehlerZeigen();
    }

    private static Label Abschnitt(string text) => new() { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };

    public string KopfText => OffenText.Text;

    // „Weitere Bewohner …“: jemand aus einem anderen Bereich kommt dazu und wird gleich gewählt.
    public bool WeitereHinzufuegen()
    {
        var andere = speicher.AlleBewohner().Where(b => alle.All(a => a.Id != b.Id))
            .OrderBy(b => b.Nachname, StringComparer.CurrentCulture).ThenBy(b => b.Vorname, StringComparer.CurrentCulture).ToList();
        if (andere.Count == 0 || WeitereWaehlen([.. andere.Select(b => $"{b.Nachname}, {b.Vorname} · Zi. {b.Zimmer} ({b.Wohnbereich})")]) is not int i
            || i < 0 || i >= andere.Count) return false;
        alle.Add(andere[i]);
        Sortieren();
        Waehlen(Bewohner.FindIndex(b => b.Id == andere[i].Id));
        return true;
    }

    private void Sortieren()
    {
        Merken();
        long? id = Aktuell >= 0 ? Bewohner[Aktuell].Id : null;
        Bewohner = Sortierung.SelectedIndex == 1 ? [.. alle.OrderBy(b => b.Nachname).ThenBy(b => b.Vorname)] : NachZimmer(alle);
        ListeFuellen();
        Aktuell = -1;
        Waehlen(Math.Max(0, Bewohner.FindIndex(b => b.Id == id)));
    }

    // Wie auf der Station: Zimmer für Zimmer, im Doppelzimmer erst das Bett am Fenster.
    private static List<Bewohner> NachZimmer(IEnumerable<Bewohner> liste) =>
        [.. liste.OrderBy(b => b.Zimmer, StringComparer.Ordinal).ThenBy(b => b.Bett == ZimmerRegeln.Tuer ? 1 : 0)];

    private void ListeFuellen()
    {
        fuellt = true;
        var farben = BezugAnzeige.Farben(speicher);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var b in Bewohner)
        {
            string? bezug = speicher.Bezug(b.Id!.Value).Bezugspflege?.Kuerzel;
            var item = new ListViewItem([b.Zimmer, b.Name, bezug is null ? "" : $"● {bezug}"]) { Tag = b.Id, Checked = Entwurf.Eintrag(b.Id!.Value).Erledigt, UseItemStyleForSubItems = false };
            if (bezug is not null) item.SubItems[2].ForeColor = BezugAnzeige.Farbe(farben, bezug);
            Liste.Items.Add(item);
        }
        Liste.EndUpdate();
        fuellt = false;
    }

    private void HakenSetzen(long id, bool erledigt)
    {
        Entwurf.Eintrag(id).Erledigt = erledigt;
        Speichern();
        ZaehlerZeigen();
    }

    private void ZaehlerZeigen()
    {
        int offen = Bewohner.Count(b => !Entwurf.Eintrag(b.Id!.Value).Erledigt);
        OffenText.Text = offen == 0 ? $"alle {Bewohner.Count} besprochen" : $"noch offen: {offen} von {Bewohner.Count}";
        int n = VisitenEntwurfRegeln.Vorgemerkt(Entwurf);
        VorgemerktText.Text = n == 1 ? "1 Aufgabe vorgemerkt" : $"{n} Aufgaben vorgemerkt";
        NaechsterKnopf.Enabled = Aktuell < Bewohner.Count - 1;   // „◀ Vorheriger“ ist immer aktiv
        NaechsterSperren();
        Geaendert?.Invoke();
    }

    // Den gewählten Bewohner zeigen; vorher den bisherigen als Entwurf sichern.
    public void Waehlen(int index)
    {
        if (index < 0 || index >= Bewohner.Count) return;
        Merken();
        Aktuell = index;
        var b = Bewohner[index];
        long id = b.Id!.Value;
        fuellt = true;
        Liste.SelectedIndices.Clear();
        if (index < Liste.Items.Count) { Liste.Items[index].Selected = true; Liste.EnsureVisible(index); }
        fuellt = false;
        Name_.Text = $"{b.Name} · Zi. {b.Zimmer}";
        Allergie.Text = BewohnerRegeln.AllergieText(b);   // beginnt schon mit „Allergien:“
        Allergie.ForeColor = b.Allergie == AllergieZustand.Eingetragen ? Stil.Beschluss.Text : b.Allergie == AllergieZustand.NichtErfasst ? Stil.Achtung.Text : Stil.TextMittel;
        diagnosenZeigen = Recht.Darf(speicher, Funktionen.AerztlicheAnsehen);   // nur Information, wird nicht ausgewertet
        Diagnosen.Visible = diagnosenZeigen;
        Diagnosen.Text = diagnosenZeigen ? $"Diagnosen: {DiagnoseRegeln.Kurz(speicher.Diagnosen(id))}" : "";
        var heute = DateOnly.FromDateTime(Jetzt());
        var verordnungen = speicher.Verordnungen(id);
        var geaendert = MedikationsAenderungen.Liste(verordnungen, heute);
        MedikationGeaendert.Text = geaendert.Count == 0 ? ""
            : $"{MedikationsAenderungen.MarkeText}: " + string.Join("; ", geaendert.Select(a => $"{a.Medikament}: {a.Was} (ab {Datum.Text(a.Ab)})"));
        MedikationAnsicht.TabellenFuellen(Tabelle, BedarfTabelle, verordnungen.Select(v => MedikationRegeln.Stand(v, heute)), abgesetzteZeigen: false);
        Tabelle.Visible = Tabelle.Rows.Count > 0;
        BedarfTabelle.Visible = bedarfTitel.Visible = BedarfTabelle.Rows.Count > 0;
        KeineMedikation.Visible = Tabelle.Rows.Count == 0 && BedarfTabelle.Rows.Count == 0;
        var letzte = speicher.Visiten(id).FirstOrDefault();
        var eintrag = letzte?.Eintraege.FirstOrDefault(e => e.BewohnerId == id);
        LetzteVisite.Text = letzte is null ? "Letzte Visite: noch keine." : $"Letzte Visite: {Datum.Text(letzte.Datum)} ({letzte.Arzt}): {eintrag?.Besprochen}";
        var offen = speicher.Aufgaben(Jetzt()).Where(a => a.BewohnerId == id && (a.VisiteAnordnungId is not null || a.Text.Contains("Medik", StringComparison.Ordinal))).ToList();
        OffeneAufgaben.Text = offen.Count == 0 ? "" : "Offen zur Medikation: " + string.Join(" · ", offen.Select(a => a.Text));
        OffeneAufgaben.Visible = offen.Count > 0;
        if (Feld is not null) { rechts.Controls.Remove(Feld); Feld.Dispose(); }
        Feld = new VisiteEintragFeld(b, speicher.Medikamente().Where(m => !m.Gesperrt).ToList());
        Feld.EntfernenKnopf.Visible = false;
        Feld.Leave += (_, _) => Merken();   // verlässt man die Eingabe, steht sie schon im Entwurf
        Feld.Besprochen.TextChanged += (_, _) => { NachTippen(); NaechsterSperren(); };
        Feld.Fachrichtung.TextChanged += (_, _) => NachTippen();
        fuellt = true;
        FeldSetzen(Feld, Entwurf.Eintrag(id));
        fuellt = false;
        rechts.Controls.Add(Feld);
        rechts.AutoScrollPosition = Point.Empty;
        ZaehlerZeigen();
        rechts.PerformLayout();
    }

    private static void FeldSetzen(VisiteEintragFeld f, EntwurfEintrag e)
    {
        f.Besprochen.Text = e.Besprochen;
        if (e.Anordnungen.Count > 0)
        {
            f.AnordnungenGeaendert.Checked = true;   // legt eine leere Zeile an
            for (int i = 0; i < e.Anordnungen.Count; i++)
                (i == 0 ? f.Zeilen[0] : f.ZeileHinzufuegen()).Setzen(VisitenRegeln.ArtAus(e.Anordnungen[i].Art), e.Anordnungen[i].Medikament);
        }
        f.FacharztNoetig.Checked = e.FacharztNoetig;
        f.Fachrichtung.Text = e.Facharzt;
        f.Dringend.Checked = e.Dringend;
    }

    // „Nächster Bewohner ▶“ grau mit Hinweis, solange „Besprochenes“ leer ist.
    private void NaechsterSperren() =>
        Stil.Sperren(NaechsterKnopf, Feld is { } f && f.Besprochen.Text.Trim().Length > 0 ? null : ErstEintragenText);

    private void NachTippen()
    {
        if (fuellt) return;
        merkenUhr.Stop();
        merkenUhr.Start();
    }

    // Eingabe des gezeigten Bewohners in den Entwurf übernehmen und speichern. Mit Inhalt gilt er als besprochen.
    public void Merken()
    {
        merkenUhr.Stop();
        if (Feld is null || Aktuell < 0 || Aktuell >= Bewohner.Count) return;
        long id = Bewohner[Aktuell].Id!.Value;
        bool vorher = Entwurf.Eintrag(id).Erledigt;
        var neu = VisitenEntwurfRegeln.AusEingabe(Feld.Eingabe(), vorher);
        neu.Erledigt = vorher || neu.HatInhalt;
        Entwurf.Eintraege.RemoveAll(e => e.BewohnerId == id);
        Entwurf.Eintraege.Add(neu);
        Speichern();
        if (Aktuell < Liste.Items.Count && Liste.Items[Aktuell].Checked != neu.Erledigt)
        {
            fuellt = true;
            Liste.Items[Aktuell].Checked = neu.Erledigt;
            fuellt = false;
        }
        ZaehlerZeigen();
    }

    private void Speichern()
    {
        try { speicher.VisitenEntwurfSpeichern(Entwurf); }
        catch (Eingabefehler e) { FehlerZeigen(string.Join(" ", e.Fehler)); }
    }

    public void Naechster()
    {
        if (Stil.Sperrgrund(NaechsterKnopf) is not null) return;   // erst eintragen (die Liste links bleibt frei wählbar)
        Waehlen(Aktuell + 1);
    }

    public void Vorheriger() => Waehlen(Aktuell - 1);

    public void Unterbrechen()
    {
        Merken();
        Unterbrochen?.Invoke();
    }

    // Alles speichern: Berichte, Übergaben, Aufgaben. Bewohner ohne Eintrag zählen nicht.
    public bool Abschliessen()
    {
        Merken();
        if (!Entwurf.Eintraege.Any(e => e.HatInhalt)) return FehlerZeigen("Noch nichts eingetragen – bitte bei mindestens einem Bewohner eintragen, was besprochen wurde.");
        Speicher.VisiteGespeichert gespeichert;
        try { gespeichert = speicher.VisiteAbschliessen(Entwurf, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            var namen = alle.ToDictionary(b => b.Id!.Value, b => b.Name);
            return FehlerZeigen("Bitte prüfen: " + string.Join(" ", e.Fehler) +
                                " (betrifft: " + string.Join(", ", Entwurf.Eintraege.Where(x => x.HatInhalt && x.Besprochen.Trim().Length == 0).Select(x => namen.GetValueOrDefault(x.BewohnerId, "?"))) + ")");
        }
        Abgeschlossen?.Invoke(gespeichert);
        return true;
    }

    private bool FehlerZeigen(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = true;
        rechts.PerformLayout();
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) merkenUhr.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, ClientSize.Width - 2 * rand);
        kopfleiste.MaximumSize = new Size(breite, 0);
        kopfleiste.Bounds = new Rectangle(new Point(rand, rand), kopfleiste.GetPreferredSize(new Size(breite, 0)));
        int y = kopfleiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        int links = Stil.Px(this, 300), hoehe = Math.Max(100, ClientSize.Height - y - rand);
        Liste.SetBounds(rand, y, links, hoehe);
        rechts.SetBounds(rand + links + Stil.Px(this, Stil.AbstandGross), y, Math.Max(100, breite - links - Stil.Px(this, Stil.AbstandGross)), hoehe);
    }

    private void RechtsAnordnen()
    {
        int rand = Stil.Px(this, Stil.AbstandMini), breite = Math.Max(100, rechts.ClientSize.Width - 2 * rand - SystemInformation.VerticalScrollBarWidth);
        int y = rand + rechts.AutoScrollPosition.Y;
        void Setzen(Control c, int abstand = Stil.AbstandKlein)
        {
            c.MaximumSize = c is Label ? new Size(breite, 0) : c.MaximumSize;
            c.Location = new Point(rand, y);
            y = c.Bottom + Stil.Px(this, abstand);
        }
        Setzen(Name_, Stil.AbstandMini);
        Setzen(Allergie);
        MedikationGeaendert.Visible = MedikationGeaendert.Text.Length > 0;
        if (MedikationGeaendert.Text.Length > 0) Setzen(MedikationGeaendert);
        if (diagnosenZeigen) Setzen(Diagnosen);
        if (Fehler.Text.Length > 0 && Fehler.Visible) Setzen(Fehler);
        Setzen(medikationTitel, Stil.AbstandMini);
        foreach (var (t, sichtbar) in new[] { (Tabelle, Tabelle.Rows.Count > 0), (BedarfTabelle, BedarfTabelle.Rows.Count > 0) })
        {
            if (!sichtbar) continue;
            if (t == BedarfTabelle) Setzen(bedarfTitel, Stil.AbstandMini);
            t.Width = breite;
            t.Height = PlanTabelle.Hoehe(t);
            Setzen(t);
        }
        if (Tabelle.Rows.Count == 0 && BedarfTabelle.Rows.Count == 0) Setzen(KeineMedikation);
        Setzen(LetzteVisite, Stil.AbstandMini);
        if (OffeneAufgaben.Text.Length > 0) Setzen(OffeneAufgaben);
        y += Stil.Px(this, Stil.AbstandKlein);
        Setzen(eingabeTitel, Stil.AbstandMini);
        if (Feld is not null) Setzen(Feld);
        Setzen(weiter);
        rechts.AutoScrollMinSize = new Size(0, y - rechts.AutoScrollPosition.Y + rand);
    }
}

// „Visite beginnen …“: Datum (heute), Arzt aus dem Verzeichnis, dabei (angemeldete Person vorbelegt).
public sealed class VisiteBeginnenFenster : Form
{
    private readonly Speicher speicher;
    private readonly string bereich, art;

    public TextBox DatumFeld { get; } = new DatumFeld() { Width = 100 };
    public ArztWahl Arzt { get; }
    public PersonenWahl Teilnehmer { get; }
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public VisitenEntwurf? Entwurf { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VisiteBeginnenFenster(Speicher speicher, string bereich, string art = VisitenRegeln.Psychiater)
    {
        this.speicher = speicher;
        this.bereich = bereich;
        this.art = art;
        Arzt = new ArztWahl(speicher);
        Teilnehmer = new PersonenWahl(speicher) { AccessibleName = "Dabei" };
        Fenster.Grundform(this, $"Visite {art} beginnen");
        DatumFeld.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Datum", DatumFeld);
        zeile(art, Arzt);
        zeile("Dabei", Teilnehmer);
        zeile(null, Fenster.Hinweis($"Danach geht man die Bewohner von „{bereich}“ der Reihe nach durch; „Weitere Bewohner …“ nimmt andere dazu. Alles wird sofort als Entwurf gespeichert; " +
                                    "Berichte, Übergaben und Aufgaben entstehen erst mit „Visite abschließen“."));
        zeile("Eingetragen von", Recht.KuerzelAnzeige());
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Visite beginnen", () => Beginnen()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Beginnen()
    {
        var fehler = new List<string>();
        DateOnly tag = default;
        try
        {
            tag = Datum.Lesen(DatumFeld.Text, "Datum");
            if (tag > DateOnly.FromDateTime(Jetzt())) fehler.Add("Die Visite liegt in der Zukunft.");
        }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (Arzt.Gewaehlt.Length == 0) fehler.Add("Arzt fehlt (aus dem Ärzteverzeichnis wählen).");
        if (!Recht.Angemeldet) fehler.Add(RechteRegeln.NichtAngemeldetText);
        if (fehler.Count > 0)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", fehler);
            Fehler.Visible = true;
            return false;
        }
        Entwurf = new VisitenEntwurf
        {
            Bereich = bereich, Art = art, Datum = tag, Arzt = Arzt.Gewaehlt, Teilnehmer = Teilnehmer.AlsText, Kuerzel = Recht.Kuerzel, Begonnen = Jetzt(),
        };
        speicher.VisitenEntwurfSpeichern(Entwurf);
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Ausdruck „Visitenliste“ (A4 hoch): alle Bewohner des Bereichs mit aktueller Medikation, ein Block je
// Bewohner mit Platz für Notizen – als Papiervorlage, falls am Bildschirm nicht gearbeitet werden kann.
public sealed class VisitenlisteDruck(string bereich, DateOnly tag, IReadOnlyList<(Bewohner Bewohner, IReadOnlyList<Tagesstand> Staende)> bloecke)
{
    private int naechster;

    public PrintDocument DokumentBauen()
    {
        naechster = 0;
        var dokument = new PrintDocument { DocumentName = $"Visitenliste {bereich} {Datum.Text(tag)}" };
        dokument.BeginPrint += (_, _) => naechster = 0;
        dokument.PrintPage += (_, e) => e.HasMorePages = SeiteZeichnen(e.Graphics!);
        return dokument;
    }

    // Zeichnet eine Seite; true = es folgen weitere.
    public bool SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 15f, rechts = 195f, unten = 282f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var normal = new Font("Segoe UI", 9F);
        using var klein = new Font("Segoe UI", 7.5F);
        using var linie = new Pen(Color.Black, 0.2f);
        using var hell = new Pen(Color.Gray, 0.15f);
        float y = 15f;
        g.DrawString($"Visitenliste – {bereich} – {Datum.Text(tag)}", titel, Brushes.Black, links, y);
        y += 9f;
        g.DrawString($"{MedikationRegeln.DruckHinweis} · {Stil.DemoHinweis}", klein, Brushes.Black, links, y);
        y += 6f;
        if (bloecke.Count == 0) { g.DrawString("Keine Bewohner in diesem Bereich.", normal, Brushes.Black, links, y); return false; }
        while (naechster < bloecke.Count)
        {
            var (b, staende) = bloecke[naechster];
            var zeilen = staende.Where(s => s.Sichtbar || s.Zustand == Zustand.NochNicht)
                .Select(s => $"{MedikationAnsicht.MedikamentText(s.Verordnung)} — " +
                             (s.Verordnung.Bedarf ? $"bei Bedarf: {s.Verordnung.BedarfAnlass}, {s.Verordnung.Dosis.Morgens} {s.Verordnung.Einheit}" : s.Dosis.Text) +
                             $" — {MedikationAnsicht.StatusText(s)}").ToList();
            if (zeilen.Count == 0) zeilen.Add("keine Medikation");
            float hoehe = 7f + 5f + zeilen.Count * 4.5f + 24f;
            if (y + hoehe > unten && y > 40f) return true;   // nächste Seite
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
            g.DrawString($"Zi. {b.Zimmer} · {b.Name}", fett, Brushes.Black, links, y);
            y += 6f;
            g.DrawString(BewohnerRegeln.AllergieText(b), normal, Brushes.Black, links, y);
            y += 5f;
            foreach (var z in zeilen)
            {
                g.DrawString(z, normal, Brushes.Black, new RectangleF(links + 3f, y, rechts - links - 3f, 4.5f));
                y += 4.5f;
            }
            y += 2f;
            g.DrawString("Notizen:", klein, Brushes.Black, links, y);
            for (int i = 1; i <= 3; i++) g.DrawLine(hell, links + 14f, y + i * 6f, rechts, y + i * 6f);
            y += 22f;
            naechster++;
        }
        return false;
    }
}
