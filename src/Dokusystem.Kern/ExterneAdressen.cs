namespace Dokusystem.Kern;

// Die einzigen Internetadressen im Programm außer dem Selbst-Update (CLAUDE.md, Regel 6). Sie werden
// an den Browser übergeben oder in der Nachschlage-Ansicht des Formulars „Medikament“ gezeigt.
// Ein Test prüft, dass sonst nirgends Adressen im Quelltext stehen.
public static class ExterneAdressen
{
    // Öffentliche Arzneimittelsuche des BfArM (AMIce, öffentlicher Teil), verlinkt auf PharmNet.Bund;
    // leitet über die anonyme Anmeldung des BfArM zur Suche weiter (geprüft am 08.11.2026).
    public const string Arzneimittelsuche = "https://auth.bfarm.de/sso-home/amguifree";

    public static readonly string[] Alle = [Arzneimittelsuche];

    // Nur diese Bereiche (und ihre Unteradressen) zeigt die Nachschlage-Ansicht – nur das BfArM
    // (Entscheidung Tobi, 08.11.2026); alles andere öffnet im normalen Browser.
    public static readonly string[] Nachschlagebereiche = ["bfarm.de", "pharmnet-bund.de"];
}
