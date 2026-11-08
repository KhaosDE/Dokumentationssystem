namespace DokuLine.Kern;

// Nachrichten zwischen Kolleginnen und Kollegen – an Personen und/oder Teams. Alles bleibt in
// der Datenbank des Programms: kein E-Mail-Versand, kein Internet (CLAUDE.md, Regel 6).
// Nachrichten werden nicht gelöscht, nur archiviert.

// Ausgedachte Kollegin oder ausgedachter Kollege der Demo, mit Kürzel und Teams.
public sealed record Kollege(string Kuerzel, string Vorname, string Nachname, IReadOnlyList<string> Teams)
{
    public string Name => $"{Vorname} {Nachname}";
    public string Anzeige => $"{Name} ({Kuerzel})";
}

// Ein Empfänger: eine Person (Kürzel) oder ein Team (Name).
public sealed record Empfaenger(bool IstTeam, string Ziel)
{
    public static Empfaenger Person(string kuerzel) => new(false, kuerzel);
    public static Empfaenger Team(string name) => new(true, name);
}

public sealed record Nachricht
{
    public long Id { get; init; }
    public long? VerlaufId { get; init; }   // null = erste Nachricht des Verlaufs
    public string Von { get; init; } = "";
    public string Betreff { get; init; } = "";
    public string Text { get; init; } = "";
    public DateTime Gesendet { get; init; }
    public long? BewohnerId { get; init; }
    public IReadOnlyList<Empfaenger> An { get; init; } = [];
    public IReadOnlyList<string> GelesenVon { get; init; } = [];

    public long Verlauf => VerlaufId ?? Id;
}

// Ein Verlauf (erste Nachricht und alle Antworten), aus Sicht einer Person.
public sealed record Verlauf
{
    public long Id { get; init; }
    public string Betreff { get; init; } = "";
    public long? BewohnerId { get; init; }
    public IReadOnlyList<Nachricht> Nachrichten { get; init; } = [];
    public int Ungelesen { get; init; }
    public bool Archiviert { get; init; }

    public DateTime Letzte => Nachrichten[^1].Gesendet;
}

public enum NachrichtenOrdner { Posteingang, Gesendet, Archiv }

public static class NachrichtenRegeln
{
    public const int BetreffLaenge = 120;

    public static string OrdnerText(NachrichtenOrdner o) => o switch
    {
        NachrichtenOrdner.Posteingang => "Posteingang",
        NachrichtenOrdner.Gesendet => "Gesendet",
        _ => "Archiv",
    };

    // Ist „ich“ Empfänger – direkt oder über eines meiner Teams?
    public static bool AnMich(Nachricht n, Kollege ich) =>
        n.An.Any(e => e.IstTeam ? ich.Teams.Contains(e.Ziel) : e.Ziel == ich.Kuerzel);

    // Darf „ich“ diesen Verlauf sehen? Nur wer geschrieben hat oder angeschrieben wurde.
    public static bool Beteiligt(IEnumerable<Nachricht> verlauf, Kollege ich) =>
        verlauf.Any(n => n.Von == ich.Kuerzel || AnMich(n, ich));

    public static bool Ungelesen(Nachricht n, Kollege ich) =>
        n.Von != ich.Kuerzel && AnMich(n, ich) && !n.GelesenVon.Contains(ich.Kuerzel);

    // Antwort geht an alle Beteiligten außer mir: Absender und Personen, dazu die Teams.
    public static List<Empfaenger> AntwortAn(IEnumerable<Nachricht> verlauf, string ich)
    {
        var liste = new List<Empfaenger>();
        foreach (var n in verlauf)
        {
            liste.Add(Empfaenger.Person(n.Von));
            liste.AddRange(n.An);
        }
        return liste.Where(e => e.IstTeam || e.Ziel != ich).Distinct().ToList();
    }

    public static string EmpfaengerText(IEnumerable<Empfaenger> an, IReadOnlyDictionary<string, Kollege> kollegen) =>
        string.Join(", ", an.OrderByDescending(e => e.IstTeam).Select(e => e.IstTeam ? $"Team {e.Ziel}"
            : kollegen.TryGetValue(e.Ziel, out var k) ? k.Name : e.Ziel));

    // Prüft eine neue Nachricht; wirft Eingabefehler mit allen Fehlern auf einmal.
    public static void Pruefen(Nachricht n, IReadOnlyDictionary<string, Kollege> kollegen, IEnumerable<string> teams, bool antwort)
    {
        var fehler = new List<string>();
        if (!kollegen.ContainsKey(n.Von)) fehler.Add("Bitte oben rechts unter „Angemeldet als …“ einen Benutzer wählen.");
        if (n.An.Count == 0) fehler.Add("An wen? Bitte mindestens eine Person oder ein Team wählen.");
        var teamListe = teams.ToHashSet();
        foreach (var e in n.An)
            if (e.IstTeam ? !teamListe.Contains(e.Ziel) : !kollegen.ContainsKey(e.Ziel))
                fehler.Add($"Unbekannter Empfänger: {e.Ziel}.");
        if (!antwort && string.IsNullOrWhiteSpace(n.Betreff)) fehler.Add("Betreff fehlt.");
        if (n.Betreff.Trim().Length > BetreffLaenge) fehler.Add($"Der Betreff ist zu lang (höchstens {BetreffLaenge} Zeichen).");
        if (string.IsNullOrWhiteSpace(n.Text)) fehler.Add("Text fehlt.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
    }
}
