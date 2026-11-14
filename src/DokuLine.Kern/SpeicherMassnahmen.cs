using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Maßnahmen speichern (Bauplan 39): Fassungen ab Datum (nie überschrieben), Beenden mit Grund, Bestätigungen je Dienst
// (höchstens eine je Maßnahme und Dienst; „bei Bedarf“ beliebig oft). Jede Bestätigung ist zugleich ein kurzer Eintrag in
// der Dokumentation (Berichtsart „Maßnahme“) im Lebensbereich der Maßnahme.
public sealed partial class Speicher
{
    private const string DemoMassnahmen = "demo_massnahmen";

    public event Action? MassnahmenGeaendert;

    public List<Massnahme> Massnahmen(long? bewohnerId = null, bool auchBeendete = false)
    {
        var fassungen = new Dictionary<long, List<MassnahmeFassung>>();
        using (var befehl = Befehl("""
            SELECT f.massnahme_id, f.gilt_ab, f.gilt_bis, f.titel, f.beschreibung, f.lebensbereich, f.ziel_id, f.art, f.rhythmus, f.wochentage,
                   f.alle_wochen, f.monatstag, f.schicht, f.uhrzeit, f.minuten, f.kuerzel, f.erfasst
            FROM massnahme_fassungen f JOIN massnahmen m ON m.id = f.massnahme_id
            WHERE ($b IS NULL OR m.bewohner_id = $b) ORDER BY f.gilt_ab, f.id
            """, null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                long id = leser.GetInt64(0);
                if (!fassungen.TryGetValue(id, out var liste)) fassungen[id] = liste = [];
                liste.Add(new MassnahmeFassung
                {
                    GiltAb = AusIso(leser.GetString(1)), GiltBis = leser.IsDBNull(2) ? null : AusIso(leser.GetString(2)), Titel = leser.GetString(3),
                    Beschreibung = leser.GetString(4), Lebensbereich = leser.GetInt32(5), ZielId = leser.IsDBNull(6) ? null : leser.GetInt64(6),
                    Art = leser.GetString(7), Rhythmus = Enum.Parse<MassnahmeRhythmus>(leser.GetString(8), true),
                    Wochentage = [.. leser.GetString(9).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => (DayOfWeek)int.Parse(t, CultureInfo.InvariantCulture))],
                    AlleWochen = leser.GetInt32(10), MonatsTag = leser.GetInt32(11), Schicht = BerichtRegeln.SchichtAus(leser.GetString(12)),
                    Uhrzeit = leser.IsDBNull(13) ? null : TimeOnly.ParseExact(leser.GetString(13), "HH:mm", CultureInfo.InvariantCulture),
                    Minuten = leser.GetInt32(14), Kuerzel = leser.GetString(15), Erfasst = AusZeit(leser.GetString(16)),
                });
            }
        var massnahmen = new List<Massnahme>();
        using (var befehl = Befehl("SELECT id, bewohner_id, beendet_am, beendet_grund, beendet_von FROM massnahmen WHERE ($b IS NULL OR bewohner_id = $b) ORDER BY id",
                   null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                if (fassungen.TryGetValue(leser.GetInt64(0), out var f))
                    massnahmen.Add(new Massnahme(leser.GetInt64(0), leser.GetInt64(1), f, leser.IsDBNull(2) ? null : AusIso(leser.GetString(2)),
                        leser.GetString(3), leser.GetString(4)));
        return auchBeendete ? massnahmen : [.. massnahmen.Where(m => m.BeendetAm is null)];
    }

    // Anlegen und Ändern: Fachkräfte (Recht „Maßnahmen anlegen und ändern“) und die Bezugspflege des Bewohners.
    public string? MassnahmenAnlegenGrund(string kuerzel, long bewohnerId)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) return RechteRegeln.NichtAngemeldetText;
        if (Grund(kuerzel, Funktionen.MassnahmenAnlegen) is not string grund) return null;
        bool bezugspflege = Bezug(bewohnerId).Bezugspflege?.Kuerzel.Equals(kuerzel.Trim(), StringComparison.OrdinalIgnoreCase) == true;
        return bezugspflege && Grund(kuerzel, Funktionen.MassnahmenBestaetigen) is null ? null : grund;
    }

