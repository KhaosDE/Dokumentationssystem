using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Bauplan Schritt 24: Kalender „Bereich“ und „Haus“, Fahrzeuge mit Fahrten, „vor Ort / unterwegs“,
// Doppelbuchung, mitfahrende Bewohner im Tagesplan und als abwesend.
[Collection("Datenordner")]
public sealed class FahrtenTests : IDisposable
{
    private DateTime jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public FahrtenTests()
    {
        s = new Speicher { Uhr = () => jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;

    private Fahrt Neu(string fahrzeug, string von, string bis, string datum = "09.11.2026", params long[] bewohner) =>
        FahrtRegeln.AusEingabe(new FahrtEingabe
        {
            Fahrzeug = fahrzeug, Datum = datum, Von = von, Bis = bis, Zweck = FahrtZweck.Sporttherapie, Fahrer = "OP",
            Bewohner = [.. bewohner], Ziel = "Hallenbad", Kuerzel = "OP",
        }, s.AktiveFahrzeuge(), jetzt);

    [Fact]
    public void Demo_Fahrzeuge_vor_Ort_und_unterwegs()
    {
        Assert.Equal(["VW Bus", "Caddy"], s.AktiveFahrzeuge());
        var staende = s.FahrzeugStaende(jetzt);
        var caddy = staende.Single(x => x.Fahrzeug == "Caddy");
        Assert.True(caddy.Unterwegs);
        Assert.Equal("unterwegs seit 09:35, zurück geplant 11:05 (Fahrer KL)", caddy.Text);
        Assert.StartsWith("vor Ort", staende.Single(x => x.Fahrzeug == "VW Bus").Text);
        // Ahorn fährt mit: abwesend, und die Fahrt steht in seinem Wochen- bzw. Tagesplan.
        Assert.Equal("abwesend: unterwegs mit Caddy bis 11:05", s.Mitfahrende(jetzt)[B("Ahorn")].Text);
        Assert.Equal(MarkenArt.Abwesend, s.Mitfahrende(jetzt)[B("Ahorn")].Art);
        Assert.Contains(s.Tagesplan(DateOnly.FromDateTime(jetzt), [s.Bewohner(B("Ahorn"))!]),
            z => z.Termin.Quelle == TerminQuelle.Fahrt && z.Termin.Titel == "Fahrt: Arzttermin (Caddy)");
        // Rückkehr überschritten.
        Assert.EndsWith("– Rückkehr überschritten", s.FahrzeugStaende(jetzt.AddHours(2)).Single(x => x.Fahrzeug == "Caddy").Text);
        Assert.True(s.FahrzeugStaende(jetzt.AddHours(2)).Single(x => x.Fahrzeug == "Caddy").Ueberfaellig);
    }

    [Fact]
    public void Pruefregeln_und_Doppelbuchung()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => FahrtRegeln.AusEingabe(new FahrtEingabe { Datum = "x" }, s.AktiveFahrzeuge(), jetzt)).Fehler;
        Assert.Contains("Bitte ein Fahrzeug wählen.", fehler);
        Assert.Contains("Fahrer (Kürzel) fehlt.", fehler);
        Assert.Contains("Ziel fehlt.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("Kürzel fehlt"));
        Assert.Throws<Eingabefehler>(() => Neu("Traktor", "10:00", "11:00"));
        Assert.Contains("„Zurück geplant“ muss nach „los“ liegen.", Assert.Throws<Eingabefehler>(() => Neu("VW Bus", "11:00", "10:00")).Fehler);
        Assert.Equal("VW Bus", Neu("vw bus", "12:00", "13:00").Fahrzeug);   // Schreibweise aus der Liste

        // Tierpark-Ausflug mit dem VW Bus in 8 Tagen: Überschneidung → Warnung.
        Assert.NotEmpty(s.FahrtUeberschneidungen(Neu("VW Bus", "10:00", "11:00", "17.11.2026")));
        Assert.Empty(s.FahrtUeberschneidungen(Neu("Caddy", "10:00", "11:00", "17.11.2026")));
        Assert.NotEmpty(s.FahrtUeberschneidungen(Neu("Caddy", "10:30", "12:00")));   // Caddy ist bis 11:05 unterwegs
    }

    [Fact]
    public void Losgefahren_und_zurueck_mit_Uhrzeit()
    {
        var f = s.FahrtAnlegen(Neu("VW Bus", "10:15", "12:00", bewohner: B("Birke")));
        Assert.Equal("vor Ort · nächste Fahrt 10:15 (Sporttherapie)", s.FahrzeugStaende(jetzt).Single(x => x.Fahrzeug == "VW Bus").Text);
        Assert.Throws<Eingabefehler>(() => s.Zurueck(f.Id, "OP", jetzt));        // noch nicht losgefahren
        jetzt = jetzt.AddMinutes(20);
        var los = s.Losgefahren(f.Id, "OP", jetzt);
        Assert.Equal(new DateTime(2026, 11, 9, 10, 20, 0), los.Losgefahren);
        Assert.Equal("OP", los.LosVon);
        Assert.Throws<Eingabefehler>(() => s.Losgefahren(f.Id, "OP", jetzt));    // nicht zweimal
        Assert.StartsWith("unterwegs seit 10:20, zurück geplant 12:00", s.FahrzeugStaende(jetzt).Single(x => x.Fahrzeug == "VW Bus").Text);
        Assert.Contains(B("Birke"), s.Mitfahrende(jetzt).Keys);
        // Ein zweites Losfahren desselben Fahrzeugs geht erst nach „zurück“.
        var zweite = s.FahrtAnlegen(Neu("VW Bus", "12:30", "13:00"));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.Losgefahren(zweite.Id, "OP", jetzt)).Fehler, x => x.Contains("noch unterwegs"));
        jetzt = jetzt.AddHours(1);
        Assert.NotNull(s.Zurueck(f.Id, "AB", jetzt).Zurueck);
        Assert.StartsWith("vor Ort", s.FahrzeugStaende(jetzt).Single(x => x.Fahrzeug == "VW Bus").Text);
        Assert.DoesNotContain(B("Birke"), s.Mitfahrende(jetzt).Keys);
        Assert.Throws<Eingabefehler>(() => s.Zurueck(f.Id, "AB", jetzt));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE fahrten SET ziel = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Fahrzeugliste_erweiterbar_und_stilllegen()
    {
        s.FahrzeugAnlegen(" Kleinbus ");
        Assert.Contains("Kleinbus", s.AktiveFahrzeuge());
        Assert.Throws<Eingabefehler>(() => s.FahrzeugAnlegen("kleinbus"));
        Assert.Throws<Eingabefehler>(() => s.FahrzeugAnlegen("  "));
        s.FahrzeugAktiv("Kleinbus", false);
        Assert.DoesNotContain("Kleinbus", s.AktiveFahrzeuge());
        Assert.Contains(s.Fahrzeuge(), f => f.Name == "Kleinbus" && !f.Aktiv);
    }

