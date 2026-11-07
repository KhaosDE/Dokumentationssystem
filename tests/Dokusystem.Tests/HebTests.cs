using System.Security.Cryptography;
using Dokusystem.Kern;
using Microsoft.Data.Sqlite;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;

namespace Dokusystem.Tests;

// HEB-Bogen: Felder und Grenzen, Zeitraum, Nummer, Planungszeitraum, Fortschreiben, Fassungen,
// „HEB fällig“, Stilregeln für Ziele, PDF zuteilen, zuordnen und ausfüllen (Test-PDF), Demo.
[Collection("Datenordner")]
public sealed class HebTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private static readonly DateOnly Heute = new(2026, 11, 7);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public HebTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Bewohner B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname);
    private static string TestPdf => Path.Combine(Repo.Wurzel, "tests", "daten", "heb-test.pdf");

    [Fact]
    public void Felder_und_Grenzen_je_Bogenart()
    {
        Assert.Equal(["1a", "1c", "1d"], HebRegeln.Felder(Bogenart.A, 1).Select(f => f.Schluessel));
        Assert.Equal([970, 795, 795], HebRegeln.Felder(Bogenart.A, 1).Select(f => f.Grenze!.Value));
        Assert.Equal(FeldArt.Rahmenziel, HebRegeln.Felder(Bogenart.A, 2).Single(f => f.Schluessel == "2c").Art);
        Assert.Equal(["5a", "5b", "5d", "5e"], HebRegeln.Felder(Bogenart.B, 5).Select(f => f.Schluessel));
        Assert.Equal(865, HebRegeln.Felder(Bogenart.B, 5).Single(f => f.Schluessel == "5b").Grenze);
        Assert.DoesNotContain(HebRegeln.Felder(Bogenart.C, 3), f => f.Art == FeldArt.Rahmenziel);   // HEB C ohne Rahmenziele
        Assert.Contains(HebRegeln.Zusatzfelder(Bogenart.C), f => f.Schluessel == "beendigung");
        Assert.DoesNotContain(HebRegeln.Zusatzfelder(Bogenart.C), f => f.Schluessel == "leitziel");
        Assert.Equal("Einschätzung des noch bestehenden Hilfebedarfs", HebRegeln.HilfebedarfTitel(Bogenart.C));
        Assert.Equal(("941 / 940", true), HebRegeln.Zaehler(new string('x', 941), 940));
        Assert.Equal(("12 Zeichen", false), HebRegeln.Zaehler(new string('x', 12), null));
    }

    [Fact]
    public void Zeitraum_Nummer_und_Planungszeitraum()
    {
        var ahorn = B("Ahorn");
        var boegen = s.HebBoegen(ahorn.Id!.Value);
        Assert.Equal((boegen[^1].Bis, Heute), HebRegeln.Zeitraum(Bogenart.B, ahorn, boegen, Heute));
        Assert.Equal((ahorn.Aufnahmedatum, Heute), HebRegeln.Zeitraum(Bogenart.A, ahorn, boegen, Heute));
        Assert.Equal(4, HebRegeln.Nummer(Bogenart.B, boegen));
        Assert.Null(HebRegeln.Nummer(Bogenart.C, boegen));
        Assert.Equal(7, HebRegeln.Planungsmonate(ahorn, Heute));   // Beschluss bis 30.06.2027
        Assert.Equal("Planungszeitraum: frei, 7 Monate", HebRegeln.PlanungText(7));
        Assert.Equal("Planungszeitraum: 12 Monate", HebRegeln.PlanungText(12));
        Assert.Null(HebRegeln.Planungsmonate(B("Birke"), Heute));   // kein Beschluss
    }

    [Fact]
    public void Neuer_Entwicklungsbericht_legt_Ziele_des_Vorberichts_vor()
    {
        var ahorn = B("Ahorn");
        var (von, bis) = HebRegeln.Zeitraum(Bogenart.B, ahorn, s.HebBoegen(ahorn.Id!.Value), Heute);
        var neu = s.HebAnlegen(ahorn.Id!.Value, Bogenart.B, von, bis, "CD");
        Assert.Equal(4, neu.Nummer);
        Assert.False(neu.Fertig);
        Assert.StartsWith("1. Hr. A. pflegt", neu.Aktuell!.Text("1d"));
        Assert.StartsWith("Ich, Hr. A.", neu.Aktuell.Text("leitziel"));
        Assert.Empty(neu.Aktuell.Hilfebedarf);   // nie vorbelegt
        Assert.Contains("Der Zeitraum beginnt vor der Aufnahme.",
            Assert.Throws<Eingabefehler>(() => s.HebAnlegen(ahorn.Id!.Value, Bogenart.A, ahorn.Aufnahmedatum.AddDays(-1), Heute, "CD")).Fehler);
    }

    [Fact]
    public void Fassungen_Status_Verlauf_und_Pruefregeln()
    {
        var b = B("Ginster");
        var h = s.HebAnlegen(b.Id!.Value, Bogenart.B, Heute.AddMonths(-10), Heute, "AB");
        Assert.Empty(h.Fassungen);   // kein Vorbericht mit Zielen
        var texte = new Dictionary<string, string> { ["1a"] = "Gespräche wöchentlich.", ["2b"] = "  " };
        var hilfe = new Dictionary<int, int?> { [1] = 3 };
        Assert.Contains("Hilfebedarf: nur Stufe 1 bis 6.",
            Assert.Throws<Eingabefehler>(() => HebRegeln.AusEingabe(h.Id, Bogenart.B, texte, new Dictionary<int, int?> { [1] = 7 }, false, "AB", Jetzt)).Fehler);
        Assert.Contains("Unbekanntes Feld.",
            Assert.Throws<Eingabefehler>(() => HebRegeln.AusEingabe(h.Id, Bogenart.B, new Dictionary<string, string> { ["1c"] = "x" }, hilfe, false, "AB", Jetzt)).Fehler);
        var f = HebRegeln.AusEingabe(h.Id, Bogenart.B, texte, hilfe, false, "AB", Jetzt);
        Assert.False(f.Texte.ContainsKey("2b"));   // leer fällt weg
        Assert.Equal(3, f.Hilfebedarf[1]);
        Assert.Null(f.Hilfebedarf[2]);
        s.HebSpeichern(f);
        s.HebSpeichern(f with { Kuerzel = "CD" });   // gleich: keine neue Fassung
        h = s.HebSpeichern(f with { Fertig = true, Kuerzel = "RS" });
        Assert.Equal(2, h.Fassungen.Count);
        var verlauf = HebRegeln.Verlauf(h);
        Assert.Contains("als fertig markiert von RS", verlauf[1]);
        Assert.Empty(HebRegeln.ZuLang(Bogenart.B, h.Aktuell!));
        Assert.Single(HebRegeln.ZuLang(Bogenart.B, h.Aktuell! with { Texte = new Dictionary<string, string> { ["1a"] = new string('x', 341) } }));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE heb_fassungen SET fertig = 0";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void HEB_faellig()
    {
        HebBogen Bogen(DateOnly bis, Bogenart art = Bogenart.B) => new(1, 1, art, bis.AddMonths(-12), bis, null, bis,
            [new HebFassung { Fertig = true }]);
        var ahorn = B("Ahorn");
        Assert.StartsWith("HEB fällig (letzter ", HebRegeln.Faellig(ahorn, s.HebBoegen(ahorn.Id!.Value), Heute, 12));
        Assert.Null(HebRegeln.Faellig(B("Birke"), s.HebBoegen(B("Birke").Id!.Value), Heute, 12));
        Assert.NotNull(HebRegeln.Faellig(B("Immergrün"), s.HebBoegen(B("Immergrün").Id!.Value), Heute, 12));
        var neu = ahorn with { Aufnahmedatum = Heute.AddMonths(-1) };
        Assert.Null(HebRegeln.Faellig(neu, [], Heute, 12));
        Assert.Equal("HEB fällig (kein HEB A)", HebRegeln.Faellig(neu with { Aufnahmedatum = Heute.AddMonths(-4) }, [], Heute, 12));
        Assert.Null(HebRegeln.Faellig(ahorn, [Bogen(Heute.AddMonths(-5))], Heute, 6));
        Assert.NotNull(HebRegeln.Faellig(ahorn, [Bogen(Heute.AddMonths(-7))], Heute, 6));
    }

    [Fact]
    public void Stilregeln_fuer_Ziele()
    {
        Assert.Contains(Stilpruefung.Pruefen("1. Ich lerne, mein Zimmer aufzuräumen.", FeldArt.Rahmenziel), h => h.Regel.StartsWith("Rahmenziel in der 3. Person"));
        Assert.Contains(Stilpruefung.Pruefen("Hr. A. geht spazieren.", FeldArt.IchZiel), h => h.Regel.StartsWith("Leitziel und Nahziele in Ich-Form"));
        Assert.Empty(Stilpruefung.Pruefen("Ich gehe jeden Nachmittag spazieren.", FeldArt.IchZiel));
        Assert.Empty(Stilpruefung.Pruefen("1. Hr. A. übt, sein Zimmer aufzuräumen.", FeldArt.Rahmenziel));
    }

    [Fact]
    public void PDF_zuteilen_zuordnen_und_ausfuellen()
    {
        var felder = HebPdf.Felder(TestPdf);
        Assert.Contains("1a", felder);
        Assert.Contains("Feld_Leitziel", felder);
        Assert.Null(HebPdf.Vorlage(Bogenart.B));
        HebPdf.VorlageZuteilen(Bogenart.B, TestPdf);
        Assert.NotNull(HebPdf.Vorlage(Bogenart.B));
        string pruefsummeVorher = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(HebPdf.Vorlage(Bogenart.B)!)));

        HebPdf.ZuordnungSpeichern(Bogenart.B, new Dictionary<string, string> { ["leitziel"] = "Feld_Leitziel", ["1b"] = "" });
        Assert.Equal(new Dictionary<string, string> { ["leitziel"] = "Feld_Leitziel" }, HebPdf.Zuordnung(Bogenart.B));
        Assert.Empty(HebPdf.Zuordnung(Bogenart.A));

        var kiefer = B("Kiefer");
        var bogen = s.HebBoegen(kiefer.Id!.Value).Single(x => !x.Fertig);
        var f = bogen.Aktuell! with { Texte = new Dictionary<string, string>(bogen.Aktuell!.Texte) { ["leitziel"] = "Ich, Hr. K., möchte Kontakt halten.", ["2a"] = "Text ohne PDF-Feld" } };
        var werte = HebPdf.Werte(bogen, kiefer, f);
        Assert.Equal("Karl Kiefer", werte["name"]);
        Assert.Equal("3", werte["nummer"]);
        var (pfad, ohne) = HebPdf.Ausfuellen(HebPdf.Vorlage(Bogenart.B)!, HebPdf.AusgabePfad(kiefer, bogen), werte, HebPdf.Zuordnung(Bogenart.B));
        Assert.StartsWith(Path.Combine(daten.Ordner, "heb"), pfad);
        Assert.Contains("2a", ohne);
        using (var doc = PdfReader.Open(pfad, PdfDocumentOpenMode.Import))
        {
            Assert.Equal("Karl Kiefer", ((PdfTextField)doc.AcroForm!.Fields["name"]!).Text);
            Assert.StartsWith("Gespräche mit dem Bezugsbetreuer", ((PdfTextField)doc.AcroForm.Fields["1a"]!).Text);
            Assert.Equal("Ich, Hr. K., möchte Kontakt halten.", ((PdfTextField)doc.AcroForm.Fields["Feld_Leitziel"]!).Text);
        }
        Assert.Equal(pruefsummeVorher, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(HebPdf.Vorlage(Bogenart.B)!))));   // Vorlage unverändert

        string keinPdf = Path.Combine(daten.Ordner, "kein.pdf");
        File.WriteAllText(keinPdf, "kein PDF");
        Assert.Throws<Eingabefehler>(() => HebPdf.VorlageZuteilen(Bogenart.A, keinPdf));
        HebPdf.VorlageEntfernen(Bogenart.B);
        Assert.Null(HebPdf.Vorlage(Bogenart.B));
    }

    [Fact]
    public void Demo_HEB_Boegen()
    {
        var kiefer = s.HebBoegen(B("Kiefer").Id!.Value);
        Assert.Equal(2, kiefer.Count);
        Assert.False(kiefer[^1].Fertig);
        Assert.Equal(kiefer[0].Bis, kiefer[1].Von);   // Zeitraum schließt an
        Assert.Equal(2, s.HebBoegen(B("Ginster").Id!.Value).Count);
        Assert.Equal(Bogenart.A, s.HebBoegen(B("Ginster").Id!.Value)[0].Art);
        int faellig = s.AlleBewohner().Count(b => HebRegeln.Faellig(b, s.HebBoegen(b.Id!.Value), Heute, 12) is not null);
        Assert.Equal(2, faellig);   // Ahorn und Immergrün
    }

    [Fact]
    public void Nach_dem_Update_kommt_die_Demo_HEB_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM heb_boegen; DELETE FROM meta WHERE schluessel = 'demo_heb'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.NotEmpty(s.HebBoegen(B("Ahorn").Id!.Value));
        Assert.False(s.DemoBeimErstenStart());
    }
}
