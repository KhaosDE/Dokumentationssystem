using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 33: Überleitungsbogen aus der Akte – Kopie in die Dokumente, Bericht, Statusmeldung, PDF, Vorschau.
[Collection("Oberflaeche")]
public class UeberleitungOberflaechenTests
{
    [Fact]
    public void Erstellen_und_als_PDF_speichern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var ahorn = u.Demo("Ahorn");
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(ahorn.Id!.Value);
        Assert.Contains(f.Kopf.Knoepfe, k => k.Text == "Überleitungsbogen …");
        using var fenster = new UeberleitungFenster(u.Speicher, ahorn);
        Assert.Equal(UeberleitungRegeln.AlleAbschnitte.Length, fenster.Abschnitte.CheckedItems.Count);   // alle vorbelegt
        fenster.Anlass.Text = "Einweisung zur Abklärung";
        Hilfe.Foto(fenster, "141_ueberleitung");
        string ziel = Path.Combine(u.Ordner, "bogen.pdf");
        fenster.PdfZiel = () => ziel;
        Assert.True(fenster.PdfSpeichern());
        Assert.True(File.ReadAllBytes(ziel).Length > 500);
        string? vorschau = null;
        fenster.Vorschau = (bauen, titel) => { using var d = bauen(); vorschau = titel; };
        Assert.True(fenster.Erstellen());
        Assert.Equal(UeberleitungRegeln.Titel(ahorn), vorschau);
        Assert.NotNull(fenster.Kopie);
        Assert.Single(u.Speicher.Ueberleitungen(ahorn.Id!.Value));
        Assert.Contains(u.Speicher.Berichte(ahorn.Id!.Value), b => b.Text.StartsWith("Überleitungsbogen erstellt: Einweisung zur Abklärung"));
        Assert.StartsWith("in der Klinik seit", u.Speicher.Statusmeldung(ahorn.Id!.Value, DateTime.Now)!.Text);
    });
}
