using System.Globalization;

namespace Dokusystem.Kern;

// Was eine Bewohnerkarte zusätzlich zu Bild, Name und Zimmer zeigt (Gestaltung Stufe 2):
// Marken für das, was Aufmerksamkeit braucht, und die Aufenthaltsdauer.
// Farbe sparsam: Der Normalfall („keine bekannt“, „kein Beschluss“) bekommt keine Marke.
public enum MarkenArt { Beschluss, Allergie, NichtErfasst, Uebergabe, Wichtig }

public sealed record Marke(MarkenArt Art, string Text);

// Die Filterknöpfe über dem Kartenraster.
public enum BewohnerFilter { Alle, Beschluss, Allergie, NichtErfasst }

public static class Kennzeichen
{
    // ungeleseneUebergaben: offene Übergabe-Einträge, die das eigene Kürzel noch nicht gelesen hat;
    // wichtigeBerichte: als wichtig markierte Berichte der letzten 7 Tage.
    public static List<Marke> Marken(Bewohner b, int ungeleseneUebergaben = 0, int wichtigeBerichte = 0)
    {
        var marken = new List<Marke>();
        if (ungeleseneUebergaben > 0) marken.Add(new Marke(MarkenArt.Uebergabe, $"Übergabe {ungeleseneUebergaben}"));
        if (wichtigeBerichte > 0) marken.Add(new Marke(MarkenArt.Wichtig, "Wichtig"));
        if (b.Beschluss == BeschlussZustand.Ende)
            marken.Add(new Marke(MarkenArt.Beschluss, $"Beschluss bis {Datum.Text(b.Beschlussende)}"));
        if (b.Allergie == AllergieZustand.Eingetragen)
            marken.Add(new Marke(MarkenArt.Allergie, $"Allergie: {b.Allergien}"));
        if (b.Allergie == AllergieZustand.NichtErfasst)
            marken.Add(new Marke(MarkenArt.NichtErfasst, "Allergien nicht erfasst"));
        if (b.Beschluss == BeschlussZustand.NichtErfasst)
            marken.Add(new Marke(MarkenArt.NichtErfasst, "Beschluss nicht erfasst"));
        return marken;
    }

    public static string FilterText(BewohnerFilter f) => f switch
    {
        BewohnerFilter.Beschluss => "Beschluss",
        BewohnerFilter.Allergie => "Allergie",
        BewohnerFilter.NichtErfasst => "Nicht erfasst",
        _ => "Alle",
    };

    public static bool Passt(Bewohner b, BewohnerFilter f) => f switch
    {
        BewohnerFilter.Beschluss => b.Beschluss == BeschlussZustand.Ende,
        BewohnerFilter.Allergie => b.Allergie == AllergieZustand.Eingetragen,
        BewohnerFilter.NichtErfasst => b.Allergie == AllergieZustand.NichtErfasst || b.Beschluss == BeschlussZustand.NichtErfasst,
        _ => true,
    };

    // „seit 01.09.2019 · 7 Jahre, 2 Monate“ – wie lange jemand schon im Wohnbereich lebt.
    public static string Aufenthalt(DateOnly aufnahme, DateOnly heute)
    {
        if (aufnahme > heute) return $"Aufnahme am {Datum.Text(aufnahme)}";
        int monate = (heute.Year - aufnahme.Year) * 12 + heute.Month - aufnahme.Month;
        if (heute.Day < aufnahme.Day) monate--;
        int jahre = monate / 12;
        monate %= 12;
        string dauer;
        if (jahre > 0)
            dauer = Anzahl(jahre, "Jahr", "Jahre") + (monate > 0 ? ", " + Anzahl(monate, "Monat", "Monate") : "");
        else if (monate > 0)
            dauer = Anzahl(monate, "Monat", "Monate");
        else
            dauer = Anzahl(heute.DayNumber - aufnahme.DayNumber, "Tag", "Tage");
        return $"seit {Datum.Text(aufnahme)} · {dauer}";
    }

    private static string Anzahl(int n, string einzahl, string mehrzahl) =>
        $"{n.ToString(CultureInfo.InvariantCulture)} {(n == 1 ? einzahl : mehrzahl)}";
}
