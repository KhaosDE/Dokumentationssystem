using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Das Modul „Ausgangsliste“ für die Modulverwaltung von DokuLine.
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

    public TagesplanTeil? TagesplanBauen(Speicher speicher) => new KuechendienstTagesplan(speicher);

    // Marken auf den Bewohnerkarten des Kerns: nur, was Aufmerksamkeit braucht.
    public IEnumerable<Marke> Marken(Speicher speicher, Bewohner bewohner, DateTime jetzt)
    {
        var db = new Datenbank(speicher);
        var p = db.Platz(bewohner.Id!.Value);
        if (p is null) return [];
        var stand = speicher.ModulAn(DigitalerAusgang.Schluessel)
            ? DigitalerAusgang.StandFuer(p, db.AusgaengeLaden(jetzt), jetzt)
            : null;
        var marken = AusgangsMarken.Fuer(p, jetzt, stand, wartezeit: Einstellungen.Laden().WartezeitMinuten, abgaengig: speicher.OffeneAbgaengigkeit(bewohner.Id!.Value));
        // Küchendienst (Bauplan 45): wer heute dran ist; ist niemand vom Dienst da, „Küchendienst unbesetzt“ in Achtung-Farbe.
        if (speicher.ModulAn(Kuechendienst.Schluessel) && Kuechendienst.Heute(db.KuechendienstLaden(), jetzt, db.Laden()) is KuechendienstHeute heute
            && heute.Zimmer.Split('/').Select(z => z.Trim()).Contains(p.Zimmernummer))
        {
            if (p.Anwesend) marken.Add(new Marke(MarkenArt.Ausgang, "Küchendienst heute"));
            else if (heute.Unbesetzt) marken.Add(new Marke(MarkenArt.NichtErfasst, Kuechendienst.Unbesetzt));
        }
        return marken;
    }
}
