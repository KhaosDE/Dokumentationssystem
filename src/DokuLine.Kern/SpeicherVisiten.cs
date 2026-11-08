using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Visiten im Speicher (Bauplan Schritt 26). Eine Visite wird in einem Zug gespeichert: Visite,
// Einträge je Bewohner, Anordnungen, je Bewohner ein Bericht (Lebensbereich 5, mit Übergabe, wenn
// sich etwas ändert) und die Aufgaben (je Anordnung, je Facharzttermin). Nichts wird überschrieben.
public sealed partial class Speicher
{
    public event Action? VisitenGeaendert;

    public sealed record VisiteGespeichert(Visite Visite, IReadOnlyList<Aufgabe> Aufgaben);

    public VisiteGespeichert VisiteSpeichern(Visite v)
    {
        foreach (var e in v.Eintraege)
            if (Bewohner(e.BewohnerId) is null) throw new Eingabefehler(["Einen der Bewohner gibt es nicht mehr."]);
        // Frei eingetragen, steht aber schon in der Datenbank: gleich zuordnen.
        v = v with
        {
            Eintraege = v.Eintraege.Select(e => e with
            {
                Anordnungen = e.Anordnungen.Select(a => a.MedikamentId is null && VisitenMedikament(a) is Medikament m ? a with { MedikamentId = m.Id } : a).ToList(),
            }).ToList(),
        };
        var aufgabenIds = new List<long>();
        long id;
        using (var transaktion = db.BeginTransaction())
        {
            id = VisiteEinfuegen(v, transaktion, mitBerichtUndAufgaben: true, aufgabenIds, erledigt: false);
            transaktion.Commit();
        }
        VisitenGeaendert?.Invoke();
        AufgabenGeaendert?.Invoke();
        return new VisiteGespeichert(Visiten().Single(x => x.Id == id), aufgabenIds.Select(a => AufgabeLesen(a)!).ToList());
    }

