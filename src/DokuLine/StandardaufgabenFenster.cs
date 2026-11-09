using DokuLine.Kern;

namespace DokuLine;

// Begründet schließen (Bauplan Schritt 25): Eine Standardaufgabe wurde nicht erledigt und ist nicht
// mehr zu tun – mit kurzer Begründung, Kürzel und Uhrzeit.
public sealed class SchliessenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Aufgabe aufgabe;

    public TextBox Begruendung { get; } = new() { Width = 440, MaxLength = 200, PlaceholderText = "z. B. „Notfall im anderen Bereich, keine Zeit“" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public SchliessenFenster(Speicher speicher, Aufgabe aufgabe)
    {
        this.speicher = speicher;
        this.aufgabe = aufgabe;
        Fenster.Grundform(this, "Aufgabe schließen");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = aufgabe.Text, AutoSize = true, MaximumSize = new Size(440, 0), Font = Stil.Fett, UseMnemonic = false });
        zeile(null, Fenster.Hinweis("Nicht erledigt, aber nicht mehr zu tun. Die Begründung steht danach bei der Aufgabe."));
        zeile("Begründung", Begruendung);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Schließen", () => Uebernehmen()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Uebernehmen()
    {
        try { speicher.AufgabeSchliessen(aufgabe.Id, Recht.Kuerzel, Begruendung.Text, Jetzt()); }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Standardaufgaben verwalten (Schritt 25): Liste mit Schicht, Tagen und Uhrzeit; neu, ändern,
// stilllegen. Verwalten darf, wer „Aufgaben-Vorlagen verwalten“ hat; ansehen dürfen alle.
public sealed class VorlagenFenster : Form
{
    private readonly Speicher speicher;
    private List<AufgabeVorlage> vorlagen = [];

    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 760, Height = 320, Font = Stil.Standard,
    };
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neue Standardaufgabe …");
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern …");
    public Button UmschaltenKnopf { get; } = Stil.NebenKnopf("Stilllegen / wieder aktiv");

    public VorlagenFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Standardaufgaben");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Wiederkehrende Aufgaben je Schicht. Zu Beginn jeder Schicht entstehen daraus die Aufgaben dieser Schicht " +
                                    "(an den gewählten Wochentagen). Nicht erledigte bleiben als „aus der … offen“ stehen."));
        foreach (var (titel, breite) in new[] { ("Aufgabe", 280), ("Bereich", 70), ("Schicht", 100), ("Tage", 120), ("Uhrzeit", 60),
                                                ("Bewohner", 110), ("Status", 70) })
            Liste.Columns.Add(titel, breite);
        Liste.DoubleClick += (_, _) => Aendern();
        zeile(null, Liste);
        string? grund = Recht.Grund(speicher, Funktionen.AufgabenVorlagen);
        foreach (var k in new[] { NeuKnopf, AendernKnopf, UmschaltenKnopf }) { k.AutoSize = true; Stil.Sperren(k, grund); }
        NeuKnopf.Click += (_, _) => Bearbeiten(null);
        AendernKnopf.Click += (_, _) => Aendern();
        UmschaltenKnopf.Click += (_, _) => Umschalten();
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        zeile(null, Formular.Reihe(NeuKnopf, AendernKnopf, UmschaltenKnopf, schliessen));
        CancelButton = schliessen;
        Controls.Add(raster);
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        vorlagen = [.. speicher.Vorlagen().Where(v => BereichswahlRegeln.Passt(v.Bereich, Arbeitsbereich.Gewaehlt))];
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        Liste.Items.Clear();
        foreach (var v in vorlagen)
            Liste.Items.Add(new ListViewItem([v.Text, v.Bereich, BerichtRegeln.SchichtText(v.Schicht), AufgabenRegeln.TageText(v.Tage),
                v.Uhrzeit?.ToString("HH:mm") ?? "", v.BewohnerId is long b ? namen.GetValueOrDefault(b, "?") : "", v.Aktiv ? "aktiv" : "stillgelegt"]));
    }

    private AufgabeVorlage? Gewaehlt => Liste.SelectedIndices.Count == 1 ? vorlagen[Liste.SelectedIndices[0]] : null;

    public void Waehlen(int index) { Liste.SelectedIndices.Clear(); Liste.SelectedIndices.Add(index); }

    private void Aendern() { if (Gewaehlt is AufgabeVorlage v) Bearbeiten(v); }

    public void Bearbeiten(AufgabeVorlage? v)
    {
        if (Recht.Grund(speicher, Funktionen.AufgabenVorlagen) is not null) return;
        using var fenster = new VorlageFenster(speicher, v);
        if (Dialog.Zeigen(fenster, this) == DialogResult.OK) Fuellen();
    }

    public void Umschalten()
    {
        if (Gewaehlt is not AufgabeVorlage v || Recht.Grund(speicher, Funktionen.AufgabenVorlagen) is not null) return;
        speicher.VorlageAktiv(v.Id, !v.Aktiv);
        Fuellen();
    }
}

