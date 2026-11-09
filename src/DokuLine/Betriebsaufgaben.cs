using System.Diagnostics;
using DokuLine.Kern;
using DokuLine.Update;
using Microsoft.Data.Sqlite;

namespace DokuLine;

// Aufgaben im Betrieb ohne Hauptfenster (Bauplan 37): „--sicherung“ jede Nacht (Aufgabenplanung, Dienstkonto), „--update-einspielen“
// nachts (Aufgabenplanung, SYSTEM), dazu für den Verwalter in einer PowerShell mit Administratorrechten
// „--update-vorbereiten <datei>“, „--update-herunterladen“ und „--ruecksichern <datei>“ – ohne die Datenbank mit erhöhten
// Rechten anzuzeigen. Ergebnis in update.log, nie im Datenordner (den Benutzer ändern dürfen); die Befehle des Verwalters
// melden es zusätzlich in einem Fenster (DokuLine ist ein Fensterprogramm, in die PowerShell schreibt es nicht).
internal static class Betriebsaufgaben
{
    public const string UpdateOrdner = "update";

    // Im Programmordner, nicht im Datenordner: Die nächtliche Aufgabe läuft als SYSTEM – was sie startet, darf nur ein
    // Administrator ablegen können (den Datenordner dürfen alle DokuLine-Benutzer ändern).
    public static string Vorbereitet => Path.Combine(AppContext.BaseDirectory, UpdateOrdner, Aktualisierung.Datei);
    public const string AlsAdministrator = "Das Update wird im Programmordner abgelegt, den nur Administratoren ändern dürfen: PowerShell als " +
                                           "Administrator, „DokuLine.exe --update-herunterladen“ oder „DokuLine.exe --update-vorbereiten <Datei>“ (docs/betrieb.md).";

    public static int Ausfuehren(string[] args)
    {
        string? Wert(string schalter) => Array.IndexOf(args, schalter) is int i && i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        if (args.Contains("--sicherung")) return Sichern();
        Aktualisierung.LogOrdner = () => Path.Combine(AppContext.BaseDirectory, UpdateOrdner);
        if (args.Contains("--update-einspielen")) return UpdateEinspielen();
        try
        {
            if (Wert("--update-vorbereiten") is string datei) { Vorbereiten(datei, File.ReadAllText(datei + ".sha256")); return Melden("Update geprüft und vorbereitet."); }
            if (args.Contains("--update-herunterladen")) return Melden($"Version (Build {Herunterladen()}) geprüft und vorbereitet.");
            if (Wert("--ruecksichern") is string stand) return Ruecksichern(stand);
        }
        catch (Exception e) when (e is UpdateFehler or IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException or Eingabefehler or SpeicherFehler
                                       or SqliteException)
        {
            string text = e is Eingabefehler f ? string.Join(" ", f.Fehler) : e.Message;
            Aktualisierung.Protokoll(text);
            return Fehler(args, text);
        }
        return Fehler(args, "Unvollständiger Befehl – nach „--update-vorbereiten“ und „--ruecksichern“ gehört der Pfad der Datei.");
    }

    public static bool IstAufgabe(string[] args) =>
        args.Any(a => a is "--sicherung" or "--update-einspielen" or "--update-vorbereiten" or "--update-herunterladen" or "--ruecksichern");

    // Die nächtlichen Aufgaben laufen ohne angemeldeten Benutzer: ein Fenster könnte niemand schließen.
    private static bool Nachts(string[] args) => args.Contains("--sicherung") || args.Contains("--update-einspielen");

    public static int Fehler(string[] args, string text)
    {
        if (!Nachts(args)) MessageBox.Show(text, Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        return 1;
    }

    private static int Melden(string text)
    {
        Aktualisierung.Protokoll(text);
        MessageBox.Show(text, Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return 0;
    }

    private static int AndereOffen(string exe) =>
        Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)).Count(p => p.Id != Environment.ProcessId);

    // Nur, wenn niemand DokuLine offen hat: vorher eine Kopie, dann zurück auf den Stand der Sicherung.
    private static int Ruecksichern(string stand)
    {
        Aktualisierung.LogOrdner = () => Betrieb.Sicherungsordner;
        if (AndereOffen(Environment.ProcessPath ?? "DokuLine") > 0) throw new UpdateFehler("DokuLine ist noch geöffnet – bitte alle abmelden lassen.");
        var (speicher, _) = Speicher.Oeffnen(Einstellungen.Laden());
        using (speicher) return Melden($"Zurückgesichert. Der vorige Stand liegt als „{Path.GetFileName(speicher.Ruecksichern(stand))}“ daneben.");
    }

    public static int Sichern()
    {
        Aktualisierung.LogOrdner = () => Betrieb.Sicherungsordner;
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
            int offen = AndereOffen(exe);
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
