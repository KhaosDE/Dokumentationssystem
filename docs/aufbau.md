# Aufbau von DokuLine

Für alle, die den Code ansehen oder prüfen. Regeln für Änderungen: `docs/code-regeln.md`,
Gestaltung: `docs/gestaltung.md`, Stand und Entscheidungen: `STAND.md`.

## Projekte

| Ordner | Wofür | Hängt ab von |
|---|---|---|
| `src/DokuLine.Kern/` | Datenmodell, Prüfregeln, Speicher (SQLite), Demo-Daten. Ohne Fenster, vollständig testbar. | – |
| `src/DokuLine.Bausteine/` | Gemeinsame Bausteine der Oberfläche: `Stil` (Farben, Schriften, Abstände), Knöpfe, Karten, Kopfleisten, Formulare, die Wahl des Bereichs (`Arbeitsbereich`). | Kern |
| `src/DokuLine/` | Das Windows-Programm: Hauptfenster, Seitenleiste, alle Bereiche und Fenster. Ruft nur den Kern auf, kein SQL. | Kern, Bausteine, Module, Update |
| `src/DokuLine.Modul.Ausgangsliste/` | Ein Modul (einschaltbar in den Einstellungen): eigene Tabellen, Seite, Reiter in der Akte. | Kern, Bausteine |
| `src/DokuLine.Update/` | Selbst-Update über die Releases dieses Repositorys (einziger Netzzugriff neben BfArM). | Kern |
| `tests/DokuLine.Tests/` | Tests des Kerns (laufen überall). | Kern |
| `tests/DokuLine.Oberflaeche.Tests/` | Tests der Oberfläche (nur unter Windows, in der CI). | alles |
| `daten/demo/` | Ausgedachte Demo-Daten als lesbare JSON-Dateien. | – |
| `werkzeuge/` | Hilfsskripte (Programmsymbol, Code-Maße). | – |

## Wie Daten fließen

