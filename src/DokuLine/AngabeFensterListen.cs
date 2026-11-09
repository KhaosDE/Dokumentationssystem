using DokuLine.Kern;

namespace DokuLine;

// Die Reiter mit Listen: Angehörige, Ärzte (weitere), Kostenträger (Bewilligungen). Je Liste eine Zeile zum Hinzufügen
// und „Entfernen“ für die gewählte Zeile.
public sealed partial class AngabeFenster
{
    private static ListBox Liste() => new() { Width = 520, Height = 110, IntegralHeight = false, Font = Stil.Standard };

    private static Button Knopf(string text, Action aktion)
    {
        var k = Stil.KleinerKnopf(text);
        k.Click += (_, _) => aktion();
        return k;
    }

    private void Fehlermeldung(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = true;
    }

    // ----- Angehörige

    public List<Angehoeriger> AngehoerigeListe { get; } = [];

    private void AngehoerigeBauen(Action<string, Control> feld, Angehoerige? a)
    {
        var personen = speicher.Personen(PersonGruppe.Angehoerige).ToDictionary(p => p.Id);
        string Text(Angehoeriger x) => $"{(personen.TryGetValue(x.PersonId, out var p) ? p.Name : "?")} ({x.Beziehung})" +
                                        (x.Hauptkontakt ? " · Hauptkontakt" : "") + (x.Notfall ? " · Notfall" : "") + (x.Hinweis.Length > 0 ? $" · {x.Hinweis}" : "");
        AngehoerigeListe.AddRange(a?.Personen ?? []);
        var liste = Liste();
        void Fuellen() { liste.Items.Clear(); liste.Items.AddRange([.. AngehoerigeListe.Select(Text)]); }
        Fuellen();
        var wahl = Personenwahl(PersonGruppe.Angehoerige, null);
        var beziehung = new TextBox { Width = 140, PlaceholderText = "z. B. Mutter" };
        var haupt = new CheckBox { Text = "Hauptkontakt", AutoSize = true };
        var notfall = new CheckBox { Text = "im Notfall benachrichtigen", AutoSize = true };
        var hinweis = new TextBox { Width = 220, PlaceholderText = "Hinweis (z. B. Besuch nach Absprache)" };
        feld("Angehörige", liste);
        feld("Person", Formular.Reihe(wahl, AnlegenKnopf(PersonGruppe.Angehoerige, wahl)));
        feld("Beziehung", Formular.Reihe(beziehung, haupt, notfall));
        feld("Hinweis", Formular.Reihe(hinweis,
            Knopf("Hinzufügen", () =>
            {
                if (Gewaehlt(wahl) is not long id) { Fehlermeldung("Bitte eine Person wählen."); return; }
                if (!personen.ContainsKey(id) && speicher.PersonNach(id) is Person neu) personen[id] = neu;
                AngehoerigeListe.Add(new Angehoeriger { PersonId = id, Beziehung = beziehung.Text.Trim(), Hauptkontakt = haupt.Checked, Notfall = notfall.Checked, Hinweis = hinweis.Text.Trim() });
                Fuellen();
            }),
            Knopf("Entfernen", () => { if (liste.SelectedIndex >= 0) { AngehoerigeListe.RemoveAt(liste.SelectedIndex); Fuellen(); } })));
        lesen = () => new Angehoerige { Personen = [.. AngehoerigeListe] };
    }

    // ----- Ärzte

    public ComboBox Hausarzt { get; private set; } = null!;
    public ComboBox Psychiater { get; private set; } = null!;
    public List<WeitererArzt> WeitereAerzte { get; } = [];
    public const string Keiner = "keiner";

    // „nicht erfasst“, „keiner“, dann die Ärzte.
    private ComboBox Arztwahl(ZugeteilterArzt w)
    {
        var box = Personenwahl(PersonGruppe.Arzt, w.Stand == ArztStand.Arzt ? w.PersonId : null, AngabenRegeln.NichtErfasst);
        box.Items.Insert(1, Keiner);
        if (w.Stand == ArztStand.Keiner) box.SelectedIndex = 1;
        return box;
    }

    private static ZugeteilterArzt ArztAus(ComboBox box) => box.SelectedItem switch
    {
        string => new ZugeteilterArzt(ArztStand.Keiner),
        Wahl { Id: long id } => new ZugeteilterArzt(ArztStand.Arzt, id),
        _ => new ZugeteilterArzt(),
    };

