namespace DokuLine.Kern;

// Demo für die Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger (Bauplan 30): aus den vorhandenen
// Demo-Personen abgeleitet, gemischt mit „keine“ und „nicht erfasst“, Fristen relativ zu heute. Alles ausgedacht.
public static partial class Demodaten
{
    public static readonly string[] Krankenkassen = ["Musterkasse Süd", "Beispiel-BKK Nord", "Ersatzkasse Musterland"];
    public static readonly string[] Kostentraeger = ["Bezirk Musterland", "Landkreis Beispielstadt"];
    private const string Leistung = "Besondere Wohnform (Eingliederungshilfe)";
    public const string Klinik = "Forensische Klinik Musterstadt", Ambulanz = "FOSA Musterstadt";

    // inhalt null: „keine“; fehlt ein Reiter in der Folge: „nicht erfasst“.
    public static IEnumerable<(Angabe Art, long BewohnerId, object? Inhalt)> AkteAngaben(IReadOnlyList<Bewohner> bewohner, IReadOnlyList<Person> personen,
        IReadOnlyList<(long Person, long Bewohner, string Rolle)> zuordnungen, IReadOnlyDictionary<long, Betreuer> betreuer, DateOnly heute)
    {
        var aerzte = personen.Where(p => p.Gruppe == PersonGruppe.Arzt).ToList();
        var hausaerzte = aerzte.Where(a => a.Fachrichtung.Contains("Allgemein", StringComparison.OrdinalIgnoreCase) || a.Fachrichtung.Contains("Hausarzt", StringComparison.OrdinalIgnoreCase)).ToList();
        var psychiater = aerzte.Where(a => a.Fachrichtung.Contains("Psychiatr", StringComparison.OrdinalIgnoreCase)).ToList();
        var andere = aerzte.Except(hausaerzte).Except(psychiater).ToList();
        var forensik = personen.Where(p => p.Gruppe == PersonGruppe.Forensik).Select(p => p.Id).ToList();
        var fosa = personen.Where(p => p.Gruppe == PersonGruppe.Fosa).Select(p => p.Id).ToList();
        var jeBereich = new Dictionary<string, int>();
        for (int i = 0; i < bewohner.Count; i++)
        {
            long id = bewohner[i].Id!.Value;
            // Zuständigkeit: je Bereich zwei mit Forensik, drei mit FOSA (einer davon auch Forensik); jeder fünfte übrige
            // nicht erfasst. Bestellweg folgt der Zuständigkeit, bei jedem achten nicht erfasst.
            int n = jeBereich[bewohner[i].Wohnbereich] = jeBereich.GetValueOrDefault(bewohner[i].Wohnbereich) + 1;
            bool inForensik = n is 2 or 9, inFosa = n is 9 or 13 or 17;
            var zust = new Zustaendigkeit
            {
                Forensik = inForensik ? JaNein.Ja : JaNein.Nein, Fosa = inFosa ? JaNein.Ja : JaNein.Nein,
                Klinik = inForensik ? Klinik : "", ForensikSeit = inForensik ? heute.AddDays(-900 - 31 * n) : null,
                Status = inForensik ? (n == 2 ? "Langzeitbeurlaubung" : "Probewohnen") : "",
                Ueberpruefung = inForensik ? heute.AddDays(n == 2 ? 40 : 200) : null, ForensikPersonen = inForensik ? forensik : [],
                Ambulanz = inFosa ? Ambulanz : "", FosaSeit = inFosa ? heute.AddDays(-400 - 7 * n) : null, FosaPersonen = inFosa ? fosa : [],
            };
            if (inForensik || inFosa || n % 5 != 0) yield return (Angabe.Zustaendigkeit, id, zust);
            if (n % 8 != 0)
            {
                var weg = ZustaendigkeitRegeln.Vorschlag(zust, heute);
                long? person = weg switch { Bestellweg.Forensik => forensik.FirstOrDefault(), Bestellweg.Fosa => fosa.FirstOrDefault(), _ => null };
                yield return (Angabe.Bestellweg, id, new BestellwegAngabe { Weg = weg, PersonId = person == 0 ? null : person });
            }
            // Betreuung: wie in der bisherigen Demo (Betreuer, „keine“ oder nicht erfasst), dazu Art, Aufgaben, Fristen.
            if (betreuer.TryGetValue(id, out var bt))
            {
                if (bt.Keine) yield return (Angabe.Betreuung, id, null);
                else if (personen.FirstOrDefault(p => p.Gruppe == PersonGruppe.Betreuer && p.Name.Equals(bt.Name, StringComparison.OrdinalIgnoreCase)) is Person p)
                    yield return (Angabe.Betreuung, id, new Betreuung
                    {
                        Art = AngabenRegeln.Betreuungsarten[i % 3 == 0 ? 1 : 0], PersonId = p.Id,
                        Aufgaben = [.. AngabenRegeln.Aufgabenvorgabe.Where((_, k) => (k + i) % 3 != 0)],
                        BestelltAm = heute.AddDays(-400 - 11 * i), UeberpruefungBis = heute.AddDays(-10 + 37 * i % 900),
                    });
            }
            // Angehörige: aus dem Verzeichnis; ohne Zuordnung jeder dritte „keine bekannt“, sonst nicht erfasst.
            var eigene = zuordnungen.Where(z => z.Bewohner == id && personen.Any(p => p.Id == z.Person && p.Gruppe == PersonGruppe.Angehoerige)).ToList();
            if (eigene.Count > 0)
                yield return (Angabe.Angehoerige, id, new Angehoerige
                {
                    Personen = [.. eigene.Select((z, k) => new Angehoeriger
                    {
                        PersonId = z.Person, Beziehung = z.Rolle.Length > 0 ? z.Rolle : "Angehörige", Hauptkontakt = k == 0, Notfall = k == 0,
                    })],
                });
            else if (i % 3 == 0) yield return (Angabe.Angehoerige, id, null);
            // Ärzte: Hausarzt und Psychiater im Wechsel; jeder siebte ohne Psychiater-Angabe, jeder elfte ohne Hausarzt.
            if (aerzte.Count > 0 && i % 13 != 5)
                yield return (Angabe.Aerzte, id, new AerzteAngabe
                {
                    Hausarzt = i % 11 == 3 ? new ZugeteilterArzt(ArztStand.Keiner) : hausaerzte.Count > 0 ? new ZugeteilterArzt(ArztStand.Arzt, hausaerzte[i % hausaerzte.Count].Id) : new(),
                    Psychiater = i % 7 == 2 ? new ZugeteilterArzt() : psychiater.Count > 0 ? new ZugeteilterArzt(ArztStand.Arzt, psychiater[i % psychiater.Count].Id) : new(),
                    Weitere = i % 4 == 1 && andere.Count > 0 ? [new WeitererArzt(andere[i % andere.Count].Id, andere[i % andere.Count].Fachrichtung)] : [],
                });
            // Krankenkasse: jeder zehnte nicht erfasst.
            if (i % 10 != 7)
                yield return (Angabe.Krankenkasse, id, new Krankenkasse
                {
                    Kasse = Krankenkassen[i % Krankenkassen.Length], Nummer = $"Z{100000000 + 7919 * (i + 1)}",
                    Status = AngabenRegeln.Versichertenstatus[i % 4 == 3 ? 2 : 0],
                    Befreiung = (i % 3) switch { 0 => JaNein.Ja, 1 => JaNein.Nein, _ => JaNein.NichtErfasst },
                    BefreitBis = i % 3 == 0 ? heute.AddDays(5 + 23 * i % 200) : null,
                    Pflegegrad = i % 7 == 4 ? null : i % 5 == 0 ? 2 : 0,
                    Ansprechpartner = "Servicestelle (ausgedacht)", Telefon = "0000 300 300",
                });
            // Kostenträger: jeder neunte nicht erfasst; die laufende Bewilligung endet bei manchen in den nächsten 12 Wochen.
            if (i % 9 != 4)
            {
                var von = heute.AddDays(-320 + 13 * i % 300);
                yield return (Angabe.Kostentraeger, id, new Kostentraeger
                {
                    Name = Kostentraeger[i % Kostentraeger.Length], Aktenzeichen = $"AZ-{2024 + i % 3}-{1000 + i}",
                    Sachbearbeiter = "Sachbearbeitung Eingliederungshilfe (ausgedacht)", Telefon = "0000 400 400", Email = "eingliederungshilfe@example.org",
                    Bewilligungen = [new(Leistung, von.AddDays(-20), von, von.AddDays(364)), new(Leistung, von.AddDays(-385), von.AddDays(-365), von.AddDays(-1))],
                });
            }
        }
    }
}

