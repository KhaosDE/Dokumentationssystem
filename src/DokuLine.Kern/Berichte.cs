using System.Globalization;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Tägliche Dokumentation (Bauplan Schritt 3): Berichte je Bewohner mit Schicht, Kürzel,
// Lebensbereichen und Text. Nichts wird gelöscht oder überschrieben – Korrekturen sind Nachträge
// mit eigenem Zeitstempel, der ursprüngliche Text bleibt sichtbar.

public enum Schicht { Frueh, Spaet, Nacht }

public sealed record Nachtrag(long Id, long BerichtId, DateTime Erfasst, string Verfasser, string Text);

public sealed record Bericht
{
    public long? Id { get; init; }
    public required long BewohnerId { get; init; }
    public required DateTime Zeitpunkt { get; init; }   // worum es geht (Datum und Uhrzeit)
    public required Schicht Schicht { get; init; }
    public required string Verfasser { get; init; }      // Kürzel
    public IReadOnlyList<int> Bereiche { get; init; } = [];   // Lebensbereiche 1–5; leer = allgemein
    public required string Text { get; init; }
    public bool Wichtig { get; init; }                   // bleibt in der Akte dauerhaft hervorgehoben
    public DateTime Erfasst { get; init; }               // wann eingetragen
    public bool Uebergabe { get; init; }                 // hat einen Übergabe-Eintrag (nur lesend)
    public IReadOnlyList<Nachtrag> Nachtraege { get; init; } = [];

    public bool Allgemein => Bereiche.Count == 0;
}

// Texteingaben aus dem Formular, noch ungeprüft.
public sealed class BerichtEingabe
{
    public long BewohnerId { get; set; }
    public string Datum { get; set; } = "";
    public string Uhrzeit { get; set; } = "";
    public Schicht? Schicht { get; set; }
    public string Verfasser { get; set; } = "";
    public HashSet<int> Bereiche { get; set; } = [];
    public bool Allgemein { get; set; }
    public string Text { get; set; } = "";
    public bool Wichtig { get; set; }
    public bool Uebergabe { get; set; }
    public UebergabeDauer UebergabeDauer { get; set; }
    public string UebergabeBis { get; set; } = "";
    public string UebergabeText { get; set; } = "";
}

// Filter für die Liste der Berichte eines Bewohners.
public sealed record BerichtFilter(DateOnly? Von = null, DateOnly? Bis = null, int? Bereich = null, Schicht? Schicht = null, string Suche = "", bool NurWichtige = false)
{
    public const int Allgemein = 0;   // Bereich = 0 heißt: nur „allgemein“
}

public static partial class BerichtRegeln
{
    // Die fünf Lebensbereiche, Wortlaut wie im HEB-Bogen (docs/heb-wissen.md), immer diese Reihenfolge.
    public static readonly string[] Lebensbereiche =
    [
        "Aufnahme und Gestaltung persönlicher, sozialer Beziehungen",
        "Selbstversorgung und Wohnen",
        "Arbeit, arbeitsähnliche Tätigkeiten, Ausbildung",
        "Tagesgestaltung, Freizeit, Teilnahme am gesellschaftlichen Leben",
        "Umgang mit den Auswirkungen der Behinderung",
    ];

    // Kurzform für Marken und Auswahllisten.
    public static readonly string[] Kurz = ["Beziehungen", "Selbstversorgung, Wohnen", "Arbeit", "Tagesgestaltung, Freizeit", "Umgang mit der Behinderung"];

    public static string BereichText(int nummer) => nummer is >= 1 and <= 5 ? $"{nummer} {Kurz[nummer - 1]}" : "allgemein";

    // ----- Schichten: Früh 6–14 Uhr, Spät 14–22 Uhr, Nacht 22–6 Uhr -----

    public static string SchichtText(Schicht s) => s switch
    {
        Schicht.Frueh => "Frühdienst",
        Schicht.Spaet => "Spätdienst",
        _ => "Nachtdienst",
    };

