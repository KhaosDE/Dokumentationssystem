using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Medikamentenänderungen: Kasten in der Übersicht, Marke in Liste und Akte, hervorgehobene Zeile im Medikamentenplan.
[Collection("Oberflaeche")]
public class MedikationsAenderungOberflaechenTests
{
    [Fact]
    public void Kasten_Marke_und_Zeile_im_Medikamentenplan() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Übersicht");
        var s = f.Uebersicht;
        var heute = DateOnly.FromDateTime(DateTime.Now);
        var r1 = u.Speicher.AlleBewohner().Where(b => b.Wohnbereich == "R1").Select(b => b.Id!.Value).ToHashSet();
        var geaendert = u.Speicher.Medikamentenaenderungen(heute).Where(a => r1.Contains(a.BewohnerId)).ToList();
        Assert.NotEmpty(geaendert);   // Demo: Pausen und Stufen um heute herum
        Assert.True(s.MedikationZeigen);
        Assert.Equal(geaendert.Count, s.Medikation.Anzahl);
        // Ein Bewohner, dessen geänderte Verordnung heute im Plan steht (nicht schon abgesetzt).
        long id = geaendert.First(a => MedikationRegeln.Stand(u.Speicher.Verordnung(a.VerordnungId)!, heute).Sichtbar).BewohnerId;
        Assert.Contains(s.Zeilen.Single(z => z.Bewohner.Id == id).Marken, m => m.Art == MarkenArt.Medikation);
        Hilfe.Foto(f, "120_medikamentenaenderungen");

        f.Bewohner.AkteZeigen(id);
        Assert.Contains(MedikationsAenderungen.MarkeText, f.Bewohner.Akte.KopfMarken);
        f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
        var plan = f.Bewohner.Akte.Medikation;
        var zeilen = plan.Tabelle.Rows.Cast<DataGridViewRow>().Concat(plan.BedarfTabelle.Rows.Cast<DataGridViewRow>()).ToList();
        Assert.Contains(zeilen, z => z.DefaultCellStyle.BackColor == Stil.MedikationZeile && z.Cells["status"].Value is string t && t.Length > 0);
        Hilfe.Foto(f, "121_medikamentenplan_geaendert");
    });
}
