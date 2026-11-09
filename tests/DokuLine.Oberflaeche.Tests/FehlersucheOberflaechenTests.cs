using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;

namespace DokuLine.Oberflaeche.Tests;

// Korrekturen aus der Fehlersuche vom 08.11.2026 (0.33.2), Oberfläche: Übersicht bei Schichtwechsel und mit
// Standardaufgaben, Doppelklickschutz (Dienst-Haken, Therapie-Tafel), Bildlauf der Tafel, Berichte im
// Bereich „Bewohner“ rollbar, Rücksprung aus der Akte, Bereichswahl nach dem Umbenennen eines Bereichs.
[Collection("Oberflaeche")]
public class FehlersucheOberflaechenTests
{
    private static DateTime Heute(int stunde, int minute) => DateTime.Today.AddHours(stunde).AddMinutes(minute);

    private static Hauptfenster Fenster(Umgebung u, string kuerzel)
    {
        Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.WindowState = FormWindowState.Maximized;
        Application.DoEvents();
        return f;
    }

    [Fact]
    public void Uebersicht_folgt_der_Schicht_und_legt_Standardaufgaben_an() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "EF");
        f.BereichWaehlen("Übersicht");
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));

        // Eine Schicht von morgen, zu der eine Standardaufgabe passt (Umgebung: alle Standardaufgaben aus).
        var (zeit, vorlage) = Enumerable.Range(1, 3).SelectMany(tag => Enumerable.Range(0, 24).Select(h => DateTime.Today.AddDays(tag).AddHours(h).AddMinutes(30)))
            .SelectMany(z => u.Speicher.Vorlagen().Select(v => (z, v)))
            .First(x => AufgabenRegeln.Gilt(x.v with { Aktiv = true }, SchichtTermin.Zu(x.z)));
        u.Speicher.VorlageAktiv(vorlage.Id, true);
        bool Angelegt() => u.Speicher.Aufgaben(zeit, auchErledigte: true).Any(a => a.VorlageId == vorlage.Id && a.FuerSchicht == SchichtTermin.Zu(zeit));
        Assert.False(Angelegt());

        // Kurz vor der Schicht: gezeigt ist die Schicht davor; dann wechselt die Schicht → neu bauen.
        var davor = SchichtTermin.Zu(zeit).Beginn.AddMinutes(-1);
        s.Jetzt = () => davor;
        s.Aktualisieren();
        Assert.False(s.Veraltet);
        Assert.NotEqual(SchichtTermin.Zu(zeit), s.DienstStand!.Schicht);
        s.Jetzt = () => zeit;
        Assert.True(s.Veraltet);   // 0.33.2: vorher nur beim Datumswechsel
        s.Aktualisieren();
        Assert.Equal(SchichtTermin.Zu(zeit), s.DienstStand!.Schicht);
        Assert.True(Angelegt());   // 0.33.2: vorher erst beim Öffnen von „Aufgaben“
        Assert.Contains(s.DienstStand.Offen, a => a.VorlageId == vorlage.Id);
    });

    [Fact]
    public void Doppelklick_im_Kasten_Dienst_trifft_nicht_die_naechste_Aufgabe() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        var jetzt = DateTime.Now;
        foreach (var text in new[] { "Erste (Test)", "Zweite (Test)" })
            u.Speicher.AufgabeSpeichern(new Aufgabe
            {
                Text = text, Bereich = "R1", Fuer = AufgabeFuer.WerZeitHat, Erstellt = jetzt.AddHours(-1), Verfasser = "AB",
            });
        using var f = Fenster(u, "EF");
        f.BereichWaehlen("Übersicht");
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        long uhr = 1_000_000;
        s.Schutz.Uhr = () => uhr;
        CheckBox Haken(string text) => s.Dienst.Zeilen.OfType<CheckBox>().Single(c => c.Text.StartsWith(text));
        bool Erledigt(string text) => u.Speicher.Aufgaben(DateTime.Now, auchErledigte: true).Single(a => a.Text == text).Erledigt is not null;

        Haken("Erste (Test)").Checked = true;
        Application.DoEvents();
        Assert.True(Erledigt("Erste (Test)"));
        var zweite = Haken("Zweite (Test)");
        zweite.Checked = true;                 // zweiter Klick desselben Doppelklicks
        Application.DoEvents();
        Assert.False(zweite.Checked);
        Assert.False(Erledigt("Zweite (Test)"));
        uhr += 10_000;                           // später, eigener Klick
        Haken("Zweite (Test)").Checked = true;
        Application.DoEvents();
        Assert.True(Erledigt("Zweite (Test)"));
    });

    [Fact]
    public void Tafel_Doppelklick_und_Bildlauf() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "OP");
        f.BereichWaehlen("Therapie");
        var t = f.Therapie;
        var l = t.Laufend;
        t.Jetzt = () => Heute(10, 15);
        l.Aktualisieren();
        long uhr = 1_000_000;
        l.Schutz.Uhr = () => uhr;
        var offen = l.Tafel!.NochOffen;
        Assert.True(offen.Count >= 2);
        var (t1, p1) = offen[0];
        var (t2, p2) = offen[1];
        Assert.True(l.Setzen(t1, p1, Teilnahme.Teilgenommen));
        Assert.False(l.Setzen(t2, p2, Teilnahme.Abgesagt));   // zweiter Klick desselben Doppelklicks
        Assert.False(u.Speicher.TeilnahmeEintraege(DateOnly.FromDateTime(DateTime.Today)).ContainsKey(p2.ZuteilungId));
        uhr += 10_000;
        Assert.True(l.Setzen(t2, p2, Teilnahme.Abgesagt));

        // Bildlauf der Spalten bleibt beim Neuaufbau (jede Minute und nach jedem Klick).
        var spalte = l.Spalten[^1];
        string titel = spalte.Titel;   // vor dem Neuaufbau lesen: danach ist die alte Spalte freigegeben
        spalte.Bildlauf = 60;
        int gesetzt = spalte.Bildlauf;
        l.Aktualisieren();
        Assert.Equal(gesetzt, l.Spalten.Single(s => s.Titel == titel).Bildlauf);
    });

    [Fact]
    public void Dokumentation_rollbar_Ruecksprung_und_Bereichswahl() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "EF");
        f.BereichWaehlen("Dokumentation");
        var b = f.Bewohner;

        // Berichte rechts: die Liste rollt (30 Berichte reichen über jede Bildschirmhöhe).
        var erster = b.Gewaehlt!;
        var jetzt = DateTime.Now;
        for (int i = 0; i < 30; i++)
            u.Speicher.BerichtSpeichern(new Bericht
            {
                BewohnerId = erster.Id!.Value, Zeitpunkt = jetzt.AddHours(-i), Schicht = Schicht.Frueh, Verfasser = "EF",
                Text = $"Bericht {i} (Test).", Erfasst = jetzt,
            });
        b.UebersichtZeigen();
        Application.DoEvents();
        var liste = (ScrollableControl)b.Dokumentation.Zeilen[0].Parent!;
        Assert.True(liste.DisplayRectangle.Height > liste.ClientSize.Height,
            $"Inhalt {liste.DisplayRectangle.Height} px, Fläche {liste.ClientSize.Height} px");
        Assert.True(liste.VerticalScroll.Visible);

        // Zurück aus der Akte: deren Bewohner bleibt gewählt.
        var anderer = b.Zeilen[5];
        Assert.NotEqual(anderer.Id, b.Gewaehlt!.Id);
        b.AkteZeigen(anderer.Id!.Value);
        b.UebersichtZeigen();
        Assert.Equal(anderer.Id, b.Gewaehlt!.Id);

        // Bereich umbenannt: die Bereichswahl in der Übersicht zeigt den neuen Namen, alle folgen ihr.
        u.Speicher.ListeneintragUmbenennen(Liste.Bereiche, "R2", "Nord");
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        s.Aktualisieren();
        Assert.Contains("Nord", s.Umschalter.Knoepfe.Select(k => k.Text));
        Assert.DoesNotContain("R2", s.Umschalter.Knoepfe.Select(k => k.Text));
        f.ArbeitsbereichWaehlen("Nord");
        b.UebersichtZeigen();
        Assert.NotEmpty(b.Zeilen);
        Assert.All(b.Zeilen, z => Assert.Equal("Nord", z.Wohnbereich));
        f.Therapie.Laufend.Aktualisieren();
        Assert.Equal("Nord", f.Therapie.Laufend.Bereich);
    });

    // ----- 0.33.3 -----

    [Fact]
    public void Uebersicht_Nachrichtenzaehler_und_Bereichswahl_verdeckt_nichts() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "AB");
        f.BereichWaehlen("Übersicht");
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        int ungelesen = u.Speicher.UngeleseneNachrichten("AB");
        Assert.True(ungelesen > s.Nachrichten.Zeilen.Count, "Demo: mehrere ungelesene in einem Verlauf");
        Assert.Equal(ungelesen, s.Nachrichten.Anzahl);   // wie die Seitenleiste (vorher: Zahl der Verläufe)

        // Schmal: die Bereichswahl oben bleibt ganz sichtbar und liegt über den Kästen, nicht auf ihnen.
        foreach (int breite in new[] { 1600, 1000, 800, 640 })
        {
            f.WindowState = FormWindowState.Normal;
            f.Size = new Size(breite, 700);
            Application.DoEvents();
            Assert.True(s.Umschalter.Right <= s.ClientSize.Width, $"{breite}: Bereichswahl ragt heraus");
            Assert.True(s.Umschalter.Bottom <= s.Uebergaben.Parent!.Top, $"{breite}: Bereichswahl über den Kästen");
        }
        f.WindowState = FormWindowState.Maximized;
        Application.DoEvents();
    });

    [Fact]
    public void Einstellungen_Modulhaken_Schichtzeiten_Dialogsymbol() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "RS");   // Hausleitung: darf Module schalten
        Assert.True(f.EinstellungenOeffnen("Module"));
        var e = f.Einstellungsseite;
        e.Module["ausgangsliste"].Checked = false;
        Assert.False(e.Module[Kuechendienst.Schluessel].Enabled);
        e.Zeigen();   // erneut öffnen: Untermodul bleibt gesperrt (vorher gab die Kartensperre es frei)
        Assert.False(e.Module[Kuechendienst.Schluessel].Enabled);
        e.Module["ausgangsliste"].Checked = true;
        Assert.True(e.Module[Kuechendienst.Schluessel].Enabled);

        // Ohne Recht: Feld unverändert verlassen ist kein Fehler.
        f.BenutzerWaehlen("CD");
        Assert.True(f.EinstellungenOeffnen("Wohnbereich"));
        Assert.NotNull(Recht.Grund(u.Speicher, Funktionen.EinstellungenBereich));
        Assert.True(e.SchichtzeitenUebernehmen());
        Assert.False(e.Fehler.Visible);

        // Dialoge haben ein eigenes Symbol-Objekt (sonst zeigt der Dialograhmen keins), auch ohne „Grundform“.
        using (var formular = new BewohnerFormular(u.Speicher, null))
        {
            Stil.SymbolSetzen(formular);
            Assert.Same(Stil.Dialogsymbol, formular.Icon);
        }
    });

    [Fact]
    public void Therapie_Balkenklick_zeigt_Angebot_trotz_Anbieterfilter() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "OP");
        f.BereichWaehlen("Therapie");
        var t = f.Therapie;
        t.Jetzt = () => Heute(10, 15);
        t.Laufend.Aktualisieren();
        var termin = t.Laufend.Tafel!.Laufend.First();
        string anderer = t.AnbieterFilter.Items.Cast<string>().First(a => a != TherapieSeite.AlleAnbieter && a != termin.Angebot.Anbieter);
        t.AnbieterFilter.SelectedItem = anderer;
        Assert.DoesNotContain(t.Karten, k => k.Angebot.Id == termin.Angebot.Id);
        t.Laufend.Leiste.Oeffnen(termin);
        Assert.Equal(TherapieSeite.AngeboteReiter, t.Reiter.Aktiv);
        Assert.Equal(TherapieSeite.AlleAnbieter, t.AnbieterFilter.SelectedItem);
        Assert.Contains(t.Karten, k => k.Angebot.Id == termin.Angebot.Id);
    });
}
