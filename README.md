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
3. Doppelklicken. Der erste Start dauert ein paar Sekunden. Mehr ist nicht nötig: Die Datei
   bringt alles mit, was sie braucht.

### Die Warnung „Der Computer wurde durch Windows geschützt“

Beim ersten Start zeigt Windows diese blaue Warnung (SmartScreen). Der Grund: Die Datei
ist nicht „signiert“ – das ist eine Art Ausweis für Programme, der Geld kostet. Für eine
Demo lohnt sich das nicht. So startest du trotzdem:

1. Auf **Weitere Informationen** klicken.
2. Auf **Trotzdem ausführen** klicken.

Danach fragt Windows für diese Datei nicht mehr.

## Bedienung in Kürze

- Links stehen alle Bereiche. Was noch nicht gebaut ist, zeigt „Kommt in Version …“.
- **Bewohner**: alle Bewohner als Karten. Ein Klick öffnet die Akte; ihre Teile (Stammdaten,
  Berichte, Medikation …) liegen auf Reitern.
- In der Akte steht oben der **Allergiehinweis**: rot = Allergien eingetragen,
  grün = keine bekannt, gelb gestrichelt = noch nicht erfasst. Der Text sagt immer, was gilt.
- **Neuer Bewohner …** und **Bearbeiten …** (oben rechts) öffnen das Formular. Datum immer
  als TT.MM.JJJJ (z. B. 03.04.1965).
- **Demo-Daten zurücksetzen …** (unten links) stellt die ausgedachten Demo-Bewohner wieder
  her. Eigene Änderungen und hinterlegte Bilder gehen dabei verloren.
- **Einstellungen** (links): eigenes Kürzel, Update beim Start, eigenes Bild für den Kopf
  (z. B. ein Logo – die Datei wird nur in den Datenordner kopiert).

## Updates

Bei jedem Start fragt das Programm kurz bei GitHub nach, ob es eine neuere Version gibt –
nur bei den Releases dieses Repositorys, sonst geht es nirgends ins Internet.

- Gibt es eine neue Version, erscheint „Neue Version wird geladen …“ mit Fortschrittsbalken.
  Das Programm prüft die Datei (Prüfsumme), tauscht sich aus und startet neu.
- Ohne Internet oder bei einem Fehler startet einfach die vorhandene Version.
- Deine Daten bleiben beim Update erhalten (sie liegen im Datenordner, nicht neben der `.exe`).
- Welche Version läuft, steht unten links im Fenster.

**Update abschalten** – eine der beiden Möglichkeiten:

- Im Programm unter **Einstellungen** das Häkchen **„Beim Start nach Updates suchen“**
  entfernen.
- Oder die `.exe` mit `--kein-update` starten (Verknüpfung anlegen, Rechtsklick →
  Eigenschaften → bei „Ziel“ hinten ` --kein-update` anhängen).

### Von Version 0.2.0 auf 0.3.0

Ab Version 0.3.0 ist das Programm in C# geschrieben statt in Python. Die installierte
Version 0.2.0 holt sich die neue Version über ihr Update selbst – du musst nichts tun.
Falls das nicht klappt (z. B. weil das Update abgeschaltet ist), die neue `.exe` einmal über
den Link oben von Hand laden und die alte Datei ersetzen.

Die neue Version beginnt mit den Demo-Daten. Was du in 0.2.0 geändert hast, wird nicht
übernommen; die alte Datenbank bleibt aber unberührt im Datenordner liegen.

## Wo liegen die Daten?

Im Datenordner `%APPDATA%\Dokumentationssystem` (im Windows-Explorer oben in die
Adresszeile eingeben). Dort liegen:

- `dokumentationssystem.db` – die Datenbank,
- `bilder\` – hinterlegte Bewohnerbilder,
- `logo\` – das eigene Bild für den Kopf (falls gewählt),
- `einstellungen.json` – die Einstellungen,
- `update.log` – was beim Update passiert ist (zum Nachsehen, wenn etwas nicht klappt),
- `dokusystem.sqlite3` – nur falls vorhanden: die alte Datenbank der Version 0.2.0
  (wird nicht mehr benutzt).

## Für Entwickler

C# mit .NET 10 und Windows Forms. Aufbau:

| Ordner | Inhalt |
|---|---|
| `src/Dokusystem.Kern` | Datenmodell, Prüfregeln, Speicher (SQLite), Demo-Daten – ohne Fenster |
| `src/Dokusystem.Update` | Selbst-Update – das einzige Projekt mit Internetzugriff |
| `src/Dokusystem` | Oberfläche (Windows Forms), ergibt `Dokumentationssystem.exe` |
| `tests/Dokusystem.Tests` | Tests für Kern und Update (laufen auch unter Linux) |
| `tests/Dokusystem.Oberflaeche.Tests` | Tests für die Oberfläche (nur Windows) |
| `daten/demo` | Demo-Daten als lesbare Datei (werden in die `.exe` eingebaut) |

Mit dem .NET 10 SDK im Projektordner:

```bash
dotnet test Dokumentationssystem.sln
```

```bash
dotnet run --project src/Dokusystem
```

Die `.exe` baut GitHub Actions (`.github/workflows/tests-und-exe.yml`) bei jedem Push nach
`main` und veröffentlicht sie als Release `build-<Laufnummer>`.
