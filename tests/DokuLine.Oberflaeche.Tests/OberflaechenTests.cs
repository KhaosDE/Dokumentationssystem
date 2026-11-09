using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

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
        Assert.Equal(["Übersicht", "Tagesplan", "Dokumentation", "Übergabe", "Visiten", "Aufgaben", "Nachrichten", "Kalender", "Therapie",
                      "Laufende Therapien", "Angebote", "Wochenpläne", "Ausgangsliste", "Einstellungen"],
            fenster.Navigation.Select(n => n.Text));
        Assert.All(Hauptfenster.TherapieBereiche, b => Assert.True(fenster.Ausgeblendet(b)));   // nur im Bereich Therapie (29n)
        Assert.Equal("Dokumentation", fenster.AktiverBereich);
        Assert.True(fenster.Navigation.Single(n => n.Text == "Dokumentation").Aktiv);
        Assert.Matches(@"^Version \d+\.\d+\.\d+ · ", fenster.VersionText.Text);
    });

    [Fact]
    public void Einstellungen_Benutzer_Update_und_Logo() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var fenster = new Hauptfenster(u.Speicher);
        string logo = Path.Combine(u.Ordner, "eigenes-bild.png");
        Hilfe.Testbild(logo, 120, 40);
        Assert.False(fenster.Kopf.HatLogo);
        fenster.BenutzerWaehlen("EF");
        Hilfe.Zeigen(fenster);
        // Schritt 29: eine ganze Seite, „Einstellungen“ fest unten in der Seitenleiste; Änderungen gelten sofort.
        fenster.Navigation.Single(n => n.Text == "Einstellungen").Ausloesen();
        Assert.Equal("Einstellungen", fenster.AktiverBereich);
        var einstellungen = fenster.Einstellungsseite;
        Assert.Equal("Persönlich", einstellungen.AktiveGruppe);
        Assert.Equal(EinstellungenSeite.GruppenListe.Select(g => g.Name), einstellungen.Gruppen.Select(g => g.Text));
        Assert.Equal("Emma Fink (EF)", einstellungen.Angemeldet.Text);
        Assert.True(einstellungen.AutoUpdate.Checked);
        Assert.False(einstellungen.HatLogo);
        einstellungen.AutoUpdate.Checked = false;
        Assert.True(einstellungen.LogoSetzen(logo));
        var werte = Einstellungen.Laden();
        Assert.Equal(("EF", false), (werte.Kuerzel, werte.AutoUpdate));
        Assert.Equal("Angemeldet: EF – Emma Fink", fenster.Kopf.BenutzerText);
        Assert.NotNull(Einstellungen.LogoPfad());
        Assert.True(fenster.Kopf.HatLogo);

        // Suche über Name und Erklärung; springt zur Gruppe.
        Assert.Contains(einstellungen.Suchen("Logo"), k => k.Titel.StartsWith("Bild im Kopf"));
        Assert.Contains(einstellungen.Suchen("Nachtdienst"), k => k.Titel == "Standardaufgaben");
        Assert.True(einstellungen.Springen("Logo"));
        Assert.Equal("Wohnbereich", einstellungen.AktiveGruppe);
        Assert.True(einstellungen.Karten.Single(k => k.Titel.StartsWith("Bild im Kopf")).Hervorgehoben);
        einstellungen.LogoWeg.PerformClick();
        Assert.Null(Einstellungen.LogoPfad());
        Assert.False(fenster.Kopf.HatLogo);
        einstellungen.Suche.Text = "Schicht";
        Assert.True(einstellungen.Treffer.Visible);
        einstellungen.Suche.Text = "";

        // Ohne Recht ausgeblendet (CD: kein Recht „Einstellungen des Bereichs“); übernehmen geht auch nicht.
        fenster.BenutzerWaehlen("CD");
        var schichtzeiten = einstellungen.Karten.Single(k => k.Titel == "Schichtzeiten");
        Assert.Equal(RechteRegeln.FehltText, schichtzeiten.Sperrgrund);
        Assert.True(schichtzeiten.OhneRecht);
        Assert.DoesNotContain(schichtzeiten, einstellungen.Suchen("Schicht"));
        einstellungen.Bereich.Text = "R2";
        Assert.False(einstellungen.BereichUebernehmen());
        Assert.Equal("R1", Einstellungen.Laden().Bereich);
        Assert.Equal(RechteRegeln.FehltText, einstellungen.Fehler.Text);

        // Bildschirmfoto je Gruppe (Administrator: alle Gruppen).
        fenster.BenutzerWaehlen("TST");
        for (int i = 0; i < EinstellungenSeite.GruppenListe.Length; i++)
        {
            einstellungen.Gruppen[i].Ausloesen();
            Assert.Equal(EinstellungenSeite.GruppenListe[i].Name, einstellungen.AktiveGruppe);
            Hilfe.Foto(fenster, $"92_einstellungen_{i:00}");
        }
    });

    [Fact]
    public void Selbsttest() => Hilfe.Sta(() =>
    {
        string? vorher = Environment.GetEnvironmentVariable(Pfade.Umlenkung);
        Assert.Equal(0, DokuLine.Selbsttest.Ausfuehren());
        Assert.Equal(vorher, Environment.GetEnvironmentVariable(Pfade.Umlenkung));
    });
}

