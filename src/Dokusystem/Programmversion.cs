using System.Globalization;
using System.Reflection;

namespace Dokusystem;

// Versionsnummer (von Hand, Directory.Build.props) und Build-Nummer (Laufnummer von GitHub Actions).
public static class Programmversion
{
    private static Assembly Programm => typeof(Programmversion).Assembly;

    public static string Version =>
        Programm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?";

    public static int Build =>
        int.TryParse(Programm.GetCustomAttributes<AssemblyMetadataAttribute>()
                         .FirstOrDefault(a => a.Key == "BuildNummer")?.Value,
                     NumberStyles.Integer, CultureInfo.InvariantCulture, out int b) ? b : 0;

    public static string Text => $"Version {Version} · {(Build > 0 ? $"Build {Build}" : "Entwicklerversion")}";
}
