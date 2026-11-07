using System.Text.Json;

namespace Dokusystem.Kern;

// Demo-Daten aus daten/demo/*.json (lesbare Dateien im Repository, in die .exe eingebaut).
// Alle Angaben dort sind ausgedacht. Datumsangaben der Bereiche entstehen relativ zum heutigen
// Tag, damit die Demo an jedem Tag etwas zeigt (Bauplan in IDEEN.md).
public static class Demodaten
{
    private static readonly JsonSerializerOptions Optionen = new() { PropertyNameCaseInsensitive = true };

    private static T Lesen<T>(string datei)
    {
        using var strom = typeof(Demodaten).Assembly.GetManifestResourceStream($"Dokusystem.Kern.Demo.{datei}")
            ?? throw new InvalidOperationException($"Demo-Daten {datei} fehlen im Programm.");
        return JsonSerializer.Deserialize<T>(strom, Optionen) ?? throw new InvalidOperationException($"Demo-Daten {datei} sind leer.");
    }

    // ----- Bewohner -----

    private sealed class BewohnerDatei
    {
        public List<BewohnerEingabe> Bewohner { get; set; } = [];
    }

    public static List<Bewohner> Bewohner() =>
        Lesen<BewohnerDatei>("bewohner.json").Bewohner.Select(e => BewohnerRegeln.AusEingabe(e)).ToList();

    // ----- Berichte -----

    public sealed class BerichtTexte
    {
        public List<string> Kuerzel { get; set; } = [];
        public Dictionary<string, List<string>> Texte { get; set; } = [];
        public List<string> Nachtraege { get; set; } = [];
        public UebergabeTexte Uebergabe { get; set; } = new();
    }

    public sealed class UebergabeTexte
    {
        public List<string> Saetze { get; set; } = [];
        public List<string> Notizen { get; set; } = [];
    }

    // Ein Demo-Bericht mit seinem Übergabe-Eintrag (falls einer dazugehört).
    public sealed record DemoBericht(Bericht Bericht, UebergabeWunsch? Uebergabe = null, string? GelesenVon = null, string? ErledigtVon = null);
    public sealed record DemoNotiz(string Text, UebergabeWunsch Wunsch, DateTime Erstellt, string Verfasser);
    public sealed record DemoTagebuch(List<DemoBericht> Berichte, List<DemoNotiz> Allgemein);

    public static BerichtTexte BerichtBausteine() => Lesen<BerichtTexte>("berichte.json");

