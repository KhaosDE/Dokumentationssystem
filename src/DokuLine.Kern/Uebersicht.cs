using System.Globalization;

namespace DokuLine.Kern;

// Startseite als Übersicht: was heute wichtig ist, je Bewohner als Marken und oben im Bereich
// „Heute“. Zeigt nur, was es im Programm schon gibt.

// Was heute für einen Bewohner gilt (zusammengestellt vom Speicher).
public sealed record HeuteFuer(Bewohner Bewohner, IReadOnlyList<Termin> Termine,
    IReadOnlyList<(Verordnung Verordnung, string Aenderung)> Medikation, KalenderTermin? Heimfahrt)
{
    public string? HebFaellig { get; init; }   // z. B. „HEB fällig (letzter 10/2025)“
}

public static class UebersichtRegeln
{
    public const int BeschlussFristVorgabe = 90;
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    // Änderung einer Verordnung an diesem Tag gegenüber dem Vortag, sonst null.
    public static string? MedikationAm(Verordnung v, DateOnly tag)
    {
        var heute = MedikationRegeln.Stand(v, tag);
        var gestern = MedikationRegeln.Stand(v, tag.AddDays(-1));
        if (gestern.Zustand == Zustand.NochNicht && heute.Zustand != Zustand.NochNicht) return "neu verordnet";
        if (heute.Zustand == Zustand.Abgesetzt && gestern.Zustand != Zustand.Abgesetzt) return "abgesetzt";
        if (heute.Zustand == Zustand.Pausiert && gestern.Zustand != Zustand.Pausiert) return "Pause beginnt";
        if (gestern.Zustand == Zustand.Pausiert && heute.Zustand != Zustand.Pausiert) return "Pause endet";
        if (heute.Zustand != Zustand.Abgesetzt && heute.Dosis != gestern.Dosis) return "neue Dosis";
        return null;
    }

    // Tage bis zum Beschlussende; null ohne Beschluss.
    public static int? BeschlussTage(Bewohner b, DateOnly heute) =>
        b.Beschluss == BeschlussZustand.Ende && b.Beschlussende is DateOnly e ? e.DayNumber - heute.DayNumber : null;

    // Marken der Startseite: Allergie, nicht erfasst, Beschluss endet bald, Medikation heute,
    // Termine heute, Heimfahrt (abwesend). Farbe nie allein – jede Marke trägt ein Wort.
    public static List<Marke> Marken(HeuteFuer h, DateOnly heute, int beschlussFristTage)
    {
        var b = h.Bewohner;
        var marken = new List<Marke>();
        if (h.Heimfahrt is KalenderTermin k)
            marken.Add(new Marke(MarkenArt.Abwesend, $"abwesend: Heimfahrt bis {k.Aktuell.Ende.ToString("ddd dd.MM.", De)}"));
        if (BeschlussTage(b, heute) is int tage && tage <= beschlussFristTage)
            marken.Add(new Marke(MarkenArt.Beschluss, tage switch
            {
                < 0 => $"Beschluss abgelaufen seit {Datum.Text(b.Beschlussende)}",
                0 => "Beschluss endet heute",
                1 => "Beschluss endet morgen",
                _ => $"Beschluss endet in {tage} Tagen",
            }));
        if (b.Allergie == AllergieZustand.Eingetragen) marken.Add(new Marke(MarkenArt.Allergie, $"Allergie: {b.Allergien}"));
        if (b.Allergie == AllergieZustand.NichtErfasst) marken.Add(new Marke(MarkenArt.NichtErfasst, "Allergien nicht erfasst"));
        if (b.Beschluss == BeschlussZustand.NichtErfasst) marken.Add(new Marke(MarkenArt.NichtErfasst, "Beschluss nicht erfasst"));
        if (Kennzeichen.OffenGefuehrtMarke(b) is Marke offen) marken.Add(offen);
        if (h.HebFaellig is string heb) marken.Add(new Marke(MarkenArt.NichtErfasst, heb));
        foreach (var (v, aenderung) in h.Medikation)
            marken.Add(new Marke(MarkenArt.Wichtig, $"Medikation: {aenderung}"));
        var termine = h.Termine.OrderBy(t => t.Ganztags ? TimeOnly.MinValue : t.Von).ToList();
        if (termine.Count > 0)
            marken.Add(new Marke(MarkenArt.Termin, $"heute {(termine[0].Ganztags ? "" : $"{termine[0].Von:HH\\:mm} ")}{termine[0].Titel}" +
                                                   (termine.Count > 1 ? $" (+{termine.Count - 1})" : "")));
        return marken;
    }

    // Kopfzeile des Bereichs „Heute“.
    public static string HeuteText(IReadOnlyList<HeuteFuer> alle, int abwesend)
    {
        int termine = alle.SelectMany(h => h.Termine).Select(t => (t.Titel, t.Von, t.Ganztags)).Distinct().Count();
        int medikation = alle.Sum(h => h.Medikation.Count);
        return $"{alle.Count} Bewohner · {abwesend} abwesend · {termine} {(termine == 1 ? "Termin" : "Termine")} heute · " +
               $"{medikation} {(medikation == 1 ? "Änderung" : "Änderungen")} in der Medikation";
    }

    // Termine des Tages über alle Bewohner, gleiche Termine zusammengefasst: „09:00–11:30 Holzwerkstatt (Anton Ahorn, Dieter Distel)“.
    public static List<string> TerminZeilen(IReadOnlyList<HeuteFuer> alle) =>
        alle.SelectMany(h => h.Termine.Select(t => (h.Bewohner, t)))
            .GroupBy(x => (x.t.Titel, x.t.ZeitText, x.t.Von))
            .OrderBy(g => g.Key.ZeitText == "ganztags" ? TimeOnly.MinValue : g.Key.Von)
            .Select(g => $"{g.Key.ZeitText} {g.Key.Titel} ({string.Join(", ", g.Select(x => x.Bewohner.Name).Distinct())})")
            .ToList();

    public static List<string> MedikationZeilen(IReadOnlyList<HeuteFuer> alle) =>
        alle.SelectMany(h => h.Medikation.Select(m => $"{h.Bewohner.Name}: {m.Verordnung.Medikament} {m.Verordnung.Staerke} – {m.Aenderung}")).ToList();
}
