using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Mehrplatzbetrieb (Demo-Stufe): zwei Verbindungen auf dieselbe Datei wie zwei Rechner mit einem
// gemeinsamen Ordner. Konfliktschutz, Bereiche, Statusmeldungen, Ort der Datenbank.
[Collection("Datenordner")]
public sealed class MehrplatzTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 8, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher a;
    private readonly Speicher b;

    public MehrplatzTests()
    {
        a = new Speicher { Uhr = () => Jetzt };
        a.DemoBeimErstenStart();
        b = new Speicher(a.Pfad) { Uhr = () => Jetzt };   // „zweiter Rechner“
    }

    public void Dispose()
    {
        b.Dispose();
        a.Dispose();
        daten.Dispose();
    }

    private Bewohner Demo(Speicher s, string nachname) => s.AlleBewohner().First(x => x.Nachname == nachname);

    [Fact]
    public void Datenstand_aendert_sich_nur_durch_den_anderen()
    {
        long vorher = b.Datenstand();
        long eigenerVorher = a.Datenstand();
        a.StatusmeldungSetzen(Demo(a, "Birke").Id!.Value, "beim Einkaufen", Jetzt.AddHours(1), "AB", "Wohnbereich A");
        Assert.NotEqual(vorher, b.Datenstand());          // b sieht: jemand anderes hat gespeichert
        Assert.Equal(eigenerVorher, a.Datenstand());      // eigenes Speichern zählt nicht
        Assert.Equal("beim Einkaufen", b.Statusmeldung(Demo(b, "Birke").Id!.Value, Jetzt)!.Text);
    }

    [Fact]
    public void Bewohner_zweimal_geaendert_gibt_Konflikt_mit_beiden_Fassungen()
    {
        var beiA = Demo(a, "Birke");
        var beiB = Demo(b, "Birke");
        a.BewohnerSpeichern(beiA with { Zimmer = "202" }, ausgangsfassung: beiA);
        var k = Assert.Throws<Konflikt>(() => b.BewohnerSpeichern(beiB with { Zimmer = "303" }, ausgangsfassung: beiB));
        Assert.Contains("Zimmer: 202", k.Andere);
        Assert.Contains("Zimmer: 303", k.Meine);
        Assert.Equal("202", Demo(a, "Birke").Zimmer);    // nichts still überschrieben
        // Dieselbe Änderung wie der andere ist kein Konflikt; ohne Ausgangsfassung („Meine speichern“) gewinnt meine.
        b.BewohnerSpeichern(beiB with { Zimmer = "202" }, ausgangsfassung: beiB);
        b.BewohnerSpeichern(beiB with { Zimmer = "303" });
        Assert.Equal("303", Demo(a, "Birke").Zimmer);
    }

    [Fact]
    public void Fassungen_mit_Konfliktschutz()
    {
        long birke = Demo(a, "Birke").Id!.Value;
        // Biografie
        var abschnitt = b.Biografie(birke)[0];
        a.BiografieSpeichern(birke, BiografieRegeln.AusEingabe(0, "Text von A.", false, "AB", Jetzt), abschnitt.Fassungen.Count);
        var k = Assert.Throws<Konflikt>(() => b.BiografieSpeichern(birke, BiografieRegeln.AusEingabe(0, "Text von B.", false, "GH", Jetzt), abschnitt.Fassungen.Count));
        Assert.Contains("Text von A.", k.Andere);
        Assert.Contains("Text von B.", k.Meine);
        Assert.Equal("Text von A.", a.Biografie(birke)[0].Aktuell!.Text);
        b.BiografieSpeichern(birke, BiografieRegeln.AusEingabe(0, "Text von B.", false, "GH", Jetzt));   // „Meine speichern“
        Assert.Equal(["Text von A.", "Text von B."], a.Biografie(birke)[0].Fassungen.TakeLast(2).Select(f => f.Text));   // beide im Verlauf

        // Reflexion
        var monat = ReflexionRegeln.Vorschlag(DateOnly.FromDateTime(Jetzt));
        int n = b.Reflexion(birke, monat).Fassungen.Count;
        string[] texte = ["von A", "", "", "", ""];
        a.ReflexionSpeichern(ReflexionRegeln.AusEingabe(birke, monat, texte, false, "AB", Jetzt), n);
        Assert.Throws<Konflikt>(() => b.ReflexionSpeichern(ReflexionRegeln.AusEingabe(birke, monat, ["von B", "", "", "", ""], false, "GH", Jetzt), n));

        // HEB
        var bogen = a.HebAnlegen(birke, Bogenart.A, Demo(a, "Birke").Aufnahmedatum, DateOnly.FromDateTime(Jetzt), "AB");
        int h = bogen.Fassungen.Count;
        a.HebSpeichern(HebRegeln.AusEingabe(bogen.Id, Bogenart.A, new Dictionary<string, string> { ["1a"] = "von A" }, new Dictionary<int, int?>(), false, "AB", Jetzt), h);
        var hk = Assert.Throws<Konflikt>(() => b.HebSpeichern(HebRegeln.AusEingabe(bogen.Id, Bogenart.A, new Dictionary<string, string> { ["1a"] = "von B" }, new Dictionary<int, int?>(), false, "GH", Jetzt), h));
        Assert.Contains("von A", hk.Andere);

        // Kalender
        var termin = a.KalenderAnlegen(KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Bewohner, Titel = "Friseur", BeginnDatum = "09.11.2026", BeginnUhrzeit = "10:00", EndeUhrzeit = "11:00",
            Bewohner = [birke], Kuerzel = "AB",
        }, Jetzt));
        int t = termin.Fassungen.Count;
        a.KalenderAendern(termin.Id, termin.Aktuell with { Ort = "Salon" }, t);
        var kk = Assert.Throws<Konflikt>(() => b.KalenderAendern(termin.Id, termin.Aktuell with { Ort = "Stadt" }, t));
        Assert.Contains("Ort: Salon", kk.Andere);
        Assert.Contains("Ort: Stadt", kk.Meine);
        Assert.Contains("Berta Birke", kk.Andere);
    }

    [Fact]
    public void Statusmeldung_gilt_bis_sie_ablaeuft_oder_ersetzt_wird()
    {
        long birke = Demo(a, "Birke").Id!.Value;
        Assert.Null(a.Statusmeldung(birke, Jetzt));
        a.StatusmeldungSetzen(birke, "  beim Arzttermin ", Jetzt.AddHours(1), "AB", "Wohnbereich A");
        var s = b.Statusmeldung(birke, Jetzt)!;   // auch am anderen Arbeitsplatz
        Assert.Equal(("beim Arzttermin", "Wohnbereich A", "AB"), (s.Text, s.Bereich, s.Kuerzel));
        Assert.Equal("beim Arzttermin · bis 11:00 · Wohnbereich A, AB um 10:00", StatusmeldungRegeln.Langtext(s, Jetzt));
        Assert.Equal(new Marke(MarkenArt.Status, "Status: beim Arzttermin, bis 11:00"), StatusmeldungRegeln.Marke(s, Jetzt));
        Assert.Null(a.Statusmeldung(birke, Jetzt.AddHours(1)));   // abgelaufen

        b.StatusmeldungSetzen(birke, "in der Holzwerkstatt", Jetzt.AddHours(2), "OP", "Therapie");   // ersetzt
        Assert.Equal("Therapie", a.Statusmeldung(birke, Jetzt)!.Bereich);
        Assert.Equal("in der Holzwerkstatt", a.GeltendeStatusmeldungen(Jetzt)[birke].Text);
        a.StatusmeldungAufheben(birke, "AB", "Wohnbereich A");
        Assert.Null(b.Statusmeldung(birke, Jetzt));
        Assert.DoesNotContain(birke, b.GeltendeStatusmeldungen(Jetzt).Keys);
        var verlauf = StatusmeldungRegeln.Verlauf(a.Statusmeldungen(birke));
        Assert.Equal(3, verlauf.Count);                            // Verlauf bleibt
        Assert.EndsWith("AB (Wohnbereich A) · aufgehoben", verlauf[0]);
        Assert.Contains("in der Holzwerkstatt", verlauf[1]);
    }

    [Fact]
    public void Statusmeldung_Pruefregeln_und_unveraenderlich()
    {
        long birke = Demo(a, "Birke").Id!.Value;
        var fehler = Assert.Throws<Eingabefehler>(() => a.StatusmeldungSetzen(birke, "", Jetzt.AddHours(-1), "", "Wohnbereich A")).Fehler;
        Assert.Contains("Text fehlt.", fehler);
        Assert.Contains("„Gilt bis“ liegt in der Vergangenheit.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("Kürzel fehlt"));
        Assert.Throws<Eingabefehler>(() => a.StatusmeldungSetzen(birke, new string('x', StatusmeldungRegeln.Laenge + 1), Jetzt.AddHours(1), "AB", "A"));
        var (text, bis) = StatusmeldungRegeln.AusEingabe("beim Zahnarzt", "08.11.2026", "9:30", "AB", Jetzt.AddHours(-2));
        Assert.Equal(("beim Zahnarzt", new DateTime(2026, 11, 8, 9, 30, 0)), (text, bis));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => StatusmeldungRegeln.AusEingabe("x", "08.11.2026", "halb zehn", "AB", Jetzt)).Fehler,
            f => f.StartsWith("Gilt bis (Uhrzeit)"));

        a.StatusmeldungSetzen(birke, "unterwegs", Jetzt.AddHours(1), "AB", "Wohnbereich A");
        using var befehl = a.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE statusmeldungen SET text = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Wohnbereich_je_Bewohner()
    {
        var alle = a.AlleBewohner();
        Assert.Equal(["Ginster", "Immergrün"], alle.Where(x => x.Wohnbereich == "Wohnbereich B").Select(x => x.Nachname));
        Assert.Equal(alle.Count - 2, alle.Count(x => x.Wohnbereich == Bereiche.Vorgabe));
        var e = BewohnerRegeln.AlsEingabe(alle[0]);
        e.Wohnbereich = "  Wohnbereich   C ";
        Assert.Equal("Wohnbereich C", BewohnerRegeln.AusEingabe(e, alle[0].Id).Wohnbereich);
        e.Wohnbereich = "";
        Assert.Equal(Bereiche.Vorgabe, BewohnerRegeln.AusEingabe(e).Wohnbereich);
        e.Wohnbereich = new string('W', Bereiche.Laenge + 1);
        Assert.Contains(Assert.Throws<Eingabefehler>(() => BewohnerRegeln.AusEingabe(e)).Fehler, f => f.StartsWith("Wohnbereich:"));
    }

    [Fact]
    public void Demo_Statusmeldungen_relativ_zu_jetzt()
    {
        var status = a.GeltendeStatusmeldungen(Jetzt);
        var ahorn = status[Demo(a, "Ahorn").Id!.Value];
        Assert.Equal(("in der Holzwerkstatt", "Therapie", "OP"), (ahorn.Text, ahorn.Bereich, ahorn.Kuerzel));
        Assert.True(ahorn.GiltBis > Jetzt);
        Assert.Equal("Wohnbereich B", status[Demo(a, "Immergrün").Id!.Value].Bereich);
        Assert.Equal(2, a.Statusmeldungen(Demo(a, "Ahorn").Id!.Value).Count);   // eine ältere im Verlauf
        Assert.Equal(2, status.Count);
    }

    [Fact]
    public void Einstellungen_Bereich_und_Ort_der_Datenbank()
    {
        Assert.Equal((Bereiche.Vorgabe, (string?)null), (Einstellungen.Laden().Bereich, Einstellungen.Laden().DatenbankOrdner));
        Assert.Equal(Pfade.ImDatenordner(Speicher.Datei), Einstellungen.DatenbankPfad(Einstellungen.Laden()));
        using var gemeinsam = new Hilfsordner();
        Einstellungen.Aendern(w => w with { Bereich = "Therapie", DatenbankOrdner = gemeinsam.Ordner });
        var w = Einstellungen.Laden();
        Assert.Equal(("Therapie", gemeinsam.Ordner), (w.Bereich, w.DatenbankOrdner));
        Assert.True(Einstellungen.Gemeinsam(w));
        Assert.Equal(Path.Combine(gemeinsam.Ordner, Speicher.Datei), Einstellungen.DatenbankPfad(w));
        Assert.Throws<Eingabefehler>(() => Einstellungen.Speichern(w with { Bereich = "" }));
        File.WriteAllText(Pfade.ImDatenordner(Einstellungen.Datei), """{ "bereich": "", "datenbank_ordner": "  " }""");
        Assert.Equal((Bereiche.Vorgabe, (string?)null), (Einstellungen.Laden().Bereich, Einstellungen.Laden().DatenbankOrdner));
    }

    [Fact]
    public void Oeffnen_im_gemeinsamen_Ordner_oder_lokal_mit_Hinweis()
    {
        using var gemeinsam = new Hilfsordner();
        var (dort, hinweis) = Speicher.Oeffnen(new EinstellungsWerte(DatenbankOrdner: gemeinsam.Ordner));
        using (dort)
        {
            Assert.Null(hinweis);
            Assert.Equal(Path.Combine(gemeinsam.Ordner, Speicher.Datei), dort.Pfad);
        }
        string fehlt = Path.Combine(gemeinsam.Ordner, "gibt-es-nicht");
        var (lokal, warnung) = Speicher.Oeffnen(new EinstellungsWerte(DatenbankOrdner: fehlt));
        using (lokal)
        {
            Assert.Equal(a.Pfad, lokal.Pfad);
            Assert.Contains("lokalen Datenbank", warnung);
            Assert.False(Directory.Exists(fehlt));   // kein Ordner angelegt
        }
        Assert.Null(Speicher.OrdnerPruefen(gemeinsam.Ordner));
        Assert.NotNull(Speicher.OrdnerPruefen(fehlt));
        Assert.Empty(Directory.GetFiles(gemeinsam.Ordner, ".dokusystem-probe*"));
    }
}
