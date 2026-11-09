using System.Globalization;
using DokuLine.Kern;
using DokuLine.Update;

namespace DokuLine;

// Fenster des Betriebsmodus (Bauplan 37). Gebaut und getestet nur mit ausgedachten Daten.

// Erster Start im Betrieb: den Verwalter anlegen (Position „Administrator“, alle Bereiche, eigenes Passwort).
public sealed class ErsteinrichtungFenster : Form
{
    private readonly Speicher speicher;
    public TextBox Kuerzel { get; } = new() { Width = 100, MaxLength = 6, CharacterCasing = CharacterCasing.Upper };
    public TextBox Vorname { get; } = new() { Width = 200 };
    public TextBox Nachname { get; } = new() { Width = 200 };
    public TextBox Passwort { get; } = new() { Width = 220, UseSystemPasswordChar = true };
    public TextBox Wiederholung { get; } = new() { Width = 220, UseSystemPasswordChar = true };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public ErsteinrichtungFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Ersteinrichtung");
        var raster = Formular.Raster(out var zeile);
        zeile(null, Fenster.Hinweis("Noch niemand ist angelegt. Zuerst der Verwalter: Er hat alle Rechte und legt danach die Mitarbeiter an. " +
            $"Das Passwort braucht mindestens {PasswortRegeln.MindestLaenge} Zeichen."));
        zeile("Kürzel", Kuerzel);
        zeile("Vorname", Vorname);
        zeile("Nachname", Nachname);
        zeile("Passwort", Passwort);
        zeile("Wiederholen", Wiederholung);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Anlegen", () => Anlegen()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Anlegen()
    {
        try
        {
            if (Passwort.Text != Wiederholung.Text) throw new Eingabefehler(["Die beiden Passwörter sind verschieden."]);
            speicher.VerwalterAnlegen(Kuerzel.Text, Vorname.Text, Nachname.Text, Passwort.Text, DateTime.Now);
        }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Eigenes Passwort ändern (nach einem Einmalpasswort Pflicht).
public sealed class PasswortAendernFenster : Form
{
    private readonly Speicher speicher;
    private readonly string kuerzel;
    public TextBox Bisher { get; } = new() { Width = 220, UseSystemPasswordChar = true };
    public TextBox Neu { get; } = new() { Width = 220, UseSystemPasswordChar = true };
    public TextBox Wiederholung { get; } = new() { Width = 220, UseSystemPasswordChar = true };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public PasswortAendernFenster(Speicher speicher, string kuerzel, string bisher = "", bool pflicht = false)
    {
        this.speicher = speicher;
        this.kuerzel = kuerzel;
        Fenster.Grundform(this, $"Passwort ändern: {kuerzel}");
        Bisher.Text = bisher;
        var raster = Formular.Raster(out var zeile);
        if (pflicht) zeile(null, Fenster.Hinweis("Das Einmalpasswort gilt nur für die erste Anmeldung. Bitte jetzt ein eigenes Passwort festlegen."));
        zeile("Bisheriges Passwort", Bisher);
        zeile("Neues Passwort", Neu);
        zeile("Wiederholen", Wiederholung);
        zeile(null, Fenster.Hinweis($"Mindestens {PasswortRegeln.MindestLaenge} Zeichen, nicht das Kürzel. Das Passwort wird nur verschlüsselt (als Hash) gespeichert."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Ändern", () => Aendern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Aendern()
    {
        try
        {
            if (Neu.Text != Wiederholung.Text) throw new Eingabefehler(["Die beiden neuen Passwörter sind verschieden."]);
            speicher.PasswortAendern(kuerzel, Bisher.Text, Neu.Text, DateTime.Now);
        }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Leitung/Verwalter: Einmalpasswort vergeben (auch zum Entsperren nach 5 Fehlversuchen).
public sealed class PasswoerterFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Mitarbeiter> mitarbeiter;
    public ListBox Liste { get; } = new() { Width = 420, Height = 200, IntegralHeight = false };
    public TextBox Einmal { get; } = new() { Width = 220, ReadOnly = true, Font = Stil.KarteName };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public PasswoerterFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Passwörter der Mitarbeiter");
        mitarbeiter = [.. speicher.AlleMitarbeiter().Where(m => m.Aktiv)];
        Fuellen();
        var raster = Formular.Raster(out var zeile);
        zeile(null, Fenster.Hinweis("Ein Einmalpasswort gilt nur für die nächste Anmeldung; danach legt die Person ihr eigenes fest. Es entsperrt auch nach 5 Fehlversuchen."));
        zeile("Mitarbeiter", Liste);
        var knopf = Stil.HauptKnopf("Einmalpasswort vergeben");
        knopf.Click += (_, _) => Vergeben();
        Stil.Sperren(knopf, Recht.Grund(speicher, Funktionen.MitarbeiterBereich) is null ? null : Recht.Grund(speicher, Funktionen.MitarbeiterAlle));
        zeile(null, knopf);
        zeile("Einmalpasswort", Einmal);
        zeile(null, Fenster.Hinweis("Nur jetzt sichtbar – bitte persönlich weitergeben, nicht per E-Mail."));
        zeile(null, Fehler);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.OK;
        CancelButton = schliessen;
        zeile(null, schliessen);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        int wahl = Liste.SelectedIndex;
        Liste.Items.Clear();
        foreach (var m in mitarbeiter)
            Liste.Items.Add($"{m.Kuerzel} – {m.Name}" + (speicher.Gesperrt(m.Kuerzel) ? " · gesperrt" : !speicher.HatPasswort(m.Kuerzel) ? " · noch kein Passwort" : ""));
        Liste.SelectedIndex = Math.Min(wahl, Liste.Items.Count - 1);
    }

    public string? Vergeben()
    {
        if (Liste.SelectedIndex < 0) { Fehler.Text = "Bitte zuerst einen Mitarbeiter wählen."; Fehler.Visible = true; return null; }
        string passwort = PasswortRegeln.Einmalpasswort();
        try { speicher.EinmalpasswortSetzen(mitarbeiter[Liste.SelectedIndex].Kuerzel, passwort, Recht.Kuerzel, DateTime.Now); }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return null; }
        Einmal.Text = passwort;
        Fehler.Visible = false;
        Fuellen();
        return passwort;
    }
}

// Zugriffsprotokoll: wer wann welche Akte geöffnet, gedruckt oder geändert hat (nur mit eigenem Recht).
public sealed class ZugriffsprotokollFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> bewohner;
    public TextBox Von { get; } = new DatumFeld() { Width = 100 };
    public TextBox Bis { get; } = new DatumFeld() { Width = 100 };
    public ComboBox Bewohnerwahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    public TextBox Wer { get; } = new() { Width = 80, CharacterCasing = CharacterCasing.Upper, PlaceholderText = "Kürzel" };
    public ListView Liste { get; } = new() { View = View.Details, FullRowSelect = true, Width = 720, Height = 320, HeaderStyle = ColumnHeaderStyle.Nonclickable };
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public ZugriffsprotokollFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Zugriffsprotokoll");
        bewohner = speicher.AlleBewohner();
        Bewohnerwahl.Items.Add("(alle)");
        Bewohnerwahl.Items.AddRange([.. bewohner.Select(b => b.Name)]);
        Bewohnerwahl.SelectedIndex = 0;
        var heute = DateOnly.FromDateTime(DateTime.Now);
        Von.Text = Datum.Text(heute.AddDays(-7));
        Bis.Text = Datum.Text(heute);
        foreach (var (titel, breite) in new[] { ("Zeit", 130), ("Kürzel", 70), ("Bewohner", 180), ("Aktion", 90), ("Was", 230) }) Liste.Columns.Add(titel, breite);
        var raster = Formular.Raster(out var zeile);
        zeile("Zeitraum", Formular.Reihe(Von, Bis));
        zeile("Bewohner", Bewohnerwahl);
        zeile("Wer", Wer);
        var zeigen = Stil.HauptKnopf("Anzeigen");
        zeigen.Click += (_, _) => Anzeigen();
        zeile(null, zeigen);
        zeile(null, Liste);
        zeile(null, Fehler);
        Controls.Add(raster);
        Anzeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Anzeigen()
    {
        try
        {
            DateOnly von, bis;
            try { (von, bis) = (Datum.Lesen(Von.Text, "von"), Datum.Lesen(Bis.Text, "bis")); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            long? id = Bewohnerwahl.SelectedIndex > 0 ? bewohner[Bewohnerwahl.SelectedIndex - 1].Id : null;
            var namen = bewohner.ToDictionary(b => b.Id!.Value, b => b.Name);
            var zugriffe = speicher.Zugriffe(Recht.Kuerzel, von, bis, id, Wer.Text.Trim().Length > 0 ? Wer.Text.Trim() : null);
            Liste.BeginUpdate();
            Liste.Items.Clear();
            foreach (var z in zugriffe)
                Liste.Items.Add(new ListViewItem([z.Zeit.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), z.Kuerzel,
                    z.BewohnerId is long b ? namen.GetValueOrDefault(b, "(gelöscht)") : "", z.Aktion, z.Was]));
            Liste.EndUpdate();
            Fehler.Visible = false;
            return true;
        }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
    }
}

// Sicherung: jetzt sichern, Stände ansehen und prüfen, zurücksichern (mit Kopie des jetzigen Stands vorher).
public sealed class SicherungFenster : Form
{
    private readonly Speicher speicher;
    private List<Sicherung> staende = [];
    public Label Stand { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), UseMnemonic = false };
    public ListBox Liste { get; } = new() { Width = 560, Height = 200, IntegralHeight = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<string, bool> Bestaetigen { get; set; }

    public SicherungFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Sicherung");
        Bestaetigen = text => MessageBox.Show(this, text, "Rücksicherung", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        var raster = Formular.Raster(out var zeile);
        zeile(null, Stand);
        zeile("Stände", Liste);
        var jetzt = Stil.HauptKnopf("Jetzt sichern");
        var pruefen = Stil.NebenKnopf("Prüfen");
        var zurueck = Stil.NebenKnopf("Zurücksichern …");
        jetzt.Click += (_, _) => Sichern();
        pruefen.Click += (_, _) => Pruefen();
        zurueck.Click += (_, _) => Ruecksichern();
        Recht.Anwenden(speicher, Funktionen.BetriebVerwalten, jetzt, zurueck);
        zeile(null, Formular.Reihe(jetzt, pruefen, zurueck));
        zeile(null, Fenster.Hinweis($"Jede Nacht automatisch (Aufgabenplanung „DokuLine.exe --sicherung“ oder beim nächsten Start); behalten werden " +
            $"{SicherungRegeln.Taeglich} tägliche und {SicherungRegeln.Monatlich} monatliche Stände. Vor einer Rücksicherung legt DokuLine eine Kopie des jetzigen Stands an."));
        zeile(null, Fehler);
        Controls.Add(raster);
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        staende = Speicher.Sicherungen(Betrieb.Sicherungsordner);
        Liste.Items.Clear();
        Liste.Items.AddRange([.. staende.Select(s => $"{s.Zeit:dd.MM.yyyy HH:mm} · {s.Groesse / 1024} KB")]);
        var letzte = speicher.LetzteSicherungZeit;
        Stand.Text = $"Ordner: {Betrieb.Sicherungsordner}\nLetzte Sicherung: {(letzte is DateTime l ? l.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) : "noch keine")}";
        Stand.ForeColor = SicherungRegeln.Warnen(letzte, DateTime.Now) ? Stil.Achtung.Text : Stil.TextNormal;
    }

    private bool Melden(string text, bool fehler)
    {
        Fehler.Text = text;
        Fehler.ForeColor = fehler ? Stil.Beschluss.Text : Stil.Offen.Text;
        Fehler.Visible = true;
        return !fehler;
    }

    public bool Sichern()
    {
        try { var s = speicher.SicherungErstellen(Betrieb.Sicherungsordner, DateTime.Now); Fuellen(); return Melden($"Gesichert und geprüft: {Path.GetFileName(s.Pfad)}", false); }
        catch (Exception e) when (e is SpeicherFehler or IOException or UnauthorizedAccessException) { return Melden(e.Message, true); }
    }

    public bool Pruefen()
    {
        if (Liste.SelectedIndex < 0) return Melden("Bitte zuerst einen Stand wählen.", true);
        return Speicher.SicherungPruefen(staende[Liste.SelectedIndex].Pfad) is string f ? Melden($"Fehlerhaft: {f}", true) : Melden("In Ordnung.", false);
    }

    public bool Ruecksichern()
    {
        if (Liste.SelectedIndex < 0) return Melden("Bitte zuerst einen Stand wählen.", true);
        var s = staende[Liste.SelectedIndex];
        if (!Bestaetigen($"Den Stand vom {s.Zeit:dd.MM.yyyy HH:mm} zurückspielen? Alles, was danach eingetragen wurde, ist dann nicht mehr da " +
                         "(der jetzige Stand wird vorher als Kopie neben der Datenbank abgelegt). Bitte vorher alle anderen abmelden lassen.")) return false;
        try { string vorher = speicher.Ruecksichern(s.Pfad, Recht.Kuerzel); return Melden($"Zurückgesichert. Der vorige Stand liegt als „{Path.GetFileName(vorher)}“ daneben.", false); }
        catch (Eingabefehler e) { return Melden(string.Join(" ", e.Fehler), true); }
    }
}

// Update im Betrieb: Datei herunterladen oder vom USB-Stick wählen, Prüfsumme prüfen, nachts einspielen.
public sealed class UpdateFenster : Form
{
    public Label Stand { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), UseMnemonic = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<(string Exe, string Summe)?> DateiWaehlen { get; set; }

    public UpdateFenster(Speicher speicher)
    {
        Fenster.Grundform(this, "Update vorbereiten");
        DateiWaehlen = () =>
        {
            using var dialog = new OpenFileDialog { Title = "DokuLine.exe wählen (daneben DokuLine.exe.sha256)", Filter = "DokuLine (*.exe)|*.exe" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return null;
            string summe = dialog.FileName + ".sha256";
            return File.Exists(summe) ? (dialog.FileName, summe) : throw new UpdateFehler("Neben der Datei fehlt die Prüfsumme (…exe.sha256).");
        };
        var raster = Formular.Raster(out var zeile);
        zeile(null, Fenster.Hinweis("Im Betrieb aktualisiert sich DokuLine nicht selbst. Das vorbereitete Update spielt die Aufgabenplanung nachts ein " +
            "(„DokuLine.exe --update-einspielen“, nur wenn niemand DokuLine offen hat); die bisherige Fassung bleibt als „.alt“ daneben."));
        zeile(null, Stand);
        var laden = Stil.NebenKnopf("Herunterladen");
        var waehlen = Stil.NebenKnopf("Datei wählen …");
        laden.Click += (_, _) => Ausfuehren(() => $"Version (Build {Betriebsaufgaben.Herunterladen()}) geprüft und vorbereitet.");
        waehlen.Click += (_, _) => Ausfuehren(() =>
        {
            if (DateiWaehlen() is not var (exe, summe)) return "";
            Betriebsaufgaben.Vorbereiten(exe, File.ReadAllText(summe));
            return "Datei geprüft und vorbereitet.";
        });
        Recht.Anwenden(speicher, Funktionen.BetriebVerwalten, laden, waehlen);
        zeile(null, Formular.Reihe(laden, waehlen));
        zeile(null, Fehler);
        Controls.Add(raster);
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen() => Stand.Text = $"Diese Fassung: {Programmversion.Text}. " +
        (File.Exists(Betriebsaufgaben.Vorbereitet) ? "Ein Update ist vorbereitet und wird beim nächsten nächtlichen Lauf eingespielt." : "Kein Update vorbereitet.");

    public bool Ausfuehren(Func<string> aktion)
    {
        try
        {
            string text = aktion();
            if (text.Length == 0) return false;
            Fehler.Text = text;
            Fehler.ForeColor = Stil.Offen.Text;
        }
        catch (Exception e) when (e is UpdateFehler or IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            Fehler.Text = e is UnauthorizedAccessException ? Betriebsaufgaben.AlsAdministrator : e.Message;
            Fehler.ForeColor = Stil.Beschluss.Text;
        }
        Fehler.Visible = true;
        Fuellen();
        return Fehler.ForeColor == Stil.Offen.Text;
    }
}

// Häuser und ihre Bereiche: die Bereichswahl zeigt die Bereiche des eigenen Hauses, andere nur mit Recht.
public sealed class HaeuserFenster : Form
{
    private readonly Speicher speicher;
    public ListBox Haeuser { get; } = new() { Width = 260, Height = 110, IntegralHeight = false };
    public TextBox Name_ { get; } = new() { Width = 200, PlaceholderText = "z. B. Haus am See (ausgedacht)" };
    public Dictionary<string, ComboBox> Zuordnung { get; } = [];
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public HaeuserFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Häuser und ihre Bereiche");
        var raster = Formular.Raster(out var zeile);
        zeile(null, Fenster.Hinweis("Ohne Häuser sieht jeder alle Bereiche. Mit Häusern zeigt die Bereichswahl die Bereiche des eigenen Hauses; " +
            "andere nur mit dem Recht „Bereiche anderer Häuser wählen“. Haus-Termine im Kalender gelten für das Haus des gewählten Bereichs."));
        zeile("Häuser", Haeuser);
        var anlegen = Stil.NebenKnopf("Anlegen");
        var loeschen = Stil.NebenKnopf("Löschen");
        anlegen.Click += (_, _) => Ausfuehren(() => speicher.HausAnlegen(Name_.Text, Recht.Kuerzel));
        loeschen.Click += (_, _) => Ausfuehren(() => { if (Haeuser.SelectedItem is string h) speicher.HausLoeschen(h, Recht.Kuerzel); });
        Recht.Anwenden(speicher, Funktionen.BetriebVerwalten, anlegen, loeschen);
        zeile("Neues Haus", Formular.Reihe(Name_, anlegen, loeschen));
        foreach (var w in speicher.Wohnbereiche().Select(w => w.Name).Concat(speicher.Bereichsliste().Select(b => b.Name)).Distinct())
        {
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, AccessibleName = $"Haus von {w}" };
            box.SelectionChangeCommitted += (_, _) => Ausfuehren(() => speicher.BereichHausSetzen(w, box.SelectedIndex > 0 ? (string)box.SelectedItem! : null, Recht.Kuerzel));
            Zuordnung[w] = box;
            zeile(w, box);
        }
        zeile(null, Fehler);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.OK;
        CancelButton = schliessen;
        zeile(null, schliessen);
        Controls.Add(raster);
        Fuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Fuellen()
    {
        var haeuser = speicher.Haeuser();
        Haeuser.Items.Clear();
        Haeuser.Items.AddRange([.. haeuser]);
        var von = speicher.HausVonBereich();
        foreach (var (bereich, box) in Zuordnung)
        {
            box.Items.Clear();
            box.Items.Add("(kein Haus)");
            box.Items.AddRange([.. haeuser]);
            box.SelectedIndex = von.TryGetValue(bereich, out var h) ? haeuser.IndexOf(h) + 1 : 0;
        }
    }

    public bool Ausfuehren(Action aktion)
    {
        try { aktion(); Fehler.Visible = false; Name_.Text = ""; }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        Fuellen();
        return true;
    }
}
