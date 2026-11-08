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
  als TT.MM.JJJJ (z. B. 03.04.1965). **Offen geführt** hat drei Zustände: ja, nein, nicht erfasst.
- In den Stammdaten steht das **Alter** neben dem Geburtsdatum. Oben in der Akte stehen Marken:
  „offen geführt“, „offen geführt: nicht erfasst“ und Hinweise der Module – etwa, wenn die
  Ausgangsliste etwas anderes zur Unterbringung sagt als die Akte („bitte klären“).
- **Seitenmenü einklappen:** Der Knopf mit den drei Strichen oben links macht das Menü schmal
  (nur Symbole, der Name erscheint beim Darüberfahren). Das Programm merkt sich die Wahl.
- **Übersicht** (links oben, öffnet beim Start): oben „Heute“ mit Abwesenden, den Terminen des
  Tages und den Änderungen in der Medikation; darunter alle Bewohner als Karten mit den Marken
  des Tages (z. B. „Beschluss endet in … Tagen“, „Medikation: Pause beginnt“, „heute 10:00 …“,
  „abwesend: …“). Klick auf eine Karte öffnet die Akte. Die Frist für „Beschluss endet bald“
  steht in den Einstellungen.
- **Berichte** schreibt man in der Akte (oben „Neuer Bericht …“ oder Reiter „Berichte“) oder per
  Rechtsklick auf eine Bewohnerkarte → „Neuer Bericht …“. Zwei Kennzeichen: **Wichtig** (bleibt in der Akte hervorgehoben)
  und **Übergabe** (erscheint im Bereich „Übergabe“, so lange wie gewählt). Korrigiert wird
  mit **Nachtrag …** – der ursprüngliche Text bleibt stehen.
- **Übergabe** (links): nur die Übergabe-Einträge, oben „Wohnbereich allgemein“, darunter je
  Bewohner. Mit eigenem Kürzel (Einstellungen) lassen sie sich als gelesen bestätigen,
  verlängern oder erledigen. „Übergabeblatt drucken …“ druckt die offenen Einträge.
- **Aufgaben** (links, mit Zähler): was die nächste Schicht (oder wer Zeit hat) tun soll.
  Aufgaben bleiben stehen, bis jemand sie als erledigt markiert; schafft man es nicht,
  „An die nächste Schicht weitergeben …“ mit kurzer Notiz. Die ersten offenen Aufgaben stehen
  auch über den Bewohnerkarten. Aus einem Bericht heraus: „Aufgabe daraus …“.
- **Angemeldet als …** (oben rechts): wer gerade am Programm sitzt – eine der ausgedachten
  Kolleginnen und Kollegen, ohne Passwort (nur für die Vorführung). Das Kürzel steht dann bei
  Berichten, Übergabe und Aufgaben.
- **Nachrichten** (links, mit Zähler): an Personen oder Teams, mit Antworten im selben
  Verlauf, optional zu einem Bewohner (in der Akte „Nachrichten …“). Alles bleibt im Programm,
  kein E-Mail-Versand. Nachrichten werden nicht gelöscht, nur archiviert.
- **Medikamente** (links): Demo-Bestand gängiger Medikamente mit Quelle (Fachinformation),
  **nicht zum Verordnen verwenden**. Suchen, ansehen, eigene Einträge anlegen, ändern (mit
  Verlauf). Die Quelle lässt sich kopieren und im Browser öffnen; das Programm selbst geht
  nicht ins Internet.
- **Medikation** (Reiter in der Akte): „Was gilt am …“ aus Grundverordnung, Pausen und
  Stufenplänen. Die Grundverordnung wird nie überschrieben; Pause, Stufenplan und Absetzen sind
  eigene Einträge mit Anordnung. **Demo — nicht zum Stellen verwenden.**
- **Therapie** (links): Angebote mit Terminen, Teilnehmern und freien Plätzen; Bewohner
  zuteilen (mit Warnung bei Überschneidung), Teilnahme beenden, Änderungen „ab Datum“.
- **Wochenplan** (Reiter in der Akte): die Regelwoche mit zugeteilten Therapien und freien
  Einträgen; Teilnahme an Therapien eintragen; Änderungen gelten „ab Datum“; Ausdruck in
  großer Schrift nur mit Name und Plan.
- **Biografie** (Reiter in der Akte): Lebensgeschichte in festen Abschnitten als Fließtext.
  „Vom Bewohner angegeben, nicht überprüft“ lässt sich je Abschnitt ankreuzen (Hinweis auf die
  Formulierung „gibt an, dass …“). Leere Abschnitte stehen unten als „Fehlende Angaben“.
  „Als Fließtext kopieren“ legt alles als reinen Text in die Zwischenablage. Jede Änderung ist
  eine neue Fassung mit Datum und Kürzel („Verlauf …“).
