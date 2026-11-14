using DokuLine.Kern;

namespace DokuLine;

// Bereich „Archiv“ (Bauplan 46, Recht „Archiv einsehen“): ehemalige Bewohner mit Bereich, Aufenthalt, Grund und
// „Aufbewahrung bis“; Suche und „nur abgelaufene“. Akte nur lesend, Wiederaufnahme, Löschen nur zu zweit nach Ablauf der Frist.
public sealed class ArchivSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Archiv";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [AnsehenKnopf, WiederKnopf, FreigebenKnopf, LoeschenKnopf, ProtokollKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    public TextBox Suche { get; } = new() { Width = 220, PlaceholderText = "Name, Zimmer, Bereich, Grund", AccessibleName = "Im Archiv suchen" };
    public CheckBox NurAbgelaufen { get; } = new() { Text = "nur Aufbewahrung abgelaufen", AutoSize = true, BackColor = Stil.Seite };
    public Button AnsehenKnopf { get; } = Stil.HauptKnopf("Akte ansehen …");
    public Button WiederKnopf { get; } = Stil.NebenKnopf("Wieder aufnehmen …");
    public Button FreigebenKnopf { get; } = Stil.NebenKnopf("Löschen freigeben");
    public Button LoeschenKnopf { get; } = Stil.NebenKnopf("Endgültig löschen …");
    public Button ProtokollKnopf { get; } = Stil.NebenKnopf("Löschprotokoll …");
    public Label Hinweis { get; } = new() { AutoSize = true, BackColor = Stil.Achtung.Flaeche, ForeColor = Stil.Achtung.Text, Padding = new Padding(Stil.AbstandMini), Visible = false };
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        AccessibleName = "Archiv", Font = Stil.Standard,
    };
    public List<ArchivZeile> Zeilen { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // Zum Testen ersetzbar: Rückfrage vor dem endgültigen Löschen (true = löschen).
    internal Func<string, bool> LoeschenBestaetigen { get; set; }
    public event Action? Geaendert;

    public ArchivSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        LoeschenBestaetigen = text => MessageBox.Show(FindForm(), text, "Endgültig löschen", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        foreach (var c in new Control[] { Suche, NurAbgelaufen }) c.Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandKlein);
        leiste.Controls.AddRange([Suche, NurAbgelaufen]);
        foreach (var (titel, breite) in new[] { ("Name", 180), ("Bereich", 70), ("Zimmer", 70), ("Aufenthalt", 190), ("Grund", 110), ("Aufbewahrung bis", 140), ("Löschen", 200) })
            Liste.Columns.Add(titel, breite);
        Suche.TextChanged += (_, _) => Fuellen();
        NurAbgelaufen.CheckedChanged += (_, _) => Fuellen();
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Liste.DoubleClick += (_, _) => Ansehen();
        AnsehenKnopf.Click += (_, _) => Ansehen();
        WiederKnopf.Click += (_, _) => Wiederaufnehmen();
        FreigebenKnopf.Click += (_, _) => Freigeben();
        LoeschenKnopf.Click += (_, _) => Loeschen();
        ProtokollKnopf.Click += (_, _) => ProtokollZeigen();
        Controls.AddRange([leiste, Hinweis, Liste]);
    }

    public ArchivZeile? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as ArchivZeile : null;

    public void Aktualisieren()
    {
        Zeilen = speicher.Archiv();
        Fuellen();
    }

    private void Fuellen()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        var gezeigt = ArchivRegeln.Suchen(Zeilen, Suche.Text).Where(z => !NurAbgelaufen.Checked || z.Abgelaufen(heute)).ToList();
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var z in gezeigt)
        {
            string loeschen = z.Freigabe is { } f ? $"freigegeben von {f.Kuerzel} am {Datum.Text(DateOnly.FromDateTime(f.Zeit))}" : z.Abgelaufen(heute) ? "abgelaufen" : "";
            var zeile = new ListViewItem([z.Bewohner.Name, z.Eintrag.Wohnbereich, z.Eintrag.Zimmer, ArchivRegeln.ZeitraumText(z.Eintrag),
                ArchivRegeln.GrundText(z.Eintrag.Grund), ArchivRegeln.AufbewahrenText(z.AufbewahrenBis), loeschen])
            { Tag = z, UseItemStyleForSubItems = false };
            if (z.AufbewahrenBis is null || z.Abgelaufen(heute)) zeile.SubItems[5].ForeColor = Stil.Achtung.Text;
            Liste.Items.Add(zeile);
        }
        Liste.EndUpdate();
        Hinweis.Text = ArchivRegeln.FristFehlt;
        Hinweis.Visible = speicher.ArchivFrist() is null;
        int abgelaufen = Zeilen.Count(z => z.Abgelaufen(heute));
        KopfText = $"{Zeilen.Count} ehemalige Bewohner" + (abgelaufen > 0 ? $" · {abgelaufen} mit abgelaufener Aufbewahrung" : "");
        KnoepfeSetzen();
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void KnoepfeSetzen()
    {
        Recht.Anwenden(speicher, Funktionen.ArchivEinsehen, AnsehenKnopf, ProtokollKnopf);
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, WiederKnopf);
        Recht.Anwenden(speicher, Funktionen.ArchivLoeschen, FreigebenKnopf, LoeschenKnopf);
        string? ohne = Gewaehlt is null ? "Bitte zuerst einen Bewohner wählen." : null;
        var heute = DateOnly.FromDateTime(Jetzt());
        foreach (var k in new[] { AnsehenKnopf, WiederKnopf })
            if (Stil.Sperrgrund(k) is null) Stil.Sperren(k, ohne);
        if (Stil.Sperrgrund(FreigebenKnopf) is null) Stil.Sperren(FreigebenKnopf, ohne ?? ArchivRegeln.LoeschenPruefen(Gewaehlt!, heute, Recht.Kuerzel, endgueltig: false));
        if (Stil.Sperrgrund(LoeschenKnopf) is null) Stil.Sperren(LoeschenKnopf, ohne ?? ArchivRegeln.LoeschenPruefen(Gewaehlt!, heute, Recht.Kuerzel, endgueltig: true));
    }

    public bool Ansehen()
    {
        if (Gewaehlt is not ArchivZeile z || Stil.Sperrgrund(AnsehenKnopf) is not null) return false;
        using var fenster = new ArchivAkteFenster(speicher, z.Bewohner);
        Dialog.Zeigen(fenster, FindForm());
        return true;
    }

    public bool Wiederaufnehmen()
    {
        if (Gewaehlt is not ArchivZeile z || Stil.Sperrgrund(WiederKnopf) is not null) return false;
        using var fenster = new WiederaufnahmeFenster(speicher, z.Bewohner) { Jetzt = Jetzt };
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return false;
        Aktualisieren();
        return true;
    }

    public bool Freigeben()
    {
        if (Gewaehlt is not ArchivZeile z || Stil.Sperrgrund(FreigebenKnopf) is not null) return false;
        try { speicher.LoeschenFreigeben(z.Bewohner.Id!.Value, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Löschen freigeben"); return false; }
        Aktualisieren();
        return true;
    }

    public bool Loeschen()
    {
        if (Gewaehlt is not ArchivZeile z || Stil.Sperrgrund(LoeschenKnopf) is not null) return false;
        if (!LoeschenBestaetigen($"Akte, Berichte und Dokumente von {z.Bewohner.Name} werden endgültig gelöscht. Freigegeben von {z.Freigabe?.Kuerzel}. " +
                "Im Löschprotokoll stehen nur Nummer, Zeitraum, wer und wann. Wirklich löschen?"))
            return false;
        try { speicher.EndgueltigLoeschen(z.Bewohner.Id!.Value, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Endgültig löschen"); return false; }
        Aktualisieren();
        return true;
    }

    public void ProtokollZeigen()
    {
        var zeilen = speicher.Loeschprotokolle().Select(p => $"{p.Zeit:dd.MM.yyyy HH:mm} · Bewohner-Nr. {p.BewohnerNr} · {p.Zeitraum} · freigegeben {p.Freigabe}, gelöscht {p.Geloescht}");
        using var fenster = new TextFenster("Löschprotokoll", zeilen.Any() ? string.Join(Environment.NewLine, zeilen) : "Noch nichts gelöscht.");
        Dialog.Zeigen(fenster, FindForm());
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, ClientSize.Width - 2 * rand), y = rand;
        foreach (var c in new Control[] { leiste, Hinweis }.Where(c => c.Visible))
        {
            c.MaximumSize = new Size(breite, 0);
            c.Location = new Point(rand, y);
            y = c.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        Liste.SetBounds(rand, y, breite, Math.Max(Stil.Px(this, 120), ClientSize.Height - y - rand));
    }
}
