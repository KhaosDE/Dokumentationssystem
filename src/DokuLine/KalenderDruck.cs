using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Ausdruck Monat (A4 quer, Raster) oder Woche (A4 hoch, Liste je Tag).
public sealed class KalenderDruck(KalenderAnsicht ansicht, DateOnly tag, IReadOnlyList<KalenderTermin> termine, IReadOnlyDictionary<long, string> namen, string? nurFuer)
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Kalender" };
        dokument.DefaultPageSettings.Landscape = ansicht == KalenderAnsicht.Monat;
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    private string Zeile(KalenderTermin k, DateOnly d)
    {
        var f = k.Aktuell;
        string zeit = f.Ganztags || DateOnly.FromDateTime(f.Beginn) != d ? "" : $"{f.Beginn:HH\\:mm} ";
        string wer = f.Bewohner.Count > 0 ? " – " + string.Join(", ", f.Bewohner.Select(b => namen.GetValueOrDefault(b, "?"))) : "";
        return zeit + (f.Art == KalenderArt.Heimfahrt ? "Heimfahrt" : f.Art == KalenderArt.Ausflug ? $"Ausflug {f.Titel}" : f.Titel) + wer;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 9F);
        using var normal = new Font("Segoe UI", 9F);
        using var klein = new Font("Segoe UI", 7.5F);
        using var linie = new Pen(Color.Gray, 0.2f);
        string ueber = $"Kalender – {KalenderRegeln.ZeitraumText(ansicht, tag)}" + (nurFuer is null ? "" : $" – {nurFuer}");
        if (ansicht == KalenderAnsicht.Monat)
        {
            const float links = 10f, oben = 10f, breite = 277f, hoehe = 190f;
            g.DrawString(ueber, titel, Brushes.Black, links, oben);
            g.DrawString(KopfBereich.DemoHinweis, klein, Brushes.DimGray, links, oben + 8f);
            var erster = WochenplanRegeln.Montag(new DateOnly(tag.Year, tag.Month, 1));
            float y0 = oben + 14f, spalte = breite / 7f, zeile = (hoehe - 14f - 6f) / 6f;
            for (int i = 0; i < 7; i++) g.DrawString(De.DateTimeFormat.GetDayName(TherapieRegeln.Woche[i]), fett, Brushes.Black, links + i * spalte + 1f, y0);
            for (int w = 0; w < 6; w++)
                for (int i = 0; i < 7; i++)
                {
                    var d = erster.AddDays(w * 7 + i);
                    float x = links + i * spalte, y = y0 + 6f + w * zeile;
                    g.DrawRectangle(linie, x, y, spalte, zeile);
                    g.DrawString(d.Day.ToString(), d.Month == tag.Month ? fett : normal, d.Month == tag.Month ? Brushes.Black : Brushes.Gray, x + 1f, y + 0.5f);
                    float zy = y + 4.5f;
                    foreach (var k in termine.Where(k => k.Am(d)))
                    {
                        if (zy > y + zeile - 3.5f) { g.DrawString("…", klein, Brushes.Black, x + 1f, zy - 1f); break; }
                        g.DrawString(Zeile(k, d), klein, Brushes.Black, new RectangleF(x + 1f, zy, spalte - 2f, 3.6f));
                        zy += 3.4f;
                    }
                }
        }
        else
        {
            const float links = 20f, oben = 15f;
            g.DrawString(ueber, titel, Brushes.Black, links, oben);
            g.DrawString(KopfBereich.DemoHinweis, klein, Brushes.DimGray, links, oben + 8f);
            float y = oben + 15f;
            var montag = WochenplanRegeln.Montag(tag);
            for (int i = 0; i < 7; i++)
            {
                var d = montag.AddDays(i);
                g.DrawString(d.ToString("dddd, dd.MM.", De), fett, Brushes.Black, links, y);
                y += 5.5f;
                var heute = termine.Where(k => k.Am(d)).ToList();
                if (heute.Count == 0) { g.DrawString("–", normal, Brushes.Gray, links + 4f, y); y += 5f; }
                foreach (var k in heute)
                {
                    var r = new RectangleF(links + 4f, y, 166f, 20f);
                    var groesse = g.MeasureString(Zeile(k, d), normal, (int)r.Width);
                    g.DrawString(Zeile(k, d), normal, Brushes.Black, r);
                    y += groesse.Height + 1f;
                }
                g.DrawLine(linie, links, y + 1f, 190f, y + 1f);
                y += 4f;
            }
        }
    }
}

// Teilnehmerliste eines Ausflugs (A4 hoch) zum Abhaken.
public sealed class AusflugDruck(KalenderFassung f, IReadOnlyList<Bewohner> teilnehmer)
{
    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = $"Teilnehmerliste {f.Titel}" };
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f;
        float y = 15f;
        using var titel = new Font("Segoe UI Semibold", 16F);
        using var normal = new Font("Segoe UI", 12F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        g.DrawString($"Ausflug: {f.Titel}", titel, Brushes.Black, links, y);
        y += 10f;
        foreach (var z in new[] { KalenderRegeln.ZeitText(f), f.Treffpunkt.Length > 0 ? $"Treffpunkt: {f.Treffpunkt}" : "", f.Begleitung.Length > 0 ? $"Begleitung: {f.Begleitung}" : "", f.Hinweis })
        {
            if (z.Length == 0) continue;
            g.DrawString(z, normal, Brushes.Black, links, y);
            y += 7f;
        }
        y += 4f;
        g.DrawString("Teilnehmer", normal, Brushes.Black, links, y);
        y += 8f;
        foreach (var b in teilnehmer)
        {
            g.DrawString("☐", normal, Brushes.Black, links, y);
            g.DrawString($"{b.Name}, Zi. {b.Zimmer}", normal, Brushes.Black, links + 8f, y);
            y += 8f;
            g.DrawLine(linie, links, y - 1f, 190f, y - 1f);
        }
        g.DrawString($"{teilnehmer.Count} Teilnehmer · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y + 4f);
    }
}
