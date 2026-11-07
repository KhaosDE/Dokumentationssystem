using Dokusystem.Kern;
using Dokusystem.Update;

namespace Dokusystem;

// Einstiegspunkt der .exe.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Grundeinstellungen für die Oberfläche (Schriften, Skalierung) – vor dem ersten Fenster.
        ApplicationConfiguration.Initialize();

        if (args.Contains("--selbsttest")) return Selbsttest.Ausfuehren();

        // Zuerst das Selbst-Update: Ist eine neue Version geladen und gestartet, endet dieses Programm.
        if (Aktualisierung.BeimStart(args, Programmversion.Build, () => new Fortschrittsfenster())) return 0;

        // Auffangnetz: Ein unerwarteter Fehler zeigt eine verständliche Meldung statt des
        // technischen Fehlerfensters von Windows.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => MessageBox.Show(
            "Ein unerwarteter Fehler ist aufgetreten:\n\n" + e.Exception.Message +
            "\n\nDie letzte Aktion wurde möglicherweise nicht ausgeführt.",
            "Dokumentationssystem", MessageBoxButtons.OK, MessageBoxIcon.Error);

        Speicher speicher;
        try
        {
            speicher = new Speicher();
            Modulverwaltung.Anmelden(speicher);
            speicher.DemoBeimErstenStart();
        }
        catch (SpeicherFehler e)
        {
            MessageBox.Show(e.Message, "Dokumentationssystem", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        using (speicher)
        using (var fenster = new Hauptfenster(speicher))
            Application.Run(fenster);
        return 0;
    }
}
