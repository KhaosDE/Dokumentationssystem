using System.Globalization;

namespace DokuLine.Kern;

// Monatsreflexion: je Bewohner und Monat ein kurzer Text je Lebensbereich (Wortlaut wie im
// HEB-Bogen), geschrieben mit den Berichten des Monats daneben. Status Entwurf oder fertig –
// „fertig“ setzt ein Mensch. Jede Änderung ist eine neue Fassung; der HEB-Bogen zeigt später die
// fertigen Reflexionen seines Zeitraums als dritte Quelle.

public sealed record ReflexionFassung
{
    public long BewohnerId { get; init; }
    public DateOnly Monat { get; init; }   // immer der Erste des Monats
    public IReadOnlyList<string> Texte { get; init; } = ["", "", "", "", ""];   // Lebensbereiche 1–5
    public bool Fertig { get; init; }
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
}

public sealed record Reflexion(long BewohnerId, DateOnly Monat, IReadOnlyList<ReflexionFassung> Fassungen)
{
    public ReflexionFassung? Aktuell => Fassungen.Count > 0 ? Fassungen[^1] : null;
    public bool Fertig => Aktuell?.Fertig == true;
    public string Text(int bereich) => Aktuell?.Texte[bereich - 1] ?? "";
}

// Teilnahme an einer Therapie im Monat laut Wochenplan.
public sealed record TherapieImMonat(string Titel, int Termine, int Teilgenommen, int Abgesagt, int Ausgefallen, int Offen);

public static class ReflexionRegeln
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static DateOnly MonatVon(DateOnly tag) => new(tag.Year, tag.Month, 1);
    public static DateOnly MonatBis(DateOnly monat) => MonatVon(monat).AddMonths(1).AddDays(-1);
    public static string MonatText(DateOnly monat) => monat.ToString("MMMM yyyy", De);

    // Geschrieben wird meist für den vergangenen Monat.
    public static DateOnly Vorschlag(DateOnly heute) => MonatVon(heute).AddMonths(-1);

    public static string StatusText(Reflexion r) => r.Aktuell is null ? "noch nicht begonnen" : r.Fertig ? "fertig" : "Entwurf";

    public static ReflexionFassung AusEingabe(long bewohnerId, DateOnly monat, IReadOnlyList<string> texte, bool fertig, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (texte.Count != BerichtRegeln.Lebensbereiche.Length) fehler.Add("Es braucht genau fünf Lebensbereiche.");
        else if (fertig && texte.All(string.IsNullOrWhiteSpace)) fehler.Add("Für „fertig“ bitte mindestens einen Lebensbereich ausfüllen.");
        if (MonatVon(monat) > MonatVon(DateOnly.FromDateTime(jetzt))) fehler.Add("Für einen kommenden Monat gibt es noch keine Reflexion.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new ReflexionFassung
        {
            BewohnerId = bewohnerId, Monat = MonatVon(monat), Texte = texte.Select(t => t.Trim()).ToList(),
            Fertig = fertig, Erstellt = jetzt, Kuerzel = kuerzel.Trim(),
        };
    }

    // Alle Texte als ein Text (für den Konflikt-Hinweis).
    public static string AlsText(ReflexionFassung f) =>
        (f.Fertig ? "fertig" : "Entwurf") + Environment.NewLine + string.Join(Environment.NewLine,
            f.Texte.Select((t, i) => $"{Environment.NewLine}{i + 1}. {BerichtRegeln.Lebensbereiche[i]}:{Environment.NewLine}{(t.Length > 0 ? t : "(leer)")}"));

    // Gleich, wenn Texte und Status gleich sind (dann entsteht keine neue Fassung).
    public static bool Gleich(ReflexionFassung a, ReflexionFassung b) => a.Fertig == b.Fertig && a.Texte.SequenceEqual(b.Texte);

    public static List<string> Verlauf(Reflexion r) =>
        r.Fassungen.Select((f, i) =>
        {
            string was = i == 0 ? "angelegt" :
                f.Fertig && !r.Fassungen[i - 1].Fertig ? "als fertig markiert" :
                !f.Fertig && r.Fassungen[i - 1].Fertig ? "wieder Entwurf" : "geändert";
            int gefuellt = f.Texte.Count(t => t.Length > 0);
            return $"{f.Erstellt:dd.MM.yyyy, HH:mm} · {was} von {f.Kuerzel} · {gefuellt} von 5 Lebensbereichen ausgefüllt";
        }).ToList();

    // Therapien des Monats aus den Wochen des Wochenplans, je Angebot gezählt.
    public static List<TherapieImMonat> Therapien(IEnumerable<Termin> termine) =>
        termine.Where(t => t.Quelle == TerminQuelle.Therapie)
            .GroupBy(t => t.Titel)
            .Select(g => new TherapieImMonat(g.Key, g.Count(),
                g.Count(t => t.Teilnahme == Teilnahme.Teilgenommen), g.Count(t => t.Teilnahme == Teilnahme.Abgesagt),
                g.Count(t => t.Teilnahme == Teilnahme.Ausgefallen), g.Count(t => t.Teilnahme == Teilnahme.Offen)))
            .OrderBy(t => t.Titel, StringComparer.CurrentCulture)
            .ToList();

    public static string TherapieText(TherapieImMonat t) =>
        $"{t.Titel}: {t.Termine} {(t.Termine == 1 ? "Termin" : "Termine")}, {t.Teilgenommen} teilgenommen" +
        (t.Abgesagt > 0 ? $", {t.Abgesagt} abgesagt" : "") +
        (t.Ausgefallen > 0 ? $", {t.Ausgefallen} ausgefallen" : "") +
        (t.Offen > 0 ? $", {t.Offen} ohne Eintrag" : "");
}
