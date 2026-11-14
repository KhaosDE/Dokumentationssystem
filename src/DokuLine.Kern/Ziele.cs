namespace DokuLine.Kern;

// Ziele der Bewohner (Bauplan 43): ein Leitziel und Nahziele, in den Worten des Bewohners. Änderungen sind neue Fassungen;
// frühere bleiben im Verlauf. Drei Zustände (CLAUDE.md, Regel 5): Ziele eingetragen · „Bewohner möchte derzeit kein Ziel
// benennen“ (mit Datum und Grund) · nicht erfasst. Maßnahmen und Berichte können einem Ziel zugeordnet werden.
public enum ZielArt { Leitziel, Nahziel }

public enum ZielStand { Aktiv, Erreicht, Geaendert, Beendet }

public sealed record ZielFassung
{
    public string Wortlaut { get; init; } = "";
    public int Lebensbereich { get; init; }               // 1–5
    public DateOnly VereinbartAm { get; init; }
    public string MitWem { get; init; } = "";             // Kürzel
    public DateOnly UeberpruefenBis { get; init; }
    public ZielStand Stand { get; init; } = ZielStand.Aktiv;
    public DateOnly? StandAm { get; init; }
    public string StandSatz { get; init; } = "";
    public string Kuerzel { get; init; } = "";
    public DateTime Erfasst { get; init; }
}

public sealed record Ziel(long Id, long BewohnerId, ZielArt Art, IReadOnlyList<ZielFassung> Fassungen)
{
    public ZielFassung Aktuell => Fassungen[^1];
    public bool Aktiv => Aktuell.Stand == ZielStand.Aktiv;
}

// „Bewohner möchte derzeit kein Ziel benennen“ – mit Datum und Grund.
public sealed record ZielVerzicht(long BewohnerId, DateOnly Ab, string Grund, string Kuerzel, DateTime Erfasst);

public enum ZieleZustand { Eingetragen, Keine, NichtErfasst }

public static class ZielRegeln
{
    public const int LeitzielMonate = 12, NahzielMonate = 3, LeitzieleVorgabe = 1, NahzieleVorgabe = 2;
    public const string KeinZielText = "Bewohner möchte derzeit kein Ziel benennen";

    public static string ArtText(ZielArt a) => a == ZielArt.Leitziel ? "Leitziel" : "Nahziel";

    public static string Text(Ziel z) => $"{ArtText(z.Art)}: {z.Aktuell.Wortlaut}";

    // Zur Auswahl in Bericht und Maßnahme: die aktiven Ziele, dazu ein schon zugeordnetes, das inzwischen beendet ist.
    public static List<Ziel> Waehlbar(IEnumerable<Ziel> ziele, long? zugeordnet)
    {
        var liste = ziele.ToList();
        var waehlbar = Aktive(liste);
        if (zugeordnet is long id && waehlbar.All(z => z.Id != id) && liste.FirstOrDefault(z => z.Id == id) is Ziel alt) waehlbar.Add(alt);
        return waehlbar;
    }

    public static string StandText(ZielStand s) => s switch
    {
        ZielStand.Aktiv => "aktiv",
        ZielStand.Erreicht => "erreicht",
        ZielStand.Geaendert => "geändert",
        _ => "beendet",
    };

    // Vorgabe für „überprüfen bis“: Nahziel 3, Leitziel 12 Monate nach der Vereinbarung.
    public static DateOnly UeberpruefenVorschlag(ZielArt art, DateOnly vereinbart) =>
        vereinbart.AddMonths(art == ZielArt.Leitziel ? LeitzielMonate : NahzielMonate);

    public static List<string> Pruefen(ZielFassung f)
    {
        var fehler = new List<string>();
        if (f.Wortlaut.Trim().Length == 0) fehler.Add("Bitte das Ziel in den Worten des Bewohners eintragen.");
        if (f.Lebensbereich is < 1 or > 5) fehler.Add("Bitte den Lebensbereich wählen.");
        if (f.MitWem.Trim().Length == 0) fehler.Add("Bitte angeben, mit wem das Ziel vereinbart wurde.");
        if (f.UeberpruefenBis < f.VereinbartAm) fehler.Add("„Überprüfen bis“ liegt vor „vereinbart am“.");
        if (f.Stand != ZielStand.Aktiv && f.StandSatz.Trim().Length == 0) fehler.Add($"Bitte kurz beschreiben, warum das Ziel „{StandText(f.Stand)}“ ist.");
        return fehler;
    }

    // Aktive Ziele in fester Reihenfolge: Leitziele zuerst, dann Nahziele nach Vereinbarung.
    public static List<Ziel> Aktive(IEnumerable<Ziel> ziele) =>
        [.. ziele.Where(z => z.Aktiv).OrderBy(z => z.Art).ThenBy(z => z.Aktuell.VereinbartAm).ThenBy(z => z.Id)];

