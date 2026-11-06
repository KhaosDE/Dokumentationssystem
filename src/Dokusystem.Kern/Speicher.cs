using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Der Speicher ist eine SQLite-Datei im Datenordner.
//
// Der Aufbau der Datenbank wächst mit dem Programm. Jede Erweiterung ist ein Schritt in
// Schritte; die Datei merkt sich in „PRAGMA user_version“, wie viele Schritte sie schon hat.
// Beim Öffnen werden fehlende Schritte nachgeholt – jeder ganz oder gar nicht.
//
// Die Python-Version (bis 0.2.0) hatte eine eigene Datei (dokusystem.sqlite3). Sie wird nicht
// gelöscht und nicht gelesen; die C#-Version beginnt mit den Demo-Daten.
public sealed class Speicher : IDisposable
{
    public const string Datei = "dokumentationssystem.db";
    private const string DemoGeladen = "demo_geladen";

    private static readonly string[] Schritte =
    [
        // 1 – Verwaltungsangaben und Bewohnerakte. Zustände mit CHECK, damit nie ein leerer
        //     Wert wie „keine“ aussieht (Regel 5).
        """
        CREATE TABLE meta (
            schluessel TEXT PRIMARY KEY,
            wert       TEXT NOT NULL
        );
        CREATE TABLE bewohner (
            id            INTEGER PRIMARY KEY,
            vorname       TEXT NOT NULL,
            nachname      TEXT NOT NULL,
            zimmer        TEXT NOT NULL,
            geburtsdatum  TEXT NOT NULL,              -- JJJJ-MM-TT
            aufnahmedatum TEXT NOT NULL,              -- JJJJ-MM-TT
            allergie      TEXT NOT NULL DEFAULT 'nicht_erfasst'
                          CHECK (allergie IN ('eingetragen', 'keine_bekannt', 'nicht_erfasst')),
            allergien     TEXT NOT NULL DEFAULT '',
            beschluss     TEXT NOT NULL DEFAULT 'nicht_erfasst'
                          CHECK (beschluss IN ('ende', 'kein_beschluss', 'nicht_erfasst')),
            beschlussende TEXT,                       -- JJJJ-MM-TT, nur bei beschluss = 'ende'
            bild          TEXT                        -- Dateiname im Ordner „bilder“
        );
        """,
    ];

    public static int AktuellerStand => Schritte.Length;

    private const string Spalten =
        "vorname, nachname, zimmer, geburtsdatum, aufnahmedatum, allergie, allergien, beschluss, beschlussende, bild";

    private readonly SqliteConnection db;
    public string Pfad { get; }

