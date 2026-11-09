using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Eine Zeile der Ausgangsliste am Bildschirm (von DokuLine), angelehnt an
// die ausdruckbare Tagesliste: Zimmer, Bewohner, Ausgang, Unterbringung, Ausgänge heute (von–bis,
// Hz.) und „Stand jetzt“. Rechts „raus“ bzw. „zurück“ direkt in der Zeile, dazu „…“ (auch per
// Rechtsklick) für Ausgang, Anwesenheit, Unterbringung und Verlauf. Wer unterwegs ist, ist
// hinterlegt und hat links einen Balken; Überfällige sind rot und tragen das Wort „überfällig“.
public sealed class ListenZeile : Panel
{
    public static readonly string[] Spalten = ["Zi.", "Bewohner", "Ausgang", "Unterbringung", "Heute (von–bis, Hz.)", "Stand jetzt"];
    private static readonly int[] Anteile = [5, 17, 22, 18, 17, 21];
    public const int Schmal = 980;   // darunter stehen die Knöpfe unter der Zeile

    private readonly Label[] zellen;
    private readonly Label hinweise = new() { AutoSize = false, AutoEllipsis = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, UseMnemonic = false };
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
    private readonly ContextMenuStrip menue = new();
    private readonly Color balken;

    public Platz Platz { get; }
    public IReadOnlyList<string> MarkenTexte { get; }
    public string AusgangZeile { get; }
    public string UnterbringungZeile { get; }
    public string HeuteText { get; }
    public string StandText { get; }
    public string HinweisText => hinweise.Text;
    public bool Unterwegs { get; }
    public bool Ueberfaellig { get; }
    public Button ZurueckKnopf { get; } = Stil.HauptKnopf("zurück");
    public Button AlleinKnopf { get; } = Stil.NebenKnopf("raus allein");
    public Button MitBewohnerKnopf { get; } = Stil.NebenKnopf("raus mit Bewohner");
    public Button MehrKnopf { get; } = Stil.NebenKnopf("…");
    public ToolStripMenuItem AusgangEintrag { get; } = new("Ausgang ändern …");
    public ToolStripMenuItem AnwesenheitEintrag { get; } = new("Anwesenheit …");
    public ToolStripMenuItem UnterbringungEintrag { get; } = new("Unterbringung …");
    public ToolStripMenuItem VerlaufEintrag { get; } = new("Verlauf …");

    // Gemeinsame Breite des Knopfbereichs aller Zeilen (setzt die Seite), damit die Spalten fluchten.
    public int KnopfBreite { get; set; }
    public int EigeneKnopfBreite => knoepfe.GetPreferredSize(Size.Empty).Width;

    // digital: null = Untermodul „Digitaler Ausgang“ ausgeschaltet.
    public ListenZeile(Platz p, DateTime jetzt, DigitalStand? digital, bool kueche)
    {
        Platz = p;
        DoubleBuffered = true;
        ResizeRedraw = true;
        var marken = AusgangsMarken.Fuer(p, jetzt, digital, alle: true, kueche: kueche);
        MarkenTexte = marken.Select(m => m.Text).ToList();
        Unterwegs = digital?.Laufend is not null;
        Ueberfaellig = Unterwegs && digital!.Ueberfaellig;
        AusgangZeile = PlatzTexte.Ausgang(p, jetzt);
        UnterbringungZeile = PlatzTexte.Unterbringung(p, jetzt);
        var heute = new List<string>();
        if (digital is not null)
        {
            heute.AddRange(digital.Erledigt.Select(e => $"{DigitalerAusgang.Uhrzeit(e.Beginn)}–{DigitalerAusgang.Uhrzeit(e.Ende!.Value)} Hz. {e.Handzeichen}"));
            if (digital.Laufend is AusgangEintrag l) heute.Add($"{DigitalerAusgang.Uhrzeit(l.Beginn)}– Hz. {l.HdzBeginn}");
        }
        HeuteText = heute.Count > 0 ? string.Join(" · ", heute) : "–";
        StandText = digital?.Laufend is AusgangEintrag e
            ? Ueberfaellig && digital.ZurueckBis is TimeSpan bis
                ? $"überfällig seit {DigitalerAusgang.Uhrzeit(bis)} ({e.Art})"
                : $"unterwegs seit {DigitalerAusgang.Uhrzeit(e.Beginn)} ({e.Art})" + (digital.ZurueckBis is TimeSpan z ? $", zurück bis {DigitalerAusgang.Uhrzeit(z)}" : "")
            : p.Status switch
            {
                AnwesenheitsStatus.Abwesenheit => $"abwesend: {KartenText.GrundOhneTitel(p)}",
                AnwesenheitsStatus.Ausgangsbeschraenkung => "Ausgangsbeschränkung" + (KartenText.GrundOhneTitel(p) is { Length: > 0 } r ? $" {r}" : ""),
                _ => "im Haus",
            };
        // Was Aufmerksamkeit braucht (Fristen, Widerspruch, nicht erfasst), steht klein unter der Zeile.
        var hinweisTexte = marken.Where(m => m.Art == MarkenArt.NichtErfasst && !m.Text.StartsWith("überfällig", StringComparison.Ordinal))
            .Select(m => m.Text).Concat(PlatzTexte.Infos(p)).ToList();
        hinweise.Text = string.Join(" · ", hinweisTexte);

        var hinter = Ueberfaellig ? Stil.Beschluss.Flaeche : Unterwegs ? Stil.Zimmer.Flaeche : Stil.Flaeche;
        balken = Ueberfaellig ? Stil.Beschluss.Text : Stil.Zimmer.Text;
        BackColor = hinter;
        string name = p.Bewohnername + (kueche ? " · Küche" : "");
        string[] texte = [p.Zimmernummer, name, AusgangZeile, UnterbringungZeile, HeuteText, StandText];
        zellen = texte.Select((t, i) => new Label
        {
            Text = t, AutoSize = false, AutoEllipsis = true, UseMnemonic = false, BackColor = hinter, TextAlign = ContentAlignment.MiddleLeft,
            Font = i == 1 || i == 5 && (Unterwegs || p.HatStatus) ? Stil.Fett : Stil.Standard,
            ForeColor = i == 5 && Ueberfaellig ? Stil.Beschluss.Text : i is 0 or 3 or 4 ? Stil.TextMittel : Stil.TextNormal,
        }).ToArray();
        hinweise.BackColor = hinter;
        hinweise.Visible = hinweise.Text.Length > 0;
        Controls.AddRange(zellen);
        Controls.Add(hinweise);

        menue.Items.AddRange([AusgangEintrag, AnwesenheitEintrag, UnterbringungEintrag, VerlaufEintrag]);
        ContextMenuStrip = menue;
        foreach (var zelle in zellen) zelle.ContextMenuStrip = menue;
        MehrKnopf.Click += (_, _) => menue.Show(MehrKnopf, new Point(0, MehrKnopf.Height));
        MehrKnopf.AccessibleName = $"Weitere Aktionen für {p.Bewohnername}";
        bool frei = digital is not null && !p.HatStatus && !Unterwegs;
        AlleinKnopf.Visible = frei && DigitalerAusgang.Anspruch(p, "allein") > 0;
        MitBewohnerKnopf.Visible = frei && DigitalerAusgang.Anspruch(p, "mit Bewohner") > 0;
        ZurueckKnopf.Visible = Unterwegs;
        foreach (var k in new[] { ZurueckKnopf, AlleinKnopf, MitBewohnerKnopf, MehrKnopf })
        {
            k.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
            k.AutoSize = true;
            knoepfe.Controls.Add(k);   // unsichtbare zählen bei der Breite nicht mit
        }
        knoepfe.BackColor = hinter;
        Controls.Add(knoepfe);
        AccessibleName = $"{p.Bewohnername}, Zimmer {p.Zimmernummer}, {StandText}. {AusgangZeile}. Unterbringung: {UnterbringungZeile}" +
                         (hinweise.Text.Length > 0 ? $". {hinweise.Text}" : "");
    }

