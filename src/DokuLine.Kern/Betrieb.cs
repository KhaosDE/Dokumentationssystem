using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DokuLine.Kern;

// Betriebsmodus für den Terminalserver (Bauplan 37). Ein Programm, zwei Modi: Liegt im Programmordner eine Datei
// betrieb.json (legt nur der Verwalter an, nie im Repository), läuft DokuLine im Betrieb – gemeinsamer Datenordner,
// Anmeldung mit Passwort, keine Demo-Daten, kein Selbst-Update. Gebaut und getestet wird nur mit ausgedachten Daten.
public sealed record BetriebWerte
{
    public string Datenordner { get; init; } = "";
    public string Sicherungsordner { get; init; } = "";
    public int SperreMinuten { get; init; } = 10;                // ohne Eingabe: Bildschirm sperren
    public int ProtokollMonate { get; init; } = 24;              // Aufbewahrung des Zugriffsprotokolls
    public TimeOnly SicherungUm { get; init; } = new(2, 30);     // nächtliche Sicherung (Aufgabenplanung oder nächster Start)
}

public static class Betrieb
{
    public const string Datei = "betrieb.json";
    public const string StatusText = "Betrieb";

    public static BetriebWerte? Werte { get; private set; }
    public static bool Aktiv => Werte is not null;

    // Beim Start einmal: liest betrieb.json aus dem Programmordner. Fehlt sie, bleibt es die Demo.
    public static BetriebWerte? Laden(string programmordner)
    {
        string pfad = Path.Combine(programmordner, Datei);
        if (!File.Exists(pfad)) return null;
        JsonObject k;
        try { k = JsonNode.Parse(File.ReadAllText(pfad)) as JsonObject ?? throw new SpeicherFehler($"{Datei}: kein gültiger Inhalt."); }
        catch (JsonException e) { throw new SpeicherFehler($"{Datei} ist nicht lesbar: {e.Message}"); }
        string Text(string name) => k[name] is JsonValue v && v.TryGetValue(out string? s) ? s.Trim() : "";
        int Zahl(string name, int vorgabe, int min, int max) =>
            k[name] is JsonValue v && v.TryGetValue(out int z) ? (z >= min && z <= max ? z : throw new SpeicherFehler($"{Datei}: „{name}“ muss zwischen {min} und {max} liegen.")) : vorgabe;
        var w = new BetriebWerte
        {
            Datenordner = Text("datenordner"), Sicherungsordner = Text("sicherungsordner"),
            SperreMinuten = Zahl("sperre_minuten", 10, 1, 120), ProtokollMonate = Zahl("protokoll_monate", 24, 1, 120),
            SicherungUm = Text("sicherung_um") is { Length: > 0 } u && TimeOnly.TryParseExact(u, "HH:mm", out var t) ? t : new TimeOnly(2, 30),
        };
        if (w.Datenordner.Length == 0 || !Path.IsPathFullyQualified(w.Datenordner))
            throw new SpeicherFehler($"{Datei}: „datenordner“ fehlt oder ist kein vollständiger Pfad (z. B. D:\\DokuLine\\Daten).");
        // Die Sicherung schreibt mit mehr Rechten als ein Benutzer: nie in einen Ordner, den Benutzer ändern dürfen.
        if (w.Sicherungsordner.Length == 0 || !Path.IsPathFullyQualified(w.Sicherungsordner))
            throw new SpeicherFehler($"{Datei}: „sicherungsordner“ fehlt oder ist kein vollständiger Pfad (z. B. E:\\DokuLine-Sicherungen).");
        if (Innerhalb(w.Sicherungsordner, w.Datenordner))
            throw new SpeicherFehler($"{Datei}: „sicherungsordner“ darf nicht im Datenordner liegen (den dürfen alle Benutzer ändern).");
        return w;
    }

    public static void Setzen(BetriebWerte? werte) => Werte = werte;

    private static bool Innerhalb(string pfad, string ordner)
    {
        string p = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pfad)), o = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ordner));
        return p.Equals(o, StringComparison.OrdinalIgnoreCase) || p.StartsWith(o + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    // Eine Datenbank, die einmal im Betrieb geöffnet wurde, öffnet die Demo nie (sie würde Demo-Daten laden).
    public const string MetaSchluessel = "betrieb";
    public const string DemoVerbotenText = "Diese Datenbank gehört zum Betrieb. Die Demo öffnet sie nicht – sie würde dort Demo-Daten laden. " +
                                           "Im Betrieb wird DokuLine mit der Datei betrieb.json im Programmordner gestartet.";

    public static string Sicherungsordner => Werte is { Sicherungsordner.Length: > 0 } w ? w.Sicherungsordner : Pfade.ImDatenordner("sicherungen");
}

// Wer in diesem Programmlauf angemeldet ist. Im Betrieb nur im Arbeitsspeicher: Die Einstellungsdatei gehört dem
// Benutzer und ließe sich ändern (dort steht nur der Vorschlag für die nächste Anmeldung). In der Demo wie bisher.
public static class Sitzung
{
    private static string angemeldet = "";

    public static string Kuerzel => Betrieb.Aktiv ? angemeldet : Einstellungen.Laden().Kuerzel;

    // Nach erfolgreicher Anmeldung bzw. beim Abmelden ("").
    public static void Anmelden(string kuerzel)
    {
        angemeldet = kuerzel;
        Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
    }
}

public enum AnmeldeErgebnis { Ok, AendernPflicht, Falsch, Gesperrt, OhnePasswort, Abgelaufen }

