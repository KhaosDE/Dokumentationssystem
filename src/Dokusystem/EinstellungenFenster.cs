using Dokusystem.Kern;

namespace Dokusystem;

// Einstellungen: wer angemeldet ist (Kürzel), Update beim Start, eigenes Bild für den Kopf (Logo).
// Gespeichert wird erst mit „Speichern“; das Bild landet als Kopie im Datenordner (Regel 7).
public sealed class EinstellungenFenster : Form
{
    private const int LogoBreite = 160, LogoHoehe = 64;

    private readonly Panel logoVorschau = new();
    // Inhalt mit Bildlaufleiste; „Speichern“ und „Abbrechen“ bleiben unten immer sichtbar
    // (das Fenster ist höher als kleinere Bildschirme).
    private readonly TableLayoutPanel raster = new()
    {
        ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Top,
    };
    private readonly Panel inhalt = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly TableLayoutPanel unten = new()
    {
        ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Bottom,
        Padding = new Padding(Stil.AbstandGross, 0, Stil.AbstandGross, Stil.AbstandGross),
    };
    private Image? logoBild;
    private bool logoGeaendert;

    // Wer angemeldet ist: eine der ausgedachten Kolleginnen oder Kollegen (gleich wie oben rechts
    // „Angemeldet als …“). Das Kürzel steht dann bei Berichten, Übergabe und Aufgaben.
    public ComboBox Benutzer { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly List<string> benutzerKuerzel = [];
    public CheckBox AutoUpdate { get; } = new();
    public NumericUpDown BeschlussFrist { get; } = new() { Minimum = 1, Maximum = 365, Width = 80 };
    public NumericUpDown HebFrist { get; } = new() { Minimum = 1, Maximum = 60, Width = 80 };

    // Mehrplatz: Bereichskennung dieses Arbeitsplatzes und Ort der Datenbank (gilt nach Neustart).
    public ComboBox Bereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 220, MaxLength = Kern.Bereiche.Laenge };
    public Label DatenbankOrt { get; } = new() { AutoSize = true, MaximumSize = new Size(380, 0), UseMnemonic = false };
    public Button OrdnerKnopf { get; } = Stil.NebenKnopf("Gemeinsamen Ordner wählen …");
    public Button LokalKnopf { get; } = Stil.NebenKnopf("Lokal (Vorgabe)");
    public string? DatenbankOrdner { get; private set; }

