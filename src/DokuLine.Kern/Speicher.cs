using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Der Speicher ist eine SQLite-Datei im Datenordner. Jede Erweiterung des Aufbaus ist ein Eintrag in Schritte; die
// Datei merkt sich in „PRAGMA user_version“, wie viele sie hat, fehlende werden beim Öffnen nachgeholt (jeder ganz
// oder gar nicht). Die Datei der Python-Version (dokusystem.sqlite3) wird weder gelesen noch gelöscht.
public sealed partial class Speicher : IDisposable
{
    public const string Datei = "dokumentationssystem.db";
    private const string DemoGeladen = "demo_geladen";
    private const string DemoVoll = "demo_23";   // 23 Bewohner je Wohnbereich
    private const string DemoBerichte = "demo_berichte";
    private const string DemoAufgaben = "demo_aufgaben";
    private const string DemoNachrichten = "demo_nachrichten";
    private const string DemoMedikamente = "demo_medikamente";
    private const string DemoMedikation = "demo_medikation";
    private const string DemoTherapie = "demo_therapie";
    private const string DemoTherapieAnbieter = "demo_therapie_anbieter";   // Demo-Angebote mit Anbieter
    private const string DemoTherapieTag = "demo_therapie_tag";   // Angebote über den ganzen Tag
    private const string DemoKalenderHaus = "demo_kalender_haus";            // Demo-Termine im Kalender „Haus“
    private const string DemoFahrten = "demo_fahrten";                      // Fahrzeuge und Fahrten
    private const string DemoStandardaufgaben = "demo_standardaufgaben";    // Standardaufgaben je Schicht
    private const string DemoVisiten = "demo_visiten";                      // Visiten mit allen drei Fällen
    private const string DemoAerzte = "demo_aerzte";                        // Ärzteverzeichnis
    private const string DemoBedarf = "demo_bedarf";                        // Bedarfsgaben (mit Absprache)
    private const string DemoBetreuer = "demo_betreuer";                    // Betreuer (E-Mail nur @example.org)
    private const string DemoWochenplan = "demo_wochenplan";
    private const string DemoKalender = "demo_kalender";
    private const string DemoBiografie = "demo_biografie";
    private const string DemoReflexion = "demo_reflexion";
    private const string DemoHeb = "demo_heb";
    private const string DemoStatus = "demo_status";

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
        // 11 – Monatsreflexion: je Bewohner und Monat Fassungen; die fünf Texte als JSON-Liste.
        """
        CREATE TABLE reflexionen (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            monat       TEXT NOT NULL,
            texte       TEXT NOT NULL,
            fertig      INTEGER NOT NULL DEFAULT 0 CHECK (fertig IN (0, 1)),
            erstellt    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TRIGGER reflexionen_bleiben BEFORE UPDATE ON reflexionen
            BEGIN SELECT RAISE(ABORT, 'Reflexionen werden nicht überschrieben – Änderungen sind neue Fassungen.'); END;
        """,
        // 12 – HEB-Bogen: Kopf je Bogen, Fassungen mit Texten und Hilfebedarf als JSON.
        """
        CREATE TABLE heb_boegen (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            art         TEXT NOT NULL CHECK (art IN ('A', 'B', 'C')),
            von         TEXT NOT NULL,
            bis         TEXT NOT NULL,
            nummer      INTEGER,
            erstellt    TEXT NOT NULL
        );
        CREATE TABLE heb_fassungen (
            id          INTEGER PRIMARY KEY,
            bogen_id    INTEGER NOT NULL REFERENCES heb_boegen(id) ON DELETE CASCADE,
            texte       TEXT NOT NULL,
            hilfebedarf TEXT NOT NULL,
            fertig      INTEGER NOT NULL DEFAULT 0 CHECK (fertig IN (0, 1)),
            erstellt    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TRIGGER heb_boegen_bleiben BEFORE UPDATE ON heb_boegen
            BEGIN SELECT RAISE(ABORT, 'HEB-Bögen werden nicht überschrieben.'); END;
        CREATE TRIGGER heb_fassungen_bleiben BEFORE UPDATE ON heb_fassungen
            BEGIN SELECT RAISE(ABORT, 'HEB-Bögen werden nicht überschrieben – Änderungen sind neue Fassungen.'); END;
        """,
        // 13 – „Offen geführt“ in der Bewohnerakte (drei Zustände). Vorhandene Bewohner: nicht erfasst.
        """
        ALTER TABLE bewohner ADD COLUMN offen_gefuehrt TEXT NOT NULL DEFAULT 'nicht_erfasst'
            CHECK (offen_gefuehrt IN ('ja', 'nein', 'nicht_erfasst'));
        """,
        // 14 – Mehrplatz (Demo-Stufe): Wohnbereich je Bewohner (vorhandene: „Wohnbereich A“) und
        //      Statusmeldungen. Eine neue Meldung ersetzt die alte, „aufheben“ ist eine Meldung mit
        //      leerem Text; nichts wird überschrieben, der Verlauf bleibt.
        """
        ALTER TABLE bewohner ADD COLUMN wohnbereich TEXT NOT NULL DEFAULT 'Wohnbereich A';
        CREATE TABLE statusmeldungen (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            text        TEXT NOT NULL,                -- leer = aufgehoben
            bereich     TEXT NOT NULL,
            kuerzel     TEXT NOT NULL,
            erstellt    TEXT NOT NULL,
            gilt_bis    TEXT NOT NULL
        );
        CREATE INDEX statusmeldungen_bewohner ON statusmeldungen(bewohner_id, erstellt);
        CREATE TRIGGER statusmeldungen_bleiben BEFORE UPDATE ON statusmeldungen
            BEGIN SELECT RAISE(ABORT, 'Statusmeldungen werden nicht überschrieben – eine neue ersetzt die alte.'); END;
        """,
        // 15 – Mitarbeiter, Anmeldung und Rechte: Fachkraft und aktiv je Mitarbeiter (Tabelle
        //      „kollegen“), Listen der Bereiche und Positionen, Positionen je Mitarbeiter (die Bereiche
        //      stehen in „team_mitglieder“), Rechte als Häkchen Funktion × Position, „nur Fachkräfte“.
        """
        ALTER TABLE kollegen ADD COLUMN fachkraft INTEGER NOT NULL DEFAULT 0 CHECK (fachkraft IN (0, 1));
        ALTER TABLE kollegen ADD COLUMN aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1));
        CREATE TABLE bereiche (
            name  TEXT PRIMARY KEY,
            aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)),
            nr    INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE positionen (
            name  TEXT PRIMARY KEY,
            aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)),
            nr    INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE mitarbeiter_positionen (
            kuerzel  TEXT NOT NULL REFERENCES kollegen(kuerzel) ON DELETE CASCADE,
            position TEXT NOT NULL REFERENCES positionen(name) ON UPDATE CASCADE,
            PRIMARY KEY (kuerzel, position)
        );
        CREATE TABLE rechte (
            funktion TEXT NOT NULL,
            position TEXT NOT NULL REFERENCES positionen(name) ON DELETE CASCADE ON UPDATE CASCADE,
            PRIMARY KEY (funktion, position)
        );
        CREATE TABLE rechte_fachkraft (
            funktion TEXT PRIMARY KEY
        );
        """,
        // 16 – Bezugspflege und Einzeltrainer: Wechsel sind neue Einträge (der neueste gilt, Kürzel
        //      leer = keine), Arten der Einzeltrainings als Liste, eigene Farbe je Mitarbeiter.
        """
        ALTER TABLE kollegen ADD COLUMN farbe TEXT;
        CREATE TABLE bezugspflege (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            kuerzel     TEXT NOT NULL,
            ab          TEXT NOT NULL,
            erstellt    TEXT NOT NULL,
            von         TEXT NOT NULL
        );
        CREATE TABLE einzeltrainer (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            kuerzel     TEXT NOT NULL,
            art         TEXT NOT NULL,
            ab          TEXT NOT NULL,
            erstellt    TEXT NOT NULL,
            von         TEXT NOT NULL
        );
        CREATE TABLE einzeltrainer_arten (
            name TEXT PRIMARY KEY,
            nr   INTEGER NOT NULL DEFAULT 0
        );
        INSERT INTO einzeltrainer_arten (name, nr) VALUES ('DBT', 1), ('Suchtgruppe', 2);
        CREATE TRIGGER bezugspflege_bleibt BEFORE UPDATE ON bezugspflege
            BEGIN SELECT RAISE(ABORT, 'Die Bezugspflege wird nicht überschrieben – ein Wechsel ist ein neuer Eintrag.'); END;
        CREATE TRIGGER einzeltrainer_bleibt BEFORE UPDATE ON einzeltrainer
            BEGIN SELECT RAISE(ABORT, 'Einzeltrainer werden nicht überschrieben – ein Wechsel ist ein neuer Eintrag.'); END;
        """,
        // 17 – Medikamentendatenbank: Prüfdatum der Bestätigung, „gesperrt“ und Zulassungsnummer je Fassung.
        """
        ALTER TABLE medikament_fassungen ADD COLUMN geprueft_am TEXT;
        ALTER TABLE medikament_fassungen ADD COLUMN zulassungsnummer TEXT NOT NULL DEFAULT '';
        ALTER TABLE medikament_fassungen ADD COLUMN gesperrt INTEGER NOT NULL DEFAULT 0 CHECK (gesperrt IN (0, 1));
        """,
        // 18 – Therapieangebote: Anbieter je Fassung („Therapie“ oder ein Wohnbereich).
        """
        ALTER TABLE angebot_fassungen ADD COLUMN anbieter TEXT NOT NULL DEFAULT 'Therapie';
        """,
        // 19 – Kalender „Haus“ und Fahrzeuge mit Fahrten („losgefahren“, „zurück“ als eigene Einträge).
        """
        ALTER TABLE kalender_fassungen ADD COLUMN haus INTEGER NOT NULL DEFAULT 0 CHECK (haus IN (0, 1));
        CREATE TABLE fahrzeuge (
            name  TEXT PRIMARY KEY COLLATE NOCASE,
            nr    INTEGER NOT NULL DEFAULT 0,
            aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1))
        );
        CREATE TABLE fahrten (
            id       INTEGER PRIMARY KEY,
            fahrzeug TEXT NOT NULL,
            von      TEXT NOT NULL,
            bis      TEXT NOT NULL,
            zweck    TEXT NOT NULL CHECK (zweck IN ('arzttermin', 'ausflug', 'sporttherapie', 'sonstiges')),
            fahrer   TEXT NOT NULL,
            bewohner TEXT NOT NULL DEFAULT '[]',
            ziel     TEXT NOT NULL DEFAULT '',
            erstellt TEXT NOT NULL,
            kuerzel  TEXT NOT NULL
        );
        CREATE TABLE fahrt_ereignisse (
            id       INTEGER PRIMARY KEY,
            fahrt_id INTEGER NOT NULL REFERENCES fahrten(id) ON DELETE CASCADE,
            art      TEXT NOT NULL CHECK (art IN ('los', 'zurueck')),
            zeit     TEXT NOT NULL,
            kuerzel  TEXT NOT NULL,
            UNIQUE (fahrt_id, art)
        );
        CREATE TRIGGER fahrten_bleiben BEFORE UPDATE ON fahrten
            BEGIN SELECT RAISE(ABORT, 'Fahrten werden nicht überschrieben.'); END;
        CREATE TRIGGER fahrt_ereignisse_bleiben BEFORE UPDATE ON fahrt_ereignisse
            BEGIN SELECT RAISE(ABORT, '„losgefahren“ und „zurück“ werden nicht überschrieben.'); END;
        """,
        // 20 – Schichtzeiten je Bereich und Standardaufgaben (Vorlagen); Aufgaben merken ihre Vorlage,
        // je Vorlage und Schicht entsteht höchstens eine Aufgabe.
        """
        CREATE TABLE schichtzeiten (
            bereich TEXT PRIMARY KEY,
            frueh   TEXT NOT NULL,
            spaet   TEXT NOT NULL,
            nacht   TEXT NOT NULL
        );
        CREATE TABLE aufgabe_vorlagen (
            id          INTEGER PRIMARY KEY,
            text        TEXT NOT NULL,
            schicht     TEXT NOT NULL CHECK (schicht IN ('frueh', 'spaet', 'nacht')),
            tage        INTEGER NOT NULL DEFAULT 127,
            uhrzeit     TEXT,
            bewohner_id INTEGER REFERENCES bewohner(id) ON DELETE CASCADE,
            aktiv       INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)),
            erstellt    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        ALTER TABLE aufgaben ADD COLUMN vorlage_id INTEGER REFERENCES aufgabe_vorlagen(id);
        CREATE UNIQUE INDEX aufgaben_aus_vorlage ON aufgaben (vorlage_id, schicht_tag, schicht) WHERE vorlage_id IS NOT NULL;
        """,
        // 21 – Visiten: je Bewohner das Besprochene, geänderte Anordnungen und Facharzttermine; die
        // Aufgaben daraus merken sich ihre Anordnung bzw. ihren Eintrag. Nichts wird überschrieben.
        """
        CREATE TABLE visiten (
            id         INTEGER PRIMARY KEY,
            datum      TEXT NOT NULL,
            art        TEXT NOT NULL,
            arzt       TEXT NOT NULL,
            teilnehmer TEXT NOT NULL DEFAULT '',
            erstellt   TEXT NOT NULL,
            kuerzel    TEXT NOT NULL
        );
        CREATE TABLE visite_eintraege (
            id          INTEGER PRIMARY KEY,
            visite_id   INTEGER NOT NULL REFERENCES visiten(id) ON DELETE CASCADE,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            besprochen  TEXT NOT NULL,
            facharzt    TEXT NOT NULL DEFAULT '',
            dringend    INTEGER NOT NULL DEFAULT 0 CHECK (dringend IN (0, 1))
        );
        CREATE TABLE visite_anordnungen (
            id             INTEGER PRIMARY KEY,
            eintrag_id     INTEGER NOT NULL REFERENCES visite_eintraege(id) ON DELETE CASCADE,
            art            TEXT NOT NULL CHECK (art IN ('neu', 'dosis', 'pause', 'absetzen')),
            medikament_id  INTEGER,   -- Medikament aus der Datenbank (ohne Fremdschlüssel: der Demo-Bestand kann neu geladen werden)
            medikament     TEXT NOT NULL
        );
        CREATE TRIGGER visiten_bleiben BEFORE UPDATE ON visiten
            BEGIN SELECT RAISE(ABORT, 'Visiten werden nicht überschrieben.'); END;
        CREATE TRIGGER visite_eintraege_bleiben BEFORE UPDATE ON visite_eintraege
            BEGIN SELECT RAISE(ABORT, 'Visiten werden nicht überschrieben.'); END;
        CREATE TRIGGER visite_anordnungen_bleiben BEFORE UPDATE ON visite_anordnungen
            BEGIN SELECT RAISE(ABORT, 'Visiten werden nicht überschrieben.'); END;
        ALTER TABLE aufgaben ADD COLUMN visite_anordnung_id INTEGER REFERENCES visite_anordnungen(id);
        ALTER TABLE aufgaben ADD COLUMN visite_eintrag_id INTEGER REFERENCES visite_eintraege(id);
        """,
        // 22 – Entwürfe: angefangene Eingaben beim Abmelden; gehören dem Kürzel, das sie anfing.
        """
        CREATE TABLE entwuerfe (
            id          INTEGER PRIMARY KEY,
            kuerzel     TEXT NOT NULL COLLATE NOCASE,
            art         TEXT NOT NULL,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            inhalt      TEXT NOT NULL,
            geaendert   TEXT NOT NULL,
            UNIQUE (kuerzel, art, bewohner_id)
        );
        """,
        // 23 – Ärzteverzeichnis: Auswahl statt Freitext; nie löschen, nur inaktiv.
        """
        CREATE TABLE aerzte (
            id           INTEGER PRIMARY KEY,
            name         TEXT NOT NULL,
            fachrichtung TEXT NOT NULL DEFAULT '',
            praxis       TEXT NOT NULL DEFAULT '',
            telefon      TEXT NOT NULL DEFAULT '',
            email        TEXT NOT NULL DEFAULT '',
            aktiv        INTEGER NOT NULL DEFAULT 1,
            geaendert    TEXT NOT NULL,
            kuerzel      TEXT NOT NULL
        );
        CREATE UNIQUE INDEX aerzte_name ON aerzte (name COLLATE NOCASE);
        """,
        // 24 – Visite mit allen Bewohnern: je Bereich ein Entwurf, bis „Visite abschließen“.
        """
        CREATE TABLE visite_entwuerfe (
            bereich    TEXT PRIMARY KEY,
            datum      TEXT NOT NULL,
            arzt       TEXT NOT NULL,
            teilnehmer TEXT NOT NULL DEFAULT '',
            kuerzel    TEXT NOT NULL,
            begonnen   TEXT NOT NULL,
            eintraege  TEXT NOT NULL DEFAULT '[]'
        );
        """,
        // 25 – Wohnbereiche R1–R3, Zimmer und Betten: Bereiche „offen“/„beschuetzt“ (leer = kein
        // Wohnbereich, z. B. Therapie), Zimmer mit 1 Bett (Einzelzimmer) oder 2 (Fenster, Tür), Bett am Bewohner.
        """
        ALTER TABLE bereiche ADD COLUMN art TEXT CHECK (art IS NULL OR art IN ('offen', 'beschuetzt'));
        CREATE TABLE zimmer (
            nummer  TEXT PRIMARY KEY,
            bereich TEXT NOT NULL,
            betten  INTEGER NOT NULL CHECK (betten IN (1, 2)),
            aktiv   INTEGER NOT NULL DEFAULT 1
        );
        ALTER TABLE bewohner ADD COLUMN bett TEXT NOT NULL DEFAULT '';
        """,
        // 26 – Ärztliche Informationen: Diagnosen (nie gelöscht, nur „beendet am“), Art der
        // Reaktion je Allergie, Hinweise für den Notfall.
        """
        CREATE TABLE diagnosen (
            id              INTEGER PRIMARY KEY,
            bewohner_id     INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            icd             TEXT NOT NULL DEFAULT '',
            bezeichnung     TEXT NOT NULL,
            seit            TEXT,
            art             TEXT NOT NULL CHECK (art IN ('haupt', 'neben')),
            arzt            TEXT NOT NULL DEFAULT '',
            hinweis         TEXT NOT NULL DEFAULT '',
            erstellt        TEXT NOT NULL,
            kuerzel         TEXT NOT NULL,
            beendet         TEXT,
            beendet_kuerzel TEXT
        );
        CREATE TRIGGER diagnosen_bleiben BEFORE UPDATE ON diagnosen
            WHEN OLD.beendet IS NOT NULL OR NEW.bezeichnung IS NOT OLD.bezeichnung OR NEW.icd IS NOT OLD.icd OR NEW.art IS NOT OLD.art
                 OR NEW.seit IS NOT OLD.seit OR NEW.arzt IS NOT OLD.arzt OR NEW.hinweis IS NOT OLD.hinweis
            BEGIN SELECT RAISE(ABORT, 'Diagnosen werden nicht überschrieben – beenden und neu eintragen.'); END;
        CREATE TABLE allergie_reaktionen (
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            stoff       TEXT NOT NULL,
            reaktion    TEXT NOT NULL,
            PRIMARY KEY (bewohner_id, stoff)
        );
        CREATE TABLE notfallhinweise (
            bewohner_id INTEGER PRIMARY KEY REFERENCES bewohner(id) ON DELETE CASCADE,
            text        TEXT NOT NULL,
            geaendert   TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        """,
        // 27 – Betreuer je Bewohner: jede Änderung eine neue Fassung, die neueste gilt.
        """
        CREATE TABLE betreuer (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            name        TEXT NOT NULL,
            stelle      TEXT NOT NULL DEFAULT '',
            email       TEXT NOT NULL DEFAULT '',
            telefon     TEXT NOT NULL DEFAULT '',
            geaendert   TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TRIGGER betreuer_bleiben BEFORE UPDATE ON betreuer
            BEGIN SELECT RAISE(ABORT, 'Betreuer-Fassungen werden nicht überschrieben.'); END;
        """,
        // 28 – Bedarfsgaben: jede Gabe mit Uhrzeit, Menge, Anlass, wer gab
        // und – bei einer Assistenzkraft – mit welcher Fachkraft abgesprochen; dazu der Bericht in der Akte.
        """
        CREATE TABLE bedarfsgaben (
            id            INTEGER PRIMARY KEY,
            verordnung_id INTEGER NOT NULL REFERENCES verordnungen(id) ON DELETE CASCADE,
            bewohner_id   INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            zeit          TEXT NOT NULL,
            menge         TEXT NOT NULL,
            anlass        TEXT NOT NULL,
            kuerzel       TEXT NOT NULL,
            absprache     TEXT,
            erstellt      TEXT NOT NULL,
            bericht_id    INTEGER REFERENCES berichte(id) ON DELETE CASCADE   -- kein SET NULL: das wäre ein UPDATE (Trigger)
        );
        CREATE INDEX bedarfsgaben_verordnung ON bedarfsgaben (verordnung_id, zeit);
        CREATE TRIGGER bedarfsgaben_bleiben BEFORE UPDATE ON bedarfsgaben
            BEGIN SELECT RAISE(ABORT, 'Bedarfsgaben werden nicht überschrieben.'); END;
        """,
        // 29 – Betreuer: dritter Zustand „keine gesetzliche Betreuung“, z. B. wenn eine Betreuung endet.
        // Ohne Fassung bleibt es „nicht erfasst“ (Regel 5: leer heißt nicht „keiner“).
        """
        ALTER TABLE betreuer ADD COLUMN keine INTEGER NOT NULL DEFAULT 0;
        """,
        // 30 – Personenverzeichnis: Die Ärzte-Tabelle nimmt alle Verzeichnisse auf (Feld „gruppe“, Namen je Verzeichnis
        // eindeutig); Zuordnung zu Bewohnern mit Rolle.
        """
        ALTER TABLE aerzte ADD COLUMN gruppe TEXT NOT NULL DEFAULT 'arzt'
            CHECK (gruppe IN ('arzt', 'betreuer', 'angehoerige', 'forensik', 'fosa', 'sonstige'));
        ALTER TABLE aerzte ADD COLUMN art TEXT NOT NULL DEFAULT '';
        ALTER TABLE aerzte ADD COLUMN mobil TEXT NOT NULL DEFAULT '';
        ALTER TABLE aerzte ADD COLUMN anschrift TEXT NOT NULL DEFAULT '';
        ALTER TABLE aerzte ADD COLUMN erreichbarkeit TEXT NOT NULL DEFAULT '';
        ALTER TABLE aerzte ADD COLUMN hinweis TEXT NOT NULL DEFAULT '';
        DROP INDEX aerzte_name;
        CREATE UNIQUE INDEX personen_name ON aerzte (gruppe, name COLLATE NOCASE);
        CREATE TABLE person_zuordnungen (
            id          INTEGER PRIMARY KEY,
            person_id   INTEGER NOT NULL REFERENCES aerzte(id) ON DELETE CASCADE,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            rolle       TEXT NOT NULL DEFAULT '',
            geaendert   TEXT NOT NULL,
            kuerzel     TEXT NOT NULL,
            UNIQUE (person_id, bewohner_id, rolle)
        );
        CREATE INDEX person_zuordnungen_bewohner ON person_zuordnungen (bewohner_id);
        """,
        // 31 – Berichtsart: Inhalt eines Berichts bleibt unveränderlich, nur die Art lässt sich setzen (Einordnen
        // vorhandener Berichte, Umbenennen einer Art in den Einstellungen).
        """
        ALTER TABLE berichte ADD COLUMN art TEXT NOT NULL DEFAULT 'Alltag';
        DROP TRIGGER berichte_bleiben;
        CREATE TRIGGER berichte_bleiben
            BEFORE UPDATE OF id, bewohner_id, zeitpunkt, schicht, verfasser, bereiche, text, wichtig, erfasst ON berichte
            BEGIN SELECT RAISE(ABORT, 'Berichte werden nicht überschrieben – Korrekturen sind Nachträge.'); END;
        CREATE TABLE berichtsarten (
            name  TEXT PRIMARY KEY,
            aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)),
            nr    INTEGER NOT NULL DEFAULT 0
        );
        INSERT INTO berichtsarten (name, nr) VALUES ('Alltag', 1), ('Krise', 2), ('Arztkontakt', 3), ('Visite', 4),
            ('Medikation', 5), ('Konflikt', 6), ('Ausgang/Abgängigkeit', 7), ('Angehörigenkontakt', 8),
            ('Betreuer/Behörde', 9), ('Besonderes Vorkommnis', 10);
        UPDATE berichte SET art = 'Visite' WHERE text LIKE 'Visite %';
        UPDATE berichte SET art = 'Medikation' WHERE id IN (SELECT bericht_id FROM bedarfsgaben WHERE bericht_id IS NOT NULL);
        """,
        // 32 – Aufgaben gehören zu einem Bewohner oder einem Bereich; Standardaufgaben je Bereich. Wo eine Aufgabe
        // angelegt wurde, ist nicht bekannt: Aufgaben ohne Bewohner kommen zu R1 (sonst zum ersten Wohnbereich),
        // Standardaufgaben gibt es danach in jedem Wohnbereich. Den Bereich darf nur das Umbenennen ändern.
        """
        ALTER TABLE aufgaben ADD COLUMN bereich TEXT NOT NULL DEFAULT '';
        ALTER TABLE aufgabe_vorlagen ADD COLUMN bereich TEXT NOT NULL DEFAULT '';
        DROP TRIGGER aufgaben_bleiben;
        CREATE TRIGGER aufgaben_bleiben
            BEFORE UPDATE OF id, bewohner_id, bericht_id, text, fuer, schicht_tag, schicht, faellig_bis, erstellt, verfasser,
                             vorlage_id, visite_anordnung_id, visite_eintrag_id ON aufgaben
            BEGIN SELECT RAISE(ABORT, 'Aufgaben werden nicht überschrieben – Änderungen sind Ereignisse.'); END;
        CREATE TEMP TABLE erster_bereich AS SELECT COALESCE(
            (SELECT name FROM bereiche WHERE name = 'R1'),
            (SELECT name FROM bereiche WHERE art IS NOT NULL AND aktiv = 1 ORDER BY nr, name LIMIT 1), 'R1') AS name;
        UPDATE aufgaben SET bereich = (SELECT name FROM erster_bereich) WHERE bewohner_id IS NULL;
        INSERT INTO aufgabe_vorlagen (text, schicht, tage, uhrzeit, bewohner_id, aktiv, erstellt, kuerzel, bereich)
            SELECT v.text, v.schicht, v.tage, v.uhrzeit, NULL, v.aktiv, v.erstellt, v.kuerzel, w.name
            FROM aufgabe_vorlagen v, bereiche w
            WHERE v.bewohner_id IS NULL AND w.art IS NOT NULL AND w.aktiv = 1 AND w.name <> (SELECT name FROM erster_bereich);
        UPDATE aufgabe_vorlagen SET bereich = (SELECT name FROM erster_bereich) WHERE bewohner_id IS NULL AND bereich = '';
        DROP TABLE erster_bereich;
        """,
        // 33 – Visite: Psychiater oder Hausarzt auch im Entwurf.
        """
        ALTER TABLE visite_entwuerfe ADD COLUMN art TEXT NOT NULL DEFAULT '';
        """,
        // 34 – Berichtsart „Therapie“ für Berichte aus dem Bereich Therapie.
        """
        INSERT INTO berichtsarten (name, nr) SELECT 'Therapie', (SELECT COALESCE(MAX(nr), 0) + 1 FROM berichtsarten)
            WHERE NOT EXISTS (SELECT 1 FROM berichtsarten WHERE name = 'Therapie');
        """,
        // 35 – Akte: Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger als Fassungen (Inhalt als JSON, nie
        // überschrieben); Auswahllisten für Krankenkassen, Kostenträger und Aufgabenbereiche der Betreuung.
        """
        CREATE TABLE IF NOT EXISTS akte_angaben (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            art         TEXT NOT NULL CHECK (art IN ('betreuung', 'angehoerige', 'aerzte', 'krankenkasse', 'kostentraeger', 'zustaendigkeit', 'bestellweg',
                                             'personenbeschreibung')),
            zustand     TEXT NOT NULL CHECK (zustand IN ('eingetragen', 'keine')),
            inhalt      TEXT NOT NULL DEFAULT '',
            ab          TEXT NOT NULL,
            geaendert   TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS akte_angaben_bewohner ON akte_angaben (bewohner_id, art);
        CREATE TRIGGER IF NOT EXISTS akte_angaben_bleiben BEFORE UPDATE ON akte_angaben
            BEGIN SELECT RAISE(ABORT, 'Angaben der Akte werden nicht überschrieben – jede Änderung ist eine neue Fassung.'); END;
        CREATE TABLE IF NOT EXISTS krankenkassen (name TEXT PRIMARY KEY, aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)), nr INTEGER NOT NULL DEFAULT 0);
        CREATE TABLE IF NOT EXISTS kostentraeger (name TEXT PRIMARY KEY, aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)), nr INTEGER NOT NULL DEFAULT 0);
        CREATE TABLE IF NOT EXISTS betreuungsaufgaben (name TEXT PRIMARY KEY, aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)), nr INTEGER NOT NULL DEFAULT 0);
        INSERT OR IGNORE INTO betreuungsaufgaben (name, nr) VALUES ('Gesundheitssorge', 1), ('Aufenthaltsbestimmung', 2), ('Vermögenssorge', 3),
            ('Behörden und Ämter', 4), ('Wohnungsangelegenheiten', 5), ('Post', 6), ('Unterbringung', 7);
        """,
        // 36 – Akte, Reiter „Dokumente“: Angaben je Datei (die Datei liegt in der Ablage neben der Datenbank), Ereignisse
        // (hinzugefügt, geöffnet, Papierkorb …) statt Änderungen; Kategorien als Liste.
        """
        CREATE TABLE IF NOT EXISTS dokumente (
            id           INTEGER PRIMARY KEY,
            bewohner_id  INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            kategorie    TEXT NOT NULL,
            titel        TEXT NOT NULL,
            datum        TEXT NOT NULL,
            gueltig_bis  TEXT,
            hinweis      TEXT NOT NULL DEFAULT '',
            datei        TEXT NOT NULL,
            original     TEXT NOT NULL,
            groesse      INTEGER NOT NULL,
            pruefsumme   TEXT NOT NULL,
            hinzugefuegt TEXT NOT NULL,
            kuerzel      TEXT NOT NULL,
            vorgaenger   INTEGER REFERENCES dokumente(id)
        );
        CREATE INDEX IF NOT EXISTS dokumente_bewohner ON dokumente (bewohner_id);
        CREATE TRIGGER IF NOT EXISTS dokumente_bleiben BEFORE UPDATE ON dokumente
            BEGIN SELECT RAISE(ABORT, 'Dokumente werden nicht überschrieben – Änderungen sind Ereignisse.'); END;
        CREATE TABLE IF NOT EXISTS dokument_ereignisse (
            id          INTEGER PRIMARY KEY,
            dokument_id INTEGER NOT NULL REFERENCES dokumente(id) ON DELETE CASCADE,
            aktion      TEXT NOT NULL,
            zeit        TEXT NOT NULL,
            kuerzel     TEXT NOT NULL,
            grund       TEXT NOT NULL DEFAULT ''
        );
        CREATE INDEX IF NOT EXISTS dokument_ereignisse_dokument ON dokument_ereignisse (dokument_id);
        CREATE TABLE IF NOT EXISTS dokumentkategorien (name TEXT PRIMARY KEY, aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)), nr INTEGER NOT NULL DEFAULT 0);
        INSERT OR IGNORE INTO dokumentkategorien (name, nr) VALUES ('Aufnahme und Verträge', 1), ('Betreuung und Gericht', 2),
            ('Kostenträger und Bescheide', 3), ('Krankenkasse', 4), ('Ärztliches und Befunde', 5), ('Ausweise und Behörden', 6),
            ('Schriftverkehr', 7), ('Sonstiges', 8);
        """,
        // 37 – Abgängigkeitsmeldung: Meldung (Inhalt als JSON), danach nur Ereignisse (informiert, zurück, entwarnt); Stellen als
        // Liste. Die Berichte bekommen die Art „Ausgang/Abgängigkeit“ (gibt es seit Schritt 19 in der Liste).
        """
        CREATE TABLE IF NOT EXISTS abgaengigkeiten (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            meldung     TEXT NOT NULL,
            gemeldet    TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        CREATE TRIGGER IF NOT EXISTS abgaengigkeiten_bleiben BEFORE UPDATE ON abgaengigkeiten
            BEGIN SELECT RAISE(ABORT, 'Abgängigkeitsmeldungen werden nicht überschrieben.'); END;
        CREATE TABLE IF NOT EXISTS abgaengigkeit_ereignisse (
            id                INTEGER PRIMARY KEY,
            abgaengigkeit_id  INTEGER NOT NULL REFERENCES abgaengigkeiten(id) ON DELETE CASCADE,
            art               TEXT NOT NULL CHECK (art IN ('informiert', 'zurueck', 'entwarnt')),
            stelle            TEXT NOT NULL DEFAULT '',
            zeit              TEXT NOT NULL,
            kuerzel           TEXT NOT NULL,
            hinweis           TEXT NOT NULL DEFAULT ''
        );
        CREATE TABLE IF NOT EXISTS meldestellen (name TEXT PRIMARY KEY, aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)), nr INTEGER NOT NULL DEFAULT 0);
        INSERT OR IGNORE INTO meldestellen (name, nr) VALUES ('Leitung bzw. Rufbereitschaft', 1), ('Polizei (110)', 2), ('Betreuer', 3),
            ('Angehörige', 4), ('Klinik', 5), ('Gericht', 6);
        """,
        // 38 – Angaben der Akte für weitere Reiter (Schweigepflicht und später): die Liste der Arten nicht mehr in der Tabelle
        // festschreiben (SQLite ändert keine CHECK-Regel – die Tabelle wird neu angelegt und umkopiert, Fassungen bleiben).
        """
        DROP TABLE IF EXISTS akte_angaben_neu;
        CREATE TABLE akte_angaben_neu (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            art         TEXT NOT NULL CHECK (length(art) > 0),
            zustand     TEXT NOT NULL CHECK (zustand IN ('eingetragen', 'keine')),
            inhalt      TEXT NOT NULL DEFAULT '',
            ab          TEXT NOT NULL,
            geaendert   TEXT NOT NULL,
            kuerzel     TEXT NOT NULL
        );
        INSERT INTO akte_angaben_neu (id, bewohner_id, art, zustand, inhalt, ab, geaendert, kuerzel)
            SELECT id, bewohner_id, art, zustand, inhalt, ab, geaendert, kuerzel FROM akte_angaben;
        DROP TABLE akte_angaben;
        ALTER TABLE akte_angaben_neu RENAME TO akte_angaben;
        CREATE INDEX IF NOT EXISTS akte_angaben_bewohner ON akte_angaben (bewohner_id, art);
        CREATE TRIGGER IF NOT EXISTS akte_angaben_bleiben BEFORE UPDATE ON akte_angaben
            BEGIN SELECT RAISE(ABORT, 'Angaben der Akte werden nicht überschrieben – jede Änderung ist eine neue Fassung.'); END;
        """,
        // 39 – Vitalwerte: Werte (nie überschrieben; Korrektur = neuer Eintrag mit Vorgänger und Grund), angeordnete Bereiche,
        // Messaufträge (je Auftrag und Schicht höchstens eine Aufgabe), die Liste der Werte.
        """
        CREATE TABLE IF NOT EXISTS vitalarten (name TEXT PRIMARY KEY, aktiv INTEGER NOT NULL DEFAULT 1 CHECK (aktiv IN (0, 1)), nr INTEGER NOT NULL DEFAULT 0);
        INSERT OR IGNORE INTO vitalarten (name, nr) VALUES ('Blutdruck', 1), ('Puls', 2), ('Temperatur', 3), ('Gewicht', 4), ('Blutzucker', 5),
            ('Sauerstoffsättigung', 6);
        CREATE TABLE IF NOT EXISTS vitalwerte (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            zeit        TEXT NOT NULL,
            art         TEXT NOT NULL,
            wert        REAL NOT NULL,
            wert2       REAL,
            bemerkung   TEXT NOT NULL DEFAULT '',
            kuerzel     TEXT NOT NULL,
            erfasst     TEXT NOT NULL,
            vorgaenger  INTEGER REFERENCES vitalwerte(id),
            grund       TEXT NOT NULL DEFAULT ''
        );
        CREATE INDEX IF NOT EXISTS vitalwerte_bewohner ON vitalwerte (bewohner_id, zeit);
        CREATE TRIGGER IF NOT EXISTS vitalwerte_bleiben BEFORE UPDATE ON vitalwerte
            BEGIN SELECT RAISE(ABORT, 'Vitalwerte werden nicht überschrieben – eine Korrektur ist ein neuer Eintrag mit Grund.'); END;
        CREATE TABLE IF NOT EXISTS vital_grenzen (
            id            INTEGER PRIMARY KEY,
            bewohner_id   INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            art           TEXT NOT NULL,
            von REAL, bis REAL, von2 REAL, bis2 REAL,
            arzt_id       INTEGER REFERENCES aerzte(id) ON DELETE SET NULL,
            angeordnet_am TEXT NOT NULL,
            kuerzel       TEXT NOT NULL,
            erfasst       TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS messauftraege (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            art         TEXT NOT NULL,
            rhythmus    TEXT NOT NULL CHECK (rhythmus IN ('taeglich', 'woechentlich', 'monatlich')),
            tag         INTEGER NOT NULL DEFAULT 0,
            schicht     TEXT NOT NULL CHECK (schicht IN ('frueh', 'spaet', 'nacht')),
            ab          TEXT NOT NULL,
            beendet     TEXT,
            kuerzel     TEXT NOT NULL,
            erfasst     TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS messauftrag_aufgaben (
            messauftrag_id INTEGER NOT NULL REFERENCES messauftraege(id) ON DELETE CASCADE,
            schicht_tag    TEXT NOT NULL,
            schicht        TEXT NOT NULL,
            PRIMARY KEY (messauftrag_id, schicht_tag, schicht)
        );
        """,
        // 40 – Betriebsmodus: Passwörter (nur Hash), Zugriffsprotokoll, Häuser mit ihren Bereichen, Haus der Haus-Termine.
        """
        CREATE TABLE IF NOT EXISTS passwoerter (
            kuerzel       TEXT PRIMARY KEY COLLATE NOCASE,
            hash          TEXT NOT NULL,
            salz          TEXT NOT NULL,
            iterationen   INTEGER NOT NULL,
            einmal        INTEGER NOT NULL DEFAULT 1 CHECK (einmal IN (0, 1)),
            fehlversuche  INTEGER NOT NULL DEFAULT 0,
            gesperrt      INTEGER NOT NULL DEFAULT 0 CHECK (gesperrt IN (0, 1)),
            geaendert     TEXT NOT NULL,
            geaendert_von TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS zugriffe (
            id          INTEGER PRIMARY KEY,
            zeit        TEXT NOT NULL,
            kuerzel     TEXT NOT NULL,
            bewohner_id INTEGER,
            aktion      TEXT NOT NULL,
            was         TEXT NOT NULL DEFAULT ''
        );
        CREATE INDEX IF NOT EXISTS zugriffe_zeit ON zugriffe (zeit);
        CREATE TRIGGER IF NOT EXISTS zugriffe_bleiben BEFORE UPDATE ON zugriffe
            BEGIN SELECT RAISE(ABORT, 'Das Zugriffsprotokoll wird nicht geändert.'); END;
        CREATE TABLE IF NOT EXISTS haeuser (name TEXT PRIMARY KEY, nr INTEGER NOT NULL DEFAULT 0);
        CREATE TABLE IF NOT EXISTS bereich_haeuser (
            bereich TEXT PRIMARY KEY REFERENCES bereiche(name) ON UPDATE CASCADE ON DELETE CASCADE,
            haus    TEXT NOT NULL REFERENCES haeuser(name) ON UPDATE CASCADE ON DELETE CASCADE
        );
        CREATE TABLE IF NOT EXISTS kalender_haeuser (
            termin_id INTEGER PRIMARY KEY REFERENCES kalender(id) ON DELETE CASCADE,
            haus      TEXT NOT NULL REFERENCES haeuser(name) ON UPDATE CASCADE ON DELETE CASCADE
        );
        """,
    ];

    public static int AktuellerStand => Schritte.Length;

    private const string Spalten =
        "vorname, nachname, zimmer, geburtsdatum, aufnahmedatum, allergie, allergien, beschluss, beschlussende, bild, offen_gefuehrt, wohnbereich, bett";

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
            GrundbestandPruefen();
            PersonenPruefen();
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
            // Mehrplatz: Hat ein anderer Arbeitsplatz den Schritt gerade gemacht, nicht noch einmal.
            if (Convert.ToInt32(WertIn(schritt, "PRAGMA user_version"), CultureInfo.InvariantCulture) > nummer) continue;
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
    // ausgangsfassung: der Bewohner, wie er beim Öffnen des Formulars war. Hat ein anderer
    // Arbeitsplatz ihn inzwischen geändert, gibt es einen Konflikt statt stillem Überschreiben.
    public Bewohner BewohnerSpeichern(Bewohner b, string? neuesBild = null, bool bildEntfernen = false, Bewohner? ausgangsfassung = null)
    {
        var fehler = BewohnerRegeln.Pruefen(b);
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        b = BettPruefen(b);   // Bett muss es geben und frei sein
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
                // In der Transaktion lesen: Sie sperrt die Datei, bis gespeichert ist.
                if (ausgangsfassung is not null &&
                    LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner WHERE id = $id", ("$id", b.Id)).FirstOrDefault() is Bewohner jetzt &&
                    jetzt != ausgangsfassung && jetzt != b)
                    throw new Konflikt(Konflikt.Text, BewohnerRegeln.Beschreibung(jetzt), BewohnerRegeln.Beschreibung(b));
                string setzen = string.Join(", ", Spalten.Split(", ").Select(s => $"{s} = ${s}"));
                Ausfuehren($"UPDATE bewohner SET {setzen} WHERE id = $id", transaktion, [.. Werte(b), ("$id", b.Id)]);
            }
            transaktion.Commit();
        }
        catch (Exception e) when (e is SqliteException or Konflikt)
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
        if (Betrieb.Aktiv || IstBetriebsdatenbank) return false;
        if (Meta(DemoGeladen) is null)
        {
            DemoZuruecksetzen();
            return true;
        }
        // Wohnbereiche R1–R3 mit Zimmern und Betten ersetzen „Wohnbereich A/B“ – das betrifft
        // fast alle Demo-Daten. Eine ältere Demo-Datenbank wird deshalb einmal neu geladen; vorher sichert
        // das Programm sie als Kopie neben der Datenbank.
        if (Meta(DemoZimmer) is null)
        {
            string sicherung = SicherungAnlegen("vor-0.29.7");
            DemoZuruecksetzen();
            Startprotokoll.Add($"Demo-Daten auf die Bereiche R1–R3 mit Zimmern und Betten umgestellt; die bisherige Datenbank liegt als „{Path.GetFileName(sicherung)}“ daneben.");
            return true;
        }
        // 23 Bewohner je Wohnbereich. Neue Bewohner betreffen Zimmer, Bezugspflege, Medikation,
        // Therapien und Berichte zugleich – deshalb auch hier einmal neu laden, vorher mit Sicherung.
        if (Meta(DemoVoll) is null)
        {
            string sicherung = SicherungAnlegen("vor-0.29.10");
            DemoZuruecksetzen();
            Startprotokoll.Add($"Demo-Daten auf 23 Bewohner je Wohnbereich erweitert; die bisherige Datenbank liegt als „{Path.GetFileName(sicherung)}“ daneben.");
            return true;
        }
        // Nach einem Update: Bereiche, die es beim ersten Laden noch nicht gab, nachholen.
        bool berichte = Meta(DemoBerichte) is null, aufgaben = Meta(DemoAufgaben) is null, nachrichten = Meta(DemoNachrichten) is null,
             medikamente = Meta(DemoMedikamente) is null, medikation = Meta(DemoMedikation) is null,
             therapie = Meta(DemoTherapie) is null || Meta(DemoTherapieAnbieter) is null || Meta(DemoTherapieTag) is null, wochenplan = Meta(DemoWochenplan) is null,
             kalender = Meta(DemoKalender) is null || Meta(DemoKalenderHaus) is null, fahrten = Meta(DemoFahrten) is null,
             standard = Meta(DemoStandardaufgaben) is null, visiten = Meta(DemoVisiten) is null, aerzte = Meta(DemoAerzte) is null,
             aerztlich = Meta(DemoAerztlich) is null, betreuer = Meta(DemoBetreuer) is null, personen = Meta(DemoPersonen) is null, bedarf = Meta(DemoBedarf) is null,
             biografie = Meta(DemoBiografie) is null, reflexion = Meta(DemoReflexion) is null, heb = Meta(DemoHeb) is null,
             status = Meta(DemoStatus) is null, mitarbeiter = Meta(DemoMitarbeiterSchluessel) is null, bezug = Meta(DemoBezug) is null,
             akte = Meta(DemoAkte) is null, dokumente = Meta(DemoDokumente) is null, schweigepflicht = Meta(DemoSchweigepflicht) is null,
             vital = Meta(DemoVital) is null;
        var neueModule = ModuleOhneDemo();
        bool modulNeu = neueModule.Count > 0;
        if (!berichte && !aufgaben && !nachrichten && !medikamente && !medikation && !therapie && !wochenplan && !kalender && !biografie && !reflexion && !heb && !status && !mitarbeiter && !bezug && !fahrten && !standard && !visiten && !aerzte && !aerztlich && !betreuer && !personen && !bedarf && !akte && !dokumente && !schweigepflicht && !vital && !modulNeu)
            return false;
        // Vorher eine Kopie: Manche Teile ersetzen vorhandene Einträge (z. B. die Therapie-Angebote).
        string kopie = SicherungAnlegen("vor-demo-nachholen");
        Startprotokoll.Add($"Demo-Daten nach dem Update ergänzt; die bisherige Datenbank liegt als „{Path.GetFileName(kopie)}“ daneben.");
        using var transaktion = db.BeginTransaction();
        if (berichte) DemoBerichteEinfuegen(transaktion);
        if (aufgaben) DemoAufgabenEinfuegen(transaktion);
        if (standard) DemoStandardaufgabenEinfuegen(transaktion);
        if (mitarbeiter) DemoMitarbeiterEinfuegen(transaktion);
        if (nachrichten) DemoNachrichtenEinfuegen(transaktion);
        if (medikamente) DemoMedikamenteEinfuegen(transaktion);
        if (medikamente || medikation) DemoMedikationEinfuegen(transaktion);   // hängt am Medikamenten-Bestand
        if (therapie) DemoTherapieEinfuegen(transaktion);
        if (therapie || wochenplan) DemoWochenplanEinfuegen(transaktion);   // Teilnahmen hängen an den Zuteilungen
        if (kalender) DemoKalenderEinfuegen(transaktion);
        if (fahrten) DemoFahrtenEinfuegen(transaktion);
        if (aerzte) DemoAerzteEinfuegen(transaktion);
        if (aerztlich) DemoAerztlichEinfuegen(transaktion);
        if (visiten) DemoVisitenEinfuegen(transaktion);
        if (betreuer) DemoBetreuerEinfuegen(transaktion);
        if (personen || betreuer) DemoPersonenEinfuegen(transaktion);   // übernimmt auch die Betreuer
        if (akte || personen || betreuer || aerzte) DemoAkteAngabenEinfuegen(transaktion);   // hängt an Personen und Betreuern
        if (schweigepflicht || akte || personen || betreuer || aerzte) DemoSchweigepflichtEinfuegen(transaktion);   // hängt an den Angaben der Akte
        if (vital || aerzte) DemoVitalEinfuegen(transaktion);   // die Grenzen nennen einen Arzt
        if (dokumente) DemoDokumenteEinfuegen(transaktion);
        if (bedarf || medikamente || medikation) DemoBedarfEinfuegen(transaktion);   // hängt an den Bedarfsverordnungen
        if (biografie) DemoBiografieEinfuegen(transaktion);
        if (reflexion) DemoReflexionEinfuegen(transaktion);
        if (heb) DemoHebEinfuegen(transaktion);
        if (status) DemoStatusEinfuegen(transaktion);
        if (bezug || mitarbeiter) DemoBezugEinfuegen(transaktion);
        DemoModuleEinfuegen(transaktion, neueModule);
        transaktion.Commit();
        if (dokumente) AblageAufraeumen();
        if (modulNeu) ModuleGeaendert?.Invoke();
        return true;
    }

    // Eine Kopie der Datenbank neben ihr (VACUUM INTO: auch während sie offen ist, in sich stimmig).
    public string SicherungAnlegen(string zusatz)
    {
        string ordner = Path.GetDirectoryName(Pfad)!, name = Path.GetFileNameWithoutExtension(Pfad);
        string ziel = Path.Combine(ordner, $"{name}-{zusatz}.db");
        for (int i = 2; File.Exists(ziel); i++) ziel = Path.Combine(ordner, $"{name}-{zusatz}-{i}.db");
        Ausfuehren("VACUUM INTO $ziel", null, ("$ziel", ziel));
        return ziel;
    }

    // Alle Bewohner mit allem, was dazugehört (Bilder, Berichte …), weg und die Demo-Daten aller
    // Bereiche neu laden – alles oder nichts.
    public void DemoZuruecksetzen()
    {
        if (Betrieb.Aktiv || IstBetriebsdatenbank) throw new SpeicherFehler(Betrieb.DemoVerbotenText);
        var demo = Demodaten.Bewohner();   // erst lesen: Ist etwas kaputt, bleibt alles, wie es war
        var alteBilder = AlleBewohner().Select(b => b.Bild).Where(b => b is not null).ToList();
        using (var transaktion = db.BeginTransaction())
        {
            Ausfuehren("DELETE FROM bewohner", transaktion);   // Berichte und Nachträge folgen (CASCADE)
            foreach (var b in demo)
                Ausfuehren($"INSERT INTO bewohner ({Spalten}) VALUES ({Platzhalter})", transaktion, Werte(b));
            SetzeMeta(DemoGeladen, "1", transaktion);
            SetzeMeta(DemoVoll, "1", transaktion);
            DemoBerichteEinfuegen(transaktion);
            DemoAufgabenEinfuegen(transaktion);
            DemoStandardaufgabenEinfuegen(transaktion);
            DemoMitarbeiterEinfuegen(transaktion);
            DemoZimmerEinfuegen(transaktion);   // nach den Bereichen
            DemoNachrichtenEinfuegen(transaktion);
            DemoMedikamenteEinfuegen(transaktion);
            DemoMedikationEinfuegen(transaktion);
            DemoTherapieEinfuegen(transaktion);
            DemoWochenplanEinfuegen(transaktion);
            DemoKalenderEinfuegen(transaktion);
            DemoFahrtenEinfuegen(transaktion);
            DemoAerzteEinfuegen(transaktion);
            DemoAerztlichEinfuegen(transaktion);
            DemoVisitenEinfuegen(transaktion);
            DemoBetreuerEinfuegen(transaktion);
            DemoPersonenEinfuegen(transaktion);
            DemoAkteAngabenEinfuegen(transaktion);
            DemoSchweigepflichtEinfuegen(transaktion);
            DemoVitalEinfuegen(transaktion);
            DemoDokumenteEinfuegen(transaktion);
            DemoBedarfEinfuegen(transaktion);
            DemoBiografieEinfuegen(transaktion);
            DemoReflexionEinfuegen(transaktion);
            DemoHebEinfuegen(transaktion);
            DemoStatusEinfuegen(transaktion);
            DemoBezugEinfuegen(transaktion);
            DemoModuleEinfuegen(transaktion, module);
            transaktion.Commit();
        }
        foreach (var bild in alteBilder) Bilder.Loeschen(bild);
        AblageAufraeumen();
        AufgabenGeaendert?.Invoke();
        NachrichtenGeaendert?.Invoke();
        MedikationGeaendert?.Invoke();
        TherapieGeaendert?.Invoke();
        WochenplanGeaendert?.Invoke();
        KalenderGeaendert?.Invoke();
        StatusGeaendert?.Invoke();
        MitarbeiterGeaendert?.Invoke();
        BezugGeaendert?.Invoke();
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
            // Die Art aus dem Text raten, außer sie steht schon fest (Berichte der Therapie).
            string art = d.Bericht.Art == DokuRegeln.ArtTherapie ? d.Bericht.Art : DokuRegeln.ArtFuerDemo(d.Bericht.Text);
            long id = BerichtEinfuegen(d.Bericht with { Art = art }, transaktion);
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
        Zugriff(b.Verfasser, b.BewohnerId, ZugriffRegeln.Geaendert, $"Bericht ({b.Art})");
        return BerichtLesen(id)!;
    }

    private long BerichtEinfuegen(Bericht b, SqliteTransaction transaktion)
    {
        Ausfuehren("INSERT INTO berichte (bewohner_id, zeitpunkt, schicht, verfasser, bereiche, text, wichtig, erfasst, art) " +
                   "VALUES ($b, $z, $s, $v, $bereiche, $t, $w, $e, $art)", transaktion,
            ("$b", b.BewohnerId), ("$z", Zeit(b.Zeitpunkt)), ("$s", BerichtRegeln.Kuerzel(b.Schicht)), ("$v", b.Verfasser),
            ("$bereiche", string.Join(",", b.Bereiche)), ("$t", b.Text), ("$w", b.Wichtig ? 1 : 0), ("$e", Zeit(b.Erfasst)),
            ("$art", b.Art));
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

    // Bereich „Dokumentation“: die Berichte eines Bewohners nach dem Filter, neueste zuerst.
    public List<Bericht> Berichte(long bewohnerId, DokuFilter filter, DateOnly heute) =>
        BerichteLesen("WHERE bewohner_id = $b", ("$b", bewohnerId))
            .Where(b => DokuRegeln.Passt(b, filter, heute))
            .OrderByDescending(b => b.Zeitpunkt).ThenByDescending(b => b.Id)
            .ToList();

    // Gespeicherte Filter je Mitarbeiter (in „meta“, damit sie an jedem Arbeitsplatz gelten).
    public List<GespeicherterFilter> GespeicherteFilter(string kuerzel) =>
        DokuRegeln.AusJson(Meta(FilterSchluessel(kuerzel)));

    public void FilterSpeichern(string kuerzel, string name, DokuFilter filter)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        name = name.Trim();
        if (name.Length is 0 or > 40) throw new Eingabefehler(["Name des Filters: 1 bis 40 Zeichen."]);
        var liste = GespeicherteFilter(kuerzel).Where(f => !f.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)).ToList();
        liste.Add(new GespeicherterFilter(name, filter));
        SetzeMeta(FilterSchluessel(kuerzel), DokuRegeln.AlsJson(liste));
    }

    public void FilterLoeschen(string kuerzel, string name) =>
        SetzeMeta(FilterSchluessel(kuerzel), DokuRegeln.AlsJson(GespeicherteFilter(kuerzel).Where(f => f.Name != name)));

    private static string FilterSchluessel(string kuerzel) => $"doku_filter:{kuerzel.Trim().ToUpperInvariant()}";

    // Die aktiven Berichtsarten in ihrer Reihenfolge (Auswahl beim Schreiben, Filter).
    public List<string> Berichtsarten() => [.. Liste_(Liste.Berichtsarten).Where(e => e.Aktiv).Select(e => e.Name)];

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
                                   "EXISTS (SELECT 1 FROM uebergaben u WHERE u.bericht_id = berichte.id), art, " +
                                   $"EXISTS (SELECT 1 FROM bedarfsgaben g WHERE g.bericht_id = berichte.id) FROM berichte {bedingung}",
                                   null, parameter))
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
                    Art = leser.GetString(10),
                    Bedarfsgabe = leser.GetInt64(11) != 0,
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
        ("$offen_gefuehrt", BewohnerRegeln.Kuerzel(b.OffenGefuehrt)),
        ("$wohnbereich", b.Wohnbereich),
        ("$bett", b.Bett),
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
                OffenGefuehrt = BewohnerRegeln.OffenGefuehrtAus(leser.GetString(11)) ?? OffenGefuehrtZustand.NichtErfasst,
                Wohnbereich = leser.GetString(12),
                Bett = leser.GetString(13),
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
