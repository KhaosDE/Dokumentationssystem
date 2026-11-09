namespace DokuLine.Kern;

// Betreuer eines Bewohners (Bauplan Schritt 28): Name, Stelle, E-Mail, Telefon. Jede Änderung ist
// eine neue Fassung (wer, wann); gezeigt wird die neueste. „E-Mail an Betreuer …“ übergibt nur
// Empfänger und Betreff an das E-Mail-Programm des Rechners (mailto:) – das Programm selbst sendet
// nichts (CLAUDE.md, Regel 6). Demo-Adressen nur mit der Endung @example.org.
public sealed record Betreuer
{
    public string Name { get; init; } = "";
    public string Stelle { get; init; } = "";
    public string Email { get; init; } = "";
    public string Telefon { get; init; } = "";
    public DateTime Geaendert { get; init; }
    public string Kuerzel { get; init; } = "";
    public bool Keine { get; init; }   // „keine gesetzliche Betreuung“ (0.33.3); dann sind die übrigen Angaben leer
}

public sealed class BetreuerEingabe
{
    public string Name { get; set; } = "";
    public string Stelle { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string Kuerzel { get; set; } = "";
    public bool Keine { get; set; }
}

public static class BetreuerRegeln
{
    public const string EmailHinweis = "Keine Gesundheitsdaten unverschlüsselt per E-Mail senden.";
    public const string DemoEndung = "@example.org";
    public const string KeineText = "keine gesetzliche Betreuung";
    public const string NichtErfasstText = "nicht erfasst";

    public static Betreuer AusEingabe(BetreuerEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (e.Keine)
        {
            // Drei Zustände (Regel 5): eingetragen, keine, nicht erfasst. „Keine“ ist eine eigene Fassung.
            if (e.Kuerzel.Trim().Length == 0) throw new Eingabefehler(["Kürzel fehlt (oben rechts „Angemeldet als …“)."]);
            return new Betreuer { Keine = true, Geaendert = jetzt, Kuerzel = e.Kuerzel.Trim() };
        }
        string email = e.Email.Trim();
        if (e.Name.Trim().Length == 0) fehler.Add("Name des Betreuers fehlt.");
        if (email.Length > 0 && !EmailGueltig(email)) fehler.Add("E-Mail-Adresse bitte so: name@beispiel.de (ohne Leerzeichen).");
        if (e.Kuerzel.Trim().Length == 0) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Betreuer
        {
            Name = e.Name.Trim(), Stelle = e.Stelle.Trim(), Email = email, Telefon = e.Telefon.Trim(), Geaendert = jetzt, Kuerzel = e.Kuerzel.Trim(),
        };
    }

    // Einfache Prüfung: genau ein @, davor etwas, danach ein Name mit Punkt; keine Leer- oder Steuerzeichen,
    // nichts, was eine mailto-Adresse verändert (?, &, #, %, Komma, Semikolon; 0.33.3: # und % dazu).
    public static bool EmailGueltig(string email)
    {
        if (email.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '?' or '&' or '#' or '%' or ',' or ';' or '<' or '>' or '"')) return false;
        int at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@')) return false;
        string domain = email[(at + 1)..];
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }

    public static BetreuerEingabe AlsEingabe(Betreuer? b) => new()
    {
        Name = b?.Name ?? "", Stelle = b?.Stelle ?? "", Email = b?.Email ?? "", Telefon = b?.Telefon ?? "", Keine = b?.Keine ?? false,
    };

    // „Heike Hecht (Betreuungsverein) · Tel. … · hecht@example.org“; ohne Eintrag „nicht erfasst“.
    public static string Text(Betreuer? b) =>
        b is null ? NichtErfasstText
            : b.Keine ? KeineText
            : string.Join(" · ", new[]
            {
                b.Name + (b.Stelle.Length > 0 ? $" ({b.Stelle})" : ""),
                b.Telefon.Length > 0 ? $"Tel. {b.Telefon}" : "",
                b.Email,
            }.Where(t => t.Length > 0));

    // Betreff ohne Gesundheitsangaben: nur der Name des Bewohners.
    public static string Betreff(Bewohner bewohner) => $"Betreuung {bewohner.Name}";

    // mailto: mit Empfänger und Betreff; der Text entsteht im E-Mail-Programm.
    public static string Mailto(Betreuer b, string betreff)
    {
        if (b.Keine || !EmailGueltig(b.Email)) throw new Eingabefehler(["Für den Betreuer ist keine gültige E-Mail-Adresse eingetragen."]);
        return $"mailto:{b.Email}?subject={Uri.EscapeDataString(betreff)}";
    }

    // Verlauf: je Fassung eine Zeile, neueste zuerst.
    public static List<string> Verlauf(IReadOnlyList<Betreuer> fassungen) =>
        fassungen.Reverse().Select(f => $"{BerichtRegeln.ZeitText(f.Geaendert)} · {f.Kuerzel}: {Text(f)}").ToList();
}
