using DokuLine.Kern;

namespace DokuLine;

// Zimmer und Betten: Einstellungen → Wohnbereiche → „Zimmer und Betten …“.
// Oben die Wohnbereiche mit Art (offen/beschützend), darunter die Zimmer mit Belegung.
public sealed class ZimmerFenster : Form
{
    private readonly Speicher speicher;
    private List<Zimmer> zimmer = [];
    private List<Wohnbereich> bereiche = [];

    public ListView Bereiche { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 760, Height = 110, Font = Stil.Standard,
    };
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 760, Height = 300, Font = Stil.Standard,
    };
    public Button ArtKnopf { get; } = Stil.NebenKnopf("offen / beschützend");
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neues Zimmer …");
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern …");
    public Button UmschaltenKnopf { get; } = Stil.NebenKnopf("Stilllegen / wieder aktiv");
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public ZimmerFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Zimmer und Betten");
        foreach (var (titel, breite) in new[] { ("Wohnbereich", 160), ("Art", 140), ("Zimmer", 100), ("Betten frei", 120) })
            Bereiche.Columns.Add(titel, breite);
        foreach (var (titel, breite) in new[] { ("Zimmer", 80), ("Bereich", 90), ("Art", 120), ("Belegung", 400), ("Status", 70) })
            Liste.Columns.Add(titel, breite);
        Liste.DoubleClick += (_, _) => Aendern();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Einzelzimmer haben ein Bett, Doppelzimmer „Bett Fenster“ und „Bett Tür“. Die Aufnahme zeigt nur freie Betten. " +
                                    "Belegte Zimmer lassen sich nicht stilllegen oder verkleinern."));
        zeile(null, Bereiche);
        zeile(null, Liste);
        string? grund = Recht.Grund(speicher, Funktionen.EinstellungenBereich);
        foreach (var k in new[] { ArtKnopf, NeuKnopf, AendernKnopf, UmschaltenKnopf }) { k.AutoSize = true; Stil.Sperren(k, grund); }
        ArtKnopf.Click += (_, _) => ArtUmschalten();
        NeuKnopf.Click += (_, _) => Neu();
        AendernKnopf.Click += (_, _) => Aendern();
        UmschaltenKnopf.Click += (_, _) => Umschalten();
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        CancelButton = schliessen;
        zeile(null, Formular.Reihe(ArtKnopf, NeuKnopf, AendernKnopf, UmschaltenKnopf, schliessen));
        zeile(null, Fehler);
        Controls.Add(raster);
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        bereiche = speicher.Wohnbereiche();
        zimmer = speicher.Zimmerliste();
        var bewohner = speicher.AlleBewohner();
        Bereiche.Items.Clear();
        foreach (var w in bereiche)
            Bereiche.Items.Add(new ListViewItem([w.Name, w.Art, zimmer.Count(z => z.Bereich == w.Name && z.Aktiv).ToString(System.Globalization.CultureInfo.CurrentCulture),
                speicher.FreieBetten(w.Name).Count.ToString(System.Globalization.CultureInfo.CurrentCulture)]));
        Liste.Items.Clear();
        foreach (var z in zimmer)
        {
            var belegung = ZimmerRegeln.Bettschluessel(z.Betten).Select(b =>
                (z.Betten == 2 ? ZimmerRegeln.BettText(b) + ": " : "") +
                (bewohner.FirstOrDefault(x => x.Zimmer == z.Nummer && x.Bett == b)?.Name ?? "frei"));
            Liste.Items.Add(new ListViewItem([z.Nummer, z.Bereich, z.Art, string.Join(" · ", belegung), z.Aktiv ? "aktiv" : "stillgelegt"]));
        }
    }

    public void Waehlen(int index) { Liste.SelectedIndices.Clear(); Liste.SelectedIndices.Add(index); }
    public void BereichWaehlen(int index) { Bereiche.SelectedIndices.Clear(); Bereiche.SelectedIndices.Add(index); }
    public int Index(string nummer) => zimmer.FindIndex(z => z.Nummer == nummer);

    private Zimmer? Gewaehlt => Liste.SelectedIndices.Count == 1 ? zimmer[Liste.SelectedIndices[0]] : null;

    private bool Darf => Recht.Grund(speicher, Funktionen.EinstellungenBereich) is null;

    private bool Ausfuehren(Action aktion)
    {
        if (!Darf) return false;
        try { aktion(); }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        Fehler.Visible = false;
        Fuellen();
        return true;
    }

    public bool ArtUmschalten()
    {
        if (Bereiche.SelectedIndices.Count != 1) return false;
        var w = bereiche[Bereiche.SelectedIndices[0]];
        return Ausfuehren(() => speicher.BereichArtSetzen(w.Name, !w.Beschuetzt));
    }

    public bool Umschalten() => Gewaehlt is Zimmer z && Ausfuehren(() => speicher.ZimmerSpeichern(z with { Aktiv = !z.Aktiv }, neu: false));

    public void Neu()
    {
        if (!Darf) return;
        using var f = new ZimmerFormular(speicher, null);
        if (Dialog.Zeigen(f, this) == DialogResult.OK) Fuellen();
    }

    public void Aendern()
    {
        if (!Darf || Gewaehlt is not Zimmer z) return;
        using var f = new ZimmerFormular(speicher, z);
        if (Dialog.Zeigen(f, this) == DialogResult.OK) Fuellen();
    }
}

// Ein Zimmer: Nummer (nur beim Anlegen), Wohnbereich, Einzel- oder Doppelzimmer.
public sealed class ZimmerFormular : Form
{
    private readonly Speicher speicher;
    private readonly Zimmer? bisher;
    private readonly List<Wohnbereich> bereiche;

    public TextBox Nummer { get; } = new() { Width = 100, MaxLength = 10 };
    public ComboBox Bereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = Stil.Standard };
    public RadioButton Einzel { get; } = new() { Text = "Einzelzimmer (ein Bett)", AutoSize = true, Checked = true };
    public RadioButton Doppel { get; } = new() { Text = "Doppelzimmer (Bett Fenster, Bett Tür)", AutoSize = true };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public ZimmerFormular(Speicher speicher, Zimmer? bisher)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        bereiche = speicher.Wohnbereiche();
        Fenster.Grundform(this, bisher is null ? "Neues Zimmer" : $"Zimmer {bisher.Nummer}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Zimmernummer", Nummer);
        Bereich.Items.AddRange([.. bereiche.Select(w => (object)w.Anzeige)]);
        zeile("Wohnbereich", Bereich);
        var art = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        art.Controls.AddRange([Einzel, Doppel]);
        zeile("Art", art);
        if (bisher is not null)
        {
            Nummer.Text = bisher.Nummer;
            Nummer.ReadOnly = true;
            Bereich.SelectedIndex = bereiche.FindIndex(w => w.Name == bisher.Bereich);
            Doppel.Checked = bisher.Betten == 2;
        }
        else if (bereiche.Count > 0) Bereich.SelectedIndex = 0;
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            string bereich = Bereich.SelectedIndex >= 0 ? bereiche[Bereich.SelectedIndex].Name : "";
            speicher.ZimmerSpeichern(new Zimmer(Nummer.Text, bereich, Doppel.Checked ? 2 : 1, bisher?.Aktiv ?? true), neu: bisher is null);
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
