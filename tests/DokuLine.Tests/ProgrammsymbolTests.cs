namespace DokuLine.Tests;

// Bauplan Schritt 26n: Programmsymbol aus zwei SVG-Vorlagen (selbst gezeichnet), umgewandelt mit
// werkzeuge/symbol-erzeugen.py. Die .ico enthält 16–256 px; die kleinen aus der kleinen Vorlage.
public sealed class ProgrammsymbolTests
{
    private static string Bilder => Path.Combine(Repo.Wurzel, "src", "DokuLine", "Bilder");

    [Fact]
    public void Symboldatei_mit_allen_Groessen()
    {
        byte[] ico = File.ReadAllBytes(Path.Combine(Bilder, "DokuLine.ico"));
        Assert.Equal(0, BitConverter.ToUInt16(ico, 0));
        Assert.Equal(1, BitConverter.ToUInt16(ico, 2));   // Symbol, kein Mauszeiger
        int anzahl = BitConverter.ToUInt16(ico, 4);
        var groessen = new List<int>();
        for (int i = 0; i < anzahl; i++)
        {
            int e = 6 + 16 * i;
            int groesse = ico[e] == 0 ? 256 : ico[e];
            groessen.Add(groesse);
            Assert.Equal(32, BitConverter.ToUInt16(ico, e + 6));   // 32 Bit mit Alpha
            int laenge = BitConverter.ToInt32(ico, e + 8), versatz = BitConverter.ToInt32(ico, e + 12);
            Assert.True(versatz + laenge <= ico.Length);
            if (groesse == 256) Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], ico[versatz..(versatz + 4)]);
            else Assert.Equal(40, BitConverter.ToInt32(ico, versatz));   // klassisches Symbolbild
        }
        Assert.Equal([16, 20, 24, 32, 40, 48, 64, 256], groessen);
    }

    [Fact]
    public void Vorlagen_liegen_im_Repository_ohne_fremde_Bilder()
    {
        foreach (var name in new[] { "programmsymbol.svg", "programmsymbol-klein.svg" })
        {
            string svg = File.ReadAllText(Path.Combine(Bilder, name));
            Assert.StartsWith("<svg ", svg);
            Assert.DoesNotContain("<image", svg);   // nur gezeichnete Formen, kein eingebettetes Bild
            Assert.DoesNotContain("href", svg);
        }
        Assert.DoesNotContain("radialGradient", File.ReadAllText(Path.Combine(Bilder, "programmsymbol-klein.svg")));   // klein ohne Verlauf
        Assert.True(File.Exists(Path.Combine(Repo.Wurzel, "werkzeuge", "symbol-erzeugen.py")));
    }
}
