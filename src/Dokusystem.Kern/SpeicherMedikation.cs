using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Medikation im Speicher. Verordnungen und Änderungen werden nie geändert oder gelöscht
// (UPDATE verboten); jede Änderung ist ein eigener Eintrag mit Datum und Anordnung.
public sealed partial class Speicher
{
    public event Action? MedikationGeaendert;

    public List<Verordnung> Verordnungen(long bewohnerId) =>
        VerordnungenLesen("WHERE v.bewohner_id = $b", ("$b", bewohnerId));

    public Verordnung? Verordnung(long id) => VerordnungenLesen("WHERE v.id = $id", ("$id", id)).FirstOrDefault();

    public Verordnung VerordnungSpeichern(Verordnung v)
    {
        if (Bewohner(v.BewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (Medikament(v.MedikamentId) is null) throw new Eingabefehler(["Dieses Medikament gibt es nicht mehr."]);
        if (v.Id != 0) throw new InvalidOperationException("Verordnungen werden nicht überschrieben – Änderungen sind eigene Einträge.");
        using var transaktion = db.BeginTransaction();
        long id = VerordnungEinfuegen(v, transaktion);
        transaktion.Commit();
        MedikationGeaendert?.Invoke();
        return Verordnung(id)!;
    }

    public Verordnung AenderungSpeichern(long verordnungId, VerordnungsAenderung a)
    {
        var v = Verordnung(verordnungId) ?? throw new Eingabefehler(["Diese Verordnung gibt es nicht mehr."]);
        MedikationRegeln.Pruefen(v, a);
        using var transaktion = db.BeginTransaction();
        AenderungEinfuegen(verordnungId, a, transaktion);
        transaktion.Commit();
        MedikationGeaendert?.Invoke();
        return Verordnung(verordnungId)!;
    }

    private long VerordnungEinfuegen(Verordnung v, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO verordnungen (bewohner_id, medikament_id, staerke, einheit, morgens, mittags, abends, nachts, bedarf,
                                      bedarf_anlass, bedarf_hoechstens, beginn, arzt, angeordnet_am, hinweis, erstellt, kuerzel)
            VALUES ($b, $m, $s, $e, $mo, $mi, $ab, $na, $bd, $ba, $bh, $be, $ar, $an, $hi, $er, $ku)
            """, transaktion,
            ("$b", v.BewohnerId), ("$m", v.MedikamentId), ("$s", v.Staerke), ("$e", v.Einheit),
            ("$mo", v.Dosis.Morgens), ("$mi", v.Dosis.Mittags), ("$ab", v.Dosis.Abends), ("$na", v.Dosis.Nachts),
            ("$bd", v.Bedarf ? 1 : 0), ("$ba", v.BedarfAnlass), ("$bh", v.BedarfHoechstens), ("$be", Iso(v.Beginn)),
            ("$ar", v.Anordnung.Arzt), ("$an", Iso(v.Anordnung.Am)), ("$hi", v.Hinweis), ("$er", Zeit(v.Erstellt)), ("$ku", v.Kuerzel));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    private void AenderungEinfuegen(long verordnungId, VerordnungsAenderung a, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO verordnung_aenderungen (verordnung_id, art, von, bis, arzt, angeordnet_am, hinweis, erstellt, kuerzel)
            VALUES ($v, $art, $von, $bis, $ar, $an, $hi, $er, $ku)
            """, transaktion,
            ("$v", verordnungId), ("$art", MedikationRegeln.AenderungKuerzel(a.Art)), ("$von", Iso(a.Von)),
            ("$bis", a.Bis is DateOnly b ? Iso(b) : null), ("$ar", a.Anordnung.Arzt), ("$an", Iso(a.Anordnung.Am)),
            ("$hi", a.Hinweis), ("$er", Zeit(a.Erstellt)), ("$ku", a.Kuerzel));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        for (int i = 0; i < a.Stufen.Count; i++)
        {
            var s = a.Stufen[i];
            Ausfuehren("INSERT INTO stufen (aenderung_id, nr, ab, morgens, mittags, abends, nachts) VALUES ($a, $n, $ab, $mo, $mi, $abd, $na)", transaktion,
                ("$a", id), ("$n", i + 1), ("$ab", Iso(s.Ab)), ("$mo", s.Dosis.Morgens), ("$mi", s.Dosis.Mittags), ("$abd", s.Dosis.Abends), ("$na", s.Dosis.Nachts));
        }
    }

    private List<Verordnung> VerordnungenLesen(string bedingung, params (string, object?)[] parameter)
    {
        var namen = Medikamente().ToDictionary(m => m.Id, m => m.Anzeige);
        var liste = new List<Verordnung>();
        using (var befehl = Befehl($"""
            SELECT v.id, v.bewohner_id, v.medikament_id, v.staerke, v.einheit, v.morgens, v.mittags, v.abends, v.nachts, v.bedarf,
                   v.bedarf_anlass, v.bedarf_hoechstens, v.beginn, v.arzt, v.angeordnet_am, v.hinweis, v.erstellt, v.kuerzel
            FROM verordnungen v {bedingung} ORDER BY v.bedarf, v.id
            """, null, parameter))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                liste.Add(new Verordnung
                {
                    Id = leser.GetInt64(0),
                    BewohnerId = leser.GetInt64(1),
                    MedikamentId = leser.GetInt64(2),
                    Medikament = namen.GetValueOrDefault(leser.GetInt64(2), "?"),
                    Staerke = leser.GetString(3),
                    Einheit = leser.GetString(4),
                    Dosis = new Dosis(leser.GetString(5), leser.GetString(6), leser.GetString(7), leser.GetString(8)),
                    Bedarf = leser.GetInt64(9) == 1,
                    BedarfAnlass = leser.GetString(10),
                    BedarfHoechstens = leser.GetString(11),
                    Beginn = AusIso(leser.GetString(12)),
                    Anordnung = new Anordnung(leser.GetString(13), AusIso(leser.GetString(14))),
                    Hinweis = leser.GetString(15),
                    Erstellt = AusZeit(leser.GetString(16)),
                    Kuerzel = leser.GetString(17),
                });
        if (liste.Count == 0) return liste;
        string ids = string.Join(",", liste.Select(v => v.Id));
        var aenderungen = new Dictionary<long, List<VerordnungsAenderung>>();
        var stufen = new Dictionary<long, List<Stufe>>();
        using (var befehl = Befehl($"""
            SELECT s.aenderung_id, s.ab, s.morgens, s.mittags, s.abends, s.nachts FROM stufen s
            JOIN verordnung_aenderungen a ON a.id = s.aenderung_id WHERE a.verordnung_id IN ({ids}) ORDER BY s.aenderung_id, s.nr
            """, null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!stufen.TryGetValue(leser.GetInt64(0), out var l)) stufen[leser.GetInt64(0)] = l = [];
                l.Add(new Stufe(AusIso(leser.GetString(1)), new Dosis(leser.GetString(2), leser.GetString(3), leser.GetString(4), leser.GetString(5))));
            }
        using (var befehl = Befehl($"""
            SELECT id, verordnung_id, art, von, bis, arzt, angeordnet_am, hinweis, erstellt, kuerzel
            FROM verordnung_aenderungen WHERE verordnung_id IN ({ids}) ORDER BY erstellt, id
            """, null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                long id = leser.GetInt64(0);
                if (!aenderungen.TryGetValue(leser.GetInt64(1), out var l)) aenderungen[leser.GetInt64(1)] = l = [];
                l.Add(new VerordnungsAenderung
                {
                    Id = id,
                    Art = MedikationRegeln.AenderungAus(leser.GetString(2)),
                    Von = AusIso(leser.GetString(3)),
                    Bis = leser.IsDBNull(4) ? null : AusIso(leser.GetString(4)),
                    Anordnung = new Anordnung(leser.GetString(5), AusIso(leser.GetString(6))),
                    Hinweis = leser.GetString(7),
                    Erstellt = AusZeit(leser.GetString(8)),
                    Kuerzel = leser.GetString(9),
                    Stufen = stufen.GetValueOrDefault(id) ?? [],
                });
            }
        return liste.Select(v => v with { Aenderungen = aenderungen.GetValueOrDefault(v.Id) ?? [] }).ToList();
    }

    // Demo: Verordnungen relativ zu heute (mit Pause, Stufenplänen, Absetzen, Bedarf).
    private void DemoMedikationEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM verordnungen", transaktion);
        var medikamente = MedikamenteLesenIn(transaktion);
        foreach (var d in Demodaten.Medikation(bewohner, medikamente, Uhr()))
        {
            long id = VerordnungEinfuegen(d.Verordnung, transaktion);
            foreach (var a in d.Verordnung.Aenderungen) AenderungEinfuegen(id, a, transaktion);
        }
        SetzeMeta(DemoMedikation, "1", transaktion);
    }
}
