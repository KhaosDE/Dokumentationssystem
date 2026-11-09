using DokuLine.Kern;

namespace DokuLine;

// Rechte des Angemeldeten für die Oberfläche: Knöpfe ohne Berechtigung
// bleiben sichtbar, aber gesperrt (Stil.Sperren). Wer angemeldet ist, steht in den Einstellungen.
// Das Kürzel kommt immer aus der Anmeldung – „eingetragen von“ tippt niemand ein.
public static class Recht
{
    public const string NichtAngemeldetAnzeige = "– bitte zuerst oben rechts anmelden –";

    public static string Kuerzel => Einstellungen.Laden().Kuerzel;

    public static bool Angemeldet => Kuerzel.Length > 0;

    public static string? Grund(Speicher speicher, string funktion) => speicher.Grund(Kuerzel, funktion);

    public static bool Darf(Speicher speicher, string funktion) => Grund(speicher, funktion) is null;

    public static void Anwenden(Speicher speicher, string funktion, params Button[] knoepfe)
    {
        string? grund = Grund(speicher, funktion);
        foreach (var k in knoepfe) Stil.Sperren(k, grund);
    }

    // Ohne Anmeldung nur ansehen: Knöpfe zum Eintragen gesperrt mit „Bitte zuerst anmelden.“
    public static void NurAngemeldet(params Button[] knoepfe)
    {
        foreach (var k in knoepfe) Stil.Sperren(k, Angemeldet ? null : RechteRegeln.NichtAngemeldetText);
    }

    // Schreibgeschützte Anzeige „eingetragen von“ in Formularen.
    public static Label KuerzelAnzeige() => new()
    {
        Text = Angemeldet ? Kuerzel : NichtAngemeldetAnzeige, AutoSize = true, Font = Stil.Fett, UseMnemonic = false,
        ForeColor = Angemeldet ? Stil.TextNormal : Stil.Achtung.Text, Margin = new Padding(0, Stil.AbstandKlein, 0, 0),
    };
}

// Personen-Feld mit einer Person (z. B. Fahrer): Auswahl aus den aktiven Mitarbeitern, kein Freitext;
// vorbelegt mit der angemeldeten Person.
public sealed class PersonWahl : ComboBox
{
    private readonly List<Mitarbeiter> personen;

    public PersonWahl(Speicher speicher, string? vorbelegt = null)
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        Width = 240;
        personen = speicher.AlleMitarbeiter().Where(m => m.Aktiv).ToList();
        Items.AddRange([.. personen.Select(m => (object)m.Anzeige)]);
        Kuerzel = vorbelegt ?? Recht.Kuerzel;
    }

    public string Kuerzel
    {
        get => SelectedIndex >= 0 ? personen[SelectedIndex].Kuerzel : "";
        set => SelectedIndex = personen.FindIndex(m => m.Kuerzel.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

// Personen-Feld mit mehreren Personen (z. B. Teilnehmer der Visite, Begleitung eines Ausflugs):
// Häkchen bei den aktiven Mitarbeitern, vorbelegt mit der angemeldeten Person. Ältere Einträge, die
// keinem Kürzel entsprechen (früher Freitext), bleiben erhalten.
public sealed class PersonenWahl : CheckedListBox
{
    private readonly List<Mitarbeiter> personen;
    private List<string> fremde = [];

    public PersonenWahl(Speicher speicher, IEnumerable<string>? vorbelegt = null)
    {
        CheckOnClick = true;
        IntegralHeight = false;
        Width = 300;
        Height = 110;
        personen = speicher.AlleMitarbeiter().Where(m => m.Aktiv).ToList();
        Items.AddRange([.. personen.Select(m => (object)m.Anzeige)]);
        Setzen(vorbelegt ?? (Recht.Angemeldet ? [Recht.Kuerzel] : []));
    }

    public static List<string> Lesen(string text) =>
        [.. text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    public void Setzen(IEnumerable<string> kuerzel)
    {
        var liste = kuerzel.ToList();
        for (int i = 0; i < personen.Count; i++)
            SetItemChecked(i, liste.Contains(personen[i].Kuerzel, StringComparer.OrdinalIgnoreCase));
        fremde = [.. liste.Where(k => !personen.Any(m => m.Kuerzel.Equals(k, StringComparison.OrdinalIgnoreCase)))];
    }

    public List<string> Kuerzel => [.. CheckedIndices.Cast<int>().Select(i => personen[i].Kuerzel).Concat(fremde)];

    public string AlsText => string.Join(", ", Kuerzel);
}
