using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// „Drucken …“ in der Übersicht: Kopf, die vier Kästen als Text und die Bewohnerliste als Tabelle.
public sealed class UebersichtDruck(string listenKopf, string kopf, IReadOnlyList<HeuteKasten> kaesten, IReadOnlyList<BewohnerZeile> zeilen, DateTime jetzt)
{
    private int naechster;

    // Alle Zeilen des Ausdrucks: (Abschnitt?, Text, Spalten?) – Spalten nur für die Bewohnerliste.
    public List<(string? Abschnitt, string Text)> Zeilen()
    {
        var liste = new List<(string?, string)>();
        foreach (var k in kaesten)
        {
            if (k.Zeilen.Count == 0) liste.Add((k.KopfText, k.Leer.Text));
            foreach (var z in k.Zeilen)
                liste.Add((k.KopfText, z switch
                {
                    HeuteEintrag e => e.AccessibleName ?? e.Text1.Text,
                    CheckBox c => "☐ " + c.Text,
                    _ => z.Text,
                }));
        }
        foreach (var z in zeilen)
            liste.Add((listenKopf, $"{z.ZimmerText}\t{z.Bewohner.Name}\t{z.Bezugspflege?.Kuerzel ?? ""}\t{z.Wo}\t{z.HinweisText}"));
        return liste;
    }

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Übersicht" };
        dokument.DefaultPageSettings.Landscape = true;
        dokument.BeginPrint += (_, _) => naechster = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechster = SeiteZeichnen(e.Graphics!, naechster);
            e.HasMorePages = naechster < Zeilen().Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 15f, rechts = 282f, unten = 195f;
        float y = 12f;
        using var titel = new Font("Segoe UI Semibold", 13F);
        using var fett = new Font("Segoe UI Semibold", 9.5F);
        using var normal = new Font("Segoe UI", 9F);
        using var klein = new Font("Segoe UI", 7.5F);
        using var linie = new Pen(Color.Gray, 0.2f);
        var alle = Zeilen();
        g.DrawString("Übersicht – " + kopf, titel, Brushes.Black, links, y);
        y += 7f;
        g.DrawString($"Gedruckt {BerichtRegeln.ZeitText(jetzt)} · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y);
        y += 5f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 2f;
        float[] spalten = [0f, 22f, 70f, 88f, 150f];   // Zimmer, Name, Bezug, Wo, Hinweise (mm ab links)
        string? letzter = null;
        int i = start;
        for (; i < alle.Count; i++)
        {
            var (abschnitt, text) = alle[i];
            bool tabelle = abschnitt == listenKopf;
            float hoehe = (tabelle ? 4.5f : g.MeasureString(text, normal, new SizeF(rechts - links - 4f, 1000f)).Height + 0.5f) + (abschnitt != letzter ? 7f : 0f);
            if (y + hoehe > unten && i > start) break;
            if (abschnitt != letzter)
            {
                y += 1.5f;
                g.DrawString(abschnitt, fett, Brushes.Black, links, y);
                y += 5.5f;
                letzter = abschnitt;
            }
            if (tabelle)
            {
                var teile = text.Split('\t');
                for (int s = 0; s < teile.Length && s < spalten.Length; s++)
                {
                    float x = links + spalten[s], bis = s + 1 < spalten.Length ? links + spalten[s + 1] - 1f : rechts;
                    g.DrawString(teile[s], normal, Brushes.Black, new RectangleF(x, y, bis - x, 4.5f), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                }
                y += 4.5f;
            }
            else
            {
                g.DrawString(text, normal, Brushes.Black, new RectangleF(links + 4f, y, rechts - links - 4f, 1000f));
                y += g.MeasureString(text, normal, new SizeF(rechts - links - 4f, 1000f)).Height + 0.5f;
            }
        }
        return i;
    }
}
