using System.Text.Json;
using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Bewohner speichern, Bilder im Datenordner, Demo-Daten laden und zurücksetzen.
[Collection("Datenordner")]
public sealed class SpeicherBewohnerTests : IDisposable
{
    private readonly Datenordner daten = new();
    private readonly Hilfsordner hilfe = new();
    private Speicher s = new();

    public void Dispose()
    {
        s.Dispose();
        hilfe.Dispose();
        daten.Dispose();
    }

    private static Bewohner Neuer(Action<BewohnerEingabe>? aendern = null)
    {
        var e = new BewohnerEingabe
        {
            Vorname = "Test", Nachname = "Tanne", Zimmer = "201",
            Geburtsdatum = "01.01.1970", Aufnahmedatum = "01.01.2020",
        };
        aendern?.Invoke(e);
        return BewohnerRegeln.AusEingabe(e);
    }

    private string Bilddatei(string name = "foto.png") =>
        hilfe.Datei(name, "\u0089PNG kein echtes Bild, reicht hier"u8.ToArray());

    private void SqlAusfuehren(string sql)
    {
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = sql;
        befehl.ExecuteNonQuery();
    }

    [Fact]
    public void Anlegen_lesen_aendern()
    {
        var b = s.BewohnerSpeichern(Neuer(e =>
        {
            e.Allergie = "eingetragen"; e.Allergien = "Erdnüsse"; e.Beschluss = "ende"; e.Beschlussende = "31.12.2027";
        }));
        Assert.NotNull(b.Id);
        var gelesen = s.Bewohner(b.Id!.Value);
        Assert.Equal(b, gelesen);
        Assert.Equal(new DateOnly(2027, 12, 31), gelesen!.Beschlussende);

        var eingabe = BewohnerRegeln.AlsEingabe(gelesen);
        eingabe.Zimmer = "202";
        eingabe.Allergie = "keine_bekannt";
        var geaendert = s.BewohnerSpeichern(BewohnerRegeln.AusEingabe(eingabe, id: b.Id));
        Assert.Equal("202", s.Bewohner(b.Id.Value)!.Zimmer);
        Assert.Equal("", s.Bewohner(b.Id.Value)!.Allergien);
        Assert.Equal(b.Id, geaendert.Id);
        Assert.Single(s.AlleBewohner());
    }

    [Fact]
    public void Ungueltiges_wird_nicht_gespeichert()
    {
        var b = Neuer() with { Aufnahmedatum = new DateOnly(1960, 1, 1) };
        Assert.Throws<Eingabefehler>(() => s.BewohnerSpeichern(b));
        Assert.Empty(s.AlleBewohner());
    }

    [Fact]
    public void Datenbank_kennt_nur_die_drei_Zustaende()
    {
        Assert.Throws<SqliteException>(() => SqlAusfuehren(
            "INSERT INTO bewohner (vorname, nachname, zimmer, geburtsdatum, aufnahmedatum, allergie) " +
            "VALUES ('a', 'b', '1', '1970-01-01', '2020-01-01', '')"));
        SqlAusfuehren(
            "INSERT INTO bewohner (vorname, nachname, zimmer, geburtsdatum, aufnahmedatum) " +
            "VALUES ('a', 'b', '1', '1970-01-01', '2020-01-01')");
        var b = Assert.Single(s.AlleBewohner());
        Assert.Equal((AllergieZustand.NichtErfasst, BeschlussZustand.NichtErfasst), (b.Allergie, b.Beschluss));
        Assert.Equal(OffenGefuehrtZustand.NichtErfasst, b.OffenGefuehrt);   // auch nach dem Update für vorhandene Bewohner
        Assert.Throws<SqliteException>(() => SqlAusfuehren(
            "INSERT INTO bewohner (vorname, nachname, zimmer, geburtsdatum, aufnahmedatum, offen_gefuehrt) " +
            "VALUES ('a', 'b', '1', '1970-01-01', '2020-01-01', 'vielleicht')"));
        var offen = s.BewohnerSpeichern(b with { OffenGefuehrt = OffenGefuehrtZustand.Ja });
        Assert.Equal(OffenGefuehrtZustand.Ja, s.Bewohner(offen.Id!.Value)!.OffenGefuehrt);
    }

