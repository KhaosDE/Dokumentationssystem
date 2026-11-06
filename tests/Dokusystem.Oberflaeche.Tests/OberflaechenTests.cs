using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

[Collection("Oberflaeche")]
public class HauptfensterTests
{
    [Fact]
    public void Demo_Hinweis_Navigation_und_Version() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var fenster = new Hauptfenster(u.Speicher);
        Assert.Contains("Demo — ausgedachte Daten", fenster.Text);
        Assert.Equal("Demo — ausgedachte Daten", KopfBereich.DemoHinweis);
        Assert.Equal(KopfBereich.DemoHinweis, fenster.Kopf.AccessibleName);
        Assert.Equal("Bewohner", fenster.Navigation[0].Text);
        Assert.True(fenster.Navigation[0].Aktiv);
        Assert.Matches(@"^Version \d+\.\d+\.\d+ · ", fenster.VersionText.Text);
    });

    [Fact]
    public void Update_Haekchen_schreibt_Einstellung() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var fenster = new Hauptfenster(u.Speicher);
        Assert.True(fenster.AutoUpdate.Checked);
        fenster.AutoUpdate.Checked = false;
        Assert.False(Einstellungen.Laden().AutoUpdate);
        fenster.AutoUpdate.Checked = true;
        Assert.True(Einstellungen.Laden().AutoUpdate);
    });

    [Fact]
    public void Selbsttest() => Hilfe.Sta(() =>
    {
        string? vorher = Environment.GetEnvironmentVariable(Pfade.Umlenkung);
        Assert.Equal(0, Dokusystem.Selbsttest.Ausfuehren());
        Assert.Equal(vorher, Environment.GetEnvironmentVariable(Pfade.Umlenkung));
    });
}

[Collection("Oberflaeche")]
public class BewohnerOberflaechenTests
{
    private static List<BewohnerKarte> Karten(Hauptfenster f) => f.Bewohner.Raster.Controls.OfType<BewohnerKarte>().ToList();

