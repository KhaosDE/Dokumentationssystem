using System.Globalization;
using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Modul.Ausgangsliste;

// Der Küchendienst-Tausch (übernommen aus der Ausgangsliste): An genau einem Tag übernimmt ein
// anderes Zimmer den Dienst; sonst ändert sich nichts, auch nicht die Reihenfolge danach.
// Der Tag wird im Plan ausgewählt, damit steht das eingeteilte Zimmer schon fest; hier wählt man
// nur noch, wer einspringt – oder „kein Tausch“, um einen Tausch aufzuheben.
public sealed class TauschFenster : Form
{
    public const string KeinTausch = "— kein Tausch —";
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private readonly Datenbank db;

    public KuechendienstTag Plantag { get; }
    public ComboBox Zimmerauswahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Button UebernehmenKnopf { get; } = Stil.HauptKnopf("Übernehmen");
    public Button AbbrechenKnopf { get; } = Stil.NebenKnopf("Abbrechen");

    // Das Ergebnis nach dem Speichern: "" heißt „kein Tausch“.
    public string NeuerTausch { get; private set; } = "";

    // Zum Testen ersetzbar: Ja/Nein-Rückfrage vor dem Speichern.
    public Func<string, DialogResult> Rueckfrage { get; set; }

    public TauschFenster(Datenbank db, KuechendienstTag tag, IReadOnlyList<string> kandidaten)
    {
        this.db = db;
        Plantag = tag;
        NeuerTausch = tag.Tausch;
        Text = $"Küchendienst – Tausch ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Rueckfrage = text => MessageBox.Show(this, text, "Tausch bestätigen", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        Zimmerauswahl.Items.Add(KeinTausch);
        foreach (string zimmer in kandidaten) Zimmerauswahl.Items.Add(zimmer);
        // Ein bestehender Tausch bleibt wählbar, auch wenn das Zimmer sonst nicht mehr dran ist.
        if (tag.Tausch != "" && !Zimmerauswahl.Items.Contains(tag.Tausch)) Zimmerauswahl.Items.Add(tag.Tausch);
        int vorwahl = Zimmerauswahl.Items.IndexOf(tag.Tausch);
        Zimmerauswahl.SelectedIndex = vorwahl > 0 ? vorwahl : 0;

        var raster = Formular.Raster(out var zeile);
        zeile("Datum", Wert(tag.Datum.ToString("dddd, dd.MM.yyyy", De)));
        zeile("Eingeteilt", Wert("Zimmer " + tag.Zimmer));
        zeile("Übernimmt", Zimmerauswahl);
        zeile(null, new Label
        {
            Text = "Der Tausch gilt nur für diesen Tag. Die Reihenfolge danach bleibt gleich.",
            AutoSize = true, MaximumSize = new Size(420, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        zeile(null, Fehler);
        zeile(null, KnopfReihe());
        Controls.Add(raster);

        UebernehmenKnopf.Click += (_, _) => Speichern();
        AcceptButton = UebernehmenKnopf;
        CancelButton = AbbrechenKnopf;
        Stil.DpiSkalierungEinschalten(this);
    }

    private static Label Wert(string text) => new() { Text = text, AutoSize = true, Font = Stil.Fett };

    private FlowLayoutPanel KnopfReihe()
    {
        AbbrechenKnopf.DialogResult = DialogResult.Cancel;
        UebernehmenKnopf.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var reihe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        reihe.Controls.AddRange([UebernehmenKnopf, AbbrechenKnopf]);
        return reihe;
    }

    public string Gewaehlt => Zimmerauswahl.SelectedIndex > 0 ? (string)Zimmerauswahl.SelectedItem! : "";

    // Speichert den Tausch nach einer Rückfrage. false: Fehler (steht im Fenster) oder die
    // Rückfrage wurde verneint. Ist nichts geändert, wird nichts geschrieben.
    public bool Speichern()
    {
        FehlerZeigen("");
        string gewaehlt = Gewaehlt;
        if (!Kuechendienst.TauschMoeglich(Plantag.Datum, db.Jetzt()))
            return FehlerZeigen("Heute und vergangene Tage stehen fest und lassen sich nicht mehr ändern. Ein Tausch ist ab morgen möglich.");

        if (gewaehlt != Plantag.Tausch)
        {
            string datum = Plantag.Datum.ToString("dd.MM.yyyy", De);
            string frage = gewaehlt == ""
                ? $"Tausch aufheben?\n\nDatum: {datum}\nDann ist wieder Zimmer {Plantag.Zimmer} dran."
                : $"Tausch bestätigen:\n\nZimmer {Plantag.Zimmer} → Zimmer {gewaehlt}\nDatum: {datum}\n\nFortfahren?";
            if (Rueckfrage(frage) != DialogResult.Yes) return false;
            try
            {
                db.KuechendienstTauschSetzen(Plantag.Datum, gewaehlt);
            }
            catch (Exception e) when (e is SqliteException or SpeicherFehler)
            {
                return FehlerZeigen("Der Tausch konnte nicht gespeichert werden: " + e.Message);
            }
        }

        NeuerTausch = gewaehlt;
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    private bool FehlerZeigen(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = text != "";
        return false;
    }
}
