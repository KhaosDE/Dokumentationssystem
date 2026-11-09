using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DokuLine.Kern;

// Anzeige der laufenden Therapien für einen Fernseher im Wohnbereich: je Wohnbereich nur die Therapien, an denen
// jemand aus dem Bereich teilnimmt, mit Name, Uhrzeit, Ort und den Namen der Teilnehmer aus diesem Bereich – ohne
// Zimmer, Marken, Bezugspflege und Teilnahmestatus. Nur lesend, nur im LAN, mit Zugangscode, standardmäßig aus.
public sealed record AnzeigeTermin(string Name, TimeOnly Von, TimeOnly Bis, string Ort, IReadOnlyList<string> Namen)
{
    public string Zeit => $"{Von:HH\\:mm}–{Bis:HH\\:mm}";
}

// Farben der Seite als CSS-Werte; die Oberfläche gibt sie aus ihrem Stil vor.
public sealed record AnzeigeFarben(string Grund, string Flaeche, string Text, string Leise, string Akzent, string Rand);

public sealed record AnzeigeWerte
{
    public bool An { get; init; }
    public int Port { get; init; } = AnzeigeRegeln.StandardPort;
    public bool VollerName { get; init; }
    public IReadOnlyDictionary<string, string> Codes { get; init; } = new Dictionary<string, string>();
}

public static class AnzeigeRegeln
{
    public const int StandardPort = 8080, GleichMinuten = 60, CodeLaenge = 6;
    public const string Pfad = "/anzeige/";
    private const string Zeichen = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // ohne 0/O und 1/I

    // Vorgabe „Vorname und erster Buchstabe des Nachnamens“: Auf dem Fernseher lesen auch Besucher mit.
    public static string Name(Bewohner b, bool voll) =>
        voll || b.Nachname.Length == 0 ? b.Name : $"{b.Vorname} {b.Nachname[..1]}.";

    // Die Termine des Tages für einen Bereich (aus LaufendeRegeln.Heute mit diesem Bereich). Wer abgesagt hat oder
    // wessen Termin ausfällt, steht nicht da; ein Termin ohne Teilnehmer aus dem Bereich fällt weg.
    public static List<AnzeigeTermin> Tag(IEnumerable<TherapieTermin> heute, bool vollerName) =>
        [.. heute.Select(t => new AnzeigeTermin(t.Name, t.Von, t.Bis, t.Fassung.Ort,
                [.. t.Teilnehmer.Where(p => p.Status is not (TafelStatus.Abgesagt or TafelStatus.Ausgefallen))
                    .Select(p => Name(p.Bewohner, vollerName)).Order(StringComparer.CurrentCulture)]))
            .Where(t => t.Namen.Count > 0)];

    public static List<AnzeigeTermin> Jetzt(IEnumerable<AnzeigeTermin> tag, TimeOnly uhr) =>
        [.. tag.Where(t => t.Von <= uhr && uhr < t.Bis)];

    public static List<AnzeigeTermin> Gleich(IEnumerable<AnzeigeTermin> tag, TimeOnly uhr) =>
        [.. tag.Where(t => t.Von > uhr && (t.Von.ToTimeSpan() - uhr.ToTimeSpan()).TotalMinutes <= GleichMinuten)];

    public static string CodeErzeugen() =>
        new(Enumerable.Range(0, CodeLaenge).Select(_ => Zeichen[RandomNumberGenerator.GetInt32(Zeichen.Length)]).ToArray());

