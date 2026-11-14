using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Mitarbeiter, Anmeldung und Rechte. Anmelden mit dem Kürzel, ohne Passwort
// (Demo). Rechte hängen an Positionen; wer mehrere Positionen hat, hat die Rechte aller zusammen.
// Ausgeschiedene können sich nicht anmelden, bleiben aber an ihren Einträgen sichtbar.

public sealed record Mitarbeiter(string Kuerzel, string Vorname, string Nachname, bool Fachkraft, bool Aktiv,
    IReadOnlyList<string> Bereiche, IReadOnlyList<string> Positionen)
{
    public string Name => $"{Vorname} {Nachname}";
    public string Anzeige => $"{Name} ({Kuerzel})";
    public string Qualifikation => RechteRegeln.Qualifikation(Fachkraft);   // „Assistenzkraft“ oder „Fachkraft“
    public string? Farbe { get; init; }   // #RRGGBB; null = Vorgabe aus dem Kürzel
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
    public const string BedarfGeben = "bedarf_geben";   // „Bedarf gegeben …“ (Assistenzkraft nur mit Absprache)
    public const string TherapienBearbeiten = "therapien_bearbeiten";
    public const string TherapienBereich = "therapien_bereich";   // Angebote des eigenen Wohnbereichs
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
    public const string Aerzte = "aerzte";
    public const string AerztlicheAnsehen = "aerztliche_ansehen";
    public const string AerztlicheBearbeiten = "aerztliche_bearbeiten";
    public const string BereicheWohnbereiche = "bereiche_wohnbereiche";   // Übersicht: Knöpfe der Wohnbereiche
    public const string BereichTherapie = "bereich_therapie";             // Übersicht: Knopf „Therapie“
    public const string BereicheAlle = "bereiche_alle";                   // Übersicht: Knopf „Alle“
    public const string AnzeigeVerwalten = "anzeige_verwalten";           // Therapie-Anzeige für den Fernseher
    public const string DokumenteAnsehen = "dokumente_ansehen";           // Akte, Reiter „Dokumente“
    public const string DokumenteHinzufuegen = "dokumente_hinzufuegen";
    public const string DokumenteLoeschen = "dokumente_loeschen";
    public const string VitalwerteErfassen = "vitalwerte_erfassen";
    public const string VitalGrenzen = "vital_grenzen";   // angeordnete Bereiche, Messaufträge, Richtwerte
    public const string Zugriffsprotokoll = "zugriffsprotokoll";   // Betrieb: wer hat welche Akte geöffnet, gedruckt, geändert
    public const string BetriebVerwalten = "betrieb_verwalten";   // Sicherung, Rücksicherung, Update, Häuser
    public const string AndereHaeuser = "andere_haeuser";         // Bereiche anderer Häuser wählen
    public const string MassnahmenBestaetigen = "massnahmen_bestaetigen";
    public const string MassnahmenAnlegen = "massnahmen_anlegen";   // dazu die Bezugspflege des Bewohners
    public const string Zeitauswertung = "zeitauswertung";          // nur je Bewohner, nie je Mitarbeiter
    public const string MedikamenteAusgeben = "medikamente_ausgeben";   // Ausgabe abhaken (Bauplan 42)
    public const string Qualitaet = "qualitaet";                       // Prüfseite, Prüfvermerke, Hinweise (Bauplan 44)
    public const string ArchivEinsehen = "archiv_einsehen";            // ehemalige Bewohner, nur lesen (Bauplan 46)
    public const string ArchivLoeschen = "archiv_loeschen";            // nach Ablauf der Frist, nur zu zweit
    public const string SchluesselwoerterVerwalten = "schluesselwoerter"; // Liste der Schlüsselwörter ändern (Bauplan 48)
    public const string FachwoerterVerwalten = "fachwoerter";           // Fachwörterbuch der Rechtschreibung (Bauplan 49)

    public static readonly (string Schluessel, string Name)[] Alle =
    [
        (BewohnerBearbeiten, "Bewohner anlegen und bearbeiten"),
        (BerichteSchreiben, "Berichte schreiben"),
        (Visiten, "Visiten eintragen"),
        (AerztlicheAnsehen, "Ärztliche Informationen ansehen"),
        (AerztlicheBearbeiten, "Ärztliche Informationen bearbeiten"),
        (MedikationBearbeiten, "Medikation bearbeiten"),
        (MedikamentAnlegen, "Medikament anlegen"),
        (MedikamenteVerwalten, "Medikamentendatenbank verwalten (prüfen, ändern, sperren)"),
        (BedarfGeben, "Bedarfsmedikation geben"),
        (TherapienBearbeiten, "Therapien bearbeiten"),
        (TherapienBereich, "Therapien des eigenen Bereichs bearbeiten"),
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
        (Aerzte, "Personenverzeichnis verwalten"),
        (BereicheWohnbereiche, "Wohnbereiche wählen (Übersicht)"),
        (BereichTherapie, "Bereich Therapie wählen (Übersicht)"),
        (AnzeigeVerwalten, "Anzeige für den Fernseher verwalten"),
        (DokumenteAnsehen, "Dokumente ansehen"),
        (DokumenteHinzufuegen, "Dokumente hinzufügen und in den Papierkorb legen"),
        (DokumenteLoeschen, "Dokumente endgültig löschen"),
        (VitalwerteErfassen, "Vitalwerte erfassen"),
        (VitalGrenzen, "Grenzen und Messaufträge festlegen"),
        (MassnahmenBestaetigen, "Maßnahmen bestätigen"),
        (MassnahmenAnlegen, "Maßnahmen anlegen und ändern"),
        (Zeitauswertung, "Zeitauswertung ansehen"),
        (MedikamenteAusgeben, "Medikamente ausgeben (abhaken)"),
        (Qualitaet, "Qualität prüfen (Prüfseite, Prüfvermerke, Hinweise)"),
        (ArchivEinsehen, "Archiv einsehen (ehemalige Bewohner, nur lesen)"),
        (ArchivLoeschen, "Archiv löschen (nach Ablauf der Frist, zu zweit)"),
        (SchluesselwoerterVerwalten, "Schlüsselwörter verwalten"),
        (FachwoerterVerwalten, "Fachwörter verwalten (Rechtschreibung)"),
        (Zugriffsprotokoll, "Zugriffsprotokoll ansehen"),
        (BetriebVerwalten, "Betrieb verwalten (Sicherung, Update, Häuser)"),
        (AndereHaeuser, "Bereiche anderer Häuser wählen"),
        (BereicheAlle, "Alle Bereiche zusammen ansehen (Übersicht)"),
        (DemoZuruecksetzen, "Demo-Daten zurücksetzen"),
    ];

    public static string Name(string schluessel) => Alle.FirstOrDefault(f => f.Schluessel == schluessel).Name ?? schluessel;
}

