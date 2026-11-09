using DokuLine.Kern;

namespace DokuLine;

// Eine Einstellung als Karte: Name, ein Satz Erklärung, rechts (oder darunter) das Bedienelement.
// Ohne Recht gesperrt (Knöpfe grau mit Grund, Felder nicht änderbar) und mit dem Satz darunter.
public sealed class EinstellungKarte : KartenFlaeche
{
    private readonly Label name = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label erklaerung = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
    private readonly Label sperre = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(2), Visible = false };
    private readonly bool darunter;
    private bool hervorgehoben;

    public string Gruppe { get; }
    public string Titel { get; }
    public string Erklaerung { get; }
    public Control Bedienung { get; }
    public string? Funktion { get; init; }   // Recht, ohne das die Karte ausgeblendet ist
    public string? Sperrgrund { get; private set; }

    // Ohne Recht ausgeblendet: das Recht der Karte fehlt, oder ihr einziger Knopf ist ohne Recht.
    public bool OhneRecht => RechteRegeln.FehltRecht(Sperrgrund) || Bedienung is Knopf { OhneRecht: true };

    public EinstellungKarte(string gruppe, string name, string erklaerung, Control bedienung, bool darunter)
    {
        Gruppe = gruppe;
        Titel = name;
        Erklaerung = erklaerung;
        Bedienung = bedienung;
        this.darunter = darunter;
        this.name.Text = name;
        this.erklaerung.Text = erklaerung;
        if (bedienung is not Button && bedienung is not Label && bedienung is not MarkenFeld) bedienung.BackColor = Stil.Flaeche;
        Controls.AddRange([this.name, this.erklaerung, sperre, bedienung]);
        AccessibleName = $"{gruppe}: {name}";
        Visible = false;
    }

    public bool Hervorgehoben
    {
        get => hervorgehoben;
        set { hervorgehoben = value; Invalidate(); }
    }

    public bool Passt(string text) =>
        Titel.Contains(text, StringComparison.CurrentCultureIgnoreCase) || Erklaerung.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
        Gruppe.Contains(text, StringComparison.CurrentCultureIgnoreCase);

    // Gesperrt (z. B. ohne Anmeldung): Knöpfe nennen den Grund, alles andere ist nicht änderbar.
    public void Sperren(string? grund)
    {
        Sperrgrund = grund;
        sperre.Text = grund ?? "";
        sperre.Visible = grund is not null;
        void Durch(Control c)
        {
            if (c is Button b) Stil.Sperren(b, grund);
            else if (c is CheckBox or NumericUpDown or ComboBox) c.Enabled = grund is null;
            else if (c is TextBox t) t.ReadOnly = grund is not null;
            foreach (Control kind in c.Controls) Durch(kind);
        }
        Durch(Bedienung);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Bedienung is null) return;   // Anordnung schon während des Aufbaus
        int innen = Stil.Px(this, Stil.AbstandGross), luecke = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, Width - 2 * innen);
        var bedien = Bedienung.GetPreferredSize(new Size(breite, 0));
        bool unten = darunter || bedien.Width > breite / 2;
        int textBreite = unten ? breite : Math.Max(100, breite - bedien.Width - luecke);
        int y = innen;
        foreach (var l in new[] { name, erklaerung, sperre }.Where(l => l != sperre || Sperrgrund is not null))   // Visible meldet false, solange die Seite verborgen ist
        {
            l.MaximumSize = new Size(textBreite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, 2);
        }
        if (unten)
        {
            Bedienung.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), bedien);
            y = Bedienung.Bottom;
        }
        else
        {
            Bedienung.Bounds = new Rectangle(new Point(innen + breite - bedien.Width, Math.Max(innen, innen + (y - innen - bedien.Height) / 2)), bedien);
            y = Math.Max(y, Bedienung.Bottom);
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!hervorgehoben) return;   // Sprungziel der Suche: Balken links
        using var pinsel = new SolidBrush(Stil.Zimmer.Text);
        e.Graphics.FillRectangle(pinsel, Stil.Px(this, 2), Stil.Px(this, Stil.RadiusKarte), Stil.Px(this, 4), Height - 2 * Stil.Px(this, Stil.RadiusKarte));
    }
}
