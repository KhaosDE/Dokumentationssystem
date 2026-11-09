# CLAUDE.md — Regeln für Sitzungen an DokuLine

## Was DokuLine ist

**DokuLine** („Betreuung auf Augenhöhe“; bis 08.11.2026 „Dokumentationssystem“) ist eine
**Demo** für ein Dokumentationssystem in einem Wohnbereich eines psychiatrischen
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
   Erlaubt ist, an Programme auf dem Rechner zu **übergeben**, ohne selbst eine Verbindung
   aufzubauen: das E-Mail-Programm öffnen (`mailto:`), `.ics`-Dateien ex- und importieren,
   eine Adresse im Browser öffnen (Entscheidung Tobi, 08.11.2026). E-Mail-Adressen in
   Demo-Daten nur mit der Endung `@example.org`.
   **Einzige eingebettete Webansicht:** im Formular „Medikament anlegen/ändern“ zum
   Nachschlagen beim BfArM (Entscheidung Tobi, 08.11.2026). Nur dessen Adressen, gesucht wird
   nur mit dem Medikamentennamen — nie mit Angaben zu Bewohnern. Abschaltbar.
   **Anzeige im lokalen Netzwerk** (Entscheidung Tobi, 09.11.2026): DokuLine darf eine nur lesende
   Seite mit den laufenden Therapien für einen Fernseher im Wohnbereich bereitstellen — nur für
   Adressen im LAN, mit Zugangscode, standardmäßig aus, ohne Verbindung ins Internet (Eintrag
   „Therapie-Anzeige für den Fernseher im Wohnbereich“ in `IDEEN.md`).
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

Vault-Ordner (Obsidian, nur Dokumentation): `DokuLine/` (bis 08.11.2026 `Dokumentationssystem/`). Cloud-Sitzungen erreichen
ihn nicht; Claude Code schreibt nur ins Repository.

## Technik

**Windows-Programm in C# / .NET 10** (Entscheidung Tobi, 06.11.2026; vorher Python/PySide6
bis Version 0.2.0, abgelöst durch den Eintrag „Umstieg auf C# / .NET 10“ in `IDEEN.md`).

- **.NET 10 (LTS, Unterstützung bis November 2028)**, `net10.0-windows`, Oberfläche mit
  **Windows Forms** wie die Ausgangsliste. Nicht .NET 8: Dessen Unterstützung endet am
  10.11.2026.
- Abhängigkeiten nur mit Begründung in `STAND.md`, nur mit freier Lizenz (MIT, Apache-2.0,
  BSD). Vorgesehen: `Microsoft.Data.Sqlite` (Speicher), ein Testrahmen (z. B. xUnit),
  später `PDFsharp` (MIT) für den HEB-Bogen.
- **Kern ohne Oberfläche:** Datenmodell, Speichern, Prüfregeln und Demo-Daten in einem
  eigenen Projekt (`src/DokuLine.Kern/`), testbar ohne Fenster. Die Oberfläche
  (`src/DokuLine/`) ruft nur den Kern auf. Tests in `tests/`.
- **Vorbild und Bausteine: `KhaosDE/Ausgangsliste`** (privat, C#, Windows Forms; in der
  Sitzung als zweites Repository anhängen). Übernehmen und anpassen statt neu erfinden:
  `Stil.cs` (Farben, Schriften, Abstände), Karten- und Kopfbereich, Druckvorschau,
  Datenbank mit Mehrplatz-Schutz, Sicherung. **Beim Übernehmen bereinigen:** kein Logo,
  kein Name der Einrichtung oder des Wohnbereichs, keine Namen von Personen — auch nicht
  in Kommentaren (Regel 1 und 7). Gestaltung: `docs/gestaltung.md`.
- **Speicher:** SQLite-Datei im Datenordner `%APPDATA%\DokuLine\` (bis 0.29.1
  `%APPDATA%\Dokumentationssystem\`; beim ersten Start wird dessen Inhalt kopiert). Hinterlegte
  Bilder und zugeteilte Vorlagen ebenfalls dort, nie im Repository.
- **Demo-Daten:** lesbare Datei im Repository (`daten/demo/`), beim ersten Start geladen,
  über „Demo-Daten zurücksetzen“ wiederherstellbar.
- **Programmdatei:** eine einzelne `DokuLine.exe` (self-contained, single-file, **nicht**
  komprimiert — gepackte Dateien lösen öfter Virenwarnungen aus, Erfahrung Ausgangsliste).
- **Selbst-Update** über die Releases dieses Repositorys, Ablauf wie bisher (Vorbild
  `KhaosDE/N3XTBYT3`): GitHub Actions testet bei jedem Push nach `main` unter Windows, baut
  die `.exe` und veröffentlicht sie mit SHA-256-Prüfsumme als Release `build-<Laufnummer>`
  (nicht bei reinen `.md`-Änderungen). Die `.exe` fragt beim Start nach dem neuesten
  Release, lädt, prüft die Prüfsumme, tauscht sich aus und startet neu. Übergang bis zur
  Umbenennung des Repositorys in `DokuLine`: Jedes Release enthält die Datei zusätzlich als
  `Dokumentationssystem.exe`; der Updater kennt beide Repository-Adressen. Nur diese Adressen,
  kein anderer Netzzugriff. Abschaltbar (`--kein-update` und Einstellung). Schlägt etwas
  fehl, startet die alte Version.
- Die `.exe` ist nicht signiert; Windows SmartScreen warnt beim ersten Start. Im README in
  einfachen Worten erklären.

## Sprache

Oberfläche, Dokumentation und Commit-Nachrichten auf Deutsch, einfach und ohne unnötige
Fachsprache.
