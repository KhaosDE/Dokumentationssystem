using DokuLine.Kern;

namespace DokuLine;

// Ein zuschaltbares Modul (Modulverwaltung). Das Programm fragt jedes eingeschaltete Modul nach
// seinen Teilen und zeigt sie an: Eintrag in der Seitenleiste mit eigener Seite, Reiter in der
// Bewohnerakte, Marken auf den Bewohnerkarten. Ein ausgeschaltetes Modul wird nicht gefragt und
// hinterlässt keine Spuren; seine Daten bleiben in der Datenbank.
public interface IModul
{
    IModulDaten Daten { get; }                       // Tabellen und Demo-Daten (Kern)
    string Name { get; }                             // Seitenleiste und Einstellungen
    string Beschreibung { get; }                     // Einstellungen, Abschnitt „Module“
    IReadOnlyList<Untermodul> Untermodule { get; }   // eigene Häkchen unter dem Modul

    ModulSeite SeiteBauen(Speicher speicher);
    ModulReiter? ReiterBauen(Speicher speicher);
    IEnumerable<Marke> Marken(Speicher speicher, Bewohner bewohner, DateTime jetzt);
}

// Schluessel ist der volle Schlüssel für Speicher.ModulAn, z. B. „ausgangsliste.kuechendienst“.
public sealed record Untermodul(string Schluessel, string Name, string Beschreibung);

// Die Seite eines Moduls rechts neben der Seitenleiste.
public abstract class ModulSeite : Panel
{
    public abstract string KopfText { get; }
    public virtual IReadOnlyList<Button> Knoepfe => [];

    // Beim Wechsel in den Bereich: frisch laden.
    public abstract void Zeigen();

    // Etwas hat sich geändert (Kopftext und Marken neu).
    public event Action? Geaendert;
    protected void Melden() => Geaendert?.Invoke();
}

// Ein Reiter eines Moduls in der Bewohnerakte.
public abstract class ModulReiter : Panel
{
    public abstract string Titel { get; }
    public abstract void Zeigen(Bewohner bewohner);
    public event Action? Geaendert;
    protected void Melden() => Geaendert?.Invoke();
}
