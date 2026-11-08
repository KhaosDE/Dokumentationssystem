using System.Globalization;

namespace DokuLine.Kern;

// Wochenplan je Bewohner: eine Regelwoche (gilt jede Woche gleich, bis sie „ab Datum …“ geändert
// wird) aus freien Einträgen und den zugeteilten Therapien; dazu einzelne Termine aus dem
// Kalender (ab 0.13.0). Frühere Fassungen bleiben – der HEB-Bogen muss zeigen können, welcher
// Plan in seinem Zeitraum galt. Bei Therapien lässt sich je Tag die Teilnahme eintragen.

public enum Teilnahme { Offen, Teilgenommen, Abgesagt, Ausgefallen }

// Ein freier Eintrag der Regelwoche (z. B. „Einkaufen“, „Wäsche“), gültig ab–bis.
public sealed record PlanEintrag
{
    public long Id { get; init; }
    public long BewohnerId { get; init; }
    public Wochentermin Termin { get; init; } = new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0));
    public string Titel { get; init; } = "";
    public string Ort { get; init; } = "";
    public string Hinweis { get; init; } = "";
    public DateOnly Ab { get; init; }
    public long? Ersetzt { get; init; }           // Id des Eintrags, den dieser „ab Datum“ ablöst
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
    public DateOnly? EndetAb { get; init; }       // „ab Datum nicht mehr“ (eigener Eintrag)
    public string? EndetVon { get; init; }
    public DateTime? EndetErstellt { get; init; }

    public bool GiltAm(DateOnly tag) => Ab <= tag && (EndetAb is not DateOnly e || tag < e);
}

public enum TerminQuelle { Plan, Therapie, Kalender, Fahrt }

// Ein Termin an einem bestimmten Tag der angezeigten Woche.
public sealed record Termin
{
    public DateOnly Tag { get; init; }
    public TimeOnly Von { get; init; }
    public TimeOnly Bis { get; init; }
    public string Titel { get; init; } = "";
    public string Ort { get; init; } = "";
    public string Hinweis { get; init; } = "";
    public string Begleitung { get; init; } = "";   // Tagesplan-Tafel (Schritt 26g): Therapeut, Begleitung des Ausflugs, Fahrzeug und Fahrer
    public TerminQuelle Quelle { get; init; }
    public long? PlanEintragId { get; init; }
    public long? ZuteilungId { get; init; }
    public long? KalenderId { get; init; }
    public Teilnahme Teilnahme { get; init; } = Teilnahme.Offen;
    public bool Ganztags { get; init; }

    public string ZeitText => Ganztags ? "ganztags" : $"{Von:HH\\:mm}–{Bis:HH\\:mm}";
}

