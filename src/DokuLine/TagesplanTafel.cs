using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Tagesplan als Anzeigetafel (Bauplan Schritt 26g), nach dem Vorbild einer Abflugtafel am Flughafen:
// dunkle Fläche, helle große Schrift, Uhrzeit in fester Zeichenbreite, oben rechts groß Uhrzeit und
// Datum. Spalten: Zeit · Bewohner · Termin · Ort · Begleitung · Status. Der Status ist immer ein Wort,
// die Farbe kommt nur dazu. Der Rest des Programms bleibt hell.
public static class TafelStil
{
    public static readonly Color Flaeche = Color.FromArgb(14, 20, 27);
    public static readonly Color Zeile = Color.FromArgb(21, 29, 39);
    public static readonly Color ZeileAndere = Color.FromArgb(25, 34, 45);
    public static readonly Color Linie = Color.FromArgb(38, 49, 62);
    public static readonly Color Text = Color.FromArgb(242, 245, 248);
    public static readonly Color TextBlass = Color.FromArgb(120, 132, 147);
    public static readonly Color Spaltenkopf = Color.FromArgb(150, 162, 178);
    public static readonly Color Zeit = Color.FromArgb(255, 201, 77);   // gelb wie auf der Abflugtafel

    public static readonly Font ZeitSchrift = new("Consolas", 15F, FontStyle.Bold);
    public static readonly Font ZellenSchrift = new("Segoe UI Semibold", 13.5F);
    public static readonly Font StatusSchrift = new("Segoe UI Semibold", 11F);
    public static readonly Font KleinSchrift = new("Segoe UI", 9.5F);
    public static readonly Font KopfSchrift = new("Segoe UI Semibold", 9F);
    public static readonly Font BlockSchrift = new("Segoe UI Semibold", 10.5F);
    public static readonly Font UhrSchrift = new("Consolas", 30F, FontStyle.Bold);
    public static readonly Font DatumSchrift = new("Segoe UI", 12F);
    public static readonly Font TitelSchrift = new("Segoe UI Semibold", 16F);

