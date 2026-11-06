using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dokusystem.Kern;

// Einstellungen als lesbare Datei einstellungen.json im Datenordner.
// Bisher nur eine: auto_update (beim Start nach einer neuen Version fragen, Vorgabe: ja).
// Eine kaputte oder fremde Datei führt nie zum Absturz; dann gelten die Vorgaben.
// Dieselbe Datei hat schon die Python-Version (bis 0.2.0) benutzt.
public sealed record EinstellungsWerte(bool AutoUpdate = true);

public static class Einstellungen
{
    public const string Datei = "einstellungen.json";

    public static EinstellungsWerte Laden()
    {
        try
        {
            var knoten = JsonNode.Parse(File.ReadAllText(Pfade.ImDatenordner(Datei)));
            if (knoten?["auto_update"] is JsonValue wert && wert.TryGetValue(out bool an))
                return new EinstellungsWerte(an);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            // fehlt oder unlesbar: Vorgaben
        }
        return new EinstellungsWerte();
    }

    public static void Speichern(EinstellungsWerte werte)
    {
        string ziel = Pfade.ImDatenordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        string zwischen = ziel + ".neu";
        var inhalt = new JsonObject { ["auto_update"] = werte.AutoUpdate };
        File.WriteAllText(zwischen, inhalt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.Move(zwischen, ziel, overwrite: true);   // erst ganz schreiben, dann tauschen
    }
}
