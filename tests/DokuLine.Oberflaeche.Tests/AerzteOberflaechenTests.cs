using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26b/29a: Ärzte im Personenverzeichnis (Einstellungen → Verzeichnisse), Auswahl statt Freitext,
// „Arzt anlegen …“ direkt aus der Auswahl (mit Recht), Termin beim Arzt füllt Ort und Hinweis; die übrigen
// Verzeichnisse mit Zuordnung zu Bewohnern und Sprung in die Akte.
[Collection("Oberflaeche")]
public class AerzteOberflaechenTests
{
    [Fact]
    public void Auswahl_statt_Freitext_und_Arzt_anlegen() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var neu = new VerordnungFenster(u.Speicher, u.Demo("Fichte"));
        Assert.Equal(ComboBoxStyle.DropDownList, neu.Arzt.Liste.DropDownStyle);   // kein Freitext
        Assert.Equal(u.Speicher.Aerzte(nurAktive: true).Count, neu.Arzt.Liste.Items.Count);
        Assert.Contains("Dr. Ulme (ausgedacht) · Allgemeinmedizin (Hausarzt)", neu.Arzt.Liste.Items.Cast<string>());
        neu.Arzt.Text = "Dr. Linde (ausgedacht)";
        Assert.Equal("Dr. Linde (ausgedacht)", neu.Arzt.Gewaehlt);
        Assert.Null(Stil.Sperrgrund(neu.Arzt.AnlegenKnopf));
        Hilfe.Zeigen(neu);
        using (new DialogErsatz(f =>
        {
            var a = Assert.IsType<PersonFenster>(f);
            a.NameFeld.Text = "Dr. Kastanie (ausgedacht)";
            a.Funktion.Text = "Orthopädie";
            a.Email.Text = "kastanie@example.org";
            Hilfe.Foto(a, "93_arzt_anlegen");
            return a.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            neu.Arzt.AnlegenKnopf.PerformClick();
        Assert.Equal("Dr. Kastanie (ausgedacht)", neu.Arzt.Gewaehlt);   // gleich gewählt
        Assert.NotNull(u.Speicher.ArztNach("Dr. Kastanie (ausgedacht)"));
        // Ein älterer Name, der nicht im Verzeichnis steht, bleibt wählbar und ist gekennzeichnet.
        neu.Arzt.Text = "Dr. Alt (ausgedacht)";
        Assert.EndsWith("(nicht im Verzeichnis)", (string)neu.Arzt.Liste.SelectedItem!);

        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Mitarbeiter Wohnbereich: nur auswählen
        using var ohne = new AenderungFenster(u.Speicher, u.Speicher.Verordnungen(u.Demo("Ahorn").Id!.Value)[0], AenderungArt.Pause);
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(ohne.Arzt.AnlegenKnopf));
    });

