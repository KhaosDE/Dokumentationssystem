using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// „Assistenzkraft und Fachkraft“ (0.33.0): Qualifikation in Anmeldung, Mitarbeiterliste und Formular;
// Visiten nur Fachkräfte; „Bedarf gegeben …“ am Medikamentenplan – als Assistenzkraft nur mit Absprache,
// Warnung bei erreichter Höchstmenge, danach als Bericht in der Akte.
[Collection("Oberflaeche")]
public class AssistenzkraftOberflaechenTests
{
    [Fact]
    public void Qualifikation_und_Visiten_nur_Fachkraefte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using (var anmelden = new AnmeldenFenster(u.Speicher, ""))
        {
            var eintraege = anmelden.DemoListe.Items.Cast<string>().ToList();
            Assert.Contains("KL – Karin Lerche · Mitarbeiter Wohnbereich · Assistenzkraft", eintraege);
            Assert.Contains("AB – Anna Bachstelze · Mitarbeiter Wohnbereich · Fachkraft", eintraege);
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using (var fenster = new MitarbeiterFenster(u.Speicher))
        {
            Hilfe.Zeigen(fenster);
            Assert.Equal("Qualifikation", fenster.Liste.Columns[2].Text);
            var kl = fenster.Liste.Items.Cast<ListViewItem>().Single(i => i.Text == "KL");
            Assert.Equal("Assistenzkraft", kl.SubItems[2].Text);
            using var formular = fenster.FormularErstellen(u.Speicher.MitarbeiterLesen("KL"));
            Assert.True(formular.Assistenzkraft.Checked);
            Assert.False(formular.Fachkraft.Checked);
            using var neu = fenster.FormularErstellen(null);
            Assert.True(neu.Assistenzkraft.Checked);   // die kleinste Stufe ist die Vorgabe
            Hilfe.Foto(formular, "118_mitarbeiter_qualifikation");
        }

        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("KL");
        f.BereichWaehlen("Visiten");
        Assert.Equal(RechteRegeln.NurFachkraftText, Stil.Sperrgrund(f.Visiten.BeginnenKnopf));
        f.BenutzerWaehlen("AB");
        f.BereichWaehlen("Visiten");
        Assert.Null(Stil.Sperrgrund(f.Visiten.BeginnenKnopf));
    });

    [Fact]
    public void Bedarf_gegeben_mit_Absprache_und_Bericht() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });   // Assistenzkraft R1
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var tagetes = u.Demo("Tagetes");
        f.BereichWaehlen("Bewohner");
        f.Bewohner.AkteZeigen(tagetes.Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
        var m = f.Bewohner.Akte.Medikation;
        var bedarf = m.BedarfEintraege.First(s => s.Verordnung.Bedarf);
        int vorher = u.Speicher.Bedarfsgaben(bedarf.Verordnung.Id).Count;

        string? fehlerOhne = null, wer = null;
        using (new DialogErsatz(fenster =>
        {
            switch (fenster)
            {
                case VerordnungDetailFenster d:
                    Assert.True(d.Karte.BedarfMoeglich);   // der Dialog ist im Test nicht angezeigt: Visible wäre false
                    Assert.Null(Stil.Sperrgrund(d.Karte.BedarfKnopf));
                    Assert.Contains(d.Karte.MarkenTexte, t => t.StartsWith("in 24 Std. "));
                    Assert.True(d.BedarfWaehlen());
                    return DialogResult.OK;
                case BedarfFenster b:
                    wer = b.Wer.Text;
                    Assert.StartsWith("In den letzten 24 Stunden", b.Bisher.Text);
                    Assert.Equal("Höchstens: 2 in 24 Stunden", b.Hoechstens.Text);
                    Assert.Equal("bei starker Unruhe", b.Anlass.Text);
                    Assert.False(b.Speichern());                         // ohne Absprache nicht
                    fehlerOhne = b.Fehler.Text;
                    Assert.True(b.AbspracheWaehlen("AB"));
                    Hilfe.Foto(b, "119_bedarf_gegeben");
                    return b.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                default:
                    throw new InvalidOperationException(fenster.GetType().Name);
            }
        }))
            m.Oeffnen(bedarf);
        Assert.Equal("KL – Karin Lerche (Assistenzkraft)", wer);
        Assert.Contains(BedarfRegeln.AbspracheText, fehlerOhne);
        var gaben = u.Speicher.Bedarfsgaben(bedarf.Verordnung.Id);
        Assert.Equal(vorher + 1, gaben.Count);
        var neu = gaben.OrderBy(g => g.Erstellt).Last();
        Assert.Equal(("KL", "AB"), (neu.Kuerzel, neu.Absprache));
        var bericht = u.Speicher.Berichte(tagetes.Id!.Value).Single(b => b.Id == neu.BerichtId);
        Assert.EndsWith("In Absprache mit Anna Bachstelze (AB).", bericht.Text);
        Assert.Equal("KL", bericht.Verfasser);

        // Fachkraft AB: ohne Absprache; die Höchstmenge (2 in 24 Stunden) ist erreicht → erst nach Rückfrage.
        f.BenutzerWaehlen("AB");
        f.BereichWaehlen("Bewohner");
        f.Bewohner.AkteZeigen(tagetes.Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
        m = f.Bewohner.Akte.Medikation;
        bedarf = m.BedarfEintraege.First(s => s.Verordnung.Bedarf);
        bool zuViel = BedarfRegeln.Warnung(bedarf.Verordnung, u.Speicher.BedarfsgabenMittel(bedarf.Verordnung), DateTime.Now, 1) is not null;
        string? warnung = null, knopf = null;
        using (new DialogErsatz(fenster =>
        {
            if (fenster is VerordnungDetailFenster d) { Assert.True(d.BedarfWaehlen()); return DialogResult.OK; }
            var b = Assert.IsType<BedarfFenster>(fenster);
            Assert.Equal(BedarfFenster.OhneAbsprache, b.Absprache.Text);
            if (zuViel)
            {
                Assert.False(b.Speichern());
                warnung = b.Warnung.Text;
                knopf = b.SpeichernKnopf.Text;
            }
            return b.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            m.Oeffnen(bedarf);
        if (zuViel)
        {
            Assert.StartsWith("Achtung: höchstens 2 in 24 Stunden", warnung);
            Assert.Equal("Trotzdem eintragen", knopf);
        }
        Assert.Null(u.Speicher.Bedarfsgaben(bedarf.Verordnung.Id).OrderBy(g => g.Erstellt).Last().Absprache);
    });
}
