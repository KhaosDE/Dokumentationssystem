using DokuLine.Kern;

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
            u.Speicher.AufgabeSpeichern(new Aufgabe { Text = text, Fuer = AufgabeFuer.WerZeitHat, Erstellt = jetzt.AddHours(-1), Verfasser = "AB" });
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
        spalte.Bildlauf = 60;
        int gesetzt = spalte.Bildlauf;
        l.Aktualisieren();
        Assert.Equal(gesetzt, l.Spalten.Single(s => s.Titel == spalte.Titel).Bildlauf);
    });

    [Fact]
    public void Bewohner_Berichte_rollbar_Ruecksprung_und_Bereichswahl() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = Fenster(u, "EF");
        f.BereichWaehlen("Bewohner");
        var b = f.Bewohner;

        // Berichte rechts: wachsen mit dem Inhalt, der Rahmen rollt (30 Berichte reichen über jede Bildschirmhöhe).
        var erster = b.Gewaehlt!.Bewohner;
        var jetzt = DateTime.Now;
        for (int i = 0; i < 30; i++)
            u.Speicher.BerichtSpeichern(new Bericht
            {
                BewohnerId = erster.Id!.Value, Zeitpunkt = jetzt.AddHours(-i), Schicht = Schicht.Frueh, Verfasser = "EF",
                Text = $"Bericht {i} (Test).", Erfasst = jetzt,
            });
        b.UebersichtZeigen();
        Application.DoEvents();
        Assert.Same(b.BerichteRahmen, b.Berichte.Parent);
        Assert.True(b.Berichte.Height > b.BerichteRahmen.ClientSize.Height, $"Berichte {b.Berichte.Height} px, Rahmen {b.BerichteRahmen.ClientSize.Height} px");
        Assert.True(b.BerichteRahmen.VerticalScroll.Visible);

        // Zurück aus der Akte: deren Bewohner bleibt gewählt.
        var anderer = b.Zeilen[5].Bewohner;
        Assert.NotEqual(anderer.Id, b.Gewaehlt!.Bewohner.Id);
        b.AkteZeigen(anderer.Id!.Value);
        b.UebersichtZeigen();
        Assert.Equal(anderer.Id, b.Gewaehlt!.Bewohner.Id);

        // Bereich umbenannt: die Bereichswahlen zeigen den neuen Namen.
        u.Speicher.ListeneintragUmbenennen(Liste.Bereiche, "R2", "Nord");
        b.UebersichtZeigen();
        Assert.Contains("Nord", b.BereichWahl.Items.Cast<string>());
        Assert.DoesNotContain("R2", b.BereichWahl.Items.Cast<string>());
        b.BereichWahl.SelectedItem = "Nord";
        Assert.NotEmpty(b.Zeilen);
        Assert.All(b.Zeilen, z => Assert.Equal("Nord", z.Bewohner.Wohnbereich));
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        s.Aktualisieren();
        Assert.Contains("Nord", s.BereichWahl.Items.Cast<string>());
        Assert.DoesNotContain("R2", s.BereichWahl.Items.Cast<string>());
        f.Therapie.Laufend.Aktualisieren();
        Assert.Contains("Nord", f.Therapie.Laufend.BereichWahl.Items.Cast<string>());
    });
}
