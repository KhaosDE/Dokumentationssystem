using DokuLine.Kern;

namespace DokuLine;

// Akte, Reiter „Dokumente“: links die Kategorien mit Zähler (dazu „Alle“ und „Papierkorb“), rechts die Dokumente
// (neueste oben) mit Suchfeld über alle Kategorien. Öffnen im Standardprogramm des Rechners (Regel 6: Übergabe, keine
// Webansicht). Dateien lassen sich in die Liste ziehen; sie werden kopiert, das Original bleibt.
public sealed class DokumenteAnsicht : KartenFlaeche
{
    public const string Alle = "Alle", Papierkorb = "Papierkorb";
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche };
    private List<Dokument> alle = [];

    public ListBox Kategorien { get; } = new() { IntegralHeight = false, Font = Stil.Standard, BorderStyle = BorderStyle.None, BackColor = Stil.Flaeche };
    public TextBox Suche { get; } = new() { PlaceholderText = "In allen Dokumenten suchen", AccessibleName = "Dokumente suchen" };
    public ListView Liste { get; } = new() { View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, AllowDrop = true, Font = Stil.Standard };
    public List<Dokument> Zeilen { get; private set; } = [];
    public Button HinzufuegenKnopf { get; } = Stil.HauptKnopf("Dokument hinzufügen …");
    public Button OeffnenKnopf { get; } = Stil.NebenKnopf("Öffnen");
    public Button OrdnerKnopf { get; } = Stil.NebenKnopf("Im Ordner zeigen");
    public Button ErsetzenKnopf { get; } = Stil.NebenKnopf("Ersetzen …");
    public Button EntfernenKnopf { get; } = Stil.NebenKnopf("In den Papierkorb …");
    public Button WiederherstellenKnopf { get; } = Stil.NebenKnopf("Wiederherstellen");
    public Button LoeschenKnopf { get; } = Stil.NebenKnopf("Endgültig löschen …");
    public Button ProtokollKnopf { get; } = Stil.NebenKnopf("Protokoll …");
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Flaeche, Visible = false };
    public Bewohner? Bewohner { get; private set; }
    public Dokument? Gewaehlt => Liste.SelectedIndices.Count == 1 ? Zeilen[Liste.SelectedIndices[0]] : null;
    public string Kategorie => Kategorien.SelectedItem is string s ? s[..(s.LastIndexOf(" (", StringComparison.Ordinal) is int i and > 0 ? i : s.Length)] : Alle;

    // Zum Testen ersetzbar: Dateien wählen, Grund fragen, Angaben zum Dokument (Dialog).
    internal Func<string[]?> DateienWaehlen { get; set; }
    internal Func<string, string?> GrundFragen { get; set; }
    internal Func<DokumentFenster, bool> AngabenZeigen { get; set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public event Action? Geaendert;

    public DokumenteAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        DateienWaehlen = () =>
        {
            using var dialog = new OpenFileDialog { Title = "Dokument hinzufügen", Multiselect = true };
            return dialog.ShowDialog(FindForm()) == DialogResult.OK ? dialog.FileNames : null;
        };
        GrundFragen = frage => Eingabe.Fragen(FindForm(), "Grund", frage);
        AngabenZeigen = f => Dialog.Zeigen(f, FindForm()) == DialogResult.OK;
        foreach (var (titel, breite) in new[] { ("Titel", 220), ("Kategorie", 170), ("Datum", 90), ("gültig bis", 110), ("hinzugefügt", 130) })
            Liste.Columns.Add(titel, breite);
        Liste.AccessibleName = "Dokumente";
        Kategorien.AccessibleName = "Kategorien";
        Kategorien.SelectedIndexChanged += (_, _) => ListeFuellen();
        Suche.TextChanged += (_, _) => ListeFuellen();
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Liste.DoubleClick += (_, _) => Oeffnen();
        Liste.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; Oeffnen(); } };
        Liste.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true && Stil.Sperrgrund(HinzufuegenKnopf) is null
            ? DragDropEffects.Copy : DragDropEffects.None;
        Liste.DragDrop += (_, e) => { if (e.Data?.GetData(DataFormats.FileDrop) is string[] dateien) Hinzufuegen(dateien); };
        HinzufuegenKnopf.Click += (_, _) => { if (DateienWaehlen() is string[] d) Hinzufuegen(d); };
        OeffnenKnopf.Click += (_, _) => Oeffnen();
        OrdnerKnopf.Click += (_, _) => OrdnerZeigen();
        ErsetzenKnopf.Click += (_, _) => Ersetzen();
        EntfernenKnopf.Click += (_, _) => Entfernen();
        WiederherstellenKnopf.Click += (_, _) => Ausfuehren(() => speicher.DokumentWiederherstellen(Gewaehlt!.Id, Recht.Kuerzel, Jetzt()));
        LoeschenKnopf.Click += (_, _) => Loeschen();
        ProtokollKnopf.Click += (_, _) => ProtokollZeigen();
        foreach (var k in new[] { HinzufuegenKnopf, OeffnenKnopf, OrdnerKnopf, ErsetzenKnopf, EntfernenKnopf, WiederherstellenKnopf, LoeschenKnopf, ProtokollKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            knoepfe.Controls.Add(k);
        }
        Controls.AddRange([Kategorien, Suche, Liste, knoepfe, Fehler]);
    }

    public void Zeigen(Bewohner b, string? kategorie = null)
    {
        Bewohner = b;
        Fehler.Visible = false;
        Neu(kategorie);
    }

    private void Neu(string? kategorie = null)
    {
        if (Bewohner?.Id is not long id) return;
        string wahl = kategorie ?? Kategorie;
        alle = speicher.Dokumente(id, auchEntfernte: true);
        var namen = speicher.Dokumentkategorien().Concat(alle.Select(d => d.Kategorie)).Distinct().ToList();   // auch umbenannte
        Kategorien.BeginUpdate();
        Kategorien.Items.Clear();
        Kategorien.Items.Add($"{Alle} ({alle.Count(d => !d.Entfernt)})");
        foreach (var n in namen) Kategorien.Items.Add($"{n} ({alle.Count(d => !d.Entfernt && d.Kategorie == n)})");
        Kategorien.Items.Add($"{Papierkorb} ({alle.Count(d => d.Entfernt)})");
        Kategorien.EndUpdate();
        int index = Kategorien.Items.Cast<string>().ToList().FindIndex(x => x.StartsWith(wahl + " (", StringComparison.Ordinal));
        Kategorien.SelectedIndex = Math.Max(0, index);   // füllt die Liste
        ListeFuellen();
    }

    private void ListeFuellen()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        string suche = Suche.Text.Trim(), kategorie = Kategorie;
        Zeilen = [.. alle.Where(d => kategorie == Papierkorb ? d.Entfernt : !d.Entfernt && (kategorie == Alle || suche.Length > 0 || d.Kategorie == kategorie))
            .Where(d => suche.Length == 0 || $"{d.Titel} {d.Hinweis} {d.Original}".Contains(suche, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(d => d.Datum).ThenByDescending(d => d.Id)];
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var d in Zeilen)
            Liste.Items.Add(new ListViewItem([d.Titel, d.Kategorie, Datum.Text(d.Datum),
                d.GueltigBis is DateOnly b ? Datum.Text(b) + (d.Abgelaufen(heute) ? " – abgelaufen" : "") : "–", $"{d.Kuerzel}, {d.Hinzugefuegt:dd.MM.yyyy}"])
            { ForeColor = d.Abgelaufen(heute) ? Stil.Beschluss.Text : Stil.TextNormal, ToolTipText = d.Hinweis });
        Liste.EndUpdate();
        KnoepfeSetzen();
    }

    private void KnoepfeSetzen()
    {
        var d = Gewaehlt;
        string? ohne = d is null ? "Bitte zuerst ein Dokument wählen." : null;
        string? ansehen = Recht.Grund(speicher, Funktionen.DokumenteAnsehen), hinzu = Recht.Grund(speicher, Funktionen.DokumenteHinzufuegen);
        Stil.Sperren(HinzufuegenKnopf, hinzu);
        Stil.Sperren(OeffnenKnopf, ohne ?? ansehen);
        Stil.Sperren(OrdnerKnopf, ohne ?? ansehen);
        Stil.Sperren(ErsetzenKnopf, ohne ?? hinzu ?? (d!.Entfernt ? "Liegt im Papierkorb." : null));
        Stil.Sperren(EntfernenKnopf, ohne ?? hinzu ?? (d!.Entfernt ? "Liegt schon im Papierkorb." : null));
        Stil.Sperren(WiederherstellenKnopf, ohne ?? hinzu ?? (!d!.Entfernt ? "Liegt nicht im Papierkorb." : null));
        Stil.Sperren(LoeschenKnopf, ohne ?? Recht.Grund(speicher, Funktionen.DokumenteLoeschen) ?? (!d!.Entfernt ? "Erst in den Papierkorb legen." : null));
        WiederherstellenKnopf.Visible = LoeschenKnopf.Visible = Kategorie == Papierkorb;
    }

    private bool Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            PerformLayout();
            return false;
        }
        catch (IOException e)
        {
            Fehler.Text = $"Die Datei ließ sich nicht verarbeiten: {e.Message}";
            Fehler.Visible = true;
            PerformLayout();
            return false;
        }
        Fehler.Visible = false;
        Neu();
        Geaendert?.Invoke();
        return true;
    }

    public int Hinzufuegen(IEnumerable<string> dateien)
    {
        if (Bewohner is null || Stil.Sperrgrund(HinzufuegenKnopf) is not null) return 0;
        int anzahl = 0;
        foreach (var datei in dateien.Where(File.Exists))
        {
            using var fenster = new DokumentFenster(speicher, Bewohner, datei, null, Kategorie is Alle or Papierkorb ? null : Kategorie) { Jetzt = Jetzt };
            if (AngabenZeigen(fenster) && fenster.Gespeichert is not null) anzahl++;
        }
        if (anzahl > 0) { Neu(); Geaendert?.Invoke(); }
        return anzahl;
    }

    public string? Oeffnen()
    {
        if (Gewaehlt is not Dokument d || Stil.Sperrgrund(OeffnenKnopf) is not null) return null;
        string? pfad = null;
        if (!Ausfuehren(() => pfad = speicher.DokumentOeffnen(d.Id, Recht.Kuerzel, Jetzt()))) return null;
        Extern.Oeffnen(pfad!);
        return pfad;
    }

    public void OrdnerZeigen()
    {
        if (Bewohner?.Id is long id && Stil.Sperrgrund(OrdnerKnopf) is null) Extern.Oeffnen(speicher.Ablage(id));
    }

    public void Ersetzen()
    {
        if (Bewohner is null || Gewaehlt is not Dokument d || Stil.Sperrgrund(ErsetzenKnopf) is not null || DateienWaehlen() is not [string datei, ..]) return;
        using var fenster = new DokumentFenster(speicher, Bewohner, datei, d, d.Kategorie) { Jetzt = Jetzt };
        if (AngabenZeigen(fenster)) { Neu(); Geaendert?.Invoke(); }
    }

    public bool Entfernen()
    {
        if (Gewaehlt is not Dokument d || Stil.Sperrgrund(EntfernenKnopf) is not null || GrundFragen($"Warum kommt „{d.Titel}“ in den Papierkorb?") is not string grund) return false;
        return Ausfuehren(() => speicher.DokumentEntfernen(d.Id, grund, Recht.Kuerzel, Jetzt()));
    }

    public bool Loeschen()
    {
        if (Gewaehlt is not Dokument d || Stil.Sperrgrund(LoeschenKnopf) is not null
            || GrundFragen($"„{d.Titel}“ endgültig löschen? Die Datei ist danach weg. Grund:") is not string grund) return false;
        return Ausfuehren(() => speicher.DokumentEndgueltigLoeschen(d.Id, grund, Recht.Kuerzel, Jetzt()));
    }

    public void ProtokollZeigen()
    {
        if (Bewohner?.Id is not long id) return;
        var zeilen = speicher.DokumentProtokoll(id).Select(e => $"{e.Zeit:dd.MM.yyyy HH:mm} · {e.Kuerzel} · {e.Titel}: {DokumentRegeln.AktionText(e.Aktion)}" +
                                                                 (e.Grund.Length > 0 ? $" ({e.Grund})" : ""));
        using var fenster = new TextFenster($"Protokoll Dokumente: {Bewohner.Name}", string.Join(Environment.NewLine, zeilen));
        Dialog.Zeigen(fenster, FindForm());
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), abstand = Stil.Px(this, Stil.AbstandMittel);
        int links = Stil.Px(this, 230), breite = Math.Max(200, Width - 2 * innen);
        knoepfe.MaximumSize = new Size(breite, 0);
        knoepfe.Bounds = new Rectangle(new Point(innen, innen), knoepfe.GetPreferredSize(new Size(breite, 0)));
        int y = knoepfe.Bottom + Stil.Px(this, Stil.AbstandKlein);
        if (Fehler.Text.Length > 0 && Fehler.Visible) { Fehler.Location = new Point(innen, y); y = Fehler.Bottom + Stil.Px(this, Stil.AbstandKlein); }
        int hoehe = Stil.Px(this, 360);
        Kategorien.SetBounds(innen, y, links, hoehe);
        int rechts = innen + links + abstand, rechtsBreite = Math.Max(150, breite - links - abstand);
        Suche.SetBounds(rechts, y, rechtsBreite, Suche.Height);
        Liste.SetBounds(rechts, Suche.Bottom + Stil.Px(this, Stil.AbstandKlein), rechtsBreite, hoehe - Suche.Height - Stil.Px(this, Stil.AbstandKlein));
        int gesamt = y + hoehe + innen;
        if (Height != gesamt) Height = gesamt;
    }
}

