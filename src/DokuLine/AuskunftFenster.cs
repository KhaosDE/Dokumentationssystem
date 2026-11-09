using DokuLine.Kern;

namespace DokuLine;

// „Auskunft prüfen …“ (Bauplan 35): Name eintippen → Antwort mit Zeichen, Wort und Farbe. Das Programm bewertet nichts
// rechtlich; es zeigt, was eingetragen ist. Darunter die ganze Auskunftsleiste.
public sealed class AuskunftFenster : Form
{
    private readonly AngabeFassung? fassung;
    private readonly Func<long?, string> namen;
    private readonly DateOnly heute;

    public TextBox Suche { get; } = new() { Width = 320, PlaceholderText = "Name oder Stelle, z. B. „Ahornblatt“ oder „Hausarzt“" };
    public MarkenFeld Antwort { get; } = new("Bitte einen Namen eingeben.", Stil.Zimmer);
    public AuskunftMarke? Ergebnis { get; private set; }

    public AuskunftFenster(Speicher speicher, Bewohner bewohner)
    {
        Fenster.Grundform(this, $"Auskunft prüfen: {bewohner.Name}");
        heute = DateOnly.FromDateTime(DateTime.Now);
        fassung = speicher.Angaben(bewohner.Id!.Value, heute).GetValueOrDefault(Angabe.Schweigepflicht);
        namen = speicher.PersonenNamen();
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Wer fragt?", Suche);
        zeile("Antwort", Antwort);
        var leiste = new FlowLayoutPanel { AutoSize = true, MaximumSize = new Size(560, 0), WrapContents = true, Margin = new Padding(0) };
        foreach (var m in SchweigepflichtRegeln.Leiste(fassung, namen, heute))
            leiste.Controls.Add(new MarkenFeld(m.Text, Stil.AuskunftFarbe(m.Art)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini), Durchgestrichen = m.Widerrufen });
        zeile("Eingetragen", leiste);
        zeile(null, Fenster.Hinweis("Das Programm bewertet nichts rechtlich; es zeigt, was in der Akte eingetragen ist. Im Zweifel: keine Auskunft."));
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.OK;
        CancelButton = schliessen;
        zeile(null, schliessen);
        Controls.Add(raster);
        Suche.TextChanged += (_, _) => Pruefen();
        if (fassung is null) Pruefen();
        Stil.DpiSkalierungEinschalten(this);
    }

    public AuskunftMarke Pruefen()
    {
        Ergebnis = SchweigepflichtRegeln.Pruefen(fassung, Suche.Text, namen, heute);
        Antwort.Text = Ergebnis.Text;
        Antwort.Marke = Stil.AuskunftFarbe(Ergebnis.Art);
        Antwort.Durchgestrichen = false;
        return Ergebnis;
    }
}
