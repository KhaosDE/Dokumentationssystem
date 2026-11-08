using DokuLine.Kern;

namespace DokuLine.Tests;

// Lenkt den Datenordner für die Dauer eines Tests in einen leeren Ordner um.
public sealed class Datenordner : IDisposable
{
    private readonly string? vorher = Environment.GetEnvironmentVariable(Pfade.Umlenkung);
    public string Ordner { get; } = Directory.CreateTempSubdirectory("dokusystem-test-").FullName;

    public Datenordner() => Environment.SetEnvironmentVariable(Pfade.Umlenkung, Ordner);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Pfade.Umlenkung, vorher);
        try { Directory.Delete(Ordner, recursive: true); } catch (IOException) { }
    }
}

// Ein leerer Ordner für Hilfsdateien eines Tests.
public sealed class Hilfsordner : IDisposable
{
    public string Ordner { get; } = Directory.CreateTempSubdirectory("dokusystem-hilfe-").FullName;

    public string Datei(string name, byte[] inhalt)
    {
        string pfad = Path.Combine(Ordner, name);
        File.WriteAllBytes(pfad, inhalt);
        return pfad;
    }

    public void Dispose()
    {
        try { Directory.Delete(Ordner, recursive: true); } catch (IOException) { }
    }
}

// Der Datenordner ist eine Umgebungsvariable des ganzen Prozesses: Tests, die ihn umlenken,
// dürfen nicht gleichzeitig laufen. Alle solchen Testklassen gehören in diese Sammlung.
[CollectionDefinition("Datenordner", DisableParallelization = true)]
public sealed class DatenordnerSammlung;

public static class Repo
{
    // Wurzel des Repositorys (dort, wo CLAUDE.md liegt).
    public static string Wurzel
    {
        get
        {
            var ordner = new DirectoryInfo(AppContext.BaseDirectory);
            while (ordner is not null && !File.Exists(Path.Combine(ordner.FullName, "CLAUDE.md")))
                ordner = ordner.Parent;
            return ordner?.FullName ?? throw new InvalidOperationException("Repository-Wurzel nicht gefunden.");
        }
    }
}
