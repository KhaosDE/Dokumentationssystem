using DokuLine.Kern;

namespace DokuLine;

// Hauptfenster: Seitenleiste links mit allen Bereichen (unten Demo-Daten und Version), rechts der
// Kopf mit Logofeld und dauerhaft sichtbarem Demo-Hinweis, darunter der aktive Bereich, ganz unten
// die Statusleiste mit dem Farbschlüssel.
// Mehrplatz: Alle 30 Sekunden prüft das Fenster, ob ein anderer Arbeitsplatz (oder ein zweites
// Fenster) etwas gespeichert hat, und liest dann Zähler und die gezeigte Ansicht neu.
// Noch nicht gebaute Bereiche zeigen „Kommt in Version …“ (Bauplan in IDEEN.md).
public sealed class Hauptfenster : Form
{
    private const int LeisteBreite = 232, LeisteSchmal = 60;

    // Reihenfolge der Seitenleiste. „Einstellungen“ steht fest ganz unten (Schritt 29: eine Seite wie die
    // übrigen). Eingeschaltete Module (Modulverwaltung) stehen zusätzlich vor „Einstellungen“.
    // „Neuer Bericht …“ gibt es nur mit gewähltem Bewohner (Dokumentation, Akte, Rechtsklick in der Übersicht).
    public static readonly string[] Bereiche =
        ["Übersicht", "Tagesplan", "Dokumentation", "Übergabe", "Visiten", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
         "Einstellungen"];   // die Medikamentendatenbank steht in den Einstellungen

    private readonly Speicher speicher;
    private readonly Panel leiste = new();
    private readonly Label programmName = new();
    public Label LeittextAnzeige { get; } = new();   // unter dem Namen (Schritt 26a1)
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
    public VisitenSeite Visiten { get; }
    public EinstellungenSeite Einstellungsseite { get; }

    // Damit öffnet das Programm (Program.cs). Tests beginnen bei „Bewohner“ (Konstruktor).
    public const string Startbereich = "Übersicht";
    public List<SeitenleistenEintrag> Navigation { get; } = [];
    public Label VersionText { get; } = new();
    // Einklappen: oben der Menü-Knopf; eingeklappt stehen unten Version/Bereich und „Demo-Daten
    // zurücksetzen“ nur als Symbol mit Hinweis.
    public SeitenleistenEintrag MenueKnopf { get; } = new("Menü einklappen") { Symbol = Symbole.Menue, Eingeklappt = true };
    public SeitenleistenEintrag InfoSymbol { get; } = new("Version") { Symbol = Symbole.Info, Eingeklappt = true, Visible = false, Cursor = Cursors.Default };
    // Schritt 26a3: Abmelden auch unten in der Seitenleiste (eingeklappt als Symbol mit Hinweis), F12 überall.
    public SeitenleistenEintrag AbmeldenEintrag { get; } = new(Abmeldung.KnopfText) { Symbol = Symbole.Abmelden };
    public Sperrflaeche Sperre { get; } = new();
    public bool Gesperrt => Sperre.Visible;
    internal IMessageFilter F12 { get; }
    public bool MenueEingeklappt { get; private set; }
    public Label Arbeitsplatz { get; } = new();   // Bereich und Ort der Datenbank
    private readonly ToolTip hinweis = new();
    private readonly System.Windows.Forms.Timer abgleich = new() { Interval = 30_000 };
    private long? datenstand;
    public string AktiverBereich { get; private set; } = "";


    public Hauptfenster(Speicher speicher)
    {
        this.speicher = speicher;
        Modulverwaltung.Anmelden(speicher);   // schon beim Start geschehen; sicherheitshalber (legt nur fehlende Tabellen an)
        Text = $"{Programmversion.Name} – {KopfBereich.DemoHinweis}";
        Stil.SymbolFuerAlleFenster();   // schon beim Start geschehen; sicherheitshalber (Schritt 26n)
        Icon = Stil.Programmsymbol;
        BackColor = Stil.Seite;
        Font = Stil.Standard;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 680);
        Size = new Size(1180, 800);

