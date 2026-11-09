using DokuLine.Kern;

namespace DokuLine;

// Kleine Bausteine für Formulare: Raster aus Beschriftung und Feld, Reihe, Speichern/Abbrechen.
public static class Formular
{
    public static TableLayoutPanel Raster(out Action<string?, Control> zeile)
    {
        var raster = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        zeile = (beschriftung, feld) =>
        {
            if (beschriftung is null) { raster.Controls.Add(feld); raster.SetColumnSpan(feld, 2); return; }
            raster.Controls.Add(new Label { Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein) });
            feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            raster.Controls.Add(feld);
        };
        return raster;
    }

    public static FlowLayoutPanel Reihe(params Control[] teile)
    {
        var reihe = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        foreach (var t in teile) t.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        reihe.Controls.AddRange(teile);
        return reihe;
    }

    public static FlowLayoutPanel Knoepfe(Form fenster, string haupt, Action klick)
    {
        var ok = Stil.HauptKnopf(haupt);
        ok.Click += (_, _) => klick();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        ok.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([ok, abbrechen]);
        fenster.CancelButton = abbrechen;
        return knoepfe;
    }
}

// Ein Fenster mit einem längeren Text zum Lesen (z. B. Verlauf).
public sealed class TextFenster : Form
{
    public TextBox Inhalt { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 600, Height = 300, BackColor = Stil.Flaeche };

    public TextFenster(string titel, string text)
    {
        Text = $"{titel} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Inhalt.Text = text;
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        schliessen.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        stapel.Controls.AddRange([Inhalt, schliessen]);
        Controls.Add(stapel);
        CancelButton = schliessen;
        Stil.DpiSkalierungEinschalten(this);
    }
}

// Kleines Fenster mit einem Datum (z. B. „nicht mehr ab“).
public sealed class DatumFenster : Form
{
    private readonly Action<DateOnly> speichern;
    public TextBox Datumsfeld { get; } = new DatumFeld() { Width = 100 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    public DatumFenster(string titel, string beschriftung, DateOnly vorschlag, Action<DateOnly> speichern)
    {
        this.speichern = speichern;
        Text = $"{titel} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        Datumsfeld.Text = Datum.Text(vorschlag);
        zeile(beschriftung, Datumsfeld);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            DateOnly d;
            try { d = Datum.Lesen(Datumsfeld.Text, "Datum"); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            speichern(d);
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

// Ein kurzer Text in einem kleinen Fenster (z. B. der Name eines gespeicherten Filters); null = abgebrochen.
public static class Eingabe
{
    public static string? Fragen(IWin32Window? besitzer, string titel, string frage)
    {
        using var fenster = new Form
        {
            Text = titel, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Stil.Fensterhintergrund, Font = Stil.Standard,
        };
        var feld = new TextBox { Width = 320, AccessibleName = frage };
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = frage, AutoSize = true, MaximumSize = new Size(360, 0), UseMnemonic = false });
        zeile(null, feld);
        zeile(null, Formular.Knoepfe(fenster, "OK", () => { fenster.DialogResult = DialogResult.OK; fenster.Close(); }));
        fenster.Controls.Add(raster);
        Stil.SymbolSetzen(fenster);
        return fenster.ShowDialog(besitzer) == DialogResult.OK ? feld.Text.Trim() : null;
    }

    // Einen von mehreren Einträgen wählen; Index oder null = abgebrochen.
    public static int? Waehlen(IWin32Window? besitzer, string titel, string frage, IReadOnlyList<string> eintraege)
    {
        using var fenster = new Form
        {
            Text = titel, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Stil.Fensterhintergrund, Font = Stil.Standard,
        };
        var wahl = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360, AccessibleName = frage };
        wahl.Items.AddRange([.. eintraege]);
        if (eintraege.Count > 0) wahl.SelectedIndex = 0;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = frage, AutoSize = true, MaximumSize = new Size(380, 0), UseMnemonic = false });
        zeile(null, wahl);
        zeile(null, Formular.Knoepfe(fenster, "OK", () => { fenster.DialogResult = DialogResult.OK; fenster.Close(); }));
        fenster.Controls.Add(raster);
        Stil.SymbolSetzen(fenster);
        return fenster.ShowDialog(besitzer) == DialogResult.OK && wahl.SelectedIndex >= 0 ? wahl.SelectedIndex : null;
    }
}
