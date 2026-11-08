using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 23: Tagesplan unter „Übersicht“ – alle Termine von heute, „gegangen“ (derselbe
// Eintrag wie im Wochenplan) und „Wo müsste er sein?“ auf Karte und Akte.
[Collection("Oberflaeche")]
public class TagesplanOberflaechenTests
{
    private const string Name = "Testgruppe Tagesplan";

    // Eine ausgedachte Therapie heute, den ganzen Tag (damit der Test zu jeder Uhrzeit gleich läuft).
    private static long TherapieHeute(Umgebung u, string nachname)
    {
        var jetzt = DateTime.Now;
        var heute = DateOnly.FromDateTime(jetzt);
        var a = u.Speicher.AngebotAnlegen(TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = Name, Therapeut = "Test (ausgedacht)", Ort = "Raum 1", Termine = [(heute.DayOfWeek, "00:00", "23:59")],
            Hoechstens = "4", GiltAb = Datum.Text(heute), Kuerzel = "OP",
        }, jetzt));
        return u.Speicher.Zuteilen(a.Id, u.Demo(nachname).Id!.Value, heute, null, "OP", jetzt).Id;
    }

    [Fact]
    public void Gegangen_im_Tagesplan_und_Wo_muesste_er_sein() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        long zuteilung = TherapieHeute(u, "Ahorn");
        long ahorn = u.Demo("Ahorn").Id!.Value;
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.Equal("Tagesplan", f.Navigation[1].Text);   // direkt unter „Übersicht“
        f.Navigation.Single(n => n.Text == "Tagesplan").Ausloesen();
        Assert.Equal("Tagesplan", f.AktiverBereich);
        var t = f.Tagesplan;
        Assert.StartsWith("Heute, ", t.TagText.Text);
        TagesplanKarte Karte() => t.Karten.Single(k => k.Zeile.Termin.Titel == Name);
        Assert.Equal(["Therapie", TagesplanRegeln.NochOffen], Karte().MarkenTexte);
        Assert.Contains("noch offen", f.Kopf.KleinText);
        Assert.All(t.Karten.Where(k => !k.Zeile.Therapie), k => Assert.False(k.GegangenKnopf.Visible));   // nur Therapien
        Hilfe.Foto(f, "75_tagesplan");

        Karte().GegangenKnopf.PerformClick();
        Assert.StartsWith("gegangen ", Karte().MarkenTexte[1]);
        Assert.EndsWith("· CD", Karte().MarkenTexte[1]);
        // Derselbe Eintrag wie im Wochenplan.
        var heute = DateOnly.FromDateTime(DateTime.Now);
        Assert.Equal(Teilnahme.Teilgenommen, u.Speicher.Woche(ahorn, WochenplanRegeln.Montag(heute)).Single(x => x.ZuteilungId == zuteilung && x.Tag == heute).Teilnahme);
        Karte().GegangenKnopf.PerformClick();   // noch einmal: kein zweiter Eintrag
        Assert.Single(u.Speicher.TeilnahmeEintraege(heute), e => e.Key == zuteilung);

        // Wo müsste er sein? Karte und Akte zeigen „in Therapie: … bis 23:59“.
        string wo = TagesplanRegeln.InTherapieText(Name, new TimeOnly(23, 59));
        f.BereichWaehlen("Bewohner");
        Assert.Contains(f.Bewohner.Raster.Controls.OfType<BewohnerKarte>().Single(k => k.Bewohner.Id == ahorn).Marken, m => m.Text == wo);
        f.Bewohner.AkteZeigen(ahorn);
        Assert.Equal(wo, f.Bewohner.Akte.WoText);
        f.BereichWaehlen("Übersicht");
        Assert.Contains(f.Uebersicht.Karten.Single(k => k.Bewohner.Id == ahorn).Marken, m => m.Text == wo);

        // Nach Bewohner, abgesagt, und ohne Anmeldung gesperrt.
        f.BereichWaehlen("Tagesplan");
        t.NachBewohner.Checked = true;
        Assert.NotEmpty(t.Gruppen);
        Assert.Equal(t.Gruppen.Count, t.Zeilen.Select(z => z.Bewohner.Id).Distinct().Count());
        Hilfe.Foto(f, "76_tagesplan_nach_bewohner");
        Karte().AbgesagtKnopf.PerformClick();
        Assert.StartsWith("abgesagt ", Karte().MarkenTexte[1]);
        Assert.Empty(u.Speicher.InTherapie(DateTime.Now));
        f.BenutzerWaehlen("");
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(Karte().GegangenKnopf));
        Assert.False(t.Setzen(Karte().Zeile, Teilnahme.Teilgenommen));
    });

    [Fact]
    public void Eigener_Bereich_oder_alle_Bereiche() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "GH" });   // Bereichsleitung Wohnbereich B
        TherapieHeute(u, "Ahorn");                               // Wohnbereich A
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Tagesplan");
        var t = f.Tagesplan;
        Assert.All(t.Zeilen, z => Assert.Equal("Wohnbereich B", z.Bewohner.Wohnbereich));
        Assert.DoesNotContain(t.Karten, k => k.Zeile.Termin.Titel == Name);
        Assert.Contains("Wohnbereich B", f.Kopf.KleinText);
        t.AlleBereiche.Checked = true;
        Assert.Contains(t.Karten, k => k.Zeile.Termin.Titel == Name);
        Assert.Contains("alle Bereiche", f.Kopf.KleinText);
    });
}
