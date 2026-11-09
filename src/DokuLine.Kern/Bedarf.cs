using System.Globalization;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// „Bedarf gegeben …“ (Fachregel: IDEEN.md „Assistenzkraft und Fachkraft“): Jede Gabe einer Bedarfsmedikation
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

// Eingaben aus dem Fenster, noch ungeprüft. Die Gabe gilt für heute (Uhrzeit); kurz nach Mitternacht meint
// eine spätere Uhrzeit gestern.
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

    // „2 in 24 Stunden“ → 2 (in der Einheit der Verordnung, z. B. Stück); ohne Zahl vorne: nicht prüfbar.
    public static int? Hoechstens(string text) =>
        ZahlVorne().Match(text) is { Success: true } m && int.TryParse(m.Groups[1].Value, out int n) && n > 0 ? n : null;

    public static string HoechstensText(Verordnung v) =>
        v.BedarfHoechstens.Length == 0 ? "Höchstens: keine Angabe in der Verordnung – das Programm kann die Menge nicht prüfen."
        : Hoechstens(v.BedarfHoechstens) is null ? $"Höchstens: {v.BedarfHoechstens} – ohne Zahl vorne kann das Programm die Menge nicht prüfen."
        : $"Höchstens: {v.BedarfHoechstens}";

    // Eine gespeicherte Menge als Zahl („0,5“ → 0.5); gespeichert wird nur, was Pruefen als Zahl angenommen hat.
    public static decimal MengeZahl(string menge) =>
        decimal.TryParse(menge.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var m) ? m : 0;

    private static string Zahl(decimal d) => d.ToString("0.##", CultureInfo.GetCultureInfo("de-DE"));

    // „14:30“ → wann: heute; liegt das mehr als 5 Minuten in der Zukunft, aber höchstens 12 Stunden zurück
    // gestern (kurz nach Mitternacht eingetragen). null = keine Uhrzeit.
    public static DateTime? Zeitpunkt(string uhrzeit, DateTime jetzt)
    {
        if (!TimeOnly.TryParseExact(uhrzeit.Trim(), ["HH:mm", "H:mm", "HH.mm", "H.mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var uhr))
            return null;
        var zeit = DateOnly.FromDateTime(jetzt).ToDateTime(uhr);
        if (zeit > jetzt.AddMinutes(5) && zeit.AddDays(-1) >= jetzt.AddHours(-12)) zeit = zeit.AddDays(-1);
        return zeit;
    }

    // Gaben in den 24 Stunden vor einem Zeitpunkt (einschließlich).
    public static List<Bedarfsgabe> Letzte24Stunden(IEnumerable<Bedarfsgabe> gaben, DateTime zeit) =>
        [.. gaben.Where(g => g.Zeit > zeit.AddHours(-24) && g.Zeit <= zeit).OrderBy(g => g.Zeit)];

    // Hinweis, wenn die neue Gabe die Höchstmenge überschreiten würde (dann erst nach Rückfrage eintragen).
    // Gezählt wird die Menge (nicht die Zahl der Gaben), und zwar in jedem 24-Stunden-Fenster, in dem die
    // neue Gabe liegt – auch bei einem Nachtrag, nach dem schon weitere Gaben kamen.
    public static string? Warnung(Verordnung v, IEnumerable<Bedarfsgabe> gaben, DateTime zeit, decimal menge)
    {
        if (Hoechstens(v.BedarfHoechstens) is not int n) return null;
        var liste = gaben.ToList();
        var enden = liste.Where(g => g.Zeit > zeit && g.Zeit < zeit.AddHours(24)).Select(g => g.Zeit).Prepend(zeit).Order();
        foreach (var ende in enden)
        {
            decimal summe = menge + Letzte24Stunden(liste, ende).Sum(g => MengeZahl(g.Menge));
            if (summe > n)
                return $"Achtung: höchstens {v.BedarfHoechstens} – mit dieser Gabe {Menge(Zahl(summe), v.Einheit)} innerhalb von 24 Stunden" +
                       (ende > zeit ? $" (bis zur Gabe um {ende:HH\\:mm})" : "") + ". Bitte mit einer Fachkraft klären.";
        }
        return null;
    }

    // „In den letzten 24 Stunden 2× gegeben: gestern 23:50 (1 Stück, AB), 09:15 (1 Stück, KL mit CD)“ – die letzten
    // 24 Stunden, nicht der Kalendertag: kurz nach Mitternacht zählt die Gabe von eben mit.
    public static string Letzte24Text(IEnumerable<Bedarfsgabe> gaben, DateTime jetzt, string einheit)
    {
        var liste = Letzte24Stunden(gaben, jetzt);
        if (liste.Count == 0) return "In den letzten 24 Stunden noch nicht gegeben.";
        var heute = DateOnly.FromDateTime(jetzt);
        return $"In den letzten 24 Stunden {liste.Count}× gegeben: " + string.Join(", ", liste.Select(g =>
            $"{(DateOnly.FromDateTime(g.Zeit) == heute ? "" : "gestern ")}{g.Zeit:HH\\:mm} ({Menge(g.Menge, einheit)}, {g.Kuerzel}{(g.Absprache is string a ? $" mit {a}" : "")})"));
    }

    // Kurz für die Karte: „in 24 Std. 2× gegeben“ / „in 24 Std. nicht gegeben“.
    public static string Letzte24Kurz(IEnumerable<Bedarfsgabe> gaben, DateTime jetzt) =>
        Letzte24Stunden(gaben, jetzt).Count is int n && n > 0 ? $"in 24 Std. {n}× gegeben" : "in 24 Std. nicht gegeben";

    public static string Menge(string menge, string einheit) =>
        einheit.Length == 0 || einheit == "Stück" ? $"{menge} Stück" : $"{menge} {einheit}";

    // Prüft die Eingabe; ich = wer gibt (angemeldet). fachkraefte: aktive Fachkräfte zur Auswahl.
    public static Bedarfsgabe Pruefen(BedarfEingabe e, Verordnung v, Mitarbeiter? ich, IReadOnlyCollection<Mitarbeiter> fachkraefte, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (ich is null) fehler.Add(RechteRegeln.NichtAngemeldetText);
        if (!v.Bedarf) fehler.Add("Das ist keine Bedarfsverordnung.");
        DateTime zeit = jetzt;
        if (Zeitpunkt(e.Uhrzeit, jetzt) is not DateTime z)
            fehler.Add("Uhrzeit: bitte als hh:mm, z. B. 14:30.");
        else
        {
            zeit = z;
            if (zeit > jetzt.AddMinutes(5)) fehler.Add("Uhrzeit: Die Gabe liegt in der Zukunft.");
        }
        if (v.Bedarf && MedikationRegeln.Stand(v, DateOnly.FromDateTime(zeit)).Zustand != Zustand.Gilt)
            fehler.Add("Diese Bedarfsverordnung gilt zu dieser Zeit nicht (pausiert, abgesetzt oder noch nicht begonnen).");
        string menge = e.Menge.Trim();
        if (!decimal.TryParse(menge.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var m) || m <= 0)
            fehler.Add("Menge: bitte eine Zahl größer als 0, z. B. 1 oder 0,5.");
        string anlass = e.Anlass.Trim();
        if (anlass.Length == 0) fehler.Add("Anlass fehlt.");
        // Es zählt die Qualifikation, nicht die Position.
        string absprache = e.Absprache.Trim();
        var mit = fachkraefte.FirstOrDefault(f => f.Kuerzel.Equals(absprache, StringComparison.OrdinalIgnoreCase) && f.Fachkraft && f.Aktiv);
        if (absprache.Length == 0)
        {
            if (ich is not null && !ich.Fachkraft) fehler.Add(AbspracheText);
        }
        else if (mit is null)
            fehler.Add(KeineFachkraftText);
        else if (ich is not null && mit.Kuerzel.Equals(ich.Kuerzel, StringComparison.OrdinalIgnoreCase))
            fehler.Add("In Absprache mit: bitte eine andere Fachkraft als sich selbst.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Bedarfsgabe
        {
            VerordnungId = v.Id, BewohnerId = v.BewohnerId, Zeit = zeit, Menge = menge, Anlass = anlass, Kuerzel = ich!.Kuerzel,
            Absprache = mit?.Kuerzel, Erstellt = jetzt,   // so geschrieben wie in der Mitarbeiterliste
        };
    }

    // Der Bericht in der Akte: stellt die Angaben zusammen (formuliert nichts dazu, Regel 6).
    public static string BerichtText(Verordnung v, Bedarfsgabe g, string? abspracheName) =>
        $"Bedarfsmedikation gegeben um {g.Zeit:HH\\:mm}: {v.Medikament}{(v.Staerke.Length > 0 ? $" {v.Staerke}" : "")}, {Menge(g.Menge, v.Einheit)}. " +
        $"Anlass: {g.Anlass}." + (g.Absprache is string a ? $" In Absprache mit {abspracheName ?? a} ({a})." : "");
}
