using System.Globalization;
using System.Text.Json;

namespace DokuLine.Kern;

// Akte, Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger (Bauplan 30). Je Reiter eine Folge von
// Fassungen („ab Datum …“, wer, wann); es gilt die neueste, die schon begonnen hat. Drei Zustände (Regel 5):
// eingetragen · „keine“ · ohne Fassung „nicht erfasst“.
public enum Angabe { Betreuung, Angehoerige, Aerzte, Krankenkasse, Kostentraeger, Zustaendigkeit, Bestellweg, Personenbeschreibung }

public enum AngabeZustand { Eingetragen, Keine }

public sealed record AngabeFassung(long Id, long BewohnerId, Angabe Art, AngabeZustand Zustand, string Inhalt, DateOnly Ab,
    DateTime Geaendert, string Kuerzel)
{
    public T? Lesen<T>() where T : class => Zustand == AngabeZustand.Eingetragen ? JsonSerializer.Deserialize<T>(Inhalt) : null;
}

public sealed record Betreuung
{
    public string Art { get; init; } = "";               // Berufsbetreuer, ehrenamtlich, Angehörige, Vorsorgebevollmächtigte
    public long? PersonId { get; init; }                 // Verzeichnis „Betreuer“
    public List<string> Aufgaben { get; init; } = [];
    public string AufgabenText { get; init; } = "";
    public DateOnly? BestelltAm { get; init; }
    public DateOnly? UeberpruefungBis { get; init; }
    public long? ErsatzId { get; init; }
}

public sealed record Angehoeriger
{
    public long PersonId { get; init; }                  // Verzeichnis „Angehörige“
    public string Beziehung { get; init; } = "";
    public bool Hauptkontakt { get; init; }
    public bool Notfall { get; init; }
    public string Hinweis { get; init; } = "";
}

public sealed record Angehoerige
{
    public List<Angehoeriger> Personen { get; init; } = [];
}

public enum ArztStand { NichtErfasst, Keiner, Arzt }

public sealed record ZugeteilterArzt(ArztStand Stand = ArztStand.NichtErfasst, long? PersonId = null);

public sealed record WeitererArzt(long PersonId, string Rolle, string Hinweis = "");

public sealed record AerzteAngabe
{
    public ZugeteilterArzt Hausarzt { get; init; } = new();
    public ZugeteilterArzt Psychiater { get; init; } = new();
    public List<WeitererArzt> Weitere { get; init; } = [];
}

public enum JaNein { NichtErfasst, Ja, Nein }

public sealed record Krankenkasse
{
    public string Kasse { get; init; } = "";
    public string Nummer { get; init; } = "";            // Versichertennummer (Demo: ausgedacht)
    public string Status { get; init; } = "";            // Mitglied, familienversichert …
    public JaNein Befreiung { get; init; }
    public DateOnly? BefreitBis { get; init; }
    public int? Pflegegrad { get; init; }                // null: nicht erfasst, 0: keiner, 1–5
    public string Ansprechpartner { get; init; } = "";
    public string Telefon { get; init; } = "";
}

public sealed record Bewilligung(string Leistung, DateOnly? BescheidVom, DateOnly Von, DateOnly Bis);

public sealed record Kostentraeger
{
    public string Name { get; init; } = "";
    public string Aktenzeichen { get; init; } = "";
    public string Sachbearbeiter { get; init; } = "";
    public string Telefon { get; init; } = "";
    public string Email { get; init; } = "";
    public List<Bewilligung> Bewilligungen { get; init; } = [];
}

