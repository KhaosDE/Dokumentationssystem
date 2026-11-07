using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Der Speicher ist eine SQLite-Datei im Datenordner.
//
// Der Aufbau der Datenbank wächst mit dem Programm. Jede Erweiterung ist ein Schritt in
// Schritte; die Datei merkt sich in „PRAGMA user_version“, wie viele Schritte sie schon hat.
// Beim Öffnen werden fehlende Schritte nachgeholt – jeder ganz oder gar nicht.
//
// Die Python-Version (bis 0.2.0) hatte eine eigene Datei (dokusystem.sqlite3). Sie wird nicht
// gelöscht und nicht gelesen; die C#-Version beginnt mit den Demo-Daten.
public sealed partial class Speicher : IDisposable
{
    public const string Datei = "dokumentationssystem.db";
    private const string DemoGeladen = "demo_geladen";
    private const string DemoBerichte = "demo_berichte";
    private const string DemoAufgaben = "demo_aufgaben";
    private const string DemoNachrichten = "demo_nachrichten";
    private const string DemoMedikamente = "demo_medikamente";
    private const string DemoMedikation = "demo_medikation";
    private const string DemoTherapie = "demo_therapie";
    private const string DemoWochenplan = "demo_wochenplan";
    private const string DemoKalender = "demo_kalender";
    private const string DemoBiografie = "demo_biografie";

    // Die Uhr des Speichers (für Demo-Daten relativ zu „jetzt“); zum Testen ersetzbar.
    public Func<DateTime> Uhr { get; set; } = () => DateTime.Now;

