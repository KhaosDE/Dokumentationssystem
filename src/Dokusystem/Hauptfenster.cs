using Dokusystem.Kern;

namespace Dokusystem;

// Hauptfenster: Seitenleiste links mit allen Bereichen (unten Demo-Daten und Version), rechts der
// Kopf mit Logofeld und dauerhaft sichtbarem Demo-Hinweis, darunter der aktive Bereich, ganz unten
// die Statusleiste mit dem Farbschlüssel.
// Mehrplatz: Alle 30 Sekunden prüft das Fenster, ob ein anderer Arbeitsplatz (oder ein zweites
// Fenster) etwas gespeichert hat, und liest dann Zähler und die gezeigte Ansicht neu.
// Noch nicht gebaute Bereiche zeigen „Kommt in Version …“ (Bauplan in IDEEN.md).
public sealed class Hauptfenster : Form
{
    private const int LeisteBreite = 232, LeisteSchmal = 60;

    // Reihenfolge der Seitenleiste. „Einstellungen“ öffnet ein Fenster statt einer Seite.
    // Eingeschaltete Module (Modulverwaltung) stehen zusätzlich vor „Einstellungen“.
    // „Neuer Bericht …“ gibt es nur mit gewähltem Bewohner (Akte, Reiter „Berichte“, Rechtsklick auf eine Karte).
    public static readonly string[] Bereiche =
        ["Übersicht", "Tagesplan", "Bewohner", "Übergabe", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
         "Einstellungen"];   // die Medikamentendatenbank steht seit Schritt 21 in den Einstellungen

    private readonly Speicher speicher;
    private readonly Panel leiste = new();
    private readonly Label programmName = new();
    private readonly Panel bereichsFlaeche = new() { Dock = DockStyle.Fill, BackColor = Stil.Seite };
    private readonly Dictionary<string, Control> seiten = [];
    private readonly Dictionary<string, ModulSeite> modulSeiten = [];

    public KopfBereich Kopf { get; } = new() { Dock = DockStyle.Top };
    public Statusleiste Statusleiste { get; } = new() { Dock = DockStyle.Bottom };
    public BewohnerBereich Bewohner { get; }
    public UebergabeSeite Uebergabe { get; }
    public AufgabenSeite Aufgaben { get; }
    public NachrichtenSeite Nachrichten { get; }
    public TherapieSeite Therapie { get; }
    public KalenderSeite Kalender { get; }
    public UebersichtSeite Uebersicht { get; }
    public TagesplanSeite Tagesplan { get; }

    // Damit öffnet das Programm (Program.cs). Tests beginnen bei „Bewohner“ (Konstruktor).
    public const string Startbereich = "Übersicht";
    public List<SeitenleistenEintrag> Navigation { get; } = [];
    public Label VersionText { get; } = new();
    // Einklappen: oben der Menü-Knopf; eingeklappt stehen unten Version/Bereich und „Demo-Daten
    // zurücksetzen“ nur als Symbol mit Hinweis.
    public SeitenleistenEintrag MenueKnopf { get; } = new("Menü einklappen") { Symbol = Symbole.Menue, Eingeklappt = true };
    public SeitenleistenEintrag InfoSymbol { get; } = new("Version") { Symbol = Symbole.Info, Eingeklappt = true, Visible = false, Cursor = Cursors.Default };
    public SeitenleistenEintrag DemoSymbol { get; } = new("Demo-Daten zurücksetzen …") { Symbol = Symbole.Zuruecksetzen, Eingeklappt = true, Visible = false };
    public bool MenueEingeklappt { get; private set; }
    public Label Arbeitsplatz { get; } = new();   // Bereich und Ort der Datenbank
    private readonly ToolTip hinweis = new();
    private readonly System.Windows.Forms.Timer abgleich = new() { Interval = 30_000 };
    private long? datenstand;
    public Button DemoKnopf { get; } = Stil.NebenKnopf("Demo-Daten zurücksetzen …");
    public string AktiverBereich { get; private set; } = "";

