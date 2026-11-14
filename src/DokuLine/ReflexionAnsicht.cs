using DokuLine.Kern;

namespace DokuLine;

// Reiter „Reflexion“ in der Akte: Monat wählen, je Lebensbereich ein Textfeld mit Zeichenzahl und
// Stilhinweisen, daneben die Berichte des Monats zu diesem Bereich. Oben die Teilnahme an
// Therapien laut Wochenplan. „Speichern“ hält den Entwurf fest; „Als fertig markieren“ setzt ein
// Mensch, danach sind die Felder schreibgeschützt („Wieder als Entwurf“ öffnet sie).
public sealed class ReflexionAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly KartenFlaeche therapieKarte = new();
    private readonly Label therapieTitel = new() { Text = "THERAPIEN IM MONAT (LAUT WOCHENPLAN)", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private Bewohner? bewohner;

    public DateOnly Monat { get; private set; }
    public Reflexion? Aktuell { get; private set; }
    public Button ZurueckKnopf { get; } = Stil.NebenKnopf("◀");
    public Label MonatText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite };
    public Button VorKnopf { get; } = Stil.NebenKnopf("▶");
    public MarkenFeld Status { get; } = new("Entwurf", Stil.Achtung);
    public Button SpeichernKnopf { get; } = Stil.NebenKnopf("Speichern");
    public Button FertigKnopf { get; } = Stil.HauptKnopf("Als fertig markieren");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public Label Therapien { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label Meldung { get; } = new() { AutoSize = true, BackColor = Stil.Seite, Visible = false };
    public List<ReflexionBereich> Bereiche { get; } = [];

    // Zum Testen ersetzbar: Rückfrage bei ungespeicherten Änderungen (Ja = speichern, Nein = verwerfen).
    public Func<string, DialogResult> Rueckfrage { get; set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public ReflexionAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Rueckfrage = frage => MessageBox.Show(FindForm(), frage, "Reflexion", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        ZurueckKnopf.Width = VorKnopf.Width = 44;
        MonatText.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        Status.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, 0);
        Meldung.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        foreach (var k in new Control[] { ZurueckKnopf, VorKnopf, SpeichernKnopf, FertigKnopf, VerlaufKnopf })
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        leiste.Controls.AddRange([ZurueckKnopf, MonatText, VorKnopf, Status, SpeichernKnopf, FertigKnopf, VerlaufKnopf, Meldung]);
        ZurueckKnopf.Click += (_, _) => MonatWaehlen(Monat.AddMonths(-1));
        VorKnopf.Click += (_, _) => MonatWaehlen(Monat.AddMonths(1));
        SpeichernKnopf.Click += (_, _) => Speichern(Aktuell?.Fertig == true);
        FertigKnopf.Click += (_, _) => Speichern(Aktuell?.Fertig != true);
        VerlaufKnopf.Click += (_, _) =>
        {
            if (Aktuell is null || bewohner is null) return;
            using var fenster = new TextFenster($"Verlauf: Reflexion {ReflexionRegeln.MonatText(Monat)} – {bewohner.Name}",
                string.Join(Environment.NewLine, ReflexionRegeln.Verlauf(Aktuell)));
            Dialog.Zeigen(fenster, FindForm());
        };
        for (int i = 1; i <= BerichtRegeln.Lebensbereiche.Length; i++)
        {
            var b = new ReflexionBereich(i);
            Bereiche.Add(b);
            Controls.Add(b);
        }
        therapieKarte.Controls.AddRange([therapieTitel, Therapien]);
        Controls.Add(leiste);
        Controls.Add(therapieKarte);
    }

    public bool Geaendert => Aktuell is not null && Bereiche.Any(b => b.Feld.Text.Trim() != Aktuell.Text(b.Nummer));

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        Monat = ReflexionRegeln.Vorschlag(DateOnly.FromDateTime(Jetzt()));
        Laden();
    }

    // Wechsel des Monats; ungespeicherte Änderungen vorher speichern oder verwerfen.
    public void MonatWaehlen(DateOnly monat)
    {
        if (Geaendert)
        {
            var antwort = Rueckfrage("Die Reflexion hat ungespeicherte Änderungen. Jetzt speichern?");
            if (antwort == DialogResult.Cancel) return;
            if (antwort == DialogResult.Yes && !Speichern(Aktuell!.Fertig)) return;
        }
        Monat = ReflexionRegeln.MonatVon(monat);
        Laden();
    }

    private void Laden()
    {
        if (bewohner is null) return;
        long id = bewohner.Id!.Value;
        Aktuell = speicher.Reflexion(id, Monat);
        MonatText.Text = ReflexionRegeln.MonatText(Monat);
        VorKnopf.Enabled = Monat < ReflexionRegeln.MonatVon(DateOnly.FromDateTime(Jetzt()));
        bool fertig = Aktuell.Fertig;
        Status.Text = ReflexionRegeln.StatusText(Aktuell);
        Status.Marke = fertig ? Stil.Offen : Aktuell.Aktuell is null ? Stil.Zimmer : Stil.Achtung;
        FertigKnopf.Text = fertig ? "Wieder als Entwurf" : "Als fertig markieren";
        SpeichernKnopf.Enabled = !fertig;
        VerlaufKnopf.Enabled = Aktuell.Fassungen.Count > 0;
        Meldung.Visible = false;

        // Geschriebene Berichte einzeln; bestätigte Maßnahmen gezählt je Maßnahme (Bauplan 39).
        var berichte = speicher.Berichte(id, new BerichtFilter(Monat, ReflexionRegeln.MonatBis(Monat))).Where(x => x.Art != MassnahmenRegeln.ArtBericht).ToList();
        var massnahmen = MassnahmenRegeln.ReflexionZeilen(speicher.MassnahmeBestaetigungen(id, Monat, ReflexionRegeln.MonatBis(Monat)));
        foreach (var b in Bereiche)
            b.Setzen(Aktuell.Text(b.Nummer), fertig, berichte.Where(x => x.Bereiche.Contains(b.Nummer)).ToList(),
                [.. massnahmen.Where(m => m.Lebensbereich == b.Nummer).Select(m => m.Zeile)]);
        var therapien = ReflexionRegeln.Therapien(speicher.TermineImMonat(id, Monat));
        Therapien.Text = therapien.Count == 0
            ? "Keine Therapien im Wochenplan dieses Monats."
            : string.Join(Environment.NewLine, therapien.Select(ReflexionRegeln.TherapieText));
        PerformLayout();
    }

    // fertig: der Status nach dem Speichern. true = gespeichert.
    public bool Speichern(bool fertig) => Speichern(fertig, ohnePruefung: false);

    private bool Speichern(bool fertig, bool ohnePruefung)
    {
        if (bewohner is null) return false;
        try
        {
            var f = ReflexionRegeln.AusEingabe(bewohner.Id!.Value, Monat, Bereiche.Select(b => b.Feld.Text).ToList(), fertig,
                Recht.Kuerzel, Jetzt());
            speicher.ReflexionSpeichern(f, ohnePruefung ? null : Aktuell?.Fassungen.Count);
        }
        catch (Konflikt k)
        {
            switch (KonfliktFenster.Fragen(FindForm(), k))
            {
                case KonfliktWahl.MeineSpeichern:
                    return Speichern(fertig, ohnePruefung: true);
                case KonfliktWahl.AndereUebernehmen:
                    Laden();
                    MeldungZeigen("Die Fassung des anderen Arbeitsplatzes ist übernommen.", Stil.Offen);
                    return false;
                default:
                    MeldungZeigen("Nicht gespeichert – ein anderer Arbeitsplatz hat die Reflexion inzwischen geändert.", Stil.Achtung);
                    return false;
            }
        }
        catch (Eingabefehler e)
        {
            MeldungZeigen(string.Join(" ", e.Fehler), Stil.Beschluss);
            return false;
        }
        Laden();
        MeldungZeigen(fertig ? "Als fertig gespeichert." : "Entwurf gespeichert.", Stil.Offen);
        return true;
    }

    private void MeldungZeigen(string text, Stil.Marke marke)
    {
        Meldung.Text = text;
        Meldung.ForeColor = marke.Text;
        Meldung.Visible = true;
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Bereiche.Count == 0) return;   // Anordnung schon während des Aufbaus
        int breite = Math.Max(200, ClientSize.Width), abstand = Stil.Px(this, Stil.AbstandMittel), innen = Stil.Px(this, Stil.AbstandGross);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(breite, 0)));
        int y = leiste.Bottom + Stil.Px(this, Stil.AbstandMini);
        therapieTitel.Location = new Point(innen, innen);
        Therapien.MaximumSize = new Size(breite - 2 * innen, 0);
        Therapien.Location = new Point(innen, therapieTitel.Bottom + Stil.Px(this, Stil.AbstandMini));
        therapieKarte.Bounds = new Rectangle(0, y, breite, Therapien.Bottom + innen);
        y = therapieKarte.Bottom + abstand;
        foreach (var b in Bereiche)
        {
            b.SetBounds(0, y, breite, b.Height);
            b.PerformLayout();
            y = b.Bottom + abstand;
        }
        if (Height != y) Height = y;
    }
}

