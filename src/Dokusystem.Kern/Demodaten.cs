using System.Text.Json;

namespace Dokusystem.Kern;

// Demo-Daten aus daten/demo/*.json (lesbare Dateien im Repository, in die .exe eingebaut).
// Alle Angaben dort sind ausgedacht. Datumsangaben der Bereiche entstehen relativ zum heutigen
// Tag, damit die Demo an jedem Tag etwas zeigt (Bauplan in IDEEN.md).
public static class Demodaten
{
    private static readonly JsonSerializerOptions Optionen = new() { PropertyNameCaseInsensitive = true };

    private static T Lesen<T>(string datei)
    {
        using var strom = typeof(Demodaten).Assembly.GetManifestResourceStream($"Dokusystem.Kern.Demo.{datei}")
            ?? throw new InvalidOperationException($"Demo-Daten {datei} fehlen im Programm.");
        return JsonSerializer.Deserialize<T>(strom, Optionen) ?? throw new InvalidOperationException($"Demo-Daten {datei} sind leer.");
    }

    // ----- Bewohner -----

    private sealed class BewohnerDatei
    {
        public List<BewohnerEingabe> Bewohner { get; set; } = [];
    }

    public static List<Bewohner> Bewohner() =>
        Lesen<BewohnerDatei>("bewohner.json").Bewohner.Select(e => BewohnerRegeln.AusEingabe(e)).ToList();

    // ----- Berichte -----

    public sealed class BerichtTexte
    {
        public List<string> Kuerzel { get; set; } = [];
        public Dictionary<string, List<string>> Texte { get; set; } = [];
        public List<string> Nachtraege { get; set; } = [];
        public UebergabeTexte Uebergabe { get; set; } = new();
    }

    public sealed class UebergabeTexte
    {
        public List<string> Saetze { get; set; } = [];
        public List<string> Notizen { get; set; } = [];
    }

    // Ein Demo-Bericht mit seinem Übergabe-Eintrag (falls einer dazugehört).
    public sealed record DemoBericht(Bericht Bericht, UebergabeWunsch? Uebergabe = null, string? GelesenVon = null, string? ErledigtVon = null);
    public sealed record DemoNotiz(string Text, UebergabeWunsch Wunsch, DateTime Erstellt, string Verfasser);
    public sealed record DemoTagebuch(List<DemoBericht> Berichte, List<DemoNotiz> Allgemein);

    public static BerichtTexte BerichtBausteine() => Lesen<BerichtTexte>("berichte.json");

