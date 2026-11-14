using DokuLine.Kern;

namespace DokuLine;

// Anmelden, Abmelden und Rechte: Kopf, Seitenleiste und gezeigte Ansicht folgen dem Angemeldeten; dazu die Wahl
// des Bereichs, die überall gilt.
public sealed partial class Hauptfenster
{
    // „Angemeldet: TST – Name“ und „Abmelden (F12)“ oben rechts; ohne Passwort, nur für die Vorführung.
    public void BenutzerAnzeigen()
    {
        string kuerzel = Recht.Kuerzel;
        Kopf.BenutzerSetzen(kuerzel.Length > 0 ? kuerzel : null, speicher.MitarbeiterLesen(kuerzel)?.Name);
        AbmeldenEintrag.Text = kuerzel.Length > 0 ? Abmeldung.KnopfText : "Anmelden …";
        AbmeldenEintrag.AccessibleName = AbmeldenEintrag.Text;
        hinweis.SetToolTip(AbmeldenEintrag, AbmeldenEintrag.Text);
        EntwuerfeZaehlen();
        NachrichtenZaehlen();
    }

    public void BenutzerWaehlen(string kuerzel)
    {
        Recht.Anmelden(kuerzel);
        RechteNeu();
    }

    // Anmeldung oder Rechte geändert: Kopf, Zähler, Seitenleiste und die gezeigte Ansicht neu.
    private void RechteNeu()
    {
        if (IsDisposed) return;
        BenutzerAnzeigen();
        ArbeitsbereichPruefen(zuruecksetzen: !Recht.Angemeldet);
        AufgabenZaehlen();
        if (Ausgeblendet(AktiverBereich)) { BereichWaehlen(StartSeite); return; }
        if (AktiverBereich == "Dokumentation")
        {
            Bewohner.NeuLesen();
            if (Bewohner.AkteSichtbar) Bewohner.Akte.AnsichtNeu();
        }
        else if (AktiverBereich.Length > 0) BereichWaehlen(AktiverBereich, filterBehalten: true);
    }

    // Die Bereiche der Auswahl in der Seitenleiste, je nach Recht.
    public List<string> BereichKnoepfe() =>
        BereichswahlRegeln.Knoepfe(speicher.SichtbareWohnbereiche(Recht.Kuerzel), f => Recht.Darf(speicher, f));

    // Den Bereich wählen, der überall gilt (Seitenleiste); die gezeigte Ansicht und die Zähler folgen sofort.
    public void ArbeitsbereichWaehlen(string bereich)
    {
        bool vorher = TherapieListe.Aktiv;
        Arbeitsbereich.Gewaehlt = bereich;
        KopfBereichSetzen();
        AufgabenZaehlen();
        NavigationRechte();
        if (bereich == QualitaetRegeln.BereichQm && !Ausgeblendet("Qualität")) BereichWaehlen("Qualität");   // QM beginnt mit der Prüfseite
        else if (vorher != TherapieListe.Aktiv) BereichWaehlen(Entsprechung(AktiverBereich), filterBehalten: true);
        else if (AktiverBereich.Length > 0) BereichWaehlen(AktiverBereich, filterBehalten: true);
    }

    // In den Bereich Therapie und zurück wechselt die Seitenleiste: dieselbe Seite, wenn es sie dort gibt, sonst die
    // Startseite. Die Therapie-Seite heißt dort nach ihrem Reiter.
    private string Entsprechung(string bereich)
    {
        if (Schluessel(bereich) == "Therapie") return TherapieListe.Aktiv ? Therapie.Reiter.Aktiv : "Therapie";
        return bereich.Length > 0 && !Ausgeblendet(bereich) ? bereich : StartSeite;
    }

    // Im Bereich Therapie sind die Reiter Einträge der Seitenleiste: Ein Wechsel (z. B. Klick in die Tagesleiste)
    // markiert den passenden Eintrag.
    private void TherapieReiterGewechselt(string reiter)
    {
        if (!TherapieListe.Aktiv || Schluessel(AktiverBereich) != "Therapie" || AktiverBereich == reiter) return;
        AktiverBereich = reiter;
        foreach (var eintrag in Navigation) eintrag.Aktiv = eintrag.Text == reiter;
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
        NavigationRechte();   // der Bereich Therapie hat eine eigene Seitenleiste
    }

