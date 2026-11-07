using System.Globalization;
using System.Text.RegularExpressions;

namespace Dokusystem.Modul.Ausgangsliste;

// Texte für Karten und Liste (übernommen aus der Ausgangsliste, BewohnerKarte.cs, Klasse KartenText).
public static partial class KartenText
{
    private static readonly CultureInfo Deutsch = CultureInfo.GetCultureInfo("de-DE");

    [GeneratedRegex(@"\d{2}\.\d{2}\.\d{4}")]
    private static partial Regex DatumMuster();

    [GeneratedRegex(@"\((\d{2}\.\d{2}\.\d{4})\)")]
    private static partial Regex DatumInKlammern();

    // Stunden je Art: „2 Allein / 1 MBW“ → (2, „allein“), (1, „mit Bewohner“).
    public static List<(int Stunden, string Art)> Stundenteile(Platz platz)
    {
        var teile = new List<(int, string)>();
        foreach (string baustein in AusgangText.Bausteine(platz.Ausgang))
        {
            string[] woerter = baustein.Split(' ');
            if (woerter.Length >= 2 && int.TryParse(woerter[0], out int zahl))
            {
                string art = string.Join(' ', woerter.Skip(1));
                if (art.Equals("Allein", StringComparison.OrdinalIgnoreCase)) art = "allein";
                else if (art.Equals("MBW", StringComparison.OrdinalIgnoreCase)) art = "mit Bewohner";
                teile.Add((zahl, art));
            }
        }
        return teile;
    }

    // „2 Std allein · 1 Std mit Bewohner“
    public static string Aufteilung(Platz platz) =>
        string.Join(" · ", Stundenteile(platz).Select(t => $"{t.Stunden} Std {t.Art}"));

    // Die Info-Bausteine ohne Klammern.
    public static string Info(Platz platz) =>
        string.Join(" · ", AusgangText.Bausteine(platz.Ausgang).Where(AusgangText.IstInfo).Select(b => b.Trim('(', ')')));

    // Feste Texte als kurze Hinweise.
    public static IEnumerable<string> Hinweise(Platz platz)
    {
        var bausteine = AusgangText.Bausteine(platz.Ausgang);
        if (bausteine.Contains(AusgangText.TextPersonalBegleitung)) yield return "nur mit Personal";
        if (bausteine.Contains(AusgangText.TextWieOffenGefuehrt)) yield return "wie offen geführt";
    }

    // Der Grund ohne die Wiederholung des Titels und mit Datum ohne Klammern:
    // „Ausgangsbeschränkung bis 11.11.2026“ → „bis 11.11.2026“.
    public static string GrundOhneTitel(Platz platz)
    {
        string grund = DatumInKlammern().Replace(platz.StatusGrund.Trim(), "$1");
        const string titel = "Ausgangsbeschränkung";
        if (platz.Status == AnwesenheitsStatus.Ausgangsbeschraenkung && grund.StartsWith(titel, StringComparison.OrdinalIgnoreCase))
            grund = grund[titel.Length..].Trim();
        return grund;
    }

    // Die Frist zu einem Grund, wenn er ein Datum enthält:
    //   „… bis 11.11.2026“   → „noch 7 Tage“ (abgelaufen: „seit 2 Tagen abgelaufen“)
    //   „… seit (02.11.2026)“ → „seit 2 Tagen“
    // Ohne Datum: leer.
    public static string Frist(string grund, DateTime heute, out bool abgelaufen)
    {
        abgelaufen = false;
        var treffer = DatumMuster().Match(grund);
        if (!treffer.Success) return "";
        if (!DateTime.TryParseExact(treffer.Value, "dd.MM.yyyy", Deutsch, DateTimeStyles.None, out DateTime datum)) return "";

        string davor = grund[..treffer.Index].TrimEnd(' ', '(').ToLowerInvariant();
        int tage = (datum.Date - heute.Date).Days;
        if (davor.EndsWith("bis"))
        {
            if (tage > 1) return $"noch {tage} Tage";
            if (tage == 1) return "endet morgen";
            if (tage == 0) return "endet heute";
            abgelaufen = true;
            return tage == -1 ? "seit gestern abgelaufen" : $"seit {-tage} Tagen abgelaufen";
        }
        int seit = -tage;
        return seit switch
        {
            < 0 => "",
            0 => "seit heute",
            1 => "seit gestern",
            _ => $"seit {seit} Tagen",
        };
    }

    // Kurzfassung (Bildschirmleser, Akte).
    public static string HeuteKurz(Platz platz) => platz.Status switch
    {
        AnwesenheitsStatus.Abwesenheit => "abwesend: " + platz.StatusGrund,
        AnwesenheitsStatus.Ausgangsbeschraenkung => "Ausgangsbeschränkung: " + platz.StatusGrund,
        _ => platz.Stunden > 0 ? "Ausgang " + Aufteilung(platz) : "im Haus, kein Ausgang eingetragen",
    };
}