    // Berichte der letzten vier Wochen für die übergebenen Bewohner, über alle Lebensbereiche
    // verteilt; einige wichtig, einige mit Nachtrag, einige mit Übergabe-Eintrag (offen, gelesen,
    // erledigt, abgelaufen), dazu Notizen für den ganzen Wohnbereich. Immer gleich für denselben
    // Zeitpunkt (fester Zufallsstartwert je Bewohner), nichts liegt in der Zukunft.
    public static DemoTagebuch Berichte(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var bausteine = BerichtBausteine();
        var berichte = new List<DemoBericht>();
        string wochentag = jetzt.AddDays(2).ToString("dddd", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        string Satz(int i) => bausteine.Uebergabe.Saetze[i % bausteine.Uebergabe.Saetze.Count].Replace("{tag}", wochentag);
        for (int i = 0; i < bewohner.Count; i++)
        {
            var b = bewohner[i];
            var zufall = new Random(1000 + i);
            for (int tageZurueck = 27; tageZurueck >= 0; tageZurueck--)
            {
                var tag = jetzt.Date.AddDays(-tageZurueck);
                if (DateOnly.FromDateTime(tag) < b.Aufnahmedatum) continue;
                int anzahl = zufall.Next(1, 3);
                for (int n = 0; n < anzahl; n++)
                {
                    // Früh oder Spät, selten Nacht.
                    int schicht = zufall.Next(10) switch { < 5 => 0, < 9 => 1, _ => 2 };
                    var zeit = schicht switch
                    {
                        0 => tag.AddHours(7).AddMinutes(zufall.Next(0, 13 * 30) / 30 * 30),
                        1 => tag.AddHours(14).AddMinutes(30 + zufall.Next(0, 14 * 30) / 30 * 30),
                        _ => tag.AddHours(23).AddMinutes(zufall.Next(0, 4) * 15),
                    };
                    // Immer erzeugen (gleiche Zufallsfolge zu jeder Uhrzeit), nur Vergangenes behalten.
                    var bericht = Erzeugen(b, zeit, zufall, bausteine, jetzt, wichtig: zufall.Next(10) == 0);
                    if (zeit <= jetzt) berichte.Add(new DemoBericht(bericht));
                }
            }
            // Übergabe: bei jedem zweiten Bewohner ein Bericht aus den letzten Stunden für die
            // nächste Schicht, dazu einige länger laufende, erledigte und abgelaufene Einträge.
            if (i % 2 == 0)
            {
                var bericht = Erzeugen(b, jetzt.AddMinutes(-(40 + i * 25)), zufall, bausteine, jetzt, wichtig: i % 4 == 0);
                var wunsch = i switch
                {
                    2 => new UebergabeWunsch(UebergabeDauer.DreiTage),
                    4 => new UebergabeWunsch(UebergabeDauer.BisErledigt, Text: Satz(0)),
                    _ => new UebergabeWunsch(UebergabeDauer.NaechsteSchicht),
                };
                berichte.Add(new DemoBericht(bericht, wunsch, GelesenVon: i == 0 ? "AB" : null));
            }
            if (i == 1)
            {
                var bericht = Erzeugen(b, jetzt.AddDays(-2).Date.AddHours(16), zufall, bausteine, jetzt, wichtig: false);
                berichte.Add(new DemoBericht(bericht, new UebergabeWunsch(UebergabeDauer.BisErledigt, Text: Satz(2)), "AB", ErledigtVon: "CD"));
            }
            if (i == 3)
            {
                var bericht = Erzeugen(b, jetzt.AddDays(-5).Date.AddHours(9), zufall, bausteine, jetzt, wichtig: true);
                berichte.Add(new DemoBericht(bericht, new UebergabeWunsch(UebergabeDauer.NaechsteSchicht)));   // längst abgelaufen
            }
            if (i == 5)
            {
                var bericht = Erzeugen(b, jetzt.AddHours(-20), zufall, bausteine, jetzt, wichtig: true);
                berichte.Add(new DemoBericht(bericht, new UebergabeWunsch(UebergabeDauer.Woche, Text: Satz(1))));
            }
        }
        var notizen = bausteine.Uebergabe.Notizen;
        var allgemein = new List<DemoNotiz>
        {
            new(notizen[0].Replace("{tag}", wochentag), new UebergabeWunsch(UebergabeDauer.Woche), jetzt.AddDays(-1).Date.AddHours(9), bausteine.Kuerzel[1]),
            new(notizen[1].Replace("{tag}", wochentag), new UebergabeWunsch(UebergabeDauer.BisErledigt), jetzt.AddDays(-2).Date.AddHours(15), bausteine.Kuerzel[2]),
        };
        return new DemoTagebuch(berichte, allgemein);
    }

    private static Bericht Erzeugen(Bewohner b, DateTime zeit, Random zufall, BerichtTexte bausteine, DateTime jetzt, bool wichtig)
    {
        int bereich = zufall.Next(0, 6);   // 0 = allgemein
        var texte = bausteine.Texte[bereich == 0 ? "allgemein" : bereich.ToString()];
        var nachtraege = new List<Nachtrag>();
        if (zufall.Next(20) == 0)
        {
            var erfasst = zeit.AddHours(zufall.Next(1, 4));
            if (erfasst <= jetzt)
                nachtraege.Add(new Nachtrag(0, 0, erfasst, bausteine.Kuerzel[zufall.Next(bausteine.Kuerzel.Count)],
                    bausteine.Nachtraege[zufall.Next(bausteine.Nachtraege.Count)]));
        }
        return new Bericht
        {
            BewohnerId = b.Id ?? 0,
            Zeitpunkt = zeit,
            Schicht = BerichtRegeln.SchichtZu(zeit),
            Verfasser = bausteine.Kuerzel[zufall.Next(bausteine.Kuerzel.Count)],
            Bereiche = bereich == 0 ? [] : [bereich],
            Text = texte[zufall.Next(texte.Count)],
            Wichtig = wichtig,
            Erfasst = zeit.AddMinutes(zufall.Next(5, 40)) is var e && e <= jetzt ? e : jetzt,
            Nachtraege = nachtraege,
        };
    }
}
