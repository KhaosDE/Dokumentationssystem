using System.Globalization;

namespace Dokusystem.Kern;

// Wohnbereich-ToDo: Aufgaben, die an die nächste Schicht (oder eine bestimmte Schicht, oder an
// „wer Zeit hat“) gehen und stehen bleiben, bis sie erledigt sind. Abgrenzung zur Übergabe:
// Übergabe = etwas wissen, Aufgabe = etwas tun.

public enum AufgabeFuer { NaechsteSchicht, Schicht, WerZeitHat }

// Eine Schicht an einem Tag. Die Nachtschicht gehört zu dem Tag, an dem sie um 22 Uhr beginnt.
public readonly record struct SchichtTermin(DateOnly Tag, Schicht Schicht)
{
    public DateTime Beginn => Tag.ToDateTime(Schicht switch
    {
        Schicht.Frueh => new TimeOnly(6, 0),
        Schicht.Spaet => new TimeOnly(14, 0),
        _ => new TimeOnly(22, 0),
    });

    public DateTime Ende => Beginn.AddHours(8);

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
    public long? BewohnerId { get; init; }      // null = betrifft keinen einzelnen Bewohner
    public long? BerichtId { get; init; }       // aus einem Bericht heraus angelegt
    public string Text { get; init; } = "";
    public AufgabeFuer Fuer { get; init; }
    public SchichtTermin? FuerSchicht { get; init; }   // nur bei AufgabeFuer.Schicht
    public DateTime? FaelligBis { get; init; }
    public DateTime Erstellt { get; init; }
    public string Verfasser { get; init; } = "";
    public DateTime? Erledigt { get; init; }
    public string? ErledigtVon { get; init; }
    public IReadOnlyList<Weitergabe> Weitergaben { get; init; } = [];

    public bool Offen => Erledigt is null;
}

// Was im Fenster „Neue Aufgabe“ steht – noch ungeprüft, als Text.
public sealed class AufgabeEingabe
{
    public string Text { get; set; } = "";
    public long? BewohnerId { get; set; }
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
    public static readonly string[] FuerTexte = ["nächste Schicht", "bestimmte Schicht …", "wer Zeit hat"];

    // Baut eine Aufgabe aus den Eingaben. Wirft Eingabefehler mit allen Fehlern auf einmal.
    public static Aufgabe AusEingabe(AufgabeEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Text)) fehler.Add("Was ist zu tun? Text fehlt.");
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
