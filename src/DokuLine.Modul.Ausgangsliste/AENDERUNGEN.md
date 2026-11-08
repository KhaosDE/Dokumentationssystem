# Änderungen im Modul „Ausgangsliste“

Für den Abgleich mit der eigenständigen Ausgangsliste (`docs/bruecke-ausgangsliste.md`): je
Version von DokuLine, was sich im Modul geändert hat. Neueste oben.

## 0.30.0 (08.11.2026)

- `AusgangslisteSeite` zeigt eine **Liste** statt Karten (Bauplan Schritt 27 von
  DokuLine), angelehnt an `Druckliste`: neue Klassen `ListenZeile` (eine Zeile je
  Bewohner: Zimmer, Name, Ausgang, Unterbringung, heute von–bis mit Hz., Stand jetzt; Knöpfe
  „raus allein“, „raus mit Bewohner“, „zurück“, „…“ mit Menü) und `ListenKopf` (Spaltenköpfe).
  Unterwegs oben (Überfällige zuerst), Filter „nur unterwegs“ und „nur mit Beschränkung“.
- `Aktionen`: die Abläufe (Ausgang ändern, Anwesenheit, Unterbringung, Verlauf, Beginnen,
  Zurück) sind eigene Methoden; `Verbinden` gibt es für `PlatzKarte` und `ListenZeile`.
- `PlatzTexte` (in `PlatzKarte.cs`): Ausgangs-, Unterbringungs- und Info-Texte für Karte und Zeile.
- `PlatzKarte` bleibt für den Reiter „Ausgang“ in der Akte.

## 0.21.0 (08.11.2026)

- `AusgangslisteSeite`: keine feste Höchstbreite mehr (880); die Seite füllt die Breite des
  Fensters (Bauplan Schritt 18 von DokuLine).

## 0.19.0 (08.11.2026)

- `Platz.OffenGefuehrtLautAkte` und `Platz.Widerspruch`: „offen geführt“ aus der Bewohnerakte wird
  mit der Unterbringung des Moduls verglichen. Widersprüche erscheinen als Marke („bitte
  klären“); geändert wird nichts automatisch.

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

Angepasst an DokuLine:
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
- **Karten:** `PlatzKarte` (in der Ausgangsliste `BewohnerKarte`) im Aufbau der Karten in
  DokuLine; Marken über `AusgangsMarken`.
- **Ein- und ausschalten** über die Modulverwaltung (`AusgangslisteModul`, `AusgangslisteDaten`):
  Modul, Küchendienst und Digitaler Ausgang je mit eigenem Häkchen.
- Rückfragen und Meldungen über ersetzbare Eigenschaften (`Rueckfrage`, `Aktionen.Melden`),
  damit die Oberfläche testbar ist.
- Nicht übernommen: Protokoll, Sicherung, Update-Ordner, Installer, Logo, „Was ist neu?“.
