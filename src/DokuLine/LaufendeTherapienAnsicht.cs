using System.Drawing.Drawing2D;
using DokuLine.Kern;

namespace DokuLine;

// Therapie, Reiter „Laufende Therapien“ (Bauplan Schritt 26m): oben die Tagesleiste 7–21 Uhr mit allen
// Angeboten des Tages und einem Strich für „jetzt“; darunter je laufender Therapie eine Spalte mit den
// Teilnehmern (Bereich und Zimmer, Name, Bezugspflege, Status als Wort mit Farbe), daneben „Gleich“ und
// „Noch offen“ (mit „gegangen“/„abgesagt“); unten der Rest des Tages. Bestätigt wird derselbe Eintrag wie
// im Tagesplan und im Wochenplan. Aktualisiert sich jede Minute.
public sealed class LaufendeTherapienAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 60_000 };
    private readonly Label bereichLabel = new() { Text = "Bereich", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite };
    private readonly FlowLayoutPanel spaltenFlaeche = new() { WrapContents = false, AutoScroll = true, FlowDirection = FlowDirection.LeftToRight, BackColor = Stil.Seite };
    private readonly Label restTitel = new() { Text = "REST DES TAGES", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };
    private string? vorgabeFuer;

    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = Stil.Standard, AccessibleName = "Bereich" };
    public Label Titel { get; } = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, UseMnemonic = false };
    public Tagesleiste Leiste { get; } = new();
    public Label Hinweis { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false, Visible = false };
    public Label Rest { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
    public List<TherapieSpalte> Spalten { get; } = [];
    public LaufendeTafel? Tafel { get; private set; }
    public const string GleichTitel = "GLEICH", NochOffenTitel = "NOCH OFFEN";
    public event Action<Angebot>? AngebotGewaehlt;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public LaufendeTherapienAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        BereichWahl.Items.Add(LaufendeRegeln.AlleBereiche);
        foreach (var w in speicher.Wohnbereiche()) BereichWahl.Items.Add(w.Name);
        BereichWahl.SelectedIndex = 0;
        BereichWahl.SelectedIndexChanged += (_, _) => Aktualisieren();
        Leiste.TerminGeklickt += t => AngebotGewaehlt?.Invoke(t.Angebot);
        Controls.AddRange([Titel, bereichLabel, BereichWahl, Leiste, Hinweis, spaltenFlaeche, restTitel, Rest]);
        uhr.Tick += (_, _) => { if (Visible) Aktualisieren(); };
        uhr.Start();
    }

    // Beim Öffnen: Vorgabe der Bereichswahl für den Angemeldeten (Wohnbereich oder alle), dann neu lesen.
    public void Zeigen()
    {
        var werte = Einstellungen.Laden();
        if (vorgabeFuer != werte.Kuerzel)
        {
            vorgabeFuer = werte.Kuerzel;
            var eigene = speicher.MitarbeiterLesen(werte.Kuerzel)?.Bereiche is { Count: > 0 } b ? b : [werte.Bereich];
            BereichWahl.SelectedItem = LaufendeRegeln.Vorgabe(eigene, speicher.AlleBewohner());
        }
        Aktualisieren();
    }

    public string? Bereich => BereichWahl.SelectedItem is string w && w != LaufendeRegeln.AlleBereiche ? w : null;

    public string KopfText => Tafel is null ? "Laufende Therapien"
        : $"{Bereich ?? LaufendeRegeln.AlleBereiche} · {Tafel.Laufend.Count} {(Tafel.Laufend.Count == 1 ? "läuft" : "laufen")} · " +
          $"{Tafel.Gleich.Count} gleich · {Tafel.NochOffen.Count} noch offen · Stand {Jetzt():HH:mm} Uhr";

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var tag = DateOnly.FromDateTime(jetzt);
        var bewohner = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value);
        var heute = LaufendeRegeln.Heute(speicher.Angebote(), bewohner, speicher.TeilnahmeEintraege(tag), jetzt, Bereich);
        var tafel = LaufendeRegeln.Tafel(heute, jetzt);
        Tafel = tafel;
        var bezuege = speicher.Bezuege();
        var kollegen = speicher.Kollegen();
        string? grund = Recht.Grund(speicher, Funktionen.TeilnahmeBestaetigen);   // wie im Tagesplan
        string? Bezug(Bewohner b) => bezuege.GetValueOrDefault(b.Id!.Value)?.Bezugspflege?.Kuerzel;
        string Kuerzel(string name) => kollegen.FirstOrDefault(k => k.Name == name)?.Kuerzel
            ?? string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => char.ToUpperInvariant(t[0])));

        Titel.Text = $"LAUFENDE THERAPIEN · {jetzt:HH:mm} UHR";
        Leiste.Setzen(heute, jetzt);
        Hinweis.Text = tafel.Hinweis ?? "";
        Hinweis.Visible = tafel.Hinweis is not null;

        spaltenFlaeche.SuspendLayout();
        foreach (var s in Spalten) { spaltenFlaeche.Controls.Remove(s); s.Dispose(); }
        Spalten.Clear();
        foreach (var t in tafel.Laufend)
        {
            var spalte = new TherapieSpalte(t.Name, [$"{t.Fassung.Ort} · {Kuerzel(t.Fassung.Therapeut)}", $"bis {t.Bis:HH\\:mm} · {t.Belegung}"]) { Termin = t };
            foreach (var p in t.Teilnehmer) spalte.Hinzufuegen(new Kaertchen(p, Bezug(p.Bewohner), null));
            if (t.Teilnehmer.Count == 0) spalte.Leer("Niemand aus dem Bereich.");
            Spalten.Add(spalte);
        }
        var gleich = new TherapieSpalte(GleichTitel, ["Beginn in den nächsten 30 Minuten"]);
        foreach (var t in tafel.Gleich)
            gleich.Hinzufuegen(new Kaertchen($"{t.Von:HH\\:mm} {t.Name}", $"{t.Fassung.Ort} · {t.Teilnehmer.Count} Teilnehmer"));
        if (tafel.Gleich.Count == 0) gleich.Leer("Nichts in der nächsten halben Stunde.");
        Spalten.Add(gleich);
        var offen = new TherapieSpalte(NochOffenTitel, ["Hat begonnen, Teilnahme nicht bestätigt"]);
        foreach (var (t, p) in tafel.NochOffen)
        {
            var k = new Kaertchen(p, Bezug(p.Bewohner), $"{t.Von:HH\\:mm} {t.Name}");
            k.Knopf("gegangen", grund, () => Setzen(t, p, Teilnahme.Teilgenommen));
            k.Knopf("abgesagt", grund, () => Setzen(t, p, Teilnahme.Abgesagt));
            offen.Hinzufuegen(k);
        }
        if (tafel.NochOffen.Count == 0) offen.Leer("Alles bestätigt.");
        Spalten.Add(offen);
        foreach (var s in Spalten) spaltenFlaeche.Controls.Add(s);
        spaltenFlaeche.ResumeLayout();

        Rest.Text = tafel.RestDesTages.Count == 0 ? "Heute nichts mehr."
            : string.Join(Environment.NewLine, tafel.RestDesTages.Select(t => $"{t.Zeit}  {t.Name} · {t.Fassung.Ort} · {t.Teilnehmer.Count} Teilnehmer"));
        PerformLayout();
        Geaendert?.Invoke();
    }

    // „gegangen“ oder „abgesagt“: derselbe Eintrag wie im Tagesplan (Kürzel und Uhrzeit).
    public bool Setzen(TherapieTermin t, TafelTeilnehmer p, Teilnahme status)
    {
        if (Recht.Grund(speicher, Funktionen.TeilnahmeBestaetigen) is not null) return false;
        try { speicher.TeilnahmeSetzen(p.ZuteilungId, t.Tag, status, Einstellungen.Laden().Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Laufende Therapien", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        Aktualisieren();
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = ClientSize.Width, klein = Stil.Px(this, Stil.AbstandKlein), mittel = Stil.Px(this, Stil.AbstandMittel);
        Titel.Location = new Point(0, Stil.Px(this, 6));
        BereichWahl.Location = new Point(breite - BereichWahl.Width, 0);
        bereichLabel.Location = new Point(BereichWahl.Left - klein - bereichLabel.PreferredWidth, Stil.Px(this, 5));
        int y = Math.Max(Titel.Bottom, BereichWahl.Bottom) + klein;
        Leiste.SetBounds(0, y, breite, Leiste.Height);
        y = Leiste.Bottom + mittel;
        if (Hinweis.Visible)
        {
            Hinweis.MaximumSize = new Size(breite, 0);
            Hinweis.Location = new Point(0, y);
            y = Hinweis.Bottom + mittel;
        }
        // Rest des Tages unten (eigene Höhe), die Spalten nehmen den Platz dazwischen.
        Rest.MaximumSize = new Size(breite, 0);
        int restHoehe = restTitel.PreferredHeight + klein + Rest.PreferredSize.Height;
        int spaltenHoehe = Math.Max(Stil.Px(this, 160), ClientSize.Height - y - restHoehe - mittel);
        spaltenFlaeche.SetBounds(0, y, breite, spaltenHoehe);
        foreach (var s in Spalten) s.HoeheSetzen(spaltenHoehe - SystemInformation.HorizontalScrollBarHeight - klein);
        restTitel.Location = new Point(0, spaltenFlaeche.Bottom + mittel);
        Rest.Location = new Point(0, restTitel.Bottom + klein);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) uhr.Dispose();
        base.Dispose(disposing);
    }
}

