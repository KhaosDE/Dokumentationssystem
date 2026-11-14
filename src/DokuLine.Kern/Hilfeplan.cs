namespace DokuLine.Kern;

// Hilfeplangespräche (Bauplan 47): je Bewohner letztes Gespräch, nächstes fällig bis (Vorschlag: Ende der Bewilligung im
// Reiter „Kostenträger“, änderbar), Stand offen · Termin vergeben · geführt. Ereignisse werden nie geändert; der Stand ergibt
// sich aus ihnen. Der Termin steht im Kalender (damit auch im Wochenplan und Tagesplan).
public enum HilfeplanStandArt { Offen, TerminVergeben, Gefuehrt }

public sealed record HilfeplanEreignis(long Id, long BewohnerId, string Art, DateOnly Datum, long? KalenderId, string Teilnehmer, long? BerichtId,
    string Kuerzel, DateTime Erfasst);

public sealed record HilfeplanStand(Bewohner Bewohner, DateOnly? LetztesAm, DateOnly? FaelligBis, bool Vorschlag, HilfeplanStandArt Stand,
    KalenderTermin? Termin);

public static class HilfeplanRegeln
{
    public const string ArtBericht = "Hilfeplangespräch";
    public const string Titel = "Hilfeplangespräch";
    public const int HinweisWochen = 12;   // ab dann „fällig in n Wochen“
    public const int DauerVorgabe = 60;

    public static string StandText(HilfeplanStandArt s) => s switch
    {
        HilfeplanStandArt.Offen => "offen",
        HilfeplanStandArt.TerminVergeben => "Termin vergeben",
        _ => "geführt",
    };

    // Vorschlag für „fällig bis“: das Ende der jüngsten Bewilligung; ohne Bewilligung keiner.
    public static DateOnly? Vorschlag(Kostentraeger? k) => k?.Bewilligungen.Count > 0 ? k.Bewilligungen.Max(b => b.Bis) : null;

    // Aus den Ereignissen: letztes „geführt“, die danach gesetzte Fälligkeit (sonst der Vorschlag) und ein Termin danach.
    public static HilfeplanStand Stand(Bewohner b, IReadOnlyList<HilfeplanEreignis> ereignisse, Kostentraeger? kostentraeger,
        IReadOnlyDictionary<long, KalenderTermin> kalender)
    {
        var sortiert = ereignisse.OrderBy(e => e.Erfasst).ThenBy(e => e.Id).ToList();
        var gefuehrt = sortiert.LastOrDefault(e => e.Art == "gefuehrt");
        var danach = gefuehrt is null ? sortiert : sortiert.SkipWhile(e => e != gefuehrt).Skip(1).ToList();
        var faellig = danach.LastOrDefault(e => e.Art == "faellig");
        var termin = danach.LastOrDefault(e => e.Art == "termin" && e.KalenderId is long id && kalender.TryGetValue(id, out var k) && !k.Aktuell.Abgesagt);
        DateOnly? bis = faellig?.Datum;
        bool vorschlag = false;
        if (bis is null && Vorschlag(kostentraeger) is DateOnly v && (gefuehrt is null || v > gefuehrt.Datum)) { bis = v; vorschlag = true; }
        var stand = termin is not null ? HilfeplanStandArt.TerminVergeben : gefuehrt is not null && faellig is null && !vorschlag ? HilfeplanStandArt.Gefuehrt : HilfeplanStandArt.Offen;
        return new HilfeplanStand(b, gefuehrt?.Datum, bis, vorschlag, stand, termin is null ? null : kalender[termin.KalenderId!.Value]);
    }

    // Eine Zeile für den Reiter „Kostenträger“.
    public static string KurzText(HilfeplanStand s) =>
        $"letztes {(s.LetztesAm is DateOnly l ? Datum.Text(l) : "nicht erfasst")} · nächstes fällig bis " +
        (s.FaelligBis is DateOnly f ? Datum.Text(f) + (s.Vorschlag ? " (Vorschlag aus der Bewilligung)" : "") : "nicht erfasst") +
        $" · {StandText(s.Stand)}" + (s.Termin is KalenderTermin t ? $" am {t.Aktuell.Beginn:dd.MM.yyyy HH:mm}" : "");

