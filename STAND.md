# STAND — DokuLine

Stand: 09.11.2026. Version: **0.33.12** (Bauplan Schritt 29f: kleine Korrekturen).

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

Neu in 0.25.0: Jedes **Therapieangebot gehört der Therapie oder einem Wohnbereich** (Feld
„Anbieter“, Filter danach). Alle sehen alle Angebote; ändern und Bewohner zuteilen darf nur der
Anbieter (Therapie: „Therapien bearbeiten“, Wohnbereich: neues Recht „Therapien des eigenen
Bereichs bearbeiten“ und Zugehörigkeit zum Bereich).

Neu in 0.26.0: **Tagesplan** direkt unter „Übersicht“ – alle Termine von heute (Therapien,
Wochenplan, Kalender) nach Uhrzeit oder nach Bewohner. Bei Therapien „gegangen“, „abgesagt“,
„ausgefallen“ (derselbe Eintrag wie im Wochenplan); nach Beginn ohne Bestätigung „noch offen“.
Nach „gegangen“ steht der Bewohner auf Karte und Akte als **„in Therapie: … bis …“**.

Neu in 0.27.0: **Kalender „Bereich“ und „Haus“** (Umschalter, Haus-Termine grau und beschriftet,
nur mit Recht änderbar) und **Fahrzeuge** (VW Bus, Caddy): Fahrten eintragen, „vor Ort“ oder
„unterwegs seit …, zurück geplant …“ mit „losgefahren“/„zurück“ in Übersicht und Kalender,
Warnung bei Doppelbuchung, Mitfahrende im Tagesplan und während der Fahrt abwesend.

Neu in 0.28.0: **Schichtzeiten** des Bereichs in den Einstellungen, **Aufgaben nach Schicht
gruppiert** (oben die laufende, Liegengebliebenes davor) und **Standardaufgaben**, aus denen zu
Beginn jeder Schicht die Aufgaben entstehen; nicht erledigte bleiben „aus der Nachtschicht offen“,
bis jemand sie erledigt oder **begründet schließt**. Ausdruck: Aufgaben der Schicht.

Neu in 0.29.0: **Visiten** unter „Übergabe“ – je Bewohner das Besprochene, geänderte Anordnungen
(Art und Medikament, aus der Datenbank oder frei) und nötige Facharzttermine. Nach dem Speichern
blendet das Programm sofort die **Aufgaben** ein: „Medikament anlegen …“ (Name vorbelegt, danach
gleich die Verordnung), „Im Medikamentenplan eintragen …“ (Akte, Reiter „Medikamentenplan“, das
passende Formular mit Arzt und Datum der Visite), „Termin eintragen …“ (Kalender beim Bewohner, auf
Wunsch mit Fahrt). Die Medikation ändert das Programm nicht selbst. Je Bewohner entsteht ein Bericht
(Lebensbereich 5).

Neu in 0.29.1: Das **Kürzel kommt aus der Anmeldung** – „eingetragen von“ ist in allen Formularen
eine feste Anzeige (Bericht, Nachtrag, Aufgabe; die übrigen setzten es schon selbst). Fahrer,
Teilnehmer der Visite und Begleitung eines Ausflugs sind eine **Auswahl aus den Mitarbeitern**,
vorbelegt mit der angemeldeten Person. Ohne Anmeldung sind „Neue Aufgabe“, „Notiz“ und „Neuer
Termin“ gesperrt („Bitte zuerst anmelden.“).

Neu in 0.29.2: Das Programm heißt **DokuLine** – „Betreuung auf Augenhöhe“ (Fenstertitel,
Seitenleiste, Anmeldung, Einstellungen, Fußzeile jedes Ausdrucks). Programmdatei `DokuLine.exe`,
Datenordner `%APPDATA%\DokuLine` (der Inhalt des alten Ordners wird beim ersten Start kopiert, der
alte bleibt). Übergang: Jedes Release enthält die Datei auch als `Dokumentationssystem.exe`, und das
Update findet das Repository unter altem und neuem Namen.

Neu in 0.29.3: Position **„Administrator“** mit allen Rechten (in der Rechte-Tabelle nicht abwählbar)
und der Mitarbeiter **TST** („Test Administrator“); der letzte aktive Administrator bleibt immer.
**Abmelden** ist sichtbar: oben rechts „Angemeldet: TST – Test Administrator“ und der Knopf
**„Abmelden (F12)“**, die Taste **F12** aus jedem Fenster, unten in der Seitenleiste und in den
Einstellungen. Danach ist nur das Anmeldefenster über einer leeren Fläche zu sehen. Ein angefangener
Bericht kann als **Entwurf** bleiben („Entwurf fortsetzen“ oben rechts nach dem Anmelden).

Neu in 0.29.4: **Ärzteverzeichnis** (Einstellungen → „VERWALTUNG“) mit ausgedachten Ärztinnen und
Ärzten. Wo ein Arzt eingetragen wird – Verordnung, Pause/Stufenplan/Absetzen, Visite, Facharzttermin,
Termin –, steht eine **Auswahl** statt eines Textfelds; „Arzt anlegen …“ gleich daneben (Recht
„Ärzteverzeichnis verwalten“).

Neu in 0.29.5: **„Visite beginnen …“** (Datum, Arzt aus dem Verzeichnis, dabei) öffnet die
**Visitenansicht** über die ganze Fläche: links alle Bewohner des Bereichs (Zimmer, Name,
Bezugspflege, Häkchen „besprochen“, „noch offen: 7 von 10“), rechts Allergien, die **aktuelle
Medikation** wie im Medikamentenplan, die letzte Visite, offene Aufgaben zur Medikation und die
Eingabe. Alles steht sofort als **Entwurf** in der Datenbank; „Unterbrechen“ und später **„Visite
fortsetzen“**. Erst **„Visite abschließen …“** legt Berichte und Aufgaben an und zeigt die Aufgaben
gesammelt. Ausdruck **„Visitenliste“** als Papiervorlage. Das Feld „Art der Visite“ entfällt.

Neu in 0.29.6: Nachrichten im Posteingang haben einen Knopf **„Gelesen“** (auch mehrere markiert auf
einmal) – sie wandern dann ins **Archiv**, nur für die Person, die sie gelesen hat; im Archiv holt
**„Wieder als ungelesen“** sie zurück. In **jedem Datumsfeld** setzt das Programm die Punkte selbst
(`08112026` → `08.11.2026`, `081126` → `08.11.2026`), in Uhrzeitfeldern den Doppelpunkt (`0930` →
`09:30`). Der **Kopf der Akte** zeigt groß „Bewohnerakte“, der Name steht nur einmal, auf der Karte.

Neu in 0.29.7: Die Demo hat die Wohnbereiche **R1 (offen), R2 und R3 (beschützend)** statt „Wohnbereich
A/B“, je sechs **Zimmer** mit **Betten** (Einzelzimmer oder „Bett Fenster“/„Bett Tür“; Einstellungen →
„WOHNBEREICHE“ → „Zimmer und Betten …“). Die **Aufnahme** fragt erst den Bereich, dann zeigt sie nur
**freie Betten**; „offen geführt“ steht dort nicht mehr (neue Bewohner: „nein“). Die Übersicht filtert
nach Bereich und zeigt „R2 · 2 Bewohner · 7 Betten frei“. Ältere Demo-Datenbanken werden einmal neu
geladen; die bisherige liegt als Kopie daneben.

Neu in 0.29.8: In der Akte der Knopf **„Ärztliche Informationen …“**: **Diagnosen** (ICD-10 freiwillig,
Haupt-/Nebendiagnose, seit, gestellt von – aus dem Ärzteverzeichnis; nie löschen, nur „beenden am …“),
**Allergien** mit den drei Zuständen und je Stoff der **Art der Reaktion**, dazu **„Hinweise für den
Notfall“**. Der Allergiehinweis bleibt oben in der Akte; bearbeitet wird er nur noch dort. Neue Rechte
„Ärztliche Informationen ansehen/bearbeiten“. Visite und HEB-Bogen zeigen die Diagnosen als Information.

Neu in 0.29.9: Der **Tagesplan** ist eine **Anzeigetafel** wie am Flughafen: dunkle Fläche, große helle
Schrift, oben rechts groß Uhrzeit und Datum. Spalten Zeit · Bewohner · Termin · Ort · Begleitung ·
**Status als Wort mit Farbe** (JETZT, GLEICH, GEPLANT, GEGANGEN, NOCH OFFEN, ABGESAGT, AUSGEFALLEN,
ZURÜCK, VORBEI). Oben **„Jetzt und gleich“**, darunter „Später heute“, unten blasser „Erledigt“. Die Tafel
erneuert sich jede Minute. Bestätigen in der Zeile („bestätigen ▾“). **Vollbild** mit Knopf oder **F11**,
Esc beendet.

Neu in 0.29.10: Die Demo hat **23 Bewohner je Wohnbereich** (69 in R1–R3), je Bereich zwei Betten frei,
dazu 14 weitere ausgedachte Mitarbeiter für die Bezugspflege (bis etwa vier Bewohner je Person). Alle
Bereiche haben Daten für die neuen Bewohner: Berichte, Medikamentenplan, Diagnosen, Ausgang, Bezugspflege,
Therapien (neu: Gartengruppe R3, Frühstücksgruppe R2, Entspannung), Termine, Biografien, Visiten in R2 und
R3, HEB-Bögen. Ältere Demo-Datenbanken werden einmal neu geladen; die bisherige liegt als Kopie daneben.

Neu in 0.29.11: Die Knöpfe eines Bereichs stehen nicht mehr in der Kopfzeile, sondern in einer
**Aktionsleiste direkt darunter** – überall gleich: links die Hauptaktion (farbig), dann die übrigen,
rechts „… drucken …“ und „Entwurf fortsetzen“. Die Kopfzeile sieht aus wie bisher, nur um diese Zeile
niedriger. Ohne Aktion gibt es keine Leiste.

Neu in 0.29.12: Der Bereich **„Bewohner“** ist eine **Liste** statt Karten (Zimmer und Bett, Name,
Bezugspflege, Hinweise; sortierbar, Suche, Bereichswahl). **Rechts** stehen die **Berichte** des gewählten
Bewohners mit den gewohnten Filtern; Pfeiltasten blättern. „Neuer Bericht …“ (Aktionsleiste) gilt für den
gewählten Bewohner; die Akte öffnet ein Doppelklick oder „Akte öffnen“.

Neu in 0.29.13: In der Visite stehen **„◀ Vorheriger“** und **„Nächster Bewohner ▶“** direkt unter der
Eingabe. „Nächster Bewohner ▶“ ist grau („Erst den Bericht zu diesem Bewohner schreiben.“), bis im Feld
„Besprochenes“ etwas steht. Die Liste links bleibt frei wählbar, sonst ist die Visite wie bisher.

Neu in 0.29.14: Die **Übersicht** ist neu aufgebaut. **Links „Heute wichtig“** mit vier Kästen:
Übergaben offen, Termine heute (mit dem Status aus dem Tagesplan), laufender Dienst („6 von 9 erledigt“,
Balken, offene Aufgaben zum Abhaken) und ungelesene Nachrichten; ein Klick führt an die Stelle. **Rechts**
alle Bewohner des Bereichs als **Liste** (Zimmer/Bett, Name, Bezugspflege, **Wo**, Hinweise) mit Suche;
Doppelklick öffnet die Akte. Die Aktionsleiste hat „Neuer Bericht …“, „Neue Aufgabe …“, „Neue Nachricht …“,
„Neuer Bewohner …“ (grün), rechts die Bereichswahl und „Drucken …“. Ist die Fläche schmaler als 1100 px,
steht „Heute wichtig“ oben. Die Statusleiste zeigt Demo-Hinweis, Datum, Dienst und Stand. Die Fahrzeuge
stehen nur noch im Kalender.

Neu in 0.29.15: Der Bereich **Therapie** hat zwei Reiter. Der erste, **„Laufende Therapien“**, ist eine
Anwesenheitstafel: oben eine Tagesleiste von 7 bis 21 Uhr mit allen Angeboten und einem orangefarbenen
Strich für „jetzt“, darunter je laufender Therapie eine Spalte mit den Teilnehmern (Bereich und Zimmer, Name,
Bezugspflege, Status als Wort mit Farbe), daneben „Gleich“ und „Noch offen“ (mit „gegangen“ und
„abgesagt“), unten der Rest des Tages. Bestätigt wird derselbe Eintrag wie im Tagesplan. Die Demo hat
zehn tägliche Angebote von 07:30 bis 20:45. Der zweite Reiter „Angebote“ ist der bisherige Bereich.

Neu in 0.29.16: DokuLine hat ein eigenes **Programmsymbol** (Entwurf „Hell wie die Kopfzeile“) für die
`.exe`, die Taskleiste und alle Fenster. Das Logofeld oben rechts bleibt für das Bild der Einrichtung.

Neu in 0.30.0: Die **Ausgangsliste** ist eine **Liste** wie die Tagesliste (eine Zeile je Bewohner:
Zimmer, Name, Ausgang, Unterbringung, heute von–bis mit Hz., Stand jetzt). Wer unterwegs ist, steht
oben und ist hinterlegt, Überfällige rot mit „überfällig seit …“; „raus“/„zurück“ direkt in der
Zeile, Filter „nur unterwegs“ und „nur mit Beschränkung“.

Neu in 0.31.0: **Betreuer** in der Akte (Name, Stelle, Telefon, E-Mail) und **„E-Mail an Betreuer
…“** – nach dem Hinweis „Keine Gesundheitsdaten unverschlüsselt per E-Mail senden.“ öffnet sich das
E-Mail-Programm des Rechners mit Empfänger und Betreff. Im Kalender **„Als .ics speichern …“** und
**„.ics übernehmen …“** (auch per Ziehen) mit Vorschau und Zuordnung zu Bewohner und Bereich/Haus.

Neu in 0.32.0: **Einstellungen als ganze Seite** wie in Windows 11 – links zehn Gruppen mit Symbol
und eine Suche, rechts je Einstellung eine Karte; Änderungen gelten sofort, ohne Recht ist eine Karte
grau und nennt den Grund. „Einstellungen“ steht fest unten in der Seitenleiste; „Demo-Daten
zurücksetzen“ und „Standardaufgaben …“ gibt es nur noch dort (keine zweite Stelle).

Neu in 0.33.0: Jeder Mitarbeiter ist **Assistenzkraft** oder **Fachkraft** (Anmeldung, Mitarbeiterliste,
Formular). **Visiten** machen nur Fachkräfte. Neu im Medikamentenplan: **„Bedarf gegeben …“** an jeder
Bedarfsverordnung – Uhrzeit, Menge, Anlass; man sieht, was heute schon gegeben wurde und was höchstens
erlaubt ist. Eine Assistenzkraft muss die Fachkraft angeben, mit der die Gabe abgesprochen ist. Jede Gabe
steht als Bericht in der Akte.

Neu in 0.33.1: Korrektur. War eine der vorgegebenen Positionen (z. B. „Mitarbeiter Wohnbereich“) umbenannt oder
gelöscht, brach der erste Start nach dem Update auf 0.33.0 mit „Die Datenbank lässt sich nicht öffnen“ ab. Jetzt
wird eine fehlende Position beim Vorbelegen neuer Rechte übersprungen. Dazu ein **Sicherheitsnetz für Updates**:
Tests starten das Programm mit einer vom Nutzer veränderten Datenbank und holen dabei jeden einmaligen Schritt nach.

Neu in 0.33.2: Korrekturen. Die **Übersicht** folgt dem Schichtwechsel und legt die Standardaufgaben der Schicht
selbst an. Ein **Doppelklick** auf einen Haken im Kasten „Laufender Dienst“ oder auf „gegangen“/„abgesagt“ der
Therapie-Tafel trifft nicht mehr den nächsten Eintrag; die Tafel behält beim Neuaufbau ihren Bildlauf. Im Bereich
**Bewohner** lassen sich die Berichte rollen. **Bedarf:** Eine Gabe kurz vor Mitternacht lässt sich danach noch
eintragen; angezeigt werden die letzten 24 Stunden; die **Höchstmenge** zählt die Menge (auch bei Nachträgen und über
alle Bedarfsverordnungen desselben Medikaments). Die **Visite** prüft beim Abschließen das Recht.

Neu in 0.33.3: Korrekturen. Beim **Betreuer** gibt es den dritten Zustand **„keine gesetzliche Betreuung“** (z. B. wenn
eine Betreuung endet). Das Einlesen von **.ics-Dateien** übersteht kaputte Termine, Wochen-Dauern und UTC-Zeitzonen;
mehrere Termine werden in einem Zug übernommen. **Dialoge** zeigen das Programmsymbol. In den **Einstellungen** bleiben
Untermodule gesperrt, solange das Modul aus ist. Die Bereichswahl verdeckt nie „Drucken …“.

Neu in 0.33.4: Das Ärzteverzeichnis ist jetzt das **Personenverzeichnis** – Einstellungen → **„Verzeichnisse“** mit
Ärzte · Betreuer · Angehörige · Forensik · FOSA · Sonstige Stellen (je mit Zähler, eigenen Spalten und Suche). Eine
Person kann mehreren Bewohnern zugeordnet sein, je mit Rolle (z. B. „Mutter“, „Betreuer“); aus der Person springt
„Akte öffnen“ in die Akte. Die Betreuer aus den Stammdaten stehen jetzt auch im Verzeichnis.

Neu in 0.33.5: Der Bereich „Bewohner“ heißt **„Dokumentation“**. Links eine schmale, einklappbare Liste nur mit
„Nachname, Vorname“ (Bereichswahl, Suche, Pfeiltasten), rechts **alle Berichte** des gewählten Bewohners nach Tagen,
jeder in der Farbe seiner Schicht umrandet, kompakt mit „… mehr“ oder vollständig. Nur noch drei Marken: „Wichtig“,
„Medikation geändert“, „Bedarfsgabe“. **Filter** (Zeitraum, Wichtig, Medikation geändert, Bedarfsgabe, Visite, Übergabe,
mit Nachtrag, Berichtsart, Lebensbereich, Schicht, Verfasser, Suche mit Hervorhebung) lassen sich unter eigenem Namen
**speichern**. Neu ist die **Berichtsart** beim Schreiben (Vorgabe „Alltag“, Liste in den Einstellungen). Der Reiter
„Berichte“ in der Akte entfällt; dort führt „Berichte ansehen“ zur Dokumentation.

Neu in 0.33.6: **Was jemand nicht darf, sieht er nicht.** Knöpfe, Menüeinträge, Einstellungen und Bereiche der
Seitenleiste ohne Recht sind ausgeblendet, die übrigen rücken zusammen (z. B. „Visiten“ fehlt für Assistenzkräfte).
Grau mit Hinweis bleibt nur, was gerade nicht geht. Nach An- und Abmelden baut sich alles sofort neu auf.

Neu in 0.33.7: Oben in der Übersicht **„Wohnbereich: R1 · R2 · R3“** (je nach Recht dazu „Therapie“ und „Alle“).
Die Wahl gilt **überall** – Dokumentation, Übergabe, Tagesplan, Visiten, Aufgaben, Kalender, Laufende Therapien,
Ausgangsliste – bis zum Abmelden; vorgegeben ist der Bereich des Rechners. Der Bereich steht vorn in der Kopfzeile,
bei einem anderen Bereich mit der Marke „eingesprungen“. Die einzelnen Bereichswahlen in den Fenstern entfallen.
**Aufgaben** gehören zu einem Bewohner oder einem Bereich; jeder sieht nur die seines Bereichs, Standardaufgaben gibt
es je Bereich. Die Kästen der Übersicht haben eine eigene **Kopfleiste**.

Neu in 0.33.8 (nur für die, die den Code ansehen; das Programm verhält sich wie vorher): Jeder Windows-Build prüft
Leerzeichen und Einrückung nach `.editorconfig` und listet Dateien, Methoden und Zeilen über den Richtwerten
(`werkzeuge/code-masse.py`). Neu `docs/aufbau.md` mit Überblick über die Projekte, den Weg der Daten und einem Glossar.

Neu in 0.33.9 (Aufräumen, das Programm verhält sich wie vorher): Die Kommentare im Code erzählen keinen Verlauf mehr
(„Schritt …“, „0.33.2: vorher …“); lange Erklärungen stehen in `docs/technik.md`.

Neu in 0.33.10 (Aufräumen, das Programm verhält sich wie vorher): Alle Bereiche der Seitenleiste folgen einer
gemeinsamen Schnittstelle; die größten Dateien sind geteilt. Damit ist Schritt 29s abgeschlossen.

Neu in 0.33.11: In der Kopfzeile links neben „Angemeldet: …“ steht ein **Kuvert**. Mit ungelesenen Nachrichten trägt
es einen roten Kreis mit Ausrufezeichen und daneben die Zahl („3“); der Hinweis beim Darüberfahren sagt „3 ungelesene
Nachrichten“. Ohne ungelesene ist es grau und ohne Zeichen. Ein Klick öffnet „Nachrichten“. Der Hinweis über der
Liste im Bereich „Dokumentation“ entfällt; der Zähler in der Seitenleiste bleibt.

Neu in 0.33.12: In den Demo-Daten tragen nur noch Berichte mit Inhalt für die nächste Schicht „Übergabe“ (nie „Keine
Besonderheiten“). Beim Angebot heißt das Feld **„Teilnehmer“** mit „(Anzahl)“ darunter. Im Bereich „Dokumentation“
steht keine Aufgabenliste mehr über den Berichten; die Bewohnerleiste liegt auf einer abgerundeten Fläche, der gewählte
Name ist abgerundet hell hervorgehoben.

**Nächster Schritt:** Bauplan der Reihe nach (Auftrag Tobi, 09.11.2026: „alle Schritte, danach Fehlersuche“) –
als Nächstes 29g „Tagesplan im normalen Programmstil“ (0.33.13).

**Danach:** Der Bauplan ab 07.11.2026 ist abgearbeitet; neue Aufträge kommen über `IDEEN.md`.

