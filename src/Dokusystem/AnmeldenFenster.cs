using Dokusystem.Kern;

namespace Dokusystem;

// Anmelden mit dem Kürzel (Bauplan Schritt 19). Demo: ohne Passwort – das Fenster ist so gebaut,
// dass ein Passwortfeld später darunter Platz hat. Beim Start Pflicht; oben rechts im Kopf als
// „Abmelden / Benutzer wechseln“.
public sealed class AnmeldenFenster : Form
{
    private readonly Speicher speicher;

    public TextBox Kuerzel { get; } = new() { Width = 120, MaxLength = 6, CharacterCasing = CharacterCasing.Upper, Font = Stil.KarteName };
    public ListBox DemoListe { get; } = new() { Width = 440, Height = 150, IntegralHeight = false, Font = Stil.Standard };
    public Label Fehler { get; } = new()
    {
        AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche,
        Padding = new Padding(Stil.AbstandKlein), Visible = false,
    };
    public Button AnmeldenKnopf { get; } = Stil.HauptKnopf("Anmelden");
    public Button AbmeldenKnopf { get; } = Stil.NebenKnopf("Abmelden");
    public Mitarbeiter? Angemeldet { get; private set; }

    // wechsel: aus dem laufenden Programm (mit „Abmelden“); sonst beim Start („Beenden“ statt „Abbrechen“).
    public AnmeldenFenster(Speicher speicher, string vorschlag, bool wechsel)
    {
        this.speicher = speicher;
        Text = (wechsel ? "Abmelden / Benutzer wechseln" : "Anmelden") + $" ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = !wechsel;
        StartPosition = wechsel ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label { Text = "Dokumentationssystem", AutoSize = true, Font = Stil.Ueberschrift, ForeColor = Stil.TextNormal });
        zeile(null, new Label
        {
            Text = "Mit dem eigenen Kürzel anmelden. In der Demo ohne Passwort – alles, was du tust, steht mit deinem Kürzel im Programm.",
            AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.TextMittel,
        });
        Kuerzel.Text = vorschlag;
        zeile("Kürzel", Kuerzel);
        zeile(null, Fehler);
        zeile(null, new Label { Text = "DEMO: AUSGEDACHTE MITARBEITER (KLICK ÜBERNIMMT DAS KÜRZEL)", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandMittel, 0, 0) });
        var aktive = speicher.AlleMitarbeiter().Where(m => m.Aktiv).ToList();
        foreach (var m in aktive)
            DemoListe.Items.Add($"{m.Kuerzel} – {m.Name} · {string.Join(", ", m.Positionen)}{(m.Fachkraft ? " · Fachkraft" : "")}");
        DemoListe.SelectedIndexChanged += (_, _) =>
        {
            if (DemoListe.SelectedIndex >= 0) Kuerzel.Text = aktive[DemoListe.SelectedIndex].Kuerzel;
        };
        DemoListe.DoubleClick += (_, _) => Anmelden();
        zeile(null, DemoListe);

        AnmeldenKnopf.Click += (_, _) => Anmelden();
        AbmeldenKnopf.Click += (_, _) => { DialogResult = DialogResult.Abort; Close(); };
        var abbrechen = Stil.NebenKnopf(wechsel ? "Abbrechen" : "Beenden");
        abbrechen.DialogResult = DialogResult.Cancel;
        foreach (var b in new[] { AnmeldenKnopf, AbmeldenKnopf, abbrechen }) { b.AutoSize = true; b.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0); }
        AbmeldenKnopf.Visible = wechsel && vorschlag.Length > 0;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([AnmeldenKnopf, AbmeldenKnopf, abbrechen]);
        zeile(null, knoepfe);
        Controls.Add(raster);
        AcceptButton = AnmeldenKnopf;
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Anmelden()
    {
        try
        {
            Angemeldet = speicher.Anmelden(Kuerzel.Text);
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
