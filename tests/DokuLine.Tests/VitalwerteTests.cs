using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 36: Vitalwerte – erfassen, Korrektur mit Grund, angeordneter Bereich vor Richtwert, BMI, Messaufträge als Aufgaben,
// Rechte, Demo, Datenbank.
[Collection("Datenordner")]
public sealed class VitalwerteTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);   // Montag, Frühdienst
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public VitalwerteTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static Vitalwert W(string art, double wert, double? wert2 = null, string bemerkung = "") =>
        new(1, 1, Jetzt, art, wert, wert2, bemerkung, "EF", Jetzt);

    [Fact]
    public void Erfassen_mehrere_Werte_und_Korrektur_nur_mit_Grund()
    {
        long id = s.AlleBewohner()[8].Id!.Value;   // in der Demo ohne Werte
        Assert.Empty(s.Vitalwerte(id));
        var neu = s.VitalwerteErfassen(id, Jetzt.AddMinutes(-10), [(VitalRegeln.Blutdruck, 128, 84), (VitalRegeln.Puls, 72, null)], "in Ruhe", "EF", Jetzt);
        Assert.Equal(2, neu.Count);
        Assert.All(neu, w => Assert.Equal(("in Ruhe", "EF"), (w.Bemerkung, w.Kuerzel)));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => s.VitalwerteErfassen(id, Jetzt, [(VitalRegeln.Blutdruck, 128, null)], "", "EF", Jetzt)).Fehler,
            f => f.Contains("systolisch und diastolisch"));
        Assert.Throws<Eingabefehler>(() => s.VitalwerteErfassen(id, Jetzt, [(VitalRegeln.Temperatur, 368, null)], "", "EF", Jetzt));   // Tippfehler
        Assert.Throws<Eingabefehler>(() => s.VitalwerteErfassen(id, Jetzt, [], "", "EF", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.VitalwerteErfassen(id, Jetzt.AddHours(1), [(VitalRegeln.Puls, 70, null)], "", "EF", Jetzt));

        var puls = neu.Single(w => w.Art == VitalRegeln.Puls);
        Assert.Contains("Grund", Assert.Throws<Eingabefehler>(() => s.VitalwertKorrigieren(puls.Id, 78, null, "", " ", "EF", Jetzt)).Fehler[0]);
        var korr = s.VitalwertKorrigieren(puls.Id, 78, null, "", "Zahlendreher", "EF", Jetzt);
        Assert.Equal((78.0, puls.Id, "Zahlendreher", puls.Zeit), (korr.Wert, korr.Vorgaenger, korr.Grund, korr.Zeit));
        Assert.Equal(2, s.Vitalwerte(id).Count);                        // der alte zählt nicht mehr
        Assert.Equal(3, s.Vitalwerte(id, auchAbgeloeste: true).Count);   // bleibt aber gespeichert
        Assert.Contains("schon korrigiert", Assert.Throws<Eingabefehler>(() => s.VitalwertKorrigieren(puls.Id, 80, null, "", "nochmal", "EF", Jetzt)).Fehler[0]);
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE vitalwerte SET wert = 1";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Angeordneter_Bereich_geht_vor_sonst_Richtwert()
    {
        var r = new VitalRichtwerte();
        Assert.Equal("über dem allgemeinen Richtwert", VitalRegeln.Bewerten(W(VitalRegeln.Blutdruck, 145, 80), null, r)?.Text);
        Assert.Equal("unter dem allgemeinen Richtwert", VitalRegeln.Bewerten(W(VitalRegeln.Blutdruck, 120, 55), null, r)?.Text);
        Assert.Null(VitalRegeln.Bewerten(W(VitalRegeln.Blutdruck, 139, 89), null, r));
        Assert.Equal("Fieber – über dem allgemeinen Richtwert", VitalRegeln.Bewerten(W(VitalRegeln.Temperatur, 38.0), null, r)?.Text);
        Assert.Equal("erhöht – über dem allgemeinen Richtwert", VitalRegeln.Bewerten(W(VitalRegeln.Temperatur, 37.5), null, r)?.Text);
        Assert.Null(VitalRegeln.Bewerten(W(VitalRegeln.Blutzucker, 120), null, r));                             // nicht nüchtern: erst ab 140
        Assert.NotNull(VitalRegeln.Bewerten(W(VitalRegeln.Blutzucker, 120, bemerkung: "Nüchtern"), null, r));  // nüchtern: ab 100
        Assert.Equal("unter dem allgemeinen Richtwert", VitalRegeln.Bewerten(W(VitalRegeln.Saettigung, 93), null, r)?.Text);
        Assert.False(VitalRegeln.Bewerten(W(VitalRegeln.Puls, 101), null, r)!.Angeordnet);
        // Mit Anordnung: nur „außerhalb des angeordneten Bereichs“, kein Richtwert-Hinweis mehr.
        var g = new VitalGrenze(1, 1, VitalRegeln.Blutdruck, 100, 160, 60, 95, 7, DateOnly.FromDateTime(Jetzt), "EF", Jetzt);
        Assert.Null(VitalRegeln.Bewerten(W(VitalRegeln.Blutdruck, 150, 90), g, r));
        Assert.Equal(new VitalHinweis(VitalRegeln.Ausserhalb, true), VitalRegeln.Bewerten(W(VitalRegeln.Blutdruck, 150, 96), g, r));
        var aufgehoben = g with { Von = null, Bis = null, Von2 = null, Bis2 = null };
        Assert.False(aufgehoben.Gilt);
        Assert.False(VitalRegeln.Bewerten(W(VitalRegeln.Blutdruck, 150, 96), aufgehoben, r)!.Angeordnet);
    }

    [Fact]
    public void BMI_Einheiten_und_letzte_Werte()
    {
        Assert.Equal(1.78, VitalRegeln.Groesse("1,78 m"));
        Assert.Equal(1.78, VitalRegeln.Groesse("178 cm"));
        Assert.Null(VitalRegeln.Groesse("groß"));
        Assert.Equal(23.7, VitalRegeln.BmiWert(75, "1,78 m"));
        var r = new VitalRichtwerte();
        Assert.Null(VitalRegeln.BmiHinweis(23.7, r));
        Assert.StartsWith("Übergewicht", VitalRegeln.BmiHinweis(27, r)!.Text);
        Assert.StartsWith("Adipositas", VitalRegeln.BmiHinweis(31, r)!.Text);
        Assert.Equal("128/84 mmHg", VitalRegeln.WertText(W(VitalRegeln.Blutdruck, 128, 84), r));
        Assert.Equal("36,8 °C", VitalRegeln.WertText(W(VitalRegeln.Temperatur, 36.8), r));
        var mmol = r with { BlutzuckerEinheit = VitalRegeln.MmolL };
        Assert.Equal("5,4 mmol/l", VitalRegeln.WertText(W(VitalRegeln.Blutzucker, 97), mmol));
        Assert.Equal(97, VitalRegeln.Speicherwert(VitalRegeln.Blutzucker, 5.4, mmol));
        var letzte = VitalRegeln.Letzte([W(VitalRegeln.Gewicht, 75), W(VitalRegeln.Puls, 70) with { Id = 2 }, W(VitalRegeln.Puls, 90) with { Id = 3, Zeit = Jetzt.AddDays(-1) }], r, "1,78 m");
        Assert.Equal(["Puls: 70 /min (09.11.2026 09:00)", "Gewicht: 75 kg (09.11.2026 09:00)", "BMI: 23,7 (aus Gewicht und Größe)"], letzte);
    }

    [Fact]
    public void Messauftraege_werden_Aufgaben_einmal_je_Schicht()
    {
        long id = s.AlleBewohner()[8].Id!.Value;
        int vorher = s.Aufgaben(Jetzt).Count(a => a.BewohnerId == id);
        var m = s.MessauftragAnlegen(new Messauftrag(0, id, VitalRegeln.Gewicht, Messrhythmus.Woechentlich, (int)DayOfWeek.Monday, Schicht.Frueh,
            DateOnly.FromDateTime(Jetzt), null, "TST", Jetzt), "TST", Jetzt);
        s.StandardaufgabenErzeugen(Jetzt);
        s.StandardaufgabenErzeugen(Jetzt.AddMinutes(30));   // dieselbe Schicht: nicht doppelt
        Assert.Equal(vorher + 1, s.Aufgaben(Jetzt).Count(a => a.BewohnerId == id));
        Assert.Contains(s.Aufgaben(Jetzt), a => a.BewohnerId == id && a.Text == "Gewicht messen");
        Assert.False(VitalRegeln.Faellig(m, new SchichtTermin(DateOnly.FromDateTime(Jetzt).AddDays(1), Schicht.Frueh)));   // Dienstag
        Assert.False(VitalRegeln.Faellig(m, new SchichtTermin(DateOnly.FromDateTime(Jetzt), Schicht.Spaet)));
        s.MessauftragBeenden(m.Id, "TST", DateOnly.FromDateTime(Jetzt));
        Assert.DoesNotContain(s.Messauftraege(id), x => x.Id == m.Id);
        Assert.Contains(s.Messauftraege(id, auchBeendete: true), x => x.Id == m.Id);
        Assert.NotNull(VitalRegeln.AuftragPruefen(m with { Rhythmus = Messrhythmus.Monatlich, Tag = 31 }));
    }

    [Fact]
    public void Rechte_Grenzen_nur_mit_Arzt_und_nur_Fachkraefte()
    {
        long id = s.AlleBewohner()[1].Id!.Value;
        var g = new VitalGrenze(0, id, VitalRegeln.Puls, 50, 110, null, null, null, DateOnly.FromDateTime(Jetzt), "TST", Jetzt);
        Assert.Contains("anordnenden Arzt", Assert.Throws<Eingabefehler>(() => s.GrenzeSetzen(g, "TST", Jetzt)).Fehler[0]);
        long arzt = s.Personen(PersonGruppe.Arzt)[0].Id;
        s.GrenzeSetzen(g with { ArztId = arzt }, "TST", Jetzt);
        Assert.Equal(110, s.Grenzen(id)[VitalRegeln.Puls].Bis);
        s.GrenzeSetzen(g with { Von = null, Bis = null }, "TST", Jetzt.AddMinutes(1));   // aufheben
        Assert.False(s.Grenzen(id).ContainsKey(VitalRegeln.Puls));
        Assert.True(RechteRegeln.NurFachkraftVorgabe.Contains(Funktionen.VitalGrenzen));
        Assert.True(RechteRegeln.Vorbelegt("Therapie", Funktionen.VitalwerteErfassen));
        Assert.False(RechteRegeln.Vorbelegt("Therapie", Funktionen.VitalGrenzen));
        // Eine Assistenzkraft darf erfassen, aber keine Grenzen setzen.
        var assistenz = s.AlleMitarbeiter().First(m => m.Aktiv && !m.Fachkraft && !RechteRegeln.IstAdministrator(m) && m.Positionen.Contains("Mitarbeiter Wohnbereich"));
        Assert.Null(s.Grund(assistenz.Kuerzel, Funktionen.VitalwerteErfassen));
        Assert.Equal(RechteRegeln.NurFachkraftText, s.Grund(assistenz.Kuerzel, Funktionen.VitalGrenzen));
        Assert.Throws<Eingabefehler>(() => s.RichtwerteSpeichern(new VitalRichtwerte(), assistenz.Kuerzel));
        s.RichtwerteSpeichern(new VitalRichtwerte { PulsHoch = 110, BlutzuckerEinheit = VitalRegeln.MmolL }, "TST");
        Assert.Equal((110.0, VitalRegeln.MmolL), (s.Richtwerte().PulsHoch, s.Richtwerte().BlutzuckerEinheit));
        Assert.Throws<Eingabefehler>(() => s.ListeneintragUmbenennen(Liste.Vitalarten, VitalRegeln.Puls, "Herzschlag"));
        s.ListeneintragAnlegen(Liste.Vitalarten, "Schmerzskala");
        Assert.Contains("Schmerzskala", s.Vitalarten());
    }

    [Fact]
    public void Demo_mit_Verlauf_Anordnung_und_Ueberleitung()
    {
        var alle = s.AlleBewohner();
        var erster = s.Vitalwerte(alle[0].Id!.Value);
        Assert.True(erster.Count(w => w.Art == VitalRegeln.Blutdruck) >= 10);   // über Monate
        Assert.True(erster.Max(w => w.Zeit) - erster.Min(w => w.Zeit) > TimeSpan.FromDays(90));
        Assert.Empty(s.Vitalwerte(alle[8].Id!.Value));
        var grenze = s.Grenzen(alle[0].Id!.Value)[VitalRegeln.Blutdruck];
        Assert.NotNull(grenze.ArztId);
        Assert.Equal(VitalRegeln.Ausserhalb, VitalRegeln.Bewerten(erster.First(w => w.Art == VitalRegeln.Blutdruck), grenze, s.Richtwerte())?.Text);   // der neueste
        Assert.NotEmpty(s.Messauftraege());
        var bogen = UeberleitungRegeln.Zusammenstellen(s.UeberleitungDaten(alle[0].Id!.Value, Jetzt), new UeberleitungEingabe { Zeitpunkt = Jetzt });
        var zeilen = bogen.Single(a => a.Titel == UeberleitungRegeln.Vitalwerte).Zeilen;
        Assert.True(zeilen.Any(z => z.StartsWith("Blutdruck: 162/98 mmHg")), string.Join(" | ", zeilen));
    }

    [Fact]
    public void Bestehende_Datenbank_bekommt_Tabellen_und_Demo()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DROP TABLE messauftrag_aufgaben; DROP TABLE messauftraege; DROP TABLE vital_grenzen; DROP TABLE vitalwerte; DROP TABLE vitalarten; " +
                                 "DELETE FROM meta WHERE schluessel = 'demo_vitalwerte'; PRAGMA user_version = 38";
            befehl.ExecuteNonQuery();
        }
        string pfad = s.Pfad;
        s.Dispose();
        using var neu = new Speicher(pfad) { Uhr = () => Jetzt };
        Assert.Equal(VitalRegeln.Feste, neu.Vitalarten());
        Assert.True(neu.DemoBeimErstenStart());
        Assert.NotEmpty(neu.Vitalwerte(neu.AlleBewohner()[0].Id!.Value));
    }
}
