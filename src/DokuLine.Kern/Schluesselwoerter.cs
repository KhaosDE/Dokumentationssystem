using System.Globalization;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Schlüsselwörter (Bauplan 48): eine feste, einstellbare Liste – keine KI (CLAUDE.md, Regel 6). Trifft ein Wort im Text,
// bietet das Programm den nächsten Schritt an; es legt nie selbst etwas an und ändert keinen Text. Ein Treffer in
// „kein Arzttermin nötig“ bietet trotzdem an – hingenommen, weil nur angeboten wird.
public enum Folgeschritt { Termin, Aufgabe, Medikamentenplan, Bedarf, Vitalwerte, Vorkommnis, Abgaengigkeit, Ueberleitung, Wiedervorlage, Schweigepflicht, Hilfeplan, Ziele }

public enum Treffer { Wort, Wortanfang, Wortgruppe }

public sealed record Schluesselwort(long Id, string Wort, Folgeschritt Folgeschritt, Treffer Art, bool Aktiv, bool Mitgeliefert);

public sealed record Vorschlag(Folgeschritt Folgeschritt, string Wort, string Text, DateOnly? Datum, TimeOnly? Uhrzeit);

public static class SchluesselwortRegeln
{
    public const int Hoechstens = 2;

    public static string Name(Folgeschritt a) => a switch
    {
        Folgeschritt.Termin => "Termin im Kalender",
        Folgeschritt.Aufgabe => "Aufgabe mit Fälligkeit",
        Folgeschritt.Medikamentenplan => "Medikamentenplan öffnen",
        Folgeschritt.Bedarf => "„Bedarf gegeben …“",
        Folgeschritt.Vitalwerte => "Vitalwerte erfassen",
        Folgeschritt.Vorkommnis => "als „Besonderes Vorkommnis“ kennzeichnen",
        Folgeschritt.Abgaengigkeit => "Abgängigkeit melden",
        Folgeschritt.Ueberleitung => "Überleitungsbogen",
        Folgeschritt.Wiedervorlage => "Wiedervorlage (Aufgabe)",
        Folgeschritt.Schweigepflicht => "Schweigepflicht prüfen",
        Folgeschritt.Hilfeplan => "Hilfeplangespräch: Termin vergeben",
        _ => "Ziele öffnen",
    };

    public static string ArtText(Treffer t) => t switch { Treffer.Wort => "ganzes Wort", Treffer.Wortanfang => "Wortanfang", _ => "Wortgruppe" };

    // Die mitgelieferten Wörter (Werkseinstellung).
    public static readonly (string Wort, Folgeschritt Folgeschritt, Treffer Art)[] Mitgeliefert =
    [
        ("Arzttermin", Folgeschritt.Termin, Treffer.Wortanfang), ("Termin bei", Folgeschritt.Termin, Treffer.Wortgruppe), ("Überweisung", Folgeschritt.Termin, Treffer.Wortanfang),
        ("Blutabnahme", Folgeschritt.Aufgabe, Treffer.Wortanfang), ("Labor", Folgeschritt.Aufgabe, Treffer.Wortanfang), ("Ergebnis nachfragen", Folgeschritt.Aufgabe, Treffer.Wortgruppe),
        ("Medikament geändert", Folgeschritt.Medikamentenplan, Treffer.Wortgruppe), ("abgesetzt", Folgeschritt.Medikamentenplan, Treffer.Wort),
        ("neu verordnet", Folgeschritt.Medikamentenplan, Treffer.Wortgruppe), ("Dosis", Folgeschritt.Medikamentenplan, Treffer.Wortanfang),
        ("Bedarf gegeben", Folgeschritt.Bedarf, Treffer.Wortgruppe), ("Bedarfsmedikation", Folgeschritt.Bedarf, Treffer.Wortanfang),
        ("Blutdruck", Folgeschritt.Vitalwerte, Treffer.Wortanfang), ("Gewicht", Folgeschritt.Vitalwerte, Treffer.Wortanfang), ("Temperatur", Folgeschritt.Vitalwerte, Treffer.Wortanfang),
        ("Blutzucker", Folgeschritt.Vitalwerte, Treffer.Wortanfang),
        ("Sturz", Folgeschritt.Vorkommnis, Treffer.Wortanfang), ("gestürzt", Folgeschritt.Vorkommnis, Treffer.Wort), ("Verletzung", Folgeschritt.Vorkommnis, Treffer.Wortanfang),
        ("Gewalt", Folgeschritt.Vorkommnis, Treffer.Wortanfang), ("Polizei", Folgeschritt.Vorkommnis, Treffer.Wortanfang),
        ("nicht zurück", Folgeschritt.Abgaengigkeit, Treffer.Wortgruppe), ("abgängig", Folgeschritt.Abgaengigkeit, Treffer.Wortanfang), ("vermisst", Folgeschritt.Abgaengigkeit, Treffer.Wortanfang),
        ("Klinik", Folgeschritt.Ueberleitung, Treffer.Wortanfang), ("Einweisung", Folgeschritt.Ueberleitung, Treffer.Wortanfang), ("Notarzt", Folgeschritt.Ueberleitung, Treffer.Wortanfang),
        ("Betreuer angerufen", Folgeschritt.Wiedervorlage, Treffer.Wortgruppe), ("Rückruf", Folgeschritt.Wiedervorlage, Treffer.Wortanfang),
        ("Kostenzusage", Folgeschritt.Wiedervorlage, Treffer.Wortanfang),
        ("Angehörige", Folgeschritt.Schweigepflicht, Treffer.Wortanfang), ("Auskunft gegeben", Folgeschritt.Schweigepflicht, Treffer.Wortgruppe),
        ("Hilfeplangespräch", Folgeschritt.Hilfeplan, Treffer.Wortanfang),
        ("Ziel erreicht", Folgeschritt.Ziele, Treffer.Wortgruppe), ("Nahziel", Folgeschritt.Ziele, Treffer.Wortanfang),
    ];

