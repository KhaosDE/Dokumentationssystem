using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Einen HEB-Bogen bearbeiten: je Lebensbereich die Felder der Bogenart mit Zeichenzähler und
// Stilhinweisen, der Hilfebedarf (nie vorbelegt), daneben die Quellen des Zeitraums (Berichte,
// Wochenplan, fertige Reflexionen). Textbausteine werden ins zuletzt gewählte Feld eingefügt.
// Das Programm schreibt keinen Text selbst (Regel 6).
public sealed class HebFenster : Form, IEntwurfFenster
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly Panel inhalt = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Stil.Seite };
    private readonly Dictionary<string, List<Control>> seiten = [];
    private readonly ContextMenuStrip bausteinMenue = new() { Font = Stil.Standard, ShowImageMargin = false };
    private TextBox? zuletzt;

    public HebBogen Bogen { get; private set; }
    public Reiterleiste Reiter { get; }
    public Dictionary<string, TextBox> Felder { get; } = [];
    public Dictionary<string, Label> Zaehler { get; } = [];
    public Dictionary<string, Label> Hinweise { get; } = [];
    public Dictionary<int, RadioButton[]> Hilfebedarf { get; } = [];   // [0] = noch nicht eingeschätzt, [1..6] = Stufe
    public Dictionary<int, Label> Quellen { get; } = [];
    // Zum Nachsehen: die Ziele aus der Akte mit ihren Berichten und Maßnahmen im Zeitraum (Bauplan 43).
    public Label ZielQuellen { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Font = Stil.Klein, UseMnemonic = false };
    public Label Kopf { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Fensterhintergrund, UseMnemonic = false };
    public MarkenFeld Status { get; } = new("Entwurf", Stil.Achtung);
    public Label Meldung { get; } = new() { AutoSize = true, BackColor = Stil.Fensterhintergrund, Visible = false, MaximumSize = new Size(700, 0) };
    public Button BausteinKnopf { get; } = Stil.NebenKnopf("Textbaustein einfügen ▾");
    public Button SpeichernKnopf { get; } = Stil.NebenKnopf("Speichern");
    public Button FertigKnopf { get; } = Stil.HauptKnopf("Als fertig markieren");
    public Button PdfKnopf { get; } = Stil.NebenKnopf("PDF ausfüllen …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");

    // Zum Testen ersetzbar: Ja/Nein-Rückfrage.
    public Func<string, DialogResult> Rueckfrage { get; set; }

    public HebFenster(Speicher speicher, Bewohner bewohner, HebBogen bogen)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Bogen = bogen;
        Rueckfrage = frage => MessageBox.Show(this, frage, "HEB-Bogen", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        Text = $"{HebRegeln.ArtKurz(bogen.Art)} – {bewohner.Name} ({Stil.DemoHinweis})";
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        Size = new Size(1180, 820);
        MinimumSize = new Size(900, 600);

        var reiter = Enumerable.Range(1, 5).Select(b => $"{b} {BerichtRegeln.Kurz[b - 1]}").ToList();
        if (bogen.Art != Bogenart.C) reiter.Add("Ziele");
        reiter.Add("Weiteres");
        Reiter = new Reiterleiste(reiter) { Dock = DockStyle.Top };
        Reiter.Gewechselt += _ => SeiteZeigen();

        // ----- Kopf und Knöpfe -----
        var oben = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(Stil.AbstandGross, Stil.AbstandMittel, Stil.AbstandGross, 0), BackColor = Stil.Fensterhintergrund };
        Status.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandGross, 0);
        oben.Controls.AddRange([Status, Kopf]);
        var unten = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandMittel), BackColor = Stil.Fensterhintergrund };
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.OK;
        foreach (var k in new[] { BausteinKnopf, SpeichernKnopf, FertigKnopf, PdfKnopf, DruckKnopf, schliessen })
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        BausteinKnopf.Width = 220;
        FertigKnopf.Width = 200;
        unten.Controls.AddRange([BausteinKnopf, SpeichernKnopf, FertigKnopf, PdfKnopf, DruckKnopf, schliessen, Meldung]);
        CancelButton = schliessen;

        foreach (var (gruppe, bausteine) in HebBausteine.Alle)
        {
            var eintrag = new ToolStripMenuItem(gruppe);
            foreach (var b in bausteine)
            {
                var unterpunkt = new ToolStripMenuItem(b);
                unterpunkt.Click += (_, _) => BausteinEinfuegen(b);
                eintrag.DropDownItems.Add(unterpunkt);
            }
            bausteinMenue.Items.Add(eintrag);
        }
        BausteinKnopf.Click += (_, _) => bausteinMenue.Show(BausteinKnopf, new Point(0, BausteinKnopf.Height));
        SpeichernKnopf.Click += (_, _) => Speichern(Bogen.Fertig);
        FertigKnopf.Click += (_, _) => Speichern(!Bogen.Fertig);
        PdfKnopf.Click += (_, _) => PdfAusfuellen();
        DruckKnopf.Click += (_, _) => Druckvorschau.Zeigen(this, () => new HebDruck(bewohner, Bogen, Werte()).DokumentBauen(), $"{HebRegeln.ArtKurz(Bogen.Art)} {bewohner.Name}");

        Controls.Add(inhalt);
        Controls.Add(Reiter);
        Controls.Add(oben);
        Controls.Add(unten);
        SeitenBauen();
        Laden();
        SeiteZeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private static Label Abschnitt(string text) => new() { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };

    // Alle Felder einmal anlegen (Eingaben bleiben beim Reiterwechsel erhalten).
    private void SeitenBauen()
    {
        var a = Bogen.Art;
        for (int b = 1; b <= 5; b++)
        {
            var liste = new List<Control> { Abschnitt($"{b}  {BerichtRegeln.Lebensbereiche[b - 1].ToUpperInvariant()}") };
            foreach (var feld in HebRegeln.Felder(a, b)) liste.AddRange(FeldBauen(feld));
            liste.Add(Abschnitt(HebRegeln.HilfebedarfTitel(a).ToUpperInvariant() + " (WIRD NIE VORGESCHLAGEN)"));
            var knoepfe = new RadioButton[7];
            var reihe = new FlowLayoutPanel { AutoSize = true, BackColor = Stil.Seite, WrapContents = true };
            for (int s = 0; s <= 6; s++)
            {
                knoepfe[s] = new RadioButton { Text = s == 0 ? "noch nicht eingeschätzt" : $"Stufe {s}", AutoSize = true, Margin = new Padding(0, 0, Stil.AbstandGross, 0) };
                reihe.Controls.Add(knoepfe[s]);
            }
            Hilfebedarf[b] = knoepfe;
            liste.Add(reihe);
            Quellen[b] = new Label { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Font = Stil.Klein, UseMnemonic = false };
            liste.Add(Abschnitt("QUELLEN IM ZEITRAUM"));
            liste.Add(Quellen[b]);
            seiten[Reiter.Namen[b - 1]] = liste;
        }
        var zusatz = HebRegeln.Zusatzfelder(a);
        if (a != Bogenart.C)
            seiten["Ziele"] = [Abschnitt("LEITZIEL UND NAHZIELE"), .. zusatz.Where(f => f.Schluessel.StartsWith("leitziel") || f.Schluessel.StartsWith("nahziel")).SelectMany(FeldBauen),
                Abschnitt("AUS DER AKTE: ZIELE MIT BERICHTEN UND MASSNAHMEN IM ZEITRAUM"), ZielQuellen];
        seiten["Weiteres"] =
        [
            Abschnitt("WEITERE ANGABEN"), .. zusatz.Where(f => !f.Schluessel.StartsWith("leitziel") && !f.Schluessel.StartsWith("nahziel")).SelectMany(FeldBauen),
            Abschnitt("MERKHILFE (NUR DER MENSCH KANN DAS PRÜFEN)"),
            new Label { Text = string.Join(Environment.NewLine, HebBausteine.Merkhilfe.Select(m => "• " + m)), AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite },
        ];
        foreach (var c in seiten.Values.SelectMany(x => x)) { c.Visible = false; inhalt.Controls.Add(c); }
        inhalt.Layout += (_, _) => Anordnen();
    }

    private IEnumerable<Control> FeldBauen(HebFeld feld)
    {
        var titel = new Label { Text = feld.Titel, AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
        var box = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, Height = feld.Grenze is int g ? Math.Clamp(g / 6, 90, 180) : 90 };
        var zaehler = new Label { AutoSize = true, Font = Stil.Klein, BackColor = Stil.Seite };
        var hinweise = new Label { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini), Visible = false };
        box.Enter += (_, _) => zuletzt = box;
        box.TextChanged += (_, _) => Pruefen(feld);
        Felder[feld.Schluessel] = box;
        Zaehler[feld.Schluessel] = zaehler;
        Hinweise[feld.Schluessel] = hinweise;
        box.Tag = feld;
        return [titel, box, zaehler, hinweise];
    }

    private void Pruefen(HebFeld feld)
    {
        var box = Felder[feld.Schluessel];
        var (text, ueber) = HebRegeln.Zaehler(box.Text, feld.Grenze);
        Zaehler[feld.Schluessel].Text = text + (ueber ? " – zu lang für den Bogen" : "");
        Zaehler[feld.Schluessel].ForeColor = ueber ? Stil.Gefahr : Stil.TextLeise;
        var h = Stilpruefung.Pruefen(box.Text, feld.Art);
        Hinweise[feld.Schluessel].Text = string.Join(Environment.NewLine, h.Select(x => "Hinweis: " + x.Text));
        bool zeigen = h.Count > 0;
        Hinweise[feld.Schluessel].Tag = zeigen;
        Hinweise[feld.Schluessel].Visible = zeigen && seiten[Reiter.Aktiv].Contains(Hinweise[feld.Schluessel]);
        inhalt.PerformLayout();
    }

    private void Laden()
    {
        var f = Bogen.Aktuell;
        foreach (var (k, box) in Felder) box.Text = f?.Text(k) ?? "";
        for (int b = 1; b <= 5; b++)
        {
            int? stufe = f?.Hilfebedarf.GetValueOrDefault(b);
            Hilfebedarf[b][stufe ?? 0].Checked = true;
        }
        bool fertig = Bogen.Fertig;
        foreach (var box in Felder.Values) { box.ReadOnly = fertig; box.BackColor = fertig ? Stil.Fensterhintergrund : Stil.Flaeche; }
        foreach (var r in Hilfebedarf.Values.SelectMany(x => x)) r.Enabled = !fertig;
        Status.Text = fertig ? "fertig" : "Entwurf";
        Status.Marke = fertig ? Stil.Offen : Stil.Achtung;
        FertigKnopf.Text = fertig ? "Wieder als Entwurf" : "Als fertig markieren";
        SpeichernKnopf.Enabled = !fertig;
        BausteinKnopf.Enabled = !fertig;
        Recht.Anwenden(speicher, Funktionen.Heb, SpeichernKnopf, FertigKnopf);   // Rechte
        Kopf.Text = $"{HebRegeln.ArtText(Bogen.Art)}" + (Bogen.Nummer is int n ? $" · Nr. {n}" : "") +
                    $" · Zeitraum {HebRegeln.ZeitraumText(Bogen)}{Environment.NewLine}" +
                    $"{bewohner.Name} · Aufnahme {Datum.Text(bewohner.Aufnahmedatum)} · Beschluss: {BewohnerRegeln.BeschlussText(bewohner)}" +
                    (Bogen.Art == Bogenart.C ? "" : $" · {HebRegeln.PlanungText(HebRegeln.Planungsmonate(bewohner, Bogen.Erstellt))}") +
                    // Diagnosen nur als Information und nur mit Recht
                    (Recht.Darf(speicher, Funktionen.AerztlicheAnsehen) ? $"{Environment.NewLine}Diagnosen: {DiagnoseRegeln.Kurz(speicher.Diagnosen(bewohner.Id!.Value))}" : "");
        QuellenLaden();
    }

    private void QuellenLaden()
    {
        long id = bewohner.Id!.Value;
        var plan = speicher.HebWochenplan(id, Bogen.Bis);
        string planText = plan.Count == 0 ? "Wochenplan: keine Einträge."
            : "Wochenplan (Woche am Ende des Zeitraums): " + string.Join(" · ", plan.GroupBy(t => t.Titel).Select(g => $"{g.Key} ({g.Count()}×)"));
        for (int b = 1; b <= 5; b++)
        {
            var berichte = speicher.HebBerichte(id, Bogen.Von, Bogen.Bis, b);
            var reflexionen = speicher.HebReflexionen(id, Bogen.Von, Bogen.Bis, b);
            var zeilen = new List<string> { berichte.Count == 0 ? "Keine Berichte zu diesem Bereich im Zeitraum." : $"Berichte ({berichte.Count}):" };
            zeilen.AddRange(berichte.OrderBy(x => x.Zeitpunkt).TakeLast(12).Select(x =>
            {
                string t = x.Text.ReplaceLineEndings(" ");
                return $"  {x.Zeitpunkt:dd.MM.yy} {x.Verfasser}: {(t.Length > 160 ? t[..159] + "…" : t)}";
            }));
            if (berichte.Count > 12) zeilen.Add($"  … und {berichte.Count - 12} ältere (in der Akte unter „Berichte“).");
            zeilen.Add(reflexionen.Count == 0 ? "Keine fertigen Monatsreflexionen zu diesem Bereich." : "Monatsreflexionen:");
            zeilen.AddRange(reflexionen.Select(r => $"  {ReflexionRegeln.MonatText(r.Monat)}: {r.Text}"));
            zeilen.Add(planText);
            Quellen[b].Text = string.Join(Environment.NewLine, zeilen);
        }
        var ziele = ZielRegeln.Zusammenfassung(speicher.Ziele(id), speicher.Berichte(id, new BerichtFilter(Bogen.Von, Bogen.Bis)),
            speicher.Massnahmen(id, auchBeendete: true), speicher.MassnahmeBestaetigungen(id, Bogen.Von, Bogen.Bis));
        ZielQuellen.Text = ziele.Count == 0 ? "In der Akte sind keine aktiven Ziele eingetragen." : string.Join(Environment.NewLine, ziele);
    }

    public void SeiteZeigen()
    {
        foreach (var (name, liste) in seiten)
            foreach (var c in liste)
                c.Visible = name == Reiter.Aktiv && (c.Tag is not bool zeigen || zeigen);
        foreach (var feld in HebRegeln.AlleTextfelder(Bogen.Art)) Pruefen(feld);
        inhalt.AutoScrollPosition = Point.Empty;
        inhalt.PerformLayout();
    }

    public void ReiterWaehlen(string name) => Reiter.Waehlen(name);

    private void Anordnen()
    {
        if (!seiten.TryGetValue(Reiter.Aktiv, out var liste)) return;
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(300, inhalt.ClientSize.Width - 2 * rand);
        int y = rand + inhalt.AutoScrollPosition.Y;
        foreach (var c in liste.Where(c => c.Visible))
        {
            if (c is TextBox box) box.SetBounds(rand, y, breite, box.Height);
            else
            {
                if (c is Label l) l.MaximumSize = new Size(breite, 0);
                if (c is FlowLayoutPanel f) f.MaximumSize = new Size(breite, 0);
                c.Location = new Point(rand, y + (c.Font == Stil.Abschnitt ? Stil.Px(this, Stil.AbstandMittel) : 0));
            }
            y = c.Bottom + Stil.Px(this, Stil.AbstandMini);
        }
        inhalt.AutoScrollMinSize = new Size(0, y - inhalt.AutoScrollPosition.Y + rand);
    }

    // Baustein an der Schreibmarke des zuletzt gewählten Feldes einfügen.
    public bool BausteinEinfuegen(string baustein)
    {
        if (zuletzt is null || zuletzt.ReadOnly)
        {
            MeldungZeigen("Erst in ein Feld klicken, dann den Baustein wählen.", Stil.Achtung);
            return false;
        }
        int stelle = zuletzt.SelectionStart;
        string vorher = zuletzt.Text[..stelle];
        string einfuegen = (vorher.Length > 0 && !char.IsWhiteSpace(vorher[^1]) ? " " : "") + baustein;
        zuletzt.Text = vorher + einfuegen + zuletzt.Text[stelle..];
        zuletzt.SelectionStart = stelle + einfuegen.Length;
        zuletzt.Focus();
        return true;
    }

    public void FeldWaehlen(string schluessel) => zuletzt = Felder[schluessel];

    private Dictionary<string, string> Texte() => Felder.ToDictionary(x => x.Key, x => x.Value.Text);

    private Dictionary<int, int?> Stufen() =>
        Hilfebedarf.ToDictionary(x => x.Key, x => Array.FindIndex(x.Value, r => r.Checked) is int i and > 0 ? (int?)i : null);

    private Dictionary<string, string> Werte() =>
        HebPdf.Werte(Bogen, bewohner, Bogen.Aktuell is HebFassung f && !Geaendert ? f
            : new HebFassung { Texte = Texte(), Hilfebedarf = Stufen() },
            speicher.Angaben(bewohner.Id!.Value, DateOnly.FromDateTime(DateTime.Today)).GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>());

    public bool Geaendert => Bogen.Aktuell is not HebFassung f
        ? Felder.Values.Any(b => b.Text.Trim().Length > 0) || Stufen().Values.Any(s => s is not null)
        : Felder.Any(x => x.Value.Text.Trim() != f.Text(x.Key)) || Stufen().Any(x => x.Value != f.Hilfebedarf.GetValueOrDefault(x.Key));

    public bool Speichern(bool fertig) => Speichern(fertig, ohnePruefung: false);

    // Beim Abmelden bleibt der Bogen als Entwurf (wie beim Schließen mit „Ja“).
    public bool HatEingaben => Geaendert && !Bogen.Fertig;
    public bool AlsEntwurfSpeichern() => Speichern(false);

    private bool Speichern(bool fertig, bool ohnePruefung)
    {
        try
        {
            var f = HebRegeln.AusEingabe(Bogen.Id, Bogen.Art, Texte(), Stufen(), fertig, Recht.Kuerzel, DateTime.Now);
            if (fertig && HebRegeln.ZuLang(Bogen.Art, f) is { Count: > 0 } lang &&
                Rueckfrage($"Diese Felder sind länger als im Bogen vorgesehen:\n{string.Join("\n", lang)}\n\nTrotzdem als fertig markieren?") != DialogResult.Yes)
                return false;
            Bogen = speicher.HebSpeichern(f, ohnePruefung ? null : Bogen.Fassungen.Count);
        }
        catch (Konflikt k)
        {
            switch (KonfliktFenster.Fragen(this, k))
            {
                case KonfliktWahl.MeineSpeichern:
                    return Speichern(fertig, ohnePruefung: true);
                case KonfliktWahl.AndereUebernehmen:
                    Bogen = speicher.HebBogen(Bogen.Id) ?? Bogen;
                    Laden();
                    SeiteZeigen();
                    MeldungZeigen("Die Fassung des anderen Arbeitsplatzes ist übernommen.", Stil.Offen);
                    return false;
                default:
                    MeldungZeigen("Nicht gespeichert – ein anderer Arbeitsplatz hat den Bogen inzwischen geändert.", Stil.Achtung);
                    return false;
            }
        }
        catch (Eingabefehler e)
        {
            MeldungZeigen(string.Join(" ", e.Fehler), Stil.Beschluss);
            return false;
        }
        Laden();
        SeiteZeigen();
        MeldungZeigen(fertig ? "Als fertig gespeichert." : "Entwurf gespeichert.", Stil.Offen);
        return true;
    }

    // Kopie der zugeteilten PDF ausfüllen. Ohne Vorlage: Hinweis auf die Einstellungen.
    public string? PdfAusfuellen()
    {
        if (HebPdf.Vorlage(Bogen.Art) is not string vorlage)
        {
            MeldungZeigen($"Für {HebRegeln.ArtKurz(Bogen.Art)} ist keine PDF-Vorlage zugeteilt (Einstellungen → HEB-VORLAGEN). Speichern und Drucken gehen trotzdem.", Stil.Achtung);
            return null;
        }
        if (Geaendert && !Speichern(Bogen.Fertig)) return null;
        try
        {
            var (pfad, ohne) = HebPdf.Ausfuellen(vorlage, HebPdf.AusgabePfad(bewohner, Bogen), Werte(), HebPdf.Zuordnung(Bogen.Art));
            MeldungZeigen($"PDF gespeichert: {pfad}" + (ohne.Count > 0 ? $" – ohne passendes PDF-Feld: {string.Join(", ", ohne)} (Einstellungen → Zuordnung …)" : ""), Stil.Offen);
            return pfad;
        }
        catch (Exception e) when (e is Eingabefehler or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MeldungZeigen($"PDF ließ sich nicht ausfüllen: {(e is Eingabefehler f ? string.Join(" ", f.Fehler) : e.Message)}", Stil.Beschluss);
            return null;
        }
    }

    private void MeldungZeigen(string text, Stil.Marke marke)
    {
        Meldung.Text = text;
        Meldung.ForeColor = marke.Text;
        Meldung.Visible = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel && !Abmeldung.SchliesstFenster && Geaendert && !Bogen.Fertig && Rueckfrage("Ungespeicherte Änderungen als Entwurf speichern?") == DialogResult.Yes)
            Speichern(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) bausteinMenue.Dispose();
        base.Dispose(disposing);
    }
}

