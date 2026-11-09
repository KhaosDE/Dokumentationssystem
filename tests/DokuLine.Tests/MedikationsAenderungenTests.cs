using DokuLine.Kern;

namespace DokuLine.Tests;

// Medikamentenänderungen: sieben Tage auffällig, automatisch in die Übergabe, bis alle drei Schichten gelesen haben.
[Collection("Datenordner")]
public sealed class MedikationsAenderungenTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public MedikationsAenderungenTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;

    [Fact]
    public void Dosis_geaendert_wird_Bericht_und_Uebergabe_bis_drei_Schichten_gelesen_haben()
    {
        var v = s.Verordnungen(B("Ahorn")).First(x => !x.Bedarf && MedikationRegeln.Stand(x, Heute).Sichtbar);
        var plan = MedikationRegeln.Stufenplan([(Datum.Text(Heute), "2", "0", "0", "0")], "Dr. Test (ausgedacht)", Datum.Text(Heute), "", "AB", Jetzt);
        s.AenderungSpeichern(v.Id, plan);

        var liste = s.Medikamentenaenderungen(Heute).Where(a => a.VerordnungId == v.Id).ToList();
        var neu = Assert.Single(liste, a => a.Was.StartsWith("Dosis geändert: "));
        Assert.EndsWith("→ 2–0–0–0", neu.Was);
        var u = s.Uebergaben(Jetzt).Single(x => x.BewohnerId == B("Ahorn") && x.Anzeige.StartsWith("Medikamentenänderung: "));
        Assert.Equal($"Medikamentenänderung: {v.Medikament}: {neu.Was} ab {Datum.Text(Heute)}, angeordnet von Dr. Test (ausgedacht).", u.Anzeige);
        Assert.True(u.Bericht!.Wichtig);
        Assert.Equal(DokuRegeln.ArtMedikation, u.Bericht.Art);
        Assert.Contains(DokuMarke.MedikationGeaendert, DokuRegeln.Marken(u.Bericht));
        Assert.Equal(Jetzt.AddDays(7), u.GiltBis);

        // Zwei Schichten reichen nicht; erst die dritte nimmt sie aus der Übergabe.
        s.UebergabeGelesen(u.Id, "AB", Heute.ToDateTime(new TimeOnly(10, 0)));
        s.UebergabeGelesen(u.Id, "CD", Heute.ToDateTime(new TimeOnly(15, 0)));
        Assert.Contains(s.Uebergaben(Jetzt.AddHours(8)), x => x.Id == u.Id);
        s.UebergabeGelesen(u.Id, "EF", Heute.ToDateTime(new TimeOnly(23, 0)));
        Assert.DoesNotContain(s.Uebergaben(Jetzt.AddHours(14)), x => x.Id == u.Id);
    }

    [Fact]
    public void Neue_Verordnung_und_sieben_Tage_auffaellig()
    {
        Assert.True(MedikationsAenderungen.Frisch(Heute, Heute));
        Assert.True(MedikationsAenderungen.Frisch(Heute.AddDays(-6), Heute));
        Assert.False(MedikationsAenderungen.Frisch(Heute.AddDays(-7), Heute));
        Assert.False(MedikationsAenderungen.Frisch(Heute.AddDays(1), Heute));

        var med = s.Medikamente().First(m => !m.Gesperrt);
        var v = s.VerordnungSpeichern(MedikationRegeln.AusEingabe(new VerordnungEingabe
        {
            BewohnerId = B("Distel"), MedikamentId = med.Id, Staerke = "5 mg", Einheit = "Stück", Morgens = "1", Mittags = "0", Abends = "0",
            Nachts = "0", Beginn = Datum.Text(Heute), Arzt = "Dr. Test (ausgedacht)", AngeordnetAm = Datum.Text(Heute), Kuerzel = "AB",
        }, med.Anzeige, Jetzt));
        Assert.Equal("neu seit 09.11.", MedikationsAenderungen.ZeilenText(v, Heute));
        Assert.Null(MedikationsAenderungen.ZeilenText(v, Heute.AddDays(7)));
        Assert.Contains(s.Uebergaben(Jetzt), x => x.BewohnerId == B("Distel") && x.Anzeige.Contains("neu verordnet (1–0–0–0)"));
    }
}