// Demo-Dokumente: je Wohnbereich bei den ersten vier Bewohnern Betreuerausweis, Kostenzusage und Arztbrief als kleine PDF
// mit großem „DEMO — ausgedachte Daten“; eine Kostenzusage ist abgelaufen.
public static partial class Demodaten
{
    public static IEnumerable<(Bewohner Bewohner, string Kategorie, string Titel, DateOnly Datum, DateOnly? GueltigBis, string[] Zeilen)> Dokumente(
        IReadOnlyList<Bewohner> bewohner, DateOnly heute)
    {
        foreach (var gruppe in bewohner.GroupBy(b => b.Wohnbereich))
            foreach (var (b, i) in gruppe.OrderBy(b => b.Nachname, StringComparer.CurrentCulture).Take(4).Select((b, i) => (b, i)))
            {
                string name = $"{b.Vorname} {b.Nachname}";
                yield return (b, "Betreuung und Gericht", "Betreuerausweis", heute.AddDays(-400 - 9 * i), heute.AddDays(300 + 40 * i),
                    [$"Betreute Person: {name} (ausgedacht)", "Betreuerin: Betreuungsverein (ausgedacht)", "Aufgabenbereiche: Gesundheitssorge, Behörden"]);
                yield return (b, "Kostenträger und Bescheide", "Kostenzusage", heute.AddDays(-200 - 13 * i), i == 3 ? heute.AddDays(-5) : heute.AddDays(160 - 20 * i),
                    [$"Leistungsberechtigt: {name} (ausgedacht)", "Leistung: Besondere Wohnform (Eingliederungshilfe)", "Kostenträger: Bezirk Musterland (ausgedacht)"]);
                yield return (b, "Ärztliches und Befunde", "Arztbrief", heute.AddDays(-30 - 11 * i), null,
                    [$"Patient: {name} (ausgedacht)", "Kurzer ausgedachter Brief ohne Diagnose.", "Praxis am Lindenplatz (ausgedacht)"]);
            }
    }
}
