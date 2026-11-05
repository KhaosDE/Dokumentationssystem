# IDEEN — Dokumentationssystem

Aufträge aus den Chats. Gebaut wird nur, was `Status: freigegeben` hat, von oben nach
unten (Regel: `CLAUDE.md`, „Arbeitsweise“).

Aufbau eines Eintrags:

```
## <Titel>
Status: offen | freigegeben | erledigt (<Datum>)
Datum: <Datum>
Was: <ein bis drei Sätze>
Warum: <wofür Tobi es braucht>
Umfang: <was dazu gehört, was ausdrücklich nicht>
```

## Programmrahmen und Selbst-Update
Status: erledigt (05.11.2026)
Datum: 05.11.2026
Was: Lauffähiges Windows-Programm mit Hauptfenster, Datenordner, SQLite-Speicher,
Versionsanzeige und Selbst-Update über die Releases dieses Repositorys.
Warum: Grundlage für alles Weitere; Tobi will die Update-Funktion erproben.
Umfang:
- Aufbau, Build und Update wie in `CLAUDE.md`, Abschnitt „Technik“.
- Hauptfenster mit Navigation links (zunächst nur „Bewohner“; weitere Bereiche kommen über
  eigene Einträge), dauerhaft sichtbarem Hinweis „Demo — ausgedachte Daten“ und
  Versionsnummer.
- Beim Update: kurze Meldung „Neue Version … wird geladen“, danach Neustart; Ablauf in
  `update.log` im Datenordner.
- README in einfachen Worten: Herunterladen, SmartScreen-Warnung, Update abschalten.
- Der Build-Workflow läuft nicht bei Pushes, die nur `.md`-Dateien ändern (`paths-ignore`).
  Grund: Der Chat schreibt Aufträge direkt in `IDEEN.md` auf `main`; das soll kein neues
  Release erzeugen.
- Ausdrücklich nicht: Benutzerkonten, Anmeldung, Netzwerkbetrieb mit mehreren Rechnern.

## Bewohnerakte
Status: freigegeben
Datum: 05.11.2026
Was: Eine Akte pro Bewohner mit den wichtigsten Angaben auf einen Blick, dazu eine
Übersicht aller Bewohner des Wohnbereichs.
Warum: Kern der Demo. In DANlife stehen wichtige Angaben (z. B. Beschluss) an
pflegebezogenen Stellen, die im Wohnbereich ohne Pflege kaum jemand findet.
Umfang:
- Felder laut Tobi: Name, Zimmernummer, Geburtsdatum, Aufnahmedatum, Beschlussende,
  Hinweis auf Allergien, Platz für ein Bewohnerbild.
- Übersicht: alle Bewohner als Liste oder Kacheln mit Bild, Name und Zimmernummer; Klick
  öffnet die Akte. Bewohner anlegen und bearbeiten.
- Allergiehinweis mit drei Zuständen (`CLAUDE.md`, Regel 5), gut sichtbar oben in der Akte.
- Bewohnerbild: per Dateiauswahl hinterlegbar (Ablage im Datenordner); ohne Bild ein
  Platzhalter mit Initialen.
- Demo-Daten: etwa 10 ausgedachte Bewohner mit ausgedachten Zimmernummern, gemischt: mit
  und ohne Allergie, „keine bekannt“, „nicht erfasst“, mit und ohne Beschlussende.
- Datumsangaben im Format TT.MM.JJJJ; Prüfung, dass das Aufnahmedatum nicht vor dem
  Geburtsdatum liegt.
- **Noch nicht entschieden, nicht bauen:** Warnung vor Beschlussende, Angabe „offen geführt
  ja/nein“, Alter aus dem Geburtsdatum berechnet.
- Ausdrücklich nicht: Tagesdokumentation, Medikation, Ausgang (eigene Einträge).

## Tägliche Dokumentation
Status: offen
Datum: 05.11.2026
Was: Tägliche schriftliche Dokumentation je Bewohner. Zweiter Teil des Kerns.
Warum: Laut Tobi Hauptteil des Programms neben der Bewohnerführung.
Umfang: wird im Chat ausgearbeitet. Nicht bauen.
