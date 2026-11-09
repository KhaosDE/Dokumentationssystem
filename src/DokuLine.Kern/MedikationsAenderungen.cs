using System.Globalization;

namespace DokuLine.Kern;

// Eine Änderung am Medikamentenplan: neu, abgesetzt, Dosis geändert, Pause, Stufe eines Stufenplans, neue Bedarfsmedikation.
public sealed record Medikamentenaenderung(long BewohnerId, long VerordnungId, string Medikament, string Was, DateOnly Ab,
    string Arzt, DateTime Erstellt)
{
    // „Medikamentenänderung: Quetiapin: Dosis geändert: 1–0–1–0 → 1–0–2–0 ab 09.11.2026, angeordnet von Dr. …“
    public string UebergabeText => $"Medikamentenänderung: {Medikament}: {Was} ab {Datum.Text(Ab)}, angeordnet von {Arzt}.";
}

// Sieben Tage lang ist eine Änderung auffällig (Marke „Medikation geändert“, Zeile im Medikamentenplan, Übersicht): ab dem
// Tag, an dem sie gilt (auch die nächste Stufe), und schon vorher, wenn sie in den letzten Tagen für später eingetragen wurde.
public static class MedikationsAenderungen
{
    public const int Tage = 7;
    public const string MarkeText = "Medikation geändert";

    public static bool Frisch(DateOnly tag, DateOnly heute) => tag <= heute && tag > heute.AddDays(-Tage);

    public static List<Medikamentenaenderung> Liste(IEnumerable<Verordnung> verordnungen, DateOnly heute) =>
        [.. verordnungen.SelectMany(Alle).Where(a => Frisch(a.Ab, heute) || a.Ab > heute && Frisch(DateOnly.FromDateTime(a.Erstellt), heute))
            .OrderByDescending(a => a.Ab).ThenByDescending(a => a.Erstellt)];

    // Alle Änderungen einer Verordnung, älteste zuerst.
    public static IEnumerable<Medikamentenaenderung> Alle(Verordnung v)
    {
        Medikamentenaenderung Neu(string was, DateOnly ab, Anordnung an, DateTime erstellt) =>
            new(v.BewohnerId, v.Id, v.Medikament, was, ab, an.Arzt, erstellt);
        yield return Neu(v.Bedarf ? "neue Bedarfsmedikation" : $"neu verordnet ({v.Dosis.Text})", v.Beginn, v.Anordnung, v.Erstellt);
        foreach (var a in v.Aenderungen)
        {
            if (a.Art != AenderungArt.Stufenplan || a.Stufen.Count == 0)
            {
                yield return Neu(Text(v, a), a.Von, a.Anordnung, a.Erstellt);
                continue;
            }
            for (int i = 0; i < a.Stufen.Count; i++)
                yield return Neu(StufeText(v, a, i), a.Stufen[i].Ab, a.Anordnung, a.Erstellt);
        }
    }

    // Was eine Änderung bewirkt, in Worten (für die Übergabe beim Speichern).
    public static string Text(Verordnung v, VerordnungsAenderung a) => a.Art switch
    {
        AenderungArt.Pause => a.Bis is DateOnly b ? $"pausiert bis {Datum.Text(b)}" : "pausiert bis auf Weiteres",
        AenderungArt.PauseEnde => "Pause beendet",
        AenderungArt.Absetzen => "abgesetzt",
        _ => a.Stufen.Count == 0 ? "Stufenplan" : StufeText(v, a, 0),
    };

    private static string StufeText(Verordnung v, VerordnungsAenderung a, int i)
    {
        var stufe = a.Stufen[i];
        if (stufe.Dosis.IstNull) return "abgesetzt (letzte Stufe)";
        string vorher = (i == 0 ? MedikationRegeln.Stand(v with { Aenderungen = [.. v.Aenderungen.TakeWhile(x => x != a)] }, stufe.Ab.AddDays(-1)).Dosis
            : a.Stufen[i - 1].Dosis).Text;
        return a.Stufen.Count == 1 ? $"Dosis geändert: {vorher} → {stufe.Dosis.Text}"
            : $"Stufe {i + 1} von {a.Stufen.Count}: {vorher} → {stufe.Dosis.Text}";
    }

    // Kurz für die Zeile im Medikamentenplan: „neu seit 09.11.“ oder die letzte Änderung.
    public static string? ZeilenText(Verordnung v, DateOnly heute)
    {
        var frisch = Liste([v], heute).FirstOrDefault();
        if (frisch is null) return null;
        string seit = frisch.Ab.ToString("dd.MM.", CultureInfo.GetCultureInfo("de-DE"));
        return frisch.Was.StartsWith("neu", StringComparison.Ordinal) ? $"neu seit {seit}" : $"{frisch.Was} (ab {seit})";
    }
}
