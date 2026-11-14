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
        // Die kurzen Einträge bestätigter Maßnahmen stehen je Tag gesammelt unter den geschriebenen Berichten.
        var zeilen = d.Zeilen.Where(z => z.Bericht.Art != MassnahmenRegeln.ArtBericht).ToList();
        Assert.True(zeilen.Count >= 28);
        Assert.Equal(zeilen.Select(z => z.Bericht.Zeitpunkt).OrderDescending(),
            zeilen.Select(z => z.Bericht.Zeitpunkt));
        int alle = d.Zeilen.Count;

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
    public void Abschnitte_nach_Schicht_laufende_oben_nur_ungelesene() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Uebergabe;
            Assert.Same(seite, f.Seite("Übergabe"));
            Assert.Equal("Übergabe", f.Kopf.TitelText);
            Assert.Equal($"R1 · {seite.KopfText}", f.Kopf.KleinText);   // vorn der gewählte Bereich
            Assert.Contains(f.Kopf.Knoepfe, k => k == seite.NotizKnopf);
            Assert.Contains(f.Kopf.Knoepfe, k => k == seite.DruckKnopf);
            Assert.Equal("Wohnbereich allgemein", seite.Abschnitte[0].Titel);
            Assert.True(seite.Abschnitte[0].Eingeklappt);
            Assert.Equal(UebergabeRegeln.SchichtFolge(DateTime.Now).Select(BerichtRegeln.SchichtText),
                seite.Abschnitte.Skip(1).Select(a => a.Titel));
            foreach (var a in seite.Abschnitte.Skip(1))
            {
                Assert.All(a.Zeilen, z => Assert.Equal(a.Titel, BerichtRegeln.SchichtText(UebergabeRegeln.SchichtVon(z.Uebergabe))));
                Assert.All(a.Zeilen, z => Assert.Equal(a.Farbe, z.Farbe));   // jede Zeile in der Farbe ihrer Schicht
                for (int i = 1; i < a.Zeilen.Count; i++)   // ein Bewohner: Name nur beim ersten Eintrag
                    if (a.Zeilen[i].Uebergabe.BewohnerId == a.Zeilen[i - 1].Uebergabe.BewohnerId) Assert.Equal("", a.Zeilen[i].NameText.Text);
            }
            // Vorgabe „nur ungelesene“; CD hat noch nichts gelesen, also alles.
            Assert.True(seite.NurUngelesene.Checked);
            Assert.Equal(seite.Offen, seite.Ungelesen);
            Assert.Equal(seite.Ungelesen, seite.Zeilen.Count);
            Assert.Equal($"nur ungelesene ({seite.Ungelesen})", seite.NurUngelesene.Text);
            Assert.Equal(seite.Offen, seite.Neu);   // erster Besuch mit diesem Kürzel: alles neu
            Assert.All(seite.Zeilen, z => Assert.Contains("neu", z.MarkenTexte));
            Assert.All(seite.Zeilen.Where(z => !z.Aufgeklappt && !z.Uebergabe.Allgemein), z => Assert.True(z.Height <= Stil.Px(z, 32)));   // kompakt
            Assert.False(seite.KuerzelHinweis.Visible);
            Hilfe.Foto(f, "15b_uebergabe_schichten");

            // Kurzer Übergabe-Satz mit Bericht dahinter: Klick klappt den Bericht in der Zeile auf.
            var satz = seite.Zeilen.First(z => z.Uebergabe.Text.Length > 0 && z.Uebergabe.Bericht is not null);
            satz.Umschalten();
            Assert.True(satz.Aufgeklappt);
            Assert.Contains("Bericht: ", satz.TextFeld.Text);
            Assert.True(satz.Verlaengern.Visible);
            seite.Abschnitte[0].Umschalten();
            Assert.False(seite.Abschnitte[0].Eingeklappt);

            // Zweiter Besuch: Was vorher da war, ist nicht mehr neu.
            f.BereichWaehlen("Dokumentation");
            f.BereichWaehlen("Übergabe");
            Assert.Equal(0, seite.Neu);
            Assert.DoesNotContain(seite.Zeilen, z => z.MarkenTexte.Contains("neu"));
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
            seite.AlleOffenen.Checked = true;
            Assert.Equal(seite.Offen, seite.Zeilen.Count);
            long id = seite.Zeilen.First(z => !z.Uebergabe.Allgemein && z.Uebergabe.GiltBis is not null).Uebergabe.Id;
            UebergabeZeile Zeile() => seite.Zeilen.Single(z => z.Uebergabe.Id == id);

            int ungelesen = seite.Ungelesen;
            Zeile().Gelesen.PerformClick();
            Assert.Equal(ungelesen - 1, seite.Ungelesen);
            Assert.Contains("CD (", Zeile().GelesenText.Text);
            Assert.Equal("Gelesen ✓", Zeile().Gelesen.Text);
            Assert.False(Zeile().Gelesen.Enabled);
            seite.NurUngelesene.Checked = true;
            Assert.DoesNotContain(seite.Zeilen, z => z.Uebergabe.Id == id);   // gelesen: weg aus „nur ungelesene“
            seite.AlleOffenen.Checked = true;

            Zeile().Umschalten();
            using (new DialogErsatz(fenster =>
            {
                var dauer = Assert.IsType<DauerFenster>(fenster);
                dauer.Dauer.SelectedIndex = (int)UebergabeDauer.BisErledigt;
                return dauer.Uebernehmen() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Zeile().Verlaengern.PerformClick();
            Assert.Null(Zeile().Uebergabe.GiltBis);
            Assert.StartsWith("seit ", Zeile().Gueltig.Text);
            Assert.EndsWith("(verlängert)", Zeile().Gueltig.Text);

            int offen = seite.Offen;
            Zeile().Erledigt.PerformClick();
            Assert.Equal(offen - 1, seite.Offen);
            Assert.DoesNotContain(seite.Zeilen, z => z.Uebergabe.Id == id);
            Assert.Equal($"R1 · {seite.KopfText}", f.Kopf.KleinText);   // Kopf zählt mit

            seite.ErledigteZeigen.Checked = true;
            Assert.Equal("Erledigt / abgelaufen", seite.Abschnitte[^1].Titel);
            var erledigt = Zeile();
            Assert.StartsWith("erledigt ", erledigt.Gueltig.Text);
            Assert.EndsWith("· CD", erledigt.Gueltig.Text);
            Assert.False(erledigt.Erledigt.Visible);
            Assert.Contains(seite.Zeilen, z => z.Gueltig.Text.StartsWith("abgelaufen "));   // Demo: ein abgelaufener Eintrag
        }
    });

    [Fact]
    public void Alle_in_einem_Abschnitt_gelesen_mit_Rueckfrage() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Uebergabe;
            var abschnitt = seite.Abschnitte.Skip(1).First(a => a.Zeilen.Count > 0);
            int n = abschnitt.Zeilen.Count, ungelesen = seite.Ungelesen;
            string? frage = null;
            seite.Rueckfrage = t => { frage = t; return false; };
            abschnitt.AlleGelesen.PerformClick();
            Assert.Equal(n == 1 ? "1 Eintrag als gelesen bestätigen?" : $"{n} Einträge als gelesen bestätigen?", frage);
            Assert.Equal(ungelesen, seite.Ungelesen);   // „Nein“: nichts geändert

            seite.Rueckfrage = _ => true;
            abschnitt.AlleGelesen.PerformClick();
            Assert.Equal(ungelesen - n, seite.Ungelesen);
            Assert.Empty(seite.Abschnitte.Single(a => a.Titel == abschnitt.Titel).Zeilen);
            Assert.All(u.Speicher.Uebergaben(DateTime.Now).Where(x => abschnitt.Zeilen.Any(z => z.Uebergabe.Id == x.Id)),
                x => Assert.True(UebergabeRegeln.GelesenVon(x, "CD")));
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
            var zeile = seite.Abschnitte[0].Zeilen.Single(z => z.Uebergabe.Text == "Heizungsableser kommt morgen zwischen 8 und 12 Uhr.");
            Assert.True(zeile.Uebergabe.Allgemein);
            Assert.Equal("CD", zeile.Uebergabe.Verfasser);
            Assert.Equal("CD", zeile.Kuerzel.Text);
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
            Assert.All(seite.Zeilen, z => Assert.False(z.Gelesen.Enabled || z.Erledigt.Enabled || z.Verlaengern.Enabled));
            Assert.All(seite.Abschnitte, a => Assert.False(a.AlleGelesen.Enabled));
        }
    });

    [Fact]
    public void Uebergabeliste_nach_Schicht_auf_einer_Seite() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                f.Uebergabe.DruckKnopf.PerformClick();
                Assert.StartsWith("Übergabeliste", Assert.Single(ersatz.Titel));
            }
            var jetzt = DateTime.Now;
            var offen = u.Speicher.Uebergaben(jetzt);
            var druck = new UebergabeDruck(offen, u.Speicher.AlleBewohner().ToDictionary(b => b.Id!.Value), jetzt);
            var abschnitte = druck.Zeilen.Select(z => z.Abschnitt).Distinct().ToList();
            Assert.Equal("Wohnbereich allgemein", abschnitte[0]);
            Assert.Equal(UebergabeRegeln.SchichtFolge(jetzt).Select(BerichtRegeln.SchichtText).Where(abschnitte.Contains), abschnitte.Skip(1));
            using var bild = new Bitmap(800, 1130);
            using var g = Graphics.FromImage(bild);
            Assert.Equal(offen.Count, druck.SeiteZeichnen(g, 0));   // passt auf eine Seite
        }
    });
}
