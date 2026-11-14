using System.Globalization;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Maßnahmen je Bewohner (Bauplan 39): etwas Regelmäßiges, das zu tun ist – mit Lebensbereich, Rhythmus, Dienst und
// geplantem Zeitwert. Jede Durchführung wird bestätigt (durchgeführt · abgelehnt · nicht möglich), mit Kürzel, Uhrzeit
// und tatsächlicher Zeit. Ändern ist eine neue Fassung ab einem Datum, Beenden geschieht mit Grund; nichts wird gelöscht.
// Der Lebensbereich gehört weiter zum Bericht – eine Maßnahme muss niemand anlegen, damit Berichte ihren Platz haben.
public enum MassnahmeRhythmus { Taeglich, Wochentage, AlleNWochen, Monatlich, BeiBedarf }

public enum MassnahmeErgebnis { Durchgefuehrt, Abgelehnt, NichtMoeglich }

public sealed record MassnahmeFassung
{
    public DateOnly GiltAb { get; init; }
    public DateOnly? GiltBis { get; init; }                   // „gültig bis“; leer = ohne Ende
    public string Titel { get; init; } = "";
    public string Beschreibung { get; init; } = "";
    public int Lebensbereich { get; init; }                    // 1–5, Pflicht
    public long? ZielId { get; init; }                         // Leitziel oder Nahziel; leer = ohne Ziel
    public string Art { get; init; } = "";
    public MassnahmeRhythmus Rhythmus { get; init; }
    public IReadOnlyList<DayOfWeek> Wochentage { get; init; } = [];   // Wochentage; alle n Wochen: genau einer
    public int AlleWochen { get; init; } = 2;                  // nur „alle n Wochen“
    public int MonatsTag { get; init; } = 1;                   // nur „monatlich“ (1–28)
    public Schicht Schicht { get; init; }
    public TimeOnly? Uhrzeit { get; init; }                    // leer = irgendwann im Dienst
    public int Minuten { get; init; }                          // geplanter Zeitwert
    public string Kuerzel { get; init; } = "";
    public DateTime Erfasst { get; init; }
}

public sealed record Massnahme(long Id, long BewohnerId, IReadOnlyList<MassnahmeFassung> Fassungen, DateOnly? BeendetAm = null, string BeendetGrund = "",
    string BeendetVon = "")
{
    public DateOnly Von => Fassungen.Min(f => f.GiltAb);

    // Die Fassung, die an diesem Tag gilt (die jüngste mit GiltAb bis zu diesem Tag).
    public MassnahmeFassung? Am(DateOnly tag) =>
        Fassungen.Where(f => f.GiltAb <= tag).OrderByDescending(f => f.GiltAb).ThenByDescending(f => f.Erfasst).FirstOrDefault();

    // Für die Anzeige: die heute geltende, sonst die nächste (ab Datum geplant).
    public MassnahmeFassung Aktuell(DateOnly heute) => Am(heute) ?? Fassungen.OrderBy(f => f.GiltAb).First();

    public bool Laeuft(DateOnly tag) =>
        (BeendetAm is not DateOnly e || tag < e) && Am(tag) is MassnahmeFassung f && (f.GiltBis is not DateOnly bis || tag <= bis);
}

public sealed record MassnahmeBestaetigung(long Id, long MassnahmeId, long BewohnerId, DateOnly Tag, Schicht Schicht, MassnahmeErgebnis Ergebnis,
    string Grund, int Minuten, string Bemerkung, string Kuerzel, DateTime Zeit, int Lebensbereich, string Titel, long? BerichtId = null);

// Eine fällige Durchführung im Dienst; Bestaetigung ist gesetzt, sobald jemand sie bestätigt hat.
public sealed record MassnahmeTermin(Massnahme Massnahme, MassnahmeFassung Fassung, SchichtTermin Dienst, MassnahmeBestaetigung? Bestaetigung)
{
    public bool Offen => Bestaetigung is null;
    public DateTime? Uhrzeit => Fassung.Uhrzeit is TimeOnly u ? DienstUhrzeit(Dienst, u) : null;

    // Die Nacht reicht über Mitternacht: Uhrzeiten vor Dienstbeginn gehören zum nächsten Kalendertag.
    private static DateTime DienstUhrzeit(SchichtTermin d, TimeOnly u)
    {
        var zeit = d.Tag.ToDateTime(u);
        return zeit < d.Beginn ? zeit.AddDays(1) : zeit;
    }
}

// Ein Lebensbereich in der Akte: Berichte im Quartal, letzter Bericht, nächste Reflexion.
public sealed record LebensbereichStand(int Nr, int BerichteImQuartal, DateTime? LetzterBericht, DateOnly NaechsteReflexion, Marke? Marke);

