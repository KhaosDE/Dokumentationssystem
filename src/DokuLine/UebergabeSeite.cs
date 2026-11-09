using DokuLine.Kern;

namespace DokuLine;

// Bereich „Übergabe“: nur die Übergabe-Einträge, in kompakten Zeilen. Oben „Wohnbereich allgemein“ (eingeklappt),
// darunter je Schicht ein Abschnitt in ihrer Farbe, die laufende Schicht zuerst. Vorgabe: nur, was ich noch nicht als
// gelesen bestätigt habe. Einträge bleiben stehen, bis sie ablaufen oder erledigt sind; gelöscht wird nichts.
public sealed class UebergabeSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Übergabe";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [NotizKnopf, DruckKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Besuch();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<UebergabeAbschnitt> inhalt = [];
    private DateTime? vergleich;   // letzter Besuch vor diesem (für „neu“)

    public Button NotizKnopf { get; } = Stil.NebenKnopf("Notiz für den Wohnbereich …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Übergabeliste drucken …");
    public RadioButton NurUngelesene { get; } = new() { AutoSize = true, Checked = true, BackColor = Stil.Seite };
    public RadioButton AlleOffenen { get; } = new() { AutoSize = true, BackColor = Stil.Seite };
    public CheckBox ErledigteZeigen { get; } = new() { Text = "Erledigte und abgelaufene zeigen", AutoSize = true, BackColor = Stil.Seite };
    public Label KuerzelHinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public IReadOnlyList<UebergabeAbschnitt> Abschnitte => inhalt;
    public List<UebergabeZeile> Zeilen { get; } = [];
    public int Offen { get; private set; }
    public int Ungelesen { get; private set; }
    public int Neu { get; private set; }
    public event Action? Geaendert;   // nach jedem Neuaufbau (z. B. für den Kopftext)

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // „9 Einträge als gelesen bestätigen?“; zum Testen ersetzbar.
    public Func<string, bool> Rueckfrage { get; set; }

    public UebergabeSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        Rueckfrage = text => MessageBox.Show(FindForm(), text, "Übergabe", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        var zeigen = new Label { Text = "Zeigen:", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite };
        foreach (var c in new Control[] { zeigen, NurUngelesene, AlleOffenen, ErledigteZeigen, KuerzelHinweis })
        {
            c.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandMittel, Stil.AbstandMini);
            leiste.Controls.Add(c);
        }
        KuerzelHinweis.Text = "Für Notizen, „gelesen“ und „erledigt“ bitte zuerst oben rechts anmelden.";
        NotizKnopf.Click += (_, _) => NotizSchreiben();
        DruckKnopf.Click += (_, _) => Drucken();
        NurUngelesene.CheckedChanged += (_, _) => { if (NurUngelesene.Checked) Aktualisieren(); };
        AlleOffenen.CheckedChanged += (_, _) => { if (AlleOffenen.Checked) Aktualisieren(); };
        ErledigteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(leiste);
    }

    private static string Kuerzel => Einstellungen.Laden().Kuerzel;

    // Beim Öffnen des Bereichs: Was nach dem letzten Besuch mit diesem Kürzel kam, gilt als „neu“.
    public void Besuch()
    {
        vergleich = speicher.LetzterUebergabeBesuch(Kuerzel);
        speicher.UebergabeBesuchMerken(Kuerzel, Jetzt());
        Aktualisieren();
    }

    public string KopfText => $"{(Offen == 1 ? "1 offener Eintrag" : $"{Offen} offene Einträge")} · {Ungelesen} ungelesen" +
                              (Neu > 0 ? $" · {Neu} neu" : "");

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        string kuerzel = Kuerzel;
        // Nur der gewählte Bereich; allgemeine Einträge des Wohnbereichs stehen immer da.
        var bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt).ToDictionary(b => b.Id!.Value);
        var alle = speicher.Uebergaben(jetzt, auchErledigte: true).Where(u => u.BewohnerId is not long b || bewohner.ContainsKey(b)).ToList();
        var offen = alle.Where(u => UebergabeRegeln.Offen(u, jetzt)).ToList();
        var ungelesen = offen.Where(u => !UebergabeRegeln.GelesenVon(u, kuerzel)).ToList();
        (Offen, Ungelesen) = (offen.Count, ungelesen.Count);
        Neu = offen.Count(u => UebergabeRegeln.Neu(u, kuerzel, vergleich));
        NurUngelesene.Text = $"nur ungelesene ({Ungelesen})";
        AlleOffenen.Text = $"alle offenen ({Offen})";
        KuerzelHinweis.Visible = kuerzel.Length == 0;
        Recht.NurAngemeldet(NotizKnopf);   // ohne Anmeldung nur ansehen
        var gezeigt = NurUngelesene.Checked ? ungelesen : offen;

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Zeilen.Clear();
        inhalt.Add(new UebergabeAbschnitt("Wohnbereich allgemein", Stil.KarteRand,
            ZeilenFuer(gezeigt.Where(u => u.Allgemein), bewohner, jetzt, kuerzel), einklappbar: true));
        foreach (var schicht in UebergabeRegeln.SchichtFolge(jetzt))
        {
            var zeilen = ZeilenFuer(gezeigt.Where(u => !u.Allgemein && UebergabeRegeln.SchichtVon(u) == schicht), bewohner, jetzt, kuerzel);
            var abschnitt = new UebergabeAbschnitt(BerichtRegeln.SchichtText(schicht), BerichtZeile.Schichtfarbe(schicht), zeilen, einklappbar: false);
            AlleGelesenVorbereiten(abschnitt, kuerzel);
            inhalt.Add(abschnitt);
        }
        if (ErledigteZeigen.Checked)
        {
            var rest = ZeilenFuer(alle.Where(u => !UebergabeRegeln.Offen(u, jetzt)), bewohner, jetzt, kuerzel);
            inhalt.Add(new UebergabeAbschnitt("Erledigt / abgelaufen", Stil.KarteRand, rest, einklappbar: false, alleGelesen: false));
        }
        foreach (var a in inhalt) a.Umgeschaltet += PerformLayout;
        Controls.AddRange([.. inhalt]);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    // Die Zeilen eines Abschnitts; der Name steht nur beim ersten Eintrag eines Bewohners.
    private List<UebergabeZeile> ZeilenFuer(IEnumerable<Uebergabe> liste, Dictionary<long, Bewohner> bewohner, DateTime jetzt, string kuerzel)
    {
        var zeilen = new List<UebergabeZeile>();
        long? vorher = -1;
        foreach (var u in UebergabeRegeln.Ordnen(liste, id => bewohner.TryGetValue(id, out var b) ? b.Zimmer : ""))
        {
            string? name = u.BewohnerId != vorher && u.BewohnerId is long id && bewohner.TryGetValue(id, out var b)
                ? $"{b.Nachname}, {b.Vorname} · {b.Zimmer}" : null;
            vorher = u.BewohnerId;
            var zeile = new UebergabeZeile(u, name, jetzt, kuerzel, vergleich);
            zeile.GelesenGeklickt += x => Ausfuehren(() => speicher.UebergabeGelesen(x.Id, Kuerzel, Jetzt()));
            zeile.ErledigtGeklickt += x => Ausfuehren(() => speicher.UebergabeErledigt(x.Id, Kuerzel, Jetzt()));
            zeile.VerlaengernGeklickt += Verlaengern;
            zeile.Umgeschaltet += _ => PerformLayout();
            zeilen.Add(zeile);
        }
        Zeilen.AddRange(zeilen);
        return zeilen;
    }

    private void AlleGelesenVorbereiten(UebergabeAbschnitt abschnitt, string kuerzel)
    {
        var offen = abschnitt.Zeilen.Select(z => z.Uebergabe).Where(u => !UebergabeRegeln.GelesenVon(u, kuerzel)).ToList();
        abschnitt.AlleGelesen.Enabled = kuerzel.Length > 0 && offen.Count > 0;
        abschnitt.AlleGelesen.Click += (_, _) => AlleGelesen(offen);
    }

    // „Alle in diesem Abschnitt gelesen“: nach einer Rückfrage jeden Eintrag einzeln als gelesen bestätigen.
    public bool AlleGelesen(IReadOnlyList<Uebergabe> eintraege)
    {
        if (eintraege.Count == 0) return false;
        string frage = eintraege.Count == 1 ? "1 Eintrag als gelesen bestätigen?" : $"{eintraege.Count} Einträge als gelesen bestätigen?";
        if (!Rueckfrage(frage)) return false;
        Ausfuehren(() => { foreach (var u in eintraege) speicher.UebergabeGelesen(u.Id, Kuerzel, Jetzt()); });
        return true;
    }

    private void Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Übergabe", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    public void Verlaengern(Uebergabe u)
    {
        using var fenster = new DauerFenster("Übergabe verlängern", "Ab jetzt gültig:", null);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK && fenster.Wunsch is UebergabeWunsch wunsch)
            Ausfuehren(() => speicher.UebergabeVerlaengern(u.Id, wunsch, Kuerzel, Jetzt()));
    }

    public void NotizSchreiben()
    {
        using var fenster = new DauerFenster("Notiz für den Wohnbereich", "Wie lange?", (text, wunsch) =>
            speicher.WohnbereichNotiz(text, Kuerzel, wunsch, Jetzt()));
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Drucken()
    {
        var jetzt = Jetzt();
        var druck = new UebergabeDruck(speicher.Uebergaben(jetzt), speicher.AlleBewohner().ToDictionary(b => b.Id!.Value), jetzt);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, "Übergabeliste");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = Stil.Px(this, Stil.AbstandKlein) + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Location = new Point(rand, y);
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var a in inhalt)
        {
            int hoehe = a.Anordnen(breite);
            a.SetBounds(rand, y, breite, hoehe);
            a.Invalidate();
            y += hoehe + Stil.Px(this, Stil.AbstandKlein);
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}
