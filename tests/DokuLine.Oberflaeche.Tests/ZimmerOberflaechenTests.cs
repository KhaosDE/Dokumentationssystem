using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26e: Zimmer und Betten in den Einstellungen, Filter der Übersicht nach Bereich.
[Collection("Oberflaeche")]
public class ZimmerOberflaechenTests
{
    [Fact]
    public void Zimmer_und_Betten_verwalten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // ohne Recht: nur ansehen
        using (var ansehen = new ZimmerFenster(u.Speicher))
        {
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(ansehen.NeuKnopf));
            Assert.Equal(45, ansehen.Liste.Items.Count);   // 15 je Bereich (Schritt 26h)
            Assert.Equal(3, ansehen.Bereiche.Items.Count);
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung
        using var fenster = new ZimmerFenster(u.Speicher);
        Hilfe.Zeigen(fenster);
        var zeile101 = fenster.Liste.Items[fenster.Index("101")];
        Assert.Equal("Bett Fenster: Anton Ahorn · Bett Tür: Hanna Holunder", zeile101.SubItems[3].Text);
        Assert.Equal("beschützend", fenster.Bereiche.Items[1].SubItems[1].Text);
        Hilfe.Foto(fenster, "104_zimmer_und_betten");
        using (new DialogErsatz(f =>
        {
            var z = Assert.IsType<ZimmerFormular>(f);
            z.Nummer.Text = "316";
            z.Bereich.SelectedIndex = 2;   // R3
            z.Doppel.Checked = true;
            return z.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            fenster.NeuKnopf.PerformClick();
        Assert.Equal(46, fenster.Liste.Items.Count);
        Assert.Equal(4, u.Speicher.FreieBetten("R3").Count);
        // Belegtes Zimmer stilllegen geht nicht – Hinweis statt Fehler.
        fenster.Waehlen(fenster.Index("101"));
        Assert.False(fenster.Umschalten());
        Assert.Contains("belegt", fenster.Fehler.Text);
        fenster.BereichWaehlen(2);
        Assert.True(fenster.ArtUmschalten());
        Assert.Equal("offen", fenster.Bereiche.Items[2].SubItems[1].Text);
    });

    [Fact]
    public void Uebersicht_nach_Bereich() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var s = f.Uebersicht;
        Assert.Equal([UebersichtSeite.EigeneBereiche, "R1", "R2", "R3"], s.BereichWahl.Items.Cast<string>());
        s.BereichWahl.SelectedIndex = 2;   // R2
        Assert.StartsWith("R2 · 23 Bewohner · 2 Betten frei · ", f.Kopf.KleinText);
        Assert.Equal(23, s.Karten.Count);
        s.BereichWahl.SelectedIndex = 3;   // R3
        Assert.StartsWith("R3 · 23 Bewohner · 2 Betten frei · ", f.Kopf.KleinText);
        Hilfe.Foto(f, "105_uebersicht_bereich");
    });
}
