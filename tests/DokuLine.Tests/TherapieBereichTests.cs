using DokuLine.Kern;

namespace DokuLine.Tests;

// Bereich „Therapie“ (Bauplan 29n): wer in der Bewohnerleiste steht, Gruppen je Wohnbereich, Berichtsart
// „Therapie“ mit dem Namen des Angebots, Demo für das Team Therapie.
[Collection("Datenordner")]
public sealed class TherapieBereichTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private Speicher s;

    public TherapieBereichTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private void Sql(string text)
    {
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = text;
        befehl.ExecuteNonQuery();
    }

    private static Bewohner B(long id, string nachname, string vorname, string bereich) =>
        new() { Id = id, Nachname = nachname, Vorname = vorname, Wohnbereich = bereich, Zimmer = "1", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };

    [Fact]
    public void Bewohnerleiste_nur_mit_Angebot_der_Therapie()
    {
        var alle = s.AlleBewohner();
        var mit = TherapieRegeln.MitTherapie(alle, s.Angebote(), Heute);
        Assert.NotEmpty(mit);
        Assert.True(mit.Count < alle.Count);
        var therapie = s.Angebote().Where(a => a.Anbieter == TherapieRegeln.AnbieterTherapie).ToList();
        Assert.All(mit, b => Assert.Contains(therapie, a => a.TeilnehmerAm(Heute).Any(z => z.BewohnerId == b.Id)));
        // Ein Angebot eines Wohnbereichs reicht nicht.
        var nurWohnbereich = s.Angebote().Where(a => a.Anbieter != TherapieRegeln.AnbieterTherapie)
            .SelectMany(a => a.TeilnehmerAm(Heute)).Select(z => z.BewohnerId)
            .Where(id => therapie.All(a => a.TeilnehmerAm(Heute).All(z => z.BewohnerId != id))).ToHashSet();
        Assert.DoesNotContain(mit, b => nurWohnbereich.Contains(b.Id!.Value));
    }

    [Fact]
    public void Gruppen_je_Wohnbereich_in_der_Reihenfolge_der_Bereiche()
    {
        var liste = new[] { B(1, "Zeder", "Ute", "R2"), B(2, "Ahorn", "Lia", "R3"), B(3, "Birke", "Ole", "R2"), B(4, "Esche", "Kai", "R1") };
        var gruppen = TherapieRegeln.NachBereich(liste, ["R1", "R2", "R3", "Therapie"]);
        Assert.Equal(["R1", "R2", "R3"], gruppen.Select(g => g.Bereich));
        Assert.Equal(["Birke", "Zeder"], gruppen[1].Bewohner.Select(b => b.Nachname));
        // Ein Bereich, den es in der Liste nicht (mehr) gibt, steht hinten.
        Assert.Equal(["R1", "R2", "Alt"], TherapieRegeln.NachBereich([B(5, "A", "B", "Alt"), .. liste.Take(1), liste[3]], ["R1", "R2"])
            .Select(g => g.Bereich));
    }

    [Fact]
    public void Bericht_der_Therapie_mit_Angebot()
    {
        Assert.Equal("Therapie Holzwerkstatt: War konzentriert.", TherapieRegeln.BerichtText("Holzwerkstatt", "War konzentriert."));
        Assert.Equal("Ohne Angebot.", TherapieRegeln.BerichtText("", "Ohne Angebot."));
        Assert.Contains(DokuRegeln.ArtTherapie, s.Berichtsarten());
        var mit = TherapieRegeln.MitTherapie(s.AlleBewohner(), s.Angebote(), Heute)[0];
        var angebote = TherapieRegeln.AngeboteVon(mit.Id!.Value, s.Angebote(), Heute);
        Assert.NotEmpty(angebote);
        Assert.All(angebote, n => Assert.Contains(s.Angebote(), a => a.Anbieter == TherapieRegeln.AnbieterTherapie && a.Am(Heute).Name == n));
        // Demo: Therapie-Berichte bei mehreren Bewohnern, mit dem Namen des Angebots am Anfang.
        var berichte = s.AlleBewohner().SelectMany(b => s.Berichte(b.Id!.Value)).Where(r => r.Art == DokuRegeln.ArtTherapie).ToList();
        Assert.True(berichte.Select(r => r.BewohnerId).Distinct().Count() >= 3);
        Assert.All(berichte, r => Assert.StartsWith("Therapie ", r.Text));
    }

    [Fact]
    public void Demo_fuer_das_Team_Therapie()
    {
        var therapie = s.AlleMitarbeiter().Where(m => m.Positionen.Contains("Therapie") && m.Aktiv).ToList();
        Assert.True(therapie.Count >= 2);
        Assert.Contains("Therapie", s.Teams());
        var aufgaben = s.Aufgaben(Jetzt, bereich: TherapieRegeln.AnbieterTherapie);
        Assert.Equal(2, aufgaben.Count);
        Assert.All(aufgaben, a => Assert.NotEqual(AufgabeFuer.Schicht, a.Fuer));   // die Therapie arbeitet tagsüber
        Assert.Contains(s.Verlaeufe("TH", NachrichtenOrdner.Gesendet), v => v.Betreff == "Material Holzwerkstatt");
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_die_Berichtsart_einmal()
    {
        Sql("DELETE FROM berichte WHERE art = 'Therapie'; DELETE FROM berichtsarten WHERE name = 'Therapie'; PRAGMA user_version = 33");
        string pfad = s.Pfad;
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Single(s.Berichtsarten(), a => a == DokuRegeln.ArtTherapie);
        Assert.Equal(DokuRegeln.ArtTherapie, s.Listeneintraege(Liste.Berichtsarten)[^1].Name);   // hinten angefügt
        Sql("PRAGMA user_version = 33");
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Single(s.Berichtsarten(), a => a == DokuRegeln.ArtTherapie);
    }
}