## Fertig

- Repository `KhaosDE/Dokumentationssystem` (öffentlich, wegen der Update-Funktion).
- `CLAUDE.md`, `IDEEN.md`, `STAND.md`, `SITZUNGEN.md`, `README.md`, `docs/`.
- 0.1.0 und 0.2.0 (Python, 05.11.2026): Programmrahmen mit Selbst-Update, Bewohnerakte.
- 0.3.0 — Umstieg auf C# / .NET 10 und Gestaltung (06.11.2026):
  - Aufbau: `src/DokuLine.Kern` (ohne Fenster, auch unter Linux testbar),
    `src/DokuLine.Update` (einziges Projekt mit Internet), `src/DokuLine` (Windows Forms),
    Tests in `tests/`. Projektmappe `DokuLine.sln`.
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
  - Neues Projekt `src/DokuLine.Bausteine` (Stil, Karten, Marken, Dialoge, Druckvorschau,
    kleine Formulare, Schnittstelle für Module) – Programm und Module nutzen es gemeinsam.
  - Neues Projekt `src/DokuLine.Modul.Ausgangsliste` mit `AENDERUNGEN.md`, übernommen aus der
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

- 0.25.0 — Therapien: Zuordnung und Rechte (Bauplan Schritt 22, 08.11.2026):
  - Feld „Anbieter“ je Angebot: „Therapie“ oder ein Wohnbereich; auf der Karte als erste Marke.
    Im Formular stehen nur die Anbieter zur Wahl, für die man anlegen darf. Eine Änderung des
    Anbieters ist eine neue Fassung (steht im Verlauf).
  - Bereich „Therapie“: Filter „Anbieter“ (alle, Therapie, Wohnbereich A, Wohnbereich B …).
  - Rechte: Angebote der Therapie ändert und teilt zu, wer „Therapien bearbeiten“ hat (Vorbelegung
    Position Therapie und Leitungen). Angebote eines Wohnbereichs: neues Recht „Therapien des
    eigenen Bereichs bearbeiten“ (Vorbelegung Bereichsleitung) und der Bereich muss zu einem
    gehören; sonst „Nur für Wohnbereich …“. Gesperrte Knöpfe bleiben sichtbar und nennen den Grund.
  - Demo: Kochgruppe gehört Wohnbereich A (Emma Fink), Kreativwerkstatt Wohnbereich B (Georg
    Habicht), die übrigen der Therapie. Nach dem Update werden die Demo-Angebote einmal neu geladen.

- 0.26.0 — Tagesplan (Bauplan Schritt 23, 08.11.2026):
  - Seitenleiste: „Tagesplan“ direkt unter „Übersicht“. Oben „Heute, …“, „nach Uhrzeit“ / „nach
    Bewohner“ (mit Überschrift je Bewohner) und „alle Bereiche“ (sonst die eigenen Wohnbereiche
    wie in der Übersicht).
  - Je Termin eine Karte: Zeit und Titel, Bewohner mit Zimmer und Ort, Marke „Therapie“,
    „Wochenplan“ oder „Kalender“. Bei Therapien „gegangen“, „abgesagt“, „ausgefallen“
    (Recht „Therapie-Teilnahme bestätigen“); die Marke zeigt Status, Uhrzeit und Kürzel
    („gegangen 09:05 · CD“). Nach Beginn ohne Bestätigung: „noch offen“ (gelb).
  - Derselbe Eintrag wie im Wochenplan (dort „teilgenommen“); derselbe Status zweimal ergibt
    keinen zweiten Eintrag.
  - „Wo müsste er sein?“: Nach „gegangen“ zeigen Karte (Bewohner, Übersicht) und Akte bis zum Ende
    des Termins „in Therapie: Holzwerkstatt bis 11:30“, danach nichts mehr (wieder im Haus). Eine
    später gesetzte Statusmeldung geht vor.
  - Kopf: „… Termine heute · Bereich · x von y Therapien bestätigt · z noch offen“. Andere Rechner
    sehen Änderungen nach spätestens 30 Sekunden; der Tagesplan prüft dabei auch die Uhrzeit.

- 0.27.0 — Kalender: Bereich und Haus, Fahrzeuge (Bauplan Schritt 24, 08.11.2026):
  - Kalender: beim Anlegen „Kalender: Bereich / Haus“; oben „Bereich und Haus“, „nur Bereich“,
    „nur Haus“. Haus-Termine grau mit „Haus:“ vor dem Titel bzw. Marke „Kalender Haus“; anlegen,
    ändern und absagen nur mit „Kalender Haus bearbeiten“ (vorbelegt: Bereichsleitung, Leitungen).
  - Fahrzeuge: Einstellungen → „Fahrzeuge …“ (Demo „VW Bus“, „Caddy“; hinzufügen, stilllegen).
  - „Neue Fahrt …“: Fahrzeug, Datum, los – zurück (geplant), Zweck (Arzttermin, Ausflug,
    Sporttherapie, Sonstiges), Fahrer (Kürzel), mitfahrende Bewohner, Ziel. Überschneidet sie sich
    mit einer Fahrt desselben Fahrzeugs: Warnung, Speichern erst mit „Trotzdem speichern“.
  - Übersicht („FAHRZEUGE“ in der Heute-Karte) und Kalender (oben): je Fahrzeug „vor Ort“ (mit der
    nächsten Fahrt von heute) oder „unterwegs seit 09:40, zurück geplant 11:30 (Fahrer AB)“, rot mit
    „Rückkehr überschritten“, Knöpfe „losgefahren“ und „zurück“ (Recht „Fahrzeuge eintragen“;
    Uhrzeit und Kürzel setzt das Programm, eigene Einträge, nie überschrieben).
  - Mitfahrende: im Wochen- und Tagesplan („Fahrt: Arzttermin (Caddy)“), während der Fahrt auf
    Karten und in der Akte „abwesend: unterwegs mit Caddy bis 11:05“.
  - Demo `daten/demo/fahrten.json`: Caddy gerade mit Emil Eiche zum Arzt unterwegs, eine
    Sporttherapie-Fahrt gestern, der Tierpark-Ausflug mit dem VW Bus in acht Tagen; im Kalender
    „Haus“ Hausversammlung und Brandschutzübung.

- 0.28.0 — Aufgaben nach Schichten mit Standardaufgaben (Bauplan Schritt 25, 08.11.2026):
  - Einstellungen → „SCHICHTEN“: Beginn von Früh, Spät, Nacht (Vorgabe 6, 14, 22 Uhr) für den
    Bereich dieses Rechners; gilt für alle Rechner des Bereichs (Recht „Einstellungen des
    Bereichs“). Danach richten sich Schicht der Berichte, Übergabe und Aufgaben.
  - Aufgaben gruppiert: „AUS FRÜHEREN SCHICHTEN OFFEN“, „JETZT: Frühdienst Di 10.11.“, die
    folgenden Schichten, „WER ZEIT HAT“.
  - „Standardaufgaben …“: Text, Schicht, Wochentage (täglich oder ausgewählte), Uhrzeit,
    Bewohner; neu, ändern, stilllegen (Recht „Aufgaben-Vorlagen verwalten“). Zu Beginn jeder
    Schicht entstehen daraus die Aufgaben (je Vorlage und Schicht genau eine), Marke
    „Standardaufgabe“, fällig zur Uhrzeit bzw. zum Schichtende.
  - Nicht erledigte Standardaufgaben: Marke „aus der Nachtschicht offen“; „Schließen mit
    Begründung …“ (Begründung Pflicht, steht danach bei der Aufgabe).
  - „Aufgaben der Schicht drucken …“: was jetzt dran ist (auch Liegengebliebenes) und „wer Zeit
    hat“.
  - Demo `daten/demo/standardaufgaben.json`: Rundgänge, Türen und Fenster, Kühlschränke, Wäsche,
    Nachtbericht im Nachtdienst; Post und Pinnwand im Frühdienst; Abendrunde im Spätdienst.
- 0.29.0 — Visiten (Bauplan Schritt 26, 08.11.2026):
  - Seitenleiste „Visiten“ unter „Übergabe“: alle Visiten, neueste oben, Filter nach Bewohner und
    Zeitraum (von/bis). Je Visite Datum, Art, Arzt, wer dabei war; je Bewohner das Besprochene und
    Marken („Quetiapin: Dosis geändert“, „Anordnungen nicht geändert“, „Facharzttermin nötig:
    Neurologie, dringend“).
  - „Neue Visite …“ (Recht „Visiten eintragen“): Datum, Art (Hausarzt, Psychiater, Facharzt oder
    frei), Arzt (ausgedacht), dabei (Kürzel), Bewohner hinzufügen; je Bewohner „Was wurde
    besprochen?“, „Anordnungen geändert“ mit Zeilen (neu verordnet, Dosis geändert, pausiert,
    abgesetzt; Medikament aus der Datenbank oder frei eingetippt) und „Facharzttermin nötig“
    (Fachrichtung, dringend).
  - Beim Speichern in einem Zug: die Visite, je Bewohner ein Bericht (Lebensbereich 5 – Quelle für
    Monatsreflexion und HEB-Bogen), eine Übergabe („Visite: Anordnungen geändert“ bzw.
    „Facharzttermin nötig“) und je geänderter Anordnung bzw. Facharzttermin eine Aufgabe für die
    laufende Schicht.
  - Einblendung „Aufgaben aus der Visite“ gleich danach, je Aufgabe ein Knopf; nach jeder
    erledigten kommt sie mit den übrigen wieder, „Später“ schließt sie. Dieselben Knöpfe auf den
    Karten unter „Aufgaben“ (Herkunft „aus einer Visite“).
  - Medikament fehlt in der Datenbank → „Neues Medikament“ mit vorbelegtem Namen (Recht
    „Medikament anlegen“), danach gleich „Neue Verordnung“ im Medikamentenplan des Bewohners.
    Medikament vorhanden → Akte beim Reiter „Medikamentenplan“ und das passende Formular: neu →
    „Neue Verordnung“, Dosis → Stufenplan, pausiert → Pause, abgesetzt → Absetzen; vorbelegt mit Arzt,
    Datum und dem Hinweis „laut Visite …“ (steht im Verlauf). Gespeichert → Aufgabe erledigt.
  - Facharzttermin: „Termin eintragen …“ oder „Erledigt“ fragt Datum, Uhrzeit, Ort und auf Wunsch
    eine Fahrt (Fahrzeug, los/zurück, Fahrer; Warnung bei Doppelbuchung) → Termin „Facharzt: …“ im
    Kalender (Bereich) beim Bewohner, Aufgabe erledigt.
  - Demo `daten/demo/visiten.json`: drei Visiten (Dosis geändert, neues Medikament noch nicht in der
    Datenbank, dringender Facharzttermin); ihre Aufgaben sind erledigt.
- 0.29.1 — Kürzel automatisch aus der Anmeldung (Bauplan Schritt 26a, 08.11.2026):
  - „Eingetragen von“ (schreibgeschützt) statt Eingabefeld in „Neuer Bericht“, „Nachtrag“ und „Neue
    Aufgabe“; „Medikament“, „Neue Verordnung“ und „Neue Visite“ zeigen es ebenso. Alle anderen
    Einträge (Übergabe, Erledigt, Weitergeben, Teilnahme, Medikamentenplan, Statusmeldung, Fahrten,
    Ausgang, Biografie, Reflexion, HEB, Nachrichten, Kalender) nahmen das Kürzel schon aus der
    Anmeldung.
  - Personen-Felder als Auswahl aus den aktiven Mitarbeitern, vorbelegt mit der angemeldeten Person:
    Fahrer (Neue Fahrt, Facharzttermin), „Dabei“ (Visite, Häkchen), Begleitung (Ausflug, Häkchen).
    Alte Freitext-Angaben bei der Begleitung bleiben beim Ändern erhalten.
  - Ohne Anmeldung: „Neue Aufgabe …“, „Notiz für den Wohnbereich …“ und „Neuer Termin …“ gesperrt mit
    „Bitte zuerst anmelden.“ (die übrigen Knöpfe waren über die Rechte schon gesperrt); Hinweise
    „bitte zuerst oben rechts anmelden“ statt „unter Einstellungen ein Kürzel eintragen“.
- 0.29.2 — Umbenennung in DokuLine, Teil 1 (Bauplan Schritt 26a1, 08.11.2026):
  - Angezeigter Name „DokuLine“, darunter der Leittext „Betreuung auf Augenhöhe“: Fenstertitel,
    Seitenleiste oben (Name und Leittext), Anmeldefenster, Update-Fenster, Meldungen, Einstellungen →
    „PROGRAMM“ → „Über“. Jeder Ausdruck trägt unten „DokuLine · Demo — ausgedachte Daten“.
  - Projekte, Ordner und Namensräume `DokuLine.*` (`src/DokuLine.Kern`, `src/DokuLine`,
    `src/DokuLine.Modul.Ausgangsliste`, …), Lösung `DokuLine.sln`, Programmdatei `DokuLine.exe`.
  - Release: `DokuLine.exe` und dieselbe Datei als `Dokumentationssystem.exe`, je mit `.sha256`;
    Titel „DokuLine …“, Tags weiter `build-<Laufnummer>`.
  - Update: fragt zuerst `…/repos/KhaosDE/DokuLine/releases/latest`, sonst die alte Adresse;
    akzeptiert Downloads unter beiden Repository-Namen und beide Dateinamen; tauscht die laufende
    Datei unter ihrem eigenen Namen aus (eine `Dokumentationssystem.exe` heißt weiter so).
  - Datenordner `%APPDATA%\DokuLine`: Gibt es ihn nicht, aber `%APPDATA%\Dokumentationssystem`,
    wird dessen Inhalt kopiert (erst in einen Zwischenordner, dann umbenannt); der alte Ordner bleibt
    unangetastet, `update.log` vermerkt die Übernahme. Ein selbst gewählter Datenbank-Ort bleibt.
  - Dokumentation: `CLAUDE.md` (Name, Pfade, Projektnamen), `README.md`, `docs/`, Pfade in `IDEEN.md`.
- 0.29.3 — Administrator TST und Abmelden mit F12 (Bauplan Schritte 26a2 und 26a3, 08.11.2026):
  - Position „Administrator“ an erster Stelle: hat immer alle Rechte, in allen Bereichen, auch ohne
    „Fachkraft“. In der Rechte-Tabelle ist die Spalte voll, ausgegraut, mit Hinweis „Der
    Administrator hat immer alle Rechte“. Die Position lässt sich nicht stilllegen, umbenennen oder
    löschen; vergeben kann sie nur ein Administrator.
  - Mindestens ein aktiver Administrator bleibt: Beim letzten sind „aktiv“ und die Position im
    Mitarbeiterformular gesperrt, mit Hinweis.
  - Mitarbeiter TST („Test Administrator“, Fachkraft, alle Bereiche) in `daten/demo/mitarbeiter.json`.
    Bestehende Datenbanken bekommen die Position und – falls es TST noch nicht gibt – TST einmal
    dazu (Eintrag in `update.log`); ein vorhandener TST bleibt, wie er ist.
  - Kopf oben rechts: „Angemeldet: TST – Test Administrator“ und der Knopf „Abmelden (F12)“ (ohne
    Anmeldung „Anmelden …“) statt des Klappmenüs. Dazu „Abmelden (F12)“ unten in der Seitenleiste
    (eingeklappt als Symbol mit Hinweis) und in den Einstellungen unter „ICH“; „Über“ nennt die Taste.
  - F12 meldet aus jedem Fenster ab. Sind Fenster offen, fragt das Programm einmal: „Als Entwurf
    speichern“ (wenn ein Bericht angefangen ist) / „Verwerfen“ / „Abbrechen“; dann schließt es sie.
  - Nach dem Abmelden liegt eine Fläche mit Name, Leittext und Demo-Hinweis über dem ganzen Fenster,
    darüber das Anmeldefenster („Anmelden“ – auch mit anderem Kürzel – oder „Programm beenden“).
  - Entwürfe (neue Tabelle `entwuerfe`): gehören dem Kürzel, je Bewohner einer. Nach dem Anmelden
    steht oben rechts „Entwurf fortsetzen“; „Neuer Bericht …“ beim selben Bewohner übernimmt ihn
    („Entwurf vom … übernommen“, „Entwurf verwerfen“). Speichern löscht den Entwurf.

- 0.29.4 — Ärzteverzeichnis (Bauplan Schritt 26b, 08.11.2026):
  - Einstellungen → „VERWALTUNG“ → „Ärzteverzeichnis …“: Liste (Name, Fachrichtung, Praxis/Ort,
    Telefon, Status), „Neuer Arzt …“, „Ändern …“, „Inaktiv / wieder aktiv“. Nie löschen.
  - Neues Recht „Ärzteverzeichnis verwalten“ (Vorbelegung: Geschäftsführung, Hausleitung,
    Verwaltung, Bereichsleitung, Psychologischer und Pädagogischer Fachdienst); in bestehenden
    Datenbanken einmal nachträglich vorbelegt.
  - Auswahl statt Freitext (nur aktive Ärzte) mit „Arzt anlegen …“ daneben: Neue Verordnung,
    Pause/Pause beenden/Stufenplan/Absetzen, Neue Visite, Facharzttermin (freiwillig; füllt den Ort)
    und Kalendertermin (freiwillig; füllt Ort und Hinweis mit Name, Praxis, Telefon). Ein älterer
    Name, der nicht im Verzeichnis steht, bleibt wählbar („nicht im Verzeichnis“).
  - Demo `daten/demo/aerzte.json`: sieben ausgedachte Ärztinnen und Ärzte (eine inaktiv), Adressen
    nur `@example.org`; Dr. Ulme und Dr. Linde aus den bisherigen Demo-Daten stehen darin.
- 0.29.5 — Visite: Übersicht aller Bewohner (Bauplan Schritt 26c, 08.11.2026):
  - Seite „Visiten“: Hauptknopf „Visite beginnen …“ (oder „Visite fortsetzen“), daneben „Neue Visite …“
    (einzelne Bewohner, wie bisher) und „Visitenliste drucken …“. Fenster „Visite beginnen“: Datum
    (heute), Arzt (Auswahl aus dem Ärzteverzeichnis), dabei (angemeldete Person vorbelegt).
  - Visitenansicht über die ganze Fläche. Kopfleiste: Datum, Arzt, dabei, „noch offen: X von N“,
    „N Aufgaben vorgemerkt“, Sortierung „nach Zimmer“/„nach Name“, „◀ Vorheriger“, „Nächster ▶“,
    „Unterbrechen“, „Visite abschließen …“. Links die Bewohner des eigenen Wohnbereichs mit Häkchen
    „besprochen“ (setzt sich bei einem Eintrag selbst, lässt sich auch von Hand setzen). Rechts Name,
    Zimmer, Allergien (drei Zustände), Tabelle der aktuellen Medikation und Bedarf (wie im
    Medikamentenplan, mit Pausen und Stufen), letzte Visite in einer Zeile, offene Aufgaben zur
    Medikation, darunter die Eingabe (Besprochenes, Anordnungen geändert, Facharzttermin nötig).
  - Entwurf je Wohnbereich in der Datenbank (neue Tabelle `visite_entwuerfe`): gespeichert beim
    Wechsel des Bewohners, beim Verlassen der Eingabe und 1,5 Sekunden nach dem Tippen.
  - „Visite abschließen …“: eine Visite mit allen besprochenen Bewohnern (Berichte, Aufgaben wie in
    0.29.0), danach **eine** Einblendung mit allen Aufgaben. Während der Visite keine Einblendungen.
  - Ausdruck „Visitenliste“ (A4 hoch): je Bewohner ein Block mit Zimmer, Allergien, Medikation und
    Linien für Notizen.
  - Feld „Art der Visite“ entfällt (ältere Visiten zeigen die Art weiter im Kopf). Ein Arzt ist Pflicht.
- 0.29.6 — Kleine Verbesserungen (Bauplan Schritt 26d, 08.11.2026):
  - Nachrichten: Knopf „Gelesen“ an jedem Verlauf im Posteingang und Häkchen zum Markieren, oben
    „Markierte als gelesen (n)“. Gelesen heißt: gelesen und im Archiv – nur für die lesende Person
    (bei Team-Nachrichten bleibt sie bei den anderen ungelesen). Im Archiv „Wieder als ungelesen“
    (zurück in den Posteingang, wieder neu). Zähler passen sich an. Nichts wird gelöscht.
  - Datum und Uhrzeit: gemeinsame Bausteine `DatumFeld` und `UhrzeitFeld` für alle Datums- und
    Uhrzeitfelder. Beim Tippen setzt das Programm Punkt bzw. Doppelpunkt vor die nächste Ziffer;
    beim Verlassen und beim Lesen: 8 Ziffern → TT.MM.JJJJ, 6 Ziffern und TT.MM.JJ → 20JJ, 4 (oder 3)
    Ziffern → SS:MM. Eigene Punkte stören nicht.
  - Akte: Kopf groß „Bewohnerakte“ (kleine Zeile „Bewohner“), der Name nur auf der Karte darunter.
