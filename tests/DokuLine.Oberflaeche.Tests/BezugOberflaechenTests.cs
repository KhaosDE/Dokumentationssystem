using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bezugspflege und Einzeltrainer (Bauplan Schritt 20): Farbstreifen und Punkt auf den Karten, Zeilen
// in der Akte, „Bezug ändern …“ mit Hinweis ab fünf, Filter in der Übersicht, eigene Farbe.
[Collection("Oberflaeche")]
public class BezugOberflaechenTests
{
    [Fact]
    public void Karten_und_Akte_zeigen_Bezug() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var ahorn = f.Uebersicht.Zeilen.Single(k => k.Bewohner.Nachname == "Ahorn");
        Assert.Equal("AB", ahorn.Bezugspflege!.Value.Kuerzel);
        Assert.Equal(ColorTranslator.FromHtml("#2F6FB7").ToArgb(), ahorn.Bezugspflege.Value.Farbe.ToArgb());
        Assert.Null(ahorn.Trainer);
        Assert.Contains("Bezugspflege AB", ahorn.Beschreibung);
        Assert.Equal("DBT · PR", f.Uebersicht.Zeilen.Single(k => k.Bewohner.Nachname == "Birke").Trainer!.Value.Text);
        Assert.Contains(Statusleiste.BezugText, f.Statusleiste.AccessibleName);

        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.StartsWith("Anna Bachstelze (AB) · seit ", akte.Bezugspflege.Text);
        Assert.StartsWith("DBT · Paula Rabe (PR) · seit ", akte.Einzeltrainer.Text);
        Assert.Null(Stil.Sperrgrund(akte.BezugKnopf));
        Hilfe.Foto(f, "66_akte_bezug");

        // Bezug ändern: Kiefer bekommt AB (dann sechs Bezugsbewohner – nur ein Hinweis) und einen Einzeltrainer.
        f.Bewohner.AkteZeigen(u.Demo("Kiefer").Id!.Value);
        Assert.Equal("keiner", akte.Einzeltrainer.Text);
        using (new DialogErsatz(fenster =>
        {
            var b = Assert.IsType<BezugFenster>(fenster);
            Hilfe.Zeigen(b);
            Assert.False(b.Art.Enabled);   // ohne Einzeltrainer keine Art
            b.Bezugspflege.SelectedIndex = b.Bezugspflege.Items.Cast<string>().ToList().FindIndex(x => x.StartsWith("Anna Bachstelze (AB)"));
            Assert.True(b.Hinweis.Visible);
            Assert.Contains("6 Bewohner", b.Hinweis.Text);
            b.Trainer.SelectedIndex = b.Trainer.Items.Cast<string>().ToList().FindIndex(x => x.StartsWith("Bruno Meise (BM)"));
            b.Art.Text = "Achtsamkeit";
            Hilfe.Foto(b, "67_bezug_fenster");
            return b.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            akte.BezugKnopf.PerformClick();
        Assert.StartsWith("Anna Bachstelze (AB)", akte.Bezugspflege.Text);
        Assert.StartsWith("Achtsamkeit · Bruno Meise (BM)", akte.Einzeltrainer.Text);
        string? verlauf = null;
        using (new DialogErsatz(fenster => { verlauf = Assert.IsType<TextFenster>(fenster).Inhalt.Text; return DialogResult.Cancel; }))
            akte.BezugVerlaufKnopf.PerformClick();
        Assert.Contains("Bezugspflege: Anna Bachstelze (AB)", verlauf);
        Assert.Contains("Bezugspflege: Karin Lerche (KL)", verlauf);   // die frühere bleibt im Verlauf

        // OP darf Bewohner nicht bearbeiten: „Bezug ändern …“ gesperrt.
        f.BenutzerWaehlen("OP");
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(akte.BezugKnopf));
    });

    [Fact]
    public void Filter_meine_Bezugsbewohner_und_Einzeltrainings() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var s = f.Uebersicht;
        Assert.False(s.MeineBezugsbewohner.Enabled);   // nicht angemeldet
        f.BenutzerWaehlen("AB");
        s.MeineBezugsbewohner.Checked = true;
        Assert.Equal(["Ahorn", "Birke", "Clematis", "Distel", "Eiche"], s.Zeilen.Select(k => k.Bewohner.Nachname).Order());
        Assert.Contains("meine Bezugsbewohner", f.Kopf.KleinText);
        Hilfe.Foto(f, "68_uebersicht_meine");
        s.MeineBezugsbewohner.Checked = false;
        f.BenutzerWaehlen("PR");
        s.MeineEinzeltrainings.Checked = true;
        Assert.Equal(["Birke", "Ginster", "Sonnenblume", "Tagetes"], s.Zeilen.Select(k => k.Bewohner.Nachname).Order());   // auch aus R2
        s.MeineEinzeltrainings.Checked = false;
        Assert.Equal(46, s.Zeilen.Count);   // PR: R1 und R2
    });

    [Fact]
    public void Eigene_Farbe_in_den_Einstellungen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using (var f = new Hauptfenster(u.Speicher))
        {
            Hilfe.Zeigen(f);
            f.EinstellungenOeffnen("Persönlich");
            var e = f.Einstellungsseite;
            Assert.Equal(ColorTranslator.FromHtml(BezugRegeln.Vorgabefarbe("EF")).ToArgb(), e.MeineFarbe.BackColor.ToArgb());
            e.FarbeWaehlen = _ => Color.FromArgb(0x12, 0x34, 0x56);
            e.FarbeKnopf.PerformClick();
            Assert.Equal("#123456", u.Speicher.Farbe("EF"));   // gilt sofort, gehört zur Person
            e.FarbeVorgabeKnopf.PerformClick();
            Assert.Equal(BezugRegeln.Vorgabefarbe("EF"), u.Speicher.Farbe("EF"));
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "" });
        using (var e = new EinstellungenSeite(u.Speicher))
        {
            e.Zeigen();
            Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(e.FarbeKnopf));
        }
    });
}