[Collection("Oberflaeche")]
public class BewohnerOberflaechenTests
{
    // Marken, Aufenthalt und Doppelklick zur Akte stehen in der Übersicht.
    private static List<BewohnerZeile> Karten(Hauptfenster f)
    {
        f.BereichWaehlen("Übersicht");
        return f.Uebersicht.Zeilen;
    }

    [Fact]
    public void Karten_zeigen_Marken_und_Aufenthalt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        f.Uebersicht.Jetzt = () => new DateTime(2026, 11, 7, 12, 0, 0);
        f.BereichWaehlen("Übersicht");
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);   // Ginster wohnt in R2
        var birke = f.Uebersicht.Zeilen.Single(k => k.Bewohner.Nachname == "Birke");
        // keine bekannt, kein Beschluss: Normalfall ohne Marke dazu
        Assert.DoesNotContain(birke.Marken, m => m.Art is MarkenArt.Beschluss or MarkenArt.Allergie or MarkenArt.NichtErfasst);
        Assert.Equal("seit 15.02.2016 · 10 Jahre, 8 Monate", birke.AufenthaltText);
        var ginster = f.Uebersicht.Zeilen.Single(k => k.Bewohner.Nachname == "Ginster");
        Assert.Equal(["Allergien nicht erfasst", "Beschluss nicht erfasst"],
            ginster.Marken.Where(m => m.Art == MarkenArt.NichtErfasst && !m.Text.StartsWith("überfällig")).Select(m => m.Text));
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
    public void Akte_oeffnen_und_zurueck_zur_Dokumentation() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Karten(f).Single(k => k.Bewohner.Nachname == "Birke").Waehlen();
        Assert.Equal("Dokumentation", f.AktiverBereich);
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Bewohnerakte", f.Kopf.TitelText);
        Assert.DoesNotContain("Berta Birke", f.Kopf.KleinText);
        Assert.Equal("Berta Birke", f.Bewohner.Akte.Bewohner!.Name);
        Assert.Equal("22.11.1958", f.Bewohner.Akte.Geburtsdatum.Text);
        Assert.Equal(["← Dokumentation", "Berichte ansehen", "Nachrichten …", "Status …", "Ärztliche Informationen …", "Neuer Bericht …",
            "Bearbeiten …"], f.Kopf.Knoepfe.Select(k => k.Text));
        f.Kopf.Knoepfe[0].PerformClick();
        Assert.False(f.Bewohner.AkteSichtbar);
        Assert.Equal("Berta Birke", f.Bewohner.Gewaehlt!.Name);   // die Wahl bleibt
        f.Bewohner.AkteOeffnen();
        Assert.True(f.Bewohner.AkteSichtbar);
        f.Navigation.Single(n => n.Text == "Dokumentation").Ausloesen();
        Assert.False(f.Bewohner.AkteSichtbar);   // Klick auf „Dokumentation“ führt zur Liste
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
        Assert.Equal(["Stammdaten", "Medikamentenplan", "Wochenplan", "Ausgang", "Biografie", "Reflexion", "HEB"], akte.Reiter.Namen);
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
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Recht „Bewohner anlegen“ (Schritt 19)
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.FormularZeigen = formular =>
        {
            Assert.Equal("nicht_erfasst", formular.Beschluss.Zustand);   // Allergien: im Fenster „Ärztliche Informationen“ (Schritt 26f)
            formular.Vorname.Text = "Paul";
            formular.Nachname.Text = "Pappel";
            Assert.Equal(["105 – Bett Tür", "106 – Bett Tür"], formular.Bett.Items.Cast<string>());   // nur freie Betten (Schritt 26e)
            Assert.True(formular.BettWaehlen("106 – Bett Tür"));
            formular.Geburtsdatum.Text = "01.01.1990";
            formular.Aufnahmedatum.Text = "01.01.1989";
            Assert.False(formular.Speichern());
            Assert.Contains("Aufnahmedatum liegt vor dem Geburtsdatum", formular.Fehler.Text);
            Assert.Equal(69, u.Speicher.AlleBewohner().Count);
            formular.Aufnahmedatum.Text = "1.2.2026";
            Assert.True(formular.Speichern());
            return formular.Gespeichert;
        };
        f.BereichWaehlen("Übersicht");   // „Neuer Bewohner …“ steht nur in der Übersicht
        f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bewohner …").PerformClick();
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Bewohnerakte", f.Kopf.TitelText);
        Assert.Equal("Paul Pappel", f.Bewohner.Akte.Bewohner!.Name);
        Assert.Equal(("106", ZimmerRegeln.Tuer, OffenGefuehrtZustand.Nein), (f.Bewohner.Akte.Bewohner.Zimmer, f.Bewohner.Akte.Bewohner.Bett, f.Bewohner.Akte.Bewohner.OffenGefuehrt));
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
        Assert.StartsWith("104 – Einzelzimmer (bisher)", (string)formular.Bett.Items[0]!);
        Assert.True(formular.BereichWaehlen("R2"));   // Zimmerwechsel: dieselbe Auswahl freier Betten
        Assert.True(formular.BettWaehlen("204 – Einzelzimmer"));
        Assert.True(formular.Speichern());
        var gespeichert = u.Speicher.Bewohner(b.Id!.Value)!;
        Assert.Equal(("204", "R2"), (gespeichert.Zimmer, gespeichert.Wohnbereich));
        Assert.Equal((AllergieZustand.Eingetragen, "Wespen- und Bienenstiche"), (gespeichert.Allergie, gespeichert.Allergien));   // Formular lässt Allergien unverändert
        Assert.NotNull(Bilder.Pfad(gespeichert.Bild));
    });

    [Fact]
    public void Dreiwahl_kennt_nie_eingetragen_als_Rueckfall() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var fenster = new AerztlichFenster(u.Speicher, u.Demo("Birke"));
        fenster.Allergie.Setzen("unbekannt", "");
        Assert.Equal("nicht_erfasst", fenster.Allergie.Zustand);
        foreach (var knopf in fenster.Allergie.Knoepfe) knopf.Checked = false;
        Assert.Equal("nicht_erfasst", fenster.Allergie.Zustand);
    });

    [Fact]
    public void Demo_Daten_zuruecksetzen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        var eiche = BewohnerRegeln.AlsEingabe(u.Demo("Eiche"));
        eiche.Zimmer = "999";
        eiche.Bett = "";
        u.Speicher.BewohnerSpeichern(BewohnerRegeln.AusEingabe(eiche, u.Demo("Eiche").Id));
        f.Bewohner.AkteZeigen(u.Demo("Eiche").Id!.Value);
        Assert.True(f.DemoZuruecksetzen(nachfragen: false));
        Assert.False(f.Bewohner.AkteSichtbar);
        Assert.Equal("105", u.Demo("Eiche").Zimmer);
        Assert.Equal(23, Karten(f).Count);   // eigener Bereich R1
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
            formular.Geburtsdatum.Text = "01.01.1990";
            formular.Aufnahmedatum.Text = "01.01.1989";
            formular.Speichern();
            Hilfe.Foto(formular, "6_formular_fehler");
        }
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.DokumentationZeigen(u.Demo("Ahorn").Id!.Value);
        Hilfe.Foto(f, "8_dokumentation_berichte");
        f.Bewohner.Dokumentation.FilterSetzen(new DokuFilter { Wichtig = true });
        Hilfe.Foto(f, "13_dokumentation_nur_wichtige");
        f.Bewohner.Dokumentation.FilterSetzen(new DokuFilter());
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
                u.Speicher.AlleBewohner().ToDictionary(b => b.Id!.Value), DateTime.Now).SeiteZeichnen(g, 0);
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
        f.BereichWaehlen("Dokumentation");
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
        var db = new DokuLine.Modul.Ausgangsliste.Datenbank(u.Speicher);
        using (var ausgang = new DokuLine.Modul.Ausgangsliste.AusgangFenster(db, db.Platz(u.Demo("Ahorn").Id!.Value)!))
            Hilfe.Foto(ausgang, "42_ausgang_fenster");
        using (var status = new DokuLine.Modul.Ausgangsliste.StatusFenster(db, db.Platz(u.Demo("Birke").Id!.Value)!))
            Hilfe.Foto(status, "43_anwesenheit_offen_gefuehrt");
        using (var kueche = new DokuLine.Modul.Ausgangsliste.KuechendienstFenster(db))
            Hilfe.Foto(kueche, "44_kuechendienst");
        using (var nacht = new DokuLine.Modul.Ausgangsliste.NachtberichtFenster(db.Laden(), db.AusgaengeLaden(DateTime.Now)))
            Hilfe.Foto(nacht, "45_nachtbericht");
        f.EinstellungenOeffnen("Module");
        Hilfe.Foto(f, "46_einstellungen_module");
        f.Navigation.Single(n => n.Text == "Übersicht").Ausloesen();
        Hilfe.Foto(f, "9_uebersicht");
        string logo = Path.Combine(u.Ordner, "eigenes-bild.png");
        Hilfe.Testbild(logo, 180, 60);
        Einstellungen.LogoSetzen(logo);
        f.Kopf.LogoSetzen(Einstellungen.LogoPfad());
        f.BenutzerWaehlen("AB");
        f.BereichWaehlen("Dokumentation");
        Hilfe.Foto(f, "10_kopf_mit_bild");
        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
        Hilfe.Foto(f, "25_medikation_stufenplan");
        f.Bewohner.AkteZeigen(u.Demo("Clematis").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
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
        f.BereichWaehlen("Dokumentation");
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
            new WochenplanDruck(u.Demo("Ahorn"), u.Speicher.Regelwoche(u.Demo("Ahorn").Id!.Value, DateOnly.FromDateTime(DateTime.Today))).SeiteZeichnen(g);
            string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
            if (!string.IsNullOrWhiteSpace(ordner)) bild.Save(Path.Combine(ordner, "35_wochenplan_druck.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        f.Navigation.Single(n => n.Text == "Therapie").Ausloesen();
        Hilfe.Foto(f, "30_therapie");
        using (var angebot = new AngebotFenster(u.Speicher, u.Speicher.Angebote()[0]))
            Hilfe.Foto(angebot, "31_angebot_aendern");
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
        f.EinstellungenOeffnen("Persönlich");
        Hilfe.Foto(f, "11_einstellungen");
        f.BereichWaehlen("Dokumentation");
        using (var fortschritt = new Fortschrittsfenster())
        {
            fortschritt.Zeige("Neue Version wird geladen (Build 4 → Build 5) …", 0.42);
            Hilfe.Foto(fortschritt, "7_update");
        }
    });
}
