using System.Globalization;

namespace DokuLine.Kern;

// Wohnbereich-ToDo: Aufgaben, die an die nächste Schicht (oder eine bestimmte Schicht, oder an
// „wer Zeit hat“) gehen und stehen bleiben, bis sie erledigt sind. Abgrenzung zur Übergabe:
// Übergabe = etwas wissen, Aufgabe = etwas tun.

public enum AufgabeFuer { NaechsteSchicht, Schicht, WerZeitHat }

// Beginn der drei Schichten (Schritt 25: in den Einstellungen je Bereich). Die Nachtschicht geht
// über Mitternacht bis zum Beginn der Frühschicht.
public sealed record Schichtzeiten(TimeOnly Frueh, TimeOnly Spaet, TimeOnly Nacht)
{
    public static readonly Schichtzeiten Vorgabe = new(new TimeOnly(6, 0), new TimeOnly(14, 0), new TimeOnly(22, 0));

    public TimeOnly Beginn(Schicht s) => s switch { Schicht.Frueh => Frueh, Schicht.Spaet => Spaet, _ => Nacht };

    public string Text => $"Früh {Frueh:HH\\:mm} · Spät {Spaet:HH\\:mm} · Nacht {Nacht:HH\\:mm}";

    // Aus drei Uhrzeiten; Früh vor Spät vor Nacht (die Nacht beginnt vor Mitternacht).
    public static Schichtzeiten AusEingabe(string frueh, string spaet, string nacht)
    {
        var fehler = new List<string>();
        TimeOnly Lesen(string text, string name)
        {
            try { return BerichtRegeln.UhrzeitLesen(text); }
            catch (FormatException) { fehler.Add($"{name}: bitte eine Uhrzeit wie 06:00."); return default; }
        }
        var f = Lesen(frueh, "Frühdienst");
        var s = Lesen(spaet, "Spätdienst");
        var n = Lesen(nacht, "Nachtdienst");
        if (fehler.Count == 0 && !(f < s && s < n)) fehler.Add("Die Schichten bitte in der Reihenfolge Früh, Spät, Nacht (Nacht beginnt vor Mitternacht).");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Schichtzeiten(f, s, n);
    }
}

// Eine Schicht an einem Tag. Die Nachtschicht gehört zu dem Tag, an dem sie beginnt.
public readonly record struct SchichtTermin(DateOnly Tag, Schicht Schicht)
{
    public DateTime Beginn => Tag.ToDateTime(BerichtRegeln.Zeiten.Beginn(Schicht));

    public DateTime Ende => Schicht switch
    {
        Schicht.Frueh => Tag.ToDateTime(BerichtRegeln.Zeiten.Spaet),
        Schicht.Spaet => Tag.ToDateTime(BerichtRegeln.Zeiten.Nacht),
        _ => Tag.AddDays(1).ToDateTime(BerichtRegeln.Zeiten.Frueh),
    };

    public static SchichtTermin Zu(DateTime zeit)
    {
        var beginn = BerichtRegeln.SchichtBeginn(zeit);
        return new SchichtTermin(DateOnly.FromDateTime(beginn), BerichtRegeln.SchichtZu(beginn));
    }

    public SchichtTermin Naechste() => Zu(Ende);

    public string Text => $"{BerichtRegeln.SchichtText(Schicht)} {Tag.ToString("ddd dd.MM.", CultureInfo.GetCultureInfo("de-DE"))}";
}

// „An die nächste Schicht weitergereicht“, mit kurzer Notiz.
public sealed record Weitergabe(DateTime Zeit, string Kuerzel, string Notiz);

