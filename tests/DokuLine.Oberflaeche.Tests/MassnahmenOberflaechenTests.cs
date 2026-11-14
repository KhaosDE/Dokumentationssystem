using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 39: Reiter „Maßnahmen“ (Lebensbereiche, Maßnahmen, Medikamentenplan lesend), anlegen mit der Frage nach
// Medikamenten, bestätigen in der Aufgaben-Seite, Zusammenfassung in der Dokumentation, Zeilen in der Reflexion.
[Collection("Oberflaeche")]
public class MassnahmenOberflaechenTests
{
    private static MassnahmeFassung Jetzt(string titel) => new()
    {
        Titel = titel, Lebensbereich = 2, Art = "Anleitung", Rhythmus = MassnahmeRhythmus.Taeglich, Schicht = SchichtTermin.Zu(DateTime.Now).Schicht,
        Minuten = 20, GiltAb = DateOnly.FromDateTime(DateTime.Now).AddDays(-1),
    };

    [Fact]
    public void Reiter_anlegen_beenden_und_Lebensbereich() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Demo("Ahorn");
        long id = b.Id!.Value;
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(id);
        var akte = f.Bewohner.Akte;
        Assert.Contains(MassnahmenAnsicht.ReiterName, akte.Reiter.Namen);
        akte.Reiter.Waehlen(MassnahmenAnsicht.ReiterName);
        var m = akte.Massnahmen;
        Assert.True(m.Visible);
        Assert.Equal(5, m.Lebensbereiche.Items.Count);
        Assert.True(m.Liste.Items.Count >= 2);
        Assert.StartsWith("Aus dem Medikamentenplan", m.MedikamentenplanText.Text);
        Assert.Null(Stil.Sperrgrund(m.NeuKnopf));
        Hilfe.Foto(f, "155_massnahmen");

        // Ein Medikament im Titel: Rückfrage; „nein“ speichert nichts.
        string medikament = u.Speicher.MedikamentNamen().First(n => n.Length >= 4);
        int vorher = u.Speicher.Massnahmen(id).Count;
        using (var fenster = new MassnahmeFenster(u.Speicher, b))
        {
            string? frage = null;
            fenster.TrotzMedikament = text => { frage = text; return false; };
            fenster.Titel.Text = $"Morgens {medikament}";
            fenster.Lebensbereich.SelectedIndex = 4;
            fenster.Art.SelectedIndex = 0;
            Assert.False(fenster.Speichern());
            Assert.Contains("ärztlich verordnet", frage);
            fenster.Titel.Text = "Wochenplan gemeinsam ansehen (Test)";
            fenster.Rhythmus.SelectedIndex = 1;   // an Wochentagen
            Assert.False(fenster.Speichern());     // ohne Wochentag
            fenster.Wochentage[0].Checked = true;
            Hilfe.Foto(fenster, "156_massnahme_anlegen");
            Assert.True(fenster.Speichern());
        }
        var neu = u.Speicher.Massnahmen(id).Single(x => x.Fassungen[0].Titel == "Wochenplan gemeinsam ansehen (Test)");
        Assert.Equal(vorher + 1, u.Speicher.Massnahmen(id).Count);
        Assert.Equal([DayOfWeek.Monday], neu.Fassungen[0].Wochentage);

        // Beenden mit Grund.
        m.Zeigen(b);
        m.Liste.Items.Cast<ListViewItem>().First(i => i.Tag is Massnahme x && x.Id == neu.Id).Selected = true;
        m.GrundFragen = _ => "nicht mehr nötig";
        Assert.True(m.Beenden());
        Assert.DoesNotContain(u.Speicher.Massnahmen(id), x => x.Id == neu.Id);

