using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Tägliche Dokumentation: Prüfregeln, Schichten, Nachträge, nichts wird überschrieben, Demo.
[Collection("Datenordner")]
public sealed class BerichteTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private readonly Bewohner b;

    public BerichteTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        b = s.BewohnerSpeichern(BewohnerRegeln.AusEingabe(new BewohnerEingabe
        {
            Vorname = "Test", Nachname = "Tanne", Zimmer = "201", Geburtsdatum = "01.01.1970", Aufnahmedatum = "01.01.2020",
        }));
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private BerichtEingabe Eingabe(Action<BerichtEingabe>? aendern = null)
    {
        var e = new BerichtEingabe
        {
            BewohnerId = b.Id!.Value, Datum = "07.11.2026", Uhrzeit = "14:30", Verfasser = "AB",
            Bereiche = [2], Text = "Hat das Zimmer aufgeräumt.",
        };
        aendern?.Invoke(e);
        return e;
    }

    private static IReadOnlyList<string> FehlerVon(BerichtEingabe e) =>
        Assert.Throws<Eingabefehler>(() => BerichtRegeln.AusEingabe(e, Jetzt)).Fehler;

    [Theory]
    [InlineData(6, 0, Schicht.Frueh)]
    [InlineData(13, 59, Schicht.Frueh)]
    [InlineData(14, 0, Schicht.Spaet)]
    [InlineData(21, 59, Schicht.Spaet)]
    [InlineData(22, 0, Schicht.Nacht)]
    [InlineData(5, 59, Schicht.Nacht)]
    public void Schicht_aus_der_Uhrzeit(int stunde, int minute, Schicht erwartet)
    {
        Assert.Equal(erwartet, BerichtRegeln.SchichtZu(new DateTime(2026, 11, 7, stunde, minute, 0)));
    }

    [Fact]
    public void Uebergabe_beginnt_mit_der_vorigen_Schicht()
    {
        Assert.Equal(new DateTime(2026, 11, 7, 6, 0, 0), BerichtRegeln.UebergabeBeginn(Jetzt));                        // Spät → seit Früh
        Assert.Equal(new DateTime(2026, 11, 6, 14, 0, 0), BerichtRegeln.UebergabeBeginn(new DateTime(2026, 11, 7, 2, 0, 0)));   // Nacht → seit Spät
        Assert.Equal(new DateTime(2026, 11, 6, 22, 0, 0), BerichtRegeln.UebergabeBeginn(new DateTime(2026, 11, 7, 9, 0, 0)));   // Früh → seit Nacht
    }

    [Fact]
    public void Gueltiger_Bericht_Schicht_aus_Uhrzeit()
    {
        var bericht = BerichtRegeln.AusEingabe(Eingabe(), Jetzt);
        Assert.Equal((new DateTime(2026, 11, 7, 14, 30, 0), Schicht.Spaet, "AB", Jetzt), (bericht.Zeitpunkt, bericht.Schicht, bericht.Verfasser, bericht.Erfasst));
        Assert.Equal([2], bericht.Bereiche);
    }

    [Fact]
    public void Pruefregeln()
    {
        Assert.Equal(["Bitte einen Bewohner wählen.", "Datum fehlt.", "Uhrzeit fehlt.", "Verfasser (Kürzel) fehlt.",
                      "Lebensbereich: mindestens einen wählen oder „allgemein“.", "Text fehlt."],
            FehlerVon(new BerichtEingabe()));
        Assert.Equal(["Der Zeitpunkt liegt in der Zukunft."], FehlerVon(Eingabe(e => e.Uhrzeit = "18:00")));
        Assert.Equal(["Uhrzeit: 25:00 gibt es nicht."], FehlerVon(Eingabe(e => e.Uhrzeit = "25:00")));
        Assert.Equal(["Lebensbereich: „allgemein“ nur ohne Lebensbereich wählen."], FehlerVon(Eingabe(e => e.Allgemein = true)));
        Assert.Empty(BerichtRegeln.AusEingabe(Eingabe(e => { e.Bereiche = []; e.Allgemein = true; }), Jetzt).Bereiche);
        Assert.Equal(["Verfasser: nur Buchstaben und Ziffern."], FehlerVon(Eingabe(e => e.Verfasser = "A B")));
    }

    [Fact]
    public void Speichern_lesen_neueste_oben()
    {
        s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(e => e.Uhrzeit = "08:00"), Jetzt));
        s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(e => { e.Uhrzeit = "15:00"; e.Bereiche = [1, 4]; e.Wichtig = true; }), Jetzt));
        var liste = s.Berichte(b.Id!.Value);
        Assert.Equal(["15:00", "08:00"], liste.Select(x => BerichtRegeln.UhrzeitText(x.Zeitpunkt)));
        Assert.Equal([1, 4], liste[0].Bereiche);
        Assert.True(liste[0].Wichtig);
        Assert.Equal(Schicht.Frueh, liste[1].Schicht);
    }

    [Fact]
    public void Filter_Zeitraum_Bereich_Schicht_Suche()
    {
        s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(e => { e.Datum = "01.11.2026"; e.Uhrzeit = "08:00"; e.Text = "Hat gekocht."; e.Bereiche = [4]; }), Jetzt));
        var zweiter = s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(e => { e.Uhrzeit = "15:00"; e.Bereiche = []; e.Allgemein = true; }), Jetzt));
        s.NachtragSpeichern(zweiter.Id!.Value, "CD", "Ergänzung: mit Musik.", Jetzt);
        long id = b.Id!.Value;
        Assert.Single(s.Berichte(id, new BerichtFilter(Von: new DateOnly(2026, 11, 5))));
        Assert.Single(s.Berichte(id, new BerichtFilter(Bis: new DateOnly(2026, 11, 2))));
        Assert.Equal("Hat gekocht.", Assert.Single(s.Berichte(id, new BerichtFilter(Bereich: 4))).Text);
        Assert.True(Assert.Single(s.Berichte(id, new BerichtFilter(Bereich: BerichtFilter.Allgemein))).Allgemein);
        Assert.Single(s.Berichte(id, new BerichtFilter(Schicht: Schicht.Frueh)));
        Assert.Single(s.Berichte(id, new BerichtFilter(Suche: "MUSIK")));   // auch in Nachträgen, Groß/klein egal
        Assert.Empty(s.Berichte(id, new BerichtFilter(Suche: "Fahrrad")));
    }

    [Fact]
    public void Korrektur_ist_ein_Nachtrag_der_Text_bleibt()
    {
        var bericht = s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(), Jetzt));
        var spaeter = Jetzt.AddMinutes(10);
        s.NachtragSpeichern(bericht.Id!.Value, "CD", "Korrektur: Es war 14:45.", spaeter);
        var gelesen = s.BerichtLesen(bericht.Id.Value)!;
        Assert.Equal("Hat das Zimmer aufgeräumt.", gelesen.Text);
        var nachtrag = Assert.Single(gelesen.Nachtraege);
        Assert.Equal(("CD", "Korrektur: Es war 14:45.", spaeter), (nachtrag.Verfasser, nachtrag.Text, nachtrag.Erfasst));
        Assert.Throws<Eingabefehler>(() => s.NachtragSpeichern(bericht.Id.Value, "", "", spaeter));
    }

    [Fact]
    public void Berichte_lassen_sich_nicht_ueberschreiben()
    {
        var bericht = s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(), Jetzt));
        Assert.Throws<InvalidOperationException>(() => s.BerichtSpeichern(bericht));
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE berichte SET text = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
        Assert.Equal("Hat das Zimmer aufgeräumt.", s.BerichtLesen(bericht.Id!.Value)!.Text);
    }

    [Fact]
    public void Nur_wichtige_filtern()
    {
        s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(e => e.Uhrzeit = "08:00"), Jetzt));
        s.BerichtSpeichern(BerichtRegeln.AusEingabe(Eingabe(e => { e.Uhrzeit = "09:00"; e.Wichtig = true; }), Jetzt));
        Assert.True(Assert.Single(s.Berichte(b.Id!.Value, new BerichtFilter(NurWichtige: true))).Wichtig);
        Assert.Equal(1, s.WichtigeBerichteSeit(b.Id.Value, Jetzt.AddDays(-7)));
    }

    // ----- Übergabe -----

    [Fact]
    public void Laufzeit_der_Uebergabe()
    {
        Assert.Equal(new DateTime(2026, 11, 7, 22, 0, 0) + TimeSpan.FromHours(8),   // Spät → Ende der folgenden Nachtschicht
            UebergabeRegeln.GiltBis(UebergabeDauer.NaechsteSchicht, Jetzt));
        Assert.Equal(new DateTime(2026, 11, 10).AddDays(1).AddTicks(-1), UebergabeRegeln.GiltBis(UebergabeDauer.DreiTage, Jetzt));
        Assert.Equal(new DateTime(2026, 11, 14).AddDays(1).AddTicks(-1), UebergabeRegeln.GiltBis(UebergabeDauer.Woche, Jetzt));
        Assert.Equal(new DateTime(2026, 11, 20).AddDays(1).AddTicks(-1), UebergabeRegeln.GiltBis(UebergabeDauer.BisDatum, Jetzt, new DateOnly(2026, 11, 20)));
        Assert.Null(UebergabeRegeln.GiltBis(UebergabeDauer.BisErledigt, Jetzt));
    }

    [Fact]
    public void Bericht_mit_Uebergabe_bleibt_bis_Ablauf_oder_erledigt()
    {
        var e = Eingabe(x => { x.Uebergabe = true; x.UebergabeDauer = UebergabeDauer.NaechsteSchicht; x.UebergabeText = "Bitte nachfragen."; });
        var bericht = s.BerichtSpeichern(BerichtRegeln.AusEingabe(e, Jetzt), BerichtRegeln.UebergabeAus(e));
        var u = Assert.Single(s.Uebergaben(Jetzt));
        Assert.Equal((b.Id, bericht.Id, "Bitte nachfragen.", "AB"), (u.BewohnerId, u.BerichtId, u.Anzeige, u.Verfasser));
        Assert.Equal("Hat das Zimmer aufgeräumt.", u.Bericht!.Text);
        Assert.Single(s.Uebergaben(Jetzt.AddHours(14)));          // nächste Schicht: noch da
        Assert.Empty(s.Uebergaben(Jetzt.AddHours(16)));           // danach abgelaufen …
        Assert.Single(s.Uebergaben(Jetzt.AddHours(16), auchErledigte: true));   // … aber einsehbar

        s.UebergabeVerlaengern(u.Id, new UebergabeWunsch(UebergabeDauer.DreiTage), "CD", Jetzt.AddHours(1));
        var verlaengert = s.UebergabeLesen(u.Id)!;
        Assert.Equal(1, verlaengert.Verlaengert);
        Assert.Single(s.Uebergaben(Jetzt.AddDays(2)));
        s.UebergabeErledigt(u.Id, "EF", Jetzt.AddHours(2));
        var erledigt = s.UebergabeLesen(u.Id)!;
        Assert.Equal(("EF", Jetzt.AddHours(2)), (erledigt.ErledigtVon, erledigt.Erledigt!.Value));
        Assert.Empty(s.Uebergaben(Jetzt.AddHours(3)));
        Assert.StartsWith("erledigt", UebergabeRegeln.GueltigText(erledigt, Jetzt.AddHours(3)));
    }

    [Fact]
    public void Gelesen_neu_und_Zaehler_fuer_die_Karte()
    {
        var e = Eingabe(x => { x.Uebergabe = true; x.UebergabeDauer = UebergabeDauer.BisErledigt; });
        s.BerichtSpeichern(BerichtRegeln.AusEingabe(e, Jetzt), BerichtRegeln.UebergabeAus(e));
        var u = Assert.Single(s.Uebergaben(Jetzt));
        Assert.True(UebergabeRegeln.Neu(u, "CD", letzterBesuch: Jetzt.AddHours(-2)));
        Assert.False(UebergabeRegeln.Neu(u, "CD", letzterBesuch: Jetzt.AddHours(1)));
        Assert.False(UebergabeRegeln.Neu(u, "", null));   // ohne Kürzel kein „neu“
        Assert.Equal(1, s.UngeleseneUebergaben(b.Id!.Value, "CD", Jetzt));
        s.UebergabeGelesen(u.Id, "CD", Jetzt);
        s.UebergabeGelesen(u.Id, "CD", Jetzt.AddMinutes(5));   // doppelt zählt nicht
        Assert.Equal(0, s.UngeleseneUebergaben(b.Id.Value, "cd", Jetzt));
        Assert.Equal(1, s.UngeleseneUebergaben(b.Id.Value, "AB", Jetzt));
        Assert.Equal(["CD"], s.UebergabeLesen(u.Id)!.Gelesen.Select(g => g.Kuerzel));
        Assert.Throws<Eingabefehler>(() => s.UebergabeGelesen(u.Id, "", Jetzt));
        Assert.Null(s.LetzterUebergabeBesuch("CD"));
        s.UebergabeBesuchMerken("CD", Jetzt);
        Assert.Equal(Jetzt, s.LetzterUebergabeBesuch("cd"));
    }

    [Fact]
    public void Wohnbereich_Notiz_ohne_Bericht_und_ohne_Bewohner()
    {
        var notiz = s.WohnbereichNotiz("Getränkelieferung morgen.", "AB", new UebergabeWunsch(UebergabeDauer.Woche), Jetzt);
        Assert.True(notiz.Allgemein);
        Assert.Null(notiz.Bericht);
        Assert.Equal("Getränkelieferung morgen.", notiz.Anzeige);
        Assert.Throws<Eingabefehler>(() => s.WohnbereichNotiz(" ", "AB", new UebergabeWunsch(), Jetzt));
        Assert.Throws<Eingabefehler>(() => s.WohnbereichNotiz("x", "AB", new UebergabeWunsch(UebergabeDauer.BisDatum, new DateOnly(2026, 1, 1)), Jetzt));
    }

    [Fact]
    public void Uebergaben_lassen_sich_nicht_ueberschreiben()
    {
        s.WohnbereichNotiz("Notiz", "AB", new UebergabeWunsch(), Jetzt);
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE uebergaben SET text = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Wortliste_fuer_den_Hinweis()
    {
        var woerter = UebergabeWoerter.Laden();
        Assert.Contains("Arzt", woerter);
        Assert.Equal(["Arzt", "Termin"], UebergabeWoerter.Gefunden("Arzttermin am Donnerstag", woerter));
        Assert.Empty(UebergabeWoerter.Gefunden("Hat Karten gespielt.", woerter));
        File.WriteAllText(Pfade.ImDatenordner(UebergabeWoerter.Datei), "# eigene\nZahnbürste\n");
        Assert.Contains("Zahnbürste", UebergabeWoerter.Laden());
    }

    [Fact]
    public void Demo_Berichte_der_letzten_vier_Wochen_nichts_in_der_Zukunft()
    {
        s.DemoZuruecksetzen();
        var bewohner = s.AlleBewohner();
        Assert.All(bewohner, x => Assert.True(s.Berichte(x.Id!.Value).Count >= 28));
        var alle = bewohner.SelectMany(x => s.Berichte(x.Id!.Value)).ToList();
        Assert.All(alle, x => Assert.InRange(x.Zeitpunkt, Jetzt.Date.AddDays(-28), Jetzt));
        Assert.All(alle, x => Assert.True(x.Erfasst <= Jetzt));
        Assert.Equal([1, 2, 3, 4, 5], alle.SelectMany(x => x.Bereiche).Distinct().Order());
        Assert.Contains(alle, x => x.Allgemein);
        Assert.Contains(alle, x => x.Wichtig);
        Assert.Contains(alle, x => x.Nachtraege.Count > 0);
        Assert.DoesNotContain(alle, x => x.Text.Contains(';'));
        var offen = s.Uebergaben(Jetzt);
        var alleUebergaben = s.Uebergaben(Jetzt, auchErledigte: true);
        Assert.True(offen.Count >= 5);
        Assert.Equal(2, offen.Count(u => u.Allgemein));
        Assert.Contains(offen, u => u.GiltBis is null);
        Assert.Contains(offen, u => u.Text.Length > 0);
        Assert.Contains(offen, u => u.Gelesen.Count > 0);
        Assert.Contains(alleUebergaben, u => u.Erledigt is not null);
        Assert.Contains(alleUebergaben, u => u.Erledigt is null && u.GiltBis < Jetzt);   // abgelaufen
        Assert.All(alleUebergaben, u => Assert.True(u.Erstellt <= Jetzt));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(15, 20)]
    [InlineData(23, 50)]
    public void Demo_Marken_auf_der_Karte_zu_jeder_Uhrzeit(int stunde, int minute)
    {
        var jetzt = new DateTime(2026, 11, 7, stunde, minute, 0);
        s.Uhr = () => jetzt;
        s.DemoZuruecksetzen();
        var ahorn = s.AlleBewohner().First(x => x.Nachname == "Ahorn");
        Assert.Equal(1, s.UngeleseneUebergaben(ahorn.Id!.Value, "", jetzt));     // ohne Kürzel: alle offenen
        Assert.Equal(0, s.UngeleseneUebergaben(ahorn.Id!.Value, "AB", jetzt));   // AB hat sie gelesen
        Assert.True(s.WichtigeBerichteSeit(ahorn.Id!.Value, jetzt.AddDays(-7)) >= 1);
        var ginster = s.AlleBewohner().First(x => x.Nachname == "Ginster");
        Assert.Equal(1, s.UngeleseneUebergaben(ginster.Id!.Value, "", jetzt));
        var birke = s.AlleBewohner().First(x => x.Nachname == "Birke");
        Assert.Equal(0, s.UngeleseneUebergaben(birke.Id!.Value, "", jetzt));     // ihre ist erledigt
    }

    [Fact]
    public void Demo_Zufall_haengt_nicht_an_der_Uhrzeit()
    {
        var bewohner = s.AlleBewohner();
        // Die Übergabe-Berichte entstehen nach den Tagesberichten: Ihr Text darf nicht davon abhängen,
        // wie viele Berichte von heute schon in der Vergangenheit liegen.
        List<string> Texte(DateTime jetzt) => Demodaten.Berichte(bewohner, jetzt).Berichte
            .Where(x => x.Uebergabe is not null).Select(x => x.Bericht.Text).ToList();
        Assert.Equal(Texte(new DateTime(2026, 11, 7, 0, 30, 0)), Texte(new DateTime(2026, 11, 7, 23, 50, 0)));
    }

    [Fact]
    public void Nach_dem_Update_kommen_Demo_Berichte_dazu()
    {
        s.DemoZuruecksetzen();
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM berichte; DELETE FROM meta WHERE schluessel = 'demo_berichte'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.NotEmpty(s.Berichte(s.AlleBewohner()[0].Id!.Value));
        Assert.False(s.DemoBeimErstenStart());
    }
}
