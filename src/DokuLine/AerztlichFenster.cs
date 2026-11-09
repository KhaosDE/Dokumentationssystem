using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Fenster „Ärztliche Informationen“: Diagnosen (nie gelöscht, „beendet am …“),
// Allergien und Unverträglichkeiten mit den drei Zuständen (Regel 5) und Art der Reaktion, Hinweise für
// den Notfall. Ansehen und Bearbeiten hängen an eigenen Rechten; ohne „bearbeiten“ ist alles nur zum Lesen.
public sealed class AerztlichFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private List<Diagnose> diagnosen = [];

    public ListView Diagnosen { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 860, Height = 140, Font = Stil.Standard,
    };
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Diagnose …");
    public Button BeendenKnopf { get; } = Stil.NebenKnopf("Beenden …");
    public Dreiwahl Allergie { get; } = new(["eingetragen", "keine_bekannt", "nicht_erfasst"],
                                            ["eingetragen:", "keine bekannt", "nicht erfasst"], "z. B. Haselnüsse, Latex");
    public DataGridView Reaktionen { get; } = new()
    {
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, Width = 860, Height = 84, Font = Stil.Standard,
        BackgroundColor = Stil.Flaeche, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };
    public Button AllergieKnopf { get; } = Stil.NebenKnopf("Allergien speichern");
    public TextBox Notfall { get; } = new() { Multiline = true, Width = 860, Height = 56, ScrollBars = ScrollBars.Vertical };
    public Button NotfallKnopf { get; } = Stil.NebenKnopf("Hinweise speichern");
    public Label NurAnsehen { get; } = new() { AutoSize = true, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Meldung { get; } = new() { AutoSize = true, MaximumSize = new Size(860, 0), Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public bool Geaendert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AerztlichFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Ärztliche Informationen – {bewohner.Name}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, NurAnsehen);
        zeile(null, Abschnitt("DIAGNOSEN"));
        foreach (var (titel, breite) in new[] { ("ICD-10", 70), ("Bezeichnung", 280), ("Art", 110), ("seit", 85), ("gestellt von", 150), ("Stand", 150) })
            Diagnosen.Columns.Add(titel, breite);
        Diagnosen.AccessibleName = "Diagnosen";
        zeile(null, Diagnosen);
        foreach (var k in new[] { NeuKnopf, BeendenKnopf, AllergieKnopf, NotfallKnopf }) k.AutoSize = true;
        NeuKnopf.Click += (_, _) => Neu();
        BeendenKnopf.Click += (_, _) => Beenden();
        zeile(null, Formular.Reihe(NeuKnopf, BeendenKnopf));
        zeile(null, Abschnitt("ALLERGIEN UND UNVERTRÄGLICHKEITEN"));
        Allergie.Setzen(BewohnerRegeln.Kuerzel(bewohner.Allergie), bewohner.Allergien);
        Allergie.Feld.Width = 520;   // mehrere Stoffe lesbar
        zeile(null, Allergie);
        Reaktionen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stoff", ReadOnly = true, FillWeight = 35 });
        Reaktionen.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Art der Reaktion (freiwillig)", FillWeight = 65 });
        Reaktionen.AccessibleName = "Art der Reaktion";
        Allergie.Feld.Leave += (_, _) => ReaktionenFuellen();
        foreach (var k in Allergie.Knoepfe) k.CheckedChanged += (_, _) => ReaktionenFuellen();
        zeile(null, Reaktionen);
        AllergieKnopf.Click += (_, _) => AllergienSpeichern();
        zeile(null, AllergieKnopf);
        zeile(null, Abschnitt("HINWEISE FÜR DEN NOTFALL"));
        Notfall.Text = speicher.Notfallhinweise(bewohner.Id!.Value);
        Notfall.AccessibleName = "Hinweise für den Notfall";
        zeile(null, Notfall);
        NotfallKnopf.Click += (_, _) => NotfallSpeichern();
        zeile(null, NotfallKnopf);
        zeile(null, Meldung);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        CancelButton = schliessen;
        zeile(null, schliessen);
        Controls.Add(raster);
        DiagnosenFuellen();
        ReaktionenFuellen(speicher.Reaktionen(bewohner.Id!.Value));
        RechteAnwenden();
        Stil.DpiSkalierungEinschalten(this);
    }

    private static Label Abschnitt(string text) => new() { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandMittel, 0, Stil.AbstandMini) };

    private string? Grund => Recht.Grund(speicher, Funktionen.AerztlicheBearbeiten);

    private void RechteAnwenden()
    {
        string? grund = Grund;
        NurAnsehen.Text = grund is null ? "" : $"Nur ansehen: {grund}";
        NurAnsehen.Visible = grund is not null;
        foreach (var k in new[] { NeuKnopf, BeendenKnopf, AllergieKnopf, NotfallKnopf }) Stil.Sperren(k, grund);
        Allergie.Enabled = grund is null;
        Reaktionen.ReadOnly = grund is not null;
        Notfall.ReadOnly = grund is not null;
    }

    private void DiagnosenFuellen()
    {
        diagnosen = speicher.Diagnosen(bewohner.Id!.Value);
        Diagnosen.Items.Clear();
        foreach (var d in diagnosen)
        {
            var item = new ListViewItem([d.Icd, d.Bezeichnung, DiagnoseRegeln.ArtText(d.Art), d.Seit is DateOnly s ? Datum.Text(s) : "",
                d.Arzt, d.Aktuell ? "aktuell" : $"beendet am {Datum.Text(d.Beendet)}"])
            { ToolTipText = d.Hinweis };
            if (!d.Aktuell) item.ForeColor = Stil.TextLeise;
            Diagnosen.Items.Add(item);
        }
    }

    // Je Stoff aus dem Allergie-Text eine Zeile; schon eingetragene Reaktionen bleiben stehen.
    private void ReaktionenFuellen(IReadOnlyDictionary<string, string>? vorher = null)
    {
        vorher ??= ReaktionenLesen();
        Reaktionen.Rows.Clear();
        if (Allergie.Zustand != "eingetragen") return;
        foreach (string stoff in DiagnoseRegeln.Stoffe(Allergie.Feld.Text))
            Reaktionen.Rows.Add(stoff, vorher.GetValueOrDefault(stoff, ""));
    }

    public Dictionary<string, string> ReaktionenLesen() =>
        Reaktionen.Rows.Cast<DataGridViewRow>().Where(r => r.Cells[0].Value is string)
                  .ToDictionary(r => (string)r.Cells[0].Value!, r => Convert.ToString(r.Cells[1].Value, CultureInfo.CurrentCulture) ?? "");

    public void DiagnoseWaehlen(int index) { Diagnosen.SelectedIndices.Clear(); Diagnosen.SelectedIndices.Add(index); }

    private bool Ausfuehren(Action aktion, string erfolg)
    {
        if (Grund is not null) return false;
        try { aktion(); }
        catch (Eingabefehler e)
        {
            Meldung.Text = "Bitte prüfen: " + string.Join(" ", e.Fehler);
            Meldung.ForeColor = Stil.Beschluss.Text;
            Meldung.BackColor = Stil.Beschluss.Flaeche;
            Meldung.Visible = true;
            return false;
        }
        Meldung.Text = erfolg;
        Meldung.ForeColor = Stil.Offen.Text;
        Meldung.BackColor = Stil.Offen.Flaeche;
        Meldung.Visible = true;
        Geaendert = true;
        return true;
    }

    public bool Neu()
    {
        if (Grund is not null) return false;
        using var f = new DiagnoseFenster(speicher, bewohner) { Jetzt = Jetzt };
        if (Dialog.Zeigen(f, this) != DialogResult.OK) return false;
        Geaendert = true;
        DiagnosenFuellen();
        return true;
    }

    public bool Beenden()
    {
        if (Grund is not null || Diagnosen.SelectedIndices.Count != 1) return false;
        var d = diagnosen[Diagnosen.SelectedIndices[0]];
        if (!d.Aktuell) return false;
        bool ok = false;
        using var f = new DatumFenster($"Diagnose beenden – {d.Anzeige}", "Beendet am", DateOnly.FromDateTime(Jetzt()),
            am => { speicher.DiagnoseBeenden(d.Id, am, Recht.Kuerzel, Jetzt()); ok = true; });
        Dialog.Zeigen(f, this);
        if (ok) { Geaendert = true; DiagnosenFuellen(); }
        return ok;
    }

    public bool AllergienSpeichern() => Ausfuehren(() =>
    {
        ReaktionenFuellen();   // Zeilen passend zum Allergie-Text, auch wenn das Feld noch nicht verlassen wurde
        var zustand = BewohnerRegeln.AllergieAus(Allergie.Zustand) ?? AllergieZustand.NichtErfasst;
        speicher.AllergienSpeichern(bewohner.Id!.Value, zustand, Allergie.Feld.Text, ReaktionenLesen());
    }, "Allergien gespeichert.");

    public bool NotfallSpeichern() => Ausfuehren(() => speicher.NotfallhinweiseSpeichern(bewohner.Id!.Value, Notfall.Text, Recht.Kuerzel, Jetzt()), "Hinweise gespeichert.");
}

