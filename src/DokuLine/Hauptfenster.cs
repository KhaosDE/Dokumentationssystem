using DokuLine.Kern;

namespace DokuLine;

// Hauptfenster: Seitenleiste links mit allen Bereichen (unten Demo-Daten und Version), rechts der
// Kopf mit Logofeld und dauerhaft sichtbarem Demo-Hinweis, darunter der aktive Bereich, ganz unten
// die Statusleiste mit dem Farbschlüssel.
// Mehrplatz: Alle 30 Sekunden prüft das Fenster, ob ein anderer Arbeitsplatz (oder ein zweites
// Fenster) etwas gespeichert hat, und liest dann Zähler und die gezeigte Ansicht neu.
// Noch nicht gebaute Bereiche zeigen „Kommt in Version …“ (Bauplan in IDEEN.md).
public sealed partial class Hauptfenster : Form
{
    private const int LeisteBreite = 232, LeisteSchmal = 60;

    // Reihenfolge der Seitenleiste. „Einstellungen“ steht fest ganz unten (eine Seite wie die übrigen).
    // Eingeschaltete Module (Modulverwaltung) stehen zusätzlich vor „Einstellungen“.
    // „Neuer Bericht …“ gibt es nur mit gewähltem Bewohner (Dokumentation, Akte, Rechtsklick in der Übersicht).
    // Bereich „Therapie“ (Bereichswahl): nur diese Einträge, in dieser Reihenfolge. Sonst fehlen die ersten drei.
    // Vor „Bereiche“: Statische Felder werden in der Reihenfolge des Textes belegt.
    public static readonly string[] TherapieBereiche = [TherapieSeite.LaufendReiter, TherapieSeite.AngeboteReiter, "Wochenpläne"];
    private static readonly string[] TherapieSeitenleiste = [.. TherapieBereiche, "Dokumentation", "Aufgaben", "Nachrichten", "Einstellungen"];

    // Die Einträge des Bereichs „Therapie“ stehen nur dort in der Seitenleiste, in eigener Reihenfolge.
    public static readonly string[] Bereiche =
        ["Übersicht", "Tagesplan", "Dokumentation", "Übergabe", "Medikamentenausgabe", "Visiten", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
         .. TherapieBereiche, "Auswertung", "Qualität", "Einstellungen"];   // die Medikamentendatenbank steht in den Einstellungen

    private readonly Speicher speicher;
    private readonly Panel leiste = new();
    private readonly Label programmName = new();
    public Label LeittextAnzeige { get; } = new();   // unter dem Namen
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
    public WochenplaeneSeite Wochenplaene { get; }
    public KalenderSeite Kalender { get; }
    public UebersichtSeite Uebersicht { get; }
    public TagesplanSeite Tagesplan { get; }
    public VisitenSeite Visiten { get; }
    public AuswertungSeite Auswertung { get; }
    public QualitaetSeite Qualitaet { get; }
    public AusgabeSeite Ausgabe { get; }
    public EinstellungenSeite Einstellungsseite { get; }
    public AnzeigeDienst Anzeige { get; }

    // Damit öffnet das Programm (Program.cs). Tests beginnen bei „Bewohner“ (Konstruktor).
    public const string Startbereich = "Übersicht";
    public List<SeitenleistenEintrag> Navigation { get; } = [];
    public Label VersionText { get; } = new();
    // Einklappen: oben der Menü-Knopf; eingeklappt stehen unten Version/Bereich und „Demo-Daten
    // zurücksetzen“ nur als Symbol mit Hinweis.
    public SeitenleistenEintrag MenueKnopf { get; } = new("Menü einklappen") { Symbol = Symbole.Menue, Eingeklappt = true };
    public SeitenleistenEintrag InfoSymbol { get; } = new("Version") { Symbol = Symbole.Info, Eingeklappt = true, Visible = false, Cursor = Cursors.Default };
    // Abmelden auch unten in der Seitenleiste (eingeklappt als Symbol mit Hinweis), F12 überall.
    public SeitenleistenEintrag AbmeldenEintrag { get; } = new(Abmeldung.KnopfText) { Symbol = Symbole.Abmelden };
    public Sperrflaeche Sperre { get; } = new();
    public bool Gesperrt => Sperre.Visible;
    internal IMessageFilter F12 { get; }
    public bool MenueEingeklappt { get; private set; }
    public Label Arbeitsplatz { get; } = new();   // Bereich und Ort der Datenbank
    public BereichsAuswahl BereichsWahl { get; } = new();   // über „Übersicht“: der Wohnbereich, der überall gilt
    private readonly ToolTip hinweis = new();
    private readonly System.Windows.Forms.Timer abgleich = new() { Interval = 30_000 };
    private long? datenstand;
    public string AktiverBereich { get; private set; } = "";


