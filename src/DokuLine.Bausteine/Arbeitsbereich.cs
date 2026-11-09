namespace DokuLine;

// Der in der Übersicht gewählte Bereich. Er gilt für alle Bereiche des Programms bis zum Abmelden; danach und beim
// Start der Bereich des Rechners. Fachregel: IDEEN.md „Wohnbereich einmal in der Übersicht wählen, gilt überall“.
public static class Arbeitsbereich
{
    public static string Gewaehlt { get; set; } = "";
}
