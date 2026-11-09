# DokuLine im Betrieb (Terminalserver)

Stand: 09.11.2026, Version 0.41.0. Für Tobi als Verwalter, in einfachen Worten.

> **Vor dem ersten echten Datensatz** müssen alle Freigaben unten vorliegen (`CLAUDE.md`, Regel 4).
> Bis dahin wird der Betriebsmodus nur mit **ausgedachten Daten** ausprobiert – am besten zuerst auf
> einem Test-Rechner („Betriebsprobe“), bevor der echte Server berührt wird.
>
> Diese Anleitung ist **nicht auf einem echten Server getestet** (gebaut und geprüft wurde unter Linux und in
> der Windows-Testumgebung von GitHub). Die Befehle für Ordnerrechte und Aufgabenplanung bitte bei der
> Betriebsprobe Schritt für Schritt ausprobieren.

## Was ist anders als in der Demo?

| | Demo | Betrieb |
|---|---|---|
| Woran erkennbar | keine Datei `betrieb.json` | Datei `betrieb.json` im Programmordner |
| Daten | `%APPDATA%\DokuLine` (je Benutzer) | gemeinsamer Ordner aus `betrieb.json` |
| Einstellungen (Kürzel, Menü) | im Datenordner | je Benutzer in `%APPDATA%\DokuLine` |
| Demo-Daten | ja, „Demo-Daten zurücksetzen“ | nein |
| Anmelden | Kürzel ohne Passwort | Kürzel **mit Passwort** |
| Sperre | F12 | F12 und automatisch nach Leerlauf (Vorgabe 10 Minuten) |
| Update | Selbst-Update beim Start | „Update vorbereiten …“, nachts eingespielt |
| Hinweis unten | „Demo — ausgedachte Daten“ | „Betrieb“ |

## 1. Welcher Windows Server?

