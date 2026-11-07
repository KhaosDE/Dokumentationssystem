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
    public static readonly string[] Bereiche =
        ["Übersicht", "Bewohner", "Neuer Bericht …", "Übergabe", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
         "Medikamente", "Ausgangsliste", "Einstellungen"];

    private readonly Speicher speicher;
    private readonly Panel leiste = new();
    private readonly Label programmName = new();
    private readonly Panel bereichsFlaeche = new() { Dock = DockStyle.Fill, BackColor = Stil.Seite };
    private readonly Dictionary<string, Control> seiten = [];

    public KopfBereich Kopf { get; } = new() { Dock = DockStyle.Top };
    public Statusleiste Statusleiste { get; } = new() { Dock = DockStyle.Bottom };
    public BewohnerBereich Bewohner { get; }
    public UebergabeSeite Uebergabe { get; }
    public List<SeitenleistenEintrag> Navigation { get; } = [];
    public Label VersionText { get; } = new();
    public Button DemoKnopf { get; } = Stil.NebenKnopf("Demo-Daten zurücksetzen …");
    public string AktiverBereich { get; private set; } = "";

    // Zum Testen ersetzbar: zeigt das Einstellungsfenster; true = gespeichert.
    internal Func<EinstellungenFenster, bool> EinstellungenZeigen { get; set; }

    public Hauptfenster(Speicher speicher)
    {
        this.speicher = speicher;
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

        // ----- Inhalt rechts: Kopf oben, Bereich darunter -----
        var inhalt = new Panel { Dock = DockStyle.Fill, BackColor = Stil.Seite };
        inhalt.Controls.Add(bereichsFlaeche);
        inhalt.Controls.Add(Kopf);
        inhalt.Controls.Add(Statusleiste);
        bereichsFlaeche.BringToFront();   // Fill wird zuletzt angeordnet und nimmt den Rest

        Controls.Add(inhalt);
        Controls.Add(leiste);
        inhalt.BringToFront();

        Kopf.LogoSetzen(Einstellungen.LogoPfad());
        // Start im Bereich „Bewohner“, solange die Übersicht noch nicht gebaut ist (kommt mit 0.17.0).
        BereichWaehlen("Bewohner");
    }

    private int S(int wert) => Stil.Px(this, wert);

    private static string Schluessel(string eintrag) => eintrag.Replace(" …", "");

    public Control Seite(string bereich) => seiten[Schluessel(bereich)];

    // Wechselt den Bereich. „Bewohner“ führt immer zur Übersicht der Bewohner.
    public void BereichWaehlen(string name)
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
        if (schluessel == "Bewohner")
        {
            Bewohner.UebersichtZeigen();
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
        using var fenster = new EinstellungenFenster();
        if (!EinstellungenZeigen(fenster)) return false;
        Kopf.LogoSetzen(Einstellungen.LogoPfad());
        return true;
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
