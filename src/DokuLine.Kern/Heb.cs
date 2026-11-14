using System.Globalization;

namespace DokuLine.Kern;

// HEB-Bogen (Hilfe- und Entwicklungsbogen) nach docs/heb-wissen.md: Bogenarten, Felder je
// Lebensbereich mit Zeichengrenzen, Ziele, Kopfdaten. Das Programm formuliert nichts (Regel 6):
// Es stellt Quellen zusammen, zählt Zeichen, prüft den Stil und bietet Textbausteine an. Die
// Einschätzung des Hilfebedarfs wird nie vorgeschlagen oder vorbelegt.

public enum Bogenart { A, B, C }

public enum FeldArt { Text, Rahmenziel, IchZiel }

// Ein Feld des Bogens. Schluessel ist zugleich der Programmfeldname für die PDF-Zuordnung.
public sealed record HebFeld(string Schluessel, string Titel, int? Grenze, FeldArt Art = FeldArt.Text, int Bereich = 0);

public sealed record HebFassung
{
    public long BogenId { get; init; }
    public IReadOnlyDictionary<string, string> Texte { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<int, int?> Hilfebedarf { get; init; } = new Dictionary<int, int?>();   // Bereich → Stufe 1–6 oder leer
    public bool Fertig { get; init; }
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";

    public string Text(string schluessel) => Texte.TryGetValue(schluessel, out var t) ? t : "";
}

public sealed record HebBogen(long Id, long BewohnerId, Bogenart Art, DateOnly Von, DateOnly Bis, int? Nummer,
    DateOnly Erstellt, IReadOnlyList<HebFassung> Fassungen)
{
    public HebFassung? Aktuell => Fassungen.Count > 0 ? Fassungen[^1] : null;
    public bool Fertig => Aktuell?.Fertig == true;
}

public static class HebRegeln
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string ArtText(Bogenart a) => a switch
    {
        Bogenart.A => "HEB A – Ergebnis der vorläufigen Hilfeplanung (Erstbericht)",
        Bogenart.B => "HEB B – Entwicklungsbericht (Folgebericht)",
        _ => "HEB C – Abschlussbericht",
    };

    public static string ArtKurz(Bogenart a) => $"HEB {a}";

    // Zeichengrenzen je Feld und Lebensbereich 1–5 (docs/heb-wissen.md).
    private static readonly Dictionary<(Bogenart, char), int[]> Grenzen = new()
    {
        [(Bogenart.A, 'a')] = [970, 1045, 940, 940, 940],
        [(Bogenart.A, 'c')] = [795, 785, 770, 770, 770],
        [(Bogenart.A, 'd')] = [795, 1220, 940, 1025, 1025],
        [(Bogenart.B, 'a')] = [340, 510, 345, 430, 430],
        [(Bogenart.B, 'b')] = [770, 940, 950, 855, 865],
        [(Bogenart.B, 'd')] = [685, 770, 605, 845, 690],
        [(Bogenart.B, 'e')] = [600, 600, 525, 515, 520],
        [(Bogenart.C, 'a')] = [345, 520, 430, 430, 340],
        [(Bogenart.C, 'b')] = [1035, 1120, 1120, 1120, 1110],
        [(Bogenart.C, 'd')] = [690, 690, 605, 515, 600],
        [(Bogenart.C, 'e')] = [605, 605, 520, 600, 600],
    };

