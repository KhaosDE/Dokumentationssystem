using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 44: Seite „Qualität“ nur mit Recht, Bereichswahl „QM“ beginnt dort; Kennzahlen, Prüfliste mit Filtern,
// Hinweis an den Wohnbereich, Prüfvermerk, Nachweis zum Drucken; Prüfpunkte in den Einstellungen.
[Collection("Oberflaeche")]
public class QualitaetOberflaechenTests
{
    [Fact]
    public void Pruefseite_Hinweis_Vermerk_und_Nachweis() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.True(f.Ausgeblendet("Qualität"));
        Assert.DoesNotContain(QualitaetRegeln.BereichQm, f.BereichKnoepfe());
        f.BenutzerWaehlen("QS");   // Qualitätsmanagement: liest, schreibt keine Berichte
        Assert.False(f.Ausgeblendet("Qualität"));
        Assert.Contains(QualitaetRegeln.BereichQm, f.BereichKnoepfe());
        Assert.False(Recht.Darf(u.Speicher, Funktionen.BerichteSchreiben));
        f.ArbeitsbereichWaehlen(QualitaetRegeln.BereichQm);
        Assert.Equal("Qualität", f.AktiverBereich);
        var q = f.Qualitaet;
        Assert.Equal(["Hinweis senden …", "Als geprüft vermerken", "Prüfverlauf …", "Nachweis Ziele und Maßnahmen …"], f.Kopf.Knoepfe.Select(k => k.Text));
        int alle = u.Speicher.AlleBewohner().Count;
        Assert.Equal(alle, q.Liste.Items.Count);
        Assert.Contains("R1: Ziele vollständig ", q.Kennzahlen.Text);
        Assert.Equal(3 + QualitaetRegeln.Alle.Length + 2, q.Liste.Columns.Count);
        Hilfe.Foto(f, "175_qualitaet");

        q.NurMaengel.Checked = true;
        Assert.All(q.Liste.Items.Cast<ListViewItem>(), i => Assert.True(((PruefZeile)i.Tag!).Maengel));
        q.BereichWahl.SelectedItem = "R1";
        Assert.All(q.Liste.Items.Cast<ListViewItem>(), i => Assert.Equal("R1", i.SubItems[2].Text));

        var zeile = q.Liste.Items.Cast<ListViewItem>().First();
        var bewohner = ((PruefZeile)zeile.Tag!).Bewohner;
        zeile.Selected = true;
        Assert.Null(Stil.Sperrgrund(q.HinweisKnopf));
        IReadOnlyList<string>? angeboten = null;
        q.MaengelWaehlen = texte => { angeboten = texte; return [0]; };
        Assert.True(q.HinweisSenden());
        Assert.NotEmpty(angeboten!);
        var aufgabe = u.Speicher.Aufgaben(DateTime.Now, bereich: "R1").Single(a => a.BewohnerId == bewohner.Id && a.Text.StartsWith("Qualität: ", StringComparison.Ordinal));
        Assert.Contains(angeboten![0], aufgabe.Text);
        zeile = q.Liste.Items.Cast<ListViewItem>().Single(i => ((PruefZeile)i.Tag!).Bewohner.Id == bewohner.Id);
        Assert.StartsWith("Hinweis gesendet am ", zeile.SubItems[^2].Text);

        zeile.Selected = true;
        q.BemerkungFragen = () => "Ziele mit dem Bereich besprochen (Test)";
        Assert.True(q.Vermerken());
        zeile = q.Liste.Items.Cast<ListViewItem>().Single(i => ((PruefZeile)i.Tag!).Bewohner.Id == bewohner.Id);
        Assert.Equal($"geprüft am {Datum.Text(DateOnly.FromDateTime(DateTime.Now))} von QS", zeile.SubItems[^1].Text);

        string? titel = null;
        q.Vorschau = (_, t) => titel = t;
        Assert.True(q.NachweisDrucken());
        Assert.Equal("Nachweis Ziele und Maßnahmen", titel);
    });

    [Fact]
    public void Pruefpunkte_in_den_Einstellungen_ausschalten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Einstellungen");
        var e = (EinstellungenSeite)f.Seite("Einstellungen");
        Assert.Equal(QualitaetRegeln.Alle.Length, e.Pruefpunkte.CheckedItems.Count);
        e.Pruefpunkte.SetItemChecked(Array.IndexOf(QualitaetRegeln.Alle, Pruefpunkt.Heb), false);
        Assert.Equal("Heb", Einstellungen.Laden().QmAus);
        f.BereichWaehlen("Qualität");
        Assert.Equal(3 + QualitaetRegeln.Alle.Length - 1 + 2, f.Qualitaet.Liste.Columns.Count);
        Assert.DoesNotContain(f.Qualitaet.Liste.Columns.Cast<ColumnHeader>(), c => c.Text == "HEB");
    });
}