public static class MassnahmenRegeln
{
    public const string ArtBericht = "Maßnahme";   // Berichtsart der Bestätigungen
    public const int KeinBerichtWochenVorgabe = 4, MinutenHoechstens = 600;
    public static readonly string[] ArtenVorgabe = ["Anleitung", "Begleitung", "Gespräch", "Kontrolle", "Übernahme", "Sonstiges"];
    public static readonly string[] VorlagenVorgabe =
        ["Zimmerhygiene", "Wäschepflege", "Körperpflege anleiten", "Tagesstruktur besprechen", "Einkauf begleiten", "Geldverwaltung üben"];
    public const string OhneZiel = "ohne Ziel";
    public const string MedikamentFrage = "Ist das ärztlich verordnet? Dann bitte im Medikamentenplan eintragen – verordnete Medikamente " +
                                          "stehen nur dort, nicht als Maßnahme.";
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string RhythmusText(MassnahmeFassung f) => f.Rhythmus switch
    {
        MassnahmeRhythmus.Taeglich => "täglich",
        MassnahmeRhythmus.Wochentage => string.Join(", ", f.Wochentage.Order().Select(Wochentag)),
        MassnahmeRhythmus.AlleNWochen => $"alle {f.AlleWochen} Wochen {(f.Wochentage.Count > 0 ? Wochentag(f.Wochentage[0]) : "")}".TrimEnd(),
        MassnahmeRhythmus.Monatlich => $"monatlich am {f.MonatsTag}.",
        _ => "bei Bedarf",
    };

    public static string Wochentag(DayOfWeek t) => De.DateTimeFormat.GetAbbreviatedDayName(t).TrimEnd('.');

    public static string ErgebnisText(MassnahmeErgebnis e) => e switch
    {
        MassnahmeErgebnis.Durchgefuehrt => "durchgeführt",
        MassnahmeErgebnis.Abgelehnt => "abgelehnt",
        _ => "nicht möglich",
    };

    public static string Dauer(int minuten) =>
        minuten < 60 ? $"{minuten} Min." : minuten % 60 == 0 ? $"{minuten / 60} Std." : $"{minuten / 60} Std. {minuten % 60} Min.";

    // Wann im Dienst: Uhrzeit oder „im Frühdienst“.
    public static string WannText(MassnahmeFassung f) =>
        f.Rhythmus == MassnahmeRhythmus.BeiBedarf ? "bei Bedarf" : f.Uhrzeit is TimeOnly u ? $"{u:HH\\:mm} Uhr" : $"im {BerichtRegeln.SchichtText(f.Schicht)}";

    // Ist die Fassung an diesem Tag dran (ohne Blick auf Dienst und Ende)?
    public static bool AmTag(MassnahmeFassung f, DateOnly tag) => f.Rhythmus switch
    {
        MassnahmeRhythmus.Taeglich => true,
        MassnahmeRhythmus.Wochentage => f.Wochentage.Contains(tag.DayOfWeek),
        MassnahmeRhythmus.AlleNWochen => f.Wochentage.Count > 0 && tag.DayOfWeek == f.Wochentage[0] && WochenSeit(f.GiltAb, tag) % Math.Max(1, f.AlleWochen) == 0,
        MassnahmeRhythmus.Monatlich => tag.Day == f.MonatsTag,
        _ => false,
    };

