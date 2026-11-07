using Dokusystem.Kern;

namespace Dokusystem.Modul.Ausgangsliste;

// Die Ausgangszeiten eines Bewohners (übernommen aus der Ausgangsliste). Die Knöpfe arbeiten auf
// einer Arbeitskopie; erst „Übernehmen“ speichert (mit Eintrag im Verlauf), Abbrechen verwirft.
public sealed class AusgangFenster : Form
{
    private readonly Datenbank db;
    private readonly Platz platz;
    private string aktuell;

    public Label Anzeige { get; } = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Stil.TextNormal };
    public Label Summe { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.Akzent };
    public Button AlleinPlus { get; } = Stil.NebenKnopf("Allein  +1");
    public Button AlleinMinus { get; } = Stil.NebenKnopf("Allein  −1");
    public Button MbwPlus { get; } = Stil.NebenKnopf("Mit Bewohner  +1");
    public Button MbwMinus { get; } = Stil.NebenKnopf("Mit Bewohner  −1");
    public Button InfoKnopf { get; } = Stil.NebenKnopf("Info anfügen …");
    public Button PersonalKnopf { get; } = Stil.NebenKnopf("Personal-Begleitung");
    public Button OffenKnopf { get; } = Stil.NebenKnopf("Wie offen geführt");
    public Button LeerenKnopf { get; } = Stil.NebenKnopf("Zurücksetzen");
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    // Zum Testen ersetzbar: Ja/Nein-Rückfrage und die Eingabe der Info (null = abgebrochen).
    public Func<string, DialogResult> Rueckfrage { get; set; }
    public Func<string?> InfoAbfragen { get; set; }

    public string NeuerText => aktuell;

    public AusgangFenster(Datenbank db, Platz platz)
    {
        this.db = db;
        this.platz = platz;
        aktuell = platz.Ausgang;
        Rueckfrage = frage => MessageBox.Show(this, frage, "Ausgang", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        InfoAbfragen = InfoEingeben;

        Text = $"Ausgangszeit – {platz.Bewohnername} ({Stil.DemoHinweis})";
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
        zeile(null, Anzeige);
        zeile(null, Summe);
        Verbinden(AlleinPlus, () => aktuell = AusgangText.MarkerErhoehen(aktuell, "Allein"));
        Verbinden(AlleinMinus, () => aktuell = AusgangText.MarkerVerringern(aktuell, "Allein"));
        Verbinden(MbwPlus, () => aktuell = AusgangText.MarkerErhoehen(aktuell, "MBW"));
        Verbinden(MbwMinus, () => aktuell = AusgangText.MarkerVerringern(aktuell, "MBW"));
        Verbinden(InfoKnopf, () => { if (InfoAbfragen() is string info) aktuell = AusgangText.InfoSetzen(aktuell, info); });
        Verbinden(PersonalKnopf, () => aktuell = AusgangText.TextUmschalten(aktuell, AusgangText.TextPersonalBegleitung));
        Verbinden(OffenKnopf, () =>
        {
            if (Rueckfrage($"„Wie offen geführt“ setzen?\n\nDie täglichen Ausgangsstunden werden entfernt, stattdessen steht dort „{AusgangText.TextWieOffenGefuehrt}“.") == DialogResult.Yes)
                aktuell = AusgangText.TextWieOffenGefuehrt;
        });
        Verbinden(LeerenKnopf, () =>
        {
            if (Rueckfrage("Täglichen Ausgang zurücksetzen?\n\nAlle Einträge werden geleert.") == DialogResult.Yes) aktuell = "";
        });
        zeile(null, Formular.Reihe(AlleinPlus, AlleinMinus));
        zeile(null, Formular.Reihe(MbwPlus, MbwMinus));
        zeile(null, Formular.Reihe(InfoKnopf, PersonalKnopf));
        zeile(null, Formular.Reihe(OffenKnopf, LeerenKnopf));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Übernehmen", () => Speichern()));
        Controls.Add(raster);
        AnzeigeAktualisieren();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Verbinden(Button knopf, Action aktion)
    {
        knopf.Width = 190;
        knopf.Click += (_, _) => { aktion(); AnzeigeAktualisieren(); };
    }

    private void AnzeigeAktualisieren()
    {
        Anzeige.Text = aktuell == "" ? "– noch kein Eintrag –" : aktuell;
        Summe.Text = $"Summe: {new Platz { Ausgang = aktuell }.Stunden} Std";
    }

    public bool Speichern()
    {
        try
        {
            db.AusgangSetzen(platz.BewohnerId, aktuell, Einstellungen.Laden().Kuerzel);
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

    // Kleines Eingabefenster für die Info; leer + OK entfernt alle Infos.
    private string? InfoEingeben()
    {
        string? ergebnis = null;
        using var fenster = new Form
        {
            Text = $"Info anfügen – {platz.Bewohnername} ({Stil.DemoHinweis})", FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, StartPosition = FormStartPosition.CenterParent,
            BackColor = Stil.Fensterhintergrund, Font = Stil.Standard, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        var feld = new TextBox { Width = 360 };
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = "Information eingeben (leer lassen + OK = alle Infos entfernen):", AutoSize = true });
        zeile(null, feld);
        zeile(null, Formular.Knoepfe(fenster, "OK", () => { ergebnis = feld.Text; fenster.DialogResult = DialogResult.OK; }));
        fenster.Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(fenster);
        return Dialog.Zeigen(fenster, this) == DialogResult.OK ? ergebnis : null;
    }
}
