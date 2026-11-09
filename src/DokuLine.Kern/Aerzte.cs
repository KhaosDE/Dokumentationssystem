using System.Globalization;

namespace DokuLine.Kern;

// Personenverzeichnis (Bauplan Schritt 29a; bis 0.33.3 „Ärzteverzeichnis“, Schritt 26b): Ärzte, Betreuer,
// Angehörige, die Zuständigen aus Forensik und FOSA und sonstige Stellen – für den Nutzer getrennte
// Verzeichnisse, technisch eine Tabelle mit dem Feld „Gruppe“. Überall Auswahl statt Freitext. Gelöscht
// wird nicht – wer nicht mehr gebraucht wird, wird inaktiv gesetzt (frühere Einträge behalten den Namen).
// Eine Person kann vielen Bewohnern zugeordnet sein, je mit Rolle („Mutter“, „Betreuer“ …). Nur
// ausgedachte Angaben; E-Mail-Adressen in der Demo nur mit @example.org.
public enum PersonGruppe { Arzt, Betreuer, Angehoerige, Forensik, Fosa, Sonstige }

public sealed record Person
{
    public long Id { get; init; }
    public PersonGruppe Gruppe { get; init; } = PersonGruppe.Arzt;
    public string Name { get; init; } = "";
    public string Art { get; init; } = "";            // Betreuer: Berufsbetreuer, ehrenamtlich …
    public string Fachrichtung { get; init; } = "";   // Ärzte: Fachrichtung; Forensik/FOSA: Funktion
    public string Praxis { get; init; } = "";         // Stelle: Praxis, Büro, Klinik, Ambulanz
    public string Telefon { get; init; } = "";
    public string Mobil { get; init; } = "";
    public string Email { get; init; } = "";
    public string Anschrift { get; init; } = "";
    public string Erreichbarkeit { get; init; } = "";
    public string Hinweis { get; init; } = "";
    public bool Aktiv { get; init; } = true;

    // „Dr. Ulme (ausgedacht) · Allgemeinmedizin“ – für Auswahllisten.
    public string Anzeige => Name + (Fachrichtung.Length > 0 ? $" · {Fachrichtung}" : Art.Length > 0 ? $" · {Art}" : "");
}

// Eine Person bei einem Bewohner, mit Rolle (z. B. „Mutter“, „Betreuer“, „zuständige Psychologin Forensik“).
public sealed record PersonZuordnung(long Id, long PersonId, long BewohnerId, string Rolle, DateTime Geaendert, string Kuerzel);

public sealed class PersonEingabe
{
    public PersonGruppe Gruppe { get; set; } = PersonGruppe.Arzt;
    public string Name { get; set; } = "";
    public string Art { get; set; } = "";
    public string Fachrichtung { get; set; } = "";
    public string Praxis { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string Mobil { get; set; } = "";
    public string Email { get; set; } = "";
    public string Anschrift { get; set; } = "";
    public string Erreichbarkeit { get; set; } = "";
    public string Hinweis { get; set; } = "";
}

public static class PersonRegeln
{
    public static readonly PersonGruppe[] Gruppen = Enum.GetValues<PersonGruppe>();

    public static string GruppeText(PersonGruppe g) => g switch
    {
        PersonGruppe.Arzt => "Ärzte",
        PersonGruppe.Betreuer => "Betreuer",
        PersonGruppe.Angehoerige => "Angehörige",
        PersonGruppe.Forensik => "Forensik",
        PersonGruppe.Fosa => "FOSA",
        _ => "Sonstige Stellen",
    };

    // Ein Eintrag der Gruppe in der Einzahl (für „Neue … “, Fenstertitel).
    public static string EinzahlText(PersonGruppe g) => g switch
    {
        PersonGruppe.Arzt => "Arzt",
        PersonGruppe.Betreuer => "Betreuer",
        PersonGruppe.Angehoerige => "Angehörige Person",
        PersonGruppe.Forensik => "Zuständige Person Forensik",
        PersonGruppe.Fosa => "Zuständige Person FOSA",
        _ => "Stelle",
    };