// Passwörter nur als Hash (PBKDF2 mit SHA-256 aus .NET, keine neue Abhängigkeit); Vergleich in fester Zeit.
public static class PasswortRegeln
{
    // 600 000 Durchläufe: Empfehlung von OWASP für PBKDF2-SHA256 (die Datei mit den Hashes können Benutzer lesen, docs/betrieb.md).
    public const int Iterationen = 600_000, SalzBytes = 16, HashBytes = 32, MindestLaenge = 10, Fehlversuche = 5, EinmalTage = 7;
    public const string GesperrtText = "Nach 5 Fehlversuchen gesperrt. Entsperren kann die Leitung oder der Verwalter.";

    public static (string Hash, string Salz) Hashen(string passwort, byte[]? salz = null)
    {
        salz ??= RandomNumberGenerator.GetBytes(SalzBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passwort), salz, Iterationen, HashAlgorithmName.SHA256, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salz));
    }

    public static bool Pruefen(string passwort, string hash, string salz, int iterationen)
    {
        var erwartet = Convert.FromBase64String(hash);
        var ist = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passwort), Convert.FromBase64String(salz), iterationen, HashAlgorithmName.SHA256, erwartet.Length);
        return CryptographicOperations.FixedTimeEquals(ist, erwartet);
    }

    public static string? Schwach(string passwort, string kuerzel)
    {
        if (passwort.Length < MindestLaenge) return $"Das Passwort braucht mindestens {MindestLaenge} Zeichen.";
        if (passwort.Contains(kuerzel, StringComparison.OrdinalIgnoreCase)) return "Das Passwort darf das Kürzel nicht enthalten.";
        if (passwort.Distinct().Count() < 4) return "Das Passwort ist zu einfach (zu wenige verschiedene Zeichen).";
        return null;
    }

    // Einmalpasswort für den ersten Start: gut lesbar (ohne 0/O, 1/l), zum Weitergeben auf Papier.
    public static string Einmalpasswort()
    {
        const string zeichen = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string([.. Enumerable.Range(0, 12).Select(_ => zeichen[RandomNumberGenerator.GetInt32(zeichen.Length)])]);
    }
}

public sealed record Zugriff(long Id, DateTime Zeit, string Kuerzel, long? BewohnerId, string Aktion, string Was);

public static class ZugriffRegeln
{
    public const string Geoeffnet = "geöffnet", Gedruckt = "gedruckt", Geaendert = "geändert";
}

public sealed record Sicherung(string Pfad, DateTime Zeit, long Groesse);

// Nächtliche Sicherung: 30 tägliche und 12 monatliche Stände; eine Sicherung je Tag.
public static class SicherungRegeln
{
    public const string Praefix = "dokuline-", Endung = ".zip", DatenbankEintrag = "dokuline.db";
    public const int Taeglich = 30, Monatlich = 12, WarnTage = 2;

    public static string Name(DateTime zeit) => $"{Praefix}{zeit:yyyy-MM-dd_HHmm}{Endung}";

    public static DateTime? ZeitAus(string datei)
    {
        string name = Path.GetFileNameWithoutExtension(datei);
        return name.StartsWith(Praefix, StringComparison.Ordinal) &&
               DateTime.TryParseExact(name[Praefix.Length..], "yyyy-MM-dd_HHmm", null, System.Globalization.DateTimeStyles.None, out var z) ? z : null;
    }

    // Was weg darf: behalten werden die neueste je Tag der letzten 30 Tage mit Sicherung und die neueste je Monat für 12 Monate.
    public static List<Sicherung> Ueberzaehlig(IReadOnlyList<Sicherung> alle)
    {
        var sortiert = alle.OrderByDescending(s => s.Zeit).ToList();
        var behalten = sortiert.GroupBy(s => s.Zeit.Date).Take(Taeglich).Select(g => g.First())
            .Concat(sortiert.GroupBy(s => (s.Zeit.Year, s.Zeit.Month)).Take(Monatlich).Select(g => g.First())).ToHashSet();
        return [.. sortiert.Where(s => !behalten.Contains(s))];
    }

    // Fällig, wenn es seit dem letzten Sicherungszeitpunkt (heute oder gestern um „sicherung_um“) keine gibt.
    public static bool Faellig(DateTime? letzte, DateTime jetzt, TimeOnly um)
    {
        var heuteUm = DateOnly.FromDateTime(jetzt).ToDateTime(um);
        var stichtag = jetzt >= heuteUm ? heuteUm : heuteUm.AddDays(-1);
        return letzte is not DateTime l || l < stichtag;
    }

    public static bool Warnen(DateTime? letzte, DateTime jetzt) => letzte is not DateTime l || jetzt - l > TimeSpan.FromDays(WarnTage);
}

// Häuser: Bereiche gehören zu einem Haus; ohne Häuser (Demo) sieht jeder alle Bereiche.
public static class HaeuserRegeln
{
    // Welche Wohnbereiche jemand wählen kann: die des eigenen Hauses (Häuser seiner Bereiche), andere nur mit Recht.
    public static List<string> Sichtbar(IReadOnlyList<string> wohnbereiche, IReadOnlyDictionary<string, string> hausVon, IEnumerable<string> eigeneBereiche,
        bool andereHaeuser)
    {
        if (andereHaeuser || hausVon.Count == 0) return [.. wohnbereiche];
        var eigene = eigeneBereiche.Select(b => hausVon.GetValueOrDefault(b)).OfType<string>().ToHashSet();
        if (eigene.Count == 0) return [.. wohnbereiche];   // keinem Haus zugeordnet: nichts verstecken
        return [.. wohnbereiche.Where(w => !hausVon.TryGetValue(w, out var h) || eigene.Contains(h))];
    }
}
