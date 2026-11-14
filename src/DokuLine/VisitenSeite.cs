using DokuLine.Kern;

namespace DokuLine;

// Bereich „Visiten“: oben der Umschalter Psychiater · Hausarzt, darunter dessen Visiten (neueste oben) mit Filter nach
// Bewohner und Zeitraum. „Visite beginnen …“ und „Neue Visite …“ gelten für den gewählten Arzt. Nach dem Speichern blendet
// das Programm sofort die entstandenen Aufgaben ein; in der Dokumentation steht die Visite als Bericht der Art „Visite“.
public sealed class VisitenSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Visiten";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [BeginnenKnopf, NeuKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Zeigen();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];
    private List<Bewohner> bewohner = [];

    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Visite …");   // einzelne Bewohner
    // Visite mit allen Bewohnern des Bereichs (fortsetzen, wenn ein Entwurf besteht).
    public Button BeginnenKnopf { get; } = Stil.HauptKnopf("Visite beginnen …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Visitenliste drucken …");
    public FilterKnopf PsychiaterKnopf { get; } = new(VisitenRegeln.Psychiater, mitZahl: false) { Aktiv = true };
    public FilterKnopf HausarztKnopf { get; } = new(VisitenRegeln.Hausarzt, mitZahl: false);
    public string Art => HausarztKnopf.Aktiv ? VisitenRegeln.Hausarzt : VisitenRegeln.Psychiater;
    public VisitenRundgang? Rundgang { get; private set; }
    public ComboBox BewohnerFilter { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    public TextBox Von { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox Bis { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public Label FilterFehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Seite, Visible = false };
    public List<VisiteKarte> Karten { get; } = [];
    public event Action? Geaendert;
    // Knopf in der Einblendung nach dem Speichern: das Hauptfenster führt die Aufgabe aus.
    public event Action<Aufgabe>? AufgabeGewuenscht;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VisitenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var c in new Control[] { PsychiaterKnopf, HausarztKnopf, Beschriftung("Bewohner"), BewohnerFilter, Beschriftung("von"), Von, Beschriftung("bis"), Bis, DruckKnopf, FilterFehler })
        {
            c.Margin = c is Label ? new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) : new Padding(0, 0, Stil.AbstandMittel, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        HausarztKnopf.Margin = new Padding(0, 0, Stil.AbstandGross * 2, Stil.AbstandKlein);
        PsychiaterKnopf.Click += (_, _) => ArtWaehlen(VisitenRegeln.Psychiater);
        HausarztKnopf.Click += (_, _) => ArtWaehlen(VisitenRegeln.Hausarzt);
        BewohnerFilter.AccessibleName = "Bewohner";
        Von.AccessibleName = "Zeitraum von";
        Bis.AccessibleName = "Zeitraum bis";
        BewohnerFilter.SelectedIndexChanged += (_, _) => Aktualisieren();
        Von.TextChanged += (_, _) => Aktualisieren();
        Bis.TextChanged += (_, _) => Aktualisieren();
        NeuKnopf.Click += (_, _) => NeueVisite();
        BeginnenKnopf.Click += (_, _) => Beginnen();
        DruckKnopf.Click += (_, _) => VisitenlisteDrucken();
        Controls.Add(leiste);
        AutoScroll = true;
    }

    public void ArtWaehlen(string art)
    {
        PsychiaterKnopf.Aktiv = art == VisitenRegeln.Psychiater;
        HausarztKnopf.Aktiv = art == VisitenRegeln.Hausarzt;
        Aktualisieren();
    }

    private static Label Beschriftung(string text) => new() { Text = text, AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite };

    // Visiten gehören zu einem Wohnbereich: dem in der Seitenleiste gewählten.
    private static string Bereich => Arbeitsbereich.Gewaehlt.Length > 0 ? Arbeitsbereich.Gewaehlt : Einstellungen.Laden().Bereich;
    public const string WohnbereichWaehlenText = "Bitte zuerst in der Übersicht einen Wohnbereich wählen.";
    private string? OhneWohnbereich => speicher.AlleBewohner().Any(b => b.Wohnbereich == Bereich) ? null : WohnbereichWaehlenText;

    public string KopfText => Rundgang is not null ? $"Visite läuft · {Rundgang.KopfText}" : $"{Art} · " + (Karten.Count == 1 ? "1 Visite" : $"{Karten.Count} Visiten") +
                              (BewohnerFilter.SelectedIndex > 0 ? $" · {bewohner[BewohnerFilter.SelectedIndex - 1].Name}" : "");

    // Beim Öffnen des Bereichs: Bewohnerliste neu (Auswahl bleibt), dann die Liste.
    public void Zeigen()
    {
        long? gewaehlt = GewaehlterBewohner;
        bewohner = speicher.AlleBewohner();
        BewohnerFilter.BeginUpdate();
        BewohnerFilter.Items.Clear();
        BewohnerFilter.Items.Add("Alle Bewohner");
        BewohnerFilter.Items.AddRange([.. bewohner.Select(b => (object)$"{b.Name}, Zi. {b.Zimmer}")]);
        int index = bewohner.FindIndex(b => b.Id == gewaehlt);
        BewohnerFilter.SelectedIndex = index + 1;   // nicht gefunden: „Alle Bewohner“
        BewohnerFilter.EndUpdate();
        Aktualisieren();
    }

    public long? GewaehlterBewohner => BewohnerFilter.SelectedIndex > 0 ? bewohner[BewohnerFilter.SelectedIndex - 1].Id : null;

    public void BewohnerWaehlen(long? id)
    {
        if (bewohner.Count == 0) Zeigen();
        BewohnerFilter.SelectedIndex = bewohner.FindIndex(b => b.Id == id) + 1;
    }

    public void Aktualisieren()
    {
        if (BewohnerFilter.Items.Count == 0) return;   // noch nicht gezeigt
        Stil.Sperren(NeuKnopf, Recht.Grund(speicher, Funktionen.Visiten));   // Rechte
        Stil.Sperren(BeginnenKnopf, Recht.Grund(speicher, Funktionen.Visiten) ?? OhneWohnbereich);
        // Je Bereich gibt es einen Entwurf; er nennt seine Art (Psychiater oder Hausarzt), auch wenn gerade die andere gewählt ist.
        BeginnenKnopf.Text = speicher.VisitenEntwurf(Bereich) is not VisitenEntwurf e ? "Visite beginnen …"
            : e.Art.Length > 0 ? $"Visite {e.Art} fortsetzen" : "Visite fortsetzen";
        bool fehler = false;
        DateOnly? Lesen(TextBox feld)
        {
            if (feld.Text.Trim().Length == 0) return null;
            try { return Datum.Lesen(feld.Text, "Zeitraum"); }
            catch (FormatException) { fehler = true; return null; }
        }
        var von = Lesen(Von);
        var bis = Lesen(Bis);
        FilterFehler.Text = "Zeitraum bitte als TT.MM.JJJJ eingeben.";
        FilterFehler.Visible = fehler;
        long? id = GewaehlterBewohner;
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        foreach (var v in speicher.Visiten(id, von, bis).Where(v => v.Art == Art || v.Art.Length == 0))
        {
            var karte = new VisiteKarte(v, namen, id);
            Karten.Add(karte);
            inhalt.Add(karte);
        }
        if (Karten.Count == 0)
            inhalt.Add(new Label { Text = "Keine Visiten für diese Auswahl.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        foreach (var c in inhalt)
        {
            c.Visible = Rundgang is null;   // während einer Visite bleibt die Liste verborgen
            Controls.Add(c);
        }
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    // „Visite beginnen …“ (Datum, Arzt, dabei) bzw. eine unterbrochene Visite fortsetzen.
    public bool Beginnen()
    {
        if (Recht.Grund(speicher, Funktionen.Visiten) is not null || OhneWohnbereich is not null) return false;
        var entwurf = speicher.VisitenEntwurf(Bereich);
        if (entwurf is null)
        {
            using var fenster = new VisiteBeginnenFenster(speicher, Bereich, Art);
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK || fenster.Entwurf is null) return false;
            entwurf = fenster.Entwurf;
        }
        else if (entwurf.Art.Length == 0)   // Entwurf aus einer Version ohne Psychiater/Hausarzt: die gewählte Art
        {
            entwurf.Art = Art;
            speicher.VisitenEntwurfSpeichern(entwurf);
        }
        RundgangZeigen(entwurf);
        return true;
    }

    private void RundgangZeigen(VisitenEntwurf entwurf)
    {
        RundgangBeenden();
        Rundgang = new VisitenRundgang(speicher, entwurf) { Jetzt = Jetzt };
        Rundgang.Geaendert += () => Geaendert?.Invoke();
        Rundgang.Unterbrochen += () => { RundgangBeenden(); Aktualisieren(); };
        Rundgang.Abgeschlossen += gespeichert =>
        {
            RundgangBeenden();
            Aktualisieren();
            AufgabenEinblenden(gespeichert.Aufgaben);   // erst jetzt, gesammelt
        };
        foreach (Control c in Controls) if (c != Rundgang) c.Visible = false;
        Controls.Add(Rundgang);
        AutoScroll = false;
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void RundgangBeenden()
    {
        if (Rundgang is null) return;
        Controls.Remove(Rundgang);
        Rundgang.Dispose();
        Rundgang = null;
        foreach (Control c in Controls) c.Visible = true;
        AutoScroll = true;
        PerformLayout();
    }

    public void VisitenlisteDrucken()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        var bloecke = speicher.AlleBewohner().Where(b => b.Wohnbereich == Bereich).OrderBy(b => b.Zimmer, StringComparer.Ordinal)
            .Select(b => (b, (IReadOnlyList<Tagesstand>)speicher.Verordnungen(b.Id!.Value).Select(v => MedikationRegeln.Stand(v, heute)).ToList())).ToList();
        var druck = new VisitenlisteDruck(Bereich, heute, bloecke);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Visitenliste {Bereich}");
    }

    public void NeueVisite()
    {
        if (Recht.Grund(speicher, Funktionen.Visiten) is not null) return;
        Speicher.VisiteGespeichert? gespeichert;
        using (var fenster = new VisiteFenster(speicher, GewaehlterBewohner, Art))
        {
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
            gespeichert = fenster.Gespeichert;
        }
        Aktualisieren();
        if (gespeichert is not null) AufgabenEinblenden(gespeichert.Aufgaben);
    }

    // Gleich nach dem Speichern: die entstandenen Aufgaben, je mit Knopf. Ist eine erledigt, kommt die
    // Einblendung mit den übrigen wieder; „Später“ (oder Abbrechen) schließt nur die Einblendung.
    public void AufgabenEinblenden(IReadOnlyList<Aufgabe> aufgaben)
    {
        var ids = aufgaben.Select(a => a.Id).ToList();
        while (true)
        {
            var offen = ids.Select(i => speicher.AufgabeLesen(i)).OfType<Aufgabe>().Where(a => a.Offen).ToList();
            if (offen.Count == 0) return;
            Aufgabe? wunsch;
            using (var fenster = new VisitenAufgabenFenster(speicher, offen))
            {
                if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
                wunsch = fenster.Wunsch;
            }
            if (wunsch is null) return;
            AufgabeGewuenscht?.Invoke(wunsch);
            if (speicher.AufgabeLesen(wunsch.Id)?.Offen != false) return;   // nicht erledigt: nicht noch einmal fragen
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Rundgang is not null)   // die Visitenansicht füllt die ganze Fläche
        {
            Rundgang.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            return;
        }
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(breite, 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is VisiteKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandMittel));
                y = c.Bottom + Stil.Px(this, Stil.AbstandMini);
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Eine Visite: Datum, Arzt (ältere auch die Art), wer dabei war; je Bewohner das Besprochene und Marken für geänderte
// Anordnungen und nötige Facharzttermine. Mit Bewohnerfilter nur dessen Eintrag.
public sealed class VisiteKarte : KartenFlaeche
{
    private readonly List<Control> teile = [];
    private readonly HashSet<Control> abstandDavor = [];

    public Visite Visite { get; }
    public IReadOnlyList<string> Texte { get; }

    public VisiteKarte(Visite v, IReadOnlyDictionary<long, string> namen, long? nurBewohner)
    {
        Visite = v;
        var texte = new List<string>();
        Label Text(string text, Font font, Color farbe)
        {
            var l = new Label { Text = text, Font = font, ForeColor = farbe, AutoSize = true, BackColor = Stil.Flaeche, UseMnemonic = false };
            teile.Add(l);
            Controls.Add(l);
            texte.Add(text);
            return l;
        }
        Text($"{Kern.Datum.Text(v.Datum)} · {(v.Art.Length > 0 ? $"{v.Art} · " : "")}{v.Arzt}", Stil.KarteName, Stil.TextNormal);
        Text((v.Teilnehmer.Length > 0 ? $"dabei: {v.Teilnehmer} · " : "") + $"eingetragen von {v.Kuerzel}, {BerichtRegeln.ZeitText(v.Erstellt)}", Stil.Klein, Stil.TextLeise);
        foreach (var e in v.Eintraege.Where(e => nurBewohner is null || e.BewohnerId == nurBewohner))
        {
            abstandDavor.Add(Text(namen.GetValueOrDefault(e.BewohnerId) ?? "(Bewohner nicht mehr vorhanden)", Stil.Fett, Stil.TextNormal));
            Text(e.Besprochen, Stil.Standard, Stil.TextNormal);
            var marken = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
            void Marke(string text, Stil.Marke m)
            {
                marken.Controls.Add(new MarkenFeld(text, m) { Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandMini, 0) });
                texte.Add(text);
            }
            if (e.Anordnungen.Count == 0) Marke("Anordnungen nicht geändert", Stil.Zimmer);
            foreach (var a in e.Anordnungen) Marke($"{a.Medikament}: {VisitenRegeln.ArtText(a.Art)}", Stil.Hinweis);
            if (e.Facharzt.Length > 0) Marke($"Facharzttermin nötig: {e.Facharzt}" + (e.Dringend ? ", dringend" : ""), e.Dringend ? Stil.Beschluss : Stil.Achtung);
            teile.Add(marken);
            Controls.Add(marken);
        }
        Texte = texte;
        AccessibleName = $"Visite {string.Join(", ", texte.Take(1))}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        foreach (var c in teile)
        {
            if (abstandDavor.Contains(c)) y += Stil.Px(this, Stil.AbstandMittel);
            c.MaximumSize = new Size(breite, 0);
            c.Bounds = new Rectangle(new Point(innen, y), c.GetPreferredSize(new Size(breite, 0)));
            y = c.Bottom + Stil.Px(this, 2);
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }
}
