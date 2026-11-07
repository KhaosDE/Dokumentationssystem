using Dokusystem.Kern;
using Dokusystem.Modul.Ausgangsliste;

namespace Dokusystem;

// Alle Module, die das Programm kennt (Modulverwaltung). Ein neues Modul wird hier eingetragen;
// alles andere (Seitenleiste, Akte, Marken, Einstellungen) fragt es über IModul ab.
public static class Modulverwaltung
{
    public static IReadOnlyList<IModul> Alle { get; } = [new AusgangslisteModul()];

    // Beim Start, bevor Demo-Daten geladen werden: Tabellen der Module anlegen.
    public static void Anmelden(Speicher speicher)
    {
        foreach (var m in Alle) speicher.ModulAnmelden(m.Daten);
    }

    public static IEnumerable<IModul> Eingeschaltet(Speicher speicher) => Alle.Where(m => speicher.ModulAn(m.Daten.Schluessel));

    // Marken der eingeschalteten Module für eine Bewohnerkarte.
    public static List<Marke> Marken(Speicher speicher, Bewohner b, DateTime jetzt) =>
        Eingeschaltet(speicher).SelectMany(m => m.Marken(speicher, b, jetzt)).ToList();
}
