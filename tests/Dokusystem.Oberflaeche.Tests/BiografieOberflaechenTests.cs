using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Reiter „Biografie“: Karten je Abschnitt, fehlende Angaben, Bearbeiten mit Hinweis auf die
// Formulierung, Verlauf, „Als Fließtext kopieren“.
[Collection("Oberflaeche")]
public class BiografieOberflaechenTests
{
    private static (Umgebung, Hauptfenster, BiografieAnsicht) Oeffnen(string nachname)
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Biografie");
        return (u, f, f.Bewohner.Akte.Biografie);
    }

    [Fact]
    public void Abschnitte_fehlende_Angaben_und_Hinweis() => Hilfe.Sta(() =>
    {
        var (u, f, b) = Oeffnen("Distel");
        using (u)
        using (f)
        {
            Assert.True(b.Visible);
            Assert.False(f.Bewohner.Akte.ReiterPlatzhalter.Visible);
            Assert.Equal(6, b.Karten.Count);
            Assert.Equal("Keine Angabe – fehlt noch.", b.Karten[0].InhaltText);
            Assert.Contains("vom Bewohner angegeben, nicht überprüft", b.Karten[3].MarkenTexte);
            Assert.Contains(b.Karten[3].MarkenTexte, t => t.StartsWith("Hinweis: Formulierung"));
            Assert.Equal("Fehlende Angaben: Herkunft und Familie, Wohnen, Wichtige Lebensereignisse, Wünsche.", b.Fehlend.AccessibleName);
        }
    });

    [Fact]
    public void Bearbeiten_Verlauf_und_Kopieren() => Hilfe.Sta(() =>
    {
        var (u, f, b) = Oeffnen("Distel");
        using (u)
        using (f)
        {
            using (new DialogErsatz(fenster =>
            {
                var w = Assert.IsType<BiografieFenster>(fenster);
                Hilfe.Zeigen(w);   // Visible stimmt nur bei angezeigten Fenstern
                w.Inhalt.Text = "- Garten\n- Vögel";
                Assert.False(w.Speichern());
                Assert.True(w.Fehler.Visible);
                w.Inhalt.Text = "Er lebte lange in einer Wohngemeinschaft.";
                w.Angegeben.Checked = true;
                Assert.True(w.FormulierungHinweis.Visible);
                w.Inhalt.Text = "Er gibt an, dass er lange in einer Wohngemeinschaft gelebt hat.";
                Assert.False(w.FormulierungHinweis.Visible);
                return w.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                b.Karten[2].BearbeitenKnopf.PerformClick();
            Assert.StartsWith("Er gibt an", b.Karten[2].InhaltText);
            Assert.DoesNotContain("Wohnen", b.Fehlend.AccessibleName);

            using (var ersatz = new DialogErsatz(fenster =>
            {
                Assert.Contains("angelegt von CD", Assert.IsType<TextFenster>(fenster).Inhalt.Text);
                return DialogResult.Cancel;
            }))
            {
                b.Karten[2].VerlaufKnopf.PerformClick();
                Assert.Single(ersatz.Titel);
            }

            string kopiert = "";
            b.Kopieren = t => kopiert = t;
            b.KopierenKnopf.PerformClick();
            Assert.StartsWith("Biografie Dieter Distel", kopiert);
            Assert.True(b.Kopiert.Visible);
        }
    });
}
