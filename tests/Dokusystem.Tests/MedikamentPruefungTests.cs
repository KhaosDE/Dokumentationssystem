using Dokusystem.Kern;

namespace Dokusystem.Tests;

// Bauplan Schritt 21: eigene Prüfung neuer Medikamente (Quelle, Prüfdatum), sperren, Zulassungsnummer
// und das Nachschlagen beim BfArM (nur mit dem Medikamentennamen, nur feste Adressen).
[Collection("Datenordner")]
public sealed class MedikamentPruefungTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 8, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public MedikamentPruefungTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static MedikamentEingabe Eingabe(Action<MedikamentEingabe>? aendern = null)
    {
        var e = new MedikamentEingabe { Wirkstoff = "Beispielin", Darreichungsform = "Tablette", Staerken = "5 mg", Kuerzel = "CD" };
        aendern?.Invoke(e);
        return e;
    }

    [Fact]
    public void Bestaetigen_mit_Quelle_und_Pruefdatum()
    {
        var zukunft = Assert.Throws<Eingabefehler>(() => MedikamentRegeln.AusEingabe(Eingabe(e =>
        {
            e.Bestaetigt = true;
            e.Quelle = "https://example.org/fachinformation";
            e.GeprueftAm = "09.11.2026";
        }), Jetzt)).Fehler;
        Assert.Equal(["„Geprüft am“ liegt in der Zukunft."], zukunft);

        var m = s.MedikamentAnlegen(MedikamentRegeln.AusEingabe(Eingabe(e =>
        {
            e.Bestaetigt = true;
            e.Quelle = "https://example.org/fachinformation";
            e.GeprueftAm = "08.11.2026";
        }), Jetzt));
        Assert.True(m.Geprueft);
        Assert.Equal(new DateOnly(2026, 11, 8), s.Medikament(m.Id)!.Aktuell.GeprueftAm);
        Assert.Equal("geprüft am 08.11.2026 von CD", MedikamentRegeln.PruefText(m.Aktuell));
        Assert.Equal("Beispielin · Tablette", m.Auswahltext);   // geprüft: ohne Kennzeichen

        var ungeprueft = s.MedikamentAnlegen(MedikamentRegeln.AusEingabe(Eingabe(e => e.Wirkstoff = "Musterol"), Jetzt));
        Assert.Equal("ungeprüft", MedikamentRegeln.PruefText(ungeprueft.Aktuell));
        Assert.EndsWith(" – ungeprüft", ungeprueft.Auswahltext);
        Assert.Null(ungeprueft.Aktuell.GeprueftAm);
        Assert.Equal("", MedikamentRegeln.AlsEingabe(ungeprueft.Aktuell).GeprueftAm);
    }

    [Fact]
    public void Gesperrtes_ist_beim_Verordnen_nicht_waehlbar_und_bleibt_im_Verlauf()
    {
        var m = s.MedikamentAnlegen(MedikamentRegeln.AusEingabe(Eingabe(), Jetzt));
        Assert.Throws<Eingabefehler>(() => s.MedikamentSperren(m.Id, true, ""));   // ohne Anmeldung nicht
        var gesperrt = s.MedikamentSperren(m.Id, true, "PR");
        Assert.True(gesperrt.Gesperrt);
        Assert.Equal(2, gesperrt.Fassungen.Count);
        Assert.Equal("PR", gesperrt.Aktuell.Kuerzel);
        Assert.Equal(["gesperrt"], MedikamentRegeln.Unterschiede(gesperrt.Fassungen[0], gesperrt.Aktuell));

        var b = s.AlleBewohner()[0];
        Verordnung Neu() => MedikationRegeln.AusEingabe(new VerordnungEingabe
        {
            BewohnerId = b.Id!.Value, MedikamentId = m.Id, Staerke = "5 mg", Morgens = "1",
            Beginn = "08.11.2026", Arzt = "Dr. Test (ausgedacht)", AngeordnetAm = "08.11.2026", Kuerzel = "CD",
        }, m.Anzeige, Jetzt);
        var fehler = Assert.Throws<Eingabefehler>(() => s.VerordnungSpeichern(Neu())).Fehler;
        Assert.Contains("ist in der Medikamentendatenbank gesperrt", Assert.Single(fehler));

        var frei = s.MedikamentSperren(m.Id, false, "PR");
        Assert.False(frei.Gesperrt);
        Assert.Equal(["wieder freigegeben"], MedikamentRegeln.Unterschiede(frei.Fassungen[1], frei.Aktuell));
        s.VerordnungSpeichern(Neu());

        // Ändern behält „gesperrt“ (das Formular gibt den bisherigen Stand mit).
        var wieder = s.MedikamentSperren(m.Id, true, "PR");
        var geaendert = s.MedikamentAendern(m.Id, MedikamentRegeln.AusEingabe(Eingabe(e => { e.Staerken = "5 mg, 10 mg"; e.Gesperrt = wieder.Gesperrt; }), Jetzt));
        Assert.True(geaendert.Gesperrt);
    }

    [Fact]
    public void Zulassungsnummer_optional_gespeichert_und_suchbar()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => MedikamentRegeln.AusEingabe(Eingabe(e => e.Zulassungsnummer = "12345<script>"), Jetzt)).Fehler;
        Assert.Contains(fehler, f => f.StartsWith("Zulassungsnummer:"));
        Assert.Throws<Eingabefehler>(() => MedikamentRegeln.AusEingabe(Eingabe(e => e.Zulassungsnummer = new string('1', 31)), Jetzt));

        var ohne = s.MedikamentAnlegen(MedikamentRegeln.AusEingabe(Eingabe(e => e.Wirkstoff = "Ohnenummerin"), Jetzt));
        Assert.Equal("", ohne.Aktuell.Zulassungsnummer);

        var m = s.MedikamentAnlegen(MedikamentRegeln.AusEingabe(Eingabe(e => e.Zulassungsnummer = " 12345.00.00 "), Jetzt));
        Assert.Equal("12345.00.00", s.Medikament(m.Id)!.Aktuell.Zulassungsnummer);
        Assert.Equal(m.Id, Assert.Single(s.Medikamente("12345.00")).Id);
        var geaendert = s.MedikamentAendern(m.Id, MedikamentRegeln.AusEingabe(Eingabe(e => e.Zulassungsnummer = "12345.01.00"), Jetzt));
        Assert.Equal(["Zulassungsnummer: „12345.00.00“ → „12345.01.00“"], MedikamentRegeln.Unterschiede(geaendert.Fassungen[0], geaendert.Aktuell));
    }

    [Fact]
    public void Nachschlagen_nur_mit_dem_Namen()
    {
        Assert.Equal("Quetiapin", Nachschlagen.Suchbegriff("  Quetiapin  "));
        Assert.Equal("Quetiapin Seroquel", Nachschlagen.Suchbegriff("Quetiapin (Seroquel)"));
        Assert.Equal("L-Thyroxin 50 x 1", Nachschlagen.Suchbegriff("L-Thyroxin 50?&x=1"));   // keine Zeichen für Adressen
        Assert.Equal(Nachschlagen.SuchbegriffLaenge, Nachschlagen.Suchbegriff(new string('a', 100)).Length);

        Assert.Equal(new Uri(ExterneAdressen.Arzneimittelsuche), Nachschlagen.Start);
        Assert.True(Nachschlagen.Zulaessig(Nachschlagen.Start));   // die Startadresse selbst ist zulässig
    }

    [Theory]
    [InlineData("https://portal.bfarm.de/amguifree/?accessid=x", true)]
    [InlineData("https://auth.bfarm.de/auth/realms/anon-wirk/protocol/saml", true)]
    [InlineData("https://www.pharmnet-bund.de/", true)]
    [InlineData("https://www.gelbe-liste.de/wirkstoffe/Quetiapin", false)]   // nur das BfArM (Entscheidung 08.11.2026)
    [InlineData("https://BFARM.DE/", true)]
    [InlineData("http://portal.bfarm.de/", false)]          // nur https
    [InlineData("https://bfarm.de.example.org/", false)]
    [InlineData("https://evil-bfarm.de/", false)]
    [InlineData("https://www.example.org/", false)]
    [InlineData("file:///C:/Windows/", false)]
    public void Ansicht_zeigt_nur_die_festen_Bereiche(string adresse, bool erlaubt) =>
        Assert.Equal(erlaubt, Nachschlagen.Zulaessig(new Uri(adresse)));

    [Fact]
    public void Markierter_Text_wird_eine_Zeile()
    {
        Assert.Equal("25 mg 100 mg", Nachschlagen.Bereinigen("  25 mg\r\n\t100 mg "));
        Assert.Equal(300, Nachschlagen.Bereinigen(new string('x', 500)).Length);
        Assert.False(Nachschlagen.Zulaessig(null));
    }
}
