using DokuLine.Kern;

namespace DokuLine;

// Anmelden mit dem Kürzel. Demo: ohne Passwort – das Fenster ist so gebaut,
// dass ein Passwortfeld später darunter Platz hat. Beim Start Pflicht; nach dem Abmelden (F12) liegt es über
// dem leeren Hauptfenster – anmelden (auch mit anderem Kürzel) oder beenden.
public sealed class AnmeldenFenster : Form
{
    private readonly Speicher speicher;

    public TextBox Kuerzel { get; } = new() { Width = 120, MaxLength = 6, CharacterCasing = CharacterCasing.Upper, Font = Stil.KarteName };
    // Betrieb (Bauplan 37): mit eigenem Passwort; ein Einmalpasswort muss gleich geändert werden.
    public TextBox Passwort { get; } = new() { Width = 220, UseSystemPasswordChar = true, AccessibleName = "Passwort" };
    internal Func<string, bool> PasswortAendernFragen { get; set; }
    public ListBox DemoListe { get; } = new() { Width = 440, Height = 150, IntegralHeight = false, Font = Stil.Standard };
    public Label Fehler { get; } = new()
    {
        AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche,
        Padding = new Padding(Stil.AbstandKlein), Visible = false,
    };
    public Button AnmeldenKnopf { get; } = Stil.HauptKnopf("Anmelden");
    public Button BeendenKnopf { get; }
    public Mitarbeiter? Angemeldet { get; private set; }

    // nachAbmelden: aus dem laufenden Programm nach dem Abmelden; sonst beim Start.
    public AnmeldenFenster(Speicher speicher, string vorschlag, bool nachAbmelden = false)
    {
        this.speicher = speicher;
        BeendenKnopf = Stil.NebenKnopf(nachAbmelden ? "Programm beenden" : "Beenden");
        Text = (nachAbmelden ? "Abgemeldet – Anmelden" : "Anmelden") + $" ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = !nachAbmelden;
        StartPosition = nachAbmelden ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label { Text = Programmversion.Name, AutoSize = true, Font = Stil.Ueberschrift, ForeColor = Stil.TextNormal });
        zeile(null, new Label { Text = Programmversion.Leittext, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label
        {
            Text = Betrieb.Aktiv
                ? "Mit dem eigenen Kürzel und Passwort anmelden. Alles, was du tust, steht mit deinem Kürzel im Programm. " +
                  "Sperren: Taste F12; nach einiger Zeit ohne Eingabe sperrt DokuLine von selbst."
                : "Mit dem eigenen Kürzel anmelden. In der Demo ohne Passwort – alles, was du tust, steht mit deinem Kürzel im Programm. " +
                  "Abmelden: Taste F12 oder „Abmelden (F12)“ oben rechts.",
            AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.TextMittel,
        });
        Kuerzel.Text = vorschlag;
        zeile("Kürzel", Kuerzel);
        if (Betrieb.Aktiv) zeile("Passwort", Passwort);
        zeile(null, Fehler);
        PasswortAendernFragen = kuerzel =>
        {
            using var f = new PasswortAendernFenster(speicher, kuerzel, Passwort.Text, pflicht: true);
            return Dialog.Zeigen(f, this) == DialogResult.OK;
        };
        if (!Betrieb.Aktiv)
        {
            zeile(null, new Label { Text = "DEMO: AUSGEDACHTE MITARBEITER (KLICK ÜBERNIMMT DAS KÜRZEL)", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandMittel, 0, 0) });
            var aktive = speicher.AlleMitarbeiter().Where(m => m.Aktiv).ToList();
            foreach (var m in aktive)
                DemoListe.Items.Add($"{m.Kuerzel} – {m.Name} · {string.Join(", ", m.Positionen)} · {m.Qualifikation}");
            DemoListe.SelectedIndexChanged += (_, _) =>
            {
                if (DemoListe.SelectedIndex >= 0) Kuerzel.Text = aktive[DemoListe.SelectedIndex].Kuerzel;
            };
            DemoListe.DoubleClick += (_, _) => Anmelden();
            zeile(null, DemoListe);
        }

        AnmeldenKnopf.Click += (_, _) => Anmelden();
        BeendenKnopf.DialogResult = DialogResult.Cancel;
        foreach (var b in new[] { AnmeldenKnopf, BeendenKnopf }) { b.AutoSize = true; b.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0); }
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([AnmeldenKnopf, BeendenKnopf]);
        zeile(null, knoepfe);
        Controls.Add(raster);
        AcceptButton = AnmeldenKnopf;
        CancelButton = BeendenKnopf;
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Anmelden()
    {
        try
        {
            if (!Betrieb.Aktiv) Angemeldet = speicher.Anmelden(Kuerzel.Text);
            else
            {
                var (ergebnis, m) = speicher.PasswortAnmelden(Kuerzel.Text, Passwort.Text);
                Passwort.Text = ergebnis == AnmeldeErgebnis.AendernPflicht ? Passwort.Text : "";
                Angemeldet = ergebnis switch
                {
                    AnmeldeErgebnis.Ok => m,
                    AnmeldeErgebnis.AendernPflicht => PasswortAendernFragen(m!.Kuerzel) ? m : throw new Eingabefehler(["Bitte zuerst ein eigenes Passwort festlegen."]),
                    AnmeldeErgebnis.Gesperrt => throw new Eingabefehler([PasswortRegeln.GesperrtText]),
                    AnmeldeErgebnis.OhnePasswort => throw new Eingabefehler(["Für dieses Kürzel ist noch kein Passwort festgelegt. Bitte an die Leitung oder den Verwalter wenden."]),
                    _ => throw new Eingabefehler(["Kürzel oder Passwort stimmt nicht."]),
                };
            }
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
