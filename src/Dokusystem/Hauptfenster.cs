using Dokusystem.Kern;

namespace Dokusystem;

// Hauptfenster: Seitenleiste links mit allen Bereichen (unten Demo-Daten und Version), rechts der
// Kopf mit Logofeld und dauerhaft sichtbarem Demo-Hinweis, darunter der aktive Bereich, ganz unten
// die Statusleiste mit dem Farbschlüssel.
// Noch nicht gebaute Bereiche zeigen „Kommt in Version …“ (Bauplan in IDEEN.md).
public sealed class Hauptfenster : Form
{
    private const int LeisteBreite = 232;

    // Reihenfolge der Seitenleiste. „Neuer Bericht …“ und „Einstellungen“ öffnen ein Fenster statt einer Seite.
    // Eingeschaltete Module (Modulverwaltung) stehen zusätzlich vor „Einstellungen“.
    public static readonly string[] Bereiche =
        ["Übersicht", "Bewohner", "Neuer Bericht …", "Übergabe", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
         "Medikamente", "Einstellungen"];

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
    public MedikamenteSeite Medikamente { get; }
    public TherapieSeite Therapie { get; }
    public KalenderSeite Kalender { get; }
    public UebersichtSeite Uebersicht { get; }

    // Damit öffnet das Programm (Program.cs). Tests beginnen bei „Bewohner“ (Konstruktor).
    public const string Startbereich = "Übersicht";
    public List<SeitenleistenEintrag> Navigation { get; } = [];
    public Label VersionText { get; } = new();
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
        Kopf.BenutzerKnopf.Click += (_, _) => BenutzerMenueZeigen();
        Medikamente = new MedikamenteSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Medikamente"] = Medikamente;
        Medikamente.Geaendert += () => { if (AktiverBereich == "Medikamente") Kopf.TexteSetzen(Medikamente.KopfText, "Medikamente"); };
        Therapie = new TherapieSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Therapie"] = Therapie;
        Therapie.Geaendert += () => { if (AktiverBereich == "Therapie") Kopf.TexteSetzen(Therapie.KopfText, "Therapie"); };
        Kalender = new KalenderSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Kalender"] = Kalender;
        Kalender.Geaendert += () => { if (AktiverBereich == "Kalender") Kopf.TexteSetzen(Kalender.KopfText, "Kalender"); };
        Uebersicht = new UebersichtSeite(speicher) { Dock = DockStyle.Fill, Visible = false };
        seiten["Übersicht"] = Uebersicht;
        Uebersicht.Geaendert += () => { if (AktiverBereich == "Übersicht") Kopf.TexteSetzen(Uebersicht.KopfText, "Übersicht"); };
        Uebersicht.AkteGewuenscht += id =>
        {
            BereichWaehlen("Bewohner");
            Bewohner.AkteZeigen(id);
        };
        foreach (var (name, vorhaben) in Bauplan.Bereiche)
            seiten[name] = new PlatzhalterSeite(vorhaben) { Dock = DockStyle.Fill, Visible = false };
        foreach (var seite in seiten.Values) bereichsFlaeche.Controls.Add(seite);

