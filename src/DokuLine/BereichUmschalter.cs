using DokuLine.Kern;

namespace DokuLine;

// Oben in der Übersicht: „Wohnbereich: R1 · R2 · R3 …“ als große Umschaltknöpfe (nur die, die man wählen darf).
public sealed class BereichUmschalter : FlowLayoutPanel
{
    private readonly Label titel = new()
    {
        Text = "Wohnbereich:", AutoSize = true, Font = Stil.TagKopf, ForeColor = Stil.TextNormal, BackColor = Stil.Seite,
        Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMittel, 0), UseMnemonic = false,
    };

    public List<FilterKnopf> Knoepfe { get; } = [];
    public MarkenFeld Eingesprungen { get; } = new("eingesprungen", Stil.Hinweis) { Visible = false };
    public string Aktiv { get; private set; } = "";
    public event Action<string>? Gewaehlt;

    public BereichUmschalter()
    {
        AutoSize = true;
        WrapContents = true;
        BackColor = Stil.Seite;
        Eingesprungen.Margin = new Padding(Stil.AbstandMittel, Stil.AbstandKlein, 0, 0);
        Controls.Add(titel);
    }

    // bereiche: die erlaubten Knöpfe; ohne einen (nicht angemeldet) steht nur der gewählte Bereich da.
    public void Setzen(IReadOnlyList<string> bereiche, string aktiv, bool eingesprungen)
    {
        Aktiv = aktiv;
        IReadOnlyList<string> namen = bereiche.Count > 0 ? bereiche : [aktiv];
        if (!Knoepfe.Select(k => k.Text).SequenceEqual(namen))
        {
            SuspendLayout();
            foreach (var k in Knoepfe) { Controls.Remove(k); k.Dispose(); }
            Knoepfe.Clear();
            Controls.Remove(Eingesprungen);
            foreach (string name in namen)
            {
                var k = new FilterKnopf(name, mitZahl: false) { Font = Stil.TagKopf, AccessibleName = $"Wohnbereich {name}" };
                k.Size = k.GetPreferredSize(Size.Empty) + new Size(Stil.AbstandGross, Stil.AbstandKlein);
                k.Click += (_, _) => { if (bereiche.Count > 0 && Aktiv != name) Gewaehlt?.Invoke(name); };
                Knoepfe.Add(k);
                Controls.Add(k);
            }
            Controls.Add(Eingesprungen);
            ResumeLayout();
        }
        foreach (var k in Knoepfe) k.Aktiv = k.Text == aktiv;
        Eingesprungen.Visible = eingesprungen;
    }
}
