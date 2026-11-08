using Dokusystem.Kern;

namespace Dokusystem.Tests;

// Bauplan Schritt 22: Jedes Therapieangebot gehört der Therapie oder einem Wohnbereich; ändern und
// zuteilen darf nur der Anbieter. Neues Recht „Therapien des eigenen Bereichs bearbeiten“.
[Collection("Datenordner")]
public sealed class TherapieAnbieterTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 8, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public TherapieAnbieterTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Angebot Demo(string name) => s.Angebote().Single(a => a.Fassungen[^1].Name == name);

    private static AngebotEingabe Eingabe(string anbieter) => new()
    {
        Name = "Gartengruppe", Therapeut = "Test (ausgedacht)", Ort = "Garten", Termine = [(DayOfWeek.Friday, "10:00", "11:00")],
        Hoechstens = "4", GiltAb = "08.11.2026", Kuerzel = "EF", Anbieter = anbieter,
    };

    [Fact]
    public void Anbieter_ist_Pflicht_und_bleibt_in_den_Fassungen()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => TherapieRegeln.AusEingabe(Eingabe(" "), Jetzt)).Fehler;
        Assert.Contains("Anbieter fehlt (Therapie oder ein Wohnbereich).", fehler);
        Assert.Equal(TherapieRegeln.AnbieterTherapie, new AngebotEingabe().Anbieter);   // Vorgabe

        var a = s.AngebotAnlegen(TherapieRegeln.AusEingabe(Eingabe(" Wohnbereich A "), Jetzt));
        Assert.Equal("Wohnbereich A", s.Angebot(a.Id)!.Anbieter);
        var e = Eingabe("Wohnbereich B");
        e.GiltAb = "20.11.2026";
        var geaendert = s.AngebotAendern(a.Id, TherapieRegeln.AusEingabe(e, Jetzt));
        Assert.Equal("Wohnbereich B", geaendert.Anbieter);                       // es zählt die letzte Fassung
        Assert.Equal("Wohnbereich A", geaendert.Am(new DateOnly(2026, 11, 10)).Anbieter);
        var verlauf = TherapieRegeln.Verlauf(geaendert, new Dictionary<long, string>());
        Assert.Contains(verlauf, z => z.Contains("Anbieter Wohnbereich A"));
        Assert.Contains(verlauf, z => z.Contains("Anbieter Wohnbereich B"));
    }

    [Fact]
    public void Demo_mit_beiden_Anbietern()
    {
        Assert.Equal("Wohnbereich A", Demo("Kochgruppe").Anbieter);
        Assert.Equal("EF", Demo("Kochgruppe").Fassungen[0].Kuerzel);
        Assert.Equal("Wohnbereich B", Demo("Kreativwerkstatt").Anbieter);
        Assert.Equal(TherapieRegeln.AnbieterTherapie, Demo("Holzwerkstatt").Anbieter);
        Assert.Equal(5, s.Angebote().Count(a => a.Anbieter == TherapieRegeln.AnbieterTherapie));
    }

    [Theory]
    [InlineData("OP", "Holzwerkstatt", null)]                        // Therapie: ihre Angebote
    [InlineData("OP", "Kochgruppe", RechteRegeln.FehltText)]          // aber nicht die eines Wohnbereichs
    [InlineData("EF", "Kochgruppe", null)]                           // Bereichsleitung A: Angebote von A
    [InlineData("EF", "Holzwerkstatt", RechteRegeln.FehltText)]       // nicht die der Therapie
    [InlineData("EF", "Kreativwerkstatt", "Nur für Wohnbereich B.")]  // nicht die von B
    [InlineData("GH", "Kreativwerkstatt", null)]
    [InlineData("GH", "Kochgruppe", "Nur für Wohnbereich A.")]
    [InlineData("CD", "Kochgruppe", RechteRegeln.FehltText)]          // Mitarbeiter A ohne das Recht
    [InlineData("UT", "Holzwerkstatt", null)]                        // Leitung: Angebote der Therapie
    [InlineData("UT", "Kochgruppe", "Nur für Wohnbereich A.")]        // Wohnbereich: nur dessen Bereich
    [InlineData("", "Holzwerkstatt", RechteRegeln.NichtAngemeldetText)]
    public void Aendern_und_zuteilen_nur_der_Anbieter(string kuerzel, string angebot, string? grund) =>
        Assert.Equal(grund, s.AngebotGrund(kuerzel, Demo(angebot).Anbieter));

    [Fact]
    public void Wer_wofuer_neue_Angebote_anlegen_darf()
    {
        Assert.Equal([TherapieRegeln.AnbieterTherapie], s.AngebotAnbieter("OP"));
        Assert.Equal(["Wohnbereich A"], s.AngebotAnbieter("EF"));
        Assert.Equal([TherapieRegeln.AnbieterTherapie], s.AngebotAnbieter("UT"));   // „Leitung“ ist kein Wohnbereich
        Assert.Empty(s.AngebotAnbieter("CD"));
        Assert.Empty(s.AngebotAnbieter(""));

        // Das Recht lässt sich in der Rechte-Tabelle auch Mitarbeitern eines Bereichs geben.
        s.RechtSetzen(Funktionen.TherapienBereich, "Mitarbeiter Wohnbereich", true);
        Assert.Equal(["Wohnbereich A"], s.AngebotAnbieter("CD"));
        Assert.Null(s.AngebotGrund("CD", "Wohnbereich A"));
        Assert.Equal(RechteRegeln.FehltText, s.AngebotGrund("CD", TherapieRegeln.AnbieterTherapie));
    }

    [Fact]
    public void Neues_Recht_wird_in_bestehenden_Datenbanken_einmal_vorbelegt()
    {
        void Sql(string text)
        {
            using var befehl = s.Verbindung.CreateCommand();
            befehl.CommandText = text;
            befehl.ExecuteNonQuery();
        }
        // Wie eine Datenbank aus 0.22.0/0.23.0: Recht unbekannt, keine Häkchen.
        Sql($"DELETE FROM rechte WHERE funktion = '{Funktionen.TherapienBereich}'; DELETE FROM meta WHERE schluessel = 'rechte_vorbelegt_{Funktionen.TherapienBereich}'");
        Assert.Equal(RechteRegeln.FehltText, s.AngebotGrund("EF", "Wohnbereich A"));
        using (var neu = new Speicher { Uhr = () => Jetzt })
            Assert.Null(neu.AngebotGrund("EF", "Wohnbereich A"));   // Bereichsleitung vorbelegt

        // Später bewusst entfernt: bleibt entfernt.
        Sql($"DELETE FROM rechte WHERE funktion = '{Funktionen.TherapienBereich}'");
        using (var neu = new Speicher { Uhr = () => Jetzt })
            Assert.Equal(RechteRegeln.FehltText, neu.AngebotGrund("EF", "Wohnbereich A"));
    }

    [Fact]
    public void Nach_dem_Update_kommen_die_Demo_Angebote_mit_Anbieter()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM meta WHERE schluessel = 'demo_therapie_anbieter'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal("Wohnbereich A", Demo("Kochgruppe").Anbieter);
        Assert.Equal(7, s.Angebote().Count);
        Assert.False(s.DemoBeimErstenStart());
    }
}
