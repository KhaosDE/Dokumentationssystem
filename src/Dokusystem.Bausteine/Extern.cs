using System.Diagnostics;

namespace Dokusystem;

// Übergabe an Programme auf dem Rechner (CLAUDE.md, Regel 6, Entscheidung 08.11.2026): eine Adresse
// im Browser öffnen. Das Programm baut dabei selbst keine Verbindung auf. Adressen nur aus
// Kern/ExterneAdressen.cs. Zum Testen ersetzbar.
public static class Extern
{
    public static Action<string> Oeffnen { get; set; } =
        adresse => Process.Start(new ProcessStartInfo(adresse) { UseShellExecute = true })?.Dispose();
}
