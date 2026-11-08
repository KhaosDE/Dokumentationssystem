# STAND — Dokumentationssystem

Stand: 08.11.2026. Version: **0.24.0** (Medikamentenplan als Tabelle, Medikamentendatenbank in den
Einstellungen, Bauplan Schritt 21).

## Aktuell

Das Programm (C# / .NET 10, Windows Forms) zeigt alle geplanten Bereiche in der
Seitenleiste; was noch nicht gebaut ist, sagt „Kommt in Version …“. Gebaut sind die
Bewohnerakte und jetzt die **tägliche Dokumentation**: Berichte je Bewohner mit den
Kennzeichen „Wichtig“ und „Übergabe“, Nachträge statt Überschreiben, Ausdruck, und der
Bereich **„Übergabe“**, in dem nur die Übergabe-Einträge stehen, bis sie ablaufen oder
erledigt sind. Dazu das **Wohnbereich-ToDo** („Aufgaben“) und **Nachrichten** zwischen
ausgedachten Kolleginnen und Kollegen; oben rechts „Angemeldet als …“ (ohne Passwort, nur für
die Vorführung). Dazu die **Medikamentendatenbank** (Demo-Bestand mit Quellen) und neu die
**Medikation** je Bewohner (Grundverordnung, Pause, Stufenplan, „Was gilt heute“), der
**Therapie-Bereich** (Angebote, Zuteilungen, freie Plätze), der **Wochenplan** je Bewohner, der
**Wohnbereichskalender** (Termine, Heimfahrten, Ausflüge) und neu die **Modulverwaltung** mit dem
ersten Modul **Ausgangsliste** (Ausgang, Anwesenheit, Unterbringung, Tagesliste, Küchendienst,
Digitaler Ausgang), übernommen aus der Ausgangsliste. Dazu kommen die **Biografie** und neu die
**Monatsreflexion** als Reiter in der Akte. Das Programm öffnet mit der **Übersicht** (was heute
wichtig ist). Neu ist der **HEB-Bogen** (A, B, C) mit Quellen, Zählern, Stilhinweisen,
Textbausteinen und dem Ausfüllen einer zugeteilten PDF.

In der Akte stehen **„offen geführt“** (ja / nein / nicht erfasst) und das **Alter**.

Neu ist der **Mehrplatzbetrieb (Demo-Stufe)**: Datenbank lokal oder in einem gemeinsamen Ordner,
Konfliktschutz mit beiden Fassungen, Bereichskennung je Rechner, Wohnbereich je Bewohner,
**Statusmeldungen** für alle Bereiche und Abgleich alle 30 Sekunden.

Neu in 0.21.0: Die Inhalte füllen die ganze Fläche, das Seitenmenü lässt sich auf Symbole
einklappen, „Neuer Bericht …“ gibt es nur noch beim Bewohner (Akte, Reiter, Rechtsklick).

Neu in 0.22.0: **Anmelden mit dem Kürzel** beim Start (ohne Passwort), **Mitarbeiter** mit
Bereichen, Positionen und „Fachkraft“, eine **Rechte-Tabelle** nach Positionen. Ohne Recht bleiben
Knöpfe sichtbar, aber grau und nennen den Grund.

Neu in 0.23.0: **Bezugspflege** und **Einzeltrainer** je Bewohner – farbig auf Karten und in der
Akte (Farbe der Person, Kürzel immer dabei), Filter „Meine Bezugsbewohner“ und „Meine
Einzeltrainings“, eigene Farbe in den Einstellungen.

Neu in 0.24.0: Der Reiter heißt **„Medikamentenplan“** und ist eine **Tabelle** (Medikament,
morgens–nachts, verordnet von, seit, Status, Hinweis; Bedarf darunter), sortierbar; ein Klick auf
eine Zeile zeigt Einzelheiten und Verlauf. Die **Medikamentendatenbank** steht in den
**Einstellungen** (Abschnitt „Medikamente“): neue Medikamente prüft, wer sie anlegt (Quelle und
Prüfdatum), Einträge lassen sich sperren, neues Feld **Zulassungsnummer**. Über der Maske die
**Nachschlage-Ansicht des BfArM** (WebView2, abschaltbar).

**Nächster Schritt:** Bauplan Schritt 22 „Therapien: Zuordnung und Rechte“ (0.25.0).

## Fertig

- Repository `KhaosDE/Dokumentationssystem` (öffentlich, wegen der Update-Funktion).
- `CLAUDE.md`, `IDEEN.md`, `STAND.md`, `SITZUNGEN.md`, `README.md`, `docs/`.
- 0.1.0 und 0.2.0 (Python, 05.11.2026): Programmrahmen mit Selbst-Update, Bewohnerakte.
- 0.3.0 — Umstieg auf C# / .NET 10 und Gestaltung (06.11.2026):
  - Aufbau: `src/Dokusystem.Kern` (ohne Fenster, auch unter Linux testbar),
    `src/Dokusystem.Update` (einziges Projekt mit Internet), `src/Dokusystem` (Windows Forms),
    Tests in `tests/`. Projektmappe `Dokumentationssystem.sln`.
  - Gleicher Funktionsumfang wie 0.2.0: Hauptfenster mit Demo-Hinweis, Navigation, Version
    und Build; Datenordner; Update-Häkchen und `--kein-update`; `update.log`; Bewohnerakte
    mit Übersicht, Akte, Formular, Prüfregeln, drei Zuständen bei Allergien und
    Beschlussende, Bild per Dateiauswahl, „Demo-Daten zurücksetzen“.
  - Gestaltung nach `docs/gestaltung.md`: zentrale Klasse `Stil` (übernommen aus der
    Ausgangsliste, bereinigt), Kopf mit Farbverlauf, Kartenraster, Seitenleiste mit hellem
    Akzent für den aktiven Eintrag, eine Hauptaktion pro Ansicht in Akzentblau, Haarlinien
    statt Schatten, Radien 4/8, Abschnittsbeschriftung „STAMMDATEN“ in der Akte.
  - Selbst-Update in C# nach demselben Ablauf; die installierte Python-Version ersetzt sich
    über ihr eigenes Update durch die C#-Version (geprüft, siehe Entscheidungen).
  - `.exe`: eine Datei, mit .NET, nicht komprimiert; Selbsttest in GitHub Actions.
  - Tests: 50 für Kern und Update (xUnit), dazu Oberflächentests unter Windows, die auch
    Bildschirmfotos als Artefakt „Bildschirmfotos“ ablegen.

- 0.4.0 — Programmgerüst (07.11.2026, Bauplan Schritt 1):
  - Seitenleiste: Übersicht, Bewohner, Neuer Bericht …, Übergabe, Kalender, Therapie,
    Medikamente, Einstellungen. Noch nicht gebaute Bereiche zeigen „Kommt in Version …“ mit
    kurzer Beschreibung (Versionen aus dem Bauplan).
  - Bewohnerakte mit Reitern: Stammdaten, Berichte, Medikation, Wochenplan, Biografie,
    Reflexion, HEB. Kopfkarte und Allergiehinweis bleiben über den Reitern immer sichtbar.
  - Einstellungsfenster: eigenes Kürzel (bis 6 Buchstaben/Ziffern), Update-Häkchen (von unten
    links hierher verschoben), eigenes Bild für den Kopf, Hinweis auf spätere Einstellungen.
  - Kopf: Logofeld rechts oben mit dem gewählten Bild; der Demo-Hinweis steht daneben.

- 0.5.0 — Gestaltung Stufe 2 (07.11.2026, Bauplan Schritt 2):
  - Bewohnerkarte nach Vorbild der Ausgangsliste: Bild/Initialen, Name, Zimmer-Marke, darunter
    Marken „Beschluss bis …“, „Allergie: …“, „Allergien nicht erfasst“, „Beschluss nicht
    erfasst“, unten die Aufenthaltsdauer („seit 01.09.2019 · 7 Jahre, 2 Monate“); weicher
    Schatten, Rand unter der Maus kräftiger.
  - Filterknöpfe über dem Raster mit Zählern: Alle, Beschluss, Allergie, Nicht erfasst.
  - Statusleiste unten mit Farbschlüssel der Marken.
  - Logofeld im Kopf (seit 0.4.0, Bild aus den Einstellungen).

- 0.6.0 — Tägliche Dokumentation mit Bereich „Übergabe“ (07.11.2026, Bauplan Schritt 3):
  - Reiter „Berichte“ in der Akte: Liste neueste oben, Filter nach Zeitraum, Lebensbereich,
    Schicht, „nur wichtige“ und Suche im Text; Marken „Wichtig“, „Übergabe“ und die
    Lebensbereiche an jedem Bericht. „Drucken …“ mit Druckvorschau (übernommen aus der
    Ausgangsliste, bereinigt).
  - Bericht schreiben aus der Akte oder über „Neuer Bericht …“ in der Seitenleiste (mit
    Bewohnerauswahl): Datum, Uhrzeit, Schicht passend zur Uhrzeit und eigenes Kürzel sind
    vorbelegt; Lebensbereiche 1–5 oder „allgemein“; die Kennzeichen „Wichtig“ und
    „Übergabe“ gut sichtbar im Fenster, bei „Übergabe“ die Dauer (nächste Schicht, 3 Tage,
    1 Woche, bis Datum, bis erledigt) und ein freiwilliger kurzer Übergabe-Satz.
  - Hinweis vor dem Speichern: Stehen Wörter wie „Arzt“, „Termin“, „Sturz“ im Text und ist
    „Übergabe“ nicht gesetzt, fragt das Programm einmal „Soll das in die Übergabe?“. Wortliste
    in `daten/uebergabe-woerter.txt`, ergänzbar über eine gleichnamige Datei im Datenordner.
  - Nichts wird gelöscht oder überschrieben: Korrekturen sind Nachträge mit eigener Zeit und
    eigenem Kürzel; die Datenbank verbietet das Ändern von Berichten.
  - Bereich „Übergabe“: oben „Wohnbereich allgemein“ (eigene Notizen ohne Bericht), darunter
    nach Bewohner gruppiert. Marken „neu seit deiner letzten Schicht“, „läuft heute ab“ und
    die Gültigkeit; „Gelesen“ (Kenntnisnahme mit Kürzel, sichtbar wer schon gelesen hat),
    „Verlängern …“, „Erledigt“; erledigte und abgelaufene Einträge einblendbar;
    „Übergabeblatt drucken …“ mit den offenen Einträgen.
  - Karten der Bewohner: Marke „Übergabe N“ (offene, vom eigenen Kürzel ungelesene
    Einträge) und „Wichtig“ (wichtiger Bericht in den letzten 7 Tagen).
  - Seitenleiste nach dem neuen Bauplan: zusätzlich „Aufgaben“ (0.7.0), „Nachrichten“
    (0.8.0) und „Ausgangsliste“ (0.14.0) als Platzhalter; alle Versionsangaben angepasst.
  - Demo: für jeden Bewohner Berichte der letzten vier Wochen über alle Lebensbereiche,
    einige wichtige, einige mit Nachtrag; Übergabe-Einträge (offen, mehrtägig, erledigt,
    abgelaufen) und zwei Notizen für den Wohnbereich – alles relativ zum heutigen Tag.

- 0.7.0 — Wohnbereich-ToDo (07.11.2026, Bauplan Schritt 4):
  - Bereich „Aufgaben“: offene Aufgaben, überfällige oben (rot, mit dem Wort „überfällig“ und
    rotem Balken), dann was jetzt dran ist, dann nach Fälligkeit. Marken „für Spätdienst
    Sa 07.11.“ bzw. „wer Zeit hat“, „fällig bis …“, „jetzt dran“. Erledigte einblendbar.
  - „Neue Aufgabe …“: Text, Bewohner (oder keiner), für wen (nächste Schicht – Vorgabe –,
    bestimmte Schicht an einem Tag, wer Zeit hat), fällig bis (Datum, Uhrzeit freiwillig),
    angelegt von (Kürzel vorbelegt).
  - „Erledigt“ (mit Kürzel und Zeit) und „An die nächste Schicht weitergeben …“ mit kurzer
    Notiz; die Weitergabe steht sichtbar an der Aufgabe. Nichts wird gelöscht oder
    überschrieben.
  - Aus einem Bericht: „Aufgabe daraus …“ übernimmt den Bewohner.
  - Zähler offener Aufgaben in der Seitenleiste (rot, wenn etwas überfällig ist) und eine
    Kurzliste der ersten drei offenen Aufgaben über den Bewohnerkarten mit Knopf zu allen.
  - Ausdruck der offenen Aufgaben (Druckvorschau).
  - Demo: sechs ausgedachte Aufgaben relativ zu heute, eine überfällig und schon einmal
    weitergegeben, eine erledigt. „Wichtig“ steht in den Demo-Berichten seltener.

- 0.8.0 — Nachrichten (07.11.2026, Bauplan Schritt 5):
  - Ausgedachte Kolleginnen und Kollegen mit Kürzel und Team (Wohnbereich A, Wohnbereich B,
    Therapie, Leitung) in `daten/demo/nachrichten.json`.
  - Oben rechts im Kopf „Angemeldet als …“: Klick öffnet die Liste, Wechsel ohne Passwort.
    Dasselbe steht in den Einstellungen („Angemeldet als“ statt des freien Kürzel-Felds); das
    Kürzel gilt für Berichte, Übergabe und Aufgaben.
  - Bereich „Nachrichten“ mit Zähler ungelesener Nachrichten: Posteingang, Gesendet, Archiv.
    Verläufe mit Marken „2 neu“, Bewohner, Anzahl; Klick öffnet den Verlauf (gilt dann als
    gelesen), dort „Antworten …“ (an alle Beteiligten) und „Archivieren“ bzw. „Zurück in den
    Posteingang“.
  - „Neue Nachricht …“: an Personen und/oder Teams, Betreff, Text, optional Bezug zu einem
    Bewohner. Team-Nachrichten sieht jedes Teammitglied.
  - In der Akte „Nachrichten …“: zeigt die Verläufe zu diesem Bewohner; eine neue Nachricht
    hat den Bewohner dann schon gewählt.
  - Über den Bewohnerkarten ein Hinweis „3 ungelesene Nachrichten für …“ mit „Öffnen …“.
  - Nichts wird gelöscht; kein E-Mail-Versand, kein Internet.

- 0.9.0 — Medikamentendatenbank (07.11.2026, Bauplan Schritt 6):
  - Bereich „Medikamente“: Liste mit Suche (Wirkstoff, Handelsname, Form), dauerhaft der
    Hinweis „Demo-Bestand, nicht zum Verordnen verwenden“. Je Eintrag Wirkstoff,
    Handelsnamen, Marken für Prüfstatus, Darreichungsform und Stärken, die Quelle als
    Adresse mit „Adresse kopieren“.
  - „Neues Medikament …“ (gekennzeichnet „selbst angelegt“) und „Ändern …“: jede Änderung ist
    eine neue Fassung mit Datum und Kürzel; „Verlauf …“ zeigt alle Fassungen und was sich
    geändert hat.
  - Demo-Bestand `daten/demo/medikamente.json`: 36 gängige Wirkstoffe (Psychiatrie und
    Allgemeinmedizin), alle „ungeprüft“, jede Angabe mit Quelle.

- 0.10.0 — Medikation (07.11.2026, Bauplan Schritt 7):
  - Reiter „Medikation“ in der Akte: „Was gilt am …“ (heute oder ein anderer Tag), getrennt nach
    „Regelmäßig“ und „Bei Bedarf“; dauerhaft „Demo — nicht zum Stellen verwenden“.
  - „Neue Verordnung …“: Medikament aus der Datenbank, Stärke (aus der Datenbank vorgeschlagen),
    Menge morgens, mittags, abends, nachts (auch ½ oder 0,5), oder Bedarfsmedikation mit
    Anlass und Höchstmenge; Beginn; Anordnung (Arzt und Datum, ausgedacht).
  - Je Verordnung: „Pause …“ (von – bis oder bis auf Weiteres), „Pause beenden …“,
    „Stufenplan …“ (bis zu 6 Stufen „ab Datum: Menge“), „Absetzen …“, „Verlauf …“ – jede
    Änderung ein eigener Eintrag mit Anordnung; die Grundverordnung wird nie überschrieben.
  - Anzeige: „pausiert bis …“ (sichtbar, ausgegraut), „Stufe 2 von 3“ mit „nächste Stufe ab …“,
    „neue Grundverordnung seit …“; Abgesetztes einblendbar.
  - Ausdruck „Was gilt am …“ je Bewohner (Druckvorschau) mit dem Vermerk „Demo — nicht zum
    Stellen verwenden“.
  - Demo: Verordnungen für alle Bewohner relativ zu heute – eine Pause, ein Stufenplan zum
    Ansetzen, einer zum Ausschleichen, ein abgeschlossener, ein abgesetztes Medikament, zwei
    Bedarfsmedikationen; Ärzte erkennbar ausgedacht („Dr. Ulme (ausgedacht)“).

- 0.11.0 — Therapie-Bereich (07.11.2026, Bauplan Schritt 8):
  - Bereich „Therapie“: alle Angebote mit Art (Gruppe/Einzel), Terminen der Regelwoche,
    Therapeut, Ort, Teilnehmern und freien Plätzen („4 Plätze frei“, „voll“); „Stand am …“
    für einen anderen Tag.
  - „Neues Angebot …“ und „Ändern ab …“: Name, Art, Therapeut/in, Ort, bis zu drei Termine je
    Woche, höchste Teilnehmerzahl, Hinweis; Änderungen gelten ab einem Datum, die bisherige
    Fassung bleibt.
  - „Bewohner zuteilen …“ (ab, optional bis) mit Warnung bei Überschneidung mit einer
    anderen Therapie des Bewohners; „Teilnahme beenden …“ als eigener Eintrag; „Verlauf …“.
  - Demo `daten/demo/therapie.json`: sieben ausgedachte, allgemeine Angebote mit Zuteilungen
    relativ zu heute (eine Zeitänderung, eine beendete Teilnahme, eine beginnt später).

- 0.12.0 — Wochenplan (07.11.2026, Bauplan Schritt 9):
  - Reiter „Wochenplan“ in der Akte: Montag bis Sonntag als Spalten, Uhrzeiten als Zeilen;
    zugeteilte Therapien erscheinen von selbst (blaugrün), freie Einträge blaugrau; heute
    hervorgehoben; Blättern nach Kalenderwochen.
  - Klick auf eine Therapie: Teilnahme „teilgenommen“, „abgesagt“, „ausgefallen“ oder zurück
    auf „offen“. Klick auf einen freien Eintrag: „Ändern ab …“ oder „Entfernen ab …“.
  - „Neuer Eintrag …“ (jede Woche am …, von–bis, Ort, Hinweis, gilt ab) mit Warnung bei
    Überschneidung; „Verlauf …“ zeigt alle Fassungen.
  - Ausdruck: A4 quer, große Schrift, nur Name und Plan (Druckvorschau, „Microsoft Print to
    PDF“ möglich).
  - Die Zuteilung im Therapie-Bereich warnt jetzt auch vor Überschneidungen mit freien
    Einträgen des Wochenplans.
  - Demo `daten/demo/wochenplan.json`: Spaziergang, Zimmer aufräumen, Wäschetag, Einkaufen,
    Telefonat – eine Änderung „ab Datum“, ein entfernter Eintrag; Teilnahmen der letzten vier
    Wochen (meist teilgenommen, ab und zu abgesagt oder ausgefallen).

- 0.13.0 — Wohnbereichskalender (07.11.2026, Bauplan Schritt 10):
  - Bereich „Kalender“: Monat (Raster, je Tag bis zu drei Termine, Klick öffnet den Tag),
    Woche (wie der Wochenplan, alle Bewohner), Tag (Karten mit allen Angaben); Filter „alle“
    oder ein Bewohner; ◀ ▶ und „Heute“.
  - Terminarten: Termin eines Bewohners, Termin des Wohnbereichs, Heimfahrt (auch über
    mehrere Tage), Tagesausflug (Ziel, Treffpunkt, Teilnehmer, Begleitung). Ein Termin kann
    mehreren Bewohnern gehören.
  - „Neuer Termin …“, „Ändern …“, „Absagen …“ (mit Rückfrage) – jeweils neue Fassung,
    „Verlauf …“ zeigt alle.
  - Termine eines Bewohners erscheinen in seinem Wochenplan (violett; mehrtägige Heimfahrt an
    den Tagen dazwischen ganztags).
  - Ausdruck: Monat (A4 quer) oder Woche (A4 hoch), Teilnehmerliste eines Ausflugs zum
    Abhaken.
  - Demo `daten/demo/kalender.json`: Arzttermine, Friseur, Teamsitzung, Hausversammlung, eine
    laufende und eine geplante Heimfahrt, zwei Ausflüge, ein verschobener und ein abgesagter
    Termin – alles relativ zu heute.

- 0.14.0 — Modul Ausgangsliste mit Modulverwaltung (Bauplan Schritt 11, 07.11.2026):
  - Modulverwaltung: Schnittstelle `IModul` (Seitenleiste mit Seite, Akte-Reiter, Marken auf den
    Bewohnerkarten) und `IModulDaten` im Kern (eigene Tabellen, eigene Demo-Daten). Einstellungen
    → „MODULE“ mit Häkchen je Modul und Untermodul. Ausgeschaltet: kein Eintrag links, kein
    Reiter, keine Marken; die Daten bleiben.
  - Neues Projekt `src/Dokusystem.Bausteine` (Stil, Karten, Marken, Dialoge, Druckvorschau,
    kleine Formulare, Schnittstelle für Module) – Programm und Module nutzen es gemeinsam.
  - Neues Projekt `src/Dokusystem.Modul.Ausgangsliste` mit `AENDERUNGEN.md`, übernommen aus der
    Ausgangsliste 0.23.0 (bereinigt): Ausgangszeiten (allein / mit Bewohner, Infos,
    Personal-Begleitung, „wie offen geführt“), Anwesenheit und Abwesenheit mit Grund und Frist,
    Unterbringung mit Fristen und Rechtsregel, Tagesliste (A4 quer), Verlauf je Person;
    Untermodule Küchendienst (Plan, Tausch, Zimmergruppen, Ausdruck) und Digitaler Ausgang
    (beginnen, beenden, unterwegs, überfällig, Nachtbericht zum Kopieren).
  - Akte: Reiter „Ausgang“ (Karte und Verlauf). Bewohnerkarten: „abwesend: …“,
    „Ausgangsbeschränkung …“, „unterwegs seit …“, „überfällig seit …“, Fristen.
  - Demo `daten/demo/ausgangsliste.json`: Ausgänge, eine Heimfahrt (passt zum Kalender), ein
    Krankenhausaufenthalt, eine Ausgangsbeschränkung, Forensik, ablaufendes Einverständnis,
    digitale Ausgänge (einer unterwegs, einer überfällig), Küchendienst mit Zimmergruppe und
    Tausch – relativ zu heute und zur Uhrzeit.

- 0.15.0 — Biografie (Bauplan Schritt 12, 07.11.2026):
  - Reiter „Biografie“ in der Akte: sechs feste Abschnitte (Herkunft und Familie, Schule und
    Beruf, Wohnen, Interessen und Gewohnheiten, Wichtige Lebensereignisse, Wünsche) als Karten
    mit Fließtext; leere Abschnitte deutlich als „Keine Angabe – fehlt noch.“.
  - „Bearbeiten …“: Fließtext ohne Aufzählungszeichen (sonst Fehlermeldung), Häkchen „vom
    Bewohner angegeben, nicht überprüft“ mit Hinweis auf „gibt an, dass …“ (nicht automatisch
    eingesetzt). Jede Änderung ist eine neue Fassung mit Datum und Kürzel; „Verlauf …“ zeigt alle.
  - Unten „Fehlende Angaben: …“; oben „Als Fließtext kopieren“ (reiner Text, fehlende Angaben am
    Ende).
  - Demo `daten/demo/biografie.json`: kurze, wertneutrale, ausgedachte Biografien für sechs
    Bewohner (einige Abschnitte leer, eine Änderung mit Verlauf, ein Hinweis auf die
    Formulierung), vier Bewohner ohne Biografie.

- 0.16.0 — Monatsreflexion (Bauplan Schritt 13, 07.11.2026):
  - Reiter „Reflexion“ in der Akte: Monat wählen (◀ ▶, vorgeschlagen ist der vergangene
    Monat), fünf Textfelder nach den Lebensbereichen des HEB-Bogens, daneben je Bereich die
    Berichte des Monats („Keine Berichte zu diesem Bereich im Monat.“, wenn keine da sind),
    oben die Teilnahme an Therapien laut Wochenplan.
  - Zeichenzahl je Feld und Stilprüfung als Hinweisliste nach `docs/heb-wissen.md` (Semikolon,
    Datum im Text, gemiedene Wörter, Floskeln, „nicht …, sondern …“, „Mitarbeitende“,
    Gendersternchen, abgekürzter Nachname ohne Punkt, mehrere Gedankenstriche) – nie
    automatisch geändert. Die Prüfung liegt im Kern (`Stilpruefung`) und dient später auch dem
    HEB-Bogen.
  - Status Entwurf / fertig; fertig ist schreibgeschützt, „Wieder als Entwurf“ öffnet es. Jede
    Änderung ist eine neue Fassung („Verlauf …“). Beim Monatswechsel mit ungespeicherten
    Änderungen kommt eine Rückfrage.
  - Demo `daten/demo/reflexion.json`: Reflexionen des vergangenen Monats für vier Bewohner, zwei
    fertig, zwei Entwurf (einer mit Stilhinweisen).

- 0.17.0 — Startseite als Übersicht (Bauplan Schritt 14, 07.11.2026):
  - Bereich „Übersicht“ (erster Eintrag, öffnet beim Programmstart): oben „Heute“ mit Anzahl
    Bewohner, Abwesenden, Terminen des Tages (gleiche Termine zusammengefasst, mit Namen) und
    Änderungen in der Medikation; darunter das Kartenraster aller Bewohner.
  - Marken je Karte (nur wenn zutreffend): abwesend (Heimfahrt laut Kalender oder Modul
    Ausgangsliste – nur einmal), Beschluss endet bald / abgelaufen, Allergie eingetragen,
    nicht erfasst, Medikation heute (neu verordnet, Pause beginnt/endet, neue Dosis,
    abgesetzt), Termine heute laut Wochenplan (erster mit Uhrzeit, „+n“), dazu die Marken der
    Module. Klick öffnet die Akte.
  - Immer aktuell: Neuaufbau beim Öffnen, nach Änderungen an Medikation, Therapie, Wochenplan,
    Kalender und Modulen und beim Datumswechsel um Mitternacht.
  - Einstellungen → „FRISTEN“: „Beschluss endet bald“ ab 90 Tagen (1–365).
  - Demo: Bei Frieda Fichte beginnt heute eine Pause (Metformin), damit „Medikation heute“
    etwas zeigt.

- 0.18.0 — HEB-Bogen aus Berichten (Bauplan Schritt 15, 07.11.2026):
  - Reiter „HEB“ in der Akte: die Bögen des Bewohners (Art, Zeitraum, Nummer, Entwurf/fertig,
    Verlauf), oben „HEB fällig …“, wenn es so ist. „Neuer HEB-Bogen …“: Art wählen, Zeitraum
    vorgeschlagen (A ab Aufnahme, B/C ab dem Ende des letzten Bogens), laufende Nummer,
    Planungszeitraum aus dem Beschlussende (6/12/24 oder frei).
  - Bearbeiten: je Lebensbereich die Felder der Bogenart mit Zeichenzähler (Grenze aus
    `docs/heb-wissen.md`, rot darüber) und Stilhinweisen (dazu: Rahmenziele nicht in Ich-Form,
    Leitziel und Nahziele in Ich-Form); Hilfebedarf Stufe 1–6, nie vorgeschlagen oder vorbelegt;
    daneben Berichte des Zeitraums, Wochenplan, fertige Monatsreflexionen. Reiter „Ziele“
    (Leitziel, zwei Nahziele mit Einschätzung) und „Weiteres“ (Sichtweise, Ergänzendes, bei C
    Beendigung) mit Merkhilfe. Textbausteine (Verben, feste Formulierungen, Häufigkeit) per Klick.
  - HEB B: Rahmenziele, Leitziel und Nahziele des letzten Bogens werden zum Fortschreiben
    vorgelegt.
  - Speichern als Entwurf, „Als fertig markieren“ (Rückfrage, wenn Felder zu lang sind),
    Fassungen mit Verlauf. „Drucken …“ der eigenen Maske (auch ohne PDF).
  - PDF: Einstellungen → „HEB-VORLAGEN“ je Bogenart zuteilen (Kopie im Datenordner `vorlagen`),
    „Zuordnung …“ Programmfeld → PDF-Feld (`heb-zuordnung.json`, Vorgabe: gleicher Name).
    „PDF ausfüllen …“ füllt eine Kopie (Ordner `heb`), die Vorlage bleibt unverändert; Felder
    ohne PDF-Feld werden gemeldet. Getestet mit einer selbst gebauten Test-PDF
    (`tests/daten/heb-test.pdf`, Bauskript `werkzeuge/heb-test-pdf.py`).
  - „HEB fällig“ auf der Übersicht und in der Akte: letzter Bogen älter als die Frist
    (Einstellungen, Vorgabe 12 Monate) oder kein HEB A drei Monate nach der Aufnahme.
  - Demo `daten/demo/heb.json`: frühere Bögen für alle Bewohner (bei Anton Ahorn und Ida
    Immergrün fällig), ein Entwurf bei Karl Kiefer.

- 0.19.0 — Bewohnerakte: Ergänzungen (Bauplan Schritt 16, 08.11.2026):
  - „Offen geführt“ in den Stammdaten mit drei Zuständen (ja, nein, nicht erfasst; Regel 5), im
    Formular, in der Akte und als Marke auf Karten, Übersicht und Akte („offen geführt“,
    „offen geführt: nicht erfasst“). Filter „Nicht erfasst“ zählt es mit. Vorhandene Bewohner
    stehen nach dem Update auf „nicht erfasst“.
  - Ist das Modul Ausgangsliste eingeschaltet: Abgleich mit dessen Unterbringung, Widerspruch als
    Marke „Unterbringung widersprüchlich – bitte klären“ (auf der Seite des Moduls ausführlich).
    Nichts wird automatisch geändert.
  - Alter aus dem Geburtsdatum neben dem Geburtsdatum („(63 Jahre)“).
  - Oben in der Akte stehen jetzt Marken: Zimmer, „offen geführt“ und Hinweise der Module.
  - Demo: Angaben für alle Bewohner; Dieter Distel „nicht erfasst“, Ida Immergrün mit
    Widerspruch zur Ausgangsliste.

- 0.20.0 — Mehrplatzbetrieb, Demo-Stufe (Bauplan Schritt 17, 08.11.2026):
  - Einstellungen → „MEHRPLATZ“: **Bereich** dieses Arbeitsplatzes (ausgedacht, Vorschläge
    „Wohnbereich A/B“, „Therapie“, „Leitung“) und **Ort der Datenbank**: lokal (Vorgabe) oder ein
    gemeinsamer Ordner (geprüft: erreichbar und beschreibbar). Der Ort gilt nach dem Neustart;
    ist der Ordner beim Start nicht erreichbar, arbeitet das Programm lokal und sagt es.
    Unten links steht „Wohnbereich A · Datenbank lokal“ (bzw. „gemeinsamer Ordner“).
  - **Konfliktschutz** wie in der Ausgangsliste: Haben zwei Arbeitsplätze denselben Datensatz
    geändert, kommt „Inzwischen geändert“ mit beiden Fassungen nebeneinander und der Wahl
    „Meine Fassung speichern“, „Andere übernehmen“ oder „Abbrechen“. Gilt für Stammdaten der
    Akte, Biografie, Monatsreflexion, HEB-Bogen und Kalendertermine. Frühere Fassungen bleiben
    im Verlauf.
  - **Wohnbereich je Bewohner** (Stammdaten, Formular). Die Übersicht zeigt den eigenen Bereich,
    umschaltbar mit „alle Bereiche“; ein Bereich ohne eigene Bewohner (z. B. Therapie) sieht
    alle mit Hinweis. Auf den Karten der Seite „Bewohner“ steht ein fremder Wohnbereich hinten.
  - **Statusmeldung je Bewohner** (Akte → „Status …“): kurzer Text, gilt bis, mit Bereich, Kürzel
    und Zeit; vorne auf Karten und Übersicht, lang oben in der Akte, in allen Bereichen. Neue
    Meldung ersetzt die alte, „Aufheben“ ist ein eigener Eintrag; der Verlauf bleibt (Datenbank
    verbietet Überschreiben). Farbschlüssel: „Übergabe, Status“.
  - **Abgleich:** Alle 30 Sekunden prüft jedes Fenster, ob ein anderer gespeichert hat (SQLite
    `data_version`), und liest dann Zähler (Aufgaben, Nachrichten) und die gezeigte Ansicht neu.
    Beim Wechsel der Ansicht wird ohnehin frisch gelesen. Damit gehen Nachrichten, Übergabe und
    Aufgaben auch zwischen Rechnern.
  - Einstellungsfenster: Der Inhalt hat eine Bildlaufleiste, „Speichern“ und „Abbrechen“ stehen
    immer sichtbar unten (vorher war das Fenster auf kleineren Bildschirmen unten abgeschnitten).
  - Demo: Ginster und Immergrün wohnen in „Wohnbereich B“; Statusmeldungen aus
    `daten/demo/status.json` (Ahorn „in der Holzwerkstatt“ von der Therapie, Immergrün „Besuch bei
    der Familie“ von Wohnbereich B, eine ältere im Verlauf).

- 0.21.0 — Oberfläche: volle Fläche, einklappbares Seitenmenü (Bauplan Schritt 18, 08.11.2026):
  - Keine feste Höchstbreite mehr: Akte (Kopf, Allergiehinweis, Reiter und ihr Inhalt),
    Übergabe, Aufgaben, Nachrichten, Kalender, Therapie, Medikamente, Ausgangsliste und die
    Platzhalter wachsen mit dem Fenster. Der Wochenplan verteilt die Tage auf die volle Breite
    und die Stunden auf die Höhe bis zum unteren Rand (zu wenig Platz: Bildlauf).
  - Seitenmenü: oben der Menü-Knopf (Symbol aus „Segoe Fluent Icons“, unter Windows 10 „Segoe
    MDL2 Assets“). Eingeklappt nur Symbole mit Hinweis beim Darüberfahren, Zähler als kleine Zahl
    am Symbol, aktiver Bereich hervorgehoben; unten Version/Bereich und „Demo-Daten
    zurücksetzen“ als Symbol. Ausgeklappt stehen die Symbole vor den Namen. Der Zustand bleibt
    in den Einstellungen (`menue_eingeklappt`).
  - „Neuer Bericht …“ ist kein Menüpunkt mehr: Knopf im Kopf der Akte (neben „Status …“), im
    Reiter „Berichte“ und per Rechtsklick auf eine Bewohnerkarte (Übersicht und Bewohner; das
    Menü hat „Akte öffnen“ und „Neuer Bericht …“).

- 0.22.0 — Mitarbeiter, Anmeldung und Rechte (Bauplan Schritt 19, 08.11.2026):
  - **Anmelden** beim Start mit dem Kürzel (Fenster „Anmelden“, ohne Passwort; Platz für ein
    Passwortfeld später). Oben rechts „Angemeldet als …“ öffnet „Abmelden / Benutzer wechseln“.
    Das frühere „Benutzer wählen“ (Menü und Einstellungen) ist weg. Ausgeschiedene können sich
    nicht anmelden.
  - **Mitarbeiter** (Einstellungen → „VERWALTUNG“ → „Mitarbeiter …“): Kürzel (fest), Name,
    Fachkraft ja/nein, aktiv/ausgeschieden, Bereiche und Positionen per Häkchen (mehrere möglich).
    Gelöscht wird niemand. Die bisherigen Kolleginnen und Kollegen der Nachrichten sind jetzt die
    Mitarbeiter; Nachrichten an einen Bereich gehen an alle seine Mitarbeiter.
  - **Bereiche und Positionen** als Listen: anlegen, umbenennen (Zuordnungen, Bewohner und
    Nachrichten gehen mit), stilllegen, löschen nur ohne Zuordnung.
  - **Rechte-Tabelle** (Funktionen × Positionen, dazu „nur Fachkräfte“), Vorbelegung nach Tobi;
    Änderungen gelten sofort. Bearbeiten nur mit „Rechte verwalten“, sonst nur ansehen.
  - Geprüft werden: Bewohner anlegen/bearbeiten, Berichte und Nachträge (auch Rechtsklick auf
    Karten), Medikation (nur Fachkräfte), Medikament anlegen (nur Fachkräfte) und Medikamente
    ändern, Therapien (anlegen, ändern, zuteilen, beenden), Teilnahme im Wochenplan, HEB-Bogen,
    Mitarbeiter, Bereiche/Positionen, Rechte, Demo-Daten zurücksetzen. Die übrigen Funktionen der
    Tabelle (Visiten, Kalender Haus, Fahrzeuge, Aufgaben-Vorlagen, Module, Einstellungen des
    Bereichs) greifen mit ihren Bauplan-Schritten.
  - Die Übersicht zeigt die Wohnbereiche des Angemeldeten (z. B. beide bei Paula Rabe);
    hat er keinen mit Bewohnern, den Bereich des Rechners.
  - Demo `daten/demo/mitarbeiter.json`: 13 ausgedachte Mitarbeiter, je Position mindestens einer,
    Fachkräfte und Nicht-Fachkräfte, eine ausgeschiedene Person (Hugo Ibis).
  - Außerdem (Wunsch Tobi in der Sitzung): **Platzhalter für das Einrichtungslogo** oben rechts im
    Kopf – ein gestrichelter Rahmen „Logo der Einrichtung“, solange kein Bild gewählt ist; ein
    Klick öffnet die Einstellungen („Bild im Kopf“). Das Logo selbst kommt nie ins Repository.

- 0.23.0 — Bezugspflege und Einzeltrainer (Bauplan Schritt 20, 08.11.2026):
  - Akte → Stammdaten: Bezugspflege und Einzeltrainer (mit Art) mit Farbpunkt, Name, Kürzel und
    „seit“; „Bezug ändern …“ (Recht „Bewohner anlegen und bearbeiten“) und „Verlauf …“. Wechsel
    sind neue Einträge mit Datum und Kürzel; die Datenbank verbietet Überschreiben.
  - Ab fünf Bezugsbewohnern ein Hinweis im Fenster, keine Grenze. Arten: DBT, Suchtgruppe; eine
    neu eingetippte Art kommt in die Liste.
  - Karten (Bewohner und Übersicht): kräftiger Farbstreifen links für die Bezugspflege, ihr
    Kürzel als farbige Marke neben dem Zimmer; Einzeltrainer als kleiner Punkt mit „DBT · PR“.
    Statusleiste: „Streifen: Bezugspflege, Punkt: Einzeltrainer“ (wenn Platz ist).
  - Übersicht: „Meine Bezugsbewohner“ und „Meine Einzeltrainings“ (aus allen Bereichen).
  - Einstellungen → „Meine Farbe“ (Farbauswahl, „Vorgabe“); die Farbe gehört zur Person und gilt
    auf allen Rechnern. Ohne eigene Farbe eine feste Vorgabe aus dem Kürzel.
  - Demo `daten/demo/bezug.json`: Bezugspflege für alle, Anna Bachstelze mit fünf (Hinweis),
    ein früherer Wechsel bei Anton Ahorn, drei Einzeltrainings (DBT, Suchtgruppe).

- 0.24.0 — Medikamentenplan als Tabelle, Medikamentendatenbank in den Einstellungen (Bauplan
  Schritt 21, 08.11.2026):
  - Reiter „Medikation“ heißt „Medikamentenplan“. Tabelle „Regelmäßig“: Medikament (Name, Form,
    Stärke), morgens, mittags, abends, nachts, verordnet von, seit, Status („aktiv“, „pausiert bis
    …“, „Stufe 2 von 3 · nächste Stufe ab …“, „beginnt am …“, „abgesetzt am …“), Hinweis.
    Darunter „Bei Bedarf“ (Anlass, Einzeldosis, höchstens). Sortierbar über die Spaltenköpfe
    („seit“ nach Datum), Zeilen abwechselnd hinterlegt, Status farbig.
  - Klick (oder Eingabetaste) auf eine Zeile: Einzelheiten mit Pause, Pause beenden, Stufenplan,
    Absetzen und dem Verlauf. Die Logik aus 0.10.0 ist unverändert. Ausdruck „Medikamentenplan“.
  - Medikamentendatenbank nicht mehr in der Seitenleiste, sondern Einstellungen → „Medikamente“
    → „Medikamentendatenbank …“ (eigenes Fenster). Ansehen dürfen alle; anlegen, ändern, sperren
    nach den Rechten.
  - Prüfung neuer Medikamente: „bestätigt“ nur mit Quelle und Prüfdatum (Vorgabe heute, nicht in
    der Zukunft); Kürzel und Zeit setzt das Programm. Karte: „geprüft am … von …“. Beim Verordnen
    sind ungeprüfte gekennzeichnet („… – ungeprüft“), gesperrte nicht wählbar.
  - „Sperren“ / „Wieder freigeben“ (Recht „Medikamentendatenbank verwalten“) – als neue Fassung im
    Verlauf. Neues Feld „Zulassungsnummer“ (optional, auch in der Suche).
  - Nachschlage-Ansicht über der Maske: „Nachschlagen“ öffnet die Arzneimittelsuche des BfArM;
    der Name liegt in der Zwischenablage. Text markieren, Feld anklicken, „In Feld übernehmen“;
    „Adresse als Quelle übernehmen“; „Im Browser öffnen“. Nur Adressen des BfArM; anderes öffnet
    im normalen Browser. Abschaltbar (Einstellungen → Medikamente). Fehlt WebView2: Hinweis, das
    Formular bleibt nutzbar. Einstellungen → Programm zeigt, ob WebView2 da ist.
  - README: Abschnitt „Für die IT“ (Windows Server, WebView2, Netzadressen).
  - Behoben: In der Übersicht überdeckten sich bei wenig Platz „Meine Bezugsbewohner“ und „Meine
    Einzeltrainings“ (0.23.0); die Filter rücken jetzt in eine eigene Zeile.

## Offene Punkte

- Nachschlage-Ansicht (0.24.0): Mit der echten Seite des BfArM (anonyme Anmeldung, Weiterleitungen)
  nur so weit geprüft, dass die Adressen stimmen (08.11.2026); in den automatischen Tests wird das
  Laden ersetzt. Bitte in der Testliste von Hand prüfen. Ob die BfArM-Suche auf weitere Adressen
  (außerhalb von bfarm.de und pharmnet-bund.de) springt, ist offen – solche Links öffnen dann im
  normalen Browser.
- Bewohner löschen oder als ausgezogen markieren gibt es noch nicht (war nicht
  beauftragt). Zum Aufräumen in der Demo: „Demo-Daten zurücksetzen“.
- Dunkle Darstellung ist nicht gebaut (`docs/gestaltung.md` sieht sie vor, freigegeben ist
  bisher nur die helle).
- Aus der Ausgangsliste noch nicht übernommen: Sicherung und Protokoll (die Druckvorschau seit
  0.6.0, die Ausgangs-Funktionen seit 0.14.0 als Modul, der Mehrplatz-Schutz seit 0.20.0).
- Mehrplatz: Bewohnerbilder, das Bild im Kopf und die HEB-Vorlagen bleiben im Datenordner des
  jeweiligen Rechners (nicht im gemeinsamen Ordner). Die Einstellungen der Module stehen in der
  Datenbank und gelten damit für alle Rechner am selben Ordner. Ausgangsliste-Modul: Änderungen
  am Ausgang prüft es nicht auf Konflikte (es speichert einzelne Werte, die Ereignisse stehen im
  Verlauf).
- Mehrplatz ist nur mit zwei Fenstern auf einem Rechner getestet (Tests und Testliste), nicht mit
  einem echten Netzlaufwerk. SQLite im Netzordner geht bei wenigen Rechnern, ist aber kein
  Server-Betrieb (siehe Eintrag „Mehrplatzbetrieb über einen Server (später)“).
- HEB-Bogen: Ob PDFsharp die Formularfelder des echten Bezirks-Bogens sauber füllt, ist
  ungeprüft (nur mit der Test-PDF erprobt). Feldnamen des echten Bogens sind unbekannt; die
  Zuordnung in den Einstellungen macht ihn ohne Programmänderung nutzbar. Ankreuzfelder
  (Hilfebedarf, Planungszeitraum) des echten Bogens werden noch nicht angekreuzt – übertragen
  wird ihr Wert nur in Textfelder.
- Ein Abgleich Modul → eigenständige Ausgangsliste ist noch nicht gelaufen
  (`docs/bruecke-ausgangsliste.md`).
- „Bericht lesen …“ in der Übergabe zeigt den Bericht in einem einfachen Meldungsfenster;
  ein eigenes Lesefenster wäre schöner, war aber nicht verlangt.
- Der Wechsel 0.2.0 → 0.3.0 über das Update ist am Code geprüft und mit dem echten Release
  durchgespielt (Laden, Prüfsumme), aber nicht auf einem echten Windows-Rechner – bitte
  Testliste Punkt 1.

## Entscheidungen

- 05.11.2026 (Tobi): Demo mit ausgedachten Daten, zu Präsentationszwecken.
- 05.11.2026 (Tobi): Alle Werkzeuge des Wohnbereichs verschmelzen zu einem Programm; Kern
  sind Bewohnerführung und tägliche Dokumentation.
- 05.11.2026 (Tobi): Repository öffentlich, damit eine Update-Funktion gebaut und erprobt
  werden kann.
- 05.11.2026 (Tobi): Windows-Programm. Ideen werden im Chat besprochen, landen in
  `IDEEN.md`, Claude Code setzt sie um.
- 05.11.2026 (Chat): Python + PySide6 + SQLite. **Abgelöst am 06.11.2026** (nächster Punkt).
- 06.11.2026 (Tobi): Umstieg auf C# / .NET 10 mit Windows Forms. Grund: Die Ausgangsliste
  ist bereits ein C#-Programm mit vielen passenden Bausteinen (Kartenraster, Mehrplatz-
  Datenbank, Druck, Installer); ein späterer Server geht in derselben Sprache; freie
  Lizenzen. .NET 10 statt 8, weil .NET 8 ab 10.11.2026 keine Updates mehr bekommt.
