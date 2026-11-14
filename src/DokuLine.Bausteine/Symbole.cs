namespace DokuLine;

// Zeichen aus „Segoe Fluent Icons“ / „Segoe MDL2 Assets“ (Stil.Symbolschrift) für die Seitenleiste.
// Ein Symbol steht nie allein: Eingeklappt zeigt der Hinweis (Tooltip) den Namen.
public static class Symbole
{
    public const string Menue = "\uE700";          // GlobalNavigationButton
    public const string Uebersicht = "\uE80F";     // Home
    public const string Tagesplan = "\uE823";      // Recent (Uhr)
    public const string Bewohner = "\uE716";       // People
    public const string Uebergabe = "\uE8F1";      // Library
    public const string Visiten = "\uE9D9";        // Diagnostic
    public const string Aufgaben = "\uE73E";       // CheckMark
    public const string Nachrichten = "\uE715";    // Mail
    public const string Kalender = "\uE787";       // Calendar
    public const string Therapie = "\uEB51";       // Heart
    public const string Angebote = "\uE8FD";       // BulletedList
    public const string Wochenplaene = "\uE8BF";   // CalendarWeek
    public const string Medikamente = "\uE95E";    // Health
    public const string Einstellungen = "\uE713";  // Setting
    public const string Auswertung = "\uE9D2";     // AreaChart
    public const string Zuruecksetzen = "\uE72C";  // Refresh
    public const string Info = "\uE946";           // Info
    public const string Modul = "\uE74C";          // OEM (Baustein)
    public const string Abmelden = "\uF3B1";       // SignOut

    public static string Fuer(string bereich) => bereich switch
    {
        "Übersicht" => Uebersicht,
        "Tagesplan" => Tagesplan,
        "Bewohner" => Bewohner,
        "Übergabe" => Uebergabe,
        "Visiten" => Visiten,
        "Aufgaben" => Aufgaben,
        "Nachrichten" => Nachrichten,
        "Kalender" => Kalender,
        "Therapie" or "Laufende Therapien" => Therapie,
        "Angebote" => Angebote,
        "Wochenpläne" => Wochenplaene,
        "Medikamente" => Medikamente,
        "Einstellungen" => Einstellungen,
        "Auswertung" => Auswertung,
        _ => Modul,
    };
}
