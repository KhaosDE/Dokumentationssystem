using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26e: Wohnbereiche R1–R3 (offen/beschützend), Zimmer und Betten, freie Betten.
[Collection("Datenordner")]
public sealed class ZimmerTests : IDisposable
{
    private readonly Datenordner daten = new();
    private readonly Speicher s = new();

    public ZimmerTests() => s.DemoBeimErstenStart();

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Bewohner Demo(string nachname) => s.AlleBewohner().Single(b => b.Nachname == nachname);

    [Fact]
    public void Demo_Bereiche_Zimmer_und_freie_Betten()
    {
        Assert.Equal(["R1 (offen)", "R2 (beschützend)", "R3 (beschützend)"], s.Wohnbereiche().Select(w => w.Anzeige));
        Assert.Equal(18, s.Zimmerliste().Count);
        Assert.All(s.Wohnbereiche(), w => Assert.Equal(6, s.Zimmerliste(w.Name).Count));
        Assert.Equal(["105 – Bett Tür", "106 – Bett Tür"], s.FreieBetten("R1").Select(b => b.Anzeige));
        Assert.Equal(7, s.FreieBetten("R2").Count);
        Assert.Equal(9, s.FreieBetten("R3").Count);
        Assert.Contains("204 – Einzelzimmer", s.FreieBetten("R2").Select(b => b.Anzeige));
        // Wer umzieht, sieht sein eigenes Bett als frei.
        var ahorn = Demo("Ahorn");
        Assert.Contains(s.FreieBetten("R1", ahorn.Id), b => b.Zimmer == "101" && b.Bett == ZimmerRegeln.Fenster);
        Assert.Equal("Bett Fenster", ZimmerRegeln.BettText(ahorn.Bett));
    }

    [Fact]
    public void Belegtes_Bett_wird_abgelehnt_freies_genommen()
    {
        var birke = Demo("Birke");
        var fehler = Assert.Throws<Eingabefehler>(() => s.BewohnerSpeichern(birke with { Zimmer = "101", Bett = ZimmerRegeln.Fenster })).Fehler;
        Assert.Contains("schon belegt (Anton Ahorn)", Assert.Single(fehler));
        Assert.Throws<Eingabefehler>(() => s.BewohnerSpeichern(birke with { Zimmer = "102", Bett = ZimmerRegeln.Tuer }));   // Einzelzimmer hat kein „Tür“
        var umgezogen = s.BewohnerSpeichern(birke with { Zimmer = "301", Bett = ZimmerRegeln.Einzel });
        Assert.Equal("R3", umgezogen.Wohnbereich);   // der Bereich folgt dem Zimmer
        Assert.Contains("102 – Einzelzimmer", s.FreieBetten("R1").Select(b => b.Anzeige));   // altes Bett wieder frei
        Assert.DoesNotContain("301 – Einzelzimmer", s.FreieBetten("R3").Select(b => b.Anzeige));
    }

    [Fact]
    public void Zimmer_anlegen_und_aendern()
    {
        s.ZimmerSpeichern(new Zimmer("307", "R3", 2), neu: true);
        Assert.Equal(11, s.FreieBetten("R3").Count);
        Assert.Contains("gibt es schon", Assert.Throws<Eingabefehler>(() => s.ZimmerSpeichern(new Zimmer("307", "R3", 1), neu: true)).Message);
        Assert.Contains("Wohnbereich", Assert.Throws<Eingabefehler>(() => s.ZimmerSpeichern(new Zimmer("901", "Therapie", 1), neu: true)).Message);
        // 101 ist voll belegt: nicht zum Einzelzimmer, nicht stilllegen, nicht in einen anderen Bereich.
        Assert.Throws<Eingabefehler>(() => s.ZimmerSpeichern(new Zimmer("101", "R1", 1), neu: false));
        Assert.Throws<Eingabefehler>(() => s.ZimmerSpeichern(new Zimmer("101", "R1", 2, Aktiv: false), neu: false));
        Assert.Throws<Eingabefehler>(() => s.ZimmerSpeichern(new Zimmer("101", "R2", 2), neu: false));
        // 105: Eiche liegt am Fenster – ein Einzelzimmer hat kein „Bett Fenster“, also geht es nicht.
        Assert.Throws<Eingabefehler>(() => s.ZimmerSpeichern(new Zimmer("105", "R1", 1), neu: false));
        s.ZimmerSpeichern(new Zimmer("307", "R3", 1, Aktiv: false), neu: false);   // leer: stilllegen geht
        Assert.Equal(9, s.FreieBetten("R3").Count);
        s.BereichArtSetzen("R3", beschuetzt: false);
        Assert.Equal("R3 (offen)", s.Wohnbereiche().Single(w => w.Name == "R3").Anzeige);
    }

    [Fact]
    public void Aeltere_Demo_Datenbank_wird_einmal_umgestellt_mit_Sicherung()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM meta WHERE schluessel = 'demo_zimmer'; UPDATE bewohner SET wohnbereich = 'Wohnbereich A', bett = ''";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Contains(s.Startprotokoll, z => z.Contains("R1–R3"));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(s.Pfad)!, "*-vor-0.29.7.db"));
        Assert.All(s.AlleBewohner(), b => Assert.NotEqual("", b.Bett));
        Assert.False(s.DemoBeimErstenStart());   // nur einmal
    }
}

public class BereicheUmbenanntTests
{
    [Theory]
    [InlineData("Wohnbereich A", "R1")]
    [InlineData("Wohnbereich B", "R2")]
    [InlineData("Therapie", "Therapie")]
    public void Alte_Bereichskennung_wird_umbenannt(string alt, string neu) => Assert.Equal(neu, Bereiche.Umbenannt(alt));
}
