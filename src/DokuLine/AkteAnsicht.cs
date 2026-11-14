using DokuLine.Kern;

namespace DokuLine;

// Die Akte eines Bewohners: Kopfkarte (Bild, Name, Zimmer) und Allergiehinweis immer gut sichtbar
// oben, darunter Reiter für die Teile der Akte. Noch nicht gebaute Reiter zeigen „Kommt in Version …“.
public sealed class AkteAnsicht : Panel
{
    private const int BildGroesse = 96;

    private readonly KartenFlaeche kopfKarte = new();
    private readonly BildFeld bild = new();
    private readonly Label name = new();
    private readonly MarkenFeld zimmer = new("Zimmer", Stil.Zimmer) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };
    private readonly FlowLayoutPanel kopfMarken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label abschnitt = new();
    private readonly KartenFlaeche stammdaten = new();
    private readonly TableLayoutPanel tabelle = new();

    public HinweisFeld Allergie { get; } = new();
    // Auskunftsleiste (Bauplan 35): unter dem Allergiehinweis, über allen Reitern – mit einem Blick, wem man Auskunft geben darf.
    public FlowLayoutPanel Auskunft { get; } = new() { AutoSize = false, WrapContents = true, BackColor = Stil.Seite, Margin = new Padding(0) };
    public Button AuskunftKnopf { get; } = Stil.KleinerKnopf("Auskunft prüfen …");
    public IReadOnlyList<AuskunftMarke> AuskunftMarken { get; private set; } = [];
    // Zielleiste (Bauplan 43): unter der Auskunftsleiste; ein Klick öffnet den Reiter „Ziele und Maßnahmen“.
    public ZielLeiste Zielleiste { get; } = new();
    public event Action<Bewohner>? AuskunftGewuenscht;
    public Reiterleiste Reiter { get; } = new(Bauplan.AktenReiter);
    public PlatzhalterKarte ReiterPlatzhalter { get; } = new() { Visible = false };
    public MedikationAnsicht Medikation { get; }
    public WochenplanAnsicht Wochenplan { get; }
    public BiografieAnsicht Biografie { get; }
    public ReflexionAnsicht Reflexion { get; }
    public HebAnsicht Heb { get; }
    // Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger (Bauplan 30): eine Ansicht für alle fünf Reiter.
    public AngabenAnsicht Angaben { get; }
    public DokumenteAnsicht Dokumente { get; }
    public VitalwerteAnsicht Vital { get; }
    public MassnahmenAnsicht Massnahmen { get; }
    public Label KrankenkasseText { get; } = new() { Cursor = Cursors.Hand };
    public Label KostentraegerText { get; } = new() { Cursor = Cursors.Hand };
    public Label HebText { get; } = new() { Cursor = Cursors.Hand, UseMnemonic = false };
    // Personenbeschreibung (Bauplan 34): für das Meldeblatt; Marke, wenn älter als 12 Monate.
    public Label BeschreibungText { get; } = new() { Cursor = Cursors.Hand };
    public Button BeschreibungKnopf { get; } = Stil.NebenKnopf("Personenbeschreibung …");
    // Entlassen (Bauplan 46): die Akte wandert ins Archiv.
    public Button EntlassenKnopf { get; } = Stil.NebenKnopf("Entlassen …");
    public event Action? Entlassen;
    public List<ModulReiter> ModulAnsichten { get; } = [];
    public Label Geburtsdatum { get; } = new();
    public Label Aufnahmedatum { get; } = new();
    public Label Beschlussende { get; } = new();
    public Label OffenGefuehrt { get; } = new();
    public Label Wohnbereich { get; } = new();
    public Label Alter { get; } = new();
    // Bezugspflege und Einzeltrainer: Farbpunkt der Person, Name und Kürzel, seit wann.
    public Label Bezugspflege { get; } = new();
    public Label Einzeltrainer { get; } = new();
    private readonly FarbPunkt bezugPunkt = new(), trainerPunkt = new();
    public Button BezugKnopf { get; } = Stil.NebenKnopf("Bezug ändern …");
    public Button BezugVerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    private readonly FlowLayoutPanel bezugKnoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    public event Action<long>? BezugGewuenscht;
    // Betreuer: Name, Stelle, Telefon, E-Mail; „E-Mail an Betreuer …“ öffnet das E-Mail-Programm.
    public Label BetreuerText { get; } = new();
    public Button BetreuerKnopf { get; } = Stil.NebenKnopf("Betreuer …");
    public Button EmailKnopf { get; } = Stil.NebenKnopf("E-Mail an Betreuer …");
    // Zum Testen ersetzbar: Hinweis vor dem Öffnen des E-Mail-Programms; true = weiter.
    internal Func<string, bool> EmailBestaetigen { get; set; }

    // Marken im Kopf der Akte: Statusmeldung (lang, mit Bereich, Kürzel und Zeit), Zimmer, „offen geführt“,
    // Hinweise der Module (z. B. Widersprüche).
    public IEnumerable<string> KopfMarken => kopfMarken.Controls.OfType<MarkenFeld>().Select(m => m.Text);
    public Bewohner? Bewohner { get; private set; }
    public string? StatusText { get; private set; }   // die geltende Statusmeldung, wie sie im Kopf steht
    public string? WoText { get; private set; }       // „in Therapie: … bis …“ (Tagesplan) oder „abwesend: unterwegs mit …“ (Fahrt)

    private readonly Speicher speicher;

    public AkteAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        Medikation = new MedikationAnsicht(speicher) { Visible = false };
        Wochenplan = new WochenplanAnsicht(speicher) { Visible = false };
        Biografie = new BiografieAnsicht(speicher) { Visible = false };
        Reflexion = new ReflexionAnsicht(speicher) { Visible = false };
        Heb = new HebAnsicht(speicher) { Visible = false };
        Angaben = new AngabenAnsicht(speicher) { Visible = false };
        Dokumente = new DokumenteAnsicht(speicher) { Visible = false };
        Vital = new VitalwerteAnsicht(speicher) { Visible = false };
        Vital.Geaendert += () => { if (Bewohner is not null) Fuellen(Bewohner); };
        Massnahmen = new MassnahmenAnsicht(speicher) { Visible = false };
        Massnahmen.Geaendert += () => { if (Bewohner is not null) ZieleZeigen(Bewohner); };
        Zielleiste.Geklickt += () => Reiter.Waehlen(MassnahmenAnsicht.ReiterName);
        Angaben.DokumenteGewuenscht += kategorie => { Reiter.Waehlen("Dokumente"); if (Bewohner is not null) Dokumente.Zeigen(Bewohner, kategorie); };
        Angaben.Geaendert += () => { if (Bewohner is not null) BetreuerZeigen(Bewohner); };
        KrankenkasseText.Click += (_, _) => Reiter.Waehlen("Krankenkasse");
        KostentraegerText.Click += (_, _) => Reiter.Waehlen("Kostenträger");
        HebText.Click += (_, _) => Reiter.Waehlen("HEB");
        BeschreibungText.Click += (_, _) => BeschreibungBearbeiten();
        BeschreibungKnopf.Click += (_, _) => BeschreibungBearbeiten();
        AutoScroll = true;
        BackColor = Stil.Seite;
        DoubleBuffered = true;

        name.AutoSize = true;
        name.Font = Stil.Ueberschrift;
        name.ForeColor = Stil.TextNormal;
        name.BackColor = Stil.Flaeche;
        name.UseMnemonic = false;
        kopfMarken.Controls.Add(zimmer);
        kopfKarte.Controls.AddRange([bild, name, kopfMarken]);

        abschnitt.Text = "STAMMDATEN";
        abschnitt.AutoSize = true;
        abschnitt.Font = Stil.Abschnitt;
        abschnitt.ForeColor = Stil.TextLeise;
        abschnitt.BackColor = Stil.Seite;

        tabelle.ColumnCount = 2;
        tabelle.AutoSize = true;
        tabelle.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        tabelle.BackColor = Stil.Flaeche;
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        foreach (var (beschriftung, wert) in new[]
                 {
                     ("Geburtsdatum", Geburtsdatum), ("Aufnahmedatum", Aufnahmedatum), ("Beschlussende", Beschlussende),
                     ("Offen geführt", OffenGefuehrt), ("Wohnbereich", Wohnbereich),
                     ("Bezugspflege", Bezugspflege), ("Einzeltrainer", Einzeltrainer), ("Betreuer", BetreuerText),
                     ("Krankenkasse", KrankenkasseText), ("Kostenträger", KostentraegerText), ("Nächster HEB", HebText),
                     ("Personenbeschreibung", BeschreibungText),
                 })
        {
            tabelle.Controls.Add(new Label
            {
                Text = beschriftung, AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextMittel,
                Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandGross * 2, Stil.AbstandMini),
            });
            wert.AutoSize = true;
            wert.Font = Stil.Fett;
            wert.ForeColor = Stil.TextNormal;
            wert.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            wert.UseMnemonic = false;
            if (wert == Bezugspflege || wert == Einzeltrainer)
            {
                var punkt = wert == Bezugspflege ? bezugPunkt : trainerPunkt;
                punkt.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0);
                var mitPunkt = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
                mitPunkt.Controls.AddRange([punkt, wert]);
                tabelle.Controls.Add(mitPunkt);
                continue;
            }
            if (wert != Geburtsdatum)
            {
                tabelle.Controls.Add(wert);
                continue;
            }
            // Das Alter steht neben dem Geburtsdatum.
            Alter.AutoSize = true;
            Alter.Font = Stil.Standard;
            Alter.ForeColor = Stil.TextMittel;
            Alter.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandMini, 0, Stil.AbstandMini);
            var zelle = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
            zelle.Controls.AddRange([wert, Alter]);
            tabelle.Controls.Add(zelle);
        }
        stammdaten.Controls.Add(tabelle);
        foreach (var k in new[] { BezugKnopf, BezugVerlaufKnopf, BetreuerKnopf, EmailKnopf, BeschreibungKnopf, EntlassenKnopf }) { k.AutoSize = true; k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini); }
        bezugKnoepfe.Controls.AddRange([BezugKnopf, BezugVerlaufKnopf, BetreuerKnopf, EmailKnopf, BeschreibungKnopf, EntlassenKnopf]);
        EntlassenKnopf.Click += (_, _) => EntlassenOeffnen();
        BetreuerKnopf.Click += (_, _) => BetreuerBearbeiten();
        EmailKnopf.Click += (_, _) => EmailSchreiben();
        EmailBestaetigen = text => MessageBox.Show(FindForm(), text, "E-Mail an Betreuer", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        stammdaten.Controls.Add(bezugKnoepfe);
        BezugKnopf.Click += (_, _) => { if (Bewohner?.Id is long id) BezugGewuenscht?.Invoke(id); };
        BezugVerlaufKnopf.Click += (_, _) => BezugVerlaufZeigen();

        Reiter.Gewechselt += _ => ReiterZeigen();
        AuskunftKnopf.Click += (_, _) => { if (Bewohner is not null) AuskunftGewuenscht?.Invoke(Bewohner); };
        Controls.AddRange([kopfKarte, Allergie, Auskunft, Zielleiste, Reiter, abschnitt, stammdaten, ReiterPlatzhalter, Medikation, Wochenplan, Biografie, Reflexion, Heb, Angaben, Dokumente, Vital, Massnahmen]);
        ModuleAktualisieren();
    }

    // Reiter der eingeschalteten Module nach „Wochenplan“; ausgeschaltete verschwinden ganz.
    public void ModuleAktualisieren()
    {
        foreach (var r in ModulAnsichten) { Controls.Remove(r); r.Dispose(); }
        ModulAnsichten.Clear();
        foreach (var m in Modulverwaltung.Eingeschaltet(speicher))
            if (m.ReiterBauen(speicher) is ModulReiter r)
            {
                r.Visible = false;
                ModulAnsichten.Add(r);
                Controls.Add(r);
            }
        var namen = Bauplan.AktenReiter.ToList();
        namen.InsertRange(namen.IndexOf("Wochenplan") + 1, ModulAnsichten.Select(r => r.Titel));
        Reiter.NamenSetzen(namen);
        ReiterZeigen();
    }

    // Nach einem Benutzerwechsel: den offenen Reiter neu zeigen (gesperrte Knöpfe nach den neuen Rechten).
    public void AnsichtNeu() => ReiterZeigen();

    private void ReiterZeigen()
    {
        bool stamm = Reiter.Aktiv == "Stammdaten";
        bool medikation = Reiter.Aktiv == "Medikamentenplan";
        bool wochenplan = Reiter.Aktiv == "Wochenplan";
        bool biografie = Reiter.Aktiv == "Biografie";
        bool reflexion = Reiter.Aktiv == "Reflexion";
        bool heb = Reiter.Aktiv == "HEB";
        bool dokumente = Reiter.Aktiv == "Dokumente";
        bool vital = Reiter.Aktiv == "Vitalwerte";
        bool massnahmen = Reiter.Aktiv == MassnahmenAnsicht.ReiterName;
        Dokumente.Visible = dokumente;
        Vital.Visible = vital;
        Massnahmen.Visible = massnahmen;
        if (massnahmen && Bewohner is not null) Massnahmen.Zeigen(Bewohner);
        if (vital && Bewohner is not null) Vital.Zeigen(Bewohner);
        if (dokumente && Bewohner is not null) Dokumente.Zeigen(Bewohner);
        Angabe? angabe = Enum.GetValues<Angabe>().Cast<Angabe?>().FirstOrDefault(a => AngabenRegeln.Name(a!.Value) == Reiter.Aktiv);
        Angaben.Visible = angabe is not null;
        if (angabe is Angabe art && Bewohner is not null) Angaben.Zeigen(Bewohner, art);
        var modul = ModulAnsichten.FirstOrDefault(r => r.Titel == Reiter.Aktiv);
        foreach (var r in ModulAnsichten) r.Visible = r == modul;
        if (modul is not null && Bewohner is not null) modul.Zeigen(Bewohner);
        abschnitt.Visible = stammdaten.Visible = stamm;
        Medikation.Visible = medikation;
        Wochenplan.Visible = wochenplan;
        Biografie.Visible = biografie;
        Reflexion.Visible = reflexion;
        Heb.Visible = heb;
        if (medikation && Bewohner is not null) Medikation.Zeigen(Bewohner);
        if (wochenplan && Bewohner is not null) Wochenplan.Zeigen(Bewohner);
        if (biografie && Bewohner is not null) Biografie.Zeigen(Bewohner);
        if (reflexion && Bewohner is not null) Reflexion.Zeigen(Bewohner);
        if (heb && Bewohner is not null) Heb.Zeigen(Bewohner);
        ReiterPlatzhalter.Visible = !stamm && !medikation && !wochenplan && !biografie && !reflexion && !heb && modul is null && angabe is null && !dokumente && !vital && !massnahmen;
        if (!stamm && Bauplan.Reiter.TryGetValue(Reiter.Aktiv, out var vorhaben))
            ReiterPlatzhalter.Setzen(vorhaben.Version, vorhaben.Beschreibung);
        PerformLayout();
        Invalidate(true);
    }

    private int S(int wert) => Stil.Px(this, wert);

    public void Zeigen(Bewohner b)
    {
        if (Bewohner?.Id != b.Id) speicher.Zugriff(Recht.Kuerzel, b.Id, ZugriffRegeln.Geoeffnet, "Akte");   // Zugriffsprotokoll (Bauplan 37)
        Bewohner = b;
        Reiter.Waehlen("Stammdaten");
        Fuellen(b);
    }

    // Mehrplatz: Kopf und Stammdaten neu lesen, ohne den Reiter zu wechseln (z. B. nach 30 Sekunden,
    // wenn ein anderer Arbeitsplatz etwas gespeichert hat). Die anderen Reiter lesen beim Wechsel neu.
    public void NeuLaden()
    {
        if (Bewohner?.Id is not long id || speicher.Bewohner(id) is not Bewohner b) return;
        Bewohner = b;
        var scroll = AutoScrollPosition;
        Fuellen(b);
        AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
    }

    // Bezugspflege und Einzeltrainer mit Farbe der Person; „keine“ grau.
    private void BezugZeigen(Bewohner b)
    {
        var bezug = speicher.Bezug(b.Id!.Value);
        var farben = BezugAnzeige.Farben(speicher);
        var namen = speicher.AlleMitarbeiter().ToDictionary(m => m.Kuerzel, m => m.Anzeige, StringComparer.OrdinalIgnoreCase);
        void Zeile(Label text, FarbPunkt punkt, Zuordnung? z, string keine, string vorne)
        {
            text.Text = z is null ? keine : $"{vorne}{namen.GetValueOrDefault(z.Kuerzel, z.Kuerzel)} · seit {Datum.Text(z.Ab)}";
            text.ForeColor = z is null ? Stil.TextLeise : Stil.TextNormal;
            punkt.Farbe = z is null ? null : BezugAnzeige.Farbe(farben, z.Kuerzel);
        }
        Zeile(Bezugspflege, bezugPunkt, bezug.Bezugspflege, "keine", "");
        Zeile(Einzeltrainer, trainerPunkt, bezug.Einzeltrainer, "keiner", bezug.Einzeltrainer is { } t ? $"{t.Art} · " : "");
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, BezugKnopf);
    }

    // Krankenkasse und Kostenträger als Kurzzeile; ein Klick öffnet den Reiter. „nicht erfasst“ in Achtung-Farbe.
    private void KurzzeilenZeigen(Bewohner b)
    {
        var heute = DateOnly.FromDateTime(DateTime.Now);
        var angaben = speicher.Angaben(b.Id!.Value, heute);
        var person = AngabenAnsicht.PersonText(speicher);
        foreach (var (feld, art) in new[] { (KrankenkasseText, Angabe.Krankenkasse), (KostentraegerText, Angabe.Kostentraeger) })
        {
            var f = angaben.GetValueOrDefault(art);
            feld.Text = AngabenRegeln.Kurz(art, f, person, heute);
            feld.ForeColor = f is null ? Stil.Achtung.Text : Stil.Akzent;
            feld.Font = f is null ? Stil.FettKursiv : Stil.Fett;
            feld.AccessibleName = $"{AngabenRegeln.Name(art)}: {feld.Text} (öffnet den Reiter)";
        }
        var w = Einstellungen.Laden();
        var heb = speicher.HebFrist(b.Id!.Value, heute);
        var hebMarke = HebFristRegeln.Marke(heb, heute, w.HebErinnernWochen, w.HebAchtungWochen);
        HebText.Text = HebFristRegeln.KurzText(heb) + (hebMarke is null || heb.Zustand == HebFristZustand.NichtErfasst ? "" : $" · {hebMarke.Text}");
        HebText.ForeColor = Stil.TextFarbe(hebMarke?.Art) ?? Stil.Akzent;
        HebText.Font = heb.Zustand == HebFristZustand.NichtErfasst ? Stil.FettKursiv : Stil.Fett;
        HebText.AccessibleName = $"Nächster HEB: {HebText.Text} (öffnet den Reiter)";
        var beschreibung = angaben.GetValueOrDefault(Angabe.Personenbeschreibung);
        var marke = AbgaengigkeitRegeln.BeschreibungMarke(beschreibung, heute);
        BeschreibungText.Text = AbgaengigkeitRegeln.BeschreibungKurz(beschreibung) + (beschreibung is not null && marke is not null ? " – älter als 12 Monate, bitte prüfen" : "");
        BeschreibungText.ForeColor = marke is not null ? Stil.Achtung.Text : Stil.TextNormal;
        BeschreibungText.Font = marke is not null ? Stil.FettKursiv : Stil.Fett;
        BeschreibungText.MaximumSize = new Size(S(560), 0);
        BeschreibungText.AccessibleName = $"Personenbeschreibung: {BeschreibungText.Text} (öffnet das Formular)";
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, BeschreibungKnopf, EntlassenKnopf);
    }

    // Je Eintrag eine Marke mit Zeichen, Wort und Farbe; widerrufene durchgestrichen. Am Ende „Auskunft prüfen …“.
    public void AuskunftZeigen(Bewohner b)
    {
        var heute = DateOnly.FromDateTime(DateTime.Now);
        AuskunftMarken = SchweigepflichtRegeln.Leiste(speicher.Angaben(b.Id!.Value, heute).GetValueOrDefault(Angabe.Schweigepflicht), speicher.PersonenNamen(), heute);
        Auskunft.SuspendLayout();
        foreach (var alt in Auskunft.Controls.OfType<MarkenFeld>().ToList()) { Auskunft.Controls.Remove(alt); alt.Dispose(); }
        Auskunft.Controls.Remove(AuskunftKnopf);
        foreach (var m in AuskunftMarken)
        {
            var feld = new MarkenFeld(m.Text, Stil.AuskunftFarbe(m.Art)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini), Durchgestrichen = m.Widerrufen };
            Auskunft.Controls.Add(feld);
        }
        AuskunftKnopf.Margin = new Padding(Stil.AbstandKlein, 0, 0, Stil.AbstandMini);
        Auskunft.Controls.Add(AuskunftKnopf);
        Auskunft.AccessibleName = "Auskunft: " + string.Join("; ", AuskunftMarken.Select(m => m.Text));
        Auskunft.ResumeLayout();
    }

    public void ZieleZeigen(Bewohner b)
    {
        Zielleiste.Zeigen(speicher, b.Id!.Value, DateOnly.FromDateTime(DateTime.Now));
        PerformLayout();
    }

    // Nur nach ärztlicher Anordnung: der neueste Wert je Art außerhalb des angeordneten Bereichs (Gefahr-Farbe, mit Wort).
    private IEnumerable<Marke> VitalMarken(long id)
    {
        var grenzen = speicher.Grenzen(id);
        if (grenzen.Count == 0) yield break;
        var r = speicher.Richtwerte();
        foreach (var w in speicher.Vitalwerte(id).GroupBy(w => w.Art).Select(g => g.First()))
            if (VitalRegeln.Bewerten(w, grenzen.GetValueOrDefault(w.Art), r) is { Angeordnet: true })
                yield return new Marke(MarkenArt.Vital, $"{w.Art} {VitalRegeln.WertText(w, r)}: {VitalRegeln.Ausserhalb}");
    }

    public bool EntlassenOeffnen()
    {
        if (Bewohner is null || Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return false;
        using var fenster = new EntlassenFenster(speicher, Bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return false;
        Entlassen?.Invoke();
        return true;
    }

    public void BeschreibungBearbeiten()
    {
        if (Bewohner is null || Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return;
        using var fenster = new AngabeFenster(speicher, Bewohner, Angabe.Personenbeschreibung);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) KurzzeilenZeigen(Bewohner);
        PerformLayout();
    }

    // Betreuer: „nicht erfasst“ in Achtung-Farbe (leer heißt nicht „keiner“, Regel 5).
    private void BetreuerZeigen(Bewohner b)
    {
        var betreuer = speicher.Betreuer(b.Id!.Value);
        BetreuerText.Text = BetreuerRegeln.Text(betreuer);
        BetreuerText.ForeColor = betreuer is null ? Stil.Achtung.Text : Stil.TextNormal;
        BetreuerText.Font = betreuer is null ? Stil.FettKursiv : Stil.Fett;
        BetreuerText.MaximumSize = new Size(S(560), 0);
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, BetreuerKnopf);
        Stil.Sperren(EmailKnopf, betreuer is null ? "Kein Betreuer eingetragen."
            : betreuer.Keine ? "Keine gesetzliche Betreuung eingetragen."
            : !BetreuerRegeln.EmailGueltig(betreuer.Email) ? "Für den Betreuer ist keine E-Mail-Adresse eingetragen." : null);
    }

    public void BetreuerBearbeiten()
    {
        if (Bewohner is null || Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return;
        using var fenster = new BetreuerFenster(speicher, Bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) BetreuerZeigen(Bewohner);
        PerformLayout();
    }

    // Erst der Hinweis, dann das E-Mail-Programm mit Empfänger und Betreff (mailto:). Das Programm
    // selbst sendet nichts; Text und Versand macht der Mensch im E-Mail-Programm.
    public bool EmailSchreiben()
    {
        if (Bewohner?.Id is not long id || speicher.Betreuer(id) is not Betreuer b || b.Keine || !BetreuerRegeln.EmailGueltig(b.Email)) return false;
        if (!EmailBestaetigen($"{BetreuerRegeln.EmailHinweis}\n\nDas E-Mail-Programm öffnet sich mit Empfänger und Betreff. Text und Versand machen Sie dort."))
            return false;
        Extern.Oeffnen(BetreuerRegeln.Mailto(b, BetreuerRegeln.Betreff(Bewohner)));
        return true;
    }

    public void BezugVerlaufZeigen()
    {
        if (Bewohner?.Id is not long id) return;
        var namen = speicher.AlleMitarbeiter().ToDictionary(m => m.Kuerzel, m => m.Name, StringComparer.OrdinalIgnoreCase);
        var verlauf = BezugRegeln.Verlauf(speicher.Bezugspflegen(id), speicher.Einzeltrainer(id), namen);
        using var fenster = new TextFenster($"Bezugspflege und Einzeltrainer – {Bewohner.Name}",
            verlauf.Count == 0 ? "Noch keine Einträge." : string.Join(Environment.NewLine, verlauf));
        Dialog.Zeigen(fenster, FindForm());
    }

    private void Fuellen(Bewohner b)
    {
        bild.Setzen(Bilder.Pfad(b.Bild), BewohnerRegeln.Initialen(b), b.Nachname);
        name.Text = b.Name;
        zimmer.Text = $"Zimmer {b.Zimmer}" + (b.Bett.Length > 0 && b.Bett != ZimmerRegeln.Einzel ? $" · {ZimmerRegeln.BettText(b.Bett)}" : "");
        foreach (var alt in kopfMarken.Controls.OfType<MarkenFeld>().Where(m => m != zimmer).ToList())
        {
            kopfMarken.Controls.Remove(alt);
            alt.Dispose();
        }
        var jetzt = DateTime.Now;
        var marken = new List<Marke>();
        if (MedikationsAenderungen.Liste(speicher.Verordnungen(b.Id!.Value), DateOnly.FromDateTime(jetzt)).Count > 0)
            marken.Add(new Marke(MarkenArt.Medikation, MedikationsAenderungen.MarkeText));
        if (Kennzeichen.OffenGefuehrtMarke(b) is Marke offenMarke) marken.Add(offenMarke);
        marken.AddRange(Modulverwaltung.Marken(speicher, b, jetzt));
        if (speicher.OffeneAbgaengigkeit(b.Id!.Value) is Abgaengigkeit ab && !marken.Any(m => m.Art == MarkenArt.Abgaengig))
            marken.Insert(0, AbgaengigkeitRegeln.Marke(ab));
        marken.AddRange(VitalMarken(b.Id!.Value));
        foreach (var m in marken)
            kopfMarken.Controls.Add(new MarkenFeld(m.Text, Stil.MarkeFuer(m.Art)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });
        StatusText = speicher.Statusmeldung(b.Id!.Value, jetzt) is Statusmeldung status ? "Status: " + StatusmeldungRegeln.Langtext(status, jetzt) : null;
        if (StatusText is not null)
        {
            var feld = new MarkenFeld(StatusText, Stil.MarkeFuer(MarkenArt.Status)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };
            kopfMarken.Controls.Add(feld);
            kopfMarken.Controls.SetChildIndex(feld, 0);   // vorne, vor dem Zimmer
        }
        // Wo müsste er sein? (Tagesplan): nach „gegangen“ bis zum Ende der Therapie.
        WoText = (speicher.Mitfahrende(jetzt).GetValueOrDefault(b.Id!.Value) ?? speicher.InTherapie(jetzt).GetValueOrDefault(b.Id!.Value))?.Text;
        if (WoText is not null)
        {
            var feld = new MarkenFeld(WoText, Stil.MarkeFuer(WoText.StartsWith("abwesend") ? MarkenArt.Abwesend : MarkenArt.Status)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };
            kopfMarken.Controls.Add(feld);
            kopfMarken.Controls.SetChildIndex(feld, 0);
        }
        // Forensik, FOSA (Bauplan 30a): als Wort in Hinweis-Farbe; die Überprüfung steht im Reiter „Betreuung“.
        var zust = speicher.Angaben(b.Id!.Value, DateOnly.FromDateTime(DateTime.Now)).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>();
        foreach (var m in ZustaendigkeitRegeln.Marken(zust, DateOnly.FromDateTime(DateTime.Now)).Where(m => m.Art == MarkenArt.Zustaendig))
            kopfMarken.Controls.Add(new MarkenFeld(m.Text, Stil.MarkeFuer(m.Art)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });
        AccessibleName = $"Akte von {b.Name}";

        // Allergiehinweis: drei klar verschiedene Darstellungen (Regel 5), immer mit Wort.
        var (marke, gestrichelt) = b.Allergie switch
        {
            AllergieZustand.Eingetragen => (Stil.Beschluss, false),
            AllergieZustand.KeineBekannt => (Stil.Offen, false),
            _ => (Stil.Achtung, true),
        };
        Allergie.Setzen(BewohnerRegeln.AllergieText(b), marke, gestrichelt);
        AuskunftZeigen(b);
        ZieleZeigen(b);

        Geburtsdatum.Text = Datum.Text(b.Geburtsdatum);
        Aufnahmedatum.Text = Datum.Text(b.Aufnahmedatum);
        Beschlussende.Text = BewohnerRegeln.BeschlussText(b);
        bool offen = b.Beschluss == BeschlussZustand.NichtErfasst;
        Beschlussende.ForeColor = offen ? Stil.Achtung.Text : Stil.TextNormal;
        Beschlussende.Font = offen ? Stil.FettKursiv : Stil.Fett;
        Alter.Text = $"({BewohnerRegeln.AlterText(b.Geburtsdatum, DateOnly.FromDateTime(DateTime.Now))})";
        OffenGefuehrt.Text = BewohnerRegeln.OffenGefuehrtText(b);
        bool offenUnklar = b.OffenGefuehrt == OffenGefuehrtZustand.NichtErfasst;
        OffenGefuehrt.ForeColor = offenUnklar ? Stil.Achtung.Text : Stil.TextNormal;
        OffenGefuehrt.Font = offenUnklar ? Stil.FettKursiv : Stil.Fett;
        Wohnbereich.Text = b.Wohnbereich;
        BezugZeigen(b);
        BetreuerZeigen(b);
        KurzzeilenZeigen(b);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Invalidate(true);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = S(Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int x = rand, y = rand + AutoScrollPosition.Y;
        int innen = S(Stil.AbstandGross);

        int bildSeite = S(BildGroesse);
        int kopfHoehe = bildSeite + 2 * innen;
        kopfKarte.Bounds = new Rectangle(x, y, breite, kopfHoehe);
        bild.Bounds = new Rectangle(innen, innen, bildSeite, bildSeite);
        int textX = innen + bildSeite + S(Stil.AbstandGross);
        var markenGroesse = kopfMarken.GetPreferredSize(new Size(Math.Max(100, breite - textX - innen), 0));
        int blockHoehe = name.PreferredHeight + S(Stil.AbstandKlein) + markenGroesse.Height;
        int textY = Math.Max(innen, (kopfHoehe - blockHoehe) / 2);
        name.Location = new Point(textX, textY);
        kopfMarken.MaximumSize = new Size(Math.Max(100, breite - textX - innen), 0);
        kopfMarken.Bounds = new Rectangle(new Point(textX, textY + name.PreferredHeight + S(Stil.AbstandKlein)), markenGroesse);
        y += kopfHoehe + S(Stil.AbstandMittel);

        Allergie.Bounds = new Rectangle(x, y, breite, S(52));
        y += Allergie.Height + S(Stil.AbstandKlein);
        var auskunftGroesse = Auskunft.GetPreferredSize(new Size(breite, 0));
        Auskunft.Bounds = new Rectangle(x, y, breite, auskunftGroesse.Height);
        y += Auskunft.Height + S(Stil.AbstandKlein);
        Zielleiste.Bounds = new Rectangle(x, y, breite, Zielleiste.HoeheFuer(breite));
        y += Zielleiste.Height + S(Stil.AbstandMittel);

        Reiter.Bounds = new Rectangle(x, y, breite, Reiter.HoeheFuer(breite));
        y += Reiter.Height + S(Stil.AbstandGross);

        foreach (Control ansicht in new Control[] { Medikation, Wochenplan, Biografie, Reflexion, Heb, Angaben, Dokumente, Vital, Massnahmen }.Concat(ModulAnsichten))
        {
            if (!ansicht.Visible) continue;
            // Der Wochenplan füllt die Höhe bis zum unteren Rand.
            if (ansicht == Wochenplan) Wochenplan.VerfuegbareHoehe = ClientSize.Height - (y - AutoScrollPosition.Y) - rand;
            ansicht.SetBounds(x, y, breite, ansicht.Height);
            ansicht.PerformLayout();
            y += ansicht.Height + rand;
            AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
            return;
        }
        if (ReiterPlatzhalter.Visible)
        {
            ReiterPlatzhalter.Bounds = new Rectangle(x, y, breite, ReiterPlatzhalter.Height);
            y += ReiterPlatzhalter.Height + rand;
            AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
            return;
        }

        abschnitt.Location = new Point(x + S(Stil.AbstandMini), y);
        y += abschnitt.PreferredHeight + S(Stil.AbstandMini);

        var groesse = tabelle.GetPreferredSize(Size.Empty);
        var knoepfe = bezugKnoepfe.GetPreferredSize(new Size(Math.Max(100, breite - 2 * innen), 0));
        stammdaten.Bounds = new Rectangle(x, y, breite, groesse.Height + S(Stil.AbstandKlein) + knoepfe.Height + 2 * innen);
        tabelle.Location = new Point(innen, innen);
        tabelle.Size = groesse;
        bezugKnoepfe.MaximumSize = new Size(Math.Max(100, breite - 2 * innen), 0);
        bezugKnoepfe.Bounds = new Rectangle(new Point(innen, tabelle.Bottom + S(Stil.AbstandKlein)), knoepfe);
        y += stammdaten.Height + rand;
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
    }
}
