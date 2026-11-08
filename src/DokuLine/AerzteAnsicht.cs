using System.Diagnostics.CodeAnalysis;
using DokuLine.Kern;

namespace DokuLine;

// Ärzteverzeichnis (Bauplan Schritt 26b): Auswahl statt Freitext, wo ein Arzt eingetragen wird
// (Verordnung, Anordnung, Visite, Facharzttermin, Termin). Fehlt jemand: „Arzt anlegen …“ direkt
// daneben (Recht „Ärzteverzeichnis verwalten“). Ein Name, der nicht (mehr) im Verzeichnis steht –
// z. B. aus älteren Einträgen –, bleibt wählbar und ist so gekennzeichnet.
public sealed class ArztWahl : FlowLayoutPanel
{
    private readonly Speicher speicher;
    private readonly List<string> namen = [];

    public ComboBox Liste { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    public Button AnlegenKnopf { get; } = Stil.NebenKnopf("Arzt anlegen …");
    public event Action<Arzt?>? Gewechselt;

    public ArztWahl(Speicher speicher)
    {
        this.speicher = speicher;
        AutoSize = true;
        WrapContents = false;
        Margin = new Padding(0);
        Liste.AccessibleName = "Arzt";
        Liste.Margin = AnlegenKnopf.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        AnlegenKnopf.AutoSize = true;
        Stil.Sperren(AnlegenKnopf, Recht.Grund(speicher, Funktionen.Aerzte));
        AnlegenKnopf.Click += (_, _) => Anlegen();
        Liste.SelectedIndexChanged += (_, _) => Gewechselt?.Invoke(GewaehlterArzt);
        Controls.AddRange([Liste, AnlegenKnopf]);
        Fuellen();
    }

    private void Fuellen(string? waehlen = null)
    {
        string bisher = waehlen ?? Gewaehlt;
        namen.Clear();
        Liste.Items.Clear();
        foreach (var a in speicher.Aerzte(nurAktive: true))
        {
            namen.Add(a.Name);
            Liste.Items.Add(a.Anzeige);
        }
        if (bisher.Length > 0) Gewaehlt = bisher;
    }

    // Der gewählte Name (so wird er gespeichert); leer = keiner.
    public string Gewaehlt
    {
        get => Liste.SelectedIndex >= 0 ? namen[Liste.SelectedIndex] : "";
        set
        {
            string name = value.Trim();
            if (name.Length == 0) { Liste.SelectedIndex = -1; return; }
            int i = namen.FindIndex(n => n.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            if (i < 0)
            {
                namen.Add(name);
                Liste.Items.Add($"{name} (nicht im Verzeichnis)");
                i = namen.Count - 1;
            }
            Liste.SelectedIndex = i;
        }
    }

    // Wie ein Textfeld nutzbar: Text = gewählter Name.
    [AllowNull]
    public override string Text
    {
        get => Gewaehlt;
        set => Gewaehlt = value ?? "";
    }

    public Arzt? GewaehlterArzt => Gewaehlt.Length > 0 ? speicher.ArztNach(Gewaehlt) : null;

    public Arzt? Anlegen()
    {
        if (Recht.Grund(speicher, Funktionen.Aerzte) is not null) return null;
        using var fenster = new ArztFenster(speicher, null);
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK || fenster.Gespeichert is not Arzt neu) return null;
        Fuellen(neu.Name);
        return neu;
    }
}

// Arzt anlegen oder ändern (ein ganzer Datensatz: „Speichern“ und „Abbrechen“).
public sealed class ArztFenster : Form
{
    private readonly Speicher speicher;
    private readonly Arzt? bisher;