    public static bool CodeStimmt(string erwartet, string? gegeben) =>
        gegeben is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(erwartet), Encoding.UTF8.GetBytes(gegeben.Trim().ToUpperInvariant()));

    public static string? PortPruefen(int port) => port is >= 1024 and <= 65535 ? null : "Port: eine Zahl von 1024 bis 65535.";

    // Antwort auf „GET <ziel>“: 404 für unbekannte Seiten und Bereiche, 403 ohne passenden Code, sonst die Seite.
    public static (int Status, string Html) Antwort(string ziel, AnzeigeStand stand, AnzeigeWerte werte, AnzeigeFarben farben, DateTime jetzt,
        string hinweis = "")
    {
        int frage = ziel.IndexOf('?');
        string pfad = frage < 0 ? ziel : ziel[..frage];
        string? code = null;
        if (frage >= 0)
            foreach (var teil in ziel[(frage + 1)..].Split('&'))
                if (teil.StartsWith("code=", StringComparison.Ordinal)) code = Uri.UnescapeDataString(teil[5..]);
        if (!pfad.StartsWith(Pfad, StringComparison.Ordinal)) return (404, Hinweis("Diese Seite gibt es nicht.", farben));
        string bereich = Uri.UnescapeDataString(pfad[Pfad.Length..]);
        if (!werte.Codes.TryGetValue(bereich, out var erwartet)) return (404, Hinweis("Diese Seite gibt es nicht.", farben));
        if (!CodeStimmt(erwartet, code)) return (403, Hinweis("Zugangscode fehlt oder stimmt nicht.", farben));
        return (200, Html(bereich, stand.Lesen(bereich, DateOnly.FromDateTime(jetzt)), jetzt, farben, hinweis));
    }

    // hinweis: klein unten (z. B. „Demo — ausgedachte Daten“).
    public static string Html(string bereich, IReadOnlyList<AnzeigeTermin> tag, DateTime jetzt, AnzeigeFarben farben, string hinweis = "")
    {
        var uhr = TimeOnly.FromDateTime(jetzt);
        var s = new StringBuilder();
        Kopf(s, $"Therapien {bereich}", farben);
        s.Append("<header><h1>Therapien · ").Append(E(bereich)).Append("</h1><div class=\"uhr\">")
            .Append(jetzt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)).Append(" Uhr</div></header>");
        Abschnitt(s, "Jetzt", Jetzt(tag, uhr), "Gerade läuft keine Therapie.");
        Abschnitt(s, "Gleich", Gleich(tag, uhr), "In der nächsten Stunde beginnt keine.");
        if (hinweis.Length > 0) s.Append("<footer>").Append(E(hinweis)).Append("</footer>");
        s.Append("</body></html>");
        return s.ToString();
    }

    private static void Abschnitt(StringBuilder s, string titel, List<AnzeigeTermin> termine, string leer)
    {
        s.Append("<section><h2>").Append(titel).Append("</h2>");
        if (termine.Count == 0) s.Append("<p class=\"leer\">").Append(leer).Append("</p>");
        foreach (var t in termine)
        {
            s.Append("<div class=\"karte\"><div class=\"name\">").Append(E(t.Name)).Append("</div><div class=\"zeit\">")
                .Append(E(t.Zeit)).Append(t.Ort.Length > 0 ? " · " + E(t.Ort) : "").Append("</div><div class=\"wer\">")
                .Append(string.Join(", ", t.Namen.Select(E))).Append("</div></div>");
        }
        s.Append("</section>");
    }

    private static string Hinweis(string text, AnzeigeFarben farben)
    {
        var s = new StringBuilder();
        Kopf(s, "Anzeige", farben);
        s.Append("<p class=\"leer\">").Append(E(text)).Append("</p></body></html>");
        return s.ToString();
    }

    // Groß und hell, auf Abstand lesbar; lädt sich jede Minute neu.
    private static void Kopf(StringBuilder s, string titel, AnzeigeFarben f) =>
        s.Append("<!doctype html><html lang=\"de\"><head><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"60\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>").Append(E(titel)).Append("</title><style>")
            .Append($"body{{margin:0;padding:3vw;background:{f.Grund};color:{f.Text};font-family:'Segoe UI',Arial,sans-serif;font-size:2.2vw}}")
            .Append($"header{{display:flex;justify-content:space-between;align-items:baseline;border-bottom:3px solid {f.Akzent};margin-bottom:2vw}}")
            .Append($"h1{{font-size:3.6vw;margin:0 0 1vw}}.uhr{{font-size:3.6vw;color:{f.Akzent};font-weight:600}}")
            .Append($"h2{{font-size:2.4vw;color:{f.Leise};text-transform:uppercase;letter-spacing:.1em;margin:2vw 0 1vw}}")
            .Append($".karte{{background:{f.Flaeche};border:1px solid {f.Rand};border-radius:1vw;padding:1.4vw 2vw;margin-bottom:1.2vw}}")
            .Append($".name{{font-size:3vw;font-weight:600}}.zeit{{color:{f.Leise}}}.wer{{margin-top:.6vw}}.leer{{color:{f.Leise}}}")
            .Append($"footer{{margin-top:2vw;font-size:1.4vw;color:{f.Leise}}}")
            .Append("</style></head><body>");

    private static string E(string text) => WebUtility.HtmlEncode(text);
}

