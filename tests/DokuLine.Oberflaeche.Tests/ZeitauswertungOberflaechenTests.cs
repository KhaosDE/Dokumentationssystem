using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 41: Bereich „Auswertung“ nur mit Recht, Tabelle je Bewohner, CSV und Druck; Dauer im Berichtsformular.
[Collection("Oberflaeche")]
public class ZeitauswertungOberflaechenTests
{
    [Fact]
    public void Auswertung_nur_mit_Recht_Tabelle_Csv_und_Druck() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });   // Assistenzkraft: ohne Recht
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.True(f.Ausgeblendet("Auswertung"));
        f.BenutzerWaehlen("EF");                                // Bereichsleitung
        Assert.False(f.Ausgeblendet("Auswertung"));
        f.ArbeitsbereichWaehlen("R1");
        f.BereichWaehlen("Auswertung");
        var a = f.Auswertung;
        Assert.Equal(["Auswerten", "Drucken …", "Als CSV speichern …"], f.Kopf.Knoepfe.Select(k => k.Text));
        a.Von.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Now).AddDays(-30));
        a.Aktualisieren();
        int r1 = u.Speicher.AlleBewohner().Count(b => b.Wohnbereich == "R1");
        Assert.Equal(r1, a.Tabelle.Items.Count);
        Assert.Contains(a.Zeilen, z => z.Massnahmen > 0);
        Assert.Contains("keine Abrechnung", a.Hinweis.Text);
        Assert.DoesNotContain(a.Tabelle.Columns.Cast<ColumnHeader>(), c => c.Text.Contains("Mitarbeiter") || c.Text.Contains("Kürzel"));
        Hilfe.Foto(f, "159_zeitauswertung");

        string datei = Path.Combine(u.Ordner, "auswertung.csv");
        a.DateiWaehlen = () => datei;
        Assert.True(a.CsvSpeichern());
        Assert.Equal(r1 + 3, File.ReadAllLines(datei).Length);
        string? titel = null;
        a.Vorschau = (bauen, t) => { using var d = bauen(); titel = t; };
        Assert.True(a.Drucken());
        Assert.Equal("Zeitauswertung", titel);
    });

    [Fact]
    public void Bericht_mit_Dauer() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "AB" });
        var b = u.Demo("Ahorn");
        using var formular = new BerichtFormular(u.Speicher, b);
        Assert.DoesNotContain(MassnahmenRegeln.ArtBericht, formular.Art.Items.Cast<string>());   // vergibt das Programm
        formular.Bereiche[0].Checked = true;
        formular.Inhalt.Text = "Gespräch über den Wochenplan (Test).";
        formular.Dauer.Text = "abc";
        Assert.False(formular.Speichern());
        formular.Dauer.Text = "20";
        Assert.True(formular.Speichern());
        Assert.Contains(u.Speicher.Berichte(b.Id!.Value), x => x.Text == "Gespräch über den Wochenplan (Test)." && x.Dauer == 20);
    });
}