public sealed record Aufgabe
{
    public long Id { get; init; }
    public long? BewohnerId { get; init; }      // null = Aufgabe des Bereichs
    public string Bereich { get; init; } = "";  // mit Bewohner: dessen Wohnbereich (wechselt beim Umzug mit)
    public long? BerichtId { get; init; }       // aus einem Bericht heraus angelegt
    public string Text { get; init; } = "";
    public AufgabeFuer Fuer { get; init; }
    public SchichtTermin? FuerSchicht { get; init; }   // nur bei AufgabeFuer.Schicht
    public DateTime? FaelligBis { get; init; }
    public DateTime Erstellt { get; init; }
    public string Verfasser { get; init; } = "";
    public DateTime? Erledigt { get; init; }
    public string? ErledigtVon { get; init; }
    public string ErledigtNotiz { get; init; } = "";   // Schritt 25: begründet geschlossen statt erledigt
    public long? VorlageId { get; init; }              // Schritt 25: aus einer Standardaufgabe entstanden
    public long? VisiteAnordnungId { get; init; }      // Schritt 26: geänderte Anordnung einer Visite
    public long? VisiteEintragId { get; init; }        // Schritt 26: Facharzttermin aus einer Visite
    public IReadOnlyList<Weitergabe> Weitergaben { get; init; } = [];

    public bool Offen => Erledigt is null;
    public bool Geschlossen => Erledigt is not null && ErledigtNotiz.Length > 0;
}

// Standardaufgabe (Schritt 25): kehrt in einer Schicht an bestimmten Wochentagen wieder.
public sealed record AufgabeVorlage
{
    public long Id { get; init; }
    public string Text { get; init; } = "";
    public Schicht Schicht { get; init; }
    public IReadOnlyList<DayOfWeek> Tage { get; init; } = [];
    public TimeOnly? Uhrzeit { get; init; }
    public long? BewohnerId { get; init; }
    public string Bereich { get; init; } = "";   // ohne Bewohner: für welchen Bereich
    public bool Aktiv { get; init; } = true;
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
}

public sealed class VorlageEingabe
{
    public string Text { get; set; } = "";
    public Schicht Schicht { get; set; }
    public List<DayOfWeek> Tage { get; set; } = [];
    public string Uhrzeit { get; set; } = "";
    public long? BewohnerId { get; set; }
    public string Bereich { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

// Was im Fenster „Neue Aufgabe“ steht – noch ungeprüft, als Text.
public sealed class AufgabeEingabe
{
    public string Text { get; set; } = "";
    public long? BewohnerId { get; set; }
    public string Bereich { get; set; } = "";   // ohne Bewohner Pflicht
    public long? BerichtId { get; set; }
    public AufgabeFuer Fuer { get; set; } = AufgabeFuer.NaechsteSchicht;
    public string SchichtTag { get; set; } = "";
    public Schicht SchichtArt { get; set; } = Schicht.Frueh;
    public string FaelligDatum { get; set; } = "";
    public string FaelligUhrzeit { get; set; } = "";
    public string Verfasser { get; set; } = "";
}

public static class AufgabenRegeln
{
    private static readonly DayOfWeek[] Woche =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static AufgabeVorlage VorlageAusEingabe(VorlageEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Text)) fehler.Add("Was ist zu tun? Text fehlt.");
        if (e.Tage.Count == 0) fehler.Add("Bitte mindestens einen Wochentag wählen (oder „täglich“).");
        if (e.BewohnerId is null && string.IsNullOrWhiteSpace(e.Bereich)) fehler.Add(BereichFehlt);
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        TimeOnly? uhrzeit = null;
        if (e.Uhrzeit.Trim().Length > 0)
        {
            try { uhrzeit = BerichtRegeln.UhrzeitLesen(e.Uhrzeit); }
            catch (FormatException x) { fehler.Add(x.Message); }
        }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new AufgabeVorlage
        {
            Text = e.Text.Trim(), Schicht = e.Schicht, Tage = Woche.Where(e.Tage.Contains).ToList(), Uhrzeit = uhrzeit,
            BewohnerId = e.BewohnerId, Bereich = e.BewohnerId is null ? e.Bereich.Trim() : "", Erstellt = jetzt,
            Kuerzel = e.Kuerzel.Trim(),
        };
    }

    public static string TageText(IReadOnlyCollection<DayOfWeek> tage) =>
        tage.Count == 7 ? "täglich"
        : string.Join(", ", Woche.Where(tage.Contains).Select(t => CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetAbbreviatedDayName(t)));

    public static int TageMaske(IEnumerable<DayOfWeek> tage) => tage.Aggregate(0, (m, t) => m | (1 << (int)t));
    public static List<DayOfWeek> TageAus(int maske) => Woche.Where(t => (maske & (1 << (int)t)) != 0).ToList();

