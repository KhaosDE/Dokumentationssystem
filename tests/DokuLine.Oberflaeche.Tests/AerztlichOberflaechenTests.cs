using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26f: Fenster „Ärztliche Informationen“ mit Diagnosen (nie löschen, nur beenden),
// Allergien mit Art der Reaktion und Hinweisen für den Notfall; Rechte „ansehen“ und „bearbeiten“;
// Visite und HEB-Bogen zeigen die Diagnosen nur als Information.
[Collection("Oberflaeche")]
public class AerztlichOberflaechenTests
{
    [Fact]
    public void Diagnosen_Allergien_und_Notfall_bearbeiten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });   // Fachkraft
        long ahorn = u.Demo("Ahorn").Id!.Value;
        var heute = DateOnly.FromDateTime(DateTime.Now);
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(ahorn);
        Assert.Null(Stil.Sperrgrund(f.Bewohner.AerztlichKnopf));
        string? icdFehler = null;
        using (new DialogErsatz(fenster =>
        {
            switch (fenster)
            {
                case DiagnoseFenster d:
                    d.Icd.Text = "X1";
                    d.Bezeichnung.Text = "Generalisierte Angststörung";
                    Assert.False(d.Speichern());
                    icdFehler = d.Fehler.Text;
                    d.Icd.Text = "F41.1";
                    d.Neben.Checked = true;
                    d.Seit.Text = "01.03.2024";
                    d.Arzt.Text = "Dr. Linde (ausgedacht)";
                    Hilfe.Foto(d, "107_neue_diagnose");
                    return d.Speichern() ? DialogResult.OK : DialogResult.Cancel;
                case DatumFenster b:
                    Assert.StartsWith("Diagnose beenden – E11.9", b.Text);
                    return b.Speichern() ? DialogResult.OK : DialogResult.Cancel;   // Vorschlag: heute
                case AerztlichFenster a:
                    Hilfe.Zeigen(a);
                    Assert.Equal(["F20.0", "E11.9"], a.Diagnosen.Items.Cast<ListViewItem>().Select(i => i.Text));
                    Assert.Equal("Hauptdiagnose", a.Diagnosen.Items[0].SubItems[2].Text);
                    Assert.Equal("Dr. Linde (ausgedacht)", a.Diagnosen.Items[0].SubItems[4].Text);
                    Assert.Equal("", a.NurAnsehen.Text);
                    Assert.True(a.Neu());
                    Assert.Equal(["F20.0", "E11.9", "F41.1"], a.Diagnosen.Items.Cast<ListViewItem>().Select(i => i.Text));
                    a.DiagnoseWaehlen(1);
                    Assert.True(a.Beenden());
                    Assert.Equal(["F20.0", "F41.1", "E11.9"], a.Diagnosen.Items.Cast<ListViewItem>().Select(i => i.Text));   // beendet: unten, bleibt sichtbar
                    Assert.Equal($"beendet am {Datum.Text(heute)}", a.Diagnosen.Items[2].SubItems[5].Text);
                    a.DiagnoseWaehlen(2);
                    Assert.False(a.Beenden());   // schon beendet

                    Assert.Equal("eingetragen", a.Allergie.Zustand);
                    Assert.Equal("Haselnüsse", a.Allergie.Feld.Text);
                    Assert.Equal("Schwellung im Mund, Atemnot möglich", a.ReaktionenLesen()["Haselnüsse"]);
                    a.Allergie.Feld.Text = "Haselnüsse, Latex";
                    Assert.True(a.AllergienSpeichern());
                    Assert.Equal(2, a.Reaktionen.Rows.Count);
                    a.Reaktionen.Rows[1].Cells[1].Value = "Hautausschlag";
                    Assert.True(a.AllergienSpeichern());
                    a.Notfall.Text = "Bei Atemnot sofort Notruf 112 (ausgedacht).";
                    Assert.True(a.NotfallSpeichern());
                    Hilfe.Foto(a, "106_aerztliche_informationen");
                    return DialogResult.Cancel;
                default:
                    throw new InvalidOperationException(fenster.Text);
            }
        }))
            f.Bewohner.AerztlichKnopf.PerformClick();
        Assert.Contains("ICD-10", icdFehler);
        var diagnosen = u.Speicher.Diagnosen(ahorn);
        var neu = diagnosen.Single(d => d.Icd == "F41.1");
        Assert.Equal((DiagnoseArt.Neben, "CD", new DateOnly(2024, 3, 1)), (neu.Art, neu.Kuerzel, neu.Seit!.Value));
        Assert.Equal((heute, "CD"), (diagnosen.Single(d => d.Icd == "E11.9").Beendet!.Value, diagnosen.Single(d => d.Icd == "E11.9").BeendetKuerzel));
        Assert.Equal("F20.0 Paranoide Schizophrenie · F41.1 Generalisierte Angststörung", DiagnoseRegeln.Kurz(diagnosen));
        Assert.Equal("Hautausschlag", u.Speicher.Reaktionen(ahorn)["Latex"]);
        Assert.Equal("Bei Atemnot sofort Notruf 112 (ausgedacht).", u.Speicher.Notfallhinweise(ahorn));
        Assert.Equal("Allergien: Haselnüsse, Latex", f.Bewohner.Akte.Allergie.Text);   // Hinweis oben in der Akte ist neu geladen
    });

    [Fact]
    public void Ohne_Recht_nur_ansehen_oder_gesperrt() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        var ahorn = u.Demo("Ahorn");
        Einstellungen.Aendern(w => w with { Kuerzel = "KL" });   // keine Fachkraft: ansehen ja, bearbeiten nein
        using (var a = new AerztlichFenster(u.Speicher, ahorn))
        {
            Assert.Equal($"Nur ansehen: {RechteRegeln.NurFachkraftText}", a.NurAnsehen.Text);
            Assert.All(new[] { a.NeuKnopf, a.BeendenKnopf, a.AllergieKnopf, a.NotfallKnopf }, k => Assert.Equal(RechteRegeln.NurFachkraftText, Stil.Sperrgrund(k)));
            Assert.True(a.Notfall.ReadOnly);
            Assert.False(a.Neu());
            a.Allergie.Setzen("keine_bekannt", "");
            Assert.False(a.AllergienSpeichern());
            Assert.Equal(2, a.Diagnosen.Items.Count);   // ansehen geht
        }
        Assert.Equal(AllergieZustand.Eingetragen, u.Speicher.Bewohner(ahorn.Id!.Value)!.Allergie);

        Einstellungen.Aendern(w => w with { Kuerzel = "WZ" });   // Verwaltung: nicht einmal ansehen
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(ahorn.Id!.Value);
        Assert.Equal(RechteRegeln.FehltText, Stil.Sperrgrund(f.Bewohner.AerztlichKnopf));
        Assert.False(f.Bewohner.AerztlichOeffnen());
        Assert.Equal("Allergien: Haselnüsse", f.Bewohner.Akte.Allergie.Text);   // der Allergiehinweis bleibt für alle sichtbar
    });

    [Fact]
    public void Visite_und_HEB_zeigen_Diagnosen_nur_mit_Recht() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        var kiefer = u.Demo("Kiefer");
        var bogen = u.Speicher.HebBoegen(kiefer.Id!.Value)[0];
        var entwurf = new VisitenEntwurf { Bereich = "R1", Datum = DateOnly.FromDateTime(DateTime.Now), Arzt = "Dr. Linde (ausgedacht)", Kuerzel = "EF", Begonnen = DateTime.Now };
        foreach (var (kuerzel, sichtbar) in new[] { ("EF", true), ("WZ", false) })
        {
            Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
            using (var r = new VisitenRundgang(u.Speicher, entwurf))
            {
                r.Waehlen(r.Bewohner.FindIndex(b => b.Nachname == "Ahorn"));
                Assert.Equal("Allergien: Haselnüsse", r.Allergie.Text);   // nicht doppelt „Allergien: Allergien:“ (Fehler aus 0.29.5)
                Assert.Equal(sichtbar ? "Diagnosen: F20.0 Paranoide Schizophrenie · E11.9 Diabetes mellitus Typ 2" : "", r.Diagnosen.Text);
            }
            using var h = new HebFenster(u.Speicher, kiefer, bogen);
            Assert.Equal(sichtbar, h.Kopf.Text.Contains("Diagnosen: F20.0 Paranoide Schizophrenie", StringComparison.Ordinal));
        }
    });
}
