using DokuLine.Kern;

namespace DokuLine;

// Die leise Zeile unter dem Textfeld (Bauplan 48): „„Arzttermin“ erkannt — Termin im Kalender?“ mit „Nach dem Speichern
// anlegen“ und „×“. Nie ein Fenster beim Schreiben; höchstens zwei Angebote; ein weggeklicktes kommt für diesen Text nicht
// wieder. Vorgemerkte Schritte öffnen nach dem Speichern das Zielformular – angelegt wird erst dort, vom Menschen.
public sealed class VorschlagLeiste : FlowLayoutPanel
{
    private readonly List<Schluesselwort> woerter;

    public HashSet<Folgeschritt> Weggeklickt { get; } = [];
    public List<Vorschlag> Vorgemerkt { get; } = [];
    public List<Vorschlag> Gezeigt { get; private set; } = [];
    public bool Aus { get; }
    // „Besonderes Vorkommnis“ ändert nur das Formular (Berichtsart) – sofort, auf Klick.
    public event Action<Vorschlag>? Sofort;

    public VorschlagLeiste(IEnumerable<Schluesselwort> woerter, bool aus)
    {
        this.woerter = [.. woerter];
        Aus = aus;
        AutoSize = true;
        WrapContents = true;
        FlowDirection = FlowDirection.TopDown;
        Margin = new Padding(0, 0, 0, Stil.AbstandKlein);
        Visible = false;
        AccessibleName = "Vorschläge zum nächsten Schritt";
    }

    public static bool SofortSchritt(Folgeschritt f) => f == Folgeschritt.Vorkommnis;

    private string letzterText = "";

    public void Pruefen(string text)
    {
        letzterText = text;
        Aufbauen(erzwingen: false);
    }

    private void Aufbauen(bool erzwingen)
    {
        if (Aus) return;
        var neu = SchluesselwortRegeln.Finden(letzterText, woerter, Weggeklickt);
        // Datum und Uhrzeit aus dem Text auch in den vorgemerkten Schritten auf Stand halten.
        for (int i = 0; i < Vorgemerkt.Count; i++)
            if (neu.FirstOrDefault(v => v.Folgeschritt == Vorgemerkt[i].Folgeschritt) is Vorschlag v) Vorgemerkt[i] = v;
        bool gleich = neu.Select(v => v.Folgeschritt).SequenceEqual(Gezeigt.Select(v => v.Folgeschritt));
        Gezeigt = neu;
        if (gleich && !erzwingen) return;
        SuspendLayout();
        foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); c.Dispose(); }
        foreach (var v in Gezeigt)
        {
            var reihe = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = BackColor };
            var text = new Label { Text = v.Text, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel, UseMnemonic = false, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) };
            bool vorgemerkt = Vorgemerkt.Any(x => x.Folgeschritt == v.Folgeschritt);
            var knopf = Stil.KleinerKnopf(SofortSchritt(v.Folgeschritt) ? "Kennzeichnen" : vorgemerkt ? "✓ vorgemerkt" : "Nach dem Speichern anlegen …");
            knopf.AccessibleName = $"{SchluesselwortRegeln.Name(v.Folgeschritt)} vormerken";
            knopf.Click += (_, _) => Annehmen(v.Folgeschritt);
            var weg = Stil.KleinerKnopf("×");
            weg.AccessibleName = $"Vorschlag „{SchluesselwortRegeln.Name(v.Folgeschritt)}“ wegklicken";
            weg.Click += (_, _) => Wegklicken(v.Folgeschritt);
            reihe.Controls.AddRange([text, knopf, weg]);
            Controls.Add(reihe);
        }
        Visible = Gezeigt.Count > 0;
        ResumeLayout();
        Parent?.PerformLayout();
    }

    public void Annehmen(Folgeschritt f)
    {
        if (Gezeigt.FirstOrDefault(v => v.Folgeschritt == f) is not Vorschlag v) return;
        if (SofortSchritt(f)) { Sofort?.Invoke(v); Wegklicken(f); return; }
        if (Vorgemerkt.All(x => x.Folgeschritt != f)) Vorgemerkt.Add(v);
        else Vorgemerkt.RemoveAll(x => x.Folgeschritt == f);   // zweiter Klick: nicht mehr vormerken
        Aufbauen(erzwingen: true);
    }

    public void Wegklicken(Folgeschritt f)
    {
        Weggeklickt.Add(f);
        Vorgemerkt.RemoveAll(x => x.Folgeschritt == f);
        Aufbauen(erzwingen: true);
    }
}
