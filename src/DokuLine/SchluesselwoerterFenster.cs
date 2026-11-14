using DokuLine.Kern;

namespace DokuLine;

// Einstellungen → „Schlüsselwörter“ (Bauplan 48): links die nächsten Schritte, rechts deren Wörter mit ein/aus, Art des
// Treffers und Schritt; Wort hinzufügen, entfernen (mitgelieferte nur aus), Werkseinstellung je Schritt. Darunter ein
// Probefeld. Ändern mit Recht „Schlüsselwörter verwalten“, ansehen alle; „für mich keine Vorschläge“ je Mitarbeiter.
public sealed class SchluesselwoerterFenster : Form
{
    private readonly Speicher speicher;
    private List<Schluesselwort> woerter = [];

    public ListBox Schritte { get; } = new() { Width = 260, Height = 300, AccessibleName = "Nächste Schritte" };
    public ListView Woerter { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Width = 470, Height = 300, AccessibleName = "Wörter",
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
    };
    public Button HinzuKnopf { get; } = Stil.NebenKnopf("Wort hinzufügen …");
    public Button AnAusKnopf { get; } = Stil.NebenKnopf("Ein/aus");
    public Button ArtKnopf { get; } = Stil.NebenKnopf("Art ändern");
    public Button SchrittKnopf { get; } = Stil.NebenKnopf("Schritt ändern …");
    public Button EntfernenKnopf { get; } = Stil.NebenKnopf("Entfernen");
    public Button WerkKnopf { get; } = Stil.NebenKnopf("Werkseinstellung");
    public TextBox Probe { get; } = new() { Width = 740, PlaceholderText = "Probefeld: Satz eintippen, z. B. „Arzttermin am 14.11. um 10:30“", AccessibleName = "Probefeld" };
    public Label ProbeErgebnis { get; } = new() { AutoSize = true, MaximumSize = new Size(740, 0), ForeColor = Stil.TextMittel, UseMnemonic = false };
    public CheckBox KeineVorschlaege { get; } = new() { Text = "Für mich keine Vorschläge anzeigen", AutoSize = true };
    internal Func<string?> WortFragen { get; set; }
    internal Func<IReadOnlyList<string>, int?> SchrittWaehlen { get; set; }

