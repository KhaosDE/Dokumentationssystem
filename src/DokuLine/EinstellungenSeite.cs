using DokuLine.Kern;

namespace DokuLine;

// Einstellungen wie in den Windows-11-Einstellungen: eine ganze Seite, links die
// Gruppen mit Symbol und oben die Suche, rechts die Einstellungen der Gruppe als Karten (Name, ein
// Satz Erklärung, rechts das Bedienelement). Änderungen wirken sofort – Häkchen und Zahlen beim
// Ändern, Textfelder beim Verlassen; nur ganze Datensätze (Mitarbeiter, Medikament, Fahrzeug,
// Standardaufgabe, HEB-Zuordnung) haben eigene Fenster mit „Speichern“. Ohne Recht bleibt alles
// sichtbar, aber gesperrt, mit dem Grund. Folgenreiches (Datenbank-Ort, Demo-Daten) steht nur unter
// „System“ und fragt vorher nach.
public sealed class EinstellungenSeite : Panel
{
    public static readonly (string Name, string Symbol)[] GruppenListe =
    [
        ("Persönlich", ""), ("Wohnbereich", ""), ("Module", ""), ("Aufgaben", ""), ("Fahrzeuge", ""),
        ("Vorlagen", ""), ("Medikamente", ""), ("Verzeichnisse", ""), ("Verwaltung", ""), ("System", ""), ("Über", ""),
    ];
    private const int LogoBreite = 160, LogoHoehe = 64;

    private readonly Speicher speicher;
    private readonly Panel links = new() { BackColor = Stil.Seite };
    private readonly Panel rechts = new() { AutoScroll = true, BackColor = Stil.Seite };
    private readonly Label titel = new() { AutoSize = true, Font = Stil.Ueberschrift, ForeColor = Stil.TextNormal, BackColor = Stil.Seite };
    private readonly Panel logoVorschau = new() { Size = new Size(LogoBreite, LogoHoehe), BackColor = Stil.Flaeche };
    private Image? logoBild;
    private bool laedt;   // beim Füllen der Felder nichts übernehmen
    private bool trefferZeigen;

    public TextBox Suche { get; } = new() { PlaceholderText = "Einstellung suchen", AccessibleName = "Einstellung suchen" };
    public ListBox Treffer { get; } = new() { IntegralHeight = false, Visible = false, BorderStyle = BorderStyle.None, Font = Stil.Standard };
    public List<SeitenleistenEintrag> Gruppen { get; } = [];
    public List<EinstellungKarte> Karten { get; } = [];
    public string AktiveGruppe { get; private set; } = "Persönlich";
    public string KopfText => AktiveGruppe == VerzeichnisGruppe ? Verzeichnis.KopfText : $"{AktiveGruppe} · Änderungen gelten sofort";
    // „Verzeichnisse“ zeigt statt Karten das Personenverzeichnis; dessen Knöpfe stehen in der Aktionsleiste.
    public const string VerzeichnisGruppe = "Verzeichnisse";
    public VerzeichnisAnsicht Verzeichnis { get; }
    public Button[] Knoepfe => AktiveGruppe == VerzeichnisGruppe ? Verzeichnis.Knoepfe : [];
    public event Action<long>? AkteGewuenscht;

