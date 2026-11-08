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
| 27 | Ausgangsliste als übersichtliche Liste | 0.30.0 |
| 28 | E-Mail und Kalender über externe Programme | 0.31.0 |

Für alle Schritte gilt:
- Endet eine Sitzung mitten im Plan, beginnt die nächste beim ersten Schritt, der noch nicht
  `erledigt` ist.
- **Jeder Bereich bekommt ausgedachte Demo-Daten**, damit er sich vorführen lässt. Datumsangaben
  in Demo-Daten **relativ zum heutigen Tag** erzeugen (z. B. „vor 3 Tagen“, „in 2 Wochen“),
  damit die Demo an jedem Tag etwas zeigt. „Demo-Daten zurücksetzen“ setzt alle Bereiche zurück.
- Demo-Texte (Berichte, Biografien) sind erkennbar ausgedacht, kurz und wertneutral nach
  `docs/heb-wissen.md`; keine Diagnosen.
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
- **Eigenes Projekt** `src/Dokusystem.Modul.Ausgangsliste/`, abhängig nur von Schnittstellen
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
Status: freigegeben (Bauplan Schritt 18)
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
Status: freigegeben (Bauplan Schritt 19)
Datum: 08.11.2026
Was: Eine Mitarbeiterdatenbank, Anmeldung beim Programmstart mit dem Namenskürzel und eine
Rechteverwaltung nach Positionen.
Warum: Vorgaben Tobi (08.11.2026): Man meldet sich beim Start mit seinem Kürzel an; das
Kürzel wird beim Anlegen eines Mitarbeiters festgelegt. Nicht jeder darf alles.
Umfang:
- **Mitarbeiterdatenbank:** Kürzel (eindeutig, Pflicht), Vor- und Nachname (ausgedacht),
  Position, Bereich(e), **Fachkraft ja/nein**, aktiv/ausgeschieden (ausgeschiedene können
  sich nicht mehr anmelden, bleiben aber an ihren Einträgen sichtbar). Gelöscht wird nicht.
- **Anmeldung beim Start:** Fenster „Anmelden“ mit Kürzel. Demo: **ohne Passwort**; das
  Fenster ist so gebaut, dass ein Passwortfeld später ergänzt werden kann. „Angemeldet als …“
  oben rechts wird zu „Abmelden / Benutzer wechseln“. Das bisherige „Benutzer wählen“ ohne
  Anmeldung entfällt.
- **Positionen:** Geschäftsführung, Hausleitung, Verwaltung, Bereichsleitung, Mitarbeiter
  Wohnbereich, Psychologischer Fachdienst, Pädagogischer Fachdienst, Therapie. Liste
  erweiterbar.
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
  Medikamentendatenbank. „Medikament anlegen“ hat „nur Fachkräfte“. „Therapien bearbeiten“
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
Status: freigegeben (Bauplan Schritt 20)
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
Status: freigegeben (Bauplan Schritt 21)
Datum: 08.11.2026
Was: Die Medikation in der Akte wird eine übersichtliche Tabelle. Die Medikamentendatenbank
wandert in die Einstellungen. Neu angelegte Medikamente werden geprüft.
Warum: Rückmeldung Tobi (08.11.2026): Ab mehr als drei Medikamenten wird die bisherige
Darstellung sehr unübersichtlich. Auf einen Blick muss erkennbar sein, wie das Medikament
heißt, welcher Arzt es verordnet hat und seit wann.
Umfang:
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
  gekennzeichnet. Knopf „In offizieller Datenbank nachsehen“ öffnet die Suche des BfArM im
  **Browser** (das Programm selbst geht nicht ins Internet, `CLAUDE.md`, Regel 6). Wer
  „Medikamentendatenbank verwalten“ darf, kann Einträge später korrigieren oder sperren;
  jede Änderung bleibt im Verlauf.
- Ein **automatischer** Abgleich mit offiziellen Datenbanken ist ausdrücklich nicht dabei: Er
  bräuchte Internet und Nutzungsrechte, die nicht geklärt sind.

## Therapien: Zuordnung und Rechte
Status: freigegeben (Bauplan Schritt 22)
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
Status: freigegeben (Bauplan Schritt 23)
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
Status: freigegeben (Bauplan Schritt 24)
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
Status: freigegeben (Bauplan Schritt 25)
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
Status: freigegeben (Bauplan Schritt 26)
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
    Knopf, der das passende Formular der Medikation beim Bewohner öffnet, vorbelegt mit der
    Visite als Anordnung (Arzt, Datum).
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
Status: freigegeben (Bauplan Schritt 27)
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
  `src/Dokusystem.Modul.Ausgangsliste/AENDERUNGEN.md` eintragen (Abgleich mit der
  eigenständigen Ausgangsliste, `docs/bruecke-ausgangsliste.md`).

## E-Mail und Kalender über externe Programme
Status: freigegeben (Bauplan Schritt 28)
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
