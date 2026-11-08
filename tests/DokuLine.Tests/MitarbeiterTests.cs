using DokuLine.Kern;

namespace DokuLine.Tests;

// Mitarbeiter, Anmeldung und Rechte (Bauplan Schritt 19).
[Collection("Datenordner")]
public sealed class MitarbeiterTests : IDisposable
{
    private readonly Datenordner daten = new();
    private readonly Speicher s = new();

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    [Fact]
    public void Grundbestand_auch_ohne_Demo()
    {
        Assert.Equal(RechteRegeln.Positionen, s.Positionsliste().Select(p => p.Name));
        var rechte = s.Rechte();
        Assert.Equal(Funktionen.Alle.Length, rechte.Count);
        Assert.All(Funktionen.Alle, f => Assert.Contains("Hausleitung", rechte[f.Schluessel]));       // die Leitungen dürfen alles
        Assert.Equal(["Administrator", "Geschäftsführung", "Hausleitung", "Verwaltung", "Therapie"],
            RechteRegeln.Positionen.Where(rechte[Funktionen.TherapienBearbeiten].Contains));
        Assert.Equal(["Administrator", "Geschäftsführung", "Hausleitung", "Verwaltung", "Bereichsleitung"],
            RechteRegeln.Positionen.Where(rechte[Funktionen.MitarbeiterBereich].Contains));
        Assert.Contains("Psychologischer Fachdienst", rechte[Funktionen.MedikamenteVerwalten]);
        Assert.DoesNotContain("Mitarbeiter Wohnbereich", rechte[Funktionen.MedikamenteVerwalten]);
        Assert.Equal([Funktionen.AerztlicheBearbeiten, Funktionen.MedikamentAnlegen, Funktionen.MedikationBearbeiten, Funktionen.Visiten],
            s.NurFachkraft().Order());   // Visiten seit 0.33.0 (Assistenzkräfte machen keine Visiten)
        Assert.Equal(["Administrator", "Geschäftsführung", "Hausleitung", "Verwaltung", "Bereichsleitung", "Mitarbeiter Wohnbereich"],
            RechteRegeln.Positionen.Where(rechte[Funktionen.BedarfGeben].Contains));
        Assert.Empty(s.AlleMitarbeiter());
        Assert.Equal(RechteRegeln.NichtAngemeldetText, s.Grund("", Funktionen.BerichteSchreiben));
    }

    [Fact]
    public void Demo_Mitarbeiter_Bereiche_Positionen()
    {
        s.DemoBeimErstenStart();
        var alle = s.AlleMitarbeiter();
        Assert.Equal(28, alle.Count);   // mit TST (Schritt 26a2) und 14 weiteren für die Bezugspflege (26h)
        Assert.Equal(["R1", "R2", "R3", "Therapie", "Leitung", "Verwaltung"], s.Bereichsliste().Select(b => b.Name));
        Assert.All(RechteRegeln.Positionen, p => Assert.Contains(alle, m => m.Positionen.Contains(p)));   // je Position mindestens einer
        Assert.Contains(alle, m => m.Fachkraft);
        Assert.Contains(alle, m => !m.Fachkraft && m.Aktiv);
        Assert.False(s.MitarbeiterLesen("HI")!.Aktiv);
        Assert.Equal(["R1", "R2"], s.MitarbeiterLesen("PR")!.Bereiche.Order());
        Assert.Equal(["Bereichsleitung", "Mitarbeiter Wohnbereich"], s.MitarbeiterLesen("gh")!.Positionen);
        Assert.Equal(20, s.Positionsliste().Single(p => p.Name == "Mitarbeiter Wohnbereich").Zugeordnet);   // AB, CD, KL, GH, MN, HI und 14 aus Schritt 26h
    }

    [Fact]
    public void Rechte_nach_Position_und_Fachkraft()
    {
        s.DemoBeimErstenStart();
        Assert.True(s.Darf("CD", Funktionen.MedikationBearbeiten));                         // Fachkraft
        Assert.Equal(RechteRegeln.NurFachkraftText, s.Grund("KL", Funktionen.MedikationBearbeiten));   // keine Fachkraft
        Assert.True(s.Darf("KL", Funktionen.BewohnerBearbeiten));
        Assert.Equal(RechteRegeln.FehltText, s.Grund("CD", Funktionen.TherapienBearbeiten));
        Assert.True(s.Darf("OP", Funktionen.TherapienBearbeiten));
        Assert.Equal(RechteRegeln.NichtAngemeldetText, s.Grund("HI", Funktionen.BerichteSchreiben));   // ausgeschieden
        Assert.Equal(RechteRegeln.NichtAngemeldetText, s.Grund("XY", Funktionen.BerichteSchreiben));
        Assert.True(s.Darf("GH", Funktionen.MitarbeiterBereich));                           // Rechte mehrerer Positionen zusammen
        Assert.True(s.Darf("UT", Funktionen.MedikamenteVerwalten));                         // Geschäftsführung darf alles …
        Assert.Equal(RechteRegeln.NurFachkraftText, s.Grund("UT", Funktionen.MedikationBearbeiten));   // … außer was nur Fachkräfte dürfen

        s.RechtSetzen(Funktionen.TherapienBearbeiten, "Mitarbeiter Wohnbereich", true);
        Assert.True(s.Darf("CD", Funktionen.TherapienBearbeiten));
        s.NurFachkraftSetzen(Funktionen.MedikationBearbeiten, false);
        Assert.True(s.Darf("KL", Funktionen.MedikationBearbeiten));
        s.RechtSetzen(Funktionen.BerichteSchreiben, "Mitarbeiter Wohnbereich", false);
        Assert.False(s.Darf("CD", Funktionen.BerichteSchreiben));
    }

