using DokuLine.Kern;

namespace DokuLine;

// Bereich „Tagesplan“ (Bauplan Schritt 23): alle Termine von heute für die Bewohner des eigenen
// Bereichs (Häkchen „alle Bereiche“) – nach Uhrzeit oder nach Bewohner. Bei Therapien „gegangen“,
// „abgesagt“, „ausgefallen“; das ist derselbe Eintrag wie im Wochenplan, mit Kürzel und Uhrzeit.
// Nach Beginn ohne Bestätigung: „noch offen“. Nach „gegangen“ steht der Bewohner auf Karte und Akte
// bis zum Ende als „in Therapie: … bis …“. Andere Rechner sehen es über den Mehrplatz-Abgleich.
public sealed class TagesplanSeite : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];
    private string bereichText = "";

    public Label TagText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite };
    public RadioButton NachUhrzeit { get; } = new() { Text = "nach Uhrzeit", AutoSize = true, Checked = true, BackColor = Stil.Seite };
    public RadioButton NachBewohner { get; } = new() { Text = "nach Bewohner", AutoSize = true, BackColor = Stil.Seite };
    public CheckBox AlleBereiche { get; } = new() { Text = "alle Bereiche", AutoSize = true, BackColor = Stil.Seite };
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public List<TagesplanKarte> Karten { get; } = [];
    public List<TagesplanZeile> Zeilen { get; private set; } = [];
    public List<string> Gruppen { get; } = [];   // „nach Bewohner“: Überschriften
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public TagesplanSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        TagText.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein);
        foreach (var c in new Control[] { NachUhrzeit, NachBewohner, AlleBereiche }) c.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMittel, Stil.AbstandKlein);
        Fehler.Margin = new Padding(Stil.AbstandGross, Stil.AbstandMini, 0, Stil.AbstandKlein);
        leiste.Controls.AddRange([TagText, NachUhrzeit, NachBewohner, AlleBereiche, Fehler]);
        NachUhrzeit.CheckedChanged += (_, _) => { if (NachUhrzeit.Checked) Aktualisieren(); };
        NachBewohner.CheckedChanged += (_, _) => { if (NachBewohner.Checked) Aktualisieren(); };
        AlleBereiche.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(leiste);
    }

    public string KopfText
    {
        get
        {
            var jetzt = Jetzt();
            int therapien = Zeilen.Count(z => z.Therapie);
            int bestaetigt = Zeilen.Count(z => z.Therapie && z.Status != Teilnahme.Offen);
            int offen = Zeilen.Count(z => TagesplanRegeln.Marke(z, jetzt) == TagesplanRegeln.NochOffen);
            return $"{Zeilen.Count} Termine heute · {bereichText} · {bestaetigt} von {therapien} Therapien bestätigt" +
                   (offen > 0 ? $" · {offen} noch offen" : "");
        }
    }

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        var werte = Einstellungen.Laden();
        TagText.Text = "Heute, " + heute.ToString("dddd, dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        // Eigene Bereiche wie in der Übersicht: Wohnbereiche des Angemeldeten, sonst der des Rechners.
        var alle = speicher.AlleBewohner();
        var meine = speicher.MitarbeiterLesen(werte.Kuerzel)?.Bereiche.Where(b => alle.Any(x => x.Wohnbereich == b)).ToList()
            is { Count: > 0 } liste ? liste : [werte.Bereich];
        var eigene = alle.Where(b => meine.Contains(b.Wohnbereich)).ToList();
        bool allesZeigen = AlleBereiche.Checked || eigene.Count == 0;
        bereichText = allesZeigen ? "alle Bereiche" : string.Join(", ", meine);
        var zeilen = speicher.Tagesplan(heute, allesZeigen ? alle : eigene);
        Zeilen = NachBewohner.Checked ? TagesplanRegeln.NachBewohner(zeilen) : TagesplanRegeln.NachUhrzeit(zeilen);
        string? grund = Recht.Grund(speicher, Funktionen.TeilnahmeBestaetigen);   // Rechte (Schritt 19)

        var scroll = AutoScrollPosition;
        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        Gruppen.Clear();
        Bewohner? zuletzt = null;
        foreach (var z in Zeilen)
        {
            if (NachBewohner.Checked && z.Bewohner.Id != zuletzt?.Id)
            {
                string titel = $"{z.Bewohner.Name} · Zimmer {z.Bewohner.Zimmer}".ToUpperInvariant();
                Gruppen.Add(titel);
                inhalt.Add(new Label { Text = titel, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
                zuletzt = z.Bewohner;
            }
            var karte = new TagesplanKarte(z, jetzt);
            foreach (var (knopf, status) in karte.Knoepfe)
            {
                Stil.Sperren(knopf, grund);
                knopf.Click += (_, _) => Setzen(z, status);
            }
            Karten.Add(karte);
            inhalt.Add(karte);
        }
        if (Zeilen.Count == 0) inhalt.Add(new Label { Text = "Heute stehen keine Termine an.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        PerformLayout();
        AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
        Geaendert?.Invoke();
    }

    // „gegangen“, „abgesagt“ oder „ausgefallen“ – ein Eintrag mit Kürzel und Uhrzeit (wie im Wochenplan).
    public bool Setzen(TagesplanZeile z, Teilnahme status)
    {
        if (!z.Therapie || Recht.Grund(speicher, Funktionen.TeilnahmeBestaetigen) is not null) return false;
        try
        {
            speicher.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, z.Termin.Tag, status, Einstellungen.Laden().Kuerzel, Jetzt());
            Fehler.Visible = false;
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        Aktualisieren();
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(breite, 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is TagesplanKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandKlein));
                y = c.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Ein Termin im Tagesplan: Zeit und Titel, Bewohner und Ort, Marken (woher, Teilnahme) und bei
// Therapien die Knöpfe „gegangen“, „abgesagt“, „ausgefallen“.
public sealed class TagesplanKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label titel = new();
    private readonly Label wer = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public TagesplanZeile Zeile { get; }
    public Button GegangenKnopf { get; } = Stil.NebenKnopf("gegangen");
    public Button AbgesagtKnopf { get; } = Stil.NebenKnopf("abgesagt");
    public Button AusgefallenKnopf { get; } = Stil.NebenKnopf("ausgefallen");
    public IReadOnlyList<(Button Knopf, Teilnahme Status)> Knoepfe =>
        [(GegangenKnopf, Teilnahme.Teilgenommen), (AbgesagtKnopf, Teilnahme.Abgesagt), (AusgefallenKnopf, Teilnahme.Ausgefallen)];
    public IReadOnlyList<string> MarkenTexte { get; }
    public string TitelText => titel.Text;

    public TagesplanKarte(TagesplanZeile z, DateTime jetzt)
    {
        Zeile = z;
        var t = z.Termin;
        var liste = new List<(string, Stil.Marke)>
        {
            (t.Quelle switch { TerminQuelle.Therapie => "Therapie", TerminQuelle.Kalender => "Kalender", TerminQuelle.Fahrt => "Fahrt", _ => "Wochenplan" },
             t.Quelle == TerminQuelle.Therapie ? Stil.Hinweis : Stil.Zimmer),
        };
        if (TagesplanRegeln.Marke(z, jetzt) is string status)
            liste.Add((status, z.Status switch
            {
                Teilnahme.Teilgenommen => Stil.Offen,
                Teilnahme.Abgesagt or Teilnahme.Ausgefallen => Stil.Zimmer,
                _ => Stil.Achtung,   // noch offen
            }));
        MarkenTexte = liste.Select(x => x.Item1).ToList();
        foreach (var (text, m) in liste) marken.Controls.Add(new MarkenFeld(text, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        titel.Text = $"{t.ZeitText}  {t.Titel}";
        titel.Font = Stil.KarteName;
        titel.ForeColor = z.Status is Teilnahme.Abgesagt or Teilnahme.Ausgefallen ? Stil.TextLeise : Stil.TextNormal;
        wer.Text = $"{z.Bewohner.Name} · Zimmer {z.Bewohner.Zimmer}" + (t.Ort.Length > 0 ? $" · {t.Ort}" : "");
        wer.Font = Stil.Standard;
        wer.ForeColor = Stil.TextMittel;
        foreach (var l in new[] { titel, wer })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        foreach (var (k, _) in Knoepfe)
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            k.Visible = z.Therapie;
            knoepfe.Controls.Add(k);
        }
        knoepfe.Visible = z.Therapie;
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{titel.Text}, {wer.Text}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Zeile is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var l in new[] { titel, wer })
        {
            l.MaximumSize = new Size(breite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, 3);
        }
        int hoehe;
        if (knoepfe.Visible)
        {
            knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
            hoehe = knoepfe.Bottom + innen - Stil.Px(this, Stil.AbstandMini);
        }
        else hoehe = y + innen - Stil.Px(this, 3);
        if (Height != hoehe) Height = hoehe;
    }
}
