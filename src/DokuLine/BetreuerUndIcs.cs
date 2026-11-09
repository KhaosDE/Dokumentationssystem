using System.Text;
using DokuLine.Kern;

namespace DokuLine;

// Bauplan Schritt 28: Betreuer bearbeiten (neue Fassung, Verlauf bleibt) und Termine aus einer
// .ics-Datei übernehmen (Vorschau, Zuordnung zu Bewohner und Kalender Bereich/Haus).

public sealed class BetreuerFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;

    public TextBox NameFeld { get; } = new() { Width = 320 };
    public TextBox Stelle { get; } = new() { Width = 320, PlaceholderText = "z. B. Betreuungsverein, Berufsbetreuer, Angehörige" };
    public TextBox Email { get; } = new() { Width = 320, PlaceholderText = "name@example.org (Demo)" };
    public TextBox Telefon { get; } = new() { Width = 200 };
    // 0.33.3: dritter Zustand neben „eingetragen“ und „nicht erfasst“, z. B. wenn eine Betreuung endet.
    public CheckBox Keine { get; } = new() { Text = "Keine gesetzliche Betreuung", AutoSize = true };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Betreuer? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public BetreuerFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Betreuer – {bewohner.Name}");
        var e = BetreuerRegeln.AlsEingabe(speicher.Betreuer(bewohner.Id!.Value));
        (NameFeld.Text, Stelle.Text, Email.Text, Telefon.Text, Keine.Checked) = (e.Name, e.Stelle, e.Email, e.Telefon, e.Keine);
        Keine.CheckedChanged += (_, _) => FelderSperren();
        FelderSperren();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Keine);
        zeile("Name", NameFeld);
        zeile("Stelle", Stelle);
        zeile("E-Mail", Email);
        zeile("Telefon", Telefon);
        zeile(null, Fenster.Hinweis("Nur ausgedachte Angaben; E-Mail-Adressen in der Demo bitte mit der Endung @example.org. " +
                                    "Die bisherige Fassung bleibt im Verlauf."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private void FelderSperren()
    {
        foreach (var feld in new[] { NameFeld, Stelle, Email, Telefon }) feld.Enabled = !Keine.Checked;
    }

    public bool Speichern()
    {
        try
        {
            var b = BetreuerRegeln.AusEingabe(new BetreuerEingabe
            { Name = NameFeld.Text, Stelle = Stelle.Text, Email = Email.Text, Telefon = Telefon.Text, Kuerzel = Recht.Kuerzel, Keine = Keine.Checked }, Jetzt());
            speicher.BetreuerSpeichern(bewohner.Id!.Value, b);
            Gespeichert = b;
        }
        catch (Eingabefehler x)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", x.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Termine aus einer .ics-Datei: Liste mit Häkchen (alle an), darunter die Zuordnung für alle
// gewählten – Art, Bewohner, Kalender Bereich oder Haus. Wiederholungen: nur der erste Termin.
public sealed class IcsImportFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> bewohner;

    public List<IcsTermin> Gelesen { get; }
    public CheckedListBox Liste { get; } = new() { Width = 560, Height = 150, CheckOnClick = true, IntegralHeight = false };
    public RadioButton[] Arten { get; } = Enum.GetValues<KalenderArt>()
        .Select(a => new RadioButton { Text = KalenderRegeln.ArtText(a) == "Termin" ? "Termin eines Bewohners" : KalenderRegeln.ArtText(a), AutoSize = true }).ToArray();
    public CheckedListBox BewohnerListe { get; } = new() { Width = 320, Height = 130, CheckOnClick = true, IntegralHeight = false };
    public RadioButton BereichWahl { get; } = new() { Text = "Bereich", AutoSize = true, Checked = true };
    public RadioButton HausWahl { get; } = new() { Text = "Haus", AutoSize = true };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public List<KalenderTermin> Angelegt { get; } = [];

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public IcsImportFenster(Speicher speicher, IReadOnlyList<IcsTermin> gelesen, string dateiname)
    {
        this.speicher = speicher;
        Gelesen = [.. gelesen];
        bewohner = speicher.AlleBewohner();
        Fenster.Grundform(this, "Termine aus .ics-Datei übernehmen");
        foreach (var t in Gelesen)
            Liste.Items.Add($"{t.ZeitText} · {t.Titel}" + (t.Ort.Length > 0 ? $" · {t.Ort}" : "") + (t.Wiederholung ? " (Wiederholung: nur der erste)" : ""), true);
        Liste.AccessibleName = "Gelesene Termine";
        BewohnerListe.Items.AddRange([.. bewohner.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})")]);
        Arten[(int)KalenderArt.Wohnbereich].Checked = true;
        foreach (var r in Arten) r.CheckedChanged += (_, _) => BewohnerListe.Enabled = Art != KalenderArt.Wohnbereich;
        BewohnerListe.Enabled = false;
        if (Recht.Grund(speicher, Funktionen.KalenderHaus) is string grund)
        {
            HausWahl.Enabled = false;
            HausWahl.Text = $"Haus ({grund.TrimEnd('.')})";
        }
        var arten = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(560, 0), Margin = new Padding(0) };
        arten.Controls.AddRange(Arten);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label { Text = $"Aus „{dateiname}“: {Gelesen.Count} Termin(e). Häkchen = übernehmen.", AutoSize = true, MaximumSize = new Size(560, 0), UseMnemonic = false });
        zeile(null, Liste);
        zeile("Art", arten);
        zeile("Bewohner", BewohnerListe);
        zeile("Kalender", Formular.Reihe(BereichWahl, HausWahl));
        zeile(null, Fenster.Hinweis("Die Termine werden wie neu angelegte gespeichert (mit Ihrem Kürzel); der Hinweis nennt die .ics-Datei. " +
                                    "Zeiten ohne Zeitzone gelten als Ortszeit."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Übernehmen", () => Uebernehmen()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public KalenderArt Art => (KalenderArt)Math.Max(0, Array.FindIndex(Arten, r => r.Checked));

    public void BewohnerAnkreuzen(long id) => BewohnerListe.SetItemChecked(bewohner.FindIndex(b => b.Id == id), true);

    // Erst alle prüfen, dann alle speichern – entweder alle gewählten oder keiner.
    public bool Uebernehmen()
    {
        try
        {
            var gewaehlt = Liste.CheckedIndices.Cast<int>().Select(i => Gelesen[i]).ToList();
            if (gewaehlt.Count == 0) throw new Eingabefehler(["Bitte mindestens einen Termin ankreuzen."]);
            var ids = Art == KalenderArt.Wohnbereich ? [] : BewohnerListe.CheckedIndices.Cast<int>().Select(i => bewohner[i].Id!.Value).ToList();
            var fassungen = new List<KalenderFassung>();
            var fehler = new List<string>();
            foreach (var t in gewaehlt)
            {
                try { fassungen.Add(KalenderRegeln.AusEingabe(Ics.AlsEingabe(t, Art, HausWahl.Checked, ids, Recht.Kuerzel), Jetzt())); }
                catch (Eingabefehler x) { fehler.AddRange(x.Fehler.Select(f => $"{t.Titel}: {f}")); }
            }
            if (fehler.Count > 0) throw new Eingabefehler([.. fehler.Distinct()]);
            Angelegt.AddRange(speicher.KalenderAnlegenAlle(fassungen));
        }
        catch (Eingabefehler x)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", x.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Datei-Dialoge für den Austausch (zum Testen ersetzbar).
public static class KalenderAustausch
{
    public static Func<IWin32Window?, string, string?> SpeichernUnter { get; set; } = (besitzer, vorschlag) =>
    {
        using var d = new SaveFileDialog { Filter = "Kalenderdatei (*.ics)|*.ics", FileName = vorschlag, AddExtension = true, DefaultExt = "ics" };
        return d.ShowDialog(besitzer) == DialogResult.OK ? d.FileName : null;
    };

    public static Func<IWin32Window?, string?> Oeffnen { get; set; } = besitzer =>
    {
        using var d = new OpenFileDialog { Filter = "Kalenderdatei (*.ics)|*.ics|Alle Dateien|*.*" };
        return d.ShowDialog(besitzer) == DialogResult.OK ? d.FileName : null;
    };

    public static void Schreiben(string pfad, string inhalt) => File.WriteAllText(pfad, inhalt, new UTF8Encoding(false));
}
