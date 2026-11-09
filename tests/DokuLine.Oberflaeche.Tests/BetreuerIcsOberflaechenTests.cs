using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 28: Betreuer in der Akte, „E-Mail an Betreuer …“ (Hinweis, dann mailto:),
// Kalender als .ics speichern und .ics-Dateien übernehmen (Vorschau, Zuordnung, Bereich/Haus).
[Collection("Oberflaeche")]
public class BetreuerIcsOberflaechenTests
{
    [Fact]
    public void Betreuer_in_der_Akte_und_EMail_ueber_das_EMail_Programm() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Equal("Heike Hecht (Betreuungsverein Lindenhof (ausgedacht)) · Tel. 0000 111 222 · h.hecht@example.org", akte.BetreuerText.Text);
        Assert.Null(Stil.Sperrgrund(akte.EmailKnopf));
        Hilfe.Foto(f, "89_akte_betreuer");

        string? hinweis = null, geoeffnet = null;
        var vorher = Extern.Oeffnen;
        Extern.Oeffnen = adresse => geoeffnet = adresse;
        try
        {
            akte.EmailBestaetigen = _ => false;   // Abbrechen: nichts öffnet sich
            akte.EmailKnopf.PerformClick();
            Assert.Null(geoeffnet);
            akte.EmailBestaetigen = text => { hinweis = text; return true; };
            akte.EmailKnopf.PerformClick();
        }
        finally { Extern.Oeffnen = vorher; }
        Assert.StartsWith(BetreuerRegeln.EmailHinweis, hinweis);
        Assert.Equal("mailto:h.hecht@example.org?subject=Betreuung%20Anton%20Ahorn", geoeffnet);

        // Ohne E-Mail-Adresse bzw. ohne Betreuer: Knopf gesperrt, „nicht erfasst“ sichtbar.
        f.Bewohner.AkteZeigen(u.Demo("Clematis").Id!.Value);
        Assert.Equal("Für den Betreuer ist keine E-Mail-Adresse eingetragen.", Stil.Sperrgrund(akte.EmailKnopf));
        f.Bewohner.AkteZeigen(u.Demo("Distel").Id!.Value);
        Assert.Equal("nicht erfasst", akte.BetreuerText.Text);
        Assert.Equal("Kein Betreuer eingetragen.", Stil.Sperrgrund(akte.EmailKnopf));

