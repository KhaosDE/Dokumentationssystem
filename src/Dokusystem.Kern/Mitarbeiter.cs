using System.Text.RegularExpressions;

namespace Dokusystem.Kern;

// Mitarbeiter, Anmeldung und Rechte (Bauplan Schritt 19). Anmelden mit dem Kürzel, ohne Passwort
// (Demo). Rechte hängen an Positionen; wer mehrere Positionen hat, hat die Rechte aller zusammen.
// Ausgeschiedene können sich nicht anmelden, bleiben aber an ihren Einträgen sichtbar.

public sealed record Mitarbeiter(string Kuerzel, string Vorname, string Nachname, bool Fachkraft, bool Aktiv,
    IReadOnlyList<string> Bereiche, IReadOnlyList<string> Positionen)
{
    public string Name => $"{Vorname} {Nachname}";
    public string Anzeige => $"{Name} ({Kuerzel})";
}

// Ein Eintrag der Listen „Bereiche“ und „Positionen“ (stillgelegt = nicht mehr wählbar).
public sealed record Listeneintrag(string Name, bool Aktiv, int Zugeordnet);

// Die Funktionen der Rechte-Tabelle (Zeilen). Schlüssel stehen in der Datenbank.
public static class Funktionen
{
    public const string BewohnerBearbeiten = "bewohner_bearbeiten";
    public const string BerichteSchreiben = "berichte_schreiben";
    public const string Visiten = "visiten";
    public const string MedikationBearbeiten = "medikation_bearbeiten";
    public const string MedikamentAnlegen = "medikament_anlegen";
    public const string MedikamenteVerwalten = "medikamente_verwalten";
    public const string TherapienBearbeiten = "therapien_bearbeiten";
    public const string TeilnahmeBestaetigen = "teilnahme_bestaetigen";
    public const string KalenderHaus = "kalender_haus";
    public const string Fahrzeuge = "fahrzeuge";
    public const string AufgabenVorlagen = "aufgaben_vorlagen";
    public const string Heb = "heb";
    public const string MitarbeiterBereich = "mitarbeiter_bereich";
    public const string MitarbeiterAlle = "mitarbeiter_alle";
    public const string RechteVerwalten = "rechte";
    public const string Module = "module";
    public const string EinstellungenBereich = "einstellungen_bereich";
    public const string DemoZuruecksetzen = "demo_zuruecksetzen";

    public static readonly (string Schluessel, string Name)[] Alle =
    [
        (BewohnerBearbeiten, "Bewohner anlegen und bearbeiten"),
        (BerichteSchreiben, "Berichte schreiben"),
        (Visiten, "Visiten eintragen"),
        (MedikationBearbeiten, "Medikation bearbeiten"),
        (MedikamentAnlegen, "Medikament anlegen"),
        (MedikamenteVerwalten, "Medikamentendatenbank verwalten (prüfen, ändern, sperren)"),
        (TherapienBearbeiten, "Therapien bearbeiten"),
        (TeilnahmeBestaetigen, "Therapie-Teilnahme bestätigen"),
        (KalenderHaus, "Kalender Haus bearbeiten"),
        (Fahrzeuge, "Fahrzeuge eintragen"),
        (AufgabenVorlagen, "Aufgaben-Vorlagen verwalten"),
        (Heb, "HEB-Bogen erstellen"),
        (MitarbeiterBereich, "Mitarbeiter verwalten (eigener Bereich)"),
        (MitarbeiterAlle, "Mitarbeiter verwalten (alle)"),
        (RechteVerwalten, "Rechte verwalten"),
        (Module, "Module ein- und ausschalten"),
        (EinstellungenBereich, "Einstellungen des Bereichs"),
        (DemoZuruecksetzen, "Demo-Daten zurücksetzen"),
    ];

    public static string Name(string schluessel) => Alle.FirstOrDefault(f => f.Schluessel == schluessel).Name ?? schluessel;
}

public static partial class RechteRegeln
{
    public const string FehltText = "Dafür fehlt die Berechtigung.";
    public const string NichtAngemeldetText = "Bitte zuerst anmelden.";
    public const string NurFachkraftText = "Nur für Fachkräfte.";

    // Vorbelegung (Tobi, 08.11.2026; in der Tabelle änderbar): Geschäftsführung, Hausleitung und
    // Verwaltung dürfen alles; Mitarbeiterverwaltung im Wohnbereich nur die Bereichsleitung (eigener
    // Bereich); die Medikamentendatenbank verwaltet der Psychologische Fachdienst; „Therapien
    // bearbeiten“ nur die Position Therapie (und die drei Leitungen).
    public static readonly string[] Positionen =
        ["Geschäftsführung", "Hausleitung", "Verwaltung", "Bereichsleitung", "Mitarbeiter Wohnbereich",
         "Psychologischer Fachdienst", "Pädagogischer Fachdienst", "Therapie"];

