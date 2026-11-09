using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Betriebsmodus (Bauplan 37): Passwörter, Zugriffsprotokoll, Sicherung und Rücksicherung, Häuser.
public sealed partial class Speicher
{
    private const string LetzteSicherung = "letzte_sicherung";

    // ------------------------------------------------------------------ Passwörter

    public bool HatPasswort(string kuerzel) => Wert("SELECT 1 FROM passwoerter WHERE kuerzel = $k", ("$k", kuerzel.Trim())) is not null;

    // Anmelden mit Passwort: nach 5 Fehlversuchen gesperrt; ein Einmalpasswort muss danach geändert werden.
    public (AnmeldeErgebnis Ergebnis, Mitarbeiter? Mitarbeiter) PasswortAnmelden(string kuerzel, string passwort)
    {
        var m = Anmelden(kuerzel);   // unbekannt oder ausgeschieden: Eingabefehler wie bisher
        using var befehl = Befehl("SELECT hash, salz, iterationen, einmal, fehlversuche, gesperrt FROM passwoerter WHERE kuerzel = $k", null, [("$k", m.Kuerzel)]);
        using var leser = befehl.ExecuteReader();
        if (!leser.Read()) return (AnmeldeErgebnis.OhnePasswort, null);
        string hash = leser.GetString(0), salz = leser.GetString(1);
        int iterationen = leser.GetInt32(2), fehl = leser.GetInt32(4);
        bool einmal = leser.GetInt64(3) == 1, gesperrt = leser.GetInt64(5) == 1;
        leser.Close();
        if (gesperrt) return (AnmeldeErgebnis.Gesperrt, null);
        if (!PasswortRegeln.Pruefen(passwort, hash, salz, iterationen))
        {
            fehl++;
            Ausfuehren("UPDATE passwoerter SET fehlversuche = $f, gesperrt = $g WHERE kuerzel = $k", null,
                ("$f", fehl), ("$g", fehl >= PasswortRegeln.Fehlversuche ? 1 : 0), ("$k", m.Kuerzel));
            return (fehl >= PasswortRegeln.Fehlversuche ? AnmeldeErgebnis.Gesperrt : AnmeldeErgebnis.Falsch, null);
        }
        Ausfuehren("UPDATE passwoerter SET fehlversuche = 0 WHERE kuerzel = $k", null, ("$k", m.Kuerzel));
        return (einmal ? AnmeldeErgebnis.AendernPflicht : AnmeldeErgebnis.Ok, m);
    }

    // Einmalpasswort setzen (Leitung oder Verwalter): entsperrt auch; beim nächsten Anmelden muss es geändert werden.
    public void EinmalpasswortSetzen(string kuerzel, string passwort, string durch, DateTime jetzt)
    {
        if (Grund(durch, Funktionen.MitarbeiterBereich) is not null && Grund(durch, Funktionen.MitarbeiterAlle) is string grund) throw new Eingabefehler([grund]);
        var m = MitarbeiterLesen(kuerzel) ?? throw new Eingabefehler([$"Das Kürzel „{kuerzel}“ ist nicht bekannt."]);
        PasswortSchreiben(m.Kuerzel, passwort, einmal: true, durch, jetzt);
    }

    // Eigenes Passwort ändern: das bisherige ist nötig (auch das Einmalpasswort).
    public void PasswortAendern(string kuerzel, string alt, string neu, DateTime jetzt)
    {
        var (ergebnis, m) = PasswortAnmelden(kuerzel, alt);
        if (ergebnis is not (AnmeldeErgebnis.Ok or AnmeldeErgebnis.AendernPflicht)) throw new Eingabefehler(["Das bisherige Passwort stimmt nicht."]);
        if (PasswortRegeln.Schwach(neu, m!.Kuerzel) is string schwach) throw new Eingabefehler([schwach]);
        if (alt == neu) throw new Eingabefehler(["Bitte ein neues Passwort wählen."]);
        PasswortSchreiben(m.Kuerzel, neu, einmal: false, m.Kuerzel, jetzt);
    }

    public void Entsperren(string kuerzel, string durch)
    {
        if (Grund(durch, Funktionen.MitarbeiterBereich) is not null && Grund(durch, Funktionen.MitarbeiterAlle) is string grund) throw new Eingabefehler([grund]);
        Ausfuehren("UPDATE passwoerter SET gesperrt = 0, fehlversuche = 0 WHERE kuerzel = $k", null, ("$k", kuerzel.Trim()));
    }

