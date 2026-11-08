using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Nachrichten: „Angemeldet als …“, Zähler, Hinweis über den Karten, Ordner, Verlauf öffnen,
// antworten, archivieren, neue Nachricht mit Bewohnerbezug aus der Akte.
[Collection("Oberflaeche")]
public class NachrichtenOberflaechenTests
{
    private static SeitenleistenEintrag Eintrag(Hauptfenster f) => f.Navigation.Single(n => n.Text == "Nachrichten");

    [Fact]
    public void Angemeldet_als_im_Kopf_mit_Zaehler_und_Hinweis() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.Equal("Nicht angemeldet – Anmelden ▾", f.Kopf.BenutzerText);
        Assert.Null(Eintrag(f).Anzahl);
        Assert.False(f.Bewohner.NachrichtenHinweisSichtbar);

        f.BenutzerWaehlen("AB");
        Assert.Equal("Angemeldet als Anna Bachstelze (AB) ▾", f.Kopf.BenutzerText);
        Assert.Equal("AB", Einstellungen.Laden().Kuerzel);   // gilt auch für Berichte, Übergabe, Aufgaben
        Assert.Equal(3, Eintrag(f).Anzahl);
        Assert.True(f.Bewohner.NachrichtenHinweisSichtbar);
        Assert.Equal("3 ungelesene Nachrichten für Anna Bachstelze", f.Bewohner.Nachrichten.Text2);
        f.Bewohner.Nachrichten.OeffnenKnopf.PerformClick();
        Assert.Equal("Nachrichten", f.AktiverBereich);
        Assert.Equal("Anna Bachstelze · 3 ungelesen", f.Kopf.KleinText);

