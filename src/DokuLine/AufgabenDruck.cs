using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Ausdruck der offenen Aufgaben (A4 hoch), in der Reihenfolge der Liste.
public sealed class AufgabenDruck(IReadOnlyList<Aufgabe> offen, IReadOnlyDictionary<long, string> namen, DateTime jetzt, string ueberschrift = "Offene Aufgaben")
{
    private int naechste;

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = ueberschrift };
        dokument.BeginPrint += (_, _) => naechste = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechste = SeiteZeichnen(e.Graphics!, naechste);
            e.HasMorePages = naechste < offen.Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f, unten = 280f;
        float breite = rechts - links - 6f, y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var normal = new Font("Segoe UI", 10F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        g.DrawString(ueberschrift, titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Stand {BerichtRegeln.ZeitText(jetzt)} · {offen.Count} offen · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y);
        y += 6f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 3f;
        int i = start;
        for (; i < offen.Count; i++)
        {
            var a = offen[i];
            bool ueberfaellig = AufgabenRegeln.Ueberfaellig(a, jetzt);
            string meta = string.Join(" · ", new[]
            {
                a.BewohnerId is long id ? namen.GetValueOrDefault(id, "?") : "Wohnbereich",
                AufgabenRegeln.FuerText(a),
                AufgabenRegeln.FaelligText(a, jetzt),
                $"von {a.Verfasser}",
                a.Weitergaben.Count > 0 ? $"{a.Weitergaben.Count}× weitergegeben" : "",
            }.Where(t => t.Length > 0));
            float hoehe = g.MeasureString(a.Text, normal, new SizeF(breite, 1000f)).Height + 9f;
            if (y + hoehe > unten && i > start) break;
            g.DrawString("☐", normal, Brushes.Black, links, y);
            g.DrawString(a.Text, ueberfaellig ? fett : normal, Brushes.Black, new RectangleF(links + 6f, y, breite, 1000f));
            y += g.MeasureString(a.Text, normal, new SizeF(breite, 1000f)).Height;
            g.DrawString(meta, klein, Brushes.DimGray, links + 6f, y);
            y += 6f;
        }
        if (offen.Count == 0) g.DrawString("Keine offenen Aufgaben.", normal, Brushes.Black, links, y);
        return i;
    }
}
