using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// „Neue Aufgabe“: was, für welchen Bewohner (oder keinen), für wen, bis wann, von wem.
public sealed class AufgabeFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> auswahl;
    private readonly long? berichtId;

    public TextBox Inhalt { get; } = new() { Multiline = true, Width = 420, Height = 54, AcceptsReturn = false };
    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    // Ohne Bewohner gehört die Aufgabe zu einem Bereich (Vorgabe: der in der Seitenleiste gewählte).
    public ComboBox BereichWahl { get; } = new()
    { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, AccessibleName = "Bereich" };
    public const string FuerBereich = "– für einen Bereich –";
    public RadioButton NaechsteSchicht { get; } = new() { AutoSize = true, Checked = true };
    public RadioButton BestimmteSchicht { get; } = new() { Text = "bestimmte Schicht:", AutoSize = true };
    public RadioButton WerZeitHat { get; } = new() { Text = "wer Zeit hat", AutoSize = true };
    public TextBox SchichtTag { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public ComboBox SchichtArt { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    public TextBox FaelligDatum { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox FaelligUhrzeit { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "SS:MM" };
    public Label Verfasser { get; } = Recht.KuerzelAnzeige();   // aus der Anmeldung
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Aufgabe? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    // bewohnerId: vorgewählt (z. B. aus einem Bericht heraus); berichtId: Herkunft.
    public AufgabeFenster(Speicher speicher, long? bewohnerId, long? berichtId)
    {
        this.speicher = speicher;
        this.berichtId = berichtId;
        auswahl = speicher.AlleBewohner();
        Text = $"Neue Aufgabe ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        void Zeile(string? beschriftung, Control feld)
        {
            if (beschriftung is null) { raster.Controls.Add(feld); raster.SetColumnSpan(feld, 2); return; }
            raster.Controls.Add(new Label { Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein) });
            feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            raster.Controls.Add(feld);
        }
        FlowLayoutPanel Reihe(params Control[] teile)
        {
            var reihe = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            foreach (var t in teile) t.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
            reihe.Controls.AddRange(teile);
            return reihe;
        }

        Zeile(null, new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Zeile("Was ist zu tun?", Inhalt);
        BewohnerWahl.Items.Add(FuerBereich);
        BewohnerWahl.Items.AddRange(auswahl.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
        BewohnerWahl.SelectedIndex = bewohnerId is long id ? auswahl.FindIndex(b => b.Id == id) + 1 : 0;
        Zeile("Bewohner", BewohnerWahl);
        string[] bereiche = [.. speicher.Wohnbereiche().Select(w => w.Name), TherapieRegeln.AnbieterTherapie];
        BereichWahl.Items.AddRange(bereiche);
        string vorgabe = Arbeitsbereich.Gewaehlt is { Length: > 0 } g && bereiche.Contains(g)
            ? g : Einstellungen.Laden().Bereich;
        BereichWahl.SelectedIndex = Math.Max(0, Array.IndexOf(bereiche, vorgabe));
        BereichWahl.Enabled = BewohnerWahl.SelectedIndex == 0;
        BewohnerWahl.SelectedIndexChanged += (_, _) => BereichWahl.Enabled = BewohnerWahl.SelectedIndex == 0;
        Zeile("Bereich", BereichWahl);

        var jetzt = Jetzt();
        NaechsteSchicht.Text = $"nächste Schicht ({SchichtTermin.Zu(jetzt).Naechste().Text})";
        var morgen = SchichtTermin.Zu(jetzt).Naechste();
        SchichtTag.Text = Kern.Datum.Text(morgen.Tag);
        SchichtArt.Items.AddRange(["Frühdienst", "Spätdienst", "Nachtdienst"]);
        SchichtArt.SelectedIndex = (int)morgen.Schicht;
        SchichtTag.Enabled = SchichtArt.Enabled = false;
        // Die drei Knöpfe liegen in verschiedenen Reihen; Windows Forms verbindet sie dann nicht von selbst.
        RadioButton[] fuerKnoepfe = [NaechsteSchicht, BestimmteSchicht, WerZeitHat];
        foreach (var knopf in fuerKnoepfe)
            knopf.CheckedChanged += (_, _) =>
            {
                if (knopf.Checked) foreach (var anderer in fuerKnoepfe) if (anderer != knopf) anderer.Checked = false;
                SchichtTag.Enabled = SchichtArt.Enabled = BestimmteSchicht.Checked;
            };
        var fuer = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        fuer.Controls.Add(NaechsteSchicht);
        fuer.Controls.Add(Reihe(BestimmteSchicht, SchichtTag, SchichtArt));
        fuer.Controls.Add(WerZeitHat);
        Zeile("Für wen?", fuer);
        // Im Bereich Therapie ohne Schichtbezug (die Therapie arbeitet tagsüber), dafür mit Fälligkeit: morgen.
        bool therapie = TherapieListe.Aktiv;
        if (therapie)
        {
            WerZeitHat.Checked = true;
            NaechsteSchicht.Enabled = BestimmteSchicht.Enabled = false;
            FaelligDatum.Text = Kern.Datum.Text(DateOnly.FromDateTime(jetzt).AddDays(1));
        }
        Zeile(therapie ? "Fällig bis" : "Fällig bis (freiwillig)", Reihe(FaelligDatum, FaelligUhrzeit));
        Zeile("Eingetragen von", Verfasser);
        Zeile(null, Fehler);

        var speichern = Stil.HauptKnopf("Speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        speichern.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([speichern, abbrechen]);
        Zeile(null, knoepfe);
        Controls.Add(raster);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public AufgabeEingabe Eingabe() => new()
    {
        Text = Inhalt.Text,
        BewohnerId = BewohnerWahl.SelectedIndex > 0 ? auswahl[BewohnerWahl.SelectedIndex - 1].Id : null,
        Bereich = BewohnerWahl.SelectedIndex > 0 ? "" : BereichWahl.SelectedItem as string ?? "",
        BerichtId = berichtId,
        Fuer = BestimmteSchicht.Checked ? AufgabeFuer.Schicht : WerZeitHat.Checked ? AufgabeFuer.WerZeitHat : AufgabeFuer.NaechsteSchicht,
        SchichtTag = SchichtTag.Text,
        SchichtArt = (Schicht)Math.Max(0, SchichtArt.SelectedIndex),
        FaelligDatum = FaelligDatum.Text,
        FaelligUhrzeit = FaelligUhrzeit.Text,
        Verfasser = Recht.Kuerzel,
    };

    public bool Speichern()
    {
        try
        {
            Gespeichert = speicher.AufgabeSpeichern(AufgabenRegeln.AusEingabe(Eingabe(), Jetzt()));
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

// „An die nächste Schicht weitergeben“: mit kurzer Notiz, warum es nicht geschafft wurde.
public sealed class WeitergebenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Aufgabe aufgabe;
    public TextBox Notiz { get; } = new() { Width = 440, MaxLength = 200, PlaceholderText = "kurze Notiz (freiwillig), z. B. „Praxis war schon geschlossen“" };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public WeitergebenFenster(Speicher speicher, Aufgabe aufgabe)
    {
        this.speicher = speicher;
        this.aufgabe = aufgabe;
        Text = $"Aufgabe weitergeben ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        stapel.Controls.Add(new Label { Text = aufgabe.Text, AutoSize = true, MaximumSize = new Size(440, 0), Font = Stil.Fett, UseMnemonic = false });
        stapel.Controls.Add(new Label
        {
            Text = $"geht an die nächste Schicht ({SchichtTermin.Zu(Jetzt()).Naechste().Text})",
            AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandKlein),
        });
        stapel.Controls.Add(Notiz);
        stapel.Controls.Add(Fehler);
        var ok = Stil.HauptKnopf("Weitergeben");
        ok.Click += (_, _) => Uebernehmen();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([ok, abbrechen]);
        stapel.Controls.Add(knoepfe);
        Controls.Add(stapel);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Uebernehmen()
    {
        try
        {
            speicher.AufgabeWeitergeben(aufgabe.Id, Recht.Kuerzel, Notiz.Text, Jetzt());
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join("\n", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