- 06.11.2026 (Tobi): Keine KI und keine Internetverbindung außer dem Update (Datenschutz).
- 06.11.2026 (Tobi): Gestaltung nach Vorbild der Ausgangsliste (`docs/gestaltung.md`).

Von Claude Code (Begründungen; gelten weiter, sofern nicht anders vermerkt):

- **Zwei Nummern.** Die Versionsnummer (z. B. 0.6.0) steht von Hand in
  `Directory.Build.props` und in dieser Datei; die Build-Nummer ist die Laufnummer von
  GitHub Actions und wird beim Bauen in die `.exe` geschrieben. Das Update vergleicht nur
  Build-Nummern. Im Fenster steht beides („Version 0.3.0 · Build 7“).
- **Workflow-Datei bleibt `tests-und-exe.yml`.** Die Laufnummer zählt je Workflow-Datei;
  eine umbenannte Datei finge wieder bei 1 an, und installierte Programme sähen kein neueres
  Release mehr.
- **Übergang ohne Neuinstallation (06.11.2026), geprüft am Code des Python-Updaters:** Er
  nimmt jedes Release `build-N` mit höherer Nummer, das `Dokumentationssystem.exe` und
  `Dokumentationssystem.exe.sha256` enthält, prüft die Prüfsumme, tauscht die Datei aus und
  startet sie – welche Sprache die neue Datei hat, spielt keine Rolle. Die C#-Version löscht
  beim Start die alte Programmdatei (`.exe.alt`) und fragt nach dem Wechsel nicht gleich
  erneut (gleiche Umgebungsvariable wie die Python-Version). Ein Durchlauf mit dem echten
  Release (Laden, Prüfsumme) klappte mit dem C#-Code.