    // Zum Testen ersetzbar: zeigt das Einstellungsfenster; true = gespeichert.
    internal Func<EinstellungenFenster, bool> EinstellungenZeigen { get; set; }

    public Hauptfenster(Speicher speicher)
    {
        this.speicher = speicher;
        Modulverwaltung.Anmelden(speicher);   // schon beim Start geschehen; sicherheitshalber (legt nur fehlende Tabellen an)
        Text = $"Dokumentationssystem – {KopfBereich.DemoHinweis}";
        BackColor = Stil.Seite;
        Font = Stil.Standard;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 680);
        Size = new Size(1180, 800);
        EinstellungenZeigen = f => f.ShowDialog(this) == DialogResult.OK;

        Bewohner = new BewohnerBereich(speicher, Kopf) { Dock = DockStyle.Fill };
        seiten["Bewohner"] = Bewohner;
        Uebergabe = new UebergabeSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Übergabe"] = Uebergabe;
        Uebergabe.Geaendert += () => { if (AktiverBereich == "Übergabe") Kopf.TexteSetzen(Uebergabe.KopfText, "Übergabe"); };
        Aufgaben = new AufgabenSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Aufgaben"] = Aufgaben;
        Aufgaben.Geaendert += () => { if (AktiverBereich == "Aufgaben") Kopf.TexteSetzen(Aufgaben.KopfText, "Aufgaben"); };
        Bewohner.AufgabenAnzeigen += () => BereichWaehlen("Aufgaben");
        Nachrichten = new NachrichtenSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Nachrichten"] = Nachrichten;
        Nachrichten.Geaendert += () => { if (AktiverBereich == "Nachrichten") Kopf.TexteSetzen(Nachrichten.KopfText, "Nachrichten"); };
        Bewohner.NachrichtenAnzeigen += bewohnerId =>
        {
            Nachrichten.BewohnerZeigen(bewohnerId);
            BereichWaehlen("Nachrichten", filterBehalten: true);
        };
        Kopf.BenutzerKnopf.Click += (_, _) => BenutzerWechseln();
        Kopf.LogoGewuenscht += () => EinstellungenOeffnen();
        Therapie = new TherapieSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Therapie"] = Therapie;
        Therapie.Geaendert += () => { if (AktiverBereich == "Therapie") Kopf.TexteSetzen(Therapie.KopfText, "Therapie"); };
        Kalender = new KalenderSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Kalender"] = Kalender;
        Kalender.Geaendert += () => { if (AktiverBereich == "Kalender") Kopf.TexteSetzen(Kalender.KopfText, "Kalender"); };
        Tagesplan = new TagesplanSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Tagesplan"] = Tagesplan;
        Tagesplan.Geaendert += () => { if (AktiverBereich == "Tagesplan") Kopf.TexteSetzen(Tagesplan.KopfText, "Tagesplan"); };
        Uebersicht = new UebersichtSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Übersicht"] = Uebersicht;
        Uebersicht.Geaendert += () => { if (AktiverBereich == "Übersicht") Kopf.TexteSetzen(Uebersicht.KopfText, "Übersicht"); };
        Uebersicht.AkteGewuenscht += id =>
        {
            BereichWaehlen("Bewohner");
            Bewohner.AkteZeigen(id);
        };
        Uebersicht.BerichtGewuenscht += id => NeuerBericht(id);
        Bewohner.BerichtGewuenscht += id => NeuerBericht(id);
        foreach (var (name, vorhaben) in Bauplan.Bereiche)
            seiten[name] = new PlatzhalterSeite(vorhaben) { Dock = DockStyle.Fill, Visible = false };
        foreach (var seite in seiten.Values) bereichsFlaeche.Controls.Add(seite);

