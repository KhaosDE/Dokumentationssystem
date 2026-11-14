using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 47: Seite „Hilfeplangespräche“ nach Fälligkeit, Termin vergeben (Kalender, Tagesplan), einladen
// (E-Mail-Programm und .ics), Vorbereitung als Aufgaben, „geführt“; Zeile im Reiter „Kostenträger“.
[Collection("Oberflaeche")]
public class HilfeplanOberflaechenTests
{
    [Fact]
    public void Termin_einladen_vorbereiten_und_gefuehrt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen("R1");
        f.BereichWaehlen("Hilfeplangespräche");
        var h = f.Hilfeplan;
        Assert.Equal(u.Speicher.AlleBewohner().Count(b => b.Wohnbereich == "R1"), h.Liste.Items.Count);
        Assert.Contains(h.Liste.Items.Cast<ListViewItem>(), i => i.SubItems[6].Text == "Hilfeplangespräch überfällig");
        Hilfe.Foto(f, "179_hilfeplan");
        var zeile = h.Liste.Items.Cast<ListViewItem>().First();
        var b = ((HilfeplanStand)zeile.Tag!).Bewohner;
        zeile.Selected = true;
        var antworten = new Queue<string>([DateTime.Now.Date.AddDays(7).AddHours(10).ToString("dd.MM.yyyy HH:mm"), "Besprechungsraum (Test)", ""]);
        h.Fragen = (_, _) => antworten.Dequeue();
        h.Waehlen = texte => [0, texte.Count - 1];
        h.Warnen = _ => true;
        Assert.True(h.TerminVergeben());
        var stand = u.Speicher.Hilfeplan(b.Id!.Value, DateOnly.FromDateTime(DateTime.Now));
        Assert.Equal(HilfeplanStandArt.TerminVergeben, stand.Stand);

        zeile = h.Liste.Items.Cast<ListViewItem>().Single(i => ((HilfeplanStand)i.Tag!).Bewohner.Id == b.Id);
        zeile.Selected = true;
        string? geoeffnet = null, ics = null;
        var vorher = Extern.Oeffnen;
        try
        {
            Extern.Oeffnen = adresse => geoeffnet = adresse;
            string datei = Path.Combine(u.Ordner, "hpg.ics");
            h.IcsSpeichern = _ => datei;
            Assert.True(h.Einladen());
            ics = File.ReadAllText(datei);
        }
        finally { Extern.Oeffnen = vorher; }
        Assert.StartsWith("mailto:", geoeffnet);
        Assert.Contains("BEGIN:VEVENT", ics);
        h.Waehlen = _ => [0];
        Assert.True(h.Vorbereiten());
        Assert.Contains(u.Speicher.Aufgaben(DateTime.Now.AddDays(1), bereich: "R1"), a => a.BewohnerId == b.Id && a.Text.StartsWith("Hilfeplangespräch ", StringComparison.Ordinal));

        // Der Termin liegt in der Zukunft; „geführt“ hier mit dem heutigen Datum (ein Gespräch in der Zukunft lehnt das Programm ab).
        antworten = new Queue<string>([Datum.Text(DateOnly.FromDateTime(DateTime.Now)), "Ergebnis besprochen (Test).", DateTime.Now.Date.AddYears(1).ToString("dd.MM.yyyy")]);
        zeile = h.Liste.Items.Cast<ListViewItem>().Single(i => ((HilfeplanStand)i.Tag!).Bewohner.Id == b.Id);
        zeile.Selected = true;
        Assert.True(h.Gefuehrt());
        Assert.Contains(u.Speicher.Berichte(b.Id!.Value), x => x.Art == HilfeplanRegeln.ArtBericht);

        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(b.Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Kostenträger");
        Assert.Contains(f.Bewohner.Akte.Angaben.Zeilen, z => z.Beschriftung == "Hilfeplangespräch" && z.Wert.StartsWith("letztes ", StringComparison.Ordinal));
    });
}
