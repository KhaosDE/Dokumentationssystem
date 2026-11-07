using System.Drawing.Printing;
using System.Globalization;

namespace Dokusystem.Modul.Ausgangsliste;

// Der Küchendienst-Plan eines Monats zum Aushängen (übernommen aus der Ausgangsliste): A4 hoch,
// Datum | Zimmer | Tausch, nur Zimmernummern (keine Namen, weil der Plan aushängt),
// Tausch farbig und fett, unten das Druckdatum. Der Monat passt immer auf eine Seite.
public sealed class KuechendienstDruck
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    // Feste Maße in Millimetern für A4 hoch (210 × 297).
    private const float Oben = 18f;
    private const float Unten = 280f;
    private const float BreiteDatum = 62f;
    private const float BreiteZimmer = 34f;
    private const float BreiteTausch = 34f;
    private const float Links = (210f - BreiteDatum - BreiteZimmer - BreiteTausch) / 2; // mittig

    private readonly DateTime monat;
    private readonly DateTime gedruckt;
    private readonly List<KuechendienstTag> tage;

    public KuechendienstDruck(DateTime monat, IEnumerable<KuechendienstTag> plan, DateTime gedruckt)
    {
        this.monat = new DateTime(monat.Year, monat.Month, 1);
        this.gedruckt = gedruckt;
        tage = plan.Where(t => t.Datum.Year == monat.Year && t.Datum.Month == monat.Month).OrderBy(t => t.Datum).ToList();
    }

    public string Titel => "Küchendienst – " + monat.ToString("MMMM yyyy", De);
    public bool HatTage => tage.Count > 0;
    public IReadOnlyList<KuechendienstTag> Tage => tage;

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = Titel };
        dokument.DefaultPageSettings.Landscape = false; // A4 hoch
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;

        using var stiftDuenn = new Pen(Color.Black, 0.15f);
        using var stiftDick = new Pen(Color.Black, 0.45f);
        using var schriftTitel = new Font("Segoe UI Semibold", 16F);
        using var schriftKopf = new Font("Segoe UI Semibold", 12F);
        using var schriftZelle = new Font("Segoe UI", 12F);
        using var schriftTausch = new Font("Segoe UI Semibold", 12F);
        using var schriftKlein = new Font("Segoe UI", 7.5F);
        using var tauschPinsel = new SolidBrush(Stil.Akzent);
        using var kopfPinsel = new SolidBrush(Color.FromArgb(220, 220, 220));
        using var zentriert = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        float rechts = Links + BreiteDatum + BreiteZimmer + BreiteTausch;
        float[] kanteX = [Links, Links + BreiteDatum, Links + BreiteDatum + BreiteZimmer, rechts];

        // ---- Titel und Demo-Hinweis, mittig ----
        g.DrawString(Titel, schriftTitel, Brushes.Black, new RectangleF(0, Oben, 210f, 10f), zentriert);
        g.DrawString(Stil.DemoHinweis, schriftKlein, Brushes.DimGray, new RectangleF(0, Oben + 10f, 210f, 5f), zentriert);

        // ---- Kopfzeile, grau hinterlegt ----
        float tabelleOben = Oben + 20f;
        const float kopfHoehe = 9f;
        g.FillRectangle(kopfPinsel, Links, tabelleOben, rechts - Links, kopfHoehe);
        string[] kopf = ["Datum", "Zimmer", "Tausch"];
        for (int s = 0; s < kopf.Length; s++)
            g.DrawString(kopf[s], schriftKopf, Brushes.Black,
                new RectangleF(kanteX[s], tabelleOben, kanteX[s + 1] - kanteX[s], kopfHoehe), zentriert);

        // ---- Tage ----
        float zeilenHoehe = Math.Min(9f, (Unten - tabelleOben - kopfHoehe) / Math.Max(1, tage.Count));
        for (int i = 0; i < tage.Count; i++)
        {
            float y = tabelleOben + kopfHoehe + i * zeilenHoehe;
            var tag = tage[i];
            g.DrawString(tag.Datum.ToString("ddd, dd.MM.yyyy", De), schriftZelle, Brushes.Black,
                new RectangleF(kanteX[0], y, BreiteDatum, zeilenHoehe), zentriert);
            g.DrawString(tag.Zimmer, schriftZelle, Brushes.Black,
                new RectangleF(kanteX[1], y, BreiteZimmer, zeilenHoehe), zentriert);
            g.DrawString(tag.Tausch, schriftTausch, tauschPinsel,
                new RectangleF(kanteX[2], y, BreiteTausch, zeilenHoehe), zentriert);
        }

        // ---- Linien ----
        float tabelleUnten = tabelleOben + kopfHoehe + tage.Count * zeilenHoehe;
        for (int i = 0; i <= tage.Count; i++)
        {
            float y = tabelleOben + kopfHoehe + i * zeilenHoehe;
            g.DrawLine(i == 0 || i == tage.Count ? stiftDick : stiftDuenn, Links, y, rechts, y);
        }
        g.DrawLine(stiftDick, Links, tabelleOben, rechts, tabelleOben);
        foreach (float x in kanteX)
            g.DrawLine(x == Links || x == rechts ? stiftDick : stiftDuenn, x, tabelleOben, x, tabelleUnten);

        // ---- Fußzeile ----
        g.DrawString($"Gedruckt: {gedruckt.ToString("dd.MM.yyyy HH:mm", De)}  ·  Tausch = ein anderes Zimmer übernimmt an diesem Tag",
            schriftKlein, Brushes.Black, Links, tabelleUnten + 3f);
    }
}
