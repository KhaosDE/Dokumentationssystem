using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 30: Akte, Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger – Fassungen, drei Zustände,
// Fristen, Listen, Demo.
[Collection("Datenordner")]
public sealed class AkteAngabenTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private Speicher s;

    public AkteAngabenTests()
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

    private static AngabeFassung F(Angabe art, object? inhalt, int id = 1) =>
        new(id, 1, art, inhalt is null ? AngabeZustand.Keine : AngabeZustand.Eingetragen, inhalt is null ? "" : System.Text.Json.JsonSerializer.Serialize(inhalt, inhalt.GetType()),
            Heute.AddDays(-1), Jetzt, "EF");

    [Fact]
    public void Demo_hat_alle_drei_Zustaende_je_Reiter()
    {
        var alle = s.AlleBewohner();
        foreach (var art in Enum.GetValues<Angabe>())
        {
            var zustaende = alle.Select(b => s.Angaben(b.Id!.Value, Heute).GetValueOrDefault(art)?.Zustand.ToString() ?? "nicht erfasst").ToHashSet();
            Assert.Contains(nameof(AngabeZustand.Eingetragen), zustaende);
            Assert.Contains("nicht erfasst", zustaende);
            if (art is Angabe.Betreuung or Angabe.Angehoerige) Assert.Contains(nameof(AngabeZustand.Keine), zustaende);
        }
        Assert.Equal(Demodaten.Krankenkassen, s.Listeneintraege(Liste.Krankenkassen).Select(e => e.Name));
        Assert.All(s.Listeneintraege(Liste.Krankenkassen), e => Assert.True(e.Zugeordnet > 0));
        Assert.Equal(AngabenRegeln.Aufgabenvorgabe, s.Listeneintraege(Liste.Betreuungsaufgaben).Select(e => e.Name));
        // Die Fristen erscheinen in der Übersicht.
        Assert.Contains(s.Heute(Jetzt), h => h.Fristen.Any(m => m.Text.StartsWith("Kostenzusage endet")));
        Assert.Contains(s.Heute(Jetzt), h => h.Fristen.Any(m => m.Text.StartsWith("Betreuung")));
    }

    [Fact]
    public void Fristen_acht_vier_und_zwoelf_Wochen_vorher()
    {
        Marke? Eine(Angabe art, object inhalt) =>
            AngabenRegeln.Fristen(new Dictionary<Angabe, AngabeFassung> { [art] = F(art, inhalt) }, Heute).SingleOrDefault();
        Assert.Equal("Betreuung endet 04.01.2027", Eine(Angabe.Betreuung, new Betreuung { PersonId = 1, UeberpruefungBis = Heute.AddDays(56) })!.Text);
        Assert.Null(Eine(Angabe.Betreuung, new Betreuung { PersonId = 1, UeberpruefungBis = Heute.AddDays(57) }));
        Assert.StartsWith("Befreiung endet", Eine(Angabe.Krankenkasse, new Krankenkasse { Kasse = "K", Befreiung = JaNein.Ja, BefreitBis = Heute.AddDays(28) })!.Text);
        Assert.Null(Eine(Angabe.Krankenkasse, new Krankenkasse { Kasse = "K", Befreiung = JaNein.Nein, BefreitBis = Heute.AddDays(3) }));
        var bewilligt = new Kostentraeger { Name = "T", Bewilligungen = [new("L", null, Heute.AddDays(-300), Heute.AddDays(84)), new("L", null, Heute.AddDays(-700), Heute.AddDays(-301))] };
        Assert.StartsWith("Kostenzusage endet", Eine(Angabe.Kostentraeger, bewilligt)!.Text);
        var abgelaufen = Eine(Angabe.Kostentraeger, new Kostentraeger { Name = "T", Bewilligungen = [new("L", null, Heute.AddDays(-400), Heute.AddDays(-2))] })!;
        Assert.Equal(MarkenArt.Beschluss, abgelaufen.Art);
        Assert.StartsWith("Kostenzusage abgelaufen seit", abgelaufen.Text);
        Assert.Empty(AngabenRegeln.Fristen(new Dictionary<Angabe, AngabeFassung> { [Angabe.Betreuung] = F(Angabe.Betreuung, null) }, Heute));
    }

    [Fact]
    public void Fassungen_ab_Datum_nie_ueberschrieben_und_mit_Recht()
    {
        long id = s.AlleBewohner()[0].Id!.Value;
        int vorher = s.Fassungen(id, Angabe.Krankenkasse).Count;
        var neu = new Krankenkasse { Kasse = Demodaten.Krankenkassen[1], Nummer = "Z1", Befreiung = JaNein.NichtErfasst, Pflegegrad = 3 };
        s.AngabeSpeichern(id, Angabe.Krankenkasse, neu, Heute.AddDays(10), "EF", Jetzt);   // gilt erst in zehn Tagen
        Assert.Equal(vorher + 1, s.Fassungen(id, Angabe.Krankenkasse).Count);
        Assert.NotEqual(3, s.Angaben(id, Heute).GetValueOrDefault(Angabe.Krankenkasse)?.Lesen<Krankenkasse>()?.Pflegegrad);
        Assert.Equal(3, s.Angaben(id, Heute.AddDays(10))[Angabe.Krankenkasse].Lesen<Krankenkasse>()!.Pflegegrad);
        s.AngabeSpeichern(id, Angabe.Krankenkasse, null, Heute.AddDays(20), "EF", Jetzt);   // „keine Krankenkasse“
        Assert.Equal(AngabeZustand.Keine, s.Angaben(id, Heute.AddDays(20))[Angabe.Krankenkasse].Zustand);
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => Sql("UPDATE akte_angaben SET inhalt = ''"));
        // Ohne Recht „Bewohner anlegen und bearbeiten“ (Therapie) und ohne Anmeldung: nein.
        Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(id, Angabe.Krankenkasse, neu, Heute, "OP", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(id, Angabe.Krankenkasse, neu, Heute, "", Jetzt));
        // Prüfungen.
        var fehler = Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(id, Angabe.Kostentraeger,
            new Kostentraeger { Name = "", Bewilligungen = [new("", null, Heute, Heute.AddDays(-1))] }, Heute, "EF", Jetzt)).Fehler;
        Assert.Equal(3, fehler.Count);
        Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(id, Angabe.Betreuung, new Betreuung { PersonId = null }, Heute, "EF", Jetzt));
    }

    [Fact]
    public void Betreuung_gleicht_die_Betreuer_Zeile_an()
    {
        long id = s.AlleBewohner()[1].Id!.Value;
        var person = s.Personen(PersonGruppe.Betreuer)[0];
        s.AngabeSpeichern(id, Angabe.Betreuung, new Betreuung { Art = "Berufsbetreuer", PersonId = person.Id, Aufgaben = ["Post"] }, Heute, "EF", Jetzt);
        Assert.Equal(person.Name, s.Betreuer(id)!.Name);
        Assert.Equal(person.Email, s.Betreuer(id)!.Email);
        s.AngabeSpeichern(id, Angabe.Betreuung, null, Heute, "EF", Jetzt);
        Assert.True(s.Betreuer(id)!.Keine);
        string kurz = AngabenRegeln.Kurz(Angabe.Betreuung, s.Angaben(id, Heute)[Angabe.Betreuung], _ => "?", Heute);
        Assert.Equal("keine gesetzliche Betreuung", kurz);
        Assert.Equal(AngabenRegeln.NichtErfasst, AngabenRegeln.Kurz(Angabe.Kostentraeger, null, _ => "?", Heute));
    }

    [Fact]
    public void Zugeteilte_Aerzte_stehen_oben()
    {
        var aerzte = s.Aerzte();
        var zugeteilt = new AerzteAngabe
        {
            Hausarzt = new ZugeteilterArzt(ArztStand.Arzt, aerzte[^1].Id), Psychiater = new ZugeteilterArzt(ArztStand.Keiner),
            Weitere = [new WeitererArzt(aerzte[^2].Id, "Zahnarzt")],
        };
        var sortiert = AngabenRegeln.AerzteSortiert(aerzte, zugeteilt);
        Assert.Equal([aerzte[^1].Id, aerzte[^2].Id], sortiert.Take(2).Select(a => a.Id));
        Assert.Equal(aerzte.Count, sortiert.Count);
        Assert.Equal(aerzte.Select(a => a.Id), AngabenRegeln.AerzteSortiert(aerzte, null).Select(a => a.Id));
        var zeilen = AngabenRegeln.Zeilen(Angabe.Aerzte, F(Angabe.Aerzte, new AerzteAngabe()), _ => "?", Heute);
        Assert.All(zeilen.Take(2), z => Assert.True(z.Achtung));   // Hausarzt und Psychiater nicht erfasst
    }

    [Fact]
    public void HEB_uebernimmt_Kostentraeger_und_Aktenzeichen()
    {
        var b = s.AlleBewohner().First(x => s.Angaben(x.Id!.Value, Heute).ContainsKey(Angabe.Kostentraeger)
                                         && s.Angaben(x.Id!.Value, Heute)[Angabe.Kostentraeger].Zustand == AngabeZustand.Eingetragen);
        var k = s.Angaben(b.Id!.Value, Heute)[Angabe.Kostentraeger].Lesen<Kostentraeger>()!;
        var w = HebPdf.Werte(new HebBogen(1, b.Id!.Value, Bogenart.A, Heute, Heute.AddYears(1), null, Heute, []), b, new HebFassung(), k);
        Assert.Equal(k.Name, w["kostentraeger"]);
        Assert.Equal(k.Aktenzeichen, w["aktenzeichen"]);
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_Tabellen_und_Demo()
    {
        Sql("DROP TABLE akte_angaben; DROP TABLE krankenkassen; DROP TABLE kostentraeger; DROP TABLE betreuungsaufgaben; " +
            "DELETE FROM meta WHERE schluessel = 'demo_akte_angaben'; PRAGMA user_version = 34");
        string pfad = s.Pfad;
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.True(s.DemoBeimErstenStart());
        Assert.NotEmpty(s.Fassungen(s.AlleBewohner()[0].Id!.Value, Angabe.Krankenkasse).Concat(s.Fassungen(s.AlleBewohner()[1].Id!.Value, Angabe.Krankenkasse)));
        Assert.Equal(7, s.Listeneintraege(Liste.Betreuungsaufgaben).Count);
    }
}