        Bewohner = new BewohnerBereich(speicher, Kopf) { Dock = DockStyle.Fill };
        seiten["Dokumentation"] = Bewohner;
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
        Kopf.AbmeldenKnopf.Click += (_, _) => AbmeldenOderAnmelden();
        Kopf.EntwurfKnopf.Click += (_, _) => EntwurfFortsetzen();
        speicher.EntwuerfeGeaendert += EntwuerfeZaehlen;
        Kopf.LogoGewuenscht += () => EinstellungenOeffnen("Wohnbereich");
        Therapie = new TherapieSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Therapie"] = Therapie;
        Therapie.Geaendert += () => { if (AktiverBereich == "Therapie") Kopf.TexteSetzen(Therapie.KopfText, "Therapie"); };
        Kalender = new KalenderSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Kalender"] = Kalender;
        Kalender.Geaendert += () => { if (AktiverBereich == "Kalender") Kopf.TexteSetzen(Kalender.KopfText, "Kalender"); };
        Tagesplan = new TagesplanSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Tagesplan"] = Tagesplan;
        Tagesplan.Geaendert += () => { if (AktiverBereich == "Tagesplan") Kopf.TexteSetzen(Tagesplan.KopfText, "Tagesplan"); };
        Visiten = new VisitenSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Visiten"] = Visiten;
        Visiten.Geaendert += () => { if (AktiverBereich == "Visiten") Kopf.TexteSetzen(Visiten.KopfText, "Visiten"); };
        Visiten.AufgabeGewuenscht += a => VisitenAufgabe(a);
        Aufgaben.VisitenAufgabeGewuenscht += a => { VisitenAufgabe(a); if (AktiverBereich == "Aufgaben") Aufgaben.Aktualisieren(); };
        Einstellungsseite = new EinstellungenSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Einstellungen"] = Einstellungsseite;
        Einstellungsseite.Geaendert += () =>
        {
            if (AktiverBereich != "Einstellungen") return;
            Kopf.TexteSetzen(Einstellungsseite.KopfText, "Einstellungen");
            Kopf.KnoepfeSetzen(Einstellungsseite.Knoepfe);   // „Verzeichnisse“ hat eine Aktionsleiste
        };
        Einstellungsseite.AkteGewuenscht += id => { BereichWaehlen("Dokumentation"); Bewohner.AkteZeigen(id); };
        Einstellungsseite.LogoGeaendert += () => Kopf.LogoSetzen(Einstellungen.LogoPfad());
        Einstellungsseite.BereichGeaendert += () =>
        {
            ArbeitsbereichPruefen(zuruecksetzen: true);
            ArbeitsplatzAnzeigen();
            AufgabenZaehlen();
        };
        Einstellungsseite.SchichtzeitenGeaendert += AufgabenZaehlen;
        Einstellungsseite.AbmeldenGewuenscht += () => Abmelden();   // Schritt 26a3: Abmelden auch unter „Persönlich“
        Einstellungsseite.MenueGewuenscht += eingeklappt => MenueSetzen(eingeklappt, merken: true);
        Einstellungsseite.DemoGewuenscht += () => DemoZuruecksetzen();
        Uebersicht = new UebersichtSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Übersicht"] = Uebersicht;
        Uebersicht.Geaendert += () =>
        {
            if (AktiverBereich != "Übersicht") return;
            Kopf.TexteSetzen(Uebersicht.KopfText, "Übersicht");
            StatuszeileSetzen(neuGelesen: true);
        };
        // „Heute wichtig“ (Schritt 26l): ein Klick führt an die Stelle, an die der Eintrag gehört.
        Uebersicht.StelleGewuenscht += bereich => BereichWaehlen(bereich);
        Uebersicht.VerlaufGewuenscht += v =>
        {
            BereichWaehlen("Nachrichten");
            Nachrichten.VerlaufOeffnen(v);
        };
        Uebersicht.VisitenAufgabeGewuenscht += a => VisitenAufgabe(a);
        Uebersicht.AufgabeNeuGewuenscht += () => { Aufgaben.NeueAufgabe(); AufgabenZaehlen(); Uebersicht.Aktualisieren(); };
        Uebersicht.NachrichtNeuGewuenscht += () => { Nachrichten.NeueNachricht(); NachrichtenZaehlen(); Uebersicht.Aktualisieren(); };
        Uebersicht.BewohnerNeuGewuenscht += () =>
        {
            if (Bewohner.NeuerBewohner() is not Kern.Bewohner neu) return;
            BereichWaehlen("Dokumentation");
            Bewohner.AkteZeigen(neu.Id!.Value);
        };
        Uebersicht.AkteGewuenscht += id =>
        {
            BereichWaehlen("Dokumentation");
            Bewohner.AkteZeigen(id);
        };
        Uebersicht.BerichtGewuenscht += id => NeuerBericht(id);
        Uebersicht.BereichGewechselt += ArbeitsbereichWaehlen;
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

