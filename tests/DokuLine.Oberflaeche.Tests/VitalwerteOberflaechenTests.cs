using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 36: Reiter „Vitalwerte“ – Tabelle und Verlauf, erfassen, Korrektur mit Grund, Grenzen und Messaufträge,
// Kopfmarke „außerhalb des angeordneten Bereichs“, Druck, Richtwerte in den Einstellungen, Visite.
[Collection("Oberflaeche")]
public class VitalwerteOberflaechenTests
{
    [Fact]
    public void Reiter_erfassen_korrigieren_drucken() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Demo("Ahorn");
        long id = b.Id!.Value;
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(id);
        var akte = f.Bewohner.Akte;
        Assert.Contains(akte.KopfMarken, m => m.Contains(VitalRegeln.Ausserhalb));   // Demo: angeordneter Bereich, neuester Wert darüber
        akte.Reiter.Waehlen("Vitalwerte");
        var v = akte.Vital;
        Assert.True(v.Visible);
        Assert.True(v.Tabelle.Items.Count > 10);
        Assert.Equal(VitalRegeln.Blutdruck, v.VerlaufArt.SelectedItem);
        Assert.True(v.Verlauf.Punkte > 5);
        Assert.Contains(v.Tabelle.Items.Cast<ListViewItem>(), i => i.SubItems[2].Text == VitalRegeln.Ausserhalb);
        Assert.StartsWith("Angeordnete Bereiche: Blutdruck:", v.Angeordnet.Text);
        Hilfe.Foto(f, "149_vitalwerte");

        using (var fenster = new VitalwertFenster(u.Speicher, b, null))
        {
            fenster.Felder[VitalRegeln.Blutdruck].Text = "131";
            fenster.Diastolisch.Text = "82";
            fenster.Felder[VitalRegeln.Temperatur].Text = "36,9";
            fenster.Bemerkung.Text = "nach dem Frühstück";
            Hilfe.Foto(fenster, "150_vitalwerte_erfassen");
            Assert.True(fenster.Speichern());
        }
        var neu = u.Speicher.Vitalwerte(id).Where(w => w.Bemerkung == "nach dem Frühstück").ToList();
        Assert.Equal(2, neu.Count);
        Assert.All(neu, w => Assert.Equal("TST", w.Kuerzel));
        using (var korrektur = new VitalwertFenster(u.Speicher, b, neu.Single(w => w.Art == VitalRegeln.Temperatur)))
        {
            Assert.Single(korrektur.Felder);
            korrektur.Felder[VitalRegeln.Temperatur].Text = "37,0";
            Assert.False(korrektur.Speichern());   // ohne Grund
            korrektur.Grund.Text = "verlesen";
            Assert.True(korrektur.Speichern());
        }
        Assert.Contains(u.Speicher.Vitalwerte(id), w => w.Art == VitalRegeln.Temperatur && w.Wert == 37.0 && w.Grund == "verlesen");

        v.Zeigen(b);
        string? titel = null;
        v.Vorschau = (bauen, t) => { using var d = bauen(); titel = t; };
        Assert.True(v.Drucken());
        Assert.Equal($"Vitalwerte {b.Name}", titel);
        Assert.Contains(v.DruckInhalt(), a => a.Titel == VitalRegeln.Blutdruck);
    });

    [Fact]
    public void Grenzen_Messauftraege_und_Richtwerte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        var b = u.Demo("Birke");
        long id = b.Id!.Value;
        using (var fenster = new VitalGrenzenFenster(u.Speicher, b))
        {
            fenster.Art.SelectedItem = VitalRegeln.Puls;
            fenster.Von.Text = "50";
            fenster.Bis.Text = "110";
            Assert.False(fenster.Anordnen());   // ohne Arzt
            Assert.Contains("anordnenden Arzt", fenster.Fehler.Text);
            fenster.Arzt.SelectedIndex = 1;
            Assert.True(fenster.Anordnen());
            int vorher = fenster.Auftraege.Items.Count;
            fenster.AuftragArt.SelectedItem = VitalRegeln.Gewicht;
            fenster.Rhythmus.SelectedIndex = 2;   // monatlich
            Assert.True(fenster.AuftragAnlegen());
            Assert.Equal(vorher + 1, fenster.Auftraege.Items.Count);
            Hilfe.Foto(fenster, "151_vitalwerte_grenzen");
        }
        Assert.Equal(110, u.Speicher.Grenzen(id)[VitalRegeln.Puls].Bis);
        Assert.Contains(u.Speicher.Messauftraege(id), m => m.Art == VitalRegeln.Gewicht && m.Rhythmus == Messrhythmus.Monatlich && m.Tag == 1);

        using (var r = new RichtwerteFenster(u.Speicher))
        {
            r.Felder["Puls über"].Text = "105";
            r.Einheit.SelectedItem = VitalRegeln.MmolL;
            Assert.True(r.Speichern());
        }
        Assert.Equal((105.0, VitalRegeln.MmolL), (u.Speicher.Richtwerte().PulsHoch, u.Speicher.Richtwerte().BlutzuckerEinheit));
    });
}
