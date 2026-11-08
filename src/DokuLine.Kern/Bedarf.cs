using System.Globalization;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// „Bedarf gegeben …“ (Eintrag „Assistenzkraft und Fachkraft“, 0.33.0): Jede Gabe einer Bedarfsmedikation
// mit Uhrzeit, Menge und Anlass. Eine Assistenzkraft gibt Bedarf nur nach Absprache mit einer Fachkraft;
// die steht dann dabei. Jede Gabe steht zugleich als Bericht in der Akte. Nichts wird überschrieben.

public sealed record Bedarfsgabe
{
    public long Id { get; init; }
    public long VerordnungId { get; init; }
    public long BewohnerId { get; init; }
    public DateTime Zeit { get; init; }          // wann gegeben
    public string Menge { get; init; } = "";
    public string Anlass { get; init; } = "";
    public string Kuerzel { get; init; } = "";    // wer gegeben hat (aus der Anmeldung)
    public string? Absprache { get; init; }       // Kürzel der Fachkraft, mit der es abgesprochen war
    public DateTime Erstellt { get; init; }
    public long? BerichtId { get; init; }
}

// Eingaben aus dem Fenster, noch ungeprüft. Die Gabe gilt für heute (Uhrzeit).
public sealed class BedarfEingabe
{
    public long VerordnungId { get; set; }
    public string Uhrzeit { get; set; } = "";
    public string Menge { get; set; } = "";
    public string Anlass { get; set; } = "";
    public string Absprache { get; set; } = "";   // Kürzel; leer = ohne
}

public static partial class BedarfRegeln
{
    public const string AbspracheText = "In Absprache mit: Als Assistenzkraft bitte die Fachkraft wählen, mit der die Gabe abgesprochen ist.";
    public const string KeineFachkraftText = "In Absprache mit: Das muss eine Fachkraft sein.";

    [GeneratedRegex(@"^\s*(\d+)")]
    private static partial Regex ZahlVorne();

    // „2 in 24 Stunden“ → 2; ohne Zahl vorne: keine Grenze bekannt.
    public static int? Hoechstens(string text) =>
        ZahlVorne().Match(text) is { Success: true } m && int.TryParse(m.Groups[1].Value, out int n) && n > 0 ? n : null;

    // Gaben in den 24 Stunden vor einem Zeitpunkt (einschließlich).
    public static List<Bedarfsgabe> Letzte24Stunden(IEnumerable<Bedarfsgabe> gaben, DateTime zeit) =>
        [.. gaben.Where(g => g.Zeit > zeit.AddHours(-24) && g.Zeit <= zeit).OrderBy(g => g.Zeit)];

    // Hinweis, wenn die Höchstmenge schon erreicht ist (dann erst nach Rückfrage eintragen).
    public static string? Warnung(Verordnung v, IEnumerable<Bedarfsgabe> gaben, DateTime zeit)
    {
        if (Hoechstens(v.BedarfHoechstens) is not int n) return null;
        int schon = Letzte24Stunden(gaben, zeit).Count;
        return schon >= n
            ? $"Achtung: höchstens {v.BedarfHoechstens} – in den 24 Stunden davor schon {schon}× gegeben. Bitte mit einer Fachkraft klären."
            : null;
    }

    // „Heute 2× gegeben: 09:15 (1 Stück, AB), 14:30 (1 Stück, KL mit CD)“
    public static string HeuteText(IEnumerable<Bedarfsgabe> gaben, DateOnly heute, string einheit)
    {
        var liste = gaben.Where(g => DateOnly.FromDateTime(g.Zeit) == heute).OrderBy(g => g.Zeit).ToList();
        if (liste.Count == 0) return "Heute noch nicht gegeben.";
        return $"Heute {liste.Count}× gegeben: " + string.Join(", ", liste.Select(g =>
            $"{g.Zeit:HH\\:mm} ({Menge(g.Menge, einheit)}, {g.Kuerzel}{(g.Absprache is string a ? $" mit {a}" : "")})"));
    }

