using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Mehrplatzbetrieb (Demo-Stufe): mehrere Rechner arbeiten mit derselben Datenbank (lokal oder in
// einem gemeinsamen Ordner). Kein Server, keine Anmeldung mit Passwort – das gehört zum Echtbetrieb.

// Ein anderer Arbeitsplatz hat denselben Datensatz inzwischen gespeichert. Es gewinnt nicht still
// der Letzte: Der Aufrufer zeigt beide Fassungen und lässt wählen.
public sealed class Konflikt(string meldung, string andere, string meine) : Exception(meldung)
{
    public string Andere { get; } = andere;   // inzwischen gespeichert
    public string Meine { get; } = meine;     // noch nicht gespeichert

    public const string Text = "Ein anderer Arbeitsplatz hat das inzwischen geändert.";
}

// Bereichskennung je Installation und Wohnbereich je Bewohner (allgemeine Kürzel, z. B. „R1“).
public static class Bereiche
{
    public const string Vorgabe = "R1";
    public const int Laenge = 30;

    // Vorschläge in den Einstellungen; eigene Kennungen sind möglich.
    public static readonly string[] Vorschlaege = ["R1", "R2", "R3", "Therapie", "Leitung"];

    // Die alten Demo-Bereiche heißen jetzt R1 und R2 (Bereichskennung dieses Rechners).
    public static string Umbenannt(string bereich) => bereich switch { "Wohnbereich A" => "R1", "Wohnbereich B" => "R2", _ => bereich };

    public static string Bereinigen(string? text) => string.IsNullOrWhiteSpace(text) ? Vorgabe : string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string? Pruefen(string? text)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 0) return "Bereich fehlt.";
        if (t.Length > Laenge) return $"Bereich: höchstens {Laenge} Zeichen.";
        if (t.Any(char.IsControl)) return "Bereich: bitte nur eine Zeile.";
        return null;
    }
}

// Statusmeldung zu einem Bewohner: kurzer Text mit Bereich, Kürzel und Zeit, sichtbar in allen
// Bereichen, bis sie abläuft oder ersetzt wird. Leerer Text = aufgehoben. Der Verlauf bleibt.
public sealed record Statusmeldung(long Id, long BewohnerId, string Text, string Bereich, string Kuerzel, DateTime Erstellt, DateTime GiltBis)
{
    public bool Gilt(DateTime jetzt) => Text.Length > 0 && GiltBis > jetzt;
}

public static class StatusmeldungRegeln
{
    public const int Laenge = 80;
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static (string Text, DateTime GiltBis) AusEingabe(string text, string datum, string uhrzeit, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        text = (text ?? "").Trim().ReplaceLineEndings(" ");
        if (text.Length == 0) fehler.Add("Text fehlt.");
        if (text.Length > Laenge) fehler.Add($"Bitte kurz halten (höchstens {Laenge} Zeichen).");
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        DateTime bis = default;
        try
        {
            var tag = Datum.Lesen(datum, "Gilt bis (Datum)");
            if (!TimeOnly.TryParseExact((uhrzeit ?? "").Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var zeit))
                fehler.Add("Gilt bis (Uhrzeit): bitte als SS:MM eingeben, z. B. 11:30.");
            else
            {
                bis = tag.ToDateTime(zeit);
                if (bis <= jetzt) fehler.Add("„Gilt bis“ liegt in der Vergangenheit.");
            }
        }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return (text, bis);
    }

    // Prüft eine neue Meldung (auch im Speicher, falls jemand die Oberfläche umgeht).
    public static void Pruefen(string text, string kuerzel, string bereich, DateTime giltBis, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) fehler.Add("Text fehlt.");
        else if (text.Trim().Length > Laenge) fehler.Add($"Bitte kurz halten (höchstens {Laenge} Zeichen).");
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (Bereiche.Pruefen(bereich) is string b) fehler.Add(b);
        if (giltBis <= jetzt) fehler.Add("„Gilt bis“ liegt in der Vergangenheit.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
    }

    // „bis 11:30“, an einem anderen Tag „bis Mo 10.11., 11:30“.
    public static string BisText(Statusmeldung s, DateTime jetzt) =>
        $"bis {(s.GiltBis.Date == jetzt.Date ? "" : s.GiltBis.ToString("ddd dd.MM., ", De))}{s.GiltBis:HH:mm}";

