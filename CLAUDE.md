# CLAUDE.md — Regeln für Sitzungen am Dokumentationssystem

## Was das Dokumentationssystem ist

Eine **Demo** für ein Dokumentationssystem in einem Wohnbereich eines psychiatrischen
Wohnheims (ohne Pflege). Sie dient zum **Vorführen** (Kolleginnen, Kollegen, Leitung) und
zeigt, wie die Arbeit des Wohnbereichs in **einem** Programm zusammenläuft.

- **Kern:** Bewohnerführung (Bewohnerakte je Person) und die tägliche schriftliche
  Dokumentation.
- **Später dazu** (eigene Einträge in `IDEEN.md`, erst bauen, wenn freigegeben):
  Ausgang, Medikation, Wochenplan, Biografie, Monatsreflexion, HEB-Bogen.
- **Nutzer:** Tobi zum Vorführen. Kein Echtbetrieb.

## Unverhandelbar

1. **Nur ausgedachte Daten.** Keine echten Namen, Geburtsdaten, Diagnosen, Medikamente,
   Fotos oder Texte von Bewohnern, Kolleginnen oder Kollegen — nicht im Code, nicht in
   Testdaten, nicht in Commits, nicht in Bildern. Demo-Namen sind erkennbar ausgedacht.
   Ausnahme: Allgemeine Nachschlagedaten wie echte Wirkstoff- und Handelsnamen in einer
   Medikamentendatenbank sind erlaubt; verboten ist nur die Zuordnung zu echten Personen.
   Das Repository ist **öffentlich**.
2. **Bewohnerbilder:** In den Demo-Daten nur Platzhalter (Initialen oder Silhouette),
   keine Fotos von Menschen und keine KI-Gesichter.
3. **Keine Geheimnisse** im Repository (Passwörter, Tokens, Schlüssel).
4. **Kein Echtbetrieb.** Die Oberfläche zeigt dauerhaft sichtbar „Demo — ausgedachte
   Daten“. Echte Daten wären Gesundheitsdaten (Art. 9 DSGVO) und bräuchten Freigaben,
   die es nicht gibt.
5. **Leere Felder sind nicht „nichts“.** Bei Sicherheitsangaben (z. B. Allergien) gibt es
   drei Zustände: *eingetragen*, *keine bekannt*, *nicht erfasst*. Ein leeres Feld darf nie
   wie „keine Allergie“ aussehen.
6. **Keine KI, kein Internet** außer dem Selbst-Update (Entscheidung Tobi, 06.11.2026,
   Datenschutz). Das Programm formuliert keine Texte selbst; es stellt zusammen, zählt,
   prüft und bietet Textbausteine an.
7. **Fremde Vorlagen bleiben draußen.** PDF-Bögen des Kostenträgers, Logos und Namen der
   Einrichtung kommen nie ins Repository; das Programm lässt sie in den Einstellungen
   zuteilen und speichert sie im Datenordner.

## Arbeitsweise

- Zu Beginn jeder Sitzung zuerst `IDEEN.md`, dann `STAND.md`.
- Gebaut wird nur, was in `IDEEN.md` `Status: freigegeben` hat. Fertige Einträge auf
  `Status: erledigt (<Datum>)` setzen.
- Lässt ein freigegebener Eintrag etwas offen: die einfachere Lösung nehmen und in
  `STAND.md`, Abschnitt „Entscheidungen“, mit Begründung festhalten.
- Anhalten und fragen nur, wenn eine Regel aus „Unverhandelbar“ verletzt würde oder
  etwas Unumkehrbares nötig ist.
- Kleine, lauffähige Schritte: ändern, testen, committen, `STAND.md` aktualisieren.
- Zweig: Arbeitszweig mit Pull Request. Zusammenführen nach `main` erst, wenn Tests und
  Windows-Build grün sind; dann darfst du selbst zusammenführen. Jeder Push nach `main`
  erzeugt ein Release, das Tobis Programm als Update bekommt.
- `IDEEN.md` ist der einzige Weg, auf dem Aufträge aus den Chats zu dir kommen.
- Jede ausgelieferte Änderung bekommt eine neue Versionsnummer (in `STAND.md`).

## Stand-Dateien (Brücke zu den Chats)

| Datei | Art | Inhalt |
|---|---|---|
| `STAND.md` | wird **überarbeitet** | Version, was fertig ist, offene Punkte, Entscheidungen, Testliste für Tobi |
| `SITZUNGEN.md` | wird **angehängt** | ein Einzeiler pro Sitzung: Datum — was erledigt wurde |
| `IDEEN.md` | wird überarbeitet | Aufträge aus den Chats, jeder mit Status |

## Technik

**Windows-Programm** (Entscheidung Tobi, 05.11.2026).

- Python 3.12, Oberfläche mit **PySide6** (Qt; Lizenz LGPLv3 — vor einem späteren Verkauf
  prüfen), Tests mit `unittest`. Weitere Abhängigkeiten nur mit Begründung in `STAND.md`.
- **Kern ohne Oberfläche:** Datenmodell, Speichern, Prüfregeln und Demo-Daten liegen in
  `dokusystem/kern/` und sind ohne PySide6 testbar. Die Oberfläche
  (`dokusystem/oberflaeche/`) ruft nur den Kern auf.
- **Speicher:** SQLite-Datei im Datenordner `%APPDATA%\Dokumentationssystem\` (sonst
  `~/.local/share/dokumentationssystem/`). Hinterlegte Bilder ebenfalls dort, nie im
  Repository.
- **Demo-Daten:** liegen als lesbare Datei im Repository (`daten/demo/`), werden beim ersten
  Start geladen und lassen sich über „Demo-Daten zurücksetzen“ wiederherstellen.
- **Programmdatei und Selbst-Update nach Vorbild von `KhaosDE/N3XTBYT3`** (öffentlich; dort
  Paket `n3xtbyt3_update` und `.github/workflows/tests-und-exe.yml`): GitHub Actions testet
  bei jedem Push nach `main` unter Windows, baut die `.exe` (PyInstaller) und veröffentlicht
  sie mit SHA-256-Prüfsumme als Release `build-<Laufnummer>`. Die `.exe` fragt beim Start
  nach dem neuesten Release *dieses* Repositorys, lädt, prüft die Prüfsumme, tauscht sich
  aus und startet neu. Nur diese Adressen, kein anderer Netzzugriff. Abschaltbar
  (`--kein-update` und Einstellung). Schlägt etwas fehl, startet die alte Version.
- Die `.exe` ist nicht signiert; Windows SmartScreen warnt beim ersten Start. Im README in
  einfachen Worten erklären.

## Sprache

Oberfläche, Dokumentation und Commit-Nachrichten auf Deutsch, einfach und ohne unnötige
Fachsprache.
