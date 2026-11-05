# STAND — Dokumentationssystem

Stand: 05.11.2026. Version: **0.1.0** (Programmrahmen und Selbst-Update).

## Aktuell

Das Programm läuft als Windows-Programm (`.exe`) mit Hauptfenster, Datenordner,
SQLite-Speicher, Versionsanzeige und Selbst-Update. Der Bereich „Bewohner“ ist noch ein
Platzhalter.

**Nächster Schritt:** „Bewohnerakte“ (in `IDEEN.md` freigegeben).

## Fertig

- Repository `KhaosDE/Dokumentationssystem` (öffentlich, wegen der Update-Funktion).
- `CLAUDE.md`, `IDEEN.md`, `STAND.md`, `SITZUNGEN.md`, `README.md`.
- 0.1.0 — Programmrahmen und Selbst-Update:
  - Hauptfenster: oben dauerhaft „Demo — ausgedachte Daten“, links Navigation (zunächst
    nur „Bewohner“), unten links Versionsnummer und Häkchen „Beim Start nach Updates
    suchen“.
  - Datenordner `%APPDATA%\Dokumentationssystem` mit `dokusystem.sqlite3`,
    `einstellungen.json`, `update.log`.
  - Selbst-Update: fragt beim Start nach dem neuesten Release, zeigt „Neue Version wird
    geladen …“, prüft die SHA-256-Prüfsumme, tauscht die `.exe` aus, startet neu. Jeder
    Schritt steht in `update.log`. Abschaltbar (Häkchen oder `--kein-update`).
  - GitHub Actions: Tests unter Windows, `.exe` bauen, Selbsttest der `.exe`, Release
    `build-<Laufnummer>` bei Push nach `main` (nicht bei reinen `.md`-Änderungen).
  - README: Herunterladen, SmartScreen-Warnung, Update abschalten.

## Offene Punkte

- Bewohnerakte: Vorschläge Beschlusswarnung, „offen geführt“, Altersanzeige — Tobi
  entscheidet.
- Tägliche Dokumentation: Inhalt wird im Chat ausgearbeitet.
- PySide6 steht unter LGPLv3 — vor einem späteren Verkauf prüfen (siehe `CLAUDE.md`).

## Entscheidungen

- 05.11.2026 (Tobi): Demo mit ausgedachten Daten, zu Präsentationszwecken.
- 05.11.2026 (Tobi): Alle Werkzeuge des Wohnbereichs verschmelzen zu einem Programm; Kern
  sind Bewohnerführung und tägliche Dokumentation.
- 05.11.2026 (Tobi): Repository öffentlich, damit eine Update-Funktion gebaut und erprobt
  werden kann.
- 05.11.2026 (Tobi): Windows-Programm. Ideen werden im Chat besprochen, landen in
  `IDEEN.md`, Claude Code setzt sie um.
- 05.11.2026 (Chat): Python + PySide6 + SQLite, Build und Selbst-Update nach Vorbild
  N3XTBYT3. Grund: Formulare und Listen gehen mit Qt gut, Python und der Update-Weg sind
  aus N3XTBYT3 schon erprobt.
- 05.11.2026 (Claude Code): **Zwei Nummern.** Die Versionsnummer (z. B. 0.1.0) steht von
  Hand in `dokusystem/_version.py` und in dieser Datei; die Build-Nummer ist die
  Laufnummer von GitHub Actions. Das Update vergleicht nur Build-Nummern. Grund: Die
  Versionsnummer muss schon vor dem Build in `STAND.md` stehen, die Laufnummer kennt man
  erst danach. Im Fenster steht beides („Version 0.1.0 · Build 7“).
- 05.11.2026 (Claude Code): **Update-Einstellung** als Häkchen unten links im Hauptfenster,
  gespeichert in `einstellungen.json` (nicht in der Datenbank). Grund: Die Update-Prüfung
  läuft vor dem Öffnen der Datenbank, und die Datei lässt sich notfalls von Hand ändern.
- 05.11.2026 (Claude Code): `update.log` wird fortgeschrieben (ein Eintrag pro Schritt,
  die letzten 300 Zeilen bleiben), damit man mehrere Starts nachvollziehen kann.
- 05.11.2026 (Claude Code): Der Workflow läuft auch bei Pull Requests (Tests, `.exe`,
  Selbsttest – ohne Release). Grund: Vor dem Zusammenführen muss feststehen, dass der
  Windows-Build grün ist (`CLAUDE.md`, „Arbeitsweise“). Tests laufen nur unter Windows,
  dem Zielsystem.
- 05.11.2026 (Claude Code): Zusätzliche Abhängigkeit nur zum Bauen: PyInstaller (macht
  die `.exe`, wie in N3XTBYT3). Im Programm selbst nur PySide6.

## Testliste für Tobi

Version 0.1.0:

1. `.exe` über den Link im README laden, in „Dokumente“ legen, starten. SmartScreen-Warnung
   wie im README beschrieben wegklicken.
2. Oben steht orange „Demo — ausgedachte Daten“, links „Bewohner“, unten links
   „Version 0.1.0 · Build …“.
3. Update erproben: Sobald eine neuere Version erschienen ist, beim nächsten Start auf die
   Meldung „Neue Version wird geladen …“ achten. Danach startet das Programm neu, unten
   links steht die neue Nummer. In `%APPDATA%\Dokumentationssystem\update.log` stehen die
   Schritte.
4. Häkchen „Beim Start nach Updates suchen“ entfernen, Programm neu starten: In
   `update.log` steht „in den Einstellungen abgeschaltet“.
