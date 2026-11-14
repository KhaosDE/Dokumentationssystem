using System.Globalization;
using System.Text;

namespace DokuLine.Kern;

// Zeitauswertung je Bewohner (Bauplan 41): erfasste Minuten je Zeitraum nach Lebensbereich und nach Art (Maßnahmen,
// Therapie, Gespräche/Sonstiges), dazu geplant gegenüber tatsächlich bei den Maßnahmen. Eine Zählung, keine Abrechnung;
// ausgewertet wird nur je Bewohner, nie je Mitarbeiter (Mitarbeitervertretung, docs/betrieb.md).
public enum ZeitArt { Massnahmen, Therapie, Sonstiges }

// Ein erfasster Zeitbetrag; Lebensbereich null = ohne Zuordnung (z. B. Therapie).
public sealed record ZeitPosten(long BewohnerId, DateOnly Tag, ZeitArt Art, int? Lebensbereich, int Minuten);

public sealed record ZeitZeile(Bewohner Bewohner, IReadOnlyList<int> JeLebensbereich, int OhneLebensbereich, int Massnahmen, int Therapie, int Sonstiges,
    int MassnahmenGeplant)
{
    public int Summe => Massnahmen + Therapie + Sonstiges;
}

public static class ZeitauswertungRegeln
{
    public const string Hinweis = "Zählung erfasster Zeiten, keine Abrechnung – das Verfahren des Bezirks wird nachgezogen, sobald es feststeht. " +
                                  "Ausgewertet wird nur je Bewohner, nie je Mitarbeiter.";
    public const int DauerHoechstens = 600;

    public static string ArtText(ZeitArt a) => a switch
    {
        ZeitArt.Massnahmen => "Maßnahmen",
        ZeitArt.Therapie => "Therapie",
        _ => "Gespräche/Sonstiges",
    };

    // „Dauer in Minuten“ an einem Bericht: freiwillig; leer = keine Angabe.
    public static int? DauerLesen(string? text, List<string> fehler)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 0) return null;
        if (int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out int d) && d is > 0 and <= DauerHoechstens) return d;
        fehler.Add($"Dauer: eine ganze Zahl von 1 bis {DauerHoechstens} Minuten (oder leer lassen).");
        return null;
    }

    // Geplante Minuten der Maßnahmen im Zeitraum: je fälligem Termin der Zeitwert der geltenden Fassung („bei Bedarf“ nicht).
    public static int Geplant(IEnumerable<Massnahme> massnahmen, DateOnly von, DateOnly bis)
    {
        int summe = 0;
        foreach (var m in massnahmen)
            for (var tag = von; tag <= bis; tag = tag.AddDays(1))
                if (m.Laeuft(tag) && m.Am(tag) is MassnahmeFassung f && f.Rhythmus != MassnahmeRhythmus.BeiBedarf && MassnahmenRegeln.AmTag(m, f, tag))
                    summe += f.Minuten;
        return summe;
    }

    // Ein Bericht mit Dauer zählt bei seinem ersten Lebensbereich (mehrere teilen hieße Minuten erfinden); „allgemein“ ohne.
    public static ZeitPosten? AusBericht(Bericht b) =>
        b.Dauer is int d && b.Art != MassnahmenRegeln.ArtBericht
            ? new ZeitPosten(b.BewohnerId, DateOnly.FromDateTime(b.Zeitpunkt), ZeitArt.Sonstiges, b.Bereiche.Count > 0 ? b.Bereiche.Min() : null, d)
            : null;

    public static List<ZeitZeile> Zeilen(IEnumerable<Bewohner> bewohner, IEnumerable<ZeitPosten> posten, IReadOnlyDictionary<long, int> geplant)
    {
        var je = posten.ToLookup(p => p.BewohnerId);
        return [.. bewohner.Select(b =>
        {
            var eigene = je[b.Id!.Value].ToList();
            int[] lb = [.. Enumerable.Range(1, 5).Select(n => eigene.Where(p => p.Lebensbereich == n).Sum(p => p.Minuten))];
            int Art(ZeitArt a) => eigene.Where(p => p.Art == a).Sum(p => p.Minuten);
            return new ZeitZeile(b, lb, eigene.Where(p => p.Lebensbereich is null).Sum(p => p.Minuten), Art(ZeitArt.Massnahmen), Art(ZeitArt.Therapie),
                Art(ZeitArt.Sonstiges), geplant.GetValueOrDefault(b.Id!.Value));
        })];
    }

    public static readonly string[] Spalten =
        ["Zimmer", "Bewohner", "LB 1", "LB 2", "LB 3", "LB 4", "LB 5", "ohne LB", "Maßnahmen", "Therapie", "Gespräche/Sonstiges", "Summe", "Maßnahmen geplant"];

    public static string[] Werte(ZeitZeile z) =>
        [z.Bewohner.Zimmer, z.Bewohner.Name, .. z.JeLebensbereich.Select(Min), Min(z.OhneLebensbereich), Min(z.Massnahmen), Min(z.Therapie),
         Min(z.Sonstiges), Min(z.Summe), Min(z.MassnahmenGeplant)];

    private static string Min(int minuten) => minuten.ToString(CultureInfo.InvariantCulture);

    // Für Excel: Semikolon, Minuten als ganze Zahlen, oben Zeitraum und Hinweis. Kein Netz, nur eine Datei.
    public static string Csv(IReadOnlyList<ZeitZeile> zeilen, DateOnly von, DateOnly bis)
    {
        static string Feld(string s) => s.Contains(';') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        var sb = new StringBuilder();
        sb.AppendLine($"Zeitauswertung (Minuten);{Datum.Text(von)} – {Datum.Text(bis)}");
        sb.AppendLine(Feld(Hinweis));
        sb.AppendLine(string.Join(';', Spalten));
        foreach (var z in zeilen) sb.AppendLine(string.Join(';', Werte(z).Select(Feld)));
        return sb.ToString();
    }
}
