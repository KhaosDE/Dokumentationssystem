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
Status: freigegeben
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
Status: offen
Datum: 05.11.2026
Was: Tägliche schriftliche Dokumentation je Bewohner. Zweiter Teil des Kerns.
Warum: Laut Tobi Hauptteil des Programms neben der Bewohnerführung.
Umfang: wird im Chat ausgearbeitet. Nicht bauen.
- Bekannte Anforderung aus „HEB-Bogen aus Berichten“: Berichte müssen sich später einem
  Zeitraum und einem Lebensbereich bzw. Ziel zuordnen lassen.

## Wochenplan
Status: offen
Datum: 06.11.2026
Was: Eine Anzeige, in der man für jeden Bewohner einzeln seinen Wochenplan sieht. Der Plan
lässt sich jederzeit mit Therapien bearbeiten.
Warum: Der Wochenplan gibt dem Bewohner Struktur und ist eine Grundlage für den HEB-Bogen.
Im bisherigen System gibt es keinen übersichtlichen Wochenplan für den Bewohner.
Umfang (Vorschlag aus dem Chat, von Tobi noch zu bestätigen):
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
Offene Fragen (nicht bauen, bevor geklärt):
- Ist der Wochenplan eine Regelwoche, die jede Woche gleich gilt, bis man sie ändert —
  oder wird jede Kalenderwoche einzeln geplant?
- Soll festgehalten werden, ob der Bewohner einen Termin wahrgenommen hat (teilgenommen,
  abgesagt, ausgefallen)? Das wäre für den HEB-Bogen aussagekräftig.

## HEB-Bogen aus Berichten
Status: offen
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
Offene Fragen (nicht bauen, bevor geklärt):
- Hat der Bogen des Bezirks ausfüllbare Formularfelder, und wie heißen sie? Der Chat trägt
  die Feldnamen nach, sobald Tobi einen leeren Bogen bereitstellt.
- Gibt es einen Zwischenschritt (Monats- oder Quartalsreflexion) zwischen Berichten und
  HEB-Bogen?
- Weitere Felder außerhalb der Lebensbereiche (Sichtweise des Leistungsberechtigten,
  ergänzende Ausführungen, Erläuterung zur Beendigung) — Aufbau noch nicht erfasst.

## Medikation: Grundverordnung, Pause, Stufenplan
Status: offen
Datum: 06.11.2026
Was: Medikation je Bewohner. Die Grundverordnung bleibt immer erhalten; Pausen und
Änderungen in Etappen (ansetzen, absetzen) liegen als eigene Einträge darüber.
Warum: Im bisherigen System lassen sich Medikamente nicht pausieren, und eine Anpassung
über mehrere Tage geht nur, indem das Medikament mehrfach angelegt wird.
Umfang (Vorschlag aus dem Chat, von Tobi noch zu bestätigen):
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
Offene Fragen (nicht bauen, bevor geklärt):
- Was gilt am Ende eines Stufenplans: Beim Ansetzen oder Umstellen wird die letzte Stufe
  zur neuen Grundverordnung? Beim Absetzen auf null bleibt die Grundverordnung als
  „abgesetzt am …“ im Verlauf stehen?

## Medikamentendatenbank
Status: offen
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
Offen: Eine vollständige echte Datenbank (z. B. vom BfArM oder einem kommerziellen
Anbieter) erst klären, wenn das Programm über eine Demo hinausgeht — Nutzungsrechte sind
nicht geprüft.

## Startseite als Übersicht (Dashboard)
Status: offen
Datum: 06.11.2026
Was: Die Hauptseite zeigt alle Bewohner des Wohnbereichs auf einen Blick und ist immer
aktuell.
Warum: Wer den Dienst beginnt, soll sofort sehen, was heute wichtig ist, ohne jede Akte zu
öffnen.
Umfang (Vorschlag aus dem Chat, von Tobi noch zu bestätigen):
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
Offene Fragen:
- Ab wie vielen Tagen vor Beschlussende soll „Beschluss endet bald“ erscheinen?

## Therapie-Bereich
Status: offen
Datum: 06.11.2026
Was: Ein eigener Bereich in der Navigation, in dem die Therapie ihre Angebote verwaltet,
einträgt und Bewohnern zuteilt. Zugeteilte Therapien erscheinen in den Wochenplänen.
Warum: Die Therapie plant ihre Angebote selbst; der Wohnbereich soll das Ergebnis im
Wochenplan sehen, ohne es abzuschreiben.
Umfang (Vorschlag aus dem Chat, von Tobi noch zu bestätigen):
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
Status: freigegeben (wird zusammen mit „Umstieg auf C# / .NET 10“ gebaut)
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
Status: offen
Datum: 06.11.2026
Was: Ein Kalender für den ganzen Wohnbereich. Er zeigt Termine, die Bewohner betreffen, und
dient zum Planen von Heimfahrten und Tagesausflügen.
Warum: Wunsch Tobi (06.11.2026). Termine einzelner Bewohner und gemeinsame Unternehmungen
stehen an einer Stelle, statt verstreut.
Umfang (Vorschlag aus dem Chat, von Tobi noch zu bestätigen):
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
Offene Fragen:
- Muss bei einer Heimfahrt etwas mitgeplant werden (z. B. mitgegebene Medikation,
  Rückkehrzeit)?
