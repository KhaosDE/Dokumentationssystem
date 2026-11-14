using DokuLine.Kern;

namespace DokuLine;

// Die fälligen Maßnahmen eines Dienstes zum Abhaken (Bauplan 39, in der Übersicht ab 40): offene oben nach Uhrzeit.
// „durchgeführt“ mit einem Klick (Zeit = geplanter Zeitwert); „abgelehnt …“, „nicht möglich …“ und „mit Zeit …“ öffnen
// das kleine Fenster für Grund, Zeit und Bemerkung. Kürzel und Uhrzeit setzt das Programm.
public sealed class MassnahmenListe : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite, Margin = new Padding(0) };
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        ShowItemToolTips = true, AccessibleName = "Fällige Maßnahmen",
    };
    public Button DurchgefuehrtKnopf { get; } = Stil.HauptKnopf("durchgeführt");
    public Button AbgelehntKnopf { get; } = Stil.NebenKnopf("abgelehnt …");
    public Button NichtMoeglichKnopf { get; } = Stil.NebenKnopf("nicht möglich …");
    public Button MitZeitKnopf { get; } = Stil.NebenKnopf("mit Zeit und Bemerkung …");
    public List<MassnahmeTermin> Termine { get; private set; } = [];
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    public event Action? Geaendert;

    public MassnahmenListe(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var (titel, breite) in new[] { ("Wann", 110), ("Zimmer", 70), ("Bewohner", 190), ("Maßnahme", 240), ("Zeit", 70), ("Stand", 220) })
            Liste.Columns.Add(titel, breite);
        foreach (var k in new[] { DurchgefuehrtKnopf, AbgelehntKnopf, NichtMoeglichKnopf, MitZeitKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            knoepfe.Controls.Add(k);
        }
        DurchgefuehrtKnopf.Click += (_, _) => Durchgefuehrt();
        AbgelehntKnopf.Click += (_, _) => MitFenster(MassnahmeErgebnis.Abgelehnt);
        NichtMoeglichKnopf.Click += (_, _) => MitFenster(MassnahmeErgebnis.NichtMoeglich);
        MitZeitKnopf.Click += (_, _) => MitFenster(MassnahmeErgebnis.Durchgefuehrt);
        Liste.DoubleClick += (_, _) => MitFenster(MassnahmeErgebnis.Durchgefuehrt);
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Controls.AddRange([knoepfe, Liste]);
    }

    private readonly Dictionary<long, Bewohner> bewohner = [];

    public MassnahmeTermin? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as MassnahmeTermin : null;

    public void Fuellen(IReadOnlyList<MassnahmeTermin> termine, IEnumerable<Bewohner> alle)
    {
        bewohner.Clear();
        foreach (var b in alle) bewohner[b.Id!.Value] = b;
        Termine = [.. termine];
        var jetzt = Jetzt();
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var t in Termine)
        {
            var b = bewohner.GetValueOrDefault(t.Massnahme.BewohnerId);
            bool ueberfaellig = t.Offen && t.Uhrzeit is DateTime u && u < jetzt;
            string stand = t.Bestaetigung is MassnahmeBestaetigung x
                ? $"{MassnahmenRegeln.ErgebnisText(x.Ergebnis)} · {x.Kuerzel} {x.Zeit:HH:mm}" : ueberfaellig ? "offen · überfällig" : "offen";
            var zeile = new ListViewItem([t.Fassung.Uhrzeit is TimeOnly z ? $"{z:HH\\:mm} Uhr" : "im Dienst", b?.Zimmer ?? "", b?.Name ?? "", t.Fassung.Titel,
                MassnahmenRegeln.Dauer(t.Fassung.Minuten), stand])
            { Tag = t, ToolTipText = t.Fassung.Beschreibung, UseItemStyleForSubItems = false };
            if (!t.Offen) foreach (ListViewItem.ListViewSubItem s in zeile.SubItems) s.ForeColor = Stil.TextLeise;
            else if (ueberfaellig) { zeile.SubItems[5].ForeColor = Stil.Achtung.Text; zeile.SubItems[5].BackColor = Stil.Achtung.Flaeche; }
            Liste.Items.Add(zeile);
        }
        Liste.EndUpdate();
        KnoepfeSetzen();
        PerformLayout();
    }

    private void KnoepfeSetzen()
    {
        var alle = new[] { DurchgefuehrtKnopf, AbgelehntKnopf, NichtMoeglichKnopf, MitZeitKnopf };
        Recht.Anwenden(speicher, Funktionen.MassnahmenBestaetigen, alle);
        if (Stil.Sperrgrund(DurchgefuehrtKnopf) is not null) return;
        string? ohne = Gewaehlt is null ? "Bitte zuerst eine Maßnahme in der Liste wählen." : !Gewaehlt.Offen ? "Diese Maßnahme ist schon bestätigt." : null;
        foreach (var k in alle) Stil.Sperren(k, ohne);
    }

    // Ein Klick: durchgeführt, mit dem geplanten Zeitwert.
    public bool Durchgefuehrt()
    {
        if (Gewaehlt is not MassnahmeTermin t || Stil.Sperrgrund(DurchgefuehrtKnopf) is not null) return false;
        try { speicher.MassnahmeBestaetigen(t.Massnahme.Id, t.Dienst, MassnahmeErgebnis.Durchgefuehrt, "", t.Fassung.Minuten, "", Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Maßnahme bestätigen", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Geaendert?.Invoke();
            return false;
        }
        Geaendert?.Invoke();
        return true;
    }

    public bool MitFenster(MassnahmeErgebnis vorwahl)
    {
        if (Gewaehlt is not MassnahmeTermin t || Stil.Sperrgrund(MitZeitKnopf) is not null) return false;
        string name = bewohner.GetValueOrDefault(t.Massnahme.BewohnerId)?.Name ?? "";
        using var fenster = new MassnahmeBestaetigenFenster(speicher, t.Massnahme, t.Dienst, name, vorwahl) { Jetzt = Jetzt };
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return false;
        Geaendert?.Invoke();
        return true;
    }

    // Höhe für die Anordnung der Seite: Knöpfe und alle Zeilen ohne Bildlauf (höchstens 14).
    public int Hoehe(int breite)
    {
        knoepfe.MaximumSize = new Size(breite, 0);
        return knoepfe.GetPreferredSize(new Size(breite, 0)).Height + Stil.Px(this, Stil.AbstandMini) + ListenHoehe;
    }

    private int ListenHoehe => Stil.Px(this, 26) + Math.Clamp(Liste.Items.Count, 1, 14) * Stil.Px(this, 20);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = ClientSize.Width;
        knoepfe.MaximumSize = new Size(breite, 0);
        knoepfe.Bounds = new Rectangle(Point.Empty, knoepfe.GetPreferredSize(new Size(breite, 0)));
        Liste.Bounds = new Rectangle(0, knoepfe.Bottom + Stil.Px(this, Stil.AbstandMini), breite, ListenHoehe);
    }
}
