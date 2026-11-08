namespace DokuLine.Kern;

// Therapie, Bauplan Schritt 26m: „Laufende Therapien“ als Anwesenheitstafel. Oben eine Tagesleiste
// (7–21 Uhr), darunter je laufender Therapie eine Spalte mit den Teilnehmern, daneben „Gleich“ und
// „Noch offen“, unten der Rest des Tages. Dieselben Teilnahme-Einträge wie Tagesplan und Wochenplan.

// Ein Teilnehmer auf der Tafel: Status wie im Tagesplan (gegangen, noch offen, abgesagt, ausgefallen …).
public sealed record TafelTeilnehmer(Bewohner Bewohner, long ZuteilungId, TafelStatus Status, TeilnahmeEintrag? Eintrag)
{
    public string StatusText => TafelRegeln.Wort(Status).ToLowerInvariant();
}

// Ein Termin eines Angebots heute. Teilnehmer: die gezeigten (z. B. nur R2); Belegt: alle des Angebots.
public sealed record TherapieTermin(Angebot Angebot, AngebotFassung Fassung, DateOnly Tag, TimeOnly Von, TimeOnly Bis,
    IReadOnlyList<TafelTeilnehmer> Teilnehmer, int Belegt)
{
    public string Name => Fassung.Name;
    public string Belegung => $"{Belegt} von {Fassung.Hoechstens}";
    public string Zeit => $"{Von:HH\\:mm}–{Bis:HH\\:mm}";

    public bool Laeuft(DateTime jetzt) => Uhr(jetzt) is var u && Von <= u && u < Bis;
    public bool Vorbei(DateTime jetzt) => Uhr(jetzt) >= Bis;
    public bool Gleich(DateTime jetzt) =>
        Uhr(jetzt) is var u && u < Von && (Von.ToTimeSpan() - u.ToTimeSpan()).TotalMinutes <= TafelRegeln.GleichMinuten;

    private static TimeOnly Uhr(DateTime jetzt) => TimeOnly.FromDateTime(jetzt);
}

// Die Tafel zu einem Zeitpunkt.
public sealed record LaufendeTafel(
    IReadOnlyList<TherapieTermin> Heute,
    IReadOnlyList<TherapieTermin> Laufend,
    IReadOnlyList<TherapieTermin> Gleich,
    IReadOnlyList<(TherapieTermin Termin, TafelTeilnehmer Teilnehmer)> NochOffen,
    IReadOnlyList<TherapieTermin> RestDesTages,
    string? Hinweis);

public static class LaufendeRegeln
{
    public const int TagBeginn = 7, TagEnde = 21;
    public const string AlleBereiche = "Alle Bereiche";

    // Alle Termine von heute. Mit Bereich: nur Teilnehmer dieses Wohnbereichs, und nur Angebote, bei denen
    // jemand aus dem Bereich dabei ist. Teilnehmer nach Bereich, Zimmer und Name.
    public static List<TherapieTermin> Heute(IEnumerable<Angebot> angebote, IReadOnlyDictionary<long, Bewohner> bewohner,
        IReadOnlyDictionary<long, TeilnahmeEintrag> eintraege, DateTime jetzt, string? bereich = null)
    {
        var tag = DateOnly.FromDateTime(jetzt);
        var liste = new List<TherapieTermin>();
        foreach (var a in angebote)
        {
            var f = a.Am(tag);
            if (f.GiltAb > tag) continue;   // noch nicht angelaufen
            var alle = a.TeilnehmerAm(tag).Where(z => bewohner.ContainsKey(z.BewohnerId)).ToList();
            foreach (var t in f.Termine.Where(t => t.Tag == tag.DayOfWeek))
            {
                var teilnehmer = alle.Where(z => bereich is null || bewohner[z.BewohnerId].Wohnbereich == bereich)
                    .Select(z =>
                    {
                        var b = bewohner[z.BewohnerId];
                        var eintrag = eintraege.GetValueOrDefault(z.Id);
                        var termin = new Termin { Tag = tag, Von = t.Von, Bis = t.Bis, Titel = f.Name, Ort = f.Ort, Quelle = TerminQuelle.Therapie, ZuteilungId = z.Id };
                        var (status, _) = TafelRegeln.Einordnen(new TagesplanZeile(b, termin, eintrag), jetzt);
                        return new TafelTeilnehmer(b, z.Id, status, eintrag);
                    })
                    .OrderBy(x => x.Bewohner.Wohnbereich, StringComparer.Ordinal)
                    .ThenBy(x => x.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal)
                    .ThenBy(x => x.Bewohner.Nachname, StringComparer.CurrentCulture)
                    .ToList();
                if (bereich is not null && teilnehmer.Count == 0) continue;
                liste.Add(new TherapieTermin(a, f, tag, t.Von, t.Bis, teilnehmer, alle.Count));
            }
        }
        return [.. liste.OrderBy(t => t.Von).ThenBy(t => t.Name, StringComparer.CurrentCulture)];
    }

