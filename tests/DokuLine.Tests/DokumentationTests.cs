using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Bereich „Dokumentation“: Filter, die drei Marken, Gliederung nach Tagen, gespeicherte Filter und die
// Berichtsart (auch in einer Datenbank aus der Zeit davor).
[Collection("Datenordner")]
public sealed class DokumentationTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 15, 20, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private Speicher s;

    public DokumentationTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static Bericht B(DateTime zeit, string text = "Hat gekocht.", string art = DokuRegeln.ArtAlltag,
        bool wichtig = false,
        bool bedarf = false, int[]? bereiche = null) => new()
        {
            BewohnerId = 1, Zeitpunkt = zeit, Schicht = BerichtRegeln.SchichtZu(zeit), Verfasser = "AB", Text = text,
            Art = art,
            Wichtig = wichtig, Bedarfsgabe = bedarf, Bereiche = bereiche ?? [1], Erfasst = zeit,
        };

    private void Sql(string text)
    {
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = text;
        befehl.ExecuteNonQuery();
    }

    [Fact]
    public void Drei_Marken_und_Medikation_geaendert()
    {
        Assert.Empty(DokuRegeln.Marken(B(Jetzt)));
        Assert.Equal([DokuMarke.Wichtig], DokuRegeln.Marken(B(Jetzt, wichtig: true)));
        Assert.Equal([DokuMarke.MedikationGeaendert], DokuRegeln.Marken(B(Jetzt, art: DokuRegeln.ArtMedikation)));
        // Eine Bedarfsgabe ist keine Änderung der Medikation.
        Assert.Equal([DokuMarke.Bedarfsgabe], DokuRegeln.Marken(B(Jetzt, art: DokuRegeln.ArtMedikation, bedarf: true)));
        Assert.Equal(["Wichtig", "Medikation geändert", "Bedarfsgabe"],
            Enum.GetValues<DokuMarke>().Select(DokuRegeln.MarkeText));
    }

    [Fact]
    public void Zeitraum_und_Filter()
    {
        var heute = B(Jetzt);
        var vorSechs = B(Jetzt.AddDays(-6));
        var vorSieben = B(Jetzt.AddDays(-7));
        var vorDreissig = B(Jetzt.AddDays(-30));
        var standard = new DokuFilter();
        Assert.Equal(Zeitraum.SiebenTage, standard.Zeitraum);
        Assert.True(DokuRegeln.Passt(heute, standard, Heute));
        Assert.True(DokuRegeln.Passt(vorSechs, standard, Heute));
        Assert.False(DokuRegeln.Passt(vorSieben, standard, Heute));
        Assert.False(DokuRegeln.Passt(vorSechs, standard with { Zeitraum = Zeitraum.Heute }, Heute));
        Assert.True(DokuRegeln.Passt(vorSieben, standard with { Zeitraum = Zeitraum.DreissigTage }, Heute));
        Assert.False(DokuRegeln.Passt(vorDreissig, standard with { Zeitraum = Zeitraum.DreissigTage }, Heute));
        var frei = standard with { Zeitraum = Zeitraum.Frei, Von = Heute.AddDays(-31), Bis = Heute.AddDays(-29) };
        Assert.True(DokuRegeln.Passt(vorDreissig, frei, Heute));
        Assert.False(DokuRegeln.Passt(heute, frei, Heute));

        Assert.False(DokuRegeln.Passt(heute, standard with { Wichtig = true }, Heute));
        Assert.True(DokuRegeln.Passt(B(Jetzt, wichtig: true), standard with { Wichtig = true }, Heute));
        Assert.True(DokuRegeln.Passt(B(Jetzt, art: "Krise"), standard with { Art = "Krise" }, Heute));
        Assert.False(DokuRegeln.Passt(heute, standard with { Art = "Krise" }, Heute));
        Assert.True(DokuRegeln.Passt(B(Jetzt, art: DokuRegeln.ArtVisite), standard with { Visite = true }, Heute));
        Assert.False(DokuRegeln.Passt(B(Jetzt, art: DokuRegeln.ArtMedikation, bedarf: true),
            standard with { MedikationGeaendert = true }, Heute));
        Assert.True(DokuRegeln.Passt(B(Jetzt, art: DokuRegeln.ArtMedikation, bedarf: true),
            standard with { Bedarfsgabe = true }, Heute));
        Assert.False(DokuRegeln.Passt(heute, standard with { Bereich = 2 }, Heute));
        Assert.True(DokuRegeln.Passt(B(Jetzt, bereiche: []), standard with { Bereich = BerichtFilter.Allgemein },
            Heute));
        Assert.True(DokuRegeln.Passt(heute, standard with { Verfasser = "ab" }, Heute));   // Kürzel ohne Groß/klein
        Assert.False(DokuRegeln.Passt(heute, standard with { MitNachtrag = true }, Heute));
        Assert.True(DokuRegeln.Passt(heute, standard with { Suche = " GEKOCHT " }, Heute));
        Assert.False(DokuRegeln.Passt(heute, standard with { Suche = "Ausflug" }, Heute));
    }

    [Fact]
    public void Nach_Tagen_neueste_oben_mit_Tagestext()
    {
        var a = B(Jetzt.AddHours(-1)) with { Id = 1 };
        var b = B(Jetzt) with { Id = 2 };
        var c = B(Jetzt.AddDays(-1)) with { Id = 3 };
        var tage = DokuRegeln.NachTagen([c, a, b]);
        Assert.Equal([Heute, Heute.AddDays(-1)], tage.Select(t => t.Tag));
        Assert.Equal([2L, 1L], tage[0].Berichte.Select(x => x.Id!.Value));
        Assert.Equal("Montag, 09.11.2026 · 2 Berichte", DokuRegeln.TagText(Heute, 2));
        Assert.Equal("Sonntag, 08.11.2026 · 1 Bericht", DokuRegeln.TagText(Heute.AddDays(-1), 1));
        Assert.Equal(["Früh", "Spät", "Nacht"], new[] { Schicht.Frueh, Schicht.Spaet,
            Schicht.Nacht }.Select(DokuRegeln.SchichtWort));
    }

    [Fact]
    public void Fetter_Anfang_der_Visite_und_Fundstellen()
    {
        Assert.Equal("Visite Psychiater – Dr. Test",
            DokuRegeln.FettAnfang(B(Jetzt, "Visite Psychiater – Dr. Test, dabei: AB: Schlaf besprochen.",
                DokuRegeln.ArtVisite)));
        Assert.Equal("Visite Hausarzt – Dr. Test", DokuRegeln.FettAnfang(B(Jetzt,
            "Visite Hausarzt – Dr. Test: Blutdruck.", DokuRegeln.ArtVisite)));
        Assert.Null(DokuRegeln.FettAnfang(B(Jetzt, "Visite: kein Visitenbericht.")));   // nur Berichte der Art „Visite“
        Assert.Equal([(4, 4), (14, 4)], DokuRegeln.Fundstellen("Ein Test, ein test.", "TEST"));
        Assert.Empty(DokuRegeln.Fundstellen("Ein Test.", "  "));
    }

    [Fact]
    public void Gesetzte_Filter_als_Marken_mit_x()
    {
        Assert.Empty(DokuRegeln.Gesetzt(new DokuFilter()));
        var f = new DokuFilter
        {
            Zeitraum = Zeitraum.DreissigTage, Wichtig = true, Art = "Krise", Bereich = 3,
            Suche = " Garten "
        };
        var gesetzt = DokuRegeln.Gesetzt(f);
        Assert.Equal(["30 Tage", "Wichtig", "Art: Krise", "Lebensbereich 3", "„Garten“"], gesetzt.Select(g => g.Text));
        Assert.Equal(f with { Wichtig = false }, gesetzt[1].Ohne);
        Assert.Equal(f with { Zeitraum = Zeitraum.SiebenTage }, gesetzt[0].Ohne);
        var frei = new DokuFilter { Zeitraum = Zeitraum.Frei, Von = new DateOnly(2026, 10, 1) };
        Assert.Equal("01.10.2026 – …", DokuRegeln.Gesetzt(frei)[0].Text);
    }

    [Fact]
    public void Gespeicherte_Filter_je_Kuerzel()
    {
        var krisen = new DokuFilter { Zeitraum = Zeitraum.DreissigTage, Art = "Krise" };
        s.FilterSpeichern("ab", "Krisen der letzten 30 Tage", krisen);
        s.FilterSpeichern("AB", "Nur Wichtiges", new DokuFilter { Wichtig = true });
        s.FilterSpeichern("AB", "krisen der letzten 30 tage",
            krisen with { Zeitraum = Zeitraum.Heute });   // gleicher Name: ersetzt
        var liste = s.GespeicherteFilter("AB");
        Assert.Equal(["Nur Wichtiges", "krisen der letzten 30 tage"], liste.Select(g => g.Name));
        Assert.Equal(krisen with { Zeitraum = Zeitraum.Heute }, liste[1].Filter);
        Assert.Empty(s.GespeicherteFilter("CD"));
        s.FilterLoeschen("AB", "Nur Wichtiges");
        Assert.Single(s.GespeicherteFilter("AB"));
        Assert.Throws<Eingabefehler>(() => s.FilterSpeichern("AB", "  ", krisen));
        Assert.Throws<Eingabefehler>(() => s.FilterSpeichern("", "Ohne Anmeldung", krisen));
        Assert.Empty(DokuRegeln.AusJson("kein json"));   // beschädigt: leer statt Absturz
    }

    [Fact]
    public void Berichtsart_beim_Schreiben_und_aus_Bedarf_und_Visite()
    {
        long id = s.AlleBewohner().First(b => b.Nachname == "Ahorn").Id!.Value;
        var e = new BerichtEingabe
        {
            BewohnerId = id, Datum = "09.11.2026", Uhrzeit = "14:30", Verfasser = "AB", Bereiche = [2],
            Text = "Streit beim Essen.", Art = "Konflikt",
        };
        var gespeichert = s.BerichtSpeichern(BerichtRegeln.AusEingabe(e, Jetzt));
        Assert.Equal("Konflikt", s.Berichte(id).Single(b => b.Id == gespeichert.Id).Art);
        e.Art = "";
        Assert.Equal(DokuRegeln.ArtAlltag, BerichtRegeln.AusEingabe(e, Jetzt).Art);   // Vorgabe „Alltag“

        // Demo: Visitenberichte haben die Art „Visite“, Bedarfsgaben „Medikation“ mit Marke „Bedarfsgabe“.
        var alle = s.AlleBewohner().SelectMany(b => s.Berichte(b.Id!.Value)).ToList();
        Assert.Contains(alle, b => b.Art == DokuRegeln.ArtVisite && b.Text.StartsWith("Visite "));
        Assert.All(alle.Where(b => b.Text.StartsWith("Visite Psychiater") || b.Text.StartsWith("Visite Hausarzt")),
            b => Assert.Equal(DokuRegeln.ArtVisite, b.Art));
        Assert.True(alle.Select(b => b.Art).Distinct().Count() >= 4);   // Demo-Berichte mit passenden Arten
        var bedarf = s.AlleBewohner().SelectMany(b => s.Verordnungen(b.Id!.Value)).First(v => v.Bedarf);
        var gabe = s.BedarfGeben(new BedarfEingabe
        {
            VerordnungId = bedarf.Id, Uhrzeit = "15:00", Menge = "1",
            Anlass = "unruhig"
        }, "AB", Jetzt);
        var bericht = s.Berichte(bedarf.BewohnerId).Single(b => b.Id == gabe.BerichtId);
        Assert.Equal(DokuRegeln.ArtMedikation, bericht.Art);
        Assert.True(bericht.Bedarfsgabe);
        Assert.Equal([DokuMarke.Bedarfsgabe], DokuRegeln.Marken(bericht));
        Assert.Contains(s.Berichte(bedarf.BewohnerId, new DokuFilter { Bedarfsgabe = true }, Heute),
            b => b.Id == bericht.Id);
    }

    [Fact]
    public void Berichtsarten_in_den_Einstellungen()
    {
        Assert.Equal(DokuRegeln.Berichtsarten, s.Berichtsarten());
        s.ListeneintragAnlegen(Liste.Berichtsarten, "Gesundheit");
        Assert.Equal("Gesundheit", s.Berichtsarten()[^1]);
        // Umbenennen nimmt die Berichte mit; der Text bleibt unverändert.
        long id = s.AlleBewohner().First().Id!.Value;
        var e = new BerichtEingabe
        {
            BewohnerId = id, Datum = "09.11.2026", Uhrzeit = "10:00", Verfasser = "AB",
            Bereiche = [1], Text = "Krise.", Art = "Krise"
        };
        var bericht = s.BerichtSpeichern(BerichtRegeln.AusEingabe(e, Jetzt));
        s.ListeneintragUmbenennen(Liste.Berichtsarten, "Krise", "Krisensituation");
        var neu = s.Berichte(id).Single(b => b.Id == bericht.Id);
        Assert.Equal("Krisensituation", neu.Art);
        Assert.Equal("Krise.", neu.Text);
        Assert.Throws<Eingabefehler>(() => s.ListeneintragLoeschen(Liste.Berichtsarten,
            "Krisensituation"));   // noch zugeordnet
        s.ListeneintragAktivSetzen(Liste.Berichtsarten, "Krisensituation", false);
        Assert.DoesNotContain("Krisensituation", s.Berichtsarten());
        // Was das Programm selbst vergibt, bleibt.
        foreach (string fest in DokuRegeln.FesteArten)
        {
            Assert.Throws<Eingabefehler>(() => s.ListeneintragUmbenennen(Liste.Berichtsarten, fest, fest + "x"));
            Assert.Throws<Eingabefehler>(() => s.ListeneintragAktivSetzen(Liste.Berichtsarten, fest, false));
            Assert.Throws<Eingabefehler>(() => s.ListeneintragLoeschen(Liste.Berichtsarten, fest));
        }
        // Die Datenbank lässt weiter keine Änderung am Bericht selbst zu, nur an der Art.
        var fehler = Assert.Throws<SqliteException>(
            () => Sql($"UPDATE berichte SET text = 'anders' WHERE id = {bericht.Id}"));
        Assert.Contains("nicht überschrieben", fehler.Message);
        Sql($"UPDATE berichte SET art = 'Alltag' WHERE id = {bericht.Id}");
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_die_Berichtsart()
    {
        // Wie vor Schritt 31: keine Spalte „art“, kein Verzeichnis der Arten, der alte Schutz vor Änderungen.
        var bedarf = s.AlleBewohner().SelectMany(b => s.Verordnungen(b.Id!.Value)).First(v => v.Bedarf);
        var gabe = s.BedarfGeben(new BedarfEingabe
        {
            VerordnungId = bedarf.Id, Uhrzeit = "15:00", Menge = "1",
            Anlass = "unruhig"
        }, "AB", Jetzt);
        Sql("""
            DROP TRIGGER berichte_bleiben;
            DROP TABLE berichtsarten;
            ALTER TABLE berichte DROP COLUMN art;
            CREATE TRIGGER berichte_bleiben BEFORE UPDATE ON berichte
                BEGIN SELECT RAISE(ABORT, 'Berichte werden nicht überschrieben – Korrekturen sind Nachträge.'); END;
            ALTER TABLE aufgaben DROP COLUMN bereich;
            ALTER TABLE aufgabe_vorlagen DROP COLUMN bereich;
            PRAGMA user_version = 30;
            """);
        string pfad = s.Pfad;
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        var alle = s.AlleBewohner().SelectMany(b => s.Berichte(b.Id!.Value)).ToList();
        Assert.All(alle.Where(b => b.Text.StartsWith("Visite ")), b => Assert.Equal(DokuRegeln.ArtVisite, b.Art));
        Assert.Equal(DokuRegeln.ArtMedikation, alle.Single(b => b.Id == gabe.BerichtId).Art);
        Assert.Contains(alle, b => b.Art == DokuRegeln.ArtAlltag);
        Assert.Equal(DokuRegeln.Berichtsarten, s.Berichtsarten());
        Assert.Throws<SqliteException>(() => Sql($"UPDATE berichte SET text = 'anders' WHERE id = {gabe.BerichtId}"));
    }
}