    public Speicher(string? pfad = null)
    {
        Pfad = pfad ?? Pfade.ImDatenordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(Pfad)!);
        // Pooling=False: Beim Schließen wird die Datei wirklich freigegeben (wichtig unter Windows).
        db = new SqliteConnection($"Data Source={Pfad};Mode=ReadWriteCreate;Pooling=False;Default Timeout=5");
        try
        {
            db.Open();
            Ausfuehren("PRAGMA foreign_keys = ON");
            Aufbauen();
        }
        catch (SqliteException e)
        {
            db.Dispose();
            throw new SpeicherFehler($"Die Datenbank {Pfad} lässt sich nicht öffnen: {e.Message}", e);
        }
        catch
        {
            db.Dispose();   // sonst bliebe die Datei unter Windows gesperrt
            throw;
        }
    }

    public void Dispose() => db.Dispose();

    // ------------------------------------------------------------------ Aufbau

    public int Stand() => Convert.ToInt32(Wert("PRAGMA user_version"), CultureInfo.InvariantCulture);

    private void Aufbauen()
    {
        int stand = Stand();
        if (stand > AktuellerStand)
            throw new SpeicherFehler("Die Daten stammen aus einer neueren Version des Programms. Bitte die neueste Version verwenden.");
        for (int nummer = stand; nummer < AktuellerStand; nummer++)
        {
            using var schritt = db.BeginTransaction();
            Ausfuehren(Schritte[nummer], schritt);
            Ausfuehren($"PRAGMA user_version = {nummer + 1}", schritt);
            schritt.Commit();
        }
    }

    // ------------------------------------------------------------------ Verwaltungsangaben

    public string? Meta(string schluessel) =>
        Wert("SELECT wert FROM meta WHERE schluessel = $s", ("$s", schluessel)) as string;

    public void SetzeMeta(string schluessel, string wert, SqliteTransaction? transaktion = null) =>
        Ausfuehren("INSERT INTO meta (schluessel, wert) VALUES ($s, $w) " +
                   "ON CONFLICT(schluessel) DO UPDATE SET wert = excluded.wert",
                   transaktion, ("$s", schluessel), ("$w", wert));

    // ------------------------------------------------------------------ Bewohner

    public List<Bewohner> AlleBewohner()
    {
        var liste = Lesen($"SELECT id, {Spalten} FROM bewohner");
        liste.Sort(BewohnerRegeln.NachName);
        return liste;
    }

    public Bewohner? Bewohner(long id) =>
        Lesen($"SELECT id, {Spalten} FROM bewohner WHERE id = $id", ("$id", id)).FirstOrDefault();

    // Legt an (ohne Id) oder ändert (mit Id). Gibt den gespeicherten Bewohner zurück.
    // neuesBild wird in den Datenordner kopiert; das bisherige Bild wird danach gelöscht.
    public Bewohner BewohnerSpeichern(Bewohner b, string? neuesBild = null, bool bildEntfernen = false)
    {
        var fehler = BewohnerRegeln.Pruefen(b);
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        Bewohner? vorher = b.Id is long id ? Bewohner(id) : null;
        if (b.Id is not null && vorher is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);

        string? bild = vorher is not null ? vorher.Bild : b.Bild;
        if (neuesBild is not null) bild = Bilder.Uebernehmen(neuesBild);
        else if (bildEntfernen) bild = null;
        b = b with { Bild = bild };

        try
        {
            using var transaktion = db.BeginTransaction();
            if (b.Id is null)
            {
                Ausfuehren($"INSERT INTO bewohner ({Spalten}) VALUES ({Platzhalter})", transaktion, Werte(b));
                b = b with { Id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture) };
            }
            else
            {
                string setzen = string.Join(", ", Spalten.Split(", ").Select(s => $"{s} = ${s}"));
                Ausfuehren($"UPDATE bewohner SET {setzen} WHERE id = $id", transaktion, [.. Werte(b), ("$id", b.Id)]);
            }
            transaktion.Commit();
        }
        catch (SqliteException)
        {
            if (neuesBild is not null) Bilder.Loeschen(bild);
            throw;
        }
        if (vorher?.Bild is string altesBild && altesBild != b.Bild) Bilder.Loeschen(altesBild);
        return b;
    }

    // ------------------------------------------------------------------ Demo-Daten

    // Lädt die Demo-Daten, wenn das noch nie geschehen ist. true = gerade geladen.
    public bool DemoBeimErstenStart()
    {
        if (Meta(DemoGeladen) is not null) return false;
        DemoZuruecksetzen();
        return true;
    }

    // Alle Bewohner und ihre Bilder weg, Demo-Daten neu laden – alles oder nichts.
    public void DemoZuruecksetzen()
    {
        var demo = Demodaten.Bewohner();   // erst lesen: Ist etwas kaputt, bleibt alles, wie es war
        var alteBilder = AlleBewohner().Select(b => b.Bild).Where(b => b is not null).ToList();
        using (var transaktion = db.BeginTransaction())
        {
            Ausfuehren("DELETE FROM bewohner", transaktion);
            foreach (var b in demo)
                Ausfuehren($"INSERT INTO bewohner ({Spalten}) VALUES ({Platzhalter})", transaktion, Werte(b));
            SetzeMeta(DemoGeladen, "1", transaktion);
            transaktion.Commit();
        }
        foreach (var bild in alteBilder) Bilder.Loeschen(bild);
    }

    // ------------------------------------------------------------------ Hilfen

    private static string Platzhalter => string.Join(", ", Spalten.Split(", ").Select(s => "$" + s));

    private static (string, object?)[] Werte(Bewohner b) =>
    [
        ("$vorname", b.Vorname),
        ("$nachname", b.Nachname),
        ("$zimmer", b.Zimmer),
        ("$geburtsdatum", Iso(b.Geburtsdatum)),
        ("$aufnahmedatum", Iso(b.Aufnahmedatum)),
        ("$allergie", BewohnerRegeln.Kuerzel(b.Allergie)),
        ("$allergien", b.Allergien),
        ("$beschluss", BewohnerRegeln.Kuerzel(b.Beschluss)),
        ("$beschlussende", b.Beschlussende is DateOnly e ? Iso(e) : null),
        ("$bild", b.Bild),
    ];

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly AusIso(string text) => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private List<Bewohner> Lesen(string sql, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, null, parameter);
        using var leser = befehl.ExecuteReader();
        var liste = new List<Bewohner>();
        while (leser.Read())
        {
            liste.Add(new Bewohner
            {
                Id = leser.GetInt64(0),
                Vorname = leser.GetString(1),
                Nachname = leser.GetString(2),
                Zimmer = leser.GetString(3),
                Geburtsdatum = AusIso(leser.GetString(4)),
                Aufnahmedatum = AusIso(leser.GetString(5)),
                // Die Datenbank lässt nur gültige Kürzel zu (CHECK); im Zweifel „nicht erfasst“.
                Allergie = BewohnerRegeln.AllergieAus(leser.GetString(6)) ?? AllergieZustand.NichtErfasst,
                Allergien = leser.GetString(7),
                Beschluss = BewohnerRegeln.BeschlussAus(leser.GetString(8)) ?? BeschlussZustand.NichtErfasst,
                Beschlussende = leser.IsDBNull(9) ? null : AusIso(leser.GetString(9)),
                Bild = leser.IsDBNull(10) ? null : leser.GetString(10),
            });
        }
        return liste;
    }

    private SqliteCommand Befehl(string sql, SqliteTransaction? transaktion, (string, object?)[] parameter)
    {
        var befehl = db.CreateCommand();
        befehl.CommandText = sql;
        befehl.Transaction = transaktion;
        foreach (var (name, wert) in parameter)
            befehl.Parameters.AddWithValue(name, wert ?? DBNull.Value);
        return befehl;
    }

    private void Ausfuehren(string sql, SqliteTransaction? transaktion = null, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, transaktion, parameter);
        befehl.ExecuteNonQuery();
    }

    private object? Wert(string sql, params (string, object?)[] parameter) => WertIn(null, sql, parameter);

    private object? WertIn(SqliteTransaction? transaktion, string sql, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, transaktion, parameter);
        return befehl.ExecuteScalar();
    }

    // Nur für Tests: direkter Zugriff auf die Verbindung (z. B. um die CHECK-Regeln zu prüfen).
    internal SqliteConnection Verbindung => db;
}

// Die Datenbank lässt sich nicht verwenden. Der Text ist für Menschen gedacht.
public sealed class SpeicherFehler(string meldung, Exception? ursache = null) : Exception(meldung, ursache);
