using DokuLine.Kern;

namespace DokuLine;

// Personenverzeichnis (Bauplan Schritt 29a) in den Einstellungen, Gruppe „Verzeichnisse“: oben die Verzeichnisse
// mit Zähler (Ärzte · Betreuer · Angehörige · Forensik · FOSA · Sonstige Stellen), darunter die Suche und die Liste
// des gewählten Verzeichnisses mit eigenen Spalten. Doppelklick öffnet die Person; dort „zugeordnet zu …“ mit
// Sprung in die Akte. Ansehen dürfen alle; ändern nur mit Recht (Angehörige: „Bewohner anlegen und bearbeiten“,
// sonst „Personenverzeichnis verwalten“). Gelöscht wird nicht, nur inaktiv gesetzt.
public sealed class VerzeichnisAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel gruppenLeiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite, Margin = new Padding(0) };
    private List<Person> gezeigt = [];

    public Dictionary<PersonGruppe, FilterKnopf> GruppenKnoepfe { get; } = [];
    public PersonGruppe Gruppe { get; private set; } = PersonGruppe.Arzt;
    public TextBox Suche { get; } = new() { Width = 260, PlaceholderText = "Name, Stelle oder Telefon suchen", Font = Stil.Standard, AccessibleName = "Im Verzeichnis suchen" };
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Font = Stil.Standard, AccessibleName = "Personen",
    };
    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neu …");
    public Button AendernKnopf { get; } = Stil.NebenKnopf("Öffnen …");
    public Button UmschaltenKnopf { get; } = Stil.NebenKnopf("Inaktiv / wieder aktiv");
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Button[] Knoepfe => [NeuKnopf, AendernKnopf, UmschaltenKnopf];
    public event Action<long>? AkteGewuenscht;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public VerzeichnisAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var g in PersonRegeln.Gruppen)
        {
            var knopf = new FilterKnopf(PersonRegeln.GruppeText(g)) { AccessibleName = PersonRegeln.GruppeText(g) };
            knopf.Click += (_, _) => GruppeWaehlen(g);
            GruppenKnoepfe[g] = knopf;
            gruppenLeiste.Controls.Add(knopf);
        }
        Suche.TextChanged += (_, _) => Fuellen();
        Liste.DoubleClick += (_, _) => Oeffnen();
        Liste.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; Oeffnen(); } };
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSperren();
        NeuKnopf.Click += (_, _) => Neu();
        AendernKnopf.Click += (_, _) => Oeffnen();
        UmschaltenKnopf.Click += (_, _) => Umschalten();
        Controls.AddRange([gruppenLeiste, Suche, Fehler, Liste]);
        GruppeWaehlen(PersonGruppe.Arzt);
    }

    public string KopfText => $"Verzeichnisse · {PersonRegeln.GruppeText(Gruppe)} · {gezeigt.Count} Einträge";

    public void GruppeWaehlen(PersonGruppe g)
    {
        Gruppe = g;
        foreach (var (gruppe, knopf) in GruppenKnoepfe) knopf.Aktiv = gruppe == g;
        NeuKnopf.Text = g switch
        {
            PersonGruppe.Arzt => "Neuer Arzt …",
            PersonGruppe.Betreuer => "Neuer Betreuer …",
            PersonGruppe.Angehoerige => "Neue angehörige Person …",
            PersonGruppe.Sonstige => "Neue Stelle …",
            _ => "Neue zuständige Person …",
        };
        Liste.BeginUpdate();
        Liste.Columns.Clear();
        foreach (var titel in PersonRegeln.Spalten(g))
            Liste.Columns.Add(titel, titel switch { "Name" => 200, "zugeordnet zu" => 280, "Bewohner" or "Status" => 70, "E-Mail" => 170, _ => 140 });
        Liste.EndUpdate();
        Fehler.Visible = false;
        Aktualisieren();
    }

    // Zähler je Verzeichnis und die Liste neu lesen.
    public void Aktualisieren()
    {
        var alle = speicher.Personen();
        foreach (var (g, knopf) in GruppenKnoepfe) knopf.Anzahl = alle.Count(p => p.Gruppe == g && p.Aktiv);
        Fuellen(alle);
    }

    private void Fuellen() => Fuellen(speicher.Personen());

    private void Fuellen(List<Person> alle)
    {
        long? vorher = Gewaehlt?.Id;
        var zuordnungen = speicher.Zuordnungen();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        gezeigt = [.. alle.Where(p => p.Gruppe == Gruppe && PersonRegeln.Passt(p, Suche.Text)).OrderBy(p => p.Aktiv ? 0 : 1).ThenBy(p => p.Name, StringComparer.CurrentCulture)];
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var p in gezeigt)
            Liste.Items.Add(new ListViewItem(PersonRegeln.Zeile(p, zuordnungen, namen)) { ForeColor = p.Aktiv ? Stil.TextNormal : Stil.TextLeise });
        Liste.EndUpdate();
        int i = gezeigt.FindIndex(p => p.Id == vorher);
        if (i >= 0) Liste.Items[i].Selected = true;
        KnoepfeSperren();
        Geaendert?.Invoke();
    }

    public Person? Gewaehlt => Liste.SelectedIndices.Count == 1 ? gezeigt[Liste.SelectedIndices[0]] : null;

    public bool Waehlen(string name)
    {
        int i = gezeigt.FindIndex(p => p.Name == name);
        if (i < 0) return false;
        Liste.SelectedIndices.Clear();
        Liste.Items[i].Selected = true;
        KnoepfeSperren();
        return true;
    }

    private void KnoepfeSperren()
    {
        string? grund = speicher.PersonGrund(Recht.Kuerzel, Gruppe);
        Stil.Sperren(NeuKnopf, grund);
        Stil.Sperren(AendernKnopf, Gewaehlt is null ? "Bitte zuerst eine Person in der Liste wählen." : null);   // ansehen dürfen alle
        Stil.Sperren(UmschaltenKnopf, grund ?? (Gewaehlt is null ? "Bitte zuerst eine Person in der Liste wählen." : null));
    }

    public bool Neu()
    {
        if (speicher.PersonGrund(Recht.Kuerzel, Gruppe) is not null) return false;
        using var f = new PersonFenster(speicher, Gruppe, null);
        bool ok = Dialog.Zeigen(f, FindForm()) == DialogResult.OK;
        Aktualisieren();
        if (f.Gespeichert is Person neu) Waehlen(neu.Name);
        return ok;
    }

    // Öffnen: ansehen (ohne Recht nur lesen) oder ändern; „Akte öffnen“ aus der Zuordnung springt in die Akte.
    public bool Oeffnen()
    {
        if (Gewaehlt is not Person p) return false;
        using var f = new PersonFenster(speicher, p.Gruppe, p);
        bool ok = Dialog.Zeigen(f, FindForm()) == DialogResult.OK;
        Aktualisieren();
        if (f.AkteFuer is long id) AkteGewuenscht?.Invoke(id);
        return ok;
    }

    public bool Umschalten()
    {
        if (Gewaehlt is not Person p) return false;
        try { speicher.PersonAktiv(p.Id, !p.Aktiv, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; PerformLayout(); return false; }
        Fehler.Visible = false;
        Aktualisieren();
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = ClientSize.Width, klein = Stil.Px(this, Stil.AbstandKlein);
        gruppenLeiste.MaximumSize = new Size(breite, 0);
        gruppenLeiste.Location = Point.Empty;
        Suche.Location = new Point(0, gruppenLeiste.Bottom + klein);
        int y = Suche.Bottom + klein;
        if (Fehler.Visible)
        {
            Fehler.MaximumSize = new Size(breite, 0);
            Fehler.Location = new Point(0, y);
            y = Fehler.Bottom + klein;
        }
        Liste.SetBounds(0, y, breite, Math.Max(Stil.Px(this, 120), ClientSize.Height - y));
    }
}

