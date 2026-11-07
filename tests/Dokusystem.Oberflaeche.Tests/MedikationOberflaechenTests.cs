using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Reiter „Medikation“: Was gilt heute, Pause, Stufenplan, Absetzen, neue Verordnung, Verlauf, Ausdruck.
[Collection("Oberflaeche")]
public class MedikationOberflaechenTests
{
    private static (Umgebung, Hauptfenster, MedikationAnsicht) Oeffnen(string nachname, string kuerzel = "CD")
    {
        var u = new Umgebung();
        if (kuerzel.Length > 0) Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikation");
        return (u, f, f.Bewohner.Akte.Medikation);
    }

    private static string Heute(int tage = 0) => Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(tage)));

    [Fact]
    public void Pausiertes_steht_sichtbar_da_und_gilt_danach_wieder() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Clematis");
        using (u)
        using (f)
        {
            Assert.Equal(Heute(), m.TagFeld.Text);
            Assert.Equal("Demo — nicht zum Stellen verwenden", m.Hinweis.Text);
            Assert.Equal(["REGELMÄSSIG", "BEI BEDARF"], m.Abschnitte);
            var karte = Assert.Single(m.Karten);
            Assert.StartsWith("pausiert bis ", karte.MarkenTexte[0]);
            Assert.StartsWith("pausiert – sonst", karte.DosisText);
            Assert.True(karte.PauseEndeKnopf.Visible);
            Assert.False(karte.PauseKnopf.Visible);
            m.TagFeld.Text = Heute(5);   // nach der Pause
            Assert.Equal("Grundverordnung", Assert.Single(m.Karten).MarkenTexte[0]);
            m.TagFeld.Text = "irgendwann";
            Assert.True(m.TagFehler.Visible);
            m.HeuteKnopf.PerformClick();
            Assert.Equal(Heute(), m.TagFeld.Text);
        }
    });

    [Fact]
    public void Stufenplan_Bedarf_und_Abgesetztes() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Birke");
        using (u)
        using (f)
        {
            var karte = Assert.Single(m.Karten);
            Assert.Equal("Stufe 2 von 3", karte.MarkenTexte[0]);
            Assert.StartsWith("nächste Stufe ab ", karte.MarkenTexte[1]);
            Assert.StartsWith("morgens 1 · mittags 0", karte.DosisText);
        }
        var (u2, f2, m2) = Oeffnen("Holunder");
        using (u2)
        using (f2)
        {
            Assert.Empty(m2.Karten);   // abgesetzt: nicht unter „Was gilt heute“
            m2.AbgesetzteZeigen.Checked = true;
            Assert.Contains("ABGESETZT", m2.Abschnitte);
            Assert.StartsWith("abgesetzt am ", Assert.Single(m2.Karten).MarkenTexte[0]);
        }
        var (u3, f3, m3) = Oeffnen("Distel");
        using (u3)
        using (f3)
        {
            var bedarf = Assert.Single(m3.Karten);
            Assert.True(bedarf.Stand.Verordnung.Bedarf);
            Assert.StartsWith("bei Bedarf: bei starker Unruhe", bedarf.DosisText);
            Assert.False(bedarf.StufenplanKnopf.Visible);
        }
    });

    [Fact]
    public void Neue_Verordnung_dann_Pause_und_Stufenplan() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Ahorn");
        using (u)
        using (f)
        {
            int vorher = m.Karten.Count;
            string? fehler = null;
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<VerordnungFenster>(fenster);
                neu.MedikamentWahl.SelectedIndex = neu.MedikamentWahl.Items.Cast<string>().ToList().FindIndex(x => x.StartsWith("Citalopram"));
                Assert.Equal("20 mg", neu.Staerke.Text);   // erste Stärke aus der Datenbank
                neu.Mengen[0].Text = "1";
                Assert.False(neu.Speichern());
                fehler = neu.Fehler.Text;
                neu.Arzt.Text = "Dr. Test (ausgedacht)";
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                m.NeuKnopf.PerformClick();
            Assert.Contains("Wer hat es angeordnet?", fehler);
            Assert.Equal(vorher + 1, m.Karten.Count);
            VerordnungKarte Karte() => m.Karten.Single(k => k.Stand.Verordnung.Medikament.StartsWith("Citalopram"));
            Assert.Equal("Grundverordnung", Karte().MarkenTexte[0]);

            using (new DialogErsatz(fenster =>
            {
                var pause = Assert.IsType<AenderungFenster>(fenster);
                pause.Bis.Text = Heute(2);
                pause.Arzt.Text = "Dr. Test (ausgedacht)";
                return pause.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().PauseKnopf.PerformClick();
            Assert.StartsWith("pausiert bis ", Karte().MarkenTexte[0]);

            m.TagFeld.Text = Heute(3);
            using (new DialogErsatz(fenster =>
            {
                var plan = Assert.IsType<AenderungFenster>(fenster);
                plan.StufeSetzen(0, Heute(3), "½", "0", "0", "0");
                plan.StufeSetzen(1, Heute(10), "0", "0", "0", "0");
                plan.Arzt.Text = "Dr. Test (ausgedacht)";
                return plan.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().StufenplanKnopf.PerformClick();
            Assert.Equal("Stufe 1 von 2", Karte().MarkenTexte[0]);
            Assert.EndsWith("0–0–0–0", Karte().MarkenTexte[1]);

            string? verlauf = null;
            using (new DialogErsatz(fenster =>
            {
                verlauf = Assert.IsType<TextFenster>(fenster).Inhalt.Text;
                return DialogResult.Cancel;
            }))
                Karte().VerlaufKnopf.PerformClick();
            Assert.Contains("Grundverordnung 1–0–0–0", verlauf);
            Assert.Contains("Pause bis", verlauf);
            Assert.Contains("(absetzen)", verlauf);
            Assert.Contains("eingetragen von CD", verlauf);
        }
    });

    [Fact]
    public void Ausdruck_Was_gilt_heute() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Ahorn");
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                m.DruckKnopf.PerformClick();
                Assert.StartsWith("Medikation Anton Ahorn", Assert.Single(ersatz.Titel));
            }
            using var bild = new Bitmap(800, 1130);
            using var g = Graphics.FromImage(bild);
            new MedikationDruck(u.Demo("Ahorn"), DateOnly.FromDateTime(DateTime.Today), m.Staende(DateOnly.FromDateTime(DateTime.Today))).SeiteZeichnen(g);
        }
    });
}
