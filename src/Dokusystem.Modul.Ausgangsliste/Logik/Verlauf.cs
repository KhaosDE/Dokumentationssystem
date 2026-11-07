namespace Dokusystem.Modul.Ausgangsliste;

// Der Verlauf je Person (übernommen aus der Ausgangsliste): Status mit Grund, Unterbringung mit
// Datum, Ausgangsanspruch, digitale Ausgänge. Eigene Tabelle (al_verlauf), nur lesen; mit dem
// Bewohner gelöscht. Statt des Windows-Anmeldenamens steht das Kürzel („Angemeldet als …“) dabei.
public static class Verlauf
{
    public static string Unterbringung(Ausgangsliste.Unterbringung u) => u switch
    {
        Ausgangsliste.Unterbringung.Beschluss => "Beschluss",
        Ausgangsliste.Unterbringung.Forensik => "Forensik",
        Ausgangsliste.Unterbringung.NichtErfasst => "nicht erfasst",
        _ => "offen geführt",
    };

    public static string Status(AnwesenheitsStatus s) => s switch
    {
        AnwesenheitsStatus.Ausgangsbeschraenkung => "Ausgangsbeschränkung",
        AnwesenheitsStatus.Abwesenheit => "Abwesenheit",
        _ => "kein Status",
    };

    // „Beschluss (Datum 31.12.2026)“ – beim Beschluss das Ablaufdatum, bei offen geführt die
    // Einverständniserklärung.
    public static string UnterbringungMitDatum(Platz platz) =>
        Unterbringung(platz.Unterbringung) + (platz.UnterbringungDatum is DateTime d ? $" (Datum {d:dd.MM.yyyy})" : "");

    // Leerer Ausgang wird lesbar statt als "".
    public static string Ausgang(string text) => text.Trim() == "" ? "(kein Ausgang)" : $"„{text.Trim()}“";

    // Eine Zeile je Eintrag, neueste oben: „07.11.2026, 14:05 · AB · Status: …“
    public static List<string> Zeilen(IEnumerable<VerlaufEintrag> eintraege) =>
        eintraege.Select((e, i) => (e, i)).OrderByDescending(x => x.e.Zeitpunkt).ThenByDescending(x => x.i).Select(x => x.e)
            .Select(e => $"{e.Zeitpunkt:dd.MM.yyyy, HH:mm} · {(e.Kuerzel.Length > 0 ? e.Kuerzel : "–")} · {e.Text}").ToList();
}

public sealed record VerlaufEintrag(DateTime Zeitpunkt, string Kuerzel, string Text);