    public static string Schluessel(PersonGruppe g) => g switch
    {
        PersonGruppe.Arzt => "arzt",
        PersonGruppe.Betreuer => "betreuer",
        PersonGruppe.Angehoerige => "angehoerige",
        PersonGruppe.Forensik => "forensik",
        PersonGruppe.Fosa => "fosa",
        _ => "sonstige",
    };

    public static PersonGruppe AusSchluessel(string s) => Gruppen.FirstOrDefault(g => Schluessel(g) == s, PersonGruppe.Sonstige);

    // Bezeichnung der Felder „Fachrichtung“ und „Praxis“ je Gruppe; null = gibt es in dieser Gruppe nicht.
    public static string? FunktionText(PersonGruppe g) => g switch
    {
        PersonGruppe.Arzt => "Fachrichtung",
        PersonGruppe.Forensik or PersonGruppe.Fosa => "Funktion",
        _ => null,
    };

    public static string? StelleText(PersonGruppe g) => g switch
    {
        PersonGruppe.Arzt => "Praxis / Ort",
        PersonGruppe.Betreuer => "Büro",
        PersonGruppe.Forensik => "Klinik",
        PersonGruppe.Fosa => "Ambulanz",
        PersonGruppe.Sonstige => "Stelle",
        _ => null,
    };

    public static readonly string[] BetreuerArten = ["Berufsbetreuer", "ehrenamtlich", "Angehörige", "Vorsorgebevollmächtigte", "Betreuungsverein"];
    public static readonly string[] FunktionAuswahl = ["Psychologe/Psychologin", "Arzt/Ärztin", "Pflege", "Sozialdienst"];
    public static readonly string[] Beziehungen = ["Mutter", "Vater", "Schwester", "Bruder", "Tochter", "Sohn", "Partnerin", "Partner", "Tante", "Onkel", "Freundin", "Freund"];

    // Vorschlag für die Rolle bei einem Bewohner.
    public static string RolleVorschlag(Person p) => p.Gruppe switch
    {
        PersonGruppe.Arzt => p.Fachrichtung,
        PersonGruppe.Betreuer => "Betreuer",
        PersonGruppe.Forensik => p.Fachrichtung.Length > 0 ? $"{p.Fachrichtung} Forensik" : "zuständig Forensik",
        PersonGruppe.Fosa => p.Fachrichtung.Length > 0 ? $"{p.Fachrichtung} FOSA" : "zuständig FOSA",
        _ => "",
    };

    public static string[] RolleAuswahl(PersonGruppe g) => g switch
    {
        PersonGruppe.Angehoerige => Beziehungen,
        PersonGruppe.Betreuer => ["Betreuer", "Ersatzbetreuer"],
        PersonGruppe.Forensik => [.. FunktionAuswahl.Select(f => $"{f} Forensik")],
        PersonGruppe.Fosa => [.. FunktionAuswahl.Select(f => $"{f} FOSA")],
        _ => [],
    };

    // Ändern dürfen: Angehörige, wer Bewohner bearbeiten darf; das übrige Verzeichnis „Personenverzeichnis verwalten“.
    public static string RechtFuer(PersonGruppe g) => g == PersonGruppe.Angehoerige ? Funktionen.BewohnerBearbeiten : Funktionen.Aerzte;

    public static Person AusEingabe(PersonEingabe e, long id = 0, bool aktiv = true)
    {
        var fehler = new List<string>();
        if (e.Name.Trim().Length == 0) fehler.Add("Name fehlt (ausgedacht, z. B. „Dr. Ulme“).");
        if (e.Email.Trim().Length > 0 && !EmailGueltig(e.Email.Trim())) fehler.Add("E-Mail-Adresse bitte so: name@beispiel.de (ohne Leerzeichen).");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        bool funktion = FunktionText(e.Gruppe) is not null, stelle = StelleText(e.Gruppe) is not null;
        return new Person
        {
            Id = id, Gruppe = e.Gruppe, Name = e.Name.Trim(), Art = e.Gruppe == PersonGruppe.Betreuer ? e.Art.Trim() : "",
            Fachrichtung = funktion ? e.Fachrichtung.Trim() : "", Praxis = stelle ? e.Praxis.Trim() : "", Telefon = e.Telefon.Trim(), Mobil = e.Mobil.Trim(),
            Email = e.Email.Trim(), Anschrift = e.Anschrift.Trim(), Erreichbarkeit = e.Erreichbarkeit.Trim(), Hinweis = e.Hinweis.Trim(), Aktiv = aktiv,
        };
    }

