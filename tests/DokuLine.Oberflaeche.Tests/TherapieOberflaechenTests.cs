using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Therapie-Bereich: Angebote mit Teilnehmern und freien Plätzen, anlegen, ändern ab Datum,
// zuteilen mit Warnung bei Überschneidung, Teilnahme beenden, Verlauf; Anbieter und Rechte (Schritt 22).
[Collection("Oberflaeche")]
public class TherapieOberflaechenTests
{
    private static (Umgebung, Hauptfenster, TherapieSeite) Oeffnen()
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "OP" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Navigation.Single(n => n.Text == "Therapie").Ausloesen();
        return (u, f, f.Therapie);
    }

    private static string Tag(int tage) => Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(tage)));

    [Fact]
    public void Angebote_mit_Teilnehmern_und_freien_Plaetzen() => Hilfe.Sta(() =>
    {
        var (u, f, t) = Oeffnen();
        using (u)
        using (f)
        {
            Assert.Same(t, f.Seite("Therapie"));
            Assert.Equal("Neues Angebot …", Assert.Single(f.Kopf.Knoepfe).Text);
            Assert.StartsWith("10 Angebote · ", f.Kopf.KleinText);   // seit Schritt 26h drei mehr
            Assert.Equal(10, t.Karten.Count);
            var holz = t.Karten.Single(k => k.Angebot.Am(DateOnly.FromDateTime(DateTime.Today)).Name == "Holzwerkstatt");
            Assert.Equal(["Therapie", "Gruppe", "Mo 09:00–11:30, Mi 09:00–11:30", "4 Plätze frei"], holz.MarkenTexte);
            Assert.StartsWith("Teilnehmer (2 von 6): Anton Ahorn, Dieter Distel", holz.TeilnehmerText);
            var koch = t.Karten.Single(k => k.Angebot.Am(DateOnly.FromDateTime(DateTime.Today)).Name == "Kochgruppe");
            Assert.Contains("später dazu: Emil Eiche ab ", koch.TeilnehmerText);
            var einzel = t.Karten.Single(k => k.Angebot.Am(DateOnly.FromDateTime(DateTime.Today)).Name == "Einzelgespräch");
            Assert.Contains("voll", einzel.MarkenTexte);
            t.TagFeld.Text = Tag(-20);   // vor der Zeitänderung der Bewegungsgruppe
            var bewegung = t.Karten.Single(k => k.Angebot.Am(DateOnly.FromDateTime(DateTime.Today)).Name == "Bewegungsgruppe");
            Assert.Contains("Do 14:00–15:00", bewegung.MarkenTexte);
            Assert.Contains(bewegung.MarkenTexte, m => m.StartsWith("geändert ab "));
        }
    });

    [Fact]
    public void Neues_Angebot_zuteilen_mit_Warnung_und_beenden() => Hilfe.Sta(() =>
    {
        var (u, f, t) = Oeffnen();
        using (u)
        using (f)
        {
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<AngebotFenster>(fenster);
                Assert.False(neu.Speichern());
                neu.NameFeld.Text = "Kräutergruppe";   // „Gartengruppe“ gibt es seit Schritt 26h in R3
                neu.Therapeut.Text = "Ben Amsel";
                neu.Ort.Text = "Garten";
                neu.TerminSetzen(0, DayOfWeek.Monday, "10:00", "11:00");   // überschneidet sich mit der Holzwerkstatt
                neu.Hoechstens.Text = "4";
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                f.Kopf.Knoepfe[0].PerformClick();
            AngebotKarte Karte() => t.Karten.Single(k => k.Angebot.Fassungen[0].Name == "Kräutergruppe");
            Assert.Contains("4 Plätze frei", Karte().MarkenTexte);

            string? warnung = null;
            using (new DialogErsatz(fenster =>
            {
                var zu = Assert.IsType<ZuteilenFenster>(fenster);
                zu.BewohnerWaehlen(u.Demo("Ahorn").Id!.Value);
                Assert.False(zu.Speichern());          // erst die Warnung
                warnung = zu.Warnung.Text;
                return zu.Speichern() ? DialogResult.OK : DialogResult.Cancel;   // dann trotzdem
            }))
                Karte().ZuteilenKnopf.PerformClick();
            Assert.Contains("Überschneidung mit: Holzwerkstatt Mo 09:00–11:30", warnung);
            Assert.StartsWith("Teilnehmer (1 von 4): Anton Ahorn", Karte().TeilnehmerText);

            using (new DialogErsatz(fenster =>
            {
                var ende = Assert.IsType<BeendenFenster>(fenster);
                ende.Ab.Text = Tag(1);
                return ende.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().BeendenKnopf.PerformClick();
            Assert.Contains($"Anton Ahorn (bis {Tag(0)})", Karte().TeilnehmerText);

            string? verlauf = null;
            using (new DialogErsatz(fenster => { verlauf = Assert.IsType<TextFenster>(fenster).Inhalt.Text; return DialogResult.Cancel; }))
                Karte().VerlaufKnopf.PerformClick();
            Assert.Contains("angelegt, gilt ab", verlauf);
            Assert.Contains("Anton Ahorn zugeteilt ab", verlauf);
            Assert.Contains($"Teilnahme beendet ab {Tag(1)}", verlauf);
        }
    });

    [Fact]
    public void Aendern_ab_Datum() => Hilfe.Sta(() =>
    {
        var (u, f, t) = Oeffnen();
        using (u)
        using (f)
        {
            var musik = t.Karten.Single(k => k.Angebot.Fassungen[0].Name == "Musikgruppe");
            using (new DialogErsatz(fenster =>
            {
                var aendern = Assert.IsType<AngebotFenster>(fenster);
                Assert.Equal("Musikgruppe", aendern.NameFeld.Text);
                aendern.TerminSetzen(0, DayOfWeek.Tuesday, "16:00", "17:00");
                aendern.GiltAb.Text = Tag(7);
                return aendern.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                musik.AendernKnopf.PerformClick();
            musik = t.Karten.Single(k => k.Angebot.Fassungen[0].Name == "Musikgruppe");
            Assert.Contains("Mo 15:00–16:00", musik.MarkenTexte);   // heute noch die alte Zeit
            Assert.Contains($"geändert ab {Tag(7)}", musik.MarkenTexte);
            t.TagFeld.Text = Tag(8);
            Assert.Contains("Di 16:00–17:00", t.Karten.Single(k => k.Angebot.Fassungen[0].Name == "Musikgruppe").MarkenTexte);
        }
    });
    // Schritt 22: Jedes Angebot gehört der Therapie oder einem Wohnbereich; alle sehen alles,
    // ändern und zuteilen darf nur der Anbieter. Filter nach Anbieter.
    [Fact]
    public void Anbieter_Filter_und_Rechte() => Hilfe.Sta(() =>
    {
        var (u, f, t) = Oeffnen();   // angemeldet als OP (Therapie)
        using (u)
        using (f)
        {
            AngebotKarte Karte(string name) => t.Karten.Single(k => k.Angebot.Fassungen[^1].Name == name);
            Assert.Equal("R1", Karte("Kochgruppe").MarkenTexte[0]);
            Assert.Null(Stil.Sperrgrund(Karte("Holzwerkstatt").ZuteilenKnopf));
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(Karte("Kochgruppe").ZuteilenKnopf));
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(Karte("Kochgruppe").AendernKnopf));
            Assert.Null(Stil.Sperrgrund(Karte("Kochgruppe").VerlaufKnopf));   // ansehen dürfen alle
            using (var neu = new AngebotFenster(u.Speicher, null))
                Assert.Equal([TherapieRegeln.AnbieterTherapie], neu.Anbieter.Items.Cast<string>());

            // Filter
            Assert.Equal([TherapieSeite.AlleAnbieter, "Therapie", "R1", "R2"], t.AnbieterFilter.Items.Cast<string>().Take(4));
            t.AnbieterFilter.SelectedItem = "R1";
            Assert.Equal("Kochgruppe", Assert.Single(t.Karten).Angebot.Fassungen[^1].Name);
            Assert.StartsWith("1 Angebote · ", f.Kopf.KleinText);
            t.AnbieterFilter.SelectedItem = "Therapie";
            Assert.Equal(6, t.Karten.Count);
            t.AnbieterFilter.SelectedItem = TherapieSeite.AlleAnbieter;
            Assert.Equal(10, t.Karten.Count);

            // Bereichsleitung R1: nur die eigenen Angebote; neue nur für R1.
            f.BenutzerWaehlen("EF");
            Assert.Null(Stil.Sperrgrund(Karte("Kochgruppe").ZuteilenKnopf));
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(Karte("Holzwerkstatt").ZuteilenKnopf));
            Assert.Equal("Nur für R2.", Stil.Sperrgrund(Karte("Kreativwerkstatt").AendernKnopf));
            Assert.Null(Stil.Sperrgrund(t.NeuKnopf));
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<AngebotFenster>(fenster);
                Assert.Equal(["R1"], neu.Anbieter.Items.Cast<string>());
                neu.NameFeld.Text = "Gartengruppe A";
                neu.Therapeut.Text = "Emma Fink";
                neu.TerminSetzen(0, DayOfWeek.Friday, "10:00", "11:00");
                neu.Hoechstens.Text = "4";
                Hilfe.Foto(neu, "74_angebot_anbieter");
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                t.NeuKnopf.PerformClick();
            Assert.Equal("R1", Karte("Gartengruppe A").Angebot.Anbieter);
            Hilfe.Foto(f, "73_therapie_anbieter");

            // Mitarbeiter Wohnbereich ohne das Recht: nichts ändern, aber alles sehen.
            f.BenutzerWaehlen("CD");
            Assert.All(t.Karten, k => Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(k.ZuteilenKnopf)));
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(t.NeuKnopf));
            Assert.Equal(11, t.Karten.Count);
        }
    });
}
