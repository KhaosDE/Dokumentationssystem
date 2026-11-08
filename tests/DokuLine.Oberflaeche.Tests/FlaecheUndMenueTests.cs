using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 18: Inhalte füllen die ganze Fläche, Seitenmenü einklappbar,
// „Neuer Bericht …“ nur noch mit gewähltem Bewohner (Akte, Reiter, Rechtsklick auf eine Karte).
[Collection("Oberflaeche")]
public class FlaecheUndMenueTests
{
    [Fact]
    public void Akte_und_Wochenplan_fuellen_die_Breite() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        // Der Bildschirm des Testrechners ist klein: Mehr Platz entsteht hier durch das Einklappen des Menüs.
        using var f = new Hauptfenster(u.Speicher) { Size = new Size(900, 700) };
        Hilfe.Zeigen(f);
        var akte = f.Bewohner.Akte;
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        int Rand() => Stil.Px(akte, Stil.AbstandGross);
        Assert.Equal(akte.ClientSize.Width - 2 * Rand(), akte.Reiter.Width);
        int vorher = akte.Reiter.Width;
        f.MenueUmschalten();
        f.PerformLayout();
        Assert.True(akte.Reiter.Width > vorher);
        Assert.Equal(akte.ClientSize.Width - 2 * Rand(), akte.Reiter.Width);   // wächst mit, keine feste Höchstbreite

        akte.Reiter.Waehlen("Wochenplan");
        Assert.Equal(akte.Reiter.Width, akte.Wochenplan.Width);
        Assert.Equal(akte.Wochenplan.Width, akte.Wochenplan.Raster.Width);
        // Die Stunden verteilen sich auf die verfügbare Höhe; zu wenig Platz: Mindesthöhe (Bildlauf).
        var raster = akte.Wochenplan.Raster;
        int knapp = raster.BenoetigteHoehe(0);
        Assert.Equal(knapp, raster.BenoetigteHoehe(100));
        Assert.True(raster.BenoetigteHoehe(knapp * 2) > knapp * 2 - 20);
        Hilfe.Foto(f, "58_akte_wochenplan_breit");

        // Auch die Seiten: Aufgaben füllen die Breite.
        f.BereichWaehlen("Aufgaben");
        int aufgabenRand = Stil.Px(f.Aufgaben, Stil.AbstandGross);
        Assert.Contains(f.Aufgaben.Controls.OfType<AufgabeKarte>(), k => k.Width == f.Aufgaben.ClientSize.Width - 2 * aufgabenRand);
    });

    [Fact]
    public void Seitenmenue_einklappen_und_merken() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using (var f = new Hauptfenster(u.Speicher))
        {
            Hilfe.Zeigen(f);
            f.BereichWaehlen("Übersicht");
            Assert.False(f.MenueEingeklappt);
            Assert.Equal("Menü einklappen", f.MenueKnopf.Text);
            int eintragBreit = f.Navigation[0].Width, kopfBreit = f.Kopf.Width;
            f.MenueKnopf.Ausloesen();
            Assert.True(f.MenueEingeklappt);
            Assert.True(Einstellungen.Laden().MenueEingeklappt);
            Assert.Equal("Menü ausklappen", f.MenueKnopf.Text);
            Assert.All(f.Navigation, n => Assert.True(n.Eingeklappt));
            Assert.All(f.Navigation, n => Assert.NotEmpty(n.Symbol));
            Assert.True(f.Navigation[0].Width < eintragBreit);
            Assert.True(f.Kopf.Width > kopfBreit);                      // die Inhalte nutzen die Fläche sofort
            Assert.Equal(5, f.Navigation.Single(n => n.Text == "Aufgaben").Anzahl);   // Zähler bleibt am Symbol
            Assert.True(f.DemoSymbol.Visible);
            Assert.False(f.DemoKnopf.Visible);
            Assert.False(f.VersionText.Visible);
            Assert.StartsWith("Version ", f.InfoSymbol.Text);
            Assert.Contains("R1", f.InfoSymbol.Text);
            Hilfe.Foto(f, "57_menue_eingeklappt");
            f.Navigation.Single(n => n.Text == "Kalender").Ausloesen();
            Assert.Equal("Kalender", f.AktiverBereich);                  // Bedienung geht eingeklappt wie sonst
        }
        using (var f = new Hauptfenster(u.Speicher))
        {
            Hilfe.Zeigen(f);
            Assert.True(f.MenueEingeklappt);                             // gemerkt
            f.MenueUmschalten();
            Assert.False(f.MenueEingeklappt);
            Assert.True(f.DemoKnopf.Visible);
            Assert.False(Einstellungen.Laden().MenueEingeklappt);
        }
    });

    [Fact]
    public void Neuer_Bericht_per_Rechtsklick_auf_eine_Karte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var karte = f.Uebersicht.Karten.Single(k => k.Bewohner.Nachname == "Birke");
        Assert.Equal(["Akte öffnen", "Neuer Bericht …"], karte.ContextMenuStrip!.Items.Cast<ToolStripItem>().Select(i => i.Text));
        using (new DialogErsatz(fenster =>
        {
            var formular = Assert.IsType<BerichtFormular>(fenster);
            Assert.False(formular.BewohnerWahl.Enabled);
            Assert.StartsWith("Birke, Berta", formular.BewohnerWahl.Text);
            formular.Bereiche[0].Checked = true;
            formular.Inhalt.Text = "Hat beim Kochen geholfen.";
            formular.Rueckfrage = _ => DialogResult.No;
            return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            karte.ContextMenuStrip.Items[1].PerformClick();
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Berichte", f.Bewohner.Akte.Reiter.Aktiv);
        Assert.Contains(f.Bewohner.Akte.Berichte.Liste.Karten, k => k.Bericht.Text == "Hat beim Kochen geholfen.");

        // Auf der Seite „Bewohner“ genauso.
        f.BereichWaehlen("Bewohner");
        var bewohnerKarte = f.Bewohner.Raster.Controls.OfType<BewohnerKarte>().Single(k => k.Bewohner.Nachname == "Eiche");
        using (var ersatz = new DialogErsatz(fenster =>
        {
            Assert.StartsWith("Eiche, Emil", Assert.IsType<BerichtFormular>(fenster).BewohnerWahl.Text);
            return DialogResult.Cancel;
        }))
        {
            bewohnerKarte.BerichtWaehlen();
            Assert.Single(ersatz.Titel);
        }
    });

    [Fact]
    public void Platzhalter_fuer_das_Logo_der_Einrichtung() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.False(f.Kopf.HatLogo);
        Assert.False(f.Kopf.PlatzhalterFeld.IsEmpty);                       // ohne Logo: Platzhalter oben rechts
        Assert.True(f.Kopf.PlatzhalterFeld.Right <= f.Kopf.Width);
        bool geoeffnet = false;
        f.EinstellungenZeigen = _ => { geoeffnet = true; return false; };
        f.Kopf.PlatzhalterKlicken();
        Assert.True(geoeffnet);                                             // Klick führt zu den Einstellungen
        Hilfe.Foto(f.Kopf, "65_kopf_logo_platzhalter");
        string logo = Path.Combine(u.Ordner, "logo.png");
        Hilfe.Testbild(logo, 120, 40);
        Einstellungen.LogoSetzen(logo);
        f.Kopf.LogoSetzen(Einstellungen.LogoPfad());
        Assert.True(f.Kopf.PlatzhalterFeld.IsEmpty);                        // mit Logo: kein Platzhalter
    });
}
