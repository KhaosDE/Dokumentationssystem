using System.Globalization;

namespace DokuLine.Kern;

// Übergabe (Teil der Täglichen Dokumentation, Bauplan Schritt 3): Einträge, die die folgenden
// Schichten wissen oder tun müssen. Sie bleiben stehen, bis sie ablaufen oder als erledigt
// markiert werden – auch über mehrere Tage. Verlängern und „erledigt“ sind eigene Ereignisse;
// gelöscht oder überschrieben wird nichts.

public enum UebergabeDauer { NaechsteSchicht, DreiTage, Woche, BisDatum, BisErledigt }

// Was beim Setzen von „Übergabe“ gewählt wird.
public sealed record UebergabeWunsch(UebergabeDauer Dauer = UebergabeDauer.NaechsteSchicht, DateOnly? Bis = null, string Text = "");

public sealed record Kenntnisnahme(string Kuerzel, DateTime Zeit);

public sealed record Uebergabe
{
    public required long Id { get; init; }
    public long? BewohnerId { get; init; }      // null = Wohnbereich allgemein
    public long? BerichtId { get; init; }       // null = eigene Notiz ohne Bericht
    public string Text { get; init; } = "";      // kurzer eigener Übergabe-Satz (darf leer sein, dann zählt der Bericht)
    public required DateTime Erstellt { get; init; }
    public required string Verfasser { get; init; }
    public DateTime? GiltBis { get; init; }      // nach allen Verlängerungen; null = bis erledigt
    public DateTime? Erledigt { get; init; }
    public string? ErledigtVon { get; init; }
    public int Verlaengert { get; init; }        // wie oft verlängert
    public IReadOnlyList<Kenntnisnahme> Gelesen { get; init; } = [];
    public Bericht? Bericht { get; init; }

    public bool Allgemein => BewohnerId is null;

    // Was in der Übergabe steht: der kurze Satz, sonst der Bericht selbst.
    public string Anzeige => Text.Length > 0 ? Text : Bericht?.Text ?? "";
}

public static class UebergabeRegeln
{
    public static readonly string[] DauerTexte = ["nächste Schicht", "3 Tage", "1 Woche", "bis Datum …", "bis erledigt"];

    // Bis wann ein Eintrag gilt. „Nächste Schicht“: bis zum Ende der Schicht nach der, in der er
    // entstand. Tage und Datum: bis zum Ende des Tages. „Bis erledigt“: ohne Ablauf (null).
    public static DateTime? GiltBis(UebergabeDauer dauer, DateTime ab, DateOnly? bis = null) => dauer switch
    {
        UebergabeDauer.NaechsteSchicht => BerichtRegeln.SchichtBeginn(ab).AddHours(16),
        UebergabeDauer.DreiTage => ab.Date.AddDays(4).AddTicks(-1),
        UebergabeDauer.Woche => ab.Date.AddDays(8).AddTicks(-1),
        UebergabeDauer.BisDatum => bis is DateOnly d ? d.ToDateTime(TimeOnly.MaxValue) : throw new Eingabefehler(["Übergabe: Datum fehlt."]),
        _ => null,
    };

    public static void Pruefen(UebergabeWunsch wunsch, DateTime jetzt)
    {
        if (wunsch.Dauer == UebergabeDauer.BisDatum)
        {
            if (wunsch.Bis is not DateOnly bis) throw new Eingabefehler(["Übergabe: bis wann? Datum fehlt."]);
            if (bis < DateOnly.FromDateTime(jetzt)) throw new Eingabefehler(["Übergabe: Das Datum liegt in der Vergangenheit."]);
        }
    }

    public static bool Offen(Uebergabe u, DateTime jetzt) => u.Erledigt is null && (u.GiltBis is null || u.GiltBis >= jetzt);

    public static bool LaeuftHeuteAb(Uebergabe u, DateTime jetzt) => Offen(u, jetzt) && u.GiltBis?.Date == jetzt.Date;

    public static bool GelesenVon(Uebergabe u, string kuerzel) =>
        kuerzel.Length > 0 && u.Gelesen.Any(g => g.Kuerzel.Equals(kuerzel, StringComparison.OrdinalIgnoreCase));

    // „Neu seit deiner letzten Schicht“: nach dem letzten Besuch der Übergabe mit diesem Kürzel
    // entstanden und von dir noch nicht gelesen. Ohne Kürzel gibt es kein „neu“.
    public static bool Neu(Uebergabe u, string kuerzel, DateTime? letzterBesuch) =>
        kuerzel.Length > 0 && !GelesenVon(u, kuerzel) && (letzterBesuch is null || u.Erstellt > letzterBesuch);

    public static string GueltigText(Uebergabe u, DateTime jetzt)
    {
        if (u.Erledigt is DateTime e) return $"erledigt {Zeit(e)} · {u.ErledigtVon}";
        if (u.GiltBis is not DateTime bis) return "bis erledigt";
        if (bis < jetzt) return $"abgelaufen {Zeit(bis)}";
        return $"bis {Zeit(bis)}";
    }

    // Gilt etwas bis Tagesende, reicht das Datum.
    private static string Zeit(DateTime t) =>
        t.ToString(t.TimeOfDay >= new TimeSpan(23, 59, 0) ? "ddd dd.MM." : "ddd dd.MM., HH:mm 'Uhr'", CultureInfo.GetCultureInfo("de-DE"));
}

// Die Wortliste für den Hinweis „Soll das in die Übergabe?“ (daten/uebergabe-woerter.txt, in
// die .exe eingebaut) – ergänzbar über eine gleichnamige Datei im Datenordner.
public static class UebergabeWoerter
{
    public const string Datei = "uebergabe-woerter.txt";

    public static IReadOnlyList<string> Laden()
    {
        var woerter = new List<string>();
        using (var strom = typeof(UebergabeWoerter).Assembly.GetManifestResourceStream("DokuLine.Kern.uebergabe-woerter.txt"))
        using (var leser = new StreamReader(strom ?? throw new InvalidOperationException("Wortliste fehlt im Programm.")))
            woerter.AddRange(Zeilen(leser.ReadToEnd()));
        try
        {
            string eigene = Pfade.ImDatenordner(Datei);
            if (File.Exists(eigene)) woerter.AddRange(Zeilen(File.ReadAllText(eigene)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return woerter.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> Zeilen(string text) =>
        text.Split('\n').Select(z => z.Trim()).Where(z => z.Length > 0 && !z.StartsWith('#'));

    // Welche Wörter der Liste kommen im Text vor (Groß/klein egal, Wortteile genügen)?
    public static List<string> Gefunden(string text, IReadOnlyList<string> woerter) =>
        woerter.Where(w => text.Contains(w, StringComparison.CurrentCultureIgnoreCase)).ToList();
}