    public static string Kuerzel(Schicht s) => s switch { Schicht.Frueh => "frueh", Schicht.Spaet => "spaet", _ => "nacht" };

    public static Schicht SchichtAus(string kuerzel) => kuerzel switch
    {
        "frueh" => Schicht.Frueh,
        "spaet" => Schicht.Spaet,
        _ => Schicht.Nacht,
    };

    // Schichtzeiten des Bereichs (Schritt 25, Einstellungen); Vorgabe 6, 14 und 22 Uhr. Das Programm
    // setzt sie beim Start und nach dem Speichern der Einstellungen.
    public static Schichtzeiten Zeiten { get; set; } = Schichtzeiten.Vorgabe;

    public static Schicht SchichtZu(DateTime zeit) => SchichtZu(zeit, Zeiten);

    public static Schicht SchichtZu(DateTime zeit, Schichtzeiten z)
    {
        var t = TimeOnly.FromDateTime(zeit);
        return t >= z.Frueh && t < z.Spaet ? Schicht.Frueh : t >= z.Spaet && t < z.Nacht ? Schicht.Spaet : Schicht.Nacht;
    }

    // Beginn der Schicht, in der „zeit“ liegt.
    public static DateTime SchichtBeginn(DateTime zeit) => SchichtBeginn(zeit, Zeiten);

    public static DateTime SchichtBeginn(DateTime zeit, Schichtzeiten z)
    {
        var tag = DateOnly.FromDateTime(zeit);
        var t = TimeOnly.FromDateTime(zeit);
        if (t >= z.Frueh && t < z.Spaet) return tag.ToDateTime(z.Frueh);
        if (t >= z.Spaet && t < z.Nacht) return tag.ToDateTime(z.Spaet);
        return t >= z.Nacht ? tag.ToDateTime(z.Nacht) : tag.AddDays(-1).ToDateTime(z.Nacht);
    }

    // Übergabe: alles seit Beginn der vorigen Schicht.
    public static DateTime UebergabeBeginn(DateTime jetzt) => SchichtBeginn(jetzt).AddHours(-8);

    // ----- Prüfregeln -----

    [GeneratedRegex(@"^(\d{1,2})[:.](\d{2})$")]
    private static partial Regex UhrzeitMuster();

    public static TimeOnly UhrzeitLesen(string? text)
    {
        text = Eingabeformat.UhrzeitFertig(text);   // 0930 → 09:30 (Schritt 26d)
        var treffer = UhrzeitMuster().Match(text);
        if ((text ?? "").Trim().Length == 0) throw new FormatException("Uhrzeit fehlt.");
        if (!treffer.Success) throw new FormatException("Uhrzeit: bitte als SS:MM eingeben, z. B. 14:30.");
        int stunde = int.Parse(treffer.Groups[1].Value, CultureInfo.InvariantCulture);
        int minute = int.Parse(treffer.Groups[2].Value, CultureInfo.InvariantCulture);
        if (stunde > 23 || minute > 59) throw new FormatException($"Uhrzeit: {text!.Trim()} gibt es nicht.");
        return new TimeOnly(stunde, minute);
    }

    public static string UhrzeitText(DateTime zeit) => zeit.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string ZeitText(DateTime zeit) =>
        zeit.ToString("ddd dd.MM.yyyy, HH:mm", CultureInfo.GetCultureInfo("de-DE")) + " Uhr";

