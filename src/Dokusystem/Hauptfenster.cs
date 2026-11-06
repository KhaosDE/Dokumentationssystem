using Dokusystem.Kern;

namespace Dokusystem;

// Hauptfenster: Seitenleiste links (Navigation, unten Demo-Daten, Update-Häkchen, Version),
// rechts der Kopf mit dem dauerhaft sichtbaren Demo-Hinweis und darunter der aktive Bereich.
public sealed class Hauptfenster : Form
{
    private const int LeisteBreite = 232;

    private readonly Speicher speicher;
    private readonly Panel leiste = new();
    private readonly Label programmName = new();

    public KopfBereich Kopf { get; } = new() { Dock = DockStyle.Top };
    public BewohnerBereich Bewohner { get; }
    public List<SeitenleistenEintrag> Navigation { get; } = [];
    public CheckBox AutoUpdate { get; } = new();
    public Label VersionText { get; } = new();
    public Button DemoKnopf { get; } = Stil.NebenKnopf("Demo-Daten zurücksetzen …");

    public Hauptfenster(Speicher speicher)
    {
        this.speicher = speicher;
        Text = $"Dokumentationssystem – {KopfBereich.DemoHinweis}";
        BackColor = Stil.Seite;
        Font = Stil.Standard;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 560);
        Size = new Size(1180, 760);

        Bewohner = new BewohnerBereich(speicher, Kopf) { Dock = DockStyle.Fill };

        // ----- Seitenleiste -----
        leiste.Dock = DockStyle.Left;
        leiste.Width = LeisteBreite;
        leiste.BackColor = Stil.Fensterhintergrund;
        leiste.Padding = new Padding(Stil.AbstandMittel);
        leiste.Paint += (_, e) =>
        {
            using var linie = new Pen(Stil.Trennlinie);
            e.Graphics.DrawLine(linie, leiste.Width - 1, 0, leiste.Width - 1, leiste.Height);
        };

        programmName.Text = "Dokumentationssystem";
        programmName.Font = Stil.Fett;
        programmName.ForeColor = Stil.TextNormal;
        programmName.AutoSize = true;

        var bewohnerEintrag = new SeitenleistenEintrag("Bewohner") { Aktiv = true };
        bewohnerEintrag.Click += (_, _) => Bewohner.UebersichtZeigen();   // führt immer zur Übersicht
        Navigation.Add(bewohnerEintrag);

        DemoKnopf.AutoSize = false;
        DemoKnopf.Padding = new Padding(Stil.AbstandMini, 0, Stil.AbstandMini, 0);   // passt so in eine Zeile
        DemoKnopf.Click += (_, _) => DemoZuruecksetzen();
        AutoUpdate.Text = "Beim Start nach Updates suchen";
        AutoUpdate.Font = Stil.Klein;
        AutoUpdate.ForeColor = Stil.TextMittel;
        AutoUpdate.AutoSize = true;
        AutoUpdate.Checked = Einstellungen.Laden().AutoUpdate;
        AutoUpdate.CheckedChanged += (_, _) => Einstellungen.Speichern(new EinstellungsWerte(AutoUpdate.Checked));
        VersionText.Text = Programmversion.Text;
        VersionText.Font = Stil.Klein;
        VersionText.ForeColor = Stil.TextLeise;
        VersionText.AutoSize = true;

        leiste.Controls.Add(programmName);
        foreach (var eintrag in Navigation) leiste.Controls.Add(eintrag);
        leiste.Controls.AddRange([DemoKnopf, AutoUpdate, VersionText]);
        leiste.Layout += (_, _) => LeisteAnordnen();

        // ----- Inhalt rechts: Kopf oben, Bereich darunter -----
        var inhalt = new Panel { Dock = DockStyle.Fill, BackColor = Stil.Seite };
        inhalt.Controls.Add(Bewohner);
        inhalt.Controls.Add(Kopf);
        Bewohner.BringToFront();   // Fill wird zuletzt angeordnet und nimmt den Rest

        Controls.Add(inhalt);
        Controls.Add(leiste);
        inhalt.BringToFront();

        Bewohner.UebersichtZeigen();
    }

    private int S(int wert) => Stil.Px(this, wert);

    private void LeisteAnordnen()
    {
        int x = leiste.Padding.Left, breite = leiste.ClientSize.Width - leiste.Padding.Horizontal;
        int y = leiste.Padding.Top + S(Stil.AbstandKlein);
        programmName.Location = new Point(x + S(Stil.AbstandMini), y);
        y += programmName.PreferredHeight + S(Stil.AbstandGross);
        foreach (var eintrag in Navigation)
        {
            eintrag.Bounds = new Rectangle(x, y, breite, S(40));
            y += eintrag.Height + S(Stil.AbstandMini);
        }

        int unten = leiste.ClientSize.Height - leiste.Padding.Bottom;
        VersionText.Location = new Point(x + S(Stil.AbstandMini), unten - VersionText.PreferredHeight);
        unten = VersionText.Top - S(Stil.AbstandKlein);
        AutoUpdate.Location = new Point(x, unten - AutoUpdate.PreferredSize.Height);
        unten = AutoUpdate.Top - S(Stil.AbstandMittel);
        DemoKnopf.Bounds = new Rectangle(x, unten - S(Stil.KnopfHoehe), breite, S(Stil.KnopfHoehe));
    }

    // Alle Bewohner und Bilder weg, Demo-Daten neu laden – mit Rückfrage.
    public bool DemoZuruecksetzen(bool nachfragen = true)
    {
        if (nachfragen && MessageBox.Show(this,
                "Alle Änderungen und hinterlegten Bilder werden gelöscht und die ausgedachten Demo-Bewohner neu geladen.\n\nFortfahren?",
                "Demo-Daten zurücksetzen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            != DialogResult.Yes)
            return false;
        speicher.DemoZuruecksetzen();
        Bewohner.UebersichtZeigen();
        return true;
    }
}
