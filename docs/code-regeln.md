# Code-Regeln — DokuLine

Stand: 09.11.2026 (Vorgabe Tobi: der Code soll effektiver werden und für jemanden, der ihn
prüft, leicht zu verstehen sein). Gilt für **jede** Änderung am Code. Ausnahmen nur mit
Begründung in `STAND.md`, Abschnitt „Entscheidungen“.

Ausgangslage 09.11.2026 (zum Vergleich nach dem Aufräumen): rund 31.700 Zeilen Programmcode,
20 Dateien über 400 Zeilen, 2.140 Zeilen über 120 Zeichen, rund 2.000 Kommentarzeilen, davon
viele mit Versions- oder Schrittverlauf.

## 1. Kommentare: warum, nicht was und nicht wann

1. Der Code zeigt, **was** passiert. Ein Kommentar erklärt nur, **warum** etwas so ist, wenn
   man es dem Code nicht ansieht (Fachregel, Ausnahme, Falle).
2. **Kein Verlauf im Code.** Keine Kommentare wie „Schritt 26a3“, „0.33.0: vorher …“, „neu in
   …“. Der Verlauf steht in den Commits, in `STAND.md` und in `SITZUNGEN.md`.
3. Jede Datei beginnt mit **höchstens drei Zeilen**: was diese Datei tut.
4. Ein Kommentar ist höchstens drei Zeilen lang. Längeres gehört nach `docs/`.
5. Fachregeln von Tobi mit kurzem Verweis: `// Fachregel: IDEEN.md „Übergabe nach Schichten“`.
6. Auskommentierter Code wird gelöscht, nicht stehen gelassen.

Hinweis: Kürzere Kommentare machen die **Quelldateien** kleiner und lesbarer. Die fertige
`DokuLine.exe` wird dadurch weder kleiner noch schneller — Kommentare kommen dort gar nicht an.
Schneller machen das Programm die Regeln in Abschnitt 5.

## 2. Größe und Aufbau

1. **Datei höchstens 400 Zeilen**, **Methode höchstens 40 Zeilen**, **Zeile höchstens 120
   Zeichen** (Richtwerte; darüber nur mit Grund).
2. **Eine Datei, eine Aufgabe.** Große Fenster teilen in Aufbau (Steuerelemente anlegen),
   Füllen (Daten anzeigen) und Handeln (Klicks, Speichern) — z. B. mit `partial class` in
   `XyzSeite.cs`, `XyzSeite.Fuellen.cs`, `XyzSeite.Handeln.cs`.
3. **Regel der drei:** Steht dasselbe zum dritten Mal im Code, wird es zusammengefasst.
4. **Gleiche Dinge gleich bauen:** Alle Bereiche der Seitenleiste erfüllen dieselbe
   Schnittstelle (z. B. `IBereichSeite` mit `KopfText`, `Knoepfe`, `Zeigen()`), statt dass das
   Hauptfenster für jede Seite einen eigenen `if`-Block hat.
5. **Kern und Oberfläche getrennt:** Regeln, Berechnungen und Datenbankzugriffe nur im Kern
   (`DokuLine.Kern`). Die Oberfläche zeigt an und ruft den Kern auf — kein SQL in Fenstern.
6. Farben, Schriften, Abstände nur aus `Stil.cs` (keine Zahlen für Farben im Fenstercode).

## 3. Namen

1. Deutsch, ausgeschrieben, ohne Umlaute im Code (`Uebergabe`, `Aerzte`); Oberflächentexte mit
   Umlauten.
2. Ein Begriff, ein Wort — wie im Glossar in `docs/aufbau.md` (z. B. immer „Bereich“, nicht
   abwechselnd „Wohnbereich“, „Station“, „Gruppe“ für dasselbe).
3. Keine Abkürzungen außer festen Fachkürzeln (HEB, FOSA, BfArM) und `Kuerzel`.
4. Methoden sagen, was sie tun: `BerichteLaden`, `UebergabeErledigen`; Wahrheitswerte als
   Frage: `IstAbgelaufen`, `HatRecht`.

## 4. Für den Prüfer lesbar

1. **`docs/aufbau.md`** (eine bis zwei Seiten): Projekte und wofür sie da sind, wie Daten
   fließen (Fenster → Kern → Speicher), wo man was findet, Glossar der Fachbegriffe (Übergabe,
   Beschluss, HEB, Bezugspflege, Forensik, FOSA …). Wird bei jeder größeren Änderung
   nachgezogen.
2. Jede Fachregel hat einen Test, dessen Name sie beschreibt:
   `Uebergabe_KeineBesonderheiten_wirdNichtUebergeben`.
3. Ein Commit, ein Zweck; die Nachricht sagt was und warum.
4. Neue Funktionen und Aufräumen nicht im selben Pull Request mischen.

## 5. Tempo

1. Daten nur laden, wenn sie sich geändert haben (Änderungszähler) — nicht bei jedem Wechsel
   der Ansicht und nie beim Zeichnen.
2. Listen füllen zwischen `BeginUpdate()`/`EndUpdate()`, Aufbau zwischen
   `SuspendLayout()`/`ResumeLayout()`.
3. Einstellungen einmal laden und im Speicher halten; nach dem Speichern neu laden.
4. Teures Zeichnen (Verläufe) einmal als Bild zwischenspeichern.
5. Vorher messen, nachher messen; Zahlen in `STAND.md`.

## 6. Sicherheit und Datenschutz

1. SQL nur mit Parametern, nie mit zusammengesetzten Texten.
2. Keine Passwörter, Schlüssel oder Tokens im Code oder in Dateien im Repository.
3. Keine Gesundheits- oder Personendaten in Protokollen, Fehlermeldungen und Absturzberichten —
   dort nur Kennungen (Bewohner-Nr.), nie Namen oder Texte.
4. Was ins Netz geht, steht an genau einer Stelle im Code (Update, BfArM, LAN-Anzeige) und
   nirgends sonst.

## 7. Aufräumen

1. **Pfadfinderregel:** Wer eine Datei ändert, lässt sie etwas aufgeräumter zurück (Verlauf-
   Kommentare in diesem Abschnitt kürzen, Namen angleichen) — ohne fremde Teile umzubauen.
2. Größeres Aufräumen nur als eigener Schritt im Bauplan; das Verhalten bleibt gleich, alle
   Tests bleiben grün.
3. Automatisch geprüft: `.editorconfig` mit diesen Regeln und `dotnet format --verify-no-changes`
   im Windows-Build. Größen (Datei, Methode, Zeile) meldet ein kleines Skript in `werkzeuge/`
   als Liste in jedem Build — zunächst als Hinweis, nicht als Fehler.
