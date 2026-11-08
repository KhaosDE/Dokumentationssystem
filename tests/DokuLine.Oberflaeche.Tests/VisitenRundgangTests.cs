using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26c: Visite mit allen Bewohnern – beginnen (Datum, Arzt, dabei), der Reihe nach
// durchgehen mit Medikation und Eingabe, Entwurf in der Datenbank, unterbrechen und fortsetzen,
// abschließen mit gesammelten Aufgaben, Ausdruck „Visitenliste“.
[Collection("Oberflaeche")]
public class VisitenRundgangTests
{
    [Fact]
    public void Visite_mit_allen_Bewohnern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Visiten");
        var seite = f.Visiten;
        Assert.Equal("Visite beginnen …", seite.BeginnenKnopf.Text);
        Assert.Contains(seite.BeginnenKnopf, f.Kopf.Knoepfe);
        long ahorn = u.Demo("Ahorn").Id!.Value, birke = u.Demo("Birke").Id!.Value;
        int imBereich = u.Speicher.AlleBewohner().Count(b => b.Wohnbereich == "R1");

        string? fehler = null;
        using (new DialogErsatz(fenster =>
        {
            var b = Assert.IsType<VisiteBeginnenFenster>(fenster);
            Assert.False(b.Beginnen());
            fehler = b.Fehler.Text;
            Assert.Equal("EF", b.Teilnehmer.AlsText);
            b.Arzt.Text = "Dr. Linde (ausgedacht)";
            Hilfe.Foto(b, "95_visite_beginnen");
            return b.Beginnen() ? DialogResult.OK : DialogResult.Cancel;
        }))
            seite.BeginnenKnopf.PerformClick();
        Assert.Contains("Arzt fehlt", fehler);
        var r = Assert.IsType<VisitenRundgang>(seite.Rundgang);
        Assert.Equal(imBereich, r.Bewohner.Count);
        Assert.Equal($"noch offen: {imBereich} von {imBereich}", r.OffenText.Text);
        Assert.Equal(ahorn, r.Bewohner[r.Aktuell].Id);   // nach Zimmer: 101 zuerst
        Assert.StartsWith("Anton Ahorn · Zi. 101", r.Name_.Text);
        Assert.StartsWith("Allergien: ", r.Allergie.Text);
        Assert.True(r.Tabelle.Rows.Count >= 2);   // Quetiapin und Pantoprazol
        Assert.Contains("Dr. Linde (ausgedacht)", r.LetzteVisite.Text);   // Demo-Visite vor 6 Tagen

        r.Feld!.Besprochen.Text = "Schläft gut, Abenddosis bleibt.";
        r.Feld.AnordnungenGeaendert.Checked = true;
        r.Feld.Zeilen[0].Setzen(AnordnungArt.Dosis, "Quetiapin");
        r.Naechster();
        Assert.Equal($"noch offen: {imBereich - 1} von {imBereich}", r.OffenText.Text);
        Assert.Equal("1 Aufgabe vorgemerkt", r.VorgemerktText.Text);
        Assert.True(r.Liste.Items[0].Checked);   // Häkchen „besprochen“
        Assert.Equal("Schläft gut, Abenddosis bleibt.", u.Speicher.VisitenEntwurf("R1")!.Eintrag(ahorn).Besprochen);   // schon gespeichert
        Assert.Equal(birke, r.Bewohner[r.Aktuell].Id);
        r.Feld!.Besprochen.Text = "Herzklopfen beim Treppensteigen.";
        r.Feld.FacharztNoetig.Checked = true;
        r.Feld.Fachrichtung.Text = "Kardiologie";
        Hilfe.Foto(f, "96_visite_rundgang");
        r.Vorheriger();
        Assert.Equal("Schläft gut, Abenddosis bleibt.", r.Feld!.Besprochen.Text);   // Entwurf wieder da
        Assert.Equal("2 Aufgaben vorgemerkt", r.VorgemerktText.Text);