public static class AngabenRegeln
{
    public const int BetreuungWochen = 8, BefreiungWochen = 4, KostenzusageWochen = 12;
    public const string NichtErfasst = "nicht erfasst";
    public static readonly string[] Betreuungsarten = ["Berufsbetreuer", "ehrenamtlich", "Angehörige", "Vorsorgebevollmächtigte"];
    public static readonly string[] Aufgabenvorgabe =
        ["Gesundheitssorge", "Aufenthaltsbestimmung", "Vermögenssorge", "Behörden und Ämter", "Wohnungsangelegenheiten", "Post", "Unterbringung"];
    public static readonly string[] Versichertenstatus = ["Mitglied", "familienversichert", "Rentner", "freiwillig versichert"];
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string Name(Angabe a) => a switch
    {
        Angabe.Betreuung => "Betreuung",
        Angabe.Angehoerige => "Angehörige",
        Angabe.Aerzte => "Ärzte",
        Angabe.Krankenkasse => "Krankenkasse",
        Angabe.Kostentraeger => "Kostenträger",
        Angabe.Zustaendigkeit => "Zuständigkeit",
        Angabe.Bestellweg => "Bestellweg",
        _ => "Personenbeschreibung",
    };

    public static string KeineText(Angabe a) => a switch
    {
        Angabe.Betreuung => "keine gesetzliche Betreuung",
        Angabe.Angehoerige => "keine Angehörigen bekannt",
        Angabe.Aerzte => "keine Ärzte",
        Angabe.Krankenkasse => "keine Krankenkasse",
        Angabe.Kostentraeger => "kein Kostenträger",
        Angabe.Zustaendigkeit => "weder Forensik noch FOSA",
        Angabe.Bestellweg => "kein Bestellweg",
        _ => "keine Personenbeschreibung",
    };

    public static string Schluessel(Angabe a) => a.ToString().ToLowerInvariant();

    public static string Inhalt<T>(T wert) => JsonSerializer.Serialize(wert);

    // Es gilt die neueste Fassung, die an diesem Tag schon begonnen hat.
    public static AngabeFassung? Aktuell(IEnumerable<AngabeFassung> fassungen, DateOnly heute) =>
        fassungen.Where(f => f.Ab <= heute).OrderBy(f => f.Ab).ThenBy(f => f.Id).LastOrDefault();

    // Die Bewilligung, die heute gilt; sonst die zuletzt endende.
    public static Bewilligung? AktuelleBewilligung(Kostentraeger k, DateOnly heute) =>
        k.Bewilligungen.Where(b => b.Von <= heute && heute <= b.Bis).OrderByDescending(b => b.Bis).FirstOrDefault()
        ?? k.Bewilligungen.OrderByDescending(b => b.Bis).FirstOrDefault();

    // Fristen als Marken (Übersicht, Bewohnerliste, Kopf der Akte): Betreuung 8, Befreiung 4, Kostenzusage 12 Wochen vorher.
    public static List<Marke> Fristen(IReadOnlyDictionary<Angabe, AngabeFassung> aktuell, DateOnly heute)
    {
        var marken = new List<Marke>();
        void Frist(string was, DateOnly? bis, int wochen)
        {
            if (bis is not DateOnly b || b.DayNumber - heute.DayNumber > wochen * 7) return;
            marken.Add(new Marke(MarkenArt.Beschluss, b < heute ? $"{was} abgelaufen seit {Datum.Text(b)}" : $"{was} endet {b.ToString("dd.MM.yyyy", De)}"));
        }
        if (aktuell.GetValueOrDefault(Angabe.Betreuung)?.Lesen<Betreuung>() is Betreuung bt) Frist("Betreuung", bt.UeberpruefungBis, BetreuungWochen);
        if (aktuell.GetValueOrDefault(Angabe.Krankenkasse)?.Lesen<Krankenkasse>() is Krankenkasse kk && kk.Befreiung == JaNein.Ja)
            Frist("Befreiung", kk.BefreitBis, BefreiungWochen);
        if (aktuell.GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>() is Kostentraeger kt && AktuelleBewilligung(kt, heute) is Bewilligung bw)
            Frist("Kostenzusage", bw.Bis, KostenzusageWochen);
        return marken;
    }

