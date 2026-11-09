using System.Globalization;

namespace DokuLine.Kern;

// Schweigepflichtentbindungen (Bauplan 35): wem gegenüber die Schweigepflicht aufgehoben ist und wem nicht. Gespeichert als
// Fassung im Reiter „Schweigepflicht“ (wie die übrigen Reiter der Akte). Das Programm bewertet nichts rechtlich; es zeigt,
// was eingetragen ist. Abgelaufene oder widerrufene Entbindungen zählen nicht als entbunden; alle anderen: keine Auskunft.
public enum Entbindungsstand { Entbunden, KeineAuskunft, NichtErfasst }

public enum Auskunftsrichtung { Geben, Einholen, Beides }

public sealed record Schweigepflichteintrag
{
    public long? PersonId { get; init; }                 // Personenverzeichnis (Betreuer, Angehörige, Ärzte, Forensik, FOSA)
    public string Stelle { get; init; } = "";            // ohne Person: Kostenträger oder Freitext
    public string Bezeichnung { get; init; } = "";       // z. B. „Mutter“, „Hausarzt“
    public Entbindungsstand Stand { get; init; } = Entbindungsstand.NichtErfasst;
    public string Umfang { get; init; } = SchweigepflichtRegeln.UmfangAlle;
    public Auskunftsrichtung Richtung { get; init; } = Auskunftsrichtung.Beides;
    public DateOnly? ErteiltAm { get; init; }
    public DateOnly? GueltigBis { get; init; }           // leer: unbefristet
    public DateOnly? WiderrufenAm { get; init; }
    public string Form { get; init; } = "";              // schriftlich, mündlich mit Zeuge
    public long? DokumentId { get; init; }
}

public sealed record Schweigepflicht
{
    public List<Schweigepflichteintrag> Eintraege { get; init; } = [];
}

// Eine Zeile der Auskunftsleiste: Zeichen und Wort stehen im Text, die Art gibt die Farbe.
public sealed record AuskunftMarke(string Text, Entbindungsstand Art, bool Widerrufen = false);

public static class SchweigepflichtRegeln
{
    public const string UmfangAlle = "alle Angaben", UmfangGesundheit = "nur Gesundheit", UmfangOrganisatorisch = "nur Organisatorisches";
    public static readonly string[] Umfaenge = [UmfangAlle, UmfangGesundheit, UmfangOrganisatorisch];
    public static readonly string[] Formen = ["schriftlich", "mündlich mit Zeuge"];
    public const int FristWochen = 4;
    public const string AlleAnderen = "Alle anderen: keine Auskunft";
    public const string NichtErfasstText = "Schweigepflicht: nicht erfasst – keine Auskunft geben";
    public const string KeineText = "Schweigepflicht: keine Entbindung – keine Auskunft geben";

    public static string RichtungText(Auskunftsrichtung r) => r switch
    {
        Auskunftsrichtung.Geben => "Auskunft geben",
        Auskunftsrichtung.Einholen => "Auskunft einholen",
        _ => "geben und einholen",
    };

    public static string StandText(Entbindungsstand s) => s switch
    {
        Entbindungsstand.Entbunden => "entbunden",
        Entbindungsstand.KeineAuskunft => "keine Auskunft",
        _ => AngabenRegeln.NichtErfasst,
    };

    public static bool Widerrufen(Schweigepflichteintrag e, DateOnly heute) => e.WiderrufenAm is DateOnly w && w <= heute;
    public static bool Abgelaufen(Schweigepflichteintrag e, DateOnly heute) => e.GueltigBis is DateOnly b && b < heute;

    // Was heute gilt: entbunden nur, solange weder abgelaufen noch widerrufen.
    public static Entbindungsstand Wirksam(Schweigepflichteintrag e, DateOnly heute) =>
        e.Stand == Entbindungsstand.Entbunden && (Widerrufen(e, heute) || Abgelaufen(e, heute)) ? Entbindungsstand.KeineAuskunft : e.Stand;

    // „Mutter · Erika Ahornblatt“, „Kostenträger · Bezirk Musterland“.
    public static string Wer(Schweigepflichteintrag e, Func<long?, string> name)
    {
        string wer = e.PersonId is not null ? name(e.PersonId) : e.Stelle.Trim();
        if (wer.Length == 0) wer = "?";
        return e.Bezeichnung.Trim().Length > 0 ? $"{e.Bezeichnung.Trim()} · {wer}" : wer;
    }

