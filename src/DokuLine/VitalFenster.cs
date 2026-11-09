using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// „Vitalwerte erfassen …“: Datum und Uhrzeit vorbelegt, mehrere Werte in einem Schritt, Bemerkung; das Kürzel kommt von
// selbst. Mit einem Wert: Korrektur – nur dieser Wert, dazu Pflicht „Grund“.
public sealed class VitalwertFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly Vitalwert? alt;

    public TextBox Datum_ { get; } = new DatumFeld() { Width = 100 };
    public TextBox Uhrzeit { get; } = new UhrzeitFeld() { Width = 70 };
    public Dictionary<string, TextBox> Felder { get; } = [];
    public TextBox Diastolisch { get; } = new() { Width = 60, PlaceholderText = "dia" };
    public TextBox Bemerkung { get; } = new() { Width = 320, PlaceholderText = "z. B. nüchtern, nach dem Essen" };
    public TextBox Grund { get; } = new() { Width = 320, PlaceholderText = "Pflicht bei einer Korrektur" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VitalwertFenster(Speicher speicher, Bewohner bewohner, Vitalwert? alt)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        this.alt = alt;
        Fenster.Grundform(this, alt is null ? $"Vitalwerte erfassen: {bewohner.Name}" : $"Vitalwert korrigieren: {bewohner.Name}");
        var r = speicher.Richtwerte();
        var jetzt = alt?.Zeit ?? Jetzt();
        Datum_.Text = Kern.Datum.Text(DateOnly.FromDateTime(jetzt));
        Uhrzeit.Text = jetzt.ToString("HH:mm", CultureInfo.InvariantCulture);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Datum, Uhrzeit", Formular.Reihe(Datum_, Uhrzeit));
        Datum_.Enabled = Uhrzeit.Enabled = alt is null;
        foreach (var art in alt is null ? speicher.Vitalarten() : [alt.Art])
        {
            var feld = new TextBox { Width = 80, AccessibleName = art };
            Felder[art] = feld;
            var einheit = new Label { Text = VitalRegeln.Einheit(art, r), AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
            if (art == VitalRegeln.Blutdruck)
            {
                feld.PlaceholderText = "sys";
                Diastolisch.AccessibleName = "Blutdruck diastolisch";
                zeile(art, Formular.Reihe(feld, new Label { Text = "/", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, Diastolisch, einheit));
            }
            else zeile(art, Formular.Reihe(feld, einheit));
        }
        if (alt is not null)
        {
            Felder[alt.Art].Text = VitalRegeln.Zahl(VitalRegeln.Anzeige(alt.Art, alt.Wert, r));
            if (alt.Wert2 is double d) Diastolisch.Text = VitalRegeln.Zahl(d);
            Bemerkung.Text = alt.Bemerkung;
        }
        zeile("Bemerkung", Bemerkung);
        if (alt is not null) zeile("Grund", Grund);
        zeile(null, Fenster.Hinweis(alt is null ? "Leere Felder werden nicht gespeichert. Das Kürzel kommt von selbst." :
            "Der bisherige Wert bleibt gespeichert, zählt aber nicht mehr; die Korrektur steht mit Grund in der Tabelle."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, alt is null ? "Speichern" : "Korrigieren", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public List<(string Art, double Wert, double? Wert2)> Werte()
    {
        var liste = new List<(string, double, double?)>();
        foreach (var (art, feld) in Felder)
        {
            if (VitalRegeln.Lesen(feld.Text, art) is not double w) continue;
            double? w2 = art == VitalRegeln.Blutdruck ? VitalRegeln.Lesen(Diastolisch.Text, "Blutdruck diastolisch") : null;
            liste.Add((art, w, w2));
        }
        return liste;
    }

    public bool Speichern()
    {
        try
        {
            if (alt is null)
            {
                DateTime zeit;
                try { zeit = Kern.Datum.Lesen(Datum_.Text, "Datum").ToDateTime(TimeOnly.ParseExact(Uhrzeit.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture)); }
                catch (FormatException) { throw new Eingabefehler(["Datum bitte als TT.MM.JJJJ, Uhrzeit als SS:MM."]); }
                speicher.VitalwerteErfassen(bewohner.Id!.Value, zeit, Werte(), Bemerkung.Text, Recht.Kuerzel, Jetzt());
            }
            else
            {
                var w = Werte().SingleOrDefault();
                if (w.Art is null) throw new Eingabefehler(["Bitte den richtigen Wert eintragen."]);
                speicher.VitalwertKorrigieren(alt.Id, w.Wert, w.Wert2, Bemerkung.Text, Grund.Text, Recht.Kuerzel, Jetzt());
            }
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Grenzen nur nach ärztlicher Anordnung (anordnender Arzt und Datum Pflicht) und Messaufträge je Bewohner.
public sealed class VitalGrenzenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;

    public ComboBox Art { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    public TextBox Von { get; } = new() { Width = 70, PlaceholderText = "von" };
    public TextBox Bis { get; } = new() { Width = 70, PlaceholderText = "bis" };
    public TextBox Von2 { get; } = new() { Width = 70, PlaceholderText = "dia von" };
    public TextBox Bis2 { get; } = new() { Width = 70, PlaceholderText = "dia bis" };
    public ComboBox Arzt { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    public TextBox AngeordnetAm { get; } = new DatumFeld() { Width = 100 };
    public Label Bisher { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.TextMittel, UseMnemonic = false };
    public ListBox Auftraege { get; } = new() { Width = 520, Height = 90, IntegralHeight = false };
    public ComboBox AuftragArt { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    public ComboBox Rhythmus { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    public ComboBox Wann { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    public ComboBox Schicht_ { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    private List<Messauftrag> auftraege = [];
    private readonly List<long?> arztIds = [];
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VitalGrenzenFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Grenzen und Messaufträge: {bewohner.Name}");
        var arten = speicher.Vitalarten();
        Art.Items.AddRange([.. arten]);
        AuftragArt.Items.AddRange([.. arten]);
        if (arten.Count > 0) Art.SelectedIndex = AuftragArt.SelectedIndex = 0;
        Arzt.Items.Add("(bitte wählen)");
        arztIds.Add(null);
        foreach (var p in speicher.Personen(PersonGruppe.Arzt, nurAktive: true)) { Arzt.Items.Add(p.Anzeige); arztIds.Add(p.Id); }
        Arzt.SelectedIndex = 0;
        AngeordnetAm.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        Rhythmus.Items.AddRange(["täglich", "wöchentlich", "monatlich"]);
        Rhythmus.SelectedIndex = 1;
        Schicht_.Items.AddRange([.. Enum.GetValues<Schicht>().Select(BerichtRegeln.SchichtText)]);
        Schicht_.SelectedIndex = 0;
        Rhythmus.SelectedIndexChanged += (_, _) => TageFuellen();
        TageFuellen();
        Art.SelectedIndexChanged += (_, _) => GrenzeZeigen();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Grenzen nur nach ärztlicher Anordnung. Nur dann zeigt das Programm „außerhalb des angeordneten Bereichs“."));
        zeile("Wert", Art);
        zeile("Bereich", Formular.Reihe(Von, Bis, Von2, Bis2));
        zeile("Angeordnet von", Arzt);
        zeile("Angeordnet am", AngeordnetAm);
        zeile(null, Bisher);
        zeile(null, Formular.Reihe(Knopf("Anordnen", () => Anordnen()), Knopf("Anordnung aufheben", () => Aufheben())));
        zeile("Messaufträge", Auftraege);
        zeile("Neuer Auftrag", Formular.Reihe(AuftragArt, Rhythmus, Wann, Schicht_));
        zeile(null, Formular.Reihe(Knopf("Auftrag anlegen", () => AuftragAnlegen()), Knopf("Auftrag beenden", () => AuftragBeenden())));
        zeile(null, Fehler);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.OK;
        CancelButton = schliessen;
        zeile(null, schliessen);
        Controls.Add(raster);
        GrenzeZeigen();
        AuftraegeZeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private static Button Knopf(string text, Action klick)
    {
        var k = Stil.NebenKnopf(text);
        k.Click += (_, _) => klick();
        return k;
    }

    private void TageFuellen()
    {
        Wann.Items.Clear();
        if (Rhythmus.SelectedIndex == 1) Wann.Items.AddRange([.. Enumerable.Range(0, 7).Select(d => CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetDayName((DayOfWeek)d))]);
        else if (Rhythmus.SelectedIndex == 2) Wann.Items.AddRange([.. Enumerable.Range(1, 28).Select(d => $"am {d}.")]);
        else Wann.Items.Add("jeden Tag");
        Wann.SelectedIndex = Rhythmus.SelectedIndex == 1 ? 1 : 0;
    }

    private void GrenzeZeigen()
    {
        bool blutdruck = Art.SelectedItem as string == VitalRegeln.Blutdruck;
        Von2.Visible = Bis2.Visible = blutdruck;
        Von.PlaceholderText = blutdruck ? "sys von" : "von";
        Bis.PlaceholderText = blutdruck ? "sys bis" : "bis";
        var g = speicher.Grenzen(bewohner.Id!.Value).GetValueOrDefault(Art.SelectedItem as string ?? "");
        Bisher.Text = g is null ? "Bisher: keine Anordnung." : "Bisher: " + VitalRegeln.GrenzeText(g, AngabenAnsicht.PersonText(speicher), speicher.Richtwerte());
    }

    private bool Ausfuehren(Action aktion)
    {
        try { aktion(); Fehler.Visible = false; return true; }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
    }

    public bool Anordnen() => Ausfuehren(() =>
    {
        DateOnly am;
        try { am = Datum.Lesen(AngeordnetAm.Text, "Angeordnet am"); }
        catch (FormatException x) { throw new Eingabefehler([x.Message]); }
        if (Art.SelectedItem is not string art) throw new Eingabefehler(["Bitte einen Wert wählen."]);
        var r = speicher.Richtwerte();
        double? Wert(TextBox feld, string name) => VitalRegeln.Lesen(feld.Text, name) is double w ? VitalRegeln.Speicherwert(art, w, r) : null;
        var g = new VitalGrenze(0, bewohner.Id!.Value, art, Wert(Von, "von"), Wert(Bis, "bis"),
            Von2.Visible ? VitalRegeln.Lesen(Von2.Text, "diastolisch von") : null, Bis2.Visible ? VitalRegeln.Lesen(Bis2.Text, "diastolisch bis") : null,
            arztIds[Math.Max(0, Arzt.SelectedIndex)], am, Recht.Kuerzel, Jetzt());
        if (!g.Gilt) throw new Eingabefehler(["Bitte einen Bereich eintragen (oder „Anordnung aufheben“)."]);
        speicher.GrenzeSetzen(g, Recht.Kuerzel, Jetzt());
        GrenzeZeigen();
    });

    public bool Aufheben() => Ausfuehren(() =>
    {
        if (Art.SelectedItem is null) throw new Eingabefehler(["Bitte einen Wert wählen."]);
        speicher.GrenzeSetzen(new VitalGrenze(0, bewohner.Id!.Value, (string)Art.SelectedItem!, null, null, null, null, null,
            DateOnly.FromDateTime(Jetzt()), Recht.Kuerzel, Jetzt()), Recht.Kuerzel, Jetzt());
        GrenzeZeigen();
    });

    private void AuftraegeZeigen()
    {
        auftraege = speicher.Messauftraege(bewohner.Id!.Value);
        Auftraege.Items.Clear();
        Auftraege.Items.AddRange([.. auftraege.Select(m => $"{m.Art} {VitalRegeln.RhythmusText(m)}, {BerichtRegeln.SchichtText(m.Schicht)} (seit {Datum.Text(m.Ab)})")]);
    }

    public bool AuftragAnlegen() => Ausfuehren(() =>
    {
        if (AuftragArt.SelectedItem is null) throw new Eingabefehler(["Bitte einen Wert wählen."]);
        var rhythmus = (Messrhythmus)Rhythmus.SelectedIndex;
        int tag = rhythmus switch { Messrhythmus.Woechentlich => Wann.SelectedIndex, Messrhythmus.Monatlich => Wann.SelectedIndex + 1, _ => 0 };
        speicher.MessauftragAnlegen(new Messauftrag(0, bewohner.Id!.Value, (string)AuftragArt.SelectedItem!, rhythmus, tag, (Schicht)Schicht_.SelectedIndex,
            DateOnly.FromDateTime(Jetzt()), null, Recht.Kuerzel, Jetzt()), Recht.Kuerzel, Jetzt());
        AuftraegeZeigen();
    });

    public bool AuftragBeenden() => Ausfuehren(() =>
    {
        if (Auftraege.SelectedIndex < 0) throw new Eingabefehler(["Bitte zuerst einen Auftrag wählen."]);
        speicher.MessauftragBeenden(auftraege[Auftraege.SelectedIndex].Id, Recht.Kuerzel, DateOnly.FromDateTime(Jetzt()));
        AuftraegeZeigen();
    });
}

// Einstellungen: allgemeine Richtwerte (Lehrbuchwerte, nicht ärztlich geprüft) und die Einheit des Blutzuckers.
public sealed class RichtwerteFenster : Form
{
    private readonly Speicher speicher;
    public Dictionary<string, TextBox> Felder { get; } = [];
    public ComboBox Einheit { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    private static readonly (string Beschriftung, Func<VitalRichtwerte, double> Lesen, Func<VitalRichtwerte, double, VitalRichtwerte> Setzen)[] Werte =
    [
        ("Blutdruck systolisch ab", r => r.BlutdruckSysHoch, (r, w) => r with { BlutdruckSysHoch = w }),
        ("Blutdruck diastolisch ab", r => r.BlutdruckDiaHoch, (r, w) => r with { BlutdruckDiaHoch = w }),
        ("Blutdruck systolisch unter", r => r.BlutdruckSysTief, (r, w) => r with { BlutdruckSysTief = w }),
        ("Blutdruck diastolisch unter", r => r.BlutdruckDiaTief, (r, w) => r with { BlutdruckDiaTief = w }),
        ("Puls unter", r => r.PulsTief, (r, w) => r with { PulsTief = w }),
        ("Puls über", r => r.PulsHoch, (r, w) => r with { PulsHoch = w }),
        ("Temperatur unter", r => r.TemperaturTief, (r, w) => r with { TemperaturTief = w }),
        ("Temperatur erhöht ab", r => r.TemperaturErhoeht, (r, w) => r with { TemperaturErhoeht = w }),
        ("Fieber ab", r => r.TemperaturFieber, (r, w) => r with { TemperaturFieber = w }),
        ("Blutzucker unter (mg/dl)", r => r.BlutzuckerTief, (r, w) => r with { BlutzuckerTief = w }),
        ("Blutzucker nüchtern ab (mg/dl)", r => r.BlutzuckerNuechternHoch, (r, w) => r with { BlutzuckerNuechternHoch = w }),
        ("Blutzucker sonst ab (mg/dl)", r => r.BlutzuckerHoch, (r, w) => r with { BlutzuckerHoch = w }),
        ("Sauerstoffsättigung unter", r => r.SaettigungTief, (r, w) => r with { SaettigungTief = w }),
        ("BMI unter", r => r.BmiTief, (r, w) => r with { BmiTief = w }),
        ("BMI Übergewicht ab", r => r.BmiUebergewicht, (r, w) => r with { BmiUebergewicht = w }),
        ("BMI Adipositas ab", r => r.BmiAdipositas, (r, w) => r with { BmiAdipositas = w }),
    ];

    public RichtwerteFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Vitalwerte: allgemeine Richtwerte");
        var r = speicher.Richtwerte();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis(VitalRegeln.RichtwertVermerk));
        foreach (var (beschriftung, lesen, _) in Werte)
        {
            var feld = new TextBox { Width = 70, Text = VitalRegeln.Zahl(lesen(r)), AccessibleName = beschriftung };
            Felder[beschriftung] = feld;
            zeile(beschriftung, feld);
        }
        Einheit.Items.AddRange([VitalRegeln.MgDl, VitalRegeln.MmolL]);
        Einheit.SelectedItem = r.BlutzuckerEinheit;
        zeile("Einheit Blutzucker", Einheit);
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        Stil.Sperren(knoepfe.Controls.OfType<Button>().First(k => k.Text == "Speichern"), Recht.Grund(speicher, Funktionen.VitalGrenzen));
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            var r = new VitalRichtwerte { BlutzuckerEinheit = (string)Einheit.SelectedItem! };
            foreach (var (beschriftung, _, setzen) in Werte)
                r = setzen(r, VitalRegeln.Lesen(Felder[beschriftung].Text, beschriftung) ?? throw new Eingabefehler([$"{beschriftung}: bitte eine Zahl eintragen."]));
            speicher.RichtwerteSpeichern(r, Recht.Kuerzel);
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
