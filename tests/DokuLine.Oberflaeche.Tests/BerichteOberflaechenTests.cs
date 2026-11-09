using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Tägliche Dokumentation: Berichte im Bereich „Dokumentation“, „Neuer Bericht …“ und der Bereich „Übergabe“.
[Collection("Oberflaeche")]
public class BerichteOberflaechenTests
{
    [Fact]
    public void Berichte_neueste_oben_mit_Filtern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.DokumentationZeigen(u.Demo("Ahorn").Id!.Value);
        var d = f.Bewohner.Dokumentation;
        d.Zeitraum.SelectedIndex = (int)Zeitraum.DreissigTage;
        Assert.Equal(Zeitraum.DreissigTage, d.Filter.Zeitraum);
        var zeilen = d.Zeilen;
        Assert.True(zeilen.Count >= 28);
        Assert.Equal(zeilen.Select(z => z.Bericht.Zeitpunkt).OrderDescending(),
            zeilen.Select(z => z.Bericht.Zeitpunkt));
        int alle = zeilen.Count;

        d.Bereich.SelectedIndex = 1;   // Lebensbereich 1
        Assert.NotEmpty(d.Zeilen);
        Assert.True(d.Zeilen.Count < alle);
        Assert.All(d.Zeilen, z => Assert.Contains(1, z.Bericht.Bereiche));
        d.Bereich.SelectedIndex = 0;
        d.Suche.Text = "kommt-in-keinem-bericht-vor";
        Assert.Empty(d.Zeilen);
        Assert.Empty(d.Tage);
    });

    [Fact]
    public void Neuer_Bericht_aus_der_Akte_mit_Hinweis_auf_die_Uebergabe() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        string? rueckfrage = null;
        using var ersatz = new DialogErsatz(fenster =>
        {
            var formular = Assert.IsType<BerichtFormular>(fenster);
            Assert.Equal("CD", formular.Verfasser.Text);                // eigenes Kürzel vorbelegt
            Assert.Matches(@"^\d\d\.\d\d\.\d{4}$", formular.Datum.Text);  // heutiges Datum vorbelegt
            Assert.False(formular.BewohnerWahl.Enabled);                // Bewohner steht fest (aus der Akte)
            Assert.Equal(0, formular.BewohnerWahl.SelectedIndex);
            formular.Bereiche[2].Checked = true;
            formular.Inhalt.Text = "Hat den Tisch gedeckt. Termin beim Arzt am Donnerstag.";
            formular.Rueckfrage = woerter => { rueckfrage = woerter; return DialogResult.Yes; };
            return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        });
        Assert.DoesNotContain(f.Navigation, n => n.Text == "Neuer Bericht …");
        f.Bewohner.AkteZeigen(u.Demo("Distel").Id!.Value);
        f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bericht …").PerformClick();

        Assert.NotNull(rueckfrage);
        Assert.Contains("Arzt", rueckfrage);
        // Danach die Berichte des Bewohners in der Dokumentation.
        Assert.False(f.Bewohner.AkteSichtbar);
        Assert.Equal("Dokumentation", f.Kopf.TitelText);
        Assert.Equal("Dieter Distel", f.Bewohner.Gewaehlt!.Name);
        var zeile = f.Bewohner.Dokumentation.Zeilen.Single(z => z.Bericht.Text.StartsWith("Hat den Tisch gedeckt"));
        Assert.True(zeile.Bericht.Uebergabe);
        Assert.True(zeile.UebergabeZeichen.Visible);
        Assert.Equal(DokuRegeln.ArtAlltag, zeile.Bericht.Art);
        Assert.Contains(u.Speicher.Uebergaben(DateTime.Now), x => x.BerichtId == zeile.Bericht.Id);
    });

    [Fact]
    public void Ohne_Pflichtangaben_wird_nicht_gespeichert() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "" });   // nicht angemeldet: kein Verfasser
        using var formular = new BerichtFormular(u.Speicher, u.Demo("Ahorn"));
        Assert.Equal(Recht.NichtAngemeldetAnzeige, formular.Verfasser.Text);
        formular.Rueckfrage = _ => DialogResult.No;
        Assert.False(formular.Speichern());
        Assert.Contains("Verfasser", formular.Fehler.Text);
        Assert.Contains("Lebensbereich", formular.Fehler.Text);
        Assert.Contains("Text fehlt", formular.Fehler.Text);
        Assert.Null(formular.Gespeichert);
    });

    [Fact]
    public void Nachtrag_haengt_an_und_der_Text_bleibt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.DokumentationZeigen(u.Demo("Clematis").Id!.Value);
        var d = f.Bewohner.Dokumentation;
        var erste = d.Zeilen[0].Bericht;
        Assert.True(d.Zeilen[0].ContextMenuStrip!.Items[0].Enabled);   // „Nachtrag schreiben …“
        using var ersatz = new DialogErsatz(fenster =>
        {
            var formular = Assert.IsType<NachtragFormular>(fenster);
            Assert.Equal("CD", formular.Verfasser.Text);
            formular.Inhalt.Text = "Uhrzeit korrigiert: es war eine halbe Stunde später.";
            return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        });
        d.NachtragSchreiben(erste);
        var zeile = d.Zeilen.Single(z => z.Bericht.Id == erste.Id);
        Assert.Equal(erste.Text, zeile.Bericht.Text);
        Assert.Contains(zeile.Bericht.Nachtraege,
            n => n.Text == "Uhrzeit korrigiert: es war eine halbe Stunde später.");
        Assert.True(d.MitNachtrag.Text.Length > 0);
        d.MitNachtrag.Checked = true;
        Assert.Contains(d.Zeilen, z => z.Bericht.Id == erste.Id);
        Assert.All(d.Zeilen, z => Assert.NotEmpty(z.Bericht.Nachtraege));
    });
}