// Neue Diagnose: ICD-10 (freiwillig), Bezeichnung, Haupt- oder Nebendiagnose, seit, gestellt von (Ärzteverzeichnis), Hinweis.
public sealed class DiagnoseFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;

    public TextBox Icd { get; } = new() { Width = 100, CharacterCasing = CharacterCasing.Upper, PlaceholderText = "z. B. F20.0" };
    public TextBox Bezeichnung { get; } = new() { Width = 380 };
    public RadioButton Haupt { get; } = new() { Text = "Hauptdiagnose", AutoSize = true, Checked = true };
    public RadioButton Neben { get; } = new() { Text = "Nebendiagnose", AutoSize = true };
    public TextBox Seit { get; } = new DatumFeld();
    public ArztWahl Arzt { get; }
    public TextBox Hinweis { get; } = new() { Width = 380 };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public DiagnoseFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Arzt = new ArztWahl(speicher);
        Fenster.Grundform(this, $"Neue Diagnose – {bewohner.Name}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("ICD-10 (freiwillig)", Icd);
        zeile("Bezeichnung", Bezeichnung);
        zeile("Art", Formular.Reihe(Haupt, Neben));
        zeile("seit", Seit);
        zeile("gestellt von", Arzt);
        zeile("Hinweis", Hinweis);
        zeile("Eingetragen von", Recht.KuerzelAnzeige());
        zeile(null, Fenster.Hinweis("Diagnosen werden nicht gelöscht oder geändert: Trifft eine nicht mehr zu, „Beenden …“ und neu eintragen."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            speicher.DiagnoseSpeichern(DiagnoseRegeln.AusEingabe(new DiagnoseEingabe
            {
                Icd = Icd.Text, Bezeichnung = Bezeichnung.Text, Seit = Seit.Text, Art = Neben.Checked ? DiagnoseArt.Neben : DiagnoseArt.Haupt,
                Arzt = Arzt.Gewaehlt, Hinweis = Hinweis.Text,
            }, bewohner.Id!.Value, Recht.Kuerzel, Jetzt()));
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
