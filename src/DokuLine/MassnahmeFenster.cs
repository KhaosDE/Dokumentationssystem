using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// „Neue Maßnahme …“ und „Ändern …“ (Bauplan 39): Titel (mit Vorlagen), Lebensbereich, Ziel, Art, Rhythmus, Dienst oder
// Uhrzeit, geplanter Zeitwert, gültig von–bis. Ändern speichert eine neue Fassung ab dem gewählten Datum.
public sealed class MassnahmeFenster : Form
{
    private readonly Speicher speicher;
    private readonly long bewohnerId;
    private readonly Massnahme? alt;
    private readonly List<Ziel> ziele;

    public ComboBox Titel { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 340, AccessibleName = "Titel" };
    public TextBox Beschreibung { get; } = new() { Width = 420, Height = 60, Multiline = true, AccessibleName = "Beschreibung" };
    public ComboBox Lebensbereich { get; } = Auswahl(420, "Lebensbereich");
    public ComboBox Ziel { get; } = Auswahl(420, "Ziel");
    public ComboBox Art { get; } = Auswahl(200, "Art");
    public ComboBox Rhythmus { get; } = Auswahl(160, "Rhythmus", "täglich", "an Wochentagen", "alle n Wochen", "monatlich", "bei Bedarf");
    public List<CheckBox> Wochentage { get; } = [];
    public NumericUpDown AlleWochen { get; } = new() { Minimum = 2, Maximum = 8, Value = 2, Width = 50, AccessibleName = "alle n Wochen" };
    public NumericUpDown MonatsTag { get; } = new() { Minimum = 1, Maximum = 28, Value = 1, Width = 50, AccessibleName = "Tag im Monat" };
    public ComboBox Schicht_ { get; } = Auswahl(140, "Dienst", "Frühdienst", "Spätdienst", "Nachtdienst");
    public TextBox Uhrzeit { get; } = new UhrzeitFeld { Width = 70, PlaceholderText = "frei", AccessibleName = "Uhrzeit" };
    public NumericUpDown Minuten { get; } = new() { Minimum = 0, Maximum = MassnahmenRegeln.MinutenHoechstens, Increment = 5, Value = 15, Width = 70, AccessibleName = "Zeitwert in Minuten" };
    public TextBox GiltAb { get; } = new DatumFeld { Width = 100, AccessibleName = "gültig ab" };
    public TextBox GiltBis { get; } = new DatumFeld { Width = 100, PlaceholderText = "ohne Ende", AccessibleName = "gültig bis" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // Zum Testen ersetzbar: „Ist das ärztlich verordnet?“ – true = trotzdem als Maßnahme speichern.
    internal Func<string, bool> TrotzMedikament { get; set; }

    public MassnahmeFenster(Speicher speicher, Bewohner bewohner, Massnahme? alt = null, string titel = "")
    {
        this.speicher = speicher;
        bewohnerId = bewohner.Id!.Value;
        this.alt = alt;
        Fenster.Grundform(this, alt is null ? $"Neue Maßnahme: {bewohner.Name}" : $"Maßnahme ändern: {bewohner.Name}");
        TrotzMedikament = text => MessageBox.Show(this, text + "\n\nTrotzdem als Maßnahme speichern?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        var heute = DateOnly.FromDateTime(Jetzt());
        Titel.Items.AddRange([.. speicher.Listeneintraege(Kern.Liste.Massnahmenvorlagen).Where(e => e.Aktiv).Select(e => e.Name)]);
        Lebensbereich.Items.AddRange([.. Enumerable.Range(1, 5).Select(n => $"{n} {BerichtRegeln.Lebensbereiche[n - 1]}")]);
        Art.Items.AddRange([.. speicher.Listeneintraege(Kern.Liste.Massnahmenarten).Where(e => e.Aktiv).Select(e => e.Name)]);
        var tage = Formular.Reihe();
        foreach (var t in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            var box = new CheckBox { Text = MassnahmenRegeln.Wochentag(t), Tag = t, AutoSize = true, AccessibleName = $"Wochentag {MassnahmenRegeln.Wochentag(t)}" };
            Wochentage.Add(box);
            tage.Controls.Add(box);
        }
        var f = alt?.Aktuell(heute);
        ziele = ZielRegeln.Waehlbar(speicher.Ziele(bewohnerId), f?.ZielId);
        Ziel.Items.Add(MassnahmenRegeln.OhneZiel);
        Ziel.Items.AddRange([.. ziele.Select(ZielRegeln.Text)]);
        Ziel.SelectedIndex = f?.ZielId is long zid ? ziele.FindIndex(z => z.Id == zid) + 1 : 0;
        Titel.Text = f?.Titel ?? titel;
        Beschreibung.Text = f?.Beschreibung ?? "";
        Lebensbereich.SelectedIndex = f is null ? -1 : f.Lebensbereich - 1;
        Art.SelectedItem = f?.Art;
        if (f?.Art is string a && Art.SelectedItem is null) { Art.Items.Add(a); Art.SelectedItem = a; }   // stillgelegte Art bleibt wählbar
        Rhythmus.SelectedIndex = (int)(f?.Rhythmus ?? MassnahmeRhythmus.Taeglich);
        foreach (var box in Wochentage) box.Checked = f?.Wochentage.Contains((DayOfWeek)box.Tag!) == true;
        AlleWochen.Value = Math.Clamp(f?.AlleWochen ?? 2, 2, 8);
        MonatsTag.Value = Math.Clamp(f?.MonatsTag ?? 1, 1, 28);
        Schicht_.SelectedIndex = (int)(f?.Schicht ?? SchichtTermin.Zu(Jetzt()).Schicht);
        Uhrzeit.Text = f?.Uhrzeit?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "";
        Minuten.Value = Math.Clamp(f?.Minuten ?? 15, 0, MassnahmenRegeln.MinutenHoechstens);
        GiltAb.Text = Datum.Text(alt is null ? heute : heute.AddDays(1));
        GiltBis.Text = Datum.Text(f?.GiltBis);

        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Titel", Titel);
        zeile("Beschreibung", Beschreibung);
        zeile("Lebensbereich", Lebensbereich);
        zeile("Ziel", Ziel);
        zeile("Art", Art);
        zeile("Rhythmus", Formular.Reihe(Rhythmus, new Label { Text = "alle", AutoSize = true, Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, 0, 0) },
            AlleWochen, new Label { Text = "Wochen · am", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, MonatsTag,
            new Label { Text = ". im Monat", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }));
        zeile("Wochentage", tage);
        zeile("Dienst, Uhrzeit", Formular.Reihe(Schicht_, Uhrzeit));
        zeile("Zeitwert (Minuten)", Minuten);
        zeile(alt is null ? "Gültig ab, bis" : "Neue Fassung ab, bis", Formular.Reihe(GiltAb, GiltBis));
        zeile(null, Fenster.Hinweis(alt is null
            ? "Die Maßnahme erscheint in ihrem Dienst zum Bestätigen. Verordnete Medikamente gehören in den Medikamentenplan."
            : "Die bisherige Fassung bleibt bis zum Tag davor gültig und im Verlauf sichtbar."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Rhythmus.SelectedIndexChanged += (_, _) => FelderZeigen();
        FelderZeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private static ComboBox Auswahl(int breite, string name, params string[] eintraege)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = breite, AccessibleName = name };
        box.Items.AddRange(eintraege);
        if (eintraege.Length > 0) box.SelectedIndex = 0;
        return box;
    }

    private MassnahmeRhythmus GewaehlterRhythmus => (MassnahmeRhythmus)Math.Max(0, Rhythmus.SelectedIndex);

    // Nur die Felder, die zum Rhythmus gehören.
    private void FelderZeigen()
    {
        var r = GewaehlterRhythmus;
        AlleWochen.Enabled = r == MassnahmeRhythmus.AlleNWochen;
        MonatsTag.Enabled = r == MassnahmeRhythmus.Monatlich;
        foreach (var box in Wochentage) box.Enabled = r is MassnahmeRhythmus.Wochentage or MassnahmeRhythmus.AlleNWochen;
        Uhrzeit.Enabled = r != MassnahmeRhythmus.BeiBedarf;
    }

    public MassnahmeFassung Lesen()
    {
        TimeOnly? uhrzeit = null;
        if (Uhrzeit.Text.Trim().Length > 0)
        {
            if (!TimeOnly.TryParseExact(Uhrzeit.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var u))
                throw new Eingabefehler(["Uhrzeit bitte als SS:MM oder leer lassen."]);
            uhrzeit = u;
        }
        DateOnly ab;
        DateOnly? bis = null;
        try
        {
            ab = Datum.Lesen(GiltAb.Text, "Gültig ab");
            if (GiltBis.Text.Trim().Length > 0) bis = Datum.Lesen(GiltBis.Text, "Gültig bis");
        }
        catch (FormatException e) { throw new Eingabefehler([e.Message]); }
        return new MassnahmeFassung
        {
            GiltAb = ab, GiltBis = bis, Titel = Titel.Text.Trim(), Beschreibung = Beschreibung.Text.Trim(), Lebensbereich = Lebensbereich.SelectedIndex + 1,
            Art = Art.SelectedItem as string ?? "", Rhythmus = GewaehlterRhythmus,
            Wochentage = [.. Wochentage.Where(b => b.Checked && b.Enabled).Select(b => (DayOfWeek)b.Tag!)], AlleWochen = (int)AlleWochen.Value,
            MonatsTag = (int)MonatsTag.Value, Schicht = (Schicht)Math.Max(0, Schicht_.SelectedIndex), Uhrzeit = uhrzeit, Minuten = (int)Minuten.Value,
            ZielId = Ziel.SelectedIndex > 0 ? ziele[Ziel.SelectedIndex - 1].Id : null,
        };
    }

    public bool Speichern()
    {
        try
        {
            var f = Lesen();
            if (MassnahmenRegeln.MedikamentImTitel(f.Titel, speicher.MedikamentNamen()) is string medikament &&
                !TrotzMedikament($"„{medikament}“ steht in der Medikamentendatenbank. {MassnahmenRegeln.MedikamentFrage}"))
                return false;
            if (alt is null) speicher.MassnahmeAnlegen(bewohnerId, f, Recht.Kuerzel, Jetzt());
            else speicher.MassnahmeAendern(alt.Id, f, Recht.Kuerzel, Jetzt());
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

// Eine Durchführung bestätigen: durchgeführt · abgelehnt · nicht möglich, Zeit (vorbelegt mit dem Zeitwert), Grund und
// Bemerkung. Kürzel und Uhrzeit setzt das Programm.
public sealed class MassnahmeBestaetigenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Massnahme massnahme;
    private readonly SchichtTermin dienst;

    public RadioButton Durchgefuehrt { get; } = new() { Text = "durchgeführt", AutoSize = true, Checked = true };
    public RadioButton Abgelehnt { get; } = new() { Text = "abgelehnt", AutoSize = true };
    public RadioButton NichtMoeglich { get; } = new() { Text = "nicht möglich", AutoSize = true };
    public NumericUpDown Minuten { get; } = new() { Minimum = 0, Maximum = MassnahmenRegeln.MinutenHoechstens, Width = 70, AccessibleName = "Zeit in Minuten" };
    public TextBox Grund { get; } = new() { Width = 320, PlaceholderText = "bei „abgelehnt“ und „nicht möglich“", AccessibleName = "Grund" };
    public TextBox Bemerkung { get; } = new() { Width = 320, PlaceholderText = "optional ein Satz", AccessibleName = "Bemerkung" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public MassnahmeBestaetigenFenster(Speicher speicher, Massnahme massnahme, SchichtTermin dienst, string bewohner, MassnahmeErgebnis vorwahl = MassnahmeErgebnis.Durchgefuehrt)
    {
        this.speicher = speicher;
        this.massnahme = massnahme;
        this.dienst = dienst;
        var f = massnahme.Am(dienst.Tag) ?? massnahme.Fassungen[^1];
        Fenster.Grundform(this, $"Maßnahme bestätigen: {bewohner}");
        Minuten.Value = Math.Clamp(f.Minuten, 0, MassnahmenRegeln.MinutenHoechstens);
        Durchgefuehrt.Checked = vorwahl == MassnahmeErgebnis.Durchgefuehrt;
        Abgelehnt.Checked = vorwahl == MassnahmeErgebnis.Abgelehnt;
        NichtMoeglich.Checked = vorwahl == MassnahmeErgebnis.NichtMoeglich;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Maßnahme", new Label { Text = $"{f.Titel} · {MassnahmenRegeln.WannText(f)} · {dienst.Text}", AutoSize = true, UseMnemonic = false });
        zeile("Ergebnis", Formular.Reihe(Durchgefuehrt, Abgelehnt, NichtMoeglich));
        zeile("Zeit (Minuten)", Minuten);
        zeile("Grund", Grund);
        zeile("Bemerkung", Bemerkung);
        zeile(null, Fenster.Hinweis("Kürzel und Uhrzeit setzt das Programm; der Eintrag erscheint in der Dokumentation."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Bestätigen", () => Bestaetigen()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public MassnahmeErgebnis Ergebnis => Abgelehnt.Checked ? MassnahmeErgebnis.Abgelehnt : NichtMoeglich.Checked ? MassnahmeErgebnis.NichtMoeglich : MassnahmeErgebnis.Durchgefuehrt;

    public bool Bestaetigen()
    {
        try { speicher.MassnahmeBestaetigen(massnahme.Id, dienst, Ergebnis, Grund.Text, (int)Minuten.Value, Bemerkung.Text, Recht.Kuerzel, Jetzt()); }
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
