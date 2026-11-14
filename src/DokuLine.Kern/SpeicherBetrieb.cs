using System.Globalization;
using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Betriebsmodus (Bauplan 37): Passwörter, Zugriffsprotokoll, Sicherung und Rücksicherung, Häuser.
public sealed partial class Speicher
{
    private const string LetzteSicherung = "letzte_sicherung";

    // ------------------------------------------------------------------ Passwörter

    public bool HatPasswort(string kuerzel) => Wert("SELECT 1 FROM passwoerter WHERE kuerzel = $k", ("$k", kuerzel.Trim())) is not null;

    // Anmelden mit Passwort: nach 5 Fehlversuchen gesperrt; ein Einmalpasswort muss danach geändert werden und gilt 7 Tage.
    // Unbekannte und ausgeschiedene Kürzel melden dasselbe wie ein falsches Passwort (verrät nicht, wer existiert).
    public (AnmeldeErgebnis Ergebnis, Mitarbeiter? Mitarbeiter) PasswortAnmelden(string kuerzel, string passwort)
    {
        var m = MitarbeiterLesen(kuerzel.Trim());
        if (m is null || !m.Aktiv) return (AnmeldeErgebnis.Falsch, null);
        string hash, salz, geaendert;
        int iterationen;
        bool einmal;
        using (var befehl = Befehl("SELECT hash, salz, iterationen, einmal, gesperrt, geaendert FROM passwoerter WHERE kuerzel = $k", null, [("$k", m.Kuerzel)]))
        using (var leser = befehl.ExecuteReader())
        {
            if (!leser.Read()) return (AnmeldeErgebnis.OhnePasswort, null);
            if (leser.GetInt64(4) == 1) return (AnmeldeErgebnis.Gesperrt, null);
            (hash, salz, iterationen, einmal, geaendert) = (leser.GetString(0), leser.GetString(1), leser.GetInt32(2), leser.GetInt64(3) == 1, leser.GetString(5));
        }
        bool richtig = PasswortRegeln.Pruefen(passwort, hash, salz, iterationen);
        // Zählen und Sperren in einem Schritt (mehrere Sitzungen gleichzeitig verlieren keinen Fehlversuch); eine
        // inzwischen gesetzte Sperre gilt auch für einen richtigen Versuch.
        using var transaktion = db.BeginTransaction();
        if (!richtig)
            Ausfuehren("""
                UPDATE passwoerter SET fehlversuche = fehlversuche + 1,
                    gesperrt = CASE WHEN fehlversuche + 1 >= $max THEN 1 ELSE gesperrt END WHERE kuerzel = $k
                """, transaktion, ("$max", PasswortRegeln.Fehlversuche), ("$k", m.Kuerzel));
        bool gesperrt = WertIn(transaktion, "SELECT gesperrt FROM passwoerter WHERE kuerzel = $k", ("$k", m.Kuerzel)) is long g && g == 1;
        if (richtig && !gesperrt) Ausfuehren("UPDATE passwoerter SET fehlversuche = 0 WHERE kuerzel = $k", transaktion, ("$k", m.Kuerzel));
        transaktion.Commit();
        if (gesperrt) return (AnmeldeErgebnis.Gesperrt, null);
        if (!richtig) return (AnmeldeErgebnis.Falsch, null);
        if (einmal && Uhr() - AusZeit(geaendert) > TimeSpan.FromDays(PasswortRegeln.EinmalTage)) return (AnmeldeErgebnis.Abgelaufen, null);
        return (einmal ? AnmeldeErgebnis.AendernPflicht : AnmeldeErgebnis.Ok, m);
    }

