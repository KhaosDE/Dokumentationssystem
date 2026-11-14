namespace DokuLine.Kern;

// Zeitauswertung (Bauplan 41): sammelt die erfassten Minuten der Bewohner im Zeitraum – bestätigte Maßnahmen (durchgeführt),
// Therapie-Teilnahmen „gegangen“ (Dauer des Termins) und Berichte mit Dauer. Nur mit Recht, nur je Bewohner.
public sealed partial class Speicher
{
    public List<ZeitZeile> Zeitauswertung(IReadOnlyList<Bewohner> bewohner, DateOnly von, DateOnly bis, string kuerzel)
    {
        Recht(kuerzel, Funktionen.Zeitauswertung);
        if (bis < von) throw new Eingabefehler(["„bis“ liegt vor „von“."]);
        if (bis.DayNumber - von.DayNumber > 400) throw new Eingabefehler(["Bitte höchstens ein Jahr auswerten."]);
        var ids = bewohner.Select(b => b.Id!.Value).ToHashSet();
        var posten = new List<ZeitPosten>();
        posten.AddRange(MassnahmeBestaetigungen(von: von, bis: bis)
            .Where(b => ids.Contains(b.BewohnerId) && b.Ergebnis == MassnahmeErgebnis.Durchgefuehrt && b.Minuten > 0)
            .Select(b => new ZeitPosten(b.BewohnerId, b.Tag, ZeitArt.Massnahmen, b.Lebensbereich, b.Minuten)));
        foreach (long id in ids)
        {
            for (var montag = WochenplanRegeln.Montag(von); montag <= bis; montag = montag.AddDays(7))
                posten.AddRange(Woche(id, montag)
                    .Where(t => t.Tag >= von && t.Tag <= bis && t.Quelle == TerminQuelle.Therapie && t.Teilnahme == Teilnahme.Teilgenommen && !t.Ganztags)
                    .Select(t => new ZeitPosten(id, t.Tag, ZeitArt.Therapie, null, (int)(t.Bis - t.Von).TotalMinutes)));
            posten.AddRange(Berichte(id, new BerichtFilter(von, bis)).Select(ZeitauswertungRegeln.AusBericht).OfType<ZeitPosten>());
        }
        var heute = DateOnly.FromDateTime(Uhr());
        var massnahmen = Massnahmen(auchBeendete: true).Where(m => ids.Contains(m.BewohnerId)).ToLookup(m => m.BewohnerId);
        var geplant = ids.ToDictionary(id => id, id => ZeitauswertungRegeln.Geplant(massnahmen[id], von, bis < heute ? bis : heute));
        return ZeitauswertungRegeln.Zeilen(bewohner, posten, geplant);
    }
}
