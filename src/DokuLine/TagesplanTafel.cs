using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Tagesplan im Stil des übrigen Programms: helle Fläche, normale Schrift und Zeilenhöhe.
// Spalten: Zeit · Bewohner · Termin · Ort · Begleitung · Status. Der Status ist immer ein Wort, die Farbe kommt dazu.
public static class TafelStil
{
    public static Color Flaeche => Stil.Flaeche;
    public static Color Text => Stil.TextNormal;
    public static Color TextBlass => Stil.TextLeise;
    public static Color Linie => Stil.Trennlinie;

    public static Font ZeitSchrift => Stil.Fett;
    public static Font ZellenSchrift => Stil.Standard;
    public static Font KleinSchrift => Stil.Klein;
    public static Font KopfSchrift => Stil.Abschnitt;

    // Dieselben Farben wie in der Übersicht und bei den laufenden Therapien.
    public static Stil.Marke Status(TafelStatus s) => s switch
    {
        TafelStatus.Gegangen or TafelStatus.Zurueck => Stil.Offen,
        TafelStatus.NochOffen => Stil.Achtung,
        TafelStatus.Abgesagt or TafelStatus.Ausgefallen => Stil.Beschluss,
        TafelStatus.Jetzt or TafelStatus.Gleich => Stil.Unterwegs,
        _ => Stil.Zimmer,
    };
}

// Die Tafel selbst: Kopf (Titel, Demo-Hinweis, Uhr, Datum), Spaltenköpfe, Blöcke und Zeilen.
// Sie zeigt nur an; was eine Bestätigung bewirkt, sagt die Seite über „Setzen“.
public sealed class AnzeigeTafel : Panel
{
    private readonly Panel kopf = new() { BackColor = TafelStil.Flaeche };
    private readonly Panel spalten = new() { BackColor = TafelStil.Flaeche };
    private readonly Panel liste = new() { AutoScroll = true, BackColor = TafelStil.Flaeche };
    private readonly List<Control> inhalt = [];
    private readonly ToolTip tipp = new();
    private bool nachBewohner;

    public Label Titel { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = TafelStil.Text, BackColor = TafelStil.Flaeche, UseMnemonic = false };
    public Label Demo { get; } = new() { AutoSize = true, Text = Stil.DemoHinweis, Font = TafelStil.KleinSchrift, ForeColor = Stil.Hinweis.Text, BackColor = TafelStil.Flaeche };
    public Label Uhr { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = TafelStil.Text, BackColor = TafelStil.Flaeche };
    public Label DatumText { get; } = new() { AutoSize = true, Font = Stil.Standard, ForeColor = TafelStil.TextBlass, BackColor = TafelStil.Flaeche };
    public List<TafelZeileAnzeige> Zeilen { get; } = [];
    public List<string> Ueberschriften { get; } = [];   // „JETZT UND GLEICH“ … oder je Bewohner
    // Ein Menü für alle Zeilen („gegangen“, „abgesagt“, „ausgefallen“); es gehört der Tafel und
    // überlebt das Neu-Füllen. Gewaehlt = die Zeile, deren „bestätigen ▾“ es geöffnet hat.
    public ContextMenuStrip Menue { get; } = new();
    public TafelZeileAnzeige? Gewaehlt { get; set; }
    private Action<TagesplanZeile, Teilnahme>? setzen;
    // Aufteilung der Spalten bei der aktuellen Breite (für Köpfe und Zeilen dieselbe).
    public TafelAufteilung Aufteilung { get; private set; } = TafelAufteilung.Leer;

    public AnzeigeTafel()
    {
        BackColor = TafelStil.Flaeche;
        DoubleBuffered = true;
        foreach (var (text, status) in new[] { ("gegangen", Teilnahme.Teilgenommen), ("abgesagt", Teilnahme.Abgesagt), ("ausgefallen", Teilnahme.Ausgefallen) })
            Menue.Items.Add(new ToolStripMenuItem(text, null, (_, _) => Bestaetigen(status)));
        kopf.Controls.AddRange([Titel, Demo, Uhr, DatumText]);
        spalten.Paint += (_, e) => SpaltenkoepfeZeichnen(e.Graphics);
        liste.Layout += (_, _) => ListeAnordnen();
        Controls.AddRange([liste, spalten, kopf]);
    }

