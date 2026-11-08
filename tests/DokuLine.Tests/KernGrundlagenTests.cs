using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Datenordner, Einstellungen und SQLite-Speicher.
[Collection("Datenordner")]
public class KernGrundlagenTests
{
    [Fact]
    public void Umlenkung_und_Schutz_vor_Ausbruch()
    {
        using var daten = new Datenordner();
        Assert.Equal(Path.GetFullPath(daten.Ordner), Pfade.Datenordner);
        Assert.Equal("bilder", Path.GetFileName(Path.GetDirectoryName(Pfade.ImDatenordner("bilder", "a.png"))));
        Assert.Throws<ArgumentException>(() => Pfade.ImDatenordner("..", "anderswo.txt"));
    }

    // Schritt 26a1: Der Datenordner heißt DokuLine; der Inhalt des alten Ordners wird einmal kopiert.
    [Fact]
    public void Alter_Datenordner_wird_kopiert_und_bleibt()
    {
        using var hilfe = new Hilfsordner();
        string alt = Path.Combine(hilfe.Ordner, "Dokumentationssystem"), neu = Path.Combine(hilfe.Ordner, "DokuLine");
        Directory.CreateDirectory(Path.Combine(alt, "bilder", "bewohner"));
        File.WriteAllText(Path.Combine(alt, "dokusystem.db"), "Datenbank");
        File.WriteAllText(Path.Combine(alt, "einstellungen.json"), """{"kuerzel": "EF"}""");
        File.WriteAllText(Path.Combine(alt, "bilder", "bewohner", "a.png"), "Bild");
        Directory.CreateDirectory(Path.Combine(alt, "leer"));

        string? meldung = Pfade.AltenOrdnerUebernehmen(alt, neu);
        Assert.Contains("3 Dateien", meldung);
        Assert.Equal("Datenbank", File.ReadAllText(Path.Combine(neu, "dokusystem.db")));
        Assert.Equal("Bild", File.ReadAllText(Path.Combine(neu, "bilder", "bewohner", "a.png")));
        Assert.True(Directory.Exists(Path.Combine(neu, "leer")));
        Assert.True(File.Exists(Path.Combine(alt, "dokusystem.db")));   // alter Ordner unangetastet
        Assert.False(Directory.Exists(neu + ".kopie"));

        // Gibt es den neuen Ordner schon, passiert nichts (auch nicht beim nächsten Start).
        File.WriteAllText(Path.Combine(alt, "dokusystem.db"), "geändert");
        Assert.Null(Pfade.AltenOrdnerUebernehmen(alt, neu));
        Assert.Equal("Datenbank", File.ReadAllText(Path.Combine(neu, "dokusystem.db")));
        // Ohne alten Ordner ebenso nichts.
        Assert.Null(Pfade.AltenOrdnerUebernehmen(Path.Combine(hilfe.Ordner, "gibtsnicht"), Path.Combine(hilfe.Ordner, "neu2")));
        Assert.False(Directory.Exists(Path.Combine(hilfe.Ordner, "neu2")));
        // Umgelenkt (Tests, Selbsttest): kein alter Ordner.
        using var daten = new Datenordner();
        Assert.Null(Pfade.AlterDatenordner);
        Assert.Null(Pfade.AltenOrdnerUebernehmen());
    }

    [Fact]
    public void Demo_Datei_liegt_im_Repository()
    {
        Assert.True(File.Exists(Path.Combine(Repo.Wurzel, "daten", "demo", "bewohner.json")));
    }

    [Fact]
    public void Einstellungen_Vorgabe_Speichern_und_kaputte_Datei()
    {
        using var daten = new Datenordner();
        Assert.True(Einstellungen.Laden().AutoUpdate);
        Einstellungen.Speichern(new EinstellungsWerte(AutoUpdate: false));
        Assert.False(Einstellungen.Laden().AutoUpdate);
        File.WriteAllText(Pfade.ImDatenordner(Einstellungen.Datei), "{kaputt");
        Assert.True(Einstellungen.Laden().AutoUpdate);
        File.WriteAllText(Pfade.ImDatenordner(Einstellungen.Datei), """{"auto_update": "nein"}""");
        Assert.True(Einstellungen.Laden().AutoUpdate);
    }

