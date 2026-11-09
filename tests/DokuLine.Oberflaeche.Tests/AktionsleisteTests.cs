using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26i: Die Knöpfe der Bereiche stehen in einer Aktionsleiste direkt unter der Kopfzeile
// (gleiche Seitenränder); die Kopfzeile bleibt im Aussehen gleich und wird um diese Zeile niedriger.
// Reihenfolge überall gleich: Rücksprung, Hauptaktion, übrige Aktionen; rechts „… drucken …“ und
// „Entwurf fortsetzen“. Ohne Aktion keine Leiste.
[Collection("Oberflaeche")]
public class AktionsleisteTests
{
    [Fact]
    public void Knoepfe_unter_der_Kopfzeile_in_fester_Reihenfolge() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var leiste = f.Kopf.Leiste;
        Assert.Equal(Stil.Px(f.Kopf, KopfBereich.GesamtHoehe), f.Kopf.Height);   // ohne Knopfzeile

        f.BereichWaehlen("Aufgaben");
        var neu = Assert.Single(f.Kopf.Knoepfe);
        Assert.Same(leiste, neu.Parent);
        Assert.True(leiste.Visible);
        Assert.True(leiste.Top >= f.Kopf.Bottom);                                      // direkt darunter
        Assert.Equal(Stil.Px(leiste, 16), neu.Left);                                   // Rand wie die Kopfzeile
        Hilfe.Foto(f, "110_aktionsleiste_aufgaben");

        f.BereichWaehlen("Tagesplan");
        Assert.Empty(f.Kopf.Knoepfe);
        Assert.False(leiste.Visible);                                                  // kein leerer Streifen

        // Akte: Rücksprung, Hauptaktion („Bearbeiten …“), dann die übrigen in ihrer Reihenfolge.
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        Assert.Equal(["← Dokumentation", "Bearbeiten …", "Berichte ansehen", "Nachrichten …", "Status …", "Ärztliche Informationen …",
            "Überleitungsbogen …", "Abgängigkeit …", "Neuer Bericht …"],
            f.Kopf.Knoepfe.OrderBy(k => k.Left).Select(k => k.Text));
        Hilfe.Foto(f, "111_aktionsleiste_akte");

        // „… drucken …“ steht rechts.
        f.BereichWaehlen("Ausgangsliste");
        var drucken = Assert.Single(f.Kopf.Knoepfe);
        Assert.Equal(leiste.Width - Stil.Px(leiste, 16), drucken.Right);
    });

    [Fact]
    public void Reihenfolge_nach_Art_des_Knopfs() => Hilfe.Sta(() =>
    {
        using var zurueck = Stil.NebenKnopf("← Dokumentation");
        using var neben = Stil.NebenKnopf("Status …");
        using var haupt = Stil.HauptKnopf("Neue Aufgabe …");
        using var drucken = Stil.NebenKnopf("Tagesliste drucken …");
        var (links, rechts) = AktionsLeiste.Reihenfolge([drucken, neben, haupt, zurueck]);
        Assert.Equal([zurueck, haupt, neben], links);
        Assert.Equal([drucken], rechts);
        Stil.Sperren(haupt, RechteRegeln.FehltText);   // gesperrt (grau) bleibt sie die Hauptaktion
        Assert.Equal(1, AktionsLeiste.Gruppe(haupt));
    });
}
