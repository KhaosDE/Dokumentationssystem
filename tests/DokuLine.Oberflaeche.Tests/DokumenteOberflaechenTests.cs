using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 31: Akte, Reiter „Dokumente“ – Kategorien, öffnen (Übergabe an das Standardprogramm), hinzufügen,
// Papierkorb, endgültig löschen nur mit Recht, Sprung aus dem Reiter „Kostenträger“.
[Collection("Oberflaeche")]
public class DokumenteOberflaechenTests
{
    [Fact]
    public void Ablage_in_der_Akte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Speicher.AlleBewohner().First(x => u.Speicher.Dokumente(x.Id!.Value).Count == 3);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(b.Id!.Value);
        var akte = f.Bewohner.Akte;
        akte.Reiter.Waehlen("Dokumente");
        var d = akte.Dokumente;
        Assert.True(d.Visible);
        Assert.Equal("Alle (3)", d.Kategorien.Items[0]);
        Assert.Equal("Papierkorb (0)", d.Kategorien.Items[^1]);
        Assert.Equal(3, d.Zeilen.Count);
        Hilfe.Foto(f, "139_akte_dokumente");

        string? geoeffnet = null;
        var vorher = Extern.Oeffnen;
        Extern.Oeffnen = a => geoeffnet = a;
        try
        {
            d.Liste.Items[0].Selected = true;
            Assert.NotNull(d.Oeffnen());
            Assert.True(File.Exists(geoeffnet));
            Assert.Contains(u.Speicher.DokumentProtokoll(b.Id!.Value), e => e.Aktion == DokumentAktion.Geoeffnet && e.Kuerzel == "EF");
        }
        finally { Extern.Oeffnen = vorher; }

        // Hinzufügen: Kategorie nach dem Dateinamen vorgeschlagen; das Original bleibt.
        string ordner = Path.Combine(u.Ordner, "eingang");
        Directory.CreateDirectory(ordner);
        string datei = Path.Combine(ordner, "Bescheid_Bezirk.pdf");
        File.WriteAllBytes(datei, DemoPdf.Erzeugen("Bescheid", ["ausgedacht"]));
        d.AngabenZeigen = fenster =>
        {
            Assert.Equal("Kostenträger und Bescheide", fenster.KategorieWahl.SelectedItem);
            Assert.Equal("Bescheid Bezirk", fenster.Titel.Text);
            return fenster.Speichern();
        };
        Assert.Equal(1, d.Hinzufuegen([datei]));
        Assert.True(File.Exists(datei));
        Assert.Equal(4, d.Zeilen.Count);

        // Papierkorb und endgültig löschen (EF darf es).
        d.Liste.Items[d.Zeilen.FindIndex(x => x.Titel == "Bescheid Bezirk")].Selected = true;
        d.GrundFragen = _ => "falsch abgelegt";
        Assert.True(d.Entfernen());
        Assert.Equal(3, d.Zeilen.Count);
        d.Kategorien.SelectedItem = "Papierkorb (1)";
        Assert.Single(d.Zeilen);
        d.Liste.Items[0].Selected = true;
        Assert.Null(Stil.Sperrgrund(d.LoeschenKnopf));
        Assert.True(d.Loeschen());
        Assert.Empty(d.Zeilen);

        // Aus dem Reiter „Kostenträger“: „Dokumente …“ führt zur Kategorie.
        akte.Reiter.Waehlen("Kostenträger");
        akte.Angaben.DokumenteKnopf.PerformClick();
        Assert.Equal("Dokumente", akte.Reiter.Aktiv);
        Assert.Equal("Kostenträger und Bescheide", d.Kategorie);
    });

    [Fact]
    public void Ohne_Recht_kein_Loeschen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Mitarbeiter Wohnbereich: ansehen und hinzufügen, nicht löschen
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Speicher.AlleBewohner().First(x => u.Speicher.Dokumente(x.Id!.Value).Count == 3);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(b.Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Dokumente");
        var d = f.Bewohner.Akte.Dokumente;
        Assert.Null(Stil.Sperrgrund(d.HinzufuegenKnopf));
        d.Liste.Items[0].Selected = true;
        d.GrundFragen = _ => "Test";
        Assert.True(d.Entfernen());
        d.Kategorien.SelectedItem = "Papierkorb (1)";
        d.Liste.Items[0].Selected = true;
        Assert.NotNull(Stil.Sperrgrund(d.LoeschenKnopf));
    });
}
