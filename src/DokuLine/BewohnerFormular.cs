using DokuLine.Kern;

namespace DokuLine;

// Formular: Bewohner anlegen oder bearbeiten. Prüfen und Speichern macht der Kern.
public sealed class BewohnerFormular : Form
{
    private const int BildGroesse = 96;

    private readonly Speicher speicher;
    private readonly Bewohner? bewohner;
    private readonly BildFeld bild = new();
    private readonly Button bildWeg = Stil.NebenKnopf("Bild entfernen");

    public TextBox Vorname { get; } = Feld();
    public TextBox Nachname { get; } = Feld();
    public TextBox Zimmer { get; } = Feld();
    // Mehrplatz: zu welchem Wohnbereich der Bewohner gehört (Vorschläge aus den vorhandenen, eigene möglich).
    public ComboBox Wohnbereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Fill, Font = Stil.Standard, MaxLength = Kern.Bereiche.Laenge };
    public TextBox Geburtsdatum { get; } = Feld("TT.MM.JJJJ");
    public TextBox Aufnahmedatum { get; } = Feld("TT.MM.JJJJ");
    public Dreiwahl Beschluss { get; } = new(["ende", "kein_beschluss", "nicht_erfasst"],
                                             ["endet am", "kein Beschluss", "nicht erfasst"], "TT.MM.JJJJ");
    public Dreiwahl OffenGefuehrt { get; } = new(["ja", "nein", "nicht_erfasst"], ["ja", "nein", "nicht erfasst"], null);
    public Dreiwahl Allergie { get; } = new(["eingetragen", "keine_bekannt", "nicht_erfasst"],
                                            ["eingetragen:", "keine bekannt", "nicht erfasst"], "z. B. Haselnüsse");
    public Label Fehler { get; } = new();
    public Button SpeichernKnopf { get; } = Stil.HauptKnopf("Speichern");

    public Bewohner? Gespeichert { get; private set; }
    public string? NeuesBild { get; private set; }
    public bool BildEntfernen { get; private set; }

    public BewohnerFormular(Speicher speicher, Bewohner? bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        ausgangsfassung = bewohner;
        string titel = bewohner is null ? "Neuer Bewohner" : $"Akte bearbeiten – {bewohner.Name}";
        Text = $"{titel} ({KopfBereich.DemoHinweis})";
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
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(Stil.AbstandGross),
            Dock = DockStyle.Fill,
        };
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raster.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));

        void Zeile(string? beschriftung, Control feld)
        {
            if (beschriftung is null)
            {
                raster.Controls.Add(feld);
                raster.SetColumnSpan(feld, 2);
                return;
            }
            raster.Controls.Add(new Label
            {
                Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel,
                Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein),
            });
            raster.Controls.Add(feld);
        }

        var demo = new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) };
        Zeile(null, demo);
        Zeile("Vorname", Vorname);
        Zeile("Nachname", Nachname);
        Zeile("Zimmer", Zimmer);
        Zeile("Wohnbereich", Wohnbereich);
        Zeile("Geburtsdatum", Geburtsdatum);
        Zeile("Aufnahmedatum", Aufnahmedatum);
        Beschluss.Dock = DockStyle.Fill;
        Allergie.Dock = DockStyle.Fill;
        Beschluss.Margin = Allergie.Margin = new Padding(0, Stil.AbstandKlein, 0, Stil.AbstandKlein);
        Zeile("Beschluss", Beschluss);
        OffenGefuehrt.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
        Zeile("Offen geführt", OffenGefuehrt);
        Zeile("Allergien", Allergie);

        // Bild: Vorschau, Knöpfe und Hinweis (Regel 2).
        var bildZeile = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        bild.Size = new Size(BildGroesse, BildGroesse);
        bild.Margin = new Padding(0, 0, Stil.AbstandGross, 0);
        var bildKnoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        var bildWaehlen = Stil.NebenKnopf("Bild wählen …");
        bildWaehlen.Margin = bildWeg.Margin = new Padding(0, 0, 0, Stil.AbstandKlein);
        bildWaehlen.Click += (_, _) => BildWaehlen();
        bildWeg.Click += (_, _) => { NeuesBild = null; BildEntfernen = true; BildZeigen(); };
        var bildHinweis = new Label { Text = "Demo: bitte keine Fotos echter Personen.", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise };
        bildKnoepfe.Controls.AddRange([bildWaehlen, bildWeg, bildHinweis]);
        bildZeile.Controls.AddRange([bild, bildKnoepfe]);
        Zeile("Bild", bildZeile);

        Fehler.AutoSize = true;
        Fehler.MaximumSize = new Size(560, 0);
        Fehler.ForeColor = Stil.Beschluss.Text;
        Fehler.BackColor = Stil.Beschluss.Flaeche;
        Fehler.Padding = new Padding(Stil.AbstandKlein);
        Fehler.Margin = new Padding(0, Stil.AbstandMittel, 0, 0);
        Fehler.Visible = false;
        Zeile(null, Fehler);

        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        SpeichernKnopf.Click += (_, _) => Speichern();
        var knoepfe = new FlowLayoutPanel
        {
            AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill,
            Margin = new Padding(0, Stil.AbstandGross, 0, 0),
        };
        SpeichernKnopf.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        abbrechen.Margin = new Padding(0);
        knoepfe.Controls.AddRange([SpeichernKnopf, abbrechen]);
        Zeile(null, knoepfe);

        Controls.Add(raster);
        CancelButton = abbrechen;

        // Vorschläge: alle Wohnbereiche der vorhandenen Bewohner und „Wohnbereich A/B“; neu: der eigene Bereich,
        // wenn er ein Wohnbereich ist.
        var vorhanden = speicher.AlleBewohner().Select(x => x.Wohnbereich)
            .Concat(Kern.Bereiche.Vorschlaege.Where(v => v.StartsWith("Wohnbereich", StringComparison.Ordinal)))
            .Distinct().Order(StringComparer.CurrentCulture).ToList();
        Wohnbereich.Items.AddRange([.. vorhanden]);
        string eigener = Einstellungen.Laden().Bereich;
        Wohnbereich.Text = vorhanden.Contains(eigener) ? eigener : Kern.Bereiche.Vorgabe;
        if (bewohner is not null)
        {
            var werte = BewohnerRegeln.AlsEingabe(bewohner);
            Wohnbereich.Text = werte.Wohnbereich;
            Vorname.Text = werte.Vorname;
            Nachname.Text = werte.Nachname;
            Zimmer.Text = werte.Zimmer;
            Geburtsdatum.Text = werte.Geburtsdatum;
            Aufnahmedatum.Text = werte.Aufnahmedatum;
            Beschluss.Setzen(werte.Beschluss, werte.Beschlussende);
            OffenGefuehrt.Setzen(werte.OffenGefuehrt, "");
            Allergie.Setzen(werte.Allergie, werte.Allergien);
        }
        Vorname.TextChanged += (_, _) => BildZeigen();
        Nachname.TextChanged += (_, _) => BildZeigen();
        BildZeigen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private static TextBox Feld(string platzhalter = "")
    {
        var feld = platzhalter == "TT.MM.JJJJ" ? new DatumFeld() : new TextBox();   // Datum mit Punkten (Schritt 26d)
        feld.Dock = DockStyle.Fill;
        feld.PlaceholderText = platzhalter;
        feld.Font = Stil.Standard;
        feld.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
        return feld;
    }

    // ------------------------------------------------------------------ Bild

    private void BildZeigen()
    {
        string? pfad = NeuesBild ?? (BildEntfernen ? null : Bilder.Pfad(bewohner?.Bild));
        bild.Setzen(pfad, BewohnerRegeln.Initialen(Vorname.Text, Nachname.Text), Nachname.Text);
        bildWeg.Enabled = pfad is not null;
    }

    public bool HatBild => bild.HatBild;

    public bool BildSetzen(string pfad)
    {
        using var probe = Bewohnerbild.Laden(pfad);
        if (probe is null || !Bilder.EndungErlaubt(pfad))
        {
            FehlerZeigen([$"„{Path.GetFileName(pfad)}“ ist kein lesbares Bild ({string.Join(", ", Bilder.Endungen)})."]);
            return false;
        }
        NeuesBild = pfad;
        BildEntfernen = false;
        Fehler.Visible = false;
        BildZeigen();
        return true;
    }

    private void BildWaehlen()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Bild wählen",
            Filter = $"Bilder ({string.Join(";", Bilder.Endungen.Select(e => "*" + e))})|{string.Join(";", Bilder.Endungen.Select(e => "*" + e))}",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) BildSetzen(dialog.FileName);
    }

    // ------------------------------------------------------------------ Speichern

    public BewohnerEingabe Eingabe() => new()
    {
        Vorname = Vorname.Text,
        Nachname = Nachname.Text,
        Zimmer = Zimmer.Text,
        Geburtsdatum = Geburtsdatum.Text,
        Aufnahmedatum = Aufnahmedatum.Text,
        Beschluss = Beschluss.Zustand,
        Beschlussende = Beschluss.Feld.Text,
        OffenGefuehrt = OffenGefuehrt.Zustand,
        Wohnbereich = Wohnbereich.Text,
        Allergie = Allergie.Zustand,
        Allergien = Allergie.Feld.Text,
    };

    private void FehlerZeigen(IEnumerable<string> fehler)
    {
        Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", fehler);
        Fehler.Visible = true;
    }

    // Mehrplatz: so sah der Bewohner beim Öffnen aus. Hat ihn ein anderer Arbeitsplatz inzwischen
    // geändert, kommt der Konflikt-Hinweis (beide Fassungen) statt stillem Überschreiben.
    private Bewohner? ausgangsfassung;

    public bool Speichern()
    {
        try
        {
            var b = BewohnerRegeln.AusEingabe(Eingabe(), bewohner?.Id);
            Gespeichert = speicher.BewohnerSpeichern(b, NeuesBild, BildEntfernen, ausgangsfassung);
        }
        catch (Konflikt k)
        {
            switch (KonfliktFenster.Fragen(this, k))
            {
                case KonfliktWahl.MeineSpeichern:
                    ausgangsfassung = null;
                    return Speichern();
                case KonfliktWahl.AndereUebernehmen:
                    DialogResult = DialogResult.Cancel;   // die Akte zeigt danach die andere Fassung
                    Close();
                    return false;
                default:
                    return false;
            }
        }
        catch (Eingabefehler e)
        {
            FehlerZeigen(e.Fehler);
            return false;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            FehlerZeigen([$"Das Bild ließ sich nicht übernehmen: {e.Message}"]);
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