// Ausdruck der eigenen Maske (ohne PDF-Vorlage): alle Felder mit Überschrift, über mehrere Seiten.
public sealed class HebDruck(Bewohner bewohner, HebBogen bogen, IReadOnlyDictionary<string, string> werte)
{
    private int naechster;

    public PrintDocument DokumentBauen()
    {
        naechster = 0;
        var dokument = new PrintDocument { DocumentName = $"{HebRegeln.ArtKurz(bogen.Art)} {bewohner.Name}" };
        dokument.PrintPage += (_, e) => e.HasMorePages = SeiteZeichnen(e.Graphics!);
        return dokument;
    }

    // Abschnitte: (Überschrift, Text) in der Reihenfolge des Bogens.
    public List<(string Titel, string Text)> Abschnitte()
    {
        var liste = new List<(string, string)>();
        for (int b = 1; b <= 5; b++)
        {
            foreach (var f in HebRegeln.Felder(bogen.Art, b))
                liste.Add(($"{b} {BerichtRegeln.Kurz[b - 1]} – {f.Titel}", werte.GetValueOrDefault(f.Schluessel, "")));
            liste.Add(($"{b} {BerichtRegeln.Kurz[b - 1]} – {HebRegeln.HilfebedarfTitel(bogen.Art)}",
                HebRegeln.HilfebedarfText(int.TryParse(werte.GetValueOrDefault($"hilfe{b}", ""), out int s) ? s : null)));
        }
        foreach (var f in HebRegeln.Zusatzfelder(bogen.Art)) liste.Add((f.Titel, werte.GetValueOrDefault(f.Schluessel, "")));
        return liste;
    }