    // Ganze Wochen zwischen den Montagen der beiden Wochen (so zählt auch eine Woche mit, die mitten drin beginnt).
    private static int WochenSeit(DateOnly ab, DateOnly tag)
    {
        static DateOnly Montag(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
        return (Montag(tag).DayNumber - Montag(ab).DayNumber) / 7;
    }

    // Die fälligen Durchführungen im Dienst, mit den schon vorhandenen Bestätigungen; offene nach Uhrzeit oben.
    public static List<MassnahmeTermin> Termine(IEnumerable<Massnahme> massnahmen, SchichtTermin dienst, IEnumerable<MassnahmeBestaetigung> bestaetigungen)
    {
        var bestaetigt = bestaetigungen.Where(b => b.Tag == dienst.Tag && b.Schicht == dienst.Schicht).ToLookup(b => b.MassnahmeId);
        var liste = new List<MassnahmeTermin>();
        foreach (var m in massnahmen)
        {
            if (!m.Laeuft(dienst.Tag) || m.Am(dienst.Tag) is not MassnahmeFassung f) continue;
            if (f.Rhythmus == MassnahmeRhythmus.BeiBedarf || f.Schicht != dienst.Schicht || !AmTag(f, dienst.Tag)) continue;
            liste.Add(new MassnahmeTermin(m, f, dienst, bestaetigt[m.Id].FirstOrDefault()));
        }
        return [.. liste.OrderBy(t => t.Offen ? 0 : 1).ThenBy(t => t.Uhrzeit ?? DateTime.MaxValue).ThenBy(t => t.Fassung.Titel, StringComparer.Create(De, true))];
    }

    // Prüfungen beim Speichern einer Fassung; Rückgabe: Fehlertexte.
    public static List<string> Pruefen(MassnahmeFassung f)
    {
        var fehler = new List<string>();
        if (f.Titel.Trim().Length == 0) fehler.Add("Bitte einen Titel eingeben.");
        if (f.Lebensbereich is < 1 or > 5) fehler.Add("Bitte den Lebensbereich wählen.");
        if (f.Art.Trim().Length == 0) fehler.Add("Bitte die Art wählen.");
        if (f.Minuten is < 0 or > MinutenHoechstens) fehler.Add($"Zeitwert: 0 bis {MinutenHoechstens} Minuten.");
        if (f.Rhythmus == MassnahmeRhythmus.Wochentage && f.Wochentage.Count == 0) fehler.Add("Bitte mindestens einen Wochentag wählen.");
        if (f.Rhythmus == MassnahmeRhythmus.AlleNWochen && (f.Wochentage.Count != 1 || f.AlleWochen is < 2 or > 8))
            fehler.Add("„Alle n Wochen“: einen Wochentag und 2 bis 8 Wochen wählen.");
        if (f.Rhythmus == MassnahmeRhythmus.Monatlich && f.MonatsTag is < 1 or > 28) fehler.Add("Monatlich: Tag 1 bis 28.");
        if (f.Uhrzeit is TimeOnly u && !InDienst(f.Schicht, u)) fehler.Add($"Die Uhrzeit liegt nicht im {BerichtRegeln.SchichtText(f.Schicht)}.");
        if (f.GiltBis is DateOnly bis && bis < f.GiltAb) fehler.Add("„gültig bis“ liegt vor „gültig ab“.");
        return fehler;
    }

    private static bool InDienst(Schicht s, TimeOnly u)
    {
        var z = BerichtRegeln.Zeiten;
        TimeOnly beginn = z.Beginn(s), ende = z.Beginn(s switch { Schicht.Frueh => Schicht.Spaet, Schicht.Spaet => Schicht.Nacht, _ => Schicht.Frueh });
        return beginn < ende ? u >= beginn && u < ende : u >= beginn || u < ende;
    }

    public static string? BestaetigungPruefen(MassnahmeErgebnis e, string grund, int minuten)
    {
        if (e != MassnahmeErgebnis.Durchgefuehrt && grund.Trim().Length == 0) return "Bitte kurz den Grund angeben.";
        if (minuten is < 0 or > MinutenHoechstens) return $"Zeit: 0 bis {MinutenHoechstens} Minuten.";
        return null;
    }

    // Der kurze Eintrag in der Dokumentation (Berichtsart „Maßnahme“).
    public static string BerichtText(string titel, MassnahmeErgebnis e, string grund, int minuten, string bemerkung)
    {
        string text = $"Maßnahme „{titel}“: {ErgebnisText(e)}";
        if (e != MassnahmeErgebnis.Durchgefuehrt && grund.Trim().Length > 0) text += $" ({grund.Trim()})";
        if (e == MassnahmeErgebnis.Durchgefuehrt && minuten > 0) text += $", {Dauer(minuten)}";
        if (!text.EndsWith('.')) text += ".";
        return bemerkung.Trim().Length > 0 ? $"{text} {bemerkung.Trim()}" : text;
    }

    // In der Berichtsliste je Tag zusammengefasst: „Maßnahmen: 3 durchgeführt, 1 abgelehnt“.
    public static string Zusammenfassung(IEnumerable<string> texte)
    {
        var zahlen = Enum.GetValues<MassnahmeErgebnis>().Select(e => (e, n: texte.Count(t => t.Contains($"“: {ErgebnisText(e)}", StringComparison.Ordinal))))
            .Where(x => x.n > 0).Select(x => $"{x.n} {ErgebnisText(x.e)}");
        return $"Maßnahmen: {string.Join(", ", zahlen)}";
    }

    // Reflexion: je Maßnahme eine Zeile („Zimmerhygiene: 11 von 13 durchgeführt, 2 abgelehnt, 3 Std. 40 Min.“). Nur gezählt.
    public static List<(int Lebensbereich, string Zeile)> ReflexionZeilen(IEnumerable<MassnahmeBestaetigung> bestaetigungen) =>
        [.. bestaetigungen.GroupBy(b => (b.Lebensbereich, b.MassnahmeId)).OrderBy(g => g.Key.Lebensbereich).ThenBy(g => g.First().Titel, StringComparer.Create(De, true))
            .Select(g =>
            {
                int gesamt = g.Count(), durch = g.Count(b => b.Ergebnis == MassnahmeErgebnis.Durchgefuehrt);
                var teile = new List<string> { $"{durch} von {gesamt} durchgeführt" };
                foreach (var e in new[] { MassnahmeErgebnis.Abgelehnt, MassnahmeErgebnis.NichtMoeglich })
                    if (g.Count(b => b.Ergebnis == e) is int n and > 0) teile.Add($"{n} {ErgebnisText(e)}");
                int minuten = g.Where(b => b.Ergebnis == MassnahmeErgebnis.Durchgefuehrt).Sum(b => b.Minuten);
                if (minuten > 0) teile.Add(Dauer(minuten));
                return (g.Key.Lebensbereich, $"{g.OrderByDescending(b => b.Zeit).First().Titel}: {string.Join(", ", teile)}");
            })];

    // Die fünf Lebensbereiche in der Akte: geschriebene Berichte (ohne die kurzen Einträge der Maßnahmen) im Quartal,
    // letzter Bericht, nächste Reflexion (Ende des Quartals) und „seit n Wochen kein Bericht“.
    public static List<LebensbereichStand> Lebensbereiche(IEnumerable<Bericht> berichte, DateTime jetzt, int keinBerichtWochen)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        var quartalBeginn = QuartalBeginn(heute);
        var naechste = quartalBeginn.AddMonths(3).AddDays(-1);
        var geschrieben = berichte.Where(b => b.Art != ArtBericht).ToList();
        return [.. Enumerable.Range(1, 5).Select(nr =>
        {
            var eigene = geschrieben.Where(b => b.Bereiche.Contains(nr)).ToList();
            DateTime? letzter = eigene.Count > 0 ? eigene.Max(b => b.Zeitpunkt) : null;
            int imQuartal = eigene.Count(b => DateOnly.FromDateTime(b.Zeitpunkt) >= quartalBeginn);
            Marke? marke = letzter is not DateTime l || jetzt - l > TimeSpan.FromDays(7 * keinBerichtWochen)
                ? new Marke(MarkenArt.NichtErfasst, letzter is DateTime x ? $"seit {(int)((jetzt - x).TotalDays / 7)} Wochen kein Bericht" : "noch kein Bericht")
                : null;
            return new LebensbereichStand(nr, imQuartal, letzter, naechste, marke);
        })];
    }

