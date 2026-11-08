using System.ComponentModel;
using System.Drawing.Printing;
using Dokusystem.Kern;

namespace Dokusystem;

// Reiter „Medikamentenplan“ in der Akte (Schritt 21): „Was gilt am …“ (heute oder ein gewählter Tag) als
// Tabelle, berechnet aus Grundverordnung, Pausen und Stufenplänen; Bedarfsmedikation als eigene Tabelle
// darunter. Klick auf eine Zeile öffnet Einzelheiten und Verlauf; dort Pause, Stufenplan, Absetzen.
// Demo — nicht zum Stellen verwenden.
public sealed class MedikationAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly Label regelTitel = Titel("REGELMÄSSIG");
    private readonly Label bedarfTitel = Titel("BEI BEDARF");
    private Bewohner? bewohner;

    public TextBox TagFeld { get; } = new() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public Button HeuteKnopf { get; } = Stil.NebenKnopf("Heute");
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Verordnung …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public CheckBox AbgesetzteZeigen { get; } = new() { Text = "Abgesetzte zeigen", AutoSize = true, BackColor = Stil.Seite };
    public MarkenFeld Hinweis { get; } = new(MedikationRegeln.DruckHinweis, Stil.Achtung);
    public Label TagFehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Seite, Visible = false };
    public DataGridView Tabelle { get; } = PlanTabelle.Bauen(
        ("medikament", "Medikament", 30), ("morgens", "morgens", 7), ("mittags", "mittags", 7), ("abends", "abends", 7), ("nachts", "nachts", 7),
        ("arzt", "verordnet von", 13), ("seit", "seit", 9), ("status", "Status", 14), ("hinweis", "Hinweis", 16));
    public DataGridView BedarfTabelle { get; } = PlanTabelle.Bauen(
        ("medikament", "Medikament", 30), ("anlass", "Anlass", 18), ("einzeldosis", "Einzeldosis", 9), ("hoechstens", "höchstens", 11),
        ("arzt", "verordnet von", 13), ("seit", "seit", 9), ("status", "Status", 14), ("hinweis", "Hinweis", 16));
    public Label RegelLeer { get; } = Leer("Keine regelmäßige Medikation an diesem Tag.");
    public Label BedarfLeer { get; } = Leer("Keine Bedarfsmedikation.");

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public MedikationAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        var beschriftung = new Label { Text = "Was gilt am", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) };
        foreach (var c in new Control[] { beschriftung, TagFeld, HeuteKnopf, NeuKnopf, DruckKnopf, AbgesetzteZeigen, Hinweis, TagFehler })
        {
            if (c != beschriftung) c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        TagFeld.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandKlein);
        AbgesetzteZeigen.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        Hinweis.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        TagFeld.TextChanged += (_, _) => Aktualisieren();
        HeuteKnopf.Click += (_, _) => TagFeld.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        NeuKnopf.Click += (_, _) => NeueVerordnung();
        DruckKnopf.Click += (_, _) => Drucken();
        AbgesetzteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Tabelle.AccessibleName = "Medikamentenplan, regelmäßig";
        BedarfTabelle.AccessibleName = "Medikamentenplan, bei Bedarf";
        foreach (var t in new[] { Tabelle, BedarfTabelle })
        {
            t.CellClick += (_, e) => { if (e.RowIndex >= 0) Oeffnen(PlanTabelle.Stand(t.Rows[e.RowIndex])); };
            t.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter || t.CurrentRow is null) return;
                e.SuppressKeyPress = true;
                Oeffnen(PlanTabelle.Stand(t.CurrentRow));
            };
        }
        Controls.AddRange([leiste, regelTitel, Tabelle, RegelLeer, bedarfTitel, BedarfTabelle, BedarfLeer]);
    }

    private static Label Titel(string text) => new() { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };

    private static Label Leer(string text) => new() { Text = text, AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, Visible = false };

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        if (TagFeld.Text != heute) TagFeld.Text = heute;   // löst Aktualisieren aus
        else Aktualisieren();
    }

    public DateOnly? Stichtag()
    {
        try { return Datum.Lesen(TagFeld.Text, "Tag"); }
        catch (FormatException) { return null; }
    }

    public List<Tagesstand> Staende(DateOnly tag) =>
        bewohner?.Id is long id ? speicher.Verordnungen(id).Select(v => MedikationRegeln.Stand(v, tag)).ToList() : [];

    // Zeilen der Tabelle „Regelmäßig“ in der gezeigten Reihenfolge (nach Sortierung).
    public List<Tagesstand> Eintraege => PlanTabelle.Staende(Tabelle);
    public List<Tagesstand> BedarfEintraege => PlanTabelle.Staende(BedarfTabelle);

    // „aktiv“, „pausiert bis …“, „Stufe 2 von 4 · nächste Stufe ab …“, „abgesetzt am …“, „beginnt am …“.
    public static string StatusText(Tagesstand s) =>
        (s.Zustand == Zustand.Gilt ? "aktiv" : s.Text) + (s.Naechstes is string n ? $" · {n}" : "");

    public void Aktualisieren()
    {
        if (bewohner is null) return;
        Stil.Sperren(NeuKnopf, Recht.Grund(speicher, Funktionen.MedikationBearbeiten));   // Rechte (Schritt 19)
        var tag = Stichtag();
        TagFehler.Visible = tag is null;
        TagFehler.Text = "Tag bitte als TT.MM.JJJJ eingeben.";
        var staende = tag is DateOnly t ? Staende(t) : [];
        bool Gezeigt(Tagesstand s) => s.Sichtbar || s.Zustand == Zustand.NochNicht || (AbgesetzteZeigen.Checked && s.Zustand == Zustand.Abgesetzt);
        PlanTabelle.Fuellen(Tabelle, staende.Where(s => Gezeigt(s) && !s.Verordnung.Bedarf), s =>
        {
            var v = s.Verordnung;
            var d = s.Dosis.Werte.Select(w => Menge(w, v.Einheit)).ToArray();
            return [MedikamentText(v), d[0], d[1], d[2], d[3], v.Anordnung.Arzt, v.Beginn.ToDateTime(TimeOnly.MinValue), StatusText(s), v.Hinweis];
        });
        PlanTabelle.Fuellen(BedarfTabelle, staende.Where(s => Gezeigt(s) && s.Verordnung.Bedarf), s =>
        {
            var v = s.Verordnung;
            return [MedikamentText(v), v.BedarfAnlass, Menge(v.Dosis.Morgens, v.Einheit), v.BedarfHoechstens.Length > 0 ? v.BedarfHoechstens : "keine Angabe",
                    v.Anordnung.Arzt, v.Beginn.ToDateTime(TimeOnly.MinValue), StatusText(s), v.Hinweis];
        });
        Tabelle.Visible = Tabelle.Rows.Count > 0;
        RegelLeer.Visible = tag is not null && Tabelle.Rows.Count == 0;
        BedarfTabelle.Visible = BedarfTabelle.Rows.Count > 0;
        BedarfLeer.Visible = tag is not null && BedarfTabelle.Rows.Count == 0;
        PerformLayout();
    }

    private static string MedikamentText(Verordnung v) => v.Medikament + (v.Staerke.Length > 0 ? $" · {v.Staerke}" : " · Stärke: keine Angabe");

    // Menge je Einnahme; die Einheit nur, wenn es nicht „Stück“ sind (z. B. „20 Tropfen“).
    private static string Menge(string wert, string einheit) =>
        einheit.Length == 0 || einheit == "Stück" || wert == "0" ? wert : $"{wert} {einheit}";

    // Klick auf eine Zeile: Einzelheiten und Verlauf. Wählt man dort eine Änderung, öffnet sie sich danach.
    public void Oeffnen(Tagesstand s)
    {
        string? grund = Recht.Grund(speicher, Funktionen.MedikationBearbeiten);
        AenderungArt? wunsch;
        using (var fenster = new VerordnungDetailFenster(s, grund))
        {
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
            wunsch = fenster.Wunsch;
        }
        if (wunsch is AenderungArt art) Aendern(s.Verordnung, art);
    }

    public void NeueVerordnung()
    {
        if (bewohner is null) return;
        using var fenster = new VerordnungFenster(speicher, bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Aendern(Verordnung v, AenderungArt art)
    {
        using var fenster = new AenderungFenster(speicher, v, art);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Drucken()
    {
        if (bewohner is null || Stichtag() is not DateOnly tag) return;
        var druck = new MedikationDruck(bewohner, tag, Staende(tag));
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Medikamentenplan {bewohner.Name}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        leiste.MaximumSize = new Size(Width, 0);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(Math.Max(100, Width), 0)));   // AutoSize greift hier zu spät
        int y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var (titel, tabelle, leer) in new[] { (regelTitel, Tabelle, RegelLeer), (bedarfTitel, BedarfTabelle, BedarfLeer) })
        {
            titel.Location = new Point(Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandKlein));
            y = titel.Bottom + Stil.Px(this, Stil.AbstandKlein);
            if (tabelle.Visible)
            {
                tabelle.Width = Width;
                tabelle.Height = PlanTabelle.Hoehe(tabelle);
                tabelle.Location = new Point(0, y);
                y = tabelle.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
            if (leer.Visible)
            {
                leer.Location = new Point(Stil.Px(this, Stil.AbstandMini), y);
                y = leer.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
        }
        if (Height != y) Height = y;
    }
}