    private void AerzteBauen(Action<string, Control> feld, AerzteAngabe? a)
    {
        Hausarzt = Arztwahl(a?.Hausarzt ?? new());
        Psychiater = Arztwahl(a?.Psychiater ?? new());
        var aerzte = speicher.Personen(PersonGruppe.Arzt).ToDictionary(p => p.Id);
        WeitereAerzte.AddRange(a?.Weitere ?? []);
        var liste = Liste();
        void Fuellen()
        {
            liste.Items.Clear();
            liste.Items.AddRange([.. WeitereAerzte.Select(w => $"{w.Rolle}: {(aerzte.TryGetValue(w.PersonId, out var p) ? p.Name : "?")}" + (w.Hinweis.Length > 0 ? $" · {w.Hinweis}" : ""))]);
        }
        Fuellen();
        var wahl = Personenwahl(PersonGruppe.Arzt, null);
        var rolle = new TextBox { Width = 180, PlaceholderText = "z. B. Zahnarzt" };
        wahl.SelectedIndexChanged += (_, _) => { if (Gewaehlt(wahl) is long id && aerzte.TryGetValue(id, out var p)) rolle.Text = p.Fachrichtung; };
        var hinweis = new TextBox { Width = 200, PlaceholderText = "Hinweis" };
        feld("Hausarzt", Formular.Reihe(Hausarzt, AnlegenKnopf(PersonGruppe.Arzt, Hausarzt)));
        feld("Psychiater", Formular.Reihe(Psychiater, AnlegenKnopf(PersonGruppe.Arzt, Psychiater)));
        feld("Weitere Ärzte", liste);
        feld("Arzt", Formular.Reihe(wahl, rolle));
        feld("Hinweis", Formular.Reihe(hinweis,
            Knopf("Hinzufügen", () =>
            {
                if (Gewaehlt(wahl) is not long id) { Fehlermeldung("Bitte einen Arzt wählen."); return; }
                WeitereAerzte.Add(new WeitererArzt(id, rolle.Text.Trim(), hinweis.Text.Trim()));
                Fuellen();
            }),
            Knopf("Entfernen", () => { if (liste.SelectedIndex >= 0) { WeitereAerzte.RemoveAt(liste.SelectedIndex); Fuellen(); } })));
        lesen = () => new AerzteAngabe { Hausarzt = ArztAus(Hausarzt), Psychiater = ArztAus(Psychiater), Weitere = [.. WeitereAerzte] };
    }

    // ----- Kostenträger

    public ComboBox Traeger { get; private set; } = null!;
    public List<Bewilligung> Bewilligungen { get; } = [];

    private void KostentraegerBauen(Action<string, Control> feld, Kostentraeger? k)
    {
        Traeger = Liste(Kern.Liste.Kostentraeger, k?.Name ?? "");
        var az = Feld(k?.Aktenzeichen ?? "", 180);
        var sb = Feld(k?.Sachbearbeiter ?? "");
        var tel = Feld(k?.Telefon ?? "", 160);
        var mail = Feld(k?.Email ?? "");
        mail.PlaceholderText = "name@example.org (Demo)";
        Bewilligungen.AddRange(k?.Bewilligungen ?? []);
        var liste = Liste();
        void Fuellen()
        {
            liste.Items.Clear();
            liste.Items.AddRange([.. Bewilligungen.OrderByDescending(b => b.Bis).Select(b => $"{b.Leistung}: {Datum.Text(b.Von)} bis {Datum.Text(b.Bis)}" +
                (b.BescheidVom is DateOnly v ? $" (Bescheid vom {Datum.Text(v)})" : ""))]);
        }
        Fuellen();
        var leistung = new TextBox { Width = 260, Text = "Besondere Wohnform (Eingliederungshilfe)" };
        var bescheid = DatumsFeld(null);
        var von = DatumsFeld(null);
        var bis = DatumsFeld(null);
        feld("Kostenträger", Traeger);
        feld("Aktenzeichen", az);
        feld("Sachbearbeitung", sb);
        feld("Telefon, E-Mail", Formular.Reihe(tel, mail));
        feld("Bewilligungen", liste);
        feld("Leistung", leistung);
        feld("Bescheid vom, von, bis", Formular.Reihe(bescheid, von, bis,
            Knopf("Hinzufügen", () =>
            {
                try
                {
                    var anfang = DatumOderLeer(von, "Bewilligt von") ?? throw new Eingabefehler(["Bewilligt von: bitte ein Datum."]);
                    var ende = DatumOderLeer(bis, "Bewilligt bis") ?? throw new Eingabefehler(["Bewilligt bis: bitte ein Datum."]);
                    Bewilligungen.Add(new Bewilligung(leistung.Text.Trim(), DatumOderLeer(bescheid, "Bescheid vom"), anfang, ende));
                    Fuellen();
                }
                catch (Eingabefehler e) { Fehlermeldung(string.Join("\n", e.Fehler)); }
            }),
            Knopf("Entfernen", () =>
            {
                if (liste.SelectedIndex < 0) return;
                Bewilligungen.Remove(Bewilligungen.OrderByDescending(b => b.Bis).ElementAt(liste.SelectedIndex));
                Fuellen();
            })));
        lesen = () => new Kostentraeger
        {
            Name = Traeger.SelectedItem as string ?? "", Aktenzeichen = az.Text.Trim(), Sachbearbeiter = sb.Text.Trim(), Telefon = tel.Text.Trim(),
            Email = mail.Text.Trim(), Bewilligungen = [.. Bewilligungen],
        };
    }
}
