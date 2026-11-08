using DokuLine.Kern;

namespace DokuLine.Tests;

// „Assistenzkraft und Fachkraft“ (0.33.0): Qualifikation, Visiten nur Fachkräfte (auch in bestehenden
// Datenbanken), „Bedarf gegeben …“ mit Pflicht-Absprache für Assistenzkräfte und Bericht in der Akte.
[Collection("Datenordner")]
public sealed class BedarfTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private readonly Datenordner daten = new();
    private Speicher s;

    public BedarfTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Verordnung BedarfVon(string nachname) =>
        s.Verordnungen(s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value).First(v => v.Bedarf);

    private static Mitarbeiter M(string kuerzel, bool fachkraft) => new(kuerzel, "Test", kuerzel, fachkraft, true, ["R1"], ["Mitarbeiter Wohnbereich"]);

    [Fact]
    public void Qualifikation_und_Rechte()
    {
        Assert.Equal("Assistenzkraft", s.MitarbeiterLesen("KL")!.Qualifikation);
        Assert.Equal("Fachkraft", s.MitarbeiterLesen("AB")!.Qualifikation);
        // Assistenzkräfte machen keine Visiten; Bedarf geben dürfen sie (mit Absprache).
        Assert.Equal(RechteRegeln.NurFachkraftText, s.Grund("KL", Funktionen.Visiten));
        Assert.Null(s.Grund("AB", Funktionen.Visiten));
        Assert.Null(s.Grund("KL", Funktionen.BedarfGeben));
        Assert.Null(s.Grund("EF", Funktionen.BedarfGeben));
        Assert.Equal(RechteRegeln.FehltText, s.Grund("OP", Funktionen.BedarfGeben));   // Therapie
        Assert.Contains(Funktionen.Visiten, s.NurFachkraft());
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_Visiten_nur_Fachkraefte_einmal()
    {
        void Sql(string text)
        {
            using var befehl = s.Verbindung.CreateCommand();
            befehl.CommandText = text;
            befehl.ExecuteNonQuery();
        }
        void NeuOeffnen()
        {
            string pfad = s.Pfad;
            s.Dispose();
            s = new Speicher(pfad) { Uhr = () => Jetzt };
        }
        // Wie vor 0.33.0: Visiten ohne „nur Fachkräfte“, Merker fehlt.
        Sql("DELETE FROM rechte_fachkraft WHERE funktion = 'visiten'; DELETE FROM meta WHERE schluessel = 'nur_fachkraft_vorbelegt_visiten'");
        NeuOeffnen();
        Assert.Contains(Funktionen.Visiten, s.NurFachkraft());
        // Danach gilt die Rechte-Tabelle: abgewählt bleibt abgewählt.
        s.NurFachkraftSetzen(Funktionen.Visiten, false);
        NeuOeffnen();
        Assert.DoesNotContain(Funktionen.Visiten, s.NurFachkraft());
    }

    [Fact]
    public void Pruefen_Absprache_Uhrzeit_Menge()
    {
        var v = BedarfVon("Tagetes");
        var fach = new[] { M("AB", true), M("CD", true) };
        BedarfEingabe E(string absprache = "", string uhr = "15:00", string menge = "1") =>
            new() { VerordnungId = v.Id, Uhrzeit = uhr, Menge = menge, Anlass = "bei starker Unruhe", Absprache = absprache };
        // Assistenzkraft: ohne Absprache nicht.
        Assert.Contains(BedarfRegeln.AbspracheText, Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(E(), v, M("KL", false), fach, Jetzt)).Fehler);
        var g = BedarfRegeln.Pruefen(E("ab"), v, M("KL", false), fach, Jetzt);
        Assert.Equal("AB", g.Absprache);
        Assert.Equal(new DateTime(2026, 11, 7, 15, 0, 0), g.Zeit);
        // Nur mit einer Fachkraft, nicht mit sich selbst.
        Assert.Contains(BedarfRegeln.KeineFachkraftText, Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(E("KL"), v, M("KL", false), fach, Jetzt)).Fehler);
        Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(E("AB"), v, M("AB", true), fach, Jetzt));
        // Fachkraft: ohne Absprache.
        Assert.Null(BedarfRegeln.Pruefen(E(), v, M("AB", true), fach, Jetzt).Absprache);
        // Uhrzeit in der Zukunft, Menge keine Zahl.
        Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(E(uhr: "18:00"), v, M("AB", true), fach, Jetzt));
        Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(E(menge: "eine"), v, M("AB", true), fach, Jetzt));
        Assert.Equal("0,5", BedarfRegeln.Pruefen(E(menge: "0,5"), v, M("AB", true), fach, Jetzt).Menge);
    }

    [Fact]
    public void Hoechstmenge_und_Heute()
    {
        Assert.Equal(2, BedarfRegeln.Hoechstens("2 in 24 Stunden"));
        Assert.Null(BedarfRegeln.Hoechstens(""));
        Assert.Null(BedarfRegeln.Hoechstens("höchstens zweimal"));
        var v = BedarfVon("Tagetes") with { BedarfHoechstens = "2 in 24 Stunden" };
        Bedarfsgabe G(int vorStunden, string k = "AB", string? a = null) => new() { Zeit = Jetzt.AddHours(-vorStunden), Menge = "1", Kuerzel = k, Absprache = a };
        Assert.Null(BedarfRegeln.Warnung(v, [G(3), G(30)], Jetzt));               // nur eine in 24 Stunden
        Assert.StartsWith("Achtung: höchstens 2 in 24 Stunden – in den 24 Stunden davor schon 2×", BedarfRegeln.Warnung(v, [G(3), G(20)], Jetzt));
        Assert.Equal("Heute noch nicht gegeben.", BedarfRegeln.HeuteText([G(30)], DateOnly.FromDateTime(Jetzt), "Stück"));
        Assert.Equal("Heute 2× gegeben: 12:20 (1 Stück, AB), 14:20 (1 Stück, KL mit AB)",
            BedarfRegeln.HeuteText([G(1, "KL", "AB"), G(3)], DateOnly.FromDateTime(Jetzt), "Stück"));
    }

    [Fact]
    public void Gabe_eintragen_mit_Bericht_und_Demo()
    {
        var v = BedarfVon("Tagetes");
        // Demo: zwei Gaben (eine von KL mit Absprache AB), jede mit Bericht.
        var demo = s.Bedarfsgaben(v.Id);
        Assert.Equal(2, demo.Count);
        Assert.Contains(demo, g => g.Kuerzel == "KL" && g.Absprache == "AB" && g.BerichtId is not null);
        var berichte = s.Berichte(v.BewohnerId);
        Assert.Contains(berichte, b => b.Text.StartsWith("Bedarfsmedikation gegeben um ") && b.Text.EndsWith("In Absprache mit Anna Bachstelze (AB)."));

        // KL (Assistenzkraft) ohne Absprache: nein; mit CD: ja, Bericht von KL.
        var e = new BedarfEingabe { VerordnungId = v.Id, Uhrzeit = "15:10", Menge = "1", Anlass = "bei starker Unruhe" };
        Assert.Contains(BedarfRegeln.AbspracheText, Assert.Throws<Eingabefehler>(() => s.BedarfGeben(e, "KL", Jetzt)).Fehler);
        e.Absprache = "CD";
        var neu = s.BedarfGeben(e, "KL", Jetzt);
        Assert.Equal("CD", neu.Absprache);
        var bericht = s.Berichte(v.BewohnerId).Single(b => b.Id == neu.BerichtId);
        Assert.Equal("KL", bericht.Verfasser);
        Assert.Equal([5], bericht.Bereiche);
        Assert.Contains("1 Stück. Anlass: bei starker Unruhe. In Absprache mit Carl Dohle (CD).", bericht.Text);
        Assert.Equal(3, s.Bedarfsgaben(v.Id).Count);
        // Ohne Recht (Therapie) nicht.
        Assert.Equal([RechteRegeln.FehltText], Assert.Throws<Eingabefehler>(() => s.BedarfGeben(e, "OP", Jetzt)).Fehler);
        // Keine Bedarfsverordnung.
        var grund = s.Verordnungen(v.BewohnerId).FirstOrDefault(x => !x.Bedarf);
        if (grund is not null)
            Assert.Throws<Eingabefehler>(() => s.BedarfGeben(new BedarfEingabe { VerordnungId = grund.Id, Uhrzeit = "15:00", Menge = "1", Anlass = "x" }, "AB", Jetzt));
    }
}
