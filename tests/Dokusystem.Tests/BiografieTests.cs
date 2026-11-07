using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Biografie: feste Abschnitte, Fließtext ohne Aufzählungszeichen, „vom Bewohner angegeben“ mit
// Hinweis auf die Formulierung, fehlende Angaben, Fließtext zum Kopieren, Fassungen mit Verlauf.
[Collection("Datenordner")]
public sealed class BiografieTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public BiografieTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;

    [Fact]
    public void Pruefregeln_und_Formulierung()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => BiografieRegeln.AusEingabe(0, "- Schwester\n- Bruder", false, "", Jetzt)).Fehler;
        Assert.Contains("Bitte als Fließtext schreiben, ohne Aufzählungszeichen.", fehler);
        Assert.Contains("Kürzel fehlt (oben rechts „Angemeldet als …“).", fehler);
        Assert.Throws<Eingabefehler>(() => BiografieRegeln.AusEingabe(0, "1. Schule", false, "AB", Jetzt));
        Assert.Throws<Eingabefehler>(() => BiografieRegeln.AusEingabe(6, "Text", false, "AB", Jetzt));
        // Ein Gedankenstrich mitten im Satz ist keine Aufzählung.
        var f = BiografieRegeln.AusEingabe(0, "  Er ist – wie er sagt – gern draußen.\n\n\n\nZweiter Absatz.  ", true, " AB ", Jetzt);
        Assert.Equal("Er ist – wie er sagt – gern draußen." + Environment.NewLine + Environment.NewLine + "Zweiter Absatz.", f.Text);
        Assert.Equal("AB", f.Kuerzel);
        Assert.True(BiografieRegeln.FormulierungFehlt(f));
        Assert.False(BiografieRegeln.FormulierungFehlt(f with { Text = "Er gibt an, dass er gern draußen ist." }));
        Assert.False(BiografieRegeln.FormulierungFehlt(f with { Angegeben = false }));
        Assert.False(BiografieRegeln.AusEingabe(0, "  ", true, "AB", Jetzt).Angegeben);   // leer: nichts angegeben
    }

    [Fact]
    public void Fassungen_Verlauf_und_unveraenderbar()
    {
        long id = B("Ginster");
        Assert.All(s.Biografie(id), a => Assert.True(a.Leer));
        Assert.Equal(6, BiografieRegeln.FehlendeAngaben(s.Biografie(id)).Count);
        s.BiografieSpeichern(id, BiografieRegeln.AusEingabe(2, "", false, "AB", Jetzt));   // leer bleibt leer: keine Fassung
        Assert.Empty(s.Biografie(id)[2].Fassungen);
        s.BiografieSpeichern(id, BiografieRegeln.AusEingabe(2, "Er wohnte zuletzt bei seinem Bruder.", false, "AB", Jetzt));
        s.BiografieSpeichern(id, BiografieRegeln.AusEingabe(2, "Er wohnte zuletzt bei seinem Bruder.", false, "CD", Jetzt));   // unverändert
        var a = s.BiografieSpeichern(id, BiografieRegeln.AusEingabe(2, "Er gibt an, dass er zuletzt bei seinem Bruder wohnte.", true, "CD", Jetzt.AddHours(1)));
        Assert.Equal(2, a.Fassungen.Count);
        Assert.Equal("CD", a.Aktuell!.Kuerzel);
        var verlauf = BiografieRegeln.Verlauf(a);
        Assert.StartsWith("07.11.2026, 15:20 · angelegt von AB", verlauf[0]);
        Assert.Contains("geändert von CD · vom Bewohner angegeben", verlauf[1]);
        Assert.DoesNotContain("Wohnen", BiografieRegeln.FehlendeAngaben(s.Biografie(id)));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE biografie SET text = 'x'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Als_Fliesstext_mit_fehlenden_Angaben()
    {
        var text = BiografieRegeln.AlsFliesstext("Dieter Distel", s.Biografie(B("Distel")));
        Assert.StartsWith("Biografie Dieter Distel", text);
        Assert.Contains("Interessen und Gewohnheiten" + Environment.NewLine, text);
        Assert.Contains("(vom Bewohner angegeben, nicht überprüft)", text);
        Assert.EndsWith("Fehlende Angaben: Herkunft und Familie, Wohnen, Wichtige Lebensereignisse, Wünsche.", text);
        Assert.DoesNotContain("•", text);
    }

    [Fact]
    public void Demo_Biografien()
    {
        var ahorn = s.Biografie(B("Ahorn"));
        Assert.Equal(5, ahorn.Count(a => !a.Leer));
        Assert.Equal(2, ahorn[3].Fassungen.Count);   // eine Änderung mit Verlauf
        Assert.True(ahorn[1].Aktuell!.Angegeben);
        Assert.True(BiografieRegeln.FormulierungFehlt(s.Biografie(B("Distel"))[3].Aktuell!));   // Hinweis in der Demo sichtbar
        Assert.All(s.Biografie(B("Kiefer")), a => Assert.True(a.Leer));
    }

    [Fact]
    public void Nach_dem_Update_kommt_die_Demo_Biografie_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM biografie; DELETE FROM meta WHERE schluessel = 'demo_biografie'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Contains(s.Biografie(B("Ahorn")), a => !a.Leer);
        Assert.False(s.DemoBeimErstenStart());
    }
}