        string? fehler = null;
        using (new DialogErsatz(fenster =>
        {
            var b = Assert.IsType<BetreuerFenster>(fenster);
            b.NameFeld.Text = "Test Taucher";
            b.Stelle.Text = "Betreuungsverein (ausgedacht)";
            b.Email.Text = "taucher at example";
            Assert.False(b.Speichern());
            fehler = b.Fehler.Text;
            b.Email.Text = "taucher@example.org";
            Hilfe.Foto(b, "90_betreuer");
            return b.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            akte.BetreuerKnopf.PerformClick();
        Assert.Contains("E-Mail-Adresse bitte so", fehler);
        Assert.StartsWith("Test Taucher (Betreuungsverein (ausgedacht))", akte.BetreuerText.Text);
        Assert.Null(Stil.Sperrgrund(akte.EmailKnopf));

        // 0.33.3: Die Betreuung endet → „keine gesetzliche Betreuung“ (dritter Zustand); E-Mail gesperrt.
        using (new DialogErsatz(fenster =>
        {
            var b = Assert.IsType<BetreuerFenster>(fenster);
            Assert.True(b.NameFeld.Enabled);
            b.Keine.Checked = true;
            Assert.False(b.NameFeld.Enabled);
            return b.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            akte.BetreuerKnopf.PerformClick();
        Assert.Equal(BetreuerRegeln.KeineText, akte.BetreuerText.Text);
        Assert.Equal("Keine gesetzliche Betreuung eingetragen.", Stil.Sperrgrund(akte.EmailKnopf));
        Assert.False(akte.EmailSchreiben());
        f.Bewohner.AkteZeigen(u.Demo("Ginster").Id!.Value);   // Demo: keine Betreuung
        Assert.Equal(BetreuerRegeln.KeineText, akte.BetreuerText.Text);
    });

    [Fact]
    public void Kalender_als_ics_speichern_und_ics_uebernehmen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Kalender");
        var k = f.Kalender;
        string export = Path.Combine(u.Ordner, "export.ics");
        var speichernVorher = KalenderAustausch.SpeichernUnter;
        var oeffnenVorher = KalenderAustausch.Oeffnen;
        try
        {
            string? vorschlag = null;
            KalenderAustausch.SpeichernUnter = (_, v) => { vorschlag = v; return export; };
            k.ExportKnopf.PerformClick();
            Assert.EndsWith(".ics", vorschlag);
            Assert.Equal(k.Termine().Count, Ics.Lesen(File.ReadAllText(export)).Count);

            var tag = DateTime.Today.AddDays(3);
            string datei = Path.Combine(u.Ordner, "einladung.ics");
            File.WriteAllText(datei, string.Join("\r\n",
                "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Beispiel//DE",
                "BEGIN:VEVENT", "UID:a", "SUMMARY:Hilfeplangespräch (ausgedacht)", $"DTSTART:{tag:yyyyMMdd}T140000", $"DTEND:{tag:yyyyMMdd}T150000", "LOCATION:Raum 2", "END:VEVENT",
                "BEGIN:VEVENT", "UID:b", "SUMMARY:Teamsitzung (ausgedacht)", $"DTSTART:{tag:yyyyMMdd}T080000", "DURATION:PT1H", "RRULE:FREQ=WEEKLY", "END:VEVENT",
                "END:VCALENDAR"));
            KalenderAustausch.Oeffnen = _ => datei;
            string? fehler = null;
            using (new DialogErsatz(fenster =>
            {
                var i = Assert.IsType<IcsImportFenster>(fenster);
                Assert.Equal(2, i.Liste.CheckedItems.Count);
                Assert.Contains("(Wiederholung: nur der erste)", (string)i.Liste.Items[1]);
                i.Arten[(int)KalenderArt.Bewohner].Checked = true;
                Assert.False(i.Uebernehmen());   // ohne Bewohner geht es nicht
                fehler = i.Fehler.Text;
                i.Arten[(int)KalenderArt.Wohnbereich].Checked = true;
                Assert.True(i.HausWahl.Enabled);   // Bereichsleitung darf „Haus“
                i.HausWahl.Checked = true;
                Hilfe.Foto(i, "91_ics_uebernehmen");
                return i.Uebernehmen() ? DialogResult.OK : DialogResult.Cancel;
            }))
                k.ImportKnopf.PerformClick();
            Assert.Contains("Bitte mindestens einen Bewohner wählen.", fehler);
        }
        finally
        {
            KalenderAustausch.SpeichernUnter = speichernVorher;
            KalenderAustausch.Oeffnen = oeffnenVorher;
        }
        var heute = DateOnly.FromDateTime(DateTime.Today);
        var neu = u.Speicher.Kalender(heute, heute.AddDays(7)).Where(t => t.Aktuell.Titel.EndsWith("(ausgedacht)") && t.Aktuell.Hinweis.StartsWith("aus einer .ics-Datei")).ToList();
        Assert.Equal(2, neu.Count);
        Assert.All(neu, t => Assert.True(t.Aktuell.Haus));
        Assert.Contains(neu, t => t.Aktuell.Hinweis.Contains("Wiederholung: nur der erste Termin"));

        // Ohne Recht „Kalender Haus bearbeiten“ ist „Haus“ beim Übernehmen aus.
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var ohne = new IcsImportFenster(u.Speicher, Ics.Lesen(File.ReadAllText(export)), "export.ics");
        Assert.False(ohne.HausWahl.Enabled);
    });
}
