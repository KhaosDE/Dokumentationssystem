using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26m: Therapie, Reiter „Laufende Therapien“ – Tagesleiste, Spalten je laufender Therapie,
// „Gleich“, „Noch offen“ mit „gegangen“/„abgesagt“ (derselbe Eintrag wie im Tagesplan), Rest des Tages,
// Bereichswahl mit Vorgabe, Hinweis ohne laufende Therapie.
[Collection("Oberflaeche")]
public class LaufendeTherapienOberflaechenTests
{
    private static DateTime Heute(int stunde, int minute) => DateTime.Today.AddHours(stunde).AddMinutes(minute);

    [Fact]
    public void Tafel_bestaetigen_und_Tagesleiste() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "OP" });   // Therapie
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.WindowState = FormWindowState.Maximized;
        Application.DoEvents();
        f.BereichWaehlen("Therapie");
        var t = f.Therapie;
        Assert.Equal(TherapieSeite.LaufendReiter, t.Reiter.Aktiv);   // erster Reiter
        Assert.Equal([TherapieSeite.LaufendReiter, TherapieSeite.AngeboteReiter], t.Reiter.Namen);
        var l = t.Laufend;
        Assert.True(l.Visible);
        f.ArbeitsbereichWaehlen("Therapie");   // die Therapie sieht alle Wohnbereiche
        Assert.Null(l.Bereich);

        t.Jetzt = () => Heute(10, 15);
        l.Aktualisieren();
        var tafel = l.Tafel!;
        var spaziergang = l.Spalten.Single(s => s.Titel == "Spaziergang am Vormittag");   // läuft 10:00–11:00, jeden Tag
        Assert.Equal(["Treffpunkt Eingang · BA", "bis 11:00 · 5 von 8"], spaziergang.KopfTexte);
        Assert.Equal(5, spaziergang.Kaertchen.Count);
        Assert.All(spaziergang.Kaertchen, k => Assert.Equal("noch offen", k.Status!.Text));
        Assert.Equal(tafel.Laufend.Count + 2, l.Spalten.Count);
        Assert.Equal(LaufendeTherapienAnsicht.GleichTitel, l.Spalten[^2].Titel);
        var offen = l.Spalten[^1];
        Assert.Equal(LaufendeTherapienAnsicht.NochOffenTitel, offen.Titel);
        Assert.Equal(tafel.NochOffen.Count, offen.Kaertchen.Count);
        Assert.Contains(offen.Kaertchen, k => k.Teilnehmer!.Bewohner.Nachname == "Tagetes");   // Morgenrunde vorbei, nicht bestätigt
        Assert.Contains("Alle Bereiche · ", f.Kopf.KleinText);
        Assert.Contains("noch offen", f.Kopf.KleinText);
        Assert.Contains("Küchenteam", l.Rest.Text);                                            // 11:00, später
        Assert.False(l.Hinweis.Visible);
        Hilfe.Foto(f, "116_laufende_therapien");

        // „gegangen“ am Kärtchen: derselbe Eintrag wie im Tagesplan, mit Kürzel.
        var raps = offen.Kaertchen.Single(k => k.Teilnehmer!.Bewohner.Nachname == "Raps");
        Assert.Equal(["gegangen", "abgesagt"], raps.Knoepfe.Select(k => k.Text));
        long zuteilung = raps.Teilnehmer!.ZuteilungId;
        raps.Knoepfe[0].PerformClick();
        Application.DoEvents();
        var eintrag = u.Speicher.TeilnahmeEintraege(DateOnly.FromDateTime(DateTime.Today))[zuteilung];
        Assert.Equal(Teilnahme.Teilgenommen, eintrag.Status);
        Assert.Equal("OP", eintrag.Kuerzel);
        Assert.Equal("gegangen", l.Spalten.Single(s => s.Titel == "Spaziergang am Vormittag").Kaertchen
            .Single(k => k.Teilnehmer!.Bewohner.Nachname == "Raps").Status!.Text);
        Assert.DoesNotContain(l.Spalten[^1].Kaertchen, k => k.Teilnehmer!.Bewohner.Nachname == "Raps");

        // Tagesleiste: ein Balken je Termin, der Strich „jetzt“ liegt im Spaziergang; Klick öffnet das Angebot.
        Assert.Equal(l.Tafel!.Heute.Count, l.Leiste.Felder.Count);
        var feld = l.Leiste.Felder.First(x => x.Termin.Name == "Spaziergang am Vormittag");
        Assert.Same(feld.Termin, l.Leiste.TerminAn(new Point(feld.Feld.X + 2, feld.Feld.Y + 2)));
        Assert.InRange(l.Leiste.JetztX, feld.Feld.Left, feld.Feld.Right);
        l.Leiste.Oeffnen(feld.Termin);
        Assert.Equal(TherapieSeite.AngeboteReiter, t.Reiter.Aktiv);
        Assert.Contains("20 Angebote · ", f.Kopf.KleinText);

        // Nur R2: nur Teilnehmer aus R2.
        t.Reiter.Waehlen(TherapieSeite.LaufendReiter);
        f.ArbeitsbereichWaehlen("R2");
        Assert.All(l.Spalten.SelectMany(s => s.Kaertchen).Where(k => k.Teilnehmer is not null),
            k => Assert.Equal("R2", k.Teilnehmer!.Bewohner.Wohnbereich));
        Assert.StartsWith("R2 · ", f.Kopf.KleinText);

        // Abends läuft nichts mehr.
        t.Jetzt = () => Heute(21, 30);
        l.Aktualisieren();
        Assert.True(l.Hinweis.Visible);
        Assert.Equal("Gerade läuft keine Therapie. Heute kommt keine mehr.", l.Hinweis.Text);
        Assert.Equal(2, l.Spalten.Count);   // nur „Gleich“ und „Noch offen“
    });

    [Fact]
    public void Wohnbereich_als_Vorgabe_und_ohne_Anmeldung_gesperrt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung R1
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Therapie");
        var t = f.Therapie;
        var l = t.Laufend;
        Assert.Equal("R1", l.Bereich);   // der Bereich des Rechners
        t.Jetzt = () => Heute(10, 15);
        l.Aktualisieren();
        Assert.All(l.Spalten.SelectMany(s => s.Kaertchen).Where(k => k.Teilnehmer is not null),
            k => Assert.Equal("R1", k.Teilnehmer!.Bewohner.Wohnbereich));
        Assert.All(l.Spalten[^1].Kaertchen.SelectMany(k => k.Knoepfe), k => Assert.Null(Stil.Sperrgrund(k)));

        f.BenutzerWaehlen("");
        f.BereichWaehlen("Therapie");
        l.Aktualisieren();
        var knoepfe = l.Spalten[^1].Kaertchen.SelectMany(k => k.Knoepfe).ToList();
        Assert.NotEmpty(knoepfe);
        Assert.All(knoepfe, k => Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(k)));
    });
}
