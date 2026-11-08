namespace DokuLine.Kern;

// Tagesplan (Bauplan Schritt 23): alle Termine eines Tages für die Bewohner eines Bereichs – nach
// Uhrzeit oder nach Bewohner. Bei Therapien die Teilnahme „gegangen“, „abgesagt“, „ausgefallen“;
// das ist derselbe Datensatz wie im Wochenplan („gegangen“ = „teilgenommen“), nicht doppelt.
// „Wo müsste er sein?“: Nach „gegangen“ steht der Bewohner bis zum Ende des Termins als „in
// Therapie: … bis …“ auf Karte und Akte; danach automatisch wieder im Haus.

// Die zuletzt eingetragene Teilnahme an einem Tag: Status, wann und von wem.
public sealed record TeilnahmeEintrag(Teilnahme Status, DateTime Erstellt, string Kuerzel);

public sealed record TagesplanZeile(Bewohner Bewohner, Termin Termin, TeilnahmeEintrag? Eintrag)
{
    // Nur Therapien haben eine Teilnahme (wie im Wochenplan).
    public bool Therapie => Termin.Quelle == TerminQuelle.Therapie && Termin.ZuteilungId is not null;
    public Teilnahme Status => Eintrag?.Status ?? Teilnahme.Offen;
}

public static class TagesplanRegeln
{
    public const string NochOffen = "noch offen";

    // Im Tagesplan heißt „teilgenommen“ „gegangen“.
    public static string StatusText(Teilnahme t) => t == Teilnahme.Teilgenommen ? "gegangen" : WochenplanRegeln.TeilnahmeText(t);

    // Marke einer Zeile: „gegangen 09:05 · CD“, „abgesagt 08:40 · AB“, „noch offen“ (Therapie hat
    // begonnen, nichts bestätigt), sonst null.
    public static string? Marke(TagesplanZeile z, DateTime jetzt)
    {
        if (!z.Therapie) return null;
        if (z.Eintrag is TeilnahmeEintrag e && e.Status != Teilnahme.Offen) return $"{StatusText(e.Status)} {e.Erstellt:HH\\:mm} · {e.Kuerzel}";
        var heute = DateOnly.FromDateTime(jetzt);
        bool begonnen = z.Termin.Tag < heute || (z.Termin.Tag == heute && (z.Termin.Ganztags || TimeOnly.FromDateTime(jetzt) >= z.Termin.Von));
        return begonnen ? NochOffen : null;
    }

    public static List<TagesplanZeile> NachUhrzeit(IEnumerable<TagesplanZeile> zeilen) =>
        zeilen.OrderBy(z => z.Termin.Ganztags ? TimeOnly.MinValue : z.Termin.Von).ThenBy(z => z.Bewohner.Nachname, StringComparer.CurrentCulture).ToList();

    public static List<TagesplanZeile> NachBewohner(IEnumerable<TagesplanZeile> zeilen) =>
        zeilen.OrderBy(z => z.Bewohner.Nachname, StringComparer.CurrentCulture).ThenBy(z => z.Bewohner.Vorname, StringComparer.CurrentCulture)
              .ThenBy(z => z.Termin.Ganztags ? TimeOnly.MinValue : z.Termin.Von).ToList();

    public static string InTherapieText(string titel, TimeOnly bis) => $"in Therapie: {titel} bis {bis:HH\\:mm}";
}

// Tagesplan als Anzeigetafel (Bauplan Schritt 26g), nach dem Vorbild einer Abflugtafel: Status als Wort
// (die Farbe kommt dazu, nie allein) und drei Blöcke – „Jetzt und gleich“ oben, „Später heute“,
// „Erledigt“ unten.
public enum TafelStatus { Jetzt, Gleich, Geplant, Gegangen, NochOffen, Abgesagt, Ausgefallen, Zurueck, Vorbei }

public enum TafelBlock { JetztUndGleich, Spaeter, Erledigt }

public sealed record TafelZeile(TagesplanZeile Zeile, TafelStatus Status, TafelBlock Block)
{
    public Bewohner Bewohner => Zeile.Bewohner;
    public Termin Termin => Zeile.Termin;
}

public static class TafelRegeln
{
    public const int GleichMinuten = 30;   // „GLEICH“: beginnt in den nächsten 30 Minuten

