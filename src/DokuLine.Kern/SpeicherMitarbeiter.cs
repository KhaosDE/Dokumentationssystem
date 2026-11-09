using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

public enum Liste { Bereiche, Positionen }

// Mitarbeiter, Bereiche, Positionen und Rechte im Speicher. Die Mitarbeiter stehen in der Tabelle
// „kollegen“ (seit den Nachrichten), ihre Bereiche in „team_mitglieder“ – so gehen Nachrichten an
// einen Bereich an alle seine Mitarbeiter. Gelöscht wird kein Mitarbeiter, nur „ausgeschieden“.
public sealed partial class Speicher
{
    private const string Grundbestand = "rechte_vorbelegt";
    private const string DemoMitarbeiterSchluessel = "demo_mitarbeiter";
    private const string AdminSchluessel = "admin_tst";   // Schritt 26a2: Position Administrator und TST einmal ergänzt
    public const string AdminKuerzel = "TST";

    // Was beim Öffnen von selbst ergänzt wurde (fürs Protokoll; die Oberfläche schreibt es in update.log).
    public List<string> Startprotokoll { get; } = [];

    public event Action? MitarbeiterGeaendert;

    public List<Mitarbeiter> AlleMitarbeiter()
    {
        var bereiche = Zuordnungen("SELECT kuerzel, team FROM team_mitglieder");
        var positionen = Zuordnungen("SELECT mp.kuerzel, mp.position FROM mitarbeiter_positionen mp JOIN positionen p ON p.name = mp.position ORDER BY p.nr, p.name");
        var liste = new List<Mitarbeiter>();
        using var befehl = Befehl("SELECT kuerzel, vorname, nachname, fachkraft, aktiv, farbe FROM kollegen ORDER BY nachname, vorname", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
        {
            string k = leser.GetString(0);
            liste.Add(new Mitarbeiter(k, leser.GetString(1), leser.GetString(2), leser.GetInt64(3) == 1, leser.GetInt64(4) == 1,
                bereiche.GetValueOrDefault(k) ?? [], positionen.GetValueOrDefault(k) ?? []) { Farbe = leser.IsDBNull(5) ? null : leser.GetString(5) });
        }
        return liste;
    }

    private Dictionary<string, List<string>> Zuordnungen(string sql)
    {
        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        using var befehl = Befehl(sql, null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
        {
            if (!d.TryGetValue(leser.GetString(0), out var l)) d[leser.GetString(0)] = l = [];
            l.Add(leser.GetString(1));
        }
        return d;
    }

    public Mitarbeiter? MitarbeiterLesen(string? kuerzel) =>
        string.IsNullOrWhiteSpace(kuerzel) ? null
            : AlleMitarbeiter().FirstOrDefault(m => m.Kuerzel.Equals(kuerzel.Trim(), StringComparison.OrdinalIgnoreCase));

    // Anlegen (neu) oder ändern. Das Kürzel eines vorhandenen Mitarbeiters bleibt, wie es ist.
    public Mitarbeiter MitarbeiterSpeichern(Mitarbeiter m, bool neu)
    {
        m = m with
        {
            Kuerzel = m.Kuerzel.Trim(), Vorname = m.Vorname.Trim(), Nachname = m.Nachname.Trim(),
            Bereiche = m.Bereiche.Distinct().ToList(), Positionen = m.Positionen.Distinct().ToList(),
        };
        var alle = AlleMitarbeiter();
        if (!neu && !alle.Any(x => x.Kuerzel.Equals(m.Kuerzel, StringComparison.OrdinalIgnoreCase)))
            throw new Eingabefehler(["Diesen Mitarbeiter gibt es nicht."]);
        var fehler = RechteRegeln.Pruefen(m, neu ? alle.Select(x => x.Kuerzel).ToList() : []);
        // Den letzten aktiven Administrator weder ausscheiden lassen noch seine Position entziehen.
        var bisher = alle.FirstOrDefault(x => x.Kuerzel.Equals(m.Kuerzel, StringComparison.OrdinalIgnoreCase));
        if (!neu && RechteRegeln.IstAdministrator(bisher) && !RechteRegeln.IstAdministrator(m) &&
            !alle.Any(x => x != bisher && RechteRegeln.IstAdministrator(x)))
            fehler.Add(RechteRegeln.LetzterAdminText);
        var bekannteBereiche = Liste_(Liste.Bereiche).Select(x => x.Name).ToHashSet();
        var bekanntePositionen = Liste_(Liste.Positionen).Select(x => x.Name).ToHashSet();
        if (m.Bereiche.Any(b => !bekannteBereiche.Contains(b))) fehler.Add("Einen der Bereiche gibt es nicht.");
        if (m.Positionen.Any(p => !bekanntePositionen.Contains(p))) fehler.Add("Eine der Positionen gibt es nicht.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        using (var transaktion = db.BeginTransaction())
        {
            MitarbeiterSchreiben(m, neu, transaktion);
            transaktion.Commit();
        }
        MitarbeiterGeaendert?.Invoke();
        return MitarbeiterLesen(m.Kuerzel)!;
    }

    private void MitarbeiterSchreiben(Mitarbeiter m, bool neu, SqliteTransaction transaktion)
    {
        if (neu)
            Ausfuehren("INSERT INTO kollegen (kuerzel, vorname, nachname, fachkraft, aktiv, farbe) VALUES ($k, $v, $n, $f, $a, $c)", transaktion,
                ("$k", m.Kuerzel), ("$v", m.Vorname), ("$n", m.Nachname), ("$f", m.Fachkraft ? 1 : 0), ("$a", m.Aktiv ? 1 : 0), ("$c", m.Farbe));
        else
            Ausfuehren("UPDATE kollegen SET vorname = $v, nachname = $n, fachkraft = $f, aktiv = $a WHERE kuerzel = $k COLLATE NOCASE", transaktion,
                ("$k", m.Kuerzel), ("$v", m.Vorname), ("$n", m.Nachname), ("$f", m.Fachkraft ? 1 : 0), ("$a", m.Aktiv ? 1 : 0));
        Ausfuehren("DELETE FROM team_mitglieder WHERE kuerzel = $k COLLATE NOCASE; DELETE FROM mitarbeiter_positionen WHERE kuerzel = $k COLLATE NOCASE",
            transaktion, ("$k", m.Kuerzel));
        string kuerzel = Convert.ToString(WertIn(transaktion, "SELECT kuerzel FROM kollegen WHERE kuerzel = $k COLLATE NOCASE", ("$k", m.Kuerzel)),
            CultureInfo.InvariantCulture)!;
        foreach (var b in m.Bereiche)
            Ausfuehren("INSERT INTO team_mitglieder (team, kuerzel) VALUES ($t, $k)", transaktion, ("$t", b), ("$k", kuerzel));
        foreach (var p in m.Positionen)
            Ausfuehren("INSERT INTO mitarbeiter_positionen (kuerzel, position) VALUES ($k, $p)", transaktion, ("$k", kuerzel), ("$p", p));
    }

    // ------------------------------------------------------------------ Bereiche und Positionen

    public List<Listeneintrag> Bereichsliste() => Liste_(Liste.Bereiche);
    public List<Listeneintrag> Positionsliste() => Liste_(Liste.Positionen);

    private List<Listeneintrag> Liste_(Liste art)
    {
        string sql = art == Liste.Bereiche
            ? "SELECT name, aktiv, (SELECT COUNT(*) FROM team_mitglieder t WHERE t.team = b.name) FROM bereiche b ORDER BY nr, name"
            : "SELECT name, aktiv, (SELECT COUNT(*) FROM mitarbeiter_positionen m WHERE m.position = p.name) FROM positionen p ORDER BY nr, name";
        var liste = new List<Listeneintrag>();
        using var befehl = Befehl(sql, null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(new Listeneintrag(leser.GetString(0), leser.GetInt64(1) == 1, leser.GetInt32(2)));
        return liste;
    }

    private static string Tabelle(Liste art) => art == Liste.Bereiche ? "bereiche" : "positionen";

    private static string NamePruefen(string name)
    {
        name = Bereiche.Bereinigen(name);
        if (Bereiche.Pruefen(name) is string f) throw new Eingabefehler([f.Replace("Bereich", "Name")]);
        return name;
    }

    public void ListeneintragAnlegen(Liste art, string name)
    {
        name = NamePruefen(name);
        if (Liste_(art).Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new Eingabefehler([$"„{name}“ gibt es schon."]);
        Ausfuehren($"INSERT INTO {Tabelle(art)} (name, nr) VALUES ($n, (SELECT COALESCE(MAX(nr), 0) + 1 FROM {Tabelle(art)}))", null, ("$n", name));
        MitarbeiterGeaendert?.Invoke();
    }

    // Umbenennen: Zuordnungen gehen mit (Positionen über die Datenbank, Bereiche auch bei Bewohnern
    // und als Empfänger von Nachrichten).
    public void ListeneintragUmbenennen(Liste art, string alt, string neu)
    {
        neu = NamePruefen(neu);
        if (art == Liste.Positionen && alt == RechteRegeln.Administrator && neu != alt) throw new Eingabefehler([RechteRegeln.AdminPositionText]);
        if (alt == neu) return;
        if (Liste_(art).Any(x => x.Name.Equals(neu, StringComparison.OrdinalIgnoreCase) && x.Name != alt)) throw new Eingabefehler([$"„{neu}“ gibt es schon."]);
        using (var transaktion = db.BeginTransaction())
        {
            Ausfuehren($"UPDATE {Tabelle(art)} SET name = $neu WHERE name = $alt", transaktion, ("$neu", neu), ("$alt", alt));
            if (art == Liste.Bereiche)
                Ausfuehren("UPDATE team_mitglieder SET team = $neu WHERE team = $alt; " +
                           "UPDATE bewohner SET wohnbereich = $neu WHERE wohnbereich = $alt; " +
                           "UPDATE nachricht_empfaenger SET ziel = $neu WHERE art = 'team' AND ziel = $alt", transaktion, ("$neu", neu), ("$alt", alt));
            transaktion.Commit();
        }
        MitarbeiterGeaendert?.Invoke();
    }

    public void ListeneintragAktivSetzen(Liste art, string name, bool aktiv)
    {
        if (art == Liste.Positionen && name == RechteRegeln.Administrator && !aktiv) throw new Eingabefehler([RechteRegeln.AdminPositionText]);
        Ausfuehren($"UPDATE {Tabelle(art)} SET aktiv = $a WHERE name = $n", null, ("$a", aktiv ? 1 : 0), ("$n", name));
        MitarbeiterGeaendert?.Invoke();
    }

    // Löschen nur, solange niemand zugeordnet ist (und bei Bereichen kein Bewohner dort wohnt).
    public void ListeneintragLoeschen(Liste art, string name)
    {
        if (art == Liste.Positionen && name == RechteRegeln.Administrator) throw new Eingabefehler([RechteRegeln.AdminPositionText]);
        var eintrag = Liste_(art).FirstOrDefault(x => x.Name == name) ?? throw new Eingabefehler([$"„{name}“ gibt es nicht."]);
        bool bewohner = art == Liste.Bereiche && AlleBewohner().Any(b => b.Wohnbereich == name);
        if (eintrag.Zugeordnet > 0 || bewohner)
            throw new Eingabefehler([$"„{name}“ ist noch zugeordnet – stattdessen stilllegen."]);
        Ausfuehren($"DELETE FROM {Tabelle(art)} WHERE name = $n", null, ("$n", name));
        MitarbeiterGeaendert?.Invoke();
    }

    // ------------------------------------------------------------------ Rechte

    // Funktion → Positionen mit Häkchen.
    public Dictionary<string, HashSet<string>> Rechte()
    {
        var d = Funktionen.Alle.ToDictionary(f => f.Schluessel, _ => new HashSet<string>());
        using var befehl = Befehl("SELECT funktion, position FROM rechte", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            if (d.TryGetValue(leser.GetString(0), out var s)) s.Add(leser.GetString(1));
        return d;
    }

    public HashSet<string> NurFachkraft()
    {
        var s = new HashSet<string>();
        using var befehl = Befehl("SELECT funktion FROM rechte_fachkraft", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) s.Add(leser.GetString(0));
        return s;
    }

    public void RechtSetzen(string funktion, string position, bool an)
    {
        if (position == RechteRegeln.Administrator && !an) throw new Eingabefehler([RechteRegeln.AdminImmerText]);
        Ausfuehren(an ? "INSERT OR IGNORE INTO rechte (funktion, position) VALUES ($f, $p)" : "DELETE FROM rechte WHERE funktion = $f AND position = $p",
            null, ("$f", funktion), ("$p", position));
        MitarbeiterGeaendert?.Invoke();
    }

    public void NurFachkraftSetzen(string funktion, bool an)
    {
        Ausfuehren(an ? "INSERT OR IGNORE INTO rechte_fachkraft (funktion) VALUES ($f)" : "DELETE FROM rechte_fachkraft WHERE funktion = $f",
            null, ("$f", funktion));
        MitarbeiterGeaendert?.Invoke();
    }

    // Warum der Angemeldete etwas nicht darf (null = darf).
    public string? Grund(string? kuerzel, string funktion) => RechteRegeln.Grund(MitarbeiterLesen(kuerzel), funktion, Rechte(), NurFachkraft());

    public bool Darf(string? kuerzel, string funktion) => Grund(kuerzel, funktion) is null;

    // Therapieangebote nach Anbieter (Schritt 22).
    public string? AngebotGrund(string? kuerzel, string anbieter) => RechteRegeln.AngebotGrund(MitarbeiterLesen(kuerzel), anbieter, Rechte(), NurFachkraft());
    public List<string> AngebotAnbieter(string? kuerzel) =>
        RechteRegeln.Anbieter(MitarbeiterLesen(kuerzel), AlleBewohner().Select(b => b.Wohnbereich).ToHashSet(), Rechte(), NurFachkraft());

    // Anmelden: nur aktive Mitarbeiter. Rückgabe: der Mitarbeiter oder ein Eingabefehler.
    public Mitarbeiter Anmelden(string kuerzel)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Bitte das Kürzel eingeben."]);
        var m = MitarbeiterLesen(kuerzel) ?? throw new Eingabefehler([$"Das Kürzel „{kuerzel.Trim()}“ ist nicht bekannt."]);
        if (!m.Aktiv) throw new Eingabefehler([$"{m.Name} ist ausgeschieden und kann sich nicht mehr anmelden."]);
        return m;
    }

    // ------------------------------------------------------------------ Grundbestand und Demo

    // Positionen und Rechte-Vorbelegung, einmal je Datenbank (auch ohne Demo-Daten).
    private void GrundbestandPruefen()
    {
        if (Meta(Grundbestand) is null)
        {
            using var transaktion = db.BeginTransaction();
            if (WertIn(transaktion, "SELECT wert FROM meta WHERE schluessel = $s", ("$s", Grundbestand)) is null)
            {
                GrundbestandEinfuegen(transaktion);
                transaktion.Commit();
            }
        }
        AdministratorPruefen();
        // Später dazugekommene Funktionen einmal vorbelegen (bestehende Häkchen bleiben, wie sie sind).
        foreach (var f in RechteRegeln.Nachtraeglich.Where(f => Meta(NachtraeglichSchluessel(f)) is null))
        {
            using var transaktion = db.BeginTransaction();
            if (WertIn(transaktion, "SELECT wert FROM meta WHERE schluessel = $s", ("$s", NachtraeglichSchluessel(f))) is not null) continue;
            FunktionVorbelegen(f, transaktion);
            transaktion.Commit();
        }
        // „Nur Fachkräfte“ für später dazugekommene Fälle (0.33.0: Visiten) einmal setzen; danach gilt die Rechte-Tabelle.
        foreach (var f in RechteRegeln.NurFachkraftNachtraeglich.Where(f => Meta(FachkraftSchluessel(f)) is null))
        {
            using var transaktion = db.BeginTransaction();
            Ausfuehren("INSERT OR IGNORE INTO rechte_fachkraft (funktion) VALUES ($f)", transaktion, ("$f", f));
            SetzeMeta(FachkraftSchluessel(f), "1", transaktion);
            transaktion.Commit();
        }
    }

    private static string NachtraeglichSchluessel(string funktion) => $"rechte_vorbelegt_{funktion}";
    private static string FachkraftSchluessel(string funktion) => $"nur_fachkraft_vorbelegt_{funktion}";

    // Schritt 26a2, einmal je Datenbank: Position „Administrator“ (an erster Stelle, alle Häkchen) und –
    // in einer schon benutzten Datenbank ohne TST – den Mitarbeiter TST in allen Bereichen. Ein
    // vorhandener TST wird nicht verändert.
    private void AdministratorPruefen()
    {
        if (Meta(AdminSchluessel) is not null) return;
        using var transaktion = db.BeginTransaction();
        if (WertIn(transaktion, "SELECT wert FROM meta WHERE schluessel = $s", ("$s", AdminSchluessel)) is not null) return;
        Ausfuehren("INSERT OR IGNORE INTO positionen (name, nr) VALUES ($n, (SELECT COALESCE(MIN(nr), 1) - 1 FROM positionen))", transaktion,
            ("$n", RechteRegeln.Administrator));
        Ausfuehren("UPDATE positionen SET aktiv = 1 WHERE name = $n", transaktion, ("$n", RechteRegeln.Administrator));
        foreach (var (f, _) in Funktionen.Alle)
            Ausfuehren("INSERT OR IGNORE INTO rechte (funktion, position) VALUES ($f, $p)", transaktion, ("$f", f), ("$p", RechteRegeln.Administrator));
        bool benutzt = Convert.ToInt64(WertIn(transaktion, "SELECT COUNT(*) FROM kollegen"), CultureInfo.InvariantCulture) > 0;
        bool tstDa = WertIn(transaktion, "SELECT kuerzel FROM kollegen WHERE kuerzel = $k COLLATE NOCASE", ("$k", AdminKuerzel)) is not null;
        if (benutzt && !tstDa)
        {
            var bereiche = new List<string>();
            using (var befehl = Befehl("SELECT name FROM bereiche ORDER BY nr, name", transaktion, []))
            using (var leser = befehl.ExecuteReader())
                while (leser.Read()) bereiche.Add(leser.GetString(0));
            MitarbeiterSchreiben(new Mitarbeiter(AdminKuerzel, "Test", "Administrator", true, true, bereiche, [RechteRegeln.Administrator]), neu: true, transaktion);
            Startprotokoll.Add($"Mitarbeiter {AdminKuerzel} (Position „{RechteRegeln.Administrator}“, alle Bereiche) ergänzt.");
        }
        SetzeMeta(AdminSchluessel, "1", transaktion);
        transaktion.Commit();
    }

    // Nur Positionen, die es noch gibt: Eine umbenannte oder gelöschte Position der Vorgabe wird übersprungen
    // (sonst bricht der Fremdschlüssel den Start ab; Fehler in 0.33.0, behoben in 0.33.1).
    private void FunktionVorbelegen(string funktion, SqliteTransaction transaktion)
    {
        foreach (var p in RechteRegeln.Positionen)
            if (RechteRegeln.Vorbelegt(p, funktion))
                Ausfuehren("INSERT OR IGNORE INTO rechte (funktion, position) SELECT $f, name FROM positionen WHERE name = $p",
                    transaktion, ("$f", funktion), ("$p", p));
        if (RechteRegeln.NurFachkraftVorgabe.Contains(funktion))
            Ausfuehren("INSERT OR IGNORE INTO rechte_fachkraft (funktion) VALUES ($f)", transaktion, ("$f", funktion));
        SetzeMeta(NachtraeglichSchluessel(funktion), "1", transaktion);
    }

    private void GrundbestandEinfuegen(SqliteTransaction transaktion)
    {
        for (int i = 0; i < RechteRegeln.Positionen.Length; i++)
            Ausfuehren("INSERT OR IGNORE INTO positionen (name, nr) VALUES ($n, $i)", transaktion, ("$n", RechteRegeln.Positionen[i]), ("$i", i + 1));
        foreach (var p in RechteRegeln.Positionen)
            foreach (var (f, _) in Funktionen.Alle)
                if (RechteRegeln.Vorbelegt(p, f))
                    Ausfuehren("INSERT OR IGNORE INTO rechte (funktion, position) VALUES ($f, $p)", transaktion, ("$f", f), ("$p", p));
        foreach (var f in RechteRegeln.NurFachkraftVorgabe)
            Ausfuehren("INSERT OR IGNORE INTO rechte_fachkraft (funktion) VALUES ($f)", transaktion, ("$f", f));
        Ausfuehren("INSERT OR IGNORE INTO bereiche (name) SELECT DISTINCT team FROM team_mitglieder", transaktion);
        Ausfuehren("INSERT OR IGNORE INTO bereiche (name) SELECT DISTINCT wohnbereich FROM bewohner", transaktion);
        SetzeMeta(Grundbestand, "1", transaktion);
        foreach (var f in RechteRegeln.Nachtraeglich) SetzeMeta(NachtraeglichSchluessel(f), "1", transaktion);   // oben schon vorbelegt
        foreach (var f in RechteRegeln.NurFachkraftNachtraeglich) SetzeMeta(FachkraftSchluessel(f), "1", transaktion);
    }

    // Demo: Mitarbeiter, Bereiche, Positionen und Rechte wie neu (daten/demo/mitarbeiter.json).
    private void DemoMitarbeiterEinfuegen(SqliteTransaction transaktion)
    {
        var demo = Demodaten.Mitarbeiter();
        Ausfuehren("DELETE FROM kollegen; DELETE FROM rechte; DELETE FROM rechte_fachkraft; DELETE FROM positionen; DELETE FROM bereiche;", transaktion);
        GrundbestandEinfuegen(transaktion);
        SetzeMeta(AdminSchluessel, "1", transaktion);   // TST steht in den Demo-Daten
        for (int i = 0; i < demo.Bereiche.Count; i++)
            Ausfuehren("INSERT INTO bereiche (name, nr) VALUES ($n, $i) ON CONFLICT(name) DO UPDATE SET nr = excluded.nr", transaktion,
                ("$n", demo.Bereiche[i]), ("$i", i + 1));
        foreach (var m in demo.Mitarbeiter) MitarbeiterSchreiben(m, neu: true, transaktion);
        SetzeMeta(DemoMitarbeiterSchluessel, "1", transaktion);
    }
}
