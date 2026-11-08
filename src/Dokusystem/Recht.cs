using Dokusystem.Kern;

namespace Dokusystem;

// Rechte des Angemeldeten (Bauplan Schritt 19) für die Oberfläche: Knöpfe ohne Berechtigung
// bleiben sichtbar, aber gesperrt (Stil.Sperren). Wer angemeldet ist, steht in den Einstellungen.
public static class Recht
{
    public static string Kuerzel => Einstellungen.Laden().Kuerzel;

    public static string? Grund(Speicher speicher, string funktion) => speicher.Grund(Kuerzel, funktion);

    public static bool Darf(Speicher speicher, string funktion) => Grund(speicher, funktion) is null;

    public static void Anwenden(Speicher speicher, string funktion, params Button[] knoepfe)
    {
        string? grund = Grund(speicher, funktion);
        foreach (var k in knoepfe) Stil.Sperren(k, grund);
    }
}
