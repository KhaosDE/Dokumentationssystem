using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 42: Bereich „Medikamentenausgabe“ – Einnahmezeit, Liste, ein Klick, einzeln, Recht, Übersicht, Verlauf.
[Collection("Oberflaeche")]
public class AusgabeOberflaechenTests
{
    [Fact]
    public void Ausgabe_abhaken_einzeln_Verlauf_und_Uebersicht() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });   // Assistenzkraft: ohne Recht
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.True(f.Ausgeblendet("Medikamentenausgabe"));
        f.BenutzerWaehlen("AB");                                // Fachkraft
        Assert.False(f.Ausgeblendet("Medikamentenausgabe"));
        f.ArbeitsbereichWaehlen("R1");
        f.BereichWaehlen("Medikamentenausgabe");
        var a = f.Ausgabe;
        Assert.Equal(AusgabeRegeln.Jetzt(DateTime.Now).Zeit, a.Zeit);   // vorgewählt: die laufende Einnahmezeit
        // Die Demo lässt die laufende und alle späteren Einnahmezeiten von heute offen; die erste davon mit Medikamenten.
        var r1 = u.Speicher.AlleBewohner().Where(b => b.Wohnbereich == "R1").ToList();
        var zeit = AusgabeRegeln.Zeiten.First(z => u.Speicher.Ausgabeliste(r1, a.Ausgabetag, z).Any(x => x.NochOffen));
        a.ZeitWaehlen(zeit);
        Assert.True(a.ZeitKnoepfe.Single(k => k.Text == AusgabeRegeln.ZeitText(zeit)).Aktiv);
        Assert.All(a.Zeilen, z => Assert.Equal("R1", z.Bewohner.Wohnbereich));
        Assert.Contains(" ausgegeben", a.Zaehler.Text);
        Hilfe.Foto(f, "160_medikamentenausgabe");

        var offen = a.Zeilen.First(z => z.NochOffen);
        {
            a.Liste.Items.Cast<ListViewItem>().Single(i => ReferenceEquals(i.Tag, offen)).Selected = true;
            Assert.True(a.Setzen(AusgabeStatus.NichtAbgeholt));
            var neu = a.Zeilen.First(z => z.Bewohner.Id == offen.Bewohner.Id);
            Assert.True(neu.NichtAbgeholt);
            Assert.Same(neu, a.Zeilen[0]);   // „nicht abgeholt“ oben
            if (AusgabeRegeln.Jetzt(DateTime.Now).Zeit == zeit)
            {
                f.BereichWaehlen("Übersicht");
                Assert.True(f.Uebersicht.NichtAbgeholt >= 1);
                f.BereichWaehlen("Medikamentenausgabe");
                a.ZeitWaehlen(zeit);
            }
            a.Liste.Items.Cast<ListViewItem>().Single(i => i.Tag is AusgabeZeile z && z.Bewohner.Id == offen.Bewohner.Id).Selected = true;
            using (new DialogErsatz(fenster =>
            {
                var einzeln = Assert.IsType<AusgabeEinzelnFenster>(fenster);
                foreach (var c in einzeln.Medikamente.Skip(1)) c.Checked = false;
                einzeln.Medikamente[0].Checked = true;
                einzeln.StatusWahl.SelectedIndex = (int)AusgabeStatus.Ausgegeben;
                return DialogResult.OK;
            }))
                Assert.True(a.Einzeln());
            Assert.Contains(u.Speicher.AusgabeEintraege(a.Ausgabetag, a.Ausgabetag, offen.Bewohner.Id), e => e.Status == AusgabeStatus.Ausgegeben && e.Kuerzel == "AB");

            // Verlauf im Medikamentenplan.
            f.BereichWaehlen("Dokumentation");
            f.Bewohner.AkteZeigen(offen.Bewohner.Id!.Value);
            f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
            Assert.Contains("nicht abgeholt (AB", f.Bewohner.Akte.Medikation.AusgabeText());
        }
    });
}
