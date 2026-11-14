using DokuLine.Kern;

namespace DokuLine;

// In der Seitenleiste über „Übersicht“: der Wohnbereich, der überall gilt – als Auswahlliste mit den Bereichen, für die
// man zuständig ist. Mit nur einem Bereich steht der Name ohne Auswahl da; eingeklappt ein Knopf mit dem Kürzel, ein
// Klick öffnet die Liste. Fachregel: IDEEN.md „Wohnbereich als Auswahlliste in der Seitenleiste“.
public sealed class BereichsAuswahl : Panel
{
    public Label Titel { get; } = new()
    {
        Text = "Wohnbereich", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel, BackColor = Stil.Fensterhintergrund, UseMnemonic = false,
    };
    public ComboBox Liste { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Fett, AccessibleName = "Wohnbereich wählen" };
    public Label Einzig { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Fensterhintergrund, UseMnemonic = false };
    public Button Kurz { get; } = new() { FlatStyle = FlatStyle.Flat, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Fensterhintergrund, UseMnemonic = false };
    public ContextMenuStrip Menue { get; } = new();

    public IReadOnlyList<string> Bereiche { get; private set; } = [];
    public string Aktiv { get; private set; } = "";
    public event Action<string>? Gewaehlt;
    private bool eingeklappt, fuellen;

    public BereichsAuswahl()
    {
        BackColor = Stil.Fensterhintergrund;
        Kurz.FlatAppearance.BorderColor = Stil.KarteRand;
        Liste.SelectedIndexChanged += (_, _) => { if (!fuellen && Liste.SelectedItem is string b) Waehlen(b); };
        Kurz.Click += (_, _) => { if (Bereiche.Count > 1) Menue.Show(Kurz, new Point(Kurz.Width, 0)); };
        Controls.AddRange([Titel, Liste, Einzig, Kurz]);
        Layout += (_, _) => Anordnen();
    }

    public bool Eingeklappt
    {
        get => eingeklappt;
        set { eingeklappt = value; Sichtbarkeit(); PerformLayout(); }
    }

    // bereiche: die wählbaren (leer, solange niemand angemeldet ist: dann steht nur der gewählte da).
    public void Setzen(IReadOnlyList<string> bereiche, string aktiv)
    {
        Bereiche = bereiche;
        Aktiv = aktiv;
        fuellen = true;
        if (!Liste.Items.Cast<string>().SequenceEqual(bereiche))
        {
            Liste.Items.Clear();
            Liste.Items.AddRange([.. bereiche]);
            Menue.Items.Clear();
            foreach (string b in bereiche) Menue.Items.Add(new ToolStripMenuItem(b, null, (_, _) => Waehlen(b)) { AccessibleName = $"Wohnbereich {b}" });
        }
        Liste.SelectedItem = bereiche.Contains(aktiv) ? aktiv : null;
        fuellen = false;
        foreach (ToolStripMenuItem m in Menue.Items) m.Checked = m.Text == aktiv;
        Einzig.Text = aktiv;
        Kurz.Text = BereichswahlRegeln.Kuerzel(aktiv);
        Kurz.AccessibleName = $"Wohnbereich: {aktiv}";
        Sichtbarkeit();
        PerformLayout();
    }

    // Wie ein Klick in die Liste: nur erlaubte Bereiche, und nur ein Wechsel meldet sich.
    public void Waehlen(string bereich)
    {
        if (!Bereiche.Contains(bereich) || bereich == Aktiv) return;
        Gewaehlt?.Invoke(bereich);
    }

    // Höhe für die Seitenleiste: eingeklappt so hoch wie ein Eintrag, sonst Überschrift und Liste.
    public int Hoehe(int eintrag) => eingeklappt ? eintrag : Titel.PreferredHeight + Stil.Px(this, Stil.AbstandMini) + Liste.PreferredHeight;

    private void Sichtbarkeit()
    {
        bool auswahl = Bereiche.Count > 1;
        Titel.Visible = !eingeklappt;
        Liste.Visible = !eingeklappt && auswahl;
        Einzig.Visible = !eingeklappt && !auswahl;
        Kurz.Visible = eingeklappt;
    }

    private void Anordnen()
    {
        Titel.Location = new Point(Stil.Px(this, Stil.AbstandMini), 0);
        int unten = Titel.Bottom + Stil.Px(this, Stil.AbstandMini);
        Liste.Bounds = new Rectangle(0, unten, ClientSize.Width, Liste.PreferredHeight);
        Einzig.Location = new Point(Stil.Px(this, Stil.AbstandMini), unten + Math.Max(0, (Liste.PreferredHeight - Einzig.PreferredHeight) / 2));
        Kurz.Bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
    }
}
