namespace Dokusystem.Kern;

// Wo das Programm seine Dateien ablegt.
// Datenordner (Datenbank, Bilder, Einstellungen, update.log):
//   Windows %APPDATA%\Dokumentationssystem, sonst ~/.local/share/dokumentationssystem.
// Mit der Umgebungsvariable DOKUSYSTEM_DATEN lässt er sich umlenken (Tests, Selbsttest).
// Das Programm schreibt nur in den Datenordner.
public static class Pfade
{
    public const string Umlenkung = "DOKUSYSTEM_DATEN";

    public static string Datenordner
    {
        get
        {
            string? umgelenkt = Environment.GetEnvironmentVariable(Umlenkung);
            if (!string.IsNullOrWhiteSpace(umgelenkt)) return Path.GetFullPath(umgelenkt);
            if (OperatingSystem.IsWindows())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dokumentationssystem");
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "dokumentationssystem");
        }
    }

    // Pfad im Datenordner. Ein Pfad, der aus dem Ordner herausführt („..“), wird abgelehnt.
    public static string ImDatenordner(params string[] teile)
    {
        string basis = Path.GetFullPath(Datenordner);
        string ziel = Path.GetFullPath(Path.Combine([basis, .. teile]));
        string mitTrenner = basis.EndsWith(Path.DirectorySeparatorChar) ? basis : basis + Path.DirectorySeparatorChar;
        if (ziel != basis && !ziel.StartsWith(mitTrenner, StringComparison.Ordinal))
            throw new ArgumentException($"Pfad liegt außerhalb des Datenordners: {ziel}");
        return ziel;
    }
}
