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

## Bauplan ab 07.11.2026 (gilt vor der Reihenfolge in dieser Datei)

Entscheidung Tobi (07.11.2026): **Alle gesammelten Ideen werden umgesetzt**, damit die Demo
alle Bereiche zeigt. Gebaut wird in dieser Reihenfolge, jeder Eintrag als eigener Pull
Request mit eigener Version und Testliste in `STAND.md` (Schritte 4 und 5 am
07.11.2026 eingefügt, die übrigen rücken nach):

| Schritt | Eintrag | Version |
|---|---|---|
| 1 | Programmgerüst mit allen Bereichen und Einstellungen | 0.4.0 |
| 2 | Gestaltung Stufe 2 | 0.5.0 |
| 3 | Tägliche Dokumentation (mit Bereich „Übergabe“) | 0.6.0 |
| 4 | Wohnbereich-ToDo | 0.7.0 |
| 5 | Nachrichten | 0.8.0 |
| 6 | Medikamentendatenbank | 0.9.0 |
| 7 | Medikation: Grundverordnung, Pause, Stufenplan | 0.10.0 |
| 8 | Therapie-Bereich | 0.11.0 |
| 9 | Wochenplan | 0.12.0 |
| 10 | Wohnbereichskalender | 0.13.0 |
| 11 | Modul Ausgangsliste (mit Modulverwaltung) | 0.14.0 |
| 12 | Biografie | 0.15.0 |
| 13 | Monatsreflexion | 0.16.0 |
| 14 | Startseite als Übersicht (Dashboard) | 0.17.0 |
| 15 | HEB-Bogen aus Berichten | 0.18.0 |
| 16 | Bewohnerakte: Ergänzungen | 0.19.0 |
| 17 | Mehrplatzbetrieb (Demo-Stufe) | 0.20.0 |
| 18 | Oberfläche: volle Fläche, einklappbares Seitenmenü | 0.21.0 |
| 19 | Mitarbeiter, Anmeldung und Rechte | 0.22.0 |
| 20 | Bezugspflege und Einzeltrainer | 0.23.0 |
| 21 | Medikation als Tabelle, Medikamentendatenbank in den Einstellungen | 0.24.0 |
| 22 | Therapien: Zuordnung und Rechte | 0.25.0 |
| 23 | Tagesplan | 0.26.0 |
| 24 | Kalender: Bereich und Haus, Fahrzeuge | 0.27.0 |
| 25 | Aufgaben nach Schichten mit Standardaufgaben | 0.28.0 |
| 26 | Visiten | 0.29.0 |
| 26a | Kürzel automatisch aus der Anmeldung | 0.29.1 |
| 26a1 | Umbenennung in DokuLine, Teil 1 (Übergang) | 0.29.2 |
| 26a2 | Position „Administrator“ mit Kürzel TST | 0.29.3 |
| 26a3 | Abmelden sichtbar machen, Tastenkürzel F12 | 0.29.3 |
| 26b | Ärzteverzeichnis | 0.29.4 |
| 26c | Visite: Übersicht aller Bewohner | 0.29.5 |
| 26d | Kleine Verbesserungen: Nachrichten „gelesen“, Datum mit Punkten, Kopf der Akte | 0.29.6 |
| 26e | Wohnbereiche R1–R3, Zimmer und Betten, Aufnahme | 0.29.7 |
| 26f | Fenster „Ärztliche Informationen“ | 0.29.8 |
| 26g | Tagesplan als Anzeigetafel | 0.29.9 |
| 26h | Demo-Daten: 23 Bewohner je Wohnbereich | 0.29.10 |
| 26i | Kopfzeile nur „Wo bin ich“, Logo, Abmelden; Aktionen darunter | 0.29.11 |
| 26j | Bereich „Bewohner“ als Liste mit Berichten | 0.29.12 |
| 26k | Visite: „Nächster Bewohner“ unter der Texteingabe | 0.29.13 |
| 26l | Übersicht neu: „Heute wichtig“ und Bewohnerliste | 0.29.14 |
| 26m | Therapie: Ansicht „Laufende Therapien“ als Anwesenheitstafel | 0.29.15 |
| 26n | Programmsymbol „Hell wie die Kopfzeile“ | 0.29.16 |
| 27 | Ausgangsliste als übersichtliche Liste | 0.30.0 |
| 28 | E-Mail und Kalender über externe Programme | 0.31.0 |
| 29 | Einstellungen neu gegliedert | 0.32.0 |
| 29a | Personenverzeichnis (Ärzte, Betreuer, Angehörige, Forensik, FOSA) | 0.33.3 |
| 29b | Bereich „Dokumentation“: alle Berichte an einem Ort, kompakt, mit Filter | 0.33.4 |
| 29c | Rechte: Fehlendes ausblenden statt ausgrauen | 0.33.5 |
| 29d | Wohnbereich einmal in der Übersicht wählen, gilt überall | 0.33.6 |
| 29q | Aufgaben gehören zu einem Bewohner oder einem Bereich | 0.33.6 |
| 29r | Übersicht: Überschriften der Kästen eingerahmt | 0.33.6 |
| 29s | Aufräumen nach den Code-Regeln (ohne neue Funktionen) | 0.33.7 |
| 29e | Nachrichten: Kuvert in der Kopfzeile statt Hinweis in jedem Fenster | 0.33.7 |
| 29f | Kleine Korrekturen (Demo-Übergaben, Angebot, Dokumentation) | 0.33.8 |
| 29g | Tagesplan im normalen Programmstil, kleiner und übersichtlicher | 0.33.9 |
| 29h | Übergabe nach Schichten, kompakt und schneller zu bearbeiten | 0.33.10 |
| 29i | Wochenplan ohne Datum | 0.33.11 |
| 29j | Ausgangsliste: alle Bewohner auf einen Blick, ohne Unterbringung | 0.33.12 |
| 29k | Visite: Psychiater und Hausarzt getrennt, Berichte in der Dokumentation | 0.33.13 |
| 29l | Medikamentenänderung auffällig markieren und in die Übergabe | 0.33.14 |
| 29m | Schnellerer Wechsel zwischen den Bereichen | 0.33.15 |
| 29n | Therapie als eigener Bereich (Dokumentation, Nachrichten, Aufgaben, Wochenpläne) | 0.33.16 |
| 29o | Therapie-Anzeige für den Fernseher im Wohnbereich | 0.33.17 |
| 29p | Nachschlagen beim BfArM absichern | 0.33.18 |
| 30 | Akte: Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger | 0.34.0 |
| 30a | Zuständigkeit Forensik und FOSA, Bestellweg der Medikamente | 0.34.1 |
| 31 | Akte: Reiter „Dokumente“ – digitale Akte in DokuLine | 0.35.0 |
| 32 | Vorhandene digitale Akte übernehmen | 0.36.0 |
| 33 | Überleitungsbogen auf Knopfdruck | 0.37.0 |
| 34 | Abgängigkeitsmeldung | 0.38.0 |
| 35 | Schweigepflichtentbindungen | 0.39.0 |
| 36 | Vitalwerte | 0.40.0 |
| 37 | Betriebsmodus für den Terminalserver | 0.41.0 |

Für alle Schritte gilt:
- Endet eine Sitzung mitten im Plan, beginnt die nächste beim ersten Schritt, der noch nicht
  `erledigt` ist.
- **Jeder Bereich bekommt ausgedachte Demo-Daten**, damit er sich vorführen lässt. Datumsangaben
  in Demo-Daten **relativ zum heutigen Tag** erzeugen (z. B. „vor 3 Tagen“, „in 2 Wochen“),
  damit die Demo an jedem Tag etwas zeigt. „Demo-Daten zurücksetzen“ setzt alle Bereiche zurück.
- Demo-Texte (Berichte, Biografien) sind erkennbar ausgedacht, kurz und wertneutral nach
  `docs/heb-wissen.md`; keine Diagnosen.
- **Kürzel nie von Hand** (Tobi, 08.11.2026): Wer etwas einträgt, ist über die Anmeldung
  bekannt. „Eingetragen von“ setzt das Programm immer selbst (siehe Schritt 26a).
- Steht in einem Eintrag „Festlegungen für die Demo“, sind das Annahmen. Weicht etwas davon
  ab, in `STAND.md` unter „Entscheidungen“ festhalten.
- Schritte 16 und 17 am 08.11.2026 ergänzt (Tobi: „Alles freigeben“). Schritt 17 ist die
  Demo-Stufe des Mehrplatzbetriebs, kein Echtbetrieb.

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
Status: erledigt (05.11.2026)
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
- Warnung vor Beschlussende: gebaut über die Startseite (Marke „Beschluss endet bald“).
  „offen geführt“ und Alter: siehe Eintrag „Bewohnerakte: Ergänzungen“.
- Ausdrücklich nicht: Tagesdokumentation, Medikation, Ausgang (eigene Einträge).

## Umstieg auf C# / .NET 10
Status: erledigt (06.11.2026)
Datum: 06.11.2026
Was: Das Programm wird in C# mit .NET 10 und Windows Forms neu aufgesetzt. Alles, was die
Python-Version 0.2.0 kann, funktioniert danach genauso in C#. Die Python-Version wird
entfernt (sie bleibt in der Git-Geschichte).
Warum: Entscheidung Tobi (06.11.2026): die Sprache mit den meisten und besten Möglichkeiten.
C# erlaubt, die Ausgangsliste (C#, Windows Forms) später als Bereich zu übernehmen, und
einen späteren Server in derselben Sprache; die Bausteine stehen unter MIT-Lizenz.
Umfang:
- Technik, Aufbau und Bereinigungsregeln wie in `CLAUDE.md`, Abschnitt „Technik“.
- **Gleicher Funktionsumfang wie 0.2.0:** Hauptfenster mit Demo-Hinweis, Navigation,
  Version und Build; Datenordner; Update-Häkchen und `--kein-update`; `update.log`;
  Bewohnerakte mit Übersicht, Akte, Formular, Prüfregeln, drei Zuständen bei Allergien und
  Beschlussende, Bild per Dateiauswahl, „Demo-Daten zurücksetzen“. Die Entscheidungen in
  `STAND.md` (Vor-/Nachname, Pflichtfelder, Datumseingabe usw.) gelten weiter.
- Demo-Daten: `daten/demo/bewohner.json` weiterverwenden.
- Aussehen gleich nach Eintrag „Gestaltung nach Vorbild der Ausgangsliste“ (freigegeben,
  unten): Bewohner-Übersicht als Kartenraster wie in der Ausgangsliste.
- **Übergang ohne Neuinstallation:** Die installierte Python-Version soll sich über ihr
  Update selbst durch die C#-Version ersetzen. Dafür Release-Namen (`build-<Laufnummer>`),
  Dateinamen (`Dokumentationssystem.exe`, `.sha256`) und steigende Laufnummer beibehalten.
  Vorher am Code des Python-Updaters prüfen, ob das so klappt; falls nicht, im README und
  in `STAND.md` erklären, dass die neue `.exe` einmal von Hand geladen werden muss.
- Daten der Python-Version müssen nicht übernommen werden (nur Demo-Daten). Liegt im
  Datenordner eine alte Datenbank, wird sie nicht gelöscht, sondern ignoriert.
- Neue Version: **0.3.0**.
- Ausdrücklich nicht: neue Funktionen über 0.2.0 hinaus (außer dem Aussehen).

## Tägliche Dokumentation
Status: erledigt (07.11.2026; Bauplan Schritt 3, Version 0.6.0)
Datum: 05.11.2026
Was: Tägliche schriftliche Dokumentation je Bewohner: der Teil, in dem Berichte über
Bewohner geschrieben werden. **Wichtigster Bereich des Programms** (Tobi, 07.11.2026).
Warum: Laut Tobi Hauptteil des Programms neben der Bewohnerführung. In den Berichten gibt es
immer wichtige und übergaberelevante Inhalte; sie dürfen nicht in der Menge untergehen.
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- Ein Bericht hat: Bewohner, Datum und Uhrzeit, Schicht (Früh, Spät, Nacht), Verfasser
  (Kürzel; eigenes Kürzel in den Einstellungen), **Lebensbereich(e)** 1–5 aus
  `docs/heb-wissen.md` (Mehrfachauswahl, mindestens einer oder „allgemein“), Text.
- **Zwei getrennte Kennzeichen** je Bericht, beide beim Schreiben gut sichtbar (nicht
  versteckt in einem Menü), Farbe nie allein:
  - **„Wichtig“** — bleibt in der Akte dauerhaft hervorgehoben (eigener Filter „nur
    wichtige“, Marke in der Liste). Für Inhalte, die man über die Schicht hinaus wissen muss.
  - **„Übergabe“** — erscheint in der Übergabe-Ansicht der folgenden Schicht. Für Inhalte,
    die die nächste Schicht wissen oder tun muss.
  - Ein Bericht kann beides haben, eines oder keines.
- **Hinweis vor dem Speichern:** Kommen im Text Wörter vor, die oft übergaberelevant sind
  (z. B. „Arzt“, „Termin“, „Krise“, „Sturz“, „verweigert“, „abgängig“), und ist „Übergabe“
  nicht gesetzt, fragt das Programm einmal nach: „Soll das in die Übergabe?“ — nur ein
  Hinweis, kein Zwang. Die Wortliste steht in einer Datei und ist erweiterbar.
- **Kenntnisnahme:** In der Übergabe-Ansicht kann jeder Übergabe-Eintrag mit dem eigenen
  Kürzel als „gelesen“ bestätigt werden; sichtbar, wer ihn schon gelesen hat.
- Eingabe aus der Akte (Reiter „Berichte“) und über „Neuer Bericht …“ in der Seitenleiste
  (mit Bewohnerauswahl).
- Liste je Bewohner, neueste oben; Filter nach Zeitraum, Lebensbereich und Schicht; Suche im
  Text.
- **Bereich „Übergabe“** (eigener Bereich in der Seitenleiste, Wunsch Tobi 07.11.2026): Hier
  steht **nur** die Übergabe, keine übrigen Berichte. So bleibt es auf das Wichtige
  eingegrenzt.
  - **Übergabe-Einträge bleiben stehen, bis sie nicht mehr gebraucht werden** — nicht nur
    eine Schicht lang. Beim Setzen von „Übergabe“ wählt man, wie lange: nächste Schicht
    (Vorgabe), 3 Tage, 1 Woche, bis Datum … oder „bis erledigt“. So geht nichts unter, was
    über mehrere Tage weitergegeben werden muss.
  - Jeder Eintrag lässt sich **verlängern** oder als **erledigt** markieren (mit Kürzel und
    Zeit). Erledigte und abgelaufene Einträge verschwinden aus der Liste, bleiben aber unter
    „Erledigt / abgelaufen“ einsehbar. Gelöscht wird nichts.
  - Anzeige nach Bewohner gruppiert; oben ein Abschnitt „Wohnbereich allgemein“ für
    Übergaben, die keinen einzelnen Bewohner betreffen (eigene kurze Übergabe-Notiz, ohne
    Bericht). Marke „läuft heute ab“ und „neu seit deiner letzten Schicht“.
  - Der Übergabe-Text ist der Bericht selbst; ein kurzer eigener Übergabe-Satz ist möglich
    (z. B. „Arzttermin Do 10 Uhr, Begleitung nötig“), der Bericht bleibt per Klick lesbar.
  - Ausdruck als Übergabeblatt (nur die offenen Einträge).
- **Auf den Karten** (Übersicht und Bewohner): Marke „Übergabe 2“ mit der Zahl der noch
  ungelesenen Übergabe-Einträge, Marke „Wichtig“ bei wichtigen Berichten der letzten 7 Tage.
- Bericht schreiben geht schnell: Fenster öffnet mit dem aktuellen Datum, der aktuellen
  Schicht (aus der Uhrzeit) und dem eigenen Kürzel vorbelegt.
- **Nichts wird gelöscht oder überschrieben:** Korrekturen sind Nachträge mit eigenem
  Zeitstempel, der ursprüngliche Text bleibt sichtbar.
- Ausdruck: Berichte eines Bewohners für einen Zeitraum (Druckvorschau).
- Demo: für jeden Bewohner Berichte der letzten vier Wochen, über alle Lebensbereiche verteilt.
- Ausdrücklich nicht: Pflegeplanung, Vitalwerte.

## Wochenplan
Status: erledigt (07.11.2026; Bauplan Schritt 9, Version 0.12.0)
Datum: 06.11.2026
Was: Eine Anzeige, in der man für jeden Bewohner einzeln seinen Wochenplan sieht. Der Plan
lässt sich jederzeit mit Therapien bearbeiten.
Warum: Der Wochenplan gibt dem Bewohner Struktur und ist eine Grundlage für den HEB-Bogen.
Im bisherigen System gibt es keinen übersichtlichen Wochenplan für den Bewohner.
Umfang (Vorschlag aus dem Chat, freigegeben 07.11.2026):
- Ansicht je Bewohner: Montag bis Sonntag als Spalten, Uhrzeiten als Zeilen; jeder Termin
  mit Therapie, Uhrzeit von–bis, Ort und Hinweis. Erreichbar aus der Bewohnerakte.
- Therapien kommen aus dem Bereich „Therapie“ (eigener Eintrag unten): Dort zugeteilte
  Therapien erscheinen automatisch im Wochenplan. Daneben freie Einträge (z. B. Termine im
  Wohnbereich).
- Termine anlegen, verschieben, ändern, löschen — jederzeit.
- **Ausdruck** (Entscheidung Tobi, 06.11.2026: viele Bewohner brauchen ihn): Wochenplan als
  A4-Seite, gut lesbar (große Schrift), nur Name und Plan — keine Allergien, Medikation oder
  andere Angaben aus der Akte. Druckvorschau vor dem Drucken; Speichern als PDF über den
  Windows-Druckdialog („Microsoft Print to PDF“).
- **Verlauf bleibt erhalten:** Jede Änderung gilt „ab Datum …“; frühere Fassungen werden
  nicht überschrieben. Grund: Der HEB-Bogen muss zeigen können, welcher Plan in seinem
  Zeitraum galt.
- Ausdrücklich nicht: Dienstplan der Mitarbeiter, Raumbelegung, Abgleich mit Kalendern.
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- **Regelwoche:** Der Plan gilt jede Woche gleich, bis er „ab Datum …“ geändert wird.
  Einzelne Termine aus dem Kalender (Arzttermin, Heimfahrt, Ausflug) erscheinen in der
  jeweiligen Woche zusätzlich, farblich abgesetzt. Blättern nach Kalenderwochen.
- **Teilnahme:** Bei Therapie-Terminen lässt sich für einen Tag „teilgenommen“, „abgesagt“
  oder „ausgefallen“ eintragen (ohne Eintrag: offen). Der HEB-Bogen zeigt die Zahlen je
  Angebot im Zeitraum.

## HEB-Bogen aus Berichten
Status: erledigt (07.11.2026)
Datum: 06.11.2026
Was: Für einen Bewohner einen HEB-Bogen (Hilfe- und Entwicklungsbogen) für einen Zeitraum
„von … bis …“ erstellen. Das Programm nutzt dafür die eingetragenen Berichte dieses
Zeitraums.
Warum: Die Bewohner des beschützenden Wohnbereichs haben meist für ein oder mehrere Jahre
einen Beschluss. Für ihre Entwicklung ist ein HEB-Bogen nötig; die Inhalte stehen schon in
den Berichten und sollen nicht noch einmal zusammengesucht werden.
Fachgrundlage: `docs/heb-wissen.md` (Bogenarten, Lebensbereiche, Felder, Zeichengrenzen,
Formulierungsregeln).
Umfang (Entscheidungen Tobi, 06.11.2026, Rest Vorschlag aus dem Chat):
- **Keine KI und keine Internetverbindung** (Datenschutz, `CLAUDE.md`, Regel 6). Das Programm
  formuliert keine Texte. Es stellt zusammen, zählt, prüft und bietet Textbausteine an;
  schreiben tut ein Mensch.
- **Vorlage ist der PDF-Bogen des Bezirks Oberbayern.** In den Einstellungen wird je
  Bogenart (A, B, C) eine PDF-Datei zugeteilt. Die Datei bleibt im Datenordner bzw. dort,
  wo sie liegt, und kommt nie ins Repository. Das Programm liest die Formularfelder der
  PDF aus und zeigt sie an; eine Zuordnung „Programmfeld → PDF-Feld“ wird in den
  Einstellungen gespeichert.
- Ablauf: Bewohner wählen → Bogenart wählen → Zeitraum „von … bis …“ (Vorschlag nach
  `docs/heb-wissen.md`, z. B. HEB B: Datum des letzten Bogens bis heute) → Eingabemaske je
  Lebensbereich mit den Feldern der Bogenart.
- Neben jedem Feld: die Berichte des Zeitraums zu diesem Lebensbereich (Quelle 1) und der
  Wochenplan, der im Zeitraum galt (Quelle 2, siehe „Wochenplan“). Fehlt etwas: sichtbar
  „keine Berichte zu diesem Bereich im Zeitraum“.
- **Zeichenzähler** je Feld mit der Grenze aus `docs/heb-wissen.md`, z. B. „912 / 940“; über
  der Grenze rot.
- **Kopfdaten** aus der Bewohnerakte (Name, Aufnahmedatum, Beschlussende). HEB B: Rahmenziele,
  Leitziel und Nahziele des letzten Bogens im Programm werden zum Fortschreiben vorgelegt.
  Planungszeitraum (6/12/24/frei) wird aus Beschlussende berechnet und vorgeschlagen.
- Hilfebedarf-Stufe: wird **nie** vorgeschlagen oder vorbelegt.
- **Textbausteine** aus `docs/heb-wissen.md` (Verben für Rahmenziele und Maßnahmen,
  feststehende Formulierungen, Häufigkeitsmarker) zum Einfügen per Klick.
- **Stilprüfung** als Hinweisliste (nie automatisch ändern): die Prüfregeln aus
  `docs/heb-wissen.md`, Abschnitt „Regeln, die das Programm prüfen kann“.
- Speichern als Entwurf; „fertig“ setzt ein Mensch. Danach **PDF ausfüllen**: Programm
  schreibt die Texte in die Formularfelder einer Kopie der zugeteilten PDF und speichert sie
  im Datenordner. Die Vorlage selbst bleibt unverändert.
- Demo: Tests laufen mit einer selbst gebauten Test-PDF mit Formularfeldern (im
  Repository erlaubt, weil ausgedacht), nicht mit dem Bogen des Bezirks.
- Neue Abhängigkeit nur zum PDF-Ausfüllen: `PDFsharp` (MIT) — Begründung in `STAND.md`.
  Ob es die Formularfelder des Bezirks-Bogens sauber füllt, ist ungeprüft; zuerst mit der
  Test-PDF erproben.
- Ausdrücklich nicht: Versand an den Kostenträger, Unterschrift.
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- Die Feldnamen des echten Bogens sind unbekannt. Gebaut wird gegen die selbst gebaute
  Test-PDF; die Zuordnung „Programmfeld → PDF-Feld“ in den Einstellungen macht den echten
  Bogen später ohne Programmänderung nutzbar. Ohne zugeteilte PDF bleibt die Eingabemaske
  nutzbar (Entwurf speichern, Ausdruck der eigenen Maske).
- Dritte Quelle neben jedem Feld: die Monatsreflexionen des Zeitraums (siehe
  „Monatsreflexion“), falls vorhanden.
- Felder außerhalb der Lebensbereiche: je ein freies Textfeld „Sichtweise des
  Leistungsberechtigten“ und „Ergänzende Ausführungen“, bei HEB C zusätzlich „Erläuterung zur
  Beendigung“ — ohne Zeichengrenze, bis der echte Bogen vorliegt.

## Medikation: Grundverordnung, Pause, Stufenplan
Status: erledigt (07.11.2026; Bauplan Schritt 7, Version 0.10.0)
Datum: 06.11.2026
Was: Medikation je Bewohner. Die Grundverordnung bleibt immer erhalten; Pausen und
Änderungen in Etappen (ansetzen, absetzen) liegen als eigene Einträge darüber.
Warum: Im bisherigen System lassen sich Medikamente nicht pausieren, und eine Anpassung
über mehrere Tage geht nur, indem das Medikament mehrfach angelegt wird.
Umfang (Vorschlag aus dem Chat, freigegeben 07.11.2026):
- **Grundverordnung:** Medikament (aus der Medikamentendatenbank), Dosis je Einnahmezeit
  (morgens, mittags, abends, nachts), Beginn, Anordnung (Arzt und Datum, ausgedacht),
  Hinweis. Bedarfsmedikation getrennt davon.
