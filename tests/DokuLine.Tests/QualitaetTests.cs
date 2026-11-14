using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 44: Qualitätsmanagement – Prüfpunkte je Bewohner (ein- und ausschaltbar), Kennzahlen je Wohnbereich, Position QMB
// mit Lese-Rechten, Bereichswahl „QM“, Prüfvermerk mit Verlauf, Hinweis als Aufgabe des Bereichs, Nachweis zum Drucken.
[Collection("Datenordner")]
public sealed class QualitaetTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public QualitaetTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static readonly QmEinstellung Vorgabe = new(new HashSet<Pruefpunkt>(), 14, 1, 2, 4);

    private static PruefDaten Leer(Bewohner b) => new(b, [], null, [], [], [], new Reflexion(1, ReflexionRegeln.Vorschlag(Heute), []), null,
        new Dictionary<Angabe, AngabeFassung>());

    [Fact]
    public void Pruefpunkte_mit_und_ohne_Maengel()
    {
        var b = new Bewohner { Id = 1, Vorname = "A", Nachname = "Ahorn (Test)", Zimmer = "101", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1), Wohnbereich = "R1" };
        var leer = QualitaetRegeln.Pruefen(Leer(b), Heute, Vorgabe);
        Assert.Equal(QualitaetRegeln.Alle, leer.Select(e => e.Punkt));
        Assert.Equal("Ziele nicht erfasst", leer[0].Text);
        Assert.False(leer.Single(e => e.Punkt == Pruefpunkt.Reflexion).Ok);
        Assert.True(leer.Single(e => e.Punkt == Pruefpunkt.Heb).Ok);   // nichts fällig gemeldet
        Assert.Equal("nicht erfasst: Allergien, Ärzte, Schweigepflicht", leer.Single(e => e.Punkt == Pruefpunkt.Sicherheit).Text);
        Assert.Equal("seit 4 Wochen kein Bericht: LB 1, 2, 3, 4, 5", leer.Single(e => e.Punkt == Pruefpunkt.Lebensbereiche).Text);

        // Mit Verzicht gelten Leitziel und Nahziele als dokumentiert; ausgeschaltete Punkte fehlen ganz.
        var verzicht = Leer(b) with { Verzicht = new ZielVerzicht(1, Heute.AddDays(-5), "möchte nicht", "EF", Jetzt.AddDays(-5)) };
        var aus = Vorgabe with { Aus = new HashSet<Pruefpunkt> { Pruefpunkt.Heb, Pruefpunkt.Sicherheit } };
        var mitVerzicht = QualitaetRegeln.Pruefen(verzicht, Heute, aus);
        Assert.Equal(QualitaetRegeln.Alle.Length - 2, mitVerzicht.Count);
        Assert.Equal(("kein Ziel benannt", true), (mitVerzicht[0].Text, mitVerzicht[0].Ok));
        Assert.True(mitVerzicht.Single(e => e.Punkt == Pruefpunkt.NahzielMassnahme).Ok);

        // Eine tägliche Maßnahme, die seit Wochen läuft und nie bestätigt wurde.
        var m = new Massnahme(5, 1, [new MassnahmeFassung { Titel = "Zimmer (Test)", Lebensbereich = 2, GiltAb = Heute.AddDays(-30), Rhythmus = MassnahmeRhythmus.Taeglich }]);
        var unbestaetigt = QualitaetRegeln.Pruefen(Leer(b) with { Massnahmen = [m] }, Heute, Vorgabe);
        Assert.Equal("1 Maßnahme seit 14 Tagen nicht bestätigt", unbestaetigt.Single(e => e.Punkt == Pruefpunkt.MassnahmenBestaetigt).Text);
        var bestaetigt = new MassnahmeBestaetigung(1, 5, 1, Heute.AddDays(-2), Schicht.Frueh, MassnahmeErgebnis.Durchgefuehrt, "", 20, "", "AB", Jetzt, 2, "Zimmer (Test)");
        Assert.True(QualitaetRegeln.Pruefen(Leer(b) with { Massnahmen = [m], Bestaetigungen = [bestaetigt] }, Heute, Vorgabe)
            .Single(e => e.Punkt == Pruefpunkt.MassnahmenBestaetigt).Ok);

        var zeile = new PruefZeile(b, leer, null, null);
        Assert.True(zeile.Maengel);
        Assert.StartsWith("Qualität: Unterlagen von A Ahorn (Test) (Zi. 101) bitte ergänzen – Leitziel vorhanden: Ziele nicht erfasst",
            QualitaetRegeln.HinweisText(b, zeile.MaengelTexte));
        Assert.Equal(new HashSet<Pruefpunkt> { Pruefpunkt.Heb, Pruefpunkt.Reflexion }, QualitaetRegeln.AusLesen("heb, Reflexion, gibtsnicht"));
        Assert.Equal("Reflexion,Heb", QualitaetRegeln.AusText([Pruefpunkt.Heb, Pruefpunkt.Reflexion, Pruefpunkt.Heb]));
    }

    [Fact]
    public void Position_QMB_Rechte_und_Bereichswahl_QM()
    {
        Assert.Contains(QualitaetRegeln.Position, RechteRegeln.Positionen);
        foreach (var f in new[] { Funktionen.Qualitaet, Funktionen.BereicheWohnbereiche, Funktionen.BereicheAlle, Funktionen.DokumenteAnsehen })
            Assert.True(RechteRegeln.Vorbelegt(QualitaetRegeln.Position, f), f);
        foreach (var f in new[] { Funktionen.BerichteSchreiben, Funktionen.MassnahmenBestaetigen, Funktionen.MassnahmenAnlegen, Funktionen.VitalwerteErfassen,
                     Funktionen.AerztlicheBearbeiten, Funktionen.AerztlicheAnsehen, Funktionen.BewohnerBearbeiten })
            Assert.False(RechteRegeln.Vorbelegt(QualitaetRegeln.Position, f), f);
        Assert.False(RechteRegeln.Vorbelegt("Mitarbeiter Wohnbereich", Funktionen.Qualitaet));
        Assert.True(RechteRegeln.Vorbelegt("Hausleitung", Funktionen.Qualitaet));
        Assert.True(s.Darf("QS", Funktionen.Qualitaet));
        Assert.False(s.Darf("QS", Funktionen.BerichteSchreiben));
        Assert.False(s.Darf("AB", Funktionen.Qualitaet));

        Assert.Contains(QualitaetRegeln.BereichQm, BereichswahlRegeln.Knoepfe(["R1"], f => s.Darf("QS", f)));
        Assert.DoesNotContain(QualitaetRegeln.BereichQm, BereichswahlRegeln.Knoepfe(["R1"], f => s.Darf("AB", f)));
        Assert.Equal(s.AlleBewohner().Count, BereichswahlRegeln.Bewohner(s.AlleBewohner(), QualitaetRegeln.BereichQm).Count);
        Assert.True(BereichswahlRegeln.Passt("R2", QualitaetRegeln.BereichQm));
        Assert.False(BereichswahlRegeln.Eingesprungen(QualitaetRegeln.BereichQm, "R1"));
    }

    [Fact]
    public void Pruefliste_Kennzahlen_Vermerk_Hinweis_und_Nachweis()
    {
        var bewohner = s.AlleBewohner();
        var liste = s.Pruefliste(bewohner, Heute);
        Assert.Equal(bewohner.Count, liste.Count);
        Assert.Contains(liste, z => z.Maengel);
        Assert.Contains(liste, z => z.Ergebnisse.Single(e => e.Punkt == Pruefpunkt.Leitziel).Text == "kein Ziel benannt");
        var zahlen = s.QmKennzahlen(bewohner, Heute);
        Assert.Equal(bewohner.Select(b => b.Wohnbereich).Distinct().Count(), zahlen.Count);
        Assert.All(zahlen, k => Assert.InRange(k.ZieleVollstaendig, 0, k.Bewohner));
        Assert.Contains(zahlen, k => k.NahzieleMitMassnahme > 0 && k.NahzieleMitMassnahme <= k.Nahziele);
        Assert.StartsWith($"{zahlen[0].Bereich}: Ziele vollständig ", QualitaetRegeln.KennzahlText(zahlen[0]));

        // Ausgeschaltete Prüfpunkte verschwinden aus der Liste, die Kennzahlen bleiben.
        Einstellungen.Aendern(w => w with { QmAus = "Heb,Sicherheit" });
        Assert.DoesNotContain(s.Pruefliste(bewohner, Heute)[0].Ergebnisse, e => e.Punkt is Pruefpunkt.Heb or Pruefpunkt.Sicherheit);
        Assert.Equal(zahlen, s.QmKennzahlen(bewohner, Heute));

        var mit = liste.First(z => z.Maengel).Bewohner;
        long id = mit.Id!.Value;
        Assert.Throws<Eingabefehler>(() => s.PruefvermerkSetzen(id, "", "AB", Jetzt));   // ohne Recht
        s.PruefvermerkSetzen(id, "Ziele besprochen", "QS", Jetzt);
        s.PruefvermerkSetzen(id, "", "QS", Jetzt.AddDays(1));
        Assert.Equal(2, s.Pruefvermerke(id).Count);
        Assert.Equal("geprüft am 10.11.2026 von QS", QualitaetRegeln.VermerkText(s.Pruefliste([mit], Heute).Single().Vermerk));

        Assert.Throws<Eingabefehler>(() => s.HinweisSenden(id, "x", "AB", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.HinweisSenden(id, " ", "QS", Jetzt));
        var hinweis = s.HinweisSenden(id, QualitaetRegeln.HinweisText(mit, ["Leitziel vorhanden: fehlt"]), "QS", Jetzt);
        var aufgabe = s.AufgabeLesen(hinweis.AufgabeId)!;
        Assert.Equal((id, mit.Wohnbereich, AufgabeFuer.WerZeitHat), (aufgabe.BewohnerId!.Value, aufgabe.Bereich, aufgabe.Fuer));
        Assert.Contains(s.Aufgaben(Jetzt, bereich: mit.Wohnbereich), a => a.Id == aufgabe.Id);
        Assert.Equal("Hinweis gesendet am 09.11.2026, offen", QualitaetRegeln.HinweisStand(s.Pruefliste([mit], Heute).Single().Hinweis));
        s.AufgabeErledigt(aufgabe.Id, "AB", Jetzt.AddHours(3));
        Assert.Equal("Hinweis gesendet am 09.11.2026, erledigt am 09.11.2026", QualitaetRegeln.HinweisStand(s.QmHinweise(id).Single()));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE qm_vermerke SET kuerzel = 'XX'";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());

        var nachweis = QualitaetRegeln.Nachweis(s.PruefDatenLesen(bewohner, Heute), Heute);
        Assert.All(nachweis, z => Assert.Equal(QualitaetRegeln.NachweisSpalten.Length, z.Length));
        Assert.Contains(nachweis, z => z[2].StartsWith("Leitziel: ", StringComparison.Ordinal));
        Assert.Contains(nachweis, z => z[2] == "Ziele: nicht erfasst");
        Assert.Contains(nachweis, z => z[2].StartsWith(ZielRegeln.KeinZielText, StringComparison.Ordinal));
    }
}
