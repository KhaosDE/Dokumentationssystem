# DokuLine (Demo)

**Betreuung auf Augenhöhe**

Vorführ-Version von DokuLine, einem Dokumentationsprogramm für einen Wohnbereich: Bewohnerakte
und tägliche Dokumentation in einem Programm. Bis Version 0.29.1 hieß das Programm
„Dokumentationssystem“.

**Alle Daten sind ausgedacht.** Das Programm ist nicht für echte Bewohnerdaten gedacht
und nicht dafür freigegeben. Oben im Fenster steht deshalb immer „Demo — ausgedachte Daten“.

Stand der Entwicklung: [`STAND.md`](STAND.md).

## Herunterladen und starten (Windows)

1. Die neueste Version laden:
   <https://github.com/KhaosDE/Dokumentationssystem/releases/latest/download/DokuLine.exe>
   (Wer schon eine `Dokumentationssystem.exe` hat, braucht nichts zu tun: Sie holt sich das
   Update selbst und behält ihren Dateinamen.)
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
- **Tagesplan** (links, unter „Übersicht“): alle Termine von heute nach Uhrzeit oder nach
  Bewohner. Bei Therapien „gegangen“, „abgesagt“, „ausgefallen“ (wie im Wochenplan); nach Beginn
  ohne Bestätigung „noch offen“. Nach „gegangen“ zeigen Karte und Akte bis zum Ende „in Therapie:
  … bis …“.
- **Übergabe** (links): nur die Übergabe-Einträge, oben „Wohnbereich allgemein“, darunter je
  Bewohner. Mit eigenem Kürzel (Einstellungen) lassen sie sich als gelesen bestätigen,
  verlängern oder erledigen. „Übergabeblatt drucken …“ druckt die offenen Einträge.
- **Visiten** (links, unter „Übergabe“): was in der Visite besprochen wurde, geänderte Anordnungen
  und nötige Facharzttermine, je Bewohner; Liste mit Filter nach Bewohner und Zeitraum. Nach dem
  Speichern zeigt das Programm sofort die Aufgaben dazu: Medikament anlegen, im Medikamentenplan
  eintragen (das passende Formular mit Arzt und Datum der Visite) oder den Facharzttermin in den
  Kalender eintragen (auf Wunsch mit Fahrt). Die Medikation ändert das Programm nicht selbst.
- **Aufgaben** (links, mit Zähler): was die nächste Schicht (oder wer Zeit hat) tun soll,
  gruppiert nach Schicht (oben die laufende). Aufgaben bleiben stehen, bis jemand sie als erledigt
  markiert; schafft man es nicht, „An die nächste Schicht weitergeben …“ mit kurzer Notiz. Die
  ersten offenen Aufgaben stehen auch über den Bewohnerkarten. Aus einem Bericht heraus:
  „Aufgabe daraus …“. **Standardaufgaben** (z. B. Rundgänge im Nachtdienst) entstehen zu Beginn
  jeder Schicht von selbst; nicht erledigte bleiben „aus der Nachtschicht offen“, bis jemand sie
  erledigt oder mit Begründung schließt. Die Schichtzeiten stehen in den Einstellungen.
- **Angemeldet als …** (oben rechts): wer gerade am Programm sitzt – eine der ausgedachten
  Kolleginnen und Kollegen, ohne Passwort (nur für die Vorführung). Das Kürzel setzt das
  Programm überall selbst („eingetragen von …“, nicht änderbar); Fahrer, Teilnehmer und
  Begleitung wählt man aus den Mitarbeitern. Ohne Anmeldung lässt sich nur ansehen.
- **Nachrichten** (links, mit Zähler): an Personen oder Teams, mit Antworten im selben
  Verlauf, optional zu einem Bewohner (in der Akte „Nachrichten …“). Alles bleibt im Programm,
  kein E-Mail-Versand. Nachrichten werden nicht gelöscht, nur archiviert.
