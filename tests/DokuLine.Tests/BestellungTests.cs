using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 51: Medikamentenbestellung – Verbrauch und „reicht bis“, Vorrat aus Zählung, Lieferung und Ausgaben, Empfänger
// je Bestellweg, Anforderung ohne Diagnosen, Verlauf bis erledigt, Erinnerung, Bestand zuschaltbar.
[Collection("Datenordner")]
public sealed class BestellungTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public BestellungTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    [Fact]
    public void Verbrauch_Vorrat_und_Empfaenger()
    {
        Assert.Equal(2.5m, BestellRegeln.ProTag(new Dosis("1", "0", "½", "1")));
        Assert.Equal(Heute.AddDays(4), BestellRegeln.ReichtBis(10, 2.5m, Heute));
        Assert.Null(BestellRegeln.ReichtBis(10, 0, Heute));
        var v = new Verordnung
        {
            Id = 7, BewohnerId = 1, Medikament = "Testmittel", Staerke = "5 mg", Dosis = new Dosis("1", "0", "1", "0"), Beginn = Heute.AddDays(-30),
            Anordnung = new("Dr. Test (ausgedacht)", Heute.AddDays(-30))
        };
        var zaehlung = new Bestandszaehlung(7, 20, 50, Jetzt.AddDays(-3), "AB");
        AusgabeEintrag A(int tageVorher, Einnahmezeit z) => new(0, 1, Heute.AddDays(-tageVorher), z, 7, AusgabeStatus.Ausgegeben, "AB", Jetzt.AddDays(-tageVorher));
        var vorrat = BestellRegeln.Vorrat(zaehlung, [(Jetzt.AddDays(-1), 50m), (Jetzt.AddDays(-10), 99m)], [A(2, Einnahmezeit.Morgens), A(2, Einnahmezeit.Abends), A(5, Einnahmezeit.Morgens)], v);
        Assert.Equal(68m, vorrat);   // 20 + 50 − 2 (vor der Zählung zählt nicht)
        // Je Gabe zählt der zuletzt gesetzte Status: korrigiert auf „abgelehnt“ zieht nicht ab, doppelt abgehakt einmal, „mitgegeben“ zieht ab.
        var korrigiert = A(1, Einnahmezeit.Morgens) with { Id = 2, Status = AusgabeStatus.Abgelehnt };
        var mitgegeben = A(1, Einnahmezeit.Abends) with { Id = 3, Status = AusgabeStatus.Mitgegeben };
        Assert.Equal(68m, BestellRegeln.Vorrat(zaehlung, [(Jetzt.AddDays(-1), 50m)],   // 20 + 50 − 1 (mitgegeben) − 1 (doppelt, einmal gezählt)
            [A(1, Einnahmezeit.Morgens) with { Id = 1 }, korrigiert, mitgegeben, A(2, Einnahmezeit.Morgens) with { Id = 4 }, A(2, Einnahmezeit.Morgens) with { Id = 5 }], v));
        Assert.Null(BestellRegeln.Vorrat(null, [], [], v));
        Assert.Equal("Dr. Test (ausgedacht)", BestellRegeln.Empfaenger(Bestellweg.Hausarzt, null, v, "Apotheke Sonnenschein"));
        Assert.Equal("Ambulanz (Test)", BestellRegeln.Empfaenger(Bestellweg.Fosa, "Ambulanz (Test)", v, ""));
        Assert.Equal("Apotheke Sonnenschein", BestellRegeln.Empfaenger(Bestellweg.NichtErfasst, null, v, "Apotheke Sonnenschein"));
        var b = new Bewohner { Id = 1, Vorname = "A", Nachname = "Ahorn (Test)", Zimmer = "101", Geburtsdatum = new(1980, 1, 2), Aufnahmedatum = new(2020, 1, 1) };
        Assert.Equal("A Ahorn (Test), geb. 02.01.1980: Testmittel 5 mg", BestellRegeln.PostenText(b, v));
    }

    [Fact]
    public void Liste_Anforderung_Verlauf_und_Erinnerung()
    {
        var bewohner = s.AlleBewohner().Where(b => b.Wohnbereich == "R1").ToList();
        var ohneBestand = s.BestellListe(bewohner, "R1", Heute);
        Assert.NotEmpty(ohneBestand);
        Assert.All(ohneBestand, p => Assert.Null(p.Vorrat));
        Assert.Throws<Eingabefehler>(() => s.BestellEinstellungen("R1", true, 7, "Apotheke Sonnenschein (ausgedacht)", "apotheke@example.org", "KL"));
        s.BestellEinstellungen("R1", true, 7, "Apotheke Sonnenschein (ausgedacht)", "apotheke@example.org", "EF");
        Assert.True(s.BestandFuehren("R1"));
        Assert.False(s.BestandFuehren("R2"));
        // Mit Bestand: ohne Zählung nicht gezeigt? – ohne Zählung kein „reicht bis“, also zur Auswahl; nach einer großen Zählung nicht mehr.
        var p = ohneBestand[0];
        s.BestandZaehlen(p.Verordnung.Id, 500, 50, "AB", Jetzt);
        Assert.DoesNotContain(s.BestellListe(bewohner, "R1", Heute), x => x.Verordnung.Id == p.Verordnung.Id);
        s.BestandZaehlen(p.Verordnung.Id, 1, 50, "AB", Jetzt.AddMinutes(1));
        var knapp = Assert.Single(s.BestellListe(bewohner, "R1", Heute), x => x.Verordnung.Id == p.Verordnung.Id);
        Assert.Equal(1m, knapp.Vorrat);

        Assert.Throws<Eingabefehler>(() => s.AnforderungenErstellen([], "AB", Jetzt));
        var anf = Assert.Single(s.AnforderungenErstellen([(knapp, "")], "AB", Jetzt));
        Assert.Equal((AnforderungStand.Angefordert, "1 Packung"), (anf.Stand, anf.Posten[0].Menge));
        Assert.DoesNotContain("Diagnose", BestellRegeln.Text(anf));
        Assert.StartsWith("mailto:", BestellRegeln.Mailto(anf));

        Assert.Equal(1, s.BestellErinnerungen(Jetzt.AddDays(6)));
        Assert.Equal(0, s.BestellErinnerungen(Jetzt.AddDays(7)));   // einmal
        s.AnforderungWeiter(anf.Id, AnforderungStand.Bestaetigt, null, "AB", Jetzt.AddDays(1));
        s.AnforderungWeiter(anf.Id, AnforderungStand.Geliefert, new Dictionary<long, decimal> { [knapp.Verordnung.Id] = 50 }, "AB", Jetzt.AddDays(2));
        Assert.Throws<Eingabefehler>(() => s.AnforderungWeiter(anf.Id, AnforderungStand.Bestaetigt, null, "AB", Jetzt.AddDays(3)));   // zurück geht nicht
        s.AnforderungWeiter(anf.Id, AnforderungStand.Erledigt, null, "AB", Jetzt.AddDays(3));
        Assert.Equal(AnforderungStand.Erledigt, s.Anforderungen().Single(a => a.Id == anf.Id).Stand);
        Assert.DoesNotContain(s.Anforderungen(auchErledigte: false), a => a.Id == anf.Id);
        Assert.Equal(51m, BestellRegeln.Vorrat(s.LetzteZaehlung(knapp.Verordnung.Id), [(Jetzt.AddDays(2), 50m)], [], knapp.Verordnung));
        Assert.False(s.EmailFreigegeben("Apotheke Sonnenschein (ausgedacht)"));
        s.EmailFreigeben("Apotheke Sonnenschein (ausgedacht)", true, "EF");
        Assert.True(s.EmailFreigegeben("apotheke sonnenschein (ausgedacht)"));
    }
}
