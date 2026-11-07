using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Therapie: Angebote, Fassungen „ab Datum“, Zuteilungen, freie Plätze, Überschneidungen, Demo.
[Collection("Datenordner")]
public sealed class TherapieTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private static readonly DateOnly Heute = new(2026, 11, 7);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public TherapieTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;

    private Angebot Neu(string name = "Testgruppe", string hoechstens = "2", DayOfWeek tag = DayOfWeek.Tuesday, string von = "14:00", string bis = "15:00") =>
        s.AngebotAnlegen(TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = name, Therapeut = "Test (ausgedacht)", Ort = "Raum 1", Termine = [(tag, von, bis)],
            Hoechstens = hoechstens, GiltAb = "01.11.2026", Kuerzel = "OP",
        }, Jetzt));

    [Fact]
    public void Pruefregeln()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => TherapieRegeln.AusEingabe(new AngebotEingabe { Hoechstens = "x" }, Jetzt)).Fehler;
        Assert.Contains("Name des Angebots fehlt.", fehler);
        Assert.Contains("Therapeutin oder Therapeut fehlt.", fehler);
        Assert.Contains("Mindestens ein Termin (Wochentag, von, bis).", fehler);
        Assert.Contains("Höchste Teilnehmerzahl: bitte eine Zahl von 1 bis 50.", fehler);
        Assert.Contains("Gilt ab fehlt.", fehler);
        fehler = Assert.Throws<Eingabefehler>(() => TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = "x", Therapeut = "y", Kuerzel = "OP", Hoechstens = "3", GiltAb = "01.11.2026",
            Termine = [(DayOfWeek.Monday, "10:00", "09:00"), (DayOfWeek.Friday, "10:00", "11:00"), (DayOfWeek.Friday, "10:30", "12:00")],
        }, Jetzt)).Fehler;
        Assert.Contains(fehler, f => f.Contains("„bis“ muss nach „von“"));
        Assert.Contains(fehler, f => f.Contains("überschneiden sich"));
        var einzel = TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = "x", Therapeut = "y", Kuerzel = "OP", Art = AngebotArt.Einzel, GiltAb = "01.11.2026", Termine = [(DayOfWeek.Monday, "9:00", "9:45")],
        }, Jetzt);
        Assert.Equal(1, einzel.Hoechstens);
        Assert.Equal("Mo 09:00–09:45", einzel.TermineText);
    }

    [Fact]
    public void Zuteilen_freie_Plaetze_und_voll()
    {
        var a = Neu();
        s.Zuteilen(a.Id, B("Ahorn"), Heute, null, "OP", Jetzt);
        Assert.Equal(1, s.Angebot(a.Id)!.FreiAm(Heute));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.Zuteilen(a.Id, B("Ahorn"), Heute.AddDays(3), null, "OP", Jetzt)).Fehler,
            f => f.Contains("schon zugeteilt"));
        s.Zuteilen(a.Id, B("Birke"), Heute, Heute.AddDays(13), "OP", Jetzt);
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.Zuteilen(a.Id, B("Clematis"), Heute.AddDays(5), null, "OP", Jetzt)).Fehler,
            f => f.Contains("voll"));
        s.Zuteilen(a.Id, B("Clematis"), Heute.AddDays(14), null, "OP", Jetzt);   // Birke ist dann nicht mehr dabei
        var angebot = s.Angebot(a.Id)!;
        Assert.Equal(["Ahorn", "Clematis"], angebot.TeilnehmerAm(Heute.AddDays(20)).Select(z => s.Bewohner(z.BewohnerId)!.Nachname).Order());
        Assert.Throws<Eingabefehler>(() => s.Zuteilen(a.Id, B("Distel"), new DateOnly(2026, 10, 1), null, "OP", Jetzt));   // vor Beginn
    }

    [Fact]
    public void Beenden_ist_ein_eigener_Eintrag()
    {
        var a = Neu();
        var z = s.Zuteilen(a.Id, B("Ahorn"), new DateOnly(2026, 11, 2), null, "OP", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.ZuteilungBeenden(z.Id, new DateOnly(2026, 11, 2), "OP", Jetzt));
        s.ZuteilungBeenden(z.Id, Heute, "OP", Jetzt);
        var neu = s.Angebot(a.Id)!.Zuteilungen.Single();
        Assert.Equal(Heute.AddDays(-1), neu.Ende);
        Assert.True(neu.GiltAm(Heute.AddDays(-1)));
        Assert.False(neu.GiltAm(Heute));
        Assert.Throws<Eingabefehler>(() => s.ZuteilungBeenden(z.Id, Heute.AddDays(5), "OP", Jetzt));   // schon beendet
        Assert.Contains(TherapieRegeln.Verlauf(s.Angebot(a.Id)!, new Dictionary<long, string> { [B("Ahorn")] = "Anton Ahorn" }),
            v => v.StartsWith("Anton Ahorn: Teilnahme beendet ab 07.11.2026"));
    }

    [Fact]
    public void Aenderung_ab_Datum_frueheres_bleibt()
    {
        var a = Neu();
        var neu = TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = "Testgruppe", Therapeut = "Test (ausgedacht)", Ort = "Raum 2", Termine = [(DayOfWeek.Thursday, "10:00", "11:00")],
            Hoechstens = "4", GiltAb = "16.11.2026", Kuerzel = "OP",
        }, Jetzt);
        a = s.AngebotAendern(a.Id, neu);
        Assert.Equal("Di 14:00–15:00", a.Am(Heute).TermineText);
        Assert.Equal("Do 10:00–11:00", a.Am(new DateOnly(2026, 11, 20)).TermineText);
        Assert.Equal(2, a.Fassungen.Count);
        s.Zuteilen(a.Id, B("Ahorn"), Heute, null, "OP", Jetzt);
        s.Zuteilen(a.Id, B("Birke"), Heute, null, "OP", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.AngebotAendern(a.Id, neu with { GiltAb = new DateOnly(2026, 12, 1), Hoechstens = 1 }));
        Assert.Throws<Eingabefehler>(() => s.AngebotAendern(a.Id, neu with { GiltAb = new DateOnly(2026, 10, 1) }));
    }

    [Fact]
    public void Ueberschneidung_wird_gemeldet()
    {
        var dienstag = Neu("Dienstag 14 Uhr");
        var auchDienstag = Neu("Dienstag halb drei", "4", DayOfWeek.Tuesday, "14:30", "15:30");
        var mittwoch = Neu("Mittwoch", "4", DayOfWeek.Wednesday);
        s.Zuteilen(dienstag.Id, B("Ahorn"), Heute, null, "OP", Jetzt);
        var konflikte = s.Ueberschneidungen(auchDienstag.Id, B("Ahorn"), Heute, null);
        Assert.Equal("Dienstag 14 Uhr", Assert.Single(konflikte).Was);
        Assert.Empty(s.Ueberschneidungen(mittwoch.Id, B("Ahorn"), Heute, null));
        Assert.Empty(s.Ueberschneidungen(auchDienstag.Id, B("Birke"), Heute, null));
    }

    [Fact]
    public void Nichts_wird_ueberschrieben()
    {
        var a = Neu();
        s.Zuteilen(a.Id, B("Ahorn"), Heute, null, "OP", Jetzt);
        using var befehl = s.Verbindung.CreateCommand();
        foreach (var sql in new[] { "UPDATE angebot_fassungen SET name = 'x'", "UPDATE zuteilungen SET ab = '2020-01-01'" })
        {
            befehl.CommandText = sql;
            Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
        }
    }

    [Fact]
    public void Demo_Angebote_relativ_zu_heute()
    {
        var alle = s.Angebote();
        Assert.Equal(7, alle.Count);
        Assert.Contains(alle, a => a.Fassungen.Count == 2);                                   // eine Änderung „ab Datum“
        Assert.Contains(alle.SelectMany(a => a.Zuteilungen), z => z.BeendetAb is not null);   // eine beendete Teilnahme
        Assert.Contains(alle.SelectMany(a => a.Zuteilungen), z => z.Ab > Heute);              // eine beginnt später
        Assert.All(alle, a => Assert.True(a.TeilnehmerAm(Heute).Count <= a.Am(Heute).Hoechstens));
        // Keine Demo-Zuteilung überschneidet sich mit einer anderen desselben Bewohners.
        foreach (var a in alle)
            foreach (var z in a.Zuteilungen)
                Assert.Empty(s.Ueberschneidungen(a.Id, z.BewohnerId, z.Ab, z.Ende));
        Assert.NotEmpty(s.TherapienAm(B("Ahorn"), Heute));
    }

    [Fact]
    public void Nach_dem_Update_kommt_Demo_Therapie_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM angebote; DELETE FROM meta WHERE schluessel = 'demo_therapie'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(7, s.Angebote().Count);
        Assert.False(s.DemoBeimErstenStart());
    }
}
