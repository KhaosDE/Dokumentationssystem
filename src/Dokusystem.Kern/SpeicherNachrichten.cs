using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Nachrichten im Speicher. Nachrichten werden nie geändert oder gelöscht (die Datenbank
// verbietet UPDATE); „gelesen“ und „archiviert“ stehen je Person in eigenen Tabellen.
public sealed partial class Speicher
{
    // Nach jeder Änderung an Nachrichten (neue Nachricht, gelesen, archiviert) – für Zähler.
    public event Action? NachrichtenGeaendert;

    // nurAktive: ohne Ausgeschiedene (z. B. als Empfänger); sonst alle (Namen an alten Einträgen).
    public List<Kollege> Kollegen(bool nurAktive = false)
    {
        var teams = new Dictionary<string, List<string>>();
        using (var befehl = Befehl("SELECT kuerzel, team FROM team_mitglieder ORDER BY team", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!teams.TryGetValue(leser.GetString(0), out var liste)) teams[leser.GetString(0)] = liste = [];
                liste.Add(leser.GetString(1));
            }
        var kollegen = new List<Kollege>();
        using (var befehl = Befehl($"SELECT kuerzel, vorname, nachname FROM kollegen {(nurAktive ? "WHERE aktiv = 1 " : "")}ORDER BY nachname, vorname", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                kollegen.Add(new Kollege(leser.GetString(0), leser.GetString(1), leser.GetString(2), teams.GetValueOrDefault(leser.GetString(0)) ?? []));
        return kollegen;
    }

    public Kollege? Kollege(string kuerzel) =>
        Kollegen().FirstOrDefault(k => k.Kuerzel.Equals(kuerzel.Trim(), StringComparison.OrdinalIgnoreCase));

    public List<string> Teams() => Kollegen().SelectMany(k => k.Teams).Distinct().Order(StringComparer.CurrentCulture).ToList();

    // Neue Nachricht (verlaufId == null) oder Antwort in einem Verlauf.
    public Nachricht NachrichtSenden(Nachricht n)
    {
        var kollegen = Kollegen().ToDictionary(k => k.Kuerzel);
        bool antwort = n.VerlaufId is not null;
        if (antwort)
        {
            var erste = NachrichtenLesen("WHERE id = $v", ("$v", n.VerlaufId)).FirstOrDefault()
                ?? throw new Eingabefehler(["Diesen Verlauf gibt es nicht mehr."]);
            if (erste.VerlaufId is not null) throw new InvalidOperationException("Antworten hängen an der ersten Nachricht des Verlaufs.");
            n = n with { Betreff = erste.Betreff, BewohnerId = erste.BewohnerId };
        }
        NachrichtenRegeln.Pruefen(n, kollegen, Teams(), antwort);
        if (n.BewohnerId is long b && Bewohner(b) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        using var transaktion = db.BeginTransaction();
        long id = NachrichtEinfuegen(n, transaktion);
        transaktion.Commit();
        NachrichtenGeaendert?.Invoke();
        return NachrichtenLesen("WHERE id = $id", ("$id", id)).Single();
    }

    private long NachrichtEinfuegen(Nachricht n, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO nachrichten (verlauf_id, von, betreff, text, gesendet, bewohner_id)
            VALUES ($v, $von, $b, $t, $g, $bw)
            """, transaktion, ("$v", n.VerlaufId), ("$von", n.Von), ("$b", n.Betreff.Trim()), ("$t", n.Text.Trim()),
            ("$g", Zeit(n.Gesendet)), ("$bw", n.BewohnerId));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        foreach (var e in n.An.Distinct())
            Ausfuehren("INSERT INTO nachricht_empfaenger (nachricht_id, art, ziel) VALUES ($n, $a, $z)", transaktion,
                ("$n", id), ("$a", e.IstTeam ? "team" : "person"), ("$z", e.Ziel));
        return id;
    }

    // Alle Verläufe, die „ich“ sehen darf, für einen Ordner; neueste oben. Optional nur zu einem Bewohner.
    public List<Verlauf> Verlaeufe(string ich, NachrichtenOrdner ordner, long? bewohnerId = null)
    {
        if (Kollege(ich) is not Kollege person) return [];
        var archiv = ArchivZeiten(person.Kuerzel);
        var liste = new List<Verlauf>();
        foreach (var gruppe in NachrichtenLesen("").GroupBy(n => n.Verlauf))
        {
            var nachrichten = gruppe.OrderBy(n => n.Gesendet).ThenBy(n => n.Id).ToList();
            if (!NachrichtenRegeln.Beteiligt(nachrichten, person)) continue;
            var erste = nachrichten[0];
            if (bewohnerId is not null && erste.BewohnerId != bewohnerId) continue;
            // Archiviert, solange danach nichts Neues an mich kam.
            bool archiviert = archiv.TryGetValue(gruppe.Key, out var seit) &&
                              !nachrichten.Any(n => n.Gesendet > seit && n.Von != person.Kuerzel && NachrichtenRegeln.AnMich(n, person));
            bool bekommen = nachrichten.Any(n => n.Von != person.Kuerzel && NachrichtenRegeln.AnMich(n, person));
            bool gesendet = nachrichten.Any(n => n.Von == person.Kuerzel);
            bool passt = ordner switch
            {
                NachrichtenOrdner.Posteingang => bekommen && !archiviert,
                NachrichtenOrdner.Gesendet => gesendet,
                _ => archiviert,
            };
            if (!passt) continue;
            liste.Add(new Verlauf
            {
                Id = gruppe.Key,
                Betreff = erste.Betreff,
                BewohnerId = erste.BewohnerId,
                Nachrichten = nachrichten,
                Ungelesen = nachrichten.Count(n => NachrichtenRegeln.Ungelesen(n, person)),
                Archiviert = archiviert,
            });
        }
        return liste.OrderByDescending(v => v.Letzte).ToList();
    }

    public Verlauf? VerlaufLesen(string ich, long verlaufId) =>
        Enum.GetValues<NachrichtenOrdner>().SelectMany(o => Verlaeufe(ich, o)).FirstOrDefault(v => v.Id == verlaufId);

    public int UngeleseneNachrichten(string ich) =>
        Kollege(ich) is Kollege person ? NachrichtenLesen("").Count(n => NachrichtenRegeln.Ungelesen(n, person)) : 0;

    // Beim Öffnen eines Verlaufs: alles darin, was an mich ging, gilt als gelesen.
    public void VerlaufGelesen(string ich, long verlaufId, DateTime zeit)
    {
        if (Kollege(ich) is not Kollege person) return;
        var offen = NachrichtenLesen("WHERE id = $v OR verlauf_id = $v", ("$v", verlaufId)).Where(n => NachrichtenRegeln.Ungelesen(n, person)).ToList();
        if (offen.Count == 0) return;
        using var transaktion = db.BeginTransaction();
        foreach (var n in offen)
            Ausfuehren("INSERT OR IGNORE INTO nachricht_gelesen (nachricht_id, kuerzel, zeit) VALUES ($n, $k, $z)", transaktion,
                ("$n", n.Id), ("$k", person.Kuerzel), ("$z", Zeit(zeit)));
        transaktion.Commit();
        NachrichtenGeaendert?.Invoke();
    }

    public void VerlaufArchivieren(string ich, long verlaufId, DateTime zeit)
    {
        if (Kollege(ich) is not Kollege person) throw new Eingabefehler(["Bitte oben rechts unter „Angemeldet als …“ einen Benutzer wählen."]);
        Ausfuehren("INSERT INTO verlauf_archiv (verlauf_id, kuerzel, zeit) VALUES ($v, $k, $z) ON CONFLICT(verlauf_id, kuerzel) DO UPDATE SET zeit = excluded.zeit",
            null, ("$v", verlaufId), ("$k", person.Kuerzel), ("$z", Zeit(zeit)));
        NachrichtenGeaendert?.Invoke();
    }

    // „Zurück in den Posteingang“: nur die Ablage ändert sich, die Nachrichten bleiben.
    public void VerlaufZurueckholen(string ich, long verlaufId)
    {
        if (Kollege(ich) is not Kollege person) return;
        Ausfuehren("DELETE FROM verlauf_archiv WHERE verlauf_id = $v AND kuerzel = $k", null, ("$v", verlaufId), ("$k", person.Kuerzel));
        NachrichtenGeaendert?.Invoke();
    }

    private Dictionary<long, DateTime> ArchivZeiten(string kuerzel)
    {
        var zeiten = new Dictionary<long, DateTime>();
        using var befehl = Befehl("SELECT verlauf_id, zeit FROM verlauf_archiv WHERE kuerzel = $k", null, [("$k", kuerzel)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) zeiten[leser.GetInt64(0)] = AusZeit(leser.GetString(1));
        return zeiten;
    }

    private List<Nachricht> NachrichtenLesen(string bedingung, params (string, object?)[] parameter)
    {
        var liste = new List<Nachricht>();
        using (var befehl = Befehl($"SELECT id, verlauf_id, von, betreff, text, gesendet, bewohner_id FROM nachrichten {bedingung}", null, parameter))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                liste.Add(new Nachricht
                {
                    Id = leser.GetInt64(0),
                    VerlaufId = leser.IsDBNull(1) ? null : leser.GetInt64(1),
                    Von = leser.GetString(2),
                    Betreff = leser.GetString(3),
                    Text = leser.GetString(4),
                    Gesendet = AusZeit(leser.GetString(5)),
                    BewohnerId = leser.IsDBNull(6) ? null : leser.GetInt64(6),
                });
        if (liste.Count == 0) return liste;
        string ids = string.Join(",", liste.Select(n => n.Id));
        var an = new Dictionary<long, List<Empfaenger>>();
        using (var befehl = Befehl($"SELECT nachricht_id, art, ziel FROM nachricht_empfaenger WHERE nachricht_id IN ({ids})", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!an.TryGetValue(leser.GetInt64(0), out var l)) an[leser.GetInt64(0)] = l = [];
                l.Add(new Empfaenger(leser.GetString(1) == "team", leser.GetString(2)));
            }
        var gelesen = new Dictionary<long, List<string>>();
        using (var befehl = Befehl($"SELECT nachricht_id, kuerzel FROM nachricht_gelesen WHERE nachricht_id IN ({ids})", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!gelesen.TryGetValue(leser.GetInt64(0), out var l)) gelesen[leser.GetInt64(0)] = l = [];
                l.Add(leser.GetString(1));
            }
        return liste.Select(n => n with
        {
            An = an.GetValueOrDefault(n.Id) ?? [],
            GelesenVon = gelesen.GetValueOrDefault(n.Id) ?? [],
        }).ToList();
    }

    // Demo: Kollegen, Teams und Nachrichten neu laden (Zeiten relativ zu jetzt).
    private void DemoNachrichtenEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        var demo = Demodaten.Nachrichten(bewohner, Uhr());
        // Die Kolleginnen und Kollegen (Mitarbeiter) und ihre Bereiche lädt DemoMitarbeiterEinfuegen.
        Ausfuehren("DELETE FROM verlauf_archiv; DELETE FROM nachricht_gelesen; DELETE FROM nachricht_empfaenger; DELETE FROM nachrichten;", transaktion);
        foreach (var v in demo.Verlaeufe)
        {
            long? verlauf = null;
            foreach (var d in v.Nachrichten)
            {
                long id = NachrichtEinfuegen(d.Nachricht with { VerlaufId = verlauf }, transaktion);
                verlauf ??= id;
                foreach (var leser in d.Nachricht.GelesenVon)
                    Ausfuehren("INSERT INTO nachricht_gelesen (nachricht_id, kuerzel, zeit) VALUES ($n, $k, $z)", transaktion,
                        ("$n", id), ("$k", leser), ("$z", Zeit(d.Nachricht.Gesendet.AddMinutes(20))));
            }
            foreach (var wer in v.ArchiviertVon)
                Ausfuehren("INSERT INTO verlauf_archiv (verlauf_id, kuerzel, zeit) VALUES ($v, $k, $z)", transaktion,
                    ("$v", verlauf), ("$k", wer), ("$z", Zeit(v.Nachrichten[^1].Nachricht.Gesendet.AddHours(1))));
        }
        SetzeMeta(DemoNachrichten, "1", transaktion);
    }
}
