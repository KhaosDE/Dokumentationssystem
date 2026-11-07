namespace Dokusystem.Kern;

// Bilder im Datenordner (Bewohnerbilder im Unterordner „bilder“, das eigene Logo in „logo“),
// nie im Repository.
// Die Datenbank merkt sich nur den Dateinamen. Ob eine Datei wirklich ein Bild ist,
// prüft die Oberfläche beim Auswählen; der Kern kommt ohne Bildbibliothek aus.
public static class Bilder
{
    public const string Ordner = "bilder";
    public static readonly string[] Endungen = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    public static bool EndungErlaubt(string pfad) =>
        Endungen.Contains(Path.GetExtension(pfad).ToLowerInvariant());

    // Kopiert ein Bild in den Datenordner und gibt den neuen Dateinamen zurück.
    public static string Uebernehmen(string quelle, string ordner = Ordner)
    {
        string endung = Path.GetExtension(quelle).ToLowerInvariant();
        if (!Endungen.Contains(endung))
            throw new ArgumentException($"Bilder bitte als {string.Join(", ", Endungen)}.");
        string name = Guid.NewGuid().ToString("N") + endung;
        string ziel = Pfade.ImDatenordner(ordner, name);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        File.Copy(quelle, ziel);
        return name;
    }

    // Pfad zum hinterlegten Bild – oder null, wenn keins da ist.
    public static string? Pfad(string? name, string ordner = Ordner)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        try
        {
            string datei = Pfade.ImDatenordner(ordner, name);
            return File.Exists(datei) ? datei : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static void Loeschen(string? name, string ordner = Ordner)
    {
        if (Pfad(name, ordner) is string datei)
        {
            try { File.Delete(datei); }
            catch (IOException) { }                    // z. B. gerade geöffnet – bleibt liegen, stört nicht
            catch (UnauthorizedAccessException) { }
        }
    }
}