    // Erster Start im Betrieb (noch niemand angelegt): der Verwalter mit Position „Administrator“, allen Bereichen und Passwort.
    public Mitarbeiter VerwalterAnlegen(string kuerzel, string vorname, string nachname, string passwort, DateTime jetzt)
    {
        if (AlleMitarbeiter().Count > 0) throw new Eingabefehler(["Es gibt schon Mitarbeiter – neue legt der Verwalter in den Einstellungen an."]);
        if (kuerzel.Trim().Length == 0) throw new Eingabefehler(["Bitte ein Kürzel eingeben."]);
        if (Einstellungen.KuerzelPruefen(kuerzel) is string falsch) throw new Eingabefehler([falsch]);
        if (vorname.Trim().Length == 0 || nachname.Trim().Length == 0) throw new Eingabefehler(["Bitte Vor- und Nachnamen eingeben."]);
        if (PasswortRegeln.Schwach(passwort, kuerzel) is string schwach) throw new Eingabefehler([schwach]);
        var m = new Mitarbeiter(kuerzel.Trim().ToUpperInvariant(), vorname.Trim(), nachname.Trim(), true, true, [.. Bereichsliste().Select(b => b.Name)],
            [RechteRegeln.Administrator]);
        using (var transaktion = db.BeginTransaction())
        {
            MitarbeiterSchreiben(m, neu: true, transaktion);
            transaktion.Commit();
        }
        PasswortSchreiben(m.Kuerzel, passwort, einmal: false, m.Kuerzel, jetzt);
        return m;
    }

    public void WalEinschalten() => Ausfuehren("PRAGMA journal_mode = WAL");

    public bool Gesperrt(string kuerzel) => Wert("SELECT gesperrt FROM passwoerter WHERE kuerzel = $k", ("$k", kuerzel.Trim())) is long g && g == 1;

    private void PasswortSchreiben(string kuerzel, string passwort, bool einmal, string durch, DateTime jetzt)
    {
        if (!einmal && PasswortRegeln.Schwach(passwort, kuerzel) is string schwach) throw new Eingabefehler([schwach]);
        if (einmal && passwort.Length < 8) throw new Eingabefehler(["Das Einmalpasswort braucht mindestens 8 Zeichen."]);
        var (hash, salz) = PasswortRegeln.Hashen(passwort);
        Ausfuehren("""
            INSERT INTO passwoerter (kuerzel, hash, salz, iterationen, einmal, fehlversuche, gesperrt, geaendert, geaendert_von)
            VALUES ($k, $h, $s, $i, $e, 0, 0, $z, $d)
            ON CONFLICT(kuerzel) DO UPDATE SET hash = excluded.hash, salz = excluded.salz, iterationen = excluded.iterationen, einmal = excluded.einmal,
                fehlversuche = 0, gesperrt = 0, geaendert = excluded.geaendert, geaendert_von = excluded.geaendert_von
            """, null, ("$k", kuerzel), ("$h", hash), ("$s", salz), ("$i", PasswortRegeln.Iterationen), ("$e", einmal ? 1 : 0), ("$z", Zeit(jetzt)), ("$d", durch.Trim()));
    }

    // ------------------------------------------------------------------ Zugriffsprotokoll

