using System.Text;
using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 33: Überleitungsbogen – Zusammenstellen, „nicht erfasst“ statt leer, nur angehakte Abschnitte, PDF, Kopie.
[Collection("Datenordner")]
public sealed class UeberleitungTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public UeberleitungTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static UeberleitungEingabe E(params string[] abschnitte) => new()
    {
        Anlass = "Einweisung nach Sturz", Zeitpunkt = Jetzt, Begleitung = "EF",
        Abschnitte = abschnitte.Length > 0 ? abschnitte.ToHashSet() : UeberleitungRegeln.AlleAbschnitte.ToHashSet(),
    };

    [Fact]
    public void Inhalt_mit_Forensik_und_nicht_erfasst()
    {
        var b = s.AlleBewohner().First(x => ZustaendigkeitRegeln.InForensik(s.Angaben(x.Id!.Value, Heute).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>()));
        var inhalt = UeberleitungRegeln.Zusammenstellen(s.UeberleitungDaten(b.Id!.Value, Jetzt), E());
        Assert.Equal(["Überleitung", .. UeberleitungRegeln.AlleAbschnitte], inhalt.Select(a => a.Titel));
        Assert.Contains("Anlass: Einweisung nach Sturz", inhalt[0].Zeilen);
        Assert.Contains(inhalt[0].Zeilen, z => z.Contains("Tel. Dienstzimmer 0000 555 000"));   // ausgedachte Vorgabe
        var betreuung = inhalt.Single(a => a.Titel == UeberleitungRegeln.Betreuung).Zeilen;
        Assert.Contains(betreuung, z => z.StartsWith("Zuständig (Forensik"));
        Assert.Contains(betreuung, z => z.Contains(ZustaendigkeitRegeln.BetreuerNichtZustaendig) || z.Contains(AngabenRegeln.NichtErfasst) || z.StartsWith("keine"));
        Assert.Equal([AngabenRegeln.NichtErfasst], inhalt.Single(a => a.Titel == UeberleitungRegeln.Vitalwerte).Zeilen);   // kommt mit Schritt 36
        Assert.All(inhalt, a => Assert.NotEmpty(a.Zeilen));   // nie leer
        // Nur Angehaktes.
        var wenig = UeberleitungRegeln.Zusammenstellen(s.UeberleitungDaten(b.Id!.Value, Jetzt), E(UeberleitungRegeln.Person, UeberleitungRegeln.Allergien));
        Assert.Equal(["Überleitung", UeberleitungRegeln.Person, UeberleitungRegeln.Allergien], wenig.Select(a => a.Titel));
        Assert.Contains("Allergien", UeberleitungRegeln.Text(wenig));
    }

    [Fact]
    public void PDF_mehrere_Seiten_Kopie_Bericht_und_Protokoll()
    {
        var b = s.AlleBewohner()[0];
        var viel = new List<UeberleitungAbschnitt> { new("Lang", [.. Enumerable.Range(1, 120).Select(i => $"Zeile {i} mit „Anführung“ – und (Klammer)")]) };
        string text = Encoding.Latin1.GetString(TextPdf.Erzeugen("Überleitungsbogen Test", viel, "Demo — ausgedachte Daten"));
        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/Count 3", text);   // 120 Zeilen: drei Seiten
        Assert.Contains("\\(Klammer\\)", text);
        var pdf = TextPdf.Erzeugen("Überleitungsbogen", UeberleitungRegeln.Zusammenstellen(s.UeberleitungDaten(b.Id!.Value, Jetzt), E()), "Demo — ausgedachte Daten");
        var d = s.UeberleitungAblegen(b.Id!.Value, "Einweisung nach Sturz", pdf, "EF", Jetzt);
        Assert.Equal(Speicher.UeberleitungKategorie, d.Kategorie);
        Assert.Equal("Überleitungsbogen 09.11.2026", d.Titel);
        Assert.Single(s.Ueberleitungen(b.Id!.Value));
        var bericht = s.UeberleitungBericht(b.Id!.Value, "Einweisung nach Sturz", "EF", Jetzt);
        Assert.True(bericht.Wichtig);
        Assert.Contains("Überleitungsbogen erstellt: Einweisung nach Sturz", bericht.Text);
        Assert.StartsWith("in der Klinik seit", s.UeberleitungStatus(b.Id!.Value, "EF", "R1", Jetzt).Text);
    }

    [Fact]
    public void Einrichtung_im_Datenordner()
    {
        Assert.EndsWith("(ausgedacht)", Einrichtung.Laden().Name);
        Einrichtung.Speichern(new EinrichtungWerte("Haus Beispiel (ausgedacht)", "Weg 2 (ausgedacht)", "0000 1"));
        Assert.Equal(new EinrichtungWerte("Haus Beispiel (ausgedacht)", "Weg 2 (ausgedacht)", "0000 1"), Einrichtung.Laden());
    }
}
