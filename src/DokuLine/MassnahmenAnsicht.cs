using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Akte, Reiter „Maßnahmen“ (Bauplan 39): oben die fünf Lebensbereiche (Berichte im Quartal, letzter Bericht, nächste
// Reflexion, „seit n Wochen kein Bericht“; Doppelklick öffnet die Dokumentation mit diesem Filter), darunter die
// Maßnahmen des Bewohners, unten lesend, was im Medikamentenplan steht (dort wird es geführt, nicht hier).
public sealed class MassnahmenAnsicht : KartenFlaeche
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel kopf = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neue Maßnahme …");
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Ändern …");
    public Button BeendenKnopf { get; } = Stil.NebenKnopf("Beenden …");
    public Button BestaetigenKnopf { get; } = Stil.NebenKnopf("Durchführung bestätigen …");
    public CheckBox BeendeteZeigen { get; } = new() { Text = "beendete zeigen", AutoSize = true, BackColor = Stil.Flaeche };
    public ListView Lebensbereiche { get; } = Tabelle(("Lebensbereich", 330), ("Berichte im Quartal", 130), ("Letzter Bericht", 120), ("Reflexion bis", 110), ("Hinweis", 220));
    public ListView Liste { get; } = Tabelle(("Maßnahme", 220), ("Lebensbereich", 150), ("Art", 100), ("Rhythmus", 120), ("Wann", 110), ("Zeit", 70),
        ("Ziel", 110), ("Zuletzt", 170), ("Stand", 160));
    public Label MedikamentenplanText { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Bewohner? Bewohner { get; private set; }
    public List<Massnahme> Gezeigt { get; private set; } = [];
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    internal Func<string, string?> GrundFragen { get; set; }
    public event Action? Geaendert;
    public event Action<int>? LebensbereichGewuenscht;

    public MassnahmenAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        GrundFragen = titel => Eingabe.Fragen(FindForm(), "Maßnahme beenden", $"Warum endet „{titel}“? (z. B. Ziel erreicht, nicht mehr nötig)");
        foreach (var k in new Control[] { NeuKnopf, AendernKnopf, BeendenKnopf, BestaetigenKnopf, BeendeteZeigen }) k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        kopf.Controls.AddRange([NeuKnopf, AendernKnopf, BestaetigenKnopf, BeendenKnopf, BeendeteZeigen]);
        Lebensbereiche.AccessibleName = "Lebensbereiche";
        Liste.AccessibleName = "Maßnahmen";
        NeuKnopf.Click += (_, _) => Neu();
        AendernKnopf.Click += (_, _) => Aendern();
        BeendenKnopf.Click += (_, _) => Beenden();
        BestaetigenKnopf.Click += (_, _) => Bestaetigen();
        BeendeteZeigen.CheckedChanged += (_, _) => { if (Bewohner is not null) Zeigen(Bewohner); };
        Liste.DoubleClick += (_, _) => Aendern();
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Lebensbereiche.DoubleClick += (_, _) => { if (Lebensbereiche.SelectedItems.Count == 1) LebensbereichOeffnen(Lebensbereiche.SelectedIndices[0] + 1); };
        Controls.AddRange([kopf, Lebensbereiche, Liste, MedikamentenplanText]);
    }

    private static ListView Tabelle(params (string Titel, int Breite)[] spalten)
    {
        var liste = new ListView { View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable, ShowItemToolTips = true };
        foreach (var (titel, breite) in spalten) liste.Columns.Add(titel, breite);
        return liste;
    }

    public void LebensbereichOeffnen(int nr) => LebensbereichGewuenscht?.Invoke(nr);

    public Massnahme? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as Massnahme : null;

    public void Zeigen(Bewohner b)
    {
        Bewohner = b;
        long id = b.Id!.Value;
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        int wochen = Einstellungen.Laden().KeinBerichtWochen;
        Lebensbereiche.BeginUpdate();
        Lebensbereiche.Items.Clear();
        foreach (var lb in MassnahmenRegeln.Lebensbereiche(speicher.Berichte(id), jetzt, wochen))
        {
            var zeile = new ListViewItem([$"{lb.Nr} {BerichtRegeln.Lebensbereiche[lb.Nr - 1]}", lb.BerichteImQuartal.ToString(CultureInfo.InvariantCulture),
                lb.LetzterBericht is DateTime l ? Datum.Text(DateOnly.FromDateTime(l)) : "–", Datum.Text(lb.NaechsteReflexion), lb.Marke?.Text ?? ""])
            { ToolTipText = "Doppelklick: Berichte dieses Lebensbereichs in der Dokumentation" };
            if (lb.Marke is not null) zeile.SubItems[4].ForeColor = Stil.Achtung.Text;
            zeile.UseItemStyleForSubItems = false;
            Lebensbereiche.Items.Add(zeile);
        }
        Lebensbereiche.EndUpdate();

        var bestaetigungen = speicher.MassnahmeBestaetigungen(id);
        Gezeigt = speicher.Massnahmen(id, auchBeendete: BeendeteZeigen.Checked);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var m in Gezeigt.OrderBy(m => m.BeendetAm is null ? 0 : 1).ThenBy(m => m.Aktuell(heute).Lebensbereich).ThenBy(m => m.Aktuell(heute).Titel))
        {
            var f = m.Aktuell(heute);
            var letzte = bestaetigungen.Where(x => x.MassnahmeId == m.Id).MaxBy(x => x.Zeit);
            string stand = m.BeendetAm is DateOnly e ? $"beendet ab {Datum.Text(e)} ({m.BeendetGrund})"
                : f.GiltAb > heute ? $"ab {Datum.Text(f.GiltAb)}" : m.Fassungen.Count > 1 ? $"geändert, seit {Datum.Text(m.Am(heute)?.GiltAb)}" : $"seit {Datum.Text(m.Von)}";
            var zeile = new ListViewItem([f.Titel, BerichtRegeln.BereichText(f.Lebensbereich), f.Art, MassnahmenRegeln.RhythmusText(f), MassnahmenRegeln.WannText(f),
                MassnahmenRegeln.Dauer(f.Minuten), f.ZielId is null ? MassnahmenRegeln.OhneZiel : "", letzte is null ? "–"
                    : $"{letzte.Zeit:dd.MM. HH:mm} {MassnahmenRegeln.ErgebnisText(letzte.Ergebnis)} ({letzte.Kuerzel})", stand])
            { Tag = m, ToolTipText = f.Beschreibung };
            if (m.BeendetAm is not null) zeile.ForeColor = Stil.TextLeise;
            Liste.Items.Add(zeile);
        }
        Liste.EndUpdate();

        var plan = speicher.Verordnungen(id).Where(v => !v.Bedarf).Select(v => MedikationRegeln.Stand(v, heute)).Where(s => s.Sichtbar).ToList();
        MedikamentenplanText.Text = "Aus dem Medikamentenplan (nur lesend; geführt wird er im Reiter „Medikamentenplan“): " +
            (plan.Count == 0 ? "nichts Regelmäßiges eingetragen." : string.Join(" · ", plan.Select(s => $"{s.Verordnung.Medikament} {s.Verordnung.Staerke} {s.Dosis.Text}")));
        KnoepfeSetzen();
        PerformLayout();
    }

    private void KnoepfeSetzen()
    {
        if (Bewohner?.Id is not long id) return;
        string? anlegen = speicher.MassnahmenAnlegenGrund(Recht.Kuerzel, id);
        foreach (var k in new[] { NeuKnopf, AendernKnopf, BeendenKnopf }) Stil.Sperren(k, anlegen);
        Recht.Anwenden(speicher, Funktionen.MassnahmenBestaetigen, BestaetigenKnopf);
        string? ohne = Gewaehlt is null ? "Bitte zuerst eine Maßnahme wählen." : Gewaehlt.BeendetAm is not null ? "Die Maßnahme ist beendet." : null;
        if (anlegen is null) { Stil.Sperren(AendernKnopf, ohne); Stil.Sperren(BeendenKnopf, ohne); }
        if (Stil.Sperrgrund(BestaetigenKnopf) is null) Stil.Sperren(BestaetigenKnopf, ohne);
    }

    public bool Neu(string titel = "")
    {
        if (Bewohner is null || Stil.Sperrgrund(NeuKnopf) is not null) return false;
        using var fenster = new MassnahmeFenster(speicher, Bewohner, null, titel) { Jetzt = Jetzt };
        return Fertig(Dialog.Zeigen(fenster, FindForm()));
    }

    public bool Aendern()
    {
        if (Bewohner is null || Gewaehlt is not Massnahme m || Stil.Sperrgrund(AendernKnopf) is not null) return false;
        using var fenster = new MassnahmeFenster(speicher, Bewohner, m) { Jetzt = Jetzt };
        return Fertig(Dialog.Zeigen(fenster, FindForm()));
    }

    public bool Beenden()
    {
        if (Gewaehlt is not Massnahme m || Stil.Sperrgrund(BeendenKnopf) is not null) return false;
        if (GrundFragen(m.Aktuell(DateOnly.FromDateTime(Jetzt())).Titel) is not string grund) return false;
        try { speicher.MassnahmeBeenden(m.Id, grund, Recht.Kuerzel, DateOnly.FromDateTime(Jetzt())); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Maßnahme beenden", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return Fertig(DialogResult.OK);
    }

    // Bestätigen aus der Akte: im laufenden Dienst (bei Bedarf jederzeit).
    public bool Bestaetigen()
    {
        if (Bewohner is null || Gewaehlt is not Massnahme m || Stil.Sperrgrund(BestaetigenKnopf) is not null) return false;
        using var fenster = new MassnahmeBestaetigenFenster(speicher, m, SchichtTermin.Zu(Jetzt()), Bewohner.Name) { Jetzt = Jetzt };
        return Fertig(Dialog.Zeigen(fenster, FindForm()));
    }

    private bool Fertig(DialogResult ergebnis)
    {
        if (ergebnis != DialogResult.OK || Bewohner is null) return false;
        Zeigen(Bewohner);
        Geaendert?.Invoke();
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), luecke = Stil.Px(this, Stil.AbstandKlein);
        kopf.MaximumSize = new Size(breite, 0);
        kopf.Bounds = new Rectangle(new Point(innen, innen), kopf.GetPreferredSize(new Size(breite, 0)));
        int zeile = Stil.Px(this, 20), kopfzeile = Stil.Px(this, 26);
        Lebensbereiche.Bounds = new Rectangle(innen, kopf.Bottom + luecke, breite, kopfzeile + 5 * zeile);
        Liste.Bounds = new Rectangle(innen, Lebensbereiche.Bottom + luecke, breite, kopfzeile + Math.Clamp(Liste.Items.Count, 3, 14) * zeile);
        MedikamentenplanText.MaximumSize = new Size(breite, 0);
        MedikamentenplanText.Location = new Point(innen, Liste.Bottom + luecke);
        int hoehe = MedikamentenplanText.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }
}