// Tabellen des Medikamentenplans: nur lesen, ganze Zeile wählbar, sortierbar über die Spaltenköpfe,
// Zeilen abwechselnd hinterlegt; jede Zeile trägt ihren Tagesstand. Ohne senkrechte Bildlaufleiste –
// die Tabelle ist so hoch wie ihr Inhalt, gerollt wird die Akte. Ist sie schmaler als die
// Mindestbreiten der Spalten, rollt sie waagerecht.
public static class PlanTabelle
{
    public static DataGridView Bauen(params (string Name, string Titel, float Gewicht)[] spalten)
    {
        var t = new DataGridView
        {
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AllowUserToOrderColumns = false,
            RowHeadersVisible = false, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize, ScrollBars = ScrollBars.Horizontal,
            BackgroundColor = Stil.Flaeche, BorderStyle = BorderStyle.FixedSingle, GridColor = Stil.Trennlinie,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single, Font = Stil.Standard, StandardTab = true, Cursor = Cursors.Hand,
        };
        t.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Stil.Fensterhintergrund, ForeColor = Stil.TextMittel, Font = Stil.Fett, WrapMode = DataGridViewTriState.True,
            SelectionBackColor = Stil.Fensterhintergrund, SelectionForeColor = Stil.TextMittel, Padding = new Padding(4, 6, 4, 6),
        };
        t.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Stil.Flaeche, ForeColor = Stil.TextNormal, SelectionBackColor = Stil.AkzentHell, SelectionForeColor = Stil.TextNormal,
            WrapMode = DataGridViewTriState.True, Padding = new Padding(4, 6, 4, 6),
        };
        t.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Stil.Seite };
        foreach (var (name, titel, gewicht) in spalten)
        {
            var spalte = new DataGridViewTextBoxColumn { Name = name, HeaderText = titel, FillWeight = gewicht, SortMode = DataGridViewColumnSortMode.Automatic };
            // Nie schmaler als die Überschrift (mit Sortierpfeil); „seit“ mindestens ein ganzes Datum.
            int noetig = TextRenderer.MeasureText(titel, Stil.Fett).Width;
            if (name == "seit")
            {
                spalte.ValueType = typeof(DateTime);   // sortiert nach Datum, nicht nach Text
                spalte.DefaultCellStyle.Format = "dd.MM.yyyy";
                noetig = Math.Max(noetig, TextRenderer.MeasureText("00.00.0000", Stil.Standard).Width);
            }
            spalte.MinimumWidth = noetig + 26;
            if (name is "morgens" or "mittags" or "abends" or "nachts" or "einzeldosis")
                spalte.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            t.Columns.Add(spalte);
        }
        return t;
    }

    // Neu füllen; eine gewählte Sortierung bleibt erhalten.
    public static void Fuellen(DataGridView t, IEnumerable<Tagesstand> staende, Func<Tagesstand, object[]> werte)
    {
        var sortiert = t.SortedColumn;
        var richtung = t.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        t.Rows.Clear();
        foreach (var s in staende)
        {
            var zeile = t.Rows[t.Rows.Add(werte(s))];
            zeile.Tag = s;
            if (s.Zustand is Zustand.Pausiert or Zustand.Abgesetzt or Zustand.NochNicht) zeile.DefaultCellStyle.ForeColor = Stil.TextLeise;
            var marke = s.Zustand switch
            {
                Zustand.Pausiert => Stil.Achtung,
                Zustand.Stufenplan => Stil.Unterwegs,
                Zustand.Gilt => Stil.Offen,
                _ => Stil.Zimmer,
            };
            var status = zeile.Cells["status"].Style;
            status.BackColor = status.SelectionBackColor = marke.Flaeche;
            status.ForeColor = status.SelectionForeColor = marke.Text;
        }
        if (sortiert is not null) t.Sort(sortiert, richtung);
        t.ClearSelection();
    }

    public static Tagesstand Stand(DataGridViewRow zeile) => (Tagesstand)zeile.Tag!;

    public static List<Tagesstand> Staende(DataGridView t) => t.Rows.Cast<DataGridViewRow>().Select(Stand).ToList();

    // Angezeigter Text einer Zelle (Datum als TT.MM.JJJJ).
    public static string Text(DataGridView t, int zeile, string spalte) => t.Rows[zeile].Cells[spalte].FormattedValue as string ?? "";

    public static int Hoehe(DataGridView t)
    {
        int waagerecht = t.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) > t.ClientSize.Width - 2 ? SystemInformation.HorizontalScrollBarHeight : 0;
        return t.ColumnHeadersHeight + t.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + waagerecht + 2;
    }
}