    // Felder a–e je Bogenart; null beim Hilfebedarf (Ankreuzfeld, kein Text).
    private static readonly Dictionary<Bogenart, (char Buchstabe, string Titel)[]> Bereichsfelder = new()
    {
        [Bogenart.A] =
        [
            ('a', "Beschreibung der aktuellen Situation bzw. Problemlage unter Berücksichtigung der Ressourcen"),
            ('b', "Einschätzung des Hilfebedarfs"),
            ('c', "Rahmenziele für den Planungszeitraum"),
            ('d', "Beschreibung der geplanten Maßnahmen"),
        ],
        [Bogenart.B] =
        [
            ('a', "Reflexion der durchgeführten Maßnahmen"),
            ('b', "Beschreibung der Entwicklung anhand der Rahmenziele"),
            ('c', "Einschätzung des Hilfebedarfs"),
            ('d', "Fortschreibung der Rahmenziele"),
            ('e', "Beschreibung der geplanten Maßnahmen"),
        ],
        [Bogenart.C] =
        [
            ('a', "Reflexion der durchgeführten Maßnahmen"),
            ('b', "Beschreibung der Entwicklung anhand der Rahmenziele"),
            ('c', "Einschätzung des noch bestehenden Hilfebedarfs"),
            ('d', "Welche weiteren Maßnahmen sind vorgesehen"),
            ('e', "Durch wen werden diese Maßnahmen erbracht"),
        ],
    };

    public static char HilfebedarfBuchstabe(Bogenart a) => a == Bogenart.A ? 'b' : 'c';
    public static char? RahmenzielBuchstabe(Bogenart a) => a switch { Bogenart.A => 'c', Bogenart.B => 'd', _ => null };

    public static string HilfebedarfTitel(Bogenart a) => Bereichsfelder[a].First(f => f.Buchstabe == HilfebedarfBuchstabe(a)).Titel;

    // Die Textfelder eines Lebensbereichs (ohne Hilfebedarf).
    public static List<HebFeld> Felder(Bogenart a, int bereich) =>
        Bereichsfelder[a].Where(f => f.Buchstabe != HilfebedarfBuchstabe(a))
            .Select(f => new HebFeld($"{bereich}{f.Buchstabe}", $"{f.Buchstabe}) {f.Titel}",
                Grenzen.TryGetValue((a, f.Buchstabe), out var g) ? g[bereich - 1] : null,
                f.Buchstabe == RahmenzielBuchstabe(a) ? FeldArt.Rahmenziel : FeldArt.Text, bereich))
            .ToList();

    // Felder nach den Lebensbereichen: Ziele (A, B) und weitere Angaben (ohne Zeichengrenze, bis
    // der echte Bogen vorliegt).
    public static List<HebFeld> Zusatzfelder(Bogenart a)
    {
        var liste = new List<HebFeld>();
        if (a != Bogenart.C)
        {
            liste.Add(new("leitziel", "Leitziel (Ich-Form)", null, FeldArt.IchZiel));
            for (int i = 1; i <= 2; i++)
            {
                liste.Add(new($"nahziel{i}", $"Nahziel {i} (Ich-Form, ein Satz, ohne Zeitraum)", null, FeldArt.IchZiel));
                liste.Add(new($"nahziel{i}_einschaetzung", $"Einschätzung zu Nahziel {i} (3–4 Sätze)", null));
            }
        }
        liste.Add(new("sichtweise", "Sichtweise des Leistungsberechtigten", null));
        liste.Add(new("ergaenzend", "Ergänzende Ausführungen", null));
        if (a == Bogenart.C) liste.Add(new("beendigung", "Erläuterung zur Beendigung", null));
        return liste;
    }

    public static IEnumerable<HebFeld> AlleTextfelder(Bogenart a) =>
        Enumerable.Range(1, 5).SelectMany(b => Felder(a, b)).Concat(Zusatzfelder(a));

    // Kopffelder für die PDF-Zuordnung.
    public static readonly string[] Kopffelder = ["name", "geburtsdatum", "aufnahme", "beschlussende", "von", "bis", "nummer", "planung", "erstellt"];

    public static IEnumerable<string> AlleProgrammfelder(Bogenart a) =>
        Kopffelder.Concat(AlleTextfelder(a).Select(f => f.Schluessel)).Concat(Enumerable.Range(1, 5).Select(b => $"hilfe{b}"));

    // „912 / 940“; über der Grenze: true.
    public static (string Text, bool Ueber) Zaehler(string text, int? grenze)
    {
        int n = text.Length;
        return grenze is int g ? ($"{n} / {g}", n > g) : ($"{n} Zeichen", false);
    }

