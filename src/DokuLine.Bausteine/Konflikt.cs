using DokuLine.Kern;

namespace DokuLine;

public enum KonfliktWahl { Abbrechen, MeineSpeichern, AndereUebernehmen }

// Hinweis, wenn ein anderer Arbeitsplatz denselben Datensatz inzwischen gespeichert hat (Mehrplatz-
// betrieb): beide Fassungen nebeneinander, dann wählen. Es gewinnt nie still der Letzte.
public sealed class KonfliktFenster : Form
{
    public TextBox Meine { get; } = Feld();
    public TextBox Andere { get; } = Feld();
    public Button MeineKnopf { get; } = Stil.HauptKnopf("Meine Fassung speichern");
    public Button AndereKnopf { get; } = Stil.NebenKnopf("Andere übernehmen");
    public KonfliktWahl Wahl { get; private set; } = KonfliktWahl.Abbrechen;

    private static TextBox Feld() => new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 340, Height = 260, BackColor = Stil.Flaeche,
    };

    public KonfliktFenster(Konflikt k)
    {
        Text = $"Inzwischen geändert ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Meine.Text = k.Meine.ReplaceLineEndings("\r\n");
        Andere.Text = k.Andere.ReplaceLineEndings("\r\n");

        var raster = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        void Ganz(Control c) { raster.Controls.Add(c); raster.SetColumnSpan(c, 2); }
        Label Titel(string text) => new()
        {
            Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandMittel, 0, Stil.AbstandMini),
        };
        Ganz(new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        Ganz(new Label
        {
            Text = $"{k.Message} Damit nichts verloren geht, speichert das Programm nicht einfach darüber. " +
                   "Bitte vergleichen und wählen. Frühere Fassungen bleiben im Verlauf.",
            AutoSize = true, MaximumSize = new Size(700, 0), Font = Stil.Fett, ForeColor = Stil.TextNormal,
        });
        raster.Controls.Add(Titel("IHRE FASSUNG (NOCH NICHT GESPEICHERT)"));
        raster.Controls.Add(Titel("INZWISCHEN GESPEICHERT"));
        Meine.Margin = new Padding(0, 0, Stil.AbstandGross, 0);
        Andere.Margin = new Padding(0);
        raster.Controls.Add(Meine);
        raster.Controls.Add(Andere);

        MeineKnopf.Click += (_, _) => Waehlen(KonfliktWahl.MeineSpeichern);
        AndereKnopf.Click += (_, _) => Waehlen(KonfliktWahl.AndereUebernehmen);
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        foreach (var b in new[] { MeineKnopf, AndereKnopf, abbrechen }) { b.AutoSize = true; b.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0); }
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([MeineKnopf, AndereKnopf, abbrechen]);
        Ganz(knoepfe);
        Ganz(new Label
        {
            Text = "Abbrechen: nichts speichern, Ihre Eingaben bleiben im Fenster.",
            AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandKlein, 0, 0),
        });
        Controls.Add(raster);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public void Waehlen(KonfliktWahl wahl)
    {
        Wahl = wahl;
        DialogResult = DialogResult.OK;
        Close();
    }

    // Zeigt den Hinweis (in Tests über Dialog.Zeigen ersetzbar) und gibt die Wahl zurück.
    public static KonfliktWahl Fragen(IWin32Window? besitzer, Konflikt k)
    {
        using var fenster = new KonfliktFenster(k);
        Dialog.Zeigen(fenster, besitzer);
        return fenster.Wahl;
    }
}
