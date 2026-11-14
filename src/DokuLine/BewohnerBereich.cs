using DokuLine.Kern;

namespace DokuLine;

// Bereich „Dokumentation“: links die Bewohner als schmale, einklappbare Liste auf einer abgerundeten Fläche
// („Nachname, Vorname“, Suche darüber), rechts ihre Berichte mit Filter. Pfeiltasten blättern; Doppelklick oder „Akte öffnen“ öffnet die
// Akte. Pro Ansicht genau eine Hauptaktion in Akzentfarbe.
public sealed class BewohnerBereich : Panel, IBereichSeite
{
    // Kopfzeile und Aktionsleiste je nach Ansicht (Liste oder Akte); das Hauptfenster übernimmt sie beim Wechsel.
    public string Titel { get; private set; } = "Dokumentation";
    public string KopfText { get; private set; } = "";
    public IReadOnlyList<Button> Knoepfe { get; private set; } = [];
    void IBereichSeite.Zeigen(bool filterBehalten) => UebersichtZeigen();

    private void KopfSetzen(string klein, string titel, IReadOnlyList<Button>? knoepfe = null)
    {
        (KopfText, Titel) = (klein, titel);
        kopf.TexteSetzen(klein, titel);
        if (knoepfe is null) return;
        Knoepfe = knoepfe;
        kopf.KnoepfeSetzen([.. knoepfe]);
    }

    public const int LinksBreite = 240, LinksEingeklappt = 36;
    private readonly Speicher speicher;
    private readonly KopfBereich kopf;
    public Button BerichtNeuKnopf { get; } = Stil.HauptKnopf("Neuer Bericht …");   // für den gewählten Bewohner
    public Button BedarfKnopf { get; } = Stil.NebenKnopf("Bedarf gegeben …");
    public Button AkteKnopf { get; } = Stil.NebenKnopf("Akte öffnen");
    public Button AnsichtKnopf { get; } = Stil.NebenKnopf("Ansicht: kompakt");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    private readonly Button zurueckKnopf = Stil.NebenKnopf("← Dokumentation");
    private readonly Button berichteKnopf = Stil.NebenKnopf("Berichte ansehen");
    private readonly Button bearbeitenKnopf = Stil.HauptKnopf("Bearbeiten …");

    public ListBox Namen { get; } = new RundeListe { AccessibleName = "Bewohner" };
    public List<Bewohner> Zeilen { get; private set; } = [];
    // Je Zeile der Liste der Bewohner; null bei einer Überschrift (Wohnbereich im Bereich Therapie).
    private List<Bewohner?> eintraege = [];
    public Bewohner? Gewaehlt => Namen.SelectedIndex >= 0
        && Namen.SelectedIndex < eintraege.Count ? eintraege[Namen.SelectedIndex] : null;
    public CheckBox AlleKnopf { get; } = TherapieListe.AlleSchalter();
    public DokumentationAnsicht Dokumentation { get; }
    // Ein Bewohner aus einem anderen Bereich, dessen Berichte gerade gewünscht sind (z. B. aus dem Verzeichnis).
    private long? zusaetzlich;
    public TextBox Suche { get; } = new()
    {
        PlaceholderText = "Name suchen", Font = Stil.Standard,
        AccessibleName = "Bewohner suchen"
    };
    public Button EinklappenKnopf { get; } = Stil.NebenKnopf("◀");
    public bool Eingeklappt { get; private set; }
    private readonly Panel listenFlaeche = new() { Dock = DockStyle.Fill, BackColor = Stil.Seite };
    private readonly KartenFlaeche links = new() { Dock = DockStyle.Left, Width = LinksBreite, Padding = new Padding(0) };
    private long? zuletzt;
    public event Action<long?>? NachrichtenAnzeigen;
    private readonly Button nachrichtenKnopf = Stil.NebenKnopf("Nachrichten …");
    private readonly Button statusKnopf = Stil.NebenKnopf("Status …");
    private readonly Button berichtKnopf = Stil.NebenKnopf("Neuer Bericht …");
    public Button UeberleitungKnopf { get; } = Stil.NebenKnopf("Überleitungsbogen …");
    public Button AbgaengigkeitKnopf { get; } = Stil.NebenKnopf("Abgängigkeit …");
    public Button AuskunftKnopf { get; } = Stil.NebenKnopf("Auskunft prüfen …");
    public Button AerztlichKnopf { get; } = Stil.NebenKnopf("Ärztliche Informationen …");

