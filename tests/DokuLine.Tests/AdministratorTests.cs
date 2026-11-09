using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26a2: Position „Administrator“ mit allen Rechten, Mitarbeiter TST, immer mindestens
// ein aktiver Administrator; bestehende Datenbanken bekommen TST einmal dazu.
[Collection("Datenordner")]
public sealed class AdministratorTests : IDisposable
{
    private readonly Datenordner daten = new();
    private Speicher s = new();

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

    private void NeuOeffnen()
    {
        string pfad = s.Pfad;
        s.Dispose();
        s = new Speicher(pfad);
    }

    [Fact]
    public void TST_hat_alle_Rechte_in_allen_Bereichen()
    {
        s.DemoBeimErstenStart();
        Assert.Equal("Administrator", s.Positionsliste()[0].Name);
        var tst = s.MitarbeiterLesen("tst")!;
        Assert.Equal("Test Administrator (TST)", tst.Anzeige);
        Assert.Equal(["Administrator"], tst.Positionen);
        Assert.Equal(s.Bereichsliste().Select(b => b.Name), tst.Bereiche);
        Assert.All(Funktionen.Alle, f => Assert.True(s.Darf("TST", f.Schluessel), f.Name));
        Assert.Equal(RechteRegeln.AdminImmerText, Assert.Throws<Eingabefehler>(() => s.RechtSetzen(Funktionen.RechteVerwalten, "Administrator", false)).Fehler.Single());
        // Auch ohne Häkchen in der Tabelle und ohne „Fachkraft“ (für den Administrator gilt das nicht).
        Sql("DELETE FROM rechte WHERE position = 'Administrator'");
        s.MitarbeiterSpeichern(new Mitarbeiter("ZZ", "Zweite", "Administratorin", false, true, ["Leitung"], ["Administrator"]), neu: true);
        Assert.True(s.Darf("ZZ", Funktionen.MedikationBearbeiten));
        Assert.True(s.Darf("ZZ", Funktionen.MitarbeiterAlle));
        Assert.Null(s.AngebotGrund("ZZ", "R2"));   // Therapien jedes Bereichs, ohne dort zu sein
    }

    [Fact]
    public void Letzter_Administrator_bleibt()
    {
        s.DemoBeimErstenStart();
        var tst = s.MitarbeiterLesen("TST")!;
        Assert.Contains(RechteRegeln.LetzterAdminText, Assert.Throws<Eingabefehler>(() => s.MitarbeiterSpeichern(tst with { Aktiv = false }, neu: false)).Fehler);
        Assert.Contains(RechteRegeln.LetzterAdminText, Assert.Throws<Eingabefehler>(() => s.MitarbeiterSpeichern(tst with { Positionen = ["Verwaltung"] }, neu: false)).Fehler);
        // Mit einem zweiten Administrator geht es.
        s.MitarbeiterSpeichern(new Mitarbeiter("ZZ", "Zweite", "Administratorin", true, true, ["Leitung"], ["Administrator"]), neu: true);
        s.MitarbeiterSpeichern(tst with { Aktiv = false }, neu: false);
        Assert.False(s.MitarbeiterLesen("TST")!.Aktiv);
        Assert.Equal(RechteRegeln.NichtAngemeldetText, s.Grund("TST", Funktionen.BerichteSchreiben));   // ausgeschieden: keine Rechte
        // Die Position selbst bleibt.
        Assert.Throws<Eingabefehler>(() => s.ListeneintragAktivSetzen(Liste.Positionen, "Administrator", false));
        Assert.Throws<Eingabefehler>(() => s.ListeneintragUmbenennen(Liste.Positionen, "Administrator", "Chef"));
        Assert.Throws<Eingabefehler>(() => s.ListeneintragLoeschen(Liste.Positionen, "Administrator"));
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_TST_einmal()
    {
        s.DemoBeimErstenStart();
        // Stand vor 0.29.3: keine Position Administrator, kein TST.
        Sql("DELETE FROM kollegen WHERE kuerzel = 'TST'; DELETE FROM rechte WHERE position = 'Administrator'; " +
            "DELETE FROM positionen WHERE name = 'Administrator'; DELETE FROM meta WHERE schluessel = 'admin_tst'");
        NeuOeffnen();
        Assert.Equal("Administrator", s.Positionsliste()[0].Name);   // an erster Stelle
        var tst = s.MitarbeiterLesen("TST")!;
        Assert.True(RechteRegeln.IstAdministrator(tst));
        Assert.Equal(s.Bereichsliste().Select(b => b.Name).Order(), tst.Bereiche.Order());
        Assert.Contains(s.Startprotokoll, z => z.Contains("TST"));
        Assert.Equal(29, s.AlleMitarbeiter().Count);
        // Ein vorhandener TST wird nicht verändert, auch nicht bei erneuter Prüfung.
        s.MitarbeiterSpeichern(tst with { Vorname = "Probe", Positionen = ["Administrator", "Verwaltung"] }, neu: false);
        Sql("DELETE FROM meta WHERE schluessel = 'admin_tst'");
        NeuOeffnen();
        Assert.Equal("Probe", s.MitarbeiterLesen("TST")!.Vorname);
        Assert.Empty(s.Startprotokoll);
        NeuOeffnen();
        Assert.Empty(s.Startprotokoll);
    }
}