        f.BenutzerWaehlen("OP");
        Assert.Equal("Angemeldet als Olga Pirol (OP) ▾", f.Kopf.BenutzerText);
        Assert.Equal("Nachrichten", f.AktiverBereich);
        Assert.Empty(f.Nachrichten.Karten);                                        // OP hat nichts bekommen
        Assert.Equal(1, f.Nachrichten.Ordner[NachrichtenOrdner.Gesendet].Anzahl);   // nur „Schwimmkurs“ geschrieben
        f.BenutzerWaehlen("");
        Assert.Equal("Nicht angemeldet – Anmelden ▾", f.Kopf.BenutzerText);
        Assert.True(f.Nachrichten.Hinweis.Visible);
        Assert.False(f.Nachrichten.NeuKnopf.Enabled);
        Assert.Empty(f.Nachrichten.Karten);
    });

    [Fact]
    public void Posteingang_Gesendet_Archiv() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("AB");
        f.Navigation.Single(n => n.Text == "Nachrichten").Ausloesen();
        var seite = f.Nachrichten;
        Assert.Equal([3, 2, 1], seite.Ordner.Values.Select(k => k.Anzahl));
        Assert.True(seite.Ordner[NachrichtenOrdner.Posteingang].Aktiv);
        Assert.Equal(["Arzttermin am Donnerstag", "Schwimmkurs", "Teamsitzung am Mittwoch"], seite.Karten.Select(k => k.Verlauf.Betreff));
        Assert.Equal(["2 neu", "Bewohner: Anton Ahorn", "2 Nachrichten"], seite.Karten[0].MarkenTexte);
        Assert.StartsWith("Karin Lerche: Ich habe Frühdienst", seite.Karten[0].Vorschau);
        Assert.Equal("Neue Nachricht …", Assert.Single(f.Kopf.Knoepfe).Text);

        seite.Ordner[NachrichtenOrdner.Gesendet].Ausloesen();
        Assert.Equal(["Ersatzschlüssel Fahrradkeller", "Dienst am Samstag tauschen?"], seite.Karten.Select(k => k.Verlauf.Betreff));
        seite.Ordner[NachrichtenOrdner.Archiv].Ausloesen();
        Assert.Equal(["Dienst am Samstag tauschen?"], seite.Karten.Select(k => k.Verlauf.Betreff));
    });

    [Fact]
    public void Verlauf_oeffnen_antworten_archivieren() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("AB");
        f.Navigation.Single(n => n.Text == "Nachrichten").Ausloesen();
        var seite = f.Nachrichten;
        long id = seite.Karten[0].Verlauf.Id;
        seite.Karten[0].Waehlen();
        Assert.NotNull(seite.Offen);
        Assert.Equal(2, seite.Offen!.Texte.Count);
        Assert.Equal(1, Eintrag(f).Anzahl);                       // gelesen beim Öffnen
        Assert.Equal("Anna Bachstelze · 1 ungelesen", f.Kopf.KleinText);

        using (new DialogErsatz(fenster =>
        {
            var antwort = Assert.IsType<NachrichtFenster>(fenster);
            Assert.Equal([Empfaenger.Person("EF"), Empfaenger.Team("Wohnbereich A"), Empfaenger.Person("KL")], antwort.AntwortAn);
            antwort.Inhalt.Text = "Danke, Karin! Die Unterlagen liegen im Büro.";
            return antwort.Senden() ? DialogResult.OK : DialogResult.Cancel;
        }))
            seite.Offen.AntwortenKnopf.PerformClick();
        Assert.Equal(3, seite.Offen!.Texte.Count);
        Assert.Equal("Danke, Karin! Die Unterlagen liegen im Büro.", seite.Offen.Texte[^1]);

        seite.Offen.ArchivKnopf.PerformClick();
        Assert.Null(seite.Offen);
        Assert.DoesNotContain(seite.Karten, k => k.Verlauf.Id == id);
        seite.Ordner[NachrichtenOrdner.Archiv].Ausloesen();
        seite.Karten.Single(k => k.Verlauf.Id == id).Waehlen();
        Assert.Equal("Zurück in den Posteingang", seite.Offen!.ArchivKnopf.Text);
        seite.Offen.ArchivKnopf.PerformClick();
        seite.Ordner[NachrichtenOrdner.Posteingang].Ausloesen();
        Assert.Contains(seite.Karten, k => k.Verlauf.Id == id);
    });

    [Fact]
    public void Aus_der_Akte_Nachricht_mit_Bewohnerbezug_an_ein_Team() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("CD");
        var clematis = u.Demo("Clematis");
        f.Bewohner.AkteZeigen(clematis.Id!.Value);
        f.Kopf.Knoepfe.Single(k => k.Text == "Nachrichten …").PerformClick();
        var seite = f.Nachrichten;
        Assert.Equal("Nachrichten", f.AktiverBereich);
        Assert.Equal(clematis.Id, seite.BewohnerFilter);
        Assert.Equal("Bewohner: Clara Clematis  ✕", seite.BewohnerFilterKnopf.Text);
        Assert.Empty(seite.Karten);   // CD ist am „Schwimmkurs“ (OP an AB) nicht beteiligt

        string? fehler = null;
        using (new DialogErsatz(fenster =>
        {
            var neu = Assert.IsType<NachrichtFenster>(fenster);
            Assert.StartsWith("Clematis, Clara", neu.BewohnerWahl.Text);
            Assert.False(neu.Senden());
            fehler = neu.Fehler.Text;
            neu.Ankreuzen("Team Therapie");
            neu.Betreff.Text = "Begleitung zum Schwimmkurs";
            neu.Inhalt.Text = "Ich kann Clara am Dienstag zum Schwimmkurs begleiten.";
            return neu.Senden() ? DialogResult.OK : DialogResult.Cancel;
        }))
            f.Kopf.Knoepfe.Single(k => k.Text == "Neue Nachricht …").PerformClick();
        Assert.Contains("An wen?", fehler);
        Assert.Equal("Begleitung zum Schwimmkurs", seite.Offen!.Verlauf.Betreff);
        Assert.Equal(NachrichtenOrdner.Gesendet, seite.AktiverOrdner);

        f.BenutzerWaehlen("OP");   // Team Therapie
        Assert.Equal(1, Eintrag(f).Anzahl);   // die neue Nachricht an das Team Therapie
        seite.BewohnerFilterKnopf.PerformClick();
        Assert.Null(seite.BewohnerFilter);
    });

    [Fact]
    public void Demo_zuruecksetzen_stellt_Nachrichten_wieder_her() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        f.BenutzerWaehlen("AB");
        var arzt = u.Speicher.Verlaeufe("AB", NachrichtenOrdner.Posteingang)[0];
        u.Speicher.VerlaufGelesen("AB", arzt.Id, DateTime.Now);
        Assert.Equal(1, Eintrag(f).Anzahl);
        Assert.True(f.DemoZuruecksetzen(nachfragen: false));
        Assert.Equal(3, Eintrag(f).Anzahl);
    });
}
