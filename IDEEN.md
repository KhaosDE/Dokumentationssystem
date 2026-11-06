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
- Therapien kommen aus einer Auswahlliste (in der Demo ausgedachte, allgemeine Angebote),
  die sich ergänzen lässt; daneben freie Einträge.
- Termine anlegen, verschieben, ändern, löschen — jederzeit.
- **Verlauf bleibt erhalten:** Jede Änderung gilt „ab Datum …“; frühere Fassungen werden
  nicht überschrieben. Grund: Der HEB-Bogen muss zeigen können, welcher Plan in seinem
  Zeitraum galt.
- Ausdrücklich nicht: Dienstplan der Mitarbeiter, Raumbelegung, Abgleich mit Kalendern.
Offene Fragen (nicht bauen, bevor geklärt):
- Ist der Wochenplan eine Regelwoche, die jede Woche gleich gilt, bis man sie ändert —
  oder wird jede Kalenderwoche einzeln geplant?
- Soll festgehalten werden, ob der Bewohner einen Termin wahrgenommen hat (teilgenommen,
  abgesagt, ausgefallen)? Das wäre für den HEB-Bogen aussagekräftig.
- Braucht es einen Ausdruck für den Bewohner?

## HEB-Bogen aus Berichten
Status: offen
Datum: 06.11.2026
Was: Für einen Bewohner einen HEB-Bogen (Hilfe- und Entwicklungsbogen) für einen Zeitraum
„von … bis …“ erstellen. Das Programm nutzt dafür die eingetragenen Berichte dieses
Zeitraums.
Warum: Die Bewohner des beschützenden Wohnbereichs haben meist für ein oder mehrere Jahre
einen Beschluss. Für ihre Entwicklung ist ein HEB-Bogen nötig; die Inhalte stehen schon in
den Berichten und sollen nicht noch einmal zusammengesucht werden.
Umfang (Rahmen, Einzelheiten folgen aus dem Chat):
- Zeitraum wählen, Berichte des Zeitraums werden gesammelt und den Feldern des Bogens
  zugeordnet.
- Zweite Quelle neben den Berichten: der Wochenplan, der im Zeitraum galt (siehe
  „Wochenplan“).
- Nichts erfinden: Im Bogen steht nur, was in den Berichten steht. Fehlt zu einem Feld
  etwas, steht dort sichtbar „keine Berichte zu diesem Bereich im Zeitraum“.
- Keine klinische Bewertung durch das Programm.
- Ergebnis ist ein **Entwurf**, den ein Mensch prüft, ändert und erst dann als fertig
  markiert. Jede Aussage im Entwurf zeigt, aus welchem Bericht (Datum) sie stammt.
- Ausdrücklich nicht: Versand an den Kostenträger.
Offene Fragen (nicht bauen, bevor geklärt):
- Wie entsteht der Text: Programm stellt die Berichte je Bereich zusammen und ein Mensch
  formuliert — oder eine KI formuliert einen Entwurf (bräuchte Netzzugriff zu einem
  KI-Anbieter, einen Schlüssel und kostet Geld; widerspricht heute `CLAUDE.md`, Technik:
  „kein anderer Netzzugriff“)?
- Welche Bogenart(en) und Felder in der Demo, mit Zeichengrenzen je Feld. Die Felderliste
  liefert der Chat nach (Quelle: Tobis Vault, `Psychiatrie/HEB-Berichte`).
- Gibt es einen Zwischenschritt (Monats- oder Quartalsreflexion) zwischen Berichten und
  HEB-Bogen?

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