    // Fläche und Schrift des Status-Worts (Kontrast mindestens 4,5:1).
    public static (Color Flaeche, Color Text) Status(TafelStatus s) => s switch
    {
        TafelStatus.Jetzt => (Color.FromArgb(25, 135, 84), Color.White),
        TafelStatus.Gleich => (Color.FromArgb(242, 184, 7), Color.FromArgb(16, 21, 28)),
        TafelStatus.Gegangen => (Color.FromArgb(47, 111, 183), Color.White),
        TafelStatus.NochOffen => (Color.FromArgb(194, 65, 12), Color.White),
        TafelStatus.Abgesagt => (Color.FromArgb(201, 42, 42), Color.White),
        TafelStatus.Ausgefallen => (Color.FromArgb(166, 30, 77), Color.White),
        TafelStatus.Zurueck => (Color.FromArgb(42, 54, 68), Color.FromArgb(143, 211, 168)),
        TafelStatus.Vorbei => (Color.FromArgb(42, 54, 68), Color.FromArgb(170, 180, 192)),
        _ => (Color.FromArgb(42, 54, 68), Color.FromArgb(214, 222, 231)),   // geplant
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

    public Label Titel { get; } = new() { AutoSize = true, Font = TafelStil.TitelSchrift, ForeColor = TafelStil.Text, BackColor = TafelStil.Flaeche, UseMnemonic = false };
    public Label Demo { get; } = new() { AutoSize = true, Text = Stil.DemoHinweis, Font = TafelStil.KleinSchrift, ForeColor = TafelStil.Zeit, BackColor = TafelStil.Flaeche };
    public Label Uhr { get; } = new() { AutoSize = true, Font = TafelStil.UhrSchrift, ForeColor = TafelStil.Zeit, BackColor = TafelStil.Flaeche };
    public Label DatumText { get; } = new() { AutoSize = true, Font = TafelStil.DatumSchrift, ForeColor = TafelStil.Text, BackColor = TafelStil.Flaeche };
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
                TextRenderer.DrawText(g, name, TafelStil.KopfSchrift, new Rectangle(r.X, 0, r.Width, spalten.Height), TafelStil.Spaltenkopf,
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
        int nr = 0;
        foreach (var z in zeilen)
        {
            string gruppe = nachBewohner ? $"{z.Bewohner.Name} · Zimmer {z.Bewohner.Zimmer}".ToUpperInvariant() : TafelRegeln.BlockText(z.Block);
            if (gruppe != bisher)
            {
                Ueberschriften.Add(gruppe);
                inhalt.Add(new TafelUeberschrift(gruppe, !nachBewohner && z.Block == TafelBlock.JetztUndGleich));
                bisher = gruppe;
                nr = 0;
            }
            var zeile = new TafelZeileAnzeige(z, nr++ % 2 == 1, grund, MenueZeigen);
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
        // Kopf: links Titel und Demo-Hinweis, rechts groß Uhr und Datum.
        Titel.Location = new Point(innen, innen);
        Demo.Location = new Point(innen, Titel.Bottom + Stil.Px(this, Stil.AbstandMini));
        Uhr.Location = new Point(Math.Max(Titel.Right + innen, breite - innen - Uhr.Width), Stil.Px(this, Stil.AbstandKlein));
        DatumText.Location = new Point(Math.Max(Titel.Right + innen, breite - innen - DatumText.Width), Uhr.Bottom);
        int kopfHoehe = Math.Max(Demo.Bottom, DatumText.Bottom) + Stil.Px(this, Stil.AbstandKlein);
        kopf.SetBounds(0, 0, breite, kopfHoehe);
        int spaltenHoehe = Stil.Px(this, 28);
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
                TafelZeileAnzeige => Stil.Px(this, 46),
                TafelUeberschrift => Stil.Px(this, 36),
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

// Überschrift eines Blocks („JETZT UND GLEICH“ mit gelbem Balken) oder eines Bewohners.
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
        var farbe = hervorheben ? TafelStil.Zeit : TafelStil.Spaltenkopf;
        using var balken = new SolidBrush(farbe);
        e.Graphics.FillRectangle(balken, innen, Height / 2 - Stil.Px(this, 8), Stil.Px(this, 4), Stil.Px(this, 16));
        TextRenderer.DrawText(e.Graphics, Text, TafelStil.BlockSchrift, new Rectangle(innen + Stil.Px(this, 12), 0, Width - innen, Height), farbe,
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

    public TafelZeileAnzeige(TafelZeile daten, bool andere, string? grund, Action<TafelZeileAnzeige> menueZeigen)
    {
        Daten = daten;
        StatusWort = TafelRegeln.Wort(daten.Status);
        Detail = TafelRegeln.Bestaetigt(daten.Zeile);
        BackColor = andere ? TafelStil.ZeileAndere : TafelStil.Zeile;
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
        Zeichnen(t.Ganztags ? "ganztags" : $"{t.Von:HH\\:mm}–{t.Bis:HH\\:mm}", TafelStil.ZeitSchrift, a.Zeit, Blass ? TafelStil.TextBlass : TafelStil.Zeit);
        Zeichnen($"{Daten.Bewohner.Name} · {Daten.Bewohner.Zimmer}", TafelStil.ZellenSchrift, a.Bewohner, text);
        Zeichnen(t.Titel, TafelStil.ZellenSchrift, a.Termin, text);
        Zeichnen(t.Ort, TafelStil.ZellenSchrift, a.Ort, text);
        Zeichnen(t.Begleitung, TafelStil.ZellenSchrift, a.Begleitung, text);
        // Status: Wort auf farbiger Fläche, daneben klein „wann · wer“.
        var (flaeche, schrift) = TafelStil.Status(Daten.Status);
        var groesse = TextRenderer.MeasureText(g, StatusWort, TafelStil.StatusSchrift);
        int pw = groesse.Width + Stil.Px(this, 14), ph = Stil.Px(this, 26);
        var pille = new Rectangle(a.Status.X, (Height - ph) / 2, pw, ph);
        using (var pinsel = new SolidBrush(flaeche)) g.FillRectangle(pinsel, pille);
        TextRenderer.DrawText(g, StatusWort, TafelStil.StatusSchrift, pille, schrift, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        if (Detail.Length > 0 && a.Detail)
            TextRenderer.DrawText(g, Detail, TafelStil.KleinSchrift, new Rectangle(pille.Right + Stil.Px(this, 6), 0, a.Status.Right - pille.Right - Stil.Px(this, 6), Height),
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
        int innen = P(Stil.AbstandGross), luecke = P(Stil.AbstandMittel), zeit = P(135), pille = P(124), detail = P(84), knopf = P(100), mindest = P(120);
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
