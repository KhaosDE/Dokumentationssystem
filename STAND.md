# STAND — Dokumentationssystem

Stand: 07.11.2026. Version: **0.4.0** (Programmgerüst mit allen Bereichen und Einstellungen,
Bauplan Schritt 1).

## Aktuell

Das Programm (C# / .NET 10, Windows Forms) zeigt jetzt alle geplanten Bereiche in der
Seitenleiste; was noch nicht gebaut ist, sagt „Kommt in Version …“. Die Bewohnerakte hat
Reiter, und es gibt ein Einstellungsfenster (Kürzel, Update, eigenes Bild im Kopf).

**Nächster Schritt:** Bauplan Schritt 2 „Gestaltung Stufe 2“ (0.5.0), danach der Reihe nach
bis Schritt 15 (`IDEEN.md`, oben). Für die Schritte 2 und 11 das Repository
`KhaosDE/Ausgangsliste` anhängen.

## Fertig

- Repository `KhaosDE/Dokumentationssystem` (öffentlich, wegen der Update-Funktion).
- `CLAUDE.md`, `IDEEN.md`, `STAND.md`, `SITZUNGEN.md`, `README.md`, `docs/`.
- 0.1.0 und 0.2.0 (Python, 05.11.2026): Programmrahmen mit Selbst-Update, Bewohnerakte.
- 0.3.0 — Umstieg auf C# / .NET 10 und Gestaltung (06.11.2026):
  - Aufbau: `src/Dokusystem.Kern` (ohne Fenster, auch unter Linux testbar),
    `src/Dokusystem.Update` (einziges Projekt mit Internet), `src/Dokusystem` (Windows Forms),
    Tests in `tests/`. Projektmappe `Dokumentationssystem.sln`.
  - Gleicher Funktionsumfang wie 0.2.0: Hauptfenster mit Demo-Hinweis, Navigation, Version
    und Build; Datenordner; Update-Häkchen und `--kein-update`; `update.log`; Bewohnerakte
    mit Übersicht, Akte, Formular, Prüfregeln, drei Zuständen bei Allergien und
    Beschlussende, Bild per Dateiauswahl, „Demo-Daten zurücksetzen“.
  - Gestaltung nach `docs/gestaltung.md`: zentrale Klasse `Stil` (übernommen aus der
    Ausgangsliste, bereinigt), Kopf mit Farbverlauf, Kartenraster, Seitenleiste mit hellem
    Akzent für den aktiven Eintrag, eine Hauptaktion pro Ansicht in Akzentblau, Haarlinien
    statt Schatten, Radien 4/8, Abschnittsbeschriftung „STAMMDATEN“ in der Akte.
  - Selbst-Update in C# nach demselben Ablauf; die installierte Python-Version ersetzt sich
    über ihr eigenes Update durch die C#-Version (geprüft, siehe Entscheidungen).
  - `.exe`: eine Datei, mit .NET, nicht komprimiert; Selbsttest in GitHub Actions.
  - Tests: 50 für Kern und Update (xUnit), dazu Oberflächentests unter Windows, die auch
    Bildschirmfotos als Artefakt „Bildschirmfotos“ ablegen.

- 0.4.0 — Programmgerüst (07.11.2026, Bauplan Schritt 1):
  - Seitenleiste: Übersicht, Bewohner, Neuer Bericht …, Übergabe, Kalender, Therapie,
    Medikamente, Einstellungen. Noch nicht gebaute Bereiche zeigen „Kommt in Version …“ mit
    kurzer Beschreibung (Versionen aus dem Bauplan).
  - Bewohnerakte mit Reitern: Stammdaten, Berichte, Medikation, Wochenplan, Biografie,
    Reflexion, HEB. Kopfkarte und Allergiehinweis bleiben über den Reitern immer sichtbar.
  - Einstellungsfenster: eigenes Kürzel (bis 6 Buchstaben/Ziffern), Update-Häkchen (von unten
    links hierher verschoben), eigenes Bild für den Kopf, Hinweis auf spätere Einstellungen.
  - Kopf: Logofeld rechts oben mit dem gewählten Bild; der Demo-Hinweis steht daneben.

## Offene Punkte

- Bewohnerakte: Vorschläge Beschlusswarnung, „offen geführt“, Altersanzeige — Tobi
  entscheidet.
- Bewohner löschen oder als ausgezogen markieren gibt es noch nicht (war nicht
  beauftragt). Zum Aufräumen in der Demo: „Demo-Daten zurücksetzen“.
- Dunkle Darstellung ist nicht gebaut (`docs/gestaltung.md` sieht sie vor, freigegeben ist
  bisher nur die helle).
- Aus der Ausgangsliste noch nicht übernommen (nicht im Umfang von 0.2.0): Datenbank mit
  Mehrplatz-Schutz, Sicherung, Druckvorschau.
- Der Wechsel 0.2.0 → 0.3.0 über das Update ist am Code geprüft und mit dem echten Release
  durchgespielt (Laden, Prüfsumme), aber nicht auf einem echten Windows-Rechner – bitte
  Testliste Punkt 1.

## Entscheidungen

- 05.11.2026 (Tobi): Demo mit ausgedachten Daten, zu Präsentationszwecken.
- 05.11.2026 (Tobi): Alle Werkzeuge des Wohnbereichs verschmelzen zu einem Programm; Kern
  sind Bewohnerführung und tägliche Dokumentation.
- 05.11.2026 (Tobi): Repository öffentlich, damit eine Update-Funktion gebaut und erprobt
  werden kann.
- 05.11.2026 (Tobi): Windows-Programm. Ideen werden im Chat besprochen, landen in
  `IDEEN.md`, Claude Code setzt sie um.
- 05.11.2026 (Chat): Python + PySide6 + SQLite. **Abgelöst am 06.11.2026** (nächster Punkt).
- 06.11.2026 (Tobi): Umstieg auf C# / .NET 10 mit Windows Forms. Grund: Die Ausgangsliste
  ist bereits ein C#-Programm mit vielen passenden Bausteinen (Kartenraster, Mehrplatz-
  Datenbank, Druck, Installer); ein späterer Server geht in derselben Sprache; freie
  Lizenzen. .NET 10 statt 8, weil .NET 8 ab 10.11.2026 keine Updates mehr bekommt.
