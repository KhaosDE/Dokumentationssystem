using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Wochenplan: Regelwoche „ab Datum“, Therapien automatisch, Teilnahme, Überschneidung, Verlauf, Demo.
[Collection("Datenordner")]
public sealed class WochenplanTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);   // Samstag
    private static readonly DateOnly Heute = new(2026, 11, 7);
    private static readonly DateOnly Montag = new(2026, 11, 2);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public WochenplanTests()
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

    private PlanEintrag Neu(long bewohner, DayOfWeek tag, string von, string bis, string ab = "02.11.2026", string titel = "Testtermin") =>
        WochenplanRegeln.AusEingabe(new PlanEingabe { Tag = tag, Von = von, Bis = bis, Titel = titel, Ab = ab, Kuerzel = "AB" }, bewohner, Jetzt);

    [Fact]
    public void Woche_und_Kalenderwoche()
    {
        Assert.Equal(Montag, WochenplanRegeln.Montag(Heute));
        Assert.Equal(Montag, WochenplanRegeln.Montag(Montag));
        Assert.Equal(new DateOnly(2026, 11, 9), WochenplanRegeln.Montag(new DateOnly(2026, 11, 15)));   // Sonntag
        Assert.Equal(45, WochenplanRegeln.Kalenderwoche(Heute));
        Assert.Equal("KW 45 · 02.11.–08.11.2026", WochenplanRegeln.WochenText(Montag));
    }

    [Fact]
    public void Pruefregeln()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => WochenplanRegeln.AusEingabe(new PlanEingabe { Von = "10:00", Bis = "09:00" }, 1, Jetzt)).Fehler;
        Assert.Contains("Was? Titel fehlt.", fehler);
        Assert.Contains("Wochentag fehlt.", fehler);
        Assert.Contains("„Bis“ muss nach „von“ liegen.", fehler);
        Assert.Contains("Gilt ab fehlt.", fehler);
    }

    [Fact]
    public void Therapien_erscheinen_automatisch_mit_Teilnahme()
    {
        var woche = s.Woche(B("Ahorn"), Montag);
        var holz = woche.Where(t => t.Titel == "Holzwerkstatt").ToList();
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday], holz.Select(t => t.Tag.DayOfWeek));
        Assert.All(holz, t => Assert.Equal(TerminQuelle.Therapie, t.Quelle));
        Assert.All(holz, t => Assert.NotEqual(Teilnahme.Offen, t.Teilnahme));   // Demo: vergangene Termine haben eine Teilnahme
        Assert.Contains(woche, t => t.Titel == "Spaziergang mit der Wohngruppe" && t.Quelle == TerminQuelle.Plan);
        Assert.Equal(woche.OrderBy(t => t.Tag).ThenBy(t => t.Von).Select(t => (t.Tag, t.Von)), woche.Select(t => (t.Tag, t.Von)));

        var naechste = s.Woche(B("Ahorn"), Montag.AddDays(7));
        var montagHolz = naechste.First(t => t.Titel == "Holzwerkstatt");
        Assert.Equal(Teilnahme.Offen, montagHolz.Teilnahme);
        s.TeilnahmeSetzen(montagHolz.ZuteilungId!.Value, montagHolz.Tag, Teilnahme.Abgesagt, "AB", Jetzt);
        Assert.Equal(Teilnahme.Abgesagt, s.Woche(B("Ahorn"), Montag.AddDays(7)).First(t => t.Titel == "Holzwerkstatt").Teilnahme);
        Assert.Throws<Eingabefehler>(() => s.TeilnahmeSetzen(montagHolz.ZuteilungId!.Value, montagHolz.Tag, Teilnahme.Teilgenommen, "AB", Jetzt));   // Zukunft
        Assert.Throws<Eingabefehler>(() => s.TeilnahmeSetzen(montagHolz.ZuteilungId!.Value, montagHolz.Tag.AddDays(1), Teilnahme.Abgesagt, "AB", Jetzt));   // kein Termin
    }

    [Fact]
    public void Aendern_ab_Datum_frueheres_bleibt()
    {
        var e = s.PlanEintragAnlegen(Neu(B("Birke"), DayOfWeek.Thursday, "17:00", "18:00", titel: "Chor"));
        var neu = s.PlanEintragAendern(e.Id, Neu(B("Birke"), DayOfWeek.Thursday, "18:00", "19:00", ab: "09.11.2026", titel: "Chor"));
        Assert.Equal("17:00–18:00", s.Woche(B("Birke"), Montag).Single(t => t.Titel == "Chor").ZeitText);
        Assert.Equal("18:00–19:00", s.Woche(B("Birke"), Montag.AddDays(7)).Single(t => t.Titel == "Chor").ZeitText);
        Assert.Equal(e.Id, neu.Ersetzt);
        Assert.Throws<Eingabefehler>(() => s.PlanEintragAendern(e.Id, Neu(B("Birke"), DayOfWeek.Friday, "18:00", "19:00", ab: "16.11.2026")));   // schon beendet
        s.PlanEintragEntfernen(neu.Id, new DateOnly(2026, 11, 16), "AB", Jetzt);
        Assert.DoesNotContain(s.Woche(B("Birke"), Montag.AddDays(14)), t => t.Titel == "Chor");
        var verlauf = WochenplanRegeln.Verlauf(s.PlanEintraege(B("Birke")));
        Assert.Contains(verlauf, v => v.StartsWith("neu ab 02.11.2026: Chor Do 17:00–18:00"));
        Assert.Contains(verlauf, v => v.StartsWith("geändert ab 09.11.2026: Chor Do 18:00–19:00"));
        Assert.Contains(verlauf, v => v.StartsWith("entfernt ab 16.11.2026: Chor"));
    }

    [Fact]
    public void Ueberschneidung_mit_Therapie_und_Plan()
    {
        var montagVormittag = Neu(B("Ahorn"), DayOfWeek.Monday, "10:00", "11:00");
        Assert.Contains(s.PlanUeberschneidungen(montagVormittag), k => k.StartsWith("Holzwerkstatt"));
        var sonntag = Neu(B("Ahorn"), DayOfWeek.Sunday, "14:30", "15:30");
        Assert.Contains(s.PlanUeberschneidungen(sonntag), k => k.StartsWith("Spaziergang"));
        Assert.Empty(s.PlanUeberschneidungen(Neu(B("Ahorn"), DayOfWeek.Sunday, "19:00", "20:00")));
        // Auch die Therapie-Zuteilung warnt jetzt vor freien Einträgen.
        var garten = s.AngebotAnlegen(TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = "Garten", Therapeut = "x", Termine = [(DayOfWeek.Sunday, "14:00", "15:00")], Hoechstens = "4", GiltAb = "01.11.2026", Kuerzel = "OP",
        }, Jetzt));
        Assert.Contains(s.Ueberschneidungen(garten.Id, B("Ahorn"), Heute, null), k => k.Was.StartsWith("Spaziergang"));
    }

    [Fact]
    public void Nichts_wird_ueberschrieben()
    {
        using var befehl = s.Verbindung.CreateCommand();
        foreach (var sql in new[] { "UPDATE plan_eintraege SET titel = 'x'", "UPDATE teilnahmen SET status = 'offen'" })
        {
            befehl.CommandText = sql;
            Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
        }
    }

    [Fact]
    public void Demo_Wochenplan()
    {
        var ahorn = s.PlanEintraege(B("Ahorn"));
        Assert.Contains(ahorn, e => e.Ersetzt is not null && e.Titel == "Wäschetag" && e.Termin.Tag == DayOfWeek.Tuesday);
        Assert.Contains(s.PlanEintraege(B("Birke")), e => e.EndetAb is not null && e.Titel.StartsWith("Einkaufen"));
        Assert.DoesNotContain(s.Woche(B("Birke"), Montag), t => t.Titel.StartsWith("Einkaufen"));
        // Keine Überschneidungen in der Demo-Regelwoche.
        foreach (var b in s.AlleBewohner())
            foreach (var e in s.PlanEintraege(b.Id!.Value).Where(e => e.GiltAm(Heute)))
                Assert.Empty(s.PlanUeberschneidungen(e with { Ersetzt = e.Id }));
        Assert.NotEmpty(s.Teilnahmen());
    }

    [Fact]
    public void Nach_dem_Update_kommt_Demo_Wochenplan_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM plan_eintraege; DELETE FROM teilnahmen; DELETE FROM meta WHERE schluessel = 'demo_wochenplan'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.NotEmpty(s.PlanEintraege(B("Ahorn")));
        Assert.False(s.DemoBeimErstenStart());
    }
}
