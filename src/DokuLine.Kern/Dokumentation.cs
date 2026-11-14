using System.Globalization;
using System.Text.Json;

namespace DokuLine.Kern;

// Bereich „Dokumentation“: Filter über die Berichte eines Bewohners, die drei Marken und die Gliederung nach Tagen.
// Fachregel: IDEEN.md „Bereich Dokumentation: alle Berichte an einem Ort, kompakt, mit Filter“.
public enum Zeitraum { Heute, SiebenTage, DreissigTage, Frei }

public enum DokuMarke { Wichtig, MedikationGeaendert, Bedarfsgabe }

public sealed record DokuFilter
{
    public Zeitraum Zeitraum { get; init; } = Zeitraum.SiebenTage;
    public DateOnly? Von { get; init; }   // nur bei „frei“
    public DateOnly? Bis { get; init; }
    public bool Wichtig { get; init; }
    public bool MedikationGeaendert { get; init; }
    public bool Bedarfsgabe { get; init; }
    public bool Visite { get; init; }
    public bool Uebergabe { get; init; }
    public bool MitNachtrag { get; init; }
    public bool OhneMassnahmen { get; init; }   // die kurzen Einträge der bestätigten Maßnahmen ausblenden
    public string? Art { get; init; }
    public int? Bereich { get; init; }    // 0 = allgemein
    public Schicht? Schicht { get; init; }
    public string? Verfasser { get; init; }
    public string Suche { get; init; } = "";
}

public sealed record GespeicherterFilter(string Name, DokuFilter Filter);

public static class DokuRegeln
{
    public const string ArtAlltag = "Alltag", ArtVisite = "Visite", ArtMedikation = "Medikation", ArtTherapie = "Therapie",
        ArtAbgaengigkeit = "Ausgang/Abgängigkeit", ArtMassnahme = MassnahmenRegeln.ArtBericht;

    public static readonly string[] FesteArten = [ArtAlltag, ArtVisite, ArtMedikation, ArtTherapie, ArtAbgaengigkeit, ArtMassnahme];

    public static readonly string[] Berichtsarten =
    [
        ArtAlltag, "Krise", "Arztkontakt", ArtVisite, ArtMedikation, "Konflikt", ArtAbgaengigkeit,
        "Angehörigenkontakt", "Betreuer/Behörde", "Besonderes Vorkommnis", ArtTherapie, ArtMassnahme,
    ];

    public static string ZeitraumText(Zeitraum z) => z switch
    {
        Zeitraum.Heute => "Heute",
        Zeitraum.SiebenTage => "7 Tage",
        Zeitraum.DreissigTage => "30 Tage",
        _ => "frei",
    };

    public static (DateOnly Von, DateOnly Bis) Spanne(DokuFilter f, DateOnly heute) => f.Zeitraum switch
    {
        Zeitraum.Heute => (heute, heute),
        Zeitraum.SiebenTage => (heute.AddDays(-6), heute),
        Zeitraum.DreissigTage => (heute.AddDays(-29), heute),
        _ => (f.Von ?? DateOnly.MinValue, f.Bis ?? DateOnly.MaxValue),
    };

    // „Medikation geändert“ ist ein Bericht der Art „Medikation“, der keine Bedarfsgabe ist.
    public static List<DokuMarke> Marken(Bericht b)
    {
        var marken = new List<DokuMarke>();
        if (b.Wichtig) marken.Add(DokuMarke.Wichtig);
        if (b.Art == ArtMedikation && !b.Bedarfsgabe) marken.Add(DokuMarke.MedikationGeaendert);
        if (b.Bedarfsgabe) marken.Add(DokuMarke.Bedarfsgabe);
        return marken;
    }

    public static string MarkeText(DokuMarke m) => m switch
    {
        DokuMarke.Wichtig => "Wichtig",
        DokuMarke.MedikationGeaendert => "Medikation geändert",
        _ => "Bedarfsgabe",
    };

