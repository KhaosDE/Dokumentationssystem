using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Startseite „Übersicht“ (seit Bauplan Schritt 26l): links „Heute wichtig“ (Übergaben, Termine, laufender
// Dienst, Nachrichten), rechts die Bewohnerliste mit „Wo“ und Hinweisen; Aktionsleiste mit Bereichswahl;
// unter 1100 px „Heute wichtig“ oben; Statusleiste mit Demo-Hinweis, Datum, Dienst und Stand.
[Collection("Oberflaeche")]
public class UebersichtOberflaechenTests
{
    [Fact]
    public void Heute_wichtig_und_Bewohnerliste() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung R1
        var jetzt = DateTime.Now;
        u.Speicher.WohnbereichNotiz("Getränkelieferung kommt (Test).", "AB", new UebergabeWunsch(UebergabeDauer.Woche), jetzt);
        u.Speicher.AufgabeSpeichern(new Aufgabe
        {
            Text = "Kühlschrank abtauen (Test)", Fuer = AufgabeFuer.WerZeitHat, Erstellt = jetzt.AddHours(-1), Verfasser = "AB",
        });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.WindowState = FormWindowState.Maximized;   // CI: Bildschirm 1920 × 1080
        Application.DoEvents();
        Assert.Equal("Übersicht", Hauptfenster.Startbereich);
        f.BereichWaehlen(Hauptfenster.Startbereich);
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        Assert.StartsWith("R1 · 23 Bewohner · 2 Betten frei · Stand ", f.Kopf.KleinText);   // eigener Bereich (Mehrplatz, Schritt 26e)

        // Aktionsleiste: Hauptaktion, drei weitere, „Neuer Bewohner …“ grün; rechts Bereichswahl und Drucken.
        Assert.Equal(["Neuer Bericht …", "Neue Aufgabe …", "Neue Nachricht …", "Neuer Bewohner …", "Drucken …"], f.Kopf.Knoepfe.Select(k => k.Text));
        Assert.Equal(1, AktionsLeiste.Gruppe(s.BerichtNeuKnopf));
        Assert.Equal(Stil.Erfolg.ToArgb(), s.BewohnerNeuKnopf.BackColor.ToArgb());
        Assert.Same(s.BereichWahl, f.Kopf.Leiste.Zusatz);
        Assert.Same(f.Kopf.Leiste, s.BereichWahl.Parent);
        Assert.True(s.BereichWahl.Right <= s.DruckKnopf.Left);
        Assert.StartsWith("DEMO — ausgedachte Daten · ", f.Statusleiste.StatusText);
        Assert.Contains("dienst · Stand ", f.Statusleiste.StatusText);

        // Rechts die Liste: eine Zeile je Bewohner, nach Zimmer; Kopf „Bewohner R1 · 23 · … im Haus“.
        Assert.Equal(["Zimmer", "Name", "Bezug", "Wo", "Hinweise"], s.Liste.Columns.Cast<ColumnHeader>().Select(c => c.Text));
        Assert.Equal(23, s.Zeilen.Count);
        Assert.Equal(s.Zeilen.Select(z => z.Bewohner.Zimmer.PadLeft(8)).Order(StringComparer.Ordinal), s.Zeilen.Select(z => z.Bewohner.Zimmer.PadLeft(8)));
        int imHaus = s.Zeilen.Count(z => z.Wo == HeuteWichtigRegeln.ImHaus);
        Assert.Equal($"BEWOHNER R1 · 23 · {imHaus} IM HAUS", s.ListenKopfText);
        var holunder = s.Zeilen.Single(z => z.Bewohner.Nachname == "Holunder");
        Assert.Single(holunder.Marken, m => m.Art == MarkenArt.Abwesend);   // nur einmal, obwohl Kalender und Modul es melden
        Assert.NotEqual(HeuteWichtigRegeln.ImHaus, holunder.Wo);
        Assert.DoesNotContain("abwesend:", holunder.HinweisText);             // steht nicht noch einmal bei den Hinweisen
        Assert.Contains(s.Zeilen.Single(z => z.Bewohner.Nachname == "Fichte").Marken, m => m.Text == "Medikation: Pause beginnt");
        Assert.Equal(5, s.Zeilen[0].Eintrag.SubItems.Count);
        // Bei 1920 × 1080 passen alle 23 Zeilen ohne Bildlauf.
        if (Screen.PrimaryScreen!.Bounds.Height >= 1080 && !s.Schmal)
            Assert.True(s.Liste.Items[22].Bounds.Bottom <= s.Liste.ClientSize.Height, $"Liste {s.Liste.ClientSize.Height} px hoch");

        // Links „Heute wichtig“: vier Kästen mit Zähler; leer bleibt der Kasten mit „Nichts offen“.
        Assert.Equal(["ÜBERGABEN OFFEN", "TERMINE HEUTE", "LAUFENDER DIENST · NOCH OFFEN", "NACHRICHTEN UNGELESEN"],
            new[] { s.Uebergaben, s.Termine, s.Dienst, s.Nachrichten }.Select(k => k.Titel));
        foreach (var k in new[] { s.Uebergaben, s.Termine, s.Dienst, s.Nachrichten })
        {
            Assert.StartsWith(k.Titel + " · ", k.KopfText);
            Assert.Equal(k.Zeilen.Count == 0, k.Leer.Visible);
        }
        var notiz = s.Uebergaben.Zeilen.OfType<HeuteEintrag>().Single(e => e.Text1.Text.Contains("Getränkelieferung kommt (Test)."));
        Assert.Equal("Wohnbereich allgemein: Getränkelieferung kommt (Test).", notiz.Text1.Text);
        Assert.StartsWith("seit ", notiz.Text2!.Text);
        Assert.EndsWith(" · AB", notiz.Text2.Text);
        var dienst = s.DienstStand!;
        Assert.Equal(dienst.Offen.Count, s.Dienst.Zeilen.Count);
        Assert.EndsWith(dienst.Text, s.DienstText.Text);
        Assert.Equal(dienst.Erledigt, s.DienstBalken.Value);
        Hilfe.Foto(f, "114_uebersicht");

        // Abhaken im Kasten: Kürzel und Uhrzeit wie in „Aufgaben“.
        var haken = s.Dienst.Zeilen.OfType<CheckBox>().Single(c => c.Text.StartsWith("Kühlschrank abtauen (Test)"));
        int erledigtVorher = dienst.Erledigt;
        haken.Checked = true;
        Application.DoEvents();
        Assert.Equal(erledigtVorher + 1, s.DienstStand!.Erledigt);
        var erledigt = u.Speicher.Aufgaben(DateTime.Now, auchErledigte: true).Single(a => a.Text == "Kühlschrank abtauen (Test)");
        Assert.Equal("EF", erledigt.ErledigtVon);
        Assert.DoesNotContain(s.Dienst.Zeilen, c => c.Text.StartsWith("Kühlschrank abtauen (Test)"));

        // Klick auf einen Eintrag öffnet ihn an seiner Stelle.
        s.Uebergaben.Zeilen.OfType<HeuteEintrag>().First().Klicken();
        Assert.Equal("Übergabe", f.AktiverBereich);
        f.BereichWaehlen("Übersicht");
        Assert.Same(s.BereichWahl, f.Kopf.Leiste.Zusatz);   // nach dem Wechsel wieder da

        // „Neuer Bericht …“ braucht eine gewählte Zeile; Doppelklick bzw. Enter öffnet die Akte.
        Assert.Equal("Bitte zuerst einen Bewohner in der Liste wählen.", Stil.Sperrgrund(s.BerichtNeuKnopf));
        long birke = u.Demo("Birke").Id!.Value;
        Assert.True(s.Waehlen(birke));
        Assert.Null(Stil.Sperrgrund(s.BerichtNeuKnopf));
        using (var ersatz = new DialogErsatz(fenster =>
        {
            Assert.StartsWith("Birke, Berta", Assert.IsType<BerichtFormular>(fenster).BewohnerWahl.Text);
            return DialogResult.Cancel;
        }))
            s.BerichtNeuKnopf.PerformClick();
        s.AkteOeffnen();
        Assert.Equal("Bewohner", f.AktiverBereich);
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Birke", f.Bewohner.Akte.Bewohner!.Nachname);

        // Suche und Bereichswahl.
        f.BereichWaehlen("Übersicht");
        s.Suche.Text = "Birke";
        Assert.Equal("Birke", Assert.Single(s.Zeilen).Bewohner.Nachname);
        s.Suche.Text = "";
        s.BereichWahl.SelectedItem = "R2";
        Assert.All(s.Zeilen, z => Assert.Equal("R2", z.Bewohner.Wohnbereich));
        Assert.StartsWith("BEWOHNER R2 · 23 · ", s.ListenKopfText);
    });

    [Fact]
    public void Schmal_steht_heute_wichtig_oben_und_Drucken() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        f.WindowState = FormWindowState.Maximized;
        Application.DoEvents();
        var s = f.Uebersicht;
        var links = s.Uebergaben.Parent!;
        var rechts = s.Liste.Parent!;
        if (!s.Schmal) Assert.True(rechts.Left > links.Right && rechts.Top == links.Top);   // breit: nebeneinander

        f.WindowState = FormWindowState.Normal;
        f.Size = new Size(Stil.Px(f, 1000), Stil.Px(f, 760));
        Application.DoEvents();
        s.PerformLayout();
        Assert.True(s.Schmal);
        Assert.True(rechts.Top > links.Bottom - 1);   // „Heute wichtig“ über der Liste
        Assert.Equal(links.Width, rechts.Width);
        Hilfe.Foto(f, "115_uebersicht_schmal");

        // Drucken: Kopf, die vier Kästen und die Liste.
        var druck = new UebersichtDruck(s.ListenKopfText, s.KopfText, [s.Uebergaben, s.Termine, s.Dienst, s.Nachrichten], s.Zeilen, DateTime.Now);
        var zeilen = druck.Zeilen();
        Assert.Contains(zeilen, z => z.Abschnitt == s.ListenKopfText && z.Text.Contains("Berta Birke"));
        Assert.Contains(zeilen, z => z.Abschnitt!.StartsWith("TERMINE HEUTE"));
        using var bild = new Bitmap(1200, 900);
        using var g = Graphics.FromImage(bild);
        Assert.True(druck.SeiteZeichnen(g, 0) > 0);
    });

    [Fact]
    public void Frist_einstellen_und_neu_aufbauen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var s = Assert.IsType<UebersichtSeite>(f.Seite("Übersicht"));
        bool Bald() => s.Zeilen.Single(k => k.Bewohner.Nachname == "Clematis").Marken.Any(m => m.Text.StartsWith("Beschluss endet in"));
        Assert.True(Bald());
        f.EinstellungenZeigen = fenster =>
        {
            Assert.Equal(90, fenster.BeschlussFrist.Value);
            fenster.BeschlussFrist.Value = 14;
            return fenster.Speichern();
        };
        Assert.True(f.EinstellungenOeffnen());
        f.BereichWaehlen("Übersicht");
        Assert.False(Bald());

        // Immer aktuell: eine Änderung im Kalender baut die Übersicht neu auf (Kasten „Termine heute“).
        string Termine() => string.Join("\n", s.Termine.Zeilen.OfType<HeuteEintrag>().Select(e => e.Text1.Text));
        Assert.DoesNotContain("Fußpflege (Test)", Termine());
        long kiefer = u.Demo("Kiefer").Id!.Value;
        u.Speicher.KalenderAnlegen(KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Bewohner, Titel = "Fußpflege (Test)", BeginnDatum = Datum.Text(DateOnly.FromDateTime(DateTime.Today)),
            BeginnUhrzeit = "23:00", EndeUhrzeit = "23:30", Bewohner = [kiefer], Kuerzel = "AB",
        }, DateTime.Now));
        Assert.Contains("23:00  Fußpflege (Test) – Karl Kiefer", Termine());
        s.Termine.Zeilen.OfType<HeuteEintrag>().First(e => e.Text1.Text.Contains("Fußpflege (Test)")).Klicken();
        Assert.Equal("Tagesplan", f.AktiverBereich);
    });
}
