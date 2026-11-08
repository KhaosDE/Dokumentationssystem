using DokuLine.Kern;

namespace DokuLine;

// Verwaltung (Bauplan Schritt 19): Mitarbeiter, Bereiche und Positionen, Rechte-Tabelle. Wer was
// darf, steht in der Rechte-Tabelle selbst. Ohne Recht bleibt alles sichtbar, aber gesperrt.

static class Fenster
{
    public static void Grundform(Form f, string titel)
    {
        f.Text = $"{titel} ({Stil.DemoHinweis})";
        f.Icon = Stil.Programmsymbol;
        f.FormBorderStyle = FormBorderStyle.FixedDialog;
        f.MaximizeBox = f.MinimizeBox = false;
        f.ShowInTaskbar = false;
        f.StartPosition = FormStartPosition.CenterParent;
        f.BackColor = Stil.Fensterhintergrund;
        f.Font = Stil.Standard;
        f.AutoSize = true;
        f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    public static Label Fehlerfeld() => new()
    {
        AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche,
        Padding = new Padding(Stil.AbstandKlein), Visible = false,
    };

    public static Label Hinweis(string text) => new() { Text = text, AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise };
}

// Liste aller Mitarbeiter mit „Neuer Mitarbeiter …“ und „Bearbeiten …“.
public sealed class MitarbeiterFenster : Form
{
    private readonly Speicher speicher;
    private List<Mitarbeiter> mitarbeiter = [];

    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 760, Height = 360, Font = Stil.Standard,
    };
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neuer Mitarbeiter …");
    public Button BearbeitenKnopf { get; } = Stil.NebenKnopf("Bearbeiten …");

    public MitarbeiterFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Mitarbeiter");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Ausgedachte Mitarbeiter der Demo. Das Kürzel ist zugleich die Anmeldung. Gelöscht wird niemand: " +
                                    "Wer ausscheidet, wird als „ausgeschieden“ markiert und bleibt an seinen Einträgen sichtbar."));
        foreach (var (titel, breite) in new[] { ("Kürzel", 70), ("Name", 170), ("Qualifikation", 110), ("Bereiche", 180), ("Positionen", 180), ("Status", 80) })
            Liste.Columns.Add(titel, breite);
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Liste.DoubleClick += (_, _) => Bearbeiten();
        zeile(null, Liste);
        NeuKnopf.Click += (_, _) => Neu();
        BearbeitenKnopf.Click += (_, _) => Bearbeiten();
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        zeile(null, Formular.Reihe(NeuKnopf, BearbeitenKnopf, schliessen));
        Controls.Add(raster);
        CancelButton = schliessen;
        Laden();
        Stil.DpiSkalierungEinschalten(this);
    }

    private Mitarbeiter? Ich => speicher.MitarbeiterLesen(Recht.Kuerzel);
    private bool Alle => Recht.Darf(speicher, Funktionen.MitarbeiterAlle);
    private bool EigenerBereich => Recht.Darf(speicher, Funktionen.MitarbeiterBereich);

    public void Laden()
    {
        mitarbeiter = speicher.AlleMitarbeiter();
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var m in mitarbeiter)
            Liste.Items.Add(new ListViewItem([m.Kuerzel, m.Name, m.Qualifikation, string.Join(", ", m.Bereiche),
                string.Join(", ", m.Positionen), m.Aktiv ? "aktiv" : "ausgeschieden"]) { ForeColor = m.Aktiv ? Stil.TextNormal : Stil.TextLeise });
        Liste.EndUpdate();
        KnoepfeSetzen();
    }

    public Mitarbeiter? Gewaehlt => Liste.SelectedIndices.Count == 1 ? mitarbeiter[Liste.SelectedIndices[0]] : null;

    public void Waehlen(string kuerzel)
    {
        int i = mitarbeiter.FindIndex(m => m.Kuerzel == kuerzel);
        Liste.SelectedIndices.Clear();
        if (i >= 0) Liste.SelectedIndices.Add(i);
        KnoepfeSetzen();
    }

    private void KnoepfeSetzen()
    {
        Stil.Sperren(NeuKnopf, Alle || EigenerBereich ? null : Recht.Grund(speicher, Funktionen.MitarbeiterAlle));
        string? grund = Gewaehlt is not Mitarbeiter m ? "Bitte zuerst einen Mitarbeiter wählen."
            : Ich is Mitarbeiter ich && RechteRegeln.DarfVerwalten(ich, Alle, EigenerBereich, m.Bereiche) ? null
            : Alle || EigenerBereich ? "Nur Mitarbeiter des eigenen Bereichs." : Recht.Grund(speicher, Funktionen.MitarbeiterAlle);
        Stil.Sperren(BearbeitenKnopf, grund);
    }

    public MitarbeiterFormular FormularErstellen(Mitarbeiter? m) => new(speicher, m, Alle ? null : Ich?.Bereiche);

    private void Neu()
    {
        using var f = FormularErstellen(null);
        if (Dialog.Zeigen(f, this) == DialogResult.OK) { Laden(); if (f.Gespeichert is Mitarbeiter m) Waehlen(m.Kuerzel); }
    }

    private void Bearbeiten()
    {
        if (Gewaehlt is not Mitarbeiter m || Stil.Sperrgrund(BearbeitenKnopf) is not null) return;
        using var f = FormularErstellen(m);
        if (Dialog.Zeigen(f, this) == DialogResult.OK) { Laden(); Waehlen(m.Kuerzel); }
    }
}