    // Welcher der drei Zustände? Ein Verzicht geht nur ohne aktives Ziel und gilt, bis danach eines eingetragen wird.
    public static ZieleZustand Zustand(IReadOnlyCollection<Ziel> ziele, ZielVerzicht? verzicht)
    {
        if (Aktive(ziele).Count > 0) return ZieleZustand.Eingetragen;
        if (verzicht is not null && ziele.All(z => z.Aktuell.Erfasst < verzicht.Erfasst)) return ZieleZustand.Keine;
        return ZieleZustand.NichtErfasst;
    }

    // Die schmale Zeile über der Berichtsliste und im Kopf der Akte.
    public static string Leiste(IReadOnlyCollection<Ziel> ziele, ZielVerzicht? verzicht)
    {
        switch (Zustand(ziele, verzicht))
        {
            case ZieleZustand.Keine: return $"{KeinZielText} (seit {Datum.Text(verzicht!.Ab)}: {verzicht.Grund})";
            case ZieleZustand.NichtErfasst: return "Ziele: nicht erfasst";
        }
        int nah = 0;
        return string.Join(" · ", Aktive(ziele).Select(z => z.Art == ZielArt.Leitziel ? $"Leitziel: {z.Aktuell.Wortlaut}" : $"Nahziel {++nah}: {z.Aktuell.Wortlaut}"));
    }

    // Marken: „Ziel fehlt“ (zu wenige Ziele oder nicht erfasst), „Nahziel ohne Maßnahme“, „Überprüfung fällig“. Mit Verzicht: keine.
    public static List<Marke> Marken(IReadOnlyCollection<Ziel> ziele, ZielVerzicht? verzicht, IEnumerable<Massnahme> massnahmen, DateOnly heute,
        int leitziele = LeitzieleVorgabe, int nahziele = NahzieleVorgabe)
    {
        var zustand = Zustand(ziele, verzicht);
        if (zustand == ZieleZustand.Keine) return [];
        var marken = new List<Marke>();
        var aktive = Aktive(ziele);
        if (zustand == ZieleZustand.NichtErfasst) return [new Marke(MarkenArt.NichtErfasst, "Ziele nicht erfasst")];
        int leit = aktive.Count(z => z.Art == ZielArt.Leitziel), nah = aktive.Count(z => z.Art == ZielArt.Nahziel);
        if (leit < leitziele || nah < nahziele)
            marken.Add(new Marke(MarkenArt.NichtErfasst, leit < leitziele ? "Ziel fehlt: Leitziel" : $"Ziel fehlt: {nahziele - nah} Nahziel{(nahziele - nah == 1 ? "" : "e")}"));
        var mitMassnahme = massnahmen.Where(m => m.Laeuft(heute)).Select(m => m.Am(heute)?.ZielId).OfType<long>().ToHashSet();
        if (aktive.Where(z => z.Art == ZielArt.Nahziel).Count(z => !mitMassnahme.Contains(z.Id)) is int ohne and > 0)
            marken.Add(new Marke(MarkenArt.NichtErfasst, ohne == 1 ? "Nahziel ohne Maßnahme" : $"{ohne} Nahziele ohne Maßnahme"));
        if (aktive.Any(z => z.Aktuell.UeberpruefenBis <= heute)) marken.Add(new Marke(MarkenArt.NichtErfasst, "Überprüfung der Ziele fällig"));
        return marken;
    }

    // Für Reflexion und HEB: je Ziel die zugeordneten Berichte und Maßnahmen im Zeitraum (nur gezählt).
    public static List<string> Zusammenfassung(IEnumerable<Ziel> ziele, IEnumerable<Bericht> berichte, IEnumerable<Massnahme> massnahmen,
        IEnumerable<MassnahmeBestaetigung> bestaetigungen)
    {
        var liste = new List<string>();
        var berichteJe = berichte.Where(b => b.ZielId is not null && b.Art != MassnahmenRegeln.ArtBericht).ToLookup(b => b.ZielId!.Value);
        var massnahmenJe = massnahmen.ToLookup(m => m.Fassungen.Select(f => f.ZielId).LastOrDefault(z => z is not null));
        var bestaetigtJe = bestaetigungen.ToLookup(b => b.MassnahmeId);
        foreach (var z in Aktive(ziele))
        {
            var eigene = massnahmenJe[z.Id].ToList();
            int durch = eigene.Sum(m => bestaetigtJe[m.Id].Count(b => b.Ergebnis == MassnahmeErgebnis.Durchgefuehrt));
            int anzahl = berichteJe[z.Id].Count();
            liste.Add($"{ArtText(z.Art)}: {z.Aktuell.Wortlaut} – {anzahl} Bericht{(anzahl == 1 ? "" : "e")}, " +
                $"{eigene.Count} Maßnahme{(eigene.Count == 1 ? "" : "n")} ({durch}× durchgeführt)");
        }
        return liste;
    }
}