    // „Neuer Bericht …“ aus der Akte; das Hauptfenster öffnet ihn.
    public event Action<long>? BerichtGewuenscht;
    // Ein anderer Bereich soll sich öffnen (z. B. „Hilfeplangespräche“ aus einem Schlüsselwort).
    public event Action<string>? BereichGewuenscht;

    // Wahl einer Bedarfsverordnung, wenn es mehrere gibt (Index oder null); zum Testen ersetzbar.
    public Func<IReadOnlyList<string>, int?> BedarfWaehlen { get; set; }

    // „Bezug ändern …“ in der Akte.
    public bool BezugOeffnen(long bewohnerId)
    {
        if (speicher.Bewohner(bewohnerId) is not Bewohner b) return false;
        using var fenster = new BezugFenster(speicher, b);
        bool gespeichert = Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK;
        Akte.NeuLaden();
        return gespeichert;
    }

    // Der heutige Tag (für die Aufenthaltsdauer); zum Testen ersetzbar.
    internal Func<DateOnly> Heute { get; set; } = () => DateOnly.FromDateTime(DateTime.Today);
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public AkteAnsicht Akte { get; }
    // Eigenes Feld statt Akte.Visible: Visible meldet „unsichtbar“, solange das Fenster selbst
    // noch nicht angezeigt ist.
    public bool AkteSichtbar { get; private set; }

    // Zum Testen ersetzbar: öffnet das Formular als Dialog und gibt den gespeicherten Bewohner zurück.
    internal Func<BewohnerFormular, Bewohner?> FormularZeigen { get; set; } =
        f => { Stil.SymbolSetzen(f); return f.ShowDialog() == DialogResult.OK ? f.Gespeichert : null; };