    // Erst nach dem Schließen des Menüs ausführen: Das Neu-Füllen ersetzt auch den Knopf, an dem es hing.
    private void Bestaetigen(Teilnahme status)
    {
        if (Gewaehlt?.Daten.Zeile is not TagesplanZeile z || setzen is not { } aktion) return;
        Gewaehlt = null;
        if (IsHandleCreated) BeginInvoke(() => aktion(z, status));
        else aktion(z, status);
    }

    internal void MenueZeigen(TafelZeileAnzeige zeile)
    {
        Gewaehlt = zeile;
        Menue.Show(zeile.BestaetigenKnopf!, new Point(0, zeile.BestaetigenKnopf!.Height));
    }

    private void SpaltenkoepfeZeichnen(Graphics g)
    {
        var a = Aufteilung;
        foreach (var (name, r) in new[] { ("ZEIT", a.Zeit), ("BEWOHNER", a.Bewohner), ("TERMIN", a.Termin), ("ORT", a.Ort), ("BEGLEITUNG", a.Begleitung), ("STATUS", a.Status) })
            if (r.Width > 0)
                TextRenderer.DrawText(g, name, TafelStil.KopfSchrift, new Rectangle(r.X, 0, r.Width, spalten.Height), TafelStil.TextBlass,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        using var stift = new Pen(TafelStil.Linie);
        g.DrawLine(stift, 0, spalten.Height - 1, spalten.Width, spalten.Height - 1);
    }

    public void UhrSetzen(DateTime jetzt)
    {
        Uhr.Text = jetzt.ToString("HH:mm", CultureInfo.InvariantCulture);
        DatumText.Text = jetzt.ToString("dddd, dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
        PerformLayout();
    }

    // Neu füllen: „Blöcke“ = Überschriften „Jetzt und gleich“, „Später heute“, „Erledigt“; sonst je Bewohner.
    public void Fuellen(string titel, IReadOnlyList<TafelZeile> zeilen, bool nachBewohner, DateTime jetzt, string? grund, Action<TagesplanZeile, Teilnahme> setzen)
    {
        Titel.Text = titel;
        this.setzen = setzen;
        this.nachBewohner = nachBewohner;
        Gewaehlt = null;
        UhrSetzen(jetzt);
        var scroll = liste.AutoScrollPosition;
        liste.SuspendLayout();
        tipp.RemoveAll();
        foreach (var c in inhalt) { liste.Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Zeilen.Clear();
        Ueberschriften.Clear();
        string? bisher = null;
        foreach (var z in zeilen)
        {
            string gruppe = nachBewohner ? $"{z.Bewohner.Name} · Zimmer {z.Bewohner.Zimmer}".ToUpperInvariant() : TafelRegeln.BlockText(z.Block);
            if (gruppe != bisher)
            {
                Ueberschriften.Add(gruppe);
                inhalt.Add(new TafelUeberschrift(gruppe, !nachBewohner && z.Block == TafelBlock.JetztUndGleich));
                bisher = gruppe;
            }
            var zeile = new TafelZeileAnzeige(z, grund, MenueZeigen);
            tipp.SetToolTip(zeile, zeile.AccessibleName);   // die ganze Zeile, falls Spalten gekürzt sind
            Zeilen.Add(zeile);
            inhalt.Add(zeile);
        }
        if (zeilen.Count == 0)
            inhalt.Add(new Label { Text = "Heute stehen keine Termine an.", AutoSize = true, Font = TafelStil.ZellenSchrift, ForeColor = TafelStil.TextBlass, BackColor = TafelStil.Flaeche });
        liste.Controls.AddRange([.. inhalt]);
        liste.ResumeLayout(false);
        liste.PerformLayout();
        liste.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { Menue.Dispose(); tipp.Dispose(); }
        base.Dispose(disposing);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross);
        int breite = ClientSize.Width;
        // Kopf: links Titel und Demo-Hinweis, rechts Uhr und Datum in einer Zeile.
        int oben = Stil.Px(this, Stil.AbstandMittel);
        Titel.Location = new Point(innen, oben);
        Demo.Location = new Point(Titel.Right + Stil.Px(this, Stil.AbstandKlein), oben + (Titel.Height - Demo.Height) / 2);
        Uhr.Location = new Point(Math.Max(Demo.Right + innen, breite - innen - Uhr.Width), oben);
        DatumText.Location = new Point(Math.Max(Demo.Right + innen, Uhr.Left - Stil.Px(this, Stil.AbstandKlein) - DatumText.Width),
            oben + (Uhr.Height - DatumText.Height) / 2);
        int kopfHoehe = Math.Max(Titel.Bottom, Uhr.Bottom) + Stil.Px(this, Stil.AbstandKlein);
        kopf.SetBounds(0, 0, breite, kopfHoehe);
        int spaltenHoehe = Stil.Px(this, 24);
        spalten.SetBounds(0, kopfHoehe, breite, spaltenHoehe);
        spalten.Invalidate();
        liste.SetBounds(0, kopfHoehe + spaltenHoehe, breite, Math.Max(0, ClientSize.Height - kopfHoehe - spaltenHoehe));
    }

    // Breite der Zeilen: ohne Platz für die senkrechte Bildlaufleiste, ob sie gerade da ist oder nicht –
    // so gibt es keine waagerechte Leiste und die Köpfe stehen über denselben Spalten.
    private int Zeilenbreite() => Math.Max(0, liste.Width - SystemInformation.VerticalScrollBarWidth);

    private void ListeAnordnen()
    {
        int breite = Zeilenbreite();
        Aufteilung = TafelAufteilung.Berechnen(this, breite, nachBewohner);
        spalten.Invalidate();
        int y = liste.AutoScrollPosition.Y + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            int hoehe = c switch
            {
                TafelZeileAnzeige => Stil.Px(this, Stil.KnopfHoehe),
                TafelUeberschrift => Stil.Px(this, 30),
                _ => c.PreferredSize.Height,
            };
            if (c is Label leer) leer.Location = new Point(Stil.Px(this, Stil.AbstandGross), y + Stil.Px(this, Stil.AbstandKlein));
            else
            {
                if (c is TafelZeileAnzeige z && z.Aufteilung != Aufteilung) { z.Aufteilung = Aufteilung; z.Invalidate(); z.PerformLayout(); }
                c.SetBounds(0, y, breite, hoehe);
            }
            y += hoehe + (c is Label ? Stil.Px(this, Stil.AbstandGross) : 0);
        }
        liste.AutoScrollMinSize = new Size(0, y - liste.AutoScrollPosition.Y + Stil.Px(this, Stil.AbstandGross));
    }
}

// Überschrift eines Blocks („JETZT UND GLEICH“ mit Balken in Akzentfarbe) oder eines Bewohners.
public sealed class TafelUeberschrift : Control
{
    private readonly bool hervorheben;