public static partial class RechteRegeln
{
    public const string FehltText = "Dafür fehlt die Berechtigung.";
    public const string NichtAngemeldetText = "Bitte zuerst anmelden.";
    public const string NurFachkraftText = "Nur für Fachkräfte.";

    // Fehlt das Recht, wird das Element ausgeblendet; ohne Anmeldung oder „geht gerade nicht“ bleibt es grau.
    // Fachregel: IDEEN.md „Rechte: Fehlendes ausblenden statt ausgrauen“.
    public static bool FehltRecht(string? grund) => grund is FehltText or NurFachkraftText;

    // Die zwei Stufen (Tobi, 08.11.2026): die kleinste ist die Assistenzkraft, darüber die Fachkraft.
    public const string Assistenzkraft = "Assistenzkraft", Fachkraft = "Fachkraft";
    public static string Qualifikation(bool fachkraft) => fachkraft ? Fachkraft : Assistenzkraft;

    // Der Administrator hat immer alle Rechte, in allen Bereichen, auch ohne „Fachkraft“.
    // Es bleibt immer mindestens ein aktiver Administrator, damit sich niemand aussperren kann.
    public const string Administrator = "Administrator";
    public const string AdminImmerText = "Der Administrator hat immer alle Rechte.";
    public const string LetzterAdminText = "Mindestens ein aktiver Administrator muss bleiben – sonst kann niemand mehr Rechte und Mitarbeiter verwalten.";
    public const string AdminPositionText = "Die Position „Administrator“ bleibt immer bestehen.";

    public static bool IstAdministrator(Mitarbeiter? m) => m is not null && m.Aktiv && m.Positionen.Contains(Administrator);

    // Vorbelegung (Tobi, 08.11.2026; in der Tabelle änderbar): Geschäftsführung, Hausleitung und
    // Verwaltung dürfen alles; Mitarbeiterverwaltung im Wohnbereich nur die Bereichsleitung (eigener
    // Bereich); die Medikamentendatenbank verwaltet der Psychologische Fachdienst; „Therapien
    // bearbeiten“ nur die Position Therapie (und die drei Leitungen).
    public static readonly string[] Positionen =
        [Administrator, "Geschäftsführung", "Hausleitung", "Verwaltung", "Bereichsleitung", "Mitarbeiter Wohnbereich",
         "Psychologischer Fachdienst", "Pädagogischer Fachdienst", "Therapie", QualitaetRegeln.Position];

    // Positionen, die nach der ersten Vorbelegung dazukamen: in bestehenden Datenbanken einmal anlegen und vorbelegen.
    public static readonly string[] PositionenNachtraeglich = [QualitaetRegeln.Position];

