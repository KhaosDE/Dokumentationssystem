using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bewohnerakte, Ergänzungen (Bauplan Schritt 16): „offen geführt“ mit drei Zuständen im Formular,
// in der Akte und als Marke; Widerspruch zum Modul Ausgangsliste als Hinweis; Alter.
[Collection("Oberflaeche")]
public class AkteErgaenzungenTests
{
    [Fact]
    public void Offen_gefuehrt_und_Alter_in_der_Akte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var akte = f.Bewohner.Akte;

        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        Assert.Equal("ja", akte.OffenGefuehrt.Text);
        Assert.Contains("offen geführt", akte.KopfMarken);
        int alter = BewohnerRegeln.Alter(u.Demo("Birke").Geburtsdatum, DateOnly.FromDateTime(DateTime.Today));
        Assert.Equal($"({alter} Jahre)", akte.Alter.Text);

        f.Bewohner.AkteZeigen(u.Demo("Distel").Id!.Value);
        Assert.Equal("nicht erfasst", akte.OffenGefuehrt.Text);
        Assert.Equal(Stil.FettKursiv, akte.OffenGefuehrt.Font);
        Assert.Contains("offen geführt: nicht erfasst", akte.KopfMarken);

        // Widerspruch zum Modul Ausgangsliste: nur Hinweis, nichts geändert.
        f.Bewohner.AkteZeigen(u.Demo("Immergrün").Id!.Value);
        Assert.Equal("nein", akte.OffenGefuehrt.Text);
        Assert.Contains("Unterbringung widersprüchlich – bitte klären", akte.KopfMarken);
        u.Speicher.ModulSetzen("ausgangsliste", false);
        f.Bewohner.AkteZeigen(u.Demo("Immergrün").Id!.Value);
        Assert.DoesNotContain("Unterbringung widersprüchlich – bitte klären", akte.KopfMarken);   // Modul aus: kein Abgleich
    });

    [Fact]
    public void Offen_gefuehrt_im_Formular() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Recht „Bewohner bearbeiten“ (Schritt 19)
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        f.Bewohner.FormularZeigen = formular =>
        {
            Assert.Equal("ja", formular.OffenGefuehrt.Zustand);
            Assert.Equal(3, formular.OffenGefuehrt.Knoepfe.Length);
            formular.OffenGefuehrt.Knoepfe[2].Checked = true;   // nicht erfasst
            Assert.True(formular.Speichern());
            return formular.Gespeichert;
        };
        f.Kopf.Knoepfe.Single(k => k.Text == "Bearbeiten …").PerformClick();
        Assert.Equal(OffenGefuehrtZustand.NichtErfasst, u.Speicher.Bewohner(u.Demo("Birke").Id!.Value)!.OffenGefuehrt);
        Assert.Equal("nicht erfasst", f.Bewohner.Akte.OffenGefuehrt.Text);

        // Neuer Bewohner (Schritt 26e): „offen geführt“ steht nicht in der Aufnahme; aufgenommen wird nur
        // geschlossen, also „nein“ – kein leeres Feld, sondern eine bekannte Angabe.
        using var neu = f.Bewohner.FormularErstellen(null);
        Assert.Equal("nein", neu.Eingabe().OffenGefuehrt);
    });

    [Fact]
    public void Marke_auf_Karte_und_Uebersicht() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.UebersichtZeigen();
        var karten = f.Bewohner.Zeilen;
        Assert.Contains(karten.Single(k => k.Bewohner.Nachname == "Birke").Marken, m => m.Art == MarkenArt.Angabe && m.Text == "offen geführt");
        Assert.DoesNotContain(karten.Single(k => k.Bewohner.Nachname == "Ahorn").Marken, m => m.Text.StartsWith("offen geführt"));   // „nein“: keine Marke
        f.BereichWaehlen("Übersicht");
        Assert.Contains(f.Uebersicht.Zeilen.Single(k => k.Bewohner.Nachname == "Distel").Marken, m => m.Text == "offen geführt: nicht erfasst");
    });
}
