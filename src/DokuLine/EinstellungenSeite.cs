using DokuLine.Kern;

namespace DokuLine;

// Einstellungen wie in den Windows-11-Einstellungen: eine ganze Seite, links die
// Gruppen mit Symbol und oben die Suche, rechts die Einstellungen der Gruppe als Karten (Name, ein
// Satz Erklärung, rechts das Bedienelement). Änderungen wirken sofort – Häkchen und Zahlen beim
// Ändern, Textfelder beim Verlassen; nur ganze Datensätze (Mitarbeiter, Medikament, Fahrzeug,
// Standardaufgabe, HEB-Zuordnung) haben eigene Fenster mit „Speichern“. Ohne Recht bleibt alles
// sichtbar, aber gesperrt, mit dem Grund. Folgenreiches (Datenbank-Ort, Demo-Daten) steht nur unter
// „System“ und fragt vorher nach.
public sealed partial class EinstellungenSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Einstellungen";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => Knoepfe;
    void IBereichSeite.Zeigen(bool filterBehalten) => Zeigen();

    public static readonly (string Name, string Symbol)[] GruppenListe =
    [
        ("Persönlich", ""), ("Wohnbereich", ""), ("Module", ""), ("Aufgaben", ""), ("Fahrzeuge", ""),
        ("Vorlagen", ""), ("Medikamente", ""), ("Verzeichnisse", ""), ("Verwaltung", ""), ("System", ""), ("Über", ""),
    ];
    private const int LogoBreite = 160, LogoHoehe = 64;

    private readonly Speicher speicher;
    private readonly Panel links = new() { BackColor = Stil.Seite };
    private readonly Panel rechts = new() { AutoScroll = true, BackColor = Stil.Seite };
    private readonly Label titel = new() { AutoSize = true, Font = Stil.Ueberschrift, ForeColor = Stil.TextNormal, BackColor = Stil.Seite };
    private readonly Panel logoVorschau = new() { Size = new Size(LogoBreite, LogoHoehe), BackColor = Stil.Flaeche };
    private Image? logoBild;
    private bool laedt;   // beim Füllen der Felder nichts übernehmen
    private bool trefferZeigen;

    public TextBox Suche { get; } = new() { PlaceholderText = "Einstellung suchen", AccessibleName = "Einstellung suchen" };
    public ListBox Treffer { get; } = new() { IntegralHeight = false, Visible = false, BorderStyle = BorderStyle.None, Font = Stil.Standard };
    public List<SeitenleistenEintrag> Gruppen { get; } = [];
    public List<EinstellungKarte> Karten { get; } = [];
    public string AktiveGruppe { get; private set; } = "Persönlich";
    public string KopfText => AktiveGruppe == VerzeichnisGruppe ? Verzeichnis.KopfText : $"{AktiveGruppe} · Änderungen gelten sofort";
    // „Verzeichnisse“ zeigt statt Karten das Personenverzeichnis; dessen Knöpfe stehen in der Aktionsleiste.
    public const string VerzeichnisGruppe = "Verzeichnisse";
    public VerzeichnisAnsicht Verzeichnis { get; }
    public Button[] Knoepfe => AktiveGruppe == VerzeichnisGruppe ? Verzeichnis.Knoepfe : [];
    public event Action<long>? AkteGewuenscht;

    // Persönlich
    public Label Angemeldet { get; } = new() { AutoSize = true, Font = Stil.Fett, UseMnemonic = false };
    public Button AbmeldenKnopf { get; } = Stil.NebenKnopf(Abmeldung.KnopfText);
    public Panel MeineFarbe { get; } = new() { Size = new Size(28, 28), BorderStyle = BorderStyle.FixedSingle };
    public Button FarbeKnopf { get; } = Stil.NebenKnopf("Farbe wählen …");
    public Button FarbeVorgabeKnopf { get; } = Stil.NebenKnopf("Vorgabe");
    public Func<Color, Color?> FarbeWaehlen { get; set; } = aktuell =>
    {
        using var dialog = new ColorDialog { Color = aktuell, FullOpen = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.Color : null;
    };
    public CheckBox MenueEingeklappt { get; } = new() { Text = "eingeklappt", AutoSize = true };
    // Wohnbereich
    public ComboBox Bereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 220, MaxLength = Kern.Bereiche.Laenge };
    public TextBox Frueh { get; } = new UhrzeitFeld() { Width = 60 };   // Doppelpunkt setzt das Programm
    public TextBox Spaet { get; } = new UhrzeitFeld() { Width = 60 };
    public TextBox Nacht { get; } = new UhrzeitFeld() { Width = 60 };
    public Button ZimmerKnopf { get; } = Stil.NebenKnopf("Zimmer und Betten …");
    public NumericUpDown BeschlussFrist { get; } = new() { Minimum = 1, Maximum = 365, Width = 80 };
    public NumericUpDown Wartezeit { get; } = new() { Minimum = 5, Maximum = 240, Width = 80 };
    public NumericUpDown KeinBericht { get; } = new() { Minimum = 1, Maximum = 26, Width = 80, AccessibleName = "Wochen ohne Bericht" };
    public NumericUpDown ZieleLeit { get; } = new() { Minimum = 0, Maximum = 3, Width = 60, AccessibleName = "Mindestzahl Leitziele" };
    public NumericUpDown ZieleNah { get; } = new() { Minimum = 0, Maximum = 6, Width = 60, AccessibleName = "Mindestzahl Nahziele" };
    public Button MassnahmenListenKnopf { get; } = Stil.NebenKnopf("Arten und Vorlagen …");
    public NumericUpDown HebFrist { get; } = new() { Minimum = 1, Maximum = 60, Width = 80 };
    public Button LogoKnopf { get; } = Stil.NebenKnopf("Bild wählen …");
    public Button LogoWeg { get; } = Stil.NebenKnopf("Bild entfernen");
    // Module, Aufgaben, Fahrzeuge, Vorlagen, Medikamente, Verwaltung
    public Dictionary<string, CheckBox> Module { get; } = [];
    public Button VorlagenKnopf { get; } = Stil.NebenKnopf("Standardaufgaben …");
    public Button FahrzeugeKnopf { get; } = Stil.NebenKnopf("Fahrzeuge …");
    // Anzeige der laufenden Therapien für den Fernseher (Bauplan 29o); das Hauptfenster gibt den Dienst.
    public AnzeigeDienst? Anzeige { get; set; }
    public Button AnzeigeKnopf { get; } = Stil.NebenKnopf("Anzeige einrichten …");
    public ComboBox VollbildBereich { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, AccessibleName = "Wohnbereich für das Vollbild" };
    public Button VollbildKnopf { get; } = Stil.NebenKnopf("Im Vollbild zeigen");
    public Dictionary<Bogenart, Label> Vorlagen { get; } = [];
    public Func<string?> PdfWaehlen { get; set; } = () =>
    {
        using var dialog = new OpenFileDialog { Title = "HEB-Vorlage (PDF) wählen", Filter = "PDF (*.pdf)|*.pdf" };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    };
    public Button MedikamenteKnopf { get; } = Stil.NebenKnopf("Medikamentendatenbank …");
    public CheckBox NachschlageAnsicht { get; } = new() { Text = "anzeigen", AutoSize = true };
    public Button MitarbeiterKnopf { get; } = Stil.NebenKnopf("Mitarbeiter …");
    public Button ListenKnopf { get; } = Stil.NebenKnopf("Bereiche, Positionen, Berichtsarten …");
    public Button AkteListenKnopf { get; } = Stil.NebenKnopf("Krankenkassen, Kostenträger, Aufgaben, Kategorien …");
    public Button UebernahmeKnopf { get; } = Stil.NebenKnopf("Digitale Akte übernehmen …");
    public Button EinrichtungKnopf { get; } = Stil.NebenKnopf("Angaben der Einrichtung …");
    public Button MeldestellenKnopf { get; } = Stil.NebenKnopf("Stellen bearbeiten …");
    public Button RichtwerteKnopf { get; } = Stil.NebenKnopf("Richtwerte …");
    public Button SicherungKnopf { get; } = Stil.NebenKnopf("Sicherung …");
    public Button UpdateKnopf { get; } = Stil.NebenKnopf("Update vorbereiten …");
    public Button PasswortKnopf { get; } = Stil.NebenKnopf("Passwort ändern …");
    public Button PasswoerterKnopf { get; } = Stil.NebenKnopf("Passwörter …");
    public Button ProtokollKnopf { get; } = Stil.NebenKnopf("Zugriffsprotokoll …");
    public Button HaeuserKnopf { get; } = Stil.NebenKnopf("Häuser …");
    public Button RechteKnopf { get; } = Stil.NebenKnopf("Rechte …");
    // System, Über
    public Label DatenbankOrt { get; } = new() { AutoSize = true, MaximumSize = new Size(360, 0), UseMnemonic = false };
    public Button OrdnerKnopf { get; } = Stil.NebenKnopf("Gemeinsamen Ordner wählen …");
    public Button LokalKnopf { get; } = Stil.NebenKnopf("Lokal (Vorgabe)");
    public Func<string?> OrdnerWaehlen { get; set; } = () =>
    {
        using var dialog = new FolderBrowserDialog { Description = "Gemeinsamer Ordner für die Datenbank (nur ausgedachte Daten)", UseDescriptionForTitle = true };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    };
    public CheckBox AutoUpdate { get; } = new() { Text = "beim Start suchen", AutoSize = true };
    public Button DemoKnopf { get; } = Stil.NebenKnopf("Demo-Daten zurücksetzen …");
    public Label WebAnsichtStand { get; } = new() { AutoSize = true, MaximumSize = new Size(360, 0) };
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    // Rückfrage vor Folgenreichem (Datenbank-Ort); true = ja. Zum Testen ersetzbar.
    public Func<string, bool> Rueckfrage { get; set; }

    public event Action? LogoGeaendert;
    public event Action? BereichGeaendert;
    public event Action? SchichtzeitenGeaendert;
    public event Action? AbmeldenGewuenscht;
    public event Action<bool>? MenueGewuenscht;
    public event Action? DemoGewuenscht;
    public event Action? Geaendert;

    public EinstellungenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Rueckfrage = text => MessageBox.Show(FindForm(), text, "Einstellungen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

        // ----- links: Suche und Gruppen -----
        links.Controls.Add(Suche);
        links.Controls.Add(Treffer);
        foreach (var (name, symbol) in GruppenListe)
        {
            var eintrag = new SeitenleistenEintrag(name) { Symbol = symbol };
            eintrag.Click += (_, _) => GruppeWaehlen(name);
            Gruppen.Add(eintrag);
            links.Controls.Add(eintrag);
        }
        Suche.TextChanged += (_, _) => SucheZeigen();
        Suche.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Springen(Suche.Text); } };
        Treffer.Click += (_, _) => { if (Treffer.SelectedItem is string t) Springen(t.Split(" · ")[0]); };
        Treffer.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter && Treffer.SelectedItem is string t) Springen(t.Split(" · ")[0]); };
        links.Layout += (_, _) => LinksAnordnen();
        rechts.Controls.Add(titel);
        Verzeichnis = new VerzeichnisAnsicht(speicher) { Visible = false };
        Verzeichnis.AkteGewuenscht += id => AkteGewuenscht?.Invoke(id);
        Verzeichnis.Geaendert += () => { if (AktiveGruppe == VerzeichnisGruppe) Geaendert?.Invoke(); };
        rechts.Controls.Add(Verzeichnis);
        rechts.Layout += (_, _) => RechtsAnordnen();
        Controls.AddRange([rechts, links]);

        KartenBauen();
        foreach (var k in Karten) rechts.Controls.Add(k);
        Fehler.MaximumSize = new Size(560, 0);
        rechts.Controls.Add(Fehler);
        GruppeWaehlen("Persönlich");
    }

    // ------------------------------------------------------------------ Anzeigen

    // Beim Öffnen: Werte und Rechte neu lesen (Gruppe bleibt).
    public void Zeigen()
    {
        laedt = true;
        try
        {
            var werte = Einstellungen.Laden();
            var ich = speicher.MitarbeiterLesen(Recht.Kuerzel);
            Angemeldet.Text = ich is null ? "niemand" : ich.Anzeige;
            FarbeZeigen();
            MenueEingeklappt.Checked = werte.MenueEingeklappt;
            Bereich.Text = werte.Bereich;
            var zeiten = speicher.Schichtzeiten(werte.Bereich);
            (Frueh.Text, Spaet.Text, Nacht.Text) = (zeiten.Frueh.ToString("HH:mm"), zeiten.Spaet.ToString("HH:mm"), zeiten.Nacht.ToString("HH:mm"));
            BeschlussFrist.Value = Math.Clamp(werte.BeschlussFristTage, (int)BeschlussFrist.Minimum, (int)BeschlussFrist.Maximum);
            Wartezeit.Value = Math.Clamp(werte.WartezeitMinuten, (int)Wartezeit.Minimum, (int)Wartezeit.Maximum);
            KeinBericht.Value = Math.Clamp(werte.KeinBerichtWochen, (int)KeinBericht.Minimum, (int)KeinBericht.Maximum);
            ZieleLeit.Value = Math.Clamp(werte.ZieleLeitziele, 0, 3);
            ZieleNah.Value = Math.Clamp(werte.ZieleNahziele, 0, 6);
            HebFrist.Value = Math.Clamp(werte.HebFristMonate, (int)HebFrist.Minimum, (int)HebFrist.Maximum);
            LogoLaden(Einstellungen.LogoPfad());
            foreach (var (schluessel, haken) in Module) haken.Checked = speicher.ModulAn(schluessel);
            foreach (var art in Vorlagen.Keys) VorlageAnzeigen(art);
            NachschlageAnsicht.Checked = werte.NachschlageAnsicht;
            OrtAnzeigen(werte.DatenbankOrdner);
            AutoUpdate.Checked = werte.AutoUpdate;
            WebAnsichtStand.Text = $"WebView2 {Nachschlageansicht.Laufzeit}";
            foreach (var k in Karten) k.Sperren(k.Funktion is string r ? Recht.Grund(speicher, r) : null);
            ModulHakenAbgleichen();   // nach den Karten – deren Sperre gäbe die Untermodule sonst wieder frei
            // Erst nach den Karten: Deren Sperre setzt die Knöpfe sonst wieder frei. Die eigene Farbe braucht eine Anmeldung.
            foreach (var k in new[] { FarbeKnopf, FarbeVorgabeKnopf }) Stil.Sperren(k, ich is null ? RechteRegeln.NichtAngemeldetText : null);
            Stil.Sperren(MitarbeiterKnopf, Recht.Darf(speicher, Funktionen.MitarbeiterAlle) ? null : Recht.Grund(speicher, Funktionen.MitarbeiterBereich));
            Fehler.Visible = fehlerZeigen = false;
        }
        finally { laedt = false; }
        GruppeWaehlen(AktiveGruppe);
    }

    // Eine Gruppe ohne eine einzige erlaubte Karte verschwindet; die Verzeichnisse darf jeder ansehen.
    public bool GruppeSichtbar(string gruppe) => gruppe == VerzeichnisGruppe || Karten.Any(k => k.Gruppe == gruppe && !k.OhneRecht);

    public void GruppeWaehlen(string gruppe)
    {
        if (!GruppeSichtbar(gruppe)) gruppe = "Persönlich";
        AktiveGruppe = gruppe;
        foreach (var g in Gruppen) g.Visible = GruppeSichtbar(g.Text);
        LinksAnordnen();
        foreach (var g in Gruppen) g.Aktiv = g.Text == gruppe;
        titel.Text = gruppe;
        foreach (var k in Karten) k.Visible = k.Gruppe == gruppe && !k.OhneRecht;
        Verzeichnis.Visible = gruppe == VerzeichnisGruppe;
        if (Verzeichnis.Visible) Verzeichnis.Aktualisieren();
        rechts.AutoScrollPosition = Point.Empty;
        rechts.PerformLayout();
        Geaendert?.Invoke();
    }

    // Suche über Name und Erklärung; Treffer als Liste links.
    public List<EinstellungKarte> Suchen(string text) =>
        text.Trim().Length == 0 ? [] : Karten.Where(k => !k.OhneRecht && k.Passt(text.Trim())).ToList();

    private void SucheZeigen()
    {
        var treffer = Suchen(Suche.Text);
        Treffer.BeginUpdate();
        Treffer.Items.Clear();
        Treffer.Items.AddRange([.. treffer.Select(k => (object)$"{k.Titel} · {k.Gruppe}")]);
        if (treffer.Count == 0 && Suche.Text.Trim().Length > 0) Treffer.Items.Add("Nichts gefunden.");
        Treffer.EndUpdate();
        Treffer.Visible = trefferZeigen = Suche.Text.Trim().Length > 0;
        LinksAnordnen();
    }

    // Zum ersten Treffer springen: Gruppe wählen, Karte zeigen und kurz hervorheben.
    public bool Springen(string text)
    {
        var k = Karten.FirstOrDefault(x => !x.OhneRecht && x.Titel.Equals(text.Trim(), StringComparison.CurrentCultureIgnoreCase))
            ?? Suchen(text).FirstOrDefault();
        if (k is null) return false;
        GruppeWaehlen(k.Gruppe);
        foreach (var x in Karten) x.Hervorgehoben = x == k;
        rechts.ScrollControlIntoView(k);
        return true;
    }

    // ------------------------------------------------------------------ Anordnung

    private int S(int wert) => Stil.Px(this, wert);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breiteLinks = S(240);
        links.SetBounds(0, 0, breiteLinks, ClientSize.Height);
        rechts.SetBounds(breiteLinks, 0, Math.Max(100, ClientSize.Width - breiteLinks), ClientSize.Height);
    }

    private void LinksAnordnen()
    {
        int rand = S(Stil.AbstandGross), breite = Math.Max(100, links.ClientSize.Width - 2 * rand), y = rand;
        Suche.SetBounds(rand, y, breite, Suche.PreferredHeight);
        y = Suche.Bottom + S(Stil.AbstandKlein);
        if (trefferZeigen)
        {
            Treffer.SetBounds(rand, y, breite, Math.Min(S(240), Math.Max(1, Treffer.Items.Count) * Treffer.ItemHeight + S(6)));
            y = Treffer.Bottom + S(Stil.AbstandKlein);
        }
        foreach (var g in Gruppen.Where(g => GruppeSichtbar(g.Text)))
        {
            g.SetBounds(rand, y, breite, S(36));
            y += S(36) + S(Stil.AbstandMini);
        }
    }

    private void RechtsAnordnen()
    {
        int rand = S(Stil.AbstandGross), breite = Math.Max(100, Math.Min(rechts.ClientSize.Width - 2 * rand, S(900)));
        int y = rand + rechts.AutoScrollPosition.Y;
        titel.Location = new Point(rand, y);
        y = titel.Bottom + S(Stil.AbstandMittel);
        if (Fehler.Text.Length > 0 && fehlerZeigen)
        {
            Fehler.MaximumSize = new Size(breite, 0);
            Fehler.Location = new Point(rand, y);
            y = Fehler.Bottom + S(Stil.AbstandKlein);
        }
        foreach (var k in Karten.Where(k => k.Gruppe == AktiveGruppe && !k.OhneRecht))
        {
            k.SetBounds(rand, y, breite, k.Height);
            k.PerformLayout();
            y = k.Bottom + S(Stil.AbstandKlein);
        }
        if (Verzeichnis.Visible)
        {
            // Füllt die Fläche (ohne Bildlauf der Seite; die Liste rollt selbst).
            int breiteVoll = Math.Max(100, rechts.ClientSize.Width - 2 * rand);
            Verzeichnis.SetBounds(rand, y, breiteVoll, Math.Max(S(240), rechts.ClientSize.Height - y - rand));
            y = Verzeichnis.Bottom;
        }
        rechts.AutoScrollMinSize = new Size(0, y - rechts.AutoScrollPosition.Y + rand);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) logoBild?.Dispose();
        base.Dispose(disposing);
    }
}