    // QMB (Bauplan 44): liest überall, ändert nichts außer Prüfvermerken und Hinweisen (und Nachrichten, die jeder schreibt).
    private static readonly string[] QmbVorgabe = [Funktionen.Qualitaet, Funktionen.BereicheWohnbereiche, Funktionen.BereicheAlle, Funktionen.DokumenteAnsehen,
        Funktionen.ArchivEinsehen];

    // Assistenzkräfte machen keine Visiten.
    public static readonly string[] NurFachkraftVorgabe = [Funktionen.MedikationBearbeiten, Funktionen.MedikamentAnlegen, Funktionen.AerztlicheBearbeiten, Funktionen.Visiten,
        Funktionen.VitalGrenzen, Funktionen.MassnahmenAnlegen, Funktionen.MedikamenteAusgeben];

    // „Nur Fachkräfte“, das nach der ersten Vorbelegung dazukam: in bestehenden Datenbanken einmal setzen.
    public static readonly string[] NurFachkraftNachtraeglich = [Funktionen.Visiten, Funktionen.MassnahmenAnlegen, Funktionen.MedikamenteAusgeben];

    private static readonly Dictionary<string, string[]> Vorgabe = new()
    {
        ["Bereichsleitung"] =
        [
            Funktionen.BewohnerBearbeiten, Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.MedikationBearbeiten,
            Funktionen.MedikamentAnlegen, Funktionen.TeilnahmeBestaetigen, Funktionen.KalenderHaus, Funktionen.Fahrzeuge,
            Funktionen.BedarfGeben, Funktionen.AufgabenVorlagen, Funktionen.Heb, Funktionen.MitarbeiterBereich, Funktionen.EinstellungenBereich,
            Funktionen.DemoZuruecksetzen, Funktionen.TherapienBereich, Funktionen.Aerzte,
            Funktionen.BereicheWohnbereiche, Funktionen.AnzeigeVerwalten,
            Funktionen.DokumenteAnsehen, Funktionen.DokumenteHinzufuegen, Funktionen.DokumenteLoeschen,
        ],
        ["Mitarbeiter Wohnbereich"] =
        [
            Funktionen.BewohnerBearbeiten, Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.MedikationBearbeiten,
            Funktionen.MedikamentAnlegen, Funktionen.TeilnahmeBestaetigen, Funktionen.Fahrzeuge, Funktionen.Heb,
            Funktionen.BedarfGeben, Funktionen.BereicheWohnbereiche, Funktionen.DokumenteAnsehen, Funktionen.DokumenteHinzufuegen,
        ],
        ["Psychologischer Fachdienst"] =
        [
            Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.MedikationBearbeiten, Funktionen.MedikamentAnlegen,
            Funktionen.MedikamenteVerwalten, Funktionen.TeilnahmeBestaetigen, Funktionen.Heb, Funktionen.Aerzte,
            Funktionen.BereicheWohnbereiche, Funktionen.DokumenteAnsehen, Funktionen.DokumenteHinzufuegen,
        ],
        ["Pädagogischer Fachdienst"] =
        [
            Funktionen.BewohnerBearbeiten, Funktionen.BerichteSchreiben, Funktionen.Visiten, Funktionen.TeilnahmeBestaetigen, Funktionen.Heb,
            Funktionen.Aerzte, Funktionen.BereicheWohnbereiche, Funktionen.DokumenteAnsehen, Funktionen.DokumenteHinzufuegen,
        ],
        ["Therapie"] =
        [
            Funktionen.BerichteSchreiben, Funktionen.TherapienBearbeiten, Funktionen.TeilnahmeBestaetigen,
            Funktionen.Fahrzeuge, Funktionen.BereicheWohnbereiche, Funktionen.BereichTherapie,
        ],
    };

    // Funktionen, die nach der ersten Vorbelegung dazukamen: werden in bestehenden
    // Datenbanken einmal nachträglich vorbelegt.
    public static readonly string[] Nachtraeglich = [Funktionen.TherapienBereich, Funktionen.Aerzte, Funktionen.AerztlicheAnsehen, Funktionen.AerztlicheBearbeiten,
        Funktionen.BedarfGeben, Funktionen.BereicheWohnbereiche, Funktionen.BereichTherapie, Funktionen.BereicheAlle,
        Funktionen.AnzeigeVerwalten, Funktionen.DokumenteAnsehen, Funktionen.DokumenteHinzufuegen, Funktionen.DokumenteLoeschen,
        Funktionen.VitalwerteErfassen, Funktionen.VitalGrenzen, Funktionen.Zugriffsprotokoll, Funktionen.BetriebVerwalten, Funktionen.AndereHaeuser,
        Funktionen.MassnahmenBestaetigen, Funktionen.MassnahmenAnlegen, Funktionen.Zeitauswertung, Funktionen.MedikamenteAusgeben, Funktionen.Qualitaet,
        Funktionen.ArchivEinsehen, Funktionen.ArchivLoeschen, Funktionen.SchluesselwoerterVerwalten, Funktionen.FachwoerterVerwalten];

