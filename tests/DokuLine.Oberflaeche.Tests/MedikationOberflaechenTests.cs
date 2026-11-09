using System.ComponentModel;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Reiter „Medikamentenplan“ (Schritt 21): Tabelle „Was gilt heute“ mit Arzt und „seit“, Bedarf als
// eigene Tabelle, sortierbar; Klick auf eine Zeile öffnet Einzelheiten und Verlauf, dort Pause,
// Stufenplan, Absetzen; neue Verordnung; Ausdruck.
[Collection("Oberflaeche")]
public class MedikationOberflaechenTests
{
    private static (Umgebung, Hauptfenster, MedikationAnsicht) Oeffnen(string nachname, string kuerzel = "CD")
    {
        var u = new Umgebung();
        if (kuerzel.Length > 0) Einstellungen.Aendern(w => w with { Kuerzel = kuerzel });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BereichWaehlen("Dokumentation");
        f.Bewohner.AkteZeigen(u.Demo(nachname).Id!.Value);
        f.Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
        return (u, f, f.Bewohner.Akte.Medikation);
    }

    private static string Heute(int tage = 0) => Datum.Text(DateOnly.FromDateTime(DateTime.Today.AddDays(tage)));

    private static string Zelle(DataGridView t, string spalte, int zeile = 0) => PlanTabelle.Text(t, zeile, spalte);

    [Fact]
    public void Tabelle_mit_Arzt_seit_und_Status() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Ahorn");
        using (u)
        using (f)
        {
            Assert.Equal(["Stammdaten", "Medikamentenplan", "Wochenplan"], f.Bewohner.Akte.Reiter.Namen.Take(3));
            Assert.Equal(Heute(), m.TagFeld.Text);
            Assert.Equal("Demo — nicht zum Stellen verwenden", m.Hinweis.Text);
            Assert.Equal(["Medikament", "morgens", "mittags", "abends", "nachts", "verordnet von", "seit", "Status", "Hinweis"],
                m.Tabelle.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText));
            Assert.Equal(2, m.Tabelle.Rows.Count);
            Assert.False(m.BedarfTabelle.Visible);
            Assert.Equal("Keine Bedarfsmedikation.", m.BedarfLeer.Text);
            int q = m.Eintraege.FindIndex(s => s.Verordnung.Medikament.StartsWith("Quetiapin"));
            Assert.StartsWith("Quetiapin (Seroquel) · Filmtablette · 25 mg", Zelle(m.Tabelle, "medikament", q));
            Assert.Equal(["0", "0", "1", "0"], new[] { "morgens", "mittags", "abends", "nachts" }.Select(s => Zelle(m.Tabelle, s, q)));
            Assert.Equal("Dr. Ulme (ausgedacht)", Zelle(m.Tabelle, "arzt", q));
            Assert.Equal(Heute(-400), Zelle(m.Tabelle, "seit", q));
            Assert.Equal("aktiv", Zelle(m.Tabelle, "status", q));
            Assert.NotEqual(m.Tabelle.AlternatingRowsDefaultCellStyle.BackColor, m.Tabelle.DefaultCellStyle.BackColor);

