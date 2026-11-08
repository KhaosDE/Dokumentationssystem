namespace DokuLine.Kern;

// Ärzteverzeichnis (Bauplan Schritt 26b): eine Liste der Ärztinnen und Ärzte, die überall zur Auswahl
// steht (Visite, Verordnung, Anordnung, Facharzttermin, Termine). Gelöscht wird nicht – wer nicht
// mehr gebraucht wird, wird inaktiv gesetzt (frühere Einträge behalten den Namen). Nur ausgedachte
// Angaben; E-Mail-Adressen in der Demo nur mit @example.org.
public sealed record Arzt
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Fachrichtung { get; init; } = "";
    public string Praxis { get; init; } = "";      // Praxis bzw. Ort
    public string Telefon { get; init; } = "";
    public string Email { get; init; } = "";
    public bool Aktiv { get; init; } = true;

    // „Dr. Ulme (ausgedacht) · Allgemeinmedizin“ – für Auswahllisten.
    public string Anzeige => Name + (Fachrichtung.Length > 0 ? $" · {Fachrichtung}" : "");
}

public sealed class ArztEingabe
{
    public string Name { get; set; } = "";
    public string Fachrichtung { get; set; } = "";
    public string Praxis { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string Email { get; set; } = "";
}

public static class ArztRegeln
{
    public static Arzt AusEingabe(ArztEingabe e, long id = 0, bool aktiv = true)
    {
        var fehler = new List<string>();
        if (e.Name.Trim().Length == 0) fehler.Add("Name fehlt (ausgedacht, z. B. „Dr. Ulme“).");
        if (e.Email.Trim().Length > 0 && !EmailGueltig(e.Email.Trim())) fehler.Add("E-Mail-Adresse bitte so: name@beispiel.de (ohne Leerzeichen).");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Arzt
        {
            Id = id, Name = e.Name.Trim(), Fachrichtung = e.Fachrichtung.Trim(), Praxis = e.Praxis.Trim(), Telefon = e.Telefon.Trim(),
            Email = e.Email.Trim(), Aktiv = aktiv,
        };
    }

    public static ArztEingabe AlsEingabe(Arzt a) => new() { Name = a.Name, Fachrichtung = a.Fachrichtung, Praxis = a.Praxis, Telefon = a.Telefon, Email = a.Email };

    // Einfache Prüfung: ein @, davor etwas, danach ein Name mit Punkt; keine Leerzeichen.
    public static bool EmailGueltig(string email)
    {
        if (email.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '?' or '&' or ',' or ';' or '<' or '>' or '"')) return false;
        int at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@')) return false;
        string domain = email[(at + 1)..];
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }

    // Für den Hinweis eines Termins: „Dr. Esche (ausgedacht), Praxis am Markt, Tel. 0000 …“
    public static string Kontakt(Arzt a) =>
        string.Join(", ", new[] { a.Name, a.Praxis, a.Telefon.Length > 0 ? $"Tel. {a.Telefon}" : "" }.Where(t => t.Length > 0));
}