    // Trifft das Wort? Groß-/Kleinschreibung egal; Wortanfang auch als Teil eines längeren Wortes am Wortanfang.
    public static bool Trifft(string text, string wort, Treffer art)
    {
        var w = Regex.Escape(wort.Trim()).Replace("\\ ", "\\s+");
        if (w.Length == 0) return false;
        string muster = art == Treffer.Wortanfang ? $@"(?<![\p{{L}}\d]){w}" : $@"(?<![\p{{L}}\d]){w}(?![\p{{L}}\d])";
        return Regex.IsMatch(text, muster, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    // Höchstens zwei Folgeschritte, in der Reihenfolge des ersten Treffers im Text; weggeklickte kommen nicht wieder.
    public static List<Vorschlag> Finden(string text, IEnumerable<Schluesselwort> woerter, IReadOnlySet<Folgeschritt> weggeklickt)
    {
        if (text.Trim().Length == 0) return [];
        var (datum, uhrzeit) = DatumImText(text);
        var gefunden = new List<(int Stelle, Vorschlag V)>();
        foreach (var w in woerter.Where(w => w.Aktiv && !weggeklickt.Contains(w.Folgeschritt)))
        {
            if (gefunden.Any(g => g.V.Folgeschritt == w.Folgeschritt) || !Trifft(text, w.Wort, w.Art)) continue;
            int stelle = text.IndexOf(w.Wort.Split(' ')[0], StringComparison.CurrentCultureIgnoreCase);
            gefunden.Add((stelle < 0 ? int.MaxValue : stelle, new Vorschlag(w.Folgeschritt, w.Wort, $"„{w.Wort}“ erkannt — {Name(w.Folgeschritt)}?", datum, uhrzeit)));
        }
        return [.. gefunden.OrderBy(g => g.Stelle).Take(Hoechstens).Select(g => g.V)];
    }

    // Erkannte Datums- und Uhrzeitangaben als Vorschlag: „14.11.“, „14.11.2026“, „10:30“, „10 Uhr“.
    public static (DateOnly? Datum, TimeOnly? Uhrzeit) DatumImText(string text, int? jahr = null)
    {
        DateOnly? datum = null;
        TimeOnly? uhrzeit = null;
        var d = Regex.Match(text, @"(?<!\d)(\d{1,2})\.(\d{1,2})\.(\d{4}|\d{2})?(?!\d)");
        if (d.Success && int.TryParse(d.Groups[1].Value, out int tag) && int.TryParse(d.Groups[2].Value, out int monat))
        {
            int j = d.Groups[3].Success ? int.Parse(d.Groups[3].Value, CultureInfo.InvariantCulture) : jahr ?? DateTime.Today.Year;
            if (j < 100) j += 2000;
            if (monat is >= 1 and <= 12 && tag >= 1 && tag <= DateTime.DaysInMonth(j, monat)) datum = new DateOnly(j, monat, tag);
        }
        var u = Regex.Match(text, @"(?<!\d)(\d{1,2}):(\d{2})(?!\d)");
        if (u.Success && int.Parse(u.Groups[1].Value, CultureInfo.InvariantCulture) < 24 && int.Parse(u.Groups[2].Value, CultureInfo.InvariantCulture) < 60)
            uhrzeit = new TimeOnly(int.Parse(u.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(u.Groups[2].Value, CultureInfo.InvariantCulture));
        else if (Regex.Match(text, @"(?<!\d)(\d{1,2})\s*Uhr", RegexOptions.IgnoreCase) is { Success: true } h && int.Parse(h.Groups[1].Value, CultureInfo.InvariantCulture) < 24)
            uhrzeit = new TimeOnly(int.Parse(h.Groups[1].Value, CultureInfo.InvariantCulture), 0);
        return (datum, uhrzeit);
    }

    public static string? Pruefen(string wort)
    {
        var w = wort.Trim();
        if (w.Length < 3) return "Bitte mindestens drei Zeichen.";
        if (w.Length > 60) return "Bitte höchstens 60 Zeichen.";
        return null;
    }

    // Verweis im Bericht auf das, was daraus entstand.
    public static string VerweisText(Folgeschritt a, string was) => a switch
    {
        Folgeschritt.Termin or Folgeschritt.Hilfeplan => $"→ Termin {was}",
        Folgeschritt.Aufgabe or Folgeschritt.Wiedervorlage => $"→ Aufgabe {was}",
        _ => $"→ {Name(a)}",
    };
}