// Bauplan 30a: Zuständigkeit Forensik und FOSA, Bestellweg der Medikamente.
[Collection("Datenordner")]
public sealed class ZustaendigkeitTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public ZustaendigkeitTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Zustaendigkeit? Z(Bewohner b) => s.Angaben(b.Id!.Value, Heute).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>();

    [Fact]
    public void Demo_je_Bereich_zwei_Forensik_drei_FOSA()
    {
        foreach (var bereich in new[] { "R1", "R2", "R3" })
        {
            var hier = s.AlleBewohner().Where(b => b.Wohnbereich == bereich).ToList();
            Assert.Equal(2, hier.Count(b => ZustaendigkeitRegeln.InForensik(Z(b))));
            Assert.Equal(3, hier.Count(b => ZustaendigkeitRegeln.InFosa(Z(b), Heute)));
            Assert.Single(hier, b => ZustaendigkeitRegeln.InForensik(Z(b)) && ZustaendigkeitRegeln.InFosa(Z(b), Heute));
        }
        var forensik = s.AlleBewohner().First(b => ZustaendigkeitRegeln.InForensik(Z(b)));
        Assert.Equal(Demodaten.Klinik, Z(forensik)!.Klinik);
        Assert.NotEmpty(Z(forensik)!.ForensikPersonen);
        Assert.Equal(Bestellweg.Forensik, s.Angaben(forensik.Id!.Value, Heute)[Angabe.Bestellweg].Lesen<BestellwegAngabe>()!.Weg);
        // In der Übersicht: Marken „Forensik“ und „FOSA“.
        var h = s.Heute(Jetzt).Single(x => x.Bewohner.Id == forensik.Id);
        Assert.Contains(UebersichtRegeln.Marken(h, Heute, 90), m => m.Art == MarkenArt.Zustaendig && m.Text == "Forensik");
    }

    [Fact]
    public void Vorschlag_Marken_und_Bestellung()
    {
        var forensik = new Zustaendigkeit { Forensik = JaNein.Ja, Klinik = "K", Fosa = JaNein.Ja, Ambulanz = "A", Ueberpruefung = Heute.AddDays(30) };
        Assert.Equal(Bestellweg.Forensik, ZustaendigkeitRegeln.Vorschlag(forensik, Heute));
        Assert.Equal(Bestellweg.Fosa, ZustaendigkeitRegeln.Vorschlag(forensik with { Forensik = JaNein.Nein }, Heute));
        Assert.Equal(Bestellweg.Hausarzt, ZustaendigkeitRegeln.Vorschlag(forensik with { Forensik = JaNein.Nein, FosaBis = Heute.AddDays(-1) }, Heute));
        Assert.Equal(["Forensik", "FOSA", "Forensik: Überprüfung 09.12.2026"], ZustaendigkeitRegeln.Marken(forensik, Heute).Select(m => m.Text));
        Assert.Empty(ZustaendigkeitRegeln.Marken(null, Heute));
        var b = s.AlleBewohner()[0];
        Assert.Equal($"Medikamentenbestellung {b.Name}, geb. {Datum.Text(b.Geburtsdatum)}", ZustaendigkeitRegeln.Betreff(b));
        var v = new Verordnung { Medikament = "Quetiapin Tabletten", Staerke = "25 mg" };
        string text = ZustaendigkeitRegeln.Bestelltext([v]);
        Assert.Contains("- Quetiapin Tabletten 25 mg", text);
        string mail = ZustaendigkeitRegeln.Mailto("fosa@example.org", "Betreff mit Ä", text);
        Assert.StartsWith("mailto:fosa@example.org?subject=Betreff%20mit%20%C3%84&body=", mail);
        Assert.Equal("Bestellung über: nicht erfasst", ZustaendigkeitRegeln.BestellungText(null, null));
        Assert.Equal("Bestellung über: FOSA · Frau X, 0000 1", ZustaendigkeitRegeln.BestellungText(new BestellwegAngabe { Weg = Bestellweg.Fosa },
            new Person { Name = "Frau X", Telefon = "0000 1" }));
        // Prüfung: „ja“ braucht Klinik bzw. Ambulanz.
        Assert.Throws<Eingabefehler>(() => s.AngabeSpeichern(b.Id!.Value, Angabe.Zustaendigkeit, new Zustaendigkeit { Forensik = JaNein.Ja }, Heute, "EF", Jetzt));
    }
}