    [Fact]
    public void Kalender_Bereich_und_Haus()
    {
        var alle = s.Kalender(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        var haus = alle.Where(k => k.Aktuell.Haus).Select(k => k.Aktuell.Titel).ToList();
        Assert.Contains("Hausversammlung", haus);
        Assert.Contains("Brandschutzübung", haus);
        Assert.Contains(alle, k => !k.Aktuell.Haus);
        Assert.All(alle.Where(k => KalenderRegeln.Passt(k.Aktuell, KalenderWahl.Haus)), k => Assert.True(k.Aktuell.Haus));
        Assert.All(alle.Where(k => KalenderRegeln.Passt(k.Aktuell, KalenderWahl.Bereich)), k => Assert.False(k.Aktuell.Haus));
        Assert.Equal(alle.Count, alle.Count(k => KalenderRegeln.Passt(k.Aktuell, KalenderWahl.Beide)));
        var brand = alle.Single(k => k.Aktuell.Titel == "Brandschutzübung");
        Assert.StartsWith("Haus: ", KalenderRegeln.AlsTermin(brand, DateOnly.FromDateTime(brand.Aktuell.Beginn)).Titel);

        var f = KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Wohnbereich, Titel = "Sommerfest", BeginnDatum = "20.11.2026", BeginnUhrzeit = "15:00", Haus = true, Kuerzel = "RS",
        }, jetzt);
        var neu = s.KalenderAnlegen(f);
        Assert.True(s.KalenderTermin(neu.Id)!.Aktuell.Haus);
        Assert.Contains("Kalender: Haus", KalenderRegeln.AlsText(neu.Aktuell, new Dictionary<long, string>()));
    }

    [Fact]
    public void Nach_dem_Update_kommen_Fahrzeuge_und_Haus_Termine_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM fahrten; DELETE FROM fahrzeuge; DELETE FROM meta WHERE schluessel IN ('demo_fahrten', 'demo_kalender_haus')";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(["VW Bus", "Caddy"], s.AktiveFahrzeuge());
        Assert.Contains(s.Kalender(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)), k => k.Aktuell.Haus);
        Assert.False(s.DemoBeimErstenStart());
    }
}
