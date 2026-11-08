using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26m: „Laufende Therapien“ – Termine von heute mit Teilnahme-Status, Spalten „läuft“,
// „Gleich“, „Noch offen“, „Rest des Tages“, Hinweis ohne laufende Therapie, Bahnen der Tagesleiste.
public sealed class LaufendeTherapienTests
{
    private static readonly DateTime Montag10 = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Montag = DateOnly.FromDateTime(Montag10);

    private static readonly Dictionary<long, Bewohner> Bewohner = new[]
    {
        B(1, "Ahorn", "R1", "101"), B(2, "Birke", "R2", "201"), B(3, "Clematis", "R1", "103"), B(4, "Distel", "R1", "104"),
        B(5, "Eiche", "R2", "205"), B(6, "Fichte", "R1", "106"), B(7, "Ginster", "R1", "107"),
    }.ToDictionary(b => b.Id!.Value);

    private static Bewohner B(long id, string nachname, string bereich, string zimmer) => new()
    {
        Id = id, Vorname = "Test", Nachname = nachname, Zimmer = zimmer, Wohnbereich = bereich,
        Geburtsdatum = new DateOnly(1970, 1, 1), Aufnahmedatum = new DateOnly(2020, 1, 1),
    };

    private static Angebot A(long id, string name, DayOfWeek tag, string von, string bis, params long[] bewohner) => new()
    {
        Id = id,
        Fassungen = [new AngebotFassung
        {
            GiltAb = new DateOnly(2026, 1, 1), Name = name, Ort = "Raum", Therapeut = "Olga Pirol", Hoechstens = 6,
            Termine = [new Wochentermin(tag, TimeOnly.Parse(von), TimeOnly.Parse(bis))],
        }],
        Zuteilungen = [.. bewohner.Select(b => new Zuteilung { Id = id * 10 + b, AngebotId = id, BewohnerId = b, Ab = new DateOnly(2026, 1, 1) })],
    };

    private static readonly Angebot[] Angebote =
    [
        A(1, "Holzwerkstatt", DayOfWeek.Monday, "09:00", "11:30", 1, 2, 3),
        A(2, "Musikgruppe", DayOfWeek.Monday, "10:20", "11:00", 4),
        A(3, "Bewegung", DayOfWeek.Monday, "14:00", "15:00", 5),
        A(4, "Morgenrunde", DayOfWeek.Monday, "07:30", "08:15", 6, 7),
        A(5, "Gespräch", DayOfWeek.Tuesday, "10:00", "11:00", 1),
    ];

    private static readonly Dictionary<long, TeilnahmeEintrag> Eintraege = new()
    {
        [11] = new TeilnahmeEintrag(Teilnahme.Teilgenommen, Montag10.AddMinutes(-55), "CD"),   // Ahorn gegangen
        [47] = new TeilnahmeEintrag(Teilnahme.Abgesagt, Montag10.AddHours(-3), "CD"),          // Ginster abgesagt
    };

    [Fact]
    public void Tafel_um_zehn_Uhr()
    {
        var heute = LaufendeRegeln.Heute(Angebote, Bewohner, Eintraege, Montag10);
        Assert.Equal(["Morgenrunde", "Holzwerkstatt", "Musikgruppe", "Bewegung"], heute.Select(t => t.Name));   // nicht Dienstag
        var holz = heute[1];
        Assert.Equal(["Ahorn", "Clematis", "Birke"], holz.Teilnehmer.Select(p => p.Bewohner.Nachname));   // Bereich, Zimmer
        Assert.Equal(["gegangen", "noch offen", "noch offen"], holz.Teilnehmer.Select(p => p.StatusText));
        Assert.Equal("3 von 6", holz.Belegung);
        Assert.Equal("09:00–11:30", holz.Zeit);

        var tafel = LaufendeRegeln.Tafel(heute, Montag10);
        Assert.Equal(["Holzwerkstatt"], tafel.Laufend.Select(t => t.Name));
        Assert.Equal(["Musikgruppe"], tafel.Gleich.Select(t => t.Name));                       // beginnt in 20 Minuten
        Assert.Equal(["Fichte", "Clematis", "Birke"], tafel.NochOffen.Select(x => x.Teilnehmer.Bewohner.Nachname));
        Assert.Equal(["Bewegung"], tafel.RestDesTages.Select(t => t.Name));
        Assert.Null(tafel.Hinweis);
        Assert.Equal(TafelStatus.Abgesagt, heute[0].Teilnehmer.Single(p => p.Bewohner.Nachname == "Ginster").Status);
    }

    [Fact]
    public void Nur_ein_Wohnbereich()
    {
        var r2 = LaufendeRegeln.Heute(Angebote, Bewohner, Eintraege, Montag10, "R2");
        Assert.Equal(["Holzwerkstatt", "Bewegung"], r2.Select(t => t.Name));   // nur Angebote mit Teilnehmern aus R2
        Assert.Equal("Birke", Assert.Single(r2[0].Teilnehmer).Bewohner.Nachname);
        Assert.Equal("3 von 6", r2[0].Belegung);                                 // Belegung zählt alle
    }

    [Fact]
    public void Hinweis_wenn_nichts_laeuft()
    {
        var mittag = Montag.ToDateTime(new TimeOnly(12, 0));
        var tafel = LaufendeRegeln.Tafel(LaufendeRegeln.Heute(Angebote, Bewohner, Eintraege, mittag), mittag);
        Assert.Empty(tafel.Laufend);
        Assert.Equal("Gerade läuft keine Therapie. Nächste: Bewegung um 14:00.", tafel.Hinweis);
        var abend = Montag.ToDateTime(new TimeOnly(16, 0));
        Assert.Equal("Gerade läuft keine Therapie. Heute kommt keine mehr.", LaufendeRegeln.Tafel(LaufendeRegeln.Heute(Angebote, Bewohner, Eintraege, abend), abend).Hinweis);
    }

    [Fact]
    public void Bahnen_ohne_Ueberschneidung_und_Lage_in_der_Leiste()
    {
        var bahnen = LaufendeRegeln.Bahnen(LaufendeRegeln.Heute(Angebote, Bewohner, Eintraege, Montag10));
        Assert.Equal(2, bahnen.Count);
        Assert.Equal(["Morgenrunde", "Holzwerkstatt", "Bewegung"], bahnen[0].Select(t => t.Name));
        Assert.Equal(["Musikgruppe"], bahnen[1].Select(t => t.Name));
        Assert.Equal(0, LaufendeRegeln.Anteil(new TimeOnly(6, 0)));
        Assert.Equal(0.5, LaufendeRegeln.Anteil(new TimeOnly(14, 0)));
        Assert.Equal(1, LaufendeRegeln.Anteil(new TimeOnly(22, 0)));
    }

    [Fact]
    public void Vorgabe_der_Bereichswahl()
    {
        Assert.Equal(LaufendeRegeln.AlleBereiche, LaufendeRegeln.Vorgabe(["Therapie"], Bewohner.Values));
        Assert.Equal("R2", LaufendeRegeln.Vorgabe(["Therapie", "R2"], Bewohner.Values));
    }
}