    public static bool Passt(Bericht b, DokuFilter f, DateOnly heute)
    {
        var (von, bis) = Spanne(f, heute);
        var tag = DateOnly.FromDateTime(b.Zeitpunkt);
        if (tag < von || tag > bis) return false;
        var marken = Marken(b);
        if (f.Wichtig && !marken.Contains(DokuMarke.Wichtig)) return false;
        if (f.MedikationGeaendert && !marken.Contains(DokuMarke.MedikationGeaendert)) return false;
        if (f.Bedarfsgabe && !b.Bedarfsgabe) return false;
        if (f.Visite && b.Art != ArtVisite) return false;
        if (f.Uebergabe && !b.Uebergabe) return false;
        if (f.MitNachtrag && b.Nachtraege.Count == 0) return false;
        if (f.OhneMassnahmen && b.Art == ArtMassnahme) return false;
        if (f.Art is string art && b.Art != art) return false;
        if (f.Bereich is int bereich
            && (bereich == BerichtFilter.Allgemein ? !b.Allgemein : !b.Bereiche.Contains(bereich))) return false;
        if (f.Schicht is Schicht s && b.Schicht != s) return false;
        if (f.Verfasser is string v && !b.Verfasser.Equals(v, StringComparison.OrdinalIgnoreCase)) return false;
        string suche = f.Suche.Trim();
        return suche.Length == 0 || b.Text.Contains(suche, StringComparison.CurrentCultureIgnoreCase)
            || b.Nachtraege.Any(n => n.Text.Contains(suche, StringComparison.CurrentCultureIgnoreCase));
    }

    // Die kurzen Einträge der Maßnahmen eines Tages stehen zusammengefasst unter den geschriebenen Berichten
    // (Bauplan 39), damit sie diese nicht verdrängen.
    public static (List<Bericht> Geschrieben, List<Bericht> Massnahmen) Teilen(IEnumerable<Bericht> tag)
    {
        var liste = tag.ToList();
        return ([.. liste.Where(b => b.Art != ArtMassnahme)], [.. liste.Where(b => b.Art == ArtMassnahme)]);
    }

    // Neueste oben, je Tag ein Abschnitt.
    public static List<(DateOnly Tag, List<Bericht> Berichte)> NachTagen(IEnumerable<Bericht> berichte) =>
        [.. berichte.OrderByDescending(b => b.Zeitpunkt).ThenByDescending(b => b.Id)
            .GroupBy(b => DateOnly.FromDateTime(b.Zeitpunkt))
            .Select(g => (g.Key, g.ToList()))];

    // „Montag, 09.11.2026 · 3 Berichte“
    public static string TagText(DateOnly tag, int anzahl) =>
        tag.ToString("dddd, dd.MM.yyyy",
            CultureInfo.GetCultureInfo("de-DE")) + $" · {anzahl} {(anzahl == 1 ? "Bericht" : "Berichte")}";

    public static string SchichtWort(Schicht s) => s switch
    {
        Schicht.Frueh => "Früh",
        Schicht.Spaet => "Spät",
        _ => "Nacht"
    };

    // Visiten und Therapie-Berichte erkennt man am fetten Anfang „Visite Psychiater – Dr. …“ bzw. „Therapie Holzwerkstatt“
    // (vor „, dabei:“ bzw. dem ersten Doppelpunkt).
    public static string? FettAnfang(Bericht b)
    {
        if (b.Art is not (ArtVisite or ArtTherapie)) return null;
        int[] stellen = [b.Text.IndexOf(", dabei: ", StringComparison.Ordinal), b.Text.IndexOf(": ",
            StringComparison.Ordinal)];
        int i = stellen.Where(x => x > 0).DefaultIfEmpty(-1).Min();
        return i > 0 ? b.Text[..i] : null;
    }

