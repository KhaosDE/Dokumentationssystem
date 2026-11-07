using Dokusystem.Kern;

namespace Dokusystem.Modul.Ausgangsliste;

// Eine Karte je Bewohner auf der Seite „Ausgangsliste“ und im Akte-Reiter „Ausgang“: Marken,
// Name, Ausgang oder Grund des Status, Unterbringung, Infos, heutige digitale Ausgänge und die
// Knöpfe. Entspricht der Bewohnerkarte der Ausgangsliste, im Aufbau aber wie die Karten des
// Dokumentationssystems (Marken oben, Text, Knöpfe unten).
public sealed class PlatzKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly List<Label> zeilen = [];
    private readonly List<bool> zeigen = [];   // eigene Merker: Visible meldet false, solange die Karte nicht angezeigt wird

    public Platz Platz { get; }
    public IReadOnlyList<string> MarkenTexte { get; }
    public string AusgangZeile => zeilen[1].Text;
    public string UnterbringungZeile => zeilen[2].Text;
    public Button AusgangKnopf { get; } = Stil.NebenKnopf("Ausgang …");
    public Button AnwesenheitKnopf { get; } = Stil.NebenKnopf("Anwesenheit …");
    public Button UnterbringungKnopf { get; } = Stil.NebenKnopf("Unterbringung …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public Button AlleinKnopf { get; } = Stil.NebenKnopf("Geht raus: allein");
    public Button MitBewohnerKnopf { get; } = Stil.NebenKnopf("Geht raus: mit Bewohner");
    public Button ZurueckKnopf { get; } = Stil.HauptKnopf("Ist zurück");

    // digital: null = Untermodul „Digitaler Ausgang“ ausgeschaltet.
    public PlatzKarte(Platz p, DateTime jetzt, DigitalStand? digital, bool kueche, bool mitName = true)
    {
        Platz = p;
        var liste = AusgangsMarken.Fuer(p, jetzt, digital, alle: true, kueche: kueche);
        MarkenTexte = liste.Select(m => m.Text).ToList();
        foreach (var m in liste) marken.Controls.Add(new MarkenFeld(m.Text, Stil.MarkeFuer(m.Art)) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        string ausgang;
        if (p.HatStatus)
        {
            string frist = KartenText.Frist(p.StatusGrund, jetzt, out _);
            ausgang = $"{Verlauf.Status(p.Status)}: {p.StatusGrund}" + (frist.Length > 0 ? $" ({frist})" : "") +
                      (p.Ausgang.Length > 0 ? $" · danach wieder: {p.Ausgang}" : "");
        }
        else if (p.Stunden > 0) ausgang = $"Ausgang {p.Stunden} Std: {KartenText.Aufteilung(p)}";
        else ausgang = p.Ausgang.Length > 0 ? $"Ausgang: {string.Join(" · ", AusgangText.Bausteine(p.Ausgang).Where(b => !AusgangText.IstInfo(b)))}" : "Kein Ausgang eingetragen";
        string unterbringung = p.Unterbringung switch
        {
            Unterbringung.NichtErfasst => "Unterbringung: nicht erfasst – Beschluss in der Akte klären",
            Unterbringung.Forensik => "Unterbringung: Forensik",
            _ => $"Unterbringung: {Verlauf.Unterbringung(p.Unterbringung)} · {p.FristText(jetzt)}",
        };
        var infos = new List<string>();
        if (KartenText.Info(p) is { Length: > 0 } info) infos.Add($"Info: {info}");
        infos.AddRange(KartenText.Hinweise(p));
        if (digital is not null && digital.Erledigt.Count > 0)
            infos.Add("Heute: " + string.Join(", ", digital.Erledigt.Select(e => $"{e.Zeitraum} (Hz. {e.Handzeichen})")));

        foreach (var (text, schrift, farbe) in new[]
                 {
                     (p.Bewohnername, Stil.KarteName, Stil.TextNormal),
                     (ausgang, Stil.Fett, Stil.TextNormal),
                     (unterbringung, Stil.Standard, Stil.TextMittel),
                     (string.Join(" · ", infos), Stil.Standard, Stil.TextMittel),
                 })
        {
            var l = new Label { Text = text, Font = schrift, ForeColor = farbe, AutoSize = true, BackColor = Stil.Flaeche, UseMnemonic = false };
            zeilen.Add(l);
            Controls.Add(l);
        }
        zeigen.AddRange([mitName, true, true, infos.Count > 0]);
        for (int i = 0; i < zeilen.Count; i++) zeilen[i].Visible = zeigen[i];

        var alle = new List<Button> { AusgangKnopf, AnwesenheitKnopf, UnterbringungKnopf, VerlaufKnopf };
        if (digital is not null)
        {
            bool frei = !p.HatStatus && digital.Laufend is null;
            AlleinKnopf.Visible = frei && DigitalerAusgang.Anspruch(p, "allein") > 0;
            MitBewohnerKnopf.Visible = frei && DigitalerAusgang.Anspruch(p, "mit Bewohner") > 0;
            ZurueckKnopf.Visible = digital.Laufend is not null;
            alle.InsertRange(0, [ZurueckKnopf, AlleinKnopf, MitBewohnerKnopf]);
        }
        else
            AlleinKnopf.Visible = MitBewohnerKnopf.Visible = ZurueckKnopf.Visible = false;
        foreach (var k in alle)
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"{p.Bewohnername}, {string.Join(", ", MarkenTexte)}. {ausgang}. {unterbringung}";
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Platz is null || zeigen.Count < zeilen.Count) return;   // Anordnung schon während des Aufbaus
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), y = innen;
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var l in zeilen.Where((_, i) => zeigen[i]))
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
