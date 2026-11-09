using System.Net;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 29o: Anzeige der laufenden Therapien für den Fernseher – einrichten (nur mit Recht), ein- und
// ausschalten, Seite mit und ohne Zugangscode, Vollbild auf einem Bildschirm.
[Collection("Oberflaeche")]
public class TherapieAnzeigeOberflaechenTests
{
    private const int Port = 18080;

    [Fact]
    public void Einrichten_einschalten_und_abrufen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.False(f.Anzeige.Laeuft);   // Vorgabe: aus

        using (var fenster = new AnzeigeFenster(u.Speicher, f.Anzeige))
        {
            Assert.False(fenster.AnKnopf.Checked);
            Assert.True(fenster.KurzerName.Checked);
            Assert.Equal(["R1", "R2", "R3"], fenster.Adressen.Keys.Order());
            Assert.Contains($":{AnzeigeRegeln.StandardPort}/anzeige/R3?code=", fenster.Adressen["R3"].Text);
            Assert.Null(Stil.Sperrgrund(fenster.SpeichernKnopf));
            fenster.PortFeld.Text = "80";
            Assert.False(fenster.Speichern());
            Assert.Contains("Port", fenster.Fehler.Text);
            fenster.PortFeld.Text = Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            fenster.AnKnopf.Checked = true;
            Hilfe.Foto(fenster, "133_anzeige_einrichten");
            Assert.True(fenster.Speichern());
        }
        Assert.True(f.Anzeige.Laeuft);
        Assert.Equal(Port, f.Anzeige.Port);
        string code = AnzeigeEinstellungen.Laden().Codes["R1"];

        using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{Port}") };
        var seite = http.GetAsync($"/anzeige/R1?code={code}").GetAwaiter().GetResult();
        Assert.Equal(HttpStatusCode.OK, seite.StatusCode);
        string html = seite.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.Contains("Therapien · R1", html);
        Assert.Contains(Stil.DemoHinweis, html);
        Assert.DoesNotContain("Zi.", html);
        Assert.Equal(HttpStatusCode.Forbidden, http.GetAsync("/anzeige/R1?code=FALSCH").GetAwaiter().GetResult().StatusCode);

        // Ein neuer Code macht den alten ungültig; ausgeschaltet antwortet niemand mehr.
        using (var fenster = new AnzeigeFenster(u.Speicher, f.Anzeige))
        {
            string vorher = fenster.Adressen["R1"].Text;
            static IEnumerable<Control> Alle(Control c) => c.Controls.Cast<Control>().SelectMany(k => Alle(k).Prepend(k));
            Assert.Single(Alle(fenster).OfType<Button>(), b => b.AccessibleName == "Neuer Code für R1");
            fenster.CodeNeu("R1");
            Assert.NotEqual(vorher, fenster.Adressen["R1"].Text);
            fenster.AnKnopf.Checked = false;
            Assert.True(fenster.Speichern());
        }
        Assert.False(f.Anzeige.Laeuft);
        Assert.NotEqual(code, AnzeigeEinstellungen.Laden().Codes["R1"]);
        Assert.ThrowsAny<HttpRequestException>(() => http.GetAsync($"/anzeige/R1?code={code}").GetAwaiter().GetResult());
    });

    [Fact]
    public void Ohne_Recht_nur_ansehen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Mitarbeiter Wohnbereich
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        using var fenster = new AnzeigeFenster(u.Speicher, f.Anzeige);
        Assert.NotNull(Stil.Sperrgrund(fenster.SpeichernKnopf));
        Assert.False(fenster.Speichern());
        Assert.False(AnzeigeEinstellungen.Laden().An);
    });

    [Fact]
    public void Vollbild_fuer_einen_Bereich() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        using var vollbild = new AnzeigeVollbild(u.Speicher, "R1", vollerName: false, Screen.PrimaryScreen!);
        vollbild.Jetzt = () => DateTime.Today.AddHours(10).AddMinutes(15);
        vollbild.Aktualisieren();
        Assert.NotEmpty(vollbild.Termine);
        var r1 = u.Speicher.AlleBewohner().Where(b => b.Wohnbereich == "R1").Select(b => AnzeigeRegeln.Name(b, false)).ToHashSet();
        Assert.All(vollbild.Termine.SelectMany(t => t.Namen), n => Assert.Contains(n, r1));
        vollbild.Show(f);
        Application.DoEvents();
        Assert.Equal(Screen.PrimaryScreen!.Bounds, vollbild.Bounds);
        Hilfe.Foto(vollbild, "134_anzeige_vollbild");
        vollbild.Close();
    });
}