- **Medikamentendatenbank** (Einstellungen → „Medikamente“): Demo-Bestand gängiger Medikamente
  mit Quelle (Fachinformation), **nicht zum Verordnen verwenden**. Suchen, ansehen, eigene
  Einträge anlegen, ändern (mit Verlauf), sperren. Wer ein Medikament anlegt, prüft es selbst
  gegen eine offizielle Quelle und bestätigt es mit Quelle und Prüfdatum; ungeprüfte sind beim
  Verordnen gekennzeichnet. Über der Maske eine kleine **Nachschlage-Ansicht des BfArM**: Text
  markieren, Feld anklicken, „In Feld übernehmen“. Sie zeigt nur Seiten des BfArM und lässt sich in
  den Einstellungen abschalten (dann „Im Browser öffnen“).
- **Medikamentenplan** (Reiter in der Akte): „Was gilt am …“ als Tabelle – Medikament,
  morgens bis nachts, verordnet von, seit, Status; Bedarfsmedikation darunter; sortierbar. Ein
  Klick auf eine Zeile zeigt Einzelheiten und Verlauf; dort Pause, Stufenplan, Absetzen. Die
  Grundverordnung wird nie überschrieben. **Demo — nicht zum Stellen verwenden.**
- **Therapie** (links): Angebote mit Terminen, Teilnehmern und freien Plätzen; Bewohner
  zuteilen (mit Warnung bei Überschneidung), Teilnahme beenden, Änderungen „ab Datum“. Jedes
  Angebot gehört der Therapie oder einem Wohnbereich (Filter „Anbieter“); alle sehen alles,
  ändern und zuteilen darf nur der Anbieter.
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
  Tagesausflüge; Termine eines Bewohners stehen auch in seinem Wochenplan. Zwei Kalender:
  „Bereich“ und „Haus“ (Umschalter oben; Haus-Termine grau). Kein Abgleich mit anderen Kalendern.
- **Fahrzeuge** (Übersicht und Kalender): je Firmenwagen „vor Ort“ oder „unterwegs seit …,
  zurück geplant …“ mit „losgefahren“ und „zurück“; „Neue Fahrt …“ mit Warnung bei Doppelbuchung.
  Mitfahrende stehen im Tagesplan und sind während der Fahrt abwesend. Die Fahrzeugliste steht in
  den Einstellungen.
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
  Daten; Passwörter und Verschlüsselung gibt es nicht (das gehört zum Echtbetrieb).
- **Demo-Daten zurücksetzen …** (unten links) stellt die ausgedachten Demo-Bewohner wieder
  her. Eigene Änderungen und hinterlegte Bilder gehen dabei verloren – im gemeinsamen Ordner
  für alle Rechner.
- **Bezugspflege und Einzeltrainer:** In der Akte unter „Stammdaten“ („Bezug ändern …“). Auf
  den Karten zeigt ein farbiger Streifen die Bezugspflege, ein Punkt den Einzeltrainer – in der
  Farbe der Person (Einstellungen → „Meine Farbe“), immer mit Kürzel. In der Übersicht filtern
  „Meine Bezugsbewohner“ und „Meine Einzeltrainings“.
- **Anmelden:** Beim Start mit dem Kürzel (Demo: ohne Passwort; die ausgedachten Mitarbeiter
  stehen im Anmeldefenster). Oben rechts „Angemeldet als …“ → „Abmelden / Benutzer wechseln“.
  Was jemand darf, hängt an seiner Position (Einstellungen → „VERWALTUNG“ → „Rechte …“). Ohne
  Recht bleiben Knöpfe grau und sagen beim Darüberfahren, warum.
- **Logo der Einrichtung:** Oben rechts im Kopf steht ein Platzhalter. Ein Klick darauf öffnet die
  Einstellungen; unter „Bild im Kopf“ lässt sich das eigene Logo wählen (es bleibt auf dem Rechner).
