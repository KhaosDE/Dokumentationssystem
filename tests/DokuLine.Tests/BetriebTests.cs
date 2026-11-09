using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 37: Betriebsmodus – betrieb.json, getrennte Ordner, Passwörter mit Sperre, Zugriffsprotokoll, Sicherung und
// Rücksicherung, Häuser. Nur mit ausgedachten Daten.
[Collection("Datenordner")]
public sealed class BetriebTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly string programm = Path.Combine(Path.GetTempPath(), $"dokuline-programm-{Guid.NewGuid():N}");
    private readonly Speicher s;

    public BetriebTests()
    {
        Directory.CreateDirectory(programm);
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        Betrieb.Setzen(null);
        s.Dispose();
        daten.Dispose();
        try { Directory.Delete(programm, recursive: true); }
        catch (IOException) { }
    }

    private void Json(string text) => File.WriteAllText(Path.Combine(programm, Betrieb.Datei), text);

    [Fact]
    public void Betrieb_json_lesen_und_Ordner_trennen()
    {
        Assert.Null(Betrieb.Laden(programm));   // ohne Datei: Demo
        string ordner = Path.Combine(programm, "daten");
        string sicherung = Path.Combine(programm, "sicherungen");
        Json($$"""{ "datenordner": {{System.Text.Json.JsonSerializer.Serialize(ordner)}}, "sicherungsordner": {{System.Text.Json.JsonSerializer.Serialize(sicherung)}}, "sperre_minuten": 5, "sicherung_um": "03:15" }""");
        var w = Betrieb.Laden(programm)!;
        Assert.Equal((ordner, 5, 24, new TimeOnly(3, 15)), (w.Datenordner, w.SperreMinuten, w.ProtokollMonate, w.SicherungUm));
        string benutzer = Pfade.Benutzerordner;
        Betrieb.Setzen(w);
        Assert.True(Betrieb.Aktiv);
        Assert.Equal(ordner, Pfade.Datenordner);                    // gemeinsam (Datenbank, Bilder, Ablage)
        Assert.Equal(benutzer, Pfade.Benutzerordner);               // Einstellungen je Benutzer
        Assert.Equal(Path.Combine(benutzer, Einstellungen.Datei), Pfade.ImBenutzerordner(Einstellungen.Datei));
        Assert.Equal(sicherung, Betrieb.Sicherungsordner);
        // Im Betrieb zählt nur die Anmeldung dieses Programmlaufs, nicht das Kürzel in der Datei des Benutzers.
        Einstellungen.Aendern(e => e with { Kuerzel = "TST" });
        Assert.Equal("", Sitzung.Kuerzel);
        Sitzung.Anmelden("AB");
        Einstellungen.Aendern(e => e with { Kuerzel = "TST" });
        Assert.Equal("AB", Sitzung.Kuerzel);
        Sitzung.Anmelden("");
        Betrieb.Setzen(null);
        Assert.Equal("", Sitzung.Kuerzel);   // Demo: wieder aus den Einstellungen
        // Sicherungsordner ist Pflicht und darf nicht im Datenordner liegen (den dürfen alle Benutzer ändern).
        Json($$"""{ "datenordner": {{System.Text.Json.JsonSerializer.Serialize(ordner)}} }""");
        Assert.Throws<SpeicherFehler>(() => Betrieb.Laden(programm));
        Json($$"""{ "datenordner": {{System.Text.Json.JsonSerializer.Serialize(ordner)}}, "sicherungsordner": {{System.Text.Json.JsonSerializer.Serialize(Path.Combine(ordner, "sich"))}} }""");
        Assert.Contains("nicht im Datenordner", Assert.Throws<SpeicherFehler>(() => Betrieb.Laden(programm)).Message);
        Json("""{ "datenordner": "relativ\\daten" }""");
        Assert.Throws<SpeicherFehler>(() => Betrieb.Laden(programm));
        Json("""{ "datenordner": "C:\\DokuLine", "sperre_minuten": 0 }""");
        if (OperatingSystem.IsWindows()) Assert.Throws<SpeicherFehler>(() => Betrieb.Laden(programm));
        Json("{kaputt");
        Assert.Throws<SpeicherFehler>(() => Betrieb.Laden(programm));   // nie still in die Demo
    }

    [Fact]
    public void Passwort_Hash_Sperre_Einmalpasswort()
    {
        var (hash, salz) = PasswortRegeln.Hashen("Sonnenblume-42");
        Assert.DoesNotContain("Sonnenblume", hash);
        Assert.True(PasswortRegeln.Pruefen("Sonnenblume-42", hash, salz, PasswortRegeln.Iterationen));
        Assert.False(PasswortRegeln.Pruefen("sonnenblume-42", hash, salz, PasswortRegeln.Iterationen));
        Assert.NotNull(PasswortRegeln.Schwach("kurz", "AB"));
        Assert.NotNull(PasswortRegeln.Schwach("ab-geheim-123", "AB"));   // enthält das Kürzel
        Assert.NotNull(PasswortRegeln.Schwach("aaaaaaaaaaaa", "XY"));
        Assert.Null(PasswortRegeln.Schwach("Sonnenblume-42", "XY"));
        Assert.Equal(12, PasswortRegeln.Einmalpasswort().Length);

        var m = s.AlleMitarbeiter().First(x => x.Aktiv && !RechteRegeln.IstAdministrator(x) && x.Positionen.Contains("Mitarbeiter Wohnbereich"));
        Assert.Equal(AnmeldeErgebnis.OhnePasswort, s.PasswortAnmelden(m.Kuerzel, "egal").Ergebnis);
        Assert.Throws<Eingabefehler>(() => s.EinmalpasswortSetzen(m.Kuerzel, "Einmal-1234", m.Kuerzel, Jetzt));   // ohne Recht
        s.EinmalpasswortSetzen(m.Kuerzel, "Einmal-1234", "TST", Jetzt);
        Assert.Equal(AnmeldeErgebnis.AendernPflicht, s.PasswortAnmelden(m.Kuerzel, "Einmal-1234").Ergebnis);
        Assert.Throws<Eingabefehler>(() => s.PasswortAendern(m.Kuerzel, "falsch", "Sonnenblume-42", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.PasswortAendern(m.Kuerzel, "Einmal-1234", "kurz", Jetzt));
        s.PasswortAendern(m.Kuerzel, "Einmal-1234", "Sonnenblume-42", Jetzt);
        var (ok, wer) = s.PasswortAnmelden(m.Kuerzel.ToLowerInvariant(), "Sonnenblume-42");
        Assert.Equal((AnmeldeErgebnis.Ok, m.Kuerzel), (ok, wer!.Kuerzel));
        for (int i = 1; i < PasswortRegeln.Fehlversuche; i++) Assert.Equal(AnmeldeErgebnis.Falsch, s.PasswortAnmelden(m.Kuerzel, "falsch").Ergebnis);
        Assert.Equal(AnmeldeErgebnis.Gesperrt, s.PasswortAnmelden(m.Kuerzel, "falsch").Ergebnis);
        Assert.Equal(AnmeldeErgebnis.Gesperrt, s.PasswortAnmelden(m.Kuerzel, "Sonnenblume-42").Ergebnis);   // auch mit richtigem
        Assert.True(s.Gesperrt(m.Kuerzel));
        s.Entsperren(m.Kuerzel, "TST");
        Assert.Equal(AnmeldeErgebnis.Ok, s.PasswortAnmelden(m.Kuerzel, "Sonnenblume-42").Ergebnis);
        Assert.Equal(AnmeldeErgebnis.Falsch, s.PasswortAnmelden("XYZ", "egal").Ergebnis);   // unbekannt wie falsch
        s.EinmalpasswortSetzen(m.Kuerzel, "Einmal-1234", "TST", Jetzt.AddDays(-PasswortRegeln.EinmalTage - 1));
        Assert.Equal(AnmeldeErgebnis.Abgelaufen, s.PasswortAnmelden(m.Kuerzel, "Einmal-1234").Ergebnis);
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "SELECT hash || salz FROM passwoerter";
        Assert.DoesNotContain("Sonnenblume", (string)befehl.ExecuteScalar()!);
    }

    [Fact]
    public void Einmalpasswort_nur_fuer_Mitarbeiter_mit_weniger_Rechten()
    {
        var alle = s.AlleMitarbeiter().Where(x => x.Aktiv).ToList();
        var leitung = alle.First(x => x.Positionen.SequenceEqual(["Bereichsleitung"]));
        string bereich = leitung.Bereiche[0];
        var eigener = alle.First(x => x.Positionen.SequenceEqual(["Mitarbeiter Wohnbereich"]) && x.Bereiche.SequenceEqual([bereich]));
        var fremder = alle.First(x => x.Positionen.SequenceEqual(["Mitarbeiter Wohnbereich"]) && !x.Bereiche.Contains(bereich));
        var hausleitung = alle.First(x => x.Positionen.SequenceEqual(["Hausleitung"]));
        s.EinmalpasswortSetzen(eigener.Kuerzel, "Einmal-1234", leitung.Kuerzel, Jetzt);
        Assert.Throws<Eingabefehler>(() => s.EinmalpasswortSetzen(fremder.Kuerzel, "Einmal-1234", leitung.Kuerzel, Jetzt));      // anderer Bereich
        Assert.Throws<Eingabefehler>(() => s.EinmalpasswortSetzen(hausleitung.Kuerzel, "Einmal-1234", leitung.Kuerzel, Jetzt));  // mehr Rechte
        Assert.Throws<Eingabefehler>(() => s.EinmalpasswortSetzen("TST", "Einmal-1234", hausleitung.Kuerzel, Jetzt));            // Administrator
        Assert.Throws<Eingabefehler>(() => s.EinmalpasswortSetzen(leitung.Kuerzel, "Einmal-1234", leitung.Kuerzel, Jetzt));      // sich selbst
        s.EinmalpasswortSetzen(leitung.Kuerzel, "Einmal-1234", hausleitung.Kuerzel, Jetzt);
        s.EinmalpasswortSetzen(hausleitung.Kuerzel, "Einmal-1234", "TST", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.Entsperren(hausleitung.Kuerzel, leitung.Kuerzel));
    }

    [Fact]
    public void Betriebsdatenbank_oeffnet_die_Demo_nie()
    {
        Assert.False(s.IstBetriebsdatenbank);
        s.AlsBetriebMarkieren();
        Assert.True(s.IstBetriebsdatenbank);
        Assert.Equal(Betrieb.DemoVerbotenText, Assert.Throws<SpeicherFehler>(s.DemoZuruecksetzen).Message);
        Assert.False(s.DemoBeimErstenStart());
        s.Dispose();
        Assert.Equal(Betrieb.DemoVerbotenText, Assert.Throws<SpeicherFehler>(() => Speicher.Oeffnen(Einstellungen.Laden())).Message);
    }

    [Fact]
    public void Verwalter_nur_beim_ersten_Start()
    {
        Assert.Throws<Eingabefehler>(() => s.VerwalterAnlegen("VW", "Vera", "Walter", "Sonnenblume-42", Jetzt));   // Demo hat Mitarbeiter
        string pfad = Path.Combine(programm, "leer.db");
        using var leer = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Empty(leer.AlleMitarbeiter());
        Assert.Throws<Eingabefehler>(() => leer.VerwalterAnlegen("VW", "Vera", "Walter", "kurz", Jetzt));
        var m = leer.VerwalterAnlegen("vw", "Vera", "Walter", "Sonnenblume-42", Jetzt);
        Assert.Equal("VW", m.Kuerzel);
        Assert.True(RechteRegeln.IstAdministrator(m));
        Assert.Equal(AnmeldeErgebnis.Ok, leer.PasswortAnmelden("VW", "Sonnenblume-42").Ergebnis);
        Assert.Null(leer.Grund("VW", Funktionen.BetriebVerwalten));
    }

    [Fact]
    public void Zugriffsprotokoll_nur_mit_Recht_und_Aufbewahrung()
    {
        long id = s.AlleBewohner()[0].Id!.Value;
        s.Zugriff("TST", id, ZugriffRegeln.Geoeffnet, "Akte");
        s.AngabeSpeichern(id, Angabe.Personenbeschreibung, new Personenbeschreibung { Groesse = "1,70 m" }, DateOnly.FromDateTime(Jetzt), "TST", Jetzt);
        var heute = DateOnly.FromDateTime(Jetzt);
        var liste = s.Zugriffe("TST", heute, heute, id);
        Assert.Contains(liste, z => z.Aktion == ZugriffRegeln.Geoeffnet && z.Kuerzel == "TST");
        Assert.Contains(liste, z => z.Aktion == ZugriffRegeln.Geaendert && z.Was == "Akte: Personenbeschreibung");
        var mitarbeiter = s.AlleMitarbeiter().First(x => x.Aktiv && x.Positionen.SequenceEqual(["Mitarbeiter Wohnbereich"]));
        Assert.Throws<Eingabefehler>(() => s.Zugriffe(mitarbeiter.Kuerzel, heute, heute));
        Assert.False(RechteRegeln.Vorbelegt("Verwaltung", Funktionen.Zugriffsprotokoll));
        Assert.True(RechteRegeln.Vorbelegt("Hausleitung", Funktionen.Zugriffsprotokoll));
        Assert.Equal(0, s.ZugriffeAufraeumen(Jetzt, 24));
        Assert.True(s.ZugriffeAufraeumen(Jetzt.AddMonths(25), 24) >= 2);
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE zugriffe SET kuerzel = 'X'";
        befehl.ExecuteNonQuery();   // leer: kein Fehler
        s.Zugriff("TST", id, ZugriffRegeln.Gedruckt, "Vitalwerte");
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());   // unveränderlich
    }

    [Fact]
    public void Sicherung_pruefen_aufraeumen_und_zuruecksichern()
    {
        string ordner = Path.Combine(programm, "sicherungen");
        // Zur Sicherung gehört die Ablage neben der Datenbank (Dokumente, Bilder).
        string dokumente = Path.Combine(Path.GetDirectoryName(s.Pfad)!, DokumentRegeln.Ordner), bilder = Pfade.ImDatenordner(Bilder.Ordner);
        Directory.CreateDirectory(dokumente);
        Directory.CreateDirectory(bilder);
        File.WriteAllText(Path.Combine(dokumente, "alt.txt"), "ausgedacht");
        File.WriteAllText(Path.Combine(bilder, "platzhalter.png"), "kein Bild");
        Assert.True(SicherungRegeln.Faellig(null, Jetzt, new TimeOnly(2, 30)));
        var erste = s.SicherungErstellen(ordner, Jetzt);
        Assert.EndsWith(".zip", erste.Pfad);
        Assert.Null(Speicher.SicherungPruefen(erste.Pfad));
        using (var zip = System.IO.Compression.ZipFile.OpenRead(erste.Pfad))
            Assert.Superset(new HashSet<string> { SicherungRegeln.DatenbankEintrag, $"{DokumentRegeln.Ordner}/alt.txt", $"{Bilder.Ordner}/platzhalter.png" },
                zip.Entries.Select(e => e.FullName).ToHashSet());
        Assert.Equal(Jetzt, s.LetzteSicherungZeit);
        Assert.False(SicherungRegeln.Faellig(s.LetzteSicherungZeit, Jetzt.AddHours(5), new TimeOnly(2, 30)));
        Assert.True(SicherungRegeln.Faellig(s.LetzteSicherungZeit, Jetzt.AddDays(1).Date.AddHours(3), new TimeOnly(2, 30)));
        Assert.False(SicherungRegeln.Warnen(s.LetzteSicherungZeit, Jetzt.AddDays(2)));
        Assert.True(SicherungRegeln.Warnen(s.LetzteSicherungZeit, Jetzt.AddDays(2).AddHours(1)));
        File.WriteAllText(Path.Combine(ordner, SicherungRegeln.Name(Jetzt.AddDays(-1))), "keine Datenbank");
        Assert.NotNull(Speicher.SicherungPruefen(Path.Combine(ordner, SicherungRegeln.Name(Jetzt.AddDays(-1)))));

        // Rücksicherung: ein Bewohner nach der Sicherung angelegt ist danach wieder weg; der vorige Stand liegt daneben.
        int vorher = s.AlleBewohner().Count;
        s.DemoZuruecksetzen();
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM bewohner WHERE id = (SELECT MIN(id) FROM bewohner)";
            befehl.ExecuteNonQuery();
        }
        Assert.Equal(vorher - 1, s.AlleBewohner().Count);
        var assistenz = s.AlleMitarbeiter().First(x => x.Aktiv && x.Positionen.SequenceEqual(["Mitarbeiter Wohnbereich"]));
        Assert.Throws<Eingabefehler>(() => s.Ruecksichern(erste.Pfad, assistenz.Kuerzel));
        File.Delete(Path.Combine(dokumente, "alt.txt"));
        File.WriteAllText(Path.Combine(dokumente, "neu.txt"), "ausgedacht");
        string kopie = s.Ruecksichern(erste.Pfad, "TST");
        Assert.True(File.Exists(kopie));
        Assert.Equal(vorher, s.AlleBewohner().Count);
        Assert.True(File.Exists(Path.Combine(dokumente, "alt.txt")));
        Assert.False(File.Exists(Path.Combine(dokumente, "neu.txt")));   // liegt im Ordner „…-vor-ruecksicherung-…“ daneben
        Assert.Single(Directory.GetDirectories(Path.GetDirectoryName(dokumente)!, DokumentRegeln.Ordner + "-vor-ruecksicherung-*"));
        Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(s.Pfad)!, "ruecksicherung-*"));   // ausgepackter Stand aufgeräumt
        Assert.Throws<Eingabefehler>(() => s.Ruecksichern(Path.Combine(ordner, SicherungRegeln.Name(Jetzt.AddDays(-1))), "TST"));
    }

    [Fact]
    public void Aufbewahrung_30_taegliche_und_12_monatliche()
    {
        var alle = Enumerable.Range(0, 400).Select(i => new Sicherung($"s{i}", Jetzt.AddDays(-i), 1)).ToList();
        alle.Add(new Sicherung("zweite-heute", Jetzt.AddHours(-1), 1));
        var weg = SicherungRegeln.Ueberzaehlig(alle);
        var bleiben = alle.Except(weg).ToList();
        Assert.Contains(bleiben, x => x.Pfad == "s0");
        Assert.Contains(weg, x => x.Pfad == "zweite-heute");    // je Tag nur die neueste
        Assert.Equal(30, bleiben.Count(x => x.Zeit > Jetzt.AddDays(-30)));
        Assert.Equal(12, bleiben.Select(x => (x.Zeit.Year, x.Zeit.Month)).Distinct().Count());
        Assert.InRange(bleiben.Count, 30, 42);
        Assert.Equal(new DateTime(2026, 11, 9, 2, 30, 0), SicherungRegeln.ZeitAus(SicherungRegeln.Name(new DateTime(2026, 11, 9, 2, 30, 0))));
        Assert.Null(SicherungRegeln.ZeitAus("fremd.db"));
    }

    [Fact]
    public void Haeuser_bestimmen_die_Bereichswahl()
    {
        var bereiche = s.Wohnbereiche().Select(w => w.Name).ToList();
        Assert.Equal(bereiche, s.SichtbareWohnbereiche("TST"));   // ohne Häuser: alle
        Assert.Throws<Eingabefehler>(() => s.HausAnlegen("Haus am See", s.AlleMitarbeiter().First(x => x.Positionen.SequenceEqual(["Mitarbeiter Wohnbereich"])).Kuerzel));
        s.HausAnlegen("Haus am See (ausgedacht)", "TST");
        s.HausAnlegen("Haus am Wald (ausgedacht)", "TST");
        Assert.Throws<Eingabefehler>(() => s.HausAnlegen("haus am see (ausgedacht)", "TST"));
        s.BereichHausSetzen(bereiche[0], "Haus am See (ausgedacht)", "TST");
        s.BereichHausSetzen(bereiche[1], "Haus am Wald (ausgedacht)", "TST");
        var r1 = s.AlleMitarbeiter().First(x => x.Aktiv && !RechteRegeln.IstAdministrator(x) && x.Positionen.SequenceEqual(["Mitarbeiter Wohnbereich"]) &&
                                                x.Bereiche.Contains(bereiche[0]) && !x.Bereiche.Contains(bereiche[1]));
        var sichtbar = s.SichtbareWohnbereiche(r1.Kuerzel);
        Assert.Contains(bereiche[0], sichtbar);
        Assert.DoesNotContain(bereiche[1], sichtbar);
        Assert.Equal(bereiche, s.SichtbareWohnbereiche("TST"));   // mit Recht „andere Häuser“
        Assert.Equal(["a", "c"], HaeuserRegeln.Sichtbar(["a", "b", "c"], new Dictionary<string, string> { ["a"] = "X", ["b"] = "Y" }, ["a"], false));
        s.HausLoeschen("Haus am Wald (ausgedacht)", "TST");
        Assert.False(s.HausVonBereich().ContainsKey(bereiche[1]));
        // Bereich umbenennen nimmt die Zuordnung mit.
        s.ListeneintragUmbenennen(Liste.Bereiche, bereiche[0], "R1 neu");
        Assert.Equal("Haus am See (ausgedacht)", s.HausVonBereich()["R1 neu"]);
    }
}
