using System.Drawing.Printing;
using System.Globalization;

namespace Dokusystem.Modul.Ausgangsliste;

// Die druckbare Tagesliste (übernommen aus der Ausgangsliste): A4 quer, je Bewohner eine Zeile mit
// Name, Ausgang (oder Grund des Status) und vier Feldern „von | bis | Hz.“ zum Ausfüllen von Hand.
// Wer heute Küchendienst hat, trägt die Marke „Küche“.
public sealed class Druckliste
{
    private const int HandschriftGruppen = 4;   // wie oft „von | bis | Hz.“ nebeneinander passt
    private const float Links = 12f;
    private const float Rechts = 285f;
    private const float Oben = 12f;
    private const float Unten = 188f;

    private readonly List<Platz> bewohner;
    private readonly HashSet<string> kuecheZimmer;
    private readonly string kuecheText;
    private readonly DateTime heute;

    public Druckliste(IEnumerable<Platz> sortiert, DateTime heute, HashSet<string>? kuecheZimmer = null, string kuecheText = "")
    {
        bewohner = sortiert.ToList();
        this.heute = heute.Date;
        this.kuecheZimmer = kuecheZimmer ?? [];
        this.kuecheText = kuecheText;
    }

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Ausgangsliste " + heute.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) };
        dokument.DefaultPageSettings.Landscape = true;   // A4 quer
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        using var stiftDuenn = new Pen(Color.Black, 0.15f);
        using var stiftDick = new Pen(Color.Black, 0.45f);
        using var schriftTitel = new Font("Segoe UI Semibold", 14F);
        using var schriftKopf = new Font("Segoe UI Semibold", 9F);
        using var schriftZelle = new Font("Segoe UI", 10F);
        using var schriftKlein = new Font("Segoe UI", 7F);
        using var linksMitte = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,   // zu lange Texte enden mit „…“
            FormatFlags = StringFormatFlags.NoWrap,
        };
        using var zentriert = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        // ----- Kopf: Titel links, Datum rechts, Küchendienst in der Mitte -----
        g.DrawString("Ausgangsliste", schriftTitel, Brushes.Black, Links, Oben);
        string datum = heute.ToString("dddd, dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
        float datumBreite = g.MeasureString(datum, schriftTitel).Width;
        g.DrawString(datum, schriftTitel, Brushes.Black, Rechts - datumBreite, Oben);
        if (kuecheText != "")
        {
            using var schriftKueche = new Font("Segoe UI Semibold", 10F);
            string kueche = $"Küchendienst heute: {kuecheText}";
            float kuecheBreite = g.MeasureString(kueche, schriftKueche).Width;
            g.DrawString(kueche, schriftKueche, Brushes.Black, (Links + Rechts - kuecheBreite) / 2, Oben + 1.5f);
        }

        // ----- Spalten: Bewohner | Ausgang | 4 × (von | bis | Hz.) -----
        float tabelleOben = Oben + 11f;
        const float breiteName = 55f, breiteAusgang = 38f;
        float breiteGruppe = (Rechts - Links - breiteName - breiteAusgang) / HandschriftGruppen;
        var spaltenX = new List<float> { Links, Links + breiteName, Links + breiteName + breiteAusgang };
        for (int i = 1; i <= HandschriftGruppen; i++)
        {
            float gruppeLinks = Links + breiteName + breiteAusgang + (i - 1) * breiteGruppe;
            spaltenX.Add(gruppeLinks + breiteGruppe * 0.38f);
            spaltenX.Add(gruppeLinks + breiteGruppe * 0.76f);
            spaltenX.Add(gruppeLinks + breiteGruppe);
        }
        const float kopfHoehe = 8f;
        float zeilenHoehe = Math.Min(14f, (Unten - tabelleOben - kopfHoehe) / Math.Max(1, bewohner.Count));
        float tabelleUnten = tabelleOben + kopfHoehe + bewohner.Count * zeilenHoehe;

        g.DrawString("Bewohner", schriftKopf, Brushes.Black, new RectangleF(Links + 1.5f, tabelleOben, breiteName - 2f, kopfHoehe), linksMitte);
        g.DrawString("Ausgang", schriftKopf, Brushes.Black, new RectangleF(Links + breiteName + 1.5f, tabelleOben, breiteAusgang - 2f, kopfHoehe), linksMitte);
        string[] gruppenTitel = ["von", "bis", "Hz."];
        for (int i = 0; i < HandschriftGruppen; i++)
        {
            float gruppeLinks = Links + breiteName + breiteAusgang + i * breiteGruppe;
            float[] teile = [0f, breiteGruppe * 0.38f, breiteGruppe * 0.76f, breiteGruppe];
            for (int t = 0; t < 3; t++)
                g.DrawString(gruppenTitel[t], schriftKopf, Brushes.Black,
                    new RectangleF(gruppeLinks + teile[t], tabelleOben, teile[t + 1] - teile[t], kopfHoehe), zentriert);
        }

        for (int i = 0; i < bewohner.Count; i++)
        {
            float y = tabelleOben + kopfHoehe + i * zeilenHoehe;
            float nameBreite = breiteName - 2f;
            if (kuecheZimmer.Contains(bewohner[i].Zimmernummer) && bewohner[i].Anwesend)
            {
                const string marke = "Küche";
                SizeF groesse = g.MeasureString(marke, schriftKopf);
                var feld = new RectangleF(Links + breiteName - groesse.Width - 3f, y + (zeilenHoehe - groesse.Height) / 2, groesse.Width + 1.5f, groesse.Height);
                g.DrawRectangle(stiftDick, feld.X, feld.Y, feld.Width, feld.Height);
                g.DrawString(marke, schriftKopf, Brushes.Black, feld, zentriert);
                nameBreite -= feld.Width + 2f;
            }
            g.DrawString(bewohner[i].Bewohnername, schriftZelle, Brushes.Black, new RectangleF(Links + 1.5f, y, nameBreite, zeilenHoehe), linksMitte);
            g.DrawString(bewohner[i].AusgangAnzeige, schriftZelle, Brushes.Black, new RectangleF(Links + breiteName + 1.5f, y, breiteAusgang - 2f, zeilenHoehe), linksMitte);
        }

        // ----- Linien -----
        g.DrawLine(stiftDick, Links, tabelleOben, Rechts, tabelleOben);
        g.DrawLine(stiftDick, Links, tabelleOben + kopfHoehe, Rechts, tabelleOben + kopfHoehe);
        for (int i = 1; i < bewohner.Count; i++)
        {
            float y = tabelleOben + kopfHoehe + i * zeilenHoehe;
            g.DrawLine(stiftDuenn, Links, y, Rechts, y);
        }
        g.DrawLine(stiftDick, Links, tabelleUnten, Rechts, tabelleUnten);
        g.DrawLine(stiftDick, Links, tabelleOben, Links, tabelleUnten);
        g.DrawLine(stiftDick, Rechts, tabelleOben, Rechts, tabelleUnten);
        for (int i = 0; i < spaltenX.Count; i++)
        {
            bool gruppengrenze = i <= 2 || (i - 2) % 3 == 0;
            if (spaltenX[i] > Links && spaltenX[i] < Rechts)
                g.DrawLine(gruppengrenze ? stiftDick : stiftDuenn, spaltenX[i], tabelleOben, spaltenX[i], tabelleUnten);
        }
        g.DrawString($"Hz. = Handzeichen  ·  Ausgefüllten Bogen 3 Monate aufbewahren  ·  {Stil.DemoHinweis}",
            schriftKlein, Brushes.Black, Links, tabelleUnten + 3f);
    }
}
