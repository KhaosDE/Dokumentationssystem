using System.Globalization;

namespace Dokusystem.Kern;

// Wohnbereichskalender: Termine von Bewohnern, Termine des Wohnbereichs, Heimfahrten (auch über
// mehrere Tage) und Tagesausflüge (Ziel, Treffpunkt, Teilnehmer, Begleitung). Ein Termin kann
// mehreren Bewohnern gehören. Ändern und Absagen sind neue Fassungen (frühere bleiben einsehbar).
// Kein Abgleich mit anderen Kalendern (kein Internet, Regel 6).

public enum KalenderArt { Bewohner, Wohnbereich, Heimfahrt, Ausflug }

public sealed record KalenderFassung
{
    public KalenderArt Art { get; init; }
    public string Titel { get; init; } = "";
    public DateTime Beginn { get; init; }
    public DateTime Ende { get; init; }
    public bool Ganztags { get; init; }
    public string Ort { get; init; } = "";
    public string Hinweis { get; init; } = "";
    public IReadOnlyList<long> Bewohner { get; init; } = [];
    public string Treffpunkt { get; init; } = "";   // Ausflug
    public string Begleitung { get; init; } = "";   // Ausflug
    public bool Abgesagt { get; init; }
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
}

public sealed record KalenderTermin
{
    public long Id { get; init; }
    public IReadOnlyList<KalenderFassung> Fassungen { get; init; } = [];   // älteste zuerst
    public KalenderFassung Aktuell => Fassungen[^1];

    // Liegt der Termin (ganz oder teilweise) an diesem Tag?
    public bool Am(DateOnly tag) =>
        !Aktuell.Abgesagt && DateOnly.FromDateTime(Aktuell.Beginn) <= tag && tag <= KalenderRegeln.LetzterTag(Aktuell);
}

