using DokuLine.Kern;

namespace DokuLine;

// Bereich „Medikamentenausgabe“ (Bauplan 42, nur Wohnbereiche): Einnahmezeit wählen (vorgewählt die laufende), je Bewohner
// des Bereichs die Medikamente dieser Zeit und ein Klick „ausgegeben“, „nicht abgeholt“, „abgelehnt“, „abwesend“ oder
// „mitgegeben“; „Einzeln …“ kennzeichnet einzelne Medikamente. „Nicht abgeholt“ steht oben. Kürzel und Uhrzeit setzt das
// Programm. Gegen Ende des Dienstes ein Hinweis, wenn noch etwas offen ist.
public sealed class AusgabeSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Medikamentenausgabe";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel zeiten = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    public List<FilterKnopf> ZeitKnoepfe { get; } = [];
    public Label Zaehler { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
    public Label DienstendeHinweis { get; } = new()
    {
        AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini), Visible = false,
        UseMnemonic = false,
    };
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        ShowItemToolTips = true, Font = Stil.Standard, AccessibleName = "Medikamentenausgabe",
    };
    public Dictionary<AusgabeStatus, Button> StatusKnoepfe { get; } = [];
    public Button EinzelnKnopf { get; } = Stil.NebenKnopf("Einzeln …");
    public Label Hinweis { get; } = new()
    {
        Text = "Das Programm bestätigt nur die Ausgabe – es prüft keine Einnahme. Pausierte, abgesetzte und Bedarfsmedikamente stehen nicht hier.",
        AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, UseMnemonic = false,
    };
    public Einnahmezeit Zeit { get; private set; }
    public DateOnly Ausgabetag { get; private set; }
    public List<AusgabeZeile> Zeilen { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public event Action? Geaendert;
    private bool zeitGewaehlt;

    public AusgabeSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var z in AusgabeRegeln.Zeiten)
        {
            var k = new FilterKnopf(AusgabeRegeln.ZeitText(z), mitZahl: false) { Font = Stil.TagKopf, Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein) };
            k.Size = k.GetPreferredSize(Size.Empty) + new Size(Stil.AbstandGross, Stil.AbstandKlein);
            k.Click += (_, _) => ZeitWaehlen(z);
            ZeitKnoepfe.Add(k);
            zeiten.Controls.Add(k);
        }
        foreach (var (titel, breite) in new[] { ("Zimmer", 90), ("Name", 190), ("Medikamente", 460), ("Stand", 260) }) Liste.Columns.Add(titel, breite);
        foreach (var s in Enum.GetValues<AusgabeStatus>())
        {
            var k = s == AusgabeStatus.Ausgegeben ? Stil.HauptKnopf(AusgabeRegeln.StatusText(s)) : Stil.NebenKnopf(AusgabeRegeln.StatusText(s));
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            k.Click += (_, _) => Setzen(s);
            StatusKnoepfe[s] = k;
            knoepfe.Controls.Add(k);
        }
        EinzelnKnopf.Margin = new Padding(Stil.AbstandMittel, 0, 0, Stil.AbstandKlein);
        EinzelnKnopf.Click += (_, _) => Einzeln();
        knoepfe.Controls.Add(EinzelnKnopf);
        Liste.DoubleClick += (_, _) => Setzen(AusgabeStatus.Ausgegeben);
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Controls.AddRange([zeiten, Zaehler, DienstendeHinweis, knoepfe, Liste, Hinweis]);
    }

    public AusgabeZeile? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as AusgabeZeile : null;

    // Die eigene Wahl gilt nur, solange dieselbe Einnahmezeit läuft; danach springt die Seite wieder auf die laufende.
    private (DateOnly Tag, Einnahmezeit Zeit)? gewaehltBei;

    public void ZeitWaehlen(Einnahmezeit z)
    {
        Zeit = z;
        zeitGewaehlt = true;
        gewaehltBei = AusgabeRegeln.Jetzt(Jetzt());
        Aktualisieren();
    }

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var (tag, laufend) = AusgabeRegeln.Jetzt(jetzt);
        Ausgabetag = tag;
        if (zeitGewaehlt && gewaehltBei != (tag, laufend)) zeitGewaehlt = false;
        if (!zeitGewaehlt) Zeit = laufend;
        foreach (var k in ZeitKnoepfe) k.Aktiv = k.Text == AusgabeRegeln.ZeitText(Zeit);
        var bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt);
        var abwesend = bewohner.Where(b => Modulverwaltung.Marken(speicher, b, jetzt).Any(m => m.Art == MarkenArt.Abwesend)).Select(b => b.Id!.Value).ToHashSet();
        long? vorher = Gewaehlt?.Bewohner.Id;
        Zeilen = speicher.Ausgabeliste(bewohner, Ausgabetag, Zeit, abwesend);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var z in Zeilen)
        {
            var zeile = new ListViewItem([z.Bewohner.Zimmer, z.Bewohner.Name, string.Join(" · ", z.Medikamente.Select(m => m.Text)), z.StandText])
            { Tag = z, UseItemStyleForSubItems = false, ToolTipText = string.Join("\n", z.Medikamente.Select(m => $"{m.Text}: {(m.Eintrag is { } e ? AusgabeRegeln.StatusText(e.Status) : "offen")}")) };
            if (z.NichtAbgeholt) { zeile.SubItems[3].ForeColor = Stil.Achtung.Text; zeile.SubItems[3].BackColor = Stil.Achtung.Flaeche; }
            else if (z.Erledigt) foreach (ListViewItem.ListViewSubItem s in zeile.SubItems) s.ForeColor = Stil.TextLeise;
            Liste.Items.Add(zeile);
            if (z.Bewohner.Id == vorher) zeile.Selected = true;
        }
        Liste.EndUpdate();
        Zaehler.Text = $"{AusgabeRegeln.ZeitText(Zeit)} · {AusgabeRegeln.Zaehler(Zeilen)}" + (Zeilen.Count(z => z.NichtAbgeholt) is int n and > 0 ? $" · {n} noch nicht abgeholt" : "");
        // Gegen Ende des eigenen Dienstes (letzte Stunde): noch Offenes dieses Dienstes?
        var dienst = SchichtTermin.Zu(jetzt);
        int offen = dienst.Ende - jetzt <= TimeSpan.FromHours(1)
            ? AusgabeRegeln.ZeitenDesDienstes(dienst.Schicht).Sum(z => speicher.Ausgabeliste(bewohner, dienst.Tag, z).Count(x => !x.Erledigt)) : 0;
        DienstendeHinweis.Text = $"Der {BerichtRegeln.SchichtText(dienst.Schicht)} endet bald: {offen} Bewohner noch ohne Ausgabe – Offenes geht als Sammeleintrag in die Übergabe.";
        DienstendeHinweis.Visible = offen > 0;
        KopfText = $"{Arbeitsbereich.Gewaehlt} · {Datum.Text(Ausgabetag)} {AusgabeRegeln.ZeitText(Zeit)} · {AusgabeRegeln.Zaehler(Zeilen)}";
        KnoepfeSetzen();
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void KnoepfeSetzen()
    {
        var alle = StatusKnoepfe.Values.Append(EinzelnKnopf).ToArray();
        Recht.Anwenden(speicher, Funktionen.MedikamenteAusgeben, alle);
        if (Stil.Sperrgrund(EinzelnKnopf) is not null) return;
        string? ohne = Gewaehlt is null ? "Bitte zuerst einen Bewohner in der Liste wählen." : null;
        foreach (var k in alle) Stil.Sperren(k, ohne);
    }

    // Ein Klick: der Status für alle Medikamente dieser Zeit.
    public bool Setzen(AusgabeStatus status, IReadOnlyCollection<long>? verordnungen = null)
    {
        if (Gewaehlt is not AusgabeZeile z || Stil.Sperrgrund(StatusKnoepfe[status]) is not null) return false;
        try { speicher.AusgabeSetzen(z.Bewohner.Id!.Value, Ausgabetag, Zeit, status, Recht.Kuerzel, Jetzt(), verordnungen); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Medikamentenausgabe", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        Aktualisieren();
        return true;
    }

    // Einzelne Medikamente: aufklappen und getrennt kennzeichnen (z. B. eines abgelehnt).
    public bool Einzeln()
    {
        if (Gewaehlt is not AusgabeZeile z || Stil.Sperrgrund(EinzelnKnopf) is not null) return false;
        using var fenster = new AusgabeEinzelnFenster(z, AusgabeRegeln.ZeitText(Zeit));
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK || fenster.Gewaehlt.Count == 0) return false;
        return Setzen(fenster.Status, fenster.Gewaehlt);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, ClientSize.Width - 2 * rand), y = rand;
        foreach (var c in new Control[] { zeiten, Zaehler, DienstendeHinweis, knoepfe }.Where(c => c.Visible))
        {
            c.MaximumSize = new Size(breite, 0);
            c.Location = new Point(rand, y);
            y = c.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        Hinweis.MaximumSize = new Size(breite, 0);
        int unten = ClientSize.Height - rand - Hinweis.PreferredHeight;
        Liste.SetBounds(rand, y, breite, Math.Max(Stil.Px(this, 120), unten - y - Stil.Px(this, Stil.AbstandKlein)));
        Hinweis.Location = new Point(rand, Liste.Bottom + Stil.Px(this, Stil.AbstandKlein));
    }
}