// Eine Spalte der Tafel: Kopf (Name in Fett, darunter kleine Zeilen) und Kärtchen untereinander;
// zu viele Kärtchen: Bildlauf in der Spalte.
public sealed class TherapieSpalte : KartenFlaeche
{
    public const int Breite = 240;
    private readonly Label titel = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly List<Label> zeilen = [];
    private readonly Panel inhalt = new() { AutoScroll = true, BackColor = Stil.Flaeche };
    public string Titel => titel.Text;
    public IReadOnlyList<string> KopfTexte => [.. zeilen.Select(z => z.Text)];
    public List<Kaertchen> Kaertchen { get; } = [];
    public string? LeerText { get; private set; }
    public TherapieTermin? Termin { get; init; }

    public TherapieSpalte(string titelText, IEnumerable<string> kopf)
    {
        titel.Text = titelText;
        Controls.Add(titel);
        foreach (var text in kopf)
        {
            var l = new Label { Text = text, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
            zeilen.Add(l);
            Controls.Add(l);
        }
        Controls.Add(inhalt);
        Padding = new Padding(Stil.AbstandMittel);
        Width = Stil.Px(this, Breite);
        Margin = new Padding(0, 0, Stil.AbstandMittel, 0);
        AccessibleName = $"{titelText}: {string.Join(", ", KopfTexte)}";
    }

    public void Hinzufuegen(Kaertchen k)
    {
        Kaertchen.Add(k);
        inhalt.Controls.Add(k);
    }

    public void Leer(string text)
    {
        LeerText = text;
        inhalt.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false });
    }

    public void HoeheSetzen(int hoehe)
    {
        if (Height != hoehe) Height = Math.Max(Stil.Px(this, 120), hoehe);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandMittel), mini = Stil.Px(this, Stil.AbstandMini);
        int breite = Width - 2 * innen;
        titel.MaximumSize = new Size(breite, 0);
        titel.Location = new Point(innen, innen);
        int y = titel.Bottom + mini;
        foreach (var z in zeilen) { z.MaximumSize = new Size(breite, 0); z.Location = new Point(innen, y); y = z.Bottom; }
        y += Stil.Px(this, Stil.AbstandKlein);
        inhalt.SetBounds(innen, y, breite, Math.Max(40, Height - y - innen));
        int ky = inhalt.AutoScrollPosition.Y, kb = inhalt.ClientSize.Width;
        foreach (Control c in inhalt.Controls)
        {
            if (c is Kaertchen k) { k.Width = kb - SystemInformation.VerticalScrollBarWidth; k.PerformLayout(); }
            else if (c is Label l) l.MaximumSize = new Size(kb, 0);
            c.Location = new Point(0, ky);
            ky = c.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        inhalt.AutoScrollMinSize = new Size(0, ky - inhalt.AutoScrollPosition.Y);
    }
}

