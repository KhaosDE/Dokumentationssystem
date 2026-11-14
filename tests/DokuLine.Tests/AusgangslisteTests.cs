using DokuLine.Kern;
using DokuLine.Modul.Ausgangsliste;
using Microsoft.Data.Sqlite;
using Verlauf = DokuLine.Modul.Ausgangsliste.Verlauf;

namespace DokuLine.Tests;

// Modul Ausgangsliste: Modulverwaltung (anmelden, ein/aus, Demo), Platz aus Akte und Modul,
// Rechtsregel, Fristen, Ausgangstext, Verlauf, Küchendienst, Digitaler Ausgang.
[Collection("Datenordner")]
public sealed class AusgangslisteTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);   // Samstag
    private readonly Datenordner daten = new();
    private readonly Speicher s;
    private readonly Datenbank db;

    public AusgangslisteTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.ModulAnmelden(new AusgangslisteDaten());
        s.DemoZuruecksetzen();
        db = new Datenbank(s);
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname).Id!.Value;
    private Platz P(string nachname) => db.Platz(B(nachname))!;

    [Fact]
    public void Modul_anmelden_ein_und_ausschalten()
    {
        Assert.Equal(new AusgangslisteDaten().Schritte.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), s.Meta("modul_ausgangsliste_stand"));
        Assert.True(s.ModulAn("ausgangsliste"));
        Assert.True(s.ModulAn(Kuechendienst.Schluessel));
        int gemeldet = 0;
        s.ModuleGeaendert += () => gemeldet++;
        s.ModulSetzen("ausgangsliste", false);
        Assert.False(s.ModulAn("ausgangsliste"));
        Assert.Equal(1, gemeldet);
        // Ausgeschaltet bleiben die Daten erhalten.
        Assert.Equal("2 Allein / 1 MBW", P("Ahorn").Ausgang);
        // Zweites Anmelden ändert nichts; die Demo schaltet wieder ein.
        s.ModulAnmelden(new AusgangslisteDaten());
        Assert.Single(s.Module);
        s.DemoZuruecksetzen();
        Assert.True(s.ModulAn("ausgangsliste"));
    }

    [Fact]
    public void Nach_dem_Update_kommt_die_Demo_des_Moduls_dazu()
    {
        using var t = new Speicher(Path.Combine(daten.Ordner, "update.db")) { Uhr = () => Jetzt };
        t.DemoBeimErstenStart();   // ohne Modul
        t.ModulAnmelden(new AusgangslisteDaten());
        Assert.True(t.DemoBeimErstenStart());
        Assert.Equal(69, new Datenbank(t).Laden().Count);
        Assert.Contains(new Datenbank(t).Laden(), p => p.Status == AnwesenheitsStatus.Abwesenheit);
        Assert.False(t.DemoBeimErstenStart());
    }

    [Fact]
    public void Platz_aus_Akte_und_Modul()
    {
        Assert.Equal(Unterbringung.Beschluss, P("Ahorn").Unterbringung);
        Assert.Equal(new DateTime(2027, 6, 30), P("Ahorn").UnterbringungDatum);
        Assert.Equal(Unterbringung.OffenGefuehrt, P("Birke").Unterbringung);
        Assert.Equal(Unterbringung.NichtErfasst, P("Distel").Unterbringung);
        Assert.Equal(Unterbringung.Forensik, P("Fichte").Unterbringung);
        Assert.Equal(["101", "101", "102"], db.Laden().Take(3).Select(p => p.Zimmernummer));   // 101 ist ein Doppelzimmer (Schritt 26e)
        Assert.Equal("Heimfahrt bis 09.11.2026", P("Holunder").StatusGrund);
        Assert.Equal("noch 2 Tage", KartenText.Frist(P("Holunder").StatusGrund, Jetzt, out _));
        Assert.Equal("seit 3 Tagen", KartenText.Frist(P("Kiefer").StatusGrund, Jetzt, out _));
        Assert.Equal(3, P("Ahorn").Stunden);
        Assert.Equal("2 Std allein · 1 Std mit Bewohner", KartenText.Aufteilung(P("Ahorn")));
        Assert.Equal("Befindlichkeit abfragen", KartenText.Info(P("Clematis")));
        Assert.Equal(["nur mit Personal"], KartenText.Hinweise(P("Eiche")));
        Assert.Equal("", P("Holunder").StundenText);   // bei Status leer
    }

    [Fact]
    public void Fristen_der_Unterbringung()
    {
        Assert.Equal(FristStufe.Bald, P("Immergrün").Frist(Jetzt));   // Einverständnis vor 170 Tagen
        Assert.StartsWith("Einverständnis endet in", P("Immergrün").FristText(Jetzt));
        Assert.Equal(FristStufe.Gueltig, P("Birke").Frist(Jetzt));
        Assert.Equal(FristStufe.Keine, P("Fichte").Frist(Jetzt));
        Assert.Equal(FristStufe.Fehlt, P("Distel").Frist(Jetzt));
        Assert.Equal("Unterbringung nicht erfasst", P("Distel").FristText(Jetzt));
        var abgelaufen = new Platz { Unterbringung = Unterbringung.Beschluss, UnterbringungDatum = Jetzt.Date.AddDays(-3) };
        Assert.Equal("Beschluss abgelaufen seit 3 Tagen", abgelaufen.FristText(Jetzt));
        Assert.Equal("Einverständnis: Datum fehlt", new Platz { Unterbringung = Unterbringung.OffenGefuehrt }.FristText(Jetzt));
    }

    [Fact]
    public void Rechtsregel_Ausgangsbeschraenkung_nur_mit_Beschluss_oder_Forensik()
    {
        var offen = Assert.Throws<Eingabefehler>(() => db.StatusSetzen(B("Birke"), AnwesenheitsStatus.Ausgangsbeschraenkung, "Ausgangsbeschränkung bis 20.11.2026", "AB"));
        Assert.Contains(Platz.HinweisNurMitBeschluss, offen.Fehler);
        var unklar = Assert.Throws<Eingabefehler>(() => db.StatusSetzen(B("Distel"), AnwesenheitsStatus.Ausgangsbeschraenkung, "Ausgangsbeschränkung bis 20.11.2026", "AB"));
        Assert.Contains(Platz.HinweisNichtErfasst, unklar.Fehler);
        db.StatusSetzen(B("Fichte"), AnwesenheitsStatus.Ausgangsbeschraenkung, "Ausgangsbeschränkung bis 20.11.2026", "AB");
        Assert.Equal("bis 20.11.2026", KartenText.GrundOhneTitel(P("Fichte")));
        Assert.Contains("Bitte zuerst einen Grund auswählen oder eintragen.",
            Assert.Throws<Eingabefehler>(() => db.StatusSetzen(B("Ahorn"), AnwesenheitsStatus.Abwesenheit, " ", "AB")).Fehler);
        Assert.Contains("Dieser Grund gehört zur Ausgangsbeschränkung, nicht zur Abwesenheit.",
            Assert.Throws<Eingabefehler>(() => db.StatusSetzen(B("Ahorn"), AnwesenheitsStatus.Abwesenheit, "Neuaufnahme seit (07.11.2026)", "AB")).Fehler);

        // Wechsel zu offen geführt hebt die laufende Beschränkung auf.
        Assert.Equal(AnwesenheitsStatus.Ausgangsbeschraenkung, P("Eiche").Status);
        var eiche = db.UnterbringungSetzen(B("Eiche"), Unterbringung.OffenGefuehrt, new DateOnly(2026, 11, 7), "CD");
        Assert.Equal(AnwesenheitsStatus.Keiner, eiche.Status);
        Assert.Equal(BeschlussZustand.KeinBeschluss, s.Bewohner(B("Eiche"))!.Beschluss);   // steht in der Akte
        Assert.Contains(db.VerlaufLaden(B("Eiche")), v => v.Text.Contains("weil offen geführt"));
    }

    [Fact]
    public void Beschluss_aus_dem_Modul_steht_in_der_Akte()
    {
        var distel = db.UnterbringungSetzen(B("Distel"), Unterbringung.Beschluss, new DateOnly(2027, 5, 31), "AB");
        Assert.Equal(Unterbringung.Beschluss, distel.Unterbringung);
        var akte = s.Bewohner(B("Distel"))!;
        Assert.Equal(BeschlussZustand.Ende, akte.Beschluss);
        Assert.Equal(new DateOnly(2027, 5, 31), akte.Beschlussende);
        Assert.Contains("Beschlussende fehlt.", Assert.Throws<Eingabefehler>(() => db.UnterbringungSetzen(B("Distel"), Unterbringung.Beschluss, null, "AB")).Fehler);
        db.UnterbringungSetzen(B("Distel"), Unterbringung.Forensik, null, "AB");
        Assert.Equal(Unterbringung.Forensik, P("Distel").Unterbringung);
        Assert.Null(s.Bewohner(B("Distel"))!.Beschlussende);
    }

    [Fact]
    public void Ausgangstext_und_Verlauf()
    {
        string t = AusgangText.MarkerErhoehen("", "Allein");
        t = AusgangText.MarkerErhoehen(t, "Allein");
        t = AusgangText.MarkerErhoehen(t, "MBW");
        t = AusgangText.InfoSetzen(t, "Befindlichkeit / abfragen");
        Assert.Equal("2 Allein / 1 MBW / (Befindlichkeit, abfragen)", t);
        t = AusgangText.MarkerVerringern(t, "MBW");
        Assert.Equal("2 Allein / (Befindlichkeit, abfragen)", t);
        Assert.Equal("2 Allein", AusgangText.InfoSetzen(t, ""));
        Assert.Equal("2 Allein / " + AusgangText.TextPersonalBegleitung, AusgangText.TextUmschalten("2 Allein", AusgangText.TextPersonalBegleitung));

        db.AusgangSetzen(B("Distel"), "2 MBW", "KL");
        db.StatusSetzen(B("Distel"), AnwesenheitsStatus.Abwesenheit, "Heimfahrt bis 10.11.2026", "KL");
        Assert.Equal("Heimfahrt bis 10.11.2026", P("Distel").AusgangAnzeige);
        db.StatusSetzen(B("Distel"), AnwesenheitsStatus.Keiner, "", "KL");
        Assert.Equal("2 MBW", P("Distel").AusgangAnzeige);   // alter Stand wieder da
        var zeilen = Verlauf.Zeilen(db.VerlaufLaden(B("Distel")));
        Assert.Equal(3, zeilen.Count);
        Assert.Contains("KL · Ausgang: „1 MBW“ → „2 MBW“", zeilen[2]);
        Assert.Contains("Status aufgehoben (war: Abwesenheit – Heimfahrt bis 10.11.2026)", zeilen[0]);

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE al_verlauf SET text = 'x'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Verlauf_und_Angaben_verschwinden_mit_dem_Bewohner()
    {
        using var befehl = s.Verbindung.CreateCommand();
        const string Zaehlen = "SELECT (SELECT COUNT(*) FROM al_plaetze) + (SELECT COUNT(*) FROM al_verlauf WHERE text LIKE '%2 Allein / 1 MBW%')";
        befehl.CommandText = Zaehlen;
        long vorher = (long)befehl.ExecuteScalar()!;
        befehl.CommandText = $"DELETE FROM bewohner WHERE id = {B("Ahorn")}";
        befehl.ExecuteNonQuery();
        befehl.CommandText = Zaehlen;
        Assert.Equal(vorher - 2, (long)befehl.ExecuteScalar()!);   // Ahorns Platz und sein Verlaufseintrag sind weg
    }

    [Fact]
    public void Kuechendienst_reihum_mit_Gruppen_Abwesenheit_und_Tausch()
    {
        var plan = db.KuechendienstLaden();
        var heute = Jetzt.Date;
        Assert.Equal(heute.AddDays(-5), plan[0].Datum);
        Assert.Equal(new DateTime(2026, 12, 31), plan[^1].Datum);   // bis Ende des Folgemonats
        // Holunder und Kiefer sind abwesend, teilen aber Doppelzimmer mit Anwesenden (101, 103; Schritt 26e):
        // alle Zimmer sind dabei, die Gruppe 201/202 ist ein Eintrag.
        var zimmer = plan.Select(t => t.Zimmer).Distinct().ToList();
        Assert.Contains("201/202", zimmer);
        Assert.DoesNotContain("201", zimmer);
        Assert.Contains("101", zimmer);
        Assert.Equal(42, zimmer.Count);   // seit Schritt 26h alle belegten Zimmer aller drei Wohnbereiche (siehe STAND, Offene Punkte)
        // Reihum ohne Lücke.
        for (int i = 1; i < 20; i++) Assert.NotEqual(plan[i - 1].Zimmer, plan[i].Zimmer);

        var tausch = plan.Single(t => t.Tausch != "");
        Assert.Equal(heute.AddDays(2), tausch.Datum);
        Assert.Equal("103", tausch.Wirksam);
        Assert.Equal("Clara Clematis", Kuechendienst.AnzeigeFuer(plan, tausch.Datum, db.Laden()));

        // Neu planen: Vergangenes und heute bleiben, der Tausch bleibt.
        string heuteVorher = plan.Single(t => t.Datum == heute).Zimmer;
        Kuechendienst.Planen(db, db.Laden(), heute);
        var neu = db.KuechendienstLaden();
        Assert.Equal(heuteVorher, neu.Single(t => t.Datum == heute).Zimmer);
        Assert.Equal("103", neu.Single(t => t.Datum == heute.AddDays(2)).Tausch);
        Assert.False(Kuechendienst.TauschMoeglich(heute, heute));
        Assert.True(Kuechendienst.TauschMoeglich(heute.AddDays(1), heute));

        // Karenz: Wer erst seit weniger als 4 Tagen da ist, macht noch keinen Dienst.
        var neuer = new Platz { Zimmernummer = "999", Aufnahmedatum = heute.AddDays(-2) };
        Assert.True(Kuechendienst.InKarenz(neuer, heute));
        Assert.False(Kuechendienst.ZimmerKannDienst("999", [neuer], heute));
    }

    // Bauplan 45: der Küchendienst des Tages für den Tagesplan – getauscht, abwesend, unbesetzt, bestätigen.
    [Fact]
    public void Kuechendienst_im_Tagesplan_getauscht_unbesetzt_und_bestaetigt()
    {
        var plan = db.KuechendienstLaden();
        var heute = Jetzt.Date;
        var h = Kuechendienst.Heute(plan, heute, db.Laden())!;
        Assert.False(h.Getauscht);
        Assert.StartsWith($"Küchendienst heute: {string.Join(" + ", h.Namen)} (Zi. {h.Zimmer})", Kuechendienst.ZeileText(h));
        var getauscht = Kuechendienst.Heute(plan, heute.AddDays(2), db.Laden())!;
        Assert.True(getauscht.Getauscht);
        Assert.Contains($"getauscht mit Zi. {getauscht.Geplant}", Kuechendienst.ZeileText(getauscht));
        Assert.Equal("Kein Küchendienst heute.", Kuechendienst.ZeileText(Kuechendienst.Heute(plan, heute.AddYears(1), db.Laden())));

        // Alle vom Dienst abwesend: unbesetzt.
        var zimmer = h.Zimmer.Split('/');
        var abwesend = db.Laden().Where(p => zimmer.Contains(p.Zimmernummer))
            .Select(p => new Platz { Zimmernummer = p.Zimmernummer, Bewohnername = p.Bewohnername, Status = AnwesenheitsStatus.Abwesenheit }).ToList();
        var leer = Kuechendienst.Heute(plan, heute, [.. db.Laden().Where(p => !zimmer.Contains(p.Zimmernummer)), .. abwesend])!;
        Assert.True(leer.Unbesetzt);
        Assert.Contains("niemand da", Kuechendienst.ZeileText(leer));
        Assert.Contains("abwesend: ", Kuechendienst.ZeileText(leer));

        Assert.NotNull(Kuechendienst.BestaetigenPruefen(h, heute.AddDays(1), Jetzt, KuechendienstStatus.Gemacht, ""));   // Zukunft
        Assert.NotNull(Kuechendienst.BestaetigenPruefen(h, heute, Jetzt, KuechendienstStatus.Vertreten, " "));
        Assert.Null(db.KuechendienstErledigtLaden(heute));
        db.KuechendienstErledigtSetzen(heute, KuechendienstStatus.NichtGemacht, "", "AB", Jetzt);
        db.KuechendienstErledigtSetzen(heute, KuechendienstStatus.Vertreten, "Zi. 105 (Test)", "CD", Jetzt.AddMinutes(10));
        var e = db.KuechendienstErledigtLaden(heute)!;
        Assert.Equal("vertreten durch Zi. 105 (Test) · CD 15:30", Kuechendienst.ErledigtText(e));
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE al_kuechendienst_erledigt SET kuerzel = 'XX'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Kuechendienst_Reihenfolge_und_Gruppen()
    {
        Kuechendienst.ReihenfolgeSpeichern(db, ["105", "101", "unbekannt"]);
        var reihe = Kuechendienst.Reihenfolge(db, db.Laden());
        Assert.Equal(["105", "101", "102", "103", "104", "106", "107"], reihe.Take(7));   // gespeicherte zuerst, dann nach Nummer
        Assert.DoesNotContain("unbekannt", reihe);
        var fest = Kuechendienst.FesteReihenfolge(reihe, Kuechendienst.Gruppen(db));
        Assert.Equal(reihe.Count - 1, fest.Count);   // 201 und 202 sind eine Gruppe
        Assert.Contains(fest, g => g.SequenceEqual(["201", "202"]));
    }

    [Fact]
    public void Digitaler_Ausgang_unterwegs_ueberfaellig_und_Nachtbericht()
    {
        var heute = db.AusgaengeLaden(Jetzt);
        Assert.Equal(5, heute.Count);
        var clematis = DigitalerAusgang.StandFuer(P("Clematis"), heute, Jetzt);
        Assert.NotNull(clematis.Laufend);
        Assert.False(clematis.Ueberfaellig);
        var ginster = DigitalerAusgang.StandFuer(P("Ginster"), heute, Jetzt);
        Assert.True(ginster.Ueberfaellig);
        Assert.Equal("Anton Ahorn hat den Ausgang wahrgenommen: 10:20–12:20 Uhr allein, 13:20–14:20 Uhr mit Bewohner.",
            DigitalerAusgang.Berichtstext(P("Ahorn"), heute.Where(e => e.BewohnerId == B("Ahorn"))));
        Assert.Equal(2, DigitalerAusgang.Nachtbericht(db.Laden(), heute).Split(Environment.NewLine).Length);

        // Beginnen und beenden, mit Regeln und Verlauf.
        Assert.Contains("Anton Ahorn ist schon unterwegs.",
            Assert.Throws<Eingabefehler>(() => { db.AusgangBeginnen(B("Ahorn"), "allein", "AB"); db.AusgangBeginnen(B("Ahorn"), "allein", "AB"); }).Fehler);
        Assert.Contains("Für Berta Birke ist kein Ausgang „allein“ eingetragen.",
            Assert.Throws<Eingabefehler>(() => db.AusgangBeginnen(B("Birke"), "allein", "AB")).Fehler);
        Assert.Throws<Eingabefehler>(() => db.AusgangBeginnen(B("Holunder"), "allein", "AB"));   // abwesend
        var laufend = db.AusgaengeLaden(Jetzt).Single(e => e.BewohnerId == B("Ahorn") && e.Unterwegs);
        db.AusgangBeenden(laufend.Id, "cd");
        Assert.Contains(db.VerlaufLaden(B("Ahorn")), v => v.Text == "Ausgang beendet: 15:20–15:20 Uhr allein (Hz. AB/CD)");
        Assert.Throws<Eingabefehler>(() => db.AusgangBeenden(laufend.Id, "CD"));

        db.AusgaengeLoeschenVor(Jetzt.Date.AddDays(1));
        Assert.Empty(db.AusgaengeLaden(Jetzt));
    }

    [Fact]
    public void Widerspruch_zwischen_Akte_und_Modul_ist_nur_ein_Hinweis()
    {
        // Immergrün: Akte „nicht offen geführt“, Modul „offen geführt“.
        Assert.True(P("Immergrün").Widerspruch);
        Assert.Contains(AusgangsMarken.Fuer(P("Immergrün"), Jetzt, null), m => m.Text == "Unterbringung widersprüchlich – bitte klären");
        Assert.Contains(AusgangsMarken.Fuer(P("Immergrün"), Jetzt, null, alle: true), m => m.Text.StartsWith("Widerspruch: Akte „nicht offen geführt“, Ausgangsliste „offen geführt“"));
        Assert.False(P("Birke").Widerspruch);   // beide „offen geführt“
        Assert.False(P("Ahorn").Widerspruch);   // Beschluss, „nein“
        Assert.False(P("Distel").Widerspruch);  // nicht erfasst
        Assert.Equal(OffenGefuehrtZustand.Nein, s.Bewohner(B("Immergrün"))!.OffenGefuehrt);   // nichts automatisch geändert
        var ahorn = s.Bewohner(B("Ahorn"))!;
        s.BewohnerSpeichern(ahorn with { OffenGefuehrt = OffenGefuehrtZustand.Ja });
        Assert.True(P("Ahorn").Widerspruch);   // Akte „ja“, aber Beschluss
    }

    [Fact]
    public void Platzhalter_in_den_Demo_Texten()
    {
        Assert.Equal("bis 12.11.2026 und seit (04.11.2026)", AusgangslisteDaten.Einsetzen("bis {+5} und seit ({-3})", new DateOnly(2026, 11, 7)));
    }
}
