using System.Globalization;
using System.Text.RegularExpressions;

namespace Dokusystem.Kern;

// Datumsangaben im Format TT.MM.JJJJ. Führende Nullen dürfen beim Eingeben fehlen (3.4.1965).
public static partial class Datum
{
    [GeneratedRegex(@"^(\d{1,2})\.(\d{1,2})\.(\d{4})$")]
    private static partial Regex Muster();

    public static string Text(DateOnly? datum) =>
        datum is DateOnly d ? d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : "";

    // Liest ein Datum. Fehler kommen als verständlicher Satz (FormatException).
    public static DateOnly Lesen(string? text, string feld)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) throw new FormatException($"{feld} fehlt.");
        var treffer = Muster().Match(text);
        if (!treffer.Success)
            throw new FormatException($"{feld}: bitte im Format TT.MM.JJJJ eingeben, z. B. 03.04.1965.");
        int tag = int.Parse(treffer.Groups[1].Value, CultureInfo.InvariantCulture);
        int monat = int.Parse(treffer.Groups[2].Value, CultureInfo.InvariantCulture);
        int jahr = int.Parse(treffer.Groups[3].Value, CultureInfo.InvariantCulture);
        try
        {
            return new DateOnly(jahr, monat, tag);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new FormatException($"{feld}: Den {text} gibt es nicht.");
        }
    }
}