    // Kurzzeile für die Stammdaten („Betreuer: …“, „Krankenkasse: …“, „Kostenträger: … bis …“).
    public static string Kurz(Angabe art, AngabeFassung? f, Func<long?, string> personName, DateOnly heute)
    {
        if (f is null) return NichtErfasst;
        if (f.Zustand == AngabeZustand.Keine) return KeineText(art);
        return art switch
        {
            Angabe.Betreuung when f.Lesen<Betreuung>() is Betreuung b =>
                string.Join(" · ", new[] { personName(b.PersonId), b.Art, b.UeberpruefungBis is DateOnly u ? $"Überprüfung bis {Datum.Text(u)}" : "" }.Where(x => x.Length > 0)),
            Angabe.Angehoerige when f.Lesen<Angehoerige>() is Angehoerige a =>
                a.Personen.Count == 0 ? KeineText(art) : string.Join(", ", a.Personen.Select(p => $"{personName(p.PersonId)} ({p.Beziehung})")),
            Angabe.Aerzte when f.Lesen<AerzteAngabe>() is AerzteAngabe a =>
                $"Hausarzt: {ArztText(a.Hausarzt, personName)} · Psychiater: {ArztText(a.Psychiater, personName)}",
            Angabe.Krankenkasse when f.Lesen<Krankenkasse>() is Krankenkasse k => string.Join(" · ", new[] { k.Kasse, k.Status }.Where(x => x.Length > 0)),
            Angabe.Zustaendigkeit when f.Lesen<Zustaendigkeit>() is Zustaendigkeit z =>
                string.Join(" · ", new[] { ZustaendigkeitRegeln.InForensik(z) ? "Forensik" : "", ZustaendigkeitRegeln.InFosa(z, heute) ? "FOSA" : "" }
                    .Where(x => x.Length > 0).DefaultIfEmpty("weder Forensik noch FOSA")),
            Angabe.Bestellweg when f.Lesen<BestellwegAngabe>() is BestellwegAngabe w => ZustaendigkeitRegeln.WegText(w.Weg),
            Angabe.Kostentraeger when f.Lesen<Kostentraeger>() is Kostentraeger k =>
                AktuelleBewilligung(k, heute) is Bewilligung b ? $"{k.Name} bis {Datum.Text(b.Bis)}" : k.Name,
            _ => NichtErfasst,
        };
    }

