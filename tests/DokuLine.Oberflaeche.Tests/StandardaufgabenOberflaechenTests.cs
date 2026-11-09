using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 25: Standardaufgaben entstehen zu Beginn der Schicht, Gruppen nach Schicht,
// begründet schließen, Vorlagen nur mit Recht, Schichtzeiten in den Einstellungen.
[Collection("Oberflaeche")]
public class StandardaufgabenOberflaechenTests
{
    [Fact]
    public void Standardaufgabe_der_laufenden_Schicht_und_schliessen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        // Eine Vorlage für die gerade laufende Schicht, jeden Tag (damit der Test zu jeder Zeit gleich läuft).
        var schicht = SchichtTermin.Zu(DateTime.Now);
        u.Speicher.VorlageSpeichern(AufgabenRegeln.VorlageAusEingabe(new VorlageEingabe
        {
            Text = "Testrundgang", Schicht = schicht.Schicht, Tage = [.. Enum.GetValues<DayOfWeek>()], Kuerzel = "EF", Bereich = "R1",
        }, DateTime.Now));
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Aufgaben");
        var seite = f.Aufgaben;
        AufgabeKarte Karte() => seite.Karten.Single(k => k.Aufgabe.Text == "Testrundgang");
        Assert.Contains("Standardaufgabe", Karte().MarkenTexte);
        Assert.StartsWith($"JETZT: {schicht.Text.ToUpperInvariant()}", seite.Abschnitte.First(a => a.StartsWith("JETZT")));
        Assert.True(Karte().SchliessenKnopf.Visible);
        f.BereichWaehlen("Übersicht");
        f.BereichWaehlen("Aufgaben");
        Assert.Single(seite.Karten, k => k.Aufgabe.Text == "Testrundgang");   // nicht doppelt
        Hilfe.Foto(f, "80_aufgaben_schichten");

        string? fehler = null;
        using (new DialogErsatz(fenster =>
        {
            var zu = Assert.IsType<SchliessenFenster>(fenster);
            Assert.False(zu.Uebernehmen());
            fehler = zu.Fehler.Text;
            zu.Begruendung.Text = "Notfall, keine Zeit";
            return zu.Uebernehmen() ? DialogResult.OK : DialogResult.Cancel;
        }))
            Karte().SchliessenKnopf.PerformClick();
        Assert.StartsWith("Bitte kurz begründen", fehler);
        Assert.DoesNotContain(seite.Karten, k => k.Aufgabe.Text == "Testrundgang");
        seite.ErledigteZeigen.Checked = true;
        Assert.Contains(Karte().MarkenTexte, m => m.StartsWith("geschlossen ") && m.EndsWith("CD: Notfall, keine Zeit"));

        // Ausdruck: Aufgaben der laufenden Schicht.
        using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
        {
            seite.DruckKnopf.PerformClick();
            Assert.Equal($"Aufgaben – {schicht.Text}", Assert.Single(ersatz.Titel).Split(" (")[0]);
        }
    });

    [Fact]
    public void Vorlagen_verwalten_nur_mit_Recht_und_Schichtzeiten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        Arbeitsbereich.Gewaehlt = "R1";   // wie im Programm: Die Liste zeigt den gewählten Bereich
        using (var fenster = new VorlagenFenster(u.Speicher))
        {
            Assert.Equal(9, fenster.Liste.Items.Count);   // Demo-Standardaufgaben von R1 (in den Tests stillgelegt)
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(fenster.NeuKnopf));
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung
        using (var fenster = new VorlagenFenster(u.Speicher))
        {
            Hilfe.Zeigen(fenster);   // die Liste nimmt eine Auswahl erst an, wenn sie angezeigt wird
            Assert.Null(Stil.Sperrgrund(fenster.NeuKnopf));
            using (new DialogErsatz(f =>
            {
                var neu = Assert.IsType<VorlageFenster>(f);
                neu.TextFeld.Text = "Blumen gießen";
                neu.SchichtWahl.SelectedIndex = (int)Schicht.Frueh;
                neu.Taeglich.Checked = false;
                for (int i = 0; i < 7; i++) neu.Tage[i].Checked = i == 0;   // nur Montag
                neu.Uhrzeit.Text = "09:00";
                Hilfe.Foto(neu, "81_standardaufgabe");
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                fenster.Bearbeiten(null);
            Assert.Equal(10, fenster.Liste.Items.Count);
            var item = fenster.Liste.Items[9];
            Assert.Equal(["Blumen gießen", "R1", "Frühdienst", "Mo", "09:00"], item.SubItems.Cast<ListViewItem.ListViewSubItem>().Take(5).Select(s => s.Text));
            fenster.Waehlen(9);
            fenster.Umschalten();
            Assert.Equal("stillgelegt", fenster.Liste.Items[9].SubItems[6].Text);
            Hilfe.Foto(fenster, "82_standardaufgaben");
        }

        // Schichtzeiten in den Einstellungen (Bereich dieses Rechners).
        var vorher = BerichtRegeln.Zeiten;
        try
        {
            using var einstellungen = new EinstellungenSeite(u.Speicher);
            einstellungen.Zeigen();
            Assert.Equal("06:00", einstellungen.Frueh.Text);
            einstellungen.Spaet.Text = "05:00";
            Assert.False(einstellungen.SchichtzeitenUebernehmen());
            Assert.True(einstellungen.Fehler.Text.Length > 0);
            einstellungen.Frueh.Text = "06:30";
            einstellungen.Spaet.Text = "14:30";
            einstellungen.Nacht.Text = "21:45";
            Assert.True(einstellungen.SchichtzeitenUebernehmen());
            Assert.Equal(new TimeOnly(21, 45), u.Speicher.Schichtzeiten(Einstellungen.Laden().Bereich).Nacht);
            Assert.Equal(new TimeOnly(6, 30), BerichtRegeln.Zeiten.Frueh);
        }
        finally { BerichtRegeln.Zeiten = vorher; }
    });
}
