using System.Text.RegularExpressions;

namespace Dokusystem.Kern;

// Biografie je Bewohner in festen Abschnitten, jeweils Fließtext ohne Aufzählungszeichen.
// Je Abschnitt wählbar „vom Bewohner angegeben, nicht überprüft“ – dann erscheint ein Hinweis auf
// die Formulierung „gibt an, dass …“ (nicht automatisch eingesetzt). Leere Abschnitte stehen am
// Ende als „fehlende Angaben“. Jede Änderung ist eine neue Fassung mit Datum und Kürzel.

public sealed record BiografieFassung
{
    public int Abschnitt { get; init; }        // Index in BiografieRegeln.Abschnitte
    public string Text { get; init; } = "";
    public bool Angegeben { get; init; }       // „vom Bewohner angegeben, nicht überprüft“
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
}

public sealed record BiografieAbschnitt(int Nummer, string Titel, IReadOnlyList<BiografieFassung> Fassungen)
{
    public BiografieFassung? Aktuell => Fassungen.Count > 0 ? Fassungen[^1] : null;
    public bool Leer => string.IsNullOrWhiteSpace(Aktuell?.Text);
}

public static partial class BiografieRegeln
{
    public static readonly string[] Abschnitte =
    [
        "Herkunft und Familie",
        "Schule und Beruf",
        "Wohnen",
        "Interessen und Gewohnheiten",
        "Wichtige Lebensereignisse",
        "Wünsche",
    ];

    public const string Formulierung = "gibt an, dass …";

    [GeneratedRegex(@"^\s*([-–•*·]|\d+[.)])\s", RegexOptions.Multiline)]
    private static partial Regex Aufzaehlung();

    public static bool HatAufzaehlung(string text) => Aufzaehlung().IsMatch(text);

    // Hinweis für „angegeben“: Steht die Formulierung „gibt an“ (oder „berichtet“, „erzählt“) noch nicht im Text?
    public static bool FormulierungFehlt(BiografieFassung f) =>
        f.Angegeben && f.Text.Trim().Length > 0 &&
        !Regex.IsMatch(f.Text, @"\b(gibt|gab)\s+an\b|\bberichtet\b|\berzählt\b|\bnach eigenen Angaben\b", RegexOptions.IgnoreCase);

    public static BiografieFassung AusEingabe(int abschnitt, string text, bool angegeben, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (abschnitt < 0 || abschnitt >= Abschnitte.Length) fehler.Add("Unbekannter Abschnitt.");
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (HatAufzaehlung(text)) fehler.Add("Bitte als Fließtext schreiben, ohne Aufzählungszeichen.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        // Zeilenumbrüche innerhalb eines Absatzes bleiben; überzählige Leerzeilen fallen weg.
        string sauber = Regex.Replace(text.Trim(), @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
        return new BiografieFassung { Abschnitt = abschnitt, Text = sauber, Angegeben = angegeben && sauber.Length > 0, Erstellt = jetzt, Kuerzel = kuerzel.Trim() };
    }

    public static List<string> FehlendeAngaben(IEnumerable<BiografieAbschnitt> abschnitte) =>
        abschnitte.Where(a => a.Leer).Select(a => a.Titel).ToList();

    // Alle Abschnitte als reiner Text (für die Zwischenablage), fehlende Angaben am Ende.
    public static string AlsFliesstext(string name, IReadOnlyList<BiografieAbschnitt> abschnitte)
    {
        var teile = new List<string> { $"Biografie {name}" };
        foreach (var a in abschnitte.Where(a => !a.Leer))
            teile.Add($"{a.Titel}{Environment.NewLine}{a.Aktuell!.Text}" +
                      (a.Aktuell.Angegeben ? $"{Environment.NewLine}(vom Bewohner angegeben, nicht überprüft)" : ""));
        var fehlend = FehlendeAngaben(abschnitte);
        if (fehlend.Count > 0) teile.Add($"Fehlende Angaben: {string.Join(", ", fehlend)}.");
        return string.Join(Environment.NewLine + Environment.NewLine, teile);
    }

    public static List<string> Verlauf(BiografieAbschnitt a) =>
        a.Fassungen.Select((f, i) => $"{f.Erstellt:dd.MM.yyyy, HH:mm} · {(i == 0 ? "angelegt" : "geändert")} von {f.Kuerzel}" +
                                     (f.Angegeben ? " · vom Bewohner angegeben" : "") + Environment.NewLine +
                                     (f.Text.Length > 0 ? f.Text : "(leer)")).ToList();
}