- 0.29.7 — Wohnbereiche R1–R3, Zimmer und Betten, Aufnahme (Bauplan Schritt 26e, 08.11.2026):
  - Bereiche R1 (offen), R2, R3 (beschützend) in Demo-Daten und Einstellungen; Merkmal „offen /
    beschützend“ je Bereich (Bereiche ohne Merkmal wie Therapie oder Leitung sind keine Wohnbereiche).
    Bewohner, Mitarbeiter, Therapien, Statusmeldungen, Nachrichten und Bereichskennung: R1/R2 statt
    „Wohnbereich A/B“ (die Kennung des Rechners wird einmal umbenannt).
  - Zimmer und Betten (neue Tabelle `zimmer`, Bett am Bewohner): je Bereich sechs Zimmer (R1: 101–106,
    R2: 201–206, R3: 301–306), gemischt Einzel- und Doppelzimmer („Bett Fenster“, „Bett Tür“). Demo:
    alle Bewohner haben ein Bett; R1 hat 2, R2 7, R3 9 freie Betten. Einstellungen → „WOHNBEREICHE“ →
    „Zimmer und Betten …“: Bereiche mit Art (umschaltbar), Zimmer mit Belegung, anlegen, ändern,
    stilllegen (belegte nicht; ein belegtes Bett fällt nie weg). Recht „Einstellungen des Bereichs“.
  - Aufnahme („Neuer Bewohner …“): Wohnbereich wählen, dann nur freie Betten („105 – Bett Tür“, „104 –
    Einzelzimmer“); kein freies Bett: „In diesem Bereich ist kein Bett frei.“ „Offen geführt“ steht nicht
    im Aufnahmeformular – neue Bewohner sind „nein“; ändern in der Akte („Bearbeiten …“).
  - Zimmerwechsel: „Bearbeiten …“ zeigt das bisherige Bett und dieselbe Auswahl freier Betten (auch
    anderer Bereiche); das alte Bett wird frei. Belegte Betten lehnt das Programm ab.
  - Akte: Marke „Zimmer 101 · Bett Fenster“. Übersicht: Auswahl „eigene Bereiche / R1 / R2 / R3“, Kopf
    „R2 · 2 Bewohner · 7 Betten frei · Stand …“.
  - Modul Ausgangsliste: nimmt die Zimmer der Bewohner (keine zweite Liste); die Küchendienst-Gruppe der
    Demo ist jetzt 201/202.
  - Ältere Demo-Datenbanken (noch „Wohnbereich A/B“) werden beim ersten Start einmal neu geladen; vorher
    sichert das Programm sie als `dokumentationssystem-vor-0.29.7.db` neben der Datenbank (`update.log`
    vermerkt es).
- 0.29.8 — Fenster „Ärztliche Informationen“ (Bauplan Schritt 26f, 08.11.2026):
  - Akte: Knopf „Ärztliche Informationen …“ (neben „Status …“). Das Fenster hat drei Teile:
    Diagnosen, Allergien und Unverträglichkeiten, Hinweise für den Notfall.
  - Diagnosen (neue Tabelle `diagnosen`): ICD-10-Schlüssel freiwillig (geprüft auf die Form „F20.0“),
    Bezeichnung, Haupt- oder Nebendiagnose, seit (nicht in der Zukunft), gestellt von (Auswahl aus dem
    Ärzteverzeichnis, „Arzt anlegen …“ daneben), Hinweis (beim Darüberfahren). „Eingetragen von“ setzt
    das Programm. Nichts wird gelöscht oder geändert: „Beenden …“ mit Datum; beendete Diagnosen stehen
    grau unten mit „beendet am …“. Die Datenbank lehnt das Überschreiben selbst ab (außer „beenden“);
    einen Knopf zum Löschen gibt es nicht.
  - Allergien: dieselben drei Zustände wie bisher (Regel 5), gespeichert am Bewohner; je Stoff aus dem
    Text (durch Komma getrennt) eine Zeile „Art der Reaktion (freiwillig)“ (neue Tabelle
    `allergie_reaktionen`). Das Bewohnerformular hat kein Allergie-Feld mehr, nur einen Hinweis auf das
    Fenster; der Allergiehinweis oben in der Akte bleibt für alle sichtbar.
  - Hinweise für den Notfall: ein Freitext je Bewohner (neue Tabelle `notfallhinweise`).
  - Rechte: „Ärztliche Informationen ansehen“ (alle Positionen außer Verwaltung) und „… bearbeiten“
    (ebenso, mit „nur Fachkräfte“). Ohne „ansehen“ ist der Knopf grau mit Begründung; ohne
    „bearbeiten“ öffnet das Fenster mit „Nur ansehen: …“ und grauen Knöpfen.
  - Visite (Rundgang): unter dem Allergiehinweis „Diagnosen: …“ (nur aktuelle, Hauptdiagnosen zuerst).
    HEB-Bogen: dieselbe Zeile im Kopf. Beides nur mit Recht „ansehen“; das Programm wertet sie nicht aus.
  - Demo: 14 ausgedachte Diagnosen für die zehn Bewohner (allgemein gehalten, eine davon beendet),
    zwei Reaktionen und zwei Notfallhinweise. Bestehende Demo-Datenbanken bekommen sie beim ersten Start.
  - Behoben: Die Visite (0.29.5) zeigte „Allergien: Allergien: …“ doppelt, auf dem Bildschirm und in
    der Visitenliste.
- 0.29.9 — Tagesplan als Anzeigetafel (Bauplan Schritt 26g, 08.11.2026):
  - Die Seite „Tagesplan“ zeigt oben hell die Schalter („nach Uhrzeit“, „nach Bewohner“, „alle
    Bereiche“, „Vollbild (F11)“), darunter die Tafel: dunkle Fläche, helle große Schrift, Zeit in fester
    Zeichenbreite (gelb), oben links „TAGESPLAN · R1“ und „Demo — ausgedachte Daten“, oben rechts groß
    Uhrzeit und Datum.
  - Spalten: Zeit · Bewohner (mit Zimmer) · Termin bzw. Therapie · Ort · Begleitung (Therapeut,
    Begleitung des Ausflugs, Fahrzeug und Fahrer) · Status. Der Status ist immer ein Wort auf farbiger
    Fläche, daneben klein, wann und von wem bestätigt („09:05 · CD“).
  - Die Spalten passen sich der Breite an: Ist es zu schmal, fällt zuerst „wann · wer“ weg, dann
    „Begleitung“, dann „Ort“ (Bewohner und Termin bleiben immer). „nach Bewohner“ hat keine Spalte
    „Bewohner“, der Name steht in der Überschrift. Beim Darüberfahren zeigt ein Hinweis die ganze Zeile.
  - Status: JETZT (läuft), GLEICH (beginnt in den nächsten 30 Minuten), GEPLANT, GEGANGEN (bis zum Ende
    der Therapie), NOCH OFFEN (Therapie hat begonnen, nichts bestätigt), ABGESAGT, AUSGEFALLEN, ZURÜCK
    (nach „gegangen“, Therapie zu Ende), VORBEI (anderer Termin zu Ende). Ganztägiges läuft den ganzen Tag.
  - Blöcke: „JETZT UND GLEICH“ (gelb hervorgehoben), „SPÄTER HEUTE“, „ERLEDIGT“ (blasser); darin nach
    Uhrzeit. „nach Bewohner“: je Bewohner eine Überschrift, Status wie oben.
  - Erneuert sich jede Minute (Uhr, Status, Reihenfolge), nach jeder Bestätigung und über den
    Mehrplatz-Abgleich (alle 30 Sekunden).
  - Therapien: „bestätigen ▾“ in der Zeile mit „gegangen“, „abgesagt“, „ausgefallen“ (derselbe Eintrag wie
    im Wochenplan; Recht „Teilnahme bestätigen“).
  - Vollbild: Knopf oder F11 (im Tagesplan) – nur die Tafel über den ganzen Bildschirm, auch mit
    Demo-Hinweis; Esc oder F11 beendet. Beim Abmelden (F12) wird es geschlossen.
- 0.29.10 — Demo-Daten: 23 Bewohner je Wohnbereich (Bauplan Schritt 26h, 08.11.2026):
  - 59 weitere ausgedachte Bewohner (Pflanzennamen mit gleichem Anfangsbuchstaben, alle von L bis Z), also
    23 je Wohnbereich. Zimmer erweitert auf 15 je Bereich (Einzel- und Doppelzimmer gemischt); frei sind je
    zwei Betten: R1 105 Tür und 106 Tür, R2 204 und 206, R3 304 Tür und 306 Tür.
  - 14 weitere ausgedachte Mitarbeiter (Vögel, wie bisher), darunter eine Bereichsleitung für R3 (VK),
    teils Fachkräfte, teils nicht. Bezugspflege für alle neuen Bewohner, je Person bis etwa vier.
  - Daten der neuen Bewohner: Berichte der letzten vier Wochen (wie bisher erzeugt; Übergabe-Einträge nur
    bei jedem neunten, sonst wird die Übergabe unübersichtlich), Grundverordnungen und einige
    Bedarfsmedikamente (`daten/demo/medikation.json`), je eine Hauptdiagnose (manchmal eine Neben-
    diagnose), Ausgang, ein fertiger HEB-Bogen aus den letzten Monaten, drei neue Therapieangebote mit
    Teilnehmern, vier Termine, sechs kurze Biografien, je eine Visite in R2 und R3, vier Einzeltrainings.
  - Ältere Demo-Datenbanken werden beim ersten Start einmal neu geladen; vorher sichert das Programm sie
    als `dokumentationssystem-vor-0.29.10.db` neben der Datenbank (`update.log` vermerkt es).
  - Testlauf: Bildschirmfotos aller Hauptansichten bei 1920 × 1080 und 1366 × 768 (`109_…`) und die Zeit
    für den Aufbau jeder Ansicht (`109_zeiten.txt`).
  - Behoben (0.29.8): Im Fenster „Ärztliche Informationen“ ist das Allergie-Feld breiter, das Fenster
    niedriger (passt auf 768 Pixel Höhe).
  - Behoben (0.29.9): „bestätigen ▾“ auf der Tagesplan-Tafel bricht nicht mehr in zwei Zeilen um.
- 0.29.11 — Aktionsleiste unter der Kopfzeile (Bauplan Schritt 26i, 08.11.2026):
  - Die Kopfzeile bleibt im Aussehen gleich (Fläche, Verlauf, Titel, Logofeld, Demo-Hinweis, „Angemeldet:
    …“, „Abmelden (F12)“) und ist um die Knopfzeile niedriger.
  - Aktionsleiste darunter, gleiche Seitenränder wie die Kopfzeile, Knöpfe im bestehenden Stil (36 px).
    Reihenfolge in allen Bereichen: links ein Rücksprung („← Alle Bewohner“), dann die Hauptaktion (farbig,
    auch gesperrt noch an erster Stelle), dann die übrigen Aktionen; rechts „… drucken …“, ganz außen
    „Entwurf fortsetzen“. Ohne Aktion keine Leiste.
  - Betrifft Bewohner (Übersicht und Akte), Visiten, Aufgaben, Nachrichten, Kalender, Therapie und das Modul
    Ausgangsliste; Bereiche ohne eigene Knöpfe (Übersicht, Tagesplan, Übergabe) haben keine Leiste.
- 0.29.12 — Bereich „Bewohner“ als Liste mit Berichten (Bauplan Schritt 26j, 08.11.2026):
  - Links eine Tabelle der Bewohner: Zimmer und Bett („101 · Fenster“), Name, Bezugspflege (Kürzel in ihrer
    Farbe), Hinweise (dieselben Marken wie auf den Karten, als Text; rot bei Beschluss/Allergie, gelb bei
    „nicht erfasst“). Beim Darüberfahren die ganze Beschreibung. Klick auf einen Spaltenkopf sortiert,
    nochmals umgekehrt; Vorgabe nach Zimmer.
  - Darüber die Filterknöpfe (Alle, Beschluss, Allergie, Nicht erfasst – gezählt im gewählten Bereich), die
    Bereichswahl (eigene Bereiche, alle Bereiche, R1, R2, R3) und ein Suchfeld (Name oder Zimmer).
  - Rechts die Berichte des gewählten Bewohners (neueste oben) mit Zeitraum, Lebensbereich, Schicht, Suche,
    „nur wichtige“, Nachtrag, Drucken. Pfeiltasten blättern, die Berichte wechseln mit.
  - Aktionsleiste: „Neuer Bericht …“ (Hauptaktion, für den gewählten Bewohner; danach bleibt man in der
    Liste), „Akte öffnen“, „Neuer Bewohner …“. Die Akte öffnet auch ein Doppelklick oder Enter; „← Alle
    Bewohner“ führt zurück, die Auswahl bleibt. Rechtsklick: „Akte öffnen“, „Neuer Bericht …“.
- 0.29.13 — Visite: „Nächster Bewohner“ unter der Texteingabe (Bauplan Schritt 26k, 08.11.2026):
  - „◀ Vorheriger“ und „Nächster Bewohner ▶“ stehen nur noch unter dem Feld „Besprochenes“ (nicht mehr oben).
  - „Nächster Bewohner ▶“ ist grau, solange „Besprochenes“ leer ist; Hinweis beim Darüberfahren und beim
    Klick: „Erst den Bericht zu diesem Bewohner schreiben.“ Sobald Text da ist, ist er aktiv.
  - „◀ Vorheriger“ ist immer aktiv; die Liste links bleibt anklickbar. Sonst keine Änderung.
- 0.29.14 — Übersicht neu: „Heute wichtig“ und Bewohnerliste (Bauplan Schritt 26l, 08.11.2026):
  - Links „Heute wichtig“ (380 px), vier Kästen mit Überschrift und Zähler: „Übergaben offen“ (Bewohner und
    Zimmer, Text, „seit 07.11. · AB“), „Termine heute“ (Uhrzeit, Text, Status aus dem Tagesplan; gleiche
    Termine zusammengefasst), „Laufender Dienst · noch offen“ („Spätdienst: 6 von 9 erledigt“, Balken,
    Häkchen je offener Aufgabe, „Alle Aufgaben …“), „Nachrichten ungelesen“ („AB → R1: Betreff“). Leer:
    „Nichts offen“.
  - Klick: Übergabe → Bereich „Übergabe“, Termin → „Tagesplan“, Nachricht → der Verlauf, Überschrift → der
    Bereich. Abhaken im Kasten trägt Kürzel und Uhrzeit ein wie in „Aufgaben“.
  - Rechts die Bewohnerliste nach Zimmer: Zimmer/Bett · Name · Bezug (Farbe mit Kürzel) · Wo (im Haus,
    Heimfahrt/Klinik/unterwegs mit …, unterwegs seit …, in Therapie … bis …, Termin … bis …) · Hinweise
    (Beschluss, Allergie, Medikation, Übergabe, Geburtstag …). Kopf „BEWOHNER R1 · 23 · 17 IM HAUS“, Suche,
    „Meine Bezugsbewohner“, „Meine Einzeltrainings“. Keine Berichte (die zeigt „Bewohner“).
  - Aktionsleiste: „Neuer Bericht …“ (für die gewählte Zeile), „Neue Aufgabe …“, „Neue Nachricht …“, „Neuer
    Bewohner …“ (grün); rechts Bereichswahl (eigene, alle, R1–R3) und „Drucken …“ (Querformat).
  - Unter 1100 px Breite der Übersicht steht „Heute wichtig“ über der Liste (höchstens 40 % der Höhe).
  - Statusleiste (alle Bereiche): „DEMO — ausgedachte Daten · So 08.11.2026 · Spätdienst · Stand 14:32“, dann
    der Farbschlüssel.
- 0.29.15 — Therapie: „Laufende Therapien“ als Anwesenheitstafel (Bauplan Schritt 26m, 08.11.2026):
  - Therapie mit zwei Reitern: „Laufende Therapien“ (zuerst) und „Angebote“ (wie bisher).
  - Tagesleiste 7–21 Uhr: je Angebot ein Balken in seiner Länge (laufend blau, vorbei grau), Überschneidungen
    in eigenen Zeilen, orangefarbener Strich „jetzt“. Klick auf einen Balken: Reiter „Angebote“ beim Angebot.
  - Je laufender Therapie eine Spalte: Name, „Ort · Therapeut-Kürzel“, „bis 11:30 · 5 von 8“, darunter die
    Teilnehmer als Kärtchen (Bereich und Zimmer, Name, Bezugspflege) mit Status „gegangen“, „noch offen“,
    „abgesagt“, „ausgefallen“ als Wort mit Farbe.
  - Spalte „Gleich“ (Beginn in den nächsten 30 Minuten, mit Teilnehmerzahl) und „Noch offen“ (hat begonnen,
    nicht bestätigt; „gegangen“ und „abgesagt“ am Kärtchen). Ohne laufende Therapie: „Gerade läuft keine
    Therapie. Nächste: … um …“. Unten „Rest des Tages“.
  - Bereichswahl „Alle Bereiche“ oder R1–R3; Vorgabe: der eigene Wohnbereich, für die Therapie alle.
    Bestätigen darf, wer es im Tagesplan darf. Neu lesen jede Minute und bei Änderungen anderer Rechner.
  - Demo: zehn tägliche Angebote (Morgenrunde 07:30 bis Abendrunde 20:45) mit 4–7 Teilnehmern aus R1–R3.
- 0.29.16 — Programmsymbol „Hell wie die Kopfzeile“ (Bauplan Schritt 26n, 08.11.2026):
  - Vorlagen `src/DokuLine/Bilder/programmsymbol.svg` (groß, mit Farbverlauf) und `programmsymbol-klein.svg`
    (ohne Verlauf), selbst gezeichnet im Chat.
  - `src/DokuLine/Bilder/DokuLine.ico` mit 16, 20, 24, 32 px (kleine Vorlage) und 40, 48, 64, 256 px (große);
    erzeugt mit `python werkzeuge/symbol-erzeugen.py` (`--vorschau <ordner>` legt PNGs zum Ansehen ab).
  - Symbol der `.exe` (`ApplicationIcon`), des Hauptfensters und aller weiteren Fenster und Dialoge (zentral
    in `Stil.cs`); nicht im Logofeld der Kopfzeile.
- 0.30.0 — Ausgangsliste als übersichtliche Liste (Bauplan Schritt 27, 08.11.2026):
  - Seite „Ausgangsliste“: Spaltenköpfe „Zi., Bewohner, Ausgang, Unterbringung, Heute (von–bis,
    Hz.), Stand jetzt“, darunter eine Zeile je Bewohner. Zu lange Texte enden mit „…“; der volle Text
    steht im Hinweis beim Darüberfahren.
  - Abschnitt „UNTERWEGS (n)“ oben: hellblau hinterlegt mit blauem Balken links; Überfällige rot
    hinterlegt, roter Balken, „überfällig seit 16:05“ und ganz oben. Darunter „IM HAUS ODER
    ABWESEND (n)“ nach Zimmer.
  - „Stand jetzt“: „im Haus“, „unterwegs seit 14:05 (allein), zurück bis 16:05“, „überfällig seit …“,
    „abwesend: …“ oder „Ausgangsbeschränkung …“. Was zu klären ist (Fristen, Widerspruch, nicht
    erfasst) und Infos stehen klein unter der Zeile.
  - Rechts in der Zeile „raus allein“, „raus mit Bewohner“ bzw. „zurück“ (Digitaler Ausgang) und „…“
    mit „Ausgang ändern …“, „Anwesenheit …“, „Unterbringung …“, „Verlauf …“ (auch per Rechtsklick).
    In schmalen Fenstern stehen die Knöpfe unter der Zeile.
  - Filter „nur unterwegs“ (mit Digitalem Ausgang) und „nur mit Beschränkung“.
  - Der Reiter „Ausgang“ in der Akte bleibt eine Karte. Änderungen im Modul:
    `src/DokuLine.Modul.Ausgangsliste/AENDERUNGEN.md`.
- 0.31.0 — E-Mail und Kalender über externe Programme (Bauplan Schritt 28, 08.11.2026):
  - Akte → „Stammdaten“: Zeile „Betreuer“ („nicht erfasst“ in Achtung-Farbe, wenn nichts
    eingetragen ist), Knöpfe „Betreuer …“ (Recht „Bewohner anlegen und bearbeiten“; jede Änderung
    eine neue Fassung) und „E-Mail an Betreuer …“ (gesperrt ohne Betreuer oder ohne E-Mail-Adresse).
  - „E-Mail an Betreuer …“: Hinweis „Keine Gesundheitsdaten unverschlüsselt per E-Mail senden.“ mit
    OK/Abbrechen, dann `mailto:` mit Empfänger und Betreff „Betreuung <Name>“ an das E-Mail-Programm
    des Rechners. Kein eigener Versand, keine Gesundheitsangaben im Betreff.
  - Kalender: „Als .ics speichern …“ speichert die gezeigten Termine (Zeitraum, Bewohner, Bereich/
    Haus) im Standardformat; „.ics übernehmen …“ oder eine .ics-Datei auf den Kalender ziehen →
    Vorschau mit Häkchen, Zuordnung (Art, Bewohner, Kalender Bereich/Haus – Haus nur mit Recht) →
    „Übernehmen“ legt die Termine an (Hinweis „aus einer .ics-Datei …“).
  - Demo `daten/demo/betreuer.json`: ausgedachte Betreuer, Adressen nur `@example.org`, Telefon
    „0000 …“; eine Betreuerin ohne E-Mail, einige Bewohner ohne Eintrag.
- 0.32.0 — Einstellungen neu gegliedert (Bauplan Schritt 29, 08.11.2026):
  - „Einstellungen“ fest ganz unten in der Seitenleiste; öffnet als ganze Seite (kein Fenster mehr).
  - Links Suche und zehn Gruppen mit Symbol: Persönlich (angemeldet als, „Abmelden (F12)“, meine
    Farbe, Seitenmenü eingeklappt), Wohnbereich (Bereich dieses Rechners, Schichtzeiten, „Beschluss
    endet bald“, „HEB fällig“, Logo, Zimmer und Betten), Module (Ausgangsliste mit Küchendienst und Digitalem Ausgang),
    Aufgaben (Standardaufgaben), Fahrzeuge (Fahrzeugliste), Vorlagen (HEB-PDFs A/B/C mit Zuordnung),
    Medikamente (Datenbank, Nachschlage-Ansicht), Verwaltung (Mitarbeiter, Bereiche und Positionen,
    Rechte, Ärzteverzeichnis), System (Ort der Datenbank, Sicherung – noch nicht gebaut, Update, Demo-Daten
    zurücksetzen), Über (Programmname und Leittext, Version, Tastenkürzel F12 und F11, Demo-Hinweis,
    Bausteine und Lizenzen, WebView2).
  - Seitenleiste unten: „Einstellungen“, darunter „Abmelden (F12)“, Arbeitsplatz und Version (eingeklappt:
    die Symbole).
  - Je Einstellung eine Karte: Name, ein Satz Erklärung, rechts (oder darunter) das Bedienelement.
    Häkchen und Zahlen gelten beim Ändern, Textfelder (Bereich, Schichtzeiten) beim Verlassen oder
    mit Enter; Fehler stehen oben in der Gruppe (rot hinterlegt, mit Text).
  - Suche über Name, Erklärung und Gruppe: Trefferliste links, Enter oder Klick springt hin und hebt
    die Karte mit einem Balken hervor.
  - Ohne Recht: Karte grau mit dem Satz „Dafür fehlt die Berechtigung.“ (bzw. „Bitte zuerst
    anmelden.“), Knöpfe sichtbar mit Grund.
  - Ort der Datenbank und Demo-Daten zurücksetzen fragen vorher; Hinweis in Gefahrenfarbe mit
    „Achtung:“.
  - Umgezogen (keine zweite Stelle mehr): „Demo-Daten zurücksetzen“ aus der Seitenleiste,
    „Standardaufgaben …“ von der Seite „Aufgaben“.
