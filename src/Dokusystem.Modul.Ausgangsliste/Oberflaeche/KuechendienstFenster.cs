using System.Globalization;
using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Modul.Ausgangsliste;

// Übersicht des Küchendienst-Plans (übernommen aus der Ausgangsliste): laufender und nächster
// Monat, vergangene Tage leise, heute hervorgehoben. Beim Öffnen wird neu geplant; heute und
// die Vergangenheit bleiben dabei fest. Unten: Tausch, Reihenfolge und Zimmergruppen, Drucken.
public sealed class KuechendienstFenster : Form
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private readonly Datenbank db;
    private List<KuechendienstTag> tage = [];

    public DataGridView Tabelle { get; } = new();
    public Button TauschKnopf { get; } = Stil.NebenKnopf("Tausch …");
    public Button GruppenKnopf { get; } = Stil.NebenKnopf("Reihenfolge und Zimmergruppen …");
    public Button DruckenKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public Button SchliessenKnopf { get; } = Stil.NebenKnopf("Schließen");
    public ContextMenuStrip DruckMenue { get; } = new() { Font = Stil.Standard, ShowImageMargin = false };

    // Hinweis im Fenster, wenn etwas nicht geht (statt eines Meldungsfensters).
    public Label Meldung { get; } = new() { AutoSize = true, MaximumSize = new Size(680, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    // Die angezeigten Tage, in derselben Reihenfolge wie die Zeilen der Tabelle.
    public IReadOnlyList<KuechendienstTag> Tage => tage;

    public KuechendienstFenster(Datenbank db)
    {
        this.db = db;
        Text = $"Küchendienst ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 640);
        MinimumSize = new Size(560, 420);
        BackColor = Stil.Fensterhintergrund;
        ForeColor = Stil.TextNormal;
        Font = Stil.Standard;

        var hinweis = new Label
        {
            Text = "Vergangene Tage und heute stehen fest und ändern sich nicht mehr. " +
                   "Für einen Tausch einen Tag ab morgen auswählen (oder doppelt anklicken).",
            AutoSize = true, MaximumSize = new Size(680, 0), ForeColor = Stil.TextLeise, Margin = new Padding(0, 0, 0, Stil.AbstandMittel),
        };

        TabelleGestalten();
        Meldung.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);

        SchliessenKnopf.DialogResult = DialogResult.Cancel;
        TauschKnopf.Click += (_, _) => TauschBearbeiten();
        GruppenKnopf.Click += (_, _) => GruppenBearbeiten();
        DruckenKnopf.Click += (_, _) =>
        {
            DruckMenueFuellen();
            DruckMenue.Show(DruckenKnopf, new Point(0, DruckenKnopf.Height));
        };
        var knoepfe = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, Stil.AbstandMittel, 0, 0) };
        knoepfe.Controls.AddRange([SchliessenKnopf, DruckenKnopf, GruppenKnopf, TauschKnopf]);
        foreach (Control knopf in knoepfe.Controls) knopf.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);

        var raster = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(Stil.AbstandGross), ColumnCount = 1, RowCount = 4 };
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        raster.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Hinweis
        raster.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Plan
        raster.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Meldung
        raster.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Knöpfe
        raster.Controls.Add(hinweis, 0, 0);
        raster.Controls.Add(Tabelle, 0, 1);
        raster.Controls.Add(Meldung, 0, 2);
        raster.Controls.Add(knoepfe, 0, 3);
        Controls.Add(raster);
        CancelButton = SchliessenKnopf;
        Disposed += (_, _) => DruckMenue.Dispose(); // Menü mit dem Fenster aufräumen

        Planen();
        Aktualisieren();
        DruckMenueFuellen();

        // Beim Öffnen gleich zu heute springen.
        Shown += (_, _) =>
        {
            int heuteZeile = tage.FindIndex(t => t.Datum == Heute);
            if (heuteZeile < 0) return;
            Tabelle.FirstDisplayedScrollingRowIndex = Math.Max(0, heuteZeile - 3);
            Tabelle.ClearSelection();
        };

        Stil.DpiSkalierungEinschalten(this);
    }

    private DateTime Heute => db.Jetzt().Date;

    private void TabelleGestalten()
    {
        var t = Tabelle;
        t.Dock = DockStyle.Fill;
        t.Margin = new Padding(0);
        t.AccessibleName = "Küchendienstplan";
        t.ReadOnly = true;
        t.AllowUserToAddRows = false;
        t.AllowUserToDeleteRows = false;
        t.AllowUserToResizeRows = false;
        t.RowHeadersVisible = false;
        t.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        t.MultiSelect = false;
        t.BorderStyle = BorderStyle.FixedSingle;
        t.BackgroundColor = Stil.Flaeche;
        t.GridColor = Stil.Trennlinie;
        t.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        t.DefaultCellStyle.BackColor = Stil.Flaeche;
        t.DefaultCellStyle.ForeColor = Stil.TextNormal;
        t.DefaultCellStyle.SelectionBackColor = Stil.AkzentHell;
        t.DefaultCellStyle.SelectionForeColor = Stil.TextNormal;
        t.DefaultCellStyle.Padding = new Padding(Stil.AbstandKlein, 0, Stil.AbstandKlein, 0);
        t.RowTemplate.Height = 30;
        t.EnableHeadersVisualStyles = false;
        t.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        t.ColumnHeadersDefaultCellStyle.BackColor = Stil.Flaeche;
        t.ColumnHeadersDefaultCellStyle.ForeColor = Stil.TextMittel;
        t.ColumnHeadersDefaultCellStyle.Font = Stil.Fett;
        t.ColumnHeadersHeight = 36;
        t.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        t.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Ungebunden: Aktualisieren füllt die Zeilen von Hand.
        t.Columns.Add("datum", "Datum");
        t.Columns.Add("zimmer", "Zimmer");
        t.Columns.Add("tausch", "Tausch");
        t.Columns.Add("dienst", "Dienst (anwesend)");
        t.Columns["datum"]!.FillWeight = 30;
        t.Columns["zimmer"]!.FillWeight = 12;
        t.Columns["tausch"]!.FillWeight = 12;
        t.Columns["dienst"]!.FillWeight = 46;
        foreach (DataGridViewColumn s in t.Columns) s.SortMode = DataGridViewColumnSortMode.NotSortable;
        t.Columns["tausch"]!.DefaultCellStyle.Font = Stil.Fett; // ein Tausch fällt auf
        t.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) TauschBearbeiten(); };
    }

    // Plant ab heute neu (steht heute schon fest: ab morgen). Tausche in der Zukunft bleiben.
    private void Planen()
    {
        try
        {
            Kuechendienst.Planen(db, db.Laden(), db.Jetzt());
        }
        catch (Exception e) when (e is SqliteException or SpeicherFehler)
        {
            MeldungZeigen("Der Küchendienst konnte nicht neu geplant werden: " + e.Message);
        }
    }

    // Liest den Plan ab Monatsanfang und baut die Zeilen neu auf – beim Öffnen und nach jedem
    // Tausch oder jeder Änderung an Reihenfolge und Gruppen.
    public void Aktualisieren()
    {
        DateTime heute = Heute;
        var monatsanfang = new DateTime(heute.Year, heute.Month, 1);
        var plaetze = db.Laden();
        DateTime? gewaehlt = GewaehlterTag?.Datum;
        int erste = Tabelle.IsHandleCreated && Tabelle.Rows.Count > 0 ? Tabelle.FirstDisplayedScrollingRowIndex : -1;

        tage = db.KuechendienstLaden().Where(t => t.Datum >= monatsanfang).ToList();
        Tabelle.Rows.Clear();
        foreach (var tag in tage)
        {
            // Namen nur für heute und die Zukunft: Für vergangene Tage wären es die heutigen
            // Bewohner der Zimmer, und das wäre irreführend.
            string dienst = tag.Datum >= heute ? Kuechendienst.NamenFuer(tag.Wirksam, plaetze) : "";
            string datum = tag.Datum.ToString("ddd, dd.MM.yyyy", De) + (tag.Datum == heute ? " (heute)" : "");
            int zeile = Tabelle.Rows.Add(datum, tag.Zimmer, tag.Tausch, dienst);

            var stil = Tabelle.Rows[zeile].DefaultCellStyle;
            if (tag.Datum < heute)
            {
                stil.ForeColor = Stil.TextLeise;
            }
            else if (tag.Datum == heute)
            {
                stil.BackColor = Stil.AkzentHell;
                stil.Font = Stil.Fett;
            }
        }

        // Nach dem Neuaufbau an derselben Stelle bleiben und den gewählten Tag wieder wählen.
        if (!Tabelle.IsHandleCreated) return;
        if (erste >= 0 && erste < Tabelle.Rows.Count) Tabelle.FirstDisplayedScrollingRowIndex = erste;
        int wieder = gewaehlt is DateTime d ? tage.FindIndex(t => t.Datum == d) : -1;
        if (wieder >= 0) Tabelle.CurrentCell = Tabelle.Rows[wieder].Cells[0];
        else Tabelle.ClearSelection();
    }

    // Der in der Tabelle gewählte Tag (oder keiner).
    public KuechendienstTag? GewaehlterTag =>
        Tabelle.SelectedRows.Count > 0 && Tabelle.SelectedRows[0].Index < tage.Count ? tage[Tabelle.SelectedRows[0].Index] : null;

    // ----- Tausch -----

    public void TauschBearbeiten()
    {
        if (GewaehlterTag is not KuechendienstTag tag)
        {
            MeldungZeigen("Bitte zuerst in der Liste den Tag auswählen, an dem getauscht werden soll.");
            return;
        }
        TauschBearbeiten(tag.Datum);
    }

    public void TauschBearbeiten(DateTime datum)
    {
        MeldungZeigen("");
        DateTime heute = Heute;
        var tag = tage.FirstOrDefault(t => t.Datum == datum.Date);
        if (tag is null)
        {
            MeldungZeigen($"Für den {datum.ToString("dd.MM.yyyy", De)} gibt es keinen Plan.");
            return;
        }
        if (!Kuechendienst.TauschMoeglich(tag.Datum, heute))
        {
            MeldungZeigen("Heute und vergangene Tage stehen fest und lassen sich nicht mehr ändern. Ein Tausch ist ab morgen möglich.");
            return;
        }

        using var fenster = new TauschFenster(db, tag, Kuechendienst.TauschKandidaten(tage, tag, heute));
        if (Dialog.Zeigen(fenster, this) == DialogResult.OK) Aktualisieren();
    }

    // ----- Reihenfolge und Zimmergruppen -----

    public void GruppenBearbeiten()
    {
        MeldungZeigen("");
        using var fenster = new ZimmergruppenFenster(db);
        if (Dialog.Zeigen(fenster, this) != DialogResult.OK) return;
        // Neue Reihenfolge oder Gruppen wirken erst ab dem nächsten noch nicht festen Tag.
        Planen();
        Aktualisieren();
    }

    // ----- Drucken -----

    // Ein kleines Menü am Knopf: laufender oder nächster Monat. Gedruckt wird über die Vorschau.
    private void DruckMenueFuellen()
    {
        DruckMenue.Items.Clear();
        var diesen = new DateTime(Heute.Year, Heute.Month, 1);
        foreach (var monat in new[] { diesen, diesen.AddMonths(1) })
            DruckMenue.Items.Add($"{monat.ToString("MMMM yyyy", De)} drucken …", null, (_, _) => MonatDrucken(monat));
    }

    public void MonatDrucken(DateTime monat)
    {
        MeldungZeigen("");
        var druck = new KuechendienstDruck(monat, db.KuechendienstLaden(), db.Jetzt());
        if (!druck.HatTage)
        {
            MeldungZeigen($"Für {monat.ToString("MMMM yyyy", De)} gibt es noch keinen Plan.");
            return;
        }
        Druckvorschau.Zeigen(this, druck.DokumentBauen, druck.Titel);
    }

    private void MeldungZeigen(string text)
    {
        Meldung.Text = text;
        Meldung.Visible = text != "";
    }
}
