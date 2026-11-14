namespace DokuLine.Kern;

// Der Bereich, der in der Seitenleiste gewählt ist, gilt für alle Bereiche des Programms.
// Fachregel: IDEEN.md „Wohnbereich einmal in der Übersicht wählen, gilt überall“ und „Wohnbereich als Auswahlliste in der
// Seitenleiste“.
public static class BereichswahlRegeln
{
    public const string Alle = "Alle";

    // Was jemand wählen kann: die Wohnbereiche, „Therapie“, „Alle“ und „QM“ (Qualitätsmanagement) – je nach Recht.
    public static List<string> Knoepfe(IEnumerable<string> wohnbereiche, Func<string, bool> darf)
    {
        var liste = new List<string>();
        if (darf(Funktionen.BereicheWohnbereiche)) liste.AddRange(wohnbereiche);
        if (darf(Funktionen.BereichTherapie)) liste.Add(TherapieRegeln.AnbieterTherapie);
        if (darf(Funktionen.BereicheAlle)) liste.Add(Alle);
        if (darf(Funktionen.Qualitaet)) liste.Add(QualitaetRegeln.BereichQm);
        return liste;
    }

    // „QM“ ist kein Wohnbereich: Es zeigt wie „Alle“ das ganze Haus, beginnt aber mit der Seite „Qualität“.
    public static bool GanzesHaus(string gewaehlt) => gewaehlt == Alle || gewaehlt == QualitaetRegeln.BereichQm;

    // Vorgabe beim Start: der Bereich des Rechners, wenn man ihn wählen darf, sonst der erste erlaubte.
    public static string Start(string rechner, IReadOnlyList<string> knoepfe) =>
        knoepfe.Count == 0 || knoepfe.Contains(rechner) ? rechner : knoepfe[0];

    // Bewohner des Bereichs; „Alle“ und Bereiche ohne Bewohner (z. B. Therapie) zeigen alle.
    public static List<Bewohner> Bewohner(IReadOnlyList<Bewohner> alle, string gewaehlt) =>
        GanzesHaus(gewaehlt) || !alle.Any(b => b.Wohnbereich == gewaehlt) ? [.. alle] : [.. alle.Where(b => b.Wohnbereich == gewaehlt)];

    // Gehört etwas mit diesem Bereich in den gewählten Bereich (Aufgaben, Übergaben)?
    public static bool Passt(string bereich, string gewaehlt) => gewaehlt.Length == 0 || GanzesHaus(gewaehlt) || bereich == gewaehlt;

    // Eingeklappte Seitenleiste: kurze Namen ganz („R2“, „Alle“), längere mit den ersten drei Buchstaben.
    public static string Kuerzel(string bereich) => bereich.Length <= 4 ? bereich : bereich[..3];

    // „eingesprungen“: ein anderer Bereich als der des Rechners („Alle“ ist kein Bereich).
    public static bool Eingesprungen(string gewaehlt, string rechner) =>
        gewaehlt.Length > 0 && !GanzesHaus(gewaehlt) && gewaehlt != rechner;
}