            // Sortierbar: nach „seit“ (Datum, nicht Text) und nach Name; die Sortierung bleibt beim Neuladen.
            m.Tabelle.Sort(m.Tabelle.Columns["seit"]!, ListSortDirection.Ascending);
            Assert.StartsWith("Quetiapin", m.Eintraege[0].Verordnung.Medikament);   // seit 400 Tagen
            m.Tabelle.Sort(m.Tabelle.Columns["medikament"]!, ListSortDirection.Descending);
            Assert.StartsWith("Quetiapin", m.Eintraege[0].Verordnung.Medikament);
            m.Tabelle.Sort(m.Tabelle.Columns["medikament"]!, ListSortDirection.Ascending);
            Assert.StartsWith("Pantoprazol", m.Eintraege[0].Verordnung.Medikament);
            m.Aktualisieren();
            Assert.StartsWith("Pantoprazol", m.Eintraege[0].Verordnung.Medikament);
            Hilfe.Foto(f, "69_medikamentenplan");
        }
    });

    [Fact]
    public void Pausiertes_steht_sichtbar_da_und_gilt_danach_wieder() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Clematis");
        using (u)
        using (f)
        {
            Assert.Single(m.Eintraege);
            Assert.StartsWith("pausiert bis ", Zelle(m.Tabelle, "status"));
            // Eine Pause der letzten sieben Tage ist eine frische Änderung (29l): rot statt Achtung-Farbe.
            bool frisch = MedikationsAenderungen.ZeilenText(m.Eintraege[0].Verordnung, DateOnly.FromDateTime(DateTime.Now)) is not null;
            Assert.Equal(frisch ? Stil.MedikationGeaendert.Flaeche : Stil.Achtung.Flaeche, m.Tabelle.Rows[0].Cells["status"].Style.BackColor);
            m.TagFeld.Text = Heute(5);   // nach der Pause
            Assert.EndsWith("aktiv", Zelle(m.Tabelle, "status"));
            m.TagFeld.Text = "irgendwann";
            Assert.True(m.TagFehler.Visible);
            Assert.False(m.Tabelle.Visible);
            m.HeuteKnopf.PerformClick();
            Assert.Equal(Heute(), m.TagFeld.Text);
            Assert.True(m.Tabelle.Visible);
        }
    });

    [Fact]
    public void Stufenplan_Bedarf_und_Abgesetztes() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Birke");
        using (u)
        using (f)
        {
            Assert.Contains("Stufe 2 von 3 · nächste Stufe ab ", Zelle(m.Tabelle, "status"));   // davor ggf. die frische Änderung (29l)
            Assert.Equal("1", Zelle(m.Tabelle, "morgens"));
        }
        var (u2, f2, m2) = Oeffnen("Holunder");
        using (u2)
        using (f2)
        {
            Assert.Empty(m2.Eintraege);   // abgesetzt: nicht unter „Was gilt heute“
            Assert.True(m2.RegelLeer.Visible);
            m2.AbgesetzteZeigen.Checked = true;
            Assert.StartsWith("abgesetzt am ", Zelle(m2.Tabelle, "status"));
        }
        var (u3, f3, m3) = Oeffnen("Distel");
        using (u3)
        using (f3)
        {
            Assert.Empty(m3.Eintraege);
            var bedarf = Assert.Single(m3.BedarfEintraege);
            Assert.True(bedarf.Verordnung.Bedarf);
            Assert.Equal("bei starker Unruhe", Zelle(m3.BedarfTabelle, "anlass"));
            Assert.Equal("1", Zelle(m3.BedarfTabelle, "einzeldosis"));
            Assert.Equal("2 in 24 Stunden", Zelle(m3.BedarfTabelle, "hoechstens"));
            AenderungArt? wahl = null;
            using (new DialogErsatz(fenster =>
            {
                var d = Assert.IsType<VerordnungDetailFenster>(fenster);
                Assert.False(d.Waehlen(AenderungArt.Stufenplan));   // kein Stufenplan bei Bedarf
                Assert.False(d.Karte.Moeglich.Contains(AenderungArt.Stufenplan));
                wahl = d.Wunsch;
                return DialogResult.Cancel;
            }))
                m3.Oeffnen(bedarf);
            Assert.Null(wahl);
        }
    });

    [Fact]
    public void Neue_Verordnung_dann_Pause_und_Stufenplan_ueber_die_Zeile() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Ahorn");
        using (u)
        using (f)
        {
            int vorher = m.Eintraege.Count;
            string? fehler = null;
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<VerordnungFenster>(fenster);
                var namen = neu.MedikamentWahl.Items.Cast<string>().ToList();
                Assert.All(namen, n => Assert.EndsWith(" – ungeprüft", n));   // Demo-Bestand: alle ungeprüft
                neu.MedikamentWahl.SelectedIndex = namen.FindIndex(x => x.StartsWith("Citalopram"));
                Assert.Equal("20 mg", neu.Staerke.Text);   // erste Stärke aus der Datenbank
                neu.Mengen[0].Text = "1";
                Assert.False(neu.Speichern());
                fehler = neu.Fehler.Text;
                neu.Arzt.Text = "Dr. Test (ausgedacht)";
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                m.NeuKnopf.PerformClick();
            Assert.Contains("Wer hat es angeordnet?", fehler);
            Assert.Equal(vorher + 1, m.Eintraege.Count);
            Tagesstand Stand() => m.Eintraege.Single(s => s.Verordnung.Medikament.StartsWith("Citalopram"));
            int Zeile() => m.Eintraege.IndexOf(Stand());
            Assert.Equal($"neu seit {DateTime.Today:dd.MM.} · aktiv", Zelle(m.Tabelle, "status", Zeile()));   // frisch verordnet (29l)
            Assert.Equal("Dr. Test (ausgedacht)", Zelle(m.Tabelle, "arzt", Zeile()));

            // Klick auf die Zeile: Einzelheiten, dort „Pause …“ – danach öffnet sich die Pause.
            var titel = new List<string>();
            using (new DialogErsatz(fenster =>
            {
                titel.Add(fenster.GetType().Name);
                if (fenster is VerordnungDetailFenster d)
                {
                    Assert.Contains("Grundverordnung 1–0–0–0", d.Verlauf.Text);
                    Assert.True(d.Waehlen(AenderungArt.Pause));
                    return d.DialogResult;
                }
                var pause = Assert.IsType<AenderungFenster>(fenster);
                pause.Bis.Text = Heute(2);
                pause.Arzt.Text = "Dr. Test (ausgedacht)";
                return pause.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                m.Oeffnen(Stand());
            Assert.Equal(["VerordnungDetailFenster", "AenderungFenster"], titel);
            Assert.StartsWith("pausiert bis ", Zelle(m.Tabelle, "status", Zeile()));

            m.TagFeld.Text = Heute(3);
            using (new DialogErsatz(fenster =>
            {
                if (fenster is VerordnungDetailFenster d) return d.Waehlen(AenderungArt.Stufenplan) ? d.DialogResult : DialogResult.Cancel;
                var plan = Assert.IsType<AenderungFenster>(fenster);
                plan.StufeSetzen(0, Heute(3), "½", "0", "0", "0");
                plan.StufeSetzen(1, Heute(10), "0", "0", "0", "0");
                plan.Arzt.Text = "Dr. Test (ausgedacht)";
                return plan.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                m.Oeffnen(Stand());
            Assert.StartsWith("Stufe 1 von 2 · ", Zelle(m.Tabelle, "status", Zeile()));
            Assert.EndsWith("0–0–0–0", Zelle(m.Tabelle, "status", Zeile()));
            Assert.Equal("½", Zelle(m.Tabelle, "morgens", Zeile()));

            string? verlauf = null;
            using (new DialogErsatz(fenster =>
            {
                var d = Assert.IsType<VerordnungDetailFenster>(fenster);
                verlauf = d.Verlauf.Text;
                Hilfe.Foto(d, "72_medikamentenplan_einzelheiten");
                return DialogResult.Cancel;
            }))
                m.Oeffnen(Stand());
            Assert.Contains("Pause bis", verlauf);
            Assert.Contains("(absetzen)", verlauf);
            Assert.Contains("eingetragen von CD", verlauf);
        }
    });

    [Fact]
    public void Ausdruck_Was_gilt_heute() => Hilfe.Sta(() =>
    {
        var (u, f, m) = Oeffnen("Ahorn");
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                m.DruckKnopf.PerformClick();
                Assert.StartsWith("Medikamentenplan Anton Ahorn", Assert.Single(ersatz.Titel));
            }
            using var bild = new Bitmap(800, 1130);
            using var g = Graphics.FromImage(bild);
            new MedikationDruck(u.Demo("Ahorn"), DateOnly.FromDateTime(DateTime.Today), m.Staende(DateOnly.FromDateTime(DateTime.Today))).SeiteZeichnen(g);
        }
    });
}
