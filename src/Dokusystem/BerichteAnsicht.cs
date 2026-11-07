using Dokusystem.Kern;

namespace Dokusystem;

// Ein Bericht als Karte: Zeitpunkt, Schicht und Kürzel, Lebensbereiche und „wichtig“ als Marken,
// der Text und darunter alle Nachträge. Der ursprüngliche Text bleibt immer sichtbar.
public sealed class BerichtKarte : KartenFlaeche
{
    private readonly Label kopf = new();
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label text = new();
    private readonly List<(Label Kopf, Label Text)> nachtraege = [];

    public Bericht Bericht { get; }
    public Button NachtragKnopf { get; } = Stil.NebenKnopf("Nachtrag …");
    public event Action<Bericht>? NachtragGewuenscht;

    // name: Bewohnername (nur in der Übergabe, wo Berichte aller Bewohner stehen).
    public BerichtKarte(Bericht bericht, string? name = null)
    {
        Bericht = bericht;
        Padding = new Padding(Stil.AbstandGross);
        string zeile = $"{BerichtRegeln.ZeitText(bericht.Zeitpunkt)} · {BerichtRegeln.SchichtText(bericht.Schicht)} · {bericht.Verfasser}";
        kopf.Text = name is null ? zeile : $"{name} · {zeile}";
        kopf.Font = name is null ? Stil.Fett : Stil.KarteName;
        kopf.ForeColor = Stil.TextNormal;

        if (bericht.Wichtig) marken.Controls.Add(Marke("Wichtig", Stil.Hinweis));
        if (bericht.Uebergabe) marken.Controls.Add(Marke("Übergabe", Stil.Unterwegs));
        if (bericht.Allgemein) marken.Controls.Add(Marke("allgemein", Stil.Zimmer));
        foreach (int nummer in bericht.Bereiche) marken.Controls.Add(Marke(BerichtRegeln.BereichText(nummer), Stil.Zimmer));

        text.Text = bericht.Text;
        text.Font = Stil.Standard;
        text.ForeColor = Stil.TextNormal;
        foreach (var n in bericht.Nachtraege)
        {
            var nk = new Label { Text = $"Nachtrag {BerichtRegeln.ZeitText(n.Erfasst)} · {n.Verfasser}", Font = Stil.Abschnitt, ForeColor = Stil.Hinweis.Text };
            var nt = new Label { Text = n.Text, Font = Stil.Standard, ForeColor = Stil.TextMittel };
            nachtraege.Add((nk, nt));
        }
        foreach (var label in new[] { kopf, text }.Concat(nachtraege.SelectMany(n => new[] { n.Kopf, n.Text })))
        {
            label.AutoSize = true;
            label.BackColor = Stil.Flaeche;
            label.UseMnemonic = false;
            Controls.Add(label);
        }
        NachtragKnopf.Click += (_, _) => NachtragGewuenscht?.Invoke(Bericht);
        Controls.Add(marken);
        Controls.Add(NachtragKnopf);
        AccessibleName = $"Bericht {kopf.Text}, {string.Join(", ", MarkenTexte)}: {bericht.Text}";
    }

    public IEnumerable<string> MarkenTexte => marken.Controls.OfType<MarkenFeld>().Select(m => m.Text);

    private static MarkenFeld Marke(string text, Stil.Marke marke) => new(text, marke) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) };

    public IEnumerable<string> NachtragTexte => nachtraege.Select(n => n.Text.Text);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        kopf.MaximumSize = new Size(breite - NachtragKnopf.Width - Stil.Px(this, Stil.AbstandKlein), 0);
        kopf.Location = new Point(innen, y);
        NachtragKnopf.Location = new Point(Width - innen - NachtragKnopf.Width, y - Stil.Px(this, Stil.AbstandMini));
        y = Math.Max(kopf.Bottom, NachtragKnopf.Bottom - Stil.Px(this, Stil.AbstandKlein)) + Stil.Px(this, Stil.AbstandKlein);
        marken.MaximumSize = new Size(breite, 0);
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        text.MaximumSize = new Size(breite, 0);
        text.Location = new Point(innen, y);
        y = text.Bottom;
        foreach (var (nk, nt) in nachtraege)
        {
            y += Stil.Px(this, Stil.AbstandMittel);
            nk.Location = new Point(innen + Stil.Px(this, Stil.AbstandMittel), y);
            nt.MaximumSize = new Size(breite - Stil.Px(this, Stil.AbstandMittel), 0);
            nt.Location = new Point(innen + Stil.Px(this, Stil.AbstandMittel), nk.Bottom + Stil.Px(this, 2));
            y = nt.Bottom;
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Nachträge mit einem schmalen Balken links absetzen.
        using var balken = new SolidBrush(Stil.Hinweis.Flaeche);
        foreach (var (nk, nt) in nachtraege)
            e.Graphics.FillRectangle(balken, nk.Left - Stil.Px(this, Stil.AbstandKlein), nk.Top, Stil.Px(this, 3), nt.Bottom - nk.Top);
    }
}

// Eine senkrechte Liste von Berichtskarten. Wächst mit ihrem Inhalt (die umgebende Ansicht
// rollt); zeigt einen Hinweis, wenn nichts da ist.
public sealed class BerichtListe : Panel
{
    private readonly Label leer = new() { AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };
    public List<BerichtKarte> Karten { get; } = [];

    public BerichtListe()
    {
        BackColor = Stil.Seite;
        Controls.Add(leer);
    }

