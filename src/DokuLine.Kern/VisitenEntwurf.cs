using System.Text.Json;

namespace DokuLine.Kern;

// Visite mit allen Bewohnern: Man geht mit dem Arzt die Bewohner der Reihe nach
// durch. Was je Bewohner eingetragen wird, steht sofort als Entwurf in der Datenbank (je Bereich ein
// Entwurf) – beim Wechsel zum nächsten Bewohner, auch nach einem Absturz geht nichts verloren. Erst
// „Visite abschließen“ macht daraus eine Visite mit Berichten, Übergaben und Aufgaben.
public sealed class VisitenEntwurf
{
    public string Bereich { get; set; } = "";
    public DateOnly Datum { get; set; }
    public string Arzt { get; set; } = "";
    public string Teilnehmer { get; set; } = "";
    public string Kuerzel { get; set; } = "";
    public DateTime Begonnen { get; set; }
    public List<EntwurfEintrag> Eintraege { get; set; } = [];

    public EntwurfEintrag Eintrag(long bewohnerId)
    {
        if (Eintraege.FirstOrDefault(e => e.BewohnerId == bewohnerId) is EntwurfEintrag e) return e;
        e = new EntwurfEintrag { BewohnerId = bewohnerId };
        Eintraege.Add(e);
        return e;
    }
}

public sealed class EntwurfEintrag
{
    public long BewohnerId { get; set; }
    public string Besprochen { get; set; } = "";
    public bool Erledigt { get; set; }   // Häkchen „besprochen“ in der Liste
    public List<EntwurfAnordnung> Anordnungen { get; set; } = [];
    public bool FacharztNoetig { get; set; }
    public string Facharzt { get; set; } = "";
    public bool Dringend { get; set; }

    public bool HatInhalt => Besprochen.Trim().Length > 0 || Anordnungen.Count > 0 || FacharztNoetig;
}

public sealed class EntwurfAnordnung
{
    public string Art { get; set; } = "dosis";   // wie VisitenRegeln.Kuerzel
    public long? MedikamentId { get; set; }
    public string Medikament { get; set; } = "";
}

public static class VisitenEntwurfRegeln
{
    private static readonly JsonSerializerOptions Optionen = new() { WriteIndented = false };

    public static string AlsJson(List<EntwurfEintrag> eintraege) => JsonSerializer.Serialize(eintraege, Optionen);

    public static List<EntwurfEintrag> AusJson(string json)
    {
        try { return JsonSerializer.Deserialize<List<EntwurfEintrag>>(json, Optionen) ?? []; }
        catch (JsonException) { return []; }
    }

    public static EntwurfEintrag AusEingabe(VisiteEintragEingabe e, bool erledigt) => new()
    {
        BewohnerId = e.BewohnerId, Besprochen = e.Besprochen, Erledigt = erledigt, FacharztNoetig = e.FacharztNoetig, Facharzt = e.Facharzt, Dringend = e.Dringend,
        Anordnungen = [.. e.Anordnungen.Select(a => new EntwurfAnordnung { Art = VisitenRegeln.Kuerzel(a.Art), MedikamentId = a.MedikamentId, Medikament = a.Medikament })],
    };

    public static VisiteEintragEingabe AlsEingabe(EntwurfEintrag e) => new()
    {
        BewohnerId = e.BewohnerId, Besprochen = e.Besprochen, FacharztNoetig = e.FacharztNoetig, Facharzt = e.Facharzt, Dringend = e.Dringend,
        Anordnungen = [.. e.Anordnungen.Select(a => (VisitenRegeln.ArtAus(a.Art), a.MedikamentId, a.Medikament))],
    };

    // Was beim Abschließen als Aufgabe entsteht (je Anordnung eine, je Facharzttermin eine).
    public static int Vorgemerkt(VisitenEntwurf v) =>
        v.Eintraege.Sum(e => e.Anordnungen.Count(a => a.Medikament.Trim().Length > 0) + (e.FacharztNoetig && e.Facharzt.Trim().Length > 0 ? 1 : 0));

    public static VisiteEingabe AlsVisite(VisitenEntwurf v) => new()
    {
        Datum = Kern.Datum.Text(v.Datum), Arzt = v.Arzt, Teilnehmer = v.Teilnehmer, Kuerzel = v.Kuerzel,
        Eintraege = [.. v.Eintraege.Where(e => e.HatInhalt).Select(AlsEingabe)],
    };
}