- 0.33.0 — Assistenzkraft und Fachkraft (Eintrag in `IDEEN.md`, bestätigt von Tobi direkt, 08.11.2026):
  - Qualifikation „Assistenzkraft“ oder „Fachkraft“: im Anmeldefenster, in der Mitarbeiterliste (Spalte
    „Qualifikation“) und im Formular (Auswahl statt Häkchen; neue Mitarbeiter: Assistenzkraft).
  - Visiten: in der Rechte-Tabelle „nur Fachkräfte“ (auch in bestehenden Datenbanken einmal gesetzt). Für
    Assistenzkräfte ist „Visite beginnen …“ grau: „Nur für Fachkräfte.“
  - Neues Recht „Bedarfsmedikation geben“ (Bereichsleitung, Mitarbeiter Wohnbereich; die Leitungen wie immer).
  - „Bedarf gegeben …“ an jeder Bedarfsverordnung, die heute gilt (Medikamentenplan → Zeile → Knopf): Uhrzeit
    (heute, nicht in der Zukunft), Menge (Einzeldosis vorbelegt), Anlass (aus der Verordnung), „In Absprache
    mit“ (aktive Fachkräfte, eigener Bereich zuerst). Oben „Heute 1× gegeben: 09:15 (1 Stück, AB)“ und
    „Höchstens: 2 in 24 Stunden“. Ist die Höchstmenge in den 24 Stunden davor erreicht: Warnung, eingetragen
    wird erst mit „Trotzdem eintragen“.
  - Jede Gabe ist ein Bericht in der Akte (Lebensbereich 5, Verfasser = wer gegeben hat): „Bedarfsmedikation
    gegeben um 14:30: Lorazepam … 1 Stück. Anlass: … In Absprache mit Anna Bachstelze (AB).“
  - Datenbank-Schritt 28: Tabelle `bedarfsgaben` (wird nie überschrieben). Demo: sieben Gaben in R1–R3, vier
    davon von Assistenzkräften mit Absprache.
- 0.33.1 — Korrektur aus der Fehlersuche vom 08.11.2026: Neue Rechte werden beim Start nur noch für Positionen
  vorbelegt, die es gibt. Vorher brach der Start ab, wenn eine vorgegebene Position umbenannt oder gelöscht war
  (Fremdschlüssel). Test: `Vorbelegen_neuer_Rechte_uebersteht_umbenannte_und_geloeschte_Positionen`.
  - **Sicherheitsnetz für Updates** (Wunsch Tobi, 09.11.2026: „sichere Testungen für die Zukunft“):
    - Kern (`UpdateSicherheitTests`): Eine Demo-Datenbank wird verändert wie von einem Nutzer (alle Positionen
      und Bereiche umbenannt, eigene Einträge, AB und KL ausgeschieden, drei Bewohner entfernt). Dann wird
      jeder Merker eines einmaligen Schritts (Tabelle `meta`: Rechte vorbelegen, Demo-Daten nachholen …)
      einzeln und alle zusammen entfernt und neu gestartet. Der Start darf nicht abbrechen, der Merker muss
      danach wieder gesetzt sein, ein zweiter Start holt nichts mehr nach. Die Merker liest der Test aus der
      Datenbank – jeder künftige Schritt ist automatisch dabei (heute 35). Gegenprobe: Ohne die Korrektur
      meldet der Test 5 von 35 Schritten mit „FOREIGN KEY constraint failed“.
    - Oberfläche (`UpdateSicherheitOberflaechenTests`, nur in GitHub Actions): Mit so einer Datenbank öffnet
      sich jeder Bereich der Seitenleiste und jeder Reiter der Akte, als TST und als EF. Die Bereiche kommen
      aus der Seitenleiste – neue Bereiche sind automatisch dabei.
    - Die Änderungen des Nutzers stehen einmal in `tests/DokuLine.Tests/NutzerStand.cs` (beide Testprojekte).
- 0.33.2 — Korrekturen aus der Fehlersuche vom 08.11.2026 (jede mit Test, der vorher rot war):
  - Übersicht: baut sich beim Schichtwechsel neu auf (vorher nur beim Datumswechsel) und legt die
    Standardaufgaben der Schicht selbst an (vorher erst beim Öffnen von „Aufgaben“).
  - Doppelklickschutz (`Doppelklickschutz` in den Bausteinen): Nach dem Abhaken im Kasten „Laufender Dienst“ und
    nach „gegangen“/„abgesagt“ auf der Tafel zählt ein weiterer Klick innerhalb der Doppelklickzeit von Windows
    nicht.
  - Therapie-Tafel: Bildlauf je Spalte und quer bleibt beim Neuaufbau (jede Minute, nach jedem Klick).
  - Bewohner: Berichte rechts in einem rollbaren Rahmen (vorher abgeschnitten); zurück aus der Akte bleibt deren
    Bewohner gewählt.
  - Bereichswahl in Übersicht, Bewohner und Tafel folgt umbenannten und neuen Bereichen (`Auswahl.Abgleichen`).
  - Bedarf: Uhrzeit kurz nach Mitternacht (später als jetzt + 5 Minuten, höchstens 12 Stunden zurück) gilt für
    gestern. Fenster: „In den letzten 24 Stunden 2× gegeben: gestern 23:50 (…), 09:15 (…)“; Karte: „in 24 Std.
    2× gegeben“. Höchstmenge: Summe der Mengen in jedem 24-Stunden-Fenster, in dem die neue Gabe liegt, über alle
    Bedarfsverordnungen desselben Medikaments; ohne Zahl vorne „… kann die Menge nicht prüfen“. Absprache-Kürzel
    wie in der Mitarbeiterliste. Keine Sonderregel mehr für Administratoren.
  - Visite: „Visite abschließen“ prüft das Recht „Visiten“ dessen, der abschließt.
  - Demo: Vor jedem Nachholen von Demo-Daten eine Sicherungskopie (`…-vor-demo-nachholen.db`, mit Startprotokoll);
    beim Nachholen der Medikation bleiben keine Bedarf-Berichte ohne Gabe stehen (vorher doppelt).
- 0.33.3 — letzte Korrekturen aus der Fehlersuche vom 08.11.2026:
  - Übersicht: Kasten „Nachrichten ungelesen“ zählt alle ungelesenen Nachrichten wie die Seitenleiste (vorher die
    Verläufe). Aktionsleiste: Die Bereichswahl wird bei wenig Platz schmaler, unter 90 px ausgeblendet; sie verdeckt
    nie „Drucken …“ oder andere Knöpfe.
  - Therapie: Klick auf einen Balken der Tagesleiste zeigt das Angebot auch bei einem anderen gefilterten Anbieter
    (Filter dann „alle Anbieter“). Höchstens ein Termin je Wochentag und Angebot (die Teilnahme gilt je Tag). Knöpfe
    melden beim Löschen ihren Hinweistext ab (vorher blieben sie im Speicher).
  - Betreuer: dritter Zustand „keine gesetzliche Betreuung“ (Häkchen im Formular; Datenbank-Schritt 29, Spalte
    `keine`); neue Fassung, die alte bleibt im Verlauf. „E-Mail an Betreuer …“ dann gesperrt. Demo: Ginster.
    E-Mail-Adressen mit `#` oder `%` werden abgelehnt (sie veränderten die mailto-Adresse).
  - .ics: ein Termin, der sich nicht lesen lässt, wird übersprungen; `P2W` (Wochen); Ende gleich Beginn → eine Stunde;
    `TZID=UTC` gilt als UTC; Schreiben unabhängig von der Sprache des Rechners; UID mit zufälliger Kennung je
    Datenbank (`ics_kennung` in `meta`); Übernahme mehrerer Termine in einem Zug; kein Absturz ohne Lese- oder
    Schreibrecht; Hereinziehen öffnet die Vorschau erst nach dem Ziehen.
  - Einstellungen: Untermodule bleiben gesperrt, solange das Modul aus ist (auch nach erneutem Öffnen); Schichtzeiten
    unverändert verlassen ist kein Fehler; nach den Fenstern Mitarbeiter, Bereiche und Positionen, Rechte und
    Standardaufgaben werden Werte und Sperren neu gelesen; Fristen und Update prüfen das Recht auch im Handler.
  - Dialoge zeigen das Programmsymbol (`Stil.Dialogsymbol`, gesetzt in `Fenster.Grundform`, `Dialog.Zeigen`, beim
    Anmelden und im Bewohnerformular).

- 0.33.4 — Bauplan Schritt 29a „Personenverzeichnis“:
  - Datenbank-Schritt 30: Tabelle `aerzte` mit Feld `gruppe` (arzt, betreuer, angehoerige, forensik, fosa, sonstige),
    dazu Art, Mobil, Anschrift, Erreichbarkeit, Hinweis; Namen je Verzeichnis eindeutig; neue Tabelle
    `person_zuordnungen` (Person, Bewohner, Rolle).
  - Einstellungen → „Verzeichnisse“ (eigene Gruppe): oben die Verzeichnisse mit Zähler (aktive), Suche, Liste mit
    Spalten je Verzeichnis; „Neu …“, „Öffnen …“, „Inaktiv / wieder aktiv“ in der Aktionsleiste. Doppelklick öffnet die
    Person: Felder je Verzeichnis, darunter „zugeordnet zu …“ mit „Bewohner zuordnen …“ (Bewohner und Rolle bzw.
    Beziehung, Vorschläge), „Zuordnung entfernen“, „Akte öffnen“. Ohne Recht nur ansehen.
  - Rechte: Angehörige mit „Bewohner anlegen und bearbeiten“, alles Übrige mit „Personenverzeichnis verwalten“
    (bisher „Ärzteverzeichnis verwalten“, gleiche Vorbelegung); auch im Kern geprüft.
  - Übernahme: die Betreuer aus den Stammdaten (neueste Fassung, nicht „keine gesetzliche Betreuung“) werden Personen
    „Betreuer“ mit Zuordnung – einmal je Datenbank, gleicher Name = dieselbe Person.
  - Demo (`daten/demo/personen.json`): 35 Angehörige (etwa jeder zweite Bewohner, eine Person bei zwei Bewohnern),
    je eine ausgedachte Forensik-Klinik und FOSA mit drei Zuständigen, drei sonstige Stellen; Betreuer aus
    `betreuer.json` (Heike Hecht und Bernd Barsch je für zwei Bewohner).
  - Die Karte „Ärzteverzeichnis“ unter „Verwaltung“ entfällt; „Arzt anlegen …“ an jeder Arztauswahl bleibt.

- 0.33.5 — Bauplan Schritt 29b „Bereich Dokumentation“:
  - Seitenleiste: „Bewohner“ heißt „Dokumentation“ (gleiche Stelle). Links die Bewohner (240 px, einklappbar auf
    36 px) nur mit „Nachname, Vorname“, alphabetisch, ohne „Alle Bewohner“; Bereichswahl und Suche darüber; die Wahl
    bleibt beim Zurückkehren. Doppelklick oder Enter öffnet die Akte.
  - Rechts `DokumentationAnsicht`: Tagesbalken („Montag, 09.11.2026 · 3 Berichte“), je Bericht eine Zeile
    (`BerichtZeile`) mit Uhrzeit und Schichtwort, Marken, Text (kompakt höchstens zwei Zeilen, Klick klappt auf),
    Kürzel und eingerückten Nachträgen; Rand 1,5 px in der Schichtfarbe, Farbschlüssel in der Filterleiste.
    Rechtsklick: „Nachtrag schreiben …“, „Aufgabe daraus machen …“.
  - Marken: „Wichtig“, „Medikation geändert“ (Berichtsart „Medikation“ ohne Bedarfsgabe; Zeile leicht rot),
    „Bedarfsgabe“ (Bericht aus „Bedarf gegeben …“); „Übergabe“ als Wort-Zeichen. Visiten mit fettem Anfang.
  - Filter (`DokuFilter`, Prüfung in `DokuRegeln.Passt`): Zeitraum Heute · 7 Tage (Vorgabe) · 30 Tage · frei, Knöpfe
    Wichtig · Medikation geändert · Bedarfsgabe · Visite, weitere Filter Übergabe, mit Nachtrag, Berichtsart,
    Lebensbereich, Schicht, Verfasser, Suche mit Hervorhebung. Gesetzte Filter als Marken mit „×“, „Filter
    zurücksetzen“. Der Filter bleibt beim Wechsel des Bewohners.
  - Gespeicherte Filter mit eigenem Namen je Kürzel (in `meta`, gelten an jedem Arbeitsplatz); Rechtsklick löscht.
  - Aktionsleiste: „Neuer Bericht …“ (Hauptaktion), „Bedarf gegeben …“ (bei mehreren Bedarfsverordnungen erst
    wählen), „Akte öffnen“, „Ansicht: kompakt/vollständig“, „Drucken …“ (druckt, was der Filter zeigt).
  - Berichtsart: Datenbank-Schritt 31 (Spalte `art`, Tabelle `berichtsarten` mit Alltag · Krise · Arztkontakt ·
    Visite · Medikation · Konflikt · Ausgang/Abgängigkeit · Angehörigenkontakt · Betreuer/Behörde · Besonderes
    Vorkommnis). Auswahl im Formular „Neuer Bericht“, Vorgabe „Alltag“. Visiten bekommen „Visite“, Bedarfsgaben
    „Medikation“. Vorhandene Berichte: Visiten und Bedarfsgaben werden eingeordnet, alles andere „Alltag“; die Demo
    ordnet ihre Berichte nach Stichworten ein.
  - Einstellungen → „Bereiche, Positionen, Berichtsarten …“: Berichtsarten anlegen, umbenennen (die Berichte gehen
    mit), stilllegen, löschen (nur ungenutzt). Alltag, Visite und Medikation bleiben fest.
  - Akte: Reiter „Berichte“ entfällt; im Kopf „← Dokumentation“ und „Berichte ansehen“. Nach „Neuer Bericht …“ aus
    Akte oder Übersicht zeigt die Dokumentation den Bewohner mit dem neuen Bericht.
  - Die Akte steht in einer eigenen Datei (`AkteAnsicht.cs`); `BerichteAnsicht.cs` (Karten, Liste) entfällt.

- 0.33.6 — Bauplan Schritt 29c „Fehlendes ausblenden statt ausgrauen“:
  - `RechteRegeln.FehltRecht`: „Dafür fehlt die Berechtigung.“ und „Nur für Fachkräfte.“ bedeuten ausblenden;
    alle anderen Gründe (ohne Anmeldung, kein Bewohner gewählt, kein Betreuer …) lassen den Knopf grau mit Hinweis.
  - `Knopf`: ohne Recht unsichtbar, auch wenn der Aufruf ihn zeigen will; mit dem Recht kommt er wieder. Damit gilt
    das für alle Knöpfe, die über `Stil.Sperren`/`Recht.Anwenden` gesperrt werden.
  - Aktionsleiste: ausgeblendete Knöpfe nehmen keinen Platz; ohne sichtbaren Knopf entfällt die Leiste.
  - Seitenleiste: „Visiten“ (eigenes Recht) verschwindet ohne Recht; steht man gerade dort, geht es zur Übersicht.
  - Menüs: „Neuer Bericht …“ (Übersicht, Dokumentation), „Nachtrag schreiben …“ (Bericht), Teilnahme im Wochenplan.
  - Einstellungen: Karten ohne Recht (oder deren einziger Knopf ohne Recht) fehlen, auch in der Suche; Gruppen ohne
    erlaubte Karte verschwinden. „Persönlich“, „Über“ und „Verzeichnisse“ (ansehen dürfen alle) bleiben.
  - Neu aufgebaut wird nach An- und Abmelden und nach Änderungen an Mitarbeitern oder Rechten.

- 0.33.7 — Bauplan Schritte 29d „Wohnbereich einmal in der Übersicht wählen“, 29q „Aufgaben gehören zu einem Bewohner
  oder einem Bereich“, 29r „Kopfleisten der Kästen“:
  - `Arbeitsbereich.Gewaehlt` (Bausteine) hält den gewählten Bereich; `BereichswahlRegeln` (Kern): Knöpfe je Recht,
    Vorgabe, wer dazugehört, „eingesprungen“. Drei neue Rechte: „Wohnbereiche wählen“ (alle Positionen mit
    Wohnbereichsbezug und Therapie), „Bereich Therapie wählen“ (Therapie), „Alle Bereiche zusammen ansehen“
    (Leitungen, Verwaltung, Administrator); in bestehenden Datenbanken einmal vorbelegt.
  - Übersicht: oben „Wohnbereich:“ mit großen Umschaltknöpfen (`BereichUmschalter`) statt der Bereichswahl in der
    Aktionsleiste; ohne Anmeldung nur der Bereich des Rechners.
  - Kopfzeile: vorn immer der gewählte Bereich („R1 · …“), daneben „eingesprungen“ (Hinweis-Farbe), wenn es nicht der
    Bereich des Rechners ist.
  - Es folgen dem gewählten Bereich: Dokumentation (Bereichswahl entfällt; ein Bewohner aus einem anderen Bereich, z. B.
    aus dem Verzeichnis, steht zusätzlich in der Liste), Übergabe (allgemeine Einträge stehen immer da), Tagesplan
    (Häkchen „alle Bereiche“ entfällt), Visiten (nur in einem Wohnbereich), Aufgaben mit Zähler und „Laufender
    Dienst“, Kalender (Termine von Bewohnern), Laufende Therapien (Bereichswahl entfällt), Ausgangsliste, Standardaufgaben.
  - Aufgaben: Datenbank-Schritt 32 (Spalte `bereich` bei Aufgaben und Standardaufgaben). „Neue Aufgabe“ und
    „Standardaufgabe“: Bewohner oder Bereich (R1, R2, R3, Therapie; Vorgabe der gewählte). Mit Bewohner gehört sie
    zu dessen Wohnbereich, auch nach einem Umzug. Bestehende Aufgaben ohne Bewohner kommen zu R1, bestehende
    Standardaufgaben gibt es danach in jedem Wohnbereich. Umbenennen eines Bereichs nimmt beides mit.
  - Demo: Bereichsaufgaben für R1, R2 und R3; Standardaufgaben je Wohnbereich.
  - Kopfleiste `KastenKopf` (heller Akzent, oben abgerundet, Trennlinie, Überschrift links, Zähler rechts, beim
    Darüberfahren kräftiger) für die vier Kästen und die Bewohnerliste der Übersicht und die Spalten der Laufenden
    Therapien; `docs/gestaltung.md` ergänzt.

- 0.33.8 — Bauplan Schritt 29s, Teil 1 (Aufräumen, ohne neue Funktion):
  - `.editorconfig` (UTF-8, Einrückung 4, Zeilenende ohne Leerzeichen, Schreibweise des Projekts für kurze Blöcke)
    und im Windows-Build „Format prüfen“ (`dotnet format whitespace --folder --verify-no-changes`); die Abweichungen
    sind einmal angeglichen.
  - `werkzeuge/code-masse.py`: Zeilen, Kommentare (mit Verlauf), Dateien über 400 Zeilen, Methoden über 40 Zeilen,
    Zeilen über 120 Zeichen – als Hinweis in jedem Build.
  - `docs/aufbau.md`: Projekte, Weg der Daten, wo man was findet, Glossar.
  - Code-Maße vorher (Programmcode in `src/`, 09.11.2026, Stand 0.33.7): 33.480 Zeilen, 2.038 Kommentarzeilen
    (davon 301 mit Verlauf), 21 Dateien über 400 Zeilen, 88 Methoden über 40 Zeilen, 2.057 Zeilen über 120 Zeichen.

- 0.33.9 — Bauplan Schritt 29s, Teil 2 (Aufräumen, ohne neue Funktion):
  - Verlauf aus den Kommentaren entfernt (Schrittnummern, Versionsangaben, „vorher …“): von 301 auf 12 Zeilen; die
    übrigen nennen Altlasten, die noch gelten (z. B. „Ältere Installationen bis 0.29.1 suchen …“).
  - Lange Dateiköpfe (Selbst-Update, Einstellungsdatei, Speicher) gekürzt; die Erklärungen stehen in
    `docs/technik.md`.
  - Code-Maße nachher: 33.442 Zeilen, 2.000 Kommentarzeilen (12 mit Verlauf).

- 0.33.10 — Bauplan Schritt 29s, Teil 3 (Aufräumen, ohne neue Funktion):
  - `IBereichSeite` (Titel, Kopftext, Knöpfe, Zeigen): alle Seiten der Seitenleiste, Platzhalter und Module;
    `Hauptfenster.BereichWaehlen` ruft jede Seite gleich auf, statt einer `if`-Kette je Bereich.
  - Große Dateien geteilt: `Hauptfenster` (Aufbau, Anmeldung, Abläufe, Seitenleiste) und `EinstellungenSeite`
    (Aufbau, Karten, Handeln) als Teil-Klassen; Nebenklassen von Kalender, Medikation, Visiten, Aufgaben, Übersicht,
    Nachrichten und Therapie in eigene Dateien.
  - Code-Maße nachher (vorher in Klammern): 33.574 Zeilen (33.480), 2.010 Kommentarzeilen (2.038), davon 12 mit
    Verlauf (301), 14 Dateien über 400 Zeilen (21), 87 Methoden über 40 Zeilen (88), rund 2.050 Zeilen über 120
    Zeichen (2.057).