    private void KopfBereichSetzen()
    {
        string bereich = Arbeitsbereich.Gewaehlt;
        Kopf.BereichSetzen(bereich, BereichswahlRegeln.Eingesprungen(bereich, Einstellungen.Laden().Bereich));
        BereichsWahl.Setzen(BereichKnoepfe(), bereich);
        leiste.PerformLayout();   // mit einem Bereich ohne Auswahl ändert sich die Höhe nicht, eingeklappt das Kürzel
    }

    // Bereiche mit eigenem Recht; ohne dieses Recht verschwinden sie aus der Seitenleiste.
    private static readonly (string Bereich, string Funktion)[] BereichsRechte = [("Visiten", Funktionen.Visiten), ("Auswertung", Funktionen.Zeitauswertung),
        ("Medikamentenausgabe", Funktionen.MedikamenteAusgeben), ("Bestellung", Funktionen.MedikamenteAusgeben), ("Qualität", Funktionen.Qualitaet),
        ("Archiv", Funktionen.ArchivEinsehen)];

    public HashSet<string> OhneRechtAusgeblendet { get; } = [];

    // Damit beginnt das Programm (nach dem Start und dem Anmelden): im Bereich Therapie die laufenden Therapien.
    public string StartSeite => TherapieListe.Aktiv ? TherapieSeite.LaufendReiter : Startbereich;

    public bool Ausgeblendet(string eintrag) => OhneRechtAusgeblendet.Contains(eintrag)
        || (TherapieListe.Aktiv ? !TherapieSeitenleiste.Contains(eintrag) : TherapieBereiche.Contains(eintrag));

    private static int Rang(string eintrag) => TherapieListe.Aktiv ? Array.IndexOf(TherapieSeitenleiste, eintrag) : 0;

    private void NavigationRechte()
    {
        OhneRechtAusgeblendet.Clear();
        foreach (var (bereich, funktion) in BereichsRechte)
            if (RechteRegeln.FehltRecht(Recht.Grund(speicher, funktion))) OhneRechtAusgeblendet.Add(bereich);
        foreach (var eintrag in Navigation) eintrag.Visible = !Ausgeblendet(eintrag.Text);
        leiste.PerformLayout();
    }

    // ----- Abmelden -----

    private void AbmeldenOderAnmelden()
    {
        if (Recht.Angemeldet) Abmelden();
        else AnmeldenZeigen();
    }

    // Offene Fenster des Programms (Dialoge), die beim Abmelden geschlossen werden.
    // F11 im Tagesplan: nur die Tafel, bildschirmfüllend.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F11 && AktiverBereich == "Tagesplan" && !Gesperrt) { Tagesplan.VollbildZeigen(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    public List<Form> OffeneFenster() =>
        [.. Application.OpenForms.Cast<Form>().Where(f => f != this && f is not Hauptfenster && f is not AnmeldenFenster && f is not AbmeldenFrage && f.Visible)];

    // Abmelden: offene Fenster schließen (vorher fragen, angefangene Eingaben als Entwurf), dann liegt die
    // Sperrfläche über allem und das Anmeldefenster darüber. false = abgebrochen.
    // Bei der automatischen Sperre ausgeblendete Windows-Dialoge; nach der Anmeldung wieder sichtbar.
    private readonly List<IntPtr> verborgeneDialoge = [];

    // automatisch: Sperre nach Leerlauf im Betrieb – ohne Frage, angefangene Eingaben werden Entwurf.
    public bool Abmelden(bool automatisch = false)
    {
        if (Gesperrt) return false;
        if (automatisch) verborgeneDialoge.AddRange(Leerlauf.DialogeVerbergen());
        var offen = OffeneFenster();
        if (offen.Count > 0)
        {
            var mitEingaben = offen.OfType<IEntwurfFenster>().Where(e => e.HatEingaben).ToList();
            var antwort = DialogResult.Yes;
            if (!automatisch)
            {
                using var frage = new AbmeldenFrage(offen.Select(f => f.Text.Replace($" ({Stil.DemoHinweis})", "")).ToList(), mitEingaben.Count > 0);
                antwort = Dialog.Zeigen(frage, offen[^1]);
            }
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
            Leerlauf.DialogeZeigen(verborgeneDialoge);
            verborgeneDialoge.Clear();
            if (gesperrt)
            {
                Sperren(false);
                BereichWaehlen(StartSeite);
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
}
