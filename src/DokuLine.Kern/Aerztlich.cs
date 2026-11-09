using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Ärztliche Informationen: Diagnosen, Allergien mit Art der Reaktion, Hinweise für
// den Notfall. Das Programm zeigt Diagnosen nur an (Akte, Visite, HEB-Bogen) und wertet sie nicht.
public enum DiagnoseArt { Haupt, Neben }

public sealed record Diagnose
{
    public long Id { get; init; }
    public long BewohnerId { get; init; }
    public string Icd { get; init; } = "";
    public required string Bezeichnung { get; init; }
    public DateOnly? Seit { get; init; }
    public DiagnoseArt Art { get; init; }
    public string Arzt { get; init; } = "";
    public string Hinweis { get; init; } = "";
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
    public DateOnly? Beendet { get; init; }
    public string? BeendetKuerzel { get; init; }

    public bool Aktuell => Beendet is null;
    public string Anzeige => (Icd.Length > 0 ? Icd + " " : "") + Bezeichnung;
}

public sealed class DiagnoseEingabe
{
    public string Icd { get; set; } = "";
    public string Bezeichnung { get; set; } = "";
    public string Seit { get; set; } = "";
    public DiagnoseArt Art { get; set; }
    public string Arzt { get; set; } = "";
    public string Hinweis { get; set; } = "";
}

public static partial class DiagnoseRegeln
{
    [GeneratedRegex(@"^[A-Z]\d{2}(\.\d{1,2})?[-+!*]?$")]
    private static partial Regex IcdMuster();

    public static string ArtText(DiagnoseArt art) => art == DiagnoseArt.Haupt ? "Hauptdiagnose" : "Nebendiagnose";

    public static Diagnose AusEingabe(DiagnoseEingabe e, long bewohnerId, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        string icd = (e.Icd ?? "").Trim().ToUpperInvariant();
        if (icd.Length > 0 && !IcdMuster().IsMatch(icd)) fehler.Add("ICD-10: z. B. F20.0 – oder leer lassen.");
        string bezeichnung = (e.Bezeichnung ?? "").Trim();
        if (bezeichnung.Length == 0) fehler.Add("Bezeichnung fehlt.");
        DateOnly? seit = null;
        if (!string.IsNullOrWhiteSpace(e.Seit))
        {
            try { seit = Datum.Lesen(e.Seit, "Seit"); }
            catch (FormatException ex) { fehler.Add(ex.Message); }
            if (seit > DateOnly.FromDateTime(jetzt)) fehler.Add("Seit: Das Datum liegt in der Zukunft.");
        }
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add(RechteRegeln.NichtAngemeldetText);
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Diagnose
        {
            BewohnerId = bewohnerId, Icd = icd, Bezeichnung = bezeichnung, Seit = seit, Art = e.Art,
            Arzt = (e.Arzt ?? "").Trim(), Hinweis = (e.Hinweis ?? "").Trim(), Erstellt = jetzt, Kuerzel = kuerzel,
        };
    }

    // Kurz für Visite und HEB-Bogen: nur die aktuellen, Hauptdiagnosen zuerst.
    public static string Kurz(IEnumerable<Diagnose> diagnosen)
    {
        var aktuelle = diagnosen.Where(d => d.Aktuell).OrderBy(d => d.Art).Select(d => d.Anzeige).ToList();
        return aktuelle.Count == 0 ? "keine eingetragen" : string.Join(" · ", aktuelle);
    }

    // Die einzelnen Stoffe aus dem Allergie-Text („Haselnüsse, Wespenstiche“), für die Art der Reaktion.
    public static List<string> Stoffe(string allergien) =>
        [.. allergien.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct()];
}
