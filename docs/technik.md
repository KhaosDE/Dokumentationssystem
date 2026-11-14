# Technik im Einzelnen

Ergänzt `docs/aufbau.md` um Abläufe, die im Code nur kurz erwähnt sind.

## Selbst-Update (`src/DokuLine.Update/Aktualisierung.cs`)

Vorbild: KhaosDE/N3XTBYT3; Ablauf wie in der Python-Version bis 0.2.0.

1. Beim Start fragt das Programm die Adresse `ApiNeueste`, ob es einen neueren Build gibt (Zeitlimit wenige
   Sekunden; ohne Netz geht es einfach weiter). Antwortet die Adresse des umbenannten Repositorys (noch) nicht,
   fragt es die alte (`ApiNeuesteAlt`).
2. Wenn ja: Meldung „Neue Version wird geladen“, dann werden `DokuLine.exe` (oder, in älteren Releases,
   `Dokumentationssystem.exe`) und die Prüfsumme dazu geladen und verglichen.
3. Die laufende `.exe` wird in „….exe.alt“ umbenannt (das erlaubt Windows auch im Betrieb), die neue unter dem
   Namen der laufenden Datei an ihren Platz gelegt und gestartet; dieses Programm endet. Die neue Version löscht
   beim Start die alte Datei.

Übergang beim Umbenennen: Das Repository heißt noch „Dokumentationssystem“ und wird danach in „DokuLine“
umbenannt. Erlaubt sind deshalb beide Adressen und beide Dateinamen. Ältere Installationen (bis 0.29.1) suchen nur
`Dokumentationssystem.exe` – der Workflow legt diese Datei deshalb zusätzlich in jedes Release.

Jeder Schritt und jeder Fehler steht in `update.log` im Datenordner. Bei einem Fehler startet das Programm ganz
normal in der alten Version. Abschalten: Häkchen „Beim Start nach Updates suchen“ (`einstellungen.json`) oder
`--kein-update`.

## Betriebsmodus (`src/DokuLine.Kern/Betrieb.cs`)

Eine Datei `betrieb.json` im Programmordner schaltet den Betrieb ein (Schlüssel `datenordner`, `sicherungsordner`,
`sperre_minuten`, `protokoll_monate`, `sicherung_um`; Anleitung in `docs/betrieb.md`). Im Betrieb liegt die Datenbank im
gemeinsamen Datenordner (WAL), die Einstellungsdatei je Benutzer in `%APPDATA%\DokuLine`; das angemeldete Kürzel nur im
Arbeitsspeicher (`Sitzung`). Kommandozeile (nur im Betrieb): für die Aufgabenplanung `--sicherung` (Dienstkonto) und
`--update-einspielen` (SYSTEM), ohne Fenster; für den Verwalter `--update-herunterladen`, `--update-vorbereiten <Datei>`
und `--ruecksichern <Sicherung.zip>`, Ergebnis in einem Meldungsfenster. Protokoll: Sicherung und Rücksicherung in
`update.log` im Sicherungsordner, Update in `<Programmordner>\update\update.log`.

## Einstellungsdatei (`src/DokuLine.Kern/Einstellungen.cs`)

Lesbare Datei `einstellungen.json` im Datenordner:

| Schlüssel | Bedeutung | Vorgabe |
|---|---|---|
| `auto_update` | beim Start nach einer neuen Version fragen | ja |
| `kuerzel` | eigenes Kürzel (Anmeldung) | leer |
| `logo` | Dateiname des eigenen Bildes für den Kopf im Ordner „logo“ | keins |
| `beschluss_frist_tage` | Startseite: „Beschluss endet bald“ ab so vielen Tagen vorher | 90 |
| `heb_frist_monate` | HEB-Frist: Vorschlag „letzter Bogen plus so viele Monate“ (ohne Bewilligung) | 12 |
| `bereich` | Mehrplatz: Bereichskennung dieses Rechners (bis 0.29.6 „Wohnbereich A“) | „R1“ |
| `datenbank_ordner` | Mehrplatz: gemeinsamer Ordner der Datenbank; leer = lokal im Datenordner | leer |
| `menue_eingeklappt` | Seitenmenü nur mit Symbolen | nein |
| `nachschlage_ansicht` | Formular „Medikament“: eingebettete Nachschlage-Ansicht des BfArM | ja |
| `wartezeit_minuten` | Ausgangsliste: so viele Minuten nach der Rückkehrzeit „Abgängigkeit melden?“ (5–240) | 30 |
| `kein_bericht_wochen` | Akte, Reiter „Maßnahmen“: „seit … Wochen kein Bericht“ je Lebensbereich (1–26) | 4 |
| `heb_erinnern_wochen` | HEB-Frist: erste Stufe (Aufgabe, Marke in Hinweis-Farbe) so viele Wochen vorher (1–26) | 8 |
| `heb_achtung_wochen` | HEB-Frist: zweite Stufe (Achtung-Farbe, Übersicht) so viele Wochen vorher (1–26, höchstens wie oben) | 4 |

Eine kaputte oder fremde Datei führt nie zum Absturz; dann gelten die Vorgaben. Dieselbe Datei hat schon die
Python-Version (bis 0.2.0) benutzt.
