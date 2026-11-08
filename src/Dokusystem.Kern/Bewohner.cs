using System.Globalization;

namespace Dokusystem.Kern;

// Sicherheitsangaben haben drei Zustände (Regel 5 in CLAUDE.md). „Nicht erfasst“ ist der
// Wert 0 und damit die Vorgabe: Ein neu angelegter Bewohner sieht nie wie „keine Allergie“ aus.
public enum AllergieZustand { NichtErfasst = 0, Eingetragen, KeineBekannt }

// Beschlussende genauso: Datum eingetragen / kein Beschluss / nicht erfasst.
public enum BeschlussZustand { NichtErfasst = 0, Ende, KeinBeschluss }

// „Offen geführt“ ebenso: ja / nein / nicht erfasst.
public enum OffenGefuehrtZustand { NichtErfasst = 0, Ja, Nein }

// Ein Bewohner, wie er gespeichert wird. „record“ heißt: Werte statt Verweise; eine
// geänderte Kopie entsteht mit „with { … }“.
public sealed record Bewohner
{
    public long? Id { get; init; }
    public required string Vorname { get; init; }
    public required string Nachname { get; init; }
    public required string Zimmer { get; init; }
    public required DateOnly Geburtsdatum { get; init; }
    public required DateOnly Aufnahmedatum { get; init; }
    public AllergieZustand Allergie { get; init; }
    public string Allergien { get; init; } = "";
    public BeschlussZustand Beschluss { get; init; }
    public DateOnly? Beschlussende { get; init; }
    public OffenGefuehrtZustand OffenGefuehrt { get; init; }
    public string? Bild { get; init; }   // Dateiname im Ordner „bilder“ des Datenordners

    public string Name => $"{Vorname} {Nachname}";
}

// Die Texteingaben aus dem Formular oder der Demo-Datei, noch ungeprüft.
// Zustände als Kürzel wie in daten/demo/bewohner.json.
public sealed class BewohnerEingabe
{
    public string Vorname { get; set; } = "";
    public string Nachname { get; set; } = "";
    public string Zimmer { get; set; } = "";
    public string Geburtsdatum { get; set; } = "";
    public string Aufnahmedatum { get; set; } = "";
    public string Allergie { get; set; } = "";
    public string Allergien { get; set; } = "";
    public string Beschluss { get; set; } = "";
    public string Beschlussende { get; set; } = "";
    public string OffenGefuehrt { get; set; } = "";
}

// Eingaben passen nicht. Fehler enthält verständliche Sätze in der Reihenfolge des Formulars.
public sealed class Eingabefehler(IReadOnlyList<string> fehler) : Exception(string.Join(" ", fehler))
{
    public IReadOnlyList<string> Fehler { get; } = fehler;
}

public static class BewohnerRegeln
{
    // ----- Kürzel der Zustände (Datenbank und Demo-Datei) -----

    public static string Kuerzel(AllergieZustand z) => z switch
    {
        AllergieZustand.Eingetragen => "eingetragen",
        AllergieZustand.KeineBekannt => "keine_bekannt",
        _ => "nicht_erfasst",
    };

    public static string Kuerzel(OffenGefuehrtZustand z) => z switch
    {
        OffenGefuehrtZustand.Ja => "ja",
        OffenGefuehrtZustand.Nein => "nein",
        _ => "nicht_erfasst",
    };

    public static OffenGefuehrtZustand? OffenGefuehrtAus(string? kuerzel) => (kuerzel ?? "").Trim() switch
    {
        "ja" => OffenGefuehrtZustand.Ja,
        "nein" => OffenGefuehrtZustand.Nein,
        "nicht_erfasst" or "" => OffenGefuehrtZustand.NichtErfasst,
        _ => null,
    };

    public static string Kuerzel(BeschlussZustand z) => z switch
    {
        BeschlussZustand.Ende => "ende",
        BeschlussZustand.KeinBeschluss => "kein_beschluss",
        _ => "nicht_erfasst",
    };

    // Leer zählt als „nicht erfasst“; ein unbekanntes Kürzel ergibt null.
    public static AllergieZustand? AllergieAus(string? kuerzel) => (kuerzel ?? "").Trim() switch
    {
        "eingetragen" => AllergieZustand.Eingetragen,
        "keine_bekannt" => AllergieZustand.KeineBekannt,
        "nicht_erfasst" or "" => AllergieZustand.NichtErfasst,
        _ => null,
    };