    // Zeitraumvorschlag: A ab Aufnahme, B und C ab dem Ende des letzten Bogens (ohne: Aufnahme).
    public static (DateOnly Von, DateOnly Bis) Zeitraum(Bogenart a, Bewohner b, IReadOnlyList<HebBogen> fruehere, DateOnly heute)
    {
        var letzter = fruehere.Where(x => x.Fertig).OrderBy(x => x.Bis).LastOrDefault();
        var von = a == Bogenart.A || letzter is null ? b.Aufnahmedatum : letzter.Bis;
        return (von, heute);
    }

    // Laufende Nummer eines Entwicklungsberichts: die höchste bisherige plus eins.
    public static int? Nummer(Bogenart a, IReadOnlyList<HebBogen> fruehere) =>
        a == Bogenart.B ? fruehere.Where(x => x.Art == Bogenart.B).Select(x => x.Nummer ?? 0).DefaultIfEmpty(0).Max() + 1 : null;

    // Planungszeitraum in Monaten vom Erstellungsdatum bis zum Ablauf des Beschlusses; ohne
    // Beschluss null. 6, 12 oder 24 werden angekreuzt, sonst freie Angabe.
    public static int? Planungsmonate(Bewohner b, DateOnly erstellt)
    {
        if (b.Beschluss != BeschlussZustand.Ende || b.Beschlussende is not DateOnly ende || ende <= erstellt) return null;
        int monate = (ende.Year - erstellt.Year) * 12 + ende.Month - erstellt.Month;
        if (ende.Day < erstellt.Day) monate--;
        return Math.Max(1, monate);
    }

    public static string PlanungText(int? monate) => monate switch
    {
        null => "Planungszeitraum: offen – kein Beschlussende in der Akte",
        6 or 12 or 24 => $"Planungszeitraum: {monate} Monate",
        _ => $"Planungszeitraum: frei, {monate} Monate",
    };

    public static string HilfebedarfText(int? stufe) => stufe is int s ? $"Stufe {s}" : "noch nicht eingeschätzt";

    // HEB B: Rahmenziele, Leitziel und Nahziele des letzten Bogens zum Fortschreiben vorlegen.
    public static Dictionary<string, string> Fortschreiben(Bogenart neu, HebBogen? letzter)
    {
        var texte = new Dictionary<string, string>();
        if (neu != Bogenart.B || letzter?.Aktuell is not HebFassung f) return texte;
        if (RahmenzielBuchstabe(letzter.Art) is char alt)
            for (int b = 1; b <= 5; b++)
                if (f.Text($"{b}{alt}") is { Length: > 0 } t) texte[$"{b}d"] = t;
        foreach (var k in new[] { "leitziel", "nahziel1", "nahziel2" })
            if (f.Text(k) is { Length: > 0 } t) texte[k] = t;
        return texte;
    }

