using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Übergabeliste (A4 hoch): alle offenen Einträge, je eine Zeile mit Kästchen zum Abhaken; Wohnbereich allgemein
// zuerst, dann nach Schicht (die laufende zuerst) wie auf dem Bildschirm. Lange Texte werden gekürzt („…“).
public sealed class UebergabeDruck(IReadOnlyList<Uebergabe> offen, IReadOnlyDictionary<long, Bewohner> bewohner, DateTime jetzt)
{
    private int naechster;

    public List<(string Abschnitt, Uebergabe U)> Zeilen
    {
        get
        {
            string Zimmer(long id) => bewohner.TryGetValue(id, out var b) ? b.Zimmer : "";
            var liste = offen.Where(u => u.Allgemein).Select(u => ("Wohnbereich allgemein", u)).ToList();
            foreach (var s in UebergabeRegeln.SchichtFolge(jetzt))
                liste.AddRange(UebergabeRegeln.Ordnen(offen.Where(u => !u.Allgemein && UebergabeRegeln.SchichtVon(u) == s), Zimmer)
                    .Select(u => (BerichtRegeln.SchichtText(s), u)));
            return liste;
        }
    }

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Übergabeliste" };
        dokument.BeginPrint += (_, _) => naechster = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechster = SeiteZeichnen(e.Graphics!, naechster);
            e.HasMorePages = naechster < Zeilen.Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 15f, rechts = 195f, unten = 285f, zeile = 4.6f;
        float breite = rechts - links, y = 12f;
        using var linie = new Pen(Color.Gray, 0.2f);
        using var kuerzen = new StringFormat(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter };
        var zeilen = Zeilen;
        g.DrawString("Übergabeliste – offene Einträge", Stil.TagKopf, Brushes.Black, links, y);
        y += 6.5f;
        g.DrawString($"Stand {BerichtRegeln.ZeitText(jetzt)} · {zeilen.Count} offen · {KopfBereich.DemoHinweis}", Stil.Klein, Brushes.DimGray, links, y);
        y += 5f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 1.5f;
        string? letzterAbschnitt = null;
        int i = start;
        for (; i < zeilen.Count; i++)
        {
            var (abschnitt, u) = zeilen[i];
            if (y + zeile + (abschnitt != letzterAbschnitt ? 5.5f : 0f) > unten && i > start) break;
            if (abschnitt != letzterAbschnitt)
            {
                y += 1f;
                g.DrawString(abschnitt, Stil.Fett, Brushes.Black, links, y);
                y += 4.5f;
                letzterAbschnitt = abschnitt;
            }
            string name = u.BewohnerId is long id && bewohner.TryGetValue(id, out var b) ? $"{b.Name}, Zi. {b.Zimmer}" : "";
            string rest = $"{UebergabeRegeln.GueltigText(u, jetzt)} · {u.Verfasser}";
            float restBreite = g.MeasureString(rest, Stil.Klein).Width;
            g.DrawString("☐", Stil.Klein, Brushes.Black, links, y);
            g.DrawString(name, Stil.Klein, Brushes.Black, new RectangleF(links + 5f, y, 45f, zeile), kuerzen);
            g.DrawString(HeuteWichtigRegeln.ErsteZeile(u.Anzeige), Stil.Klein, Brushes.Black,
                new RectangleF(links + 52f, y, breite - 54f - restBreite, zeile), kuerzen);
            g.DrawString(rest, Stil.Klein, Brushes.DimGray, rechts - restBreite, y);
            y += zeile;
        }
        if (zeilen.Count == 0) g.DrawString("Keine offenen Übergabe-Einträge.", Stil.Standard, Brushes.Black, links, y);
        return i;
    }
}
