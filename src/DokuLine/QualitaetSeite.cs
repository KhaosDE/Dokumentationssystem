using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Bereich „Qualität“ (Bauplan 44, Recht „Qualität prüfen“): oben die Kennzahlen je Wohnbereich, darunter die Prüfliste –
// je Bewohner ein Haken oder der Mangel je Prüfpunkt, dazu Hinweis und Prüfvermerk. „Hinweis senden …“ legt eine Aufgabe
// für den Wohnbereich an, „Als geprüft vermerken“ den Prüfvermerk. Doppelklick öffnet die Akte (nur lesen, wer nichts ändern darf).
public sealed class QualitaetSeite : Panel, IBereichSeite
{
    public const string AlleBereiche = "alle Bereiche";
    string IBereichSeite.Titel => "Qualität";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [HinweisKnopf, GeprueftKnopf, VerlaufKnopf, NachweisKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren(filterBehalten);

    private readonly Speicher speicher;
    private bool fuellt;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    public ComboBox BereichWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, AccessibleName = "Bereich" };
    public CheckBox NurMaengel { get; } = new() { Text = "nur mit Mängeln", AutoSize = true, BackColor = Stil.Seite };
    public Button HinweisKnopf { get; } = Stil.HauptKnopf("Hinweis senden …");
    public Button GeprueftKnopf { get; } = Stil.NebenKnopf("Als geprüft vermerken");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Prüfverlauf …");
    public Button NachweisKnopf { get; } = Stil.NebenKnopf("Nachweis Ziele und Maßnahmen …");
    public Label Kennzahlen { get; } = new() { AutoSize = true, BackColor = Stil.Seite, ForeColor = Stil.TextNormal, UseMnemonic = false };
    public Label Hinweis { get; } = new()
    {
        Text = "Geprüft werden die Unterlagen der Bewohner, nicht einzelne Mitarbeiter.", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise,
        BackColor = Stil.Seite,
    };
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        ShowItemToolTips = true, AccessibleName = "Prüfliste", Font = Stil.Standard,
    };
    public List<PruefZeile> Zeilen { get; private set; } = [];
    public List<QmKennzahl> Zahlen { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // Zum Testen ersetzbar: welche Mängel in den Hinweis gehören (Indizes) und die Bemerkung zum Prüfvermerk.
    internal Func<IReadOnlyList<string>, List<int>?> MaengelWaehlen { get; set; }
    internal Func<string?> BemerkungFragen { get; set; }
    internal Action<Func<PrintDocument>, string> Vorschau { get; set; }
    public event Action? Geaendert;
    public event Action<long>? AkteGewuenscht;

    public QualitaetSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        MaengelWaehlen = texte => MehrfachWahl.Zeigen(FindForm(), "Hinweis an den Wohnbereich", "Welche Punkte sollen in den Hinweis? Er erscheint als Aufgabe des Bereichs.", texte);
        BemerkungFragen = () => Eingabe.Fragen(FindForm(), "Als geprüft vermerken", "Bemerkung (freiwillig)");
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(FindForm()!, bauen, titel);
        foreach (var c in new Control[] { BereichWahl, NurMaengel }) c.Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandKlein);
        leiste.Controls.AddRange([new Label { Text = "Bereich", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) }, BereichWahl, NurMaengel]);
        BereichWahl.SelectedIndexChanged += (_, _) => { if (!fuellt) Fuellen(); };
        NurMaengel.CheckedChanged += (_, _) => Fuellen();
        HinweisKnopf.Click += (_, _) => HinweisSenden();
        GeprueftKnopf.Click += (_, _) => Vermerken();
        VerlaufKnopf.Click += (_, _) => VerlaufZeigen();
        NachweisKnopf.Click += (_, _) => NachweisDrucken();
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Liste.DoubleClick += (_, _) => { if (Gewaehlt is PruefZeile z) AkteGewuenscht?.Invoke(z.Bewohner.Id!.Value); };
        Controls.AddRange([leiste, Kennzahlen, Hinweis, Liste]);
    }

    public PruefZeile? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as PruefZeile : null;

    private List<Bewohner> GewaehlteBewohner()
    {
        var alle = speicher.AlleBewohner();
        string bereich = BereichWahl.SelectedItem as string ?? AlleBereiche;
        return [.. (bereich == AlleBereiche ? alle : alle.Where(b => b.Wohnbereich == bereich)).OrderBy(b => b.Wohnbereich, StringComparer.Ordinal)
            .ThenBy(b => b.Zimmer.PadLeft(8), StringComparer.Ordinal)];
    }

    public void Aktualisieren(bool filterBehalten = false)
    {
        if (!filterBehalten || BereichWahl.Items.Count == 0)
        {
            var bereiche = speicher.Wohnbereiche().Select(w => w.Name).ToList();
            BereichWahl.Items.Clear();
            BereichWahl.Items.Add(AlleBereiche);
            BereichWahl.Items.AddRange([.. bereiche]);
            string gewaehlt = Arbeitsbereich.Gewaehlt;
            fuellt = true;
            BereichWahl.SelectedIndex = bereiche.Contains(gewaehlt) ? BereichWahl.Items.IndexOf(gewaehlt) : 0;
            fuellt = false;
        }
        Fuellen();
    }

    public void Fuellen()
    {
        Recht.Anwenden(speicher, Funktionen.Qualitaet, HinweisKnopf, GeprueftKnopf);
        var heute = DateOnly.FromDateTime(Jetzt());
        var bewohner = GewaehlteBewohner();
        Zahlen = speicher.QmKennzahlen(bewohner, heute);
        Kennzahlen.Text = Zahlen.Count == 0 ? "Keine Bewohner in diesem Bereich." : string.Join(Environment.NewLine, Zahlen.Select(QualitaetRegeln.KennzahlText));
        Zeilen = speicher.Pruefliste(bewohner, heute);
        var gezeigt = NurMaengel.Checked ? Zeilen.Where(z => z.Maengel).ToList() : Zeilen;
        var punkte = Zeilen.FirstOrDefault()?.Ergebnisse.Select(e => e.Punkt).ToList()
            ?? [.. QualitaetRegeln.Alle.Except(QualitaetRegeln.AusLesen(Einstellungen.Laden().QmAus))];
        Liste.BeginUpdate();
        Liste.Items.Clear();
        Liste.Columns.Clear();
        foreach (var (titel, breite) in new[] { ("Zimmer", 70), ("Name", 170), ("Bereich", 60) }.Concat(punkte.Select(p => (QualitaetRegeln.Kurz(p), 110)))
                     .Concat([("Hinweis", 220), ("Geprüft", 170)]))
            Liste.Columns.Add(titel, breite);
        foreach (var z in gezeigt)
        {
            var werte = new List<string> { z.Bewohner.Zimmer, z.Bewohner.Name, z.Bewohner.Wohnbereich };
            werte.AddRange(z.Ergebnisse.Select(e => e.Text));
            werte.Add(QualitaetRegeln.HinweisStand(z.Hinweis));
            werte.Add(QualitaetRegeln.VermerkText(z.Vermerk));
            var zeile = new ListViewItem([.. werte]) { Tag = z, UseItemStyleForSubItems = false, ToolTipText = z.Maengel ? string.Join(Environment.NewLine, z.MaengelTexte) : "ohne Mängel" };
            for (int i = 0; i < z.Ergebnisse.Count; i++)
                if (!z.Ergebnisse[i].Ok) zeile.SubItems[3 + i].ForeColor = Stil.Achtung.Text;
            if (z.Vermerk is null) zeile.SubItems[^1].ForeColor = Stil.TextLeise;
            Liste.Items.Add(zeile);
        }
        Liste.EndUpdate();
        int maengel = Zeilen.Count(z => z.Maengel);
        KopfText = $"{BereichWahl.SelectedItem} · {Zeilen.Count} Bewohner · {maengel} mit Mängeln";
        KnoepfeSetzen();
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void KnoepfeSetzen()
    {
        bool recht = Recht.Grund(speicher, Funktionen.Qualitaet) is null;
        if (!recht) return;
        string? ohne = Gewaehlt is null ? "Bitte zuerst einen Bewohner wählen." : null;
        Stil.Sperren(HinweisKnopf, ohne ?? (Gewaehlt!.Maengel ? null : "Bei diesem Bewohner fehlt nichts."));
        Stil.Sperren(GeprueftKnopf, ohne);
        Stil.Sperren(VerlaufKnopf, ohne);
    }

    public bool HinweisSenden()
    {
        if (Gewaehlt is not PruefZeile z || Stil.Sperrgrund(HinweisKnopf) is not null) return false;
        var maengel = z.MaengelTexte;
        if (MaengelWaehlen(maengel) is not { Count: > 0 } auswahl) return false;
        try { speicher.HinweisSenden(z.Bewohner.Id!.Value, QualitaetRegeln.HinweisText(z.Bewohner, auswahl.Select(i => maengel[i])), Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Hinweis senden", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        Fuellen();
        return true;
    }

    public bool Vermerken()
    {
        if (Gewaehlt is not PruefZeile z || Stil.Sperrgrund(GeprueftKnopf) is not null || BemerkungFragen() is not string bemerkung) return false;
        try { speicher.PruefvermerkSetzen(z.Bewohner.Id!.Value, bemerkung, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Als geprüft vermerken", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        Fuellen();
        return true;
    }

    public void VerlaufZeigen()
    {
        if (Gewaehlt is not PruefZeile z) return;
        long id = z.Bewohner.Id!.Value;
        var zeilen = speicher.Pruefvermerke(id).Select(v => (v.Zeit, $"{v.Zeit:dd.MM.yyyy HH:mm} geprüft von {v.Kuerzel}" + (v.Bemerkung.Length > 0 ? $": {v.Bemerkung}" : "")))
            .Concat(speicher.QmHinweise(id).Select(h => (h.Gesendet, $"{h.Gesendet:dd.MM.yyyy HH:mm} Hinweis von {h.Kuerzel}: {h.Text}" +
                (h.Erledigt is DateTime e ? $" (erledigt am {e:dd.MM.yyyy})" : " (offen)"))))
            .OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
        using var fenster = new TextFenster($"Prüfverlauf – {z.Bewohner.Name}", zeilen.Count == 0 ? "Noch nicht geprüft." : string.Join(Environment.NewLine, zeilen));
        Dialog.Zeigen(fenster, FindForm());
    }

    // Für die Heimaufsicht: je Ziel eine Zeile mit Maßnahmen und letzter Überprüfung (der gewählte Bereich).
    public bool NachweisDrucken()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        var zeilen = QualitaetRegeln.Nachweis(speicher.PruefDatenLesen(GewaehlteBewohner(), heute), heute);
        var druck = new TabellenDruck("Nachweis Ziele und Maßnahmen", $"{BereichWahl.SelectedItem} · Stand {Datum.Text(heute)}",
            QualitaetRegeln.NachweisSpalten, [0.6f, 1.6f, 3.2f, 1.6f, 0.9f, 0.9f, 0.9f, 3f], zeilen);
        Vorschau(druck.DokumentBauen, "Nachweis Ziele und Maßnahmen");
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, ClientSize.Width - 2 * rand), y = rand;
        foreach (var c in new Control[] { leiste, Kennzahlen, Hinweis })
        {
            c.MaximumSize = new Size(breite, 0);
            c.Location = new Point(rand, y);
            y = c.Bottom + Stil.Px(this, Stil.AbstandKlein);
        }
        Liste.SetBounds(rand, y, breite, Math.Max(Stil.Px(this, 120), ClientSize.Height - y - rand));
    }
}
