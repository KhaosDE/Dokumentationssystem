using Dokusystem.Kern;

namespace Dokusystem;

// Einstellungen: eigenes Kürzel, Update beim Start, eigenes Bild für den Kopf (Logo).
// Gespeichert wird erst mit „Speichern“; das Bild landet als Kopie im Datenordner (Regel 7).
public sealed class EinstellungenFenster : Form
{
    private const int LogoBreite = 160, LogoHoehe = 64;

    private readonly Panel logoVorschau = new();
    private Image? logoBild;
    private bool logoGeaendert;

    public TextBox Kuerzel { get; } = new();
    public CheckBox AutoUpdate { get; } = new();
    public Button LogoWeg { get; } = Stil.NebenKnopf("Bild entfernen");
    public Label Fehler { get; } = new();
    public string? NeuesLogo { get; private set; }
    public bool LogoEntfernen { get; private set; }

    public EinstellungenFenster()
    {
        var werte = Einstellungen.Laden();
        Text = $"Einstellungen ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var raster = new TableLayoutPanel
        {
            ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill,
        };
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));

        Label Beschriftung(string text) => new()
        {
            Text = text, AutoSize = true, ForeColor = Stil.TextMittel,
            Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein),
        };
        Label Abschnitt(string text) => new()
        {
            Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise,
            Margin = new Padding(0, Stil.AbstandGross, 0, Stil.AbstandMini),
        };
        void Ganz(Control c) { raster.Controls.Add(c); raster.SetColumnSpan(c, 2); }

        Ganz(new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });

        Ganz(Abschnitt("ICH"));
        Kuerzel.Text = werte.Kuerzel;
        Kuerzel.MaxLength = Einstellungen.KuerzelLaenge;
        Kuerzel.Width = 120;
        Kuerzel.PlaceholderText = "z. B. AB";
        Kuerzel.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
        raster.Controls.Add(Beschriftung("Eigenes Kürzel"));
        raster.Controls.Add(Kuerzel);

        Ganz(Abschnitt("PROGRAMM"));
        AutoUpdate.Text = "Beim Start nach Updates suchen";
        AutoUpdate.AutoSize = true;
        AutoUpdate.Checked = werte.AutoUpdate;
        AutoUpdate.Margin = new Padding(0, Stil.AbstandKlein, 0, Stil.AbstandKlein);
        raster.Controls.Add(Beschriftung("Update"));
        raster.Controls.Add(AutoUpdate);

        // Logo: Vorschau, Knöpfe, Hinweis.
        logoVorschau.Size = new Size(LogoBreite, LogoHoehe);
        logoVorschau.BackColor = Stil.Flaeche;
        logoVorschau.Margin = new Padding(0, 0, Stil.AbstandGross, 0);
        logoVorschau.Paint += (_, e) => LogoZeichnen(e.Graphics);
        var logoWaehlen = Stil.NebenKnopf("Bild wählen …");
        logoWaehlen.Margin = LogoWeg.Margin = new Padding(0, 0, 0, Stil.AbstandKlein);
        logoWaehlen.Click += (_, _) => LogoWaehlen();
        LogoWeg.Click += (_, _) => { NeuesLogo = null; LogoEntfernen = true; LogoLaden(null); };
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        knoepfe.Controls.AddRange([logoWaehlen, LogoWeg]);
        var logoZeile = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        logoZeile.Controls.AddRange([logoVorschau, knoepfe]);
        raster.Controls.Add(Beschriftung("Bild im Kopf"));
        raster.Controls.Add(logoZeile);
        raster.Controls.Add(new Label());
        raster.Controls.Add(new Label
        {
            Text = "Erscheint oben rechts. Die Datei wird in den Datenordner kopiert und nie mitgeliefert.",
            AutoSize = true, MaximumSize = new Size(380, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });

        Ganz(Abschnitt("SPÄTER"));
        Ganz(new Label
        {
            Text = "Hier kommen weitere Einstellungen dazu: Fristen für die Übersicht (Version 0.15.0) " +
                   "und die PDF-Vorlagen für den HEB-Bogen (Version 0.16.0).",
            AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextLeise,
        });

        Fehler.AutoSize = true;
        Fehler.MaximumSize = new Size(560, 0);
        Fehler.ForeColor = Stil.Beschluss.Text;
        Fehler.BackColor = Stil.Beschluss.Flaeche;
        Fehler.Padding = new Padding(Stil.AbstandKlein);
        Fehler.Margin = new Padding(0, Stil.AbstandMittel, 0, 0);
        Fehler.Visible = false;
        Ganz(Fehler);

        var speichern = Stil.HauptKnopf("Speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        speichern.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        abbrechen.Margin = new Padding(0);
        var leiste = new FlowLayoutPanel
        {
            AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill,
            Margin = new Padding(0, Stil.AbstandGross, 0, 0),
        };
        leiste.Controls.AddRange([speichern, abbrechen]);
        Ganz(leiste);

        Controls.Add(raster);
        CancelButton = abbrechen;
        LogoLaden(Einstellungen.LogoPfad());
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool HatLogo => logoBild is not null;

    private void LogoLaden(string? pfad)
    {
        logoBild?.Dispose();
        logoBild = Bewohnerbild.Laden(pfad);
        LogoWeg.Enabled = logoBild is not null;
        logoVorschau.Invalidate();
    }

    private void LogoZeichnen(Graphics g)
    {
        g.Clear(Stil.Flaeche);
        var rahmen = new Rectangle(0, 0, logoVorschau.Width - 1, logoVorschau.Height - 1);
        using (var stift = new Pen(Stil.KarteRand)) g.DrawRectangle(stift, rahmen);
        if (logoBild is null)
        {
            TextRenderer.DrawText(g, "kein Bild", Stil.Klein, rahmen, Stil.TextLeise,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(logoBild, KopfBereich.Einpassen(logoBild.Size, Rectangle.Inflate(rahmen, -4, -4)));
    }

    public bool LogoSetzen(string pfad)
    {
        using var probe = Bewohnerbild.Laden(pfad);
        if (probe is null || !Bilder.EndungErlaubt(pfad))
        {
            FehlerZeigen($"„{Path.GetFileName(pfad)}“ ist kein lesbares Bild ({string.Join(", ", Bilder.Endungen)}).");
            return false;
        }
        NeuesLogo = pfad;
        LogoEntfernen = false;
        logoGeaendert = true;
        Fehler.Visible = false;
        LogoLaden(pfad);
        return true;
    }

    private void LogoWaehlen()
    {
        string muster = string.Join(";", Bilder.Endungen.Select(e => "*" + e));
        using var dialog = new OpenFileDialog { Title = "Bild für den Kopf wählen", Filter = $"Bilder ({muster})|{muster}" };
        if (dialog.ShowDialog(this) == DialogResult.OK) LogoSetzen(dialog.FileName);
    }

    private void FehlerZeigen(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = true;
    }

    public bool Speichern()
    {
        if (Einstellungen.KuerzelPruefen(Kuerzel.Text) is string fehler)
        {
            FehlerZeigen(fehler);
            return false;
        }
        try
        {
            Einstellungen.Aendern(w => w with { Kuerzel = Kuerzel.Text.Trim(), AutoUpdate = AutoUpdate.Checked });
            if (logoGeaendert && NeuesLogo is not null) Einstellungen.LogoSetzen(NeuesLogo);
            else if (LogoEntfernen) Einstellungen.LogoSetzen(null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            FehlerZeigen($"Speichern ging nicht: {e.Message}");
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) logoBild?.Dispose();
        base.Dispose(disposing);
    }
}
