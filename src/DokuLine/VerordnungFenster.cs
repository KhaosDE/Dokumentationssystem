using System.ComponentModel;
using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Einzelheiten einer Verordnung (Klick auf eine Zeile): Karte mit Zustand, Dosis, Anordnung und den
// Knöpfen für Änderungen, darunter der Verlauf. Ein Knopf schließt das Fenster mit dem Wunsch;
// die Änderung öffnet die Ansicht danach (keine Fenster in Fenstern).
public sealed class VerordnungDetailFenster : Form
{
    private readonly string? sperrgrund;

    public VerordnungKarte Karte { get; }
    public TextBox Verlauf { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Width = 620, Height = 220, BackColor = Stil.Flaeche };
    public AenderungArt? Wunsch { get; private set; }
    public bool BedarfGewuenscht { get; private set; }
    private readonly string? bedarfGrund;

    public VerordnungDetailFenster(Tagesstand s, string? sperrgrund, string? bedarfGrund = null, string? bedarfHeute = null)
    {
        this.sperrgrund = sperrgrund;
        this.bedarfGrund = bedarfGrund;
        Fenster.Grundform(this, s.Verordnung.Medikament);
        Karte = new VerordnungKarte(s, bedarfHeute) { Width = 620, Margin = new Padding(0, 0, 0, Stil.AbstandMittel) };
        Stil.Sperren(Karte.BedarfKnopf, bedarfGrund);   // eigenes Recht, nicht „Medikation bearbeiten“
        Karte.BedarfKnopf.Click += (_, _) => BedarfWaehlen();
        Karte.VerlaufKnopf.Visible = false;   // der Verlauf steht darunter
        foreach (var (knopf, art) in Karte.Aenderungen)
        {
            Stil.Sperren(knopf, sperrgrund);   // Rechte: sichtbar, aber gesperrt
            knopf.Click += (_, _) => Waehlen(art);
        }
        Karte.PerformLayout();
        Verlauf.Text = string.Join(Environment.NewLine + Environment.NewLine, MedikationRegeln.Verlauf(s.Verordnung));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(MedikationRegeln.DruckHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        zeile(null, Karte);
        zeile(null, new Label { Text = "VERLAUF", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Verlauf);
        var schliessen = Stil.NebenKnopf("Schließen");
        schliessen.DialogResult = DialogResult.Cancel;
        schliessen.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        zeile(null, schliessen);
        Controls.Add(raster);
        CancelButton = schliessen;
        Stil.DpiSkalierungEinschalten(this);
    }

    // „Bedarf gegeben …“ (wie der Knopf); nur an einer Bedarfsverordnung, die heute gilt, und mit Recht.
    public bool BedarfWaehlen()
    {
        if (bedarfGrund is not null || !Karte.BedarfMoeglich) return false;
        BedarfGewuenscht = true;
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    // Änderung wählen (wie der Knopf); geht nicht, wenn sie hier nicht passt oder das Recht fehlt.
    public bool Waehlen(AenderungArt art)
    {
        if (sperrgrund is not null || !Karte.Moeglich.Contains(art)) return false;
        Wunsch = art;
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Neue Verordnung: Medikament aus der Datenbank, Stärke, Mengen je Einnahmezeit oder Bedarf,
// Beginn und Anordnung (Arzt und Datum, in der Demo ausgedacht).
public sealed class VerordnungFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<Medikament> medikamente;

    public ComboBox MedikamentWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    public ComboBox Staerke { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 160 };
    public TextBox Einheit { get; } = new() { Width = 100, Text = "Stück" };
    public CheckBox Bedarf { get; } = new() { Text = "Bedarfsmedikation", AutoSize = true };
    public TextBox[] Mengen { get; } = Enumerable.Range(0, 4).Select(_ => new TextBox { Width = 50, Text = "0", TextAlign = HorizontalAlignment.Center }).ToArray();
    public TextBox Anlass { get; } = new() { Width = 260, PlaceholderText = "z. B. bei starker Unruhe", Enabled = false };
    public TextBox Hoechstens { get; } = new() { Width = 160, PlaceholderText = "z. B. 2 in 24 Stunden", Enabled = false };
    public TextBox Beginn { get; } = new DatumFeld() { Width = 100 };
    public ArztWahl Arzt { get; }   // Auswahl aus dem Ärzteverzeichnis
    public TextBox AngeordnetAm { get; } = new DatumFeld() { Width = 100 };
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Verordnung? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VerordnungFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Arzt = new ArztWahl(speicher);
        medikamente = speicher.Medikamente().Where(m => !m.Gesperrt).ToList();   // gesperrte nicht wählbar
        Text = $"Neue Verordnung – {bewohner.Name} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(MedikationRegeln.DruckHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        MedikamentWahl.Items.AddRange(medikamente.Select(m => (object)m.Auswahltext).ToArray());   // „… – ungeprüft“
        MedikamentWahl.SelectedIndexChanged += (_, _) =>
        {
            Staerke.Items.Clear();
            if (MedikamentWahl.SelectedIndex >= 0) Staerke.Items.AddRange(medikamente[MedikamentWahl.SelectedIndex].Aktuell.Staerken.Select(x => (object)x).ToArray());
            Staerke.Text = Staerke.Items.Count > 0 ? (string)Staerke.Items[0]! : "";
        };
        zeile("Medikament", MedikamentWahl);
        zeile("Stärke, Einheit", Formular.Reihe(Staerke, Einheit));
        zeile("", Bedarf);
        var mengen = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        for (int i = 0; i < 4; i++)
        {
            mengen.Controls.Add(new Label { Text = MedikationRegeln.Zeiten[i], AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMini, 0) });
            Mengen[i].Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandMittel, 0);
            mengen.Controls.Add(Mengen[i]);
        }
        zeile("Menge", mengen);
        zeile("Bei Bedarf: Anlass", Anlass);
        zeile("höchstens", Hoechstens);
        Bedarf.CheckedChanged += (_, _) =>
        {
            Anlass.Enabled = Hoechstens.Enabled = Bedarf.Checked;
            for (int i = 1; i < 4; i++) Mengen[i].Enabled = !Bedarf.Checked;   // Bedarf: nur eine Einzeldosis (erstes Feld)
        };
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        Beginn.Text = heute;
        AngeordnetAm.Text = heute;
        zeile("Beginn", Beginn);
        zeile("Angeordnet von, am", Formular.Reihe(Arzt, AngeordnetAm));
        zeile("Hinweis", Hinweis);
        zeile("Eingetragen von", Recht.KuerzelAnzeige());
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Ein Medikament vorwählen (aus einer Visite); gesperrte stehen nicht zur Wahl.
    public bool MedikamentWaehlen(long medikamentId)
    {
        int i = medikamente.FindIndex(m => m.Id == medikamentId);
        if (i >= 0) MedikamentWahl.SelectedIndex = i;
        return i >= 0;
    }

    public bool Speichern()
    {
        try
        {
            var m = MedikamentWahl.SelectedIndex >= 0 ? medikamente[MedikamentWahl.SelectedIndex] : null;
            var v = MedikationRegeln.AusEingabe(new VerordnungEingabe
            {
                BewohnerId = bewohner.Id!.Value, MedikamentId = m?.Id ?? 0, Staerke = Staerke.Text, Einheit = Einheit.Text,
                Morgens = Mengen[0].Text, Mittags = Mengen[1].Text, Abends = Mengen[2].Text, Nachts = Mengen[3].Text,
                Bedarf = Bedarf.Checked, BedarfAnlass = Anlass.Text, BedarfHoechstens = Hoechstens.Text,
                Beginn = Beginn.Text, Arzt = Arzt.Text, AngeordnetAm = AngeordnetAm.Text, Hinweis = Hinweis.Text,
                Kuerzel = Einstellungen.Laden().Kuerzel,
            }, m?.Anzeige ?? "", Jetzt());
            Gespeichert = speicher.VerordnungSpeichern(v);
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

// Pause, Pause beenden, Stufenplan oder Absetzen – immer mit Anordnung (Arzt, Datum).
public sealed class AenderungFenster : Form
{
    public const int StufenZeilen = 6;
    private readonly Speicher speicher;
    private readonly Verordnung verordnung;
    private readonly AenderungArt art;

    public TextBox Von { get; } = new DatumFeld() { Width = 100 };
    public TextBox Bis { get; } = new DatumFeld() { Width = 100, PlaceholderText = "leer = bis auf Weiteres" };
    public TextBox[,] Stufen { get; } = new TextBox[StufenZeilen, 5];
    public ArztWahl Arzt { get; }   // Auswahl aus dem Ärzteverzeichnis
    public TextBox AngeordnetAm { get; } = new DatumFeld() { Width = 100 };
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AenderungFenster(Speicher speicher, Verordnung v, AenderungArt art)
    {
        this.speicher = speicher;
        verordnung = v;
        this.art = art;
        Arzt = new ArztWahl(speicher);
        string titel = art switch
        {
            AenderungArt.Pause => "Pause",
            AenderungArt.PauseEnde => "Pause beenden",
            AenderungArt.Stufenplan => "Stufenplan",
            _ => "Absetzen",
        };
        Text = $"{titel} – {v.Medikament} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = $"{v.Medikament} · {v.Staerke} · Grundverordnung {v.Dosis.Text}", AutoSize = true, Font = Stil.Fett, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Von.Text = heute;
        switch (art)
        {
            case AenderungArt.Pause:
                zeile("Pause von", Von);
                zeile("bis (einschließlich)", Bis);
                break;
            case AenderungArt.PauseEnde:
                zeile("Pause endet am", Von);
                break;
            case AenderungArt.Absetzen:
                zeile("Abgesetzt ab", Von);
                break;
            default:
                var tabelle = new TableLayoutPanel { ColumnCount = 5, AutoSize = true, Margin = new Padding(0) };
                foreach (var kopf in new[] { "ab Datum", "morgens", "mittags", "abends", "nachts" })
                    tabelle.Controls.Add(new Label { Text = kopf, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel });
                for (int r = 0; r < StufenZeilen; r++)
                    for (int c = 0; c < 5; c++)
                    {
                        Stufen[r, c] = new TextBox { Width = c == 0 ? 100 : 50, TextAlign = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center, Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini) };
                        tabelle.Controls.Add(Stufen[r, c]);
                    }
                zeile("Stufen", tabelle);
                zeile(null, new Label
                {
                    Text = "Leere Zeilen werden nicht beachtet. Ist die letzte Stufe überall 0, ist das Medikament danach abgesetzt; " +
                           "sonst wird die letzte Stufe zur neuen Grundverordnung (die bisherige bleibt im Verlauf).",
                    AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
                });
                break;
        }
        AngeordnetAm.Text = heute;
        zeile("Angeordnet von, am", Formular.Reihe(Arzt, AngeordnetAm));
        zeile("Hinweis", Hinweis);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Eine Stufenzeile füllen (für Tests und schnelle Eingabe).
    public void StufeSetzen(int zeile, string ab, string morgens, string mittags, string abends, string nachts)
    {
        string[] werte = [ab, morgens, mittags, abends, nachts];
        for (int c = 0; c < 5; c++) Stufen[zeile, c].Text = werte[c];
    }

    public bool Speichern()
    {
        string kuerzel = Einstellungen.Laden().Kuerzel;
        var jetzt = Jetzt();
        try
        {
            var a = art switch
            {
                AenderungArt.Pause => MedikationRegeln.Pause(Von.Text, Bis.Text, Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
                AenderungArt.PauseEnde => MedikationRegeln.PauseEnde(Von.Text, Arzt.Text, AngeordnetAm.Text, kuerzel, jetzt),
                AenderungArt.Absetzen => MedikationRegeln.Absetzen(Von.Text, Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
                _ => MedikationRegeln.Stufenplan(Enumerable.Range(0, StufenZeilen)
                        .Select(r => (Stufen[r, 0].Text, Stufen[r, 1].Text, Stufen[r, 2].Text, Stufen[r, 3].Text, Stufen[r, 4].Text))
                        .Where(z => z.Item1.Trim().Length > 0).ToList(), Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
            };
            speicher.AenderungSpeichern(verordnung.Id, a);
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