- **Reflexion** (Reiter in der Akte): je Monat ein kurzer Text je Lebensbereich, daneben die
  Berichte des Monats zu diesem Bereich und oben die Teilnahme an Therapien laut Wochenplan.
  Das Programm zählt die Zeichen und gibt Stilhinweise (z. B. Semikolon, Datum im Text), ändert
  aber nichts selbst. „Speichern“ hält einen Entwurf fest, „Als fertig markieren“ setzt ein
  Mensch.
- **HEB** (Reiter in der Akte): HEB-Bogen A, B oder C für einen Zeitraum. Je Lebensbereich die
  Felder des Bogens mit Zeichenzähler und Stilhinweisen, daneben die Berichte des Zeitraums, der
  Wochenplan und die fertigen Monatsreflexionen. Textbausteine per Klick, der Hilfebedarf wird
  nie vorgeschlagen. „Speichern“ (Entwurf), „Als fertig markieren“ (ein Mensch), „Drucken …“ und
  „PDF ausfüllen …“: Unter Einstellungen → „HEB-VORLAGEN“ teilt man je Bogenart eine PDF mit
  Formularfeldern zu; ausgefüllt wird eine Kopie im Datenordner (Ordner `heb`). Ist ein HEB fällig,
  steht das in der Akte und auf der Übersicht.
- **Kalender** (links): Monat, Woche, Tag; Termine von Bewohnern, Wohnbereich, Heimfahrten und
  Tagesausflüge; Termine eines Bewohners stehen auch in seinem Wochenplan. Kein Abgleich mit
  anderen Kalendern.
- **Ausgangsliste** (links, Modul): je Bewohner Ausgangszeiten (allein / mit Bewohner),
  Anwesenheit und Abwesenheit mit Grund und Frist, Unterbringung mit Fristen (eine
  Ausgangsbeschränkung geht nur mit Beschluss oder Forensik), Verlauf je Person und die
  druckbare Tagesliste. Dazu Küchendienst (Plan reihum, Tausch) und Digitaler Ausgang (per
  Klick, Nachtbericht zum Kopieren). In der Akte steht der Reiter „Ausgang“. Unter
  **Einstellungen → Module** lässt sich das Modul (und jeder Teil) abschalten; es verschwindet
  dann ganz, die Daten bleiben.
- Die Wortliste für den Hinweis „Soll das in die Übergabe?“ lässt sich ergänzen: eine Datei
  `uebergabe-woerter.txt` in den Datenordner legen, ein Wort pro Zeile.
- **Statusmeldung** (Akte → „Status …“): kurzer Text mit „gilt bis“, z. B. „beim Arzttermin“.
  Bereich, Kürzel und Uhrzeit kommen dazu. Sie steht auf der Karte und oben in der Akte – in
  allen Bereichen –, bis sie abläuft oder eine neue sie ersetzt. Der Verlauf bleibt.
- **Mehrere Rechner (Demo)**: In den Einstellungen unter „MEHRPLATZ“ bekommt jeder Rechner eine
  **Bereichskennung** (z. B. „Wohnbereich A“, „Therapie“); die Übersicht zeigt zuerst die
  Bewohner dieses Bereichs, umschaltbar auf „alle Bereiche“. Mit **„Gemeinsamen Ordner wählen
  …“** liegt die Datenbank in einem Ordner, den mehrere Rechner erreichen (z. B. ein
  Netzlaufwerk); das gilt nach dem Neustart. Jeder Rechner liest Änderungen der anderen
  spätestens nach 30 Sekunden nach. Haben zwei denselben Eintrag geändert, zeigt das Programm
  beide Fassungen und lässt wählen – es überschreibt nichts still. Zum Ausprobieren ohne
  zweiten Rechner: das Programm zweimal starten. Auch im gemeinsamen Ordner nur ausgedachte
  Daten; Anmeldung, Rechte und Verschlüsselung gibt es nicht (das gehört zum Echtbetrieb).
- **Demo-Daten zurücksetzen …** (unten links) stellt die ausgedachten Demo-Bewohner wieder
  her. Eigene Änderungen und hinterlegte Bilder gehen dabei verloren – im gemeinsamen Ordner
  für alle Rechner.
- **Einstellungen** (links): wer angemeldet ist, Update beim Start, Module ein- und
  ausschalten, Frist „Beschluss endet bald“, eigenes Bild für den Kopf (z. B. ein Logo – die Datei wird nur in den
  Datenordner kopiert).

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

- `dokumentationssystem.db` – die Datenbank (oder im gemeinsamen Ordner, falls in den
  Einstellungen gewählt),
- `bilder\` – hinterlegte Bewohnerbilder,
- `logo\` – das eigene Bild für den Kopf (falls gewählt),
- `einstellungen.json` – die Einstellungen,
- `uebergabe-woerter.txt` – nur falls angelegt: eigene Wörter für den Übergabe-Hinweis,
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