public sealed class PlanEingabe
{
    public DayOfWeek? Tag { get; set; }
    public string Von { get; set; } = "";
    public string Bis { get; set; } = "";
    public string Titel { get; set; } = "";
    public string Ort { get; set; } = "";
    public string Hinweis { get; set; } = "";
    public string Ab { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

public static class WochenplanRegeln
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string TeilnahmeText(Teilnahme t) => t switch
    {
        Teilnahme.Teilgenommen => "teilgenommen",
        Teilnahme.Abgesagt => "abgesagt",
        Teilnahme.Ausgefallen => "ausgefallen",
        _ => "offen",
    };

    public static string TeilnahmeKuerzel(Teilnahme t) => t switch
    {
        Teilnahme.Teilgenommen => "teilgenommen",
        Teilnahme.Abgesagt => "abgesagt",
        Teilnahme.Ausgefallen => "ausgefallen",
        _ => "offen",
    };

    public static Teilnahme TeilnahmeAus(string s) => s switch
    {
        "teilgenommen" => Teilnahme.Teilgenommen,
        "abgesagt" => Teilnahme.Abgesagt,
        "ausgefallen" => Teilnahme.Ausgefallen,
        _ => Teilnahme.Offen,
    };

    // Montag der Woche, in der „tag“ liegt.
    public static DateOnly Montag(DateOnly tag) => tag.AddDays(-(((int)tag.DayOfWeek + 6) % 7));

    public static int Kalenderwoche(DateOnly tag) => ISOWeek.GetWeekOfYear(tag.ToDateTime(TimeOnly.MinValue));

    public static string WochenText(DateOnly montag) =>
        $"KW {Kalenderwoche(montag)} · {montag.ToString("dd.MM.", De)}–{montag.AddDays(6).ToString("dd.MM.yyyy", De)}";

    public static PlanEintrag AusEingabe(PlanEingabe e, long bewohnerId, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Titel)) fehler.Add("Was? Titel fehlt.");
        if (e.Tag is null) fehler.Add("Wochentag fehlt.");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        TimeOnly von = default, bis = default;
        try
        {
            von = BerichtRegeln.UhrzeitLesen(e.Von);
            bis = BerichtRegeln.UhrzeitLesen(e.Bis);
            if (bis <= von) fehler.Add("„Bis“ muss nach „von“ liegen.");
        }
        catch (FormatException x) { fehler.Add(x.Message); }
        DateOnly ab = default;
        try { ab = Datum.Lesen(e.Ab, "Gilt ab"); }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new PlanEintrag
        {
            BewohnerId = bewohnerId, Termin = new Wochentermin(e.Tag!.Value, von, bis), Titel = e.Titel.Trim(), Ort = e.Ort.Trim(),
            Hinweis = e.Hinweis.Trim(), Ab = ab, Erstellt = jetzt, Kuerzel = e.Kuerzel.Trim(),
        };
    }

    // Die Termine einer Woche: freie Einträge und Therapien aus der Regelwoche, dazu einzelne
    // (z. B. aus dem Kalender). Sortiert nach Tag und Uhrzeit.
    public static List<Termin> Woche(DateOnly montag, IEnumerable<PlanEintrag> eintraege,
        IEnumerable<(Angebot Angebot, Zuteilung Zuteilung)> therapien,
        IReadOnlyDictionary<(long ZuteilungId, DateOnly Tag), Teilnahme> teilnahmen,
        IEnumerable<Termin>? einzelne = null)
    {
        var liste = new List<Termin>();
        for (int i = 0; i < 7; i++)
        {
            var tag = montag.AddDays(i);
            foreach (var e in eintraege.Where(e => e.GiltAm(tag) && e.Termin.Tag == tag.DayOfWeek))
                liste.Add(new Termin
                {
                    Tag = tag, Von = e.Termin.Von, Bis = e.Termin.Bis, Titel = e.Titel, Ort = e.Ort, Hinweis = e.Hinweis,
                    Quelle = TerminQuelle.Plan, PlanEintragId = e.Id,
                });
            foreach (var (a, z) in therapien.Where(x => x.Zuteilung.GiltAm(tag)))
            {
                var f = a.Am(tag);
                if (f.GiltAb > tag && a.Fassungen[0].GiltAb > tag) continue;   // Angebot beginnt erst später
                foreach (var t in f.Termine.Where(t => t.Tag == tag.DayOfWeek))
                    liste.Add(new Termin
                    {
                        Tag = tag, Von = t.Von, Bis = t.Bis, Titel = f.Name, Ort = f.Ort, Hinweis = f.Therapeut, Begleitung = f.Therapeut,
                        Quelle = TerminQuelle.Therapie, ZuteilungId = z.Id,
                        Teilnahme = teilnahmen.GetValueOrDefault((z.Id, tag), Teilnahme.Offen),
                    });
            }
        }
        if (einzelne is not null) liste.AddRange(einzelne.Where(t => t.Tag >= montag && t.Tag <= montag.AddDays(6)));
        return liste.OrderBy(t => t.Tag).ThenBy(t => t.Ganztags ? 0 : 1).ThenBy(t => t.Von).ToList();
    }

    // Überschneidungen eines (neuen) Plan-Eintrags mit der Regelwoche ab „ab“ (Warnung).
    public static List<string> Ueberschneidungen(PlanEintrag neu, IEnumerable<PlanEintrag> eintraege, IEnumerable<(Angebot Angebot, Zuteilung Zuteilung)> therapien)
    {
        var liste = new List<string>();
        foreach (var e in eintraege.Where(e => e.Id != neu.Ersetzt && e.Id != neu.Id && (e.EndetAb is not DateOnly ende || ende > neu.Ab) && e.Termin.Ueberschneidet(neu.Termin)))
            liste.Add($"{e.Titel} {e.Termin.Text}");
        foreach (var (a, z) in therapien.Where(x => x.Zuteilung.Ueberlappt(neu.Ab, null)))
            foreach (var t in a.Am(z.Ab > neu.Ab ? z.Ab : neu.Ab).Termine.Where(t => t.Ueberschneidet(neu.Termin)))
                liste.Add($"{a.Am(neu.Ab).Name} {t.Text}");
        return liste;
    }

    public static List<string> Verlauf(IEnumerable<PlanEintrag> eintraege)
    {
        var liste = new List<(DateTime Zeit, string Text)>();
        foreach (var e in eintraege)
        {
            liste.Add((e.Erstellt, $"{(e.Ersetzt is null ? "neu" : "geändert")} ab {Datum.Text(e.Ab)}: {e.Titel} {e.Termin.Text}" +
                                   (e.Ort.Length > 0 ? $" · {e.Ort}" : "") + $" · von {e.Kuerzel}"));
            if (e.EndetAb is DateOnly ende && !eintraege.Any(x => x.Ersetzt == e.Id))
                liste.Add((e.EndetErstellt ?? e.Erstellt, $"entfernt ab {Datum.Text(ende)}: {e.Titel} {e.Termin.Text} · von {e.EndetVon}"));
        }
        return liste.OrderBy(x => x.Zeit).Select(x => x.Text).ToList();
    }
}
