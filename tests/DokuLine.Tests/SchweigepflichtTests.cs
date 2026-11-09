using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 35: Schweigepflichtentbindungen – Auskunftsleiste, Kurzform, Schnellabfrage, Fristen, Prüfungen, Demo, Datenbank.
[Collection("Datenordner")]
public sealed class SchweigepflichtTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public SchweigepflichtTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static string Name(long? id) => id switch { 1 => "Erika Ahornblatt", 2 => "Bernd Ahornblatt", 3 => "Dr. Ulme (ausgedacht)", _ => "?" };

    private static AngabeFassung F(params Schweigepflichteintrag[] e) =>
        new(1, 1, Angabe.Schweigepflicht, AngabeZustand.Eingetragen, System.Text.Json.JsonSerializer.Serialize(new Schweigepflicht { Eintraege = [.. e] }),
            Heute, Jetzt, "EF");

    private static readonly Schweigepflichteintrag Mutter = new()
    {
        PersonId = 1, Bezeichnung = "Mutter", Stand = Entbindungsstand.Entbunden, ErteiltAm = Heute.AddDays(-100), Form = "schriftlich",
    };
    private static readonly Schweigepflichteintrag Bruder = new() { PersonId = 2, Bezeichnung = "Bruder", Stand = Entbindungsstand.KeineAuskunft };

    [Fact]
    public void Leiste_mit_Zeichen_Wort_und_alle_anderen()
    {
        var leiste = SchweigepflichtRegeln.Leiste(F(Bruder, Mutter with { Umfang = SchweigepflichtRegeln.UmfangOrganisatorisch }), Name, Heute);
        Assert.Equal(["✓ Mutter · Erika Ahornblatt – entbunden (nur Organisatorisches)", "✕ Bruder · Bernd Ahornblatt – keine Auskunft",
            SchweigepflichtRegeln.AlleAnderen], leiste.Select(m => m.Text));
        Assert.Equal([Entbindungsstand.Entbunden, Entbindungsstand.KeineAuskunft, Entbindungsstand.KeineAuskunft], leiste.Select(m => m.Art));
        // Nichts eingetragen: nie wie „alles erlaubt“ (Regel 5).
        Assert.Equal([new AuskunftMarke(SchweigepflichtRegeln.NichtErfasstText, Entbindungsstand.NichtErfasst)], SchweigepflichtRegeln.Leiste(null, Name, Heute));
        var keine = new AngabeFassung(1, 1, Angabe.Schweigepflicht, AngabeZustand.Keine, "", Heute, Jetzt, "EF");
        Assert.Equal(SchweigepflichtRegeln.KeineText, Assert.Single(SchweigepflichtRegeln.Leiste(keine, Name, Heute)).Text);
    }

    [Fact]
    public void Abgelaufen_und_widerrufen_zaehlen_nicht_als_entbunden()
    {
        var abgelaufen = Mutter with { GueltigBis = Heute.AddDays(-1) };
        var widerrufen = Mutter with { PersonId = 3, Bezeichnung = "Hausarzt", WiderrufenAm = Heute.AddDays(-3) };
        var spaeter = Mutter with { PersonId = 2, Bezeichnung = "Bruder", WiderrufenAm = Heute.AddDays(5) };   // erst in Zukunft widerrufen
        Assert.Equal(Entbindungsstand.KeineAuskunft, SchweigepflichtRegeln.Wirksam(abgelaufen, Heute));
        Assert.Equal(Entbindungsstand.KeineAuskunft, SchweigepflichtRegeln.Wirksam(widerrufen, Heute));
        Assert.Equal(Entbindungsstand.Entbunden, SchweigepflichtRegeln.Wirksam(spaeter, Heute));
        Assert.Equal(Entbindungsstand.Entbunden, SchweigepflichtRegeln.Wirksam(Mutter with { GueltigBis = Heute }, Heute));   // bis einschließlich
        var leiste = SchweigepflichtRegeln.Leiste(F(abgelaufen, widerrufen), Name, Heute);
        Assert.Contains(leiste, m => m.Text == "✕ Mutter · Erika Ahornblatt – Entbindung abgelaufen am 08.11.2026" && !m.Widerrufen);
        Assert.Contains(leiste, m => m.Text == "✕ Hausarzt · Dr. Ulme (ausgedacht) – widerrufen am 06.11.2026" && m.Widerrufen);
        Assert.Equal(("2 keine Auskunft", Entbindungsstand.KeineAuskunft), (SchweigepflichtRegeln.Kurz(F(abgelaufen, widerrufen), Name, Heute).Kurz,
            SchweigepflichtRegeln.Kurz(F(abgelaufen, widerrufen), Name, Heute).Art));
    }

    [Fact]
    public void Kurzform_und_Schnellabfrage()
    {
        var offen = new Schweigepflichteintrag { Stelle = "Bezirk Musterland", Bezeichnung = "Kostenträger" };
        var f = F(Mutter, Bruder, offen);
        var kurz = SchweigepflichtRegeln.Kurz(f, Name, Heute);
        Assert.Equal("1 entbunden · 1 keine Auskunft · 1 nicht erfasst", kurz.Kurz);
        Assert.Equal(Entbindungsstand.NichtErfasst, kurz.Art);
        Assert.Contains("Erika Ahornblatt", kurz.Namen);
        Assert.Equal(("nicht erfasst", Entbindungsstand.NichtErfasst), (SchweigepflichtRegeln.Kurz(null, Name, Heute).Kurz, SchweigepflichtRegeln.Kurz(null, Name, Heute).Art));

        var ja = SchweigepflichtRegeln.Pruefen(f, "erika", Name, Heute);
        Assert.Equal(Entbindungsstand.Entbunden, ja.Art);
        Assert.Equal("entbunden für Mutter · Erika Ahornblatt (alle Angaben, geben und einholen, unbefristet)", ja.Text);
        Assert.Equal(Entbindungsstand.KeineAuskunft, SchweigepflichtRegeln.Pruefen(f, "Bruder", Name, Heute).Art);
        Assert.Equal(Entbindungsstand.NichtErfasst, SchweigepflichtRegeln.Pruefen(f, "Bezirk", Name, Heute).Art);
        Assert.Equal("„Polizei“ ist nicht eingetragen – keine Auskunft", SchweigepflichtRegeln.Pruefen(f, "Polizei", Name, Heute).Text);
        // Mehrere Treffer („Ahornblatt“): die vorsichtigste Antwort.
        Assert.Equal(Entbindungsstand.KeineAuskunft, SchweigepflichtRegeln.Pruefen(f, "Ahornblatt", Name, Heute).Art);
        Assert.StartsWith(SchweigepflichtRegeln.NichtErfasstText, SchweigepflichtRegeln.Pruefen(null, "Erika", Name, Heute).Text);
        Assert.Equal(Entbindungsstand.NichtErfasst, SchweigepflichtRegeln.Pruefen(f, "e", Name, Heute).Art);   // zu kurz
    }

    [Fact]
    public void Frist_vier_Wochen_vorher()
    {
        var bald = Mutter with { GueltigBis = Heute.AddDays(28) };
        var spaet = Mutter with { PersonId = 2, Bezeichnung = "Bruder", GueltigBis = Heute.AddDays(29) };
        var fristen = SchweigepflichtRegeln.Fristen(F(bald, spaet, Mutter with { GueltigBis = Heute.AddDays(3), WiderrufenAm = Heute }), Name, Heute);
        Assert.Equal(["Entbindung endet 07.12.2026 (Mutter · Erika Ahornblatt)"], fristen.Select(m => m.Text));
    }

    [Fact]
    public void Speichern_prueft_und_bleibt_als_Fassung()
    {
        long id = s.AlleBewohner()[0].Id!.Value;
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(id, Angabe.Schweigepflicht,
            new Schweigepflicht { Eintraege = [new() { Stand = Entbindungsstand.Entbunden }] }, Heute, "EF", Jetzt)).Fehler, f => f.Contains("Person oder Stelle"));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(id, Angabe.Schweigepflicht,
            new Schweigepflicht { Eintraege = [Mutter with { GueltigBis = Heute.AddDays(-200) }] }, Heute, "EF", Jetzt)).Fehler, f => f.Contains("vor „erteilt am“"));
        int vorher = s.Fassungen(id, Angabe.Schweigepflicht).Count;
        s.AngabeSpeichern(id, Angabe.Schweigepflicht, new Schweigepflicht { Eintraege = [Mutter with { PersonId = s.Personen()[0].Id }] }, Heute, "EF", Jetzt);
        s.AngabeSpeichern(id, Angabe.Schweigepflicht, new Schweigepflicht { Eintraege = [Mutter with { PersonId = s.Personen()[0].Id, WiderrufenAm = Heute }] },
            Heute, "EF", Jetzt.AddMinutes(1));
        Assert.Equal(vorher + 2, s.Fassungen(id, Angabe.Schweigepflicht).Count);   // der Widerruf ist eine neue Fassung
        var aktuell = s.Angaben(id, Heute)[Angabe.Schweigepflicht];
        Assert.True(SchweigepflichtRegeln.Leiste(aktuell, s.PersonenNamen(), Heute)[0].Widerrufen);
        Assert.Contains(AngabenRegeln.Zeilen(Angabe.Schweigepflicht, aktuell, _ => "", Heute), z => z.Wert.Contains("widerrufen am 09.11.2026"));
    }

    [Fact]
    public void Demo_gemischt_in_Uebersicht_und_Ueberleitung()
    {
        var alle = s.AlleBewohner();
        var namen = s.PersonenNamen();
        var fassungen = alle.Select(b => s.Angaben(b.Id!.Value, Heute).GetValueOrDefault(Angabe.Schweigepflicht)).ToList();
        Assert.Contains(fassungen, f => f is null);
        Assert.Contains(fassungen, f => f?.Zustand == AngabeZustand.Keine);
        var marken = fassungen.SelectMany(f => SchweigepflichtRegeln.Leiste(f, namen, Heute)).ToList();
        Assert.Contains(marken, m => m.Art == Entbindungsstand.Entbunden);
        Assert.Contains(marken, m => m.Widerrufen);
        Assert.Contains(marken, m => m.Text.Contains("abgelaufen am"));
        Assert.Contains(marken, m => m.Text.Contains("– keine Auskunft"));
        Assert.DoesNotContain(marken, m => m.Text.Contains(" ?"));   // jede Person aus dem Verzeichnis gefunden
        var heute = s.Heute(Jetzt);
        Assert.Contains(heute, h => h.Fristen.Any(m => m.Text.StartsWith("Entbindung endet")));
        Assert.Contains(heute, h => h.Auskunft.Kurz.Contains("entbunden"));
        Assert.Contains(heute, h => h.Auskunft.Kurz == "nicht erfasst");
        // Überleitungsbogen: der Abschnitt zeigt, was eingetragen ist.
        var b = alle[fassungen.FindIndex(f => f?.Lesen<Schweigepflicht>()?.Eintraege.Count > 0)];
        var bogen = UeberleitungRegeln.Zusammenstellen(s.UeberleitungDaten(b.Id!.Value, Jetzt), new UeberleitungEingabe { Zeitpunkt = Jetzt });
        Assert.Contains(bogen.Single(a => a.Titel == UeberleitungRegeln.Schweigepflicht).Zeilen, z => z == SchweigepflichtRegeln.AlleAnderen);
    }

    [Fact]
    public void Datenbank_wird_umgebaut_Fassungen_bleiben_und_Demo_wird_nachgeholt()
    {
        int vorher = Convert.ToInt32(Wert("SELECT COUNT(*) FROM akte_angaben"));
        Sql("DELETE FROM akte_angaben WHERE art = 'schweigepflicht'; DELETE FROM meta WHERE schluessel = 'demo_schweigepflicht'; PRAGMA user_version = 37");
        int ohne = Convert.ToInt32(Wert("SELECT COUNT(*) FROM akte_angaben"));
        string pfad = s.Pfad;
        s.Dispose();
        using var neu = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Equal(ohne, Convert.ToInt32(Wert(neu, "SELECT COUNT(*) FROM akte_angaben")));   // nichts verloren beim Umbau
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => Sql(neu, "UPDATE akte_angaben SET inhalt = ''"));   // weiter unveränderlich
        Assert.True(neu.DemoBeimErstenStart());   // die Demo-Schweigepflicht wird nachgeholt
        Assert.Equal(vorher, Convert.ToInt32(Wert(neu, "SELECT COUNT(*) FROM akte_angaben")));
    }

    private object? Wert(string sql) => Wert(s, sql);

    private static object? Wert(Speicher speicher, string sql)
    {
        using var befehl = speicher.Verbindung.CreateCommand();
        befehl.CommandText = sql;
        return befehl.ExecuteScalar();
    }

    private void Sql(string sql) => Sql(s, sql);

    private static void Sql(Speicher speicher, string sql)
    {
        using var befehl = speicher.Verbindung.CreateCommand();
        befehl.CommandText = sql;
        befehl.ExecuteNonQuery();
    }
}
