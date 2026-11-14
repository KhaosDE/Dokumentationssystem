using DokuLine.Kern;

namespace DokuLine;

// Rechts im Bereich „Dokumentation“: Filterleiste, gesetzte Filter als Marken mit „×“, gespeicherte Filter und die
// Berichte des gewählten Bewohners nach Tagen (neueste oben). Der Filter bleibt beim Wechsel des Bewohners stehen.
// Die kurzen Einträge bestätigter Maßnahmen stehen je Tag in einer Zeile zusammengefasst, zum Aufklappen.
public sealed class DokumentationAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly FlowLayoutPanel weitere = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly FlowLayoutPanel gespeichert = new()
    {
        AutoSize = true, WrapContents = true,
        BackColor = Stil.Seite
    };
    private readonly FlowLayoutPanel gesetzt = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly Panel liste = new() { AutoScroll = true, BackColor = Stil.Seite };
    private readonly Label leer = new()
    {
        AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite,
        Text = "Keine Berichte für diese Auswahl."
    };
    private bool fuellt;

    public DokuFilter Filter { get; private set; } = new();
    public bool Kompakt { get; private set; } = true;
    public Bewohner? Bewohner { get; private set; }
    public List<Bericht> Berichte { get; private set; } = [];
    public List<BerichtZeile> Zeilen { get; } = [];
    public List<TagKopf> Tage { get; } = [];
    public List<MassnahmenTagZeile> MassnahmenZeilen { get; } = [];
    // Zielleiste (Bauplan 43): beim Durchgehen der Bewohner sofort sehen, was jemand erreichen will.
    public ZielLeiste Zielleiste { get; } = new();

    public ComboBox Zeitraum { get; } = Auswahl(140, "Zeitraum", "Heute", "7 Tage", "30 Tage", "frei …");
    public DatumFeld Von { get; } = new() { Width = 100, PlaceholderText = "von", Visible = false };
    public DatumFeld Bis { get; } = new() { Width = 100, PlaceholderText = "bis", Visible = false };
    public FilterKnopf WichtigKnopf { get; } = new("Wichtig", mitZahl: false);
    public FilterKnopf MedikationKnopf { get; } = new("Medikation geändert", mitZahl: false);
    public FilterKnopf BedarfKnopf { get; } = new("Bedarfsgabe", mitZahl: false);
    public FilterKnopf VisiteKnopf { get; } = new("Visite", mitZahl: false);
    public CheckBox Uebergabe { get; } = new() { Text = "Übergabe", AutoSize = true, BackColor = Stil.Seite };
    public CheckBox MitNachtrag { get; } = new() { Text = "mit Nachtrag", AutoSize = true, BackColor = Stil.Seite };
    public CheckBox Massnahmen { get; } = new() { Text = "Maßnahmen", AutoSize = true, BackColor = Stil.Seite, Checked = true };
    public ComboBox Art { get; } = Auswahl(190, "Berichtsart", "alle Berichtsarten");
    public ComboBox Bereich { get; } = Auswahl(190, "Lebensbereich",
        ["alle Lebensbereiche", .. Enumerable.Range(1, 5).Select(BerichtRegeln.BereichText), "allgemein"]);
    public ComboBox Schichtwahl { get; } =
        Auswahl(140, "Schicht", "alle Schichten", "Frühdienst", "Spätdienst", "Nachtdienst");
    public ComboBox Verfasser { get; } = Auswahl(140, "Verfasser", "alle Verfasser");
    public TextBox Suche { get; } = new()
    {
        Width = 200, PlaceholderText = "Im Text suchen", Font = Stil.Standard, AccessibleName = "Im Text suchen",
    };
    public Button SpeichernKnopf { get; } = Stil.NebenKnopf("Filter speichern …");
    public Button ZuruecksetzenKnopf { get; } = Stil.NebenKnopf("Filter zurücksetzen");
    public Label Farbschluessel { get; } = new()
    {
        AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Seite,
        Text = "Rand: Früh · Spät · Nacht",
    };

    // Name eines neuen gespeicherten Filters; zum Testen ersetzbar.
    public Func<string?> NameFragen { get; set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public DokumentationAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        NameFragen = () =>
            Eingabe.Fragen(FindForm(), "Filter speichern", "Name des Filters (z. B. „Krisen der letzten 30 Tage“)");
        Art.Items.AddRange([.. speicher.Berichtsarten()]);
        Control[] oben = [Zeitraum, Von, Bis, WichtigKnopf, MedikationKnopf, BedarfKnopf, VisiteKnopf, Suche, Farbschluessel];
        Control[] unten = [Uebergabe, MitNachtrag, Massnahmen, Art, Bereich, Schichtwahl, Verfasser];
        foreach (var c in oben.Concat(unten)) c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        leiste.Controls.AddRange(oben);
        weitere.Controls.AddRange(unten);
        SpeichernKnopf.Click += (_, _) => FilterSpeichern();
        ZuruecksetzenKnopf.Click += (_, _) => FilterSetzen(new DokuFilter());
        foreach (var box in new[] { Zeitraum, Art, Bereich, Schichtwahl, Verfasser })
            box.SelectedIndexChanged += (_, _) => AusFeldern();
        foreach (var c in new[] { Uebergabe, MitNachtrag, Massnahmen }) c.CheckedChanged += (_, _) => AusFeldern();
        foreach (var k in new[] { WichtigKnopf, MedikationKnopf, BedarfKnopf, VisiteKnopf })
            k.Click += (_, _) => { k.Aktiv = !k.Aktiv; AusFeldern(); };
        Suche.TextChanged += (_, _) => AusFeldern();
        foreach (var d in new[] { Von, Bis }) d.Leave += (_, _) => AusFeldern();
        Farbschluessel.Paint += (_, e) => FarbschluesselZeichnen(e.Graphics);
        liste.Controls.Add(leer);
        liste.Layout += (_, _) => ListeAnordnen();
        Controls.AddRange([leiste, weitere, gespeichert, gesetzt, Zielleiste, liste]);
        FelderSetzen();
    }

    private static ComboBox Auswahl(int breite, string name, params string[] eintraege)
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Standard, Width = breite, AccessibleName = name,
        };
        box.Items.AddRange(eintraege);
        box.SelectedIndex = eintraege.Length > 1 && name == "Zeitraum" ? 1 : 0;
        return box;
    }

    public void Zeigen(Bewohner b)
    {
        Bewohner = b;
        Aktualisieren();
    }

    public void FilterSetzen(DokuFilter f)
    {
        Filter = f;
        FelderSetzen();
        Aktualisieren();
    }

    public void KompaktSetzen(bool kompakt)
    {
        Kompakt = kompakt;
        Aktualisieren();
    }

    // Die Felder zeigen den Filter (ohne dabei neu zu filtern).
    private void FelderSetzen()
    {
        fuellt = true;
        try
        {
            var f = Filter;
            Zeitraum.SelectedIndex = (int)f.Zeitraum;
            Von.Visible = Bis.Visible = f.Zeitraum == Kern.Zeitraum.Frei;
            Von.Text = f.Von is DateOnly v ? Datum.Text(v) : "";
            Bis.Text = f.Bis is DateOnly bis ? Datum.Text(bis) : "";
            (WichtigKnopf.Aktiv, MedikationKnopf.Aktiv, BedarfKnopf.Aktiv, VisiteKnopf.Aktiv) =
                (f.Wichtig, f.MedikationGeaendert, f.Bedarfsgabe, f.Visite);
            (Uebergabe.Checked, MitNachtrag.Checked, Massnahmen.Checked) = (f.Uebergabe, f.MitNachtrag, !f.OhneMassnahmen);
            Art.SelectedIndex = f.Art is string art && Art.Items.IndexOf(art) is int i && i > 0 ? i : 0;
            Bereich.SelectedIndex = f.Bereich switch { null => 0, BerichtFilter.Allgemein => 6, int n => n };
            Schichtwahl.SelectedIndex = f.Schicht switch
            {
                Schicht.Frueh => 1,
                Schicht.Spaet => 2,
                Schicht.Nacht => 3,
                _ => 0
            };
            if (f.Verfasser is string vf && !Verfasser.Items.Contains(vf)) Verfasser.Items.Add(vf);
            Verfasser.SelectedIndex = f.Verfasser is string vv ? Verfasser.Items.IndexOf(vv) : 0;
            if (Suche.Text != f.Suche) Suche.Text = f.Suche;
        }
        finally { fuellt = false; }
    }

    // Der Filter aus den Feldern.
    private void AusFeldern()
    {
        if (fuellt) return;
        static DateOnly? Lesen(TextBox t)
        {
            try { return t.Text.Trim().Length > 0 ? Datum.Lesen(t.Text, "Datum") : null; }
            catch (FormatException) { return null; }
        }
        Filter = new DokuFilter
        {
            Zeitraum = (Kern.Zeitraum)Math.Max(0, Zeitraum.SelectedIndex), Von = Lesen(Von), Bis = Lesen(Bis),
            Wichtig = WichtigKnopf.Aktiv, MedikationGeaendert = MedikationKnopf.Aktiv, Bedarfsgabe = BedarfKnopf.Aktiv,
            Visite = VisiteKnopf.Aktiv, Uebergabe = Uebergabe.Checked, MitNachtrag = MitNachtrag.Checked, OhneMassnahmen = !Massnahmen.Checked,
            Art = Art.SelectedIndex > 0 ? Art.SelectedItem as string : null,
            Bereich = Bereich.SelectedIndex switch { 0 => null, 6 => BerichtFilter.Allgemein, int n => n },
            Schicht = Schichtwahl.SelectedIndex switch
            {
                1 => Schicht.Frueh,
                2 => Schicht.Spaet,
                3 => Schicht.Nacht,
                _ => null
            },
            Verfasser = Verfasser.SelectedIndex > 0 ? Verfasser.SelectedItem as string : null,
            Suche = Suche.Text,
        };
        Von.Visible = Bis.Visible = Filter.Zeitraum == Kern.Zeitraum.Frei;
        Aktualisieren();
    }

    public void Aktualisieren()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        Berichte = Bewohner?.Id is long id ? speicher.Berichte(id, Filter, heute) : [];
        Zielleiste.Visible = Bewohner is not null;
        if (Bewohner?.Id is long bid) Zielleiste.Zeigen(speicher, bid, heute);
        if (Bewohner?.Id is long b)
        {
            fuellt = true;
            string? gewaehlt = Verfasser.SelectedItem as string;
            foreach (var k in speicher.Berichte(b).Select(x => x.Verfasser).Distinct().Order())
                if (!Verfasser.Items.Contains(k)) Verfasser.Items.Add(k);
            if (gewaehlt is not null) Verfasser.SelectedItem = gewaehlt;
            fuellt = false;
        }
        liste.SuspendLayout();
        foreach (Control c in Zeilen.Cast<Control>().Concat(Tage).Concat(MassnahmenZeilen)) { liste.Controls.Remove(c); c.Dispose(); }
        Zeilen.Clear();
        Tage.Clear();
        MassnahmenZeilen.Clear();
        BerichtZeile Zeile(Bericht bericht, bool sichtbar)
        {
            var zeile = new BerichtZeile(bericht, Filter.Suche, Kompakt) { ContextMenuStrip = Menue(bericht), Visible = sichtbar };
            zeile.Umgeschaltet += _ => liste.PerformLayout();
            Zeilen.Add(zeile);
            liste.Controls.Add(zeile);
            return zeile;
        }
        foreach (var (tag, berichte) in DokuRegeln.NachTagen(Berichte))
        {
            var (geschrieben, massnahmen) = DokuRegeln.Teilen(berichte);
            var kopf = new TagKopf(tag, geschrieben.Count);
            Tage.Add(kopf);
            liste.Controls.Add(kopf);
            foreach (var bericht in geschrieben) Zeile(bericht, true);
            if (massnahmen.Count == 0) continue;
            var summe = new MassnahmenTagZeile(MassnahmenRegeln.Zusammenfassung(massnahmen.Select(b => b.Text)));
            MassnahmenZeilen.Add(summe);
            liste.Controls.Add(summe);
            var einzeln = massnahmen.Select(b => Zeile(b, false)).ToList();
            summe.Umgeschaltet += offen => { foreach (var z in einzeln) z.Visible = offen; liste.PerformLayout(); };
        }
        leer.Visible = Berichte.Count == 0;
        liste.ResumeLayout(false);
        liste.PerformLayout();
        GesetztZeigen();
        GespeichertZeigen();
        PerformLayout();
    }

    private ContextMenuStrip Menue(Bericht b)
    {
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        var nachtrag = menue.Items.Add("Nachtrag schreiben …", null, (_, _) => NachtragSchreiben(b));
        string? grund = Recht.Grund(speicher, Funktionen.BerichteSchreiben);
        nachtrag.Enabled = grund is null;
        nachtrag.Visible = !RechteRegeln.FehltRecht(grund);
        menue.Items.Add("Aufgabe daraus machen …", null, (_, _) => AufgabeDaraus(b));
        return menue;
    }

    private void GesetztZeigen()
    {
        foreach (Control c in gesetzt.Controls.Cast<Control>().ToList()) { if (c != ZuruecksetzenKnopf) c.Dispose(); }
        gesetzt.Controls.Clear();
        foreach (var (text, ohne) in DokuRegeln.Gesetzt(Filter))
        {
            var k = Stil.NebenKnopf($"{text} ×");
            k.AccessibleName = $"Filter „{text}“ entfernen";
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            k.Click += (_, _) => FilterSetzen(ohne);
            gesetzt.Controls.Add(k);
        }
        if (gesetzt.Controls.Count > 0) gesetzt.Controls.Add(ZuruecksetzenKnopf);
        gesetzt.Visible = gesetzt.Controls.Count > 0;
    }

    private void GespeichertZeigen()
    {
        foreach (Control c in gespeichert.Controls.Cast<Control>().ToList()) { if (c != SpeichernKnopf) c.Dispose(); }
        gespeichert.Controls.Clear();
        foreach (var f in speicher.GespeicherteFilter(Recht.Kuerzel))
        {
            var k = Stil.NebenKnopf(f.Name);
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            k.Click += (_, _) => FilterSetzen(f.Filter);
            var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
            menue.Items.Add("Gespeicherten Filter löschen", null, (_, _) =>
            {
                speicher.FilterLoeschen(Recht.Kuerzel, f.Name);
                GespeichertZeigen();
                PerformLayout();
            });
            k.ContextMenuStrip = menue;
            gespeichert.Controls.Add(k);
        }
        SpeichernKnopf.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        Stil.Sperren(SpeichernKnopf, Recht.Angemeldet ? null : RechteRegeln.NichtAngemeldetText);
        gespeichert.Controls.Add(SpeichernKnopf);
    }

    public bool FilterSpeichern()
    {
        if (!Recht.Angemeldet || NameFragen() is not string name || name.Trim().Length == 0) return false;
        try { speicher.FilterSpeichern(Recht.Kuerzel, name, Filter); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Filter speichern", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }
        GespeichertZeigen();
        PerformLayout();
        return true;
    }

    // Vorgemerkte nächste Schritte aus dem Bericht (Bauplan 48); die Akte bzw. das Zielformular öffnet der Bereich.
    public event Action<Bericht, IReadOnlyList<Vorschlag>>? FolgeschritteGewuenscht;

    public bool NeuerBericht()
    {
        if (Bewohner is null || Recht.Grund(speicher, Funktionen.BerichteSchreiben) is not null) return false;
        using var formular = new BerichtFormular(speicher, Bewohner);
        bool ok = Dialog.Zeigen(formular, FindForm()) == DialogResult.OK;
        if (ok) Aktualisieren();
        if (ok && formular.Gespeichert is Bericht b && formular.Folgeschritte.Count > 0) FolgeschritteGewuenscht?.Invoke(b, [.. formular.Folgeschritte]);
        return ok;
    }

    public void NachtragSchreiben(Bericht b)
    {
        using var formular = new NachtragFormular(speicher, b);
        if (Dialog.Zeigen(formular, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void AufgabeDaraus(Bericht b)
    {
        using var fenster = new AufgabeFenster(speicher, b.BewohnerId, b.Id);
        Dialog.Zeigen(fenster, FindForm());
    }

    // Druckt, was der Filter zeigt.
    public void Drucken()
    {
        if (Bewohner is null) return;
        string auswahl = string.Join(", ", DokuRegeln.Gesetzt(Filter).Select(g => g.Text).DefaultIfEmpty("7 Tage"));
        var druck = new BerichteDruck(Bewohner, Berichte, auswahl);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Berichte {Bewohner.Name}");
    }

    private void FarbschluesselZeichnen(Graphics g)
    {
        // Die Wörter in ihrer Schichtfarbe über den grauen Text malen.
        int x = TextRenderer.MeasureText(g, "Rand: ", Farbschluessel.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
        var flags = TextFormatFlags.NoPadding;
        foreach (var (wort, farbe) in new[] { ("Früh", Stil.SchichtFrueh), ("Spät", Stil.SchichtSpaet), ("Nacht", Stil.SchichtNacht) })
        {
            TextRenderer.DrawText(g, wort, Stil.MarkeSchrift, new Point(x, 0), farbe, Stil.Seite, flags);
            x += TextRenderer.MeasureText(g, wort + " · ", Farbschluessel.Font, Size.Empty, flags).Width;
        }
    }

    private void ListeAnordnen()
    {
        int breite = liste.ClientSize.Width - SystemInformation.VerticalScrollBarWidth, y = liste.AutoScrollPosition.Y;
        int abstand = Stil.Px(this, Stil.AbstandMini), tagAbstand = Stil.Px(this, Stil.AbstandMittel);
        leer.Location = new Point(0, y);
        foreach (Control c in liste.Controls)
        {
            if (!c.Visible && c != leer) continue;
            if (c is MassnahmenTagZeile m)
            {
                m.SetBounds(Stil.Px(this, Stil.AbstandMittel), y, breite - Stil.Px(this, Stil.AbstandMittel), m.PreferredHeight);
                y = m.Bottom + abstand;
            }
            else if (c is TagKopf t)
            {
                if (c != liste.Controls.OfType<TagKopf>().FirstOrDefault()) y += tagAbstand;
                t.SetBounds(0, y, breite, t.Hoehe);
                y = t.Bottom + abstand;
            }
            else if (c is BerichtZeile z)
            {
                int h = z.Anordnen(breite);
                z.SetBounds(0, y, breite, h);
                y = z.Bottom + abstand;
            }
        }
        liste.AutoScrollMinSize = new Size(0, y - liste.AutoScrollPosition.Y);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = ClientSize.Width, y = 0;
        foreach (var teil in new Control[] { leiste, weitere, gespeichert, gesetzt })
        {
            if (!teil.Visible) continue;
            teil.MaximumSize = new Size(breite, 0);
            teil.Location = new Point(0, y);
            y = teil.Bottom;
        }
        if (Zielleiste.Visible)
        {
            Zielleiste.SetBounds(0, y, breite, Zielleiste.HoeheFuer(breite));
            y = Zielleiste.Bottom + Stil.Px(this, Stil.AbstandMini);
        }
        liste.SetBounds(0, y, breite, Math.Max(Stil.Px(this, 120), ClientSize.Height - y));
    }
}
