using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Stilprüfung als Hinweisliste, nie automatisch ändern (docs/heb-wissen.md, Abschnitt „Regeln,
// die das Programm prüfen kann“). Genutzt von der Monatsreflexion und später vom HEB-Bogen.
// Regeln, die nur für bestimmte Felder des HEB-Bogens gelten (Ich-Form bei Zielen), kommen mit
// dem HEB-Bogen dazu.
public sealed record StilHinweis(string Regel, string Fundstelle)
{
    public string Text => $"{Regel}: „{Fundstelle}“";
}

public static partial class Stilpruefung
{
    public static readonly string[] Woerter =
    [
        "diskontinuierlich", "differenziert", "vielschichtig", "maßgeblich", "unterstreicht", "verdeutlicht",
        "facettenreich", "entscheidend", "umfassend", "zeugt von", "spannend", "essenziell", "ganzheitlich",
    ];

    public static readonly string[] Floskeln =
    [
        "Zusammenfassend lässt sich sagen", "Insgesamt zeigt sich", "Abschließend ist festzuhalten",
        "Es ist wichtig zu erwähnen", "Hervorzuheben ist", "Bemerkenswert ist",
    ];

    [GeneratedRegex(@"\b\d{1,2}\.\d{1,2}\.(\d{2,4})?|\b\d{4}-\d{2}-\d{2}\b")]
    private static partial Regex Datum();

    [GeneratedRegex(@"\bnicht\b[^.!?;]*,\s*sondern\b", RegexOptions.IgnoreCase)]
    private static partial Regex NichtSondern();

    [GeneratedRegex(@"\w[*:_]innen\b|\w\*in\b")]
    private static partial Regex Gendersternchen();

    [GeneratedRegex(@"\bMitarbeitende\w*")]
    private static partial Regex Mitarbeitende();

    // „Hr. M“ ohne Punkt nach dem abgekürzten Nachnamen (richtig: „Hr. M.“).
    [GeneratedRegex(@"\b(Hr|Fr)\.\s[A-ZÄÖÜ](?![.\p{L}])")]
    private static partial Regex NachnameOhnePunkt();

    [GeneratedRegex(@"\s[–—-]\s")]
    private static partial Regex Gedankenstrich();

    [GeneratedRegex(@"\r?\n\s*\r?\n")]
    private static partial Regex Absatz();

    [GeneratedRegex(@"\b(ich|mein|meine|meinen|meinem|meiner|mir|mich)\b", RegexOptions.IgnoreCase)]
    private static partial Regex IchForm();

    // Mit Feldart: Rahmenziele in der 3. Person, Leitziel und Nahziele in Ich-Form.
    public static List<StilHinweis> Pruefen(string text, FeldArt art)
    {
        var h = Pruefen(text);
        if (string.IsNullOrWhiteSpace(text)) return h;
        bool ich = IchForm().IsMatch(text);
        if (art == FeldArt.Rahmenziel && ich) h.Add(new("Rahmenziel in der 3. Person schreiben (nicht in Ich-Form)", IchForm().Match(text).Value));
        if (art == FeldArt.IchZiel && !ich) h.Add(new("Leitziel und Nahziele in Ich-Form schreiben", Kurz(text.Trim())));
        return h;
    }

    public static List<StilHinweis> Pruefen(string text)
    {
        var h = new List<StilHinweis>();
        if (string.IsNullOrWhiteSpace(text)) return h;
        if (text.Contains(';')) h.Add(new("Semikolon – lieber zwei Sätze", ";"));
        foreach (Match m in Datum().Matches(text))
            h.Add(new("Konkretes Datum – im Text lieber Häufigkeit („zuletzt“, „wöchentlich“)", m.Value));
        foreach (var w in Woerter)
        {
            var m = Regex.Match(text, $@"\b{Regex.Escape(w)}\w*", RegexOptions.IgnoreCase);
            if (m.Success) h.Add(new("Wort, das im HEB nicht verwendet wird", m.Value));
        }
        foreach (var f in Floskeln.Where(f => text.Contains(f, StringComparison.OrdinalIgnoreCase)))
            h.Add(new("Floskel", f));
        if (NichtSondern().Match(text) is { Success: true } ns) h.Add(new("Konstruktion „nicht …, sondern …“", Kurz(ns.Value)));
        if (Mitarbeitende().Match(text) is { Success: true } ma) h.Add(new("„das Personal“ statt „Mitarbeitende“", ma.Value));
        if (Gendersternchen().Match(text) is { Success: true } g) h.Add(new("Gendersternchen – im HEB nicht verwendet", g.Value));
        if (NachnameOhnePunkt().Match(text) is { Success: true } n) h.Add(new("Abgekürzter Nachname mit Punkt schreiben („Hr. M.“)", n.Value));
        foreach (string absatz in Absatz().Split(text))
            if (Gedankenstrich().Matches(absatz).Count > 1)
            {
                h.Add(new("Mehr als ein Gedankenstrich in einem Absatz", Kurz(absatz.Trim())));
                break;
            }
        return h;
    }

    private static string Kurz(string s) => s.Length <= 40 ? s : s[..39] + "…";
}