        programmName.Text = Programmversion.Name;
        programmName.Font = Stil.Fett;
        programmName.ForeColor = Stil.TextNormal;
        programmName.AutoSize = true;
        LeittextAnzeige.Text = Programmversion.Leittext;
        LeittextAnzeige.Font = Stil.Klein;
        LeittextAnzeige.ForeColor = Stil.TextLeise;
        LeittextAnzeige.AutoSize = true;

        foreach (string name in Bereiche)
        {
            var eintrag = new SeitenleistenEintrag(name) { Symbol = Symbole.Fuer(name) };
            eintrag.Click += (_, _) => BereichWaehlen(name);
            Navigation.Add(eintrag);
        }
        MenueKnopf.Click += (_, _) => MenueUmschalten();
        AbmeldenEintrag.Click += (_, _) => AbmeldenOderAnmelden();
        Sperre.AnmeldenKnopf.Click += (_, _) => AnmeldenZeigen();
        VersionText.Text = Programmversion.Text;
        VersionText.Font = Stil.Klein;
        VersionText.ForeColor = Stil.TextLeise;
        VersionText.AutoSize = true;
        Arbeitsplatz.Font = Stil.Klein;
        Arbeitsplatz.ForeColor = Stil.TextMittel;
        Arbeitsplatz.AutoSize = true;
        Arbeitsplatz.UseMnemonic = false;

        leiste.Controls.AddRange([MenueKnopf, programmName, LeittextAnzeige]);
        foreach (var eintrag in Navigation) leiste.Controls.Add(eintrag);
        leiste.Controls.AddRange([AbmeldenEintrag, Arbeitsplatz, VersionText, InfoSymbol]);
        leiste.Layout += (_, _) => LeisteAnordnen();
        ModuleAnzeigen();
        NavigationRechte();
        speicher.ModuleGeaendert += ModuleAnzeigen;

        // ----- Inhalt rechts: Kopf oben, Bereich darunter -----
        var inhalt = new Panel { Dock = DockStyle.Fill, BackColor = Stil.Seite };
        inhalt.Controls.Add(bereichsFlaeche);
        inhalt.Controls.Add(Kopf.Leiste);   // Schritt 26i: Aktionsleiste direkt unter der Kopfzeile
        inhalt.Controls.Add(Kopf);
        inhalt.Controls.Add(Statusleiste);
        bereichsFlaeche.BringToFront();   // Fill wird zuletzt angeordnet und nimmt den Rest

