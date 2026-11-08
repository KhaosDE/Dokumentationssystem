using System.Globalization;

namespace Dokusystem.Kern;

// Therapie-Bereich: Angebote (Einzel/Gruppe) mit festen Wochenterminen, Bewohner werden
// zugeteilt (ab Datum, optional bis Datum). Zugeteilte Therapien erscheinen im Wochenplan.
// Änderungen gelten „ab Datum …“; frühere Fassungen und Zuteilungen bleiben im Verlauf.

public enum AngebotArt { Gruppe, Einzel }

// Ein Termin in der Regelwoche: Wochentag und Uhrzeit von–bis.
public sealed record Wochentermin(DayOfWeek Tag, TimeOnly Von, TimeOnly Bis)
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    public string Text => $"{De.DateTimeFormat.GetAbbreviatedDayName(Tag)} {Von:HH\\:mm}–{Bis:HH\\:mm}";
    public bool Ueberschneidet(Wochentermin t) => Tag == t.Tag && Von < t.Bis && t.Von < Bis;
}

public sealed record AngebotFassung
{
    public DateOnly GiltAb { get; init; }
    public string Name { get; init; } = "";
    public AngebotArt Art { get; init; }
    public string Therapeut { get; init; } = "";
    public string Ort { get; init; } = "";
    public IReadOnlyList<Wochentermin> Termine { get; init; } = [];
    public int Hoechstens { get; init; }
    public string Hinweis { get; init; } = "";
    public string Anbieter { get; init; } = TherapieRegeln.AnbieterTherapie;   // Schritt 22: „Therapie“ oder ein Wohnbereich
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";

    public string TermineText => string.Join(", ", Termine.Select(t => t.Text));
}

public sealed record Zuteilung
{
    public long Id { get; init; }
    public long AngebotId { get; init; }
    public long BewohnerId { get; init; }
    public DateOnly Ab { get; init; }
    public DateOnly? Bis { get; init; }          // einschließlich; null = offen
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
    public DateOnly? BeendetAb { get; init; }    // eigener Eintrag „beendet ab …“ (letzter Tag davor)
    public string? BeendetVon { get; init; }
    public DateTime? BeendetErstellt { get; init; }

    // Letzter Tag der Teilnahme (aus „bis“ oder „beendet ab“), null = offen.
    public DateOnly? Ende => BeendetAb is DateOnly b ? (Bis is DateOnly bis && bis < b.AddDays(-1) ? bis : b.AddDays(-1)) : Bis;
    public bool GiltAm(DateOnly tag) => Ab <= tag && (Ende is not DateOnly e || tag <= e);
    public bool Ueberlappt(DateOnly ab, DateOnly? bis) => (Ende is not DateOnly e || ab <= e) && (bis is not DateOnly b || Ab <= b);
}

public sealed record Angebot
{
    public long Id { get; init; }
    public IReadOnlyList<AngebotFassung> Fassungen { get; init; } = [];   // nach „gilt ab“, dann Anlage
    public IReadOnlyList<Zuteilung> Zuteilungen { get; init; } = [];

    // Wem das Angebot gehört (letzte Fassung) – danach richten sich die Rechte.
    public string Anbieter => Fassungen[^1].Anbieter;

    // Die Fassung, die an einem Tag gilt (vor der ersten: die erste).
    public AngebotFassung Am(DateOnly tag) => Fassungen.LastOrDefault(f => f.GiltAb <= tag) ?? Fassungen[0];
    public List<Zuteilung> TeilnehmerAm(DateOnly tag) => Zuteilungen.Where(z => z.GiltAm(tag)).ToList();
    public int FreiAm(DateOnly tag) => Math.Max(0, Am(tag).Hoechstens - TeilnehmerAm(tag).Count);
}

// Ein Termin eines Bewohners, der mit einem neuen kollidiert (für die Warnung).
public sealed record Ueberschneidung(string Was, Wochentermin Termin);

