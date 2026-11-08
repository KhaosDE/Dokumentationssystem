using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Wohnbereich-ToDo: Zähler, Kurzliste über den Karten, Bereich „Aufgaben“, neue Aufgabe,
// weitergeben, erledigt, aus einem Bericht heraus, Ausdruck.
[Collection("Oberflaeche")]
public class AufgabenOberflaechenTests
{
    private static (Umgebung, Hauptfenster) Oeffnen(string kuerzel = "CD", bool bereich = true)
    {
        var u = new Umgebung();
        if (kuerzel.Length > 0) Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        if (bereich) f.Navigation.Single(n => n.Text == "Aufgaben").Ausloesen();
        return (u, f);
    }

    private static SeitenleistenEintrag Eintrag(Hauptfenster f) => f.Navigation.Single(n => n.Text == "Aufgaben");

    [Fact]
    public void Zaehler_in_der_Seitenleiste_und_Kurzliste_ueber_den_Karten() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen(bereich: false);
        using (u)
        using (f)
        {
            Assert.Equal(5, Eintrag(f).Anzahl);
            Assert.True(Eintrag(f).Dringend);   // eine Demo-Aufgabe ist überfällig
            Assert.Equal("Aufgaben, 5 offen, 1 überfällig", Eintrag(f).AccessibleName);
            var kurz = f.Bewohner.Aufgaben;
            Assert.Equal("AUFGABEN · 5 OFFEN · 1 ÜBERFÄLLIG", kurz.KopfText);
            Assert.Equal(AufgabenKurzliste.Hoechstens, kurz.ZeilenTexte.Count());
            Assert.StartsWith("überfällig · Aushang mit dem Wochenplan", kurz.ZeilenTexte.First());
            Assert.Equal("Alle 5 Aufgaben …", kurz.AlleKnopf.Text);
            kurz.AlleKnopf.PerformClick();
            Assert.Equal("Aufgaben", f.AktiverBereich);
        }
    });

    [Fact]
    public void Bereich_Aufgaben_ueberfaellige_oben_mit_Weitergabe() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Aufgaben;
            Assert.Same(seite, f.Seite("Aufgaben"));
            Assert.Equal("5 offene Aufgaben · 1 überfällig", f.Kopf.KleinText);
            Assert.Equal("Neue Aufgabe …", Assert.Single(f.Kopf.Knoepfe).Text);
            // Schritt 25: gruppiert nach Schicht (Summe wie bisher 5), oben die laufende.
            Assert.Equal(5, seite.Abschnitte.Sum(a => int.Parse(a[(a.LastIndexOf('(') + 1)..^1])));
            Assert.Contains(seite.Abschnitte, a => a.StartsWith("JETZT: "));
            var erste = seite.Karten.Single(k => k.MarkenTexte[0].StartsWith("überfällig seit "));
            Assert.StartsWith("überfällig seit ", erste.MarkenTexte[0]);
            Assert.Contains("jetzt dran", erste.MarkenTexte);
            Assert.Contains(erste.VerlaufTexte, v => v.StartsWith("weitergegeben von KL") && v.EndsWith("Drucker war leer, Papier ist bestellt."));
            Assert.Contains(seite.Karten, k => k.MarkenTexte.Contains("wer Zeit hat"));
            Assert.Contains(seite.Karten, k => k.MarkenTexte.Any(m => m.StartsWith("für Spätdienst ")));
            Assert.False(seite.KuerzelHinweis.Visible);
            seite.ErledigteZeigen.Checked = true;
            Assert.Equal("ERLEDIGT (4)", seite.Abschnitte.Last());   // 1 Demo-Aufgabe und 3 aus den Demo-Visiten (Schritt 26)
            Assert.StartsWith("erledigt ", seite.Karten.Last().MarkenTexte[0]);
            Assert.False(seite.Karten.Last().Erledigt.Visible);
        }
    });

    [Fact]
    public void Neue_Aufgabe_fuer_eine_bestimmte_Schicht() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            string? fehler = null;
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<AufgabeFenster>(fenster);
                Assert.Equal("CD", neu.Verfasser.Text);
                Assert.True(neu.NaechsteSchicht.Checked);                 // Vorgabe
                Assert.StartsWith("nächste Schicht (", neu.NaechsteSchicht.Text);
                Assert.Equal(0, neu.BewohnerWahl.SelectedIndex);           // kein bestimmter Bewohner
                neu.Inhalt.Text = "Blumen im Gemeinschaftsraum gießen";
                neu.BestimmteSchicht.Checked = true;
                Assert.False(neu.NaechsteSchicht.Checked);   // nur eine Wahl
                Assert.True(neu.SchichtTag.Enabled);
                neu.SchichtTag.Text = "gestern";
                Assert.False(neu.Speichern());
                fehler = neu.Fehler.Text;
                neu.SchichtTag.Text = Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(2)));
                neu.SchichtArt.SelectedIndex = 0;   // Frühdienst
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                f.Kopf.Knoepfe[0].PerformClick();
            Assert.Contains("Tag der Schicht", fehler);
            var karte = f.Aufgaben.Karten.Single(k => k.Aufgabe.Text == "Blumen im Gemeinschaftsraum gießen");
            Assert.Contains(karte.MarkenTexte, m => m.StartsWith("für Frühdienst "));
            Assert.Equal(6, Eintrag(f).Anzahl);
            Assert.Equal("6 offene Aufgaben · 1 überfällig", f.Kopf.KleinText);
        }
    });

    [Fact]
    public void Weitergeben_mit_Notiz_und_erledigen() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            var seite = f.Aufgaben;
            long id = seite.Karten.Single(k => k.Aufgabe.Text.StartsWith("Getränkevorrat")).Aufgabe.Id;
            AufgabeKarte Karte() => seite.Karten.Single(k => k.Aufgabe.Id == id);

            using (new DialogErsatz(fenster =>
            {
                var weiter = Assert.IsType<WeitergebenFenster>(fenster);
                weiter.Notiz.Text = "Heute keine Zeit wegen Ausflug.";
                return weiter.Uebernehmen() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().Weitergeben.PerformClick();
            Assert.Contains(Karte().VerlaufTexte, v => v.StartsWith("weitergegeben von CD") && v.EndsWith("Heute keine Zeit wegen Ausflug."));
            Assert.Contains(Karte().MarkenTexte, m => m.StartsWith("für "));   // jetzt für die nächste Schicht statt „wer Zeit hat“

            Karte().Erledigt.PerformClick();
            Assert.DoesNotContain(seite.Karten, k => k.Aufgabe.Id == id);
            Assert.Equal(4, Eintrag(f).Anzahl);
            seite.ErledigteZeigen.Checked = true;
            Assert.EndsWith("· CD", Karte().MarkenTexte[0]);
        }
    });

    [Fact]
    public void Ohne_Kuerzel_gesperrt_mit_Hinweis() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen(kuerzel: "");
        using (u)
        using (f)
        {
            Assert.True(f.Aufgaben.KuerzelHinweis.Visible);
            Assert.All(f.Aufgaben.Karten, k => Assert.False(k.Erledigt.Enabled || k.Weitergeben.Enabled));
        }
    });

    [Fact]
    public void Aufgabe_aus_einem_Bericht_uebernimmt_den_Bewohner() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen(bereich: false);
        using (u)
        using (f)
        {
            var ahorn = u.Demo("Ahorn");
            f.Bewohner.AkteZeigen(ahorn.Id!.Value);
            f.Bewohner.Akte.Reiter.Waehlen("Berichte");
            var karte = f.Bewohner.Akte.Berichte.Liste.Karten[0];
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<AufgabeFenster>(fenster);
                Assert.StartsWith("Ahorn, Anton", neu.BewohnerWahl.Text);
                neu.Inhalt.Text = "Mit Anton den neuen Termin in den Kalender eintragen";
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                karte.AufgabeKnopf.PerformClick();
            var aufgabe = u.Speicher.Aufgaben(DateTime.Now).Single(a => a.Text.StartsWith("Mit Anton"));
            Assert.Equal(ahorn.Id, aufgabe.BewohnerId);
            Assert.Equal(karte.Bericht.Id, aufgabe.BerichtId);
            Assert.Equal(6, Eintrag(f).Anzahl);   // Zähler folgt sofort
        }
    });

    [Fact]
    public void Ausdruck_der_offenen_Aufgaben() => Hilfe.Sta(() =>
    {
        var (u, f) = Oeffnen();
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                f.Aufgaben.DruckKnopf.PerformClick();
                Assert.StartsWith("Aufgaben – ", Assert.Single(ersatz.Titel));   // Aufgabenliste der Schicht
            }
            var jetzt = DateTime.Now;
            var offen = u.Speicher.Aufgaben(jetzt);
            using var bild = new Bitmap(800, 1130);
            using var g = Graphics.FromImage(bild);
            Assert.Equal(offen.Count, new AufgabenDruck(offen, new Dictionary<long, string>(), jetzt).SeiteZeichnen(g, 0));
        }
    });
}