- 06.11.2026 (Tobi): Keine KI und keine Internetverbindung außer dem Update (Datenschutz).
- 06.11.2026 (Tobi): Gestaltung nach Vorbild der Ausgangsliste (`docs/gestaltung.md`).

Von Claude Code (Begründungen; gelten weiter, sofern nicht anders vermerkt):

- **Zwei Nummern.** Die Versionsnummer (jetzt 0.3.0) steht von Hand in
  `Directory.Build.props` und in dieser Datei; die Build-Nummer ist die Laufnummer von
  GitHub Actions und wird beim Bauen in die `.exe` geschrieben. Das Update vergleicht nur
  Build-Nummern. Im Fenster steht beides („Version 0.3.0 · Build 7“).
- **Workflow-Datei bleibt `tests-und-exe.yml`.** Die Laufnummer zählt je Workflow-Datei;
  eine umbenannte Datei finge wieder bei 1 an, und installierte Programme sähen kein neueres
  Release mehr.
- **Übergang ohne Neuinstallation (06.11.2026), geprüft am Code des Python-Updaters:** Er
  nimmt jedes Release `build-N` mit höherer Nummer, das `Dokumentationssystem.exe` und
  `Dokumentationssystem.exe.sha256` enthält, prüft die Prüfsumme, tauscht die Datei aus und
  startet sie – welche Sprache die neue Datei hat, spielt keine Rolle. Die C#-Version löscht
  beim Start die alte Programmdatei (`.exe.alt`) und fragt nach dem Wechsel nicht gleich
  erneut (gleiche Umgebungsvariable wie die Python-Version). Ein Durchlauf mit dem echten
  Release (Laden, Prüfsumme) klappte mit dem C#-Code.
