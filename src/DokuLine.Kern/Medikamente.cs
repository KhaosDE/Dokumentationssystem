namespace DokuLine.Kern;

// Medikamentendatenbank: eine vorgefertigte Liste, aus der beim Verordnen gewählt wird.
// Echte Wirkstoff- und Handelsnamen sind hier erlaubt – das sind Nachschlagedaten, keine
// Bewohnerdaten (CLAUDE.md, Regel 1). Demo-Bestand: nicht zum Verordnen verwenden.
// Jede Änderung ist eine neue Fassung mit Datum und Kürzel; frühere Fassungen bleiben einsehbar.

public enum Pruefstatus { Ungeprueft, Bestaetigt }

public sealed record MedikamentFassung
{
    public string Wirkstoff { get; init; } = "";
    public IReadOnlyList<string> Handelsnamen { get; init; } = [];
    public IReadOnlyList<string> Staerken { get; init; } = [];
    public string Darreichungsform { get; init; } = "";
    public string Quelle { get; init; } = "";
    public string Zulassungsnummer { get; init; } = "";   // Schritt 21: optional, damit ein Eintrag eindeutig wiederzufinden ist
    public Pruefstatus Pruefstatus { get; init; } = Pruefstatus.Ungeprueft;
    public DateOnly? GeprueftAm { get; init; }   // Schritt 21: wann gegen die Quelle geprüft (bei „bestätigt“)
    public bool Gesperrt { get; init; }           // Schritt 21: gesperrt = nicht mehr wählbar
    public DateTime Geaendert { get; init; }
    public string Kuerzel { get; init; } = "";
}

public sealed record Medikament
{
    public long Id { get; init; }
    public bool SelbstAngelegt { get; init; }
    public IReadOnlyList<MedikamentFassung> Fassungen { get; init; } = [];   // älteste zuerst

    public MedikamentFassung Aktuell => Fassungen[^1];
    public string Wirkstoff => Aktuell.Wirkstoff;
    public bool Gesperrt => Aktuell.Gesperrt;
    public bool Geprueft => Aktuell.Pruefstatus == Pruefstatus.Bestaetigt;

    // Für die Auswahl beim Verordnen: Ungeprüftes ist gekennzeichnet.
    public string Auswahltext => Anzeige + (Geprueft ? "" : " – ungeprüft");

    // „Quetiapin (Seroquel) · Filmtablette“ – für Auswahllisten.
    public string Anzeige =>
        Aktuell.Wirkstoff +
        (Aktuell.Handelsnamen.Count > 0 ? $" ({string.Join(", ", Aktuell.Handelsnamen)})" : "") +
        (Aktuell.Darreichungsform.Length > 0 ? $" · {Aktuell.Darreichungsform}" : "");
}

// Was im Fenster „Medikament“ steht – noch ungeprüft, als Text.
public sealed class MedikamentEingabe
{
    public string Wirkstoff { get; set; } = "";
    public string Handelsnamen { get; set; } = "";   // durch Komma getrennt
    public string Staerken { get; set; } = "";       // durch Komma getrennt
    public string Darreichungsform { get; set; } = "";
    public string Quelle { get; set; } = "";
    public string Zulassungsnummer { get; set; } = "";
    public bool Bestaetigt { get; set; }
    public string GeprueftAm { get; set; } = "";   // TT.MM.JJJJ, Pflicht bei „bestätigt“
    public bool Gesperrt { get; set; }
    public string Kuerzel { get; set; } = "";
}

public static partial class MedikamentRegeln
{
    public const int ZulassungsnummerLaenge = 30;

    [System.Text.RegularExpressions.GeneratedRegex(@"^[\p{L}\d./\- ]*$")]
    private static partial System.Text.RegularExpressions.Regex ZulassungsnummerMuster();

    public const string DemoHinweis = "Demo-Bestand, nicht zum Verordnen verwenden";

    public static IReadOnlyList<string> Liste(string text) =>
        text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();

    public static MedikamentEingabe AlsEingabe(MedikamentFassung f) => new()
    {
        Wirkstoff = f.Wirkstoff,
        Handelsnamen = string.Join(", ", f.Handelsnamen),
        Staerken = string.Join(", ", f.Staerken),
        Darreichungsform = f.Darreichungsform,
        Quelle = f.Quelle,
        Zulassungsnummer = f.Zulassungsnummer,
        Bestaetigt = f.Pruefstatus == Pruefstatus.Bestaetigt,
        GeprueftAm = Datum.Text(f.GeprueftAm),
        Gesperrt = f.Gesperrt,
    };

