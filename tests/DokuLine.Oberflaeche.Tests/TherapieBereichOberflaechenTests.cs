using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 29n: Therapie als eigener Bereich – eigene Seitenleiste, Bewohnerleisten nach Wohnbereich
// gruppiert, Seite „Wochenpläne“, Berichte der Therapie mit Angebot, Aufgaben ohne Schicht.
[Collection("Oberflaeche")]
public class TherapieBereichOberflaechenTests
{
    private static readonly string[] Seitenleiste =
        [TherapieSeite.LaufendReiter, TherapieSeite.AngeboteReiter, "Wochenpläne", "Dokumentation", "Aufgaben", "Nachrichten"];

    private static Hauptfenster Oeffnen(Umgebung u)
    {
        Einstellungen.Aendern(w => w with { Kuerzel = "TH", Bereich = TherapieRegeln.AnbieterTherapie });   // Rechner der Therapie
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen(f.StartSeite);
        return f;
    }

    [Fact]
    public void Eigene_Seitenleiste_und_zurueck() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Oeffnen(u);
        Assert.Equal(TherapieRegeln.AnbieterTherapie, Arbeitsbereich.Gewaehlt);
        Assert.Equal(TherapieSeite.LaufendReiter, f.AktiverBereich);
        var sichtbar = f.Navigation.Where(n => n.Visible && n.Text != "Einstellungen").OrderBy(n => n.Top).ToList();
        Assert.Equal(Seitenleiste, sichtbar.Select(n => n.Text));
        for (int i = 1; i < sichtbar.Count; i++)
            Assert.Equal(sichtbar[i - 1].Bottom + Stil.Px(f, Stil.AbstandMini), sichtbar[i].Top);   // ohne Lücke
        Assert.True(f.Navigation.Single(n => n.Text == "Einstellungen").Visible);
        Assert.Equal(TherapieSeite.LaufendReiter, f.Kopf.TitelText);
        Assert.Contains(TherapieRegeln.AnbieterTherapie, f.BereichsWahl.Bereiche);   // die Bereichswahl steht in der Seitenleiste
        Assert.Equal(TherapieRegeln.AnbieterTherapie, f.BereichsWahl.Liste.SelectedItem);
        Assert.False(f.Therapie.Reiter.Visible);
        Hilfe.Foto(f, "130_therapie_laufend");

        f.Navigation.Single(n => n.Text == TherapieSeite.AngeboteReiter).Ausloesen();
        Assert.Equal(TherapieSeite.AngeboteReiter, f.Therapie.Reiter.Aktiv);
        Assert.NotEmpty(f.Therapie.Karten);
        Assert.Equal(TherapieSeite.AngeboteReiter, f.Kopf.TitelText);
        // Klick in die Tagesleiste wechselt den Reiter: Die Seitenleiste zeigt mit.
        f.Navigation.Single(n => n.Text == TherapieSeite.LaufendReiter).Ausloesen();
        var termin = f.Therapie.Laufend.Leiste.Felder.FirstOrDefault().Termin;
        if (termin is not null)
        {
            f.Therapie.Laufend.Leiste.Oeffnen(termin);
            Assert.Equal(TherapieSeite.AngeboteReiter, f.AktiverBereich);
            Assert.True(f.Navigation.Single(n => n.Text == TherapieSeite.AngeboteReiter).Aktiv);
        }

