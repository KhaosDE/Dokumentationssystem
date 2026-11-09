using System.Diagnostics;
using DokuLine.Kern;

namespace DokuLine;

// Zeit je Wechsel zwischen den Bereichen bis zur fertigen Anzeige, nur mit „--messen“: je Wechsel eine Zeile in
// messung.log im Datenordner (nur Bereich und Millisekunden, keine Inhalte).
public static class Messung
{
    public const string Datei = "messung.log";
    public static bool An { get; set; }
    public static List<(string Bereich, long Millisekunden)> Werte { get; } = [];

    public static Stopwatch? Beginnen() => An ? Stopwatch.StartNew() : null;

    public static void Merken(string bereich, Stopwatch uhr)
    {
        long ms = uhr.ElapsedMilliseconds;
        Werte.Add((bereich, ms));
        try { File.AppendAllText(Pfade.ImDatenordner(Datei), $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {bereich} {ms} ms{Environment.NewLine}"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
