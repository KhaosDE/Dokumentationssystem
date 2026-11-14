using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 46: Entlassen aus der Akte, Seite „Archiv“ nur mit Recht, Akte nur lesend mit Drucken, Wiederaufnahme,
// Löschen zu zweit, Frist in den Einstellungen.
[Collection("Oberflaeche")]
public class ArchivOberflaechenTests
{
    [Fact]
    public void Entlassen_Archiv_ansehen_und_wieder_aufnehmen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.True(f.Ausgeblendet("Archiv"));   // Assistenzkraft: kein Archiv
        f.BenutzerWaehlen("EF");                 // Bereichsleitung
        Assert.False(f.Ausgeblendet("Archiv"));
        var b = u.Demo("Ahorn");
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(b.Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Null(Stil.Sperrgrund(akte.EntlassenKnopf));
        using (var fenster = new EntlassenFenster(u.Speicher, b))
        {
            fenster.Grund.SelectedIndex = 1;   // Verlegung
            fenster.EntlassDatum.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Now).AddDays(3));
            Assert.False(fenster.Speichern());   // Zukunft
            fenster.EntlassDatum.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Now));
            Hilfe.Foto(fenster, "177_entlassen");
            Assert.True(fenster.Speichern());
        }
        Assert.DoesNotContain(u.Speicher.AlleBewohner(), x => x.Id == b.Id);

        f.BereichWaehlen("Archiv");
        var a = f.Archiv;
        Assert.Equal(4, a.Liste.Items.Count);   // drei aus der Demo und Ahorn
        Assert.False(a.Hinweis.Visible);         // Demo: Frist 10 Jahre
        a.Suche.Text = "Ahorn";
        var zeile = Assert.Single(a.Liste.Items.Cast<ListViewItem>());
        Assert.Equal("Verlegung", zeile.SubItems[4].Text);
        Hilfe.Foto(f, "178_archiv");
        zeile.Selected = true;
        using (var lesen = new ArchivAkteFenster(u.Speicher, b))
        {
            Assert.True(lesen.Inhalt.ReadOnly);
            Assert.Contains("Aufenthalte:", lesen.Inhalt.Text);
            lesen.Reiter.Waehlen("Berichte");
            Assert.NotEmpty(lesen.Inhalt.Text);
            string? titel = null;
            lesen.Vorschau = (_, t) => titel = t;
            lesen.Drucken();
            Assert.Equal($"Archiv {b.Name}", titel);
        }
        var heute = DateOnly.FromDateTime(DateTime.Now);
        Assert.Contains(u.Speicher.Zugriffe("TST", heute, heute, b.Id), z => z.Aktion == ZugriffRegeln.Gedruckt && z.Was == "Archiv");

        using (var wieder = new WiederaufnahmeFenster(u.Speicher, b))
        {
            Assert.True(wieder.Bett.Items.Count > 0);
            Assert.True(wieder.Speichern());
        }
        a.Aktualisieren();
        Assert.Contains(u.Speicher.AlleBewohner(), x => x.Id == b.Id);
        Assert.Equal(3, u.Speicher.Archiv().Count);
    });

    [Fact]
    public void Loeschen_zu_zweit_und_Frist() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Archiv");
        var a = f.Archiv;
        a.NurAbgelaufen.Checked = true;
        var zeile = Assert.Single(a.Liste.Items.Cast<ListViewItem>());
        long id = ((ArchivZeile)zeile.Tag!).Bewohner.Id!.Value;
        zeile.Selected = true;
        Assert.True(a.Freigeben());
        a.Liste.Items[0].Selected = true;
        Assert.NotNull(Stil.Sperrgrund(a.LoeschenKnopf));   // nicht dieselbe Person
        f.BenutzerWaehlen("RS");                            // zweite Person (Leitung)
        f.BereichWaehlen("Archiv");
        a.NurAbgelaufen.Checked = true;
        a.Liste.Items[0].Selected = true;
        a.LoeschenBestaetigen = _ => true;
        Assert.True(a.Loeschen());
        Assert.Null(u.Speicher.Bewohner(id));
        Assert.Single(u.Speicher.Loeschprotokolle());

        f.BereichWaehlen("Einstellungen");
        var e = (EinstellungenSeite)f.Seite("Einstellungen");
        Assert.Equal("10", e.ArchivFrist.Text);
        e.ArchivFrist.Text = "";
        Assert.True(e.ArchivFristUebernehmen());
        Assert.Null(u.Speicher.ArchivFrist());
        f.BereichWaehlen("Archiv");
        Assert.True(f.Archiv.Hinweis.Visible);
    });
}