    public Hauptfenster(Speicher speicher)
    {
        this.speicher = speicher;
        Modulverwaltung.Anmelden(speicher);   // schon beim Start geschehen; sicherheitshalber (legt nur fehlende Tabellen an)
        Text = $"{Programmversion.Name} – {KopfBereich.DemoHinweis}";
        Stil.SymbolFuerAlleFenster();   // schon beim Start geschehen; sicherheitshalber
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
        Nachrichten = new NachrichtenSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Nachrichten"] = Nachrichten;
        Nachrichten.Geaendert += () => { if (AktiverBereich == "Nachrichten") Kopf.TexteSetzen(Nachrichten.KopfText, "Nachrichten"); };
        Bewohner.NachrichtenAnzeigen += bewohnerId =>
        {
            Nachrichten.BewohnerZeigen(bewohnerId);
            BereichWaehlen("Nachrichten", filterBehalten: true);
        };
        Kopf.AbmeldenKnopf.Click += (_, _) => AbmeldenOderAnmelden();
        Kopf.Kuvert.Click += (_, _) => BereichWaehlen("Nachrichten");
        Kopf.EntwurfKnopf.Click += (_, _) => EntwurfFortsetzen();
        speicher.EntwuerfeGeaendert += EntwuerfeZaehlen;
        Kopf.LogoGewuenscht += () => EinstellungenOeffnen("Wohnbereich");
        Therapie = new TherapieSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Therapie"] = Therapie;
        Therapie.Geaendert += () => { if (Schluessel(AktiverBereich) == "Therapie") Kopf.TexteSetzen(Therapie.KopfText, ((IBereichSeite)Therapie).Titel); };
        Therapie.Reiter.Gewechselt += TherapieReiterGewechselt;
        Wochenplaene = new WochenplaeneSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Wochenpläne"] = Wochenplaene;
        Wochenplaene.Geaendert += () => { if (AktiverBereich == "Wochenpläne") Kopf.TexteSetzen(Wochenplaene.KopfText, "Wochenpläne"); };
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
        Ausgabe = new AusgabeSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Medikamentenausgabe"] = Ausgabe;
        Ausgabe.Geaendert += () => { if (AktiverBereich == "Medikamentenausgabe") Kopf.TexteSetzen(Ausgabe.KopfText, "Medikamentenausgabe"); };
        Auswertung = new AuswertungSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Auswertung"] = Auswertung;
        Auswertung.Geaendert += () => { if (AktiverBereich == "Auswertung") Kopf.TexteSetzen(Auswertung.KopfText, "Auswertung"); };
        Qualitaet = new QualitaetSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Qualität"] = Qualitaet;
        Qualitaet.Geaendert += () => { if (AktiverBereich == "Qualität") Kopf.TexteSetzen(Qualitaet.KopfText, "Qualität"); };
        Qualitaet.AkteGewuenscht += id => { BereichWaehlen("Dokumentation"); Bewohner.AkteZeigen(id); };
        Aufgaben.VisitenAufgabeGewuenscht += a => { VisitenAufgabe(a); if (AktiverBereich == "Aufgaben") Aufgaben.Aktualisieren(); };
        Anzeige = new AnzeigeDienst(speicher);
        Einstellungsseite = new EinstellungenSeite(speicher) { Dock = DockStyle.Fill, Visible = false, Anzeige = Anzeige };
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
        Einstellungsseite.AbmeldenGewuenscht += () => Abmelden();   // Abmelden auch unter „Persönlich“
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
        // „Heute wichtig“: ein Klick führt an die Stelle, an die der Eintrag gehört.
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
        Uebersicht.UeberleitungGewuenscht += b => Bewohner.UeberleitungOeffnen(b);
        Uebersicht.AbgaengigkeitGewuenscht += b => { Bewohner.AbgaengigkeitOeffnen(b); Uebersicht.Aktualisieren(); };
        Uebersicht.AuskunftGewuenscht += b => Bewohner.AuskunftOeffnen(b);
        Modul.Ausgangsliste.Aktionen.AbgaengigkeitMelden = id => { if (speicher.Bewohner(id) is Kern.Bewohner b) Bewohner.AbgaengigkeitOeffnen(b); };
        Bewohner.BerichtGewuenscht += id => NeuerBericht(id);
        foreach (var (name, vorhaben) in Bauplan.Bereiche)
            seiten[name] = new PlatzhalterSeite(vorhaben) { Dock = DockStyle.Fill, Visible = false, Titel = name };
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

        BereichsWahl.Gewaehlt += ArbeitsbereichWaehlen;
        leiste.Controls.AddRange([MenueKnopf, programmName, LeittextAnzeige, BereichsWahl]);
        foreach (var eintrag in Navigation) leiste.Controls.Add(eintrag);
        leiste.Controls.AddRange([AbmeldenEintrag, Arbeitsplatz, VersionText, InfoSymbol]);
        leiste.Layout += (_, _) => LeisteAnordnen();
        ModuleAnzeigen();
        speicher.ModuleGeaendert += ModuleAnzeigen;

        // ----- Inhalt rechts: Kopf oben, Bereich darunter -----
        var inhalt = new Panel { Dock = DockStyle.Fill, BackColor = Stil.Seite };
        inhalt.Controls.Add(bereichsFlaeche);
        inhalt.Controls.Add(Kopf.Leiste);   // Aktionsleiste direkt unter der Kopfzeile
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
        ArbeitsbereichPruefen(zuruecksetzen: true);   // vor den Zählern: Sie zählen nur den gewählten Bereich
        AufgabenZaehlen();
        BenutzerAnzeigen();
        ArbeitsplatzAnzeigen();
        MenueSetzen(Einstellungen.Laden().MenueEingeklappt, merken: false);
        Kopf.LogoSetzen(Einstellungen.LogoPfad());
        // Erst „Dokumentation“ (baut die Liste); das Programm wechselt danach zur Übersicht (Startbereich).
        BereichWaehlen("Dokumentation");
        datenstand = Datenstand();
        Anzeige.Anwenden();   // nur wenn auf diesem Rechner eingeschaltet (Vorgabe: aus)
        // Alle 30 Sekunden: Änderungen anderer Rechner; der Tagesplan zusätzlich wegen der Uhrzeit
        // („noch offen“ nach Beginn, „in Therapie“ bis zum Ende).
        abgleich.Tick += (_, _) =>
        {
            bool neu = AenderungenVonAnderen();
            if (!neu && AktiverBereich == "Tagesplan") Tagesplan.Aktualisieren();
            StatuszeileSetzen(neu);
            BetriebPruefen(DateTime.Now);
        };
        abgleich.Start();
    }

