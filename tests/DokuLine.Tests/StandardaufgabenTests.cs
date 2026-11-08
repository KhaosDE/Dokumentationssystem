using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 25: Schichtzeiten je Bereich, Standardaufgaben (Vorlagen), die zu Beginn jeder
// Schicht als Aufgaben entstehen, „aus der Nachtschicht offen“, begründet schließen, Gruppen.
[Collection("Datenordner")]
public sealed class StandardaufgabenTests : IDisposable
{
    private DateTime jetzt = new(2026, 11, 9, 23, 30, 0);   // Montag, Nachtdienst
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public StandardaufgabenTests()
    {
        s = new Speicher { Uhr = () => jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private List<Aufgabe> AusVorlagen() => s.Aufgaben(jetzt, auchErledigte: true).Where(a => a.VorlageId is not null).ToList();

    [Fact]
    public void Schichtzeiten_einstellbar_je_Bereich()
    {
        var z = Schichtzeiten.AusEingabe("06:30", "14:30", "21:45");
        Assert.Equal(Schicht.Frueh, BerichtRegeln.SchichtZu(new DateTime(2026, 11, 9, 14, 0, 0), z));
        Assert.Equal(Schicht.Nacht, BerichtRegeln.SchichtZu(new DateTime(2026, 11, 9, 21, 50, 0), z));
        Assert.Equal(Schicht.Nacht, BerichtRegeln.SchichtZu(new DateTime(2026, 11, 10, 6, 0, 0), z));
        Assert.Equal(new DateTime(2026, 11, 9, 21, 45, 0), BerichtRegeln.SchichtBeginn(new DateTime(2026, 11, 10, 6, 0, 0), z));
        Assert.Contains("Reihenfolge", Assert.Single(Assert.Throws<Eingabefehler>(() => Schichtzeiten.AusEingabe("14:00", "06:00", "22:00")).Fehler));
        Assert.Throws<Eingabefehler>(() => Schichtzeiten.AusEingabe("x", "14:00", "22:00"));

        Assert.Equal(Schichtzeiten.Vorgabe, s.Schichtzeiten("R1"));
        s.SchichtzeitenSetzen("R1", z);
        Assert.Equal(z, s.Schichtzeiten("R1"));
        Assert.Equal(Schichtzeiten.Vorgabe, s.Schichtzeiten("R2"));   // je Bereich
        Assert.Equal("Früh 06:30 · Spät 14:30 · Nacht 21:45", z.Text);
    }

    [Fact]
    public void Zu_Schichtbeginn_entstehen_die_Aufgaben_einmal()
    {
        Assert.Equal(9, s.Vorlagen().Count);
        Assert.Empty(AusVorlagen());
        // Montagnacht: vier tägliche Nacht-Aufgaben und „Kühlschränke prüfen“ (Mo, Do).
        Assert.Equal(5, s.StandardaufgabenErzeugen(jetzt));
        Assert.Equal(0, s.StandardaufgabenErzeugen(jetzt.AddMinutes(30)));   // nicht doppelt
        var nacht = AusVorlagen();
        Assert.Equal(5, nacht.Count);
        Assert.All(nacht, a => Assert.Equal(new SchichtTermin(new DateOnly(2026, 11, 9), Schicht.Nacht), a.FuerSchicht));
        Assert.Equal(new DateTime(2026, 11, 10, 3, 0, 0), nacht.Single(a => a.Text.StartsWith("Rundgang") && a.FaelligBis!.Value.Hour == 3).FaelligBis);
        Assert.Equal(new DateTime(2026, 11, 10, 6, 0, 0), nacht.Single(a => a.Text.StartsWith("Kühlschränke")).FaelligBis);   // ohne Uhrzeit: Schichtende

        // Dienstag früh: „Post verteilen“ entsteht; die Nachtaufgaben sind „aus der Nachtschicht offen“.
        s.AufgabeErledigt(nacht[0].Id, "MN", jetzt.AddMinutes(10));
        jetzt = new DateTime(2026, 11, 10, 6, 30, 0);
        Assert.Equal(1, s.StandardaufgabenErzeugen(jetzt));
        var offen = s.Aufgaben(jetzt).Where(a => a.VorlageId is not null).ToList();
        Assert.Equal(5, offen.Count);
        Assert.Equal("aus der Nachtschicht offen", AufgabenRegeln.AusSchichtOffen(offen.First(a => a.FuerSchicht!.Value.Schicht == Schicht.Nacht), jetzt));
        Assert.Null(AufgabenRegeln.AusSchichtOffen(offen.Single(a => a.Text == "Post verteilen"), jetzt));

        var gruppen = AufgabenRegeln.Gruppen(s.Aufgaben(jetzt), jetzt);
        Assert.Equal("AUS FRÜHEREN SCHICHTEN OFFEN", gruppen[0].Titel);
        Assert.Equal(4, gruppen[0].Aufgaben.Count(a => a.VorlageId is not null));
        Assert.StartsWith("JETZT: FRÜHDIENST", gruppen[1].Titel);
        Assert.Contains(gruppen[1].Aufgaben, a => a.Text == "Post verteilen");

        // Begründet schließen.
        var rundgang = offen.First(a => a.FuerSchicht!.Value.Schicht == Schicht.Nacht);
        Assert.Throws<Eingabefehler>(() => s.AufgabeSchliessen(rundgang.Id, "CD", "  ", jetzt));
        s.AufgabeSchliessen(rundgang.Id, "CD", "Notfall im anderen Bereich, keine Zeit", jetzt);
        var zu = s.AufgabeLesen(rundgang.Id)!;
        Assert.False(zu.Offen);
        Assert.True(zu.Geschlossen);
        Assert.Equal("Notfall im anderen Bereich, keine Zeit", zu.ErledigtNotiz);
        Assert.False(s.AufgabeLesen(nacht[0].Id)!.Geschlossen);   // erledigt ist nicht geschlossen
    }

    [Fact]
    public void Vorlagen_anlegen_aendern_stilllegen()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => AufgabenRegeln.VorlageAusEingabe(new VorlageEingabe(), jetzt)).Fehler;
        Assert.Contains("Was ist zu tun? Text fehlt.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("Bitte mindestens einen Wochentag"));
        var v = s.VorlageSpeichern(AufgabenRegeln.VorlageAusEingabe(new VorlageEingabe
        {
            Text = "Blumen gießen", Schicht = Schicht.Nacht, Tage = [DayOfWeek.Monday], Uhrzeit = "23:15", Kuerzel = "EF",
        }, jetzt));
        Assert.Equal("Mo", AufgabenRegeln.TageText(v.Tage));
        Assert.Equal("täglich", AufgabenRegeln.TageText(Enum.GetValues<DayOfWeek>()));
        s.VorlageSpeichern(v with { Text = "Blumen gießen (Flur)" });
        Assert.Equal("Blumen gießen (Flur)", s.Vorlagen().Single(x => x.Id == v.Id).Text);
        s.VorlageAktiv(v.Id, false);
        s.StandardaufgabenErzeugen(jetzt);
        Assert.DoesNotContain(AusVorlagen(), a => a.VorlageId == v.Id);
        s.VorlageAktiv(v.Id, true);
        s.StandardaufgabenErzeugen(jetzt);
        Assert.Contains(AusVorlagen(), a => a.VorlageId == v.Id && a.Text == "Blumen gießen (Flur)");
    }

    [Fact]
    public void Nach_dem_Update_kommen_die_Demo_Standardaufgaben()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM aufgabe_vorlagen; DELETE FROM meta WHERE schluessel = 'demo_standardaufgaben'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(9, s.Vorlagen().Count);
        Assert.False(s.DemoBeimErstenStart());
    }
}
