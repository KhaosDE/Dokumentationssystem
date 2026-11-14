using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Ziele speichern (Bauplan 43): Ziel mit Fassungen (nie überschrieben), „kein Ziel benennen“ mit Grund. Anlegen und ändern
// dürfen, die auch Maßnahmen anlegen dürfen (Fachkräfte und die Bezugspflege).
public sealed partial class Speicher
{
    private const string DemoZiele = "demo_ziele";

    public event Action? ZieleGeaendert;

    public List<Ziel> Ziele(long? bewohnerId = null) => ZieleLesen(null, bewohnerId);

    private List<Ziel> ZieleLesen(SqliteTransaction? transaktion, long? bewohnerId)
    {
        var fassungen = new Dictionary<long, List<ZielFassung>>();
        using (var befehl = Befehl("""
            SELECT f.ziel_id, f.wortlaut, f.lebensbereich, f.vereinbart_am, f.mit_wem, f.ueberpruefen_bis, f.stand, f.stand_am, f.stand_satz, f.kuerzel, f.erfasst
            FROM ziel_fassungen f JOIN ziele z ON z.id = f.ziel_id WHERE ($b IS NULL OR z.bewohner_id = $b) ORDER BY f.id
            """, transaktion, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                long id = leser.GetInt64(0);
                if (!fassungen.TryGetValue(id, out var liste)) fassungen[id] = liste = [];
                liste.Add(new ZielFassung
                {
                    Wortlaut = leser.GetString(1), Lebensbereich = leser.GetInt32(2), VereinbartAm = AusIso(leser.GetString(3)), MitWem = leser.GetString(4),
                    UeberpruefenBis = AusIso(leser.GetString(5)), Stand = Enum.Parse<ZielStand>(leser.GetString(6), true),
                    StandAm = leser.IsDBNull(7) ? null : AusIso(leser.GetString(7)), StandSatz = leser.GetString(8), Kuerzel = leser.GetString(9),
                    Erfasst = AusZeit(leser.GetString(10)),
                });
            }
        var ziele = new List<Ziel>();
        using (var befehl = Befehl("SELECT id, bewohner_id, art FROM ziele WHERE ($b IS NULL OR bewohner_id = $b) ORDER BY id", transaktion, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                if (fassungen.TryGetValue(leser.GetInt64(0), out var f))
                    ziele.Add(new Ziel(leser.GetInt64(0), leser.GetInt64(1), Enum.Parse<ZielArt>(leser.GetString(2), true), f));
        return ziele;
    }

    public Ziel ZielAnlegen(long bewohnerId, ZielArt art, ZielFassung f, string kuerzel, DateTime jetzt)
    {
        if (MassnahmenAnlegenGrund(kuerzel, bewohnerId) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (ZielRegeln.Pruefen(f) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT INTO ziele (bewohner_id, art, erstellt, kuerzel) VALUES ($b, $a, $e, $k)", transaktion,
            ("$b", bewohnerId), ("$a", art.ToString().ToLowerInvariant()), ("$e", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        ZielFassungEinfuegen(id, f with { Kuerzel = kuerzel.Trim(), Erfasst = jetzt }, transaktion);
        transaktion.Commit();
        ZieleGeaendert?.Invoke();
        return Ziele(bewohnerId).Single(z => z.Id == id);
    }

    // Ändern (auch Stand: erreicht, geändert, beendet): eine neue Fassung; frühere bleiben im Verlauf.
    public void ZielAendern(long zielId, ZielFassung f, string kuerzel, DateTime jetzt)
    {
        var ziel = Ziele().FirstOrDefault(z => z.Id == zielId) ?? throw new Eingabefehler(["Dieses Ziel gibt es nicht mehr."]);
        if (MassnahmenAnlegenGrund(kuerzel, ziel.BewohnerId) is string grund) throw new Eingabefehler([grund]);
        if (!ziel.Aktiv) throw new Eingabefehler([$"Das Ziel ist {ZielRegeln.StandText(ziel.Aktuell.Stand)} – bitte ein neues vereinbaren."]);
        if (ZielRegeln.Pruefen(f) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        using var transaktion = db.BeginTransaction();
        ZielFassungEinfuegen(zielId, f with { Kuerzel = kuerzel.Trim(), Erfasst = jetzt, StandAm = f.Stand == ZielStand.Aktiv ? null : f.StandAm ?? DateOnly.FromDateTime(jetzt) },
            transaktion);
        transaktion.Commit();
        ZieleGeaendert?.Invoke();
    }

    private void ZielFassungEinfuegen(long zielId, ZielFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO ziel_fassungen (ziel_id, wortlaut, lebensbereich, vereinbart_am, mit_wem, ueberpruefen_bis, stand, stand_am, stand_satz, kuerzel, erfasst)
            VALUES ($z, $w, $lb, $v, $m, $u, $s, $sa, $ss, $k, $e)
            """, transaktion, ("$z", zielId), ("$w", f.Wortlaut.Trim()), ("$lb", f.Lebensbereich), ("$v", Iso(f.VereinbartAm)), ("$m", f.MitWem.Trim()),
            ("$u", Iso(f.UeberpruefenBis)), ("$s", f.Stand.ToString().ToLowerInvariant()), ("$sa", f.StandAm is DateOnly d ? Iso(d) : null),
            ("$ss", f.StandSatz.Trim()), ("$k", f.Kuerzel), ("$e", Zeit(f.Erfasst)));

    public ZielVerzicht? Verzicht(long bewohnerId) => VerzichteLesen(bewohnerId).LastOrDefault();

    public Dictionary<long, ZielVerzicht> Verzichte() => VerzichteLesen(null).GroupBy(v => v.BewohnerId).ToDictionary(g => g.Key, g => g.Last());

    private List<ZielVerzicht> VerzichteLesen(long? bewohnerId)
    {
        var liste = new List<ZielVerzicht>();
        using var befehl = Befehl("SELECT bewohner_id, ab, grund, kuerzel, erfasst FROM ziel_verzicht WHERE ($b IS NULL OR bewohner_id = $b) ORDER BY id", null,
            [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new ZielVerzicht(leser.GetInt64(0), AusIso(leser.GetString(1)), leser.GetString(2), leser.GetString(3), AusZeit(leser.GetString(4))));
        return liste;
    }

    public void VerzichtSetzen(long bewohnerId, string grund, string kuerzel, DateTime jetzt)
    {
        if (MassnahmenAnlegenGrund(kuerzel, bewohnerId) is string fehlt) throw new Eingabefehler([fehlt]);
        if (grund.Trim().Length == 0) throw new Eingabefehler(["Bitte den Grund angeben."]);
        if (ZielRegeln.Aktive(Ziele(bewohnerId)).Count > 0) throw new Eingabefehler(["Es sind noch Ziele eingetragen – bitte diese zuerst beenden."]);
        Ausfuehren("INSERT INTO ziel_verzicht (bewohner_id, ab, grund, kuerzel, erfasst) VALUES ($b, $a, $g, $k, $e)", null,
            ("$b", bewohnerId), ("$a", Iso(DateOnly.FromDateTime(jetzt))), ("$g", grund.Trim()), ("$k", kuerzel.Trim()), ("$e", Zeit(jetzt)));
        ZieleGeaendert?.Invoke();
    }

    // Marken der Ziele je Bewohner (Akte, Prüfseite).
    public List<Marke> ZielMarken(long bewohnerId, DateOnly heute)
    {
        var w = Einstellungen.Laden();
        return ZielRegeln.Marken(Ziele(bewohnerId), Verzicht(bewohnerId), Massnahmen(bewohnerId), heute, w.ZieleLeitziele, w.ZieleNahziele);
    }

    // Demo: Leitziel und zwei Nahziele für die meisten; einige mit nur einem Nahziel, einige „kein Ziel benennen“, einige
    // nicht erfasst – damit die Prüfseite etwas zeigt.
    private void DemoZieleEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM bericht_ziel; DELETE FROM ziel_verzicht; DELETE FROM ziel_fassungen; DELETE FROM ziele", transaktion);
        var jetzt = Uhr();
        var bewohner = LesenIn(transaktion, AktiveBewohnerSql);
        bewohner.Sort(BewohnerRegeln.NachName);
        var heute = DateOnly.FromDateTime(jetzt);
        for (int i = 0; i < bewohner.Count; i++)
        {
            long id = bewohner[i].Id!.Value;
            if (i % 13 == 12) continue;   // nicht erfasst
            if (i % 11 == 10)
            {
                Ausfuehren("INSERT INTO ziel_verzicht (bewohner_id, ab, grund, kuerzel, erfasst) VALUES ($b, $a, $g, 'EF', $e)", transaktion,
                    ("$b", id), ("$a", Iso(heute.AddDays(-20))), ("$g", "möchte sich erst eingewöhnen"), ("$e", Zeit(jetzt.AddDays(-20))));
                continue;
            }
            foreach (var (art, f) in Demodaten.Ziele(i, heute))
            {
                Ausfuehren("INSERT INTO ziele (bewohner_id, art, erstellt, kuerzel) VALUES ($b, $a, $e, 'EF')", transaktion,
                    ("$b", id), ("$a", art.ToString().ToLowerInvariant()), ("$e", Zeit(f.Erfasst)));
                ZielFassungEinfuegen(Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture), f, transaktion);
            }
        }
        SetzeMeta(DemoZiele, "1", transaktion);
    }
}