- 0.33.11 — Bauplan Schritt 29e:
  - `KuvertKnopf` (Bausteine): Kuvert-Symbol, roter Kreis mit „!“ und Zahl bei ungelesenen Nachrichten, Tooltip,
    mit Tastatur bedienbar; nur sichtbar, wenn jemand angemeldet ist.
  - `KopfBereich.NachrichtenSetzen`; das Hauptfenster zählt wie bisher und setzt Seitenleiste und Kuvert.
  - Der Nachrichten-Hinweis über der Liste im Bereich „Dokumentation“ ist entfernt (Klasse `NachrichtenHinweis`
    gelöscht).

- 0.33.12 — Bauplan Schritt 29f:
  - Demo-Daten: eigene Texte für Berichte mit Übergabe (`berichte.json`, `uebergabe.berichte`); Test, dass keine
    Demo-Übergabe „Keine Besonderheiten“ oder „Ruhiger Dienst“ trägt.
  - Angebot: „Teilnehmer“ mit „(Anzahl)“ (`Formular.ZeileMitZusatz`); Fehlertext und Verlauf ohne „höchstens“.
  - Dokumentation: Aufgaben-Kurzliste entfernt (Klasse `AufgabenKurzliste` gelöscht); Bewohnerleiste als
    `KartenFlaeche`, Namen in `RundeListe` (gewählte Zeile abgerundet, heller Akzent, mit Fokus umrandet).

## Offene Punkte

- Aufräumen (29s): 45 Dateien beginnen noch mit vier bis sieben Kommentarzeilen statt höchstens drei; rund 2.000
  Zeilen sind länger als 120 Zeichen, 87 Methoden länger als 40 Zeilen (vor allem Aufbau von Fenstern) und 14
  Dateien länger als 400 Zeilen (am größten `Speicher.cs` und `Demodaten.cs`). Sie werden nach der Pfadfinderregel
  kleiner, sobald die Datei geändert wird; das Skript im Build zeigt den Stand.

- Fehlersuche vom 08.11.2026: abgearbeitet in 0.33.1–0.33.3. Bewusst nicht geändert: Die Übersicht liest die
  Termine des Tages für den Kasten „Termine heute“ ein zweites Mal (der Kasten braucht auch abgesagte Termine, die
  Bewohnerliste nicht); gemessen ist das nur im Windows-Testlauf, nicht auf einem echten Rechner.
- Nicht automatisch geprüft (nur von Hand): dass Knöpfe ihren Hinweistext beim Löschen abmelden; dass das
  Hereinziehen einer .ics-Datei den Explorer nicht mehr festhält.

- Übersichtlichkeit mit 23 Bewohnern je Bereich (Schritt 26h, nicht umgebaut, nur festgehalten; Befund aus
  den Bildschirmfotos `109_…` und den Zeiten des Testlaufs von 0.29.10):
  - **Ausgangsliste** zeigt alle 69 Bewohner aller Bereiche; einen Filter nach Wohnbereich hat sie nicht.
    Ihr Aufbau dauert im Testlauf **6–9 Sekunden** (alle anderen Ansichten unter 0,25 Sekunden); bei
    1366 × 768 passen zwei Bewohner auf den Bildschirm.
  - **Küchendienst** geht reihum über alle belegten Zimmer aller drei Bereiche (42); je Wohnbereich
    getrennt wäre näher an der Wirklichkeit.
  - ~~**Übersicht:** Bei 1920 × 1080 sind 12 der 23 Karten ohne Bildlauf zu sehen; „Termine heute“ zählt bei
    Gruppenterminen alle Namen auf (Spaziergang: 23 Namen).~~ Erledigt mit 0.29.14 (Liste, höchstens drei
    Namen je Termin).
  - **Übergabe:** Jeder Eintrag ist eine große Karte; bei 1366 × 768 passen zwei der neun offenen Einträge
    auf den Bildschirm.
  - **Tagesplan:** Ein Gruppentermin („Spaziergang mit der Wohngruppe“) steht je Bewohner in einer eigenen
    Zeile (23 Zeilen).
- **Teil 2: Repository umbenennen — sobald dieses Release erschienen ist** (`KhaosDE/Dokumentationssystem`
  → `KhaosDE/DokuLine`, kein Claude-Code-Auftrag). Installationen älter als 0.29.2 kennen nur die
  alte Download-Adresse; sie sollten vorher einmal gestartet werden, damit sie 0.29.2 holen.
- Nachschlage-Ansicht (0.24.0): Mit der echten Seite des BfArM (anonyme Anmeldung, Weiterleitungen)
  nur so weit geprüft, dass die Adressen stimmen (08.11.2026); in den automatischen Tests wird das
  Laden ersetzt. Bitte in der Testliste von Hand prüfen. Ob die BfArM-Suche auf weitere Adressen
  (außerhalb von bfarm.de und pharmnet-bund.de) springt, ist offen – solche Links öffnen dann im
  normalen Browser.
