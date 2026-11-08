using System.Globalization;
using Dokusystem.Kern;

namespace Dokusystem;

// Statusmeldung zu einem Bewohner setzen oder aufheben (Mehrplatzbetrieb): kurzer Text, gilt bis,
// dazu automatisch Bereich, Kürzel und Zeit. Sichtbar auf Karte und Akte in allen Bereichen.
// Eine neue Meldung ersetzt die alte; „Aufheben“ ist ein eigener Eintrag. Der Verlauf bleibt.
public sealed class StatusmeldungFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;

    public TextBox Inhalt { get; } = new() { Width = 380, MaxLength = StatusmeldungRegeln.Laenge, PlaceholderText = "z. B. beim Arzttermin, zurück gegen 11 Uhr" };
    public TextBox BisDatum { get; } = new() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox BisUhrzeit { get; } = new() { Width = 60, PlaceholderText = "SS:MM" };
    public Label Aktuell { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), UseMnemonic = false };
    public TextBox Verlauf { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 520, Height = 110, BackColor = Stil.Flaeche };
    public Label Fehler { get; } = new();
    public Button SetzenKnopf { get; } = Stil.HauptKnopf("Status setzen");
    public Button AufhebenKnopf { get; } = Stil.NebenKnopf("Aufheben");

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public StatusmeldungFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        var werte = Einstellungen.Laden();
        Text = $"Statusmeldung – {bewohner.Name} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label
        {
            Text = "Sichtbar auf Karte und Akte in allen Bereichen, bis die Zeit abläuft oder eine neue Meldung sie ersetzt. " +
                   $"Bereich und Kürzel kommen von diesem Arbeitsplatz: {werte.Bereich}, {(werte.Kuerzel.Length > 0 ? werte.Kuerzel : "kein Kürzel")}.",
            AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextLeise,
        });
        zeile("Gilt jetzt", Aktuell);
        zeile("Neue Meldung", Inhalt);
        var schnell = new[] { 1, 2, 4 }.Select(stunden =>
        {
            var k = Stil.NebenKnopf($"+{stunden} Std.");
            k.AutoSize = true;
            k.Click += (_, _) => BisSetzen(Jetzt().AddHours(stunden));
            return (Control)k;
        }).ToArray();
        zeile("Gilt bis", Formular.Reihe([BisDatum, BisUhrzeit, .. schnell]));
        Fehler.AutoSize = true;
        Fehler.MaximumSize = new Size(560, 0);
        Fehler.ForeColor = Stil.Beschluss.Text;
        Fehler.BackColor = Stil.Beschluss.Flaeche;
        Fehler.Padding = new Padding(Stil.AbstandKlein);
        Fehler.Visible = false;
        zeile(null, Fehler);
        zeile(null, new Label { Text = "VERLAUF", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandGross, 0, 0) });
        zeile(null, Verlauf);

        var abbrechen = Stil.NebenKnopf("Schließen");
        abbrechen.DialogResult = DialogResult.Cancel;
        SetzenKnopf.Click += (_, _) => Setzen();
        AufhebenKnopf.Click += (_, _) => Aufheben();
        foreach (var b in new[] { SetzenKnopf, AufhebenKnopf, abbrechen }) { b.AutoSize = true; b.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0); }
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([SetzenKnopf, AufhebenKnopf, abbrechen]);
        zeile(null, knoepfe);
        Controls.Add(raster);
        CancelButton = abbrechen;
        AcceptButton = SetzenKnopf;
        BisSetzen(Jetzt().AddHours(2));
        Anzeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    // Auf die nächste Viertelstunde aufrunden.
    private void BisSetzen(DateTime bis)
    {
        var gerundet = new DateTime(bis.Year, bis.Month, bis.Day, bis.Hour, 0, 0).AddMinutes((bis.Minute + 14) / 15 * 15);
        BisDatum.Text = Datum.Text(DateOnly.FromDateTime(gerundet));
        BisUhrzeit.Text = gerundet.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private void Anzeigen()
    {
        var jetzt = Jetzt();
        var geltend = speicher.Statusmeldung(bewohner.Id!.Value, jetzt);
        Aktuell.Text = geltend is null ? "keine Statusmeldung" : StatusmeldungRegeln.Langtext(geltend, jetzt);
        Aktuell.Font = geltend is null ? Stil.Standard : Stil.Fett;
        AufhebenKnopf.Enabled = geltend is not null;
        var verlauf = StatusmeldungRegeln.Verlauf(speicher.Statusmeldungen(bewohner.Id!.Value));
        Verlauf.Text = verlauf.Count == 0 ? "Noch keine Statusmeldungen." : string.Join(Environment.NewLine, verlauf);
    }

    private void FehlerZeigen(IEnumerable<string> fehler)
    {
        Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", fehler);
        Fehler.Visible = true;
    }

    public bool Setzen()
    {
        var werte = Einstellungen.Laden();
        try
        {
            var (text, bis) = StatusmeldungRegeln.AusEingabe(Inhalt.Text, BisDatum.Text, BisUhrzeit.Text, werte.Kuerzel, Jetzt());
            speicher.StatusmeldungSetzen(bewohner.Id!.Value, text, bis, werte.Kuerzel, werte.Bereich);
        }
        catch (Eingabefehler e)
        {
            FehlerZeigen(e.Fehler);
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    public bool Aufheben()
    {
        var werte = Einstellungen.Laden();
        try
        {
            speicher.StatusmeldungAufheben(bewohner.Id!.Value, werte.Kuerzel, werte.Bereich);
        }
        catch (Eingabefehler e)
        {
            FehlerZeigen(e.Fehler);
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
