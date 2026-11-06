# STAND — Dokumentationssystem

Stand: 05.11.2026. Version: **0.2.0** (Bewohnerakte).

## Aktuell

Das Programm läuft als Windows-Programm (`.exe`) mit Selbst-Update. Der Bereich
„Bewohner“ zeigt alle ausgedachten Demo-Bewohner als Kacheln; jede Akte hat die
wichtigsten Angaben auf einen Blick, Allergiehinweis oben.

**Nächster Schritt (06.11.2026):** Freigegeben in `IDEEN.md`: „Umstieg auf C# / .NET 10“
mit „Gestaltung nach Vorbild der Ausgangsliste“ → Version 0.3.0. Für die Sitzung das
Repository `KhaosDE/Ausgangsliste` zusätzlich anhängen (Bausteine, siehe `CLAUDE.md`).

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
- 0.2.0 — Bewohnerakte:
  - Übersicht: alle Bewohner als Kacheln mit Bild (sonst Initialen), Name und Zimmer,
    sortiert nach Nachname. Klick öffnet die Akte.
  - Akte: Name, Zimmer, Bild, Geburtsdatum, Aufnahmedatum, Beschlussende. Allergiehinweis
    oben, je Zustand deutlich anders: rot (eingetragen), grün (keine bekannt), orange
    gestrichelt (nicht erfasst – bitte klären).
  - Formular „Neuer Bewohner …“ / „Bearbeiten …“ mit Prüfung: Pflichtfelder, Datum
    TT.MM.JJJJ, Aufnahmedatum nicht vor dem Geburtsdatum. Neue Bewohner stehen bei
    Allergien und Beschluss auf „nicht erfasst“.
  - Bewohnerbild per Dateiauswahl, Ablage im Datenordner (`bilder\`).
  - 10 ausgedachte Demo-Bewohner (`daten/demo/bewohner.json`), gemischt bei Allergien und
    Beschluss, ohne Bilder. Werden beim ersten Start geladen; Knopf „Demo-Daten
    zurücksetzen …“ unten links.
  - Behoben: Absturz beim Beenden, der mit den neuen Fenstern auftreten konnte (Fenster
    werden jetzt am Programmende ausdrücklich abgebaut).

## Offene Punkte

- Bewohnerakte: Vorschläge Beschlusswarnung, „offen geführt“, Altersanzeige — Tobi
  entscheidet.
- Bewohner löschen oder als ausgezogen markieren gibt es noch nicht (war nicht
  beauftragt). Zum Aufräumen in der Demo: „Demo-Daten zurücksetzen“.
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
  aus N3XTBYT3 schon erprobt. **Abgelöst am 06.11.2026** (nächster Punkt).
- 06.11.2026 (Tobi): Umstieg auf C# / .NET 10 mit Windows Forms. Grund: Die Ausgangsliste
  ist bereits ein C#-Programm mit vielen passenden Bausteinen (Kartenraster, Mehrplatz-
  Datenbank, Druck, Installer); ein späterer Server geht in derselben Sprache; freie
  Lizenzen. .NET 10 statt 8, weil .NET 8 ab 10.11.2026 keine Updates mehr bekommt.
- 06.11.2026 (Tobi): Keine KI und keine Internetverbindung außer dem Update (Datenschutz).
- 06.11.2026 (Tobi): Gestaltung nach Vorbild der Ausgangsliste (`docs/gestaltung.md`).
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
- 05.11.2026 (Claude Code): **Name als Vorname und Nachname** (zwei Felder). Grund:
  Sortieren nach Nachname und Initialen im Platzhalter gehen damit ohne Raten.
- 05.11.2026 (Claude Code): **Beschlussende mit drei Zuständen** wie die Allergien:
  Datum / „kein Beschluss“ / „nicht erfasst“. Grund: Ein leeres Feld könnte sonst wie
  „kein Beschluss“ gelesen werden (Gedanke von Regel 5). Eine Warnung vor dem Ende ist
  ausdrücklich nicht gebaut.
- 05.11.2026 (Claude Code): Pflichtfelder sind Vorname, Nachname, Zimmer, Geburtsdatum,
  Aufnahmedatum. Datum darf auch ohne führende Nullen eingegeben werden (3.4.1965),
  angezeigt wird immer TT.MM.JJJJ.
- 05.11.2026 (Claude Code): Übersicht als Kacheln (nicht als Liste), sortiert nach
  Nachname. Grund: Bild, Name und Zimmer sind so auf einen Blick zu sehen.
- 05.11.2026 (Claude Code): Bilder werden unverändert in den Datenordner kopiert (PNG,
  JPG, BMP, GIF), nicht verkleinert. Grund: einfachster Weg; für eine Demo reicht das.
  Im Formular steht der Hinweis „Demo: bitte keine Fotos echter Personen“ (Regel 2).
- 05.11.2026 (Claude Code): Demo-Daten werden genau einmal automatisch geladen (Merker in
  der Datenbank). „Demo-Daten zurücksetzen“ löscht alle Bewohner und hinterlegten Bilder
  und lädt die Demo-Datei neu – mit Rückfrage.

## Testliste für Tobi

Version 0.2.0 (Bewohnerakte):

1. Programm starten (läuft schon 0.1.0, kommt 0.2.0 über das Update). Unten links steht
   „Version 0.2.0 · Build …“.
2. Unter „Bewohner“ stehen 10 ausgedachte Bewohner als Kacheln mit Initialen.
3. Akte öffnen und die drei Allergie-Anzeigen vergleichen: Anton Ahorn (rot,
   „Haselnüsse“), Berta Birke (grün, „keine bekannt“), Clara Clematis (orange
   gestrichelt, „nicht erfasst – bitte klären“).
4. Beschlussende vergleichen: Ahorn (Datum), Birke („kein Beschluss“), Ginster („nicht
   erfasst“).
5. „Neuer Bewohner …“: Aufnahmedatum vor das Geburtsdatum setzen → Fehlermeldung, nichts
   gespeichert. Danach richtig ausfüllen → neue Kachel; Allergien stehen auf „nicht
   erfasst“.
6. „Bearbeiten …“: ein ausgedachtes Bild (keine echte Person) hinterlegen → erscheint in
   Akte und Übersicht.
7. „Demo-Daten zurücksetzen …“ → Rückfrage, danach wieder die 10 Demo-Bewohner, eigene
   Änderungen und Bilder sind weg.

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
