using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Reiter „Wochenplan“: Regelwoche mit Therapien, Blättern, Teilnahme, neuer Eintrag mit Warnung,
// ändern ab Datum, entfernen, Verlauf, Ausdruck.
[Collection("Oberflaeche")]
public class WochenplanOberflaechenTests
{
    private static (Umgebung, Hauptfenster, WochenplanAnsicht) Oeffnen(string nachname = "Ahorn")
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Wochenplan");
        return (u, f, f.Bewohner.Akte.Wochenplan);
    }

    private static DateOnly DieserMontag => WochenplanRegeln.Montag(DateOnly.FromDateTime(DateTime.Today));

    [Fact]
    public void Regelwoche_mit_Therapien_und_Blaettern() => Hilfe.Sta(() =>
    {
        var (u, f, w) = Oeffnen();
        using (u)
        using (f)
        {
            Assert.Equal(DieserMontag, w.Montag);
            Assert.StartsWith("KW ", w.WocheText.Text);
            Assert.Contains(w.Raster.Termine, t => t.Titel == "Holzwerkstatt" && t.Quelle == TerminQuelle.Therapie);
            Assert.Contains(w.Raster.Termine, t => t.Titel == "Spaziergang mit der Wohngruppe" && t.Quelle == TerminQuelle.Plan);
            Assert.Contains("Holzwerkstatt", w.Raster.AccessibleName);
            w.VorKnopf.PerformClick();
            Assert.Equal(DieserMontag.AddDays(7), w.Montag);
            w.ZurueckKnopf.PerformClick();
            w.ZurueckKnopf.PerformClick();
            Assert.Equal(DieserMontag.AddDays(-7), w.Montag);
            w.DieseWocheKnopf.PerformClick();
            Assert.Equal(DieserMontag, w.Montag);
            Assert.True(w.Raster.Height > 400);
        }
    });

    [Fact]
    public void Teilnahme_eintragen() => Hilfe.Sta(() =>
    {
        var (u, f, w) = Oeffnen();
        using (u)
        using (f)
        {
            w.WocheZeigen(DieserMontag.AddDays(7));
            var holz = w.Raster.Termine.First(t => t.Titel == "Holzwerkstatt");
            Assert.Equal(Teilnahme.Offen, holz.Teilnahme);
            w.TeilnahmeSetzen(holz, Teilnahme.Abgesagt);
            Assert.Equal(Teilnahme.Abgesagt, w.Raster.Termine.First(t => t.Titel == "Holzwerkstatt").Teilnahme);
            Assert.Contains("(abgesagt)", w.Raster.AccessibleName);
        }
    });

    [Fact]
    public void Neuer_Eintrag_mit_Warnung_aendern_und_entfernen() => Hilfe.Sta(() =>
    {
        var (u, f, w) = Oeffnen();
        using (u)
        using (f)
        {
            string? warnung = null;
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<PlanEintragFenster>(fenster);
                neu.Titel.Text = "Chor";
                neu.Wochentag.SelectedIndex = 0;   // Montag
                neu.Von.Text = "10:00";
                neu.Bis.Text = "11:00";
                Assert.False(neu.Speichern());
                warnung = neu.Warnung.Text;
                neu.Von.Text = "18:00";
                neu.Bis.Text = "19:00";
                neu.Ab.Text = Datum.Text(DieserMontag);
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                w.NeuKnopf.PerformClick();
            Assert.Contains("Holzwerkstatt", warnung);
            var chor = w.Raster.Termine.Single(t => t.Titel == "Chor");
            Assert.Equal("18:00–19:00", chor.ZeitText);

            using (new DialogErsatz(fenster =>
            {
                var aendern = Assert.IsType<PlanEintragFenster>(fenster);
                Assert.Equal("Chor", aendern.Titel.Text);
                aendern.Von.Text = "19:00";
                aendern.Bis.Text = "20:00";
                aendern.Ab.Text = Datum.Text(DieserMontag.AddDays(7));
                return aendern.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                w.Bearbeiten(chor);
            Assert.Equal("18:00–19:00", w.Raster.Termine.Single(t => t.Titel == "Chor").ZeitText);
            w.VorKnopf.PerformClick();
            var spaeter = w.Raster.Termine.Single(t => t.Titel == "Chor");
            Assert.Equal("19:00–20:00", spaeter.ZeitText);

            using (new DialogErsatz(fenster =>
            {
                var ende = Assert.IsType<DatumFenster>(fenster);
                ende.Datumsfeld.Text = Datum.Text(DieserMontag.AddDays(14));
                return ende.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                w.Entfernen(spaeter);
            Assert.Contains(w.Raster.Termine, t => t.Titel == "Chor");
            w.VorKnopf.PerformClick();
            Assert.DoesNotContain(w.Raster.Termine, t => t.Titel == "Chor");

            string? verlauf = null;
            using (new DialogErsatz(fenster => { verlauf = Assert.IsType<TextFenster>(fenster).Inhalt.Text; return DialogResult.Cancel; }))
                w.VerlaufKnopf.PerformClick();
            Assert.Contains("neu ab", verlauf);
            Assert.Contains("geändert ab", verlauf);
            Assert.Contains("entfernt ab", verlauf);
        }
    });

    [Fact]
    public void Ausdruck_nur_Name_und_Plan() => Hilfe.Sta(() =>
    {
        var (u, f, w) = Oeffnen();
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                w.DruckKnopf.PerformClick();
                Assert.StartsWith("Wochenplan Anton Ahorn", Assert.Single(ersatz.Titel));
            }
            using var bild = new Bitmap(1130, 800);
            using var g = Graphics.FromImage(bild);
            new WochenplanDruck(u.Demo("Ahorn"), w.Montag, w.Termine).SeiteZeichnen(g);
        }
    });
}
