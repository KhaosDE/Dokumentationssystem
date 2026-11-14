using DokuLine.Kern;

namespace DokuLine;

// Ist ein Bewohner gerade abwesend? Erst die Ausgangsliste (Marken der Module), dann Heimfahrt, Fahrt und Klinik aus dem
// Kern (Bauplan 50). Ein Text wie „Klinik seit 03.11.“ oder null.
public static class AbwesenheitAnzeige
{
    public static string? Text(Speicher speicher, Bewohner b, DateTime jetzt)
    {
        if (Modulverwaltung.Marken(speicher, b, jetzt).FirstOrDefault(m => m.Art == MarkenArt.Abwesend) is Marke m)
            return m.Text.StartsWith("abwesend: ", StringComparison.Ordinal) ? m.Text["abwesend: ".Length..] : m.Text;
        return speicher.AbwesenheitJetzt(b.Id!.Value, jetzt);
    }
}
