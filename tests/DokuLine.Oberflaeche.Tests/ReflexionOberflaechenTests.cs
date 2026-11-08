using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Reiter „Reflexion“: Monat, Lebensbereiche mit Berichten daneben, Zeichenzahl, Stilhinweise,
// Entwurf speichern, als fertig markieren (schreibgeschützt), Rückfrage beim Monatswechsel.
[Collection("Oberflaeche")]
public class ReflexionOberflaechenTests
{
    private static (Umgebung, Hauptfenster, ReflexionAnsicht) Oeffnen(string nachname)
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Reflexion");
        return (u, f, f.Bewohner.Akte.Reflexion);
    }

    private static DateOnly Vormonat => ReflexionRegeln.Vorschlag(DateOnly.FromDateTime(DateTime.Today));

    [Fact]
    public void Vormonat_mit_Quellen_und_Stilhinweisen() => Hilfe.Sta(() =>
    {
        var (u, f, r) = Oeffnen("Clematis");
        using (u)
        using (f)
        {
            Assert.True(r.Visible);
            Assert.Equal(Vormonat, r.Monat);
            Assert.Equal(ReflexionRegeln.MonatText(Vormonat), r.MonatText.Text);
            Assert.Equal("Entwurf", r.Status.Text);
            Assert.True(r.VorKnopf.Enabled);   // der laufende Monat liegt noch vorn
            Assert.Equal(5, r.Bereiche.Count);
            Assert.NotEmpty(r.Bereiche[0].StilHinweise);   // Semikolon in der Demo
            Assert.True(r.Bereiche[0].Hinweise.Visible);
            Assert.EndsWith("Zeichen", r.Bereiche[0].Zeichen.Text);
            Assert.Equal("Noch nichts geschrieben.", r.Bereiche[1].Zeichen.Text);
            Assert.All(r.Bereiche, b => Assert.StartsWith("BERICHTE IM MONAT", b.Controls.OfType<Label>().First(l => l.Text.StartsWith("BERICHTE")).Text));
            Assert.Contains(r.Bereiche.SelectMany(b => b.QuellenTexte), t => t == "Keine Berichte zu diesem Bereich im Monat." || t.Contains(" · "));
        }
    });

    [Fact]
    public void Entwurf_speichern_fertig_und_zurueck() => Hilfe.Sta(() =>
    {
        var (u, f, r) = Oeffnen("Ginster");
        using (u)
        using (f)
        {
            Assert.Equal("noch nicht begonnen", r.Status.Text);
            Assert.False(r.VerlaufKnopf.Enabled);
            r.Bereiche[3].Feld.Text = "Er nimmt regelmäßig an der Gartengruppe teil.";
            r.SpeichernKnopf.PerformClick();
            Assert.Equal("Entwurf", r.Status.Text);
            Assert.Equal("Entwurf gespeichert.", r.Meldung.Text);
            r.FertigKnopf.PerformClick();
            Assert.Equal("fertig", r.Status.Text);
            Assert.True(r.Bereiche[0].Feld.ReadOnly);
            Assert.False(r.SpeichernKnopf.Enabled);
            Assert.Equal("Wieder als Entwurf", r.FertigKnopf.Text);
            r.FertigKnopf.PerformClick();
            Assert.False(r.Bereiche[0].Feld.ReadOnly);
            Assert.Equal(3, u.Speicher.Reflexion(u.Demo("Ginster").Id!.Value, Vormonat).Fassungen.Count);
            using (var ersatz = new DialogErsatz(fenster =>
            {
                Assert.Contains("als fertig markiert von CD", Assert.IsType<TextFenster>(fenster).Inhalt.Text);
                return DialogResult.Cancel;
            }))
            {
                r.VerlaufKnopf.PerformClick();
                Assert.Single(ersatz.Titel);
            }
        }
    });

    [Fact]
    public void Monatswechsel_fragt_bei_ungespeicherten_Aenderungen() => Hilfe.Sta(() =>
    {
        var (u, f, r) = Oeffnen("Eiche");
        using (u)
        using (f)
        {
            r.Bereiche[2].Feld.Text = "Er hilft wöchentlich in der Küche mit.";
            var fragen = new List<string>();
            r.Rueckfrage = frage => { fragen.Add(frage); return DialogResult.Cancel; };
            r.ZurueckKnopf.PerformClick();
            Assert.Equal(Vormonat, r.Monat);   // abgebrochen: bleibt
            r.Rueckfrage = _ => DialogResult.Yes;
            r.ZurueckKnopf.PerformClick();
            Assert.Equal(Vormonat.AddMonths(-1), r.Monat);
            Assert.Equal("Er hilft wöchentlich in der Küche mit.", u.Speicher.Reflexion(u.Demo("Eiche").Id!.Value, Vormonat).Text(3));
            Assert.Single(fragen);
        }
    });
}
