namespace DokuLine.Kern;

// Zuständigkeit (Bauplan 30a): Forensik und FOSA-Nachsorge mit ihren Zuständigen aus dem Personenverzeichnis. Solange
// jemand über die Forensik läuft, ist der Betreuer nicht zuständig. Danach richtet sich auch der Bestellweg der
// Medikamente (Forensik, FOSA oder Hausarzt/Apotheke).
public sealed record Zustaendigkeit
{
    public JaNein Forensik { get; init; }
    public string Klinik { get; init; } = "";
    public DateOnly? ForensikSeit { get; init; }
    public string Status { get; init; } = "";             // z. B. „Langzeitbeurlaubung“
    public DateOnly? Ueberpruefung { get; init; }          // nächste Überprüfung (Marke 8 Wochen vorher)
    public List<long> ForensikPersonen { get; init; } = [];
    public JaNein Fosa { get; init; }
    public string Ambulanz { get; init; } = "";
    public DateOnly? FosaSeit { get; init; }
    public DateOnly? FosaBis { get; init; }
    public List<long> FosaPersonen { get; init; } = [];
}

public enum Bestellweg { NichtErfasst, Forensik, Fosa, Hausarzt }

public sealed record BestellwegAngabe
{
    public Bestellweg Weg { get; init; }
    public long? PersonId { get; init; }                  // Ansprechperson aus dem Verzeichnis
}

public static class ZustaendigkeitRegeln
{
    public const string BetreuerNichtZustaendig = "nicht zuständig, solange Forensik";
    public const string EmailHinweis =
        "Die Bestellung enthält Namen, Geburtsdatum und Medikamente – Gesundheitsdaten. Nur über einen sicheren Weg senden " +
        "(z. B. verschlüsselte E-Mail der Einrichtung), sonst lieber telefonisch oder per Fax bestellen.";

    public static bool InForensik(Zustaendigkeit? z) => z?.Forensik == JaNein.Ja;

    public static bool InFosa(Zustaendigkeit? z, DateOnly heute) => z?.Fosa == JaNein.Ja && (z.FosaBis is null || z.FosaBis >= heute);

    // Marken „Forensik“ und „FOSA“ (Hinweis-Farbe, mit Wort) und die Überprüfung der Forensik 8 Wochen vorher.
    public static List<Marke> Marken(Zustaendigkeit? z, DateOnly heute)
    {
        var marken = new List<Marke>();
        if (InForensik(z)) marken.Add(new Marke(MarkenArt.Zustaendig, "Forensik"));
        if (InFosa(z, heute)) marken.Add(new Marke(MarkenArt.Zustaendig, "FOSA"));
        if (InForensik(z) && z!.Ueberpruefung is DateOnly u && u.DayNumber - heute.DayNumber <= AngabenRegeln.BetreuungWochen * 7)
            marken.Add(new Marke(MarkenArt.Beschluss, u < heute ? $"Forensik: Überprüfung überfällig seit {Datum.Text(u)}" : $"Forensik: Überprüfung {Datum.Text(u)}"));
        return marken;
    }

    // Vorschlag: Forensik → Forensik, sonst FOSA → FOSA, sonst Hausarzt/Apotheke.
    public static Bestellweg Vorschlag(Zustaendigkeit? z, DateOnly heute) =>
        InForensik(z) ? Bestellweg.Forensik : InFosa(z, heute) ? Bestellweg.Fosa : Bestellweg.Hausarzt;

    public static string WegText(Bestellweg w) => w switch
    {
        Bestellweg.Forensik => "Forensik",
        Bestellweg.Fosa => "FOSA",
        Bestellweg.Hausarzt => "Hausarzt/Apotheke",
        _ => AngabenRegeln.NichtErfasst,
    };

    // „Bestellung über: FOSA · Name, Telefon“ (oben im Medikamentenplan).
    public static string BestellungText(BestellwegAngabe? b, Person? p) =>
        b is null || b.Weg == Bestellweg.NichtErfasst ? $"Bestellung über: {AngabenRegeln.NichtErfasst}"
        : $"Bestellung über: {WegText(b.Weg)}" + (p is null ? "" : $" · {p.Name}" + (p.Telefon.Length > 0 ? $", {p.Telefon}" : ""));

    public static string Betreff(Bewohner b) => $"Medikamentenbestellung {b.Name}, geb. {Datum.Text(b.Geburtsdatum)}";

    // Text der Bestellung: je Medikament Name mit Form und Stärke; keine Dosis, keine weiteren Angaben.
    public static string Bestelltext(IEnumerable<Verordnung> medikamente) =>
        "Bitte folgende Medikamente bestellen:\n" + string.Join("\n", medikamente.Select(v =>
            "- " + string.Join(" ", new[] { v.Medikament, v.Staerke }.Where(x => !string.IsNullOrWhiteSpace(x)))));

    // mailto: mit Empfänger, Betreff und Text (Regel 6: das E-Mail-Programm sendet, nicht DokuLine).
    public static string Mailto(string empfaenger, string betreff, string text) =>
        $"mailto:{empfaenger}?subject={Uri.EscapeDataString(betreff)}&body={Uri.EscapeDataString(text)}";

    public static List<(string Beschriftung, string Wert, bool Achtung)> Zeilen(AngabeFassung? f, Func<long?, string> person)
    {
        if (f is null) return [("Zuständigkeit", AngabenRegeln.NichtErfasst, true)];
        if (f.Lesen<Zustaendigkeit>() is not Zustaendigkeit z) return [("Zuständigkeit", "keine Forensik, keine FOSA", false)];
        string JaNeinText(JaNein j) => j switch { JaNein.Ja => "ja", JaNein.Nein => "nein", _ => AngabenRegeln.NichtErfasst };
        var zeilen = new List<(string, string, bool)> { ("Forensik", JaNeinText(z.Forensik), z.Forensik == JaNein.NichtErfasst) };
        if (z.Forensik == JaNein.Ja)
        {
            zeilen.Add(("Klinik", string.Join(" · ", new[] { z.Klinik, z.ForensikSeit is DateOnly s ? $"seit {Datum.Text(s)}" : "", z.Status }.Where(x => x.Length > 0)), false));
            if (z.Ueberpruefung is DateOnly u) zeilen.Add(("Nächste Überprüfung", Datum.Text(u), false));
            foreach (long p in z.ForensikPersonen) zeilen.Add(("Zuständig (Forensik)", person(p), false));
        }
        zeilen.Add(("FOSA-Nachsorge", JaNeinText(z.Fosa), z.Fosa == JaNein.NichtErfasst));
        if (z.Fosa == JaNein.Ja)
        {
            zeilen.Add(("Ambulanz", string.Join(" · ", new[] { z.Ambulanz, z.FosaSeit is DateOnly s ? $"seit {Datum.Text(s)}" : "",
                z.FosaBis is DateOnly b ? $"bis {Datum.Text(b)}" : "" }.Where(x => x.Length > 0)), false));
            foreach (long p in z.FosaPersonen) zeilen.Add(("Zuständig (FOSA)", person(p), false));
        }
        return zeilen;
    }
}
