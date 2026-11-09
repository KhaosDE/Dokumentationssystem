using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 30: Akte, Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger – anzeigen, als neue
// Fassung ändern, „keine“, Kurzzeilen in den Stammdaten, Fristen in der Übersicht, zugeteilte Ärzte oben.
[Collection("Oberflaeche")]
public class AkteAngabenOberflaechenTests
{
    private static DateOnly Heute => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void Reiter_zeigen_und_aendern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var ahorn = u.Demo("Ahorn");
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(ahorn.Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Equal(["Stammdaten", "Betreuung", "Angehörige", "Ärzte", "Krankenkasse", "Kostenträger", "Medikamentenplan"], akte.Reiter.Namen.Take(7));

        foreach (var name in new[] { "Betreuung", "Angehörige", "Ärzte", "Krankenkasse", "Kostenträger" })
        {
            akte.Reiter.Waehlen(name);
            Assert.True(akte.Angaben.Visible);
            Assert.False(akte.ReiterPlatzhalter.Visible);
            Assert.NotEmpty(akte.Angaben.Zeilen);
            Hilfe.Foto(f, $"135_akte_{name.ToLowerInvariant()}");
        }

        // Krankenkasse ändern: eine neue Fassung, die bisherige bleibt im Verlauf.
        akte.Reiter.Waehlen("Krankenkasse");
        int vorher = u.Speicher.Fassungen(ahorn.Id!.Value, Angabe.Krankenkasse).Count;
        using (var fenster = new AngabeFenster(u.Speicher, ahorn, Angabe.Krankenkasse))
        {
            fenster.Kasse.SelectedItem = Demodaten.Krankenkassen[2];
            fenster.Pflegegrad.SelectedItem = "3";
            Hilfe.Foto(fenster, "136_krankenkasse_aendern");
            Assert.True(fenster.Speichern());
        }
        Assert.Equal(vorher + 1, u.Speicher.Fassungen(ahorn.Id!.Value, Angabe.Krankenkasse).Count);
        var kasse = u.Speicher.Angaben(ahorn.Id!.Value, Heute)[Angabe.Krankenkasse].Lesen<Krankenkasse>()!;
        Assert.Equal((Demodaten.Krankenkassen[2], 3), (kasse.Kasse, kasse.Pflegegrad));

        // „keine“: die Felder sind gesperrt, gespeichert wird der dritte Zustand.
        using (var fenster = new AngabeFenster(u.Speicher, ahorn, Angabe.Angehoerige))
        {
            fenster.Keine.Checked = true;
            Assert.True(fenster.Speichern());
        }
        Assert.Equal(AngabeZustand.Keine, u.Speicher.Angaben(ahorn.Id!.Value, Heute)[Angabe.Angehoerige].Zustand);

        // Kurzzeilen in den Stammdaten; ein Klick führt in den Reiter.
        akte.Reiter.Waehlen("Stammdaten");
        f.Bewohner.AkteZeigen(ahorn.Id!.Value);
        Assert.StartsWith(Demodaten.Krankenkassen[2], akte.KrankenkasseText.Text);
        Assert.Equal(Stil.Akzent, akte.KrankenkasseText.ForeColor);
    });

    [Fact]
    public void Ohne_Recht_nur_lesen_und_nicht_erfasst_in_Achtung_Farbe() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "OP" });   // Therapie: kein „Bewohner bearbeiten“
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var ohne = u.Speicher.AlleBewohner().First(b => !u.Speicher.Angaben(b.Id!.Value, Heute).ContainsKey(Angabe.Krankenkasse));
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(ohne.Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Equal(AngabenRegeln.NichtErfasst, akte.KrankenkasseText.Text);
        Assert.Equal(Stil.Achtung.Text, akte.KrankenkasseText.ForeColor);
        akte.Reiter.Waehlen("Krankenkasse");
        Assert.True(akte.Angaben.Zeilen.Single().Achtung);
        Assert.NotNull(Stil.Sperrgrund(akte.Angaben.AendernKnopf));
    });

    [Fact]
    public void Fristen_in_der_Uebersicht_und_Aerzte_oben() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);
        f.BereichWaehlen("Übersicht");
        Assert.True(f.Uebersicht.FristenZeigen);
        Assert.Contains(f.Uebersicht.Zeilen.SelectMany(z => z.Marken), m => m.Text.StartsWith("Kostenzusage endet"));
        Hilfe.Foto(f, "137_uebersicht_fristen");

        var mitArzt = u.Speicher.AlleBewohner().First(b =>
            u.Speicher.Angaben(b.Id!.Value, Heute).GetValueOrDefault(Angabe.Aerzte)?.Lesen<AerzteAngabe>()?.Hausarzt.Stand == ArztStand.Arzt);
        var hausarzt = u.Speicher.PersonNach(u.Speicher.Angaben(mitArzt.Id!.Value, Heute)[Angabe.Aerzte].Lesen<AerzteAngabe>()!.Hausarzt.PersonId!.Value)!;
        using var wahl = new ArztWahl(u.Speicher, mitArzt.Id);
        Assert.Equal(hausarzt.Anzeige, wahl.Liste.Items[0]);
    });
}

