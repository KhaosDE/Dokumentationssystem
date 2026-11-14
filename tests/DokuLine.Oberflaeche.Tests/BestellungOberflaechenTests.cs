using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 51: Seite „Bestellung“ – ohne Bestand von Hand ankreuzen, Anforderung je Empfänger, Ausdruck, E-Mail nur
// nach Hinweis zum Datenschutz, Verlauf bis erledigt.
[Collection("Oberflaeche")]
public class BestellungOberflaechenTests
{
    [Fact]
    public void Ankreuzen_Anforderung_Ausdruck_und_Verlauf() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen("R1");
        f.BereichWaehlen("Bestellung");
        var s = f.Bestellung;
        Assert.NotEmpty(s.Liste.Items);
        Assert.All(s.Liste.Items.Cast<ListViewItem>(), i => Assert.False(i.Checked));   // ohne Bestand: von Hand
        Assert.False(s.ZaehlenKnopf.Visible);
        s.Liste.Items[0].Checked = true;
        Hilfe.Foto(f, "181_bestellung");
        Assert.True(s.Erstellen());
        Assert.Single(s.Anforderungen.Items);
        s.Anforderungen.Items[0].Selected = true;
        string? titel = null;
        s.Vorschau = (_, t) => titel = t;
        Assert.True(s.Drucken());
        Assert.Equal("Rezeptanforderung", titel);
        string? hinweis = null;
        s.Bestaetigen = text => { hinweis = text; return false; };
        Assert.False(s.Email());
        Assert.StartsWith(BestellRegeln.DatenschutzHinweis, hinweis);
        s.Anforderungen.Items[0].Selected = true;
        Assert.True(s.Weiter());
        Assert.Equal(AnforderungStand.Bestaetigt, u.Speicher.Anforderungen().Single().Stand);
    });
}
