using DokuLine.Kern;

namespace DokuLine;

// Neue Nachricht (an Personen und/oder Teams, Betreff, Text, optional Bewohner) oder Antwort in
// einem Verlauf (dann nur der Text; sie geht an alle Beteiligten außer mir).
public sealed class NachrichtFenster : Form
{
    private readonly Speicher speicher;
    private readonly Kollege ich;
    private readonly Verlauf? verlauf;
    private readonly List<Empfaenger> ziele = [];
    private readonly List<Bewohner> bewohner;

    public CheckedListBox An { get; } = new() { Width = 420, Height = 150, CheckOnClick = true, IntegralHeight = false };
    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    public TextBox Betreff { get; } = new() { Width = 420 };
    public TextBox Inhalt { get; } = new() { Multiline = true, Width = 420, Height = 120, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Nachricht? Gesendet { get; private set; }
    public IReadOnlyList<Empfaenger> AntwortAn { get; } = [];

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public NachrichtFenster(Speicher speicher, Kollege ich, long? bewohnerId, Verlauf? verlauf)
    {
        this.speicher = speicher;
        this.ich = ich;
        this.verlauf = verlauf;
        bewohner = speicher.AlleBewohner();
        var kollegen = speicher.Kollegen().ToDictionary(k => k.Kuerzel);
        Text = (verlauf is null ? "Neue Nachricht" : "Antworten") + $" ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        void Zeile(string? beschriftung, Control feld)
        {
            if (beschriftung is null) { raster.Controls.Add(feld); raster.SetColumnSpan(feld, 2); return; }
            raster.Controls.Add(new Label { Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein) });
            feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            raster.Controls.Add(feld);
        }
        Zeile(null, new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Zeile("Von", new Label { Text = ich.Anzeige, AutoSize = true, Font = Stil.Fett, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) });
        if (verlauf is null)
        {
            foreach (var team in speicher.Teams())
            {
                ziele.Add(Empfaenger.Team(team));
                An.Items.Add($"Team {team}");
            }
            foreach (var k in kollegen.Values.Where(k => k.Kuerzel != ich.Kuerzel).OrderBy(k => k.Nachname, StringComparer.CurrentCulture))
            {
                ziele.Add(Empfaenger.Person(k.Kuerzel));
                An.Items.Add($"{k.Anzeige} – {string.Join(", ", k.Teams)}");
            }
            Zeile("An", An);
            BewohnerWahl.Items.Add("– kein Bewohner –");
            BewohnerWahl.Items.AddRange(bewohner.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
            BewohnerWahl.SelectedIndex = bewohnerId is long id ? bewohner.FindIndex(b => b.Id == id) + 1 : 0;
            Zeile("Bezug zu", BewohnerWahl);
            Betreff.MaxLength = NachrichtenRegeln.BetreffLaenge;
            Zeile("Betreff", Betreff);
        }
        else
        {
            AntwortAn = NachrichtenRegeln.AntwortAn(verlauf.Nachrichten, ich.Kuerzel);
            Zeile("Betreff", new Label { Text = verlauf.Betreff, AutoSize = true, Font = Stil.Fett, UseMnemonic = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) });
            Zeile("An", new Label
            {
                Text = NachrichtenRegeln.EmpfaengerText(AntwortAn, kollegen), AutoSize = true, MaximumSize = new Size(420, 0),
                ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, 0, 0),
            });
        }
        Zeile("Text", Inhalt);
        Zeile(null, new Label
        {
            Text = "Bleibt im Programm – kein E-Mail-Versand. Nachrichten werden nicht gelöscht, nur archiviert.",
            AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        Zeile(null, Fehler);
        var senden = Stil.HauptKnopf("Senden");
        senden.Click += (_, _) => Senden();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        senden.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([senden, abbrechen]);
        Zeile(null, knoepfe);
        Controls.Add(raster);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    // Empfänger nach Anzeigetext ankreuzen (für Tests und Vorbelegung).
    public void Ankreuzen(string text)
    {
        int i = An.Items.IndexOf(text);
        if (i < 0) i = An.Items.Cast<string>().ToList().FindIndex(x => x.StartsWith(text));
        An.SetItemChecked(i, true);
    }

    public bool Senden()
    {
        try
        {
            Gesendet = speicher.NachrichtSenden(new Nachricht
            {
                VerlaufId = verlauf?.Id,
                Von = ich.Kuerzel,
                An = verlauf is null ? An.CheckedIndices.Cast<int>().Select(i => ziele[i]).ToList() : AntwortAn,
                Betreff = verlauf is null ? Betreff.Text : "",
                Text = Inhalt.Text,
                Gesendet = Jetzt(),
                BewohnerId = verlauf is null && BewohnerWahl.SelectedIndex > 0 ? bewohner[BewohnerWahl.SelectedIndex - 1].Id : null,
            });
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Kleiner Hinweis über den Bewohnerkarten: ungelesene Nachrichten des angemeldeten Benutzers.
public sealed class NachrichtenHinweis : KartenFlaeche
{
    private readonly Label text = new() { AutoSize = true, BackColor = Stil.Flaeche, ForeColor = Stil.TextNormal, Font = Stil.Fett };
    public Button OeffnenKnopf { get; } = Stil.NebenKnopf("Öffnen …");
    public string Text2 => text.Text;

    public NachrichtenHinweis()
    {
        Controls.Add(text);
        Controls.Add(OeffnenKnopf);
    }

    public void Setzen(int ungelesen, string? name)
    {
        text.Text = ungelesen == 1 ? $"1 ungelesene Nachricht für {name}" : $"{ungelesen} ungelesene Nachrichten für {name}";
        AccessibleName = text.Text;
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandMittel);
        OeffnenKnopf.Location = new Point(Width - innen - OeffnenKnopf.Width, innen);
        text.Location = new Point(innen, innen + (OeffnenKnopf.Height - text.Height) / 2);
        int hoehe = OeffnenKnopf.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Farbiger Balken links (Farbe der Übergabe-/Nachrichten-Marken), zusätzlich zum Text.
        using var balken = new SolidBrush(Stil.Unterwegs.Text);
        e.Graphics.FillRectangle(balken, Stil.Px(this, 2), Stil.Px(this, Stil.RadiusKarte), Stil.Px(this, 4), Height - 2 * Stil.Px(this, Stil.RadiusKarte));
    }
}
