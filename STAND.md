# STAND — Dokumentationssystem

Stand: 07.11.2026. Version: **0.7.0** (Wohnbereich-ToDo, Bauplan Schritt 4).

## Aktuell

Das Programm (C# / .NET 10, Windows Forms) zeigt alle geplanten Bereiche in der
Seitenleiste; was noch nicht gebaut ist, sagt „Kommt in Version …“. Gebaut sind die
Bewohnerakte und jetzt die **tägliche Dokumentation**: Berichte je Bewohner mit den
Kennzeichen „Wichtig“ und „Übergabe“, Nachträge statt Überschreiben, Ausdruck, und der
Bereich **„Übergabe“**, in dem nur die Übergabe-Einträge stehen, bis sie ablaufen oder
erledigt sind. Neu ist das **Wohnbereich-ToDo** („Aufgaben“): Aufgaben an die nächste Schicht,
die stehen bleiben, bis sie erledigt sind.

**Nächster Schritt:** Bauplan Schritt 5 „Nachrichten“ (0.8.0), danach der Reihe nach bis
Schritt 15 (`IDEEN.md`, oben). Für Schritt 11 (Modul Ausgangsliste) das Repository
`KhaosDE/Ausgangsliste` anhängen; Grundlage ist `docs/bruecke-ausgangsliste.md`.

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

- 0.5.0 — Gestaltung Stufe 2 (07.11.2026, Bauplan Schritt 2):
  - Bewohnerkarte nach Vorbild der Ausgangsliste: Bild/Initialen, Name, Zimmer-Marke, darunter
    Marken „Beschluss bis …“, „Allergie: …“, „Allergien nicht erfasst“, „Beschluss nicht
    erfasst“, unten die Aufenthaltsdauer („seit 01.09.2019 · 7 Jahre, 2 Monate“); weicher
    Schatten, Rand unter der Maus kräftiger.
  - Filterknöpfe über dem Raster mit Zählern: Alle, Beschluss, Allergie, Nicht erfasst.
  - Statusleiste unten mit Farbschlüssel der Marken.
  - Logofeld im Kopf (seit 0.4.0, Bild aus den Einstellungen).

- 0.6.0 — Tägliche Dokumentation mit Bereich „Übergabe“ (07.11.2026, Bauplan Schritt 3):
  - Reiter „Berichte“ in der Akte: Liste neueste oben, Filter nach Zeitraum, Lebensbereich,
    Schicht, „nur wichtige“ und Suche im Text; Marken „Wichtig“, „Übergabe“ und die
    Lebensbereiche an jedem Bericht. „Drucken …“ mit Druckvorschau (übernommen aus der
    Ausgangsliste, bereinigt).
  - Bericht schreiben aus der Akte oder über „Neuer Bericht …“ in der Seitenleiste (mit
    Bewohnerauswahl): Datum, Uhrzeit, Schicht passend zur Uhrzeit und eigenes Kürzel sind
    vorbelegt; Lebensbereiche 1–5 oder „allgemein“; die Kennzeichen „Wichtig“ und
    „Übergabe“ gut sichtbar im Fenster, bei „Übergabe“ die Dauer (nächste Schicht, 3 Tage,
    1 Woche, bis Datum, bis erledigt) und ein freiwilliger kurzer Übergabe-Satz.
  - Hinweis vor dem Speichern: Stehen Wörter wie „Arzt“, „Termin“, „Sturz“ im Text und ist
    „Übergabe“ nicht gesetzt, fragt das Programm einmal „Soll das in die Übergabe?“. Wortliste
    in `daten/uebergabe-woerter.txt`, ergänzbar über eine gleichnamige Datei im Datenordner.
  - Nichts wird gelöscht oder überschrieben: Korrekturen sind Nachträge mit eigener Zeit und
    eigenem Kürzel; die Datenbank verbietet das Ändern von Berichten.
  - Bereich „Übergabe“: oben „Wohnbereich allgemein“ (eigene Notizen ohne Bericht), darunter
    nach Bewohner gruppiert. Marken „neu seit deiner letzten Schicht“, „läuft heute ab“ und
    die Gültigkeit; „Gelesen“ (Kenntnisnahme mit Kürzel, sichtbar wer schon gelesen hat),
    „Verlängern …“, „Erledigt“; erledigte und abgelaufene Einträge einblendbar;
    „Übergabeblatt drucken …“ mit den offenen Einträgen.
  - Karten der Bewohner: Marke „Übergabe N“ (offene, vom eigenen Kürzel ungelesene
    Einträge) und „Wichtig“ (wichtiger Bericht in den letzten 7 Tagen).
  - Seitenleiste nach dem neuen Bauplan: zusätzlich „Aufgaben“ (0.7.0), „Nachrichten“
    (0.8.0) und „Ausgangsliste“ (0.14.0) als Platzhalter; alle Versionsangaben angepasst.
  - Demo: für jeden Bewohner Berichte der letzten vier Wochen über alle Lebensbereiche,
    einige wichtige, einige mit Nachtrag; Übergabe-Einträge (offen, mehrtägig, erledigt,
    abgelaufen) und zwei Notizen für den Wohnbereich – alles relativ zum heutigen Tag.

- 0.7.0 — Wohnbereich-ToDo (07.11.2026, Bauplan Schritt 4):
  - Bereich „Aufgaben“: offene Aufgaben, überfällige oben (rot, mit dem Wort „überfällig“ und
    rotem Balken), dann was jetzt dran ist, dann nach Fälligkeit. Marken „für Spätdienst
    Sa 07.11.“ bzw. „wer Zeit hat“, „fällig bis …“, „jetzt dran“. Erledigte einblendbar.
  - „Neue Aufgabe …“: Text, Bewohner (oder keiner), für wen (nächste Schicht – Vorgabe –,
    bestimmte Schicht an einem Tag, wer Zeit hat), fällig bis (Datum, Uhrzeit freiwillig),
    angelegt von (Kürzel vorbelegt).
  - „Erledigt“ (mit Kürzel und Zeit) und „An die nächste Schicht weitergeben …“ mit kurzer
    Notiz; die Weitergabe steht sichtbar an der Aufgabe. Nichts wird gelöscht oder
    überschrieben.
  - Aus einem Bericht: „Aufgabe daraus …“ übernimmt den Bewohner.
  - Zähler offener Aufgaben in der Seitenleiste (rot, wenn etwas überfällig ist) und eine
    Kurzliste der ersten drei offenen Aufgaben über den Bewohnerkarten mit Knopf zu allen.
  - Ausdruck der offenen Aufgaben (Druckvorschau).
  - Demo: sechs ausgedachte Aufgaben relativ zu heute, eine überfällig und schon einmal
    weitergegeben, eine erledigt. „Wichtig“ steht in den Demo-Berichten seltener.

## Offene Punkte

- Bewohnerakte: Vorschläge Beschlusswarnung, „offen geführt“, Altersanzeige — Tobi
  entscheidet.
- Bewohner löschen oder als ausgezogen markieren gibt es noch nicht (war nicht
  beauftragt). Zum Aufräumen in der Demo: „Demo-Daten zurücksetzen“.
- Dunkle Darstellung ist nicht gebaut (`docs/gestaltung.md` sieht sie vor, freigegeben ist
  bisher nur die helle).
- Aus der Ausgangsliste noch nicht übernommen: Datenbank mit Mehrplatz-Schutz, Sicherung
  (die Druckvorschau ist seit 0.6.0 übernommen).
- „Bericht lesen …“ in der Übergabe zeigt den Bericht in einem einfachen Meldungsfenster;
  ein eigenes Lesefenster wäre schöner, war aber nicht verlangt.
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

- **Zwei Nummern.** Die Versionsnummer (z. B. 0.6.0) steht von Hand in
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
- **Marken auf den Karten (0.5.0):** Nur was Aufmerksamkeit braucht, bekommt eine Marke
  (Farbe sparsam): Beschluss mit Enddatum, eingetragene Allergie, „nicht erfasst“ bei
  Allergien oder Beschluss. „Keine bekannt“ und „kein Beschluss“ sind der Normalfall ohne
  Marke – „nicht erfasst“ bleibt dadurch immer sichtbar (Regel 5). Allergie und Beschluss
  teilen sich Rot (wichtig, wie der Allergiehinweis in der Akte), „nicht erfasst“ ist Gelb;
  jede Marke trägt ein Wort. Lange Allergietexte werden auf der Karte mit „…“ gekürzt.
- **Schatten (0.5.0):** Die Karten haben den weichen, mehrlagigen Schatten der Ausgangsliste
  (Eintrag „Gestaltung Stufe 2“ verlangt ihn); `docs/gestaltung.md` sah nur einen Hauch vor.
- **Filterknöpfe** mit Radius 4 statt Pillenform (`docs/gestaltung.md`: keine Pillen-Knöpfe).
  „Nicht erfasst“ zählt Bewohner, bei denen Allergien oder Beschluss nicht erfasst sind. Der
  gewählte Filter bleibt, bis man einen anderen wählt.
- **Farbschlüssel in der Statusleiste:** Die Ausgangsliste hat ihren abgeschafft; hier
  verlangt ihn der Eintrag. Er erklärt Rot, Gelb und Blaugrau mit Worten.
- **Oberflächentests nur unter Windows** (Windows Forms läuft nicht unter Linux); sie laufen
  in GitHub Actions vor jedem Zusammenführen und legen Bildschirmfotos ab.
- **Programmgerüst (07.11.2026):** Das Programm startet im Bereich „Bewohner“, solange die
  Übersicht nur ein Platzhalter ist (Startseite wird sie mit 0.17.0). „Einstellungen“ und
  „Neuer Bericht …“ öffnen ein Fenster und sind keine eigenen Bereiche; „Einstellungen“ steht
  etwas abgesetzt unten in der Liste. Die Ausgangsliste steht als Platzhalter in der Liste;
  mit der Modulverwaltung (0.14.0) lässt sie sich ausblenden.
- **Eigenes Bild im Kopf:** Kopie im Datenordner (`logo\`), Dateiname in
  `einstellungen.json`; das alte Bild wird beim Ersetzen gelöscht. Es steht rechts oben, der
  Demo-Hinweis direkt links daneben, damit er immer sichtbar bleibt (Regel 4).
- **Kürzel:** leer oder bis zu 6 Buchstaben/Ziffern, gespeichert in `einstellungen.json`
  (wie das Update-Häkchen). Gebraucht ab 0.6.0 für Berichte.
- **Tägliche Dokumentation (0.6.0), wo der Eintrag etwas offen ließ:**
  - Schichten nach Uhrzeit: Früh 6–14 Uhr, Spät 14–22 Uhr, Nacht 22–6 Uhr. Die Schicht lässt
    sich im Fenster auch von Hand wählen.
  - „Nächste Schicht“ heißt: gilt bis zum Ende der Schicht nach der, in der geschrieben wurde.
    „3 Tage“, „1 Woche“ und „bis Datum“ gelten bis zum Ende des jeweiligen Tages.
  - „Neu seit deiner letzten Schicht“: entstanden nach deinem letzten Besuch im Bereich
    „Übergabe“ (je Kürzel gemerkt) und von dir noch nicht gelesen. Ohne Kürzel gibt es kein
    „neu“, und „Gelesen“, „Verlängern“, „Erledigt“ sind gesperrt (Hinweis auf die
    Einstellungen).
  - Marke „Übergabe N“ auf der Karte: Ohne eigenes Kürzel zählt sie alle offenen Einträge.
  - Verlängern und Erledigt werden als eigene Ereignisse mit Kürzel und Zeit gespeichert; der
    ursprüngliche Eintrag bleibt unverändert (nichts wird überschrieben).
  - Rückfrage bei Übergabe-Wörtern mit drei Knöpfen: Ja (in die Übergabe, nächste Schicht),
    Nein (ohne Übergabe speichern), Abbrechen (zurück zum Text). Gesucht wird auch in
    zusammengesetzten Wörtern („Arzttermin“ enthält „Arzt“), Groß-/Kleinschreibung egal.
  - Farben: „Übergabe“ Blaugrün, „Wichtig“ Violett, „läuft heute ab“ Gelb (wie „bitte
    klären“), Gültigkeit und Lebensbereiche Blaugrau; immer mit Wort.
  - Eine Karte zeigt jetzt bis zu drei Zeilen Marken (vorher zwei), damit „nicht erfasst“
    auch neben „Übergabe“ und „Wichtig“ sichtbar bleibt (Regel 5).
  - Demo-Daten: Die ausgedachten Berichte sind bei gleichem Datum zu jeder Uhrzeit gleich
    (fester Zufallswert je Bewohner); Berichte in der Zukunft werden weggelassen.
- **Wohnbereich-ToDo (0.7.0), wo der Eintrag etwas offen ließ:**
  - „Hauptbildschirm“: Die Übersicht ist bis 0.17.0 noch ein Platzhalter. Darum steht die
    Kurzliste der Aufgaben vorerst über den Bewohnerkarten (dort startet das Programm); die
    Startseite übernimmt sie später.
  - „Nächste Schicht“ heißt die Schicht nach der, in der die Aufgabe angelegt oder zuletzt
    weitergegeben wurde. Nach dem Weitergeben gilt eine Aufgabe immer für die nächste
    Schicht, auch wenn sie vorher „wer Zeit hat“ oder für eine bestimmte Schicht war.
  - „Jetzt dran“: Die Schicht der Aufgabe hat begonnen (auch wenn sie aus einer früheren
    Schicht liegen geblieben ist).
  - „Fällig bis“ ohne Uhrzeit gilt bis zum Ende des Tages. Die Notiz beim Weitergeben ist
    freiwillig (höchstens 200 Zeichen).
  - „Erledigt“ und „Weitergeben“ brauchen das eigene Kürzel aus den Einstellungen (wie in der
    Übergabe); ohne Kürzel sind die Knöpfe gesperrt, mit Hinweis.
  - Erledigte Aufgaben lassen sich nicht wieder öffnen; dafür legt man eine neue an (nichts
    wird überschrieben).
- 05.11.2026, weiter gültig: Update-Einstellung als Häkchen unten links, gespeichert in
  `einstellungen.json`; `update.log` behält die letzten 300 Zeilen; der Workflow läuft auch
  bei Pull Requests (ohne Release); Name als Vorname und Nachname; Beschlussende mit drei
  Zuständen; Pflichtfelder Vorname, Nachname, Zimmer, Geburtsdatum, Aufnahmedatum; Datum
  auch ohne führende Nullen; Übersicht sortiert nach Nachname; Bilder werden unverändert in
  den Datenordner kopiert (PNG, JPG, BMP, GIF); Demo-Daten werden genau einmal automatisch
  geladen.

## Testliste für Tobi

Version 0.7.0:

1. Unten links „Version 0.7.0 · Build …“. Links bei „Aufgaben“ ein roter Zähler „5“ (eine
   Demo-Aufgabe ist überfällig).
2. Über den Bewohnerkarten: „AUFGABEN · 5 OFFEN · 1 ÜBERFÄLLIG“ mit drei Zeilen, oben die
   überfällige. „Alle 5 Aufgaben …“ führt zum Bereich „Aufgaben“.
3. Bereich „Aufgaben“: oben „Aushang mit dem Wochenplan …“ mit „überfällig seit …“, rotem
   Balken und der Weitergabe „Drucker war leer …“.
4. Einstellungen: Kürzel „TS“. „Neue Aufgabe …“: ohne Text speichern → Fehlermeldung.
   „bestimmte Schicht“ wählen, Tag in zwei Tagen, Frühdienst → Aufgabe erscheint mit „für
   Frühdienst …“, der Zähler steigt.
5. Bei „Getränkevorrat …“ „An die nächste Schicht weitergeben …“ mit Notiz → die Notiz steht
   an der Aufgabe. „Erledigt“ → sie verschwindet; „Erledigte zeigen“ → sie steht unter
   „ERLEDIGT“ mit „TS“.
6. Akte → Reiter „Berichte“ → „Aufgabe daraus …“: Der Bewohner ist schon gewählt.
7. „Offene Aufgaben drucken …“ → Druckvorschau.
8. „Demo-Daten zurücksetzen …“ → wieder 5 offene Aufgaben.

Version 0.6.0:

1. Unten links „Version 0.6.0 · Build …“. Links stehen jetzt auch „Aufgaben“, „Nachrichten“
   und „Ausgangsliste“ mit „Kommt in Version …“.
2. Einstellungen: Kürzel „TS“ eintragen.
3. Karten: Anton Ahorn zeigt „Übergabe 1“ und „Wichtig“, Berta Birke keine Übergabe-Marke.
4. Akte Anton Ahorn → Reiter „Berichte“: Berichte der letzten vier Wochen, neueste oben, mit
   Marken. „nur wichtige“ anhaken → nur noch violett markierte. Lebensbereich, Schicht und
   Suche ausprobieren.
5. „Neuer Bericht …“ in der Akte: Datum, Uhrzeit, Schicht und „TS“ sind vorbelegt. Ohne
   Lebensbereich und Text speichern → Fehlermeldung. Lebensbereich wählen, Text „Termin beim
   Arzt am Donnerstag“ → beim Speichern kommt „Soll das in die Übergabe?“.
6. Bei einem Bericht „Nachtrag …“ → Nachtrag steht darunter, der Text darüber bleibt gleich.
7. „Neuer Bericht …“ in der Seitenleiste: Bewohner wählen, „Übergabe“ anhaken, „3 Tage“ und
   einen kurzen Satz eingeben → danach öffnet sich die Akte beim Reiter „Berichte“.
8. Bereich „Übergabe“: oben „Wohnbereich allgemein“, darunter nach Bewohner. Beim ersten
   Besuch mit „TS“ ist alles „neu seit deiner letzten Schicht“, beim nächsten Besuch nicht
   mehr. „Gelesen“ → „gelesen: TS (…)“. „Verlängern …“ → neue Dauer. „Erledigt“ → Eintrag
   verschwindet; „Erledigte und abgelaufene zeigen“ → er steht unter „ERLEDIGT /
   ABGELAUFEN“.
9. „Notiz für den Wohnbereich …“ → erscheint oben unter „Wohnbereich allgemein“.
10. „Übergabeblatt drucken …“ und in der Akte „Drucken …“ → Druckvorschau; „Drucken …“ dort
    lässt z. B. „Microsoft Print to PDF“ wählen.
11. „Demo-Daten zurücksetzen …“ → Berichte und Übergabe sind wieder im Demo-Zustand.

Version 0.5.0:

1. Unten links „Version 0.5.0 · Build …“.
2. Bewohnerkarten zeigen Bild/Initialen, Name, „Zi. …“, darunter Marken: Anton Ahorn
   „Beschluss bis 30.06.2027“ und „Allergie: Haselnüsse“ (rot), Gustav Ginster „Allergien
   nicht erfasst“ und „Beschluss nicht erfasst“ (gelb), Berta Birke keine Marke. Unten auf
   jeder Karte „seit … · … Jahre, … Monate“.
3. Filterknöpfe über den Karten: Alle 10, Beschluss 5, Allergie 4, Nicht erfasst 4. Ein Klick
   zeigt nur die passenden Karten, oben steht „4 von 10 Bewohner“.
4. Unten die Statusleiste mit Farbschlüssel (Rot: Beschluss, Allergie; Gelb: nicht erfasst;
   Blaugrau: Zimmer).
5. Maus über eine Karte: Der Rand wird blauer.

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
