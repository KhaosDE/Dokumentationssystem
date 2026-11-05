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

## Arbeitsweise

- Zu Beginn jeder Sitzung zuerst `IDEEN.md`, dann `STAND.md`.
- Gebaut wird nur, was in `IDEEN.md` `Status: freigegeben` hat. Fertige Einträge auf
  `Status: erledigt (<Datum>)` setzen.
- Lässt ein freigegebener Eintrag etwas offen: die einfachere Lösung nehmen und in
  `STAND.md`, Abschnitt „Entscheidungen“, mit Begründung festhalten.
- Anhalten und fragen nur, wenn eine Regel aus „Unverhandelbar“ verletzt würde oder
  etwas Unumkehrbares nötig ist.
- Kleine, lauffähige Schritte: ändern, testen, committen, `STAND.md` aktualisieren.
- Zweig: Arbeitszweig mit Pull Request, `main` enthält nur Geprüftes.
- Jede ausgelieferte Änderung bekommt eine neue Versionsnummer (in `STAND.md`).

## Stand-Dateien (Brücke zu den Chats)

| Datei | Art | Inhalt |
|---|---|---|
| `STAND.md` | wird **überarbeitet** | Version, was fertig ist, offene Punkte, Entscheidungen, Testliste für Tobi |
| `SITZUNGEN.md` | wird **angehängt** | ein Einzeiler pro Sitzung: Datum — was erledigt wurde |
| `IDEEN.md` | wird überarbeitet | Aufträge aus den Chats, jeder mit Status |

## Technik

**Noch offen** (Entscheidung Tobi steht aus, siehe `STAND.md`). Fest steht nur: Das
Programm bekommt eine **Update-Funktion**, die über dieses öffentliche Repository läuft.

## Sprache

Oberfläche, Dokumentation und Commit-Nachrichten auf Deutsch, einfach und ohne unnötige
Fachsprache.