        Controls.Add(inhalt);
        Controls.Add(leiste);
        Controls.Add(Sperre);
        F12 = new F12Taste(this);
        Application.AddMessageFilter(F12);
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
        ArbeitsbereichPruefen(zuruecksetzen: true);
        // Erst „Dokumentation“ (baut die Liste); das Programm wechselt danach zur Übersicht (Startbereich).
        BereichWaehlen("Dokumentation");
        datenstand = Datenstand();
        // Alle 30 Sekunden: Änderungen anderer Rechner; der Tagesplan zusätzlich wegen der Uhrzeit
        // („noch offen“ nach Beginn, „in Therapie“ bis zum Ende).
        abgleich.Tick += (_, _) =>
        {
            bool neu = AenderungenVonAnderen();
            if (!neu && AktiverBereich == "Tagesplan") Tagesplan.Aktualisieren();
            StatuszeileSetzen(neu);
        };
        abgleich.Start();
    }

    // ------------------------------------------------------------------ Mehrplatz

    private long? Datenstand()
    {
        try { return speicher.Datenstand(); }
        catch (SpeicherFehler) { return null; }   // Netz kurz weg: beim nächsten Mal wieder versuchen
    }

    // „R1 · Datenbank lokal“ unten in der Seitenleiste; der volle Pfad als Hinweis.
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
            case "Dokumentation": Bewohner.NeuLesen(); break;
            case "Übersicht": Uebersicht.Aktualisieren(); break;
            case "Tagesplan": Tagesplan.Aktualisieren(); break;
            case "Übergabe": Uebergabe.Aktualisieren(); break;
            case "Visiten": Visiten.Aktualisieren(); break;
            case "Aufgaben": Aufgaben.Aktualisieren(); break;
            case "Nachrichten": Nachrichten.Aktualisieren(); break;
            case "Kalender": Kalender.Aktualisieren(); break;
            case "Therapie": Therapie.NeuLesen(); break;
        }
        return true;
    }

    private void StatusNeu()
    {
        if (IsDisposed) return;
        if (AktiverBereich == "Übersicht") Uebersicht.Aktualisieren();
        else if (AktiverBereich == "Dokumentation") Bewohner.NeuLesen();
    }

    private int S(int wert) => Stil.Px(this, wert);

    // Statusleiste (Schritt 26l): Demo-Hinweis, Datum, Dienst und wann die gezeigten Daten gelesen wurden.
    private DateTime gelesen = DateTime.Now;

    private void StatuszeileSetzen(bool neuGelesen)
    {
        var jetzt = DateTime.Now;
        if (neuGelesen) gelesen = jetzt;
        Statusleiste.StatusText = HeuteWichtigRegeln.StatusText(jetzt, gelesen);
    }

    // „Bewohner“ ist der frühere Name von „Dokumentation“ (Module und Aufrufe von außen).
    private static string Schluessel(string eintrag)
    {
        string schluessel = eintrag.Replace(" …", "");
        return schluessel == "Bewohner" ? "Dokumentation" : schluessel;
    }

    public Control Seite(string bereich) => seiten[Schluessel(bereich)];

    // Wechselt den Bereich. „Bewohner“ führt immer zur Übersicht der Bewohner.
    // filterBehalten: Nachrichten bleiben auf einen Bewohner eingegrenzt (Aufruf aus der Akte).
    public void BereichWaehlen(string name) => BereichWaehlen(name, filterBehalten: false);

    public void BereichWaehlen(string name, bool filterBehalten)
    {
        name = Schluessel(name) == "Dokumentation" ? "Dokumentation" : name;
        string schluessel = Schluessel(name);
        AktiverBereich = name;
        StatuszeileSetzen(neuGelesen: true);
        foreach (var eintrag in Navigation) eintrag.Aktiv = eintrag.Text == name;
        foreach (var (key, seite) in seiten) seite.Visible = key == schluessel;
        if (modulSeiten.TryGetValue(schluessel, out var modulSeite))
        {
            modulSeite.Zeigen();
            Kopf.TexteSetzen(modulSeite.KopfText, schluessel);
            Kopf.KnoepfeSetzen([.. modulSeite.Knoepfe]);
            return;
        }
        if (schluessel == "Dokumentation")
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
            Kopf.KnoepfeSetzen(Uebersicht.Knoepfe);
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
        if (schluessel == "Einstellungen")
        {
            Einstellungsseite.Zeigen();
            Kopf.TexteSetzen(Einstellungsseite.KopfText, "Einstellungen");
            Kopf.KnoepfeSetzen(Einstellungsseite.Knoepfe);
            return;
        }
        if (schluessel == "Visiten")
        {
            Visiten.Zeigen();
            Kopf.TexteSetzen(Visiten.KopfText, "Visiten");
            Kopf.KnoepfeSetzen(Visiten.BeginnenKnopf, Visiten.NeuKnopf);
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
        var offen = speicher.Aufgaben(jetzt, bereich: Arbeitsbereich.Gewaehlt);
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

    // „Angemeldet: TST – Name“ und „Abmelden (F12)“ oben rechts; ohne Passwort, nur für die Vorführung.
    public void BenutzerAnzeigen()
    {
        string kuerzel = Einstellungen.Laden().Kuerzel;
        Kopf.BenutzerSetzen(kuerzel.Length > 0 ? kuerzel : null, speicher.MitarbeiterLesen(kuerzel)?.Name);
        AbmeldenEintrag.Text = kuerzel.Length > 0 ? Abmeldung.KnopfText : "Anmelden …";
        AbmeldenEintrag.AccessibleName = AbmeldenEintrag.Text;
        hinweis.SetToolTip(AbmeldenEintrag, AbmeldenEintrag.Text);
        EntwuerfeZaehlen();
        NachrichtenZaehlen();
    }

    public void BenutzerWaehlen(string kuerzel)
    {
        Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        RechteNeu();
    }

    // Anmeldung oder Rechte geändert: Kopf, Zähler, Seitenleiste und die gezeigte Ansicht neu.
    private void RechteNeu()
    {
        if (IsDisposed) return;
        BenutzerAnzeigen();
        AufgabenZaehlen();
        NavigationRechte();
        ArbeitsbereichPruefen(zuruecksetzen: !Recht.Angemeldet);
        if (OhneRechtAusgeblendet.Contains(AktiverBereich)) { BereichWaehlen(Startbereich); return; }
        if (AktiverBereich == "Dokumentation")
        {
            Bewohner.NeuLesen();
            if (Bewohner.AkteSichtbar) Bewohner.Akte.AnsichtNeu();
        }
        else if (AktiverBereich.Length > 0) BereichWaehlen(AktiverBereich, filterBehalten: true);
    }

    // Die Knöpfe der Bereichswahl in der Übersicht, je nach Recht.
    public List<string> BereichKnoepfe() =>
        BereichswahlRegeln.Knoepfe(speicher.Wohnbereiche().Select(w => w.Name), f => Recht.Darf(speicher, f));

    // Den Bereich wählen, der überall gilt (Übersicht); die gezeigte Ansicht und die Zähler folgen sofort.
    public void ArbeitsbereichWaehlen(string bereich)
    {
        Arbeitsbereich.Gewaehlt = bereich;
        KopfBereichSetzen();
        AufgabenZaehlen();
        if (AktiverBereich.Length > 0) BereichWaehlen(AktiverBereich, filterBehalten: true);
    }

    // Vorgabe beim Start und nach dem Abmelden: der Bereich des Rechners; sonst bleibt die Wahl, solange sie
    // erlaubt ist.
    private void ArbeitsbereichPruefen(bool zuruecksetzen)
    {
        var knoepfe = BereichKnoepfe();
        string rechner = Einstellungen.Laden().Bereich;
        bool verboten = knoepfe.Count > 0 && !knoepfe.Contains(Arbeitsbereich.Gewaehlt);
        if (zuruecksetzen || Arbeitsbereich.Gewaehlt.Length == 0 || verboten)
            Arbeitsbereich.Gewaehlt = BereichswahlRegeln.Start(rechner, knoepfe);
        KopfBereichSetzen();
    }

    private void KopfBereichSetzen()
    {
        string bereich = Arbeitsbereich.Gewaehlt;
        Kopf.BereichSetzen(bereich, BereichswahlRegeln.Eingesprungen(bereich, Einstellungen.Laden().Bereich));
    }

    // Bereiche mit eigenem Recht; ohne dieses Recht verschwinden sie aus der Seitenleiste.
    private static readonly (string Bereich, string Funktion)[] BereichsRechte = [("Visiten", Funktionen.Visiten)];

    public HashSet<string> OhneRechtAusgeblendet { get; } = [];

    private void NavigationRechte()
    {
        OhneRechtAusgeblendet.Clear();
        foreach (var (bereich, funktion) in BereichsRechte)
            if (RechteRegeln.FehltRecht(Recht.Grund(speicher, funktion))) OhneRechtAusgeblendet.Add(bereich);
        foreach (var eintrag in Navigation) eintrag.Visible = !OhneRechtAusgeblendet.Contains(eintrag.Text);
        leiste.PerformLayout();
    }

    // ----- Abmelden -----

    private void AbmeldenOderAnmelden()
    {
        if (Recht.Angemeldet) Abmelden();
        else AnmeldenZeigen();
    }

    // Offene Fenster des Programms (Dialoge), die beim Abmelden geschlossen werden.
    // F11 im Tagesplan: nur die Tafel, bildschirmfüllend (Schritt 26g).
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F11 && AktiverBereich == "Tagesplan" && !Gesperrt) { Tagesplan.VollbildZeigen(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    public List<Form> OffeneFenster() =>
        [.. Application.OpenForms.Cast<Form>().Where(f => f != this && f is not Hauptfenster && f is not AnmeldenFenster && f is not AbmeldenFrage && f.Visible)];

    // Abmelden: offene Fenster schließen (vorher fragen, angefangene Eingaben als Entwurf), dann liegt die
    // Sperrfläche über allem und das Anmeldefenster darüber. false = abgebrochen.
    public bool Abmelden()
    {
        if (Gesperrt) return false;
        var offen = OffeneFenster();
        if (offen.Count > 0)
        {
            var mitEingaben = offen.OfType<IEntwurfFenster>().Where(e => e.HatEingaben).ToList();
            using var frage = new AbmeldenFrage(offen.Select(f => f.Text.Replace($" ({Stil.DemoHinweis})", "")).ToList(), mitEingaben.Count > 0);
            var antwort = Dialog.Zeigen(frage, offen[^1]);
            if (antwort != DialogResult.Yes && antwort != DialogResult.No) return false;
            if (antwort == DialogResult.Yes) foreach (var e in mitEingaben) e.AlsEntwurfSpeichern();
            Abmeldung.SchliesstFenster = true;
            try
            {
                foreach (var f in Enumerable.Reverse(offen))
                {
                    f.DialogResult = DialogResult.Cancel;
                    f.Close();
                }
            }
            finally { Abmeldung.SchliesstFenster = false; }
        }
        BenutzerWaehlen("");
        Sperren(true);
        // Waren Dialoge offen, erst deren Schleifen enden lassen, dann das Anmeldefenster zeigen.
        if (offen.Count > 0 && IsHandleCreated) BeginInvoke(AnmeldenZeigen);
        else AnmeldenZeigen();
        return true;
    }

    // Anmelden (nach dem Abmelden oder ohne Anmeldung). Nach dem Abmelden: „Programm beenden“ schließt.
    public void AnmeldenZeigen()
    {
        if (IsDisposed) return;
        bool gesperrt = Gesperrt;
        using var fenster = new AnmeldenFenster(speicher, Einstellungen.Laden().Kuerzel, nachAbmelden: gesperrt);
        if (Dialog.Zeigen(fenster, this) == DialogResult.OK && fenster.Angemeldet is Kern.Mitarbeiter m)
        {
            BenutzerWaehlen(m.Kuerzel);
            if (gesperrt)
            {
                Sperren(false);
                BereichWaehlen(Startbereich);
            }
        }
        else if (gesperrt) Close();
    }

    private void Sperren(bool an)
    {
        // Alles andere ausblenden (nicht nur überdecken): So ist sicher nichts mehr zu sehen.
        foreach (Control c in Controls)
            if (c != Sperre) c.Visible = !an;
        if (an)
        {
            Sperre.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            Sperre.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Sperre.Visible = true;
            Sperre.BringToFront();
        }
        else Sperre.Visible = false;
    }

    private void EntwuerfeZaehlen()
    {
        if (!IsDisposed) Kopf.EntwuerfeSetzen(speicher.Entwuerfe(Recht.Kuerzel).Count);
    }

    // Den ältesten eigenen Entwurf fortsetzen (öffnet „Neuer Bericht“ mit dem Entwurf).
    public Bericht? EntwurfFortsetzen()
    {
        var entwurf = speicher.Entwuerfe(Recht.Kuerzel).FirstOrDefault(e => e.Art == EntwurfRegeln.Bericht);
        return entwurf is null ? null : NeuerBericht(entwurf.BewohnerId);
    }

    // Neuer Bericht für einen Bewohner (aus Akte oder Liste); danach seine Berichte in der Dokumentation.
    public Bericht? NeuerBericht(long bewohnerId)
    {
        if (speicher.Bewohner(bewohnerId) is not Kern.Bewohner b) return null;
        using var formular = new BerichtFormular(speicher, b);
        if (Dialog.Zeigen(formular, this) != DialogResult.OK || formular.Gespeichert is not Bericht bericht) return null;
        BereichWaehlen("Dokumentation");
        Bewohner.DokumentationZeigen(bericht.BewohnerId);
        return bericht;
    }

    // Aufgabe aus einer Visite ausführen (Schritt 26). Facharzttermin: Datum, Uhrzeit, Ort (Kalender,
    // auf Wunsch Fahrt). Medikation: fehlt das Medikament in der Datenbank, zuerst „Neues Medikament“
    // (Name vorbelegt); dann die Akte beim Reiter „Medikamentenplan“ und dort das passende Formular.
    // Ist es gespeichert, ist die Aufgabe erledigt. true = erledigt.
    public bool VisitenAufgabe(Aufgabe a)
    {
        if (speicher.VisitenAufgabe(a) is not { } info) return false;
        if (VisitenAblauf.Sperrgrund(speicher, info.Aktion) is string grund)
        {
            MessageBox.Show(this, grund, "Visiten", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        bool erledigt;
        if (info.Aktion == VisitenAktion.FacharztTermin)
        {
            using var fenster = new FacharztTerminFenster(speicher, a, info.Eintrag);
            erledigt = Dialog.Zeigen(fenster, this) == DialogResult.OK;
        }
        else
        {
            var anordnung = info.Anordnung!;
            var medikament = speicher.VisitenMedikament(anordnung);
            if (medikament is null)
            {
                using var anlegen = new MedikamentFenster(speicher, null);
                anlegen.Wirkstoff.Text = anordnung.Medikament;
                if (Dialog.Zeigen(anlegen, this) != DialogResult.OK || anlegen.Gespeichert is not Medikament neu) return false;
                medikament = neu;
            }
            BereichWaehlen("Dokumentation");
            Bewohner.AkteZeigen(info.Eintrag.BewohnerId);
            Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
            erledigt = Bewohner.Akte.Medikation.AusVisite(info.Visite, anordnung, medikament);
            if (erledigt)
            {
                try { speicher.AufgabeErledigt(a.Id, Recht.Kuerzel, DateTime.Now); }
                catch (Eingabefehler) { erledigt = false; }   // schon erledigt (anderer Arbeitsplatz)
            }
        }
        AufgabenZaehlen();
        if (AktiverBereich == "Visiten") Visiten.Aktualisieren();
        return erledigt;
    }

    // Einstellungen als Seite (Schritt 29), auf Wunsch gleich bei einer Gruppe (z. B. „Wohnbereich“ für das Logo).
    public bool EinstellungenOeffnen(string? gruppe = null)
    {
        BereichWaehlen("Einstellungen");
        if (gruppe is not null) Einstellungsseite.GruppeWaehlen(gruppe);
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
        if (!seiten.ContainsKey(Schluessel(AktiverBereich))) BereichWaehlen("Dokumentation");
        else if (AktiverBereich == "Dokumentation" && !Bewohner.AkteSichtbar) Bewohner.UebersichtZeigen();   // Marken der Module
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
            Application.RemoveMessageFilter(F12);
            speicher.EntwuerfeGeaendert -= EntwuerfeZaehlen;
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
        AbmeldenEintrag.Eingeklappt = eingeklappt;
        programmName.Visible = LeittextAnzeige.Visible = Arbeitsplatz.Visible = VersionText.Visible = !eingeklappt;
        InfoSymbol.Visible = eingeklappt;
        InfoAktualisieren();
        leiste.Width = S(eingeklappt ? LeisteSchmal : LeisteBreite);   // die Inhalte nutzen die Fläche sofort (Dock)
        leiste.PerformLayout();
        leiste.Invalidate();
    }

    // Eingeklappt: Version und Bereich als Hinweis am Info-Symbol.
    private void InfoAktualisieren()
    {
        InfoSymbol.Text = $"{VersionText.Text}\n{Arbeitsplatz.Text}\n{Programmversion.Name} – {Programmversion.Leittext}";
        InfoSymbol.AccessibleName = InfoSymbol.Text.Replace("\n", " · ");
        hinweis.SetToolTip(InfoSymbol, InfoSymbol.Text);
    }

    private void LeisteAnordnen()
    {
        int x = leiste.Padding.Left, breite = leiste.ClientSize.Width - leiste.Padding.Horizontal;
        int y = leiste.Padding.Top;
        MenueKnopf.Bounds = new Rectangle(x, y, MenueEingeklappt ? breite : S(40), S(36));
        // Name und darunter der Leittext, zusammen mittig neben dem Menüknopf.
        int namensblock = programmName.PreferredHeight + LeittextAnzeige.PreferredHeight;
        programmName.Location = new Point(MenueKnopf.Right + S(Stil.AbstandKlein), y + Math.Max(0, (S(36) - namensblock) / 2));
        LeittextAnzeige.Location = new Point(programmName.Left, programmName.Bottom);
        y = MenueKnopf.Bottom + S(Stil.AbstandGross);
        foreach (var eintrag in Navigation.Where(e => e.Text != "Einstellungen" && !OhneRechtAusgeblendet.Contains(e.Text)))
        {
            eintrag.Bounds = new Rectangle(x, y, breite, S(36));
            y += eintrag.Height + S(Stil.AbstandMini);
        }

        // Unten fest: „Einstellungen“ (Schritt 29), darunter Arbeitsplatz und Version bzw. das Info-Symbol.
        int unten = leiste.ClientSize.Height - leiste.Padding.Bottom;
        var einstellungen = Navigation.Single(e => e.Text == "Einstellungen");
        if (MenueEingeklappt)
        {
            InfoSymbol.Bounds = new Rectangle(x, unten - S(36), breite, S(36));
            AbmeldenEintrag.Bounds = new Rectangle(x, InfoSymbol.Top - S(Stil.AbstandMini) - S(36), breite, S(36));
            unten = AbmeldenEintrag.Top - S(Stil.AbstandMini);
        }
        else
        {
            VersionText.Location = new Point(x + S(Stil.AbstandMini), unten - VersionText.PreferredHeight);
            Arbeitsplatz.MaximumSize = new Size(breite - S(Stil.AbstandMini), 0);
            Arbeitsplatz.Location = new Point(x + S(Stil.AbstandMini), VersionText.Top - S(Stil.AbstandKlein) - Arbeitsplatz.PreferredHeight);
            AbmeldenEintrag.Bounds = new Rectangle(x, Arbeitsplatz.Top - S(Stil.AbstandKlein) - S(36), breite, S(36));
            unten = AbmeldenEintrag.Top - S(Stil.AbstandMini);
        }
        einstellungen.Bounds = new Rectangle(x, Math.Max(y + S(Stil.AbstandMittel), unten - S(36)), breite, S(36));
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
        BereichWaehlen("Dokumentation");
        return true;
    }
}