    // Berichte der letzten vier Wochen für die übergebenen Bewohner, über alle Lebensbereiche
    // verteilt; einige wichtig, einige mit Nachtrag, einige mit Übergabe-Eintrag (offen, gelesen,
    // erledigt, abgelaufen), dazu Notizen für den ganzen Wohnbereich. Immer gleich für denselben
    // Zeitpunkt (fester Zufallsstartwert je Bewohner), nichts liegt in der Zukunft.
    public static DemoTagebuch Berichte(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var bausteine = BerichtBausteine();
        var berichte = new List<DemoBericht>();
        string wochentag = jetzt.AddDays(2).ToString("dddd", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        string Satz(int i) => bausteine.Uebergabe.Saetze[i % bausteine.Uebergabe.Saetze.Count].Replace("{tag}", wochentag);
        for (int i = 0; i < bewohner.Count; i++)
        {
            var b = bewohner[i];
            var zufall = new Random(1000 + i);
            for (int tageZurueck = 27; tageZurueck >= 0; tageZurueck--)
            {
                var tag = jetzt.Date.AddDays(-tageZurueck);
                if (DateOnly.FromDateTime(tag) < b.Aufnahmedatum) continue;
                int anzahl = zufall.Next(1, 3);
                for (int n = 0; n < anzahl; n++)
                {
                    // Früh oder Spät, selten Nacht.
                    int schicht = zufall.Next(10) switch { < 5 => 0, < 9 => 1, _ => 2 };
                    var zeit = schicht switch
                    {
                        0 => tag.AddHours(7).AddMinutes(zufall.Next(0, 13 * 30) / 30 * 30),
                        1 => tag.AddHours(14).AddMinutes(30 + zufall.Next(0, 14 * 30) / 30 * 30),
                        _ => tag.AddHours(23).AddMinutes(zufall.Next(0, 4) * 15),
                    };
                    // Immer erzeugen (gleiche Zufallsfolge zu jeder Uhrzeit), nur Vergangenes behalten.
                    var bericht = Erzeugen(b, zeit, zufall, bausteine, jetzt, wichtig: zufall.Next(30) == 0);   // selten, sonst verliert „Wichtig“ seinen Sinn
                    if (zeit <= jetzt) berichte.Add(new DemoBericht(bericht));
                }
            }
            // Übergabe: bei jedem zweiten Bewohner ein Bericht aus den letzten Stunden für die
            // nächste Schicht, dazu einige länger laufende, erledigte und abgelaufene Einträge.
            if (i % 2 == 0)
            {
                var bericht = Erzeugen(b, jetzt.AddMinutes(-(40 + i * 25)), zufall, bausteine, jetzt, wichtig: i == 0);
                var wunsch = i switch
                {
                    2 => new UebergabeWunsch(UebergabeDauer.DreiTage),
                    4 => new UebergabeWunsch(UebergabeDauer.BisErledigt, Text: Satz(0)),
                    _ => new UebergabeWunsch(UebergabeDauer.NaechsteSchicht),
                };
                berichte.Add(new DemoBericht(bericht, wunsch, GelesenVon: i == 0 ? "AB" : null));
            }
            if (i == 1)
            {
                var bericht = Erzeugen(b, jetzt.AddDays(-2).Date.AddHours(16), zufall, bausteine, jetzt, wichtig: false);
                berichte.Add(new DemoBericht(bericht, new UebergabeWunsch(UebergabeDauer.BisErledigt, Text: Satz(2)), "AB", ErledigtVon: "CD"));
            }
            if (i == 3)
            {
                var bericht = Erzeugen(b, jetzt.AddDays(-5).Date.AddHours(9), zufall, bausteine, jetzt, wichtig: false);
                berichte.Add(new DemoBericht(bericht, new UebergabeWunsch(UebergabeDauer.NaechsteSchicht)));   // längst abgelaufen
            }
            if (i == 5)
            {
                var bericht = Erzeugen(b, jetzt.AddHours(-20), zufall, bausteine, jetzt, wichtig: true);
                berichte.Add(new DemoBericht(bericht, new UebergabeWunsch(UebergabeDauer.Woche, Text: Satz(1))));
            }
        }
        var notizen = bausteine.Uebergabe.Notizen;
        var allgemein = new List<DemoNotiz>
        {
            new(notizen[0].Replace("{tag}", wochentag), new UebergabeWunsch(UebergabeDauer.Woche), jetzt.AddDays(-1).Date.AddHours(9), bausteine.Kuerzel[1]),
            new(notizen[1].Replace("{tag}", wochentag), new UebergabeWunsch(UebergabeDauer.BisErledigt), jetzt.AddDays(-2).Date.AddHours(15), bausteine.Kuerzel[2]),
        };
        return new DemoTagebuch(berichte, allgemein);
    }

    // ----- Aufgaben (Wohnbereich-ToDo) -----

    private sealed class AufgabenDatei
    {
        public List<AufgabeVorlage> Aufgaben { get; set; } = [];
    }

    public sealed class AufgabeVorlage
    {
        public string Text { get; set; } = "";
        public string? Bewohner { get; set; }          // Nachname aus bewohner.json
        public string Fuer { get; set; } = "naechste_schicht";
        public int SchichtInTagen { get; set; }
        public string Schicht { get; set; } = "frueh";
        public double ErstelltVorStunden { get; set; }
        public double? FaelligInStunden { get; set; }
        public string Verfasser { get; set; } = "";
        public EreignisVorlage? Weitergabe { get; set; }
        public EreignisVorlage? Erledigt { get; set; }
    }

    public sealed class EreignisVorlage
    {
        public double VorStunden { get; set; }
        public string Kuerzel { get; set; } = "";
        public string Notiz { get; set; } = "";
    }

    public sealed record DemoAufgabe(Aufgabe Aufgabe);

    // Aufgaben aus daten/demo/aufgaben.json, Zeiten relativ zu „jetzt“. Fehlt der genannte
    // Bewohner (z. B. umbenannt), entfällt die Aufgabe.
    public static List<DemoAufgabe> Aufgaben(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var liste = new List<DemoAufgabe>();
        DateTime Minute(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0);
        DateTime Stunde(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, 0, 0);
        foreach (var v in Lesen<AufgabenDatei>("aufgaben.json").Aufgaben)
        {
            Bewohner? b = null;
            if (v.Bewohner is string name && (b = bewohner.FirstOrDefault(x => x.Nachname == name)) is null) continue;
            var fuer = AufgabenRegeln.FuerAus(v.Fuer);
            liste.Add(new DemoAufgabe(new Aufgabe
            {
                BewohnerId = b?.Id,
                Text = v.Text.Replace("{vorname}", b?.Vorname ?? ""),
                Fuer = fuer,
                FuerSchicht = fuer == AufgabeFuer.Schicht
                    ? new SchichtTermin(DateOnly.FromDateTime(jetzt).AddDays(v.SchichtInTagen), BerichtRegeln.SchichtAus(v.Schicht))
                    : null,
                FaelligBis = v.FaelligInStunden is double h ? Stunde(jetzt.AddHours(h)) : null,
                Erstellt = Minute(jetzt.AddHours(-v.ErstelltVorStunden)),
                Verfasser = v.Verfasser,
                Weitergaben = v.Weitergabe is EreignisVorlage w ? [new Weitergabe(Minute(jetzt.AddHours(-w.VorStunden)), w.Kuerzel, w.Notiz)] : [],
                Erledigt = v.Erledigt is EreignisVorlage e ? Minute(jetzt.AddHours(-e.VorStunden)) : null,
                ErledigtVon = v.Erledigt?.Kuerzel,
            }));
        }
        return liste;
    }

    // ----- Nachrichten -----

    private sealed class NachrichtenDatei
    {
        public List<KollegeVorlage> Kollegen { get; set; } = [];
        public List<VerlaufVorlage> Verlaeufe { get; set; } = [];
    }

    public sealed class KollegeVorlage
    {
        public string Kuerzel { get; set; } = "";
        public string Vorname { get; set; } = "";
        public string Nachname { get; set; } = "";
        public List<string> Teams { get; set; } = [];
    }

    public sealed class VerlaufVorlage
    {
        public string Betreff { get; set; } = "";
        public string? Bewohner { get; set; }
        public List<string> ArchiviertVon { get; set; } = [];
        public List<NachrichtVorlage> Nachrichten { get; set; } = [];
    }

    public sealed class NachrichtVorlage
    {
        public string Von { get; set; } = "";
        public List<string>? An { get; set; }          // „team:Name“ oder Kürzel; fehlt = Antwort an alle
        public double VorStunden { get; set; }
        public string Text { get; set; } = "";
        public List<string> GelesenVon { get; set; } = [];
    }

    public sealed record DemoNachricht(Nachricht Nachricht);
    public sealed record DemoVerlauf(List<DemoNachricht> Nachrichten, List<string> ArchiviertVon);
    public sealed record DemoPost(List<Kollege> Kollegen, List<DemoVerlauf> Verlaeufe);

    // Kollegen, Teams und Verläufe aus daten/demo/nachrichten.json, Zeiten relativ zu „jetzt“.
    // Fehlt der genannte Bewohner (z. B. umbenannt), entfällt der Verlauf.
    public static DemoPost Nachrichten(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var datei = Lesen<NachrichtenDatei>("nachrichten.json");
        var kollegen = datei.Kollegen.Select(k => new Kollege(k.Kuerzel, k.Vorname, k.Nachname, k.Teams)).ToList();
        DateTime Minute(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0);
        var verlaeufe = new List<DemoVerlauf>();
        foreach (var v in datei.Verlaeufe)
        {
            Bewohner? b = null;
            if (v.Bewohner is string name && (b = bewohner.FirstOrDefault(x => x.Nachname == name)) is null) continue;
            var bisher = new List<Nachricht>();
            foreach (var d in v.Nachrichten)
            {
                var an = d.An is null
                    ? NachrichtenRegeln.AntwortAn(bisher, d.Von)
                    : d.An.Select(z => z.StartsWith("team:") ? Empfaenger.Team(z[5..]) : Empfaenger.Person(z)).ToList();
                bisher.Add(new Nachricht
                {
                    Von = d.Von,
                    Betreff = v.Betreff,
                    Text = d.Text.Replace("{vorname}", b?.Vorname ?? ""),
                    Gesendet = Minute(jetzt.AddHours(-d.VorStunden)),
                    BewohnerId = b?.Id,
                    An = an,
                    GelesenVon = d.GelesenVon,
                });
            }
            verlaeufe.Add(new DemoVerlauf(bisher.Select(x => new DemoNachricht(x)).ToList(), v.ArchiviertVon));
        }
        return new DemoPost(kollegen, verlaeufe);
    }

    // ----- Medikamentendatenbank (echte Nachschlagedaten, keine Bewohnerdaten) -----

    private sealed class MedikamenteDatei
    {
        public List<MedikamentVorlage> Medikamente { get; set; } = [];
    }

    public sealed class MedikamentVorlage
    {
        public string Wirkstoff { get; set; } = "";
        public List<string> Handelsnamen { get; set; } = [];
        public List<string> Staerken { get; set; } = [];
        public string Darreichungsform { get; set; } = "";
        public string Quelle { get; set; } = "";
        public string Pruefstatus { get; set; } = "ungeprueft";
    }

    // Kürzel der ersten Fassung im Demo-Bestand (kein Mensch, sondern die Datei).
    public const string DemoKuerzel = "DEMO";

    public static List<MedikamentFassung> Medikamente(DateTime jetzt) =>
        Lesen<MedikamenteDatei>("medikamente.json").Medikamente.Select(v => new MedikamentFassung
        {
            Wirkstoff = v.Wirkstoff,
            Handelsnamen = v.Handelsnamen,
            Staerken = v.Staerken,
            Darreichungsform = v.Darreichungsform,
            Quelle = v.Quelle,
            Pruefstatus = v.Pruefstatus == "bestaetigt" ? Pruefstatus.Bestaetigt : Pruefstatus.Ungeprueft,
            Geaendert = jetzt.Date,
            Kuerzel = DemoKuerzel,
        }).ToList();

    // ----- Medikation -----

    public sealed record DemoVerordnung(Verordnung Verordnung);

    // Ausgedachte Verordnungen relativ zu heute: Grundverordnungen, eine Pause, ein Stufenplan zum
    // Ansetzen, einer zum Absetzen, ein abgeschlossener (neue Grundverordnung), ein abgesetztes
    // Medikament und Bedarfsmedikation. Ärztinnen und Ärzte sind ausgedacht.
    // Fehlt ein Wirkstoff in der Medikamentendatenbank, entfällt die Verordnung.
    public static List<DemoVerordnung> Medikation(IReadOnlyList<Bewohner> bewohner, IReadOnlyList<Medikament> medikamente, DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        var liste = new List<DemoVerordnung>();
        var arzt1 = "Dr. Ulme (ausgedacht)";
        var arzt2 = "Dr. Linde (ausgedacht)";
        Medikament? M(string wirkstoff) => medikamente.FirstOrDefault(m => m.Wirkstoff.StartsWith(wirkstoff, StringComparison.CurrentCultureIgnoreCase));
        string Staerke(Medikament m, string gewuenscht) =>
            m.Aktuell.Staerken.Contains(gewuenscht) ? gewuenscht : m.Aktuell.Staerken.FirstOrDefault() ?? "";
        Bewohner? B(string nachname) => bewohner.FirstOrDefault(b => b.Nachname == nachname);
        VerordnungsAenderung A(AenderungArt art, DateOnly von, DateOnly? bis = null, IReadOnlyList<Stufe>? stufen = null, string hinweis = "") => new()
        {
            Art = art, Von = von, Bis = bis, Stufen = stufen ?? [], Anordnung = new Anordnung(arzt2, von < heute ? von : heute),
            Hinweis = hinweis, Erstellt = jetzt.AddDays(-1), Kuerzel = "AB",
        };
        void V(string nachname, string wirkstoff, string staerke, Dosis dosis, int beginnVorTagen, params VerordnungsAenderung[] aenderungen)
        {
            if (B(nachname) is not Bewohner b || M(wirkstoff) is not Medikament m) return;
            var beginn = heute.AddDays(-beginnVorTagen);
            liste.Add(new DemoVerordnung(new Verordnung
            {
                BewohnerId = b.Id!.Value, MedikamentId = m.Id, Medikament = m.Anzeige, Staerke = Staerke(m, staerke),
                Einheit = "Stück", Dosis = dosis, Beginn = beginn, Anordnung = new Anordnung(arzt1, beginn),
                Erstellt = beginn.ToDateTime(new TimeOnly(10, 0)), Kuerzel = "CD", Aenderungen = aenderungen,
            }));
        }
        void Bedarf(string nachname, string wirkstoff, string staerke, string einzeldosis, string anlass, string hoechstens, int beginnVorTagen)
        {
            if (B(nachname) is not Bewohner b || M(wirkstoff) is not Medikament m) return;
            var beginn = heute.AddDays(-beginnVorTagen);
            liste.Add(new DemoVerordnung(new Verordnung
            {
                BewohnerId = b.Id!.Value, MedikamentId = m.Id, Medikament = m.Anzeige, Staerke = Staerke(m, staerke),
                Einheit = "Stück", Dosis = new Dosis(einzeldosis, "0", "0", "0"), Bedarf = true, BedarfAnlass = anlass,
                BedarfHoechstens = hoechstens, Beginn = beginn, Anordnung = new Anordnung(arzt1, beginn),
                Erstellt = beginn.ToDateTime(new TimeOnly(10, 0)), Kuerzel = "CD",
            }));
        }
        Dosis D(string m, string mi, string a, string n) => new(m, mi, a, n);

        V("Ahorn", "Quetiapin", "25 mg", D("0", "0", "1", "0"), 400);
        V("Ahorn", "Pantoprazol", "40 mg", D("1", "0", "0", "0"), 200);
        V("Birke", "Sertralin", "50 mg", D("1", "0", "0", "0"), 12,
            A(AenderungArt.Stufenplan, heute.AddDays(-10), stufen: [new(heute.AddDays(-10), D("½", "0", "0", "0")), new(heute.AddDays(-3), D("1", "0", "0", "0")), new(heute.AddDays(4), D("1½", "0", "0", "0"))], hinweis: "langsam ansetzen"));
        V("Clematis", "Mirtazapin", "15 mg", D("0", "0", "0", "1"), 90,
            A(AenderungArt.Pause, heute.AddDays(-2), heute.AddDays(3), hinweis: "wegen Blutabnahme"));
        Bedarf("Distel", "Lorazepam", "1,0 mg", "1", "bei starker Unruhe", "2 in 24 Stunden", 60);
        V("Eiche", "Olanzapin", "10 mg", D("0", "0", "1", "0"), 300,
            A(AenderungArt.Stufenplan, heute.AddDays(-7), stufen: [new(heute.AddDays(-7), D("0", "0", "½", "0")), new(heute.AddDays(7), D("0", "0", "0", "0"))], hinweis: "ausschleichen"));
        V("Fichte", "Ramipril", "5 mg", D("1", "0", "0", "0"), 500);
        V("Fichte", "Metformin", "500 mg", D("1", "0", "1", "0"), 500);
        V("Ginster", "Levothyroxin", "50 Mikrogramm", D("1", "0", "0", "0"), 700);
        V("Holunder", "Escitalopram", "10 mg", D("1", "0", "0", "0"), 150,
            A(AenderungArt.Absetzen, heute.AddDays(-5), hinweis: "auf ärztliche Anordnung beendet"));
        Bedarf("Immergrün", "Paracetamol", "500 mg", "1", "bei Kopfschmerzen", "4 in 24 Stunden", 30);
        V("Kiefer", "Risperidon", "2 mg", D("1", "0", "1", "0"), 120,
            A(AenderungArt.Stufenplan, heute.AddDays(-40), stufen: [new(heute.AddDays(-40), D("1", "0", "½", "0")), new(heute.AddDays(-30), D("1", "0", "0", "0"))], hinweis: "Umstellung"));
        return liste;
    }

    // ----- Therapie -----

    private sealed class TherapieDatei
    {
        public List<AngebotVorlage> Angebote { get; set; } = [];
    }

    public sealed class TerminVorlage
    {
        public string Tag { get; set; } = "Mo";
        public string Von { get; set; } = "";
        public string Bis { get; set; } = "";
    }

    public sealed class AenderungVorlage
    {
        public int GiltAbVorTagen { get; set; }
        public List<TerminVorlage> Termine { get; set; } = [];
        public string Hinweis { get; set; } = "";
    }

    public sealed class TeilnehmerVorlage
    {
        public string Bewohner { get; set; } = "";
        public int AbVorTagen { get; set; }
        public int? BeendetVorTagen { get; set; }
    }

    public sealed class AngebotVorlage
    {
        public string Name { get; set; } = "";
        public string Art { get; set; } = "gruppe";
        public string Therapeut { get; set; } = "";
        public string Ort { get; set; } = "";
        public int Hoechstens { get; set; }
        public string Hinweis { get; set; } = "";
        public int SeitTagen { get; set; }
        public List<TerminVorlage> Termine { get; set; } = [];
        public AenderungVorlage? Aenderung { get; set; }
        public List<TeilnehmerVorlage> Teilnehmer { get; set; } = [];
    }

    public sealed record DemoAngebot(List<AngebotFassung> Fassungen, List<Zuteilung> Zuteilungen);

    private static readonly Dictionary<string, DayOfWeek> Kurztage = new()
    {
        ["Mo"] = DayOfWeek.Monday, ["Di"] = DayOfWeek.Tuesday, ["Mi"] = DayOfWeek.Wednesday, ["Do"] = DayOfWeek.Thursday,
        ["Fr"] = DayOfWeek.Friday, ["Sa"] = DayOfWeek.Saturday, ["So"] = DayOfWeek.Sunday,
    };

    // Angebote aus daten/demo/therapie.json; Zuteilungen relativ zu heute. Fehlt ein Bewohner,
    // entfällt nur seine Zuteilung.
    public static List<DemoAngebot> Therapie(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        static List<Wochentermin> Termine(IEnumerable<TerminVorlage> t) => t.Select(x => new Wochentermin(Kurztage[x.Tag],
            TimeOnly.ParseExact(x.Von, "HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(x.Bis, "HH:mm", System.Globalization.CultureInfo.InvariantCulture))).ToList();
        var liste = new List<DemoAngebot>();
        foreach (var v in Lesen<TherapieDatei>("therapie.json").Angebote)
        {
            var erste = new AngebotFassung
            {
                GiltAb = heute.AddDays(-v.SeitTagen), Name = v.Name, Art = v.Art == "einzel" ? AngebotArt.Einzel : AngebotArt.Gruppe,
                Therapeut = v.Therapeut, Ort = v.Ort, Termine = Termine(v.Termine), Hoechstens = v.Hoechstens, Hinweis = v.Hinweis,
                Erstellt = heute.AddDays(-v.SeitTagen - 3).ToDateTime(new TimeOnly(9, 0)), Kuerzel = "OP",
            };
            var fassungen = new List<AngebotFassung> { erste };
            if (v.Aenderung is AenderungVorlage a)
                fassungen.Add(erste with
                {
                    GiltAb = heute.AddDays(-a.GiltAbVorTagen), Termine = Termine(a.Termine), Hinweis = a.Hinweis,
                    Erstellt = heute.AddDays(-a.GiltAbVorTagen - 2).ToDateTime(new TimeOnly(9, 0)),
                });
            var zuteilungen = new List<Zuteilung>();
            foreach (var t in v.Teilnehmer)
            {
                if (bewohner.FirstOrDefault(b => b.Nachname == t.Bewohner) is not Bewohner b) continue;
                var ab = heute.AddDays(-t.AbVorTagen);
                zuteilungen.Add(new Zuteilung
                {
                    BewohnerId = b.Id!.Value, Ab = ab, Kuerzel = "OP",
                    Erstellt = (t.AbVorTagen > 0 ? ab.AddDays(-2) : heute.AddDays(-1)).ToDateTime(new TimeOnly(10, 0)),
                    BeendetAb = t.BeendetVorTagen is int e ? heute.AddDays(-e) : null,
                    BeendetVon = t.BeendetVorTagen is null ? null : "OP",
                    BeendetErstellt = t.BeendetVorTagen is int e2 ? heute.AddDays(-e2 - 1).ToDateTime(new TimeOnly(11, 0)) : null,
                });
            }
            liste.Add(new DemoAngebot(fassungen, zuteilungen));
        }
        return liste;
    }

    // ----- Wochenplan -----

    private sealed class WochenplanDatei
    {
        public List<PlanVorlage> Eintraege { get; set; } = [];
    }

    public sealed class PlanVorlage
    {
        public string Titel { get; set; } = "";
        public string Tag { get; set; } = "Mo";
        public string Von { get; set; } = "";
        public string Bis { get; set; } = "";
        public string Ort { get; set; } = "";
        public System.Text.Json.JsonElement Fuer { get; set; }
        public int AbVorTagen { get; set; }
        public int? EntferntVorTagen { get; set; }
        public PlanAenderungVorlage? Aenderung { get; set; }
    }

    public sealed class PlanAenderungVorlage
    {
        public string Fuer { get; set; } = "";
        public string Tag { get; set; } = "Mo";
        public string Von { get; set; } = "";
        public string Bis { get; set; } = "";
        public int AbVorTagen { get; set; }
        public string Hinweis { get; set; } = "";
    }

    public sealed record DemoPlan(PlanEintrag Eintrag, PlanEintrag? Ersatz, DateOnly? EntferntAb);

    // Freie Einträge aus daten/demo/wochenplan.json, relativ zu heute.
    public static List<DemoPlan> Wochenplan(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        static TimeOnly Zeit(string s) => TimeOnly.ParseExact(s, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var liste = new List<DemoPlan>();
        foreach (var v in Lesen<WochenplanDatei>("wochenplan.json").Eintraege)
        {
            var namen = v.Fuer.ValueKind == System.Text.Json.JsonValueKind.String && v.Fuer.GetString() == "alle"
                ? bewohner.Select(b => b.Nachname).ToList()
                : v.Fuer.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            foreach (var b in bewohner.Where(b => namen.Contains(b.Nachname)))
            {
                var ab = heute.AddDays(-v.AbVorTagen);
                var eintrag = new PlanEintrag
                {
                    BewohnerId = b.Id!.Value, Termin = new Wochentermin(Kurztage[v.Tag], Zeit(v.Von), Zeit(v.Bis)), Titel = v.Titel, Ort = v.Ort,
                    Ab = ab, Erstellt = ab.AddDays(-1).ToDateTime(new TimeOnly(10, 0)), Kuerzel = "AB",
                };
                PlanEintrag? ersatz = null;
                if (v.Aenderung is PlanAenderungVorlage a && a.Fuer == b.Nachname)
                {
                    var neuAb = heute.AddDays(-a.AbVorTagen);
                    ersatz = eintrag with
                    {
                        Termin = new Wochentermin(Kurztage[a.Tag], Zeit(a.Von), Zeit(a.Bis)), Hinweis = a.Hinweis, Ab = neuAb,
                        Erstellt = neuAb.AddDays(-1).ToDateTime(new TimeOnly(11, 0)), Kuerzel = "CD",
                    };
                }
                liste.Add(new DemoPlan(eintrag, ersatz, v.EntferntVorTagen is int e ? heute.AddDays(-e) : null));
            }
        }
        return liste;
    }

    // ----- Kalender -----

    private sealed class KalenderDatei
    {
        public List<KalenderVorlage> Termine { get; set; } = [];
    }

    public sealed class KalenderVorlage
    {
        public string Art { get; set; } = "bewohner";
        public string Titel { get; set; } = "";
        public List<string> Bewohner { get; set; } = [];
        public int? InTagen { get; set; }
        public string? Naechster { get; set; }
        public string Von { get; set; } = "";
        public int? EndeInTagen { get; set; }
        public string Bis { get; set; } = "";
        public string Ort { get; set; } = "";
        public string Hinweis { get; set; } = "";
        public string Treffpunkt { get; set; } = "";
        public string Begleitung { get; set; } = "";
        public bool Abgesagt { get; set; }
        public KalenderAenderungVorlage? Aenderung { get; set; }
    }

    public sealed class KalenderAenderungVorlage
    {
        public int InTagen { get; set; }
        public string Von { get; set; } = "";
        public string Bis { get; set; } = "";
        public string Hinweis { get; set; } = "";
    }

    // Termine aus daten/demo/kalender.json, relativ zu heute; je Termin die Fassungen.
    // Fehlt ein genannter Bewohner, entfällt der Termin.
    public static List<List<KalenderFassung>> Kalender(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        static TimeOnly Zeit(string s) => TimeOnly.ParseExact(s, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var liste = new List<List<KalenderFassung>>();
        foreach (var v in Lesen<KalenderDatei>("kalender.json").Termine)
        {
            var ids = v.Bewohner.Select(n => bewohner.FirstOrDefault(b => b.Nachname == n)?.Id).ToList();
            if (ids.Any(i => i is null)) continue;
            DateOnly tag = heute.AddDays(v.InTagen ?? 0);
            if (v.Naechster is string w)
            {
                int abstand = ((int)Kurztage[w] - (int)heute.DayOfWeek + 7) % 7;
                tag = heute.AddDays(abstand == 0 ? 7 : abstand);   // der nächste solche Wochentag nach heute
            }
            var ende = v.EndeInTagen is int e ? heute.AddDays(e) : tag;
            var erste = new KalenderFassung
            {
                Art = KalenderRegeln.ArtAus(v.Art), Titel = v.Titel, Beginn = tag.ToDateTime(Zeit(v.Von)), Ende = ende.ToDateTime(Zeit(v.Bis)),
                Ort = v.Ort, Hinweis = v.Hinweis, Bewohner = ids.Select(i => i!.Value).ToList(), Treffpunkt = v.Treffpunkt, Begleitung = v.Begleitung,
                Erstellt = (tag < heute ? tag : heute).AddDays(-7).ToDateTime(new TimeOnly(9, 0)), Kuerzel = "AB",
            };
            var fassungen = new List<KalenderFassung> { erste };
            if (v.Aenderung is KalenderAenderungVorlage a)
            {
                var neu = heute.AddDays(a.InTagen);
                fassungen.Add(erste with
                {
                    Beginn = neu.ToDateTime(Zeit(a.Von)), Ende = neu.ToDateTime(Zeit(a.Bis)), Hinweis = a.Hinweis,
                    Erstellt = jetzt.AddHours(-5), Kuerzel = "CD",
                });
            }
            if (v.Abgesagt) fassungen.Add(fassungen[^1] with { Abgesagt = true, Erstellt = jetzt.AddHours(-3), Kuerzel = "CD" });
            liste.Add(fassungen);
        }
        return liste;
    }

    // ----- Biografie -----

    private sealed class BiografieDatei
    {
        public List<BiografieVorlage> Biografien { get; set; } = [];
    }

    public sealed class BiografieVorlage
    {
        public string Bewohner { get; set; } = "";
        public List<AbschnittVorlage> Abschnitte { get; set; } = [];
        public BiografieAenderungVorlage? Aenderung { get; set; }
    }

    public sealed class AbschnittVorlage
    {
        public int Nr { get; set; }
        public string Text { get; set; } = "";
        public bool Angegeben { get; set; }
    }

    public sealed class BiografieAenderungVorlage
    {
        public int Nr { get; set; }
        public int VorTagen { get; set; }
        public string Alt { get; set; } = "";
    }

    // Biografien aus daten/demo/biografie.json; bei einer Änderung zuerst die alte Fassung.
    public static List<(long BewohnerId, BiografieFassung Fassung)> Biografien(IReadOnlyList<Bewohner> bewohner, DateTime jetzt)
    {
        var liste = new List<(long, BiografieFassung)>();
        foreach (var v in Lesen<BiografieDatei>("biografie.json").Biografien)
        {
            if (bewohner.FirstOrDefault(b => b.Nachname == v.Bewohner) is not Bewohner b) continue;
            var angelegt = jetzt.Date.AddDays(-60).AddHours(10);
            foreach (var a in v.Abschnitte)
            {
                if (v.Aenderung is BiografieAenderungVorlage ae && ae.Nr == a.Nr)
                {
                    liste.Add((b.Id!.Value, new BiografieFassung { Abschnitt = a.Nr, Text = ae.Alt, Erstellt = angelegt, Kuerzel = "AB" }));
                    liste.Add((b.Id!.Value, new BiografieFassung { Abschnitt = a.Nr, Text = a.Text, Angegeben = a.Angegeben, Erstellt = jetzt.Date.AddDays(-ae.VorTagen).AddHours(15), Kuerzel = "CD" }));
                }
                else liste.Add((b.Id!.Value, new BiografieFassung { Abschnitt = a.Nr, Text = a.Text, Angegeben = a.Angegeben, Erstellt = angelegt, Kuerzel = "AB" }));
            }
        }
        return liste;
    }

    private static Bericht Erzeugen(Bewohner b, DateTime zeit, Random zufall, BerichtTexte bausteine, DateTime jetzt, bool wichtig)
    {
        int bereich = zufall.Next(0, 6);   // 0 = allgemein
        var texte = bausteine.Texte[bereich == 0 ? "allgemein" : bereich.ToString()];
        var nachtraege = new List<Nachtrag>();
        if (zufall.Next(20) == 0)
        {
            var erfasst = zeit.AddHours(zufall.Next(1, 4));
            if (erfasst <= jetzt)
                nachtraege.Add(new Nachtrag(0, 0, erfasst, bausteine.Kuerzel[zufall.Next(bausteine.Kuerzel.Count)],
                    bausteine.Nachtraege[zufall.Next(bausteine.Nachtraege.Count)]));
        }
        return new Bericht
        {
            BewohnerId = b.Id ?? 0,
            Zeitpunkt = zeit,
            Schicht = BerichtRegeln.SchichtZu(zeit),
            Verfasser = bausteine.Kuerzel[zufall.Next(bausteine.Kuerzel.Count)],
            Bereiche = bereich == 0 ? [] : [bereich],
            Text = texte[zufall.Next(texte.Count)],
            Wichtig = wichtig,
            Erfasst = zeit.AddMinutes(zufall.Next(5, 40)) is var e && e <= jetzt ? e : jetzt,
            Nachtraege = nachtraege,
        };
    }
}
