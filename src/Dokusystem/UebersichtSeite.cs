using Dokusystem.Kern;

namespace Dokusystem;

// Startseite „Übersicht“: oben der Bereich „Heute“ (Bewohner, Abwesende, Termine des Tages,
// Änderungen in der Medikation), darunter je Bewohner eine Karte mit den Marken des Tages.
// Klick auf eine Karte öffnet die Akte. Baut sich nach Änderungen und um Mitternacht neu auf.
public sealed class UebersichtSeite : Panel
{
    private readonly Speicher speicher;
    private readonly KartenFlaeche heuteKarte = new();
    private readonly Label heuteTitel = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly Label termineTitel = new() { Text = "TERMINE HEUTE", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly Label medikationTitel = new() { Text = "MEDIKATION HEUTE", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 60_000 };
    private DateOnly gezeigtAm;

    public Label HeuteText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label Termine { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label Medikation { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public KartenRaster Raster { get; } = new();
    public List<BewohnerKarte> Karten { get; } = [];
    public event Action<long>? AkteGewuenscht;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public UebersichtSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        heuteKarte.Controls.AddRange([heuteTitel, HeuteText, termineTitel, Termine, medikationTitel, Medikation]);
        Controls.Add(heuteKarte);
        Controls.Add(Raster);
        // Immer aktuell: Datumswechsel um Mitternacht (jede Minute prüfen, nur wenn sichtbar neu bauen).
        uhr.Tick += (_, _) => { if (Visible && DateOnly.FromDateTime(Jetzt()) != gezeigtAm) Aktualisieren(); };
        uhr.Start();
    }

    public string KopfText { get; private set; } = "";

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        gezeigtAm = heute;
        int frist = Einstellungen.Laden().BeschlussFristTage;
        var alle = speicher.Heute(jetzt);
        Karten.Clear();
        int abwesend = 0;
        foreach (var h in alle)
        {
            var modul = Modulverwaltung.Marken(speicher, h.Bewohner, jetzt);
            var marken = UebersichtRegeln.Marken(h, heute, frist);
            // Abwesend nur einmal: Meldet ein Modul schon „abwesend“, entfällt die Heimfahrt-Marke.
            if (modul.Any(m => m.Art == MarkenArt.Abwesend)) marken.RemoveAll(m => m.Art == MarkenArt.Abwesend);
            marken.InsertRange(0, modul.Where(m => m.Art == MarkenArt.Abwesend));
            marken.AddRange(modul.Where(m => m.Art != MarkenArt.Abwesend));
            if (marken.Any(m => m.Art == MarkenArt.Abwesend)) abwesend++;
            var karte = new BewohnerKarte(h.Bewohner, heute, marken);
            karte.Gewaehlt += b => AkteGewuenscht?.Invoke(b.Id!.Value);
            Karten.Add(karte);
        }
        Raster.KartenSetzen(Karten);
        heuteTitel.Text = $"HEUTE · {heute.ToString("dddd, dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE")).ToUpperInvariant()}";
        HeuteText.Text = UebersichtRegeln.HeuteText(alle, abwesend);
        var termine = UebersichtRegeln.TerminZeilen(alle);
        Termine.Text = termine.Count == 0 ? "Keine Termine heute." : string.Join(Environment.NewLine, termine);
        var medikation = UebersichtRegeln.MedikationZeilen(alle);
        Medikation.Text = medikation.Count == 0 ? "Keine Änderungen heute." : string.Join(Environment.NewLine, medikation);
        KopfText = $"{alle.Count} Bewohner · Stand {jetzt:HH:mm} Uhr";
        PerformLayout();
        Geaendert?.Invoke();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        // Oben der Bereich „Heute“, darunter das Kartenraster mit eigener Bildlaufleiste.
        int rand = Stil.Px(this, Stil.AbstandGross), innen = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(300, ClientSize.Width - 2 * rand);
        int y = rand;
        heuteTitel.Location = new Point(innen, innen);
        HeuteText.MaximumSize = new Size(breite - 2 * innen, 0);
        HeuteText.Location = new Point(innen, heuteTitel.Bottom + Stil.Px(this, Stil.AbstandMini));
        int spalte = (breite - 3 * innen) / 2;
        int oben = HeuteText.Bottom + Stil.Px(this, Stil.AbstandMittel);
        termineTitel.Location = new Point(innen, oben);
        Termine.MaximumSize = new Size(spalte, 0);
        Termine.Location = new Point(innen, termineTitel.Bottom + Stil.Px(this, Stil.AbstandMini));
        medikationTitel.Location = new Point(2 * innen + spalte, oben);
        Medikation.MaximumSize = new Size(spalte, 0);
        Medikation.Location = new Point(2 * innen + spalte, medikationTitel.Bottom + Stil.Px(this, Stil.AbstandMini));
        heuteKarte.Bounds = new Rectangle(rand, y, breite, Math.Max(Termine.Bottom, Medikation.Bottom) + innen);
        y = heuteKarte.Bottom + Stil.Px(this, Stil.AbstandKlein);
        Raster.Bounds = new Rectangle(0, y, ClientSize.Width, Math.Max(Stil.Px(this, 120), ClientSize.Height - y));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) uhr.Dispose();
        base.Dispose(disposing);
    }
}
