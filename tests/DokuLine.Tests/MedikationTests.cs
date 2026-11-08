using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Medikation: Grundverordnung bleibt, Pause, Stufenplan, Absetzen, „Was gilt heute“, Demo.
[Collection("Datenordner")]
public sealed class MedikationTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private static readonly DateOnly Heute = new(2026, 11, 7);
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private readonly Bewohner b;
    private readonly Medikament m;

    public MedikationTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
        b = s.AlleBewohner().First(x => x.Nachname == "Birke");
        m = s.Medikamente("Quetiapin")[0];
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Verordnung Neu(Action<VerordnungEingabe>? aendern = null)
    {
        var e = new VerordnungEingabe
        {
            BewohnerId = b.Id!.Value, MedikamentId = m.Id, Staerke = "25 mg", Morgens = "0", Abends = "1",
            Beginn = "01.10.2026", Arzt = "Dr. Test (ausgedacht)", AngeordnetAm = "01.10.2026", Kuerzel = "AB",
        };
        aendern?.Invoke(e);
        return s.VerordnungSpeichern(MedikationRegeln.AusEingabe(e, m.Anzeige, Jetzt));
    }

    [Theory]
    [InlineData("1", 1.0)]
    [InlineData("0,5", 0.5)]
    [InlineData("½", 0.5)]
    [InlineData("1 ½", 1.5)]
    [InlineData("", 0.0)]
    public void Mengen_lesen(string text, double erwartet) => Assert.Equal((decimal)erwartet, MedikationRegeln.Menge(text));

    [Fact]
    public void Pruefregeln()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => MedikationRegeln.AusEingabe(new VerordnungEingabe { Morgens = "x" }, "", Jetzt)).Fehler;
        Assert.Contains("Bitte ein Medikament aus der Liste wählen.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("Kürzel fehlt"));
        Assert.Contains(fehler, f => f.StartsWith("Menge morgens"));
        Assert.Contains("Beginn fehlt.", fehler);
        Assert.Contains("Anordnung: Wer hat es angeordnet? (Arzt)", fehler);
        Assert.Contains(Assert.Throws<Eingabefehler>(() => Neu(e => e.Abends = "0")).Fehler, f => f.StartsWith("Dosis: Mindestens"));
        Assert.Contains(Assert.Throws<Eingabefehler>(() => Neu(e => { e.Bedarf = true; e.Morgens = "1"; })).Fehler, f => f.StartsWith("Bedarf: Bei welchem"));
    }

    [Fact]
    public void Grundverordnung_gilt_ab_Beginn()
    {
        var v = Neu();
        Assert.Equal(Zustand.NochNicht, MedikationRegeln.Stand(v, new DateOnly(2026, 9, 30)).Zustand);
        var heute = MedikationRegeln.Stand(v, Heute);
        Assert.Equal(Zustand.Gilt, heute.Zustand);
        Assert.Equal("0–0–1–0", heute.Dosis.Text);
        Assert.Equal("Grundverordnung", heute.Text);
        Assert.True(heute.Sichtbar);
    }

    [Fact]
    public void Pause_von_bis_danach_wieder_die_Grundverordnung()
    {
        var v = Neu();
        v = s.AenderungSpeichern(v.Id, MedikationRegeln.Pause("05.11.2026", "09.11.2026", "Dr. Test", "05.11.2026", "", "AB", Jetzt));
        var heute = MedikationRegeln.Stand(v, Heute);
        Assert.Equal(Zustand.Pausiert, heute.Zustand);
        Assert.Equal("pausiert bis Mo 09.11.", heute.Text);
        Assert.True(heute.Sichtbar);   // pausiert steht sichtbar da, nicht einfach nicht
        Assert.Equal(Zustand.Gilt, MedikationRegeln.Stand(v, new DateOnly(2026, 11, 10)).Zustand);
        Assert.Equal("0–0–1–0", v.Dosis.Text);   // Grundverordnung unverändert
    }

    [Fact]
    public void Pause_bis_auf_Weiteres_und_beenden()
    {
        var v = Neu();
        v = s.AenderungSpeichern(v.Id, MedikationRegeln.Pause("06.11.2026", "", "Dr. Test", "06.11.2026", "", "AB", Jetzt));
        Assert.Equal("pausiert bis auf Weiteres", MedikationRegeln.Stand(v, new DateOnly(2027, 1, 1)).Text);
        Assert.Throws<Eingabefehler>(() => s.AenderungSpeichern(v.Id, MedikationRegeln.PauseEnde("01.11.2026", "Dr. Test", "07.11.2026", "AB", Jetzt)));
        v = s.AenderungSpeichern(v.Id, MedikationRegeln.PauseEnde("08.11.2026", "Dr. Test", "07.11.2026", "AB", Jetzt.AddMinutes(1)));
        Assert.Equal(Zustand.Pausiert, MedikationRegeln.Stand(v, Heute).Zustand);
        Assert.Equal(Zustand.Gilt, MedikationRegeln.Stand(v, new DateOnly(2026, 11, 8)).Zustand);
    }

    [Fact]
    public void Stufenplan_ansetzen_letzte_Stufe_wird_neue_Grundverordnung()
    {
        var v = Neu(e => { e.Beginn = "01.11.2026"; e.AngeordnetAm = "01.11.2026"; });
        v = s.AenderungSpeichern(v.Id, MedikationRegeln.Stufenplan(
            [("01.11.2026", "0", "0", "½", "0"), ("05.11.2026", "0", "0", "1", "0"), ("10.11.2026", "0", "0", "2", "0"), ("15.11.2026", "0", "0", "3", "0")],
            "Dr. Test", "01.11.2026", "", "AB", Jetzt));
        var heute = MedikationRegeln.Stand(v, Heute);
        Assert.Equal(Zustand.Stufenplan, heute.Zustand);
        Assert.Equal("Stufe 2 von 4", heute.Text);
        Assert.Equal("nächste Stufe ab Di 10.11.: 0–0–2–0", heute.Naechstes);
        Assert.Equal("0–0–1–0", heute.Dosis.Text);
        var danach = MedikationRegeln.Stand(v, new DateOnly(2026, 12, 1));
        Assert.Equal(Zustand.Gilt, danach.Zustand);
        Assert.Equal("neue Grundverordnung seit So 15.11.", danach.Text);
        Assert.Equal("0–0–3–0", danach.Dosis.Text);
        Assert.Contains(MedikationRegeln.Verlauf(v), z => z.Contains("Stufenplan") && z.Contains("neue Grundverordnung"));
    }

    [Fact]
    public void Stufenplan_auf_null_heisst_abgesetzt()
    {
        var v = Neu();
        v = s.AenderungSpeichern(v.Id, MedikationRegeln.Stufenplan(
            [("01.11.2026", "0", "0", "½", "0"), ("14.11.2026", "0", "0", "0", "0")], "Dr. Test", "01.11.2026", "", "AB", Jetzt));
        Assert.Equal("Stufe 1 von 2", MedikationRegeln.Stand(v, Heute).Text);
        var weg = MedikationRegeln.Stand(v, new DateOnly(2026, 11, 14));
        Assert.Equal(Zustand.Abgesetzt, weg.Zustand);
        Assert.False(weg.Sichtbar);
        Assert.Equal("abgesetzt am Sa 14.11.", weg.Text);
        Assert.Throws<Eingabefehler>(() => s.AenderungSpeichern(v.Id, MedikationRegeln.Pause("20.11.2026", "", "Dr. Test", "07.11.2026", "", "AB", Jetzt)));
    }

    [Fact]
    public void Absetzen_und_Pruefungen_gegen_die_Verordnung()
    {
        var v = Neu();
        Assert.Throws<Eingabefehler>(() => s.AenderungSpeichern(v.Id, MedikationRegeln.Pause("01.09.2026", "", "Dr. Test", "07.11.2026", "", "AB", Jetzt)));   // vor Beginn
        Assert.Contains(Assert.Throws<Eingabefehler>(() => MedikationRegeln.Stufenplan(
            [("10.11.2026", "1", "0", "0", "0"), ("09.11.2026", "0", "0", "0", "0")], "Dr. Test", "07.11.2026", "", "AB", Jetzt)).Fehler,
            f => f.Contains("muss nach Stufe 1 beginnen"));
        v = s.AenderungSpeichern(v.Id, MedikationRegeln.Absetzen("07.11.2026", "Dr. Test", "07.11.2026", "", "AB", Jetzt));
        Assert.Equal(Zustand.Abgesetzt, MedikationRegeln.Stand(v, Heute).Zustand);
        Assert.Equal(Zustand.Gilt, MedikationRegeln.Stand(v, Heute.AddDays(-1)).Zustand);
        Assert.Equal(2, MedikationRegeln.Verlauf(v).Count);
    }

    [Fact]
    public void Nichts_wird_ueberschrieben()
    {
        var v = Neu();
        s.AenderungSpeichern(v.Id, MedikationRegeln.Absetzen("07.11.2026", "Dr. Test", "07.11.2026", "", "AB", Jetzt));
        using var befehl = s.Verbindung.CreateCommand();
        foreach (var sql in new[] { "UPDATE verordnungen SET abends = '2'", "UPDATE verordnung_aenderungen SET von = '2026-01-01'" })
        {
            befehl.CommandText = sql;
            Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => s.VerordnungSpeichern(v));
    }

    [Fact]
    public void Demo_Medikation_zeigt_alle_Faelle()
    {
        var staende = s.AlleBewohner().SelectMany(x => s.Verordnungen(x.Id!.Value)).Select(v => MedikationRegeln.Stand(v, Heute)).ToList();
        Assert.True(staende.Count >= 10);
        Assert.Contains(staende, x => x.Zustand == Zustand.Pausiert);
        Assert.Contains(staende, x => x.Zustand == Zustand.Stufenplan && x.Text.StartsWith("Stufe 2 von 3"));
        Assert.Contains(staende, x => x.Zustand == Zustand.Stufenplan && x.Naechstes!.EndsWith("0–0–0–0"));   // ausschleichen
        Assert.Contains(staende, x => x.Zustand == Zustand.Abgesetzt);
        Assert.Contains(staende, x => x.Text.StartsWith("neue Grundverordnung seit"));
        Assert.Contains(staende, x => x.Verordnung.Bedarf);
        Assert.All(staende, x => Assert.Contains("(ausgedacht)", x.Verordnung.Anordnung.Arzt));
    }

    [Fact]
    public void Nach_dem_Update_kommt_Demo_Medikation_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM verordnungen; DELETE FROM meta WHERE schluessel = 'demo_medikation'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.NotEmpty(s.Verordnungen(s.AlleBewohner()[0].Id!.Value));
        Assert.False(s.DemoBeimErstenStart());
    }
}
