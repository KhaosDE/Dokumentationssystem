using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Nachrichten: Personen und Teams, Verläufe, gelesen, archiviert, nichts wird gelöscht, Demo.
[Collection("Datenordner")]
public sealed class NachrichtenTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public NachrichtenTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Nachricht Senden(string von, IReadOnlyList<Empfaenger> an, string betreff = "Testbetreff", string text = "Testtext",
        long? verlauf = null, long? bewohner = null) =>
        s.NachrichtSenden(new Nachricht { Von = von, An = an, Betreff = betreff, Text = text, Gesendet = Jetzt, VerlaufId = verlauf, BewohnerId = bewohner });

    [Fact]
    public void Demo_Kollegen_Teams_und_Posteingang()
    {
        Assert.Equal(8, s.Kollegen().Count);
        Assert.Equal(["Leitung", "Therapie", "Wohnbereich A", "Wohnbereich B"], s.Teams());
        Assert.Equal("Anna Bachstelze (AB)", s.Kollege("ab")!.Anzeige);
        // AB: zwei ungelesene im Team-Verlauf „Arzttermin“, eine direkte von OP.
        Assert.Equal(3, s.UngeleseneNachrichten("AB"));
        var eingang = s.Verlaeufe("AB", NachrichtenOrdner.Posteingang);
        Assert.Equal(["Arzttermin am Donnerstag", "Schwimmkurs", "Teamsitzung am Mittwoch"], eingang.Select(v => v.Betreff));
        Assert.Equal(2, eingang[0].Ungelesen);
        Assert.Equal(["Ersatzschlüssel Fahrradkeller", "Dienst am Samstag tauschen?"],
            s.Verlaeufe("AB", NachrichtenOrdner.Gesendet).Select(v => v.Betreff));
        Assert.Equal(["Dienst am Samstag tauschen?"], s.Verlaeufe("AB", NachrichtenOrdner.Archiv).Select(v => v.Betreff));
        Assert.All(s.Verlaeufe("AB", NachrichtenOrdner.Posteingang).SelectMany(v => v.Nachrichten), n => Assert.DoesNotContain("{", n.Text));
    }

    [Fact]
    public void Nur_Beteiligte_sehen_einen_Verlauf()
    {
        // OP (Therapie) ist am „Arzttermin“ nicht beteiligt, GH (Wohnbereich B) auch nicht.
        Assert.DoesNotContain(s.Verlaeufe("OP", NachrichtenOrdner.Posteingang), v => v.Betreff == "Arzttermin am Donnerstag");
        Assert.DoesNotContain(s.Verlaeufe("GH", NachrichtenOrdner.Posteingang), v => v.Betreff == "Arzttermin am Donnerstag");
        Assert.Contains(s.Verlaeufe("GH", NachrichtenOrdner.Posteingang), v => v.Betreff == "Teamsitzung am Mittwoch");
        Assert.Empty(s.Verlaeufe("XY", NachrichtenOrdner.Posteingang));   // unbekannt: nichts
        Assert.Equal(0, s.UngeleseneNachrichten(""));
    }

    [Fact]
    public void Senden_an_Team_und_Person_pruefen()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => Senden("AB", [], betreff: "", text: "")).Fehler;
        Assert.Contains("An wen? Bitte mindestens eine Person oder ein Team wählen.", fehler);
        Assert.Contains("Betreff fehlt.", fehler);
        Assert.Contains("Text fehlt.", fehler);
        Assert.Contains(Assert.Throws<Eingabefehler>(() => Senden("ZZ", [Empfaenger.Person("AB")])).Fehler, f => f.Contains("Angemeldet als"));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => Senden("AB", [Empfaenger.Team("Küche")])).Fehler, f => f.Contains("Küche"));

        int vorher = s.UngeleseneNachrichten("GH");
        var n = Senden("AB", [Empfaenger.Team("Wohnbereich B"), Empfaenger.Person("OP")], "Grillfest", "Wer hilft beim Aufbau?");
        Assert.Equal(vorher + 1, s.UngeleseneNachrichten("GH"));   // über das Team
        Assert.Contains(s.Verlaeufe("MN", NachrichtenOrdner.Posteingang), v => v.Id == n.Id && v.Ungelesen == 1);
        Assert.Contains(s.Verlaeufe("OP", NachrichtenOrdner.Posteingang), v => v.Id == n.Id);
        Assert.Contains(s.Verlaeufe("AB", NachrichtenOrdner.Gesendet), v => v.Id == n.Id);
        Assert.DoesNotContain(s.Verlaeufe("AB", NachrichtenOrdner.Posteingang), v => v.Id == n.Id);   // nur Gesendetes
        Assert.DoesNotContain(s.Verlaeufe("CD", NachrichtenOrdner.Posteingang), v => v.Id == n.Id);
    }

    [Fact]
    public void Antwort_im_selben_Verlauf_an_alle_ausser_mir()
    {
        var n = Senden("OP", [Empfaenger.Person("AB"), Empfaenger.Person("CD")], "Ausflug", "Wer kommt mit?",
            bewohner: s.AlleBewohner()[0].Id);
        var verlauf = s.VerlaufLesen("AB", n.Id)!;
        var an = NachrichtenRegeln.AntwortAn(verlauf.Nachrichten, "AB");
        Assert.Equal([Empfaenger.Person("OP"), Empfaenger.Person("CD")], an);
        var antwort = Senden("AB", an, betreff: "", text: "Ich komme mit.", verlauf: n.Id);
        Assert.Equal("Ausflug", antwort.Betreff);                    // übernimmt den Betreff
        Assert.Equal(n.BewohnerId, antwort.BewohnerId);             // und den Bewohner
        var neu = s.VerlaufLesen("OP", n.Id)!;
        Assert.Equal(2, neu.Nachrichten.Count);
        Assert.Equal(1, neu.Ungelesen);
        Assert.Throws<InvalidOperationException>(() => Senden("CD", [Empfaenger.Person("AB")], verlauf: antwort.Id));
    }

    [Fact]
    public void Gelesen_und_Archiv_neue_Antwort_holt_zurueck()
    {
        var arzt = s.Verlaeufe("AB", NachrichtenOrdner.Posteingang).Single(v => v.Betreff == "Arzttermin am Donnerstag");
        s.VerlaufGelesen("AB", arzt.Id, Jetzt);
        Assert.Equal(1, s.UngeleseneNachrichten("AB"));
        Assert.Equal(0, s.VerlaufLesen("AB", arzt.Id)!.Ungelesen);
        Assert.True(s.VerlaufLesen("CD", arzt.Id)!.Ungelesen > 0);   // gelesen gilt nur für AB

        s.VerlaufArchivieren("AB", arzt.Id, Jetzt);
        Assert.DoesNotContain(s.Verlaeufe("AB", NachrichtenOrdner.Posteingang), v => v.Id == arzt.Id);
        Assert.Contains(s.Verlaeufe("AB", NachrichtenOrdner.Archiv), v => v.Id == arzt.Id);
        Assert.Contains(s.Verlaeufe("CD", NachrichtenOrdner.Posteingang), v => v.Id == arzt.Id);   // Archiv nur für AB

        s.Uhr = () => Jetzt.AddHours(1);
        s.NachrichtSenden(new Nachricht
        {
            Von = "CD", VerlaufId = arzt.Id, Text = "Fahrdienst kommt um 9:30 Uhr.", Gesendet = Jetzt.AddHours(1),
            An = NachrichtenRegeln.AntwortAn(arzt.Nachrichten, "CD"),
        });
        Assert.Contains(s.Verlaeufe("AB", NachrichtenOrdner.Posteingang), v => v.Id == arzt.Id);   // Neues holt zurück

        s.VerlaufArchivieren("AB", arzt.Id, Jetzt.AddHours(2));
        s.VerlaufZurueckholen("AB", arzt.Id);
        Assert.Contains(s.Verlaeufe("AB", NachrichtenOrdner.Posteingang), v => v.Id == arzt.Id);
    }

    [Fact]
    public void Nachrichten_lassen_sich_nicht_aendern()
    {
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE nachrichten SET text = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Nur_Verlaeufe_zu_einem_Bewohner()
    {
        var ahorn = s.AlleBewohner().First(b => b.Nachname == "Ahorn");
        var liste = s.Verlaeufe("AB", NachrichtenOrdner.Posteingang, ahorn.Id);
        Assert.Equal(["Arzttermin am Donnerstag"], liste.Select(v => v.Betreff));
        Assert.Contains("Anton", liste[0].Nachrichten[0].Text);
    }

    [Fact]
    public void Nach_dem_Update_kommen_Demo_Nachrichten_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM nachrichten; DELETE FROM kollegen; DELETE FROM meta WHERE schluessel = 'demo_nachrichten'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(8, s.Kollegen().Count);
        Assert.Equal(3, s.UngeleseneNachrichten("AB"));
        Assert.False(s.DemoBeimErstenStart());
        s.DemoZuruecksetzen();   // zweimal laden verdoppelt nichts
        Assert.Equal(8, s.Kollegen().Count);
        Assert.Equal(3, s.UngeleseneNachrichten("AB"));
    }
}
