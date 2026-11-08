using Dokusystem.Kern;

namespace Dokusystem;

// Reiter „Biografie“ in der Akte: je Abschnitt eine Karte mit Fließtext, Hinweis „vom Bewohner
// angegeben, nicht überprüft“ und „Bearbeiten …“/„Verlauf …“. Am Ende die fehlenden Angaben.
// Oben „Als Fließtext kopieren“ (reiner Text, ohne Formatierung).
public sealed class BiografieAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private Bewohner? bewohner;

    public Button KopierenKnopf { get; } = Stil.NebenKnopf("Als Fließtext kopieren");
    public Label Kopiert { get; } = new() { AutoSize = true, ForeColor = Stil.Offen.Text, BackColor = Stil.Seite, Visible = false };
    public List<BiografieKarte> Karten { get; } = [];
    public HinweisFeld Fehlend { get; } = new();
    public IReadOnlyList<BiografieAbschnitt> Abschnitte { get; private set; } = [];

    // Zum Testen ersetzbar: in die Zwischenablage.
    public Action<string> Kopieren { get; set; } = text => Clipboard.SetText(text);

    public BiografieAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        KopierenKnopf.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        Kopiert.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        KopierenKnopf.Click += (_, _) =>
        {
            if (bewohner is null) return;
            Kopieren(BiografieRegeln.AlsFliesstext(bewohner.Name, speicher.Biografie(bewohner.Id!.Value)));
            Kopiert.Text = "Kopiert ✓ – zum Einfügen Strg+V";
            Kopiert.Visible = true;
        };
        leiste.Controls.AddRange([KopierenKnopf, Kopiert]);
        Controls.Add(leiste);
        Controls.Add(Fehlend);
    }

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        Kopiert.Visible = false;
        Abschnitte = speicher.Biografie(b.Id!.Value);
        SuspendLayout();
        foreach (var k in Karten) { Controls.Remove(k); k.Dispose(); }
        Karten.Clear();
        foreach (var a in Abschnitte)
        {
            var karte = new BiografieKarte(a);
            karte.BearbeitenKnopf.Click += (_, _) => Bearbeiten(a.Nummer);
            karte.VerlaufKnopf.Click += (_, _) =>
            {
                using var fenster = new TextFenster($"Verlauf: {a.Titel} – {b.Name}",
                    string.Join(Environment.NewLine + Environment.NewLine, BiografieRegeln.Verlauf(a)));
                Dialog.Zeigen(fenster, FindForm());
            };
            Karten.Add(karte);
            Controls.Add(karte);
        }
        var fehlend = BiografieRegeln.FehlendeAngaben(Abschnitte);
        if (fehlend.Count > 0) Fehlend.Setzen($"Fehlende Angaben: {string.Join(", ", fehlend)}.", Stil.Achtung, true);
        else Fehlend.Setzen("Keine fehlenden Angaben.", Stil.Offen, false);
        ResumeLayout(false);
        PerformLayout();
    }

    public void Bearbeiten(int abschnitt)
    {
        if (bewohner is null) return;
        using var fenster = new BiografieFenster(speicher, bewohner, Abschnitte[abschnitt]);
        Dialog.Zeigen(fenster, FindForm());
        Zeigen(bewohner);   // immer neu lesen: auch nach „Andere übernehmen“ im Konflikt-Hinweis
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = Math.Max(100, ClientSize.Width), abstand = Stil.Px(this, Stil.AbstandMittel);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(breite, 0)));
        int y = leiste.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var k in Karten)
        {
            k.SetBounds(0, y, breite, k.Height);
            k.PerformLayout();
            y = k.Bottom + abstand;
        }
        Fehlend.Bounds = new Rectangle(0, y, breite, Stil.Px(this, 52));
        int hoehe = Fehlend.Bottom + Stil.Px(this, Stil.AbstandKlein);
        if (Height != hoehe) Height = hoehe;
    }
}