        // ----- Seitenleiste -----
        leiste.Dock = DockStyle.Left;
        leiste.Width = S(LeisteBreite);
        leiste.BackColor = Stil.Fensterhintergrund;
        leiste.Padding = new Padding(Stil.AbstandMittel);
        leiste.Paint += (_, e) =>
        {
            using var linie = new Pen(Stil.Trennlinie);
            e.Graphics.DrawLine(linie, leiste.Width - 1, 0, leiste.Width - 1, leiste.Height);
        };

        programmName.Text = "Dokumentationssystem";
        programmName.Font = Stil.Fett;
        programmName.ForeColor = Stil.TextNormal;
        programmName.AutoSize = true;

        foreach (string name in Bereiche)
        {
            var eintrag = new SeitenleistenEintrag(name) { Symbol = Symbole.Fuer(name) };
            eintrag.Click += (_, _) => BereichWaehlen(name);
            Navigation.Add(eintrag);
        }
        MenueKnopf.Click += (_, _) => MenueUmschalten();
        DemoSymbol.Click += (_, _) =>
        {
            if (Recht.Grund(speicher, Funktionen.DemoZuruecksetzen) is string grund) hinweis.Show(grund, DemoSymbol, DemoSymbol.Width, 0, 3000);
            else DemoZuruecksetzen();
        };

        DemoKnopf.AutoSize = false;
        DemoKnopf.Padding = new Padding(Stil.AbstandMini, 0, Stil.AbstandMini, 0);   // passt so in eine Zeile
        DemoKnopf.Click += (_, _) => DemoZuruecksetzen();
        VersionText.Text = Programmversion.Text;
        VersionText.Font = Stil.Klein;
        VersionText.ForeColor = Stil.TextLeise;
        VersionText.AutoSize = true;
        Arbeitsplatz.Font = Stil.Klein;
        Arbeitsplatz.ForeColor = Stil.TextMittel;
        Arbeitsplatz.AutoSize = true;
        Arbeitsplatz.UseMnemonic = false;

        leiste.Controls.AddRange([MenueKnopf, programmName]);
        foreach (var eintrag in Navigation) leiste.Controls.Add(eintrag);
        leiste.Controls.AddRange([Arbeitsplatz, DemoKnopf, VersionText, InfoSymbol, DemoSymbol]);
        leiste.Layout += (_, _) => LeisteAnordnen();
        ModuleAnzeigen();
        speicher.ModuleGeaendert += ModuleAnzeigen;

        // ----- Inhalt rechts: Kopf oben, Bereich darunter -----
        var inhalt = new Panel { Dock = DockStyle.Fill, BackColor = Stil.Seite };
        inhalt.Controls.Add(bereichsFlaeche);
        inhalt.Controls.Add(Kopf);
        inhalt.Controls.Add(Statusleiste);
        bereichsFlaeche.BringToFront();   // Fill wird zuletzt angeordnet und nimmt den Rest

        Controls.Add(inhalt);
        Controls.Add(leiste);
        inhalt.BringToFront();