    private long VisiteEinfuegen(Visite v, SqliteTransaction transaktion, bool mitBerichtUndAufgaben, List<long> aufgabenIds, bool erledigt)
    {
        Ausfuehren("INSERT INTO visiten (datum, art, arzt, teilnehmer, erstellt, kuerzel) VALUES ($d, $a, $r, $t, $e, $k)", transaktion,
            ("$d", Iso(v.Datum)), ("$a", v.Art), ("$r", v.Arzt), ("$t", v.Teilnehmer), ("$e", Zeit(v.Erstellt)), ("$k", v.Kuerzel));
        long id = Neu(transaktion);
        var schicht = SchichtTermin.Zu(v.Erstellt);
        var gespeichert = v with { Id = id };
        foreach (var e in v.Eintraege)
        {
            Ausfuehren("INSERT INTO visite_eintraege (visite_id, bewohner_id, besprochen, facharzt, dringend) VALUES ($v, $b, $t, $f, $d)", transaktion,
                ("$v", id), ("$b", e.BewohnerId), ("$t", e.Besprochen), ("$f", e.Facharzt), ("$d", e.Dringend ? 1 : 0));
            long eintragId = Neu(transaktion);
            var anordnungen = new List<VisiteAnordnung>();
            foreach (var a in e.Anordnungen)
            {
                Ausfuehren("INSERT INTO visite_anordnungen (eintrag_id, art, medikament_id, medikament) VALUES ($e, $a, $m, $t)", transaktion,
                    ("$e", eintragId), ("$a", VisitenRegeln.Kuerzel(a.Art)), ("$m", a.MedikamentId), ("$t", a.Medikament));
                anordnungen.Add(a with { Id = Neu(transaktion) });
            }
            if (!mitBerichtUndAufgaben) continue;
            var eintrag = e with { Id = eintragId, Anordnungen = anordnungen };
            // Bericht je Bewohner: Quelle für Monatsreflexion und HEB-Bogen (Lebensbereich 5).
            bool aenderung = anordnungen.Count > 0 || e.Facharzt.Length > 0;
            var zeitpunkt = v.Datum == DateOnly.FromDateTime(v.Erstellt) ? v.Erstellt : v.Datum.ToDateTime(new TimeOnly(12, 0));
            long berichtId = BerichtEinfuegen(new Bericht
            {
                BewohnerId = e.BewohnerId, Zeitpunkt = zeitpunkt, Schicht = BerichtRegeln.SchichtZu(zeitpunkt), Verfasser = v.Kuerzel,
                Bereiche = [5], Text = VisitenRegeln.BerichtText(gespeichert, eintrag), Erfasst = v.Erstellt,
            }, transaktion);
            if (aenderung && !erledigt)
                UebergabeEinfuegen(e.BewohnerId, berichtId, $"Visite: {(anordnungen.Count > 0 ? "Anordnungen geändert" : "Facharzttermin nötig")}",
                    v.Erstellt, v.Kuerzel, UebergabeRegeln.GiltBis(UebergabeDauer.NaechsteSchicht, v.Erstellt), transaktion);
            // Aufgaben: je geänderter Anordnung eine, je Facharzttermin eine – für die laufende Schicht.
            Aufgabe Neue(string text) => new()
            {
                BewohnerId = e.BewohnerId, BerichtId = berichtId, Text = text, Fuer = AufgabeFuer.Schicht, FuerSchicht = schicht,
                Erstellt = v.Erstellt, Verfasser = v.Kuerzel,
            };
            var neue = new List<long>();
            foreach (var a in anordnungen)
                neue.Add(AufgabeEinfuegen(Neue(VisitenRegeln.AufgabeText(gespeichert, a)) with { VisiteAnordnungId = a.Id }, transaktion));
            if (e.Facharzt.Length > 0)
                neue.Add(AufgabeEinfuegen(Neue(VisitenRegeln.FacharztText(gespeichert, eintrag)) with { VisiteEintragId = eintragId }, transaktion));
            if (erledigt)   // Demo: schon erledigt
                foreach (var a in neue)
                    Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel) VALUES ($a, 'erledigt', $z, $k)", transaktion,
                        ("$a", a), ("$z", Zeit(v.Erstellt.AddHours(3))), ("$k", "EF"));
            aufgabenIds.AddRange(neue);
        }
        return id;
    }

    private long Neu(SqliteTransaction transaktion) => Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);

    // Alle Visiten (neueste zuerst), optional nur eines Bewohners und in einem Zeitraum.
    public List<Visite> Visiten(long? bewohnerId = null, DateOnly? von = null, DateOnly? bis = null)
    {
        var anordnungen = new Dictionary<long, List<VisiteAnordnung>>();
        using (var befehl = Befehl("SELECT id, eintrag_id, art, medikament_id, medikament FROM visite_anordnungen ORDER BY id", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!anordnungen.TryGetValue(leser.GetInt64(1), out var l)) anordnungen[leser.GetInt64(1)] = l = [];
                l.Add(new VisiteAnordnung(leser.GetInt64(0), VisitenRegeln.ArtAus(leser.GetString(2)), leser.IsDBNull(3) ? null : leser.GetInt64(3), leser.GetString(4)));
            }
        var eintraege = new Dictionary<long, List<VisiteEintrag>>();
        using (var befehl = Befehl("SELECT id, visite_id, bewohner_id, besprochen, facharzt, dringend FROM visite_eintraege ORDER BY id", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!eintraege.TryGetValue(leser.GetInt64(1), out var l)) eintraege[leser.GetInt64(1)] = l = [];
                l.Add(new VisiteEintrag
                {
                    Id = leser.GetInt64(0), BewohnerId = leser.GetInt64(2), Besprochen = leser.GetString(3), Facharzt = leser.GetString(4),
                    Dringend = leser.GetInt64(5) == 1, Anordnungen = anordnungen.GetValueOrDefault(leser.GetInt64(0)) ?? [],
                });
            }
        var liste = new List<Visite>();
        using (var befehl = Befehl("SELECT id, datum, art, arzt, teilnehmer, erstellt, kuerzel FROM visiten ORDER BY datum DESC, id DESC", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                liste.Add(new Visite
                {
                    Id = leser.GetInt64(0), Datum = AusIso(leser.GetString(1)), Art = leser.GetString(2), Arzt = leser.GetString(3),
                    Teilnehmer = leser.GetString(4), Erstellt = AusZeit(leser.GetString(5)), Kuerzel = leser.GetString(6),
                    Eintraege = eintraege.GetValueOrDefault(leser.GetInt64(0)) ?? [],
                });
        return liste.Where(v => (bewohnerId is not long b || v.Eintraege.Any(e => e.BewohnerId == b))
                                && (von is not DateOnly vo || v.Datum >= vo) && (bis is not DateOnly bi || v.Datum <= bi)).ToList();
    }

    // Zu einer Aufgabe aus einer Visite: was zu tun ist, die Visite, der Eintrag und ggf. die Anordnung.
    public (VisitenAktion Aktion, Visite Visite, VisiteEintrag Eintrag, VisiteAnordnung? Anordnung)? VisitenAufgabe(Aufgabe a)
    {
        if (a.VisiteAnordnungId is null && a.VisiteEintragId is null) return null;
        foreach (var v in Visiten())
            foreach (var e in v.Eintraege)
            {
                if (a.VisiteEintragId == e.Id) return (VisitenAktion.FacharztTermin, v, e, null);
                if (e.Anordnungen.FirstOrDefault(x => x.Id == a.VisiteAnordnungId) is VisiteAnordnung an)
                    return (VisitenMedikament(an) is null ? VisitenAktion.MedikamentAnlegen : VisitenAktion.MedikationAendern, v, e, an);
            }
        return null;
    }

    // Das Medikament einer Anordnung: aus der Datenbank gewählt oder – bei Freitext – inzwischen
    // angelegt (gleicher Wirkstoff oder Handelsname, Groß-/Kleinschreibung egal).
    public Medikament? VisitenMedikament(VisiteAnordnung a)
    {
        var alle = Medikamente();
        if (a.MedikamentId is long id) return alle.FirstOrDefault(m => m.Id == id);
        bool Gleich(string s) => s.Equals(a.Medikament, StringComparison.CurrentCultureIgnoreCase);
        return alle.FirstOrDefault(m => Gleich(m.Wirkstoff) || m.Aktuell.Handelsnamen.Any(Gleich));
    }

    // Facharzttermin vereinbart (Aufgabe erledigen): Termin im Kalender (Bereich) beim Bewohner, auf
    // Wunsch eine Fahrt, dann ist die Aufgabe erledigt.
    public KalenderTermin FacharztTerminEintragen(Aufgabe a, DateOnly datum, string uhrzeit, string ort, string kuerzel, DateTime jetzt, Fahrt? fahrt = null)
    {
        if (VisitenAufgabe(a) is not { Aktion: VisitenAktion.FacharztTermin } info) throw new Eingabefehler(["Das ist keine Aufgabe für einen Facharzttermin."]);
        var f = KalenderRegeln.AusEingabe(new KalenderEingabe
        {
            Art = KalenderArt.Bewohner, Titel = $"Facharzt: {info.Eintrag.Facharzt}", BeginnDatum = Kern.Datum.Text(datum), BeginnUhrzeit = uhrzeit,
            Ort = ort, Hinweis = $"vereinbart nach der {VisitenRegeln.Kopf(info.Visite)}", Bewohner = [info.Eintrag.BewohnerId], Kuerzel = kuerzel,
        }, jetzt);
        var termin = KalenderAnlegen(f);
        if (fahrt is not null) FahrtAnlegen(fahrt);
        AufgabeErledigt(a.Id, kuerzel, jetzt);
        return termin;
    }

    // Demo: drei Visiten (Dosis eines vorhandenen Medikaments, ein neues Medikament, ein
    // Facharzttermin) mit Berichten; ihre Aufgaben sind in der Demo schon erledigt.
    private void DemoVisitenEinfuegen(SqliteTransaction transaktion)
    {
        // Aufgaben aus früheren Demo-Visiten zuerst (sie verweisen auf die Visiten).
        Ausfuehren("DELETE FROM aufgaben WHERE visite_anordnung_id IS NOT NULL OR visite_eintrag_id IS NOT NULL; DELETE FROM visiten", transaktion);
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        var medikamente = MedikamenteLesen(transaktion, "");
        foreach (var v in Demodaten.Visiten(bewohner, medikamente, Uhr()))
            VisiteEinfuegen(v, transaktion, mitBerichtUndAufgaben: true, [], erledigt: true);
        SetzeMeta(DemoVisiten, "1", transaktion);
    }
}