    [Fact]
    public void Einstellungsdatei_der_Python_Version_wird_weiter_gelesen()
    {
        using var daten = new Datenordner();
        File.WriteAllText(Pfade.ImDatenordner(Einstellungen.Datei), "{\n  \"auto_update\": false\n}\n");
        Assert.False(Einstellungen.Laden().AutoUpdate);
    }

    [Fact]
    public void Kuerzel_speichern_und_pruefen()
    {
        using var daten = new Datenordner();
        Assert.Equal("", Einstellungen.Laden().Kuerzel);
        Einstellungen.Aendern(w => w with { Kuerzel = " AB1 " });
        Assert.Equal("AB1", Einstellungen.Laden().Kuerzel);
        Assert.True(Einstellungen.Laden().AutoUpdate);   // andere Werte bleiben
        Assert.Throws<Eingabefehler>(() => Einstellungen.Aendern(w => w with { Kuerzel = "A.B" }));
        Assert.Throws<Eingabefehler>(() => Einstellungen.Aendern(w => w with { Kuerzel = "ABCDEFG" }));
        Assert.Equal("AB1", Einstellungen.Laden().Kuerzel);
        Assert.Null(Einstellungen.KuerzelPruefen(""));
        Assert.Null(Einstellungen.KuerzelPruefen("Tö2"));
    }

    [Fact]
    public void Logo_liegt_im_Datenordner_und_wird_ersetzt()
    {
        using var daten = new Datenordner();
        using var hilfe = new Hilfsordner();
        Assert.Null(Einstellungen.LogoPfad());
        Einstellungen.LogoSetzen(hilfe.Datei("eins.png", [1, 2, 3]));
        string erstes = Einstellungen.LogoPfad()!;
        Assert.Equal(Pfade.ImDatenordner(Einstellungen.LogoOrdner), Path.GetDirectoryName(erstes));
        Einstellungen.LogoSetzen(hilfe.Datei("zwei.jpg", [4, 5]));
        Assert.False(File.Exists(erstes));
        Assert.EndsWith(".jpg", Einstellungen.LogoPfad());
        Einstellungen.LogoSetzen(null);
        Assert.Null(Einstellungen.LogoPfad());
        Assert.Empty(Directory.GetFiles(Pfade.ImDatenordner(Einstellungen.LogoOrdner)));
    }

    [Fact]
    public void Speicher_legt_Datei_im_Datenordner_an_und_baut_auf()
    {
        using var daten = new Datenordner();
        using (var s = new Speicher())
        {
            Assert.Equal(Path.GetFullPath(daten.Ordner), Path.GetDirectoryName(s.Pfad));
            Assert.Equal(Speicher.AktuellerStand, s.Stand());
            s.SetzeMeta("probe", "1");
            s.SetzeMeta("probe", "2");
        }
        using (var s = new Speicher())   // zweites Öffnen: nichts doppelt, Daten noch da
        {
            Assert.Equal("2", s.Meta("probe"));
            Assert.Null(s.Meta("gibt-es-nicht"));
        }
    }

    [Fact]
    public void Daten_aus_neuerer_Version_werden_nicht_angefasst()
    {
        using var daten = new Datenordner();
        string pfad;
        using (var s = new Speicher()) pfad = s.Pfad;
        using (var db = new SqliteConnection($"Data Source={pfad};Pooling=False"))
        {
            db.Open();
            using var befehl = db.CreateCommand();
            befehl.CommandText = $"PRAGMA user_version = {Speicher.AktuellerStand + 1}";
            befehl.ExecuteNonQuery();
        }
        Assert.Throws<SpeicherFehler>(() => new Speicher());
    }

    [Fact]
    public void Kaputte_Datei_gibt_verstaendlichen_Fehler()
    {
        using var daten = new Datenordner();
        File.WriteAllBytes(Pfade.ImDatenordner(Speicher.Datei), System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("das ist keine Datenbank", 100))));
        Assert.Throws<SpeicherFehler>(() => new Speicher());
    }

    [Fact]
    public void Alte_Datenbank_der_Python_Version_bleibt_unberuehrt()
    {
        using var daten = new Datenordner();
        string alt = Pfade.ImDatenordner("dokusystem.sqlite3");
        File.WriteAllText(alt, "alte Daten");
        using (var s = new Speicher()) s.DemoBeimErstenStart();
        Assert.Equal("alte Daten", File.ReadAllText(alt));
    }
}
