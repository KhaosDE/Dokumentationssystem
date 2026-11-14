namespace DokuLine.Kern;

// Ausgedachte ehemalige Bewohner für das Archiv (Bauplan 46). Einer ist so lange entlassen, dass die Demo-Frist abgelaufen ist.
public static partial class Demodaten
{
    public static List<(Bewohner Bewohner, DateOnly Entlassen, EntlassGrund Grund, List<(int TageVorher, string Text)> Berichte)> Ehemalige(DateOnly heute) =>
    [
        (new Bewohner
        {
            Vorname = "Ulrich", Nachname = "Ulme", Zimmer = "104", Geburtsdatum = new(1958, 4, 2), Aufnahmedatum = heute.AddYears(-16), Wohnbereich = "R1",
            Allergie = AllergieZustand.KeineBekannt,
        }, heute.AddYears(-12), EntlassGrund.Verlegung,
        [(30, "Besprechung zur Verlegung in eine Einrichtung näher an der Familie."), (2, "Hat sich von den Mitbewohnern verabschiedet.")]),
        (new Bewohner
        {
            Vorname = "Erika", Nachname = "Efeu", Zimmer = "206", Geburtsdatum = new(1971, 9, 18), Aufnahmedatum = heute.AddYears(-4), Wohnbereich = "R2",
            Allergie = AllergieZustand.Eingetragen, Allergien = "Pflaster (ausgedacht)",
        }, heute.AddMonths(-8), EntlassGrund.Auszug,
        [(45, "Wohnungsbesichtigung mit der Betreuerin."), (7, "Umzugskartons gepackt, freut sich auf die eigene Wohnung.")]),
        (new Bewohner
        {
            Vorname = "Karl", Nachname = "Kamille", Zimmer = "312", Geburtsdatum = new(1949, 1, 27), Aufnahmedatum = heute.AddYears(-9), Wohnbereich = "R3",
            Allergie = AllergieZustand.NichtErfasst,
        }, heute.AddYears(-2), EntlassGrund.Verstorben,
        [(20, "War ruhig und hat viel geschlafen."), (1, "Im Krankenhaus.")]),
    ];
}
