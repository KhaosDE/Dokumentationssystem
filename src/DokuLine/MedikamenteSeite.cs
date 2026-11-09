using DokuLine.Kern;

namespace DokuLine;

// Die Medikamentendatenbank (Demo-Bestand) mit Suche; Einträge ansehen, anlegen, ändern und sperren.
// Jede Änderung ist eine neue Fassung; der Verlauf bleibt einsehbar. Steht in den Einstellungen
// (MedikamenteFenster). Die Quelle steht als Adresse da (zum Kopieren) – das Programm geht nicht ins Internet.
public sealed class MedikamenteSeite : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public TextBox Suche { get; } = new() { Width = 260, PlaceholderText = "Wirkstoff, Handelsname oder Form suchen", Font = Stil.Standard };
    public MarkenFeld Hinweis { get; } = new(MedikamentRegeln.DemoHinweis, Stil.Achtung);
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neues Medikament …");
    public List<MedikamentKarte> Karten { get; } = [];
    public int Anzahl { get; private set; }
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // Zum Testen ersetzbar: legt den Text in die Zwischenablage.
    internal Action<string> Kopieren { get; set; } = text => Clipboard.SetText(text);

    public MedikamenteSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        NeuKnopf.Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandKlein);
        Suche.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandGross, Stil.AbstandKlein);
        Hinweis.Margin = new Padding(0, Stil.AbstandKlein, 0, Stil.AbstandKlein);
        leiste.Controls.AddRange([NeuKnopf, Suche, Hinweis]);
        Suche.TextChanged += (_, _) => Aktualisieren();
        NeuKnopf.Click += (_, _) => Bearbeiten(null);
        Controls.Add(leiste);
    }

    public string KopfText => (Anzahl == 1 ? "1 Medikament" : $"{Anzahl} Medikamente") + " · Demo-Bestand";

    public void Aktualisieren()
    {
        var liste = speicher.Medikamente(Suche.Text);
        Anzahl = speicher.Medikamente().Count;
        Recht.Anwenden(speicher, Funktionen.MedikamentAnlegen, NeuKnopf);   // Rechte
        string? verwalten = Recht.Grund(speicher, Funktionen.MedikamenteVerwalten);
        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        foreach (var m in liste)
        {
            var karte = new MedikamentKarte(m);
            Stil.Sperren(karte.AendernKnopf, verwalten);
            Stil.Sperren(karte.SperrenKnopf, verwalten);
            karte.AendernKnopf.Click += (_, _) => Bearbeiten(m);
            karte.SperrenKnopf.Click += (_, _) => Sperren(m, !m.Gesperrt);
            karte.VerlaufKnopf.Click += (_, _) => VerlaufZeigen(m);
            karte.KopierenKnopf.Click += (_, _) => Kopieren(m.Aktuell.Quelle);
            Karten.Add(karte);
            inhalt.Add(karte);
        }
        if (liste.Count == 0)
            inhalt.Add(new Label { Text = "Nichts gefunden. Fehlt ein Medikament, lässt es sich mit „Neues Medikament …“ anlegen.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    public void Bearbeiten(Medikament? m)
    {
        using var fenster = new MedikamentFenster(speicher, m);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    // Sperren (nicht mehr wählbar beim Verordnen) oder wieder freigeben; bleibt im Verlauf.
    public bool Sperren(Medikament m, bool gesperrt)
    {
        if (Recht.Grund(speicher, Funktionen.MedikamenteVerwalten) is not null) return false;
        speicher.MedikamentSperren(m.Id, gesperrt, Recht.Kuerzel);
        Aktualisieren();
        return true;
    }

    public void VerlaufZeigen(Medikament m)
    {
        using var fenster = new MedikamentVerlaufFenster(m);
        Dialog.Zeigen(fenster, FindForm());
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Location = new Point(rand, y);
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is MedikamentKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandKlein));
                y = c.Bottom;
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Ein Medikament: Wirkstoff, Handelsnamen, Marken (gesperrt, Prüfstatus, Form, Stärken, selbst angelegt,
// Fassungen), Quelle mit „Adresse kopieren“, Knöpfe „Ändern …“, „Sperren“ und „Verlauf …“.
public sealed class MedikamentKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label wirkstoff = new();
    private readonly Label namen = new();
    private readonly Label quelle = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = false, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Medikament Medikament { get; }
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public Button KopierenKnopf { get; } = Stil.NebenKnopf("Adresse kopieren");
    public Button SperrenKnopf { get; } = Stil.NebenKnopf("Sperren");
    public IReadOnlyList<string> MarkenTexte { get; }

    public MedikamentKarte(Medikament m)
    {
        Medikament = m;
        SperrenKnopf.Text = m.Gesperrt ? "Wieder freigeben" : "Sperren";
        var f = m.Aktuell;
        var liste = new List<(string, Stil.Marke)>
        {
            (MedikamentRegeln.PruefText(f), f.Pruefstatus == Pruefstatus.Bestaetigt ? Stil.Offen : Stil.Achtung),
        };
        if (f.Darreichungsform.Length > 0) liste.Add((f.Darreichungsform, Stil.Zimmer));
        if (f.Staerken.Count > 0) liste.Add((string.Join(" · ", f.Staerken), Stil.Zimmer));
        else liste.Add(("Stärken: keine Angabe", Stil.Zimmer));
        if (f.Gesperrt) liste.Insert(0, ("gesperrt – nicht wählbar", Stil.Beschluss));
        if (f.Zulassungsnummer.Length > 0) liste.Add(($"Zul.-Nr. {f.Zulassungsnummer}", Stil.Zimmer));
        if (m.SelbstAngelegt) liste.Add(("selbst angelegt", Stil.Hinweis));
        if (m.Fassungen.Count > 1) liste.Add(($"geändert ({m.Fassungen.Count} Fassungen)", Stil.Hinweis));
        MarkenTexte = liste.Select(x => x.Item1).ToList();
        foreach (var (t, mk) in liste) marken.Controls.Add(new MarkenFeld(t, mk) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        wirkstoff.Text = f.Wirkstoff;
        wirkstoff.Font = Stil.KarteName;
        wirkstoff.ForeColor = Stil.TextNormal;
        namen.Text = f.Handelsnamen.Count > 0 ? $"Handelsnamen: {string.Join(", ", f.Handelsnamen)}" : "Handelsnamen: keine Angabe";
        namen.Font = Stil.Standard;
        namen.ForeColor = Stil.TextMittel;
        quelle.Text = f.Quelle.Length > 0 ? $"Quelle: {f.Quelle}" : "Quelle: keine";
        quelle.Font = Stil.Klein;
        quelle.ForeColor = Stil.TextLeise;
        foreach (var l in new[] { wirkstoff, namen, quelle })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        KopierenKnopf.Visible = f.Quelle.Length > 0;
        VerlaufKnopf.Visible = m.Fassungen.Count > 1;
        foreach (var k in new[] { AendernKnopf, SperrenKnopf, VerlaufKnopf, KopierenKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{f.Wirkstoff}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Medikament is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        wirkstoff.Location = new Point(innen, y);
        y = wirkstoff.Bottom + Stil.Px(this, 2);
        namen.MaximumSize = new Size(breite, 0);
        namen.Location = new Point(innen, y);
        y = namen.Bottom + Stil.Px(this, Stil.AbstandKlein);
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        quelle.MaximumSize = new Size(breite, 0);
        quelle.Location = new Point(innen, y);
        y = quelle.Bottom + Stil.Px(this, Stil.AbstandKlein);
        knoepfe.Bounds = new Rectangle(new Point(innen, y), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int hoehe = knoepfe.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Anlegen oder ändern. Beim Ändern entsteht eine neue Fassung mit Datum und Kürzel.
// Prüfung: Wer anlegt, prüft selbst gegen eine offizielle Quelle und bestätigt mit
// Quelle und Prüfdatum. Oben die Nachschlage-Ansicht des BfArM (abschaltbar, dann nur „Im Browser
// öffnen“); markierter Text geht mit „In Feld übernehmen“ in das zuletzt gewählte Feld.
public sealed class MedikamentFenster : Form
{
    private readonly Speicher speicher;
    private readonly Medikament? bisher;
    private readonly List<(TextBox Feld, string Name)> felder;
    private TextBox ziel;
    // Inhalt mit Bildlaufleiste; Fehler, „Speichern“ und „Abbrechen“ bleiben unten immer sichtbar
    // (mit der Nachschlage-Ansicht ist das Formular sonst auf kleinen Bildschirmen zu hoch).
    private readonly Panel inhalt = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly FlowLayoutPanel unten = new()
    {
        Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandGross),
    };
    private readonly TableLayoutPanel raster = new() { ColumnCount = 2, AutoSize = true, Padding = new Padding(Stil.AbstandGross) };

    public TextBox Wirkstoff { get; } = new() { Width = 360 };
    public TextBox Handelsnamen { get; } = new() { Width = 360, PlaceholderText = "durch Komma getrennt" };
    public TextBox Staerken { get; } = new() { Width = 360, PlaceholderText = "durch Komma getrennt, z. B. 25 mg, 100 mg" };
    public TextBox Darreichungsform { get; } = new() { Width = 240, PlaceholderText = "z. B. Filmtablette" };
    public TextBox Quelle { get; } = new() { Width = 420, PlaceholderText = "Adresse der Fach- oder Gebrauchsinformation" };
    public TextBox Zulassungsnummer { get; } = new() { Width = 180, PlaceholderText = "optional" };
    public CheckBox Bestaetigt { get; } = new() { Text = "bestätigt – Angaben mit der Quelle geprüft", AutoSize = true };
    public TextBox GeprueftAm { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ", Enabled = false };
    // Nachschlage-Ansicht (null, wenn in den Einstellungen abgeschaltet) – dann nur dieser Knopf.
    public Nachschlageansicht? Nachschlage { get; }
    public Button BrowserKnopf { get; } = Stil.NebenKnopf("Im Browser öffnen");
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(700, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Medikament? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public MedikamentFenster(Speicher speicher, Medikament? bisher)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        Text = (bisher is null ? "Neues Medikament" : "Medikament ändern") + $" ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = inhalt.BackColor = unten.BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        void Zeile(string? beschriftung, Control feld)
        {
            if (beschriftung is null) { raster.Controls.Add(feld); raster.SetColumnSpan(feld, 2); return; }
            raster.Controls.Add(new Label { Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein) });
            feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            raster.Controls.Add(feld);
        }
        Zeile(null, new MarkenFeld(MedikamentRegeln.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        if (Einstellungen.Laden().NachschlageAnsicht)
        {
            Nachschlage = new Nachschlageansicht { Size = new Size(760, 340), Margin = new Padding(0, 0, 0, Stil.AbstandMittel) };
            Nachschlage.Suchname = () => Wirkstoff.Text.Trim().Length > 0 ? Wirkstoff.Text : MedikamentRegeln.Liste(Handelsnamen.Text).FirstOrDefault() ?? "";
            Nachschlage.TextUebernehmen += InFeldUebernehmen;
            Nachschlage.QuelleUebernehmen += adresse => Quelle.Text = adresse;
            Zeile(null, Nachschlage);
        }
        else
        {
            BrowserKnopf.AutoSize = true;
            BrowserKnopf.Click += (_, _) => Nachsehen();
            var reihe = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, Stil.AbstandMittel) };
            reihe.Controls.Add(new Label { Text = "Nachschlagen beim BfArM", AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, 0) });
            reihe.Controls.Add(BrowserKnopf);
            Zeile(null, reihe);
        }
        felder = [(Wirkstoff, "Wirkstoff"), (Handelsnamen, "Handelsnamen"), (Staerken, "Stärken"), (Darreichungsform, "Darreichungsform"),
                  (Zulassungsnummer, "Zulassungsnummer"), (Quelle, "Quelle")];
        foreach (var (feld, name) in felder)
        {
            Zeile(name, feld);
            feld.Enter += (_, _) => ZielWaehlen(feld);
        }
        ziel = Wirkstoff;
        ZielWaehlen(Wirkstoff);
        Zeile("Prüfstatus", Bestaetigt);
        Zeile("Geprüft am", GeprueftAm);
        Bestaetigt.CheckedChanged += (_, _) =>
        {
            GeprueftAm.Enabled = Bestaetigt.Checked;
            if (Bestaetigt.Checked && GeprueftAm.Text.Trim().Length == 0) GeprueftAm.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        };
        Zeile(null, new Label
        {
            Text = "Wer ein Medikament anlegt, prüft es selbst gegen eine offizielle Quelle (Fach- oder Gebrauchsinformation) " +
                   "und bestätigt es hier mit Quelle und Prüfdatum. Ohne Bestätigung bleibt es „ungeprüft“ und ist beim Verordnen so gekennzeichnet. " +
                   "Stärken und Formen nur eintragen, wenn sie in der Quelle stehen – sonst leer lassen. " +
                   (bisher is null ? "Neue Einträge sind als „selbst angelegt“ gekennzeichnet." : "Die bisherige Fassung bleibt im Verlauf."),
            AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        Zeile("Eingetragen von", Recht.KuerzelAnzeige());
        if (bisher is not null)
        {
            var e = MedikamentRegeln.AlsEingabe(bisher.Aktuell);
            Wirkstoff.Text = e.Wirkstoff;
            Handelsnamen.Text = e.Handelsnamen;
            Staerken.Text = e.Staerken;
            Darreichungsform.Text = e.Darreichungsform;
            Quelle.Text = e.Quelle;
            Zulassungsnummer.Text = e.Zulassungsnummer;
            GeprueftAm.Text = e.GeprueftAm;
            Bestaetigt.Checked = e.Bestaetigt;
        }
        var speichern = Stil.HauptKnopf("Speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        speichern.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        knoepfe.Controls.AddRange([speichern, abbrechen]);
        unten.Controls.AddRange([Fehler, knoepfe]);
        inhalt.Controls.Add(raster);
        Controls.Add(inhalt);
        Controls.Add(unten);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
        // Vorläufige Größe (auch für Tests ohne Anzeige); OnLoad passt sie an den Bildschirm an.
        ClientSize = new Size(raster.PreferredSize.Width + SystemInformation.VerticalScrollBarWidth, raster.PreferredSize.Height + unten.PreferredSize.Height);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var flaeche = Screen.FromControl(this).WorkingArea;
        int rahmen = Height - ClientSize.Height;
        int breite = raster.GetPreferredSize(Size.Empty).Width + SystemInformation.VerticalScrollBarWidth;
        int hoehe = raster.GetPreferredSize(new Size(breite, 0)).Height + unten.GetPreferredSize(new Size(breite, 0)).Height;
        ClientSize = new Size(Math.Min(breite, flaeche.Width - 40), Math.Min(hoehe, flaeche.Height - rahmen - 40));
        CenterToParent();
    }

    // Öffnet die Arzneimittelsuche des BfArM im normalen Browser (Übergabe, kein eigener Netzzugriff).
    public void Nachsehen()
    {
        if (Nachschlage is not null) Nachschlage.ImBrowserOeffnen();
        else Extern.Oeffnen(Nachschlagen.Start.AbsoluteUri);
    }

    // Zielfeld für „In Feld übernehmen“: das zuletzt angeklickte Feld der Maske.
    public void ZielWaehlen(TextBox feld)
    {
        ziel = feld;
        Nachschlage?.ZielSetzen(felder.First(f => f.Feld == feld).Name);
    }

    // Markierten Text aus der Ansicht übernehmen. Bei Listenfeldern (Handelsnamen, Stärken) kommt er
    // dazu, sonst ersetzt er den Inhalt.
    public void InFeldUebernehmen(string text)
    {
        bool liste = ziel == Handelsnamen || ziel == Staerken;
        var vorhanden = MedikamentRegeln.Liste(ziel.Text);
        if (liste && vorhanden.Count > 0)
        {
            if (!vorhanden.Contains(text, StringComparer.CurrentCultureIgnoreCase)) ziel.Text = string.Join(", ", vorhanden.Append(text));
        }
        else ziel.Text = text;
    }

    public bool Speichern()
    {
        try
        {
            var fassung = MedikamentRegeln.AusEingabe(new MedikamentEingabe
            {
                Wirkstoff = Wirkstoff.Text, Handelsnamen = Handelsnamen.Text, Staerken = Staerken.Text,
                Darreichungsform = Darreichungsform.Text, Quelle = Quelle.Text, Zulassungsnummer = Zulassungsnummer.Text, Bestaetigt = Bestaetigt.Checked,
                GeprueftAm = GeprueftAm.Text, Gesperrt = bisher?.Gesperrt ?? false, Kuerzel = Recht.Kuerzel,
            }, Jetzt());
            Gespeichert = bisher is null ? speicher.MedikamentAnlegen(fassung) : speicher.MedikamentAendern(bisher.Id, fassung);
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

// Verlauf eines Medikaments: alle Fassungen mit Datum, Kürzel und was sich geändert hat.
public sealed class MedikamentVerlaufFenster : Form
{
    public TextBox Inhalt { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 560, Height = 300, BackColor = Stil.Flaeche };

    public MedikamentVerlaufFenster(Medikament m)
    {
        Text = $"Verlauf: {m.Wirkstoff} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var zeilen = new List<string>();
        for (int i = m.Fassungen.Count - 1; i >= 0; i--)
        {
            var f = m.Fassungen[i];
            string wer = f.Kuerzel == Demodaten.DemoKuerzel ? "aus dem Demo-Bestand" : $"von {f.Kuerzel}";
            zeilen.Add($"Fassung {i + 1} · {BerichtRegeln.ZeitText(f.Geaendert)} · {wer}" + (i == m.Fassungen.Count - 1 ? " · gilt jetzt" : ""));
            if (i > 0) zeilen.AddRange(MedikamentRegeln.Unterschiede(m.Fassungen[i - 1], f).Select(u => "   " + u));
            else zeilen.Add(m.SelbstAngelegt ? "   selbst angelegt" : "   erste Fassung");
            zeilen.Add("");
        }
        Inhalt.Text = string.Join(Environment.NewLine, zeilen).TrimEnd();
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        stapel.Controls.AddRange([Inhalt, schliessen]);
        schliessen.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        Controls.Add(stapel);
        CancelButton = schliessen;
        Stil.DpiSkalierungEinschalten(this);
    }
}

// Medikamentendatenbank als eigenes Fenster: aus den Einstellungen, Abschnitt „Medikamente“.
public sealed class MedikamenteFenster : Form
{
    public MedikamenteSeite Seite { get; }

    public MedikamenteFenster(Speicher speicher)
    {
        Font = Stil.Standard;
        BackColor = Stil.Seite;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        var flaeche = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 768);
        Size = new Size(Math.Min(980, flaeche.Width - 40), Math.Min(760, flaeche.Height - 40));
        MinimumSize = new Size(560, 400);
        Seite = new MedikamenteSeite(speicher) { Dock = DockStyle.Fill };
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        schliessen.AutoSize = true;
        var unten = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, BackColor = Stil.Fensterhintergrund,
            Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein),
        };
        unten.Controls.Add(schliessen);
        Controls.Add(Seite);
        Controls.Add(unten);
        CancelButton = schliessen;
        Seite.Geaendert += () => Text = $"Medikamentendatenbank – {Seite.KopfText} ({Stil.DemoHinweis})";
        Seite.Aktualisieren();
    }
}
