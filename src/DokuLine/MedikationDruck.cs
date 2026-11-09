using System.ComponentModel;
using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Ausdruck des Medikamentenplans „Was gilt am …“ je Bewohner (A4 hoch), mit dem Vermerk „Demo — nicht zum Stellen verwenden“.
public sealed class MedikationDruck(Bewohner bewohner, DateOnly tag, IReadOnlyList<Tagesstand> staende)
{
    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = $"Medikamentenplan {bewohner.Name}" };
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f;
        float y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var normal = new Font("Segoe UI", 10F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        using var rot = new SolidBrush(Color.FromArgb(163, 34, 26));
        g.DrawString($"Medikamentenplan – {bewohner.Name}, Zi. {bewohner.Zimmer}", titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Was gilt am {tag.ToString("dddd, dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))}", normal, Brushes.Black, links, y);
        y += 6f;
        g.DrawString($"{MedikationRegeln.DruckHinweis} · {KopfBereich.DemoHinweis}", fett, rot, links, y);
        y += 7f;
        float[] spalten = [links, links + 80f, links + 100f, links + 120f, links + 140f, links + 160f];
        void Kopf()
        {
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
            string[] k = ["Medikament, Stärke", "morgens", "mittags", "abends", "nachts"];
            for (int i = 0; i < k.Length; i++) g.DrawString(k[i], klein, Brushes.DimGray, spalten[i], y);
            y += 5f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
        }
        g.DrawString("Regelmäßig", fett, Brushes.Black, links, y);
        y += 6f;
        Kopf();
        foreach (var s in staende.Where(x => x.Sichtbar && !x.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            g.DrawString($"{v.Medikament} {v.Staerke}", normal, Brushes.Black, new RectangleF(spalten[0], y, 78f, 12f));
            if (s.Zustand == Zustand.Pausiert)
                g.DrawString(s.Text.ToUpperInvariant(), fett, rot, spalten[1], y);
            else
                for (int i = 0; i < 4; i++) g.DrawString(s.Dosis.Werte.ElementAt(i), normal, Brushes.Black, spalten[i + 1] + 6f, y);
            y += 6f;
            string zusatz = s.Text + (s.Naechstes is string n ? $" · {n}" : "") + $" · angeordnet von {v.Anordnung.Arzt} am {Datum.Text(v.Anordnung.Am)}";
            g.DrawString(zusatz, klein, Brushes.DimGray, spalten[0], y);
            y += 5f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
        }
        y += 4f;
        g.DrawString("Bei Bedarf", fett, Brushes.Black, links, y);
        y += 6f;
        if (!staende.Any(x => x.Sichtbar && x.Verordnung.Bedarf)) g.DrawString("Keine Bedarfsmedikation.", normal, Brushes.DimGray, links, y);
        foreach (var s in staende.Where(x => x.Sichtbar && x.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            g.DrawString($"{v.Medikament} {v.Staerke}: {v.BedarfAnlass}, Einzeldosis {v.Dosis.Morgens} {v.Einheit}" +
                         (v.BedarfHoechstens.Length > 0 ? $", höchstens {v.BedarfHoechstens}" : "") +
                         (s.Zustand == Zustand.Pausiert ? $" – {s.Text}" : ""), normal, Brushes.Black, new RectangleF(links, y, rechts - links, 12f));
            y += 6f;
        }
    }
}