    public void Setzen(IEnumerable<BerichtKarte> karten, string leerText)
    {
        SuspendLayout();
        foreach (var alt in Karten) { Controls.Remove(alt); alt.Dispose(); }
        Karten.Clear();
        Karten.AddRange(karten);
        foreach (var karte in Karten) Controls.Add(karte);
        leer.Text = leerText;
        leer.Visible = Karten.Count == 0;
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int y = 0, abstand = Stil.Px(this, Stil.AbstandMittel);
        leer.Location = new Point(Stil.Px(this, Stil.AbstandMini), 0);
        if (leer.Visible) y = leer.Bottom;
        foreach (var karte in Karten)
        {
            karte.SetBounds(0, y, Width, karte.Height);
            karte.PerformLayout();
            karte.Top = y;
            y += karte.Height + abstand;
        }
        if (Height != y) Height = y;
    }
}

// Reiter „Berichte“ in der Akte: Filter (Zeitraum, Lebensbereich, Schicht, Suche), „Neuer
// Bericht …“, „Drucken …“ und die Berichte des Bewohners, neueste oben.
public sealed class BerichteAnsicht : Panel
{
    public static readonly string[] Zeitraeume = ["letzte 7 Tage", "letzte 4 Wochen", "letzte 3 Monate", "alle"];

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private Bewohner? bewohner;

    public ComboBox Zeitraum { get; } = Auswahl(Zeitraeume);
    public ComboBox Bereich { get; } = Auswahl(["alle Lebensbereiche", .. Enumerable.Range(1, 5).Select(BerichtRegeln.BereichText), "allgemein"]);
    public ComboBox Schichtwahl { get; } = Auswahl(["alle Schichten", "Frühdienst", "Spätdienst", "Nachtdienst"]);
    public TextBox Suche { get; } = new() { Width = 180, PlaceholderText = "Im Text suchen", Font = Stil.Standard };
    public CheckBox NurWichtige { get; } = new() { Text = "nur wichtige", AutoSize = true, Font = Stil.Standard, BackColor = Stil.Seite };
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neuer Bericht …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public Label Anzahl { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };
    public BerichtListe Liste { get; } = new();

    // Der heutige Tag (für den Zeitraum); zum Testen ersetzbar.
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public BerichteAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Zeitraum.SelectedIndex = 1;   // letzte 4 Wochen
        foreach (var c in new Control[] { Zeitraum, Bereich, Schichtwahl, Suche, NurWichtige, NeuKnopf, DruckKnopf, Anzahl })
        {
            c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        Anzahl.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandMittel, 0, 0);
        Zeitraum.SelectedIndexChanged += (_, _) => Aktualisieren();
        Bereich.SelectedIndexChanged += (_, _) => Aktualisieren();
        Schichtwahl.SelectedIndexChanged += (_, _) => Aktualisieren();
        Suche.TextChanged += (_, _) => Aktualisieren();
        NurWichtige.CheckedChanged += (_, _) => Aktualisieren();
        NurWichtige.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein);
        NeuKnopf.Click += (_, _) => NeuerBericht();
        DruckKnopf.Click += (_, _) => Drucken();
        Controls.Add(leiste);
        Controls.Add(Liste);
    }

    private static ComboBox Auswahl(string[] eintraege)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Stil.Standard, Width = 190 };
        box.Items.AddRange(eintraege);
        box.SelectedIndex = 0;
        return box;
    }

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        Aktualisieren();
    }

    public BerichtFilter Filter()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        DateOnly? von = Zeitraum.SelectedIndex switch { 0 => heute.AddDays(-6), 1 => heute.AddDays(-27), 2 => heute.AddMonths(-3), _ => null };
        int? bereich = Bereich.SelectedIndex switch { 0 => null, 6 => BerichtFilter.Allgemein, int i => i };
        Schicht? schicht = Schichtwahl.SelectedIndex switch { 1 => Schicht.Frueh, 2 => Schicht.Spaet, 3 => Schicht.Nacht, _ => null };
        return new BerichtFilter(von, null, bereich, schicht, Suche.Text, NurWichtige.Checked);
    }

    public List<Bericht> Berichte => bewohner?.Id is long id ? speicher.Berichte(id, Filter()) : [];

    public void Aktualisieren()
    {
        if (bewohner is null) return;
        var berichte = Berichte;
        Liste.Setzen(berichte.Select(KarteBauen), "Keine Berichte für diese Auswahl.");
        Anzahl.Text = berichte.Count == 1 ? "1 Bericht" : $"{berichte.Count} Berichte";
        PerformLayout();
    }

    private BerichtKarte KarteBauen(Bericht b)
    {
        var karte = new BerichtKarte(b);
        karte.NachtragGewuenscht += NachtragSchreiben;
        return karte;
    }

    public void NeuerBericht()
    {
        if (bewohner is null) return;
        using var formular = new BerichtFormular(speicher, bewohner);
        if (Dialog.Zeigen(formular, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void NachtragSchreiben(Bericht b)
    {
        using var formular = new NachtragFormular(speicher, b);
        if (Dialog.Zeigen(formular, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Drucken()
    {
        if (bewohner is null) return;
        var druck = new BerichteDruck(bewohner, Berichte, Zeitraum.Text);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Berichte {bewohner.Name}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        leiste.MaximumSize = new Size(Width, 0);
        leiste.Location = Point.Empty;
        Liste.SetBounds(0, leiste.Bottom + Stil.Px(this, Stil.AbstandKlein), Width, Liste.Height);
        Liste.PerformLayout();
        int hoehe = Liste.Bottom;
        if (Height != hoehe) Height = hoehe;
    }
}
