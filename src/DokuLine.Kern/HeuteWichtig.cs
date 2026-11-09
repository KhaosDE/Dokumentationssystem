using System.Globalization;

namespace DokuLine.Kern;

// Übersicht: links „Heute wichtig“ (vier Kästen), rechts die Bewohnerliste mit
// der Spalte „Wo“. Hier nur die Regeln; die Oberfläche stellt sie dar.

// Laufender Dienst: „6 von 9 erledigt“ und die offenen Aufgaben zum Abhaken.
public sealed record DienstStand(SchichtTermin Schicht, int Erledigt, int Gesamt, IReadOnlyList<Aufgabe> Offen)
{
    public string Text => $"{Erledigt} von {Gesamt} erledigt";
}

// Ein Eintrag im Kasten „Termine heute“: gleiche Termine mit gleichem Status zusammengefasst.
public sealed record TerminEintrag(TimeOnly Von, bool Ganztags, string Titel, IReadOnlyList<string> Namen, TafelStatus Status)
{
    public const int NamenHoechstens = 3;

    public string Zeit => Ganztags ? "ganztags" : Von.ToString("HH\\:mm", CultureInfo.InvariantCulture);
    public string StatusText => TafelRegeln.Wort(Status).ToLower(CultureInfo.GetCultureInfo("de-DE"));

    // „Holzwerkstatt – Anton Ahorn, Dieter Distel, Erna Eiche +2“
    public string Text => Namen.Count == 0 ? Titel
        : $"{Titel} – {string.Join(", ", Namen.Take(NamenHoechstens))}" + (Namen.Count > NamenHoechstens ? $" +{Namen.Count - NamenHoechstens}" : "");
}

public static class HeuteWichtigRegeln
{
    public const string NichtsOffen = "Nichts offen";
    public const string ImHaus = "im Haus";
    private const string Abwesend = "abwesend: ";

    // Aufgaben des laufenden Dienstes: offen ist, was jetzt dran ist (auch aus früheren Schichten
    // liegen geblieben) oder „wer Zeit hat“; erledigt zählt, was in dieser Schicht abgehakt wurde.
    // Aufgaben für eine spätere Schicht zählen noch nicht.
    public static DienstStand Dienst(IEnumerable<Aufgabe> aufgaben, DateTime jetzt)
    {
        var schicht = SchichtTermin.Zu(jetzt);
        var liste = aufgaben.ToList();
        var offen = liste.Where(a => a.Offen && (AufgabenRegeln.Zielschicht(a) is not SchichtTermin s || s.Beginn <= jetzt)).ToList();
        int erledigt = liste.Count(a => a.Erledigt is DateTime e && e >= schicht.Beginn && e <= jetzt);
        return new DienstStand(schicht, erledigt, erledigt + offen.Count, offen);
    }

    // Termine heute aus dem Tagesplan, nach Uhrzeit (ganztags zuerst), mit dem Status der Tafel.
    // Gleicher Termin mit gleichem Status steht einmal da, mit den Namen.
    public static List<TerminEintrag> Termine(IEnumerable<TagesplanZeile> zeilen, DateTime jetzt) =>
        TafelRegeln.Tafel(zeilen, jetzt)
            .GroupBy(z => (z.Termin.Ganztags, z.Termin.Von, z.Termin.Titel, z.Status))
            .OrderBy(g => g.Key.Ganztags ? 0 : 1).ThenBy(g => g.Key.Von).ThenBy(g => g.Key.Titel, StringComparer.CurrentCulture)
            .Select(g => new TerminEintrag(g.Key.Von, g.Key.Ganztags, g.Key.Titel,
                g.Select(z => z.Bewohner.Name).Distinct().ToList(), g.Key.Status))
            .ToList();

    // Kasten „Übergaben offen“: „Anton Ahorn, Zi. 101: Text“ und darunter „seit 07.11. · AB“.
    public static string UebergabeText(Uebergabe u, Bewohner? b) =>
        (b is null ? "Wohnbereich allgemein" : $"{b.Name}, Zi. {b.Zimmer}") + ": " + ErsteZeile(u.Anzeige);

    public static string SeitText(Uebergabe u) => $"seit {u.Erstellt:dd.MM.} · {u.Verfasser}";

