using System.Globalization;

namespace DokuLine.Kern;

// Qualität im Speicher (Bauplan 44): Prüfliste, Kennzahlen, Prüfvermerke (mit Verlauf, nie geändert) und Hinweise an den
// Wohnbereich (als Aufgabe des Bereichs). Prüfvermerk und Hinweis brauchen das Recht „Qualität prüfen“.
public sealed partial class Speicher
{
    // Alles für die Prüfung der Bewohner: Ziele und Verzichte in einem Zug, der Rest je Bewohner.
    public List<PruefDaten> PruefDatenLesen(IEnumerable<Bewohner> bewohner, DateOnly heute)
    {
        var w = Einstellungen.Laden();
        var ziele = Ziele().ToLookup(z => z.BewohnerId);
        var verzichte = Verzichte();
        var massnahmen = Massnahmen().ToLookup(m => m.BewohnerId);
        var tage = Math.Clamp(w.QmTage, 1, 90);
        var bestaetigungen = MassnahmeBestaetigungen(null, heute.AddDays(-tage), heute).ToLookup(b => b.BewohnerId);
        var vonBerichte = new[] { heute.AddDays(-7 * w.KeinBerichtWochen - 1), MassnahmenRegeln.QuartalBeginn(heute) }.Min();
        var liste = new List<PruefDaten>();
        foreach (var b in bewohner)
        {
            long id = b.Id!.Value;
            var berichte = Berichte(id, new BerichtFilter(vonBerichte, heute)).Where(x => x.Art != MassnahmenRegeln.ArtBericht).ToList();
            liste.Add(new PruefDaten(b, [.. ziele[id]], verzichte.GetValueOrDefault(id), [.. massnahmen[id]], [.. bestaetigungen[id]], berichte,
                Reflexion(id, ReflexionRegeln.Vorschlag(heute)), HebRegeln.Faellig(b, HebBoegen(id), heute, w.HebFristMonate), Angaben(id, heute)));
        }
        return liste;
    }

    public List<PruefZeile> Pruefliste(IEnumerable<Bewohner> bewohner, DateOnly heute)
    {
        var e = QualitaetRegeln.Einstellung(Einstellungen.Laden());
        var vermerke = Pruefvermerke().GroupBy(v => v.BewohnerId).ToDictionary(g => g.Key, g => g.Last());
        var hinweise = QmHinweise().GroupBy(h => h.BewohnerId).ToDictionary(g => g.Key, g => g.Last());
        return [.. PruefDatenLesen(bewohner, heute).Select(d => new PruefZeile(d.Bewohner, QualitaetRegeln.Pruefen(d, heute, e),
            vermerke.GetValueOrDefault(d.Bewohner.Id!.Value), hinweise.GetValueOrDefault(d.Bewohner.Id!.Value)))];
    }

    public List<QmKennzahl> QmKennzahlen(IEnumerable<Bewohner> bewohner, DateOnly heute) =>
        QualitaetRegeln.Kennzahlen(PruefDatenLesen(bewohner, heute), heute, QualitaetRegeln.Einstellung(Einstellungen.Laden()));

    public List<Pruefvermerk> Pruefvermerke(long? bewohnerId = null)
    {
        var liste = new List<Pruefvermerk>();
        using var befehl = Befehl("SELECT id, bewohner_id, zeit, kuerzel, bemerkung FROM qm_vermerke WHERE ($b IS NULL OR bewohner_id = $b) ORDER BY zeit, id", null,
            [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Pruefvermerk(leser.GetInt64(0), leser.GetInt64(1), AusZeit(leser.GetString(2)), leser.GetString(3), leser.GetString(4)));
        return liste;
    }

    public Pruefvermerk PruefvermerkSetzen(long bewohnerId, string bemerkung, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.Qualitaet) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        Ausfuehren("INSERT INTO qm_vermerke (bewohner_id, zeit, kuerzel, bemerkung) VALUES ($b, $z, $k, $t)", null,
            ("$b", bewohnerId), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()), ("$t", bemerkung.Trim()));
        return Pruefvermerke(bewohnerId).Last();
    }

    public List<QmHinweis> QmHinweise(long? bewohnerId = null)
    {
        var liste = new List<QmHinweis>();
        using var befehl = Befehl("""
            SELECT h.id, h.bewohner_id, h.aufgabe_id, h.text, h.gesendet, h.kuerzel, (SELECT MIN(e.zeit) FROM aufgabe_ereignisse e WHERE e.aufgabe_id = h.aufgabe_id AND e.art = 'erledigt')
            FROM qm_hinweise h WHERE ($b IS NULL OR h.bewohner_id = $b) ORDER BY h.gesendet, h.id
            """, null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new QmHinweis(leser.GetInt64(0), leser.GetInt64(1), leser.GetInt64(2), leser.GetString(3), AusZeit(leser.GetString(4)), leser.GetString(5),
                leser.IsDBNull(6) ? null : AusZeit(leser.GetString(6))));
        return liste;
    }

    // Eine Aufgabe für den Wohnbereich des Bewohners („wer Zeit hat“) und der Eintrag, damit die Prüfliste den Stand zeigt.
    public QmHinweis HinweisSenden(long bewohnerId, string text, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.Qualitaet) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is not Bewohner b) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (text.Trim().Length == 0) throw new Eingabefehler(["Bitte den Hinweis eintragen."]);
        using var transaktion = db.BeginTransaction();
        long aufgabe = AufgabeEinfuegen(new Aufgabe
        {
            BewohnerId = bewohnerId, Bereich = b.Wohnbereich, Text = text.Trim(), Fuer = AufgabeFuer.WerZeitHat, Erstellt = jetzt, Verfasser = kuerzel.Trim(),
        }, transaktion);
        Ausfuehren("INSERT INTO qm_hinweise (bewohner_id, aufgabe_id, text, gesendet, kuerzel) VALUES ($b, $a, $t, $g, $k)", transaktion,
            ("$b", bewohnerId), ("$a", aufgabe), ("$t", text.Trim()), ("$g", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        transaktion.Commit();
        AufgabenGeaendert?.Invoke();
        return QmHinweise(bewohnerId).Single(h => h.Id == id);
    }
}
