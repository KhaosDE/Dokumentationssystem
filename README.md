# Dokumentationssystem (Demo)

Vorführ-Version eines Dokumentationssystems für einen Wohnbereich: Bewohnerakte und
tägliche Dokumentation in einem Programm.

**Alle Daten sind ausgedacht.** Das Programm ist nicht für echte Bewohnerdaten gedacht
und nicht dafür freigegeben. Oben im Fenster steht deshalb immer „Demo — ausgedachte Daten“.

Stand der Entwicklung: [`STAND.md`](STAND.md).

## Herunterladen und starten (Windows)

1. Die neueste Version laden:
   <https://github.com/KhaosDE/Dokumentationssystem/releases/latest/download/Dokumentationssystem.exe>
2. Die Datei in einen Ordner legen, in dem du schreiben darfst – zum Beispiel
   „Dokumente“ oder „Downloads“, **nicht** „Programme“. (Das Update tauscht die Datei
   dort aus.)
3. Doppelklicken. Der erste Start dauert ein paar Sekunden.

### Die Warnung „Der Computer wurde durch Windows geschützt“

Beim ersten Start zeigt Windows diese blaue Warnung (SmartScreen). Der Grund: Die Datei
ist nicht „signiert“ – das ist eine Art Ausweis für Programme, der Geld kostet. Für eine
Demo lohnt sich das nicht. So startest du trotzdem:

1. Auf **Weitere Informationen** klicken.
2. Auf **Trotzdem ausführen** klicken.

Danach fragt Windows für diese Datei nicht mehr.

## Updates

Bei jedem Start fragt das Programm kurz bei GitHub nach, ob es eine neuere Version gibt –
nur bei den Releases dieses Repositorys, sonst geht es nirgends ins Internet.

- Gibt es eine neue Version, erscheint „Neue Version wird geladen …“ mit Fortschrittsbalken.
  Das Programm prüft die Datei (Prüfsumme), tauscht sich aus und startet neu.
- Ohne Internet oder bei einem Fehler startet einfach die vorhandene Version.
- Deine Daten bleiben beim Update erhalten (sie liegen im Datenordner, nicht neben der `.exe`).
- Welche Version läuft, steht unten links im Fenster.

**Update abschalten** – eine der beiden Möglichkeiten:

- Im Programm unten links das Häkchen **„Beim Start nach Updates suchen“** entfernen.
- Oder die `.exe` mit `--kein-update` starten (Verknüpfung anlegen, Rechtsklick →
  Eigenschaften → bei „Ziel“ hinten ` --kein-update` anhängen).

## Wo liegen die Daten?

Im Datenordner `%APPDATA%\Dokumentationssystem` (im Windows-Explorer oben in die
Adresszeile eingeben). Dort liegen:

- `dokusystem.sqlite3` – die Datenbank,
- `einstellungen.json` – die Einstellungen,
- `update.log` – was beim Update passiert ist (zum Nachsehen, wenn etwas nicht klappt).

## Für Entwickler: Start aus dem Quelltext

Python 3.12, dann im Projektordner:

```bash
pip install -r requirements.txt
```

```bash
python -m dokusystem
```

Tests:

```bash
python -m unittest
```

Die `.exe` baut GitHub Actions (`.github/workflows/tests-und-exe.yml`) bei jedem Push nach
`main` und veröffentlicht sie als Release `build-<Laufnummer>`.
