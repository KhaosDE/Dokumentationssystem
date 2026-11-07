using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Therapie im Speicher. Fassungen, Zuteilungen und „beendet ab“ werden nie überschrieben.
public sealed partial class Speicher
{
    public event Action? TherapieGeaendert;

    private sealed record TerminDaten(int Tag, string Von, string Bis);

    private static string TermineJson(IEnumerable<Wochentermin> t) =>
        JsonSerializer.Serialize(t.Select(x => new TerminDaten((int)x.Tag, x.Von.ToString("HH:mm", CultureInfo.InvariantCulture), x.Bis.ToString("HH:mm", CultureInfo.InvariantCulture))));

    private static List<Wochentermin> TermineAus(string json) =>
        (JsonSerializer.Deserialize<List<TerminDaten>>(json) ?? [])
        .Select(x => new Wochentermin((DayOfWeek)x.Tag, TimeOnly.ParseExact(x.Von, "HH:mm", CultureInfo.InvariantCulture), TimeOnly.ParseExact(x.Bis, "HH:mm", CultureInfo.InvariantCulture)))
        .ToList();

    public List<Angebot> Angebote() =>
        AngeboteLesen(null, "").OrderBy(a => a.Fassungen[^1].Name, StringComparer.CurrentCulture).ToList();

    public Angebot? Angebot(long id) => AngeboteLesen(null, "WHERE id = $id", ("$id", id)).FirstOrDefault();

    public Angebot AngebotAnlegen(AngebotFassung f)
    {
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT INTO angebote DEFAULT VALUES", transaktion);
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        FassungEinfuegen(id, f, transaktion);
        transaktion.Commit();
        TherapieGeaendert?.Invoke();
        return Angebot(id)!;
    }

    // Änderung „gilt ab …“: eine neue Fassung; die bisherigen bleiben.
    public Angebot AngebotAendern(long id, AngebotFassung f)
    {
        var a = Angebot(id) ?? throw new Eingabefehler(["Dieses Angebot gibt es nicht mehr."]);
        if (f.GiltAb < a.Fassungen[0].GiltAb) throw new Eingabefehler([$"„Gilt ab“ darf nicht vor dem ersten Tag des Angebots liegen ({Datum.Text(a.Fassungen[0].GiltAb)})."]);
        // Höchstzahl nicht unter die Belegung ab „gilt ab“ (an jedem Tag, an dem jemand dazukommt).
        int hoechstBelegt = new[] { f.GiltAb }.Concat(a.Zuteilungen.Select(z => z.Ab).Where(d => d > f.GiltAb))
            .Max(tag => a.TeilnehmerAm(tag).Count);
        if (f.Hoechstens < hoechstBelegt) throw new Eingabefehler([$"Ab {Datum.Text(f.GiltAb)} sind schon {hoechstBelegt} Bewohner zugeteilt – höchste Teilnehmerzahl bitte nicht darunter."]);
        using var transaktion = db.BeginTransaction();
        FassungEinfuegen(id, f, transaktion);
        transaktion.Commit();
        TherapieGeaendert?.Invoke();
        return Angebot(id)!;
    }

