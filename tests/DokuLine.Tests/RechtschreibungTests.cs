using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 49: Rechtschreibung – mit einem ausgedachten Prüfer (kennt nur eine kleine Wortliste): Fachwörter, Bewohnernamen,
// Kürzel und Medikamente werden nicht angestrichen, echte Tippfehler schon; Fachwörterbuch mit Recht; Autokorrektur.
[Collection("Datenordner")]
public sealed class RechtschreibungTests : IDisposable
{
    private readonly Datenordner daten = new();
    private readonly Speicher s = new() { Uhr = () => new DateTime(2026, 11, 9, 9, 0, 0) };

    public RechtschreibungTests() => s.DemoZuruecksetzen();

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    // Meldet jedes Wort, das nicht in seiner kleinen Liste steht – wie ein Prüfer ohne Fachwortschatz.
    private sealed class Testpruefer(params string[] bekannt) : IRechtschreibung
    {
        private readonly HashSet<string> woerter = new(bekannt, StringComparer.CurrentCultureIgnoreCase);

        public IReadOnlyList<(int Start, int Laenge)> Fehler(string text) =>
            [.. System.Text.RegularExpressions.Regex.Matches(text, @"[\p{L}][\p{L}\-']*").Where(m => !woerter.Contains(m.Value)).Select(m => (m.Index, m.Length))];

        public IReadOnlyList<string> Vorschlaege(string wort) => [];
        public void Ignorieren(string wort) => woerter.Add(wort);
    }

    [Fact]
    public void Eigene_Woerter_gelten_als_richtig_Tippfehler_nicht()
    {
        var b = s.AlleBewohner().First();
        var medikament = s.Medikamente().First().Aktuell.Wirkstoff;
        string text = $"Herr {b.Nachname} hat die Bedarfsmedikation {medikament} genommen, mit EF und der Bezugspflege besprochen. Er wra ruhig und hatt geschlafen.";
        var pruefer = new Testpruefer("Herr", "hat", "die", "genommen", "mit", "und", "der", "besprochen", "Er", "ruhig", "geschlafen");
        var fehler = RechtschreibRegeln.Filtern(text, pruefer.Fehler(text), s.EigeneWoerter(), new HashSet<string>());
        Assert.Equal(["wra", "hatt"], fehler.Select(f => f.Wort));
        Assert.Equal(text.IndexOf("wra", StringComparison.Ordinal), fehler[0].Start);
        Assert.Equal("2 Wörter unbekannt", RechtschreibRegeln.HinweisText(fehler.Count));
        Assert.Null(RechtschreibRegeln.HinweisText(0));
        // Ignoriert (nur diese Sitzung) und Wörter mit Ziffern, kurze Großbuchstaben-Kürzel, Bindestrich aus eigenen Teilen.
        var ohne = RechtschreibRegeln.Filtern("wra 3x FOSAB Esche-Linde", [(0, 3), (4, 2), (7, 5), (13, 11)], RechtschreibRegeln.EigeneWoerter(["Dr. Esche", "Linde"]),
            new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { "WRA" });
        Assert.Empty(ohne);
    }

    [Fact]
    public void Fachwoerterbuch_Recht_und_Autokorrektur()
    {
        var liste = s.Fachwoerter();
        Assert.Contains("Exazerbation", liste);
        Assert.Throws<Eingabefehler>(() => s.FachwortHinzufuegen("Remission", "AB"));   // ohne Recht
        s.FachwortHinzufuegen("Remission", "EF");
        Assert.Throws<Eingabefehler>(() => s.FachwortHinzufuegen("remission", "EF"));    // gibt es schon
        Assert.Throws<Eingabefehler>(() => s.FachwortHinzufuegen("zwei Wörter", "EF"));
        Assert.Contains("Remission", s.EigeneWoerter());
        s.FachwortEntfernen("Remission", "EF");
        Assert.DoesNotContain("Remission", s.Fachwoerter());

        Assert.Equal("und", RechtschreibRegeln.Korrektur("udn"));
        Assert.Null(RechtschreibRegeln.Korrektur("und"));
        Assert.False(s.AutokorrekturAn("AB"));
        s.AutokorrekturSetzen("ab", true);
        Assert.True(s.AutokorrekturAn("AB"));
    }
}