        speicher.AufgabenGeaendert += AufgabenZaehlen;
        speicher.NachrichtenGeaendert += NachrichtenZaehlen;
        // Startseite immer aktuell: nach Änderungen neu aufbauen, wenn sie gerade zu sehen ist.
        speicher.MedikationGeaendert += UebersichtNeu;
        speicher.TherapieGeaendert += UebersichtNeu;
        speicher.WochenplanGeaendert += UebersichtNeu;
        speicher.KalenderGeaendert += UebersichtNeu;
        speicher.StatusGeaendert += StatusNeu;
        speicher.MitarbeiterGeaendert += RechteNeu;
        speicher.BezugGeaendert += StatusNeu;
        AufgabenZaehlen();
        BenutzerAnzeigen();
        ArbeitsplatzAnzeigen();
        MenueSetzen(Einstellungen.Laden().MenueEingeklappt, merken: false);
        Kopf.LogoSetzen(Einstellungen.LogoPfad());
        // Erst „Bewohner“ (baut die Karten); das Programm wechselt danach zur Übersicht (Startbereich).
        BereichWaehlen("Bewohner");
        datenstand = Datenstand();
        // Alle 30 Sekunden: Änderungen anderer Rechner; der Tagesplan zusätzlich wegen der Uhrzeit
        // („noch offen“ nach Beginn, „in Therapie“ bis zum Ende).
        abgleich.Tick += (_, _) => { if (!AenderungenVonAnderen() && AktiverBereich == "Tagesplan") Tagesplan.Aktualisieren(); };
        abgleich.Start();
    }

    // ------------------------------------------------------------------ Mehrplatz

    private long? Datenstand()
    {
        try { return speicher.Datenstand(); }
        catch (SpeicherFehler) { return null; }   // Netz kurz weg: beim nächsten Mal wieder versuchen
    }

    // „Wohnbereich A · Datenbank lokal“ unten in der Seitenleiste; der volle Pfad als Hinweis.
    public void ArbeitsplatzAnzeigen(bool erreichbar = true)
    {
        var werte = Einstellungen.Laden();
        bool gemeinsam = !string.Equals(Path.GetDirectoryName(speicher.Pfad), Pfade.Datenordner, StringComparison.OrdinalIgnoreCase);
        string ort = gemeinsam ? "gemeinsamer Ordner" : "Datenbank lokal";
        bool neustart = !string.Equals(Path.GetFullPath(Einstellungen.DatenbankPfad(werte)), Path.GetFullPath(speicher.Pfad), StringComparison.OrdinalIgnoreCase);
        Arbeitsplatz.Text = $"{werte.Bereich} · {ort}" + (erreichbar ? "" : " – nicht erreichbar") + (neustart ? "\nneuer Ort nach Neustart" : "");
        Arbeitsplatz.ForeColor = erreichbar ? Stil.TextMittel : Stil.Beschluss.Text;
        hinweis.SetToolTip(Arbeitsplatz, $"Bereich dieses Arbeitsplatzes: {werte.Bereich}\nDatenbank: {speicher.Pfad}");
        InfoAktualisieren();
        leiste.PerformLayout();
    }

    // Hat ein anderer Arbeitsplatz etwas gespeichert? Dann Zähler und die gezeigte Ansicht neu lesen.
    // Fenster zum Bearbeiten bleiben unberührt; ihr Speichern prüft selbst (Konflikt-Hinweis).
    public bool AenderungenVonAnderen()
    {
        long? stand = Datenstand();
        ArbeitsplatzAnzeigen(stand is not null);
        if (stand is null || stand == datenstand || IsDisposed) return false;
        datenstand = stand;
        AufgabenZaehlen();
        NachrichtenZaehlen();
        string schluessel = Schluessel(AktiverBereich);
        if (modulSeiten.TryGetValue(schluessel, out var modulSeite))
        {
            modulSeite.Zeigen();
            Kopf.TexteSetzen(modulSeite.KopfText, schluessel);
            return true;
        }
        switch (schluessel)
        {
            case "Bewohner": Bewohner.NeuLesen(); break;
            case "Übersicht": Uebersicht.Aktualisieren(); break;
            case "Tagesplan": Tagesplan.Aktualisieren(); break;
            case "Übergabe": Uebergabe.Aktualisieren(); break;
            case "Aufgaben": Aufgaben.Aktualisieren(); break;
            case "Nachrichten": Nachrichten.Aktualisieren(); break;
            case "Kalender": Kalender.Aktualisieren(); break;
            case "Therapie": Therapie.Aktualisieren(); break;
        }
        return true;
    }

    private void StatusNeu()
    {
        if (IsDisposed) return;
        if (AktiverBereich == "Übersicht") Uebersicht.Aktualisieren();
        else if (AktiverBereich == "Bewohner") Bewohner.NeuLesen();
    }

    private int S(int wert) => Stil.Px(this, wert);

    private static string Schluessel(string eintrag) => eintrag.Replace(" …", "");

    public Control Seite(string bereich) => seiten[Schluessel(bereich)];

    // Wechselt den Bereich. „Bewohner“ führt immer zur Übersicht der Bewohner.
    // filterBehalten: Nachrichten bleiben auf einen Bewohner eingegrenzt (Aufruf aus der Akte).
    public void BereichWaehlen(string name) => BereichWaehlen(name, filterBehalten: false);

    public void BereichWaehlen(string name, bool filterBehalten)
    {
        if (name == "Einstellungen")
        {
            EinstellungenOeffnen();
            return;
        }
        string schluessel = Schluessel(name);
        AktiverBereich = name;
        foreach (var eintrag in Navigation) eintrag.Aktiv = eintrag.Text == name;
        foreach (var (key, seite) in seiten) seite.Visible = key == schluessel;
        if (modulSeiten.TryGetValue(schluessel, out var modulSeite))
        {
            modulSeite.Zeigen();
            Kopf.TexteSetzen(modulSeite.KopfText, schluessel);
            Kopf.KnoepfeSetzen([.. modulSeite.Knoepfe]);
            return;
        }
        if (schluessel == "Bewohner")
        {
            Bewohner.UebersichtZeigen();
            return;
        }
        if (schluessel == "Nachrichten")
        {
            if (filterBehalten) Nachrichten.Aktualisieren();
            else Nachrichten.BewohnerZeigen(null);
            Kopf.TexteSetzen(Nachrichten.KopfText, "Nachrichten");
            Kopf.KnoepfeSetzen(Nachrichten.NeuKnopf);
            return;
        }
        if (schluessel == "Übersicht")
        {
            Uebersicht.Aktualisieren();
            Kopf.TexteSetzen(Uebersicht.KopfText, "Übersicht");
            Kopf.KnoepfeSetzen();
            return;
        }
        if (schluessel == "Tagesplan")
        {
            Tagesplan.Aktualisieren();
            Kopf.TexteSetzen(Tagesplan.KopfText, "Tagesplan");
            Kopf.KnoepfeSetzen();
            return;
        }
        if (schluessel == "Kalender")
        {
            Kalender.Zeigen();
            Kopf.TexteSetzen(Kalender.KopfText, "Kalender");
            Kopf.KnoepfeSetzen(Kalender.NeuKnopf);
            return;
        }
        if (schluessel == "Therapie")
        {
            Therapie.Zeigen();
            Kopf.TexteSetzen(Therapie.KopfText, "Therapie");
            Kopf.KnoepfeSetzen(Therapie.NeuKnopf);
            return;
        }
        if (schluessel == "Aufgaben")
        {
            Aufgaben.Aktualisieren();
            Kopf.TexteSetzen(Aufgaben.KopfText, "Aufgaben");
            Kopf.KnoepfeSetzen(Aufgaben.NeuKnopf);
            return;
        }
        if (schluessel == "Übergabe")
        {
            Uebergabe.Besuch();
            Kopf.TexteSetzen(Uebergabe.KopfText, "Übergabe");
            Kopf.KnoepfeSetzen();
            return;
        }
        Kopf.TexteSetzen($"Kommt in Version {Bauplan.Bereiche[schluessel].Version}", schluessel);
        Kopf.KnoepfeSetzen();
    }

    // Zähler offener Aufgaben in der Seitenleiste (rot, wenn etwas überfällig ist) und die
    // Kurzliste über den Bewohnerkarten.
    public void AufgabenZaehlen()
    {
        var jetzt = DateTime.Now;
        speicher.StandardaufgabenErzeugen(jetzt);   // Schritt 25: zu Schichtbeginn die Standardaufgaben
        var offen = speicher.Aufgaben(jetzt);
        int ueberfaellig = offen.Count(a => AufgabenRegeln.Ueberfaellig(a, jetzt));
        Navigation.Single(n => n.Text == "Aufgaben").ZaehlerSetzen(offen.Count, ueberfaellig > 0,
            $"{offen.Count} offen" + (ueberfaellig > 0 ? $", {ueberfaellig} überfällig" : ""));
        Bewohner.AufgabenSetzen(offen, jetzt);
    }

    // Zähler ungelesener Nachrichten des angemeldeten Benutzers (Seitenleiste und Hinweis über den Karten).
    public void NachrichtenZaehlen()
    {
        var ich = speicher.Kollege(Einstellungen.Laden().Kuerzel);
        int n = ich is null ? 0 : speicher.UngeleseneNachrichten(ich.Kuerzel);
        Navigation.Single(e => e.Text == "Nachrichten").ZaehlerSetzen(n > 0 ? n : null, false, $"{n} ungelesen");
        Bewohner.NachrichtenSetzen(n, ich?.Name);
    }

    // „Angemeldet als …“ oben rechts; ohne Passwort, nur für die Vorführung.
    public void BenutzerAnzeigen()
    {
        string kuerzel = Einstellungen.Laden().Kuerzel;
        Kopf.BenutzerSetzen(speicher.Kollege(kuerzel)?.Anzeige ?? (kuerzel.Length > 0 ? kuerzel : null));
        NachrichtenZaehlen();
        Recht.Anwenden(speicher, Funktionen.DemoZuruecksetzen, DemoKnopf);   // Rechte (Schritt 19)
    }

    public void BenutzerWaehlen(string kuerzel)
    {
        Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        RechteNeu();
    }

    // Anmeldung oder Rechte geändert: Kopf, Zähler und die gezeigte Ansicht (gesperrte Knöpfe) neu.
    private void RechteNeu()
    {
        if (IsDisposed) return;
        BenutzerAnzeigen();
        AufgabenZaehlen();
        if (AktiverBereich == "Bewohner")
        {
            Bewohner.NeuLesen();
            if (Bewohner.AkteSichtbar) Bewohner.Akte.AnsichtNeu();
        }
        else if (AktiverBereich.Length > 0) BereichWaehlen(AktiverBereich, filterBehalten: true);
    }

    // „Abmelden / Benutzer wechseln“ oben rechts: Anmelden mit Kürzel (Schritt 19).
    public void BenutzerWechseln()
    {
        using var fenster = new AnmeldenFenster(speicher, Einstellungen.Laden().Kuerzel, wechsel: true);
        var ergebnis = Dialog.Zeigen(fenster, this);
        if (ergebnis == DialogResult.OK && fenster.Angemeldet is Kern.Mitarbeiter m) BenutzerWaehlen(m.Kuerzel);
        else if (ergebnis == DialogResult.Abort) BenutzerWaehlen("");
    }

    // Neuer Bericht für einen Bewohner (aus Akte oder Karte); danach seine Akte beim Reiter „Berichte“.
    public Bericht? NeuerBericht(long bewohnerId)
    {
        if (speicher.Bewohner(bewohnerId) is not Kern.Bewohner b) return null;
        using var formular = new BerichtFormular(speicher, b);
        if (Dialog.Zeigen(formular, this) != DialogResult.OK || formular.Gespeichert is not Bericht bericht) return null;
        BereichWaehlen("Bewohner");
        Bewohner.AkteZeigen(bericht.BewohnerId);
        Bewohner.Akte.Reiter.Waehlen("Berichte");
        return bericht;
    }

    public bool EinstellungenOeffnen()
    {
        using var fenster = new EinstellungenFenster(speicher.Kollegen(), speicher);
        if (!EinstellungenZeigen(fenster)) return false;
        Kopf.LogoSetzen(Einstellungen.LogoPfad());
        BenutzerAnzeigen();
        AufgabenZaehlen();
        ArbeitsplatzAnzeigen();
        if (AktiverBereich == "Übersicht") Uebersicht.Aktualisieren();   // Bereich kann sich geändert haben
        else if (AktiverBereich == "Bewohner" && !Bewohner.AkteSichtbar) Bewohner.UebersichtZeigen();
        return true;
    }

    // Seitenleiste und Seiten der Module: Eingeschaltete Module bekommen einen Eintrag vor
    // „Einstellungen“ und einen Reiter in der Akte; ausgeschaltete verschwinden ganz (die Daten bleiben).
    public void ModuleAnzeigen()
    {
        foreach (var (name, seite) in modulSeiten)
        {
            seiten.Remove(name);
            bereichsFlaeche.Controls.Remove(seite);
            seite.Dispose();
        }
        foreach (var eintrag in Navigation.Where(e => modulSeiten.ContainsKey(e.Text)).ToList())
        {
            Navigation.Remove(eintrag);
            leiste.Controls.Remove(eintrag);
            eintrag.Dispose();
        }
        modulSeiten.Clear();
        int vor = Navigation.FindIndex(e => e.Text == "Einstellungen");
        foreach (var m in Modulverwaltung.Eingeschaltet(speicher))
        {
            string name = m.Name;
            var seite = m.SeiteBauen(speicher);
            seite.Dock = DockStyle.Fill;
            seite.Visible = false;
            seite.Geaendert += () => { if (AktiverBereich == name) Kopf.TexteSetzen(seite.KopfText, name); };
            seiten[name] = seite;
            modulSeiten[name] = seite;
            bereichsFlaeche.Controls.Add(seite);
            var eintrag = new SeitenleistenEintrag(name) { Symbol = Symbole.Fuer(name), Eingeklappt = MenueEingeklappt };
            eintrag.Click += (_, _) => BereichWaehlen(name);
            hinweis.SetToolTip(eintrag, MenueEingeklappt ? name : null);
            Navigation.Insert(vor++, eintrag);
            leiste.Controls.Add(eintrag);
        }
        Bewohner.Akte.ModuleAktualisieren();
        leiste.PerformLayout();
        if (AktiverBereich.Length == 0) return;
        if (!seiten.ContainsKey(Schluessel(AktiverBereich))) BereichWaehlen("Bewohner");
        else if (AktiverBereich == "Bewohner" && !Bewohner.AkteSichtbar) Bewohner.UebersichtZeigen();   // Marken der Module
        else if (modulSeiten.ContainsKey(AktiverBereich) || AktiverBereich == "Übersicht") BereichWaehlen(AktiverBereich);
    }

    private void UebersichtNeu()
    {
        if (AktiverBereich == "Übersicht" && !IsDisposed) Uebersicht.Aktualisieren();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            speicher.MedikationGeaendert -= UebersichtNeu;
            speicher.TherapieGeaendert -= UebersichtNeu;
            speicher.WochenplanGeaendert -= UebersichtNeu;
            speicher.KalenderGeaendert -= UebersichtNeu;
            speicher.StatusGeaendert -= StatusNeu;
            speicher.MitarbeiterGeaendert -= RechteNeu;
            speicher.BezugGeaendert -= StatusNeu;
            abgleich.Dispose();
            hinweis.Dispose();
        }
        if (disposing) speicher.ModuleGeaendert -= ModuleAnzeigen;
        base.Dispose(disposing);
    }

    // Seitenmenü ein- oder ausklappen; der Zustand bleibt in den Einstellungen.
    public void MenueUmschalten() => MenueSetzen(!MenueEingeklappt, merken: true);

    private void MenueSetzen(bool eingeklappt, bool merken)
    {
        MenueEingeklappt = eingeklappt;
        if (merken) Einstellungen.Aendern(w => w with { MenueEingeklappt = eingeklappt });
        MenueKnopf.Text = eingeklappt ? "Menü ausklappen" : "Menü einklappen";
        MenueKnopf.AccessibleName = MenueKnopf.Text;
        hinweis.SetToolTip(MenueKnopf, MenueKnopf.Text);
        foreach (var eintrag in Navigation)
        {
            eintrag.Eingeklappt = eingeklappt;
            hinweis.SetToolTip(eintrag, eingeklappt ? eintrag.Text : null);
        }
        programmName.Visible = Arbeitsplatz.Visible = DemoKnopf.Visible = VersionText.Visible = !eingeklappt;
        InfoSymbol.Visible = DemoSymbol.Visible = eingeklappt;
        InfoAktualisieren();
        hinweis.SetToolTip(DemoSymbol, DemoSymbol.Text);
        leiste.Width = S(eingeklappt ? LeisteSchmal : LeisteBreite);   // die Inhalte nutzen die Fläche sofort (Dock)
        leiste.PerformLayout();
        leiste.Invalidate();
    }

    // Eingeklappt: Version und Bereich als Hinweis am Info-Symbol.
    private void InfoAktualisieren()
    {
        InfoSymbol.Text = $"{VersionText.Text}\n{Arbeitsplatz.Text}";
        InfoSymbol.AccessibleName = InfoSymbol.Text.Replace("\n", " · ");
        hinweis.SetToolTip(InfoSymbol, InfoSymbol.Text);
    }

    private void LeisteAnordnen()
    {
        int x = leiste.Padding.Left, breite = leiste.ClientSize.Width - leiste.Padding.Horizontal;
        int y = leiste.Padding.Top;
        MenueKnopf.Bounds = new Rectangle(x, y, MenueEingeklappt ? breite : S(40), S(36));
        programmName.Location = new Point(MenueKnopf.Right + S(Stil.AbstandKlein), y + (S(36) - programmName.PreferredHeight) / 2);
        y = MenueKnopf.Bottom + S(Stil.AbstandGross);
        foreach (var eintrag in Navigation)
        {
            // Einstellungen etwas abgesetzt: Sie sind kein Arbeitsbereich.
            if (eintrag.Text == "Einstellungen") y += S(Stil.AbstandMittel);
            eintrag.Bounds = new Rectangle(x, y, breite, S(36));
            y += eintrag.Height + S(Stil.AbstandMini);
        }

        int unten = leiste.ClientSize.Height - leiste.Padding.Bottom;
        if (MenueEingeklappt)
        {
            InfoSymbol.Bounds = new Rectangle(x, unten - S(36), breite, S(36));
            DemoSymbol.Bounds = new Rectangle(x, InfoSymbol.Top - S(Stil.AbstandMini) - S(36), breite, S(36));
            return;
        }
        VersionText.Location = new Point(x + S(Stil.AbstandMini), unten - VersionText.PreferredHeight);
        unten = VersionText.Top - S(Stil.AbstandMittel);
        DemoKnopf.Bounds = new Rectangle(x, unten - S(Stil.KnopfHoehe), breite, S(Stil.KnopfHoehe));
        Arbeitsplatz.MaximumSize = new Size(breite - S(Stil.AbstandMini), 0);
        Arbeitsplatz.Location = new Point(x + S(Stil.AbstandMini), DemoKnopf.Top - S(Stil.AbstandKlein) - Arbeitsplatz.PreferredHeight);
    }

    // Alle Bewohner und Bilder weg, Demo-Daten neu laden – mit Rückfrage.
    public bool DemoZuruecksetzen(bool nachfragen = true)
    {
        bool gemeinsam = !string.Equals(Path.GetDirectoryName(speicher.Pfad), Pfade.Datenordner, StringComparison.OrdinalIgnoreCase);
        if (nachfragen && MessageBox.Show(this,
                "Alle Änderungen und hinterlegten Bilder werden gelöscht und die ausgedachten Demo-Daten neu geladen." +
                (gemeinsam ? "\n\nDie Datenbank liegt im gemeinsamen Ordner: Das gilt für alle Arbeitsplätze." : "") + "\n\nFortfahren?",
                "Demo-Daten zurücksetzen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            != DialogResult.Yes)
            return false;
        speicher.DemoZuruecksetzen();
        BereichWaehlen("Bewohner");
        return true;
    }
}