    // Die Auskunftsleiste im Akte-Kopf: je Eintrag eine Marke, am Ende immer „Alle anderen: keine Auskunft“.
    public static List<AuskunftMarke> Leiste(AngabeFassung? f, Func<long?, string> name, DateOnly heute)
    {
        if (f is null) return [new(NichtErfasstText, Entbindungsstand.NichtErfasst)];
        if (f.Lesen<Schweigepflicht>() is not Schweigepflicht s || s.Eintraege.Count == 0) return [new(KeineText, Entbindungsstand.KeineAuskunft)];
        var liste = new List<AuskunftMarke>();
        foreach (var e in s.Eintraege.OrderBy(e => Wirksam(e, heute)))
        {
            string wer = Wer(e, name);
            liste.Add(Wirksam(e, heute) switch
            {
                Entbindungsstand.Entbunden => new($"✓ {wer} – entbunden ({e.Umfang.Trim()})", Entbindungsstand.Entbunden),
                Entbindungsstand.KeineAuskunft when Widerrufen(e, heute) => new($"✕ {wer} – widerrufen am {Datum.Text(e.WiderrufenAm)}", Entbindungsstand.KeineAuskunft, true),
                Entbindungsstand.KeineAuskunft when Abgelaufen(e, heute) => new($"✕ {wer} – Entbindung abgelaufen am {Datum.Text(e.GueltigBis)}", Entbindungsstand.KeineAuskunft),
                Entbindungsstand.KeineAuskunft => new($"✕ {wer} – keine Auskunft", Entbindungsstand.KeineAuskunft),
                _ => new($"? {wer} – nicht erfasst, keine Auskunft geben", Entbindungsstand.NichtErfasst),
            });
        }
        liste.Add(new(AlleAnderen, Entbindungsstand.KeineAuskunft));
        return liste;
    }

    // Kurzform für die Bewohnerliste: „2 entbunden · 1 keine Auskunft“; dazu die Namen für den Hinweis.
    public static (string Kurz, string Namen, Entbindungsstand Art) Kurz(AngabeFassung? f, Func<long?, string> name, DateOnly heute)
    {
        if (f is null) return ("nicht erfasst", NichtErfasstText, Entbindungsstand.NichtErfasst);
        var eintraege = f.Lesen<Schweigepflicht>()?.Eintraege ?? [];
        if (eintraege.Count == 0) return ("keine Entbindung", KeineText, Entbindungsstand.KeineAuskunft);
        int entbunden = eintraege.Count(e => Wirksam(e, heute) == Entbindungsstand.Entbunden);
        int keine = eintraege.Count(e => Wirksam(e, heute) == Entbindungsstand.KeineAuskunft);
        int offen = eintraege.Count - entbunden - keine;
        var teile = new List<string>();
        if (entbunden > 0) teile.Add($"{entbunden} entbunden");
        if (keine > 0) teile.Add($"{keine} keine Auskunft");
        if (offen > 0) teile.Add($"{offen} nicht erfasst");
        string namen = string.Join(" · ", Leiste(f, name, heute).Select(m => m.Text));
        return (string.Join(" · ", teile), namen, offen > 0 ? Entbindungsstand.NichtErfasst : entbunden > 0 ? Entbindungsstand.Entbunden : Entbindungsstand.KeineAuskunft);
    }

    // Schnellabfrage „Auskunft prüfen …“: Name eintippen → was eingetragen ist. Ohne Treffer: keine Auskunft.
    public static AuskunftMarke Pruefen(AngabeFassung? f, string suche, Func<long?, string> name, DateOnly heute)
    {
        suche = suche.Trim();
        if (f is null) return new($"{NichtErfasstText}, bis geklärt", Entbindungsstand.NichtErfasst);
        var eintraege = f.Lesen<Schweigepflicht>()?.Eintraege ?? [];
        if (suche.Length < 2) return new("Bitte mindestens zwei Buchstaben eingeben.", Entbindungsstand.NichtErfasst);
        var treffer = eintraege.Where(e => Wer(e, name).Contains(suche, StringComparison.CurrentCultureIgnoreCase)).ToList();
        if (treffer.Count == 0) return new($"„{suche}“ ist nicht eingetragen – keine Auskunft", Entbindungsstand.KeineAuskunft);
        // Mehrere Treffer: die vorsichtigste Antwort zuerst (nicht erfasst, keine Auskunft, dann entbunden).
        var e = treffer.OrderByDescending(x => Wirksam(x, heute)).First();
        string wer = Wer(e, name);
        AuskunftMarke antwort = Wirksam(e, heute) switch
        {
            Entbindungsstand.Entbunden => new($"entbunden für {wer} ({e.Umfang.Trim()}, {RichtungText(e.Richtung)}" +
                                              (e.GueltigBis is DateOnly b ? $", gültig bis {Datum.Text(b)}" : ", unbefristet") + ")", Entbindungsstand.Entbunden),
            Entbindungsstand.KeineAuskunft => new($"keine Auskunft an {wer}" + (Widerrufen(e, heute) ? $" (widerrufen am {Datum.Text(e.WiderrufenAm)})"
                                                  : Abgelaufen(e, heute) ? $" (abgelaufen am {Datum.Text(e.GueltigBis)})" : ""), Entbindungsstand.KeineAuskunft),
            _ => new($"{wer}: nicht erfasst – keine Auskunft geben, bis geklärt", Entbindungsstand.NichtErfasst),
        };
        return antwort with { Widerrufen = Widerrufen(e, heute) };
    }

