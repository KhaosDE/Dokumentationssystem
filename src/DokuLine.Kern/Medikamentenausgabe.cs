using System.Globalization;

namespace DokuLine.Kern;

// Medikamentenausgabe abhaken (Bauplan 42): je Einnahmezeit eine Liste der Bewohner, die laut Plan etwas bekommen, und je
// Bewohner (oder je Medikament) „ausgegeben“, „nicht abgeholt“, „abgelehnt“, „abwesend“, „mitgegeben“ – mit Kürzel und Uhrzeit.
// Das Programm bestätigt nur die Ausgabe; es prüft keine Einnahme und rechnet nichts. Pausierte, abgesetzte und Bedarfs-
// medikamente stehen nicht in der Liste. Einträge werden nie geändert; ein neuer Eintrag zum selben Medikament gilt.
public enum Einnahmezeit { Morgens, Mittags, Abends, Nachts }

public enum AusgabeStatus { Ausgegeben, NichtAbgeholt, Abgelehnt, Abwesend, Mitgegeben }

public sealed record AusgabeEintrag(long Id, long BewohnerId, DateOnly Tag, Einnahmezeit Zeit, long VerordnungId, AusgabeStatus Status, string Kuerzel,
    DateTime Erfasst, long? BerichtId = null);

// Ein Medikament zu einer Einnahmezeit: Menge laut Plan an diesem Tag; Eintrag = der zuletzt gesetzte Status (oder keiner).
public sealed record AusgabeMedikament(Verordnung Verordnung, string Menge, AusgabeEintrag? Eintrag)
{
    public string Text => $"{Verordnung.Medikament} {Verordnung.Staerke} · {Menge}".Replace("  ", " ");
}

public sealed record AusgabeZeile(Bewohner Bewohner, IReadOnlyList<AusgabeMedikament> Medikamente, bool Abwesend)
{
    // Alle gleich: dieser Status; keiner gesetzt: null (noch offen); gemischt: null mit „teilweise“.
    public AusgabeStatus? Status => Medikamente.Select(m => m.Eintrag?.Status).Distinct().ToList() is [AusgabeStatus s] ? s : null;
    public bool NochOffen => Medikamente.All(m => m.Eintrag is null);
    public bool Teilweise => Status is null && !NochOffen;
    public bool NichtAbgeholt => Medikamente.Any(m => m.Eintrag?.Status == AusgabeStatus.NichtAbgeholt);
    public bool Erledigt => Medikamente.All(m => m.Eintrag is { Status: not AusgabeStatus.NichtAbgeholt });
    public bool Ausgegeben => Medikamente.All(m => m.Eintrag?.Status is AusgabeStatus.Ausgegeben or AusgabeStatus.Mitgegeben);

    public string StandText
    {
        get
        {
            if (NochOffen) return Abwesend ? "noch offen · abwesend laut Ausgangsliste" : "noch offen";
            var letzte = Medikamente.Select(m => m.Eintrag).OfType<AusgabeEintrag>().MaxBy(e => e.Erfasst)!;
            string wer = $"{letzte.Kuerzel} {letzte.Erfasst:HH:mm}";
            if (Status is AusgabeStatus s) return $"{AusgabeRegeln.StatusText(s)} · {wer}";
            int offen = Medikamente.Count(m => m.Eintrag is null);
            return $"teilweise ({string.Join(", ", Medikamente.Where(m => m.Eintrag is not null).GroupBy(m => m.Eintrag!.Status)
                .Select(g => $"{g.Count()} {AusgabeRegeln.StatusText(g.Key)}"))}{(offen > 0 ? $", {offen} offen" : "")}) · {wer}";
        }
    }
}

