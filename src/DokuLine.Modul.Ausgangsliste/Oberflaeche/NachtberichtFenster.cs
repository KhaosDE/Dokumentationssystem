namespace DokuLine.Modul.Ausgangsliste;

// Nachtbericht (übernommen aus der Ausgangsliste): je Bewohner mit Ausgängen heute ein Satz zum
// Kopieren. Das Programm schreibt keinen Bericht selbst; der Satz wird kopiert und von Hand in den
// Bericht eingefügt.
public sealed class NachtberichtFenster : Form
{
    public List<(string Name, string Satz, Button Knopf)> Eintraege { get; } = [];
    public Button AlleKnopf { get; } = Stil.HauptKnopf("Alle kopieren");
    public string AlleText { get; }

    // Zum Testen ersetzbar: in die Zwischenablage.
    public Action<string> Kopieren { get; set; } = text => Clipboard.SetText(text);

    public NachtberichtFenster(List<Platz> plaetze, List<AusgangEintrag> heute)
    {
        Text = $"Nachtbericht – Ausgänge heute ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label
        {
            Text = "Je Bewohner einen Satz kopieren und im Bericht einfügen (Strg+V). Wer noch unterwegs ist, steht dabei.",
            AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextMittel,
        });
        foreach (var p in plaetze)
        {
            var eigene = heute.Where(e => e.BewohnerId == p.BewohnerId).ToList();
            if (eigene.Count == 0) continue;
            string satz = DigitalerAusgang.Berichtstext(p, eigene);
            var laufend = eigene.FirstOrDefault(e => e.Unterwegs);
            string anzeige = (satz.Length > 0 ? satz : "Noch kein Ausgang beendet.") +
                             (laufend is not null ? $" (noch unterwegs: {laufend.Zeitraum})" : "");
            var knopf = Stil.NebenKnopf("Kopieren");
            knopf.Enabled = satz.Length > 0;
            knopf.Click += (_, _) => { Kopieren(satz); knopf.Text = "Kopiert ✓"; };
            Eintraege.Add((p.Bewohnername, satz, knopf));
            zeile(null, new Label { Text = p.Bewohnername, AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) });
            zeile(null, new Label { Text = anzeige, AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextNormal });
            zeile(null, knopf);
        }
        if (Eintraege.Count == 0) zeile(null, new Label { Text = "Heute ist noch kein Ausgang erfasst.", AutoSize = true, ForeColor = Stil.TextLeise });
        AlleText = DigitalerAusgang.Nachtbericht(plaetze, heute);
        AlleKnopf.Enabled = AlleText.Length > 0;
        AlleKnopf.Click += (_, _) => { Kopieren(AlleText); AlleKnopf.Text = "Kopiert ✓"; };
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        CancelButton = schliessen;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        AlleKnopf.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        knoepfe.Controls.AddRange([schliessen, AlleKnopf]);
        zeile(null, knoepfe);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }
}