// Was die Anzeige gerade zeigen kann: die Termine des Tages je Bereich. Das Programm setzt sie (Oberfläche, jede
// Minute und nach Änderungen); der Server liest sie aus anderen Threads.
public sealed class AnzeigeStand
{
    private readonly Lock sperre = new();
    private DateOnly tag;
    private IReadOnlyDictionary<string, List<AnzeigeTermin>> jeBereich = new Dictionary<string, List<AnzeigeTermin>>();

    public void Setzen(DateOnly neuerTag, IReadOnlyDictionary<string, List<AnzeigeTermin>> neu)
    {
        lock (sperre) (tag, jeBereich) = (neuerTag, neu);
    }

    public List<AnzeigeTermin> Lesen(string bereich, DateOnly heute)
    {
        lock (sperre) return heute == tag ? jeBereich.GetValueOrDefault(bereich) ?? [] : [];
    }
}

// Einstellungen der Anzeige in einer eigenen Datei im Datenordner (gelten für diesen Rechner).
public static class AnzeigeEinstellungen
{
    public const string Datei = "anzeige.json";

    public static AnzeigeWerte Laden()
    {
        var werte = new AnzeigeWerte();
        try
        {
            if (JsonNode.Parse(File.ReadAllText(Pfade.ImDatenordner(Datei))) is not JsonObject k) return werte;
            if (k["an"] is JsonValue a && a.TryGetValue(out bool an)) werte = werte with { An = an };
            if (k["port"] is JsonValue p && p.TryGetValue(out int port) && AnzeigeRegeln.PortPruefen(port) is null) werte = werte with { Port = port };
            if (k["voller_name"] is JsonValue v && v.TryGetValue(out bool voll)) werte = werte with { VollerName = voll };
            if (k["codes"] is JsonObject c)
                werte = werte with
                {
                    Codes = c.Where(x => x.Value is JsonValue w && w.TryGetValue(out string? s) && s.Length == AnzeigeRegeln.CodeLaenge)
                        .ToDictionary(x => x.Key, x => x.Value!.GetValue<string>()),
                };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            // fehlt oder unlesbar: aus
        }
        return werte;
    }

    public static void Speichern(AnzeigeWerte werte)
    {
        if (AnzeigeRegeln.PortPruefen(werte.Port) is string fehler) throw new Eingabefehler([fehler]);
        string ziel = Pfade.ImDatenordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        var codes = new JsonObject();
        foreach (var (bereich, code) in werte.Codes) codes[bereich] = code;
        var inhalt = new JsonObject { ["an"] = werte.An, ["port"] = werte.Port, ["voller_name"] = werte.VollerName, ["codes"] = codes };
        File.WriteAllText(ziel + ".neu", inhalt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.Move(ziel + ".neu", ziel, overwrite: true);
    }

    // Jeder Wohnbereich bekommt einen Code; vorhandene bleiben.
    public static AnzeigeWerte MitCodes(AnzeigeWerte werte, IEnumerable<string> bereiche)
    {
        var codes = new Dictionary<string, string>(werte.Codes);
        foreach (var b in bereiche) if (!codes.ContainsKey(b)) codes[b] = AnzeigeRegeln.CodeErzeugen();
        return werte with { Codes = codes };
    }
}
