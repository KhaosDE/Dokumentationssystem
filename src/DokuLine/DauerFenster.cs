using DokuLine.Kern;

namespace DokuLine;

// Kleines Fenster: „Wie lange?“ (nächste Schicht, 3 Tage, 1 Woche, bis Datum, bis erledigt) –
// zum Verlängern, oder mit Textfeld für eine Notiz an den Wohnbereich.
public sealed class DauerFenster : Form
{
    private readonly Func<string, UebergabeWunsch, Uebergabe>? notizSpeichern;
    public ComboBox Dauer { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    public TextBox Bis { get; } = new DatumFeld() { Width = 110, PlaceholderText = "TT.MM.JJJJ", Enabled = false };
    public TextBox Inhalt { get; } = new() { Multiline = true, Width = 440, Height = 70 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public UebergabeWunsch? Wunsch { get; private set; }

    // notizSpeichern == null: nur die Dauer wählen (Verlängern). Sonst mit Textfeld (Notiz).
    public DauerFenster(string titel, string frage, Func<string, UebergabeWunsch, Uebergabe>? notizSpeichern)
    {
        this.notizSpeichern = notizSpeichern;
        Text = $"{titel} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        if (notizSpeichern is not null)
        {
            stapel.Controls.Add(new Label { Text = "Notiz für alle (betrifft keinen einzelnen Bewohner):", AutoSize = true, ForeColor = Stil.TextMittel });
            stapel.Controls.Add(Inhalt);
        }
        Dauer.Items.AddRange(UebergabeRegeln.DauerTexte);
        Dauer.SelectedIndex = notizSpeichern is null ? (int)UebergabeDauer.DreiTage : (int)UebergabeDauer.NaechsteSchicht;
        Dauer.SelectedIndexChanged += (_, _) => Bis.Enabled = Dauer.SelectedIndex == (int)UebergabeDauer.BisDatum;
        var zeile = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        zeile.Controls.Add(new Label { Text = frage, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) });
        Dauer.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, 0);
        Bis.Margin = new Padding(0, Stil.AbstandMini, 0, 0);
        zeile.Controls.AddRange([Dauer, Bis]);
        stapel.Controls.Add(zeile);
        stapel.Controls.Add(Fehler);
        var ok = Stil.HauptKnopf(notizSpeichern is null ? "Verlängern" : "Notiz speichern");
        ok.Click += (_, _) => Uebernehmen();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([ok, abbrechen]);
        stapel.Controls.Add(knoepfe);
        Controls.Add(stapel);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Uebernehmen()
    {
        try
        {
            var dauer = (UebergabeDauer)Dauer.SelectedIndex;
            DateOnly? bis = dauer == UebergabeDauer.BisDatum ? Datum.Lesen(Bis.Text, "Bis") : null;
            Wunsch = new UebergabeWunsch(dauer, bis);
            notizSpeichern?.Invoke(Inhalt.Text, Wunsch);
        }
        catch (Exception e) when (e is Eingabefehler or FormatException)
        {
            Fehler.Text = e is Eingabefehler f ? string.Join("\n", f.Fehler) : e.Message;
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
