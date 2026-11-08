using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Wohnbereiche (offen/beschützend), Zimmer und Betten im Speicher (Bauplan Schritt 26e).
public sealed partial class Speicher
{
    private const string DemoZimmer = "demo_zimmer";   // Schritt 26e: R1–R3 mit Zimmern und Betten

    public event Action? ZimmerGeaendert;

    // Bereiche mit Art (offen/beschützend) sind Wohnbereiche; Therapie, Leitung usw. haben keine Art.
    public List<Wohnbereich> Wohnbereiche()
    {
        var liste = new List<Wohnbereich>();
        using var befehl = Befehl("SELECT name, art FROM bereiche WHERE art IS NOT NULL AND aktiv = 1 ORDER BY nr, name", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(new Wohnbereich(leser.GetString(0), leser.GetString(1) == "beschuetzt"));
        return liste;
    }

    public void BereichArtSetzen(string bereich, bool? beschuetzt)
    {
        Ausfuehren("UPDATE bereiche SET art = $a WHERE name = $n", null,
            ("$a", beschuetzt is null ? null : beschuetzt.Value ? "beschuetzt" : "offen"), ("$n", bereich));
        ZimmerGeaendert?.Invoke();
    }

    public List<Zimmer> Zimmerliste(string? bereich = null)
    {
        var liste = new List<Zimmer>();
        using var befehl = Befehl("SELECT nummer, bereich, betten, aktiv FROM zimmer" + (bereich is null ? "" : " WHERE bereich = $b"), null,
            bereich is null ? [] : [("$b", bereich)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(new Zimmer(leser.GetString(0), leser.GetString(1), leser.GetInt32(2), leser.GetInt64(3) == 1));
        return [.. liste.OrderBy(z => z.Bereich, StringComparer.Ordinal).ThenBy(z => z.Nummer, StringComparer.Ordinal)];
    }

    // Anlegen (neu) oder ändern. Die Nummer eines vorhandenen Zimmers bleibt; ein belegtes Bett
    // verschwindet nicht (Doppelzimmer → Einzelzimmer nur, wenn „Bett Tür“ frei ist; stilllegen nur leer).
    public void ZimmerSpeichern(Zimmer z, bool neu)
    {
        z = z with { Nummer = z.Nummer.Trim() };
        var fehler = ZimmerRegeln.Pruefen(z, Wohnbereiche().Select(w => w.Name).ToList());
        var alle = Zimmerliste();
        var bisher = alle.FirstOrDefault(x => x.Nummer.Equals(z.Nummer, StringComparison.OrdinalIgnoreCase));
        if (neu && bisher is not null) fehler.Add($"Zimmer {z.Nummer} gibt es schon.");
        if (!neu && bisher is null) fehler.Add($"Zimmer {z.Nummer} gibt es nicht.");
        if (!neu && bisher is not null)
        {
            var belegt = AlleBewohner().Where(b => b.Zimmer == bisher.Nummer && b.Bett.Length > 0).ToList();
            if (belegt.Count > 0 && (!z.Aktiv || z.Bereich != bisher.Bereich))
                fehler.Add($"Zimmer {z.Nummer} ist belegt ({string.Join(", ", belegt.Select(b => b.Name))}) – erst die Bewohner umziehen.");
            else if (belegt.Any(b => !ZimmerRegeln.Bettschluessel(z.Betten).Contains(b.Bett)))
                fehler.Add($"Zimmer {z.Nummer}: ein belegtes Bett fiele weg – erst den Bewohner umziehen.");
        }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        if (neu)
            Ausfuehren("INSERT INTO zimmer (nummer, bereich, betten, aktiv) VALUES ($n, $b, $z, $a)", null,
                ("$n", z.Nummer), ("$b", z.Bereich), ("$z", z.Betten), ("$a", z.Aktiv ? 1 : 0));
        else
            Ausfuehren("UPDATE zimmer SET bereich = $b, betten = $z, aktiv = $a WHERE nummer = $n", null,
                ("$n", bisher!.Nummer), ("$b", z.Bereich), ("$z", z.Betten), ("$a", z.Aktiv ? 1 : 0));
        ZimmerGeaendert?.Invoke();
    }

    // Freie Betten eines Bereichs; ausser = der Bewohner, der gerade umzieht (sein Bett gilt als frei).
    public List<Bettplatz> FreieBetten(string bereich, long? ausser = null) =>
        ZimmerRegeln.FreieBetten(Zimmerliste(bereich), AlleBewohner(), bereich, ausser);

    // Vor dem Speichern eines Bewohners: Das gewählte Bett muss es geben und frei sein; Zimmer und
    // Wohnbereich folgen aus dem Bett.
    private Bewohner BettPruefen(Bewohner b)
    {
        if (b.Bett.Length == 0) return b;
        var zimmer = Zimmerliste().FirstOrDefault(z => z.Nummer == b.Zimmer);
        if (zimmer is null || !ZimmerRegeln.Bettschluessel(zimmer.Betten).Contains(b.Bett))
            throw new Eingabefehler([$"Zimmer {b.Zimmer}: dieses Bett gibt es nicht."]);
        if (AlleBewohner().FirstOrDefault(x => x.Id != b.Id && x.Zimmer == b.Zimmer && x.Bett == b.Bett) is Bewohner andere)
            throw new Eingabefehler([$"{zimmer.Nummer} – {ZimmerRegeln.BettText(b.Bett)} ist schon belegt ({andere.Name})."]);
        return b with { Wohnbereich = zimmer.Bereich };
    }

    // Demo: Art der Bereiche und Zimmer aus daten/demo/zimmer.json.
    private void DemoZimmerEinfuegen(SqliteTransaction transaktion)
    {
        var demo = Demodaten.Zimmer();
        Ausfuehren("DELETE FROM zimmer; UPDATE bereiche SET art = NULL;", transaktion);
        foreach (var (name, beschuetzt) in demo.Bereiche)
            Ausfuehren("UPDATE bereiche SET art = $a WHERE name = $n", transaktion, ("$a", beschuetzt ? "beschuetzt" : "offen"), ("$n", name));
        foreach (var z in demo.Zimmer)
            Ausfuehren("INSERT INTO zimmer (nummer, bereich, betten, aktiv) VALUES ($n, $b, $z, 1)", transaktion,
                ("$n", z.Nummer), ("$b", z.Bereich), ("$z", z.Betten));
        SetzeMeta(DemoZimmer, "1", transaktion);
    }
}