    public static string Wort(TafelStatus s) => s switch
    {
        TafelStatus.Jetzt => "JETZT",
        TafelStatus.Gleich => "GLEICH",
        TafelStatus.Geplant => "GEPLANT",
        TafelStatus.Gegangen => "GEGANGEN",
        TafelStatus.NochOffen => "NOCH OFFEN",
        TafelStatus.Abgesagt => "ABGESAGT",
        TafelStatus.Ausgefallen => "AUSGEFALLEN",
        TafelStatus.Zurueck => "ZURÜCK",
        _ => "VORBEI",
    };

    public static string BlockText(TafelBlock b) => b switch
    {
        TafelBlock.JetztUndGleich => "JETZT UND GLEICH",
        TafelBlock.Spaeter => "SPÄTER HEUTE",
        _ => "ERLEDIGT",
    };

    // Ein Termin von heute: Status und Block nach Uhrzeit und (bei Therapien) Teilnahme.
    // Therapien: „gegangen“ bis zum Ende, danach „zurück“; „abgesagt“ und „ausgefallen“ sind erledigt;
    // ohne Bestätigung nach Beginn „noch offen“ – das bleibt oben, bis jemand es bestätigt.
    // Andere Termine: „jetzt“ während, „vorbei“ danach. Ganztags läuft den ganzen Tag.
    public static (TafelStatus Status, TafelBlock Block) Einordnen(TagesplanZeile z, DateTime jetzt)
    {
        var t = z.Termin;
        var uhr = TimeOnly.FromDateTime(jetzt);
        var von = t.Ganztags ? TimeOnly.MinValue : t.Von;
        var bis = t.Ganztags ? TimeOnly.MaxValue : t.Bis;
        bool vorbei = t.Tag < DateOnly.FromDateTime(jetzt) || (t.Tag == DateOnly.FromDateTime(jetzt) && uhr >= bis);
        bool begonnen = vorbei || (t.Tag == DateOnly.FromDateTime(jetzt) && uhr >= von);
        bool gleich = !begonnen && t.Tag == DateOnly.FromDateTime(jetzt) && (von.ToTimeSpan() - uhr.ToTimeSpan()).TotalMinutes <= GleichMinuten;
        if (z.Therapie)
            return z.Status switch
            {
                Teilnahme.Abgesagt => (TafelStatus.Abgesagt, TafelBlock.Erledigt),
                Teilnahme.Ausgefallen => (TafelStatus.Ausgefallen, TafelBlock.Erledigt),
                Teilnahme.Teilgenommen => vorbei ? (TafelStatus.Zurueck, TafelBlock.Erledigt) : (TafelStatus.Gegangen, TafelBlock.JetztUndGleich),
                _ => begonnen ? (TafelStatus.NochOffen, TafelBlock.JetztUndGleich)
                   : gleich ? (TafelStatus.Gleich, TafelBlock.JetztUndGleich) : (TafelStatus.Geplant, TafelBlock.Spaeter),
            };
        if (vorbei) return (TafelStatus.Vorbei, TafelBlock.Erledigt);
        if (begonnen) return (TafelStatus.Jetzt, TafelBlock.JetztUndGleich);
        return gleich ? (TafelStatus.Gleich, TafelBlock.JetztUndGleich) : (TafelStatus.Geplant, TafelBlock.Spaeter);
    }

    // Die Tafel: erst „Jetzt und gleich“, dann „Später heute“, dann „Erledigt“; darin nach Uhrzeit.
    public static List<TafelZeile> Tafel(IEnumerable<TagesplanZeile> zeilen, DateTime jetzt) =>
        TagesplanRegeln.NachUhrzeit(zeilen).Select(z => Zeile(z, jetzt))
            .OrderBy(x => x.Block).ToList();   // OrderBy ist stabil: die Uhrzeit-Reihenfolge bleibt im Block

    // „nach Bewohner“: dieselben Zeilen mit Status, nach Bewohner und Uhrzeit (ohne Blöcke).
    public static List<TafelZeile> NachBewohner(IEnumerable<TagesplanZeile> zeilen, DateTime jetzt) =>
        TagesplanRegeln.NachBewohner(zeilen).Select(z => Zeile(z, jetzt)).ToList();

    private static TafelZeile Zeile(TagesplanZeile z, DateTime jetzt)
    {
        var (status, block) = Einordnen(z, jetzt);
        return new TafelZeile(z, status, block);
    }

    // Kleiner Zusatz in der Status-Spalte: wann und von wem bestätigt („09:05 · CD“).
    public static string Bestaetigt(TagesplanZeile z) =>
        z.Therapie && z.Eintrag is TeilnahmeEintrag e && e.Status != Teilnahme.Offen ? $"{e.Erstellt:HH\\:mm} · {e.Kuerzel}" : "";
}