    private int S(int wert) => Stil.Px(this, wert);

    // Statusleiste: Demo-Hinweis, Datum, Dienst und wann die gezeigten Daten gelesen wurden.
    private DateTime gelesen = DateTime.Now;

    private void StatuszeileSetzen(bool neuGelesen)
    {
        var jetzt = DateTime.Now;
        if (neuGelesen) gelesen = jetzt;
        Statusleiste.StatusText = HeuteWichtigRegeln.StatusText(jetzt, gelesen) + SicherungWarnung(jetzt);
    }

    // Betrieb: für den Verwalter eine Warnung, wenn die letzte Sicherung älter als 2 Tage ist.
    public string SicherungWarnung(DateTime jetzt) =>
        Betrieb.Aktiv && Recht.Darf(speicher, Funktionen.BetriebVerwalten) && SicherungRegeln.Warnen(speicher.LetzteSicherungZeit, jetzt)
            ? $" · Achtung: letzte Sicherung {(speicher.LetzteSicherungZeit is DateTime l ? $"vom {l:dd.MM.yyyy}" : "fehlt")}" : "";

    // Betrieb (alle 30 Sekunden): nach der eingestellten Zeit ohne Eingabe sperren. Gesichert wird nicht aus den Sitzungen
    // (dort fehlt das Schreibrecht im Sicherungsordner), sondern von der Aufgabenplanung; fehlt die Sicherung, warnt die
    // Statusleiste den Verwalter.
    public void BetriebPruefen(DateTime jetzt)
    {
        if (Betrieb.Werte is not BetriebWerte b) return;
        if (Recht.Angemeldet && !Gesperrt && Leerlauf.Dauer() >= TimeSpan.FromMinutes(b.SperreMinuten)) Abmelden(automatisch: true);
    }