    private static readonly string[] Schritte =
    [
        // 1 – Verwaltungsangaben und Bewohnerakte. Zustände mit CHECK, damit nie ein leerer
        //     Wert wie „keine“ aussieht (Regel 5).
        """
        CREATE TABLE meta (
            schluessel TEXT PRIMARY KEY,
            wert       TEXT NOT NULL
        );
        CREATE TABLE bewohner (
            id            INTEGER PRIMARY KEY,
            vorname       TEXT NOT NULL,
            nachname      TEXT NOT NULL,
            zimmer        TEXT NOT NULL,
            geburtsdatum  TEXT NOT NULL,              -- JJJJ-MM-TT
            aufnahmedatum TEXT NOT NULL,              -- JJJJ-MM-TT
            allergie      TEXT NOT NULL DEFAULT 'nicht_erfasst'
                          CHECK (allergie IN ('eingetragen', 'keine_bekannt', 'nicht_erfasst')),
            allergien     TEXT NOT NULL DEFAULT '',
            beschluss     TEXT NOT NULL DEFAULT 'nicht_erfasst'
                          CHECK (beschluss IN ('ende', 'kein_beschluss', 'nicht_erfasst')),
            beschlussende TEXT,                       -- JJJJ-MM-TT, nur bei beschluss = 'ende'
            bild          TEXT                        -- Dateiname im Ordner „bilder“
        );
        """,
        // 2 – Tägliche Dokumentation. Berichte und Nachträge werden nie geändert (Trigger);
        //     Korrekturen sind neue Nachträge. Gelöscht wird nur mit „Demo-Daten zurücksetzen“.
        """
        CREATE TABLE berichte (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            zeitpunkt   TEXT NOT NULL,                -- JJJJ-MM-TTTSS:MM
            schicht     TEXT NOT NULL CHECK (schicht IN ('frueh', 'spaet', 'nacht')),
            verfasser   TEXT NOT NULL,
            bereiche    TEXT NOT NULL DEFAULT '',     -- z. B. '1,4'; leer = allgemein
            text        TEXT NOT NULL,
            wichtig     INTEGER NOT NULL DEFAULT 0,
            erfasst     TEXT NOT NULL                 -- wann eingetragen
        );
        CREATE INDEX berichte_bewohner ON berichte(bewohner_id, zeitpunkt);
        CREATE TABLE nachtraege (
            id         INTEGER PRIMARY KEY,
            bericht_id INTEGER NOT NULL REFERENCES berichte(id) ON DELETE CASCADE,
            erfasst    TEXT NOT NULL,
            verfasser  TEXT NOT NULL,
            text       TEXT NOT NULL
        );
        CREATE TRIGGER berichte_bleiben BEFORE UPDATE ON berichte
            BEGIN SELECT RAISE(ABORT, 'Berichte werden nicht überschrieben – Korrekturen sind Nachträge.'); END;
        CREATE TRIGGER nachtraege_bleiben BEFORE UPDATE ON nachtraege
            BEGIN SELECT RAISE(ABORT, 'Nachträge werden nicht überschrieben.'); END;
        -- Übergabe: Einträge bleiben, bis sie ablaufen oder erledigt sind. Verlängern und
        -- „erledigt“ sind eigene Ereignisse; nichts wird überschrieben.
        CREATE TABLE uebergaben (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER REFERENCES bewohner(id) ON DELETE CASCADE,   -- NULL = Wohnbereich allgemein
            bericht_id  INTEGER REFERENCES berichte(id) ON DELETE CASCADE,   -- NULL = Notiz ohne Bericht
            text        TEXT NOT NULL DEFAULT '',
            erstellt    TEXT NOT NULL,
            verfasser   TEXT NOT NULL,
            gilt_bis    TEXT                                                -- NULL = bis erledigt
        );
        CREATE TABLE uebergabe_ereignisse (
            id           INTEGER PRIMARY KEY,
            uebergabe_id INTEGER NOT NULL REFERENCES uebergaben(id) ON DELETE CASCADE,
            art          TEXT NOT NULL CHECK (art IN ('verlaengert', 'erledigt')),
            zeit         TEXT NOT NULL,
            kuerzel      TEXT NOT NULL,
            gilt_bis     TEXT
        );
        CREATE TABLE uebergabe_gelesen (
            uebergabe_id INTEGER NOT NULL REFERENCES uebergaben(id) ON DELETE CASCADE,
            kuerzel      TEXT NOT NULL,
            zeit         TEXT NOT NULL,
            PRIMARY KEY (uebergabe_id, kuerzel)
        );
        CREATE TRIGGER uebergaben_bleiben BEFORE UPDATE ON uebergaben
            BEGIN SELECT RAISE(ABORT, 'Übergaben werden nicht überschrieben – Änderungen sind Ereignisse.'); END;
        CREATE TRIGGER uebergabe_ereignisse_bleiben BEFORE UPDATE ON uebergabe_ereignisse
            BEGIN SELECT RAISE(ABORT, 'Ereignisse werden nicht überschrieben.'); END;
        """,
        // 3 – Wohnbereich-ToDo: Aufgaben bleiben, bis sie erledigt sind. Weitergeben und
        //     „erledigt“ sind eigene Ereignisse; nichts wird überschrieben.
        """
        CREATE TABLE aufgaben (
            id           INTEGER PRIMARY KEY,
            bewohner_id  INTEGER REFERENCES bewohner(id) ON DELETE CASCADE,   -- NULL = kein einzelner Bewohner
            bericht_id   INTEGER REFERENCES berichte(id) ON DELETE CASCADE,
            text         TEXT NOT NULL,
            fuer         TEXT NOT NULL CHECK (fuer IN ('naechste_schicht', 'schicht', 'wer_zeit_hat')),
            schicht_tag  TEXT,
            schicht      TEXT CHECK (schicht IS NULL OR schicht IN ('frueh', 'spaet', 'nacht')),
            faellig_bis  TEXT,
            erstellt     TEXT NOT NULL,
            verfasser    TEXT NOT NULL
        );
        CREATE TABLE aufgabe_ereignisse (
            id         INTEGER PRIMARY KEY,
            aufgabe_id INTEGER NOT NULL REFERENCES aufgaben(id) ON DELETE CASCADE,
            art        TEXT NOT NULL CHECK (art IN ('weitergegeben', 'erledigt')),
            zeit       TEXT NOT NULL,
            kuerzel    TEXT NOT NULL,
            notiz      TEXT NOT NULL DEFAULT ''
        );
        CREATE TRIGGER aufgaben_bleiben BEFORE UPDATE ON aufgaben
            BEGIN SELECT RAISE(ABORT, 'Aufgaben werden nicht überschrieben – Änderungen sind Ereignisse.'); END;
        CREATE TRIGGER aufgabe_ereignisse_bleiben BEFORE UPDATE ON aufgabe_ereignisse
            BEGIN SELECT RAISE(ABORT, 'Ereignisse werden nicht überschrieben.'); END;
        """,
        // 4 – Nachrichten: Kollegen und Teams (Demo), Nachrichten mit Empfängern (Personen und
        //     Teams), „gelesen“ und „archiviert“ je Person. Nachrichten werden nicht geändert.
        """
        CREATE TABLE kollegen (
            kuerzel  TEXT PRIMARY KEY,
            vorname  TEXT NOT NULL,
            nachname TEXT NOT NULL
        );
        CREATE TABLE team_mitglieder (
            team    TEXT NOT NULL,
            kuerzel TEXT NOT NULL REFERENCES kollegen(kuerzel) ON DELETE CASCADE,
            PRIMARY KEY (team, kuerzel)
        );
        CREATE TABLE nachrichten (
            id          INTEGER PRIMARY KEY,
            verlauf_id  INTEGER REFERENCES nachrichten(id) ON DELETE CASCADE,   -- NULL = erste Nachricht
            von         TEXT NOT NULL,
            betreff     TEXT NOT NULL,
            text        TEXT NOT NULL,
            gesendet    TEXT NOT NULL,
            bewohner_id INTEGER REFERENCES bewohner(id) ON DELETE CASCADE
        );
        CREATE TABLE nachricht_empfaenger (
            nachricht_id INTEGER NOT NULL REFERENCES nachrichten(id) ON DELETE CASCADE,
            art          TEXT NOT NULL CHECK (art IN ('person', 'team')),
            ziel         TEXT NOT NULL,
            PRIMARY KEY (nachricht_id, art, ziel)
        );
        CREATE TABLE nachricht_gelesen (
            nachricht_id INTEGER NOT NULL REFERENCES nachrichten(id) ON DELETE CASCADE,
            kuerzel      TEXT NOT NULL,
            zeit         TEXT NOT NULL,
            PRIMARY KEY (nachricht_id, kuerzel)
        );
        CREATE TABLE verlauf_archiv (
            verlauf_id INTEGER NOT NULL REFERENCES nachrichten(id) ON DELETE CASCADE,
            kuerzel    TEXT NOT NULL,
            zeit       TEXT NOT NULL,
            PRIMARY KEY (verlauf_id, kuerzel)
        );
        CREATE TRIGGER nachrichten_bleiben BEFORE UPDATE ON nachrichten
            BEGIN SELECT RAISE(ABORT, 'Nachrichten werden nicht überschrieben.'); END;
        """,
        // 5 – Medikamentendatenbank: je Medikament Fassungen (älteste zuerst); jede Änderung ist
        //     eine neue Fassung mit Datum und Kürzel. Listen (Handelsnamen, Stärken) als JSON.
        """
        CREATE TABLE medikamente (
            id              INTEGER PRIMARY KEY,
            selbst_angelegt INTEGER NOT NULL DEFAULT 0 CHECK (selbst_angelegt IN (0, 1))
        );
        CREATE TABLE medikament_fassungen (
            id               INTEGER PRIMARY KEY,
            medikament_id    INTEGER NOT NULL REFERENCES medikamente(id) ON DELETE CASCADE,
            wirkstoff        TEXT NOT NULL,
            handelsnamen     TEXT NOT NULL DEFAULT '[]',
            staerken         TEXT NOT NULL DEFAULT '[]',
            darreichungsform TEXT NOT NULL DEFAULT '',
            quelle           TEXT NOT NULL DEFAULT '',
            pruefstatus      TEXT NOT NULL CHECK (pruefstatus IN ('ungeprueft', 'bestaetigt')),
            geaendert        TEXT NOT NULL,
            kuerzel          TEXT NOT NULL
        );
        CREATE TRIGGER medikamente_bleiben BEFORE UPDATE ON medikamente
            BEGIN SELECT RAISE(ABORT, 'Medikamente werden nicht überschrieben – Änderungen sind neue Fassungen.'); END;
        CREATE TRIGGER medikament_fassungen_bleiben BEFORE UPDATE ON medikament_fassungen
            BEGIN SELECT RAISE(ABORT, 'Fassungen werden nicht überschrieben.'); END;
        """,
        // 6 – Medikation: Grundverordnung je Medikament und Bewohner; Pausen, Stufenpläne und
        //     Absetzen als eigene Einträge darüber. Nichts wird überschrieben.
        """
        CREATE TABLE verordnungen (
            id                INTEGER PRIMARY KEY,
            bewohner_id       INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            medikament_id     INTEGER NOT NULL REFERENCES medikamente(id) ON DELETE CASCADE,
            staerke           TEXT NOT NULL DEFAULT '',
            einheit           TEXT NOT NULL DEFAULT 'Stück',
            morgens           TEXT NOT NULL,
            mittags           TEXT NOT NULL,
            abends            TEXT NOT NULL,
            nachts            TEXT NOT NULL,
            bedarf            INTEGER NOT NULL DEFAULT 0 CHECK (bedarf IN (0, 1)),
            bedarf_anlass     TEXT NOT NULL DEFAULT '',
            bedarf_hoechstens TEXT NOT NULL DEFAULT '',
            beginn            TEXT NOT NULL,
            arzt              TEXT NOT NULL,
            angeordnet_am     TEXT NOT NULL,
            hinweis           TEXT NOT NULL DEFAULT '',
            erstellt          TEXT NOT NULL,
            kuerzel           TEXT NOT NULL
        );
        CREATE TABLE verordnung_aenderungen (
            id            INTEGER PRIMARY KEY,
            verordnung_id INTEGER NOT NULL REFERENCES verordnungen(id) ON DELETE CASCADE,
            art           TEXT NOT NULL CHECK (art IN ('pause', 'pause_ende', 'stufenplan', 'absetzen')),
            von           TEXT NOT NULL,
            bis           TEXT,                                   -- Pause: NULL = bis auf Weiteres
            arzt          TEXT NOT NULL,
            angeordnet_am TEXT NOT NULL,
            hinweis       TEXT NOT NULL DEFAULT '',
            erstellt      TEXT NOT NULL,
            kuerzel       TEXT NOT NULL
        );
        CREATE TABLE stufen (
            aenderung_id INTEGER NOT NULL REFERENCES verordnung_aenderungen(id) ON DELETE CASCADE,
            nr           INTEGER NOT NULL,
            ab           TEXT NOT NULL,
            morgens      TEXT NOT NULL,
            mittags      TEXT NOT NULL,
            abends       TEXT NOT NULL,
            nachts       TEXT NOT NULL,
            PRIMARY KEY (aenderung_id, nr)
        );
        CREATE TRIGGER verordnungen_bleiben BEFORE UPDATE ON verordnungen
            BEGIN SELECT RAISE(ABORT, 'Verordnungen werden nicht überschrieben – Änderungen sind eigene Einträge.'); END;
        CREATE TRIGGER verordnung_aenderungen_bleiben BEFORE UPDATE ON verordnung_aenderungen
            BEGIN SELECT RAISE(ABORT, 'Änderungen werden nicht überschrieben.'); END;
        CREATE TRIGGER stufen_bleiben BEFORE UPDATE ON stufen
            BEGIN SELECT RAISE(ABORT, 'Stufen werden nicht überschrieben.'); END;
        """,
        // 7 – Therapie: Angebote mit Fassungen („gilt ab“), Zuteilungen von Bewohnern und
        //     „beendet ab“ als eigener Eintrag. Termine je Fassung als JSON (Wochentag, von, bis).
        """
        CREATE TABLE angebote (
            id INTEGER PRIMARY KEY
        );
        CREATE TABLE angebot_fassungen (
            id         INTEGER PRIMARY KEY,
            angebot_id INTEGER NOT NULL REFERENCES angebote(id) ON DELETE CASCADE,
            gilt_ab    TEXT NOT NULL,
            name       TEXT NOT NULL,
            art        TEXT NOT NULL CHECK (art IN ('gruppe', 'einzel')),
            therapeut  TEXT NOT NULL,
            ort        TEXT NOT NULL DEFAULT '',
            termine    TEXT NOT NULL,
            hoechstens INTEGER NOT NULL CHECK (hoechstens BETWEEN 1 AND 50),
            hinweis    TEXT NOT NULL DEFAULT '',
            erstellt   TEXT NOT NULL,
            kuerzel    TEXT NOT NULL
        );
        CREATE TABLE zuteilungen (
            id          INTEGER PRIMARY KEY,
            angebot_id  INTEGER NOT NULL REFERENCES angebote(id) ON DELETE CASCADE,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            ab          TEXT NOT NULL,
            bis         TEXT,
            erstellt    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TABLE zuteilung_ende (
            zuteilung_id INTEGER PRIMARY KEY REFERENCES zuteilungen(id) ON DELETE CASCADE,
            ab           TEXT NOT NULL,
            erstellt     TEXT NOT NULL,
            kuerzel      TEXT NOT NULL
        );
        CREATE TRIGGER angebot_fassungen_bleiben BEFORE UPDATE ON angebot_fassungen
            BEGIN SELECT RAISE(ABORT, 'Fassungen werden nicht überschrieben.'); END;
        CREATE TRIGGER zuteilungen_bleiben BEFORE UPDATE ON zuteilungen
            BEGIN SELECT RAISE(ABORT, 'Zuteilungen werden nicht überschrieben – Beenden ist ein eigener Eintrag.'); END;
        CREATE TRIGGER zuteilung_ende_bleibt BEFORE UPDATE ON zuteilung_ende
            BEGIN SELECT RAISE(ABORT, 'Einträge werden nicht überschrieben.'); END;
        """,
        // 8 – Wochenplan: freie Einträge der Regelwoche (ändern = neuer Eintrag „ab Datum“, der
        //     alte endet), „endet ab“ als eigener Eintrag, Teilnahme an Therapien je Tag.
        """
        CREATE TABLE plan_eintraege (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            tag         INTEGER NOT NULL CHECK (tag BETWEEN 0 AND 6),   -- 0 = Sonntag (wie .NET)
            von         TEXT NOT NULL,
            bis         TEXT NOT NULL,
            titel       TEXT NOT NULL,
            ort         TEXT NOT NULL DEFAULT '',
            hinweis     TEXT NOT NULL DEFAULT '',
            ab          TEXT NOT NULL,
            ersetzt     INTEGER REFERENCES plan_eintraege(id) ON DELETE CASCADE,
            erstellt    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TABLE plan_ende (
            eintrag_id INTEGER PRIMARY KEY REFERENCES plan_eintraege(id) ON DELETE CASCADE,
            ab         TEXT NOT NULL,
            erstellt   TEXT NOT NULL,
            kuerzel    TEXT NOT NULL
        );
        CREATE TABLE teilnahmen (
            id           INTEGER PRIMARY KEY,
            zuteilung_id INTEGER NOT NULL REFERENCES zuteilungen(id) ON DELETE CASCADE,
            tag          TEXT NOT NULL,
            status       TEXT NOT NULL CHECK (status IN ('offen', 'teilgenommen', 'abgesagt', 'ausgefallen')),
            erstellt     TEXT NOT NULL,
            kuerzel      TEXT NOT NULL
        );
        CREATE TRIGGER plan_eintraege_bleiben BEFORE UPDATE ON plan_eintraege
            BEGIN SELECT RAISE(ABORT, 'Einträge werden nicht überschrieben – Änderungen gelten ab Datum.'); END;
        CREATE TRIGGER plan_ende_bleibt BEFORE UPDATE ON plan_ende
            BEGIN SELECT RAISE(ABORT, 'Einträge werden nicht überschrieben.'); END;
        CREATE TRIGGER teilnahmen_bleiben BEFORE UPDATE ON teilnahmen
            BEGIN SELECT RAISE(ABORT, 'Teilnahmen werden nicht überschrieben – ein neuer Eintrag gilt.'); END;
        """,
        // 9 – Kalender: je Termin Fassungen (ändern und absagen sind neue Fassungen). Die
        //     beteiligten Bewohner als JSON-Liste der Ids.
        """
        CREATE TABLE kalender (
            id INTEGER PRIMARY KEY
        );
        CREATE TABLE kalender_fassungen (
            id         INTEGER PRIMARY KEY,
            termin_id  INTEGER NOT NULL REFERENCES kalender(id) ON DELETE CASCADE,
            art        TEXT NOT NULL CHECK (art IN ('bewohner', 'wohnbereich', 'heimfahrt', 'ausflug')),
            titel      TEXT NOT NULL,
            beginn     TEXT NOT NULL,
            ende       TEXT NOT NULL,
            ganztags   INTEGER NOT NULL DEFAULT 0 CHECK (ganztags IN (0, 1)),
            ort        TEXT NOT NULL DEFAULT '',
            hinweis    TEXT NOT NULL DEFAULT '',
            bewohner   TEXT NOT NULL DEFAULT '[]',
            treffpunkt TEXT NOT NULL DEFAULT '',
            begleitung TEXT NOT NULL DEFAULT '',
            abgesagt   INTEGER NOT NULL DEFAULT 0 CHECK (abgesagt IN (0, 1)),
            erstellt   TEXT NOT NULL,
            kuerzel    TEXT NOT NULL
        );
        CREATE TRIGGER kalender_fassungen_bleiben BEFORE UPDATE ON kalender_fassungen
            BEGIN SELECT RAISE(ABORT, 'Termine werden nicht überschrieben – Änderungen sind neue Fassungen.'); END;
        """,
        // 10 – Biografie: je Bewohner und Abschnitt Fassungen (ändern ist eine neue Fassung).
        """
        CREATE TABLE biografie (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            abschnitt   INTEGER NOT NULL CHECK (abschnitt BETWEEN 0 AND 5),
            text        TEXT NOT NULL,
            angegeben   INTEGER NOT NULL DEFAULT 0 CHECK (angegeben IN (0, 1)),
            erstellt    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TRIGGER biografie_bleibt BEFORE UPDATE ON biografie
            BEGIN SELECT RAISE(ABORT, 'Die Biografie wird nicht überschrieben – Änderungen sind neue Fassungen.'); END;
        """,
    ];

