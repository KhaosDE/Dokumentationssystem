using DokuLine.Kern;

namespace DokuLine;

// Ein Angebot am Stichtag: Name, Art, Termine, Therapeut, Ort, Teilnehmer und freie Plätze.
public sealed class AngebotKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label name = new();
    private readonly Label zeile = new();
    private readonly Label teilnehmer = new();
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Angebot Angebot { get; }
    public Button ZuteilenKnopf { get; } = Stil.NebenKnopf("Bewohner zuteilen …");
    public Button BeendenKnopf { get; } = Stil.NebenKnopf("Teilnahme beenden …");
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern ab …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public IReadOnlyList<string> MarkenTexte { get; }
    public string TeilnehmerText => teilnehmer.Text;

    public AngebotKarte(Angebot a, DateOnly tag, IReadOnlyDictionary<long, string> namen)
    {
        Angebot = a;
        var f = a.Am(tag);
        var dabei = a.TeilnehmerAm(tag);
        int frei = a.FreiAm(tag);
        var liste = new List<(string, Stil.Marke)>
        {
            (a.Anbieter, a.Anbieter == TherapieRegeln.AnbieterTherapie ? Stil.Hinweis : Stil.Unterwegs),   // Anbieter
            (TherapieRegeln.ArtText(f.Art), Stil.Zimmer),
            (f.TermineText, Stil.Unterwegs),
            (frei == 0 ? "voll" : frei == 1 ? "1 Platz frei" : $"{frei} Plätze frei", frei == 0 ? Stil.Achtung : Stil.Offen),
        };
        if (a.Fassungen[0].GiltAb > tag) liste.Add(($"beginnt am {Datum.Text(a.Fassungen[0].GiltAb)}", Stil.Achtung));
        var spaeter = a.Fassungen.FirstOrDefault(x => x.GiltAb > tag && x != a.Fassungen[0]);
        if (spaeter is not null) liste.Add(($"geändert ab {Datum.Text(spaeter.GiltAb)}", Stil.Hinweis));
        MarkenTexte = liste.Select(x => x.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        name.Text = f.Name;
        name.Font = Stil.KarteName;
        name.ForeColor = Stil.TextNormal;
        zeile.Text = $"{f.Therapeut} · {f.Ort}" + (f.Hinweis.Length > 0 ? $" · {f.Hinweis}" : "");
        zeile.Font = Stil.Standard;
        zeile.ForeColor = Stil.TextMittel;
        var kommen = a.Zuteilungen.Where(z => z.Ab > tag).ToList();
        teilnehmer.Text = $"Teilnehmer ({dabei.Count} von {f.Hoechstens}): " +
                          (dabei.Count == 0 ? "niemand" : string.Join(", ", dabei.Select(z => namen.GetValueOrDefault(z.BewohnerId, "?") + (z.Ende is DateOnly e ? $" (bis {Datum.Text(e)})" : "")))) +
                          (kommen.Count > 0 ? " · später dazu: " + string.Join(", ", kommen.Select(z => $"{namen.GetValueOrDefault(z.BewohnerId, "?")} ab {Datum.Text(z.Ab)}")) : "");
        teilnehmer.Font = Stil.Standard;
        teilnehmer.ForeColor = Stil.TextNormal;
        foreach (var l in new[] { name, zeile, teilnehmer })
        {
            l.AutoSize = true;
            l.BackColor = Stil.Flaeche;
            l.UseMnemonic = false;
            Controls.Add(l);
        }
        BeendenKnopf.Visible = dabei.Count > 0 || kommen.Count > 0;
        foreach (var k in new[] { ZuteilenKnopf, BeendenKnopf, AendernKnopf, VerlaufKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{f.Name}, {string.Join(", ", MarkenTexte)}. {teilnehmer.Text}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Angebot is null) return;   // Anordnung schon im Konstruktor der Basisklasse
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var l in new[] { name, zeile, teilnehmer })
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

// Angebot anlegen oder „ab Datum“ ändern (neue Fassung; die bisherige bleibt im Verlauf).
public sealed class AngebotFenster : Form
{
    public const int TerminZeilen = 3;
    private readonly Speicher speicher;
    private readonly Angebot? bisher;

    public TextBox NameFeld { get; } = new() { Width = 300 };
    public RadioButton Gruppe { get; } = new() { Text = "Gruppe", AutoSize = true, Checked = true };
    public RadioButton Einzel { get; } = new() { Text = "Einzel", AutoSize = true };
    public TextBox Therapeut { get; } = new() { Width = 260, PlaceholderText = "ausgedacht" };
    public TextBox Ort { get; } = new() { Width = 260 };
    public ComboBox[] Tage { get; } = new ComboBox[TerminZeilen];
    public TextBox[] Von { get; } = new TextBox[TerminZeilen];
    public TextBox[] Bis { get; } = new TextBox[TerminZeilen];
    public TextBox Teilnehmer { get; } = new() { Width = 60 };
    public Label TeilnehmerZusatz { get; }
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public TextBox GiltAb { get; } = new DatumFeld() { Width = 100 };
    public ComboBox Anbieter { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Angebot? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AngebotFenster(Speicher speicher, Angebot? bisher)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        Text = (bisher is null ? "Neues Angebot" : "Angebot ändern") + $" ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        zeile("Name", NameFeld);
        // Anbieter: nur, wofür der Angemeldete Angebote anlegen darf; beim Ändern bleibt der bisherige wählbar.
        var anbieter = speicher.AngebotAnbieter(Einstellungen.Laden().Kuerzel);
        if (bisher is not null && !anbieter.Contains(bisher.Anbieter)) anbieter.Insert(0, bisher.Anbieter);
        Anbieter.Items.AddRange([.. anbieter]);
        if (Anbieter.Items.Count > 0) Anbieter.SelectedItem = bisher?.Anbieter ?? anbieter[0];
        zeile("Anbieter", Anbieter);
        zeile("Art", Formular.Reihe(Gruppe, Einzel));
        zeile("Therapeut/in", Therapeut);
        zeile("Ort", Ort);
        var termine = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Margin = new Padding(0) };
        string[] tagNamen = ["", .. TherapieRegeln.Woche.Select(TherapieRegeln.TagName)];
        for (int i = 0; i < TerminZeilen; i++)
        {
            Tage[i] = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini) };
            Tage[i].Items.AddRange(tagNamen.Cast<object>().ToArray());
            Tage[i].SelectedIndex = 0;
            Von[i] = new UhrzeitFeld { Width = 70, PlaceholderText = "von", Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini) };
            Bis[i] = new UhrzeitFeld { Width = 70, PlaceholderText = "bis", Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini) };
            termine.Controls.AddRange([Tage[i], Von[i], Bis[i]]);
        }
        zeile("Termine (jede Woche)", termine);
        TeilnehmerZusatz = Formular.ZeileMitZusatz(raster, "Teilnehmer", "(Anzahl)", Teilnehmer);
        Einzel.CheckedChanged += (_, _) => { Teilnehmer.Enabled = !Einzel.Checked; if (Einzel.Checked) Teilnehmer.Text = "1"; };
        zeile("Hinweis", Hinweis);
        GiltAb.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        zeile(bisher is null ? "Beginnt am" : "Änderung gilt ab", GiltAb);
        if (bisher is not null)
        {
            var f = bisher.Am(DateOnly.FromDateTime(Jetzt()));
            NameFeld.Text = f.Name;
            (f.Art == AngebotArt.Einzel ? Einzel : Gruppe).Checked = true;
            Therapeut.Text = f.Therapeut;
            Ort.Text = f.Ort;
            for (int i = 0; i < Math.Min(TerminZeilen, f.Termine.Count); i++)
            {
                Tage[i].SelectedIndex = Array.IndexOf(TherapieRegeln.Woche, f.Termine[i].Tag) + 1;
                Von[i].Text = f.Termine[i].Von.ToString("HH:mm");
                Bis[i].Text = f.Termine[i].Bis.ToString("HH:mm");
            }
            Teilnehmer.Text = f.Hoechstens.ToString();
            Hinweis.Text = f.Hinweis;
        }
        zeile(null, new Label { Text = bisher is null ? "" : "Die bisherige Fassung bleibt im Verlauf; bis zum Datum gilt sie weiter.", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise });
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    // Einen Termin füllen (für Tests und schnelle Eingabe). tag: Index in TherapieRegeln.Woche.
    public void TerminSetzen(int zeile, DayOfWeek tag, string von, string bis)
    {
        Tage[zeile].SelectedIndex = Array.IndexOf(TherapieRegeln.Woche, tag) + 1;
        Von[zeile].Text = von;
        Bis[zeile].Text = bis;
    }

    public bool Speichern()
    {
        try
        {
            var f = TherapieRegeln.AusEingabe(new AngebotEingabe
            {
                Name = NameFeld.Text, Art = Einzel.Checked ? AngebotArt.Einzel : AngebotArt.Gruppe, Therapeut = Therapeut.Text, Ort = Ort.Text,
                Termine = Enumerable.Range(0, TerminZeilen)
                    .Select(i => (Tage[i].SelectedIndex > 0 ? (DayOfWeek?)TherapieRegeln.Woche[Tage[i].SelectedIndex - 1] : null, Von[i].Text, Bis[i].Text)).ToList(),
                Hoechstens = Teilnehmer.Text, Hinweis = Hinweis.Text, GiltAb = GiltAb.Text, Kuerzel = Einstellungen.Laden().Kuerzel,
                Anbieter = Anbieter.SelectedItem as string ?? "",
            }, Jetzt());
            Gespeichert = bisher is null ? speicher.AngebotAnlegen(f) : speicher.AngebotAendern(bisher.Id, f);
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

// Bewohner zuteilen: ab Datum, optional bis Datum. Überschneidet sich das mit einer anderen
// Therapie des Bewohners, erscheint eine Warnung; ein zweiter Klick teilt trotzdem zu.
public sealed class ZuteilenFenster : Form
{
    private readonly Speicher speicher;
    private readonly Angebot angebot;
    private readonly List<Bewohner> bewohner;
    private string? gewarnt;   // für welche Auswahl schon gewarnt wurde

    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    public TextBox Ab { get; } = new DatumFeld() { Width = 100 };
    public TextBox Bis { get; } = new DatumFeld() { Width = 100, PlaceholderText = "leer = offen" };
    public Label Warnung { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public ZuteilenFenster(Speicher speicher, Angebot angebot)
    {
        this.speicher = speicher;
        this.angebot = angebot;
        bewohner = speicher.AlleBewohner();
        var heute = DateOnly.FromDateTime(Jetzt());
        Text = $"Zuteilen: {angebot.Am(heute).Name} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = $"{angebot.Am(heute).Name} · {angebot.Am(heute).TermineText} · {angebot.FreiAm(heute)} frei", AutoSize = true, Font = Stil.Fett, Margin = new Padding(0, 0, 0, Stil.AbstandMittel) });
        BewohnerWahl.Items.AddRange(bewohner.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
        zeile("Bewohner", BewohnerWahl);
        Ab.Text = Datum.Text(heute);
        zeile("ab", Ab);
        zeile("bis (freiwillig)", Bis);
        zeile(null, Warnung);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Zuteilen", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public void BewohnerWaehlen(long id) => BewohnerWahl.SelectedIndex = bewohner.FindIndex(b => b.Id == id);

    public bool Speichern()
    {
        Fehler.Visible = false;
        try
        {
            if (BewohnerWahl.SelectedIndex < 0) throw new Eingabefehler(["Bitte einen Bewohner wählen."]);
            var b = bewohner[BewohnerWahl.SelectedIndex];
            DateOnly ab;
            DateOnly? bis = null;
            try
            {
                ab = Datum.Lesen(Ab.Text, "Ab");
                if (Bis.Text.Trim().Length > 0) bis = Datum.Lesen(Bis.Text, "Bis");
            }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            string auswahl = $"{b.Id}|{ab}|{bis}";
            var konflikte = speicher.Ueberschneidungen(angebot.Id, b.Id!.Value, ab, bis);
            if (konflikte.Count > 0 && gewarnt != auswahl)
            {
                gewarnt = auswahl;
                Warnung.Text = "Überschneidung mit: " + string.Join("; ", konflikte.Select(k => $"{k.Was} {k.Termin.Text}")) +
                               ". Noch einmal „Zuteilen“ teilt trotzdem zu.";
                Warnung.Visible = true;
                return false;
            }
            speicher.Zuteilen(angebot.Id, b.Id!.Value, ab, bis, Einstellungen.Laden().Kuerzel, Jetzt());
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

// Teilnahme beenden: welche Zuteilung, ab welchem Tag nicht mehr (eigener Eintrag).
public sealed class BeendenFenster : Form
{
    private readonly Speicher speicher;
    private readonly List<Zuteilung> zuteilungen;

    public ComboBox Teilnehmer { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
    public TextBox Ab { get; } = new DatumFeld() { Width = 100 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public BeendenFenster(Speicher speicher, Angebot angebot, DateOnly tag)
    {
        this.speicher = speicher;
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        zuteilungen = angebot.Zuteilungen.Where(z => z.BeendetAb is null && (z.Ende is not DateOnly e || e >= tag)).ToList();
        Text = $"Teilnahme beenden: {angebot.Am(tag).Name} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        Teilnehmer.Items.AddRange(zuteilungen.Select(z => (object)$"{namen.GetValueOrDefault(z.BewohnerId, "?")} (seit {Datum.Text(z.Ab)})").ToArray());
        if (Teilnehmer.Items.Count > 0) Teilnehmer.SelectedIndex = 0;
        zeile("Teilnehmer", Teilnehmer);
        Ab.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()).AddDays(1));
        zeile("nicht mehr ab", Ab);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Beenden", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            if (Teilnehmer.SelectedIndex < 0) throw new Eingabefehler(["Bitte einen Teilnehmer wählen."]);
            DateOnly ab;
            try { ab = Datum.Lesen(Ab.Text, "Ab"); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            speicher.ZuteilungBeenden(zuteilungen[Teilnehmer.SelectedIndex].Id, ab, Einstellungen.Laden().Kuerzel, Jetzt());
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
