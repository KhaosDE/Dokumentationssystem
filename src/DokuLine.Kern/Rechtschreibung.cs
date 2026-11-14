using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Rechtschreibung (Bauplan 49): geprüft wird mit der Rechtschreibprüfung von Windows (ohne Internet, ohne KI). Besser als
// der Standard, weil DokuLine seine eigenen Wörter kennt: Namen der Bewohner und der Verzeichnisse, Kürzel, Medikamente,
// Therapieangebote, Orte und das Fachwörterbuch gelten als richtig. Diese Wörter verlassen DokuLine nicht – sie werden nur
// hier herausgefiltert, nie in Wörterbuchdateien von Windows geschrieben. Grammatik und Stil prüft das Programm nicht.
public interface IRechtschreibung
{
    // Unbekannte Stellen im Text (Beginn, Länge).
    IReadOnlyList<(int Start, int Laenge)> Fehler(string text);
    IReadOnlyList<string> Vorschlaege(string wort);
    // Für diese Sitzung als richtig behandeln (nichts wird gespeichert).
    void Ignorieren(string wort);
}

public sealed record Schreibfehler(int Start, int Laenge, string Wort);

public static partial class RechtschreibRegeln
{
    public const int Vorschlaege = 5;
    public const string NichtVerfuegbar = "Die deutsche Rechtschreibprüfung von Windows ist auf diesem Rechner nicht verfügbar (Sprachpaket „Deutsch“ fehlt). DokuLine läuft ohne Prüfung weiter.";

    [GeneratedRegex(@"[\p{L}][\p{L}\-']*[\p{L}]|[\p{L}]")]
    private static partial Regex WortMuster();

    // Die Wörter, die das Programm selbst kennt: in Wortteile zerlegt („Dr. Esche-Linde (ausgedacht)“ → Esche, Linde …).
    public static HashSet<string> EigeneWoerter(IEnumerable<string> quellen)
    {
        var woerter = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var q in quellen)
            foreach (Match m in WortMuster().Matches(q))
            {
                woerter.Add(m.Value);
                foreach (var teil in m.Value.Split('-', StringSplitOptions.RemoveEmptyEntries)) woerter.Add(teil);
            }
        return woerter;
    }

    // Was die Prüfung meldet, ohne eigene Wörter, Wörter mit Ziffern, Kürzel in Großbuchstaben (bis 5 Zeichen) und Einzelbuchstaben.
    public static List<Schreibfehler> Filtern(string text, IEnumerable<(int Start, int Laenge)> gemeldet, IReadOnlySet<string> eigene, IReadOnlySet<string> ignoriert)
    {
        var liste = new List<Schreibfehler>();
        foreach (var (start, laenge) in gemeldet)
        {
            if (start < 0 || laenge <= 0 || start + laenge > text.Length) continue;
            string wort = text.Substring(start, laenge);
            if (wort.Length < 2 || wort.Any(char.IsDigit)) continue;
            if (wort.Length <= 5 && wort.All(c => !char.IsLetter(c) || char.IsUpper(c))) continue;
            if (eigene.Contains(wort) || ignoriert.Contains(wort)) continue;
            if (wort.Contains('-') && wort.Split('-', StringSplitOptions.RemoveEmptyEntries).All(eigene.Contains)) continue;
            liste.Add(new Schreibfehler(start, laenge, wort));
        }
        return liste;
    }

    public static string? HinweisText(int anzahl) => anzahl switch { 0 => null, 1 => "1 Wort unbekannt", _ => $"{anzahl} Wörter unbekannt" };

    // Mitgelieferte Fachwörter (selbst zusammengestellt, ohne fremde Wörterbuchdateien).
    public static readonly string[] Grundliste =
    [
        "Bezugspflege", "Bezugspflegekraft", "Bezugsbetreuung", "Einzeltrainer", "Einzeltraining", "Nahziel", "Nahziele", "Leitziel", "Leitziele", "Lebensbereich",
        "Lebensbereiche", "FOSA", "Bedarfsmedikation", "Exazerbation", "Dekompensation", "Rezidiv", "Psychoedukation", "Krisenintervention", "Deeskalation",
        "Fixierung", "Unterbringungsbeschluss", "Betreuungsrecht", "Hilfeplangespräch", "Hilfeplanung", "Eingliederungshilfe", "Kostenträger", "Kostenzusage",
        "Wohnbereich", "Wohnheim", "Tagesstruktur", "Wochenplan", "Monatsreflexion", "Übergabe", "Visite", "Ergotherapie", "Arbeitstherapie", "Soziotherapie",
        "Medikamentenplan", "Vitalwerte", "Compliance", "Adhärenz", "Antriebsminderung", "Antriebssteigerung", "Affektlabilität", "Halluzination",
        "Halluzinationen", "Wahnerleben", "Ich-Störung", "Suizidalität", "Selbstfürsorge", "Ressourcen", "ressourcenorientiert", "Partizipation", "Teilhabe",
        "Empowerment", "Recovery", "Frühwarnzeichen", "Krisenplan", "Schweigepflicht", "Schweigepflichtentbindung", "Personenbeschreibung", "Abgängigkeit",
        "Überleitungsbogen", "Ausgangsregelung", "Ausgangsbeschränkung", "Zimmergruppe", "Küchendienst", "Bedarfsgabe", "Tagesplan", "Nachtdienst",
        "Frühdienst", "Spätdienst", "Wiedervorlage", "Fachdienst", "Fachkraft", "Assistenzkraft", "Heimaufsicht",
    ];

    // Autokorrektur (wahlweise): typische Vertipper → richtiges Wort; nur ganze Wörter.
    public static readonly IReadOnlyDictionary<string, string> Vertipper = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["udn"] = "und", ["ud"] = "und", ["nicth"] = "nicht", ["nciht"] = "nicht", ["ncht"] = "nicht", ["dsa"] = "das",
        ["hatt"] = "hat", ["wurd"] = "wurde", ["Bewohenr"] = "Bewohner", ["Bwohner"] = "Bewohner", ["Medikamnet"] = "Medikament", ["Medikamnete"] = "Medikamente",
        ["agbelehnt"] = "abgelehnt", ["abegelehnt"] = "abgelehnt", ["Zimemr"] = "Zimmer", ["Termni"] = "Termin", ["Gespärch"] = "Gespräch",
    };

    public static string? Korrektur(string wort) => Vertipper.TryGetValue(wort, out var richtig) && richtig != wort ? richtig : null;

    public static string? WortPruefen(string wort)
    {
        var w = wort.Trim();
        if (w.Length < 2 || w.Length > 40) return "Bitte ein Wort mit 2 bis 40 Zeichen.";
        if (w.Contains(' ')) return "Bitte ein einzelnes Wort (ohne Leerzeichen).";
        return null;
    }
}
