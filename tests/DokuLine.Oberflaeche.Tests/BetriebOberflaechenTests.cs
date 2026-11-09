using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 37: Betriebsmodus – Anmelden mit Passwort (Einmalpasswort ändern, Sperre), automatische Sperre nach
// Leerlauf, Einstellungen ohne Demo, Sicherung, Häuser. Nur mit ausgedachten Daten.
[Collection("Oberflaeche")]
public class BetriebOberflaechenTests
{
    private static void ImBetrieb(Umgebung u, Action aktion)
    {
        Betrieb.Setzen(new BetriebWerte { Datenordner = u.Ordner, SperreMinuten = 10 });
        try { aktion(); }
        finally { Betrieb.Setzen(null); }
    }

    [Fact]
    public void Anmelden_mit_Einmalpasswort_und_Sperre() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        ImBetrieb(u, () =>
        {
            Assert.Equal(Betrieb.StatusText, Stil.DemoHinweis);
            var m = u.Speicher.AlleMitarbeiter().First(x => x.Aktiv && !RechteRegeln.IstAdministrator(x));
            u.Speicher.EinmalpasswortSetzen(m.Kuerzel, "Einmal-1234", "TST", DateTime.Now);
            using var f = new AnmeldenFenster(u.Speicher, m.Kuerzel);
            Assert.Empty(f.DemoListe.Items);   // im Betrieb keine Liste der Mitarbeiter
            Hilfe.Foto(f, "152_betrieb_anmelden");
            f.Passwort.Text = "falsch";
            Assert.False(f.Anmelden());
            Assert.Equal("Kürzel oder Passwort stimmt nicht.", f.Fehler.Text);
            string? geaendert = null;
            f.PasswortAendernFragen = k =>
            {
                u.Speicher.PasswortAendern(k, "Einmal-1234", "Sonnenblume-42", DateTime.Now);
                geaendert = k;
                return true;
            };
            f.Passwort.Text = "Einmal-1234";
            Assert.True(f.Anmelden());
            Assert.Equal(m.Kuerzel, geaendert);
            Assert.Equal(m.Kuerzel, f.Angemeldet!.Kuerzel);
            for (int i = 0; i < PasswortRegeln.Fehlversuche; i++) u.Speicher.PasswortAnmelden(m.Kuerzel, "falsch");
            using var zwei = new AnmeldenFenster(u.Speicher, m.Kuerzel);
            zwei.Passwort.Text = "Sonnenblume-42";
            Assert.False(zwei.Anmelden());
            Assert.Equal(PasswortRegeln.GesperrtText, zwei.Fehler.Text);
            using var tst = new AnmeldenFenster(u.Speicher, "TST");   // TST hat im Betrieb kein Passwort
            Assert.False(tst.Anmelden());
            Assert.StartsWith("Für dieses Kürzel ist noch kein Passwort", tst.Fehler.Text);
        });
    });

    [Fact]
    public void Leerlauf_sperrt_und_Einstellungen_ohne_Demo() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        ImBetrieb(u, () =>
        {
            Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
            var vorher = Leerlauf.Dauer;
            try
            {
                using var f = new Hauptfenster(u.Speicher);
                Hilfe.Zeigen(f);
                f.BenutzerWaehlen("TST");
                bool gesperrt = false;
                // Das Anmeldefenster nach der Sperre: hier nur festhalten, dass gesperrt ist (Abbrechen schließt das Programm).
                using (new DialogErsatz(fenster => { if (fenster is AnmeldenFenster) gesperrt = f.Gesperrt; return DialogResult.Cancel; }))
                {
                    Leerlauf.Dauer = () => TimeSpan.FromMinutes(5);
                    f.BetriebPruefen(DateTime.Now);
                    Assert.False(f.Gesperrt);
                    Leerlauf.Dauer = () => TimeSpan.FromMinutes(11);
                    f.BetriebPruefen(DateTime.Now);
                }
                Assert.True(gesperrt);   // nach 10 Minuten ohne Eingabe gesperrt
                Assert.NotNull(u.Speicher.LetzteSicherungZeit);   // fällige Sicherung nachgeholt
                Assert.NotEmpty(Speicher.Sicherungen(Betrieb.Sicherungsordner));
            }
            finally { Leerlauf.Dauer = vorher; }

            using var seite = new EinstellungenSeite(u.Speicher) { Size = new Size(1200, 900) };
            seite.Zeigen();
            var titel = seite.Karten.Select(k => k.Titel).ToList();
            Assert.DoesNotContain("Demo-Daten zurücksetzen", titel);
            Assert.DoesNotContain("Ort der Datenbank", titel);
            Assert.Contains("Update vorbereiten", titel);
            Assert.Contains("Passwort ändern", titel);
            Assert.Contains("Zugriffsprotokoll", titel);
            Assert.Contains("Häuser", titel);
        });
    });

    [Fact]
    public void Sicherung_Haeuser_und_Zugriffsprotokoll() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using (var sicherung = new SicherungFenster(u.Speicher))
        {
            Assert.True(sicherung.Sichern());
            Assert.Single(sicherung.Liste.Items);
            sicherung.Liste.SelectedIndex = 0;
            Assert.True(sicherung.Pruefen());
            sicherung.Bestaetigen = _ => true;
            Assert.True(sicherung.Ruecksichern());
            Hilfe.Foto(sicherung, "153_sicherung");
        }
        using (var haeuser = new HaeuserFenster(u.Speicher))
        {
            haeuser.Name_.Text = "Haus am See (ausgedacht)";
            Assert.True(haeuser.Ausfuehren(() => u.Speicher.HausAnlegen(haeuser.Name_.Text, Recht.Kuerzel)));
            Assert.Single(haeuser.Haeuser.Items);
            Hilfe.Foto(haeuser, "154_haeuser");
        }
        u.Speicher.Zugriff("TST", u.Demo("Ahorn").Id, ZugriffRegeln.Geoeffnet, "Akte");
        using var protokoll = new ZugriffsprotokollFenster(u.Speicher);
        Assert.True(protokoll.Liste.Items.Count >= 1);
        Assert.Contains(protokoll.Liste.Items.Cast<ListViewItem>(), i => i.SubItems[2].Text == u.Demo("Ahorn").Name && i.SubItems[3].Text == ZugriffRegeln.Geoeffnet);
    });
}
