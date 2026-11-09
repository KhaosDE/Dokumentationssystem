using System.ComponentModel;
using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

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
    public Button BedarfKnopf { get; } = Stil.NebenKnopf("Bedarf gegeben …");
    public bool BedarfMoeglich { get; }
    public IReadOnlyList<string> MarkenTexte { get; }
    public string DosisText => dosis.Text;
    // Welche Änderungen hier passen (z. B. kein „Pause …“ bei Pausiertem) und ihre Knöpfe.
    public IReadOnlySet<AenderungArt> Moeglich { get; }
    public IReadOnlyList<(Button Knopf, AenderungArt Art)> Aenderungen =>
        [(PauseKnopf, AenderungArt.Pause), (PauseEndeKnopf, AenderungArt.PauseEnde), (StufenplanKnopf, AenderungArt.Stufenplan), (AbsetzenKnopf, AenderungArt.Absetzen)];

    public VerordnungKarte(Tagesstand s, string? bedarfHeute = null)
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
        if (bedarfHeute is not null) liste.Add((bedarfHeute, Stil.Zimmer));
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
        BedarfMoeglich = v.Bedarf && s.Zustand == Zustand.Gilt;
        BedarfKnopf.Visible = BedarfMoeglich;
        foreach (var k in new[] { BedarfKnopf, PauseKnopf, PauseEndeKnopf, StufenplanKnopf, AbsetzenKnopf, VerlaufKnopf })
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