    // Persönlich
    public Label Angemeldet { get; } = new() { AutoSize = true, Font = Stil.Fett, UseMnemonic = false };
    public Button AbmeldenKnopf { get; } = Stil.NebenKnopf(Abmeldung.KnopfText);
    public Panel MeineFarbe { get; } = new() { Size = new Size(28, 28), BorderStyle = BorderStyle.FixedSingle };
    public Button FarbeKnopf { get; } = Stil.NebenKnopf("Farbe wählen …");
    public Button FarbeVorgabeKnopf { get; } = Stil.NebenKnopf("Vorgabe");
    public Func<Color, Color?> FarbeWaehlen { get; set; } = aktuell =>
    {
        using var dialog = new ColorDialog { Color = aktuell, FullOpen = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.Color : null;
    };
    public CheckBox MenueEingeklappt { get; } = new() { Text = "eingeklappt", AutoSize = true };
    // Wohnbereich
    public ComboBox Bereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 220, MaxLength = Kern.Bereiche.Laenge };
    public TextBox Frueh { get; } = new UhrzeitFeld() { Width = 60 };   // Doppelpunkt setzt das Programm
    public TextBox Spaet { get; } = new UhrzeitFeld() { Width = 60 };
    public TextBox Nacht { get; } = new UhrzeitFeld() { Width = 60 };
    public Button ZimmerKnopf { get; } = Stil.NebenKnopf("Zimmer und Betten …");
    public NumericUpDown BeschlussFrist { get; } = new() { Minimum = 1, Maximum = 365, Width = 80 };
    public NumericUpDown HebFrist { get; } = new() { Minimum = 1, Maximum = 60, Width = 80 };
    public Button LogoKnopf { get; } = Stil.NebenKnopf("Bild wählen …");
    public Button LogoWeg { get; } = Stil.NebenKnopf("Bild entfernen");
    // Module, Aufgaben, Fahrzeuge, Vorlagen, Medikamente, Verwaltung
    public Dictionary<string, CheckBox> Module { get; } = [];
    public Button VorlagenKnopf { get; } = Stil.NebenKnopf("Standardaufgaben …");
    public Button FahrzeugeKnopf { get; } = Stil.NebenKnopf("Fahrzeuge …");
    public Dictionary<Bogenart, Label> Vorlagen { get; } = [];
    public Func<string?> PdfWaehlen { get; set; } = () =>
    {
        using var dialog = new OpenFileDialog { Title = "HEB-Vorlage (PDF) wählen", Filter = "PDF (*.pdf)|*.pdf" };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    };
    public Button MedikamenteKnopf { get; } = Stil.NebenKnopf("Medikamentendatenbank …");
    public CheckBox NachschlageAnsicht { get; } = new() { Text = "anzeigen", AutoSize = true };
    public Button MitarbeiterKnopf { get; } = Stil.NebenKnopf("Mitarbeiter …");
    public Button ListenKnopf { get; } = Stil.NebenKnopf("Bereiche, Positionen, Berichtsarten …");
    public Button RechteKnopf { get; } = Stil.NebenKnopf("Rechte …");
    // System, Über
    public Label DatenbankOrt { get; } = new() { AutoSize = true, MaximumSize = new Size(360, 0), UseMnemonic = false };
    public Button OrdnerKnopf { get; } = Stil.NebenKnopf("Gemeinsamen Ordner wählen …");
    public Button LokalKnopf { get; } = Stil.NebenKnopf("Lokal (Vorgabe)");
    public Func<string?> OrdnerWaehlen { get; set; } = () =>
    {
        using var dialog = new FolderBrowserDialog { Description = "Gemeinsamer Ordner für die Datenbank (nur ausgedachte Daten)", UseDescriptionForTitle = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    };
    public CheckBox AutoUpdate { get; } = new() { Text = "beim Start suchen", AutoSize = true };
    public Button DemoKnopf { get; } = Stil.NebenKnopf("Demo-Daten zurücksetzen …");
    public Label WebAnsichtStand { get; } = new() { AutoSize = true, MaximumSize = new Size(360, 0) };
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    // Rückfrage vor Folgenreichem (Datenbank-Ort); true = ja. Zum Testen ersetzbar.
    public Func<string, bool> Rueckfrage { get; set; }

    public event Action? LogoGeaendert;
    public event Action? BereichGeaendert;
    public event Action? SchichtzeitenGeaendert;
    public event Action? AbmeldenGewuenscht;
    public event Action<bool>? MenueGewuenscht;
    public event Action? DemoGewuenscht;
    public event Action? Geaendert;

    public EinstellungenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Rueckfrage = text => MessageBox.Show(FindForm(), text, "Einstellungen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

        // ----- links: Suche und Gruppen -----
        links.Controls.Add(Suche);
        links.Controls.Add(Treffer);
        foreach (var (name, symbol) in GruppenListe)
        {
            var eintrag = new SeitenleistenEintrag(name) { Symbol = symbol };
            eintrag.Click += (_, _) => GruppeWaehlen(name);
            Gruppen.Add(eintrag);
            links.Controls.Add(eintrag);
        }
        Suche.TextChanged += (_, _) => SucheZeigen();
        Suche.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Springen(Suche.Text); } };
        Treffer.Click += (_, _) => { if (Treffer.SelectedItem is string t) Springen(t.Split(" · ")[0]); };
        Treffer.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter && Treffer.SelectedItem is string t) Springen(t.Split(" · ")[0]); };
        links.Layout += (_, _) => LinksAnordnen();
        rechts.Controls.Add(titel);
        Verzeichnis = new VerzeichnisAnsicht(speicher) { Visible = false };
        Verzeichnis.AkteGewuenscht += id => AkteGewuenscht?.Invoke(id);
        Verzeichnis.Geaendert += () => { if (AktiveGruppe == VerzeichnisGruppe) Geaendert?.Invoke(); };
        rechts.Controls.Add(Verzeichnis);
        rechts.Layout += (_, _) => RechtsAnordnen();
        Controls.AddRange([rechts, links]);

        KartenBauen();
        foreach (var k in Karten) rechts.Controls.Add(k);
        Fehler.MaximumSize = new Size(560, 0);
        rechts.Controls.Add(Fehler);
        GruppeWaehlen("Persönlich");
    }

    // ------------------------------------------------------------------ Karten

    private EinstellungKarte Karte(string gruppe, string name, string erklaerung, Control bedienung, string? recht = null, bool darunter = false)
    {
        var k = new EinstellungKarte(gruppe, name, erklaerung, bedienung, darunter) { Funktion = recht };
        Karten.Add(k);
        return k;
    }

    private static FlowLayoutPanel Reihe(params Control[] teile)
    {
        var r = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0), BackColor = Stil.Flaeche };
        foreach (var t in teile)
        {
            t.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            if (t is Button b) b.AutoSize = true;
            r.Controls.Add(t);
        }
        return r;
    }

    private void KartenBauen()
    {
        // Persönlich
        AbmeldenKnopf.Click += (_, _) => AbmeldenGewuenscht?.Invoke();
        Karte("Persönlich", "Angemeldet als", "Anmelden mit dem Kürzel, ohne Passwort (Demo). Das Kürzel steht bei Berichten, Übergabe und Aufgaben. Benutzer wechseln: abmelden (F12) und mit dem anderen Kürzel anmelden.",
            Reihe(Angemeldet, AbmeldenKnopf));
        FarbeKnopf.Click += (_, _) =>
        {
            if (Recht.Kuerzel is { Length: > 0 } k && FarbeWaehlen(MeineFarbe.BackColor) is Color c)
            {
                speicher.FarbeSetzen(k, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
                FarbeZeigen();
            }
        };
        FarbeVorgabeKnopf.Click += (_, _) => { if (Recht.Kuerzel is { Length: > 0 } k) { speicher.FarbeSetzen(k, null); FarbeZeigen(); } };
        Karte("Persönlich", "Meine Farbe", "Für Bezugspflege (Streifen auf der Karte) und Einzeltrainer (Punkt). Gilt auf allen Rechnern.",
            Reihe(MeineFarbe, FarbeKnopf, FarbeVorgabeKnopf));
        MenueEingeklappt.CheckedChanged += (_, _) => { if (!laedt) MenueGewuenscht?.Invoke(MenueEingeklappt.Checked); };
        Karte("Persönlich", "Seitenmenü", "Eingeklappt zeigt die Seitenleiste nur Symbole; der Name steht beim Darüberfahren.", MenueEingeklappt);

        // Wohnbereich (Recht „Einstellungen des Bereichs“)
        Bereich.Items.AddRange([.. Kern.Bereiche.Vorschlaege]);
        Bereich.Leave += (_, _) => BereichUebernehmen();
        Bereich.SelectedIndexChanged += (_, _) => BereichUebernehmen();
        Bereich.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BereichUebernehmen(); } };
        Karte("Wohnbereich", "Bereich dieses Rechners", "Ausgedachte Kennung des Arbeitsplatzes. Die Übersicht zeigt zuerst die Bewohner dieses Bereichs; Statusmeldungen tragen ihn mit.",
            Bereich, Funktionen.EinstellungenBereich);
        foreach (var feld in new[] { Frueh, Spaet, Nacht })
        {
            feld.Leave += (_, _) => SchichtzeitenUebernehmen();
            feld.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SchichtzeitenUebernehmen(); } };
        }
        Frueh.AccessibleName = "Beginn Frühdienst";
        Spaet.AccessibleName = "Beginn Spätdienst";
        Nacht.AccessibleName = "Beginn Nachtdienst";
        Karte("Wohnbereich", "Schichtzeiten", "Beginn von Früh, Spät und Nacht für diesen Bereich (alle Rechner). Danach richten sich Schicht der Berichte, Übergabe und Aufgaben.",
            Reihe(Frueh, Spaet, Nacht), Funktionen.EinstellungenBereich);
        BeschlussFrist.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { BeschlussFristTage = (int)BeschlussFrist.Value }); };
        Karte("Wohnbereich", "Beschluss endet bald", "So viele Tage vorher zeigt die Übersicht „Beschluss endet in …“.", BeschlussFrist, Funktionen.EinstellungenBereich);
        HebFrist.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { HebFristMonate = (int)HebFrist.Value }); };
        Karte("Wohnbereich", "HEB fällig", "So viele Monate nach dem letzten Bogen gilt der HEB-Bogen als fällig.", HebFrist, Funktionen.EinstellungenBereich);
        logoVorschau.Paint += (_, e) => LogoZeichnen(e.Graphics);
        LogoKnopf.Click += (_, _) => LogoWaehlen();
        LogoWeg.Click += (_, _) => LogoEntfernen();
        Karte("Wohnbereich", "Bild im Kopf (Logo)", "Erscheint oben rechts. Die Datei wird in den Datenordner kopiert und nie mitgeliefert.",
            Reihe(logoVorschau, LogoKnopf, LogoWeg), Funktionen.EinstellungenBereich);
        // Wohnbereiche R1–R3 (offen/beschützend), Zimmer und Betten; Rechte prüft das Fenster.
        ZimmerKnopf.Click += (_, _) => { using var f = new ZimmerFenster(speicher); Dialog.Zeigen(f, FindForm()); };
        Karte("Wohnbereich", "Zimmer und Betten", "Wohnbereiche (offen oder beschützend) mit ihren Zimmern: Einzelzimmer oder „Bett Fenster“ und „Bett Tür“. Die Aufnahme zeigt nur freie Betten.",
            ZimmerKnopf);

        // Module (Recht „Module ein- und ausschalten“): ein Modul je Karte, Untermodule darin (eine Ebene).
        foreach (var m in Modulverwaltung.Alle)
        {
            var spalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
            var haken = new CheckBox { Text = "eingeschaltet", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche };
            Module[m.Daten.Schluessel] = haken;
            spalte.Controls.Add(haken);
            string schluessel = m.Daten.Schluessel;
            haken.CheckedChanged += (_, _) => ModulSetzen(schluessel, haken.Checked);
            foreach (var u in m.Untermodule)
            {
                var unter = new CheckBox { Text = $"{u.Name} – {u.Beschreibung}", AutoSize = true, MaximumSize = new Size(420, 0), BackColor = Stil.Flaeche, Margin = new Padding(Stil.AbstandGross, 0, 0, 0) };
                Module[u.Schluessel] = unter;
                string us = u.Schluessel;
                unter.CheckedChanged += (_, _) => ModulSetzen(us, unter.Checked);
                spalte.Controls.Add(unter);
            }
            Karte("Module", m.Name, $"{m.Beschreibung} Ausgeschaltet verschwindet es aus Seitenleiste, Akte und Karten; die Daten bleiben.",
                spalte, Funktionen.Module, darunter: true);
        }

        // Aufgaben, Fahrzeuge (eigene Fenster mit „Speichern“)
        VorlagenKnopf.Click += (_, _) => { FensterZeigen(new VorlagenFenster(speicher)); Geaendert?.Invoke(); };
        Karte("Aufgaben", "Standardaufgaben", "Aufgaben, die zu Beginn jeder Schicht von selbst entstehen (z. B. Rundgänge im Nachtdienst). Ohne Recht nur ansehen.", VorlagenKnopf);
        FahrzeugeKnopf.Click += (_, _) => { using var f = new FahrzeugeFenster(speicher); Dialog.Zeigen(f, FindForm()); Geaendert?.Invoke(); };
        Karte("Fahrzeuge", "Fahrzeugliste", "Firmenwagen für Fahrten (Arzt, Ausflug, Sporttherapie); hinzufügen, stilllegen, wieder aktivieren.", FahrzeugeKnopf, Funktionen.Fahrzeuge);

        // Vorlagen: HEB-PDFs je Bogenart
        foreach (var art in Enum.GetValues<Bogenart>())
        {
            var status = new Label { AutoSize = true, MaximumSize = new Size(360, 0), BackColor = Stil.Flaeche, Margin = new Padding(0, 0, 0, Stil.AbstandMini) };
            Vorlagen[art] = status;
            var zuteilen = Stil.NebenKnopf("Zuteilen …");
            var entfernen = Stil.NebenKnopf("Entfernen");
            var zuordnung = Stil.NebenKnopf("Zuordnung …");
            zuteilen.Click += (_, _) => { if (PdfWaehlen() is string pfad) VorlageZuteilen(art, pfad); };
            entfernen.Click += (_, _) => { HebPdf.VorlageEntfernen(art); VorlageAnzeigen(art); };
            zuordnung.Click += (_, _) => { using var f = new HebZuordnungFenster(art); Dialog.Zeigen(f, FindForm()); };
            var spalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
            spalte.Controls.AddRange([status, Reihe(zuteilen, entfernen, zuordnung)]);
            Karte("Vorlagen", $"HEB-Bogen {HebRegeln.ArtKurz(art)}", "PDF mit Formularfeldern des Kostenträgers. Sie wird in den Datenordner kopiert und nie mitgeliefert; ausgefüllt wird immer eine Kopie.",
                spalte, Funktionen.Heb);
        }

        // Medikamente
        MedikamenteKnopf.Click += (_, _) => { using var f = new MedikamenteFenster(speicher); Dialog.Zeigen(f, FindForm()); };
        Karte("Medikamente", "Medikamentendatenbank", "Liste, aus der beim Verordnen gewählt wird. Anlegen dürfen Fachkräfte (sie prüfen mit Quelle und Datum), verwalten der Psychologische Fachdienst.",
            MedikamenteKnopf);
        NachschlageAnsicht.CheckedChanged += (_, _) => { if (!laedt) Einstellungen.Aendern(w => w with { NachschlageAnsicht = NachschlageAnsicht.Checked }); };
        Karte("Medikamente", "Nachschlage-Ansicht (BfArM)", "Eingebettete Suche des BfArM über der Maske „Medikament“ – die einzige Webansicht im Programm, nur feste Adressen (" +
            string.Join(", ", ExterneAdressen.Nachschlagebereiche) + "). Aus: nur „Im Browser öffnen“.", NachschlageAnsicht);

        // Verwaltung
        MitarbeiterKnopf.Click += (_, _) => FensterZeigen(new MitarbeiterFenster(speicher));
        ListenKnopf.Click += (_, _) => FensterZeigen(new ListenFenster(speicher));
        RechteKnopf.Click += (_, _) => FensterZeigen(new RechteFenster(speicher));
        Karte("Verwaltung", "Mitarbeiter", "Mitarbeiter mit Bereichen, Positionen und Qualifikation (Assistenzkraft oder Fachkraft). Die Bereichsleitung verwaltet ihren Bereich.", MitarbeiterKnopf);
        Karte("Verwaltung", "Bereiche, Positionen und Berichtsarten",
            "Listen zum Zuordnen; stilllegen statt löschen, solange etwas zugeordnet ist.", ListenKnopf);
        Karte("Verwaltung", "Rechte", "Tabelle: welche Position was darf, je Funktion „nur Fachkräfte“. Ohne Recht nur ansehen.", RechteKnopf);

        // System (folgenreich: fragt vorher)
        OrdnerKnopf.Click += (_, _) => { if (OrdnerWaehlen() is string ordner) OrdnerSetzen(ordner); };
        LokalKnopf.Click += (_, _) => OrdnerSetzen(null);
        var ort = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
        ort.Controls.AddRange([DatenbankOrt, Reihe(OrdnerKnopf, LokalKnopf)]);
        DatenbankOrt.BackColor = Stil.Flaeche;
        Karte("System", "Ort der Datenbank", "Mehrere Rechner mit demselben Ordner arbeiten gemeinsam. Gilt nach dem Neustart; ist der Ordner leer, entstehen dort die Demo-Daten. Auch dort nur ausgedachte Daten.",
            ort, Funktionen.EinstellungenBereich, darunter: true);
        Karte("System", "Sicherung", "Noch nicht gebaut (siehe STAND.md, offene Punkte). Die Datenbank ist eine Datei im Datenordner bzw. im gemeinsamen Ordner.",
            new Label { Text = "–", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche }, Funktionen.EinstellungenBereich);
        AutoUpdate.CheckedChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { AutoUpdate = AutoUpdate.Checked }); };
        Karte("System", "Update", "Beim Start nach einer neuen Version suchen (nur die Releases dieses Programms). Abschaltbar auch mit „--kein-update“.",
            AutoUpdate, Funktionen.EinstellungenBereich);
        DemoKnopf.Click += (_, _) => DemoGewuenscht?.Invoke();
        var achtung = new Label
        {
            Text = "Achtung: löscht alle Änderungen und hinterlegten Bilder und lädt die ausgedachten Demo-Daten neu (fragt vorher).",
            AutoSize = true, MaximumSize = new Size(360, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandMini),
        };
        Karte("System", "Demo-Daten zurücksetzen", "Für die Vorführung: alles auf den Anfang.", Reihe(DemoKnopf, achtung), Funktionen.DemoZuruecksetzen, darunter: true);

        // Über
        Karte("Über", "Programm", "Bis Version 0.29.1 hieß das Programm „Dokumentationssystem“.",
            new Label { Text = $"{Programmversion.Name} – {Programmversion.Leittext}", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche });
        Karte("Über", "Version", "Neue Versionen kommen über das Selbst-Update.", new Label { Text = Programmversion.Text, AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche });
        Karte("Über", "Tastenkürzel", "Gelten im ganzen Programm.",
            new Label { Text = "F12 = Abmelden · F11 = Tagesplan im Vollbild (Esc beendet)", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche });
        Karte("Über", "Demo", "Kein Echtbetrieb: Echte Daten wären Gesundheitsdaten (Art. 9 DSGVO) und bräuchten Freigaben, die es nicht gibt.",
            new MarkenFeld(Stil.DemoHinweis, Stil.Achtung));
        Karte("Über", "Bausteine und Lizenzen", "Freie Bibliotheken, die das Programm benutzt.",
            new Label
            {
                Text = "Microsoft.Data.Sqlite (MIT) · PDFsharp 6.2.4 (MIT) · System.Security.Cryptography.Pkcs (MIT) · WebView2-Steuerelement von Microsoft (BSD-artige Lizenz) · .NET 10 (MIT)",
                AutoSize = true, MaximumSize = new Size(360, 0), BackColor = Stil.Flaeche,
            }, darunter: true);
        WebAnsichtStand.BackColor = Stil.Flaeche;
        Karte("Über", "Nachschlage-Ansicht", "Ob Microsoft Edge WebView2 auf diesem Rechner da ist (Hinweis für die IT).", WebAnsichtStand);
    }

    // ------------------------------------------------------------------ Anzeigen

    // Beim Öffnen: Werte und Rechte neu lesen (Gruppe bleibt).
    public void Zeigen()
    {
        laedt = true;
        try
        {
            var werte = Einstellungen.Laden();
            var ich = speicher.MitarbeiterLesen(werte.Kuerzel);
            Angemeldet.Text = ich is null ? "niemand" : ich.Anzeige;
            FarbeZeigen();
            MenueEingeklappt.Checked = werte.MenueEingeklappt;
            Bereich.Text = werte.Bereich;
            var zeiten = speicher.Schichtzeiten(werte.Bereich);
            (Frueh.Text, Spaet.Text, Nacht.Text) = (zeiten.Frueh.ToString("HH:mm"), zeiten.Spaet.ToString("HH:mm"), zeiten.Nacht.ToString("HH:mm"));
            BeschlussFrist.Value = Math.Clamp(werte.BeschlussFristTage, (int)BeschlussFrist.Minimum, (int)BeschlussFrist.Maximum);
            HebFrist.Value = Math.Clamp(werte.HebFristMonate, (int)HebFrist.Minimum, (int)HebFrist.Maximum);
            LogoLaden(Einstellungen.LogoPfad());
            foreach (var (schluessel, haken) in Module) haken.Checked = speicher.ModulAn(schluessel);
            foreach (var art in Vorlagen.Keys) VorlageAnzeigen(art);
            NachschlageAnsicht.Checked = werte.NachschlageAnsicht;
            OrtAnzeigen(werte.DatenbankOrdner);
            AutoUpdate.Checked = werte.AutoUpdate;
            WebAnsichtStand.Text = $"WebView2 {Nachschlageansicht.Laufzeit}";
            foreach (var k in Karten) k.Sperren(k.Funktion is string r ? Recht.Grund(speicher, r) : null);
            ModulHakenAbgleichen();   // nach den Karten – deren Sperre gäbe die Untermodule sonst wieder frei
            // Erst nach den Karten: Deren Sperre setzt die Knöpfe sonst wieder frei. Die eigene Farbe braucht eine Anmeldung.
            foreach (var k in new[] { FarbeKnopf, FarbeVorgabeKnopf }) Stil.Sperren(k, ich is null ? RechteRegeln.NichtAngemeldetText : null);
            Stil.Sperren(MitarbeiterKnopf, Recht.Darf(speicher, Funktionen.MitarbeiterAlle) ? null : Recht.Grund(speicher, Funktionen.MitarbeiterBereich));
            Fehler.Visible = fehlerZeigen = false;
        }
        finally { laedt = false; }
        GruppeWaehlen(AktiveGruppe);
    }

    // Eine Gruppe ohne eine einzige erlaubte Karte verschwindet; die Verzeichnisse darf jeder ansehen.
    public bool GruppeSichtbar(string gruppe) => gruppe == VerzeichnisGruppe || Karten.Any(k => k.Gruppe == gruppe && !k.OhneRecht);

    public void GruppeWaehlen(string gruppe)
    {
        if (!GruppeSichtbar(gruppe)) gruppe = "Persönlich";
        AktiveGruppe = gruppe;
        foreach (var g in Gruppen) g.Visible = GruppeSichtbar(g.Text);
        LinksAnordnen();
        foreach (var g in Gruppen) g.Aktiv = g.Text == gruppe;
        titel.Text = gruppe;
        foreach (var k in Karten) k.Visible = k.Gruppe == gruppe && !k.OhneRecht;
        Verzeichnis.Visible = gruppe == VerzeichnisGruppe;
        if (Verzeichnis.Visible) Verzeichnis.Aktualisieren();
        rechts.AutoScrollPosition = Point.Empty;
        rechts.PerformLayout();
        Geaendert?.Invoke();
    }

    // Suche über Name und Erklärung; Treffer als Liste links.
    public List<EinstellungKarte> Suchen(string text) =>
        text.Trim().Length == 0 ? [] : Karten.Where(k => !k.OhneRecht && k.Passt(text.Trim())).ToList();

    private void SucheZeigen()
    {
        var treffer = Suchen(Suche.Text);
        Treffer.BeginUpdate();
        Treffer.Items.Clear();
        Treffer.Items.AddRange([.. treffer.Select(k => (object)$"{k.Titel} · {k.Gruppe}")]);
        if (treffer.Count == 0 && Suche.Text.Trim().Length > 0) Treffer.Items.Add("Nichts gefunden.");
        Treffer.EndUpdate();
        Treffer.Visible = trefferZeigen = Suche.Text.Trim().Length > 0;
        LinksAnordnen();
    }

    // Zum ersten Treffer springen: Gruppe wählen, Karte zeigen und kurz hervorheben.
    public bool Springen(string text)
    {
        var k = Karten.FirstOrDefault(x => !x.OhneRecht && x.Titel.Equals(text.Trim(), StringComparison.CurrentCultureIgnoreCase))
            ?? Suchen(text).FirstOrDefault();
        if (k is null) return false;
        GruppeWaehlen(k.Gruppe);
        foreach (var x in Karten) x.Hervorgehoben = x == k;
        rechts.ScrollControlIntoView(k);
        return true;
    }

    // ------------------------------------------------------------------ Übernehmen

    public bool BereichUebernehmen()
    {
        if (laedt) return false;
        string neu = Kern.Bereiche.Bereinigen(Bereich.Text);
        if (neu == Einstellungen.Laden().Bereich) return true;
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        if (Kern.Bereiche.Pruefen(Bereich.Text) is string fehler) return FehlerZeigen(fehler);
        Einstellungen.Aendern(w => w with { Bereich = neu });
        var zeiten = speicher.Schichtzeiten(neu);
        BerichtRegeln.Zeiten = zeiten;
        laedt = true;
        (Frueh.Text, Spaet.Text, Nacht.Text) = (zeiten.Frueh.ToString("HH:mm"), zeiten.Spaet.ToString("HH:mm"), zeiten.Nacht.ToString("HH:mm"));
        laedt = false;
        Fehler.Visible = fehlerZeigen = false;
        BereichGeaendert?.Invoke();
        return true;
    }

    public bool SchichtzeitenUebernehmen()
    {
        if (laedt) return false;
        // Unverändert: nichts zu tun – auch ohne Recht kein Fehler beim Verlassen des Felds.
        var bisher = speicher.Schichtzeiten(Einstellungen.Laden().Bereich);
        if ((Frueh.Text.Trim(), Spaet.Text.Trim(), Nacht.Text.Trim()) == (bisher.Frueh.ToString("HH:mm"), bisher.Spaet.ToString("HH:mm"), bisher.Nacht.ToString("HH:mm")))
        {
            Fehler.Visible = fehlerZeigen = false;
            return true;
        }
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        Kern.Schichtzeiten zeiten;
        try { zeiten = Kern.Schichtzeiten.AusEingabe(Frueh.Text, Spaet.Text, Nacht.Text); }
        catch (Eingabefehler f) { return FehlerZeigen(string.Join(" ", f.Fehler)); }
        speicher.SchichtzeitenSetzen(Einstellungen.Laden().Bereich, zeiten);
        BerichtRegeln.Zeiten = zeiten;
        Fehler.Visible = fehlerZeigen = false;
        SchichtzeitenGeaendert?.Invoke();
        return true;
    }

    private void ModulSetzen(string schluessel, bool an)
    {
        ModulHakenAbgleichen();
        if (laedt || speicher.ModulAn(schluessel) == an) return;
        if (Recht.Grund(speicher, Funktionen.Module) is string grund) { FehlerZeigen(grund); return; }
        speicher.ModulSetzen(schluessel, an);   // Hauptfenster baut Seitenleiste und Akte neu (ModuleGeaendert)
    }

    // Ein Fenster der Einstellungen (Mitarbeiter, Rechte …): danach Werte und Sperren neu – die Rechte des Angemeldeten
    // können sich geändert haben.
    private void FensterZeigen(Form fenster)
    {
        using (fenster) Dialog.Zeigen(fenster, FindForm());
        Zeigen();
    }

    // Rechte auch im Handler prüfen: Die Sperre der Karte allein verhindert nicht jeden Weg.
    private bool Darf(string funktion)
    {
        if (Recht.Grund(speicher, funktion) is not string grund) return true;
        FehlerZeigen(grund);
        return false;
    }

    // Untermodule nur, wenn das Modul an ist.
    private void ModulHakenAbgleichen()
    {
        foreach (var m in Modulverwaltung.Alle)
            foreach (var u in m.Untermodule)
                Module[u.Schluessel].Enabled = Module[m.Daten.Schluessel].Checked && Module[m.Daten.Schluessel].Enabled;
    }

    // null = lokal. Ein gewählter Ordner muss erreichbar und beschreibbar sein; vorher fragen.
    public bool OrdnerSetzen(string? ordner)
    {
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        if (ordner is not null && Speicher.OrdnerPruefen(ordner) is string fehler) return FehlerZeigen(fehler);
        if (ordner == Einstellungen.Laden().DatenbankOrdner) return true;
        if (!Rueckfrage((ordner is null ? "Wieder die lokale Datenbank dieses Rechners nehmen?" : $"Die Datenbank im gemeinsamen Ordner „{ordner}“ nehmen?") +
                        "\n\nDas gilt nach dem Neustart des Programms."))
            return false;
        Einstellungen.Aendern(w => w with { DatenbankOrdner = ordner });
        Fehler.Visible = fehlerZeigen = false;
        OrtAnzeigen(ordner);
        BereichGeaendert?.Invoke();   // Hinweis „neuer Ort nach Neustart“ in der Seitenleiste
        return true;
    }

    private void OrtAnzeigen(string? ordner)
    {
        DatenbankOrt.Text = ordner is null ? "lokal im Datenordner dieses Rechners" : $"gemeinsamer Ordner: {ordner}";
        LokalKnopf.Enabled = ordner is not null;
    }

    public bool VorlageZuteilen(Bogenart art, string pfad)
    {
        if (Recht.Grund(speicher, Funktionen.Heb) is string grund) return FehlerZeigen(grund);
        try { HebPdf.VorlageZuteilen(art, pfad); }
        catch (Exception e) when (e is Eingabefehler or IOException or UnauthorizedAccessException)
        {
            return FehlerZeigen(e is Eingabefehler f ? string.Join(" ", f.Fehler) : $"Zuteilen ging nicht: {e.Message}");
        }
        Fehler.Visible = fehlerZeigen = false;
        VorlageAnzeigen(art);
        return true;
    }

    private void VorlageAnzeigen(Bogenart art)
    {
        string? pfad = HebPdf.Vorlage(art);
        try
        {
            Vorlagen[art].Text = pfad is null ? "keine Vorlage – der Bogen lässt sich trotzdem schreiben und drucken"
                : $"zugeteilt ({HebPdf.Felder(pfad).Count} Formularfelder)";
        }
        catch (Eingabefehler) { Vorlagen[art].Text = "zugeteilt, aber nicht lesbar – bitte neu zuteilen"; }
    }

    private void FarbeZeigen() =>
        MeineFarbe.BackColor = Recht.Kuerzel is { Length: > 0 } k && speicher.MitarbeiterLesen(k) is not null
            ? ColorTranslator.FromHtml(speicher.Farbe(k)) : Stil.Fensterhintergrund;

    public bool HatLogo => logoBild is not null;

    public bool LogoSetzen(string pfad)
    {
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        using (var probe = Bewohnerbild.Laden(pfad))
            if (probe is null || !Bilder.EndungErlaubt(pfad))
                return FehlerZeigen($"„{Path.GetFileName(pfad)}“ ist kein lesbares Bild ({string.Join(", ", Bilder.Endungen)}).");
        try { Einstellungen.LogoSetzen(pfad); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return FehlerZeigen($"Übernehmen ging nicht: {e.Message}"); }
        Fehler.Visible = fehlerZeigen = false;
        LogoLaden(Einstellungen.LogoPfad());
        LogoGeaendert?.Invoke();
        return true;
    }

    public void LogoEntfernen()
    {
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) { FehlerZeigen(grund); return; }
        Einstellungen.LogoSetzen(null);
        LogoLaden(null);
        LogoGeaendert?.Invoke();
    }

    private void LogoWaehlen()
    {
        string muster = string.Join(";", Bilder.Endungen.Select(e => "*" + e));
        using var dialog = new OpenFileDialog { Title = "Bild für den Kopf wählen", Filter = $"Bilder ({muster})|{muster}" };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LogoSetzen(dialog.FileName);
    }

    private void LogoLaden(string? pfad)
    {
        logoBild?.Dispose();
        logoBild = Bewohnerbild.Laden(pfad);
        LogoWeg.Enabled = logoBild is not null;
        logoVorschau.Invalidate();
    }

    private void LogoZeichnen(Graphics g)
    {
        g.Clear(Stil.Flaeche);
        var rahmen = new Rectangle(0, 0, logoVorschau.Width - 1, logoVorschau.Height - 1);
        using (var stift = new Pen(Stil.KarteRand)) g.DrawRectangle(stift, rahmen);
        if (logoBild is null)
        {
            TextRenderer.DrawText(g, "kein Bild", Stil.Klein, rahmen, Stil.TextLeise, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(logoBild, KopfBereich.Einpassen(logoBild.Size, Rectangle.Inflate(rahmen, -4, -4)));
    }

    // Fehler oben in der Gruppe, in Gefahrenfarbe mit Text (Farbe nie allein). Immer false.
    private bool fehlerZeigen;

    private bool FehlerZeigen(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = fehlerZeigen = true;
        rechts.PerformLayout();
        return false;
    }

    // ------------------------------------------------------------------ Anordnung

    private int S(int wert) => Stil.Px(this, wert);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breiteLinks = S(240);
        links.SetBounds(0, 0, breiteLinks, ClientSize.Height);
        rechts.SetBounds(breiteLinks, 0, Math.Max(100, ClientSize.Width - breiteLinks), ClientSize.Height);
    }

    private void LinksAnordnen()
    {
        int rand = S(Stil.AbstandGross), breite = Math.Max(100, links.ClientSize.Width - 2 * rand), y = rand;
        Suche.SetBounds(rand, y, breite, Suche.PreferredHeight);
        y = Suche.Bottom + S(Stil.AbstandKlein);
        if (trefferZeigen)
        {
            Treffer.SetBounds(rand, y, breite, Math.Min(S(240), Math.Max(1, Treffer.Items.Count) * Treffer.ItemHeight + S(6)));
            y = Treffer.Bottom + S(Stil.AbstandKlein);
        }
        foreach (var g in Gruppen.Where(g => GruppeSichtbar(g.Text)))
        {
            g.SetBounds(rand, y, breite, S(36));
            y += S(36) + S(Stil.AbstandMini);
        }
    }

    private void RechtsAnordnen()
    {
        int rand = S(Stil.AbstandGross), breite = Math.Max(100, Math.Min(rechts.ClientSize.Width - 2 * rand, S(900)));
        int y = rand + rechts.AutoScrollPosition.Y;
        titel.Location = new Point(rand, y);
        y = titel.Bottom + S(Stil.AbstandMittel);
        if (Fehler.Text.Length > 0 && fehlerZeigen)
        {
            Fehler.MaximumSize = new Size(breite, 0);
            Fehler.Location = new Point(rand, y);
            y = Fehler.Bottom + S(Stil.AbstandKlein);
        }
        foreach (var k in Karten.Where(k => k.Gruppe == AktiveGruppe && !k.OhneRecht))
        {
            k.SetBounds(rand, y, breite, k.Height);
            k.PerformLayout();
            y = k.Bottom + S(Stil.AbstandKlein);
        }
        if (Verzeichnis.Visible)
        {
            // Füllt die Fläche (ohne Bildlauf der Seite; die Liste rollt selbst).
            int breiteVoll = Math.Max(100, rechts.ClientSize.Width - 2 * rand);
            Verzeichnis.SetBounds(rand, y, breiteVoll, Math.Max(S(240), rechts.ClientSize.Height - y - rand));
            y = Verzeichnis.Bottom;
        }
        rechts.AutoScrollMinSize = new Size(0, y - rechts.AutoScrollPosition.Y + rand);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) logoBild?.Dispose();
        base.Dispose(disposing);
    }
}

