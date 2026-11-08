using DokuLine.Kern;

namespace DokuLine;

// Fahrzeuge (Bauplan Schritt 24): je Fahrzeug „vor Ort“ oder „unterwegs seit …, zurück geplant …“
// mit den Knöpfen „losgefahren“ und „zurück“ (Uhrzeit und Kürzel setzt das Programm), darunter die
// Fahrten von heute; „Neue Fahrt …“. Steht in der Übersicht und im Kalender.
public sealed class FahrzeugLeiste : FlowLayoutPanel
{
    private readonly Speicher speicher;

    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Fahrt …");
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public List<FahrzeugZeile> Zeilen { get; } = [];
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public FahrzeugLeiste(Speicher speicher)
    {
        this.speicher = speicher;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoSize = true;
        BackColor = Stil.Flaeche;
        Margin = new Padding(0);
        NeuKnopf.AutoSize = true;
        NeuKnopf.Click += (_, _) => NeueFahrt();
    }

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        string? grund = Recht.Grund(speicher, Funktionen.Fahrzeuge);   // Rechte (Schritt 19)
        SuspendLayout();
        foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); if (c != NeuKnopf && c != Fehler) c.Dispose(); }
        Controls.Add(Fehler);
        Zeilen.Clear();
        var heute = DateOnly.FromDateTime(jetzt);
        var fahrten = speicher.Fahrten(heute, heute);
        foreach (var stand in speicher.FahrzeugStaende(jetzt))
        {
            var zeile = new FahrzeugZeile(stand, fahrten.Where(f => f.Fahrzeug == stand.Fahrzeug && f.Zurueck is null).ToList());
            Stil.Sperren(zeile.LosKnopf, grund);
            Stil.Sperren(zeile.ZurueckKnopf, grund);
            zeile.LosKnopf.Click += (_, _) => { if (stand.Naechste is Fahrt n) Ausfuehren(() => speicher.Losgefahren(n.Id, Recht.Kuerzel, Jetzt())); };
            zeile.ZurueckKnopf.Click += (_, _) => { if (stand.Aktuell is Fahrt a) Ausfuehren(() => speicher.Zurueck(a.Id, Recht.Kuerzel, Jetzt())); };
            Zeilen.Add(zeile);
            Controls.Add(zeile);
        }
        if (Zeilen.Count == 0)
            Controls.Add(new Label { Text = "Keine Fahrzeuge eingetragen (Einstellungen → Fahrzeuge).", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche });
        Stil.Sperren(NeuKnopf, grund);
        NeuKnopf.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        Controls.Add(NeuKnopf);
        ResumeLayout();
    }

    private void Ausfuehren(Action aktion)
    {
        string? fehler = null;
        try { aktion(); }
        catch (Eingabefehler e) { fehler = string.Join(" ", e.Fehler); }
        Aktualisieren();
        Fehler.Text = fehler ?? "";
        Fehler.Visible = fehler is not null;
        Geaendert?.Invoke();
    }

    // Für Tests und Tastatur: dasselbe wie die Knöpfe.
    public void Losfahren(string fahrzeug) { if (Zeilen.Single(z => z.Stand.Fahrzeug == fahrzeug).Stand.Naechste is Fahrt n) Ausfuehren(() => speicher.Losgefahren(n.Id, Recht.Kuerzel, Jetzt())); }
    public void Zurueck(string fahrzeug) { if (Zeilen.Single(z => z.Stand.Fahrzeug == fahrzeug).Stand.Aktuell is Fahrt a) Ausfuehren(() => speicher.Zurueck(a.Id, Recht.Kuerzel, Jetzt())); }

    public void NeueFahrt()
    {
        using var fenster = new FahrtFenster(speicher);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK)
        {
            Aktualisieren();
            Geaendert?.Invoke();
        }
    }
}

// Ein Fahrzeug: Name, Stand (farbig: vor Ort grün, unterwegs blaugrün, überschritten rot), Knopf
// „losgefahren“ (für die nächste Fahrt von heute) oder „zurück“, darunter die offenen Fahrten von heute.
public sealed class FahrzeugZeile : FlowLayoutPanel
{
    public FahrzeugStand Stand { get; }
    public MarkenFeld StandFeld { get; }
    public Button LosKnopf { get; } = Stil.NebenKnopf("losgefahren");
    public Button ZurueckKnopf { get; } = Stil.NebenKnopf("zurück");
    public Label Fahrten { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };

