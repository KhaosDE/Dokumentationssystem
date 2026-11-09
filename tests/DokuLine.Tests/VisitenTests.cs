using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Bauplan Schritt 26: Visiten mit Besprochenem, geänderten Anordnungen und Facharztterminen; beim
// Speichern entstehen sofort Aufgaben, Berichte (Lebensbereich 5) und Übergaben.
[Collection("Datenordner")]
public sealed class VisitenTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public VisitenTests()
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

    private VisiteEingabe Eingabe() => new()
    {
        Datum = "09.11.2026", Arzt = "Dr. Test (ausgedacht)", Teilnehmer = "EF", Kuerzel = "EF",
        Eintraege =
        [
            new VisiteEintragEingabe
            {
                BewohnerId = B("Ahorn"), Besprochen = "Schläft besser.",
                Anordnungen =
                [
                    (AnordnungArt.Dosis, s.Medikamente().First(m => m.Wirkstoff == "Quetiapin").Id, "Quetiapin"),
                    (AnordnungArt.Neu, null, "Beispielsalbe (ausgedacht)"),
                ],
            },
            new VisiteEintragEingabe { BewohnerId = B("Birke"), Besprochen = "Herzklopfen abklären.", Facharzt = "Kardiologie", Dringend = true },
            new VisiteEintragEingabe { BewohnerId = B("Clematis") },   // leer: zählt nicht
        ],
    };

    [Fact]
    public void Pruefregeln()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => VisitenRegeln.AusEingabe(new VisiteEingabe { Datum = "10.11.2026" }, Jetzt)).Fehler;
        Assert.Contains(fehler, f => f.Contains("in der Zukunft"));
        Assert.Contains("Arzt fehlt (aus dem Ärzteverzeichnis wählen).", fehler);
        Assert.DoesNotContain(fehler, f => f.Contains("Art der Visite"));   // Schritt 26c: entfällt
        Assert.Contains("Bitte für mindestens einen Bewohner eintragen, was besprochen wurde.", fehler);
        var e = Eingabe();
        e.Eintraege[0].Besprochen = "";
        e.Eintraege[1].Anordnungen.Add((AnordnungArt.Pause, null, " "));
        e.Eintraege[2].FacharztNoetig = true;
        fehler = Assert.Throws<Eingabefehler>(() => VisitenRegeln.AusEingabe(e, Jetzt)).Fehler;
        Assert.Contains("Bei jedem Bewohner bitte eintragen, was besprochen wurde.", fehler);
        Assert.Contains("Geänderte Anordnung: Medikament fehlt.", fehler);
        Assert.Contains("Facharzttermin nötig: Fachrichtung fehlt.", fehler);
        Assert.Equal(2, VisitenRegeln.AusEingabe(Eingabe(), Jetzt).Eintraege.Count);
    }

    [Fact]
    public void Speichern_blendet_Aufgaben_ein_und_schreibt_Berichte()
    {
        int aufgabenVorher = s.Aufgaben(Jetzt, auchErledigte: true).Count;
        var (visite, aufgaben) = s.VisiteSpeichern(VisitenRegeln.AusEingabe(Eingabe(), Jetzt));
        Assert.Equal(3, aufgaben.Count);
        Assert.Equal(aufgabenVorher + 3, s.Aufgaben(Jetzt, auchErledigte: true).Count);
        Assert.StartsWith("Medikation ändern: Quetiapin (Dosis geändert) – Visite 09.11.2026 (Dr. Test (ausgedacht))", aufgaben[0].Text);
        Assert.StartsWith("Neues Medikament anlegen: Beispielsalbe (ausgedacht) (neu verordnet)", aufgaben[1].Text);
        Assert.StartsWith("Termin bei Kardiologie vereinbaren (dringend)", aufgaben[2].Text);
        Assert.All(aufgaben, a => Assert.Equal(SchichtTermin.Zu(Jetzt), a.FuerSchicht));   // Aufgaben der laufenden Schicht
        Assert.Equal(VisitenAktion.MedikationAendern, s.VisitenAufgabe(aufgaben[0])!.Value.Aktion);
        Assert.Equal(VisitenAktion.MedikamentAnlegen, s.VisitenAufgabe(aufgaben[1])!.Value.Aktion);
        Assert.Equal(VisitenAktion.FacharztTermin, s.VisitenAufgabe(aufgaben[2])!.Value.Aktion);
        Assert.Null(s.VisitenAufgabe(s.Aufgaben(Jetzt).First(a => a.VisiteAnordnungId is null && a.VisiteEintragId is null)));

        // Berichte (Lebensbereich 5) und Übergaben.
        var bericht = s.Berichte(B("Ahorn")).First(b => b.Text.StartsWith("Visite 09.11.2026"));
        Assert.Equal([5], bericht.Bereiche);
        Assert.Contains("Anordnungen geändert: Quetiapin", bericht.Text);
        Assert.True(bericht.Uebergabe);
        Assert.Contains(s.Uebergaben(Jetzt), u => u.Text == "Visite: Facharzttermin nötig");
        Assert.DoesNotContain(s.Berichte(B("Clematis")), b => b.Text.StartsWith("Visite 09.11.2026"));

        // Nach dem Anlegen des Medikaments geht es mit der Verordnung weiter (auch über einen Handelsnamen).
        var neu = s.MedikamentAnlegen(MedikamentRegeln.AusEingabe(new MedikamentEingabe
        { Wirkstoff = "Beispielwirkstoff (ausgedacht)", Handelsnamen = "Beispielsalbe (ausgedacht)", Kuerzel = "CD" }, Jetzt));
        Assert.Equal(VisitenAktion.MedikationAendern, s.VisitenAufgabe(aufgaben[1])!.Value.Aktion);
        Assert.Equal(neu.Id, s.VisitenMedikament(s.VisitenAufgabe(aufgaben[1])!.Value.Anordnung!)!.Id);

        // Liste: Filter nach Bewohner und Zeitraum.
        Assert.Contains(s.Visiten(B("Birke")), v => v.Id == visite.Id);
        Assert.DoesNotContain(s.Visiten(B("Clematis")), v => v.Id == visite.Id);
        Assert.Empty(s.Visiten(von: new DateOnly(2026, 11, 10)));

        // Frei eingetragen, aber schon in der Datenbank: wird beim Speichern zugeordnet.
        var e2 = Eingabe();
        e2.Eintraege = [new VisiteEintragEingabe { BewohnerId = B("Clematis"), Besprochen = "x", Anordnungen = [(AnordnungArt.Pause, null, "mirtazapin")] }];
        var frei = s.VisiteSpeichern(VisitenRegeln.AusEingabe(e2, Jetzt));
        Assert.NotNull(frei.Visite.Eintraege[0].Anordnungen[0].MedikamentId);
        Assert.StartsWith("Medikation ändern: mirtazapin (pausiert)", frei.Aufgaben[0].Text);

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE visite_eintraege SET besprochen = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Facharzttermin_kommt_in_den_Kalender_und_auf_Wunsch_eine_Fahrt()
    {
        var aufgabe = s.VisiteSpeichern(VisitenRegeln.AusEingabe(Eingabe(), Jetzt)).Aufgaben[2];
        Assert.Throws<Eingabefehler>(() => s.FacharztTerminEintragen(s.VisiteSpeichern(VisitenRegeln.AusEingabe(Eingabe(), Jetzt)).Aufgaben[0], new DateOnly(2026, 11, 20), "10:00", "Praxis", "CD", Jetzt));
        var fahrt = FahrtRegeln.AusEingabe(new FahrtEingabe
        {
            Fahrzeug = "Caddy", Datum = "20.11.2026", Von = "09:30", Bis = "11:30", Zweck = FahrtZweck.Arzttermin, Fahrer = "CD",
            Bewohner = [B("Birke")], Ziel = "Kardiologische Praxis (ausgedacht)", Kuerzel = "CD",
        }, s.AktiveFahrzeuge(), Jetzt);
        var termin = s.FacharztTerminEintragen(aufgabe, new DateOnly(2026, 11, 20), "10:00", "Kardiologische Praxis (ausgedacht)", "CD", Jetzt, fahrt);
        Assert.Equal("Facharzt: Kardiologie", termin.Aktuell.Titel);
        Assert.Equal([B("Birke")], termin.Aktuell.Bewohner);
        Assert.False(s.AufgabeLesen(aufgabe.Id)!.Offen);
        Assert.Contains(s.Fahrten(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20)), f => f.Bewohner.Contains(B("Birke")));
    }

    [Fact]
    public void Demo_Visiten_mit_allen_drei_Faellen()
    {
        var visiten = s.Visiten();
        Assert.Equal(5, visiten.Count);   // drei in R1, seit Schritt 26h je eine in R2 und R3
        var alle = visiten.SelectMany(v => v.Eintraege).ToList();
        Assert.Contains(alle, e => e.Anordnungen.Any(a => a.MedikamentId is not null && a.Art == AnordnungArt.Dosis));
        Assert.Contains(alle, e => e.Anordnungen.Any(a => a.MedikamentId is null && a.Art == AnordnungArt.Neu));
        Assert.Contains(alle, e => e.Facharzt == "Neurologie" && e.Dringend);
        Assert.All(s.Aufgaben(Jetzt, auchErledigte: true).Where(a => a.VisiteAnordnungId is not null || a.VisiteEintragId is not null), a => Assert.False(a.Offen));
        Assert.Contains(s.Berichte(B("Distel")), b => b.Text.Contains("Facharzttermin nötig: Neurologie"));
    }

    [Fact]
    public void Entwurf_je_Bereich_bis_zum_Abschliessen()
    {
        Assert.Null(s.VisitenEntwurf("R1"));
        var v = new VisitenEntwurf { Bereich = "R1", Datum = new DateOnly(2026, 11, 9), Arzt = "Dr. Linde (ausgedacht)", Teilnehmer = "EF", Kuerzel = "EF", Begonnen = Jetzt };
        v.Eintrag(B("Ahorn")).Besprochen = "Schläft besser.";
        v.Eintrag(B("Ahorn")).Anordnungen.Add(new EntwurfAnordnung { Art = "dosis", MedikamentId = s.Medikamente().First(m => m.Wirkstoff == "Quetiapin").Id, Medikament = "Quetiapin" });
        v.Eintrag(B("Birke")).FacharztNoetig = true;
        v.Eintrag(B("Birke")).Facharzt = "Kardiologie";
        v.Eintrag(B("Birke")).Besprochen = "Herzklopfen.";
        v.Eintrag(B("Clematis")).Erledigt = true;   // besprochen, aber nichts eingetragen
        s.VisitenEntwurfSpeichern(v);
        Assert.Equal(2, VisitenEntwurfRegeln.Vorgemerkt(v));

        var gelesen = s.VisitenEntwurf("R1")!;   // „auch nach einem Absturz“: steht in der Datenbank
        Assert.Equal(3, gelesen.Eintraege.Count);
        Assert.Equal("Schläft besser.", gelesen.Eintrag(B("Ahorn")).Besprochen);
        Assert.True(gelesen.Eintrag(B("Clematis")).Erledigt);
        Assert.Null(s.VisitenEntwurf("R2"));
        gelesen.Eintrag(B("Ahorn")).Besprochen = "Schläft besser, Dosis angepasst.";
        s.VisitenEntwurfSpeichern(gelesen);   // überschreibt den Entwurf (kein zweiter)
        Assert.Equal("Schläft besser, Dosis angepasst.", s.VisitenEntwurf("R1")!.Eintrag(B("Ahorn")).Besprochen);

        // 0.33.2: Abschließen braucht das Recht „Visiten“ (nur Fachkräfte) – geprüft wird, wer abschließt.
        Assert.Equal([RechteRegeln.NurFachkraftText], Assert.Throws<Eingabefehler>(() => s.VisiteAbschliessen(s.VisitenEntwurf("R1")!, "KL", Jetzt)).Fehler);
        Assert.Equal([RechteRegeln.NichtAngemeldetText], Assert.Throws<Eingabefehler>(() => s.VisiteAbschliessen(s.VisitenEntwurf("R1")!, "", Jetzt)).Fehler);
        Assert.NotNull(s.VisitenEntwurf("R1"));   // Entwurf bleibt
        var (visite, aufgaben) = s.VisiteAbschliessen(s.VisitenEntwurf("R1")!, "EF", Jetzt);
        Assert.Equal(2, visite.Eintraege.Count);   // Clematis ohne Inhalt zählt nicht
        Assert.Equal("", visite.Art);
        Assert.Equal(2, aufgaben.Count);
        Assert.Null(s.VisitenEntwurf("R1"));
        Assert.Contains(s.Berichte(B("Ahorn")), b => b.Text.StartsWith("Visite 09.11.2026 (Dr. Linde (ausgedacht)), dabei: EF: Schläft besser"));
    }
}