    // Zeichnet eine Seite; true = es folgt noch eine.
    public bool SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, breite = 170f, unten = 280f;
        float y = 15f;
        using var titel = new Font("Segoe UI Semibold", 13F);
        using var fett = new Font("Segoe UI Semibold", 9.5F);
        using var normal = new Font("Segoe UI", 9.5F);
        using var klein = new Font("Segoe UI", 8F);
        if (naechster == 0)
        {
            g.DrawString($"{HebRegeln.ArtText(bogen.Art)} – {bewohner.Name}", titel, Brushes.Black, links, y);
            y += 7f;
            g.DrawString($"Zeitraum {HebRegeln.ZeitraumText(bogen)}" + (bogen.Nummer is int n ? $" · Nr. {n}" : "") +
                         $" · {(bogen.Fertig ? "fertig" : "Entwurf")} · {Stil.DemoHinweis}", klein, Brushes.DimGray, links, y);
            y += 8f;
        }
        var abschnitte = Abschnitte();
        while (naechster < abschnitte.Count)
        {
            var (t, text) = abschnitte[naechster];
            string inhaltText = text.Length > 0 ? text : "–";
            var hoehe = g.MeasureString(inhaltText, normal, (int)breite).Height;
            if (y + 6f + hoehe > unten && y > 30f) return true;   // nächste Seite
            g.DrawString(t, fett, Brushes.Black, links, y);
            y += 5.5f;
            g.DrawString(inhaltText, normal, Brushes.Black, new RectangleF(links, y, breite, hoehe + 1f));
            y += hoehe + 4f;
            naechster++;
        }
        naechster = 0;
        return false;
    }
}