- **Eigene Datenbankdatei `dokumentationssystem.db`.** Die Datei der Python-Version
  (`dokusystem.sqlite3`) wird nicht gelesen und nicht gelöscht (Eintrag „Umstieg“).
  `einstellungen.json` (Update-Häkchen) und `update.log` werden weiter gemeinsam benutzt.
- **„Demo-Daten zurücksetzen“ löscht nur die Bilder der Bewohner in der Datenbank**, nicht
  mehr den ganzen Ordner `bilder\`. Grund: Bilder der Python-Version bleiben so unberührt.
- **Update-Projekt getrennt** (`Dokusystem.Update`): einziges Projekt mit Internetzugriff;
  ein Test prüft, dass Kern und Oberfläche keine Netzbausteine und keine Adressen
  enthalten. Der Dateiname der `.exe` darf abweichen (z. B. „Dokumentationssystem (1).exe“
  nach doppeltem Herunterladen) – wie bei der Python-Version.
- **Abhängigkeiten** (alle mit freier Lizenz): im Programm nur `Microsoft.Data.Sqlite` (MIT,
  Speicher). Nur für Tests: `xunit` und `xunit.runner.visualstudio` (Apache-2.0),
  `Microsoft.NET.Test.Sdk` (MIT). Windows Forms und Segoe UI gehören zu .NET bzw. Windows.
- **Übernommen aus der Ausgangsliste**, angepasst und bereinigt (kein Logo, kein Name der
  Einrichtung oder des Wohnbereichs, keine Personenkürzel, auch nicht in Kommentaren):
  `Stil` (Farben, Schriften, Abstände, Knöpfe, abgerundete Flächen), Kopfbereich mit
  Farbverlauf, Kartenraster, Aufbau der Bewohnerkarte. Die Update-Funktion der Ausgangsliste
  (Update-Ordner) passt nicht; es bleibt der GitHub-Weg.
- **Gestaltung im Einzelnen:** Wo `docs/gestaltung.md` die Ausgangsliste verfeinert, gilt
  `docs/gestaltung.md` (Hauch von Schatten statt mehrlagig, Radius 8 statt 12/16).
  Allergiehinweis mit den Markenfarben: eingetragen = „Beschluss“ (rot), keine bekannt =
  „Offen“ (grün), nicht erfasst = „Achtung“ (gelb, gestrichelt) – immer mit Text und einem
  farbigen Balken links.
- **Demo-Hinweis** rechts oben im Kopf (dort, wo die Ausgangsliste ihr Logofeld hat), als
  Marke „Achtung“; zusätzlich im Fenstertitel und oben im Formular.
- **Karten zeigen wie in 0.2.0 nur Bild, Name und Zimmer** – keine Marken für Beschluss oder
  Allergie. Grund: Der Eintrag erlaubt keine neuen Funktionen; Marken gehören zur
  „Startseite als Übersicht“ (offen).
- **Oberflächentests nur unter Windows** (Windows Forms läuft nicht unter Linux); sie laufen
  in GitHub Actions vor jedem Zusammenführen und legen Bildschirmfotos ab.
- **Programmgerüst (07.11.2026):** Das Programm startet im Bereich „Bewohner“, solange die
  Übersicht nur ein Platzhalter ist (Startseite wird sie mit 0.15.0). „Einstellungen“ öffnet
  ein Fenster und ist kein eigener Bereich; der Eintrag steht etwas abgesetzt unten in der
  Liste. „Neuer Bericht …“ ist bis 0.6.0 eine Platzhalter-Seite.
- **Eigenes Bild im Kopf:** Kopie im Datenordner (`logo\`), Dateiname in
  `einstellungen.json`; das alte Bild wird beim Ersetzen gelöscht. Es steht rechts oben, der
  Demo-Hinweis direkt links daneben, damit er immer sichtbar bleibt (Regel 4).
- **Kürzel:** leer oder bis zu 6 Buchstaben/Ziffern, gespeichert in `einstellungen.json`
  (wie das Update-Häkchen). Gebraucht ab 0.6.0 für Berichte.
- 05.11.2026, weiter gültig: Update-Einstellung als Häkchen unten links, gespeichert in
  `einstellungen.json`; `update.log` behält die letzten 300 Zeilen; der Workflow läuft auch
  bei Pull Requests (ohne Release); Name als Vorname und Nachname; Beschlussende mit drei
  Zuständen; Pflichtfelder Vorname, Nachname, Zimmer, Geburtsdatum, Aufnahmedatum; Datum
  auch ohne führende Nullen; Übersicht sortiert nach Nachname; Bilder werden unverändert in
  den Datenordner kopiert (PNG, JPG, BMP, GIF); Demo-Daten werden genau einmal automatisch
  geladen.

## Testliste für Tobi

Version 0.4.0:

1. Nach dem Update steht unten links „Version 0.4.0 · Build …“.
2. Links stehen: Übersicht, Bewohner, Neuer Bericht …, Übergabe, Kalender, Therapie,
   Medikamente, Einstellungen. Jeder noch nicht gebaute Bereich zeigt „Kommt in Version …“
   mit kurzer Beschreibung.
3. Akte öffnen: Unter Kopfkarte und Allergiehinweis stehen Reiter. „Stammdaten“ zeigt die
   bekannten Angaben, die anderen „Kommt in Version …“. Eine andere Akte beginnt wieder bei
   „Stammdaten“.
4. Einstellungen: Kürzel „A.B“ → Fehlermeldung; Kürzel „TS“ → gespeichert und beim nächsten
   Öffnen wieder da. Das Update-Häkchen steht jetzt hier (nicht mehr unten links).
5. Einstellungen → „Bild wählen …“: ein ausgedachtes Bild (kein echtes Logo der Einrichtung
   im Repository – nur bei dir im Datenordner) → erscheint rechts oben im Kopf, der
   Demo-Hinweis bleibt daneben sichtbar. „Bild entfernen“ → Logofeld wieder leer.

Version 0.3.0:

1. **Wechsel von 0.2.0:** Die vorhandene 0.2.0 starten. Erwartet: „Neue Version wird
   geladen …“, danach startet das Programm neu – jetzt mit neuem Aussehen. Unten links
   steht „Version 0.3.0 · Build …“. In `%APPDATA%\Dokumentationssystem\update.log` stehen
   die Schritte. Klappt das nicht: neue `.exe` über den Link im README von Hand laden.
2. Oben rechts im Kopf steht „Demo — ausgedachte Daten“, auch im Fenstertitel.
3. Unter „Bewohner“ stehen 10 ausgedachte Bewohner als Karten mit Initialen (die Demo-Daten
   beginnen neu; Änderungen aus 0.2.0 sind nicht übernommen).
4. Akte öffnen und die drei Allergie-Anzeigen vergleichen: Anton Ahorn (rot,
   „Haselnüsse“), Berta Birke (grün, „keine bekannt“), Clara Clematis (gelb gestrichelt,
   „nicht erfasst – bitte klären“).
5. Beschlussende vergleichen: Ahorn (Datum), Birke („kein Beschluss“), Ginster („nicht
   erfasst“).
6. „Neuer Bewohner …“: Aufnahmedatum vor das Geburtsdatum setzen → Fehlermeldung, nichts
   gespeichert. Danach richtig ausfüllen → Akte öffnet sich, neue Karte in der Übersicht;
   Allergien stehen auf „nicht erfasst“.
7. „Bearbeiten …“: ein ausgedachtes Bild (keine echte Person) hinterlegen → erscheint in
   Akte und Karte.
8. „Demo-Daten zurücksetzen …“ → Rückfrage, danach wieder die 10 Demo-Bewohner.
9. Häkchen „Beim Start nach Updates suchen“ (ab 0.4.0 unter Einstellungen) entfernen, neu
   starten: In `update.log` steht „in den Einstellungen abgeschaltet“.
10. Windows-Anzeige auf 125 % oder 150 % stellen: Nichts darf abgeschnitten sein.