    public void Hinweise(ToolTip tip)
    {
        string[] titel = Spalten;
        for (int i = 0; i < zellen.Length; i++) tip.SetToolTip(zellen[i], $"{titel[i]}: {zellen[i].Text}");
        if (hinweise.Visible) tip.SetToolTip(hinweise, hinweise.Text);
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

    public static bool KnoepfeUnten(Control c) => c.Width < Stil.Px(c, Schmal);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (zellen is null) return;   // Anordnung schon während des Aufbaus
        int innen = Stil.Px(this, Stil.AbstandMittel), luecke = Stil.Px(this, Stil.AbstandKlein);
        bool unten = KnoepfeUnten(this);
        var knopfGroesse = knoepfe.GetPreferredSize(Size.Empty);
        int knopfBereich = Math.Max(KnopfBreite, knopfGroesse.Width);
        int tabelle = Math.Max(100, Width - 2 * innen - (unten ? 0 : knopfBereich + luecke));
        int zeile = Math.Max(zellen[1].PreferredHeight, knopfGroesse.Height);
        int y = innen;
        var pos = Positionen(innen, tabelle);
        for (int i = 0; i < zellen.Length; i++) zellen[i].SetBounds(pos[i].X, y, pos[i].Breite - luecke, zeile);
        y += zeile;
        if (hinweise.Visible)
        {
            int h = hinweise.PreferredHeight;
            hinweise.SetBounds(pos[1].X, y, innen + tabelle - pos[1].X, h);
            y += h;
        }
        if (unten)
        {
            knoepfe.Bounds = new Rectangle(new Point(pos[1].X, y + Stil.Px(this, Stil.AbstandMini)), knopfGroesse);
            y = knoepfe.Bottom;
        }
        else knoepfe.Bounds = new Rectangle(new Point(Width - innen - knopfGroesse.Width, innen + (zeile - knopfGroesse.Height) / 2), knopfGroesse);
        int hoehe = y + innen;
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
        if (disposing) menue.Dispose();
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
        int innen = Stil.Px(this, Stil.AbstandMittel), luecke = Stil.Px(this, Stil.AbstandKlein);
        bool unten = ListenZeile.KnoepfeUnten(this);
        int tabelle = Math.Max(100, Width - 2 * innen - (unten ? 0 : KnopfBreite + luecke));
        var pos = ListenZeile.Positionen(innen, tabelle);
        int h = titel[0].PreferredHeight;
        for (int i = 0; i < titel.Length; i++) titel[i].SetBounds(pos[i].X, 0, pos[i].Breite - luecke, h);
        if (Height != h + Stil.Px(this, Stil.AbstandMini)) Height = h + Stil.Px(this, Stil.AbstandMini);
    }
}
