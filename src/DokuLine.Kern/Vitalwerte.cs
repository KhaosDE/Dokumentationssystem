using System.Globalization;
using System.Text.Json;

namespace DokuLine.Kern;

// Vitalwerte (Bauplan 36): erfassen, als Tabelle und Verlauf ansehen, drucken. Grenzen nur nach ärztlicher Anordnung
// („außerhalb des angeordneten Bereichs“); ohne Anordnung ein leiser Hinweis auf allgemeine Richtwerte. Werte werden nie
// überschrieben – eine Korrektur ist ein neuer Eintrag mit Grund, der den alten ablöst.
public sealed record Vitalwert(long Id, long BewohnerId, DateTime Zeit, string Art, double Wert, double? Wert2, string Bemerkung, string Kuerzel,
    DateTime Erfasst, long? Vorgaenger = null, string Grund = "");

// Angeordneter Bereich je Wert (beim Blutdruck: Wert = systolisch, Wert2 = diastolisch). Ohne Von und Bis: aufgehoben.
public sealed record VitalGrenze(long Id, long BewohnerId, string Art, double? Von, double? Bis, double? Von2, double? Bis2, long? ArztId,
    DateOnly AngeordnetAm, string Kuerzel, DateTime Erfasst)
{
    public bool Gilt => Von is not null || Bis is not null || Von2 is not null || Bis2 is not null;
}

public enum Messrhythmus { Taeglich, Woechentlich, Monatlich }

// Messauftrag: erscheint als Aufgabe in der gewählten Schicht (wöchentlich: Wochentag 0–6 ab Sonntag; monatlich: Tag 1–28).
public sealed record Messauftrag(long Id, long BewohnerId, string Art, Messrhythmus Rhythmus, int Tag, Schicht Schicht, DateOnly Ab, DateOnly? Beendet,
    string Kuerzel, DateTime Erfasst);

// Allgemeine Lehrbuchwerte für Erwachsene (aus dem Chat, nicht ärztlich geprüft); in den Einstellungen änderbar.
public sealed record VitalRichtwerte
{
    public double BlutdruckSysHoch { get; init; } = 140;
    public double BlutdruckDiaHoch { get; init; } = 90;
    public double BlutdruckSysTief { get; init; } = 90;
    public double BlutdruckDiaTief { get; init; } = 60;
    public double PulsTief { get; init; } = 60;
    public double PulsHoch { get; init; } = 100;
    public double TemperaturTief { get; init; } = 36.0;
    public double TemperaturErhoeht { get; init; } = 37.5;
    public double TemperaturFieber { get; init; } = 38.0;
    public double BlutzuckerTief { get; init; } = 70;           // mg/dl
    public double BlutzuckerNuechternHoch { get; init; } = 100;
    public double BlutzuckerHoch { get; init; } = 140;
    public double SaettigungTief { get; init; } = 94;
    public double BmiTief { get; init; } = 18.5;
    public double BmiUebergewicht { get; init; } = 25;
    public double BmiAdipositas { get; init; } = 30;
    public string BlutzuckerEinheit { get; init; } = VitalRegeln.MgDl;
}

// Hinweis zu einem Wert: angeordnet (Gefahr-Farbe) oder allgemeiner Richtwert (Achtung-Farbe, leise).
public sealed record VitalHinweis(string Text, bool Angeordnet);

public static class VitalRegeln
{
    public const string Blutdruck = "Blutdruck", Puls = "Puls", Temperatur = "Temperatur", Gewicht = "Gewicht", Blutzucker = "Blutzucker",
        Saettigung = "Sauerstoffsättigung", Bmi = "BMI";
    public static readonly string[] Feste = [Blutdruck, Puls, Temperatur, Gewicht, Blutzucker, Saettigung];
    public const string MgDl = "mg/dl", MmolL = "mmol/l";
    public const double MmolFaktor = 18.0;
    public const string Ausserhalb = "außerhalb des angeordneten Bereichs";
    public const string RichtwertTooltip = "Allgemeiner Richtwert für Erwachsene, kein ärztlicher Grenzwert. Bei Unsicherheit Rücksprache halten.";
    public const string RichtwertVermerk = "Allgemeine Lehrbuchwerte, nicht ärztlich geprüft. Eine ärztliche Anordnung geht immer vor.";
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string Einheit(string art, VitalRichtwerte r) => art switch
    {
        Blutdruck => "mmHg",
        Puls => "/min",
        Temperatur => "°C",
        Gewicht => "kg",
        Blutzucker => r.BlutzuckerEinheit,
        Saettigung => "%",
        _ => "",
    };

    // Blutzucker wird in mg/dl gespeichert; angezeigt und eingegeben in der Einheit aus den Einstellungen.
    public static double Anzeige(string art, double wert, VitalRichtwerte r) =>
        art == Blutzucker && r.BlutzuckerEinheit == MmolL ? Math.Round(wert / MmolFaktor, 1) : wert;

