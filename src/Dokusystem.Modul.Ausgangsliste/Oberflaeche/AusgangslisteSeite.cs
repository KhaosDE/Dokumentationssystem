using Dokusystem.Kern;

namespace Dokusystem.Modul.Ausgangsliste;

// Die Seite „Ausgangsliste“: oben Küchendienst und Digitaler Ausgang (wenn eingeschaltet), darunter
// je Bewohner eine Karte, nach Zimmer. Ausdruck der Tagesliste über den Knopf im Kopf.
public sealed class AusgangslisteSeite : ModulSeite
{
    private readonly Datenbank db;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public Button DruckKnopf { get; } = Stil.HauptKnopf("Tagesliste drucken …");
    public Button KuechendienstKnopf { get; } = Stil.NebenKnopf("Küchendienst …");
    public Button NachtberichtKnopf { get; } = Stil.NebenKnopf("Nachtbericht …");
    public Label KuechendienstText { get; } = new() { AutoSize = true, BackColor = Stil.Seite, ForeColor = Stil.TextNormal, Font = Stil.Fett, UseMnemonic = false };
    public List<PlatzKarte> Karten { get; } = [];

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
        leiste.Controls.AddRange([KuechendienstText, KuechendienstKnopf, NachtberichtKnopf]);
        Controls.Add(leiste);
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
        NachtberichtKnopf.Visible = DigitalAn;
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
        Karten.Clear();
        foreach (var p in plaetze)
        {
            var stand = DigitalAn ? DigitalerAusgang.StandFuer(p, ausgaenge, jetzt.TimeOfDay) : null;
            var karte = new PlatzKarte(p, jetzt, stand, kuecheZimmer.Contains(p.Zimmernummer) && p.Anwesend);
            Aktionen.Verbinden(karte, db, this, Zeigen);
            Karten.Add(karte);
            inhalt.Add(karte);
        }
        if (Karten.Count == 0) inhalt.Add(new Label { Text = "Noch keine Bewohner.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
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

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Bounds = new Rectangle(new Point(rand, y), leiste.GetPreferredSize(new Size(Math.Max(100, breite), 0)));
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is PlatzKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand, y + Stil.Px(this, Stil.AbstandKlein));
                y = c.Bottom;
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
        void Fenster(Form f)
        {
            using (f)
                if (Dialog.Zeigen(f, besitzer.FindForm()) == DialogResult.OK) danach();
        }
        karte.AusgangKnopf.Click += (_, _) => Fenster(new AusgangFenster(db, db.Platz(p.BewohnerId) ?? p));
        karte.AnwesenheitKnopf.Click += (_, _) => Fenster(new StatusFenster(db, db.Platz(p.BewohnerId) ?? p));
        karte.UnterbringungKnopf.Click += (_, _) => Fenster(new UnterbringungFenster(db, db.Platz(p.BewohnerId) ?? p));
        karte.VerlaufKnopf.Click += (_, _) =>
        {
            var zeilen = Verlauf.Zeilen(db.VerlaufLaden(p.BewohnerId));
            using var f = new TextFenster($"Verlauf: {p.Bewohnername}",
                zeilen.Count == 0 ? "Noch keine Einträge." : string.Join(Environment.NewLine, zeilen));
            Dialog.Zeigen(f, besitzer.FindForm());
        };
        void Beginnen(string art)
        {
            try { db.AusgangBeginnen(p.BewohnerId, art, Einstellungen.Laden().Kuerzel); }
            catch (Eingabefehler e)
            {
                Melden(besitzer.FindForm(), string.Join("\n", e.Fehler.Select(f => f == "Handzeichen fehlt."
                    ? "Handzeichen fehlt: Bitte zuerst oben rechts anmelden – das Kürzel ist das Handzeichen." : f)));
                return;
            }
            danach();
        }
        karte.AlleinKnopf.Click += (_, _) => Beginnen("allein");
        karte.MitBewohnerKnopf.Click += (_, _) => Beginnen("mit Bewohner");
        karte.ZurueckKnopf.Click += (_, _) =>
        {
            var laufend = db.AusgaengeLaden(db.Jetzt()).LastOrDefault(e => e.BewohnerId == p.BewohnerId && e.Unterwegs);
            if (laufend is null) { danach(); return; }
            try { db.AusgangBeenden(laufend.Id, Einstellungen.Laden().Kuerzel); }
            catch (Eingabefehler e)
            {
                Melden(besitzer.FindForm(), string.Join("\n", e.Fehler.Select(f => f == "Handzeichen fehlt."
                    ? "Handzeichen fehlt: Bitte zuerst oben rechts anmelden – das Kürzel ist das Handzeichen." : f)));
                return;
            }
            danach();
        };
    }
}
