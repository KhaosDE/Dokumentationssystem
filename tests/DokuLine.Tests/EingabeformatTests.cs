using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26d: Datum und Uhrzeit mit automatischen Punkten – gemeinsamer Baustein.
public class EingabeformatTests
{
    // Tippt man Zeichen für Zeichen, entsteht dasselbe wie im Feld (Punkt vor der nächsten Ziffer).
    private static string Tippen(string zeichen, Func<string, string?> vorZiffer)
    {
        string text = "";
        foreach (char c in zeichen)
        {
            if (char.IsAsciiDigit(c) && vorZiffer(text) is string neu) text = neu;
            text += c;
        }
        return text;
    }

    [Theory]
    [InlineData("08112026", "08.11.2026", "08.11.2026")]
    [InlineData("081126", "08.11.26", "08.11.2026")]
    [InlineData("08.11.2026", "08.11.2026", "08.11.2026")]   // eigene Punkte stören nicht
    [InlineData("8.11.2026", "8.11.2026", "8.11.2026")]
    [InlineData("08.112026", "08.11.2026", "08.11.2026")]
    [InlineData("3.4.65", "3.4.65", "3.4.2065")]             // zweistellige Jahre als 20xx (laut Auftrag)
    [InlineData("99", "99", "99")]                            // kein gültiger Tag: kein Punkt
    public void Datum_beim_Tippen_und_fertig(string getippt, string beimTippen, string fertig)
    {
        string text = Tippen(getippt, Eingabeformat.DatumVorZiffer);
        Assert.Equal(beimTippen, text);
        Assert.Equal(fertig, Eingabeformat.DatumFertig(text));
    }

    [Theory]
    [InlineData("0930", "09:30", "09:30")]
    [InlineData("09:30", "09:30", "09:30")]
    [InlineData("930", "930", "09:30")]                       // 93 ist keine Stunde: kein Doppelpunkt
    [InlineData("1415", "14:15", "14:15")]
    public void Uhrzeit_beim_Tippen_und_fertig(string getippt, string beimTippen, string fertig)
    {
        string text = Tippen(getippt, Eingabeformat.UhrzeitVorZiffer);
        Assert.Equal(beimTippen, text);
        Assert.Equal(fertig, Eingabeformat.UhrzeitFertig(text));
    }

    [Fact]
    public void Lesen_versteht_Ziffern_ohne_Punkte()
    {
        Assert.Equal(new DateOnly(2026, 11, 8), Datum.Lesen("08112026", "Datum"));
        Assert.Equal(new DateOnly(2026, 11, 8), Datum.Lesen("081126", "Datum"));
        Assert.Equal(new DateOnly(2026, 11, 8), Datum.Lesen("08.11.26", "Datum"));
        Assert.Equal(new TimeOnly(9, 30), BerichtRegeln.UhrzeitLesen("0930"));
        Assert.Equal(new TimeOnly(9, 30), BerichtRegeln.UhrzeitLesen("930"));
        Assert.Throws<FormatException>(() => Datum.Lesen("0811", "Datum"));
        Assert.Throws<FormatException>(() => BerichtRegeln.UhrzeitLesen("2575"));
    }
}
