using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26: Visiten unter „Übergabe“, Liste mit Filter, „Neue Visite“ mit sofortiger
// Einblendung der Aufgaben; Medikation über den Medikamentenplan, Facharzttermin mit Kalender und Fahrt.
[Collection("Oberflaeche")]
public class VisitenOberflaechenTests
{
    private static string Heute(int tage = 0) => Datum.Text(DateOnly.FromDateTime(DateTime.Now).AddDays(tage));

    [Fact]
    public void Liste_mit_Filter_nach_Bewohner_und_Zeitraum() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var namen = f.Navigation.Select(n => n.Text).ToList();
        Assert.InRange(namen.IndexOf("Visiten"), namen.IndexOf("Übergabe") + 1, namen.IndexOf("Aufgaben") - 1);   // dazwischen die Medikamente
        f.BereichWaehlen("Visiten");
        var seite = f.Visiten;
        Assert.Equal(VisitenRegeln.Psychiater, seite.Art);   // Vorgabe
        Assert.Equal(3, seite.Karten.Count);   // Demo: drei Visiten des Psychiaters, zwei des Hausarztes
        Assert.Equal("Psychiater · 3 Visiten", seite.KopfText);
        Assert.Contains(seite.Karten.SelectMany(k => k.Texte), t => t == "Quetiapin: Dosis geändert");
        Assert.All(seite.Karten, k => Assert.Equal(VisitenRegeln.Psychiater, k.Visite.Art));
        Hilfe.Foto(f, "83_visiten");
        seite.HausarztKnopf.Ausloesen();
        Assert.Equal(VisitenRegeln.Hausarzt, seite.Art);
        Assert.Equal("Hausarzt · 2 Visiten", seite.KopfText);
        Assert.Contains(seite.Karten.SelectMany(k => k.Texte), t => t == "Facharzttermin nötig: Neurologie, dringend");
        seite.ArtWaehlen(VisitenRegeln.Psychiater);