    public TafelUeberschrift(string text, bool hervorheben)
    {
        Text = text;
        this.hervorheben = hervorheben;
        BackColor = TafelStil.Flaeche;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = text;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int innen = Stil.Px(this, Stil.AbstandGross);
        var farbe = hervorheben ? Stil.Akzent : TafelStil.TextBlass;
        using var balken = new SolidBrush(farbe);
        e.Graphics.FillRectangle(balken, innen, Height / 2 - Stil.Px(this, 8), Stil.Px(this, 4), Stil.Px(this, 16));
        TextRenderer.DrawText(e.Graphics, Text, TafelStil.KopfSchrift, new Rectangle(innen + Stil.Px(this, 12), 0, Width - innen, Height), farbe,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

// Eine Zeile der Tafel. Gezeichnet (nicht aus Einzelfeldern), damit die Spalten genau untereinander
// stehen. Bei Therapien rechts „bestätigen ▾“ mit „gegangen“, „abgesagt“, „ausgefallen“.
public sealed class TafelZeileAnzeige : Control
{
    public TafelZeile Daten { get; }
    public string StatusWort { get; }
    public string Detail { get; }   // „09:05 · CD“
    public bool Blass => Daten.Block == TafelBlock.Erledigt;
    public Button? BestaetigenKnopf { get; }
    public TafelAufteilung Aufteilung { get; set; } = TafelAufteilung.Leer;

    public TafelZeileAnzeige(TafelZeile daten, string? grund, Action<TafelZeileAnzeige> menueZeigen)
    {
        Daten = daten;
        StatusWort = TafelRegeln.Wort(daten.Status);
        Detail = TafelRegeln.Bestaetigt(daten.Zeile);
        BackColor = TafelStil.Flaeche;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        var t = daten.Termin;
        AccessibleRole = AccessibleRole.Row;
        AccessibleName = $"{t.ZeitText}, {daten.Bewohner.Name}, Zimmer {daten.Bewohner.Zimmer}, {t.Titel}" + (t.Ort.Length > 0 ? $", {t.Ort}" : "") +
                         (t.Begleitung.Length > 0 ? $", {t.Begleitung}" : "") + $", {StatusWort}" + (Detail.Length > 0 ? $" {Detail}" : "");
        if (!daten.Zeile.Therapie) return;
        BestaetigenKnopf = Stil.NebenKnopf("bestätigen ▾");
        BestaetigenKnopf.Font = Stil.Klein;
        BestaetigenKnopf.AccessibleName = $"Teilnahme bestätigen: {daten.Bewohner.Name}, {t.Titel}";
        BestaetigenKnopf.Click += (_, _) => menueZeigen(this);
        Stil.Sperren(BestaetigenKnopf, grund);
        Controls.Add(BestaetigenKnopf);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (BestaetigenKnopf is null) return;
        var r = Aufteilung.Knopf;
        int hoehe = Math.Min(Height - Stil.Px(this, 8), Stil.Px(this, 32));
        BestaetigenKnopf.SetBounds(r.X, (Height - hoehe) / 2, r.Width, hoehe);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var a = Aufteilung;
        var t = Daten.Termin;
        var text = Blass ? TafelStil.TextBlass : TafelStil.Text;
        const TextFormatFlags Zelle = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
        void Zeichnen(string inhalt, Font schrift, Rectangle spalte, Color farbe)
        {
            if (spalte.Width > 0) TextRenderer.DrawText(g, inhalt, schrift, new Rectangle(spalte.X, 0, spalte.Width, Height), farbe, Zelle);
        }
        Zeichnen(t.Ganztags ? "ganztags" : $"{t.Von:HH\\:mm}–{t.Bis:HH\\:mm}", TafelStil.ZeitSchrift, a.Zeit, text);
        Zeichnen($"{Daten.Bewohner.Name} · {Daten.Bewohner.Zimmer}", TafelStil.ZellenSchrift, a.Bewohner, text);
        Zeichnen(t.Titel, TafelStil.ZellenSchrift, a.Termin, text);
        Zeichnen(t.Ort, TafelStil.ZellenSchrift, a.Ort, text);
        Zeichnen(t.Begleitung, TafelStil.ZellenSchrift, a.Begleitung, text);
        // Status: Wort als Marke (wie überall im Programm), daneben klein „wann · wer“.
        float skala = Stil.Skala(this);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int pw = Stil.MarkeZeichnen(g, StatusWort, a.Status.X, (Height - Stil.Px(this, 22)) / 2, TafelStil.Status(Daten.Status), skala);
        int rechts = a.Status.X + pw + Stil.Px(this, 6);
        if (Detail.Length > 0 && a.Detail)
            TextRenderer.DrawText(g, Detail, TafelStil.KleinSchrift, new Rectangle(rechts, 0, a.Status.Right - rechts, Height),
                TafelStil.TextBlass, Zelle);
        using var stift = new Pen(TafelStil.Linie);
        g.DrawLine(stift, 0, Height - 1, Width, Height - 1);
    }
}

// Spalten der Tafel bei einer Breite. Schmal wird zuerst der Zusatz „wann · wer“ weggelassen, dann
// „Begleitung“, dann „Ort“; „nach Bewohner“ entfällt die Spalte „Bewohner“ (der Name steht in der
// Überschrift). Weggelassene Spalten haben die Breite 0; die ganze Zeile steht im Hinweis.
public sealed record TafelAufteilung(Rectangle Zeit, Rectangle Bewohner, Rectangle Termin, Rectangle Ort, Rectangle Begleitung,
                                     Rectangle Status, Rectangle Knopf, bool Detail)
{
    public static readonly TafelAufteilung Leer = new(default, default, default, default, default, default, default, false);

    public int Textspalten => new[] { Bewohner, Termin, Ort, Begleitung }.Count(r => r.Width > 0);

    public static TafelAufteilung Berechnen(Control bezug, int breite, bool nachBewohner)
    {
        int P(int wert) => Stil.Px(bezug, wert);
        int innen = P(Stil.AbstandGross), luecke = P(Stil.AbstandMittel), zeit = P(100), pille = P(100), detail = P(84), knopf = P(110), mindest = P(110);
        // Textspalten nach Wichtigkeit, mit Anteil an der freien Breite.
        var spalten = new List<(string Name, int Anteil)> { ("bewohner", 28), ("termin", 34), ("ort", 21), ("begleitung", 17) };
        if (nachBewohner) spalten.RemoveAt(0);
        int Fest(int n, bool mitDetail) => 2 * innen + zeit + pille + (mitDetail ? luecke + detail : 0) + knopf + (n + 2) * luecke;
        int anzahl = spalten.Count, wenigstens = Math.Min(2, spalten.Count);
        bool mitDetail = true;
        while (Fest(anzahl, mitDetail) + anzahl * mindest > breite && (mitDetail || anzahl > wenigstens))
        {
            if (mitDetail) mitDetail = false;
            else anzahl--;
        }
        int frei = Math.Max(anzahl * P(40), breite - Fest(anzahl, mitDetail));
        int summe = spalten.Take(anzahl).Sum(s => s.Anteil);
        var r = new Dictionary<string, Rectangle>();
        int x = innen;
        var zeitR = new Rectangle(x, 0, zeit, 0);
        x += zeit + luecke;
        // Jede Spalte bekommt erst die Mindestbreite, der Rest wird nach Anteil verteilt.
        int basis = Math.Min(mindest, frei / anzahl), extra = frei - basis * anzahl, rest = frei;
        for (int i = 0; i < anzahl; i++)
        {
            int w = i == anzahl - 1 ? rest : basis + extra * spalten[i].Anteil / summe;
            r[spalten[i].Name] = new Rectangle(x, 0, w, 0);
            x += w + luecke;
            rest -= w;
        }
        var status = new Rectangle(x, 0, pille + (mitDetail ? luecke + detail : 0), 0);
        x = status.Right + luecke;
        return new TafelAufteilung(zeitR, r.GetValueOrDefault("bewohner"), r.GetValueOrDefault("termin"), r.GetValueOrDefault("ort"),
            r.GetValueOrDefault("begleitung"), status, new Rectangle(x, 0, knopf, 0), mitDetail);
    }
}

// Vollbild: nur die Tafel, z. B. für einen Bildschirm im Dienstzimmer. Esc (oder F11) beendet.
public sealed class TafelVollbild : Form
{
    public AnzeigeTafel Tafel { get; } = new() { Dock = DockStyle.Fill };

    public TafelVollbild()
    {
        Text = $"Tagesplan – Vollbild ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.Manual;
        BackColor = TafelStil.Flaeche;
        ShowInTaskbar = false;
        KeyPreview = true;
        Controls.Add(Tafel);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.Escape or Keys.F11) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
