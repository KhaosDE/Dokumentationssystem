using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;

namespace DokuLine.Tests;

// Bauplan 34: Abgängigkeitsmeldung – Meldung mit Pflichtangabe, Marke und Bericht, wer informiert/entwarnt wurde, „Zurück“,
// Meldeblatt, Personenbeschreibung mit Marke, Wartezeit in der Ausgangsliste.
[Collection("Datenordner")]
public sealed class AbgaengigkeitTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 16, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public AbgaengigkeitTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.ModulAnmelden(new AusgangslisteDaten());
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static AbgaengigkeitMeldung M(string kleidung = "blaue Jacke, graue Jogginghose, weiße Turnschuhe") => new()
    {
        VermisstSeit = Jetzt.AddMinutes(-45), ZuletztWo = "Garten", ZuletztWann = Jetzt.AddMinutes(-60), ZuletztVon = "EF",
        Kleidung = kleidung, Gefaehrdung = "braucht Abendmedikation", Orte = "Bahnhof Musterstadt", Kontakte = "Betreuer",
    };

    [Fact]
    public void Melden_setzt_Marke_und_wichtigen_Bericht_mit_Uebergabe()
    {
        var b = s.AlleBewohner()[0];
        long id = b.Id!.Value;
        int vorher = s.Berichte(id).Count;
        var a = s.AbgaengigkeitMelden(id, M(), "EF", Jetzt);
        Assert.True(a.Offen);
        Assert.Equal("abgängig seit 09.11. 15:15", AbgaengigkeitRegeln.MarkeText(a));
        Assert.Equal(MarkenArt.Abgaengig, AbgaengigkeitRegeln.Marke(a).Art);
        Assert.Equal(a.Id, s.OffeneAbgaengigkeit(id)?.Id);
        Assert.True(s.OffeneAbgaengigkeiten().ContainsKey(id));
        var berichte = s.Berichte(id);
        Assert.Equal(vorher + 1, berichte.Count);
        var bericht = berichte.Single(x => x.Text.StartsWith("Abgängigkeit gemeldet"));
        Assert.True(bericht.Wichtig);
        Assert.True(bericht.Uebergabe);
        Assert.Equal(DokuRegeln.ArtAbgaengigkeit, bericht.Art);   // Filter „Ausgang/Abgängigkeit“ in der Dokumentation
        Assert.Contains("Kleidung: blaue Jacke", bericht.Text);
        Assert.Contains("zuletzt gesehen: Garten um 15:00 (von EF)", bericht.Text);
        // Eine laufende Meldung je Bewohner.
        Assert.Contains("schon eine Meldung", Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitMelden(id, M(), "EF", Jetzt)).Fehler[0]);
        // Die Art kann man in den Einstellungen nicht umbenennen (sonst fände der Filter die Meldungen nicht).
        Assert.Throws<Eingabefehler>(() => s.ListeneintragUmbenennen(Liste.Berichtsarten, DokuRegeln.ArtAbgaengigkeit, "Weg"));
    }

    [Fact]
    public void Kleidung_ist_Pflicht_ohne_Kuerzel_nichts()
    {
        long id = s.AlleBewohner()[1].Id!.Value;
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitMelden(id, M(kleidung: "  "), "EF", Jetzt)).Fehler, f => f.StartsWith("Kleidung heute fehlt"));
        Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitMelden(id, M(), "", Jetzt));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitMelden(id, M() with { VermisstSeit = Jetzt.AddHours(2) }, "EF", Jetzt)).Fehler,
            f => f.Contains("Zukunft"));
        Assert.Null(s.OffeneAbgaengigkeit(id));
    }

    [Fact]
    public void Informiert_zurueck_entwarnt_und_Meldung_bleibt_unveraendert()
    {
        long id = s.AlleBewohner()[2].Id!.Value;
        var a = s.AbgaengigkeitMelden(id, M(), "EF", Jetzt);
        s.AbgaengigkeitInformiert(a.Id, "Polizei (110)", "POK Muster, Vorgang 0000/26", "EF", Jetzt.AddMinutes(5));
        s.AbgaengigkeitInformiert(a.Id, "Betreuer", "", "EF", Jetzt.AddMinutes(7));
        Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitInformiert(a.Id, " ", "", "EF", Jetzt));
        Assert.Contains("vor „vermisst seit“", Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitZurueck(a.Id, Jetzt.AddHours(-3), "", "EF", Jetzt)).Fehler[0]);
        s.AbgaengigkeitZurueck(a.Id, Jetzt.AddMinutes(50), "unverletzt, durchgefroren", "EF", Jetzt.AddMinutes(55));
        s.AbgaengigkeitEntwarnt(a.Id, "Polizei (110)", "EF", Jetzt.AddMinutes(58));
        var neu = s.Abgaengigkeiten(id).Single();
        Assert.False(neu.Offen);
        Assert.Equal(["Polizei (110)", "Betreuer"], neu.Informiert.Select(i => i.Stelle));
        Assert.Equal("POK Muster, Vorgang 0000/26", neu.Informiert[0].Hinweis);
        Assert.Equal(Jetzt.AddMinutes(5), neu.Informiert[0].Zeit);
        Assert.Equal(["Polizei (110)"], neu.Entwarnt.Select(e => e.Stelle));
        Assert.Equal("unverletzt, durchgefroren", neu.Zustand);
        Assert.Null(s.OffeneAbgaengigkeit(id));
        Assert.Contains(s.Berichte(id), x => x.Text == "Zurück um 09.11.2026 16:50. Zustand: unverletzt, durchgefroren." && x.Art == DokuRegeln.ArtAbgaengigkeit);
        Assert.Contains("geschlossen", Assert.Throws<Eingabefehler>(() => s.AbgaengigkeitZurueck(a.Id, Jetzt.AddMinutes(60), "", "EF", Jetzt)).Fehler[0]);
        // Danach ist eine neue Meldung möglich.
        Assert.True(s.AbgaengigkeitMelden(id, M(), "EF", Jetzt.AddHours(2)).Offen);
    }

    [Fact]
    public void Stellen_mit_Forensik_und_Liste_aus_den_Einstellungen()
    {
        Assert.Equal(AbgaengigkeitRegeln.StellenVorgabe, s.Meldestellen());
        s.ListeneintragAnlegen(Liste.Meldestellen, "Rufbereitschaft (0000 111111)");
        s.ListeneintragAktivSetzen(Liste.Meldestellen, "Gericht", false);
        var b = s.AlleBewohner().First(x => ZustaendigkeitRegeln.InForensik(s.Angaben(x.Id!.Value, Heute).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>()));
        var zust = s.Angaben(b.Id!.Value, Heute)[Angabe.Zustaendigkeit].Lesen<Zustaendigkeit>();
        var stellen = AbgaengigkeitRegeln.Stellen(s.Meldestellen(), zust, p => $"Person {p}", Heute);
        Assert.Contains("Rufbereitschaft (0000 111111)", stellen);
        Assert.DoesNotContain("Gericht", stellen);
        Assert.Contains(stellen, x => x.StartsWith("Forensik: Person "));
        Assert.Equal(AbgaengigkeitRegeln.StellenVorgabe.Length - 1 + 1 + zust!.ForensikPersonen.Count +
                     (ZustaendigkeitRegeln.InFosa(zust, Heute) ? zust.FosaPersonen.Count : 0), stellen.Count);
    }

    [Fact]
    public void Meldeblatt_nie_leer_mit_Beschreibung_Kleidung_und_Kontakt()
    {
        var mit = s.AlleBewohner().First(x => s.Angaben(x.Id!.Value, Heute).ContainsKey(Angabe.Personenbeschreibung));
        var a = s.AbgaengigkeitMelden(mit.Id!.Value, M() with { Orte = "" }, "EF", Jetzt);
        var blatt = AbgaengigkeitRegeln.Meldeblatt(s.UeberleitungDaten(mit.Id!.Value, Jetzt), a);
        Assert.Equal(["Vermisste Person", "Personenbeschreibung", "Bild", "Kleidung heute", "Zuletzt gesehen", "Gefährdung", "Mögliche Aufenthaltsorte",
            "Kontakte", "Kontakt der Einrichtung"], blatt.Select(x => x.Titel));
        Assert.All(blatt, x => Assert.NotEmpty(x.Zeilen));
        Assert.Contains(blatt[1].Zeilen, z => z.StartsWith("Größe: 1,"));
        Assert.Equal(["blaue Jacke, graue Jogginghose, weiße Turnschuhe"], blatt[3].Zeilen);
        Assert.Equal([AngabenRegeln.NichtErfasst], blatt[6].Zeilen);   // leer heißt „nicht erfasst“
        Assert.Contains(blatt[8].Zeilen, z => z.Contains("0000 555 000"));
        Assert.Contains(blatt[2].Zeilen, z => z.Contains("Platzhalter"));
        // Ohne Personenbeschreibung: „nicht erfasst“.
        var ohne = s.AlleBewohner().First(x => !s.Angaben(x.Id!.Value, Heute).ContainsKey(Angabe.Personenbeschreibung));
        var b = s.AbgaengigkeitMelden(ohne.Id!.Value, M(), "EF", Jetzt);
        Assert.Equal([AngabenRegeln.NichtErfasst], AbgaengigkeitRegeln.Meldeblatt(s.UeberleitungDaten(ohne.Id!.Value, Jetzt), b)[1].Zeilen);
        // Gefährdung nennt, was laut Plan regelmäßig gebraucht wird.
        var d = s.UeberleitungDaten(mit.Id!.Value, Jetzt);
        var gefaehrdung = blatt.Single(x => x.Titel == "Gefährdung").Zeilen;
        Assert.Equal(AbgaengigkeitRegeln.BenoetigteMedikamente(d).Count > 0, gefaehrdung[1].StartsWith("Benötigt laut Plan: "));
    }

    [Fact]
    public void Personenbeschreibung_Fassung_und_Marke_nach_12_Monaten()
    {
        var alle = s.AlleBewohner();
        var angaben = alle.Select(b => s.Angaben(b.Id!.Value, Heute).GetValueOrDefault(Angabe.Personenbeschreibung)).ToList();
        Assert.Contains(angaben, f => f is null);   // nicht erfasst
        Assert.Contains(angaben, f => f is not null && AbgaengigkeitRegeln.BeschreibungMarke(f, Heute) is not null);   // älter als 12 Monate
        Assert.Contains(angaben, f => f is not null && AbgaengigkeitRegeln.BeschreibungMarke(f, Heute) is null);
        Assert.Equal("Personenbeschreibung nicht erfasst", AbgaengigkeitRegeln.BeschreibungMarke(null, Heute)!.Text);
        long id = alle[3].Id!.Value;
        var f = s.AngabeSpeichern(id, Angabe.Personenbeschreibung, new Personenbeschreibung { Groesse = "1,70 m", Statur = "schlank" }, Heute, "EF", Jetzt);
        Assert.Null(AbgaengigkeitRegeln.BeschreibungMarke(f, Heute));
        Assert.NotNull(AbgaengigkeitRegeln.BeschreibungMarke(f, Heute.AddMonths(13)));
        Assert.Equal("1,70 m · schlank (Stand 09.11.2026)", AbgaengigkeitRegeln.BeschreibungKurz(f));
        Assert.Contains(AngabenRegeln.Zeilen(Angabe.Personenbeschreibung, f, _ => "", Heute), z => z.Beschriftung == "Größe" && z.Wert == "1,70 m");
        var keine = s.AngabeSpeichern(id, Angabe.Personenbeschreibung, null, Heute, "EF", Jetzt.AddMinutes(1));
        Assert.Equal("keine Personenbeschreibung", AbgaengigkeitRegeln.BeschreibungKurz(keine));
    }

    [Fact]
    public void Uebersicht_Wo_und_Wartezeit_in_der_Ausgangsliste()
    {
        var a = s.AbgaengigkeitMelden(s.AlleBewohner()[4].Id!.Value, M(), "EF", Jetzt);
        var wo = HeuteWichtigRegeln.Wo([new Marke(MarkenArt.Abwesend, "abwesend: Heimfahrt"), AbgaengigkeitRegeln.Marke(a)], [], Jetzt);
        Assert.Equal((AbgaengigkeitRegeln.MarkeText(a), MarkenArt.Abgaengig), (wo.Text, wo.Art));
        var bis = new TimeSpan(15, 0, 0);
        Assert.False(AbgaengigkeitRegeln.WartezeitVorbei(bis, new TimeSpan(15, 29, 0), 30));
        Assert.True(AbgaengigkeitRegeln.WartezeitVorbei(bis, new TimeSpan(15, 30, 0), 30));
        Assert.Equal("Rückkehrzeit überschritten seit 15:00 – Abgängigkeit melden?", AbgaengigkeitRegeln.UeberschrittenText(bis));
        Assert.Null(AbgaengigkeitRegeln.WartezeitPruefen(30));
        Assert.NotNull(AbgaengigkeitRegeln.WartezeitPruefen(4));
        Assert.NotNull(AbgaengigkeitRegeln.WartezeitPruefen(241));
    }

    [Fact]
    public void Ausgangsliste_fragt_nach_der_Wartezeit_und_zeigt_abgaengig()
    {
        var db = new Datenbank(s);
        var p = db.Platz(s.AlleBewohner().First(b => db.Platz(b.Id!.Value) is Platz x && DigitalerAusgang.Anspruch(x, "allein") > 0).Id!.Value)!;
        var eintrag = new AusgangEintrag { BewohnerId = p.BewohnerId, Datum = Jetzt.Date, Beginn = new TimeSpan(10, 0, 0), Art = "allein", HdzBeginn = "EF" };
        var bis = DigitalerAusgang.ZurueckBis(p, eintrag)!.Value;
        DigitalStand Stand(TimeSpan jetzt) => DigitalerAusgang.StandFuer(p, [eintrag], jetzt);
        var kurz = Jetzt.Date + bis + TimeSpan.FromMinutes(10);
        Assert.Contains(AusgangsMarken.Fuer(p, kurz, Stand(kurz.TimeOfDay)), m => m.Text.StartsWith("überfällig seit"));
        var lang = Jetzt.Date + bis + TimeSpan.FromMinutes(31);
        Assert.Contains(AusgangsMarken.Fuer(p, lang, Stand(lang.TimeOfDay), wartezeit: 30), m => m.Text == AbgaengigkeitRegeln.UeberschrittenText(bis));
        Assert.DoesNotContain(AusgangsMarken.Fuer(p, lang, Stand(lang.TimeOfDay), wartezeit: 60), m => m.Text.StartsWith("Rückkehrzeit"));
        var a = s.AbgaengigkeitMelden(p.BewohnerId, M(), "EF", Jetzt);
        var marken = AusgangsMarken.Fuer(p, lang, Stand(lang.TimeOfDay), wartezeit: 30, abgaengig: a);
        Assert.Equal(AbgaengigkeitRegeln.MarkeText(a), marken[0].Text);
        Assert.DoesNotContain(marken, m => m.Text.StartsWith("Rückkehrzeit"));   // schon gemeldet: keine Frage mehr
    }

    [Fact]
    public void Wartezeit_wird_gespeichert_falsche_Werte_gelten_nicht()
    {
        Assert.Equal(AbgaengigkeitRegeln.WartezeitVorgabe, Einstellungen.Laden().WartezeitMinuten);
        Einstellungen.Aendern(w => w with { WartezeitMinuten = 45 });
        Assert.Equal(45, Einstellungen.Laden().WartezeitMinuten);
        Assert.Throws<Eingabefehler>(() => Einstellungen.Aendern(w => w with { WartezeitMinuten = 1 }));
        File.WriteAllText(Pfade.ImDatenordner(Einstellungen.Datei), "{\"wartezeit_minuten\": 9999}");
        Assert.Equal(AbgaengigkeitRegeln.WartezeitVorgabe, Einstellungen.Laden().WartezeitMinuten);
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_Tabellen()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DROP TABLE abgaengigkeit_ereignisse; DROP TABLE abgaengigkeiten; DROP TABLE meldestellen; PRAGMA user_version = 36";
            befehl.ExecuteNonQuery();
        }
        string pfad = s.Pfad;
        s.Dispose();
        using var neu = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Equal(AbgaengigkeitRegeln.StellenVorgabe, neu.Meldestellen());
        Assert.Empty(neu.Abgaengigkeiten());
        Assert.True(neu.AbgaengigkeitMelden(neu.AlleBewohner()[0].Id!.Value, M(), "EF", Jetzt).Offen);
    }
}
