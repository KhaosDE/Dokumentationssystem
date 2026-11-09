using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Was ein Mitarbeiter nicht darf, sieht er nicht: Knöpfe, Menüeinträge, Bereiche der Seitenleiste und Einstellungen
// ohne Recht sind ausgeblendet, die übrigen rücken zusammen. Grau bleibt, was gerade nicht geht.
[Collection("Oberflaeche")]
public class RechteAusblendenTests
{
    private static string Datei(string name) =>
        new([.. name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_')]);

    [Fact]
    public void Knopf_ohne_Recht_bleibt_weg_bis_das_Recht_da_ist() => Hilfe.Sta(() =>
    {
        using var form = new Form();
        var k = Stil.NebenKnopf("Probe");
        form.Controls.Add(k);
        Hilfe.Zeigen(form);
        Stil.Sperren(k, RechteRegeln.FehltText);
        Assert.False(k.Visible);
        k.Visible = true;   // der Aufrufer will ihn zeigen – ohne Recht bleibt er weg
        Assert.False(k.Visible);
        Stil.Sperren(k, RechteRegeln.NurFachkraftText);
        Assert.False(k.Visible);
        Stil.Sperren(k, RechteRegeln.NichtAngemeldetText);   // geht gerade nicht: grau, aber da
        Assert.True(k.Visible);
        k.Visible = false;
        Stil.Sperren(k, null);
        Assert.False(k.Visible);   // der Wunsch des Aufrufers gilt weiter
    });

    [Fact]
    public void Je_Position_Seitenleiste_und_Akte_ohne_Fehlendes() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.WindowState = FormWindowState.Maximized;
        long ahorn = u.Demo("Ahorn").Id!.Value;
        int geprueft = 0;
        foreach (var position in u.Speicher.Positionsliste().Where(p => p.Aktiv))
        {
            var m = u.Speicher.AlleMitarbeiter().FirstOrDefault(x => x.Aktiv && x.Positionen.Contains(position.Name));
            if (m is null) continue;
            f.BenutzerWaehlen(m.Kuerzel);
            f.BereichWaehlen("Dokumentation");
            f.Bewohner.AkteZeigen(ahorn);
            Application.DoEvents();
            bool visiten = !RechteRegeln.FehltRecht(u.Speicher.Grund(m.Kuerzel, Funktionen.Visiten));
            Assert.Equal(visiten, f.Navigation.Single(n => n.Text == "Visiten").Visible);
            // Sichtbar ist nur, was erlaubt ist; die Knöpfe stehen ohne Lücke nebeneinander.
            Assert.All(f.Kopf.Knoepfe.OfType<Knopf>(), k => Assert.Equal(!k.OhneRecht, k.Visible));
            var sichtbar = f.Kopf.Knoepfe.Where(k => k.Visible).OrderBy(k => k.Left).ToList();
            for (int i = 1; i < sichtbar.Count; i++)
                Assert.InRange(sichtbar[i].Left - sichtbar[i - 1].Right, 0, Stil.Px(f, Stil.AbstandKlein) + 1);
            Hilfe.Foto(f, $"124_rechte_{Datei(position.Name)}");
            geprueft++;
        }
        Assert.True(geprueft >= 3);
    });

    [Fact]
    public void Visiten_verschwinden_fuer_Assistenzkraefte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "AB" });   // Fachkraft
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var visiten = f.Navigation.Single(n => n.Text == "Visiten");
        f.BereichWaehlen("Visiten");
        Assert.True(visiten.Visible);
        f.BenutzerWaehlen("KL");   // Assistenzkraft: keine Visiten
        Assert.False(visiten.Visible);
        Assert.Equal(Hauptfenster.Startbereich, f.AktiverBereich);   // aus dem ausgeblendeten Bereich zur Übersicht
        var eintraege = f.Navigation.Where(n => n.Visible && n.Text != "Einstellungen").ToList();
        for (int i = 1; i < eintraege.Count; i++)
            Assert.Equal(eintraege[i - 1].Bottom + Stil.Px(f, Stil.AbstandMini), eintraege[i].Top);   // ohne Lücke
        Hilfe.Foto(f, "125_rechte_assistenzkraft");
        f.BenutzerWaehlen("");   // ohne Anmeldung gibt es niemanden, dem ein Recht fehlt
        Assert.True(visiten.Visible);
    });

    [Fact]
    public void Menues_und_Einstellungen_ohne_Recht_ausgeblendet() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        u.Speicher.RechtSetzen(Funktionen.BerichteSchreiben, "Therapie", false);
        Einstellungen.Aendern(w => w with { Kuerzel = "OP" });   // Therapie
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        long ahorn = u.Demo("Ahorn").Id!.Value;
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.DokumentationZeigen(ahorn);
        var b = f.Bewohner;
        Assert.True(((Knopf)b.BerichtNeuKnopf).OhneRecht);
        Assert.DoesNotContain("Neuer Bericht …", f.Kopf.Knoepfe.Where(k => k.Visible).Select(k => k.Text));
        Assert.Equal("Akte öffnen", f.Kopf.Knoepfe.Where(k => k.Visible).OrderBy(k => k.Left).First().Text);
        Assert.False(b.Namen.ContextMenuStrip!.Items[1].Available);   // „Neuer Bericht …“
        Assert.False(b.Dokumentation.Zeilen[0].ContextMenuStrip!.Items[0].Available);   // „Nachtrag schreiben …“
        Assert.True(b.Dokumentation.Zeilen[0].ContextMenuStrip!.Items[1].Available);    // „Aufgabe daraus machen …“
        f.BereichWaehlen("Übersicht");
        Assert.True(f.Uebersicht.Waehlen(ahorn));
        Assert.False(f.Uebersicht.Liste.ContextMenuStrip!.Items[1].Available);

        // Einstellungen: Karten ohne Recht fehlen, Gruppen ohne eine erlaubte Karte auch.
        f.EinstellungenOeffnen("Persönlich");
        var e = f.Einstellungsseite;
        Assert.True(e.GruppeSichtbar("Persönlich"));
        Assert.True(e.GruppeSichtbar(EinstellungenSeite.VerzeichnisGruppe));   // ansehen dürfen alle
        Assert.Equal(!RechteRegeln.FehltRecht(u.Speicher.Grund("OP", Funktionen.Module)), e.GruppeSichtbar("Module"));
        foreach (var (name, _) in EinstellungenSeite.GruppenListe.Where(g => e.GruppeSichtbar(g.Name)))
        {
            e.GruppeWaehlen(name);
            Assert.Equal(name, e.AktiveGruppe);
            Assert.All(e.Karten.Where(k => k.Gruppe == name), k => Assert.Equal(!k.OhneRecht, k.Visible));
        }
        Assert.All(e.Karten.Where(k => k.Funktion is string r && RechteRegeln.FehltRecht(u.Speicher.Grund("OP", r))),
            k => Assert.DoesNotContain(k, e.Suchen(k.Titel)));
        Hilfe.Foto(f, "126_rechte_einstellungen_therapie");
    });
}
