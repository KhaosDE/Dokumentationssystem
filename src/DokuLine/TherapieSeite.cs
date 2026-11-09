using DokuLine.Kern;

namespace DokuLine;

// Bereich „Therapie“: Angebote mit Terminen, Teilnehmern und freien Plätzen. Angebote anlegen,
// „ab Datum“ ändern, Bewohner zuteilen (Warnung bei Überschneidung), Teilnahme beenden, Verlauf.
// Zugeteilte Therapien erscheinen im Wochenplan des Bewohners . Jedes Angebot gehört
// der Therapie oder einem Wohnbereich; alle sehen alles, ändern und zuteilen darf nur
// der Anbieter. Filter nach Anbieter.
// Im Bereich „Therapie“ (Bereichswahl) sind beide Reiter eigene Einträge der Seitenleiste; oben steht dann die
// Bereichswahl statt der Reiter.
public sealed class TherapieSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => TherapieListe.Aktiv ? Reiter.Aktiv : "Therapie";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [NeuKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Zeigen();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public const string AlleAnbieter = "alle Anbieter";

    public TextBox TagFeld { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public ComboBox AnbieterFilter { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neues Angebot …");
    public List<AngebotKarte> Karten { get; } = [];
    public event Action? Geaendert;
    // Erster Reiter „Laufende Therapien“ (Anwesenheitstafel), zweiter die Angebote wie bisher.
    public const string LaufendReiter = "Laufende Therapien", AngeboteReiter = "Angebote";
    public Reiterleiste Reiter { get; } = new([LaufendReiter, AngeboteReiter]);
    public LaufendeTherapienAnsicht Laufend { get; }
    public bool LaufendAktiv => Reiter.Aktiv == LaufendReiter;
    public BereichUmschalter Umschalter { get; } = new() { Visible = false };
    public event Action<string>? BereichGewechselt;
    private bool stumm;   // Reiter von außen gesetzt: nur umschalten, gelesen wird danach in Zeigen()

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public TherapieSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        leiste.Controls.Add(new Label { Text = "Stand am", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) });
        TagFeld.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandKlein);
        leiste.Controls.Add(TagFeld);
        TagFeld.TextChanged += (_, _) => Aktualisieren();
        leiste.Controls.Add(new Label { Text = "Anbieter", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Margin = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandKlein, 0) });
        AnbieterFilter.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandKlein);
        AnbieterFilter.AccessibleName = "Filter nach Anbieter";
        leiste.Controls.Add(AnbieterFilter);
        AnbieterFilter.SelectedIndexChanged += (_, _) => { if (!filterFuellen) Aktualisieren(); };
        NeuKnopf.Click += (_, _) => Bearbeiten(null);
        Controls.Add(leiste);
        Laufend = new LaufendeTherapienAnsicht(speicher) { Jetzt = () => Jetzt() };
        Laufend.Geaendert += () => { if (LaufendAktiv) Geaendert?.Invoke(); };
        // Klick auf einen Balken der Tagesleiste: das Angebot im Reiter „Angebote“.
        Laufend.AngebotGewaehlt += a =>
        {
            Reiter.Waehlen(AngeboteReiter);
            // Ist ein anderer Anbieter gefiltert, stünde das Angebot nicht da: dann alle Anbieter zeigen.
            if (AnbieterFilter.SelectedItem is string f && f != AlleAnbieter && f != a.Anbieter) AnbieterFilter.SelectedItem = AlleAnbieter;
            if (Karten.FirstOrDefault(k => k.Angebot.Id == a.Id) is AngebotKarte karte) ScrollControlIntoView(karte);
        };
        Reiter.Gewechselt += _ => AnsichtWechseln();
        Umschalter.Gewaehlt += b => BereichGewechselt?.Invoke(b);
        Controls.Add(Reiter);
        Controls.Add(Umschalter);
        Controls.Add(Laufend);
        SichtbarkeitSetzen();
    }

    private void SichtbarkeitSetzen()
    {
        Laufend.Visible = LaufendAktiv;
        leiste.Visible = !LaufendAktiv;
        foreach (var c in inhalt) c.Visible = !LaufendAktiv;
    }

    // Mehrplatz und Änderungen: die gezeigte Ansicht neu lesen.
    public void NeuLesen()
    {
        if (LaufendAktiv) Laufend.Aktualisieren();
        else Aktualisieren();
    }

    // Aus der Seitenleiste im Bereich Therapie: „Laufende Therapien“ oder „Angebote“ (null: wie zuletzt).
    public void ReiterSetzen(string? reiter)
    {
        if (reiter is null) return;
        stumm = true;
        try { Reiter.Waehlen(reiter); }
        finally { stumm = false; }
    }

    // Reiter gewechselt: nur die eine Ansicht zeigen und neu lesen.
    private void AnsichtWechseln()
    {
        AutoScrollPosition = Point.Empty;
        SichtbarkeitSetzen();
        if (stumm) return;
        if (LaufendAktiv) Laufend.Zeigen();
        else Aktualisieren();
        PerformLayout();
        Geaendert?.Invoke();
    }

    public DateOnly Stichtag
    {
        get
        {
            try { return Datum.Lesen(TagFeld.Text, "Tag"); }
            catch (FormatException) { return DateOnly.FromDateTime(Jetzt()); }
        }
    }

    private bool filterFuellen;

    // „Therapie“, dann die Wohnbereiche (in der Reihenfolge der Bereiche) und Anbieter, die nur noch
    // an Angeboten hängen.
    private void FilterFuellen()
    {
        string gewaehlt = AnbieterFilter.SelectedItem as string ?? AlleAnbieter;
        var wohnbereiche = speicher.AlleBewohner().Select(b => b.Wohnbereich).ToHashSet();
        var namen = new List<string> { AlleAnbieter, TherapieRegeln.AnbieterTherapie };
        namen.AddRange(speicher.Bereichsliste().Select(b => b.Name).Where(n => n != TherapieRegeln.AnbieterTherapie && wohnbereiche.Contains(n)));
        namen.AddRange(speicher.Angebote().Select(a => a.Anbieter).Where(n => !namen.Contains(n)).Distinct());
        filterFuellen = true;
        AnbieterFilter.Items.Clear();
        AnbieterFilter.Items.AddRange([.. namen]);
        AnbieterFilter.SelectedItem = namen.Contains(gewaehlt) ? gewaehlt : AlleAnbieter;
        filterFuellen = false;
    }

    private List<Angebot> Gefiltert() =>
        AnbieterFilter.SelectedItem is string a && a != AlleAnbieter ? speicher.Angebote().Where(x => x.Anbieter == a).ToList() : speicher.Angebote();

    public string KopfText
    {
        get
        {
            if (LaufendAktiv) return Laufend.KopfText;
            var tag = Stichtag;
            var alle = Gefiltert().Where(a => a.Fassungen[0].GiltAb <= tag).ToList();
            int teilnahmen = alle.Sum(a => a.TeilnehmerAm(tag).Count), frei = alle.Sum(a => a.FreiAm(tag));
            return $"{alle.Count} Angebote · {teilnahmen} Teilnahmen · {frei} freie Plätze";
        }
    }

    public void Zeigen()
    {
        bool therapie = TherapieListe.Aktiv;
        Reiter.Visible = !therapie;
        Umschalter.Visible = therapie;
        if (therapie)
        {
            string rechner = Einstellungen.Laden().Bereich;
            Umschalter.Setzen(BereichswahlRegeln.Knoepfe(speicher.SichtbareWohnbereiche(Recht.Kuerzel), f => Recht.Darf(speicher, f)),
                Arbeitsbereich.Gewaehlt, BereichswahlRegeln.Eingesprungen(Arbeitsbereich.Gewaehlt, rechner));
        }
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        if (TagFeld.Text != heute) TagFeld.Text = heute;
        else Aktualisieren();
        if (LaufendAktiv) Laufend.Zeigen();
    }

    public void Aktualisieren()
    {
        var tag = Stichtag;
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        FilterFuellen();
        // Rechte: neu anlegen, wer für mindestens einen Anbieter darf.
        Stil.Sperren(NeuKnopf, speicher.AngebotAnbieter(Recht.Kuerzel).Count > 0 ? null
            : Recht.Grund(speicher, Funktionen.TherapienBearbeiten) ?? Recht.Grund(speicher, Funktionen.TherapienBereich));
        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        foreach (var a in Gefiltert())
        {
            var karte = new AngebotKarte(a, tag, namen);
            string? grund = speicher.AngebotGrund(Recht.Kuerzel, a.Anbieter);   // ändern und zuteilen nur der Anbieter
            foreach (var k in new[] { karte.ZuteilenKnopf, karte.AendernKnopf, karte.BeendenKnopf }) Stil.Sperren(k, grund);
            karte.ZuteilenKnopf.Click += (_, _) => Zuteilen(a);
            karte.AendernKnopf.Click += (_, _) => Bearbeiten(a);
            karte.BeendenKnopf.Click += (_, _) => Beenden(a);
            karte.VerlaufKnopf.Click += (_, _) =>
            {
                using var fenster = new TextFenster($"Verlauf: {a.Am(tag).Name}", string.Join(Environment.NewLine, TherapieRegeln.Verlauf(a, namen)));
                Dialog.Zeigen(fenster, FindForm());
            };
            Karten.Add(karte);
            inhalt.Add(karte);
        }
        if (Karten.Count == 0)
            inhalt.Add(new Label { Text = AnbieterFilter.SelectedItem is string f && f != AlleAnbieter ? $"Keine Angebote von {f}." : "Noch keine Angebote.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        foreach (var c in inhalt) { c.Visible = !LaufendAktiv; Controls.Add(c); }
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    public void Bearbeiten(Angebot? a)
    {
        using var fenster = new AngebotFenster(speicher, a);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Zuteilen(Angebot a)
    {
        using var fenster = new ZuteilenFenster(speicher, a);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Beenden(Angebot a)
    {
        using var fenster = new BeendenFenster(speicher, a, Stichtag);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        if (Laufend is null) return;   // Layout schon im Konstruktor (AutoScroll), bevor die Tafel angelegt ist
        if (TherapieListe.Aktiv)
        {
            Umschalter.MaximumSize = new Size(breite, 0);
            Umschalter.Bounds = new Rectangle(new Point(rand, y), Umschalter.GetPreferredSize(new Size(breite, 0)));
            y = Umschalter.Bottom + Stil.Px(this, Stil.AbstandMittel);
        }
        else
        {
            Reiter.SetBounds(rand, y, breite, Reiter.Height);
            y = Reiter.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        if (LaufendAktiv)
        {
            AutoScrollMinSize = Size.Empty;
            Laufend.SetBounds(rand, y, breite, Math.Max(Stil.Px(this, 300), ClientSize.Height - y - rand));
            return;
        }
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(Math.Max(100, breite), 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is AngebotKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand, y + Stil.Px(this, Stil.AbstandKlein));
                y = c.Bottom;
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}
