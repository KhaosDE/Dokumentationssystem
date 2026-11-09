namespace DokuLine.Kern;

// Wo das Programm seine Dateien ablegt.
// Datenordner (Datenbank, Bilder, Einstellungen, update.log):
//   Windows %APPDATA%\DokuLine, sonst ~/.local/share/dokuline.
// Mit der Umgebungsvariable DOKUSYSTEM_DATEN lässt er sich umlenken (Tests, Selbsttest).
// Das Programm schreibt nur in den Datenordner.
// Bis 0.29.1 hieß der Ordner „Dokumentationssystem“; beim ersten Start wird sein Inhalt kopiert
// (AltenOrdnerUebernehmen). Der alte Ordner bleibt unangetastet.
public static class Pfade
{
    public const string Umlenkung = "DOKUSYSTEM_DATEN";
    public const string Name = "DokuLine";
    public const string AlterName = "Dokumentationssystem";

    public static string Datenordner
    {
        get
        {
            string? umgelenkt = Environment.GetEnvironmentVariable(Umlenkung);
            if (!string.IsNullOrWhiteSpace(umgelenkt)) return Path.GetFullPath(umgelenkt);
            return Standardordner(Name);
        }
    }

    // Der Datenordner unter dem alten Namen – null, wenn umgelenkt (Tests, Selbsttest).
    public static string? AlterDatenordner =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Umlenkung)) ? Standardordner(AlterName) : null;

    private static string Standardordner(string name) => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", name.ToLowerInvariant());

    // Übergang: Gibt es den neuen Datenordner noch nicht, aber den alten, wird dessen
    // Inhalt (Datenbank, Bilder, Einstellungen, Protokolle) in den neuen kopiert. Erst in einen
    // Zwischenordner, dann umbenannt – bricht das Kopieren ab, gibt es keinen halben neuen Ordner,
    // und beim nächsten Start wird es wieder versucht. Gibt einen Satz fürs Protokoll zurück oder
    // null, wenn nichts zu tun war.
    public static string? AltenOrdnerUebernehmen(string? alt = null, string? neu = null)
    {
        alt ??= AlterDatenordner;
        neu ??= Datenordner;
        if (alt is null || Directory.Exists(neu) || !Directory.Exists(alt)) return null;
        string zwischen = neu + ".kopie";
        if (Directory.Exists(zwischen)) Directory.Delete(zwischen, recursive: true);   // Rest eines abgebrochenen Versuchs
        int dateien = 0;
        try
        {
            foreach (string ordner in Directory.EnumerateDirectories(alt, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(zwischen, Path.GetRelativePath(alt, ordner)));
            Directory.CreateDirectory(zwischen);
            foreach (string datei in Directory.EnumerateFiles(alt, "*", SearchOption.AllDirectories))
            {
                KopierenMitWiederholung(datei, Path.Combine(zwischen, Path.GetRelativePath(alt, datei)));
                dateien++;
            }
            Directory.Move(zwischen, neu);
        }
        catch (Exception)
        {
            try { if (Directory.Exists(zwischen)) Directory.Delete(zwischen, recursive: true); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            throw;
        }
        return $"Datenordner übernommen: {dateien} Dateien aus „{alt}“ nach „{neu}“ kopiert; der alte Ordner bleibt unverändert.";
    }

    // Eine Datei kann einen Augenblick noch offen sein (die alte Version schreibt beim Update zuletzt ins Protokoll).
    private static void KopierenMitWiederholung(string quelle, string ziel)
    {
        for (int versuch = 1; ; versuch++)
        {
            try { File.Copy(quelle, ziel, overwrite: true); return; }
            catch (IOException) when (versuch < 5) { Thread.Sleep(200); }
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