// Ein Abschnitt der Biografie als Karte.
public sealed class BiografieKarte : KartenFlaeche
{
    private readonly Label titel = new();
    private readonly Label text = new();
    private readonly Label fuss = new();
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public BiografieAbschnitt Abschnitt { get; }
    public Button BearbeitenKnopf { get; } = Stil.NebenKnopf("Bearbeiten …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public IEnumerable<string> MarkenTexte => marken.Controls.OfType<MarkenFeld>().Select(m => m.Text);
    public string InhaltText => text.Text;

    public BiografieKarte(BiografieAbschnitt a)
    {
        Abschnitt = a;
        titel.Text = a.Titel;
        titel.Font = Stil.KarteName;
        titel.ForeColor = Stil.TextNormal;
        var f = a.Aktuell;
        // Leer ist nicht „nichts“: deutlich als fehlende Angabe gekennzeichnet (Regel 5).
        text.Text = a.Leer ? "Keine Angabe – fehlt noch." : f!.Text;
        text.Font = a.Leer ? Stil.FettKursiv : Stil.Standard;
        text.ForeColor = a.Leer ? Stil.Achtung.Text : Stil.TextNormal;
        if (f is not null && !a.Leer)
        {
            if (f.Angegeben) marken.Controls.Add(new MarkenFeld("vom Bewohner angegeben, nicht überprüft", Stil.Hinweis) { Margin = new Padding(0, 0, Stil.AbstandMini, 0) });
            if (BiografieRegeln.FormulierungFehlt(f))
                marken.Controls.Add(new MarkenFeld($"Hinweis: Formulierung „{BiografieRegeln.Formulierung}“ verwenden", Stil.Achtung) { Margin = new Padding(0, 0, Stil.AbstandMini, 0) });
        }
        fuss.Text = f is null ? "" : $"{(a.Fassungen.Count == 1 ? "Angelegt" : "Zuletzt geändert")} am {f.Erstellt:dd.MM.yyyy} von {f.Kuerzel}" +
                                      (a.Fassungen.Count > 1 ? $" · {a.Fassungen.Count} Fassungen" : "");
        fuss.Font = Stil.Klein;
        fuss.ForeColor = Stil.TextLeise;
        VerlaufKnopf.Enabled = a.Fassungen.Count > 0;
        foreach (var l in new[] { titel, text, fuss })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        Controls.AddRange([marken, BearbeitenKnopf, VerlaufKnopf]);
        AccessibleName = $"{a.Titel}: {text.Text}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Abschnitt is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        VerlaufKnopf.Location = new Point(Width - innen - VerlaufKnopf.Width, y - Stil.Px(this, Stil.AbstandMini));
        BearbeitenKnopf.Location = new Point(VerlaufKnopf.Left - Stil.Px(this, Stil.AbstandKlein) - BearbeitenKnopf.Width, VerlaufKnopf.Top);
        titel.MaximumSize = new Size(Math.Max(100, BearbeitenKnopf.Left - innen - Stil.Px(this, Stil.AbstandKlein)), 0);
        titel.Location = new Point(innen, y);
        y = Math.Max(titel.Bottom, BearbeitenKnopf.Bottom - Stil.Px(this, Stil.AbstandKlein)) + Stil.Px(this, Stil.AbstandKlein);
        var markenGroesse = marken.GetPreferredSize(new Size(breite, 0));
        marken.Bounds = new Rectangle(new Point(innen, y), markenGroesse);
        if (marken.Controls.Count > 0) y = marken.Bottom + Stil.Px(this, Stil.AbstandKlein);
        text.MaximumSize = new Size(breite, 0);
        text.Location = new Point(innen, y);
        y = text.Bottom + Stil.Px(this, Stil.AbstandKlein);
        fuss.Location = new Point(innen, y);
        int hoehe = (fuss.Text.Length > 0 ? fuss.Bottom : text.Bottom) + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Einen Abschnitt bearbeiten: Fließtext, „vom Bewohner angegeben, nicht überprüft“. Gespeichert
// wird eine neue Fassung; die alte bleibt im Verlauf.
public sealed class BiografieFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly BiografieAbschnitt abschnitt;

    public TextBox Inhalt { get; } = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 560, Height = 200, AcceptsReturn = true };
    public CheckBox Angegeben { get; } = new() { Text = "vom Bewohner angegeben, nicht überprüft", AutoSize = true };
    public Label FormulierungHinweis { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    public BiografieFenster(Speicher speicher, Bewohner bewohner, BiografieAbschnitt abschnitt)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        this.abschnitt = abschnitt;
        Text = $"Biografie: {abschnitt.Titel} – {bewohner.Name} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Inhalt.Text = abschnitt.Aktuell?.Text ?? "";
        Angegeben.Checked = abschnitt.Aktuell?.Angegeben ?? false;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = abschnitt.Titel, AutoSize = true, Font = Stil.KarteName, ForeColor = Stil.TextNormal });
        zeile(null, new Label
        {
            Text = "Als Fließtext schreiben, ohne Aufzählungszeichen. Was fehlt, bleibt leer – es erscheint dann unter „Fehlende Angaben“.",
            AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextLeise,
        });
        zeile(null, Inhalt);
        zeile(null, Angegeben);
        zeile(null, FormulierungHinweis);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Inhalt.TextChanged += (_, _) => HinweisAktualisieren();
        Angegeben.CheckedChanged += (_, _) => HinweisAktualisieren();
        Controls.Add(raster);
        HinweisAktualisieren();
        Stil.DpiSkalierungEinschalten(this);
    }

    // Hinweis auf „gibt an, dass …“ – das Programm setzt die Formulierung nicht selbst ein.
    private void HinweisAktualisieren()
    {
        var probe = new BiografieFassung { Text = Inhalt.Text, Angegeben = Angegeben.Checked };
        FormulierungHinweis.Text = $"Vom Bewohner angegeben: bitte die Formulierung „{BiografieRegeln.Formulierung}“ verwenden.";
        FormulierungHinweis.Visible = BiografieRegeln.FormulierungFehlt(probe);
    }

    private int? erwartet;   // Mehrplatz: so viele Fassungen gab es beim Öffnen

    public bool Speichern()
    {
        erwartet ??= abschnitt.Fassungen.Count;
        try
        {
            var f = BiografieRegeln.AusEingabe(abschnitt.Nummer, Inhalt.Text, Angegeben.Checked, Einstellungen.Laden().Kuerzel, DateTime.Now);
            speicher.BiografieSpeichern(bewohner.Id!.Value, f, erwartet < 0 ? null : erwartet);
        }
        catch (Konflikt k)
        {
            switch (KonfliktFenster.Fragen(this, k))
            {
                case KonfliktWahl.MeineSpeichern:
                    erwartet = -1;   // ohne Prüfung: meine Fassung wird die neueste, die andere bleibt im Verlauf
                    return Speichern();
                case KonfliktWahl.AndereUebernehmen:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return false;
                default:
                    return false;
            }
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join("\n", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
