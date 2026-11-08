namespace Dokusystem;

// Zeichen aus „Segoe Fluent Icons“ / „Segoe MDL2 Assets“ (Stil.Symbolschrift) für die Seitenleiste.
// Ein Symbol steht nie allein: Eingeklappt zeigt der Hinweis (Tooltip) den Namen.
public static class Symbole
{
    public const string Menue = "\uE700";          // GlobalNavigationButton
    public const string Uebersicht = "\uE80F";     // Home
    public const string Bewohner = "\uE716";       // People
    public const string Uebergabe = "\uE8F1";      // Library
    public const string Aufgaben = "\uE73E";       // CheckMark
    public const string Nachrichten = "\uE715";    // Mail
    public const string Kalender = "\uE787";       // Calendar
    public const string Therapie = "\uEB51";       // Heart
    public const string Medikamente = "\uE95E";    // Health
    public const string Einstellungen = "\uE713";  // Setting
    public const string Zuruecksetzen = "\uE72C";  // Refresh
    public const string Info = "\uE946";           // Info
    public const string Modul = "\uE74C";          // OEM (Baustein)

    public static string Fuer(string bereich) => bereich switch
    {
        "Übersicht" => Uebersicht,
        "Bewohner" => Bewohner,
        "Übergabe" => Uebergabe,
        "Aufgaben" => Aufgaben,
        "Nachrichten" => Nachrichten,
        "Kalender" => Kalender,
        "Therapie" => Therapie,
        "Medikamente" => Medikamente,
        "Einstellungen" => Einstellungen,
        _ => Modul,
    };
}
