using System.Globalization;

namespace DokuLine.Kern;

// Abgängigkeitsmeldung (Bauplan 34): wenn ein Bewohner vermisst wird – Meldung mit Personenbeschreibung, Liste „wer wurde
// informiert“, Meldeblatt für die Polizei, „Zurück“ mit „wer wurde entwarnt“. Ab der Meldung die Marke „abgängig seit …“
// und ein wichtiger Bericht mit Übergabe. Nichts wird geändert; alles danach sind Ereignisse.
public sealed record Personenbeschreibung
{
    public string Groesse { get; init; } = "";
    public string Statur { get; init; } = "";
    public string Haarfarbe { get; init; } = "";
    public string Augenfarbe { get; init; } = "";
    public string Brille { get; init; } = "";
    public string Merkmale { get; init; } = "";
}

public sealed record AbgaengigkeitMeldung
{
    public DateTime VermisstSeit { get; init; }
    public string ZuletztWo { get; init; } = "";
    public DateTime? ZuletztWann { get; init; }
    public string ZuletztVon { get; init; } = "";
    public string Kleidung { get; init; } = "";
    public string Gefaehrdung { get; init; } = "";
    public string Orte { get; init; } = "";
    public string Kontakte { get; init; } = "";
}

public sealed record Benachrichtigung(string Stelle, DateTime Zeit, string Kuerzel, string Hinweis);

public sealed record Abgaengigkeit(long Id, long BewohnerId, AbgaengigkeitMeldung Meldung, DateTime Gemeldet, string Kuerzel,
    IReadOnlyList<Benachrichtigung> Informiert, DateTime? Zurueck, string Zustand, IReadOnlyList<Benachrichtigung> Entwarnt)
{
    public bool Offen => Zurueck is null;
}

public static class AbgaengigkeitRegeln
{
    public const string Berichtsart = DokuRegeln.ArtAbgaengigkeit;
    public const int BeschreibungMonate = 12, WartezeitVorgabe = 30;
    public static readonly string[] StellenVorgabe = ["Leitung bzw. Rufbereitschaft", "Polizei (110)", "Betreuer", "Angehörige", "Klinik", "Gericht"];
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string MarkeText(Abgaengigkeit a) => $"abgängig seit {a.Meldung.VermisstSeit.ToString("dd.MM. HH:mm", De)}";

    public static Marke Marke(Abgaengigkeit a) => new(MarkenArt.Abgaengig, MarkeText(a));

    // Ausgangsliste: Rückkehrzeit mehr als die Wartezeit überschritten → Hinweis mit Frage.
    public static bool WartezeitVorbei(TimeSpan zurueckBis, TimeSpan jetzt, int wartezeitMinuten) =>
        jetzt - zurueckBis >= TimeSpan.FromMinutes(wartezeitMinuten);

    public static string UeberschrittenText(TimeSpan zurueckBis) =>
        $"Rückkehrzeit überschritten seit {zurueckBis.ToString("hh\\:mm", CultureInfo.InvariantCulture)} – Abgängigkeit melden?";

    public static string? WartezeitPruefen(int minuten) =>
        minuten is >= 5 and <= 240 ? null : "Wartezeit bis zum Hinweis „Abgängigkeit melden?“: 5 bis 240 Minuten.";

    public static List<string> Pruefen(AbgaengigkeitMeldung m, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (m.Kleidung.Trim().Length == 0) fehler.Add("Kleidung heute fehlt (Pflichtangabe für die Suche).");
        if (m.VermisstSeit > jetzt.AddMinutes(5)) fehler.Add("„Vermisst seit“ liegt in der Zukunft.");
        if (m.ZuletztWann is DateTime w && w > jetzt.AddMinutes(5)) fehler.Add("„Zuletzt gesehen“ liegt in der Zukunft.");
        return fehler;
    }

    // Die Liste zum Abhaken: Stellen aus den Einstellungen, bei Forensik/FOSA deren Zuständige als feste Zeilen.
    public static List<string> Stellen(IEnumerable<string> aus_einstellungen, Zustaendigkeit? z, Func<long?, string> person, DateOnly heute)
    {
        var liste = aus_einstellungen.ToList();
        if (ZustaendigkeitRegeln.InForensik(z)) liste.AddRange(z!.ForensikPersonen.Select(p => $"Forensik: {person(p)}"));
        if (ZustaendigkeitRegeln.InFosa(z, heute)) liste.AddRange(z!.FosaPersonen.Select(p => $"FOSA: {person(p)}"));
        return liste;
    }

    // Personenbeschreibung älter als 12 Monate: Marke „Personenbeschreibung prüfen“.
    public static Marke? BeschreibungMarke(AngabeFassung? f, DateOnly heute) =>
        f is null ? new Marke(MarkenArt.NichtErfasst, "Personenbeschreibung nicht erfasst")
        : DateOnly.FromDateTime(f.Geaendert).AddMonths(BeschreibungMonate) < heute ? new Marke(MarkenArt.NichtErfasst, "Personenbeschreibung älter als 12 Monate")
        : null;