- **Eigene Datenbankdatei `dokumentationssystem.db`.** Die Datei der Python-Version
  (`dokusystem.sqlite3`) wird nicht gelesen und nicht gelöscht (Eintrag „Umstieg“).
  `einstellungen.json` (Update-Häkchen) und `update.log` werden weiter gemeinsam benutzt.
- **„Demo-Daten zurücksetzen“ löscht nur die Bilder der Bewohner in der Datenbank**, nicht
  mehr den ganzen Ordner `bilder\`. Grund: Bilder der Python-Version bleiben so unberührt.
- **Update-Projekt getrennt** (`Dokusystem.Update`): einziges Projekt mit Internetzugriff;
  ein Test prüft, dass Kern und Oberfläche keine Netzbausteine und keine Adressen
  enthalten. Der Dateiname der `.exe` darf abweichen (z. B. „Dokumentationssystem (1).exe“
  nach doppeltem Herunterladen) – wie bei der Python-Version.
- **Abhängigkeiten** (alle mit freier Lizenz): im Programm nur `Microsoft.Data.Sqlite` (MIT,
  Speicher). Nur für Tests: `xunit` und `xunit.runner.visualstudio` (Apache-2.0),
  `Microsoft.NET.Test.Sdk` (MIT). Windows Forms und Segoe UI gehören zu .NET bzw. Windows.
- **Übernommen aus der Ausgangsliste**, angepasst und bereinigt (kein Logo, kein Name der
  Einrichtung oder des Wohnbereichs, keine Personenkürzel, auch nicht in Kommentaren):
  `Stil` (Farben, Schriften, Abstände, Knöpfe, abgerundete Flächen), Kopfbereich mit
  Farbverlauf, Kartenraster, Aufbau der Bewohnerkarte. Die Update-Funktion der Ausgangsliste
  (Update-Ordner) passt nicht; es bleibt der GitHub-Weg.
- **Gestaltung im Einzelnen:** Wo `docs/gestaltung.md` die Ausgangsliste verfeinert, gilt
  `docs/gestaltung.md` (Hauch von Schatten statt mehrlagig, Radius 8 statt 12/16).
  Allergiehinweis mit den Markenfarben: eingetragen = „Beschluss“ (rot), keine bekannt =
  „Offen“ (grün), nicht erfasst = „Achtung“ (gelb, gestrichelt) – immer mit Text und einem
  farbigen Balken links.
- **Demo-Hinweis** rechts oben im Kopf (dort, wo die Ausgangsliste ihr Logofeld hat), als
  Marke „Achtung“; zusätzlich im Fenstertitel und oben im Formular.
- **Marken auf den Karten (0.5.0):** Nur was Aufmerksamkeit braucht, bekommt eine Marke
  (Farbe sparsam): Beschluss mit Enddatum, eingetragene Allergie, „nicht erfasst“ bei
  Allergien oder Beschluss. „Keine bekannt“ und „kein Beschluss“ sind der Normalfall ohne
  Marke – „nicht erfasst“ bleibt dadurch immer sichtbar (Regel 5). Allergie und Beschluss
  teilen sich Rot (wichtig, wie der Allergiehinweis in der Akte), „nicht erfasst“ ist Gelb;
  jede Marke trägt ein Wort. Lange Allergietexte werden auf der Karte mit „…“ gekürzt.
- **Schatten (0.5.0):** Die Karten haben den weichen, mehrlagigen Schatten der Ausgangsliste
  (Eintrag „Gestaltung Stufe 2“ verlangt ihn); `docs/gestaltung.md` sah nur einen Hauch vor.
- **Filterknöpfe** mit Radius 4 statt Pillenform (`docs/gestaltung.md`: keine Pillen-Knöpfe).
  „Nicht erfasst“ zählt Bewohner, bei denen Allergien oder Beschluss nicht erfasst sind. Der
  gewählte Filter bleibt, bis man einen anderen wählt.
- **Farbschlüssel in der Statusleiste:** Die Ausgangsliste hat ihren abgeschafft; hier
  verlangt ihn der Eintrag. Er erklärt Rot, Gelb und Blaugrau mit Worten.
- **Oberflächentests nur unter Windows** (Windows Forms läuft nicht unter Linux); sie laufen
  in GitHub Actions vor jedem Zusammenführen und legen Bildschirmfotos ab.
- **Programmgerüst (07.11.2026):** Das Programm startet im Bereich „Bewohner“, solange die
  Übersicht nur ein Platzhalter ist (Startseite wird sie mit 0.17.0). „Einstellungen“ und
  „Neuer Bericht …“ öffnen ein Fenster und sind keine eigenen Bereiche; „Einstellungen“ steht
  etwas abgesetzt unten in der Liste. Die Ausgangsliste steht als Platzhalter in der Liste;
  mit der Modulverwaltung (0.14.0) lässt sie sich ausblenden.
- **Eigenes Bild im Kopf:** Kopie im Datenordner (`logo\`), Dateiname in
  `einstellungen.json`; das alte Bild wird beim Ersetzen gelöscht. Es steht rechts oben, der
  Demo-Hinweis direkt links daneben, damit er immer sichtbar bleibt (Regel 4).
- **Kürzel:** leer oder bis zu 6 Buchstaben/Ziffern, gespeichert in `einstellungen.json`
  (wie das Update-Häkchen). Gebraucht ab 0.6.0 für Berichte.
- **Tägliche Dokumentation (0.6.0), wo der Eintrag etwas offen ließ:**
  - Schichten nach Uhrzeit: Früh 6–14 Uhr, Spät 14–22 Uhr, Nacht 22–6 Uhr. Die Schicht lässt
    sich im Fenster auch von Hand wählen.
  - „Nächste Schicht“ heißt: gilt bis zum Ende der Schicht nach der, in der geschrieben wurde.
    „3 Tage“, „1 Woche“ und „bis Datum“ gelten bis zum Ende des jeweiligen Tages.
  - „Neu seit deiner letzten Schicht“: entstanden nach deinem letzten Besuch im Bereich
    „Übergabe“ (je Kürzel gemerkt) und von dir noch nicht gelesen. Ohne Kürzel gibt es kein
    „neu“, und „Gelesen“, „Verlängern“, „Erledigt“ sind gesperrt (Hinweis auf die
    Einstellungen).
  - Marke „Übergabe N“ auf der Karte: Ohne eigenes Kürzel zählt sie alle offenen Einträge.
  - Verlängern und Erledigt werden als eigene Ereignisse mit Kürzel und Zeit gespeichert; der
    ursprüngliche Eintrag bleibt unverändert (nichts wird überschrieben).
  - Rückfrage bei Übergabe-Wörtern mit drei Knöpfen: Ja (in die Übergabe, nächste Schicht),
    Nein (ohne Übergabe speichern), Abbrechen (zurück zum Text). Gesucht wird auch in
    zusammengesetzten Wörtern („Arzttermin“ enthält „Arzt“), Groß-/Kleinschreibung egal.
  - Farben: „Übergabe“ Blaugrün, „Wichtig“ Violett, „läuft heute ab“ Gelb (wie „bitte
    klären“), Gültigkeit und Lebensbereiche Blaugrau; immer mit Wort.
  - Eine Karte zeigt jetzt bis zu drei Zeilen Marken (vorher zwei), damit „nicht erfasst“
    auch neben „Übergabe“ und „Wichtig“ sichtbar bleibt (Regel 5).
  - Demo-Daten: Die ausgedachten Berichte sind bei gleichem Datum zu jeder Uhrzeit gleich
    (fester Zufallswert je Bewohner); Berichte in der Zukunft werden weggelassen.
- **Wohnbereich-ToDo (0.7.0), wo der Eintrag etwas offen ließ:**
  - „Hauptbildschirm“: Die Übersicht ist bis 0.17.0 noch ein Platzhalter. Darum steht die
    Kurzliste der Aufgaben vorerst über den Bewohnerkarten (dort startet das Programm); die
    Startseite übernimmt sie später.
  - „Nächste Schicht“ heißt die Schicht nach der, in der die Aufgabe angelegt oder zuletzt
    weitergegeben wurde. Nach dem Weitergeben gilt eine Aufgabe immer für die nächste
    Schicht, auch wenn sie vorher „wer Zeit hat“ oder für eine bestimmte Schicht war.
  - „Jetzt dran“: Die Schicht der Aufgabe hat begonnen (auch wenn sie aus einer früheren
    Schicht liegen geblieben ist).
  - „Fällig bis“ ohne Uhrzeit gilt bis zum Ende des Tages. Die Notiz beim Weitergeben ist
    freiwillig (höchstens 200 Zeichen).
  - „Erledigt“ und „Weitergeben“ brauchen das eigene Kürzel aus den Einstellungen (wie in der
    Übergabe); ohne Kürzel sind die Knöpfe gesperrt, mit Hinweis.
  - Erledigte Aufgaben lassen sich nicht wieder öffnen; dafür legt man eine neue an (nichts
    wird überschrieben).
- **Nachrichten (0.8.0), wo der Eintrag etwas offen ließ:**
  - Kolleginnen, Kollegen und Teams sind Demo-Daten (wie die Bewohner) und werden mit
    „Demo-Daten zurücksetzen“ neu geladen. Pflegen lassen sie sich noch nicht.
  - „Angemeldet als …“ steht unter dem Demo-Hinweis oben rechts. Ohne Anmeldung zeigt der
    Bereich „Nachrichten“ nur einen Hinweis. Ein früher frei eingetragenes Kürzel bleibt
    gültig, bis man jemanden aus der Liste wählt; für Nachrichten braucht es einen Benutzer
    aus der Liste.
  - Sichtbar ist ein Verlauf nur für Beteiligte: wer geschrieben hat oder angeschrieben wurde
    (direkt oder über ein Team).
  - Ordner: Posteingang = Verläufe, in denen ich etwas bekommen habe und die ich nicht
    archiviert habe; Gesendet = Verläufe, in denen ich geschrieben habe; Archiv = von mir
    archiviert. Archivieren gilt je Person; kommt danach eine neue Nachricht an mich, steht
    der Verlauf wieder im Posteingang.
  - „Gelesen“: beim Öffnen eines Verlaufs, je Person. Der Absender sieht nicht, wer gelesen
    hat (ausdrücklich nicht in der Demo).
  - Antworten gehen an alle Beteiligten außer an mich (Absender, Personen und Teams des
    Verlaufs); Betreff und Bewohner übernimmt die Antwort von der ersten Nachricht.
  - „Aus der Akte heraus aufrufbar“: Knopf „Nachrichten …“ in der Akte zeigt die Verläufe zu
    diesem Bewohner (eingegrenzt mit „Bewohner: … ✕“).
  - Zähler und Hinweis zählen ungelesene Nachrichten, Ordnerknöpfe zählen Verläufe.
  - Hinweis aus `IDEEN.md` für einen späteren Echtbetrieb (Mitarbeitervertretung beteiligen,
    auch für „gelesen“ in der Übergabe) gilt weiter.
- **Medikamentendatenbank (0.9.0), wo der Eintrag etwas offen ließ:**
  - Quellen: Für jeden Eintrag eine öffentlich abrufbare Fachinformation (34 auf fachinfo.de,
    Duloxetin aus der deutschen Produktinformation der EMA, Lithium vom Hersteller),
    Abschnitte 1–3, abgerufen am 07.11.2026. Übernommen wurde nur, was dort steht. Stichprobe
    nachgeprüft (Quetiapin, Paracetamol).
  - Wo keine Fachinformation des Erstanbieters frei zugänglich war, steht ein Generikum als
    Handelsname (Haloperidol, Melperon, Pipamperon, Diazepam, Biperiden, Ibuprofen, Macrogol).
    Manche Einträge nennen nur einzelne Stärken, weil die Quelle nur diese abdeckt
    (z. B. Pantoprazol 40 mg, Amlodipin 5 mg). Bei einigen Wirkstoffen bezieht sich die
    Stärke auf das Salz, wie auf der Packung (z. B. Metforminhydrochlorid).
  - „Bestätigt“ setzt man im Fenster „Ändern …“ (Häkchen), nur mit Quelle. Einen Schutz, dass
    nur Tobi das darf, gibt es in der Demo nicht; jede Änderung steht mit Kürzel im Verlauf.
  - Die Quelle wird im Programm nicht geöffnet (kein Internet, Regel 6), sondern lässt sich
    kopieren und im Browser aufrufen.
  - Wirkstoff und Darreichungsform zusammen gibt es nur einmal (sonst Hinweis „gibt es
    schon“). Ohne Anmeldung (Kürzel) kein Speichern.
  - „Demo-Daten zurücksetzen“ lädt den Demo-Bestand neu; selbst angelegte Einträge und
    Änderungen sind danach weg.
- **Medikation (0.10.0), wo der Eintrag etwas offen ließ:**
  - Eine Dosisänderung ohne Etappen ist ein Stufenplan mit einer Stufe; seine letzte Stufe wird
    zur neuen Grundverordnung, die vorige steht im Verlauf („ersetzt“). Endet ein Stufenplan
    auf 0, ist das Medikament ab dieser Stufe abgesetzt.
  - Laufen mehrere Stufenpläne, gilt der zuletzt angelegte, der schon begonnen hat.
  - Eine Pause „bis auf Weiteres“ endet mit „Pause beenden …“ (eigener Eintrag). Während einer
    Pause steht die Verordnung sichtbar da („pausiert bis …“), die Mengen ausgegraut.
  - Bedarfsmedikation hat eine Einzeldosis (Feld „morgens“), Anlass und Höchstmenge als Text;
    keinen Stufenplan.
  - Prüfungen: Mengen nur als Zahl (1, 0,5, ½, 1 ½); nichts vor dem Beginn der Verordnung,
    nichts nach dem Absetzen; Stufen in zeitlicher Reihenfolge; Anordnung (Arzt, Datum) und
    Anmeldung (Kürzel) sind Pflicht.
  - Ausdrücklich nicht gebaut (laut Eintrag): Dosisvorschläge, Wechselwirkungen, Stellen und
    Ausgabe, Bestand.
- **Therapie-Bereich (0.11.0), wo der Eintrag etwas offen ließ:**
  - Bei der Überschneidung warnt das Fenster einmal; ein zweiter Klick auf „Zuteilen“ teilt
    trotzdem zu (Warnung, kein Verbot). Geprüft werden bis 0.12.0 nur andere Therapien;
    Einträge des Wochenplans und des Kalenders kommen mit den jeweiligen Schritten dazu.
  - Ist ein Angebot an einem Tag des Zeitraums voll, wird nicht zugeteilt (Hinweis „voll“).
    Einzel-Angebote haben immer einen Platz.
  - „Teilnahme beenden ab …“: ab diesem Tag nicht mehr dabei; die Zuteilung bleibt im
    Verlauf. Eine Änderung „ab Datum“ darf die Teilnehmerzahl nicht unter die Belegung senken.
  - Therapeutinnen und Therapeuten sind Text (in der Demo ausgedacht, z. B. „Olga Pirol“ aus
    den Demo-Kollegen); Anmeldung und Rechte gibt es nicht (laut Eintrag).
  - Die Teilnehmer erscheinen ab 0.12.0 im Wochenplan des Bewohners.
- **Wochenplan (0.12.0), wo der Eintrag etwas offen ließ:**
  - „Verschieben, ändern“ heißt: Der bisherige Eintrag endet an einem Datum, ein neuer
    beginnt; „löschen“ heißt „entfernen ab …“. So lässt sich für jeden Tag zeigen, welcher Plan
    galt (für den HEB-Bogen).
  - Teilnahme: Der zuletzt eingetragene Stand gilt, frühere bleiben in der Datenbank.
    „Teilgenommen“ geht erst am Tag selbst; „abgesagt“ auch vorher.
  - Überschneidungen sind eine Warnung (zweites „Speichern“ speichert trotzdem), kein Verbot.
  - Einzelne Termine aus dem Kalender kommen mit 0.13.0 dazu.
  - Der Ausdruck listet die Termine je Tag untereinander (lesbarer als ein Stundenraster in
    großer Schrift); keine Allergien, Medikation oder anderen Angaben aus der Akte.
- **Wohnbereichskalender (0.13.0), wo der Eintrag etwas offen ließ:**
  - Ändern und Absagen sind neue Fassungen (nichts wird überschrieben); ein abgesagter Termin
    verschwindet aus den Ansichten, bleibt aber im Verlauf. Löschen gibt es nicht.
  - Eine Heimfahrt gehört zu genau einem Bewohner. Mitgegebene Medikation wird nicht
    berechnet; das Hinweisfeld genügt (laut Eintrag).
  - Die Wochenansicht des Kalenders nutzt dasselbe Raster wie der Wochenplan, mit den Vornamen
    der Beteiligten im Kästchen; ein Klick springt zum Tag mit allen Angaben und Knöpfen.
  - „Abwesend: Heimfahrt bis …“ auf der Startseite kommt mit der Startseite (0.17.0); der Kern
    kennt die laufende Heimfahrt schon.
  - Die Therapie-Zuteilung warnt nicht vor einzelnen Kalenderterminen (die Regelwoche
    überschneidet sich nur an einzelnen Tagen); der Wochenplan zeigt beides nebeneinander.
- 05.11.2026, weiter gültig: Update-Einstellung als Häkchen unten links, gespeichert in
  `einstellungen.json`; `update.log` behält die letzten 300 Zeilen; der Workflow läuft auch
  bei Pull Requests (ohne Release); Name als Vorname und Nachname; Beschlussende mit drei
  Zuständen; Pflichtfelder Vorname, Nachname, Zimmer, Geburtsdatum, Aufnahmedatum; Datum
  auch ohne führende Nullen; Übersicht sortiert nach Nachname; Bilder werden unverändert in
  den Datenordner kopiert (PNG, JPG, BMP, GIF); Demo-Daten werden genau einmal automatisch
  geladen.

- 07.11.2026 (Schritt 11): Modulverwaltung mit zwei Schnittstellen: `IModulDaten` im Kern
  (Tabellen, Demo-Daten) und `IModul` in den gemeinsamen Bausteinen (Seite, Akte-Reiter,
  Marken). Dafür wurden Stil, Karten-Bausteine, Dialoge, Druckvorschau und kleine Formulare in
  das neue Projekt `src/Dokusystem.Bausteine` verschoben – sonst hinge das Modul vom Programm
  ab, und das Programm vom Modul.
- 07.11.2026 (Schritt 11): Das Modulprojekt baut zweimal: die Logik für .NET ohne Fenster
  (Kern-Tests laufen auch unter Linux) und alles zusammen für Windows.
- 07.11.2026 (Schritt 11): Ein/aus steht in der Datenbank (Verwaltungsangaben), nicht in der
  Einstellungsdatei des Arbeitsplatzes: gilt so für den ganzen Wohnbereich, und „Demo-Daten
  zurücksetzen“ schaltet das Modul wieder ein. Ohne Eintrag gilt „eingeschaltet“.
- 07.11.2026 (Schritt 11): Unterbringung ohne Doppelpflegen: Der Beschluss kommt aus der Akte;
  „offen geführt“ (mit Datum der Einverständniserklärung) und „Forensik“ führt das Modul. Wer im
  Modul „Beschluss“ wählt, trägt das Beschlussende in der Akte ein. Ist der Beschluss in der Akte
  „nicht erfasst“, gilt die Unterbringung als nicht erfasst und eine Ausgangsbeschränkung als
  nicht zulässig, bis es geklärt ist (Regel 5).
- 07.11.2026 (Schritt 11): Küchendienst ohne die Regel für Einzelzimmer – der Kern kennt keine
  Zimmerart und keine freien Plätze.
- 07.11.2026 (Schritt 11): Digitaler Ausgang ohne Uhrzeit-Fenster: Beginn und Ende zur aktuellen
  Uhrzeit, Handzeichen ist das Kürzel des angemeldeten Benutzers. Ohne Anmeldung kommt ein
  Hinweis statt eines Eintrags.
- 07.11.2026 (Schritt 11): Verlauf mit Kürzel statt Windows-Anmeldename. Mehrplatz-Schutz,
  Protokoll und Sicherung der Ausgangsliste bleiben draußen (kein Mehrplatzbetrieb im Bauplan).
- 07.11.2026 (Schritt 11): Marken des Moduls: „abwesend“ bekommt eine eigene graue Farbe (neu
  im Farbschlüssel); Ausgangsbeschränkung und „unterwegs“ neutral; Fristen und „überfällig“ in
  der Farbe „bitte klären“ – der Farbschlüssel heißt dafür jetzt „nicht erfasst, Frist“ (statt
  „nicht erfasst – bitte klären“, damit alles in die Statusleiste passt). Auf den Bewohnerkarten nur, was Aufmerksamkeit braucht; Zimmer, Unterbringung und
  Küchendienst nur auf der Seite des Moduls.
- 07.11.2026 (Schritt 11): Keine neuen Abhängigkeiten.
- 07.11.2026 (Schritt 12): Aufzählungszeichen (Strich, Punkt, Nummer am Zeilenanfang) werden beim
  Speichern abgewiesen statt entfernt – das Programm schreibt keinen Text um. Ein Gedankenstrich
  mitten im Satz ist erlaubt.
- 07.11.2026 (Schritt 12): Der Hinweis auf „gibt an, dass …“ erscheint, wenn „vom Bewohner
  angegeben“ angekreuzt ist und der Text weder „gibt an“, „gab an“, „berichtet“, „erzählt“ noch
  „nach eigenen Angaben“ enthält. Er ist nur ein Hinweis; gespeichert wird trotzdem.
- 07.11.2026 (Schritt 12): Einen Abschnitt leeren ist eine neue (leere) Fassung – er zählt dann
  wieder als fehlende Angabe, der Verlauf bleibt. Speichern ohne Änderung legt keine Fassung an.
- 07.11.2026 (Schritt 12): „Als Fließtext kopieren“: Überschrift „Biografie Name“, je Abschnitt
  Titel und Text, bei Angaben des Bewohners darunter „(vom Bewohner angegeben, nicht
  überprüft)“, am Ende „Fehlende Angaben: …“ – ohne Formatierung.
- 07.11.2026 (Schritt 13): Vorgeschlagen wird der vergangene Monat – eine Reflexion schreibt man
  im Rückblick. Für kommende Monate ist keine möglich.
- 07.11.2026 (Schritt 13): Die Therapien stehen einmal oben für den ganzen Monat, nicht je
  Lebensbereich – der Wochenplan ordnet Therapien keinem Lebensbereich zu.
- 07.11.2026 (Schritt 13): Ein Bericht erscheint bei jedem Lebensbereich, dem er zugeordnet ist;
  „allgemeine“ Berichte erscheinen bei keinem.
- 07.11.2026 (Schritt 13): Stilregeln, die nur für bestimmte Felder des HEB-Bogens gelten
  (Ich-Form bei Rahmenzielen), kommen mit dem HEB-Bogen dazu. Datumsangaben gelten als Hinweis,
  weil sie im HEB nur in den Kopffeldern stehen dürfen.
- 07.11.2026 (Schritt 14): „HEB fällig“ fehlt noch – den HEB-Bogen gibt es erst mit Schritt 15;
  die Startseite zeigt nur, was es schon gibt. Die Frist dafür kommt mit dem HEB-Bogen.
- 07.11.2026 (Schritt 14): Das Programm öffnet mit der Übersicht (`Hauptfenster.Startbereich`,
  gesetzt in `Program.cs` und im Selbsttest). Das Hauptfenster selbst beginnt weiter bei
  „Bewohner“, damit die vorhandenen Oberflächentests unverändert gelten.
- 07.11.2026 (Schritt 14): Die Frist „Beschluss endet bald“ steht in der Einstellungsdatei des
  Arbeitsplatzes (wie Kürzel und Update), nicht in der Datenbank.
- 07.11.2026 (Schritt 14): Termine heute = Wochenplan des Bewohners (Regelwoche, Therapien,
  Kalender); abgesagte und ausgefallene Therapien zählen nicht. Abwesend = Heimfahrt laut
  Kalender oder Abwesenheit im Modul Ausgangsliste; meldet beides, steht die Marke nur einmal da.
- 07.11.2026 (Schritt 15): Neue Abhängigkeit **PDFsharp 6.2.4 (MIT)** nur zum Ausfüllen der
  PDF-Formulare, im Kern (plattformunabhängiges Paket). Bewusst nicht die neueren Versionen 6.5.0
  und 7.0.0-preview-2: Sie stehen unter **AGPL-3.0** und sind nach `CLAUDE.md` ausgeschlossen.
  PDFsharp 6.2.4 bringt `System.Security.Cryptography.Pkcs` 8.0.1 mit bekannter Sicherheitslücke
  mit; deshalb ist **System.Security.Cryptography.Pkcs 10.0.13 (MIT)** direkt eingetragen.
- 07.11.2026 (Schritt 15): PDFsharp braucht eine Schriftquelle, auch zum Lesen der Felder. Unter
  Windows nimmt es die Systemschriften; für die Tests unter Linux eine dort vorhandene Schrift.
  Keine Schrift im Repository.
- 07.11.2026 (Schritt 15): HEB B legt Rahmenziele, Leitziel und Nahziele des letzten fertigen
  Bogens als erste Fassung vor (Text eines Menschen, nicht erfunden). Die Rahmenziele landen im
  Feld „Fortschreibung der Rahmenziele“.
- 07.11.2026 (Schritt 15): Quelle „Wochenplan“ ist die Woche am Ende des Zeitraums (Regelwoche und
  Therapien) – eine Auswertung über viele Monate wäre langsam und im Bogen nicht gefragt. Bei
  vielen Berichten stehen die zwölf neuesten neben dem Feld, die übrigen in der Akte.
- 07.11.2026 (Schritt 15): Vorlagen zuteilen, entfernen und zuordnen gilt in den Einstellungen
  sofort (nicht erst mit „Speichern“), weil Dateien kopiert werden.
- 07.11.2026 (Schritt 15): Der Hilfebedarf wird in Textfelder als Zahl übertragen; das Ankreuzen
  im echten Bogen hängt an dessen Feldern und ist erst mit dem echten Bogen prüfbar.
- 07.11.2026 (Schritt 15): Abhängigkeiten jetzt: `Microsoft.Data.Sqlite` (MIT), `PDFsharp` 6.2.4
  (MIT), `System.Security.Cryptography.Pkcs` 10.0.13 (MIT), Tests mit xUnit (Apache-2.0). Zum
  Bauen der Test-PDF einmalig `reportlab` (BSD), nicht im Programm.

- 08.11.2026 (Schritt 16): Marke nur für „offen geführt: ja“ und „nicht erfasst“; „nein“ ist im
  Wohnbereich der Normalfall und bekommt keine Marke (Farbe sparsam).
- 08.11.2026 (Schritt 16): Der Abgleich mit der Ausgangsliste liegt im Modul (es kennt die Akte, die
  Akte kennt das Modul nicht). Widerspruch: Akte „ja“ und Modul „Beschluss“ oder „Forensik“, oder
  Akte „nein“ und Modul „offen geführt“. Ist eine Seite „nicht erfasst“, gibt es keinen
  Widerspruch, sondern die „nicht erfasst“-Marke.
- 08.11.2026 (Schritt 16): Alter bei Geburtstag am 29. Februar: in Nicht-Schaltjahren zählt der
  28. Februar.

- 08.11.2026 (Schritt 17): Datenbank-Ort ändern gilt erst nach dem Neustart (die offene Datenbank
  wird nicht im Betrieb getauscht). Ist der gemeinsame Ordner beim Start nicht erreichbar, startet
  das Programm mit der lokalen Datenbank und meldet das – statt gar nicht zu starten.
- 08.11.2026 (Schritt 17): Konfliktschutz ohne neue Spalten: Bei Daten mit Fassungen (Biografie,
  Reflexion, HEB, Kalender) zählt das Programm die Fassungen beim Öffnen; sind es beim Speichern
  mehr, hat ein anderer gespeichert. Bei den Stammdaten vergleicht es den Bewohner beim Öffnen mit
  dem gespeicherten. Geprüft wird in derselben Transaktion wie das Speichern (sie sperrt die
  Datei). Eingetragenes ohne Überschreiben (Berichte, Nachträge, Übergabe, Aufgaben, Nachrichten,
  Statusmeldungen) kann nicht in Konflikt geraten: Beides bleibt erhalten.
- 08.11.2026 (Schritt 17): „Übersicht“ im Eintrag ist die Startseite: Sie zeigt den eigenen
  Bereich, umschaltbar. Die Seite „Bewohner“ zeigt alle (wie ein Verzeichnis) und kennzeichnet
  Bewohner anderer Wohnbereiche mit einer Marke. Die Wahl „alle Bereiche“ gilt, bis das Programm
  schließt.
- 08.11.2026 (Schritt 17): Statusmeldung auf der Karte kurz („Status: …, bis 11:30“, zu Langes
  kürzt die Karte), oben in der Akte lang mit Bereich, Kürzel und Uhrzeit. Höchstens 80 Zeichen.
  Farbe wie „Übergabe“ (beides ist Information für die Kolleginnen und Kollegen), Wort „Status:“
  davor.
- 08.11.2026 (Schritt 17): Der 30-Sekunden-Abgleich liest in der Akte nur Kopf und Stammdaten neu;
  die Reiter (Berichte, Medikation, Reflexion, HEB …) lesen beim Wechsel des Reiters neu, damit
  eine angefangene Eingabe nicht verschwindet. Offene Bearbeitungsfenster bleiben unberührt; ihr
  Speichern prüft auf Konflikte.
- 08.11.2026 (Schritt 17): Demo: zwei Bewohner (Ginster, Immergrün) in „Wohnbereich B“, damit der
  Umschalter etwas zeigt und die übrigen Demo-Abläufe gleich bleiben. Bereich „Leitung“ und
  „Therapie“ entsprechen den Teams der Demo-Nachrichten.

- 08.11.2026 (Schritt 18): Die farbige Hauptaktion der Akte bleibt „Bearbeiten …“; „Neuer Bericht
  …“ steht als normaler Knopf daneben (einfachste Lösung, nichts Gewohntes verschiebt sich).
- 08.11.2026 (Schritt 18): Die Übersicht (Startseite) blieb, wie sie ist (Wunsch Tobi); ihr
  Kartenraster füllte die Fläche schon. Fenster zum Bearbeiten (Dialoge) behalten ihre Größe – sie
  sind keine Ansichten im Hauptfenster.
- 08.11.2026 (Schritt 18): Wochenplan: Eine Stunde ist mindestens so hoch wie bisher (44 Punkte);
  ist mehr Platz, wachsen die Stunden, bis der Plan den unteren Rand erreicht.
- 08.11.2026 (Schritt 18): Symbole auch im ausgeklappten Menü (vor dem Namen), damit man die
  Symbole der eingeklappten Leiste wiedererkennt. Ein Modul ohne eigenes Symbol bekommt ein
  allgemeines Baustein-Symbol.

- 08.11.2026 (Schritt 19): Rechte prüft die Oberfläche (gesperrte Knöpfe); der Kern speichert
  weiterhin, was er bekommt. Für die Demo reicht das; im Echtbetrieb müsste auch der Speicher
  prüfen. Ein gesperrter Knopf ist grau, aber nicht ausgeschaltet: Windows zeigt an
  ausgeschalteten Knöpfen keinen Hinweis. Ein Klick zeigt den Grund und tut sonst nichts.
- 08.11.2026 (Schritt 19): Vorbelegung, wo Tobi nichts festgelegt hat: Die „drei Leitungen“ sind
  Geschäftsführung, Hausleitung und Verwaltung (sie dürfen alles). Bereichsleitung: alles für den
  Wohnbereich außer Medikamentendatenbank verwalten, Therapien, Mitarbeiter (alle), Rechte und
  Module. Mitarbeiter Wohnbereich: Bewohner, Berichte, Visiten, Medikation und Medikament anlegen
  (nur Fachkräfte), Teilnahme, Fahrzeuge, HEB. Psychologischer Fachdienst: Berichte, Visiten,
  Medikation, Medikamente anlegen und verwalten, Teilnahme, HEB. Pädagogischer Fachdienst:
  Bewohner, Berichte, Visiten, Teilnahme, HEB. Therapie: Berichte, Therapien, Teilnahme, Fahrzeuge.
  „Demo-Daten zurücksetzen“: Leitungen und Bereichsleitung.
- 08.11.2026 (Schritt 19): „Mitarbeiter verwalten (eigener Bereich)“: Bearbeiten darf man
  Mitarbeiter mit mindestens einem gemeinsamen Bereich, zuordnen nur die eigenen Bereiche.
  Bereiche und Positionen pflegt, wer „Mitarbeiter verwalten (alle)“ hat.
- 08.11.2026 (Schritt 19): Nicht angemeldet (nach „Abmelden“) ist alles gesperrt; beim Start ist
  die Anmeldung Pflicht („Beenden“ schließt das Programm).
- 08.11.2026 (Schritt 20): Bezugspflege und Einzeltrainer ändert man in einem eigenen kleinen
  Fenster aus den Stammdaten („Bezug ändern …“), nicht im Formular „Bearbeiten …“ – das ist schon
  so hoch, dass es auf kleinen Bildschirmen nicht mehr ganz zu sehen wäre. Das Kürzel der
  Bezugspflege steht als farbige Marke neben dem Zimmer (der Streifen selbst ist zu schmal für
  Schrift). „Meine …“-Filter zeigen Bewohner aller Bereiche.
- 08.11.2026 (Schritt 19): Bereich des Rechners (0.20.0) und Bereiche der Mitarbeiter sind dieselbe
  Liste; die Statusmeldung trägt weiter den Bereich des Rechners. Die Einstellungen der Module und
  Fristen bekommen ihre Rechte mit Schritt 29 (neue Einstellungen).
- 08.11.2026 (Schritt 21): Der Medikamentenplan ist eine Windows-Tabelle (DataGridView) ohne eigene
  Bildlaufleiste – so hoch wie ihr Inhalt, gerollt wird die Akte. Status „aktiv“ statt
  „Grundverordnung“ (Wortlaut des Auftrags); „beginnt später“ und „abgesetzt“ stehen in derselben
  Tabelle mit ihrem Status statt in eigenen Abschnitten. Die Einheit steht nur bei den Mengen,
  wenn es nicht „Stück“ sind (z. B. „20 Tropfen“).
- 08.11.2026 (Schritt 21): Klick auf eine Zeile öffnet die Einzelheiten; wählt man dort „Pause …“
  usw., schließt das Fenster und die Änderung öffnet danach (keine Fenster in Fenstern).
- 08.11.2026 (Schritt 21): Die Medikamentendatenbank ist ein eigenes Fenster aus den
  Einstellungen, Abschnitt „MEDIKAMENTE“; die Neugliederung der Einstellungen kommt mit Schritt 29.
  „Sperren“ fragt nicht nach (umkehrbar, steht im Verlauf). Beim Ändern bleibt „gesperrt“ erhalten.
- 08.11.2026 (Schritt 21): Prüfdatum: beim Ankreuzen von „bestätigt“ mit heute vorbelegt, nicht in
  der Zukunft. Die Einträge des Demo-Bestands bleiben „ungeprüft“ (wie bisher, „bis Tobi sie
  bestätigt“) – damit ist das Kennzeichen beim Verordnen gleich zu sehen.
- 08.11.2026 (Schritt 21): Zulassungsnummer ohne feste Form (bis 30 Zeichen: Ziffern, Buchstaben,
  Punkt, Bindestrich, Schrägstrich, Leerzeichen) – der Auftrag nennt „ENR“, im BfArM gibt es
  daneben die Zulassungsnummer mit Punkten; beide passen hinein.
- 08.11.2026 (Schritt 21): Nachschlage-Ansicht: Verbunden wird erst auf „Nachschlagen“, nicht beim
  Öffnen des Formulars. Die BfArM-Suche nimmt keinen Suchbegriff in der Adresse an; deshalb legt
  das Programm den Namen (nur Buchstaben, Ziffern, Leerzeichen, Bindestrich, höchstens 60 Zeichen)
  in die Zwischenablage, eingefügt wird mit Strg+V. Zulässig sind nur https-Adressen unter
  bfarm.de und pharmnet-bund.de (die Suche leitet über auth.bfarm.de zu portal.bfarm.de weiter).
  Fremde Links, neue Fenster und Downloads gehen an den normalen Browser. Die „feste Liste in den
  Einstellungen“ wird dort nur angezeigt, nicht geändert (Regel 6). Daten der Ansicht (Cookies,
  Zwischenspeicher) im Datenordner `webansicht\`; Passwort- und Formularspeicher und die
  Entwicklerwerkzeuge sind aus.
- 08.11.2026 (Schritt 21): „In Feld übernehmen“ geht in das zuletzt angeklickte Feld der Maske
  (Anzeige „→ Stärken“). Bei Handelsnamen und Stärken kommt der Text dazu (mit Komma, ohne
  Doppelte), sonst ersetzt er den Inhalt. Der Text wird eine Zeile, höchstens 300 Zeichen.
- 08.11.2026 (Schritt 21): Das Formular „Medikament“ hat eine Bildlaufleiste; Fehler, „Speichern“
  und „Abbrechen“ bleiben unten immer sichtbar. Grund: Mit der Nachschlage-Ansicht wäre es auf
  Bildschirmen mit wenig Höhe (z. B. Laptop mit 150 % Skalierung) höher als der Bildschirm. Die
  Spalten des Medikamentenplans sind nie schmaler als ihre Überschrift; ist die Akte schmaler,
  rollt die Tabelle waagerecht.
- 08.11.2026 (Schritt 21): Neue Abhängigkeit **`Microsoft.Web.WebView2` 1.0.4314.35** (Lizenz im
  Paket: BSD-3-Clause-artig, auf nuget.org am 08.11.2026 nachgesehen). Grund: die einzige Art,
  eine Webseite im Programmfenster zu zeigen, deren Browser Windows selbst mitbringt (Edge
  WebView2, in Windows 11 enthalten) – sonst müsste ein ganzer Browser in die `.exe`. Der
  WPF-Teil des Pakets wird beim Bauen entfernt (`Directory.Build.targets`). Die kleine
  `WebView2Loader.dll` steckt in der `.exe` und wird beim Start entpackt (wie SQLite,
  nicht komprimiert); der Selbsttest der `.exe` prüft, dass sie dabei ist. Ein Test prüft, dass
  die Bibliothek nur in `Nachschlageansicht.cs` benutzt wird.
- 08.11.2026 (Schritt 21): Abhängigkeiten jetzt: `Microsoft.Data.Sqlite` (MIT), `PDFsharp` 6.2.4
  (MIT), `System.Security.Cryptography.Pkcs` 10.0.13 (MIT), `Microsoft.Web.WebView2` (BSD-3-Clause-
  artig); Tests mit xUnit (Apache-2.0).
- 08.11.2026 (Schritt 21): „Beim Start prüft das Programm, ob WebView2 vorhanden ist“: Das Ergebnis
  steht in den Einstellungen unter „PROGRAMM → Nachschlage-Ansicht“ („WebView2 vorhanden (Version
  …)“ oder „fehlt“). Einen eigenen Abschnitt „Über“ gibt es erst mit Schritt 29.

## Testliste für Tobi

Version 0.24.0:

1. Akte Anton Ahorn → Reiter „Medikamentenplan“: Tabelle mit Quetiapin und Pantoprazol, Spalten
   „verordnet von“ und „seit“, Status „aktiv“. Auf „Medikament“ klicken → sortiert; „seit“ →
   nach Datum. Zeilen abwechselnd hell/grau.
2. Berta Birke: Status „Stufe 2 von 3 · nächste Stufe ab …“. Clara Clematis: „pausiert bis …“
   (gelb). Dieter Distel: Tabelle „Bei Bedarf“ mit Anlass und „höchstens“.
3. Eine Zeile anklicken → Fenster mit Einzelheiten und Verlauf; „Pause …“ → Pause eintragen →
   Tabelle zeigt „pausiert bis …“. Als „KL“ (keine Fachkraft): Knöpfe grau, Grund beim Klick.
4. „Neue Verordnung …“: In der Auswahl steht hinter jedem Medikament „– ungeprüft“.
5. Links ist „Medikamente“ verschwunden. Einstellungen → „Medikamente“ → „Medikamentendatenbank
   …“ öffnet die Liste. Als „PR“: „Neues Medikament …“ → oben die Nachschlage-Ansicht.
6. Wirkstoff „Quetiapin“ eintragen → „Nachschlagen“: Die Suche des BfArM lädt (Internet nötig),
   unten steht „„Quetiapin“ liegt in der Zwischenablage“ → im Suchfeld Strg+V. In der Ansicht
   „25 mg“ markieren, in der Maske „Stärken“ anklicken, „In Feld übernehmen“ → steht im Feld.
   „Adresse als Quelle übernehmen“ → Quelle gefüllt. Einen Link zu einer fremden Seite anklicken →
   öffnet im normalen Browser, nicht in der Ansicht.
7. „bestätigt“ ankreuzen → „Geprüft am“ ist heute; Speichern → Karte „geprüft am … von PR“. Bei
   „Neue Verordnung …“ steht dieses Medikament ohne „– ungeprüft“.
8. Karte → „Sperren“ → „gesperrt – nicht wählbar“; bei „Neue Verordnung …“ fehlt es. „Wieder
   freigeben“. „Verlauf …“ zeigt beides.
9. Einstellungen → Programm: „Nachschlage-Ansicht: WebView2 vorhanden (Version …)“. Einstellungen
   → Medikamente → Häkchen „Nachschlage-Ansicht anzeigen“ weg → Speichern → im Formular nur noch
   „Im Browser öffnen“.
10. Übersicht im kleineren Fenster: „Meine Bezugsbewohner“, „Meine Einzeltrainings“ und „alle
    Bereiche“ überdecken sich nicht mehr.

Version 0.23.0:

1. Übersicht: Karten mit farbigem Streifen links und Kürzel neben dem Zimmer (Bezugspflege);
   bei Berta Birke ein Punkt „DBT · PR“. Unten in der Statusleiste die Erklärung.
2. Als „AB“: „Meine Bezugsbewohner“ → fünf Karten. Als „PR“: „Meine Einzeltrainings“ → Berta
   Birke und Gustav Ginster (Wohnbereich B).
3. Akte Karl Kiefer → Stammdaten: Bezugspflege Karin Lerche. „Bezug ändern …“ → Anna Bachstelze
   → Hinweis „6 Bewohner“; Einzeltrainer Bruno Meise, Art „Achtsamkeit“ eintippen → Speichern.
   „Verlauf …“ zeigt auch den früheren Eintrag.
4. Einstellungen → „Meine Farbe“ → eine Farbe wählen → Streifen und Punkte dieser Person ändern
   sich (auch auf dem zweiten Rechner).

Version 0.22.0:

1. Programm starten: Fenster „Anmelden“. Unten die ausgedachten Mitarbeiter; ein Klick übernimmt
   das Kürzel. „HI“ eintippen → „ausgeschieden“. Mit „OP“ anmelden.
2. Akte Anton Ahorn: „Bearbeiten …“ ist grau; darüberfahren oder klicken → „Dafür fehlt die
   Berechtigung.“ „Neuer Bericht …“ geht.
3. Oben rechts „Angemeldet als Olga Pirol (OP)“ → als „KL“ anmelden → Reiter „Medikation“: Knöpfe
   grau mit „Nur für Fachkräfte.“ Als „CD“: Medikation geht, unter „Therapie“ ist „Neues
   Angebot …“ grau.
4. Als „UT“ (Geschäftsführung): Einstellungen → „VERWALTUNG“ → „Rechte …“: Tabelle mit Häkchen;
   bei „Therapien bearbeiten“ die Spalte „Mitarbeiter Wohnbereich“ anhaken → als „CD“ ist „Neues
   Angebot …“ frei.
5. Als „EF“ (Bereichsleitung A): „Mitarbeiter …“ → Georg Habicht (B) nicht bearbeitbar, Paula
   Rabe (A und B) schon; „Neuer Mitarbeiter …“ nur mit Bereich A.
6. Als „PR“: Übersicht zeigt Wohnbereich A und B (10 Bewohner).
7. „Abmelden“: alles grau, oben „Nicht angemeldet – Anmelden“.
8. Oben rechts im Kopf: gestrichelter Platzhalter „Logo der Einrichtung“. Klick → Einstellungen →
   „Bild im Kopf“ → eigenes Bild wählen → der Platzhalter ist weg, das Bild steht dort.

Version 0.21.0:

1. Unten links „Version 0.21.0 · Build …“. Links steht kein „Neuer Bericht …“ mehr; vor jedem
   Eintrag ein Symbol.
2. Akte Anton Ahorn → Reiter „Wochenplan“: Die Tage gehen über die ganze Breite, der Plan reicht
   bis unten. Fenster größer und kleiner ziehen: Alles wächst mit, nichts wird abgeschnitten
   (auch bei 1366 × 768).
3. Oben links den Menü-Knopf (drei Striche) drücken: Das Menü wird schmal, nur Symbole; über
   einem Symbol erscheint der Name; „Aufgaben“ zeigt die Zahl klein am Symbol. Unten: Info-Symbol
   (Version und Bereich) und „Demo-Daten zurücksetzen“ als Symbol. Programm neu starten → es
   bleibt eingeklappt. Wieder ausklappen.
4. Neuer Bericht: in der Akte oben „Neuer Bericht …“; im Reiter „Berichte“; in der Übersicht oder
   unter „Bewohner“ Rechtsklick auf eine Karte → „Neuer Bericht …“. Der Bewohner steht jeweils
   schon fest.

Version 0.20.0 (zwei Programmfenster genügen):

1. Unten links „Version 0.20.0 · Build …“ und darüber „Wohnbereich A · Datenbank lokal“.
2. Übersicht: 8 Bewohner, Kopf „… · Wohnbereich A · …“. Anton Ahorn vorne mit „Status: in der
   Holzwerkstatt, bis …“. Häkchen „alle Bereiche“ → 10 Bewohner, Gustav Ginster und Ida Immergrün
   mit „Wohnbereich B“, Ida mit „Status: Besuch bei der Familie …“.
3. Akte Anton Ahorn: oben „Status: in der Holzwerkstatt · bis … · Therapie, OP um …“; in den
   Stammdaten „Wohnbereich: Wohnbereich A“. „Status …“ → Verlauf zeigt auch „beim Arzttermin“.
   Angemeldet eine neue Meldung setzen, dann „Aufheben“.
4. Einstellungen → „MEHRPLATZ“ → „Gemeinsamen Ordner wählen …“ → einen leeren Ordner (zum
   Probieren z. B. auf dem Desktop) → „Speichern“ → unten links „neuer Ort nach Neustart“.
   Programm schließen und **zweimal** starten. Im zweiten Fenster in den Einstellungen den Bereich
   auf „Wohnbereich B“ stellen.
5. Fenster 1: bei Berta Birke einen Status setzen. Fenster 2 (Übersicht, „alle Bereiche“): nach
   spätestens 30 Sekunden steht er auf der Karte. Ebenso eine Nachricht oder Aufgabe: Der Zähler
   im anderen Fenster stimmt nach spätestens 30 Sekunden.
6. Konflikt: In beiden Fenstern die Akte Berta Birke → „Bearbeiten …“ öffnen. In Fenster 1 das
   Zimmer ändern und speichern, in Fenster 2 ein anderes Zimmer speichern → „Inzwischen geändert“
   mit beiden Fassungen. „Andere übernehmen“ zeigt die Akte mit der Änderung aus Fenster 1.
   Dasselbe mit der Biografie oder einem Kalendertermin.
7. Ordner nicht erreichbar machen (umbenennen) und das Programm starten → Meldung, das Programm
   arbeitet lokal. Zurück auf lokal: Einstellungen → „Lokal (Vorgabe)“.

Version 0.19.0:

1. Unten links „Version 0.19.0 · Build …“. Akte Berta Birke: in den Stammdaten „Offen geführt:
   ja“, neben dem Geburtsdatum das Alter; oben die Marke „offen geführt“.
2. Akte Dieter Distel: „Offen geführt: nicht erfasst“ (kursiv, gelb), oben „offen geführt:
   nicht erfasst“. Filter „Nicht erfasst“ in der Bewohnerübersicht zeigt ihn.
3. Akte Ida Immergrün: Marke „Unterbringung widersprüchlich – bitte klären“ (Akte „nein“,
   Ausgangsliste „offen geführt“). Auf der Seite „Ausgangsliste“ steht der Widerspruch
   ausführlich. Modul in den Einstellungen ausschalten → der Hinweis verschwindet.
4. „Bearbeiten …“: „Offen geführt“ mit ja / nein / nicht erfasst; neuer Bewohner beginnt bei
   „nicht erfasst“.

Version 0.18.0:

1. Unten links „Version 0.18.0 · Build …“. Übersicht: Anton Ahorn und Ida Immergrün tragen
   „HEB fällig …“.
2. Akte Anton Ahorn → Reiter „HEB“: oben „HEB fällig …“, darunter HEB B Nr. 3 (fertig).
3. Angemeldet „Neuer HEB-Bogen …“ → HEB B, Zeitraum ab dem Ende des letzten Bogens, „Laufende
   Nummer: 4“ → „Anlegen“: Im Reiter „1 Beziehungen“ steht unter „d) Fortschreibung der
   Rahmenziele“ der Text des Vorberichts; „Ziele“ zeigt Leitziel und Nahziele.
4. In ein Feld schreiben: Zähler „… / 340“ (rot, wenn zu lang), Hinweise z. B. bei Semikolon
   oder Datum. In ein Feld klicken → „Textbaustein einfügen ▾“ → Baustein wird eingefügt.
5. Hilfebedarf: steht auf „noch nicht eingeschätzt“; eine Stufe wählen, „Speichern“.
6. „Drucken …“ zeigt alle Felder über mehrere Seiten.
7. Einstellungen → „HEB-VORLAGEN“ → bei HEB B „Zuteilen …“ eine PDF mit Formularfeldern (zum
   Probieren `tests/daten/heb-test.pdf` aus dem Repository) → „Zuordnung …“ zeigt Programmfeld
   und PDF-Feld. Im Bogen „PDF ausfüllen …“ → Meldung mit dem Pfad; die Datei im Ordner `heb`
   des Datenordners öffnen.
8. Mit dem echten Bogen des Kostenträgers (nur auf dem eigenen Rechner, nie ins Repository):
   zuteilen, Zuordnung prüfen, ausfüllen – und berichten, was nicht passt.

Version 0.17.0:

1. Programm starten: Es öffnet die „Übersicht“. Unten links „Version 0.17.0 · Build …“.
2. Oben „Heute“: Anzahl Bewohner, Abwesende, Termine, Änderungen in der Medikation; darunter
   die Termine des Tages und „Frieda Fichte: Metformin … – Pause beginnt“.
3. Karten: Hanna Holunder „abwesend: …“ (nur einmal), Clara Clematis „Beschluss endet in …
   Tagen“, Termine „heute …“.
4. Klick auf eine Karte → Akte.
5. Einstellungen → „FRISTEN“ auf 14 Tage → in der Übersicht verschwindet „Beschluss endet …“ bei
   Clara Clematis.
6. Im Kalender einen Termin für heute anlegen, zurück zur Übersicht → er steht unter „Termine
   heute“.

Version 0.16.0:

1. Unten links „Version 0.16.0 · Build …“. Akte Clara Clematis → Reiter „Reflexion“: der
   vergangene Monat, Status „Entwurf“; im ersten Bereich Hinweise (Semikolon, Gedankenstriche).
2. Neben jedem Bereich die Berichte des Monats zu diesem Bereich; oben die Therapien mit
   Teilnahme.
3. Angemeldet einen Text ändern → Zeichenzahl und Hinweise ändern sich beim Tippen;
   „Speichern“ → „Entwurf gespeichert.“
4. „Als fertig markieren“ → Felder grau und schreibgeschützt; „Wieder als Entwurf“ öffnet sie.
   „Verlauf …“ zeigt alle Fassungen.
5. Text ändern und mit ◀ den Monat wechseln → Rückfrage „Jetzt speichern?“.
6. Akte Anton Ahorn → Reflexion „fertig“, ohne Hinweise.

Version 0.15.0:

1. Unten links „Version 0.15.0 · Build …“. Akte Anton Ahorn → Reiter „Biografie“: sechs
   Abschnitte, „Wichtige Lebensereignisse“ als „Keine Angabe – fehlt noch.“, unten „Fehlende
   Angaben: …“.
2. „Interessen und Gewohnheiten“ → „Verlauf …“: zwei Fassungen mit Datum und Kürzel.
3. Angemeldet „Bearbeiten …“ bei einem leeren Abschnitt: Text mit „- “ am Zeilenanfang →
   Fehlermeldung; als Fließtext → gespeichert, fehlt nicht mehr in „Fehlende Angaben“.
4. „vom Bewohner angegeben“ ankreuzen ohne „gibt an …“ → Hinweis im Fenster und auf der Karte.
   Akte Dieter Distel zeigt das schon in der Demo.
5. „Als Fließtext kopieren“ → in einen Editor einfügen: reiner Text, fehlende Angaben am Ende.

Version 0.14.0:

1. Unten links „Version 0.14.0 · Build …“. Links „Ausgangsliste“ (vor „Einstellungen“):
   oben Küchendienst heute und morgen, darunter je Bewohner eine Karte nach Zimmer. Hanna
   Holunder ist abwesend (Heimfahrt), Emil Eiche hat eine Ausgangsbeschränkung, Karl Kiefer ist
   im Krankenhaus; Clara Clematis ist unterwegs, Gustav Ginster überfällig.
2. „Bewohner“: Die Karten zeigen jetzt auch „abwesend: …“, „unterwegs seit …“ und Fristen.
3. Oben rechts anmelden. Anton Ahorn „Ausgang …“ → „Allein +1“, „Info anfügen …“, „Übernehmen“
   → Karte geändert; „Verlauf …“ zeigt die Änderung mit Kürzel.
4. Berta Birke (offen geführt) „Anwesenheit …“: Ausgangsbeschränkung ist gesperrt, mit
   Hinweis. „Heimfahrt bis …“ wählen, Datum ergänzen, „Abwesenheit“ → grau; wieder aufheben →
   der alte Ausgang ist wieder da.
5. Dieter Distel „Unterbringung …“ → „Unterbringungsbeschluss“, Datum → in der Akte steht das
   Beschlussende.
6. Anton Ahorn „Geht raus: allein“ → „unterwegs seit …“; „Ist zurück“. „Nachtbericht …“ →
   „Alle kopieren“ und in einen Bericht einfügen.
7. „Küchendienst …“: Plan; einen Tag ab morgen wählen → „Tausch …“; „Zimmergruppen …“;
   Drucken.
8. „Tagesliste drucken …“ → A4 quer mit „von | bis | Hz.“ und der Marke „Küche“.
9. Akte Hanna Holunder → Reiter „Ausgang“: Karte und Verlauf.
10. Einstellungen → „MODULE“: „Ausgangsliste“ aus → weg aus Seitenleiste, Akte und Karten;
    wieder an → alles wieder da. Küchendienst oder Digitaler Ausgang einzeln abschalten.

Version 0.13.0:

1. Unten links „Version 0.13.0 · Build …“. Links „Kalender“: Monatsansicht mit farbigen
   Einträgen; heute hervorgehoben; Hanna Holunder ist auf Heimfahrt (über mehrere Tage).
2. „Woche“ und „Tag“ umschalten; ◀ ▶ blättern; Filter „Anton Ahorn“ zeigt nur seine Termine.
3. Klick auf einen Tag im Monat → Tagesansicht mit Karten (Art, Zeit, Bewohner, Ort, Hinweis).
4. Angemeldet „Neuer Termin …“ → „Tagesausflug“ mit Ziel, Treffpunkt, zwei Bewohnern →
   Karte mit „Teilnehmerliste drucken …“.
5. „Ändern …“ → Marke „geändert“; „Absagen …“ → Rückfrage, dann weg; „Verlauf …“ zeigt alles.
6. Akte Anton Ahorn → Reiter „Wochenplan“, Woche mit seinem Hausarzttermin → violettes
   Kästchen.
7. „Drucken …“ im Kalender (Monat und Woche).

Version 0.12.0:

1. Unten links „Version 0.12.0 · Build …“.
2. Akte Anton Ahorn → Reiter „Wochenplan“: diese Woche mit Holzwerkstatt (Mo, Mi),
   Bewegungsgruppe (Do), Wäschetag (Di), Einkaufen (Sa), Spaziergang (So); heute hervorgehoben.
3. ◀ ▶ blättern; „Diese Woche“ springt zurück. In vergangenen Wochen stehen bei Therapien
   „TEILGENOMMEN“, „ABGESAGT“ oder „AUSGEFALLEN“.
4. Angemeldet auf eine Therapie klicken → Teilnahme wählen. Auf einen freien Eintrag klicken
   → „Ändern ab …“ (z. B. ab nächster Woche eine andere Uhrzeit) → diese Woche alt, nächste
   Woche neu; „Entfernen ab …“.
5. „Neuer Eintrag …“ am Montag 10–11 Uhr → Warnung „Überschneidung mit: Holzwerkstatt …“.
6. „Verlauf …“ zeigt neu/geändert/entfernt mit Datum und Kürzel.
7. „Drucken …“ → A4 quer, große Schrift, nur Name und Plan.

Version 0.11.0:

1. Unten links „Version 0.11.0 · Build …“. Links „Therapie“: sieben Angebote, oben
   „7 Angebote · … Teilnahmen · … freie Plätze“.
2. „Holzwerkstatt“: Gruppe, Mo und Mi 9:00–11:30, „4 Plätze frei“, Teilnehmer Anton Ahorn und
   Dieter Distel. „Einzelgespräch“: „voll“.
3. „Stand am“ auf ein Datum vor drei Wochen → Bewegungsgruppe noch Do 14:00–15:00 und
   „geändert ab …“.
4. Angemeldet „Neues Angebot …“ (z. B. Montag 10–11 Uhr) → Karte erscheint. „Bewohner
   zuteilen …“ Anton Ahorn → Warnung „Überschneidung mit: Holzwerkstatt …“; nochmals
   „Zuteilen“ → er steht als Teilnehmer da.
5. „Teilnahme beenden …“ ab morgen → „(bis heute)“ hinter dem Namen; „Verlauf …“ zeigt alles.
6. „Ändern ab …“ mit einem Datum in einer Woche → bis dahin gilt die alte Zeit.

Version 0.10.0:

1. Unten links „Version 0.10.0 · Build …“.
2. Akte Clara Clematis → Reiter „Medikation“: Mirtazapin mit „pausiert bis …“, Mengen
   ausgegraut. Oben einen Tag nach der Pause eintragen → „Grundverordnung“.
3. Akte Berta Birke: Sertralin „Stufe 2 von 3“, daneben „nächste Stufe ab …“.
4. Akte Emil Eiche: Olanzapin wird ausgeschlichen (nächste Stufe 0–0–0–0). Akte Hanna Holunder:
   leer; „Abgesetzte zeigen“ → Escitalopram „abgesetzt am …“.
5. Angemeldet „Neue Verordnung …“ bei Anton Ahorn: ohne Arzt → Fehlermeldung; mit Arzt →
   neue Karte „Grundverordnung“. Dann „Pause …“ bis übermorgen → „pausiert bis …“.
6. „Stufenplan …“ mit zwei Stufen (z. B. ½ ab heute, 0 in einer Woche) → „Stufe 1 von 2“;
   „Verlauf …“ zeigt Grundverordnung, Pause und Stufenplan mit Anordnung und Kürzel.
7. „Drucken …“ → Druckvorschau „Was gilt am …“ mit dem roten Vermerk „Demo — nicht zum
   Stellen verwenden“.

Version 0.9.0:

1. Unten links „Version 0.9.0 · Build …“. Links „Medikamente“ öffnen: oben „36 Medikamente ·
   Demo-Bestand“ und der Hinweis „Demo-Bestand, nicht zum Verordnen verwenden“.
2. Suche „Seroquel“ → Quetiapin mit 25, 100, 200, 300 mg, Filmtablette, „ungeprüft“.
3. „Adresse kopieren“ und die Adresse im Browser öffnen: Die Fachinformation zeigt dieselben
   Stärken.
4. Angemeldet (oben rechts) „Neues Medikament …“ mit Wirkstoff, Form und Stärke → Eintrag mit
   „selbst angelegt“.
5. Bei einem Eintrag „Ändern …“ eine Stärke ergänzen → Marke „geändert (2 Fassungen)“,
   „Verlauf …“ zeigt die Änderung mit Kürzel.
6. Bei einem Eintrag „bestätigt“ anhaken (nach eigener Prüfung der Quelle) → Marke grün.

Version 0.8.0:

1. Unten links „Version 0.8.0 · Build …“. Oben rechts unter dem Demo-Hinweis „Nicht angemeldet
   – Benutzer wählen ▾“ (oder dein früheres Kürzel).
2. Darauf klicken → „Anna Bachstelze (AB)“ wählen. Links bei „Nachrichten“ steht „3“, über den
   Bewohnerkarten „3 ungelesene Nachrichten für Anna Bachstelze“.
3. Nachrichten: Posteingang mit drei Verläufen, „Arzttermin am Donnerstag“ oben mit „2 neu“
   und „Bewohner: Anton Ahorn“. Öffnen → beide Nachrichten, der Zähler sinkt auf 1.
4. „Antworten …“ → geht an Emma Fink, Team Wohnbereich A, Karin Lerche. Senden → steht unten im
   Verlauf. „Archivieren“ → Verlauf steht unter „Archiv“.
5. „Neue Nachricht …“ an „Team Therapie“ mit Betreff und Text → danach oben rechts zu „Olga
   Pirol (OP)“ wechseln: Sie hat die Nachricht im Posteingang.
6. Akte Clara Clematis → „Nachrichten …“: nur Verläufe zu Clara; „Bewohner: … ✕“ hebt das auf.
7. Einstellungen: „Angemeldet als“ zeigt dieselbe Person wie oben rechts.
8. „Demo-Daten zurücksetzen …“ → Nachrichten wieder im Demo-Zustand.

Version 0.7.0:

1. Unten links „Version 0.7.0 · Build …“. Links bei „Aufgaben“ ein roter Zähler „5“ (eine
   Demo-Aufgabe ist überfällig).
2. Über den Bewohnerkarten: „AUFGABEN · 5 OFFEN · 1 ÜBERFÄLLIG“ mit drei Zeilen, oben die
   überfällige. „Alle 5 Aufgaben …“ führt zum Bereich „Aufgaben“.
3. Bereich „Aufgaben“: oben „Aushang mit dem Wochenplan …“ mit „überfällig seit …“, rotem
   Balken und der Weitergabe „Drucker war leer …“.
4. Einstellungen: Kürzel „TS“. „Neue Aufgabe …“: ohne Text speichern → Fehlermeldung.
   „bestimmte Schicht“ wählen, Tag in zwei Tagen, Frühdienst → Aufgabe erscheint mit „für
   Frühdienst …“, der Zähler steigt.
5. Bei „Getränkevorrat …“ „An die nächste Schicht weitergeben …“ mit Notiz → die Notiz steht
   an der Aufgabe. „Erledigt“ → sie verschwindet; „Erledigte zeigen“ → sie steht unter
   „ERLEDIGT“ mit „TS“.
6. Akte → Reiter „Berichte“ → „Aufgabe daraus …“: Der Bewohner ist schon gewählt.
7. „Offene Aufgaben drucken …“ → Druckvorschau.
8. „Demo-Daten zurücksetzen …“ → wieder 5 offene Aufgaben.

Version 0.6.0:

1. Unten links „Version 0.6.0 · Build …“. Links stehen jetzt auch „Aufgaben“, „Nachrichten“
   und „Ausgangsliste“ mit „Kommt in Version …“.
2. Einstellungen: Kürzel „TS“ eintragen.
3. Karten: Anton Ahorn zeigt „Übergabe 1“ und „Wichtig“, Berta Birke keine Übergabe-Marke.
4. Akte Anton Ahorn → Reiter „Berichte“: Berichte der letzten vier Wochen, neueste oben, mit
   Marken. „nur wichtige“ anhaken → nur noch violett markierte. Lebensbereich, Schicht und
   Suche ausprobieren.
5. „Neuer Bericht …“ in der Akte: Datum, Uhrzeit, Schicht und „TS“ sind vorbelegt. Ohne
   Lebensbereich und Text speichern → Fehlermeldung. Lebensbereich wählen, Text „Termin beim
   Arzt am Donnerstag“ → beim Speichern kommt „Soll das in die Übergabe?“.
6. Bei einem Bericht „Nachtrag …“ → Nachtrag steht darunter, der Text darüber bleibt gleich.
7. „Neuer Bericht …“ in der Seitenleiste: Bewohner wählen, „Übergabe“ anhaken, „3 Tage“ und
   einen kurzen Satz eingeben → danach öffnet sich die Akte beim Reiter „Berichte“.
8. Bereich „Übergabe“: oben „Wohnbereich allgemein“, darunter nach Bewohner. Beim ersten
   Besuch mit „TS“ ist alles „neu seit deiner letzten Schicht“, beim nächsten Besuch nicht
   mehr. „Gelesen“ → „gelesen: TS (…)“. „Verlängern …“ → neue Dauer. „Erledigt“ → Eintrag
   verschwindet; „Erledigte und abgelaufene zeigen“ → er steht unter „ERLEDIGT /
   ABGELAUFEN“.
9. „Notiz für den Wohnbereich …“ → erscheint oben unter „Wohnbereich allgemein“.
10. „Übergabeblatt drucken …“ und in der Akte „Drucken …“ → Druckvorschau; „Drucken …“ dort
    lässt z. B. „Microsoft Print to PDF“ wählen.
11. „Demo-Daten zurücksetzen …“ → Berichte und Übergabe sind wieder im Demo-Zustand.

Version 0.5.0:

1. Unten links „Version 0.5.0 · Build …“.
2. Bewohnerkarten zeigen Bild/Initialen, Name, „Zi. …“, darunter Marken: Anton Ahorn
   „Beschluss bis 30.06.2027“ und „Allergie: Haselnüsse“ (rot), Gustav Ginster „Allergien
   nicht erfasst“ und „Beschluss nicht erfasst“ (gelb), Berta Birke keine Marke. Unten auf
   jeder Karte „seit … · … Jahre, … Monate“.
3. Filterknöpfe über den Karten: Alle 10, Beschluss 5, Allergie 4, Nicht erfasst 4. Ein Klick
   zeigt nur die passenden Karten, oben steht „4 von 10 Bewohner“.
4. Unten die Statusleiste mit Farbschlüssel (Rot: Beschluss, Allergie; Gelb: nicht erfasst;
   Blaugrau: Zimmer).
5. Maus über eine Karte: Der Rand wird blauer.

Version 0.4.0:

1. Nach dem Update steht unten links „Version 0.4.0 · Build …“.
2. Links stehen: Übersicht, Bewohner, Neuer Bericht …, Übergabe, Kalender, Therapie,
   Medikamente, Einstellungen. Jeder noch nicht gebaute Bereich zeigt „Kommt in Version …“
   mit kurzer Beschreibung.
3. Akte öffnen: Unter Kopfkarte und Allergiehinweis stehen Reiter. „Stammdaten“ zeigt die
   bekannten Angaben, die anderen „Kommt in Version …“. Eine andere Akte beginnt wieder bei
   „Stammdaten“.
4. Einstellungen: Kürzel „A.B“ → Fehlermeldung; Kürzel „TS“ → gespeichert und beim nächsten
   Öffnen wieder da. Das Update-Häkchen steht jetzt hier (nicht mehr unten links).
5. Einstellungen → „Bild wählen …“: ein ausgedachtes Bild (kein echtes Logo der Einrichtung
   im Repository – nur bei dir im Datenordner) → erscheint rechts oben im Kopf, der
   Demo-Hinweis bleibt daneben sichtbar. „Bild entfernen“ → Logofeld wieder leer.

Version 0.3.0:

1. **Wechsel von 0.2.0:** Die vorhandene 0.2.0 starten. Erwartet: „Neue Version wird
   geladen …“, danach startet das Programm neu – jetzt mit neuem Aussehen. Unten links
   steht „Version 0.3.0 · Build …“. In `%APPDATA%\Dokumentationssystem\update.log` stehen
   die Schritte. Klappt das nicht: neue `.exe` über den Link im README von Hand laden.
2. Oben rechts im Kopf steht „Demo — ausgedachte Daten“, auch im Fenstertitel.
3. Unter „Bewohner“ stehen 10 ausgedachte Bewohner als Karten mit Initialen (die Demo-Daten
   beginnen neu; Änderungen aus 0.2.0 sind nicht übernommen).
4. Akte öffnen und die drei Allergie-Anzeigen vergleichen: Anton Ahorn (rot,
   „Haselnüsse“), Berta Birke (grün, „keine bekannt“), Clara Clematis (gelb gestrichelt,
   „nicht erfasst – bitte klären“).
5. Beschlussende vergleichen: Ahorn (Datum), Birke („kein Beschluss“), Ginster („nicht
   erfasst“).
6. „Neuer Bewohner …“: Aufnahmedatum vor das Geburtsdatum setzen → Fehlermeldung, nichts
   gespeichert. Danach richtig ausfüllen → Akte öffnet sich, neue Karte in der Übersicht;
   Allergien stehen auf „nicht erfasst“.
7. „Bearbeiten …“: ein ausgedachtes Bild (keine echte Person) hinterlegen → erscheint in
   Akte und Karte.
8. „Demo-Daten zurücksetzen …“ → Rückfrage, danach wieder die 10 Demo-Bewohner.
9. Häkchen „Beim Start nach Updates suchen“ (ab 0.4.0 unter Einstellungen) entfernen, neu
   starten: In `update.log` steht „in den Einstellungen abgeschaltet“.
10. Windows-Anzeige auf 125 % oder 150 % stellen: Nichts darf abgeschnitten sein.