    // Einmalpasswort vergeben und entsperren: nur für Mitarbeiter, die man verwalten darf (eigener Bereich bzw. alle),
    // nie für jemanden mit mehr Rechten (sonst übernähme man dessen Konto) und für Administratoren nur als Administrator.
    private Mitarbeiter PasswortZielPruefen(string kuerzel, string durch)
    {
        var ich = MitarbeiterLesen(durch) ?? throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        var ziel = MitarbeiterLesen(kuerzel) ?? throw new Eingabefehler([$"Das Kürzel „{kuerzel}“ ist nicht bekannt."]);
        bool alle = Grund(durch, Funktionen.MitarbeiterAlle) is null, bereich = Grund(durch, Funktionen.MitarbeiterBereich) is null;
        if (!alle && !bereich) throw new Eingabefehler([Grund(durch, Funktionen.MitarbeiterBereich)!]);
        if (ziel.Kuerzel.Equals(ich.Kuerzel, StringComparison.OrdinalIgnoreCase))
            throw new Eingabefehler(["Das eigene Passwort ändern Sie unter „Passwort ändern“."]);
        if (RechteRegeln.IstAdministrator(ziel) && !RechteRegeln.IstAdministrator(ich))
            throw new Eingabefehler(["Das Passwort eines Administrators setzt nur ein Administrator zurück."]);
        if (!RechteRegeln.DarfVerwalten(ich, alle, bereich, ziel.Bereiche)) throw new Eingabefehler([$"{ziel.Name} gehört nicht zu Ihrem Bereich."]);
        if (!RechteRegeln.IstAdministrator(ich) && Funktionen.Alle.Any(f => Grund(ziel.Kuerzel, f.Schluessel) is null && Grund(durch, f.Schluessel) is not null))
            throw new Eingabefehler([$"{ziel.Name} hat Rechte, die Sie nicht haben – das Passwort setzt die Hausleitung oder der Verwalter zurück."]);
        return ziel;
    }

