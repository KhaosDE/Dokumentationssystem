using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 48: leise Zeile unter dem Text im „Neuer Bericht“, vormerken, wegklicken, „Besonderes Vorkommnis“
// kennzeichnen; nach dem Speichern öffnet das Zielformular vorbelegt und der Bericht trägt den Verweis. Einstellungsfenster
// mit Probefeld.
[Collection("Oberflaeche")]
public class SchluesselwoerterOberflaechenTests
{
    [Fact]
    public void Vorschlag_vormerken_und_Termin_nach_dem_Speichern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Demo("Ahorn");
        var morgen = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        KalenderTermin? termin = null;
        using var ersatz = new DialogErsatz(fenster =>
        {
            if (fenster is BerichtFormular formular)
            {
                formular.Bereiche[2].Checked = true;
                formular.Inhalt.Text = $"Arzttermin am {morgen:dd.MM.yyyy} um 10:30 beim Hausarzt. Blutdruck gemessen. Im Flur gestürzt.";
                Assert.Equal([Folgeschritt.Termin, Folgeschritt.Vitalwerte], formular.Vorschlaege.Gezeigt.Select(v => v.Folgeschritt));
                formular.Vorschlaege.Wegklicken(Folgeschritt.Vitalwerte);
                Assert.Equal([Folgeschritt.Termin, Folgeschritt.Vorkommnis], formular.Vorschlaege.Gezeigt.Select(v => v.Folgeschritt));
                formular.Vorschlaege.Annehmen(Folgeschritt.Vorkommnis);   // kennzeichnet sofort
                Assert.Equal("Besonderes Vorkommnis", formular.Art.SelectedItem);
                formular.Vorschlaege.Annehmen(Folgeschritt.Termin);
                Assert.Single(formular.Folgeschritte);
                Hilfe.Foto(formular, "180_schluesselwort");
                formular.Rueckfrage = _ => DialogResult.No;
                return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }
            var kalender = Assert.IsType<KalenderFenster>(fenster);
            Assert.Equal("Arzttermin", kalender.Titel.Text);
            Assert.Equal(Datum.Text(morgen), kalender.BeginnDatum.Text);
            Assert.Equal("10:30", kalender.BeginnUhrzeit.Text);
            Assert.StartsWith("aus dem Bericht vom ", kalender.Hinweis.Text);
            if (!kalender.Speichern()) return DialogResult.Cancel;
            termin = kalender.Gespeichert;
            return DialogResult.OK;
        });
        var bericht = f.NeuerBericht(b.Id!.Value);
        Assert.NotNull(bericht);
        Assert.NotNull(termin);
        Assert.Equal($"→ Termin {morgen:dd.MM.} 10:30", u.Speicher.Berichte(b.Id!.Value).Single(x => x.Id == bericht!.Id).Verweis);
    });

    [Fact]
    public void Einstellungen_Probefeld_und_keine_Vorschlaege() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "AB" });   // ansehen ja, ändern nein
        using var fenster = new SchluesselwoerterFenster(u.Speicher);
        Assert.NotEmpty(fenster.Woerter.Items);
        Assert.NotNull(Stil.Sperrgrund(fenster.HinzuKnopf));
        fenster.Probe.Text = "Blutabnahme am 3.12.2026 um 8 Uhr";
        Assert.StartsWith("Treffer: „Blutabnahme“. Angebot: Aufgabe mit Fälligkeit", fenster.ProbeErgebnis.Text);
        fenster.KeineVorschlaege.Checked = true;
        Assert.True(u.Speicher.VorschlaegeAus("AB"));
        using var formular = new BerichtFormular(u.Speicher, u.Demo("Ahorn"));
        formular.Inhalt.Text = "Arzttermin morgen";
        Assert.Empty(formular.Vorschlaege.Gezeigt);
    });
}