    // Gilt die Vorlage in dieser Schicht? (Wochentag: der Tag, an dem die Schicht beginnt.)
    public static bool Gilt(AufgabeVorlage v, SchichtTermin s) => v.Aktiv && v.Schicht == s.Schicht && v.Tage.Contains(s.Tag.DayOfWeek);

    // Fällig: die Uhrzeit innerhalb der Schicht (Nacht: nach Mitternacht am Folgetag), sonst das Schichtende.
    public static DateTime FaelligIn(AufgabeVorlage v, SchichtTermin s)
    {
        if (v.Uhrzeit is not TimeOnly u) return s.Ende;
        var zeit = s.Tag.ToDateTime(u);
        return zeit < s.Beginn ? zeit.AddDays(1) : zeit;
    }

    // „aus der Nachtschicht offen“: eine Standardaufgabe einer vergangenen Schicht, noch offen.
    public static string? AusSchichtOffen(Aufgabe a, DateTime jetzt) =>
        a.Offen && a.VorlageId is not null && a.FuerSchicht is SchichtTermin s && s.Ende <= jetzt
            ? $"aus der {BerichtRegeln.SchichtText(s.Schicht).Replace("dienst", "schicht")} offen" : null;

    // Gruppen der Aufgabenliste: oben die laufende Schicht (mit Liegengebliebenem davor), dann die
    // folgenden Schichten, zuletzt „wer Zeit hat“.
    public static List<(string Titel, List<Aufgabe> Aufgaben)> Gruppen(IEnumerable<Aufgabe> offen, DateTime jetzt)
    {
        var jetztSchicht = SchichtTermin.Zu(jetzt);
        var liste = Sortiert(offen, jetzt);
        var gruppen = new List<(string, List<Aufgabe>)>();
        var frueher = liste.Where(a => Zielschicht(a) is SchichtTermin s && s.Ende <= jetzt).ToList();
        if (frueher.Count > 0) gruppen.Add(("AUS FRÜHEREN SCHICHTEN OFFEN", frueher));
        gruppen.Add(($"JETZT: {jetztSchicht.Text.ToUpperInvariant()}", liste.Where(a => Zielschicht(a) == jetztSchicht).ToList()));
        foreach (var g in liste.Where(a => Zielschicht(a) is SchichtTermin s && s.Beginn >= jetztSchicht.Ende)
                               .GroupBy(a => Zielschicht(a)!.Value).OrderBy(g => g.Key.Beginn))
            gruppen.Add((g.Key.Text.ToUpperInvariant(), g.ToList()));
        var wer = liste.Where(a => Zielschicht(a) is null).ToList();
        if (wer.Count > 0) gruppen.Add(("WER ZEIT HAT", wer));
        return gruppen;
    }

    public static readonly string[] FuerTexte = ["nächste Schicht", "bestimmte Schicht …", "wer Zeit hat"];

    // Fachregel: IDEEN.md „Aufgaben gehören zu einem Bewohner oder einem Bereich“ (kein „ganzes Haus“).
    public const string BereichFehlt = "Bitte einen Bewohner oder einen Bereich wählen.";