        seite.BewohnerWaehlen(u.Demo("Ahorn").Id);
        var karte = Assert.Single(seite.Karten);
        Assert.Contains("Quetiapin: Dosis geändert", karte.Texte);
        Assert.DoesNotContain(karte.Texte, t => t.Contains("Fichte"));   // nur der gewählte Bewohner
        seite.BewohnerWaehlen(null);
        seite.ArtWaehlen(VisitenRegeln.Hausarzt);
        seite.Von.Text = Heute(-2);
        Assert.Single(seite.Karten);   // nur die Visite von gestern
        seite.Bis.Text = "gestern";
        Assert.True(seite.FilterFehler.Visible);
        seite.Von.Text = seite.Bis.Text = "";
        Assert.Equal(2, seite.Karten.Count);
    });

    [Fact]
    public void Neue_Visite_blendet_Aufgaben_ein_Medikamentenplan_und_Facharzttermin() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF", NachschlageAnsicht = false });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Visiten");
        var ahorn = u.Demo("Ahorn");
        int einblendungen = 0;
        string? fehlerLeer = null, verlaufHinweis = null;
        using (new DialogErsatz(fenster =>
        {
            switch (fenster)
            {
                case VisiteFenster v:
                    Assert.False(v.Speichern());
                    fehlerLeer = v.Fehler.Text;
                    v.Arzt.Text = "Dr. Test (ausgedacht)";
                    var a = v.BewohnerHinzufuegen("Ahorn");
                    a.Besprochen.Text = "Schläft besser, Dosis wird angepasst.";
                    a.AnordnungenGeaendert.Checked = true;
                    a.Zeilen[0].Setzen(AnordnungArt.Dosis, "Quetiapin");
                    a.ZeileHinzufuegen().Setzen(AnordnungArt.Neu, "Beispielsalbe (ausgedacht)");
                    var b = v.BewohnerHinzufuegen("Birke");
                    b.Besprochen.Text = "Herzklopfen abklären.";
                    b.FacharztNoetig.Checked = true;
                    b.Fachrichtung.Text = "Kardiologie";
                    b.Dringend.Checked = true;
                    Hilfe.Foto(v, "84_visite_neu");
                    return v.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                case VisitenAufgabenFenster e:
                    einblendungen++;
                    if (einblendungen == 1)
                    {
                        Assert.Equal(["Im Medikamentenplan eintragen …", "Medikament anlegen …", "Termin eintragen …"], e.Knoepfe.Select(k => k.Text));
                        Hilfe.Foto(e, "85_visite_aufgaben");
                        return e.Waehlen(0) ? DialogResult.OK : DialogResult.Cancel;
                    }
                    if (einblendungen == 2)
                    {
                        Assert.Equal(2, e.Aufgaben.Count);   // die erledigte ist weg
                        return e.Waehlen(e.Knoepfe.FindIndex(k => k.Text == "Termin eintragen …")) ? DialogResult.OK : DialogResult.Cancel;
                    }
                    Assert.Single(e.Aufgaben);
                    return DialogResult.Cancel;   // „Später“
                case AenderungFenster s:
                    Assert.Equal("Dr. Test (ausgedacht)", s.Arzt.Text);
                    Assert.Equal(Heute(), s.AngeordnetAm.Text);
                    verlaufHinweis = s.Hinweis.Text;
                    s.StufeSetzen(0, Heute(), "0", "0", "2", "0");
                    return s.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                case FacharztTerminFenster t:
                    t.DatumFeld.Text = Heute(5);
                    t.Uhrzeit.Text = "10:00";
                    t.Ort.Text = "Kardiologische Praxis (ausgedacht)";
                    t.MitFahrt.Checked = true;
                    t.Los.Text = "09:30";
                    t.Zurueck.Text = "11:30";
                    Hilfe.Foto(t, "86_facharzttermin");
                    if (!t.Speichern()) Assert.Fail(t.Fehler.Text + t.Warnung.Text);
                    return DialogResult.OK;
                default:
                    throw new InvalidOperationException($"Unerwartetes Fenster: {fenster.Text}");
            }
        }))
            f.Visiten.NeuKnopf.PerformClick();

        Assert.StartsWith("Bitte prüfen:", fehlerLeer);
        Assert.Contains("Arzt fehlt", fehlerLeer);
        Assert.Equal(3, einblendungen);
        Assert.StartsWith($"laut Visite Psychiater – Dr. Test (ausgedacht), {Heute()}", verlaufHinweis);
        // Der Medikamentenplan des Bewohners ist offen; die Stufe steht dort.
        Assert.Equal("Dokumentation", f.AktiverBereich);
        Assert.Equal("Medikamentenplan", f.Bewohner.Akte.Reiter.Aktiv);
        Assert.Equal(ahorn.Id, f.Bewohner.Akte.Bewohner!.Id);
        var aufgaben = u.Speicher.Aufgaben(DateTime.Now, auchErledigte: true).Where(x => x.Text.Contains("Dr. Test")).ToList();
        Assert.Equal(3, aufgaben.Count);
        Assert.False(aufgaben.Single(x => x.Text.StartsWith("Medikation ändern: Quetiapin")).Offen);
        Assert.False(aufgaben.Single(x => x.Text.StartsWith("Termin bei Kardiologie")).Offen);
        Assert.True(aufgaben.Single(x => x.Text.StartsWith("Neues Medikament anlegen")).Offen);
        var heute = DateOnly.FromDateTime(DateTime.Now);
        Assert.Contains(u.Speicher.Kalender(heute, heute.AddDays(7), u.Demo("Birke").Id), k => k.Aktuell.Titel == "Facharzt: Kardiologie");
        Assert.Contains(u.Speicher.Fahrten(heute.AddDays(5), heute.AddDays(5)), x => x.Bewohner.Contains(u.Demo("Birke").Id!.Value));

        // Später über „Aufgaben“: Medikament anlegen (Name vorbelegt), dann gleich die Verordnung.
        f.BereichWaehlen("Aufgaben");
        var karte = f.Aufgaben.Karten.Single(k => k.Aufgabe.Text.StartsWith("Neues Medikament anlegen: Beispielsalbe"));
        Assert.True(karte.AktionKnopf.Visible);
        Assert.Contains("aus einer Visite", karte.AccessibleName + string.Join(" ", karte.Controls.OfType<Label>().Select(l => l.Text)));
        Hilfe.Foto(f, "87_aufgaben_visite");
        using (new DialogErsatz(fenster =>
        {
            switch (fenster)
            {
                case MedikamentFenster m:
                    Assert.Equal("Beispielsalbe (ausgedacht)", m.Wirkstoff.Text);
                    if (!m.Speichern()) Assert.Fail(m.Fehler.Text);
                    return DialogResult.OK;
                case VerordnungFenster v:
                    Assert.Contains("Beispielsalbe", (string)v.MedikamentWahl.SelectedItem!);
                    Assert.Equal("Dr. Test (ausgedacht)", v.Arzt.Text);
                    v.Mengen[0].Text = "1";
                    if (!v.Speichern()) Assert.Fail(v.Fehler.Text);
                    return DialogResult.OK;
                default:
                    throw new InvalidOperationException($"Unerwartetes Fenster: {fenster.Text}");
            }
        }))
            karte.AktionKnopf.PerformClick();
        Assert.False(u.Speicher.AufgabeLesen(karte.Aufgabe.Id)!.Offen);
        Assert.Contains(u.Speicher.Verordnungen(ahorn.Id!.Value), v => v.Medikament.StartsWith("Beispielsalbe"));
        Assert.Equal("Medikamentenplan", f.Bewohner.Akte.Reiter.Aktiv);
    });

    [Fact]
    public void Ohne_Recht_ist_der_Knopf_gesperrt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });   // Assistenzkraft: seit 0.33.0 keine Visiten, Medikation nein
        var jetzt = DateTime.Now;
        u.Speicher.VisiteSpeichern(VisitenRegeln.AusEingabe(new VisiteEingabe
        {
            Datum = Heute(), Art = VisitenRegeln.Hausarzt, Arzt = "Dr. Ulme (ausgedacht)", Kuerzel = "KL",
            Eintraege = [new VisiteEintragEingabe { BewohnerId = u.Demo("Clematis").Id!.Value, Besprochen = "Schlaf besprochen.", Anordnungen = [(AnordnungArt.Pause, null, "Mirtazapin")] }],
        }, jetzt));
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Visiten");
        Assert.Equal(RechteRegeln.NurFachkraftText, Stil.Sperrgrund(f.Visiten.NeuKnopf));
        f.BereichWaehlen("Aufgaben");
        var karte = f.Aufgaben.Karten.Single(k => k.Aufgabe.Text.StartsWith("Medikation ändern: Mirtazapin"));   // Freitext, aber in der Datenbank
        Assert.Equal("Im Medikamentenplan eintragen …", karte.AktionKnopf.Text);
        Assert.Equal(RechteRegeln.NurFachkraftText, Stil.Sperrgrund(karte.AktionKnopf));
    });
}
