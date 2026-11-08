using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Medikamentendatenbank im Speicher. Jede Änderung ist eine neue Fassung (UPDATE verboten).
public sealed partial class Speicher
{
    public List<Medikament> Medikamente(string suche = "") =>
        MedikamenteLesen(null, "").Where(m => MedikamentRegeln.Passt(m, suche))
            .OrderBy(m => m.Wirkstoff, StringComparer.CurrentCulture).ThenBy(m => m.Aktuell.Darreichungsform).ToList();

    public Medikament? Medikament(long id) => MedikamenteLesen(null, "WHERE id = $id", ("$id", id)).FirstOrDefault();

    // Innerhalb einer laufenden Transaktion (z. B. beim Laden der Demo-Medikation).
    private List<Medikament> MedikamenteLesenIn(SqliteTransaction transaktion) => MedikamenteLesen(transaktion, "");

    // Neues Medikament „von Hand“ (gekennzeichnet als „selbst angelegt“).
    public Medikament MedikamentAnlegen(MedikamentFassung f)
    {
        Doppelt(f, null);
        using var transaktion = db.BeginTransaction();
        long id = MedikamentEinfuegen(f, selbstAngelegt: true, transaktion);
        transaktion.Commit();
        return Medikament(id)!;
    }

    // Änderung: eine neue Fassung; die bisherigen bleiben im Verlauf.
    public Medikament MedikamentAendern(long id, MedikamentFassung f)
    {
        var bisher = Medikament(id) ?? throw new Eingabefehler(["Dieses Medikament gibt es nicht mehr."]);
        if (MedikamentRegeln.Unterschiede(bisher.Aktuell, f).Count == 0) return bisher;
        Doppelt(f, id);
        using var transaktion = db.BeginTransaction();
        FassungEinfuegen(id, f, transaktion);
        transaktion.Commit();
        return Medikament(id)!;
    }

    // Sperren oder wieder freigeben (Recht „Medikamentendatenbank verwalten“): eine neue Fassung.
    public Medikament MedikamentSperren(long id, bool gesperrt, string kuerzel)
    {
        var bisher = Medikament(id) ?? throw new Eingabefehler(["Dieses Medikament gibt es nicht mehr."]);
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Kürzel fehlt (oben rechts anmelden)."]);
        return MedikamentAendern(id, bisher.Aktuell with { Gesperrt = gesperrt, Geaendert = Uhr(), Kuerzel = kuerzel.Trim() });
    }

    private void Doppelt(MedikamentFassung f, long? selbst)
    {
        if (Medikamente().Any(m => m.Id != selbst &&
                                   m.Wirkstoff.Equals(f.Wirkstoff, StringComparison.CurrentCultureIgnoreCase) &&
                                   m.Aktuell.Darreichungsform.Equals(f.Darreichungsform, StringComparison.CurrentCultureIgnoreCase)))
            throw new Eingabefehler([$"„{f.Wirkstoff}“ als „{f.Darreichungsform}“ gibt es schon – bitte den vorhandenen Eintrag ändern."]);
    }

    private long MedikamentEinfuegen(MedikamentFassung f, bool selbstAngelegt, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO medikamente (selbst_angelegt) VALUES ($s)", transaktion, ("$s", selbstAngelegt ? 1 : 0));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        FassungEinfuegen(id, f, transaktion);
        return id;
    }

    private void FassungEinfuegen(long id, MedikamentFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO medikament_fassungen (medikament_id, wirkstoff, handelsnamen, staerken, darreichungsform, quelle, pruefstatus, geaendert, kuerzel, geprueft_am, gesperrt, zulassungsnummer)
            VALUES ($m, $w, $h, $s, $d, $q, $p, $g, $k, $ga, $gs, $z)
            """, transaktion,
            ("$m", id), ("$w", f.Wirkstoff), ("$h", JsonSerializer.Serialize(f.Handelsnamen)), ("$s", JsonSerializer.Serialize(f.Staerken)),
            ("$d", f.Darreichungsform), ("$q", f.Quelle), ("$p", f.Pruefstatus == Pruefstatus.Bestaetigt ? "bestaetigt" : "ungeprueft"),
            ("$g", Zeit(f.Geaendert)), ("$k", f.Kuerzel), ("$ga", f.GeprueftAm is DateOnly d ? Iso(d) : null), ("$gs", f.Gesperrt ? 1 : 0), ("$z", f.Zulassungsnummer));

    private List<Medikament> MedikamenteLesen(SqliteTransaction? transaktion, string bedingung, params (string, object?)[] parameter)
    {
        var liste = new List<Medikament>();
        using (var befehl = Befehl($"SELECT id, selbst_angelegt FROM medikamente {bedingung}", transaktion, parameter))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read()) liste.Add(new Medikament { Id = leser.GetInt64(0), SelbstAngelegt = leser.GetInt64(1) == 1 });
        if (liste.Count == 0) return liste;
        var fassungen = new Dictionary<long, List<MedikamentFassung>>();
        string ids = string.Join(",", liste.Select(m => m.Id));
        using (var befehl = Befehl($"""
            SELECT medikament_id, wirkstoff, handelsnamen, staerken, darreichungsform, quelle, pruefstatus, geaendert, kuerzel, geprueft_am, gesperrt, zulassungsnummer
            FROM medikament_fassungen WHERE medikament_id IN ({ids}) ORDER BY id
            """, transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!fassungen.TryGetValue(leser.GetInt64(0), out var l)) fassungen[leser.GetInt64(0)] = l = [];
                l.Add(new MedikamentFassung
                {
                    Wirkstoff = leser.GetString(1),
                    Handelsnamen = JsonSerializer.Deserialize<List<string>>(leser.GetString(2)) ?? [],
                    Staerken = JsonSerializer.Deserialize<List<string>>(leser.GetString(3)) ?? [],
                    Darreichungsform = leser.GetString(4),
                    Quelle = leser.GetString(5),
                    Pruefstatus = leser.GetString(6) == "bestaetigt" ? Pruefstatus.Bestaetigt : Pruefstatus.Ungeprueft,
                    Geaendert = AusZeit(leser.GetString(7)),
                    Kuerzel = leser.GetString(8),
                    GeprueftAm = leser.IsDBNull(9) ? null : AusIso(leser.GetString(9)),
                    Gesperrt = leser.GetInt64(10) == 1,
                    Zulassungsnummer = leser.GetString(11),
                });
            }
        return liste.Where(m => fassungen.ContainsKey(m.Id)).Select(m => m with { Fassungen = fassungen[m.Id] }).ToList();
    }

    // Demo: den Bestand aus daten/demo/medikamente.json neu laden (alles „ungeprüft“).
    private void DemoMedikamenteEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM medikamente", transaktion);
        foreach (var f in Demodaten.Medikamente(Uhr()))
            MedikamentEinfuegen(f, selbstAngelegt: false, transaktion);
        SetzeMeta(DemoMedikamente, "1", transaktion);
    }
}
