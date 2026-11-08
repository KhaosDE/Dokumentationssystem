using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Nachschlagen beim BfArM im Formular „Medikament anlegen/ändern“ (Bauplan Schritt 21, CLAUDE.md
// Regel 6): welche Adresse geöffnet wird und welche die Ansicht zeigen darf. Gesucht wird nur mit
// dem Medikamentennamen – die Methoden bekommen nichts anderes.
public static partial class Nachschlagen
{
    public const int SuchbegriffLaenge = 60;

    // Die BfArM-Suche nimmt keinen Suchbegriff in der Adresse an: Sie öffnet leer, der Name wird dort
    // eingetippt oder (aus der Zwischenablage) eingefügt.
    public static Uri Start => new(ExterneAdressen.Arzneimittelsuche);

    // Nur Buchstaben, Ziffern, Leerzeichen und Bindestrich; anderes wird zum Leerzeichen.
    public static string Suchbegriff(string medikamentenname)
    {
        string text = Leerraum().Replace(Erlaubt().Replace(medikamentenname ?? "", " "), " ").Trim();
        return text.Length <= SuchbegriffLaenge ? text : text[..SuchbegriffLaenge].TrimEnd();
    }

    // Darf die Nachschlage-Ansicht diese Adresse zeigen? Nur https und nur die festen Bereiche.
    public static bool Zulaessig(Uri? adresse)
    {
        if (adresse is null || !adresse.IsAbsoluteUri || adresse.Scheme != Uri.UriSchemeHttps) return false;
        string host = adresse.IdnHost.TrimEnd('.');
        return ExterneAdressen.Nachschlagebereiche.Any(b =>
            host.Equals(b, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + b, StringComparison.OrdinalIgnoreCase));
    }

    // Markierter Text aus der Ansicht, bereinigt für ein Eingabefeld (eine Zeile, höchstens 300 Zeichen).
    public static string Bereinigen(string markiert)
    {
        string text = Leerraum().Replace(markiert ?? "", " ").Trim();
        return text.Length <= 300 ? text : text[..300].TrimEnd();
    }

    [GeneratedRegex(@"[^\p{L}\d\- ]")]
    private static partial Regex Erlaubt();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Leerraum();
}
