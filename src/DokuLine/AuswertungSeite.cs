using DokuLine.Kern;

namespace DokuLine;

// Bereich „Auswertung“ (Bauplan 41, nur mit Recht „Zeitauswertung ansehen“): Zeitraum wählen, je Bewohner des gewählten
// Bereichs die erfassten Minuten nach Lebensbereich und Art, dazu geplant gegenüber tatsächlich bei den Maßnahmen.
// Drucken und Speichern als CSV-Datei (für Excel). Eine Auswertung nach Mitarbeiter gibt es ausdrücklich nicht.
public sealed class AuswertungSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Auswertung";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [AuswertenKnopf, DruckKnopf, CsvKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    public TextBox Von { get; } = new DatumFeld { Width = 100, AccessibleName = "von" };
    public TextBox Bis { get; } = new DatumFeld { Width = 100, AccessibleName = "bis" };
    public Button AuswertenKnopf { get; } = Stil.HauptKnopf("Auswerten");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public Button CsvKnopf { get; } = Stil.NebenKnopf("Als CSV speichern …");
    public Label Hinweis { get; } = new()
    {
        Text = ZeitauswertungRegeln.Hinweis, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche,
        Padding = new Padding(Stil.AbstandMini), UseMnemonic = false,
    };
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Seite, Visible = false, UseMnemonic = false };
    public ListView Tabelle { get; } = new()
    {
        View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable, AccessibleName = "Zeitauswertung", Font = Stil.Standard,
    };
    public List<ZeitZeile> Zeilen { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    internal Func<string?> DateiWaehlen { get; set; }
    internal Action<Func<System.Drawing.Printing.PrintDocument>, string> Vorschau { get; set; }
    public event Action? Geaendert;

    public AuswertungSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        DateiWaehlen = () =>
        {
            using var dialog = new SaveFileDialog { Filter = "CSV-Datei (*.csv)|*.csv", FileName = $"Zeitauswertung {Von.Text}-{Bis.Text}.csv" };
            return dialog.ShowDialog(FindForm()) == DialogResult.OK ? dialog.FileName : null;
        };
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(FindForm()!, bauen, titel);
        var heute = DateOnly.FromDateTime(Jetzt());
        Von.Text = Datum.Text(new DateOnly(heute.Year, heute.Month, 1));
        Bis.Text = Datum.Text(heute);
        foreach (var c in new Control[] { new Label { Text = "Zeitraum", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) }, Von,
                     new Label { Text = "bis", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) }, Bis })
        {
            if (c.Margin == Padding.Empty || c is TextBox) c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        foreach (var (titel, breite) in ZeitauswertungRegeln.Spalten.Select((s, i) => (s, i switch { 0 => 70, 1 => 180, 10 => 140, 12 => 130, _ => 80 })))
            Tabelle.Columns.Add(titel, breite, titel == "Bewohner" || titel == "Zimmer" ? HorizontalAlignment.Left : HorizontalAlignment.Right);
        AuswertenKnopf.Click += (_, _) => Aktualisieren();
        DruckKnopf.Click += (_, _) => Drucken();
        CsvKnopf.Click += (_, _) => CsvSpeichern();
        Controls.AddRange([leiste, Hinweis, Fehler, Tabelle]);
    }

    private (DateOnly Von, DateOnly Bis)? Zeitraum()
    {
        try { return (Datum.Lesen(Von.Text, "von"), Datum.Lesen(Bis.Text, "bis")); }
        catch (FormatException e) { Melden(e.Message); return null; }
    }

    private void Melden(string? text)
    {
        Fehler.Text = text ?? "";
        Fehler.Visible = text is not null;
        PerformLayout();
    }

    public void Aktualisieren()
    {
        Recht.Anwenden(speicher, Funktionen.Zeitauswertung, AuswertenKnopf, DruckKnopf, CsvKnopf);
        Tabelle.Items.Clear();
        Zeilen = [];
        if (Stil.Sperrgrund(AuswertenKnopf) is string grund) { Melden(grund); return; }
        if (Zeitraum() is not var (von, bis)) return;
        var bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt);
        bewohner = [.. bewohner.OrderBy(b => b.Zimmer.PadLeft(8), StringComparer.Ordinal)];
        try { Zeilen = speicher.Zeitauswertung(bewohner, von, bis, Recht.Kuerzel); }
        catch (Eingabefehler e) { Melden(string.Join(" ", e.Fehler)); return; }
        Melden(null);
        Tabelle.BeginUpdate();
        foreach (var z in Zeilen) Tabelle.Items.Add(new ListViewItem(ZeitauswertungRegeln.Werte(z)) { Tag = z });
        Tabelle.EndUpdate();
        int summe = Zeilen.Sum(z => z.Summe);
        KopfText = $"{Arbeitsbereich.Gewaehlt} · {Datum.Text(von)} – {Datum.Text(bis)} · {MassnahmenRegeln.Dauer(summe)} erfasst";
        PerformLayout();
        Geaendert?.Invoke();
    }

    public bool Drucken()
    {
        if (Stil.Sperrgrund(DruckKnopf) is not null || Zeitraum() is not var (von, bis)) return false;
        float[] breiten = [.. ZeitauswertungRegeln.Spalten.Select((_, i) => i switch { 0 => 0.7f, 1 => 2f, 10 => 1.4f, 12 => 1.3f, _ => 0.8f })];
        var druck = new TabellenDruck("Zeitauswertung (Minuten)", $"{Arbeitsbereich.Gewaehlt} · {Datum.Text(von)} – {Datum.Text(bis)} · {ZeitauswertungRegeln.Hinweis}",
            ZeitauswertungRegeln.Spalten, breiten, [.. Zeilen.Select(ZeitauswertungRegeln.Werte)]);
        Vorschau(druck.DokumentBauen, "Zeitauswertung");
        return true;
    }

    public bool CsvSpeichern()
    {
        if (Stil.Sperrgrund(CsvKnopf) is not null || Zeitraum() is not var (von, bis) || DateiWaehlen() is not string datei) return false;
        try { File.WriteAllText(datei, ZeitauswertungRegeln.Csv(Zeilen, von, bis), new System.Text.UTF8Encoding(true)); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Melden(e.Message); return false; }
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, ClientSize.Width - 2 * rand), y = rand;
        foreach (var c in new Control[] { leiste, Hinweis, Fehler }.Where(c => c.Visible))
        {
            c.MaximumSize = new Size(breite, 0);
            c.Location = new Point(rand, y);
            y = c.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        Tabelle.SetBounds(rand, y, breite, Math.Max(Stil.Px(this, 120), ClientSize.Height - y - rand));
    }
}