    [Fact]
    public void Uebersicht_zeigt_alle_mit_Name_und_Zimmer() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        var karten = Karten(f);
        Assert.Equal(u.Speicher.AlleBewohner().Count, karten.Count);
        Assert.Equal("Anton Ahorn, Zimmer 101. Akte öffnen", karten[0].AccessibleName);
        Assert.Equal("Bewohner", f.Kopf.TitelText);
        Assert.Equal("Neuer Bewohner …", Assert.Single(f.Kopf.Knoepfe).Text);
    });

    [Fact]
    public void Karte_oeffnet_Akte_und_zurueck() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Karten(f)[1].Waehlen();
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Berta Birke", f.Kopf.TitelText);
        Assert.Equal("22.11.1958", f.Bewohner.Akte.Geburtsdatum.Text);
        Assert.Equal(["← Alle Bewohner", "Bearbeiten …"], f.Kopf.Knoepfe.Select(k => k.Text));
        f.Kopf.Knoepfe[0].PerformClick();
        Assert.False(f.Bewohner.AkteSichtbar);
        Karten(f)[0].Waehlen();
        f.Navigation[0].Ausloesen();
        Assert.False(f.Bewohner.AkteSichtbar);   // Klick auf „Bewohner“ führt zur Übersicht
    });

    [Fact]
    public void Allergiehinweis_drei_Zustaende_sichtbar_verschieden() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        var gesehen = new Dictionary<string, (Stil.Marke, bool)>();
        foreach (string nachname in new[] { "Ahorn", "Birke", "Clematis" })
        {
            f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
            gesehen[f.Bewohner.Akte.Allergie.Text] = (f.Bewohner.Akte.Allergie.Marke, f.Bewohner.Akte.Allergie.Gestrichelt);
        }
        Assert.Equal(["Allergien: Haselnüsse", "Allergien: keine bekannt", "Allergien: nicht erfasst – bitte klären"], gesehen.Keys.Order());
        Assert.Equal(3, gesehen.Values.Select(v => v.Item1).Distinct().Count());
        Assert.True(gesehen["Allergien: nicht erfasst – bitte klären"].Item2);   // gestrichelt
        Assert.Equal(Stil.Beschluss, gesehen["Allergien: Haselnüsse"].Item1);     // rot
    });

    [Fact]
    public void Beschluss_in_der_Akte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        Assert.Equal("30.06.2027", f.Bewohner.Akte.Beschlussende.Text);
        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        Assert.Equal("kein Beschluss", f.Bewohner.Akte.Beschlussende.Text);
        f.Bewohner.AkteZeigen(u.Demo("Ginster").Id!.Value);
        Assert.Equal("nicht erfasst", f.Bewohner.Akte.Beschlussende.Text);
    });

    [Fact]
    public void Neuer_Bewohner_steht_auf_nicht_erfasst_und_prueft() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        f.Bewohner.FormularZeigen = formular =>
        {
            Assert.Equal("nicht_erfasst", formular.Allergie.Zustand);
            Assert.Equal("nicht_erfasst", formular.Beschluss.Zustand);
            formular.Vorname.Text = "Paul";
            formular.Nachname.Text = "Pappel";
            formular.Zimmer.Text = "111";
            formular.Geburtsdatum.Text = "01.01.1990";
            formular.Aufnahmedatum.Text = "01.01.1989";
            Assert.False(formular.Speichern());
            Assert.Contains("Aufnahmedatum liegt vor dem Geburtsdatum", formular.Fehler.Text);
            Assert.Equal(10, u.Speicher.AlleBewohner().Count);
            formular.Aufnahmedatum.Text = "1.2.2026";
            Assert.True(formular.Speichern());
            return formular.Gespeichert;
        };
        f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bewohner …").PerformClick();
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Paul Pappel", f.Kopf.TitelText);
        Assert.Equal("Allergien: nicht erfasst – bitte klären", f.Bewohner.Akte.Allergie.Text);
        f.Bewohner.UebersichtZeigen();
        Assert.Contains(Karten(f), k => k.Bewohner.Name == "Paul Pappel");
    });

    [Fact]
    public void Bearbeiten_mit_Bild() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        var b = u.Demo("Distel");
        using var formular = new BewohnerFormular(u.Speicher, b);
        Assert.Equal("Dieter", formular.Vorname.Text);
        Assert.Equal("Wespen- und Bienenstiche", formular.Allergie.Feld.Text);
        string keinBild = Path.Combine(u.Ordner, "kein.png");
        File.WriteAllText(keinBild, "kein Bild");
        Assert.False(formular.BildSetzen(keinBild));
        string bild = Path.Combine(u.Ordner, "probe.png");
        using (var probe = new Bitmap(40, 30))
        {
            using (var g = Graphics.FromImage(probe)) g.Clear(Color.SteelBlue);
            probe.Save(bild, System.Drawing.Imaging.ImageFormat.Png);
        }
        Assert.True(formular.BildSetzen(bild));
        Assert.True(formular.HatBild);
        formular.Allergie.Setzen("keine_bekannt", "");
        formular.Zimmer.Text = "204";
        Assert.True(formular.Speichern());
        var gespeichert = u.Speicher.Bewohner(b.Id!.Value)!;
        Assert.Equal("204", gespeichert.Zimmer);
        Assert.Equal("", gespeichert.Allergien);
        Assert.NotNull(Bilder.Pfad(gespeichert.Bild));
    });

    [Fact]
    public void Dreiwahl_kennt_nie_eingetragen_als_Rueckfall() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var formular = new BewohnerFormular(u.Speicher, null);
        formular.Allergie.Setzen("unbekannt", "");
        Assert.Equal("nicht_erfasst", formular.Allergie.Zustand);
        foreach (var knopf in formular.Allergie.Knoepfe) knopf.Checked = false;
        Assert.Equal("nicht_erfasst", formular.Allergie.Zustand);
    });

    [Fact]
    public void Demo_Daten_zuruecksetzen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        var eiche = BewohnerRegeln.AlsEingabe(u.Demo("Eiche"));
        eiche.Zimmer = "999";
        u.Speicher.BewohnerSpeichern(BewohnerRegeln.AusEingabe(eiche, u.Demo("Eiche").Id));
        f.Bewohner.AkteZeigen(u.Demo("Eiche").Id!.Value);
        Assert.True(f.DemoZuruecksetzen(nachfragen: false));
        Assert.False(f.Bewohner.AkteSichtbar);
        Assert.Equal("105", u.Demo("Eiche").Zimmer);
        Assert.Equal(10, Karten(f).Count);
    });
}

// Bildschirmfotos zur Sichtprüfung (nur mit DOKUSYSTEM_BILDSCHIRMFOTOS).
[Collection("Oberflaeche")]
public class BildschirmfotoTests
{
    [Fact]
    public void Ansichten_fotografieren() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Foto(f, "1_uebersicht");
        int nr = 2;
        foreach (string nachname in new[] { "Ahorn", "Birke", "Clematis" })
        {
            f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
            Hilfe.Foto(f, $"{nr++}_akte_{nachname}");
        }
        using (var formular = new BewohnerFormular(u.Speicher, u.Demo("Distel")))
            Hilfe.Foto(formular, "5_formular_bearbeiten");
        using (var formular = new BewohnerFormular(u.Speicher, null))
        {
            formular.Vorname.Text = "Paul";
            formular.Nachname.Text = "Pappel";
            formular.Zimmer.Text = "111";
            formular.Geburtsdatum.Text = "01.01.1990";
            formular.Aufnahmedatum.Text = "01.01.1989";
            formular.Speichern();
            Hilfe.Foto(formular, "6_formular_fehler");
        }
        using (var fortschritt = new Fortschrittsfenster())
        {
            fortschritt.Zeige("Neue Version wird geladen (Build 4 → Build 5) …", 0.42);
            Hilfe.Foto(fortschritt, "7_update");
        }
    });
}
