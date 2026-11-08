using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Kalender: Arten, Prüfregeln, mehrtägige Heimfahrt, mehrere Bewohner, ändern und absagen als
// Fassungen, Filter, Wochenplan, Demo.
[Collection("Datenordner")]
public sealed class KalenderTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);   // Samstag
    private static readonly DateOnly Heute = new(2026, 11, 7);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public KalenderTests()
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

    private KalenderFassung Fassung(Action<KalenderEingabe>? aendern = null)
    {
        var e = new KalenderEingabe
        {
            Art = KalenderArt.Bewohner, Titel = "Augenarzt", BeginnDatum = "10.11.2026", BeginnUhrzeit = "9:00", EndeUhrzeit = "10:00",
            Bewohner = [B("Birke")], Kuerzel = "AB",
        };
        aendern?.Invoke(e);
        return KalenderRegeln.AusEingabe(e, Jetzt);
    }

    [Fact]
    public void Pruefregeln()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => KalenderRegeln.AusEingabe(new KalenderEingabe { Art = KalenderArt.Heimfahrt }, Jetzt)).Fehler;
        Assert.Contains("Titel fehlt.", fehler);
        Assert.Contains("Bitte mindestens einen Bewohner wählen.", fehler);
        Assert.Contains("Beginn fehlt.", fehler);
        Assert.Contains("Ziel des Ausflugs fehlt.", Assert.Throws<Eingabefehler>(() => Fassung(e => { e.Art = KalenderArt.Ausflug; e.Titel = ""; })).Fehler);
        Assert.Contains("Eine Heimfahrt gehört zu genau einem Bewohner.",
            Assert.Throws<Eingabefehler>(() => Fassung(e => { e.Art = KalenderArt.Heimfahrt; e.Bewohner = [B("Birke"), B("Ahorn")]; })).Fehler);
        Assert.Contains("Das Ende liegt vor dem Beginn.", Assert.Throws<Eingabefehler>(() => Fassung(e => e.EndeUhrzeit = "8:00")).Fehler);
        var wohnbereich = Fassung(e => { e.Art = KalenderArt.Wohnbereich; e.Bewohner = []; e.Ganztags = true; });
        Assert.Equal("Di 10.11., ganztags", KalenderRegeln.ZeitText(wohnbereich));
        Assert.Equal("Di 10.11., 09:00–10:00 Uhr", KalenderRegeln.ZeitText(Fassung()));
    }

    [Fact]
    public void Heimfahrt_ueber_mehrere_Tage_und_mehrere_Bewohner()
    {
        var heim = s.KalenderAnlegen(Fassung(e =>
        {
            e.Art = KalenderArt.Heimfahrt; e.Titel = "Heimfahrt"; e.BeginnDatum = "13.11.2026"; e.BeginnUhrzeit = "16:00";
            e.EndeDatum = "15.11.2026"; e.EndeUhrzeit = "18:00";
        }));
        Assert.True(heim.Am(new DateOnly(2026, 11, 14)));
        Assert.Equal(3, Enumerable.Range(12, 5).Count(t => heim.Am(new DateOnly(2026, 11, t))));
        Assert.Equal(heim.Id, s.HeimfahrtJetzt(B("Birke"), new DateTime(2026, 11, 14, 12, 0, 0))!.Id);
        Assert.Null(s.HeimfahrtJetzt(B("Birke"), new DateTime(2026, 11, 15, 19, 0, 0)));

        var friseur = s.KalenderAnlegen(Fassung(e => { e.Titel = "Friseur"; e.Bewohner = [B("Birke"), B("Clematis")]; }));
        Assert.Contains(s.Kalender(Heute, Heute.AddDays(7), B("Birke")), k => k.Id == friseur.Id);
        Assert.Contains(s.Kalender(Heute, Heute.AddDays(7), B("Clematis")), k => k.Id == friseur.Id);
        Assert.DoesNotContain(s.Kalender(Heute, Heute.AddDays(7), B("Ahorn")), k => k.Id == friseur.Id);

        // Im Wochenplan: am ersten und letzten Tag mit Uhrzeit, dazwischen ganztags.
        var woche = s.Woche(B("Birke"), new DateOnly(2026, 11, 9)).Where(t => t.Quelle == TerminQuelle.Kalender).ToList();
        Assert.Contains(woche, t => t.Titel == "Friseur" && t.Tag == new DateOnly(2026, 11, 10));
        Assert.Equal(3, woche.Count(t => t.KalenderId == heim.Id));
        Assert.All(woche.Where(t => t.KalenderId == heim.Id), t => Assert.StartsWith("Heimfahrt bis So 15.11.", t.Titel));
    }

    [Fact]
    public void Aendern_und_absagen_sind_Fassungen()
    {
        var k = s.KalenderAnlegen(Fassung());
        k = s.KalenderAendern(k.Id, Fassung(e => { e.BeginnUhrzeit = "11:00"; e.EndeUhrzeit = "12:00"; e.Kuerzel = "CD"; }));
        Assert.Equal(2, k.Fassungen.Count);
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(k.Fassungen[0].Beginn));
        s.KalenderAbsagen(k.Id, "EF", Jetzt);
        Assert.DoesNotContain(s.Kalender(Heute, Heute.AddDays(7)), x => x.Id == k.Id);
        var verlauf = KalenderRegeln.Verlauf(s.KalenderTermin(k.Id)!, new Dictionary<long, string> { [B("Birke")] = "Berta Birke" });
        Assert.Equal(3, verlauf.Count);
        Assert.Contains("angelegt von AB", verlauf[0]);
        Assert.Contains("geändert von CD", verlauf[1]);
        Assert.Contains("abgesagt von EF", verlauf[2]);
        Assert.Throws<Eingabefehler>(() => s.KalenderAendern(k.Id, Fassung()));
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE kalender_fassungen SET titel = 'x'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Ansichten_Zeitraum_und_Blaettern()
    {
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)), KalenderRegeln.Zeitraum(KalenderAnsicht.Monat, Heute));
        Assert.Equal((new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8)), KalenderRegeln.Zeitraum(KalenderAnsicht.Woche, Heute));
        Assert.Equal(new DateOnly(2026, 12, 7), KalenderRegeln.Blaettern(KalenderAnsicht.Monat, Heute, 1));
        Assert.Equal("November 2026", KalenderRegeln.ZeitraumText(KalenderAnsicht.Monat, Heute));
    }

    [Fact]
    public void Demo_Kalender_relativ_zu_heute()
    {
        var alle = s.Kalender(Heute.AddDays(-30), Heute.AddDays(30));
        Assert.Equal(15, alle.Count);   // einer ist abgesagt; Brandschutzübung (Haus) seit Schritt 24; vier Termine aus Schritt 26h
        Assert.Contains(alle, k => k.Aktuell.Art == KalenderArt.Ausflug && k.Aktuell.Bewohner.Count == 4);
        Assert.Contains(alle, k => k.Fassungen.Count == 2 && k.Aktuell.Titel == "Zahnarzt");
        Assert.NotNull(s.HeimfahrtJetzt(B("Holunder"), Jetzt));
        Assert.Null(s.HeimfahrtJetzt(B("Ginster"), Jetzt));
        var arzt = Assert.Single(s.Kalender(Heute, Heute.AddDays(7), B("Ahorn")), k => k.Aktuell.Titel.StartsWith("Hausarzt"));
        Assert.Equal(DayOfWeek.Thursday, arzt.Aktuell.Beginn.DayOfWeek);
        Assert.True(arzt.Aktuell.Beginn > Jetzt);
    }

    [Fact]
    public void Nach_dem_Update_kommt_Demo_Kalender_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM kalender; DELETE FROM meta WHERE schluessel = 'demo_kalender'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.NotEmpty(s.Kalender(Heute.AddDays(-30), Heute.AddDays(30)));
        Assert.False(s.DemoBeimErstenStart());
    }
}
