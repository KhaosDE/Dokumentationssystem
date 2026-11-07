# Brücke zur Ausgangsliste

Stand: 07.11.2026. Regelt, wie die Ausgangsliste (`KhaosDE/Ausgangsliste`, privat) und das
Dokumentationssystem (dieses Repository, öffentlich) zusammenhängen. Gegenstück im Repository
der Ausgangsliste: `Ideen.md`, Abschnitt „Brücke zum Dokumentationssystem“.

## Ziel

Die Ausgangsliste wird ein **zuschaltbares Modul** des Dokumentationssystems (Eintrag
„Modul Ausgangsliste (mit Modulverwaltung)“ in `IDEEN.md`). Bereiche, die sie nicht brauchen,
schalten sie in den Einstellungen ab.

## Rollen (Entscheidung Tobi, 07.11.2026)

**Das Dokumentationssystem ist das Muttersystem.** Die Ausgangsliste bleibt daneben als
**eigenständige Version** bestehen — für Bereiche, die nur die Ausgangsliste brauchen.

| | Dokumentationssystem | Ausgangsliste (eigenständig) |
|---|---|---|
| Rolle | **Muttersystem:** Neue Funktionen rund um Ausgang, Unterbringung, Küchendienst entstehen hier zuerst, im Modul „Ausgangsliste“ | **Eigenständige Version:** bekommt die Funktionen über einen Abgleich aus dem Muttersystem |
| Zweck | Demo zum Vorführen, nur ausgedachte Daten | Programm für den Einsatz im Wohnbereich (Netzlaufwerk, Installer, Update-Ordner) |
| Repository | öffentlich | privat |
| Bleibt nur hier | alle übrigen Bereiche (Dokumentation, Medikation, HEB …) | eigene Hülle: Hauptfenster, Update über den Ordner `Update`, Installer, Logo, Excel-Arbeitsmappe |

**Übergang:** Bis das Modul gebaut ist (Bauplan Schritt 11), entwickelt sich die Ausgangsliste
wie bisher. Schritt 11 übernimmt einmal ihren Stand in das Modul. **Ab dann** entstehen neue
Ausgangs-Funktionen nur noch im Modul; in der Ausgangsliste werden nur noch Fehler behoben
und Abgleiche eingespielt.

## Damit der Abgleich leicht bleibt (Regeln für das Modul)

- Das Modul liegt in einem **eigenen Projekt** (`src/Dokusystem.Modul.Ausgangsliste/`) und hängt
  nur von festen Schnittstellen des Kerns ab (Bewohner lesen, Speicher, Stil, Druckvorschau,
  Einstellungen). So lässt es sich als Ganzes in die eigenständige Ausgangsliste übernehmen.
- Datei- und Klassennamen wie in der Ausgangsliste.
- Kein Bezug auf andere Bereiche des Dokumentationssystems im Modul selbst (Marken auf der
  Startseite usw. meldet das Modul über die Schnittstelle, der Kern zeigt sie an).
- Eine Liste der Änderungen je Version im Modul (`src/Dokusystem.Modul.Ausgangsliste/AENDERUNGEN.md`),
  damit der Abgleich weiß, was neu ist.

## Abgleich (Richtung: Dokumentationssystem → Ausgangsliste)

1. **Einmalig (Bauplan Schritt 11):** Stand der Ausgangsliste ins Modul übernehmen, bereinigt
   nach `CLAUDE.md` (kein Logo, kein Name der Einrichtung oder des Wohnbereichs, keine
   Personennamen — auch nicht in Kommentaren). Version unten eintragen.
2. **Danach:** Ändert sich das Modul, trägt der Chat im Repository der Ausgangsliste
   (`Ideen.md`) einen Eintrag „Abgleich mit Dokumentationssystem <Version>“ ein. Claude Code
   übernimmt dort die Änderungen in die eigenständige Ausgangsliste und passt sie an deren
   Hülle an.
3. Fehler, die in der eigenständigen Ausgangsliste auffallen und das Modul betreffen, werden
   **im Muttersystem** behoben (Eintrag in `IDEEN.md` hier) und dann abgeglichen.

**Einmalige Übernahme aus Ausgangsliste-Version:** 0.23.0 (Stand c57dd0e), übernommen in
Dokumentationssystem 0.14.0 am 07.11.2026. Was angepasst wurde:
`src/Dokusystem.Modul.Ausgangsliste/AENDERUNGEN.md`.
**Zuletzt an die Ausgangsliste abgeglichen:** noch nichts.

## Zuordnung

| Ausgangsliste (`src/Ausgangsliste/`) | Im Dokumentationssystem |
|---|---|
| `Stil.cs`, `Fensterdeko.cs`, `KopfBereich.cs`, `KartenRaster.cs`, `BewohnerKarte.cs` | Kern-Oberfläche (schon übernommen in 0.3.0 bzw. Gestaltung Stufe 2) |
| `Druckvorschau.cs` | Kern-Oberfläche (gemeinsame Druckvorschau für alle Ausdrucke) |
| `Datenbank.cs`, `Sicherung.cs`, `SicherungFenster.cs`, `Protokoll.cs` | Kern (Speicher, Sicherung, Protokoll) |
| `AufnahmeFenster.cs`, `BearbeitenFenster.cs`, `BewohnerAuswahlFenster.cs`, `DatumFenster.cs`, Entlassung, Zimmerwechsel | Kern: Bewohnerakte |
| `Demodaten.cs` | Kern und Modul (jeder Teil seine eigenen Demo-Daten) |
| `AusgangFenster.cs`, `AusgangText.cs` | Modul Ausgangsliste: Ausgangszeiten |
| `BewohnerKarte.cs` (Klasse `KartenText`) | Modul: `KartenText.cs`; die Karte selbst heißt im Modul `PlatzKarte` |
| `Verlauf.cs`, `VerlaufFenster.cs` | Modul: `Verlauf.cs`, Anzeige im Akte-Reiter „Ausgang“ und über „Verlauf …“ |
| `StatusFenster.cs`, `Platz.cs` (Anwesenheitsstatus) | Modul Ausgangsliste: Anwesenheit und Abwesenheit |
| `UnterbringungFenster.cs` | Modul Ausgangsliste: Unterbringung, Fristen, Rechtsregel |
| `Druckliste.cs` | Modul Ausgangsliste: Tagesliste |
| `Kuechendienst.cs`, `KuechendienstFenster.cs`, `KuechendienstDruck.cs`, `TauschFenster.cs`, `ZimmergruppenFenster.cs` | Untermodul Küchendienst |
| `DigitalerAusgang.cs`, `NachtberichtFenster.cs` | Untermodul Digitaler Ausgang |
| `EinstellungenFenster.cs` | Einstellungen des Dokumentationssystems, Abschnitt „Module“ |
| `Neuigkeiten.cs` („Was ist neu?“) | offen — bei Bedarf in den Kern |
| `Aktualisierung.cs` (Update-Ordner), Installer | **nicht** übernommen; das Dokumentationssystem hat ein eigenes Update |
| `Bilder/logo*.png/jpg` | **nie** übernehmen (Regel 7) |
