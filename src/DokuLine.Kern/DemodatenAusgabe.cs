namespace DokuLine.Kern;

// Demo der Medikamentenausgabe (Bauplan 42): fast alles ausgegeben, gelegentlich nicht abgeholt oder abgelehnt – fest aus
// Bewohner, Tag und Einnahmezeit berechnet (gleiche Demo bei jedem Zurücksetzen). Alles ausgedacht.
public static partial class Demodaten
{
    private static readonly string[] AusgabeKuerzelTag = ["AB", "CD", "EF"], AusgabeKuerzelAbend = ["GH", "MN"], AusgabeKuerzelNacht = ["KL"];

    public static (AusgabeStatus Status, string Kuerzel) Ausgabe(long bewohnerId, DateOnly tag, Einnahmezeit zeit)
    {
        long k = bewohnerId * 13 + tag.DayNumber * 7 + (int)zeit;
        var status = k % 29 == 0 ? AusgabeStatus.NichtAbgeholt : k % 37 == 0 ? AusgabeStatus.Abgelehnt : AusgabeStatus.Ausgegeben;
        var liste = zeit switch { Einnahmezeit.Morgens or Einnahmezeit.Mittags => AusgabeKuerzelTag, Einnahmezeit.Abends => AusgabeKuerzelAbend, _ => AusgabeKuerzelNacht };
        return (status, liste[tag.DayNumber % liste.Length]);
    }
}