[Collection("Oberflaeche")]
public class UebergabeOberflaechenTests
{
    private static (Umgebung, Hauptfenster) Oeffnen(string kuerzel = "CD")
    {
        var u = new Umgebung();
        if (kuerzel.Length > 0) Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Navigation.Single(n => n.Text == "Übergabe").Ausloesen();
        return (u, f);
    }

    [Fact]
    public void Nur_offene_Eintraege_allgemein_oben_dann_nach_Bewohner() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Uebergabe;
            Assert.Same(seite, f.Seite("Übergabe"));
            Assert.Equal("Übergabe", f.Kopf.TitelText);
            Assert.Equal(seite.KopfText, f.Kopf.KleinText);
            Assert.Equal("WOHNBEREICH ALLGEMEIN", seite.Abschnitte[0]);
            Assert.Contains(seite.Abschnitte, a => a.StartsWith("AHORN, ANTON"));
            Assert.DoesNotContain(seite.Abschnitte, a => a.StartsWith("ERLEDIGT"));
            Assert.Equal(seite.Offen, seite.Karten.Count);
            Assert.All(seite.Karten, k => Assert.True(UebergabeRegeln.Offen(k.Uebergabe, DateTime.Now)));
            // Erster Besuch mit diesem Kürzel: alles Ungelesene ist neu.
            Assert.Equal(seite.Offen, seite.Neu);
            Assert.All(seite.Karten, k => Assert.Contains("neu seit deiner letzten Schicht", k.MarkenTexte));
            // Kurzer Übergabe-Satz mit Bericht dahinter: Bericht per Klick lesbar.
            Assert.Contains(seite.Karten, k => k.Uebergabe.Text.Length > 0 && k.BerichtLesen.Visible);
            Assert.False(seite.KuerzelHinweis.Visible);

