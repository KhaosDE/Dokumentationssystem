using System.Drawing.Printing;

namespace DokuLine;

// Öffnet Dialogfenster an einer Stelle. In Tests wird Zeigen ersetzt: Der Test füllt das
// Fenster aus und speichert, statt auf einen Menschen zu warten.
public static class Dialog
{
    public static Func<Form, IWin32Window?, DialogResult> Zeigen { get; set; } = (fenster, besitzer) =>
    {
        Stil.SymbolSetzen(fenster);   // 0.33.3: Dialoge zeigen sonst kein Programmsymbol
        return fenster.ShowDialog(besitzer);
    };
}

// Gemeinsame Druckvorschau für alle Ausdrucke (übernommen aus der Ausgangsliste): erst die
// Vorschau, dann drucken. „Drucken …“ öffnet den Windows-Druckdialog; dort lässt sich auch
// „Microsoft Print to PDF“ wählen. Vorschau und Druck nutzen dieselbe Zeichenroutine.
public static class Druckvorschau
{
    public static void Zeigen(IWin32Window? besitzer, Func<PrintDocument> dokumentBauen, string titel)
    {
        dokumentBauen = MitFusszeile(dokumentBauen);
        using var dokument = dokumentBauen();
        using var fenster = new Form
        {
            Text = $"{titel} ({Stil.DemoHinweis})",
            StartPosition = FormStartPosition.CenterParent,
            WindowState = FormWindowState.Maximized,
            MinimizeBox = false,
            ShowInTaskbar = false,
            BackColor = Stil.Fensterhintergrund,
            Font = Stil.Standard,
        };
        var vorschau = new PrintPreviewControl { Document = dokument, Dock = DockStyle.Fill, AutoZoom = true, UseAntiAlias = true };
        var unten = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(Stil.AbstandGross, Stil.AbstandMittel, Stil.AbstandGross, Stil.AbstandMittel),
            BackColor = Stil.Fensterhintergrund,
        };
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        var drucken = Stil.HauptKnopf("Drucken …");
        drucken.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        drucken.Click += (_, _) =>
        {
            if (Drucken(fenster, dokumentBauen)) fenster.DialogResult = DialogResult.OK;
        };
        unten.Controls.AddRange([schliessen, drucken]);
        fenster.Controls.Add(vorschau);
        fenster.Controls.Add(unten);
        fenster.AcceptButton = drucken;
        fenster.CancelButton = schliessen;
        Stil.DpiSkalierungEinschalten(fenster);
        Dialog.Zeigen(fenster, besitzer);
    }

    // Fußzeile auf jeder Seite (Schritt 26a1): Programmname und Demo-Hinweis, klein im unteren Rand.
    public const string FusszeileText = Stil.ProgrammName + " · " + Stil.DemoHinweis;

    public static Func<PrintDocument> MitFusszeile(Func<PrintDocument> dokumentBauen) => () =>
    {
        var dokument = dokumentBauen();
        dokument.PrintPage += (_, e) =>
        {
            if (e.Graphics is null) return;
            using var schrift = new Font("Segoe UI", 7.5f);
            var rand = e.MarginBounds;
            e.Graphics.DrawString(FusszeileText, schrift, Brushes.DimGray, rand.Left, rand.Bottom + 30);
        };
        return dokument;
    };

    // Echter Druck über den Windows-Druckdialog. Ein frisches Dokument, weil die Vorschau ihres
    // schon verbraucht hat. Gibt zurück, ob gedruckt wurde.
    private static bool Drucken(IWin32Window besitzer, Func<PrintDocument> dokumentBauen)
    {
        using var dokument = dokumentBauen();
        using var dialog = new PrintDialog { Document = dokument, UseEXDialog = true };
        if (dialog.ShowDialog(besitzer) != DialogResult.OK) return false;
        dokument.Print();
        return true;
    }
}