// Bauplan 30a: Zuständigkeit Forensik und FOSA im Reiter „Betreuung“, Marken, Bestellweg im Medikamentenplan.
[Collection("Oberflaeche")]
public class ZustaendigkeitOberflaechenTests
{
    private static DateOnly Heute => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void Forensik_FOSA_und_Bestellung_per_EMail() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var forensik = u.Speicher.AlleBewohner().First(b => b.Wohnbereich == "R1" &&
            ZustaendigkeitRegeln.InForensik(u.Speicher.Angaben(b.Id!.Value, Heute).GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>()));
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(forensik.Id!.Value);
        var akte = f.Bewohner.Akte;
        Assert.Contains("Forensik", akte.KopfMarken);
        akte.Reiter.Waehlen("Betreuung");
        Assert.Equal(("Forensik", "ja"), (akte.Angaben.Zeilen[0].Beschriftung, akte.Angaben.Zeilen[0].Wert));
        Assert.Contains(akte.Angaben.Zeilen, z => z.Beschriftung == "Klinik" && z.Wert.StartsWith(Demodaten.Klinik));
        if (akte.Angaben.Zeilen.FirstOrDefault(z => z.Beschriftung == "Betreuer") is { Wert: not null } betreuer && betreuer.Wert != AngabenRegeln.NichtErfasst)
            Assert.EndsWith(ZustaendigkeitRegeln.BetreuerNichtZustaendig, betreuer.Wert);
        Assert.True(akte.Angaben.ZustaendigkeitKnopf.Visible);
        Hilfe.Foto(f, "138_betreuung_forensik");

        // Medikamentenplan: „Bestellung über: Forensik · …“; per E-Mail nur nach dem Hinweis, mit den angekreuzten Medikamenten.
        akte.Reiter.Waehlen("Medikamentenplan");
        var m = akte.Medikation;
        Assert.StartsWith("Bestellung über: Forensik", m.BestellungText.Text);
        string? geoeffnet = null;
        var vorher = Extern.Oeffnen;
        Extern.Oeffnen = a => geoeffnet = a;
        try
        {
            m.MedikamenteWaehlen = namen => [0];
            m.EmailBestaetigen = text => { Assert.Contains("Gesundheitsdaten", text); return false; };
            if (Stil.Sperrgrund(m.BestellenKnopf) is null)
            {
                Assert.False(m.Bestellen());   // abgebrochen: nichts geöffnet
                Assert.Null(geoeffnet);
                m.EmailBestaetigen = _ => true;
                Assert.True(m.Bestellen());
                Assert.StartsWith("mailto:", geoeffnet);
                Assert.Contains(Uri.EscapeDataString(ZustaendigkeitRegeln.Betreff(forensik)), geoeffnet);
            }
        }
        finally { Extern.Oeffnen = vorher; }

        // Zuständigkeit ändern: FOSA aus, Forensik bleibt.
        using (var fenster = new AngabeFenster(u.Speicher, forensik, Angabe.Zustaendigkeit))
        {
            Assert.Equal((int)JaNein.Ja, fenster.ForensikWahl.SelectedIndex);
            fenster.FosaWahl.SelectedIndex = (int)JaNein.Nein;
            Assert.True(fenster.Speichern());
        }
        var neu = u.Speicher.Angaben(forensik.Id!.Value, Heute)[Angabe.Zustaendigkeit].Lesen<Zustaendigkeit>()!;
        Assert.Equal((JaNein.Ja, JaNein.Nein), (neu.Forensik, neu.Fosa));
    });
}
