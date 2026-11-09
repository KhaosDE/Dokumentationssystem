using DokuLine.Kern;

namespace DokuLine;

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
    public ArztWahl Arzt { get; }   // Auswahl aus dem Ärzteverzeichnis
    public PersonenWahl Teilnehmer { get; }   // Auswahl, vorbelegt mit der angemeldeten Person
    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
    public Button HinzufuegenKnopf { get; } = Stil.NebenKnopf("Hinzufügen");
    public List<VisiteEintragFeld> Eintraege { get; } = [];
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(640, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Speicher.VisiteGespeichert? Gespeichert { get; private set; }
    public string Art { get; }   // Psychiater oder Hausarzt (Umschalter der Visitenseite)

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VisiteFenster(Speicher speicher, long? bewohnerId = null, string art = VisitenRegeln.Psychiater)
    {
        this.speicher = speicher;
        Art = art;
        bewohner = speicher.AlleBewohner();
        Arzt = new ArztWahl(speicher);
        medikamente = speicher.Medikamente().Where(m => !m.Gesperrt).ToList();
        Text = $"Neue Visite {art} ({Stil.DemoHinweis})";
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
                Datum = DatumFeld.Text, Art = Art, Arzt = Arzt.Text, Teilnehmer = Teilnehmer.AlsText, Kuerzel = Recht.Kuerzel,
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
