using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dokusystem.Kern;

namespace Dokusystem.Update;

// Selbst-Update der Windows-Programmdatei (Vorbild: KhaosDE/N3XTBYT3; Ablauf wie in der
// Python-Version bis 0.2.0).
//
// 1. Beim Start fragt das Programm die Adresse ApiNeueste, ob es einen neueren Build gibt
//    (Zeitlimit wenige Sekunden; ohne Netz geht es einfach weiter).
// 2. Wenn ja: Meldung „Neue Version wird geladen“, dann werden Dokumentationssystem.exe und
//    die Prüfsumme Dokumentationssystem.exe.sha256 geladen und verglichen.
// 3. Die laufende .exe wird in „….exe.alt“ umbenannt (das erlaubt Windows auch im Betrieb),
//    die neue an ihren Platz gelegt und gestartet; dieses Programm endet. Die neue Version
//    löscht beim Start die alte Datei.
//
// Jeder Schritt und jeder Fehler steht in update.log im Datenordner. Bei einem Fehler startet
// das Programm ganz normal in der alten Version.
// Abschalten: Häkchen „Beim Start nach Updates suchen“ (einstellungen.json) oder --kein-update.
public static partial class Aktualisierung
{
    public const string ApiNeueste = "https://api.github.com/repos/KhaosDE/Dokumentationssystem/releases/latest";
    public const string DownloadPraefix = "https://github.com/KhaosDE/Dokumentationssystem/releases/download/";
    public const string Datei = "Dokumentationssystem.exe";
    public const string Pruefsumme = "Dokumentationssystem.exe.sha256";
    public const string Log = "update.log";
    public const int LogZeilen = 300;                                      // so viele Zeilen behält update.log
    public static readonly TimeSpan ZeitlimitPruefen = TimeSpan.FromSeconds(4);   // Frage nach neuer Version
    public static readonly TimeSpan ZeitlimitLaden = TimeSpan.FromSeconds(30);    // ohne Daten → Abbruch
    public const string Frisch = "DOKUSYSTEM_FRISCH_AKTUALISIERT";         // gesetzt für die gerade gestartete neue Version

    [GeneratedRegex(@"^build-(\d+)$")]
    private static partial Regex TagMuster();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex HashMuster();

    // ------------------------------------------------------------------ Netz (nur hier)

    internal static HttpRequestMessage Anfrage(string url)
    {
        if (!(url == ApiNeueste || url.StartsWith(DownloadPraefix, StringComparison.Ordinal)))
            throw new UpdateFehler($"Adresse nicht erlaubt: {url}");
        var anfrage = new HttpRequestMessage(HttpMethod.Get, url);
        anfrage.Headers.UserAgent.ParseAdd("Dokumentationssystem-Update");
        anfrage.Headers.Accept.ParseAdd("application/vnd.github+json");
        return anfrage;
    }

    public static JsonNode? HoleJson(string url)
    {
        using var client = new HttpClient { Timeout = ZeitlimitPruefen };
        using var antwort = client.Send(Anfrage(url));
        antwort.EnsureSuccessStatusCode();
        using var strom = antwort.Content.ReadAsStream();
        return JsonNode.Parse(strom);
    }

    // Lädt in eine Datei. fortschritt(gelesen, gesamt) darf false liefern = abbrechen.
    public static void HoleDatei(string url, string ziel, Func<long, long, bool>? fortschritt = null)
    {
        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using var kopfZeit = new CancellationTokenSource(ZeitlimitLaden);
        using var antwort = client.Send(Anfrage(url), HttpCompletionOption.ResponseHeadersRead, kopfZeit.Token);
        antwort.EnsureSuccessStatusCode();
        long gesamt = antwort.Content.Headers.ContentLength ?? 0;
        using var quelle = antwort.Content.ReadAsStream();
        using var datei = File.Create(ziel);
        var puffer = new byte[64 * 1024];
        long gelesen = 0;
        while (true)
        {
            using var zeit = new CancellationTokenSource(ZeitlimitLaden);
            int anzahl = quelle.ReadAsync(puffer, zeit.Token).AsTask().GetAwaiter().GetResult();
            if (anzahl == 0) break;
            datei.Write(puffer, 0, anzahl);
            gelesen += anzahl;
            if (fortschritt is not null && !fortschritt(gelesen, gesamt))
                throw new UpdateFehler("Vom Benutzer abgebrochen.");
        }
    }

    // ------------------------------------------------------------------ Logik (ohne Netz testbar)

    public static int? BuildAusTag(string? tag)
    {
        var treffer = TagMuster().Match(tag ?? "");
        return treffer.Success && int.TryParse(treffer.Groups[1].Value, CultureInfo.InvariantCulture, out int b) ? b : null;
    }

