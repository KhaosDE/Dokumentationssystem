using Dokusystem.Kern;

namespace Dokusystem.Tests;

// Bewohner: Datumsformat, Prüfregeln, drei Zustände bei Allergien und Beschluss.
public class BewohnerTests
{
    private static BewohnerEingabe Eingabe(Action<BewohnerEingabe>? aendern = null)
    {
        var e = new BewohnerEingabe
        {
            Vorname = "Anton", Nachname = "Ahorn", Zimmer = "101",
            Geburtsdatum = "14.03.1962", Aufnahmedatum = "01.09.2019",
        };
        aendern?.Invoke(e);
        return e;
    }

    private static IReadOnlyList<string> FehlerVon(BewohnerEingabe e) =>
        Assert.Throws<Eingabefehler>(() => BewohnerRegeln.AusEingabe(e)).Fehler;

    // ----- Datum -----

    [Fact]
    public void Datum_im_Format_TT_MM_JJJJ()
    {
        Assert.Equal(new DateOnly(1965, 4, 3), Datum.Lesen("03.04.1965", "Geburtsdatum"));
        Assert.Equal(new DateOnly(1965, 4, 3), Datum.Lesen(" 3.4.1965 ", "Geburtsdatum"));
        Assert.Equal("03.04.1965", Datum.Text(new DateOnly(1965, 4, 3)));
        Assert.Equal("", Datum.Text(null));
    }

    [Theory]
    [InlineData("1965-04-03")]
    [InlineData("03.04.65")]
    [InlineData("03/04/1965")]
    [InlineData("irgendwann")]
    public void Falsches_Format(string text)
    {
        var fehler = Assert.Throws<FormatException>(() => Datum.Lesen(text, "Geburtsdatum"));
        Assert.Contains("TT.MM.JJJJ", fehler.Message);
    }

    [Fact]
    public void Datum_das_es_nicht_gibt_und_leeres_Datum()
    {
        Assert.Contains("gibt es nicht", Assert.Throws<FormatException>(() => Datum.Lesen("31.02.1965", "Geburtsdatum")).Message);
        Assert.Equal("Geburtsdatum fehlt.", Assert.Throws<FormatException>(() => Datum.Lesen("", "Geburtsdatum")).Message);
    }

    // ----- Prüfregeln -----

    [Fact]
    public void Gueltige_Eingabe()
    {
        var b = BewohnerRegeln.AusEingabe(Eingabe(), id: 7, bild: "a.png");
        Assert.Equal(("Anton Ahorn", "101", 7L, "a.png"), (b.Name, b.Zimmer, b.Id!.Value, b.Bild));
        Assert.Equal(new DateOnly(1962, 3, 14), b.Geburtsdatum);
    }

    [Fact]
    public void Pflichtfelder_in_Formular_Reihenfolge()
    {
        var fehler = FehlerVon(Eingabe(e =>
        {
            e.Vorname = " "; e.Nachname = ""; e.Zimmer = ""; e.Geburtsdatum = ""; e.Aufnahmedatum = "";
        }));
        Assert.Equal(["Vorname fehlt.", "Nachname fehlt.", "Zimmernummer fehlt.", "Geburtsdatum fehlt.", "Aufnahmedatum fehlt."], fehler);
    }

    [Fact]
    public void Aufnahme_nicht_vor_Geburt()
    {
        Assert.Equal(["Das Aufnahmedatum liegt vor dem Geburtsdatum."],
            FehlerVon(Eingabe(e => { e.Geburtsdatum = "14.03.1962"; e.Aufnahmedatum = "13.03.1962"; })));
        BewohnerRegeln.AusEingabe(Eingabe(e => { e.Geburtsdatum = "14.03.1962"; e.Aufnahmedatum = "14.03.1962"; }));   // gleicher Tag geht
    }

    [Fact]
    public void Beschluss()
    {
        var b = BewohnerRegeln.AusEingabe(Eingabe(e => { e.Beschluss = "ende"; e.Beschlussende = "30.06.2027"; }));
        Assert.Equal("30.06.2027", BewohnerRegeln.BeschlussText(b));
        Assert.Equal(["Beschlussende fehlt."], FehlerVon(Eingabe(e => { e.Beschluss = "ende"; e.Beschlussende = ""; })));
        Assert.Single(FehlerVon(Eingabe(e => { e.Beschluss = "ende"; e.Beschlussende = "30.6.27"; })));
        b = BewohnerRegeln.AusEingabe(Eingabe(e => { e.Beschluss = "kein_beschluss"; e.Beschlussende = "30.06.2027"; }));
        Assert.Null(b.Beschlussende);   // Datum ohne Beschluss wird verworfen
        Assert.Equal("kein Beschluss", BewohnerRegeln.BeschlussText(b));
        Assert.Equal(["Beschluss: unbekannte Angabe."], FehlerVon(Eingabe(e => e.Beschluss = "vielleicht")));
    }

