using DokuLine.Kern;

namespace DokuLine;

// Startseite „Übersicht“: oben der Bereich „Heute“ (Bewohner, Abwesende, Termine des Tages,
// Änderungen in der Medikation), darunter je Bewohner eine Karte mit den Marken des Tages.
// Klick auf eine Karte öffnet die Akte. Baut sich nach Änderungen und um Mitternacht neu auf.
// Mehrplatz: Gezeigt wird der eigene Bereich (Einstellungen), umschaltbar auf „alle Bereiche“.
public sealed class UebersichtSeite : Panel
{
    private readonly Speicher speicher;
    private readonly KartenFlaeche heuteKarte = new();
    private readonly Label heuteTitel = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly Label termineTitel = new() { Text = "TERMINE HEUTE", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly Label medikationTitel = new() { Text = "MEDIKATION HEUTE", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly Label fahrzeugeTitel = new() { Text = "FAHRZEUGE", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    public FahrzeugLeiste Fahrzeuge { get; }   // Schritt 24: vor Ort / unterwegs
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 60_000 };
    private DateOnly gezeigtAm;

    public Label HeuteText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label Termine { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label Medikation { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public KartenRaster Raster { get; } = new();
    public CheckBox AlleBereiche { get; } = new() { Text = "alle Bereiche", AutoSize = true, BackColor = Stil.Flaeche, Font = Stil.Standard };
    // Schritt 26e: Filter nach Wohnbereich (R1, R2, R3); „eigene Bereiche“ wie bisher.
    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = Stil.Standard, AccessibleName = "Bereich" };
    public const string EigeneBereiche = "eigene Bereiche";
    // Schritt 20: nur die eigenen Bezugsbewohner bzw. Einzeltrainings (aus allen Bereichen).
    public CheckBox MeineBezugsbewohner { get; } = new() { Text = "Meine Bezugsbewohner", AutoSize = true, BackColor = Stil.Flaeche, Font = Stil.Standard };
    public CheckBox MeineEinzeltrainings { get; } = new() { Text = "Meine Einzeltrainings", AutoSize = true, BackColor = Stil.Flaeche, Font = Stil.Standard };
    public Label BereichHinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, Visible = false, UseMnemonic = false };
    public List<BewohnerKarte> Karten { get; } = [];
    public event Action<long>? AkteGewuenscht;
    public event Action<long>? BerichtGewuenscht;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public UebersichtSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Fahrzeuge = new FahrzeugLeiste(speicher);
        Fahrzeuge.Geaendert += Aktualisieren;
        BereichWahl.Items.Add(EigeneBereiche);
        BereichWahl.Items.AddRange([.. speicher.Wohnbereiche().Select(w => (object)w.Name)]);
        BereichWahl.SelectedIndex = 0;
        BereichWahl.SelectedIndexChanged += (_, _) => Aktualisieren();
        heuteKarte.Controls.AddRange([heuteTitel, BereichWahl, MeineBezugsbewohner, MeineEinzeltrainings, AlleBereiche, BereichHinweis, HeuteText,
            termineTitel, Termine, medikationTitel, Medikation, fahrzeugeTitel, Fahrzeuge]);
        foreach (var c in new[] { AlleBereiche, MeineBezugsbewohner, MeineEinzeltrainings }) c.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(heuteKarte);
        Controls.Add(Raster);
        // Immer aktuell: Datumswechsel um Mitternacht (jede Minute prüfen, nur wenn sichtbar neu bauen).
        uhr.Tick += (_, _) => { if (Visible && DateOnly.FromDateTime(Jetzt()) != gezeigtAm) Aktualisieren(); };
        uhr.Start();
    }

    public string KopfText { get; private set; } = "";

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        gezeigtAm = heute;
        var werte = Einstellungen.Laden();
        int frist = werte.BeschlussFristTage;
        var jeder = speicher.Heute(jetzt, werte.HebFristMonate);
        // Eigene Bereiche: die Wohnbereiche des angemeldeten Mitarbeiters (Schritt 19); hat er keinen
        // mit Bewohnern, der Bereich dieses Rechners. Hat auch der keine Bewohner (z. B. „Therapie“),
        // alle mit Hinweis.
        var meine = speicher.MitarbeiterLesen(werte.Kuerzel)?.Bereiche.Where(b => jeder.Any(h => h.Bewohner.Wohnbereich == b)).ToList()
            is { Count: > 0 } liste ? liste : [werte.Bereich];
        var eigene = jeder.Where(h => meine.Contains(h.Bewohner.Wohnbereich)).ToList();
        bool allesZeigen = AlleBereiche.Checked || eigene.Count == 0;
        var alle = allesZeigen ? jeder : eigene;
        if (BereichWahl.SelectedIndex > 0 && BereichWahl.SelectedItem is string gewaehlt)
        {
            meine = [gewaehlt];
            alle = jeder.Where(h => h.Bewohner.Wohnbereich == gewaehlt).ToList();
            allesZeigen = false;
        }
        AlleBereiche.Enabled = BereichWahl.SelectedIndex <= 0;
        var bezuege = speicher.Bezuege();
        bool angemeldet = speicher.MitarbeiterLesen(werte.Kuerzel) is not null;
        MeineBezugsbewohner.Enabled = MeineEinzeltrainings.Enabled = angemeldet;
        bool meineBp = angemeldet && MeineBezugsbewohner.Checked, meineEt = angemeldet && MeineEinzeltrainings.Checked;
        if (meineBp || meineEt)
        {
            bool Meins(Zuordnung? z) => z?.Kuerzel.Equals(werte.Kuerzel, StringComparison.OrdinalIgnoreCase) == true;
            alle = jeder.Where(h => bezuege.GetValueOrDefault(h.Bewohner.Id!.Value) is Bezug b &&
                                    ((meineBp && Meins(b.Bezugspflege)) || (meineEt && Meins(b.Einzeltrainer)))).ToList();
            allesZeigen = true;
        }
        BereichHinweis.Visible = BereichWahl.SelectedIndex <= 0 && !AlleBereiche.Checked && eigene.Count == 0 && jeder.Count > 0;
        BereichHinweis.Text = $"Für „{string.Join(", ", meine)}“ gibt es keine eigenen Bewohner – gezeigt werden alle Bereiche.";
        string bereichText = meineBp || meineEt
            ? string.Join(" und ", new[] { meineBp ? "meine Bezugsbewohner" : null, meineEt ? "meine Einzeltrainings" : null }.OfType<string>())
            : allesZeigen ? "alle Bereiche" : string.Join(", ", meine);
        var status = speicher.GeltendeStatusmeldungen(jetzt);
        var inTherapie = speicher.InTherapie(jetzt);   // Tagesplan (Schritt 23)
        var mitfahrende = speicher.Mitfahrende(jetzt);  // Fahrten (Schritt 24): abwesend während der Fahrt
        string? berichtGrund = Recht.Grund(speicher, Funktionen.BerichteSchreiben);
        var farben = BezugAnzeige.Farben(speicher);
        Karten.Clear();
        int abwesend = 0;
        foreach (var h in alle)
        {
            var modul = Modulverwaltung.Marken(speicher, h.Bewohner, jetzt);
            if (mitfahrende.TryGetValue(h.Bewohner.Id!.Value, out var fahrt)) modul = [fahrt, .. modul];
            var marken = UebersichtRegeln.Marken(h, heute, frist);
            // Abwesend nur einmal: Meldet ein Modul schon „abwesend“, entfällt die Heimfahrt-Marke.
            if (modul.Any(m => m.Art == MarkenArt.Abwesend)) marken.RemoveAll(m => m.Art == MarkenArt.Abwesend);
            marken.InsertRange(0, modul.Where(m => m.Art == MarkenArt.Abwesend));
            marken.AddRange(modul.Where(m => m.Art != MarkenArt.Abwesend));
            if (marken.Any(m => m.Art == MarkenArt.Abwesend)) abwesend++;
            if (status.TryGetValue(h.Bewohner.Id!.Value, out var s)) marken.Insert(0, StatusmeldungRegeln.Marke(s, jetzt));
            if (inTherapie.TryGetValue(h.Bewohner.Id!.Value, out var wo)) marken.Insert(0, wo);
            if (allesZeigen && !meine.Contains(h.Bewohner.Wohnbereich)) marken.Add(new Marke(MarkenArt.Angabe, h.Bewohner.Wohnbereich));
            var karte = new BewohnerKarte(h.Bewohner, heute, marken);
            karte.Gewaehlt += b => AkteGewuenscht?.Invoke(b.Id!.Value);
            karte.BerichtGewuenscht += b => BerichtGewuenscht?.Invoke(b.Id!.Value);
            karte.BerichtSperren(berichtGrund);
            BezugAnzeige.Setzen(karte, bezuege.GetValueOrDefault(h.Bewohner.Id!.Value), farben);
            Karten.Add(karte);
        }
        Raster.KartenSetzen(Karten);
        heuteTitel.Text = $"HEUTE · {heute.ToString("dddd, dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE")).ToUpperInvariant()} · {bereichText.ToUpperInvariant()}";
        HeuteText.Text = UebersichtRegeln.HeuteText(alle, abwesend);
        var termine = UebersichtRegeln.TerminZeilen(alle);
        Termine.Text = termine.Count == 0 ? "Keine Termine heute." : string.Join(Environment.NewLine, termine);
        var medikation = UebersichtRegeln.MedikationZeilen(alle);
        Medikation.Text = medikation.Count == 0 ? "Keine Änderungen heute." : string.Join(Environment.NewLine, medikation);
        // Schritt 26e: „R2 · 9 Bewohner · 3 Betten frei“ (freie Betten der gezeigten Wohnbereiche).
        var gezeigt = meineBp || meineEt ? [] : allesZeigen ? speicher.Wohnbereiche().Select(w => w.Name).ToList() : meine;
        int frei = gezeigt.Sum(b => speicher.FreieBetten(b).Count);
        string betten = gezeigt.Count == 0 || !speicher.Wohnbereiche().Any(w => gezeigt.Contains(w.Name)) ? ""
            : frei == 1 ? " · 1 Bett frei" : $" · {frei} Betten frei";
        KopfText = $"{bereichText} · {alle.Count} Bewohner{betten} · Stand {jetzt:HH:mm} Uhr";
        Fahrzeuge.Aktualisieren();
        PerformLayout();
        Geaendert?.Invoke();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        // Oben der Bereich „Heute“, darunter das Kartenraster mit eigener Bildlaufleiste.
        int rand = Stil.Px(this, Stil.AbstandGross), innen = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(300, ClientSize.Width - 2 * rand);
        int y = rand;
        heuteTitel.Location = new Point(innen, innen);
        // Rechts oben, von rechts nach links: alle Bereiche, Meine Einzeltrainings, Meine Bezugsbewohner.
        // Reicht die Breite nicht, stehen sie in einer eigenen Zeile unter dem Titel (nichts überdeckt sich).
        var filter = new Control[] { BereichWahl, MeineBezugsbewohner, MeineEinzeltrainings, AlleBereiche };
        static int Breite(Control c) => c is ComboBox ? c.Width : c.PreferredSize.Width;
        static int Hoehe(Control c) => c is ComboBox ? c.Height : c.PreferredSize.Height;
        int abstand = Stil.Px(this, Stil.AbstandMittel);
        int benoetigt = filter.Sum(c => Breite(c)) + (filter.Length - 1) * abstand;
        int unterTitel = heuteTitel.Bottom + Stil.Px(this, Stil.AbstandMini);
        if (heuteTitel.Right + innen + benoetigt <= breite - innen)
        {
            int filterX = breite - innen - benoetigt;
            foreach (var c in filter)
            {
                c.Location = new Point(filterX, heuteTitel.Top + (heuteTitel.Height - Hoehe(c)) / 2);
                filterX += Breite(c) + abstand;
            }
        }
        else
        {
            int filterX = innen, unten = unterTitel;
            foreach (var c in filter)
            {
                if (filterX > innen && filterX + Breite(c) > breite - innen) { filterX = innen; unterTitel = unten; }
                c.Location = new Point(filterX, unterTitel);
                filterX += Breite(c) + abstand;
                unten = Math.Max(unten, c.Top + Hoehe(c) + Stil.Px(this, Stil.AbstandMini));
            }
            unterTitel = unten;
        }
        if (BereichHinweis.Visible)
        {
            BereichHinweis.Location = new Point(innen, unterTitel);
            unterTitel = BereichHinweis.Bottom + Stil.Px(this, Stil.AbstandMini);
        }
        HeuteText.MaximumSize = new Size(breite - 2 * innen, 0);
        HeuteText.Location = new Point(innen, unterTitel);
        int spalte = (breite - 3 * innen) / 2;
        int oben = HeuteText.Bottom + Stil.Px(this, Stil.AbstandMittel);
        termineTitel.Location = new Point(innen, oben);
        Termine.MaximumSize = new Size(spalte, 0);
        Termine.Location = new Point(innen, termineTitel.Bottom + Stil.Px(this, Stil.AbstandMini));
        medikationTitel.Location = new Point(2 * innen + spalte, oben);
        Medikation.MaximumSize = new Size(spalte, 0);
        Medikation.Location = new Point(2 * innen + spalte, medikationTitel.Bottom + Stil.Px(this, Stil.AbstandMini));
        // Fahrzeuge (Schritt 24) über die ganze Breite unter den beiden Spalten.
        fahrzeugeTitel.Location = new Point(innen, Math.Max(Termine.Bottom, Medikation.Bottom) + Stil.Px(this, Stil.AbstandMittel));
        Fahrzeuge.MaximumSize = new Size(breite - 2 * innen, 0);
        Fahrzeuge.Bounds = new Rectangle(new Point(innen, fahrzeugeTitel.Bottom + Stil.Px(this, Stil.AbstandMini)), Fahrzeuge.GetPreferredSize(new Size(breite - 2 * innen, 0)));
        heuteKarte.Bounds = new Rectangle(rand, y, breite, Fahrzeuge.Bottom + innen);
        y = heuteKarte.Bottom + Stil.Px(this, Stil.AbstandKlein);
        Raster.Bounds = new Rectangle(0, y, ClientSize.Width, Math.Max(Stil.Px(this, 120), ClientSize.Height - y));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) uhr.Dispose();
        base.Dispose(disposing);
    }
}
