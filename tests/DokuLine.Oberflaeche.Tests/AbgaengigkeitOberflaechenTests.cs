using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 34: Abgängigkeitsmeldung – Formular, wer informiert wurde, Meldeblatt, Zurück und Entwarnung; Marke in
// Akte, Übersicht und Ausgangsliste; Personenbeschreibung in den Stammdaten.
[Collection("Oberflaeche")]
public class AbgaengigkeitOberflaechenTests
{
    [Fact]
    public void Melden_informieren_Meldeblatt_zurueck_entwarnen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var ahorn = u.Demo("Ahorn");
        long id = ahorn.Id!.Value;
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(id);
        Assert.Contains(f.Kopf.Knoepfe, k => k.Text == "Abgängigkeit …");
        Assert.True(f.Bewohner.Akte.BeschreibungText.Text.Length > 0);   // Personenbeschreibung in den Stammdaten

        using var fenster = new AbgaengigkeitFenster(u.Speicher, ahorn);
        Assert.True(fenster.Formularmodus);
        Assert.StartsWith(AbgaengigkeitRegeln.BenoetigteMedikamente(u.Speicher.UeberleitungDaten(id, DateTime.Now)).Count > 0 ? "Benötigt laut Plan: " : "Regelmäßige",
            fenster.Medikamente.Text);
        Assert.NotEqual("", fenster.Kontakte.Text);   // aus der Akte vorbelegt
        Assert.False(fenster.Melden());   // ohne Kleidung
        Assert.Contains("Kleidung heute fehlt", fenster.Fehler.Text);
        fenster.Kleidung.Text = "rote Jacke, Jeans";
        fenster.ZuletztWo.Text = "Garten";
        Hilfe.Foto(fenster, "142_abgaengigkeit_formular");
        Assert.True(fenster.Melden());
        Assert.False(fenster.Formularmodus);
        Assert.Contains(fenster.Stellen.Items.Cast<ListViewItem>(), i => i.Text == "Polizei (110)");
        Assert.True(fenster.Informieren("Polizei (110)", "Vorgang 0000/26"));
        var meldung = u.Speicher.OffeneAbgaengigkeit(id)!;
        Assert.Equal(("Polizei (110)", "EF", "Vorgang 0000/26"), (meldung.Informiert[0].Stelle, meldung.Informiert[0].Kuerzel, meldung.Informiert[0].Hinweis));
        Hilfe.Foto(fenster, "143_abgaengigkeit_laufend");
        string? vorschau = null;
        fenster.Vorschau = (bauen, titel) => { using var d = bauen(); vorschau = titel; };
        Assert.True(fenster.MeldeblattDrucken());
        Assert.Equal(AbgaengigkeitRegeln.Titel(ahorn), vorschau);
        string ziel = Path.Combine(u.Ordner, "meldeblatt.pdf");
        fenster.PdfZiel = () => ziel;
        Assert.True(fenster.PdfSpeichern());
        Assert.True(File.ReadAllBytes(ziel).Length > 500);

        // Marke in Akte und Übersicht.
        f.Bewohner.Akte.NeuLaden();
        Assert.Contains(f.Bewohner.Akte.KopfMarken, m => m.StartsWith("abgängig seit"));
        f.BereichWaehlen("Übersicht");
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);
        Assert.Contains(f.Uebersicht.Zeilen.Single(z => z.Bewohner.Id == id).Marken, m => m.Art == MarkenArt.Abgaengig);

        Assert.True(fenster.Zurueck());
        Assert.Null(u.Speicher.OffeneAbgaengigkeit(id));
        Assert.Equal(["Polizei (110)"], fenster.Entwarnt.Items.Cast<ListViewItem>().Select(i => i.Text));
        Assert.True(fenster.Entwarnen("Polizei (110)"));
        Assert.Equal(["Polizei (110)"], u.Speicher.Abgaengigkeiten(id).Single().Entwarnt.Select(e => e.Stelle));
        Assert.Contains(u.Speicher.Berichte(id), b => b.Text.StartsWith("Zurück um") && b.Art == DokuRegeln.ArtAbgaengigkeit);
        // Alles entwarnt: Das Fenster beginnt wieder mit dem Formular.
        using var wieder = new AbgaengigkeitFenster(u.Speicher, ahorn);
        Assert.True(wieder.Formularmodus);
    });

    [Fact]
    public void Personenbeschreibung_aendern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        var b = u.Demo("Birke");
        using var fenster = new AngabeFenster(u.Speicher, b, Angabe.Personenbeschreibung);
        fenster.Groesse.Text = "1,82 m";
        fenster.Merkmale.Text = "trägt einen grünen Schal";
        Hilfe.Foto(fenster, "144_personenbeschreibung");
        Assert.True(fenster.Speichern());
        var p = u.Speicher.Angaben(b.Id!.Value, DateOnly.FromDateTime(DateTime.Now))[Angabe.Personenbeschreibung].Lesen<Personenbeschreibung>()!;
        Assert.Equal(("1,82 m", "trägt einen grünen Schal"), (p.Groesse, p.Merkmale));
    });

    [Fact]
    public void Ausgangsliste_zeigt_abgaengig_und_bietet_die_Meldung_an() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);
        var b = u.Demo("Ahorn");
        u.Speicher.AbgaengigkeitMelden(b.Id!.Value, new AbgaengigkeitMeldung { VermisstSeit = DateTime.Now.AddMinutes(-20), Kleidung = "graue Jacke" }, "CD", DateTime.Now);
        f.Navigation.Single(n => n.Text == "Ausgangsliste").Ausloesen();
        var seite = Assert.IsType<AusgangslisteSeite>(f.Seite("Ausgangsliste"));
        var zeile = seite.Zeilen.Single(z => z.Platz.BewohnerId == b.Id);
        Assert.True(zeile.Abgaengig);
        Assert.StartsWith("abgängig seit", zeile.StandText);
        Assert.Same(zeile, seite.Zeilen[0]);   // ganz oben
        Assert.False(zeile.AbgaengigkeitEintrag.Available);   // schon gemeldet
        long? gemeldet = null;
        var vorher = Aktionen.AbgaengigkeitMelden;
        try
        {
            Aktionen.AbgaengigkeitMelden = x => gemeldet = x;
            var andere = seite.Zeilen.First(z => !z.Abgaengig);
            Assert.True(andere.AbgaengigkeitEintrag.Available);
            andere.AbgaengigkeitEintrag.PerformClick();
            Assert.Equal(andere.Platz.BewohnerId, gemeldet);
        }
        finally { Aktionen.AbgaengigkeitMelden = vorher; }
    });
}
