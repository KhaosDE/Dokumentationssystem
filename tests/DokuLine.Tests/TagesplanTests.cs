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

    // Bauplan Schritt 26g: Anzeigetafel – Status als Wort und Block nach Uhrzeit und Teilnahme.
    private (TafelStatus, TafelBlock) Um(TagesplanZeile z, int stunde, int minute) =>
        TafelRegeln.Einordnen(z, new DateTime(2026, 11, 9, stunde, minute, 0));

    [Fact]
    public void Tafel_Status_einer_Therapie()
    {
        var z = Holz("Ahorn");   // 09:00–11:30
        Assert.Equal((TafelStatus.Geplant, TafelBlock.Spaeter), Um(z, 8, 29));
        Assert.Equal((TafelStatus.Gleich, TafelBlock.JetztUndGleich), Um(z, 8, 30));    // 30 Minuten vorher
        Assert.Equal((TafelStatus.NochOffen, TafelBlock.JetztUndGleich), Um(z, 9, 0));
        Assert.Equal((TafelStatus.NochOffen, TafelBlock.JetztUndGleich), Um(z, 13, 0));  // bleibt oben, bis jemand bestätigt
        Assert.Equal("", TafelRegeln.Bestaetigt(z));

        jetzt = new DateTime(2026, 11, 9, 9, 5, 0);
        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Teilgenommen, "CD", jetzt);
        z = Holz("Ahorn");
        Assert.Equal((TafelStatus.Gegangen, TafelBlock.JetztUndGleich), Um(z, 10, 0));
        Assert.Equal((TafelStatus.Zurueck, TafelBlock.Erledigt), Um(z, 11, 30));
        Assert.Equal("09:05 · CD", TafelRegeln.Bestaetigt(z));
        Assert.Equal("GEGANGEN", TafelRegeln.Wort(TafelStatus.Gegangen));
        Assert.Equal("NOCH OFFEN", TafelRegeln.Wort(TafelStatus.NochOffen));
        Assert.Equal("ZURÜCK", TafelRegeln.Wort(TafelStatus.Zurueck));

        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Abgesagt, "AB", jetzt.AddMinutes(5));
        Assert.Equal((TafelStatus.Abgesagt, TafelBlock.Erledigt), Um(Holz("Ahorn"), 9, 10));
        s.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, Montag, Teilnahme.Ausgefallen, "AB", jetzt.AddMinutes(6));
        Assert.Equal((TafelStatus.Ausgefallen, TafelBlock.Erledigt), Um(Holz("Ahorn"), 9, 10));
        Assert.Equal(Holz("Ahorn").Termin.Hinweis, Holz("Ahorn").Termin.Begleitung);   // Spalte „Begleitung“: Therapeut
        Assert.NotEqual("", Holz("Ahorn").Termin.Begleitung);
    }

    [Fact]
    public void Tafel_Status_anderer_Termine_und_Reihenfolge()
    {
        var arzt = new TagesplanZeile(B("Birke"), new Termin { Tag = Montag, Von = new(14, 0), Bis = new(15, 0), Titel = "Zahnarzt", Quelle = TerminQuelle.Kalender }, null);
        Assert.Equal((TafelStatus.Geplant, TafelBlock.Spaeter), Um(arzt, 13, 0));
        Assert.Equal((TafelStatus.Gleich, TafelBlock.JetztUndGleich), Um(arzt, 13, 45));
        Assert.Equal((TafelStatus.Jetzt, TafelBlock.JetztUndGleich), Um(arzt, 14, 30));
        Assert.Equal((TafelStatus.Vorbei, TafelBlock.Erledigt), Um(arzt, 15, 0));
        var ganztags = arzt with { Termin = arzt.Termin with { Ganztags = true, Titel = "Heimfahrt" } };
        Assert.Equal((TafelStatus.Jetzt, TafelBlock.JetztUndGleich), Um(ganztags, 0, 0));
        Assert.Equal((TafelStatus.Jetzt, TafelBlock.JetztUndGleich), Um(ganztags, 23, 59));

        var zehn = new DateTime(2026, 11, 9, 10, 0, 0);
        var tafel = TafelRegeln.Tafel(s.Tagesplan(Montag, s.AlleBewohner()).Append(arzt), zehn);
        Assert.Equal(tafel.Select(x => x.Block).Order(), tafel.Select(x => x.Block));   // Blöcke von oben nach unten
        foreach (var block in tafel.GroupBy(x => x.Block))
        {
            var zeiten = block.Select(x => x.Termin.Ganztags ? TimeOnly.MinValue : x.Termin.Von).ToList();
            Assert.Equal(zeiten.Order(), zeiten);   // im Block nach Uhrzeit
        }
        Assert.Contains(tafel, x => x.Termin.Titel == "Holzwerkstatt" && x.Block == TafelBlock.JetztUndGleich);
        Assert.Equal(TafelBlock.Spaeter, tafel.Single(x => x.Termin.Titel == "Zahnarzt").Block);
        var nachBewohner = TafelRegeln.NachBewohner(s.Tagesplan(Montag, s.AlleBewohner()), zehn);
        Assert.Equal(nachBewohner.Select(x => x.Bewohner.Nachname).Order(StringComparer.CurrentCulture), nachBewohner.Select(x => x.Bewohner.Nachname));
    }
}
