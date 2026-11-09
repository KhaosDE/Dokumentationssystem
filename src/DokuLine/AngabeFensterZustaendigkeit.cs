using DokuLine.Kern;

namespace DokuLine;

// Zuständigkeit (Forensik, FOSA mit ihren Zuständigen) und Bestellweg der Medikamente (Bauplan 30a).
public sealed partial class AngabeFenster
{
    public ComboBox ForensikWahl { get; private set; } = null!;
    public ComboBox FosaWahl { get; private set; } = null!;
    public CheckedListBox ForensikPersonen { get; private set; } = null!;
    public CheckedListBox FosaPersonen { get; private set; } = null!;
    public ComboBox Weg { get; private set; } = null!;

    private static ComboBox JaNeinWahl(JaNein wert)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        box.Items.AddRange([AngabenRegeln.NichtErfasst, "ja", "nein"]);
        box.SelectedIndex = (int)wert;
        return box;
    }

    // Die Zuständigen aus dem Verzeichnis zum Ankreuzen (mit Telefon).
    private CheckedListBox Zustaendige(PersonGruppe gruppe, IReadOnlyCollection<long> gewaehlt, out List<long> ids)
    {
        var liste = new CheckedListBox { Width = 420, Height = 90, CheckOnClick = true, IntegralHeight = false };
        var personen = speicher.Personen(gruppe, nurAktive: true);
        foreach (long id in gewaehlt) if (personen.All(p => p.Id != id) && speicher.PersonNach(id) is Person alt) personen.Add(alt);
        ids = [.. personen.Select(p => p.Id)];
        foreach (var p in personen)
            liste.Items.Add(string.Join(" · ", new[] { p.Name, p.Fachrichtung, p.Telefon }.Where(x => x.Length > 0)), gewaehlt.Contains(p.Id));
        return liste;
    }

    private void ZustaendigkeitBauen(Action<string, Control> feld, Zustaendigkeit? z)
    {
        ForensikWahl = JaNeinWahl(z?.Forensik ?? JaNein.NichtErfasst);
        var klinik = Feld(z?.Klinik ?? "", 300);
        var seit = DatumsFeld(z?.ForensikSeit);
        var status = Feld(z?.Status ?? "", 220);
        status.PlaceholderText = "z. B. Langzeitbeurlaubung";
        var pruefung = DatumsFeld(z?.Ueberpruefung);
        ForensikPersonen = Zustaendige(PersonGruppe.Forensik, z?.ForensikPersonen ?? [], out var forensikIds);
        FosaWahl = JaNeinWahl(z?.Fosa ?? JaNein.NichtErfasst);
        var ambulanz = Feld(z?.Ambulanz ?? "", 300);
        var fosaSeit = DatumsFeld(z?.FosaSeit);
        var fosaBis = DatumsFeld(z?.FosaBis);
        FosaPersonen = Zustaendige(PersonGruppe.Fosa, z?.FosaPersonen ?? [], out var fosaIds);
        feld("Forensik", ForensikWahl);
        feld("Klinik, seit", Formular.Reihe(klinik, seit));
        feld("Status", status);
        feld("Nächste Überprüfung", pruefung);
        feld("Zuständig (Forensik)", ForensikPersonen);
        feld("FOSA-Nachsorge", FosaWahl);
        feld("Ambulanz, seit, bis", Formular.Reihe(ambulanz, fosaSeit, fosaBis));
        feld("Zuständig (FOSA)", FosaPersonen);
        List<long> Angekreuzt(CheckedListBox liste, List<long> ids) => [.. liste.CheckedIndices.Cast<int>().Select(i => ids[i])];
        lesen = () =>
        {
            bool f = ForensikWahl.SelectedIndex == (int)JaNein.Ja, o = FosaWahl.SelectedIndex == (int)JaNein.Ja;
            return new Zustaendigkeit
            {
                Forensik = (JaNein)ForensikWahl.SelectedIndex, Klinik = f ? klinik.Text.Trim() : "", ForensikSeit = f ? DatumOderLeer(seit, "Forensik seit") : null,
                Status = f ? status.Text.Trim() : "", Ueberpruefung = f ? DatumOderLeer(pruefung, "Nächste Überprüfung") : null,
                ForensikPersonen = f ? Angekreuzt(ForensikPersonen, forensikIds) : [],
                Fosa = (JaNein)FosaWahl.SelectedIndex, Ambulanz = o ? ambulanz.Text.Trim() : "", FosaSeit = o ? DatumOderLeer(fosaSeit, "FOSA seit") : null,
                FosaBis = o ? DatumOderLeer(fosaBis, "FOSA bis") : null, FosaPersonen = o ? Angekreuzt(FosaPersonen, fosaIds) : [],
            };
        };
    }

    // Vorschlag nach der Zuständigkeit; Ansprechperson aus Forensik, FOSA oder den Ärzten.
    private void BestellwegBauen(Action<string, Control> feld, BestellwegAngabe? b, DateOnly heute)
    {
        var zust = speicher.Angaben(bewohner.Id!.Value, heute).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>();
        var vorschlag = ZustaendigkeitRegeln.Vorschlag(zust, heute);
        Weg = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        Weg.Items.AddRange([.. Enum.GetValues<Bestellweg>().Select(ZustaendigkeitRegeln.WegText)]);
        Weg.SelectedIndex = (int)(b?.Weg ?? vorschlag);
        var personen = speicher.Personen(PersonGruppe.Forensik, true).Concat(speicher.Personen(PersonGruppe.Fosa, true)).Concat(speicher.Aerzte(true)).ToList();
        var person = Personenwahl(PersonGruppe.Fosa, b?.PersonId, "(keine Ansprechperson)", personen);
        feld("Bestellung über", Formular.Reihe(Weg, new Label { Text = $"Vorschlag: {ZustaendigkeitRegeln.WegText(vorschlag)}", AutoSize = true, ForeColor = Stil.TextLeise, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }));
        feld("Ansprechperson", person);
        lesen = () => new BestellwegAngabe { Weg = (Bestellweg)Weg.SelectedIndex, PersonId = Gewaehlt(person) };
    }
}