Fenster → Kern → Speicher → SQLite-Datei im Datenordner (`%APPDATA%\DokuLine\`).

- Ein Fenster liest Eingaben als Text in eine `…Eingabe`-Klasse (z. B. `BerichtEingabe`).
- Die Regeln im Kern (`…Regeln.AusEingabe`) prüfen und bauen daraus einen Datensatz; Fehler kommen alle
  auf einmal als `Eingabefehler`.
- `Speicher` schreibt mit Parametern; vieles wird nie überschrieben (Berichte, Aufgaben, Verordnungen:
  Änderungen sind Nachträge, Ereignisse oder neue Fassungen). Die Datenbank sichert das mit Triggern ab.
- Der Aufbau der Datenbank steht in `Speicher.Schritte` (ein Schritt je Änderung, nur anhängen; die Nummer
  steht in `PRAGMA user_version`).
- Mehrere Rechner mit demselben Datenordner: Der Speicher meldet Änderungen, das Hauptfenster liest
  die gezeigte Ansicht neu.

## Wo man was findet

| Thema | Kern | Oberfläche |
|---|---|---|
| Bewohner, Akte | `Bewohner.cs`, `Speicher.cs` | `AkteAnsicht.cs`, `BewohnerFormular.cs` |
| Dokumentation (Berichte) | `Berichte.cs`, `Dokumentation.cs` | `BewohnerBereich.cs`, `DokumentationAnsicht.cs`, `BerichtZeile.cs` |
| Übergabe | `Uebergabe.cs` | `UebergabeSeite.cs` |
| Aufgaben, Standardaufgaben | `Aufgaben.cs`, `SpeicherAufgaben.cs` | `AufgabenSeite.cs`, `StandardaufgabenFenster.cs` |
| Wahl des Bereichs | `Bereichswahl.cs` | `BereichsAuswahl.cs` (Seitenleiste), `Arbeitsbereich` (Bausteine) |
| Rechte | `Mitarbeiter.cs` (`Funktionen`, `RechteRegeln`) | `Recht.cs`, `VerwaltungFenster.cs` |
| Medikation, Bedarf | `Medikation.cs`, `Bedarf.cs` | `MedikationAnsicht.cs`, `BedarfFenster.cs` |
| Ziele und Maßnahmen | `Ziele.cs`, `SpeicherZiele.cs`, `Massnahmen.cs`, `SpeicherMassnahmen.cs` | `MassnahmenAnsicht.cs` (Reiter „Ziele und Maßnahmen“), `ZielFenster.cs`, `ZielLeiste.cs`, `MassnahmeFenster.cs`, `MassnahmenListe.cs` |
| Medikamentenausgabe | `Medikamentenausgabe.cs`, `SpeicherAusgabe.cs` | `AusgabeSeite.cs` |
| Zeitauswertung | `Zeitauswertung.cs`, `SpeicherZeitauswertung.cs` | `AuswertungSeite.cs`, `TabellenDruck.cs` |
| Qualität (QMB) | `Qualitaet.cs`, `SpeicherQualitaet.cs` | `QualitaetSeite.cs` |
| Archiv | `Archiv.cs`, `SpeicherArchiv.cs`, `DemodatenArchiv.cs` | `ArchivSeite.cs`, `ArchivFenster.cs` (Entlassen, Wiederaufnahme, Akte nur lesend) |
| Hilfeplangespräche | `Hilfeplan.cs`, `SpeicherHilfeplan.cs` | `HilfeplanSeite.cs` (Zeile im Reiter „Kostenträger“: `AngabenAnsicht.cs`) |
| Schlüsselwörter | `Schluesselwoerter.cs`, `SpeicherSchluesselwoerter.cs` | `VorschlagLeiste.cs`, `SchluesselwoerterFenster.cs`, Folgeschritte in `BewohnerBereich.cs` |
| Rechtschreibung | `Rechtschreibung.cs`, `SpeicherRechtschreibung.cs` | `Rechtschreibpruefung.cs` (Windows-Prüfung, Wellenlinie, Rechtsklick), `FachwoerterFenster.cs` |
| Abwesenheit in der Dokumentation | `Abwesenheit.cs`, `SpeicherKalender.cs` (Zeiträume), Ausgangsliste `Datenbank.cs` (Beginn/Ende) | `AbwesenheitAnzeige.cs`, `DokumentationAnsicht.cs` (Zeile, Balken) |
| Medikamentenbestellung | `Bestellung.cs`, `SpeicherBestellung.cs` | `BestellungSeite.cs`, Karte in den Einstellungen |
| HEB-Fristen | `HebFrist.cs`, `SpeicherHebFrist.cs` | `HebFristenSeite.cs`, `HebFristAblauf.cs`, Reiter „HEB“ der Akte |
| Visiten | `Visiten.cs`, `VisitenEntwurf.cs` | `VisitenSeite.cs`, `VisitenRundgang.cs` |
| Therapie, Tagesplan | `Therapie.cs`, `Tagesplan.cs`, `LaufendeTherapien.cs` | `TherapieSeite.cs`, `TagesplanSeite.cs`, `LaufendeTherapienAnsicht.cs` |
| Kalender, .ics | `Kalender.cs`, `Ics.cs` | `KalenderSeite.cs` |
| Personenverzeichnis | `Aerzte.cs`, `SpeicherAerzte.cs` | `VerzeichnisAnsicht.cs` |
| HEB-Bogen, Reflexion | `Heb.cs`, `HebPdf.cs`, `Reflexion.cs` | `HebFenster.cs`, `ReflexionAnsicht.cs` |
| Einstellungen | `Einstellungen.cs` | `EinstellungenSeite.cs` |

## Glossar

- **Bereich** – ein Wohnbereich (R1, R2, R3) oder eine andere Einheit (Therapie, Leitung, Verwaltung).
  **Wohnbereich**: ein Bereich mit Zimmern und Bewohnern. In der Seitenleiste wird der Bereich gewählt, der
  überall gilt; „eingesprungen“ heißt: ein anderer als der des Rechners.
- **Bewohner** – eine Person, die im Wohnheim lebt (in der Demo ausgedacht).
- **Akte** – alles zu einem Bewohner: Stammdaten, Medikamentenplan, Wochenplan, Biografie, Reflexion, HEB.
- **Maßnahme** – etwas Regelmäßiges, das am Bewohner zu tun ist (Lebensbereich, Rhythmus, Dienst, Zeitwert); jede
  Durchführung wird bestätigt und steht als kurzer Eintrag (Berichtsart „Maßnahme“) in der Dokumentation.
- **Bericht** – ein Eintrag der täglichen Dokumentation mit Schicht, Kürzel, Lebensbereichen und Berichtsart;
  wird nie geändert, Korrekturen sind **Nachträge**.
- **Übergabe** – was die nächste Schicht wissen muss; steht, bis es abläuft oder erledigt ist.
- **Schicht** – Früh-, Spät- und Nachtdienst; die Zeiten gelten je Bereich.
- **Kürzel** – die Anmeldung eines Mitarbeiters (z. B. „AB“); steht bei allem, was jemand einträgt.
- **Position** – z. B. „Mitarbeiter Wohnbereich“, „Bereichsleitung“; an ihr hängen die **Rechte**.
  **Assistenzkraft / Fachkraft** – die Qualifikation; manches ist nur für Fachkräfte.
- **Beschluss** – richterlicher Beschluss zur Unterbringung, mit Enddatum.
- **Bezugspflege / Einzeltrainer** – die feste Ansprechperson eines Bewohners bzw. die Person für Einzeltraining.
- **Bedarf / Bedarfsgabe** – Medikament „bei Bedarf“; jede Gabe wird mit Uhrzeit, Menge und Anlass eingetragen.
- **Visite** – Besuch von Psychiater oder Hausarzt; Ergebnisse werden Berichte und Aufgaben.
- **HEB** – Hilfebedarfserhebung: Bogen des Kostenträgers (PDF), aus den Berichten vorbereitet.
- **Forensik / FOSA** – Forensische Klinik bzw. Forensische Ambulanz (Nachsorge); Zuständige im
  Personenverzeichnis.
- **Modul** – ein Teil, den man in den Einstellungen ein- und ausschaltet (z. B. Ausgangsliste).
- **Demo** – das Programm mit ausgedachten Daten; „Demo — ausgedachte Daten“ steht immer sichtbar da.