// Ein Kärtchen: oben klein Bereich und Zimmer, dann der Name, unten die Bezugspflege; rechts der Status
// als Wort mit Farbe. In „Noch offen“ mit Knöpfen. Ohne Person (Spalte „Gleich“) nur zwei Zeilen.
public sealed class Kaertchen : Panel
{
    private readonly Label oben = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label name = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label unten = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Visible = false };
    public TafelTeilnehmer? Teilnehmer { get; }
    public MarkenFeld? Status { get; }
    public List<Button> Knoepfe { get; } = [];
    public string NameText => name.Text;

    public Kaertchen(TafelTeilnehmer p, string? bezugspflege, string? zusatz)
    {
        Teilnehmer = p;
        oben.Text = $"{p.Bewohner.Wohnbereich} · {p.Bewohner.Zimmer}" + (zusatz is null ? "" : $" · {zusatz}");
        name.Text = p.Bewohner.Name;
        unten.Text = bezugspflege is null ? "Bezugspflege: –" : $"Bezugspflege: {bezugspflege}";
        Status = new MarkenFeld(p.StatusText, StatusMarke(p.Status));
        Grundform();
        Controls.Add(Status);
        AccessibleName = $"{p.Bewohner.Name}, {oben.Text}, {unten.Text}, {p.StatusText}";
    }

    public Kaertchen(string zeile1, string zeile2)
    {
        name.Text = zeile1;
        unten.Text = zeile2;
        Grundform();
        AccessibleName = $"{zeile1}, {zeile2}";
    }

    private void Grundform()
    {
        BackColor = Stil.Flaeche;
        Padding = new Padding(Stil.AbstandKlein);
        Controls.AddRange([oben, name, unten, knoepfe]);
    }

    public static Stil.Marke StatusMarke(TafelStatus s) => s switch
    {
        TafelStatus.Gegangen or TafelStatus.Zurueck => Stil.Offen,
        TafelStatus.NochOffen => Stil.Achtung,
        TafelStatus.Abgesagt or TafelStatus.Ausgefallen => Stil.Beschluss,
        TafelStatus.Jetzt or TafelStatus.Gleich => Stil.Unterwegs,
        _ => Stil.Zimmer,
    };

    public void Knopf(string text, string? grund, Action aktion)
    {
        var k = Stil.NebenKnopf(text);
        k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        Stil.Sperren(k, grund);
        // Danach wird die Tafel neu aufgebaut und dieses Kärtchen freigegeben – deshalb erst nach dem Klick.
        k.Click += (_, _) => { if (Stil.Sperrgrund(k) is null) (FindForm() ?? (Control)this).BeginInvoke(aktion); };
        Knoepfe.Add(k);
        knoepfe.Controls.Add(k);
        knoepfe.Visible = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var rand = new Pen(Stil.KarteRand);
        e.Graphics.DrawRectangle(rand, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandKlein), breite = Math.Max(60, Width - 2 * innen);
        int statusBreite = Status is null ? 0 : Status.PreferredSize.Width + Stil.Px(this, Stil.AbstandMini);
        int y = innen;
        if (oben.Text.Length > 0)
        {
            oben.MaximumSize = new Size(breite - statusBreite, 0);
            oben.Location = new Point(innen, y);
            y = oben.Bottom;
        }
        else oben.Visible = false;
        if (Status is not null) Status.Bounds = new Rectangle(Width - innen - Status.PreferredSize.Width, innen, Status.PreferredSize.Width, Status.PreferredSize.Height);
        name.MaximumSize = new Size(breite - (oben.Visible ? 0 : statusBreite), 0);
        name.Location = new Point(innen, y);
        y = name.Bottom;
        unten.MaximumSize = new Size(breite, 0);
        unten.Location = new Point(innen, y);
        y = unten.Bottom;
        if (knoepfe.Visible)
        {
            knoepfe.MaximumSize = new Size(breite, 0);
            knoepfe.Location = new Point(innen, y + Stil.Px(this, Stil.AbstandMini));
            y = knoepfe.Bottom;
        }
        int hoehe = Math.Max(y, Status?.Bottom ?? 0) + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Tagesleiste 7–21 Uhr: je Angebot ein Balken in seiner Länge, Überschneidungen in eigenen Bahnen,
// ein orangefarbener Strich für „jetzt“. Ein Klick auf einen Balken öffnet das Angebot.
public sealed class Tagesleiste : Control
{
    public static readonly Color JetztFarbe = Color.FromArgb(230, 112, 0);
    private List<List<TherapieTermin>> bahnen = [];
    private readonly List<(Rectangle Feld, TherapieTermin Termin)> felder = [];
    private DateTime jetzt;
    public IReadOnlyList<(Rectangle Feld, TherapieTermin Termin)> Felder => felder;
    public int JetztX { get; private set; }
    public event Action<TherapieTermin>? TerminGeklickt;

    public Tagesleiste()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Stil.Flaeche;
        Font = Stil.Klein;
        Height = Stil.Px(this, 60);
        AccessibleRole = AccessibleRole.Graphic;
    }

    private int Kopf => Stil.Px(this, 20);
    private int Bahn => Stil.Px(this, 24);

    public void Setzen(IEnumerable<TherapieTermin> heute, DateTime jetzt)
    {
        this.jetzt = jetzt;
        bahnen = LaufendeRegeln.Bahnen(heute);
        Height = Kopf + Math.Max(1, bahnen.Count) * Bahn + Stil.Px(this, 6);
        Anordnen();
        AccessibleName = "Tagesleiste: " + (felder.Count == 0 ? "heute keine Therapie"
            : string.Join("; ", felder.Select(f => $"{f.Termin.Zeit} {f.Termin.Name}")));
        Invalidate();
    }

    private (int Links, int Rechts) Rand => (Stil.Px(this, 8), Math.Max(Stil.Px(this, 60), Width - Stil.Px(this, 8)));

    private int X(TimeOnly zeit)
    {
        var (links, rechts) = Rand;
        return links + (int)Math.Round(LaufendeRegeln.Anteil(zeit) * (rechts - links));
    }

    private void Anordnen()
    {
        felder.Clear();
        for (int i = 0; i < bahnen.Count; i++)
            foreach (var t in bahnen[i])
            {
                int x1 = X(t.Von), x2 = Math.Max(x1 + Stil.Px(this, 4), X(t.Bis));
                felder.Add((new Rectangle(x1, Kopf + i * Bahn + Stil.Px(this, 2), x2 - x1, Bahn - Stil.Px(this, 4)), t));
            }
        JetztX = X(TimeOnly.FromDateTime(jetzt));
    }

    public TherapieTermin? TerminAn(Point p) => felder.LastOrDefault(f => f.Feld.Contains(p)).Termin;

    public void Oeffnen(TherapieTermin t) => TerminGeklickt?.Invoke(t);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Anordnen();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = TerminAn(e.Location) is null ? Cursors.Default : Cursors.Hand;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (TerminAn(e.Location) is TherapieTermin t) Oeffnen(t);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
        using (var linie = new Pen(Stil.Trennlinie))
            for (int h = LaufendeRegeln.TagBeginn; h <= LaufendeRegeln.TagEnde; h++)
            {
                int x = X(new TimeOnly(h, 0));
                g.DrawLine(linie, x, Kopf - Stil.Px(this, 3), x, Height);
                if (h < LaufendeRegeln.TagEnde)
                    TextRenderer.DrawText(g, $"{h}", Font, new Point(x + Stil.Px(this, 2), 0), Stil.TextLeise, TextFormatFlags.NoPadding);
            }
        foreach (var (feld, t) in felder)
        {
            var (flaeche, rand, text) = t.Laeuft(jetzt) ? (Stil.Mischen(Stil.Flaeche, Stil.Akzent, 0.18), Stil.Akzent, Stil.TextNormal)
                : t.Vorbei(jetzt) ? (Stil.Fensterhintergrund, Stil.KarteRand, Stil.TextLeise)
                : (Stil.Zimmer.Flaeche, Stil.KarteRand, Stil.TextMittel);
            Stil.FlaecheZeichnen(g, feld, 3f * Stil.Skala(this), flaeche, rand);
            var innen = Rectangle.Inflate(feld, -Stil.Px(this, 4), 0);
            if (innen.Width > Stil.Px(this, 12)) TextRenderer.DrawText(g, t.Name, Font, innen, text, flags);
        }
        using (var strich = new Pen(JetztFarbe, Stil.Px(this, 2)))
            if (TimeOnly.FromDateTime(jetzt).Hour >= LaufendeRegeln.TagBeginn && TimeOnly.FromDateTime(jetzt).Hour < LaufendeRegeln.TagEnde)
                g.DrawLine(strich, JetztX, Kopf - Stil.Px(this, 6), JetztX, Height);
    }
}
