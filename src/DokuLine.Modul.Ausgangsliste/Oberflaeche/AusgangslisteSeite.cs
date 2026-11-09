using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Die Seite „Ausgangsliste“ (eine Liste, angelehnt an die Tagesliste): oben Küchendienst, Digitaler Ausgang (wenn eingeschaltet) und Filter, darunter eine
// Zeile je Bewohner. Wer unterwegs ist, steht oben (Überfällige zuerst), dann alle anderen nach
// Zimmer. Ausdruck der Tagesliste über den Knopf im Kopf.
public sealed class AusgangslisteSeite : ModulSeite
{
    private readonly Datenbank db;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly ListenKopf kopf = new();
    private readonly List<Control> inhalt = [];
    private readonly ToolTip hinweis = new();

    public Button DruckKnopf { get; } = Stil.HauptKnopf("Tagesliste drucken …");
    public Button KuechendienstKnopf { get; } = Stil.NebenKnopf("Küchendienst …");
    public Button NachtberichtKnopf { get; } = Stil.NebenKnopf("Nachtbericht …");
    public CheckBox NurUnterwegs { get; } = new() { Text = "nur unterwegs", AutoSize = true, BackColor = Stil.Seite };
    public CheckBox NurBeschraenkung { get; } = new() { Text = "nur mit Beschränkung", AutoSize = true, BackColor = Stil.Seite };
    public Label KuechendienstText { get; } = new() { AutoSize = true, BackColor = Stil.Seite, ForeColor = Stil.TextNormal, Font = Stil.Fett, UseMnemonic = false };
    public List<ListenZeile> Zeilen { get; } = [];
    public List<string> Abschnitte { get; } = [];

