using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Medikamentendatenbank (seit Schritt 21 in den Einstellungen): Liste mit Suche, Demo-Hinweis, anlegen
// mit eigener Prüfung (Quelle, Prüfdatum), ändern mit Verlauf, sperren, Quelle kopieren.
[Collection("Oberflaeche")]
public class MedikamenteOberflaechenTests
{
    // PR: Psychologischer Fachdienst, Fachkraft – darf anlegen und verwalten (Rechte, Schritt 19).
    private static (Umgebung, MedikamenteFenster) Oeffnen(string kuerzel = "PR")
    {
        var u = new Umgebung();
        if (kuerzel.Length > 0) Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new MedikamenteFenster(u.Speicher);
        Hilfe.Zeigen(f);
        return (u, f);
    }

    [Fact]
    public void Nicht_mehr_in_der_Seitenleiste_sondern_in_den_Einstellungen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "PR" });
        using var f = new Hauptfenster(u.Speicher);
        Assert.DoesNotContain(f.Navigation, n => n.Text == "Medikamente");
        using var einstellungen = new EinstellungenFenster(u.Speicher.Kollegen(), u.Speicher);
        Hilfe.Zeigen(einstellungen);
        using (var ersatz = new DialogErsatz(fenster =>
        {
            var db = Assert.IsType<MedikamenteFenster>(fenster);
            Assert.NotEmpty(db.Seite.Karten);
            Hilfe.Foto(db, "23_medikamente");
            return DialogResult.Cancel;
        }))
        {
            einstellungen.MedikamenteKnopf.PerformClick();
            Assert.StartsWith("Medikamentendatenbank", Assert.Single(ersatz.Titel));
        }
    });

    [Fact]
    public void Liste_mit_Demo_Hinweis_und_Suche() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Seite;
            int alle = u.Speicher.Medikamente().Count;
            Assert.Equal($"Medikamentendatenbank – {alle} Medikamente · Demo-Bestand ({Stil.DemoHinweis})", f.Text);
            Assert.Equal("Demo-Bestand, nicht zum Verordnen verwenden", seite.Hinweis.Text);
            Assert.Equal("Neues Medikament …", seite.NeuKnopf.Text);
            Assert.Equal(alle, seite.Karten.Count);
            Assert.All(seite.Karten, k => Assert.Equal("ungeprüft", k.MarkenTexte[0]));
            string wirkstoff = seite.Karten[0].Medikament.Wirkstoff;
            seite.Suche.Text = wirkstoff;
            Assert.All(seite.Karten, k => Assert.Contains(wirkstoff, k.Medikament.Anzeige, StringComparison.CurrentCultureIgnoreCase));
            seite.Suche.Text = "gibt-es-nicht";
            Assert.Empty(seite.Karten);
            Assert.StartsWith($"Medikamentendatenbank – {alle} Medikamente", f.Text);   // zählt immer alle

            string? kopiert = null;
            seite.Kopieren = text => kopiert = text;
            seite.Suche.Text = "";
            seite.Karten[0].KopierenKnopf.PerformClick();
            Assert.StartsWith("https://", kopiert);
        }
    });

    [Fact]
    public void Anlegen_pruefen_aendern_sperren_mit_Verlauf() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Seite;
            string? geoeffnet = null;
            var vorher = Extern.Oeffnen;
            var vorherAblage = Nachschlageansicht.Zwischenablage;
            Extern.Oeffnen = adresse => geoeffnet = adresse;
            Nachschlageansicht.Zwischenablage = _ => { };
            try
            {
                using (new DialogErsatz(fenster =>
                {
                    var neu = Assert.IsType<MedikamentFenster>(fenster);
                    Assert.False(neu.Speichern());
                    Assert.Contains("Wirkstoff fehlt.", neu.Fehler.Text);
                    neu.Wirkstoff.Text = "Beispielin";
                    neu.Darreichungsform.Text = "Tablette";
                    neu.Staerken.Text = "1 mg";
                    neu.Nachsehen();   // „Im Browser öffnen“: Übergabe an den Browser, kein eigener Netzzugriff
                    Assert.False(neu.GeprueftAm.Enabled);
                    neu.Bestaetigt.Checked = true;   // selbst geprüft: Prüfdatum ist heute vorbelegt
                    Assert.True(neu.GeprueftAm.Enabled);
                    Assert.Equal(Datum.Text(DateOnly.FromDateTime(DateTime.Today)), neu.GeprueftAm.Text);
                    Assert.False(neu.Speichern());
                    Assert.Contains("„Bestätigt“ geht nur mit Quelle.", neu.Fehler.Text);
                    neu.Quelle.Text = "https://example.org/fachinformation-beispielin";
                    neu.GeprueftAm.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(1)));
                    Assert.False(neu.Speichern());
                    Assert.Contains("in der Zukunft", neu.Fehler.Text);
                    neu.GeprueftAm.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Today));
                    return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                }))
                    seite.NeuKnopf.PerformClick();
            }
            finally
            {
                Extern.Oeffnen = vorher;
                Nachschlageansicht.Zwischenablage = vorherAblage;
            }
            Assert.Equal(ExterneAdressen.Arzneimittelsuche, geoeffnet);
            MedikamentKarte Karte() => seite.Karten.Single(k => k.Medikament.Wirkstoff == "Beispielin");
            Assert.Equal($"geprüft am {Datum.Text(DateOnly.FromDateTime(DateTime.Today))} von PR", Karte().MarkenTexte[0]);
            Assert.Contains("selbst angelegt", Karte().MarkenTexte);
            Assert.Contains("Stärken: keine Angabe", new MedikamentKarte(Karte().Medikament with
            {
                Fassungen = [Karte().Medikament.Aktuell with { Staerken = [] }],
            }).MarkenTexte);
            Assert.False(Karte().VerlaufKnopf.Visible);

            using (new DialogErsatz(fenster =>
            {
                var aendern = Assert.IsType<MedikamentFenster>(fenster);
                Assert.Equal("Beispielin", aendern.Wirkstoff.Text);
                aendern.Staerken.Text = "1 mg, 2 mg";
                return aendern.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().AendernKnopf.PerformClick();
            Assert.Contains("geändert (2 Fassungen)", Karte().MarkenTexte);
            Assert.True(Karte().VerlaufKnopf.Visible);

            string? verlauf = null;
            using (new DialogErsatz(fenster =>
            {
                verlauf = Assert.IsType<MedikamentVerlaufFenster>(fenster).Inhalt.Text;
                return DialogResult.Cancel;
            }))
                Karte().VerlaufKnopf.PerformClick();
            Assert.Contains("Fassung 2", verlauf);
            Assert.Contains("gilt jetzt", verlauf);
            Assert.Contains("Stärken: „1 mg“ → „1 mg, 2 mg“", verlauf);
            Assert.Contains("von PR", verlauf);

            // Sperren: nicht mehr wählbar beim Verordnen, bleibt im Verlauf; wieder freigeben geht.
            Karte().SperrenKnopf.PerformClick();
            Assert.Equal("gesperrt – nicht wählbar", Karte().MarkenTexte[0]);
            Assert.Equal("Wieder freigeben", Karte().SperrenKnopf.Text);
            using (var neu = new VerordnungFenster(u.Speicher, u.Demo("Ahorn")))
                Assert.DoesNotContain(neu.MedikamentWahl.Items.Cast<string>(), x => x.StartsWith("Beispielin"));
            Assert.Contains("gesperrt", MedikamentRegeln.Unterschiede(Karte().Medikament.Fassungen[^2], Karte().Medikament.Aktuell));
            Karte().SperrenKnopf.PerformClick();
            Assert.DoesNotContain("gesperrt – nicht wählbar", Karte().MarkenTexte);
            using (var neu = new VerordnungFenster(u.Speicher, u.Demo("Ahorn")))
                Assert.Contains(neu.MedikamentWahl.Items.Cast<string>(), x => x.StartsWith("Beispielin") && !x.EndsWith("ungeprüft"));
            Hilfe.Foto(f, "70_medikamente_geprueft");
        }
    });

    // Nachschlage-Ansicht des BfArM (Schritt 21): lädt erst auf Knopfdruck, nur mit dem Namen; markierten
    // Text ins gewählte Feld, Adresse als Quelle; ohne WebView2 ein Hinweis; in den Einstellungen abschaltbar.
    [Fact]
    public void Nachschlagen_beim_BfArM_und_in_Feld_uebernehmen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "PR" });
        var geladen = new List<Uri>();
        string? ablage = null, geoeffnet = null;
        var vorherLaden = Nachschlageansicht.LadenErsatz;
        var vorherAblage = Nachschlageansicht.Zwischenablage;
        var vorherOeffnen = Extern.Oeffnen;
        Nachschlageansicht.LadenErsatz = (_, adresse) => { geladen.Add(adresse); return Task.CompletedTask; };
        Nachschlageansicht.Zwischenablage = text => ablage = text;
        Extern.Oeffnen = adresse => geoeffnet = adresse;
        try
        {
            using (var fenster = new MedikamentFenster(u.Speicher, null))
            {
                var n = Assert.IsType<Nachschlageansicht>(fenster.Nachschlage);
                Assert.Equal(Nachschlageansicht.StartHinweis, n.Hinweis.Text);
                Assert.Empty(geladen);   // das Öffnen des Formulars lädt nichts
                Assert.False(n.UebernehmenKnopf.Enabled);
                Assert.Equal("→ Wirkstoff", n.Ziel.Text);
                fenster.Wirkstoff.Text = "Quetiapin (Retard)";
                n.NachschlagenAsync().GetAwaiter().GetResult();
                Assert.Equal([Nachschlagen.Start], geladen);
                Assert.Equal("Quetiapin Retard", ablage);   // nur der Name, zum Einfügen ins Suchfeld
                Assert.True(n.UebernehmenKnopf.Enabled);

                fenster.ZielWaehlen(fenster.Staerken);
                Assert.Equal("→ Stärken", n.Ziel.Text);
                Assert.True(n.Uebernehmen(" 25 mg\n"));
                Assert.True(n.Uebernehmen("100 mg"));
                Assert.True(n.Uebernehmen("25 mg"));   // schon da: nicht doppelt
                Assert.Equal("25 mg, 100 mg", fenster.Staerken.Text);
                fenster.ZielWaehlen(fenster.Darreichungsform);
                n.Uebernehmen("Filmtablette");
                n.Uebernehmen("Retardtablette");
                Assert.Equal("Retardtablette", fenster.Darreichungsform.Text);   // ersetzt
                Assert.False(n.Uebernehmen("   "));
                Assert.True(n.AlsQuelleUebernehmen());
                Assert.Equal(Nachschlagen.Start.AbsoluteUri, fenster.Quelle.Text);
                fenster.Zulassungsnummer.Text = "12345.00.00";
                n.ImBrowserOeffnen();
                Assert.Equal(Nachschlagen.Start.AbsoluteUri, geoeffnet);
                Hilfe.Foto(fenster, "71_medikament_nachschlagen");
                Assert.True(fenster.Speichern());
                Assert.Equal("12345.00.00", fenster.Gespeichert!.Aktuell.Zulassungsnummer);
            }

            // WebView2 fehlt: Hinweis statt Fehler, das Formular bleibt nutzbar.
            Nachschlageansicht.LadenErsatz = (_, _) => throw new InvalidOperationException("kein WebView2");
            using (var ohne = new MedikamentFenster(u.Speicher, null))
            {
                ohne.Nachschlage!.NachschlagenAsync().GetAwaiter().GetResult();
                Assert.Equal(Nachschlageansicht.NichtVerfuegbarText, ohne.Nachschlage.Hinweis.Text);
                Assert.False(ohne.Nachschlage.QuelleKnopf.Enabled);
                ohne.Wirkstoff.Text = "Musterol";
                Assert.True(ohne.Speichern());
            }

            // Abschaltbar (Einstellungen → Medikamente): dann nur „Im Browser öffnen“.
            using (var einstellungen = new EinstellungenFenster(u.Speicher.Kollegen(), u.Speicher))
            {
                Assert.True(einstellungen.NachschlageAnsicht.Checked);
                Assert.StartsWith("WebView2 ", einstellungen.WebAnsichtStand.Text);
                einstellungen.NachschlageAnsicht.Checked = false;
                Assert.True(einstellungen.Speichern());
            }
            Assert.False(Einstellungen.Laden().NachschlageAnsicht);
            using (var aus = new MedikamentFenster(u.Speicher, null))
            {
                Assert.Null(aus.Nachschlage);
                geoeffnet = null;
                aus.Nachsehen();
                Assert.Equal(ExterneAdressen.Arzneimittelsuche, geoeffnet);
            }
        }
        finally
        {
            Nachschlageansicht.LadenErsatz = vorherLaden;
            Nachschlageansicht.Zwischenablage = vorherAblage;
            Extern.Oeffnen = vorherOeffnen;
            Einstellungen.Aendern(w => w with { NachschlageAnsicht = true });
        }
    });

    [Fact]
    public void Ohne_Anmeldung_kein_Speichern() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen(kuerzel: "");
        using (u)
        using (f)
        {
            using var fenster = new MedikamentFenster(u.Speicher, null);
            fenster.Wirkstoff.Text = "Beispielin";
            Assert.False(fenster.Speichern());
            Assert.Contains("Kürzel fehlt", fenster.Fehler.Text);
        }
    });
}
