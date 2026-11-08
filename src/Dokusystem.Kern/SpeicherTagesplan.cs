namespace Dokusystem.Kern;

// Tagesplan und „Wo müsste er sein?“ (Bauplan Schritt 23). Die Teilnahme selbst speichert
// TeilnahmeSetzen (Wochenplan) – ein Datensatz für beide Ansichten.
public sealed partial class Speicher
{
    // Die zuletzt eingetragene Teilnahme je Zuteilung an einem Tag (mit Zeit und Kürzel).
    public Dictionary<long, TeilnahmeEintrag> TeilnahmeEintraege(DateOnly tag)
    {
        var ergebnis = new Dictionary<long, TeilnahmeEintrag>();
        using var befehl = Befehl("SELECT zuteilung_id, status, erstellt, kuerzel FROM teilnahmen WHERE tag = $t ORDER BY erstellt, id", null, [("$t", Iso(tag))]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            ergebnis[leser.GetInt64(0)] = new TeilnahmeEintrag(WochenplanRegeln.TeilnahmeAus(leser.GetString(1)), AusZeit(leser.GetString(2)), leser.GetString(3));
        return ergebnis;
    }

    // Alle Termine eines Tages der gegebenen Bewohner (Wochenplan: Regelwoche, Therapien, Kalender).
    public List<TagesplanZeile> Tagesplan(DateOnly tag, IEnumerable<Bewohner> bewohner)
    {
        var montag = WochenplanRegeln.Montag(tag);
        var eintraege = TeilnahmeEintraege(tag);
        return bewohner.SelectMany(b => Woche(b.Id!.Value, montag).Where(t => t.Tag == tag)
            .Select(t => new TagesplanZeile(b, t, t.ZuteilungId is long z ? eintraege.GetValueOrDefault(z) : null))).ToList();
    }

    // Wo müsste er sein? Wer heute zu einer Therapie „gegangen“ ist, steht bis zu deren Ende als
    // „in Therapie: … bis …“ da – außer, danach wurde eine Statusmeldung gesetzt.
    public Dictionary<long, Marke> InTherapie(DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        var uhr = TimeOnly.FromDateTime(jetzt);
        var eintraege = TeilnahmeEintraege(heute);
        if (eintraege.Count == 0) return [];
        var status = GeltendeStatusmeldungen(jetzt);
        var gefunden = new Dictionary<long, (TimeOnly Bis, string Titel)>();
        foreach (var a in Angebote())
            foreach (var z in a.Zuteilungen.Where(z => z.GiltAm(heute)))
            {
                if (eintraege.GetValueOrDefault(z.Id) is not { Status: Teilnahme.Teilgenommen } e) continue;
                if (status.TryGetValue(z.BewohnerId, out var s) && s.Erstellt > e.Erstellt) continue;
                var f = a.Am(heute);
                foreach (var t in f.Termine.Where(t => t.Tag == heute.DayOfWeek && uhr < t.Bis))
                    if (!gefunden.TryGetValue(z.BewohnerId, out var bisher) || t.Bis < bisher.Bis)
                        gefunden[z.BewohnerId] = (t.Bis, f.Name);
            }
        return gefunden.ToDictionary(x => x.Key, x => new Marke(MarkenArt.Status, TagesplanRegeln.InTherapieText(x.Value.Titel, x.Value.Bis)));
    }
}