// Ein Mitarbeiter: Kürzel (nur beim Anlegen), Name, Qualifikation (Assistenzkraft oder Fachkraft, 0.33.0), aktiv,
// Bereiche und Positionen per Häkchen.
// eigeneBereiche: Wer nur den eigenen Bereich verwalten darf, kann nur diese Bereiche ändern.
public sealed class MitarbeiterFormular : Form
{
    private readonly Speicher speicher;
    private readonly Mitarbeiter? bisher;

    public TextBox Kuerzel { get; } = new() { Width = 100, MaxLength = 6, CharacterCasing = CharacterCasing.Upper };
    public TextBox Vorname { get; } = new() { Width = 260 };
    public TextBox Nachname { get; } = new() { Width = 260 };
    // Qualifikation: die kleinste Stufe ist die Assistenzkraft (Vorgabe für neue), darüber die Fachkraft.
    public RadioButton Assistenzkraft { get; } = new() { Text = RechteRegeln.Assistenzkraft, AutoSize = true, Checked = true };
    public RadioButton Fachkraft { get; } = new() { Text = RechteRegeln.Fachkraft, AutoSize = true };
    public CheckBox Aktiv { get; } = new() { Text = "aktiv (nicht ausgeschieden)", AutoSize = true, Checked = true };
    public Dictionary<string, CheckBox> Bereiche { get; } = [];
    public Dictionary<string, CheckBox> Positionen { get; } = [];
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Label LetzterAdmin { get; } = Fenster.Hinweis(RechteRegeln.LetzterAdminText);
    public Mitarbeiter? Gespeichert { get; private set; }

    public MitarbeiterFormular(Speicher speicher, Mitarbeiter? bisher, IReadOnlyList<string>? eigeneBereiche)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        Fenster.Grundform(this, bisher is null ? "Neuer Mitarbeiter" : $"Mitarbeiter – {bisher.Name}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Kürzel", Kuerzel);
        if (bisher is not null) { Kuerzel.Text = bisher.Kuerzel; Kuerzel.ReadOnly = true; }
        zeile(null, Fenster.Hinweis(bisher is null ? "Das Kürzel ist die Anmeldung und lässt sich später nicht mehr ändern." : "Das Kürzel bleibt, wie es ist."));
        zeile("Vorname", Vorname);
        zeile("Nachname", Nachname);
        zeile("Qualifikation", Formular.Reihe(Assistenzkraft, Fachkraft));
        zeile(null, Fenster.Hinweis("Assistenzkräfte machen keine Visiten und geben Bedarfsmedikation nur nach Absprache mit einer Fachkraft."));
        zeile(null, Aktiv);
        FlowLayoutPanel Haken(IEnumerable<Listeneintrag> liste, Dictionary<string, CheckBox> ziel, IReadOnlyList<string> gewaehlt, Func<string, bool> frei)
        {
            var feld = new FlowLayoutPanel { AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0) };
            foreach (var e in liste.Where(e => e.Aktiv || gewaehlt.Contains(e.Name)))
            {
                var c = new CheckBox { Text = e.Name + (e.Aktiv ? "" : " (stillgelegt)"), AutoSize = true, Checked = gewaehlt.Contains(e.Name), Enabled = frei(e.Name) };
                ziel[e.Name] = c;
                feld.Controls.Add(c);
            }
            return feld;
        }
        zeile("Bereiche", Haken(speicher.Bereichsliste(), Bereiche, bisher?.Bereiche ?? [], b => eigeneBereiche is null || eigeneBereiche.Contains(b)));
        // Schritt 26a2: „Administrator“ vergibt nur ein Administrator; der letzte aktive bleibt es (Hinweis statt Fehler).
        bool ichAdmin = RechteRegeln.IstAdministrator(speicher.MitarbeiterLesen(Recht.Kuerzel));
        bool letzterAdmin = RechteRegeln.IstAdministrator(bisher) && speicher.AlleMitarbeiter().Count(RechteRegeln.IstAdministrator) == 1;
        zeile("Positionen", Haken(speicher.Positionsliste(), Positionen, bisher?.Positionen ?? [], p => p != RechteRegeln.Administrator || (ichAdmin && !letzterAdmin)));
        zeile(null, Fenster.Hinweis("Mehrere Positionen: Es gelten die Rechte aller zusammen. Der Administrator hat immer alle Rechte; " +
                                    "die Position vergibt nur ein Administrator."));
        LetzterAdmin.Visible = letzterAdmin;
        zeile(null, LetzterAdmin);
        if (bisher is not null)
        {
            Vorname.Text = bisher.Vorname;
            Nachname.Text = bisher.Nachname;
            Fachkraft.Checked = bisher.Fachkraft;
            Assistenzkraft.Checked = !bisher.Fachkraft;
            Aktiv.Checked = bisher.Aktiv;
        }
        Aktiv.Enabled = !letzterAdmin;
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            var m = new Mitarbeiter(Kuerzel.Text, Vorname.Text, Nachname.Text, Fachkraft.Checked, Aktiv.Checked,
                Bereiche.Where(x => x.Value.Checked).Select(x => x.Key).ToList(), Positionen.Where(x => x.Value.Checked).Select(x => x.Key).ToList());
            Gespeichert = speicher.MitarbeiterSpeichern(m, neu: bisher is null);
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

