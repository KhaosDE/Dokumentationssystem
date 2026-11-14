using DokuLine.Kern;

namespace DokuLine;

// Bereich „Bestellung“ (Bauplan 51, Recht „Medikamente ausgeben“): oben, was bestellt werden kann (mit Bestand: was in den
// nächsten n Tagen ausgeht), nach Empfänger; ankreuzen, Menge anpassen, „Anforderung erstellen …“. Unten die Anforderungen
// mit Verlauf: Ausdruck oder E-Mail, „Rezept da“, „geliefert“ (Mengen einbuchen), „erledigt“.
public sealed class BestellungSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Bestellung";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [ErstellenKnopf, MengeKnopf, ZaehlenKnopf, DruckKnopf, EmailKnopf, WeiterKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    private readonly Dictionary<long, string> mengen = [];
    public Label Hinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, UseMnemonic = false };
    public ListView Liste { get; } = Tabelle("Zu bestellen", ("Bewohner", 170), ("Medikament", 220), ("Vorrat", 70), ("Reicht bis", 100), ("Empfänger", 200), ("Menge", 110));
    public ListView Anforderungen { get; } = Tabelle("Anforderungen", ("Erstellt", 120), ("Empfänger", 200), ("Posten", 70), ("Stand", 170));
    public Button ErstellenKnopf { get; } = Stil.HauptKnopf("Anforderung erstellen …");
    public Button MengeKnopf { get; } = Stil.NebenKnopf("Menge ändern …");
    public Button ZaehlenKnopf { get; } = Stil.NebenKnopf("Bestand zählen …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Ausdruck …");
    public Button EmailKnopf { get; } = Stil.NebenKnopf("E-Mail …");
    public Button WeiterKnopf { get; } = Stil.NebenKnopf("Weiter im Verlauf …");
    public List<BestellPosten> Posten { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    internal Func<string, string, string?> Fragen { get; set; }
    internal Func<string, bool> Bestaetigen { get; set; }
    internal Action<Func<System.Drawing.Printing.PrintDocument>, string> Vorschau { get; set; }
    public event Action? Geaendert;

    public BestellungSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Fragen = (titel, frage) => Eingabe.Fragen(FindForm(), titel, frage);
        Bestaetigen = text => MessageBox.Show(FindForm(), text, "E-Mail", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(FindForm()!, bauen, titel);
        Liste.CheckBoxes = true;
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Anforderungen.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        ErstellenKnopf.Click += (_, _) => Erstellen();
        MengeKnopf.Click += (_, _) => MengeAendern();
        ZaehlenKnopf.Click += (_, _) => Zaehlen();
        DruckKnopf.Click += (_, _) => Drucken();
        EmailKnopf.Click += (_, _) => Email();
        WeiterKnopf.Click += (_, _) => Weiter();
        Controls.AddRange([Hinweis, Liste, Anforderungen]);
    }

    private static ListView Tabelle(string name, params (string Titel, int Breite)[] spalten)
    {
        var l = new ListView { View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable, AccessibleName = name, Font = Stil.Standard };
        foreach (var (titel, breite) in spalten) l.Columns.Add(titel, breite);
        return l;
    }

    private string Bereich => BereichswahlRegeln.GanzesHaus(Arbeitsbereich.Gewaehlt) ? Einstellungen.Laden().Bereich : Arbeitsbereich.Gewaehlt;

    public BestellPosten? GewaehlterPosten => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as BestellPosten : null;
    public Anforderung? GewaehlteAnforderung => Anforderungen.SelectedItems.Count == 1 ? Anforderungen.SelectedItems[0].Tag as Anforderung : null;

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        speicher.BestellErinnerungen(jetzt);
        bool bestand = speicher.BestandFuehren(Bereich);
        var bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt);
        Posten = speicher.BestellListe(bewohner, Bereich, heute);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        Liste.Groups.Clear();
        foreach (var g in Posten.GroupBy(p => p.Empfaenger))
        {
            var gruppe = new ListViewGroup(g.Key);
            Liste.Groups.Add(gruppe);
            foreach (var p in g)
                Liste.Items.Add(new ListViewItem([p.Bewohner.Name, $"{p.Verordnung.Medikament} {p.Verordnung.Staerke}".Trim(),
                    p.Vorrat is decimal v ? v.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) : "–", Datum.Text(p.ReichtBis), p.Empfaenger,
                    mengen.GetValueOrDefault(p.Verordnung.Id, "1 Packung")])
                { Tag = p, Group = gruppe, Checked = bestand && p.ReichtBis is not null });
        }
        Liste.EndUpdate();
        Anforderungen.BeginUpdate();
        Anforderungen.Items.Clear();
        foreach (var a in speicher.Anforderungen().Take(50))
            Anforderungen.Items.Add(new ListViewItem([$"{a.Erstellt:dd.MM.yyyy HH:mm}", a.Empfaenger, a.Posten.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                $"{BestellRegeln.StandText(a.Stand)} ({a.StandZeit:dd.MM.})"])
            { Tag = a, ForeColor = a.Stand == AnforderungStand.Erledigt ? Stil.TextLeise : Stil.TextNormal });
        Anforderungen.EndUpdate();
        Hinweis.Text = (bestand ? $"Bestand wird geführt: gezeigt wird, was in den nächsten {speicher.BestellTage()} Tagen ausgeht. " : "Ohne Bestandsführung: bitte ankreuzen, was bestellt werden soll. ") +
            "Übermittelt werden nur Name, Geburtsdatum, Medikament, Stärke und Menge – keine Diagnosen, keine Berichte.";
        ZaehlenKnopf.Visible = bestand;
        KopfText = $"{Bereich} · {Posten.Count} Medikamente · {speicher.Anforderungen(auchErledigte: false).Count} offene Anforderungen";
        KnoepfeSetzen();
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void KnoepfeSetzen()
    {
        var alle = new[] { ErstellenKnopf, MengeKnopf, ZaehlenKnopf, DruckKnopf, EmailKnopf, WeiterKnopf };
        Recht.Anwenden(speicher, Funktionen.MedikamenteAusgeben, alle);
        if (Recht.Grund(speicher, Funktionen.MedikamenteAusgeben) is not null) return;
        string? ohnePosten = GewaehlterPosten is null ? "Bitte zuerst ein Medikament wählen." : null;
        string? ohneAnf = GewaehlteAnforderung is null ? "Bitte zuerst eine Anforderung wählen." : null;
        Stil.Sperren(MengeKnopf, ohnePosten);
        Stil.Sperren(ZaehlenKnopf, ohnePosten);
        foreach (var k in new[] { DruckKnopf, EmailKnopf, WeiterKnopf }) Stil.Sperren(k, ohneAnf);
        Stil.Sperren(WeiterKnopf, ohneAnf ?? (GewaehlteAnforderung!.Stand == AnforderungStand.Erledigt ? "Diese Anforderung ist erledigt." : null));
    }

    private bool Fehler(Eingabefehler e, string titel)
    {
        MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), titel, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    public bool MengeAendern()
    {
        if (GewaehlterPosten is not BestellPosten p || Stil.Sperrgrund(MengeKnopf) is not null || Fragen("Menge", "Menge (z. B. „1 Packung N2“, „50 Stück“)") is not string m) return false;
        mengen[p.Verordnung.Id] = m.Trim();
        Aktualisieren();
        return true;
    }

    public bool Zaehlen()
    {
        if (GewaehlterPosten is not BestellPosten p || Stil.Sperrgrund(ZaehlenKnopf) is not null) return false;
        if (Fragen("Bestand zählen", $"Wie viele Stück {p.Verordnung.Medikament} sind für {p.Bewohner.Name} da?") is not string text) return false;
        if (!decimal.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var stueck))
            return Fehler(new Eingabefehler(["Bitte eine Zahl eintragen."]), "Bestand zählen");
        try { speicher.BestandZaehlen(p.Verordnung.Id, stueck, 0, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { return Fehler(e, "Bestand zählen"); }
        Aktualisieren();
        return true;
    }

    public bool Erstellen()
    {
        if (Stil.Sperrgrund(ErstellenKnopf) is not null) return false;
        var auswahl = Liste.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => ((BestellPosten)i.Tag!, i.SubItems[5].Text)).ToList();
        try { speicher.AnforderungenErstellen(auswahl, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { return Fehler(e, "Anforderung erstellen"); }
        mengen.Clear();
        Aktualisieren();
        return true;
    }

    // Ausdruck bzw. Fax-Vorlage: je Posten eine Zeile.
    public bool Drucken()
    {
        if (GewaehlteAnforderung is not Anforderung a || Stil.Sperrgrund(DruckKnopf) is not null) return false;
        var zeilen = a.Posten.Select(p => new[] { p.Text, p.Menge }).ToList();
        var druck = new TabellenDruck($"Rezeptanforderung / Bestellung an {a.Empfaenger}", $"vom {a.Erstellt:dd.MM.yyyy}", ["Bewohner und Medikament", "Menge"], [5f, 1.5f], zeilen);
        Vorschau(druck.DokumentBauen, "Rezeptanforderung");
        return true;
    }

    // E-Mail über das E-Mail-Programm; ohne Freigabe des Empfängers vorher der Hinweis zum Datenschutz.
    public bool Email()
    {
        if (GewaehlteAnforderung is not Anforderung a || Stil.Sperrgrund(EmailKnopf) is not null) return false;
        if (!speicher.EmailFreigegeben(a.Empfaenger) && !Bestaetigen(BestellRegeln.DatenschutzHinweis + "\n\nDas E-Mail-Programm öffnet sich mit Empfänger, Betreff und Text.")) return false;
        Extern.Oeffnen(BestellRegeln.Mailto(a));
        return true;
    }

    public bool Weiter()
    {
        if (GewaehlteAnforderung is not Anforderung a || Stil.Sperrgrund(WeiterKnopf) is not null) return false;
        var naechster = (AnforderungStand)Math.Min((int)a.Stand + 1, (int)AnforderungStand.Erledigt);
        Dictionary<long, decimal>? geliefert = null;
        if (naechster == AnforderungStand.Geliefert && a.Posten.Count > 0 && speicher.BestandFuehren(Bereich))
        {
            if (Fragen("Geliefert", "Gelieferte Stück je Posten (gleich für alle; leer = nicht einbuchen)") is not string text) return false;
            if (decimal.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var stueck))
                geliefert = a.Posten.ToDictionary(p => p.VerordnungId, _ => stueck);
        }
        try { speicher.AnforderungWeiter(a.Id, naechster, geliefert, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { return Fehler(e, "Verlauf"); }
        Aktualisieren();
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, ClientSize.Width - 2 * rand);
        Hinweis.MaximumSize = new Size(breite, 0);
        Hinweis.Location = new Point(rand, rand);
        int y = Hinweis.Bottom + Stil.Px(this, Stil.AbstandKlein);
        int rest = Math.Max(Stil.Px(this, 240), ClientSize.Height - y - rand);
        Liste.SetBounds(rand, y, breite, rest * 3 / 5);
        Anforderungen.SetBounds(rand, Liste.Bottom + Stil.Px(this, Stil.AbstandKlein), breite, rest * 2 / 5 - Stil.Px(this, Stil.AbstandKlein));
    }
}