        // Doppelklick auf einen Lebensbereich: Dokumentation mit diesem Filter.
        bool gewuenscht = false;
        m.LebensbereichGewuenscht += _ => gewuenscht = true;
        m.LebensbereichOeffnen(2);
        Assert.True(gewuenscht);
        Assert.False(f.Bewohner.AkteSichtbar);
        Assert.Equal(2, f.Bewohner.Dokumentation.Filter.Bereich);
    });

    [Fact]
    public void Abhaken_in_den_Aufgaben_Dokumentation_und_Reflexion() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });   // Assistenzkraft: bestätigen ja
        var b = u.Demo("Ahorn");
        long id = b.Id!.Value;
        var neu = u.Speicher.MassnahmeAnlegen(id, Jetzt("Zimmer lüften (Test)"), "EF", DateTime.Now);
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen(b.Wohnbereich);
        f.BereichWaehlen("Aufgaben");
        var liste = f.Aufgaben.Massnahmen;
        Assert.StartsWith("MASSNAHMEN · ", f.Aufgaben.Abschnitte[0]);
        var zeile = liste.Liste.Items.Cast<ListViewItem>().Single(i => i.Tag is MassnahmeTermin t && t.Massnahme.Id == neu.Id);
        Assert.Equal("offen", zeile.SubItems[5].Text);
        zeile.Selected = true;
        Hilfe.Foto(f, "157_massnahmen_aufgaben");
        Assert.True(liste.Durchgefuehrt());
        var bestaetigt = Assert.Single(u.Speicher.MassnahmeBestaetigungen(id), x => x.MassnahmeId == neu.Id);
        Assert.Equal(("KL", 20, MassnahmeErgebnis.Durchgefuehrt), (bestaetigt.Kuerzel, bestaetigt.Minuten, bestaetigt.Ergebnis));
        zeile = f.Aufgaben.Massnahmen.Liste.Items.Cast<ListViewItem>().Single(i => i.Tag is MassnahmeTermin t && t.Massnahme.Id == neu.Id);
        Assert.StartsWith("durchgeführt · KL", zeile.SubItems[5].Text);
        zeile.Selected = true;
        Assert.NotNull(Stil.Sperrgrund(f.Aufgaben.Massnahmen.DurchgefuehrtKnopf));   // schon bestätigt

        // Dokumentation: je Tag zusammengefasst, aufklappbar; ohne Häkchen „Maßnahmen“ verschwinden sie.
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.Dokumentation.Zeigen(b);
        var doku = f.Bewohner.Dokumentation;
        var heute = doku.MassnahmenZeilen.First();
        Assert.Contains("durchgeführt", heute.Text);
        Assert.DoesNotContain(doku.Zeilen, z => z.Visible && z.Bericht.Art == MassnahmenRegeln.ArtBericht);
        heute.Umschalten();
        Assert.Contains(doku.Zeilen, z => z.Visible && z.Bericht.Text.StartsWith("Maßnahme „Zimmer lüften (Test)“", StringComparison.Ordinal));
        doku.Massnahmen.Checked = false;
        Assert.Empty(doku.MassnahmenZeilen);
        Assert.DoesNotContain(doku.Berichte, x => x.Art == MassnahmenRegeln.ArtBericht);

        // Reflexion: je Maßnahme eine Zeile im Lebensbereich.
        f.Bewohner.AkteZeigen(id);
        f.Bewohner.Akte.Reiter.Waehlen("Reflexion");
        var r = f.Bewohner.Akte.Reflexion;
        r.MonatWaehlen(DateOnly.FromDateTime(DateTime.Now));
        Assert.Contains(r.Bereiche[1].QuellenTexte, t => t.StartsWith("Maßnahme Zimmer lüften (Test): 1 von 1 durchgeführt", StringComparison.Ordinal));
    });

    // Bauplan 40: Kasten „Laufender Dienst“ der Übersicht mit den Maßnahmen, Spalte der Bewohnerliste, Umschalter.
    [Fact]
    public void Uebersicht_Kasten_abhaken_erledigte_und_Spalte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        var b = u.Demo("Ahorn");
        long id = b.Id!.Value;
        var dienst = SchichtTermin.Zu(DateTime.Now);
        var neu = u.Speicher.MassnahmeAnlegen(id, Jetzt("Fenster öffnen (Test)") with { Uhrzeit = TimeOnly.FromDateTime(dienst.Beginn) }, "EF", DateTime.Now);
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen(b.Wohnbereich);
        f.BereichWaehlen("Übersicht");
        var s = f.Uebersicht;
        Assert.Contains(s.MassnahmenTermine, t => t.Massnahme.Id == neu.Id && t.Offen);
        var zeile = s.MassnahmenZeilen.Single(z => z.Termin.Massnahme.Id == neu.Id);
        Assert.NotNull(zeile.Ueberfaellig);   // die Uhrzeit (Dienstbeginn) ist vorbei
        Assert.EndsWith("offen", s.Zeilen.Single(z => z.Bewohner.Id == id).MassnahmenText);
        Assert.True(s.Dienst.Anzahl >= 1);
        Hilfe.Foto(f, "158_uebersicht_massnahmen");

        s.MassnahmeBestaetigen(zeile.Termin, MassnahmeErgebnis.Durchgefuehrt, false);
        Application.DoEvents();
        Assert.Single(u.Speicher.MassnahmeBestaetigungen(id), x => x.MassnahmeId == neu.Id && x.Kuerzel == "EF");
        Assert.DoesNotContain(s.MassnahmenZeilen, z => z.Termin.Massnahme.Id == neu.Id);   // bestätigte sind eingeklappt
        s.ErledigteUmschalten();
        Application.DoEvents();
        Assert.StartsWith("✓", s.MassnahmenZeilen.Single(z => z.Termin.Massnahme.Id == neu.Id).Text1.Text);
        Assert.Null(s.MassnahmenZeilen.Single(z => z.Termin.Massnahme.Id == neu.Id).DurchgefuehrtKnopf);

        // Nur die eigenen Bezugsbewohner.
        s.MassnahmenFiltern(true);
        Application.DoEvents();
        var bezuege = u.Speicher.Bezuege();
        Assert.All(s.MassnahmenZeilen, z => Assert.Equal("EF", bezuege.GetValueOrDefault(z.Termin.Massnahme.BewohnerId)?.Bezugspflege?.Kuerzel));
        s.MassnahmenFiltern(false);
    });

    [Fact]
    public void Ohne_Recht_kein_Anlegen_Einstellungen_mit_Arten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "OP" });   // Therapie: weder Fachkraft der Wohnbereiche noch Bezugspflege
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Demo("Ahorn");
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(b.Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen(MassnahmenAnsicht.ReiterName);
        Assert.NotNull(Stil.Sperrgrund(f.Bewohner.Akte.Massnahmen.NeuKnopf));
        Assert.False(f.Bewohner.Akte.Massnahmen.Neu());

        using var seite = new EinstellungenSeite(u.Speicher) { Size = new Size(1200, 900) };
        seite.Zeigen();
        Assert.Contains("Maßnahmen: Arten und Vorlagen", seite.Karten.Select(k => k.Titel));
        Assert.Contains("Lebensbereiche: kein Bericht seit", seite.Karten.Select(k => k.Titel));
    });
}
