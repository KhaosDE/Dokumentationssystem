using System.Drawing.Printing;
using Dokusystem.Kern;

namespace Dokusystem;

// Bereich „Aufgaben“ (Wohnbereich-ToDo): Aufgaben bleiben stehen, bis sie erledigt sind.
// Überfällige stehen oben, rot und mit dem Wort „überfällig“. Erledigte lassen sich einblenden;
// gelöscht wird nichts.
public sealed class AufgabenSeite : Panel
{
    private const int MaxBreite = 880;
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neue Aufgabe …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Offene Aufgaben drucken …");
    public CheckBox ErledigteZeigen { get; } = new() { Text = "Erledigte zeigen", AutoSize = true, BackColor = Stil.Seite };
    public Label KuerzelHinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public List<AufgabeKarte> Karten { get; } = [];
    public List<string> Abschnitte { get; } = [];
    public int Offen { get; private set; }
    public int Ueberfaellig { get; private set; }
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AufgabenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        foreach (var c in new Control[] { DruckKnopf, ErledigteZeigen, KuerzelHinweis })
        {
            c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        ErledigteZeigen.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        KuerzelHinweis.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        KuerzelHinweis.Text = "Für „erledigt“ und „weitergeben“ bitte unter Einstellungen ein Kürzel eintragen.";
        NeuKnopf.Click += (_, _) => NeueAufgabe();
        DruckKnopf.Click += (_, _) => Drucken();
        ErledigteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(leiste);
    }

    private static string Kuerzel => Einstellungen.Laden().Kuerzel;

    public string KopfText => (Offen == 1 ? "1 offene Aufgabe" : $"{Offen} offene Aufgaben") + (Ueberfaellig > 0 ? $" · {Ueberfaellig} überfällig" : "");

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        string kuerzel = Kuerzel;
        var alle = speicher.Aufgaben(jetzt, auchErledigte: true);
        var offen = alle.Where(a => a.Offen).ToList();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        Offen = offen.Count;
        Ueberfaellig = offen.Count(a => AufgabenRegeln.Ueberfaellig(a, jetzt));
        KuerzelHinweis.Visible = kuerzel.Length == 0;

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        Abschnitte.Clear();

        void Abschnitt(string text)
        {
            Abschnitte.Add(text);
            inhalt.Add(new Label { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, UseMnemonic = false });
        }
        void Karte(Aufgabe a)
        {
            var karte = new AufgabeKarte(a, a.BewohnerId is long id ? namen.GetValueOrDefault(id) : null, jetzt, kuerzel);
            karte.ErledigtGeklickt += x => Ausfuehren(() => speicher.AufgabeErledigt(x.Id, Kuerzel, Jetzt()));
            karte.WeitergebenGeklickt += Weitergeben;
            Karten.Add(karte);
            inhalt.Add(karte);
        }