public sealed class KalenderEingabe
{
    public KalenderArt Art { get; set; }
    public string Titel { get; set; } = "";
    public string BeginnDatum { get; set; } = "";
    public string BeginnUhrzeit { get; set; } = "";
    public string EndeDatum { get; set; } = "";
    public string EndeUhrzeit { get; set; } = "";
    public bool Ganztags { get; set; }
    public string Ort { get; set; } = "";
    public string Hinweis { get; set; } = "";
    public List<long> Bewohner { get; set; } = [];
    public string Treffpunkt { get; set; } = "";
    public string Begleitung { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

public enum KalenderAnsicht { Monat, Woche, Tag }

public static class KalenderRegeln
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string ArtText(KalenderArt a) => a switch
    {
        KalenderArt.Bewohner => "Termin",
        KalenderArt.Wohnbereich => "Wohnbereich",
        KalenderArt.Heimfahrt => "Heimfahrt",
        _ => "Tagesausflug",
    };

    public static string ArtKuerzel(KalenderArt a) => a switch
    {
        KalenderArt.Bewohner => "bewohner",
        KalenderArt.Wohnbereich => "wohnbereich",
        KalenderArt.Heimfahrt => "heimfahrt",
        _ => "ausflug",
    };

    public static KalenderArt ArtAus(string s) => s switch
    {
        "wohnbereich" => KalenderArt.Wohnbereich,
        "heimfahrt" => KalenderArt.Heimfahrt,
        "ausflug" => KalenderArt.Ausflug,
        _ => KalenderArt.Bewohner,
    };

    // Letzter Tag, an dem der Termin zu sehen ist (endet er um 0:00, zählt der Tag davor).
    public static DateOnly LetzterTag(KalenderFassung f)
    {
        var ende = DateOnly.FromDateTime(f.Ende);
        return f.Ende.TimeOfDay == TimeSpan.Zero && f.Ende > f.Beginn && !f.Ganztags ? ende.AddDays(-1) : ende;
    }

    public static KalenderFassung AusEingabe(KalenderEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Titel)) fehler.Add(e.Art == KalenderArt.Ausflug ? "Ziel des Ausflugs fehlt." : "Titel fehlt.");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if ((e.Art is KalenderArt.Bewohner or KalenderArt.Heimfahrt or KalenderArt.Ausflug) && e.Bewohner.Count == 0)
            fehler.Add("Bitte mindestens einen Bewohner wählen.");
        if (e.Art == KalenderArt.Heimfahrt && e.Bewohner.Count > 1) fehler.Add("Eine Heimfahrt gehört zu genau einem Bewohner.");
        DateTime beginn = default, ende = default;
        try
        {
            var bd = Datum.Lesen(e.BeginnDatum, "Beginn");
            var ed = string.IsNullOrWhiteSpace(e.EndeDatum) ? bd : Datum.Lesen(e.EndeDatum, "Ende");
            if (e.Ganztags)
            {
                beginn = bd.ToDateTime(TimeOnly.MinValue);
                ende = ed.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                beginn = bd.ToDateTime(BerichtRegeln.UhrzeitLesen(e.BeginnUhrzeit));
                ende = ed.ToDateTime(string.IsNullOrWhiteSpace(e.EndeUhrzeit) ? BerichtRegeln.UhrzeitLesen(e.BeginnUhrzeit).AddHours(1) : BerichtRegeln.UhrzeitLesen(e.EndeUhrzeit));
            }
            if (ende < beginn || (!e.Ganztags && ende == beginn)) fehler.Add("Das Ende liegt vor dem Beginn.");
        }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new KalenderFassung
        {
            Art = e.Art, Titel = e.Titel.Trim(), Beginn = beginn, Ende = ende, Ganztags = e.Ganztags, Ort = e.Ort.Trim(),
            Hinweis = e.Hinweis.Trim(), Bewohner = e.Bewohner.Distinct().ToList(), Treffpunkt = e.Treffpunkt.Trim(),
            Begleitung = e.Begleitung.Trim(), Erstellt = jetzt, Kuerzel = e.Kuerzel.Trim(),
        };
    }

    public static string ZeitText(KalenderFassung f)
    {
        var b = DateOnly.FromDateTime(f.Beginn);
        var l = LetzterTag(f);
        if (f.Ganztags) return b == l ? $"{b.ToString("ddd dd.MM.", De)}, ganztags" : $"{b.ToString("ddd dd.MM.", De)} – {l.ToString("ddd dd.MM.", De)}";
        if (b == DateOnly.FromDateTime(f.Ende)) return $"{b.ToString("ddd dd.MM.", De)}, {f.Beginn:HH\\:mm}–{f.Ende:HH\\:mm} Uhr";
        return $"{f.Beginn.ToString("ddd dd.MM., HH:mm", De)} – {f.Ende.ToString("ddd dd.MM., HH:mm", De)} Uhr";
    }

    // Zeitraum einer Ansicht (erster und letzter Tag).
    public static (DateOnly Von, DateOnly Bis) Zeitraum(KalenderAnsicht a, DateOnly tag) => a switch
    {
        KalenderAnsicht.Tag => (tag, tag),
        KalenderAnsicht.Woche => (WochenplanRegeln.Montag(tag), WochenplanRegeln.Montag(tag).AddDays(6)),
        _ => (new DateOnly(tag.Year, tag.Month, 1), new DateOnly(tag.Year, tag.Month, 1).AddMonths(1).AddDays(-1)),
    };

    public static DateOnly Blaettern(KalenderAnsicht a, DateOnly tag, int richtung) => a switch
    {
        KalenderAnsicht.Tag => tag.AddDays(richtung),
        KalenderAnsicht.Woche => tag.AddDays(7 * richtung),
        _ => tag.AddMonths(richtung),
    };

    public static string ZeitraumText(KalenderAnsicht a, DateOnly tag)
    {
        var (von, bis) = Zeitraum(a, tag);
        return a switch
        {
            KalenderAnsicht.Tag => tag.ToString("dddd, dd. MMMM yyyy", De),
            KalenderAnsicht.Woche => WochenplanRegeln.WochenText(von),
            _ => von.ToString("MMMM yyyy", De),
        };
    }

    // Für den Wochenplan: ein Kalendertermin als Termin an einem Tag.
    public static Termin AlsTermin(KalenderTermin k, DateOnly tag)
    {
        var f = k.Aktuell;
        bool mehrtaegig = DateOnly.FromDateTime(f.Beginn) != LetzterTag(f);
        bool ganztags = f.Ganztags || (mehrtaegig && DateOnly.FromDateTime(f.Beginn) != tag && LetzterTag(f) != tag);
        var von = DateOnly.FromDateTime(f.Beginn) == tag ? TimeOnly.FromDateTime(f.Beginn) : new TimeOnly(0, 0);
        var bis = DateOnly.FromDateTime(f.Ende) == tag ? TimeOnly.FromDateTime(f.Ende) : new TimeOnly(23, 59);
        string titel = f.Art switch
        {
            KalenderArt.Heimfahrt => "Heimfahrt" + (mehrtaegig ? $" bis {f.Ende.ToString("ddd dd.MM., HH:mm", De)}" : ""),
            KalenderArt.Ausflug => $"Ausflug: {f.Titel}",
            _ => f.Titel,
        };
        if (mehrtaegig && f.Art == KalenderArt.Heimfahrt) ganztags = true;
        return new Termin
        {
            Tag = tag, Von = von, Bis = bis, Titel = titel, Ort = f.Art == KalenderArt.Ausflug ? f.Treffpunkt : f.Ort, Hinweis = f.Hinweis,
            Quelle = TerminQuelle.Kalender, KalenderId = k.Id, Ganztags = ganztags,
        };
    }

    public static List<string> Verlauf(KalenderTermin k, IReadOnlyDictionary<long, string> namen)
    {
        var liste = new List<string>();
        for (int i = 0; i < k.Fassungen.Count; i++)
        {
            var f = k.Fassungen[i];
            string was = f.Abgesagt ? "abgesagt" : i == 0 ? "angelegt" : "geändert";
            liste.Add($"{f.Erstellt.ToString("dd.MM.yyyy, HH:mm", De)} · {was} von {f.Kuerzel}: {ArtText(f.Art)} „{f.Titel}“, {ZeitText(f)}" +
                      (f.Bewohner.Count > 0 ? $", {string.Join(", ", f.Bewohner.Select(b => namen.GetValueOrDefault(b, "?")))}" : ""));
        }
        return liste;
    }
}
