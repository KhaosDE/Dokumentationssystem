using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Medikamentendatenbank: Liste mit Suche, Demo-Hinweis, anlegen, ändern mit Verlauf, Quelle kopieren.
[Collection("Oberflaeche")]
public class MedikamenteOberflaechenTests
{
    // PR: Psychologischer Fachdienst, Fachkraft – darf anlegen und verwalten (Rechte, Schritt 19).
    private static (Umgebung, Hauptfenster) Oeffnen(string kuerzel = "PR")
    {
        var u = new Umgebung();
        if (kuerzel.Length > 0) Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Navigation.Single(n => n.Text == "Medikamente").Ausloesen();
        return (u, f);
    }

    [Fact]
    public void Liste_mit_Demo_Hinweis_und_Suche() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Medikamente;
            int alle = u.Speicher.Medikamente().Count;
            Assert.Equal($"{alle} Medikamente · Demo-Bestand", f.Kopf.KleinText);
            Assert.Equal("Demo-Bestand, nicht zum Verordnen verwenden", seite.Hinweis.Text);
            Assert.Equal("Neues Medikament …", Assert.Single(f.Kopf.Knoepfe).Text);
            Assert.Equal(alle, seite.Karten.Count);
            Assert.All(seite.Karten, k => Assert.Equal("ungeprüft", k.MarkenTexte[0]));
            string wirkstoff = seite.Karten[0].Medikament.Wirkstoff;
            seite.Suche.Text = wirkstoff;
            Assert.All(seite.Karten, k => Assert.Contains(wirkstoff, k.Medikament.Anzeige, StringComparison.CurrentCultureIgnoreCase));
            seite.Suche.Text = "gibt-es-nicht";
            Assert.Empty(seite.Karten);
            Assert.Equal($"{alle} Medikamente · Demo-Bestand", f.Kopf.KleinText);   // zählt immer alle

            string? kopiert = null;
            seite.Kopieren = text => kopiert = text;
            seite.Suche.Text = "";
            seite.Karten[0].KopierenKnopf.PerformClick();
            Assert.StartsWith("https://", kopiert);
        }
    });

    [Fact]
    public void Anlegen_und_aendern_mit_Verlauf() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Medikamente;
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<MedikamentFenster>(fenster);
                Assert.False(neu.Speichern());
                Assert.Contains("Wirkstoff fehlt.", neu.Fehler.Text);
                neu.Wirkstoff.Text = "Beispielin";
                neu.Darreichungsform.Text = "Tablette";
                neu.Staerken.Text = "1 mg";
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                f.Kopf.Knoepfe[0].PerformClick();
            MedikamentKarte Karte() => seite.Karten.Single(k => k.Medikament.Wirkstoff == "Beispielin");
            Assert.Contains("selbst angelegt", Karte().MarkenTexte);
            Assert.Contains("Stärken: keine Angabe", new MedikamentKarte(Karte().Medikament with
            {
                Fassungen = [Karte().Medikament.Aktuell with { Staerken = [] }],
            }).MarkenTexte);
            Assert.False(Karte().VerlaufKnopf.Visible);

            using (new DialogErsatz(fenster =>
            {
                var aendern = Assert.IsType<MedikamentFenster>(fenster);
                Assert.Equal("Beispielin", aendern.Wirkstoff.Text);
                aendern.Staerken.Text = "1 mg, 2 mg";
                return aendern.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().AendernKnopf.PerformClick();
            Assert.Contains("geändert (2 Fassungen)", Karte().MarkenTexte);
            Assert.True(Karte().VerlaufKnopf.Visible);

            string? verlauf = null;
            using (new DialogErsatz(fenster =>
            {
                verlauf = Assert.IsType<MedikamentVerlaufFenster>(fenster).Inhalt.Text;
                return DialogResult.Cancel;
            }))
                Karte().VerlaufKnopf.PerformClick();
            Assert.Contains("Fassung 2", verlauf);
            Assert.Contains("gilt jetzt", verlauf);
            Assert.Contains("Stärken: „1 mg“ → „1 mg, 2 mg“", verlauf);
            Assert.Contains("von PR", verlauf);
        }
    });

    [Fact]
    public void Ohne_Anmeldung_kein_Speichern() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen(kuerzel: "");
        using (u)
        using (f)
        {
            using var fenster = new MedikamentFenster(u.Speicher, null);
            fenster.Wirkstoff.Text = "Beispielin";
            Assert.False(fenster.Speichern());
            Assert.Contains("Kürzel fehlt", fenster.Fehler.Text);
        }
    });
}
