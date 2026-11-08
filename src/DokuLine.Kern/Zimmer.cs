using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Zimmer und Betten (Bauplan Schritt 26e). Ein Zimmer gehört zu einem Wohnbereich und hat ein Bett
// (Einzelzimmer) oder zwei („Bett Fenster“, „Bett Tür“). Ein Bett ist frei oder von einem Bewohner
// belegt; welcher Bewohner wo liegt, steht am Bewohner (Zimmer und Bett).
public sealed record Zimmer(string Nummer, string Bereich, int Betten, bool Aktiv = true)
{
    public string Art => Betten == 1 ? "Einzelzimmer" : "Doppelzimmer";
}

// Ein Bett zur Auswahl, z. B. „102 – Bett Fenster“ oder „104 – Einzelzimmer“.
public sealed record Bettplatz(string Zimmer, string Bett, string Bereich)
{
    public string Anzeige => $"{Zimmer} – {ZimmerRegeln.BettText(Bett)}";
}

// Ein Wohnbereich mit Art: offen oder beschützend.
public sealed record Wohnbereich(string Name, bool Beschuetzt)
{
    public string Art => Beschuetzt ? "beschützend" : "offen";
    public string Anzeige => $"{Name} ({Art})";
}

public static partial class ZimmerRegeln
{
    public const string Einzel = "einzel", Fenster = "fenster", Tuer = "tuer";
    public const string KeinBettFrei = "In diesem Bereich ist kein Bett frei.";

    public static bool BettGueltig(string bett) => bett is "" or Einzel or Fenster or Tuer;

    public static string BettText(string bett) => bett switch
    {
        Einzel => "Einzelzimmer",
        Fenster => "Bett Fenster",
        Tuer => "Bett Tür",
        _ => "kein Bett zugeordnet",
    };

    public static IReadOnlyList<string> Bettschluessel(int betten) => betten == 1 ? [Einzel] : [Fenster, Tuer];

    [GeneratedRegex(@"^[\p{L}\d][\p{L}\d .-]{0,9}$")]
    private static partial Regex NummerMuster();

    public static List<string> Pruefen(Zimmer z, IReadOnlyCollection<string> wohnbereiche)
    {
        var fehler = new List<string>();
        if (!NummerMuster().IsMatch(z.Nummer.Trim())) fehler.Add("Zimmernummer: 1 bis 10 Zeichen, z. B. 101.");
        if (!wohnbereiche.Contains(z.Bereich)) fehler.Add("Bitte einen Wohnbereich wählen.");
        if (z.Betten is not (1 or 2)) fehler.Add("Ein Zimmer hat ein oder zwei Betten.");
        return fehler;
    }

    // Freie Betten: alle Betten aktiver Zimmer des Bereichs, die kein (anderer) Bewohner belegt.
    public static List<Bettplatz> FreieBetten(IEnumerable<Zimmer> zimmer, IEnumerable<Bewohner> bewohner, string bereich, long? ausser = null)
    {
        var belegt = bewohner.Where(b => b.Id != ausser && b.Bett.Length > 0).Select(b => (b.Zimmer, b.Bett)).ToHashSet();
        return [.. zimmer.Where(z => z.Aktiv && z.Bereich == bereich)
                         .OrderBy(z => z.Nummer, StringComparer.Ordinal)
                         .SelectMany(z => Bettschluessel(z.Betten).Select(b => new Bettplatz(z.Nummer, b, z.Bereich)))
                         .Where(p => !belegt.Contains((p.Zimmer, p.Bett)))];
    }
}