    // Die Zeilen eines Reiters zum Anzeigen: Beschriftung, Wert, „Achtung“ (nicht erfasst).
    public static List<(string Beschriftung, string Wert, bool Achtung)> Zeilen(Angabe art, AngabeFassung? f, Func<long?, string> person, DateOnly heute)
    {
        if (f is null) return [(Name(art), NichtErfasst, true)];
        if (f.Zustand == AngabeZustand.Keine) return [(Name(art), KeineText(art), false)];
        var z = new List<(string, string, bool)>();
        void Z(string b, string w, bool achtung = false) => z.Add((b, w.Length > 0 ? w : "–", achtung));
        switch (art)
        {
            case Angabe.Betreuung when f.Lesen<Betreuung>() is Betreuung b:
                Z("Betreuer", person(b.PersonId));
                Z("Art", b.Art);
                Z("Aufgabenbereiche", string.Join(", ", b.Aufgaben.Append(b.AufgabenText).Where(x => x.Trim().Length > 0)));
                Z("Bestellt am", b.BestelltAm is DateOnly d ? Datum.Text(d) : "");
                Z("Überprüfung bis", b.UeberpruefungBis is DateOnly u ? Datum.Text(u) : NichtErfasst, b.UeberpruefungBis is null);
                Z("Ersatzbetreuer", b.ErsatzId is null ? "keiner" : person(b.ErsatzId));
                break;
            case Angabe.Angehoerige when f.Lesen<Angehoerige>() is Angehoerige a:
                foreach (var p in a.Personen)
                    Z(p.Beziehung, string.Join(" · ", new[] { person(p.PersonId), p.Hauptkontakt ? "Hauptkontakt" : "", p.Notfall ? "im Notfall benachrichtigen" : "",
                        p.Hinweis }.Where(x => x.Length > 0)));
                if (a.Personen.Count == 0) Z("Angehörige", KeineText(art));
                break;
            case Angabe.Aerzte when f.Lesen<AerzteAngabe>() is AerzteAngabe a:
                Z("Hausarzt", ArztText(a.Hausarzt, person), a.Hausarzt.Stand == ArztStand.NichtErfasst);
                Z("Psychiater", ArztText(a.Psychiater, person), a.Psychiater.Stand == ArztStand.NichtErfasst);
                foreach (var w in a.Weitere) Z(w.Rolle.Length > 0 ? w.Rolle : "Arzt", string.Join(" · ", new[] { person(w.PersonId), w.Hinweis }.Where(x => x.Length > 0)));
                break;
            case Angabe.Krankenkasse when f.Lesen<Krankenkasse>() is Krankenkasse k:
                Z("Krankenkasse", k.Kasse);
                Z("Versichertennummer", k.Nummer);
                Z("Status", k.Status);
                Z("Zuzahlungsbefreiung", BefreiungText(k), k.Befreiung == JaNein.NichtErfasst);
                Z("Pflegegrad", PflegegradText(k.Pflegegrad), k.Pflegegrad is null);
                Z("Ansprechpartner", string.Join(" · ", new[] { k.Ansprechpartner, k.Telefon }.Where(x => x.Length > 0)));
                break;
            case Angabe.Zustaendigkeit:
                return ZustaendigkeitRegeln.Zeilen(f, person);
            case Angabe.Bestellweg when f.Lesen<BestellwegAngabe>() is BestellwegAngabe w:
                Z("Bestellweg", ZustaendigkeitRegeln.WegText(w.Weg), w.Weg == Bestellweg.NichtErfasst);
                Z("Ansprechperson", w.PersonId is null ? "" : person(w.PersonId));
                break;
            case Angabe.Personenbeschreibung when f.Lesen<Personenbeschreibung>() is Personenbeschreibung p:
                Z("Größe", p.Groesse);
                Z("Statur", p.Statur);
                Z("Haarfarbe", p.Haarfarbe);
                Z("Augenfarbe", p.Augenfarbe);
                Z("Brille", p.Brille);
                Z("Besondere Merkmale", p.Merkmale);
                break;
            case Angabe.Kostentraeger when f.Lesen<Kostentraeger>() is Kostentraeger k:
                Z("Kostenträger", k.Name);
                Z("Aktenzeichen", k.Aktenzeichen);
                Z("Sachbearbeitung", string.Join(" · ", new[] { k.Sachbearbeiter, k.Telefon, k.Email }.Where(x => x.Length > 0)));
                var aktuell = AktuelleBewilligung(k, heute);
                foreach (var b in k.Bewilligungen.OrderByDescending(x => x == aktuell).ThenByDescending(x => x.Bis))
                    Z(b == aktuell ? "Bewilligung" : "früher", $"{b.Leistung}: {Datum.Text(b.Von)} bis {Datum.Text(b.Bis)}" +
                        (b.BescheidVom is DateOnly v ? $" (Bescheid vom {Datum.Text(v)})" : "") + (b.Bis < heute ? " – abgelaufen" : ""));
                break;
        }
        return z;
    }

    // „ab 01.10.2026 · EF, 09.11.2026 14:20 · keine gesetzliche Betreuung“ – für den Verlauf.
    public static string VerlaufZeile(Angabe art, AngabeFassung f, Func<long?, string> person, DateOnly heute) =>
        $"ab {Datum.Text(f.Ab)} · {f.Kuerzel}, {f.Geaendert.ToString("dd.MM.yyyy HH:mm", De)} · {Kurz(art, f, person, heute)}";

    public static string ArztText(ZugeteilterArzt w, Func<long?, string> personName) => w.Stand switch
    {
        ArztStand.Arzt => personName(w.PersonId),
        ArztStand.Keiner => "keiner",
        _ => NichtErfasst,
    };

    public static string PflegegradText(int? grad) => grad switch { null => NichtErfasst, 0 => "keiner", _ => grad.ToString()! };

    public static string BefreiungText(Krankenkasse k) => k.Befreiung switch
    {
        JaNein.Ja => k.BefreitBis is DateOnly b ? $"ja, bis {Datum.Text(b)}" : "ja",
        JaNein.Nein => "nein",
        _ => NichtErfasst,
    };