    // „Bewohner“ ist der frühere Name von „Dokumentation“ (Module und Aufrufe von außen). „Laufende Therapien“ und
    // „Angebote“ (Bereich Therapie) sind die beiden Reiter der Seite „Therapie“.
    private static string Schluessel(string eintrag)
    {
        string schluessel = eintrag.Replace(" …", "");
        return schluessel switch
        {
            "Bewohner" => "Dokumentation",
            TherapieSeite.LaufendReiter or TherapieSeite.AngeboteReiter => "Therapie",
            _ => schluessel,
        };
    }

    public Control Seite(string bereich) => seiten[Schluessel(bereich)];

    // Wechselt den Bereich. „Bewohner“ führt immer zur Übersicht der Bewohner.
    // filterBehalten: Nachrichten bleiben auf einen Bewohner eingegrenzt (Aufruf aus der Akte).
    public void BereichWaehlen(string name) => BereichWaehlen(name, filterBehalten: false);

    public void BereichWaehlen(string name, bool filterBehalten)
    {
        var uhr = Messung.Beginnen();
        name = Schluessel(name) == "Dokumentation" ? "Dokumentation" : name;
        string schluessel = Schluessel(name);
        AktiverBereich = name;
        StatuszeileSetzen(neuGelesen: true);
        foreach (var eintrag in Navigation) eintrag.Aktiv = eintrag.Text == name;
        var neu = seiten.GetValueOrDefault(schluessel);
        // Erst alles austauschen, dann einmal anordnen: kein Zwischenbild mit zwei oder keiner Seite.
        var rahmen = neu?.Parent;
        rahmen?.SuspendLayout();
        try
        {
            foreach (var (key, s) in seiten) s.Visible = key == schluessel;
            if (neu is not IBereichSeite seite) return;
            if (schluessel == "Therapie") Therapie.ReiterSetzen(name == "Therapie" ? null : name);
            seite.Zeigen(filterBehalten);
            Kopf.TexteSetzen(seite.KopfText, seite.Titel);
            Kopf.KnoepfeSetzen([.. seite.Knoepfe]);
        }
        finally { rahmen?.ResumeLayout(true); }
        if (uhr is null) return;
        Update();   // bis zur fertigen Anzeige
        Messung.Merken(name, uhr);
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
            Anzeige.Dispose();
            hinweis.Dispose();
            Application.RemoveMessageFilter(F12);
            speicher.EntwuerfeGeaendert -= EntwuerfeZaehlen;
        }
        if (disposing) speicher.ModuleGeaendert -= ModuleAnzeigen;
        base.Dispose(disposing);
    }
}