// Eine Standardaufgabe anlegen oder ändern: Text, Schicht, Wochentage (oder täglich), Uhrzeit,
// optional ein Bewohner.
public sealed class VorlageFenster : Form
{
    private static readonly DayOfWeek[] Woche =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
    private readonly Speicher speicher;
    private readonly AufgabeVorlage? bisher;
    private readonly List<Bewohner> bewohner;

    public TextBox TextFeld { get; } = new() { Width = 420 };
    public ComboBox SchichtWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    public CheckBox Taeglich { get; } = new() { Text = "täglich", AutoSize = true, Checked = true };
    public CheckBox[] Tage { get; } = Woche.Select(t => new CheckBox
    { Text = System.Globalization.CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetAbbreviatedDayName(t), AutoSize = true, Checked = true }).ToArray();
    public TextBox Uhrzeit { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "optional" };
    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, AccessibleName = "Bereich" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VorlageFenster(Speicher speicher, AufgabeVorlage? bisher)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        bewohner = speicher.AlleBewohner();
        Fenster.Grundform(this, bisher is null ? "Neue Standardaufgabe" : "Standardaufgabe ändern");
        SchichtWahl.Items.AddRange([.. Enum.GetValues<Schicht>().Select(BerichtRegeln.SchichtText)]);
        SchichtWahl.SelectedIndex = (int)Schicht.Nacht;
        BewohnerWahl.Items.Add(AufgabeFenster.FuerBereich);
        BewohnerWahl.Items.AddRange([.. bewohner.Select(b => b.Name)]);
        BewohnerWahl.SelectedIndex = 0;
        string[] bereiche = [.. speicher.Wohnbereiche().Select(w => w.Name), TherapieRegeln.AnbieterTherapie];
        BereichWahl.Items.AddRange(bereiche);
        string vorgabe = bisher is { Bereich.Length: > 0 } ? bisher.Bereich
            : Arbeitsbereich.Gewaehlt is { Length: > 0 } g && bereiche.Contains(g) ? g : Einstellungen.Laden().Bereich;
        BereichWahl.SelectedIndex = Math.Max(0, Array.IndexOf(bereiche, vorgabe));
        BewohnerWahl.SelectedIndexChanged += (_, _) => BereichWahl.Enabled = BewohnerWahl.SelectedIndex == 0;
        Taeglich.CheckedChanged += (_, _) => { foreach (var t in Tage) { t.Checked = Taeglich.Checked || t.Checked; t.Enabled = !Taeglich.Checked; } };
        foreach (var t in Tage) t.Enabled = false;
        var raster = Formular.Raster(out var zeile);
        zeile("Was ist zu tun?", TextFeld);
        zeile("Schicht", SchichtWahl);
        zeile("Wochentage", Formular.Reihe([Taeglich, .. Tage]));
        zeile("Uhrzeit", Uhrzeit);
        zeile("Bewohner", BewohnerWahl);
        zeile("Bereich", BereichWahl);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        if (bisher is not null)
        {
            TextFeld.Text = bisher.Text;
            SchichtWahl.SelectedIndex = (int)bisher.Schicht;
            Taeglich.Checked = bisher.Tage.Count == 7;
            for (int i = 0; i < 7; i++) Tage[i].Checked = bisher.Tage.Contains(Woche[i]);
            Uhrzeit.Text = bisher.Uhrzeit?.ToString("HH:mm") ?? "";
            BewohnerWahl.SelectedIndex = bisher.BewohnerId is long b ? bewohner.FindIndex(x => x.Id == b) + 1 : 0;
        }
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            var v = AufgabenRegeln.VorlageAusEingabe(new VorlageEingabe
            {
                Text = TextFeld.Text, Schicht = (Schicht)SchichtWahl.SelectedIndex,
                Tage = Taeglich.Checked ? [.. Woche] : Woche.Where((_, i) => Tage[i].Checked).ToList(), Uhrzeit = Uhrzeit.Text,
                BewohnerId = BewohnerWahl.SelectedIndex > 0 ? bewohner[BewohnerWahl.SelectedIndex - 1].Id : null, Kuerzel = Recht.Kuerzel,
                Bereich = BewohnerWahl.SelectedIndex > 0 ? "" : BereichWahl.SelectedItem as string ?? "",
            }, Jetzt());
            speicher.VorlageSpeichern(bisher is null ? v : v with { Id = bisher.Id, Aktiv = bisher.Aktiv });
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
