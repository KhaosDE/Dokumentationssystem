using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// „Überleitungsbogen …“ (Bauplan 33): Anlass, Zeitpunkt (jetzt), Begleitung, Abschnitte zum Abhaken (alle vorbelegt),
// Besonderheiten. „Erstellen“ legt eine Kopie in die Dokumente, schreibt auf Wunsch den Bericht und die Statusmeldung
// und öffnet die Druckvorschau; „Als PDF speichern …“ legt die PDF an einen gewählten Ort.
public sealed class UeberleitungFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;

    public TextBox Anlass { get; } = new() { Width = 360, PlaceholderText = "z. B. Einweisung nach Sturz" };
    public TextBox Datum_ { get; } = new DatumFeld() { Width = 100 };
    public TextBox Uhrzeit { get; } = new UhrzeitFeld() { Width = 70 };
    public TextBox Begleitung { get; } = new() { Width = 260, PlaceholderText = "z. B. Kürzel oder „keine“" };
    public CheckedListBox Abschnitte { get; } = new() { Width = 360, Height = 190, CheckOnClick = true, IntegralHeight = false };
    public TextBox Besonderheiten { get; } = new()
    {
        Width = 360, Height = 70, Multiline = true, ScrollBars = ScrollBars.Vertical,
        PlaceholderText = "Kommunikation, Hilfsmittel, was beruhigt"
    };
    public CheckBox MitBericht { get; } = new() { Text = "Bericht „Überleitungsbogen erstellt“ schreiben (wichtig, Übergabe)", AutoSize = true, Checked = true };
    public CheckBox MitStatus { get; } = new() { Text = "Statusmeldung „in der Klinik seit …“ für alle Bereiche", AutoSize = true, Checked = true };
    public Button PdfKnopf { get; } = Stil.NebenKnopf("Als PDF speichern …");
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Dokument? Kopie { get; private set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    internal Func<string?> PdfZiel { get; set; }
    internal Action<Func<PrintDocument>, string> Vorschau { get; set; }

    public UeberleitungFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Fenster.Grundform(this, $"Überleitungsbogen: {bewohner.Name}");
        PdfZiel = () =>
        {
            using var dialog = new SaveFileDialog { Title = "Überleitungsbogen speichern", Filter = "PDF (*.pdf)|*.pdf", FileName = $"Überleitungsbogen {bewohner.Name}.pdf" };
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        };
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(Owner ?? this, bauen, titel);
        var jetzt = Jetzt();
        Datum_.Text = Kern.Datum.Text(DateOnly.FromDateTime(jetzt));
        Uhrzeit.Text = jetzt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        foreach (var a in UeberleitungRegeln.AlleAbschnitte) Abschnitte.Items.Add(a, true);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Anlass", Anlass);
        zeile("Datum, Uhrzeit", Formular.Reihe(Datum_, Uhrzeit));
        zeile("Begleitung", Begleitung);
        zeile("Abschnitte", Abschnitte);
        zeile("Besonderheiten", Besonderheiten);
        zeile(null, Fenster.Hinweis("Nur Angehaktes kommt auf das Blatt. Fehlende Angaben stehen dort als „nicht erfasst“. Eine Kopie mit Datum geht in die Dokumente."));
        zeile(null, MitBericht);
        zeile(null, MitStatus);
        zeile(null, Fehler);
        PdfKnopf.Click += (_, _) => PdfSpeichern();
        var knoepfe = Formular.Knoepfe(this, "Erstellen", () => Erstellen());
        knoepfe.Controls.Add(PdfKnopf);
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public UeberleitungEingabe Eingabe()
    {
        DateTime zeitpunkt;
        try
        {
            var tag = Kern.Datum.Lesen(Datum_.Text, "Datum");
            var uhr = TimeOnly.ParseExact(Uhrzeit.Text.Trim(), "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            zeitpunkt = tag.ToDateTime(uhr);
        }
        catch (FormatException) { throw new Eingabefehler(["Datum bitte als TT.MM.JJJJ, Uhrzeit als SS:MM."]); }
        return new UeberleitungEingabe
        {
            Anlass = Anlass.Text, Zeitpunkt = zeitpunkt, Begleitung = Begleitung.Text, Besonderheiten = Besonderheiten.Text,
            Abschnitte = Abschnitte.CheckedItems.Cast<string>().ToHashSet(),
        };
    }

    public List<UeberleitungAbschnitt> Inhalt() => UeberleitungRegeln.Zusammenstellen(speicher.UeberleitungDaten(bewohner.Id!.Value, Jetzt()), Eingabe());

    public byte[] Pdf() => TextPdf.Erzeugen(UeberleitungRegeln.Titel(bewohner), Inhalt(), Stil.DemoHinweis);

    private bool Fehlerhaft(Action aktion)
    {
        try { aktion(); return false; }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); }
        catch (IOException e) { Fehler.Text = $"Die Datei ließ sich nicht schreiben: {e.Message}"; }
        Fehler.Visible = true;
        return true;
    }

    public bool PdfSpeichern()
    {
        byte[] pdf = [];
        if (Fehlerhaft(() => pdf = Pdf()) || PdfZiel() is not string ziel) return false;
        if (Fehlerhaft(() => File.WriteAllBytes(ziel, pdf))) return false;
        speicher.Zugriff(Recht.Kuerzel, bewohner.Id, ZugriffRegeln.Gedruckt, "Überleitungsbogen (PDF)");
        return true;
    }

    // Kopie in die Dokumente (wenn man darf), Bericht und Statusmeldung auf Wunsch, dann die Druckvorschau.
    public bool Erstellen()
    {
        List<UeberleitungAbschnitt> inhalt = [];
        UeberleitungEingabe e = new();
        if (Fehlerhaft(() => { e = Eingabe(); inhalt = UeberleitungRegeln.Zusammenstellen(speicher.UeberleitungDaten(bewohner.Id!.Value, Jetzt()), e); })) return false;
        var jetzt = Jetzt();
        if (Recht.Grund(speicher, Funktionen.DokumenteHinzufuegen) is null
            && Fehlerhaft(() => Kopie = speicher.UeberleitungAblegen(bewohner.Id!.Value, e.Anlass, TextPdf.Erzeugen(UeberleitungRegeln.Titel(bewohner), inhalt, Stil.DemoHinweis), Recht.Kuerzel, jetzt)))
            return false;
        if (MitBericht.Checked && Recht.Grund(speicher, Funktionen.BerichteSchreiben) is null)
            Fehlerhaft(() => speicher.UeberleitungBericht(bewohner.Id!.Value, e.Anlass, Recht.Kuerzel, jetzt));
        if (MitStatus.Checked && Recht.Angemeldet)
            Fehlerhaft(() => speicher.UeberleitungStatus(bewohner.Id!.Value, Recht.Kuerzel, Einstellungen.Laden().Bereich, jetzt));
        speicher.Zugriff(Recht.Kuerzel, bewohner.Id, ZugriffRegeln.Gedruckt, "Überleitungsbogen");
        Vorschau(() => new UeberleitungDruck(UeberleitungRegeln.Titel(bewohner), inhalt).DokumentBauen(), UeberleitungRegeln.Titel(bewohner));
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Ausdruck: A4 hoch, Überschriften fett, mehrere Seiten; oben der Demo-Hinweis.
public sealed class UeberleitungDruck(string titel, IReadOnlyList<UeberleitungAbschnitt> abschnitte)
{
    private int abschnitt, zeile;

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = titel };
        dokument.BeginPrint += (_, _) => (abschnitt, zeile) = (0, 0);
        dokument.PrintPage += (_, e) => e.HasMorePages = SeiteZeichnen(e.Graphics!, e.MarginBounds);
        return dokument;
    }

    // true: es kommt noch eine Seite.
    public bool SeiteZeichnen(Graphics g, Rectangle rand)
    {
        using var titelSchrift = new Font(Stil.Standard.FontFamily, 16, FontStyle.Bold);
        using var kopf = new Font(Stil.Standard.FontFamily, 11, FontStyle.Bold);
        using var text = new Font(Stil.Standard.FontFamily, 10);
        float y = rand.Top;
        g.DrawString(Stil.DemoHinweis, text, Brushes.DarkRed, rand.Left, y);
        y += text.GetHeight(g) * 1.5f;
        if (abschnitt == 0 && zeile == 0)
        {
            g.DrawString(titel, titelSchrift, Brushes.Black, rand.Left, y);
            y += titelSchrift.GetHeight(g) * 1.4f;
        }
        for (; abschnitt < abschnitte.Count; abschnitt++, zeile = 0)
        {
            var a = abschnitte[abschnitt];
            if (zeile == 0)
            {
                if (y + kopf.GetHeight(g) * 2 > rand.Bottom) return true;
                y += 4;
                g.DrawString(a.Titel, kopf, Brushes.Black, rand.Left, y);
                y += kopf.GetHeight(g) * 1.2f;
            }
            for (; zeile < a.Zeilen.Count; zeile++)
            {
                var groesse = g.MeasureString(a.Zeilen[zeile], text, rand.Width);
                if (y + groesse.Height > rand.Bottom) return true;
                g.DrawString(a.Zeilen[zeile], text, Brushes.Black, new RectangleF(rand.Left, y, rand.Width, groesse.Height));
                y += groesse.Height;
            }
        }
        return false;
    }
}

