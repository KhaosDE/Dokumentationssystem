using System.Drawing.Printing;

namespace DokuLine;

// Eine Tabelle zum Drucken (A4 quer): Überschrift, eine Zeile darunter (Stand, Hinweis), Spaltenköpfe auf jeder Seite.
// Breiten sind Anteile; Text, der nicht passt, wird mit „…“ gekürzt (die Tabelle bleibt eine Zeile je Eintrag).
public sealed class TabellenDruck(string ueberschrift, string unterzeile, IReadOnlyList<string> spalten, IReadOnlyList<float> breiten,
    IReadOnlyList<string[]> zeilen)
{
    private int naechste;

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = ueberschrift };
        dokument.DefaultPageSettings.Landscape = true;
        dokument.BeginPrint += (_, _) => naechste = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechste = SeiteZeichnen(e.Graphics!, naechste);
            e.HasMorePages = naechste < zeilen.Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 15f, rechts = 282f, unten = 195f, zeile = 5.5f;
        float y = 12f, gesamt = breiten.Sum();
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var normal = new Font("Segoe UI", 8.5F);
        using var fett = new Font("Segoe UI Semibold", 8.5F);
        using var klein = new Font("Segoe UI", 7.5F);
        using var linie = new Pen(Color.Gray, 0.2f);
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(ueberschrift, titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"{unterzeile} · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, new RectangleF(links, y, rechts - links, 10f));
        y += 7f;
        float[] x = new float[spalten.Count + 1];
        x[0] = links;
        for (int s = 0; s < spalten.Count; s++) x[s + 1] = x[s] + (rechts - links) * breiten[s] / gesamt;
        void Reihe(IReadOnlyList<string> werte, Font schrift)
        {
            for (int s = 0; s < spalten.Count && s < werte.Count; s++)
                g.DrawString(werte[s], schrift, Brushes.Black, new RectangleF(x[s], y, x[s + 1] - x[s] - 1f, zeile), format);
            y += zeile;
            g.DrawLine(linie, links, y - 0.5f, rechts, y - 0.5f);
        }
        Reihe(spalten, fett);
        int i = start;
        for (; i < zeilen.Count && y + zeile <= unten; i++) Reihe(zeilen[i], normal);
        if (zeilen.Count == 0) g.DrawString("Keine Einträge.", normal, Brushes.Black, links, y);
        return i;
    }
}