    public AusgangslisteSeite(Speicher speicher)
    {
        db = new Datenbank(speicher);
        AutoScroll = true;
        BackColor = Stil.Seite;
        DruckKnopf.Click += (_, _) => Drucken();
        KuechendienstKnopf.Click += (_, _) =>
        {
            using var fenster = new KuechendienstFenster(db);
            Dialog.Zeigen(fenster, FindForm());
            Zeigen();
        };
        NachtberichtKnopf.Click += (_, _) =>
        {
            using var fenster = new NachtberichtFenster(db.Laden(), db.AusgaengeLaden(db.Jetzt()));
            Dialog.Zeigen(fenster, FindForm());
        };
        KuechendienstText.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein);
        foreach (var k in new[] { KuechendienstKnopf, NachtberichtKnopf }) k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        foreach (var c in new[] { NurUnterwegs, NurBeschraenkung })
        {
            c.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
            c.CheckedChanged += (_, _) => Zeigen();
        }
        leiste.Controls.AddRange([KuechendienstText, KuechendienstKnopf, NachtberichtKnopf, NurUnterwegs, NurBeschraenkung]);
        Controls.Add(leiste);
        Controls.Add(kopf);
    }

    private bool KuechendienstAn => db.Speicher.ModulAn(Kuechendienst.Schluessel);
    private bool DigitalAn => db.Speicher.ModulAn(DigitalerAusgang.Schluessel);

    public override IReadOnlyList<Button> Knoepfe => [DruckKnopf];

    public override string KopfText
    {
        get
        {
            var plaetze = db.Laden();
            var jetzt = db.Jetzt();
            var teile = new List<string>
            {
                $"{plaetze.Count} Bewohner",
                $"{plaetze.Count(p => p.Status == AnwesenheitsStatus.Abwesenheit)} abwesend",
                $"{plaetze.Count(p => p.Status == AnwesenheitsStatus.Ausgangsbeschraenkung)} mit Ausgangsbeschränkung",
            };
            if (DigitalAn)
            {
                var heute = db.AusgaengeLaden(jetzt);
                var staende = plaetze.Select(p => DigitalerAusgang.StandFuer(p, heute, jetzt.TimeOfDay)).ToList();
                teile.Add($"{staende.Count(s => s.Laufend is not null)} unterwegs");
                int ueber = staende.Count(s => s.Ueberfaellig);
                if (ueber > 0) teile.Add($"{ueber} überfällig");
            }
            return string.Join(" · ", teile);
        }
    }

    public override void Zeigen()
    {
        var jetzt = db.Jetzt();
        var plaetze = db.Laden();
        if (DigitalAn) db.AusgaengeLoeschenVor(jetzt.Date.AddMonths(-DigitalerAusgang.AufbewahrungMonate));
        HashSet<string> kuecheZimmer = [];
        KuechendienstKnopf.Visible = KuechendienstText.Visible = KuechendienstAn;
        NachtberichtKnopf.Visible = NurUnterwegs.Visible = DigitalAn;
        if (KuechendienstAn)
        {
            Kuechendienst.Planen(db, plaetze, jetzt);
            var plan = db.KuechendienstLaden();
            kuecheZimmer = Kuechendienst.ZimmerAm(plan, jetzt);
            KuechendienstText.Text = $"Küchendienst heute: {Kuechendienst.AnzeigeFuer(plan, jetzt, plaetze)} · morgen: {Kuechendienst.AnzeigeFuer(plan, jetzt.AddDays(1), plaetze)}";
        }
        var ausgaenge = DigitalAn ? db.AusgaengeLaden(jetzt) : [];

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Zeilen.Clear();
        Abschnitte.Clear();
        hinweis.RemoveAll();
        // Gezeigt wird der in der Übersicht gewählte Bereich (geplant wird der Küchendienst weiter für alle).
        var imBereich = BereichswahlRegeln.Bewohner(db.Speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt)
            .Select(b => b.Id!.Value).ToHashSet();
        var alle = plaetze.Where(p => imBereich.Contains(p.BewohnerId))
            .Select(p => new ListenZeile(p, jetzt, DigitalAn ? DigitalerAusgang.StandFuer(p, ausgaenge, jetzt.TimeOfDay) : null,
                kuecheZimmer.Contains(p.Zimmernummer) && p.Anwesend)).ToList();
        bool nurUnterwegs = DigitalAn && NurUnterwegs.Checked;
        var gezeigt = alle.Where(z => (!nurUnterwegs || z.Unterwegs) && (!NurBeschraenkung.Checked || z.Platz.Status == AnwesenheitsStatus.Ausgangsbeschraenkung)).ToList();
        foreach (var z in alle.Except(gezeigt)) z.Dispose();
        // Oben, wer unterwegs ist (Überfällige zuerst, dann nach Beginn); darunter alle anderen nach Zimmer.
        var unterwegs = gezeigt.Where(z => z.Unterwegs).OrderByDescending(z => z.Ueberfaellig).ThenBy(z => z.HeuteText.Split(" · ")[^1]).ToList();
        var andere = gezeigt.Where(z => !z.Unterwegs).ToList();
        void Abschnitt(string text)
        {
            Abschnitte.Add(text);
            inhalt.Add(new Label { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, UseMnemonic = false });
        }
        foreach (var (titel, zeilen) in new[] { ("UNTERWEGS", unterwegs), (unterwegs.Count > 0 ? "IM HAUS ODER ABWESEND" : "BEWOHNER", andere) })
        {
            if (zeilen.Count == 0) continue;
            Abschnitt($"{titel} ({zeilen.Count})");
            foreach (var z in zeilen)
            {
                Aktionen.Verbinden(z, db, this, Zeigen);
                z.Hinweise(hinweis);
                Zeilen.Add(z);
                inhalt.Add(z);
            }
        }
        int knopfBreite = Zeilen.Count > 0 ? Zeilen.Max(z => z.EigeneKnopfBreite) : 0;
        foreach (var z in Zeilen) z.KnopfBreite = knopfBreite;
        kopf.KnopfBreite = knopfBreite;
        kopf.Visible = Zeilen.Count > 0;
        if (Zeilen.Count == 0)
            inhalt.Add(new Label
            {
                Text = plaetze.Count == 0 ? "Noch keine Bewohner." : "Niemand passt zum Filter.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite,
            });
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Melden();
    }

    public void Drucken()
    {
        var jetzt = db.Jetzt();
        var plaetze = db.Laden();
        HashSet<string> zimmer = [];
        string text = "";
        if (KuechendienstAn)
        {
            var plan = db.KuechendienstLaden();
            zimmer = Kuechendienst.ZimmerAm(plan, jetzt);
            text = Kuechendienst.AnzeigeFuer(plan, jetzt, plaetze);
            if (text == Kuechendienst.KeinDienst) text = "";
        }
        var liste = new Druckliste(plaetze, jetzt, zimmer, text);
        Druckvorschau.Zeigen(FindForm(), liste.DokumentBauen, "Tagesliste");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) hinweis.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(Math.Max(100, breite), 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        if (kopf.Visible)
        {
            kopf.SetBounds(rand, y, breite, kopf.Height);
            kopf.PerformLayout();
            y = kopf.Bottom;
        }
        foreach (var c in inhalt)
        {
            if (c is ListenZeile zeile)
            {
                zeile.SetBounds(rand, y, breite, zeile.Height);
                zeile.PerformLayout();
                y += zeile.Height;
            }
            else
            {
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandMittel));
                y = c.Bottom + Stil.Px(this, Stil.AbstandMini);
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Was die Knöpfe einer Karte tun – auf der Seite und im Akte-Reiter gleich.
public static class Aktionen
{
    // Zum Testen ersetzbar: Meldung, wenn etwas nicht geht (z. B. niemand angemeldet).
    public static Action<IWin32Window?, string> Melden { get; set; } =
        (besitzer, text) => MessageBox.Show(besitzer, text, "Ausgangsliste", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Verbinden(PlatzKarte karte, Datenbank db, Control besitzer, Action danach)
    {
        var p = karte.Platz;
        karte.AusgangKnopf.Click += (_, _) => AusgangAendern(db, p, besitzer, danach);
        karte.AnwesenheitKnopf.Click += (_, _) => Anwesenheit(db, p, besitzer, danach);
        karte.UnterbringungKnopf.Click += (_, _) => UnterbringungAendern(db, p, besitzer, danach);
        karte.VerlaufKnopf.Click += (_, _) => VerlaufZeigen(db, p, besitzer);
        karte.AlleinKnopf.Click += (_, _) => Beginnen(db, p, "allein", besitzer, danach);
        karte.MitBewohnerKnopf.Click += (_, _) => Beginnen(db, p, "mit Bewohner", besitzer, danach);
        karte.ZurueckKnopf.Click += (_, _) => Zurueck(db, p, besitzer, danach);
    }

    // Dieselben Abläufe für eine Zeile der Liste: „raus“/„zurück“ direkt, der Rest über „…“.
    public static void Verbinden(ListenZeile zeile, Datenbank db, Control besitzer, Action danach)
    {
        var p = zeile.Platz;
        zeile.AusgangEintrag.Click += (_, _) => AusgangAendern(db, p, besitzer, danach);
        zeile.AnwesenheitEintrag.Click += (_, _) => Anwesenheit(db, p, besitzer, danach);
        zeile.UnterbringungEintrag.Click += (_, _) => UnterbringungAendern(db, p, besitzer, danach);
        zeile.VerlaufEintrag.Click += (_, _) => VerlaufZeigen(db, p, besitzer);
        zeile.AlleinKnopf.Click += (_, _) => Beginnen(db, p, "allein", besitzer, danach);
        zeile.MitBewohnerKnopf.Click += (_, _) => Beginnen(db, p, "mit Bewohner", besitzer, danach);
        zeile.ZurueckKnopf.Click += (_, _) => Zurueck(db, p, besitzer, danach);
    }

    private static void Fenster(Form f, Control besitzer, Action danach)
    {
        using (f)
            if (Dialog.Zeigen(f, besitzer.FindForm()) == DialogResult.OK) danach();
    }

    public static void AusgangAendern(Datenbank db, Platz p, Control besitzer, Action danach) =>
        Fenster(new AusgangFenster(db, db.Platz(p.BewohnerId) ?? p), besitzer, danach);

    public static void Anwesenheit(Datenbank db, Platz p, Control besitzer, Action danach) =>
        Fenster(new StatusFenster(db, db.Platz(p.BewohnerId) ?? p), besitzer, danach);

    public static void UnterbringungAendern(Datenbank db, Platz p, Control besitzer, Action danach) =>
        Fenster(new UnterbringungFenster(db, db.Platz(p.BewohnerId) ?? p), besitzer, danach);

    public static void VerlaufZeigen(Datenbank db, Platz p, Control besitzer)
    {
        var zeilen = Verlauf.Zeilen(db.VerlaufLaden(p.BewohnerId));
        using var f = new TextFenster($"Verlauf: {p.Bewohnername}",
            zeilen.Count == 0 ? "Noch keine Einträge." : string.Join(Environment.NewLine, zeilen));
        Dialog.Zeigen(f, besitzer.FindForm());
    }

    private static void FehlerMelden(Control besitzer, Eingabefehler e) =>
        Melden(besitzer.FindForm(), string.Join("\n", e.Fehler.Select(f => f == "Handzeichen fehlt."
            ? "Handzeichen fehlt: Bitte zuerst oben rechts anmelden – das Kürzel ist das Handzeichen." : f)));

    public static void Beginnen(Datenbank db, Platz p, string art, Control besitzer, Action danach)
    {
        try { db.AusgangBeginnen(p.BewohnerId, art, Einstellungen.Laden().Kuerzel); }
        catch (Eingabefehler e) { FehlerMelden(besitzer, e); return; }
        danach();
    }

    public static void Zurueck(Datenbank db, Platz p, Control besitzer, Action danach)
    {
        var laufend = db.AusgaengeLaden(db.Jetzt()).LastOrDefault(e => e.BewohnerId == p.BewohnerId && e.Unterwegs);
        if (laufend is null) { danach(); return; }
        try { db.AusgangBeenden(laufend.Id, Einstellungen.Laden().Kuerzel); }
        catch (Eingabefehler e) { FehlerMelden(besitzer, e); return; }
        danach();
    }
}
