using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 39: Maßnahmen – Fälligkeit je Rhythmus, Fassungen ab Datum, Beenden mit Grund, Bestätigen einmal je Dienst mit
// Eintrag in der Dokumentation, Rechte (Fachkräfte und Bezugspflege), Lebensbereiche, Reflexion, Demo, Datenbank.
[Collection("Datenordner")]
public sealed class MassnahmenTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);   // Montag, Frühdienst
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public MassnahmenTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static MassnahmeFassung F(MassnahmeRhythmus r = MassnahmeRhythmus.Taeglich, Schicht schicht = Schicht.Frueh, DateOnly? ab = null) => new()
    {
        Titel = "Zimmerhygiene (Test)", Lebensbereich = 2, Art = "Anleitung", Rhythmus = r, Schicht = schicht, Minuten = 30,
        GiltAb = ab ?? Heute.AddDays(-14), Wochentage = r switch
        {
            MassnahmeRhythmus.Wochentage => [DayOfWeek.Monday, DayOfWeek.Thursday],
            MassnahmeRhythmus.AlleNWochen => [DayOfWeek.Monday],
            _ => [],
        },
    };

    private long Leer() => s.AlleBewohner()[0].Id!.Value;

    [Fact]
    public void Faellig_je_Rhythmus_Fassung_ab_Datum_und_Ende()
    {
        var mo = Heute;   // Montag
        Assert.True(MassnahmenRegeln.AmTag(F(), mo));
        Assert.True(MassnahmenRegeln.AmTag(F(MassnahmeRhythmus.Wochentage), mo));
        Assert.False(MassnahmenRegeln.AmTag(F(MassnahmeRhythmus.Wochentage), mo.AddDays(1)));
        var zweiWochen = F(MassnahmeRhythmus.AlleNWochen, ab: mo.AddDays(-14)) with { AlleWochen = 2 };
        Assert.True(MassnahmenRegeln.AmTag(zweiWochen, mo));
        Assert.False(MassnahmenRegeln.AmTag(zweiWochen, mo.AddDays(7)));
        Assert.True(MassnahmenRegeln.AmTag(zweiWochen, mo.AddDays(14)));
        Assert.True(MassnahmenRegeln.AmTag(F(MassnahmeRhythmus.Monatlich) with { MonatsTag = 9 }, mo));
        Assert.False(MassnahmenRegeln.AmTag(F(MassnahmeRhythmus.BeiBedarf), mo));
        Assert.Equal("Mo, Do", MassnahmenRegeln.RhythmusText(F(MassnahmeRhythmus.Wochentage)));

        // Zwei Fassungen: ab morgen im Spätdienst. Heute gilt noch die alte, „bis“ und „beendet“ schließen ab.
        var alt = F() with { Erfasst = Jetzt.AddDays(-14) };
        var neu = F(schicht: Schicht.Spaet, ab: mo.AddDays(1)) with { Erfasst = Jetzt };
        var m = new Massnahme(1, 1, [alt, neu]);
        Assert.Equal(Schicht.Frueh, m.Am(mo)!.Schicht);
        Assert.Equal(Schicht.Spaet, m.Am(mo.AddDays(1))!.Schicht);
        Assert.Single(MassnahmenRegeln.Termine([m], new SchichtTermin(mo, Schicht.Frueh), []));
        Assert.Empty(MassnahmenRegeln.Termine([m], new SchichtTermin(mo.AddDays(1), Schicht.Frueh), []));
        Assert.Single(MassnahmenRegeln.Termine([m], new SchichtTermin(mo.AddDays(1), Schicht.Spaet), []));
        Assert.False((m with { BeendetAm = mo }).Laeuft(mo));
        Assert.False(new Massnahme(1, 1, [alt with { GiltBis = mo.AddDays(-1) }]).Laeuft(mo));

        // Offene oben, nach Uhrzeit; die Nacht reicht über Mitternacht.
        var frueh = new Massnahme(2, 1, [F() with { Titel = "B", Uhrzeit = new TimeOnly(10, 0) }]);
        var frueher = new Massnahme(3, 1, [F() with { Titel = "C", Uhrzeit = new TimeOnly(7, 0) }]);
        var erledigt = new Massnahme(4, 1, [F() with { Titel = "A" }]);
        var bestaetigt = new MassnahmeBestaetigung(9, 4, 1, mo, Schicht.Frueh, MassnahmeErgebnis.Durchgefuehrt, "", 30, "", "AB", Jetzt, 2, "A");
        Assert.Equal(["C", "B", "A"], MassnahmenRegeln.Termine([erledigt, frueh, frueher], new SchichtTermin(mo, Schicht.Frueh), [bestaetigt]).Select(t => t.Fassung.Titel));
        var nacht = new MassnahmeTermin(frueh, F(schicht: Schicht.Nacht) with { Uhrzeit = new TimeOnly(2, 0) }, new SchichtTermin(mo, Schicht.Nacht), null);
        Assert.Equal(mo.AddDays(1).ToDateTime(new TimeOnly(2, 0)), nacht.Uhrzeit);
    }

    [Fact]
    public void Pruefen_und_Texte()
    {
        Assert.Empty(MassnahmenRegeln.Pruefen(F()));
        Assert.Equal(3, MassnahmenRegeln.Pruefen(F() with { Titel = " ", Lebensbereich = 0, Minuten = 999 }).Count);
        Assert.NotEmpty(MassnahmenRegeln.Pruefen(F(MassnahmeRhythmus.Wochentage) with { Wochentage = [] }));
        Assert.NotEmpty(MassnahmenRegeln.Pruefen(F(MassnahmeRhythmus.AlleNWochen) with { AlleWochen = 1 }));
        Assert.NotEmpty(MassnahmenRegeln.Pruefen(F(MassnahmeRhythmus.Monatlich) with { MonatsTag = 31 }));
        Assert.Contains(MassnahmenRegeln.Pruefen(F() with { Uhrzeit = new TimeOnly(15, 0) }), f => f.Contains("Frühdienst"));
        Assert.Empty(MassnahmenRegeln.Pruefen(F(schicht: Schicht.Nacht) with { Uhrzeit = new TimeOnly(2, 30) }));
        Assert.NotEmpty(MassnahmenRegeln.Pruefen(F() with { GiltBis = Heute.AddDays(-30) }));
        Assert.NotNull(MassnahmenRegeln.BestaetigungPruefen(MassnahmeErgebnis.Abgelehnt, " ", 0));
        Assert.Null(MassnahmenRegeln.BestaetigungPruefen(MassnahmeErgebnis.Durchgefuehrt, "", 20));
        Assert.Equal("3 Std. 40 Min.", MassnahmenRegeln.Dauer(220));
        Assert.Equal("Maßnahme „Zimmerhygiene“: abgelehnt (wollte nicht).", MassnahmenRegeln.BerichtText("Zimmerhygiene", MassnahmeErgebnis.Abgelehnt, "wollte nicht", 30, ""));
        Assert.Equal("Maßnahmen: 2 durchgeführt, 1 abgelehnt", MassnahmenRegeln.Zusammenfassung([
            MassnahmenRegeln.BerichtText("A", MassnahmeErgebnis.Durchgefuehrt, "", 10, ""), MassnahmenRegeln.BerichtText("B", MassnahmeErgebnis.Durchgefuehrt, "", 0, "gut"),
            MassnahmenRegeln.BerichtText("C", MassnahmeErgebnis.Abgelehnt, "müde", 0, "")]));
        Assert.Equal("Movicol", MassnahmenRegeln.MedikamentImTitel("Morgens ein Beutel Movicol", ["Movicol", "Macrogol"]));
        Assert.Null(MassnahmenRegeln.MedikamentImTitel("Zimmerhygiene", ["Movicol"]));
        Assert.Equal("Testmittel-ratio AL", MassnahmenRegeln.MedikamentImTitel("Abends testmittel-ratio al geben", ["Testmittel-ratio AL"]));   // mehrere Wörter
        Assert.Null(MassnahmenRegeln.MedikamentImTitel("Movicolbeutel holen", ["Movicol"]));   // nur ganze Namen

        // Reflexion: nur gezählt, je Maßnahme eine Zeile.
        MassnahmeBestaetigung B(MassnahmeErgebnis e, int min) => new(0, 7, 1, Heute, Schicht.Frueh, e, "", min, "", "AB", Jetzt, 2, "Zimmerhygiene");
        var zeilen = MassnahmenRegeln.ReflexionZeilen([B(MassnahmeErgebnis.Durchgefuehrt, 100), B(MassnahmeErgebnis.Durchgefuehrt, 120), B(MassnahmeErgebnis.Abgelehnt, 0)]);
        Assert.Equal((2, "Zimmerhygiene: 2 von 3 durchgeführt, 1 abgelehnt, 3 Std. 40 Min."), Assert.Single(zeilen));
    }

    [Fact]
    public void Lebensbereiche_zaehlen_geschriebene_Berichte_im_Quartal()
    {
        Bericht B(int tageVorher, int bereich, string art = DokuRegeln.ArtAlltag) => new()
        {
            BewohnerId = 1, Zeitpunkt = Jetzt.AddDays(-tageVorher), Schicht = Schicht.Frueh, Verfasser = "AB", Text = "x", Bereiche = [bereich], Art = art,
        };
        var stand = MassnahmenRegeln.Lebensbereiche([B(3, 1), B(50, 1), B(40, 2), B(1, 2, MassnahmenRegeln.ArtBericht)], Jetzt, 4);
        Assert.Equal(5, stand.Count);
        Assert.Equal(1, stand[0].BerichteImQuartal);   // Quartal ab 01.10.
        Assert.Null(stand[0].Marke);
        Assert.Equal("seit 5 Wochen kein Bericht", stand[1].Marke!.Text);   // der Eintrag der Maßnahme zählt nicht
        Assert.Equal("noch kein Bericht", stand[2].Marke!.Text);
        Assert.Equal(new DateOnly(2026, 12, 31), stand[0].NaechsteReflexion);
    }

    [Fact]
    public void Anlegen_Aendern_Beenden_mit_Rechten()
    {
        long id = Leer();
        var bezug = s.Bezug(id).Bezugspflege!.Kuerzel;
        Assert.Throws<Eingabefehler>(() => s.MassnahmeAnlegen(id, F(), "KL", Jetzt));   // Assistenzkraft, nicht Bezugspflege
        Assert.Throws<Eingabefehler>(() => s.MassnahmeAnlegen(id, F(), "OP", Jetzt));   // Therapie
        Assert.Throws<Eingabefehler>(() => s.MassnahmeAnlegen(id, F(), "", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.MassnahmeAnlegen(id, F() with { Titel = "" }, "EF", Jetzt));
        s.BezugspflegeSetzen(id, "KL", Heute, "EF");
        Assert.Null(s.MassnahmenAnlegenGrund("KL", id));   // als Bezugspflege darf sie
        var m = s.MassnahmeAnlegen(id, F(), "KL", Jetzt);
        Assert.Equal("KL", m.Fassungen[0].Kuerzel);
        s.BezugspflegeSetzen(id, bezug, Heute, "EF");

        s.MassnahmeAendern(m.Id, F(schicht: Schicht.Spaet, ab: Heute.AddDays(1)) with { Minuten = 45 }, "EF", Jetzt.AddMinutes(5));
        var geaendert = s.Massnahmen(id).Single(x => x.Id == m.Id);
        Assert.Equal(2, geaendert.Fassungen.Count);
        Assert.Equal(30, geaendert.Am(Heute)!.Minuten);
        Assert.Equal(45, geaendert.Am(Heute.AddDays(1))!.Minuten);
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => Sql("UPDATE massnahme_fassungen SET titel = 'x'"));

        Assert.Throws<Eingabefehler>(() => s.MassnahmeBeenden(m.Id, " ", "EF", Heute));
        s.MassnahmeBeenden(m.Id, "Ziel erreicht", "EF", Heute.AddDays(2));
        Assert.DoesNotContain(s.Massnahmen(id), x => x.Id == m.Id);
        var beendet = s.Massnahmen(id, auchBeendete: true).Single(x => x.Id == m.Id);
        Assert.Equal(("Ziel erreicht", "EF", Heute.AddDays(2)), (beendet.BeendetGrund, beendet.BeendetVon, beendet.BeendetAm!.Value));
        Assert.Throws<Eingabefehler>(() => s.MassnahmeAendern(m.Id, F(), "EF", Jetzt));
    }

    [Fact]
    public void Bestaetigen_einmal_je_Dienst_mit_Bericht()
    {
        long id = Leer();
        var dienst = SchichtTermin.Zu(Jetzt);
        var m = s.MassnahmeAnlegen(id, F(), "EF", Jetzt);
        var termin = Assert.Single(s.MassnahmenImDienst(dienst, [id]), t => t.Massnahme.Id == m.Id);
        Assert.True(termin.Offen);
        Assert.Throws<Eingabefehler>(() => s.MassnahmeBestaetigen(m.Id, dienst, MassnahmeErgebnis.Durchgefuehrt, "", 30, "", "", Jetzt));   // nicht angemeldet
        Assert.Throws<Eingabefehler>(() => s.MassnahmeBestaetigen(m.Id, dienst, MassnahmeErgebnis.Abgelehnt, "", 0, "", "KL", Jetzt));    // ohne Grund
        var b = s.MassnahmeBestaetigen(m.Id, dienst, MassnahmeErgebnis.Durchgefuehrt, "", 25, "gut mitgemacht", "KL", Jetzt);
        Assert.Equal((25, "KL", 2, "Zimmerhygiene (Test)"), (b.Minuten, b.Kuerzel, b.Lebensbereich, b.Titel));
        var bericht = s.Berichte(id).Single(x => x.Id == b.BerichtId);
        Assert.Equal((MassnahmenRegeln.ArtBericht, "KL"), (bericht.Art, bericht.Verfasser));
        Assert.Equal([2], bericht.Bereiche);
        Assert.Equal("Maßnahme „Zimmerhygiene (Test)“: durchgeführt, 25 Min. gut mitgemacht", bericht.Text);
        Assert.Contains("schon bestätigt", Assert.Throws<Eingabefehler>(() =>
            s.MassnahmeBestaetigen(m.Id, dienst, MassnahmeErgebnis.Durchgefuehrt, "", 30, "", "AB", Jetzt)).Fehler[0], StringComparison.OrdinalIgnoreCase);
        Assert.False(Assert.Single(s.MassnahmenImDienst(dienst, [id]), t => t.Massnahme.Id == m.Id).Offen);
        // Nicht fällig (Spätdienst) – „bei Bedarf“ dagegen jederzeit und mehrmals; abgelehnt zählt keine Zeit.
        Assert.Throws<Eingabefehler>(() => s.MassnahmeBestaetigen(m.Id, dienst with { Schicht = Schicht.Spaet }, MassnahmeErgebnis.Durchgefuehrt, "", 30, "", "AB", Jetzt));
        var bedarf = s.MassnahmeAnlegen(id, F(MassnahmeRhythmus.BeiBedarf) with { Titel = "Gespräch bei Unruhe" }, "EF", Jetzt);
        Assert.DoesNotContain(s.MassnahmenImDienst(dienst, [id]), t => t.Massnahme.Id == bedarf.Id);
        s.MassnahmeBestaetigen(bedarf.Id, dienst, MassnahmeErgebnis.Durchgefuehrt, "", 15, "", "AB", Jetzt);
        var zweite = s.MassnahmeBestaetigen(bedarf.Id, dienst, MassnahmeErgebnis.Abgelehnt, "wollte allein sein", 15, "", "AB", Jetzt.AddMinutes(30));
        Assert.Equal(0, zweite.Minuten);
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => Sql("UPDATE massnahme_bestaetigungen SET minuten = 1"));
    }

    [Fact]
    public void Demo_Massnahmen_mit_Bestaetigungen_und_offenem_Dienst()
    {
        var bewohner = s.AlleBewohner();
        var alle = s.Massnahmen();
        Assert.All(bewohner, b => Assert.InRange(alle.Count(m => m.BewohnerId == b.Id), 2, 5));
        Assert.Equal([1, 2, 3, 4, 5], alle.Select(m => m.Fassungen[0].Lebensbereich).Distinct().Order());
        var bestaetigt = s.MassnahmeBestaetigungen();
        Assert.Contains(bestaetigt, b => b.Ergebnis == MassnahmeErgebnis.Abgelehnt);
        Assert.Contains(bestaetigt, b => b.Ergebnis == MassnahmeErgebnis.NichtMoeglich);
        Assert.All(bestaetigt, b => Assert.NotNull(b.BerichtId));
        Assert.DoesNotContain(bestaetigt, b => b.Tag == Heute && b.Schicht == Schicht.Frueh);   // der laufende Dienst ist offen
        Assert.Contains(s.MassnahmenImDienst(SchichtTermin.Zu(Jetzt)), t => t.Offen);
        Assert.Contains(MassnahmenRegeln.ArtBericht, s.Listeneintraege(Liste.Berichtsarten).Select(e => e.Name));
        Assert.Equal(MassnahmenRegeln.ArtenVorgabe, s.Listeneintraege(Liste.Massnahmenarten).Select(e => e.Name));
        Assert.Equal(MassnahmenRegeln.VorlagenVorgabe, s.Listeneintraege(Liste.Massnahmenvorlagen).Select(e => e.Name));
    }

    // Bauplan 40: Spalte der Übersicht, Stand im Kasten, Sammeleintrag in die Übergabe am Dienstende (einmal).
    [Fact]
    public void Uebersicht_Spalte_Stand_und_Sammeleintrag_am_Dienstende()
    {
        long id = Leer();
        var dienst = SchichtTermin.Zu(Jetzt);
        var m = s.MassnahmeAnlegen(id, F(), "EF", Jetzt);
        var zweite = s.MassnahmeAnlegen(id, F() with { Titel = "Wäsche (Test)" }, "EF", Jetzt);
        var termine = s.MassnahmenImDienst(dienst, [id]).Where(t => t.Massnahme.Id == m.Id || t.Massnahme.Id == zweite.Id).ToList();
        Assert.Equal("2 offen", MassnahmenRegeln.ListenText(termine));
        Assert.Equal("Maßnahmen: 0 von 2 bestätigt", MassnahmenRegeln.StandText(termine));
        Assert.Equal("", MassnahmenRegeln.ListenText([]));
        s.MassnahmeBestaetigen(m.Id, dienst, MassnahmeErgebnis.Durchgefuehrt, "", 30, "", "AB", Jetzt);
        termine = s.MassnahmenImDienst(dienst, [id]).Where(t => t.Massnahme.Id == m.Id || t.Massnahme.Id == zweite.Id).ToList();
        Assert.Equal("1 offen", MassnahmenRegeln.ListenText(termine));
        s.MassnahmeBestaetigen(zweite.Id, dienst, MassnahmeErgebnis.Abgelehnt, "keine Lust", 0, "", "AB", Jetzt);
        termine = s.MassnahmenImDienst(dienst, [id]).Where(t => t.Massnahme.Id == m.Id || t.Massnahme.Id == zweite.Id).ToList();
        Assert.Equal("✓", MassnahmenRegeln.ListenText(termine));

        // Spätdienst: was im Frühdienst offen blieb, steht einmal je Wohnbereich in der Übergabe – auch beim zweiten Aufruf
        // nur einmal. Die Demo bestätigt alle vergangenen Dienste, offen ist nur der laufende Frühdienst.
        var spaet = dienst.Ende.AddMinutes(10);
        int offen = s.MassnahmenImDienst(dienst).Count(t => t.Offen);
        Assert.True(offen > 0);
        int bereiche = s.MassnahmenImDienst(dienst).Where(t => t.Offen).Select(t => s.Bewohner(t.Massnahme.BewohnerId)!.Wohnbereich).Distinct().Count();
        Assert.Equal(bereiche, s.MassnahmenUebergabeErzeugen(spaet));
        Assert.Equal(0, s.MassnahmenUebergabeErzeugen(spaet.AddMinutes(30)));
        var eintraege = s.Uebergaben(spaet).Where(u => u.Verfasser == MassnahmenRegeln.UebergabeVerfasser).ToList();
        Assert.Equal(bereiche, eintraege.Count);
        Assert.All(eintraege, u => Assert.True(u.Allgemein));
        Assert.Contains(eintraege, u => u.Text.Contains("aus dem Frühdienst", StringComparison.Ordinal) && u.Text.Contains(" offen: ", StringComparison.Ordinal));
        Assert.DoesNotContain(eintraege, u => u.Text.Contains("Zimmerhygiene (Test)", StringComparison.Ordinal));   // bestätigt
        Assert.Equal($"R1 · 1 Maßnahme aus dem {dienst.Text} offen: Ahorn, Anton – Zimmer",
            MassnahmenRegeln.UebergabeText("R1", dienst, [("Ahorn, Anton", "Zimmer")]));
    }

    private void Sql(string text)
    {
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = text;
        befehl.ExecuteNonQuery();
    }
}
