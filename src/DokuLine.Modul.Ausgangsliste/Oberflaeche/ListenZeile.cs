using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Eine Zeile der Ausgangsliste am Bildschirm, einzeilig, damit alle Bewohner des Bereichs ohne Bildlauf passen:
// Zimmer · Bewohner · Status (im Haus, unterwegs, abwesend …) · Rückkehr. Rechts „raus ▾“ (allein, mit Bewohner) bzw.
// „zurück“ und „…“ (Ausgang, Anwesenheit, Unterbringung, Verlauf; auch per Rechtsklick). Doppelklick: „raus“ bzw.
// „zurück“. Unterbringung und Fristen stehen nicht in der Liste (Akte, Fenster „Unterbringung“). Wer unterwegs ist,
// ist hinterlegt und hat links einen Balken; Überfällige sind rot und tragen das Wort „überfällig“.
public sealed class ListenZeile : Panel
{
    public static readonly string[] Spalten = ["Zi.", "Bewohner", "Status", "Rückkehr"];
    private static readonly int[] Anteile = [8, 34, 44, 14];

    private readonly Label[] zellen;
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
    // Die Menüs entstehen erst beim ersten Öffnen: 23 Zeilen mit je zwei Menüs kosteten sonst merklich Zeit beim Aufbau.
    private ContextMenuStrip? menue, rausMenue;
    private readonly Color balken;

    public Platz Platz { get; }
    public IReadOnlyList<string> MarkenTexte { get; }
    public string AusgangZeile { get; }
    public string UnterbringungZeile { get; }
    public string HeuteText { get; }
    public string StandText { get; }
    public string RueckkehrText { get; }
    public bool Unterwegs { get; }
    public bool Ueberfaellig { get; }
    public bool Abgaengig { get; }
    public bool AbgaengigkeitFrage { get; }   // Rückkehrzeit länger als die Wartezeit überschritten
    public Button ZurueckKnopf { get; } = Stil.KleinerKnopf("zurück", haupt: true);
    public Button RausKnopf { get; } = Stil.KleinerKnopf("raus ▾");
    public Button MehrKnopf { get; } = Stil.KleinerKnopf("…");
    public ToolStripMenuItem AlleinKnopf { get; } = new("raus allein");
    public ToolStripMenuItem MitBewohnerKnopf { get; } = new("raus mit Bewohner");
    public ToolStripMenuItem AusgangEintrag { get; } = new("Ausgang ändern …");
    public ToolStripMenuItem AnwesenheitEintrag { get; } = new("Anwesenheit …");
    public ToolStripMenuItem UnterbringungEintrag { get; } = new("Unterbringung …");
    public ToolStripMenuItem VerlaufEintrag { get; } = new("Verlauf …");
    public ToolStripMenuItem AbgaengigkeitEintrag { get; } = new("Abgängigkeit melden …");
    public Button AbgaengigkeitKnopf { get; } = Stil.KleinerKnopf("melden …");

    // Gemeinsame Breite des Knopfbereichs aller Zeilen (setzt die Seite), damit die Spalten fluchten.
    public int KnopfBreite { get; set; }
    public int EigeneKnopfBreite => knoepfe.GetPreferredSize(Size.Empty).Width;
    public static int Hoehe(Control bezug) => Stil.Px(bezug, 32);