    // Laufend, gleich (Beginn in den nächsten 30 Minuten), noch offen (hat begonnen, Teilnahme nicht
    // bestätigt – auch bei schon beendeten Terminen), Rest des Tages (später).
    public static LaufendeTafel Tafel(IReadOnlyList<TherapieTermin> heute, DateTime jetzt)
    {
        var laufend = heute.Where(t => t.Laeuft(jetzt)).ToList();
        var gleich = heute.Where(t => t.Gleich(jetzt)).ToList();
        var nochOffen = heute.Where(t => t.Laeuft(jetzt) || t.Vorbei(jetzt))
            .SelectMany(t => t.Teilnehmer.Where(p => p.Status == TafelStatus.NochOffen).Select(p => (t, p))).ToList();
        var rest = heute.Where(t => !t.Laeuft(jetzt) && !t.Vorbei(jetzt) && !t.Gleich(jetzt)).ToList();
        string? hinweis = null;
        if (laufend.Count == 0)
            hinweis = heute.FirstOrDefault(t => !t.Laeuft(jetzt) && !t.Vorbei(jetzt)) is TherapieTermin n
                ? $"Gerade läuft keine Therapie. Nächste: {n.Name} um {n.Von:HH\\:mm}."
                : "Gerade läuft keine Therapie. Heute kommt keine mehr.";
        return new LaufendeTafel(heute, laufend, gleich, nochOffen, rest, hinweis);
    }

    // Tagesleiste: Bahnen so, dass sich Balken in einer Bahn nicht überschneiden (erste freie Bahn).
    public static List<List<TherapieTermin>> Bahnen(IEnumerable<TherapieTermin> termine)
    {
        var bahnen = new List<List<TherapieTermin>>();
        foreach (var t in termine.OrderBy(t => t.Von).ThenBy(t => t.Bis))
        {
            var frei = bahnen.FirstOrDefault(b => b[^1].Bis <= t.Von);
            if (frei is null) bahnen.Add([t]);
            else frei.Add(t);
        }
        return bahnen;
    }

    // Lage in der Tagesleiste von 0 (7 Uhr) bis 1 (21 Uhr); früher und später am Rand.
    public static double Anteil(TimeOnly zeit) =>
        Math.Clamp((zeit.ToTimeSpan().TotalHours - TagBeginn) / (TagEnde - TagBeginn), 0, 1);

    // Vorgabe der Bereichswahl: der eigene Wohnbereich, wenn es dort Bewohner gibt; sonst (z. B. Therapie)
    // alle Bereiche.
    public static string Vorgabe(IEnumerable<string> eigeneBereiche, IEnumerable<Bewohner> bewohner)
    {
        var mitBewohnern = bewohner.Select(b => b.Wohnbereich).ToHashSet();
        return eigeneBereiche.FirstOrDefault(mitBewohnern.Contains) ?? AlleBereiche;
    }
}
