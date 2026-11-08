using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26l: Regeln für „Heute wichtig“ (Dienst, Termine, Übergaben, Nachrichten),
// die Spalte „Wo“ der Bewohnerliste, die Geburtstagsmarke und die Statusleiste.
public sealed class HeuteWichtigTests
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);   // Samstag, Spätdienst
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);

    private static Bewohner B(string vorname, string nachname, DateOnly? geburt = null) => new()
    {
        Id = nachname.GetHashCode(), Vorname = vorname, Nachname = nachname, Zimmer = "101",
        Geburtsdatum = geburt ?? new DateOnly(1970, 3, 4), Aufnahmedatum = new DateOnly(2020, 1, 1),
    };

    private static Aufgabe A(long id, AufgabeFuer fuer, DateTime erstellt, DateTime? erledigt = null) => new()
    {
        Id = id, Text = $"Aufgabe {id}", Fuer = fuer, Erstellt = erstellt, Verfasser = "AB", Erledigt = erledigt,
        ErledigtVon = erledigt is null ? null : "CD",
    };

    [Fact]
    public void Dienst_zaehlt_offene_und_in_dieser_Schicht_erledigte()
    {
        var beginn = SchichtTermin.Zu(Jetzt).Beginn;
        var aufgaben = new[]
        {
            A(1, AufgabeFuer.NaechsteSchicht, beginn.AddHours(-2)),                               // für diese Schicht, offen
            A(2, AufgabeFuer.NaechsteSchicht, beginn.AddHours(-2), erledigt: beginn.AddMinutes(30)), // in dieser Schicht erledigt
            A(3, AufgabeFuer.NaechsteSchicht, beginn.AddHours(-2), erledigt: beginn.AddMinutes(-30)),// vor Schichtbeginn erledigt
            A(4, AufgabeFuer.WerZeitHat, beginn.AddDays(-1)),                                      // „wer Zeit hat“: offen
            A(5, AufgabeFuer.NaechsteSchicht, Jetzt.AddMinutes(-5)),                               // erst nächste Schicht
        };
        var d = HeuteWichtigRegeln.Dienst(aufgaben, Jetzt);
        Assert.Equal(Schicht.Spaet, d.Schicht.Schicht);
        Assert.Equal([1L, 4L], d.Offen.Select(a => a.Id));
        Assert.Equal(1, d.Erledigt);
        Assert.Equal(3, d.Gesamt);
        Assert.Equal("1 von 3 erledigt", d.Text);
    }

    [Fact]
    public void Termine_nach_Uhrzeit_gleiche_zusammengefasst()
    {
        Termin T(int stunde, string titel, TerminQuelle quelle = TerminQuelle.Kalender) =>
            new() { Tag = Heute, Von = new(stunde, 0), Bis = new(stunde + 1, 0), Titel = titel, Quelle = quelle };
        var zeilen = new[]
        {
            new TagesplanZeile(B("Erna", "Eiche"), T(17, "Fußpflege"), null),
            new TagesplanZeile(B("Anton", "Ahorn"), T(9, "Holzwerkstatt"), null),
            new TagesplanZeile(B("Berta", "Birke"), T(9, "Holzwerkstatt"), null),
            new TagesplanZeile(B("Carl", "Clematis"), T(9, "Holzwerkstatt"), null),
            new TagesplanZeile(B("Dora", "Distel"), T(9, "Holzwerkstatt"), null),
            new TagesplanZeile(B("Erich", "Esche"), new Termin { Tag = Heute, Ganztags = true, Titel = "Ausflug" }, null),
        };
        var t = HeuteWichtigRegeln.Termine(zeilen, Jetzt);
        Assert.Equal(["ganztags", "09:00", "17:00"], t.Select(x => x.Zeit));
        Assert.Equal("Holzwerkstatt – Anton Ahorn, Berta Birke, Carl Clematis +1", t[1].Text);
        Assert.Equal("vorbei", t[1].StatusText);    // Kalendertermin, schon vorbei
        Assert.Equal("geplant", t[2].StatusText);
    }

    [Fact]
    public void Uebergabe_und_Nachricht_in_einer_Zeile()
    {
        var u = new Uebergabe { Id = 1, BewohnerId = 7, Text = "Arzttermin morgen\nbitte erinnern", Erstellt = new(2026, 11, 6, 13, 0, 0), Verfasser = "AB" };
        Assert.Equal("Anton Ahorn, Zi. 101: Arzttermin morgen", HeuteWichtigRegeln.UebergabeText(u, B("Anton", "Ahorn")));
        Assert.Equal("Wohnbereich allgemein: Arzttermin morgen", HeuteWichtigRegeln.UebergabeText(u with { BewohnerId = null }, null));
        Assert.Equal("seit 06.11. · AB", HeuteWichtigRegeln.SeitText(u));

        Nachricht N(long id, string von, bool gelesen) => new()
        {
            Id = id, VerlaufId = id == 1 ? null : 1, Von = von, Betreff = "Dienstplan", Text = "Bitte tauschen", Gesendet = Jetzt.AddMinutes(id),
            An = [Empfaenger.Person("CD"), Empfaenger.Team("R1")], GelesenVon = gelesen ? ["CD"] : [],
        };
        var verlauf = new Verlauf { Id = 1, Betreff = "Dienstplan", Nachrichten = [N(1, "AB", true), N(2, "EF", false), N(3, "CD", false)], Ungelesen = 1 };
        var n = Assert.Single(HeuteWichtigRegeln.Ungelesen([verlauf, verlauf with { Id = 2, Ungelesen = 0 }], "CD"));
        Assert.Equal(2, n.Id);   // die eigene Nachricht (3) zählt nicht
        Assert.Equal("EF → CD, R1: Dienstplan", HeuteWichtigRegeln.NachrichtText(n));
    }

    [Fact]
    public void Wo_steht_der_Bewohner()
    {
        var keine = Array.Empty<Termin>();
        Assert.Equal((HeuteWichtigRegeln.ImHaus, MarkenArt.Angabe, (Marke?)null), HeuteWichtigRegeln.Wo([new Marke(MarkenArt.Allergie, "Allergie: Nüsse")], keine, Jetzt));
        var heim = new Marke(MarkenArt.Abwesend, "abwesend: Heimfahrt bis Mo 09.11.");
        var therapie = new Marke(MarkenArt.Status, "in Therapie: Ergotherapie bis 16:00");
        Assert.Equal(("Heimfahrt bis Mo 09.11.", MarkenArt.Abwesend, (Marke?)heim), HeuteWichtigRegeln.Wo([therapie, heim], keine, Jetzt));
        Assert.Equal(("in Therapie: Ergotherapie bis 16:00", MarkenArt.Status, (Marke?)therapie), HeuteWichtigRegeln.Wo([therapie], keine, Jetzt));
        var ausgang = new Marke(MarkenArt.Ausgang, "unterwegs seit 14:30 (Einkauf)");
        Assert.Equal("unterwegs seit 14:30 (Einkauf)", HeuteWichtigRegeln.Wo([therapie, ausgang], keine, Jetzt).Text);

        // Ein Kalendertermin, der gerade läuft; Therapien zählen erst mit „gegangen“ (Marke „in Therapie“).
        var arzt = new Termin { Tag = Heute, Von = new(15, 0), Bis = new(16, 0), Titel = "Hausarzt", Quelle = TerminQuelle.Kalender };
        Assert.Equal("Termin: Hausarzt bis 16:00", HeuteWichtigRegeln.Wo([], [arzt], Jetzt).Text);
        Assert.Equal(HeuteWichtigRegeln.ImHaus, HeuteWichtigRegeln.Wo([], [arzt with { Quelle = TerminQuelle.Therapie }], Jetzt).Text);
        Assert.Equal(HeuteWichtigRegeln.ImHaus, HeuteWichtigRegeln.Wo([], [arzt with { Bis = new(15, 20) }], Jetzt).Text);
    }

    [Fact]
    public void Geburtstag_heute_mit_Alter()
    {
        Assert.Equal("Geburtstag heute (56)", HeuteWichtigRegeln.Geburtstag(B("Anton", "Ahorn", new DateOnly(1970, 11, 7)), Heute)?.Text);
        Assert.Null(HeuteWichtigRegeln.Geburtstag(B("Anton", "Ahorn", new DateOnly(1970, 11, 8)), Heute));
        var schalttag = B("Berta", "Birke", new DateOnly(1980, 2, 29));
        Assert.NotNull(HeuteWichtigRegeln.Geburtstag(schalttag, new DateOnly(2027, 2, 28)));
        Assert.Null(HeuteWichtigRegeln.Geburtstag(schalttag, new DateOnly(2028, 2, 28)));
        Assert.NotNull(HeuteWichtigRegeln.Geburtstag(schalttag, new DateOnly(2028, 2, 29)));
    }

    [Fact]
    public void Statusleiste_mit_Datum_Dienst_und_Stand() =>
        Assert.Equal("DEMO — ausgedachte Daten · Sa 07.11.2026 · Spätdienst · Stand 15:18",
            HeuteWichtigRegeln.StatusText(Jetzt, Jetzt.AddMinutes(-2)));
}
