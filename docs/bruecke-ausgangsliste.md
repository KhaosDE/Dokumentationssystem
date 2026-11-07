# Brücke zur Ausgangsliste

Stand: 07.11.2026. Regelt, wie die Ausgangsliste (`KhaosDE/Ausgangsliste`, privat) und das
Dokumentationssystem (dieses Repository, öffentlich) zusammenhängen. Gegenstück im Repository
der Ausgangsliste: `Ideen.md`, Abschnitt „Brücke zum Dokumentationssystem“.

## Ziel

Die Ausgangsliste wird ein **zuschaltbares Modul** des Dokumentationssystems (Eintrag
„Modul Ausgangsliste (mit Modulverwaltung)“ in `IDEEN.md`). Bereiche, die sie nicht brauchen,
schalten sie in den Einstellungen ab.

## Rollen (Festlegung 07.11.2026, Tobi kann sie ändern)

| | Ausgangsliste | Dokumentationssystem |
|---|---|---|
| Zweck | Programm für den Einsatz im Wohnbereich, wird dafür vorbereitet (Netzlaufwerk, Installer, Update-Ordner) | Demo zum Vorführen, nur ausgedachte Daten |
| Repository | privat | öffentlich |
| Rolle bei Ausgangs-Funktionen | **Mutterprogramm:** Neue Funktionen rund um Ausgang, Unterbringung, Küchendienst entstehen hier zuerst | **Abbild:** übernimmt sie als Modul über einen Abgleich |

Warum so: Die Ausgangsliste ist in Vorbereitung für den echten Einsatz und wird laufend
weiterentwickelt (Version 0.23.0 am 07.11.2026). Das Dokumentationssystem ist eine Demo in
einem öffentlichen Repository. Beides in ein Repository zu legen, würde entweder die Demo-Regeln
(öffentlich, keine echten Daten) oder den Einsatz der Ausgangsliste behindern. Die Frage stellt
sich neu, wenn das Dokumentationssystem über eine Demo hinausgeht.

## Abgleich

1. Erscheint eine neue Version der Ausgangsliste mit Änderungen an Funktionen aus der Tabelle
   unten, trägt der Chat in `IDEEN.md` einen Eintrag „Abgleich mit Ausgangsliste <Version>“ ein.
2. Claude Code übernimmt die Änderungen in das Modul, **bereinigt** nach `CLAUDE.md` (kein Logo,
   kein Name der Einrichtung oder des Wohnbereichs, keine Personennamen — auch nicht in
   Kommentaren) und trägt unten den neuen Stand ein.
3. Fehler, die beim Abgleich im Code der Ausgangsliste auffallen, werden im Repository der
   Ausgangsliste gemeldet (dort `Ideen.md`), nicht nur im Modul behoben.

**Übernommen bis Ausgangsliste-Version:** noch keine (Modul nicht gebaut).

## Zuordnung

| Ausgangsliste (`src/Ausgangsliste/`) | Im Dokumentationssystem |
|---|---|
| `Stil.cs`, `Fensterdeko.cs`, `KopfBereich.cs`, `KartenRaster.cs`, `BewohnerKarte.cs` | Kern-Oberfläche (schon übernommen in 0.3.0 bzw. Gestaltung Stufe 2) |
| `Druckvorschau.cs` | Kern-Oberfläche (gemeinsame Druckvorschau für alle Ausdrucke) |
| `Datenbank.cs`, `Sicherung.cs`, `SicherungFenster.cs`, `Protokoll.cs` | Kern (Speicher, Sicherung, Protokoll) |
| `AufnahmeFenster.cs`, `BearbeitenFenster.cs`, `BewohnerAuswahlFenster.cs`, `DatumFenster.cs`, Entlassung, Zimmerwechsel | Kern: Bewohnerakte |
| `Demodaten.cs` | Kern und Modul (jeder Teil seine eigenen Demo-Daten) |
| `AusgangFenster.cs`, `AusgangText.cs` | Modul Ausgangsliste: Ausgangszeiten |
| `StatusFenster.cs`, `Platz.cs` (Anwesenheitsstatus) | Modul Ausgangsliste: Anwesenheit und Abwesenheit |
| `UnterbringungFenster.cs` | Modul Ausgangsliste: Unterbringung, Fristen, Rechtsregel |
| `Druckliste.cs` | Modul Ausgangsliste: Tagesliste |
| `Kuechendienst.cs`, `KuechendienstFenster.cs`, `KuechendienstDruck.cs`, `TauschFenster.cs`, `ZimmergruppenFenster.cs` | Untermodul Küchendienst |
| `DigitalerAusgang.cs`, `NachtberichtFenster.cs` | Untermodul Digitaler Ausgang |
| `EinstellungenFenster.cs` | Einstellungen des Dokumentationssystems, Abschnitt „Module“ |
| `Neuigkeiten.cs` („Was ist neu?“) | offen — bei Bedarf in den Kern |
| `Aktualisierung.cs` (Update-Ordner), Installer | **nicht** übernommen; das Dokumentationssystem hat ein eigenes Update |
| `Bilder/logo*.png/jpg` | **nie** übernehmen (Regel 7) |