    public FahrzeugZeile(FahrzeugStand stand, IReadOnlyList<Fahrt> heute)
    {
        Stand = stand;
        FlowDirection = FlowDirection.LeftToRight;
        WrapContents = true;
        AutoSize = true;
        BackColor = Stil.Flaeche;
        Margin = new Padding(0, 0, 0, Stil.AbstandMini);
        var name = new Label { Text = stand.Fahrzeug, AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, MinimumSize = new Size(80, 0), Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) };
        StandFeld = new MarkenFeld(stand.Text, stand.Ueberfaellig ? Stil.Beschluss : stand.Unterwegs ? Stil.Unterwegs : Stil.Offen)
            { Margin = new Padding(0, Stil.AbstandMini + 2, Stil.AbstandKlein, 0) };
        foreach (var k in new[] { LosKnopf, ZurueckKnopf }) { k.AutoSize = true; k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0); }
        LosKnopf.Visible = !stand.Unterwegs && stand.Naechste is not null;
        ZurueckKnopf.Visible = stand.Unterwegs;
        var offene = heute.Where(f => f != stand.Aktuell).Select(FahrtRegeln.Zeile).ToList();
        Fahrten.Text = offene.Count > 0 ? "heute: " + string.Join(" · ", offene) : "";
        Fahrten.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        Fahrten.Visible = offene.Count > 0;
        Controls.AddRange([name, StandFeld, LosKnopf, ZurueckKnopf, Fahrten]);
        AccessibleName = $"{stand.Fahrzeug}: {stand.Text}";
    }
}

