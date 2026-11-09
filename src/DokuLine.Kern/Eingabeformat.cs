using System.Globalization;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Datum und Uhrzeit mit automatischen Punkten – ein gemeinsamer Baustein für alle
// Felder: Beim Tippen setzt das Programm die Punkte (TT. MM.) bzw. den Doppelpunkt (SS:) selbst; beim
// Verlassen und beim Lesen werden 8 Ziffern zu TT.MM.JJJJ, 6 Ziffern und TT.MM.JJ zu TT.MM.20JJ,
// 4 Ziffern zu SS:MM. Wer selbst Punkte tippt, wird nicht gestört.
public static partial class Eingabeformat
{
    [GeneratedRegex(@"^(\d{1,2})$")]
    private static partial Regex NurTag();

    [GeneratedRegex(@"^(\d{1,2})\.(\d{2})$")]
    private static partial Regex TagUndMonat();

    [GeneratedRegex(@"^(\d{1,2})\.(\d{1,2})\.(\d{2})$")]
    private static partial Regex KurzesJahr();

    // Text vor dem Einfügen einer Ziffer am Ende: mit Punkt, wenn „TT“ (zwei Ziffern, gültiger Tag)
    // oder „TT.MM“ (gültiger Monat) dasteht; sonst null (nichts ändern).
    public static string? DatumVorZiffer(string bisher)
    {
        if (bisher.Length == 2 && NurTag().IsMatch(bisher) && Zahl(bisher) is >= 1 and <= 31) return bisher + ".";
        var m = TagUndMonat().Match(bisher);
        if (m.Success && Zahl(m.Groups[2].Value) is >= 1 and <= 12) return bisher + ".";
        return null;
    }

    // Doppelpunkt nach zwei Ziffern, wenn sie eine Stunde sein können (00–23).
    public static string? UhrzeitVorZiffer(string bisher) =>
        bisher.Length == 2 && bisher.All(char.IsAsciiDigit) && Zahl(bisher) <= 23 ? bisher + ":" : null;

    public static string DatumFertig(string? text)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 8 && t.All(char.IsAsciiDigit)) return $"{t[..2]}.{t[2..4]}.{t[4..]}";
        if (t.Length == 6 && t.All(char.IsAsciiDigit)) return $"{t[..2]}.{t[2..4]}.20{t[4..]}";
        var m = KurzesJahr().Match(t);
        if (m.Success) return $"{m.Groups[1].Value}.{m.Groups[2].Value}.20{m.Groups[3].Value}";
        return t;
    }

    public static string UhrzeitFertig(string? text)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 4 && t.All(char.IsAsciiDigit)) return $"{t[..2]}:{t[2..]}";
        if (t.Length == 3 && t.All(char.IsAsciiDigit)) return $"0{t[0]}:{t[1..]}";
        return t;
    }

    private static int Zahl(string s) => int.Parse(s, CultureInfo.InvariantCulture);
}
