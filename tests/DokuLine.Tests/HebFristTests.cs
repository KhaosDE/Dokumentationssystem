using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Bauplan 52: HEB-Fälligkeit – Stand aus Ereignissen (eingetragen, kein HEB nötig, nicht erfasst mit Vorschlag), Stand des
// Bogens, Stufen der Erinnerung mit Marken, Aufgabe einmal je Abschnitt, Abgabe mit nächster Fälligkeit, Kalender, Prüfseite, Demo.
[Collection("Datenordner")]
public sealed class HebFristTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 14, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public HebFristTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static readonly Bewohner B = new() { Id = 1, Vorname = "A", Nachname = "Ahorn (Test)", Zimmer = "101", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };

    private static HebBogen Bogen(long id, DateOnly bis, bool fertig = true, DateOnly? erstellt = null) =>
        new(id, 1, Bogenart.B, bis.AddMonths(-12), bis, null, erstellt ?? bis, [new HebFassung { Fertig = fertig }]);

    private static HebFristEreignis E(string art, DateOnly? datum, int minuten, long? bogen = null, string grund = "") =>
        new(minuten, 1, art, datum, grund, bogen, null, "RS", Jetzt.AddMinutes(minuten));

    private static HebFristStand S(int tage, HebFristZustand z = HebFristZustand.Eingetragen) =>
        new(B, z, Heute.AddDays(tage), "", HebBogenStand.Offen, null, null, null);

    [Fact]
    public void Stand_aus_Ereignissen_und_Vorschlag()
    {
        var neu = B with { Aufnahmedatum = Heute.AddMonths(-1) };
        var ohne = HebFristRegeln.Stand(neu, [], [], null, 12);
        Assert.Equal((HebFristZustand.NichtErfasst, Heute.AddMonths(2), HebBogenStand.Offen), (ohne.Zustand, ohne.FaelligAm, ohne.Stand));   // Erstbericht

        var k = new Kostentraeger { Bewilligungen = [new Bewilligung("Wohnen", null, Heute.AddYears(-1), Heute.AddDays(100))] };
        var alt = Bogen(10, Heute.AddMonths(-11));
        Assert.Equal(Heute.AddDays(100), HebFristRegeln.Stand(B, [], [alt], k, 12).FaelligAm);           // Ende der Bewilligung
        var ohneBewilligung = HebFristRegeln.Stand(B, [], [alt], null, 12);
        Assert.Equal(alt.Bis.AddMonths(12), ohneBewilligung.FaelligAm);                                  // letzter Bogen plus Frist
        Assert.Equal(HebBogenStand.Offen, ohneBewilligung.Stand);                                        // der alte Bogen zählt nicht

        var f = Heute.AddDays(20);
        var eingetragen = HebFristRegeln.Stand(B, [E("faellig", f, 1, grund: "Schreiben des Bezirks")], [alt], k, 12);
        Assert.Equal((HebFristZustand.Eingetragen, f, "Schreiben des Bezirks"), (eingetragen.Zustand, eingetragen.FaelligAm, eingetragen.Grund));
        Assert.StartsWith($"fällig am {Datum.Text(f)} · offen", HebFristRegeln.KurzText(eingetragen));

        var entwurf = Bogen(11, f, fertig: false, erstellt: Heute);
        Assert.Equal(HebBogenStand.InArbeit, HebFristRegeln.Stand(B, [E("faellig", f, 1)], [alt, entwurf], k, 12).Stand);
        var fertig = entwurf with { Fassungen = [new HebFassung { Fertig = true }] };
        Assert.Equal(HebBogenStand.Fertig, HebFristRegeln.Stand(B, [E("faellig", f, 1)], [alt, fertig], k, 12).Stand);

        // Abgegeben schließt die Fälligkeit; ohne nächste gilt der Vorschlag. Die laufende Bewilligung zählt dann nicht mehr (der
        // HEB wird vor ihrem Ende abgegeben), sondern der Bogen plus Frist – oder eine Bewilligung, die nach der Abgabe beginnt.
        var abgegeben = HebFristRegeln.Stand(B, [E("faellig", f, 1), E("abgegeben", Heute, 2, bogen: 11)], [alt, fertig], k, 12);
        Assert.Equal((HebFristZustand.NichtErfasst, HebBogenStand.Abgegeben, f.AddMonths(12)), (abgegeben.Zustand, abgegeben.Stand, abgegeben.FaelligAm));
        var folge = k with { Bewilligungen = [.. k.Bewilligungen, new Bewilligung("Wohnen", null, Heute.AddDays(101), Heute.AddDays(500))] };
        Assert.Equal(Heute.AddDays(500), HebFristRegeln.Stand(B, [E("faellig", f, 1), E("abgegeben", Heute, 2, bogen: 11)], [alt, fertig], folge, 12).FaelligAm);
        Assert.True(HebFristRegeln.AufgabeNoetig(abgegeben with { FaelligAm = Heute.AddDays(20) }, Heute, 8, 4));   // auch ohne nächste Fälligkeit
        Assert.Equal($"abgegeben am {Datum.Text(Heute)} (RS)", HebFristRegeln.StandText(abgegeben));
        var naechste = HebFristRegeln.Stand(B, [E("faellig", f, 1), E("abgegeben", Heute, 2, bogen: 11), E("faellig", Heute.AddYears(1), 3)], [alt, fertig], k, 12);
        Assert.Equal((HebFristZustand.Eingetragen, HebBogenStand.Offen, Heute.AddYears(1)), (naechste.Zustand, naechste.Stand, naechste.FaelligAm));

        var kein = HebFristRegeln.Stand(B, [E("faellig", f, 1), E("kein", null, 2, grund: "Auszug geplant")], [], null, 12);
        Assert.Equal((HebFristZustand.KeinHeb, (DateOnly?)null), (kein.Zustand, kein.FaelligAm));
        Assert.Equal("kein HEB nötig (Auszug geplant)", HebFristRegeln.KurzText(kein));
        Assert.Null(HebFristRegeln.Marke(kein, Heute, 8, 4));
        Assert.Null(HebFristRegeln.QmText(kein, Heute));
    }

    [Fact]
    public void Stufen_Marken_Kalender_und_Pruefseite()
    {
        HebStufe Stufe(HebFristStand x) => HebFristRegeln.Stufe(x, Heute, 8, 4);
        (MarkenArt, string)? Marke(HebFristStand x) => HebFristRegeln.Marke(x, Heute, 8, 4) is Marke m ? (m.Art, m.Text) : null;
        Assert.Equal(HebStufe.Keine, Stufe(S(57)));
        Assert.Null(Marke(S(57)));
        Assert.Equal(HebStufe.Hinweis, Stufe(S(56)));
        Assert.Equal((MarkenArt.Wichtig, "HEB fällig in 8 Wochen"), Marke(S(56)));
        Assert.Equal(HebStufe.Achtung, Stufe(S(28)));
        Assert.Equal((MarkenArt.NichtErfasst, "HEB fällig in 4 Wochen"), Marke(S(28)));
        Assert.Equal((MarkenArt.NichtErfasst, $"HEB fällig am {Datum.Text(Heute.AddDays(3))}"), Marke(S(3)));
        Assert.Equal((MarkenArt.NichtErfasst, "HEB fällig heute"), Marke(S(0)));
        Assert.Equal(HebStufe.Ueberfaellig, Stufe(S(-1)));
        Assert.Equal((MarkenArt.Ueberfaellig, $"HEB überfällig seit {Datum.Text(Heute.AddDays(-1))}"), Marke(S(-1)));
        // Nicht erfasst: mindestens Achtung, mit „(Vorschlag)“; ohne Stufe „HEB-Frist nicht erfasst“.
        Assert.Equal((MarkenArt.NichtErfasst, "HEB-Frist nicht erfasst"), Marke(S(100, HebFristZustand.NichtErfasst)));
        Assert.Equal((MarkenArt.NichtErfasst, "HEB fällig in 8 Wochen (Vorschlag)"), Marke(S(50, HebFristZustand.NichtErfasst)));
        Assert.Equal("HEB-Frist nicht erfasst", HebFristRegeln.QmText(S(100, HebFristZustand.NichtErfasst), Heute));
        Assert.StartsWith("HEB überfällig seit", HebFristRegeln.QmText(S(-1), Heute));
        Assert.Null(HebFristRegeln.QmText(S(10), Heute));

        var k = HebFristRegeln.Kalendereintrag(S(10))!;
        Assert.Equal(("HEB fällig", true, 1L, Heute.AddDays(10)), (k.Aktuell.Titel, k.Aktuell.Ganztags, k.Aktuell.Bewohner.Single(), DateOnly.FromDateTime(k.Aktuell.Beginn)));
        Assert.True(k.Am(Heute.AddDays(10)) && !k.Am(Heute.AddDays(11)));
        Assert.Null(HebFristRegeln.Kalendereintrag(S(10, HebFristZustand.NichtErfasst)));   // nur eingetragene Daten
    }

    [Fact]
    public void Setzen_Erinnerung_Abgabe_und_Rechte()
    {
        var b = s.AlleBewohner().First(x => s.HebBoegen(x.Id!.Value) is { Count: > 0 } h && h[^1].Fertig);
        long id = b.Id!.Value;
        Assert.Throws<Eingabefehler>(() => s.HebFaelligSetzen(id, Heute.AddDays(30), " ", "RS", Jetzt));   // ohne Grund
        Assert.Throws<Eingabefehler>(() => s.HebFaelligSetzen(id, Heute.AddDays(30), "Schreiben", "XY", Jetzt));   // ohne Recht
        s.HebFaelligSetzen(id, Heute.AddDays(30), "Schreiben des Bezirks", "RS", Jetzt);
        Assert.Equal((HebFristZustand.Eingetragen, Heute.AddDays(30)), (s.HebFrist(id, Heute).Zustand, s.HebFrist(id, Heute).FaelligAm));
        Assert.Contains(s.HebFristKalender(Heute, Heute.AddDays(40)), k => k.Aktuell.Bewohner.Single() == id);

        // Erste Stufe: eine Aufgabe, nicht doppelt; mit Entwurf wird sie erledigt.
        int Offen() => s.Aufgaben(Jetzt, auchErledigte: true).Count(a => a.BewohnerId == id && a.Text.StartsWith($"HEB-Bogen für {b.Name}") && a.Offen);
        s.HebErinnerungen(Jetzt);
        Assert.Equal(1, Offen());
        s.HebErinnerungen(Jetzt);
        Assert.Equal(1, Offen());
        var entwurf = s.HebAnlegen(id, Bogenart.B, s.HebBoegen(id)[^1].Bis, Heute.AddDays(30), "RS");
        Assert.Equal(HebBogenStand.InArbeit, s.HebFrist(id, Heute).Stand);
        s.HebErinnerungen(Jetzt);
        Assert.Equal(0, Offen());

        // Abgabe: nicht als Entwurf; danach die nächste Fälligkeit, der Abschnitt beginnt neu.
        Assert.Throws<Eingabefehler>(() => s.HebAbgegeben(id, Heute, Heute.AddYears(1), "RS", Jetzt));
        s.HebSpeichern(new HebFassung { BogenId = entwurf.Id, Fertig = true, Erstellt = Jetzt, Kuerzel = "RS" });
        Assert.Throws<Eingabefehler>(() => s.HebAbgegeben(id, Heute.AddDays(1), null, "RS", Jetzt));          // in der Zukunft
        Assert.Throws<Eingabefehler>(() => s.HebAbgegeben(id, Heute, Heute, "RS", Jetzt));                    // nächste nicht danach
        s.HebAbgegeben(id, Heute, Heute.AddYears(1), "RS", Jetzt);
        var danach = s.HebFrist(id, Heute);
        Assert.Equal((HebFristZustand.Eingetragen, Heute.AddYears(1), HebBogenStand.Offen, (long?)null), (danach.Zustand, danach.FaelligAm, danach.Stand, danach.AufgabeId));
        Assert.Equal(entwurf.Id, danach.LetzteAbgabe!.BogenId);

        s.HebKeinerNoetig(id, "Auszug geplant", "RS", Jetzt.AddMinutes(1));
        Assert.Equal(HebFristZustand.KeinHeb, s.HebFrist(id, Heute).Zustand);
        Assert.Throws<Eingabefehler>(() => s.HebAbgegeben(id, Heute, null, "RS", Jetzt.AddMinutes(2)));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE heb_frist SET grund = 'x'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Uebersicht_und_Pruefseite_nutzen_die_Frist()
    {
        var alle = s.HebFristen(s.AlleBewohner(), Heute);
        var ueberfaellig = alle.First(x => HebFristRegeln.Stufe(x, Heute, 8, 4) == HebStufe.Ueberfaellig && x.Zustand == HebFristZustand.Eingetragen);
        var heute = s.Heute(Jetzt).Single(h => h.Bewohner.Id == ueberfaellig.Bewohner.Id);
        Assert.Contains(heute.Fristen, m => m.Art == MarkenArt.Ueberfaellig && m.Text.StartsWith("HEB überfällig seit"));   // Kasten „Fristen“
        Assert.Null(heute.Heb);
        var hinweis = alle.First(x => HebFristRegeln.Stufe(x, Heute, 8, 4) == HebStufe.Hinweis && x.Zustand == HebFristZustand.Eingetragen);
        var h2 = s.Heute(Jetzt).Single(h => h.Bewohner.Id == hinweis.Bewohner.Id);
        Assert.Equal(MarkenArt.Wichtig, h2.Heb!.Art);
        Assert.DoesNotContain(h2.Fristen, m => m.Text.StartsWith("HEB"));
        Assert.Contains(UebersichtRegeln.Marken(h2, Heute, 90), m => m.Text.StartsWith("HEB fällig in"));

        var pruef = s.Pruefliste([ueberfaellig.Bewohner], Heute).Single();
        Assert.Contains(pruef.Ergebnisse, p => p.Punkt == Pruefpunkt.Heb && !p.Ok && p.Text.StartsWith("HEB überfällig"));
    }

    [Fact]
    public void Demo_je_Bereich_ueberfaellig_bald_kein_und_nicht_erfasst()
    {
        var alle = s.HebFristen(s.AlleBewohner(), Heute);
        foreach (var bereich in alle.GroupBy(x => x.Bewohner.Wohnbereich).Where(g => g.Count() >= 9))
        {
            Assert.Equal(1, bereich.Count(x => x.Zustand == HebFristZustand.Eingetragen && HebFristRegeln.Stufe(x, Heute, 8, 4) == HebStufe.Ueberfaellig));
            Assert.Contains(bereich, x => x.Zustand == HebFristZustand.Eingetragen && HebFristRegeln.Stufe(x, Heute, 8, 4) == HebStufe.Achtung);
            Assert.Contains(bereich, x => x.Zustand == HebFristZustand.KeinHeb);
        }
        Assert.Contains(alle, x => x.Zustand == HebFristZustand.NichtErfasst);
        Assert.Contains(alle, x => x.Stand == HebBogenStand.InArbeit);   // Kiefer: Entwurf
        Assert.All(alle.Where(x => x.Zustand == HebFristZustand.NichtErfasst), x => Assert.NotEqual(HebStufe.Ueberfaellig, HebFristRegeln.Stufe(x, Heute, 8, 4)));
    }
}
