using DokuLine.Kern;

namespace DokuLine;

// Bereich „Nachrichten“: Posteingang, Gesendet, Archiv – jeweils Verläufe (erste Nachricht und
// Antworten). Ein Klick öffnet den Verlauf; dort antworten oder archivieren. Alles bleibt in der
// Datenbank des Programms (kein E-Mail-Versand, kein Internet).
public sealed class NachrichtenSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Nachrichten";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [NeuKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten)
    {
        if (filterBehalten) Aktualisieren();
        else BewohnerZeigen(null);
    }

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public Dictionary<NachrichtenOrdner, FilterKnopf> Ordner { get; } = [];
    public NachrichtenOrdner AktiverOrdner { get; private set; } = NachrichtenOrdner.Posteingang;
    public long? BewohnerFilter { get; private set; }
    public Button BewohnerFilterKnopf { get; } = Stil.NebenKnopf("");
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neue Nachricht …");
    // Mehrere Nachrichten auf einmal als gelesen markieren (wandern ins Archiv).
    public Button MarkierteGelesenKnopf { get; } = Stil.NebenKnopf("Markierte als gelesen");
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
        MarkierteGelesenKnopf.Margin = new Padding(Stil.AbstandMittel, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        MarkierteGelesenKnopf.AutoSize = true;
        MarkierteGelesenKnopf.Click += (_, _) => Gelesen([.. Karten.Where(k => k.Auswahl.Checked).Select(k => k.Verlauf)]);
        leiste.Controls.AddRange([BewohnerFilterKnopf, MarkierteGelesenKnopf, Hinweis]);
        NeuKnopf.Click += (_, _) => NeueNachricht();
        Controls.Add(leiste);
    }

    public static string Ich => Recht.Kuerzel;

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
                var karte = new VerlaufKarte(v, ich, kollegen, v.BewohnerId is long id ? namen.GetValueOrDefault(id) : null, AktiverOrdner);
                karte.Gewaehlt += VerlaufOeffnen;
                karte.GelesenKnopf.Click += (_, _) => Gelesen([karte.Verlauf]);
                karte.UngelesenKnopf.Click += (_, _) => WiederUngelesen(karte.Verlauf);
                karte.Auswahl.CheckedChanged += (_, _) => MarkierteZeigen();
                Karten.Add(karte);
                inhalt.Add(karte);
            }
            if (verlaeufe.Count == 0)
                inhalt.Add(new Label { Text = $"Keine Nachrichten unter „{NachrichtenRegeln.OrdnerText(AktiverOrdner)}“.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        }
        foreach (var c in inhalt) Controls.Add(c);
        MarkierteZeigen();
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void MarkierteZeigen()
    {
        int n = Karten.Count(k => k.Auswahl.Checked);
        MarkierteGelesenKnopf.Text = n == 0 ? "Markierte als gelesen" : $"Markierte als gelesen ({n})";
        MarkierteGelesenKnopf.Visible = AktiverOrdner == NachrichtenOrdner.Posteingang && Offen is null && Karten.Count > 0;
        MarkierteGelesenKnopf.Enabled = n > 0;
    }

    // Als gelesen markieren – die Nachrichten wandern (nur für mich) ins Archiv.
    public void Gelesen(IReadOnlyList<Verlauf> verlaeufe)
    {
        if (speicher.Kollege(Ich) is not Kollege ich || verlaeufe.Count == 0) return;
        speicher.VerlaeufeGelesen(ich.Kuerzel, verlaeufe.Select(v => v.Id), Jetzt());
        Aktualisieren();
    }

    public void WiederUngelesen(Verlauf v)
    {
        if (speicher.Kollege(Ich) is not Kollege ich) return;
        speicher.VerlaufWiederUngelesen(ich.Kuerzel, v.Id);
        Aktualisieren();
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
