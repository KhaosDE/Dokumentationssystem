using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 23: Tagesplan mit allen Terminen des Tages, Teilnahme „gegangen“ (derselbe
// Datensatz wie im Wochenplan), „noch offen“ nach Beginn und „Wo müsste er sein?“.
[Collection("Datenordner")]
public sealed class TagesplanTests : IDisposable
{
    // Montag: Holzwerkstatt (Mo 09:00–11:30) mit Anton Ahorn und Dieter Distel.
    private static readonly DateOnly Montag = new(2026, 11, 9);
    private DateTime jetzt = new(2026, 11, 9, 8, 30, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public TagesplanTests()
    {
        s = new Speicher { Uhr = () => jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private Bewohner B(string nachname) => s.AlleBewohner().First(b => b.Nachname == nachname);
    private TagesplanZeile Holz(string nachname) =>
        s.Tagesplan(Montag, [B(nachname)]).Single(z => z.Termin.Titel == "Holzwerkstatt");

    [Fact]
    public void Alle_Termine_des_Tages_nach_Uhrzeit_oder_Bewohner()
    {
        var zeilen = s.Tagesplan(Montag, s.AlleBewohner());
        Assert.NotEmpty(zeilen);
        Assert.All(zeilen, z => Assert.Equal(Montag, z.Termin.Tag));
        Assert.Contains(zeilen, z => z.Therapie && z.Termin.Titel == "Holzwerkstatt" && z.Bewohner.Nachname == "Ahorn");
        var nachZeit = TagesplanRegeln.NachUhrzeit(zeilen);
        Assert.Equal(nachZeit.Select(z => z.Termin.Ganztags ? TimeOnly.MinValue : z.Termin.Von).Order(), nachZeit.Select(z => z.Termin.Ganztags ? TimeOnly.MinValue : z.Termin.Von));
        var nachBewohner = TagesplanRegeln.NachBewohner(zeilen);
        Assert.Equal(nachBewohner.Select(z => z.Bewohner.Nachname).Order(StringComparer.CurrentCulture), nachBewohner.Select(z => z.Bewohner.Nachname));
        // Nur Therapien haben eine Teilnahme.
        Assert.All(zeilen.Where(z => !z.Therapie), z => Assert.Null(TagesplanRegeln.Marke(z, jetzt.AddHours(10))));
    }

    [Fact]
    public void Noch_offen_nach_Beginn_dann_gegangen_mit_Zeit_und_Kuerzel()
    {
        Assert.Null(TagesplanRegeln.Marke(Holz("Ahorn"), jetzt));                       // 08:30: noch nicht begonnen
        jetzt = new DateTime(2026, 11, 9, 9, 5, 0);
        Assert.Equal(TagesplanRegeln.NochOffen, TagesplanRegeln.Marke(Holz("Ahorn"), jetzt));
        Assert.Empty(s.InTherapie(jetzt));

        var z = Holz("Ahorn");
        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Teilgenommen, "CD", jetzt);
        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Teilgenommen, "CD", jetzt.AddMinutes(1));   // nicht doppelt
        Assert.Equal("gegangen 09:05 · CD", TagesplanRegeln.Marke(Holz("Ahorn"), jetzt));
        Assert.Equal(Teilnahme.Teilgenommen, Holz("Ahorn").Termin.Teilnahme);   // derselbe Datensatz wie im Wochenplan
        Assert.Single(s.TeilnahmeEintraege(Montag));
        Assert.Equal(Teilnahme.Teilgenommen, s.Woche(B("Ahorn").Id!.Value, Montag).Single(t => t.ZuteilungId == z.Termin.ZuteilungId && t.Tag == Montag).Teilnahme);

        // Wo müsste er sein? Bis zum Ende des Termins, danach wieder im Haus.
        var marke = Assert.Single(s.InTherapie(jetzt));
        Assert.Equal(B("Ahorn").Id, marke.Key);
        Assert.Equal("in Therapie: Holzwerkstatt bis 11:30", marke.Value.Text);
        Assert.Equal(MarkenArt.Status, marke.Value.Art);
        Assert.Empty(s.InTherapie(new DateTime(2026, 11, 9, 11, 30, 0)));

        // Abgesagt ersetzt „gegangen“ (der neueste Eintrag gilt).
        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Abgesagt, "AB", jetzt.AddMinutes(10));
        Assert.Equal("abgesagt 09:15 · AB", TagesplanRegeln.Marke(Holz("Ahorn"), jetzt));
        Assert.Empty(s.InTherapie(jetzt.AddMinutes(11)));
    }

    [Fact]
    public void Eine_spaetere_Statusmeldung_geht_vor()
    {
        jetzt = new DateTime(2026, 11, 9, 9, 10, 0);
        var z = Holz("Distel");
        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Teilgenommen, "CD", jetzt);
        Assert.Contains(B("Distel").Id!.Value, s.InTherapie(jetzt.AddMinutes(5)).Keys);
        jetzt = jetzt.AddMinutes(20);
        s.StatusmeldungSetzen(B("Distel").Id!.Value, "beim Arzt", jetzt.AddHours(1), "CD", "R1");
        Assert.DoesNotContain(B("Distel").Id!.Value, s.InTherapie(jetzt.AddMinutes(1)).Keys);
    }
}