- **Einstellungen** (links): Update beim Start, Medikamentendatenbank und Nachschlage-Ansicht,
  Module ein- und ausschalten, Frist „Beschluss endet bald“, eigenes Bild für den Kopf (z. B. ein Logo – die Datei wird nur in den
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

Im Datenordner `%APPDATA%\DokuLine` (im Windows-Explorer oben in die
Adresszeile eingeben). Beim ersten Start nach der Umbenennung (Version 0.29.2) kopiert das
Programm den Inhalt des bisherigen Ordners `%APPDATA%\Dokumentationssystem` dorthin; der alte
Ordner bleibt unverändert liegen (in `update.log` steht, dass kopiert wurde). Dort liegen:

- `dokumentationssystem.db` – die Datenbank (oder im gemeinsamen Ordner, falls in den
  Einstellungen gewählt),
- `bilder\` – hinterlegte Bewohnerbilder,
- `logo\` – das eigene Bild für den Kopf (falls gewählt),
- `einstellungen.json` – die Einstellungen,
- `uebergabe-woerter.txt` – nur falls angelegt: eigene Wörter für den Übergabe-Hinweis,
- `update.log` – was beim Update passiert ist (zum Nachsehen, wenn etwas nicht klappt),
- `webansicht\` – Zwischenspeicher der Nachschlage-Ansicht des BfArM (darf gelöscht werden),
- `dokusystem.sqlite3` – nur falls vorhanden: die alte Datenbank der Version 0.2.0
  (wird nicht mehr benutzt).

## Für die IT

Das Programm ist eine einzelne `.exe` (.NET 10, mit allem Nötigen) und braucht keine
Installation. Es läuft unter Windows 10 und 11.

**Windows Server** (laut Microsofts Liste der unterstützten Systeme für .NET 10, Stand
08.11.2026): Windows Server 2016, 2019, 2022 und 2025, jeweils mit Desktopdarstellung (das
Programm hat Fenster, „Server Core“ reicht nicht). Windows Server 2012 und 2012 R2 nur mit
Microsofts Zusatz-Updates (ESU) – nicht empfohlen. Jede angemeldete Person hat ihren eigenen
Datenordner (`%APPDATA%\DokuLine`).

**Nachschlage-Ansicht (WebView2):** Im Formular „Medikament“ zeigt das Programm die
Arzneimittelsuche des BfArM in einem kleinen Fenster. Dafür braucht es die kostenlose
„Microsoft Edge WebView2 Runtime“. In Windows 11 ist sie enthalten, auf Servern oft nicht – dann
einmal von der IT installieren lassen. Für Windows Server 2012 und 2012 R2 gibt es sie nicht mehr;
dort die Ansicht abschalten (Einstellungen → Medikamente → „Nachschlage-Ansicht anzeigen“).
Ob WebView2 da ist, steht in den Einstellungen unter „Programm“. Fehlt sie, erscheint im
Formular nur ein Hinweis; alles andere funktioniert.

**Netz:** Das Programm verbindet sich nur mit
- `api.github.com` und `github.com` (Selbst-Update; der Download leitet GitHub auf seine eigenen
  Download-Server weiter; abschaltbar: Einstellungen oder `--kein-update`),
- `bfarm.de` mit Unteradressen (z. B. `auth.bfarm.de`, `portal.bfarm.de`) und `pharmnet-bund.de`
  – nur in der Nachschlage-Ansicht und erst, wenn jemand „Nachschlagen“ drückt. Gesucht wird nur
  mit dem Namen eines Medikaments, nie mit Angaben zu Bewohnern.

Alles andere (E-Mail, Links) übergibt das Programm an die Programme auf dem Rechner.

## Für Entwickler

C# mit .NET 10 und Windows Forms. Aufbau:

| Ordner | Inhalt |
|---|---|
| `src/DokuLine.Kern` | Datenmodell, Prüfregeln, Speicher (SQLite), Demo-Daten – ohne Fenster |
| `src/DokuLine.Update` | Selbst-Update – das einzige Projekt mit Internetzugriff |
| `src/DokuLine` | Oberfläche (Windows Forms), ergibt `DokuLine.exe` |
| `tests/DokuLine.Tests` | Tests für Kern und Update (laufen auch unter Linux) |
| `tests/DokuLine.Oberflaeche.Tests` | Tests für die Oberfläche (nur Windows) |
| `daten/demo` | Demo-Daten als lesbare Datei (werden in die `.exe` eingebaut) |

Mit dem .NET 10 SDK im Projektordner:

```bash
dotnet test DokuLine.sln
```

```bash
dotnet run --project src/DokuLine
```

Die `.exe` baut GitHub Actions (`.github/workflows/tests-und-exe.yml`) bei jedem Push nach
`main` und veröffentlicht sie als Release `build-<Laufnummer>`.