public sealed class AngebotEingabe
{
    public string Name { get; set; } = "";
    public AngebotArt Art { get; set; }
    public string Therapeut { get; set; } = "";
    public string Ort { get; set; } = "";
    public List<(DayOfWeek? Tag, string Von, string Bis)> Termine { get; set; } = [];
    public string Hoechstens { get; set; } = "";
    public string Hinweis { get; set; } = "";
    public string Anbieter { get; set; } = TherapieRegeln.AnbieterTherapie;
    public string GiltAb { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

public static class TherapieRegeln
{
    public const string AnbieterTherapie = "Therapie";

    public static readonly DayOfWeek[] Woche =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static string TagName(DayOfWeek t) => CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetDayName(t);
    public static string ArtText(AngebotArt a) => a == AngebotArt.Einzel ? "Einzel" : "Gruppe";

    public static AngebotFassung AusEingabe(AngebotEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Name)) fehler.Add("Name des Angebots fehlt.");
        if (string.IsNullOrWhiteSpace(e.Therapeut)) fehler.Add("Therapeutin oder Therapeut fehlt.");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (string.IsNullOrWhiteSpace(e.Anbieter)) fehler.Add("Anbieter fehlt (Therapie oder ein Wohnbereich).");
        else if (Bereiche.Pruefen(e.Anbieter) is string anbieterFehler) fehler.Add($"Anbieter: {anbieterFehler}");
        var termine = new List<Wochentermin>();
        foreach (var (tag, von, bis) in e.Termine)
        {
            if (tag is null && string.IsNullOrWhiteSpace(von) && string.IsNullOrWhiteSpace(bis)) continue;
            if (tag is not DayOfWeek t) { fehler.Add("Termin: Wochentag fehlt."); continue; }
            try
            {
                var v = BerichtRegeln.UhrzeitLesen(von);
                var b = BerichtRegeln.UhrzeitLesen(bis);
                if (b <= v) fehler.Add($"Termin {TagName(t)}: „bis“ muss nach „von“ liegen.");
                else termine.Add(new Wochentermin(t, v, b));
            }
            catch (FormatException x) { fehler.Add($"Termin {TagName(t)}: {x.Message}"); }
        }
        if (termine.Count == 0 && !fehler.Any(f => f.StartsWith("Termin"))) fehler.Add("Mindestens ein Termin (Wochentag, von, bis).");
        for (int i = 0; i < termine.Count; i++)
            for (int j = i + 1; j < termine.Count; j++)
                if (termine[i].Ueberschneidet(termine[j])) fehler.Add($"Die Termine {termine[i].Text} und {termine[j].Text} überschneiden sich.");
        int hoechstens = 0;
        if (e.Art == AngebotArt.Einzel) hoechstens = 1;
        else if (!int.TryParse(e.Hoechstens.Trim(), out hoechstens) || hoechstens < 1 || hoechstens > 50)
            fehler.Add("Höchste Teilnehmerzahl: bitte eine Zahl von 1 bis 50.");
        DateOnly giltAb = default;
        try { giltAb = Datum.Lesen(e.GiltAb, "Gilt ab"); }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new AngebotFassung
        {
            GiltAb = giltAb, Name = e.Name.Trim(), Art = e.Art, Therapeut = e.Therapeut.Trim(), Ort = e.Ort.Trim(),
            Termine = termine.OrderBy(t => Array.IndexOf(Woche, t.Tag)).ThenBy(t => t.Von).ToList(),
            Hoechstens = hoechstens, Hinweis = e.Hinweis.Trim(), Anbieter = Bereiche.Bereinigen(e.Anbieter), Erstellt = jetzt, Kuerzel = e.Kuerzel.Trim(),
        };
    }

    // Andere Therapietermine des Bewohners, die sich mit den Terminen des Angebots überschneiden,
    // im Zeitraum ab–bis. (Der Wochenplan ergänzt später freie Einträge.)
    public static List<Ueberschneidung> Ueberschneidungen(Angebot neu, long bewohnerId, DateOnly ab, DateOnly? bis, IEnumerable<Angebot> alle)
    {
        var liste = new List<Ueberschneidung>();
        var termine = neu.Am(ab).Termine;
        foreach (var anderes in alle.Where(a => a.Id != neu.Id))
            foreach (var z in anderes.Zuteilungen.Where(z => z.BewohnerId == bewohnerId && z.Ueberlappt(ab, bis)))
                foreach (var t in anderes.Am(z.Ab > ab ? z.Ab : ab).Termine.Where(t => termine.Any(n => n.Ueberschneidet(t))))
                    liste.Add(new Ueberschneidung(anderes.Am(ab).Name, t));
        return liste;
    }

    public static List<string> Verlauf(Angebot a, IReadOnlyDictionary<long, string> namen)
    {
        var liste = new List<(DateTime Zeit, string Text)>();
        for (int i = 0; i < a.Fassungen.Count; i++)
        {
            var f = a.Fassungen[i];
            liste.Add((f.Erstellt, $"{(i == 0 ? "angelegt" : "geändert")}, gilt ab {Datum.Text(f.GiltAb)}: {f.Name} · {ArtText(f.Art)} · {f.Therapeut} · {f.Ort} · {f.TermineText} · höchstens {f.Hoechstens} · Anbieter {f.Anbieter} · von {f.Kuerzel}"));
        }
        foreach (var z in a.Zuteilungen)
        {
            string name = namen.GetValueOrDefault(z.BewohnerId, "?");
            liste.Add((z.Erstellt, $"{name} zugeteilt ab {Datum.Text(z.Ab)}" + (z.Bis is DateOnly b ? $" bis {Datum.Text(b)}" : "") + $" · von {z.Kuerzel}"));
            if (z.BeendetAb is DateOnly e) liste.Add((z.BeendetErstellt ?? z.Erstellt, $"{name}: Teilnahme beendet ab {Datum.Text(e)} · von {z.BeendetVon}"));
        }
        return liste.OrderBy(x => x.Zeit).Select(x => x.Text).ToList();
    }
}
