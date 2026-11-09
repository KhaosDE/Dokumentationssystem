using System.Diagnostics;
using DokuLine.Kern;
using DokuLine.Update;
using Microsoft.Data.Sqlite;

namespace DokuLine;

// Aufgaben im Betrieb ohne Fenster (Bauplan 37), für die Windows-Aufgabenplanung: „DokuLine.exe --sicherung“ jede Nacht,
// „DokuLine.exe --update-einspielen“ nachts nach „Update vorbereiten …“. Ergebnis steht in update.log.
internal static class Betriebsaufgaben
{
    public const string UpdateOrdner = "update";

    // Im Programmordner, nicht im Datenordner: Die nächtliche Aufgabe läuft als SYSTEM – was sie startet, darf nur ein
    // Administrator ablegen können (den Datenordner dürfen alle DokuLine-Benutzer ändern).
    public static string Vorbereitet => Path.Combine(AppContext.BaseDirectory, UpdateOrdner, Aktualisierung.Datei);
    public const string AlsAdministrator = "Das Update wird im Programmordner abgelegt, den nur Administratoren ändern dürfen. " +
                                           "Bitte DokuLine dafür einmal mit Rechtsklick → „Als Administrator ausführen“ starten.";

    public static int Sichern()
    {
        try
        {
            var (speicher, _) = Speicher.Oeffnen(Einstellungen.Laden());
            using (speicher)
            {
                var s = speicher.SicherungErstellen(Betrieb.Sicherungsordner, DateTime.Now);
                Aktualisierung.Protokoll($"Sicherung erstellt und geprüft: {s.Pfad}");
            }
            return 0;
        }
        catch (Exception e) when (e is SpeicherFehler or IOException or UnauthorizedAccessException or SqliteException)
        {
            Aktualisierung.Protokoll($"Sicherung fehlgeschlagen: {e.Message}");
            return 1;
        }
    }

    // Prüft die Datei mit der Prüfsumme (Inhalt der .sha256-Datei) und legt beide in den Update-Ordner.
    public static void Vorbereiten(string exe, string pruefsummeText)
    {
        string erwartet = Aktualisierung.PruefsummeLesen(pruefsummeText);
        if (Aktualisierung.Sha256(exe) != erwartet) throw new UpdateFehler("Die Prüfsumme stimmt nicht – die Datei ist beschädigt oder nicht die richtige.");
        Directory.CreateDirectory(Path.GetDirectoryName(Vorbereitet)!);
        File.Copy(exe, Vorbereitet, overwrite: true);
        File.WriteAllText(Vorbereitet + ".sha256", $"{erwartet}  {Aktualisierung.Datei}\n");
        Aktualisierung.Protokoll($"Update vorbereitet (Prüfsumme {erwartet[..12]}…); eingespielt wird mit --update-einspielen.");
    }

    // Herunterladen über dieselben Adressen wie das Selbst-Update der Demo, dann wie oben prüfen.
    public static int Herunterladen()
    {
        var angebot = Aktualisierung.AngebotAusRelease(Aktualisierung.NeuesteRelease(Aktualisierung.HoleJson), Programmversion.Build)
                      ?? throw new UpdateFehler("Es gibt keine neuere Version.");
        string ordner = Path.Combine(Path.GetTempPath(), $"dokuline-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(ordner);
        try
        {
            string exe = Path.Combine(ordner, Aktualisierung.Datei), summe = exe + ".sha256";
            Aktualisierung.HoleDatei(angebot.PruefsummeUrl, summe);
            Aktualisierung.HoleDatei(angebot.Url, exe);
            Vorbereiten(exe, File.ReadAllText(summe));
            return angebot.Build;
        }
        finally
        {
            try { Directory.Delete(ordner, recursive: true); }
            catch (IOException) { }
        }
    }

    // Nur, wenn niemand DokuLine offen hat; die bisherige Datei bleibt als „.alt“ (Rückweg: zurückbenennen).
    public static int UpdateEinspielen()
    {
        string neu = Vorbereitet;
        if (!File.Exists(neu)) { Aktualisierung.Protokoll("Kein Update vorbereitet."); return 0; }
        string exe = Environment.ProcessPath ?? throw new InvalidOperationException("Programmpfad unbekannt.");
        try
        {
            string erwartet = Aktualisierung.PruefsummeLesen(File.ReadAllText(neu + ".sha256"));
            if (Aktualisierung.Sha256(neu) != erwartet) throw new UpdateFehler("Prüfsumme des vorbereiteten Updates stimmt nicht.");
            int offen = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)).Count(p => p.Id != Environment.ProcessId);
            if (offen > 0)
            {
                Aktualisierung.Protokoll($"Update nicht eingespielt: DokuLine ist noch {offen}-mal geöffnet. Nächster Versuch beim nächsten Lauf.");
                return 2;
            }
            string kopie = exe + ".neu";
            File.Copy(neu, kopie, overwrite: true);
            Aktualisierung.Austauschen(exe, kopie);
            File.Delete(neu);
            File.Delete(neu + ".sha256");
            Aktualisierung.Protokoll("Update eingespielt; die bisherige Fassung liegt als „.alt“ daneben.");
            return 0;
        }
        catch (Exception e) when (e is UpdateFehler or IOException or UnauthorizedAccessException)
        {
            Aktualisierung.Protokoll($"Update fehlgeschlagen: {e.Message}");
            return 1;
        }
    }
}