    public SchluesselwoerterFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Schlüsselwörter");
        WortFragen = () => Eingabe.Fragen(this, "Wort hinzufügen", "Wort oder Wortgruppe (Groß-/Kleinschreibung egal)");
        SchrittWaehlen = namen => Eingabe.Waehlen(this, "Schritt ändern", "Welchen Schritt soll das Wort anbieten?", namen);
        foreach (var (titel, breite) in new[] { ("Wort", 190), ("Art", 110), ("an", 50), ("Herkunft", 100) }) Woerter.Columns.Add(titel, breite);
        Schritte.Items.AddRange([.. Enum.GetValues<Folgeschritt>().Select(SchluesselwortRegeln.Name)]);
        Schritte.SelectedIndexChanged += (_, _) => WoerterZeigen();
        Woerter.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        HinzuKnopf.Click += (_, _) => Hinzufuegen();
        AnAusKnopf.Click += (_, _) => Aendern(w => w with { Aktiv = !w.Aktiv });
        ArtKnopf.Click += (_, _) => Aendern(w => w with { Art = (Treffer)(((int)w.Art + 1) % 3) });
        SchrittKnopf.Click += (_, _) => SchrittAendern();
        EntfernenKnopf.Click += (_, _) => Entfernen();
        WerkKnopf.Click += (_, _) => Werkseinstellung();
        Probe.TextChanged += (_, _) => ProbeZeigen();
        KeineVorschlaege.Checked = speicher.VorschlaegeAus(Recht.Kuerzel);
        KeineVorschlaege.Enabled = Recht.Angemeldet;
        KeineVorschlaege.CheckedChanged += (_, _) => speicher.VorschlaegeAusSetzen(Recht.Kuerzel, KeineVorschlaege.Checked);
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, Fenster.Hinweis("Das Programm bietet nur an; es legt nie selbst etwas an und ändert keinen Text. Auch „kein Arzttermin nötig“ löst ein Angebot aus."));
        zeile(null, Formular.Reihe(Schritte, Woerter));
        zeile(null, Formular.Reihe(HinzuKnopf, AnAusKnopf, ArtKnopf, SchrittKnopf, EntfernenKnopf, WerkKnopf));
        zeile(null, Probe);
        zeile(null, ProbeErgebnis);
        zeile(null, KeineVorschlaege);
        Controls.Add(raster);
        Laden();
        Schritte.SelectedIndex = 0;
        Stil.DpiSkalierungEinschalten(this);
    }

    private Folgeschritt Schritt => (Folgeschritt)Math.Max(0, Schritte.SelectedIndex);

    public Schluesselwort? Gewaehlt => Woerter.SelectedItems.Count == 1 ? Woerter.SelectedItems[0].Tag as Schluesselwort : null;

    private void Laden() => woerter = speicher.Schluesselwoerter();

    public void WoerterZeigen()
    {
        Woerter.BeginUpdate();
        Woerter.Items.Clear();
        foreach (var w in woerter.Where(w => w.Folgeschritt == Schritt))
            Woerter.Items.Add(new ListViewItem([w.Wort, SchluesselwortRegeln.ArtText(w.Art), w.Aktiv ? "ja" : "aus", w.Mitgeliefert ? "mitgeliefert" : "eigenes"])
            { Tag = w, ForeColor = w.Aktiv ? Stil.TextNormal : Stil.TextLeise });
        Woerter.EndUpdate();
        KnoepfeSetzen();
        ProbeZeigen();
    }

    private void KnoepfeSetzen()
    {
        var alle = new[] { HinzuKnopf, AnAusKnopf, ArtKnopf, SchrittKnopf, EntfernenKnopf, WerkKnopf };
        Recht.Anwenden(speicher, Funktionen.SchluesselwoerterVerwalten, alle);
        if (Recht.Grund(speicher, Funktionen.SchluesselwoerterVerwalten) is not null) return;
        string? ohne = Gewaehlt is null ? "Bitte zuerst ein Wort wählen." : null;
        foreach (var k in new[] { AnAusKnopf, ArtKnopf, SchrittKnopf, EntfernenKnopf }) Stil.Sperren(k, ohne);
    }

    private bool Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e)
        {
            MessageBox.Show(this, string.Join("\n", e.Fehler), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        long? vorher = Gewaehlt?.Id;
        Laden();
        WoerterZeigen();
        if (Woerter.Items.Cast<ListViewItem>().FirstOrDefault(i => (i.Tag as Schluesselwort)?.Id == vorher) is ListViewItem zeile) zeile.Selected = true;
        return true;
    }

    public bool Hinzufuegen()
    {
        if (Stil.Sperrgrund(HinzuKnopf) is not null || WortFragen() is not string wort || wort.Trim().Length == 0) return false;
        var art = wort.Trim().Contains(' ') ? Treffer.Wortgruppe : Treffer.Wortanfang;
        return Ausfuehren(() => speicher.WortHinzufuegen(wort, Schritt, art, Recht.Kuerzel));
    }

    public bool Aendern(Func<Schluesselwort, Schluesselwort> neu)
    {
        if (Gewaehlt is not Schluesselwort w || Stil.Sperrgrund(AnAusKnopf) is not null) return false;
        var n = neu(w);
        return Ausfuehren(() => speicher.WortAendern(w.Id, n.Aktiv, n.Folgeschritt, n.Art, Recht.Kuerzel));
    }

    public bool SchrittAendern()
    {
        if (Gewaehlt is not Schluesselwort w || Stil.Sperrgrund(SchrittKnopf) is not null) return false;
        var namen = Enum.GetValues<Folgeschritt>().Select(SchluesselwortRegeln.Name).ToList();
        if (SchrittWaehlen(namen) is not int i) return false;
        return Ausfuehren(() => speicher.WortAendern(w.Id, w.Aktiv, (Folgeschritt)i, w.Art, Recht.Kuerzel));
    }

    public bool Entfernen()
    {
        if (Gewaehlt is not Schluesselwort w || Stil.Sperrgrund(EntfernenKnopf) is not null) return false;
        return Ausfuehren(() => speicher.WortEntfernen(w.Id, Recht.Kuerzel));
    }

    public bool Werkseinstellung()
    {
        if (Stil.Sperrgrund(WerkKnopf) is not null) return false;
        return Ausfuehren(() => speicher.Werkseinstellung(Schritt, Recht.Kuerzel));
    }

    public void ProbeZeigen()
    {
        if (Probe.Text.Trim().Length == 0) { ProbeErgebnis.Text = ""; return; }
        var treffer = woerter.Where(w => w.Aktiv && SchluesselwortRegeln.Trifft(Probe.Text, w.Wort, w.Art)).Select(w => $"„{w.Wort}“").ToList();
        var angebote = SchluesselwortRegeln.Finden(Probe.Text, woerter, new HashSet<Folgeschritt>());
        ProbeErgebnis.Text = treffer.Count == 0 ? "Kein Wort trifft." :
            $"Treffer: {string.Join(", ", treffer)}. Angebot: {string.Join(" · ", angebote.Select(a => SchluesselwortRegeln.Name(a.Folgeschritt)))}" +
            (angebote.FirstOrDefault() is { Datum: not null } or { Uhrzeit: not null } ? $" (erkannt: {Datum.Text(angebote[0].Datum)} {angebote[0].Uhrzeit:HH\\:mm})".Replace("  ", " ") : "");
    }
}
