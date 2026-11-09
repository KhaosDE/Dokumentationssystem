# Gestaltung — Vorbild Ausgangsliste

Grundlage für den Eintrag „Gestaltung nach Vorbild der Ausgangsliste“ in `IDEEN.md`.
Werte übertragen aus Tobis Programm „Ausgangsliste“ (C#, Stil „A3 Markenfarben“, Stand
04.11.2026). Das Repository der Ausgangsliste ist privat; hier stehen nur die Werte, kein
Code, **kein Logo und kein Name der Einrichtung**.

## Grundsätze (aus der Ausgangsliste, angelehnt an Windows 11)

1. **Farbe sparsam.** Der Normalfall ist neutral. Farbe bekommt nur, was Aufmerksamkeit
   braucht (Beschluss, Allergie, Abweichungen).
2. **Farbe nie allein.** Jede farbige Kennzeichnung hat zusätzlich ein Wort oder Zeichen
   (z. B. Marke „Beschluss“ neben dem Namen), dazu ein Farbschlüssel in der Statusleiste.
3. **Abstandsraster 4 / 8 / 12 / 16 px.** Jeder Abstand im Programm ist eines dieser Maße.
4. **Knopfhöhe 36 px**, Innenabstand links/rechts 12 px, Hover an allen Knöpfen.
5. **Schrift Segoe UI** (Systemschrift von Windows, keine Lizenzfrage, keine Abhängigkeit).
   Stufen: Kopftitel 22 pt Semibold, Überschrift 18 pt Semibold, Name auf Karte 12,75 pt
   Semibold, Text 10 pt, Kleingedrucktes/Marken 8,5–9 pt Semibold.
6. **Kartenraster** als Hauptansicht: weiße Karten auf heller Seite, feiner Rand.
7. **Kopf** weiß mit weichem Farbverlauf (Orange oben links, Blau rechts), rechts ein
   Logofeld. In DokuLine: Logofeld leer bzw. mit selbst gewähltem Bild aus den
   Einstellungen (Datei bleibt im Datenordner, nie im Repository).
8. **Hell als Vorgabe**, Dunkel umschaltbar (wirksam nach Neustart).
9. Symbole aus „Segoe Fluent Icons“ (Windows-11-Symbolschrift), wo vorhanden.
10. **Auf Anhieb zu finden** (Vorgabe Tobi, 09.11.2026: viele Kollegen sind technisch wenig
    bewandert, das Programm muss übersichtlich bleiben):
    - Jeder sieht nur, was er darf und braucht (fehlende Rechte blenden aus, nicht ausgrauen).
    - Gleiche Dinge stehen immer an derselben Stelle: Ort oben links in der Kopfzeile, Aktionen
      in der Leiste darunter, Hauptaktion links, Drucken rechts.
    - Knöpfe immer mit Wort, nie nur Symbol (Ausnahme: Kuvert und Einklappen mit Tooltip).
    - Keine versteckten Klappmenüs für Wichtiges; Umschalter statt Auswahllisten, wo es wenige
      Möglichkeiten gibt (z. B. Wohnbereich R1 · R2 · R3).
    - Einfache Wörter aus dem Alltag des Wohnbereichs, keine Fachbegriffe aus der Technik.
    - Der Wohnbereich wird einmal gewählt (Übersicht) und gilt überall.

## Farben, helle Darstellung (RGB)

| Rolle | Wert |
|---|---|
| Seite (Hintergrund hinter den Karten) | 238, 241, 247 |
| Fensterhintergrund | 243, 243, 243 |
| Karte / Fläche | 255, 255, 255 |
| Kartenrand | 217, 224, 236 |
| Text normal | 23, 32, 51 |
| Text mittel | 59, 70, 96 |
| Text leise | 91, 100, 120 |
| Trennlinie | 229, 229, 229 |
| Akzent Blau (Flächen, Knöpfe, Text auf Hell) | 45, 84, 156 |
| Akzent hell (markierte Zeile) | 222, 232, 245 |
| Akzent Orange | 238, 166, 60 |
| Verlauf Kopf: Orange / Blau | 238, 166, 60 / 84, 128, 198 |
| Erfolg (Grün) | 16, 124, 16 |
| Gefahr (Rot) | 196, 43, 28 |

**Marken (Fläche / Text):**

| Marke | Fläche | Text |
|---|---|---|
| Beschluss | 253, 231, 228 | 163, 34, 26 |
| Hinweis | 240, 234, 251 | 91, 58, 153 |
| Zimmer | 238, 242, 251 | 42, 63, 107 |
| Beschränkung / Achtung | 255, 248, 230 (Rand 243, 213, 140) | 122, 74, 0 |
| Unterwegs | 221, 244, 241 | 14, 102, 94 |
| Offen / in Ordnung | 227, 244, 230 | 30, 107, 46 |
| Zahl (Zähler auf Filterknöpfen) | 47, 91, 211 | 255, 255, 255 |

Die dunkle Darstellung hat eigene Werte; sie wird erst gebaut, wenn Tobi sie freigibt.

## Verfeinerung aus Tobis Designvorlagen (claude.ai, Prüfung 06.11.2026)

Geprüft wurden 6 von 41 Vorlagen (Präzis, Foyer, Wolkengarten, Frost, Pergament, Studio),
ausgewählt nach Namen als die ruhigsten. Alle sind für Werbe- und Startseiten im Netz gebaut
(große Schlagzeile, Preiskarten, Logo-Leisten), die meisten dunkel. **Als Ganzes passt keine**
zu einem Arbeitsprogramm für den Wohnbereich. Übernehmbar sind einzelne Regeln, die zu den
Grundsätzen oben passen:

- **Eine Hauptaktion pro Ansicht** in Akzentfarbe (aus „Präzis“); alle anderen Knöpfe neutral.
  Ausnahme wie in der Ausgangsliste: Aufnahme grün, Entlassung rot.
- **Haarlinien statt Schatten** (aus „Präzis“, „Wolkengarten“): Karten mit 1-px-Rand, höchstens
  ein Hauch Schatten (2 px, 3–5 % Deckkraft).
- **Seitenleiste:** aktive Zeile mit hellem Akzent als Fläche und Akzent als Textfarbe (aus
  „Wolkengarten“, `DocsSidebar`).
- **Abschnittsbeschriftung:** kleine Überschrift in Großbuchstaben, leicht gesperrt, über
  jedem Bereich einer Akte (aus „Wolkengarten“, `EyebrowLabel`), z. B. „STAMMDATEN“,
  „MEDIKATION“, „WOCHENPLAN“.
- **Wenige Radien:** 4 px für Knöpfe und Eingaben, 8 px für Karten.

Nicht übernommen: fremde Schriften (Inter u. a. bräuchten Mitlieferung), Verläufe außer im
Kopf, dunkle Flächen als Vorgabe, Pillen-Knöpfe.