        // Zurück in einen Wohnbereich: die gewohnte Seitenleiste; die Therapie-Seite bleibt offen.
        f.Navigation.Single(n => n.Text == TherapieSeite.LaufendReiter).Ausloesen();
        f.BereichsWahl.Waehlen("R2");
        Assert.Equal("R2", Arbeitsbereich.Gewaehlt);
        Assert.Equal("Therapie", f.AktiverBereich);
        Assert.True(f.Navigation.Single(n => n.Text == "Übersicht").Visible);
        Assert.All(Hauptfenster.TherapieBereiche, b => Assert.False(f.Navigation.Single(n => n.Text == b).Visible));
        Assert.True(f.Therapie.Reiter.Visible);
        // Aus der Übersicht in den Bereich Therapie: Die Übersicht gibt es dort nicht, es beginnt mit den laufenden Therapien.
        f.BereichWaehlen("Übersicht");
        f.ArbeitsbereichWaehlen(TherapieRegeln.AnbieterTherapie);
        Assert.Equal(TherapieSeite.LaufendReiter, f.AktiverBereich);
        // Dokumentation bleibt Dokumentation.
        f.BereichWaehlen("Dokumentation");
        f.ArbeitsbereichWaehlen("R1");
        Assert.Equal("Dokumentation", f.AktiverBereich);
    });

    [Fact]
    public void Dokumentation_nach_Wohnbereich_gruppiert() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Oeffnen(u);
        f.Navigation.Single(n => n.Text == "Dokumentation").Ausloesen();
        var b = f.Bewohner;
        var heute = DateOnly.FromDateTime(DateTime.Today);
        var mit = TherapieRegeln.MitTherapie(u.Speicher.AlleBewohner(), u.Speicher.Angebote(), heute);
        Assert.Equal(mit.Count, b.Zeilen.Count);
        Assert.True(b.AlleKnopf.Visible);
        var ueberschriften = b.Namen.Items.OfType<ListenUeberschrift>().Select(x => x.Text).ToList();
        Assert.Equal(mit.Select(x => x.Wohnbereich).Distinct().Order().ToList(), ueberschriften);
        Assert.IsType<ListenUeberschrift>(b.Namen.Items[0]);
        Assert.Equal(1, b.Namen.SelectedIndex);   // die Überschrift ist nicht wählbar
        Assert.NotNull(b.Gewaehlt);
        b.Namen.SelectedIndex = 0;                 // Klick auf die Überschrift: der Bewohner darunter
        Assert.Equal(1, b.Namen.SelectedIndex);
        // Pfeil nach oben über eine Überschrift: der letzte Bewohner der Gruppe davor.
        int zweite = b.Namen.Items.Cast<object>().Select((x, i) => (x, i)).Where(p => p.x is ListenUeberschrift).Skip(1).Select(p => p.i).FirstOrDefault();
        if (zweite > 0)
        {
            b.Namen.SelectedIndex = zweite + 1;
            b.Namen.SelectedIndex = zweite;
            Assert.Equal(zweite - 1, b.Namen.SelectedIndex);
            Assert.Equal(b.Namen.Items[zweite - 1], $"{b.Gewaehlt!.Nachname}, {b.Gewaehlt.Vorname}");
        }
        Assert.True(b.Waehlen(mit[^1].Id!.Value));
        Assert.Equal(mit[^1].Id, b.Gewaehlt!.Id);
        Hilfe.Foto(f, "131_therapie_dokumentation");

        b.AlleKnopf.Checked = true;
        Assert.Equal(u.Speicher.AlleBewohner().Count, b.Zeilen.Count);
        Assert.Equal(["R1", "R2", "R3"], b.Namen.Items.OfType<ListenUeberschrift>().Select(x => x.Text));
        Assert.Contains("Therapie · ", f.Kopf.KleinText);
    });

    [Fact]
    public void Wochenplaene_lesen_und_drucken() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Oeffnen(u);
        f.Navigation.Single(n => n.Text == "Wochenpläne").Ausloesen();
        var w = f.Wochenplaene;
        Assert.Equal("Wochenpläne", f.Kopf.TitelText);
        Assert.NotEmpty(w.Zeilen);
        Assert.NotNull(w.Gewaehlt);
        Assert.True(w.Plan.NurLesen);
        Assert.False(w.Plan.NeuKnopf.Visible);
        Assert.Contains(w.Plan.Termine, t => t.Quelle == TerminQuelle.Therapie);   // nur Bewohner mit Angebot der Therapie
        // Eine Therapie des eigenen Anbieters lässt sich ändern, eine eines Wohnbereichs nicht.
        var therapie = w.Plan.Termine.Select(w.Plan.AngebotZu).OfType<Angebot>().First(a => a.Anbieter == TherapieRegeln.AnbieterTherapie);
        Assert.Null(u.Speicher.AngebotGrund("TH", therapie.Anbieter));
        Assert.NotNull(u.Speicher.AngebotGrund("TH", "R1"));
        Hilfe.Foto(f, "132_therapie_wochenplaene");

        var zweiter = w.Zeilen[^1];
        w.Namen.SelectedIndex = w.Namen.Items.IndexOf($"{zweiter.Nachname}, {zweiter.Vorname}");
        Assert.Equal(zweiter.Id, w.Gewaehlt!.Id);
        w.Suche.Text = "zzz";
        Assert.Empty(w.Zeilen);
        Assert.False(w.Plan.Visible);
        Assert.Contains("0 von ", f.Kopf.KleinText);
    });

    [Fact]
    public void Bericht_der_Therapie_mit_Angebot() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Oeffnen(u);
        var heute = DateOnly.FromDateTime(DateTime.Today);
        var b = TherapieRegeln.MitTherapie(u.Speicher.AlleBewohner(), u.Speicher.Angebote(), heute)[0];
        string angebot = TherapieRegeln.AngeboteVon(b.Id!.Value, u.Speicher.Angebote(), heute)[0];
        using var formular = new BerichtFormular(u.Speicher, b);
        Assert.Equal(DokuRegeln.ArtTherapie, formular.Art.SelectedItem);   // im Bereich Therapie vorgewählt
        Assert.True(formular.AngebotGefragt);
        Assert.Equal(angebot, formular.AngebotWahl.SelectedItem);
        formular.Bereiche[2].Checked = true;
        formular.Inhalt.Text = "War die ganze Zeit dabei.";
        Assert.True(formular.Speichern());
        var bericht = u.Speicher.Berichte(b.Id!.Value).First(r => r.Text.EndsWith("War die ganze Zeit dabei.", StringComparison.Ordinal));
        Assert.Equal($"Therapie {angebot}: War die ganze Zeit dabei.", bericht.Text);
        Assert.Equal(DokuRegeln.ArtTherapie, bericht.Art);

        // Ohne Text bleibt der Text leer (sonst ginge der Name des Angebots als Bericht durch).
        using var leer = new BerichtFormular(u.Speicher, b);
        leer.Bereiche[2].Checked = true;
        Assert.False(leer.Speichern());
        // Eine andere Art: kein Angebot.
        leer.Art.SelectedItem = DokuRegeln.ArtAlltag;
        Assert.False(leer.AngebotGefragt);
    });

    [Fact]
    public void Aufgabe_der_Therapie_ohne_Schicht() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Oeffnen(u);
        f.Navigation.Single(n => n.Text == "Aufgaben").Ausloesen();
        Assert.Equal(2, f.Aufgaben.Offen);   // die zwei Aufgaben des Teams Therapie aus der Demo
        using var fenster = new AufgabeFenster(u.Speicher, null, null);
        Assert.Equal(TherapieRegeln.AnbieterTherapie, fenster.BereichWahl.SelectedItem);
        Assert.True(fenster.WerZeitHat.Checked);
        Assert.False(fenster.NaechsteSchicht.Enabled);
        Assert.False(fenster.BestimmteSchicht.Enabled);
        Assert.Equal(Datum.Text(DateOnly.FromDateTime(DateTime.Today).AddDays(1)), fenster.FaelligDatum.Text);
    });

    [Fact]
    public void Laufende_Therapien_schlank() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        var b = u.Demo("Ahorn");
        var p = new TafelTeilnehmer(b, 1, TafelStatus.NochOffen, null);
        using var voll = new Kaertchen(p, "AB", null);
        using var schlank = new Kaertchen(p, "AB", null, schlank: true);
        Assert.Contains("Bezugspflege", voll.AccessibleName);
        Assert.DoesNotContain("Bezugspflege", schlank.AccessibleName);
        Assert.DoesNotContain(b.Zimmer, schlank.AccessibleName);
        Assert.Contains(b.Wohnbereich, schlank.AccessibleName);
    });
}
