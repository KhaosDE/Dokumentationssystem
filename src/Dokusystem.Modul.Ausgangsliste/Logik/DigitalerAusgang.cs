using System.Globalization;

namespace Dokusystem.Modul.Ausgangsliste;

// Ein einzelner Ausgang eines Bewohners an einem Tag – digitales Gegenstück zu einem Block
// „von | bis | Hz.“ auf dem Papierbogen. Ende leer = unterwegs.
public class AusgangEintrag
{
    public long Id { get; set; }
    public long BewohnerId { get; set; }
    public DateTime Datum { get; set; }
    public TimeSpan Beginn { get; set; }
    public TimeSpan? Ende { get; set; }
    public string Art { get; set; } = "";         // „allein“ oder „mit Bewohner“
    public string HdzBeginn { get; set; } = "";   // Handzeichen beim Rausgehen
    public string HdzEnde { get; set; } = "";     // Handzeichen bei der Rückkehr

    // „Hz. AB/CD“ – beide Handzeichen, gleiche nur einmal.
    public string Handzeichen => HdzEnde == "" || HdzEnde == HdzBeginn ? HdzBeginn : $"{HdzBeginn}/{HdzEnde}";

    public bool Unterwegs => Ende == null;

    // „14:00–15:30 Uhr allein“
    public string Zeitraum => Ende is TimeSpan ende
        ? $"{Beginn:hh\\:mm}–{ende:hh\\:mm} Uhr {Art}"
        : $"seit {Beginn:hh\\:mm} Uhr {Art}";
}

// Was eine Karte vom digitalen Ausgang wissen muss.
public class DigitalStand
{
    public AusgangEintrag? Laufend { get; init; }
    public bool Ueberfaellig { get; init; }
    public TimeSpan? ZurueckBis { get; init; }
    public List<AusgangEintrag> Erledigt { get; init; } = [];
}

// Untermodul „Digitaler Ausgang“ (übernommen aus der Ausgangsliste): Ausgänge per Klick beginnen
// und beenden, wer gerade unterwegs ist, wer überfällig ist, und der Text für den Nachtbericht
// zum Kopieren. Kein Bezug zu den Berichten des Dokumentationssystems: Der Text wird kopiert.
public static class DigitalerAusgang
{
    public const string Schluessel = "ausgangsliste.digital";
    public static readonly string[] Arten = ["allein", "mit Bewohner"];

    // Aufbewahrung wie beim Papierbogen: 3 Monate, danach wird gelöscht.
    public const int AufbewahrungMonate = 3;

    // Die erlaubten Stunden einer Art aus dem Ausgangsfeld („2 Allein“ → 2).
    public static int Anspruch(Platz platz, string art) =>
        KartenText.Stundenteile(platz).Where(t => t.Art == art).Sum(t => t.Stunden);

    // Spätestens zurück: Beginn plus Anspruch der Art. Ohne Anspruch: unbekannt.
    public static TimeSpan? ZurueckBis(Platz platz, AusgangEintrag eintrag)
    {
        int stunden = Anspruch(platz, eintrag.Art);
        return stunden > 0 ? eintrag.Beginn + TimeSpan.FromHours(stunden) : null;
    }

    // Stand für eine Karte aus den Einträgen des Tages.
    public static DigitalStand StandFuer(Platz platz, List<AusgangEintrag> heute, TimeSpan jetzt)
    {
        var eigene = heute.Where(e => e.BewohnerId == platz.BewohnerId).OrderBy(e => e.Beginn).ToList();
        var laufend = eigene.LastOrDefault(e => e.Unterwegs);
        TimeSpan? bis = laufend != null ? ZurueckBis(platz, laufend) : null;
        return new DigitalStand
        {
            Laufend = laufend,
            ZurueckBis = bis,
            Ueberfaellig = bis is TimeSpan b && jetzt > b,
            Erledigt = eigene.Where(e => !e.Unterwegs).ToList(),
        };
    }

    // Ein Ausgang darf beginnen, wenn der Bewohner anwesend ist, keinen Status hat, nicht schon
    // unterwegs ist und für die Art Stunden eingetragen sind. Gibt den Grund zurück, sonst null.
    public static string? BeginnenNichtMoeglich(Platz platz, DigitalStand stand, string art)
    {
        if (platz.HatStatus) return $"{platz.Bewohnername}: {KartenText.HeuteKurz(platz)}.";
        if (stand.Laufend is not null) return $"{platz.Bewohnername} ist schon unterwegs.";
        if (Anspruch(platz, art) == 0) return $"Für {platz.Bewohnername} ist kein Ausgang „{art}“ eingetragen.";
        return null;
    }

    // Der Text für die Dokumentation, ohne Datum:
    // „Anton Ahorn hat den Ausgang wahrgenommen: 14:00–15:30 Uhr allein, 17:00–18:00 Uhr mit Bewohner.“
    public static string Berichtstext(Platz platz, IEnumerable<AusgangEintrag> erledigt)
    {
        var liste = erledigt.Where(e => !e.Unterwegs).OrderBy(e => e.Beginn).Select(e => e.Zeitraum).ToList();
        if (liste.Count == 0) return "";
        return $"{platz.Bewohnername} hat den Ausgang wahrgenommen: {string.Join(", ", liste)}.";
    }

    // Nachtbericht: je Bewohner mit erledigten Ausgängen eine Zeile, nach Zimmer.
    public static string Nachtbericht(IEnumerable<Platz> plaetze, List<AusgangEintrag> heute) =>
        string.Join(Environment.NewLine, plaetze
            .Select(p => Berichtstext(p, heute.Where(e => e.BewohnerId == p.BewohnerId)))
            .Where(t => t != ""));

    public static string Uhrzeit(TimeSpan zeit) => zeit.ToString("hh\\:mm", CultureInfo.InvariantCulture);

    // Uhrzeit „vor n Stunden“ für die Demo, auf 5 Minuten gerundet, frühestens 00:05.
    public static TimeSpan Vor(DateTime jetzt, double stunden)
    {
        var t = jetzt.TimeOfDay - TimeSpan.FromHours(stunden);
        t = t < TimeSpan.FromMinutes(5) ? TimeSpan.FromMinutes(5) : t;
        return new TimeSpan(t.Hours, t.Minutes / 5 * 5, 0);
    }
}