// Fahrt eintragen: Fahrzeug, Datum, los und zurück (geplant), Zweck, Fahrer, mitfahrende Bewohner,
// Ziel. Überschneidet sie sich mit einer anderen Fahrt desselben Fahrzeugs: Warnung, Speichern erst
// beim zweiten Klick („Trotzdem speichern“).
public sealed class FahrtFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> bewohner;
    private string? gewarntFuer;

    public ComboBox Fahrzeug { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    public TextBox Datum { get; } = new DatumFeld() { Width = 100 };
    public TextBox Von { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "09:00" };
    public TextBox Bis { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "11:00" };
    public ComboBox Zweck { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    public PersonWahl Fahrer { get; }   // Schritt 26a: Auswahl aus den Mitarbeitern, vorbelegt mit der angemeldeten Person
    public CheckedListBox Mitfahrende { get; } = new() { Width = 300, Height = 150, CheckOnClick = true, IntegralHeight = false };
    public TextBox Ziel { get; } = new() { Width = 300, PlaceholderText = "z. B. Praxis, Hallenbad (ausgedacht)" };
    public Label Warnung { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Button SpeichernKnopf { get; private set; } = null!;
    public Fahrt? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public FahrtFenster(Speicher speicher)
    {
        this.speicher = speicher;
        bewohner = speicher.AlleBewohner();
        Fenster.Grundform(this, "Neue Fahrt");
        var jetzt = Jetzt();
        Fahrzeug.Items.AddRange([.. speicher.AktiveFahrzeuge()]);
        if (Fahrzeug.Items.Count > 0) Fahrzeug.SelectedIndex = 0;
        Datum.Text = Kern.Datum.Text(DateOnly.FromDateTime(jetzt));
        Zweck.Items.AddRange([.. Enum.GetValues<FahrtZweck>().Select(FahrtRegeln.ZweckText)]);
        Zweck.SelectedIndex = 0;
        Fahrer = new PersonWahl(speicher);
        Mitfahrende.Items.AddRange([.. bewohner.Select(b => b.Name)]);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Fahrzeug", Fahrzeug);
        zeile("Datum, los – zurück", Formular.Reihe(Datum, Von, Bis));
        zeile("Zweck", Zweck);
        zeile("Fahrer", Fahrer);
        zeile("Mitfahrende Bewohner", Mitfahrende);
        zeile("Ziel", Ziel);
        zeile(null, Fenster.Hinweis("Die Zeiten sind geplant. „losgefahren“ und „zurück“ trägt man in der Übersicht oder im Kalender ein. Mitfahrende stehen in ihrem Tagesplan und während der Fahrt als abwesend."));
        zeile(null, Warnung);
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        SpeichernKnopf = knoepfe.Controls.OfType<Button>().First(b => b.Text == "Speichern");
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public void BewohnerWaehlen(params string[] nachnamen)
    {
        for (int i = 0; i < bewohner.Count; i++) Mitfahrende.SetItemChecked(i, nachnamen.Contains(bewohner[i].Nachname));
    }

    // Erster Klick bei einer Überschneidung: Warnung; zweiter Klick (gleiche Angaben): speichern.
    public bool Speichern()
    {
        try
        {
            var f = FahrtRegeln.AusEingabe(new FahrtEingabe
            {
                Fahrzeug = Fahrzeug.SelectedItem as string ?? "", Datum = Datum.Text, Von = Von.Text, Bis = Bis.Text,
                Zweck = Enum.GetValues<FahrtZweck>()[Math.Max(0, Zweck.SelectedIndex)], Fahrer = Fahrer.Kuerzel,
                Bewohner = Mitfahrende.CheckedIndices.Cast<int>().Select(i => bewohner[i].Id!.Value).ToList(), Ziel = Ziel.Text,
                Kuerzel = Recht.Kuerzel,
            }, speicher.AktiveFahrzeuge(), Jetzt());
            var andere = speicher.FahrtUeberschneidungen(f);
            string schluessel = $"{f.Fahrzeug}|{f.Von:O}|{f.Bis:O}";
            if (andere.Count > 0 && gewarntFuer != schluessel)
            {
                gewarntFuer = schluessel;
                Warnung.Text = $"Achtung, Doppelbuchung: Der {f.Fahrzeug} ist zu dieser Zeit schon eingeplant – " +
                               string.Join("; ", andere.Select(FahrtRegeln.Zeile)) + ". Trotzdem speichern? Dann noch einmal „Trotzdem speichern“.";
                Warnung.Visible = true;
                SpeichernKnopf.Text = "Trotzdem speichern";
                Fehler.Visible = false;
                return false;
            }
            Gespeichert = speicher.FahrtAnlegen(f);
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

// Fahrzeugliste (Einstellungen): hinzufügen, stilllegen und wieder aktivieren. Gelöscht wird nicht –
// frühere Fahrten behalten ihr Fahrzeug.
public sealed class FahrzeugeFenster : Form
{
    private readonly Speicher speicher;

    public ListBox Liste { get; } = new() { Width = 300, Height = 160, IntegralHeight = false, Font = Stil.Standard };
    public TextBox Name_ { get; } = new() { Width = 220, PlaceholderText = "z. B. Kleinbus" };
    public Button HinzufuegenKnopf { get; } = Stil.NebenKnopf("Hinzufügen");
    public Button UmschaltenKnopf { get; } = Stil.NebenKnopf("Stilllegen / wieder aktiv");
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public FahrzeugeFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Fahrzeuge");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Firmenwagen für Fahrten (Arzt, Ausflug, Sporttherapie). Stillgelegte stehen nicht mehr zur Wahl; frühere Fahrten bleiben."));
        zeile(null, Liste);
        zeile("Name", Formular.Reihe(Name_, HinzufuegenKnopf));
        zeile(null, UmschaltenKnopf);
        zeile(null, Fehler);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        zeile(null, schliessen);
        CancelButton = schliessen;
        Controls.Add(raster);
        string? grund = Recht.Grund(speicher, Funktionen.Fahrzeuge);
        foreach (var k in new[] { HinzufuegenKnopf, UmschaltenKnopf }) { k.AutoSize = true; Stil.Sperren(k, grund); }
        HinzufuegenKnopf.Click += (_, _) => Hinzufuegen();
        UmschaltenKnopf.Click += (_, _) => Umschalten();
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        Liste.Items.Clear();
        foreach (var f in speicher.Fahrzeuge()) Liste.Items.Add(f.Aktiv ? f.Name : $"{f.Name} (stillgelegt)");
    }

    public bool Hinzufuegen()
    {
        try { speicher.FahrzeugAnlegen(Name_.Text); }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        Fehler.Visible = false;
        Name_.Text = "";
        Fuellen();
        return true;
    }

    public bool Umschalten()
    {
        if (Liste.SelectedIndex < 0) { Fehler.Text = "Bitte zuerst ein Fahrzeug wählen."; Fehler.Visible = true; return false; }
        var f = speicher.Fahrzeuge()[Liste.SelectedIndex];
        speicher.FahrzeugAktiv(f.Name, !f.Aktiv);
        Fehler.Visible = false;
        Fuellen();
        return true;
    }
}
