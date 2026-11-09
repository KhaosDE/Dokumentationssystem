namespace DokuLine.Kern;

// Demo-Vitalwerte (Bauplan 36): ausgedachte Werte über vier Monate mit leichten Schwankungen; bei einigen Bewohnern
// Messaufträge und angeordnete Bereiche, ein Wert außerhalb des angeordneten Bereichs. Alles ausgedacht.
public static partial class Demodaten
{
    private static readonly string[] VitalKuerzel = ["EF", "CD", "AB"];

    // Schwankung ohne Zufall (gleiche Demo bei jedem Zurücksetzen): −1 … +1.
    private static double Welle(int i, int tag, int art) => Math.Sin(i * 1.7 + tag * 0.45 + art * 2.3) * 0.6 + Math.Sin(tag * 0.13 + i) * 0.4;

    public static IEnumerable<(long BewohnerId, DateTime Zeit, string Art, double Wert, double? Wert2, string Bemerkung, string Kuerzel)> Vitalwerte(
        IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        for (int i = 0; i < bewohner.Count; i++)
        {
            long id = bewohner[i].Id!.Value;
            if (i % 9 == 8) continue;   // noch keine Werte
            double gewicht = 62 + i * 3.1 % 35, sys = 118 + i * 2 % 22, dia = 74 + i % 12, puls = 66 + i * 3 % 20;
            for (int woche = 16; woche >= 0; woche--)
            {
                var tag = heute.AddDays(-7 * woche - i % 5);
                if (tag > heute) continue;
                var zeit = tag.ToDateTime(new TimeOnly(8, 30 + i % 20));
                if (zeit > jetzt.AddMinutes(-30)) continue;   // die Werte unten sind die neuesten
                string k = VitalKuerzel[(i + woche) % 3];
                int t = 16 - woche;
                yield return (id, zeit, VitalRegeln.Blutdruck, Math.Round(sys + 8 * Welle(i, t, 1)), Math.Round(dia + 5 * Welle(i, t, 2)), "", k);
                yield return (id, zeit, VitalRegeln.Puls, Math.Round(puls + 7 * Welle(i, t, 3)), null, "", k);
                if (woche % 4 == 0) yield return (id, zeit, VitalRegeln.Gewicht, Math.Round(gewicht + 1.2 * Welle(i, t, 4), 1), null, "", k);
                if (woche % 3 == 1) yield return (id, zeit.AddHours(10), VitalRegeln.Temperatur, Math.Round(36.6 + 0.3 * Welle(i, t, 5), 1), null, "", k);
                if (i % 4 == 0)
                    yield return (id, zeit.AddMinutes(-60), VitalRegeln.Blutzucker, Math.Round(108 + 18 * Welle(i, t, 6)), null, "nüchtern", k);
                if (woche % 8 == 2) yield return (id, zeit, VitalRegeln.Saettigung, Math.Round(96.5 + 1.5 * Welle(i, t, 7)), null, "", k);
            }
            // Ein Wert außerhalb des angeordneten Bereichs (siehe Grenzen) und einer über dem allgemeinen Richtwert.
            if (i == 0) yield return (id, jetzt.AddMinutes(-10), VitalRegeln.Blutdruck, 162, 98, "nach dem Treppensteigen", "EF");
            if (i == 2) yield return (id, jetzt.AddMinutes(-15), VitalRegeln.Temperatur, 37.8, null, "", "CD");
        }
    }

    public static IEnumerable<(long BewohnerId, string Art, double? Von, double? Bis, double? Von2, double? Bis2)> VitalGrenzen(IReadOnlyList<Bewohner> bewohner)
    {
        if (bewohner.Count > 0) yield return (bewohner[0].Id!.Value, VitalRegeln.Blutdruck, 100, 150, 60, 95);
        if (bewohner.Count > 4) yield return (bewohner[4].Id!.Value, VitalRegeln.Blutzucker, 80, 180, null, null);
    }

    public static IEnumerable<(long BewohnerId, string Art, Messrhythmus Rhythmus, int Tag, Schicht Schicht)> Messauftraege(IReadOnlyList<Bewohner> bewohner)
    {
        for (int i = 0; i < bewohner.Count; i++)
        {
            long id = bewohner[i].Id!.Value;
            if (i % 3 == 0) yield return (id, VitalRegeln.Gewicht, Messrhythmus.Monatlich, 1, Schicht.Frueh);
            if (i % 5 == 0) yield return (id, VitalRegeln.Blutdruck, Messrhythmus.Woechentlich, (int)DayOfWeek.Monday, Schicht.Frueh);
            if (i == 4) yield return (id, VitalRegeln.Blutzucker, Messrhythmus.Taeglich, 0, Schicht.Frueh);
        }
    }
}
