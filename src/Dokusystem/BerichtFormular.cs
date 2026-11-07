using System.Drawing.Printing;
using Dokusystem.Kern;

namespace Dokusystem;

// Neuer Bericht: Bewohner (fest aus der Akte oder zur Auswahl), Datum und Uhrzeit, Schicht,
// Verfasser (Kürzel aus den Einstellungen), Lebensbereiche, Text, „für die Übergabe wichtig“.
public sealed class BerichtFormular : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> auswahl;

    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Standard, Width = 300 };
    public TextBox Datum { get; } = new() { Width = 100 };
    public TextBox Uhrzeit { get; } = new() { Width = 80 };
    public ComboBox Schichtwahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Standard, Width = 210 };
    public TextBox Verfasser { get; } = new() { Width = 80 };
    public CheckBox[] Bereiche { get; } = Enumerable.Range(1, 5).Select(n => new CheckBox
    {
        Text = BerichtRegeln.BereichText(n), AutoSize = true, Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandMini),
    }).ToArray();
    public CheckBox Allgemein { get; } = new() { Text = "allgemein (kein Lebensbereich)", AutoSize = true, Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandMini) };
    private readonly ToolTip hilfe = new();
    public TextBox Inhalt { get; } = new() { Multiline = true, Height = 100, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    public CheckBox Wichtig { get; } = new() { Text = "Wichtig – bleibt in der Akte hervorgehoben", AutoSize = true, Font = Stil.Fett, ForeColor = Stil.Hinweis.Text };
    public CheckBox Uebergabe { get; } = new() { Text = "Übergabe – erscheint in der Übergabe der folgenden Schichten", AutoSize = true, Font = Stil.Fett, ForeColor = Stil.Unterwegs.Text };
    public ComboBox UebergabeDauer { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Standard, Width = 160 };
    public TextBox UebergabeBis { get; } = new() { Width = 110, PlaceholderText = "TT.MM.JJJJ", Enabled = false };
    public TextBox UebergabeText { get; } = new() { Width = 420, PlaceholderText = "Kurzer Übergabe-Satz (freiwillig), z. B. „Arzttermin Do 10 Uhr, Begleitung nötig“" };
    private readonly Panel uebergabeOptionen = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(Stil.AbstandGross, 0, 0, Stil.AbstandKlein), Visible = false };
    private bool rueckgefragt;

    // Rückfrage „Soll das in die Übergabe?“ (Yes = in die Übergabe, No = ohne, Cancel = zurück). Zum Testen ersetzbar.
    internal Func<string, DialogResult> Rueckfrage { get; set; }
    public Label Fehler { get; } = new();
    public Bericht? Gespeichert { get; private set; }

    // Zum Testen ersetzbar.
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public BerichtFormular(Speicher speicher, Bewohner? fest)
    {
        this.speicher = speicher;
        auswahl = fest is null ? speicher.AlleBewohner() : [fest];
        Text = $"Neuer Bericht ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440));
        void Zeile(string? beschriftung, Control feld)
        {
            if (beschriftung is null) { raster.Controls.Add(feld); raster.SetColumnSpan(feld, 2); return; }
            raster.Controls.Add(new Label { Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein) });
            feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            raster.Controls.Add(feld);
        }

        Zeile(null, new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        BewohnerWahl.Items.AddRange(auswahl.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
        BewohnerWahl.SelectedIndex = auswahl.Count == 1 ? 0 : -1;
        BewohnerWahl.Enabled = fest is null;
        Zeile("Bewohner", BewohnerWahl);

        var jetzt = Jetzt();
        Datum.Text = Kern.Datum.Text(DateOnly.FromDateTime(jetzt));
        Datum.PlaceholderText = "TT.MM.JJJJ";
        Uhrzeit.Text = BerichtRegeln.UhrzeitText(jetzt);
        Uhrzeit.PlaceholderText = "SS:MM";
        // Datum, Uhrzeit und Schicht in einer Zeile: Das Fenster bleibt auch bei 150 % niedrig genug.
        var wann = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        Datum.Margin = Uhrzeit.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        Schichtwahl.Margin = new Padding(0);
        Schichtwahl.Items.AddRange(["Schicht passend zur Uhrzeit", "Frühdienst", "Spätdienst", "Nachtdienst"]);
        Schichtwahl.SelectedIndex = 0;
        wann.Controls.AddRange([Datum, Uhrzeit, Schichtwahl]);
        Zeile("Datum, Uhrzeit, Schicht", wann);
        Verfasser.Text = Einstellungen.Laden().Kuerzel;
        Verfasser.MaxLength = Einstellungen.KuerzelLaenge;
        Verfasser.PlaceholderText = "Kürzel";
        Zeile("Verfasser", Verfasser);

        // Zwei Spalten mit den kurzen Namen; der volle Name steht im Hinweis unter der Maus.
        var bereiche = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = new Padding(0) };
        for (int n = 1; n <= 5; n++)
        {
            bereiche.Controls.Add(Bereiche[n - 1], (n - 1) % 2, (n - 1) / 2);
            hilfe.SetToolTip(Bereiche[n - 1], BerichtRegeln.Lebensbereiche[n - 1]);
        }
        bereiche.Controls.Add(Allgemein, 1, 2);
        Allgemein.CheckedChanged += (_, _) => { if (Allgemein.Checked) foreach (var c in Bereiche) c.Checked = false; };
        foreach (var c in Bereiche) c.CheckedChanged += (_, _) => { if (c.Checked) Allgemein.Checked = false; };
        Zeile("Lebensbereiche", bereiche);

        Inhalt.Dock = DockStyle.Fill;
        Zeile("Text", Inhalt);
        Zeile(null, new Label
        {
            Text = "Merkhilfe: wertneutral beschreiben, nicht bewerten. Aussagen des Bewohners kennzeichnen („gibt an, dass …“). " +
                   "Erst, was der Bewohner tut oder kann, dann die Unterstützung.",
            AutoSize = true, MaximumSize = new Size(580, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise, Margin = new Padding(0, 0, 0, Stil.AbstandKlein),
        });
        // Die zwei Kennzeichen: gut sichtbar, nicht im Menü versteckt (IDEEN „Tägliche Dokumentation“).
        Zeile(null, new Label { Text = "KENNZEICHEN", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandKlein, 0, Stil.AbstandMini) });
        Zeile(null, Wichtig);
        Zeile(null, Uebergabe);
        UebergabeDauer.Items.AddRange(UebergabeRegeln.DauerTexte);
        UebergabeDauer.SelectedIndex = 0;   // nächste Schicht
        UebergabeDauer.SelectedIndexChanged += (_, _) => UebergabeBis.Enabled = UebergabeDauer.SelectedIndex == (int)Kern.UebergabeDauer.BisDatum;
        uebergabeOptionen.Controls.Add(new Label { Text = "Wie lange?", AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) });
        UebergabeDauer.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, 0);
        UebergabeBis.Margin = new Padding(0, Stil.AbstandMini, 0, 0);
        UebergabeText.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        uebergabeOptionen.Controls.AddRange([UebergabeDauer, UebergabeBis]);
        uebergabeOptionen.Controls.Add(UebergabeText);
        ((FlowLayoutPanel)uebergabeOptionen).SetFlowBreak(UebergabeBis, true);
        Uebergabe.CheckedChanged += (_, _) => uebergabeOptionen.Visible = Uebergabe.Checked;
        Zeile(null, uebergabeOptionen);

        Fehler.AutoSize = true;
        Fehler.MaximumSize = new Size(580, 0);
        Fehler.ForeColor = Stil.Beschluss.Text;
        Fehler.BackColor = Stil.Beschluss.Flaeche;
        Fehler.Padding = new Padding(Stil.AbstandKlein);
        Fehler.Visible = false;
        Zeile(null, Fehler);

        var speichern = Stil.HauptKnopf("Speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        speichern.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([speichern, abbrechen]);
        Zeile(null, knoepfe);

        Controls.Add(raster);
        CancelButton = abbrechen;
        Rueckfrage = woerter => MessageBox.Show(this,
            $"Im Text steht „{woerter}“.\n\nSoll das in die Übergabe?\n\nJa: in die Übergabe (nächste Schicht).  Nein: ohne Übergabe speichern.",
            "Übergabe?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        Stil.DpiSkalierungEinschalten(this);
    }

    public void BewohnerWaehlen(long id) => BewohnerWahl.SelectedIndex = auswahl.FindIndex(b => b.Id == id);

    protected override void Dispose(bool disposing)
    {
        if (disposing) hilfe.Dispose();
        base.Dispose(disposing);
    }

    public BerichtEingabe Eingabe() => new()
    {
        BewohnerId = BewohnerWahl.SelectedIndex >= 0 ? auswahl[BewohnerWahl.SelectedIndex].Id ?? 0 : 0,
        Datum = Datum.Text,
        Uhrzeit = Uhrzeit.Text,
        Schicht = Schichtwahl.SelectedIndex switch { 1 => Schicht.Frueh, 2 => Schicht.Spaet, 3 => Schicht.Nacht, _ => null },
        Verfasser = Verfasser.Text,
        Bereiche = Enumerable.Range(1, 5).Where(n => Bereiche[n - 1].Checked).ToHashSet(),
        Allgemein = Allgemein.Checked,
        Text = Inhalt.Text,
        Wichtig = Wichtig.Checked,
        Uebergabe = Uebergabe.Checked,
        UebergabeDauer = (Kern.UebergabeDauer)UebergabeDauer.SelectedIndex,
        UebergabeBis = UebergabeBis.Text,
        UebergabeText = UebergabeText.Text,
    };

    public bool Speichern()
    {
        // Hinweis vor dem Speichern: übergaberelevante Wörter, aber keine Übergabe gesetzt? Einmal nachfragen.
        if (!Uebergabe.Checked && !rueckgefragt)
        {
            var gefunden = UebergabeWoerter.Gefunden(Inhalt.Text, UebergabeWoerter.Laden());
            if (gefunden.Count > 0)
            {
                rueckgefragt = true;
                switch (Rueckfrage(string.Join("“, „", gefunden)))
                {
                    case DialogResult.Yes: Uebergabe.Checked = true; break;
                    case DialogResult.No: break;
                    default: return false;
                }
            }
        }
        try
        {
            var eingabe = Eingabe();
            Gespeichert = speicher.BerichtSpeichern(BerichtRegeln.AusEingabe(eingabe, Jetzt()), BerichtRegeln.UebergabeAus(eingabe));
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

// Nachtrag zu einem Bericht: Korrektur oder Ergänzung. Der ursprüngliche Text bleibt sichtbar.
public sealed class NachtragFormular : Form
{
    private readonly Speicher speicher;
    private readonly Bericht bericht;

    public TextBox Verfasser { get; } = new() { Width = 80 };
    public TextBox Inhalt { get; } = new() { Multiline = true, Height = 90, Width = 460, ScrollBars = ScrollBars.Vertical };
    public Label Fehler { get; } = new();

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public NachtragFormular(Speicher speicher, Bericht bericht)
    {
        this.speicher = speicher;
        this.bericht = bericht;
        Text = $"Nachtrag ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        Label Klein(string t, Font f, Color c) => new() { Text = t, AutoSize = true, MaximumSize = new Size(460, 0), Font = f, ForeColor = c, Margin = new Padding(0, 0, 0, Stil.AbstandMini), UseMnemonic = false };
        stapel.Controls.Add(new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        stapel.Controls.Add(Klein("URSPRÜNGLICHER BERICHT – BLEIBT, WIE ER IST", Stil.Abschnitt, Stil.TextLeise));
        stapel.Controls.Add(Klein($"{BerichtRegeln.ZeitText(bericht.Zeitpunkt)} · {bericht.Verfasser}", Stil.Fett, Stil.TextNormal));
        stapel.Controls.Add(Klein(bericht.Text, Stil.Standard, Stil.TextMittel));
        stapel.Controls.Add(Klein("Verfasser", Stil.Standard, Stil.TextMittel));
        Verfasser.Text = Einstellungen.Laden().Kuerzel;
        Verfasser.MaxLength = Einstellungen.KuerzelLaenge;
        stapel.Controls.Add(Verfasser);
        stapel.Controls.Add(Klein("Nachtrag (Korrektur oder Ergänzung)", Stil.Standard, Stil.TextMittel));
        stapel.Controls.Add(Inhalt);
        Fehler.AutoSize = true;
        Fehler.MaximumSize = new Size(460, 0);
        Fehler.ForeColor = Stil.Beschluss.Text;
        Fehler.BackColor = Stil.Beschluss.Flaeche;
        Fehler.Padding = new Padding(Stil.AbstandKlein);
        Fehler.Visible = false;
        stapel.Controls.Add(Fehler);
        var speichern = Stil.HauptKnopf("Nachtrag speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, Stil.AbstandMittel, 0, 0) };
        knoepfe.Controls.AddRange([speichern, abbrechen]);
        stapel.Controls.Add(knoepfe);
        Controls.Add(stapel);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            speicher.NachtragSpeichern(bericht.Id!.Value, Verfasser.Text, Inhalt.Text, Jetzt());
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

// Ausdruck der Berichte eines Bewohners für einen Zeitraum (A4 hoch). Zeichnet Seite für Seite;
// die Zeichenroutine ist ohne Drucker testbar (Seite zeichnen in ein Bild).
public sealed class BerichteDruck(Kern.Bewohner bewohner, IReadOnlyList<Bericht> berichte, string zeitraum)
{
    private int naechster;

    public PrintDocument DokumentBauen()
    {
        naechster = 0;
        var dokument = new PrintDocument { DocumentName = $"Berichte {bewohner.Name}" };
        dokument.BeginPrint += (_, _) => naechster = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechster = SeiteZeichnen(e.Graphics!, naechster);
            e.HasMorePages = naechster < berichte.Count;
        };
        return dokument;
    }

    // Zeichnet ab Bericht „start“ eine A4-Seite (in Millimetern) und gibt den nächsten Bericht zurück.
    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f, unten = 280f;
        float breite = rechts - links, y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 9.5F);
        using var normal = new Font("Segoe UI", 9.5F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        var format = new StringFormat { Trimming = StringTrimming.Word };

        g.DrawString($"Berichte – {bewohner.Name}, Zimmer {bewohner.Zimmer}", titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Zeitraum: {zeitraum} · {berichte.Count} Berichte · gedruckt {DateTime.Now:dd.MM.yyyy} · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y);
        y += 6f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 3f;

        int i = start;
        for (; i < berichte.Count; i++)
        {
            var b = berichte[i];
            string kopf = $"{BerichtRegeln.ZeitText(b.Zeitpunkt)} · {BerichtRegeln.SchichtText(b.Schicht)} · {b.Verfasser}" +
                          $" · {(b.Allgemein ? "allgemein" : string.Join(", ", b.Bereiche.Select(BerichtRegeln.BereichText)))}" +
                          (b.Wichtig ? " · wichtig" : "") + (b.Uebergabe ? " · Übergabe" : "");
            var teile = new List<(string Text, Font Schrift, float Einzug)> { (kopf, fett, 0f), (b.Text, normal, 0f) };
            teile.AddRange(b.Nachtraege.Select(n => ($"Nachtrag {BerichtRegeln.ZeitText(n.Erfasst)} · {n.Verfasser}: {n.Text}", normal, 5f)));
            float hoehe = teile.Sum(t => g.MeasureString(t.Text, t.Schrift, new SizeF(breite - t.Einzug, 1000f), format).Height + 0.8f) + 3f;
            if (y + hoehe > unten && i > start) break;   // passt nicht mehr – nächste Seite
            foreach (var (text, schrift, einzug) in teile)
            {
                var feld = new RectangleF(links + einzug, y, breite - einzug, 1000f);
                g.DrawString(text, schrift, Brushes.Black, feld, format);
                y += g.MeasureString(text, schrift, new SizeF(breite - einzug, 1000f), format).Height + 0.8f;
            }
            y += 2f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 3f;
        }
        if (berichte.Count == 0) g.DrawString("Keine Berichte im gewählten Zeitraum.", normal, Brushes.Black, links, y);
        return i;
    }
}
