using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Der Datenteil eines zuschaltbaren Moduls (Modulverwaltung). Ein Modul legt eigene Tabellen mit
// eigener Vorsilbe an (z. B. „al_“) und bringt eigene Demo-Daten mit. Der Kern kennt keine Module;
// das Programm meldet sie beim Start an (Speicher.ModulAnmelden).
public interface IModulDaten
{
    // Kurzname ohne Leerzeichen, z. B. „ausgangsliste“. Steht in den Verwaltungsangaben.
    string Schluessel { get; }

    // Aufbau der eigenen Tabellen, Schritt für Schritt wie im Kern. Neue Schritte nur anhängen.
    IReadOnlyList<string> Schritte { get; }

    // Demo-Daten des Moduls, nachdem die Demo-Bewohner des Kerns stehen (gleiche Transaktion).
    void DemoEinfuegen(Speicher speicher, SqliteTransaction transaktion);
}

public sealed partial class Speicher
{
    private readonly List<IModulDaten> module = [];

    public IReadOnlyList<IModulDaten> Module => module;

    private static string ModulStand(string schluessel) => $"modul_{schluessel}_stand";
    private static string ModulDemo(string schluessel) => $"demo_modul_{schluessel}";
    private static string ModulSchalter(string schluessel) => $"modul_{schluessel}_an";

    // Meldet ein Modul an: baut seine Tabellen auf (eigener Stand) und nimmt es in die Demo-Daten auf.
    public void ModulAnmelden(IModulDaten modul)
    {
        if (module.Any(m => m.Schluessel == modul.Schluessel)) return;
        int stand = int.TryParse(Meta(ModulStand(modul.Schluessel)), out int s) ? s : 0;
        if (stand > modul.Schritte.Count)
            throw new SpeicherFehler("Die Daten stammen aus einer neueren Version des Programms. Bitte die neueste Version verwenden.");
        for (int nummer = stand; nummer < modul.Schritte.Count; nummer++)
        {
            using var schritt = db.BeginTransaction();
            Ausfuehren(modul.Schritte[nummer], schritt);
            SetzeMeta(ModulStand(modul.Schluessel), (nummer + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), schritt);
            schritt.Commit();
        }
        module.Add(modul);
    }

    // Ein- und ausgeschaltet (Einstellungen, Abschnitt „Module“). Ohne Eintrag gilt: eingeschaltet.
    // Untermodule nutzen „modul.untermodul“ als Schlüssel. Ausschalten löscht keine Daten.
    public bool ModulAn(string schluessel) => Meta(ModulSchalter(schluessel)) != "0";

    public void ModulSetzen(string schluessel, bool an, SqliteTransaction? transaktion = null)
    {
        SetzeMeta(ModulSchalter(schluessel), an ? "1" : "0", transaktion);
        if (transaktion is null) ModuleGeaendert?.Invoke();
    }

    public event Action? ModuleGeaendert;

    // Module, deren Demo-Daten noch nie geladen wurden (vor der Transaktion fragen).
    private List<IModulDaten> ModuleOhneDemo() => module.Where(m => Meta(ModulDemo(m.Schluessel)) is null).ToList();

    // Demo-Daten der Module laden; in der Demo ist jedes Modul eingeschaltet.
    private void DemoModuleEinfuegen(SqliteTransaction transaktion, IEnumerable<IModulDaten> welche)
    {
        foreach (var m in welche)
        {
            m.DemoEinfuegen(this, transaktion);
            SetzeMeta(ModulDemo(m.Schluessel), "1", transaktion);
            SetzeMeta(ModulSchalter(m.Schluessel), "1", transaktion);
        }
    }
}