    public static double Speicherwert(string art, double eingabe, VitalRichtwerte r) =>
        art == Blutzucker && r.BlutzuckerEinheit == MmolL ? Math.Round(eingabe * MmolFaktor) : eingabe;

    public static string Zahl(double wert) => wert.ToString(wert == Math.Floor(wert) ? "0" : "0.0#", De);

    public static string WertText(Vitalwert w, VitalRichtwerte r)
    {
        string einheit = Einheit(w.Art, r);
        string zahl = w.Art == Blutdruck && w.Wert2 is double d ? $"{Zahl(w.Wert)}/{Zahl(d)}" : Zahl(Anzeige(w.Art, w.Wert, r));
        return einheit.Length > 0 ? $"{zahl} {einheit}" : zahl;
    }

    // „72,5“ oder „72.5“; leer → null.
    public static double? Lesen(string text, string name)
    {
        text = text.Trim().Replace(',', '.');
        if (text.Length == 0) return null;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double wert))
            throw new Eingabefehler([$"{name}: bitte eine Zahl eintragen."]);
        return wert;
    }

    // Grobe Plausibilität (Tippfehler), keine medizinische Bewertung.
    public static string? Unplausibel(string art, double wert, double? wert2) => art switch
    {
        Blutdruck when wert is < 40 or > 300 || wert2 is < 20 or > 200 => "Blutdruck: bitte prüfen (z. B. 128/84).",
        Blutdruck when wert2 is null => "Blutdruck: bitte systolisch und diastolisch eintragen.",
        Puls when wert is < 20 or > 250 => "Puls: bitte prüfen.",
        Temperatur when wert is < 30 or > 45 => "Temperatur: bitte prüfen (z. B. 36,8).",
        Gewicht when wert is < 20 or > 350 => "Gewicht: bitte prüfen (kg).",
        Blutzucker when wert is < 10 or > 1000 => "Blutzucker: bitte prüfen.",
        Saettigung when wert is < 50 or > 100 => "Sauerstoffsättigung: bitte prüfen (%).",
        _ => null,
    };

    // Nur die gültigen Werte: was durch eine Korrektur abgelöst wurde, fällt heraus.
    public static List<Vitalwert> Gueltige(IEnumerable<Vitalwert> alle)
    {
        var liste = alle.ToList();
        var abgeloest = liste.Where(w => w.Vorgaenger is not null).Select(w => w.Vorgaenger!.Value).ToHashSet();
        return [.. liste.Where(w => !abgeloest.Contains(w.Id)).OrderByDescending(w => w.Zeit).ThenByDescending(w => w.Id)];
    }

    // Was angeordnet ist, geht vor; ohne Anordnung der allgemeine Richtwert.
    public static VitalHinweis? Bewerten(Vitalwert w, VitalGrenze? grenze, VitalRichtwerte r)
    {
        if (grenze is { Gilt: true } g)
            return Ausser(w.Wert, g.Von, g.Bis) || (w.Wert2 is double d && Ausser(d, g.Von2, g.Bis2)) ? new VitalHinweis(Ausserhalb, true) : null;
        static bool Ausser(double x, double? von, double? bis) => (von is double v && x < v) || (bis is double b && x > b);
        const string Ueber = "über dem allgemeinen Richtwert", Unter = "unter dem allgemeinen Richtwert";
        string? text = w.Art switch
        {
            Blutdruck when w.Wert >= r.BlutdruckSysHoch || w.Wert2 >= r.BlutdruckDiaHoch => Ueber,
            Blutdruck when w.Wert < r.BlutdruckSysTief || w.Wert2 < r.BlutdruckDiaTief => Unter,
            Puls when w.Wert > r.PulsHoch => Ueber,
            Puls when w.Wert < r.PulsTief => Unter,
            Temperatur when w.Wert >= r.TemperaturFieber => $"Fieber – {Ueber}",
            Temperatur when w.Wert >= r.TemperaturErhoeht => $"erhöht – {Ueber}",
            Temperatur when w.Wert < r.TemperaturTief => Unter,
            Blutzucker when w.Wert < r.BlutzuckerTief => Unter,
            Blutzucker when (w.Bemerkung.Contains("nüchtern", StringComparison.OrdinalIgnoreCase) ? w.Wert >= r.BlutzuckerNuechternHoch : w.Wert >= r.BlutzuckerHoch) => Ueber,
            Saettigung when w.Wert < r.SaettigungTief => Unter,
            _ => null,
        };
        return text is null ? null : new VitalHinweis(text, false);
    }

    // BMI aus Gewicht und Größe der Personenbeschreibung („1,78 m“, „178 cm“, „178“).
    public static double? Groesse(string text)
    {
        string t = text.ToLowerInvariant().Replace("cm", "").Replace("m", "").Trim().Replace(',', '.');
        if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double g)) return null;
        if (g > 3) g /= 100;   // Zentimeter
        return g is >= 1.0 and <= 2.5 ? g : null;
    }

    public static double? BmiWert(double gewicht, string groesse) =>
        Groesse(groesse) is double g ? Math.Round(gewicht / (g * g), 1) : null;

    public static VitalHinweis? BmiHinweis(double bmi, VitalRichtwerte r) =>
        bmi >= r.BmiAdipositas ? new("Adipositas – über dem allgemeinen Richtwert", false)
        : bmi >= r.BmiUebergewicht ? new("Übergewicht – über dem allgemeinen Richtwert", false)
        : bmi < r.BmiTief ? new("unter dem allgemeinen Richtwert", false)
        : null;

    // Der jeweils neueste Wert je Art (für Visite und Überleitungsbogen), mit Datum.
    public static List<string> Letzte(IEnumerable<Vitalwert> alle, VitalRichtwerte r, string? groesse)
    {
        var gueltig = Gueltige(alle);
        var zeilen = gueltig.GroupBy(w => w.Art).OrderBy(g => Array.IndexOf(Feste, g.Key) is int i && i >= 0 ? i : 99).ThenBy(g => g.Key)
            .Select(g => g.First()).Select(w => $"{w.Art}: {WertText(w, r)} ({w.Zeit.ToString("dd.MM.yyyy HH:mm", De)})").ToList();
        if (gueltig.FirstOrDefault(w => w.Art == Gewicht) is Vitalwert gw && groesse is not null && BmiWert(gw.Wert, groesse) is double bmi)
            zeilen.Add($"{Bmi}: {Zahl(bmi)} (aus Gewicht und Größe)");
        return zeilen;
    }

    // Gilt ein Messauftrag in dieser Schicht?
    public static bool Faellig(Messauftrag m, SchichtTermin s) =>
        m.Schicht == s.Schicht && s.Tag >= m.Ab && (m.Beendet is not DateOnly b || s.Tag <= b) && m.Rhythmus switch
        {
            Messrhythmus.Taeglich => true,
            Messrhythmus.Woechentlich => (int)s.Tag.DayOfWeek == m.Tag,
            _ => s.Tag.Day == m.Tag,
        };

    public static string RhythmusText(Messauftrag m) => m.Rhythmus switch
    {
        Messrhythmus.Taeglich => "täglich",
        Messrhythmus.Woechentlich => $"wöchentlich ({De.DateTimeFormat.GetDayName((DayOfWeek)m.Tag)})",
        _ => $"monatlich (am {m.Tag}.)",
    };

    public static string AuftragText(Messauftrag m) => $"{m.Art} messen";

    public static string? AuftragPruefen(Messauftrag m) => m.Rhythmus switch
    {
        Messrhythmus.Woechentlich when m.Tag is < 0 or > 6 => "Wochentag fehlt.",
        Messrhythmus.Monatlich when m.Tag is < 1 or > 28 => "Tag im Monat: 1 bis 28.",
        _ => null,
    };

    // Gespeichert wie die Werte (Blutzucker in mg/dl); angezeigt und eingegeben in der Einheit der Einstellungen.
    public static VitalGrenze GrenzeAnzeige(VitalGrenze g, VitalRichtwerte r) => g with
    {
        Von = g.Von is double v ? Anzeige(g.Art, v, r) : null,
        Bis = g.Bis is double b ? Anzeige(g.Art, b, r) : null,
    };

    public static string GrenzeText(VitalGrenze gespeichert, Func<long?, string> arzt, VitalRichtwerte r)
    {
        var g = GrenzeAnzeige(gespeichert, r);
        string Bereich(double? von, double? bis) => von is null ? $"bis {Zahl(bis!.Value)}" : bis is null ? $"ab {Zahl(von.Value)}" : $"{Zahl(von.Value)}–{Zahl(bis.Value)}";
        string bereich = g.Art == Blutdruck
            ? string.Join(", ", new[] { g.Von is null && g.Bis is null ? "" : $"systolisch {Bereich(g.Von, g.Bis)}", g.Von2 is null && g.Bis2 is null ? "" : $"diastolisch {Bereich(g.Von2, g.Bis2)}" }.Where(x => x.Length > 0))
            : Bereich(g.Von, g.Bis);
        return $"{g.Art}: {bereich} (angeordnet am {Datum.Text(g.AngeordnetAm)}{(g.ArztId is null ? "" : $" von {arzt(g.ArztId)}")})";
    }

    public static VitalRichtwerte RichtwerteAus(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<VitalRichtwerte>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }
}