    // Einmalpasswort setzen: entsperrt auch; beim nächsten Anmelden muss es geändert werden.
    public void EinmalpasswortSetzen(string kuerzel, string passwort, string durch, DateTime jetzt)
    {
        var m = PasswortZielPruefen(kuerzel, durch);
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
        var m = PasswortZielPruefen(kuerzel, durch);
        Ausfuehren("UPDATE passwoerter SET gesperrt = 0, fehlversuche = 0 WHERE kuerzel = $k", null, ("$k", m.Kuerzel));
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

    public bool IstBetriebsdatenbank => Meta(Betrieb.MetaSchluessel) is not null;

    public void AlsBetriebMarkieren()
    {
        if (IstBetriebsdatenbank) return;
        using var transaktion = db.BeginTransaction();
        SetzeMeta(Betrieb.MetaSchluessel, "1", transaktion);
        transaktion.Commit();
    }

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

    // Was neben der Datenbank zu den Daten gehört: die Ablage der Dokumente und die hinterlegten Bilder.
    private string[] AblageOrdner => [Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Pfad))!, DokumentRegeln.Ordner), Pfade.ImDatenordner(Bilder.Ordner)];

    // Sicherung als eine Datei (ZIP): die Datenbank (Sicherungsfunktion von SQLite, auch bei laufendem Betrieb in sich
    // stimmig) und die Ablage der Dokumente und Bilder; danach geprüft. Nicht mehr nötige Stände werden gelöscht.
    public Sicherung SicherungErstellen(string ordner, DateTime jetzt)
    {
        Directory.CreateDirectory(ordner);
        string ziel = Path.Combine(ordner, SicherungRegeln.Name(jetzt));
        string zwischen = $"{ziel}.{Environment.ProcessId}.neu";
        string db_kopie = Path.Combine(Path.GetTempPath(), $"dokuline-sicherung-{Guid.NewGuid():N}.db");
        try
        {
            using (var datei = new SqliteConnection($"Data Source={db_kopie};Mode=ReadWriteCreate;Pooling=False"))
            {
                datei.Open();
                db.BackupDatabase(datei);
            }
            using (var zip = ZipFile.Open(zwischen, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(db_kopie, SicherungRegeln.DatenbankEintrag, CompressionLevel.Fastest);
                foreach (string quelle in AblageOrdner.Where(Directory.Exists))
                    foreach (string f in Directory.EnumerateFiles(quelle, "*", SearchOption.AllDirectories))
                        zip.CreateEntryFromFile(f, $"{Path.GetFileName(quelle)}/{Path.GetRelativePath(quelle, f).Replace('\\', '/')}",
                            CompressionLevel.NoCompression);
            }
            if (SicherungPruefen(zwischen) is string fehler) throw new SpeicherFehler($"Die Sicherung ist fehlerhaft und wurde verworfen: {fehler}");
            File.Move(zwischen, ziel, overwrite: true);
        }
        finally
        {
            foreach (string rest in new[] { zwischen, db_kopie })
                try { if (File.Exists(rest)) File.Delete(rest); }
                catch (IOException) { }
        }
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
        !Directory.Exists(ordner) ? [] : [.. Directory.EnumerateFiles(ordner, SicherungRegeln.Praefix + "*" + SicherungRegeln.Endung)
            .Select(p => (Pfad: p, Zeit: SicherungRegeln.ZeitAus(p)))
            .Where(x => x.Zeit is not null)
            .Select(x => new Sicherung(x.Pfad, x.Zeit!.Value, new FileInfo(x.Pfad).Length))
            .OrderByDescending(s => s.Zeit)];

    // null = in Ordnung; sonst der Grund. Prüft, ob die ZIP-Datei eine vollständige DokuLine-Datenbank enthält.
    public static string? SicherungPruefen(string pfad)
    {
        string ordner = Path.Combine(Path.GetTempPath(), $"dokuline-pruefen-{Guid.NewGuid():N}");
        try
        {
            string datei = DatenbankAuspacken(pfad, ordner);
            using var verbindung = new SqliteConnection($"Data Source={datei};Mode=ReadOnly;Pooling=False");
            verbindung.Open();
            using var pruefen = verbindung.CreateCommand();
            pruefen.CommandText = "PRAGMA integrity_check";
            if (pruefen.ExecuteScalar() as string is not "ok") return "Die Datenbank darin ist beschädigt.";
            pruefen.CommandText = "PRAGMA user_version";
            int stand = Convert.ToInt32(pruefen.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (stand < 1 || stand > AktuellerStand) return $"Unbekannter Stand der Datenbank ({stand}).";
            pruefen.CommandText = "SELECT COUNT(*) FROM bewohner";
            pruefen.ExecuteScalar();
            return null;
        }
        catch (Exception e) when (e is SqliteException or InvalidDataException or IOException) { return e.Message; }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(ordner)) Directory.Delete(ordner, recursive: true); }
            catch (IOException) { }
        }
    }

    private static string DatenbankAuspacken(string zip, string ordner)
    {
        using var archiv = ZipFile.OpenRead(zip);
        var eintrag = archiv.GetEntry(SicherungRegeln.DatenbankEintrag) ?? throw new InvalidDataException("Die Datei enthält keine Datenbank.");
        Directory.CreateDirectory(ordner);
        string ziel = Path.Combine(ordner, SicherungRegeln.DatenbankEintrag);
        eintrag.ExtractToFile(ziel, overwrite: true);
        return ziel;
    }

    // Rücksicherung: erst eine Kopie des jetzigen Stands (Datenbank, Ablage), dann die geprüfte Sicherung zurückspielen.
    public string Ruecksichern(string sicherung, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.BetriebVerwalten) is string grund) throw new Eingabefehler([grund]);
        return Ruecksichern(sicherung);
    }

    // Ohne Rechteprüfung: nur für „--ruecksichern“, das ein Windows-Administrator mit erhöhten Rechten startet.
    public string Ruecksichern(string sicherung)
    {
        if (SicherungPruefen(sicherung) is string fehler) throw new Eingabefehler([$"Diese Sicherung lässt sich nicht verwenden: {fehler}"]);
        string vorher = SicherungAnlegen("vor-ruecksicherung");
        // Neben der Datenbank auspacken, nicht im Temp-Ordner: Ordner lassen sich unter Windows nur auf demselben Laufwerk verschieben.
        string ordner = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Pfad))!, $"ruecksicherung-{Guid.NewGuid():N}");
        try
        {
            ZipFile.ExtractToDirectory(sicherung, ordner);   // prüft, dass kein Eintrag aus dem Ordner herausführt
            // Erst die Datenbank: Scheitert sie, ist noch nichts verändert.
            using (var quelle = new SqliteConnection($"Data Source={Path.Combine(ordner, SicherungRegeln.DatenbankEintrag)};Mode=ReadOnly;Pooling=False"))
            {
                quelle.Open();
                quelle.BackupDatabase(db);
            }
            string stempel = Uhr().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            foreach (string ablage in AblageOrdner)
            {
                if (Directory.Exists(ablage)) Directory.Move(ablage, $"{ablage}-vor-ruecksicherung-{stempel}");
                string aus = Path.Combine(ordner, Path.GetFileName(ablage));
                if (Directory.Exists(aus)) Directory.Move(aus, ablage);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(ordner)) Directory.Delete(ordner, recursive: true); }
            catch (IOException) { }
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

    // Die Wohnbereiche, die jemand in der Seitenleiste wählen kann (eigenes Haus; andere Häuser nur mit Recht).
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
