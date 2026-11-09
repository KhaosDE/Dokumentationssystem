using DokuLine.Kern;

namespace DokuLine;

// Bezugspflege und Einzeltrainer eines Bewohners ändern. Ein Wechsel ist ein neuer
// Eintrag mit Datum; der Verlauf bleibt. Ab fünf Bezugsbewohnern nur ein Hinweis, keine Grenze.
public sealed class BezugFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<Mitarbeiter> aktive;

    public ComboBox Bezugspflege { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    public ComboBox Trainer { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    public ComboBox Art { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 180, MaxLength = 40 };
    public TextBox Ab { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public Label Hinweis { get; } = new() { AutoSize = true, MaximumSize = new Size(480, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(480, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public BezugFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        aktive = speicher.AlleMitarbeiter().Where(m => m.Aktiv).ToList();
        Text = $"Bezug – {bewohner.Name} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var bezug = speicher.Bezug(bewohner.Id!.Value);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        Bezugspflege.Items.Add("– keine –");
        Trainer.Items.Add("– keiner –");
        foreach (var m in aktive)
        {
            Bezugspflege.Items.Add($"{m.Anzeige} · {string.Join(", ", m.Bereiche)}");
            Trainer.Items.Add($"{m.Anzeige} · {string.Join(", ", m.Positionen)}");
        }
        Bezugspflege.SelectedIndex = Index(bezug.Bezugspflege?.Kuerzel);
        Trainer.SelectedIndex = Index(bezug.Einzeltrainer?.Kuerzel);
        Art.Items.AddRange([.. speicher.EinzeltrainerArten()]);
        Art.Text = bezug.Einzeltrainer?.Art ?? "";
        Bezugspflege.SelectedIndexChanged += (_, _) => HinweisZeigen();
        Trainer.SelectedIndexChanged += (_, _) => Art.Enabled = Trainer.SelectedIndex > 0;
        Art.Enabled = Trainer.SelectedIndex > 0;
        Ab.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));

        zeile("Bezugspflege", Bezugspflege);
        zeile(null, Hinweis);
        zeile("Einzeltrainer", Trainer);
        zeile("Art", Art);
        zeile(null, new Label { Text = "Art aus der Liste oder neu eintippen (z. B. weitere Gruppen) – sie kommt dann in die Liste.", AutoSize = true, MaximumSize = new Size(480, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise });
        zeile("Gilt ab", Ab);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        HinweisZeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private int Index(string? kuerzel) => kuerzel is null ? 0 : aktive.FindIndex(m => m.Kuerzel == kuerzel) + 1;
    private string Gewaehlt(ComboBox c) => c.SelectedIndex > 0 ? aktive[c.SelectedIndex - 1].Kuerzel : "";

    private void HinweisZeigen()
    {
        string k = Gewaehlt(Bezugspflege);
        bool schon = speicher.Bezug(bewohner.Id!.Value).Bezugspflege?.Kuerzel == k;
        Hinweis.Text = k.Length == 0 ? "" : BezugRegeln.Hinweis(k, speicher.Bezugsbewohner(k) + (schon ? 0 : 1));
        Hinweis.Visible = Hinweis.Text.Length > 0;
    }

    public bool Speichern()
    {
        try
        {
            var ab = Datum.Lesen(Ab.Text, "Gilt ab");
            string von = Einstellungen.Laden().Kuerzel;
            speicher.BezugspflegeSetzen(bewohner.Id!.Value, Gewaehlt(Bezugspflege), ab, von);
            speicher.EinzeltrainerSetzen(bewohner.Id!.Value, Gewaehlt(Trainer), Art.Text, ab, von);
        }
        catch (Exception e) when (e is Eingabefehler or FormatException)
        {
            Fehler.Text = e is Eingabefehler f ? "Bitte prüfen:\n• " + string.Join("\n• ", f.Fehler) : e.Message;
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
