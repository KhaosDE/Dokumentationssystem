using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 45: Küchendienst oben im Tagesplan (aus dem Küchendienstplan des Moduls), bestätigen mit Kürzel und Uhrzeit,
// Marke „Küchendienst heute“ beim Bewohner; Untermodul aus: keine Zeile.
[Collection("Oberflaeche")]
public class KuechendienstTagesplanTests
{
    [Fact]
    public void Zeile_bestaetigen_und_abschalten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "AB" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Tagesplan");
        var teil = Assert.IsType<KuechendienstTagesplan>(Assert.Single(f.Tagesplan.Teile).Teil);
        Assert.True(teil.Visible);
        Assert.StartsWith("Küchendienst heute: ", teil.Zeile.Text);
        Assert.Equal("noch offen", teil.Stand.Text);
        Assert.Null(Stil.Sperrgrund(teil.GemachtKnopf));
        Hilfe.Foto(f, "176_kuechendienst_tagesplan");
        Assert.True(teil.Setzen(KuechendienstStatus.Gemacht));
        Assert.StartsWith("gemacht · AB ", teil.Stand.Text);
        teil.VertretungFragen = () => "Zi. 105 (Test)";
        Assert.True(teil.Setzen(KuechendienstStatus.Vertreten));
        Assert.StartsWith("vertreten durch Zi. 105 (Test) · AB ", teil.Stand.Text);

        // Beim Bewohner mit Dienst die Marke „Küchendienst heute“.
        var heute = teil.Heute!;
        var db = new Datenbank(u.Speicher);
        var mitDienst = db.Laden().FirstOrDefault(p => heute.Zimmer.Split('/').Contains(p.Zimmernummer) && p.Anwesend)
            ?? db.Laden().First(p => heute.Zimmer.Split('/').Contains(p.Zimmernummer));
        var b = u.Speicher.Bewohner(mitDienst.BewohnerId)!;
        Assert.Contains(Modulverwaltung.Marken(u.Speicher, b, DateTime.Now), m => m.Text is "Küchendienst heute" or Kuechendienst.Unbesetzt);

        u.Speicher.ModulSetzen(Kuechendienst.Schluessel, false);
        f.Tagesplan.Aktualisieren();
        Assert.False(teil.Visible);
        Assert.DoesNotContain(Modulverwaltung.Marken(u.Speicher, b, DateTime.Now), m => m.Text.StartsWith("Küchendienst", StringComparison.Ordinal));
    });
}