    [Fact]
    public void Anmelden_nur_aktive()
    {
        s.DemoBeimErstenStart();
        Assert.Equal("CD", s.Anmelden(" cd ").Kuerzel);
        Assert.Contains("nicht bekannt", Assert.Throws<Eingabefehler>(() => s.Anmelden("ZZ")).Message);
        Assert.Contains("ausgeschieden", Assert.Throws<Eingabefehler>(() => s.Anmelden("HI")).Message);
        Assert.Throws<Eingabefehler>(() => s.Anmelden(""));
    }

    [Fact]
    public void Mitarbeiter_anlegen_und_aendern()
    {
        s.DemoBeimErstenStart();
        var neu = new Mitarbeiter("QX", "Quirin", "Kranich", false, true, ["R2"], ["Mitarbeiter Wohnbereich"]);
        var fehler = Assert.Throws<Eingabefehler>(() => s.MitarbeiterSpeichern(neu with { Kuerzel = "ab" }, neu: true)).Fehler;
        Assert.Contains("Das Kürzel „ab“ ist schon vergeben.", fehler);
        fehler = Assert.Throws<Eingabefehler>(() => s.MitarbeiterSpeichern(neu with { Kuerzel = "Q X", Bereiche = [], Positionen = ["Kapitän"] }, neu: true)).Fehler;
        Assert.Contains("Kürzel: 1 bis 6 Buchstaben oder Ziffern.", fehler);
        Assert.Contains("Bitte mindestens einen Bereich wählen.", fehler);
        Assert.Contains("Eine der Positionen gibt es nicht.", fehler);
        s.MitarbeiterSpeichern(neu, neu: true);
        Assert.Equal(29, s.AlleMitarbeiter().Count);
        Assert.Contains(s.Kollegen(), k => k.Kuerzel == "QX" && k.Teams.Contains("R2"));   // auch für Nachrichten
        s.MitarbeiterSpeichern(neu with { Fachkraft = true, Aktiv = false, Bereiche = ["R1", "R2"] }, neu: false);
        var gelesen = s.MitarbeiterLesen("QX")!;
        Assert.True(gelesen.Fachkraft);
        Assert.False(gelesen.Aktiv);
        Assert.Equal(2, gelesen.Bereiche.Count);
        Assert.Throws<Eingabefehler>(() => s.MitarbeiterSpeichern(neu with { Kuerzel = "NEU" }, neu: false));   // Kürzel bleibt
    }

    [Fact]
    public void Bereiche_und_Positionen_pflegen()
    {
        s.DemoBeimErstenStart();
        Assert.Throws<Eingabefehler>(() => s.ListeneintragAnlegen(Liste.Bereiche, "r1"));
        s.ListeneintragUmbenennen(Liste.Bereiche, "R1", "Wohnbereich Nord");
        Assert.Contains("Wohnbereich Nord", s.MitarbeiterLesen("AB")!.Bereiche);
        Assert.All(s.AlleBewohner().Where(b => b.Nachname is "Ahorn" or "Birke"), b => Assert.Equal("Wohnbereich Nord", b.Wohnbereich));
        Assert.Contains("Wohnbereich Nord", s.Teams());
        Assert.Equal(3, s.UngeleseneNachrichten("AB"));   // Nachrichten an den Bereich gehen mit

        s.ListeneintragUmbenennen(Liste.Positionen, "Therapie", "Therapeutischer Dienst");
        Assert.Contains("Therapeutischer Dienst", s.Rechte()[Funktionen.TherapienBearbeiten]);   // Rechte gehen mit
        Assert.True(s.Darf("OP", Funktionen.TherapienBearbeiten));

        Assert.Contains("stilllegen", Assert.Throws<Eingabefehler>(() => s.ListeneintragLoeschen(Liste.Bereiche, "Verwaltung")).Message);
        s.ListeneintragAnlegen(Liste.Positionen, "Hauswirtschaft");
        s.ListeneintragAktivSetzen(Liste.Positionen, "Hauswirtschaft", false);
        Assert.False(s.Positionsliste().Single(p => p.Name == "Hauswirtschaft").Aktiv);
        s.ListeneintragLoeschen(Liste.Positionen, "Hauswirtschaft");
        Assert.DoesNotContain(s.Positionsliste(), p => p.Name == "Hauswirtschaft");
    }

    [Fact]
    public void Mitarbeiter_verwalten_im_eigenen_Bereich()
    {
        s.DemoBeimErstenStart();
        var ef = s.MitarbeiterLesen("EF")!;
        Assert.True(RechteRegeln.DarfVerwalten(ef, alle: false, eigenerBereich: true, s.MitarbeiterLesen("PR")!.Bereiche));
        Assert.False(RechteRegeln.DarfVerwalten(ef, alle: false, eigenerBereich: true, s.MitarbeiterLesen("GH")!.Bereiche));
        Assert.True(RechteRegeln.DarfVerwalten(ef, alle: true, eigenerBereich: false, s.MitarbeiterLesen("GH")!.Bereiche));
        Assert.False(RechteRegeln.DarfVerwalten(ef, alle: false, eigenerBereich: false, ["R1"]));
    }

    [Fact]
    public void Demo_zuruecksetzen_stellt_Rechte_wieder_her()
    {
        s.DemoBeimErstenStart();
        s.RechtSetzen(Funktionen.BerichteSchreiben, "Mitarbeiter Wohnbereich", false);
        s.ListeneintragAnlegen(Liste.Bereiche, "R4");
        s.DemoZuruecksetzen();
        Assert.True(s.Darf("CD", Funktionen.BerichteSchreiben));
        Assert.DoesNotContain(s.Bereichsliste(), b => b.Name == "R4");
        Assert.Equal(28, s.AlleMitarbeiter().Count);
    }
}