// Eine Person anlegen, ansehen oder ändern; je Verzeichnis die passenden Felder. Darunter „zugeordnet zu …“: die
// Bewohner mit Rolle, „Bewohner zuordnen …“, „Zuordnung entfernen“ und „Akte öffnen“.
public sealed class PersonFenster : Form
{
    private readonly Speicher speicher;
    private readonly PersonGruppe gruppe;
    private Person? bisher;
    private List<PersonZuordnung> zuordnungen = [];
    private readonly Dictionary<long, string> namen;

    public TextBox NameFeld { get; } = new() { Width = 320, PlaceholderText = "ausgedacht" };
    public ComboBox Art { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 220 };
    public ComboBox Funktion { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 320 };
    public TextBox Stelle { get; } = new() { Width = 320 };
    public TextBox Telefon { get; } = new() { Width = 200 };
    public TextBox Mobil { get; } = new() { Width = 200 };
    public TextBox Email { get; } = new() { Width = 320, PlaceholderText = "name@example.org (Demo)" };
    public TextBox Anschrift { get; } = new() { Width = 320 };
    public TextBox Erreichbarkeit { get; } = new() { Width = 320, PlaceholderText = "z. B. Mo–Fr 8–12 Uhr" };
    public TextBox Hinweis { get; } = new() { Width = 320 };
    public ListBox Zugeordnet { get; } = new() { Width = 420, Height = 110, IntegralHeight = false, AccessibleName = "zugeordnet zu" };
    public Button ZuordnenKnopf { get; } = Stil.NebenKnopf("Bewohner zuordnen …");
    public Button EntfernenKnopf { get; } = Stil.NebenKnopf("Zuordnung entfernen");
    public Button AkteKnopf { get; } = Stil.NebenKnopf("Akte öffnen");
    public Button SpeichernKnopf { get; }
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Person? Gespeichert { get; private set; }
    public long? AkteFuer { get; private set; }
    public string? Sperrgrund { get; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public PersonFenster(Speicher speicher, PersonGruppe gruppe, Person? bisher)
    {
        this.speicher = speicher;
        this.gruppe = gruppe;
        this.bisher = bisher;
        namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        string einzahl = PersonRegeln.EinzahlText(gruppe);
        Fenster.Grundform(this, bisher is null ? $"{einzahl} anlegen" : $"{einzahl} – {bisher.Name}");
        Sperrgrund = speicher.PersonGrund(Recht.Kuerzel, gruppe);
        Art.Items.AddRange([.. PersonRegeln.BetreuerArten]);
        if (gruppe is PersonGruppe.Forensik or PersonGruppe.Fosa) Funktion.Items.AddRange([.. PersonRegeln.FunktionAuswahl]);
        if (bisher is not null)
        {
            var e = PersonRegeln.AlsEingabe(bisher);
            (NameFeld.Text, Art.Text, Funktion.Text, Stelle.Text, Telefon.Text, Mobil.Text) = (e.Name, e.Art, e.Fachrichtung, e.Praxis, e.Telefon, e.Mobil);
            (Email.Text, Anschrift.Text, Erreichbarkeit.Text, Hinweis.Text) = (e.Email, e.Anschrift, e.Erreichbarkeit, e.Hinweis);
        }
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Verzeichnis", new Label { Text = PersonRegeln.GruppeText(gruppe), AutoSize = true, Font = Stil.Fett, UseMnemonic = false });
        zeile("Name", NameFeld);
        if (gruppe == PersonGruppe.Betreuer) zeile("Art", Art);
        if (PersonRegeln.FunktionText(gruppe) is string funktion) zeile(funktion, Funktion);
        if (PersonRegeln.StelleText(gruppe) is string stelle) zeile(stelle, Stelle);
        zeile("Telefon", Telefon);
        zeile("Mobil", Mobil);
        zeile("E-Mail", Email);
        zeile("Anschrift", Anschrift);
        zeile("Erreichbarkeit", Erreichbarkeit);
        zeile("Hinweis", Hinweis);
        zeile(null, Fenster.Hinweis("Nur ausgedachte Angaben; E-Mail-Adressen in der Demo bitte mit der Endung @example.org."));
        zeile("zugeordnet zu", Zugeordnet);
        foreach (var k in new[] { ZuordnenKnopf, EntfernenKnopf, AkteKnopf }) k.AutoSize = true;
        zeile(null, Formular.Reihe(ZuordnenKnopf, EntfernenKnopf, AkteKnopf));
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Speichern", () => Speichern());
        SpeichernKnopf = (Button)knoepfe.Controls[0];
        zeile(null, knoepfe);
        Controls.Add(raster);
        ZuordnenKnopf.Click += (_, _) => Zuordnen();
        EntfernenKnopf.Click += (_, _) => Entfernen();
        AkteKnopf.Click += (_, _) => AkteOeffnen();
        Zugeordnet.DoubleClick += (_, _) => AkteOeffnen();
        Zugeordnet.SelectedIndexChanged += (_, _) => Sperren();
        // Ohne Recht nur ansehen.
        if (Sperrgrund is not null)
            foreach (var c in new Control[] { NameFeld, Art, Funktion, Stelle, Telefon, Mobil, Email, Anschrift, Erreichbarkeit, Hinweis })
                if (c is TextBox t) t.ReadOnly = true; else c.Enabled = false;
        ZuordnungenFuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void ZuordnungenFuellen()
    {
        zuordnungen = bisher is null ? [] : speicher.Zuordnungen(personId: bisher.Id);
        Zugeordnet.Items.Clear();
        foreach (var z in zuordnungen)
            Zugeordnet.Items.Add(namen.GetValueOrDefault(z.BewohnerId, "?") + (z.Rolle.Length > 0 ? $" ({z.Rolle})" : ""));
        if (zuordnungen.Count == 0) Zugeordnet.Items.Add(bisher is null ? "(erst speichern, dann zuordnen)" : "(noch niemand)");
        Sperren();
    }

    private PersonZuordnung? GewaehlteZuordnung =>
        Zugeordnet.SelectedIndex >= 0 && Zugeordnet.SelectedIndex < zuordnungen.Count ? zuordnungen[Zugeordnet.SelectedIndex] : null;

    private void Sperren()
    {
        Stil.Sperren(SpeichernKnopf, Sperrgrund);
        Stil.Sperren(ZuordnenKnopf, Sperrgrund ?? (bisher is null ? "Bitte zuerst speichern." : null));
        Stil.Sperren(EntfernenKnopf, Sperrgrund ?? (GewaehlteZuordnung is null ? "Bitte zuerst einen Bewohner in der Liste wählen." : null));
        Stil.Sperren(AkteKnopf, GewaehlteZuordnung is null ? "Bitte zuerst einen Bewohner in der Liste wählen." : null);
    }

    public bool ZuordnungWaehlen(int index)
    {
        if (index < 0 || index >= zuordnungen.Count) return false;
        Zugeordnet.SelectedIndex = index;
        Sperren();
        return true;
    }

    public PersonEingabe Eingabe() => new()
    {
        Gruppe = gruppe, Name = NameFeld.Text, Art = Art.Text, Fachrichtung = Funktion.Text, Praxis = Stelle.Text, Telefon = Telefon.Text,
        Mobil = Mobil.Text, Email = Email.Text, Anschrift = Anschrift.Text, Erreichbarkeit = Erreichbarkeit.Text, Hinweis = Hinweis.Text,
    };

    public bool Speichern()
    {
        if (Sperrgrund is not null) return false;
        try
        {
            var p = PersonRegeln.AusEingabe(Eingabe(), bisher?.Id ?? 0, bisher?.Aktiv ?? true);
            Gespeichert = speicher.PersonSpeichern(p, Recht.Kuerzel, Jetzt());
        }
        catch (Eingabefehler e) { return FehlerZeigen(e); }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    // Bewohner zuordnen: erst speichern (neue Person), dann wählen. Wählen ersetzbar (Test).
    internal Func<PersonFenster, (long BewohnerId, string Rolle)?> ZuordnungAuswahl { get; set; } = fenster =>
    {
        using var w = new ZuordnungFenster(fenster.speicher, fenster.bisher!);
        return Dialog.Zeigen(w, fenster) == DialogResult.OK && w.Gewaehlt is long id ? (id, w.Rolle.Text.Trim()) : null;
    };

    public bool Zuordnen()
    {
        if (Sperrgrund is not null || bisher is null) return false;
        if (ZuordnungAuswahl(this) is not (long id, string rolle)) return false;
        try { speicher.ZuordnungSpeichern(bisher.Id, id, rolle, Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { return FehlerZeigen(e); }
        ZuordnungenFuellen();
        return true;
    }

    public bool Entfernen()
    {
        if (Sperrgrund is not null || GewaehlteZuordnung is not PersonZuordnung z) return false;
        try { speicher.ZuordnungEntfernen(z.Id, Recht.Kuerzel); }
        catch (Eingabefehler e) { return FehlerZeigen(e); }
        ZuordnungenFuellen();
        return true;
    }

    public bool AkteOeffnen()
    {
        if (GewaehlteZuordnung is not PersonZuordnung z) return false;
        AkteFuer = z.BewohnerId;
        DialogResult = DialogResult.Cancel;
        Close();
        return true;
    }

    private bool FehlerZeigen(Eingabefehler e)
    {
        Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
        Fehler.Visible = true;
        return false;
    }
}

// Einen Bewohner für eine Person wählen, mit Rolle (Vorschläge je Verzeichnis).
public sealed class ZuordnungFenster : Form
{
    private readonly List<Bewohner> bewohner;

    public ComboBox BewohnerWahl { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320, AccessibleName = "Bewohner" };
    public ComboBox Rolle { get; } = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 320, AccessibleName = "Rolle" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public long? Gewaehlt => BewohnerWahl.SelectedIndex >= 0 ? bewohner[BewohnerWahl.SelectedIndex].Id : null;

    public ZuordnungFenster(Speicher speicher, Person person)
    {
        Fenster.Grundform(this, $"{person.Name} zuordnen");
        bewohner = [.. speicher.AlleBewohner().OrderBy(b => b.Nachname, StringComparer.CurrentCulture).ThenBy(b => b.Vorname, StringComparer.CurrentCulture)];
        BewohnerWahl.Items.AddRange([.. bewohner.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer}, {b.Wohnbereich})")]);
        Rolle.Items.AddRange([.. PersonRegeln.RolleAuswahl(person.Gruppe)]);
        Rolle.Text = PersonRegeln.RolleVorschlag(person);
        var raster = Formular.Raster(out var zeile);
        zeile("Bewohner", BewohnerWahl);
        zeile(person.Gruppe == PersonGruppe.Angehoerige ? "Beziehung" : "Rolle", Rolle);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Zuordnen", () => Uebernehmen()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool BewohnerWaehlen(long id)
    {
        int i = bewohner.FindIndex(b => b.Id == id);
        if (i < 0) return false;
        BewohnerWahl.SelectedIndex = i;
        return true;
    }

    public bool Uebernehmen()
    {
        if (Gewaehlt is null)
        {
            Fehler.Text = "Bitte einen Bewohner wählen.";
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