// „Einzeln …“: die Medikamente dieser Zeit zum Ankreuzen und der Status für die angekreuzten.
public sealed class AusgabeEinzelnFenster : Form
{
    public List<CheckBox> Medikamente { get; } = [];
    public ComboBox StatusWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, AccessibleName = "Status" };
    public AusgabeStatus Status => (AusgabeStatus)Math.Max(0, StatusWahl.SelectedIndex);
    public List<long> Gewaehlt => [.. Medikamente.Where(c => c.Checked).Select(c => (long)c.Tag!)];

    public AusgabeEinzelnFenster(AusgabeZeile zeile, string zeit)
    {
        Fenster.Grundform(this, $"Medikamente {zeit}: {zeile.Bewohner.Name}");
        StatusWahl.Items.AddRange([.. Enum.GetValues<AusgabeStatus>().Select(AusgabeRegeln.StatusText)]);
        StatusWahl.SelectedIndex = 0;
        var raster = Formular.Raster(out var reihe);
        reihe(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        foreach (var m in zeile.Medikamente)
        {
            var box = new CheckBox
            {
                Text = $"{m.Text}{(m.Eintrag is { } e ? $" – bisher: {AusgabeRegeln.StatusText(e.Status)}" : "")}", AutoSize = true, Tag = m.Verordnung.Id,
                Checked = m.Eintrag is null, UseMnemonic = false,
            };
            Medikamente.Add(box);
            reihe(null, box);
        }
        reihe("Status", StatusWahl);
        reihe(null, Formular.Knoepfe(this, "Übernehmen", () => { DialogResult = DialogResult.OK; Close(); }));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }
}
