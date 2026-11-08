using System.Text;
using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Bauplan Schritt 28: Betreuer in der Akte, E-Mail über das E-Mail-Programm (mailto:), Kalender-
// Austausch über .ics-Dateien. Das Programm selbst geht nicht ins Internet.
[Collection("Datenordner")]
public sealed class BetreuerIcsTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public BetreuerIcsTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Bewohner B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname);

    [Fact]
    public void Betreuer_Pruefung_Fassungen_und_mailto()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => BetreuerRegeln.AusEingabe(new BetreuerEingabe { Email = "kein at" }, Jetzt)).Fehler;
        Assert.Contains("Name des Betreuers fehlt.", fehler);
        Assert.Contains(fehler, f => f.StartsWith("E-Mail-Adresse bitte so"));
        Assert.False(BetreuerRegeln.EmailGueltig("a@b"));
        Assert.False(BetreuerRegeln.EmailGueltig("a@b.de?cc=x@y.de"));
        Assert.True(BetreuerRegeln.EmailGueltig("test.person@example.org"));

        var distel = B("Distel");
        Assert.Null(s.Betreuer(distel.Id!.Value));
        Assert.Equal("nicht erfasst", BetreuerRegeln.Text(null));
        s.BetreuerSpeichern(distel.Id!.Value, BetreuerRegeln.AusEingabe(new BetreuerEingabe { Name = "Test Taucher", Stelle = "Verein", Kuerzel = "AB" }, Jetzt));
        s.BetreuerSpeichern(distel.Id!.Value, BetreuerRegeln.AusEingabe(new BetreuerEingabe
            { Name = "Test Taucher", Stelle = "Verein", Email = "taucher@example.org", Telefon = "0000 1", Kuerzel = "CD" }, Jetzt.AddHours(1)));
        var b = s.Betreuer(distel.Id!.Value)!;
        Assert.Equal("Test Taucher (Verein) · Tel. 0000 1 · taucher@example.org", BetreuerRegeln.Text(b));
        Assert.Equal(2, s.BetreuerFassungen(distel.Id!.Value).Count);
        Assert.StartsWith("Mo 09.11.2026, 11:00 Uhr · CD:", BetreuerRegeln.Verlauf(s.BetreuerFassungen(distel.Id!.Value))[0]);
        Assert.Equal("mailto:taucher@example.org?subject=Betreuung%20Dieter%20Distel", BetreuerRegeln.Mailto(b, BetreuerRegeln.Betreff(distel)));
        Assert.Throws<Eingabefehler>(() => BetreuerRegeln.Mailto(b with { Email = "" }, "x"));

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE betreuer SET name = 'anders'";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Demo_Betreuer_nur_mit_example_org()
    {
        var alle = s.AlleBewohner().Select(b => s.Betreuer(b.Id!.Value)).OfType<Betreuer>().ToList();
        Assert.True(alle.Count >= 5);
        Assert.All(alle.Where(b => b.Email.Length > 0), b => Assert.EndsWith(BetreuerRegeln.DemoEndung, b.Email));
        Assert.Contains(alle, b => b.Email.Length == 0);   // auch ohne E-Mail
        Assert.Contains(s.AlleBewohner(), x => s.Betreuer(x.Id!.Value) is null);   // und „nicht erfasst“
        string datei = File.ReadAllText(Path.Combine(Repo.Wurzel, "daten", "demo", "betreuer.json"));
        Assert.DoesNotMatch(@"@(?!example\.org)", datei);
    }

    [Fact]
    public void Ics_schreiben_und_wieder_lesen()
    {
        var heute = DateOnly.FromDateTime(Jetzt);
        var namen = s.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
        var neu = s.KalenderAnlegen(KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Bewohner, Titel = "Zahnarzt; Kontrolle, kurz", BeginnDatum = "12.11.2026", BeginnUhrzeit = "09:00", EndeUhrzeit = "09:45",
            Ort = "Praxis am Markt (ausgedacht)", Hinweis = "Versichertenkarte mitnehmen\nNüchtern", Bewohner = [B("Ahorn").Id!.Value], Kuerzel = "AB",
        }, Jetzt));
        var fest = s.KalenderAnlegen(KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Wohnbereich, Titel = "Sommerfest mit einem sehr langen Titel, damit die Zeile gefaltet werden muss – äöü ÄÖÜ ß",
            BeginnDatum = "20.11.2026", EndeDatum = "21.11.2026", Ganztags = true, Kuerzel = "AB",
        }, Jetzt));
        string text = Ics.Schreiben([neu, fest], namen, Jetzt);
        Assert.StartsWith("BEGIN:VCALENDAR\r\nVERSION:2.0\r\n", text);
        Assert.All(text.Split("\r\n"), z => Assert.True(Encoding.UTF8.GetByteCount(z) <= 75, z));
        Assert.Contains("DTSTART:20261112T090000", text);
        Assert.Contains("DTSTART;VALUE=DATE:20261120", text);
        Assert.Contains("DTEND;VALUE=DATE:20261122", text);   // ganztags: Ende ist der Tag danach
        Assert.Contains(@"SUMMARY:Zahnarzt\; Kontrolle\, kurz", text);
        Assert.Contains("Bewohner: Anton Ahorn", text.Replace("\r\n ", ""));

        var gelesen = Ics.Lesen(text);
        Assert.Equal(2, gelesen.Count);
        Assert.Equal("Zahnarzt; Kontrolle, kurz", gelesen[0].Titel);
        Assert.Equal(new DateTime(2026, 11, 12, 9, 0, 0), gelesen[0].Beginn);
        Assert.Equal(new DateTime(2026, 11, 12, 9, 45, 0), gelesen[0].Ende);
        Assert.Contains("Versichertenkarte mitnehmen\nNüchtern", gelesen[0].Beschreibung);
        Assert.True(gelesen[1].Ganztags);
        Assert.StartsWith("Sommerfest mit einem sehr langen Titel", gelesen[1].Titel);
        Assert.EndsWith("äöü ÄÖÜ ß", gelesen[1].Titel);
        Assert.Equal("Fr 20.11. – Sa 21.11.", gelesen[1].ZeitText);

        // Wieder einlesen: als Termin im Kalender (Haus), Fassung wie ein neu angelegter Termin.
        var f = KalenderRegeln.AusEingabe(Ics.AlsEingabe(gelesen[1], KalenderArt.Wohnbereich, haus: true, [], "EF"), Jetzt);
        Assert.True(f.Haus && f.Ganztags);
        Assert.Equal(new DateOnly(2026, 11, 21), KalenderRegeln.LetzterTag(f));
        Assert.StartsWith("aus einer .ics-Datei", f.Hinweis);
        var t = KalenderRegeln.AusEingabe(Ics.AlsEingabe(gelesen[0], KalenderArt.Bewohner, haus: false, [B("Birke").Id!.Value], "EF"), Jetzt);
        Assert.Equal((new DateTime(2026, 11, 12, 9, 0, 0), new DateTime(2026, 11, 12, 9, 45, 0)), (t.Beginn, t.Ende));
        _ = heute;
    }

    [Fact]
    public void Ics_aus_anderen_Programmen()
    {
        // Wie eine gespeicherte Einladung: Zeitzone, UTC, Alarm, Wiederholung, gefaltete Zeilen, Dauer.
        string einladung = string.Join("\r\n",
            "BEGIN:VCALENDAR", "PRODID:-//Beispiel//DE", "VERSION:2.0",
            "BEGIN:VTIMEZONE", "TZID:W. Europe Standard Time", "BEGIN:STANDARD", "DTSTART:16010101T030000", "END:STANDARD", "END:VTIMEZONE",
            "BEGIN:VEVENT", "UID:1", "SUMMARY;LANGUAGE=de-DE:Hilfeplangespräch", "DTSTART;TZID=W. Europe Standard Time:20261116T140000",
            "DTEND;TZID=\"W. Europe Standard Time\":20261116T150000", "LOCATION:Raum 2", "DESCRIPTION:Bitte Unterlagen mit", " bringen\\, danke.",
            "BEGIN:VALARM", "TRIGGER:-PT15M", "DESCRIPTION:Erinnerung", "END:VALARM", "END:VEVENT",
            "BEGIN:VEVENT", "UID:2", "SUMMARY:Teamsitzung", "DTSTART:20261117T080000Z", "DURATION:PT1H30M", "RRULE:FREQ=WEEKLY", "END:VEVENT",
            "BEGIN:VEVENT", "UID:3", "SUMMARY:kaputt", "DTSTART:morgen", "END:VEVENT",
            "END:VCALENDAR");
        var t = Ics.Lesen(einladung);
        Assert.Equal(2, t.Count);   // der kaputte fällt weg
        Assert.Equal("Hilfeplangespräch", t[0].Titel);
        Assert.Equal(new DateTime(2026, 11, 16, 14, 0, 0), t[0].Beginn);
        Assert.Equal("Bitte Unterlagen mitbringen, danke.", t[0].Beschreibung);
        Assert.Equal("Raum 2", t[0].Ort);
        var utc = new DateTime(2026, 11, 17, 8, 0, 0, DateTimeKind.Utc).ToLocalTime();
        Assert.Equal(utc, t[1].Beginn);
        Assert.Equal(TimeSpan.FromMinutes(90), t[1].Ende - t[1].Beginn);
        Assert.True(t[1].Wiederholung);
        Assert.Contains("Wiederholung: nur der erste Termin", Ics.AlsEingabe(t[1], KalenderArt.Wohnbereich, false, [], "EF").Hinweis);
        Assert.Empty(Ics.Lesen("kein Kalender"));
    }
}