    public static PersonEingabe AlsEingabe(Person a) => new()
    {
        Gruppe = a.Gruppe, Name = a.Name, Art = a.Art, Fachrichtung = a.Fachrichtung, Praxis = a.Praxis, Telefon = a.Telefon, Mobil = a.Mobil,
        Email = a.Email, Anschrift = a.Anschrift, Erreichbarkeit = a.Erreichbarkeit, Hinweis = a.Hinweis,
    };

    // Einfache Prüfung: ein @, davor etwas, danach ein Name mit Punkt; keine Leerzeichen und nichts, was eine
    // mailto-Adresse verändert (wie beim Betreuer, 0.33.3).
    public static bool EmailGueltig(string email) => BetreuerRegeln.EmailGueltig(email);

    // Für den Hinweis eines Termins: „Dr. Esche (ausgedacht), Praxis am Markt, Tel. 0000 …“
    public static string Kontakt(Person a) =>
        string.Join(", ", new[] { a.Name, a.Praxis, a.Telefon.Length > 0 ? $"Tel. {a.Telefon}" : "" }.Where(t => t.Length > 0));

    // Spalten der Liste je Verzeichnis (Vorgabe Tobi, 09.11.2026).
    public static string[] Spalten(PersonGruppe g) => g switch
    {
        PersonGruppe.Arzt => ["Name", "Fachrichtung", "Praxis / Ort", "Telefon", "E-Mail", "Bewohner", "Status"],
        PersonGruppe.Betreuer => ["Name", "Art", "Büro", "Telefon", "E-Mail", "Bewohner", "Status"],
        PersonGruppe.Angehoerige => ["Name", "Telefon", "Mobil", "zugeordnet zu", "Status"],
        PersonGruppe.Forensik => ["Name", "Funktion", "Klinik", "Telefon", "E-Mail", "Bewohner", "Status"],
        PersonGruppe.Fosa => ["Name", "Funktion", "Ambulanz", "Telefon", "E-Mail", "Bewohner", "Status"],
        _ => ["Name", "Stelle", "Telefon", "E-Mail", "Status"],
    };

    // Eine Zeile der Liste; namen: Bewohner-Id → Name.
    public static string[] Zeile(Person p, IEnumerable<PersonZuordnung> zuordnungen, IReadOnlyDictionary<long, string> namen)
    {
        var meine = zuordnungen.Where(z => z.PersonId == p.Id).ToList();
        string anzahl = meine.Count.ToString(CultureInfo.InvariantCulture);
        string status = p.Aktiv ? "aktiv" : "inaktiv";
        return p.Gruppe switch
        {
            PersonGruppe.Arzt or PersonGruppe.Forensik or PersonGruppe.Fosa => [p.Name, p.Fachrichtung, p.Praxis, p.Telefon, p.Email, anzahl, status],
            PersonGruppe.Betreuer => [p.Name, p.Art, p.Praxis, p.Telefon, p.Email, anzahl, status],
            PersonGruppe.Angehoerige => [p.Name, p.Telefon, p.Mobil, ZugeordnetText(meine, namen), status],
            _ => [p.Name, p.Praxis, p.Telefon, p.Email, status],
        };
    }

    // „Anton Ahorn (Mutter), Berta Birke (Tante)“
    public static string ZugeordnetText(IEnumerable<PersonZuordnung> zuordnungen, IReadOnlyDictionary<long, string> namen) =>
        string.Join(", ", zuordnungen.Select(z => namen.GetValueOrDefault(z.BewohnerId, "?") + (z.Rolle.Length > 0 ? $" ({z.Rolle})" : "")));

    // Suche über Name, Stelle, Funktion, Art und Telefon.
    public static bool Passt(Person p, string suche) =>
        suche.Trim().Length == 0 || new[] { p.Name, p.Praxis, p.Fachrichtung, p.Art, p.Telefon, p.Mobil }
            .Any(t => t.Contains(suche.Trim(), StringComparison.CurrentCultureIgnoreCase));
}