// Angaben zu einem Dokument beim Hinzufügen oder Ersetzen: Kategorie (nach Dateiname vorgeschlagen), Titel, Datum, gültig bis.
public sealed class DokumentFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly string datei;
    private readonly Dokument? alt;

    public ComboBox KategorieWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    public TextBox Titel { get; } = new() { Width = 320 };
    public TextBox DatumFeld { get; } = new DatumFeld() { Width = 100 };
    public TextBox GueltigBis { get; } = new DatumFeld() { Width = 100, PlaceholderText = "freiwillig" };
    public TextBox Hinweis { get; } = new() { Width = 320 };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Dokument? Gespeichert { get; private set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public DokumentFenster(Speicher speicher, Bewohner bewohner, string datei, Dokument? alt, string? kategorie)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        this.datei = datei;
        this.alt = alt;
        Fenster.Grundform(this, alt is null ? $"Dokument hinzufügen: {bewohner.Name}" : $"Dokument ersetzen: {alt.Titel}");
        var kategorien = speicher.Dokumentkategorien();
        KategorieWahl.Items.AddRange([.. kategorien]);
        KategorieWahl.SelectedItem = kategorie is not null && kategorien.Contains(kategorie) ? kategorie : DokumentRegeln.KategorieVorschlag(datei, kategorien);
        Titel.Text = alt?.Titel ?? DokumentRegeln.TitelVorschlag(datei);
        DatumFeld.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Today));
        GueltigBis.Text = alt?.GueltigBis is DateOnly b ? Datum.Text(b) : "";
        Hinweis.Text = alt?.Hinweis ?? "";
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Datei", new Label { Text = Path.GetFileName(datei), AutoSize = true, UseMnemonic = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) });
        zeile("Kategorie", KategorieWahl);
        zeile("Titel", Titel);
        zeile("Datum des Dokuments", DatumFeld);
        zeile("Gültig bis", GueltigBis);
        zeile("Hinweis", Hinweis);
        zeile(null, Fenster.Hinweis("Die Datei wird in die Ablage von DokuLine kopiert; das Original bleibt, wo es ist. Demo: nur ausgedachte Dokumente."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, alt is null ? "Hinzufügen" : "Ersetzen", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            DateOnly d;
            DateOnly? bis = null;
            try
            {
                d = Datum.Lesen(DatumFeld.Text, "Datum des Dokuments");
                if (GueltigBis.Text.Trim().Length > 0) bis = Datum.Lesen(GueltigBis.Text, "Gültig bis");
            }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            var e = new DokumentEingabe { Kategorie = KategorieWahl.SelectedItem as string ?? "", Titel = Titel.Text, Datum = d, GueltigBis = bis, Hinweis = Hinweis.Text };
            Gespeichert = alt is null
                ? speicher.DokumentHinzufuegen(bewohner.Id!.Value, datei, e, Recht.Kuerzel, Jetzt())
                : speicher.DokumentErsetzen(alt.Id, datei, e, Recht.Kuerzel, Jetzt());
        }
        catch (Eingabefehler x)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", x.Fehler);
            Fehler.Visible = true;
            return false;
        }
        catch (IOException x)
        {
            Fehler.Text = $"Die Datei ließ sich nicht kopieren: {x.Message}";
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