    public Massnahme MassnahmeAnlegen(long bewohnerId, MassnahmeFassung f, string kuerzel, DateTime jetzt)
    {
        if (MassnahmenAnlegenGrund(kuerzel, bewohnerId) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (MassnahmenRegeln.Pruefen(f) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT INTO massnahmen (bewohner_id, erstellt, kuerzel) VALUES ($b, $e, $k)", transaktion,
            ("$b", bewohnerId), ("$e", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        FassungEinfuegen(id, f with { Kuerzel = kuerzel.Trim(), Erfasst = jetzt }, transaktion);
        transaktion.Commit();
        MassnahmenGeaendert?.Invoke();
        return Massnahmen(bewohnerId).Single(m => m.Id == id);
    }

    // Ändern: eine neue Fassung ab Datum; frühere Fassungen und Bestätigungen bleiben.
    public void MassnahmeAendern(long id, MassnahmeFassung f, string kuerzel, DateTime jetzt)
    {
        var m = Massnahmen(auchBeendete: true).FirstOrDefault(x => x.Id == id) ?? throw new Eingabefehler(["Diese Maßnahme gibt es nicht mehr."]);
        if (MassnahmenAnlegenGrund(kuerzel, m.BewohnerId) is string grund) throw new Eingabefehler([grund]);
        if (m.BeendetAm is not null) throw new Eingabefehler(["Die Maßnahme ist beendet – bitte eine neue anlegen."]);
        if (MassnahmenRegeln.Pruefen(f) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        using var transaktion = db.BeginTransaction();
        FassungEinfuegen(id, f with { Kuerzel = kuerzel.Trim(), Erfasst = jetzt }, transaktion);
        transaktion.Commit();
        MassnahmenGeaendert?.Invoke();
    }

    public void MassnahmeBeenden(long id, string grund, string kuerzel, DateOnly ab)
    {
        var m = Massnahmen(auchBeendete: true).FirstOrDefault(x => x.Id == id) ?? throw new Eingabefehler(["Diese Maßnahme gibt es nicht mehr."]);
        if (MassnahmenAnlegenGrund(kuerzel, m.BewohnerId) is string fehlt) throw new Eingabefehler([fehlt]);
        if (grund.Trim().Length == 0) throw new Eingabefehler(["Bitte den Grund für das Beenden angeben."]);
        Ausfuehren("UPDATE massnahmen SET beendet_am = $a, beendet_grund = $g, beendet_von = $k WHERE id = $i AND beendet_am IS NULL", null,
            ("$a", Iso(ab)), ("$g", grund.Trim()), ("$k", kuerzel.Trim()), ("$i", id));
        MassnahmenGeaendert?.Invoke();
    }

    private void FassungEinfuegen(long massnahmeId, MassnahmeFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO massnahme_fassungen (massnahme_id, gilt_ab, gilt_bis, titel, beschreibung, lebensbereich, ziel_id, art, rhythmus, wochentage,
                alle_wochen, monatstag, schicht, uhrzeit, minuten, kuerzel, erfasst)
            VALUES ($m, $ab, $bis, $t, $be, $lb, $z, $art, $r, $w, $n, $mt, $s, $u, $min, $k, $e)
            """, transaktion, ("$m", massnahmeId), ("$ab", Iso(f.GiltAb)), ("$bis", f.GiltBis is DateOnly b ? Iso(b) : null), ("$t", f.Titel.Trim()),
            ("$be", f.Beschreibung.Trim()), ("$lb", f.Lebensbereich), ("$z", f.ZielId), ("$art", f.Art.Trim()), ("$r", f.Rhythmus.ToString().ToLowerInvariant()),
            ("$w", string.Join(",", f.Wochentage.Select(t => ((int)t).ToString(CultureInfo.InvariantCulture)))), ("$n", f.AlleWochen), ("$mt", f.MonatsTag),
            ("$s", BerichtRegeln.Kuerzel(f.Schicht)), ("$u", f.Uhrzeit?.ToString("HH:mm", CultureInfo.InvariantCulture)), ("$min", f.Minuten),
            ("$k", f.Kuerzel), ("$e", Zeit(f.Erfasst)));

    public List<MassnahmeBestaetigung> MassnahmeBestaetigungen(long? bewohnerId = null, DateOnly? von = null, DateOnly? bis = null)
    {
        var liste = new List<MassnahmeBestaetigung>();
        using var befehl = Befehl("""
            SELECT id, massnahme_id, bewohner_id, tag, schicht, ergebnis, grund, minuten, bemerkung, kuerzel, zeit, lebensbereich, titel, bericht_id
            FROM massnahme_bestaetigungen WHERE ($b IS NULL OR bewohner_id = $b) AND ($von IS NULL OR tag >= $von) AND ($bis IS NULL OR tag <= $bis)
            ORDER BY zeit
            """, null, [("$b", bewohnerId), ("$von", von is DateOnly v ? Iso(v) : null), ("$bis", bis is DateOnly x ? Iso(x) : null)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new MassnahmeBestaetigung(leser.GetInt64(0), leser.GetInt64(1), leser.GetInt64(2), AusIso(leser.GetString(3)),
                BerichtRegeln.SchichtAus(leser.GetString(4)), Enum.Parse<MassnahmeErgebnis>(leser.GetString(5), true), leser.GetString(6), leser.GetInt32(7),
                leser.GetString(8), leser.GetString(9), AusZeit(leser.GetString(10)), leser.GetInt32(11), leser.GetString(12),
                leser.IsDBNull(13) ? null : leser.GetInt64(13)));
        return liste;
    }

    // Die fälligen Maßnahmen im Dienst (für die Bewohner der Liste; leer = alle), mit den schon gesetzten Bestätigungen.
    public List<MassnahmeTermin> MassnahmenImDienst(SchichtTermin dienst, IReadOnlyCollection<long>? bewohner = null)
    {
        var alle = Massnahmen().Where(m => bewohner is null || bewohner.Contains(m.BewohnerId));
        return MassnahmenRegeln.Termine(alle, dienst, MassnahmeBestaetigungen(von: dienst.Tag, bis: dienst.Tag));
    }

    // Bestätigen: durchgeführt · abgelehnt · nicht möglich, mit Zeit; dazu der kurze Eintrag in der Dokumentation.
    // „bei Bedarf“ geht jederzeit, alles andere nur, wenn es in diesem Dienst fällig ist (und nur einmal).
    public MassnahmeBestaetigung MassnahmeBestaetigen(long massnahmeId, SchichtTermin dienst, MassnahmeErgebnis ergebnis, string grund, int minuten,
        string bemerkung, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.MassnahmenBestaetigen);
        var m = Massnahmen().FirstOrDefault(x => x.Id == massnahmeId) ?? throw new Eingabefehler(["Diese Maßnahme gibt es nicht mehr oder sie ist beendet."]);
        var f = m.Laeuft(dienst.Tag) ? m.Am(dienst.Tag) : null;
        if (f is null) throw new Eingabefehler(["Die Maßnahme gilt an diesem Tag nicht."]);
        bool bedarf = f.Rhythmus == MassnahmeRhythmus.BeiBedarf;
        if (!bedarf && (f.Schicht != dienst.Schicht || !MassnahmenRegeln.AmTag(f, dienst.Tag)))
            throw new Eingabefehler([$"Die Maßnahme ist im {dienst.Text} nicht fällig."]);
        if (MassnahmenRegeln.BestaetigungPruefen(ergebnis, grund, minuten) is string fehler) throw new Eingabefehler([fehler]);
        using var transaktion = db.BeginTransaction();
        if (!bedarf && WertIn(transaktion, "SELECT kuerzel || ' um ' || substr(zeit, 12, 5) FROM massnahme_bestaetigungen WHERE massnahme_id = $m AND tag = $t AND schicht = $s AND bedarf = 0",
                ("$m", m.Id), ("$t", Iso(dienst.Tag)), ("$s", BerichtRegeln.Kuerzel(dienst.Schicht))) is string schon)
            throw new Eingabefehler([$"Schon bestätigt ({schon} Uhr)."]);
        long bericht = BerichtEinfuegen(new Bericht
        {
            BewohnerId = m.BewohnerId, Zeitpunkt = jetzt, Schicht = dienst.Schicht, Verfasser = kuerzel.Trim(), Bereiche = [f.Lebensbereich],
            Text = MassnahmenRegeln.BerichtText(f.Titel, ergebnis, grund, minuten, bemerkung), Erfasst = jetzt, Art = MassnahmenRegeln.ArtBericht,
        }, transaktion);
        long id = BestaetigungEinfuegen(m, f, dienst, bedarf, ergebnis, grund, ergebnis == MassnahmeErgebnis.Durchgefuehrt ? minuten : 0, bemerkung,
            kuerzel.Trim(), jetzt, bericht, transaktion);
        transaktion.Commit();
        MassnahmenGeaendert?.Invoke();
        return MassnahmeBestaetigungen(m.BewohnerId, dienst.Tag, dienst.Tag).Single(b => b.Id == id);
    }

    private long BestaetigungEinfuegen(Massnahme m, MassnahmeFassung f, SchichtTermin dienst, bool bedarf, MassnahmeErgebnis ergebnis, string grund, int minuten,
        string bemerkung, string kuerzel, DateTime zeit, long? bericht, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO massnahme_bestaetigungen (massnahme_id, bewohner_id, tag, schicht, bedarf, ergebnis, grund, minuten, bemerkung, kuerzel, zeit,
                lebensbereich, titel, bericht_id)
            VALUES ($m, $b, $t, $s, $bed, $e, $g, $min, $bem, $k, $z, $lb, $ti, $r)
            """, transaktion, ("$m", m.Id), ("$b", m.BewohnerId), ("$t", Iso(dienst.Tag)), ("$s", BerichtRegeln.Kuerzel(dienst.Schicht)), ("$bed", bedarf ? 1 : 0),
            ("$e", ergebnis.ToString().ToLowerInvariant()), ("$g", grund.Trim()), ("$min", minuten), ("$bem", bemerkung.Trim()), ("$k", kuerzel), ("$z", Zeit(zeit)),
            ("$lb", f.Lebensbereich), ("$ti", f.Titel), ("$r", bericht));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // Nach dem Dienstwechsel einmal: Was im vorigen Dienst offen blieb, geht als Sammeleintrag je Wohnbereich in die
    // Übergabe (bis zum Ende des nächsten Dienstes). Der Eintrag in „meta“ sorgt dafür, dass es auch an mehreren Rechnern
    // nur einmal geschieht. Rückgabe: Zahl der Einträge.
    public int MassnahmenUebergabeErzeugen(DateTime jetzt)
    {
        var vorher = SchichtTermin.Zu(SchichtTermin.Zu(jetzt).Beginn.AddMinutes(-1));
        string schluessel = $"massnahmen_uebergabe:{Iso(vorher.Tag)}:{BerichtRegeln.Kuerzel(vorher.Schicht)}";
        if (Meta(schluessel) is not null) return 0;
        var offen = MassnahmenImDienst(vorher).Where(t => t.Offen).ToList();
        var bewohner = AlleBewohner().ToDictionary(b => b.Id!.Value);
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT OR IGNORE INTO meta (schluessel, wert) VALUES ($s, '1')", transaktion, ("$s", schluessel));
        if (Convert.ToInt32(WertIn(transaktion, "SELECT changes()"), CultureInfo.InvariantCulture) == 0) return 0;   // ein anderer Rechner war schneller
        int neu = 0;
        foreach (var gruppe in offen.Where(t => bewohner.ContainsKey(t.Massnahme.BewohnerId)).GroupBy(t => bewohner[t.Massnahme.BewohnerId].Wohnbereich).OrderBy(g => g.Key))
        {
            string text = MassnahmenRegeln.UebergabeText(gruppe.Key, vorher, [.. gruppe.Select(t => (bewohner[t.Massnahme.BewohnerId].Name, t.Fassung.Titel))]);
            UebergabeEinfuegen(null, null, text, jetzt, MassnahmenRegeln.UebergabeVerfasser,
                UebergabeRegeln.GiltBis(UebergabeDauer.NaechsteSchicht, jetzt, null), transaktion);
            neu++;
        }
        transaktion.Commit();
        return neu;
    }

    // Für die Frage „Ist das ärztlich verordnet?“: Wirkstoffe und Handelsnamen der Medikamentendatenbank.
    public List<string> MedikamentNamen() =>
        [.. Medikamente().SelectMany(m => m.Aktuell.Handelsnamen.Append(m.Aktuell.Wirkstoff)).Where(n => n.Trim().Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];

    // Demo: je Bewohner zwei bis fünf Maßnahmen über die Lebensbereiche, Bestätigungen der letzten Wochen (einige abgelehnt).
    private void DemoMassnahmenEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("""
            DELETE FROM berichte WHERE id IN (SELECT bericht_id FROM massnahme_bestaetigungen WHERE bericht_id IS NOT NULL);
            DELETE FROM massnahme_bestaetigungen; DELETE FROM massnahme_fassungen; DELETE FROM massnahmen;
            """, transaktion);
        var jetzt = Uhr();
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        bewohner.Sort(BewohnerRegeln.NachName);
        var dienstJetzt = SchichtTermin.Zu(jetzt);
        foreach (var (b, fassung, nr) in Demodaten.Massnahmen(bewohner, DateOnly.FromDateTime(jetzt)))
        {
            Ausfuehren("INSERT INTO massnahmen (bewohner_id, erstellt, kuerzel) VALUES ($b, $e, $k)", transaktion,
                ("$b", b.Id), ("$e", Zeit(fassung.Erfasst)), ("$k", fassung.Kuerzel));
            long id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            FassungEinfuegen(id, fassung, transaktion);
            var m = new Massnahme(id, b.Id!.Value, [fassung]);
            // Bestätigungen der vergangenen Dienste (der laufende bleibt offen – für die Checkliste).
            for (var tag = fassung.GiltAb; tag <= dienstJetzt.Tag; tag = tag.AddDays(1))
            {
                var dienst = new SchichtTermin(tag, fassung.Schicht);
                if (dienst.Ende > jetzt || !MassnahmenRegeln.AmTag(fassung, tag)) continue;
                var (ergebnis, grund) = Demodaten.MassnahmeErgebnis(id, tag);
                var zeit = dienst.Beginn.AddHours(2 + nr % 4);
                string kuerzel = Demodaten.MassnahmeKuerzel(fassung.Schicht, tag);
                int minuten = ergebnis == MassnahmeErgebnis.Durchgefuehrt ? fassung.Minuten + (int)(id + tag.DayNumber) % 3 * 5 - 5 : 0;
                long bericht = BerichtEinfuegen(new Bericht
                {
                    BewohnerId = m.BewohnerId, Zeitpunkt = zeit, Schicht = dienst.Schicht, Verfasser = kuerzel, Bereiche = [fassung.Lebensbereich],
                    Text = MassnahmenRegeln.BerichtText(fassung.Titel, ergebnis, grund, Math.Max(0, minuten), ""), Erfasst = zeit, Art = MassnahmenRegeln.ArtBericht,
                }, transaktion);
                BestaetigungEinfuegen(m, fassung, dienst, false, ergebnis, grund, Math.Max(0, minuten), "", kuerzel, zeit, bericht, transaktion);
            }
        }
        SetzeMeta(DemoMassnahmen, "1", transaktion);
    }
}