    // „beim Arzttermin · bis 11:30 · R1, CD um 09:10“ (für die Akte).
    public static string Langtext(Statusmeldung s, DateTime jetzt) =>
        $"{s.Text} · {BisText(s, jetzt)} · {s.Bereich}, {s.Kuerzel} um {(s.Erstellt.Date == jetzt.Date ? "" : s.Erstellt.ToString("dd.MM., ", De))}{s.Erstellt:HH:mm}";

    // Kurz für die Karten (zu lange Texte kürzt die Karte; der volle Text steht in der Akte).
    public static Marke Marke(Statusmeldung s, DateTime jetzt) => new(MarkenArt.Status, $"Status: {s.Text}, {BisText(s, jetzt)}");

    public static List<string> Verlauf(IEnumerable<Statusmeldung> liste) =>
        liste.OrderByDescending(s => s.Id)
            .Select(s => $"{s.Erstellt:dd.MM.yyyy, HH:mm} · {s.Kuerzel} ({s.Bereich}) · " +
                         (s.Text.Length == 0 ? "aufgehoben" : $"{s.Text} · gilt bis {s.GiltBis:dd.MM.yyyy, HH:mm}"))
            .ToList();
}

public sealed partial class Speicher
{
    // Konfliktschutz für Daten mit Fassungen: erwartet = so viele Fassungen kannte der Aufrufer.
    // Sind es inzwischen mehr, hat ein anderer Arbeitsplatz gespeichert. Die Transaktion wird dann
    // zurückgenommen und die andere Fassung gelesen.
    private void FassungenPruefen(SqliteTransaction transaktion, string zaehlen, int? erwartet, Func<string> andere, string meine,
        params (string, object?)[] parameter)
    {
        if (erwartet is not int e) return;
        long anzahl = Convert.ToInt64(WertIn(transaktion, zaehlen, parameter), CultureInfo.InvariantCulture);
        if (anzahl == e) return;
        transaktion.Rollback();
        throw new Konflikt(Konflikt.Text, andere(), meine);
    }

    internal static string FassungText(string kuerzel, DateTime zeit, string text) =>
        $"{kuerzel}, {zeit:dd.MM.yyyy, HH:mm}{Environment.NewLine}{Environment.NewLine}{text}";

    // Stand der Datenbank für Änderungen anderer Verbindungen (anderer Rechner oder Fenster).
    // SQLite ändert den Wert nur, wenn eine andere Verbindung etwas gespeichert hat.
    public long Datenstand()
    {
        try { return Convert.ToInt64(Wert("PRAGMA data_version"), CultureInfo.InvariantCulture); }
        catch (SqliteException e) { throw new SpeicherFehler($"Die Datenbank ist gerade nicht erreichbar: {e.Message}", e); }
    }

    // Öffnet die Datenbank am eingestellten Ort. Ist der gemeinsame Ordner nicht erreichbar, die
    // lokale – mit Hinweis, damit das Programm trotzdem startet.
    public static (Speicher Speicher, string? Hinweis) Oeffnen(EinstellungsWerte werte)
    {
        if (!Einstellungen.Gemeinsam(werte)) return (new Speicher(), null);
        try
        {
            if (OrdnerPruefen(werte.DatenbankOrdner!) is string fehler) throw new SpeicherFehler(fehler);
            return (new Speicher(Einstellungen.DatenbankPfad(werte)), null);
        }
        catch (Exception e) when (e is SpeicherFehler or IOException or UnauthorizedAccessException)
        {
            return (new Speicher(), $"Der gemeinsame Ordner ist nicht erreichbar: {e.Message}\n\n" +
                                    "Das Programm arbeitet jetzt mit der lokalen Datenbank. Den Ort ändern Sie in den Einstellungen.");
        }
    }

    // Lässt sich in den Ordner schreiben? Rückgabe: Fehlertext oder null.
    public static string? OrdnerPruefen(string ordner)
    {
        if (string.IsNullOrWhiteSpace(ordner) || !Directory.Exists(ordner)) return $"Den Ordner „{ordner}“ gibt es nicht oder er ist nicht erreichbar.";
        string probe = Path.Combine(ordner, $".dokusystem-probe-{Environment.ProcessId}");
        try
        {
            File.WriteAllText(probe, "Probe – kann gelöscht werden");
            File.Delete(probe);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return $"In den Ordner „{ordner}“ lässt sich nicht schreiben: {e.Message}";
        }
    }