    // digital: null = Untermodul „Digitaler Ausgang“ ausgeschaltet.
    public ListenZeile(Platz p, DateTime jetzt, DigitalStand? digital, bool kueche, int wartezeit = AbgaengigkeitRegeln.WartezeitVorgabe, Abgaengigkeit? abgaengig = null)
    {
        Platz = p;
        DoubleBuffered = true;
        ResizeRedraw = true;
        var marken = AusgangsMarken.Fuer(p, jetzt, digital, alle: true, kueche: kueche, wartezeit: wartezeit, abgaengig: abgaengig);
        MarkenTexte = marken.Select(m => m.Text).ToList();
        Unterwegs = digital?.Laufend is not null;
        Ueberfaellig = Unterwegs && digital!.Ueberfaellig;
        Abgaengig = abgaengig is not null;
        AbgaengigkeitFrage = !Abgaengig && Ueberfaellig && digital!.ZurueckUm is DateTime grenze && AbgaengigkeitRegeln.WartezeitVorbei(grenze, jetzt, wartezeit);
        AusgangZeile = PlatzTexte.Ausgang(p, jetzt);
        UnterbringungZeile = PlatzTexte.Unterbringung(p, jetzt);
        var heute = new List<string>();
        if (digital is not null)
        {
            heute.AddRange(digital.Erledigt.Select(e => $"{DigitalerAusgang.Uhrzeit(e.Beginn)}–{DigitalerAusgang.Uhrzeit(e.Ende!.Value)} Hz. {e.Handzeichen}"));
            if (digital.Laufend is AusgangEintrag l) heute.Add($"{DigitalerAusgang.Uhrzeit(l.Beginn)}– Hz. {l.HdzBeginn}");
        }
        HeuteText = heute.Count > 0 ? string.Join(" · ", heute) : "–";
        StandText = abgaengig is not null ? AbgaengigkeitRegeln.MarkeText(abgaengig)
            : digital?.Laufend is AusgangEintrag e
            ? Ueberfaellig && digital.ZurueckBis is TimeSpan bis
                ? AbgaengigkeitFrage ? AbgaengigkeitRegeln.UeberschrittenText(bis) : $"überfällig seit {DigitalerAusgang.Uhrzeit(bis)} ({e.Art})"
                : $"unterwegs seit {DigitalerAusgang.Uhrzeit(e.Beginn)} ({e.Art})"
            : p.Status switch
            {
                AnwesenheitsStatus.Abwesenheit => $"abwesend: {KartenText.GrundOhneTitel(p)}",
                AnwesenheitsStatus.Ausgangsbeschraenkung => "Ausgangsbeschränkung" + (KartenText.GrundOhneTitel(p) is { Length: > 0 } r ? $" {r}" : ""),
                _ => "im Haus",
            };
        RueckkehrText = Unterwegs && digital!.ZurueckBis is TimeSpan z ? $"bis {DigitalerAusgang.Uhrzeit(z)}" : "";

        bool rot = Ueberfaellig || Abgaengig;
        var hinter = rot ? Stil.Beschluss.Flaeche : Unterwegs ? Stil.Zimmer.Flaeche : Stil.Flaeche;
        balken = rot ? Stil.Beschluss.Text : Stil.Zimmer.Text;
        BackColor = hinter;
        string name = p.Bewohnername + (kueche ? " · Küche" : "");
        string[] texte = [p.Zimmernummer, name, StandText, RueckkehrText];
        zellen = texte.Select((t, i) => new Label
        {
            Text = t, AutoSize = false, AutoEllipsis = true, UseMnemonic = false, BackColor = hinter, TextAlign = ContentAlignment.MiddleLeft,
            Font = i == 1 || i == 2 && (Unterwegs || p.HatStatus) ? Stil.Fett : Stil.Standard,
            ForeColor = i == 2 && rot ? Stil.Beschluss.Text : i is 0 or 3 ? Stil.TextMittel : Stil.TextNormal,
        }).ToArray();
        Controls.AddRange(zellen);

        foreach (var c in zellen.Append<Control>(this))
        {
            c.DoubleClick += (_, _) => Doppelklick();
            c.MouseUp += (_, e) => { if (e.Button == MouseButtons.Right) Menue().Show(Cursor.Position); };
        }
        MehrKnopf.Click += (_, _) => Menue().Show(MehrKnopf, new Point(0, MehrKnopf.Height));
        RausKnopf.Click += (_, _) => RausMenue().Show(RausKnopf, new Point(0, RausKnopf.Height));
        MehrKnopf.AccessibleName = $"Weitere Aktionen für {p.Bewohnername}";
        RausKnopf.AccessibleName = $"Ausgang beginnen: {p.Bewohnername}";
        bool frei = digital is not null && !p.HatStatus && !Unterwegs;
        AlleinKnopf.Available = frei && DigitalerAusgang.Anspruch(p, "allein") > 0;
        MitBewohnerKnopf.Available = frei && DigitalerAusgang.Anspruch(p, "mit Bewohner") > 0;
        RausKnopf.Visible = AlleinKnopf.Available || MitBewohnerKnopf.Available;
        ZurueckKnopf.Visible = Unterwegs;
        AbgaengigkeitKnopf.Visible = AbgaengigkeitFrage;
        AbgaengigkeitKnopf.AccessibleName = $"Abgängigkeit melden: {p.Bewohnername}";
        AbgaengigkeitEintrag.Available = !Abgaengig;
        foreach (var k in new[] { AbgaengigkeitKnopf, ZurueckKnopf, RausKnopf, MehrKnopf })
        {
            k.Margin = new Padding(Stil.AbstandMini, 0, 0, 0);
            knoepfe.Controls.Add(k);   // unsichtbare zählen bei der Breite nicht mit
        }
        knoepfe.BackColor = hinter;
        Controls.Add(knoepfe);
        AccessibleName = $"{p.Bewohnername}, Zimmer {p.Zimmernummer}, {StandText}" + (RueckkehrText.Length > 0 ? $", zurück {RueckkehrText}" : "");
    }