// Ein Lebensbereich: links Textfeld mit Zeichenzahl und Stilhinweisen, rechts die Berichte des
// Monats zu diesem Bereich.
public sealed class ReflexionBereich : KartenFlaeche
{
    private readonly Label titel = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label quellenTitel = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly List<Label> quellen = [];

    public int Nummer { get; }
    public TextBox Feld { get; } = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, Height = 130 };
    public Label Zeichen { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    public Label Hinweise { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public IReadOnlyList<StilHinweis> StilHinweise { get; private set; } = [];
    public IEnumerable<string> QuellenTexte => quellen.Select(q => q.Text);

    public ReflexionBereich(int nummer)
    {
        Nummer = nummer;
        titel.Text = $"{nummer}  {BerichtRegeln.Lebensbereiche[nummer - 1]}";
        Feld.TextChanged += (_, _) => Pruefen();
        Controls.AddRange([titel, Feld, Zeichen, Hinweise, quellenTitel]);
    }

    public void Setzen(string text, bool fertig, IReadOnlyList<Bericht> berichte, IReadOnlyList<string>? massnahmen = null)
    {
        Feld.Text = text;
        Feld.ReadOnly = fertig;
        Feld.BackColor = fertig ? Stil.Fensterhintergrund : Stil.Flaeche;
        foreach (var q in quellen) { Controls.Remove(q); q.Dispose(); }
        quellen.Clear();
        quellenTitel.Text = $"BERICHTE IM MONAT ({berichte.Count})";
        if (berichte.Count == 0)
            quellen.Add(new Label { Text = "Keine Berichte zu diesem Bereich im Monat.", ForeColor = Stil.Achtung.Text, Font = Stil.FettKursiv });
        foreach (string m in massnahmen ?? [])
            quellen.Add(new Label { Text = $"Maßnahme {m}", ForeColor = Stil.TextNormal, Font = Stil.Klein });
        foreach (var b in berichte.OrderBy(b => b.Zeitpunkt))
        {
            string t = b.Text.ReplaceLineEndings(" ");
            quellen.Add(new Label { Text = $"{b.Zeitpunkt:dd.MM.} {BerichtRegeln.SchichtText(b.Schicht)} · {b.Verfasser}: {(t.Length > 180 ? t[..179] + "…" : t)}", ForeColor = Stil.TextMittel, Font = Stil.Klein });
        }
        foreach (var q in quellen)
        {
            q.AutoSize = true;
            q.BackColor = Stil.Flaeche;
            q.UseMnemonic = false;
            Controls.Add(q);
        }
        Pruefen();
        PerformLayout();
    }

    private void Pruefen()
    {
        StilHinweise = Stilpruefung.Pruefen(Feld.Text);
        Zeichen.Text = Feld.Text.Trim().Length == 0 ? "Noch nichts geschrieben." : $"{Feld.Text.Trim().Length} Zeichen";
        Hinweise.Text = string.Join(Environment.NewLine, StilHinweise.Select(h => "Hinweis: " + h.Text));
        Hinweise.Visible = StilHinweise.Count > 0;
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Feld is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), luecke = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(200, Width - 2 * innen);
        int links = breite * 55 / 100, rechts = breite - links - luecke;
        titel.MaximumSize = new Size(breite, 0);
        titel.Location = new Point(innen, innen);
        int y = titel.Bottom + Stil.Px(this, Stil.AbstandKlein);
        Feld.SetBounds(innen, y, links, Stil.Px(this, 130));
        int yl = Feld.Bottom + Stil.Px(this, Stil.AbstandMini);
        Zeichen.Location = new Point(innen, yl);
        yl = Zeichen.Bottom + Stil.Px(this, Stil.AbstandMini);
        Hinweise.MaximumSize = new Size(links, 0);
        Hinweise.Location = new Point(innen, yl);
        if (StilHinweise.Count > 0) yl = Hinweise.Bottom;
        int x = innen + links + luecke, yr = y;
        quellenTitel.Location = new Point(x, yr);
        yr = quellenTitel.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var q in quellen)
        {
            q.MaximumSize = new Size(Math.Max(100, rechts), 0);
            q.Location = new Point(x, yr);
            yr = q.Bottom + Stil.Px(this, Stil.AbstandMini);
        }
        int hoehe = Math.Max(yl, yr) + innen;
        if (Height != hoehe) Height = hoehe;
    }
}
