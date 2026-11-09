using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 23: Tagesplan unter „Übersicht“ – alle Termine von heute, „gegangen“ (derselbe
// Eintrag wie im Wochenplan) und „Wo müsste er sein?“ auf Karte und Akte.
[Collection("Oberflaeche")]
public class TagesplanOberflaechenTests
{
    private const string Name = "Testgruppe Tagesplan";

    // Eine ausgedachte Therapie heute, den ganzen Tag (damit der Test zu jeder Uhrzeit gleich läuft).
    private static long TherapieHeute(Umgebung u, string nachname)
    {
        var jetzt = DateTime.Now;
        var heute = DateOnly.FromDateTime(jetzt);
        var a = u.Speicher.AngebotAnlegen(TherapieRegeln.AusEingabe(new AngebotEingabe
        {
            Name = Name, Therapeut = "Test (ausgedacht)", Ort = "Raum 1", Termine = [(heute.DayOfWeek, "00:00", "23:59")],
            Hoechstens = "4", GiltAb = Datum.Text(heute), Kuerzel = "OP",
        }, jetzt));
        return u.Speicher.Zuteilen(a.Id, u.Demo(nachname).Id!.Value, heute, null, "OP", jetzt).Id;
    }

    [Fact]
    public void Gegangen_im_Tagesplan_und_Wo_muesste_er_sein() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        long zuteilung = TherapieHeute(u, "Ahorn");
        long ahorn = u.Demo("Ahorn").Id!.Value;
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.Equal("Tagesplan", f.Navigation[1].Text);   // direkt unter „Übersicht“
        f.Navigation.Single(n => n.Text == "Tagesplan").Ausloesen();
        Assert.Equal("Tagesplan", f.AktiverBereich);
        var t = f.Tagesplan;
        // Uhr und Datum oben rechts, Demo-Hinweis, Status als Wort.
        Assert.Matches(@"^\d\d:\d\d$", t.Tafel.Uhr.Text);
        Assert.EndsWith(DateTime.Now.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture), t.Tafel.DatumText.Text);
        Assert.Equal(Stil.DemoHinweis, t.Tafel.Demo.Text);
        Assert.StartsWith("TAGESPLAN · R1", t.Tafel.Titel.Text);
        TafelZeileAnzeige Zeile() => t.Tafel.Zeilen.Single(k => k.Daten.Termin.Titel == Name);
        Assert.Equal("NOCH OFFEN", Zeile().StatusWort);   // läuft, nichts bestätigt
        Assert.Equal(TafelBlock.JetztUndGleich, Zeile().Daten.Block);
        Assert.Equal("JETZT UND GLEICH", t.Gruppen[0]);
        Assert.Equal(t.TafelZeilen.Select(z => z.Block).Order(), t.TafelZeilen.Select(z => z.Block));   // „Erledigt“ unten
        Assert.Contains("noch offen", f.Kopf.KleinText);
        Assert.All(t.Tafel.Zeilen.Where(k => !k.Daten.Zeile.Therapie), k => Assert.Null(k.BestaetigenKnopf));   // nur Therapien
        Assert.Equal(["gegangen", "abgesagt", "ausgefallen"], t.Tafel.Menue.Items.Cast<ToolStripItem>().Select(i => i.Text));
        void Bestaetigen(AnzeigeTafel tafel, int eintrag)   // wie „bestätigen ▾“ → Eintrag (ohne das Menü aufklappen)
        {
            tafel.Gewaehlt = tafel.Zeilen.Single(k => k.Daten.Termin.Titel == Name);
            tafel.Menue.Items[eintrag].PerformClick();
            Application.DoEvents();
        }
        Hilfe.Foto(f, "75_tagesplan");

        Bestaetigen(t.Tafel, 0);   // „gegangen“
        Assert.Equal("GEGANGEN", Zeile().StatusWort);
        Assert.EndsWith("· CD", Zeile().Detail);
        // Derselbe Eintrag wie im Wochenplan.
        var heute = DateOnly.FromDateTime(DateTime.Now);
        Assert.Equal(Teilnahme.Teilgenommen, u.Speicher.Woche(ahorn, WochenplanRegeln.Montag(heute)).Single(x => x.ZuteilungId == zuteilung && x.Tag == heute).Teilnahme);
        Bestaetigen(t.Tafel, 0);   // noch einmal: kein zweiter Eintrag
        Assert.Single(u.Speicher.TeilnahmeEintraege(heute), e => e.Key == zuteilung);

        // Wo müsste er sein? Karte und Akte zeigen „in Therapie: … bis 23:59“.
        string wo = TagesplanRegeln.InTherapieText(Name, new TimeOnly(23, 59));
        f.Bewohner.AkteZeigen(ahorn);
        Assert.Equal(wo, f.Bewohner.Akte.WoText);
        f.BereichWaehlen("Übersicht");
        Assert.Contains(f.Uebersicht.Zeilen.Single(k => k.Bewohner.Id == ahorn).Marken, m => m.Text == wo);
        Assert.Equal(wo, f.Uebersicht.Zeilen.Single(k => k.Bewohner.Id == ahorn).Wo);   // Spalte „Wo“ (Schritt 26l)

        // Nach Bewohner, abgesagt (rutscht nach „Erledigt“), und ohne Anmeldung gesperrt.
        f.BereichWaehlen("Tagesplan");
        t.NachBewohner.Checked = true;
        Assert.NotEmpty(t.Gruppen);
        Assert.Equal(t.Gruppen.Count, t.Zeilen.Select(z => z.Bewohner.Id).Distinct().Count());
        Hilfe.Foto(f, "76_tagesplan_nach_bewohner");
        Bestaetigen(t.Tafel, 1);   // „abgesagt“
        Assert.Equal("ABGESAGT", Zeile().StatusWort);
        Assert.True(Zeile().Blass);
        Assert.Empty(u.Speicher.InTherapie(DateTime.Now));
        f.BenutzerWaehlen("");
        Assert.Equal(RechteRegeln.NichtAngemeldetText, Stil.Sperrgrund(Zeile().BestaetigenKnopf!));
        Assert.False(t.Setzen(Zeile().Daten.Zeile, Teilnahme.Teilgenommen));
    });

    [Fact]
    public void Gewaehlter_Bereich_oder_alle_Bereiche() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "GH", Bereich = "R2" });   // Bereichsleitung R2 am Rechner von R2
        TherapieHeute(u, "Ahorn");                                               // R1
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Tagesplan");
        var t = f.Tagesplan;
        Assert.All(t.Zeilen, z => Assert.Equal("R2", z.Bewohner.Wohnbereich));
        Assert.DoesNotContain(t.Tafel.Zeilen, k => k.Daten.Termin.Titel == Name);
        Assert.Contains("R2", f.Kopf.KleinText);
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);
        Assert.Contains(t.Tafel.Zeilen, k => k.Daten.Termin.Titel == Name);
        Assert.Contains("alle Bereiche", f.Kopf.KleinText);
    });

    [Fact]
    public void Vollbild_mit_F11_und_Esc() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        TherapieHeute(u, "Ahorn");
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Tagesplan");
        var t = f.Tagesplan;
        Assert.Null(t.Vollbild);
        var f11 = Message.Create(f.Handle, 0x0100, (IntPtr)(int)Keys.F11, IntPtr.Zero);
        Assert.True(f.PreProcessMessage(ref f11));
        var v = Assert.IsType<TafelVollbild>(t.Vollbild);
        Assert.Equal(FormBorderStyle.None, v.FormBorderStyle);
        Assert.Equal(t.Tafel.Zeilen.Select(z => z.StatusWort), v.Tafel.Zeilen.Select(z => z.StatusWort));
        Assert.Equal(Stil.DemoHinweis, v.Tafel.Demo.Text);   // auch im Vollbild sichtbar
        Hilfe.Foto(v, "108_tagesplan_vollbild");
        // Eine Bestätigung im Vollbild erneuert beide Tafeln.
        v.Tafel.Gewaehlt = v.Tafel.Zeilen.Single(k => k.Daten.Termin.Titel == Name);
        v.Tafel.Menue.Items[0].PerformClick();
        Application.DoEvents();
        Assert.Equal("GEGANGEN", v.Tafel.Zeilen.Single(k => k.Daten.Termin.Titel == Name).StatusWort);
        Assert.Equal("GEGANGEN", t.Tafel.Zeilen.Single(k => k.Daten.Termin.Titel == Name).StatusWort);
        var esc = Message.Create(v.Handle, 0x0100, (IntPtr)(int)Keys.Escape, IntPtr.Zero);
        Assert.True(v.PreProcessMessage(ref esc));
        Application.DoEvents();
        Assert.Null(t.Vollbild);
    });

    [Fact]
    public void Spalten_passen_sich_der_Breite_an() => Hilfe.Sta(() =>
    {
        using var bezug = new Panel();
        int Px(int wert) => Stil.Px(bezug, wert);
        var schmal = TafelAufteilung.Berechnen(bezug, Px(680), nachBewohner: false);
        Assert.Equal(2, schmal.Textspalten);   // Bewohner und Termin; Ort und Begleitung im Hinweis
        Assert.Equal(0, schmal.Ort.Width);
        Assert.False(schmal.Detail);
        var mittel = TafelAufteilung.Berechnen(bezug, Px(900), nachBewohner: false);
        Assert.Equal(4, mittel.Textspalten);
        Assert.False(mittel.Detail);
        var breit = TafelAufteilung.Berechnen(bezug, Px(1700), nachBewohner: false);
        Assert.True(breit.Detail);
        Assert.All(new[] { breit.Bewohner, breit.Termin, breit.Ort, breit.Begleitung }, r => Assert.True(r.Width >= Px(120)));
        Assert.True(breit.Knopf.Right <= Px(1700));
        var nachBewohner = TafelAufteilung.Berechnen(bezug, Px(680), nachBewohner: true);
        Assert.Equal(0, nachBewohner.Bewohner.Width);   // der Name steht in der Überschrift
        Assert.True(nachBewohner.Termin.Width > 0 && nachBewohner.Ort.Width > 0);
    });
}
