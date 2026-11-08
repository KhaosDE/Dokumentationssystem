using DokuLine.Kern;

namespace DokuLine;

// Bereich „Visiten“ (Bauplan Schritt 26): alle Visiten (neueste oben) mit Filter nach Bewohner und
// Zeitraum. „Neue Visite …“ trägt ein, was besprochen wurde, geänderte Anordnungen und nötige
// Facharzttermine. Nach dem Speichern blendet das Programm sofort die entstandenen Aufgaben ein;
// in der Akte steht die Visite als Bericht (Lebensbereich 5).
public sealed class VisitenSeite : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];
    private List<Bewohner> bewohner = [];

    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Visite …");   // einzelne Bewohner
    // Schritt 26c: Visite mit allen Bewohnern des Bereichs (fortsetzen, wenn ein Entwurf besteht).
    public Button BeginnenKnopf { get; } = Stil.HauptKnopf("Visite beginnen …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Visitenliste drucken …");
    public VisitenRundgang? Rundgang { get; private set; }
    public ComboBox BewohnerFilter { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    public TextBox Von { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox Bis { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public Label FilterFehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Seite, Visible = false };
    public List<VisiteKarte> Karten { get; } = [];
    public event Action? Geaendert;
    // Knopf in der Einblendung nach dem Speichern: das Hauptfenster führt die Aufgabe aus.
    public event Action<Aufgabe>? AufgabeGewuenscht;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VisitenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var c in new Control[] { Beschriftung("Bewohner"), BewohnerFilter, Beschriftung("von"), Von, Beschriftung("bis"), Bis, DruckKnopf, FilterFehler })
        {
            c.Margin = c is Label ? new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) : new Padding(0, 0, Stil.AbstandMittel, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        BewohnerFilter.AccessibleName = "Bewohner";
        Von.AccessibleName = "Zeitraum von";
        Bis.AccessibleName = "Zeitraum bis";
        BewohnerFilter.SelectedIndexChanged += (_, _) => Aktualisieren();
        Von.TextChanged += (_, _) => Aktualisieren();
        Bis.TextChanged += (_, _) => Aktualisieren();
        NeuKnopf.Click += (_, _) => NeueVisite();
        BeginnenKnopf.Click += (_, _) => Beginnen();
        DruckKnopf.Click += (_, _) => VisitenlisteDrucken();
        Controls.Add(leiste);
        AutoScroll = true;
    }

    private static Label Beschriftung(string text) => new() { Text = text, AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite };

    private static string Bereich => Einstellungen.Laden().Bereich;

    public string KopfText => Rundgang is not null ? $"Visite läuft · {Rundgang.KopfText}" : (Karten.Count == 1 ? "1 Visite" : $"{Karten.Count} Visiten") +
                              (BewohnerFilter.SelectedIndex > 0 ? $" · {bewohner[BewohnerFilter.SelectedIndex - 1].Name}" : "");

    // Beim Öffnen des Bereichs: Bewohnerliste neu (Auswahl bleibt), dann die Liste.
    public void Zeigen()
    {
        long? gewaehlt = GewaehlterBewohner;
        bewohner = speicher.AlleBewohner();
        BewohnerFilter.BeginUpdate();
        BewohnerFilter.Items.Clear();
        BewohnerFilter.Items.Add("Alle Bewohner");
        BewohnerFilter.Items.AddRange([.. bewohner.Select(b => (object)$"{b.Name}, Zi. {b.Zimmer}")]);
        int index = bewohner.FindIndex(b => b.Id == gewaehlt);
        BewohnerFilter.SelectedIndex = index + 1;   // nicht gefunden: „Alle Bewohner“
        BewohnerFilter.EndUpdate();
        Aktualisieren();
    }

    public long? GewaehlterBewohner => BewohnerFilter.SelectedIndex > 0 ? bewohner[BewohnerFilter.SelectedIndex - 1].Id : null;

    public void BewohnerWaehlen(long? id)
    {
        if (bewohner.Count == 0) Zeigen();
        BewohnerFilter.SelectedIndex = bewohner.FindIndex(b => b.Id == id) + 1;
    }

    public void Aktualisieren()
    {
        if (BewohnerFilter.Items.Count == 0) return;   // noch nicht gezeigt
        Stil.Sperren(NeuKnopf, Recht.Grund(speicher, Funktionen.Visiten));   // Rechte (Schritt 19)
        Stil.Sperren(BeginnenKnopf, Recht.Grund(speicher, Funktionen.Visiten));
        BeginnenKnopf.Text = speicher.VisitenEntwurf(Bereich) is null ? "Visite beginnen …" : "Visite fortsetzen";
        bool fehler = false;
        DateOnly? Lesen(TextBox feld)
        {
            if (feld.Text.Trim().Length == 0) return null;
            try { return Datum.Lesen(feld.Text, "Zeitraum"); }
            catch (FormatException) { fehler = true; return null; }
        }
        var von = Lesen(Von);
        var bis = Lesen(Bis);
        FilterFehler.Text = "Zeitraum bitte als TT.MM.JJJJ eingeben.";
        FilterFehler.Visible = fehler;
        long? id = GewaehlterBewohner;
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        foreach (var v in speicher.Visiten(id, von, bis))
        {
            var karte = new VisiteKarte(v, namen, id);
            Karten.Add(karte);
            inhalt.Add(karte);
        }
        if (Karten.Count == 0)
            inhalt.Add(new Label { Text = "Keine Visiten für diese Auswahl.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        foreach (var c in inhalt)
        {
            c.Visible = Rundgang is null;   // während einer Visite bleibt die Liste verborgen
            Controls.Add(c);
        }
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    // „Visite beginnen …“ (Datum, Arzt, dabei) bzw. eine unterbrochene Visite fortsetzen.
    public bool Beginnen()
    {
        if (Recht.Grund(speicher, Funktionen.Visiten) is not null) return false;
        var entwurf = speicher.VisitenEntwurf(Bereich);
        if (entwurf is null)
        {
            using var fenster = new VisiteBeginnenFenster(speicher, Bereich);
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK || fenster.Entwurf is null) return false;
            entwurf = fenster.Entwurf;
        }
        RundgangZeigen(entwurf);
        return true;
    }

    private void RundgangZeigen(VisitenEntwurf entwurf)
    {
        RundgangBeenden();
        Rundgang = new VisitenRundgang(speicher, entwurf) { Jetzt = Jetzt };
        Rundgang.Geaendert += () => Geaendert?.Invoke();
        Rundgang.Unterbrochen += () => { RundgangBeenden(); Aktualisieren(); };
        Rundgang.Abgeschlossen += gespeichert =>
        {
            RundgangBeenden();
            Aktualisieren();
            AufgabenEinblenden(gespeichert.Aufgaben);   // erst jetzt, gesammelt
        };
        foreach (Control c in Controls) if (c != Rundgang) c.Visible = false;
        Controls.Add(Rundgang);
        AutoScroll = false;
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void RundgangBeenden()
    {
        if (Rundgang is null) return;
        Controls.Remove(Rundgang);
        Rundgang.Dispose();
        Rundgang = null;
        foreach (Control c in Controls) c.Visible = true;
        AutoScroll = true;
        PerformLayout();
    }

    public void VisitenlisteDrucken()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        var bloecke = speicher.AlleBewohner().Where(b => b.Wohnbereich == Bereich).OrderBy(b => b.Zimmer, StringComparer.Ordinal)
            .Select(b => (b, (IReadOnlyList<Tagesstand>)speicher.Verordnungen(b.Id!.Value).Select(v => MedikationRegeln.Stand(v, heute)).ToList())).ToList();
        var druck = new VisitenlisteDruck(Bereich, heute, bloecke);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Visitenliste {Bereich}");
    }

    public void NeueVisite()
    {
        if (Recht.Grund(speicher, Funktionen.Visiten) is not null) return;
        Speicher.VisiteGespeichert? gespeichert;
        using (var fenster = new VisiteFenster(speicher, GewaehlterBewohner))
        {
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
            gespeichert = fenster.Gespeichert;
        }
        Aktualisieren();
        if (gespeichert is not null) AufgabenEinblenden(gespeichert.Aufgaben);
    }

    // Gleich nach dem Speichern: die entstandenen Aufgaben, je mit Knopf. Ist eine erledigt, kommt die
    // Einblendung mit den übrigen wieder; „Später“ (oder Abbrechen) schließt nur die Einblendung.
    public void AufgabenEinblenden(IReadOnlyList<Aufgabe> aufgaben)
    {
        var ids = aufgaben.Select(a => a.Id).ToList();
        while (true)
        {
            var offen = ids.Select(i => speicher.AufgabeLesen(i)).OfType<Aufgabe>().Where(a => a.Offen).ToList();
            if (offen.Count == 0) return;
            Aufgabe? wunsch;
            using (var fenster = new VisitenAufgabenFenster(speicher, offen))
            {
                if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
                wunsch = fenster.Wunsch;
            }
            if (wunsch is null) return;
            AufgabeGewuenscht?.Invoke(wunsch);
            if (speicher.AufgabeLesen(wunsch.Id)?.Offen != false) return;   // nicht erledigt: nicht noch einmal fragen
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Rundgang is not null)   // die Visitenansicht füllt die ganze Fläche
        {
            Rundgang.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            return;
        }
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(breite, 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is VisiteKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandMittel));
                y = c.Bottom + Stil.Px(this, Stil.AbstandMini);
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Eine Visite: Datum, Arzt (ältere auch die Art), wer dabei war; je Bewohner das Besprochene und Marken für geänderte
// Anordnungen und nötige Facharzttermine. Mit Bewohnerfilter nur dessen Eintrag.
public sealed class VisiteKarte : KartenFlaeche
{
    private readonly List<Control> teile = [];
    private readonly HashSet<Control> abstandDavor = [];

    public Visite Visite { get; }
    public IReadOnlyList<string> Texte { get; }

    public VisiteKarte(Visite v, IReadOnlyDictionary<long, string> namen, long? nurBewohner)
    {
        Visite = v;
        var texte = new List<string>();
        Label Text(string text, Font font, Color farbe)
        {
            var l = new Label { Text = text, Font = font, ForeColor = farbe, AutoSize = true, BackColor = Stil.Flaeche, UseMnemonic = false };
            teile.Add(l);
            Controls.Add(l);
            texte.Add(text);
            return l;
        }
        Text($"{Kern.Datum.Text(v.Datum)} · {(v.Art.Length > 0 ? $"{v.Art} · " : "")}{v.Arzt}", Stil.KarteName, Stil.TextNormal);
        Text((v.Teilnehmer.Length > 0 ? $"dabei: {v.Teilnehmer} · " : "") + $"eingetragen von {v.Kuerzel}, {BerichtRegeln.ZeitText(v.Erstellt)}", Stil.Klein, Stil.TextLeise);
        foreach (var e in v.Eintraege.Where(e => nurBewohner is null || e.BewohnerId == nurBewohner))
        {
            abstandDavor.Add(Text(namen.GetValueOrDefault(e.BewohnerId) ?? "(Bewohner nicht mehr vorhanden)", Stil.Fett, Stil.TextNormal));
            Text(e.Besprochen, Stil.Standard, Stil.TextNormal);
            var marken = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
            void Marke(string text, Stil.Marke m)
            {
                marken.Controls.Add(new MarkenFeld(text, m) { Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandMini, 0) });
                texte.Add(text);
            }
            if (e.Anordnungen.Count == 0) Marke("Anordnungen nicht geändert", Stil.Zimmer);
            foreach (var a in e.Anordnungen) Marke($"{a.Medikament}: {VisitenRegeln.ArtText(a.Art)}", Stil.Hinweis);
            if (e.Facharzt.Length > 0) Marke($"Facharzttermin nötig: {e.Facharzt}" + (e.Dringend ? ", dringend" : ""), e.Dringend ? Stil.Beschluss : Stil.Achtung);
            teile.Add(marken);
            Controls.Add(marken);
        }
        Texte = texte;
        AccessibleName = $"Visite {string.Join(", ", texte.Take(1))}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        foreach (var c in teile)
        {
            if (abstandDavor.Contains(c)) y += Stil.Px(this, Stil.AbstandMittel);
            c.MaximumSize = new Size(breite, 0);
            c.Bounds = new Rectangle(new Point(innen, y), c.GetPreferredSize(new Size(breite, 0)));
            y = c.Bottom + Stil.Px(this, 2);
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// „Neue Visite“ für einzelne Bewohner: Datum, Arzt (aus dem Verzeichnis), wer dabei war, dann je Bewohner ein Feld. Der Inhalt
// rollt; Fehler, „Speichern“ und „Abbrechen“ bleiben unten sichtbar.
public sealed class VisiteFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> bewohner;
    private readonly List<Medikament> medikamente;
    private readonly Panel inhalt = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly FlowLayoutPanel unten = new()
    {
        Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandGross),
    };
    private readonly TableLayoutPanel raster = new() { ColumnCount = 2, AutoSize = true, Padding = new Padding(Stil.AbstandGross) };
    private readonly FlowLayoutPanel eintraege = new() { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Fensterhintergrund };

    public TextBox DatumFeld { get; } = new DatumFeld() { Width = 100 };
    public ArztWahl Arzt { get; }   // Schritt 26b: Auswahl aus dem Ärzteverzeichnis
    public PersonenWahl Teilnehmer { get; }   // Schritt 26a: Auswahl, vorbelegt mit der angemeldeten Person
    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
    public Button HinzufuegenKnopf { get; } = Stil.NebenKnopf("Hinzufügen");
    public List<VisiteEintragFeld> Eintraege { get; } = [];
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(640, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Speicher.VisiteGespeichert? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VisiteFenster(Speicher speicher, long? bewohnerId = null)
    {
        this.speicher = speicher;
        bewohner = speicher.AlleBewohner();
        Arzt = new ArztWahl(speicher);
        medikamente = speicher.Medikamente().Where(m => !m.Gesperrt).ToList();
        Text = $"Neue Visite ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = inhalt.BackColor = unten.BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        void Zeile(string? beschriftung, Control feld)
        {
            if (beschriftung is null) { raster.Controls.Add(feld); raster.SetColumnSpan(feld, 2); return; }
            raster.Controls.Add(new Label { Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein) });
            feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            raster.Controls.Add(feld);
        }
        DatumFeld.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        Teilnehmer = new PersonenWahl(speicher) { AccessibleName = "Dabei" };
        BewohnerWahl.Items.AddRange([.. bewohner.Select(b => (object)$"{b.Name}, Zi. {b.Zimmer}")]);
        if (BewohnerWahl.Items.Count > 0) BewohnerWahl.SelectedIndex = 0;
        BewohnerWahl.AccessibleName = "Bewohner hinzufügen";
        HinzufuegenKnopf.Click += (_, _) => { if (BewohnerWahl.SelectedIndex >= 0) BewohnerHinzufuegen(bewohner[BewohnerWahl.SelectedIndex]); };

        Zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Zeile("Datum der Visite", DatumFeld);
        Zeile("Arzt", Arzt);
        Zeile("Dabei", Teilnehmer);
        Zeile("Bewohner", Formular.Reihe(BewohnerWahl, HinzufuegenKnopf));
        Zeile(null, eintraege);
        Zeile(null, Fenster.Hinweis(
            "Je Bewohner: was besprochen wurde, ob sich Anordnungen geändert haben und ob ein Facharzttermin nötig ist. " +
            "Nach dem Speichern zeigt das Programm sofort die Aufgaben – je geänderter Anordnung eine, je Facharzttermin eine. " +
            "Die Medikation ändert das Programm nicht selbst; das trägt eine berechtigte Person über die Aufgabe ein. " +
            "Je Bewohner entsteht ein Bericht (Lebensbereich 5)."));
        Zeile("Eingetragen von", Recht.KuerzelAnzeige());

        var speichern = Stil.HauptKnopf("Speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        speichern.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        knoepfe.Controls.AddRange([speichern, abbrechen]);
        unten.Controls.AddRange([Fehler, knoepfe]);
        inhalt.Controls.Add(raster);
        Controls.Add(inhalt);
        Controls.Add(unten);
        CancelButton = abbrechen;
        if (bewohner.FirstOrDefault(b => b.Id == bewohnerId) is Bewohner vorgewaehlt) BewohnerHinzufuegen(vorgewaehlt);
        Stil.DpiSkalierungEinschalten(this);
        ClientSize = new Size(Math.Max(raster.PreferredSize.Width, 700) + SystemInformation.VerticalScrollBarWidth, 640);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var flaeche = Screen.FromControl(this).WorkingArea;
        int rahmen = Height - ClientSize.Height;
        int breite = Math.Max(raster.GetPreferredSize(Size.Empty).Width, Stil.Px(this, 700)) + SystemInformation.VerticalScrollBarWidth;
        ClientSize = new Size(Math.Min(breite, flaeche.Width - 40), Math.Min(Stil.Px(this, 760), flaeche.Height - rahmen - 40));
        CenterToParent();
    }

    // Einen Bewohner aufnehmen (jeden nur einmal); für Tests auch über den Nachnamen.
    public VisiteEintragFeld BewohnerHinzufuegen(Bewohner b)
    {
        if (Eintraege.FirstOrDefault(e => e.Bewohner.Id == b.Id) is VisiteEintragFeld schon) return schon;
        var feld = new VisiteEintragFeld(b, medikamente) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) };
        feld.EntfernenGeklickt += () =>
        {
            Eintraege.Remove(feld);
            eintraege.Controls.Remove(feld);
            feld.Dispose();
        };
        Eintraege.Add(feld);
        eintraege.Controls.Add(feld);
        return feld;
    }

    public VisiteEintragFeld BewohnerHinzufuegen(string nachname) => BewohnerHinzufuegen(bewohner.First(b => b.Nachname == nachname));

    public bool Speichern()
    {
        try
        {
            var v = VisitenRegeln.AusEingabe(new VisiteEingabe
            {
                Datum = DatumFeld.Text, Arzt = Arzt.Text, Teilnehmer = Teilnehmer.AlsText, Kuerzel = Recht.Kuerzel,
                Eintraege = Eintraege.Select(e => e.Eingabe()).ToList(),
            }, Jetzt());
            Gespeichert = speicher.VisiteSpeichern(v);
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

// Ein Bewohner in der Visite: Besprochenes, „Anordnungen geändert“ mit Zeilen (Art und Medikament –
// aus der Datenbank oder Freitext) und „Facharzttermin nötig“ mit Fachrichtung und Dringlichkeit.
public sealed class VisiteEintragFeld : FlowLayoutPanel
{
    private readonly List<Medikament> medikamente;
    private readonly FlowLayoutPanel zeilenFlaeche = new() { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche, Visible = false };

    public Bewohner Bewohner { get; }
    public Button EntfernenKnopf { get; } = Stil.NebenKnopf("Entfernen");
    public TextBox Besprochen { get; } = new() { Multiline = true, Width = 600, Height = 64, ScrollBars = ScrollBars.Vertical };
    public CheckBox AnordnungenGeaendert { get; } = new() { Text = "Anordnungen geändert", AutoSize = true, BackColor = Stil.Flaeche };
    public List<AnordnungZeile> Zeilen { get; } = [];
    public Button WeitereKnopf { get; } = Stil.NebenKnopf("Weitere Anordnung");
    public CheckBox FacharztNoetig { get; } = new() { Text = "Facharzttermin nötig", AutoSize = true, BackColor = Stil.Flaeche };
    public TextBox Fachrichtung { get; } = new() { Width = 240, PlaceholderText = "Fachrichtung, z. B. Neurologie", Enabled = false };
    public CheckBox Dringend { get; } = new() { Text = "dringend", AutoSize = true, BackColor = Stil.Flaeche, Enabled = false };
    public event Action? EntfernenGeklickt;

    public VisiteEintragFeld(Bewohner b, List<Medikament> medikamente)
    {
        this.medikamente = medikamente;
        Bewohner = b;
        AutoSize = true;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        BackColor = Stil.Flaeche;
        Padding = new Padding(Stil.AbstandMittel);
        AccessibleName = $"Visite: {b.Name}";
        var name = new Label { Text = $"{b.Name}, Zi. {b.Zimmer}", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche, UseMnemonic = false, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, 0) };
        var kopf = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, Stil.AbstandKlein), BackColor = Stil.Flaeche };
        kopf.Controls.AddRange([name, EntfernenKnopf]);
        EntfernenKnopf.Click += (_, _) => EntfernenGeklickt?.Invoke();
        var besprochenText = new Label { Text = "Was wurde besprochen?", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, Margin = new Padding(0) };
        Besprochen.AccessibleName = $"Besprochen, {b.Name}";
        Besprochen.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandKlein);
        AnordnungenGeaendert.CheckedChanged += (_, _) =>
        {
            if (AnordnungenGeaendert.Checked && Zeilen.Count == 0) ZeileHinzufuegen();
            zeilenFlaeche.Visible = WeitereKnopf.Visible = AnordnungenGeaendert.Checked;
        };
        WeitereKnopf.Visible = false;
        WeitereKnopf.Margin = new Padding(0, 0, 0, Stil.AbstandKlein);
        WeitereKnopf.Click += (_, _) => ZeileHinzufuegen();
        FacharztNoetig.CheckedChanged += (_, _) => Fachrichtung.Enabled = Dringend.Enabled = FacharztNoetig.Checked;
        Fachrichtung.AccessibleName = $"Fachrichtung, {b.Name}";
        var facharzt = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
        foreach (var c in new Control[] { FacharztNoetig, Fachrichtung, Dringend }) c.Margin = new Padding(0, c is CheckBox ? Stil.AbstandMini : 0, Stil.AbstandKlein, 0);
        facharzt.Controls.AddRange([FacharztNoetig, Fachrichtung, Dringend]);
        Controls.AddRange([kopf, besprochenText, Besprochen, AnordnungenGeaendert, zeilenFlaeche, WeitereKnopf, facharzt]);
    }

    public AnordnungZeile ZeileHinzufuegen()
    {
        var zeile = new AnordnungZeile(medikamente);
        zeile.EntfernenGeklickt += () =>
        {
            Zeilen.Remove(zeile);
            zeilenFlaeche.Controls.Remove(zeile);
            zeile.Dispose();
        };
        Zeilen.Add(zeile);
        zeilenFlaeche.Controls.Add(zeile);
        return zeile;
    }

    public VisiteEintragEingabe Eingabe() => new()
    {
        BewohnerId = Bewohner.Id!.Value, Besprochen = Besprochen.Text,
        Anordnungen = AnordnungenGeaendert.Checked ? Zeilen.Select(z => z.Wert()).ToList() : [],
        FacharztNoetig = FacharztNoetig.Checked, Facharzt = FacharztNoetig.Checked ? Fachrichtung.Text : "",
        Dringend = FacharztNoetig.Checked && Dringend.Checked,
    };
}

// Eine geänderte Anordnung: Art (neu verordnet, Dosis geändert, pausiert, abgesetzt) und Medikament.
// Das Medikament kommt aus der Datenbank; steht es dort noch nicht, wird der Name frei eingetragen.
public sealed class AnordnungZeile : FlowLayoutPanel
{
    private readonly List<Medikament> medikamente;

    public ComboBox ArtWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    public ComboBox MedikamentWahl { get; } = new()
    {
        DropDownStyle = ComboBoxStyle.DropDown, Width = 340, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems,
    };
    public Button EntfernenKnopf { get; } = Stil.NebenKnopf("Entfernen");
    public event Action? EntfernenGeklickt;

    public AnordnungZeile(List<Medikament> medikamente)
    {
        this.medikamente = medikamente;
        AutoSize = true;
        WrapContents = false;
        BackColor = Stil.Flaeche;
        Margin = new Padding(0, 0, 0, Stil.AbstandMini);
        ArtWahl.Items.AddRange([.. Enum.GetValues<AnordnungArt>().Select(a => (object)VisitenRegeln.ArtText(a))]);
        ArtWahl.SelectedIndex = 0;
        ArtWahl.AccessibleName = "Art der Änderung";
        MedikamentWahl.Items.AddRange([.. medikamente.Select(m => (object)m.Auswahltext)]);
        MedikamentWahl.AccessibleName = "Medikament (aus der Liste oder frei eingeben)";
        EntfernenKnopf.Click += (_, _) => EntfernenGeklickt?.Invoke();
        foreach (var c in new Control[] { ArtWahl, MedikamentWahl, EntfernenKnopf }) c.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        Controls.AddRange([ArtWahl, MedikamentWahl, EntfernenKnopf]);
    }

    public void Setzen(AnordnungArt art, string medikament)
    {
        ArtWahl.SelectedIndex = (int)art;
        int i = medikamente.FindIndex(m => m.Wirkstoff.Equals(medikament, StringComparison.CurrentCultureIgnoreCase));
        if (i >= 0) MedikamentWahl.SelectedIndex = i;
        else MedikamentWahl.Text = medikament;
    }

    // Aus der Liste gewählt (oder genau so eingetippt): mit Datenbank-Eintrag; sonst Freitext.
    public (AnordnungArt Art, long? MedikamentId, string Medikament) Wert()
    {
        var art = (AnordnungArt)Math.Max(0, ArtWahl.SelectedIndex);
        string text = MedikamentWahl.Text.Trim();
        bool Gleich(string s) => s.Equals(text, StringComparison.CurrentCultureIgnoreCase);
        var m = MedikamentWahl.SelectedIndex >= 0 && (text.Length == 0 || Gleich(medikamente[MedikamentWahl.SelectedIndex].Auswahltext))
            ? medikamente[MedikamentWahl.SelectedIndex]
            : medikamente.FirstOrDefault(x => Gleich(x.Auswahltext) || Gleich(x.Anzeige) || Gleich(x.Wirkstoff) || x.Aktuell.Handelsnamen.Any(Gleich));
        return m is null ? (art, null, text) : (art, m.Id, m.Wirkstoff);
    }
}

// Einblendung gleich nach dem Speichern einer Visite: je Aufgabe ein Knopf („Medikament anlegen …“,
// „Im Medikamentenplan eintragen …“, „Termin eintragen …“). „Später“ schließt nur die Einblendung;
// die Aufgaben stehen weiter unter „Aufgaben“.
public sealed class VisitenAufgabenFenster : Form
{
    public List<Aufgabe> Aufgaben { get; }
    public List<Button> Knoepfe { get; } = [];
    public Button SpaeterKnopf { get; } = Stil.NebenKnopf("Später");
    public Aufgabe? Wunsch { get; private set; }

    public VisitenAufgabenFenster(Speicher speicher, IReadOnlyList<Aufgabe> aufgaben)
    {
        Aufgaben = [.. aufgaben];
        Fenster.Grundform(this, "Aufgaben aus der Visite");
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Aus der Visite sind diese Aufgaben entstanden. Sie stehen auch unter „Aufgaben“, bis sie erledigt sind; " +
                                    "die Übergabe nennt die Visite. „Später“ schließt nur diese Einblendung. Die Medikation ändert das Programm nicht selbst."));
        for (int i = 0; i < Aufgaben.Count; i++)
        {
            var a = Aufgaben[i];
            var info = speicher.VisitenAufgabe(a);
            var flaeche = new FlowLayoutPanel
            {
                AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Stil.Flaeche,
                Padding = new Padding(Stil.AbstandMittel), Margin = new Padding(0, Stil.AbstandKlein, 0, 0),
            };
            flaeche.Controls.Add(new Label { Text = a.Text, AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Fett, BackColor = Stil.Flaeche, UseMnemonic = false });
            flaeche.Controls.Add(new Label
            {
                Text = a.BewohnerId is long id ? namen.GetValueOrDefault(id) ?? "" : "", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise,
                BackColor = Stil.Flaeche, UseMnemonic = false, Margin = new Padding(0, 0, 0, Stil.AbstandKlein),
            });
            var knopf = Stil.NebenKnopf(info is { } x ? VisitenAblauf.KnopfText(x.Aktion) : "Öffnen …");
            Stil.Sperren(knopf, info is { } y ? VisitenAblauf.Sperrgrund(speicher, y.Aktion) : null);
            int nummer = i;
            knopf.Click += (_, _) => Waehlen(nummer);
            Knoepfe.Add(knopf);
            flaeche.Controls.Add(knopf);
            zeile(null, flaeche);
        }
        SpaeterKnopf.DialogResult = DialogResult.Cancel;
        SpaeterKnopf.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        zeile(null, SpaeterKnopf);
        CancelButton = SpaeterKnopf;
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Wie der Knopf; geht nicht, wenn das Recht fehlt.
    public bool Waehlen(int i)
    {
        if (Stil.Sperrgrund(Knoepfe[i]) is not null) return false;
        Wunsch = Aufgaben[i];
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Facharzttermin vereinbart (Aufgabe erledigen): Datum, Uhrzeit, Ort; der Termin kommt in den
// Kalender (Bereich) beim Bewohner, auf Wunsch eine Fahrt mit Fahrzeug. Danach ist die Aufgabe erledigt.
public sealed class FacharztTerminFenster : Form
{
    private readonly Speicher speicher;
    private readonly Aufgabe aufgabe;
    private readonly VisiteEintrag eintrag;
    private string? gewarntFuer;

    public TextBox DatumFeld { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox Uhrzeit { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "10:00" };
    public ArztWahl Arzt { get; }   // Schritt 26b: aus dem Verzeichnis, füllt den Ort
    public TextBox Ort { get; } = new() { Width = 320, PlaceholderText = "z. B. Praxis Dr. Esche (ausgedacht)" };
    public CheckBox MitFahrt { get; } = new() { Text = "Fahrt mit Fahrzeug eintragen", AutoSize = true };
    public ComboBox Fahrzeug { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Enabled = false };
    public TextBox Los { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "09:30", Enabled = false };
    public TextBox Zurueck { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "11:30", Enabled = false };
    public PersonWahl Fahrer { get; }
    public Label Warnung { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Button SpeichernKnopf { get; }
    public KalenderTermin? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public FacharztTerminFenster(Speicher speicher, Aufgabe aufgabe, VisiteEintrag eintrag)
    {
        this.speicher = speicher;
        this.aufgabe = aufgabe;
        this.eintrag = eintrag;
        Fenster.Grundform(this, "Facharzttermin eintragen");
        Fahrzeug.Items.AddRange([.. speicher.AktiveFahrzeuge()]);
        if (Fahrzeug.Items.Count > 0) Fahrzeug.SelectedIndex = 0;
        Fahrer = new PersonWahl(speicher) { Enabled = false };
        Arzt = new ArztWahl(speicher);
        Arzt.Gewechselt += a => { if (a is not null && a.Praxis.Length > 0) Ort.Text = a.Praxis; };
        MitFahrt.CheckedChanged += (_, _) => Fahrzeug.Enabled = Los.Enabled = Zurueck.Enabled = Fahrer.Enabled = MitFahrt.Checked;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label { Text = aufgabe.Text, AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Fett, UseMnemonic = false, Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        zeile("Datum, Uhrzeit", Formular.Reihe(DatumFeld, Uhrzeit));
        zeile("Arzt (freiwillig)", Arzt);
        zeile("Ort", Ort);
        zeile("", MitFahrt);
        zeile("Fahrzeug", Fahrzeug);
        zeile("los – zurück", Formular.Reihe(Los, Zurueck));
        zeile("Fahrer", Fahrer);
        zeile(null, Fenster.Hinweis("Der Termin kommt in den Kalender (Bereich) beim Bewohner. Mit Fahrt steht der Bewohner während der Fahrt " +
                                    "im Tagesplan als abwesend. Danach ist die Aufgabe erledigt."));
        zeile(null, Warnung);
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        SpeichernKnopf = knoepfe.Controls.OfType<Button>().First(b => b.Text == "Speichern");
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Wie im Fenster „Neue Fahrt“: Bei einer Doppelbuchung erst eine Warnung, beim zweiten Klick speichern.
    public bool Speichern()
    {
        try
        {
            var jetzt = Jetzt();
            string kuerzel = Recht.Kuerzel;
            DateOnly datum;
            try { datum = Kern.Datum.Lesen(DatumFeld.Text, "Datum"); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            Fahrt? fahrt = null;
            if (MitFahrt.Checked)
            {
                fahrt = FahrtRegeln.AusEingabe(new FahrtEingabe
                {
                    Fahrzeug = Fahrzeug.SelectedItem as string ?? "", Datum = DatumFeld.Text, Von = Los.Text, Bis = Zurueck.Text,
                    Zweck = FahrtZweck.Arzttermin, Fahrer = Fahrer.Kuerzel, Bewohner = [eintrag.BewohnerId], Ziel = Ort.Text, Kuerzel = kuerzel,
                }, speicher.AktiveFahrzeuge(), jetzt);
                var andere = speicher.FahrtUeberschneidungen(fahrt);
                string schluessel = $"{fahrt.Fahrzeug}|{fahrt.Von:O}|{fahrt.Bis:O}";
                if (andere.Count > 0 && gewarntFuer != schluessel)
                {
                    gewarntFuer = schluessel;
                    Warnung.Text = $"Achtung, Doppelbuchung: Der {fahrt.Fahrzeug} ist zu dieser Zeit schon eingeplant – " +
                                   string.Join("; ", andere.Select(FahrtRegeln.Zeile)) + ". Trotzdem speichern? Dann noch einmal „Trotzdem speichern“.";
                    Warnung.Visible = true;
                    SpeichernKnopf.Text = "Trotzdem speichern";
                    Fehler.Visible = false;
                    return false;
                }
            }
            Gespeichert = speicher.FacharztTerminEintragen(aufgabe, datum, Uhrzeit.Text, Ort.Text, kuerzel, jetzt, fahrt, Arzt.GewaehlterArzt);
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

// Was der Knopf einer Visiten-Aufgabe heißt und wann er gesperrt ist (Rechte, Schritt 19).
public static class VisitenAblauf
{
    public static string KnopfText(VisitenAktion a) => a switch
    {
        VisitenAktion.MedikamentAnlegen => "Medikament anlegen …",
        VisitenAktion.MedikationAendern => "Im Medikamentenplan eintragen …",
        _ => "Termin eintragen …",
    };

    public static string? Sperrgrund(Speicher speicher, VisitenAktion a) => a switch
    {
        VisitenAktion.MedikamentAnlegen => Recht.Grund(speicher, Funktionen.MedikamentAnlegen) ?? Recht.Grund(speicher, Funktionen.MedikationBearbeiten),
        VisitenAktion.MedikationAendern => Recht.Grund(speicher, Funktionen.MedikationBearbeiten),
        _ => Recht.Kuerzel.Length == 0 ? "Bitte zuerst oben rechts anmelden." : null,
    };
}