// Einzelheiten einer Verordnung (Klick auf eine Zeile): Karte mit Zustand, Dosis, Anordnung und den
// Knöpfen für Änderungen, darunter der Verlauf. Ein Knopf schließt das Fenster mit dem Wunsch;
// die Änderung öffnet die Ansicht danach (keine Fenster in Fenstern).
public sealed class VerordnungDetailFenster : Form
{
    private readonly string? sperrgrund;

    public VerordnungKarte Karte { get; }
    public TextBox Verlauf { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 620, Height = 220, BackColor = Stil.Flaeche };
    public AenderungArt? Wunsch { get; private set; }

    public VerordnungDetailFenster(Tagesstand s, string? sperrgrund)
    {
        this.sperrgrund = sperrgrund;
        Fenster.Grundform(this, s.Verordnung.Medikament);
        Karte = new VerordnungKarte(s) { Width = 620, Margin = new Padding(0, 0, 0, Stil.AbstandMittel) };
        Karte.VerlaufKnopf.Visible = false;   // der Verlauf steht darunter
        foreach (var (knopf, art) in Karte.Aenderungen)
        {
            Stil.Sperren(knopf, sperrgrund);   // Rechte (Schritt 19): sichtbar, aber gesperrt
            knopf.Click += (_, _) => Waehlen(art);
        }
        Karte.PerformLayout();
        Verlauf.Text = string.Join(Environment.NewLine + Environment.NewLine, MedikationRegeln.Verlauf(s.Verordnung));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(MedikationRegeln.DruckHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        zeile(null, Karte);
        zeile(null, new Label { Text = "VERLAUF", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Verlauf);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        schliessen.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        zeile(null, schliessen);
        Controls.Add(raster);
        CancelButton = schliessen;
        Stil.DpiSkalierungEinschalten(this);
    }

    // Änderung wählen (wie der Knopf); geht nicht, wenn sie hier nicht passt oder das Recht fehlt.
    public bool Waehlen(AenderungArt art)
    {
        if (sperrgrund is not null || !Karte.Moeglich.Contains(art)) return false;
        Wunsch = art;
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Eine Verordnung an einem Tag: Medikament und Stärke, die Mengen je Einnahmezeit, der Zustand
// (Grundverordnung, Stufe x von y, pausiert bis …), die Anordnung und die Knöpfe für Änderungen.
public sealed class VerordnungKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label name = new();
    private readonly Label dosis = new();
    private readonly Label anordnung = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Tagesstand Stand { get; }
    public Button PauseKnopf { get; } = Stil.NebenKnopf("Pause …");
    public Button PauseEndeKnopf { get; } = Stil.NebenKnopf("Pause beenden …");
    public Button StufenplanKnopf { get; } = Stil.NebenKnopf("Stufenplan …");
    public Button AbsetzenKnopf { get; } = Stil.NebenKnopf("Absetzen …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public IReadOnlyList<string> MarkenTexte { get; }
    public string DosisText => dosis.Text;
    // Welche Änderungen hier passen (z. B. kein „Pause …“ bei Pausiertem) und ihre Knöpfe.
    public IReadOnlySet<AenderungArt> Moeglich { get; }
    public IReadOnlyList<(Button Knopf, AenderungArt Art)> Aenderungen =>
        [(PauseKnopf, AenderungArt.Pause), (PauseEndeKnopf, AenderungArt.PauseEnde), (StufenplanKnopf, AenderungArt.Stufenplan), (AbsetzenKnopf, AenderungArt.Absetzen)];

    public VerordnungKarte(Tagesstand s)
    {
        Stand = s;
        var v = s.Verordnung;
        var liste = new List<(string, Stil.Marke)>();
        var markeZustand = s.Zustand switch
        {
            Zustand.Pausiert => Stil.Achtung,
            Zustand.Stufenplan => Stil.Unterwegs,
            Zustand.Abgesetzt => Stil.Zimmer,
            Zustand.NochNicht => Stil.Zimmer,
            _ => Stil.Offen,
        };
        liste.Add((s.Text, markeZustand));
        if (s.Naechstes is string n) liste.Add((n, Stil.Unterwegs));
        if (v.Aenderungen.Count > 0) liste.Add(($"{v.Aenderungen.Count} Änderung{(v.Aenderungen.Count == 1 ? "" : "en")}", Stil.Zimmer));
        MarkenTexte = liste.Select(x => x.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        name.Text = v.Medikament + (v.Staerke.Length > 0 ? $" · {v.Staerke}" : " · Stärke: keine Angabe");
        name.Font = Stil.KarteName;
        name.ForeColor = s.Zustand is Zustand.Pausiert or Zustand.Abgesetzt ? Stil.TextLeise : Stil.TextNormal;
        dosis.Text = v.Bedarf
            ? $"bei Bedarf: {v.BedarfAnlass} · Einzeldosis {v.Dosis.Morgens} {v.Einheit}" + (v.BedarfHoechstens.Length > 0 ? $" · höchstens {v.BedarfHoechstens}" : "")
            : s.Zustand == Zustand.Pausiert ? $"pausiert – sonst morgens {s.Dosis.Morgens} · mittags {s.Dosis.Mittags} · abends {s.Dosis.Abends} · nachts {s.Dosis.Nachts}"
            : $"morgens {s.Dosis.Morgens} · mittags {s.Dosis.Mittags} · abends {s.Dosis.Abends} · nachts {s.Dosis.Nachts}  ({v.Einheit})";
        dosis.Font = s.Zustand == Zustand.Pausiert ? Stil.Standard : Stil.Fett;
        dosis.ForeColor = s.Zustand == Zustand.Pausiert ? Stil.TextLeise : Stil.TextNormal;
        anordnung.Text = $"angeordnet von {v.Anordnung.Arzt} am {Datum.Text(v.Anordnung.Am)} · seit {Datum.Text(v.Beginn)}" + (v.Hinweis.Length > 0 ? $" · {v.Hinweis}" : "");
        anordnung.Font = Stil.Klein;
        anordnung.ForeColor = Stil.TextLeise;
        foreach (var l in new[] { name, dosis, anordnung })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        bool aktiv = s.Zustand is not Zustand.Abgesetzt;
        var moeglich = new HashSet<AenderungArt>();
        if (aktiv && s.Zustand != Zustand.Pausiert) moeglich.Add(AenderungArt.Pause);
        if (s.Zustand == Zustand.Pausiert) moeglich.Add(AenderungArt.PauseEnde);
        if (aktiv && !v.Bedarf) moeglich.Add(AenderungArt.Stufenplan);
        if (aktiv) moeglich.Add(AenderungArt.Absetzen);
        Moeglich = moeglich;
        foreach (var (knopf, art) in Aenderungen) knopf.Visible = moeglich.Contains(art);
        foreach (var k in new[] { PauseKnopf, PauseEndeKnopf, StufenplanKnopf, AbsetzenKnopf, VerlaufKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{name.Text}: {dosis.Text}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Stand is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var l in new[] { name, dosis, anordnung })
        {
            l.MaximumSize = new Size(breite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, 3);
        }
        knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int hoehe = knoepfe.Bottom + innen - Stil.Px(this, Stil.AbstandMini);
        if (Height != hoehe) Height = hoehe;
    }
}

// Neue Verordnung: Medikament aus der Datenbank, Stärke, Mengen je Einnahmezeit oder Bedarf,
// Beginn und Anordnung (Arzt und Datum, in der Demo ausgedacht).
public sealed class VerordnungFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<Medikament> medikamente;

    public ComboBox MedikamentWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    public ComboBox Staerke { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 160 };
    public TextBox Einheit { get; } = new() { Width = 100, Text = "Stück" };
    public CheckBox Bedarf { get; } = new() { Text = "Bedarfsmedikation", AutoSize = true };
    public TextBox[] Mengen { get; } = Enumerable.Range(0, 4).Select(_ => new TextBox { Width = 50, Text = "0", TextAlign = HorizontalAlignment.Center }).ToArray();
    public TextBox Anlass { get; } = new() { Width = 260, PlaceholderText = "z. B. bei starker Unruhe", Enabled = false };
    public TextBox Hoechstens { get; } = new() { Width = 160, PlaceholderText = "z. B. 2 in 24 Stunden", Enabled = false };
    public TextBox Beginn { get; } = new() { Width = 100 };
    public TextBox Arzt { get; } = new() { Width = 260, PlaceholderText = "ausgedacht, z. B. Dr. Ulme" };
    public TextBox AngeordnetAm { get; } = new() { Width = 100 };
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Verordnung? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VerordnungFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        medikamente = speicher.Medikamente().Where(m => !m.Gesperrt).ToList();   // gesperrte nicht wählbar (Schritt 21)
        Text = $"Neue Verordnung – {bewohner.Name} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(MedikationRegeln.DruckHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        MedikamentWahl.Items.AddRange(medikamente.Select(m => (object)m.Auswahltext).ToArray());   // „… – ungeprüft“
        MedikamentWahl.SelectedIndexChanged += (_, _) =>
        {
            Staerke.Items.Clear();
            if (MedikamentWahl.SelectedIndex >= 0) Staerke.Items.AddRange(medikamente[MedikamentWahl.SelectedIndex].Aktuell.Staerken.Select(x => (object)x).ToArray());
            Staerke.Text = Staerke.Items.Count > 0 ? (string)Staerke.Items[0]! : "";
        };
        zeile("Medikament", MedikamentWahl);
        zeile("Stärke, Einheit", Formular.Reihe(Staerke, Einheit));
        zeile("", Bedarf);
        var mengen = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        for (int i = 0; i < 4; i++)
        {
            mengen.Controls.Add(new Label { Text = MedikationRegeln.Zeiten[i], AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMini, 0) });
            Mengen[i].Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandMittel, 0);
            mengen.Controls.Add(Mengen[i]);
        }
        zeile("Menge", mengen);
        zeile("Bei Bedarf: Anlass", Anlass);
        zeile("höchstens", Hoechstens);
        Bedarf.CheckedChanged += (_, _) =>
        {
            Anlass.Enabled = Hoechstens.Enabled = Bedarf.Checked;
            for (int i = 1; i < 4; i++) Mengen[i].Enabled = !Bedarf.Checked;   // Bedarf: nur eine Einzeldosis (erstes Feld)
        };
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        Beginn.Text = heute;
        AngeordnetAm.Text = heute;
        zeile("Beginn", Beginn);
        zeile("Angeordnet von, am", Formular.Reihe(Arzt, AngeordnetAm));
        zeile("Hinweis", Hinweis);
        zeile("Kürzel", new Label { Text = Einstellungen.Laden().Kuerzel is { Length: > 0 } k ? k : "– bitte oben rechts anmelden –", AutoSize = true, Font = Stil.Fett, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) });
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            var m = MedikamentWahl.SelectedIndex >= 0 ? medikamente[MedikamentWahl.SelectedIndex] : null;
            var v = MedikationRegeln.AusEingabe(new VerordnungEingabe
            {
                BewohnerId = bewohner.Id!.Value, MedikamentId = m?.Id ?? 0, Staerke = Staerke.Text, Einheit = Einheit.Text,
                Morgens = Mengen[0].Text, Mittags = Mengen[1].Text, Abends = Mengen[2].Text, Nachts = Mengen[3].Text,
                Bedarf = Bedarf.Checked, BedarfAnlass = Anlass.Text, BedarfHoechstens = Hoechstens.Text,
                Beginn = Beginn.Text, Arzt = Arzt.Text, AngeordnetAm = AngeordnetAm.Text, Hinweis = Hinweis.Text,
                Kuerzel = Einstellungen.Laden().Kuerzel,
            }, m?.Anzeige ?? "", Jetzt());
            Gespeichert = speicher.VerordnungSpeichern(v);
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

// Pause, Pause beenden, Stufenplan oder Absetzen – immer mit Anordnung (Arzt, Datum).
public sealed class AenderungFenster : Form
{
    public const int StufenZeilen = 6;
    private readonly Speicher speicher;
    private readonly Verordnung verordnung;
    private readonly AenderungArt art;

    public TextBox Von { get; } = new() { Width = 100 };
    public TextBox Bis { get; } = new() { Width = 100, PlaceholderText = "leer = bis auf Weiteres" };
    public TextBox[,] Stufen { get; } = new TextBox[StufenZeilen, 5];
    public TextBox Arzt { get; } = new() { Width = 260, PlaceholderText = "ausgedacht, z. B. Dr. Ulme" };
    public TextBox AngeordnetAm { get; } = new() { Width = 100 };
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AenderungFenster(Speicher speicher, Verordnung v, AenderungArt art)
    {
        this.speicher = speicher;
        verordnung = v;
        this.art = art;
        string titel = art switch
        {
            AenderungArt.Pause => "Pause",
            AenderungArt.PauseEnde => "Pause beenden",
            AenderungArt.Stufenplan => "Stufenplan",
            _ => "Absetzen",
        };
        Text = $"{titel} – {v.Medikament} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = $"{v.Medikament} · {v.Staerke} · Grundverordnung {v.Dosis.Text}", AutoSize = true, Font = Stil.Fett, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Von.Text = heute;
        switch (art)
        {
            case AenderungArt.Pause:
                zeile("Pause von", Von);
                zeile("bis (einschließlich)", Bis);
                break;
            case AenderungArt.PauseEnde:
                zeile("Pause endet am", Von);
                break;
            case AenderungArt.Absetzen:
                zeile("Abgesetzt ab", Von);
                break;
            default:
                var tabelle = new TableLayoutPanel { ColumnCount = 5, AutoSize = true, Margin = new Padding(0) };
                foreach (var kopf in new[] { "ab Datum", "morgens", "mittags", "abends", "nachts" })
                    tabelle.Controls.Add(new Label { Text = kopf, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel });
                for (int r = 0; r < StufenZeilen; r++)
                    for (int c = 0; c < 5; c++)
                    {
                        Stufen[r, c] = new TextBox { Width = c == 0 ? 100 : 50, TextAlign = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center, Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini) };
                        tabelle.Controls.Add(Stufen[r, c]);
                    }
                zeile("Stufen", tabelle);
                zeile(null, new Label
                {
                    Text = "Leere Zeilen werden nicht beachtet. Ist die letzte Stufe überall 0, ist das Medikament danach abgesetzt; " +
                           "sonst wird die letzte Stufe zur neuen Grundverordnung (die bisherige bleibt im Verlauf).",
                    AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
                });
                break;
        }
        AngeordnetAm.Text = heute;
        zeile("Angeordnet von, am", Formular.Reihe(Arzt, AngeordnetAm));
        zeile("Hinweis", Hinweis);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Eine Stufenzeile füllen (für Tests und schnelle Eingabe).
    public void StufeSetzen(int zeile, string ab, string morgens, string mittags, string abends, string nachts)
    {
        string[] werte = [ab, morgens, mittags, abends, nachts];
        for (int c = 0; c < 5; c++) Stufen[zeile, c].Text = werte[c];
    }

    public bool Speichern()
    {
        string kuerzel = Einstellungen.Laden().Kuerzel;
        var jetzt = Jetzt();
        try
        {
            var a = art switch
            {
                AenderungArt.Pause => MedikationRegeln.Pause(Von.Text, Bis.Text, Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
                AenderungArt.PauseEnde => MedikationRegeln.PauseEnde(Von.Text, Arzt.Text, AngeordnetAm.Text, kuerzel, jetzt),
                AenderungArt.Absetzen => MedikationRegeln.Absetzen(Von.Text, Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
                _ => MedikationRegeln.Stufenplan(Enumerable.Range(0, StufenZeilen)
                        .Select(r => (Stufen[r, 0].Text, Stufen[r, 1].Text, Stufen[r, 2].Text, Stufen[r, 3].Text, Stufen[r, 4].Text))
                        .Where(z => z.Item1.Trim().Length > 0).ToList(), Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
            };
            speicher.AenderungSpeichern(verordnung.Id, a);
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

// Ausdruck des Medikamentenplans „Was gilt am …“ je Bewohner (A4 hoch), mit dem Vermerk „Demo — nicht zum Stellen verwenden“.
public sealed class MedikationDruck(Bewohner bewohner, DateOnly tag, IReadOnlyList<Tagesstand> staende)
{
    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = $"Medikamentenplan {bewohner.Name}" };
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f;
        float y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var normal = new Font("Segoe UI", 10F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        using var rot = new SolidBrush(Color.FromArgb(163, 34, 26));
        g.DrawString($"Medikamentenplan – {bewohner.Name}, Zi. {bewohner.Zimmer}", titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Was gilt am {tag.ToString("dddd, dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))}", normal, Brushes.Black, links, y);
        y += 6f;
        g.DrawString($"{MedikationRegeln.DruckHinweis} · {KopfBereich.DemoHinweis}", fett, rot, links, y);
        y += 7f;
        float[] spalten = [links, links + 80f, links + 100f, links + 120f, links + 140f, links + 160f];
        void Kopf()
        {
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
            string[] k = ["Medikament, Stärke", "morgens", "mittags", "abends", "nachts"];
            for (int i = 0; i < k.Length; i++) g.DrawString(k[i], klein, Brushes.DimGray, spalten[i], y);
            y += 5f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
        }
        g.DrawString("Regelmäßig", fett, Brushes.Black, links, y);
        y += 6f;
        Kopf();
        foreach (var s in staende.Where(x => x.Sichtbar && !x.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            g.DrawString($"{v.Medikament} {v.Staerke}", normal, Brushes.Black, new RectangleF(spalten[0], y, 78f, 12f));
            if (s.Zustand == Zustand.Pausiert)
                g.DrawString(s.Text.ToUpperInvariant(), fett, rot, spalten[1], y);
            else
                for (int i = 0; i < 4; i++) g.DrawString(s.Dosis.Werte.ElementAt(i), normal, Brushes.Black, spalten[i + 1] + 6f, y);
            y += 6f;
            string zusatz = s.Text + (s.Naechstes is string n ? $" · {n}" : "") + $" · angeordnet von {v.Anordnung.Arzt} am {Datum.Text(v.Anordnung.Am)}";
            g.DrawString(zusatz, klein, Brushes.DimGray, spalten[0], y);
            y += 5f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
        }
        y += 4f;
        g.DrawString("Bei Bedarf", fett, Brushes.Black, links, y);
        y += 6f;
        if (!staende.Any(x => x.Sichtbar && x.Verordnung.Bedarf)) g.DrawString("Keine Bedarfsmedikation.", normal, Brushes.DimGray, links, y);
        foreach (var s in staende.Where(x => x.Sichtbar && x.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            g.DrawString($"{v.Medikament} {v.Staerke}: {v.BedarfAnlass}, Einzeldosis {v.Dosis.Morgens} {v.Einheit}" +
                         (v.BedarfHoechstens.Length > 0 ? $", höchstens {v.BedarfHoechstens}" : "") +
                         (s.Zustand == Zustand.Pausiert ? $" – {s.Text}" : ""), normal, Brushes.Black, new RectangleF(links, y, rechts - links, 12f));
            y += 6f;
        }
    }
}
