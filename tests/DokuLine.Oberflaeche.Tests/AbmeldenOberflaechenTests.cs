using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritte 26a2 und 26a3: Administrator TST (alle Rechte, nicht abwählbar, der letzte bleibt)
// und Abmelden – Knopf oben rechts, Seitenleiste, Einstellungen, F12; danach nur die Sperrfläche
// und das Anmeldefenster; angefangene Berichte als Entwurf.
[Collection("Oberflaeche")]
public class AbmeldenOberflaechenTests
{
    private static DialogResult AnmeldenAls(Form fenster, string kuerzel)
    {
        var a = Assert.IsType<AnmeldenFenster>(fenster);
        a.Kuerzel.Text = kuerzel;
        return a.Anmelden() ? DialogResult.OK : DialogResult.Cancel;
    }

    [Fact]
    public void Administrator_in_Rechten_und_Mitarbeitern() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using (var rechte = new RechteFenster(u.Speicher))
        {
            Hilfe.Zeigen(rechte);
            int admin = rechte.Spalte("Administrator");
            Assert.Equal(1, admin);   // an erster Stelle
            Assert.True(rechte.Tabelle.Columns[admin].ReadOnly);
            Assert.All(rechte.Tabelle.Rows.Cast<DataGridViewRow>(), r => Assert.Equal(true, r.Cells[admin].Value));
            Assert.Equal(RechteRegeln.AdminImmerText, rechte.Tabelle.Rows[0].Cells[admin].ToolTipText);
            Assert.False(rechte.Setzen(rechte.Zeile(Funktionen.RechteVerwalten), admin, false));
            Assert.Contains("Administrator", u.Speicher.Rechte()[Funktionen.RechteVerwalten]);
            Hilfe.Foto(rechte, "98_rechte_administrator");
        }
        using (var fenster = new MitarbeiterFenster(u.Speicher))
        using (var formular = fenster.FormularErstellen(u.Speicher.MitarbeiterLesen("TST")))
        {
            Hilfe.Zeigen(formular);
            Assert.True(formular.LetzterAdmin.Visible);   // Hinweis statt Fehler
            Assert.False(formular.Aktiv.Enabled);
            Assert.False(formular.Positionen["Administrator"].Enabled);
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });   // Bereichsleitung: vergibt „Administrator“ nicht
        using (var fenster = new MitarbeiterFenster(u.Speicher))
        using (var neu = fenster.FormularErstellen(null))
            Assert.False(neu.Positionen["Administrator"].Enabled);
    });

    [Fact]
    public void Abmelden_mit_Knopf_und_wieder_anmelden() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "TST" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("TST");
        Assert.Equal("Angemeldet: TST – Test Administrator", f.Kopf.BenutzerText);
        Assert.Equal("Abmelden (F12)", f.Kopf.AbmeldenKnopf.Text);
        Assert.Equal("Abmelden (F12)", f.AbmeldenEintrag.Text);
        Hilfe.Foto(f, "99_abmelden_kopf");
        bool gesperrt = false;
        using (new DialogErsatz(fenster =>
        {
            var anmelden = Assert.IsType<AnmeldenFenster>(fenster);
            Assert.StartsWith("Abgemeldet", anmelden.Text);
            Assert.Equal("Programm beenden", anmelden.BeendenKnopf.Text);
            gesperrt = f.Gesperrt && !Recht.Angemeldet && !f.Kopf.Visible && !f.Bewohner.Visible;   // nichts mehr zu sehen, niemand angemeldet
            Hilfe.Foto(f, "100_abgemeldet");
            anmelden.Kuerzel.Text = "cd";   // Benutzer wechseln = mit anderem Kürzel anmelden
            return anmelden.Anmelden() ? DialogResult.OK : DialogResult.Cancel;
        }))
            f.Kopf.AbmeldenKnopf.PerformClick();
        Assert.True(gesperrt);
        Assert.False(f.Gesperrt);
        Assert.True(f.Kopf.Visible);
        Assert.Equal("Angemeldet: CD – Carl Dohle", f.Kopf.BenutzerText);
        Assert.Equal(Hauptfenster.Startbereich, f.AktiverBereich);

        // Eingeklappt: Symbol mit Hinweis.
        f.MenueUmschalten();
        Assert.True(f.AbmeldenEintrag.Visible);
        Assert.True(f.AbmeldenEintrag.Eingeklappt);
        f.MenueUmschalten();
    });

    [Fact]
    public void Angefangener_Bericht_wird_Entwurf() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("CD");
        var ahorn = u.Demo("Ahorn");
        var bericht = new BerichtFormular(u.Speicher, ahorn);
        Hilfe.Zeigen(bericht);
        bericht.Inhalt.Text = "Angefangen: war beim Frühstück ruhig.";
        bericht.Wichtig.Checked = true;

        // Abbrechen: bleibt angemeldet, das Fenster bleibt offen.
        using (new DialogErsatz(fenster =>
        {
            var frage = Assert.IsType<AbmeldenFrage>(fenster);
            Assert.Contains("Neuer Bericht", frage.Frage.Text);
            Assert.True(frage.MitEntwurf);
            Hilfe.Foto(frage, "101_abmelden_frage");
            return DialogResult.Cancel;
        }))
            Assert.False(f.Abmelden());
        Assert.True(Recht.Angemeldet);
        Assert.False(bericht.IsDisposed);

        // Als Entwurf speichern, abmelden, wieder anmelden.
        using (new DialogErsatz(fenster => fenster switch
        {
            AbmeldenFrage => DialogResult.Yes,
            AnmeldenFenster => AnmeldenAls(fenster, "CD"),
            _ => DialogResult.Cancel,
        }))
        {
            Assert.True(f.Abmelden());
            Assert.True(bericht.IsDisposed || !bericht.Visible);
            Application.DoEvents();   // das Anmeldefenster kommt, wenn die Dialoge zu sind
        }
        Assert.False(f.Gesperrt);
        var entwurf = Assert.Single(u.Speicher.Entwuerfe("CD"));
        Assert.Equal(ahorn.Id, entwurf.BewohnerId);
        Assert.Empty(u.Speicher.Entwuerfe("AB"));   // gehört nur CD
        Assert.True(f.Kopf.EntwurfKnopf.Visible);
        Assert.Equal("Entwurf fortsetzen", f.Kopf.EntwurfKnopf.Text);

        // Fortsetzen: „Neuer Bericht“ mit dem Entwurf; Speichern löscht ihn.
        using (new DialogErsatz(fenster =>
        {
            var formular = Assert.IsType<BerichtFormular>(fenster);
            Assert.Equal("Angefangen: war beim Frühstück ruhig.", formular.Inhalt.Text);
            Assert.True(formular.Wichtig.Checked);
            Assert.StartsWith("Entwurf vom ", formular.EntwurfHinweis.Text);
            formular.Bereiche[0].Checked = true;
            return formular.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            Assert.NotNull(f.EntwurfFortsetzen());
        Assert.Empty(u.Speicher.Entwuerfe("CD"));
        Assert.False(f.Kopf.EntwurfKnopf.Visible);
    });

    [Fact]
    public void F12_und_Einstellungen_melden_ab() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        var f = new Hauptfenster(u.Speicher);
        try
        {
            Hilfe.Zeigen(f);
            f.BenutzerWaehlen("EF");
            // Einstellungen → „Persönlich“ → „Abmelden (F12)“ (seit Schritt 29 eine Seite).
            f.EinstellungenOeffnen("Persönlich");
            Assert.Equal(Abmeldung.KnopfText, f.Einstellungsseite.AbmeldenKnopf.Text);
            using (new DialogErsatz(fenster => AnmeldenAls(fenster, "EF")))
                f.Einstellungsseite.AbmeldenKnopf.PerformClick();
            Assert.Equal("Angemeldet: EF – Emma Fink", f.Kopf.BenutzerText);

            // F12: die Taste wird abgefangen, das Abmelden läuft gleich danach.
            var taste = Message.Create(f.Handle, 0x0100, (IntPtr)(int)Keys.F12, IntPtr.Zero);
            var andere = Message.Create(f.Handle, 0x0100, (IntPtr)(int)Keys.F11, IntPtr.Zero);
            Assert.False(f.F12.PreFilterMessage(ref andere));
            int anmeldefenster = 0;
            using (new DialogErsatz(fenster =>
            {
                Assert.IsType<AnmeldenFenster>(fenster);
                anmeldefenster++;
                return DialogResult.Cancel;   // „Programm beenden“
            }))
            {
                Assert.True(f.F12.PreFilterMessage(ref taste));
                Application.DoEvents();
            }
            Assert.Equal(1, anmeldefenster);
            Assert.True(f.IsDisposed || !f.Visible);   // Programm beendet
        }
        finally { f.Dispose(); }
    });
}