    // Ärztliche Informationen ansehen alle außer Verwaltung; bearbeiten ebenso, aber nur
    // Fachkräfte (Häkchen „nur Fachkräfte“) – so sind die Leitungen dabei, wenn sie Fachkraft sind.
    // Vitalwerte erfassen alle; Grenzen und Messaufträge alle mit Fachaufgaben (dazu „nur Fachkräfte“), nicht Therapie.
    public static bool Vorbelegt(string position, string funktion) =>
        position == QualitaetRegeln.Position ? QmbVorgabe.Contains(funktion) :
        funktion is Funktionen.AerztlicheAnsehen or Funktionen.AerztlicheBearbeiten ? position != "Verwaltung" :
        funktion is Funktionen.VitalwerteErfassen or Funktionen.MassnahmenBestaetigen ? true :
        funktion == Funktionen.MassnahmenAnlegen ? position is not ("Therapie" or "Verwaltung") :
        funktion == Funktionen.MedikamenteAusgeben ? Vorbelegt(position, Funktionen.MedikationBearbeiten) :
        funktion is Funktionen.Zeitauswertung or Funktionen.ArchivEinsehen ? position is Administrator or "Geschäftsführung" or "Hausleitung" or "Verwaltung" or "Bereichsleitung" :
        funktion == Funktionen.ArchivLoeschen ? position is Administrator or "Geschäftsführung" or "Hausleitung" or "Verwaltung" :
        funktion is Funktionen.SchluesselwoerterVerwalten or Funktionen.FachwoerterVerwalten ? position is Administrator or "Geschäftsführung" or "Hausleitung" or "Bereichsleitung" :
        funktion == Funktionen.VitalGrenzen ? position is not ("Therapie" or "Verwaltung") :
        funktion == Funktionen.Zugriffsprotokoll ? position is Administrator or "Geschäftsführung" or "Hausleitung" :
        position is Administrator or "Geschäftsführung" or "Hausleitung" or "Verwaltung" ||
        (Vorgabe.TryGetValue(position, out var f) && f.Contains(funktion));

    // Warum jemand etwas nicht darf (null = darf). rechte: Funktion → Positionen mit Häkchen.
    public static string? Grund(Mitarbeiter? m, string funktion, IReadOnlyDictionary<string, HashSet<string>> rechte, IReadOnlySet<string> nurFachkraft)
    {
        if (m is null || !m.Aktiv) return NichtAngemeldetText;
        if (IstAdministrator(m)) return null;
        bool position = rechte.TryGetValue(funktion, out var erlaubt) && m.Positionen.Any(erlaubt.Contains);
        if (!position) return FehltText;
        if (nurFachkraft.Contains(funktion) && !m.Fachkraft) return NurFachkraftText;
        return null;
    }

    // Therapieangebote: Angebote der Therapie ändert, wer „Therapien bearbeiten“ hat;
    // Angebote eines Wohnbereichs ändert, wer „Therapien des eigenen Bereichs bearbeiten“ hat und
    // zu diesem Bereich gehört. Das gilt auch für das Zuteilen von Bewohnern.
    public static string? AngebotGrund(Mitarbeiter? m, string anbieter, IReadOnlyDictionary<string, HashSet<string>> rechte, IReadOnlySet<string> nurFachkraft)
    {
        if (anbieter == TherapieRegeln.AnbieterTherapie) return Grund(m, Funktionen.TherapienBearbeiten, rechte, nurFachkraft);
        if (Grund(m, Funktionen.TherapienBereich, rechte, nurFachkraft) is string grund) return grund;
        return IstAdministrator(m) || m!.Bereiche.Contains(anbieter) ? null : $"Nur für {anbieter}.";
    }

    // Für welche Anbieter jemand neue Angebote anlegen darf: „Therapie“ und die eigenen Wohnbereiche
    // (Bereiche, in denen Bewohner wohnen – nicht z. B. „Leitung“).
    public static List<string> Anbieter(Mitarbeiter? m, IReadOnlyCollection<string> wohnbereiche, IReadOnlyDictionary<string, HashSet<string>> rechte, IReadOnlySet<string> nurFachkraft)
    {
        var liste = new List<string>();
        if (Grund(m, Funktionen.TherapienBearbeiten, rechte, nurFachkraft) is null) liste.Add(TherapieRegeln.AnbieterTherapie);
        if (Grund(m, Funktionen.TherapienBereich, rechte, nurFachkraft) is null)
            liste.AddRange(m!.Bereiche.Where(b => b != TherapieRegeln.AnbieterTherapie && wohnbereiche.Contains(b)));
        return liste;
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
