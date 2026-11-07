using Dokusystem.Kern;

namespace Dokusystem.Modul.Ausgangsliste;

// Das Modul „Ausgangsliste“ für die Modulverwaltung des Dokumentationssystems.
public sealed class AusgangslisteModul : IModul
{
    public IModulDaten Daten { get; } = new AusgangslisteDaten();
    public string Name => "Ausgangsliste";
    public string Beschreibung => "Ausgangszeiten, Anwesenheit und Abwesenheit, Unterbringung mit Fristen, Tagesliste, Verlauf je Person.";

    public IReadOnlyList<Untermodul> Untermodule { get; } =
    [
        new(Kuechendienst.Schluessel, "Küchendienst", "Plan reihum nach Zimmern, Zimmergruppen, Tausch."),
        new(DigitalerAusgang.Schluessel, "Digitaler Ausgang", "Ausgänge per Klick beginnen und beenden, Nachtbericht zum Kopieren."),
    ];

    public ModulSeite SeiteBauen(Speicher speicher) => new AusgangslisteSeite(speicher);

    public ModulReiter? ReiterBauen(Speicher speicher) => new AusgangReiter(speicher);

    // Marken auf den Bewohnerkarten des Kerns: nur, was Aufmerksamkeit braucht.
    public IEnumerable<Marke> Marken(Speicher speicher, Bewohner bewohner, DateTime jetzt)
    {
        var db = new Datenbank(speicher);
        var p = db.Platz(bewohner.Id!.Value);
        if (p is null) return [];
        var stand = speicher.ModulAn(DigitalerAusgang.Schluessel)
            ? DigitalerAusgang.StandFuer(p, db.AusgaengeLaden(jetzt), jetzt.TimeOfDay)
            : null;
        return AusgangsMarken.Fuer(p, jetzt, stand);
    }
}