    // Baut einen Bericht aus den Eingaben. Wirft Eingabefehler mit allen Fehlern auf einmal.
    public static Bericht AusEingabe(BerichtEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (e.BewohnerId <= 0) fehler.Add("Bitte einen Bewohner wählen.");
        DateOnly? datum = null;
        TimeOnly? uhrzeit = null;
        try { datum = Kern.Datum.Lesen(e.Datum, "Datum"); } catch (FormatException ex) { fehler.Add(ex.Message); }
        try { uhrzeit = UhrzeitLesen(e.Uhrzeit); } catch (FormatException ex) { fehler.Add(ex.Message); }
        DateTime? zeitpunkt = datum is DateOnly d && uhrzeit is TimeOnly u ? d.ToDateTime(u) : null;
        if (zeitpunkt > jetzt.AddMinutes(1)) fehler.Add("Der Zeitpunkt liegt in der Zukunft.");

        string verfasser = (e.Verfasser ?? "").Trim();
        if (verfasser.Length == 0) fehler.Add("Verfasser (Kürzel) fehlt.");
        else if (Einstellungen.KuerzelPruefen(verfasser) is string kuerzelFehler) fehler.Add(kuerzelFehler.Replace("Kürzel", "Verfasser"));

        var bereiche = e.Bereiche.Where(b => b is >= 1 and <= 5).Order().ToList();
        if (e.Bereiche.Any(b => b is < 1 or > 5)) fehler.Add("Lebensbereich: nur 1 bis 5.");
        if (bereiche.Count == 0 && !e.Allgemein) fehler.Add("Lebensbereich: mindestens einen wählen oder „allgemein“.");
        if (bereiche.Count > 0 && e.Allgemein) fehler.Add("Lebensbereich: „allgemein“ nur ohne Lebensbereich wählen.");

        string text = (e.Text ?? "").Trim();
        if (text.Length == 0) fehler.Add("Text fehlt.");
        if (e.Uebergabe && e.UebergabeDauer == UebergabeDauer.BisDatum)
        {
            try
            {
                if (Kern.Datum.Lesen(e.UebergabeBis, "Übergabe bis") < DateOnly.FromDateTime(jetzt))
                    fehler.Add("Übergabe bis: Das Datum liegt in der Vergangenheit.");
            }
            catch (FormatException ex) { fehler.Add(ex.Message); }
        }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);

        return new Bericht
        {
            BewohnerId = e.BewohnerId,
            Zeitpunkt = zeitpunkt!.Value,
            Schicht = e.Schicht ?? SchichtZu(zeitpunkt.Value),
            Verfasser = verfasser,
            Bereiche = bereiche,
            Text = text,
            Wichtig = e.Wichtig,
            Erfasst = jetzt,
        };
    }

    public static void NachtragPruefen(string verfasser, string text)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(verfasser)) fehler.Add("Verfasser (Kürzel) fehlt.");
        else if (Einstellungen.KuerzelPruefen(verfasser) is string f) fehler.Add(f.Replace("Kürzel", "Verfasser"));
        if (string.IsNullOrWhiteSpace(text)) fehler.Add("Text des Nachtrags fehlt.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
    }

    // Passt ein Bericht zum Filter? Die Suche schaut in Text und Nachträge (Groß/klein egal).
    public static bool Passt(Bericht b, BerichtFilter f)
    {
        var tag = DateOnly.FromDateTime(b.Zeitpunkt);
        if (f.Von is DateOnly von && tag < von) return false;
        if (f.Bis is DateOnly bis && tag > bis) return false;
        if (f.Schicht is Schicht s && b.Schicht != s) return false;
        if (f.NurWichtige && !b.Wichtig) return false;
        if (f.Bereich is int bereich && (bereich == BerichtFilter.Allgemein ? !b.Allgemein : !b.Bereiche.Contains(bereich))) return false;
        string suche = f.Suche.Trim();
        if (suche.Length > 0 &&
            !b.Text.Contains(suche, StringComparison.CurrentCultureIgnoreCase) &&
            !b.Nachtraege.Any(n => n.Text.Contains(suche, StringComparison.CurrentCultureIgnoreCase)))
            return false;
        return true;
    }

    // Was zur Übergabe gewünscht ist (null = keine Übergabe).
    public static UebergabeWunsch? UebergabeAus(BerichtEingabe e) => e.Uebergabe
        ? new UebergabeWunsch(e.UebergabeDauer,
            e.UebergabeDauer == UebergabeDauer.BisDatum ? Kern.Datum.Lesen(e.UebergabeBis, "Übergabe bis") : null,
            (e.UebergabeText ?? "").Trim())
        : null;
}
