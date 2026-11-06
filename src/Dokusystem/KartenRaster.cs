namespace Dokusystem;

// Der scrollbare Bereich mit den Bewohnerkarten (übernommen aus der Ausgangsliste). Er verteilt
// die Karten in so viele Spalten, wie in die Breite passen, und zieht sie auf die volle Breite.
public sealed class KartenRaster : Panel
{
    private const int Abstand = Stil.AbstandMittel;   // zwischen den Karten (logische Pixel)
    private const int Rand = Stil.AbstandGross;       // zum Rand des Bereichs

    public KartenRaster()
    {
        AutoScroll = true;
        DoubleBuffered = true;
        BackColor = Stil.Seite;
        Padding = new Padding(0);
    }

    private int S(int wert) => Stil.Px(this, wert);

    // Ersetzt alle Karten. Die alten werden freigegeben, damit keine Fenster-Handles liegen bleiben.
    public void KartenSetzen(IEnumerable<Control> karten)
    {
        SuspendLayout();
        var alte = Controls.Cast<Control>().ToList();
        Controls.Clear();
        foreach (var alt in alte) alt.Dispose();
        foreach (var karte in karten) Controls.Add(karte);
        ResumeLayout(false);
        Anordnen();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Anordnen();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Anordnen();
    }

    private void Anordnen()
    {
        if (Controls.Count == 0) return;
        int rand = S(Rand), abstand = S(Abstand);
        int minBreite = S(BewohnerKarte.Breite), hoehe = S(BewohnerKarte.Hoehe);

        int nutzbar = ClientSize.Width - 2 * rand;
        int spalten = Math.Max(1, (nutzbar + abstand) / (minBreite + abstand));
        int zeilen = (Controls.Count + spalten - 1) / spalten;
        if (2 * rand + zeilen * hoehe + (zeilen - 1) * abstand > ClientSize.Height && !VerticalScroll.Visible)
        {
            nutzbar -= SystemInformation.VerticalScrollBarWidth;
            spalten = Math.Max(1, (nutzbar + abstand) / (minBreite + abstand));
        }
        int breite = Math.Max(minBreite, (nutzbar - (spalten - 1) * abstand) / spalten);

        SuspendLayout();
        var scroll = AutoScrollPosition;
        int i = 0;
        foreach (Control karte in Controls)
        {
            int x = rand + i % spalten * (breite + abstand);
            int y = rand + i / spalten * (hoehe + abstand);
            karte.Bounds = new Rectangle(x + scroll.X, y + scroll.Y, breite, hoehe);
            i++;
        }
        // Platz unter der letzten Zeile, damit sie nicht am Rand klebt.
        AutoScrollMinSize = new Size(0, 2 * rand + zeilen * hoehe + (zeilen - 1) * abstand);
        ResumeLayout(true);
    }
}