// Bereiche und Positionen: anlegen, umbenennen, stilllegen, löschen (nur ohne Zuordnung).
public sealed class ListenFenster : Form
{
    private readonly Speicher speicher;

    public Dictionary<Liste, ListBox> Listen { get; } = [];
    public TextBox Name_ { get; } = new() { Width = 220, PlaceholderText = "Name" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public List<Button> Knoepfe { get; } = [];

    public ListenFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Bereiche und Positionen");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Ausgedachte Namen. Umbenennen nimmt alle Zuordnungen mit. Löschen geht nur, solange niemand zugeordnet ist – sonst stilllegen."));
        zeile("Name", Name_);
        string? grund = Recht.Grund(speicher, Funktionen.MitarbeiterAlle);
        foreach (var art in Enum.GetValues<Liste>())
        {
            var liste = new ListBox { Width = 300, Height = 170, IntegralHeight = false, Font = Stil.Standard };
            liste.SelectedIndexChanged += (_, _) => { if (Gewaehlt(art) is string n) Name_.Text = n; };
            Listen[art] = liste;
            Button Knopf(string text, Action<string> aktion)
            {
                var k = Stil.NebenKnopf(text);
                k.Click += (_, _) => Ausfuehren(() => aktion(Gewaehlt(art) ?? throw new Eingabefehler(["Bitte zuerst einen Eintrag wählen."])));
                Stil.Sperren(k, grund);
                Knoepfe.Add(k);
                return k;
            }
            var anlegen = Stil.NebenKnopf("Anlegen");
            anlegen.Click += (_, _) => Ausfuehren(() => speicher.ListeneintragAnlegen(art, Name_.Text));
            Stil.Sperren(anlegen, grund);
            Knoepfe.Add(anlegen);
            var spalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 0, Stil.AbstandKlein) };
            spalte.Controls.AddRange([liste, Formular.Reihe(anlegen,
                Knopf("Umbenennen", n => speicher.ListeneintragUmbenennen(art, n, Name_.Text)),
                Knopf("Stilllegen / wieder aktiv", n => speicher.ListeneintragAktivSetzen(art, n, !Eintrag(art, n).Aktiv)),
                Knopf("Löschen", n => speicher.ListeneintragLoeschen(art, n)))]);
            zeile(art == Liste.Bereiche ? "Bereiche" : "Positionen", spalte);
        }
        zeile(null, Fehler);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        zeile(null, schliessen);
        Controls.Add(raster);
        CancelButton = schliessen;
        Laden();
        Stil.DpiSkalierungEinschalten(this);
    }

    private Listeneintrag Eintrag(Liste art, string name) =>
        (art == Liste.Bereiche ? speicher.Bereichsliste() : speicher.Positionsliste()).Single(e => e.Name == name);

    private string? Gewaehlt(Liste art) => Listen[art].SelectedIndex >= 0
        ? (art == Liste.Bereiche ? speicher.Bereichsliste() : speicher.Positionsliste())[Listen[art].SelectedIndex].Name : null;

    public void Laden()
    {
        foreach (var (art, liste) in Listen)
        {
            liste.Items.Clear();
            foreach (var e in art == Liste.Bereiche ? speicher.Bereichsliste() : speicher.Positionsliste())
                liste.Items.Add($"{e.Name}{(e.Aktiv ? "" : " (stillgelegt)")} · {e.Zugeordnet} zugeordnet");
        }
    }

    public bool Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        Fehler.Visible = false;
        Laden();
        return true;
    }
}

