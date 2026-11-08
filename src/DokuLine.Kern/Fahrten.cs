using System.Globalization;

namespace DokuLine.Kern;

// Fahrzeuge und Fahrten (Bauplan Schritt 24): Mit den Firmenwagen werden Bewohner zum Arzt
// gefahren, Ausflüge gemacht oder Sporttherapien begleitet. Je Fahrzeug sieht man „vor Ort“ oder
// „unterwegs seit …, zurück geplant …“. „losgefahren“ und „zurück“ sind eigene Einträge mit
// Uhrzeit und Kürzel; eine Fahrt wird nie überschrieben.

public enum FahrtZweck { Arzttermin, Ausflug, Sporttherapie, Sonstiges }

public sealed record Fahrt
{
    public long Id { get; init; }
    public string Fahrzeug { get; init; } = "";
    public DateTime Von { get; init; }               // geplant
    public DateTime Bis { get; init; }               // geplant
    public FahrtZweck Zweck { get; init; }
    public string Fahrer { get; init; } = "";        // Kürzel
    public IReadOnlyList<long> Bewohner { get; init; } = [];
    public string Ziel { get; init; } = "";
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
    public DateTime? Losgefahren { get; init; }
    public string? LosVon { get; init; }
    public DateTime? Zurueck { get; init; }
    public string? ZurueckVon { get; init; }

    public bool Unterwegs => Losgefahren is not null && Zurueck is null;
}