    private void FassungEinfuegen(long angebotId, AngebotFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO angebot_fassungen (angebot_id, gilt_ab, name, art, therapeut, ort, termine, hoechstens, hinweis, erstellt, kuerzel)
            VALUES ($a, $g, $n, $art, $t, $o, $te, $h, $hi, $e, $k)
            """, transaktion,
            ("$a", angebotId), ("$g", Iso(f.GiltAb)), ("$n", f.Name), ("$art", f.Art == AngebotArt.Einzel ? "einzel" : "gruppe"),
            ("$t", f.Therapeut), ("$o", f.Ort), ("$te", TermineJson(f.Termine)), ("$h", f.Hoechstens), ("$hi", f.Hinweis),
            ("$e", Zeit(f.Erstellt)), ("$k", f.Kuerzel));

    // Überschneidungen mit anderen Therapien des Bewohners (Warnung, kein Verbot).
    public List<Ueberschneidung> Ueberschneidungen(long angebotId, long bewohnerId, DateOnly ab, DateOnly? bis)
    {
        var alle = Angebote();
        var neu = alle.FirstOrDefault(a => a.Id == angebotId) ?? throw new Eingabefehler(["Dieses Angebot gibt es nicht mehr."]);
        return TherapieRegeln.Ueberschneidungen(neu, bewohnerId, ab, bis, alle);
    }

    public Zuteilung Zuteilen(long angebotId, long bewohnerId, DateOnly ab, DateOnly? bis, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        var a = Angebot(angebotId) ?? throw new Eingabefehler(["Dieses Angebot gibt es nicht mehr."]);
        if (Bewohner(bewohnerId) is null) fehler.Add("Bitte einen Bewohner wählen.");
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (bis is DateOnly b && b < ab) fehler.Add("„Bis“ liegt vor „ab“.");
        if (ab < a.Fassungen[0].GiltAb) fehler.Add($"Das Angebot beginnt erst am {Datum.Text(a.Fassungen[0].GiltAb)}.");
        if (a.Zuteilungen.Any(z => z.BewohnerId == bewohnerId && z.Ueberlappt(ab, bis))) fehler.Add("Dieser Bewohner ist in diesem Zeitraum schon zugeteilt.");
        // Voll? An jedem Tag, an dem sich die Teilnehmerzahl im Zeitraum ändern kann.
        var tage = new[] { ab }.Concat(a.Zuteilungen.Select(z => z.Ab).Where(d => d > ab && (bis is not DateOnly e || d <= e))).Distinct();
        foreach (var tag in tage)
            if (a.TeilnehmerAm(tag).Count >= a.Am(tag).Hoechstens)
            {
                fehler.Add($"Das Angebot ist am {Datum.Text(tag)} voll ({a.Am(tag).Hoechstens} Plätze).");
                break;
            }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        using var transaktion = db.BeginTransaction();
        long id = ZuteilungEinfuegen(angebotId, bewohnerId, ab, bis, jetzt, kuerzel.Trim(), transaktion);
        transaktion.Commit();
        TherapieGeaendert?.Invoke();
        return Angebot(angebotId)!.Zuteilungen.Single(z => z.Id == id);
    }

    private long ZuteilungEinfuegen(long angebotId, long bewohnerId, DateOnly ab, DateOnly? bis, DateTime jetzt, string kuerzel, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO zuteilungen (angebot_id, bewohner_id, ab, bis, erstellt, kuerzel) VALUES ($a, $b, $ab, $bis, $e, $k)", transaktion,
            ("$a", angebotId), ("$b", bewohnerId), ("$ab", Iso(ab)), ("$bis", bis is DateOnly x ? Iso(x) : null), ("$e", Zeit(jetzt)), ("$k", kuerzel));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // „Teilnahme beendet ab …“: ab diesem Tag nicht mehr dabei. Ein eigener Eintrag.
    public void ZuteilungBeenden(long zuteilungId, DateOnly ab, string kuerzel, DateTime jetzt)
    {
        var z = Angebote().SelectMany(a => a.Zuteilungen).FirstOrDefault(x => x.Id == zuteilungId)
                ?? throw new Eingabefehler(["Diese Zuteilung gibt es nicht mehr."]);
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (z.BeendetAb is not null) fehler.Add("Diese Teilnahme ist schon beendet.");
        if (ab <= z.Ab) fehler.Add($"„Beendet ab“ muss nach dem ersten Tag liegen ({Datum.Text(z.Ab)}).");
        if (z.Bis is DateOnly bis && ab > bis.AddDays(1)) fehler.Add($"Die Teilnahme endet ohnehin am {Datum.Text(bis)}.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        Ausfuehren("INSERT INTO zuteilung_ende (zuteilung_id, ab, erstellt, kuerzel) VALUES ($z, $ab, $e, $k)", null,
            ("$z", zuteilungId), ("$ab", Iso(ab)), ("$e", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        TherapieGeaendert?.Invoke();
    }

    // Therapien eines Bewohners an einem Tag (für den Wochenplan): Angebot, gültige Fassung, Zuteilung.
    public List<(Angebot Angebot, AngebotFassung Fassung, Zuteilung Zuteilung)> TherapienAm(long bewohnerId, DateOnly tag) =>
        Angebote().SelectMany(a => a.Zuteilungen.Where(z => z.BewohnerId == bewohnerId && z.GiltAm(tag)).Select(z => (a, a.Am(tag), z))).ToList();

    private List<Angebot> AngeboteLesen(SqliteTransaction? transaktion, string bedingung, params (string, object?)[] parameter)
    {
        var ids = new List<long>();
        using (var befehl = Befehl($"SELECT id FROM angebote {bedingung}", transaktion, parameter))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read()) ids.Add(leser.GetInt64(0));
        if (ids.Count == 0) return [];
        string liste = string.Join(",", ids);
        var fassungen = new Dictionary<long, List<AngebotFassung>>();
        using (var befehl = Befehl($"""
            SELECT angebot_id, gilt_ab, name, art, therapeut, ort, termine, hoechstens, hinweis, erstellt, kuerzel
            FROM angebot_fassungen WHERE angebot_id IN ({liste}) ORDER BY gilt_ab, id
            """, transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!fassungen.TryGetValue(leser.GetInt64(0), out var l)) fassungen[leser.GetInt64(0)] = l = [];
                l.Add(new AngebotFassung
                {
                    GiltAb = AusIso(leser.GetString(1)), Name = leser.GetString(2), Art = leser.GetString(3) == "einzel" ? AngebotArt.Einzel : AngebotArt.Gruppe,
                    Therapeut = leser.GetString(4), Ort = leser.GetString(5), Termine = TermineAus(leser.GetString(6)), Hoechstens = leser.GetInt32(7),
                    Hinweis = leser.GetString(8), Erstellt = AusZeit(leser.GetString(9)), Kuerzel = leser.GetString(10),
                });
            }
        var zuteilungen = new Dictionary<long, List<Zuteilung>>();
        using (var befehl = Befehl($"""
            SELECT z.id, z.angebot_id, z.bewohner_id, z.ab, z.bis, z.erstellt, z.kuerzel, e.ab, e.kuerzel, e.erstellt
            FROM zuteilungen z LEFT JOIN zuteilung_ende e ON e.zuteilung_id = z.id
            WHERE z.angebot_id IN ({liste}) ORDER BY z.ab, z.id
            """, transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!zuteilungen.TryGetValue(leser.GetInt64(1), out var l)) zuteilungen[leser.GetInt64(1)] = l = [];
                l.Add(new Zuteilung
                {
                    Id = leser.GetInt64(0), AngebotId = leser.GetInt64(1), BewohnerId = leser.GetInt64(2), Ab = AusIso(leser.GetString(3)),
                    Bis = leser.IsDBNull(4) ? null : AusIso(leser.GetString(4)), Erstellt = AusZeit(leser.GetString(5)), Kuerzel = leser.GetString(6),
                    BeendetAb = leser.IsDBNull(7) ? null : AusIso(leser.GetString(7)), BeendetVon = leser.IsDBNull(8) ? null : leser.GetString(8),
                    BeendetErstellt = leser.IsDBNull(9) ? null : AusZeit(leser.GetString(9)),
                });
            }
        return ids.Where(fassungen.ContainsKey)
            .Select(id => new Angebot { Id = id, Fassungen = fassungen[id], Zuteilungen = zuteilungen.GetValueOrDefault(id) ?? [] }).ToList();
    }

    // Demo: ausgedachte Angebote, Zuteilungen relativ zu heute (eine beendet, eine beginnt später,
    // eine Änderung „gilt ab“).
    private void DemoTherapieEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM angebote", transaktion);
        foreach (var d in Demodaten.Therapie(bewohner, Uhr()))
        {
            Ausfuehren("INSERT INTO angebote DEFAULT VALUES", transaktion);
            long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            foreach (var f in d.Fassungen) FassungEinfuegen(id, f, transaktion);
            foreach (var z in d.Zuteilungen)
            {
                long zid = ZuteilungEinfuegen(id, z.BewohnerId, z.Ab, z.Bis, z.Erstellt, z.Kuerzel, transaktion);
                if (z.BeendetAb is DateOnly e)
                    Ausfuehren("INSERT INTO zuteilung_ende (zuteilung_id, ab, erstellt, kuerzel) VALUES ($z, $ab, $e, $k)", transaktion,
                        ("$z", zid), ("$ab", Iso(e)), ("$e", Zeit(z.BeendetErstellt ?? z.Erstellt)), ("$k", z.BeendetVon ?? z.Kuerzel));
            }
        }
        SetzeMeta(DemoTherapie, "1", transaktion);
    }
}
