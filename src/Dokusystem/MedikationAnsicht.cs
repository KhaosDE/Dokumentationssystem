using System.Drawing.Printing;
using Dokusystem.Kern;

namespace Dokusystem;

// Reiter „Medikation“ in der Akte: „Was gilt am …“ (heute oder ein gewählter Tag), berechnet aus
// Grundverordnung, Pausen und Stufenplänen. Pausierte stehen sichtbar als „pausiert bis …“ da.
// Neue Verordnung, Pause, Stufenplan, Absetzen – jeweils als eigener Eintrag; Verlauf je Verordnung.
// Demo — nicht zum Stellen verwenden.
public sealed class MedikationAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];
    private Bewohner? bewohner;

    public TextBox TagFeld { get; } = new() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public Button HeuteKnopf { get; } = Stil.NebenKnopf("Heute");
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Verordnung …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public CheckBox AbgesetzteZeigen { get; } = new() { Text = "Abgesetzte zeigen", AutoSize = true, BackColor = Stil.Seite };
    public MarkenFeld Hinweis { get; } = new(MedikationRegeln.DruckHinweis, Stil.Achtung);
    public Label TagFehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Seite, Visible = false };
    public List<VerordnungKarte> Karten { get; } = [];
    public List<string> Abschnitte { get; } = [];

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public MedikationAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        var beschriftung = new Label { Text = "Was gilt am", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) };
        foreach (var c in new Control[] { beschriftung, TagFeld, HeuteKnopf, NeuKnopf, DruckKnopf, AbgesetzteZeigen, Hinweis, TagFehler })
        {
            if (c != beschriftung) c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        TagFeld.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandKlein);
        AbgesetzteZeigen.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        Hinweis.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        TagFeld.TextChanged += (_, _) => Aktualisieren();
        HeuteKnopf.Click += (_, _) => TagFeld.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        NeuKnopf.Click += (_, _) => NeueVerordnung();
        DruckKnopf.Click += (_, _) => Drucken();
        AbgesetzteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(leiste);
    }

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        if (TagFeld.Text != heute) TagFeld.Text = heute;   // löst Aktualisieren aus
        else Aktualisieren();
    }

    public DateOnly? Stichtag()
    {
        try { return Datum.Lesen(TagFeld.Text, "Tag"); }
        catch (FormatException) { return null; }
    }

    public List<Tagesstand> Staende(DateOnly tag) =>
        bewohner?.Id is long id ? speicher.Verordnungen(id).Select(v => MedikationRegeln.Stand(v, tag)).ToList() : [];

    public void Aktualisieren()
    {
        if (bewohner is null) return;
        var tag = Stichtag();
        TagFehler.Visible = tag is null;
        TagFehler.Text = "Tag bitte als TT.MM.JJJJ eingeben.";
        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        Abschnitte.Clear();
        if (tag is DateOnly t)
        {
            var staende = Staende(t);
            void Abschnitt(string text, IEnumerable<Tagesstand> liste, string leer)
            {
                Abschnitte.Add(text);
                inhalt.Add(new Label { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
                int n = 0;
                foreach (var s in liste)
                {
                    var karte = new VerordnungKarte(s);
                    karte.PauseKnopf.Click += (_, _) => Aendern(s.Verordnung, AenderungArt.Pause);
                    karte.PauseEndeKnopf.Click += (_, _) => Aendern(s.Verordnung, AenderungArt.PauseEnde);
                    karte.StufenplanKnopf.Click += (_, _) => Aendern(s.Verordnung, AenderungArt.Stufenplan);
                    karte.AbsetzenKnopf.Click += (_, _) => Aendern(s.Verordnung, AenderungArt.Absetzen);
                    karte.VerlaufKnopf.Click += (_, _) => VerlaufZeigen(s.Verordnung);
                    Karten.Add(karte);
                    inhalt.Add(karte);
                    n++;
                }
                if (n == 0) inhalt.Add(new Label { Text = leer, AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
            }
            Abschnitt("REGELMÄSSIG", staende.Where(s => s.Sichtbar && !s.Verordnung.Bedarf), "Keine regelmäßige Medikation an diesem Tag.");
            Abschnitt("BEI BEDARF", staende.Where(s => s.Sichtbar && s.Verordnung.Bedarf), "Keine Bedarfsmedikation.");
            var spaeter = staende.Where(s => s.Zustand == Zustand.NochNicht).ToList();
            if (spaeter.Count > 0) Abschnitt("BEGINNT SPÄTER", spaeter, "");
            if (AbgesetzteZeigen.Checked) Abschnitt("ABGESETZT", staende.Where(s => s.Zustand == Zustand.Abgesetzt), "Nichts abgesetzt.");
        }
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        PerformLayout();
    }

    public void NeueVerordnung()
    {
        if (bewohner is null) return;
        using var fenster = new VerordnungFenster(speicher, bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Aendern(Verordnung v, AenderungArt art)
    {
        using var fenster = new AenderungFenster(speicher, v, art);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void VerlaufZeigen(Verordnung v)
    {
        using var fenster = new TextFenster($"Verlauf: {v.Medikament}", string.Join(Environment.NewLine + Environment.NewLine, MedikationRegeln.Verlauf(v)));
        Dialog.Zeigen(fenster, FindForm());
    }

    public void Drucken()
    {
        if (bewohner is null || Stichtag() is not DateOnly tag) return;
        var druck = new MedikationDruck(bewohner, tag, Staende(tag));
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Medikation {bewohner.Name}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        leiste.MaximumSize = new Size(Width, 0);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(Math.Max(100, Width), 0)));   // AutoSize greift hier zu spät
        int y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is VerordnungKarte karte)
            {
                karte.SetBounds(0, y, Width, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                y += Stil.Px(this, Stil.AbstandKlein);
                c.Location = new Point(Stil.Px(this, Stil.AbstandMini), y);
                y = c.Bottom + Stil.Px(this, Stil.AbstandMini);
            }
        }
        if (Height != y) Height = y;
    }
}

// Eine Verordnung an einem Tag: Medikament und Stärke, die Mengen je Einnahmezeit, der Zustand
// (Grundverordnung, Stufe x von y, pausiert bis …), die Anordnung und die Knöpfe für Änderungen.
public sealed class VerordnungKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label name = new();
    private readonly Label dosis = new();
    private readonly Label anordnung = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Tagesstand Stand { get; }
    public Button PauseKnopf { get; } = Stil.NebenKnopf("Pause …");
    public Button PauseEndeKnopf { get; } = Stil.NebenKnopf("Pause beenden …");
    public Button StufenplanKnopf { get; } = Stil.NebenKnopf("Stufenplan …");
    public Button AbsetzenKnopf { get; } = Stil.NebenKnopf("Absetzen …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public IReadOnlyList<string> MarkenTexte { get; }
    public string DosisText => dosis.Text;

    public VerordnungKarte(Tagesstand s)
    {
        Stand = s;
        var v = s.Verordnung;
        var liste = new List<(string, Stil.Marke)>();
        var markeZustand = s.Zustand switch
        {
            Zustand.Pausiert => Stil.Achtung,
            Zustand.Stufenplan => Stil.Unterwegs,
            Zustand.Abgesetzt => Stil.Zimmer,
            Zustand.NochNicht => Stil.Zimmer,
            _ => Stil.Offen,
        };
        liste.Add((s.Text, markeZustand));
        if (s.Naechstes is string n) liste.Add((n, Stil.Unterwegs));
        if (v.Aenderungen.Count > 0) liste.Add(($"{v.Aenderungen.Count} Änderung{(v.Aenderungen.Count == 1 ? "" : "en")}", Stil.Zimmer));
        MarkenTexte = liste.Select(x => x.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        name.Text = v.Medikament + (v.Staerke.Length > 0 ? $" · {v.Staerke}" : " · Stärke: keine Angabe");
        name.Font = Stil.KarteName;
        name.ForeColor = s.Zustand is Zustand.Pausiert or Zustand.Abgesetzt ? Stil.TextLeise : Stil.TextNormal;
        dosis.Text = v.Bedarf
            ? $"bei Bedarf: {v.BedarfAnlass} · Einzeldosis {v.Dosis.Morgens} {v.Einheit}" + (v.BedarfHoechstens.Length > 0 ? $" · höchstens {v.BedarfHoechstens}" : "")
            : s.Zustand == Zustand.Pausiert ? $"pausiert – sonst morgens {s.Dosis.Morgens} · mittags {s.Dosis.Mittags} · abends {s.Dosis.Abends} · nachts {s.Dosis.Nachts}"
            : $"morgens {s.Dosis.Morgens} · mittags {s.Dosis.Mittags} · abends {s.Dosis.Abends} · nachts {s.Dosis.Nachts}  ({v.Einheit})";
        dosis.Font = s.Zustand == Zustand.Pausiert ? Stil.Standard : Stil.Fett;
        dosis.ForeColor = s.Zustand == Zustand.Pausiert ? Stil.TextLeise : Stil.TextNormal;
        anordnung.Text = $"angeordnet von {v.Anordnung.Arzt} am {Datum.Text(v.Anordnung.Am)} · seit {Datum.Text(v.Beginn)}" + (v.Hinweis.Length > 0 ? $" · {v.Hinweis}" : "");
        anordnung.Font = Stil.Klein;
        anordnung.ForeColor = Stil.TextLeise;
        foreach (var l in new[] { name, dosis, anordnung })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        bool aktiv = s.Zustand is not Zustand.Abgesetzt;
        PauseKnopf.Visible = aktiv && s.Zustand != Zustand.Pausiert;
        PauseEndeKnopf.Visible = s.Zustand == Zustand.Pausiert;
        StufenplanKnopf.Visible = aktiv && !v.Bedarf;
        AbsetzenKnopf.Visible = aktiv;
        foreach (var k in new[] { PauseKnopf, PauseEndeKnopf, StufenplanKnopf, AbsetzenKnopf, VerlaufKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{name.Text}: {dosis.Text}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Stand is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var l in new[] { name, dosis, anordnung })
        {
            l.MaximumSize = new Size(breite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, 3);
        }
        knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int hoehe = knoepfe.Bottom + innen - Stil.Px(this, Stil.AbstandMini);
        if (Height != hoehe) Height = hoehe;
    }
}

// Neue Verordnung: Medikament aus der Datenbank, Stärke, Mengen je Einnahmezeit oder Bedarf,
// Beginn und Anordnung (Arzt und Datum, in der Demo ausgedacht).
public sealed class VerordnungFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<Medikament> medikamente;

    public ComboBox MedikamentWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    public ComboBox Staerke { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 160 };
    public TextBox Einheit { get; } = new() { Width = 100, Text = "Stück" };
    public CheckBox Bedarf { get; } = new() { Text = "Bedarfsmedikation", AutoSize = true };
    public TextBox[] Mengen { get; } = Enumerable.Range(0, 4).Select(_ => new TextBox { Width = 50, Text = "0", TextAlign = HorizontalAlignment.Center }).ToArray();
    public TextBox Anlass { get; } = new() { Width = 260, PlaceholderText = "z. B. bei starker Unruhe", Enabled = false };
    public TextBox Hoechstens { get; } = new() { Width = 160, PlaceholderText = "z. B. 2 in 24 Stunden", Enabled = false };
    public TextBox Beginn { get; } = new() { Width = 100 };
    public TextBox Arzt { get; } = new() { Width = 260, PlaceholderText = "ausgedacht, z. B. Dr. Ulme" };
    public TextBox AngeordnetAm { get; } = new() { Width = 100 };
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Verordnung? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VerordnungFenster(Speicher speicher, Bewohner bewohner)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        medikamente = speicher.Medikamente();
        Text = $"Neue Verordnung – {bewohner.Name} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(MedikationRegeln.DruckHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        MedikamentWahl.Items.AddRange(medikamente.Select(m => (object)m.Anzeige).ToArray());
        MedikamentWahl.SelectedIndexChanged += (_, _) =>
        {
            Staerke.Items.Clear();
            if (MedikamentWahl.SelectedIndex >= 0) Staerke.Items.AddRange(medikamente[MedikamentWahl.SelectedIndex].Aktuell.Staerken.Select(x => (object)x).ToArray());
            Staerke.Text = Staerke.Items.Count > 0 ? (string)Staerke.Items[0]! : "";
        };
        zeile("Medikament", MedikamentWahl);
        zeile("Stärke, Einheit", Formular.Reihe(Staerke, Einheit));
        zeile("", Bedarf);
        var mengen = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        for (int i = 0; i < 4; i++)
        {
            mengen.Controls.Add(new Label { Text = MedikationRegeln.Zeiten[i], AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMini, 0) });
            Mengen[i].Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandMittel, 0);
            mengen.Controls.Add(Mengen[i]);
        }
        zeile("Menge", mengen);
        zeile("Bei Bedarf: Anlass", Anlass);
        zeile("höchstens", Hoechstens);
        Bedarf.CheckedChanged += (_, _) =>
        {
            Anlass.Enabled = Hoechstens.Enabled = Bedarf.Checked;
            for (int i = 1; i < 4; i++) Mengen[i].Enabled = !Bedarf.Checked;   // Bedarf: nur eine Einzeldosis (erstes Feld)
        };
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        Beginn.Text = heute;
        AngeordnetAm.Text = heute;
        zeile("Beginn", Beginn);
        zeile("Angeordnet von, am", Formular.Reihe(Arzt, AngeordnetAm));
        zeile("Hinweis", Hinweis);
        zeile("Kürzel", new Label { Text = Einstellungen.Laden().Kuerzel is { Length: > 0 } k ? k : "– bitte oben rechts anmelden –", AutoSize = true, Font = Stil.Fett, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) });
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            var m = MedikamentWahl.SelectedIndex >= 0 ? medikamente[MedikamentWahl.SelectedIndex] : null;
            var v = MedikationRegeln.AusEingabe(new VerordnungEingabe
            {
                BewohnerId = bewohner.Id!.Value, MedikamentId = m?.Id ?? 0, Staerke = Staerke.Text, Einheit = Einheit.Text,
                Morgens = Mengen[0].Text, Mittags = Mengen[1].Text, Abends = Mengen[2].Text, Nachts = Mengen[3].Text,
                Bedarf = Bedarf.Checked, BedarfAnlass = Anlass.Text, BedarfHoechstens = Hoechstens.Text,
                Beginn = Beginn.Text, Arzt = Arzt.Text, AngeordnetAm = AngeordnetAm.Text, Hinweis = Hinweis.Text,
                Kuerzel = Einstellungen.Laden().Kuerzel,
            }, m?.Anzeige ?? "", Jetzt());
            Gespeichert = speicher.VerordnungSpeichern(v);
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

// Pause, Pause beenden, Stufenplan oder Absetzen – immer mit Anordnung (Arzt, Datum).
public sealed class AenderungFenster : Form
{
    public const int StufenZeilen = 6;
    private readonly Speicher speicher;
    private readonly Verordnung verordnung;
    private readonly AenderungArt art;

    public TextBox Von { get; } = new() { Width = 100 };
    public TextBox Bis { get; } = new() { Width = 100, PlaceholderText = "leer = bis auf Weiteres" };
    public TextBox[,] Stufen { get; } = new TextBox[StufenZeilen, 5];
    public TextBox Arzt { get; } = new() { Width = 260, PlaceholderText = "ausgedacht, z. B. Dr. Ulme" };
    public TextBox AngeordnetAm { get; } = new() { Width = 100 };
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AenderungFenster(Speicher speicher, Verordnung v, AenderungArt art)
    {
        this.speicher = speicher;
        verordnung = v;
        this.art = art;
        string titel = art switch
        {
            AenderungArt.Pause => "Pause",
            AenderungArt.PauseEnde => "Pause beenden",
            AenderungArt.Stufenplan => "Stufenplan",
            _ => "Absetzen",
        };
        Text = $"{titel} – {v.Medikament} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = $"{v.Medikament} · {v.Staerke} · Grundverordnung {v.Dosis.Text}", AutoSize = true, Font = Stil.Fett, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        Von.Text = heute;
        switch (art)
        {
            case AenderungArt.Pause:
                zeile("Pause von", Von);
                zeile("bis (einschließlich)", Bis);
                break;
            case AenderungArt.PauseEnde:
                zeile("Pause endet am", Von);
                break;
            case AenderungArt.Absetzen:
                zeile("Abgesetzt ab", Von);
                break;
            default:
                var tabelle = new TableLayoutPanel { ColumnCount = 5, AutoSize = true, Margin = new Padding(0) };
                foreach (var kopf in new[] { "ab Datum", "morgens", "mittags", "abends", "nachts" })
                    tabelle.Controls.Add(new Label { Text = kopf, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel });
                for (int r = 0; r < StufenZeilen; r++)
                    for (int c = 0; c < 5; c++)
                    {
                        Stufen[r, c] = new TextBox { Width = c == 0 ? 100 : 50, TextAlign = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center, Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini) };
                        tabelle.Controls.Add(Stufen[r, c]);
                    }
                zeile("Stufen", tabelle);
                zeile(null, new Label
                {
                    Text = "Leere Zeilen werden nicht beachtet. Ist die letzte Stufe überall 0, ist das Medikament danach abgesetzt; " +
                           "sonst wird die letzte Stufe zur neuen Grundverordnung (die bisherige bleibt im Verlauf).",
                    AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise,
                });
                break;
        }
        AngeordnetAm.Text = heute;
        zeile("Angeordnet von, am", Formular.Reihe(Arzt, AngeordnetAm));
        zeile("Hinweis", Hinweis);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Eine Stufenzeile füllen (für Tests und schnelle Eingabe).
    public void StufeSetzen(int zeile, string ab, string morgens, string mittags, string abends, string nachts)
    {
        string[] werte = [ab, morgens, mittags, abends, nachts];
        for (int c = 0; c < 5; c++) Stufen[zeile, c].Text = werte[c];
    }

    public bool Speichern()
    {
        string kuerzel = Einstellungen.Laden().Kuerzel;
        var jetzt = Jetzt();
        try
        {
            var a = art switch
            {
                AenderungArt.Pause => MedikationRegeln.Pause(Von.Text, Bis.Text, Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
                AenderungArt.PauseEnde => MedikationRegeln.PauseEnde(Von.Text, Arzt.Text, AngeordnetAm.Text, kuerzel, jetzt),
                AenderungArt.Absetzen => MedikationRegeln.Absetzen(Von.Text, Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
                _ => MedikationRegeln.Stufenplan(Enumerable.Range(0, StufenZeilen)
                        .Select(r => (Stufen[r, 0].Text, Stufen[r, 1].Text, Stufen[r, 2].Text, Stufen[r, 3].Text, Stufen[r, 4].Text))
                        .Where(z => z.Item1.Trim().Length > 0).ToList(), Arzt.Text, AngeordnetAm.Text, Hinweis.Text, kuerzel, jetzt),
            };
            speicher.AenderungSpeichern(verordnung.Id, a);
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

// Ausdruck „Was gilt am …“ je Bewohner (A4 hoch), mit dem Vermerk „Demo — nicht zum Stellen verwenden“.
public sealed class MedikationDruck(Bewohner bewohner, DateOnly tag, IReadOnlyList<Tagesstand> staende)
{
    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = $"Medikation {bewohner.Name}" };
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 20f, rechts = 190f;
        float y = 15f;
        using var titel = new Font("Segoe UI Semibold", 14F);
        using var fett = new Font("Segoe UI Semibold", 10F);
        using var normal = new Font("Segoe UI", 10F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.2f);
        using var rot = new SolidBrush(Color.FromArgb(163, 34, 26));
        g.DrawString($"Medikation – {bewohner.Name}, Zi. {bewohner.Zimmer}", titel, Brushes.Black, links, y);
        y += 8f;
        g.DrawString($"Was gilt am {tag.ToString("dddd, dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))}", normal, Brushes.Black, links, y);
        y += 6f;
        g.DrawString($"{MedikationRegeln.DruckHinweis} · {KopfBereich.DemoHinweis}", fett, rot, links, y);
        y += 7f;
        float[] spalten = [links, links + 80f, links + 100f, links + 120f, links + 140f, links + 160f];
        void Kopf()
        {
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
            string[] k = ["Medikament, Stärke", "morgens", "mittags", "abends", "nachts"];
            for (int i = 0; i < k.Length; i++) g.DrawString(k[i], klein, Brushes.DimGray, spalten[i], y);
            y += 5f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
        }
        g.DrawString("Regelmäßig", fett, Brushes.Black, links, y);
        y += 6f;
        Kopf();
        foreach (var s in staende.Where(x => x.Sichtbar && !x.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            g.DrawString($"{v.Medikament} {v.Staerke}", normal, Brushes.Black, new RectangleF(spalten[0], y, 78f, 12f));
            if (s.Zustand == Zustand.Pausiert)
                g.DrawString(s.Text.ToUpperInvariant(), fett, rot, spalten[1], y);
            else
                for (int i = 0; i < 4; i++) g.DrawString(s.Dosis.Werte.ElementAt(i), normal, Brushes.Black, spalten[i + 1] + 6f, y);
            y += 6f;
            string zusatz = s.Text + (s.Naechstes is string n ? $" · {n}" : "") + $" · angeordnet von {v.Anordnung.Arzt} am {Datum.Text(v.Anordnung.Am)}";
            g.DrawString(zusatz, klein, Brushes.DimGray, spalten[0], y);
            y += 5f;
            g.DrawLine(linie, links, y, rechts, y);
            y += 1.5f;
        }
        y += 4f;
        g.DrawString("Bei Bedarf", fett, Brushes.Black, links, y);
        y += 6f;
        if (!staende.Any(x => x.Sichtbar && x.Verordnung.Bedarf)) g.DrawString("Keine Bedarfsmedikation.", normal, Brushes.DimGray, links, y);
        foreach (var s in staende.Where(x => x.Sichtbar && x.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            g.DrawString($"{v.Medikament} {v.Staerke}: {v.BedarfAnlass}, Einzeldosis {v.Dosis.Morgens} {v.Einheit}" +
                         (v.BedarfHoechstens.Length > 0 ? $", höchstens {v.BedarfHoechstens}" : "") +
                         (s.Zustand == Zustand.Pausiert ? $" – {s.Text}" : ""), normal, Brushes.Black, new RectangleF(links, y, rechts - links, 12f));
            y += 6f;
        }
    }
}
