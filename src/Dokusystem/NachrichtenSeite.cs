using Dokusystem.Kern;

namespace Dokusystem;

// Bereich „Nachrichten“: Posteingang, Gesendet, Archiv – jeweils Verläufe (erste Nachricht und
// Antworten). Ein Klick öffnet den Verlauf; dort antworten oder archivieren. Alles bleibt in der
// Datenbank des Programms (kein E-Mail-Versand, kein Internet).
public sealed class NachrichtenSeite : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public Dictionary<NachrichtenOrdner, FilterKnopf> Ordner { get; } = [];
    public NachrichtenOrdner AktiverOrdner { get; private set; } = NachrichtenOrdner.Posteingang;
    public long? BewohnerFilter { get; private set; }
    public Button BewohnerFilterKnopf { get; } = Stil.NebenKnopf("");
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neue Nachricht …");
    public Label Hinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public List<VerlaufKarte> Karten { get; } = [];
    public VerlaufAnsicht? Offen { get; private set; }
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public NachrichtenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        foreach (var o in Enum.GetValues<NachrichtenOrdner>())
        {
            var knopf = new FilterKnopf(NachrichtenRegeln.OrdnerText(o)) { Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein) };
            knopf.Click += (_, _) => OrdnerWaehlen(o);
            Ordner[o] = knopf;
            leiste.Controls.Add(knopf);
        }
        BewohnerFilterKnopf.Margin = new Padding(Stil.AbstandMittel, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        BewohnerFilterKnopf.Click += (_, _) => { BewohnerFilter = null; Aktualisieren(); };
        Hinweis.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        leiste.Controls.AddRange([BewohnerFilterKnopf, Hinweis]);
        NeuKnopf.Click += (_, _) => NeueNachricht();
        Controls.Add(leiste);
    }

    public static string Ich => Einstellungen.Laden().Kuerzel;

    public string KopfText
    {
        get
        {
            var ich = speicher.Kollege(Ich);
            if (ich is null) return "Nicht angemeldet";
            int n = speicher.UngeleseneNachrichten(ich.Kuerzel);
            return $"{ich.Name}" + (n > 0 ? $" · {n} ungelesen" : " · nichts Ungelesenes");
        }
    }

    public void OrdnerWaehlen(NachrichtenOrdner ordner)
    {
        AktiverOrdner = ordner;
        Offen = null;
        Aktualisieren();
    }

    // Aus der Akte: nur Verläufe zu diesem Bewohner (Knopf „Bewohner: … ✕“ hebt das auf).
    public void BewohnerZeigen(long? bewohnerId)
    {
        BewohnerFilter = bewohnerId;
        Offen = null;
        Aktualisieren();
    }

    private Dictionary<string, Kollege> KollegenNachKuerzel() => speicher.Kollegen().ToDictionary(k => k.Kuerzel);

    public void Aktualisieren()
    {
        var ich = speicher.Kollege(Ich);
        var kollegen = KollegenNachKuerzel();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        foreach (var (o, knopf) in Ordner)
        {
            knopf.Aktiv = o == AktiverOrdner && Offen is null;
            knopf.Anzahl = ich is null ? 0 : speicher.Verlaeufe(ich.Kuerzel, o, BewohnerFilter).Count;
        }
        BewohnerFilterKnopf.Visible = BewohnerFilter is long;
        if (BewohnerFilter is long b) BewohnerFilterKnopf.Text = $"Bewohner: {namen.GetValueOrDefault(b, "?")}  ✕";
        Hinweis.Visible = ich is null;
        Hinweis.Text = "Bitte oben rechts unter „Angemeldet als …“ einen Benutzer wählen – Nachrichten gehören zu einer Person.";
        NeuKnopf.Enabled = ich is not null;

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        if (ich is not null && Offen is VerlaufAnsicht offen && speicher.VerlaufLesen(ich.Kuerzel, offen.Verlauf.Id) is Verlauf neu)
        {
            Offen = VerlaufOeffnenIntern(neu, ich, kollegen, namen);
        }
        else if (ich is not null)
        {
            Offen = null;
            var verlaeufe = speicher.Verlaeufe(ich.Kuerzel, AktiverOrdner, BewohnerFilter);
            foreach (var v in verlaeufe)
            {
                var karte = new VerlaufKarte(v, ich, kollegen, v.BewohnerId is long id ? namen.GetValueOrDefault(id) : null);
                karte.Gewaehlt += VerlaufOeffnen;
                Karten.Add(karte);
                inhalt.Add(karte);
            }
            if (verlaeufe.Count == 0)
                inhalt.Add(new Label { Text = $"Keine Nachrichten unter „{NachrichtenRegeln.OrdnerText(AktiverOrdner)}“.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        }
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    // Öffnet einen Verlauf; was darin an mich ging, gilt danach als gelesen.
    public void VerlaufOeffnen(Verlauf v)
    {
        if (speicher.Kollege(Ich) is not Kollege ich) return;
        speicher.VerlaufGelesen(ich.Kuerzel, v.Id, Jetzt());
        Offen = new VerlaufAnsicht(v);   // Platzhalter; Aktualisieren baut die Ansicht neu auf
        Aktualisieren();
    }

    private VerlaufAnsicht VerlaufOeffnenIntern(Verlauf v, Kollege ich, Dictionary<string, Kollege> kollegen, Dictionary<long, string> namen)
    {
        var ansicht = new VerlaufAnsicht(v, ich, kollegen, v.BewohnerId is long id ? namen.GetValueOrDefault(id) : null);
        ansicht.ZurueckKnopf.Click += (_, _) => OrdnerWaehlen(AktiverOrdner);
        ansicht.AntwortenKnopf.Click += (_, _) => Antworten(ansicht.Verlauf);
        ansicht.ArchivKnopf.Click += (_, _) =>
        {
            if (ansicht.Verlauf.Archiviert) speicher.VerlaufZurueckholen(ich.Kuerzel, ansicht.Verlauf.Id);
            else speicher.VerlaufArchivieren(ich.Kuerzel, ansicht.Verlauf.Id, Jetzt());
            OrdnerWaehlen(AktiverOrdner);
        };
        inhalt.Add(ansicht);
        return ansicht;
    }

    public void NeueNachricht()
    {
        if (speicher.Kollege(Ich) is not Kollege ich) return;
        using var fenster = new NachrichtFenster(speicher, ich, BewohnerFilter, null);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK && fenster.Gesendet is Nachricht n)
        {
            AktiverOrdner = NachrichtenOrdner.Gesendet;
            VerlaufOeffnen(speicher.VerlaufLesen(ich.Kuerzel, n.Verlauf)!);
        }
    }

    public void Antworten(Verlauf v)
    {
        if (speicher.Kollege(Ich) is not Kollege ich) return;
        using var fenster = new NachrichtFenster(speicher, ich, v.BewohnerId, v);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) VerlaufOeffnen(speicher.VerlaufLesen(ich.Kuerzel, v.Id)!);
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
            if (c is KartenFlaeche or VerlaufAnsicht)
            {
                c.SetBounds(rand, y, breite, c.Height);
                c.PerformLayout();
                y += c.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandKlein));
                y = c.Bottom;
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}

// Ein Verlauf in der Liste: Betreff (fett, wenn etwas ungelesen ist), Beteiligte, letzte
// Nachricht als Vorschau, Zeit; Marken „N neu“ und Bewohner.
public sealed class VerlaufKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label betreff = new();
    private readonly Label zeile = new();
    private readonly Label vorschau = new();

    public Verlauf Verlauf { get; }
    public IReadOnlyList<string> MarkenTexte { get; }
    public string Vorschau => vorschau.Text;
    public event Action<Verlauf>? Gewaehlt;

    public VerlaufKarte(Verlauf v, Kollege ich, IReadOnlyDictionary<string, Kollege> kollegen, string? bewohner)
    {
        Verlauf = v;
        Cursor = Cursors.Hand;
        var liste = new List<(string, Stil.Marke)>();
        if (v.Ungelesen > 0) liste.Add((v.Ungelesen == 1 ? "1 neu" : $"{v.Ungelesen} neu", Stil.Unterwegs));
        if (bewohner is not null) liste.Add(($"Bewohner: {bewohner}", Stil.Zimmer));
        if (v.Nachrichten.Count > 1) liste.Add(($"{v.Nachrichten.Count} Nachrichten", Stil.Zimmer));
        MarkenTexte = liste.Select(m => m.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        var letzte = v.Nachrichten[^1];
        string Name(string k) => k == ich.Kuerzel ? "ich" : kollegen.TryGetValue(k, out var x) ? x.Name : k;
        betreff.Text = v.Betreff;
        betreff.Font = v.Ungelesen > 0 ? Stil.KarteName : Stil.Fett;
        betreff.ForeColor = Stil.TextNormal;
        zeile.Text = $"{Name(v.Nachrichten[0].Von)} an {NachrichtenRegeln.EmpfaengerText(v.Nachrichten[0].An, kollegen)} · zuletzt {BerichtRegeln.ZeitText(letzte.Gesendet)}";
        zeile.Font = Stil.Klein;
        zeile.ForeColor = Stil.TextLeise;
        vorschau.Text = $"{Name(letzte.Von)}: {letzte.Text}";
        vorschau.AutoEllipsis = true;
        vorschau.ForeColor = Stil.TextMittel;
        foreach (var label in new[] { betreff, zeile, vorschau })
        {
            label.AutoSize = label != vorschau;
            label.BackColor = Stil.Flaeche;
            label.UseMnemonic = false;
            label.Cursor = Cursors.Hand;
            label.Click += (_, _) => Waehlen();
            Controls.Add(label);
        }
        marken.Click += (_, _) => Waehlen();
        Controls.Add(marken);
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = $"{v.Betreff}, {string.Join(", ", MarkenTexte)}. Öffnen";
    }

    public void Waehlen() => Gewaehlt?.Invoke(Verlauf);

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Waehlen();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Verlauf is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        if (marken.Controls.Count > 0)
        {
            marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
            y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        }
        betreff.MaximumSize = new Size(breite, 0);
        betreff.Location = new Point(innen, y);
        y = betreff.Bottom + Stil.Px(this, 2);
        zeile.MaximumSize = new Size(breite, 0);
        zeile.Location = new Point(innen, y);
        y = zeile.Bottom + Stil.Px(this, Stil.AbstandMini);
        vorschau.Bounds = new Rectangle(innen, y, breite, vorschau.PreferredHeight);
        int hoehe = vorschau.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Ein geöffneter Verlauf: alle Nachrichten der Reihe nach, darunter Antworten und Archivieren.
public sealed class VerlaufAnsicht : Panel
{
    private readonly List<KartenFlaeche> karten = [];
    private readonly Label betreff = new() { AutoSize = true, Font = Stil.KopfTitel, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };

    public Verlauf Verlauf { get; }
    public Button ZurueckKnopf { get; } = Stil.NebenKnopf("← Zurück");
    public Button AntwortenKnopf { get; } = Stil.HauptKnopf("Antworten …");
    public Button ArchivKnopf { get; } = Stil.NebenKnopf("Archivieren");
    public List<string> Texte { get; } = [];

    // Nur als Merker, welcher Verlauf offen ist (ohne Inhalt).
    public VerlaufAnsicht(Verlauf v) => Verlauf = v;

    public VerlaufAnsicht(Verlauf v, Kollege ich, IReadOnlyDictionary<string, Kollege> kollegen, string? bewohner)
    {
        Verlauf = v;
        BackColor = Stil.Seite;
        betreff.Text = v.Betreff;
        Controls.Add(betreff);
        ArchivKnopf.Text = v.Archiviert ? "Zurück in den Posteingang" : "Archivieren";
        foreach (var k in new[] { ZurueckKnopf, ArchivKnopf, AntwortenKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(knoepfe);
        if (bewohner is not null)
            Controls.Add(new MarkenFeld($"Bewohner: {bewohner}", Stil.Zimmer) { Tag = "bewohner" });
        string Name(string kuerzel) => kollegen.TryGetValue(kuerzel, out var x) ? x.Anzeige : kuerzel;
        foreach (var n in v.Nachrichten)
        {
            var karte = new KartenFlaeche();
            var kopf = new Label
            {
                Text = $"{Name(n.Von)}{(n.Von == ich.Kuerzel ? " – ich" : "")} · {BerichtRegeln.ZeitText(n.Gesendet)}",
                Font = Stil.Fett, ForeColor = Stil.TextNormal,
            };
            var an = new Label { Text = $"an {NachrichtenRegeln.EmpfaengerText(n.An, kollegen)}", Font = Stil.Klein, ForeColor = Stil.TextLeise };
            var text = new Label { Text = n.Text, Font = Stil.Standard, ForeColor = Stil.TextNormal };
            foreach (var l in new[] { kopf, an, text })
            {
                l.AutoSize = true;
                l.BackColor = Stil.Flaeche;
                l.UseMnemonic = false;
                karte.Controls.Add(l);
            }
            Texte.Add(n.Text);
            karten.Add(karte);
            Controls.Add(karte);
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = Math.Max(200, Width), y = 0;
        betreff.MaximumSize = new Size(breite, 0);
        betreff.Location = new Point(0, y);
        y = betreff.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (Control c in Controls)
            if (c is MarkenFeld { Tag: "bewohner" } m)
            {
                m.Location = new Point(Stil.Px(this, 2), y);
                y = m.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
        int innen = Stil.Px(this, Stil.AbstandGross);
        foreach (var karte in karten)
        {
            karte.SetBounds(0, y, breite, karte.Height);
            int ky = innen;
            foreach (Label l in karte.Controls)
            {
                l.MaximumSize = new Size(breite - 2 * innen, 0);
                l.Location = new Point(innen, ky);
                ky = l.Bottom + Stil.Px(this, l.Font == Stil.Klein ? Stil.AbstandKlein : 2);
            }
            karte.Height = ky + innen - Stil.Px(this, 2);
            y = karte.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        knoepfe.MaximumSize = new Size(breite, 0);
        knoepfe.Bounds = new Rectangle(new Point(0, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int hoehe = knoepfe.Bottom;
        if (Height != hoehe) Height = hoehe;
    }
}

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