    // Kurz für die Karte: „heute 2× gegeben“ / „heute noch nicht gegeben“.
    public static string HeuteKurz(IEnumerable<Bedarfsgabe> gaben, DateOnly heute) =>
        gaben.Count(g => DateOnly.FromDateTime(g.Zeit) == heute) is int n && n > 0 ? $"heute {n}× gegeben" : "heute noch nicht gegeben";

    public static string Menge(string menge, string einheit) =>
        einheit.Length == 0 || einheit == "Stück" ? $"{menge} Stück" : $"{menge} {einheit}";

    // Prüft die Eingabe; ich = wer gibt (angemeldet). fachkraefte: aktive Fachkräfte zur Auswahl.
    public static Bedarfsgabe Pruefen(BedarfEingabe e, Verordnung v, Mitarbeiter? ich, IReadOnlyCollection<Mitarbeiter> fachkraefte, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (ich is null) fehler.Add(RechteRegeln.NichtAngemeldetText);
        if (!v.Bedarf) fehler.Add("Das ist keine Bedarfsverordnung.");
        var heute = DateOnly.FromDateTime(jetzt);
        if (v.Bedarf && MedikationRegeln.Stand(v, heute).Zustand != Zustand.Gilt)
            fehler.Add("Diese Bedarfsverordnung gilt heute nicht (pausiert, abgesetzt oder noch nicht begonnen).");
        DateTime zeit = jetzt;
        if (!TimeOnly.TryParseExact(e.Uhrzeit.Trim(), ["HH:mm", "H:mm", "HH.mm", "H.mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var uhr))
            fehler.Add("Uhrzeit: bitte als hh:mm, z. B. 14:30.");
        else
        {
            zeit = heute.ToDateTime(uhr);
            if (zeit > jetzt.AddMinutes(5)) fehler.Add("Uhrzeit: Die Gabe liegt in der Zukunft.");
        }
        string menge = e.Menge.Trim();
        if (!decimal.TryParse(menge.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var m) || m <= 0)
            fehler.Add("Menge: bitte eine Zahl größer als 0, z. B. 1 oder 0,5.");
        string anlass = e.Anlass.Trim();
        if (anlass.Length == 0) fehler.Add("Anlass fehlt.");
        string absprache = e.Absprache.Trim().ToUpperInvariant();
        bool fachkraft = ich is not null && (ich.Fachkraft || RechteRegeln.IstAdministrator(ich));
        if (absprache.Length == 0)
        {
            if (ich is not null && !fachkraft) fehler.Add(AbspracheText);
        }
        else if (!fachkraefte.Any(f => f.Kuerzel.Equals(absprache, StringComparison.OrdinalIgnoreCase) && f.Fachkraft && f.Aktiv))
            fehler.Add(KeineFachkraftText);
        else if (ich is not null && absprache.Equals(ich.Kuerzel, StringComparison.OrdinalIgnoreCase))
            fehler.Add("In Absprache mit: bitte eine andere Fachkraft als sich selbst.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Bedarfsgabe
        {
            VerordnungId = v.Id, BewohnerId = v.BewohnerId, Zeit = zeit, Menge = menge, Anlass = anlass, Kuerzel = ich!.Kuerzel,
            Absprache = absprache.Length > 0 ? absprache : null, Erstellt = jetzt,
        };
    }

    // Der Bericht in der Akte: stellt die Angaben zusammen (formuliert nichts dazu, Regel 6).
    public static string BerichtText(Verordnung v, Bedarfsgabe g, string? abspracheName) =>
        $"Bedarfsmedikation gegeben um {g.Zeit:HH\\:mm}: {v.Medikament}{(v.Staerke.Length > 0 ? $" {v.Staerke}" : "")}, {Menge(g.Menge, v.Einheit)}. " +
        $"Anlass: {g.Anlass}." + (g.Absprache is string a ? $" In Absprache mit {abspracheName ?? a} ({a})." : "");
}