// Rechte als Tabelle: Zeilen = Funktionen, Spalten = Positionen, dazu „nur Fachkräfte“. Änderungen
// gelten sofort. Bearbeiten nur mit „Rechte verwalten“.
public sealed class RechteFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<string> positionen;
    private bool laden;

    public DataGridView Tabelle { get; } = new()
    {
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.CellSelect, BackgroundColor = Stil.Flaeche, Width = 1000, Height = 520, Font = Stil.Klein,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
    };
    public Label Gesperrt { get; } = new() { AutoSize = true, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    public RechteFenster(Speicher speicher)
    {
        this.speicher = speicher;
        positionen = speicher.Positionsliste().Where(p => p.Aktiv).Select(p => p.Name).ToList();
        Fenster.Grundform(this, "Rechte");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Häkchen: Diese Position darf die Funktion. „Nur Fachkräfte“: zusätzlich muss die Person Fachkraft sein (nicht Assistenzkraft). " +
                                    "Wer mehrere Positionen hat, darf alles, was eine davon darf. Änderungen gelten sofort. " +
                                    "Der Administrator hat immer alle Rechte (auch ohne „Fachkraft“); seine Häkchen lassen sich nicht abwählen."));
        Tabelle.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Funktion", ReadOnly = true, Width = 300, Frozen = true });
        foreach (var p in positionen)
            Tabelle.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = p, Width = 82 });
        Tabelle.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "nur Fachkräfte", Width = 82 });
        Tabelle.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        Tabelle.CurrentCellDirtyStateChanged += (_, _) => { if (Tabelle.IsCurrentCellDirty) Tabelle.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        Tabelle.CellValueChanged += (_, e) => { if (!laden && e.RowIndex >= 0 && e.ColumnIndex > 0) Setzen(e.RowIndex, e.ColumnIndex, Tabelle[e.ColumnIndex, e.RowIndex].Value is true); };
        zeile(null, Gesperrt);
        zeile(null, Tabelle);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        zeile(null, schliessen);
        Controls.Add(raster);
        CancelButton = schliessen;
        Laden();
        Stil.DpiSkalierungEinschalten(this);
    }

    public void Laden()
    {
        laden = true;
        var rechte = speicher.Rechte();
        var nurFachkraft = speicher.NurFachkraft();
        Tabelle.Rows.Clear();
        foreach (var (schluessel, name) in Funktionen.Alle)
        {
            var werte = new List<object> { name };
            werte.AddRange(positionen.Select(p => (object)rechte[schluessel].Contains(p)));
            werte.Add(nurFachkraft.Contains(schluessel));
            Tabelle.Rows.Add([.. werte]);
        }
        string? grund = Recht.Grund(speicher, Funktionen.RechteVerwalten);
        Tabelle.ReadOnly = grund is not null;
        Gesperrt.Text = grund is null ? "" : $"Nur ansehen: {grund}";
        Gesperrt.Visible = grund is not null;
        // Schritt 26a2: Spalte „Administrator“ immer voll, ausgegraut, mit Hinweis.
        int admin = Spalte(RechteRegeln.Administrator);
        if (admin > 0)
        {
            var spalte = Tabelle.Columns[admin];
            spalte.ReadOnly = true;
            spalte.DefaultCellStyle.BackColor = Stil.Seite;
            spalte.DefaultCellStyle.ForeColor = Stil.TextLeise;
            spalte.HeaderCell.ToolTipText = RechteRegeln.AdminImmerText;
            foreach (DataGridViewRow r in Tabelle.Rows)
            {
                r.Cells[admin].Value = true;
                r.Cells[admin].ToolTipText = RechteRegeln.AdminImmerText;
            }
        }
        laden = false;
    }

    // Ein Häkchen setzen (auch für Tests): Zeile = Funktion, Spalte 1… = Positionen, letzte = nur Fachkräfte.
    public bool Setzen(int zeile, int spalte, bool an)
    {
        if (Recht.Grund(speicher, Funktionen.RechteVerwalten) is not null) { Laden(); return false; }
        string funktion = Funktionen.Alle[zeile].Schluessel;
        if (spalte == Spalte(RechteRegeln.Administrator)) { Laden(); return false; }   // immer alle Rechte
        if (spalte == positionen.Count + 1) speicher.NurFachkraftSetzen(funktion, an);
        else speicher.RechtSetzen(funktion, positionen[spalte - 1], an);
        if (Tabelle[spalte, zeile].Value is not bool b || b != an) { laden = true; Tabelle[spalte, zeile].Value = an; laden = false; }
        return true;
    }

    public int Spalte(string position) => positionen.IndexOf(position) + 1;
    public int Zeile(string funktion) => Array.FindIndex(Funktionen.Alle, f => f.Schluessel == funktion);
}