// Angaben der Einrichtung für den Überleitungsbogen (Einstellungen → Wohnbereich); im Datenordner, nie im Repository.
public sealed class EinrichtungFenster : Form
{
    public TextBox NameFeld { get; } = new() { Width = 360 };
    public TextBox Anschrift { get; } = new() { Width = 360 };
    public TextBox Telefon { get; } = new() { Width = 200 };

    public EinrichtungFenster(Speicher speicher)
    {
        Fenster.Grundform(this, "Angaben der Einrichtung");
        var w = Einrichtung.Laden();
        (NameFeld.Text, Anschrift.Text, Telefon.Text) = (w.Name, w.Anschrift, w.Telefon);
        var raster = Formular.Raster(out var zeile);
        zeile(null, Fenster.Hinweis("Für den Überleitungsbogen. Gilt für diesen Rechner (Datenordner). Demo: ausgedachte Angaben."));
        zeile("Name", NameFeld);
        zeile("Anschrift", Anschrift);
        zeile("Telefon Dienstzimmer", Telefon);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        Stil.Sperren(knoepfe.Controls.OfType<Button>().First(k => k.Text == "Speichern"), Recht.Grund(speicher, Funktionen.EinstellungenBereich));
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public void Speichern()
    {
        Einrichtung.Speichern(new EinrichtungWerte(NameFeld.Text, Anschrift.Text, Telefon.Text));
        DialogResult = DialogResult.OK;
        Close();
    }
}
