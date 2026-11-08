using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Bauplan Schritt 26a: Das Kürzel kommt aus der Anmeldung. „Eingetragen von“ ist nicht änderbar;
// Personen-Felder (Fahrer, Teilnehmer, Begleitung) sind eine Auswahl, vorbelegt mit der
// angemeldeten Person. Ohne Anmeldung nur ansehen.
[Collection("Oberflaeche")]
public class KuerzelOberflaechenTests
{
    [Fact]
    public void Eingetragen_von_aus_der_Anmeldung_und_Personen_als_Auswahl() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using (var bericht = new BerichtFormular(u.Speicher, u.Demo("Ahorn")))
        {
            Assert.IsType<Label>(bericht.Verfasser);   // kein Eingabefeld mehr
            Assert.Equal("CD", bericht.Verfasser.Text);
            Assert.Equal("CD", bericht.Eingabe().Verfasser);
        }
        using (var aufgabe = new AufgabeFenster(u.Speicher, null, null))
            Assert.Equal("CD", aufgabe.Eingabe().Verfasser);
        using (var fahrt = new FahrtFenster(u.Speicher))
        {
            Assert.Equal(ComboBoxStyle.DropDownList, fahrt.Fahrer.DropDownStyle);   // kein Freitext
            Assert.Equal("CD", fahrt.Fahrer.Kuerzel);
            fahrt.Fahrer.Kuerzel = "KL";
            Assert.Equal("KL", fahrt.Fahrer.Kuerzel);
        }
        using (var visite = new VisiteFenster(u.Speicher))
        {
            Assert.Equal("CD", visite.Teilnehmer.AlsText);
            visite.Teilnehmer.Setzen(["CD", "EF"]);
            Assert.Equal(["CD", "EF"], visite.Teilnehmer.Kuerzel);
        }
        using (var termin = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today), null))
        {
            termin.Begleitung.Setzen(["AB", "Frau Beispiel (früher Freitext)"]);
            Assert.Equal("AB, Frau Beispiel (früher Freitext)", termin.Begleitung.AlsText);   // Altes bleibt erhalten
        }
    });

    [Fact]
    public void Ohne_Anmeldung_nur_ansehen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Aufgaben");
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(f.Aufgaben.NeuKnopf));
        f.BereichWaehlen("Übergabe");
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(f.Uebergabe.NotizKnopf));
        f.BereichWaehlen("Kalender");
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(f.Kalender.NeuKnopf));
        f.BenutzerWaehlen("CD");
        Assert.Null(Stil.Sperrgrund(f.Kalender.NeuKnopf));
        using var bericht = new BerichtFormular(u.Speicher, u.Demo("Ahorn"));
        Assert.Equal("CD", bericht.Verfasser.Text);
    });
}