        // ----- Seitenleiste -----
        leiste.Dock = DockStyle.Left;
        leiste.Width = LeisteBreite;
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
            var eintrag = new SeitenleistenEintrag(name);
            eintrag.Click += (_, _) => BereichWaehlen(name);
            Navigation.Add(eintrag);
        }

        DemoKnopf.AutoSize = false;
        DemoKnopf.Padding = new Padding(Stil.AbstandMini, 0, Stil.AbstandMini, 0);   // passt so in eine Zeile
        DemoKnopf.Click += (_, _) => DemoZuruecksetzen();
        VersionText.Text = Programmversion.Text;
        VersionText.Font = Stil.Klein;
        VersionText.ForeColor = Stil.TextLeise;
        VersionText.AutoSize = true;

        leiste.Controls.Add(programmName);
        foreach (var eintrag in Navigation) leiste.Controls.Add(eintrag);
        leiste.Controls.AddRange([DemoKnopf, VersionText]);
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
        AufgabenZaehlen();
        BenutzerAnzeigen();
        Kopf.LogoSetzen(Einstellungen.LogoPfad());
        // Erst „Bewohner“ (baut die Karten); das Programm wechselt danach zur Übersicht (Startbereich).
        BereichWaehlen("Bewohner");
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
        if (name == "Neuer Bericht …")
        {
            NeuerBericht();
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
        if (schluessel == "Medikamente")
        {
            Medikamente.Aktualisieren();
            Kopf.TexteSetzen(Medikamente.KopfText, "Medikamente");
            Kopf.KnoepfeSetzen(Medikamente.NeuKnopf);
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
    }

    public void BenutzerWaehlen(string kuerzel)
    {
        Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        BenutzerAnzeigen();
        AufgabenZaehlen();
        if (AktiverBereich == "Bewohner")
        {
            if (!Bewohner.AkteSichtbar) Bewohner.UebersichtZeigen();
        }
        else BereichWaehlen(AktiverBereich, filterBehalten: true);
    }

    private void BenutzerMenueZeigen()
    {
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false, ShowCheckMargin = true };
        string ich = Einstellungen.Laden().Kuerzel;
        foreach (var k in speicher.Kollegen())
        {
            var eintrag = new ToolStripMenuItem($"{k.Anzeige} – {string.Join(", ", k.Teams)}") { Checked = k.Kuerzel == ich };
            eintrag.Click += (_, _) => BenutzerWaehlen(k.Kuerzel);
            menue.Items.Add(eintrag);
        }
        menue.Items.Add(new ToolStripSeparator());
        var niemand = new ToolStripMenuItem("Abmelden (niemand)") { Checked = ich.Length == 0 };
        niemand.Click += (_, _) => BenutzerWaehlen("");
        menue.Items.Add(niemand);
        menue.Closed += (_, _) => BeginInvoke(menue.Dispose);
        menue.Show(Kopf.BenutzerKnopf, new Point(Kopf.BenutzerKnopf.Width, Kopf.BenutzerKnopf.Height), ToolStripDropDownDirection.BelowLeft);
    }

    // Neuer Bericht mit Bewohnerauswahl; danach die Akte des Bewohners beim Reiter „Berichte“.
    public Bericht? NeuerBericht()
    {
        using var formular = new BerichtFormular(speicher, null);
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
            var eintrag = new SeitenleistenEintrag(name);
            eintrag.Click += (_, _) => BereichWaehlen(name);
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
        }
        if (disposing) speicher.ModuleGeaendert -= ModuleAnzeigen;
        base.Dispose(disposing);
    }

    private void LeisteAnordnen()
    {
        int x = leiste.Padding.Left, breite = leiste.ClientSize.Width - leiste.Padding.Horizontal;
        int y = leiste.Padding.Top + S(Stil.AbstandKlein);
        programmName.Location = new Point(x + S(Stil.AbstandMini), y);
        y += programmName.PreferredHeight + S(Stil.AbstandGross);
        foreach (var eintrag in Navigation)
        {
            // Einstellungen etwas abgesetzt: Sie sind kein Arbeitsbereich.
            if (eintrag.Text == "Einstellungen") y += S(Stil.AbstandMittel);
            eintrag.Bounds = new Rectangle(x, y, breite, S(36));
            y += eintrag.Height + S(Stil.AbstandMini);
        }

        int unten = leiste.ClientSize.Height - leiste.Padding.Bottom;
        VersionText.Location = new Point(x + S(Stil.AbstandMini), unten - VersionText.PreferredHeight);
        unten = VersionText.Top - S(Stil.AbstandMittel);
        DemoKnopf.Bounds = new Rectangle(x, unten - S(Stil.KnopfHoehe), breite, S(Stil.KnopfHoehe));
    }

    // Alle Bewohner und Bilder weg, Demo-Daten neu laden – mit Rückfrage.
    public bool DemoZuruecksetzen(bool nachfragen = true)
    {
        if (nachfragen && MessageBox.Show(this,
                "Alle Änderungen und hinterlegten Bilder werden gelöscht und die ausgedachten Demo-Daten neu geladen.\n\nFortfahren?",
                "Demo-Daten zurücksetzen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            != DialogResult.Yes)
            return false;
        speicher.DemoZuruecksetzen();
        BereichWaehlen("Bewohner");
        return true;
    }
}
