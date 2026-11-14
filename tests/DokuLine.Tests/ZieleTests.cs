using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 43: Ziele – Vorschlag „überprüfen bis“, Prüfung, drei Zustände (eingetragen · kein Ziel benennen · nicht
// erfasst), Leiste, Marken, Fassungen statt Überschreiben, Rechte, Bericht zu einem Ziel mit Filter, Demo, Einstellungen.
[Collection("Datenordner")]
public sealed class ZieleTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 9, 9, 0, 0);
    private static readonly DateOnly Heute = DateOnly.FromDateTime(Jetzt);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public ZieleTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private static ZielFassung F(string text = "Ich gehe allein einkaufen (Test).", DateOnly? bis = null) => new()
    {
        Wortlaut = text, Lebensbereich = 2, VereinbartAm = Heute.AddDays(-10), MitWem = "EF", UeberpruefenBis = bis ?? Heute.AddMonths(3),
        Kuerzel = "EF", Erfasst = Jetzt.AddDays(-10),
    };

    private static Ziel Z(long id, ZielArt art, ZielFassung f) => new(id, 1, art, [f]);

    [Fact]
    public void Regeln_Zustand_Leiste_und_Marken()
    {
        Assert.Equal(new DateOnly(2027, 2, 9), ZielRegeln.UeberpruefenVorschlag(ZielArt.Nahziel, Heute));
        Assert.Equal(new DateOnly(2027, 11, 9), ZielRegeln.UeberpruefenVorschlag(ZielArt.Leitziel, Heute));
        Assert.Equal(4, ZielRegeln.Pruefen(F("") with { Lebensbereich = 0, MitWem = " ", UeberpruefenBis = Heute.AddDays(-30) }).Count);
        Assert.Single(ZielRegeln.Pruefen(F() with { Stand = ZielStand.Erreicht }));   // ohne Satz

        Assert.Equal(ZieleZustand.NichtErfasst, ZielRegeln.Zustand([], null));
        Assert.Equal("Ziele: nicht erfasst", ZielRegeln.Leiste([], null));
        var verzicht = new ZielVerzicht(1, Heute, "möchte sich erst eingewöhnen", "EF", Jetzt);
        Assert.Equal(ZieleZustand.Keine, ZielRegeln.Zustand([], verzicht));
        Assert.StartsWith(ZielRegeln.KeinZielText, ZielRegeln.Leiste([], verzicht));
        Assert.Empty(ZielRegeln.Marken([], verzicht, [], Heute));
        // Ein danach vereinbartes (auch schon wieder beendetes) Ziel hebt den Verzicht auf.
        var spaeter = Z(9, ZielArt.Nahziel, F() with { Erfasst = Jetzt.AddDays(1), Stand = ZielStand.Beendet, StandSatz = "x" });
        Assert.Equal(ZieleZustand.NichtErfasst, ZielRegeln.Zustand([spaeter], verzicht));

        var leit = Z(1, ZielArt.Leitziel, F("Ich möchte eine eigene Wohnung (Test)."));
        var nah1 = Z(2, ZielArt.Nahziel, F("Ich räume mein Zimmer auf (Test).", Heute));
        var nah2 = Z(3, ZielArt.Nahziel, F());
        Assert.Equal(ZieleZustand.Eingetragen, ZielRegeln.Zustand([nah1, leit], verzicht));
        Assert.Equal("Leitziel: Ich möchte eine eigene Wohnung (Test). · Nahziel 1: Ich räume mein Zimmer auf (Test).", ZielRegeln.Leiste([nah1, leit], null));
        var massnahme = new Massnahme(5, 1, [new MassnahmeFassung { Titel = "Zimmer (Test)", Lebensbereich = 2, GiltAb = Heute.AddDays(-5), ZielId = 2 }]);
        var marken = ZielRegeln.Marken([leit, nah1], null, [massnahme], Heute).Select(m => m.Text).ToList();
        Assert.Equal(["Ziel fehlt: 1 Nahziel", "Überprüfung der Ziele fällig"], marken);
        marken = ZielRegeln.Marken([leit, nah1, nah2], null, [massnahme], Heute.AddDays(-1)).Select(m => m.Text).ToList();
        Assert.Equal(["Nahziel ohne Maßnahme"], marken);
        Assert.Empty(ZielRegeln.Marken([leit, nah1], null, [massnahme], Heute.AddDays(-1), leitziele: 1, nahziele: 1));
        Assert.Equal([1L, 2L, 3L], ZielRegeln.Waehlbar([nah2, nah1, leit], null).Select(z => z.Id));
        Assert.Equal([1L, 9L], ZielRegeln.Waehlbar([leit, spaeter], 9).Select(z => z.Id));   // schon zugeordnet, inzwischen beendet

        static Bericht B(long? ziel) => new() { BewohnerId = 1, Zeitpunkt = Jetzt, Schicht = Schicht.Frueh, Verfasser = "AB", Text = "x", ZielId = ziel };
        var berichte = new[] { B(2), B(2), B(null), B(2) with { Art = MassnahmenRegeln.ArtBericht } };   // der Eintrag einer Bestätigung zählt nicht
        var bestaetigt = new MassnahmeBestaetigung(1, 5, 1, Heute, Schicht.Frueh, MassnahmeErgebnis.Durchgefuehrt, "", 20, "", "AB", Jetzt, 2, "Zimmer (Test)");
        var zusammen = ZielRegeln.Zusammenfassung([leit, nah1], berichte, [massnahme], [bestaetigt]);
        Assert.Equal("Nahziel: Ich räume mein Zimmer auf (Test). – 2 Berichte, 1 Maßnahme (1× durchgeführt)", zusammen[1]);
    }

    [Fact]
    public void Anlegen_Aendern_Verzicht_Rechte_und_Datenbank()
    {
        var b = s.AlleBewohner().First(x => s.Ziele(x.Id!.Value).Count == 0 && s.Verzicht(x.Id!.Value) is null   // in der Demo nicht erfasst
            && s.MassnahmenAnlegenGrund("KL", x.Id!.Value) is not null);
        long id = b.Id!.Value;
        Assert.Equal(ZieleZustand.NichtErfasst, ZielRegeln.Zustand(s.Ziele(id), s.Verzicht(id)));
        Assert.Throws<Eingabefehler>(() => s.ZielAnlegen(id, ZielArt.Leitziel, F(), "KL", Jetzt));   // Assistenzkraft ohne Bezugspflege
        Assert.Throws<Eingabefehler>(() => s.ZielAnlegen(id, ZielArt.Leitziel, F(""), "EF", Jetzt));
        Assert.Throws<Eingabefehler>(() => s.VerzichtSetzen(id, " ", "EF", Jetzt));

        s.VerzichtSetzen(id, "möchte erst ankommen", "EF", Jetzt.AddDays(-1));
        Assert.Equal(ZieleZustand.Keine, ZielRegeln.Zustand(s.Ziele(id), s.Verzicht(id)));
        Assert.Empty(s.ZielMarken(id, Heute));

        var leit = s.ZielAnlegen(id, ZielArt.Leitziel, F("Ich möchte wieder arbeiten (Test)."), "EF", Jetzt);
        Assert.Equal(ZieleZustand.Eingetragen, ZielRegeln.Zustand(s.Ziele(id), s.Verzicht(id)));
        Assert.Throws<Eingabefehler>(() => s.VerzichtSetzen(id, "x", "EF", Jetzt));   // es gibt ein aktives Ziel
        Assert.Contains(s.ZielMarken(id, Heute), m => m.Text == "Ziel fehlt: 2 Nahziele");

        s.ZielAendern(leit.Id, leit.Aktuell with { Wortlaut = "Ich möchte in der Werkstatt arbeiten (Test)." }, "AB", Jetzt.AddMinutes(5));
        s.ZielAendern(leit.Id, leit.Aktuell with { Stand = ZielStand.Erreicht, StandSatz = "Arbeitsplatz gefunden" }, "AB", Jetzt.AddMinutes(10));
        var ziel = s.Ziele(id).Single();
        Assert.Equal(3, ziel.Fassungen.Count);   // nichts überschrieben
        Assert.Equal("Ich möchte in der Werkstatt arbeiten (Test).", ziel.Fassungen[1].Wortlaut);
        Assert.Equal((ZielStand.Erreicht, (DateOnly?)Heute), (ziel.Aktuell.Stand, ziel.Aktuell.StandAm));
        Assert.Throws<Eingabefehler>(() => s.ZielAendern(leit.Id, leit.Aktuell, "AB", Jetzt));   // abgeschlossen

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE ziel_fassungen SET wortlaut = 'x'";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Bericht_zu_einem_Ziel_mit_Filter()
    {
        var b = s.AlleBewohner().First(x => ZielRegeln.Aktive(s.Ziele(x.Id!.Value)).Count >= 2);
        long id = b.Id!.Value;
        var ziel = ZielRegeln.Aktive(s.Ziele(id))[1];
        var eingabe = new BerichtEingabe
        {
            BewohnerId = id, Datum = Datum.Text(Heute), Uhrzeit = "08:30", Verfasser = "AB", Bereiche = [2], Text = "War allein einkaufen (Test).", ZielId = ziel.Id,
        };
        var bericht = s.BerichtSpeichern(BerichtRegeln.AusEingabe(eingabe, Jetzt));
        Assert.Equal(ziel.Id, bericht.ZielId);
        var gefiltert = s.Berichte(id, new DokuFilter { ZielId = ziel.Id, ZielText = "Nahziel", Zeitraum = Zeitraum.Frei }, Heute);
        Assert.Contains(gefiltert, x => x.Id == bericht.Id);
        Assert.All(gefiltert, x => Assert.Equal(ziel.Id, x.ZielId));
        Assert.Contains(DokuRegeln.Gesetzt(new DokuFilter { ZielId = ziel.Id, ZielText = "Nahziel" }), g => g.Text == "Ziel: Nahziel");

        eingabe.BewohnerId = s.AlleBewohner().First(x => x.Id != id).Id!.Value;
        Assert.Throws<Eingabefehler>(() => s.BerichtSpeichern(BerichtRegeln.AusEingabe(eingabe, Jetzt)));
    }

    [Fact]
    public void Demo_alle_drei_Zustaende_Massnahmen_an_Zielen_und_Einstellungen()
    {
        var zustaende = s.AlleBewohner().Select(b => ZielRegeln.Zustand(s.Ziele(b.Id!.Value), s.Verzicht(b.Id!.Value))).ToList();
        Assert.Contains(ZieleZustand.Eingetragen, zustaende);
        Assert.Contains(ZieleZustand.Keine, zustaende);
        Assert.Contains(ZieleZustand.NichtErfasst, zustaende);
        Assert.True(zustaende.Count(z => z == ZieleZustand.Eingetragen) > zustaende.Count / 2);
        var marken = s.AlleBewohner().SelectMany(b => s.ZielMarken(b.Id!.Value, Heute)).Select(m => m.Text).ToList();
        Assert.Contains(marken, m => m.StartsWith("Ziel fehlt", StringComparison.Ordinal));
        Assert.Contains("Überprüfung der Ziele fällig", marken);

        // Maßnahmen hängen an Zielen; ihre kurzen Einträge in der Dokumentation tragen das Ziel mit.
        var mitZiel = s.AlleBewohner().SelectMany(b => s.Massnahmen(b.Id!.Value)).Where(m => m.Fassungen[0].ZielId is not null).ToList();
        Assert.NotEmpty(mitZiel);
        Assert.All(mitZiel, m => Assert.Contains(s.Ziele(m.BewohnerId), z => z.Id == m.Fassungen[0].ZielId));
        Assert.Contains(mitZiel, m => s.Berichte(m.BewohnerId).Any(x => x.Art == MassnahmenRegeln.ArtBericht && x.ZielId == m.Fassungen[0].ZielId));

        Assert.Equal((1, 2), (Einstellungen.Laden().ZieleLeitziele, Einstellungen.Laden().ZieleNahziele));
        Einstellungen.Aendern(w => w with { ZieleNahziele = 3 });
        Assert.Equal(3, Einstellungen.Laden().ZieleNahziele);
    }
}
