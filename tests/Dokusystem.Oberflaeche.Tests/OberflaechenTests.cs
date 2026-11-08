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
        Assert.Equal(["Übersicht", "Bewohner", "Übergabe", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
                      "Medikamente", "Ausgangsliste", "Einstellungen"],
            fenster.Navigation.Select(n => n.Text));
        Assert.Equal("Bewohner", fenster.AktiverBereich);
        Assert.True(fenster.Navigation.Single(n => n.Text == "Bewohner").Aktiv);
        Assert.Matches(@"^Version \d+\.\d+\.\d+ · ", fenster.VersionText.Text);
    });

    [Fact]
    public void Einstellungen_Benutzer_Update_und_Logo() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var fenster = new Hauptfenster(u.Speicher);
        string logo = Path.Combine(u.Ordner, "eigenes-bild.png");
        Hilfe.Testbild(logo, 120, 40);
        bool gezeigt = false;
        fenster.EinstellungenZeigen = einstellungen =>
        {
            gezeigt = true;
            Assert.True(einstellungen.AutoUpdate.Checked);
            Assert.False(einstellungen.HatLogo);
            Assert.Equal("– niemand –", einstellungen.Benutzer.Text);
            Assert.Equal(9, einstellungen.Benutzer.Items.Count);   // niemand + 8 ausgedachte Kollegen
            einstellungen.Benutzer.SelectedIndex = einstellungen.Benutzer.Items.Cast<string>().ToList().FindIndex(x => x.StartsWith("Emma Fink (EF)"));
            einstellungen.AutoUpdate.Checked = false;
            Assert.True(einstellungen.LogoSetzen(logo));
            return einstellungen.Speichern();
        };
        Assert.False(fenster.Kopf.HatLogo);
        fenster.Navigation.Single(n => n.Text == "Einstellungen").Ausloesen();
        Assert.True(gezeigt);
        Assert.Equal("Bewohner", fenster.AktiverBereich);   // Einstellungen sind ein Fenster, kein Bereich
        var werte = Einstellungen.Laden();
        Assert.Equal(("EF", false), (werte.Kuerzel, werte.AutoUpdate));
        Assert.Equal("Angemeldet als Emma Fink (EF) ▾", fenster.Kopf.BenutzerText);
        Assert.NotNull(Einstellungen.LogoPfad());
        Assert.True(fenster.Kopf.HatLogo);

        fenster.EinstellungenZeigen = einstellungen =>
        {
            Assert.True(einstellungen.HatLogo);
            Assert.StartsWith("Emma Fink (EF)", einstellungen.Benutzer.Text);
            Hilfe.Zeigen(einstellungen);   // PerformClick wirkt nur bei angezeigtem Fenster
            einstellungen.LogoWeg.PerformClick();
            return einstellungen.Speichern();
        };
        Hilfe.Zeigen(fenster);
        Assert.True(fenster.EinstellungenOeffnen());
        Assert.Null(Einstellungen.LogoPfad());
        Assert.False(fenster.Kopf.HatLogo);
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
        // Ohne eigenes Kürzel zählt „Übergabe“ alle offenen Einträge; „Wichtig“: wichtiger Bericht der letzten 7 Tage.
        // Vorne die Statusmeldung aus der Demo (Mehrplatz, gesetzt von der Therapie).
        Assert.StartsWith("Anton Ahorn, Zimmer 101, Status: in der Holzwerkstatt, bis ", karten[0].AccessibleName);
        Assert.EndsWith(", Übergabe 1, Wichtig, Beschluss bis 30.06.2027, Allergie: Haselnüsse. Akte öffnen", karten[0].AccessibleName);
        Assert.Equal("Bewohner", f.Kopf.TitelText);
        Assert.Equal("Neuer Bewohner …", Assert.Single(f.Kopf.Knoepfe).Text);
    });

    [Fact]
    public void Karten_zeigen_Marken_und_Aufenthalt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        f.Bewohner.Heute = () => new DateOnly(2026, 11, 7);
        f.Bewohner.UebersichtZeigen();
        var birke = Karten(f).Single(k => k.Bewohner.Nachname == "Birke");
        // keine bekannt, kein Beschluss: Normalfall ohne Marke dazu; ihre Übergabe ist erledigt
        Assert.DoesNotContain(birke.Marken, m => m.Art is MarkenArt.Beschluss or MarkenArt.Allergie or MarkenArt.NichtErfasst or MarkenArt.Uebergabe);
        Assert.Equal("seit 15.02.2016 · 10 Jahre, 8 Monate", birke.AufenthaltText);
        var ginster = Karten(f).Single(k => k.Bewohner.Nachname == "Ginster");
        Assert.Equal(["Allergien nicht erfasst", "Beschluss nicht erfasst"],
            ginster.Marken.Where(m => m.Art == MarkenArt.NichtErfasst && !m.Text.StartsWith("überfällig")).Select(m => m.Text));   // ohne Marke des Moduls
        Assert.Equal("Übergabe 1", ginster.Marken[0].Text);
    });

    [Fact]
    public void Uebergabe_Marke_zaehlt_nur_was_das_eigene_Kuerzel_noch_nicht_gelesen_hat() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Assert.Contains(Karten(f)[0].Marken, m => m.Text == "Übergabe 1");
        Einstellungen.Aendern(w => w with { Kuerzel = "AB" });   // AB hat Ahorns Übergabe schon gelesen
        f.Bewohner.UebersichtZeigen();
        Assert.DoesNotContain(Karten(f)[0].Marken, m => m.Art == MarkenArt.Uebergabe);
        Assert.Contains(Karten(f)[0].Marken, m => m.Text == "Wichtig");
    });

    [Fact]
    public void Filterknoepfe_mit_Zaehlern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        var knoepfe = f.Bewohner.FilterKnoepfe;
        Assert.Equal([("Alle", 10), ("Beschluss", 5), ("Allergie", 4), ("Nicht erfasst", 4)],
            knoepfe.Values.Select(k => (k.Text, k.Anzahl)));
        Assert.True(knoepfe[BewohnerFilter.Alle].Aktiv);
        knoepfe[BewohnerFilter.Allergie].Ausloesen();
        Assert.Equal(4, Karten(f).Count);
        Assert.All(Karten(f), k => Assert.Equal(AllergieZustand.Eingetragen, k.Bewohner.Allergie));
        Assert.Equal("4 von 10 Bewohner", f.Kopf.KleinText);
        Assert.True(knoepfe[BewohnerFilter.Allergie].Aktiv);
        Assert.False(knoepfe[BewohnerFilter.Alle].Aktiv);
        Karten(f)[0].Waehlen();
        Assert.False(f.Bewohner.FilterLeiste.Visible);   // in der Akte keine Filter
        f.Bewohner.UebersichtZeigen();
        Assert.Equal(4, Karten(f).Count);                // Filter bleibt beim Zurückkehren
        knoepfe[BewohnerFilter.Alle].Ausloesen();
        Assert.Equal(10, Karten(f).Count);
    });

    [Fact]
    public void Statusleiste_erklaert_die_Farben() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Assert.Contains("Beschluss, Allergie", f.Statusleiste.AccessibleName);
        Assert.Contains("nicht erfasst", f.Statusleiste.AccessibleName);
    });

    [Fact]
    public void Karte_oeffnet_Akte_und_zurueck() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Karten(f)[1].Waehlen();
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Berta Birke", f.Kopf.TitelText);
        Assert.Equal("22.11.1958", f.Bewohner.Akte.Geburtsdatum.Text);
        Assert.Equal(["← Alle Bewohner", "Nachrichten …", "Status …", "Neuer Bericht …", "Bearbeiten …"], f.Kopf.Knoepfe.Select(k => k.Text));
        f.Kopf.Knoepfe[0].PerformClick();
        Assert.False(f.Bewohner.AkteSichtbar);
        Karten(f)[0].Waehlen();
        f.Navigation.Single(n => n.Text == "Bewohner").Ausloesen();
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
    public void Akte_hat_Reiter_noch_nicht_gebaute_zeigen_ihre_Version() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Equal(["Stammdaten", "Berichte", "Medikation", "Wochenplan", "Ausgang", "Biografie", "Reflexion", "HEB"], akte.Reiter.Namen);
        Assert.Equal("Stammdaten", akte.Reiter.Aktiv);
        Assert.False(akte.ReiterPlatzhalter.Visible);
        Assert.Empty(Bauplan.Reiter);   // alle Reiter sind gebaut
        akte.Reiter.Waehlen("HEB");
        Assert.Equal("Allergien: Haselnüsse", akte.Allergie.Text);   // Allergiehinweis bleibt oben
        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        Assert.Equal("Stammdaten", akte.Reiter.Aktiv);   // neue Akte beginnt bei den Stammdaten
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
        Hilfe.Zeigen(f);
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
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Berichte");
        Hilfe.Foto(f, "8_akte_reiter_berichte");
        f.Bewohner.Akte.Berichte.NurWichtige.Checked = true;
        Hilfe.Foto(f, "13_akte_nur_wichtige");
        f.Bewohner.Akte.Berichte.NurWichtige.Checked = false;
        using (var formular = new BerichtFormular(u.Speicher, null))
        {
            formular.BewohnerWaehlen(u.Demo("Eiche").Id!.Value);
            formular.Bereiche[0].Checked = formular.Bereiche[3].Checked = true;
            formular.Inhalt.Text = "Hat den Einkauf für die Wohngruppe mit Liste selbstständig erledigt.";
            formular.Wichtig.Checked = formular.Uebergabe.Checked = true;
            formular.UebergabeDauer.SelectedIndex = (int)UebergabeDauer.DreiTage;
            formular.UebergabeText.Text = "Einkaufsliste für Samstag liegt im Büro";
            Hilfe.Foto(formular, "14_neuer_bericht");
        }
        f.Navigation.Single(n => n.Text == "Übergabe").Ausloesen();
        Hilfe.Foto(f, "15_uebergabe");
        f.Uebergabe.ErledigteZeigen.Checked = true;
        f.Uebergabe.AutoScrollPosition = new Point(0, 100000);
        Hilfe.Foto(f, "16_uebergabe_erledigt");
        f.Uebergabe.ErledigteZeigen.Checked = false;
        using (var bild = new Bitmap(1240, 1754))
        using (var g = Graphics.FromImage(bild))
        {
            g.Clear(Color.White);
            g.ScaleTransform(1240f / 210f / 3.7795f, 1240f / 210f / 3.7795f);
            new UebergabeDruck(u.Speicher.Uebergaben(DateTime.Now),
                u.Speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}"), DateTime.Now).SeiteZeichnen(g, 0);
            string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
            if (!string.IsNullOrWhiteSpace(ordner)) bild.Save(Path.Combine(ordner, "17_uebergabeblatt.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        f.Navigation.Single(n => n.Text == "Aufgaben").Ausloesen();
        Hilfe.Foto(f, "18_aufgaben");
        using (var neu = new AufgabeFenster(u.Speicher, u.Demo("Fichte").Id, null))
        {
            neu.Inhalt.Text = "Mit Frieda die Anmeldung für den Malkurs abgeben";
            neu.FaelligDatum.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(3)));
            Hilfe.Foto(neu, "19_neue_aufgabe");
        }
        f.Navigation.Single(n => n.Text == "Kalender").Ausloesen();
        Hilfe.Foto(f, "36_kalender_monat");
        f.Kalender.AnsichtWaehlen(KalenderAnsicht.Woche);
        Hilfe.Foto(f, "37_kalender_woche");
        f.Kalender.AnsichtWaehlen(KalenderAnsicht.Tag);
        f.Kalender.TagWaehlen(DateOnly.FromDateTime(DateTime.Today.AddDays(8)));
        Hilfe.Foto(f, "38_kalender_tag_ausflug");
        f.Kalender.AnsichtWaehlen(KalenderAnsicht.Monat);
        using (var termin = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today), u.Demo("Eiche").Id))
            Hilfe.Foto(termin, "39_kalender_termin");
        f.Navigation.Single(n => n.Text == "Ausgangsliste").Ausloesen();
        Hilfe.Foto(f, "40_ausgangsliste");
        f.BereichWaehlen("Bewohner");
        f.Bewohner.AkteZeigen(u.Demo("Eiche").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Ausgang");
        Hilfe.Foto(f, "41_akte_ausgang");
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Biografie");
        Hilfe.Foto(f, "47_akte_biografie");
        using (var bio = new BiografieFenster(u.Speicher, u.Demo("Distel"), u.Speicher.Biografie(u.Demo("Distel").Id!.Value)[3]))
            Hilfe.Foto(bio, "48_biografie_bearbeiten");
        f.Bewohner.AkteZeigen(u.Demo("Clematis").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Reflexion");
        Hilfe.Foto(f, "49_akte_reflexion");
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("HEB");
        Hilfe.Foto(f, "50_akte_heb");
        var kiefer = u.Demo("Kiefer");
        using (var heb = new HebFenster(u.Speicher, kiefer, u.Speicher.HebBoegen(kiefer.Id!.Value).Single(x => !x.Fertig)))
            Hilfe.Foto(heb, "51_heb_bogen");
        var db = new Dokusystem.Modul.Ausgangsliste.Datenbank(u.Speicher);
        using (var ausgang = new Dokusystem.Modul.Ausgangsliste.AusgangFenster(db, db.Platz(u.Demo("Ahorn").Id!.Value)!))
            Hilfe.Foto(ausgang, "42_ausgang_fenster");
        using (var status = new Dokusystem.Modul.Ausgangsliste.StatusFenster(db, db.Platz(u.Demo("Birke").Id!.Value)!))
            Hilfe.Foto(status, "43_anwesenheit_offen_gefuehrt");
        using (var kueche = new Dokusystem.Modul.Ausgangsliste.KuechendienstFenster(db))
            Hilfe.Foto(kueche, "44_kuechendienst");
        using (var nacht = new Dokusystem.Modul.Ausgangsliste.NachtberichtFenster(db.Laden(), db.AusgaengeLaden(DateTime.Now)))
            Hilfe.Foto(nacht, "45_nachtbericht");
        using (var einstellungen = new EinstellungenFenster(u.Speicher.Kollegen(), u.Speicher))
            Hilfe.Foto(einstellungen, "46_einstellungen_module");
        f.Navigation.Single(n => n.Text == "Übersicht").Ausloesen();
        Hilfe.Foto(f, "9_uebersicht");
        string logo = Path.Combine(u.Ordner, "eigenes-bild.png");
        Hilfe.Testbild(logo, 180, 60);
        Einstellungen.LogoSetzen(logo);
        f.Kopf.LogoSetzen(Einstellungen.LogoPfad());
        f.BenutzerWaehlen("AB");
        f.BereichWaehlen("Bewohner");
        Hilfe.Foto(f, "10_kopf_mit_bild");
        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikation");
        Hilfe.Foto(f, "25_medikation_stufenplan");
        f.Bewohner.AkteZeigen(u.Demo("Clematis").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikation");
        Hilfe.Foto(f, "26_medikation_pause");
        using (var neu = new VerordnungFenster(u.Speicher, u.Demo("Fichte")))
        {
            neu.MedikamentWahl.SelectedIndex = 0;
            neu.Mengen[0].Text = "1";
            neu.Arzt.Text = "Dr. Ulme (ausgedacht)";
            Hilfe.Foto(neu, "27_neue_verordnung");
        }
        using (var plan = new AenderungFenster(u.Speicher, u.Speicher.Verordnungen(u.Demo("Ahorn").Id!.Value)[0], AenderungArt.Stufenplan))
        {
            plan.StufeSetzen(0, Datum.Text(DateOnly.FromDateTime(DateTime.Today)), "0", "0", "½", "0");
            plan.StufeSetzen(1, Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(7))), "0", "0", "0", "0");
            Hilfe.Foto(plan, "28_stufenplan");
        }
        using (var bild = new Bitmap(1240, 1754))
        using (var g = Graphics.FromImage(bild))
        {
            g.Clear(Color.White);
            g.ScaleTransform(1240f / 210f / 3.7795f, 1240f / 210f / 3.7795f);
            var heute = DateOnly.FromDateTime(DateTime.Today);
            new MedikationDruck(u.Demo("Fichte"), heute, u.Speicher.Verordnungen(u.Demo("Fichte").Id!.Value).Select(v => MedikationRegeln.Stand(v, heute)).ToList()).SeiteZeichnen(g);
            string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
            if (!string.IsNullOrWhiteSpace(ordner)) bild.Save(Path.Combine(ordner, "29_medikation_druck.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        f.BereichWaehlen("Bewohner");
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Wochenplan");
        Hilfe.Foto(f, "32_wochenplan");
        f.Bewohner.Akte.AutoScrollPosition = new Point(0, 100000);
        Hilfe.Foto(f, "33_wochenplan_unten");
        using (var eintrag = new PlanEintragFenster(u.Speicher, u.Demo("Ahorn"), null, WochenplanRegeln.Montag(DateOnly.FromDateTime(DateTime.Today))))
            Hilfe.Foto(eintrag, "34_wochenplan_eintrag");
        using (var bild = new Bitmap(1754, 1240))
        using (var g = Graphics.FromImage(bild))
        {
            g.Clear(Color.White);
            g.ScaleTransform(1754f / 297f / 3.7795f, 1754f / 297f / 3.7795f);
            var montag = WochenplanRegeln.Montag(DateOnly.FromDateTime(DateTime.Today));
            new WochenplanDruck(u.Demo("Ahorn"), montag, u.Speicher.Woche(u.Demo("Ahorn").Id!.Value, montag)).SeiteZeichnen(g);
            string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
            if (!string.IsNullOrWhiteSpace(ordner)) bild.Save(Path.Combine(ordner, "35_wochenplan_druck.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        f.Navigation.Single(n => n.Text == "Therapie").Ausloesen();
        Hilfe.Foto(f, "30_therapie");
        using (var angebot = new AngebotFenster(u.Speicher, u.Speicher.Angebote()[0]))
            Hilfe.Foto(angebot, "31_angebot_aendern");
        f.Navigation.Single(n => n.Text == "Medikamente").Ausloesen();
        Hilfe.Foto(f, "23_medikamente");
        using (var aendern = new MedikamentFenster(u.Speicher, u.Speicher.Medikamente()[0]))
            Hilfe.Foto(aendern, "24_medikament_aendern");
        f.Navigation.Single(n => n.Text == "Nachrichten").Ausloesen();
        Hilfe.Foto(f, "20_nachrichten");
        f.Nachrichten.Karten[0].Waehlen();
        Hilfe.Foto(f, "21_verlauf");
        using (var neu = new NachrichtFenster(u.Speicher, u.Speicher.Kollege("AB")!, u.Demo("Distel").Id, null))
        {
            neu.Ankreuzen("Team Therapie");
            neu.Ankreuzen("Rudi Specht (RS)");
            neu.Betreff.Text = "Gartenprojekt";
            neu.Inhalt.Text = "Dieter möchte beim Gartenprojekt mitmachen. Gibt es noch freie Plätze?";
            Hilfe.Foto(neu, "22_neue_nachricht");
        }
        f.BereichWaehlen("Bewohner");
        f.Bewohner.FilterSetzen(BewohnerFilter.NichtErfasst);
        Hilfe.Foto(f, "12_filter_nicht_erfasst");
        f.Bewohner.FilterSetzen(BewohnerFilter.Alle);
        using (var einstellungen = new EinstellungenFenster(u.Speicher.Kollegen()))
            Hilfe.Foto(einstellungen, "11_einstellungen");
        using (var fortschritt = new Fortschrittsfenster())
        {
            fortschritt.Zeige("Neue Version wird geladen (Build 4 → Build 5) …", 0.42);
            Hilfe.Foto(fortschritt, "7_update");
        }
    });
}
