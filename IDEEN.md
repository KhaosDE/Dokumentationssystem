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
- „Mehrplatzbetrieb über einen Server (später)“ gehört **nicht** dazu.

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
- **Noch nicht entschieden, nicht bauen:** Warnung vor Beschlussende, Angabe „offen geführt
  ja/nein“, Alter aus dem Geburtsdatum berechnet.
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
Status: freigegeben (Bauplan Schritt 9)
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
Status: freigegeben (Bauplan Schritt 15)
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
Status: freigegeben (Bauplan Schritt 14)
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
Status: offen
Datum: 06.11.2026
Was: Das Programm arbeitet über einen Server auf allen Bereichen (Wohnbereiche, Therapie).
Dann lässt sich zu jedem Bewohner eine Statusmeldung machen, die alle Bereiche sehen.
Warum: Laut Tobi der Weg, wie das Programm im Betrieb funktionieren würde.
Umfang: **nicht für die Demo.** Voraussetzungen, bevor es überhaupt geplant wird: Freigabe
durch Einrichtung und Datenschutz, Beteiligung der Mitarbeitervertretung, Anmeldung und
Rechte, Server der Einrichtung. Vorhandene Vorarbeit: Die Ausgangsliste (C#) hat bereits
eine gemeinsame Datenbank für mehrere Arbeitsplätze mit Konfliktschutz.

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
Status: freigegeben (Bauplan Schritt 10)
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
Status: freigegeben (Bauplan Schritt 11)
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
Status: freigegeben (Bauplan Schritt 12)
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
Status: freigegeben (Bauplan Schritt 13)
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
- Ausdrücklich nicht: wiederkehrende Aufgaben (Dienstpläne, Reinigungsdienste) — die
  Arbeitszuweisung ist ein eigenes Werkzeug.

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
