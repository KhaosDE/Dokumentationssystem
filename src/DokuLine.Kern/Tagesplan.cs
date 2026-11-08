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
