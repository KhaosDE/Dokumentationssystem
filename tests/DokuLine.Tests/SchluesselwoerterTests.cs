using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 48: Schlüsselwörter – je Regel Beispiele und Gegenbeispiele, Wortanfang/ganzes Wort/Wortgruppe, höchstens zwei
// Angebote, weggeklickt bleibt weg, Datum und Uhrzeit aus dem Text, Liste in der Datenbank mit Recht, Verweis am Bericht.
[Collection("Datenordner")]
public sealed class SchluesselwoerterTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private static readonly HashSet<Folgeschritt> Keine = [];

    public SchluesselwoerterTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static List<Schluesselwort> Werk() =>
        [.. SchluesselwortRegeln.Mitgeliefert.Select((m, i) => new Schluesselwort(i, m.Wort, m.Folgeschritt, m.Art, true, true))];

    private static Folgeschritt? Erstes(string text) => SchluesselwortRegeln.Finden(text, Werk(), Keine).FirstOrDefault()?.Folgeschritt;

    [Theory]
    [InlineData("Morgen Arzttermin beim Hausarzt.", Folgeschritt.Termin)]
    [InlineData("Termin bei der Zahnärztin vereinbart.", Folgeschritt.Termin)]
    [InlineData("Überweisung zum Neurologen erhalten.", Folgeschritt.Termin)]
    [InlineData("Blutabnahme heute früh.", Folgeschritt.Aufgabe)]
    [InlineData("Laborwerte kommen nächste Woche.", Folgeschritt.Aufgabe)]
    [InlineData("Ergebnis nachfragen beim Arzt.", Folgeschritt.Aufgabe)]
    [InlineData("Das Medikament geändert, siehe Plan.", Folgeschritt.Medikamentenplan)]
    [InlineData("Tablette abgesetzt.", Folgeschritt.Medikamentenplan)]
    [InlineData("Bedarf gegeben gegen 20 Uhr.", Folgeschritt.Bedarf)]
    [InlineData("Blutdruck 130/80 gemessen.", Folgeschritt.Vitalwerte)]
    [InlineData("Im Flur gestürzt, keine Verletzung sichtbar.", Folgeschritt.Vorkommnis)]
    [InlineData("Ist nicht zurück vom Ausgang.", Folgeschritt.Abgaengigkeit)]
    [InlineData("Notarzt gerufen.", Folgeschritt.Ueberleitung)]
    [InlineData("Rückruf vom Betreuer abwarten.", Folgeschritt.Wiedervorlage)]
    [InlineData("Angehörige haben angerufen.", Folgeschritt.Schweigepflicht)]
    [InlineData("Hilfeplangespräch steht an.", Folgeschritt.Hilfeplan)]
    [InlineData("Nahziel heute geschafft.", Folgeschritt.Ziele)]
    [InlineData("Kein Arzttermin nötig.", Folgeschritt.Termin)]   // trifft trotzdem – nur ein Angebot, hingenommen
    public void Regel_trifft(string text, Folgeschritt erwartet) => Assert.Equal(erwartet, Erstes(text));

    [Theory]
    [InlineData("Hat den Tisch gedeckt und gelacht.")]
    [InlineData("Freundlich und ruhig.")]
    [InlineData("Unterstürzt etwas")]   // „Sturz“ nur am Wortanfang
    public void Gegenbeispiel_trifft_nicht(string text) => Assert.Null(Erstes(text));

    [Fact]
    public void Arten_hoechstens_zwei_weggeklickt_und_Datum()
    {
        Assert.True(SchluesselwortRegeln.Trifft("Blutabnahmen morgen", "Blutabnahme", Treffer.Wortanfang));
        Assert.False(SchluesselwortRegeln.Trifft("Blutabnahmen morgen", "Blutabnahme", Treffer.Wort));
        Assert.True(SchluesselwortRegeln.Trifft("Ergebnis   NACHFRAGEN", "Ergebnis nachfragen", Treffer.Wortgruppe));
        var drei = SchluesselwortRegeln.Finden("Arzttermin, Blutabnahme und Blutdruck.", Werk(), Keine);
        Assert.Equal([Folgeschritt.Termin, Folgeschritt.Aufgabe], drei.Select(v => v.Folgeschritt));
        var ohne = SchluesselwortRegeln.Finden("Arzttermin, Blutabnahme und Blutdruck.", Werk(), new HashSet<Folgeschritt> { Folgeschritt.Termin });
        Assert.Equal([Folgeschritt.Aufgabe, Folgeschritt.Vitalwerte], ohne.Select(v => v.Folgeschritt));
        var abgeschaltet = Werk().Select(w => w.Wort == "Arzttermin" ? w with { Aktiv = false } : w);
        Assert.Empty(SchluesselwortRegeln.Finden("Arzttermin", abgeschaltet, Keine));

        Assert.Equal((new DateOnly(2026, 11, 14), new TimeOnly(10, 30)), SchluesselwortRegeln.DatumImText("Arzttermin am 14.11. um 10:30", 2026));
        Assert.Equal((new DateOnly(2027, 1, 3), new TimeOnly(9, 0)), SchluesselwortRegeln.DatumImText("am 3.1.2027 um 9 Uhr"));
        Assert.Equal(((DateOnly?)null, (TimeOnly?)null), SchluesselwortRegeln.DatumImText("Blutdruck 130/80"));
        var v = SchluesselwortRegeln.Finden("Arzttermin am 14.11.2026 um 10:30", Werk(), Keine).Single();
        Assert.Equal(("„Arzttermin“ erkannt — Termin im Kalender?", new DateOnly(2026, 11, 14), new TimeOnly(10, 30)), (v.Text, v.Datum, v.Uhrzeit));
    }

    [Fact]
    public void Liste_in_der_Datenbank_Rechte_und_Verweis()
    {
        var liste = s.Schluesselwoerter();
        Assert.Equal(SchluesselwortRegeln.Mitgeliefert.Length, liste.Count);
        Assert.Throws<Eingabefehler>(() => s.WortHinzufuegen("Zahnarzt", Folgeschritt.Termin, Treffer.Wortanfang, "AB"));   // ohne Recht
        var neu = s.WortHinzufuegen("Zahnarzt", Folgeschritt.Termin, Treffer.Wortanfang, "EF");
        Assert.Throws<Eingabefehler>(() => s.WortHinzufuegen("zahnarzt", Folgeschritt.Termin, Treffer.Wort, "EF"));   // doppelt
        Assert.Throws<Eingabefehler>(() => s.WortHinzufuegen("ab", Folgeschritt.Termin, Treffer.Wort, "EF"));
        var arzt = liste.First(w => w.Wort == "Arzttermin");
        s.WortEntfernen(arzt.Id, "EF");   // mitgeliefert: nur aus
        Assert.False(s.Schluesselwoerter().Single(w => w.Id == arzt.Id).Aktiv);
        s.WortEntfernen(neu.Id, "EF");    // eigenes: weg
        Assert.DoesNotContain(s.Schluesselwoerter(), w => w.Id == neu.Id);
        s.WortAendern(arzt.Id, true, Folgeschritt.Aufgabe, Treffer.Wort, "EF");
        Assert.Equal((true, Folgeschritt.Aufgabe, Treffer.Wort), (s.Schluesselwoerter().Single(w => w.Id == arzt.Id) is var w1 ? (w1.Aktiv, w1.Folgeschritt, w1.Art) : default));
        s.Werkseinstellung(Folgeschritt.Aufgabe, "EF");
        Assert.DoesNotContain(s.Schluesselwoerter(), w => w.Wort == "Arzttermin" && w.Folgeschritt == Folgeschritt.Aufgabe);
        s.Werkseinstellung(Folgeschritt.Termin, "EF");
        Assert.Contains(s.Schluesselwoerter(), w => w.Wort == "Arzttermin" && w.Folgeschritt == Folgeschritt.Termin && w.Aktiv);

        Assert.False(s.VorschlaegeAus("AB"));
        s.VorschlaegeAusSetzen("ab", true);
        Assert.True(s.VorschlaegeAus("AB"));
        s.VorschlaegeAusSetzen("AB", false);
        Assert.False(s.VorschlaegeAus("AB"));

        var b = s.AlleBewohner().First();
        var bericht = s.Berichte(b.Id!.Value).First();
        s.VerweisEintragen(bericht.Id!.Value, SchluesselwortRegeln.VerweisText(Folgeschritt.Termin, "14.11. 10:30"));
        s.VerweisEintragen(bericht.Id!.Value, SchluesselwortRegeln.VerweisText(Folgeschritt.Aufgabe, "Labor nachfragen"));
        Assert.Equal("→ Termin 14.11. 10:30 · → Aufgabe Labor nachfragen", s.Berichte(b.Id!.Value).Single(x => x.Id == bericht.Id).Verweis);
    }
}