    public static int AktuellerStand => Schritte.Length;

    private const string Spalten =
        "vorname, nachname, zimmer, geburtsdatum, aufnahmedatum, allergie, allergien, beschluss, beschlussende, bild";

    private readonly SqliteConnection db;
    public string Pfad { get; }

    public Speicher(string? pfad = null)
    {
        Pfad = pfad ?? Pfade.ImDatenordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(Pfad)!);
        // Pooling=False: Beim Schließen wird die Datei wirklich freigegeben (wichtig unter Windows).
        db = new SqliteConnection($"Data Source={Pfad};Mode=ReadWriteCreate;Pooling=False;Default Timeout=5");
        try
        {
            db.Open();
            Ausfuehren("PRAGMA foreign_keys = ON");
            Aufbauen();
        }
        catch (SqliteException e)
        {
            db.Dispose();
            throw new SpeicherFehler($"Die Datenbank {Pfad} lässt sich nicht öffnen: {e.Message}", e);
        }
        catch
        {
            db.Dispose();   // sonst bliebe die Datei unter Windows gesperrt
            throw;
        }
    }

    public void Dispose() => db.Dispose();

    // ------------------------------------------------------------------ Aufbau

    public int Stand() => Convert.ToInt32(Wert("PRAGMA user_version"), CultureInfo.InvariantCulture);

    private void Aufbauen()
    {
        int stand = Stand();
        if (stand > AktuellerStand)
            throw new SpeicherFehler("Die Daten stammen aus einer neueren Version des Programms. Bitte die neueste Version verwenden.");
        for (int nummer = stand; nummer < AktuellerStand; nummer++)
        {
            using var schritt = db.BeginTransaction();
            Ausfuehren(Schritte[nummer], schritt);
            Ausfuehren($"PRAGMA user_version = {nummer + 1}", schritt);
            schritt.Commit();
        }
    }

    // ------------------------------------------------------------------ Verwaltungsangaben

    public string? Meta(string schluessel) =>
        Wert("SELECT wert FROM meta WHERE schluessel = $s", ("$s", schluessel)) as string;

    public void SetzeMeta(string schluessel, string wert, SqliteTransaction? transaktion = null) =>
        Ausfuehren("INSERT INTO meta (schluessel, wert) VALUES ($s, $w) " +
                   "ON CONFLICT(schluessel) DO UPDATE SET wert = excluded.wert",
                   transaktion, ("$s", schluessel), ("$w", wert));

    // ------------------------------------------------------------------ Bewohner

    public List<Bewohner> AlleBewohner()
    {
        var liste = Lesen($"SELECT id, {Spalten} FROM bewohner");
        liste.Sort(BewohnerRegeln.NachName);
        return liste;
    }

    // Für Module, die innerhalb einer eigenen Transaktion lesen (z. B. beim Laden der Demo-Daten).
    public List<Bewohner> AlleBewohner(SqliteTransaction transaktion)
    {
        var liste = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        liste.Sort(BewohnerRegeln.NachName);
        return liste;
    }

    public Bewohner? Bewohner(long id) =>
        Lesen($"SELECT id, {Spalten} FROM bewohner WHERE id = $id", ("$id", id)).FirstOrDefault();

    // Legt an (ohne Id) oder ändert (mit Id). Gibt den gespeicherten Bewohner zurück.
    // neuesBild wird in den Datenordner kopiert; das bisherige Bild wird danach gelöscht.
    public Bewohner BewohnerSpeichern(Bewohner b, string? neuesBild = null, bool bildEntfernen = false)
    {
        var fehler = BewohnerRegeln.Pruefen(b);
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        Bewohner? vorher = b.Id is long id ? Bewohner(id) : null;
        if (b.Id is not null && vorher is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);

        string? bild = vorher is not null ? vorher.Bild : b.Bild;
        if (neuesBild is not null) bild = Bilder.Uebernehmen(neuesBild);
        else if (bildEntfernen) bild = null;
        b = b with { Bild = bild };

        try
        {
            using var transaktion = db.BeginTransaction();
            if (b.Id is null)
            {
                Ausfuehren($"INSERT INTO bewohner ({Spalten}) VALUES ({Platzhalter})", transaktion, Werte(b));
                b = b with { Id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture) };
            }
            else
            {
                string setzen = string.Join(", ", Spalten.Split(", ").Select(s => $"{s} = ${s}"));
                Ausfuehren($"UPDATE bewohner SET {setzen} WHERE id = $id", transaktion, [.. Werte(b), ("$id", b.Id)]);
            }
            transaktion.Commit();
        }
        catch (SqliteException)
        {
            if (neuesBild is not null) Bilder.Loeschen(bild);
            throw;
        }
        if (vorher?.Bild is string altesBild && altesBild != b.Bild) Bilder.Loeschen(altesBild);
        return b;
    }

    // ------------------------------------------------------------------ Demo-Daten

    // Lädt Demo-Daten, die noch nie geladen wurden: beim allerersten Start alles, nach einem
    // Update nur die neuen Bereiche (für die vorhandenen Bewohner). true = etwas geladen.
    public bool DemoBeimErstenStart()
    {
        if (Meta(DemoGeladen) is null)
        {
            DemoZuruecksetzen();
            return true;
        }
        // Nach einem Update: Bereiche, die es beim ersten Laden noch nicht gab, nachholen.
        bool berichte = Meta(DemoBerichte) is null, aufgaben = Meta(DemoAufgaben) is null, nachrichten = Meta(DemoNachrichten) is null,
             medikamente = Meta(DemoMedikamente) is null, medikation = Meta(DemoMedikation) is null,
             therapie = Meta(DemoTherapie) is null, wochenplan = Meta(DemoWochenplan) is null, kalender = Meta(DemoKalender) is null,
             biografie = Meta(DemoBiografie) is null;
        var neueModule = ModuleOhneDemo();
        bool modulNeu = neueModule.Count > 0;
        if (!berichte && !aufgaben && !nachrichten && !medikamente && !medikation && !therapie && !wochenplan && !kalender && !biografie && !modulNeu)
            return false;
        using var transaktion = db.BeginTransaction();
        if (berichte) DemoBerichteEinfuegen(transaktion);
        if (aufgaben) DemoAufgabenEinfuegen(transaktion);
        if (nachrichten) DemoNachrichtenEinfuegen(transaktion);
        if (medikamente) DemoMedikamenteEinfuegen(transaktion);
        if (medikamente || medikation) DemoMedikationEinfuegen(transaktion);   // hängt am Medikamenten-Bestand
        if (therapie) DemoTherapieEinfuegen(transaktion);
        if (therapie || wochenplan) DemoWochenplanEinfuegen(transaktion);   // Teilnahmen hängen an den Zuteilungen
        if (kalender) DemoKalenderEinfuegen(transaktion);
        if (biografie) DemoBiografieEinfuegen(transaktion);
        DemoModuleEinfuegen(transaktion, neueModule);
        transaktion.Commit();
        if (modulNeu) ModuleGeaendert?.Invoke();
        return true;
    }

    // Alle Bewohner mit allem, was dazugehört (Bilder, Berichte …), weg und die Demo-Daten aller
    // Bereiche neu laden – alles oder nichts.
    public void DemoZuruecksetzen()
    {
        var demo = Demodaten.Bewohner();   // erst lesen: Ist etwas kaputt, bleibt alles, wie es war
        var alteBilder = AlleBewohner().Select(b => b.Bild).Where(b => b is not null).ToList();
        using (var transaktion = db.BeginTransaction())
        {
            Ausfuehren("DELETE FROM bewohner", transaktion);   // Berichte und Nachträge folgen (CASCADE)
            foreach (var b in demo)
                Ausfuehren($"INSERT INTO bewohner ({Spalten}) VALUES ({Platzhalter})", transaktion, Werte(b));
            SetzeMeta(DemoGeladen, "1", transaktion);
            DemoBerichteEinfuegen(transaktion);
            DemoAufgabenEinfuegen(transaktion);
            DemoNachrichtenEinfuegen(transaktion);
            DemoMedikamenteEinfuegen(transaktion);
            DemoMedikationEinfuegen(transaktion);
            DemoTherapieEinfuegen(transaktion);
            DemoWochenplanEinfuegen(transaktion);
            DemoKalenderEinfuegen(transaktion);
            DemoBiografieEinfuegen(transaktion);
            DemoModuleEinfuegen(transaktion, module);
            transaktion.Commit();
        }
        foreach (var bild in alteBilder) Bilder.Loeschen(bild);
        AufgabenGeaendert?.Invoke();
        NachrichtenGeaendert?.Invoke();
        MedikationGeaendert?.Invoke();
        TherapieGeaendert?.Invoke();
        WochenplanGeaendert?.Invoke();
        KalenderGeaendert?.Invoke();
        ModuleGeaendert?.Invoke();
    }

    private void DemoBerichteEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        bewohner.Sort(BewohnerRegeln.NachName);
        Ausfuehren("DELETE FROM uebergaben WHERE bewohner_id IS NULL", transaktion);   // allgemeine Notizen der Demo
        var demo = Demodaten.Berichte(bewohner, Uhr());
        foreach (var d in demo.Berichte)
        {
            long id = BerichtEinfuegen(d.Bericht, transaktion);
            if (d.Uebergabe is UebergabeWunsch wunsch)
                DemoUebergabe(d.Bericht.BewohnerId, id, wunsch, d.Bericht.Erfasst, d.Bericht.Verfasser, d, transaktion);
        }
        foreach (var d in demo.Allgemein)
            DemoUebergabe(null, null, d.Wunsch, d.Erstellt, d.Verfasser, null, transaktion, d.Text);
        SetzeMeta(DemoBerichte, "1", transaktion);
    }

    private void DemoUebergabe(long? bewohnerId, long? berichtId, UebergabeWunsch wunsch, DateTime erstellt, string verfasser,
        Demodaten.DemoBericht? d, SqliteTransaction transaktion, string? text = null)
    {
        long id = UebergabeEinfuegen(bewohnerId, berichtId, text ?? wunsch.Text, erstellt, verfasser,
            UebergabeRegeln.GiltBis(wunsch.Dauer, erstellt, wunsch.Bis), transaktion);
        if (d?.GelesenVon is string leser)
            Ausfuehren("INSERT INTO uebergabe_gelesen (uebergabe_id, kuerzel, zeit) VALUES ($u, $k, $z)", transaktion,
                ("$u", id), ("$k", leser), ("$z", Zeit(erstellt.AddMinutes(30))));
        if (d?.ErledigtVon is string erlediger)
            Ausfuehren("INSERT INTO uebergabe_ereignisse (uebergabe_id, art, zeit, kuerzel) VALUES ($u, 'erledigt', $z, $k)", transaktion,
                ("$u", id), ("$z", Zeit(erstellt.AddHours(20))), ("$k", erlediger));
    }

    // ------------------------------------------------------------------ Berichte

    // Speichert einen neuen Bericht (samt Nachträgen, falls schon welche dabei sind) und, wenn
    // gewünscht, seinen Übergabe-Eintrag.
    public Bericht BerichtSpeichern(Bericht b, UebergabeWunsch? uebergabe = null)
    {
        if (Bewohner(b.BewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (b.Id is not null) throw new InvalidOperationException("Berichte werden nicht überschrieben – Korrekturen sind Nachträge.");
        if (uebergabe is not null) UebergabeRegeln.Pruefen(uebergabe, b.Erfasst);
        using var transaktion = db.BeginTransaction();
        long id = BerichtEinfuegen(b, transaktion);
        if (uebergabe is not null)
            UebergabeEinfuegen(b.BewohnerId, id, uebergabe.Text, b.Erfasst, b.Verfasser,
                UebergabeRegeln.GiltBis(uebergabe.Dauer, b.Erfasst, uebergabe.Bis), transaktion);
        transaktion.Commit();
        return BerichtLesen(id)!;
    }

    private long BerichtEinfuegen(Bericht b, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO berichte (bewohner_id, zeitpunkt, schicht, verfasser, bereiche, text, wichtig, erfasst) " +
                   "VALUES ($b, $z, $s, $v, $bereiche, $t, $w, $e)", transaktion,
            ("$b", b.BewohnerId), ("$z", Zeit(b.Zeitpunkt)), ("$s", BerichtRegeln.Kuerzel(b.Schicht)), ("$v", b.Verfasser),
            ("$bereiche", string.Join(",", b.Bereiche)), ("$t", b.Text), ("$w", b.Wichtig ? 1 : 0), ("$e", Zeit(b.Erfasst)));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        foreach (var n in b.Nachtraege)
            NachtragEinfuegen(id, n.Verfasser, n.Text, n.Erfasst, transaktion);
        return id;
    }

    // Korrektur oder Ergänzung zu einem Bericht. Der ursprüngliche Text bleibt, wie er ist.
    public Nachtrag NachtragSpeichern(long berichtId, string verfasser, string text, DateTime erfasst)
    {
        BerichtRegeln.NachtragPruefen(verfasser, text);
        if (BerichtLesen(berichtId) is null) throw new Eingabefehler(["Diesen Bericht gibt es nicht mehr."]);
        using var transaktion = db.BeginTransaction();
        long id = NachtragEinfuegen(berichtId, verfasser.Trim(), text.Trim(), erfasst, transaktion);
        transaktion.Commit();
        return new Nachtrag(id, berichtId, erfasst, verfasser.Trim(), text.Trim());
    }

    private long NachtragEinfuegen(long berichtId, string verfasser, string text, DateTime erfasst, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO nachtraege (bericht_id, erfasst, verfasser, text) VALUES ($b, $e, $v, $t)", transaktion,
            ("$b", berichtId), ("$e", Zeit(erfasst)), ("$v", verfasser), ("$t", text));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    public Bericht? BerichtLesen(long id) => BerichteLesen("WHERE id = $id", ("$id", id)).FirstOrDefault();

    // Berichte eines Bewohners, neueste oben, mit Filter.
    public List<Bericht> Berichte(long bewohnerId, BerichtFilter? filter = null)
    {
        filter ??= new BerichtFilter();
        return BerichteLesen("WHERE bewohner_id = $b", ("$b", bewohnerId))
            .Where(b => BerichtRegeln.Passt(b, filter))
            .OrderByDescending(b => b.Zeitpunkt).ThenByDescending(b => b.Id)
            .ToList();
    }

    public int WichtigeBerichteSeit(long bewohnerId, DateTime seit) => Convert.ToInt32(
        Wert("SELECT COUNT(*) FROM berichte WHERE bewohner_id = $b AND wichtig = 1 AND zeitpunkt >= $s", ("$b", bewohnerId), ("$s", Zeit(seit))),
        CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ Übergabe

    private long UebergabeEinfuegen(long? bewohnerId, long? berichtId, string text, DateTime erstellt, string verfasser,
        DateTime? giltBis, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO uebergaben (bewohner_id, bericht_id, text, erstellt, verfasser, gilt_bis) VALUES ($b, $r, $t, $e, $v, $g)",
            transaktion, ("$b", bewohnerId), ("$r", berichtId), ("$t", text.Trim()), ("$e", Zeit(erstellt)), ("$v", verfasser),
            ("$g", giltBis is DateTime g ? Zeit(g) : null));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // Eigene kurze Übergabe-Notiz für den ganzen Wohnbereich (ohne Bericht und ohne Bewohner).
    public Uebergabe WohnbereichNotiz(string text, string verfasser, UebergabeWunsch wunsch, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) fehler.Add("Text der Notiz fehlt.");
        if (string.IsNullOrWhiteSpace(verfasser)) fehler.Add("Verfasser (Kürzel) fehlt.");
        else if (Einstellungen.KuerzelPruefen(verfasser) is string f) fehler.Add(f.Replace("Kürzel", "Verfasser"));
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        UebergabeRegeln.Pruefen(wunsch, jetzt);
        using var transaktion = db.BeginTransaction();
        long id = UebergabeEinfuegen(null, null, text, jetzt, verfasser.Trim(), UebergabeRegeln.GiltBis(wunsch.Dauer, jetzt, wunsch.Bis), transaktion);
        transaktion.Commit();
        return UebergabeLesen(id)!;
    }

    public Uebergabe? UebergabeLesen(long id) => UebergabenLesen("WHERE id = $id", ("$id", id)).FirstOrDefault();

    // Alle Übergabe-Einträge; ohne „auchErledigte“ nur die offenen (nicht erledigt, nicht abgelaufen).
    public List<Uebergabe> Uebergaben(DateTime jetzt, bool auchErledigte = false) =>
        UebergabenLesen("").Where(u => auchErledigte || UebergabeRegeln.Offen(u, jetzt))
            .OrderByDescending(u => u.Erstellt).ThenByDescending(u => u.Id).ToList();

    public void UebergabeGelesen(long id, string kuerzel, DateTime zeit)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Für „gelesen“ bitte in den Einstellungen ein Kürzel eintragen."]);
        Ausfuehren("INSERT OR IGNORE INTO uebergabe_gelesen (uebergabe_id, kuerzel, zeit) VALUES ($u, $k, $z)", null,
            ("$u", id), ("$k", kuerzel.Trim()), ("$z", Zeit(zeit)));
    }

    public void UebergabeVerlaengern(long id, UebergabeWunsch wunsch, string kuerzel, DateTime zeit)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Kürzel fehlt (Einstellungen)."]);
        UebergabeRegeln.Pruefen(wunsch, zeit);
        Ausfuehren("INSERT INTO uebergabe_ereignisse (uebergabe_id, art, zeit, kuerzel, gilt_bis) VALUES ($u, 'verlaengert', $z, $k, $g)", null,
            ("$u", id), ("$z", Zeit(zeit)), ("$k", kuerzel.Trim()),
            ("$g", UebergabeRegeln.GiltBis(wunsch.Dauer, zeit, wunsch.Bis) is DateTime g ? Zeit(g) : null));
    }

    public void UebergabeErledigt(long id, string kuerzel, DateTime zeit)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Kürzel fehlt (Einstellungen)."]);
        Ausfuehren("INSERT INTO uebergabe_ereignisse (uebergabe_id, art, zeit, kuerzel) VALUES ($u, 'erledigt', $z, $k)", null,
            ("$u", id), ("$z", Zeit(zeit)), ("$k", kuerzel.Trim()));
    }

    // Offene Übergaben eines Bewohners, die dieses Kürzel noch nicht gelesen hat (ohne Kürzel: alle offenen).
    public int UngeleseneUebergaben(long bewohnerId, string kuerzel, DateTime jetzt) =>
        UebergabenLesen("WHERE bewohner_id = $b", ("$b", bewohnerId))
            .Count(u => UebergabeRegeln.Offen(u, jetzt) && !UebergabeRegeln.GelesenVon(u, kuerzel));

    // „Neu seit deiner letzten Schicht“: der letzte Besuch der Übergabe mit diesem Kürzel.
    public DateTime? LetzterUebergabeBesuch(string kuerzel) =>
        kuerzel.Length > 0 && Meta($"uebergabe_besuch:{kuerzel.ToUpperInvariant()}") is string s ? AusZeit(s) : null;

    public void UebergabeBesuchMerken(string kuerzel, DateTime zeit)
    {
        if (kuerzel.Length > 0) SetzeMeta($"uebergabe_besuch:{kuerzel.ToUpperInvariant()}", Zeit(zeit));
    }

    private List<Uebergabe> UebergabenLesen(string bedingung, params (string, object?)[] parameter)
    {
        var liste = new List<Uebergabe>();
        using (var befehl = Befehl($"SELECT id, bewohner_id, bericht_id, text, erstellt, verfasser, gilt_bis FROM uebergaben {bedingung}", null, parameter))
        using (var leser = befehl.ExecuteReader())
        {
            while (leser.Read())
                liste.Add(new Uebergabe
                {
                    Id = leser.GetInt64(0),
                    BewohnerId = leser.IsDBNull(1) ? null : leser.GetInt64(1),
                    BerichtId = leser.IsDBNull(2) ? null : leser.GetInt64(2),
                    Text = leser.GetString(3),
                    Erstellt = AusZeit(leser.GetString(4)),
                    Verfasser = leser.GetString(5),
                    GiltBis = leser.IsDBNull(6) ? null : AusZeit(leser.GetString(6)),
                });
        }
        if (liste.Count == 0) return liste;
        string ids = string.Join(",", liste.Select(u => u.Id));
        var ereignisse = new List<(long Id, string Art, DateTime Zeit, string Kuerzel, DateTime? GiltBis)>();
        using (var befehl = Befehl($"SELECT uebergabe_id, art, zeit, kuerzel, gilt_bis FROM uebergabe_ereignisse WHERE uebergabe_id IN ({ids}) ORDER BY zeit, id", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                ereignisse.Add((leser.GetInt64(0), leser.GetString(1), AusZeit(leser.GetString(2)), leser.GetString(3),
                                leser.IsDBNull(4) ? null : AusZeit(leser.GetString(4))));
        var gelesen = new List<(long Id, Kenntnisnahme K)>();
        using (var befehl = Befehl($"SELECT uebergabe_id, kuerzel, zeit FROM uebergabe_gelesen WHERE uebergabe_id IN ({ids}) ORDER BY zeit", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                gelesen.Add((leser.GetInt64(0), new Kenntnisnahme(leser.GetString(1), AusZeit(leser.GetString(2)))));
        var berichtIds = liste.Where(u => u.BerichtId is not null).Select(u => u.BerichtId!.Value).Distinct().ToList();
        var berichte = berichtIds.Count == 0 ? [] :
            BerichteLesen($"WHERE id IN ({string.Join(",", berichtIds)})").ToDictionary(b => b.Id!.Value);

        return liste.Select(u =>
        {
            var eigene = ereignisse.Where(e => e.Id == u.Id).ToList();
            var verlaengerungen = eigene.Where(e => e.Art == "verlaengert").ToList();
            var erledigt = eigene.FirstOrDefault(e => e.Art == "erledigt");
            return u with
            {
                GiltBis = verlaengerungen.Count > 0 ? verlaengerungen[^1].GiltBis : u.GiltBis,
                Verlaengert = verlaengerungen.Count,
                Erledigt = erledigt.Art is null ? null : erledigt.Zeit,
                ErledigtVon = erledigt.Art is null ? null : erledigt.Kuerzel,
                Gelesen = gelesen.Where(g => g.Id == u.Id).Select(g => g.K).ToList(),
                Bericht = u.BerichtId is long r && berichte.TryGetValue(r, out var b) ? b : null,
            };
        }).ToList();
    }

    private List<Bericht> BerichteLesen(string bedingung, params (string, object?)[] parameter)
    {
        var berichte = new List<Bericht>();
        using (var befehl = Befehl("SELECT id, bewohner_id, zeitpunkt, schicht, verfasser, bereiche, text, wichtig, erfasst, " +
                                   $"EXISTS (SELECT 1 FROM uebergaben u WHERE u.bericht_id = berichte.id) FROM berichte {bedingung}", null, parameter))
        using (var leser = befehl.ExecuteReader())
        {
            while (leser.Read())
            {
                berichte.Add(new Bericht
                {
                    Id = leser.GetInt64(0),
                    BewohnerId = leser.GetInt64(1),
                    Zeitpunkt = AusZeit(leser.GetString(2)),
                    Schicht = BerichtRegeln.SchichtAus(leser.GetString(3)),
                    Verfasser = leser.GetString(4),
                    Bereiche = leser.GetString(5).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList(),
                    Text = leser.GetString(6),
                    Wichtig = leser.GetInt64(7) != 0,
                    Erfasst = AusZeit(leser.GetString(8)),
                    Uebergabe = leser.GetInt64(9) != 0,
                });
            }
        }
        if (berichte.Count == 0) return berichte;
        var nachtraege = new Dictionary<long, List<Nachtrag>>();
        string ids = string.Join(",", berichte.Select(b => b.Id!.Value));
        using (var befehl = Befehl($"SELECT id, bericht_id, erfasst, verfasser, text FROM nachtraege WHERE bericht_id IN ({ids}) ORDER BY erfasst, id", null, []))
        using (var leser = befehl.ExecuteReader())
        {
            while (leser.Read())
            {
                var n = new Nachtrag(leser.GetInt64(0), leser.GetInt64(1), AusZeit(leser.GetString(2)), leser.GetString(3), leser.GetString(4));
                if (!nachtraege.TryGetValue(n.BerichtId, out var liste)) nachtraege[n.BerichtId] = liste = [];
                liste.Add(n);
            }
        }
        return berichte.Select(b => nachtraege.TryGetValue(b.Id!.Value, out var n) ? b with { Nachtraege = n } : b).ToList();
    }

    private static string Zeit(DateTime t) => t.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    private static DateTime AusZeit(string s) => DateTime.ParseExact(s, ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm"], CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ Hilfen

    private static string Platzhalter => string.Join(", ", Spalten.Split(", ").Select(s => "$" + s));

    private static (string, object?)[] Werte(Bewohner b) =>
    [
        ("$vorname", b.Vorname),
        ("$nachname", b.Nachname),
        ("$zimmer", b.Zimmer),
        ("$geburtsdatum", Iso(b.Geburtsdatum)),
        ("$aufnahmedatum", Iso(b.Aufnahmedatum)),
        ("$allergie", BewohnerRegeln.Kuerzel(b.Allergie)),
        ("$allergien", b.Allergien),
        ("$beschluss", BewohnerRegeln.Kuerzel(b.Beschluss)),
        ("$beschlussende", b.Beschlussende is DateOnly e ? Iso(e) : null),
        ("$bild", b.Bild),
    ];

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly AusIso(string text) => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private List<Bewohner> Lesen(string sql, params (string, object?)[] parameter) => LesenIn(null, sql, parameter);

    private List<Bewohner> LesenIn(SqliteTransaction? transaktion, string sql, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, transaktion, parameter);
        using var leser = befehl.ExecuteReader();
        var liste = new List<Bewohner>();
        while (leser.Read())
        {
            liste.Add(new Bewohner
            {
                Id = leser.GetInt64(0),
                Vorname = leser.GetString(1),
                Nachname = leser.GetString(2),
                Zimmer = leser.GetString(3),
                Geburtsdatum = AusIso(leser.GetString(4)),
                Aufnahmedatum = AusIso(leser.GetString(5)),
                // Die Datenbank lässt nur gültige Kürzel zu (CHECK); im Zweifel „nicht erfasst“.
                Allergie = BewohnerRegeln.AllergieAus(leser.GetString(6)) ?? AllergieZustand.NichtErfasst,
                Allergien = leser.GetString(7),
                Beschluss = BewohnerRegeln.BeschlussAus(leser.GetString(8)) ?? BeschlussZustand.NichtErfasst,
                Beschlussende = leser.IsDBNull(9) ? null : AusIso(leser.GetString(9)),
                Bild = leser.IsDBNull(10) ? null : leser.GetString(10),
            });
        }
        return liste;
    }

    private SqliteCommand Befehl(string sql, SqliteTransaction? transaktion, (string, object?)[] parameter)
    {
        var befehl = db.CreateCommand();
        befehl.CommandText = sql;
        befehl.Transaction = transaktion;
        foreach (var (name, wert) in parameter)
            befehl.Parameters.AddWithValue(name, wert ?? DBNull.Value);
        return befehl;
    }

    private void Ausfuehren(string sql, SqliteTransaction? transaktion = null, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, transaktion, parameter);
        befehl.ExecuteNonQuery();
    }

    private object? Wert(string sql, params (string, object?)[] parameter) => WertIn(null, sql, parameter);

    private object? WertIn(SqliteTransaction? transaktion, string sql, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, transaktion, parameter);
        return befehl.ExecuteScalar();
    }

    // Direkter Zugriff auf die Verbindung: für Module (eigene Tabellen) und Tests (CHECK-Regeln prüfen).
    public SqliteConnection Verbindung => db;
}

// Die Datenbank lässt sich nicht verwenden. Der Text ist für Menschen gedacht.
public sealed class SpeicherFehler(string meldung, Exception? ursache = null) : Exception(meldung, ursache);
