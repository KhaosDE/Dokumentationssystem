using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26b: Ärzteverzeichnis in den Einstellungen, Auswahl statt Freitext, „Arzt anlegen …“
// direkt aus der Auswahl (mit Recht), Termin beim Arzt füllt Ort und Hinweis.
[Collection("Oberflaeche")]
public class AerzteOberflaechenTests
{
    [Fact]
    public void Auswahl_statt_Freitext_und_Arzt_anlegen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var neu = new VerordnungFenster(u.Speicher, u.Demo("Fichte"));
        Assert.Equal(ComboBoxStyle.DropDownList, neu.Arzt.Liste.DropDownStyle);   // kein Freitext
        Assert.Equal(u.Speicher.Aerzte(nurAktive: true).Count, neu.Arzt.Liste.Items.Count);
        Assert.Contains("Dr. Ulme (ausgedacht) · Allgemeinmedizin (Hausarzt)", neu.Arzt.Liste.Items.Cast<string>());
        neu.Arzt.Text = "Dr. Linde (ausgedacht)";
        Assert.Equal("Dr. Linde (ausgedacht)", neu.Arzt.Gewaehlt);
        Assert.Null(Stil.Sperrgrund(neu.Arzt.AnlegenKnopf));
        Hilfe.Zeigen(neu);
        using (new DialogErsatz(f =>
        {
            var a = Assert.IsType<ArztFenster>(f);
            a.NameFeld.Text = "Dr. Kastanie (ausgedacht)";
            a.Fachrichtung.Text = "Orthopädie";
            a.Email.Text = "kastanie@example.org";
            Hilfe.Foto(a, "93_arzt_anlegen");
            return a.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            neu.Arzt.AnlegenKnopf.PerformClick();
        Assert.Equal("Dr. Kastanie (ausgedacht)", neu.Arzt.Gewaehlt);   // gleich gewählt
        Assert.NotNull(u.Speicher.ArztNach("Dr. Kastanie (ausgedacht)"));
        // Ein älterer Name, der nicht im Verzeichnis steht, bleibt wählbar und ist gekennzeichnet.
        neu.Arzt.Text = "Dr. Alt (ausgedacht)";
        Assert.EndsWith("(nicht im Verzeichnis)", (string)neu.Arzt.Liste.SelectedItem!);

        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Mitarbeiter Wohnbereich: nur auswählen
        using var ohne = new AenderungFenster(u.Speicher, u.Speicher.Verordnungen(u.Demo("Ahorn").Id!.Value)[0], AenderungArt.Pause);
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(ohne.Arzt.AnlegenKnopf));
    });

    [Fact]
    public void Verzeichnis_in_den_Einstellungen_und_Termin_beim_Arzt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using (var ohne = new AerzteFenster(u.Speicher))
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(ohne.NeuKnopf));
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using (var fenster = new AerzteFenster(u.Speicher))
        {
            Hilfe.Zeigen(fenster);
            Assert.Equal(u.Speicher.Aerzte().Count, fenster.Liste.Items.Count);
            Assert.Contains(fenster.Liste.Items.Cast<ListViewItem>(), i => i.SubItems[4].Text == "inaktiv");
            Hilfe.Foto(fenster, "94_aerzteverzeichnis");
            int esche = u.Speicher.Aerzte().FindIndex(a => a.Name.StartsWith("Dr. Esche"));
            fenster.Waehlen(esche);
            Assert.True(fenster.Umschalten());
            Assert.Equal("inaktiv", fenster.Liste.Items[esche].SubItems[4].Text);
        }
        Assert.DoesNotContain(u.Speicher.Aerzte(nurAktive: true), a => a.Name.StartsWith("Dr. Esche"));

        using var termin = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today.AddDays(2)), u.Demo("Birke").Id);
        termin.Arzt.Text = "Dr. Pappel (ausgedacht)";
        Assert.Equal("Augenpraxis (ausgedacht)", termin.Ort.Text);
        Assert.StartsWith("Dr. Pappel (ausgedacht), Augenpraxis (ausgedacht), Tel. ", termin.Hinweis.Text);
    });
}
