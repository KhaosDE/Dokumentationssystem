using System.Text;
using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 31: Akte, Reiter „Dokumente“ – kopieren statt verschieben, Prüfsumme, Protokoll, Papierkorb, Rechte, Demo.
[Collection("Datenordner")]
public sealed class DokumenteTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private readonly string quelle;

    public DokumenteTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
        quelle = Path.Combine(Path.GetTempPath(), $"dokuline-test-{Guid.NewGuid():N}", "Arztbrief_Mai 2026.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(quelle)!);
        File.WriteAllBytes(quelle, DemoPdf.Erzeugen("Arztbrief", ["ausgedacht"]));
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
        try { Directory.Delete(Path.GetDirectoryName(quelle)!, recursive: true); } catch (IOException) { }
    }

    private static DokumentEingabe E(string titel = "Arztbrief", string kategorie = "Ärztliches und Befunde") =>
        new() { Kategorie = kategorie, Titel = titel, Datum = Heute.AddDays(-3), GueltigBis = Heute.AddDays(30) };

    [Fact]
    public void Hinzufuegen_kopiert_und_oeffnen_steht_im_Protokoll()
    {
        long id = s.AlleBewohner().Last().Id!.Value;
        int vorher = s.Dokumente(id).Count;
        var d = s.DokumentHinzufuegen(id, quelle, E(), "CD", Jetzt);
        Assert.True(File.Exists(quelle));   // das Original bleibt
        string pfad = s.DokumentOeffnen(d.Id, "CD", Jetzt);
        Assert.StartsWith(s.Ablage(id), pfad);
        Assert.Equal(File.ReadAllBytes(quelle), File.ReadAllBytes(pfad));
        Assert.Matches("^[0-9a-f]{12}-Arztbrief_Mai_2026\\.pdf$", d.Datei);
        Assert.Equal(vorher + 1, s.Dokumente(id).Count);
        Assert.Equal([DokumentAktion.Geoeffnet, DokumentAktion.Hinzugefuegt], s.DokumentProtokoll(id).Where(x => x.DokumentId == d.Id).Select(x => x.Aktion));
        // Außerhalb verändert: Öffnen meldet es, statt still Falsches zu zeigen.
        File.AppendAllText(pfad, "x");
        Assert.Contains("Prüfsumme", Assert.Throws<Eingabefehler>(() => s.DokumentOeffnen(d.Id, "CD", Jetzt)).Fehler[0]);
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() =>
        {
            using var befehl = s.Verbindung.CreateCommand();
            befehl.CommandText = "UPDATE dokumente SET titel = 'anders'";
            befehl.ExecuteNonQuery();
        });
    }

    [Fact]
    public void Papierkorb_Wiederherstellen_Ersetzen_Loeschen_mit_Rechten()
    {
        long id = s.AlleBewohner().Last().Id!.Value;
        var d = s.DokumentHinzufuegen(id, quelle, E(), "CD", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.DokumentEntfernen(d.Id, "", "CD", Jetzt));   // Grund fehlt
        s.DokumentEntfernen(d.Id, "doppelt", "CD", Jetzt);
        Assert.DoesNotContain(s.Dokumente(id), x => x.Id == d.Id);
        Assert.Contains(s.Dokumente(id, auchEntfernte: true), x => x.Id == d.Id && x.Entfernt);
        s.DokumentWiederherstellen(d.Id, "CD", Jetzt);
        Assert.Contains(s.Dokumente(id), x => x.Id == d.Id);
        // Ersetzen: eine neue Fassung, die alte verschwindet aus der Liste, bleibt aber erhalten.
        var neu = s.DokumentErsetzen(d.Id, quelle, E("Arztbrief neu"), "CD", Jetzt);
        Assert.Equal(d.Id, neu.Vorgaenger);
        Assert.DoesNotContain(s.Dokumente(id), x => x.Id == d.Id);
        Assert.True(s.DokumentNach(d.Id)!.Ersetzt);
        // Endgültig löschen: nur aus dem Papierkorb, nur mit eigenem Recht (CD hat es nicht, EF schon).
        Assert.Throws<Eingabefehler>(() => s.DokumentEndgueltigLoeschen(neu.Id, "Datenschutz", "EF", Jetzt));   // nicht im Papierkorb
        s.DokumentEntfernen(neu.Id, "falscher Bewohner", "CD", Jetzt);
        Assert.Throws<Eingabefehler>(() => s.DokumentEndgueltigLoeschen(neu.Id, "Datenschutz", "CD", Jetzt));
        string pfad = Path.Combine(s.Ablage(id), neu.Datei);
        s.DokumentEndgueltigLoeschen(neu.Id, "Datenschutz", "EF", Jetzt);
        Assert.False(File.Exists(pfad));
        Assert.True(s.DokumentNach(neu.Id)!.Geloescht);
        Assert.Throws<Eingabefehler>(() => s.DokumentOeffnen(neu.Id, "EF", Jetzt));
        // Ohne Recht „Dokumente ansehen“ (Therapie) kein Öffnen; ohne Anmeldung kein Hinzufügen.
        var demo = s.Dokumente(s.AlleBewohner().First(b => s.Dokumente(b.Id!.Value).Count > 0).Id!.Value)[0];
        Assert.Throws<Eingabefehler>(() => s.DokumentOeffnen(demo.Id, "OP", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.DokumentHinzufuegen(id, quelle, E(), "", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.DokumentHinzufuegen(id, quelle, E(kategorie: "Gibt es nicht"), "CD", Jetzt));
    }

    [Fact]
    public void Vorschlaege_Ablagename_und_Demo_PDF()
    {
        var kategorien = DokumentRegeln.Kategorienvorgabe;
        Assert.Equal("Betreuung und Gericht", DokumentRegeln.KategorieVorschlag("Betreuerausweis_2026.pdf", kategorien));
        Assert.Equal("Kostenträger und Bescheide", DokumentRegeln.KategorieVorschlag("Bescheid Bezirk.pdf", kategorien));
        Assert.Equal("Sonstiges", DokumentRegeln.KategorieVorschlag("scan0001.jpg", kategorien));
        Assert.Equal("Betreuerausweis 2026", DokumentRegeln.TitelVorschlag("Betreuerausweis_2026.pdf"));
        string name = DokumentRegeln.AblageName("abc", "..\\..\\Windows\\system32.exe");
        Assert.StartsWith("abc-", name);
        Assert.EndsWith("system32.exe", name);
        Assert.DoesNotContain('\\', name);
        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain("..", DokumentRegeln.AblageName("abc", "../../böse.pdf"));
        var pdf = DemoPdf.Erzeugen("Kostenzusage", ["Zeile (mit Klammern) und Ä"]);
        string text = Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("DEMO \u0097 ausgedachte Daten", text);
        Assert.Contains("Zeile \\(mit Klammern\\) und Ä", text);
        Assert.EndsWith("%%EOF\n", text);
        int xref = int.Parse(text[(text.LastIndexOf("startxref\n", StringComparison.Ordinal) + 10)..].Split('\n')[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.StartsWith("xref", text[xref..]);
        // Demo: je Bereich vier Bewohner mit drei Dokumenten; eine Kostenzusage abgelaufen; die Dateien liegen in der Ablage.
        var mit = s.AlleBewohner().Where(b => s.Dokumente(b.Id!.Value).Count > 0).ToList();
        Assert.Equal(12, mit.Count);
        Assert.All(mit, b => Assert.All(s.Dokumente(b.Id!.Value), d => Assert.True(File.Exists(Path.Combine(s.Ablage(b.Id!.Value), d.Datei)))));
        Assert.Contains(mit.SelectMany(b => s.Dokumente(b.Id!.Value)), d => d.Abgelaufen(Heute));
        Assert.Equal(kategorien, s.Dokumentkategorien());
    }
}

// Bauplan 32: vorhandene digitale Akte übernehmen.
[Collection("Datenordner")]
public sealed class AkteUebernahmeTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private readonly string ziel = Path.Combine(Path.GetTempPath(), $"dokuline-akte-{Guid.NewGuid():N}");

    public AkteUebernahmeTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
        try { Directory.Delete(ziel, recursive: true); } catch (IOException) { }
    }

    private static Bewohner B(long id, string vorname, string nachname) =>
        new() { Id = id, Vorname = vorname, Nachname = nachname, Zimmer = "1", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };

    [Fact]
    public void Zuordnung_nach_Name()
    {
        var liste = new[] { B(1, "Anton", "Ahorn"), B(2, "Berta", "Birke"), B(3, "Bernd", "Birke") };
        Assert.Equal(1, AkteUebernahme.Zuordnen("Ahorn, Anton", liste));
        Assert.Equal(1, AkteUebernahme.Zuordnen("AHORN_ANTON", liste));
        Assert.Equal(1, AkteUebernahme.Zuordnen("Ahorn", liste));         // einziger mit dem Nachnamen
        Assert.Null(AkteUebernahme.Zuordnen("Birke", liste));             // zwei mit dem Nachnamen: zum Bestätigen
        Assert.Equal(3, AkteUebernahme.Zuordnen("Birke Bernd", liste));
        Assert.Null(AkteUebernahme.Zuordnen("Muster, Max", liste));
        Assert.Null(AkteUebernahme.Zuordnen("Ahorn, Berta", liste));     // anderer Vorname: nicht Anton Ahorn
        Assert.Equal(1, AkteUebernahme.Zuordnen("Ahorn 2019", liste));
        // Nur Dokumente und Bilder; Programme, Skripte, Verknüpfungen und Office-Dateien mit Makros nicht.
        Assert.True(DokumentRegeln.Erlaubt("Bescheid.PDF"));
        Assert.True(DokumentRegeln.Erlaubt("Foto.jpeg"));
        foreach (string gefaehrlich in new[] { "start.exe", "skript.ps1", "verknuepfung.lnk", "makro.docm", "seite.html", "ohne_endung" })
            Assert.False(DokumentRegeln.Erlaubt(gefaehrlich));
        var kategorien = DokumentRegeln.Kategorienvorgabe;
        Assert.Equal("Krankenkasse", AkteUebernahme.KategorieFuerOrdner("krankenkasse", kategorien));
        Assert.Equal("Ärztliches und Befunde", AkteUebernahme.KategorieFuerOrdner("Arztbriefe", kategorien));
    }

    [Fact]
    public void Vorschau_Kopieren_Doppelte_und_Bericht()
    {
        var bewohner = s.AlleBewohner();
        string wurzel = AkteUebernahme.DemoOrdnerAnlegen(ziel, bewohner);
        var plan = AkteUebernahme.Planen(wurzel, bewohner, s.Dokumentkategorien());
        Assert.Equal(4, plan.Count);
        Assert.Equal(3, plan.Count(o => o.BewohnerId is not null));
        Assert.Contains(plan, o => o.Name.StartsWith("Muster") && o.BewohnerId is null);
        var erster = plan.First(o => o.BewohnerId is not null);
        Assert.Equal(["Aufnahme und Verträge", "Kostenträger und Bescheide", "Ärztliches und Befunde"],
            erster.Dateien.Select(d => d.Kategorie).Order(StringComparer.Ordinal));
        long id = erster.BewohnerId!.Value;
        int vorher = s.Dokumente(id).Count;
        var e = s.AkteUebernehmen(plan, "CD", Jetzt);
        Assert.Equal(9, e.Uebernommen);
        Assert.Single(e.Uebersprungen);
        Assert.Empty(e.Fehler);
        Assert.Equal(vorher + 3, s.Dokumente(id).Count);
        Assert.True(File.Exists(erster.Dateien[0].Pfad));   // der alte Ordner bleibt
        Assert.Contains(s.DokumentProtokoll(id), x => x.Grund == AkteUebernahme.Grund);
        // Ein zweites Mal: alles schon da.
        var zweimal = s.AkteUebernehmen(plan, "CD", Jetzt);
        Assert.Equal((0, 9), (zweimal.Uebernommen, zweimal.Doppelt));
        Assert.StartsWith("Übernommen: 0 · schon vorhanden (übersprungen): 9", AkteUebernahme.Bericht(zweimal));
        Assert.Throws<Eingabefehler>(() => s.AkteUebernehmen(plan, "OP", Jetzt));   // Therapie: kein Recht
    }
}
