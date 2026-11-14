namespace DokuLine.Kern;

// Medikamentenbestellung (Bauplan 51): Rezeptanforderung an die Ärzte bzw. Forensik/FOSA und Bestellung bei der
// heimversorgenden Apotheke, verfolgt bis zur Lieferung. Bestandsführung ist zuschaltbar (je Bereich, Vorgabe aus); „reicht
// bis“ ist reine Zählung aus dem Plan, keine medizinische Berechnung. Übermittelt wird nur das Nötige: keine Diagnosen,
// keine Berichte.
public enum AnforderungStand { Angefordert, Bestaetigt, Geliefert, Erledigt }

public sealed record Bestandszaehlung(long VerordnungId, decimal Stueck, int Packung, DateTime Gezaehlt, string Kuerzel);

public sealed record BestellPosten(Bewohner Bewohner, Verordnung Verordnung, decimal? Vorrat, DateOnly? ReichtBis, string Empfaenger, string Email);

public sealed record AnforderungPosten(long VerordnungId, long BewohnerId, string Text, string Menge);

public sealed record Anforderung(long Id, string Empfaenger, string Email, DateTime Erstellt, string Kuerzel, IReadOnlyList<AnforderungPosten> Posten,
    AnforderungStand Stand, DateTime StandZeit);

public static class BestellRegeln
{
    public const int TageVorgabe = 7, ErinnernTage = 5;
    public const string Apotheke = "Apotheke";
    public const string DatenschutzHinweis = "Normale E-Mail ist nicht verschlüsselt. Für Gesundheitsdaten nur einen Weg nutzen, den Einrichtung und Datenschutz " +
        "freigegeben haben (z. B. verschlüsselte E-Mail, Fax, KIM).";

    public static string StandText(AnforderungStand s) => s switch
    {
        AnforderungStand.Angefordert => "angefordert",
        AnforderungStand.Bestaetigt => "Rezept/Bestätigung da",
        AnforderungStand.Geliefert => "geliefert",
        _ => "erledigt",
    };

    // Verbrauch pro Tag laut heutigem Plan (Summe der vier Einnahmezeiten).
    public static decimal ProTag(Dosis d) => d.Werte.Sum(w => MedikationRegeln.Menge(w) ?? 0m);

    public static DateOnly? ReichtBis(decimal vorrat, decimal proTag, DateOnly heute) =>
        proTag <= 0 ? null : heute.AddDays((int)Math.Floor(Math.Max(0, vorrat) / proTag));

    // Vorrat: letzte Zählung + Lieferungen danach − abgehakte Ausgaben danach (je Einnahmezeit die Menge laut Plan). Je Gabe gilt
    // der zuletzt gesetzte Status wie in der Ausgabeliste; „mitgegeben“ verlässt den Vorrat wie „ausgegeben“.
    public static decimal? Vorrat(Bestandszaehlung? zaehlung, IEnumerable<(DateTime Zeit, decimal Menge)> lieferungen, IEnumerable<AusgabeEintrag> ausgaben,
        Verordnung v)
    {
        if (zaehlung is null) return null;
        decimal geliefert = lieferungen.Where(l => l.Zeit > zaehlung.Gezaehlt).Sum(l => l.Menge);
        decimal gegeben = ausgaben.Where(a => a.VerordnungId == v.Id).GroupBy(a => (a.Tag, a.Zeit)).Select(g => g.MaxBy(a => a.Id)!)
            .Where(a => (a.Status is AusgabeStatus.Ausgegeben or AusgabeStatus.Mitgegeben) && a.Erfasst > zaehlung.Gezaehlt)
            .Sum(a => MedikationRegeln.Menge(AusgabeRegeln.Faellig(v, a.Tag, a.Zeit) ?? "0") ?? 0m);
        return zaehlung.Stueck + geliefert - gegeben;
    }

    // Empfänger je Bestellweg: Hausarzt bzw. Arzt der Verordnung, Forensik, FOSA – sonst die heimversorgende Apotheke.
    public static string Empfaenger(Bestellweg weg, string? person, Verordnung v, string apotheke) => weg switch
    {
        Bestellweg.Forensik => person ?? "Forensik",
        Bestellweg.Fosa => person ?? "FOSA",
        Bestellweg.Hausarzt => v.Anordnung.Arzt.Trim().Length > 0 ? v.Anordnung.Arzt : person ?? "Hausarzt",
        _ => apotheke.Trim().Length > 0 ? apotheke : Apotheke,
    };

    // Eine Zeile der Anforderung: Bewohner, Geburtsdatum, Medikament, Stärke, Menge – keine Diagnosen, keine Berichte.
    public static string PostenText(Bewohner b, Verordnung v) => $"{b.Name}, geb. {Datum.Text(b.Geburtsdatum)}: {v.Medikament} {v.Staerke}".Trim();

    public static string Betreff(string empfaenger, DateTime jetzt) => $"Rezeptanforderung / Bestellung vom {jetzt:dd.MM.yyyy}";

    public static string Text(Anforderung a) =>
        $"Guten Tag,\n\nwir bitten um folgende Rezepte bzw. Lieferung:\n\n{string.Join("\n", a.Posten.Select(p => $"– {p.Text} · Menge: {p.Menge}"))}\n\nMit freundlichen Grüßen";

    public static string Mailto(Anforderung a) =>
        "mailto:" + (BetreuerRegeln.EmailGueltig(a.Email) ? Uri.EscapeDataString(a.Email) : "") +
        $"?subject={Uri.EscapeDataString(Betreff(a.Empfaenger, a.Erstellt))}&body={Uri.EscapeDataString(Text(a))}";

    // Erinnerung, wenn nach n Tagen nichts geliefert wurde.
    public static bool ErinnernNoetig(Anforderung a, DateTime jetzt, int tage = ErinnernTage) =>
        a.Stand is AnforderungStand.Angefordert or AnforderungStand.Bestaetigt && a.Erstellt.AddDays(tage) <= jetzt;
}