    [Fact]
    public void Sortiert_nach_Nachname()
    {
        foreach (string nachname in new[] { "Zeder", "ahorn", "Mohn" })
            s.BewohnerSpeichern(Neuer(e => e.Nachname = nachname));
        Assert.Equal(["ahorn", "Mohn", "Zeder"], s.AlleBewohner().Select(b => b.Nachname));
    }

    [Fact]
    public void Bild_hinterlegen_ersetzen_entfernen()
    {
        var b = s.BewohnerSpeichern(Neuer(), neuesBild: Bilddatei());
        string erstes = Bilder.Pfad(b.Bild)!;
        Assert.NotNull(erstes);
        Assert.Equal(Pfade.ImDatenordner("bilder"), Path.GetDirectoryName(erstes));
        b = s.BewohnerSpeichern(b, neuesBild: Bilddatei("zwei.JPG"));
        Assert.EndsWith(".jpg", b.Bild);
        Assert.False(File.Exists(erstes));   // altes Bild ist weg
        string zweites = Bilder.Pfad(b.Bild)!;
        s.BewohnerSpeichern(b);   // ohne Bildangabe: Bild bleibt
        Assert.True(File.Exists(zweites));
        b = s.BewohnerSpeichern(b, bildEntfernen: true);
        Assert.Null(b.Bild);
        Assert.False(File.Exists(zweites));
    }

    [Fact]
    public void Nur_Bildendungen_und_kein_Ausbruch()
    {
        Assert.Throws<ArgumentException>(() => Bilder.Uebernehmen(Bilddatei("text.txt")));
        Assert.Null(Bilder.Pfad("../../etwas.png"));
        Assert.Null(Bilder.Pfad(null));
    }

    [Fact]
    public void Demo_Datei_ist_gueltig_und_gemischt()
    {
        var alle = Demodaten.Bewohner();
        Assert.True(alle.Count >= 10);
        Assert.Equal(Enum.GetValues<AllergieZustand>().ToHashSet(), alle.Select(b => b.Allergie).ToHashSet());
        Assert.Equal(Enum.GetValues<BeschlussZustand>().ToHashSet(), alle.Select(b => b.Beschluss).ToHashSet());
        Assert.Equal(Enum.GetValues<OffenGefuehrtZustand>().ToHashSet(), alle.Select(b => b.OffenGefuehrt).ToHashSet());
        Assert.All(alle, b => Assert.Null(b.Bild));   // Regel 2: nur Platzhalter
        Assert.Equal(alle.Count, alle.Select(b => (b.Zimmer, b.Bett)).Distinct().Count());   // je Bett ein Bewohner (Schritt 26e)
        Assert.All(alle, b => Assert.NotEqual("", b.Bett));
    }

    [Fact]
    public void Demo_Datei_ohne_Bilder()
    {
        string ordner = Path.Combine(Repo.Wurzel, "daten", "demo");
        Assert.All(Directory.GetFiles(ordner), d => Assert.Equal(".json", Path.GetExtension(d)));   // keine Bilder (Regel 2)
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(ordner, "bewohner.json")));
        Assert.All(json.RootElement.GetProperty("bewohner").EnumerateArray(), e => Assert.False(e.TryGetProperty("bild", out _)));
    }

    [Fact]
    public void Erster_Start_und_Zuruecksetzen()
    {
        Assert.True(s.DemoBeimErstenStart());
        int anzahl = s.AlleBewohner().Count;
        Assert.True(anzahl >= 10);
        Assert.False(s.DemoBeimErstenStart());   // nur einmal
        var b = s.BewohnerSpeichern(Neuer(), neuesBild: Bilddatei());
        s.BewohnerSpeichern(b, neuesBild: Bilddatei());
        string fremdesBild = Pfade.ImDatenordner("bilder", "nicht-von-uns.png");
        File.WriteAllText(fremdesBild, "z. B. aus der Python-Version");
        s.DemoZuruecksetzen();
        Assert.Equal(anzahl, s.AlleBewohner().Count);
        Assert.Equal([fremdesBild], Directory.GetFiles(Pfade.ImDatenordner("bilder")));   // nur eigene Bilder gelöscht
    }

    [Fact]
    public void Alles_geloescht_bleibt_geloescht()
    {
        s.DemoBeimErstenStart();
        SqlAusfuehren("DELETE FROM bewohner");
        s.Dispose();
        s = new Speicher();
        Assert.False(s.DemoBeimErstenStart());
        Assert.Empty(s.AlleBewohner());
    }
}
