using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 46: Archiv – Entlassen mit Folgen (Aufgaben, Maßnahmen, Therapien enden), nicht mehr in Listen und Belegung,
// Frist ohne Vorgabe, Wiederaufnahme mit freiem Bett, Löschen nur zu zweit nach Ablauf mit Protokoll ohne Inhalte, Demo.
[Collection("Datenordner")]
public sealed class ArchivTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public ArchivTests()
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
    public void Regeln_Frist_Pruefen_und_Loeschen_zu_zweit()
    {
        var fehler = new List<string>();
        Assert.Null(ArchivRegeln.FristLesen(" ", fehler));
        Assert.Equal(10, ArchivRegeln.FristLesen("10", fehler));
        Assert.Empty(fehler);
        Assert.Null(ArchivRegeln.FristLesen("0", fehler));
        Assert.Single(fehler);
        Assert.Equal(new DateOnly(2030, 3, 1), ArchivRegeln.AufbewahrenBis(new DateOnly(2020, 3, 1), 10));
        Assert.Equal(ArchivRegeln.FristOffen, ArchivRegeln.AufbewahrenText(null));

        var b = new Bewohner { Id = 1, Vorname = "A", Nachname = "Ahorn (Test)", Zimmer = "101", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = Heute.AddYears(-1) };
        Assert.Equal(2, ArchivRegeln.EntlassenPruefen(b with { Aufnahmedatum = Heute.AddDays(5) }, Heute.AddDays(1), Heute).Count);
        var e = new ArchivEintrag(1, 1, Heute.AddYears(-20), Heute.AddYears(-12), EntlassGrund.Auszug, "", "R1", "101", "EF", Jetzt, null);
        var zeile = new ArchivZeile(b, e, [e], Heute.AddYears(-2), null);
        Assert.True(zeile.Abgelaufen(Heute));
        Assert.Null(ArchivRegeln.LoeschenPruefen(zeile, Heute, "AB", endgueltig: false));
        Assert.NotNull(ArchivRegeln.LoeschenPruefen(zeile, Heute, "AB", endgueltig: true));   // ohne Freigabe
        var frei = zeile with { Freigabe = new ArchivLoeschfreigabe(1, "AB", Jetzt) };
        Assert.Contains("zu zweit", ArchivRegeln.LoeschenPruefen(frei, Heute, "ab", endgueltig: true));
        Assert.Null(ArchivRegeln.LoeschenPruefen(frei, Heute, "TST", endgueltig: true));
        Assert.Equal(ArchivRegeln.FristFehlt, ArchivRegeln.LoeschenPruefen(zeile with { AufbewahrenBis = null }, Heute, "AB", false));
        Assert.Single(ArchivRegeln.Suchen([zeile], "ahorn"));
        Assert.Empty(ArchivRegeln.Suchen([zeile], "Birke"));
    }

    [Fact]
    public void Entlassen_Folgen_Belegung_und_Wiederaufnahme()
    {
        var b = s.AlleBewohner().First(x => s.Massnahmen(x.Id!.Value).Count > 0 && x.Bett.Length > 0);
        long id = b.Id!.Value;
        int vorher = s.AlleBewohner().Count;
        s.AufgabeSpeichern(new Aufgabe { BewohnerId = id, Text = "Termin absagen (Test)", Fuer = AufgabeFuer.WerZeitHat, Erstellt = Jetzt, Verfasser = "EF" });
        var folgen = s.EntlassFolgen(id, Heute);
        Assert.Contains(folgen, f => f.Art == "Aufgabe" && f.Text == "Aufgabe: Termin absagen (Test)");
        Assert.Contains(folgen, f => f.Art == "Maßnahme");
        Assert.Throws<Eingabefehler>(() => s.Entlassen(id, Heute, EntlassGrund.Auszug, "", "QS", Jetzt));   // QMB ändert nichts
        Assert.Throws<Eingabefehler>(() => s.Entlassen(id, Heute.AddDays(1), EntlassGrund.Auszug, "", "EF", Jetzt));

        var e = s.Entlassen(id, Heute, EntlassGrund.Verlegung, "in ein Haus näher an der Familie (Test)", "EF", Jetzt);
        Assert.Equal((Heute, EntlassGrund.Verlegung, b.Wohnbereich, b.Zimmer), (e.Entlassen, e.Grund, e.Wohnbereich, e.Zimmer));
        Assert.True(s.Archiviert(id));
        Assert.Equal(vorher - 1, s.AlleBewohner().Count);
        Assert.DoesNotContain(s.AlleBewohner(), x => x.Id == id);
        Assert.NotNull(s.Bewohner(id));   // lesbar bleibt er
        Assert.Empty(s.Massnahmen(id));
        Assert.DoesNotContain(s.Aufgaben(Jetzt.AddHours(1)), a => a.BewohnerId == id);
        Assert.Contains(s.FreieBetten(b.Wohnbereich), f => f.Zimmer == b.Zimmer);   // das Bett ist frei
        Assert.Throws<Eingabefehler>(() => s.Entlassen(id, Heute, EntlassGrund.Auszug, "", "EF", Jetzt));   // schon im Archiv
        var zeile = s.Archiv().Single(z => z.Bewohner.Id == id);
        Assert.Equal(Heute.AddYears(10), zeile.AufbewahrenBis);   // Demo-Frist 10 Jahre

        Assert.Throws<Eingabefehler>(() => s.Wiederaufnehmen(id, Heute.AddDays(-1), b.Zimmer, b.Bett, "EF", Jetzt));
        var zurueck = s.Wiederaufnehmen(id, Heute.AddDays(3), b.Zimmer, b.Bett, "EF", Jetzt.AddDays(3));
        Assert.False(s.Archiviert(id));
        Assert.Equal(Heute.AddDays(3), zurueck.Aufnahmedatum);
        Assert.Contains(s.AlleBewohner(), x => x.Id == id);
        Assert.Equal(Heute.AddDays(3), s.ArchivEintraege(id).Single().WiederAufgenommen);
    }

    [Fact]
    public void Demo_Frist_und_Loeschen_zu_zweit_mit_Protokoll()
    {
        var archiv = s.Archiv();
        Assert.Equal(3, archiv.Count);
        Assert.All(archiv, z => Assert.DoesNotContain(s.AlleBewohner(), b => b.Id == z.Bewohner.Id));
        Assert.Equal(10, s.ArchivFrist());
        var alt = Assert.Single(archiv, z => z.Abgelaufen(Heute));
        long id = alt.Bewohner.Id!.Value;
        Assert.NotEmpty(s.Berichte(id));

        Assert.Throws<Eingabefehler>(() => s.LoeschenFreigeben(id, "AB", Jetzt));   // ohne Recht
        s.LoeschenFreigeben(id, "TST", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.LoeschenFreigeben(id, "TST", Jetzt));   // schon freigegeben
        Assert.Throws<Eingabefehler>(() => s.EndgueltigLoeschen(id, "TST", Jetzt));   // nicht dieselbe Person
        var neu = s.Archiv().Single(z => z.Bewohner.Id == id);
        Assert.Equal("TST", neu.Freigabe!.Kuerzel);
        Assert.Throws<Eingabefehler>(() => s.EndgueltigLoeschen(id, "QS", Jetzt));   // QMB: einsehen ja, löschen nein
        var protokoll = s.EndgueltigLoeschen(id, "RS", Jetzt.AddMinutes(5));
        Assert.Null(s.Bewohner(id));
        Assert.Empty(s.Berichte(id));
        Assert.Equal((id, "TST", "RS"), (protokoll.BewohnerNr, protokoll.Freigabe, protokoll.Geloescht));
        Assert.DoesNotContain(alt.Bewohner.Nachname, protokoll.Zeitraum + protokoll.Freigabe + protokoll.Geloescht);
        Assert.Single(s.Loeschprotokolle());

        var noch = s.Archiv().First(z => !z.Abgelaufen(Heute));
        Assert.Throws<Eingabefehler>(() => s.LoeschenFreigeben(noch.Bewohner.Id!.Value, "TST", Jetzt));   // Frist läuft noch
        s.ArchivFristSetzen(null, "TST");
        Assert.Null(s.ArchivFrist());
        Assert.All(s.Archiv(), z => Assert.Null(z.AufbewahrenBis));
        Assert.Throws<Eingabefehler>(() => s.ArchivFristSetzen(5, "AB"));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "DELETE FROM loeschprotokoll";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());
    }
}
