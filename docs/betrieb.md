# DokuLine im Betrieb (Terminalserver)

Stand: 09.11.2026, Version 0.41.1. Für Tobi als Verwalter, in einfachen Worten.

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
| Update | Selbst-Update beim Start | Verwalter bereitet vor, nachts eingespielt |
| Sicherung | – | jede Nacht als ZIP (Datenbank, Dokumente, Bilder) |
| Anzeige für den Fernseher | einschaltbar | aus (nicht verfügbar) |
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
| `E:\DokuLine-Sicherungen\` | Sicherungen, möglichst **anderes Laufwerk** | Administratoren, System und das Sicherungskonto |

**Windows-Gruppe anlegen** (PowerShell als Administrator):

```powershell
New-LocalGroup -Name "DokuLine-Benutzer" -Description "Darf mit DokuLine arbeiten"
```

Mitglieder hinzufügen (je Benutzerkonto auf dem Server, Name anpassen):

```powershell
Add-LocalGroupMember -Group "DokuLine-Benutzer" -Member "Benutzername"
```

**Konto für die nächtliche Sicherung** (PowerShell als Administrator). Die Sicherung läuft bewusst **nicht als
SYSTEM**: Sie liest die Datenbank, die alle DokuLine-Benutzer ändern dürfen – das soll kein Konto mit allen Rechten tun.
Das Passwort wird abgefragt und nirgends gespeichert außer in Windows:

```powershell
$pw = Read-Host -AsSecureString "Passwort für das Sicherungskonto"
```

```powershell
New-LocalUser -Name "DokuLine-Sicherung" -Password $pw -PasswordNeverExpires -Description "Nächtliche Sicherung von DokuLine"
```

```powershell
Add-LocalGroupMember -Group "DokuLine-Benutzer" -Member "DokuLine-Sicherung"
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
icacls "E:\DokuLine-Sicherungen" /inheritance:r /grant:r "*S-1-5-32-544:(OI)(CI)F" "*S-1-5-18:(OI)(CI)F" "DokuLine-Sicherung:(OI)(CI)M"
```

Alle anderen haben damit keinen Zugriff – auch die DokuLine-Benutzer nicht auf die Sicherungen. Prüfen: Rechtsklick auf den Ordner → Eigenschaften → Sicherheit.

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
   | `sicherungsordner` | Ordner der Sicherungen (Pflicht, vollständiger Pfad, **nicht** im Datenordner) | – |
   | `sperre_minuten` | Sperre nach so vielen Minuten ohne Eingabe (1–120) | 10 |
   | `protokoll_monate` | so lange bleibt das Zugriffsprotokoll (1–120) | 24 |
   | `sicherung_um` | Uhrzeit der nächtlichen Sicherung | 02:30 |

   Ist die Datei fehlerhaft, startet DokuLine **nicht** und nennt den Fehler – es fällt nie still in die Demo zurück.
   Eine Datenbank, die einmal im Betrieb geöffnet wurde, öffnet die Demo nie (sie würde dort Demo-Daten laden).
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
   ihr eigenes Passwort fest. Ein Einmalpasswort gilt **7 Tage**.
   Wer Einmalpasswörter vergeben darf: wer Mitarbeiter verwalten darf (Bereichsleitung nur im eigenen Bereich) –
   nie für sich selbst, nie für jemanden mit Rechten, die man selbst nicht hat, und für Administratoren nur ein
   Administrator.
4. Nach **5 Fehlversuchen** ist ein Kürzel gesperrt. Entsperren: ein neues Einmalpasswort vergeben (Leitung oder
   Verwalter). Ein unbekanntes Kürzel meldet dasselbe wie ein falsches Passwort.
5. Gespeichert wird nie das Passwort selbst, sondern nur ein Hash (PBKDF2 mit SHA-256, 600 000 Durchläufe).
6. **Häuser** (Einstellungen → Verwaltung → Häuser …): beide Häuser anlegen und jedem Bereich sein Haus geben.
   Dann sieht jede Person in der Bereichswahl die Bereiche ihres Hauses; andere nur mit dem Recht „Bereiche anderer
   Häuser wählen“. Haus-Termine im Kalender gelten für das Haus des gewählten Bereichs.

## 5. Sicherung

- **Automatisch jede Nacht** über die Windows-Aufgabenplanung, mit dem Sicherungskonto aus Abschnitt 2
  (PowerShell als Administrator; `/RP *` fragt dessen Passwort ab):

  ```powershell
  schtasks /Create /TN "DokuLine Sicherung" /TR "\"C:\Program Files\DokuLine\DokuLine.exe\" --sicherung" /SC DAILY /ST 02:30 /RU "DokuLine-Sicherung" /RP *
  ```

  Startet die Aufgabe nicht („Anmeldung fehlgeschlagen“): dem Konto das Recht „Anmelden als
  Stapelverarbeitungsauftrag“ geben (`secpol.msc` → Lokale Richtlinien → Zuweisen von Benutzerrechten). *Nicht
  getestet – bei der Betriebsprobe prüfen.*
- Eine Sicherung ist **eine ZIP-Datei** (`dokuline-JJJJ-MM-TT_HHMM.zip`) mit der Datenbank (Sicherungsfunktion von
  SQLite, auch während gearbeitet wird), der Ablage der Dokumente und den Bildern. Danach wird sie **geprüft**
  (Datenbank vollständig lesbar, bekannter Stand). Behalten werden **30 tägliche und 12 monatliche** Stände.
- Aus den Sitzungen wird **nicht** gesichert (die Benutzer dürfen nicht in den Sicherungsordner schreiben). Für den
  Verwalter steht unten in der Statusleiste eine **Warnung**, wenn die letzte Sicherung älter als 2 Tage ist oder fehlt.
  Von Hand sichern: Aufgabenplanung → „DokuLine Sicherung“ → Ausführen.
- **Einstellungen → System → Sicherung …** zeigt die Stände und prüft sie (wenn das eigene Konto den Ordner lesen darf).
- Das Ergebnis jeder Sicherung steht in `update.log` **im Sicherungsordner**.

**Zurücksichern** nur, wenn **alle abgemeldet** sind (auch getrennte Sitzungen abmelden). PowerShell als Administrator,
Dateiname anpassen:

```powershell
& "C:\Program Files\DokuLine\DokuLine.exe" --ruecksichern "E:\DokuLine-Sicherungen\dokuline-2026-11-09_0230.zip"
```

DokuLine prüft die Sicherung, legt den jetzigen Stand als Kopie neben die Datenbank (`dokumentationssystem-vor-ruecksicherung.db`,
die Ordner der Dokumente und Bilder als `…-vor-ruecksicherung-<Zeit>`) und spielt dann den gewählten Stand zurück. Das
Ergebnis erscheint in einem Fenster und in `update.log` im Sicherungsordner. Ist DokuLine noch irgendwo offen, bricht
es ab.

## 6. Update einspielen

Im Betrieb aktualisiert sich DokuLine **nicht selbst**.

1. **Vorbereiten** in einer PowerShell als Administrator – entweder herunterladen (nur von den Releases dieses
   Programms):

   ```powershell
   & "C:\Program Files\DokuLine\DokuLine.exe" --update-herunterladen
   ```

   oder vom USB-Stick (`DokuLine.exe` und daneben `DokuLine.exe.sha256`, Pfad anpassen):

   ```powershell
   & "C:\Program Files\DokuLine\DokuLine.exe" --update-vorbereiten "F:\DokuLine.exe"
   ```

   Die Prüfsumme wird geprüft; passt sie nicht, wird nichts vorbereitet. Das Ergebnis erscheint in einem Fenster.
   Abgelegt wird im Programmordner (`C:\Program Files\DokuLine\update\`), den nur Administratoren ändern dürfen –
   die nächtliche Aufgabe läuft als SYSTEM und darf nichts starten, was ein normaler Benutzer ablegen kann. In DokuLine
   zeigt **Einstellungen → System → Update vorbereiten …**, ob ein Update vorbereitet ist.
   Die Prüfsumme sichert, dass die Datei **vollständig und unbeschädigt** ist – nicht, von wem sie stammt (die Datei
   ist nicht signiert). Darum nur aus den Releases dieses Programms oder von einem selbst bespielten Stick.
2. Eingespielt wird nachts durch eine zweite Aufgabe – nur, wenn niemand DokuLine offen hat:

   ```powershell
   schtasks /Create /TN "DokuLine Update" /TR "\"C:\Program Files\DokuLine\DokuLine.exe\" --update-einspielen" /SC DAILY /ST 03:00 /RU SYSTEM
   ```

   Das Ergebnis steht in `C:\Program Files\DokuLine\update\update.log`.
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
- **Sperre** nach Leerlauf und F12. Offene Rückfragen und Dateiauswahlen werden dabei ausgeblendet und nach der
  Anmeldung wieder gezeigt.
- **Dokumente** in der Akte: nur Dokumente und Bilder (z. B. PDF, Word, Excel, JPG) – keine Programme, Skripte,
  Verknüpfungen oder Office-Dateien mit Makros (sie liefen beim Öffnen im Konto der Kollegin, die sie öffnet).
- Die **Anzeige für den Fernseher** ist im Betrieb aus.

**Was bleibt (Restrisiko, für die Datenschutz-Folgenabschätzung):**

- Die Gruppe DokuLine-Benutzer **muss** die Datenbankdatei lesen und schreiben können – sonst ginge SQLite nicht.
  Wer sich am Server anmeldet und zur Gruppe gehört, könnte die Datei also auch an DokuLine vorbei mit einem anderen
  Programm öffnen (alle Akten lesen, Einträge ändern, das Zugriffsprotokoll umgehen). Gegenmaßnahmen außerhalb von
  DokuLine: Auf dem Terminalserver nur freigegebene Programme zulassen (**AppLocker** bzw. „Windows Defender
  Application Control“), keine eigenen Programme oder Skripte der Benutzer; Explorer-Zugriff auf `D:\DokuLine\Daten`
  per Gruppenrichtlinie ausblenden. Ganz schließt das erst ein eigener Serverdienst mit eigenem Konto (größerer Umbau,
  später möglich).
- Die Hashes der Passwörter liegen in derselben Datei; darum die hohe Zahl an Durchläufen und mindestens 10 Zeichen.
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
| Warnung „letzte Sicherung …“ | Aufgabenplanung prüfen (`update.log` im Sicherungsordner), Aufgabe von Hand ausführen |
| Datenbank beschädigt | DokuLine überall schließen, Sicherung zurückspielen (Abschnitt 5) |
| Nach einem Update geht etwas nicht | Rückweg (Abschnitt 6) und Rückmeldung über die Chats |

## 10. Freigaben vor dem ersten echten Datensatz

- [ ] **Einrichtung / Träger**: schriftliche Entscheidung für den Einsatz.
- [ ] **Datenschutzbeauftragter**: Datenschutz-Folgenabschätzung (Gesundheitsdaten, Art. 9 und Art. 35 DSGVO),
      technische und organisatorische Maßnahmen (Abschnitte 2, 5, 7, 8), Verzeichnis der Verarbeitungstätigkeiten,
      Löschfristen.
- [ ] **Mitarbeitervertretung**: Zugriffsprotokoll und Anmeldung sind technische Einrichtungen, die Verhalten
      erfassen können – Zustimmung bzw. Dienstvereinbarung. Ebenso die **Zeitauswertung** (Bereich „Auswertung“):
      Die erfassten Zeiten hängen an Kürzeln; DokuLine wertet nur je Bewohner aus, nie je Mitarbeiter. Ebenso die
      **Prüfseite „Qualität“**: Sie prüft die Unterlagen der Bewohner; Prüfvermerke und Hinweise tragen das Kürzel
      des QMB, eine Auswertung nach Mitarbeiter gibt es nicht.
- [ ] **Aufbewahrungsfrist des Archivs** (Bauplan 46): mit Träger und Datenschutz klären und in den Einstellungen
      („Archiv: Aufbewahrungsfrist“) eintragen. DokuLine hat bewusst keine Vorgabe; ohne Frist wird nichts gelöscht.
      Gelöscht wird nur von zwei Personen mit dem Recht „Archiv löschen“; Sicherungen laufen nach ihrer eigenen
      Aufbewahrung aus.
- [ ] **Medizinprodukt ja/nein**: prüfen lassen, ob der Medikamentenplan (Stand, Stufenpläne, Hinweise) und die
      Vitalwerte (Hinweise auf Bereiche) als Medizinprodukt gelten.
- [ ] **Betriebsprobe** auf einem Test-Rechner mit ausgedachten Daten: Installation, Rechte, Anmeldung, Sperre,
      Sicherung, Rücksicherung, Update und Rückweg einmal komplett durchgespielt.
