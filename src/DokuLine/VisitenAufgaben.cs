using DokuLine.Kern;

namespace DokuLine;

// Einblendung gleich nach dem Speichern einer Visite: je Aufgabe ein Knopf („Medikament anlegen …“,
// „Im Medikamentenplan eintragen …“, „Termin eintragen …“). „Später“ schließt nur die Einblendung;
// die Aufgaben stehen weiter unter „Aufgaben“.
public sealed class VisitenAufgabenFenster : Form
{
    public List<Aufgabe> Aufgaben { get; }
    public List<Button> Knoepfe { get; } = [];
    public Button SpaeterKnopf { get; } = Stil.NebenKnopf("Später");
    public Aufgabe? Wunsch { get; private set; }

    public VisitenAufgabenFenster(Speicher speicher, IReadOnlyList<Aufgabe> aufgaben)
    {
        Aufgaben = [.. aufgaben];
        Fenster.Grundform(this, "Aufgaben aus der Visite");
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Aus der Visite sind diese Aufgaben entstanden. Sie stehen auch unter „Aufgaben“, bis sie erledigt sind; " +
                                    "die Übergabe nennt die Visite. „Später“ schließt nur diese Einblendung. Die Medikation ändert das Programm nicht selbst."));
        for (int i = 0; i < Aufgaben.Count; i++)
        {
            var a = Aufgaben[i];
            var info = speicher.VisitenAufgabe(a);
            var flaeche = new FlowLayoutPanel
            {
                AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Stil.Flaeche,
                Padding = new Padding(Stil.AbstandMittel), Margin = new Padding(0, Stil.AbstandKlein, 0, 0),
            };
            flaeche.Controls.Add(new Label { Text = a.Text, AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Fett, BackColor = Stil.Flaeche, UseMnemonic = false });
            flaeche.Controls.Add(new Label
            {
                Text = a.BewohnerId is long id ? namen.GetValueOrDefault(id) ?? "" : "", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise,
                BackColor = Stil.Flaeche, UseMnemonic = false, Margin = new Padding(0, 0, 0, Stil.AbstandKlein),
            });
            var knopf = Stil.NebenKnopf(info is { } x ? VisitenAblauf.KnopfText(x.Aktion) : "Öffnen …");
            Stil.Sperren(knopf, info is { } y ? VisitenAblauf.Sperrgrund(speicher, y.Aktion) : null);
            int nummer = i;
            knopf.Click += (_, _) => Waehlen(nummer);
            Knoepfe.Add(knopf);
            flaeche.Controls.Add(knopf);
            zeile(null, flaeche);
        }
        SpaeterKnopf.DialogResult = DialogResult.Cancel;
        SpaeterKnopf.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        zeile(null, SpaeterKnopf);
        CancelButton = SpaeterKnopf;
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Wie der Knopf; geht nicht, wenn das Recht fehlt.
    public bool Waehlen(int i)
    {
        if (Stil.Sperrgrund(Knoepfe[i]) is not null) return false;
        Wunsch = Aufgaben[i];
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Facharzttermin vereinbart (Aufgabe erledigen): Datum, Uhrzeit, Ort; der Termin kommt in den
// Kalender (Bereich) beim Bewohner, auf Wunsch eine Fahrt mit Fahrzeug. Danach ist die Aufgabe erledigt.
public sealed class FacharztTerminFenster : Form
{
    private readonly Speicher speicher;
    private readonly Aufgabe aufgabe;
    private readonly VisiteEintrag eintrag;
    private string? gewarntFuer;

    public TextBox DatumFeld { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox Uhrzeit { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "10:00" };
    public ArztWahl Arzt { get; }   // aus dem Verzeichnis, füllt den Ort
    public TextBox Ort { get; } = new() { Width = 320, PlaceholderText = "z. B. Praxis Dr. Esche (ausgedacht)" };
    public CheckBox MitFahrt { get; } = new() { Text = "Fahrt mit Fahrzeug eintragen", AutoSize = true };
    public ComboBox Fahrzeug { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Enabled = false };
    public TextBox Los { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "09:30", Enabled = false };
    public TextBox Zurueck { get; } = new UhrzeitFeld() { Width = 70, PlaceholderText = "11:30", Enabled = false };
    public PersonWahl Fahrer { get; }
    public Label Warnung { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Button SpeichernKnopf { get; }
    public KalenderTermin? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public FacharztTerminFenster(Speicher speicher, Aufgabe aufgabe, VisiteEintrag eintrag)
    {
        this.speicher = speicher;
        this.aufgabe = aufgabe;
        this.eintrag = eintrag;
        Fenster.Grundform(this, "Facharzttermin eintragen");
        Fahrzeug.Items.AddRange([.. speicher.AktiveFahrzeuge()]);
        if (Fahrzeug.Items.Count > 0) Fahrzeug.SelectedIndex = 0;
        Fahrer = new PersonWahl(speicher) { Enabled = false };
        Arzt = new ArztWahl(speicher);
        Arzt.Gewechselt += a => { if (a is not null && a.Praxis.Length > 0) Ort.Text = a.Praxis; };
        MitFahrt.CheckedChanged += (_, _) => Fahrzeug.Enabled = Los.Enabled = Zurueck.Enabled = Fahrer.Enabled = MitFahrt.Checked;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label { Text = aufgabe.Text, AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Fett, UseMnemonic = false, Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        zeile("Datum, Uhrzeit", Formular.Reihe(DatumFeld, Uhrzeit));
        zeile("Arzt (freiwillig)", Arzt);
        zeile("Ort", Ort);
        zeile("", MitFahrt);
        zeile("Fahrzeug", Fahrzeug);
        zeile("los – zurück", Formular.Reihe(Los, Zurueck));
        zeile("Fahrer", Fahrer);
        zeile(null, Fenster.Hinweis("Der Termin kommt in den Kalender (Bereich) beim Bewohner. Mit Fahrt steht der Bewohner während der Fahrt " +
                                    "im Tagesplan als abwesend. Danach ist die Aufgabe erledigt."));
        zeile(null, Warnung);
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        SpeichernKnopf = knoepfe.Controls.OfType<Button>().First(b => b.Text == "Speichern");
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Wie im Fenster „Neue Fahrt“: Bei einer Doppelbuchung erst eine Warnung, beim zweiten Klick speichern.
    public bool Speichern()
    {
        try
        {
            var jetzt = Jetzt();
            string kuerzel = Recht.Kuerzel;
            DateOnly datum;
            try { datum = Kern.Datum.Lesen(DatumFeld.Text, "Datum"); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            Fahrt? fahrt = null;
            if (MitFahrt.Checked)
            {
                fahrt = FahrtRegeln.AusEingabe(new FahrtEingabe
                {
                    Fahrzeug = Fahrzeug.SelectedItem as string ?? "", Datum = DatumFeld.Text, Von = Los.Text, Bis = Zurueck.Text,
                    Zweck = FahrtZweck.Arzttermin, Fahrer = Fahrer.Kuerzel, Bewohner = [eintrag.BewohnerId], Ziel = Ort.Text, Kuerzel = kuerzel,
                }, speicher.AktiveFahrzeuge(), jetzt);
                var andere = speicher.FahrtUeberschneidungen(fahrt);
                string schluessel = $"{fahrt.Fahrzeug}|{fahrt.Von:O}|{fahrt.Bis:O}";
                if (andere.Count > 0 && gewarntFuer != schluessel)
                {
                    gewarntFuer = schluessel;
                    Warnung.Text = $"Achtung, Doppelbuchung: Der {fahrt.Fahrzeug} ist zu dieser Zeit schon eingeplant – " +
                                   string.Join("; ", andere.Select(FahrtRegeln.Zeile)) + ". Trotzdem speichern? Dann noch einmal „Trotzdem speichern“.";
                    Warnung.Visible = true;
                    SpeichernKnopf.Text = "Trotzdem speichern";
                    Fehler.Visible = false;
                    return false;
                }
            }
            Gespeichert = speicher.FacharztTerminEintragen(aufgabe, datum, Uhrzeit.Text, Ort.Text, kuerzel, jetzt, fahrt, Arzt.GewaehlterArzt);
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

// Was der Knopf einer Visiten-Aufgabe heißt und wann er gesperrt ist (Rechte).
public static class VisitenAblauf
{
    public static string KnopfText(VisitenAktion a) => a switch
    {
        VisitenAktion.MedikamentAnlegen => "Medikament anlegen …",
        VisitenAktion.MedikationAendern => "Im Medikamentenplan eintragen …",
        _ => "Termin eintragen …",
    };

    public static string? Sperrgrund(Speicher speicher, VisitenAktion a) => a switch
    {
        VisitenAktion.MedikamentAnlegen => Recht.Grund(speicher, Funktionen.MedikamentAnlegen) ?? Recht.Grund(speicher, Funktionen.MedikationBearbeiten),
        VisitenAktion.MedikationAendern => Recht.Grund(speicher, Funktionen.MedikationBearbeiten),
        _ => Recht.Kuerzel.Length == 0 ? "Bitte zuerst oben rechts anmelden." : null,
    };
}