    [Fact]
    public void Verzeichnisse_in_den_Einstellungen_und_Termin_beim_Arzt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Mitarbeiter Wohnbereich: ansehen
        using (var ohne = new EinstellungenSeite(u.Speicher))
        {
            ohne.GruppeWaehlen(EinstellungenSeite.VerzeichnisGruppe);
            Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(ohne.Verzeichnis.NeuKnopf));
            Assert.Same(ohne.Verzeichnis.NeuKnopf, ohne.Knoepfe[0]);   // in der Aktionsleiste
        }
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.WindowState = FormWindowState.Maximized;
        Assert.True(f.EinstellungenOeffnen(EinstellungenSeite.VerzeichnisGruppe));
        var v = f.Einstellungsseite.Verzeichnis;
        Assert.True(v.Visible);
        Assert.Equal(f.Einstellungsseite.Knoepfe, f.Kopf.Knoepfe.ToArray());
        Assert.StartsWith("Verzeichnisse · Ärzte · ", f.Kopf.KleinText);
        Assert.Equal(["Ärzte", "Betreuer", "Angehörige", "Forensik", "FOSA", "Sonstige Stellen"], v.GruppenKnoepfe.Values.Select(k => k.Text));
        Assert.Equal(u.Speicher.Aerzte().Count, v.Liste.Items.Count);
        Assert.Equal(PersonRegeln.Spalten(PersonGruppe.Arzt), v.Liste.Columns.Cast<ColumnHeader>().Select(c => c.Text));
        Assert.Contains(v.Liste.Items.Cast<ListViewItem>(), i => i.SubItems[6].Text == "inaktiv");
        Hilfe.Foto(f, "120_verzeichnisse");
        Assert.True(v.Waehlen("Dr. Esche (ausgedacht)"));
        Assert.True(v.Umschalten());
        Assert.DoesNotContain(u.Speicher.Aerzte(nurAktive: true), a => a.Name.StartsWith("Dr. Esche"));

        // Angehörige: Spalten, Suche, eine Person öffnen, „Akte öffnen“ springt in die Akte.
        v.GruppeWaehlen(PersonGruppe.Angehoerige);
        Assert.Equal(PersonGruppe.Angehoerige, v.Gruppe);
        Assert.Equal(["Name", "Telefon", "Mobil", "zugeordnet zu", "Status"], v.Liste.Columns.Cast<ColumnHeader>().Select(c => c.Text));
        Assert.Equal(u.Speicher.Personen(PersonGruppe.Angehoerige).Count, v.Liste.Items.Count);
        Assert.Equal(v.Liste.Items.Count, v.GruppenKnoepfe[PersonGruppe.Angehoerige].Anzahl);
        v.Suche.Text = "Ahorn";
        var hanna = Assert.Single(v.Liste.Items.Cast<ListViewItem>());
        Assert.Equal("Anton Ahorn (Mutter)", hanna.SubItems[3].Text);
        Assert.True(v.Waehlen(hanna.Text));
        using (new DialogErsatz(fenster =>
        {
            var p = Assert.IsType<PersonFenster>(fenster);
            Assert.Null(p.Sperrgrund);
            Assert.Equal("Anton Ahorn (Mutter)", p.Zugeordnet.Items[0]);
            Hilfe.Foto(p, "121_person_angehoerige");
            Assert.True(p.ZuordnungWaehlen(0));
            Assert.True(p.AkteOeffnen());
            return DialogResult.Cancel;
        }))
            v.AendernKnopf.PerformClick();
        Assert.Equal("Dokumentation", f.AktiverBereich);
        Assert.True(f.Bewohner.AkteSichtbar);
        Assert.Equal("Ahorn", f.Bewohner.Akte.Bewohner!.Nachname);

        // Neue zuständige Person der FOSA anlegen und einem Bewohner zuordnen.
        f.EinstellungenOeffnen(EinstellungenSeite.VerzeichnisGruppe);
        v.GruppeWaehlen(PersonGruppe.Fosa);
        v.Suche.Text = "";
        var birke = u.Demo("Birke").Id!.Value;
        using (new DialogErsatz(fenster =>
        {
            var p = Assert.IsType<PersonFenster>(fenster);
            p.NameFeld.Text = "Herr Brasse (ausgedacht)";
            p.Funktion.Text = "Pflege";
            p.Stelle.Text = "FOSA Musterstadt (ausgedacht)";
            return p.Speichern() ? DialogResult.OK : DialogResult.Cancel;
        }))
            Assert.True(v.Neu());
        Assert.Equal("Herr Brasse (ausgedacht)", v.Gewaehlt!.Name);
        using (new DialogErsatz(fenster =>
        {
            var p = Assert.IsType<PersonFenster>(fenster);
            p.ZuordnungAuswahl = _ => (birke, "Pflege FOSA");
            Assert.True(p.Zuordnen());
            Assert.Equal("Berta Birke (Pflege FOSA)", p.Zugeordnet.Items[0]);
            return DialogResult.OK;
        }))
            v.Oeffnen();
        Assert.Contains(u.Speicher.Zuordnungen(bewohnerId: birke), z => z.Rolle == "Pflege FOSA");

        using var termin = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today.AddDays(2)), u.Demo("Birke").Id);
        termin.Arzt.Text = "Dr. Pappel (ausgedacht)";
        Assert.Equal("Augenpraxis (ausgedacht)", termin.Ort.Text);
        Assert.StartsWith("Dr. Pappel (ausgedacht), Augenpraxis (ausgedacht), Tel. ", termin.Hinweis.Text);
    });
}
