using DokuLine.Kern;

namespace DokuLine;

// Akte, Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger: die geltende Fassung als Tabelle, „nicht erfasst“
// in Achtung-Farbe (Regel 5), darunter „ab … · wer“; „Ändern …“ legt eine neue Fassung an, „Verlauf …“ zeigt alle.
public sealed class AngabenAnsicht : KartenFlaeche
{
    private readonly Speicher speicher;
    private readonly TableLayoutPanel tabelle = new() { ColumnCount = 2, AutoSize = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche };
    public Label Stand { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Button AendernKnopf { get; } = Stil.HauptKnopf("Ändern …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public Angabe Art { get; private set; }
    public Bewohner? Bewohner { get; private set; }
    public List<(string Beschriftung, string Wert, bool Achtung)> Zeilen { get; private set; } = [];
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public event Action? Geaendert;

    public AngabenAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        AendernKnopf.Click += (_, _) => Aendern();
        VerlaufKnopf.Click += (_, _) => VerlaufZeigen();
        foreach (var k in new[] { AendernKnopf, VerlaufKnopf }) k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        knoepfe.Controls.AddRange([AendernKnopf, VerlaufKnopf]);
        Controls.AddRange([tabelle, Stand, knoepfe]);
    }

    private DateOnly Heute => DateOnly.FromDateTime(Jetzt());

    // „Name · Stelle · Tel. …“ aus dem Personenverzeichnis.
    public static Func<long?, string> PersonText(Speicher speicher)
    {
        var personen = speicher.Personen().ToDictionary(p => p.Id);
        return id => id is long x && personen.TryGetValue(x, out var p)
            ? string.Join(" · ", new[] { p.Name, p.Praxis, p.Telefon.Length > 0 ? $"Tel. {p.Telefon}" : "" }.Where(s => s.Length > 0))
            : "–";
    }

    public void Zeigen(Bewohner b, Angabe art)
    {
        Bewohner = b;
        Art = art;
        var fassung = AngabenRegeln.Aktuell(speicher.Fassungen(b.Id!.Value, art), Heute);
        Zeilen = AngabenRegeln.Zeilen(art, fassung, PersonText(speicher), Heute);
        SuspendLayout();
        tabelle.SuspendLayout();
        foreach (Control c in tabelle.Controls.Cast<Control>().ToList()) { tabelle.Controls.Remove(c); c.Dispose(); }
        foreach (var (beschriftung, wert, achtung) in Zeilen)
        {
            tabelle.Controls.Add(new Label
            {
                Text = beschriftung, AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false,
                Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandGross, Stil.AbstandMini),
            });
            tabelle.Controls.Add(new Label
            {
                Text = wert, AutoSize = true, MaximumSize = new Size(Stil.Px(this, 620), 0), UseMnemonic = false, BackColor = Stil.Flaeche,
                ForeColor = achtung ? Stil.Achtung.Text : Stil.TextNormal, Font = achtung ? Stil.FettKursiv : Stil.Fett,
                Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini),
            });
        }
        tabelle.ResumeLayout();
        Stand.Text = fassung is null ? "Noch keine Angabe." : $"gilt ab {Datum.Text(fassung.Ab)} · eingetragen von {fassung.Kuerzel} am {fassung.Geaendert:dd.MM.yyyy}";
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, AendernKnopf);
        Stil.Sperren(VerlaufKnopf, speicher.Fassungen(b.Id!.Value, art).Count == 0 ? "Noch kein Verlauf." : null);
        ResumeLayout();
        PerformLayout();
    }

    public void Aendern()
    {
        if (Bewohner is null || Stil.Sperrgrund(AendernKnopf) is not null) return;
        using var fenster = new AngabeFenster(speicher, Bewohner, Art);
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
        Zeigen(Bewohner, Art);
        Geaendert?.Invoke();
    }

    public void VerlaufZeigen()
    {
        if (Bewohner is null) return;
        var person = PersonText(speicher);
        var zeilen = speicher.Fassungen(Bewohner.Id!.Value, Art).AsEnumerable().Reverse().Select(f => AngabenRegeln.VerlaufZeile(Art, f, person, Heute));
        using var fenster = new TextFenster($"Verlauf {AngabenRegeln.Name(Art)}: {Bewohner.Name}", string.Join(Environment.NewLine, zeilen));
        Dialog.Zeigen(fenster, FindForm());
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        tabelle.Location = new Point(innen, innen);
        tabelle.Size = tabelle.GetPreferredSize(new Size(breite, 0));
        Stand.Location = new Point(innen, tabelle.Bottom + Stil.Px(this, Stil.AbstandKlein));
        knoepfe.MaximumSize = new Size(breite, 0);
        knoepfe.Bounds = new Rectangle(new Point(innen, Stand.Bottom + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int hoehe = knoepfe.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }
}