    // Wo der Suchtext vorkommt (Groß- und Kleinschreibung egal), für die Hervorhebung.
    public static List<(int Start, int Laenge)> Fundstellen(string text, string suche)
    {
        var liste = new List<(int, int)>();
        suche = suche.Trim();
        if (suche.Length == 0) return liste;
        for (int i = text.IndexOf(suche, StringComparison.CurrentCultureIgnoreCase); i >= 0;
             i = text.IndexOf(suche, i + suche.Length, StringComparison.CurrentCultureIgnoreCase))
            liste.Add((i, suche.Length));
        return liste;
    }

    // Die gesetzten Filter als kurze Marken mit „×“ (Text, Filter ohne diese Angabe).
    public static List<(string Text, DokuFilter Ohne)> Gesetzt(DokuFilter f)
    {
        var liste = new List<(string, DokuFilter)>();
        if (f.Zeitraum != Zeitraum.SiebenTage) liste.Add((ZeitraumText(f), f with
        {
            Zeitraum = Zeitraum.SiebenTage,
            Von = null,
            Bis = null
        }));
        if (f.Wichtig) liste.Add(("Wichtig", f with { Wichtig = false }));
        if (f.MedikationGeaendert) liste.Add(("Medikation geändert", f with { MedikationGeaendert = false }));
        if (f.Bedarfsgabe) liste.Add(("Bedarfsgabe", f with { Bedarfsgabe = false }));
        if (f.Visite) liste.Add(("Visite", f with { Visite = false }));
        if (f.Uebergabe) liste.Add(("Übergabe", f with { Uebergabe = false }));
        if (f.MitNachtrag) liste.Add(("mit Nachtrag", f with { MitNachtrag = false }));
        if (f.OhneMassnahmen) liste.Add(("ohne Maßnahmen", f with { OhneMassnahmen = false }));
        if (f.Art is string art) liste.Add(($"Art: {art}", f with { Art = null }));
        if (f.Bereich is int b) liste.Add((b == BerichtFilter.Allgemein ? "allgemein" : $"Lebensbereich {b}",
            f with { Bereich = null }));
        if (f.Schicht is Schicht s) liste.Add((BerichtRegeln.SchichtText(s), f with { Schicht = null }));
        if (f.Verfasser is string v) liste.Add(($"von {v}", f with { Verfasser = null }));
        if (f.Suche.Trim().Length > 0) liste.Add(($"„{f.Suche.Trim()}“", f with { Suche = "" }));
        return liste;
    }

    private static string ZeitraumText(DokuFilter f) => f.Zeitraum == Zeitraum.Frei
        ? $"{(f.Von is DateOnly v ? Datum.Text(v) : "…")} – {(f.Bis is DateOnly b ? Datum.Text(b) : "…")}"
        : ZeitraumText(f.Zeitraum);

    private static readonly JsonSerializerOptions JsonOptionen = new() { WriteIndented = false };

    public static string AlsJson(IEnumerable<GespeicherterFilter> filter) => JsonSerializer.Serialize(filter.ToList(),
        JsonOptionen);

    public static List<GespeicherterFilter> AusJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<GespeicherterFilter>>(json, JsonOptionen) ?? []; }
        catch (JsonException) { return []; }
    }

    // Nur für die Demo-Berichte: eine passende Art aus Stichworten (das Programm ordnet echte Berichte nie selbst ein).
    public static string ArtFuerDemo(string text)
    {
        string t = text.ToLower(CultureInfo.GetCultureInfo("de-DE"));
        if (t.Contains("arzt") || t.Contains("praxis") || t.Contains("termin bei")) return "Arztkontakt";
        if (t.Contains("telefon") || t.Contains("schwester") || t.Contains("mutter")
            || t.Contains("besuch")) return "Angehörigenkontakt";
        if (t.Contains("streit") || t.Contains("auseinandersetzung")) return "Konflikt";
        if (t.Contains("unruhig") || t.Contains("angespannt") || t.Contains("krise")) return "Krise";
        if (t.Contains("ausgang") || t.Contains("zurück")) return "Ausgang/Abgängigkeit";
        if (t.Contains("betreuer") || t.Contains("amt") || t.Contains("behörde")) return "Betreuer/Behörde";
        return ArtAlltag;
    }
}
