using DokuLine.Kern;

namespace DokuLine;

// „Digitale Akte übernehmen …“ (Bauplan 32): Hauptordner wählen (oder in der Demo einen ausgedachten anlegen), Vorschau
// mit vorgeschlagenem Bewohner je Ordner (änderbar), Zahl der Dateien und Kategorien; „Übernehmen“ kopiert, danach ein
// Bericht. Der alte Ordner bleibt unverändert.
public sealed class AkteUebernahmeFenster : Form
{
    public const string NichtUebernehmen = "(nicht übernehmen)";
    private readonly Speicher speicher;
    private readonly List<Bewohner> bewohner;
    private List<UebernahmeOrdner> plan = [];

    public TextBox Ordner { get; } = new() { Width = 420, ReadOnly = true };
    public Button WaehlenKnopf { get; } = Stil.NebenKnopf("Ordner wählen …");
    public Button DemoKnopf { get; } = Stil.NebenKnopf("Demo-Ordner anlegen");
    public DataGridView Vorschau { get; } = new()
    {
        Width = 680, Height = 260, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Stil.Flaeche,
    };
    public Label Zusammenfassung { get; } = new() { AutoSize = true, MaximumSize = new Size(680, 0), ForeColor = Stil.TextMittel };
    public Button UebernehmenKnopf { get; private set; } = null!;
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public UebernahmeErgebnis? Ergebnis { get; private set; }
    internal Func<string?> OrdnerWaehlen { get; set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AkteUebernahmeFenster(Speicher speicher)
    {
        this.speicher = speicher;
        bewohner = speicher.AlleBewohner();
        Fenster.Grundform(this, "Digitale Akte übernehmen");
        OrdnerWaehlen = () =>
        {
            using var dialog = new FolderBrowserDialog { Description = "Hauptordner der digitalen Akte (ein Unterordner je Bewohner)" };
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedPath : null;
        };
        Vorschau.Columns.Add(new DataGridViewTextBoxColumn { Name = "ordner", HeaderText = "Ordner", ReadOnly = true, FillWeight = 30 });
        var spalte = new DataGridViewComboBoxColumn { Name = "bewohner", HeaderText = "Bewohner", FillWeight = 30, FlatStyle = FlatStyle.Flat };
        spalte.Items.Add(NichtUebernehmen);
        spalte.Items.AddRange([.. bewohner.Select(Anzeige)]);
        Vorschau.Columns.Add(spalte);
        Vorschau.Columns.Add(new DataGridViewTextBoxColumn { Name = "dateien", HeaderText = "Dateien", ReadOnly = true, FillWeight = 10 });
        Vorschau.Columns.Add(new DataGridViewTextBoxColumn { Name = "kategorien", HeaderText = "Kategorien", ReadOnly = true, FillWeight = 40 });
        Vorschau.CellValueChanged += (_, _) => Zusammenfassen();
        Vorschau.CurrentCellDirtyStateChanged += (_, _) => { if (Vorschau.IsCurrentCellDirty) Vorschau.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        WaehlenKnopf.Click += (_, _) => { if (OrdnerWaehlen() is string o) Laden(o); };
        DemoKnopf.Click += (_, _) => Laden(AkteUebernahme.DemoOrdnerAnlegen(Pfade.ImDatenordner(), bewohner));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Ein Unterordner je Bewohner, darin Unterordner je Kategorie. Das Programm schlägt die Zuordnung vor; " +
                                    "bitte prüfen. Übernehmen kopiert die Dateien – der alte Ordner bleibt, wie er ist. In der Demo nur ausgedachte Ordner."));
        DemoKnopf.Visible = !Betrieb.Aktiv;   // ausgedachte Ordner gehören nie in echte Akten
        zeile("Hauptordner", Betrieb.Aktiv ? Formular.Reihe(Ordner, WaehlenKnopf) : Formular.Reihe(Ordner, WaehlenKnopf, DemoKnopf));
        zeile(null, Vorschau);
        zeile(null, Zusammenfassung);
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Übernehmen", () => Uebernehmen());
        UebernehmenKnopf = knoepfe.Controls.OfType<Button>().First(k => k.Text == "Übernehmen");
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
        Zusammenfassen();
    }

    private static string Anzeige(Bewohner b) => $"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})";

    public void Laden(string hauptordner)
    {
        try { plan = AkteUebernahme.Planen(hauptordner, bewohner, speicher.Dokumentkategorien()); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Fehler.Text = $"Der Ordner ließ sich nicht lesen: {e.Message}";
            Fehler.Visible = true;
            return;
        }
        Ordner.Text = hauptordner;
        Vorschau.Rows.Clear();
        foreach (var o in plan)
        {
            string wer = o.BewohnerId is long id && bewohner.FirstOrDefault(b => b.Id == id) is Bewohner b ? Anzeige(b) : NichtUebernehmen;
            string kategorien = string.Join(", ", o.Dateien.GroupBy(d => d.Kategorie).Select(g => $"{g.Key} ({g.Count()})"));
            Vorschau.Rows.Add(o.Name, wer, o.Dateien.Count, kategorien);
        }
        Fehler.Visible = false;
        Zusammenfassen();
    }

    // Der Plan mit der Zuordnung, wie sie in der Vorschau steht.
    public List<UebernahmeOrdner> Plan() =>
        [.. plan.Select((o, i) => o with
        {
            BewohnerId = Vorschau.Rows[i].Cells["bewohner"].Value is string s && bewohner.FirstOrDefault(b => Anzeige(b) == s) is Bewohner b ? b.Id : null,
        })];

    private void Zusammenfassen()
    {
        var p = Plan();
        int dateien = p.Where(o => o.BewohnerId is not null).Sum(o => o.Dateien.Count);
        Zusammenfassung.Text = plan.Count == 0 ? "Noch kein Ordner gewählt."
            : $"{p.Count(o => o.BewohnerId is not null)} von {p.Count} Ordnern zugeordnet · {dateien} Dateien werden kopiert · " +
              $"{p.Where(o => o.BewohnerId is null).Sum(o => o.Dateien.Count)} Dateien bleiben draußen.";
        Stil.Sperren(UebernehmenKnopf, Recht.Grund(speicher, Funktionen.DokumenteHinzufuegen) ?? (dateien == 0 ? "Nichts zugeordnet." : null));
    }

    public bool Uebernehmen()
    {
        if (Stil.Sperrgrund(UebernehmenKnopf) is not null) return false;
        try { Ergebnis = speicher.AkteUebernehmen(Plan(), Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        using (var bericht = new TextFenster("Digitale Akte übernommen", AkteUebernahme.Bericht(Ergebnis)))
            if (Visible) Dialog.Zeigen(bericht, this);
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