DokuLine ist eine einzelne `.exe` mit allem, was sie braucht („self-contained“) – **.NET muss nicht
installiert werden**. Die Version des Servers muss aber zu .NET 10 passen. Laut Microsoft
(„Install .NET on Windows“, Abschnitt „Supported versions“,
<https://learn.microsoft.com/dotnet/core/install/windows#supported-versions>, gelesen am 09.11.2026):

- **Windows Server 2025, 2022, 2019, 2016** (64 Bit): unterstützt. Empfehlung: 2022 oder 2025.
- **Windows Server 2012 und 2012 R2**: von .NET 10 noch unterstützt, brauchen aber Zusatzpakete; das
  Betriebssystem selbst bekommt von Microsoft keine regulären Updates mehr. **Nicht für Gesundheitsdaten verwenden.**
- 32 Bit gibt es für Windows Server nicht.

Welche Version auf dem Server läuft, steht unter **Einstellungen → System → Info** oder nach `winver`
(Startmenü → „Ausführen“ → `winver`).

## 2. Ordner und Rechte

Drei Ordner (Beispiele; Laufwerke nach dem eigenen Server):

| Ordner | Zweck | Wer darf ändern |
|---|---|---|
| `C:\Program Files\DokuLine\` | Programm (`DokuLine.exe`, `betrieb.json`) | nur Administratoren |
| `D:\DokuLine\Daten\` | Datenbank, Bilder, Ablage der Dokumente | Gruppe **DokuLine-Benutzer** |
| `E:\DokuLine-Sicherungen\` | Sicherungen, möglichst **anderes Laufwerk** | nur Administratoren und das System |

**Windows-Gruppe anlegen** (PowerShell als Administrator):

```powershell
New-LocalGroup -Name "DokuLine-Benutzer" -Description "Darf mit DokuLine arbeiten"
```

Mitglieder hinzufügen (je Benutzerkonto auf dem Server, Name anpassen):

```powershell
Add-LocalGroupMember -Group "DokuLine-Benutzer" -Member "Benutzername"
```

**Ordnerrechte** (Eingabeaufforderung oder PowerShell als Administrator; `*S-1-5-32-544` ist die Gruppe
der Administratoren, `*S-1-5-18` das System – so klappt es auch auf deutschem Windows):

```powershell
icacls "C:\Program Files\DokuLine" /inheritance:r /grant:r "*S-1-5-32-544:(OI)(CI)F" "*S-1-5-18:(OI)(CI)F" "DokuLine-Benutzer:(OI)(CI)RX"
```

```powershell
icacls "D:\DokuLine\Daten" /inheritance:r /grant:r "*S-1-5-32-544:(OI)(CI)F" "*S-1-5-18:(OI)(CI)F" "DokuLine-Benutzer:(OI)(CI)M"
```

```powershell
icacls "E:\DokuLine-Sicherungen" /inheritance:r /grant:r "*S-1-5-32-544:(OI)(CI)F" "*S-1-5-18:(OI)(CI)F"
```

Alle anderen haben damit keinen Zugriff. Prüfen: Rechtsklick auf den Ordner → Eigenschaften → Sicherheit.

Die Datenbank liegt **lokal auf dem Server**, nicht auf einem Netzlaufwerk: Alle Sitzungen laufen auf demselben
Rechner, dafür ist SQLite gebaut. DokuLine schaltet im Betrieb den WAL-Modus ein (mehrere lesen, einer schreibt).

## 3. Installation

1. `DokuLine.exe` aus dem neuesten Release nach `C:\Program Files\DokuLine\` kopieren.
2. Daneben die Datei **`betrieb.json`** anlegen (Editor, als Administrator), zum Beispiel:

   ```json
   {
     "datenordner": "D:\\DokuLine\\Daten",
     "sicherungsordner": "E:\\DokuLine-Sicherungen",
     "sperre_minuten": 10,
     "protokoll_monate": 24,
     "sicherung_um": "02:30"
   }
   ```

   | Schlüssel | Bedeutung | Vorgabe |
   |---|---|---|
   | `datenordner` | gemeinsamer Datenordner (Pflicht, vollständiger Pfad) | – |
   | `sicherungsordner` | Ordner der Sicherungen | `<datenordner>\sicherungen` |
   | `sperre_minuten` | Sperre nach so vielen Minuten ohne Eingabe (1–120) | 10 |
   | `protokoll_monate` | so lange bleibt das Zugriffsprotokoll (1–120) | 24 |
   | `sicherung_um` | Uhrzeit der nächtlichen Sicherung | 02:30 |

   Ist die Datei fehlerhaft, startet DokuLine **nicht** und nennt den Fehler – es fällt nie still in die Demo zurück.
3. Eine Verknüpfung zu `DokuLine.exe` auf den Desktop der Benutzer (oder ins Startmenü für alle).
4. Windows SmartScreen warnt beim ersten Start („unbekannter Herausgeber“), weil die Datei nicht signiert ist:
   „Weitere Informationen“ → „Trotzdem ausführen“.

## 4. Erster Start und Benutzer

1. Als Verwalter DokuLine starten. Ist noch niemand angelegt, erscheint die **Ersteinrichtung**: Kürzel, Name und
   ein eigenes Passwort (mindestens 10 Zeichen, nicht das Kürzel). Der Verwalter bekommt die Position
   „Administrator“ und damit alle Rechte.
2. **Einstellungen → Verwaltung → Mitarbeiter**: die Mitarbeiter mit Bereichen und Positionen anlegen.
3. **Einstellungen → Verwaltung → Passwörter …**: je Mitarbeiter „Einmalpasswort vergeben“. Das Passwort steht
   nur dieses eine Mal da – persönlich weitergeben, nicht per E-Mail. Bei der ersten Anmeldung legt die Person
   ihr eigenes Passwort fest.
4. Nach **5 Fehlversuchen** ist ein Kürzel gesperrt. Entsperren: ein neues Einmalpasswort vergeben (Leitung oder
   Verwalter).
5. Gespeichert wird nie das Passwort selbst, sondern nur ein Hash (PBKDF2 mit SHA-256, 210 000 Durchläufe).
6. **Häuser** (Einstellungen → Verwaltung → Häuser …): beide Häuser anlegen und jedem Bereich sein Haus geben.
   Dann sieht jede Person in der Bereichswahl die Bereiche ihres Hauses; andere nur mit dem Recht „Bereiche anderer
   Häuser wählen“. Haus-Termine im Kalender gelten für das Haus des gewählten Bereichs.

## 5. Sicherung

- **Automatisch jede Nacht** über die Windows-Aufgabenplanung (Eingabeaufforderung als Administrator):

  ```powershell
  schtasks /Create /TN "DokuLine Sicherung" /TR "\"C:\Program Files\DokuLine\DokuLine.exe\" --sicherung" /SC DAILY /ST 02:30 /RU SYSTEM
  ```

  Fehlt die Aufgabe oder lief sie nicht, holt die nächste geöffnete DokuLine-Sitzung die Sicherung nach.
- Gesichert wird mit der Sicherungsfunktion von SQLite (auch während gearbeitet wird), danach wird jede Sicherung
  **geprüft** (vollständig lesbar, bekannter Stand). Behalten werden **30 tägliche und 12 monatliche** Stände.
- Für den Verwalter steht unten in der Statusleiste eine **Warnung**, wenn die letzte Sicherung älter als 2 Tage ist.
- **Einstellungen → System → Sicherung …**: „Jetzt sichern“, einen Stand „Prüfen“, „Zurücksichern …“.
- **Zurücksichern**: vorher alle anderen abmelden lassen. DokuLine legt den jetzigen Stand als Kopie neben die
  Datenbank (`…-vor-ruecksicherung.db`) und spielt dann den gewählten Stand zurück.
- Das Ergebnis jeder nächtlichen Sicherung steht in `update.log` im Datenordner.

## 6. Update einspielen

Im Betrieb aktualisiert sich DokuLine **nicht selbst**.

1. **Einstellungen → System → Update vorbereiten …**: „Herunterladen“ (nur von den Releases dieses Programms) oder
   „Datei wählen …“ (z. B. vom USB-Stick: `DokuLine.exe` und daneben `DokuLine.exe.sha256`). Die Prüfsumme wird
   geprüft; passt sie nicht, wird nichts vorbereitet.
2. Eingespielt wird nachts durch eine zweite Aufgabe – nur, wenn niemand DokuLine offen hat:

   ```powershell
   schtasks /Create /TN "DokuLine Update" /TR "\"C:\Program Files\DokuLine\DokuLine.exe\" --update-einspielen" /SC DAILY /ST 03:00 /RU SYSTEM
   ```

3. Die bisherige Fassung bleibt als `DokuLine.exe.alt` daneben. **Rückweg:** DokuLine überall schließen,
   `DokuLine.exe` löschen und `DokuLine.exe.alt` in `DokuLine.exe` umbenennen.
4. Ein Update bringt manchmal neue Datenbank-Schritte mit; die alte Fassung kann danach eine neuere Datenbank nicht
   öffnen. Dann zusätzlich die Sicherung von vor dem Update zurückspielen.

## 7. Zugriffsprotokoll

Wer wann welche Akte **geöffnet**, **gedruckt** oder **geändert** hat (nie Inhalte). Einsehen nur mit dem Recht
„Zugriffsprotokoll ansehen“ (vorbelegt: Geschäftsführung, Hausleitung; der Datenschutz kann es zusätzlich bekommen):
**Einstellungen → Verwaltung → Zugriffsprotokoll …**. Älteres als `protokoll_monate` wird beim Start gelöscht.

## 8. Technischer Schutz

- **Laufwerksverschlüsselung (BitLocker)** für alle Laufwerke des Servers mit Daten oder Sicherungen. Schützt, wenn
  ein Laufwerk oder der Server abhandenkommt.
- **Ordnerrechte** wie oben: Nur die Gruppe DokuLine-Benutzer kommt an die Daten.
- **Sperre** nach Leerlauf und F12.
- Eine **Verschlüsselung der Datenbank selbst** (z. B. SQLCipher, BSD-Lizenz) ist vorbereitet als Möglichkeit, aber
  nicht eingebaut: Sie schützt nur zusätzlich gegen jemanden, der schon Zugriff auf den Ordner hat, macht Sicherung und
  Rücksicherung schwieriger und braucht einen Schlüssel, der sicher verwahrt werden muss. Einbauen, wenn der
  Datenschutz sie fordert.

## 9. Wenn etwas nicht geht

| Was | Was tun |
|---|---|
| DokuLine startet nicht, Meldung zu `betrieb.json` | Datei prüfen (Pfade mit doppeltem `\\`, vollständige Pfade) |
| „Den Ordner … gibt es nicht oder er ist nicht erreichbar“ | Laufwerk/Ordner und Rechte prüfen |
| Jemand ist gesperrt | Einstellungen → Verwaltung → Passwörter … → Einmalpasswort vergeben |
| Verwalter hat sein Passwort vergessen | ein zweiter Administrator vergibt ein Einmalpasswort – **deshalb mindestens zwei Personen mit Position „Administrator“** |
| Warnung „letzte Sicherung …“ | Aufgabenplanung prüfen (`update.log`), „Jetzt sichern“ |
| Datenbank beschädigt | DokuLine überall schließen, Sicherung zurückspielen |
| Nach einem Update geht etwas nicht | Rückweg (Abschnitt 6) und Rückmeldung über die Chats |

## 10. Freigaben vor dem ersten echten Datensatz

- [ ] **Einrichtung / Träger**: schriftliche Entscheidung für den Einsatz.
- [ ] **Datenschutzbeauftragter**: Datenschutz-Folgenabschätzung (Gesundheitsdaten, Art. 9 und Art. 35 DSGVO),
      technische und organisatorische Maßnahmen (Abschnitte 2, 5, 7, 8), Verzeichnis der Verarbeitungstätigkeiten,
      Löschfristen.
- [ ] **Mitarbeitervertretung**: Zugriffsprotokoll und Anmeldung sind technische Einrichtungen, die Verhalten
      erfassen können – Zustimmung bzw. Dienstvereinbarung.
- [ ] **Medizinprodukt ja/nein**: prüfen lassen, ob der Medikamentenplan (Stand, Stufenpläne, Hinweise) und die
      Vitalwerte (Hinweise auf Bereiche) als Medizinprodukt gelten.
- [ ] **Betriebsprobe** auf einem Test-Rechner mit ausgedachten Daten: Installation, Rechte, Anmeldung, Sperre,
      Sicherung, Rücksicherung, Update und Rückweg einmal komplett durchgespielt.