    // Baut eine Fassung; wirft Eingabefehler mit allen Fehlern auf einmal.
    public static MedikamentFassung AusEingabe(MedikamentEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Wirkstoff)) fehler.Add("Wirkstoff fehlt.");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        string quelle = e.Quelle.Trim();
        if (quelle.Length > 0 && !IstAdresse(quelle))
            fehler.Add("Quelle: bitte als vollständige Internetadresse angeben (wie im Browser).");
        if (e.Bestaetigt && quelle.Length == 0) fehler.Add("„Bestätigt“ geht nur mit Quelle.");
        string zulassung = e.Zulassungsnummer.Trim();
        if (zulassung.Length > ZulassungsnummerLaenge || !ZulassungsnummerMuster().IsMatch(zulassung))
            fehler.Add($"Zulassungsnummer: höchstens {ZulassungsnummerLaenge} Zeichen, nur Ziffern, Buchstaben, Punkt, Bindestrich oder Schrägstrich.");
        DateOnly? geprueft = null;
        if (e.Bestaetigt)
        {
            try
            {
                geprueft = Datum.Lesen(e.GeprueftAm, "Geprüft am");
                if (geprueft > DateOnly.FromDateTime(jetzt)) fehler.Add("„Geprüft am“ liegt in der Zukunft.");
            }
            catch (FormatException x) { fehler.Add(x.Message); }
        }
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new MedikamentFassung
        {
            Wirkstoff = e.Wirkstoff.Trim(),
            Handelsnamen = Liste(e.Handelsnamen),
            Staerken = Liste(e.Staerken),
            Darreichungsform = e.Darreichungsform.Trim(),
            Quelle = quelle,
            Zulassungsnummer = zulassung,
            Pruefstatus = e.Bestaetigt ? Pruefstatus.Bestaetigt : Pruefstatus.Ungeprueft,
            GeprueftAm = geprueft,
            Gesperrt = e.Gesperrt,
            Geaendert = jetzt,
            Kuerzel = e.Kuerzel.Trim(),
        };
    }

    // Nur eine Prüfung der Schreibweise; das Programm ruft die Adresse nie auf (Regel 6).
    public static bool IstAdresse(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var adresse) && (adresse.Scheme == Uri.UriSchemeHttps || adresse.Scheme == Uri.UriSchemeHttp);

    // Suche in Wirkstoff, Handelsnamen und Darreichungsform, Groß-/Kleinschreibung egal.
    public static bool Passt(Medikament m, string suche)
    {
        suche = suche.Trim();
        if (suche.Length == 0) return true;
        var f = m.Aktuell;
        return new[] { f.Wirkstoff, f.Darreichungsform, f.Zulassungsnummer }.Concat(f.Handelsnamen)
            .Any(t => t.Contains(suche, StringComparison.CurrentCultureIgnoreCase));
    }

    public static string PruefText(MedikamentFassung f) =>
        f.Pruefstatus == Pruefstatus.Bestaetigt ? (f.GeprueftAm is DateOnly d ? $"geprüft am {Datum.Text(d)} von {f.Kuerzel}" : "bestätigt") : "ungeprüft";

    // Was sich zwischen zwei Fassungen geändert hat, in Worten (für den Verlauf).
    public static List<string> Unterschiede(MedikamentFassung alt, MedikamentFassung neu)
    {
        var liste = new List<string>();
        void Vergleich(string feld, string a, string b) { if (a != b) liste.Add($"{feld}: „{a}“ → „{b}“"); }
        Vergleich("Wirkstoff", alt.Wirkstoff, neu.Wirkstoff);
        Vergleich("Handelsnamen", string.Join(", ", alt.Handelsnamen), string.Join(", ", neu.Handelsnamen));
        Vergleich("Stärken", string.Join(", ", alt.Staerken), string.Join(", ", neu.Staerken));
        Vergleich("Darreichungsform", alt.Darreichungsform, neu.Darreichungsform);
        Vergleich("Quelle", alt.Quelle, neu.Quelle);
        Vergleich("Zulassungsnummer", alt.Zulassungsnummer, neu.Zulassungsnummer);
        Vergleich("Prüfstatus", PruefText(alt), PruefText(neu));
        if (alt.Gesperrt != neu.Gesperrt) liste.Add(neu.Gesperrt ? "gesperrt" : "wieder freigegeben");
        return liste;
    }
}
