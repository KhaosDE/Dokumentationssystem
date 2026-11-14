namespace DokuLine.Kern;

// Demo-Ziele (Bauplan 43), in den Worten der (ausgedachten) Bewohner, wertneutral. Ohne Zufall: dieselbe Demo bei jedem
// Zurücksetzen. Jeder siebte hat nur ein Nahziel; jeder fünfte ein Ziel mit fälliger Überprüfung.
public static partial class Demodaten
{
    private static readonly (string Text, int Lebensbereich)[] Leitziele =
    [
        ("Ich möchte irgendwann in einer eigenen Wohnung leben.", 2),
        ("Ich möchte wieder arbeiten gehen.", 3),
        ("Ich möchte mehr Kontakt zu meiner Familie haben.", 1),
        ("Ich möchte meinen Tag selbst planen können.", 4),
        ("Ich möchte mit meiner Erkrankung besser zurechtkommen.", 5),
    ];

    private static readonly (string Text, int Lebensbereich)[] Nahziele =
    [
        ("Ich halte mein Zimmer selbst in Ordnung.", 2),
        ("Ich gehe einmal in der Woche allein einkaufen.", 2),
        ("Ich gehe dreimal in der Woche in die Werkstatt.", 3),
        ("Ich rufe sonntags meine Familie an.", 1),
        ("Ich nehme an einer Freizeitgruppe teil.", 4),
        ("Ich erkenne meine Frühwarnzeichen und sage Bescheid.", 5),
        ("Ich teile mein Wochengeld selbst ein.", 2),
    ];

    public static IEnumerable<(ZielArt Art, ZielFassung Fassung)> Ziele(int i, DateOnly heute)
    {
        var vereinbart = heute.AddDays(-40 - i % 30);
        ZielFassung F(string text, int lb, ZielArt art) => new()
        {
            Wortlaut = text, Lebensbereich = lb, VereinbartAm = vereinbart, MitWem = "EF",
            UeberpruefenBis = i % 5 == 4 && art == ZielArt.Nahziel ? heute.AddDays(-3) : ZielRegeln.UeberpruefenVorschlag(art, vereinbart).AddDays(i % 4 * 14),
            Kuerzel = "EF", Erfasst = vereinbart.ToDateTime(new TimeOnly(11, 0)),
        };
        var leit = Leitziele[i % Leitziele.Length];
        yield return (ZielArt.Leitziel, F(leit.Text, leit.Lebensbereich, ZielArt.Leitziel));
        int anzahl = i % 7 == 6 ? 1 : 2;
        for (int n = 0; n < anzahl; n++)
        {
            var nah = Nahziele[(i + n * 3) % Nahziele.Length];
            yield return (ZielArt.Nahziel, F(nah.Text, nah.Lebensbereich, ZielArt.Nahziel));
        }
    }
}