    public static string Berichttext(AbgaengigkeitMeldung m) =>
        $"Abgängigkeit gemeldet: vermisst seit {m.VermisstSeit.ToString("dd.MM.yyyy HH:mm", De)}" +
        (m.ZuletztWo.Trim().Length > 0 ? $", zuletzt gesehen: {m.ZuletztWo.Trim()}" + (m.ZuletztWann is DateTime w ? $" um {w:HH:mm}" : "") +
                                          (m.ZuletztVon.Trim().Length > 0 ? $" (von {m.ZuletztVon.Trim()})" : "") : "") +
        $". Kleidung: {m.Kleidung.Trim()}." + (m.Gefaehrdung.Trim().Length > 0 ? $" Gefährdung: {m.Gefaehrdung.Trim()}." : "");

    public static string Zuruecktext(DateTime zurueck, string zustand) =>
        $"Zurück um {zurueck.ToString("dd.MM.yyyy HH:mm", De)}." + (zustand.Trim().Length > 0 ? $" Zustand: {zustand.Trim()}." : "");

    // Vorschlag für „Kontakte“: Betreuer, Zuständige, Angehörige aus der Akte.
    public static string KontakteVorschlag(UeberleitungDaten d)
    {
        var z = UeberleitungRegeln.BetreuungZeilen(d);
        z.AddRange(AngabenRegeln.Zeilen(Angabe.Angehoerige, d.Angaben.GetValueOrDefault(Angabe.Angehoerige), d.Person, d.Heute)
            .Select(x => $"{x.Beschriftung}: {x.Wert}"));
        return string.Join(Environment.NewLine, z);
    }

    // Was der Bewohner laut Plan regelmäßig braucht (für die Gefährdung).
    public static List<string> BenoetigteMedikamente(UeberleitungDaten d) =>
        [.. d.Medikation.Where(s => s.Zustand == Zustand.Gilt && !s.Verordnung.Bedarf).Select(s => $"{s.Verordnung.Medikament} {s.Verordnung.Staerke}".Trim()).Distinct()];

    public static string BeschreibungKurz(AngabeFassung? f) =>
        f is null ? AngabenRegeln.NichtErfasst
        : f.Lesen<Personenbeschreibung>() is Personenbeschreibung p
            ? string.Join(" · ", new[] { p.Groesse, p.Statur, p.Haarfarbe, p.Augenfarbe, p.Brille }.Where(x => x.Trim().Length > 0).DefaultIfEmpty("eingetragen"))
              + $" (Stand {Datum.Text(DateOnly.FromDateTime(f.Geaendert))})"
            : AngabenRegeln.KeineText(Angabe.Personenbeschreibung);

    public static string Titel(Bewohner b) => $"Meldeblatt Abgängigkeit {b.Name}";

    // Meldeblatt für die Polizei: Personenbeschreibung, Bild (Platzhalter), Kleidung, zuletzt gesehen, Gefährdung, Kontakt der Einrichtung.
    public static List<UeberleitungAbschnitt> Meldeblatt(UeberleitungDaten d, Abgaengigkeit a)
    {
        string W(string s) => s.Trim().Length > 0 ? s.Trim() : AngabenRegeln.NichtErfasst;
        var b = d.Bewohner;
        var m = a.Meldung;
        var f = d.Angaben.GetValueOrDefault(Angabe.Personenbeschreibung);
        var p = f?.Lesen<Personenbeschreibung>();
        var medikamente = BenoetigteMedikamente(d);
        return
        [
            new("Vermisste Person", [$"Name: {b.Name}", $"Geburtsdatum: {Datum.Text(b.Geburtsdatum)}", $"Vermisst seit: {m.VermisstSeit.ToString("dd.MM.yyyy HH:mm", De)}"]),
            new("Personenbeschreibung", p is null ? [f is null ? AngabenRegeln.NichtErfasst : AngabenRegeln.KeineText(Angabe.Personenbeschreibung)]
                : [$"Größe: {W(p.Groesse)}", $"Statur: {W(p.Statur)}", $"Haarfarbe: {W(p.Haarfarbe)}", $"Augenfarbe: {W(p.Augenfarbe)}", $"Brille: {W(p.Brille)}",
                   $"Besondere Merkmale: {W(p.Merkmale)}", $"Stand: {Datum.Text(DateOnly.FromDateTime(f!.Geaendert))}"]),
            new("Bild", ["Bild aus der Akte (in der Demo nur ein Platzhalter)"]),
            new("Kleidung heute", [W(m.Kleidung)]),
            new("Zuletzt gesehen", [$"Wo: {W(m.ZuletztWo)}", $"Wann: {(m.ZuletztWann is DateTime w ? w.ToString("dd.MM.yyyy HH:mm", De) : AngabenRegeln.NichtErfasst)}",
                $"Von: {W(m.ZuletztVon)}"]),
            new("Gefährdung", [W(m.Gefaehrdung), medikamente.Count > 0 ? $"Benötigt laut Plan: {string.Join(", ", medikamente)}" : "Regelmäßige Medikamente laut Plan: keine"]),
            new("Mögliche Aufenthaltsorte", [W(m.Orte)]),
            new("Kontakte", [.. W(m.Kontakte).Split('\n').Select(z => z.TrimEnd('\r')).Where(z => z.Length > 0)]),
            new("Kontakt der Einrichtung", [$"{d.Einrichtung.Name}, Bereich {b.Wohnbereich}", d.Einrichtung.Anschrift, $"Telefon Dienstzimmer: {d.Einrichtung.Telefon}"]),
        ];
    }
}