    public static readonly string[] NurFachkraftVorgabe = [Funktionen.MedikationBearbeiten, Funktionen.MedikamentAnlegen];

    private static readonly Dictionary<string, string[]> Vorgabe = new()
    {
        ["Bereichsleitung"] =
        [
            Funktionen.BewohnerBearbeiten, Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.MedikationBearbeiten,
            Funktionen.MedikamentAnlegen, Funktionen.TeilnahmeBestaetigen, Funktionen.KalenderHaus, Funktionen.Fahrzeuge,
            Funktionen.AufgabenVorlagen, Funktionen.Heb, Funktionen.MitarbeiterBereich, Funktionen.EinstellungenBereich,
            Funktionen.DemoZuruecksetzen,
        ],
        ["Mitarbeiter Wohnbereich"] =
        [
            Funktionen.BewohnerBearbeiten, Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.MedikationBearbeiten,
            Funktionen.MedikamentAnlegen, Funktionen.TeilnahmeBestaetigen, Funktionen.Fahrzeuge, Funktionen.Heb,
        ],
        ["Psychologischer Fachdienst"] =
        [
            Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.MedikationBearbeiten, Funktionen.MedikamentAnlegen,
            Funktionen.MedikamenteVerwalten, Funktionen.TeilnahmeBestaetigen, Funktionen.Heb,
        ],
        ["Pädagogischer Fachdienst"] =
        [
            Funktionen.BewohnerBearbeiten, Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.TeilnahmeBestaetigen, Funktionen.Heb,
        ],
        ["Therapie"] = [Funktionen.BerichteSchreiben, Funktionen.TherapienBearbeiten, Funktionen.TeilnahmeBestaetigen, Funktionen.Fahrzeuge],
    };

    public static bool Vorbelegt(string position, string funktion) =>
        position is "Geschäftsführung" or "Hausleitung" or "Verwaltung" ||
        (Vorgabe.TryGetValue(position, out var f) && f.Contains(funktion));

    // Warum jemand etwas nicht darf (null = darf). rechte: Funktion → Positionen mit Häkchen.
    public static string? Grund(Mitarbeiter? m, string funktion, IReadOnlyDictionary<string, HashSet<string>> rechte, IReadOnlySet<string> nurFachkraft)
    {
        if (m is null || !m.Aktiv) return NichtAngemeldetText;
        bool position = rechte.TryGetValue(funktion, out var erlaubt) && m.Positionen.Any(erlaubt.Contains);
        if (!position) return FehltText;
        if (nurFachkraft.Contains(funktion) && !m.Fachkraft) return NurFachkraftText;
        return null;
    }

    // Mitarbeiter verwalten: „alle“ oder „eigener Bereich“ – dann nur Mitarbeiter mit mindestens einem
    // gemeinsamen Bereich (neue: alle gewählten Bereiche müssen eigene sein).
    public static bool DarfVerwalten(Mitarbeiter ich, bool alle, bool eigenerBereich, IReadOnlyList<string> bereicheDesZiels) =>
        alle || (eigenerBereich && bereicheDesZiels.Count > 0 && bereicheDesZiels.Any(ich.Bereiche.Contains));

    [GeneratedRegex(@"^[\p{L}\d]{1,6}$")]
    private static partial Regex KuerzelMuster();

    public static List<string> Pruefen(Mitarbeiter m, IReadOnlyCollection<string> andereKuerzel)
    {
        var fehler = new List<string>();
        if (!KuerzelMuster().IsMatch(m.Kuerzel)) fehler.Add("Kürzel: 1 bis 6 Buchstaben oder Ziffern.");
        else if (andereKuerzel.Contains(m.Kuerzel, StringComparer.OrdinalIgnoreCase)) fehler.Add($"Das Kürzel „{m.Kuerzel}“ ist schon vergeben.");
        if (string.IsNullOrWhiteSpace(m.Vorname)) fehler.Add("Vorname fehlt.");
        if (string.IsNullOrWhiteSpace(m.Nachname)) fehler.Add("Nachname fehlt.");
        if (m.Bereiche.Count == 0) fehler.Add("Bitte mindestens einen Bereich wählen.");
        if (m.Positionen.Count == 0) fehler.Add("Bitte mindestens eine Position wählen.");
        return fehler;
    }
}
