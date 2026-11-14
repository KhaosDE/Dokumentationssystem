using DokuLine.Kern;

namespace DokuLine;

// Bereich „HEB-Fristen“ (Bauplan 52): alle Bewohner des gewählten Bereichs nach Fälligkeit, mit Stand, Bezugspflege und Stufe
// der Erinnerung. „Bogen öffnen …“ bzw. „Neuer HEB-Bogen …“, Fälligkeit ändern, kein HEB nötig, abgegeben, drucken.
public sealed class HebFristenSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "HEB-Fristen";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [BogenKnopf, FaelligKnopf, KeinKnopf, AbgegebenKnopf, DruckenKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    public Button BogenKnopf { get; } = Stil.HauptKnopf("Bogen öffnen …");
    public Button FaelligKnopf { get; } = Stil.NebenKnopf("Fälligkeit ändern …");
    public Button KeinKnopf { get; } = Stil.NebenKnopf("Kein HEB nötig …");
    public Button AbgegebenKnopf { get; } = Stil.NebenKnopf("Abgegeben …");
    public Button DruckenKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        AccessibleName = "HEB-Fristen", Font = Stil.Standard,
    };
    public List<HebFristStand> Zeilen { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // Zum Testen ersetzbar: Fragen, das Öffnen eines Bogens und die Druckvorschau.
    internal Func<string, string, string?> Fragen { get; set; }
    internal Action<HebFristStand> BogenZeigen { get; set; }
    internal Action<Func<System.Drawing.Printing.PrintDocument>, string> Vorschau { get; set; }
    public event Action? Geaendert;

    public HebFristenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Fragen = (titel, frage) => Eingabe.Fragen(FindForm(), titel, frage);
        BogenZeigen = BogenOeffnen;
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(FindForm()!, bauen, titel);
        foreach (var (titel, breite) in new[] { ("Zimmer", 70), ("Name", 180), ("Fällig am", 150), ("Stand", 170), ("Bezugspflege", 110), ("Erinnerung", 230) })
            Liste.Columns.Add(titel, breite);
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        Liste.DoubleClick += (_, _) => Bogen();
        BogenKnopf.Click += (_, _) => Bogen();
        FaelligKnopf.Click += (_, _) => Ausfuehren(FaelligKnopf, HebFristAblauf.FaelligAendern);
        KeinKnopf.Click += (_, _) => Ausfuehren(KeinKnopf, HebFristAblauf.KeinHeb);
        AbgegebenKnopf.Click += (_, _) => Ausfuehren(AbgegebenKnopf, HebFristAblauf.Abgegeben);
        DruckenKnopf.Click += (_, _) => Drucken();
        Controls.Add(Liste);
    }

    public HebFristStand? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as HebFristStand : null;

    // „31.03.2027“, „31.03.2027 (Vorschlag)“ oder „kein HEB nötig“ (auch für die Seite „Hilfeplangespräche“).
    public static string FaelligText(HebFristStand s) => s.Zustand switch
    {
        HebFristZustand.KeinHeb => "kein HEB nötig",
        HebFristZustand.NichtErfasst => $"{Datum.Text(s.FaelligAm)} (Vorschlag)",
        _ => Datum.Text(s.FaelligAm),
    };

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        var heute = DateOnly.FromDateTime(jetzt);
        var w = Einstellungen.Laden();
        speicher.HebErinnerungen(jetzt);
        var bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt);
        var bezuege = speicher.Bezuege();
        long? vorher = Gewaehlt?.Bewohner.Id;
        Zeilen = [.. speicher.HebFristen(bewohner, heute).OrderBy(z => z.FaelligAm ?? DateOnly.MaxValue).ThenBy(z => z.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal)];
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var z in Zeilen)
        {
            var marke = HebFristRegeln.Marke(z, heute, w.HebErinnernWochen, w.HebAchtungWochen);
            var zeile = new ListViewItem([z.Bewohner.Zimmer, z.Bewohner.Name, FaelligText(z), HebFristRegeln.StandText(z),
                bezuege.GetValueOrDefault(z.Bewohner.Id!.Value)?.Bezugspflege?.Kuerzel ?? "", marke?.Text ?? ""])
            { Tag = z, UseItemStyleForSubItems = false };
            if (Stil.TextFarbe(marke?.Art) is Color farbe) zeile.SubItems[5].ForeColor = farbe;
            if (z.Zustand == HebFristZustand.NichtErfasst) zeile.SubItems[2].ForeColor = Stil.Achtung.Text;
            zeile.Selected = z.Bewohner.Id == vorher;
            Liste.Items.Add(zeile);
        }
        Liste.EndUpdate();
        int ueberfaellig = Zeilen.Count(z => HebFristRegeln.Stufe(z, heute, w.HebErinnernWochen, w.HebAchtungWochen) == HebStufe.Ueberfaellig);
        int bald = Zeilen.Count(z => HebFristRegeln.Stufe(z, heute, w.HebErinnernWochen, w.HebAchtungWochen) == HebStufe.Achtung);
        KopfText = $"{Zeilen.Count} Bewohner · {ueberfaellig} überfällig · {bald} in den nächsten {w.HebAchtungWochen} Wochen · " +
                   $"{Zeilen.Count(z => z.Zustand == HebFristZustand.NichtErfasst)} nicht erfasst";
        KnoepfeSetzen();
        Geaendert?.Invoke();
    }

    private void KnoepfeSetzen()
    {
        Recht.Anwenden(speicher, Funktionen.Heb, BogenKnopf, FaelligKnopf, KeinKnopf, AbgegebenKnopf);
        BogenKnopf.Text = Gewaehlt is { Bogen: null } ? "Neuer HEB-Bogen …" : "Bogen öffnen …";
        if (Recht.Grund(speicher, Funktionen.Heb) is not null) return;
        string? ohne = Gewaehlt is null ? "Bitte zuerst einen Bewohner wählen." : null;
        foreach (var k in new[] { BogenKnopf, FaelligKnopf, KeinKnopf }) Stil.Sperren(k, ohne);
        Stil.Sperren(AbgegebenKnopf, ohne ?? HebFristAblauf.AbgegebenGesperrt(Gewaehlt!));
    }

    private bool Ausfuehren(Button knopf, Func<Speicher, HebFristStand, Func<string, string, string?>, DateTime, IWin32Window?, bool> ablauf)
    {
        if (Gewaehlt is not HebFristStand z || Stil.Sperrgrund(knopf) is not null) return false;
        if (!ablauf(speicher, z, Fragen, Jetzt(), FindForm())) return false;
        Aktualisieren();
        return true;
    }

    public bool FaelligAendern() => Ausfuehren(FaelligKnopf, HebFristAblauf.FaelligAendern);

    public bool Abgegeben() => Ausfuehren(AbgegebenKnopf, HebFristAblauf.Abgegeben);

    public bool Bogen()
    {
        if (Gewaehlt is not HebFristStand z || Stil.Sperrgrund(BogenKnopf) is not null) return false;
        BogenZeigen(z);
        Aktualisieren();
        return true;
    }

    // Vorhandenen Bogen des Abschnitts öffnen, sonst einen neuen anlegen und öffnen.
    private void BogenOeffnen(HebFristStand z)
    {
        var bogen = z.Bogen;
        if (bogen is null)
        {
            using var neu = new HebNeuFenster(speicher, z.Bewohner, DateOnly.FromDateTime(Jetzt()));
            if (Dialog.Zeigen(neu, FindForm()) != DialogResult.OK || neu.Angelegt is not HebBogen angelegt) return;
            bogen = angelegt;
        }
        using var fenster = new HebFenster(speicher, z.Bewohner, bogen);
        Dialog.Zeigen(fenster, FindForm());
    }

    public bool Drucken()
    {
        if (Zeilen.Count == 0) return false;
        var heute = DateOnly.FromDateTime(Jetzt());
        var w = Einstellungen.Laden();
        var bezuege = speicher.Bezuege();
        var zeilen = Zeilen.Select(z => new[]
        {
            z.Bewohner.Zimmer, z.Bewohner.Name, FaelligText(z), HebFristRegeln.StandText(z), bezuege.GetValueOrDefault(z.Bewohner.Id!.Value)?.Bezugspflege?.Kuerzel ?? "",
            HebFristRegeln.Marke(z, heute, w.HebErinnernWochen, w.HebAchtungWochen)?.Text ?? "",
        }).ToList();
        var druck = new TabellenDruck("HEB-Fristen", $"{Arbeitsbereich.Gewaehlt} · Stand {Datum.Text(heute)}",
            ["Zimmer", "Name", "Fällig am", "Stand", "Bezugspflege", "Erinnerung"], [1f, 3f, 2.2f, 2.6f, 1.5f, 3.2f], zeilen);
        Vorschau(druck.DokumentBauen, "HEB-Fristen");
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        Liste.SetBounds(rand, rand, Math.Max(100, ClientSize.Width - 2 * rand), Math.Max(Stil.Px(this, 120), ClientSize.Height - 2 * rand));
    }
}
