using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// HEB-Bogen in der Akte: Liste mit „HEB fällig“, neuer Entwicklungsbericht mit vorgelegten Zielen,
// Zähler, Stilhinweise, Textbausteine, Hilfebedarf nie vorbelegt, PDF ausfüllen mit Test-PDF,
// Vorlagen in den Einstellungen, „HEB fällig“ auf der Übersicht.
[Collection("Oberflaeche")]
public class HebOberflaechenTests
{
    private static string TestPdf
    {
        get
        {
            var ordner = new DirectoryInfo(AppContext.BaseDirectory);
            while (ordner is not null && !File.Exists(Path.Combine(ordner.FullName, "CLAUDE.md"))) ordner = ordner.Parent;
            return Path.Combine(ordner!.FullName, "tests", "daten", "heb-test.pdf");
        }
    }

    private static (Umgebung, Hauptfenster, HebAnsicht) Oeffnen(string nachname)
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("HEB");
        return (u, f, f.Bewohner.Akte.Heb);
    }

    [Fact]
    public void Liste_faellig_und_neuer_Bogen_mit_Zielen() => Hilfe.Sta(() =>
    {
        var (u, f, h) = Oeffnen("Ahorn");
        using (u)
        using (f)
        {
            Assert.True(h.Visible);
            Assert.True(h.Faellig.Visible);
            Assert.StartsWith("HEB fällig", h.Faellig.AccessibleName);
            var karte = Assert.Single(h.Karten);
            Assert.Equal("fertig", karte.StatusText);

            bool fensterGesehen = false;
            using (new DialogErsatz(fenster =>
            {
                if (fenster is HebNeuFenster neu)
                {
                    Assert.Equal(Bogenart.B, neu.Art);
                    Assert.Equal(Datum.Text(karte.Bogen.Bis), neu.Von.Text);
                    Assert.Contains("Laufende Nummer: 4", neu.Info.Text);
                    Assert.Contains("zum Fortschreiben vorgelegt", neu.Info.Text);
                    return neu.Anlegen() ? DialogResult.OK : DialogResult.Cancel;
                }
                var w = Assert.IsType<HebFenster>(fenster);
                fensterGesehen = true;
                Hilfe.Zeigen(w);
                Assert.StartsWith("1. Hr. A. pflegt", w.Felder["1d"].Text);
                Assert.All(w.Hilfebedarf.Values, r => Assert.True(r[0].Checked));   // nie vorbelegt
                w.Felder["1a"].Text = "Gespräche wöchentlich; Begleitung zu Telefonaten.";
                Assert.Contains("Semikolon", w.Hinweise["1a"].Text);
                Assert.StartsWith($"{w.Felder["1a"].Text.Length} / 340", w.Zaehler["1a"].Text);
                w.Felder["1b"].Text = "Er telefoniert";
                w.FeldWaehlen("1b");
                w.Felder["1b"].SelectionStart = w.Felder["1b"].Text.Length;
                Assert.True(w.BausteinEinfuegen("wöchentlich"));
                Assert.Equal("Er telefoniert wöchentlich", w.Felder["1b"].Text);
                w.Hilfebedarf[1][3].Checked = true;
                Assert.True(w.Speichern(false));
                Assert.Equal("Entwurf gespeichert.", w.Meldung.Text);
                Assert.Null(w.PdfAusfuellen());   // keine Vorlage zugeteilt
                Assert.Contains("keine PDF-Vorlage", w.Meldung.Text);
                w.Rueckfrage = _ => DialogResult.No;
                return DialogResult.OK;
            }))
                h.NeuKnopf.PerformClick();
            Assert.True(fensterGesehen);
            Assert.Equal(2, h.Karten.Count);
            Assert.Equal("Entwurf", h.Karten[0].StatusText);
            var gespeichert = u.Speicher.HebBoegen(u.Demo("Ahorn").Id!.Value)[^1];
            Assert.Equal(3, gespeichert.Aktuell!.Hilfebedarf[1]);
            Assert.Null(gespeichert.Aktuell.Hilfebedarf[2]);
        }
    });

    [Fact]
    public void Vorlage_zuteilen_und_PDF_ausfuellen() => Hilfe.Sta(() =>
    {
        var (u, f, h) = Oeffnen("Kiefer");
        using (u)
        using (f)
        {
            Assert.True(f.EinstellungenOeffnen("Vorlagen"));
            var fenster = f.Einstellungsseite;
            {
                Assert.StartsWith("keine Vorlage", fenster.Vorlagen[Bogenart.B].Text);
                fenster.PdfWaehlen = () => TestPdf;
                Assert.True(fenster.VorlageZuteilen(Bogenart.B, TestPdf));
                Assert.StartsWith("zugeteilt (16 Formularfelder)", fenster.Vorlagen[Bogenart.B].Text);
                using (new DialogErsatz(z =>
                {
                    var zuordnung = Assert.IsType<HebZuordnungFenster>(z);
                    var zeile = zuordnung.Tabelle.Rows.Cast<DataGridViewRow>().Single(r => r.Cells[0].Value as string == "leitziel");
                    Assert.Equal("", zeile.Cells[1].Value);   // kein gleichnamiges Feld
                    zeile.Cells[1].Value = "Feld_Leitziel";
                    return zuordnung.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                }))
                using (var z = new HebZuordnungFenster(Bogenart.B)) Dialog.Zeigen(z, f);
            }
            Assert.Equal("Feld_Leitziel", HebPdf.Zuordnung(Bogenart.B)["leitziel"]);

            var entwurf = h.Karten.Single(k => !k.Bogen.Fertig).Bogen;
            using var w = new HebFenster(u.Speicher, u.Demo("Kiefer"), entwurf);
            Hilfe.Zeigen(w);
            w.ReiterWaehlen("Ziele");
            w.Felder["leitziel"].Text = "Ich, Hr. K., möchte den Kontakt zu meiner Familie halten.";
            string? pfad = w.PdfAusfuellen();
            Assert.NotNull(pfad);
            Assert.True(File.Exists(pfad));
            Assert.Contains("ohne passendes PDF-Feld", w.Meldung.Text);   // nicht jedes Feld hat ein PDF-Feld
            Assert.StartsWith("Ich, Hr. K.", u.Speicher.HebBogen(entwurf.Id)!.Aktuell!.Text("leitziel"));   // vorher gespeichert

            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                w.DruckKnopf.PerformClick();
                Assert.StartsWith("HEB B Karl Kiefer", Assert.Single(ersatz.Titel));
            }
            using var bild = new Bitmap(800, 1130);
            using var g = Graphics.FromImage(bild);
            var druck = new HebDruck(u.Demo("Kiefer"), entwurf, HebPdf.Werte(entwurf, u.Demo("Kiefer"), entwurf.Aktuell!));
            int seiten = 1;
            while (druck.SeiteZeichnen(g)) seiten++;
            Assert.True(seiten >= 1);
        }
    });

    [Fact]
    public void Fertig_ist_schreibgeschuetzt_und_Uebersicht_zeigt_faellig() => Hilfe.Sta(() =>
    {
        var (u, f, h) = Oeffnen("Ginster");
        using (u)
        using (f)
        {
            var fertig = h.Karten[0].Bogen;
            using (var w = new HebFenster(u.Speicher, u.Demo("Ginster"), fertig))
            {
                Hilfe.Zeigen(w);
                Assert.True(w.Felder["1a"].ReadOnly);
                Assert.False(w.SpeichernKnopf.Enabled);
                Assert.Equal("Wieder als Entwurf", w.FertigKnopf.Text);
                Assert.False(w.BausteinEinfuegen("täglich"));
            }
            f.BereichWaehlen("Übersicht");
            var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
            Assert.Contains(s.Zeilen.Single(k => k.Bewohner.Nachname == "Ahorn").Marken, m => m.Text.StartsWith("HEB fällig"));
            Assert.DoesNotContain(s.Zeilen.Single(k => k.Bewohner.Nachname == "Birke").Marken, m => m.Text.StartsWith("HEB fällig"));
        }
    });
}