    // Ärzte zur Auswahl: die dem Bewohner zugeteilten oben, alle übrigen darunter (Visite, Medikation, Kalender).
    public static List<Person> AerzteSortiert(IEnumerable<Person> aerzte, AerzteAngabe? zugeteilt)
    {
        var ids = new List<long?>();
        if (zugeteilt is not null)
        {
            ids.Add(zugeteilt.Hausarzt.Stand == ArztStand.Arzt ? zugeteilt.Hausarzt.PersonId : null);
            ids.Add(zugeteilt.Psychiater.Stand == ArztStand.Arzt ? zugeteilt.Psychiater.PersonId : null);
            ids.AddRange(zugeteilt.Weitere.Select(w => (long?)w.PersonId));
        }
        var liste = aerzte.ToList();
        int Rang(Person p) => ids.IndexOf(p.Id) is int i && i >= 0 ? i : int.MaxValue;
        return [.. liste.OrderBy(Rang).ThenBy(p => liste.IndexOf(p))];
    }

    // Prüfungen beim Speichern; Rückgabe: Fehlertexte.
    public static List<string> Pruefen(Angabe art, object inhalt)
    {
        var fehler = new List<string>();
        switch (inhalt)
        {
            case Betreuung b:
                if (b.PersonId is null) fehler.Add("Betreuer: bitte eine Person aus dem Verzeichnis wählen (oder „keine gesetzliche Betreuung“).");
                if (b.BestelltAm is DateOnly von && b.UeberpruefungBis is DateOnly bis && bis < von) fehler.Add("„Überprüfung bis“ liegt vor „bestellt am“.");
                break;
            case Angehoerige a:
                if (a.Personen.GroupBy(p => p.PersonId).Any(g => g.Count() > 1)) fehler.Add("Eine Person steht doppelt in der Liste.");
                if (a.Personen.Any(p => p.Beziehung.Trim().Length == 0)) fehler.Add("Bitte bei jedem Angehörigen die Beziehung eintragen (z. B. Mutter).");
                break;
            case AerzteAngabe a:
                if ((a.Hausarzt.Stand == ArztStand.Arzt && a.Hausarzt.PersonId is null) || (a.Psychiater.Stand == ArztStand.Arzt && a.Psychiater.PersonId is null))
                    fehler.Add("Bitte einen Arzt aus dem Verzeichnis wählen.");
                break;
            case Krankenkasse k:
                if (k.Kasse.Trim().Length == 0) fehler.Add("Krankenkasse fehlt (oder „keine Krankenkasse“).");
                if (k.Pflegegrad is < 0 or > 5) fehler.Add("Pflegegrad: keiner oder 1 bis 5.");
                break;
            case Kostentraeger k:
                if (k.Name.Trim().Length == 0) fehler.Add("Kostenträger fehlt (oder „kein Kostenträger“).");
                if (k.Bewilligungen.Any(b => b.Bis < b.Von)) fehler.Add("Bewilligung: „bis“ liegt vor „von“.");
                if (k.Bewilligungen.Any(b => b.Leistung.Trim().Length == 0)) fehler.Add("Bewilligung: Leistung fehlt.");
                if (k.Email.Trim().Length > 0 && !BetreuerRegeln.EmailGueltig(k.Email.Trim())) fehler.Add("E-Mail-Adresse bitte so: name@beispiel.de.");
                break;
            case Zustaendigkeit z:
                if (z.Forensik == JaNein.Ja && z.Klinik.Trim().Length == 0) fehler.Add("Forensik: bitte die Klinik eintragen.");
                if (z.Fosa == JaNein.Ja && z.Ambulanz.Trim().Length == 0) fehler.Add("FOSA: bitte die Ambulanz eintragen.");
                if (z.FosaSeit is DateOnly seit && z.FosaBis is DateOnly ende && ende < seit) fehler.Add("FOSA: „bis“ liegt vor „seit“.");
                break;
            case BestellwegAngabe:
            case Personenbeschreibung:
                break;
            default:
                fehler.Add($"{Name(art)}: unbekannter Inhalt.");
                break;
        }
        return fehler;
    }
}