    public TextBox NameFeld { get; } = new() { Width = 320, PlaceholderText = "ausgedacht, z. B. Dr. Ulme" };
    public TextBox Fachrichtung { get; } = new() { Width = 320, PlaceholderText = "z. B. Allgemeinmedizin, Psychiatrie" };
    public TextBox Praxis { get; } = new() { Width = 320, PlaceholderText = "Praxis bzw. Ort" };
    public TextBox Telefon { get; } = new() { Width = 200 };
    public TextBox Email { get; } = new() { Width = 320, PlaceholderText = "name@example.org (Demo)" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Arzt? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public ArztFenster(Speicher speicher, Arzt? bisher)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        Fenster.Grundform(this, bisher is null ? "Arzt anlegen" : "Arzt ändern");
        if (bisher is not null)
            (NameFeld.Text, Fachrichtung.Text, Praxis.Text, Telefon.Text, Email.Text) = (bisher.Name, bisher.Fachrichtung, bisher.Praxis, bisher.Telefon, bisher.Email);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Name", NameFeld);
        zeile("Fachrichtung", Fachrichtung);
        zeile("Praxis / Ort", Praxis);
        zeile("Telefon", Telefon);
        zeile("E-Mail", Email);
        zeile(null, Fenster.Hinweis("Nur ausgedachte Angaben; E-Mail-Adressen in der Demo bitte mit der Endung @example.org."));
        zeile("Eingetragen von", Recht.KuerzelAnzeige());
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            var a = ArztRegeln.AusEingabe(new ArztEingabe { Name = NameFeld.Text, Fachrichtung = Fachrichtung.Text, Praxis = Praxis.Text, Telefon = Telefon.Text, Email = Email.Text },
                bisher?.Id ?? 0, bisher?.Aktiv ?? true);
            Gespeichert = speicher.ArztSpeichern(a, Recht.Kuerzel, Jetzt());
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

// Das Verzeichnis (Einstellungen → Verwaltung): Liste, „Neuer Arzt …“, „Ändern …“, „Inaktiv / wieder aktiv“.
// Gelöscht wird nicht – frühere Einträge behalten den Namen.
public sealed class AerzteFenster : Form
{
    private readonly Speicher speicher;
    private List<Arzt> aerzte = [];

    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 760, Height = 260, Font = Stil.Standard,
    };
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neuer Arzt …");
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern …");
    public Button UmschaltenKnopf { get; } = Stil.NebenKnopf("Inaktiv / wieder aktiv");
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public AerzteFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Ärzteverzeichnis");
        foreach (var (titel, breite) in new[] { ("Name", 200), ("Fachrichtung", 170), ("Praxis / Ort", 190), ("Telefon", 100), ("Status", 80) })
            Liste.Columns.Add(titel, breite);
        Liste.DoubleClick += (_, _) => Aendern();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Steht überall zur Auswahl, wo ein Arzt eingetragen wird. Inaktive stehen nicht mehr zur Wahl; frühere Einträge bleiben."));
        zeile(null, Liste);
        string? grund = Recht.Grund(speicher, Funktionen.Aerzte);
        foreach (var k in new[] { NeuKnopf, AendernKnopf, UmschaltenKnopf }) { k.AutoSize = true; Stil.Sperren(k, grund); }
        NeuKnopf.Click += (_, _) => Neu();
        AendernKnopf.Click += (_, _) => Aendern();
        UmschaltenKnopf.Click += (_, _) => Umschalten();
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        CancelButton = schliessen;
        zeile(null, Formular.Reihe(NeuKnopf, AendernKnopf, UmschaltenKnopf, schliessen));
        zeile(null, Fehler);
        Controls.Add(raster);
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        aerzte = speicher.Aerzte();
        Liste.Items.Clear();
        foreach (var a in aerzte)
            Liste.Items.Add(new ListViewItem([a.Name, a.Fachrichtung, a.Praxis, a.Telefon, a.Aktiv ? "aktiv" : "inaktiv"]));
    }

    public void Waehlen(int index) { Liste.SelectedIndices.Clear(); Liste.SelectedIndices.Add(index); }

    private Arzt? Gewaehlt => Liste.SelectedIndices.Count == 1 ? aerzte[Liste.SelectedIndices[0]] : null;

    public bool Neu()
    {
        if (Recht.Grund(speicher, Funktionen.Aerzte) is not null) return false;
        using var f = new ArztFenster(speicher, null);
        bool ok = Dialog.Zeigen(f, this) == DialogResult.OK;
        Fuellen();
        return ok;
    }

    public bool Aendern()
    {
        if (Recht.Grund(speicher, Funktionen.Aerzte) is not null || Gewaehlt is not Arzt a) return false;
        using var f = new ArztFenster(speicher, a);
        bool ok = Dialog.Zeigen(f, this) == DialogResult.OK;
        Fuellen();
        return ok;
    }

    public bool Umschalten()
    {
        if (Recht.Grund(speicher, Funktionen.Aerzte) is not null) return false;
        if (Gewaehlt is not Arzt a) { Fehler.Text = "Bitte zuerst einen Arzt wählen."; Fehler.Visible = true; return false; }
        try { speicher.ArztAktiv(a.Id, !a.Aktiv, Recht.Kuerzel, DateTime.Now); }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        Fehler.Visible = false;
        Fuellen();
        return true;
    }
}
