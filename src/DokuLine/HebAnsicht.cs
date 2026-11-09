using DokuLine.Kern;

namespace DokuLine;

// Reiter „HEB“ in der Akte: die Bögen des Bewohners (neueste oben) mit Art, Zeitraum, Nummer und
// Status; „Neuer HEB-Bogen …“ schlägt Zeitraum und Nummer vor. Ist ein HEB fällig, steht das oben.
public sealed class HebAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private Bewohner? bewohner;

    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neuer HEB-Bogen …");
    public HinweisFeld Faellig { get; } = new() { Visible = false };
    public List<HebKarte> Karten { get; } = [];
    public Label Leer { get; } = new() { Text = "Noch kein HEB-Bogen im Programm.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public HebAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        NeuKnopf.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        NeuKnopf.Width = 200;
        NeuKnopf.Click += (_, _) => Neu();
        leiste.Controls.Add(NeuKnopf);
        Controls.AddRange([leiste, Faellig, Leer]);
    }

    public void Zeigen(Bewohner b)
    {
        bewohner = speicher.Bewohner(b.Id!.Value) ?? b;
        Recht.Anwenden(speicher, Funktionen.Heb, NeuKnopf);   // Rechte
        var boegen = speicher.HebBoegen(b.Id!.Value);
        SuspendLayout();
        foreach (var k in Karten) { Controls.Remove(k); k.Dispose(); }
        Karten.Clear();
        foreach (var h in Enumerable.Reverse(boegen))
        {
            var karte = new HebKarte(h);
            karte.OeffnenKnopf.Click += (_, _) => Oeffnen(h.Id);
            karte.VerlaufKnopf.Click += (_, _) =>
            {
                using var f = new TextFenster($"Verlauf: {HebRegeln.ArtKurz(h.Art)} {HebRegeln.ZeitraumText(h)}", string.Join(Environment.NewLine, HebRegeln.Verlauf(h)));
                Dialog.Zeigen(f, FindForm());
            };
            Karten.Add(karte);
            Controls.Add(karte);
        }
        Leer.Visible = Karten.Count == 0;
        string? faellig = HebRegeln.Faellig(bewohner, boegen, DateOnly.FromDateTime(Jetzt()), Einstellungen.Laden().HebFristMonate);
        if (faellig is not null) Faellig.Setzen(faellig, Stil.Achtung, false);
        Faellig.Visible = faellig is not null;
        Faellig.Tag = faellig is not null;
        ResumeLayout(false);
        PerformLayout();
    }

    public HebBogen? Neu()
    {
        if (bewohner is null) return null;
        using var fenster = new HebNeuFenster(speicher, bewohner, DateOnly.FromDateTime(Jetzt()));
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK || fenster.Angelegt is not HebBogen neu) return null;
        Zeigen(bewohner);
        Oeffnen(neu.Id);
        return neu;
    }

    public void Oeffnen(long bogenId)
    {
        if (bewohner is null || speicher.HebBogen(bogenId) is not HebBogen h) return;
        using var fenster = new HebFenster(speicher, bewohner, h);
        Dialog.Zeigen(fenster, FindForm());
        Zeigen(bewohner);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = Math.Max(200, ClientSize.Width), abstand = Stil.Px(this, Stil.AbstandMittel);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(breite, 0)));
        int y = leiste.Bottom + Stil.Px(this, Stil.AbstandMini);
        if (Faellig.Tag is true)
        {
            Faellig.Bounds = new Rectangle(0, y, breite, Stil.Px(this, 52));
            y = Faellig.Bottom + abstand;
        }
        Leer.Location = new Point(Stil.Px(this, Stil.AbstandMini), y);
        if (Karten.Count == 0) y = Leer.Bottom + abstand;
        foreach (var k in Karten)
        {
            k.SetBounds(0, y, breite, k.Height);
            k.PerformLayout();
            y = k.Bottom + abstand;
        }
        if (Height != y) Height = y;
    }
}

