using Dokusystem.Kern;

namespace Dokusystem;

// Startet alles einmal in einem leeren Datenordner, ohne Fenster zu zeigen. Für die Prüfung der
// fertigen .exe in GitHub Actions: Fehlt etwas in der Datei (Demo-Daten, Bausteine), endet der
// Selbsttest mit Exit-Code 1 und schreibt den Fehler nach selbsttest.log im aktuellen Ordner.
public static class Selbsttest
{
    public static int Ausfuehren()
    {
        string? vorher = Environment.GetEnvironmentVariable(Pfade.Umlenkung);
        string ordner = Directory.CreateTempSubdirectory("dokusystem-selbsttest-").FullName;
        Environment.SetEnvironmentVariable(Pfade.Umlenkung, ordner);
        try
        {
            using (var speicher = new Speicher())
            {
                Modulverwaltung.Anmelden(speicher);
                if (!speicher.DemoBeimErstenStart() || speicher.AlleBewohner().Count == 0)
                    throw new InvalidOperationException("Demo-Daten fehlen.");
                using var fenster = new Hauptfenster(speicher);
                fenster.Bewohner.AkteZeigen(speicher.AlleBewohner()[0].Id!.Value);
                using var formular = fenster.Bewohner.FormularErstellen(speicher.AlleBewohner()[0]);
                using var bild = new Bitmap(fenster.Width, fenster.Height);
                fenster.DrawToBitmap(bild, new Rectangle(Point.Empty, fenster.Size));
            }
            return 0;
        }
        catch (Exception e)
        {
            try { File.WriteAllText("selbsttest.log", e.ToString()); } catch (IOException) { }
            return 1;
        }
        finally
        {
            Environment.SetEnvironmentVariable(Pfade.Umlenkung, vorher);
            try { Directory.Delete(ordner, recursive: true); } catch (IOException) { }
        }
    }
}