    public static HebFassung AusEingabe(long bogenId, Bogenart art, IReadOnlyDictionary<string, string> texte,
        IReadOnlyDictionary<int, int?> hilfebedarf, bool fertig, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        var erlaubt = AlleTextfelder(art).Select(f => f.Schluessel).ToHashSet();
        if (texte.Keys.Any(k => !erlaubt.Contains(k))) fehler.Add("Unbekanntes Feld.");
        if (hilfebedarf.Values.Any(s => s is < 1 or > 6)) fehler.Add("Hilfebedarf: nur Stufe 1 bis 6.");
        if (fertig && texte.Values.All(string.IsNullOrWhiteSpace)) fehler.Add("Für „fertig“ bitte mindestens ein Feld ausfüllen.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new HebFassung
        {
            BogenId = bogenId, Texte = texte.Where(x => x.Value.Trim().Length > 0).ToDictionary(x => x.Key, x => x.Value.Trim()),
            Hilfebedarf = Enumerable.Range(1, 5).ToDictionary(b => b, b => hilfebedarf.GetValueOrDefault(b)),
            Fertig = fertig, Erstellt = jetzt, Kuerzel = kuerzel.Trim(),
        };
    }

    // Alle Felder als ein Text (für den Konflikt-Hinweis).
    public static string AlsText(Bogenart art, HebFassung f) =>
        (f.Fertig ? "fertig" : "Entwurf") + Environment.NewLine +
        string.Join(Environment.NewLine, AlleTextfelder(art).Where(x => f.Text(x.Schluessel).Length > 0)
            .Select(x => $"{Environment.NewLine}{(x.Bereich > 0 ? $"Bereich {x.Bereich} · " : "")}{x.Titel}:{Environment.NewLine}{f.Text(x.Schluessel)}")) +
        Environment.NewLine + Environment.NewLine + "Hilfebedarf: " +
        string.Join(", ", Enumerable.Range(1, 5).Select(i => $"{i}: {f.Hilfebedarf.GetValueOrDefault(i)?.ToString(CultureInfo.InvariantCulture) ?? "–"}"));

    public static bool Gleich(HebFassung a, HebFassung b) =>
        a.Fertig == b.Fertig && a.Texte.Count == b.Texte.Count && a.Texte.All(x => b.Text(x.Key) == x.Value) &&
        Enumerable.Range(1, 5).All(i => a.Hilfebedarf.GetValueOrDefault(i) == b.Hilfebedarf.GetValueOrDefault(i));

    // Überschreitungen der Zeichengrenzen (für den Hinweis vor „fertig“ und vor dem PDF).
    public static List<string> ZuLang(Bogenart a, HebFassung f) =>
        AlleTextfelder(a).Where(x => x.Grenze is int g && f.Text(x.Schluessel).Length > g)
            .Select(x => $"Bereich {x.Bereich} {x.Titel[..2]} {f.Text(x.Schluessel).Length} / {x.Grenze}").ToList();

    public static string ZeitraumText(HebBogen h) => $"{Datum.Text(h.Von)} – {Datum.Text(h.Bis)}";

    public static List<string> Verlauf(HebBogen h) =>
        h.Fassungen.Select((f, i) =>
        {
            string was = i == 0 ? "angelegt" :
                f.Fertig && !h.Fassungen[i - 1].Fertig ? "als fertig markiert" :
                !f.Fertig && h.Fassungen[i - 1].Fertig ? "wieder Entwurf" : "geändert";
            return $"{f.Erstellt:dd.MM.yyyy, HH:mm} · {was} von {f.Kuerzel} · {f.Texte.Count} Felder ausgefüllt";
        }).ToList();
}

// Textbausteine zum Einfügen per Klick (docs/heb-wissen.md). Das Programm setzt sie nicht selbst ein.
public static class HebBausteine
{
    public static readonly (string Gruppe, string[] Bausteine)[] Alle =
    [
        ("Verben für Rahmenziele", ["lernt", "übt", "erkennt", "entwickelt", "baut auf", "gestaltet", "findet", "nutzt"]),
        ("Verben für Maßnahmen", ["auffordern", "erinnern", "anleiten", "begleiten", "unterstützen", "motivieren", "üben",
                                   "besprechen", "bereitstellen", "vermitteln", "Kontakt herstellen"]),
        ("Feste Formulierungen", ["benötigt Aufforderung", "unter Begleitung und Aufforderung", "kommt … nach", "kam … nicht nach",
                                   "ließ sich auf … ein", "im therapeutischen Bereich", "bei Bedarf", "gibt an, dass"]),
        ("Häufigkeit statt Datum", ["täglich", "wöchentlich", "meist", "selten", "vermehrt", "anfänglich", "zuletzt", "gelegentlich", "regelmäßig"]),
    ];

    // Merkhilfe neben dem Feld (Regeln, die nur der Mensch einhalten kann).
    public static readonly string[] Merkhilfe =
    [
        "Wertneutral beschreiben, Aussagen des Bewohners zuordnen („gibt an, dass …“).",
        "Zuerst, was der Bewohner tut oder kann, dann die Unterstützung.",
        "Einzelereignisse verdichten, Häufigkeit statt Datum, die Reaktion des Bewohners aufnehmen.",
        "Rahmenziele als Lern- oder Entwicklungsschritt, kein Verbot, nichts Beschämendes.",
        "HEB A und B im Präsens, HEB C im Präteritum.",
    ];
}
