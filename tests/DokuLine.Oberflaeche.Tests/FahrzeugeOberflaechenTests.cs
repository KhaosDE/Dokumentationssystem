using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 24: Fahrzeuge im Kalender (bis Schritt 26l auch in der Übersicht) („vor Ort / unterwegs“, „losgefahren“,
// „zurück“), Fahrt eintragen mit Warnung bei Doppelbuchung, mitfahrende Bewohner abwesend und im
// Tagesplan; Kalender „Bereich“ und „Haus“ mit Umschalter und Recht.
[Collection("Oberflaeche")]
public class FahrzeugeOberflaechenTests
{
    private static string Heute(int tage = 0) => Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(tage)));

    [Fact]
    public void Fahrzeuge_unterwegs_zurueck_und_neue_Fahrt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Kalender");
        var leiste = f.Kalender.Fahrzeuge;   // seit Schritt 26l nur noch im Kalender
        FahrzeugZeile Zeile(string name) => leiste.Zeilen.Single(z => z.Stand.Fahrzeug == name);
        Assert.Equal(["VW Bus", "Caddy"], leiste.Zeilen.Select(z => z.Stand.Fahrzeug));
        Assert.StartsWith("unterwegs seit ", Zeile("Caddy").StandFeld.Text);
        Assert.True(Zeile("Caddy").ZurueckKnopf.Visible);
        Assert.StartsWith("vor Ort", Zeile("VW Bus").StandFeld.Text);
        long eiche = u.Demo("Eiche").Id!.Value;   // fährt in der Demo gerade mit
        BewohnerZeile Eiche() { f.Uebersicht.Aktualisieren(); return f.Uebersicht.Zeilen.Single(k => k.Bewohner.Id == eiche); }
        Assert.Contains(Eiche().Marken, m => m.Art == MarkenArt.Abwesend && m.Text.StartsWith("abwesend: unterwegs mit Caddy"));
        Assert.StartsWith("unterwegs mit Caddy bis ", Eiche().Wo);   // Spalte „Wo“ der Übersicht
        Hilfe.Foto(f, "77_kalender_fahrzeuge");

        Zeile("Caddy").ZurueckKnopf.PerformClick();
        Assert.StartsWith("vor Ort", Zeile("Caddy").StandFeld.Text);
        Assert.DoesNotContain(Eiche().Marken, m => m.Text.StartsWith("abwesend: unterwegs"));

        // Neue Fahrt, dann eine zweite zur selben Zeit: Warnung, erst „Trotzdem speichern“ speichert.
        using (new DialogErsatz(fenster =>
        {
            var neu = Assert.IsType<FahrtFenster>(fenster);
            neu.Fahrzeug.SelectedItem = "VW Bus";
            neu.Von.Text = "23:00";
            neu.Bis.Text = "23:30";
            neu.Zweck.SelectedItem = "Sporttherapie";
            neu.BewohnerWaehlen("Birke");
            Assert.False(neu.Speichern());   // Ziel fehlt
            Assert.Contains("Ziel fehlt.", neu.Fehler.Text);
            neu.Ziel.Text = "Hallenbad";
            Hilfe.Foto(neu, "78_fahrt_neu");
            return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            leiste.NeuKnopf.PerformClick();
        Assert.Contains("nächste Fahrt 23:00 (Sporttherapie)", Zeile("VW Bus").StandFeld.Text);
        using (var zweite = new FahrtFenster(u.Speicher))
        {
            zweite.Fahrzeug.SelectedItem = "VW Bus";
            zweite.Von.Text = "23:10";
            zweite.Bis.Text = "23:20";
            zweite.Ziel.Text = "Apotheke";
            Assert.False(zweite.Speichern());
            Assert.StartsWith("Achtung, Doppelbuchung", zweite.Warnung.Text);
            Assert.Equal("Trotzdem speichern", zweite.SpeichernKnopf.Text);
            Assert.True(zweite.Speichern());
        }

        // Losgefahren: Birke ist abwesend und die Fahrt steht in ihrem Tagesplan.
        f.BereichWaehlen("Kalender");
        Zeile("VW Bus").LosKnopf.PerformClick();
        Assert.StartsWith("unterwegs seit ", Zeile("VW Bus").StandFeld.Text);
        long birke = u.Demo("Birke").Id!.Value;
        f.Uebersicht.Aktualisieren();
        Assert.Contains(f.Uebersicht.Zeilen.Single(k => k.Bewohner.Id == birke).Marken, m => m.Text.StartsWith("abwesend: unterwegs mit VW Bus"));
        f.BereichWaehlen("Tagesplan");
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);
        Assert.Contains(f.Tagesplan.Tafel.Zeilen, k => k.Daten.Bewohner.Id == birke && k.Daten.Termin.Quelle == TerminQuelle.Fahrt
                                                      && k.Daten.Termin.Begleitung.StartsWith("VW Bus · Fahrer ", StringComparison.Ordinal));   // Spalte „Begleitung“ (26g)

        // Ohne Anmeldung: Knöpfe gesperrt.
        f.BenutzerWaehlen("");
        f.BereichWaehlen("Kalender");
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(f.Kalender.Fahrzeuge.NeuKnopf));
    });

    [Fact]
    public void Kalender_Bereich_und_Haus() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Mitarbeiter: Haus-Termine nur ansehen
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Kalender");
        var k = f.Kalender;
        Assert.Equal(2, k.Fahrzeuge.Zeilen.Count);
        k.TagWaehlen(DateOnly.FromDateTime(DateTime.Today.AddDays(4)));
        k.AnsichtWaehlen(KalenderAnsicht.Tag);
        KalenderKarte Brand() => k.Karten.Single(x => x.Termin.Aktuell.Titel == "Brandschutzübung");
        Assert.Equal("Kalender Haus", Brand().MarkenTexte[0]);
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(Brand().AendernKnopf));
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(Brand().AbsagenKnopf));
        k.Kalenderwahl.SelectedIndex = (int)KalenderWahl.Bereich;
        Assert.DoesNotContain(k.Karten, x => x.Termin.Aktuell.Haus);
        k.Kalenderwahl.SelectedIndex = (int)KalenderWahl.Haus;
        Assert.All(k.Termine(), x => Assert.True(x.Aktuell.Haus));
        Assert.NotEmpty(k.Karten);
        using (var neu = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today), null))
            Assert.False(neu.HausWahl.Enabled);

        // Bereichsleitung: Haus-Termine bearbeiten, neue anlegen.
        f.BenutzerWaehlen("EF");
        k.Kalenderwahl.SelectedIndex = (int)KalenderWahl.Beide;
        Assert.Null(Stil.Sperrgrund(Brand().AendernKnopf));
        using (var neu = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today.AddDays(4)), null))
        {
            Assert.True(neu.HausWahl.Enabled);
            neu.Arten[(int)KalenderArt.Wohnbereich].Checked = true;
            neu.HausWahl.Checked = true;
            neu.Titel.Text = "Sommerfest";
            neu.BeginnUhrzeit.Text = "15:00";
            neu.EndeUhrzeit.Text = "18:00";
            if (!neu.Speichern()) Assert.Fail(neu.Fehler.Text);
            Assert.True(neu.Gespeichert!.Aktuell.Haus);
        }
        k.Aktualisieren();
        Assert.Contains(k.Karten, x => x.Termin.Aktuell.Titel == "Sommerfest" && x.MarkenTexte[0] == "Kalender Haus");
        k.AnsichtWaehlen(KalenderAnsicht.Monat);
        Hilfe.Foto(f, "79_kalender_haus");

        // Fahrzeugliste in den Einstellungen.
        using var fenster = new FahrzeugeFenster(u.Speicher);
        fenster.Name_.Text = "Kleinbus";
        Assert.True(fenster.Hinzufuegen());
        Assert.Contains("Kleinbus", fenster.Liste.Items.Cast<string>());
        fenster.Liste.SelectedIndex = fenster.Liste.Items.Count - 1;
        Assert.True(fenster.Umschalten());
        Assert.Contains("Kleinbus (stillgelegt)", fenster.Liste.Items.Cast<string>());
    });
}