        Abschnitt($"OFFEN ({offen.Count})");
        offen.ForEach(Karte);
        if (offen.Count == 0)
            inhalt.Add(new Label { Text = "Keine offenen Aufgaben.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        if (ErledigteZeigen.Checked)
        {
            var erledigt = alle.Where(a => !a.Offen).OrderByDescending(a => a.Erledigt).ToList();
            Abschnitt($"ERLEDIGT ({erledigt.Count})");
            erledigt.ForEach(Karte);
        }
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Aufgaben", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    public void NeueAufgabe()
    {
        using var fenster = new AufgabeFenster(speicher, null, null);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Weitergeben(Aufgabe a)
    {
        using var fenster = new WeitergebenFenster(speicher, a);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Drucken()
    {
        var jetzt = Jetzt();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        var druck = new AufgabenDruck(speicher.Aufgaben(jetzt), namen, jetzt);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, "Offene Aufgaben");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Min(ClientSize.Width - 2 * rand, Stil.Px(this, MaxBreite));
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Location = new Point(rand, y);
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is AufgabeKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                y += Stil.Px(this, Stil.AbstandMittel);
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y);
                y += c.Height + Stil.Px(this, Stil.AbstandMini);
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Eine Aufgabe: Marken (überfällig/fällig bis, für wen, jetzt dran), Text, Bewohner und
// Herkunft, Weitergaben mit Notiz, Knöpfe „Erledigt“ und „Weitergeben …“.
public sealed class AufgabeKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label text = new();
    private readonly Label herkunft = new();
    private readonly List<Label> verlauf = [];
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = false, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Aufgabe Aufgabe { get; }
    public Button Erledigt { get; } = Stil.NebenKnopf("Erledigt");
    public Button Weitergeben { get; } = Stil.NebenKnopf("An die nächste Schicht weitergeben …");
    public IReadOnlyList<string> MarkenTexte { get; }
    public IEnumerable<string> VerlaufTexte => verlauf.Select(l => l.Text);

    public event Action<Aufgabe>? ErledigtGeklickt;
    public event Action<Aufgabe>? WeitergebenGeklickt;

    public AufgabeKarte(Aufgabe a, string? bewohner, DateTime jetzt, string kuerzel)
    {
        Aufgabe = a;
        var liste = new List<(string, Stil.Marke)>();
        if (a.Offen)
        {
            if (a.FaelligBis is not null)
                liste.Add((AufgabenRegeln.FaelligText(a, jetzt), AufgabenRegeln.Ueberfaellig(a, jetzt) ? Stil.Beschluss : Stil.Zimmer));
            liste.Add((AufgabenRegeln.FuerText(a), Stil.Zimmer));
            if (AufgabenRegeln.JetztDran(a, jetzt)) liste.Add(("jetzt dran", Stil.Unterwegs));
        }
        else
        {
            liste.Add(($"erledigt {BerichtRegeln.ZeitText(a.Erledigt!.Value)} · {a.ErledigtVon}", Stil.Offen));
        }
        MarkenTexte = liste.Select(m => m.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        text.Text = a.Text;
        text.Font = Stil.KarteName;
        text.ForeColor = a.Offen ? Stil.TextNormal : Stil.TextLeise;
        herkunft.Text = (bewohner is null ? "Wohnbereich" : bewohner) +
                        $" · angelegt von {a.Verfasser}, {BerichtRegeln.ZeitText(a.Erstellt)}" +
                        (a.BerichtId is not null ? " · aus einem Bericht" : "");
        herkunft.Font = Stil.Klein;
        herkunft.ForeColor = Stil.TextLeise;
        foreach (var w in a.Weitergaben)
            verlauf.Add(new Label
            {
                Text = $"weitergegeben von {w.Kuerzel}, {BerichtRegeln.ZeitText(w.Zeit)}" + (w.Notiz.Length > 0 ? $": {w.Notiz}" : ""),
                Font = Stil.Klein, ForeColor = Stil.Hinweis.Text,
            });
        foreach (var label in new[] { text, herkunft }.Concat(verlauf))
        {
            label.AutoSize = true;
            label.BackColor = Stil.Flaeche;
            label.UseMnemonic = false;
            Controls.Add(label);
        }

        Erledigt.Enabled = Weitergeben.Enabled = a.Offen && kuerzel.Length > 0;
        Erledigt.Visible = Weitergeben.Visible = a.Offen;
        Erledigt.Click += (_, _) => ErledigtGeklickt?.Invoke(a);
        Weitergeben.Click += (_, _) => WeitergebenGeklickt?.Invoke(a);
        foreach (var k in new[] { Erledigt, Weitergeben })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"Aufgabe: {a.Text}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Überfällige Aufgaben: roter Balken links (zusätzlich zum Wort „überfällig“).
        if (MarkenTexte.Count > 0 && MarkenTexte[0].StartsWith("überfällig"))
        {
            using var balken = new SolidBrush(Stil.Beschluss.Text);
            e.Graphics.FillRectangle(balken, Stil.Px(this, 2), Stil.Px(this, Stil.RadiusKarte), Stil.Px(this, 4), Height - 2 * Stil.Px(this, Stil.RadiusKarte));
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        marken.MaximumSize = new Size(breite, 0);
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var label in new[] { text, herkunft }.Concat(verlauf))
        {
            label.MaximumSize = new Size(breite, 0);
            label.Location = new Point(innen, y);
            y = label.Bottom + Stil.Px(this, 2);
        }
        if (Aufgabe.Offen)
        {
            knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
            y = knoepfe.Bottom;
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// „Neue Aufgabe“: was, für welchen Bewohner (oder keinen), für wen, bis wann, von wem.
public sealed class AufgabeFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Bewohner> auswahl;
    private readonly long? berichtId;

    public TextBox Inhalt { get; } = new() { Multiline = true, Width = 420, Height = 54, AcceptsReturn = false };
    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    public RadioButton NaechsteSchicht { get; } = new() { AutoSize = true, Checked = true };
    public RadioButton BestimmteSchicht { get; } = new() { Text = "bestimmte Schicht:", AutoSize = true };
    public RadioButton WerZeitHat { get; } = new() { Text = "wer Zeit hat", AutoSize = true };
    public TextBox SchichtTag { get; } = new() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public ComboBox SchichtArt { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    public TextBox FaelligDatum { get; } = new() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public TextBox FaelligUhrzeit { get; } = new() { Width = 70, PlaceholderText = "SS:MM" };
    public TextBox Verfasser { get; } = new() { Width = 80, PlaceholderText = "Kürzel" };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Aufgabe? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    // bewohnerId: vorgewählt (z. B. aus einem Bericht heraus); berichtId: Herkunft.
    public AufgabeFenster(Speicher speicher, long? bewohnerId, long? berichtId)
    {
        this.speicher = speicher;
        this.berichtId = berichtId;
        auswahl = speicher.AlleBewohner();
        Text = $"Neue Aufgabe ({KopfBereich.DemoHinweis})";
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
        FlowLayoutPanel Reihe(params Control[] teile)
        {
            var reihe = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            foreach (var t in teile) t.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
            reihe.Controls.AddRange(teile);
            return reihe;
        }

        Zeile(null, new MarkenFeld(KopfBereich.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Zeile("Was ist zu tun?", Inhalt);
        BewohnerWahl.Items.Add("– kein bestimmter Bewohner –");
        BewohnerWahl.Items.AddRange(auswahl.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
        BewohnerWahl.SelectedIndex = bewohnerId is long id ? auswahl.FindIndex(b => b.Id == id) + 1 : 0;
        Zeile("Bewohner", BewohnerWahl);

        var jetzt = Jetzt();
        NaechsteSchicht.Text = $"nächste Schicht ({SchichtTermin.Zu(jetzt).Naechste().Text})";
        var morgen = SchichtTermin.Zu(jetzt).Naechste();
        SchichtTag.Text = Kern.Datum.Text(morgen.Tag);
        SchichtArt.Items.AddRange(["Frühdienst", "Spätdienst", "Nachtdienst"]);
        SchichtArt.SelectedIndex = (int)morgen.Schicht;
        SchichtTag.Enabled = SchichtArt.Enabled = false;
        // Die drei Knöpfe liegen in verschiedenen Reihen; Windows Forms verbindet sie dann nicht von selbst.
        RadioButton[] fuerKnoepfe = [NaechsteSchicht, BestimmteSchicht, WerZeitHat];
        foreach (var knopf in fuerKnoepfe)
            knopf.CheckedChanged += (_, _) =>
            {
                if (knopf.Checked) foreach (var anderer in fuerKnoepfe) if (anderer != knopf) anderer.Checked = false;
                SchichtTag.Enabled = SchichtArt.Enabled = BestimmteSchicht.Checked;
            };
        var fuer = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        fuer.Controls.Add(NaechsteSchicht);
        fuer.Controls.Add(Reihe(BestimmteSchicht, SchichtTag, SchichtArt));
        fuer.Controls.Add(WerZeitHat);
        Zeile("Für wen?", fuer);
        Zeile("Fällig bis (freiwillig)", Reihe(FaelligDatum, FaelligUhrzeit));
        Verfasser.Text = Einstellungen.Laden().Kuerzel;
        Verfasser.MaxLength = Einstellungen.KuerzelLaenge;
        Zeile("Angelegt von", Verfasser);
        Zeile(null, Fehler);

        var speichern = Stil.HauptKnopf("Speichern");
        speichern.Click += (_, _) => Speichern();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        speichern.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([speichern, abbrechen]);
        Zeile(null, knoepfe);
        Controls.Add(raster);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public AufgabeEingabe Eingabe() => new()
    {
        Text = Inhalt.Text,
        BewohnerId = BewohnerWahl.SelectedIndex > 0 ? auswahl[BewohnerWahl.SelectedIndex - 1].Id : null,
        BerichtId = berichtId,
        Fuer = BestimmteSchicht.Checked ? AufgabeFuer.Schicht : WerZeitHat.Checked ? AufgabeFuer.WerZeitHat : AufgabeFuer.NaechsteSchicht,
        SchichtTag = SchichtTag.Text,
        SchichtArt = (Schicht)Math.Max(0, SchichtArt.SelectedIndex),
        FaelligDatum = FaelligDatum.Text,
        FaelligUhrzeit = FaelligUhrzeit.Text,
        Verfasser = Verfasser.Text,
    };

    public bool Speichern()
    {
        try
        {
            Gespeichert = speicher.AufgabeSpeichern(AufgabenRegeln.AusEingabe(Eingabe(), Jetzt()));
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

// „An die nächste Schicht weitergeben“: mit kurzer Notiz, warum es nicht geschafft wurde.
public sealed class WeitergebenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Aufgabe aufgabe;
    public TextBox Notiz { get; } = new() { Width = 440, MaxLength = 200, PlaceholderText = "kurze Notiz (freiwillig), z. B. „Praxis war schon geschlossen“" };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public WeitergebenFenster(Speicher speicher, Aufgabe aufgabe)
    {
        this.speicher = speicher;
        this.aufgabe = aufgabe;
        Text = $"Aufgabe weitergeben ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        stapel.Controls.Add(new Label { Text = aufgabe.Text, AutoSize = true, MaximumSize = new Size(440, 0), Font = Stil.Fett, UseMnemonic = false });
        stapel.Controls.Add(new Label
        {
            Text = $"geht an die nächste Schicht ({SchichtTermin.Zu(Jetzt()).Naechste().Text})",
            AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandKlein),
        });
        stapel.Controls.Add(Notiz);
        stapel.Controls.Add(Fehler);
        var ok = Stil.HauptKnopf("Weitergeben");
        ok.Click += (_, _) => Uebernehmen();
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([ok, abbrechen]);
        stapel.Controls.Add(knoepfe);
        Controls.Add(stapel);
        CancelButton = abbrechen;
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Uebernehmen()
    {
        try
        {
            speicher.AufgabeWeitergeben(aufgabe.Id, Einstellungen.Laden().Kuerzel, Notiz.Text, Jetzt());
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join("\n", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Ausdruck der offenen Aufgaben (A4 hoch), in der Reihenfolge der Liste.
public sealed class AufgabenDruck(IReadOnlyList<Aufgabe> offen, IReadOnlyDictionary<long, string> namen, DateTime jetzt)
{
    private int naechste;

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Offene Aufgaben" };
        dokument.BeginPrint += (_, _) => naechste = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechste = SeiteZeichnen(e.Graphics!, naechste);
            e.HasMorePages = naechste < offen.Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f, unten = 280f;
        float breite = rechts - links - 6f, y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var normal = new Font("Segoe UI", 10F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        g.DrawString("Offene Aufgaben", titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Stand {BerichtRegeln.ZeitText(jetzt)} · {offen.Count} offen · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y);
        y += 6f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 3f;
        int i = start;
        for (; i < offen.Count; i++)
        {
            var a = offen[i];
            bool ueberfaellig = AufgabenRegeln.Ueberfaellig(a, jetzt);
            string meta = string.Join(" · ", new[]
            {
                a.BewohnerId is long id ? namen.GetValueOrDefault(id, "?") : "Wohnbereich",
                AufgabenRegeln.FuerText(a),
                AufgabenRegeln.FaelligText(a, jetzt),
                $"von {a.Verfasser}",
                a.Weitergaben.Count > 0 ? $"{a.Weitergaben.Count}× weitergegeben" : "",
            }.Where(t => t.Length > 0));
            float hoehe = g.MeasureString(a.Text, normal, new SizeF(breite, 1000f)).Height + 9f;
            if (y + hoehe > unten && i > start) break;
            g.DrawString("☐", normal, Brushes.Black, links, y);
            g.DrawString(a.Text, ueberfaellig ? fett : normal, Brushes.Black, new RectangleF(links + 6f, y, breite, 1000f));
            y += g.MeasureString(a.Text, normal, new SizeF(breite, 1000f)).Height;
            g.DrawString(meta, klein, Brushes.DimGray, links + 6f, y);
            y += 6f;
        }
        if (offen.Count == 0) g.DrawString("Keine offenen Aufgaben.", normal, Brushes.Black, links, y);
        return i;
    }
}

// Kurzliste auf dem Startbildschirm (über den Bewohnerkarten): die ersten offenen Aufgaben,
// überfällige zuerst, und ein Knopf zu allen.
public sealed class AufgabenKurzliste : KartenFlaeche
{
    public const int Hoechstens = 3;
    private readonly Label kopf = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly List<(MarkenFeld Marke, Label Text)> zeilen = [];
    public Button AlleKnopf { get; } = Stil.NebenKnopf("Alle Aufgaben …");
    public IEnumerable<string> ZeilenTexte => zeilen.Select(z => $"{z.Marke.Text} · {z.Text.Text}");
    public string KopfText => kopf.Text;

    public AufgabenKurzliste()
    {
        Controls.Add(kopf);
        Controls.Add(AlleKnopf);
        Padding = new Padding(Stil.AbstandMittel);
    }

    public void Setzen(IReadOnlyList<Aufgabe> offen, IReadOnlyDictionary<long, string> namen, DateTime jetzt)
    {
        foreach (var (m, t) in zeilen) { Controls.Remove(m); Controls.Remove(t); m.Dispose(); t.Dispose(); }
        zeilen.Clear();
        int ueberfaellig = offen.Count(a => AufgabenRegeln.Ueberfaellig(a, jetzt));
        kopf.Text = $"AUFGABEN · {offen.Count} OFFEN" + (ueberfaellig > 0 ? $" · {ueberfaellig} ÜBERFÄLLIG" : "");
        foreach (var a in offen.Take(Hoechstens))
        {
            var (markenText, marke) = AufgabenRegeln.Ueberfaellig(a, jetzt) ? ("überfällig", Stil.Beschluss)
                : AufgabenRegeln.JetztDran(a, jetzt) ? ("jetzt dran", Stil.Unterwegs)
                : (AufgabenRegeln.FuerText(a), Stil.Zimmer);
            var feld = new MarkenFeld(markenText, marke);
            var text = new Label
            {
                Text = a.Text + (a.BewohnerId is long id && namen.TryGetValue(id, out var n) ? $" ({n})" : ""),
                AutoSize = true, AutoEllipsis = true, BackColor = Stil.Flaeche, ForeColor = Stil.TextNormal, UseMnemonic = false,
            };
            zeilen.Add((feld, text));
            Controls.Add(feld);
            Controls.Add(text);
        }
        if (offen.Count > Hoechstens) AlleKnopf.Text = $"Alle {offen.Count} Aufgaben …";
        else AlleKnopf.Text = "Alle Aufgaben …";
        AccessibleName = $"{kopf.Text}: {string.Join("; ", ZeilenTexte)}";
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandMittel), x = innen, y = innen;
        AlleKnopf.Location = new Point(Width - innen - AlleKnopf.Width, y - Stil.Px(this, 2));
        kopf.Location = new Point(x, y + Stil.Px(this, Stil.AbstandMini));
        y = Math.Max(kopf.Bottom, AlleKnopf.Bottom - Stil.Px(this, Stil.AbstandKlein)) + Stil.Px(this, Stil.AbstandMini);
        int markenBreite = zeilen.Count == 0 ? 0 : zeilen.Max(z => z.Marke.PreferredSize.Width);
        foreach (var (m, t) in zeilen)
        {
            m.Size = new Size(markenBreite, m.PreferredSize.Height);
            m.Location = new Point(x, y);
            t.AutoSize = false;
            t.Bounds = new Rectangle(x + markenBreite + Stil.Px(this, Stil.AbstandKlein), y, Math.Max(50, Width - 2 * innen - markenBreite - Stil.Px(this, Stil.AbstandKlein)), m.Height);
            t.TextAlign = ContentAlignment.MiddleLeft;
            y += m.Height + Stil.Px(this, Stil.AbstandMini);
        }
        int hoehe = y + innen - Stil.Px(this, Stil.AbstandMini);
        if (Height != hoehe) Height = hoehe;
    }
}
