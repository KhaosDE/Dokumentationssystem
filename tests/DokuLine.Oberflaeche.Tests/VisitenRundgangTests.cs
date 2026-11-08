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
        int imBereich = u.Speicher.AlleBewohner().Count(b => b.Wohnbereich == "Wohnbereich A");

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
        Assert.Equal("Schläft gut, Abenddosis bleibt.", u.Speicher.VisitenEntwurf("Wohnbereich A")!.Eintrag(ahorn).Besprochen);   // schon gespeichert
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
            Assert.StartsWith("Visitenliste Wohnbereich A", Assert.Single(ersatz.Titel));
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
        Assert.Null(u.Speicher.VisitenEntwurf("Wohnbereich A"));
        Assert.Equal(4, seite.Karten.Count);   // 3 Demo-Visiten und die neue
        Assert.Contains(u.Speicher.Berichte(ahorn), b => b.Text.Contains("(Dr. Linde (ausgedacht)), dabei: EF: Schläft gut"));
        Assert.Equal("Visite beginnen …", seite.BeginnenKnopf.Text);
    });
}
