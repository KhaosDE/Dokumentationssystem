using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Medikamentendatenbank: Demo-Bestand mit Quellen, Suche, anlegen, ändern als neue Fassung.
[Collection("Datenordner")]
public sealed class MedikamenteTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public MedikamenteTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static MedikamentFassung Fassung(Action<MedikamentEingabe>? aendern = null)
    {
        var e = new MedikamentEingabe { Wirkstoff = "Testwirkstoff", Darreichungsform = "Tablette", Staerken = "5 mg, 10 mg", Kuerzel = "AB" };
        aendern?.Invoke(e);
        return MedikamentRegeln.AusEingabe(e, Jetzt);
    }

    [Fact]
    public void Demo_Bestand_mit_Quellen_alles_ungeprueft()
    {
        var alle = s.Medikamente();
        Assert.InRange(alle.Count, 25, 45);
        Assert.All(alle, m =>
        {
            Assert.False(m.SelbstAngelegt);
            Assert.Equal(Pruefstatus.Ungeprueft, m.Aktuell.Pruefstatus);   // nur Tobi setzt „bestätigt“
            Assert.True(MedikamentRegeln.IstAdresse(m.Aktuell.Quelle));   // jede Angabe mit Quelle
            Assert.Equal(Demodaten.DemoKuerzel, m.Aktuell.Kuerzel);
            Assert.Single(m.Fassungen);
        });
        Assert.Equal(alle.Count, alle.Select(m => (m.Wirkstoff, m.Aktuell.Darreichungsform)).Distinct().Count());
        Assert.Equal(alle.Select(m => m.Wirkstoff).Order(StringComparer.CurrentCulture), alle.Select(m => m.Wirkstoff));
    }

    [Fact]
    public void Suche_in_Wirkstoff_Handelsname_und_Form()
    {
        var erste = s.Medikamente()[0];
        Assert.Contains(s.Medikamente(erste.Wirkstoff.ToUpperInvariant()[..4]), m => m.Id == erste.Id);
        Assert.Empty(s.Medikamente("gibt-es-nicht"));
        if (erste.Aktuell.Handelsnamen.Count > 0)
            Assert.Contains(s.Medikamente(erste.Aktuell.Handelsnamen[0]), m => m.Id == erste.Id);
    }

    [Fact]
    public void Anlegen_pruefen_selbst_angelegt()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => MedikamentRegeln.AusEingabe(new MedikamentEingabe { Quelle = "www.beispiel.de", Bestaetigt = true }, Jetzt)).Fehler;
        Assert.Contains("Wirkstoff fehlt.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("Kürzel fehlt"));
        Assert.Contains(fehler, f => f.StartsWith("Quelle: bitte als vollständige Internetadresse"));
        var ohneQuelle = Assert.Throws<Eingabefehler>(() =>
            MedikamentRegeln.AusEingabe(new MedikamentEingabe { Wirkstoff = "x", Kuerzel = "AB", Bestaetigt = true }, Jetzt)).Fehler;
        Assert.Equal(["„Bestätigt“ geht nur mit Quelle."], ohneQuelle);

        var neu = s.MedikamentAnlegen(Fassung());
        Assert.True(neu.SelbstAngelegt);
        Assert.Equal(["5 mg", "10 mg"], neu.Aktuell.Staerken);
        Assert.Equal("Testwirkstoff · Tablette", neu.Anzeige);
        Assert.Throws<Eingabefehler>(() => s.MedikamentAnlegen(Fassung(e => e.Wirkstoff = "testwirkstoff")));   // gibt es schon
        s.MedikamentAnlegen(Fassung(e => e.Darreichungsform = "Tropfen"));                                       // andere Form geht
    }

    [Fact]
    public void Aendern_ist_eine_neue_Fassung_die_alte_bleibt()
    {
        var m = s.MedikamentAnlegen(Fassung());
        var gleich = s.MedikamentAendern(m.Id, Fassung());
        Assert.Single(gleich.Fassungen);   // nichts geändert, keine neue Fassung
        var geaendert = s.MedikamentAendern(m.Id, Fassung(e => { e.Staerken = "5 mg, 10 mg, 20 mg"; e.Kuerzel = "CD"; }));
        Assert.Equal(2, geaendert.Fassungen.Count);
        Assert.Equal(["5 mg", "10 mg"], geaendert.Fassungen[0].Staerken);
        Assert.Equal("CD", geaendert.Aktuell.Kuerzel);
        Assert.Equal(["Stärken: „5 mg, 10 mg“ → „5 mg, 10 mg, 20 mg“"], MedikamentRegeln.Unterschiede(geaendert.Fassungen[0], geaendert.Aktuell));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE medikament_fassungen SET wirkstoff = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Demo_zuruecksetzen_und_Update()
    {
        s.MedikamentAnlegen(Fassung());
        int demo = Demodaten.Medikamente(Jetzt).Count;
        s.DemoZuruecksetzen();
        Assert.Equal(demo, s.Medikamente().Count);
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM medikamente; DELETE FROM meta WHERE schluessel = 'demo_medikamente'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.Equal(demo, s.Medikamente().Count);
        Assert.False(s.DemoBeimErstenStart());
    }
}
