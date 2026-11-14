using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Mitarbeiter, Anmeldung und Rechte: Anmelden mit Kürzel, Knöpfe ohne Recht (ausgeblendet, Sperrgrund bleibt
// lesbar), Rechte-Tabelle, Mitarbeiter im eigenen Bereich verwalten.
[Collection("Oberflaeche")]
public class RechteOberflaechenTests
{
    [Fact]
    public void Anmelden_mit_Kuerzel() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using (var fenster = new AnmeldenFenster(u.Speicher, ""))
        {
            Hilfe.Zeigen(fenster);
            Assert.StartsWith("Anmelden", fenster.Text);
            Assert.Equal("Beenden", fenster.BeendenKnopf.Text);
            Assert.Equal(29, fenster.DemoListe.Items.Count);   // nur aktive Mitarbeiter (mit TST; 14 mehr seit Schritt 26h, TH seit 29n, QS seit 44)
            fenster.Kuerzel.Text = "ZZ";
            Assert.False(fenster.Anmelden());
            Assert.Contains("nicht bekannt", fenster.Fehler.Text);
            fenster.Kuerzel.Text = "HI";
            Assert.False(fenster.Anmelden());
            Assert.Contains("ausgeschieden", fenster.Fehler.Text);
            fenster.DemoListe.SelectedIndex = 0;
            Assert.Equal(u.Speicher.AlleMitarbeiter().First(m => m.Aktiv).Kuerzel, fenster.Kuerzel.Text);
            Hilfe.Foto(fenster, "60_anmelden");
            Assert.True(fenster.Anmelden());
            Assert.NotNull(fenster.Angemeldet);
        }

        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("");
        Assert.Equal("Anmelden …", f.Kopf.AbmeldenKnopf.Text);
        using (new DialogErsatz(fenster =>
        {
            var anmelden = Assert.IsType<AnmeldenFenster>(fenster);
            Assert.StartsWith("Anmelden", anmelden.Text);
            anmelden.Kuerzel.Text = "op";
            return anmelden.Anmelden() ? DialogResult.OK : DialogResult.Cancel;
        }))
            f.Kopf.AbmeldenKnopf.PerformClick();
        Assert.Equal("Angemeldet: OP – Olga Pirol", f.Kopf.BenutzerText);
        Assert.Equal("Abmelden (F12)", f.Kopf.AbmeldenKnopf.Text);
        f.BenutzerWaehlen("");
        Assert.Equal("Nicht angemeldet", f.Kopf.BenutzerText);
        f.EinstellungenOeffnen("System");   // „Demo-Daten zurücksetzen“ steht seit Schritt 29 nur noch dort
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(f.Einstellungsseite.DemoKnopf));
    });

    [Fact]
    public void Ohne_Recht_ausgeblendet_ohne_Anmeldung_grau() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var akte = f.Bewohner.Akte;
        Button Knopf(string text) => f.Kopf.Knoepfe.Single(k => k.Text == text);

        // Nicht angemeldet: alles grau, mit Grund.
        f.BereichWaehlen("Übersicht");
        var menue = f.Uebersicht.Liste.ContextMenuStrip!;
        Assert.False(menue.Items[1].Enabled);
        Assert.Equal($"Neuer Bericht … – {RechteRegeln.NichtAngemeldetText}", menue.Items[1].Text);

        // Die Übersicht zeigt den gewählten Bereich (Vorgabe: der des Rechners), umschaltbar je nach Recht.
        f.BenutzerWaehlen("PR");
        Assert.Equal(23, f.Uebersicht.Zeilen.Count);
        Assert.StartsWith("R1 · 23 Bewohner · 2 Betten frei · ", f.Kopf.KleinText);
        f.BereichsWahl.Waehlen("R2");
        Assert.All(f.Uebersicht.Zeilen, z => Assert.Equal("R2", z.Bewohner.Wohnbereich));
        f.BenutzerWaehlen("MN");
        Assert.Equal(23, f.Uebersicht.Zeilen.Count);   // die Wahl gilt bis zum Abmelden

        // OP (Therapie): Akte ansehen ja, bearbeiten nein; Berichte schreiben ja.
        f.BenutzerWaehlen("OP");
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(Knopf("Bearbeiten …")));
        Assert.Null(Stil.Sperrgrund(Knopf("Neuer Bericht …")));
        using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
        {
            Knopf("Bearbeiten …").PerformClick();
            Assert.Empty(ersatz.Titel);   // gesperrt: kein Fenster
        }
        Hilfe.Foto(f, "64_akte_gesperrt");

        // KL: Mitarbeiter Wohnbereich ohne Fachkraft – Medikamentenplan nur ansehen.
        f.BenutzerWaehlen("KL");
        Assert.Null(Stil.Sperrgrund(Knopf("Bearbeiten …")));   // die Akte aktualisiert sich beim Wechsel
        akte.Reiter.Waehlen("Medikamentenplan");
        Assert.Equal(RechteRegeln.NurFachkraftText, Stil.Sperrgrund(akte.Medikation.NeuKnopf));
        string? pauseGesperrt = null;
        bool gewaehlt = true;
        using (new DialogErsatz(fenster =>
        {
            var d = Assert.IsType<VerordnungDetailFenster>(fenster);
            pauseGesperrt = Stil.Sperrgrund(d.Karte.PauseKnopf);
            gewaehlt = d.Waehlen(AenderungArt.Pause);
            return DialogResult.Cancel;
        }))
            akte.Medikation.Oeffnen(akte.Medikation.Eintraege[0]);
        Assert.Equal(RechteRegeln.NurFachkraftText, pauseGesperrt);
        Assert.False(gewaehlt);

        // CD: Fachkraft – Medikamentenplan ja; Therapien nein; Medikamentendatenbank anlegen ja, verwalten nein.
        f.BenutzerWaehlen("CD");
        akte.Reiter.Waehlen("Medikamentenplan");
        Assert.Null(Stil.Sperrgrund(akte.Medikation.NeuKnopf));
        f.BereichWaehlen("Therapie");
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(f.Therapie.NeuKnopf));
        Assert.All(f.Therapie.Karten, k => Assert.NotNull(Stil.Sperrgrund(k.ZuteilenKnopf)));
        using (var db = new MedikamenteFenster(u.Speicher))
        {
            Assert.Null(Stil.Sperrgrund(db.Seite.NeuKnopf));
            Assert.All(db.Seite.Karten, k => Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(k.AendernKnopf)));
            Assert.All(db.Seite.Karten, k => Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(k.SperrenKnopf)));
            Assert.False(db.Seite.Sperren(db.Seite.Karten[0].Medikament, true));
        }
        f.Einstellungsseite.Zeigen();
        Assert.NotNull(Stil.Sperrgrund(f.Einstellungsseite.DemoKnopf));

        // Rechte ändern wirkt sofort (Rechte-Tabelle, angemeldet als Geschäftsführung).
        f.BenutzerWaehlen("UT");
        using (var rechte = new RechteFenster(u.Speicher))
        {
            Assert.False(rechte.Tabelle.ReadOnly);
            Assert.True(rechte.Setzen(rechte.Zeile(Funktionen.TherapienBearbeiten), rechte.Spalte("Mitarbeiter Wohnbereich"), true));
            Hilfe.Foto(rechte, "61_rechte");
        }
        f.BenutzerWaehlen("CD");
        f.BereichWaehlen("Therapie");
        Assert.Null(Stil.Sperrgrund(f.Therapie.NeuKnopf));
        using (var rechte = new RechteFenster(u.Speicher))
        {
            Assert.True(rechte.Tabelle.ReadOnly);   // CD darf Rechte nur ansehen
            Assert.StartsWith("Nur ansehen:", rechte.Gesperrt.Text);
            Assert.False(rechte.Setzen(rechte.Zeile(Funktionen.Module), rechte.Spalte("Mitarbeiter Wohnbereich"), true));
        }
    });

    [Fact]
    public void Mitarbeiter_im_eigenen_Bereich_verwalten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung R1
        using (var fenster = new MitarbeiterFenster(u.Speicher))
        {
            Hilfe.Zeigen(fenster);
            Assert.Equal(30, fenster.Liste.Items.Count);   // mit QS (Bauplan 44)
            Assert.Null(Stil.Sperrgrund(fenster.NeuKnopf));
            fenster.Waehlen("GH");
            Assert.Equal("Nur Mitarbeiter des eigenen Bereichs.", Stil.Sperrgrund(fenster.BearbeitenKnopf));
            fenster.Waehlen("PR");   // R1 und B
            Assert.Null(Stil.Sperrgrund(fenster.BearbeitenKnopf));
            Hilfe.Foto(fenster, "62_mitarbeiter");
            using var neu = fenster.FormularErstellen(null);
            Assert.False(neu.Bereiche["R2"].Enabled);   // nur eigene Bereiche
            Assert.True(neu.Bereiche["R1"].Enabled);
            neu.Kuerzel.Text = "qx";
            neu.Vorname.Text = "Quirin";
            neu.Nachname.Text = "Kranich";
            Assert.False(neu.Speichern());
            Assert.Contains("mindestens einen Bereich", neu.Fehler.Text);
            neu.Bereiche["R1"].Checked = true;
            neu.Positionen["Mitarbeiter Wohnbereich"].Checked = true;
            Hilfe.Foto(neu, "63_mitarbeiter_formular");
            Assert.True(neu.Speichern());
            Assert.Equal("QX", neu.Gespeichert!.Kuerzel);
        }
        Assert.Equal(["R1"], u.Speicher.MitarbeiterLesen("QX")!.Bereiche);

        // CD (ohne Mitarbeiterverwaltung): in den Einstellungen gesperrt; Bereiche und Positionen nur ansehen.
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using (var einstellungen = new EinstellungenSeite(u.Speicher))
        {
            einstellungen.Zeigen();
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(einstellungen.MitarbeiterKnopf));
        }
        using (var listen = new ListenFenster(u.Speicher))
            Assert.All(listen.Knoepfe, k => Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(k)));

        // WZ (Verwaltung): darf Bereiche anlegen.
        Einstellungen.Aendern(w => w with { Kuerzel = "WZ" });
        using (var listen = new ListenFenster(u.Speicher))
        {
            listen.Name_.Text = "R4";
            Assert.True(listen.Ausfuehren(() => u.Speicher.ListeneintragAnlegen(Liste.Bereiche, listen.Name_.Text)));
            Assert.Contains("R4", listen.Listen[Liste.Bereiche].Items.Cast<string>().Last());
        }
    });
}