    // Neuerer Build im Release? Dann Download-Adressen; sonst null.
    public static Angebot? AngebotAusRelease(JsonNode? release, int eigenerBuild)
    {
        int? build = BuildAusTag(release?["tag_name"]?.GetValue<string>());
        if (build is null || build <= eigenerBuild) return null;
        var dateien = (release?["assets"] as JsonArray ?? [])
            .OfType<JsonObject>()
            .Where(a => a["name"] is JsonValue)
            .GroupBy(a => a["name"]!.GetValue<string>())
            .ToDictionary(g => g.Key, g => g.First());
        if (!dateien.TryGetValue(Datei, out var exe) || !dateien.TryGetValue(Pruefsumme, out var summe))
            throw new UpdateFehler($"Release build-{build} ohne {Datei} oder {Pruefsumme}.");
        foreach (var asset in new[] { exe, summe })
        {
            string url = asset["browser_download_url"]?.GetValue<string>() ?? "";
            if (!url.StartsWith(DownloadPraefix, StringComparison.Ordinal))
                throw new UpdateFehler($"Unerwartete Download-Adresse: {url}");
        }
        long groesse = exe["size"] is JsonValue g && g.TryGetValue(out long l) ? l : 0;
        return new Angebot(build.Value, exe["browser_download_url"]!.GetValue<string>(),
                           summe["browser_download_url"]!.GetValue<string>(), groesse);
    }

    public static string Sha256(string pfad)
    {
        using var datei = File.OpenRead(pfad);
        return Convert.ToHexStringLower(SHA256.HashData(datei));
    }

    // Erstes Wort der .sha256-Datei (Format „<hash>  Dokumentationssystem.exe“).
    public static string PruefsummeLesen(string text)
    {
        string wort = text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant() ?? "";
        if (!HashMuster().IsMatch(wort)) throw new UpdateFehler("Prüfsummen-Datei unlesbar.");
        return wort;
    }

    // Laufende .exe → .alt umbenennen, neue an ihren Platz. Klappt das Einsetzen nicht, kommt
    // die alte Datei zurück, damit das Programm startbar bleibt.
    public static string Austauschen(string exe, string neu)
    {
        string alt = exe + ".alt";
        if (File.Exists(alt)) File.Delete(alt);
        File.Move(exe, alt);
        try
        {
            File.Move(neu, exe);
        }
        catch (Exception)
        {
            File.Move(alt, exe);
            throw;
        }
        return alt;
    }

    // Reste eines früheren Updates löschen (die alte Datei ist jetzt nicht mehr in Benutzung).
    // Nach dem Wechsel von der Python-Version ist .alt deren Programmdatei.
    public static void Aufraeumen(string exe)
    {
        foreach (string rest in new[] { exe + ".alt", exe + ".neu", exe + ".sha256" })
        {
            try { if (File.Exists(rest)) File.Delete(rest); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }   // läuft vielleicht noch kurz – nächstes Mal
        }
    }

    public static void Starte(string exe)
    {
        var start = new ProcessStartInfo(exe) { UseShellExecute = false };
        start.Environment[Frisch] = "1";
        Process.Start(start);
    }

    // ------------------------------------------------------------------ Protokoll

