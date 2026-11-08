using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Startseite „Übersicht“: Bereich „Heute“, Karten mit Marken des Tages, Klick öffnet die Akte,
// Neuaufbau nach Änderungen, Frist in den Einstellungen.
[Collection("Oberflaeche")]
public class UebersichtOberflaechenTests
{
    [Fact]
    public void Heute_Karten_und_Akte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.Equal("Übersicht", Hauptfenster.Startbereich);
        f.BereichWaehlen(Hauptfenster.Startbereich);
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        Assert.Empty(f.Kopf.Knoepfe);
        Assert.StartsWith("8 Bewohner · Wohnbereich A · Stand ", f.Kopf.KleinText);   // eigener Bereich (Mehrplatz)
        Assert.StartsWith("8 Bewohner · ", s.HeuteText.Text);
        Assert.Contains("Pause beginnt", s.Medikation.Text);
        Assert.Equal(8, s.Karten.Count);
        var holunder = s.Karten.Single(k => k.Bewohner.Nachname == "Holunder");
        Assert.Single(holunder.Marken, m => m.Art == MarkenArt.Abwesend);   // nur einmal, obwohl Kalender und Modul es melden
        Assert.Contains(s.Karten.Single(k => k.Bewohner.Nachname == "Fichte").Marken, m => m.Text == "Medikation: Pause beginnt");

        s.Karten.Single(k => k.Bewohner.Nachname == "Birke").Waehlen();
        Assert.Equal("Bewohner", f.AktiverBereich);
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Birke", f.Bewohner.Akte.Bewohner!.Nachname);
    });

    [Fact]
    public void Frist_einstellen_und_neu_aufbauen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        bool Bald() => s.Karten.Single(k => k.Bewohner.Nachname == "Clematis").Marken.Any(m => m.Text.StartsWith("Beschluss endet in"));
        Assert.True(Bald());
        f.EinstellungenZeigen = fenster =>
        {
            Assert.Equal(90, fenster.BeschlussFrist.Value);
            fenster.BeschlussFrist.Value = 14;
            return fenster.Speichern();
        };
        Assert.True(f.EinstellungenOeffnen());
        f.BereichWaehlen("Übersicht");
        Assert.False(Bald());

        // Immer aktuell: eine Änderung im Kalender baut die Übersicht neu auf.
        Assert.DoesNotContain("Fußpflege (Test)", s.Termine.Text);
        long kiefer = u.Demo("Kiefer").Id!.Value;
        u.Speicher.KalenderAnlegen(KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Bewohner, Titel = "Fußpflege (Test)", BeginnDatum = Datum.Text(DateOnly.FromDateTime(DateTime.Today)),
            BeginnUhrzeit = "23:00", EndeUhrzeit = "23:30", Bewohner = [kiefer], Kuerzel = "AB",
        }, DateTime.Now));
        Assert.Contains("Fußpflege (Test)", s.Termine.Text);
        Assert.Contains("Karl Kiefer", s.Termine.Text);
    });
}
