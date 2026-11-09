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
    public void Hoechstmenge_und_letzte_24_Stunden()
    {
        Assert.Equal(2, BedarfRegeln.Hoechstens("2 in 24 Stunden"));
        Assert.Null(BedarfRegeln.Hoechstens(""));
        Assert.Null(BedarfRegeln.Hoechstens("höchstens zweimal"));
        var v = BedarfVon("Tagetes") with { BedarfHoechstens = "2 in 24 Stunden", Einheit = "Stück" };
        Assert.Equal("Höchstens: 2 in 24 Stunden", BedarfRegeln.HoechstensText(v));
        Assert.EndsWith("ohne Zahl vorne kann das Programm die Menge nicht prüfen.", BedarfRegeln.HoechstensText(v with { BedarfHoechstens = "höchstens zweimal" }));
        Assert.EndsWith("kann die Menge nicht prüfen.", BedarfRegeln.HoechstensText(v with { BedarfHoechstens = "" }));
        Bedarfsgabe G(double vorStunden, string k = "AB", string? a = null, string menge = "1") =>
            new() { Zeit = Jetzt.AddHours(-vorStunden), Menge = menge, Kuerzel = k, Absprache = a };
        Assert.Null(BedarfRegeln.Warnung(v, [G(3), G(30)], Jetzt, 1));               // eine in 24 Stunden + diese = 2
        Assert.Equal("Achtung: höchstens 2 in 24 Stunden – mit dieser Gabe 3 Stück innerhalb von 24 Stunden. Bitte mit einer Fachkraft klären.",
            BedarfRegeln.Warnung(v, [G(3), G(20)], Jetzt, 1));
        // 0.33.2: die Menge zählt, nicht die Zahl der Gaben.
        Assert.NotNull(BedarfRegeln.Warnung(v, [G(3, menge: "2")], Jetzt, 1));      // vorher: nur eine Gabe → keine Warnung
        Assert.NotNull(BedarfRegeln.Warnung(v, [], Jetzt, 3));
        Assert.Null(BedarfRegeln.Warnung(v, [G(3, menge: "0,5"), G(5, menge: "0,5")], Jetzt, 1));
        // 0.33.2: Nachtrag – das Fenster bis zu einer späteren Gabe zählt mit.
        Assert.Null(BedarfRegeln.Warnung(v, [G(30)], Jetzt.AddHours(-10), 1));
        var nachtrag = BedarfRegeln.Warnung(v, [G(2), G(1)], Jetzt.AddHours(-10), 1);   // 05:20 nachgetragen, danach 13:20 und 14:20
        Assert.NotNull(nachtrag);
        Assert.Contains("(bis zur Gabe um 14:20)", nachtrag);

        Assert.Equal("In den letzten 24 Stunden noch nicht gegeben.", BedarfRegeln.Letzte24Text([G(30)], Jetzt, "Stück"));
        Assert.Equal("In den letzten 24 Stunden 2× gegeben: 12:20 (1 Stück, AB), 14:20 (1 Stück, KL mit AB)",
            BedarfRegeln.Letzte24Text([G(1, "KL", "AB"), G(3)], Jetzt, "Stück"));
        Assert.Equal("in 24 Std. 2× gegeben", BedarfRegeln.Letzte24Kurz([G(1), G(3), G(30)], Jetzt));
        Assert.Equal("in 24 Std. nicht gegeben", BedarfRegeln.Letzte24Kurz([G(30)], Jetzt));
    }

    // 0.33.2: kurz nach Mitternacht meint eine spätere Uhrzeit gestern; die Anzeige zeigt die letzten 24 Stunden.
    [Fact]
    public void Gabe_kurz_nach_Mitternacht_gilt_fuer_gestern()
    {
        var nacht = new DateTime(2026, 11, 8, 0, 10, 0);
        Assert.Equal(new DateTime(2026, 11, 7, 23, 50, 0), BedarfRegeln.Zeitpunkt("23:50", nacht));
        Assert.Equal(new DateTime(2026, 11, 8, 0, 5, 0), BedarfRegeln.Zeitpunkt("0:05", nacht));
        Assert.Equal(new DateTime(2026, 11, 8, 0, 14, 0), BedarfRegeln.Zeitpunkt("00.14", nacht));   // bis 5 Minuten voraus: heute
        Assert.Equal(new DateTime(2026, 11, 8, 18, 0, 0), BedarfRegeln.Zeitpunkt("18:00", new DateTime(2026, 11, 8, 15, 20, 0)));   // weiter als 12 h: Zukunft
        Assert.Null(BedarfRegeln.Zeitpunkt("spät", nacht));

        var v = BedarfVon("Tagetes");
        var e = new BedarfEingabe { VerordnungId = v.Id, Uhrzeit = "23:50", Menge = "1", Anlass = "bei starker Unruhe" };
        var g = BedarfRegeln.Pruefen(e, v, M("AB", true), [M("AB", true)], nacht);
        Assert.Equal(new DateTime(2026, 11, 7, 23, 50, 0), g.Zeit);
        Assert.Equal("In den letzten 24 Stunden 1× gegeben: gestern 23:50 (1 Stück, AB)", BedarfRegeln.Letzte24Text([g], nacht, v.Einheit));
        e.Uhrzeit = "18:00";
        Assert.Contains("Uhrzeit: Die Gabe liegt in der Zukunft.", Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(e, v, M("AB", true), [], new DateTime(2026, 11, 8, 15, 20, 0))).Fehler);
    }

    // 0.33.2: Absprache so gespeichert wie in der Mitarbeiterliste; ein Administrator ohne Qualifikation „Fachkraft“
    // braucht wie jede Assistenzkraft eine Absprache.
    [Fact]
    public void Absprache_Kuerzel_und_keine_Ausnahme_fuer_Administratoren()
    {
        var v = BedarfVon("Tagetes");
        var e = new BedarfEingabe { VerordnungId = v.Id, Uhrzeit = "15:00", Menge = "1", Anlass = "x", Absprache = "ab" };
        Assert.Equal("Ab", BedarfRegeln.Pruefen(e, v, M("KL", false), [M("Ab", true)], Jetzt).Absprache);
        var admin = new Mitarbeiter("ADM", "Test", "Admin", false, true, ["R1"], [RechteRegeln.Administrator]);
        e.Absprache = "";
        Assert.Contains(BedarfRegeln.AbspracheText, Assert.Throws<Eingabefehler>(() => BedarfRegeln.Pruefen(e, v, admin, [], Jetzt)).Fehler);
        Assert.Null(BedarfRegeln.Pruefen(e, v, admin with { Fachkraft = true }, [], Jetzt).Absprache);
    }

    // 0.33.2: Holt der Start die Medikation nach, bleiben keine Berichte von Bedarfsgaben ohne Gabe zurück; vorher
    // legt er eine Sicherungskopie an.
    [Fact]
    public void Nachholen_der_Medikation_laesst_keine_Bedarf_Berichte_zurueck()
    {
        int BedarfBerichte()
        {
            using var befehl = s.Verbindung.CreateCommand();
            befehl.CommandText = "SELECT COUNT(*) FROM berichte WHERE text LIKE 'Bedarfsmedikation gegeben um %'";
            return Convert.ToInt32(befehl.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
        int vorher = BedarfBerichte();
        Assert.True(vorher > 0);
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM meta WHERE schluessel = 'demo_medikation'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(vorher, BedarfBerichte());   // vorher: doppelt (alte blieben stehen)
        Assert.Contains(s.Startprotokoll, z => z.StartsWith("Demo-Daten nach dem Update ergänzt"));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(s.Pfad)!, "*-vor-demo-nachholen.db"));
    }

    // 0.33.2: Gaben desselben Medikaments aus einer zweiten Bedarfsverordnung zählen mit.
    [Fact]
    public void Gaben_desselben_Medikaments_zaehlen_zusammen()
    {
        var v = BedarfVon("Tagetes");
        var zweite = s.Verordnungen(v.BewohnerId).FirstOrDefault(x => x.Id != v.Id && x.MedikamentId == v.MedikamentId);
        var alle = s.BedarfsgabenMittel(v);
        Assert.Equal(s.Bedarfsgaben(v.Id).Count + (zweite is null ? 0 : s.Bedarfsgaben(zweite.Id).Count), alle.Count);
        Assert.All(alle, g => Assert.Equal(v.BewohnerId, g.BewohnerId));
        // Eine andere Bewohnerin mit Bedarf: ihre Gaben zählen hier nicht.
        var andere = s.AlleBewohner().Where(b => b.Id != v.BewohnerId).SelectMany(b => s.Verordnungen(b.Id!.Value)).First(x => x.Bedarf && s.Bedarfsgaben(x.Id).Count > 0);
        Assert.DoesNotContain(alle, g => g.VerordnungId == andere.Id);
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
