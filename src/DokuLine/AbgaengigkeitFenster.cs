using System.Drawing.Printing;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// „Abgängigkeit melden …“ (Bauplan 34). Ohne laufende Meldung das Formular; danach im selben Fenster die Liste „Wer wurde
// informiert“ (Haken = jetzt, mit Kürzel), das Meldeblatt für die Polizei und „Zurück“; nach „Zurück“ die Liste „Wer wurde
// entwarnt“. Eine geschlossene Meldung bleibt 24 Stunden zum Entwarnen offen.
public sealed class AbgaengigkeitFenster : Form
{
    public const int EntwarnenStunden = 24;
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly Panel flaeche = new() { Dock = DockStyle.Fill, AutoScroll = true };

    public Abgaengigkeit? Meldung { get; private set; }
    public bool Formularmodus => Meldung is null;

    // Formular
    public TextBox VermisstDatum { get; } = new DatumFeld() { Width = 100 };
    public TextBox VermisstUhrzeit { get; } = new UhrzeitFeld() { Width = 70 };
    public TextBox ZuletztWo { get; } = new() { Width = 220, PlaceholderText = "z. B. Garten, Bushaltestelle" };
    public TextBox ZuletztUhrzeit { get; } = new UhrzeitFeld() { Width = 70 };
    public TextBox ZuletztVon { get; } = new() { Width = 120, PlaceholderText = "Kürzel oder Name" };
    public TextBox Kleidung { get; } = Mehrzeilig("Pflicht: Jacke, Hose, Schuhe, Farben, Tasche");
    public TextBox Gefaehrdung { get; } = Mehrzeilig("z. B. Suizidgefahr, Orientierung, Wetter");
    public Label Medikamente { get; } = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Stil.TextMittel, UseMnemonic = false };
    public TextBox Orte { get; } = Mehrzeilig("z. B. Bahnhof, frühere Wohnung, Angehörige");
    public TextBox Kontakte { get; } = Mehrzeilig("");

    // Laufende Meldung
    public ListView Stellen { get; } = Liste();
    public TextBox StellenHinweis { get; } = new() { Width = 320, PlaceholderText = "bei der Polizei: Name und Vorgangsnummer" };
    public Button MeldeblattKnopf { get; } = Stil.NebenKnopf("Meldeblatt drucken …");
    public Button PdfKnopf { get; } = Stil.NebenKnopf("Als PDF speichern …");
    public TextBox ZurueckDatum { get; } = new DatumFeld() { Width = 100 };
    public TextBox ZurueckUhrzeit { get; } = new UhrzeitFeld() { Width = 70 };
    public TextBox Zustand { get; } = Mehrzeilig("Zustand bei der Rückkehr");
    public Button ZurueckKnopf { get; } = Stil.HauptKnopf("Zurück");

    // Nach „Zurück“
    public ListView Entwarnt { get; } = Liste();
    public Button NeueMeldungKnopf { get; } = Stil.NebenKnopf("Neue Meldung …");

    public Label Fehler { get; } = Fenster.Fehlerfeld();
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    internal Func<string?> PdfZiel { get; set; }
    internal Action<Func<PrintDocument>, string> Vorschau { get; set; }

    public AbgaengigkeitFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Abgängigkeit: {bewohner.Name}");
        PdfZiel = () =>
        {
            using var dialog = new SaveFileDialog { Title = "Meldeblatt speichern", Filter = "PDF (*.pdf)|*.pdf", FileName = $"{AbgaengigkeitRegeln.Titel(bewohner)}.pdf" };
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        };
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(Owner ?? this, bauen, titel);
        Stellen.ItemCheck += (_, e) => Abhaken(Stellen, e, informiert: true);
        Entwarnt.ItemCheck += (_, e) => Abhaken(Entwarnt, e, informiert: false);
        MeldeblattKnopf.Click += (_, _) => MeldeblattDrucken();
        PdfKnopf.Click += (_, _) => PdfSpeichern();
        ZurueckKnopf.Click += (_, _) => Zurueck();
        NeueMeldungKnopf.Click += (_, _) => { Meldung = null; Aufbauen(); };
        Meldung = Weiterfuehren(speicher.Abgaengigkeiten(bewohner.Id!.Value), DateTime.Now);
        Controls.Add(flaeche);
        Aufbauen();
        Stil.DpiSkalierungEinschalten(this);
    }

    // Die laufende Meldung, sonst die zuletzt geschlossene, solange sie noch zum Entwarnen offen ist.
    public static Abgaengigkeit? Weiterfuehren(IReadOnlyList<Abgaengigkeit> liste, DateTime jetzt) =>
        liste.FirstOrDefault(a => a.Offen)
        ?? liste.FirstOrDefault(a => a.Zurueck is DateTime z && jetzt - z < TimeSpan.FromHours(EntwarnenStunden) && OffenZumEntwarnen(a).Count > 0);

    public static List<string> OffenZumEntwarnen(Abgaengigkeit a) =>
        [.. a.Informiert.Select(i => i.Stelle).Distinct().Where(s => a.Entwarnt.All(e => e.Stelle != s))];

    private static TextBox Mehrzeilig(string platzhalter) =>
        new() { Width = 420, Height = 54, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = platzhalter };

    private static ListView Liste()
    {
        var l = new ListView { View = View.Details, CheckBoxes = true, FullRowSelect = true, Width = 520, Height = 170, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        foreach (var (titel, breite) in new[] { ("Stelle", 200), ("Uhrzeit", 60), ("Kürzel", 60), ("Hinweis", 180) }) l.Columns.Add(titel, breite);
        return l;
    }

    private void Aufbauen()
    {
        flaeche.SuspendLayout();
        foreach (Control c in flaeche.Controls.Cast<Control>().ToList()) { flaeche.Controls.Remove(c); if (c is TableLayoutPanel) c.Dispose(); }
        Fehler.Visible = false;
        var raster = Formular.Raster(out var zeile);
        raster.Dock = DockStyle.Top;
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        if (Meldung is null) FormularBauen(zeile);
        else if (Meldung.Offen) LaufendBauen(zeile, Meldung);
        else EntwarnungBauen(zeile, Meldung);
        zeile(null, Fehler);
        flaeche.Controls.Add(raster);
        flaeche.ResumeLayout(true);
    }

    private void FormularBauen(Action<string?, Control> zeile)
    {
        var jetzt = Jetzt();
        VermisstDatum.Text = Datum.Text(DateOnly.FromDateTime(jetzt));
        VermisstUhrzeit.Text = jetzt.ToString("HH:mm", CultureInfo.InvariantCulture);
        var daten = speicher.UeberleitungDaten(bewohner.Id!.Value, jetzt);
        var medikamente = AbgaengigkeitRegeln.BenoetigteMedikamente(daten);
        Medikamente.Text = medikamente.Count > 0 ? $"Benötigt laut Plan: {string.Join(", ", medikamente)}" : "Regelmäßige Medikamente laut Plan: keine";
        if (Kontakte.Text.Length == 0) Kontakte.Text = AbgaengigkeitRegeln.KontakteVorschlag(daten).Replace("\n", Environment.NewLine);
        zeile("Vermisst seit", Formular.Reihe(VermisstDatum, VermisstUhrzeit));
        zeile("Zuletzt gesehen", Formular.Reihe(ZuletztWo, new Label { Text = "um", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, ZuletztUhrzeit,
            new Label { Text = "von", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, ZuletztVon));
        zeile("Kleidung heute", Kleidung);
        zeile("Gefährdung", Gefaehrdung);
        zeile(null, Medikamente);
        zeile("Mögliche Aufenthaltsorte", Orte);
        zeile("Kontakte", Kontakte);
        zeile(null, Fenster.Hinweis("Nach dem Melden: Marke „abgängig seit …“ in Übersicht, Bewohnerliste und Ausgangsliste, dazu ein wichtiger Bericht mit Übergabe."));
        var knoepfe = Formular.Knoepfe(this, "Melden", () => Melden());
        Recht.Anwenden(speicher, Funktionen.BerichteSchreiben, knoepfe.Controls.OfType<Button>().First(k => k.Text == "Melden"));
        zeile(null, knoepfe);
    }

    private void LaufendBauen(Action<string?, Control> zeile, Abgaengigkeit a)
    {
        var jetzt = Jetzt();
        zeile(null, new MarkenFeld(AbgaengigkeitRegeln.MarkeText(a), Stil.MarkeFuer(MarkenArt.Abgaengig)));
        zeile(null, Fenster.Hinweis($"Gemeldet {a.Gemeldet.ToString("dd.MM. HH:mm", De)} von {a.Kuerzel}. Haken setzen heißt: jetzt informiert, mit Ihrem Kürzel."));
        StellenFuellen(Stellen, StellenListe(a), a.Informiert);
        zeile("Wer wurde informiert", Stellen);
        zeile("Hinweis zur Stelle", StellenHinweis);
        zeile(null, Formular.Reihe(MeldeblattKnopf, PdfKnopf));
        ZurueckDatum.Text = Datum.Text(DateOnly.FromDateTime(jetzt));
        ZurueckUhrzeit.Text = jetzt.ToString("HH:mm", CultureInfo.InvariantCulture);
        zeile("Zurück am", Formular.Reihe(ZurueckDatum, ZurueckUhrzeit));
        zeile("Zustand", Zustand);
        Recht.Anwenden(speicher, Funktionen.BerichteSchreiben, ZurueckKnopf);
        zeile(null, Formular.Reihe(ZurueckKnopf, SchliessenKnopf()));
    }

    private void EntwarnungBauen(Action<string?, Control> zeile, Abgaengigkeit a)
    {
        zeile(null, Fenster.Hinweis($"Zurück seit {a.Zurueck!.Value.ToString("dd.MM. HH:mm", De)}" + (a.Zustand.Length > 0 ? $" – {a.Zustand}" : "") +
                                    ". Haken setzen heißt: jetzt entwarnt."));
        var stellen = a.Informiert.Select(i => i.Stelle).Distinct().OrderBy(s => s.StartsWith("Polizei", StringComparison.Ordinal) ? 0 : 1).ToList();
        StellenFuellen(Entwarnt, stellen, a.Entwarnt);
        zeile("Wer wurde entwarnt", Entwarnt);
        zeile(null, Formular.Reihe(NeueMeldungKnopf, SchliessenKnopf()));
    }

    private Button SchliessenKnopf()
    {
        var k = Stil.NebenKnopf("Schließen");
        k.DialogResult = DialogResult.OK;
        CancelButton = k;
        return k;
    }

    private List<string> StellenListe(Abgaengigkeit a)
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        var zust = speicher.Angaben(bewohner.Id!.Value, heute).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>();
        var liste = AbgaengigkeitRegeln.Stellen(speicher.Meldestellen(), zust, AngabenAnsicht.PersonText(speicher), heute);
        liste.AddRange(a.Informiert.Select(i => i.Stelle).Where(s => !liste.Contains(s)).Distinct());
        return liste;
    }

    private bool fuellt;

    private void StellenFuellen(ListView liste, IReadOnlyList<string> stellen, IReadOnlyList<Benachrichtigung> erledigt)
    {
        fuellt = true;
        liste.Items.Clear();
        foreach (var s in stellen)
        {
            var b = erledigt.LastOrDefault(x => x.Stelle == s);
            liste.Items.Add(new ListViewItem([s, b?.Zeit.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "", b?.Kuerzel ?? "", b?.Hinweis ?? ""]) { Checked = b is not null });
        }
        fuellt = false;
    }

    // Ein Haken trägt ein; einmal Eingetragenes bleibt (kein Abhaken zurück).
    private void Abhaken(ListView liste, ItemCheckEventArgs e, bool informiert)
    {
        if (fuellt) return;
        if (e.CurrentValue == CheckState.Checked) { e.NewValue = CheckState.Checked; return; }
        string stelle = liste.Items[e.Index].Text;
        if (!(informiert ? Informieren(stelle, StellenHinweis.Text) : Entwarnen(stelle))) e.NewValue = CheckState.Unchecked;
    }

    private bool Fehlerhaft(Action aktion)
    {
        try { aktion(); Fehler.Visible = false; return false; }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); }
        catch (IOException e) { Fehler.Text = $"Die Datei ließ sich nicht schreiben: {e.Message}"; }
        Fehler.Visible = true;
        return true;
    }

    private DateTime Zeitpunkt(TextBox tag, TextBox uhr, string name)
    {
        try
        {
            return Kern.Datum.Lesen(tag.Text, name).ToDateTime(TimeOnly.ParseExact(uhr.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture));
        }
        catch (FormatException) { throw new Eingabefehler([$"{name}: Datum bitte als TT.MM.JJJJ, Uhrzeit als SS:MM."]); }
    }

    public AbgaengigkeitMeldung Eingabe()
    {
        var vermisst = Zeitpunkt(VermisstDatum, VermisstUhrzeit, "Vermisst seit");
        DateTime? zuletzt = null;
        if (ZuletztUhrzeit.Text.Trim().Length > 0)
        {
            try { zuletzt = DateOnly.FromDateTime(vermisst).ToDateTime(TimeOnly.ParseExact(ZuletztUhrzeit.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture)); }
            catch (FormatException) { throw new Eingabefehler(["Zuletzt gesehen: Uhrzeit bitte als SS:MM."]); }
        }
        return new AbgaengigkeitMeldung
        {
            VermisstSeit = vermisst, ZuletztWo = ZuletztWo.Text.Trim(), ZuletztWann = zuletzt, ZuletztVon = ZuletztVon.Text.Trim(),
            Kleidung = Kleidung.Text.Trim(), Gefaehrdung = Gefaehrdung.Text.Trim(), Orte = Orte.Text.Trim(), Kontakte = Kontakte.Text.Trim(),
        };
    }

    public bool Melden()
    {
        if (Fehlerhaft(() => Meldung = speicher.AbgaengigkeitMelden(bewohner.Id!.Value, Eingabe(), Recht.Kuerzel, Jetzt()))) return false;
        Aufbauen();
        return true;
    }

    public bool Informieren(string stelle, string hinweis)
    {
        if (Meldung is not Abgaengigkeit a) return false;
        if (Fehlerhaft(() => speicher.AbgaengigkeitInformiert(a.Id, stelle, hinweis, Recht.Kuerzel, Jetzt()))) return false;
        StellenHinweis.Text = "";
        Neu();
        return true;
    }

    public bool Entwarnen(string stelle)
    {
        if (Meldung is not Abgaengigkeit a) return false;
        if (Fehlerhaft(() => speicher.AbgaengigkeitEntwarnt(a.Id, stelle, Recht.Kuerzel, Jetzt()))) return false;
        Neu();
        return true;
    }

    public bool Zurueck()
    {
        if (Meldung is not Abgaengigkeit a) return false;
        if (Fehlerhaft(() => speicher.AbgaengigkeitZurueck(a.Id, Zeitpunkt(ZurueckDatum, ZurueckUhrzeit, "Zurück am"), Zustand.Text, Recht.Kuerzel, Jetzt()))) return false;
        Neu();
        Aufbauen();
        return true;
    }

    // Nach jeder Eintragung frisch lesen; die Liste zeigt Uhrzeit und Kürzel erst nach dem Speichern.
    private void Neu()
    {
        Meldung = speicher.Abgaengigkeiten(bewohner.Id!.Value).FirstOrDefault(x => x.Id == Meldung!.Id);
        if (Meldung is null) return;
        BeginInvokeOderSofort(() =>
        {
            if (Meldung.Offen) StellenFuellen(Stellen, StellenListe(Meldung), Meldung.Informiert);
            else if (!Formularmodus) StellenFuellen(Entwarnt, [.. Entwarnt.Items.Cast<ListViewItem>().Select(i => i.Text)], Meldung.Entwarnt);
        });
    }

    // ItemCheck läuft, bevor der Haken gesetzt ist: Die Liste erst danach neu füllen.
    private void BeginInvokeOderSofort(Action a)
    {
        if (IsHandleCreated) BeginInvoke(a);
        else a();
    }

    public List<UeberleitungAbschnitt> Meldeblatt() =>
        AbgaengigkeitRegeln.Meldeblatt(speicher.UeberleitungDaten(bewohner.Id!.Value, Jetzt()), Meldung!);

    public bool MeldeblattDrucken()
    {
        if (Meldung is null) return false;
        var inhalt = Meldeblatt();
        speicher.Zugriff(Recht.Kuerzel, bewohner.Id, ZugriffRegeln.Gedruckt, "Meldeblatt Abgängigkeit");
        Vorschau(() => new UeberleitungDruck(AbgaengigkeitRegeln.Titel(bewohner), inhalt).DokumentBauen(), AbgaengigkeitRegeln.Titel(bewohner));
        return true;
    }

    public bool PdfSpeichern()
    {
        if (Meldung is null) return false;
        var pdf = TextPdf.Erzeugen(AbgaengigkeitRegeln.Titel(bewohner), Meldeblatt(), Stil.DemoHinweis);
        return PdfZiel() is string ziel && !Fehlerhaft(() => File.WriteAllBytes(ziel, pdf));
    }
}
