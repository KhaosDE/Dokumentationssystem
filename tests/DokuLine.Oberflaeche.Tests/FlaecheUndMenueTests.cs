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
            // Zähler bleibt am Symbol (wie viele offen sind, hängt vom Dienst ab: Standardaufgaben je Schicht).
            Assert.Equal(u.Speicher.Aufgaben(DateTime.Now, bereich: Arbeitsbereich.Gewaehlt).Count, f.Navigation.Single(n => n.Text == "Aufgaben").Anzahl);
            Assert.True(f.Navigation.Single(n => n.Text == "Aufgaben").Anzahl > 0);
            Assert.True(f.InfoSymbol.Visible);
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
            Assert.True(f.VersionText.Visible);
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
        Assert.True(f.Uebersicht.Waehlen(u.Demo("Birke").Id!.Value));   // Liste (Schritt 26l): Zeile wählen, Rechtsklick
        var menue = f.Uebersicht.Liste.ContextMenuStrip!;
        Assert.Equal(["Akte öffnen", "Neuer Bericht …", "Überleitungsbogen …", "Abgängigkeit melden …", "Auskunft prüfen …"], menue.Items.Cast<ToolStripItem>().Select(i => i.Text));
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
            menue.Items[1].PerformClick();
        // Danach die Berichte des Bewohners in der Dokumentation.
        Assert.Equal("Dokumentation", f.AktiverBereich);
        Assert.False(f.Bewohner.AkteSichtbar);
        Assert.Contains(f.Bewohner.Dokumentation.Zeilen, z => z.Bericht.Text == "Hat beim Kochen geholfen.");

        // In der Dokumentation genauso: Name wählen, „Neuer Bericht …“.
        Assert.True(f.Bewohner.Waehlen(u.Demo("Eiche").Id!.Value));
        using (var ersatz = new DialogErsatz(fenster =>
        {
            Assert.StartsWith("Eiche, Emil", Assert.IsType<BerichtFormular>(fenster).BewohnerWahl.Text);
            return DialogResult.Cancel;
        }))
        {
            Assert.False(f.Bewohner.NeuerBericht());   // abgebrochen
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
        f.Kopf.PlatzhalterKlicken();
        Assert.Equal("Einstellungen", f.AktiverBereich);                    // Klick führt zu den Einstellungen
        Assert.Equal("Wohnbereich", f.Einstellungsseite.AktiveGruppe);     // … gleich zum Logo
        Hilfe.Foto(f.Kopf, "65_kopf_logo_platzhalter");
        string logo = Path.Combine(u.Ordner, "logo.png");
        Hilfe.Testbild(logo, 120, 40);
        Einstellungen.LogoSetzen(logo);
        f.Kopf.LogoSetzen(Einstellungen.LogoPfad());
        Assert.True(f.Kopf.PlatzhalterFeld.IsEmpty);                        // mit Logo: kein Platzhalter
    });
}