- Die Grundverordnung wird nie überschrieben oder gelöscht. Jede Änderung ist ein eigener
  Eintrag mit Datum und Anordnung; der Verlauf bleibt sichtbar.
- **Pause:** von … bis … oder „bis auf Weiteres“. Danach gilt wieder die Grundverordnung,
  ohne dass jemand etwas neu anlegen muss.
- **Stufenplan** (ansetzen oder absetzen in Etappen): Liste von Stufen „ab Datum X:
  Dosis Y“. Anzeige z. B. „Stufe 2 von 4, nächste Stufe ab …“.
- **Ansicht „Was gilt heute“** (oder an einem gewählten Tag): berechnet aus Grundverordnung,
  Pausen und Stufenplänen. Pausierte Medikamente stehen sichtbar als „pausiert bis …“ da,
  nicht einfach nicht.
- Ausdrücklich nicht: Dosisvorschläge, Prüfung auf Wechselwirkungen, Stellen und Ausgabe
  abzeichnen, Bestand und Ablaufdaten.
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- Ende eines Stufenplans beim **Ansetzen oder Umstellen:** Die letzte Stufe wird zur neuen
  Grundverordnung; die vorige bleibt im Verlauf als „ersetzt am …“.
- Ende beim **Absetzen auf null:** Die Grundverordnung bleibt im Verlauf als „abgesetzt am …“
  stehen und erscheint nicht mehr unter „Was gilt heute“.
- Ausdruck: „Was gilt heute“ als Übersicht je Bewohner (Druckvorschau), mit dem Vermerk
  „Demo — nicht zum Stellen verwenden“.

## Medikamentendatenbank
Status: erledigt (07.11.2026; Bauplan Schritt 6, Version 0.9.0)
Datum: 06.11.2026
Was: Eine vorgefertigte Liste von Medikamenten, aus der beim Verordnen ausgewählt wird,
statt den Namen frei einzutippen.
Warum: Weniger Tippfehler, einheitliche Schreibweise, Stärken und Darreichungsformen
stehen schon bereit.
Umfang (Vorschlag aus dem Chat):
- Felder: Wirkstoff, Handelsname(n), Stärke(n), Darreichungsform, Quelle (Adresse der
  Fach- oder Gebrauchsinformation).
- Demo: kleine, selbst zusammengestellte Liste gängiger Medikamente. Jeder Eintrag hat
  `pruefstatus: ungeprueft` und eine Quelle; nur Tobi setzt „bestätigt“. In der Oberfläche
  sichtbar: „Demo-Bestand, nicht zum Verordnen verwenden“.
- Fehlt ein Medikament: von Hand anlegen, gekennzeichnet als „selbst angelegt“.
- Echte Wirkstoff- und Handelsnamen sind hier erlaubt; das sind keine Bewohnerdaten
  (`CLAUDE.md`, Regel 1).
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- Etwa 30 gängige Wirkstoffe (Psychiatrie und Allgemeinmedizin). Stärken und
  Darreichungsformen **nur mit Quelle**; ist keine Quelle gefunden, bleibt das Feld leer
  statt geschätzt.
- Eigener Bereich „Medikamente“ in der Navigation: Liste mit Suche, Eintrag ansehen,
  anlegen, ändern.
Offen: Eine vollständige echte Datenbank (z. B. vom BfArM oder einem kommerziellen
Anbieter) erst klären, wenn das Programm über eine Demo hinausgeht — Nutzungsrechte sind
nicht geprüft.

## Startseite als Übersicht (Dashboard)
Status: erledigt (07.11.2026)
Datum: 06.11.2026
Was: Die Hauptseite zeigt alle Bewohner des Wohnbereichs auf einen Blick und ist immer
aktuell.
Warum: Wer den Dienst beginnt, soll sofort sehen, was heute wichtig ist, ohne jede Akte zu
öffnen.
Umfang (Vorschlag aus dem Chat, freigegeben 07.11.2026):
- Kartenraster wie in der Ausgangsliste (`docs/gestaltung.md`): je Bewohner Bild, Name,
  Zimmer und Marken.
- Marken (nur wenn zutreffend, Farbe nie allein): Allergie eingetragen, Angabe „nicht
  erfasst“, Beschluss endet bald, heute Änderung in der Medikation (Pause beginnt/endet,
  neue Stufe), heute Termine laut Wochenplan, HEB fällig.
- Bereich „Heute“ oben: Anzahl Bewohner, Termine des Tages, Medikationsänderungen des Tages.
- **Immer aktuell:** Die Ansicht baut sich nach jeder Änderung im Programm und beim
  Datumswechsel um Mitternacht neu auf.
- Klick auf eine Karte öffnet die Bewohnerakte.
- Bezieht Daten aus Einträgen, die noch nicht gebaut sind; zeigt nur, was es schon gibt.
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- „Beschluss endet bald“ ab **90 Tagen** vor Beschlussende, einstellbar in den Einstellungen.
- „HEB fällig“: wenn der letzte HEB-Bogen länger als die eingestellte Frist her ist
  (Vorgabe 12 Monate) oder bei neuen Bewohnern kein HEB A innerhalb von drei Monaten nach
  Aufnahme vorliegt.
- Die Startseite ist der erste Bereich in der Navigation und öffnet beim Programmstart.
- Abwesende (Heimfahrt, Ausgang) werden auf der Karte gekennzeichnet.

## Therapie-Bereich
Status: erledigt (07.11.2026; Bauplan Schritt 8, Version 0.11.0)
Datum: 06.11.2026
Was: Ein eigener Bereich in der Navigation, in dem die Therapie ihre Angebote verwaltet,
einträgt und Bewohnern zuteilt. Zugeteilte Therapien erscheinen in den Wochenplänen.
Warum: Die Therapie plant ihre Angebote selbst; der Wohnbereich soll das Ergebnis im
Wochenplan sehen, ohne es abzuschreiben.
Umfang (Vorschlag aus dem Chat, freigegeben 07.11.2026):
- Therapieangebote anlegen: Name, Art (Einzel/Gruppe), Therapeut (ausgedacht), Ort,
  Wochentag(e) und Uhrzeit, höchste Teilnehmerzahl, Hinweis. In der Demo ausgedachte,
  allgemeine Angebote.
- Bewohner einem Angebot zuteilen (ab Datum, optional bis Datum) → erscheint im Wochenplan
  des Bewohners. Änderungen gelten „ab Datum …“, der Verlauf bleibt.
- Warnung bei Überschneidung mit einem anderen Termin des Bewohners.
- Übersicht je Angebot: wer nimmt teil, freie Plätze.
- Ausdrücklich nicht in der Demo: Anmeldung und Rechte (wer darf was) — das braucht den
  Mehrplatzbetrieb (Eintrag unten).

## Mehrplatzbetrieb über einen Server (später)
Status: freigegeben nur als Demo-Stufe — siehe Eintrag „Mehrplatzbetrieb (Demo-Stufe)“
Datum: 06.11.2026
Was: Das Programm arbeitet über einen Server auf allen Bereichen (Wohnbereiche, Therapie).
Dann lässt sich zu jedem Bewohner eine Statusmeldung machen, die alle Bereiche sehen.
Warum: Laut Tobi der Weg, wie das Programm im Betrieb funktionieren würde.
Umfang: Der **Echtbetrieb** bleibt außerhalb der Demo. Voraussetzungen dafür: Freigabe durch
Einrichtung und Datenschutz, Beteiligung der Mitarbeitervertretung, Anmeldung und Rechte,
Server der Einrichtung. Was davon schon vorführbar ist, steht im Eintrag „Mehrplatzbetrieb
(Demo-Stufe)“.

