using System.Text.Json;

namespace Dokusystem.Kern;

// Demo-Daten aus daten/demo/bewohner.json (lesbare Datei im Repository, in die .exe eingebaut).
// Alle Angaben dort sind ausgedacht. Sie werden beim ersten Start geladen und lassen sich
// über „Demo-Daten zurücksetzen“ wiederherstellen.
public static class Demodaten
{
    private const string Ressource = "Dokusystem.Kern.Demo.bewohner.json";

    private sealed class Datei
    {
        public List<BewohnerEingabe> Bewohner { get; set; } = [];
    }

    public static List<Bewohner> Bewohner()
    {
        using var strom = typeof(Demodaten).Assembly.GetManifestResourceStream(Ressource)
            ?? throw new InvalidOperationException("Demo-Daten fehlen im Programm.");
        var datei = JsonSerializer.Deserialize<Datei>(strom, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Demo-Daten sind leer.");
        return datei.Bewohner.Select(e => BewohnerRegeln.AusEingabe(e)).ToList();
    }
}
