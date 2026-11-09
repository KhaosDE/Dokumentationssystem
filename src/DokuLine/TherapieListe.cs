using DokuLine.Kern;

namespace DokuLine;

// Im Bereich „Therapie“ (Bereichswahl): Bewohnerleisten mit allen, die ein Angebot der Therapie haben, nach
// Wohnbereich gruppiert; der Schalter „alle Bewohner des Hauses“ nimmt die übrigen dazu.
public static class TherapieListe
{
    public const string AlleText = "alle Bewohner des Hauses";

    public static bool Aktiv => Arbeitsbereich.Gewaehlt == TherapieRegeln.AnbieterTherapie;

    public static List<Bewohner> Bewohner(Speicher speicher, bool alle, DateOnly heute) =>
        alle ? speicher.AlleBewohner() : TherapieRegeln.MitTherapie(speicher.AlleBewohner(), speicher.Angebote(), heute);

    // Füllt die Liste mit einer Überschrift je Wohnbereich; zurück kommt je Zeile der Bewohner (null: Überschrift).
    public static List<Bewohner?> Fuellen(ListBox liste, IEnumerable<Bewohner> bewohner, Speicher speicher)
    {
        var zeilen = new List<Bewohner?>();
        liste.BeginUpdate();
        liste.Items.Clear();
        foreach (var (bereich, gruppe) in TherapieRegeln.NachBereich(bewohner, [.. speicher.Bereichsliste().Select(b => b.Name)]))
        {
            liste.Items.Add(new ListenUeberschrift(bereich));
            zeilen.Add(null);
            foreach (var b in gruppe)
            {
                liste.Items.Add($"{b.Nachname}, {b.Vorname}");
                zeilen.Add(b);
            }
        }
        liste.EndUpdate();
        return zeilen;
    }

    public static CheckBox AlleSchalter() => new()
    {
        Text = AlleText, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche,
        AccessibleName = AlleText, Visible = false,
    };
}