    // Eine Zeile an update.log im Datenordner anhängen; nur die letzten LogZeilen bleiben.
    public static void Protokoll(string text)
    {
        try
        {
            string datei = Pfade.ImDatenordner(Log);
            Directory.CreateDirectory(Path.GetDirectoryName(datei)!);
            var zeilen = File.Exists(datei) ? File.ReadAllLines(datei).ToList() : [];
            zeilen.Add($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)}  {text}");
            File.WriteAllLines(datei, zeilen.TakeLast(LogZeilen));
        }
        catch (Exception)
        {
            // Das Protokoll darf den Start nie verhindern.
        }
    }

    // ------------------------------------------------------------------ Ablauf

    public static bool AlsWindowsProgramm(int build) =>
        build > 0 && OperatingSystem.IsWindows() &&
        string.Equals(Path.GetFileName(Environment.ProcessPath), Datei, StringComparison.OrdinalIgnoreCase);

    // Warum diesmal nicht nachgefragt wird – oder null, wenn nachgefragt werden soll.
    public static string? GrundOhneUpdate(IReadOnlyList<string> argumente, int build, bool? alsWindowsProgramm = null)
    {
        if (argumente.Contains("--kein-update")) return "gestartet mit --kein-update";
        if (argumente.Contains("--selbsttest")) return "Selbsttest";
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Frisch))) return "gerade aktualisiert";
        if (build <= 0) return "Entwicklerversion";
        if (!(alsWindowsProgramm ?? AlsWindowsProgramm(build))) return "nicht als Windows-Programm gestartet";
        if (!Einstellungen.Laden().AutoUpdate) return "in den Einstellungen abgeschaltet";
        return null;
    }

    // Ein Update-Durchgang. true = neue Version gestartet (aufrufendes Programm soll enden).
    // fensterMachen erzeugt das Fortschrittsfenster – erst, wenn es wirklich etwas zu laden gibt.
    public static bool Aktualisieren(string exe, int eigenerBuild,
        Func<string, JsonNode?>? holeJson = null,
        Action<string, string, Func<long, long, bool>?>? holeDatei = null,
        Func<IFortschritt>? fensterMachen = null,
        Action<string>? starten = null)
    {
        holeJson ??= HoleJson;
        holeDatei ??= HoleDatei;
        starten ??= Starte;

        Protokoll($"Build {eigenerBuild}: frage nach neuer Version.");
        var angebot = AngebotAusRelease(holeJson(ApiNeueste), eigenerBuild);
        if (angebot is null)
        {
            Protokoll($"Build {eigenerBuild} ist aktuell.");
            return false;
        }
        Protokoll($"Neue Version gefunden: Build {angebot.Build}. Lade …");
        var fenster = fensterMachen?.Invoke();
        try
        {
            return LadenUndTauschen(exe, eigenerBuild, angebot, holeDatei, fenster, starten);
        }
        finally
        {
            fenster?.Schliessen();
        }
    }

    private static bool LadenUndTauschen(string exe, int eigenerBuild, Angebot angebot,
        Action<string, string, Func<long, long, bool>?> holeDatei, IFortschritt? fenster, Action<string> starten)
    {
        string neu = exe + ".neu";
        string summe = exe + ".sha256";
        string text = $"Neue Version wird geladen (Build {eigenerBuild} → Build {angebot.Build}) …";

        bool Fortschritt(long gelesen, long gesamt)
        {
            if (gesamt <= 0) gesamt = angebot.Groesse;
            return fenster?.Zeige(text, gesamt > 0 ? (double)gelesen / gesamt : null) ?? true;
        }

        try
        {
            fenster?.Zeige(text, 0.0);
            holeDatei(angebot.PruefsummeUrl, summe, null);
            string erwartet = PruefsummeLesen(File.ReadAllText(summe));
            holeDatei(angebot.Url, neu, Fortschritt);
            if (Sha256(neu) != erwartet) throw new UpdateFehler("Prüfsumme stimmt nicht – Download beschädigt.");
            Protokoll("Prüfsumme stimmt.");
            fenster?.Zeige($"Build {angebot.Build} wird gestartet …", 1.0);
            Austauschen(exe, neu);
        }
        finally
        {
            foreach (string rest in new[] { neu, summe })
            {
                try { if (File.Exists(rest)) File.Delete(rest); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            }
        }
        starten(exe);
        Protokoll($"Ausgetauscht: Build {eigenerBuild} → Build {angebot.Build}. Neustart.");
        return true;
    }

    // Wird beim Programmstart aufgerufen. true = neue Version läuft, dieses Programm soll enden.
    public static bool BeimStart(IReadOnlyList<string> argumente, int build, Func<IFortschritt>? fensterMachen)
    {
        bool alsExe = AlsWindowsProgramm(build);
        string exe = Environment.ProcessPath ?? "";
        if (alsExe) Aufraeumen(exe);
        string? grund = GrundOhneUpdate(argumente, build, alsExe);
        if (grund is not null)
        {
            if (alsExe && !argumente.Contains("--selbsttest"))
                Protokoll($"Build {build} gestartet, keine Nachfrage ({grund}).");
            return false;
        }
        try
        {
            return Aktualisieren(exe, build, fensterMachen: fensterMachen);
        }
        catch (Exception e)   // ohne Update weiterstarten, nie hängen bleiben
        {
            Protokoll($"Kein Update, Build {build} läuft weiter: {e.Message}");
            return false;
        }
    }
}

public sealed record Angebot(int Build, string Url, string PruefsummeUrl, long Groesse);

// Etwas hat nicht geklappt – das Programm startet dann in der alten Version.
public sealed class UpdateFehler(string meldung) : Exception(meldung);

// Das Fortschrittsfenster, das die Oberfläche bereitstellt.
public interface IFortschritt
{
    // Text und Balken setzen (anteil null = unbekannt). false = abgebrochen.
    bool Zeige(string text, double? anteil);
    void Schliessen();
}
