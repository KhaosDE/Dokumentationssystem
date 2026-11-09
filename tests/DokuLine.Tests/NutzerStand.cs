using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Für die Update-Sicherheitstests (0.33.1), in beiden Testprojekten benutzt (die Oberflächentests binden
// diese Datei ein): eine Datenbank so verändern, wie es ein Nutzer mit den Mitteln des Programms (oder im
// Mehrplatzbetrieb ein anderer Arbeitsplatz) getan haben kann, und die Merker der einmaligen Schritte
// entfernen, als käme die Datenbank aus einer älteren Version.
public static class NutzerStand
{
    // Diese Merker laden alle Demo-Daten neu („Demo-Daten zurücksetzen“); sie werden einzeln geprüft.
    public static readonly string[] NeuLaden = ["demo_geladen", "demo_zimmer", "demo_23"];

    public static void Veraendern(Speicher s)
    {
        foreach (var p in s.Positionsliste().Where(p => p.Name != RechteRegeln.Administrator).ToList())
            s.ListeneintragUmbenennen(Liste.Positionen, p.Name, Anders(p.Name));
        foreach (var b in s.Bereichsliste().ToList())
            s.ListeneintragUmbenennen(Liste.Bereiche, b.Name, Anders(b.Name));
        s.ListeneintragAnlegen(Liste.Positionen, "Eigene Position");
        s.ListeneintragAnlegen(Liste.Bereiche, "Eigener Bereich");
        foreach (var kuerzel in new[] { "AB", "KL" })
            if (s.MitarbeiterLesen(kuerzel) is Mitarbeiter m)
                s.MitarbeiterSpeichern(m with { Aktiv = false }, neu: false);
        Sql(s.Verbindung, "DELETE FROM bewohner WHERE id IN (SELECT id FROM bewohner ORDER BY id LIMIT 3)");
    }

    // Namen höchstens 30 Zeichen.
    private static string Anders(string name) => (name.Length > 26 ? name[..26] : name) + " neu";

    // Alle Merker; ausgenommen der Schema-Stand der Module (das sind keine einmaligen Schritte).
    public static List<(string Schluessel, string Wert)> Merker(SqliteConnection db)
    {
        var liste = new List<(string, string)>();
        using var befehl = db.CreateCommand();
        befehl.CommandText = "SELECT schluessel, wert FROM meta WHERE schluessel NOT LIKE 'modul\\_%\\_stand' ESCAPE '\\' ORDER BY schluessel";
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add((leser.GetString(0), leser.GetString(1)));
        return liste;
    }

    public static void MerkerEntfernen(SqliteConnection db, IEnumerable<string> schluessel)
    {
        foreach (var k in schluessel)
        {
            using var befehl = db.CreateCommand();
            befehl.CommandText = "DELETE FROM meta WHERE schluessel = $s";
            befehl.Parameters.AddWithValue("$s", k);
            befehl.ExecuteNonQuery();
        }
    }

    public static void Sql(SqliteConnection db, string text)
    {
        using var befehl = db.CreateCommand();
        befehl.CommandText = text;
        befehl.ExecuteNonQuery();
    }
}
