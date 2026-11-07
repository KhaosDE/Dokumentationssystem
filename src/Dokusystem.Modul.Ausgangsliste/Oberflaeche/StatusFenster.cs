using System.Globalization;
using Dokusystem.Kern;

namespace Dokusystem.Modul.Ausgangsliste;

// Anwesenheit eines Bewohners (übernommen aus der Ausgangsliste): Ausgangsbeschränkung,
// Abwesenheit oder aufheben, jeweils mit Grund. Die Rechtsregel prüft der Speicher; hier ist der
// Knopf für die Ausgangsbeschränkung gesperrt, wenn sie nicht zulässig ist.
public sealed class StatusFenster : Form
{
    private readonly Datenbank db;
    private readonly Platz platz;
    private readonly DateTime heute;

    public ComboBox Grund { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 560 };
    public Button BeschraenkungKnopf { get; } = Stil.NebenKnopf("Ausgangsbeschränkung");
    public Button AbwesenheitKnopf { get; } = Stil.NebenKnopf("Abwesenheit");
    public Button AufhebenKnopf { get; } = Stil.NebenKnopf("Status aufheben");
    public List<Button> Schnellwahl { get; } = [];
    public Label Hinweis { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    // Zum Testen ersetzbar: Ja/Nein-Rückfrage vor dem Speichern.
    public Func<string, DialogResult> Rueckfrage { get; set; }

    public StatusFenster(Datenbank db, Platz platz)
    {
        this.db = db;
        this.platz = platz;
        heute = db.Jetzt().Date;
        Rueckfrage = frage => MessageBox.Show(this, frage, "Anwesenheit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        Text = $"Anwesenheit – {platz.Bewohnername} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = platz.Bewohnername, AutoSize = true, Font = Stil.KarteName, ForeColor = Stil.TextNormal });
        zeile(null, new Label
        {
            AutoSize = true, MaximumSize = new Size(560, 0),
            Text = platz.HatStatus ? $"Läuft gerade: {Verlauf.Status(platz.Status)} – {platz.StatusGrund}" : "Kein Status gesetzt – der Bewohner ist regulär anwesend.",
            Font = platz.HatStatus ? Stil.Fett : Stil.Standard, ForeColor = platz.HatStatus ? Stil.TextNormal : Stil.TextLeise,
        });
        zeile(null, Grund);
        var alle = Stil.NebenKnopf("Alle Gründe");
        alle.Click += (_, _) => GruendeLaden(true, heute);
        Schnellwahl.Add(alle);
        for (int wochen = 1; wochen <= 4; wochen++)
        {
            int tage = wochen * 7;
            var k = Stil.NebenKnopf(wochen == 1 ? "bis +1 Woche" : $"bis +{wochen} Wochen");
            k.Click += (_, _) => GruendeLaden(false, heute.AddDays(tage));
            Schnellwahl.Add(k);
        }
        foreach (var k in Schnellwahl) k.AutoSize = true;
        zeile(null, Formular.Reihe([.. Schnellwahl]));

        BeschraenkungKnopf.Width = 220;
        AbwesenheitKnopf.Width = 150;
        AufhebenKnopf.Width = 150;
        BeschraenkungKnopf.Click += (_, _) => Setzen(AnwesenheitsStatus.Ausgangsbeschraenkung);
        AbwesenheitKnopf.Click += (_, _) => Setzen(AnwesenheitsStatus.Abwesenheit);
        AufhebenKnopf.Click += (_, _) => Aufheben();
        BeschraenkungKnopf.Enabled = !platz.HatStatus && platz.AusgangsbeschraenkungErlaubt;
        AbwesenheitKnopf.Enabled = !platz.HatStatus;
        AufhebenKnopf.Enabled = platz.HatStatus;
        Grund.Enabled = !platz.HatStatus;
        if (!platz.AusgangsbeschraenkungErlaubt && !platz.HatStatus)
        {
            Hinweis.Text = platz.Unterbringung == Unterbringung.NichtErfasst ? Platz.HinweisNichtErfasst : Platz.HinweisNurMitBeschluss;
            Hinweis.Visible = true;
        }
        zeile(null, Formular.Reihe(BeschraenkungKnopf, AbwesenheitKnopf, AufhebenKnopf));
        zeile(null, Hinweis);
        zeile(null, Fehler);
        var schliessen = Stil.NebenKnopf("Abbrechen");
        schliessen.DialogResult = DialogResult.Cancel;
        schliessen.Anchor = AnchorStyles.Right;
        schliessen.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        CancelButton = schliessen;
        zeile(null, schliessen);
        Controls.Add(raster);
        GruendeLaden(true, heute);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Vorschläge für den Grund; mit „bis +n Wochen“ nur die mit Enddatum.
    public void GruendeLaden(bool alleZeigen, DateTime bis)
    {
        string h = heute.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        string b = bis.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        Grund.Items.Clear();
        if (alleZeigen)
            Grund.Items.AddRange([
                $"Neuaufnahme seit ({h})",
                $"Im Klinikum seit ({h})",
                $"Im Krankenhaus seit ({h})",
                $"Zurück aus Klinik, seit ({h}) vorerst Ausgangsbeschränkung",
                $"Medikamente verweigert am ({h})",
                "Ausgangsbeschränkung bis ",
                "Ausgangsbeschränkung bis Gespräch mit Bezugsperson (siehe Bericht)",
                "Ausgangsbeschränkung bis Rücksprache mit Arzt/Ärztin (siehe Bericht)",
                "Heimfahrt bis ",
                $"Probewohnen seit ({h})",
            ]);
        Grund.Items.AddRange([
            $"Auf der Flucht seit ({h})",
            $"Ausgangsbeschränkung wegen Selbstverletzung bis {b}",
            $"Ausgangsbeschränkung wegen Alkoholmissbrauch bis {b}",
            $"Ausgangsbeschränkung wegen Drogenmissbrauch bis {b}",
        ]);
    }

    public bool Setzen(AnwesenheitsStatus status)
    {
        string grund = Grund.Text.Trim();
        string name = status == AnwesenheitsStatus.Abwesenheit ? "Abwesenheit" : "Ausgangsbeschränkung";
        if (grund.Length > 0 && Rueckfrage($"{name} setzen für:\n{platz.Bewohnername}\n\nGrund: {grund}") != DialogResult.Yes) return false;
        return Speichern(status, grund);
    }

    public bool Aufheben()
    {
        bool neuaufnahme = platz.Status == AnwesenheitsStatus.Ausgangsbeschraenkung &&
                           platz.StatusGrund.StartsWith("Neuaufnahme", StringComparison.OrdinalIgnoreCase);
        string frage = neuaufnahme
            ? $"Neuaufnahme-Status aufheben für:\n{platz.Bewohnername}\n\nDer Bewohner gilt danach als regulär eingelebt."
            : $"Status aufheben für:\n{platz.Bewohnername}\n\nDie Ausgangs-Einträge von vorher sind danach wieder sichtbar.";
        if (Rueckfrage(frage) != DialogResult.Yes) return false;
        return Speichern(AnwesenheitsStatus.Keiner, "");
    }

    private bool Speichern(AnwesenheitsStatus status, string grund)
    {
        try
        {
            db.StatusSetzen(platz.BewohnerId, status, grund, Einstellungen.Laden().Kuerzel);
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