    public static BeschlussZustand? BeschlussAus(string? kuerzel) => (kuerzel ?? "").Trim() switch
    {
        "ende" => BeschlussZustand.Ende,
        "kein_beschluss" => BeschlussZustand.KeinBeschluss,
        "nicht_erfasst" or "" => BeschlussZustand.NichtErfasst,
        _ => null,
    };

    // ----- Prüfregeln -----

    // Alle Prüfregeln für einen vollständigen Bewohner. Leere Liste = in Ordnung.
    public static List<string> Pruefen(Bewohner b) =>
        Pruefen(b.Vorname, b.Nachname, b.Zimmer, b.Geburtsdatum, b.Aufnahmedatum,
                b.Allergie, b.Allergien, b.Beschluss, b.Beschlussende, b.OffenGefuehrt);

    private static List<string> Pruefen(string vorname, string nachname, string zimmer,
        DateOnly? geburt, DateOnly? aufnahme, AllergieZustand? allergie, string allergien,
        BeschlussZustand? beschluss, DateOnly? ende, OffenGefuehrtZustand? offen)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(vorname)) fehler.Add("Vorname fehlt.");
        if (string.IsNullOrWhiteSpace(nachname)) fehler.Add("Nachname fehlt.");
        if (string.IsNullOrWhiteSpace(zimmer)) fehler.Add("Zimmernummer fehlt.");
        if (geburt is DateOnly g && aufnahme is DateOnly a && a < g)
            fehler.Add("Das Aufnahmedatum liegt vor dem Geburtsdatum.");

        if (allergie is null || !Enum.IsDefined(allergie.Value))
            fehler.Add("Allergien: unbekannte Angabe.");
        else if (allergie == AllergieZustand.Eingetragen && string.IsNullOrWhiteSpace(allergien))
            fehler.Add("Allergien: bitte eintragen, welche – oder „keine bekannt“ bzw. „nicht erfasst“ wählen.");
        else if (allergie != AllergieZustand.Eingetragen && !string.IsNullOrWhiteSpace(allergien))
            fehler.Add("Allergien: Text nur bei eingetragenen Allergien.");

        if (beschluss is null || !Enum.IsDefined(beschluss.Value))
            fehler.Add("Beschluss: unbekannte Angabe.");
        else if (beschluss == BeschlussZustand.Ende && ende is null)
            fehler.Add("Beschlussende fehlt.");
        else if (beschluss != BeschlussZustand.Ende && ende is not null)
            fehler.Add("Beschlussende nur, wenn es einen Beschluss gibt.");

        if (offen is null || !Enum.IsDefined(offen.Value))
            fehler.Add("Offen geführt: unbekannte Angabe.");
        return fehler;
    }

    // Fehler erscheinen in der Reihenfolge des Formulars.
    private static readonly string[] Reihenfolge =
        ["Vorname", "Nachname", "Zimmernummer", "Geburtsdatum", "Aufnahmedatum", "Das Aufnahmedatum", "Beschluss", "Offen geführt", "Allergien"];

    private static int Rang(string satz)
    {
        int i = Array.FindIndex(Reihenfolge, wort => satz.StartsWith(wort, StringComparison.Ordinal));
        return i < 0 ? Reihenfolge.Length : i;
    }

    // Baut einen Bewohner aus Texteingaben. Fehlt ein Zustand, gilt „nicht erfasst“.
    // Wirft Eingabefehler mit allen Fehlern auf einmal.
    public static Bewohner AusEingabe(BewohnerEingabe e, long? id = null, string? bild = null)
    {
        var fehler = new List<string>();
        var unlesbar = new HashSet<string>();   // Datumsfelder, zu denen schon ein Fehler notiert ist

        DateOnly? DatumLesen(string text, string feld)
        {
            try { return Datum.Lesen(text, feld); }
            catch (FormatException ex) { fehler.Add(ex.Message); unlesbar.Add(feld); return null; }
        }

        var geburt = DatumLesen(e.Geburtsdatum, "Geburtsdatum");
        var aufnahme = DatumLesen(e.Aufnahmedatum, "Aufnahmedatum");
        var allergie = AllergieAus(e.Allergie);
        var beschluss = BeschlussAus(e.Beschluss);
        var ende = beschluss == BeschlussZustand.Ende ? DatumLesen(e.Beschlussende, "Beschlussende") : null;
        var offen = OffenGefuehrtAus(e.OffenGefuehrt);
        string allergien = allergie == AllergieZustand.Eingetragen ? (e.Allergien ?? "").Trim() : "";

        foreach (string f in Pruefen(e.Vorname, e.Nachname, e.Zimmer, geburt, aufnahme, allergie, allergien, beschluss, ende, offen))
            if (!fehler.Contains(f) && !unlesbar.Any(feld => f.StartsWith(feld, StringComparison.Ordinal)))
                fehler.Add(f);
        if (fehler.Count > 0)
            throw new Eingabefehler(fehler.OrderBy(Rang).ToList());   // OrderBy ist stabil

        return new Bewohner
        {
            Id = id,
            Vorname = e.Vorname.Trim(),
            Nachname = e.Nachname.Trim(),
            Zimmer = e.Zimmer.Trim(),
            Geburtsdatum = geburt!.Value,
            Aufnahmedatum = aufnahme!.Value,
            Allergie = allergie!.Value,
            Allergien = allergien,
            Beschluss = beschluss!.Value,
            Beschlussende = ende,
            OffenGefuehrt = offen!.Value,
            Bild = bild,
        };
    }

    // Gegenstück zu AusEingabe: Texte zum Vorbefüllen des Formulars.
    public static BewohnerEingabe AlsEingabe(Bewohner b) => new()
    {
        Vorname = b.Vorname,
        Nachname = b.Nachname,
        Zimmer = b.Zimmer,
        Geburtsdatum = Datum.Text(b.Geburtsdatum),
        Aufnahmedatum = Datum.Text(b.Aufnahmedatum),
        Allergie = Kuerzel(b.Allergie),
        Allergien = b.Allergien,
        Beschluss = Kuerzel(b.Beschluss),
        Beschlussende = Datum.Text(b.Beschlussende),
        OffenGefuehrt = Kuerzel(b.OffenGefuehrt),
    };

    // ----- Anzeige -----

    public static string AllergieText(Bewohner b) => b.Allergie switch
    {
        AllergieZustand.Eingetragen => $"Allergien: {b.Allergien}",
        AllergieZustand.KeineBekannt => "Allergien: keine bekannt",
        _ => "Allergien: nicht erfasst – bitte klären",
    };

    public static string BeschlussText(Bewohner b) => b.Beschluss switch
    {
        BeschlussZustand.Ende => Datum.Text(b.Beschlussende),
        BeschlussZustand.KeinBeschluss => "kein Beschluss",
        _ => "nicht erfasst",
    };

    public static string OffenGefuehrtText(Bewohner b) => b.OffenGefuehrt switch
    {
        OffenGefuehrtZustand.Ja => "ja",
        OffenGefuehrtZustand.Nein => "nein",
        _ => "nicht erfasst",
    };

    // Alter in vollen Jahren (Geburtstag am 29. Februar: in Nicht-Schaltjahren am 28. Februar).
    public static int Alter(DateOnly geburt, DateOnly heute)
    {
        int jahre = heute.Year - geburt.Year;
        var geburtstag = geburt.Month == 2 && geburt.Day == 29 && !DateTime.IsLeapYear(heute.Year)
            ? new DateOnly(heute.Year, 2, 28)
            : new DateOnly(heute.Year, geburt.Month, geburt.Day);
        return heute < geburtstag ? jahre - 1 : jahre;
    }

    public static string AlterText(DateOnly geburt, DateOnly heute) =>
        Alter(geburt, heute) is int a && a == 1 ? "1 Jahr" : $"{Alter(geburt, heute)} Jahre";

    public static string Initialen(string vorname, string nachname)
    {
        string text = string.Concat(new[] { vorname, nachname }
            .Select(teil => teil.Trim())
            .Where(teil => teil.Length > 0)
            .Select(teil => char.ToUpper(teil[0], CultureInfo.GetCultureInfo("de-DE"))));
        return text.Length > 0 ? text : "?";
    }

    public static string Initialen(Bewohner b) => Initialen(b.Vorname, b.Nachname);

    // Sortierung nach Nachname, dann Vorname – Groß/klein egal, Umlaute wie im Telefonbuch.
    public static readonly IComparer<Bewohner> NachName = Comparer<Bewohner>.Create((x, y) =>
    {
        var vergleich = CultureInfo.GetCultureInfo("de-DE").CompareInfo;
        int n = vergleich.Compare(x.Nachname, y.Nachname, CompareOptions.IgnoreCase);
        return n != 0 ? n : vergleich.Compare(x.Vorname, y.Vorname, CompareOptions.IgnoreCase);
    });
}