        // Unterbrechen und fortsetzen.
        r.UnterbrechenKnopf.PerformClick();
        Assert.Null(seite.Rundgang);
        Assert.Equal("Visite fortsetzen", seite.BeginnenKnopf.Text);
        Assert.True(seite.Beginnen());   // ohne Fenster: der Entwurf geht weiter
        r = seite.Rundgang!;
        Assert.Equal("Herzklopfen beim Treppensteigen.", r.Entwurf.Eintrag(birke).Besprochen);

        // Ausdruck „Visitenliste“.
        using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
        {
            seite.VisitenlisteDrucken();
            Assert.StartsWith("Visitenliste R1", Assert.Single(ersatz.Titel));
        }

        // Abschließen: erst jetzt gesammelt die Aufgaben.
        int einblendungen = 0;
        using (new DialogErsatz(fenster =>
        {
            var e = Assert.IsType<VisitenAufgabenFenster>(fenster);
            einblendungen++;
            Assert.Equal(2, e.Aufgaben.Count);
            return DialogResult.Cancel;   // „Später“
        }))
            r.AbschliessenKnopf.PerformClick();
        Assert.Equal(1, einblendungen);
        Assert.Null(seite.Rundgang);
        Assert.Null(u.Speicher.VisitenEntwurf("R1"));
        Assert.Equal(6, seite.Karten.Count);   // 5 Demo-Visiten (seit Schritt 26h) und die neue
        Assert.Contains(u.Speicher.Berichte(ahorn), b => b.Text.Contains("(Dr. Linde (ausgedacht)), dabei: EF: Schläft gut"));
        Assert.Equal("Visite beginnen …", seite.BeginnenKnopf.Text);
    });

    // Bauplan Schritt 26k: „◀ Vorheriger“ und „Nächster Bewohner ▶“ direkt unter der Eingabe; „Nächster“
    // ist grau mit Hinweis, bis zum Bewohner etwas eingetragen ist. Die Liste links bleibt frei wählbar.
    [Fact]
    public void Naechster_Bewohner_erst_nach_dem_Eintrag() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        var entwurf = new VisitenEntwurf { Bereich = "R1", Datum = DateOnly.FromDateTime(DateTime.Now), Arzt = "Dr. Linde (ausgedacht)", Kuerzel = "EF", Begonnen = DateTime.Now };
        using var fenster = new Form { Size = new Size(1200, 900) };
        var r = new VisitenRundgang(u.Speicher, entwurf) { Dock = DockStyle.Fill };
        fenster.Controls.Add(r);
        Hilfe.Zeigen(fenster);
        Assert.Equal("Nächster Bewohner ▶", r.NaechsterKnopf.Text);
        Assert.Equal(VisitenRundgang.ErstEintragenText, Stil.Sperrgrund(r.NaechsterKnopf));
        Assert.True(r.VorherigerKnopf.Enabled);
        // Unter der Eingabe (die Knöpfe liegen in einer eigenen Zeile, deshalb in Bildschirmkoordinaten).
        int feldUnten = r.Feld!.PointToScreen(new Point(0, r.Feld.Height)).Y;
        Assert.True(r.NaechsterKnopf.PointToScreen(Point.Empty).Y >= feldUnten);
        Assert.True(r.VorherigerKnopf.PointToScreen(Point.Empty).Y >= feldUnten);
        int erster = r.Aktuell;
        r.NaechsterKnopf.PerformClick();
        Assert.Equal(erster, r.Aktuell);   // gesperrt: bleibt beim Bewohner

        r.Feld.Besprochen.Text = "Keine Veränderung, Medikation bleibt.";
        Assert.Null(Stil.Sperrgrund(r.NaechsterKnopf));
        Hilfe.Foto(fenster, "113_visite_naechster_bewohner");
        r.NaechsterKnopf.PerformClick();
        Assert.Equal(erster + 1, r.Aktuell);
        Assert.Equal(VisitenRundgang.ErstEintragenText, Stil.Sperrgrund(r.NaechsterKnopf));   // beim nächsten wieder leer

        r.VorherigerKnopf.PerformClick();   // zurück geht immer
        Assert.Equal(erster, r.Aktuell);
        Assert.Null(Stil.Sperrgrund(r.NaechsterKnopf));   // hier steht schon etwas
        r.Waehlen(5);                                      // die Liste links bleibt frei wählbar
        Assert.Equal(5, r.Aktuell);
    });
}

