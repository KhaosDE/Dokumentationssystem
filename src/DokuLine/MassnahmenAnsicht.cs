using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Akte, Reiter „Ziele und Maßnahmen“ (Bauplan 39 und 43): oben die Ziele (Leitziel, dann Nahziele; drei Zustände nach
// Regel 5), darunter die Maßnahmen nach Ziel gruppiert, am Ende „ohne Ziel“. Danach die fünf Lebensbereiche (Berichte im
// Quartal, letzter Bericht, nächste Reflexion, „seit n Wochen kein Bericht“; Doppelklick öffnet die Dokumentation mit
// diesem Filter) und lesend, was im Medikamentenplan steht (dort wird es geführt, nicht hier).
public sealed class MassnahmenAnsicht : KartenFlaeche
{
    public const string ReiterName = "Ziele und Maßnahmen";
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel zielKopf = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly FlowLayoutPanel kopf = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    public Button NeuesZielKnopf { get; } = Stil.HauptKnopf("Neues Ziel …");
    public Button ZielAendernKnopf { get; } = Stil.NebenKnopf("Ziel ändern …");
    public Button ZielVerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public Button ZielBerichteKnopf { get; } = Stil.NebenKnopf("Berichte zu diesem Ziel");
    public Button KeinZielKnopf { get; } = Stil.NebenKnopf("Kein Ziel benennen …");
    public CheckBox FruehereZiele { get; } = new() { Text = "frühere Ziele zeigen", AutoSize = true, BackColor = Stil.Flaeche };
    public Label ZielStand { get; } = new() { AutoSize = true, BackColor = Stil.Flaeche, UseMnemonic = false };
    public ListView Ziele { get; } = Tabelle(("Ziel", 330), ("Art", 80), ("Lebensbereich", 150), ("Vereinbart", 130), ("Überprüfen bis", 110), ("Stand", 170),
        ("Maßnahmen", 90), ("Berichte", 70));
    public List<Ziel> ZieleGezeigt { get; private set; } = [];
    internal Func<string?> VerzichtFragen { get; set; }
    public event Action<Ziel>? ZielBerichteGewuenscht;
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
        VerzichtFragen = () => Eingabe.Fragen(FindForm(), "Kein Ziel benennen", $"„{ZielRegeln.KeinZielText}“ – warum? (z. B. möchte sich erst eingewöhnen)");
        foreach (var k in new Control[] { NeuesZielKnopf, ZielAendernKnopf, ZielVerlaufKnopf, ZielBerichteKnopf, KeinZielKnopf, FruehereZiele })
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        zielKopf.Controls.AddRange([NeuesZielKnopf, ZielAendernKnopf, ZielVerlaufKnopf, ZielBerichteKnopf, KeinZielKnopf, FruehereZiele]);
        Ziele.AccessibleName = "Ziele";
        NeuesZielKnopf.Click += (_, _) => ZielNeu();
        ZielAendernKnopf.Click += (_, _) => ZielAendern();
        ZielVerlaufKnopf.Click += (_, _) => ZielVerlauf();
        ZielBerichteKnopf.Click += (_, _) => ZielBerichteZeigen();
        KeinZielKnopf.Click += (_, _) => KeinZiel();
        FruehereZiele.CheckedChanged += (_, _) => { if (Bewohner is not null) Zeigen(Bewohner); };
        Ziele.DoubleClick += (_, _) => ZielAendern();
        Ziele.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
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
        Controls.AddRange([zielKopf, ZielStand, Ziele, kopf, Liste, Lebensbereiche, MedikamentenplanText]);
    }

    private static ListView Tabelle(params (string Titel, int Breite)[] spalten)
    {
        var liste = new ListView { View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable, ShowItemToolTips = true };
        foreach (var (titel, breite) in spalten) liste.Columns.Add(titel, breite);
        return liste;
    }

    public void LebensbereichOeffnen(int nr) => LebensbereichGewuenscht?.Invoke(nr);

    public Massnahme? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as Massnahme : null;
    public Ziel? GewaehltesZiel => Ziele.SelectedItems.Count == 1 ? Ziele.SelectedItems[0].Tag as Ziel : null;

    public void Zeigen(Bewohner b)
    {
        Bewohner = b;
        long id = b.Id!.Value;
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        int wochen = Einstellungen.Laden().KeinBerichtWochen;
        bool abwesend = AbwesenheitAnzeige.Text(speicher, b, jetzt) is not null;   // abwesende Tage zählen nicht als „kein Bericht“ (Bauplan 50)
        Lebensbereiche.BeginUpdate();
        Lebensbereiche.Items.Clear();
        foreach (var lb in MassnahmenRegeln.Lebensbereiche(speicher.Berichte(id), jetzt, wochen))
        {
            var zeile = new ListViewItem([$"{lb.Nr} {BerichtRegeln.Lebensbereiche[lb.Nr - 1]}", lb.BerichteImQuartal.ToString(CultureInfo.InvariantCulture),
                lb.LetzterBericht is DateTime l ? Datum.Text(DateOnly.FromDateTime(l)) : "–", Datum.Text(lb.NaechsteReflexion), abwesend ? "" : lb.Marke?.Text ?? ""])
            { ToolTipText = "Doppelklick: Berichte dieses Lebensbereichs in der Dokumentation" };
            if (lb.Marke is not null && !abwesend) zeile.SubItems[4].ForeColor = Stil.Achtung.Text;
            zeile.UseItemStyleForSubItems = false;
            Lebensbereiche.Items.Add(zeile);
        }
        Lebensbereiche.EndUpdate();

        var alleZiele = speicher.Ziele(id);
        var berichte = speicher.Berichte(id);
        var laufende = speicher.Massnahmen(id).Where(m => m.Laeuft(heute)).ToList();
        ZieleZeigen(alleZiele, speicher.Verzicht(id), laufende, berichte, heute);
        var namen = ZielNamen(alleZiele);

        var bestaetigungen = speicher.MassnahmeBestaetigungen(id);
        Gezeigt = speicher.Massnahmen(id, auchBeendete: BeendeteZeigen.Checked);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        Liste.Groups.Clear();
        var gruppen = new Dictionary<long, ListViewGroup>();
        foreach (var z in ZielRegeln.Aktive(alleZiele).Concat(alleZiele.Where(z => !z.Aktiv)))
            Liste.Groups.Add(gruppen[z.Id] = new ListViewGroup(z.Aktiv ? ZielRegeln.Text(z) : $"{ZielRegeln.Text(z)} ({ZielRegeln.StandText(z.Aktuell.Stand)})"));
        var ohneZiel = new ListViewGroup("Maßnahmen ohne Ziel");
        Liste.Groups.Add(ohneZiel);
        foreach (var m in Gezeigt.OrderBy(m => m.BeendetAm is null ? 0 : 1).ThenBy(m => m.Aktuell(heute).Lebensbereich).ThenBy(m => m.Aktuell(heute).Titel))
        {
            var f = m.Aktuell(heute);
            var letzte = bestaetigungen.Where(x => x.MassnahmeId == m.Id).MaxBy(x => x.Zeit);
            string stand = m.BeendetAm is DateOnly e ? $"beendet ab {Datum.Text(e)} ({m.BeendetGrund})"
                : f.GiltAb > heute ? $"ab {Datum.Text(f.GiltAb)}" : m.Fassungen.Count > 1 ? $"geändert, seit {Datum.Text(m.Am(heute)?.GiltAb)}" : $"seit {Datum.Text(m.Von)}";
            var zeile = new ListViewItem([f.Titel, BerichtRegeln.BereichText(f.Lebensbereich), f.Art, MassnahmenRegeln.RhythmusText(f), MassnahmenRegeln.WannText(f),
                MassnahmenRegeln.Dauer(f.Minuten), f.ZielId is long zid ? namen.GetValueOrDefault(zid, MassnahmenRegeln.OhneZiel) : MassnahmenRegeln.OhneZiel,
                letzte is null ? "–" : $"{letzte.Zeit:dd.MM. HH:mm} {MassnahmenRegeln.ErgebnisText(letzte.Ergebnis)} ({letzte.Kuerzel})", stand])
            { Tag = m, ToolTipText = f.Beschreibung, Group = f.ZielId is long g && gruppen.TryGetValue(g, out var gruppe) ? gruppe : ohneZiel };
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

    // Kurzname je Ziel für die Spalte „Ziel“: Leitziel, Nahziel 1, Nahziel 2 … (aktive), sonst „früheres Ziel“.
    private static Dictionary<long, string> ZielNamen(List<Ziel> ziele)
    {
        var namen = ziele.ToDictionary(z => z.Id, _ => "früheres Ziel");
        int nah = 0;
        foreach (var z in ZielRegeln.Aktive(ziele)) namen[z.Id] = z.Art == ZielArt.Leitziel ? "Leitziel" : $"Nahziel {++nah}";
        return namen;
    }

    private void ZieleZeigen(List<Ziel> alle, ZielVerzicht? verzicht, List<Massnahme> laufende, List<Bericht> berichte, DateOnly heute)
    {
        var zustand = ZielRegeln.Zustand(alle, verzicht);
        var marken = ZielRegeln.Marken(alle, verzicht, laufende, heute, Einstellungen.Laden().ZieleLeitziele, Einstellungen.Laden().ZieleNahziele);
        ZielStand.Text = zustand switch
        {
            ZieleZustand.Keine => ZielRegeln.Leiste(alle, verzicht),
            ZieleZustand.NichtErfasst => "Ziele: nicht erfasst – bitte mit dem Bewohner vereinbaren oder „Kein Ziel benennen …“.",
            _ => marken.Count > 0 ? string.Join(" · ", marken.Select(m => m.Text)) : "Ziele vollständig.",
        };
        bool achtung = zustand == ZieleZustand.NichtErfasst || marken.Count > 0;
        ZielStand.ForeColor = achtung ? Stil.Achtung.Text : Stil.TextMittel;
        ZielStand.Font = achtung ? Stil.FettKursiv : Stil.Standard;
        ZieleGezeigt = [.. ZielRegeln.Aktive(alle), .. FruehereZiele.Checked ? alle.Where(z => !z.Aktiv) : []];
        var massnahmenJe = laufende.ToLookup(m => m.Am(heute)?.ZielId);
        var berichteJe = berichte.ToLookup(b => b.ZielId);
        Ziele.BeginUpdate();
        Ziele.Items.Clear();
        foreach (var z in ZieleGezeigt)
        {
            var f = z.Aktuell;
            string stand = z.Aktiv ? (f.UeberpruefenBis <= heute ? "aktiv – Überprüfung fällig" : "aktiv")
                : $"{ZielRegeln.StandText(f.Stand)} am {Datum.Text(f.StandAm)}" + (f.StandSatz.Length > 0 ? $": {f.StandSatz}" : "");
            var zeile = new ListViewItem([f.Wortlaut, ZielRegeln.ArtText(z.Art), BerichtRegeln.BereichText(f.Lebensbereich), $"{Datum.Text(f.VereinbartAm)} ({f.MitWem})",
                Datum.Text(f.UeberpruefenBis), stand, massnahmenJe[z.Id].Count().ToString(CultureInfo.InvariantCulture),
                berichteJe[z.Id].Count().ToString(CultureInfo.InvariantCulture)])
            { Tag = z, ToolTipText = f.Wortlaut, UseItemStyleForSubItems = false };
            if (!z.Aktiv) zeile.ForeColor = Stil.TextLeise;
            if (z.Aktiv && f.UeberpruefenBis <= heute) zeile.SubItems[5].ForeColor = Stil.Achtung.Text;
            if (z.Aktiv && z.Art == ZielArt.Nahziel && !massnahmenJe[z.Id].Any()) zeile.SubItems[6].ForeColor = Stil.Achtung.Text;
            Ziele.Items.Add(zeile);
        }
        Ziele.EndUpdate();
    }

    private void KnoepfeSetzen()
    {
        if (Bewohner?.Id is not long id) return;
        string? anlegen = speicher.MassnahmenAnlegenGrund(Recht.Kuerzel, id);
        foreach (var k in new[] { NeuKnopf, AendernKnopf, BeendenKnopf, NeuesZielKnopf, ZielAendernKnopf, KeinZielKnopf }) Stil.Sperren(k, anlegen);
        string? ohneZiel = GewaehltesZiel is null ? "Bitte zuerst ein Ziel wählen." : null;
        if (anlegen is null)
        {
            Stil.Sperren(ZielAendernKnopf, ohneZiel ?? (GewaehltesZiel!.Aktiv ? null : "Das Ziel ist abgeschlossen – bitte ein neues vereinbaren."));
            Stil.Sperren(KeinZielKnopf, ZieleGezeigt.Any(z => z.Aktiv) ? "Es sind Ziele eingetragen." : null);
        }
        Stil.Sperren(ZielVerlaufKnopf, ohneZiel);
        Stil.Sperren(ZielBerichteKnopf, ohneZiel);
        Recht.Anwenden(speicher, Funktionen.MassnahmenBestaetigen, BestaetigenKnopf);
        string? ohne = Gewaehlt is null ? "Bitte zuerst eine Maßnahme wählen." : Gewaehlt.BeendetAm is not null ? "Die Maßnahme ist beendet." : null;
        if (anlegen is null) { Stil.Sperren(AendernKnopf, ohne); Stil.Sperren(BeendenKnopf, ohne); }
        if (Stil.Sperrgrund(BestaetigenKnopf) is null) Stil.Sperren(BestaetigenKnopf, ohne);
    }

    public bool ZielNeu(ZielArt art = ZielArt.Nahziel)
    {
        if (Bewohner is null || Stil.Sperrgrund(NeuesZielKnopf) is not null) return false;
        if (art == ZielArt.Nahziel && !ZieleGezeigt.Any(z => z.Aktiv && z.Art == ZielArt.Leitziel)) art = ZielArt.Leitziel;   // zuerst das Leitziel
        using var fenster = new ZielFenster(speicher, Bewohner, null, art) { Jetzt = Jetzt };
        return Fertig(Dialog.Zeigen(fenster, FindForm()));
    }

    public bool ZielAendern()
    {
        if (Bewohner is null || GewaehltesZiel is not Ziel z || Stil.Sperrgrund(ZielAendernKnopf) is not null) return false;
        using var fenster = new ZielFenster(speicher, Bewohner, z) { Jetzt = Jetzt };
        return Fertig(Dialog.Zeigen(fenster, FindForm()));
    }

    public void ZielBerichteZeigen()
    {
        if (GewaehltesZiel is Ziel z && Stil.Sperrgrund(ZielBerichteKnopf) is null) ZielBerichteGewuenscht?.Invoke(z);
    }

    public void ZielVerlauf()
    {
        if (GewaehltesZiel is not Ziel z) return;
        var zeilen = z.Fassungen.Select(f => $"{f.Erfasst:dd.MM.yyyy HH:mm} ({f.Kuerzel}): „{f.Wortlaut}“ – {BerichtRegeln.BereichText(f.Lebensbereich)}, " +
            $"vereinbart {Datum.Text(f.VereinbartAm)} mit {f.MitWem}, überprüfen bis {Datum.Text(f.UeberpruefenBis)}, {ZielRegeln.StandText(f.Stand)}" +
            (f.StandSatz.Length > 0 ? $": {f.StandSatz}" : ""));
        using var fenster = new TextFenster($"Verlauf: {ZielRegeln.ArtText(z.Art)}", string.Join(Environment.NewLine, zeilen));
        Dialog.Zeigen(fenster, FindForm());
    }

    public bool KeinZiel()
    {
        if (Bewohner?.Id is not long id || Stil.Sperrgrund(KeinZielKnopf) is not null || VerzichtFragen() is not string grund) return false;
        try { speicher.VerzichtSetzen(id, grund, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Kein Ziel benennen", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return Fertig(DialogResult.OK);
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
        int zeile = Stil.Px(this, 20), kopfzeile = Stil.Px(this, 26), gruppe = Stil.Px(this, 30);
        zielKopf.MaximumSize = new Size(breite, 0);
        zielKopf.Bounds = new Rectangle(new Point(innen, innen), zielKopf.GetPreferredSize(new Size(breite, 0)));
        ZielStand.MaximumSize = new Size(breite, 0);
        ZielStand.Location = new Point(innen, zielKopf.Bottom);
        Ziele.Bounds = new Rectangle(innen, ZielStand.Bottom + luecke, breite, kopfzeile + Math.Clamp(Ziele.Items.Count, 2, 8) * zeile);
        kopf.MaximumSize = new Size(breite, 0);
        kopf.Bounds = new Rectangle(new Point(innen, Ziele.Bottom + 2 * luecke), kopf.GetPreferredSize(new Size(breite, 0)));
        int gruppen = Liste.Groups.Cast<ListViewGroup>().Count(g => g.Items.Count > 0);
        Liste.Bounds = new Rectangle(innen, kopf.Bottom + luecke, breite, kopfzeile + Math.Clamp(Liste.Items.Count, 3, 14) * zeile + gruppen * gruppe);
        Lebensbereiche.Bounds = new Rectangle(innen, Liste.Bottom + 2 * luecke, breite, kopfzeile + 5 * zeile);
        MedikamentenplanText.MaximumSize = new Size(breite, 0);
        MedikamentenplanText.Location = new Point(innen, Lebensbereiche.Bottom + luecke);
        int hoehe = MedikamentenplanText.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }
}