public static class AusgabeRegeln
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    public static readonly Einnahmezeit[] Zeiten = [Einnahmezeit.Morgens, Einnahmezeit.Mittags, Einnahmezeit.Abends, Einnahmezeit.Nachts];

    public static string ZeitText(Einnahmezeit z) => z switch
    {
        Einnahmezeit.Morgens => "morgens",
        Einnahmezeit.Mittags => "mittags",
        Einnahmezeit.Abends => "abends",
        _ => "nachts",
    };

    public static string StatusText(AusgabeStatus s) => s switch
    {
        AusgabeStatus.Ausgegeben => "ausgegeben",
        AusgabeStatus.NichtAbgeholt => "nicht abgeholt",
        AusgabeStatus.Abgelehnt => "abgelehnt",
        AusgabeStatus.Abwesend => "abwesend",
        _ => "mitgegeben",
    };

    // Welche Einnahmezeit gerade dran ist: morgens 5–11, mittags 11–15, abends 15–21, nachts 21–5 Uhr. Nach Mitternacht
    // gehört „nachts“ noch zum Vortag (die Nachtgabe wird am Abend davor ausgegeben).
    public static (DateOnly Tag, Einnahmezeit Zeit) Jetzt(DateTime jetzt)
    {
        var tag = DateOnly.FromDateTime(jetzt);
        int h = jetzt.Hour;
        return h switch
        {
            < 5 => (tag.AddDays(-1), Einnahmezeit.Nachts),
            < 11 => (tag, Einnahmezeit.Morgens),
            < 15 => (tag, Einnahmezeit.Mittags),
            < 21 => (tag, Einnahmezeit.Abends),
            _ => (tag, Einnahmezeit.Nachts),
        };
    }

    // Welche Einnahmezeiten ein Dienst ausgibt (für den Hinweis am Dienstende und die Übergabe).
    public static Einnahmezeit[] ZeitenDesDienstes(Schicht s) => s switch
    {
        Schicht.Frueh => [Einnahmezeit.Morgens, Einnahmezeit.Mittags],
        Schicht.Spaet => [Einnahmezeit.Abends],
        _ => [Einnahmezeit.Nachts],
    };

    public static string Menge(Dosis d, Einnahmezeit z) => z switch
    {
        Einnahmezeit.Morgens => d.Morgens,
        Einnahmezeit.Mittags => d.Mittags,
        Einnahmezeit.Abends => d.Abends,
        _ => d.Nachts,
    };

    // Steht das Medikament an diesem Tag zu dieser Zeit im Plan (gilt oder Stufenplan, nicht bei Bedarf, Menge über 0)?
    public static string? Faellig(Verordnung v, DateOnly tag, Einnahmezeit z)
    {
        if (v.Bedarf) return null;
        var stand = MedikationRegeln.Stand(v, tag);
        if (stand.Zustand is not (Zustand.Gilt or Zustand.Stufenplan)) return null;
        string menge = Menge(stand.Dosis, z);
        return MedikationRegeln.Menge(menge) > 0m ? menge : null;
    }

    // Die Liste einer Einnahmezeit: wer etwas bekommt, mit dem zuletzt gesetzten Status je Medikament. „Nicht abgeholt“ oben,
    // dann die offenen, dann die erledigten; jeweils nach Zimmer.
    public static List<AusgabeZeile> Zeilen(IEnumerable<Bewohner> bewohner, IReadOnlyDictionary<long, List<Verordnung>> plaene, DateOnly tag, Einnahmezeit zeit,
        IEnumerable<AusgabeEintrag> eintraege, IReadOnlySet<long> abwesend)
    {
        var letzte = eintraege.Where(e => e.Tag == tag && e.Zeit == zeit).GroupBy(e => e.VerordnungId).ToDictionary(g => g.Key, g => g.MaxBy(e => e.Id)!);
        var zeilen = new List<AusgabeZeile>();
        foreach (var b in bewohner)
        {
            var medikamente = plaene.GetValueOrDefault(b.Id!.Value, [])
                .Select(v => Faellig(v, tag, zeit) is string menge ? new AusgabeMedikament(v, menge, letzte.GetValueOrDefault(v.Id)) : null)
                .OfType<AusgabeMedikament>().ToList();
            if (medikamente.Count > 0) zeilen.Add(new AusgabeZeile(b, medikamente, abwesend.Contains(b.Id!.Value)));
        }
        return [.. zeilen.OrderBy(z => z.NichtAbgeholt ? 0 : z.Erledigt ? 2 : 1).ThenBy(z => z.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal)
            .ThenBy(z => z.Bewohner.Bett)];
    }

    // „18 von 21 ausgegeben“ (mitgegeben zählt als ausgegeben).
    public static string Zaehler(IReadOnlyCollection<AusgabeZeile> zeilen) => $"{zeilen.Count(z => z.Ausgegeben)} von {zeilen.Count} ausgegeben";

    // „Abgelehnt“ und „nicht abgeholt“ stehen als kurzer Eintrag in der Dokumentation; „ausgegeben“ nur in der Liste.
    public static bool MitBericht(AusgabeStatus s) => s is AusgabeStatus.Abgelehnt or AusgabeStatus.NichtAbgeholt;

    public static string BerichtText(Einnahmezeit zeit, AusgabeStatus s, IEnumerable<AusgabeMedikament> medikamente) =>
        $"Medikamente {ZeitText(zeit)}: {StatusText(s)} ({string.Join(", ", medikamente.Select(m => m.Text))}).";

    // Am Dienstende ein Sammeleintrag je Wohnbereich: wer seine Medikamente nicht bekommen hat oder noch ohne Eintrag ist.
    public static string UebergabeText(string bereich, SchichtTermin dienst, IReadOnlyList<(Einnahmezeit Zeit, string Name, string Stand)> offen) =>
        $"{bereich} · Medikamente aus dem {dienst.Text} offen: " + string.Join("; ", offen.GroupBy(o => o.Zeit).OrderBy(g => g.Key)
            .Select(g => $"{ZeitText(g.Key)} {string.Join(", ", g.OrderBy(o => o.Name, StringComparer.Create(De, true)).Select(o => $"{o.Name} ({o.Stand})"))}"));

    // Verlauf im Medikamentenplan: „Mo 09.11. morgens: ausgegeben (AB 08:12)“, neueste oben.
    public static List<string> Verlauf(IEnumerable<AusgabeEintrag> eintraege, IReadOnlyDictionary<long, string> medikamente) =>
        [.. eintraege.OrderByDescending(e => e.Tag).ThenByDescending(e => e.Zeit).ThenBy(e => medikamente.GetValueOrDefault(e.VerordnungId)).ThenByDescending(e => e.Id)
            .Select(e => $"{e.Tag.ToString("ddd dd.MM.", De)} {ZeitText(e.Zeit)}: {medikamente.GetValueOrDefault(e.VerordnungId, "?")} – {StatusText(e.Status)} ({e.Kuerzel} {e.Erfasst:HH:mm})")];
}