    public event Action? StatusGeaendert;

    public Statusmeldung StatusmeldungSetzen(long bewohnerId, string text, DateTime giltBis, string kuerzel, string bereich)
    {
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var jetzt = Uhr();
        StatusmeldungRegeln.Pruefen(text, kuerzel, bereich, giltBis, jetzt);
        using (var transaktion = db.BeginTransaction())
        {
            StatusmeldungEinfuegen(bewohnerId, text.Trim().ReplaceLineEndings(" "), Bereiche.Bereinigen(bereich), kuerzel, jetzt, giltBis, transaktion);
            transaktion.Commit();
        }
        StatusGeaendert?.Invoke();
        return Statusmeldungen(bewohnerId)[^1];
    }

    public void StatusmeldungAufheben(long bewohnerId, string kuerzel, string bereich)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Kürzel fehlt (oben rechts „Angemeldet als …“)."]);
        var jetzt = Uhr();
        using (var transaktion = db.BeginTransaction())
        {
            StatusmeldungEinfuegen(bewohnerId, "", Bereiche.Bereinigen(bereich), kuerzel, jetzt, jetzt, transaktion);
            transaktion.Commit();
        }
        StatusGeaendert?.Invoke();
    }

    // Die geltende Statusmeldung (die neueste, wenn sie nicht abgelaufen oder aufgehoben ist).
    public Statusmeldung? Statusmeldung(long bewohnerId, DateTime jetzt) =>
        Statusmeldungen(bewohnerId).LastOrDefault() is Statusmeldung s && s.Gilt(jetzt) ? s : null;

    // Die geltenden Statusmeldungen aller Bewohner (für Karten und Übersicht), mit einer Abfrage.
    public Dictionary<long, Statusmeldung> GeltendeStatusmeldungen(DateTime jetzt)
    {
        var liste = new Dictionary<long, Statusmeldung>();
        using var befehl = Befehl("SELECT id, bewohner_id, text, bereich, kuerzel, erstellt, gilt_bis FROM statusmeldungen s " +
            "WHERE id = (SELECT MAX(id) FROM statusmeldungen WHERE bewohner_id = s.bewohner_id)", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
        {
            var s = new Statusmeldung(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), leser.GetString(3), leser.GetString(4),
                AusZeit(leser.GetString(5)), AusZeit(leser.GetString(6)));
            if (s.Gilt(jetzt)) liste[s.BewohnerId] = s;
        }
        return liste;
    }

    public List<Statusmeldung> Statusmeldungen(long bewohnerId)
    {
        var liste = new List<Statusmeldung>();
        using var befehl = Befehl("SELECT id, bewohner_id, text, bereich, kuerzel, erstellt, gilt_bis FROM statusmeldungen WHERE bewohner_id = $b ORDER BY id",
            null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Statusmeldung(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), leser.GetString(3), leser.GetString(4),
                AusZeit(leser.GetString(5)), AusZeit(leser.GetString(6))));
        return liste;
    }

    private void StatusmeldungEinfuegen(long bewohnerId, string text, string bereich, string kuerzel, DateTime erstellt, DateTime giltBis, SqliteTransaction transaktion) =>
        Ausfuehren("INSERT INTO statusmeldungen (bewohner_id, text, bereich, kuerzel, erstellt, gilt_bis) VALUES ($b, $t, $r, $k, $e, $g)", transaktion,
            ("$b", bewohnerId), ("$t", text), ("$r", bereich), ("$k", kuerzel.Trim()), ("$e", Zeit(erstellt)), ("$g", Zeit(giltBis)));

    // Demo: Statusmeldungen aus daten/demo/status.json, relativ zur Uhrzeit.
    private void DemoStatusEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM statusmeldungen", transaktion);
        foreach (var m in Demodaten.Statusmeldungen(bewohner, Uhr()))
            StatusmeldungEinfuegen(m.BewohnerId, m.Text, m.Bereich, m.Kuerzel, m.Erstellt, m.GiltBis, transaktion);
        SetzeMeta(DemoStatus, "1", transaktion);
    }
}
