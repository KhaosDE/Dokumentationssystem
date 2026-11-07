using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Tests;

// Monatsreflexion: Prüfregeln, Fassungen mit Status, Therapien im Monat, Stilprüfung, Demo.
[Collection("Datenordner")]
public sealed class ReflexionTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 7, 15, 20, 0);
    private static readonly DateOnly Oktober = new(2026, 10, 1);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public ReflexionTests()
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

    private static List<string> Texte(string erster = "") => [erster, "", "", "", ""];

    [Fact]
    public void Pruefregeln()
    {
        var fehler = Assert.Throws<Eingabefehler>(() => ReflexionRegeln.AusEingabe(1, Oktober, Texte(), true, "", Jetzt)).Fehler;
        Assert.Contains("Kürzel fehlt (oben rechts „Angemeldet als …“).", fehler);
        Assert.Contains("Für „fertig“ bitte mindestens einen Lebensbereich ausfüllen.", fehler);
        Assert.Contains("Für einen kommenden Monat gibt es noch keine Reflexion.",
            Assert.Throws<Eingabefehler>(() => ReflexionRegeln.AusEingabe(1, new DateOnly(2026, 12, 1), Texte("x"), false, "AB", Jetzt)).Fehler);
        Assert.Throws<Eingabefehler>(() => ReflexionRegeln.AusEingabe(1, Oktober, ["a"], false, "AB", Jetzt));
        var f = ReflexionRegeln.AusEingabe(1, new DateOnly(2026, 10, 17), Texte("  Text  "), false, " AB ", Jetzt);
        Assert.Equal(Oktober, f.Monat);
        Assert.Equal("Text", f.Texte[0]);
        Assert.Equal("Oktober 2026", ReflexionRegeln.MonatText(Oktober));
        Assert.Equal(Oktober, ReflexionRegeln.Vorschlag(DateOnly.FromDateTime(Jetzt)));
    }

    [Fact]
    public void Fassungen_Status_und_Verlauf()
    {
        long id = B("Ginster");
        var r = s.Reflexion(id, Oktober);
        Assert.Equal("noch nicht begonnen", ReflexionRegeln.StatusText(r));
        s.ReflexionSpeichern(ReflexionRegeln.AusEingabe(id, Oktober, Texte(), false, "AB", Jetzt));   // leer: keine Fassung
        Assert.Empty(s.Reflexion(id, Oktober).Fassungen);
        s.ReflexionSpeichern(ReflexionRegeln.AusEingabe(id, Oktober, Texte("Er telefoniert wöchentlich mit seiner Schwester."), false, "AB", Jetzt));
        s.ReflexionSpeichern(ReflexionRegeln.AusEingabe(id, Oktober, Texte("Er telefoniert wöchentlich mit seiner Schwester."), false, "CD", Jetzt));   // gleich
        r = s.ReflexionSpeichern(ReflexionRegeln.AusEingabe(id, Oktober, Texte("Er telefoniert wöchentlich mit seiner Schwester."), true, "CD", Jetzt));
        Assert.Equal(2, r.Fassungen.Count);
        Assert.Equal("fertig", ReflexionRegeln.StatusText(r));
        r = s.ReflexionSpeichern(ReflexionRegeln.AusEingabe(id, Oktober, Texte("Er telefoniert wöchentlich mit seiner Schwester."), false, "EF", Jetzt));
        var verlauf = ReflexionRegeln.Verlauf(r);
        Assert.Contains("angelegt von AB · 1 von 5", verlauf[0]);
        Assert.Contains("als fertig markiert von CD", verlauf[1]);
        Assert.Contains("wieder Entwurf von EF", verlauf[2]);
        Assert.Contains(s.Reflexionen(id), x => x.Monat == Oktober);

        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = "UPDATE reflexionen SET fertig = 1";
        Assert.Throws<SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Therapien_im_Monat_laut_Wochenplan()
    {
        var termine = s.TermineImMonat(B("Ahorn"), new DateOnly(2026, 11, 1));
        Assert.All(termine, t => Assert.Equal(11, t.Tag.Month));
        var liste = ReflexionRegeln.Therapien(termine);
        Assert.NotEmpty(liste);
        var holz = Assert.Single(liste, t => t.Titel == "Holzwerkstatt");
        Assert.True(holz.Teilgenommen >= 1);
        Assert.StartsWith("Holzwerkstatt: ", ReflexionRegeln.TherapieText(holz));
        Assert.Equal("Malen: 4 Termine, 2 teilgenommen, 1 abgesagt, 1 ohne Eintrag",
            ReflexionRegeln.TherapieText(new TherapieImMonat("Malen", 4, 2, 1, 0, 1)));
    }

    [Fact]
    public void Stilpruefung_nur_Hinweise()
    {
        string text = "Er ist zuverlässig; er kommt pünktlich. Am 12.10. war er umfassend beteiligt. " +
                      "Zusammenfassend lässt sich sagen, dass es nicht leicht war, sondern mühsam. " +
                      "Die Mitarbeitenden und Bewohner*innen sprachen mit Hr. M darüber.";
        var hinweise = Stilpruefung.Pruefen(text).Select(h => h.Regel).ToList();
        Assert.Contains("Semikolon – lieber zwei Sätze", hinweise);
        Assert.Contains(hinweise, h => h.StartsWith("Konkretes Datum"));
        Assert.Contains("Wort, das im HEB nicht verwendet wird", hinweise);
        Assert.Contains("Floskel", hinweise);
        Assert.Contains("Konstruktion „nicht …, sondern …“", hinweise);
        Assert.Contains("„das Personal“ statt „Mitarbeitende“", hinweise);
        Assert.Contains("Gendersternchen – im HEB nicht verwendet", hinweise);
        Assert.Contains("Abgekürzter Nachname mit Punkt schreiben („Hr. M.“)", hinweise);
        Assert.Empty(Stilpruefung.Pruefen("Hr. M. geht spazieren. Fr. Meier liest."));
        Assert.Contains("Mehr als ein Gedankenstrich in einem Absatz", Stilpruefung.Pruefen("Er geht – meist allein – spazieren.").Select(h => h.Regel));
        Assert.Empty(Stilpruefung.Pruefen("Er geht – meist allein spazieren.\n\nEr liest – gern abends."));
        Assert.Empty(Stilpruefung.Pruefen("Herr Ahorn geht wöchentlich in die Holzwerkstatt und benötigt Aufforderung."));
        Assert.Contains("„umfassend“", Stilpruefung.Pruefen("Er war umfassend dabei.")[0].Text);
    }

    [Fact]
    public void Demo_Reflexionen_im_vergangenen_Monat()
    {
        var ahorn = s.Reflexion(B("Ahorn"), Oktober);
        Assert.True(ahorn.Fertig);
        Assert.Equal(2, ahorn.Fassungen.Count);
        Assert.All(ahorn.Aktuell!.Texte, t => Assert.Empty(Stilpruefung.Pruefen(t)));
        var clematis = s.Reflexion(B("Clematis"), Oktober);
        Assert.False(clematis.Fertig);
        Assert.NotEmpty(Stilpruefung.Pruefen(clematis.Text(1)));   // Hinweis in der Demo sichtbar
        Assert.Empty(s.Reflexion(B("Kiefer"), Oktober).Fassungen);
    }

    [Fact]
    public void Nach_dem_Update_kommt_die_Demo_Reflexion_dazu()
    {
        using (var befehl = s.Verbindung.CreateCommand())
        {
            befehl.CommandText = "DELETE FROM reflexionen; DELETE FROM meta WHERE schluessel = 'demo_reflexion'";
            befehl.ExecuteNonQuery();
        }
        Assert.True(s.DemoBeimErstenStart());
        Assert.True(s.Reflexion(B("Ahorn"), Oktober).Fertig);
        Assert.False(s.DemoBeimErstenStart());
    }
}