    // Übersicht (Bauplan 40): Spalte „Maßnahmen“ der Bewohnerliste – „2 offen“, „✓“ (alle bestätigt) oder leer.
    public static string ListenText(IEnumerable<MassnahmeTermin> termine)
    {
        var liste = termine.ToList();
        int offen = liste.Count(t => t.Offen);
        return liste.Count == 0 ? "" : offen > 0 ? $"{offen} offen" : "✓";
    }

    // „Maßnahmen: 7 von 12 bestätigt“
    public static string StandText(IReadOnlyCollection<MassnahmeTermin> termine) =>
        termine.Count == 0 ? "Maßnahmen: keine in diesem Dienst" : $"Maßnahmen: {termine.Count(t => !t.Offen)} von {termine.Count} bestätigt";

    // Am Dienstende ein Sammeleintrag je Wohnbereich in die Übergabe, nicht jede Maßnahme einzeln.
    public const string UebergabeVerfasser = "AUTO";
    public static string UebergabeText(string bereich, SchichtTermin dienst, IReadOnlyList<(string Name, string Titel)> offen) =>
        $"{bereich} · {offen.Count} {(offen.Count == 1 ? "Maßnahme" : "Maßnahmen")} aus dem {dienst.Text} offen: " +
        string.Join("; ", offen.OrderBy(o => o.Name, StringComparer.Create(De, true)).Select(o => $"{o.Name} – {o.Titel}"));

    public static DateOnly QuartalBeginn(DateOnly tag) => new(tag.Year, (tag.Month - 1) / 3 * 3 + 1, 1);

    // Enthält der Titel ein Medikament aus der Datenbank (Wirkstoff oder Handelsname, ganzes Wort)? Dann nachfragen.
    // Ganze Namen, auch aus mehreren Wörtern („… AL“, „…-ratiopharm“); nicht als Teil eines längeren Wortes.
    public static string? MedikamentImTitel(string titel, IEnumerable<string> namen) =>
        namen.Select(n => n.Trim()).Where(n => n.Length >= 4).FirstOrDefault(n =>
            Regex.IsMatch(titel, $@"(?<![\p{{L}}\d]){Regex.Escape(n)}(?![\p{{L}}\d])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
}