    // Wer wann welche Akte geöffnet, gedruckt oder geändert hat. Nie Inhalte, nur was (z. B. „Reiter Vitalwerte“).
    public void Zugriff(string kuerzel, long? bewohnerId, string aktion, string was = "")
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) return;
        Ausfuehren("INSERT INTO zugriffe (zeit, kuerzel, bewohner_id, aktion, was) VALUES ($z, $k, $b, $a, $w)", null,
            ("$z", Zeit(Uhr())), ("$k", kuerzel.Trim()), ("$b", bewohnerId), ("$a", aktion), ("$w", was));
    }

    public List<Zugriff> Zugriffe(string kuerzel, DateOnly von, DateOnly bis, long? bewohnerId = null, string? wer = null)
    {
        if (Grund(kuerzel, Funktionen.Zugriffsprotokoll) is string grund) throw new Eingabefehler([grund]);
        var liste = new List<Zugriff>();
        using var befehl = Befehl("""
            SELECT id, zeit, kuerzel, bewohner_id, aktion, was FROM zugriffe
            WHERE zeit >= $von AND zeit < $bis AND ($b IS NULL OR bewohner_id = $b) AND ($w IS NULL OR kuerzel = $w COLLATE NOCASE)
            ORDER BY zeit DESC, id DESC
            """, null, [("$von", Iso(von)), ("$bis", Iso(bis.AddDays(1))), ("$b", bewohnerId), ("$w", wer)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Zugriff(leser.GetInt64(0), AusZeit(leser.GetString(1)), leser.GetString(2), leser.IsDBNull(3) ? null : leser.GetInt64(3),
                leser.GetString(4), leser.GetString(5)));
        return liste;
    }

    // Aufbewahrung: Älteres löschen (beim Start, im Betrieb nach betrieb.json).
    public int ZugriffeAufraeumen(DateTime jetzt, int monate)
    {
        Ausfuehren("DELETE FROM zugriffe WHERE zeit < $grenze", null, ("$grenze", Zeit(jetzt.AddMonths(-monate))));
        return Convert.ToInt32(Wert("SELECT changes()"), CultureInfo.InvariantCulture);
    }

    // ------------------------------------------------------------------ Sicherung

    public DateTime? LetzteSicherungZeit => Meta(LetzteSicherung) is string s ? AusZeit(s) : null;

    // Sicherung mit der Sicherungsfunktion von SQLite (auch bei laufendem Betrieb in sich stimmig), danach geprüft.
    public Sicherung SicherungErstellen(string ordner, DateTime jetzt)
    {
        Directory.CreateDirectory(ordner);
        string ziel = Path.Combine(ordner, SicherungRegeln.Name(jetzt));
        string zwischen = ziel + ".neu";
        if (File.Exists(zwischen)) File.Delete(zwischen);
        using (var datei = new SqliteConnection($"Data Source={zwischen};Mode=ReadWriteCreate;Pooling=False"))
        {
            datei.Open();
            db.BackupDatabase(datei);
        }
        if (SicherungPruefen(zwischen) is string fehler)
        {
            File.Delete(zwischen);
            throw new SpeicherFehler($"Die Sicherung ist fehlerhaft und wurde verworfen: {fehler}");
        }
        File.Move(zwischen, ziel, overwrite: true);
        using (var transaktion = db.BeginTransaction())
        {
            SetzeMeta(LetzteSicherung, Zeit(jetzt), transaktion);
            transaktion.Commit();
        }
        foreach (var weg in SicherungRegeln.Ueberzaehlig(Sicherungen(ordner)))
            try { File.Delete(weg.Pfad); }
            catch (IOException) { }   // wird beim nächsten Mal gelöscht
        return Sicherungen(ordner).First(s => s.Pfad == ziel);
    }

    public static List<Sicherung> Sicherungen(string ordner) =>
        !Directory.Exists(ordner) ? [] : [.. Directory.EnumerateFiles(ordner, SicherungRegeln.Praefix + "*.db")
            .Select(p => (Pfad: p, Zeit: SicherungRegeln.ZeitAus(p)))
            .Where(x => x.Zeit is not null)
            .Select(x => new Sicherung(x.Pfad, x.Zeit!.Value, new FileInfo(x.Pfad).Length))
            .OrderByDescending(s => s.Zeit)];

    // null = in Ordnung; sonst der Grund. Prüft, ob die Datei eine vollständige DokuLine-Datenbank ist.
    public static string? SicherungPruefen(string pfad)
    {
        try
        {
            using var datei = new SqliteConnection($"Data Source={pfad};Mode=ReadOnly;Pooling=False");
            datei.Open();
            using var pruefen = datei.CreateCommand();
            pruefen.CommandText = "PRAGMA integrity_check";
            if (pruefen.ExecuteScalar() as string is not "ok") return "Die Datei ist beschädigt.";
            pruefen.CommandText = "PRAGMA user_version";
            int stand = Convert.ToInt32(pruefen.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (stand < 1 || stand > AktuellerStand) return $"Unbekannter Stand der Datenbank ({stand}).";
            pruefen.CommandText = "SELECT COUNT(*) FROM bewohner";
            pruefen.ExecuteScalar();
            return null;
        }
        catch (SqliteException e) { return e.Message; }
    }

    // Rücksicherung: erst eine Kopie des jetzigen Stands, dann die geprüfte Sicherung zurückspielen.
    public string Ruecksichern(string sicherung, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.BetriebVerwalten) is string grund) throw new Eingabefehler([grund]);
        if (SicherungPruefen(sicherung) is string fehler) throw new Eingabefehler([$"Diese Sicherung lässt sich nicht verwenden: {fehler}"]);
        string vorher = SicherungAnlegen("vor-ruecksicherung");
        using (var quelle = new SqliteConnection($"Data Source={sicherung};Mode=ReadOnly;Pooling=False"))
        {
            quelle.Open();
            quelle.BackupDatabase(db);
        }
        Ausfuehren("PRAGMA foreign_keys = ON");
        Aufbauen();   // eine ältere Sicherung bekommt die neueren Datenbank-Schritte
        return vorher;
    }

    // ------------------------------------------------------------------ Häuser

    public List<string> Haeuser()
    {
        var liste = new List<string>();
        using var befehl = Befehl("SELECT name FROM haeuser ORDER BY nr, name", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(leser.GetString(0));
        return liste;
    }

    public Dictionary<string, string> HausVonBereich()
    {
        var d = new Dictionary<string, string>();
        using var befehl = Befehl("SELECT bereich, haus FROM bereich_haeuser", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) d[leser.GetString(0)] = leser.GetString(1);
        return d;
    }

    public void HausAnlegen(string name, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.BetriebVerwalten) is string grund) throw new Eingabefehler([grund]);
        name = name.Trim();
        if (name.Length == 0) throw new Eingabefehler(["Bitte einen Namen für das Haus eingeben."]);
        if (Haeuser().Contains(name, StringComparer.OrdinalIgnoreCase)) throw new Eingabefehler([$"„{name}“ gibt es schon."]);
        Ausfuehren("INSERT INTO haeuser (name, nr) VALUES ($n, (SELECT COALESCE(MAX(nr), 0) + 1 FROM haeuser))", null, ("$n", name));
    }

    public void HausLoeschen(string name, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.BetriebVerwalten) is string grund) throw new Eingabefehler([grund]);
        Ausfuehren("DELETE FROM haeuser WHERE name = $n", null, ("$n", name));
    }

    // haus null: der Bereich gehört zu keinem Haus.
    public void BereichHausSetzen(string bereich, string? haus, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.BetriebVerwalten) is string grund) throw new Eingabefehler([grund]);
        if (haus is null) Ausfuehren("DELETE FROM bereich_haeuser WHERE bereich = $b", null, ("$b", bereich));
        else Ausfuehren("INSERT INTO bereich_haeuser (bereich, haus) VALUES ($b, $h) ON CONFLICT(bereich) DO UPDATE SET haus = excluded.haus", null,
            ("$b", bereich), ("$h", haus));
    }

    // Die Wohnbereiche, die jemand in der Übersicht wählen kann (eigenes Haus; andere Häuser nur mit Recht).
    public List<string> SichtbareWohnbereiche(string kuerzel)
    {
        var alle = Wohnbereiche().Select(w => w.Name).ToList();
        var m = MitarbeiterLesen(kuerzel);
        bool andere = m is null || Grund(kuerzel, Funktionen.AndereHaeuser) is null;
        return HaeuserRegeln.Sichtbar(alle, HausVonBereich(), m?.Bereiche ?? [], andere);
    }

    // Haus-Termine je Haus: welches Haus ein Termin hat (ohne Eintrag: für alle).
    public Dictionary<long, string> KalenderHaeuser()
    {
        var d = new Dictionary<long, string>();
        using var befehl = Befehl("SELECT termin_id, haus FROM kalender_haeuser", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) d[leser.GetInt64(0)] = leser.GetString(1);
        return d;
    }

    public void KalenderHausSetzen(long terminId, string? haus)
    {
        if (haus is null) Ausfuehren("DELETE FROM kalender_haeuser WHERE termin_id = $t", null, ("$t", terminId));
        else Ausfuehren("INSERT INTO kalender_haeuser (termin_id, haus) VALUES ($t, $h) ON CONFLICT(termin_id) DO UPDATE SET haus = excluded.haus", null,
            ("$t", terminId), ("$h", haus));
    }
}
