# Änderungen im Modul „Ausgangsliste“

Für den Abgleich mit der eigenständigen Ausgangsliste (`docs/bruecke-ausgangsliste.md`): je
Version des Dokumentationssystems, was sich im Modul geändert hat. Neueste oben.

## 0.14.0 (07.11.2026) — einmalige Übernahme

Übernommen aus der Ausgangsliste, Version 0.23.0 (Stand c57dd0e), bereinigt nach `CLAUDE.md`.

Gleich geblieben (Datei- und Klassennamen wie in der Ausgangsliste):
- `AusgangText` (Bausteine „2 Allein / 1 MBW / (Info)“), `Verlauf` (Texte), `KartenText`
  (Stundenteile, Aufteilung, Info, Hinweise, Frist eines Grundes),
  `Platz` (Status, Unterbringung, Fristen, Rechtsregel „Ausgangsbeschränkung nur mit Beschluss
  oder Forensik“), `Kuechendienst` (Planen, Karenz, Gruppen, Tausch), `DigitalerAusgang`.
- Fenster: `AusgangFenster`, `StatusFenster`, `UnterbringungFenster`, `NachtberichtFenster`,
  `KuechendienstFenster`, `TauschFenster`, `ZimmergruppenFenster`; Ausdrucke `Druckliste`,
  `KuechendienstDruck`.

Angepasst an das Dokumentationssystem:
- **Eine Bewohnerliste:** Bewohner, Zimmer und Aufnahme kommen aus dem Kern. `Platz` ist der
  Bewohner plus die Angaben des Moduls (Tabelle `al_plaetze`); freie Plätze, Zimmerart,
  Aufnahme/Entlassung/Zimmerwechsel und der Mehrplatz-Schutz entfallen.
- **Unterbringung:** Der Beschluss steht in der Bewohnerakte (Beschlussende). Wählt man im Modul
  „Beschluss“, wird er dort eingetragen; „offen geführt“ und „Forensik“ setzen die Akte auf
  „kein Beschluss“. Neu: `Unterbringung.NichtErfasst`, wenn die Akte „nicht erfasst“ sagt –
  dann ist eine Ausgangsbeschränkung nicht zulässig, bis das geklärt ist.
- **Speicher:** `Datenbank` ist eine Klasse auf der Datenbank des Kerns (Tabellen mit Vorsilbe
  `al_`), keine eigene Datei. Einstellungen in den Verwaltungsangaben (`ausgangsliste:…`).
  Änderungen am Platz prüfen dort die Regeln und schreiben den Verlauf (mit Kürzel statt
  Windows-Anmeldenamen).
- **Küchendienst:** Regel für Einzelzimmer entfällt (keine Zimmerart).
- **Digitaler Ausgang:** Beginn und Ende zur aktuellen Uhrzeit, Handzeichen = Kürzel des
  angemeldeten Benutzers (statt `UhrzeitFenster`).
- **Karten:** `PlatzKarte` (in der Ausgangsliste `BewohnerKarte`) im Aufbau der Karten des
  Dokumentationssystems; Marken über `AusgangsMarken`.
- **Ein- und ausschalten** über die Modulverwaltung (`AusgangslisteModul`, `AusgangslisteDaten`):
  Modul, Küchendienst und Digitaler Ausgang je mit eigenem Häkchen.
- Rückfragen und Meldungen über ersetzbare Eigenschaften (`Rueckfrage`, `Aktionen.Melden`),
  damit die Oberfläche testbar ist.
- Nicht übernommen: Protokoll, Sicherung, Update-Ordner, Installer, Logo, „Was ist neu?“.
