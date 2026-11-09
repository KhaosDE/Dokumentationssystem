using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Ablage der Dokumente: Dateien im Ordner „Dokumente\<Bewohner-Nr>“ neben der Datenbank (auch im gemeinsamen Ordner),
// Angaben und Protokoll in der Datenbank. Zeilen werden nie geändert; was mit einem Dokument geschah, sind Ereignisse.
public sealed partial class Speicher
{
    private const string DemoDokumente = "demo_dokumente";
    public event Action? DokumenteGeaendert;

    public string Ablage(long bewohnerId) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Pfad))!, DokumentRegeln.Ordner, bewohnerId.ToString(CultureInfo.InvariantCulture));

    public List<string> Dokumentkategorien() => [.. Listeneintraege(Liste.Dokumentkategorien).Where(e => e.Aktiv).Select(e => e.Name)];

    // Ohne Papierkorb und ohne ersetzte Fassungen; mit auchEntfernte auch den Papierkorb.
    public List<Dokument> Dokumente(long bewohnerId, bool auchEntfernte = false) =>
        [.. AlleDokumente(bewohnerId).Where(d => !d.Ersetzt && !d.Geloescht && (auchEntfernte || !d.Entfernt))];

    public List<Dokument> AlleDokumente(long bewohnerId)
    {
        var aktionen = new Dictionary<long, List<DokumentAktion>>();
        using (var befehl = Befehl("""
            SELECT e.dokument_id, e.aktion FROM dokument_ereignisse e JOIN dokumente d ON d.id = e.dokument_id
            WHERE d.bewohner_id = $b ORDER BY e.id
            """, null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!aktionen.TryGetValue(leser.GetInt64(0), out var l)) aktionen[leser.GetInt64(0)] = l = [];
                l.Add(Enum.Parse<DokumentAktion>(leser.GetString(1)));
            }
        var liste = new List<Dokument>();
        using (var befehl = Befehl("""
            SELECT id, bewohner_id, kategorie, titel, datum, gueltig_bis, hinweis, datei, original, groesse, pruefsumme, hinzugefuegt, kuerzel, vorgaenger
            FROM dokumente WHERE bewohner_id = $b ORDER BY datum DESC, id DESC
            """, null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                long id = leser.GetInt64(0);
                var a = aktionen.GetValueOrDefault(id) ?? [];
                // Papierkorb: das letzte „Entfernt“ ohne späteres „Wiederhergestellt“.
                int entfernt = a.LastIndexOf(DokumentAktion.Entfernt), zurueck = a.LastIndexOf(DokumentAktion.Wiederhergestellt);
                liste.Add(new Dokument
                {
                    Id = id, BewohnerId = leser.GetInt64(1), Kategorie = leser.GetString(2), Titel = leser.GetString(3), Datum = Tag(leser.GetString(4)),
                    GueltigBis = leser.IsDBNull(5) ? null : Tag(leser.GetString(5)), Hinweis = leser.GetString(6), Datei = leser.GetString(7),
                    Original = leser.GetString(8), Groesse = leser.GetInt64(9), Pruefsumme = leser.GetString(10), Hinzugefuegt = AusZeit(leser.GetString(11)),
                    Kuerzel = leser.GetString(12), Vorgaenger = leser.IsDBNull(13) ? null : leser.GetInt64(13),
                    Entfernt = entfernt > zurueck, Geloescht = a.Contains(DokumentAktion.Geloescht), Ersetzt = a.Contains(DokumentAktion.Ersetzt),
                });
            }
        return liste;
    }

    private static DateOnly Tag(string text) => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string TagText(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public List<DokumentEreignis> DokumentProtokoll(long bewohnerId)
    {
        var liste = new List<DokumentEreignis>();
        using var befehl = Befehl("""
            SELECT e.dokument_id, e.aktion, e.zeit, e.kuerzel, e.grund, d.titel FROM dokument_ereignisse e JOIN dokumente d ON d.id = e.dokument_id
            WHERE d.bewohner_id = $b ORDER BY e.id DESC
            """, null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new DokumentEreignis(leser.GetInt64(0), Enum.Parse<DokumentAktion>(leser.GetString(1)), AusZeit(leser.GetString(2)), leser.GetString(3),
                leser.GetString(4), leser.GetString(5)));
        return liste;
    }

    private void Recht(string kuerzel, string funktion)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        if (Grund(kuerzel, funktion) is string grund) throw new Eingabefehler([grund]);
    }

    // Kopiert die Datei in die Ablage (das Original bleibt) und trägt sie ein; vorgaenger: „Ersetzen“.
    public Dokument DokumentHinzufuegen(long bewohnerId, string quelle, DokumentEingabe e, string kuerzel, DateTime jetzt, long? vorgaenger = null)
    {
        Recht(kuerzel, Funktionen.DokumenteHinzufuegen);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (DokumentRegeln.Pruefen(e, Dokumentkategorien()) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        var info = new FileInfo(quelle);
        if (!info.Exists) throw new Eingabefehler(["Die Datei gibt es nicht (mehr)."]);
        if (info.Length > DokumentRegeln.Hoechstgroesse) throw new Eingabefehler(["Die Datei ist größer als 50 MB."]);
        string ordner = Ablage(bewohnerId);
        Directory.CreateDirectory(ordner);
        string name = DokumentRegeln.AblageName(Guid.NewGuid().ToString("N")[..12], Path.GetFileName(quelle));
        string ziel = Path.Combine(ordner, name);
        File.Copy(quelle, ziel, overwrite: false);
        long id;
        try
        {
            string summe;
            using (var strom = File.OpenRead(ziel)) summe = Convert.ToHexString(SHA256.HashData(strom));
            using var transaktion = db.BeginTransaction();
            id = DokumentEinfuegen(bewohnerId, e, name, Path.GetFileName(quelle), info.Length, summe, kuerzel.Trim(), jetzt, vorgaenger, transaktion);
            if (vorgaenger is long alt) Ereignis(alt, DokumentAktion.Ersetzt, jetzt, kuerzel, $"durch Nr. {id}", transaktion);
            transaktion.Commit();
        }
        catch
        {
            File.Delete(ziel);   // ohne Eintrag keine Datei in der Ablage
            throw;
        }
        DokumenteGeaendert?.Invoke();
        return AlleDokumente(bewohnerId).Single(d => d.Id == id);
    }

    public Dokument DokumentErsetzen(long id, string quelle, DokumentEingabe e, string kuerzel, DateTime jetzt)
    {
        var alt = DokumentNach(id) ?? throw new Eingabefehler(["Dieses Dokument gibt es nicht mehr."]);
        return DokumentHinzufuegen(alt.BewohnerId, quelle, e, kuerzel, jetzt, vorgaenger: id);
    }

    public Dokument? DokumentNach(long id)
    {
        if (Wert("SELECT bewohner_id FROM dokumente WHERE id = $id", ("$id", id)) is not long bewohner) return null;
        return AlleDokumente(bewohner).FirstOrDefault(d => d.Id == id);
    }

    // Der Pfad zum Öffnen (im Standardprogramm des Rechners); das Öffnen steht im Protokoll. Stimmt die Prüfsumme nicht,
    // wurde die Datei außerhalb von DokuLine verändert – dann ein Fehler statt still Falsches zu zeigen.
    public string DokumentOeffnen(long id, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.DokumenteAnsehen);
        var d = DokumentNach(id) ?? throw new Eingabefehler(["Dieses Dokument gibt es nicht mehr."]);
        if (d.Geloescht) throw new Eingabefehler(["Dieses Dokument wurde endgültig gelöscht."]);
        string pfad = Path.Combine(Ablage(d.BewohnerId), d.Datei);
        if (!File.Exists(pfad)) throw new Eingabefehler([$"Die Datei fehlt in der Ablage ({d.Datei})."]);
        using (var strom = File.OpenRead(pfad))
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(strom)), d.Pruefsumme, StringComparison.OrdinalIgnoreCase))
                throw new Eingabefehler(["Die Datei wurde außerhalb von DokuLine verändert (Prüfsumme stimmt nicht). Bitte die Leitung informieren."]);
        Ereignis(id, DokumentAktion.Geoeffnet, jetzt, kuerzel, "", null);
        return pfad;
    }

    public void DokumentEntfernen(long id, string grund, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.DokumenteHinzufuegen);
        if (grund.Trim().Length == 0) throw new Eingabefehler(["Bitte einen Grund angeben."]);
        var d = DokumentNach(id) ?? throw new Eingabefehler(["Dieses Dokument gibt es nicht mehr."]);
        if (d.Entfernt) return;
        Ereignis(id, DokumentAktion.Entfernt, jetzt, kuerzel, grund.Trim(), null);
        DokumenteGeaendert?.Invoke();
    }

    public void DokumentWiederherstellen(long id, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.DokumenteHinzufuegen);
        var d = DokumentNach(id) ?? throw new Eingabefehler(["Dieses Dokument gibt es nicht mehr."]);
        if (!d.Entfernt || d.Geloescht) return;
        Ereignis(id, DokumentAktion.Wiederhergestellt, jetzt, kuerzel, "", null);
        DokumenteGeaendert?.Invoke();
    }

    // Nur aus dem Papierkorb und nur mit dem Recht „Dokumente endgültig löschen“: die Datei ist danach weg, die Angaben
    // und das Protokoll bleiben.
    public void DokumentEndgueltigLoeschen(long id, string grund, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.DokumenteLoeschen);
        var d = DokumentNach(id) ?? throw new Eingabefehler(["Dieses Dokument gibt es nicht mehr."]);
        if (!d.Entfernt) throw new Eingabefehler(["Endgültig löschen geht nur aus dem Papierkorb."]);
        string pfad = Path.Combine(Ablage(d.BewohnerId), d.Datei);
        if (File.Exists(pfad)) File.Delete(pfad);
        Ereignis(id, DokumentAktion.Geloescht, jetzt, kuerzel, grund.Trim(), null);
        DokumenteGeaendert?.Invoke();
    }

    private long DokumentEinfuegen(long bewohnerId, DokumentEingabe e, string datei, string original, long groesse, string summe, string kuerzel,
        DateTime jetzt, long? vorgaenger, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO dokumente (bewohner_id, kategorie, titel, datum, gueltig_bis, hinweis, datei, original, groesse, pruefsumme, hinzugefuegt, kuerzel, vorgaenger)
            VALUES ($b, $k, $t, $d, $g, $h, $n, $o, $s, $p, $z, $u, $v)
            """, transaktion, ("$b", bewohnerId), ("$k", e.Kategorie), ("$t", e.Titel.Trim()), ("$d", TagText(e.Datum)),
            ("$g", e.GueltigBis is DateOnly g ? TagText(g) : null), ("$h", e.Hinweis.Trim()), ("$n", datei), ("$o", original), ("$s", groesse),
            ("$p", summe), ("$z", Zeit(jetzt)), ("$u", kuerzel), ("$v", vorgaenger));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        Ereignis(id, DokumentAktion.Hinzugefuegt, jetzt, kuerzel, "", transaktion);
        return id;
    }

    private void Ereignis(long id, DokumentAktion aktion, DateTime jetzt, string kuerzel, string grund, SqliteTransaction? transaktion) =>
        Ausfuehren("INSERT INTO dokument_ereignisse (dokument_id, aktion, zeit, kuerzel, grund) VALUES ($d, $a, $z, $k, $g)", transaktion,
            ("$d", id), ("$a", aktion.ToString()), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()), ("$g", grund));

    // Demo: ausgedachte PDFs (Betreuerausweis, Kostenzusage, Arztbrief) bei den ersten Bewohnern jedes Bereichs.
    private void DemoDokumenteEinfuegen(SqliteTransaction transaktion)
    {
        string wurzel = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Pfad))!, DokumentRegeln.Ordner);
        Ausfuehren("DELETE FROM dokumente", transaktion);   // Ereignisse folgen (CASCADE)
        if (Directory.Exists(wurzel)) Directory.Delete(wurzel, recursive: true);   // nur die Ablage dieser Datenbank
        var jetzt = Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        foreach (var (b, kategorie, titel, datum, bis, zeilen) in Demodaten.Dokumente(bewohner, heute))
        {
            string ordner = Path.Combine(wurzel, b.Id!.Value.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(ordner);
            var inhalt = DemoPdf.Erzeugen(titel, zeilen);
            string name = DokumentRegeln.AblageName(Guid.NewGuid().ToString("N")[..12], $"{titel}.pdf");
            File.WriteAllBytes(Path.Combine(ordner, name), inhalt);
            DokumentEinfuegen(b.Id!.Value, new DokumentEingabe { Kategorie = kategorie, Titel = titel, Datum = datum, GueltigBis = bis }, name, $"{titel}.pdf",
                inhalt.Length, Convert.ToHexString(SHA256.HashData(inhalt)), "EF", jetzt.AddDays(-20), null, transaktion);
        }
        SetzeMeta(DemoDokumente, "1", transaktion);
    }
}
