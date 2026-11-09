using DokuLine.Kern;

namespace DokuLine;

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
    // Im Posteingang Häkchen und „Gelesen“, im Archiv „Wieder als ungelesen“.
    public CheckBox Auswahl { get; } = new() { AutoSize = true, BackColor = Stil.Flaeche, AccessibleName = "Markieren" };
    public Button GelesenKnopf { get; } = Stil.NebenKnopf("Gelesen");
    public Button UngelesenKnopf { get; } = Stil.NebenKnopf("Wieder als ungelesen");
    private readonly bool imPosteingang, imArchiv;

    public VerlaufKarte(Verlauf v, Kollege ich, IReadOnlyDictionary<string, Kollege> kollegen, string? bewohner, NachrichtenOrdner ordner = NachrichtenOrdner.Posteingang)
    {
        Verlauf = v;
        Cursor = Cursors.Hand;
        imPosteingang = ordner == NachrichtenOrdner.Posteingang;
        imArchiv = ordner == NachrichtenOrdner.Archiv;
        Auswahl.Visible = GelesenKnopf.Visible = imPosteingang;
        UngelesenKnopf.Visible = imArchiv;
        GelesenKnopf.AutoSize = UngelesenKnopf.AutoSize = true;
        Controls.AddRange([Auswahl, GelesenKnopf, UngelesenKnopf]);
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
        int rand = Stil.Px(this, Stil.AbstandGross), innen = rand, breite = Math.Max(100, Width - 2 * rand);
        int y = rand;
        // Knopf rechts oben, Häkchen links; der Text rückt ein, damit nichts überlappt.
        Button? knopf = imPosteingang ? GelesenKnopf : imArchiv ? UngelesenKnopf : null;
        if (knopf is not null)
        {
            knopf.Location = new Point(Width - rand - knopf.Width, rand);
            breite = Math.Max(100, breite - knopf.Width - Stil.Px(this, Stil.AbstandKlein));
        }
        if (imPosteingang)
        {
            Auswahl.Location = new Point(rand, rand + Stil.Px(this, 2));
            int platz = Auswahl.Width + Stil.Px(this, Stil.AbstandKlein);
            innen = rand + platz;
            breite = Math.Max(100, breite - platz);
        }
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
        int hoehe = Math.Max(vorschau.Bottom, knopf?.Bottom ?? 0) + rand;
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
