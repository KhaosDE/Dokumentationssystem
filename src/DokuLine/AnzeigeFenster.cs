using DokuLine.Kern;

namespace DokuLine;

// Einstellungen der Anzeige für den Fernseher (dieser Rechner): an/aus, Port, Namen, Zugangscode je Wohnbereich
// mit der Adresse für den Fernseher. Speichern nur mit dem Recht „Anzeige für den Fernseher verwalten“.
public sealed class AnzeigeFenster : Form
{
    private readonly Speicher speicher;
    private readonly AnzeigeDienst? dienst;
    private readonly Dictionary<string, string> codes;
    private readonly TableLayoutPanel adressen = new() { ColumnCount = 3, AutoSize = true, Margin = new Padding(0) };

    public CheckBox AnKnopf { get; } = new() { Text = "Anzeige im lokalen Netzwerk einschalten", AutoSize = true };
    public TextBox PortFeld { get; } = new() { Width = 80 };
    public RadioButton KurzerName { get; } = new() { Text = "Vorname und erster Buchstabe des Nachnamens (Vorgabe)", AutoSize = true };
    public RadioButton VollerName { get; } = new() { Text = "voller Name", AutoSize = true };
    public Dictionary<string, TextBox> Adressen { get; } = [];
    public Button SpeichernKnopf { get; private set; } = null!;
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    public AnzeigeFenster(Speicher speicher, AnzeigeDienst? dienst)
    {
        this.speicher = speicher;
        this.dienst = dienst;
        Text = $"Anzeige für den Fernseher ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var werte = AnzeigeEinstellungen.MitCodes(AnzeigeEinstellungen.Laden(), speicher.Wohnbereiche().Select(w => w.Name));
        codes = new Dictionary<string, string>(werte.Codes);
        AnKnopf.Checked = werte.An;
        PortFeld.Text = werte.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        (werte.VollerName ? VollerName : KurzerName).Checked = true;

        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label
        {
            Text = "Eine nur lesende Seite mit den laufenden Therapien je Wohnbereich für einen Fernseher (Browser) im eigenen " +
                   "Netz – ohne Zimmer, Marken, Bezugspflege und Teilnahmestatus. Nur mit Zugangscode, nur aus dem lokalen Netz. " +
                   "Beim ersten Einschalten fragt die Windows-Firewall nach: „Private Netzwerke“ erlauben. Vor dem Echtbetrieb " +
                   "mit Leitung und Datenschutz klären – auf dem Fernseher lesen auch Besucher mit.",
            AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextMittel, Margin = new Padding(0, 0, 0, Stil.AbstandMittel),
        });
        zeile(null, AnKnopf);
        zeile("Port", PortFeld);
        var namen = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        namen.Controls.AddRange([KurzerName, VollerName]);
        zeile("Namen", namen);
        AdressenFuellen();
        zeile("Adressen", adressen);
        string lan = string.Join(", ", AnzeigeServer.EigeneAdressen());
        zeile(null, new Label
        {
            Text = $"Statt des Rechnernamens geht auch seine Adresse im Netz{(lan.Length > 0 ? $": {lan}" : "")}.",
            AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        SpeichernKnopf = knoepfe.Controls.OfType<Button>().First(k => k.Text == "Speichern");
        Stil.Sperren(SpeichernKnopf, Recht.Grund(speicher, Funktionen.AnzeigeVerwalten));
        zeile(null, knoepfe);
        PortFeld.TextChanged += (_, _) => AdressenFuellen();
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private int PortOderVorgabe => int.TryParse(PortFeld.Text.Trim(), out int p) ? p : AnzeigeRegeln.StandardPort;

    private void AdressenFuellen()
    {
        adressen.SuspendLayout();
        foreach (Control c in adressen.Controls.Cast<Control>().ToList()) { adressen.Controls.Remove(c); c.Dispose(); }
        Adressen.Clear();
        foreach (var (bereich, code) in codes.OrderBy(x => x.Key, StringComparer.CurrentCulture))
        {
            var feld = new TextBox { ReadOnly = true, Width = 380, Text = AnzeigeServer.Adresse(Environment.MachineName, PortOderVorgabe, bereich, code), AccessibleName = $"Adresse {bereich}" };
            var neu = Stil.KleinerKnopf("Neuer Code");
            neu.AccessibleName = $"Neuer Code für {bereich}";
            string b = bereich;
            neu.Click += (_, _) => CodeNeu(b);
            Adressen[bereich] = feld;
            adressen.Controls.Add(new Label { Text = bereich, AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) });
            adressen.Controls.Add(feld);
            adressen.Controls.Add(neu);
        }
        adressen.ResumeLayout();
    }

    // Der alte Code gilt nach dem Speichern nicht mehr (z. B. wenn er weitergegeben wurde).
    public void CodeNeu(string bereich)
    {
        codes[bereich] = AnzeigeRegeln.CodeErzeugen();
        AdressenFuellen();
    }

    public bool Speichern()
    {
        if (Stil.Sperrgrund(SpeichernKnopf) is not null) return false;
        try
        {
            if (!int.TryParse(PortFeld.Text.Trim(), out int port)) throw new Eingabefehler([AnzeigeRegeln.PortPruefen(0)!]);
            AnzeigeEinstellungen.Speichern(new AnzeigeWerte { An = AnKnopf.Checked, Port = port, VollerName = VollerName.Checked, Codes = codes });
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join("\n", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        dienst?.Anwenden();
        if (dienst?.Fehler is string fehler)
        {
            Fehler.Text = fehler;   // gespeichert, aber nicht gestartet: Port ändern und erneut speichern
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Dieselbe Anzeige als Vollbild auf einem Bildschirm (z. B. über HDMI am Fernseher): Esc oder Doppelklick schließt.
public sealed class AnzeigeVollbild : Form
{
    private readonly Speicher speicher;
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 30_000 };
    private readonly bool vollerName;
    public string Bereich { get; }
    public List<AnzeigeTermin> Termine { get; private set; } = [];
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AnzeigeVollbild(Speicher speicher, string bereich, bool vollerName, Screen bildschirm)
    {
        this.speicher = speicher;
        this.vollerName = vollerName;
        Bereich = bereich;
        Text = $"Therapien {bereich} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bildschirm.Bounds;
        BackColor = Stil.Seite;
        DoubleBuffered = true;
        ResizeRedraw = true;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        DoubleClick += (_, _) => Close();
        uhr.Tick += (_, _) => Aktualisieren();
        Aktualisieren();
        uhr.Start();
    }

    // Lieber ein zweiter Bildschirm (Fernseher an HDMI), sonst der, auf dem das Programm steht.
    public static Screen Bildschirm(Form programm) =>
        Screen.AllScreens.FirstOrDefault(s => !s.Primary) ?? Screen.FromControl(programm);

    public void Aktualisieren()
    {
        try { Termine = AnzeigeDienst.TagFuer(speicher, Bereich, vollerName, Jetzt()); }
        catch (SpeicherFehler) { }   // Datenbank gerade nicht lesbar: beim nächsten Mal
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        float einheit = Math.Max(8f, ClientSize.Height / 40f);
        using var titel = new Font(Stil.Standard.FontFamily, einheit * 1.6f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var gross = new Font(Stil.Standard.FontFamily, einheit * 1.3f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var normal = new Font(Stil.Standard.FontFamily, einheit, FontStyle.Regular, GraphicsUnit.Pixel);
        int rand = (int)(einheit * 1.5f), breite = ClientSize.Width - 2 * rand;
        var jetzt = Jetzt();
        var uhrzeit = TimeOnly.FromDateTime(jetzt);
        TextRenderer.DrawText(g, $"Therapien · {Bereich}", titel, new Point(rand, rand), Stil.TextNormal, TextFormatFlags.NoPrefix);
        string zeit = $"{jetzt:HH:mm} Uhr";
        var zeitGroesse = TextRenderer.MeasureText(zeit, titel);
        TextRenderer.DrawText(g, zeit, titel, new Point(ClientSize.Width - rand - zeitGroesse.Width, rand), Stil.Akzent, TextFormatFlags.NoPrefix);
        int y = rand + titel.Height + (int)einheit;
        using (var linie = new Pen(Stil.Akzent, Math.Max(2f, einheit / 6f))) g.DrawLine(linie, rand, y, rand + breite, y);
        y += (int)einheit;
        foreach (var (abschnitt, termine, leer) in new[]
        {
            ("JETZT", AnzeigeRegeln.Jetzt(Termine, uhrzeit), "Gerade läuft keine Therapie."),
            ("GLEICH", AnzeigeRegeln.Gleich(Termine, uhrzeit), "In der nächsten Stunde beginnt keine."),
        })
        {
            TextRenderer.DrawText(g, abschnitt, normal, new Point(rand, y), Stil.TextLeise, TextFormatFlags.NoPrefix);
            y += normal.Height + (int)(einheit / 2);
            if (termine.Count == 0)
            {
                TextRenderer.DrawText(g, leer, normal, new Point(rand, y), Stil.TextLeise, TextFormatFlags.NoPrefix);
                y += normal.Height + (int)einheit;
            }
            foreach (var t in termine) y = Karte(g, t, rand, y, breite, einheit, gross, normal) + (int)(einheit / 2);
            y += (int)einheit;
        }
        TextRenderer.DrawText(g, Stil.DemoHinweis, normal, new Point(rand, ClientSize.Height - rand - normal.Height), Stil.TextLeise, TextFormatFlags.NoPrefix);
    }

    private static int Karte(Graphics g, AnzeigeTermin t, int x, int y, int breite, float einheit, Font gross, Font normal)
    {
        int innen = (int)einheit;
        string wer = string.Join(", ", t.Namen);
        var werGroesse = TextRenderer.MeasureText(g, wer, normal, new Size(breite - 2 * innen, 0), TextFormatFlags.WordBreak);
        int hoehe = innen + gross.Height + normal.Height + werGroesse.Height + innen;
        var flaeche = new Rectangle(x, y, breite, hoehe);
        using (var pfad = Stil.Abgerundet(flaeche, einheit / 2))
        {
            using var grund = new SolidBrush(Stil.Flaeche);
            using var rand = new Pen(Stil.KarteRand);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath(grund, pfad);
            g.DrawPath(rand, pfad);
        }
        int ty = y + innen;
        TextRenderer.DrawText(g, t.Name, gross, new Point(x + innen, ty), Stil.TextNormal, TextFormatFlags.NoPrefix);
        ty += gross.Height;
        TextRenderer.DrawText(g, t.Ort.Length > 0 ? $"{t.Zeit} · {t.Ort}" : t.Zeit, normal, new Point(x + innen, ty), Stil.TextLeise, TextFormatFlags.NoPrefix);
        ty += normal.Height;
        TextRenderer.DrawText(g, wer, normal, new Rectangle(x + innen, ty, breite - 2 * innen, werGroesse.Height), Stil.TextNormal,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        return y + hoehe;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) uhr.Dispose();
        base.Dispose(disposing);
    }
}
