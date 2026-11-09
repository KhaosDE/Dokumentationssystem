using DokuLine.Kern;

namespace DokuLine.Tests;

// Wohnbereich einmal in der Übersicht wählen (Knöpfe je Recht, Vorgabe, wer dazugehört) und Aufgaben, die zu einem
// Bewohner oder einem Bereich gehören – auch in einer Datenbank aus der Zeit davor.
[Collection("Datenordner")]
public sealed class BereichswahlTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 15, 20, 0);
    private readonly Datenordner daten = new();
    private Speicher s;

    public BereichswahlTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private List<string> Knoepfe(string kuerzel) =>
        BereichswahlRegeln.Knoepfe(s.Wohnbereiche().Select(w => w.Name), f => s.Grund(kuerzel, f) is null);

    private void Sql(string text)
    {
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = text;
        befehl.ExecuteNonQuery();
    }

    [Fact]
    public void Knoepfe_je_Position()
    {
        Assert.Equal(["R1", "R2", "R3"], Knoepfe("CD"));                   // Mitarbeiter Wohnbereich: kein „Therapie“
        Assert.Equal(["R1", "R2", "R3", "Therapie"], Knoepfe("OP"));       // Therapie springt auf den Wohnbereichen ein
        Assert.Equal(["R1", "R2", "R3", "Therapie", "Alle"], Knoepfe("TST"));   // Administrator
        Assert.Empty(Knoepfe(""));                                         // ohne Anmeldung
    }

    [Fact]
    public void Vorgabe_und_wer_dazugehoert()
    {
        Assert.Equal("R2", BereichswahlRegeln.Start("R2", ["R1", "R2", "R3"]));
        Assert.Equal("R1", BereichswahlRegeln.Start("Therapie", ["R1", "R2", "R3"]));   // Rechner der Therapie, ohne den Knopf
        Assert.Equal("R2", BereichswahlRegeln.Start("R2", []));
        var alle = s.AlleBewohner();
        Assert.All(BereichswahlRegeln.Bewohner(alle, "R2"), b => Assert.Equal("R2", b.Wohnbereich));
        Assert.Equal(23, BereichswahlRegeln.Bewohner(alle, "R2").Count);
        Assert.Equal(alle.Count, BereichswahlRegeln.Bewohner(alle, BereichswahlRegeln.Alle).Count);
        Assert.Equal(alle.Count, BereichswahlRegeln.Bewohner(alle, "Therapie").Count);   // ohne eigene Bewohner: alle
        Assert.True(BereichswahlRegeln.Passt("R3", BereichswahlRegeln.Alle));
        Assert.False(BereichswahlRegeln.Passt("R3", "R1"));
        Assert.True(BereichswahlRegeln.Eingesprungen("R2", "R1"));
        Assert.False(BereichswahlRegeln.Eingesprungen("R1", "R1"));
        Assert.False(BereichswahlRegeln.Eingesprungen(BereichswahlRegeln.Alle, "R1"));
    }

    [Fact]
    public void Aufgabe_ohne_Bewohner_braucht_einen_Bereich_mit_Bewohner_dessen_Bereich()
    {
        var e = new AufgabeEingabe { Text = "Fenster putzen", Verfasser = "AB" };
        Assert.Contains(AufgabenRegeln.BereichFehlt, Assert.Throws<Eingabefehler>(() => AufgabenRegeln.AusEingabe(e, Jetzt)).Fehler);
        e.Bereich = "R2";
        var a = s.AufgabeSpeichern(AufgabenRegeln.AusEingabe(e, Jetzt));
        Assert.Equal("R2", a.Bereich);
        Assert.Contains(s.Aufgaben(Jetzt, bereich: "R2"), x => x.Id == a.Id);
        Assert.DoesNotContain(s.Aufgaben(Jetzt, bereich: "R3"), x => x.Id == a.Id);
        // Mit Bewohner gehört sie zu dessen Bereich, auch nach einem Umzug.
        var birke = s.AlleBewohner().First(b => b.Nachname == "Birke");
        var mit = s.AufgabeSpeichern(AufgabenRegeln.AusEingabe(new AufgabeEingabe
        {
            Text = "Termin bestätigen", Verfasser = "AB", BewohnerId = birke.Id, Bereich = "R3",
        }, Jetzt));
        Assert.Equal(birke.Wohnbereich, mit.Bereich);
        Assert.Equal(s.OffeneAufgaben(), new[] { "R1", "R2", "R3" }.Sum(b => s.OffeneAufgaben(b)));
        // Umbenennen eines Bereichs nimmt Aufgaben und Standardaufgaben mit.
        s.ListeneintragUmbenennen(Liste.Bereiche, "R2", "Nord");
        Assert.Equal("Nord", s.AufgabeLesen(a.Id)!.Bereich);
        Assert.Contains(s.Vorlagen(), v => v.Bereich == "Nord");
        Assert.DoesNotContain(s.Vorlagen(), v => v.Bereich == "R2");
    }

    [Fact]
    public void Bestehende_Datenbank_ordnet_Aufgaben_und_Standardaufgaben_zu()
    {
        // Wie vor Schritt 32: Aufgaben ohne Bereich, eine Standardaufgabe für alle.
        Sql("""
            DELETE FROM aufgaben WHERE vorlage_id IS NOT NULL;
            DELETE FROM aufgabe_vorlagen;
            DROP TRIGGER aufgaben_bleiben;
            ALTER TABLE aufgaben DROP COLUMN bereich;
            ALTER TABLE aufgabe_vorlagen DROP COLUMN bereich;
            CREATE TRIGGER aufgaben_bleiben BEFORE UPDATE ON aufgaben
                BEGIN SELECT RAISE(ABORT, 'Aufgaben werden nicht überschrieben – Änderungen sind Ereignisse.'); END;
            INSERT INTO aufgabe_vorlagen (text, schicht, tage, aktiv, erstellt, kuerzel)
                VALUES ('Rundgang', 'nacht', 127, 1, '2026-11-01T10:00', 'AB');
            ALTER TABLE visite_entwuerfe DROP COLUMN art;
            PRAGMA user_version = 31;
            """);
        int ohneBewohner;
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "SELECT COUNT(*) FROM aufgaben WHERE bewohner_id IS NULL";
            ohneBewohner = Convert.ToInt32(befehl.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
        string pfad = s.Pfad;
        s.Dispose();
        s = new Speicher(pfad) { Uhr = () => Jetzt };
        var alle = s.Aufgaben(Jetzt, auchErledigte: true);
        Assert.Equal(ohneBewohner, alle.Count(a => a.BewohnerId is null && a.Bereich == "R1"));   // unbekannt: R1
        Assert.All(alle.Where(a => a.BewohnerId is not null), a => Assert.NotEqual("", a.Bereich));
        Assert.Equal(["R1", "R2", "R3"], s.Vorlagen().Where(v => v.Text == "Rundgang").Select(v => v.Bereich).Order());
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => Sql("UPDATE aufgaben SET text = 'anders'"));
    }
}
