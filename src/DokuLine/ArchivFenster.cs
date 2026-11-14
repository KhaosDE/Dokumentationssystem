using DokuLine.Kern;

namespace DokuLine;

// „Entlassen …“ (Bauplan 46): Datum und Grund; was endet (offene Aufgaben, Maßnahmen, Therapien), steht zum Bestätigen da.
// Danach ist die Akte im Archiv: nicht mehr in Listen, Zählern und der Belegung.
public sealed class EntlassenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;

    public TextBox EntlassDatum { get; } = new DatumFeld { Width = 100, AccessibleName = "Entlassdatum" };
    public ComboBox Grund { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, AccessibleName = "Grund" };
    public TextBox Bemerkung { get; } = new() { Width = 420, AccessibleName = "Bemerkung (freiwillig)" };
    public Label Folgen { get; } = new() { AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.TextMittel, UseMnemonic = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public EntlassenFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Entlassen: {bewohner.Name}");
        var heute = DateOnly.FromDateTime(Jetzt());
        EntlassDatum.Text = Datum.Text(heute);
        Grund.Items.AddRange([.. ArchivRegeln.Gruende.Select(ArchivRegeln.GrundText)]);
        Grund.SelectedIndex = 0;
        FolgenZeigen(heute);
        EntlassDatum.Leave += (_, _) => { try { FolgenZeigen(Datum.Lesen(EntlassDatum.Text, "Entlassdatum")); } catch (FormatException) { } };
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Entlassen am", EntlassDatum);
        zeile("Grund", Grund);
        zeile("Bemerkung", Bemerkung);
        zeile("Das endet", Folgen);
        zeile(null, Fenster.Hinweis("Die Akte wandert ins Archiv und bleibt dort lesbar bis zum Ende der Aufbewahrungsfrist. Das Bett wird frei."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Entlassen", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private void FolgenZeigen(DateOnly datum)
    {
        var folgen = speicher.EntlassFolgen(bewohner.Id!.Value, datum);
        Folgen.Text = folgen.Count == 0 ? "nichts Offenes" : string.Join(Environment.NewLine, folgen.Select(f => f.Text));
    }

    public bool Speichern()
    {
        try
        {
            var datum = Datum.Lesen(EntlassDatum.Text, "Entlassdatum");
            speicher.Entlassen(bewohner.Id!.Value, datum, ArchivRegeln.Gruende[Math.Max(0, Grund.SelectedIndex)], Bemerkung.Text, Recht.Kuerzel, Jetzt());
        }
        catch (FormatException e) { return Melden(e.Message); }
        catch (Eingabefehler e) { return Melden(string.Join(" ", e.Fehler)); }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    private bool Melden(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = true;
        return false;
    }
}

// „Wieder aufnehmen …“: Datum und ein freies Bett; der alte Aufenthalt bleibt im Verlauf.
public sealed class WiederaufnahmeFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private List<Bettplatz> betten = [];

    public TextBox Aufnahme { get; } = new DatumFeld { Width = 100, AccessibleName = "Aufnahmedatum" };
    public ComboBox Bereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, AccessibleName = "Wohnbereich" };
    public ComboBox Bett { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, AccessibleName = "Bett" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Bewohner? Gespeichert { get; private set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public WiederaufnahmeFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Wieder aufnehmen: {bewohner.Name}");
        Aufnahme.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        var bereiche = speicher.Wohnbereiche().Select(w => w.Name).ToList();
        Bereich.Items.AddRange([.. bereiche]);
        Bereich.SelectedIndexChanged += (_, _) => BettenFuellen();
        Bereich.SelectedIndex = Math.Max(0, bereiche.IndexOf(bewohner.Wohnbereich));
        BettenFuellen();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Aufgenommen am", Aufnahme);
        zeile("Wohnbereich, Bett", Formular.Reihe(Bereich, Bett));
        zeile(null, Fenster.Hinweis("Ein neuer Aufenthalt beginnt; Berichte und Verlauf des alten bleiben sichtbar."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Wieder aufnehmen", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private void BettenFuellen()
    {
        betten = Bereich.SelectedItem is string b ? speicher.FreieBetten(b) : [];
        Bett.Items.Clear();
        Bett.Items.AddRange([.. betten.Select(x => x.Anzeige)]);
        int alt = betten.FindIndex(x => x.Zimmer == bewohner.Zimmer && x.Bett == bewohner.Bett);
        Bett.SelectedIndex = alt >= 0 ? alt : betten.Count > 0 ? 0 : -1;
    }

    public bool Speichern()
    {
        try
        {
            if (Bett.SelectedIndex < 0) throw new Eingabefehler(["In diesem Bereich ist kein Bett frei."]);
            var bett = betten[Bett.SelectedIndex];
            Gespeichert = speicher.Wiederaufnehmen(bewohner.Id!.Value, Datum.Lesen(Aufnahme.Text, "Aufgenommen am"), bett.Zimmer, bett.Bett, Recht.Kuerzel, Jetzt());
        }
        catch (FormatException e) { Fehler.Text = e.Message; Fehler.Visible = true; return false; }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Die Akte im Archiv, nur lesend: die Teile als Text, die Dokumente als Liste (Doppelklick öffnet), Drucken. Jedes Öffnen
// und Drucken steht im Zugriffsprotokoll.
public sealed class ArchivAkteFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<(string Titel, string Text)> teile;
    private readonly List<Dokument> dokumente;

    public Reiterleiste Reiter { get; }
    public TextBox Inhalt { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 760, Height = 420, BackColor = Stil.Flaeche };
    public ListBox Dokumente { get; } = new() { Width = 760, Height = 420, Visible = false, AccessibleName = "Dokumente" };
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    internal Action<Func<System.Drawing.Printing.PrintDocument>, string> Vorschau { get; set; }

    public ArchivAkteFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        long id = bewohner.Id!.Value;
        Fenster.Grundform(this, $"Archiv (nur lesen): {bewohner.Name}");
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(this, bauen, titel);
        teile = ArchivRegeln.AkteTeile(bewohner, speicher.ArchivEintraege(id), speicher.Berichte(id), speicher.Ziele(id), speicher.Massnahmen(id, auchBeendete: true),
            speicher.Verordnungen(id), speicher.Reflexionen(id), speicher.HebBoegen(id));
        dokumente = speicher.Dokumente(id);
        Dokumente.Items.AddRange([.. dokumente.Select(d => $"{Datum.Text(d.Datum)} · {d.Kategorie} · {d.Titel}")]);
        if (dokumente.Count == 0) Dokumente.Items.Add("Keine Dokumente.");
        Dokumente.DoubleClick += (_, _) => DokumentOeffnen();
        Reiter = new Reiterleiste([.. teile.Select(t => t.Titel), "Dokumente"]);
        Reiter.Gewechselt += _ => TeilZeigen();
        DruckKnopf.Click += (_, _) => Drucken();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        Reiter.Width = 760;
        Reiter.Height = Reiter.HoeheFuer(760);
        zeile(null, Reiter);
        zeile(null, Inhalt);
        zeile(null, Dokumente);
        zeile(null, Formular.Reihe(DruckKnopf));
        Controls.Add(raster);
        TeilZeigen();
        speicher.Zugriff(Recht.Kuerzel, id, ZugriffRegeln.Geoeffnet, "Archiv");
        Stil.DpiSkalierungEinschalten(this);
    }

    private void TeilZeigen()
    {
        bool doku = Reiter.Aktiv == "Dokumente";
        Dokumente.Visible = doku;
        Inhalt.Visible = !doku;
        if (!doku) Inhalt.Text = teile.First(t => t.Titel == Reiter.Aktiv).Text;
    }

    public void DokumentOeffnen()
    {
        if (Dokumente.SelectedIndex < 0 || Dokumente.SelectedIndex >= dokumente.Count) return;
        string pfad = Path.Combine(speicher.Ablage(bewohner.Id!.Value), dokumente[Dokumente.SelectedIndex].Datei);
        if (File.Exists(pfad)) Extern.Oeffnen(pfad);
    }

    public void Drucken()
    {
        var abschnitte = teile.Select(t => new UeberleitungAbschnitt(t.Titel, t.Text.Split(Environment.NewLine))).ToList();
        abschnitte.Add(new UeberleitungAbschnitt("Dokumente", [.. Dokumente.Items.Cast<string>()]));
        speicher.Zugriff(Recht.Kuerzel, bewohner.Id!.Value, ZugriffRegeln.Gedruckt, "Archiv");
        Vorschau(new UeberleitungDruck($"Archiv – {bewohner.Name} ({Stil.DemoHinweis})", abschnitte).DokumentBauen, $"Archiv {bewohner.Name}");
    }
}
