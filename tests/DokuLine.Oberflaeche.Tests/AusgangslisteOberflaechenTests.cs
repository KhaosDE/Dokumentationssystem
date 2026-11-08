using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;

namespace DokuLine.Oberflaeche.Tests;

// Modul Ausgangsliste: Seite als Liste (Schritt 27), Fenster für Ausgang, Anwesenheit (Rechtsregel) und
// Unterbringung, Digitaler Ausgang, Küchendienst, Akte-Reiter, Marken auf den Bewohnerkarten und
// die Modulverwaltung (ausschalten ohne Spuren, Daten bleiben).
[Collection("Oberflaeche")]
public class AusgangslisteOberflaechenTests
{
    private static (Umgebung, Hauptfenster, AusgangslisteSeite) Oeffnen()
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Navigation.Single(n => n.Text == "Ausgangsliste").Ausloesen();
        return (u, f, Assert.IsType<AusgangslisteSeite>(f.Seite("Ausgangsliste")));
    }

    private static ListenZeile Karte(AusgangslisteSeite s, string nachname) =>
        s.Zeilen.Single(k => k.Platz.Bewohnername.EndsWith(" " + nachname, StringComparison.Ordinal));

    [Fact]
    public void Seite_mit_Karten_Kuechendienst_und_Kopf() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            Assert.Equal("Tagesliste drucken …", Assert.Single(f.Kopf.Knoepfe).Text);
            Assert.Equal(69, s.Zeilen.Count);   // alle Bereiche (Schritt 26h: 23 je Wohnbereich)
            // Oben, wer unterwegs ist (Demo: Clematis, Ginster), darunter alle anderen nach Zimmer.
            Assert.True(s.Zeilen[0].Unterwegs);
            Assert.StartsWith("UNTERWEGS (", s.Abschnitte[0]);
            Assert.Equal("101", s.Zeilen.First(z => !z.Unterwegs).Platz.Zimmernummer);
            Assert.Equal("im Haus", Karte(s, "Ahorn").StandText);
            Assert.StartsWith("unterwegs seit ", Karte(s, "Clematis").StandText);
            Assert.StartsWith("abwesend: ", Karte(s, "Holunder").StandText);
            Hilfe.Foto(f, "88_ausgangsliste");
            Assert.StartsWith("Küchendienst heute: ", s.KuechendienstText.Text);
            Assert.Contains(Karte(s, "Holunder").MarkenTexte, t => t.StartsWith("abwesend: Heimfahrt bis"));
            if (DateTime.Now.TimeOfDay > TimeSpan.FromHours(2.5))   // Demo-Zeiten liegen kurz nach Mitternacht nicht weit genug zurück
                Assert.Contains(Karte(s, "Ginster").MarkenTexte, t => t.StartsWith("überfällig seit"));
            Assert.Contains(Karte(s, "Clematis").MarkenTexte, t => t.StartsWith("unterwegs seit"));
            Assert.Contains("Unterbringung nicht erfasst", Karte(s, "Distel").MarkenTexte);
            Assert.StartsWith("Ausgang 3 Std: 2 Std allein · 1 Std mit Bewohner", Karte(s, "Ahorn").AusgangZeile);
            Assert.True(Karte(s, "Ahorn").AlleinKnopf.Visible);
            Assert.False(Karte(s, "Holunder").AlleinKnopf.Visible);   // abwesend
            Assert.True(Karte(s, "Clematis").ZurueckKnopf.Visible);
            if (DateTime.Now.TimeOfDay > TimeSpan.FromHours(2.5))
            {
                Assert.True(Karte(s, "Ginster").Ueberfaellig);
                Assert.StartsWith("überfällig seit ", Karte(s, "Ginster").StandText);
                Assert.True(s.Zeilen[0].Ueberfaellig);   // Überfällige ganz oben
            }

            // Filter: nur unterwegs, nur mit Beschränkung.
            s.NurUnterwegs.Checked = true;
            Assert.All(s.Zeilen, z => Assert.True(z.Unterwegs));
            Assert.NotEmpty(s.Zeilen);
            s.NurUnterwegs.Checked = false;
            s.NurBeschraenkung.Checked = true;
            Assert.All(s.Zeilen, z => Assert.Equal(AnwesenheitsStatus.Ausgangsbeschraenkung, z.Platz.Status));
            s.NurBeschraenkung.Checked = false;
            Assert.Equal(69, s.Zeilen.Count);

            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                f.Kopf.Knoepfe[0].PerformClick();
                Assert.StartsWith("Tagesliste", Assert.Single(ersatz.Titel));
            }
            using var bild = new Bitmap(1130, 800);
            using var g = Graphics.FromImage(bild);
            new Druckliste(new Datenbank(u.Speicher).Laden(), DateTime.Today, ["103"], "Clara Clematis").SeiteZeichnen(g);
        }
    });

    [Fact]
    public void Ausgang_aendern_schreibt_den_Verlauf() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            using (new DialogErsatz(fenster =>
            {
                var a = Assert.IsType<AusgangFenster>(fenster);
                Hilfe.Zeigen(a);   // PerformClick wirkt nur bei angezeigten Fenstern
                a.AlleinPlus.PerformClick();
                a.MbwMinus.PerformClick();
                a.InfoAbfragen = () => "Geld mitnehmen";
                a.InfoKnopf.PerformClick();
                Assert.Equal("3 Allein / (Geld mitnehmen)", a.NeuerText);
                Assert.Equal("Summe: 3 Std", a.Summe.Text);
                return a.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte(s, "Ahorn").AusgangEintrag.PerformClick();
            Assert.StartsWith("Ausgang 3 Std: 3 Std allein", Karte(s, "Ahorn").AusgangZeile);

            using (var ersatz = new DialogErsatz(fenster =>
            {
                var t = Assert.IsType<TextFenster>(fenster);
                Assert.Contains("CD · Ausgang: „2 Allein / 1 MBW“ → „3 Allein / (Geld mitnehmen)“", t.Inhalt.Text);
                return DialogResult.Cancel;
            }))
            {
                Karte(s, "Ahorn").VerlaufEintrag.PerformClick();
                Assert.Single(ersatz.Titel);
            }
        }
    });

    [Fact]
    public void Anwesenheit_mit_Rechtsregel_und_Marke_auf_der_Bewohnerkarte() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            // Offen geführt: keine Ausgangsbeschränkung, aber Abwesenheit geht.
            using (new DialogErsatz(fenster =>
            {
                var st = Assert.IsType<StatusFenster>(fenster);
                Hilfe.Zeigen(st);   // Visible stimmt nur bei angezeigten Fenstern
                Assert.False(st.BeschraenkungKnopf.Enabled);
                Assert.Equal(Platz.HinweisNurMitBeschluss, st.Hinweis.Text);
                st.Rueckfrage = _ => DialogResult.Yes;
                Assert.False(st.Setzen(AnwesenheitsStatus.Abwesenheit));   // ohne Grund
                Assert.True(st.Fehler.Visible);
                st.Grund.Text = "Heimfahrt bis 20.11.2026";
                return st.Setzen(AnwesenheitsStatus.Abwesenheit) ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte(s, "Birke").AnwesenheitEintrag.PerformClick();
            Assert.Contains("abwesend: Heimfahrt bis 20.11.2026", Karte(s, "Birke").MarkenTexte);
            Assert.StartsWith("Abwesenheit: Heimfahrt bis 20.11.2026", Karte(s, "Birke").AusgangZeile);

            // Beschluss in der Akte nicht erfasst: Hinweis „bitte erst klären“.
            using (new DialogErsatz(fenster =>
            {
                var st = Assert.IsType<StatusFenster>(fenster);
                Assert.False(st.BeschraenkungKnopf.Enabled);
                Assert.Equal(Platz.HinweisNichtErfasst, st.Hinweis.Text);
                return DialogResult.Cancel;
            }))
                Karte(s, "Distel").AnwesenheitEintrag.PerformClick();

            f.BereichWaehlen("Bewohner");
            var birke = f.Bewohner.Zeilen.Single(k => k.Bewohner.Nachname == "Birke");
            Assert.Contains(birke.Marken, m => m.Art == MarkenArt.Abwesend && m.Text == "abwesend: Heimfahrt bis 20.11.");   // schmal: ohne Jahr
        }
    });

    [Fact]
    public void Unterbringung_Beschluss_steht_danach_in_der_Akte() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            using (new DialogErsatz(fenster =>
            {
                if (fenster is DatumFenster datum)
                {
                    datum.Datumsfeld.Text = "31.05.2027";
                    return datum.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                }
                var w = Assert.IsType<UnterbringungFenster>(fenster);
                w.Rueckfrage = _ => DialogResult.Yes;
                return w.Waehlen(Unterbringung.Beschluss) ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte(s, "Distel").UnterbringungEintrag.PerformClick();
            Assert.Contains("Beschluss", Karte(s, "Distel").UnterbringungZeile);
            var akte = u.Speicher.Bewohner(u.Demo("Distel").Id!.Value)!;
            Assert.Equal(BeschlussZustand.Ende, akte.Beschluss);
            Assert.Equal(new DateOnly(2027, 5, 31), akte.Beschlussende);
        }
    });

    [Fact]
    public void Digitaler_Ausgang_beginnen_beenden_und_Nachtbericht() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            Karte(s, "Ahorn").AlleinKnopf.PerformClick();
            Assert.Contains(Karte(s, "Ahorn").MarkenTexte, t => t.StartsWith("unterwegs seit"));
            Assert.Contains("unterwegs", s.KopfText);
            Karte(s, "Ahorn").ZurueckKnopf.PerformClick();
            Assert.DoesNotContain(Karte(s, "Ahorn").MarkenTexte, t => t.StartsWith("unterwegs seit"));
            Assert.Contains(new Datenbank(u.Speicher).VerlaufLaden(u.Demo("Ahorn").Id!.Value), v => v.Text.StartsWith("Ausgang beendet:"));

            // Ohne Anmeldung fehlt das Handzeichen: Meldung statt Ausgang.
            Einstellungen.Aendern(w => w with { Kuerzel = "" });
            var meldungen = new List<string>();
            var vorher = Aktionen.Melden;
            Aktionen.Melden = (_, text) => meldungen.Add(text);
            try { Karte(s, "Fichte").AlleinKnopf.PerformClick(); }
            finally { Aktionen.Melden = vorher; }
            Assert.StartsWith("Handzeichen fehlt", Assert.Single(meldungen));

            string kopiert = "";
            using (new DialogErsatz(fenster =>
            {
                var n = Assert.IsType<NachtberichtFenster>(fenster);
                Hilfe.Zeigen(n);
                n.Kopieren = text => kopiert = text;
                n.AlleKnopf.PerformClick();
                return DialogResult.Cancel;
            }))
                s.NachtberichtKnopf.PerformClick();
            Assert.Contains("Anton Ahorn hat den Ausgang wahrgenommen:", kopiert);
        }
    });

    [Fact]
    public void Kuechendienst_Tausch_und_Zimmergruppen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        var db = new Datenbank(u.Speicher);
        using var k = new KuechendienstFenster(db);
        Hilfe.Zeigen(k);
        Assert.NotEmpty(k.Tage);
        var morgen = DateTime.Today.AddDays(1);
        var tag = k.Tage.Single(t => t.Datum == morgen);
        string anderes = k.Tage.First(t => t.Zimmer != tag.Zimmer && t.Datum > morgen).Zimmer;
        using (new DialogErsatz(fenster =>
        {
            var t = Assert.IsType<TauschFenster>(fenster);
            t.Rueckfrage = _ => DialogResult.Yes;
            t.Zimmerauswahl.SelectedItem = anderes;
            return t.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            k.TauschBearbeiten(morgen);
        Assert.Equal(anderes, db.KuechendienstLaden().Single(t => t.Datum == morgen).Wirksam);

        using (new DialogErsatz(fenster =>
        {
            var z = Assert.IsType<ZimmergruppenFenster>(fenster);
            z.Zimmer1.SelectedItem = "101";
            z.Zimmer2.SelectedItem = "102";
            Assert.True(z.GruppeHinzufuegen());
            return z.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            k.GruppenBearbeiten();
        Assert.Contains(Kuechendienst.Gruppen(db), g => g.SequenceEqual(["101", "102"]));

        using var bild = new Bitmap(800, 1130);
        using var g = Graphics.FromImage(bild);
        new KuechendienstDruck(DateTime.Today, db.KuechendienstLaden(), DateTime.Now).SeiteZeichnen(g);
    });

    [Fact]
    public void Akte_Reiter_Ausgang() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            f.BereichWaehlen("Bewohner");
            f.Bewohner.AkteZeigen(u.Demo("Holunder").Id!.Value);
            f.Bewohner.Akte.Reiter.Waehlen("Ausgang");
            var reiter = Assert.IsType<AusgangReiter>(Assert.Single(f.Bewohner.Akte.ModulAnsichten));
            Assert.True(reiter.Visible);
            Assert.NotNull(reiter.Karte);
            Assert.Contains("Status: Abwesenheit – Heimfahrt bis", reiter.VerlaufText.Text);
        }
    });

    [Fact]
    public void Modul_ausschalten_hinterlaesst_keine_Spuren_und_Daten_bleiben() => Hilfe.Sta(() =>
    {
        var (u, f, s) = Oeffnen();
        using (u)
        using (f)
        {
            f.BenutzerWaehlen("RS");   // Hausleitung: darf Module ein- und ausschalten
            Assert.True(f.EinstellungenOeffnen("Module"));
            var einstellungen = f.Einstellungsseite;
            Assert.True(einstellungen.Module["ausgangsliste"].Checked);
            Assert.True(einstellungen.Module[Kuechendienst.Schluessel].Checked);
            einstellungen.Module["ausgangsliste"].Checked = false;   // gilt sofort
            Assert.False(einstellungen.Module[Kuechendienst.Schluessel].Enabled);
            Assert.Equal("Einstellungen", f.AktiverBereich);
            f.BereichWaehlen("Bewohner");
            Assert.DoesNotContain(f.Navigation, n => n.Text == "Ausgangsliste");
            Assert.DoesNotContain("Ausgang", f.Bewohner.Akte.Reiter.Namen);
            Assert.Empty(f.Bewohner.Akte.ModulAnsichten);
            // „abwesend: unterwegs mit …“ kommt von den Fahrten des Kerns (Schritt 24), nicht vom Modul.
            Assert.DoesNotContain(f.Bewohner.Zeilen.SelectMany(k => k.Marken),
                m => m.Art is MarkenArt.Ausgang || m.Art == MarkenArt.Abwesend && !m.Text.StartsWith("abwesend: unterwegs mit ", StringComparison.Ordinal));

            f.EinstellungenOeffnen("Module");
            einstellungen.Module["ausgangsliste"].Checked = true;
            Assert.Contains(f.Navigation, n => n.Text == "Ausgangsliste");
            Assert.Equal("Ausgangsliste", f.Navigation[^2].Text);   // vor „Einstellungen“
            f.Navigation.Single(n => n.Text == "Ausgangsliste").Ausloesen();
            var neu = Assert.IsType<AusgangslisteSeite>(f.Seite("Ausgangsliste"));
            Assert.Contains(Karte(neu, "Holunder").MarkenTexte, t => t.StartsWith("abwesend:"));

            // Untermodul aus: kein Küchendienst und kein digitaler Ausgang auf der Seite.
            u.Speicher.ModulSetzen(Kuechendienst.Schluessel, false);
            u.Speicher.ModulSetzen(DigitalerAusgang.Schluessel, false);
            f.Navigation.Single(n => n.Text == "Ausgangsliste").Ausloesen();
            neu = Assert.IsType<AusgangslisteSeite>(f.Seite("Ausgangsliste"));
            Assert.False(neu.KuechendienstKnopf.Visible);
            Assert.False(neu.NachtberichtKnopf.Visible);
            Assert.False(Karte(neu, "Ahorn").AlleinKnopf.Visible);
            Assert.DoesNotContain(Karte(neu, "Ginster").MarkenTexte, t => t.StartsWith("überfällig"));
        }
    });
}