    // Zum Testen ersetzbar: Ordnerauswahl (null = abgebrochen).
    public Func<string?> OrdnerWaehlen { get; set; } = () =>
    {
        using var dialog = new FolderBrowserDialog { Description = "Gemeinsamen Ordner für die Datenbank wählen (z. B. ein Netzlaufwerk)", UseDescriptionForTitle = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    };
    public Dictionary<Bogenart, Label> Vorlagen { get; } = [];

    // Zum Testen ersetzbar: Dateiauswahl für eine PDF-Vorlage (null = abgebrochen).
    public Func<string?> PdfWaehlen { get; set; } = () =>
    {
        using var dialog = new OpenFileDialog { Title = "PDF-Vorlage wählen", Filter = "PDF (*.pdf)|*.pdf" };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    };
    public Button LogoWeg { get; } = Stil.NebenKnopf("Bild entfernen");
    public Label Fehler { get; } = new();
    public string? NeuesLogo { get; private set; }

    // Abschnitt „Module“: ein Häkchen je Modul und Untermodul (Schlüssel wie in Speicher.ModulAn).
    public Dictionary<string, CheckBox> Module { get; } = [];
    private readonly Speicher? speicher;
    public bool LogoEntfernen { get; private set; }

    public EinstellungenFenster(IReadOnlyList<Kollege>? kollegen = null, Speicher? speicher = null)
    {
        this.speicher = speicher;
        var werte = Einstellungen.Laden();
        Text = $"Einstellungen ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        unten.BackColor = inhalt.BackColor = Stil.Fensterhintergrund;
        unten.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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
        Benutzer.Items.Add("– niemand –");
        benutzerKuerzel.Add("");
        foreach (var k in kollegen ?? [])
        {
            Benutzer.Items.Add($"{k.Anzeige} – {string.Join(", ", k.Teams)}");
            benutzerKuerzel.Add(k.Kuerzel);
        }
        // Ein früher frei eingetragenes Kürzel bleibt wählbar, bis man jemand anderen wählt.
        if (werte.Kuerzel.Length > 0 && !benutzerKuerzel.Contains(werte.Kuerzel))
        {
            Benutzer.Items.Add($"{werte.Kuerzel} (kein Benutzer der Demo)");
            benutzerKuerzel.Add(werte.Kuerzel);
        }
        Benutzer.SelectedIndex = Math.Max(0, benutzerKuerzel.IndexOf(werte.Kuerzel));
        Benutzer.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
        raster.Controls.Add(Beschriftung("Angemeldet als"));
        raster.Controls.Add(Benutzer);
        raster.Controls.Add(new Label());
        raster.Controls.Add(new Label
        {
            Text = "Nur für die Vorführung, ohne Passwort. Wechseln geht auch oben rechts im Kopf.",
            AutoSize = true, MaximumSize = new Size(380, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });

        Ganz(Abschnitt("MEHRPLATZ"));
        Bereich.Items.AddRange([.. Kern.Bereiche.Vorschlaege]);
        Bereich.Text = werte.Bereich;
        Bereich.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
        raster.Controls.Add(Beschriftung("Bereich"));
        raster.Controls.Add(Bereich);
        raster.Controls.Add(new Label());
        raster.Controls.Add(new Label
        {
            Text = "Ausgedachte Kennung dieses Arbeitsplatzes. Die Übersicht zeigt zuerst die Bewohner dieses Bereichs; " +
                   "Statusmeldungen tragen ihn mit.",
            AutoSize = true, MaximumSize = new Size(380, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        DatenbankOrdner = werte.DatenbankOrdner;
        DatenbankOrt.Margin = new Padding(0, Stil.AbstandKlein, 0, Stil.AbstandMini);
        OrdnerKnopf.Click += (_, _) => { if (OrdnerWaehlen() is string ordner) OrdnerSetzen(ordner); };
        LokalKnopf.Click += (_, _) => OrdnerSetzen(null);
        foreach (var k in new[] { OrdnerKnopf, LokalKnopf }) k.AutoSize = true;
        var ortSpalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        ortSpalte.Controls.AddRange([DatenbankOrt, Formular.Reihe(OrdnerKnopf, LokalKnopf)]);
        raster.Controls.Add(Beschriftung("Datenbank"));
        raster.Controls.Add(ortSpalte);
        raster.Controls.Add(new Label());
        raster.Controls.Add(new Label
        {
            Text = "Mehrere Rechner mit demselben Ordner arbeiten gemeinsam (zum Ausprobieren: zwei Programmfenster). " +
                   "Gilt nach dem Neustart. Ist der Ordner leer, entstehen dort die Demo-Daten. Auch im gemeinsamen Ordner nur ausgedachte Daten.",
            AutoSize = true, MaximumSize = new Size(380, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        OrtAnzeigen();

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

        if (speicher is not null)
        {
            Ganz(Abschnitt("MODULE"));
            foreach (var m in Modulverwaltung.Alle)
            {
                var haken = new CheckBox
                {
                    Text = m.Name, AutoSize = true, Font = Stil.Fett, Checked = speicher.ModulAn(m.Daten.Schluessel),
                    Margin = new Padding(0, Stil.AbstandKlein, 0, 0),
                };
                Module[m.Daten.Schluessel] = haken;
                Ganz(haken);
                Ganz(new Label
                {
                    Text = $"{m.Beschreibung} Ausgeschaltet verschwindet es aus Seitenleiste, Akte und Karten; die Daten bleiben erhalten.",
                    AutoSize = true, MaximumSize = new Size(540, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
                    Margin = new Padding(Stil.AbstandGross + Stil.AbstandMini, 0, 0, Stil.AbstandMini),
                });
                foreach (var u in m.Untermodule)
                {
                    var unter = new CheckBox
                    {
                        Text = $"{u.Name} – {u.Beschreibung}", AutoSize = true, Checked = speicher.ModulAn(u.Schluessel),
                        Enabled = haken.Checked, Margin = new Padding(Stil.AbstandGross + Stil.AbstandMini, 0, 0, 0),
                    };
                    haken.CheckedChanged += (_, _) => unter.Enabled = haken.Checked;
                    Module[u.Schluessel] = unter;
                    Ganz(unter);
                }
            }
        }

        Ganz(Abschnitt("FRISTEN"));
        BeschlussFrist.Value = werte.BeschlussFristTage;
        BeschlussFrist.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandMini);
        raster.Controls.Add(Beschriftung("Beschluss endet bald"));
        var frist = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        frist.Controls.AddRange([BeschlussFrist, new Label { Text = "Tage vorher (Marke auf der Übersicht)", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }]);
        raster.Controls.Add(frist);
        HebFrist.Value = werte.HebFristMonate;
        HebFrist.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandMini);
        raster.Controls.Add(Beschriftung("HEB fällig"));
        var hebFrist = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        hebFrist.Controls.AddRange([HebFrist, new Label { Text = "Monate nach dem letzten Bogen", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }]);
        raster.Controls.Add(hebFrist);

        Ganz(Abschnitt("HEB-VORLAGEN"));
        Ganz(new Label
        {
            Text = "Je Bogenart eine PDF mit Formularfeldern zuteilen. Sie wird in den Datenordner kopiert und nie mitgeliefert; " +
                   "ausgefüllt wird immer eine Kopie. Die Zuteilung gilt sofort.",
            AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
        });
        foreach (var art in Enum.GetValues<Bogenart>())
        {
            var status = new Label { AutoSize = true, MaximumSize = new Size(380, 0), Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
            Vorlagen[art] = status;
            var zuteilen = Stil.NebenKnopf("Zuteilen …");
            var entfernen = Stil.NebenKnopf("Entfernen");
            var zuordnung = Stil.NebenKnopf("Zuordnung …");
            zuteilen.Click += (_, _) => { if (PdfWaehlen() is string pfad) VorlageZuteilen(art, pfad); };
            entfernen.Click += (_, _) => { HebPdf.VorlageEntfernen(art); VorlageAnzeigen(art); };
            zuordnung.Click += (_, _) =>
            {
                using var f = new HebZuordnungFenster(art);
                Dialog.Zeigen(f, this);
            };
            foreach (var k in new[] { zuteilen, entfernen, zuordnung }) { k.AutoSize = true; k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0); }
            raster.Controls.Add(Beschriftung(HebRegeln.ArtKurz(art)));
            var spalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 0, Stil.AbstandKlein) };
            spalte.Controls.AddRange([status, Formular.Reihe(zuteilen, entfernen, zuordnung)]);
            raster.Controls.Add(spalte);
            VorlageAnzeigen(art);
        }

        Fehler.AutoSize = true;
        Fehler.MaximumSize = new Size(560, 0);
        Fehler.ForeColor = Stil.Beschluss.Text;
        Fehler.BackColor = Stil.Beschluss.Flaeche;
        Fehler.Padding = new Padding(Stil.AbstandKlein);
        Fehler.Margin = new Padding(0, Stil.AbstandMittel, 0, 0);
        Fehler.Visible = false;
        unten.Controls.Add(Fehler);

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
        unten.Controls.Add(leiste);

        inhalt.Controls.Add(raster);
        Controls.Add(inhalt);
        Controls.Add(unten);
        ClientSize = new Size(raster.PreferredSize.Width + SystemInformation.VerticalScrollBarWidth,
            Math.Min(700, raster.PreferredSize.Height + unten.PreferredSize.Height));
        CancelButton = abbrechen;
        LogoLaden(Einstellungen.LogoPfad());
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool HatLogo => logoBild is not null;

    // Nach der Skalierung: so hoch wie nötig, höchstens so hoch wie der Bildschirm.
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var flaeche = Screen.FromControl(this).WorkingArea;
        int rahmen = Height - ClientSize.Height;
        int breite = raster.GetPreferredSize(Size.Empty).Width + SystemInformation.VerticalScrollBarWidth;
        int hoehe = raster.GetPreferredSize(new Size(breite, 0)).Height + unten.GetPreferredSize(new Size(breite, 0)).Height;
        ClientSize = new Size(Math.Min(breite, flaeche.Width - 40), Math.Min(hoehe, flaeche.Height - rahmen - 40));
        CenterToParent();
    }

    // null = lokal. Ein gewählter Ordner muss erreichbar und beschreibbar sein.
    public bool OrdnerSetzen(string? ordner)
    {
        if (ordner is not null && Speicher.OrdnerPruefen(ordner) is string fehler)
        {
            FehlerZeigen(fehler);
            return false;
        }
        DatenbankOrdner = ordner;
        Fehler.Visible = false;
        OrtAnzeigen();
        return true;
    }

    private void OrtAnzeigen()
    {
        DatenbankOrt.Text = DatenbankOrdner is null ? "lokal im Datenordner dieses Rechners" : $"gemeinsamer Ordner: {DatenbankOrdner}";
        LokalKnopf.Enabled = DatenbankOrdner is not null;
    }

    public bool VorlageZuteilen(Bogenart art, string pfad)
    {
        try
        {
            HebPdf.VorlageZuteilen(art, pfad);
        }
        catch (Exception e) when (e is Eingabefehler or IOException or UnauthorizedAccessException)
        {
            FehlerZeigen(e is Eingabefehler f ? string.Join(" ", f.Fehler) : $"Zuteilen ging nicht: {e.Message}");
            return false;
        }
        Fehler.Visible = false;
        VorlageAnzeigen(art);
        return true;
    }

    private void VorlageAnzeigen(Bogenart art)
    {
        string? pfad = HebPdf.Vorlage(art);
        try
        {
            Vorlagen[art].Text = pfad is null ? "keine Vorlage – der Bogen lässt sich trotzdem schreiben und drucken"
                : $"zugeteilt ({HebPdf.Felder(pfad).Count} Formularfelder)";
        }
        catch (Eingabefehler)
        {
            Vorlagen[art].Text = "zugeteilt, aber nicht lesbar – bitte neu zuteilen";
        }
    }

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
        try
        {
            string kuerzel = benutzerKuerzel[Math.Max(0, Benutzer.SelectedIndex)];
            if (Kern.Bereiche.Pruefen(Bereich.Text) is string bereichFehler)
            {
                FehlerZeigen(bereichFehler);
                return false;
            }
            Einstellungen.Aendern(w => w with
            {
                Kuerzel = kuerzel, AutoUpdate = AutoUpdate.Checked, BeschlussFristTage = (int)BeschlussFrist.Value, HebFristMonate = (int)HebFrist.Value,
                Bereich = Kern.Bereiche.Bereinigen(Bereich.Text), DatenbankOrdner = DatenbankOrdner,
            });
            if (logoGeaendert && NeuesLogo is not null) Einstellungen.LogoSetzen(NeuesLogo);
            else if (LogoEntfernen) Einstellungen.LogoSetzen(null);
            foreach (var (schluessel, haken) in Module)
                if (speicher!.ModulAn(schluessel) != haken.Checked) speicher.ModulSetzen(schluessel, haken.Checked);
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