// Eine Einstellung als Karte: Name, ein Satz Erklärung, rechts (oder darunter) das Bedienelement.
// Ohne Recht gesperrt (Knöpfe grau mit Grund, Felder nicht änderbar) und mit dem Satz darunter.
public sealed class EinstellungKarte : KartenFlaeche
{
    private readonly Label name = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label erklaerung = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label sperre = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(2), Visible = false };
    private readonly bool darunter;
    private bool hervorgehoben;

    public string Gruppe { get; }
    public string Titel { get; }
    public string Erklaerung { get; }
    public Control Bedienung { get; }
    public string? Funktion { get; init; }   // Recht, ohne das die Karte ausgeblendet ist
    public string? Sperrgrund { get; private set; }

    // Ohne Recht ausgeblendet: das Recht der Karte fehlt, oder ihr einziger Knopf ist ohne Recht.
    public bool OhneRecht => RechteRegeln.FehltRecht(Sperrgrund) || Bedienung is Knopf { OhneRecht: true };

    public EinstellungKarte(string gruppe, string name, string erklaerung, Control bedienung, bool darunter)
    {
        Gruppe = gruppe;
        Titel = name;
        Erklaerung = erklaerung;
        Bedienung = bedienung;
        this.darunter = darunter;
        this.name.Text = name;
        this.erklaerung.Text = erklaerung;
        if (bedienung is not Button && bedienung is not Label && bedienung is not MarkenFeld) bedienung.BackColor = Stil.Flaeche;
        Controls.AddRange([this.name, this.erklaerung, sperre, bedienung]);
        AccessibleName = $"{gruppe}: {name}";
        Visible = false;
    }

    public bool Hervorgehoben
    {
        get => hervorgehoben;
        set { hervorgehoben = value; Invalidate(); }
    }

    public bool Passt(string text) =>
        Titel.Contains(text, StringComparison.CurrentCultureIgnoreCase) || Erklaerung.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
        Gruppe.Contains(text, StringComparison.CurrentCultureIgnoreCase);

    // Gesperrt (z. B. ohne Anmeldung): Knöpfe nennen den Grund, alles andere ist nicht änderbar.
    public void Sperren(string? grund)
    {
        Sperrgrund = grund;
        sperre.Text = grund ?? "";
        sperre.Visible = grund is not null;
        void Durch(Control c)
        {
            if (c is Button b) Stil.Sperren(b, grund);
            else if (c is CheckBox or NumericUpDown or ComboBox) c.Enabled = grund is null;
            else if (c is TextBox t) t.ReadOnly = grund is not null;
            foreach (Control kind in c.Controls) Durch(kind);
        }
        Durch(Bedienung);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Bedienung is null) return;   // Anordnung schon während des Aufbaus
        int innen = Stil.Px(this, Stil.AbstandGross), luecke = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, Width - 2 * innen);
        var bedien = Bedienung.GetPreferredSize(new Size(breite, 0));
        bool unten = darunter || bedien.Width > breite / 2;
        int textBreite = unten ? breite : Math.Max(100, breite - bedien.Width - luecke);
        int y = innen;
        foreach (var l in new[] { name, erklaerung, sperre }.Where(l => l != sperre || Sperrgrund is not null))   // Visible meldet false, solange die Seite verborgen ist
        {
            l.MaximumSize = new Size(textBreite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, 2);
        }
        if (unten)
        {
            Bedienung.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), bedien);
            y = Bedienung.Bottom;
        }
        else
        {
            Bedienung.Bounds = new Rectangle(new Point(innen + breite - bedien.Width, Math.Max(innen, innen + (y - innen - bedien.Height) / 2)), bedien);
            y = Math.Max(y, Bedienung.Bottom);
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!hervorgehoben) return;   // Sprungziel der Suche: Balken links
        using var pinsel = new SolidBrush(Stil.Zimmer.Text);
        e.Graphics.FillRectangle(pinsel, Stil.Px(this, 2), Stil.Px(this, Stil.RadiusKarte), Stil.Px(this, 4), Height - 2 * Stil.Px(this, Stil.RadiusKarte));
    }
}