    public BewohnerBereich(Speicher speicher, KopfBereich kopf)
    {
        this.speicher = speicher;
        this.kopf = kopf;
        BedarfWaehlen = texte => Eingabe.Waehlen(FindForm(), "Bedarf gegeben", "Welche Bedarfsmedikation?", texte);
        Akte = new AkteAnsicht(speicher) { Dock = DockStyle.Fill, Visible = false };
        Akte.BezugGewuenscht += id => BezugOeffnen(id);
        Akte.Entlassen += () => { NeuLesen(); UebersichtZeigen(); };   // die Akte ist jetzt im Archiv
        BackColor = Stil.Seite;
        Suche.TextChanged += (_, _) => UebersichtZeigen();
        AlleKnopf.CheckedChanged += (_, _) => UebersichtZeigen();
        Namen.SelectedIndexChanged += (_, _) => { if (!abgleichen) BerichteZeigen(); };
        Namen.DoubleClick += (_, _) => AkteOeffnen();
        Namen.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; AkteOeffnen(); } };
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        menue.Items.Add("Akte öffnen", null, (_, _) => AkteOeffnen());
        menue.Items.Add("Neuer Bericht …", null, (_, _) => NeuerBericht());
        menue.Items.Add("Überleitungsbogen …", null, (_, _) => { if (Gewaehlt is Bewohner g) UeberleitungOeffnen(g); });
        menue.Items.Add("Abgängigkeit melden …", null, (_, _) => { if (Gewaehlt is Bewohner g) AbgaengigkeitOeffnen(g); });
        menue.Items.Add("Auskunft prüfen …", null, (_, _) => { if (Gewaehlt is Bewohner g) AuskunftOeffnen(g); });
        Namen.ContextMenuStrip = menue;
        EinklappenKnopf.AccessibleName = "Bewohnerliste ein- oder ausklappen";
        EinklappenKnopf.Click += (_, _) => Einklappen(!Eingeklappt);
        links.Controls.AddRange([EinklappenKnopf, Suche, AlleKnopf, Namen]);
        links.Layout += (_, _) => LinksAnordnen();
        Dokumentation = new DokumentationAnsicht(speicher)
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(Stil.AbstandGross, 0, Stil.AbstandGross, 0)
        };
        listenFlaeche.Controls.Add(Dokumentation);
        Dokumentation.FolgeschritteGewuenscht += (bericht, schritte) => FolgeschritteAusfuehren(bericht, schritte);
        // „Berichte zu diesem Ziel“: alle Berichte, die diesem Ziel zugeordnet sind.
        Akte.Massnahmen.ZielBerichteGewuenscht += z =>
        {
            UebersichtZeigen();
            Dokumentation.FilterSetzen(new DokuFilter { ZielId = z.Id, ZielText = ZielRegeln.ArtText(z.Art), Zeitraum = Kern.Zeitraum.Frei, Bis = Heute() });
        };
        // Doppelklick auf einen Lebensbereich (Reiter „Ziele und Maßnahmen“): dessen Berichte seit Beginn des Quartals.
        Akte.Massnahmen.LebensbereichGewuenscht += nr =>
        {
            UebersichtZeigen();
            var heute = Heute();
            Dokumentation.FilterSetzen(new DokuFilter { Bereich = nr, Zeitraum = Kern.Zeitraum.Frei, Von = MassnahmenRegeln.QuartalBeginn(heute), Bis = heute });
        };
        listenFlaeche.Controls.Add(new Panel
        {
            Dock = DockStyle.Left, Width = Stil.AbstandGross,
            BackColor = Stil.Seite
        });
        listenFlaeche.Controls.Add(links);
        listenFlaeche.Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, 0, 0);
        BerichtNeuKnopf.Click += (_, _) => NeuerBericht();
        BedarfKnopf.Click += (_, _) => BedarfGeben();
        AkteKnopf.Click += (_, _) => AkteOeffnen();
        AnsichtKnopf.Click += (_, _) => AnsichtUmschalten();
        DruckKnopf.Click += (_, _) => Dokumentation.Drucken();
        nachrichtenKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) NachrichtenAnzeigen?.Invoke(id); };
        statusKnopf.Click += (_, _) => StatusOeffnen();
        AerztlichKnopf.Click += (_, _) => AerztlichOeffnen();
        UeberleitungKnopf.Click += (_, _) => { if (Akte.Bewohner is Bewohner a) UeberleitungOeffnen(a); };
        AbgaengigkeitKnopf.Click += (_, _) => { if (Akte.Bewohner is Bewohner a) AbgaengigkeitOeffnen(a); };
        AuskunftKnopf.Click += (_, _) => { if (Akte.Bewohner is Bewohner a) AuskunftOeffnen(a); };
        Akte.AuskunftGewuenscht += AuskunftOeffnen;
        berichtKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) BerichtGewuenscht?.Invoke(id); };
        Controls.Add(listenFlaeche);
        Controls.Add(Akte);
        listenFlaeche.BringToFront();
        Akte.BringToFront();
        zurueckKnopf.Click += (_, _) => UebersichtZeigen();
        berichteKnopf.Click += (_, _) => { if (Akte.Bewohner?.Id is long id) DokumentationZeigen(id); };
        bearbeitenKnopf.Click += (_, _) =>
        {
            if (Akte.Bewohner?.Id is long id) Formular(speicher.Bewohner(id));
        };
    }

    private void LinksAnordnen()
    {
        int rand = Stil.Px(this, Stil.AbstandKlein), breite = links.ClientSize.Width - 2 * rand;
        EinklappenKnopf.SetBounds(links.ClientSize.Width - rand - Stil.Px(this, 28), rand, Stil.Px(this, 28),
            Stil.Px(this, 28));
        foreach (var c in new Control[] { Suche, Namen }) c.Visible = !Eingeklappt;
        AlleKnopf.Visible = !Eingeklappt && TherapieListe.Aktiv;
        if (Eingeklappt) return;
        Suche.SetBounds(rand, rand, breite - Stil.Px(this, 28) - rand, Suche.Height);
        int oben = Suche.Bottom + rand;
        if (TherapieListe.Aktiv)
        {
            AlleKnopf.Location = new Point(rand, oben);
            oben = AlleKnopf.Bottom + rand;
        }
        Namen.SetBounds(rand, oben, breite, Math.Max(40, links.ClientSize.Height - oben - rand));
    }

    public void Einklappen(bool ein)
    {
        Eingeklappt = ein;
        links.Width = Stil.Px(this, ein ? LinksEingeklappt : LinksBreite);
        EinklappenKnopf.Text = ein ? "▶" : "◀";
        links.PerformLayout();
    }

    private bool abgleichen;

    // Die Liste links (Bereich, Suche) und rechts die Berichte des gewählten Bewohners; die Wahl bleibt.
    public void UebersichtZeigen()
    {
        long? vorher = (AkteSichtbar ? Akte.Bewohner?.Id : null) ?? Gewaehlt?.Id ?? zuletzt;
        if (Namen.SelectedIndex < 0 && !AkteSichtbar) vorher = zuletzt;
        if (vorher != zusaetzlich) zusaetzlich = null;
        var alle = speicher.AlleBewohner();
        string bereich = Arbeitsbereich.Gewaehlt;
        bool therapie = TherapieListe.Aktiv;
        var imBereich = therapie ? TherapieListe.Bewohner(speicher, AlleKnopf.Checked, Heute()) : BereichswahlRegeln.Bewohner(alle, bereich);
        if (zusaetzlich is long z && imBereich.All(b => b.Id != z)
            && alle.FirstOrDefault(b => b.Id == z) is Bewohner extra) imBereich.Add(extra);
        abgleichen = true;
        try
        {
            string suche = Suche.Text.Trim();
            bool Passt(Bewohner b) => suche.Length == 0
                || b.Name.Contains(suche, StringComparison.CurrentCultureIgnoreCase)
                || $"{b.Nachname}, {b.Vorname}".Contains(suche, StringComparison.CurrentCultureIgnoreCase);
            Zeilen = [.. imBereich.Where(Passt)
                .OrderBy(b => b.Nachname, StringComparer.CurrentCulture)
                .ThenBy(b => b.Vorname, StringComparer.CurrentCulture)];
            if (Namen is RundeListe leise) leise.Leise.Clear();   // gilt nur für die Reihenfolge der Bewohnerliste
            if (therapie) eintraege = TherapieListe.Fuellen(Namen, Zeilen, speicher);
            else
            {
                eintraege = [.. Zeilen];
                Namen.BeginUpdate();
                Namen.Items.Clear();
                foreach (var b in Zeilen) Namen.Items.Add($"{b.Nachname}, {b.Vorname}");
                // Abwesende in leiserer Schrift (ohne Marken in der Leiste, Vorgabe Tobi 09.11.2026).
                if (Namen is RundeListe runde)
                {
                    runde.Leise.Clear();
                    var jetzt = DateTime.Now;
                    for (int i = 0; i < Zeilen.Count; i++) if (AbwesenheitAnzeige.Text(speicher, Zeilen[i], jetzt) is not null) runde.Leise.Add(i);
                }
                Namen.EndUpdate();
            }
            int index = eintraege.FindIndex(b => b is not null && b.Id == vorher);
            if (index < 0) index = eintraege.FindIndex(b => b is not null);
            Namen.SelectedIndex = index;
            links.PerformLayout();
            string anzahl = imBereich.Count == 1 ? "1 Bewohner" : $"{imBereich.Count} Bewohner";
            string zahl = Zeilen.Count == imBereich.Count ? anzahl : $"{Zeilen.Count} von {anzahl}";
            KopfSetzen($"{bereich} · {zahl}", "Dokumentation");
        }
        finally { abgleichen = false; }
        BerichteZeigen();
        Akte.Visible = false;
        listenFlaeche.Visible = true;
        AkteSichtbar = false;
        KopfSetzen(KopfText, "Dokumentation", [BerichtNeuKnopf, BedarfKnopf, AkteKnopf, AnsichtKnopf, DruckKnopf]);
    }

    public bool Waehlen(long bewohnerId)
    {
        int index = eintraege.FindIndex(b => b is not null && b.Id == bewohnerId);
        if (index < 0) return false;
        Namen.SelectedIndex = index;
        return true;
    }

    // Die Berichte eines Bewohners zeigen (nach „Neuer Bericht …“, „Berichte ansehen“); wohnt er in einem anderen
    // Bereich, steht er zusätzlich in der Liste, bis man einen anderen wählt.
    public void DokumentationZeigen(long bewohnerId)
    {
        AkteSichtbar = false;
        zuletzt = bewohnerId;
        Namen.SelectedIndex = -1;
        zusaetzlich = bewohnerId;
        Suche.Text = "";
        UebersichtZeigen();
        Waehlen(bewohnerId);
    }

    // Rechts die Berichte des gewählten Bewohners; ohne Auswahl sind die Knöpfe grau.
    private void BerichteZeigen()
    {
        var b = Gewaehlt;
        if (b is not null) { zuletzt = b.Id; Dokumentation.Zeigen(b); }
        Dokumentation.Visible = b is not null;
        string? ohne = b is null ? "Bitte zuerst einen Bewohner wählen." : null;
        Stil.Sperren(AkteKnopf, ohne);
        Stil.Sperren(DruckKnopf, ohne);
        Stil.Sperren(BerichtNeuKnopf, ohne ?? Recht.Grund(speicher, Funktionen.BerichteSchreiben));
        Stil.Sperren(BedarfKnopf, ohne ?? Recht.Grund(speicher, Funktionen.BedarfGeben)
            ?? (BedarfVerordnungen(b!).Count == 0 ? "Keine Bedarfsmedikation, die heute gilt." : null));
        if (Namen.ContextMenuStrip is { } menue)
        {
            menue.Items[1].Enabled = Stil.Sperrgrund(BerichtNeuKnopf) is null;
            menue.Items[1].Visible = !RechteRegeln.FehltRecht(Stil.Sperrgrund(BerichtNeuKnopf));
            menue.Items[1].Text = Stil.Sperrgrund(BerichtNeuKnopf) is string grund
                ? $"Neuer Bericht … – {grund}"
                : "Neuer Bericht …";
        }
    }

    private List<Verordnung> BedarfVerordnungen(Bewohner b)
    {
        var heute = Heute();
        return [.. speicher.Verordnungen(b.Id!.Value).Where(v => v.Bedarf && MedikationRegeln.Stand(v,
            heute).Zustand == Zustand.Gilt)];
    }

    public void AkteOeffnen() { if (Gewaehlt?.Id is long id) AkteZeigen(id); }

    // „Neuer Bericht …“ für den gewählten Bewohner: bleibt in der Liste, rechts erscheint er.
    public bool NeuerBericht()
    {
        if (Gewaehlt is null || Stil.Sperrgrund(BerichtNeuKnopf) is not null) return false;
        return Dokumentation.NeuerBericht();
    }

    // Schlüsselwörter (Bauplan 48): nach dem Speichern die vorgemerkten Schritte – das Zielformular vorbelegt (Bewohner,
    // erkanntes Datum, Bezug zum Bericht); angelegt wird erst dort. Danach trägt der Bericht einen Verweis.
    public void FolgeschritteAusfuehren(Bericht bericht, IReadOnlyList<Vorschlag> schritte)
    {
        if (speicher.Bewohner(bericht.BewohnerId) is not Bewohner b) return;
        long id = b.Id!.Value;
        foreach (var v in schritte)
            switch (v.Folgeschritt)
            {
                case Folgeschritt.Termin:
                    using (var f = new KalenderFenster(speicher, null, v.Datum ?? Heute(), id))
                    {
                        f.Titel.Text = v.Wort;
                        if (v.Uhrzeit is TimeOnly u)
                        {
                            f.BeginnUhrzeit.Text = u.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                            f.EndeUhrzeit.Text = u.AddHours(1).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        f.Hinweis.Text = $"aus dem Bericht vom {bericht.Zeitpunkt:dd.MM.yyyy HH:mm}";
                        if (Dialog.Zeigen(f, FindForm()) == DialogResult.OK && f.Gespeichert is KalenderTermin t)
                            speicher.VerweisEintragen(bericht.Id!.Value, SchluesselwortRegeln.VerweisText(v.Folgeschritt, $"{t.Aktuell.Beginn:dd.MM. HH:mm}"));
                    }
                    break;
                case Folgeschritt.Aufgabe or Folgeschritt.Wiedervorlage:
                    using (var f = new AufgabeFenster(speicher, id, bericht.Id))
                    {
                        f.Inhalt.Text = $"{v.Wort}: ";
                        if (v.Datum is DateOnly d) f.FaelligDatum.Text = Datum.Text(d);
                        if (Dialog.Zeigen(f, FindForm()) == DialogResult.OK && f.Gespeichert is Aufgabe a)
                            speicher.VerweisEintragen(bericht.Id!.Value, SchluesselwortRegeln.VerweisText(v.Folgeschritt, a.Text.Length > 40 ? a.Text[..40] + "…" : a.Text));
                    }
                    break;
                case Folgeschritt.Medikamentenplan: AkteZeigen(id); Akte.Reiter.Waehlen("Medikamentenplan"); break;
                case Folgeschritt.Vitalwerte: AkteZeigen(id); Akte.Reiter.Waehlen("Vitalwerte"); break;
                case Folgeschritt.Ziele: AkteZeigen(id); Akte.Reiter.Waehlen(MassnahmenAnsicht.ReiterName); break;
                case Folgeschritt.Schweigepflicht: AuskunftOeffnen(b); break;
                case Folgeschritt.Abgaengigkeit: AbgaengigkeitOeffnen(b); break;
                case Folgeschritt.Ueberleitung: UeberleitungOeffnen(b); break;
                case Folgeschritt.Bedarf: if (Gewaehlt?.Id == id) BedarfGeben(); break;
                case Folgeschritt.Hilfeplan: BereichGewuenscht?.Invoke("Hilfeplangespräche"); break;
            }
        Dokumentation.Aktualisieren();
    }

    // „Bedarf gegeben …“ wie im Medikamentenplan; bei mehreren Bedarfsverordnungen erst wählen.
    public bool BedarfGeben()
    {
        if (Gewaehlt is not Bewohner b || Stil.Sperrgrund(BedarfKnopf) is not null) return false;
        var liste = BedarfVerordnungen(b);
        var texte = liste.Select(v => $"{v.Medikament} {v.Staerke} – bei Bedarf: {v.BedarfAnlass}".Trim()).ToList();
        int? wahl = liste.Count == 1 ? 0 : BedarfWaehlen(texte);
        if (wahl is not int i || i < 0 || i >= liste.Count) return false;
        using var fenster = new BedarfFenster(speicher, liste[i], b);
        bool ok = Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK;
        Dokumentation.Aktualisieren();
        return ok;
    }

    public void AnsichtUmschalten()
    {
        Dokumentation.KompaktSetzen(!Dokumentation.Kompakt);
        AnsichtKnopf.Text = Dokumentation.Kompakt ? "Ansicht: kompakt" : "Ansicht: vollständig";
    }

    // Rechte des Angemeldeten auf die Knöpfe im Kopf der Akte.
    private void AkteKnoepfeSperren()
    {
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, bearbeitenKnopf);
        Recht.Anwenden(speicher, Funktionen.BerichteSchreiben, berichtKnopf);
        Recht.Anwenden(speicher, Funktionen.AerztlicheAnsehen, AerztlichKnopf);
    }

    // Mehrplatz: die gezeigte Ansicht neu lesen (Karten oder Kopf und Stammdaten der Akte).
    public void NeuLesen()
    {
        if (!AkteSichtbar) { UebersichtZeigen(); return; }
        if (Akte.Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) { UebersichtZeigen(); return; }
        Akte.NeuLaden();
        KopfSetzen("Dokumentation", "Bewohnerakte");   // der Name steht nur einmal, auf der Karte darunter
        AkteKnoepfeSperren();
    }

    public void AkteZeigen(long id)
    {
        if (speicher.Bewohner(id) is not Bewohner b)
        {
            UebersichtZeigen();
            return;
        }
        // Entlassene: nur lesend im Archiv (mit Recht), nie die bearbeitbare Akte.
        if (speicher.Archiviert(id))
        {
            if (Recht.Grund(speicher, Funktionen.ArchivEinsehen) is null)
            {
                using var archiv = new ArchivAkteFenster(speicher, b);
                Dialog.Zeigen(archiv, FindForm());
            }
            else MessageBox.Show(FindForm(), $"{b.Name} ist entlassen; die Akte steht im Archiv.", "Archiv", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UebersichtZeigen();
            return;
        }
        Akte.Zeigen(b);
        listenFlaeche.Visible = false;
        Akte.Visible = true;
        AkteSichtbar = true;
        AkteKnoepfeSperren();
        // Der Name steht nur einmal, auf der Karte darunter.
        KopfSetzen("Dokumentation", "Bewohnerakte",
            [zurueckKnopf, berichteKnopf, nachrichtenKnopf, statusKnopf, AuskunftKnopf, AerztlichKnopf, UeberleitungKnopf, AbgaengigkeitKnopf, berichtKnopf, bearbeitenKnopf]);
    }

    // Statusmeldung setzen oder aufheben (Mehrplatzbetrieb); danach die Akte neu lesen.
    public bool StatusOeffnen()
    {
        if (Akte.Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) return false;
        using var fenster = new StatusmeldungFenster(speicher, b);
        bool gespeichert = Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK;
        Akte.NeuLaden();
        return gespeichert;
    }

    // Ärztliche Informationen (Recht „ansehen“); danach die Akte neu (Allergiehinweis).
    public bool AerztlichOeffnen()
    {
        if (Recht.Grund(speicher, Funktionen.AerztlicheAnsehen) is not null) return false;
        if (Akte.Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) return false;
        using var fenster = new AerztlichFenster(speicher, b);
        Dialog.Zeigen(fenster, FindForm());
        Akte.NeuLaden();
        return fenster.Geaendert;
    }

    public BewohnerFormular FormularErstellen(Bewohner? b) => new(speicher, b);

    // Überleitungsbogen (Bauplan 33): aus der Akte, per Rechtsklick in der Bewohnerliste und in der Übersicht.
    public void UeberleitungOeffnen(Bewohner b)
    {
        using var fenster = new UeberleitungFenster(speicher, b);
        Dialog.Zeigen(fenster, FindForm());
    }

    // Abgängigkeitsmeldung (Bauplan 34): aus der Akte, per Rechtsklick in Bewohnerliste und Übersicht, aus der Ausgangsliste.
    // Danach die Akte neu (Marke „abgängig seit …“).
    public void AbgaengigkeitOeffnen(Bewohner b)
    {
        using (var fenster = new AbgaengigkeitFenster(speicher, b)) Dialog.Zeigen(fenster, FindForm());
        if (AkteSichtbar && Akte.Bewohner?.Id == b.Id) Akte.NeuLaden();
        AbgaengigkeitGeaendert?.Invoke();
    }

    public event Action? AbgaengigkeitGeaendert;

    // Schnellabfrage „Darf ich hier Auskunft geben?“ (Bauplan 35).
    public void AuskunftOeffnen(Bewohner b)
    {
        using var fenster = new AuskunftFenster(speicher, b);
        Dialog.Zeigen(fenster, FindForm());
    }

    // „Neuer Bewohner …“ aus der Übersicht: Formular; der gespeicherte Bewohner oder null.
    public Bewohner? NeuerBewohner()
    {
        if (Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return null;
        using var formular = FormularErstellen(null);
        return FormularZeigen(formular);
    }

    private void Formular(Bewohner? b)
    {
        using var formular = FormularErstellen(b);
        if (FormularZeigen(formular) is Bewohner gespeichert) AkteZeigen(gespeichert.Id!.Value);
        // Neu lesen: Ein anderer Arbeitsplatz kann inzwischen gespeichert haben.
        else if (b?.Id is long id) AkteZeigen(id);
    }
}
