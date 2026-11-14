using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 52: Seite „HEB-Fristen“ nach Fälligkeit (überfällig zuerst), Fälligkeit ändern mit Grund, Bogen anlegen,
// abgegeben mit nächster Fälligkeit, Drucken; Ganztagseintrag im Bereichskalender; Fristzeile im Reiter „HEB“ der Akte.
[Collection("Oberflaeche")]
public class HebFristenOberflaechenTests
{
    [Fact]
    public void Liste_aendern_Bogen_abgegeben_Kalender_und_Akte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen("R1");
        f.BereichWaehlen("HEB-Fristen");
        var s = f.HebFristen;
        Assert.Equal(u.Speicher.AlleBewohner().Count(b => b.Wohnbereich == "R1"), s.Liste.Items.Count);
        Assert.StartsWith("HEB überfällig seit", s.Liste.Items[0].SubItems[5].Text);
        Assert.Contains("1 überfällig", s.KopfText);
        Hilfe.Foto(f, "182_heb_fristen");

        var heute = DateOnly.FromDateTime(DateTime.Now);
        s.Liste.Items[0].Selected = true;
        var b = s.Gewaehlt!.Bewohner;
        Assert.Equal("Neuer HEB-Bogen …", s.BogenKnopf.Text);   // der letzte Bogen ist abgegeben
        var antworten = new Queue<string>([Datum.Text(heute.AddDays(100)), "Schreiben des Bezirks (Test)"]);
        s.Fragen = (_, _) => antworten.Dequeue();
        Assert.True(s.FaelligAendern());
        var zeile = s.Zeilen.Single(z => z.Bewohner.Id == b.Id);
        Assert.Equal((HebFristZustand.Eingetragen, heute.AddDays(100)), (zeile.Zustand, zeile.FaelligAm));

        // Kalender: Ganztagseintrag „HEB fällig“ mit dem Namen, nicht in der .ics-Ausgabe.
        f.BereichWaehlen("Kalender");
        f.Kalender.TagWaehlen(heute.AddDays(100));
        Assert.Contains(f.Kalender.Termine(), k => k.Aktuell.Titel == HebFristRegeln.KalenderTitel && k.Aktuell.Bewohner.SequenceEqual([b.Id!.Value]));

        // Bogen anlegen (ersetzt), dann fertig und abgegeben: die nächste Fälligkeit kommt aus dem Vorschlag.
        f.BereichWaehlen("HEB-Fristen");
        s.Liste.Items.Cast<ListViewItem>().Single(i => ((HebFristStand)i.Tag!).Bewohner.Id == b.Id).Selected = true;
        HebFristStand? gezeigt = null;
        s.BogenZeigen = z => gezeigt = z;
        Assert.True(s.Bogen());
        Assert.Equal(b.Id, gezeigt!.Bewohner.Id);
        var bogen = u.Speicher.HebAnlegen(b.Id!.Value, Bogenart.B, u.Speicher.HebBoegen(b.Id!.Value)[^1].Bis, heute, "EF");
        s.Aktualisieren();
        s.Liste.Items.Cast<ListViewItem>().Single(i => ((HebFristStand)i.Tag!).Bewohner.Id == b.Id).Selected = true;
        Assert.Equal("in Arbeit", s.Liste.SelectedItems[0].SubItems[3].Text);
        Assert.NotNull(Stil.Sperrgrund(s.AbgegebenKnopf));   // Entwurf
        u.Speicher.HebSpeichern(new HebFassung { BogenId = bogen.Id, Fertig = true, Erstellt = DateTime.Now, Kuerzel = "EF" });
        s.Aktualisieren();
        s.Liste.Items.Cast<ListViewItem>().Single(i => ((HebFristStand)i.Tag!).Bewohner.Id == b.Id).Selected = true;
        antworten = new Queue<string>(["", ""]);
        Assert.True(s.Abgegeben());
        zeile = s.Zeilen.Single(z => z.Bewohner.Id == b.Id);
        Assert.Equal((HebFristZustand.Eingetragen, HebBogenStand.Offen, bogen.Id), (zeile.Zustand, zeile.Stand, zeile.LetzteAbgabe!.BogenId));
        Assert.True(zeile.FaelligAm > heute);

        string? titel = null;
        s.Vorschau = (_, t) => titel = t;
        Assert.True(s.Drucken());
        Assert.Equal("HEB-Fristen", titel);

        // Akte, Reiter „HEB“: Fristzeile oben, Knöpfe; Stammdaten mit „Nächster HEB“.
        f.Bewohner.AkteZeigen(b.Id!.Value);
        Assert.StartsWith("fällig am", f.Bewohner.Akte.HebText.Text);
        f.Bewohner.Akte.Reiter.Waehlen("HEB");
        var h = f.Bewohner.Akte.Heb;
        Assert.StartsWith("Nächster HEB: fällig am", h.Faellig.AccessibleName);
        h.Fragen = (_, _) => "Auszug geplant (Test)";
        Assert.True(h.KeinHeb());
        Assert.StartsWith("Nächster HEB: kein HEB nötig", h.Faellig.AccessibleName);
        Assert.NotNull(Stil.Sperrgrund(h.AbgegebenKnopf));
    });
}
