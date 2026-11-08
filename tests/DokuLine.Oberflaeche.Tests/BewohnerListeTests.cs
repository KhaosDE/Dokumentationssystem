using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26j: Bereich „Bewohner“ als Liste (Zimmer/Bett, Name, Bezugspflege, Hinweise),
// sortierbar, mit Suche und Bereichswahl; rechts die Berichte des gewählten Bewohners; „Akte öffnen“
// per Doppelklick oder Knopf in der Aktionsleiste.
[Collection("Oberflaeche")]
public class BewohnerListeTests
{
    [Fact]
    public void Liste_mit_Berichten_des_gewaehlten_Bewohners() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung R1
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Bewohner");
        var b = f.Bewohner;
        Assert.Equal(["Zimmer", "Name", "Bezug", "Hinweise"], b.Liste.Columns.Cast<ColumnHeader>().Select(c => c.Text));
        Assert.Equal(23, b.Zeilen.Count);   // eigener Bereich
        Assert.StartsWith("R1 · 23 Bewohner", f.Kopf.KleinText);
        Assert.Equal(["Neuer Bericht …", "Akte öffnen", "Neuer Bewohner …"], f.Kopf.Knoepfe.Select(k => k.Text));

        // Nach Zimmer: 101 Fenster, 101 Tür, 102 …; der erste ist gewählt, rechts seine Berichte.
        Assert.Equal(["101 · Fenster", "101 · Tür", "102"], b.Zeilen.Take(3).Select(z => z.ZimmerText));
        long ahorn = u.Demo("Ahorn").Id!.Value, holunder = u.Demo("Holunder").Id!.Value;
        Assert.Equal(ahorn, b.Gewaehlt!.Bewohner.Id);
        Assert.NotEmpty(b.Berichte.Liste.Karten);
        Assert.All(b.Berichte.Liste.Karten, k => Assert.Equal(ahorn, k.Bericht.BewohnerId));
        Assert.Equal("● AB", b.Zeilen[0].Eintrag.SubItems[2].Text);   // Bezugspflege mit Kürzel
        Assert.Contains("Allergie: Haselnüsse", b.Zeilen[0].HinweisText);
        Hilfe.Foto(f, "112_bewohnerliste");

        // Nächste Zeile (wie Pfeil nach unten): die Berichte wechseln mit.
        b.Liste.Items[1].Selected = true;
        Assert.Equal(holunder, b.Gewaehlt!.Bewohner.Id);
        Assert.All(b.Berichte.Liste.Karten, k => Assert.Equal(holunder, k.Bericht.BewohnerId));

        // Sortieren nach Name, nochmals: umgekehrt.
        b.Sortieren(1);
        Assert.Equal(["Ahorn", "Birke"], b.Zeilen.Take(2).Select(z => z.Bewohner.Nachname));
        Assert.Equal(holunder, b.Gewaehlt!.Bewohner.Id);   // Auswahl bleibt
        b.Sortieren(1);
        Assert.Equal("Zypresse", b.Zeilen[0].Bewohner.Nachname);
        b.Sortieren(0);

        // Suche und Bereichswahl.
        b.Suche.Text = "Tanne";
        Assert.Equal("Theo Tanne", Assert.Single(b.Zeilen).Bewohner.Name);
        Assert.StartsWith("R1 · 1 von 23 Bewohner", f.Kopf.KleinText);
        b.Suche.Text = "";
        b.BereichWahl.SelectedItem = "R3";
        Assert.Equal(23, b.Zeilen.Count);
        Assert.All(b.Zeilen, z => Assert.Equal("R3", z.Bewohner.Wohnbereich));
        b.BereichWahl.SelectedItem = BewohnerBereich.AlleBereiche;
        Assert.Equal(69, b.Zeilen.Count);
        b.BereichWahl.SelectedIndex = 0;

        // Akte öffnen: Knopf in der Aktionsleiste (genauso Doppelklick oder Enter).
        Assert.True(b.Waehlen(u.Demo("Birke").Id!.Value));
        f.Kopf.Knoepfe.Single(k => k.Text == "Akte öffnen").PerformClick();
        Assert.True(b.AkteSichtbar);
        Assert.Equal("Berta Birke", b.Akte.Bewohner!.Name);
        f.Kopf.Knoepfe[0].PerformClick();   // „← Alle Bewohner“
        Assert.False(b.AkteSichtbar);
        Assert.Equal(u.Demo("Birke").Id, b.Gewaehlt!.Bewohner.Id);   // die Auswahl bleibt
    });

    [Fact]
    public void Neuer_Bericht_fuer_den_gewaehlten_Bewohner_bleibt_in_der_Liste() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Bewohner");
        var b = f.Bewohner;
        long eiche = u.Demo("Eiche").Id!.Value;
        Assert.True(b.Waehlen(eiche));
        using (new DialogErsatz(fenster =>
        {
            var formular = Assert.IsType<BerichtFormular>(fenster);
            formular.Bereiche[0].Checked = true;
            formular.Inhalt.Text = "Hat beim Abräumen geholfen.";
            return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bericht …").PerformClick();
        Assert.False(b.AkteSichtbar);
        Assert.Equal(eiche, b.Gewaehlt!.Bewohner.Id);
        Assert.Contains(b.Berichte.Liste.Karten, k => k.Bericht.Text == "Hat beim Abräumen geholfen.");

        f.BenutzerWaehlen("");   // ohne Anmeldung: grau, mit Grund
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(f.Kopf.Knoepfe.Single(k => k.Text == "Neuer Bericht …")));
    });
}