    // „überfällig“ oder „fällig in n Wochen“ (ab 12 Wochen vorher); mit vergebenem Termin keine Marke.
    public static string? Marke(HilfeplanStand s, DateOnly heute)
    {
        if (s.Stand == HilfeplanStandArt.TerminVergeben || s.FaelligBis is not DateOnly bis) return null;
        if (bis < heute) return "Hilfeplangespräch überfällig";
        int wochen = (bis.DayNumber - heute.DayNumber) / 7;
        return wochen <= HinweisWochen ? $"Hilfeplangespräch fällig in {wochen} {(wochen == 1 ? "Woche" : "Wochen")}" : null;
    }

    // Teilnehmer aus der Akte: Bewohner, Bezugspflege, Betreuer bzw. Forensik/FOSA, Sachbearbeiter des Kostenträgers,
    // Angehörige nur mit Entbindung von der Schweigepflicht.
    public static List<string> TeilnehmerVorschlag(Bewohner b, string? bezugspflege, Betreuer? betreuer, Zustaendigkeit? zustaendig, Kostentraeger? kostentraeger,
        IEnumerable<string> entbundeneAngehoerige)
    {
        var liste = new List<string> { $"{b.Name} (Bewohner)" };
        if (bezugspflege is { Length: > 0 }) liste.Add($"{bezugspflege} (Bezugspflege)");
        if (betreuer is { Keine: false } && betreuer.Name.Trim().Length > 0) liste.Add($"{betreuer.Name} (Betreuer)");
        if (ZustaendigkeitRegeln.InForensik(zustaendig)) liste.Add($"Forensik{(zustaendig!.Klinik.Trim().Length > 0 ? $" ({zustaendig.Klinik.Trim()})" : "")}");
        if (zustaendig?.Fosa == JaNein.Ja) liste.Add($"FOSA{(zustaendig.Ambulanz.Trim().Length > 0 ? $" ({zustaendig.Ambulanz.Trim()})" : "")}");
        if (kostentraeger is not null && kostentraeger.Sachbearbeiter.Trim().Length > 0) liste.Add($"{kostentraeger.Sachbearbeiter} ({kostentraeger.Name})");
        liste.AddRange(entbundeneAngehoerige.Select(a => $"{a} (Angehörige)"));
        return [.. liste.Distinct()];
    }

    public static readonly string[] Vorbereitung = ["HEB-Bogen aktuell?", "Monatsreflexion fertig?", "Ziele überprüft?", "Unterlagen bereitlegen (Bescheid, Kostenzusage)"];

    public static string VorbereitungText(string punkt, Bewohner b, DateTime termin) =>
        $"Hilfeplangespräch {b.Name} am {termin:dd.MM.yyyy}: {punkt}";

    // Einladung ohne Gesundheitsangaben: nur Anlass, Name, Zeit und Ort.
    public static string Betreff(Bewohner b, KalenderFassung f) => $"Einladung: Hilfeplangespräch {b.Name} am {f.Beginn:dd.MM.yyyy} um {f.Beginn:HH:mm} Uhr";

    public static string Einladung(Bewohner b, KalenderFassung f) =>
        $"Guten Tag,\n\nhiermit laden wir zum Hilfeplangespräch für {b.Name} ein.\n\nTermin: {f.Beginn:dd.MM.yyyy}, {f.Beginn:HH:mm}–{f.Ende:HH:mm} Uhr\nOrt: {f.Ort}\n\n" +
        "Den Termin finden Sie auch in der angehängten Kalenderdatei.\n\nMit freundlichen Grüßen";

    public static string Mailto(IEnumerable<string> empfaenger, string betreff, string text) =>
        "mailto:" + string.Join(",", empfaenger.Where(BetreuerRegeln.EmailGueltig).Select(Uri.EscapeDataString)) +
        $"?subject={Uri.EscapeDataString(betreff)}&body={Uri.EscapeDataString(text)}";

    // Überschneidungen am Tag des Gesprächs (Warnung, kein Verbot): andere Termine des Bewohners zur selben Zeit.
    public static List<string> Ueberschneidungen(IEnumerable<Termin> woche, DateTime beginn, DateTime ende) =>
        [.. woche.Where(t => t.Tag == DateOnly.FromDateTime(beginn) && t.Von < TimeOnly.FromDateTime(ende) && TimeOnly.FromDateTime(beginn) < t.Bis)
            .Select(t => $"{t.Von:HH\\:mm}–{t.Bis:HH\\:mm} {t.Titel}")];
}
