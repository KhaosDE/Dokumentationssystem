namespace DokuLine.Kern;

// Abwesenheit in der Dokumentation (Bauplan 50): über der Berichtsliste die laufende Abwesenheit, in der Tagesliste ein
// grauer Balken über den Zeitraum, Beginn und Ende als je ein kurzer Eintrag. Quellen bleiben, wo sie sind: Ausgangsliste
// (Modul), Heimfahrt im Kalender, Fahrten und „in der Klinik seit …“ aus dem Überleitungsbogen.
public sealed record AbwesenheitsZeitraum(DateOnly Von, DateOnly Bis, string Grund);

public static class AbwesenheitRegeln
{
    public static string ZeileText(string grund) => grund.StartsWith("abwesend", StringComparison.OrdinalIgnoreCase) ? grund : $"abwesend: {grund}";

    public static string BalkenText(AbwesenheitsZeitraum z) =>
        (z.Von == z.Bis ? $"{z.Von:dd.MM.}" : $"{z.Von:dd.MM.}–{z.Bis:dd.MM.}") + $" abwesend: {z.Grund}";

    public static string BeginnText(DateTime ab, string grund) => $"abwesend ab {ab:dd.MM. HH:mm} ({grund})";

    public static string EndeText(DateTime zurueck) => $"zurück am {zurueck:dd.MM. HH:mm}";

    public static string FormularText(string grund) => $"Bewohner ist {ZeileText(grund)} – der Bericht kann trotzdem geschrieben werden.";

    // Heimfahrten aus dem Kalender als Zeiträume (abgesagte nicht), auf den Zeitraum zugeschnitten.
    public static List<AbwesenheitsZeitraum> Zeitraeume(IEnumerable<KalenderTermin> kalender, long bewohnerId, DateOnly von, DateOnly bis) =>
        [.. kalender.Select(k => k.Aktuell)
            .Where(f => !f.Abgesagt && f.Art == KalenderArt.Heimfahrt && f.Bewohner.Contains(bewohnerId))
            .Select(f => (Von: DateOnly.FromDateTime(f.Beginn), Bis: DateOnly.FromDateTime(f.Ende.AddMinutes(-1))))
            .Where(z => z.Bis >= von && z.Von <= bis)
            .Select(z => new AbwesenheitsZeitraum(z.Von < von ? von : z.Von, z.Bis > bis ? bis : z.Bis, "Heimfahrt"))
            .OrderBy(z => z.Von)];
}
