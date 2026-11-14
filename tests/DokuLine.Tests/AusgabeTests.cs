using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 42: Medikamentenausgabe abhaken – Einnahmezeit, was fällig ist, Status je Bewohner oder Medikament, Bericht bei
// „abgelehnt“/„nicht abgeholt“ (ohne Marke „Medikation geändert“), Recht, Sammeleintrag am Dienstende, Demo, Datenbank.
[Collection("Datenordner")]
public sealed class AusgabeTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);   // Montag, morgens
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public AusgabeTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static Verordnung V(long id, Dosis dosis, bool bedarf = false) => new()
    {
        Id = id, BewohnerId = 1, MedikamentId = 1, Medikament = "Testmittel", Staerke = "10 mg", Dosis = dosis, Bedarf = bedarf, Beginn = Heute.AddDays(-30),
        Anordnung = new("Dr. Test (ausgedacht)", Heute.AddDays(-30)),
    };

    [Fact]
    public void Einnahmezeit_faellig_Zeilen_und_Texte()
    {
        Assert.Equal((Heute.AddDays(-1), Einnahmezeit.Nachts), AusgabeRegeln.Jetzt(Heute.ToDateTime(new TimeOnly(4, 0))));
        Assert.Equal((Heute, Einnahmezeit.Morgens), AusgabeRegeln.Jetzt(Jetzt));
        Assert.Equal((Heute, Einnahmezeit.Mittags), AusgabeRegeln.Jetzt(Heute.ToDateTime(new TimeOnly(12, 0))));
        Assert.Equal((Heute, Einnahmezeit.Abends), AusgabeRegeln.Jetzt(Heute.ToDateTime(new TimeOnly(18, 0))));
        Assert.Equal((Heute, Einnahmezeit.Nachts), AusgabeRegeln.Jetzt(Heute.ToDateTime(new TimeOnly(22, 0))));
        Assert.Equal([Einnahmezeit.Morgens, Einnahmezeit.Mittags], AusgabeRegeln.ZeitenDesDienstes(Schicht.Frueh));

        var v = V(1, new Dosis("1", "0", "½", "0"));
        Assert.Equal("1", AusgabeRegeln.Faellig(v, Heute, Einnahmezeit.Morgens));
        Assert.Null(AusgabeRegeln.Faellig(v, Heute, Einnahmezeit.Mittags));
        Assert.Equal("½", AusgabeRegeln.Faellig(v, Heute, Einnahmezeit.Abends));
        Assert.Null(AusgabeRegeln.Faellig(V(2, new Dosis("1", "0", "0", "0"), bedarf: true), Heute, Einnahmezeit.Morgens));
        var pause = v with { Aenderungen = [new VerordnungsAenderung { Art = AenderungArt.Pause, Von = Heute.AddDays(-1), Erstellt = Jetzt.AddDays(-1) }] };
        Assert.Null(AusgabeRegeln.Faellig(pause, Heute, Einnahmezeit.Morgens));

        var a = new Bewohner { Id = 1, Vorname = "A", Nachname = "Ahorn (Test)", Zimmer = "102", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };
        var b = a with { Id = 2, Nachname = "Birke (Test)", Zimmer = "101" };
        var c = a with { Id = 3, Nachname = "Clematis (Test)", Zimmer = "103" };
        var plaene = new Dictionary<long, List<Verordnung>>
        {
            [1] = [v, V(4, new Dosis("2", "0", "0", "0"))], [2] = [V(5, new Dosis("1", "0", "0", "0")) with { BewohnerId = 2 }],
            [3] = [V(6, new Dosis("1", "0", "0", "0")) with { BewohnerId = 3 }]
        };
        AusgabeEintrag E(long id, long bewohner, long verordnung, AusgabeStatus st) => new(id, bewohner, Heute, Einnahmezeit.Morgens, verordnung, st, "AB", Jetzt);
        var zeilen = AusgabeRegeln.Zeilen([a, b, c], plaene, Heute, Einnahmezeit.Morgens,
            [E(1, 1, 1, AusgabeStatus.Ausgegeben), E(2, 2, 5, AusgabeStatus.NichtAbgeholt), E(3, 3, 6, AusgabeStatus.Abgelehnt), E(4, 3, 6, AusgabeStatus.Ausgegeben)],
            new HashSet<long>());
        Assert.Equal([2L, 1L, 3L], zeilen.Select(z => z.Bewohner.Id!.Value));   // nicht abgeholt oben, dann offen, dann erledigt
        Assert.StartsWith("nicht abgeholt · AB", zeilen[0].StandText);
        Assert.True(zeilen[1].Teilweise);
        Assert.StartsWith("teilweise (1 ausgegeben, 1 offen)", zeilen[1].StandText);
        Assert.Equal(AusgabeStatus.Ausgegeben, zeilen[2].Status);   // der neuere Eintrag gilt
        Assert.Equal("1 von 3 ausgegeben", AusgabeRegeln.Zaehler(zeilen));
        Assert.Equal("noch offen · abwesend laut Ausgangsliste", new AusgabeZeile(a, [new AusgabeMedikament(v, "1", null)], true).StandText);
        Assert.Equal("Medikamente morgens: abgelehnt (Testmittel 10 mg · 1).",
            AusgabeRegeln.BerichtText(Einnahmezeit.Morgens, AusgabeStatus.Abgelehnt, [new AusgabeMedikament(v, "1", null)]));
    }

    [Fact]
    public void Setzen_mit_Recht_Bericht_einzeln_und_Datenbank()
    {
        var (zeile, b) = s.AlleBewohner().Select(x => (s.Ausgabeliste([x], Heute, Einnahmezeit.Morgens).SingleOrDefault(), x)).First(x => x.Item1 is { Medikamente.Count: >= 2 });
        long id = b.Id!.Value;
        Assert.True(zeile!.NochOffen);   // die laufende Einnahmezeit ist in der Demo offen
        Assert.Throws<Eingabefehler>(() => s.AusgabeSetzen(id, Heute, Einnahmezeit.Morgens, AusgabeStatus.Ausgegeben, "KL", Jetzt));   // Assistenzkraft
        Assert.Throws<Eingabefehler>(() => s.AusgabeSetzen(id, Heute.AddDays(1), Einnahmezeit.Morgens, AusgabeStatus.Ausgegeben, "AB", Jetzt));   // Zukunft
        long erstes = zeile.Medikamente[0].Verordnung.Id;
        s.AusgabeSetzen(id, Heute, Einnahmezeit.Morgens, AusgabeStatus.Abgelehnt, "AB", Jetzt, [erstes]);
        zeile = s.Ausgabeliste([b], Heute, Einnahmezeit.Morgens).Single();
        Assert.True(zeile.Teilweise);
        var bericht = s.Berichte(id).Single(x => x.Text.StartsWith("Medikamente morgens: abgelehnt", StringComparison.Ordinal) && x.Zeitpunkt == Jetzt);
        Assert.Equal((DokuRegeln.ArtMedikation, true), (bericht.Art, bericht.Ausgabe));
        Assert.Equal([5], bericht.Bereiche);
        Assert.DoesNotContain(DokuMarke.MedikationGeaendert, DokuRegeln.Marken(bericht));
        Assert.DoesNotContain(DokuMarke.Bedarfsgabe, DokuRegeln.Marken(bericht));
        int berichte = s.Berichte(id).Count;
        s.AusgabeSetzen(id, Heute, Einnahmezeit.Morgens, AusgabeStatus.Ausgegeben, "AB", Jetzt.AddMinutes(2));   // alle: „ausgegeben“ ohne Bericht
        Assert.Equal(berichte, s.Berichte(id).Count);
        zeile = s.Ausgabeliste([b], Heute, Einnahmezeit.Morgens).Single();
        Assert.True(zeile.Ausgegeben);
        Assert.Equal(zeile.Medikamente.Count + 1, s.AusgabeEintraege(Heute, Heute, id).Count(e => e.Zeit == Einnahmezeit.Morgens));   // nichts überschrieben
        var keinPlan = s.AlleBewohner().FirstOrDefault(x => s.Ausgabeliste([x], Heute, Einnahmezeit.Mittags).Count == 0);
        if (keinPlan is not null)
            Assert.Throws<Eingabefehler>(() => s.AusgabeSetzen(keinPlan.Id!.Value, Heute, Einnahmezeit.Mittags, AusgabeStatus.Ausgegeben, "AB", Jetzt));
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE medikamentenausgaben SET status = 'ausgegeben'";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());
        Assert.False(RechteRegeln.Vorbelegt("Therapie", Funktionen.MedikamenteAusgeben));
        Assert.Contains(Funktionen.MedikamenteAusgeben, RechteRegeln.NurFachkraftVorgabe);
    }

    [Fact]
    public void Demo_Verlauf_und_Sammeleintrag_am_Dienstende()
    {
        var eintraege = s.AusgabeEintraege(Heute.AddDays(-14), Heute);
        Assert.Contains(eintraege, e => e.Status == AusgabeStatus.NichtAbgeholt);
        Assert.Contains(eintraege, e => e.Status == AusgabeStatus.Abgelehnt && e.BerichtId is not null);
        Assert.DoesNotContain(eintraege, e => e.Tag == Heute);   // morgens läuft gerade
        long id = eintraege.First(e => e.Status == AusgabeStatus.Abgelehnt).BewohnerId;
        var namen = s.Verordnungen(id).ToDictionary(v => v.Id, v => $"{v.Medikament} {v.Staerke}".Trim());
        var verlauf = AusgabeRegeln.Verlauf(s.AusgabeEintraege(Heute.AddDays(-13), Heute, id), namen);
        Assert.Contains(verlauf, z => z.Contains(" – abgelehnt (", StringComparison.Ordinal));

        // Spätdienst: Morgens und mittags blieben in der Demo offen → je Wohnbereich ein Sammeleintrag, einmal.
        var spaet = Heute.ToDateTime(new TimeOnly(14, 10));
        int neu = s.AusgabeUebergabeErzeugen(spaet);
        Assert.True(neu > 0);
        Assert.Equal(0, s.AusgabeUebergabeErzeugen(spaet.AddMinutes(20)));
        var notizen = s.Uebergaben(spaet).Where(u => u.Allgemein && u.Text.Contains("Medikamente aus dem", StringComparison.Ordinal)).ToList();
        Assert.Equal(neu, notizen.Count);
        Assert.Contains(notizen, u => u.Text.Contains("morgens ", StringComparison.Ordinal) && u.Text.Contains("(ohne Eintrag)", StringComparison.Ordinal));
    }
}