    // Baut eine Aufgabe aus den Eingaben. Wirft Eingabefehler mit allen Fehlern auf einmal.
    public static Aufgabe AusEingabe(AufgabeEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Text)) fehler.Add("Was ist zu tun? Text fehlt.");
        if (e.BewohnerId is null && string.IsNullOrWhiteSpace(e.Bereich)) fehler.Add(BereichFehlt);
        if (string.IsNullOrWhiteSpace(e.Verfasser)) fehler.Add("Verfasser (Kürzel) fehlt.");
        else if (Einstellungen.KuerzelPruefen(e.Verfasser) is string f) fehler.Add(f.Replace("Kürzel", "Verfasser"));

        SchichtTermin? schicht = null;
        if (e.Fuer == AufgabeFuer.Schicht)
        {
            try
            {
                var termin = new SchichtTermin(Datum.Lesen(e.SchichtTag, "Tag der Schicht"), e.SchichtArt);
                if (termin.Ende <= jetzt) fehler.Add("Diese Schicht ist schon vorbei.");
                schicht = termin;
            }
            catch (FormatException x) { fehler.Add(x.Message); }
        }

        DateTime? faellig = null;
        if (e.FaelligDatum.Trim().Length > 0 || e.FaelligUhrzeit.Trim().Length > 0)
        {
            try
            {
                var tag = Datum.Lesen(e.FaelligDatum, "Fällig bis");
                // Ohne Uhrzeit gilt der ganze Tag.
                var zeit = e.FaelligUhrzeit.Trim().Length == 0 ? new TimeOnly(23, 59) : BerichtRegeln.UhrzeitLesen(e.FaelligUhrzeit);
                faellig = tag.ToDateTime(zeit);
                if (faellig < jetzt) fehler.Add("„Fällig bis“ liegt in der Vergangenheit.");
            }
            catch (FormatException x) { fehler.Add(x.Message.StartsWith("Uhrzeit") ? "Fällig bis, " + x.Message : x.Message); }
        }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Aufgabe
        {
            BewohnerId = e.BewohnerId,
            Bereich = e.BewohnerId is null ? e.Bereich.Trim() : "",
            BerichtId = e.BerichtId,
            Text = e.Text.Trim(),
            Fuer = e.Fuer,
            FuerSchicht = schicht,
            FaelligBis = faellig,
            Erstellt = jetzt,
            Verfasser = e.Verfasser.Trim(),
        };
    }

    // Für welche Schicht die Aufgabe gerade gilt. „Nächste Schicht“ rechnet ab der letzten
    // Weitergabe (oder ab dem Anlegen). „Wer Zeit hat“: keine bestimmte Schicht.
    public static SchichtTermin? Zielschicht(Aufgabe a)
    {
        if (a.Weitergaben.Count > 0) return SchichtTermin.Zu(a.Weitergaben[^1].Zeit).Naechste();
        return a.Fuer switch
        {
            AufgabeFuer.NaechsteSchicht => SchichtTermin.Zu(a.Erstellt).Naechste(),
            AufgabeFuer.Schicht => a.FuerSchicht,
            _ => null,
        };
    }

    public static string FuerText(Aufgabe a) =>
        Zielschicht(a) is SchichtTermin s ? $"für {s.Text}" : "wer Zeit hat";

    public static bool Ueberfaellig(Aufgabe a, DateTime jetzt) => a.Offen && a.FaelligBis is DateTime f && f < jetzt;

    // Gilt die Aufgabe für die Schicht, die gerade läuft (oder ist sie aus einer früheren liegen geblieben)?
    public static bool JetztDran(Aufgabe a, DateTime jetzt) =>
        a.Offen && Zielschicht(a) is SchichtTermin s && s.Beginn <= jetzt;

    public static string FaelligText(Aufgabe a, DateTime jetzt)
    {
        if (a.FaelligBis is not DateTime f) return "";
        var de = CultureInfo.GetCultureInfo("de-DE");
        string wann = f.TimeOfDay >= new TimeSpan(23, 59, 0) ? f.ToString("ddd dd.MM.", de) : f.ToString("ddd dd.MM., HH:mm 'Uhr'", de);
        return Ueberfaellig(a, jetzt) ? $"überfällig seit {wann}" : $"fällig bis {wann}";
    }

    // Reihenfolge der Liste: überfällig zuerst, dann was jetzt dran ist, dann nach Fälligkeit, dann nach Alter.
    public static List<Aufgabe> Sortiert(IEnumerable<Aufgabe> aufgaben, DateTime jetzt) =>
        aufgaben.OrderByDescending(a => Ueberfaellig(a, jetzt))
                .ThenByDescending(a => JetztDran(a, jetzt))
                .ThenBy(a => a.FaelligBis ?? DateTime.MaxValue)
                .ThenBy(a => a.Erstellt)
                .ToList();

    public static string FuerKuerzel(AufgabeFuer f) => f switch
    {
        AufgabeFuer.NaechsteSchicht => "naechste_schicht",
        AufgabeFuer.Schicht => "schicht",
        _ => "wer_zeit_hat",
    };

    public static AufgabeFuer FuerAus(string kuerzel) => kuerzel switch
    {
        "naechste_schicht" => AufgabeFuer.NaechsteSchicht,
        "schicht" => AufgabeFuer.Schicht,
        _ => AufgabeFuer.WerZeitHat,
    };
}
