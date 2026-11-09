using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 35: Auskunftsleiste im Akte-Kopf, Reiter „Schweigepflicht“, Eintrag anlegen und widerrufen, Schnellabfrage,
// Spalte „Auskunft“ in der Übersicht.
[Collection("Oberflaeche")]
public class SchweigepflichtOberflaechenTests
{
    [Fact]
    public void Leiste_Reiter_Eintrag_und_Schnellabfrage() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var b = u.Demo("Ahorn");
        long id = b.Id!.Value;
        var heute = DateOnly.FromDateTime(DateTime.Now);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(id);
        var akte = f.Bewohner.Akte;
        Assert.Contains(f.Kopf.Knoepfe, k => k.Text == "Auskunft prüfen …");
        Assert.Equal(SchweigepflichtRegeln.Leiste(u.Speicher.Angaben(id, heute).GetValueOrDefault(Angabe.Schweigepflicht), u.Speicher.PersonenNamen(), heute)
            .Select(m => m.Text), akte.AuskunftMarken.Select(m => m.Text));
        Assert.True(akte.Auskunft.Bottom <= akte.Reiter.Top);   // über allen Reitern
        Assert.True(akte.Auskunft.Top >= akte.Allergie.Bottom);  // unter dem Allergiehinweis
        Hilfe.Foto(f, "145_auskunftsleiste");

        // Neuer Eintrag über das Formular: Person aus dem Verzeichnis, entbunden „nur Gesundheit“.
        using (var fenster = new AngabeFenster(u.Speicher, b, Angabe.Schweigepflicht))
        {
            Hilfe.Zeigen(fenster);   // Knöpfe reagieren auf PerformClick nur, wenn sie sichtbar sind
            int vorher = fenster.Entbindungen.Count;
            fenster.EntbindungPerson.SelectedIndex = 1;
            fenster.EntbindungBezeichnung.Text = "Nachbarin";
            fenster.EntbindungUmfang.Text = SchweigepflichtRegeln.UmfangGesundheit;
            fenster.Controls.OfType<TableLayoutPanel>().SelectMany(Alle).OfType<Button>().First(k => k.Text == "Hinzufügen").PerformClick();
            Assert.Equal(vorher + 1, fenster.Entbindungen.Count);
            Hilfe.Foto(fenster, "146_schweigepflicht_formular");
            // Widerruf: den Eintrag wählen, Datum setzen, übernehmen.
            fenster.EntbindungsListe.SelectedIndex = vorher;
            fenster.EntbindungWiderrufen.Text = Datum.Text(heute);
            fenster.Controls.OfType<TableLayoutPanel>().SelectMany(Alle).OfType<Button>().First(k => k.Text == "Übernehmen").PerformClick();
            Assert.Equal(heute, fenster.Entbindungen[vorher].WiderrufenAm);
            Assert.True(fenster.Speichern());
        }
        akte.NeuLaden();
        Assert.Contains(akte.AuskunftMarken, m => m.Text.StartsWith("✕ Nachbarin ·") && m.Widerrufen);
        akte.Reiter.Waehlen("Schweigepflicht");
        Assert.True(akte.Angaben.Visible);
        Assert.Contains(akte.Angaben.Zeilen, z => z.Beschriftung.StartsWith("Nachbarin ·") && z.Wert.Contains("widerrufen am"));
        Hilfe.Foto(f, "147_reiter_schweigepflicht");

        using var abfrage = new AuskunftFenster(u.Speicher, b);
        abfrage.Suche.Text = "Nachbarin";
        Assert.Equal(Entbindungsstand.KeineAuskunft, abfrage.Ergebnis!.Art);
        abfrage.Suche.Text = "Polizei";
        Assert.StartsWith("„Polizei“ ist nicht eingetragen", abfrage.Antwort.Text);
        Hilfe.Foto(abfrage, "148_auskunft_pruefen");

        f.BereichWaehlen("Übersicht");
        f.ArbeitsbereichWaehlen(BereichswahlRegeln.Alle);
        var zeile = f.Uebersicht.Zeilen.Single(z => z.Bewohner.Id == id);
        Assert.Equal(zeile.Auskunft!.Value.Kurz, zeile.Eintrag.SubItems[4].Text);
        Assert.Contains("Auskunft:", zeile.Beschreibung);
    });

    [Fact]
    public void Ohne_Eintrag_nie_wie_erlaubt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        var heute = DateOnly.FromDateTime(DateTime.Now);
        var ohne = u.Speicher.AlleBewohner().First(x => !u.Speicher.Angaben(x.Id!.Value, heute).ContainsKey(Angabe.Schweigepflicht));
        using var akte = new AkteAnsicht(u.Speicher) { Size = new Size(1200, 900) };
        akte.Zeigen(ohne);
        var m = Assert.Single(akte.AuskunftMarken);
        Assert.Equal((SchweigepflichtRegeln.NichtErfasstText, Entbindungsstand.NichtErfasst), (m.Text, m.Art));
        using var abfrage = new AuskunftFenster(u.Speicher, ohne);
        Assert.Equal(Entbindungsstand.NichtErfasst, abfrage.Ergebnis!.Art);   // ohne Eingabe schon die Antwort
    });

    private static IEnumerable<Control> Alle(Control c) => c.Controls.Cast<Control>().SelectMany(x => Alle(x).Prepend(x));
}
