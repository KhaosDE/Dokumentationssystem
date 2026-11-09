using DokuLine.Kern;
using DokuLine.Tests;

namespace DokuLine.Oberflaeche.Tests;

// Sicherheitsnetz für Updates (0.33.1), Oberfläche: Nach einem Update mit einer Datenbank, die der Nutzer
// verändert hat (Positionen und Bereiche umbenannt, Ausgeschiedene, entfernte Bewohner) und in der alle
// einmaligen Schritte nachgeholt werden, öffnet sich jeder Bereich der Seitenleiste und jeder Reiter der
// Akte ohne Fehler – als Administrator und als Bereichsleitung. Die Liste der Bereiche kommt aus der
// Seitenleiste: Ein künftiger Bereich ist automatisch mitgeprüft.
[Collection("Oberflaeche")]
public class UpdateSicherheitOberflaechenTests
{
    [Fact]
    public void Alle_Bereiche_oeffnen_nach_Update_mit_veraenderter_Datenbank() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        NutzerStand.Veraendern(u.Speicher);
        NutzerStand.MerkerEntfernen(u.Speicher.Verbindung,
            NutzerStand.Merker(u.Speicher.Verbindung).Select(m => m.Schluessel).Where(k => !NutzerStand.NeuLaden.Contains(k)).ToList());

        using var s = new Speicher(u.Speicher.Pfad);   // Neustart nach dem Update
        Modulverwaltung.Anmelden(s);
        s.DemoBeimErstenStart();
        var bewohner = s.AlleBewohner()[0];
        foreach (var kuerzel in new[] { "TST", "EF" })
        {
            Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
            using var f = new Hauptfenster(s);
            Hilfe.Zeigen(f);
            var bereiche = f.Navigation.Select(n => n.Text).ToList();
            Assert.Contains("Übersicht", bereiche);
            foreach (var name in bereiche)
            {
                f.BereichWaehlen(name);
                Application.DoEvents();
            }
            f.BereichWaehlen("Bewohner");
            f.Bewohner.AkteZeigen(bewohner.Id!.Value);
            foreach (var reiter in Bauplan.AktenReiter)
            {
                f.Bewohner.Akte.Reiter.Waehlen(reiter);
                Application.DoEvents();
            }
        }
    });
}
