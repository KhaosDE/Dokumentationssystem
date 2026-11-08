using System.Drawing.Printing;
using Dokusystem.Kern;

namespace Dokusystem;

// Bereich „Übergabe“: nur die Übergabe-Einträge, keine übrigen Berichte. Oben „Wohnbereich
// allgemein“, darunter nach Bewohner gruppiert. Einträge bleiben stehen, bis sie ablaufen oder
// erledigt sind; gelöscht wird nichts (erledigte und abgelaufene lassen sich einblenden).
public sealed class UebergabeSeite : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];
    private DateTime? vergleich;   // letzter Besuch vor diesem (für „neu“)

    public Button NotizKnopf { get; } = Stil.NebenKnopf("Notiz für den Wohnbereich …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Übergabeblatt drucken …");
    public CheckBox ErledigteZeigen { get; } = new() { Text = "Erledigte und abgelaufene zeigen", AutoSize = true, BackColor = Stil.Seite };
    public Label KuerzelHinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public List<UebergabeKarte> Karten { get; } = [];
    public List<string> Abschnitte { get; } = [];
    public int Offen { get; private set; }
    public int Neu { get; private set; }
    public event Action? Geaendert;   // nach jedem Neuaufbau (z. B. für den Kopftext)

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public UebergabeSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        foreach (var c in new Control[] { NotizKnopf, DruckKnopf, ErledigteZeigen, KuerzelHinweis })
        {
            c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        ErledigteZeigen.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        KuerzelHinweis.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        KuerzelHinweis.Text = "Für „gelesen“ und „erledigt“ bitte unter Einstellungen ein Kürzel eintragen.";
        NotizKnopf.Click += (_, _) => NotizSchreiben();
        DruckKnopf.Click += (_, _) => Drucken();
        ErledigteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(leiste);
    }

    private static string Kuerzel => Einstellungen.Laden().Kuerzel;

    // Beim Öffnen des Bereichs: Was nach dem letzten Besuch mit diesem Kürzel kam, gilt als „neu“.
    public void Besuch()
    {
        vergleich = speicher.LetzterUebergabeBesuch(Kuerzel);
        speicher.UebergabeBesuchMerken(Kuerzel, Jetzt());
        Aktualisieren();
    }

    public string KopfText => $"{(Offen == 1 ? "1 offener Eintrag" : $"{Offen} offene Einträge")}{(Neu > 0 ? $" · {Neu} neu" : "")}";

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        string kuerzel = Kuerzel;
        var alle = speicher.Uebergaben(jetzt, auchErledigte: true);
        var offen = alle.Where(u => UebergabeRegeln.Offen(u, jetzt)).ToList();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Nachname.ToUpperInvariant()}, {b.Vorname.ToUpperInvariant()} · ZI. {b.Zimmer}");
        Offen = offen.Count;
        Neu = offen.Count(u => UebergabeRegeln.Neu(u, kuerzel, vergleich));
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
        void Karte(Uebergabe u)
        {
            var karte = new UebergabeKarte(u, jetzt, kuerzel, vergleich);
            karte.GelesenGeklickt += x => Ausfuehren(() => speicher.UebergabeGelesen(x.Id, Kuerzel, Jetzt()));
            karte.ErledigtGeklickt += x => Ausfuehren(() => speicher.UebergabeErledigt(x.Id, Kuerzel, Jetzt()));
            karte.VerlaengernGeklickt += Verlaengern;
            Karten.Add(karte);
            inhalt.Add(karte);
        }

        var allgemein = offen.Where(u => u.Allgemein).ToList();
        if (allgemein.Count > 0)
        {
            Abschnitt("WOHNBEREICH ALLGEMEIN");
            allgemein.ForEach(Karte);
        }
        foreach (var gruppe in offen.Where(u => !u.Allgemein).GroupBy(u => u.BewohnerId!.Value)
                     .OrderBy(g => namen.GetValueOrDefault(g.Key, "?"), StringComparer.CurrentCulture))
        {
            Abschnitt(namen.GetValueOrDefault(gruppe.Key, "?"));
            foreach (var u in gruppe) Karte(u);
        }
        if (offen.Count == 0)
            inhalt.Add(new Label { Text = "Keine offenen Übergabe-Einträge.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        if (ErledigteZeigen.Checked)
        {
            var rest = alle.Where(u => !UebergabeRegeln.Offen(u, jetzt)).ToList();
            Abschnitt($"ERLEDIGT / ABGELAUFEN ({rest.Count})");
            rest.ForEach(Karte);
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
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Übergabe", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    public void Verlaengern(Uebergabe u)
    {
        using var fenster = new DauerFenster("Übergabe verlängern", "Ab jetzt gültig:", null);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK && fenster.Wunsch is UebergabeWunsch wunsch)
            Ausfuehren(() => speicher.UebergabeVerlaengern(u.Id, wunsch, Kuerzel, Jetzt()));
    }

    public void NotizSchreiben()
    {
        using var fenster = new DauerFenster("Notiz für den Wohnbereich", "Wie lange?", (text, wunsch) =>
            speicher.WohnbereichNotiz(text, Kuerzel, wunsch, Jetzt()));
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Drucken()
    {
        var jetzt = Jetzt();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        var druck = new UebergabeDruck(speicher.Uebergaben(jetzt), namen, jetzt);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, "Übergabeblatt");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Location = new Point(rand, y);
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is UebergabeKarte karte)
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

// Ein Übergabe-Eintrag: Marken (neu, läuft heute ab, Gültigkeit), der Übergabe-Text (kurzer Satz
// oder der Bericht), Herkunft, wer gelesen hat, und die Knöpfe „Gelesen“, „Verlängern …“, „Erledigt“.
public sealed class UebergabeKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label text = new();
    private readonly Label herkunft = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = false, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Uebergabe Uebergabe { get; }
    public Label GelesenText { get; } = new();
    public Button Gelesen { get; } = Stil.NebenKnopf("Gelesen");
    public Button Verlaengern { get; } = Stil.NebenKnopf("Verlängern …");
    public Button Erledigt { get; } = Stil.NebenKnopf("Erledigt");
    public Button BerichtLesen { get; } = Stil.NebenKnopf("Bericht lesen …");
    public IReadOnlyList<string> MarkenTexte { get; }
    private readonly bool hatKnoepfe;

    public event Action<Uebergabe>? GelesenGeklickt;
    public event Action<Uebergabe>? VerlaengernGeklickt;
    public event Action<Uebergabe>? ErledigtGeklickt;

    public UebergabeKarte(Uebergabe u, DateTime jetzt, string kuerzel, DateTime? letzterBesuch)
    {
        Uebergabe = u;
        bool offen = UebergabeRegeln.Offen(u, jetzt);
        var liste = new List<(string, Stil.Marke)>();
        if (offen && UebergabeRegeln.Neu(u, kuerzel, letzterBesuch)) liste.Add(("neu seit deiner letzten Schicht", Stil.Unterwegs));
        if (UebergabeRegeln.LaeuftHeuteAb(u, jetzt)) liste.Add(("läuft heute ab", Stil.Achtung));
        liste.Add((UebergabeRegeln.GueltigText(u, jetzt) + (offen && u.Verlaengert > 0 ? " (verlängert)" : ""), Stil.Zimmer));
        MarkenTexte = liste.Select(m => m.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        text.Text = u.Anzeige;
        text.Font = u.Text.Length > 0 ? Stil.KarteName : Stil.Standard;
        text.ForeColor = offen ? Stil.TextNormal : Stil.TextLeise;
        herkunft.Text = u.Bericht is Bericht b
            ? $"aus dem Bericht vom {BerichtRegeln.ZeitText(b.Zeitpunkt)} · {BerichtRegeln.SchichtText(b.Schicht)} · {b.Verfasser}"
            : $"Notiz von {u.Verfasser}, {BerichtRegeln.ZeitText(u.Erstellt)}";
        herkunft.Font = Stil.Klein;
        herkunft.ForeColor = Stil.TextLeise;
        GelesenText.Text = u.Gelesen.Count == 0 ? "noch von niemandem als gelesen bestätigt"
            : "gelesen: " + string.Join(", ", u.Gelesen.Select(g => $"{g.Kuerzel} ({g.Zeit:dd.MM., HH:mm})"));
        GelesenText.Font = Stil.Klein;
        GelesenText.ForeColor = Stil.TextMittel;
        foreach (var label in new[] { text, herkunft, GelesenText })
        {
            label.AutoSize = true;
            label.BackColor = Stil.Flaeche;
            label.UseMnemonic = false;
            Controls.Add(label);
        }

        bool schonGelesen = UebergabeRegeln.GelesenVon(u, kuerzel);
        Gelesen.Text = schonGelesen ? "Gelesen ✓" : "Gelesen";
        Gelesen.Enabled = offen && kuerzel.Length > 0 && !schonGelesen;
        Verlaengern.Enabled = offen && kuerzel.Length > 0;
        Erledigt.Enabled = offen && kuerzel.Length > 0;
        Gelesen.Click += (_, _) => GelesenGeklickt?.Invoke(u);
        Verlaengern.Click += (_, _) => VerlaengernGeklickt?.Invoke(u);
        Erledigt.Click += (_, _) => ErledigtGeklickt?.Invoke(u);
        bool lesen = u.Bericht is not null && u.Text.Length > 0;   // Bericht per Klick lesbar, wenn ein kurzer Satz davorsteht
        BerichtLesen.Visible = lesen;
        hatKnoepfe = offen || lesen;
        BerichtLesen.Click += (_, _) => MessageBox.Show(FindForm(),
            $"{herkunft.Text}\n\n{u.Bericht?.Text}", "Bericht", MessageBoxButtons.OK, MessageBoxIcon.None);
        foreach (var k in new[] { Gelesen, Verlaengern, Erledigt, BerichtLesen })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
            if (k != BerichtLesen) k.Visible = offen;   // erledigt oder abgelaufen: nur noch lesen
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"Übergabe: {u.Anzeige}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        marken.MaximumSize = new Size(breite, 0);
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        text.MaximumSize = new Size(breite, 0);
        text.Location = new Point(innen, y);
        y = text.Bottom + Stil.Px(this, Stil.AbstandMini);
        herkunft.MaximumSize = new Size(breite, 0);
        herkunft.Location = new Point(innen, y);
        y = herkunft.Bottom + Stil.Px(this, 2);
        GelesenText.MaximumSize = new Size(breite, 0);
        GelesenText.Location = new Point(innen, y);
        y = GelesenText.Bottom;
        if (hatKnoepfe)
        {
            knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
            y = knoepfe.Bottom;
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Kleines Fenster: „Wie lange?“ (nächste Schicht, 3 Tage, 1 Woche, bis Datum, bis erledigt) –
// zum Verlängern, oder mit Textfeld für eine Notiz an den Wohnbereich.
public sealed class DauerFenster : Form
{
    private readonly Func<string, UebergabeWunsch, Uebergabe>? notizSpeichern;
    public ComboBox Dauer { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    public TextBox Bis { get; } = new() { Width = 110, PlaceholderText = "TT.MM.JJJJ", Enabled = false };
    public TextBox Inhalt { get; } = new() { Multiline = true, Width = 440, Height = 70 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public UebergabeWunsch? Wunsch { get; private set; }

    // notizSpeichern == null: nur die Dauer wählen (Verlängern). Sonst mit Textfeld (Notiz).
    public DauerFenster(string titel, string frage, Func<string, UebergabeWunsch, Uebergabe>? notizSpeichern)
    {
        this.notizSpeichern = notizSpeichern;
        Text = $"{titel} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Stil.AbstandGross), Dock = DockStyle.Fill };
        if (notizSpeichern is not null)
        {
            stapel.Controls.Add(new Label { Text = "Notiz für alle (betrifft keinen einzelnen Bewohner):", AutoSize = true, ForeColor = Stil.TextMittel });
            stapel.Controls.Add(Inhalt);
        }
        Dauer.Items.AddRange(UebergabeRegeln.DauerTexte);
        Dauer.SelectedIndex = notizSpeichern is null ? (int)UebergabeDauer.DreiTage : (int)UebergabeDauer.NaechsteSchicht;
        Dauer.SelectedIndexChanged += (_, _) => Bis.Enabled = Dauer.SelectedIndex == (int)UebergabeDauer.BisDatum;
        var zeile = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        zeile.Controls.Add(new Label { Text = frage, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) });
        Dauer.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, 0);
        Bis.Margin = new Padding(0, Stil.AbstandMini, 0, 0);
        zeile.Controls.AddRange([Dauer, Bis]);
        stapel.Controls.Add(zeile);
        stapel.Controls.Add(Fehler);
        var ok = Stil.HauptKnopf(notizSpeichern is null ? "Verlängern" : "Notiz speichern");
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
            var dauer = (UebergabeDauer)Dauer.SelectedIndex;
            DateOnly? bis = dauer == UebergabeDauer.BisDatum ? Datum.Lesen(Bis.Text, "Bis") : null;
            Wunsch = new UebergabeWunsch(dauer, bis);
            notizSpeichern?.Invoke(Inhalt.Text, Wunsch);
        }
        catch (Exception e) when (e is Eingabefehler or FormatException)
        {
            Fehler.Text = e is Eingabefehler f ? string.Join("\n", f.Fehler) : e.Message;
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Übergabeblatt (A4 hoch): nur die offenen Einträge, Wohnbereich allgemein zuerst, dann nach Bewohner.
public sealed class UebergabeDruck(IReadOnlyList<Uebergabe> offen, IReadOnlyDictionary<long, string> namen, DateTime jetzt)
{
    private int naechster;
    private List<(string? Abschnitt, Uebergabe U)> Zeilen => [
        .. offen.Where(u => u.Allgemein).Select(u => ((string?)"Wohnbereich allgemein", u)),
        .. offen.Where(u => !u.Allgemein).OrderBy(u => namen.GetValueOrDefault(u.BewohnerId!.Value, "?"), StringComparer.CurrentCulture)
                .Select(u => ((string?)namen.GetValueOrDefault(u.BewohnerId!.Value, "?"), u)),
    ];

    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = "Übergabeblatt" };
        dokument.BeginPrint += (_, _) => naechster = 0;
        dokument.PrintPage += (_, e) =>
        {
            naechster = SeiteZeichnen(e.Graphics!, naechster);
            e.HasMorePages = naechster < Zeilen.Count;
        };
        return dokument;
    }

    public int SeiteZeichnen(Graphics g, int start)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f, unten = 280f;
        float breite = rechts - links, y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var normal = new Font("Segoe UI", 10F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        var zeilen = Zeilen;
        g.DrawString("Übergabeblatt – offene Einträge", titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Stand {BerichtRegeln.ZeitText(jetzt)} · {zeilen.Count} offen · {KopfBereich.DemoHinweis}", klein, Brushes.DimGray, links, y);
        y += 6f;
        g.DrawLine(linie, links, y, rechts, y);
        y += 3f;
        string? letzterAbschnitt = null;
        int i = start;
        for (; i < zeilen.Count; i++)
        {
            var (abschnitt, u) = zeilen[i];
            string meta = $"{UebergabeRegeln.GueltigText(u, jetzt)} · von {u.Verfasser}" +
                          (u.Gelesen.Count > 0 ? $" · gelesen: {string.Join(", ", u.Gelesen.Select(x => x.Kuerzel))}" : "");
            float hoehe = g.MeasureString(u.Anzeige, normal, new SizeF(breite, 1000f)).Height + 9f + (abschnitt != letzterAbschnitt ? 7f : 0f);
            if (y + hoehe > unten && i > start) break;
            if (abschnitt != letzterAbschnitt)
            {
                g.DrawString(abschnitt, fett, Brushes.Black, links, y);
                y += 6f;
                letzterAbschnitt = abschnitt;
            }
            g.DrawString("☐", normal, Brushes.Black, links, y);
            g.DrawString(u.Anzeige, normal, Brushes.Black, new RectangleF(links + 6f, y, breite - 6f, 1000f));
            y += g.MeasureString(u.Anzeige, normal, new SizeF(breite - 6f, 1000f)).Height;
            g.DrawString(meta, klein, Brushes.DimGray, links + 6f, y);
            y += 6f;
        }
        if (zeilen.Count == 0) g.DrawString("Keine offenen Übergabe-Einträge.", normal, Brushes.Black, links, y);
        return i;
    }
}
