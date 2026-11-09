using DokuLine.Kern;

namespace DokuLine;

// Neue Fassung eines Reiters der Akte (Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger). Oben „gilt ab“ und
// „keine …“ (dritter Zustand), darunter die Felder des Reiters, vorbelegt mit der geltenden Fassung.
public sealed partial class AngabeFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly List<Control> felder = [];
    private Func<object> lesen = () => new object();

    public Angabe Art { get; }
    public TextBox Ab { get; } = new DatumFeld() { Width = 100 };
    public CheckBox Keine { get; } = new() { AutoSize = true };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public AngabeFassung? Gespeichert { get; private set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AngabeFenster(Speicher speicher, Bewohner bewohner, Angabe art)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        Art = art;
        Fenster.Grundform(this, $"{AngabenRegeln.Name(art)}: {bewohner.Name}");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        var heute = DateOnly.FromDateTime(Jetzt());
        Ab.Text = Datum.Text(heute);
        zeile("Gilt ab", Ab);
        Keine.Text = AngabenRegeln.KeineText(art);
        zeile(null, Keine);
        var bisher = AngabenRegeln.Aktuell(speicher.Fassungen(bewohner.Id!.Value, art), heute);
        Keine.Checked = bisher?.Zustand == AngabeZustand.Keine;
        void Feld(string beschriftung, Control c) { felder.Add(c); zeile(beschriftung, c); }
        switch (art)
        {
            case Angabe.Betreuung: BetreuungBauen(Feld, bisher?.Lesen<Betreuung>()); break;
            case Angabe.Angehoerige: AngehoerigeBauen(Feld, bisher?.Lesen<Angehoerige>()); break;
            case Angabe.Aerzte: AerzteBauen(Feld, bisher?.Lesen<AerzteAngabe>()); break;
            case Angabe.Krankenkasse: KrankenkasseBauen(Feld, bisher?.Lesen<Krankenkasse>()); break;
            case Angabe.Zustaendigkeit: ZustaendigkeitBauen(Feld, bisher?.Lesen<Zustaendigkeit>()); break;
            case Angabe.Bestellweg: BestellwegBauen(Feld, bisher?.Lesen<BestellwegAngabe>(), heute); break;
            case Angabe.Personenbeschreibung: BeschreibungBauen(Feld, bisher?.Lesen<Personenbeschreibung>()); break;
            case Angabe.Schweigepflicht: SchweigepflichtBauen(Feld, bisher?.Lesen<Schweigepflicht>(), heute); break;
            default: KostentraegerBauen(Feld, bisher?.Lesen<Kostentraeger>()); break;
        }
        Keine.CheckedChanged += (_, _) => FelderSperren();
        FelderSperren();
        zeile(null, Fenster.Hinweis("Jede Änderung ist eine neue Fassung; die bisherige bleibt im Verlauf. Alle Namen und Nummern sind ausgedacht."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private void FelderSperren()
    {
        foreach (var c in felder) c.Enabled = !Keine.Checked;
    }

    public bool Speichern()
    {
        try
        {
            DateOnly ab;
            try { ab = Datum.Lesen(Ab.Text, "Gilt ab"); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            Gespeichert = speicher.AngabeSpeichern(bewohner.Id!.Value, Art, Keine.Checked ? null : lesen(), ab, Recht.Kuerzel, Jetzt());
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    // ----- Bausteine

    private static DateOnly? DatumOderLeer(TextBox feld, string name)
    {
        if (feld.Text.Trim().Length == 0) return null;
        try { return Datum.Lesen(feld.Text, name); }
        catch (FormatException x) { throw new Eingabefehler([x.Message]); }
    }

    private static TextBox Feld(string wert, int breite = 260) => new() { Text = wert, Width = breite };
    private static TextBox DatumsFeld(DateOnly? wert) => new DatumFeld() { Width = 100, Text = wert is DateOnly d ? Datum.Text(d) : "", PlaceholderText = "TT.MM.JJJJ" };

    // Auswahl einer Person aus einem Verzeichnis, mit „(keine Auswahl)“ vorn.
    private sealed record Wahl(long? Id, string Text)
    {
        public override string ToString() => Text;
    }

    private ComboBox Personenwahl(PersonGruppe gruppe, long? gewaehlt, string leer = "(keine Auswahl)", IEnumerable<Person>? reihenfolge = null)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320, AccessibleName = PersonRegeln.GruppeText(gruppe) };
        box.Items.Add(new Wahl(null, leer));
        foreach (var p in reihenfolge ?? speicher.Personen(gruppe, nurAktive: true)) box.Items.Add(new Wahl(p.Id, p.Anzeige));
        if (gewaehlt is long id && box.Items.Cast<Wahl>().All(w => w.Id != id) && speicher.PersonNach(id) is Person alt)
            box.Items.Add(new Wahl(alt.Id, alt.Anzeige));   // stillgelegt, aber noch zugeteilt
        box.SelectedIndex = Math.Max(0, box.Items.Cast<Wahl>().ToList().FindIndex(w => w.Id == gewaehlt));
        return box;
    }

    private static long? Gewaehlt(ComboBox box) => (box.SelectedItem as Wahl)?.Id;

    // „Person anlegen …“ direkt aus der Auswahl; die neue Person ist danach gewählt.
    private Button AnlegenKnopf(PersonGruppe gruppe, ComboBox box)
    {
        var knopf = Stil.KleinerKnopf($"{PersonRegeln.EinzahlText(gruppe)} anlegen …");
        Stil.Sperren(knopf, speicher.PersonGrund(Recht.Kuerzel, gruppe));
        knopf.Click += (_, _) =>
        {
            using var f = new PersonFenster(speicher, gruppe, null);
            if (Dialog.Zeigen(f, this) != DialogResult.OK || f.Gespeichert is not Person neu) return;
            box.Items.Add(new Wahl(neu.Id, neu.Anzeige));
            box.SelectedIndex = box.Items.Count - 1;
        };
        return knopf;
    }

    private ComboBox Liste(Liste art, string wert, bool frei = false)
    {
        var box = new ComboBox { DropDownStyle = frei ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList, Width = 260 };
        var namen = speicher.Listeneintraege(art).Where(e => e.Aktiv).Select(e => e.Name).ToList();
        if (wert.Length > 0 && !namen.Contains(wert)) namen.Add(wert);
        box.Items.AddRange([.. namen]);
        if (frei) box.Text = wert;
        else box.SelectedIndex = namen.IndexOf(wert);
        return box;
    }

    // ----- Betreuung

    public ComboBox BetreuerWahl { get; private set; } = null!;
    public CheckedListBox Aufgaben { get; private set; } = null!;

    private void BetreuungBauen(Action<string, Control> feld, Betreuung? b)
    {
        var art = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        art.Items.AddRange([.. AngabenRegeln.Betreuungsarten]);
        art.SelectedIndex = Math.Max(0, Array.IndexOf(AngabenRegeln.Betreuungsarten, b?.Art ?? ""));
        BetreuerWahl = Personenwahl(PersonGruppe.Betreuer, b?.PersonId);
        Aufgaben = new CheckedListBox { Width = 320, Height = 130, CheckOnClick = true, IntegralHeight = false };
        var namen = speicher.Listeneintraege(Kern.Liste.Betreuungsaufgaben).Where(e => e.Aktiv).Select(e => e.Name).ToList();
        foreach (var a in b?.Aufgaben ?? []) if (!namen.Contains(a)) namen.Add(a);
        foreach (var n in namen) Aufgaben.Items.Add(n, b?.Aufgaben.Contains(n) == true);
        var freitext = Feld(b?.AufgabenText ?? "", 320);
        var bestellt = DatumsFeld(b?.BestelltAm);
        var bis = DatumsFeld(b?.UeberpruefungBis);
        var ersatz = Personenwahl(PersonGruppe.Betreuer, b?.ErsatzId, "(keiner)");
        feld("Art", art);
        feld("Betreuer", Formular.Reihe(BetreuerWahl, AnlegenKnopf(PersonGruppe.Betreuer, BetreuerWahl)));
        feld("Aufgabenbereiche", Aufgaben);
        feld("Weitere Aufgaben", freitext);
        feld("Bestellt am", bestellt);
        feld("Überprüfung bis", bis);
        feld("Ersatzbetreuer", ersatz);
        lesen = () => new Betreuung
        {
            Art = art.SelectedItem as string ?? "", PersonId = Gewaehlt(BetreuerWahl), Aufgaben = [.. Aufgaben.CheckedItems.Cast<string>()],
            AufgabenText = freitext.Text.Trim(), BestelltAm = DatumOderLeer(bestellt, "Bestellt am"), UeberpruefungBis = DatumOderLeer(bis, "Überprüfung bis"),
            ErsatzId = Gewaehlt(ersatz),
        };
    }

    // ----- Krankenkasse

    public ComboBox Kasse { get; private set; } = null!;
    public ComboBox Befreiung { get; private set; } = null!;
    public ComboBox Pflegegrad { get; private set; } = null!;

    private void KrankenkasseBauen(Action<string, Control> feld, Krankenkasse? k)
    {
        Kasse = Liste(Kern.Liste.Krankenkassen, k?.Kasse ?? "");
        var nummer = Feld(k?.Nummer ?? "", 160);
        var status = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 200, Text = k?.Status ?? "" };
        status.Items.AddRange([.. AngabenRegeln.Versichertenstatus]);
        Befreiung = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        Befreiung.Items.AddRange([AngabenRegeln.NichtErfasst, "ja", "nein"]);
        Befreiung.SelectedIndex = (int)(k?.Befreiung ?? JaNein.NichtErfasst);
        var befreitBis = DatumsFeld(k?.BefreitBis);
        Pflegegrad = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        Pflegegrad.Items.AddRange([AngabenRegeln.NichtErfasst, "keiner", "1", "2", "3", "4", "5"]);
        Pflegegrad.SelectedIndex = k?.Pflegegrad is int g ? g + 1 : 0;
        var ansprech = Feld(k?.Ansprechpartner ?? "");
        var telefon = Feld(k?.Telefon ?? "", 160);
        feld("Krankenkasse", Kasse);
        feld("Versichertennummer", nummer);
        feld("Status", status);
        feld("Zuzahlungsbefreiung", Formular.Reihe(Befreiung, new Label { Text = "gültig bis", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, befreitBis));
        feld("Pflegegrad", Pflegegrad);
        feld("Ansprechpartner", ansprech);
        feld("Telefon der Kasse", telefon);
        lesen = () => new Krankenkasse
        {
            Kasse = Kasse.SelectedItem as string ?? "", Nummer = nummer.Text.Trim(), Status = status.Text.Trim(), Befreiung = (JaNein)Befreiung.SelectedIndex,
            BefreitBis = Befreiung.SelectedIndex == (int)JaNein.Ja ? DatumOderLeer(befreitBis, "Befreiung gültig bis") : null,
            Pflegegrad = Pflegegrad.SelectedIndex == 0 ? null : Pflegegrad.SelectedIndex - 1, Ansprechpartner = ansprech.Text.Trim(), Telefon = telefon.Text.Trim(),
        };
    }
}