            // Zweiter Besuch: Was vorher da war, ist nicht mehr neu.
            f.BereichWaehlen("Dokumentation");
            f.BereichWaehlen("Übergabe");
            Assert.Equal(0, seite.Neu);
            Assert.DoesNotContain(seite.Karten, k => k.MarkenTexte.Contains("neu seit deiner letzten Schicht"));
        }
    });

    [Fact]
    public void Gelesen_Verlaengern_Erledigt_ohne_etwas_zu_loeschen() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Uebergabe;
            long id = seite.Karten.First(k => !k.Uebergabe.Allgemein && k.Uebergabe.GiltBis is not null).Uebergabe.Id;
            UebergabeKarte Karte() => seite.Karten.Single(k => k.Uebergabe.Id == id);

            Karte().Gelesen.PerformClick();
            Assert.Contains("CD (", Karte().GelesenText.Text);
            Assert.Equal("Gelesen ✓", Karte().Gelesen.Text);
            Assert.False(Karte().Gelesen.Enabled);

            using (new DialogErsatz(fenster =>
            {
                var dauer = Assert.IsType<DauerFenster>(fenster);
                dauer.Dauer.SelectedIndex = (int)UebergabeDauer.BisErledigt;
                return dauer.Uebernehmen() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().Verlaengern.PerformClick();
            Assert.Null(Karte().Uebergabe.GiltBis);
            Assert.Contains("bis erledigt (verlängert)", Karte().MarkenTexte);

            int offen = seite.Offen;
            Karte().Erledigt.PerformClick();
            Assert.Equal(offen - 1, seite.Offen);
            Assert.DoesNotContain(seite.Karten, k => k.Uebergabe.Id == id);
            Assert.Equal(seite.KopfText, f.Kopf.KleinText);   // Kopf zählt mit

            seite.ErledigteZeigen.Checked = true;
            Assert.Contains(seite.Abschnitte, a => a.StartsWith("ERLEDIGT / ABGELAUFEN"));
            var erledigt = Karte();
            Assert.StartsWith("erledigt ", erledigt.MarkenTexte.Last());
            Assert.EndsWith("· CD", erledigt.MarkenTexte.Last());
            Assert.False(erledigt.Erledigt.Visible);
            Assert.Contains(seite.Karten,
                k => k.MarkenTexte.Last().StartsWith("abgelaufen "));   // Demo: ein abgelaufener Eintrag
        }
    });

    [Fact]
    public void Notiz_fuer_den_Wohnbereich() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Uebergabe;
            string? fehler = null;
            using (new DialogErsatz(fenster =>
            {
                var notiz = Assert.IsType<DauerFenster>(fenster);
                Assert.False(notiz.Uebernehmen());          // ohne Text nicht
                fehler = notiz.Fehler.Text;
                notiz.Inhalt.Text = "Heizungsableser kommt morgen zwischen 8 und 12 Uhr.";
                notiz.Dauer.SelectedIndex = (int)UebergabeDauer.DreiTage;
                return notiz.Uebernehmen() ? DialogResult.OK : DialogResult.Cancel;
            }))
                seite.NotizKnopf.PerformClick();
            Assert.Contains("Text der Notiz fehlt", fehler);
            var karte = seite.Karten.Single(k => k.Uebergabe.Text == "Heizungsableser kommt morgen zwischen 8 und 12 Uhr.");
            Assert.True(karte.Uebergabe.Allgemein);
            Assert.Equal("CD", karte.Uebergabe.Verfasser);
            Assert.False(karte.BerichtLesen.Visible);
        }
    });

    [Fact]
    public void Ohne_Kuerzel_Hinweis_und_keine_Knoepfe() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen(kuerzel: "");
        using (u)
        using (f)
        {
            var seite = f.Uebergabe;
            Assert.True(seite.KuerzelHinweis.Visible);
            Assert.Equal(0, seite.Neu);
            Assert.All(seite.Karten,
                k => Assert.False(k.Gelesen.Enabled || k.Erledigt.Enabled || k.Verlaengern.Enabled));
        }
    });

    [Fact]
    public void Uebergabeblatt_nur_offene_Eintraege() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                f.Uebergabe.DruckKnopf.PerformClick();
                Assert.StartsWith("Übergabeblatt", Assert.Single(ersatz.Titel));
            }
            var jetzt = DateTime.Now;
            var offen = u.Speicher.Uebergaben(jetzt);
            var namen = u.Speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
            using var bild = new Bitmap(800, 1130);
            using var g = Graphics.FromImage(bild);
            Assert.Equal(offen.Count, new UebergabeDruck(offen, namen, jetzt).SeiteZeichnen(g,
                0));   // passt auf eine Seite
        }
    });
}
