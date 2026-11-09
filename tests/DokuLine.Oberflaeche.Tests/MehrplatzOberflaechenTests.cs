using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Mehrplatzbetrieb (Bauplan Schritt 17), getestet wie empfohlen mit zwei Fenstern auf derselben
// Datenbank: Abgleich nach 30 Sekunden, Konflikt-Hinweis, Bereiche, Statusmeldungen, Einstellungen.
[Collection("Oberflaeche")]
public class MehrplatzOberflaechenTests
{
    [Fact]
    public void Zwei_Fenster_sehen_die_Aenderungen_des_anderen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "AB" });
        using var zweiter = new Speicher(u.Speicher.Pfad);
        using var f1 = new Hauptfenster(u.Speicher);
        using var f2 = new Hauptfenster(zweiter);
        Hilfe.Zeigen(f1);
        Hilfe.Zeigen(f2);
        f2.BereichWaehlen("Übersicht");
        Assert.False(f2.AenderungenVonAnderen());   // nichts geändert: nichts neu lesen
        bool Status() => f2.Uebersicht.Zeilen.Single(k => k.Bewohner.Nachname == "Birke").Marken.Any(m => m.Text.StartsWith("Status: beim Friseur, bis "));
        Assert.False(Status());

        f1.Bewohner.AkteZeigen(u.Demo("Birke").Id!.Value);
        using (new DialogErsatz(fenster =>
        {
            var s = Assert.IsType<StatusmeldungFenster>(fenster);
            Assert.False(s.AufhebenKnopf.Enabled);   // Birke hat noch keine
            s.Inhalt.Text = "beim Friseur";
            return s.Setzen() ? DialogResult.OK : DialogResult.Cancel;
        }))
            f1.Kopf.Knoepfe.Single(k => k.Text == "Status …").PerformClick();
        Assert.StartsWith("Status: beim Friseur · bis ", f1.Bewohner.Akte.StatusText);
        Assert.EndsWith("R1, AB um " + DateTime.Now.ToString("HH:mm"), f1.Bewohner.Akte.StatusText);

        Assert.True(f2.AenderungenVonAnderen());    // spätestens nach 30 Sekunden
        Assert.True(Status());
        Assert.False(f2.AenderungenVonAnderen());

        // Aufgaben zwischen den Fenstern: der Zähler des anderen stimmt nach dem Abgleich.
        int vorher = f2.Navigation.Single(n => n.Text == "Aufgaben").Anzahl ?? 0;
        u.Speicher.AufgabeSpeichern(AufgabenRegeln.AusEingabe(new AufgabeEingabe { Text = "Fenster putzen", Verfasser = "AB" }, DateTime.Now));
        f2.AenderungenVonAnderen();
        Assert.Equal(vorher + 1, f2.Navigation.Single(n => n.Text == "Aufgaben").Anzahl);
    });

    [Fact]
    public void Konflikt_in_der_Akte_zeigt_beide_Fassungen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var zweiter = new Speicher(u.Speicher.Pfad);
        var birke = u.Demo("Birke");
        using var formular = new BewohnerFormular(u.Speicher, birke);   // Arbeitsplatz 1 öffnet …
        zweiter.BewohnerSpeichern(birke with { Zimmer = "204" }, ausgangsfassung: birke);   // … Arbeitsplatz 2 speichert (Umzug nach R2)
        Assert.True(formular.BettWaehlen("105 – Bett Tür"));
        string andere = "", meine = "";
        using (new DialogErsatz(fenster =>
        {
            var gezeigt = Assert.IsType<KonfliktFenster>(fenster);
            (andere, meine) = (gezeigt.Andere.Text, gezeigt.Meine.Text);   // vor dem Schließen lesen
            Hilfe.Foto(gezeigt, "54_konflikt");
            return DialogResult.Cancel;
        }))
            Assert.False(formular.Speichern());   // Abbrechen: nichts gespeichert, Eingaben bleiben
        Assert.Contains("Zimmer: 204", andere);
        Assert.Contains("Zimmer: 105 (Bett Tür)", meine);
        Assert.Equal("204", u.Demo("Birke").Zimmer);
        Assert.Equal("105", formular.Eingabe().Zimmer);   // Eingaben bleiben

        using (new DialogErsatz(fenster =>
        {
            ((KonfliktFenster)fenster).Waehlen(KonfliktWahl.MeineSpeichern);
            return DialogResult.OK;
        }))
            Assert.True(formular.Speichern());
        Assert.Equal("105", u.Demo("Birke").Zimmer);
        Assert.Equal("R1", u.Demo("Birke").Wohnbereich);   // der Bereich folgt dem Bett
    });

    [Fact]
    public void Reflexion_andere_Fassung_uebernehmen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var zweiter = new Speicher(u.Speicher.Pfad);
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo("Eiche").Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Reflexion");
        var r = f.Bewohner.Akte.Reflexion;
        zweiter.ReflexionSpeichern(ReflexionRegeln.AusEingabe(u.Demo("Eiche").Id!.Value, r.Monat, ["Vom anderen Arbeitsplatz.", "", "", "", ""], false, "GH", DateTime.Now));
        r.Bereiche[0].Feld.Text = "Meine Eingabe.";
        using (new DialogErsatz(fenster =>
        {
            ((KonfliktFenster)fenster).Waehlen(KonfliktWahl.AndereUebernehmen);
            return DialogResult.OK;
        }))
            Assert.False(r.Speichern(false));
        Assert.Equal("Vom anderen Arbeitsplatz.", r.Bereiche[0].Feld.Text);
    });

    [Fact]
    public void Uebersicht_eigener_Bereich_oder_alle() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var s = f.Uebersicht;
        Assert.Equal(23, s.Zeilen.Count);
        Assert.All(s.Zeilen, k => Assert.Equal("R1", k.Bewohner.Wohnbereich));
        Assert.StartsWith("R1 · 23 Bewohner · 2 Betten frei · Stand ", f.Kopf.KleinText);
        Assert.False(s.BereichHinweis.Visible);
        // Statusmeldung eines anderen Bereichs (Therapie) auf der Karte, vorne.
        Assert.StartsWith("Status: in der Holzwerkstatt, bis ", s.Zeilen.Single(k => k.Bewohner.Nachname == "Ahorn").Marken[0].Text);

        s.BereichWahl.SelectedItem = UebersichtSeite.AlleBereiche;   // seit Schritt 26l in der Bereichswahl
        Assert.Equal(69, s.Zeilen.Count);
        Assert.StartsWith("alle Bereiche · 69 Bewohner · 6 Betten frei · ", f.Kopf.KleinText);
        var ginster = s.Zeilen.Single(k => k.Bewohner.Nachname == "Ginster");
        Assert.Equal("R2", ginster.Marken[^1].Text);
        Assert.StartsWith("Status: Besuch bei der Familie, bis ", s.Zeilen.Single(k => k.Bewohner.Nachname == "Immergrün").Marken[0].Text);
        Hilfe.Foto(f, "56_uebersicht_alle_bereiche");

        // Ein Bereich ohne eigene Bewohner (z. B. Therapie) sieht alle, mit Hinweis.
        s.BereichWahl.SelectedItem = UebersichtSeite.EigeneBereiche;
        Einstellungen.Aendern(w => w with { Bereich = "Therapie" });
        s.Aktualisieren();
        Assert.Equal(69, s.Zeilen.Count);
        Assert.True(s.BereichHinweis.Visible);
        // In der Dokumentation: ein Bereich ohne eigene Bewohner sieht ebenfalls alle.
        f.BereichWaehlen("Dokumentation");
        Assert.Equal(69, f.Bewohner.Zeilen.Count);
        Assert.Contains("R1", s.Zeilen.Single(k => k.Bewohner.Nachname == "Birke").Marken.Select(m => m.Text));
    });

    [Fact]
    public void Statusmeldung_in_der_Akte_setzen_und_aufheben() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.StartsWith("Status: in der Holzwerkstatt · bis ", akte.StatusText);
        Assert.Contains("· Therapie, OP um ", akte.StatusText);
        Assert.Equal(akte.StatusText, akte.KopfMarken.First());   // vorne im Kopf
        Assert.Equal("R1", akte.Wohnbereich.Text);
        Hilfe.Foto(f, "52_akte_status");
        using (new DialogErsatz(fenster =>
        {
            var s = Assert.IsType<StatusmeldungFenster>(fenster);
            Assert.True(s.AufhebenKnopf.Enabled);
            Assert.Contains("in der Holzwerkstatt", s.Aktuell.Text);
            Assert.Contains("beim Arzttermin", s.Verlauf.Text);   // die ältere steht im Verlauf
            Assert.False(s.Setzen());
            Assert.Contains("Text fehlt.", s.Fehler.Text);
            s.Inhalt.Text = "beim Spaziergang mit Begleitung";
            Hilfe.Foto(s, "53_statusmeldung");
            return s.Aufheben() ? DialogResult.OK : DialogResult.Cancel;
        }))
            Assert.True(f.Bewohner.StatusOeffnen());
        Assert.Null(akte.StatusText);
        Assert.DoesNotContain(akte.KopfMarken, m => m.StartsWith("Status:"));
        Assert.Contains("CD (R1) · aufgehoben", StatusmeldungRegeln.Verlauf(u.Speicher.Statusmeldungen(u.Demo("Ahorn").Id!.Value))[0]);
    });

    [Fact]
    public void Einstellungen_Bereich_und_gemeinsamer_Ordner() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        string ordner = Path.Combine(u.Ordner, "gemeinsam");
        Directory.CreateDirectory(ordner);
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung: darf die Einstellungen des Bereichs
        using (var fenster = new EinstellungenSeite(u.Speicher))
        {
            fenster.Zeigen();
            bool gefragt = false;
            fenster.Rueckfrage = _ => gefragt = true;   // Datenbank-Ort: fragt immer vorher
            Assert.Equal("R1", fenster.Bereich.Text);
            Assert.StartsWith("lokal", fenster.DatenbankOrt.Text);
            Assert.False(fenster.LokalKnopf.Enabled);
            Assert.False(fenster.OrdnerSetzen(Path.Combine(u.Ordner, "fehlt")));
            Assert.Contains("gibt es nicht", fenster.Fehler.Text);
            Assert.True(fenster.OrdnerSetzen(ordner));
            Assert.Equal($"gemeinsamer Ordner: {ordner}", fenster.DatenbankOrt.Text);
            Assert.True(gefragt);
            fenster.Bereich.Text = "R2";
            Assert.True(fenster.BereichUebernehmen());
        }
        var w = Einstellungen.Laden();
        Assert.Equal(("R2", ordner), (w.Bereich, w.DatenbankOrdner));
        using var f = new Hauptfenster(u.Speicher);
        Assert.StartsWith("R2 · Datenbank lokal", f.Arbeitsplatz.Text);
        Assert.EndsWith("neuer Ort nach Neustart", f.Arbeitsplatz.Text);
    });
}