    // Kasten „Nachrichten ungelesen“: je Verlauf die neueste ungelesene Nachricht an mich.
    public static List<Nachricht> Ungelesen(IEnumerable<Verlauf> verlaeufe, string ich) =>
        verlaeufe.Where(v => v.Ungelesen > 0)
            .Select(v => v.Nachrichten.LastOrDefault(n => n.Von != ich && !n.GelesenVon.Contains(ich)))
            .OfType<Nachricht>()
            .OrderByDescending(n => n.Gesendet)
            .ToList();

    // „AB → Wohnbereich R1: erste Zeile“
    public static string NachrichtText(Nachricht n) =>
        $"{n.Von} → {string.Join(", ", n.An.Select(e => e.Ziel))}: {ErsteZeile(n.Betreff.Length > 0 ? n.Betreff : n.Text)}";

    public static string ErsteZeile(string text)
    {
        var zeile = text.Replace("\r", "").Split('\n').Select(z => z.Trim()).FirstOrDefault(z => z.Length > 0) ?? "";
        return zeile;
    }

    // Spalte „Wo“: abwesend (Heimfahrt, Klinik, Fahrt) vor Ausgang vor Therapie vor einem Kalendertermin,
    // der gerade läuft; sonst „im Haus“. Quelle ist die Marke, aus der die Angabe stammt (sie steht dann
    // nicht noch einmal bei den Hinweisen).
    public static (string Text, MarkenArt Art, Marke? Quelle) Wo(IReadOnlyList<Marke> marken, IEnumerable<Termin> termine, DateTime jetzt)
    {
        if (marken.FirstOrDefault(m => m.Art == MarkenArt.Abgaengig) is Marke abgaengig) return (abgaengig.Text, MarkenArt.Abgaengig, abgaengig);
        if (marken.FirstOrDefault(m => m.Art == MarkenArt.Abwesend) is Marke weg)
            return (weg.Text.StartsWith(Abwesend, StringComparison.Ordinal) ? weg.Text[Abwesend.Length..] : weg.Text, MarkenArt.Abwesend, weg);
        if (marken.FirstOrDefault(m => m.Text.StartsWith("unterwegs seit", StringComparison.Ordinal) ||
                                       m.Text.StartsWith("überfällig seit", StringComparison.Ordinal) ||
                                       m.Text.StartsWith("Rückkehrzeit überschritten", StringComparison.Ordinal)) is Marke aus)
            return (aus.Text, aus.Art, aus);
        if (marken.FirstOrDefault(m => m.Text.StartsWith("in Therapie", StringComparison.Ordinal)) is Marke therapie)
            return (therapie.Text, MarkenArt.Status, therapie);
        var heute = DateOnly.FromDateTime(jetzt);
        var uhr = TimeOnly.FromDateTime(jetzt);
        if (termine.Where(t => t.Quelle == TerminQuelle.Kalender && t.Tag == heute && !t.Ganztags && t.Von <= uhr && uhr < t.Bis)
                .OrderBy(t => t.Von).FirstOrDefault() is Termin laeuft)
            return ($"Termin: {laeuft.Titel} bis {laeuft.Bis:HH\\:mm}", MarkenArt.Termin, null);
        return (ImHaus, MarkenArt.Angabe, null);
    }

    // „Geburtstag heute (45)“; wer am 29. Februar geboren ist, feiert in anderen Jahren am 28.
    public static Marke? Geburtstag(Bewohner b, DateOnly heute)
    {
        var g = b.Geburtsdatum;
        bool heuteDran = (g.Month, g.Day) == (heute.Month, heute.Day) ||
                         (g.Month == 2 && g.Day == 29 && !DateTime.IsLeapYear(heute.Year) && heute.Month == 2 && heute.Day == 28);
        return heuteDran ? new Marke(MarkenArt.Angabe, $"Geburtstag heute ({BewohnerRegeln.Alter(g, heute)})") : null;
    }

    // Statusleiste: „DEMO — ausgedachte Daten · Sa 08.11.2026 · Spätdienst · Stand 14:32“.
    public static string StatusText(DateTime jetzt, DateTime stand) =>
        $"DEMO — ausgedachte Daten · {jetzt.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"))} · " +
        $"{BerichtRegeln.SchichtText(SchichtTermin.Zu(jetzt).Schicht)} · Stand {stand:HH\\:mm}";
}
