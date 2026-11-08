using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

// Kalender: Monat/Woche/Tag, Filter, neuer Ausflug mit Teilnehmerliste, ändern, absagen, Verlauf,
// Termine im Wochenplan.
[Collection("Oberflaeche")]
public class KalenderOberflaechenTests
{
    private static (Umgebung, Hauptfenster, KalenderSeite) Oeffnen()
    {
        var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Navigation.Single(n => n.Text == "Kalender").Ausloesen();
        return (u, f, f.Kalender);
    }

    private static DateOnly Heute => DateOnly.FromDateTime(DateTime.Today);

    private static DateOnly Naechster(DayOfWeek tag)
    {
        int abstand = ((int)tag - (int)Heute.DayOfWeek + 7) % 7;
        return Heute.AddDays(abstand == 0 ? 7 : abstand);
    }

    [Fact]
    public void Monat_Woche_Tag_und_Filter() => Hilfe.Sta(() =>
    {
        var (u, f, k) = Oeffnen();
        using (u)
        using (f)
        {
            Assert.Same(k, f.Seite("Kalender"));
            Assert.Equal("Neuer Termin …", Assert.Single(f.Kopf.Knoepfe).Text);
            Assert.True(k.Ansichten[KalenderAnsicht.Monat].Aktiv);
            Assert.Contains(k.Monat.ZeilenAm(Heute), z => z.StartsWith("Heimfahrt"));   // Holunder ist gerade zu Hause
            k.Ansichten[KalenderAnsicht.Woche].Ausloesen();
            Assert.Contains(k.Woche.Termine, t => t.Titel.StartsWith("Heimfahrt"));
            k.TagWaehlen(Naechster(DayOfWeek.Thursday));
            k.AnsichtWaehlen(KalenderAnsicht.Tag);
            var arzt = k.Karten.Single(c => c.Termin.Aktuell.Titel == "Hausarzt: Kontrolle");
            Assert.Equal(["Kalender Bereich", "Termin", KalenderRegeln.ZeitText(arzt.Termin.Aktuell)], arzt.MarkenTexte);
            Assert.Contains("Bewohner: Anton Ahorn", arzt.AngabenText);
            Assert.False(arzt.ListeKnopf.Visible);

            k.FilterSetzen(u.Demo("Fichte").Id);
            Assert.DoesNotContain(k.Karten, c => c.Termin.Aktuell.Titel == "Hausarzt: Kontrolle");
            k.FilterSetzen(null);
            k.VorKnopf.PerformClick();
            Assert.Equal(Naechster(DayOfWeek.Thursday).AddDays(1), k.Stichtag);
        }
    });

    [Fact]
    public void Neuer_Ausflug_aendern_absagen() => Hilfe.Sta(() =>
    {
        var (u, f, k) = Oeffnen();
        using (u)
        using (f)
        {
            var tag = Heute.AddDays(5);
            using (new DialogErsatz(fenster =>
            {
                var neu = Assert.IsType<KalenderFenster>(fenster);
                neu.Arten[(int)KalenderArt.Ausflug].Checked = true;
                Assert.False(neu.Speichern());
                neu.Titel.Text = "Museum";
                neu.BeginnDatum.Text = Datum.Text(tag);
                neu.BeginnUhrzeit.Text = "10:00";
                neu.EndeUhrzeit.Text = "15:00";
                neu.BewohnerAnkreuzen(u.Demo("Ahorn").Id!.Value);
                neu.BewohnerAnkreuzen(u.Demo("Birke").Id!.Value);
                neu.Treffpunkt.Text = "Eingangshalle";
                neu.Begleitung.Text = "CD";
                return neu.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                f.Kopf.Knoepfe[0].PerformClick();
            k.TagWaehlen(tag);
            k.AnsichtWaehlen(KalenderAnsicht.Tag);
            KalenderKarte Karte() => k.Karten.Single(c => c.Termin.Aktuell.Titel == "Museum");
            Assert.Equal("Tagesausflug", Karte().MarkenTexte[1]);
            Assert.Contains("Teilnehmer: Anton Ahorn, Berta Birke", Karte().AngabenText);
            Assert.Contains("Treffpunkt: Eingangshalle", Karte().AngabenText);
            Assert.True(Karte().ListeKnopf.Visible);
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                Karte().ListeKnopf.PerformClick();
                Assert.StartsWith("Teilnehmerliste Museum", Assert.Single(ersatz.Titel));
            }

            using (new DialogErsatz(fenster =>
            {
                var aendern = Assert.IsType<KalenderFenster>(fenster);
                Assert.Equal("Museum", aendern.Titel.Text);
                aendern.BeginnUhrzeit.Text = "11:00";
                return aendern.Speichern() ? DialogResult.OK : DialogResult.Cancel;
            }))
                Karte().AendernKnopf.PerformClick();
            Assert.Contains("geändert", Karte().MarkenTexte);

            long id = Karte().Termin.Id;
            k.Rueckfrage = _ => DialogResult.Yes;
            Karte().AbsagenKnopf.PerformClick();
            Assert.DoesNotContain(k.Karten, c => c.Termin.Id == id);
            Assert.Contains("abgesagt von CD", string.Join("\n", KalenderRegeln.Verlauf(u.Speicher.KalenderTermin(id)!, new Dictionary<long, string>())));
        }
    });

    [Fact]
    public void Termine_erscheinen_im_Wochenplan() => Hilfe.Sta(() =>
    {
        var (u, f, k) = Oeffnen();
        using (u)
        using (f)
        {
            f.BereichWaehlen("Bewohner");
            f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
            f.Bewohner.Akte.Reiter.Waehlen("Wochenplan");
            var w = f.Bewohner.Akte.Wochenplan;
            w.WocheZeigen(WochenplanRegeln.Montag(Naechster(DayOfWeek.Thursday)));
            Assert.Contains(w.Raster.Termine, t => t.Quelle == TerminQuelle.Kalender && t.Titel == "Hausarzt: Kontrolle");
        }
    });

    [Fact]
    public void Ausdruck_Monat_und_Woche() => Hilfe.Sta(() =>
    {
        var (u, f, k) = Oeffnen();
        using (u)
        using (f)
        {
            using (var ersatz = new DialogErsatz(_ => DialogResult.Cancel))
            {
                k.DruckKnopf.PerformClick();
                Assert.StartsWith("Kalender ", Assert.Single(ersatz.Titel));
            }
            var namen = u.Speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => b.Name);
            using var bild = new Bitmap(1130, 800);
            using var g = Graphics.FromImage(bild);
            new KalenderDruck(KalenderAnsicht.Monat, Heute, k.Termine(), namen, null).SeiteZeichnen(g);
            new KalenderDruck(KalenderAnsicht.Woche, Heute, k.Termine(), namen, "Anton Ahorn").SeiteZeichnen(g);
        }
    });
}