- Bewohner löschen, entlassen oder als ausgezogen markieren gibt es noch nicht (damit würde auch das Bett
  frei, Schritt 26e; war nicht
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

- 09.11.2026 (0.33.12): Die Demo-Übergaben bekommen eigene Texte statt einer Auswahl aus den Tagesberichten: So kann
  kein zufälliger Bericht „Keine Besonderheiten“ zur Übergabe werden. Im Datenmodell heißt die Zahl weiter
  `Hoechstens`, nur die Oberfläche sagt „Teilnehmer“ – keine Änderung an der Datenbank nötig.

- 09.11.2026 (0.33.11): Das Kuvert zeigt die Zahl zusätzlich als Text neben dem roten Kreis, damit die Farbe nicht
  allein die Information trägt (`docs/gestaltung.md`). Ohne Anmeldung steht kein Kuvert da, weil es dann niemanden
  gibt, dessen Nachrichten gezählt werden. Ab 100 ungelesenen steht „99+“.

- 09.11.2026 (0.33.10): Lange Zeilen und lange Methoden werden nicht auf einmal umgebrochen: Ein maschineller Umbruch
  macht den Code schlechter lesbar (bei 29b ausprobiert), ein Umbau von Hand aller 2.000 Stellen wäre ein großes
  Risiko ohne Nutzen für die Vorführung. Geteilt wurden die Dateien, die der Eintrag zuerst nennt.

- 09.11.2026 (0.33.8): Geprüft wird zunächst nur die Formatierung (Leerzeichen, Einrückung), nicht der ganze
  Stil: Stilregeln als Fehler hätten bei „Warnungen sind Fehler“ den Build sofort angehalten. Die Größen meldet das
  Skript als Hinweis (Vorgabe des Eintrags). Gezählt wird der Programmcode in `src/` ohne Tests.

- 09.11.2026 (0.33.7): „Leitung“ für den Knopf „Alle“ sind Geschäftsführung, Hausleitung und Verwaltung (sie haben
  schon alle Rechte) und der Administrator; die Bereichsleitung sieht die Wohnbereiche. In der Rechte-Tabelle änderbar.
- 09.11.2026 (0.33.7): Die Wahl gilt je Programmfenster bis zum Abmelden; das Anmelden eines anderen Kürzels ohne
  Abmelden behält sie, solange der Knopf erlaubt ist.
- 09.11.2026 (0.33.7): „eingesprungen“ erscheint nicht bei „Alle“ – „Alle“ ist kein Bereich, in den man einspringt.
- 09.11.2026 (0.33.7): Wo eine Aufgabe ohne Bewohner angelegt wurde, hat die Datenbank nie gespeichert; sie kommen
  beim Update deshalb alle zu R1 (Vorgabe des Eintrags). Bestehende Standardaufgaben gelten danach in jedem
  Wohnbereich, damit kein Bereich seine Rundgänge verliert.
- 09.11.2026 (0.33.7): Übergabe-Einträge „Wohnbereich allgemein“ und Kalendertermine ohne Bewohner haben keinen eigenen
  Bereich; sie stehen in jedem Bereich. Nachrichten an Teams bleiben, wie sie sind (jeder liest die seiner Teams) –
  das Einspringen ändert nicht, wer Empfänger ist.
- 09.11.2026 (0.33.7): Visiten gehen nur in einem Wohnbereich; bei „Alle“ oder „Therapie“ ist „Visite beginnen …“ grau
  mit „Bitte zuerst in der Übersicht einen Wohnbereich wählen.“

- 09.11.2026 (0.33.6): Ohne Anmeldung bleibt alles grau mit „Bitte zuerst anmelden.“ statt ausgeblendet: Es gibt
  dann noch niemanden, dem ein Recht fehlt, und man sieht, was es nach der Anmeldung gibt.
- 09.11.2026 (0.33.6): Ausgeblendet wird zentral im `Knopf` (nach dem Sperrgrund) statt an jeder der rund 50
  Stellen einzeln – so kann keine Stelle vergessen werden. Felder (Häkchen, Listen) in gesperrten Fenstern bleiben
  „nur ansehen“, weil der Inhalt angesehen werden darf.
- 09.11.2026 (0.33.6): Nur „Visiten“ hat als Bereich ein eigenes Recht; die übrigen Bereiche darf jeder ansehen und
  bleiben in der Seitenleiste.

- 09.11.2026 (0.33.5): „Medikation geändert“ ist ein Bericht der Art „Medikation“, der keine Bedarfsgabe ist. Das
  Programm erkennt Änderungen nicht selbst aus dem Text (Regel 6); wer eine Änderung dokumentiert, wählt die Art.
- 09.11.2026 (0.33.5): Marken der Bewohner (Beschluss, Allergie, nicht erfasst, Übergabe, abwesend …) stehen nur noch in
  der Übersicht; die Dokumentation zeigt links nur Namen (Vorgabe Tobi). Die Filterknöpfe nach Beschluss/Allergie der
  alten Bewohnerliste entfallen damit.
- 09.11.2026 (0.33.5): Die Berichtsart darf sich nachträglich ändern (Einordnen vorhandener Berichte, Umbenennen einer
  Art); der Datenbankschutz gilt weiter für alle übrigen Spalten eines Berichts. Alltag, Visite und Medikation vergibt
  das Programm selbst und bleiben fest.
- 09.11.2026 (0.33.5): Vorhandene echte Berichte ordnet das Programm nicht nach Stichworten ein (nur Visiten und
  Bedarfsgaben, die es sicher erkennt); die Stichwort-Zuordnung gilt nur für die ausgedachten Demo-Berichte.
- 09.11.2026 (0.33.5): Gespeicherte Filter liegen in `meta` je Kürzel statt in einer eigenen Tabelle – einfacher, und
  sie gelten an jedem Arbeitsplatz.
- 09.11.2026 (0.33.5): „Neuer Bewohner …“ steht nur in der Übersicht; die Aktionsleiste der Dokumentation hat genau die
  fünf Knöpfe aus dem Eintrag.
- 09.11.2026 (0.33.5): Jede Berichtszeile ist ein Textfeld (RichTextBox) für Fettdruck und Hervorhebung; der Richtwert
  „15 Berichte ohne Bildlauf bei 1920 × 1080“ ist nur im Windows-Test sichtbar (Foto `122_dokumentation`).

- 09.11.2026 (0.33.4): Versionsnummern werden fortlaufend vergeben (jede Auslieferung die nächste: 29a = 0.33.4,
  29b = 0.33.5 …), weil 0.33.3 schon die Korrekturen der Fehlersuche trägt und 29s in mehreren Pull Requests kommt; die
  Spalte „Version“ im Bauplan ist nur noch ein Richtwert. Ab Schritt 30 beginnt 0.34.0.
- 09.11.2026 (0.33.4): Neuer Code folgt `docs/code-regeln.md` (Kommentare ohne Verlauf, Dateiköpfe höchstens drei
  Zeilen). Einzelne Zeilen liegen knapp über 120 Zeichen (lange SQL- und Parameterlisten); das Messskript kommt mit 29s.
- 09.11.2026 (0.33.4): Personenverzeichnis technisch in der bisherigen Tabelle `aerzte` mit Feld „Gruppe“ (vom Eintrag
  erlaubt, einfacher); im Programm heißt der Datensatz `Person`. Ärzte werden weiter mit dem Namen eingetragen.
- 09.11.2026 (0.33.4): Bis Schritt 30 („Reiter Betreuung“) zeigt die Akte den Betreuer weiter aus ihren eigenen
  Fassungen; das Verzeichnis hat sie einmal übernommen. Ein in der Akte geänderter Betreuer erscheint erst mit
  Schritt 30 automatisch im Verzeichnis.
- 09.11.2026 (0.33.4): „Öffnen …“ statt „Ändern …“: Ansehen dürfen alle (Telefonnummern nachschlagen), ändern nur mit
  Recht. Zugeordnete Personen werden nie gelöscht, nur inaktiv gesetzt.
- 09.11.2026 (0.33.4): Forensik- und FOSA-Zuständige ohne Zuordnung in der Demo; die Zuständigkeit je Bewohner kommt
  mit Schritt 30a.

- 09.11.2026 (0.33.3): „Keine gesetzliche Betreuung“ ist eine eigene Fassung mit Merkmal (Datenbank-Schritt 29), kein
  leerer Name – so bleiben die drei Zustände unterscheidbar (Regel 5).
- 09.11.2026 (0.33.3): .ics: Nur `TZID=UTC` (und gleichbedeutende Namen) wird umgerechnet; andere Zeitzonen gelten
  weiter als Ortszeit – die Demo geht von Dateien aus derselben Zeitzone aus; eine Umrechnung aller Zonen hinge von
  den Zeitzonen-Daten des Rechners ab. Eine nicht lesbare Dauer ergibt eine Stunde (wie ohne Dauer).
- 09.11.2026 (0.33.3): Höchstens ein Termin je Wochentag und Angebot, statt die Teilnahme je Termin zu speichern –
  kleiner Eingriff, keine neue Tabelle; für einen zweiten Termin am selben Tag gibt es ein eigenes Angebot.

- 09.11.2026 (0.33.2): Doppelklickschutz über die Zeit (Doppelklickzeit von Windows), nicht über stehenbleibende
  erledigte Zeilen – einfacher, gilt für Haken und Tafel gleich.
- 09.11.2026 (0.33.2): Bedarf nach Mitternacht: Eine Uhrzeit später als jetzt + 5 Minuten gilt für gestern, wenn
  sie höchstens 12 Stunden zurückliegt; sonst „in der Zukunft“. Die Anzeige zeigt die letzten 24 Stunden – wie die
  Höchstmenge rechnet. Ersetzt die Entscheidung „Eine Gabe gilt für heute“ vom 08.11.2026.
- 09.11.2026 (0.33.2): Die Höchstmenge ist eine Menge in der Einheit der Verordnung („2 in 24 Stunden“ = 2 Stück),
  gezählt über alle Bedarfsverordnungen desselben Medikaments für den Bewohner. Weiter Warnung, keine Sperre.
- 09.11.2026 (0.33.2): Ob eine Absprache nötig ist, hängt nur an der Qualifikation, nicht an der Position
  „Administrator“ (TST ist in der Demo Fachkraft).
- 09.11.2026 (0.33.2): Vor jedem Nachholen von Demo-Daten eine Sicherungskopie – auch wenn der Teil nichts Vorhandenes
  ersetzt. Einfacher als je Teil zu entscheiden; nachgeholt wird nur nach Updates.

- 09.11.2026 (0.33.1): Jede Korrektur bekommt einen Test, der vor der Korrektur rot ist (Gegenprobe). Neue
  einmalige Schritte beim Start (Rechte vorbelegen, Demo nachholen) brauchen keinen eigenen Update-Test mehr:
  Der Sicherheitstest findet sie über ihren Merker in `meta`. Er prüft nur, dass der Start nicht abbricht –
  nicht, ob der Schritt inhaltlich richtig ist; dafür bleibt der Test des Schritts selbst.

- 08.11.2026 (0.33.0): Die Qualifikation bleibt in der Datenbank das bisherige Feld „Fachkraft“ (ja/nein) und
  heißt in der Oberfläche „Assistenzkraft“ oder „Fachkraft“ – keine Umstellung der Daten nötig.
- 08.11.2026 (0.33.0): „Visiten nur Fachkräfte“ ist das Häkchen „nur Fachkräfte“ der Rechte-Tabelle, nicht fest
  im Programm; wer Rechte verwaltet, kann es ändern. Bestehende Datenbanken bekommen es einmal gesetzt.
- 08.11.2026 (0.33.0): Bedarf geben ist ein eigenes Recht (nicht „Medikation bearbeiten“, das nur Fachkräfte
  haben): Assistenzkräfte dürfen geben, brauchen aber die Absprache. Fachkräfte können eine Absprache eintragen,
  müssen es nicht.
- 08.11.2026 (0.33.0): Die Höchstmenge liest das Programm aus der Zahl vorne in „höchstens“ (z. B. „2 in 24
  Stunden“) und zählt die Gaben der 24 Stunden davor. *(Ersetzt am 09.11.2026, 0.33.2: es zählt die Menge.)* Bei erreichter Menge warnt es, sperrt aber nicht – die
  Entscheidung bleibt beim Menschen (z. B. nach ärztlicher Rücksprache). Ohne Zahl: keine Warnung.
- 08.11.2026 (0.33.0): Eine Gabe gilt für heute (nur Uhrzeit), höchstens 5 Minuten in der Zukunft. Nachträge
  für frühere Tage gibt es nicht; dafür gibt es den Bericht. *(Ersetzt am 09.11.2026, 0.33.2: kurz nach Mitternacht auch gestern.)*

- 08.11.2026 (Schritt 26n): Umwandlung SVG → ICO mit einem eigenen Python-Skript **nur mit der
  Standardbibliothek** (keine weitere Abhängigkeit). Geprüft und verworfen: `Svg.Skia` (MIT) zieht `Svg.Custom`
  mit MS-PL-Lizenz nach, die nicht auf der Liste MIT/Apache-2.0/BSD steht. Das Skript zeichnet, was die
  Vorlagen nutzen (Rechtecke mit runden Ecken, Farben, radiale Verläufe), und meldet andere SVG-Teile als
  Fehler. Ein neues Symbol mit denselben Mitteln braucht nur neue SVG-Dateien; mit anderen Formen (z. B.
  Pfaden) müsste das Skript erweitert werden.
- 08.11.2026 (Schritt 26n): Die 256-px-Größe liegt als PNG in der `.ico` (für große Symbole im Explorer), die
  kleinen als klassisches Symbolbild (32 Bit mit Alpha). Fenster und Taskleiste nutzen die klassischen Bilder;
  `System.Drawing` wählt ohnehin nur unter ihnen (bis 64 px).
- 08.11.2026 (Schritt 26n): „Zentral“: Das Programmsymbol ist das Vorgabesymbol aller Fenster. Windows Forms hat
  dafür keine öffentliche Einstellung; `Stil.SymbolFuerAlleFenster()` setzt das interne Feld über Reflection.
  Fehlt das Feld in einer künftigen .NET-Version, tragen Hauptfenster und alle Dialoge mit `Fenster.Grundform`
  das Symbol trotzdem, und ein Oberflächentest meldet den Fehler.

- 08.11.2026 (Schritt 26m): Die Bereichswahl der Tafel steht in der Ansicht, nicht in der Aktionsleiste
  (dort bleibt „Neues Angebot …“ für beide Reiter). Im Wohnbereich zeigt die Tafel nur Angebote mit Teilnehmern
  aus dem Bereich; „5 von 8“ zählt alle Teilnehmer.
- 08.11.2026 (Schritt 26m): „Noch offen“ behält auch Termine, die schon vorbei sind, bis jemand bestätigt (wie
  im Tagesplan). „Rest des Tages“ zeigt nur, was noch kommt. Das Therapeut-Kürzel kommt aus der
  Mitarbeiterliste, sonst aus den Anfangsbuchstaben.
- 08.11.2026 (Schritt 26m): Für die Demo zehn tägliche Angebote (auch am Wochenende), zugeteilt nur Bewohnern
  aus Schritt 26h, ohne Überschneidung mit Wochenplan und anderen Therapien; Lücken bleiben mittags und
  am Nachmittag. Ältere Demo-Datenbanken bekommen Therapien und Wochenplan der Demo einmal neu; dort selbst
  angelegte Angebote, Zuteilungen und Wochenplan-Einträge der Demo werden dabei ersetzt.

- 08.11.2026 (Schritt 26l): **Fahrzeuge** stehen nur noch im Kalender (dort gab es dieselbe Leiste schon);
  in „Heute wichtig“ ist für sie kein Kasten vorgesehen. **Änderungen der Medikation** stehen als Hinweis in
  der Bewohnerzeile („Medikation: Pause beginnt“) statt in einem eigenen Kasten.
- 08.11.2026 (Schritt 26l): Die 1100 px gelten für die Fläche der Übersicht (ohne Seitenleiste). Beim
  Start in der Fenstergröße 1180 × 800 steht „Heute wichtig“ deshalb oben; im Vollbild nebeneinander.
- 08.11.2026 (Schritt 26l): „Laufender Dienst“: offen ist, was jetzt dran ist (auch aus früheren Schichten
  liegen geblieben) und „wer Zeit hat“; erledigt zählt, was in dieser Schicht abgehakt wurde. Aufgaben für
  spätere Schichten zählen nicht. Facharzttermine aus Visiten öffnen beim Abhaken das Formular wie in
  „Aufgaben“. Aufgaben ohne Bewohner und Übergaben „Wohnbereich allgemein“ stehen in jedem Bereich.
- 08.11.2026 (Schritt 26l): „Termine heute“ fasst gleiche Termine mit gleichem Status zusammen und nennt
  höchstens drei Namen („+2“). „Wo“: abwesend vor Ausgang vor „in Therapie“ (erst nach „gegangen“) vor
  einem Kalendertermin, der gerade läuft; Wochenplan-Einträge und unbestätigte Therapien zählen nicht.
- 08.11.2026 (Schritt 26l): „Neuer Bericht …“ braucht eine gewählte Zeile; es gibt keine Vorauswahl, damit
  nicht versehentlich für den ersten Bewohner geschrieben wird. Das Häkchen „alle Bereiche“ ist in die
  Bereichswahl gewandert (wie im Bereich „Bewohner“).
- 08.11.2026 (Schritt 26l): Grün für „Neuer Bewohner …“ nach `docs/gestaltung.md` („Aufnahme grün“); die
  Hauptaktion bleibt die eine Akzentfarbe. Die Statusleiste gilt in allen Bereichen; „Stand“ ist die Zeit,
  zu der die Ansicht zuletzt gelesen wurde.

- 08.11.2026 (Schritt 26j): Die Liste zeigt zuerst die eigenen Bereiche (wie die Übersicht); „alle Bereiche“
  und R1–R3 sind wählbar. Die Filterknöpfe bleiben und zählen im gewählten Bereich.
- 08.11.2026 (Schritt 26j): Hauptaktion ist „Neuer Bericht …“ (die häufigste Aufgabe in dieser Ansicht);
  „Neuer Bewohner …“ ist deshalb nicht mehr farbig. Nach dem Speichern bleibt man in der Liste, der neue
  Bericht steht rechts – in der Akte öffnet „Neuer Bericht …“ wie bisher die Berichte der Akte.
- 08.11.2026 (Schritt 26j): Die Hinweise sind dieselben Marken wie auf den Karten, als Text in einer Spalte;
  eine Tabellenzeile kann keine einzelnen farbigen Marken zeigen, deshalb färbt sich die Spalte nach der
  wichtigsten (rot vor gelb), der Text sagt immer, was gilt.

- 08.11.2026 (Schritt 26i): Filter und Umschalter, die schon in einer eigenen Zeile der Seite stehen (z. B.
  Tagesplan „nach Uhrzeit / nach Bewohner“, Übersicht „Meine Bezugsbewohner“), bleiben dort; sie liegen
  ohnehin direkt unter der Aktionsleiste. Umgezogen ist, wie im Auftrag präzisiert, nur die Knopfzeile.
- 08.11.2026 (Schritt 26i): „← Alle Bewohner“ steht als Rücksprung noch vor der Hauptaktion – wie ein
  Zurück-Knopf, den man immer ganz links erwartet.
- 08.11.2026 (Schritt 26i): Was rechts steht, erkennt die Leiste am Wort „drucken“ im Knopf; die Hauptaktion
  am Merkmal des Knopfs (Stil.HauptKnopf), damit sie auch gesperrt (grau) an ihrem Platz bleibt.

- 08.11.2026 (Schritt 26h): Die neuen Demo-Bewohner heißen alle mit L bis Z, also nach „Kiefer“. So bleiben die
  ersten zehn mit ihren Berichten, Übergaben und Beispielen in Tests und Testliste gleich (die Berichte
  entstehen nach Namen sortiert).
- 08.11.2026 (Schritt 26h): Wie in 0.29.7 wird eine ältere Demo-Datenbank einmal ganz neu geladen statt ergänzt
  (neue Bewohner betreffen Zimmer, Bezugspflege, Medikation, Therapien und Berichte zugleich); die alte
  liegt als Kopie daneben.
- 08.11.2026 (Schritt 26h): Für die Bildschirmfotos stellt der Testlauf den Bildschirm des Windows-Rechners auf
  1920 × 1080; gelingt das nicht, bleibt er bei 1024 × 768 und die großen Fotos sind entsprechend kleiner
  (`109_zeiten.txt` nennt die tatsächliche Fenstergröße).

- 08.11.2026 (Schritt 26g): „NOCH OFFEN“ bleibt oben in „Jetzt und gleich“, auch nach dem Ende der
  Therapie, bis jemand bestätigt – unten bei „Erledigt“ würde es übersehen.
- 08.11.2026 (Schritt 26g): Für Termine ohne Bestätigung (Kalender, Wochenplan, Fahrten) gibt es nach dem
  Ende das Wort „VORBEI“. Die Liste aus dem Chat hat dafür kein Wort; „ZURÜCK“ würde etwas behaupten,
  das niemand bestätigt hat.
- 08.11.2026 (Schritt 26g): Bestätigen über einen Knopf „bestätigen ▾“ mit Auswahl statt drei Knöpfen je
  Zeile: Drei Knöpfe passen nicht in eine Tafelzeile neben sechs Spalten.
- 08.11.2026 (Schritt 26g): Die Spalte „Begleitung“ zeigt bei Therapien den Therapeuten, bei Ausflügen
  die Begleitung, bei Fahrten Fahrzeug und Fahrer; der Wochenplan hat dafür kein Feld.
- 08.11.2026 (Schritt 26g): Schmale Fenster: Spalten fallen weg statt zu schrumpfen (zuerst „wann ·
  wer“, dann „Begleitung“, dann „Ort“) – drei Buchstaben je Spalte wären auf Abstand nicht lesbar.
  Mindestbreite je Textspalte 120 Pixel. Der volle Text steht im Hinweis und für Bildschirmleser.
- 08.11.2026 (Schritt 26g): Die Marke der Herkunft (Therapie, Kalender, Fahrt, Wochenplan) entfällt auf
  der Tafel; der Titel sagt es („Fahrt: …“, „Haus: …“).

- 08.11.2026 (Schritt 26f): „Bearbeiten nur Fachkräfte und Leitungen“ ist wie bei der Medikation
  vorbelegt: alle Positionen außer Verwaltung, mit Häkchen „nur Fachkräfte“. Leitungen, die Fachkraft
  sind, dürfen also; eine Leitung ohne Fachkraft (Demo: UT) nicht, weil „nur Fachkräfte“ im Rechte-Modell
  für alle Positionen gilt. Wer das anders will, ändert es in der Rechte-Tabelle.
- 08.11.2026 (Schritt 26f): Ein Knopf statt eines Reiters: Die Reiter der Akte sind Inhalte für alle;
  ärztliche Angaben haben ein eigenes Recht „ansehen“, ein Knopf lässt sich dafür grau schalten.
- 08.11.2026 (Schritt 26f): Der Allergiehinweis wird weiter am Bewohner gespeichert (Zustand und Text);
  neu ist nur die Art der Reaktion je Stoff. So bleiben Übersicht, Filter, Karten und Ausdrucke unverändert.
- 08.11.2026 (Schritt 26f): Eine Diagnose wird nicht geändert, sondern beendet und neu eingetragen –
  so bleibt der Verlauf vollständig (wie bei Berichten mit Nachträgen).
- 08.11.2026 (Schritt 26f): Die Diagnosen stehen in der Visite auf dem Bildschirm, nicht auf dem
  ausgedruckten Visitenbogen: Der Ausdruck geht über den Tisch, der Bildschirm fragt das Recht ab.

- 08.11.2026 (Schritt 26e): Bestehende Demo-Datenbanken werden einmal ganz neu geladen statt Feld für Feld
  umgestellt: Die Bereichsnamen stehen auch in Einträgen, die nie geändert werden (Statusmeldungen,
  Fassungen der Therapien), und die Zimmer der alten Demo (101–110) passen nicht zu R1–R3. Damit nichts
  verloren geht, liegt die alte Datenbank als Kopie daneben (`…-vor-0.29.7.db`); die Kopie aus
  `%APPDATA%\Dokumentationssystem` (0.29.2) bleibt ebenfalls.
- 08.11.2026 (Schritt 26e): Betten sind keine eigene Tabelle: Ein Zimmer hat 1 oder 2 Betten, welcher
  Bewohner wo liegt, steht am Bewohner (Zimmer und Bett). Frei = nicht belegt. Eine „Entlassung“ gibt es
  noch nicht (siehe Offene Punkte); der Zimmerwechsel läuft über „Bearbeiten …“.
- 08.11.2026 (Schritt 26e): „Neue Bewohner: nein“ bei „offen geführt“ widerspricht Regel 5 nicht: Es ist
  keine leere Angabe, sondern bekannt (aufgenommen wird nur geschlossen, Vorgabe Tobi).
- 08.11.2026 (Schritt 26e): Der Bereichsfilter der Übersicht ist eine Auswahl neben „alle Bereiche“;
  „eigene Bereiche“ verhält sich wie bisher.

- 08.11.2026 (Schritt 26d): Zweistellige Jahre gelten überall als 20xx (laut Auftrag) – auch beim
  Geburtsdatum; „03.04.65“ wird dort zu 2065 (liegt die Aufnahme davor, meldet das Formular einen
  Fehler). Wer ein Jahr vor 2000 meint, schreibt es vierstellig.
- 08.11.2026 (Schritt 26d): Der Punkt kommt erst, wenn die nächste Ziffer getippt wird („08“ → „08.1“),
  nicht schon nach der zweiten – so stört ein selbst getippter Punkt nie. Beim Lesen versteht der Kern
  dieselben Kurzformen, damit auch „Enter“ ohne Verlassen des Feldes richtig speichert.
- 08.11.2026 (Schritt 26d): „Gelesen“ steht an der Liste im Posteingang (ein Verlauf = eine Zeile);
  geöffnete Verläufe gelten wie bisher als gelesen, bleiben aber im Posteingang, bis jemand „Gelesen“
  oder „Archivieren“ drückt.

- 08.11.2026 (Schritt 26a3): Entwürfe gibt es für „Neuer Bericht“ (das ist die tägliche Dokumentation)
  und für den HEB-Bogen (der hatte schon Entwürfe). Andere offene Fenster werden beim Abmelden nach der
  einen Rückfrage geschlossen; ihre Eingaben gehen verloren (die Rückfrage sagt das).
- 08.11.2026 (Schritt 26a3): „Benutzer wechseln“ ist jetzt Abmelden und mit dem anderen Kürzel anmelden;
  das alte Klappmenü entfällt. Schließt man das Anmeldefenster nach dem Abmelden, endet das Programm –
  so bleibt nie ein Fenster mit Bewohnerdaten ohne Anmeldung offen.
- 08.11.2026 (Schritt 26a2): Die Position „Administrator“ vergibt nur ein Administrator (sonst könnte sich
  z. B. eine Bereichsleitung selbst alle Rechte geben). Fehlen Häkchen in der Tabelle (ältere Daten),
  gilt trotzdem: Der Administrator darf alles.

- 08.11.2026 (Schritt 26a1): Der Leittext steht unter dem Namen oben in der Seitenleiste (damit auf
  jeder Seite, auch im Kopf der Übersicht); der Kopfbereich der Seiten behält Titel und Zähler.
  Eingeklappt steht er im Hinweis am Info-Symbol.
- 08.11.2026 (Schritt 26a1): Die Fußzeile mit Name und Demo-Hinweis setzt die gemeinsame Druckvorschau
  auf jede Seite (unterer Rand), damit kein Ausdruck sie vergisst.
- 08.11.2026 (Schritt 26a1): Nicht umbenannt: die Datenbankdatei `dokumentationssystem.db` (im
  gemeinsamen Ordner liegt sie für alle Rechner; ältere Versionen finden sie so weiter) und die
  internen Umgebungsvariablen `DOKUSYSTEM_*` (ein Update von 0.29.1 setzt `DOKUSYSTEM_FRISCH_AKTUALISIERT`).
- 08.11.2026 (Schritt 26a1): Antwortet die neue Update-Adresse nicht (vor der Umbenennung: „nicht
  gefunden“), fragt das Programm sofort die alte; nach der Umbenennung leitet GitHub die alte Adresse
  weiter. Kann das Kopieren des alten Datenordners nicht beendet werden, meldet das Programm es und
  startet nicht – beim nächsten Start wird erneut kopiert, nichts ist halb übernommen.

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
- **Update-Projekt getrennt** (`DokuLine.Update`): einziges Projekt mit Internetzugriff;
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
  das neue Projekt `src/DokuLine.Bausteine` verschoben – sonst hinge das Modul vom Programm
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
- 08.11.2026 (Schritt 29): Bedienelemente einheitlich Häkchen statt selbst gezeichneter Schalter (die
  einfachere Lösung laut Auftrag); keine Gruppe hat mehr als fünf Möglichkeiten, also keine
  Auswahlknöpfe. Aufklappbare Unterpunkte braucht es nicht: Die Untermodule stehen eine Ebene tiefer
  in der Karte ihres Moduls.
- 08.11.2026 (Schritt 29): Rechte je Gruppe aus der bestehenden Rechte-Tabelle, ohne neue Funktion:
  Wohnbereich und System (Datenbank-Ort, Update) → „Einstellungen des Bereichs“; Demo-Daten →
  „Demo-Daten zurücksetzen“; Module → „Module ein- und ausschalten“; Fahrzeuge → „Fahrzeuge
  eintragen“; Vorlagen → „HEB-Bogen erstellen“; Aufgaben, Medikamente und Verwaltung prüfen ihre
  Rechte in ihren Fenstern wie bisher. Persönliches und „Über“ darf jeder. Damit darf eine
  Mitarbeiterin ohne Leitungsrecht z. B. den Bereich des Rechners nicht mehr umstellen (vorher ging
  das für alle).
- 08.11.2026 (Schritt 29): „Sicherung“ steht als Karte unter „System“, ist aber noch nicht gebaut
  (war nie beauftragt, siehe „Offene Punkte“). Die Nachschlage-Ansicht ist eine Einstellung je
  Rechner und für alle änderbar.
- 08.11.2026 (Schritt 28): Betreuer sind eine eigene Tabelle mit Fassungen (wie Bezug und
  Biografie), nicht Teil des Bewohner-Formulars – so bleiben Formular und Konfliktschutz der
  Stammdaten unverändert. Ein Betreuer je Bewohner (die neueste Fassung gilt).
- 08.11.2026 (Schritt 28): E-Mail-Adressen werden nur einfach geprüft (ein @, Punkt im Namen danach,
  keine Leerzeichen und keine Zeichen, die eine mailto-Adresse verändern). Der Betreff nennt nur den
  Namen des Bewohners; Text und Versand macht der Mensch im E-Mail-Programm.
- 08.11.2026 (Schritt 28): .ics: Das Programm schreibt Ortszeit ohne Zeitzone („floating“), das
  verstehen Outlook und andere als Ortszeit. Beim Lesen: Zeiten mit „Z“ sind UTC und werden
  umgerechnet, Zeiten mit Zeitzonen-Angabe gelten als Ortszeit (die Demo läuft in einer Zeitzone).
  Wiederholungen (RRULE) übernimmt das Programm nicht – nur den ersten Termin, mit Hinweis. Erinnerungen
  (VALARM) und Teilnehmer werden nicht gelesen. Exportiert werden Titel, Zeit, Ort und eine Beschreibung
  (Art, Bewohner, Hinweis, „Demo — ausgedachte Daten“).
- 08.11.2026 (Schritt 28): Die Zuordnung beim Übernehmen gilt für alle angekreuzten Termine zugleich;
  wer verschiedene Zuordnungen braucht, übernimmt in mehreren Durchgängen. Entweder alle angekreuzten
  werden angelegt oder keiner (bei einem Fehler).
- 08.11.2026 (Schritt 27): Die Liste ist aus einzelnen Zeilen gebaut (keine Tabelle mit Zellen), weil
  „raus“/„zurück“ als echte Knöpfe in der Zeile stehen sollen. Spalten sind feste Anteile der Breite,
  alle Zeilen haben dieselbe Breite für den Knopfbereich, damit die Spalten fluchten. Die seltenen
  Aktionen (Ausgang ändern, Anwesenheit, Unterbringung, Verlauf) liegen hinter „…“, damit die Zeile
  ruhig bleibt. Hervorhebung: Farbe immer mit Wort und Balken (nie Farbe allein).
- 08.11.2026 (Schritt 26c): Der Entwurf gilt je Wohnbereich (eine laufende Visite pro Bereich), nicht je
  Person – so kann eine Kollegin die Visite am anderen Rechner fortsetzen. Die Visitenansicht zeigt die
  Bewohner des eigenen Bereichs (Einstellung „Bereich“ dieses Rechners).
- 08.11.2026 (Schritt 26c): „Besprochen“ setzt sich, sobald bei einem Bewohner etwas eingetragen ist;
  ein Häkchen ohne Eintrag (z. B. „nichts Neues“) lässt sich von Hand setzen und erzeugt keinen Bericht.
- 08.11.2026 (Schritt 26c): Die Spalte „Art“ bleibt in der Datenbank, damit ältere Visiten lesbar
  bleiben; neue Visiten speichern sie leer.
- 08.11.2026 (Schritt 26b): Gespeichert wird weiter der Name des Arztes (wie bisher als Text), nicht eine
  Nummer – so bleiben ältere Einträge und Ausdrucke gleich, und ein umbenannter oder inaktiver Arzt
  ändert keine früheren Einträge. Namen sind im Verzeichnis eindeutig.
- 08.11.2026 (Schritt 26b): Vorbelegung des Rechts: „Fachkräfte“ lassen sich in der Rechte-Tabelle nicht
  mit „Leitungen und Verwaltung“ zusammen ausdrücken (das Häkchen „nur Fachkräfte“ würde
  Geschäftsführung und Verwaltung ohne Fachkraft ausschließen). Deshalb Positionen: die drei
  Leitungen, Bereichsleitung, Psychologischer und Pädagogischer Fachdienst. Fachkräfte im
  Wohnbereich bekommen es bei Bedarf in der Tabelle.
- 08.11.2026 (Schritt 26b): Kalendertermine haben kein eigenes Feld „Arzt“; die Auswahl füllt Ort und
  Hinweis (Name, Praxis, Telefon). Beim Facharzttermin steht der Arzt ebenso im Hinweis.
- 08.11.2026 (Schritt 26a): Eine eigene Einstellung „Kürzel“ gab es seit 0.22.0 nicht mehr; maßgeblich
  ist die Anmeldung. Personen-Felder mit mehreren Personen speichern die Kürzel wie bisher als Text
  („AB, CD“), damit ältere Einträge lesbar bleiben.
- 08.11.2026 (Schritt 26): Visiten stehen in der Akte als Bericht im Reiter „Berichte“ (Text beginnt
  mit „Visite …“, Lebensbereich 5) – kein eigener Reiter (die einfachere Lösung laut Auftrag).
- 08.11.2026 (Schritt 26): Eine gespeicherte Visite wird nicht geändert (wie Berichte); Korrekturen
  gehen über einen Nachtrag zum Bericht. Die Übergabe zur Visite gilt bis zur nächsten Schicht, die
  Aufgaben bleiben stehen, bis sie erledigt sind.
- 08.11.2026 (Schritt 26): Eine Medikations-Aufgabe ist erledigt, sobald das Formular im
  Medikamentenplan gespeichert ist (Kürzel der angemeldeten Person); bricht man ab, bleibt sie offen.
  „Dosis geändert“ öffnet den Stufenplan (eine Stufe ab heute = neue Grundverordnung); gibt es für das
  Medikament noch keine Verordnung, öffnet „Neue Verordnung“. Die Verknüpfung zur Visite steht im
  Hinweis der Änderung („laut Visite 08.11.2026 (Psychiater, Dr. …)“).
- 08.11.2026 (Schritt 26): Frei eingetippte Medikamente werden der Datenbank zugeordnet, wenn
  Wirkstoff oder ein Handelsname gleich lautet (beim Speichern der Visite und nach dem Anlegen).
  Die Knöpfe der Aufgaben sind gesperrt wie die Formulare dahinter („Medikament anlegen“ bzw.
  „Medikation bearbeiten“, beide nur Fachkräfte).
- 08.11.2026 (Schritt 26): Die Demo-Fahrt, die gerade unterwegs ist, nimmt Emil Eiche mit (statt
  Anton Ahorn), damit sein Status („in der Holzwerkstatt“) in Vorführung und Tests sichtbar bleibt.
- 08.11.2026 (Schritt 25): Standardaufgaben entstehen, sobald das Programm die Aufgaben liest
  (Seite „Aufgaben“, Zähler in der Seitenleiste, Abgleich alle 30 Sekunden) – also zu Beginn der
  Schicht, sofern das Programm läuft. Für Schichten, in denen kein Rechner lief, entstehen sie nicht
  nachträglich. Je Vorlage und Schicht höchstens eine Aufgabe (Datenbank-Regel).
- 08.11.2026 (Schritt 25): „Begründet schließen“ ist ein Abschluss mit Notiz (wie „erledigt“, aber
  mit Begründung) – nur bei Standardaufgaben, die anderen werden erledigt oder weitergegeben.
  Vorlagen sind Einstellungen: Ändern überschreibt sie, schon entstandene Aufgaben bleiben gleich.
- 08.11.2026 (Schritt 25): Schichtzeiten je Bereich in der Datenbank; ein Rechner liest sie beim
  Start und nach dem Speichern der Einstellungen. Früh vor Spät vor Nacht, die Nacht beginnt vor
  Mitternacht. Die Oberflächentests legen die Demo-Standardaufgaben still, damit ihre Zahlen nicht
  von der Uhrzeit abhängen.
- 08.11.2026 (Schritt 24): „Haus“ ist ein Merkmal je Termin (wie die Art), kein zweiter Kalender
  in der Datenbank – so bleiben Verlauf, Konfliktschutz und Wochenplan gleich. Haus-Termine sind
  grau (die Farbe von „abwesend“ ist sonst nur auf Karten) und tragen „Haus:“ im Titel.
- 08.11.2026 (Schritt 24): Fahrten sind eigene Einträge, „losgefahren“ und „zurück“ eigene
  Ereignisse mit Uhrzeit und Kürzel (die Datenbank verbietet Überschreiben). Geändert oder gelöscht
  wird eine Fahrt nicht – war nicht beauftragt; eine falsche Fahrt einfach nicht losfahren lassen.
  „losgefahren“ gilt für die nächste noch nicht begonnene Fahrt von heute; ein Fahrzeug kann nur
  einmal gleichzeitig unterwegs sein.
- 08.11.2026 (Schritt 24): Doppelbuchung = geplante Zeiten überschneiden sich (eine zurückgekehrte
  Fahrt zählt bis zur tatsächlichen Rückkehr). Warnung im Formular, Speichern bleibt möglich.
- 08.11.2026 (Schritt 24): Abwesend sind Mitfahrende nur, solange die Fahrt „unterwegs“ ist (nach
  „losgefahren“, vor „zurück“). Die Fahrzeugliste pflegt, wer „Fahrzeuge eintragen“ hat; Fahrzeuge
  werden stillgelegt, nicht gelöscht. Bestehende Demo-Datenbanken bekommen Fahrzeuge, Fahrten und
  die Haus-Termine nach dem Update einmal dazu (der Demo-Kalender wird dabei neu geladen).
- 08.11.2026 (Schritt 23): Knöpfe „gegangen/abgesagt/ausgefallen“ nur bei Therapien – nur dort
  gibt es eine Teilnahme (wie im Wochenplan). Termine aus Wochenplan und Kalender (auch
  Heimfahrten und Ausflüge) stehen zur Orientierung dabei. Fahrten mit Fahrzeugen kommen mit
  Schritt 24.
- 08.11.2026 (Schritt 23): „gegangen“ ist im Wochenplan „teilgenommen“ – ein Datensatz, zwei
  Wörter: Im Tagesplan geht es darum, ob jemand losgegangen ist.
- 08.11.2026 (Schritt 23): „Wo müsste er sein?“ gilt ab „gegangen“ bis zum Ende des Termins
  (auch wenn vor Beginn bestätigt); „sofern nichts anderes eingetragen ist“: eine danach gesetzte
  Statusmeldung geht vor. „noch offen“ ab Beginn des Termins.
- 08.11.2026 (Schritt 23): Bereich wie in der Übersicht: die Wohnbereiche des Angemeldeten, sonst
  der des Rechners; „alle Bereiche“ zeigt alle. Aktualisierung über den bestehenden Abgleich alle
  30 Sekunden (Änderungen anderer Rechner und Uhrzeit), kein eigener Dienst.
- 08.11.2026 (Schritt 22): Der Anbieter steht in jeder Fassung des Angebots (wie Name und
  Termine); es zählt die letzte. So bleibt ein Wechsel im Verlauf, und die Datenbank überschreibt
  weiter nichts.
- 08.11.2026 (Schritt 22): „Mitarbeiter des Bereichs mit dem Recht“ als eigenes Recht „Therapien des
  eigenen Bereichs bearbeiten“ (vorbelegt: Bereichsleitung, Leitungen). So bleibt „Therapien
  bearbeiten“ bei der Therapie, und ein Wohnbereich bekommt das Recht in der Rechte-Tabelle, ohne
  die Angebote der Therapie ändern zu können. Es gilt nur für Bereiche, zu denen man gehört – auch
  für Leitungen (die gehören zum Bereich „Leitung“ und ändern Wohnbereichs-Angebote nur, wenn sie
  dem Wohnbereich zugeordnet sind).
- 08.11.2026 (Schritt 22): Anbieter zur Wahl sind „Therapie“ und die Wohnbereiche, also Bereiche,
  in denen Bewohner wohnen (nicht „Leitung“ oder „Verwaltung“). Ein neues Recht wird in bestehenden
  Datenbanken einmal vorbelegt (Merker in der Datenbank); später entfernte Häkchen bleiben entfernt.
- 08.11.2026 (Schritt 22): Bestehende Demo-Datenbanken laden die Demo-Angebote nach dem Update einmal
  neu (wie bei früheren neuen Demo-Bereichen) – eigene Änderungen an Therapien in der Demo gehen dabei
  verloren.
- 08.11.2026 (Schritt 21): „Beim Start prüft das Programm, ob WebView2 vorhanden ist“: Das Ergebnis
  steht in den Einstellungen unter „PROGRAMM → Nachschlage-Ansicht“ („WebView2 vorhanden (Version
  …)“ oder „fehlt“). Einen eigenen Abschnitt „Über“ gibt es erst mit Schritt 29.

## Testliste für Tobi

Version 0.33.12:

1. „Demo-Daten zurücksetzen“, dann Übergabe: kein Eintrag lautet „Keine Besonderheiten“ oder „Ruhiger Dienst“.
2. Therapie → „Neues Angebot …“: Feld „Teilnehmer“, darunter klein „(Anzahl)“.
3. Dokumentation: keine Aufgaben über den Berichten; links die Namen auf einer abgerundeten weißen Fläche, der
   gewählte Name hell und abgerundet hinterlegt.

Version 0.33.11:

1. Als „AB“ anmelden: oben links neben „Angemeldet: AB – …“ ein Kuvert mit rotem Ausrufezeichen und „3“. Maus
   darüber → „3 ungelesene Nachrichten“. Klick → Bereich „Nachrichten“.
2. Als „OP“ anmelden: Kuvert grau, ohne Zeichen. Abmelden (F12): kein Kuvert.
3. Bereich „Dokumentation“: über der Liste steht kein Nachrichten-Hinweis mehr.

Version 0.33.10: Jeden Bereich der Seitenleiste einmal öffnen – Kopfzeile und Aktionsleiste sind wie vorher
(Aufräumen, keine neue Funktion).

Version 0.33.9: nichts Neues zum Ansehen im Programm (Aufräumen).

Version 0.33.8: nichts Neues zum Ansehen im Programm (Aufräumen). Im Windows-Build stehen die Code-Maße.

Version 0.33.7:

1. Übersicht: oben „Wohnbereich: R1 · R2 · R3“. R2 drücken → Liste, Kästen und Kopfzeile zeigen R2, daneben
   „eingesprungen“. Dokumentation, Übergabe, Tagesplan, Aufgaben, Ausgangsliste: überall R2.
2. Als „OP“ (Therapie): zusätzlich „Therapie“; als „TST“ zusätzlich „Alle“. Als „CD“ nur R1–R3.
3. Abmelden (F12) und wieder anmelden → wieder der Bereich des Rechners.
4. Neue Aufgabe ohne Bewohner → Feld „Bereich“ (Vorgabe der gewählte). Auf R3 wechseln → die Aufgabe von R1 ist
   nicht zu sehen, der Zähler in der Seitenleiste zählt nur R3.
5. Einstellungen → Standardaufgaben: Spalte „Bereich“, die Liste zeigt die des gewählten Bereichs.
6. Visiten bei „Alle“ (als TST): „Visite beginnen …“ ist grau mit Hinweis.
7. Übersicht: Die Kästen haben eine helle Kopfleiste mit Zähler rechts; beim Darüberfahren wird sie kräftiger.

Version 0.33.6:

1. Als „KL“ (Assistenzkraft) anmelden → in der Seitenleiste fehlt „Visiten“, die übrigen Einträge rücken zusammen.
2. Als „OP“ (Therapie) eine Akte öffnen → „Bearbeiten …“ fehlt; die übrigen Knöpfe stehen ohne Lücke.
3. Als „CD“ Einstellungen → „Wohnbereich“ zeigt nur „Zimmer und Betten“; „Module“ und „System“ fehlen, unter
   „Verwaltung“ fehlt „Mitarbeiter“. Suche „Schicht“ findet „Schichtzeiten“ nicht.
4. Abmelden (F12) → anmelden mit „AB“ → „Visiten“ ist wieder da, ohne Neustart.
5. Ohne Anmeldung: Knöpfe grau mit „Bitte zuerst anmelden.“

Version 0.33.5:

1. Seitenleiste → „Dokumentation“: links nur „Nachname, Vorname“, alphabetisch; rechts die Berichte des ersten
   Bewohners der letzten 7 Tage, nach Tagen, jeder Bericht in der Farbe seiner Schicht umrandet.
2. Pfeil nach unten in der Namensliste → die Berichte wechseln mit. „◀“ klappt die Liste ein.
3. Ein langer Bericht zeigt zwei Zeilen und „… mehr“; Klick klappt ihn auf. „Ansicht: kompakt“ → „vollständig“.
4. „Wichtig“ drücken → nur wichtige Berichte, oben die Marke „Wichtig ×“; „30 Tage“, Berichtsart „Krise“ →
   „Filter speichern …“ → Name eingeben → der Knopf steht oben; nach Neustart noch da.
5. Suche „Arzt“ → die Fundstellen sind gelb hinterlegt.
6. „Neuer Bericht …“ → Feld „Berichtsart“ (Vorgabe „Alltag“) → speichern → der Bericht steht oben, man bleibt in der
   Dokumentation.
7. Als „AB“ bei Tagetes „Bedarf gegeben …“ → der neue Bericht trägt die Marke „Bedarfsgabe“.
8. „Akte öffnen“ → kein Reiter „Berichte“ mehr; „Berichte ansehen“ führt zurück.
9. Einstellungen → „Bereiche, Positionen, Berichtsarten …“ → „Krise“ umbenennen; „Visite“ umbenennen → Hinweis, dass
   das Programm sie selbst vergibt.
10. Bei 1920 × 1080 im Kompaktmodus: mindestens 15 Berichte ohne Bildlauf (hier nicht prüfbar, bitte ansehen).

Version 0.33.4:

1. Einstellungen → „Verzeichnisse“: oben Ärzte · Betreuer · Angehörige · Forensik · FOSA · Sonstige Stellen mit Zahl.
   Ein Klick zeigt die Liste mit eigenen Spalten; oben in der Leiste „Neu …“, „Öffnen …“, „Inaktiv / wieder aktiv“.
2. „Angehörige“, Suche „Ahorn“ → „Hanna Ahorn“, zugeordnet zu „Anton Ahorn (Mutter)“. Doppelklick → Person; unten
   „Anton Ahorn (Mutter)“ wählen → „Akte öffnen“ → die Akte von Anton Ahorn.
3. „Betreuer“ → „Heike Hecht“ steht bei zwei Bewohnern (Spalte „Bewohner“ = 2).
4. „FOSA“ → „Neue zuständige Person …“ → Name, Funktion, Ambulanz → Speichern; öffnen → „Bewohner zuordnen …“.
5. Als „CD“: „Verzeichnisse“ ansehen geht; bei „Ärzte“ ist „Neu …“ grau, bei „Angehörige“ geht es.
6. Medikamentenplan → Verordnung → „Arzt anlegen …“ öffnet das neue Formular (Verzeichnis „Ärzte“).

Version 0.33.3:

1. Akte von Ginster → Betreuer: „keine gesetzliche Betreuung“, „E-Mail an Betreuer …“ grau. Bei einem anderen Bewohner
   „Betreuer …“ → Häkchen „Keine gesetzliche Betreuung“ → die Felder werden grau; speichern → der alte Betreuer steht
   im Verlauf.
2. Kalender → „Als .ics speichern …“ in einen Ordner ohne Schreibrecht → Hinweis statt Absturz. Eine .ics-Datei auf den
   Kalender ziehen → die Vorschau öffnet sich, der Explorer bleibt bedienbar.
3. Einen beliebigen Dialog öffnen (z. B. „Neue Aufgabe …“) → oben links steht das Programmsymbol.
4. Einstellungen als „RS“ → Module → „Ausgangsliste“ aus → andere Gruppe und zurück → „Küchendienst“ bleibt grau.
5. Übersicht: Fenster schmal ziehen → die Bereichswahl wird schmaler oder verschwindet, „Drucken …“ bleibt sichtbar.
6. Therapie → Angebot bearbeiten → zwei Termine am selben Wochentag → Hinweis „höchstens ein Termin je Wochentag“.

Version 0.33.2:

1. Übersicht als „EF“: im Kasten „Laufender Dienst“ schnell doppelt auf einen Haken klicken → nur diese Aufgabe ist
   erledigt, die nächste bleibt offen.
2. Therapie → „Laufende Therapien“ als „OP“: in „Noch offen“ nach unten rollen, „gegangen“ klicken → die Spalte bleibt
   an der Stelle; Doppelklick trifft nicht den Nächsten.
3. Bewohner: einen Bewohner mit vielen Berichten wählen, Zeitraum „alle“ → rechts lässt sich rollen. Akte eines
   anderen Bewohners öffnen, „Zurück“ → dieser Bewohner ist in der Liste gewählt.
4. Einstellungen → „Bereiche und Positionen …“: „R2“ umbenennen → in Übersicht, Bewohner und Therapie-Tafel steht in
   der Bereichswahl der neue Name.
5. Akte → Medikamentenplan → Bedarfsmedikation: oben „In den letzten 24 Stunden …“; Menge 3 bei „höchstens 2“ →
   Warnung „… mit dieser Gabe 3 Stück …“.

Version 0.33.1:

1. Einstellungen → Verwaltung → „Bereiche und Positionen …“: „Mitarbeiter Wohnbereich“ umbenennen (z. B. „Betreuung“). Programm
   schließen und neu starten → es startet normal (unter 0.33.0 konnte hier ein Fehler kommen). Die Rechte der
   umbenannten Position bleiben, wie sie waren.
2. Nichts zu tun für die Sicherheitstests: Sie laufen bei jedem Update in GitHub Actions; ein Update kommt nur,
   wenn sie grün sind.

Version 0.33.0:

1. Anmelden: in der Liste steht bei jeder Person „Assistenzkraft“ oder „Fachkraft“ (z. B. KL Assistenzkraft,
   AB Fachkraft). Einstellungen → Verwaltung → Mitarbeiter: Spalte „Qualifikation“; im Formular die Auswahl.
2. Als „KL“: Visiten → „Visite beginnen …“ grau, beim Darüberfahren „Nur für Fachkräfte.“ Als „AB“ geht es.
3. Als „KL“: Akte von Thea Tagetes (oder einem anderen Bewohner mit Bedarf) → Medikamentenplan → Zeile der
   Bedarfsmedikation → „Bedarf gegeben …“. Oben steht, was heute schon gegeben wurde und „Höchstens …“.
4. Ohne „In Absprache mit“ → Fehler „Als Assistenzkraft bitte die Fachkraft wählen …“. Mit „AB“ → eingetragen;
   in der Akte unter „Berichte“ steht „Bedarfsmedikation gegeben um … In Absprache mit Anna Bachstelze (AB).“
5. Als „AB“ noch einmal: kein Pflichtfeld; ist die Höchstmenge erreicht, kommt eine Warnung, erst „Trotzdem
   eintragen“ trägt ein.

Version 0.32.0:

1. „Einstellungen“ steht ganz unten in der Seitenleiste (auch eingeklappt). Klick → ganze Seite,
   links die Gruppen, oben die Suche. „Demo-Daten zurücksetzen“ ist aus der Seitenleiste
   verschwunden (jetzt unter „System“).
2. Als „EF“: „Persönlich“ → „Seitenmenü eingeklappt“ an → die Seitenleiste klappt sofort ein; aus →
   wieder auf. „Meine Farbe“ ändern → gilt sofort.
3. „Wohnbereich“: „Beschluss endet bald“ auf 14 → Übersicht zeigt weniger „Beschluss endet in …“.
   Schichtzeiten „Spät“ auf 05:00 → beim Verlassen roter Hinweis; 14:30 → gilt.
4. Suche „Logo“ → Treffer „Bild im Kopf (Logo) · Wohnbereich“ → Enter → Gruppe „Wohnbereich“, die
   Karte hat links einen Balken.
5. Als „CD“: „Wohnbereich“ ist grau mit „Dafür fehlt die Berechtigung.“; „Persönlich“ geht.
6. „System“ → „Ort der Datenbank“ → „Lokal (Vorgabe)“/„Gemeinsamen Ordner wählen …“ fragt vorher;
   „Demo-Daten zurücksetzen …“ fragt vorher (roter „Achtung“-Hinweis daneben).
7. „Aufgaben“ → „Standardaufgaben …“ öffnet die Liste; auf der Seite „Aufgaben“ gibt es den Knopf
   nicht mehr. „Über“: Version, Tastenkürzel (F12, F11) und Lizenzen.
8. „Persönlich“ → „Abmelden (F12)“ → das Anmeldefenster. „Wohnbereich“ → „Zimmer und Betten …“ öffnet
   die Zimmerliste. Seitenleiste unten: „Einstellungen“, darunter „Abmelden (F12)“.

Version 0.31.0:

1. Akte Anton Ahorn → „Stammdaten“: „Betreuer: Heike Hecht (Betreuungsverein …) · Tel. … ·
   h.hecht@example.org“. Akte Dieter Distel: „nicht erfasst“ (gelb), „E-Mail an Betreuer …“ grau.
2. Bei Anton Ahorn „E-Mail an Betreuer …“ → Hinweis „Keine Gesundheitsdaten unverschlüsselt per
   E-Mail senden.“ → OK → Outlook (bzw. das E-Mail-Programm) öffnet sich mit Empfänger und Betreff
   „Betreuung Anton Ahorn“. Nicht senden (die Adresse ist ausgedacht). Abbrechen → nichts öffnet sich.
3. Dieter Distel „Betreuer …“: falsche E-Mail („abc“) → Hinweis; richtig (`…@example.org`) →
   Speichern → steht in der Akte. Als „OP“ (Therapie) ist „Betreuer …“ grau.
4. Kalender → „Als .ics speichern …“ → Datei speichern → in Outlook öffnen: die Termine des Monats.
5. In Outlook einen Termin als .ics speichern (oder Datei aus Schritt 4 nehmen) → Kalender „.ics
   übernehmen …“ oder Datei auf den Kalender ziehen → Vorschau, „Wohnbereich“, „Haus“ (als „EF“) →
   Übernehmen → Termine stehen im Kalender, im Hinweis „aus einer .ics-Datei“.

Version 0.30.0:

1. „Ausgangsliste“: oben Spaltenköpfe, darunter „UNTERWEGS“ – Clara Clematis (hellblau, „unterwegs
   seit …, zurück bis …“) und Gustav Ginster (rot, „überfällig seit …“, ganz oben). Darunter alle
   anderen nach Zimmer, „Stand jetzt“ z. B. „im Haus“ oder „abwesend: Heimfahrt …“.
2. Anton Ahorn „raus allein“ → rutscht nach oben zu „UNTERWEGS“; „zurück“ → wieder unten, in „Heute“
   steht „hh:mm–hh:mm Hz. …“.
3. „…“ (oder Rechtsklick auf die Zeile) → „Ausgang ändern …“, „Anwesenheit …“, „Unterbringung …“,
   „Verlauf …“ öffnen wie bisher.
4. Filter „nur unterwegs“ und „nur mit Beschränkung“ an und aus.
5. Fenster schmal ziehen → die Knöpfe rutschen unter die Zeile; lange Texte enden mit „…“, beim
   Darüberfahren steht der volle Text.

Version 0.29.16:

1. Die heruntergeladene `DokuLine.exe` im Explorer: weißes Quadrat mit zwei blauen und einem orangefarbenen
   Balken (in der Ansicht „Große Symbole“ mit zartem Farbverlauf).
2. Programm starten: dasselbe Symbol in der Taskleiste (bei hellem und dunklem Windows-Design gut zu sehen)
   und oben links im Fenstertitel; auch in Dialogen wie „Einstellungen“ oder „Anmelden“.
3. Oben rechts in der Kopfzeile bleibt der Platzhalter „Logo der Einrichtung“ (bzw. das eigene Bild).
4. Hinweis: Windows merkt sich Symbole; zeigt der Desktop-Link noch das alte, hilft Abmelden und Anmelden.

Version 0.29.15:

1. „Therapie“ öffnen: Reiter „Laufende Therapien“ ist zuerst offen. Oben die Tagesleiste 7–21 Uhr mit dem
   orangefarbenen Strich bei der jetzigen Uhrzeit.
2. Je laufender Therapie eine Spalte mit Kärtchen (Bereich · Zimmer, Name, Bezugspflege, Status). Daneben
   „Gleich“ und „Noch offen“.
3. In „Noch offen“ bei einem Kärtchen „gegangen“: es wandert in seine Therapie-Spalte („gegangen“); im
   Tagesplan steht derselbe Status mit Kürzel.
4. Bereichswahl R2: nur Bewohner aus R2. Als „EF“ ist R1 vorgewählt, als „OP“ „Alle Bereiche“.
5. Klick auf einen Balken der Tagesleiste: Reiter „Angebote“ beim Angebot.
6. Abends nach 20:45: „Gerade läuft keine Therapie. Heute kommt keine mehr.“

Version 0.29.14 (am besten im Vollbild):

1. Start als „EF“: links „Heute wichtig“ mit vier Kästen (Zähler in der Überschrift; leer: „Nichts offen“),
   rechts die 23 Bewohner von R1 nach Zimmer, ohne Bildlauf. Kopf der Liste: „BEWOHNER R1 · 23 · … IM HAUS“.
2. Spalte „Wo“: z. B. „Heimfahrt bis …“, „in Therapie: … bis …“; sonst „im Haus“. Hinweise wie Beschluss,
   Allergie, Medikation, Übergabe.
3. Klick auf eine Übergabe → „Übergabe“; auf einen Termin → „Tagesplan“; auf eine Nachricht → der Verlauf.
4. Im Kasten „Laufender Dienst“ ein Häkchen setzen: die Aufgabe verschwindet, „x von y erledigt“ und der
   Balken steigen; in „Aufgaben“ steht sie erledigt mit „EF“.
5. Aktionsleiste: „Neuer Bericht …“ ist grau, bis eine Zeile gewählt ist; „Neuer Bewohner …“ ist grün;
   rechts die Bereichswahl (R2 zeigt die 23 von R2) und „Drucken …“ (Vorschau im Querformat).
6. Doppelklick auf eine Zeile öffnet die Akte. Fenster schmaler ziehen: „Heute wichtig“ rutscht über die Liste.
7. Unten in der Statusleiste: „DEMO — ausgedachte Daten · Datum · Dienst · Stand …“.
8. Die Fahrzeuge („vor Ort“/„unterwegs“) stehen im Kalender.

Version 0.29.13:

1. Als „EF“ „Visite beginnen …“: unter dem Feld „Besprochenes“ „◀ Vorheriger“ und „Nächster Bewohner ▶“;
   oben stehen sie nicht mehr.
2. Feld leer: „Nächster Bewohner ▶“ grau, Klick zeigt „Erst den Bericht zu diesem Bewohner schreiben.“
3. Etwas eintippen: der Knopf ist sofort aktiv → nächster Bewohner, dort wieder grau.
4. „◀ Vorheriger“ geht immer; ein Klick in die Liste links wählt jeden Bewohner.

Version 0.29.12:

1. „Bewohner“: links eine Liste der 23 Bewohner von R1 (nach Zimmer), rechts die Berichte des ersten.
   Pfeil nach unten: der nächste Bewohner, rechts seine Berichte.
2. Klick auf „Name“: nach Name sortiert, nochmals: umgekehrt. Suchfeld „Tanne“: nur Theo Tanne.
   Bereichswahl „R3“: 23 andere Bewohner; „alle Bereiche“: 69.
3. Als „CD“: Bewohner wählen → „Neuer Bericht …“ (Aktionsleiste) → speichern → man bleibt in der Liste,
   rechts steht der neue Bericht.
4. Doppelklick auf einen Bewohner (oder „Akte öffnen“, oder Enter): die Akte. „← Alle Bewohner“: zurück,
   derselbe Bewohner ist gewählt.
5. Filter „Allergie“: nur Bewohner mit eingetragener Allergie; Hinweis-Spalte rot.

Version 0.29.11:

1. Kopfzeile wie bisher (Titel, Logofeld, Demo-Hinweis, Angemeldet, Abmelden), nur niedriger.
2. „Aufgaben“: „Neue Aufgabe …“ links in einer Leiste direkt unter der Kopfzeile. „Tagesplan“: keine Leiste.
3. Bewohner öffnen: in der Leiste „← Alle Bewohner“, „Bearbeiten …“ (farbig), „Nachrichten …“, „Status …“,
   „Ärztliche Informationen …“, „Neuer Bericht …“.
4. „Ausgangsliste“: „Tagesliste drucken …“ rechts in der Leiste.
5. Als TST einen Bericht anfangen, F12, als Entwurf speichern, wieder als TST anmelden: „Entwurf
   fortsetzen“ rechts außen in der Leiste.

Version 0.29.10:

1. Nach dem Update: im Datenordner liegt `dokumentationssystem-vor-0.29.10.db`; `update.log` nennt die
   Erweiterung.
2. Übersicht: „R1 · 23 Bewohner · 2 Betten frei“; Auswahl R2 und R3: je 23 Bewohner, 2 Betten frei.
3. „Bewohner“: 69 Karten; Filter „Allergie“ 14, „Beschluss“ 51, „Nicht erfasst“ 14.
4. Ein neuer Bewohner (z. B. Rosa Rose): Akte mit Berichten, Medikamentenplan, Bezugspflege,
   „Ärztliche Informationen …“ mit Diagnose, HEB-Bogen.
5. Als „VK“ (Bereichsleitung R3) anmelden: Übersicht zeigt R3; Therapie → „Gartengruppe“.
6. Einstellungen → „Zimmer und Betten …“: 45 Zimmer; „Neuer Bewohner …“ in R3 → „304 – Bett Tür“, „306 – Bett Tür“.
7. Bitte selbst ansehen, wo es mit 23 Bewohnern unübersichtlich wird (Übersicht, Tagesplan, Übergabe,
   Ausgangsliste), und in den Chat schreiben.

Version 0.29.9:

1. Als „CD“ links „Tagesplan“: dunkle Tafel, oben rechts groß Uhrzeit und Datum, oben links „TAGESPLAN ·
   R1“ und „Demo — ausgedachte Daten“. Spalten Zeit · Bewohner · Termin · Ort · Begleitung · Status.
2. Oben „JETZT UND GLEICH“ (gelb), darunter „SPÄTER HEUTE“, unten blasser „ERLEDIGT“. Jede Zeile hat ein
   Status-Wort (z. B. GEPLANT, GLEICH, JETZT, NOCH OFFEN).
3. Bei einer laufenden Therapie „bestätigen ▾“ → „gegangen“ → GEGANGEN, daneben „Uhrzeit · CD“. Danach
   „abgesagt“ → ABGESAGT, die Zeile rutscht nach „ERLEDIGT“. Im Wochenplan steht dasselbe.
4. Eine Minute warten: Uhr und Status gehen von selbst weiter (z. B. GLEICH → JETZT).
5. „nach Bewohner“: je Bewohner eine Überschrift, Status bleibt, keine Spalte „Bewohner“. „alle Bereiche“
   zeigt auch R2. Fenster schmaler ziehen: „Begleitung“ und „Ort“ verschwinden, beim Darüberfahren
   steht die ganze Zeile im Hinweis; maximiert erscheint neben dem Status „Uhrzeit · Kürzel“.
6. „Vollbild (F11)“ oder F11: nur die Tafel über den ganzen Bildschirm; Esc beendet. Im Vollbild
   bestätigen → beide Tafeln zeigen es. Im Vollbild F12 → Rückfrage, dann Abmelden.
7. Ohne Anmeldung ist „bestätigen ▾“ grau mit Begründung.

Version 0.29.8:

1. Als „CD“ Anton Ahorn öffnen → oben „Ärztliche Informationen …“ → zwei Diagnosen (F20.0
   Hauptdiagnose, E11.9 Nebendiagnose, gestellt von Dr. Linde bzw. Dr. Ulme). „Neue Diagnose …“: ICD
   „X1“ → Hinweis „ICD-10: z. B. F20.0 – oder leer lassen“; „F41.1 Generalisierte Angststörung“,
   Nebendiagnose, Arzt wählen → Speichern → steht in der Liste.
2. E11.9 wählen → „Beenden …“ → heute → grau unten „beendet am …“. Nochmals „Beenden …“ → nichts.
3. Allergien: „Haselnüsse“ mit „Schwellung im Mund, Atemnot möglich“. Text „Haselnüsse, Latex“ →
   Feld verlassen → zweite Zeile „Latex“ → Reaktion eintragen → „Allergien speichern“. Fenster
   schließen → oben in der Akte „Allergien: Haselnüsse, Latex“.
4. „Hinweise für den Notfall“ ändern und speichern; wieder öffnen → der Text ist da.
5. „Bearbeiten …“: kein Allergie-Feld mehr, nur der Hinweis auf „Ärztliche Informationen“.
6. Als „KL“ (keine Fachkraft): Fenster öffnet mit „Nur ansehen: …“, Knöpfe grau. Als „WZ“
   (Verwaltung): Knopf grau mit Begründung; der Allergiehinweis oben bleibt sichtbar.
7. Visite beginnen (R1) → rechts „Allergien: …“ (einmal, nicht doppelt), darunter „Diagnosen: …“. HEB-Bogen öffnen → im Kopf
   „Diagnosen: …“. Als „WZ“ fehlt die Zeile.
8. Einstellungen → „VERWALTUNG“ → „Rechte …“: zwei neue Zeilen „Ärztliche Informationen ansehen/bearbeiten“.

Version 0.29.7:

1. Nach dem Update: Die Demo hat R1, R2, R3; im Datenordner liegt `dokumentationssystem-vor-0.29.7.db`
   (die alte Datenbank). `update.log` nennt die Umstellung.
2. Übersicht: oben „R1 · 8 Bewohner · 2 Betten frei“. Auswahl „R2“ → zwei Bewohner, „7 Betten frei“;
   „R3“ → niemand, „9 Betten frei“.
3. Einstellungen → „WOHNBEREICHE“ → „Zimmer und Betten …“: 18 Zimmer, z. B. 101 „Bett Fenster: Anton
   Ahorn · Bett Tür: Hanna Holunder“. Als „EF“: neues Zimmer 307 (R3, Doppelzimmer); Zimmer 101
   stilllegen → Hinweis „belegt“. R3 auf „offen“ umschalten und zurück.
4. „Neuer Bewohner …“: Wohnbereich R1 → nur „105 – Bett Tür“ und „106 – Bett Tür“; R2 → mehrere freie
   Betten. Kein Feld „offen geführt“. Speichern → in der Akte „offen geführt: nein“, Marke „Zimmer 106 ·
   Bett Tür“.
5. Akte Dieter Distel → „Bearbeiten …“: „104 – Einzelzimmer (bisher)“; R2 wählen → „204 – Einzelzimmer“
   → Speichern → Distel steht in R2, 104 ist wieder frei.

Version 0.29.6:

1. Als „AB“: Nachrichten → Posteingang: an jeder Zeile „Gelesen“ → die Zeile verschwindet, der Zähler
   sinkt. Zwei Häkchen setzen → „Markierte als gelesen (2)“ → beide weg.
2. Archiv: die Nachrichten stehen dort; „Wieder als ungelesen“ → wieder im Posteingang, fett, „neu“.
3. Als „CD“: eine Team-Nachricht, die AB gelesen hat, ist bei CD weiter ungelesen.
4. „Neuer Bericht …“: im Datum `08112026` tippen → `08.11.2026`; `081126` tippen, Feld verlassen →
   `08.11.2026`. Uhrzeit `0930` → `09:30`. Dasselbe in Kalender, Aufgaben, Medikamentenplan.
5. Bewohner öffnen: oben groß „Bewohnerakte“, der Name nur auf der Karte darunter.

Version 0.29.5:

1. „Visiten“ → „Visite beginnen …“: Datum heute, Arzt wählen (z. B. Dr. Linde), „dabei“ mit eigenem
   Kürzel → „Beginnen“. Die Ansicht füllt die Fläche; links die Bewohner, oben „noch offen: … von …“.
2. Rechts beim ersten Bewohner: Allergien, Medikation wie im Medikamentenplan, „Letzte Visite: …“.
   Besprochenes eintippen → „Nächster ▶“: Häkchen links gesetzt, „noch offen“ eins weniger.
3. Bei einem Bewohner „Anordnungen geändert“ (Dosis geändert, ein Medikament) und bei einem anderen
   „Facharzttermin nötig“ mit Fachrichtung → oben „2 Aufgaben vorgemerkt“, keine Einblendung.
4. „Unterbrechen“ → Knopf heißt „Visite fortsetzen“. Programm schließen und neu starten →
   „Visite fortsetzen“ → alle Einträge sind noch da.
5. „Visitenliste drucken …“ → Vorschau: je Bewohner ein Block mit Medikation und Notizlinien.
6. „Visite abschließen …“ → eine Einblendung mit allen Aufgaben; die Visite steht in der Liste, je
   Bewohner ein Bericht in der Akte.

Version 0.29.4:

1. Einstellungen → „VERWALTUNG“ → „Ärzteverzeichnis …“: sieben ausgedachte Ärzte, Dr. Weide inaktiv.
   Als „CD“ nur ansehen (Knöpfe grau); als „EF“ „Neuer Arzt …“ und „Inaktiv / wieder aktiv“.
2. Akte → Medikamentenplan → „Neue Verordnung …“: „Angeordnet von“ ist eine Liste (nur aktive Ärzte);
   „Arzt anlegen …“ daneben → neuer Arzt ist gleich gewählt.
3. „Neue Visite …“: Arzt aus der Liste. Facharzttermin eintragen: Arzt wählen → Ort ist ausgefüllt.
4. Kalender → „Neuer Termin …“ → „Arzt (freiwillig)“ wählen → Ort und Hinweis (Name, Praxis, Telefon)
   sind ausgefüllt.

Version 0.29.3:

1. Anmeldefenster: oben „TST – Test Administrator · Administrator · Fachkraft“. Als TST anmelden → oben rechts
   „Angemeldet: TST – Test Administrator“ und der Knopf „Abmelden (F12)“.
2. Einstellungen → „VERWALTUNG“ → „Rechte …“: erste Spalte „Administrator“ voll und grau; ein Klick
   ändert nichts. „Mitarbeiter …“ → TST ändern: „aktiv“ und „Administrator“ gesperrt, mit Hinweis.
3. Bewohner öffnen → „Neuer Bericht …“ → etwas eintippen, Fenster offen lassen, **F12** → Rückfrage
   „Als Entwurf speichern“ → nur noch die leere Fläche und das Anmeldefenster.
4. Als „CD“ anmelden → kein „Entwurf fortsetzen“. Abmelden (Knopf oben rechts), als TST anmelden →
   oben „Entwurf fortsetzen“ → der Bericht mit dem Text → Speichern → der Knopf ist weg.
5. Seitenleiste einklappen: unten das Symbol „Abmelden (F12)“ (Hinweis beim Darüberfahren).
6. Einstellungen → „ICH“ → „Abmelden (F12)“. Anmeldefenster → „Programm beenden“ schließt alles.

Version 0.29.2:

1. Die vorhandene `Dokumentationssystem.exe` starten: Sie lädt das Update und startet neu; die Datei
   heißt weiter `Dokumentationssystem.exe`. Fenstertitel „DokuLine – Demo — ausgedachte Daten“, oben
   links „DokuLine“ und darunter „Betreuung auf Augenhöhe“.
2. Alle bisherigen Daten (Bewohner, Berichte, Anmeldung, Logo) sind da. Im Explorer `%APPDATA%`:
   neuer Ordner `DokuLine` mit denselben Dateien, `Dokumentationssystem` liegt unverändert daneben.
   In `DokuLine\update.log` steht „Datenordner übernommen: … Dateien …“.
3. Anmeldefenster: „DokuLine“ und „Betreuung auf Augenhöhe“. Einstellungen → „PROGRAMM“ → „Über“.
4. Ein Ausdruck (z. B. Übergabeblatt): unten links „DokuLine · Demo — ausgedachte Daten“.
5. Releases auf GitHub: im neuesten Release `DokuLine.exe` und `Dokumentationssystem.exe`, je mit
   `.sha256`. Der Link aus dem README lädt `DokuLine.exe`.

Version 0.29.1:

1. Als „CD“ angemeldet: „Neuer Bericht …“ → „Eingetragen von CD“ lässt sich nicht ändern. Ebenso
   „Nachtrag …“ und „Neue Aufgabe …“.
2. „Neue Fahrt …“: „Fahrer“ ist eine Liste der Mitarbeiter, „Carl Dohle (CD)“ vorgewählt.
3. „Neue Visite …“: „Dabei“ hat ein Häkchen bei CD; weitere ankreuzen. Kalender, neuer Ausflug:
   „Begleitung“ ebenso.
4. Oben rechts abmelden: „Neue Aufgabe …“, „Notiz für den Wohnbereich …“, „Neuer Termin …“ sind grau
   („Bitte zuerst anmelden.“). Wieder anmelden → frei.

Version 0.29.0:

1. Seitenleiste: „Visiten“ steht unter „Übergabe“. Drei Demo-Visiten; Bewohner „Anton Ahorn“ →
   nur seine („Quetiapin: Dosis geändert“). „von“ = vorgestern → nur die Visite von gestern;
   „bis“ = „abc“ → roter Hinweis.
2. „Neue Visite …“ ohne Angaben → „Speichern“ → Hinweise. Dann Art „Psychiater“, Arzt „Dr. Test“,
   Anton Ahorn hinzufügen: Besprochenes, „Anordnungen geändert“: Dosis geändert – Quetiapin; weitere
   Anordnung: neu verordnet – „Testsalbe“ (eintippen). Berta Birke: Besprochenes, „Facharzttermin
   nötig“: Kardiologie, dringend → Speichern.
3. Sofort „Aufgaben aus der Visite“ mit drei Knöpfen. „Im Medikamentenplan eintragen …“ → Akte Anton
   Ahorn, Reiter „Medikamentenplan“, Fenster „Stufenplan – Quetiapin“ mit Arzt, Datum und „laut
   Visite …“; eine Stufe ab heute → Speichern → die Einblendung kommt mit zwei Aufgaben wieder.
4. „Termin eintragen …“: Datum in 5 Tagen, 10:00, Ort; „Fahrt mit Fahrzeug eintragen“ 09:30–11:30 →
   Speichern → Kalender: „Facharzt: Kardiologie“ bei Berta Birke; Fahrt beim VW Bus.
5. „Später“ → unter „Aufgaben“ steht „Neues Medikament anlegen: Testsalbe …“ (aus einer Visite) mit
   „Medikament anlegen …“ → Name vorbelegt → Speichern → „Neue Verordnung“ mit Testsalbe → Menge →
   Speichern → Aufgabe erledigt.
6. Akte Berta Birke, Reiter „Berichte“: „Visite … Facharzttermin nötig: Kardiologie, dringend.“
   Übergabe: „Visite: Facharzttermin nötig“.
7. Als „KL“ (keine Fachkraft): Visite eintragen geht; „Im Medikamentenplan eintragen …“ ist grau
   („Nur für Fachkräfte.“).

Version 0.28.0:

1. Aufgaben: Überschriften nach Schicht („JETZT: …“ oben, davor ggf. „AUS FRÜHEREN SCHICHTEN
   OFFEN“). Im Nachtdienst stehen Rundgänge, Türen/Fenster, Nachtbericht als „Standardaufgabe“.
2. Eine Standardaufgabe aus einer früheren Schicht: Marke „aus der Nachtschicht offen“; „Schließen
   mit Begründung …“ ohne Text → Hinweis; mit Text → weg; „Erledigte zeigen“ → „geschlossen …:
   Begründung“.
3. „Standardaufgaben …“: als „CD“ nur ansehen; als „EF“ „Neue Standardaufgabe …“ (z. B. Montag
   09:00, Frühdienst) → in der Liste; „Stilllegen“ → entsteht nicht mehr.
4. Einstellungen → „SCHICHTEN“: z. B. Nacht 21:45 → Speichern → Berichte und Aufgaben richten sich
   danach. Falsche Reihenfolge → Hinweis, nichts gespeichert.
5. „Aufgaben der Schicht drucken …“ → Vorschau mit Überschrift „Aufgaben – Nachtdienst …“.

Version 0.27.0:

1. Übersicht → „FAHRZEUGE“: Caddy „unterwegs seit …, zurück geplant … (Fahrer KL)“, VW Bus „vor
   Ort“. Emil Eiche hat auf seiner Karte „abwesend: unterwegs mit Caddy bis …“. Nach einiger Zeit
   wird der Caddy rot („Rückkehr überschritten“).
2. Beim Caddy „zurück“ → „vor Ort“, Emil Eiche ist nicht mehr abwesend.
3. „Neue Fahrt …“: VW Bus, heute, 23:00–23:30, Sporttherapie, Berta Birke, Ziel „Hallenbad“ →
   beim VW Bus „nächste Fahrt 23:00“. Noch eine Fahrt mit dem VW Bus um 23:10 → gelbe Warnung
   „Doppelbuchung“, Knopf heißt „Trotzdem speichern“.
4. VW Bus „losgefahren“ → „unterwegs seit …“; Berta Birke abwesend; im Tagesplan „Fahrt“.
5. Kalender: oben dieselben Fahrzeuge. „nur Haus“ → Hausversammlung und Brandschutzübung (grau,
   „Haus: …“). Als „CD“: Haus-Termine nur ansehen (Knöpfe grau), im neuen Termin ist „Haus“ aus.
   Als „EF“ (Bereichsleitung): Haus-Termin anlegen und ändern geht.
6. Einstellungen → „Fahrzeuge …“: „Kleinbus“ hinzufügen, stilllegen → steht bei neuen Fahrten
   nicht mehr zur Wahl.

Version 0.26.0:

1. Links unter „Übersicht“ steht „Tagesplan“. Oben „Heute, …“; darunter alle Termine von heute
   nach Uhrzeit (Therapien, Wochenplan, Kalender). An Tagen ohne Therapie: „Heute stehen keine
   Termine an.“ oder nur Wochenplan-Einträge.
2. Eine Therapie, die schon begonnen hat: gelbe Marke „noch offen“. „gegangen“ drücken → grüne
   Marke „gegangen 09:05 · CD“; im Wochenplan des Bewohners steht „teilgenommen“.
3. Danach Bewohner-Übersicht und Akte: Marke „in Therapie: … bis …“. Nach dem Ende der Therapie
   verschwindet sie von selbst (spätestens nach 30 Sekunden im Tagesplan, sonst beim nächsten
   Öffnen).
4. „nach Bewohner“: je Bewohner eine Überschrift. „alle Bereiche“: auch die Termine von
   Wohnbereich B.
5. Zwei Fenster am selben gemeinsamen Ordner (Mehrplatz): „gegangen“ im einen → im anderen nach
   höchstens 30 Sekunden sichtbar.
6. Abmelden: Die Knöpfe sind grau („Bitte zuerst anmelden.“).

Version 0.25.0:

1. Bereich „Therapie“: Jede Karte zeigt zuerst den Anbieter („Therapie“, „Wohnbereich A“ bei der
   Kochgruppe, „Wohnbereich B“ bei der Kreativwerkstatt).
2. Oben „Anbieter“ → „Wohnbereich A“ → nur die Kochgruppe; „alle Anbieter“ → wieder alle.
3. Als „OP“ (Therapie): Holzwerkstatt lässt sich ändern und zuteilen; bei der Kochgruppe sind die
   Knöpfe grau („Dafür fehlt die Berechtigung.“). „Neues Angebot …“ → Anbieter nur „Therapie“.
4. Als „EF“ (Bereichsleitung A): Kochgruppe ja, Holzwerkstatt nein, Kreativwerkstatt „Nur für
   Wohnbereich B.“. „Neues Angebot …“ → Anbieter „Wohnbereich A“.
5. Als „CD“ (Mitarbeiter A): alles sichtbar, nichts änderbar. Als „UT“ in den Einstellungen →
   „Rechte …“ bei „Therapien des eigenen Bereichs bearbeiten“ ein Häkchen für „Mitarbeiter
   Wohnbereich“ → als „CD“ ist die Kochgruppe änderbar, die Holzwerkstatt nicht.

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
