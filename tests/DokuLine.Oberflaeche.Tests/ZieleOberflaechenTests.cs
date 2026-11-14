using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 43: Reiter „Ziele und Maßnahmen“ (Ziele, Maßnahmen nach Ziel gruppiert), Ziel anlegen und abschließen,
// „Kein Ziel benennen“, Zielleiste in Akte und Dokumentation, Bericht zu einem Ziel, Filter „Berichte zu diesem Ziel“.
[Collection("Oberflaeche")]
public class ZieleOberflaechenTests
{
    [Fact]
    public void Reiter_Ziel_anlegen_abschliessen_und_Berichte_zum_Ziel() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Speicher.AlleBewohner().First(x => ZielRegeln.Aktive(u.Speicher.Ziele(x.Id!.Value)).Count == 3
            && u.Speicher.Massnahmen(x.Id!.Value).Any(m => m.Fassungen[0].ZielId is not null));
        long id = b.Id!.Value;
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(id);
        var akte = f.Bewohner.Akte;
        Assert.StartsWith("Leitziel: ", akte.Zielleiste.Zeile.Text);
        Assert.Contains(" · Nahziel 2: ", akte.Zielleiste.Zeile.Text);
        akte.Reiter.Waehlen(MassnahmenAnsicht.ReiterName);
        var m = akte.Massnahmen;
        Assert.True(m.Visible);
        Assert.Equal(3, m.Ziele.Items.Count);
        Assert.Equal(["Leitziel", "Nahziel", "Nahziel"], m.Ziele.Items.Cast<ListViewItem>().Select(i => i.SubItems[1].Text));
        Assert.Contains(m.Liste.Groups.Cast<ListViewGroup>(), g => g.Header == "Maßnahmen ohne Ziel");
        Assert.Contains(m.Liste.Items.Cast<ListViewItem>(), i => i.Group is not null && i.Group.Header.StartsWith("Nahziel: ", StringComparison.Ordinal));
        Assert.NotNull(Stil.Sperrgrund(m.KeinZielKnopf));   // es sind Ziele eingetragen
        Hilfe.Foto(f, "170_ziele_und_massnahmen");

        // Neues Nahziel: „überprüfen bis“ folgt der Art, bis man es selbst ändert.
        using (var fenster = new ZielFenster(u.Speicher, b))
        {
            var heute = DateOnly.FromDateTime(DateTime.Now);
            Assert.Equal(Datum.Text(heute.AddMonths(3)), fenster.UeberpruefenBis.Text);
            fenster.Art.SelectedIndex = 0;
            Assert.Equal(Datum.Text(heute.AddMonths(12)), fenster.UeberpruefenBis.Text);
            fenster.Art.SelectedIndex = 1;
            Assert.False(fenster.Speichern());   // ohne Wortlaut und Lebensbereich
            fenster.Wortlaut.Text = "Ich koche einmal in der Woche selbst (Test).";
            fenster.Lebensbereich.SelectedIndex = 1;
            Hilfe.Foto(fenster, "171_ziel_anlegen");
            Assert.True(fenster.Speichern());
            Assert.Equal(ZielArt.Nahziel, fenster.Gespeichert!.Art);
        }
        m.Zeigen(b);
        Assert.Equal(4, m.Ziele.Items.Count);
        var neu = (Ziel)m.Ziele.Items.Cast<ListViewItem>().Single(i => i.Text.StartsWith("Ich koche", StringComparison.Ordinal)).Tag!;
        Assert.Equal(Stil.Achtung.Text, m.Ziele.Items.Cast<ListViewItem>().Single(i => i.Tag == (object)neu).SubItems[6].ForeColor);   // ohne Maßnahme

        // Abschließen: „erreicht“ nur mit Satz; danach im Verlauf, nicht mehr in der Liste.
        using (var fenster = new ZielFenster(u.Speicher, b, neu))
        {
            Assert.False(fenster.Art.Enabled);
            fenster.Stand.SelectedIndex = 1;
            Assert.False(fenster.Speichern());
            fenster.StandSatz.Text = "kocht jetzt regelmäßig";
            Assert.True(fenster.Speichern());
        }
        m.Zeigen(b);
        Assert.Equal(3, m.Ziele.Items.Count);
        m.FruehereZiele.Checked = true;
        Assert.Contains(m.Ziele.Items.Cast<ListViewItem>(), i => i.SubItems[5].Text.StartsWith("erreicht am ", StringComparison.Ordinal));
        Assert.Equal(2, u.Speicher.Ziele(id).Single(z => z.Id == neu.Id).Fassungen.Count);

        // Bericht zu einem Ziel; „Berichte zu diesem Ziel“ filtert die Dokumentation.
        var ziel = ZielRegeln.Aktive(u.Speicher.Ziele(id))[1];
        using (var formular = new BerichtFormular(u.Speicher, b))
        {
            Assert.Equal(4, formular.ZielWahl.Items.Count);   // „ohne Ziel“ und drei Ziele
            formular.ZielWahl.SelectedIndex = 2;
            Assert.Equal(ziel.Id, formular.Eingabe().ZielId);
        }
        m.Ziele.Items.Cast<ListViewItem>().First(i => i.Tag is Ziel z && z.Id == ziel.Id).Selected = true;
        Assert.Null(Stil.Sperrgrund(m.ZielBerichteKnopf));
        m.ZielBerichteZeigen();
        Assert.False(f.Bewohner.AkteSichtbar);
        var doku = f.Bewohner.Dokumentation;
        Assert.Equal(ziel.Id, doku.Filter.ZielId);
        Assert.All(doku.Berichte, x => Assert.Equal(ziel.Id, x.ZielId));
        Assert.True(doku.Zielleiste.Visible);
        Assert.StartsWith("Leitziel: ", doku.Zielleiste.Zeile.Text);
        doku.Zielleiste.Umschalten();
        Assert.False(doku.Zielleiste.Zeile.Visible);
        doku.Zielleiste.Umschalten();
        Assert.True(doku.Zielleiste.Zeile.Visible);
    });

    [Fact]
    public void Nicht_erfasst_und_kein_Ziel_benennen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Speicher.AlleBewohner().First(x => u.Speicher.Ziele(x.Id!.Value).Count == 0 && u.Speicher.Verzicht(x.Id!.Value) is null);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(b.Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Equal("Ziele: nicht erfasst", akte.Zielleiste.Zeile.Text);
        Assert.True(akte.Zielleiste.Achtung);
        akte.Reiter.Waehlen(MassnahmenAnsicht.ReiterName);
        var m = akte.Massnahmen;
        Assert.Empty(m.Ziele.Items);
        Assert.StartsWith("Ziele: nicht erfasst", m.ZielStand.Text);
        Assert.Null(Stil.Sperrgrund(m.KeinZielKnopf));
        m.VerzichtFragen = () => "möchte sich erst eingewöhnen (Test)";
        Assert.True(m.KeinZiel());
        Assert.StartsWith(ZielRegeln.KeinZielText, m.ZielStand.Text);
        Assert.StartsWith(ZielRegeln.KeinZielText, akte.Zielleiste.Zeile.Text);
        Assert.False(akte.Zielleiste.Achtung);
        using (var formular = new BerichtFormular(u.Speicher, b))
            Assert.False(formular.ZielWahl.Enabled);   // nichts zu wählen
    });
}
