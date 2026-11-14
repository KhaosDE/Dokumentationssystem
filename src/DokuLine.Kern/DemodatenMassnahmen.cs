namespace DokuLine.Kern;

// Demo-Maßnahmen (Bauplan 39): je Bewohner zwei bis fünf über die Lebensbereiche, seit drei Wochen, mit Bestätigungen
// (einige abgelehnt oder nicht möglich). Ohne Zufall: dieselbe Demo bei jedem Zurücksetzen. Alles ausgedacht.
public static partial class Demodaten
{
    private static readonly MassnahmeFassung[] MassnahmenVorlagen =
    [
        new() { Titel = "Zimmerhygiene", Beschreibung = "Gemeinsam Bett beziehen, Boden wischen.", Lebensbereich = 2, Art = "Anleitung",
                Rhythmus = MassnahmeRhythmus.Wochentage, Wochentage = [DayOfWeek.Monday, DayOfWeek.Thursday], Schicht = Schicht.Spaet, Minuten = 30 },
        new() { Titel = "Tagesplan besprechen", Beschreibung = "Was steht heute an, was ist offen?", Lebensbereich = 4, Art = "Gespräch",
                Rhythmus = MassnahmeRhythmus.Taeglich, Schicht = Schicht.Frueh, Uhrzeit = new TimeOnly(8, 30), Minuten = 10 },
        new() { Titel = "Wäsche waschen", Beschreibung = "Waschmaschine selbst bedienen, nur begleiten.", Lebensbereich = 2, Art = "Begleitung",
                Rhythmus = MassnahmeRhythmus.AlleNWochen, Wochentage = [DayOfWeek.Wednesday], AlleWochen = 2, Schicht = Schicht.Frueh, Minuten = 45 },
        new() { Titel = "Einkauf begleiten", Beschreibung = "Einkaufszettel vorher schreiben.", Lebensbereich = 4, Art = "Begleitung",
                Rhythmus = MassnahmeRhythmus.Wochentage, Wochentage = [DayOfWeek.Friday], Schicht = Schicht.Spaet, Minuten = 60 },
        new() { Titel = "Telefonat mit der Familie vorbereiten", Beschreibung = "Was soll im Gespräch vorkommen?", Lebensbereich = 1, Art = "Gespräch",
                Rhythmus = MassnahmeRhythmus.Wochentage, Wochentage = [DayOfWeek.Sunday], Schicht = Schicht.Spaet, Minuten = 15 },
        new() { Titel = "Arbeitsweg üben", Beschreibung = "Busfahrt zur Werkstatt gemeinsam.", Lebensbereich = 3, Art = "Begleitung",
                Rhythmus = MassnahmeRhythmus.Wochentage, Wochentage = [DayOfWeek.Tuesday], Schicht = Schicht.Frueh, Uhrzeit = new TimeOnly(7, 15), Minuten = 40 },
        new() { Titel = "Wochengeld einteilen", Beschreibung = "Kassenbuch gemeinsam führen.", Lebensbereich = 2, Art = "Anleitung",
                Rhythmus = MassnahmeRhythmus.Monatlich, MonatsTag = 1, Schicht = Schicht.Frueh, Minuten = 20 },
        new() { Titel = "Frühwarnzeichen besprechen", Beschreibung = "Krisenplan durchgehen.", Lebensbereich = 5, Art = "Gespräch",
                Rhythmus = MassnahmeRhythmus.Wochentage, Wochentage = [DayOfWeek.Wednesday], Schicht = Schicht.Spaet, Minuten = 20 },
        new() { Titel = "Beruhigendes Gespräch", Beschreibung = "Bei Unruhe, ruhiger Ort.", Lebensbereich = 5, Art = "Gespräch",
                Rhythmus = MassnahmeRhythmus.BeiBedarf, Schicht = Schicht.Spaet, Minuten = 15 },
    ];

    private static readonly string[] MassnahmenKuerzelFrueh = ["AB", "CD", "EF"], MassnahmenKuerzelSpaet = ["GH", "MN", "CD"], MassnahmenKuerzelNacht = ["KL"];

    public static IEnumerable<(Bewohner Bewohner, MassnahmeFassung Fassung, int Nr)> Massnahmen(IReadOnlyList<Bewohner> bewohner, DateOnly heute)
    {
        for (int i = 0; i < bewohner.Count; i++)
        {
            int anzahl = 2 + i % 4;   // zwei bis fünf
            for (int n = 0; n < anzahl; n++)
            {
                var v = MassnahmenVorlagen[(i + n * 2) % MassnahmenVorlagen.Length];
                var ab = heute.AddDays(-21 - i % 7);
                yield return (bewohner[i], v with { GiltAb = ab, Kuerzel = "EF", Erfasst = ab.ToDateTime(new TimeOnly(10, 0)) }, n);
            }
        }
    }

    // Etwa jede siebte Bestätigung abgelehnt, jede elfte nicht möglich – fest aus Maßnahme und Tag berechnet.
    public static (MassnahmeErgebnis Ergebnis, string Grund) MassnahmeErgebnis(long massnahmeId, DateOnly tag)
    {
        long k = massnahmeId * 31 + tag.DayNumber;
        if (k % 7 == 0) return (Kern.MassnahmeErgebnis.Abgelehnt, "wollte heute nicht");
        if (k % 11 == 0) return (Kern.MassnahmeErgebnis.NichtMoeglich, "beim Arzt");
        return (Kern.MassnahmeErgebnis.Durchgefuehrt, "");
    }

    public static string MassnahmeKuerzel(Schicht s, DateOnly tag)
    {
        var liste = s switch { Schicht.Frueh => MassnahmenKuerzelFrueh, Schicht.Spaet => MassnahmenKuerzelSpaet, _ => MassnahmenKuerzelNacht };
        return liste[tag.DayNumber % liste.Length];
    }
}
