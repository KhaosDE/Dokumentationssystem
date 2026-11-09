using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bereich „Dokumentation“: links die Bewohner („Nachname, Vorname“, alphabetisch, Bereichswahl, Suche,
// einklappbar), rechts ihre Berichte nach Tagen mit Schichtrand, Marken, Filtern und gespeicherten Filtern.
[Collection("Oberflaeche")]
public class BewohnerListeTests
{
    private static (Umgebung, Hauptfenster) Oeffnen(string kuerzel)
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.WindowState = FormWindowState.Maximized;
        Application.DoEvents();
        f.BereichWaehlen("Dokumentation");
        return (u, f);
    }

    [Fact]
    public void Links_nur_Namen_rechts_Berichte_nach_Tagen() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen("EF");   // Bereichsleitung R1
        using (u)
        using (f)
        {
            var b = f.Bewohner;
            Assert.Contains(f.Navigation, n => n.Text == "Dokumentation");
            Assert.DoesNotContain(f.Navigation, n => n.Text == "Bewohner");
            Assert.Equal("Dokumentation", f.Kopf.TitelText);
            Assert.Equal(23, b.Zeilen.Count);   // eigener Bereich
            Assert.StartsWith("R1 · 23 Bewohner", f.Kopf.KleinText);
            Assert.Equal(["Neuer Bericht …", "Bedarf gegeben …", "Akte öffnen", "Ansicht: kompakt", "Drucken …"],
                f.Kopf.Knoepfe.Select(k => k.Text));
            // Nur „Nachname, Vorname“, alphabetisch; kein Eintrag „Alle Bewohner“.
            var namen = b.Namen.Items.Cast<string>().ToList();
            Assert.Equal(b.Zeilen.Select(z => $"{z.Nachname}, {z.Vorname}"), namen);
            Assert.Equal(namen.Order(StringComparer.CurrentCulture), namen);
            Assert.Equal("Ahorn, Anton", namen[0]);
            long ahorn = u.Demo("Ahorn").Id!.Value;
            Assert.Equal(ahorn, b.Gewaehlt!.Id);
            var d = b.Dokumentation;
            Assert.Equal(Zeitraum.SiebenTage, d.Filter.Zeitraum);   // Vorgabe 7 Tage
            Assert.NotEmpty(d.Zeilen);
            Assert.All(d.Zeilen, z => Assert.Equal(ahorn, z.Bericht.BewohnerId));
            Assert.Equal(d.Zeilen.Select(z => z.Bericht.Zeitpunkt).OrderDescending(),
                d.Zeilen.Select(z => z.Bericht.Zeitpunkt));
            Assert.All(d.Tage, t => Assert.Matches(@"^\w+, \d\d\.\d\d\.\d{4} · \d+ Berichte?$", t.Text));
            Assert.All(d.Zeilen, z => Assert.Equal(DokuRegeln.SchichtWort(z.Bericht.Schicht), z.SchichtWort.Text));
            Hilfe.Foto(f, "122_dokumentation");

            // Nächster Bewohner (wie Pfeil nach unten): die Berichte wechseln mit, der Filter bleibt.
            d.FilterSetzen(d.Filter with { Zeitraum = Zeitraum.DreissigTage });
            b.Namen.SelectedIndex = 1;
            Assert.Equal(b.Zeilen[1].Id, b.Gewaehlt!.Id);
            Assert.All(d.Zeilen, z => Assert.Equal(b.Zeilen[1].Id, z.Bericht.BewohnerId));
            Assert.Equal(Zeitraum.DreissigTage, d.Filter.Zeitraum);

            // Suche, Bereichswahl, einklappen.
            b.Suche.Text = "Tanne";
            Assert.Equal("Theo Tanne", Assert.Single(b.Zeilen).Name);
            Assert.StartsWith("R1 · 1 von 23 Bewohner", f.Kopf.KleinText);
            b.Suche.Text = "";
            b.BereichWahl.SelectedItem = BewohnerBereich.AlleBereiche;
            Assert.Equal(69, b.Zeilen.Count);
            b.BereichWahl.SelectedIndex = 0;
            b.Einklappen(true);
            Assert.False(b.Namen.Visible);
            b.Einklappen(false);

            // Akte öffnen, „Berichte ansehen“ führt zurück zu ihren Berichten.
            Assert.True(b.Waehlen(u.Demo("Birke").Id!.Value));
            f.Kopf.Knoepfe.Single(k => k.Text == "Akte öffnen").PerformClick();
            Assert.True(b.AkteSichtbar);
            Assert.DoesNotContain("Berichte", b.Akte.Reiter.Namen);   // der Reiter entfällt
            f.Kopf.Knoepfe.Single(k => k.Text == "Berichte ansehen").PerformClick();
            Assert.False(b.AkteSichtbar);
            Assert.Equal(u.Demo("Birke").Id, b.Gewaehlt!.Id);
        }
    });

    [Fact]
    public void Filter_Marken_Kompakt_und_gespeicherte_Filter() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen("EF");
        using (u)
        using (f)
        {
            var b = f.Bewohner;
            var d = b.Dokumentation;
            Assert.True(b.Waehlen(u.Demo("Ahorn").Id!.Value));
            d.FilterSetzen(new DokuFilter { Zeitraum = Zeitraum.DreissigTage });
            int alle = d.Zeilen.Count;
            Assert.True(alle >= 20);
            // Knopf „Wichtig“: nur wichtige, mit Marke; als Marke mit „×“ über der Liste.
            d.WichtigKnopf.Ausloesen();
            Assert.True(d.Filter.Wichtig);
            Assert.NotEmpty(d.Zeilen);
            Assert.True(d.Zeilen.Count < alle);
            Assert.All(d.Zeilen, z => Assert.Contains("Wichtig", z.Marken.Select(m => m.Text)));
            Assert.Contains(DokuRegeln.Gesetzt(d.Filter), g => g.Text == "Wichtig");
            Assert.True(d.ZuruecksetzenKnopf.Visible);
            // Suche mit Hervorhebung.
            d.FilterSetzen(d.Filter with { Wichtig = false, Suche = "Karten" });
            Assert.All(d.Zeilen, z => Assert.NotEmpty(z.Fundstellen));
            d.ZuruecksetzenKnopf.PerformClick();
            Assert.Equal(new DokuFilter(), d.Filter);
            // Gespeicherter Filter mit eigenem Namen erscheint als Knopf.
            d.FilterSetzen(new DokuFilter { Zeitraum = Zeitraum.DreissigTage, Art = "Krise" });
            d.NameFragen = () => "Krisen der letzten 30 Tage";
            Assert.True(d.FilterSpeichern());
            Assert.Contains(u.Speicher.GespeicherteFilter("EF"), g => g.Name == "Krisen der letzten 30 Tage");
            d.FilterSetzen(new DokuFilter());
            // Kompakt: lange Texte zwei Zeilen mit „… mehr“, Klick klappt auf; „vollständig“ zeigt alles.
            var lang = u.Speicher.BerichtSpeichern(new Bericht
            {
                BewohnerId = u.Demo("Ahorn").Id!.Value, Zeitpunkt = DateTime.Now.AddMinutes(-5),
                    Schicht = BerichtRegeln.SchichtZu(DateTime.Now),
                Verfasser = "EF", Text = string.Join(" ",
                    Enumerable.Repeat("Ein langer Bericht mit vielen Sätzen (Test).", 20)), Erfasst = DateTime.Now,
            });
            d.Aktualisieren();
            Application.DoEvents();
            var zeile = d.Zeilen.Single(z => z.Bericht.Id == lang.Id);
            Assert.True(zeile.Gekuerzt);
            Assert.True(zeile.Mehr.Visible);
            int kurz = zeile.Height;
            zeile.Umschalten();
            Application.DoEvents();
            Assert.True(zeile.Aufgeklappt);
            Assert.True(zeile.Height > kurz);
            f.Kopf.Knoepfe.Single(k => k.Text == "Ansicht: kompakt").PerformClick();
            Assert.False(d.Kompakt);
            Assert.False(d.Zeilen.Single(z => z.Bericht.Id == lang.Id).Gekuerzt);
            Hilfe.Foto(f, "123_dokumentation_vollstaendig");
        }
    });

    [Fact]
    public void Neuer_Bericht_und_Bedarf_fuer_den_gewaehlten_Bewohner() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen("CD");
        using (u)
        using (f)
        {
            var b = f.Bewohner;
            long eiche = u.Demo("Eiche").Id!.Value;
            Assert.True(b.Waehlen(eiche));
            using (new DialogErsatz(fenster =>
            {
                var formular = Assert.IsType<BerichtFormular>(fenster);
                Assert.Equal("Alltag", formular.Art.Text);   // Vorgabe
                formular.Art.SelectedItem = "Krise";
                formular.Bereiche[0].Checked = true;
                formular.Inhalt.Text = "Hat beim Abräumen geholfen.";
                formular.Rueckfrage = _ => DialogResult.No;
                return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bericht …").PerformClick();
            Assert.False(b.AkteSichtbar);
            Assert.Equal(eiche, b.Gewaehlt!.Id);
            var neu = b.Dokumentation.Zeilen.Single(z => z.Bericht.Text == "Hat beim Abräumen geholfen.");
            Assert.Equal("Krise", neu.Bericht.Art);

            // „Bedarf gegeben …“ (Tagetes hat eine Bedarfsverordnung): Bericht mit Marke „Bedarfsgabe“.
            Einstellungen.Aendern(w => w with { Kuerzel = "AB" });   // Fachkraft
            f.BenutzerWaehlen("AB");
            b.BereichWahl.SelectedItem = BewohnerBereich.AlleBereiche;
            Assert.True(b.Waehlen(u.Demo("Tagetes").Id!.Value));
            Assert.Null(Stil.Sperrgrund(b.BedarfKnopf));
            b.BedarfWaehlen = _ => 0;
            using (new DialogErsatz(fenster =>
            {
                var bedarf = Assert.IsType<BedarfFenster>(fenster);
                bedarf.Uhrzeit.Text = DateTime.Now.ToString("HH:mm");
                bedarf.Menge.Text = "1";
                // Ein zweiter Klick nur, wenn der erste eine Warnung zeigte („Trotzdem eintragen“).
                return bedarf.Speichern() || bedarf.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Assert.True(b.BedarfGeben());
            Assert.Contains(b.Dokumentation.Zeilen,
                z => z.Bericht.Bedarfsgabe && z.Marken.Any(m => m.Text == "Bedarfsgabe"));

            f.BenutzerWaehlen("");   // ohne Anmeldung: grau, mit Grund
            Assert.Equal(RechteRegeln.NichtAngemeldetText,
                Stil.Sperrgrund(f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bericht …")));
        }
    });
}