    // ----- Regel 5: Leere Felder sind nicht „nichts“ -----

    [Fact]
    public void Ohne_Angabe_gilt_nicht_erfasst()
    {
        var b = BewohnerRegeln.AusEingabe(Eingabe());
        Assert.Equal(AllergieZustand.NichtErfasst, b.Allergie);
        Assert.Equal(BeschlussZustand.NichtErfasst, b.Beschluss);
        Assert.Equal("Allergien: nicht erfasst – bitte klären", BewohnerRegeln.AllergieText(b));
        Assert.Equal("nicht erfasst", BewohnerRegeln.BeschlussText(b));
        Assert.Equal(AllergieZustand.NichtErfasst, default(AllergieZustand));
        Assert.Equal(BeschlussZustand.NichtErfasst, default(BeschlussZustand));
    }

    [Fact]
    public void Drei_verschiedene_Anzeigen()
    {
        var anzeigen = new HashSet<string>
        {
            BewohnerRegeln.AllergieText(BewohnerRegeln.AusEingabe(Eingabe(e => { e.Allergie = "eingetragen"; e.Allergien = "Haselnüsse"; }))),
            BewohnerRegeln.AllergieText(BewohnerRegeln.AusEingabe(Eingabe(e => e.Allergie = "keine_bekannt"))),
            BewohnerRegeln.AllergieText(BewohnerRegeln.AusEingabe(Eingabe(e => e.Allergie = "nicht_erfasst"))),
        };
        Assert.Equal(["Allergien: Haselnüsse", "Allergien: keine bekannt", "Allergien: nicht erfasst – bitte klären"], anzeigen.Order());
    }

    [Fact]
    public void Eingetragen_ohne_Text_geht_nicht()
    {
        var fehler = FehlerVon(Eingabe(e => { e.Allergie = "eingetragen"; e.Allergien = "  "; }));
        Assert.Single(fehler);
        Assert.Contains("bitte eintragen", fehler[0]);
    }

    [Fact]
    public void Text_bei_keine_bekannt_wird_verworfen()
    {
        var b = BewohnerRegeln.AusEingabe(Eingabe(e => { e.Allergie = "keine_bekannt"; e.Allergien = "Haselnüsse"; }));
        Assert.Equal("", b.Allergien);
        b = b with { Allergien = "Haselnüsse" };   // direkt gesetzt (z. B. Programmfehler): Prüfung schlägt an
        Assert.Equal(["Allergien: Text nur bei eingetragenen Allergien."], BewohnerRegeln.Pruefen(b));
    }

    [Fact]
    public void Hin_und_zurueck_ergibt_denselben_Bewohner()
    {
        var b = BewohnerRegeln.AusEingabe(Eingabe(e =>
        {
            e.Allergie = "eingetragen"; e.Allergien = "Erdbeeren"; e.Beschluss = "ende"; e.Beschlussende = "1.2.2027";
        }));
        Assert.Equal(b, BewohnerRegeln.AusEingabe(BewohnerRegeln.AlsEingabe(b)));
        Assert.Equal("01.02.2027", BewohnerRegeln.AlsEingabe(b).Beschlussende);
    }

    [Fact]
    public void Initialen_und_Sortierung()
    {
        var a = BewohnerRegeln.AusEingabe(Eingabe(e => { e.Vorname = "emil"; e.Nachname = "Eiche"; }));
        var b = BewohnerRegeln.AusEingabe(Eingabe(e => { e.Vorname = "Anton"; e.Nachname = "ahorn"; }));
        Assert.Equal("EE", BewohnerRegeln.Initialen(a));
        Assert.Equal("?", BewohnerRegeln.Initialen(" ", ""));
        var liste = new List<Bewohner> { a, b };
        liste.Sort(BewohnerRegeln.NachName);
        Assert.Equal([b, a], liste);
    }
}