    private ContextMenuStrip Menue()
    {
        if (menue is not null) return menue;
        menue = new ContextMenuStrip();
        menue.Items.AddRange([AusgangEintrag, AnwesenheitEintrag, UnterbringungEintrag, VerlaufEintrag, new ToolStripSeparator(), AbgaengigkeitEintrag]);
        return menue;
    }

    private ContextMenuStrip RausMenue()
    {
        if (rausMenue is not null) return rausMenue;
        rausMenue = new ContextMenuStrip();
        rausMenue.Items.AddRange([AlleinKnopf, MitBewohnerKnopf]);
        return rausMenue;
    }

    // Doppelklick: der naheliegende Handgriff – zurück, wenn unterwegs, sonst „raus“ wählen.
    private void Doppelklick()
    {
        if (Unterwegs) ZurueckKnopf.PerformClick();
        else if (AlleinKnopf.Available || MitBewohnerKnopf.Available) RausMenue().Show(Cursor.Position);
    }

    // Hinweise nur an Name (mit Ausgangszeiten) und Status – die übrigen Spalten sind kurz.
    public void Hinweise(ToolTip tip)
    {
        tip.SetToolTip(zellen[1], $"{zellen[1].Text} · {AusgangZeile}");
        tip.SetToolTip(zellen[2], $"{Spalten[2]}: {zellen[2].Text}");
    }

    // x-Position und Breite je Spalte bei gegebener Tabellenbreite.
    public static (int X, int Breite)[] Positionen(int links, int breite)
    {
        int summe = Anteile.Sum(), x = links;
        var liste = new (int, int)[Anteile.Length];
        for (int i = 0; i < Anteile.Length; i++)
        {
            int w = i == Anteile.Length - 1 ? links + breite - x : breite * Anteile[i] / summe;
            liste[i] = (x, w);
            x += w;
        }
        return liste;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (zellen is null) return;   // Anordnung schon während des Aufbaus
        int innen = Stil.Px(this, Stil.AbstandKlein), luecke = Stil.Px(this, Stil.AbstandKlein);
        var knopfGroesse = knoepfe.GetPreferredSize(Size.Empty);
        int knopfBereich = Math.Max(KnopfBreite, knopfGroesse.Width);
        int tabelle = Math.Max(100, Width - 2 * innen - knopfBereich - luecke);
        int hoehe = Hoehe(this);
        var pos = Positionen(innen, tabelle);
        for (int i = 0; i < zellen.Length; i++) zellen[i].SetBounds(pos[i].X, 0, pos[i].Breite - luecke, hoehe);
        knoepfe.Bounds = new Rectangle(new Point(Width - innen - knopfGroesse.Width, (hoehe - knopfGroesse.Height) / 2), knopfGroesse);
        if (Height != hoehe) Height = hoehe;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (var linie = new Pen(Stil.Trennlinie)) e.Graphics.DrawLine(linie, 0, Height - 1, Width, Height - 1);
        if (Unterwegs)   // Balken links: unterwegs (blau) bzw. überfällig (rot) – zusätzlich zum Wort
        {
            using var pinsel = new SolidBrush(balken);
            e.Graphics.FillRectangle(pinsel, 0, 0, Stil.Px(this, 4), Height - 1);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { menue?.Dispose(); rausMenue?.Dispose(); }
        base.Dispose(disposing);
    }
}

// Spaltenköpfe der Ausgangsliste, ausgerichtet wie die Zeilen.
public sealed class ListenKopf : Panel
{
    private readonly Label[] titel = ListenZeile.Spalten.Select(t => new Label
    {
        Text = t, AutoSize = false, AutoEllipsis = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, TextAlign = ContentAlignment.BottomLeft,
    }).ToArray();

    public int KnopfBreite { get; set; }

    public ListenKopf()
    {
        BackColor = Stil.Seite;
        Controls.AddRange(titel);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandKlein), luecke = Stil.Px(this, Stil.AbstandKlein);
        int tabelle = Math.Max(100, Width - 2 * innen - KnopfBreite - luecke);
        var pos = ListenZeile.Positionen(innen, tabelle);
        int h = titel[0].PreferredHeight;
        for (int i = 0; i < titel.Length; i++) titel[i].SetBounds(pos[i].X, 0, pos[i].Breite - luecke, h);
        if (Height != h + Stil.Px(this, Stil.AbstandMini)) Height = h + Stil.Px(this, Stil.AbstandMini);
    }
}