public sealed class HebKarte : KartenFlaeche
{
    private readonly Label titel = new() { AutoSize = true, Font = Stil.KarteName, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label zeile = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly MarkenFeld status;

    public HebBogen Bogen { get; }
    public Button OeffnenKnopf { get; } = Stil.NebenKnopf("Öffnen …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public string StatusText => status.Text;

    public HebKarte(HebBogen h)
    {
        Bogen = h;
        titel.Text = HebRegeln.ArtKurz(h.Art) + (h.Nummer is int n ? $" Nr. {n}" : "") + $" · {HebRegeln.ZeitraumText(h)}";
        zeile.Text = h.Aktuell is HebFassung f ? $"{f.Texte.Count} Felder ausgefüllt · zuletzt {f.Erstellt:dd.MM.yyyy} von {f.Kuerzel}" : "noch leer";
        status = new MarkenFeld(h.Fertig ? "fertig" : "Entwurf", h.Fertig ? Stil.Offen : Stil.Achtung);
        Controls.AddRange([status, titel, zeile, OeffnenKnopf, VerlaufKnopf]);
        AccessibleName = $"{titel.Text}, {status.Text}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Bogen is null) return;
        int innen = Stil.Px(this, Stil.AbstandGross);
        VerlaufKnopf.Location = new Point(Width - innen - VerlaufKnopf.Width, innen);
        OeffnenKnopf.Location = new Point(VerlaufKnopf.Left - Stil.Px(this, Stil.AbstandKlein) - OeffnenKnopf.Width, innen);
        status.Location = new Point(innen, innen);
        titel.Location = new Point(status.Right + Stil.Px(this, Stil.AbstandKlein), innen - Stil.Px(this, 2));
        zeile.Location = new Point(innen, Math.Max(titel.Bottom, status.Bottom) + Stil.Px(this, Stil.AbstandKlein));
        int hoehe = Math.Max(zeile.Bottom, OeffnenKnopf.Bottom) + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Neuen Bogen anlegen: Art wählen, Zeitraum (vorgeschlagen), Nummer und Planungszeitraum sehen.
public sealed class HebNeuFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<HebBogen> fruehere;
    private readonly DateOnly heute;

    public RadioButton[] Arten { get; } = new RadioButton[3];
    public TextBox Von { get; } = new DatumFeld() { Width = 100 };
    public TextBox Bis { get; } = new DatumFeld() { Width = 100 };
    public Label Info { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.TextMittel };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public HebBogen? Angelegt { get; private set; }

    public HebNeuFenster(Speicher speicher, Bewohner bewohner, DateOnly heute)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        this.heute = heute;
        fruehere = speicher.HebBoegen(bewohner.Id!.Value);
        Text = $"Neuer HEB-Bogen – {bewohner.Name} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        var arten = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        foreach (var a in Enum.GetValues<Bogenart>())
        {
            var r = new RadioButton { Text = HebRegeln.ArtText(a), AutoSize = true };
            r.CheckedChanged += (_, _) => { if (r.Checked) Vorschlagen(); };
            Arten[(int)a] = r;
            arten.Controls.Add(r);
        }
        zeile("Bogen", arten);
        zeile("Zeitraum", Formular.Reihe(Von, new Label { Text = "bis", AutoSize = true, Margin = new Padding(0, Stil.AbstandMini, 0, 0) }, Bis));
        zeile(null, Info);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Anlegen", () => Anlegen()));
        Controls.Add(raster);
        Arten[fruehere.Count == 0 ? 0 : 1].Checked = true;
        Stil.DpiSkalierungEinschalten(this);
    }

    public Bogenart Art => (Bogenart)Array.FindIndex(Arten, r => r.Checked);

    private void Vorschlagen()
    {
        var (von, bis) = HebRegeln.Zeitraum(Art, bewohner, fruehere, heute);
        Von.Text = Datum.Text(von);
        Bis.Text = Datum.Text(bis);
        var teile = new List<string>();
        if (HebRegeln.Nummer(Art, fruehere) is int n) teile.Add($"Laufende Nummer: {n}");
        if (Art != Bogenart.C) teile.Add(HebRegeln.PlanungText(HebRegeln.Planungsmonate(bewohner, heute)));
        if (Art == Bogenart.B && HebRegeln.Fortschreiben(Art, fruehere.Where(x => x.Fertig).OrderBy(x => x.Bis).LastOrDefault()).Count > 0)
            teile.Add("Rahmenziele, Leitziel und Nahziele des letzten Bogens werden zum Fortschreiben vorgelegt.");
        Info.Text = string.Join(Environment.NewLine, teile);
    }

    public bool Anlegen()
    {
        try
        {
            DateOnly von, bis;
            try
            {
                von = Datum.Lesen(Von.Text, "Beginn");
                bis = Datum.Lesen(Bis.Text, "Ende");
            }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            Angelegt = speicher.HebAnlegen(bewohner.Id!.Value, Art, von, bis, Recht.Kuerzel);
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

// Zuordnung „Programmfeld → PDF-Feld“ für eine Bogenart. Vorgabe: gleicher Name, wenn es ihn gibt.
public sealed class HebZuordnungFenster : Form
{
    private readonly Bogenart art;

    public DataGridView Tabelle { get; } = new()
    {
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, Width = 640, Height = 460,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Stil.Flaeche,
    };
    public IReadOnlyList<string> PdfFelder { get; }

    public HebZuordnungFenster(Bogenart art)
    {
        this.art = art;
        Text = $"Zuordnung {HebRegeln.ArtKurz(art)}: Programmfeld → PDF-Feld ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        PdfFelder = HebPdf.Vorlage(art) is string v ? HebPdf.Felder(v) : [];
        var zuordnung = HebPdf.Zuordnung(art);
        Tabelle.Columns.Add(new DataGridViewTextBoxColumn { Name = "programm", HeaderText = "Programmfeld", ReadOnly = true });
        var spalte = new DataGridViewComboBoxColumn { Name = "pdf", HeaderText = "PDF-Feld", FlatStyle = FlatStyle.Flat };
        spalte.Items.Add("");
        foreach (var f in PdfFelder) spalte.Items.Add(f);
        Tabelle.Columns.Add(spalte);
        foreach (var feld in HebRegeln.AlleProgrammfelder(art))
            Tabelle.Rows.Add(feld, HebPdf.PdfFeld(feld, zuordnung, PdfFelder) ?? "");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label
        {
            Text = PdfFelder.Count == 0 ? "Keine Vorlage zugeteilt – erst in den Einstellungen eine PDF zuteilen."
                : $"{PdfFelder.Count} Felder in der Vorlage. Leer = dieses Programmfeld wird nicht übertragen.",
            AutoSize = true, MaximumSize = new Size(640, 0), ForeColor = Stil.TextMittel,
        });
        zeile(null, Tabelle);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        var neu = new Dictionary<string, string>();
        foreach (DataGridViewRow r in Tabelle.Rows)
            if (r.Cells[0].Value is string programmfeld) neu[programmfeld] = r.Cells[1].Value as string ?? "";
        HebPdf.ZuordnungSpeichern(art, neu);
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