    // „Entbindung endet …“ vier Wochen vorher (nur, was heute noch gilt).
    public static List<Marke> Fristen(AngabeFassung? f, Func<long?, string> name, DateOnly heute) =>
        [.. (f?.Lesen<Schweigepflicht>()?.Eintraege ?? [])
            .Where(e => Wirksam(e, heute) == Entbindungsstand.Entbunden && e.GueltigBis is DateOnly b && b.DayNumber - heute.DayNumber <= FristWochen * 7)
            .Select(e => new Marke(MarkenArt.NichtErfasst, $"Entbindung endet {Datum.Text(e.GueltigBis)} ({Wer(e, name)})"))];

    // Zeilen für den Reiter: je Eintrag eine Zeile mit allem, was eingetragen ist.
    public static List<(string Beschriftung, string Wert, bool Achtung)> Zeilen(Schweigepflicht s, Func<long?, string> name, Func<long, string?> dokument, DateOnly heute) =>
        [.. s.Eintraege.Select(e =>
        {
            var teile = new List<string> { StandText(e.Stand) };
            if (e.Stand == Entbindungsstand.Entbunden)
            {
                teile.Add(e.Umfang.Trim());
                teile.Add(RichtungText(e.Richtung));
                if (e.ErteiltAm is DateOnly a) teile.Add($"erteilt am {Datum.Text(a)}");
                teile.Add(e.GueltigBis is DateOnly b ? (Abgelaufen(e, heute) ? $"abgelaufen am {Datum.Text(b)}" : $"gültig bis {Datum.Text(b)}") : "unbefristet");
                if (e.Form.Trim().Length > 0) teile.Add(e.Form.Trim());
            }
            if (e.DokumentId is long d && dokument(d) is string titel) teile.Add($"Dokument: {titel}");
            if (e.WiderrufenAm is DateOnly w) teile.Add(w <= heute ? $"widerrufen am {Datum.Text(w)}" : $"Widerruf ab {Datum.Text(w)}");
            return (Wer(e, name), string.Join(" · ", teile.Where(t => t.Length > 0)), e.Stand == Entbindungsstand.NichtErfasst);
        })];

    public static List<string> Pruefen(Schweigepflicht s)
    {
        var fehler = new List<string>();
        foreach (var e in s.Eintraege)
        {
            string wer = Wer(e, id => id?.ToString(CultureInfo.InvariantCulture) ?? "");
            if (e.PersonId is null && e.Stelle.Trim().Length == 0) fehler.Add("Schweigepflicht: Bitte eine Person oder Stelle angeben.");
            if (e.ErteiltAm is DateOnly a && e.GueltigBis is DateOnly b && b < a) fehler.Add($"Schweigepflicht ({wer}): „gültig bis“ liegt vor „erteilt am“.");
            if (e.ErteiltAm is DateOnly a2 && e.WiderrufenAm is DateOnly w && w < a2) fehler.Add($"Schweigepflicht ({wer}): „widerrufen am“ liegt vor „erteilt am“.");
        }
        return fehler;
    }

    // Überleitungsbogen: die ganze Leiste – wer entbunden ist (auch die Klinik, wenn eingetragen) und wer nicht.
    // Ohne die Zeichen ✓/✕ (die PDF-Schrift kennt sie nicht); das Wort steht ohnehin in jeder Zeile.
    public static List<string> FuerUeberleitung(AngabeFassung? f, Func<long?, string> name, DateOnly heute) =>
        [.. Leiste(f, name, heute).Select(m => m.Text.TrimStart('✓', '✕', '?', ' '))];
}
