using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Dokusystem.Kern;

// Einstellungen als lesbare Datei einstellungen.json im Datenordner:
//   auto_update – beim Start nach einer neuen Version fragen (Vorgabe: ja)
//   kuerzel     – eigenes Kürzel, z. B. für Berichte (Vorgabe: leer)
//   logo        – Dateiname des eigenen Bildes für den Kopf im Ordner „logo“ (Vorgabe: keins)
//   beschluss_frist_tage – Startseite: „Beschluss endet bald“ ab so vielen Tagen vorher (Vorgabe: 90)
//   heb_frist_monate     – Startseite: „HEB fällig“, wenn der letzte Bogen so alt ist (Vorgabe: 12)
// Eine kaputte oder fremde Datei führt nie zum Absturz; dann gelten die Vorgaben.
// Dieselbe Datei hat schon die Python-Version (bis 0.2.0) benutzt.
public sealed record EinstellungsWerte(bool AutoUpdate = true, string Kuerzel = "", string? Logo = null, int BeschlussFristTage = 90, int HebFristMonate = 12);

public static partial class Einstellungen
{
    public const string Datei = "einstellungen.json";
    public const string LogoOrdner = "logo";
    public const int KuerzelLaenge = 6;

    [GeneratedRegex(@"^[\p{L}\d]*$")]
    private static partial Regex KuerzelMuster();

    public static EinstellungsWerte Laden()
    {
        var werte = new EinstellungsWerte();
        try
        {
            var knoten = JsonNode.Parse(File.ReadAllText(Pfade.ImDatenordner(Datei))) as JsonObject;
            if (knoten is null) return werte;
            if (knoten["auto_update"] is JsonValue a && a.TryGetValue(out bool an)) werte = werte with { AutoUpdate = an };
            if (knoten["kuerzel"] is JsonValue k && k.TryGetValue(out string? kuerzel) && KuerzelPruefen(kuerzel) is null)
                werte = werte with { Kuerzel = kuerzel.Trim() };
            if (knoten["logo"] is JsonValue l && l.TryGetValue(out string? logo) && !string.IsNullOrWhiteSpace(logo))
                werte = werte with { Logo = logo };
            if (knoten["beschluss_frist_tage"] is JsonValue f && f.TryGetValue(out int tage) && FristPruefen(tage) is null)
                werte = werte with { BeschlussFristTage = tage };
            if (knoten["heb_frist_monate"] is JsonValue h && h.TryGetValue(out int monate) && HebFristPruefen(monate) is null)
                werte = werte with { HebFristMonate = monate };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            // fehlt oder unlesbar: Vorgaben
        }
        return werte;
    }

    public static void Speichern(EinstellungsWerte werte)
    {
        if (KuerzelPruefen(werte.Kuerzel) is string fehler) throw new Eingabefehler([fehler]);
        if (FristPruefen(werte.BeschlussFristTage) is string fristFehler) throw new Eingabefehler([fristFehler]);
        if (HebFristPruefen(werte.HebFristMonate) is string hebFehler) throw new Eingabefehler([hebFehler]);
        string ziel = Pfade.ImDatenordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        string zwischen = ziel + ".neu";
        var inhalt = new JsonObject
        {
            ["auto_update"] = werte.AutoUpdate,
            ["kuerzel"] = werte.Kuerzel.Trim(),
            ["logo"] = werte.Logo,
            ["beschluss_frist_tage"] = werte.BeschlussFristTage,
            ["heb_frist_monate"] = werte.HebFristMonate,
        };
        File.WriteAllText(zwischen, inhalt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.Move(zwischen, ziel, overwrite: true);   // erst ganz schreiben, dann tauschen
    }

    // Kürzel: leer oder bis zu 6 Buchstaben/Ziffern. Rückgabe: Fehlertext oder null.
    public static string? KuerzelPruefen(string? kuerzel)
    {
        string k = (kuerzel ?? "").Trim();
        if (k.Length > KuerzelLaenge) return $"Kürzel: höchstens {KuerzelLaenge} Zeichen.";
        if (!KuerzelMuster().IsMatch(k)) return "Kürzel: nur Buchstaben und Ziffern.";
        return null;
    }

    public static string? FristPruefen(int tage) =>
        tage is >= 1 and <= 365 ? null : "Frist „Beschluss endet bald“: 1 bis 365 Tage.";

    public static string? HebFristPruefen(int monate) =>
        monate is >= 1 and <= 60 ? null : "Frist „HEB fällig“: 1 bis 60 Monate.";

    // Ändert einen Wert und speichert sofort.
    public static EinstellungsWerte Aendern(Func<EinstellungsWerte, EinstellungsWerte> aenderung)
    {
        var neu = aenderung(Laden());
        Speichern(neu);
        return neu;
    }

    // Eigenes Bild für den Kopf hinterlegen (Kopie im Datenordner, nie im Repository, Regel 7).
    // Ein vorher hinterlegtes Bild wird gelöscht.
    public static EinstellungsWerte LogoSetzen(string? quelle)
    {
        var alt = Laden();
        string? neu = quelle is null ? null : Bilder.Uebernehmen(quelle, LogoOrdner);
        var werte = Aendern(w => w with { Logo = neu });
        if (alt.Logo is not null && alt.Logo != neu) Bilder.Loeschen(alt.Logo, LogoOrdner);
        return werte;
    }

    public static string? LogoPfad() => Bilder.Pfad(Laden().Logo, LogoOrdner);
}