## Gestaltung nach Vorbild der Ausgangsliste
Status: erledigt (06.11.2026; gebaut zusammen mit „Umstieg auf C# / .NET 10“)
Datum: 06.11.2026
Was: Das Programm übernimmt das Aussehen der Ausgangsliste und verfeinert es mit einzelnen
Regeln aus Tobis Designvorlagen.
Warum: Wunsch Tobi (06.11.2026); beide Programme sollen zusammengehören.
Umfang: alle Werte und Regeln in `docs/gestaltung.md`. Umsetzung über eine zentrale
Stil-Klasse nach Vorbild `Stil.cs` der Ausgangsliste (ein Wert, ein Ort), ergänzt um die
Verfeinerungen aus `docs/gestaltung.md`. Zunächst nur die helle Darstellung.
Bestehende Ansichten (Bewohner-Übersicht, Akte, Formular) werden angepasst.
Ausdrücklich nicht: Logo oder Name der Einrichtung im Repository.

## Wohnbereichskalender
Status: erledigt (07.11.2026; Bauplan Schritt 10, Version 0.13.0)
Datum: 06.11.2026
Was: Ein Kalender für den ganzen Wohnbereich. Er zeigt Termine, die Bewohner betreffen, und
dient zum Planen von Heimfahrten und Tagesausflügen.
Warum: Wunsch Tobi (06.11.2026). Termine einzelner Bewohner und gemeinsame Unternehmungen
stehen an einer Stelle, statt verstreut.
Umfang (Vorschlag aus dem Chat, freigegeben 07.11.2026):
- Ansichten Monat, Woche, Tag; Filter „alle“ oder ein Bewohner.
- Terminarten: Termin eines Bewohners (z. B. Arzttermin), Termin des Wohnbereichs,
  **Heimfahrt** (von Datum/Uhrzeit bis Datum/Uhrzeit, auch über mehrere Tage),
  **Tagesausflug** (Ziel, Treffpunkt, Uhrzeit, teilnehmende Bewohner, Begleitung).
- Ein Termin kann mehreren Bewohnern zugeordnet sein; er erscheint dann bei jedem von ihnen.
- Termine eines Bewohners erscheinen auch in seinem Wochenplan und auf der Startseite
  („heute“). Während einer Heimfahrt zeigt die Startseite „abwesend: Heimfahrt bis …“.
- Ausdruck: Monats- oder Wochenansicht des Kalenders; Teilnehmerliste eines Ausflugs.
- Ausdrücklich nicht: Abgleich mit Outlook oder anderen Kalendern (kein Internet,
  `CLAUDE.md`, Regel 6).
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- Heimfahrt mit geplanter Rückkehr (Datum, Uhrzeit) und Hinweisfeld. Mitgegebene
  Medikation wird **nicht** berechnet; das Hinweisfeld genügt für die Demo.

## Programmgerüst mit allen Bereichen und Einstellungen
Status: erledigt (07.11.2026; Bauplan Schritt 1, Version 0.4.0)
Datum: 07.11.2026
Was: Die Navigation bekommt alle Bereiche, die Bewohnerakte Reiter für ihre Teile, und es
gibt ein Einstellungsfenster. Noch nicht gebaute Bereiche zeigen einen freundlichen Hinweis
„kommt in Version …“ statt leer zu sein.
Warum: Tobi sieht in der Demo bisher nur das Hauptfenster (07.11.2026). Das Gerüst macht
sichtbar, wohin das Programm wächst, und gibt jedem späteren Schritt seinen festen Platz.
Umfang:
- Seitenleiste: Übersicht, Bewohner, Neuer Bericht …, Übergabe, Kalender, Therapie,
  Medikamente, Einstellungen.
- Bewohnerakte mit Reitern: Stammdaten, Berichte, Medikation, Wochenplan, Biografie,
  Reflexion, HEB.
- Einstellungsfenster: eigenes Kürzel; Update-Häkchen (dorthin verschoben, unten links
  bleibt nur die Versionsanzeige); Logo-Bild wählen (Ablage im Datenordner, erscheint im
  Kopf rechts); Platz für spätere Einstellungen (HEB-Vorlagen, Fristen).
- Einstellungen in `einstellungen.json` bzw. Datenbank, wie bisher entschieden.

## Gestaltung Stufe 2
Status: erledigt (07.11.2026; Bauplan Schritt 2, Version 0.5.0)
Datum: 07.11.2026
Was: Das Aussehen rückt näher an die Ausgangsliste: reichere Karten, Marken, Logofeld,
Farbschlüssel.
Warum: Tobi findet, die Darstellung wirkt noch nicht überarbeitet (07.11.2026).
Umfang (Vorbild `KhaosDE/Ausgangsliste`: `BewohnerKarte.cs`, `KopfBereich.cs`,
`KartenRaster.cs`; Werte in `docs/gestaltung.md`):
- Karte zeichnet sich selbst wie in der Ausgangsliste: Bild/Initialen, Name, Zimmer-Marke,
  darunter Marken (z. B. Beschluss mit Enddatum, Allergie, „nicht erfasst“), Aufenthaltsdauer
  seit Aufnahme, weicher Schatten, Hover-Rand.
- Kopf: Logofeld rechts (Bild aus den Einstellungen, sonst leer).
- Statusleiste unten mit Farbschlüssel der Marken (Farbe nie allein).
- Filterknöpfe über dem Raster mit Zählern (z. B. „Alle 10“, „Beschluss 6“, „Allergie 3“).
- Bereinigungsregeln aus `CLAUDE.md` gelten.

## Modul Ausgangsliste (mit Modulverwaltung)
Status: erledigt (07.11.2026)
Datum: 07.11.2026 (ersetzt den Eintrag „Ausgang (aus der Ausgangsliste)“)
Was: Die Ausgangsliste wird ein **zuschaltbares Modul** des Dokumentationssystems. Dazu
bekommt das Programm eine Modulverwaltung.
Warum: Entscheidung Tobi (07.11.2026): Nicht jeder Bereich braucht die Ausgangsliste. Das
Dokumentationssystem ist das **Muttersystem**; die Ausgangsliste bleibt als eigenständige
Version bestehen und bekommt die Funktionen über einen Abgleich. Rollen, Regeln für das Modul
und Abgleich: `docs/bruecke-ausgangsliste.md` (verbindlich für diesen Schritt).
Umfang:
- **Modulverwaltung:** In den Einstellungen ein Abschnitt „Module“ mit Häkchen je Modul. Ein
  ausgeschaltetes Modul hinterlässt keine Spuren: kein Eintrag in der Seitenleiste, kein
  Reiter in der Akte, keine Marken auf den Karten, nichts auf der Startseite. Seine Daten
  bleiben in der Datenbank und sind nach dem Wiedereinschalten wieder da. Gebaut als
  allgemeine Schnittstelle (ein Modul meldet Seitenleisten-Eintrag, Akte-Reiter, Marken,
  Demo-Daten, Einstellungen an), damit später weitere Bereiche zuschaltbar werden können.
- **Modul „Ausgangsliste“** übernimmt die Funktionen der Ausgangsliste nach der
  Zuordnungstabelle in `docs/bruecke-ausgangsliste.md`: Ausgangszeiten (allein / mit
  Bewohner), Anwesenheit und Abwesenheit mit Grund und Frist, Unterbringung mit Fristen und
  der Rechtsregel „Ausgangsbeschränkung nur mit Beschluss oder Forensik“, druckbare
  Tagesliste, Verlauf je Person; als Untermodule (eigene Häkchen) Küchendienst und Digitaler
  Ausgang mit Nachtbericht.
- **Eine Bewohnerliste, kein Doppelpflegen:** Bewohner, Zimmer, Aufnahme, Entlassung und
  Zimmerwechsel gehören zum Kern (Bewohnerakte). Das Modul liest und ergänzt sie, legt keine
  eigene Bewohnerliste an.
- Übernehmen und anpassen aus `KhaosDE/Ausgangsliste` (Repository anhängen), Bereinigung
  nach `CLAUDE.md`. Dateien und Klassen im Modul möglichst gleich benennen wie in der
  Ausgangsliste, damit spätere Abgleiche leicht sind.
- **Eigenes Projekt** `src/DokuLine.Modul.Ausgangsliste/`, abhängig nur von Schnittstellen
  des Kerns, mit `AENDERUNGEN.md` — so lässt es sich in die eigenständige Ausgangsliste
  übernehmen (Abschnitt „Damit der Abgleich leicht bleibt“ der Brückendatei).
- Nach dem Bau in `docs/bruecke-ausgangsliste.md` eintragen, bis zu welcher Version der
  Ausgangsliste übernommen wurde.
- Demo: Modul in den Demo-Daten eingeschaltet; Ausgänge und Abwesenheiten relativ zu heute.
- Ausdrücklich nicht: Update über den Ordner `Update` und Installer der Ausgangsliste — das
  Dokumentationssystem hat sein eigenes Update.

## Biografie
Status: erledigt (07.11.2026)
Datum: 07.11.2026
Was: Reiter „Biografie“ in der Bewohnerakte.
Warum: Ursprüngliche Idee (05.11.2026).
Umfang (Regeln aus Tobis Arbeitsweise für Biografieeinträge):
- Abschnitte: Herkunft und Familie, Schule und Beruf, Wohnen, Interessen und Gewohnheiten,
  wichtige Lebensereignisse, Wünsche — jeweils Fließtext, ohne Aufzählungszeichen.
- Je Angabe wählbar „vom Bewohner angegeben, nicht überprüft“; im Text erscheint dann die
  Formulierung „gibt an, dass …“ als Hinweis, nicht automatisch eingesetzt.
- Liste „fehlende Angaben“: leere Abschnitte werden am Ende aufgezählt statt überbrückt.
- „Als Fließtext kopieren“: alle Abschnitte als reiner Text ohne Formatierung in die
  Zwischenablage.
- Verlauf: Änderungen mit Datum und Kürzel, frühere Fassung bleibt einsehbar.

## Monatsreflexion
Status: erledigt (07.11.2026)
Datum: 07.11.2026
Was: Je Bewohner und Monat eine kurze Reflexion je Lebensbereich, geschrieben mit den
Berichten des Monats daneben.
Warum: Ursprüngliche Idee (05.11.2026); Zwischenschritt zwischen Berichten und HEB-Bogen.
Umfang:
- Reiter „Reflexion“ in der Akte: Monat wählen, fünf Textfelder (Lebensbereiche aus
  `docs/heb-wissen.md`), daneben die Berichte des Monats zu diesem Bereich und die
  Teilnahme an Therapien laut Wochenplan.
- Stilprüfung wie beim HEB-Bogen (nur Hinweise).
- Status Entwurf/fertig; der HEB-Bogen zeigt die fertigen Reflexionen seines Zeitraums als
  dritte Quelle.

## Wohnbereich-ToDo
Status: erledigt (07.11.2026; Bauplan Schritt 4, Version 0.7.0)
Datum: 07.11.2026
Was: Eine Aufgabenliste des Wohnbereichs auf dem Hauptbildschirm. Damit gibt man Aufgaben an
die nächste Schicht weiter.
Warum: Wunsch Tobi (07.11.2026). Abgrenzung zur Übergabe: **Übergabe = etwas wissen**,
**ToDo = etwas tun**. Beide bleiben stehen, bis sie erledigt sind.
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- Sichtbar auf dem Hauptbildschirm (Bereich „Übersicht“) als Liste neben bzw. über den
  Karten; zusätzlich als eigener Eintrag in der Seitenleiste mit Zähler offener Aufgaben.
- Eine Aufgabe hat: Text, optional Bewohner, **für wen** (nächste Schicht — Vorgabe —,
  bestimmte Schicht an einem Tag, oder „wer Zeit hat“), optional fällig bis (Datum, Uhrzeit),
  angelegt von (Kürzel, Zeit).
- **Bleibt stehen, bis erledigt** (mit Kürzel und Zeit). Überfällige Aufgaben rot mit Wort
  „überfällig“ (Farbe nie allein). Erledigte unter „Erledigt“ einsehbar, nichts wird gelöscht.
- Aufgabe weitergeben: „an die nächste Schicht weiterreichen“ mit kurzer Notiz, falls sie
  nicht geschafft wurde; die Weitergabe ist am Eintrag sichtbar.
- Aus einem Bericht heraus: „Aufgabe daraus machen“ übernimmt den Bewohner.
- Ausdruck der offenen Aufgaben.
- Demo: einige ausgedachte Aufgaben, relativ zum heutigen Tag, eine davon überfällig.
- Wiederkehrende Standardaufgaben: ab Bauplan Schritt 25 („Aufgaben nach Schichten mit Standardaufgaben“). Weiterhin nicht: Dienstpläne und Reinigungsdienste —
  die Arbeitszuweisung ist ein eigenes Werkzeug.

## Nachrichten
Status: erledigt (07.11.2026; Bauplan Schritt 5, Version 0.8.0)
Datum: 07.11.2026
Was: Ein Nachrichtensystem im Programm, in dem sich Kolleginnen und Kollegen gegenseitig
schreiben — an einzelne Personen oder an Teams.
Warum: Wunsch Tobi (07.11.2026).
Festlegungen für die Demo (Annahmen aus dem Chat, 07.11.2026 — Tobi kann sie ändern):
- **Benutzer und Teams in der Demo:** ausgedachte Kolleginnen und Kollegen mit Kürzel und
  Team (z. B. „Wohnbereich A“, „Wohnbereich B“, „Therapie“, „Leitung“). Oben rechts
  „Angemeldet als …“ mit **Benutzer wechseln ohne Passwort** — nur für die Vorführung, damit
  man beide Seiten einer Nachricht zeigen kann. Das bisherige „eigene Kürzel“ aus den
  Einstellungen geht in diese Auswahl über.
- Bereich „Nachrichten“ in der Seitenleiste mit Zähler ungelesener Nachrichten: Posteingang,
  Gesendet, Archiv. Neue Nachricht an eine oder mehrere Personen und/oder ein Team; Betreff,
  Text, optional Bezug zu einem Bewohner (dann aus der Akte heraus aufrufbar).
- Antworten im selben Verlauf; Team-Nachrichten sieht jedes Teammitglied.
- Ungelesene Nachrichten auch auf dem Hauptbildschirm als kleiner Hinweis.
- Nachrichten werden nicht gelöscht, nur archiviert.
- Alles bleibt in der Datenbank des Programms: **kein E-Mail-Versand, kein Internet**
  (`CLAUDE.md`, Regel 6). Auf einem einzelnen Rechner ist das eine Vorführung; echtes
  Schreiben zwischen Kollegen braucht den Mehrplatzbetrieb (Eintrag oben).
- Ausdrücklich nicht in der Demo: Anhänge, Lesebestätigung für den Absender.
Hinweis für einen späteren Echtbetrieb: Ein Nachrichtensystem mit Lese- oder
Zeitstempeln kann Verhalten und Leistung von Mitarbeitenden sichtbar machen. Vor einem
Einsatz die Mitarbeitervertretung beteiligen; das gilt auch für „gelesen“ in der Übergabe.

## Bewohnerakte: Ergänzungen
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die beiden zurückgestellten Vorschläge zur Bewohnerakte.
Warum: Tobi gibt alles frei (08.11.2026). „Offen geführt“ steht in der Lückenliste zu
DANlife: Diese Angabe liegt dort an pflegebezogenen Stellen und geht verloren.
Umfang:
- **„Offen geführt“** in den Stammdaten mit drei Zuständen wie bei den Allergien
  (`CLAUDE.md`, Regel 5): *ja*, *nein*, *nicht erfasst*. Als Marke auf Karte und Akte, Farbe
  nie allein. Ist das Modul Ausgangsliste eingeschaltet, mit dessen Angaben zur
  Unterbringung abgleichen und Widersprüche als Hinweis zeigen (nicht automatisch ändern).
- **Alter** aus dem Geburtsdatum berechnet, in der Akte neben dem Geburtsdatum („63 Jahre“).
- Demo-Daten um beide Angaben ergänzen.

## Mehrplatzbetrieb (Demo-Stufe)
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Mehrere Rechner arbeiten gleichzeitig mit derselben Datenbank, und zu jedem Bewohner gibt
es eine **Statusmeldung**, die alle Bereiche sehen. Gezeigt mit ausgedachten Daten.
Warum: Tobi gibt alles frei (08.11.2026). So lässt sich vorführen, wie das Programm über
mehrere Bereiche funktionieren würde, ohne dass ein Server der Einrichtung nötig ist.
Umfang:
- **Ort der Datenbank einstellbar** (Einstellungen): lokal (Vorgabe) oder ein gemeinsamer
  Ordner im Netz. Vorbild: gemeinsame Datenbank mit Konfliktschutz der Ausgangsliste
  (`Datenbank.cs`; Zuordnung in `docs/bruecke-ausgangsliste.md`).
- **Konfliktschutz:** Speichern zwei Rechner denselben Datensatz, gewinnt nicht still der
  Letzte; der zweite bekommt einen Hinweis und sieht beide Fassungen.
- **Bereiche:** Jede Installation hat in den Einstellungen eine Bereichskennung (ausgedacht,
  z. B. „Wohnbereich A“, „Therapie“). Bewohner gehören zu einem Wohnbereich; die Übersicht
  zeigt standardmäßig den eigenen, umschaltbar auf „alle Bereiche“.
- **Statusmeldung je Bewohner:** kurzer Text mit Bereich, Kürzel und Zeit (z. B. „beim
  Arzttermin bis 11 Uhr“), sichtbar auf Karte und Akte in allen Bereichen, bis sie abläuft
  oder ersetzt wird. Verlauf bleibt.
- Aktualisierung: Jeder Rechner liest Änderungen der anderen spätestens nach 30 Sekunden und
  beim Wechsel der Ansicht nach.
- Nachrichten, Übergabe und Aufgaben funktionieren damit auch zwischen Rechnern.
- Ausdrücklich nicht: Server-Dienst, Anmeldung mit Passwort, Rechteverwaltung, Verschlüsselung
  — das gehört zum Echtbetrieb. Die Demo-Regeln aus `CLAUDE.md` gelten unverändert, auch im
  gemeinsamen Ordner nur ausgedachte Daten.
- Testen ohne zweites Gerät: zwei Programmfenster auf einem Rechner mit demselben
  Datenbank-Ordner.

## Oberfläche: volle Fläche, einklappbares Seitenmenü
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Drei Änderungen an der Oberfläche nach Tobis Durchsicht von Version 0.20.0.
Warum: Rückmeldung Tobi (08.11.2026) mit Bildschirmfoto der Bewohnerakte (Reiter
„Wochenplan“): Die Inhalte nehmen nur etwa die linke Hälfte ein, rechts bleibt die Fläche
leer. Die Übersichtsseite ist gut und bleibt, wie sie ist.
Umfang:
- **Inhalte füllen die ganze restliche Fläche.** Keine feste Höchstbreite mehr; alle
  Ansichten (Akte mit Kopfkarte, Allergiehinweis, Reitern und deren Inhalt, Wochenplan,
  Kalender, Listen, Formulare im Fenster) wachsen mit der Fenstergröße in Breite und Höhe.
  Der Wochenplan verteilt die Tagesspalten auf die volle Breite und die Stunden auf die
  verfügbare Höhe (bei Platzmangel Bildlauf statt Abschneiden). Mindestgrößen so, dass bei
  1366 × 768 nichts abgeschnitten wird. Gilt für jeden Bereich, nicht nur die Akte.
- **Seitenmenü einklappbar:** Knopf oben im Seitenmenü (Symbol aus „Segoe Fluent Icons“)
  schaltet zwischen voller Breite und einer schmalen Leiste nur mit Symbolen um. Eingeklappt:
  Tooltip mit dem Namen je Symbol, Zähler (z. B. Aufgaben 5) als kleine Zahl am Symbol, aktiver
  Bereich weiterhin hervorgehoben. Unten Versions- und Bereichsangabe sowie „Demo-Daten
  zurücksetzen“ eingeklappt nur als Symbol mit Tooltip. Zustand wird in den Einstellungen
  gemerkt. Die frei werdende Fläche nutzen die Inhalte sofort (siehe oben).
- **„Neuer Bericht …“ ist kein eigener Menüpunkt mehr.** Stattdessen: Knopf „Neuer
  Bericht …“ im Kopf der Bewohnerakte (neben „Status …“; die farbige Hauptaktion bleibt
  „Bearbeiten …“ bzw. wird „Neuer Bericht …“ — die einfachere Lösung wählen und in
  `STAND.md` festhalten), im Reiter „Berichte“ und per Rechtsklick auf eine Bewohnerkarte
  (Übersicht und Bewohner). Ohne vorher gewählten Bewohner gibt es keinen Einstieg mehr.
- Bildschirmfotos im Testlauf für die geänderten Ansichten erneuern (ausgeklappt und
  eingeklappt).

## Mitarbeiter, Anmeldung und Rechte
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Eine Mitarbeiterdatenbank, Anmeldung beim Programmstart mit dem Namenskürzel und eine
Rechteverwaltung nach Positionen.
Warum: Vorgaben Tobi (08.11.2026): Man meldet sich beim Start mit seinem Kürzel an; das
Kürzel wird beim Anlegen eines Mitarbeiters festgelegt. Nicht jeder darf alles.
Umfang:
- **Mitarbeiterdatenbank:** Kürzel (eindeutig, Pflicht), Vor- und Nachname (ausgedacht),
  **Fachkraft ja/nein**, aktiv/ausgeschieden (ausgeschiedene können sich nicht mehr anmelden,
  bleiben aber an ihren Einträgen sichtbar). Gelöscht wird nicht.
- **Zuordnung zu Bereichen und Positionen** (Vorgabe Tobi, 08.11.2026): Im Mitarbeiter-Formular
  wählt man per Häkchen **einen oder mehrere Bereiche** und **eine oder mehrere Positionen**.
  Hat jemand mehrere Positionen, gelten die Rechte aller zusammen. Rechte, die sich auf
  „eigenen Bereich“ beziehen (z. B. Mitarbeiter verwalten für Bereichsleitungen), gelten für
  alle zugeordneten Bereiche.
- **Bereiche und Positionen sind Listen in den Einstellungen** (anlegen, umbenennen,
  stilllegen; nicht löschen, solange jemand zugeordnet ist). Demo-Bereiche ausgedacht, z. B.
  „Wohnbereich A“, „Wohnbereich B“, „Therapie“, „Verwaltung“.
- **Anmeldung beim Start:** Fenster „Anmelden“ mit Kürzel. Demo: **ohne Passwort**; das
  Fenster ist so gebaut, dass ein Passwortfeld später ergänzt werden kann. „Angemeldet als …“
  oben rechts wird zu „Abmelden / Benutzer wechseln“. Das bisherige „Benutzer wählen“ ohne
  Anmeldung entfällt.
- **Positionen (Vorbelegung):** Geschäftsführung, Hausleitung, Verwaltung, Bereichsleitung,
  Mitarbeiter Wohnbereich, Psychologischer Fachdienst, Pädagogischer Fachdienst, Therapie.
- **Rechte als Tabelle mit Häkchen** (Wunsch Tobi): Zeilen = Funktionen, Spalten =
  Positionen, dazu je Funktion ein Häkchen „nur Fachkräfte“. Bearbeiten dürfen nur
  Positionen mit dem Recht „Rechte verwalten“. Funktionen mindestens: Bewohner anlegen und
  bearbeiten; Berichte schreiben; Visiten eintragen; Medikation bearbeiten; Medikament
  anlegen; Medikamentendatenbank verwalten (prüfen, ändern, sperren); Therapien bearbeiten;
  Therapie-Teilnahme bestätigen; Kalender Haus bearbeiten; Fahrzeuge eintragen;
  Aufgaben-Vorlagen verwalten; HEB-Bogen erstellen; Mitarbeiter verwalten (eigener Bereich);
  Mitarbeiter verwalten (alle); Rechte verwalten; Module ein- und ausschalten; Einstellungen
  des Bereichs; Demo-Daten zurücksetzen.
- **Vorbelegung** (laut Tobi, in der Tabelle änderbar): Geschäftsführung, Hausleitung und
  Verwaltung dürfen alles. Wohnbereiche haben keine Mitarbeiterverwaltung, außer der
  Bereichsleitung (eigener Bereich). Der Psychologische Fachdienst verwaltet die
  Medikamentendatenbank. „Medikament anlegen“ **und „Medikation bearbeiten“** haben „nur
  Fachkräfte“ (Tobi, 08.11.2026); beide Häkchen lassen sich in den Einstellungen herausnehmen. „Therapien bearbeiten“
  nur Position Therapie (und die drei Leitungspositionen). Übrige Vorbelegung: die einfachere,
  sinnvolle Lösung, in `STAND.md` festhalten.
- Fehlt ein Recht, ist der Knopf ausgegraut mit Hinweis „Dafür fehlt die Berechtigung“ —
  nicht versteckt, damit die Demo zeigt, was es gibt.
- Demo: je Position mindestens ein ausgedachter Mitarbeiter, darunter Fachkräfte und
  Nicht-Fachkräfte; die bisherigen ausgedachten Kolleginnen und Kollegen werden übernommen.
- Ausdrücklich nicht: Passwort, Verschlüsselung, Sperre nach Fehlversuchen (Echtbetrieb).
Hinweis für einen späteren Echtbetrieb: Anmeldung mit Kürzel macht jede Handlung einer
Person zuordenbar; vor einem Einsatz Datenschutz und Mitarbeitervertretung beteiligen.

## Bezugspflege und Einzeltrainer
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Jedem Bewohner werden eine Bezugspflege und optional ein Einzeltrainer zugeteilt; beides
wird farblich gekennzeichnet.
Warum: Im Unternehmen wird mit dem Bezugspflegesystem gearbeitet: eine Bezugspflege für
mehrere Bewohner (meist bis zu vier, Anzahl nicht festgelegt). Dazu kommt ein Einzeltrainer
im Rahmen der DBT oder der Suchtgruppe (Tobi, 08.11.2026).
Festlegungen für die Demo (Annahmen aus dem Chat, 08.11.2026 — Tobi kann sie ändern):
- In der Akte (Stammdaten): Bezugspflege (Auswahl aus den Mitarbeitern), Einzeltrainer mit
  Art „DBT“ oder „Suchtgruppe“ (Liste erweiterbar). Keine feste Höchstzahl; ab fünf
  Bewohnern je Bezugspflege nur ein Hinweis.
- **Eigene Farbe je Mitarbeiter:** In den Einstellungen wählt jeder Angemeldete seine Farbe
  frei (Farbauswahl). Die Farbe gehört zum Mitarbeiter, nicht zum Rechner.
- **Darstellung auf Karten und in der Akte:** Bezugspflege als **kräftiger Farbstreifen** am
  linken Kartenrand mit Kürzel; Einzeltrainer **untergeordnet** als dünner Rahmen bzw. kleiner
  Farbpunkt mit Kürzel und Art (z. B. „DBT · AB“). Die Bezugspflege hat Vorrang und ist immer
  auffälliger. Farbe nie allein: Kürzel steht immer dabei; Farbschlüssel in der Statusleiste.
- Filter auf der Übersicht: „Meine Bezugsbewohner“ und „Meine Einzeltrainings“.
- Verlauf: Wechsel der Bezugspflege bleiben mit Datum nachvollziehbar.

## Medikation als Tabelle, Medikamentendatenbank in den Einstellungen
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Medikation in der Akte wird eine übersichtliche Tabelle. Die Medikamentendatenbank
wandert in die Einstellungen. Neu angelegte Medikamente werden geprüft.
Warum: Rückmeldung Tobi (08.11.2026): Ab mehr als drei Medikamenten wird die bisherige
Darstellung sehr unübersichtlich. Auf einen Blick muss erkennbar sein, wie das Medikament
heißt, welcher Arzt es verordnet hat und seit wann.
Umfang:
- Der Reiter „Medikation“ in der Akte heißt künftig **„Medikamentenplan“** (Begriff wie im
  Haus). Alle Änderungen an der Medikation eines Bewohners passieren hier.
- **Tabelle „Was gilt heute“:** eine Zeile je Medikament; Spalten: Medikament (Name,
  Stärke, Darreichungsform), morgens, mittags, abends, nachts, **verordnet von** (Arzt),
  **seit** (Datum), Status (aktiv / „pausiert bis …“ / „Stufe 2 von 4“), Hinweis. Sortierbar,
  Zeilen gut lesbar, abwechselnd hinterlegt. Bedarfsmedikation als eigene Tabelle darunter.
- Klick auf eine Zeile öffnet Einzelheiten und Verlauf (Pausen, Stufen, frühere Fassungen)
  — die Logik aus „Medikation: Grundverordnung, Pause, Stufenplan“ bleibt unverändert.
- Ausdruck der Tabelle (wie bisher mit „Demo — nicht zum Stellen verwenden“).
- **Medikamentendatenbank** nicht mehr in der Seitenleiste, sondern in den Einstellungen
  (Abschnitt „Medikamente“). Rechte nach „Mitarbeiter, Anmeldung und Rechte“.
- **Prüfung neuer Medikamente** (Vorgabe Tobi, 08.11.2026: kein Vier-Augen-Prinzip, das
  dauert zu lange): Die **Fachkraft, die das Medikament anlegt, prüft es selbst** gegen eine
  offizielle Quelle und bestätigt es im selben Formular. Beim Bestätigen sind **Quelle**
  (Pflicht, z. B. Adresse der Fachinformation) und Prüfdatum anzugeben; Kürzel und Zeit setzt
  das Programm. Ohne Bestätigung bleibt der Eintrag „ungeprüft“ und ist in der Auswahl so
  gekennzeichnet. Wer
  „Medikamentendatenbank verwalten“ darf, kann Einträge später korrigieren oder sperren;
  jede Änderung bleibt im Verlauf.
- **Nachschlagen im Formular** (Wunsch Tobi, 08.11.2026): Über der Eingabemaske „Medikament
  anlegen/ändern“ eine **kleine eingebettete Browseransicht**, die zum eingegebenen Namen die
  Suche öffnet — **nur beim BfArM** (Arzneimittel-Informationssystem über PharmNet.Bund; Suche
  seit 02/2025 kostenlos). Die Gelbe Liste bleibt draußen (Entscheidung Tobi, 08.11.2026).
  Die Fachkraft liest dort nach und überträgt die Angaben **selbst** in die Maske. Hilfe dazu:
  Text in der Ansicht markieren, dann „In Feld übernehmen“ setzt ihn in das gerade gewählte
  Feld. Die aktuelle Adresse der Ansicht lässt sich per Knopf als **Quelle** übernehmen.
  Technik: WebView2 (`Microsoft.Web.WebView2`, BSD-3-Clause, Laufzeit in Windows 11
  enthalten) — Begründung in `STAND.md`. Die Ansicht lässt nur Adressen des BfArM zu (feste
  Liste in den Einstellungen, Gruppe „Medikamente“); andere Links öffnen im normalen Browser.
  Ist keine Verbindung da oder WebView2 fehlt: Hinweis statt Fehler, das Formular bleibt voll
  nutzbar, daneben der Knopf „Im Browser öffnen“.
- **Abschaltbar:** Häkchen „Nachschlage-Ansicht anzeigen“ in den Einstellungen (Gruppe
  „Medikamente“), Vorgabe an. Ausgeschaltet bleibt nur der Knopf „Im Browser öffnen“. Gedacht
  für Rechner, auf denen WebView2 fehlt oder nicht erlaubt ist.
- **Betrieb auf einem Windows Server** (Frage Tobi, 08.11.2026; die Version des Servers im Haus
  ist noch nicht bekannt): Im README einen Abschnitt „Für die IT“ ergänzen —
  - Programm (self-contained .NET 10) läuft auf Windows Server 2016, 2019, 2022, 2025; auf
    Server 2012/2012 R2 nur mit Microsofts Zusatz-Updates (ESU), nicht empfohlen.
  - Die Nachschlage-Ansicht braucht die „Microsoft Edge WebView2 Runtime“; auf Servern oft
    nicht vorinstalliert, einmalig von der IT zu installieren (kostenlos). Auf Server
    2012/2012 R2 gibt es sie nicht mehr — dort die Ansicht abschalten.
  - Beim Start prüft das Programm, ob WebView2 vorhanden ist, und schreibt das Ergebnis in
    „Über“ (Einstellungen), damit man es ohne Fehlersuche sieht.
- **Neues Feld „Zulassungsnummer“** (ENR des BfArM), optional, damit ein Eintrag eindeutig
  wiederzufinden ist.
- Ausdrücklich nicht: Seiten **automatisch auslesen** und Felder selbst füllen, und kein
  automatischer Abgleich des ganzen Bestands — Nutzungsrechte nicht geklärt.

## Therapien: Zuordnung und Rechte
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Jede Therapie gehört zur Therapie oder zu einem Wohnbereich. Bearbeiten dürfen nur die
Berechtigten.
Warum: Vorgabe Tobi (08.11.2026): Nur die Therapie darf ihre Therapien bearbeiten.
Umfang:
- Feld **„Anbieter“** je Therapieangebot: „Therapie“ oder ein Wohnbereich. Filter danach.
- Angebote mit Anbieter „Therapie“ ändert nur, wer „Therapien bearbeiten“ hat (Vorbelegung:
  Position Therapie und Leitungen). Angebote eines Wohnbereichs ändert dessen Bereich
  (Bereichsleitung und Mitarbeiter des Bereichs mit dem Recht).
- Zuteilung von Bewohnern bleibt beim Anbieter; der Wohnbereich sieht alles, ändert aber nur
  eigene Angebote.
- Demo-Daten mit beiden Anbietern.

## Tagesplan
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Neuer Punkt unter „Übersicht“: wer heute welche Therapien hat, mit Bestätigung, dass der
Bewohner hingegangen ist.
Warum: Wunsch Tobi (08.11.2026): Wer ins System schaut, soll genau wissen, wo ein Bewohner
gerade sein müsste.
Umfang:
- Seitenleiste: „Tagesplan“ direkt unter „Übersicht“.
- Ansicht des heutigen Tages: Zeitleiste oder Tabelle mit allen Terminen aller Bewohner des
  Bereichs (Therapien laut Wochenplan, Kalendertermine, Fahrten), sortiert nach Uhrzeit;
  umschaltbar „nach Bewohner“.
- Je Termin Knöpfe **„gegangen“**, „abgesagt“, „ausgefallen“ (gleiche Teilnahme wie im
  Wochenplan, ein Datensatz, nicht doppelt). Gespeichert mit Kürzel und Uhrzeit.
- **„Wo müsste er sein?“:** Bei „gegangen“ zeigt der Bewohner auf Karte und Akte bis zum
  Ende des Termins „in Therapie: Holzwerkstatt bis 11:30“. Danach automatisch wieder im Haus,
  sofern nichts anderes eingetragen ist.
- Aktualisierung für alle Rechner über den Mehrplatzbetrieb (wie Statusmeldungen).
- Nicht bestätigte Termine nach Beginn: Marke „noch offen“.

## Kalender: Bereich und Haus, Fahrzeuge
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Zwei Kalender (Bereich und Haus) und eine Fahrzeugverwaltung für die beiden Firmenwagen.
Warum: Vorgabe Tobi (08.11.2026): Es gibt einen Kalender für den Bereich und einen für das
Haus. Mit einem VW Bus und einem Caddy werden Bewohner zum Arzt gefahren, Ausflüge gemacht
oder Sporttherapien wie Schwimmen begleitet. Man muss sehen, welches Fahrzeug vor Ort ist oder
weg, mit Uhrzeit.
Umfang:
- **Kalender „Bereich“ und „Haus“:** Beim Anlegen wählbar; Anzeige beider übereinander mit
  Umschalter (nur Bereich / nur Haus / beide), farblich und mit Beschriftung unterschieden.
  Haus-Termine bearbeitet, wer „Kalender Haus bearbeiten“ hat.
- **Fahrzeuge:** Liste in den Einstellungen (Demo: „VW Bus“, „Caddy“, erweiterbar).
  **Fahrt eintragen:** Fahrzeug, von–bis (geplant), Zweck (Arzttermin, Ausflug,
  Sporttherapie, Sonstiges), Fahrer (Kürzel), mitfahrende Bewohner, Ziel.
- **Anzeige „vor Ort / unterwegs“:** In der Übersicht (und im Kalender) je Fahrzeug: „vor
  Ort“ oder „unterwegs seit 09:40, zurück geplant 11:30 (Fahrer AB)“. Knöpfe „losgefahren“
  und „zurück“ mit Uhrzeit. Überschrittene Rückkehrzeit als Hinweis.
- **Doppelbuchung** desselben Fahrzeugs: Warnung beim Speichern.
- Mitfahrende Bewohner erscheinen in ihrem Tagesplan bzw. als abwesend.

## Aufgaben nach Schichten mit Standardaufgaben
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Aufgaben werden nach Schichten geordnet; es gibt wiederkehrende Standardaufgaben.
Warum: Vorgabe Tobi (08.11.2026): Im Nachtdienst zum Beispiel gibt es immer Standardaufgaben,
die regelmäßig wiederholt werden. Ersetzt die frühere Festlegung „keine wiederkehrenden
Aufgaben“ im Eintrag „Wohnbereich-ToDo“.
Umfang:
- **Schichtzeiten** des Bereichs in den Einstellungen (Früh, Spät, Nacht mit Uhrzeiten).
- Aufgabenansicht gruppiert nach Schicht; oben die aktuelle Schicht.
- **Standardaufgaben (Vorlagen):** Text, Schicht, Wochentage (täglich oder ausgewählte),
  optional Uhrzeit und Bewohner. Daraus entstehen zu Beginn jeder Schicht automatisch die
  Aufgaben dieser Schicht. Verwalten darf, wer „Aufgaben-Vorlagen verwalten“ hat.
- Abhaken mit Kürzel und Uhrzeit; nicht erledigte Standardaufgaben bleiben als „aus der
  Nachtschicht offen“ sichtbar, bis sie jemand erledigt oder begründet schließt.
- Einzelne Aufgaben wie bisher (an nächste Schicht weitergeben usw.).
- Demo: einige ausgedachte Standardaufgaben je Schicht, besonders für den Nachtdienst.
- Ausdruck: Aufgabenliste der Schicht.

## Visiten
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Neuer Punkt in der Seitenleiste unter der Berichtsübersicht („Übergabe“) für Visiten: was
besprochen wurde, ob sich Anordnungen geändert haben, und vereinbarte Facharzttermine.
Warum: Vorgabe Tobi (08.11.2026): Es ist wichtig, was in der Visite besprochen wurde und ob
sich Anordnungen geändert haben. Facharzttermine, die dort besprochen wurden, sollen als
Aufgabe auftauchen; ist ein Termin vereinbart, soll er in den Kalender.
Umfang:
- Visite anlegen: Datum, Art (z. B. Hausarzt, Psychiater, Facharzt), Arzt (ausgedacht),
  Teilnehmer (Kürzel), je Bewohner: Besprochenes (Text), **Anordnungen geändert ja/nein**
  mit Art (neu verordnet, Dosis geändert, pausiert, abgesetzt) und Medikament (Auswahl aus
  der Datenbank oder Freitext, falls noch nicht vorhanden), Facharzttermin nötig
  (Fachrichtung, Dringlichkeit).
- Ansicht: Liste aller Visiten, Filter nach Bewohner und Zeitraum; in der Akte ein Abschnitt
  „Visiten“ (im Reiter „Berichte“ oder eigener Reiter — die einfachere Lösung).
- **Anordnung geändert** (Vorgabe Tobi, 08.11.2026): **Sobald der Visiten-Bericht
  gespeichert ist**, blendet das Programm sofort eine Aufgabe ein, je Änderung eine:
  - Medikament **noch nicht in der Datenbank** → „Neues Medikament anlegen: …“ mit Knopf, der
    das Anlegen-Formular öffnet (Name aus der Visite vorbelegt; nur Fachkräfte, siehe oben).
    Danach geht es direkt weiter zur Verordnung.
  - Medikament **vorhanden** → „Medikation ändern: … (neu / Dosis / Pause / absetzen)“ mit
    Knopf, der den **Medikamentenplan des jeweiligen Bewohners** öffnet (Reiter
    „Medikamentenplan“) und dort das passende Formular, vorbelegt mit der Visite als
    Anordnung (Arzt, Datum). Geändert wird nur dort (Vorgabe Tobi, 08.11.2026).
  - Die Aufgabe steht zusätzlich in den Aufgaben der Schicht und in der Übergabe, bis sie
    erledigt ist; „Später“ schließt nur die Einblendung.
  - Die Medikation wird **nicht** automatisch geändert; eine berechtigte Person trägt die
    Änderung über die Aufgabe ein, die Verknüpfung zur Visite bleibt sichtbar.
- **Facharzttermin nötig:** erzeugt automatisch eine Aufgabe „Termin bei … vereinbaren“.
  Beim Erledigen fragt das Programm nach Datum, Uhrzeit, Ort; dann entsteht der Termin im
  Kalender (Bereich) beim Bewohner, und auf Wunsch eine Fahrt mit Fahrzeug.
- Visiten-Einträge sind Quelle für Monatsreflexion und HEB-Bogen (Lebensbereich 5).
- Demo-Daten: einige Visiten mit allen drei Fällen.

## Ausgangsliste als übersichtliche Liste
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Bildschirmdarstellung des Moduls Ausgangsliste wird neu gedacht, angelehnt an die
ausdruckbare Tagesliste.
Warum: Rückmeldung Tobi (08.11.2026): Die Darstellung ist sehr unübersichtlich; man soll sich
ungefähr an die ausdruckbare Liste halten, sie aber für den Bildschirm umdenken.
Umfang:
- Eine **Tabelle**, eine Zeile je Bewohner, Spalten wie auf der Tagesliste (Vorbild
  `Druckliste.cs` der Ausgangsliste): Zimmer, Name, Unterbringung bzw. Beschränkung (kurz),
  Ausgänge heute (von–bis, Hz.), Stand jetzt (im Haus / unterwegs seit … / abwesend).
- Für den Bildschirm umgedacht: **Wer gerade unterwegs ist, steht oben** und ist
  hervorgehoben; Überfällige deutlich markiert (Farbe nie allein). Knöpfe „raus“ / „zurück“
  direkt in der Zeile, ohne zusätzliches Fenster für den Normalfall. Filter „nur unterwegs“,
  „nur mit Beschränkung“.
- Alles füllt die Fläche (Schritt 18).
- Da das Dokumentationssystem Muttersystem ist: Änderungen in
  `src/DokuLine.Modul.Ausgangsliste/AENDERUNGEN.md` eintragen (Abgleich mit der
  eigenständigen Ausgangsliste, `docs/bruecke-ausgangsliste.md`).

## E-Mail und Kalender über externe Programme
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: E-Mails an Betreuer und Kalendertermine lassen sich mit einem externen E-Mail- bzw.
Kalenderprogramm verbinden.
Warum: Wunsch Tobi (08.11.2026): E-Mails an Betreuer sollen über ein externes E-Mail-Programm
gehen; der Kalender soll Termine von außen aufgreifen können.
Umfang (vereinbar mit `CLAUDE.md`, Regel 6 — das Programm selbst geht nicht ins Internet):
- **Betreuer** in der Akte: Name, Stelle, E-Mail, Telefon (Demo: ausgedacht, E-Mail-Adressen
  nur mit der reservierten Endung `@example.org`, damit nichts an echte Adressen gehen kann).
- **„E-Mail an Betreuer …“** öffnet das auf dem Rechner eingestellte E-Mail-Programm (z. B.
  Outlook) mit Empfänger und Betreff (`mailto:`). Text und Versand macht der Mensch im
  E-Mail-Programm. Vor dem Öffnen ein Hinweis: „Keine Gesundheitsdaten unverschlüsselt per
  E-Mail senden.“
- **Kalender-Austausch über Dateien** im Standardformat `.ics`: Termin(e) exportieren (lassen
  sich in Outlook usw. öffnen) und `.ics`-Dateien importieren (z. B. eine Termineinladung
  aus dem E-Mail-Programm speichern und ins Programm ziehen). Beim Import Vorschau und
  Zuordnung zu Bewohner und Kalender (Bereich/Haus).
- Ausdrücklich nicht: direkte Verbindung zu Outlook, Exchange, Google oder einem Mailserver.

## Einstellungen neu gegliedert
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Das Einstellungsmenü bekommt den Aufbau der Windows-11-Einstellungen: links Gruppen,
rechts deren Inhalt, oben Suche. Alle Einstellungen der Schritte davor ziehen dorthin um.
Warum: Frage Tobi „Was ist die beste Gestaltung für ein Einstellungsmenü?“, Vorschlag aus dem
Chat freigegeben (08.11.2026). Grundlage: Microsoft „Guidelines for app settings“; abweichend
davon mehrere Seiten statt einer, weil das Programm viele Einstellungen hat.
Umfang:
- **Einstieg:** „Einstellungen“ ganz unten in der Seitenleiste, fest dort. Öffnet als ganze
  Seite im Inhaltsbereich (kein kleines Fenster), füllt die Fläche (Schritt 18).
- **Links die Gruppen**, in dieser Reihenfolge, je mit Symbol aus „Segoe Fluent Icons“:

  | Gruppe | Inhalt | Wer (Vorbelegung, Rechte-Tabelle) |
  |---|---|---|
  | Persönlich | eigene Farbe, Seitenmenü eingeklappt, Abmelden | jeder |
  | Wohnbereich | Bereichskennung, Schichtzeiten, Fristen (Beschluss endet bald, HEB fällig), Logo | Bereichsleitung |
  | Module | Ausgangsliste mit Untermodulen Küchendienst und Digitaler Ausgang, an/aus | Leitungen |
  | Aufgaben | Standardaufgaben je Schicht | „Aufgaben-Vorlagen verwalten“ |
  | Fahrzeuge | Fahrzeugliste | mit Recht |
  | Vorlagen | HEB-PDFs A/B/C mit Feldzuordnung | mit Recht |
  | Medikamente | Medikamentendatenbank | Fachkräfte (anlegen), Psychologischer Fachdienst (verwalten) |
  | Verwaltung | Mitarbeiter, Bereiche und Positionen, Rechte-Tabelle | Leitungen, Verwaltung, Bereichsleitung (eigener Bereich) |
  | System | Ort der Datenbank, Sicherung, Update, Demo-Daten zurücksetzen | Leitungen |
  | Über | Version, Build, Lizenzen der Bausteine, Demo-Hinweis | jeder |

- **Suchfeld** oben: findet Einstellungen über Namen und Erklärung und springt hin.
- **Jede Einstellung als Karte:** Name, ein Satz Erklärung, rechts das Bedienelement.
  Seltene Unterpunkte aufklappbar, höchstens eine Ebene. Kurze Gruppenüberschriften (ein Wort).
- **Bedienelemente:** an/aus als Schalter (selbst gezeichnet im Stil der Ausgangsliste) oder
  Häkchen — die einfachere Lösung, einheitlich im ganzen Menü; bis 5 Möglichkeiten
  Auswahlknöpfe, mehr als 5 Klappliste; Aktionen als Knopf.
- **Änderungen wirken sofort**, ohne „Speichern“. Ausnahme: ganze Datensätze (Mitarbeiter,
  Medikament, Fahrzeug, Standardaufgabe, HEB-Feldzuordnung) haben „Speichern“ und
  „Abbrechen“.
- **Ohne Recht ausgegraut, nicht versteckt**, mit einem Satz „Dafür fehlt die Berechtigung“.
- **Folgenreiches nur in „System“:** „Demo-Daten zurücksetzen“ und Wechsel des Datenbank-Orts
  fragen immer vorher nach; Hinweis in Gefahrenfarbe mit Wort, Farbe nie allein.
- Was bisher an anderen Stellen eingestellt wurde (Update-Häkchen, Logo, Kürzel, Module,
  Medikamentendatenbank, Datenbank-Ort, Fahrzeuge, Schichtzeiten …), zieht hierher um; es
  bleibt keine zweite Stelle.
- Bildschirmfotos im Testlauf für jede Gruppe.

## Platzhalter für das Einrichtungslogo
Status: erledigt (08.11.2026; mit Version 0.22.0)
Datum: 08.11.2026
Was: In der obersten Leiste (Kopf) steht oben rechts ein Platzhalter für das Logo der Einrichtung.
Warum: Wunsch Tobi in der Sitzung (08.11.2026).
Umfang: Gestrichelter Rahmen „Logo der Einrichtung“, solange kein eigenes Bild gewählt ist; Klick
öffnet die Einstellungen („Bild im Kopf“). Das echte Logo wählt jeder Rechner selbst; es liegt im
Datenordner und kommt nie ins Repository (`CLAUDE.md`, Regel 7).

## Kürzel automatisch aus der Anmeldung
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Überall, wo ein Kürzel eingetragen wird, setzt das Programm das Kürzel der angemeldeten
Person selbst.
Warum: Vorgabe Tobi (08.11.2026): Man meldet sich mit dem Kürzel an; es soll sich bei den
meisten Dokumenten selbst setzen.
Umfang:
- **Zwei Arten von Kürzel-Feldern** unterscheiden:
  - **„Eingetragen von“** (wer hat geschrieben, bestätigt, erledigt, gelesen): wird immer
    automatisch gesetzt und ist **nicht änderbar**. Betrifft u. a. Berichte und Nachträge,
    Übergabe (setzen, gelesen, erledigt), Aufgaben (angelegt, erledigt, weitergegeben),
    Teilnahme im Tagesplan, Medikamentenplan (Eintrag, Pause, Stufe), Medikament anlegen und
    bestätigen, Visiten, Statusmeldungen, Fahrten (losgefahren/zurück), Ausgang, Biografie,
    Reflexion, HEB-Bogen, Nachrichten.
  - **Personen-Felder** (wer ist dabei oder zuständig, z. B. Fahrer, Teilnehmer der Visite,
    Bezugspflege): mit der angemeldeten Person **vorbelegt**, aber änderbar bzw. ergänzbar
    (Auswahl aus den Mitarbeitern, kein Freitext).
- Alle bisherigen Formulare durchgehen: Eingabefelder für „Verfasser“/„Kürzel“ entfernen bzw.
  durch eine schreibgeschützte Anzeige „eingetragen von AB“ ersetzen.
- Die Einstellung „eigenes Kürzel“ entfällt (falls noch vorhanden); maßgeblich ist allein
  die Anmeldung.
- Ohne Anmeldung lässt sich nichts eintragen (nur ansehen), Hinweis „Bitte zuerst anmelden“.
- Gilt ab jetzt für alle neuen Schritte (Regel oben im Bauplan).

## Ärzteverzeichnis
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Eine Liste der Ärztinnen und Ärzte, die im Programm überall zur Auswahl steht.
Warum: Vorgabe Tobi (08.11.2026): In der Visite müssen die im System hinterlegten Ärzte zur
Auswahl stehen.
Umfang:
- Verzeichnis in den Einstellungen (Gruppe „Verwaltung“): Name, Fachrichtung, Praxis/Ort,
  Telefon, E-Mail (Demo: ausgedacht, nur `@example.org`), aktiv/inaktiv. Nicht löschen, nur
  inaktiv setzen, solange der Arzt irgendwo eingetragen ist.
- Recht „Ärzteverzeichnis verwalten“ in der Rechte-Tabelle (Vorbelegung: Leitungen,
  Verwaltung, Bereichsleitung, Fachkräfte).
- **Auswahl statt Freitext** überall, wo ein Arzt eingetragen wird: Visite, Medikamentenplan
  („verordnet von“), Pause/Stufenplan (Anordnung), Facharzttermin, Kalendertermin beim Arzt.
  Fehlt jemand: „Arzt anlegen …“ direkt aus der Auswahl (mit Recht).
- Vorhandene Freitext-Einträge in den Demo-Daten auf das Verzeichnis umstellen.
- Demo: etwa sechs ausgedachte Ärztinnen und Ärzte verschiedener Fachrichtungen.

## Visite: Übersicht aller Bewohner
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Eine Visitenansicht, in der man mit dem Arzt alle Bewohner der Reihe nach durchgeht —
mit der aktuellen Medikation jedes Bewohners — und währenddessen den Bericht schreibt.
Warum: Vorgabe Tobi (08.11.2026): Die Arztvisite braucht eine Komplettübersicht über alle
Bewohner mit aktueller Medikation, damit man jeden Bewohner mit dem Arzt durchgehen und
gleichzeitig den Bericht schreiben kann.
Umfang (ändert den Eintrag „Visiten“):
- **Feld „Art der Visite“ entfällt.** Arzt wird aus dem Ärzteverzeichnis gewählt (26b).
- **„Visite beginnen …“:** Datum (heute), Arzt (Auswahl), dabei (angemeldete Person
  vorbelegt, weitere wählbar).
- **Visitenansicht**, füllt die ganze Fläche:
  - **links** die Liste aller Bewohner des Bereichs (Zimmer, Name, Bezugspflege-Farbe,
    Häkchen „besprochen“), sortierbar nach Zimmer oder Name; oben „noch offen: 7 von 10“.
  - **rechts** zum gewählten Bewohner: Kopf mit Name, Zimmer, Allergiehinweis; darunter die
    **Tabelle der aktuellen Medikation** wie im Medikamentenplan (inkl. Bedarf, Pausen,
    Stufen), die letzte Visite in einer Zeile, offene Aufgaben zur Medikation;
  - **unten rechts** die Eingabe für diesen Bewohner: Besprochenes, Anordnungen geändert,
    Facharzttermin nötig (wie im Eintrag „Visiten“).
  - Knöpfe „◀ Vorheriger“ / „Nächster ▶“; beim Wechsel wird der Eintrag des Bewohners als
    Entwurf gespeichert, nichts geht verloren (auch nicht bei Absturz).
- **„Visite abschließen“:** speichert alles, legt Berichte, Übergaben und Aufgaben an und
  zeigt **dann gesammelt** alle neuen Aufgaben (Medikament anlegen, im Medikamentenplan
  eintragen, Termin eintragen) mit ihren Knöpfen. Während der Visite erscheinen keine
  Einblendungen, damit das Gespräch mit dem Arzt nicht unterbrochen wird (Festlegung aus dem
  Chat, Tobi kann sie ändern); eine kleine Zahl „3 Aufgaben vorgemerkt“ zeigt den Stand.
- Unterbrochene Visite lässt sich später fortsetzen („Visite fortsetzen“).
- **Ausdruck „Visitenliste“:** alle Bewohner mit aktueller Medikation, ein Block je Bewohner,
  Platz für Notizen — als Papiervorlage, falls am Bildschirm nicht gearbeitet werden kann.
- Die bisherige Eingabe „Neue Visite …“ für einzelne Bewohner bleibt erhalten.

## Umbenennung in DokuLine, Teil 1 (Übergang)
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Das Programm heißt **DokuLine**, Leittext **„Betreuung auf Augenhöhe“**. Alles wird
umbenannt: Programm, Programmdatei, Projekte, Datenordner, Dokumentation — und danach das
Repository (`KhaosDE/Dokumentationssystem` → `KhaosDE/DokuLine`).
Warum: Entscheidung Tobi (08.11.2026): Name „DokuLine“, Leittext „Betreuung auf Augenhöhe“;
„Repo und so weiter müssen alle umbenannt werden“.
**Warum in Teilen:** Der Updater bereits installierter Versionen akzeptiert nur Downloads,
deren Adresse mit `https://github.com/KhaosDE/Dokumentationssystem/releases/download/`
beginnt, und sucht die Datei `Dokumentationssystem.exe` (`Aktualisierung.cs`). Würde das
Repository sofort umbenannt oder die Datei sofort anders heißen, bekämen diese Installationen
keine Updates mehr. Deshalb zuerst eine Übergangsversion unter dem alten Repository-Namen,
dann die Umbenennung des Repositorys, später das Aufräumen.
Umfang Teil 1 (Claude Code, Repository heißt dabei noch `Dokumentationssystem`):
- **Angezeigter Name** überall „DokuLine“: Fenstertitel, Kopf, Anmeldefenster, „Über“,
  Druckvorschau-Fußzeile, README. Unter dem Namen der Leittext „Betreuung auf Augenhöhe“
  (Anmeldefenster, Kopf der Übersicht, „Über“, README). Der Hinweis „Demo — ausgedachte
  Daten“ bleibt unverändert.
- **Projekte und Namensräume** umbenennen: `Dokusystem.*` → `DokuLine.*` (Ordner unter
  `src/`, `.csproj`, Namensräume, Lösung `Dokumentationssystem.sln` → `DokuLine.sln`,
  Tests, Workflow). Das Modul heißt dann `src/DokuLine.Modul.Ausgangsliste/`.
- **Programmdatei** heißt `DokuLine.exe`. **Übergang:** Der Workflow veröffentlicht in jedem
  Release **zusätzlich** dieselbe Datei als `Dokumentationssystem.exe` mit eigener
  `.sha256`, damit alte Installationen sie finden. Release-Titel „DokuLine …“, Tags weiter
  `build-<Laufnummer>`.
- **Updater** (neu): akzeptiert beide Adress-Anfänge
  (`…/KhaosDE/Dokumentationssystem/releases/download/` und
  `…/KhaosDE/DokuLine/releases/download/`) und beide Dateinamen; fragt die neueste Version über
  `…/repos/KhaosDE/DokuLine/releases/latest` ab und fällt auf die alte Adresse zurück, falls
  die neue (noch) nicht antwortet. Er tauscht die laufende Datei unter ihrem **eigenen**
  Dateinamen aus (heißt sie noch `Dokumentationssystem.exe`, bleibt das so). Tests für beide
  Fälle.
- **Datenordner** `%APPDATA%\DokuLine\`. Beim ersten Start: gibt es den neuen Ordner nicht,
  aber `%APPDATA%\Dokumentationssystem\`, wird dessen Inhalt in den neuen Ordner **kopiert**
  (Datenbank, Bilder, Einstellungen, Logs). Der alte Ordner bleibt unangetastet, damit nichts
  verloren geht. Ein Eintrag im Protokoll sagt, dass kopiert wurde.
- Selbst gewählter **Datenbank-Ort** (Mehrplatz) bleibt wie eingestellt.
- **Dokumentation:** `CLAUDE.md` (Name, Pfade, Projektnamen; die Regeln selbst nicht ändern),
  `README.md`, `STAND.md`, `docs/*.md`. In `IDEEN.md` nur Pfadangaben anpassen, erledigte
  Einträge sonst unverändert lassen.
- Nach dem Zusammenführen in `STAND.md` unter „Offene Punkte“ vermerken: „Teil 2:
  Repository umbenennen — sobald dieses Release erschienen ist.“
Teil 2 (kein Claude-Code-Auftrag, macht der Chat oder Tobi, **erst wenn Teil 1 als Release
erschienen ist**): Repository auf GitHub in `DokuLine` umbenennen. GitHub leitet alte
Adressen weiter; Version 0.29.2 kommt mit beiden Namen zurecht.
Teil 3 (später, eigener Eintrag, erst wenn alle Installationen mindestens 0.29.2 haben):
alten Dateinamen und alten Adress-Anfang entfernen, `%APPDATA%\Dokumentationssystem\` nicht
mehr beachten.

## Position „Administrator“ mit Kürzel TST
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Neue Position „Administrator“ und ein Mitarbeiter mit dem Kürzel **TST** in dieser
Position.
Warum: Wunsch Tobi (08.11.2026): „Lege ein Kürzel TST als Administrator an.“
Umfang:
- **Position „Administrator“** an erster Stelle der Positionsliste. Hat **alle** Rechte der
  Rechte-Tabelle, auch „Rechte verwalten“ und „Mitarbeiter verwalten (alle)“, in allen
  Bereichen. Diese Häkchen sind für den Administrator **nicht abwählbar** (ausgegraut mit
  Hinweis „Der Administrator hat immer alle Rechte“), damit sich niemand aus der
  Rechteverwaltung aussperren kann. „Nur Fachkräfte“ gilt für den Administrator nicht.
- Es muss immer **mindestens ein aktiver Administrator** bleiben: Den letzten kann man weder
  ausscheiden lassen noch seine Position entziehen (Hinweis statt Fehler).
- **Mitarbeiter TST** in `daten/demo/mitarbeiter.json`: Kürzel `TST`, Vorname „Test“,
  Nachname „Administrator“ (kein echter Name, `CLAUDE.md` Regel 1), Fachkraft ja, alle Bereiche,
  Position „Administrator“.
- **Bestehende Datenbanken:** Fehlt TST beim Start, wird er einmalig ergänzt (auch ohne
  „Demo-Daten zurücksetzen“), Protokolleintrag. Ein vorhandener TST wird nicht verändert.
- Anmeldung mit `TST` wie bei allen anderen (Demo ohne Passwort).

## Abmelden sichtbar machen, Tastenkürzel F12
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Eine deutlich sichtbare Möglichkeit, sich abzumelden, und das Tastenkürzel **F12**.
Warum: Rückmeldung Tobi (08.11.2026): „Es fehlt eine Möglichkeit, sich wieder abzumelden.
Tastenkürzel dafür ist F12.“ Die bisherige Lösung (Klappmenü „Abmelden / Benutzer
wechseln“ oben rechts, `Hauptfenster.cs`) wird nicht gefunden.
Umfang:
- **Oben rechts im Kopf:** „Angemeldet: TST“ (Kürzel und Name) und daneben ein eigener Knopf
  **„Abmelden (F12)“** — kein verstecktes Klappmenü.
- **F12** meldet in jedem Bereich und aus jedem Fenster des Programms heraus ab.
- Zusätzlich: Symbol „Abmelden“ unten in der Seitenleiste (auch eingeklappt, mit Tooltip
  „Abmelden (F12)“) und in den Einstellungen unter „Persönlich“.
- **Nach dem Abmelden** sieht man nur noch das Anmeldefenster über dem leeren Hauptfenster —
  keine Bewohnerdaten mehr sichtbar, damit man den Platz verlassen kann. Anmelden mit
  anderem Kürzel = Benutzer wechseln.
- **Ungespeicherte Eingaben** (z. B. ein angefangener Bericht): vor dem Abmelden fragen
  „Als Entwurf speichern / Verwerfen / Abbrechen“. Entwürfe gehören dem Kürzel, das sie
  angefangen hat.
- Tastenkürzel im Tooltip und in der Hilfe/„Über“ nennen.

## Kleine Verbesserungen: Nachrichten „gelesen“, Datum mit Punkten, Kopf der Akte
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Drei kleine Änderungen nach Tobis Durchsicht.
Warum: Rückmeldung Tobi (08.11.2026).
Umfang:
- **Nachrichten als gelesen markieren:** Knopf „Gelesen“ an jeder Nachricht im Posteingang
  (auch mehrere markiert auf einmal). Eine gelesene Nachricht wandert **automatisch ins
  Archiv** — für die Person, die sie gelesen hat (bei Team-Nachrichten bleibt sie bei den
  anderen Teammitgliedern ungelesen). Aus dem Archiv lässt sie sich mit „Wieder als ungelesen“
  zurückholen. Zähler „ungelesen“ passt sich an. Weiterhin wird nichts gelöscht.
- **Datum mit automatischen Punkten — im ganzen Programm:** In jedem Datumsfeld setzt das
  Programm die Punkte beim Tippen selbst: `08112026` wird zu `08.11.2026`, `081126` zu
  `08.11.2026` (zweistellige Jahre als 20xx). Tippt man selbst Punkte, stört das nicht. Ein
  gemeinsamer Baustein für alle Datumsfelder, keine Einzellösungen. Gilt auch für Uhrzeiten
  sinngemäß: `0930` → `09:30`.
- **Kopf der Bewohnerakte:** Öffnet man einen Bewohner, steht im Kopf groß **„Bewohnerakte“**.
  Der Name erscheint nur **einmal**, auf der Karte darunter — nicht zusätzlich im Kopf.

## Wohnbereiche R1–R3, Zimmer und Betten, Aufnahme
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Demo bekommt die Bereichsstruktur des Hauses, eine Zimmer- und Bettenverwaltung, und
die Aufnahme zeigt freie Zimmer und Betten.
Warum: Vorgaben Tobi (08.11.2026): Es gibt drei Wohnbereiche, **R1 offen**, **R2** und **R3
beschützend** — „das übernehmen wir schon mal so“. Beim Anlegen eines Bewohners muss die
Zimmerauswahl freie Zimmer und Betten zeigen (Bett Fenster, Bett Tür). „Offen geführt“ gehört
nicht in die Aufnahme, weil nur geschlossen aufgenommen wird.
Umfang:
- **Bereiche** in Demo-Daten und Einstellungen: `R1` (offen), `R2` (beschützend), `R3`
  (beschützend). Je Bereich ein Merkmal „offen / beschützend“. Die Bezeichnungen sind
  allgemeine Kürzel, kein Name der Einrichtung (Entscheidung Tobi, 08.11.2026). Bisherige
  Demo-Bereiche „Wohnbereich A/B“ werden ersetzt (Bewohner, Mitarbeiter, Therapie, Kalender,
  Mehrplatz-Bereichskennung).
- **Zimmer und Betten** (Kern, Einstellungen → „Wohnbereich“): je Zimmer Nummer, Bereich und
  Betten. Einzelzimmer = ein Bett; Doppelzimmer = **„Bett Fenster“** und **„Bett Tür“**. Ein
  Bett ist frei oder belegt (durch einen Bewohner). Demo: je Bereich etwa sechs Zimmer,
  gemischt Einzel- und Doppelzimmer, Zimmernummern ausgedacht (z. B. R1: 101–106).
- **Bewohner** haben künftig Zimmer **und** Bett. Bestehende Demo-Bewohner bekommen ein Bett
  zugeordnet.
- **Aufnahme („Neuer Bewohner …“):** Auswahl erst Bereich, dann eine Liste **nur der freien
  Betten** („102 – Bett Fenster“, „104 – Einzelzimmer“). Belegte Betten erscheinen nicht.
  Kein freies Bett: Hinweis „In diesem Bereich ist kein Bett frei“.
- **„Offen geführt“** ist **nicht** im Aufnahmeformular. Neue Bewohner stehen automatisch auf
  „nein“ (geschlossen aufgenommen); ändern lässt es sich später in der Akte.
- Zimmerwechsel und Entlassung geben das Bett frei; ein Zimmerwechsel zeigt dieselbe Auswahl
  freier Betten.
- Übersicht: Filter nach Bereich, Kopfzeile „R2 · 9 Bewohner · 3 Betten frei“.
- Modul Ausgangsliste nutzt dieselben Zimmer (keine zweite Zimmerliste; Zimmergruppen des
  Küchendienstes bleiben im Modul).

## Fenster „Ärztliche Informationen“
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Diagnosen und Allergien bekommen ein eigenes Fenster „Ärztliche Informationen“.
Warum: Vorgabe Tobi (08.11.2026).
Umfang:
- In der Akte ein Knopf bzw. Reiter **„Ärztliche Informationen“**. Darin:
  - **Diagnosen:** Liste mit ICD-10-Schlüssel (optional) und Bezeichnung, seit (Datum), Art
    (Hauptdiagnose / Nebendiagnose), gestellt von (Arzt aus dem Ärzteverzeichnis), Hinweis.
    Nichts wird gelöscht; nicht mehr zutreffende Diagnosen „beendet am …“, Verlauf bleibt.
  - **Allergien und Unverträglichkeiten:** wie bisher mit den drei Zuständen (`CLAUDE.md`,
    Regel 5), dazu je Eintrag Art der Reaktion (optional).
  - Platz für weitere ärztliche Angaben (Freitext „Hinweise für den Notfall“).
- Der **Allergiehinweis bleibt oben in der Akte sichtbar** (Sicherheit); bearbeitet wird er
  nur noch in diesem Fenster.
- **Rechte:** neue Funktionen „Ärztliche Informationen ansehen“ und „Ärztliche Informationen
  bearbeiten“ in der Rechte-Tabelle. Vorbelegung: ansehen alle Positionen außer Verwaltung;
  bearbeiten nur Fachkräfte (Häkchen „nur Fachkräfte“) und Leitungen. Ohne Recht ist der Knopf
  ausgegraut mit Begründung; der Allergiehinweis bleibt für alle sichtbar.
- **Demo-Daten:** ausgedachte Diagnosen für die ausgedachten Bewohner, allgemein gehalten und
  zum psychiatrischen Wohnheim passend; keine Fallgeschichten (`CLAUDE.md`, Regel 1).
- HEB-Bogen und Visite zeigen die Diagnosen als Information an, das Programm wertet sie nicht.

## Tagesplan als Anzeigetafel
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Der Tagesplan bekommt ein neues Aussehen nach dem Vorbild einer **Anzeigetafel am
Flughafen**. Die aktuellen Termine stehen oben und sind hervorgehoben.
Warum: Rückmeldung Tobi (08.11.2026): Die Gestaltung muss überarbeitet werden, sie soll anders
aussehen, die aktuellen Termine oben hervorgehoben „wie bei einem Flughafenterminal“.
Umfang (Festlegungen aus dem Chat, Tobi kann sie ändern):
- **Tafel-Optik** als eigener Bereich auf der Seite: dunkle Fläche, helle, große Schrift,
  Uhrzeit in fester Zeichenbreite (Spalten stehen sauber untereinander), gut lesbar auf
  Abstand. Oben rechts groß die aktuelle Uhrzeit und das Datum. Der Rest des Programms bleibt
  im hellen Stil.
- **Spalten** wie auf einer Abflugtafel: Zeit · Bewohner (mit Zimmer) · Termin bzw. Therapie ·
  Ort · Begleitung/Fahrzeug · **Status**.
- **Status als Wort mit Farbe** (Farbe nie allein): „JETZT“ (läuft gerade), „GLEICH“ (beginnt
  in den nächsten 30 Minuten), „GEPLANT“, „GEGANGEN“, „NOCH OFFEN“ (hat begonnen, keine
  Bestätigung), „ABGESAGT“, „AUSGEFALLEN“, „ZURÜCK“.
- **Reihenfolge:** oben ein abgesetzter Block **„Jetzt und gleich“**, darunter „Später
  heute“; vergangene Termine rutschen nach unten unter „Erledigt“ und werden blasser.
- **Selbstaktualisierung** jede Minute (Uhrzeit, Status, Reihenfolge) und bei jeder Änderung,
  auch von anderen Rechnern (Mehrplatz).
- Bestätigen wie bisher direkt in der Zeile („gegangen“, „abgesagt“, „ausgefallen“) —
  bedienbar ohne die Tafel zu verlassen.
- Umschalter „nach Bewohner“ bleibt.
- **Vollbild-Ansicht** (Knopf und Taste F11): nur die Tafel, z. B. für einen Bildschirm im
  Dienstzimmer; Esc beendet.
- Bildschirmfotos im Testlauf erneuern.

## Demo-Daten: 23 Bewohner je Wohnbereich
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Demo bekommt so viele Bewohner wie im Haus: **23 je Wohnbereich**, also 69 in R1–R3.
Warum: Vorgabe Tobi (08.11.2026): Aktuell hat jeder Wohnbereich 23 Bewohner; die Zahl soll zum
Testen der Übersichtlichkeit dienen.
Umfang:
- 23 ausgedachte Bewohner je Bereich (erkennbar ausgedachte Namen, `CLAUDE.md` Regel 1),
  verteilt auf Zimmer und Betten; **je Bereich zwei Betten frei**, damit die Aufnahme etwas
  anzeigt. Zimmer in den Demo-Daten entsprechend erweitern (Einzel- und Doppelzimmer gemischt).
- Alle Bereiche mit Daten füllen, in sinnvollem Umfang: Berichte (auch „Wichtig“ und
  „Übergabe“), Medikamentenplan, Wochenplan/Therapien, Kalender, Bezugspflege (bis etwa vier
  Bewohner je Bezugspflege, dafür genug ausgedachte Mitarbeiter), Ausgangsliste, Biografie,
  Visiten. Datumsangaben weiter relativ zum heutigen Tag.
- **Übersichtlichkeit prüfen:** Bildschirmfotos im Testlauf aller Hauptansichten mit 23
  Bewohnern (bei 1920 × 1080 und 1366 × 768). Wo eine Ansicht dabei unübersichtlich oder
  langsam wird, das in `STAND.md` unter „Offene Punkte“ notieren (nicht eigenmächtig umbauen).
- Start und Bildaufbau bleiben flüssig (Richtwert: Übersicht in unter einer Sekunde).

## Kopfzeile nur „Wo bin ich“, Logo, Abmelden; Aktionen darunter
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Knöpfe für Aktionen wandern aus der Kopfzeile in eine Leiste darunter. Sonst bleibt die
Kopfzeile, wie sie ist.
Warum: Vorgabe Tobi (08.11.2026): In der Kopfzeile soll immer nur stehen, wo man ist, dazu Logo
und Abmelden. Punkte wie „Neuer Bewohner“ oder „Neue Aufgabe“ gehören unter die Kopfzeile.
Ergänzung Tobi (08.11.2026): „Die Kopfzeile soll vom Aussehen her gleich bleiben, nur die
Punkte, die ich erwähnt habe, nach unten schieben.“
Umfang:
- **Kopfzeile (`KopfBereich`) bleibt im Aussehen unverändert:** abgerundete Fläche mit
  Farbverlauf, kleine Zeile und Titel links, Logofeld bzw. Platzhalter rechts, Hinweis
  „Demo — ausgedachte Daten“ an seiner Stelle, darunter „Angemeldet: …“ und „Abmelden (F12)“.
  Farben, Schriften, Abstände, Schatten und Radien nicht ändern.
- **Nur die Knopfzeile (`KnoepfeSetzen`) zieht um.** Die Kopfzeile wird um genau diese Zeile
  niedriger, sonst nichts.
- „Entwurf fortsetzen“ ist eine Aktion und zieht mit in die Aktionsleiste (rechts außen).
- **Aktionsleiste direkt unter der Kopfzeile**, gleiche Breite und gleiche Seitenränder wie die
  Kopfzeile, Knöpfe im bestehenden Stil (`Stil.cs`, Knopfhöhe 36 px). Reihenfolge in allen
  Bereichen gleich: links die Hauptaktion (farbig, eine pro Ansicht), danach die übrigen
  Aktionen, rechts Filter, Ansichtsschalter, „Drucken …“. So findet man Knöpfe immer an
  derselben Stelle.
- Gibt es in einer Ansicht keine Aktion, entfällt die Leiste (kein leerer Streifen).
- Alle Bereiche umstellen, auch Akte, Visite, Kalender, Einstellungen und Modul Ausgangsliste.
- Bildschirmfotos im Testlauf erneuern.

## Bereich „Bewohner“ als Liste mit Berichten
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Der Bereich „Bewohner“ bekommt eine Liste statt Kacheln. Wählt man einen Bewohner, erscheinen
daneben seine Berichte.
Warum: Rückmeldung Tobi (08.11.2026): Übersicht und Bewohner unterscheiden sich kaum. „Bewohner“
soll eine Liste haben; wählt man einen Bewohner aus, erscheinen die Berichte.
Umfang:
- **Übersicht** bleibt das Kartenraster mit Marken („was ist heute wichtig“).
- **Bewohner:** links eine **Liste** (Tabelle) aller Bewohner des Bereichs — Zimmer/Bett, Name,
  Bezugspflege (Farbe mit Kürzel), Marken kurz (Allergie, Beschluss, Übergabe offen); sortierbar,
  Suchfeld, Filter nach Bereich. Mit 23 Bewohnern ohne Bildlauf lesbar, wo möglich.
- **Rechts die Berichte** des ausgewählten Bewohners, neueste oben, mit den Filtern aus der
  täglichen Dokumentation (Zeitraum, Lebensbereich, „nur wichtige“) und „Neuer Bericht …“ in der
  Aktionsleiste (bezogen auf den ausgewählten Bewohner).
- Mit Pfeiltasten durch die Liste blättern; die Berichte wechseln mit.
- **Akte öffnen** per Doppelklick oder Knopf „Akte öffnen“ (Aktionsleiste).

## Visite: „Nächster Bewohner“ unter der Texteingabe
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: In der Visitenansicht stehen „◀ Vorheriger“ und „Nächster Bewohner ▶“ direkt unter der
Texteingabe. „Nächster Bewohner“ ist ausgegraut, solange für den aktuellen Bewohner kein Bericht
geschrieben ist.
Warum: Vorgabe Tobi (08.11.2026): Die Visite bleibt sonst, wie sie ist.
Umfang:
- Knöpfe unter dem Feld „Besprochenes“, nicht mehr an anderer Stelle.
- „Nächster Bewohner ▶“ wird aktiv, sobald im Feld „Besprochenes“ Text steht; Tooltip im
  ausgegrauten Zustand: „Erst den Bericht zu diesem Bewohner schreiben.“
- „◀ Vorheriger“ ist immer aktiv. Die Liste links bleibt anklickbar wie bisher.
- Sonst keine Änderung an der Visite.

## Übersicht neu: „Heute wichtig“ und Bewohnerliste
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Die Übersicht bekommt einen neuen Aufbau: links „Heute wichtig“, rechts alle Bewohner des
Bereichs als Liste. Das Kartenraster in der Übersicht entfällt.
Warum: Entscheidung Tobi (08.11.2026) aus vier Entwürfen (Variante B „Heute zuerst“). Mit
23 Bewohnern je Bereich ist eine Zeile pro Person übersichtlicher als Karten.
Umfang:
- Kopfzeile und Aktionsleiste wie in Schritt 26i. Hauptaktion „Neuer Bericht …“; außerdem
  „Neue Aufgabe …“, „Neue Nachricht …“, „Neuer Bewohner …“ (grün), rechts Bereichswahl
  R1/R2/R3 und „Drucken …“.
- **Links „Heute wichtig“** (etwa 360–400 px breit), vier Kästen untereinander, jeder mit
  Überschrift in Großbuchstaben und Zähler:
  1. **Übergaben offen:** Bewohner mit Zimmer, Text, „seit <Datum> · <Kürzel>“. Sie bleiben
     stehen, bis sie erledigt sind.
  2. **Termine heute:** Uhrzeit und Text aus dem Tagesplan, nach Uhrzeit sortiert, mit dem
     Status aus dem Tagesplan (z. B. „gegangen“, „noch offen“).
  3. **Laufender Dienst · noch offen:** „6 von 9 erledigt“, Fortschrittsbalken, die offenen
     Aufgaben zum Abhaken (Kürzel und Uhrzeit wie bisher).
  4. **Nachrichten ungelesen:** Absender → Empfänger und die erste Zeile.
  Ein Klick auf einen Eintrag öffnet ihn an seiner Stelle (Übergabe, Tagesplan, Aufgaben,
  Nachrichten). Ist ein Kasten leer, steht dort „Nichts offen“, er verschwindet nicht.
- **Rechts die Bewohnerliste** als Tabelle, eine Zeile je Bewohner, nach Zimmer sortiert,
  Spalten: Zimmer/Bett · Name · Bezugspflege (Farbe mit Kürzel) · Wo (im Haus, Ausgang bis …,
  Termin …, in Therapie … bis …, Klinik seit …) · Hinweise (Marken: Beschluss endet …,
  Allergie, Übergabe, Geburtstag). Darüber „Bewohner R2 · 23 · 17 im Haus“ und ein Suchfeld.
  Mit 23 Bewohnern bei 1920 × 1080 ohne Bildlauf lesbar, wo möglich.
- Doppelklick auf eine Zeile öffnet die Akte. Die Liste in der Übersicht zeigt **keine
  Berichte**; das macht der Bereich „Bewohner“ (Schritt 26j). So unterscheiden sich beide.
- Unter 1100 px Breite rutscht „Heute wichtig“ über die Liste.
- Statusleiste: „DEMO — ausgedachte Daten“, Datum, Dienst, Stand, Farbschlüssel der Marken.

## Therapie: Ansicht „Laufende Therapien“ als Anwesenheitstafel
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: Im Therapie-Bereich eine neue Ansicht, die zeigt, welche Therapien gerade laufen und wer
dort ist.
Warum: Entscheidung Tobi (08.11.2026): Der Entwurf „Anwesenheitstafel“ (Variante C) soll für
die Therapien verwendet werden, die aktuell laufen.
Umfang:
- Therapie-Bereich: neuer erster Reiter **„Laufende Therapien“**. Bereichswahl „Alle
  Bereiche“ (Vorgabe für die Therapie) oder R1/R2/R3 (Vorgabe für einen Wohnbereich). Alle
  sehen die Ansicht. Bestätigen darf, wer es heute im Tagesplan darf.
- **Oben eine Tagesleiste** von 7 bis 21 Uhr: alle Therapieangebote des Tages als Balken in
  ihrer Länge, ein orangefarbener Strich für „jetzt“. Ein Klick auf einen Balken öffnet das
  Angebot. Überschneidungen liegen in eigenen Zeilen.
- **Darunter eine Spalte je laufender Therapie:** Name, Ort, Therapeut-Kürzel, „bis 11:30“,
  Belegung „5 von 8“. Darin die Teilnehmer als kleine Kärtchen (Bereich und Zimmer, Name,
  Bezugspflege-Kürzel) mit dem Teilnahmestatus als Wort mit Farbe: „gegangen“, „noch offen“,
  „abgesagt“, „ausgefallen“.
- **Daneben zwei feste Spalten:** „Gleich“ (Beginn in den nächsten 30 Minuten, mit
  Teilnehmerzahl) und „Noch offen“ (hat begonnen, Teilnahme nicht bestätigt; Knöpfe
  „gegangen“, „abgesagt“ direkt am Kärtchen).
- Läuft gerade nichts: „Gerade läuft keine Therapie. Nächste: <Name> um <Uhrzeit>.“
- **Unten „Rest des Tages“:** die übrigen Angebote nach Uhrzeit, mit Teilnehmerzahl.
- **Gleiche Daten wie Tagesplan und Wochenplan:** ein Teilnahme-Datensatz, kein zweiter.
  Was hier bestätigt wird, steht sofort im Tagesplan, in der Übersicht („in Therapie … bis …“)
  und im Wochenplan des Bewohners.
- Aktualisiert sich jede Minute und bei Änderungen von anderen Rechnern (Mehrplatz).
- Demo-Daten: Therapieangebote über den ganzen Tag, sodass beim Vorführen zu den meisten
  Uhrzeiten etwas läuft. Je Angebot 3–8 Teilnehmer aus R1–R3.

## Programmsymbol „Hell wie die Kopfzeile“
Status: erledigt (08.11.2026)
Datum: 08.11.2026
Was: DokuLine bekommt ein eigenes Programmsymbol für die `.exe`, die Taskleiste, die Fenster und
den Desktop.
Warum: Entscheidung Tobi (08.11.2026) aus vier Entwürfen: Entwurf 4 „Hell wie die Kopfzeile“,
„erstmal“ — ein späterer Wechsel muss leicht bleiben.
Umfang:
- **Vorlage** (selbst gezeichnet im Chat, keine fremde Vorlage, keine Lizenzfrage): die zwei
  SVG-Dateien unten. Ablage im Repository als `src/DokuLine/Bilder/programmsymbol.svg` (groß)
  und `src/DokuLine/Bilder/programmsymbol-klein.svg` (klein, ohne Farbverlauf).
- **Symboldatei** `src/DokuLine/Bilder/DokuLine.ico` mit den Größen 16, 20, 24, 32, 40, 48, 64
  und 256 px: 16–32 px aus der kleinen Vorlage, ab 40 px aus der großen. Umwandlung mit einem
  kleinen Skript im Repository (`werkzeuge/symbol-erzeugen.*`), damit ein Wechsel des Symbols
  nur die SVG-Dateien betrifft. Werkzeug dafür nur mit freier Lizenz, Begründung in `STAND.md`;
  die fertige `.ico` liegt im Repository, der Windows-Build braucht das Werkzeug nicht.
- Einbinden: `ApplicationIcon` im Projekt `DokuLine` (Symbol der `.exe`), dasselbe Symbol für
  das Hauptfenster und alle weiteren Fenster und Dialoge (zentral, z. B. in `Stil.cs`).
- **Nicht** in das Logofeld der Kopfzeile: Das bleibt für das Bild der Einrichtung aus den
  Einstellungen (`CLAUDE.md`, Regel 7).
- Prüfen: Symbol der `.exe` im Explorer, in der Taskleiste (hell und dunkel) und im
  Fenstertitel; Bildschirmfoto im Testlauf.

Große Vorlage (`programmsymbol.svg`):

```svg
<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 64 64">
  <defs>
    <radialGradient id="dlOrange" cx="0" cy="0" r="1">
      <stop offset="0" stop-color="#EEA63C" stop-opacity="0.55"/>
      <stop offset="1" stop-color="#EEA63C" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="dlBlau" cx="1" cy="1" r="1">
      <stop offset="0" stop-color="#5480C6" stop-opacity="0.5"/>
      <stop offset="1" stop-color="#5480C6" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect x="2" y="2" width="60" height="60" rx="14" fill="#FFFFFF" stroke="#C9D3E3" stroke-width="2"/>
  <rect x="3" y="3" width="58" height="58" rx="13" fill="url(#dlOrange)"/>
  <rect x="3" y="3" width="58" height="58" rx="13" fill="url(#dlBlau)"/>
  <rect x="14" y="17" width="36" height="6" rx="3" fill="#2D549C"/>
  <rect x="14" y="29" width="26" height="6" rx="3" fill="#2D549C"/>
  <rect x="14" y="41" width="36" height="6" rx="3" fill="#EEA63C"/>
</svg>
```

Kleine Vorlage (`programmsymbol-klein.svg`, für 16–32 px):

```svg
<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 64 64">
  <rect x="2" y="2" width="60" height="60" rx="14" fill="#FFFFFF" stroke="#C9D3E3" stroke-width="2"/>
  <rect x="14" y="17" width="36" height="6" rx="3" fill="#2D549C"/>
  <rect x="14" y="29" width="26" height="6" rx="3" fill="#2D549C"/>
  <rect x="14" y="41" width="36" height="6" rx="3" fill="#EEA63C"/>
</svg>
```
## Assistenzkraft und Fachkraft
Status: erledigt (08.11.2026) – freigegeben von Tobi direkt in Claude Code („ja, bau Assistenzkraft und Fachkraft“), 08.11.2026
Datum: 08.11.2026
Was: Die kleinste Stufe ist die **Assistenzkraft**, darüber die **Fachkraft**. Assistenzkräfte machen
z. B. keine Visiten und dürfen Bedarfsmedikation nur nach Absprache mit einer Fachkraft ausgeben.
Warum: Vorgabe Tobi (08.11.2026, direkt in der Claude-Code-Sitzung).
Umsetzung (Vorschlag von Claude Code, von Tobi bestätigt):
- **Qualifikation statt Häkchen:** Jeder Mitarbeiter ist „Assistenzkraft“ oder „Fachkraft“ (bisher nur das
  Häkchen „Fachkraft“); so steht es im Anmeldefenster, in der Mitarbeiterliste und im Formular.
- **Visiten nur Fachkräfte:** Recht „Visiten eintragen“ mit Häkchen „nur Fachkräfte“; für Assistenzkräfte ist
  „Visite beginnen …“ grau mit Grund.
- **„Bedarf gegeben …“** (neu, gibt es noch nicht) im Medikamentenplan an jeder Bedarfsverordnung: Uhrzeit,
  Menge, Anlass; zeigt, wie viel heute schon gegeben wurde und was höchstens erlaubt ist. Bei einer
  Assistenzkraft Pflichtfeld „in Absprache mit“ (Fachkraft aus der Liste); erscheint als Bericht in der Akte.


## Akte: Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger
Status: freigegeben (Bauplan Schritt 30; OK Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die Bewohnerakte bekommt drei neue Reiter für die Verwaltungsangaben.
Warum: Vorgabe Tobi (09.11.2026): Das Bewohnerstammblatt muss weiter ausgebaut werden. Neben den
Daten, die auch durch die Biografie ergänzt werden, gehören Reiter für Krankenkasse,
Kostenträger, Betreuer und Dokumente hinein.
Umfang (Vorschlag):
- **Reihenfolge der Reiter** (Ergänzung 09.11.2026): Stammdaten · Betreuung · Angehörige · Ärzte ·
  Krankenkasse · Kostenträger · Schweigepflicht (Schritt 35) · Dokumente ·
  Medikamentenplan · Wochenplan · Biografie · Reflexion · HEB (Modul-Reiter wie
  „Ausgang“ danach). Die Stammdaten zeigen je eine Kurzzeile („Betreuer: …“, „Krankenkasse: …“,
  „Kostenträger: … bis …“), ein Klick springt in den Reiter.
- **Drei Zustände** für jede Angabe (`CLAUDE.md`, Regel 5): eingetragen · „keine“ (z. B. keine
  gesetzliche Betreuung) · „nicht erfasst“ (in Achtung-Farbe).
- **Jede Änderung ist eine neue Fassung** mit Kürzel und Datum, der Verlauf bleibt sichtbar
  („ab Datum …“). Bearbeiten mit dem Recht „Bewohner anlegen und bearbeiten“.
- **Reiter „Betreuung“** (heißt so, weil darin auch die Zuständigen aus Forensik bzw. FOSA
  stehen, Schritt 30a). Abschnitt **Betreuer** (die bisherige Zeile aus den Stammdaten zieht
  hierher um):
  - Art: Berufsbetreuer · ehrenamtlich · Angehörige · Vorsorgebevollmächtigte.
  - Person aus dem **Personenverzeichnis** (Schritt 29a), dort Name, Büro, Telefon, E-Mail,
    Anschrift. Fehlt sie: „Person anlegen …“ direkt aus der Auswahl.
  - **Aufgabenbereiche** zum Ankreuzen (z. B. Gesundheitssorge, Aufenthaltsbestimmung,
    Vermögenssorge, Behörden und Ämter, Wohnungsangelegenheiten, Post, Unterbringung) plus
    Freitext. Liste in den Einstellungen änderbar.
  - Bestellt am, **Überprüfung bis** → Marke „Betreuung endet …“ 8 Wochen vorher.
  - Weitere Personen: Ersatzbetreuer.
  - Knöpfe „E-Mail an Betreuer …“ (wie bisher) und „Betreuerausweis öffnen“ (aus „Dokumente“).
- **Reiter „Angehörige“** (Vorgabe Tobi, 09.11.2026: eigener Reiter):
  - Liste der Angehörigen aus dem Verzeichnis „Angehörige“ (Schritt 29a): Name, **Beziehung**
    (Mutter, Bruder, Partnerin …), Telefon, Mobil, E-Mail, Anschrift.
  - Kennzeichen „Hauptkontakt“ und „im Notfall benachrichtigen“ (je ja/nein).
  - Je Person der **Schweigepflicht-Zustand** als Marke mit Wort und Farbe (Schritt 35), ein
    Klick öffnet den Eintrag im Reiter „Schweigepflicht“.
  - Hinweis je Person (Freitext, z. B. „Besuch nur nach Absprache“).
  - „Angehörigen hinzufügen …“: aus dem Verzeichnis wählen oder neu anlegen. Drei Zustände für
    den Reiter: Angehörige eingetragen · „keine Angehörigen bekannt“ · „nicht erfasst“.
- **Reiter „Ärzte“** (Vorgabe Tobi, 09.11.2026): Zuteilung der Ärzte zum Bewohner.
  - Feste Zeilen **Hausarzt** und **Psychiater**, je mit drei Zuständen: Arzt aus dem
    Verzeichnis „Ärzte“ · „keiner“ · „nicht erfasst“ (Achtung-Farbe).
  - **Weitere Ärzte** frei ergänzbar: Arzt aus dem Verzeichnis und Rolle (Fachrichtung des Arztes
    vorbelegt, z. B. Zahnarzt, Neurologe, Gynäkologin), Hinweis.
  - Je Zeile Praxis, Telefon und „seit“; Wechsel als neue Fassung mit Datum.
  - Die zugeteilten Ärzte stehen in Visite, Medikamentenplan („verordnet von“), Kalender und
    Überleitungsbogen **oben in der Auswahl**, alle übrigen Ärzte darunter.
  - **Arztzuteilung schon bei der Aufnahme** (Vorgabe Tobi, 09.11.2026): Das Formular „Neuer
    Bewohner …“ bekommt den Abschnitt **„Ärzte“** mit **Hausarzt** und **Psychiater** (Auswahl aus
    „Verzeichnisse → Ärzte“, „Arzt anlegen …“ direkt aus der Auswahl). Für beide muss ein Zustand
    gewählt sein — Arzt · „keiner“ · „noch nicht bekannt“ (= nicht erfasst) —, sonst lässt sich
    die Aufnahme nicht abschließen. Weitere Ärzte optional. Die Angaben landen im Reiter „Ärzte“.
- **Krankenkasse:**
  - Kasse (Auswahl aus einer Liste in den Einstellungen, Demo: ausgedachte Kassen wie
    „Musterkasse Süd“), Versichertennummer (Demo: ausgedacht), Status (Mitglied,
    familienversichert …).
  - **Zuzahlungsbefreiung:** ja (gültig bis …) · nein · nicht erfasst → Marke „Befreiung endet …“
    4 Wochen vorher.
  - Pflegegrad: keiner · 1–5 · nicht erfasst. Ansprechpartner und Telefon der Kasse.
- **Kostenträger:**
  - Kostenträger (Auswahl, Demo: ausgedacht, z. B. „Bezirk Musterland“), Aktenzeichen,
    Sachbearbeiter mit Telefon und E-Mail.
  - **Bewilligungen** als Liste: Leistung (Freitext bzw. Auswahl), Bescheid vom, **bewilligt
    von–bis** → Marke „Kostenzusage endet …“ 12 Wochen vorher (rechtzeitig für den HEB-Bogen).
    Die aktuelle Bewilligung steht oben, abgelaufene darunter.
  - Der HEB-Bogen übernimmt Kostenträger und Aktenzeichen von hier.
- Die neuen Fristen erscheinen in der Übersicht unter „Heute wichtig“ und als Marke in der
  Bewohnerliste.
- Demo-Daten: alle drei Reiter für alle Bewohner, gemischt mit „keine“ und „nicht erfasst“.

## Akte: Reiter „Dokumente“ – digitale Akte in DokuLine
Status: freigegeben (Bauplan Schritt 31; OK Tobi 09.11.2026)
Datum: 09.11.2026
Was: Jeder Bewohner bekommt eine Dokumentenablage in DokuLine, sauber nach Bereichen sortiert,
direkt aus der Akte zu öffnen.
Warum: Vorgabe Tobi (09.11.2026): Heute liegt für jeden Bewohner ein Ordner in einem Ordner (die
digitale Akte). Man muss das Programm verlassen und den Explorer öffnen, um wichtige Dokumente
aufzurufen. Sinnvoller ist, wenn alle Dokumente sauber sortiert in DokuLine hinterlegt sind und
ohne Umschweife zugänglich.
Umfang (Vorschlag):
- **Eigene Ablage** (Entscheidung Tobi, 09.11.2026): DokuLine kopiert die Dokumente in seine
  Ablage und verwaltet sie dort, mit Rechten und Protokoll; kein bloßes Anzeigen eines fremden
  Ordners.
- **Ablage:** Die Dateien liegen im Datenordner (`%APPDATA%\DokuLine\Dokumente\<Bewohner-Nr>\`,
  im Mehrplatzbetrieb neben der gemeinsamen Datenbank), nie im Repository. Die Datenbank kennt
  zu jeder Datei: Kategorie, Titel, Datum des Dokuments, **gültig bis** (optional), Hinweis,
  hinzugefügt von (Kürzel) und am, Prüfsumme.
- **Kategorien** (in den Einstellungen änderbar), Vorschlag: Aufnahme und Verträge · Betreuung
  und Gericht · Kostenträger und Bescheide · Krankenkasse · Ärztliches und Befunde · Ausweise
  und Behörden · Schriftverkehr · Sonstiges.
- **Ansicht:** links die Kategorien mit Zähler, rechts die Dokumente als Liste (Titel, Datum,
  gültig bis, hinzugefügt von), neueste oben, Suchfeld über alle Kategorien. Abgelaufene
  Dokumente mit Marke „abgelaufen“. Ein Klick auf eine Frist springt in den passenden Reiter.
- **Öffnen** per Doppelklick im Standardprogramm des Rechners (Übergabe an ein Programm auf dem
  Rechner, `CLAUDE.md` Regel 6; keine eingebettete Webansicht). „Im Ordner zeigen …“ als
  Ausweg.
- **Hinzufügen:** Dateien in die Liste ziehen oder „Dokument hinzufügen …“. Danach ein kurzer
  Dialog: Kategorie (vorgeschlagen nach Dateiname), Titel, Datum, gültig bis. Die Datei wird in
  die Ablage **kopiert**; das Original bleibt, wo es war.
- **Verknüpfung mit den Reitern:** z. B. Betreuerausweis → Reiter „Betreuer“, Bescheid →
  Bewilligung im Reiter „Kostenträger“, Beschluss → Unterbringung. Dort ein Knopf „Dokument
  öffnen“.
- **Nichts geht verloren:** „Entfernen“ verschiebt in einen Papierkorb der Ablage (mit Kürzel
  und Grund), endgültig löschen nur mit eigenem Recht. Ersetzen legt eine neue Fassung an.
- **Rechte** (Rechtematrix): „Dokumente ansehen“, „Dokumente hinzufügen“, „Dokumente endgültig
  löschen“. Protokoll: wer wann welches Dokument geöffnet, hinzugefügt oder entfernt hat.
- **Demo-Daten:** Das Programm erzeugt selbst ausgedachte Beispiel-PDFs (PDFsharp) mit großem
  Aufdruck „DEMO — ausgedachte Daten“, z. B. Betreuerausweis, Kostenzusage, Arztbrief, ohne
  echte Logos, Behörden- oder Einrichtungsnamen (`CLAUDE.md`, Regeln 1 und 7).

## Vorhandene digitale Akte übernehmen
Status: freigegeben (Bauplan Schritt 32; OK Tobi 09.11.2026)
Datum: 09.11.2026
Was: Ein Assistent, der einen vorhandenen Ordner „ein Unterordner je Bewohner“ in die
Dokumentenablage übernimmt.
Warum: Die bisherige digitale Akte (Ordner im Explorer) soll nicht von Hand umgezogen werden
müssen.
Umfang (Vorschlag):
- Einstellungen → „Digitale Akte übernehmen …“: Hauptordner wählen. Das Programm ordnet die
  Unterordner den Bewohnern zu (nach Name, Vorschlag zum Bestätigen), Unterordner zweiter Ebene
  den Kategorien (nach Namen, Zuordnung einstellbar).
- Vorschau vor dem Übernehmen: Zahl der Dateien je Bewohner und Kategorie, nicht zugeordnete
  Ordner und Dateien. Übernehmen **kopiert**, der alte Ordner bleibt unverändert.
- Bericht danach (übernommen, übersprungen, doppelt) und Eintrag im Protokoll.
- In der Demo nur mit ausgedachten Ordnern zum Vorführen (Testordner aus den Demo-Daten).

## Überleitungsbogen auf Knopfdruck
Status: freigegeben (Bauplan Schritt 33; Auswahl Tobi 09.11.2026)
Datum: 09.11.2026
Was: Ein Blatt für Klinik, Notarzt oder Rettungsdienst, das das Programm aus vorhandenen Angaben
zusammenstellt.
Warum: Auswahl Tobi (09.11.2026). Bei einer Einweisung muss es schnell gehen; fast alle Angaben
stehen schon im Programm.
Umfang:
- Knopf „Überleitungsbogen …“ in der Akte (Aktionsleiste), dazu per Rechtsklick in Übersicht und
  Bewohnerliste.
- Kurzer Dialog: Anlass (Freitext), Datum und Uhrzeit (vorbelegt: jetzt), Begleitung, Häkchen
  je Abschnitt (alle vorbelegt), damit nur Nötiges weitergegeben wird.
- **Inhalt:** Einrichtung, Bereich und Telefon des Dienstzimmers (aus den Einstellungen);
  Name, Geburtsdatum, Zimmer; **Allergien** (drei Zustände, `CLAUDE.md` Regel 5); Diagnosen und
  „Hinweise für den Notfall“ (Ärztliche Informationen); **aktueller Medikamentenplan** mit Stand
  heute, auch Bedarf und Pausen; Betreuer mit Aufgabenbereichen und Telefon (bei Forensik:
  „nicht zuständig“, dafür die Zuständigen der Forensik bzw. FOSA, Schritt 30a); Krankenkasse und
  Versichertennummer; Hausarzt, Psychiater und weitere Ärzte (Reiter „Ärzte“); Unterbringung bzw. Beschluss;
  Schweigepflichtentbindung gegenüber der Klinik (Schritt 35); letzte Vitalwerte (Schritt 36);
  Freitext „Besonderheiten“ (Kommunikation, Hilfsmittel, was beruhigt).
- Fehlende Angaben stehen als „nicht erfasst“ auf dem Blatt, nie als leeres Feld.
- Druckvorschau, Drucken, als PDF speichern. Eine Kopie mit Datum geht in die Dokumente des
  Bewohners (Kategorie „Ärztliches und Befunde“).
- Danach Angebot: Bericht „Überleitungsbogen erstellt: <Anlass>“ (Wichtig, Übergabe) und
  Abwesenheit „Klinik seit …“ im Modul Ausgangsliste eintragen (je ein Häkchen).
- Protokoll: wer wann einen Bogen erstellt hat. Demo: ausgedachte Einrichtungsangaben.

## Abgängigkeitsmeldung
Status: freigegeben (Bauplan Schritt 34; Auswahl Tobi 09.11.2026)
Datum: 09.11.2026
Was: Ein geführter Ablauf, wenn ein Bewohner vermisst wird: Meldeblatt, wer informiert wurde,
Rückkehr.
Warum: Auswahl Tobi (09.11.2026).
Umfang:
- Knopf „Abgängigkeit melden …“ in Akte und Übersicht. Im Modul Ausgangsliste zusätzlich ein
  Hinweis, wenn eine Rückkehrzeit überschritten ist (Wartezeit in den Einstellungen,
  Vorgabe 30 Minuten): „Rückkehrzeit überschritten seit … — Abgängigkeit melden?“
- **Formular:** vermisst seit, zuletzt gesehen (wo, wann, von wem), Kleidung heute (Pflicht),
  Gefährdung (Freitext, dazu Hinweis auf benötigte Medikamente laut Plan), mögliche
  Aufenthaltsorte, Kontakte (Angehörige, Betreuer).
- **Personenbeschreibung** als neuer Abschnitt in den Stammdaten: Größe, Statur, Haarfarbe,
  Augenfarbe, Brille, besondere Merkmale, Datum der letzten Aktualisierung (Marke, wenn älter
  als 12 Monate). Bild aus der Akte; in der Demo nur der Platzhalter (`CLAUDE.md`, Regel 2).
- **Liste „Wer wurde informiert“** zum Abhaken, je mit Uhrzeit und Kürzel: Leitung bzw.
  Rufbereitschaft, Polizei (dazu Name und Vorgangsnummer), Betreuer, Angehörige, ggf. Klinik
  bzw. Gericht; bei Forensik bzw. FOSA deren Zuständige als feste Zeile (Schritt 30a). Die Liste und die Telefonnummern sind in den Einstellungen änderbar, weil jede
  Einrichtung ihren eigenen Ablauf hat.
- **Meldeblatt drucken** (für die Polizei): Personenbeschreibung, Bild, Kleidung, zuletzt
  gesehen, Gefährdung, Kontakt der Einrichtung.
- Ab der Meldung: Status „abgängig seit …“ in Übersicht, Bewohnerliste und Ausgangsliste (Marke
  in Gefahr-Farbe mit Wort) und ein Bericht (Wichtig, Übergabe).
- **„Zurück“** mit Uhrzeit, Zustand (Freitext) und zweiter Liste „Wer wurde entwarnt“ (vor allem
  Polizei); schließt die Meldung und ergänzt den Bericht.
- Alle Meldungen bleiben in der Akte nachlesbar (Reiter „Berichte“, Filter „Abgängigkeit“).

## Schweigepflichtentbindungen
Status: freigegeben (Bauplan Schritt 35; Auswahl Tobi 09.11.2026)
Datum: 09.11.2026
Was: In der Akte steht, wem gegenüber die Schweigepflicht aufgehoben ist und wem nicht. Eine
schnelle Abfrage beantwortet „Darf ich hier Auskunft geben?“.
Warum: Auswahl Tobi (09.11.2026). Am Telefon muss jeder sofort sehen, ob er Auskunft geben darf.
Umfang:
- Neuer Reiter **„Schweigepflicht“** nach „Kostenträger“.
- Liste je Person oder Stelle: Auswahl aus dem Personenverzeichnis (Betreuer, Angehörige, Ärzte,
  Forensik, FOSA, Schritt 29a), Kostenträger oder Freitext; **Zustand:** entbunden · **ausdrücklich keine Auskunft** · nicht
  erfasst; Umfang (alle Angaben, nur Gesundheit, nur Organisatorisches, Freitext); Richtung
  (Auskunft geben, einholen, beides); erteilt am; gültig bis oder unbefristet; widerrufen am;
  Form (schriftlich, mündlich mit Zeuge); Dokument aus „Dokumente“ verknüpft.
- **Auskunftsleiste bei jedem Bewohner** (Vorgabe Tobi, 09.11.2026: „Man muss mit einem Blick
  sehen, ob man etwas weitergeben darf oder nicht.“): im Akte-Kopf direkt unter dem
  Allergiehinweis, immer sichtbar, über allen Reitern. Je Person eine Marke mit Zeichen, Wort
  und Farbe:
  - „✓ Mutter · Erika Ahornblatt — entbunden (alle Angaben)“ in Offen-Farbe (Grün),
  - „✕ Bruder · … — keine Auskunft“ in Gefahr-Farbe (Rot),
  - am Ende immer: „Alle anderen: keine Auskunft“.
  Ist nichts eingetragen: „Schweigepflicht: nicht erfasst — keine Auskunft geben“ in
  Achtung-Farbe. Abgelaufene oder widerrufene Entbindungen zählen nicht als entbunden.
  Eingeschränkter Umfang steht in Klammern („nur Organisatorisches“).
- Dieselbe Kurzform als Spalte **„Auskunft“** in der Bewohnerliste der Übersicht und im Bereich
  „Bewohner“ (z. B. „2 entbunden · 1 keine Auskunft“, Tooltip mit den Namen).
- **Schnellabfrage** „Auskunft prüfen …“ (Akte und Aktionsleiste): Name eintippen → Antwort mit
  Wort und Farbe: „entbunden für …“, „keine Auskunft“, „nicht erfasst — keine Auskunft geben,
  bis geklärt“.
- Fristen und Widerruf: Marke „Entbindung endet …“ 4 Wochen vorher; widerrufene Einträge
  bleiben sichtbar (durchgestrichen, mit Datum).
- Das Programm bewertet nichts rechtlich; es zeigt, was eingetragen ist.
- Der Überleitungsbogen (Schritt 33) zeigt den Eintrag zur jeweiligen Klinik.

## Vitalwerte
Status: freigegeben (Bauplan Schritt 36; Auswahl Tobi 09.11.2026)
Datum: 09.11.2026
Was: Vitalwerte je Bewohner erfassen, als Tabelle und Verlauf ansehen und ausdrucken.
Warum: Wunsch Tobi (09.11.2026).
Umfang:
- Neuer Reiter **„Vitalwerte“** in der Akte (nach „Medikamentenplan“).
- **Werte** (Liste in den Einstellungen erweiterbar): Blutdruck (systolisch/diastolisch), Puls,
  Temperatur, Gewicht, Blutzucker, Sauerstoffsättigung. BMI wird aus Gewicht und Größe
  (Personenbeschreibung) berechnet. Kein Bauchumfang (Tobi, 09.11.2026: wird nicht benötigt).
- **Erfassen** über „Vitalwerte erfassen …“: Datum und Uhrzeit (vorbelegt), mehrere Werte in
  einem Schritt, Bemerkung (z. B. „nüchtern“), Kürzel automatisch. Korrektur nur als neue
  Fassung mit Grund.
- **Ansicht:** Tabelle (neueste oben) und Verlaufslinie je Wert für einen wählbaren Zeitraum.
- **Grenzen nur nach ärztlicher Anordnung:** Eine Fachkraft kann je Bewohner und Wert einen
  Bereich eintragen (mit anordnendem Arzt aus dem Ärzteverzeichnis und Datum). Nur dann zeigt
  das Programm „außerhalb des angeordneten Bereichs“ als Marke mit Wort in Gefahr-Farbe.
- **Allgemeine Richtwerte als Hinweis** (Entscheidung Tobi, 09.11.2026): Gilt für einen Wert keine
  ärztliche Anordnung, zeigt das Programm einen leisen Hinweis in Achtung-Farbe: „über dem
  allgemeinen Richtwert“ bzw. „unter dem allgemeinen Richtwert“, mit Tooltip „Allgemeiner
  Richtwert für Erwachsene, kein ärztlicher Grenzwert. Bei Unsicherheit Rücksprache halten.“
  Eine ärztliche Anordnung geht immer vor. Die Richtwerte stehen in den Einstellungen (änderbar
  mit Recht „Grenzen und Messaufträge festlegen“). **Vorbelegung** (allgemeine Lehrbuchwerte aus
  dem Chat, **nicht ärztlich geprüft** — so in `STAND.md` und in den Einstellungen vermerken):
  - Blutdruck: Hinweis ab 140 systolisch oder 90 diastolisch, unter 90 systolisch oder 60
    diastolisch (mmHg).
  - Puls in Ruhe: unter 60 oder über 100 pro Minute.
  - Temperatur: unter 36,0 °C; ab 37,5 °C „erhöht“, ab 38,0 °C „Fieber“.
  - Blutzucker: unter 70 mg/dl immer; mit Bemerkung „nüchtern“ ab 100 mg/dl, sonst ab 140 mg/dl.
    Einheit in den Einstellungen umschaltbar (mg/dl oder mmol/l).
  - Sauerstoffsättigung: unter 94 %.
  - BMI: unter 18,5; ab 25 „Übergewicht“; ab 30 „Adipositas“.
- **Messaufträge** je Bewohner (z. B. „Gewicht monatlich“, „Blutdruck wöchentlich“) erscheinen
  als Aufgabe im passenden Dienst (Aufgaben nach Schichten).
- Die letzten Werte erscheinen in der Visitenansicht und im Überleitungsbogen.
- Drucken: Verlauf eines Zeitraums für den Arztbesuch.
- Rechte: „Vitalwerte erfassen“ (Vorbelegung alle Mitarbeitenden), „Grenzen und Messaufträge
  festlegen“ (nur Fachkräfte).
- Demo-Daten: ausgedachte Werte über einige Monate mit leichten Schwankungen, bei einigen
  Bewohnern Messaufträge und angeordnete Bereiche.

## Personenverzeichnis (Ärzte, Betreuer, Angehörige, Forensik, FOSA)
Status: erledigt (09.11.2026, 0.33.4)
Datum: 09.11.2026
Was: Das Ärzteverzeichnis wird zum **Personenverzeichnis** und nimmt auch Betreuer, Angehörige
und die Zuständigen aus Forensik und FOSA auf.
Warum: Vorgabe Tobi (09.11.2026): Die Personendatenbank, in der aktuell nur Ärzte stehen, muss
um Angehörige und Betreuer erweitert werden. Für Forensik und FOSA gibt es immer Kontaktdaten zu
den Zuständigen, auch wegen der Medikamentenbestellung.
Umfang:
- **Ein eigenes Verzeichnis je Gruppe** (Vorgabe Tobi, 09.11.2026: „Seitenmenü links, ich klicke
  auf Ärzte und sehe eine Liste von allen Ärzten“): **„Verzeichnisse“ in den Einstellungen**
  (Vorgabe Tobi, 09.11.2026: „Verzeichnisse kommt in die Einstellungen hinein“), als eigene Gruppe
  im Seitenmenü der Einstellungen (Schritt 29). Darunter die Gruppen mit Zähler:
  **Ärzte · Betreuer · Angehörige · Forensik · FOSA · Sonstige Stellen**. Ein Klick zeigt rechts
  die Liste dieser Gruppe mit eigenen Spalten, Suchfeld und „Neu …“ in der Aktionsleiste:
  - Ärzte: Name, Fachrichtung, Praxis, Telefon, E-Mail, zugeordnete Bewohner (Anzahl).
  - Betreuer: Name, Art (Berufsbetreuer, ehrenamtlich …), Büro, Telefon, E-Mail, Bewohner.
  - Angehörige: Name, Telefon, Mobil, zugeordnet zu (Bewohner mit Beziehung).
  - Forensik und FOSA: Name, Funktion, Klinik bzw. Ambulanz, Telefon, E-Mail, Bewohner.
  - Sonstige Stellen: Name, Stelle, Telefon, E-Mail.
  Doppelklick öffnet die Person; dort „zugeordnet zu …“ mit Sprung in die Akte.
- Technisch darf es eine gemeinsame Tabelle mit dem Feld „Gruppe“ bleiben (einfacher); für den
  Nutzer sind es getrennte Verzeichnisse. Das bisherige „Ärzteverzeichnis“ in den Einstellungen
  wird zu „Verzeichnisse → Ärzte“. Kein eigener Bereich in der Seitenleiste des Programms.
- Ansehen dürfen alle (Telefonnummern nachschlagen); ändern nur mit Recht (siehe unten). Wer kein
  Recht für andere Teile der Einstellungen hat, sieht dort nur „Verzeichnisse“ (Schritt 29c).
- Felder: Name, Art, Stelle (Praxis, Büro, Klinik, Ambulanz), **Funktion** (bei Forensik und
  FOSA: Psychologe/Psychologin, Arzt/Ärztin, Pflege, Sozialdienst; bei Ärzten die Fachrichtung),
  Telefon, Mobil, E-Mail (Demo nur `@example.org`), Anschrift, Erreichbarkeit, Hinweis,
  aktiv/inaktiv. Nicht löschen, solange die Person irgendwo eingetragen ist.
- **Zuordnung zu Bewohnern mit Rolle** (eine Person, viele Bewohner — z. B. ein Berufsbetreuer
  für mehrere Bewohner): Rolle je Bewohner, z. B. „Mutter“, „Betreuer“, „zuständige
  Psychologin Forensik“. Im Verzeichnis steht bei jeder Person „zugeordnet zu …“.
- Überall Auswahl statt Freitext, wie bisher bei den Ärzten; „Person anlegen …“ direkt aus der
  Auswahl. Angehörige darf anlegen, wer „Bewohner anlegen und bearbeiten“ hat; das übrige
  Verzeichnis, wer „Personenverzeichnis verwalten“ hat (bisher „Ärzteverzeichnis verwalten“,
  gleiche Vorbelegung).
- Vorhandene Daten übernehmen: Ärzte bleiben, die Betreuer aus den Stammdaten (seit 0.31.0)
  werden Personen der Art „Betreuer“ mit Zuordnung.
- Demo: ausgedachte Betreuer (einige betreuen mehrere Bewohner), Angehörige für etwa die Hälfte
  der Bewohner, je eine ausgedachte Forensik-Klinik und FOSA mit Zuständigen.

## Zuständigkeit Forensik und FOSA, Bestellweg der Medikamente
Status: freigegeben (Bauplan Schritt 30a; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: In der Akte steht, wer für einen Bewohner zuständig ist: der Betreuer, die Forensik oder
die FOSA (Forensische Ambulanz). Danach richtet sich auch, über wen die Medikamente bestellt
werden.
Warum: Vorgabe Tobi (09.11.2026): Bewohner aus der Forensik haben zwar einen Betreuer, solange sie
noch über die Forensik laufen, ist der Betreuer aber nicht zuständig. Zuständig sind aus der
Forensik ein Psychologe, ein Arzt und Pflegepersonal; es gibt immer Kontaktdaten, auch wegen der
Medikamentenbestellung. Forensiker und andere Bewohner werden zum Teil durch die FOSA
nachbetreut; dann laufen auch die Medikamentenbestellungen über die FOSA.
Umfang:
- Reiter „Betreuung“ (Schritt 30), oben der Abschnitt **Zuständigkeit** mit zwei Angaben:
  - **Forensik:** ja · nein · nicht erfasst. Bei „ja“: Klinik, seit, Status (Freitext, z. B.
    „Langzeitbeurlaubung“), optional „nächste Überprüfung“ (Marke 8 Wochen vorher), und die
    **Zuständigen** aus dem Personenverzeichnis: Psychologe/Psychologin, Arzt/Ärztin, Pflege
    (mehrere möglich), je mit Telefon und E-Mail sichtbar.
  - **FOSA-Nachsorge:** ja · nein · nicht erfasst. Bei „ja“: Ambulanz, seit, bis (optional) und
    die Zuständigen der FOSA. Gilt auch für Bewohner, die nicht (mehr) in der Forensik sind.
- **Betreuer bei Forensik:** Der Betreuer bleibt eingetragen, ist aber mit Wort gekennzeichnet:
  „nicht zuständig, solange Forensik“ (ausgegraut). Als erste Ansprechpartner stehen dann die
  Zuständigen der Forensik. Bei FOSA ohne Forensik bleibt der Betreuer zuständig (einfachere
  Annahme; Tobi kann sie ändern).
- **Marken** „Forensik“ bzw. „FOSA“ (Hinweis-Farbe, mit Wort) im Akte-Kopf und in den
  Bewohnerlisten.
- **Bestellweg der Medikamente** je Bewohner: Forensik · FOSA · Hausarzt/Apotheke · nicht
  erfasst. Vorschlag nach Zuständigkeit (Forensik → Forensik, sonst FOSA → FOSA, sonst
  Hausarzt/Apotheke), änderbar, mit Ansprechperson aus dem Personenverzeichnis.
  - Im Medikamentenplan oben: „Bestellung über: FOSA · <Name>, <Telefon>“.
  - Knopf **„Bestellung per E-Mail …“**: öffnet das E-Mail-Programm (`mailto:`, `CLAUDE.md`
    Regel 6) mit Empfänger, Betreff „Medikamentenbestellung <Name>, geb. <Datum>“ und den im
    Dialog angekreuzten Medikamenten aus dem Plan (Name, Stärke, Form).
- Überleitungsbogen (Schritt 33) und Abgängigkeitsmeldung (Schritt 34) nehmen Zuständigkeit und
  Zuständige auf.
- Demo-Daten: je Bereich zwei Bewohner mit Forensik, drei mit FOSA-Nachsorge (davon einer
  zusätzlich Forensik); ausgedachte Klinik „Forensische Klinik Musterstadt“ und „FOSA
  Musterstadt“.

## Bereich „Dokumentation“: alle Berichte an einem Ort, kompakt, mit Filter
Status: erledigt (09.11.2026, 0.33.5; Bauplan Schritt 29b, Entwurf 1 „Zeilen nach Tagen“ mit Änderungen, Tobi 09.11.2026)
Datum: 09.11.2026
Was: Alle Berichte stehen im Bereich **„Dokumentation“**. Links eine Seitenleiste mit den
Bewohnern, rechts die Berichte: viele auf einmal, trotzdem übersichtlich, mit Filter. Der Reiter
„Berichte“ in der Akte entfällt.
Warum: Vorgabe Tobi (09.11.2026): Den Reiter „Berichte“ braucht es nicht bei jedem Bewohner extra.
Unter „Dokumentation“ sollen alle Berichte stehen; in einer Seitenleiste kann man jeden Bewohner
anklicken und sieht alle Berichte zu ihm. Es sollen viele Berichte auf einmal zu sehen sein,
ohne dass es unübersichtlich wird, und es braucht eine Filterfunktion für besondere Berichte.
Umfang:
- **Seitenleiste des Programms:** „Bewohner“ heißt künftig **„Dokumentation“** (gleiche Stelle).
  Sonst gäbe es wieder zwei fast gleiche Bereiche (Rückmeldung Tobi zu Übersicht/Bewohner,
  08.11.2026). Die Akte öffnet man weiter per Doppelklick in Übersicht und Dokumentation oder
  über „Akte öffnen“.
- **Links die Bewohner-Seitenleiste** (schmal, etwa 240 px, einklappbar). Ergänzung Tobi
  (09.11.2026): **kein Eintrag „Alle Bewohner“**; aufgeführt wird nur jeder Bewohner mit
  **„Nachname, Vorname“**, alphabetisch nach Nachname, ohne Zimmer, Zähler oder Marken.
  Darüber Bereichswahl und Suchfeld. Pfeiltasten blättern, die Berichte wechseln mit. Beim
  Öffnen ist der erste Bewohner gewählt (bzw. der zuletzt gewählte).
- **Rechts die Berichte als dichte Liste** — Entwurf 1 „Zeilen nach Tagen“ (Auswahl Tobi,
  09.11.2026), mit diesen Änderungen:
  - **Wochentage deutlich abgegrenzt:** Tagesüberschrift als eigener Balken über die ganze
    Breite (heller Akzent 238, 242, 251, Radius 6 px, Schrift 11,5 pt Semibold, z. B. „Montag,
    09.11.2026 · 3 Berichte“), mit 12–16 px Abstand zum vorigen Tag. Neueste oben.
  - **Keine Schicht-Marke.** Stattdessen ist **jeder Bericht in der Farbe seiner Schicht
    umrandet** (1,5 px, Radius 8 px): Früh Orange (238, 166, 60), Spät Blau (45, 84, 156),
    Nacht Violett (91, 58, 153) — dieselben Farben wie in der Übergabe (29h). Das gilt auch für
    Visitenberichte. Damit Farbe nicht allein steht: unter der Uhrzeit klein das Wort „Früh“,
    „Spät“ bzw. „Nacht“ in Schichtfarbe, und ein Farbschlüssel „Rand: Früh · Spät · Nacht“ in
    der Filterleiste.
  - **Nur drei Marken bleiben:** „Wichtig“ (Gefahr-Fläche hell), **„Medikation geändert“**
    (kräftiges Rot, weiße Schrift, Zeile leicht rot hinterlegt) und neu **„Bedarfsgabe“**
    (Hinweis-Farbe). Keine Marken mehr für Übergabe, Berichtsart oder Lebensbereich in der
    Liste (die Angaben bleiben gespeichert und filterbar).
  - **Bedarfsgabe:** Jeder Bericht, der über „Bedarf gegeben …“ (Medikamentenplan, seit 0.33.0)
    entsteht, trägt die Marke automatisch. In der Aktionsleiste der Dokumentation zusätzlich
    „Bedarf gegeben …“ für den gewählten Bewohner (gleiches Recht wie im Medikamentenplan).
  - **Visiten** erkennt man am fetten Anfang „Visite Psychiater – Dr. …“ bzw. „Visite Hausarzt –
    Dr. …“ (keine eigene Marke).
  - Eine Zeile: Uhrzeit (darunter das Schichtwort) · Marken · Text · Kürzel.
  - Der Text zeigt **höchstens zwei Zeilen**, dann „… mehr“. Ein Klick klappt den Bericht in
    der Liste auf (kein neues Fenster). Umschalter in der Aktionsleiste: **„kompakt“** (zwei
    Zeilen, Vorgabe) · **„vollständig“** (ganzer Text).
  - Nachträge stehen eingerückt direkt unter ihrem Bericht.
  - „Wichtig“ bekommt am Zeilenanfang ein Wort-Zeichen in Gefahr-Farbe, „Übergabe“ in
    Hinweis-Farbe; der übrige Bericht bleibt neutral (Farbe sparsam, `docs/gestaltung.md`).
  - Richtwert: Bei 1920 × 1080 mindestens 15 Berichte im Kompaktmodus ohne Bildlauf.
- **Filterleiste über der Liste** (gilt für den gewählten Bewohner; bleibt beim Wechsel des
  Bewohners eingestellt):
  - Zeitraum: Heute · 7 Tage (Vorgabe) · 30 Tage · frei wählbar.
  - Knöpfe: Wichtig · Medikation geändert · Bedarfsgabe · Visite; dazu „Übergabe“ und „mit
    Nachtrag“ in den weiteren Filtern.
  - **Berichtsart** (neu, siehe unten), Lebensbereich, Schicht, Verfasser (Kürzel).
  - Suche im Text mit Hervorhebung der Fundstellen.
  - Gesetzte Filter stehen als kleine Marken mit „×“ über der Liste, dazu „Filter zurücksetzen“.
  - **Gespeicherte Filter** mit eigenem Namen (z. B. „Krisen der letzten 30 Tage“), als Knöpfe
    oben in der Filterleiste.
- **Berichtsart (neu)** beim Schreiben, eine Auswahl, Vorgabe „Alltag“: Alltag · Krise ·
  Arztkontakt · Visite · Medikation · Konflikt · Ausgang/Abgängigkeit · Angehörigenkontakt ·
  Betreuer/Behörde · Besonderes Vorkommnis. Liste in den Einstellungen änderbar. Berichte aus
  Visite und Abgängigkeitsmeldung bekommen ihre Art automatisch. Vorhandene Demo-Berichte
  erhalten passende Arten.
- **Aktionsleiste:** „Neuer Bericht …“ (Hauptaktion, für den gewählten Bewohner), „Bedarf gegeben …“, „Akte öffnen“, kompakt/vollständig, „Drucken …“ (druckt, was der
  Filter zeigt).
- **Akte:** Der Reiter „Berichte“ entfällt. Im Akte-Kopf ein Knopf „Berichte ansehen“, der die
  Dokumentation mit diesem Bewohner öffnet. HEB, Reflexion, Visite und Übergabe lesen die
  Berichte weiter wie bisher.
- Gilt anstelle der rechten Seite aus Schritt 26j (Bewohnerliste mit Berichten); die Liste links
  wird zur schmalen Seitenleiste.

## Rechte: Fehlendes ausblenden statt ausgrauen
Status: erledigt (09.11.2026, 0.33.6; Bauplan Schritt 29c, Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Was ein Mitarbeiter nicht darf, sieht er nicht.
Warum: Vorgabe Tobi (09.11.2026): Elemente, für die der Mitarbeiter keine Berechtigung hat, sollen
ausgeblendet werden. Viele Kollegen sind technisch wenig bewandert; das Programm muss übersichtlich
bleiben.
Umfang:
- Knöpfe, Menüeinträge, Reiter, Bereiche der Seitenleiste und Abschnitte der Einstellungen ohne
  Recht werden **ausgeblendet**, nicht ausgegraut. Die übrigen rücken zusammen, keine Lücken.
- Ein Bereich ohne jedes Recht verschwindet aus der Seitenleiste.
- Ausgegraut bleibt nur, was der Mitarbeiter darf, aber gerade nicht geht (z. B. „Nächster
  Bewohner ▶“ in der Visite ohne Bericht), mit Hinweis beim Darüberfahren.
- Nach An- und Abmelden sofort neu aufbauen.
- Test: je Position ein Bildschirmfoto der Seitenleiste und einer Akte.

## Wohnbereich einmal in der Übersicht wählen, gilt überall
Status: erledigt (09.11.2026, 0.33.7; Bauplan Schritt 29d, Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Der Wohnbereich wird an einer Stelle gewählt, in der Übersicht. Alle Bereiche des Programms
zeigen danach nur diesen Wohnbereich.
Warum: Vorgabe Tobi (09.11.2026): In der Übersicht soll der Wohnbereich eingestellt werden können,
damit Kollegen auf einem anderen Bereich einspringen können. „Stelle ich hier auf R1, sehe ich
alles, was die R1 betrifft.“ Die Auswahl in den einzelnen Fenstern wird dadurch überflüssig.
Umfang:
- In der Übersicht gut sichtbar oben: **„Wohnbereich: R1 · R2 · R3“** als drei große
  Umschaltknöpfe (kein Klappmenü). **Wer welche Knöpfe sieht** (Ergänzung Tobi, 09.11.2026):
  - **Personal der Wohnbereiche:** R1 · R2 · R3 — alle Wohnbereiche, damit man einspringen kann.
    **Kein Knopf „Therapie“**, damit vom Wohnbereich aus keine Therapien geändert werden.
  - **Therapie:** R1 · R2 · R3 · **Therapie** — die Therapie kann auf den Wohnbereichen
    einspringen. Dort arbeitet sie mit den Rechten ihrer Position; Therapieangebote ändert sie nur
    im Bereich Therapie.
  - **Leitung und Administrator:** alle Knöpfe und zusätzlich „Alle“.
  - Die Knöpfe hängen am Recht (Rechte-Tabelle: „Wohnbereiche“, „Bereich Therapie“, „Alle
    Bereiche“), Vorbelegung wie oben; ohne Recht ist ein Knopf ausgeblendet (Schritt 29c).
- Die Wahl gilt für **alle Bereiche**: Dokumentation, Übergabe, Tagesplan, Visiten, Aufgaben,
  Kalender (Bereichskalender), Therapie (Laufende Therapien), Ausgangsliste, Nachrichten an Teams.
- Die **Bereichswahl in den einzelnen Fenstern entfällt** (z. B. Dokumentation, Übersicht-Liste,
  Ausgangsliste, Visite).
- Der gewählte Bereich steht immer in der kleinen Zeile der Kopfzeile („R1 · Dokumentation“),
  damit man nie vergisst, wo man gerade arbeitet. Ist es nicht der Bereich des Rechners, zusätzlich
  die Marke „eingesprungen“ (Hinweis-Farbe, mit Wort).
- Vorgabe beim Start: Bereich des Rechners (Bereichskennung). Die Wahl gilt bis zum Abmelden; nach
  dem Abmelden wieder die Vorgabe.
- Rechte bleiben unverändert.

## Nachrichten: Kuvert in der Kopfzeile statt Hinweis in jedem Fenster
Status: freigegeben (Bauplan Schritt 29e; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Ungelesene Nachrichten zeigt ein Kuvert in der Kopfzeile an, nicht mehr ein Hinweis in den
Fenstern.
Warum: Vorgabe Tobi (09.11.2026): Es muss nicht in jedem Fenster die Info über Nachrichten kommen;
besser in der Kopfzeile ein Kuvert mit rotem Ausrufezeichen.
Umfang:
- Kopfzeile, links neben „Angemeldet: …“: **Kuvert-Symbol** (Segoe Fluent Icons). Bei ungelesenen
  Nachrichten mit rotem Ausrufezeichen und Zahl („3“), Tooltip „3 ungelesene Nachrichten“. Ohne
  ungelesene: Kuvert ohne Zeichen, leise. Klick öffnet „Nachrichten“.
- Der Hinweis über den Listen (z. B. im Bereich „Bewohner“/„Dokumentation“) entfällt überall.
- Der Zähler an „Nachrichten“ in der Seitenleiste bleibt.
- Sonst bleibt die Kopfzeile wie in Schritt 26i.

## Kleine Korrekturen (Demo-Übergaben, Angebot, Dokumentation)
Status: freigegeben (Bauplan Schritt 29f; Vorgaben Tobi 09.11.2026)
Datum: 09.11.2026
Umfang:
- **Demo-Daten:** Berichte wie „Keine Besonderheiten“ oder „Ruhiger Dienst“ bekommen nie
  „Übergabe“ (Tobi: „Keine Besonderheiten ist nicht übergaberelevant“). Übergaben in den
  Demo-Daten nur bei Texten, die wirklich etwas für die nächste Schicht enthalten.
- **Therapie → „Neues Angebot“:** Das Feld heißt **„Teilnehmer“**, darunter klein in Klammern
  „(Anzahl)“; nicht mehr „höchstens“.
- **Dokumentation/Bewohner:** Die Aufgaben-Kurzliste (und der Nachrichten-Hinweis, Schritt 29e)
  über der Liste entfällt (Tobi: „Die Aufgaben müssen nicht unter Dokumentation angezeigt
  werden“).
- **Bewohnerleiste der Dokumentation:** ohne Hinweise, schmal und übersichtlich, in einer Fläche
  mit abgerundeten Ecken (8 px) wie das übrige Programm; die gewählte Zeile ebenfalls abgerundet
  hervorgehoben (heller Akzent, `docs/gestaltung.md`). Gilt schon für die heutige Liste, nicht erst
  mit Schritt 29b.

## Tagesplan im normalen Programmstil, kleiner und übersichtlicher
Status: freigegeben (Bauplan Schritt 29g; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Der Tagesplan verliert die dunkle Flughafen-Optik und bekommt das Aussehen des übrigen
Programms.
Warum: Rückmeldung Tobi (09.11.2026): „Wie am Flughafen“ war zu wörtlich genommen. Das Design
bleibt wie im restlichen Programm, die Liste etwas kleiner und übersichtlicher.
Umfang:
- Helle Fläche, Schrift und Farben wie überall (`docs/gestaltung.md`); keine dunkle Tafel, keine
  Großschrift, keine feste Zeichenbreite.
- Was bleibt (die Idee, nicht die Optik): oben „Jetzt und gleich“, darunter „Später heute“, unten
  „Erledigt“ (blasser); Status als Wort mit Farbe; Knöpfe „gegangen“, „abgesagt“, „ausgefallen“.
- Eine Zeile je Termin, normale Zeilenhöhe der Listen im Programm; Spalten: Zeit · Bewohner ·
  Termin · Ort · Begleitung · Status.
- Uhrzeit und Datum oben rechts in normaler Größe.

## Übergabe nach Schichten mit eigener Umrandung
Status: freigegeben (Bauplan Schritt 29h; Vorgabe Tobi 09.11.2026; ergänzt: „Die Übergabeseite muss
effektiver gestaltet werden“)
Datum: 09.11.2026
Was: Die Übergabe ist nach Schichten unterteilt; jede Schicht hat eine eigene farbige Umrandung.
Warum: Vorgabe Tobi (09.11.2026).
Umfang:
- Drei Abschnitte untereinander, je mit Überschrift und Zähler: **Frühdienst · Spätdienst ·
  Nachtdienst** (Schicht, in der der Eintrag entstanden ist). Die laufende Schicht steht oben,
  dann die vorherige usw.
- Jeder Abschnitt in einer Fläche mit **farbiger Umrandung** (2 px) und farbigem Wort in der
  Überschrift — Farbe nie allein: Früh **Orange** (238, 166, 60), Spät **Blau** (45, 84, 156),
  Nacht **Violett** (91, 58, 153). Statt einer Schicht-Marke ist jeder Eintrag in der Farbe
  seiner Schicht umrandet, wie die Berichte in der Dokumentation (29b), auch in der Übersicht
  unter „Heute wichtig“.
- Einträge „bis erledigt“ bleiben in ihrer Ursprungsschicht und tragen „seit <Tag>“.
- **Effektiver (Ergänzung 09.11.2026)** — heute ist jeder Eintrag eine große Karte; bei
  1366 × 768 passen nur zwei von neun auf den Bildschirm (`STAND.md`, offene Punkte):
  - **Kompakte Zeilen** wie in der Dokumentation: Bewohner (Nachname, Vorname, Zimmer) ·
    Übergabe-Satz bzw. die ersten zwei Zeilen des Berichts · gültig bis · Kürzel · Knöpfe.
    Ziel: mindestens 12 Einträge bei 1366 × 768 ohne Bildlauf.
  - **Reihenfolge innerhalb der Schicht:** Medikamentenänderungen (29l) zuerst, dann „Wichtig“,
    dann der Rest nach Zimmer. Mehrere Einträge eines Bewohners stehen untereinander unter
    einem Namen.
  - **Vorgabe „nur ungelesene“** (von mir noch nicht als gelesen bestätigt), umschaltbar auf
    „alle offenen“; Zähler an beiden.
  - **Ein Klick je Handgriff:** „Gelesen“ und „Erledigt“ als kleine Knöpfe mit Wort direkt in
    der Zeile. Nach „Gelesen“ rutscht die Zeile leise weg (bei „nur ungelesene“). Ein Klick auf
    den Text klappt den ganzen Bericht in der Zeile auf (kein Meldungsfenster mehr).
    „Verlängern …“ beim aufgeklappten Eintrag.
  - **„Alle in diesem Abschnitt gelesen“** je Schicht, mit einer Rückfrage („9 Einträge als
    gelesen bestätigen?“).
  - „Wohnbereich allgemein“ als eine kompakte Zeile oben, aufklappbar.
  - **„Übergabeliste drucken“**: alle offenen Einträge auf einer A4-Seite, nach Schicht.

## Wochenplan ohne Datum
Status: freigegeben (Bauplan Schritt 29i; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Der Wochenplan ist ein fester Plan Montag bis Sonntag für den ganzen Aufenthalt, ohne Datum.
Warum: Vorgabe Tobi (09.11.2026): Der Wochenplan gilt für den gesamten Zeitraum des Aufenthaltes;
ein festes Datum und „Diese Woche“ sind überflüssig.
Umfang:
- Keine Datumsangaben in den Spaltenköpfen (nur „Montag“ … „Sonntag“), kein Wochenwechsel, kein
  Knopf „Diese Woche“.
- Änderungen gelten weiter „ab Datum …“ im Hintergrund (Verlauf); angezeigt wird der heute
  gültige Plan, darüber klein „gültig seit <Datum der letzten Änderung>“ und „Verlauf …“.
- Teilnahme je Tag (gegangen, abgesagt) steht nicht im Wochenplan, sondern im Tagesplan.
- Ausdruck ebenfalls ohne Datum.

## Ausgangsliste: alle Bewohner auf einen Blick, ohne Unterbringung
Status: freigegeben (Bauplan Schritt 29j; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die Ausgangsliste zeigt alle Bewohner des Bereichs auf einmal, ohne Angaben zur Unterbringung.
Warum: Vorgabe Tobi (09.11.2026): Die Anzeige der Unterbringung ist hier überflüssig; man muss alle
Bewohner auf einmal im Blick haben.
Umfang:
- Unterbringung, Beschluss und Fristen erscheinen in der Liste der Ausgangsliste nicht mehr (sie
  bleiben in der Akte und im Unterbringungs-Fenster).
- **Alle 23 Bewohner ohne Bildlauf:** bei 1920 × 1080 in einer Liste; reicht die Höhe nicht
  (z. B. 1366 × 768), in zwei Spalten nebeneinander. Zeile: Zimmer · Name · Status (im Haus,
  Ausgang bis …, abwesend …) · Rückkehrzeit.
- Knöpfe (Ausgang eintragen, zurück) bleiben in der Zeile bzw. per Doppelklick.
- Abgleich mit der eigenständigen Ausgangsliste: Änderung in
  `src/DokuLine.Modul.Ausgangsliste/AENDERUNGEN.md` vermerken.

## Visite: Psychiater und Hausarzt getrennt, Berichte in der Dokumentation
Status: freigegeben (Bauplan Schritt 29k; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die Visitenseite unterscheidet Psychiater-Visite und Hausarzt-Visite. Visitenberichte stehen
hervorgehoben in der Dokumentation.
Warum: Vorgabe Tobi (09.11.2026): Berichte aus Visiten müssen in die Dokumentation übernommen und
hervorgehoben werden. Auf der Visitenseite gibt es eine Unterteilung zwischen Psychiater und
Hausarzt.
Umfang:
- Visitenseite oben zwei Reiter bzw. Umschalter: **Psychiater** · **Hausarzt**. Gewählt wird dann
  der Arzt; die Liste zeigt die Bewohner, denen dieser Arzt als Psychiater bzw. Hausarzt zugeteilt
  ist (Reiter „Ärzte“, Schritt 30; bis dahin alle Bewohner des Bereichs). „Weitere Bewohner …“
  nimmt andere dazu.
- Jeder Visitenbericht ist ein Bericht in der Dokumentation mit Berichtsart **„Visite“** und
  Zusatz „Psychiater“ bzw. „Hausarzt“ und Arztname. In der Berichtsliste hervorgehoben durch den
  fetten Anfang „Visite Psychiater – Dr. …“ und den Rand in Schichtfarbe wie alle Berichte (keine
  eigene Marke, Ergänzung Tobi 09.11.2026). Filter „Visite“.
- Medikamentenänderungen aus der Visite lösen Schritt 29l aus.

## Medikamentenänderung auffällig markieren und in die Übergabe
Status: freigegeben (Bauplan Schritt 29l; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Jede Änderung am Medikamentenplan ist sofort und auffällig sichtbar und geht automatisch in
die Übergabe.
Warum: Vorgabe Tobi (09.11.2026): Gibt es bei einem Bewohner eine Medikamentenänderung, muss das
auffällig markiert werden; sehr wichtig für die Übergabe.
Umfang:
- Als Änderung zählt: neues Medikament, abgesetzt, Dosis oder Uhrzeit geändert, Pause, Stufe eines
  Stufenplans, neue Bedarfsmedikation.
- **Marke „Medikation geändert“** (Gefahr-Farbe mit Wort) für 7 Tage: im Akte-Kopf, in der
  Bewohnerliste der Übersicht, in der Visite und im Medikamentenplan. Im Medikamentenplan ist die
  geänderte Zeile selbst hervorgehoben („neu seit 09.11.“, „Dosis geändert: 5 mg → 10 mg“).
- **Automatischer Übergabe-Eintrag** „Medikamentenänderung: <was> ab <Datum>, angeordnet von
  <Arzt>“ mit Kennzeichen „Wichtig“. Er bleibt in der Übergabe, bis **jede der drei Schichten** ihn
  mit Kürzel als gelesen bestätigt hat (höchstens 7 Tage).
- In „Heute wichtig“ der Übersicht ein eigener Kasten „Medikamentenänderungen“, wenn es welche
  gibt.

## Schnellerer Wechsel zwischen den Bereichen
Status: freigegeben (Bauplan Schritt 29m; Rückmeldung Tobi 09.11.2026)
Datum: 09.11.2026
Was: Der Wechsel zwischen den Bereichen soll sofort und ohne Flackern gehen.
Warum: Rückmeldung Tobi (09.11.2026): Der Wechsel zu manchen Fenstern ist sehr zaghaft; er muss
schneller und sauber gehen.
Umfang:
- **Zuerst messen:** Zeit je Wechsel in `Hauptfenster.BereichWaehlen` mit `Stopwatch` messen und
  im Protokoll ausgeben (nur bei `--messen`); Ergebnis je Bereich in `STAND.md`. Zielwert: unter
  150 ms bis zur fertigen Anzeige.
- Verdächtige Stellen aus der Durchsicht im Chat (09.11.2026, nicht gemessen):
  - Jeder Wechsel ruft `Aktualisieren()`/`Zeigen()` auf und liest alles neu aus der Datenbank,
    auch wenn sich nichts geändert hat. Besser: Seite zeigen, neu lesen nur, wenn sich die Daten
    geändert haben (Änderungszähler aus dem Mehrplatz-Abgleich) oder Tag/Schicht gewechselt hat.
  - `Einstellungen.Laden()` liest bei jedem Aufruf die Datei von der Platte (in vielen Seiten
    mehrfach). Einmal laden, im Speicher halten, nach Änderung neu laden.
  - Listen werden nur an wenigen Stellen mit `BeginUpdate`/`EndUpdate` gefüllt; Aufbau ohne
    `SuspendLayout`/`ResumeLayout` löst viele Neuberechnungen aus.
  - `Visible` aller Seiten wird bei jedem Wechsel gesetzt; nur alte ausblenden, neue einblenden.
  - Eigene Zeichnungen (Kopf mit Verlauf, Karten) bei jedem Neuzeichnen neu berechnen; Verlauf
    einmal als Bild zwischenspeichern, neu nur bei Größenänderung.
  - Gegen Flackern: Doppelpufferung für die ganze Fläche (z. B. `WS_EX_COMPOSITED` für den
    Inhaltsbereich), sofern es messbar hilft.
- Nichts am Verhalten ändern; vorher/nachher messen und in `STAND.md` festhalten.

## Therapie als eigener Bereich (Dokumentation, Nachrichten, Aufgaben, Wochenpläne)
Status: freigegeben (Bauplan Schritt 29n; Vorgaben Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die Therapie ist ein eigener Bereich neben R1, R2 und R3, mit einer schlanken, eigenen
Seitenleiste.
Warum: Vorgaben Tobi (09.11.2026): Die Ansicht für die Therapie anders gestalten, da hier weniger
Infos nötig sind als auf den Wohnbereichen. „Therapie als eigenen Bereich machen. Sie brauchen auch
den Bereich Dokumentation, Nachrichten, Aufgaben und eine extra Seite, in der sie eine
Bewohnerliste haben; mit Klick auf den Bewohner sieht man den Wochenplan.“
Umfang:
- **Bereich „Therapie“** in der Bereichswahl der Übersicht (Schritt 29d): R1 · R2 · R3 ·
  **Therapie**. Der Knopf erscheint nur mit dem Recht „Bereich Therapie“ (Vorbelegung: Position
  Therapie, Leitungen, Administrator); das Personal der Wohnbereiche sieht ihn nicht (Schritt 29d). Rechner der Therapie haben „Therapie“ als
  Bereichskennung und starten dort.
- **Seitenleiste im Bereich Therapie** (nur diese Einträge, in dieser Reihenfolge):
  1. **Laufende Therapien** (Startseite, wie Schritt 26m)
  2. **Angebote** (bisheriger Reiter „Angebote“)
  3. **Wochenpläne** (neu, siehe unten)
  4. **Dokumentation** (wie Schritt 29b)
  5. **Aufgaben** (Aufgaben des Teams Therapie)
  6. **Nachrichten** (Team Therapie und eigene)
  7. **Einstellungen** (soweit Rechte; darin die Verzeichnisse)
  Übersicht, Übergabe, Visiten, Medikamentenplan und Ausgangsliste erscheinen dort nicht.
- **Bewohner im Bereich Therapie:** alle Bewohner aus R1–R3, die mindestens ein Angebot der
  Therapie haben; in den Bewohnerleisten nach Wohnbereich gruppiert (kleine Überschrift „R1“,
  „R2“, „R3“), darunter „Nachname, Vorname“. Schalter „alle Bewohner des Hauses“ für die übrigen.
- **Neue Seite „Wochenpläne“:** links die Bewohnerleiste (wie in der Dokumentation, schmal,
  abgerundet), rechts beim Klick der **Wochenplan** des Bewohners (Montag bis Sonntag, ohne Datum,
  Schritt 29i). Angebote der Therapie sind hervorgehoben; ändern kann die Therapie nur ihre eigenen
  Angebote (Schritt „Therapien: Zuordnung und Rechte“), alles andere ist nur zu lesen. Drucken.
- **Dokumentation der Therapie:** Berichte aus dem Bereich Therapie bekommen die Berichtsart
  **„Therapie“** (neu in der Liste der Berichtsarten, 29b) und den Namen des Angebots. Sie stehen
  in derselben Dokumentation des Bewohners und sind damit auch im Wohnbereich zu lesen. Die
  Therapie sieht die Berichte der Wohnbereiche ebenfalls (lesen), soweit die Rechte es erlauben.
- **Laufende Therapien in der Therapie-Fassung** ohne Angaben, die nur der Wohnbereich braucht
  (Bezugspflege, Marken wie Beschluss oder Medikation): je Teilnehmer Name, Bereich, Teilnahme.
- **Aufgaben und Nachrichten:** neues Team „Therapie“ als Empfänger; Aufgaben ohne
  Schichtbezug (die Therapie arbeitet tagsüber), mit Fälligkeitsdatum.
- Demo: zwei ausgedachte Mitarbeitende der Position Therapie, Aufgaben und Nachrichten für das
  Team Therapie, Therapie-Berichte bei mehreren Bewohnern.

## Therapie-Anzeige für den Fernseher im Wohnbereich
Status: freigegeben (Bauplan Schritt 29o; Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die laufenden Therapien lassen sich über das Netzwerk auf einem Fernseher im Wohnbereich
zeigen, damit die Bewohner sehen, was gerade läuft.
Warum: Vorgabe Tobi (09.11.2026): Den Plan über das Netzwerk an einen Fernseher senden, um den
Bewohnern im Wohnbereich eine aktuelle Einblendung der laufenden Therapien zu zeigen. Nur die
Namen der Bewohner des Wohnbereichs; hat kein Bewohner der R3 eine Therapie, wird sie für die R3
nicht angezeigt. „Noch offen“ nur für das Personal.
Umfang:
- **Weg:** DokuLine stellt im **lokalen Netzwerk** eine schlichte, nur lesende Seite bereit
  (eingebauter kleiner Webserver von .NET, keine neue Abhängigkeit). Der Fernseher öffnet sie im
  eigenen Browser bzw. über einen Streaming-Stick mit Browser, z. B.
  `http://<Rechner>:8080/anzeige/R3?code=<Zugangscode>`. Ohne Netzwerk-Fernseher geht dieselbe
  Anzeige als Vollbildfenster auf einem zweiten Bildschirm (HDMI).
- **Absicherung** (`CLAUDE.md`, Regel 6, Ergänzung 09.11.2026): nur Adressen aus privaten
  Netzbereichen (LAN), keine Verbindung ins Internet; Zugangscode je Wohnbereich (in den
  Einstellungen, neu erzeugbar); nur lesend; Port einstellbar; **standardmäßig aus**, Einschalten
  mit Recht „Anzeige verwalten“. Beim ersten Start fragt die Windows-Firewall nach (im README
  erklären).
- **Inhalt je Wohnbereich:** nur Therapien, an denen mindestens ein Bewohner dieses Bereichs
  teilnimmt; je Therapie Name, Uhrzeit von–bis, Ort und die Namen der Teilnehmer **nur aus diesem
  Bereich**. Keine Zimmer, keine Marken, keine Bezugspflege, kein Teilnahmestatus („noch offen“,
  „abgesagt“ sieht nur das Personal im Programm).
- **Namen:** Vorgabe „Vorname und erster Buchstabe des Nachnamens“ (Datenschutz, auf dem
  Fernseher sehen es auch Besucher); in den Einstellungen umschaltbar auf „voller Name“.
- Aussehen: helles Programmdesign, gut lesbar auf Abstand (große Schrift nur hier), oben Uhrzeit;
  „Jetzt“ und „Gleich“ (nächste 60 Minuten). Aktualisiert sich jede Minute von selbst.
- Hinweis für den Echtbetrieb (`STAND.md`): Namen auf einem Bildschirm im Gemeinschaftsbereich
  vorher mit Leitung und Datenschutz klären.

## Nachschlagen beim BfArM absichern
Status: freigegeben (Bauplan Schritt 29p; „Alles freigeben“, Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die eingebettete BfArM-Ansicht (WebView2) gibt so wenig wie möglich nach außen.
Warum: Frage Tobi (09.11.2026): Kann die Medikamentenschnittstelle ins Internet ein
Datenschutzproblem geben?
Umfang (Vorschlag):
- Nur Adressen des BfArM laden; **alle Anfragen an andere Adressen blockieren**
  (`WebResourceRequested`), auch eingebettete Schriften, Statistik- oder Werbedienste. Blockierte
  Adressen im Protokoll zählen.
- WebView2: SmartScreen-Prüfung aus (sonst gehen aufgerufene Adressen an Microsoft),
  automatisches Ausfüllen und Passwortspeicher aus, eigener Datenordner, Cookies und Verlauf beim
  Schließen löschen.
- Suchbegriff nur aus dem Feld „Medikamentenname“; nie Bewohner- oder Arztangaben (wie bisher).
- Hinweis in den Einstellungen bei „Nachschlagen beim BfArM“: welche Daten das Programm dabei
  sendet (Suchbegriff, IP-Adresse der Einrichtung, Uhrzeit).

## Aufgaben gehören zu einem Bewohner oder einem Bereich
Status: erledigt (09.11.2026, 0.33.7; Bauplan Schritt 29q, Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Jede Aufgabe gehört entweder zu einem Bewohner oder zu einem Bereich. Man sieht nur die
Aufgaben des gewählten Bereichs.
Warum: Vorgabe Tobi (09.11.2026): Aufgaben können bewohnerbezogen oder bereichsbezogen sein. Jemand
von der R2 muss die Aufgaben der R3 nicht sehen. Aufgaben sind ein wichtiges Übergabeinstrument
unter Kollegen. Bisher (bis 0.33.3) stehen Aufgaben ohne Bewohner in allen drei Bereichen.
Umfang:
- Neue Aufgabe: Auswahl **„Bewohner“** (dann gehört sie zu dessen Bereich, wechselt bei einem
  Umzug mit) oder **„Bereich“** (R1, R2, R3 oder Therapie; Vorgabe: der in der Übersicht gewählte
  Bereich, Schritt 29d). Kein „ganzes Haus“.
- Aufgaben-Seite, Übersicht („Laufender Dienst“) und Zähler zeigen nur Aufgaben des gewählten
  Bereichs.
- **Standardaufgaben (Vorlagen)** ebenfalls je Bereich; Schichtzeiten sind schon je Bereich.
- Bestehende Aufgaben ohne Bewohner: beim Update einmalig dem Bereich des Rechners zuordnen, auf
  dem sie angelegt wurden; ist der unbekannt, R1 und in `STAND.md` vermerken.
- Demo-Daten: Bereichsaufgaben für jeden Bereich getrennt.

## Betriebsmodus für den Terminalserver
Status: freigegeben (Bauplan Schritt 37; OK Tobi 09.11.2026; `CLAUDE.md` Regel 4 ist geändert)
Datum: 09.11.2026
Was: Neben der Demo eine Betriebsfassung desselben Programms für den echten Einsatz auf dem Server
der Einrichtung. Gebaut und getestet wird nur mit ausgedachten Daten.
Warum: Tobi (09.11.2026): Die Demo ist für die Präsentation, gebraucht wird auch schon eine Version
für den echten Betrieb. Es gibt **einen Server**, auf den **beide Häuser** zugreifen; alle
Arbeitsplätze sind kleine Remote-Geräte (Thin Clients) mit Remotedesktop-Sitzung auf diesem
Server (Terminalserver). Eine eigene IT gibt es nicht; die Verwaltung übernimmt Tobi.
Folge für die Technik: DokuLine läuft für alle auf **einem** Rechner. Die SQLite-Datei liegt
dort lokal (kein Netzlaufwerk) — das ist der von SQLite vorgesehene Fall „mehrere Programme, ein
Rechner“. Ein eigener Datenbankserver ist vorerst nicht nötig.
Umfang (Vorschlag):
- **Ein Programm, zwei Modi.** Demo wie bisher. Betriebsmodus über eine Datei `betrieb.json`
  im Programmordner (legt nur der Verwalter an, nie im Repository). Im Betriebsmodus: keine
  Demo-Daten, kein „Demo-Daten zurücksetzen“, kein Anmelden ohne Passwort, kein Selbst-Update,
  kein Demo-Hinweis; stattdessen in der Statusleiste „Betrieb“.
- **Ordner auf dem Server:** Programm in einem Ordner, den nur der Verwalter ändern darf (z. B.
  `C:\Programme\DokuLine\`); Daten in einem gemeinsamen Ordner (z. B. `D:\DokuLine\Daten\`)
  statt `%APPDATA%` (das ist je Benutzer getrennt). Datenbank im WAL-Modus. Anleitung für die
  Ordnerrechte (Windows-Gruppe „DokuLine-Benutzer“ ändern, alle anderen nichts).
- **Anmeldung mit eigenem Passwort** je Mitarbeiter: Passwort nur als sicherer Hash
  (PBKDF2 aus .NET, keine neue Abhängigkeit); erstes Anmelden mit Einmalpasswort und Pflicht zum
  Ändern; Sperre nach 5 Fehlversuchen (Entsperren durch Leitung/Verwalter); **automatische
  Sperre** nach einstellbarer Zeit ohne Eingabe (Vorgabe 10 Minuten); F12 sperrt sofort.
- **Zugriffsprotokoll:** wer wann welche Akte geöffnet, gedruckt oder geändert hat; einsehbar nur
  mit eigenem Recht (Leitung, Datenschutz); Aufbewahrungsdauer einstellbar.
- **Sicherung:** automatisch jede Nacht (Sicherungsfunktion von SQLite, auch bei laufendem
  Betrieb), 30 tägliche und 12 monatliche Stände, Sicherungsordner einstellbar (möglichst auf
  einem anderen Laufwerk); Prüfung jeder Sicherung; Rücksicherung mit Assistent. Warnung in der
  Kopfzeile für Verwalter, wenn die letzte Sicherung älter als 2 Tage ist.
- **Update im Betrieb:** kein Selbst-Update beim Start. Stattdessen „Update vorbereiten …“ für
  den Verwalter: Datei herunterladen (oder von einem USB-Stick wählen), Prüfsumme prüfen, Einspielen
  zu einer festgelegten Zeit (z. B. nachts), danach alte Fassung als Rückweg behalten.
- **Mehrere Häuser:** Bereiche gehören zu einem Haus (Einstellungen: Häuser und ihre Bereiche);
  Kalender „Haus“ je Haus; Bereichswahl zeigt die Bereiche des eigenen Hauses, andere nur mit
  Recht.
- **Technischer Schutz der Datei:** Vorschlag Laufwerksverschlüsselung des Servers (BitLocker) und
  Ordnerrechte; eine Verschlüsselung der Datenbank selbst (z. B. SQLCipher, BSD-Lizenz) erst,
  wenn der Datenschutz sie fordert — Begründung in `STAND.md`.
- **Windows-Server-Version:** unbekannt (offener Punkt im Vault). Claude Code prüft, welche
  Windows-Server-Versionen .NET 10 unterstützt, und schreibt das Ergebnis in `docs/betrieb.md`.
- **`docs/betrieb.md` (neu)**, in einfachen Worten für Tobi: Installation Schritt für Schritt,
  Ordnerrechte, erste Benutzer anlegen, Sicherung prüfen und zurücksichern, Update einspielen,
  was tun bei Störungen. Dazu eine **Liste der Freigaben** vor dem ersten echten Datensatz:
  Einrichtung/Träger, Datenschutzbeauftragter (Datenschutz-Folgenabschätzung, technische und
  organisatorische Maßnahmen), Mitarbeitervertretung, Prüfung „Medizinprodukt ja/nein“ für den
  Medikamentenplan.
- Tests: alles im Betriebsmodus mit ausgedachten Daten; eine „Betriebsprobe“ auf einem
  Test-Rechner, bevor der echte Server berührt wird.

Änderung von `CLAUDE.md`, Regel 4 (eingetragen 09.11.2026 nach Tobis OK): „**Kein
Echtbetrieb ohne Freigabe.** Die Demo zeigt dauerhaft ‚Demo — ausgedachte Daten‘. Echte Daten nur
im Betriebsmodus und erst nach schriftlicher Freigabe durch Einrichtung, Datenschutz und
Mitarbeitervertretung. Im Repository, in Tests und Commits nie echte Daten — auch nicht aus dem
Betrieb.“

## Übersicht: Überschriften der Kästen eingerahmt
Status: erledigt (09.11.2026, 0.33.7; Bauplan Schritt 29r, Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Die Überschriften der Kästen in der Übersicht heben sich deutlich von ihren Einträgen ab.
Warum: Vorgabe Tobi (09.11.2026): In der Übersicht sollen die einzelnen Fensterbenennungen
eingerahmt werden, damit sie sich besser von den Punkten abheben.
Umfang:
- Jeder Kasten („Übergaben offen“, „Termine heute“, „Laufender Dienst“, „Nachrichten“,
  „Medikamentenänderungen“ und der Kopf der Bewohnerliste) bekommt eine **eigene Kopfleiste**:
  helle Akzentfläche (238, 242, 251), oben abgerundet wie der Kasten (8 px), darunter eine
  Trennlinie (217, 224, 236). Darin links die Überschrift (Semibold), rechts der Zähler.
- Die Einträge darunter auf weißer Fläche mit etwas Abstand zur Kopfleiste (8 px).
- Klick auf die Kopfleiste öffnet wie bisher den Bereich; Mauszeiger als Hand, Kopfleiste beim
  Darüberfahren etwas kräftiger.
- Dieselbe Kopfleiste überall, wo es solche Kästen gibt (z. B. Laufende Therapien, Wochenpläne),
  damit es im Programm einheitlich bleibt (`docs/gestaltung.md` ergänzen).

## Aufräumen nach den Code-Regeln (ohne neue Funktionen)
Status: erledigt (09.11.2026, 0.33.8–0.33.10; Bauplan Schritt 29s, Vorgabe Tobi 09.11.2026)
Datum: 09.11.2026
Was: Der vorhandene Code wird nach `docs/code-regeln.md` aufgeräumt. Das Programm verhält sich
danach genau wie vorher.
Warum: Vorgabe Tobi (09.11.2026): Der Code soll effektiver werden und für jemanden, der ihn sich
ansieht, ersichtlich sein; kürzere Kommentare machen die Dateien kleiner.
Umfang (in mehreren kleinen Pull Requests, jeder mit grünen Tests):
1. `.editorconfig` und `dotnet format --verify-no-changes` im Windows-Build; Skript in
   `werkzeuge/`, das Dateien über 400 Zeilen, Methoden über 40 Zeilen und Zeilen über 120 Zeichen
   auflistet (Hinweis im Build, kein Fehler).
2. `docs/aufbau.md` mit Überblick und Glossar (Code-Regeln, Abschnitt 4).
3. Kommentare: Verlauf („Schritt …“, „0.xx: vorher …“) entfernen, Dateiköpfe auf höchstens drei
   Zeilen, auskommentierten Code löschen.
4. Gemeinsame Schnittstelle für die Bereichsseiten; `Hauptfenster.BereichWaehlen` ohne
   `if`-Kette.
5. Die größten Dateien teilen (zuerst `Hauptfenster`, `KalenderSeite`, `MedikationAnsicht`,
   `BewohnerBereich`, `VisitenSeite`, `EinstellungenSeite`).
6. **Vorher/nachher in `STAND.md`:** Zeilen gesamt, Kommentarzeilen, Dateien über 400 Zeilen,
   Zeilen über 120 Zeichen (Ausgangswerte stehen in `docs/code-regeln.md`).
- Kann mit Schritt 29m (schnellerer Wechsel) zusammen geplant werden, aber getrennte Pull Requests.