public sealed class FahrtEingabe
{
    public string Fahrzeug { get; set; } = "";
    public string Datum { get; set; } = "";
    public string Von { get; set; } = "";
    public string Bis { get; set; } = "";
    public FahrtZweck Zweck { get; set; }
    public string Fahrer { get; set; } = "";
    public List<long> Bewohner { get; set; } = [];
    public string Ziel { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

// Wie es einem Fahrzeug gerade geht.
public sealed record FahrzeugStand(string Fahrzeug, string Text, bool Unterwegs, bool Ueberfaellig, Fahrt? Aktuell, Fahrt? Naechste);

public static class FahrtRegeln
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    public static readonly string[] DemoFahrzeuge = ["VW Bus", "Caddy"];

    public static string ZweckText(FahrtZweck z) => z switch
    {
        FahrtZweck.Arzttermin => "Arzttermin",
        FahrtZweck.Ausflug => "Ausflug",
        FahrtZweck.Sporttherapie => "Sporttherapie",
        _ => "Sonstiges",
    };

    public static string ZweckKuerzel(FahrtZweck z) => ZweckText(z).ToLowerInvariant();

    public static FahrtZweck ZweckAus(string s) => s switch
    {
        "arzttermin" => FahrtZweck.Arzttermin,
        "ausflug" => FahrtZweck.Ausflug,
        "sporttherapie" => FahrtZweck.Sporttherapie,
        _ => FahrtZweck.Sonstiges,
    };

    // Baut eine Fahrt; wirft Eingabefehler mit allen Fehlern auf einmal.
    public static Fahrt AusEingabe(FahrtEingabe e, IReadOnlyCollection<string> fahrzeuge, DateTime jetzt)
    {
        var fehler = new List<string>();
        string fahrzeug = e.Fahrzeug.Trim();
        if (fahrzeug.Length == 0) fehler.Add("Bitte ein Fahrzeug wählen.");
        else if (!fahrzeuge.Contains(fahrzeug, StringComparer.OrdinalIgnoreCase)) fehler.Add($"Das Fahrzeug „{fahrzeug}“ gibt es nicht (Einstellungen → Fahrzeuge).");
        if (string.IsNullOrWhiteSpace(e.Fahrer)) fehler.Add("Fahrer (Kürzel) fehlt.");
        else if (e.Fahrer.Trim().Length > Einstellungen.KuerzelLaenge) fehler.Add($"Fahrer: bitte das Kürzel (höchstens {Einstellungen.KuerzelLaenge} Zeichen).");
        if (string.IsNullOrWhiteSpace(e.Ziel)) fehler.Add("Ziel fehlt.");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        DateTime von = default, bis = default;
        try
        {
            var tag = Kern.Datum.Lesen(e.Datum, "Datum");
            von = tag.ToDateTime(BerichtRegeln.UhrzeitLesen(e.Von));
            bis = tag.ToDateTime(BerichtRegeln.UhrzeitLesen(e.Bis));
            if (bis <= von) fehler.Add("„Zurück geplant“ muss nach „los“ liegen.");
        }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Fahrt
        {
            Fahrzeug = fahrzeuge.First(f => f.Equals(fahrzeug, StringComparison.OrdinalIgnoreCase)), Von = von, Bis = bis, Zweck = e.Zweck,
            Fahrer = e.Fahrer.Trim(), Bewohner = e.Bewohner.Distinct().ToList(), Ziel = e.Ziel.Trim(), Erstellt = jetzt, Kuerzel = e.Kuerzel.Trim(),
        };
    }

    // Doppelbuchung: andere Fahrten desselben Fahrzeugs, die sich zeitlich überschneiden (schon
    // zurückgekommene zählen bis zur tatsächlichen Rückkehr).
    public static List<Fahrt> Ueberschneidungen(Fahrt neu, IEnumerable<Fahrt> alle) =>
        alle.Where(f => f.Id != neu.Id && f.Fahrzeug.Equals(neu.Fahrzeug, StringComparison.OrdinalIgnoreCase)
                        && f.Von < neu.Bis && neu.Von < (f.Zurueck ?? f.Bis)).ToList();

    public static string Zeile(Fahrt f) =>
        $"{f.Von:HH\\:mm}–{f.Bis:HH\\:mm} {ZweckText(f.Zweck)} · {f.Ziel} (Fahrer {f.Fahrer})";

    // „vor Ort“ oder „unterwegs seit 09:40, zurück geplant 11:30 (Fahrer AB)“; überschritten: Hinweis.
    public static FahrzeugStand Stand(string fahrzeug, IEnumerable<Fahrt> fahrten, DateTime jetzt)
    {
        var eigene = fahrten.Where(f => f.Fahrzeug.Equals(fahrzeug, StringComparison.OrdinalIgnoreCase)).ToList();
        var unterwegs = eigene.Where(f => f.Unterwegs).OrderByDescending(f => f.Losgefahren).FirstOrDefault();
        var heute = DateOnly.FromDateTime(jetzt);
        var naechste = eigene.Where(f => f.Losgefahren is null && DateOnly.FromDateTime(f.Von) == heute && f != unterwegs)
                             .OrderBy(f => f.Von).FirstOrDefault();
        if (unterwegs is Fahrt u)
        {
            bool ueber = jetzt > u.Bis;
            string text = $"unterwegs seit {u.Losgefahren:HH\\:mm}, zurück geplant {u.Bis:HH\\:mm} (Fahrer {u.Fahrer})" +
                          (ueber ? $" – Rückkehr überschritten" : "");
            return new FahrzeugStand(fahrzeug, text, true, ueber, u, naechste);
        }
        string vorOrt = "vor Ort" + (naechste is Fahrt n ? $" · nächste Fahrt {n.Von:HH\\:mm} ({ZweckText(n.Zweck)})" : "");
        return new FahrzeugStand(fahrzeug, vorOrt, false, false, null, naechste);
    }

    // Mitfahrende Bewohner sind während der Fahrt abwesend.
    public static string AbwesendText(Fahrt f) => $"abwesend: unterwegs mit {f.Fahrzeug} bis {f.Bis:HH\\:mm}";

    // Als Termin im Wochen- und Tagesplan der mitfahrenden Bewohner.
    public static Termin AlsTermin(Fahrt f) => new()
    {
        Tag = DateOnly.FromDateTime(f.Von), Von = TimeOnly.FromDateTime(f.Von), Bis = TimeOnly.FromDateTime(f.Bis),
        Titel = $"Fahrt: {ZweckText(f.Zweck)} ({f.Fahrzeug})", Ort = f.Ziel, Hinweis = $"Fahrer {f.Fahrer}", Quelle = TerminQuelle.Fahrt,
    };

    public static string Langtext(Fahrt f) =>
        $"{f.Fahrzeug}: {f.Von.ToString("ddd dd.MM., HH:mm", De)}–{f.Bis:HH\\:mm} · {ZweckText(f.Zweck)} · {f.Ziel} · Fahrer {f.Fahrer}";
}
