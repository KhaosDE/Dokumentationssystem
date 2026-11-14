using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Vitalwerte, angeordnete Bereiche, Messaufträge und Richtwerte speichern (Bauplan 36).
public sealed partial class Speicher
{
    private const string RichtwerteSchluessel = "vital_richtwerte", DemoVital = "demo_vitalwerte";
    public event Action? VitalwerteGeaendert;

    public List<string> Vitalarten() => [.. Listeneintraege(Liste.Vitalarten).Where(e => e.Aktiv).Select(e => e.Name)];

    public VitalRichtwerte Richtwerte() => VitalRegeln.RichtwerteAus(Meta(RichtwerteSchluessel));

    public void RichtwerteSpeichern(VitalRichtwerte r, string kuerzel)
    {
        Recht(kuerzel, Funktionen.VitalGrenzen);
        if (r.BlutzuckerEinheit is not (VitalRegeln.MgDl or VitalRegeln.MmolL)) throw new Eingabefehler(["Einheit Blutzucker: mg/dl oder mmol/l."]);
        using var transaktion = db.BeginTransaction();
        SetzeMeta(RichtwerteSchluessel, JsonSerializer.Serialize(r), transaktion);
        transaktion.Commit();
        VitalwerteGeaendert?.Invoke();
    }

    public List<Vitalwert> Vitalwerte(long bewohnerId, bool auchAbgeloeste = false)
    {
        var liste = new List<Vitalwert>();
        using var befehl = Befehl("""
            SELECT id, bewohner_id, zeit, art, wert, wert2, bemerkung, kuerzel, erfasst, vorgaenger, grund FROM vitalwerte WHERE bewohner_id = $b
            """, null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Vitalwert(leser.GetInt64(0), leser.GetInt64(1), AusZeit(leser.GetString(2)), leser.GetString(3), leser.GetDouble(4),
                leser.IsDBNull(5) ? null : leser.GetDouble(5), leser.GetString(6), leser.GetString(7), AusZeit(leser.GetString(8)),
                leser.IsDBNull(9) ? null : leser.GetInt64(9), leser.GetString(10)));
        return auchAbgeloeste ? [.. liste.OrderByDescending(w => w.Zeit).ThenByDescending(w => w.Id)] : VitalRegeln.Gueltige(liste);
    }

    // Mehrere Werte in einem Schritt (gleiche Zeit, gleiche Bemerkung). Werte in der Einheit der Einstellungen.
    public List<Vitalwert> VitalwerteErfassen(long bewohnerId, DateTime zeit, IReadOnlyList<(string Art, double Wert, double? Wert2)> werte, string bemerkung,
        string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.VitalwerteErfassen);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (werte.Count == 0) throw new Eingabefehler(["Bitte mindestens einen Wert eintragen."]);
        if (zeit > jetzt.AddMinutes(5)) throw new Eingabefehler(["Der Zeitpunkt liegt in der Zukunft."]);
        var r = Richtwerte();
        var fehler = werte.Select(w => VitalRegeln.Unplausibel(w.Art, VitalRegeln.Speicherwert(w.Art, w.Wert, r), w.Wert2)).OfType<string>().ToList();
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        var neu = new List<long>();
        using (var transaktion = db.BeginTransaction())
        {
            foreach (var (art, wert, wert2) in werte)
                neu.Add(VitalwertEinfuegen(bewohnerId, zeit, art, VitalRegeln.Speicherwert(art, wert, r), wert2, bemerkung, kuerzel, jetzt, null, "", transaktion));
            transaktion.Commit();
        }
        VitalwerteGeaendert?.Invoke();
        Zugriff(kuerzel, bewohnerId, ZugriffRegeln.Geaendert, "Vitalwerte erfasst");
        return [.. Vitalwerte(bewohnerId).Where(w => neu.Contains(w.Id))];
    }

    // Korrektur nur als neuer Eintrag mit Grund; der alte bleibt gespeichert, zählt aber nicht mehr.
    public Vitalwert VitalwertKorrigieren(long id, double wert, double? wert2, string bemerkung, string grund, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.VitalwerteErfassen);
        if (grund.Trim().Length == 0) throw new Eingabefehler(["Bitte den Grund der Korrektur angeben."]);
        var alt = Vitalwerte(BewohnerVonVitalwert(id), auchAbgeloeste: true).Single(w => w.Id == id);
        if (Vitalwerte(alt.BewohnerId).All(w => w.Id != id)) throw new Eingabefehler(["Dieser Wert wurde schon korrigiert."]);
        var r = Richtwerte();
        double speicher = VitalRegeln.Speicherwert(alt.Art, wert, r);
        if (VitalRegeln.Unplausibel(alt.Art, speicher, wert2) is string f) throw new Eingabefehler([f]);
        long neu;
        using (var transaktion = db.BeginTransaction())
        {
            // In derselben Transaktion noch einmal: Hat ein anderer Arbeitsplatz gerade korrigiert, gäbe es sonst zwei Nachfolger.
            if (WertIn(transaktion, "SELECT 1 FROM vitalwerte WHERE vorgaenger = $i", ("$i", id)) is not null)
                throw new Eingabefehler(["Dieser Wert wurde schon korrigiert."]);
            neu = VitalwertEinfuegen(alt.BewohnerId, alt.Zeit, alt.Art, speicher, wert2, bemerkung, kuerzel, jetzt, id, grund.Trim(), transaktion);
            transaktion.Commit();
        }
        VitalwerteGeaendert?.Invoke();
        Zugriff(kuerzel, alt.BewohnerId, ZugriffRegeln.Geaendert, "Vitalwert korrigiert");
        return Vitalwerte(alt.BewohnerId).Single(w => w.Id == neu);
    }

    private long BewohnerVonVitalwert(long id) =>
        Wert("SELECT bewohner_id FROM vitalwerte WHERE id = $i", ("$i", id)) is long b ? b : throw new Eingabefehler(["Diesen Wert gibt es nicht mehr."]);

    private long VitalwertEinfuegen(long bewohnerId, DateTime zeit, string art, double wert, double? wert2, string bemerkung, string kuerzel, DateTime jetzt,
        long? vorgaenger, string grund, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO vitalwerte (bewohner_id, zeit, art, wert, wert2, bemerkung, kuerzel, erfasst, vorgaenger, grund)
            VALUES ($b, $z, $a, $w, $w2, $be, $k, $e, $v, $g)
            """, transaktion, ("$b", bewohnerId), ("$z", Zeit(zeit)), ("$a", art), ("$w", wert), ("$w2", wert2), ("$be", bemerkung.Trim()),
            ("$k", kuerzel.Trim()), ("$e", Zeit(jetzt)), ("$v", vorgaenger), ("$g", grund));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // Angeordnete Bereiche: es gilt die neueste Anordnung je Wert (ohne Von/Bis: aufgehoben).
    public Dictionary<string, VitalGrenze> Grenzen(long bewohnerId)
    {
        var liste = new List<VitalGrenze>();
        using (var befehl = Befehl("""
            SELECT id, bewohner_id, art, von, bis, von2, bis2, arzt_id, angeordnet_am, kuerzel, erfasst FROM vital_grenzen WHERE bewohner_id = $b ORDER BY id
            """, null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                liste.Add(new VitalGrenze(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), Zahl(leser, 3), Zahl(leser, 4), Zahl(leser, 5), Zahl(leser, 6),
                    leser.IsDBNull(7) ? null : leser.GetInt64(7), AusIso(leser.GetString(8)), leser.GetString(9), AusZeit(leser.GetString(10))));
        return liste.GroupBy(g => g.Art).Select(g => g.Last()).Where(g => g.Gilt).ToDictionary(g => g.Art);
        static double? Zahl(SqliteDataReader l, int i) => l.IsDBNull(i) ? null : l.GetDouble(i);
    }

    public void GrenzeSetzen(VitalGrenze g, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.VitalGrenzen);
        if (g.Gilt && g.ArztId is null) throw new Eingabefehler(["Bitte den anordnenden Arzt wählen (nur nach ärztlicher Anordnung)."]);
        if ((g.Von > g.Bis) || (g.Von2 > g.Bis2)) throw new Eingabefehler(["„von“ ist größer als „bis“."]);
        Ausfuehren("""
            INSERT INTO vital_grenzen (bewohner_id, art, von, bis, von2, bis2, arzt_id, angeordnet_am, kuerzel, erfasst)
            VALUES ($b, $a, $v, $bi, $v2, $b2, $arzt, $am, $k, $e)
            """, null, ("$b", g.BewohnerId), ("$a", g.Art), ("$v", g.Von), ("$bi", g.Bis), ("$v2", g.Von2), ("$b2", g.Bis2), ("$arzt", g.ArztId),
            ("$am", Iso(g.AngeordnetAm)), ("$k", kuerzel.Trim()), ("$e", Zeit(jetzt)));
        VitalwerteGeaendert?.Invoke();
    }

    public List<Messauftrag> Messauftraege(long? bewohnerId = null, bool auchBeendete = false)
    {
        var liste = new List<Messauftrag>();
        using var befehl = Befehl("""
            SELECT id, bewohner_id, art, rhythmus, tag, schicht, ab, beendet, kuerzel, erfasst FROM messauftraege
            WHERE ($b IS NULL OR bewohner_id = $b) ORDER BY id
            """, null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Messauftrag(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), Enum.Parse<Messrhythmus>(leser.GetString(3), true), leser.GetInt32(4),
                BerichtRegeln.SchichtAus(leser.GetString(5)), AusIso(leser.GetString(6)), leser.IsDBNull(7) ? null : AusIso(leser.GetString(7)),
                leser.GetString(8), AusZeit(leser.GetString(9))));
        return auchBeendete ? liste : [.. liste.Where(m => m.Beendet is null)];
    }

    public Messauftrag MessauftragAnlegen(Messauftrag m, string kuerzel, DateTime jetzt)
    {
        Recht(kuerzel, Funktionen.VitalGrenzen);
        if (VitalRegeln.AuftragPruefen(m) is string f) throw new Eingabefehler([f]);
        Ausfuehren("""
            INSERT INTO messauftraege (bewohner_id, art, rhythmus, tag, schicht, ab, kuerzel, erfasst) VALUES ($b, $a, $r, $t, $s, $ab, $k, $e)
            """, null, ("$b", m.BewohnerId), ("$a", m.Art), ("$r", m.Rhythmus.ToString().ToLowerInvariant()), ("$t", m.Tag),
            ("$s", BerichtRegeln.Kuerzel(m.Schicht)), ("$ab", Iso(m.Ab)), ("$k", kuerzel.Trim()), ("$e", Zeit(jetzt)));
        long id = Convert.ToInt64(Wert("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        VitalwerteGeaendert?.Invoke();
        return Messauftraege(m.BewohnerId).Single(x => x.Id == id);
    }

    // Beenden statt löschen: entstandene Aufgaben bleiben, wie sie sind.
    public void MessauftragBeenden(long id, string kuerzel, DateOnly heute)
    {
        Recht(kuerzel, Funktionen.VitalGrenzen);
        Ausfuehren("UPDATE messauftraege SET beendet = $h WHERE id = $i AND beendet IS NULL", null, ("$h", Iso(heute)), ("$i", id));
        VitalwerteGeaendert?.Invoke();
    }

    // Demo: Werte über vier Monate, angeordnete Bereiche (mit dem ersten Arzt aus dem Verzeichnis), Messaufträge.
    private void DemoVitalEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM messauftrag_aufgaben; DELETE FROM messauftraege; DELETE FROM vital_grenzen; DELETE FROM vitalwerte", transaktion);
        var jetzt = Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        var bewohner = LesenIn(transaktion, AktiveBewohnerSql);
        bewohner.Sort(BewohnerRegeln.NachName);
        foreach (var w in Demodaten.Vitalwerte(bewohner, jetzt))
            VitalwertEinfuegen(w.BewohnerId, w.Zeit, w.Art, w.Wert, w.Wert2, w.Bemerkung, w.Kuerzel, w.Zeit.AddMinutes(5), null, "", transaktion);
        var arzt = WertIn(transaktion, "SELECT id FROM aerzte WHERE gruppe = 'arzt' AND aktiv = 1 ORDER BY id LIMIT 1") as long?;
        foreach (var g in Demodaten.VitalGrenzen(bewohner))
            Ausfuehren("""
                INSERT INTO vital_grenzen (bewohner_id, art, von, bis, von2, bis2, arzt_id, angeordnet_am, kuerzel, erfasst)
                VALUES ($b, $a, $v, $bi, $v2, $b2, $arzt, $am, 'EF', $e)
                """, transaktion, ("$b", g.BewohnerId), ("$a", g.Art), ("$v", g.Von), ("$bi", g.Bis), ("$v2", g.Von2), ("$b2", g.Bis2),
                ("$arzt", arzt), ("$am", Iso(heute.AddDays(-60))), ("$e", Zeit(jetzt.AddDays(-60))));
        foreach (var m in Demodaten.Messauftraege(bewohner))
            Ausfuehren("""
                INSERT INTO messauftraege (bewohner_id, art, rhythmus, tag, schicht, ab, kuerzel, erfasst) VALUES ($b, $a, $r, $t, $s, $ab, 'EF', $e)
                """, transaktion, ("$b", m.BewohnerId), ("$a", m.Art), ("$r", m.Rhythmus.ToString().ToLowerInvariant()), ("$t", m.Tag),
                ("$s", BerichtRegeln.Kuerzel(m.Schicht)), ("$ab", Iso(heute.AddDays(-90))), ("$e", Zeit(jetzt.AddDays(-90))));
        SetzeMeta(DemoVital, "1", transaktion);
    }

    // Zu Beginn jeder Schicht: aus den fälligen Messaufträgen Aufgaben – je Auftrag nur einmal je Schicht.
    private int MessauftraegeErzeugen(DateTime jetzt, IReadOnlySet<long> aktive)
    {
        var schicht = SchichtTermin.Zu(jetzt);
        int neu = 0;
        foreach (var m in Messauftraege().Where(m => VitalRegeln.Faellig(m, schicht) && aktive.Contains(m.BewohnerId)))
        {
            using var transaktion = db.BeginTransaction();
            Ausfuehren("INSERT OR IGNORE INTO messauftrag_aufgaben (messauftrag_id, schicht_tag, schicht) VALUES ($m, $st, $s)", transaktion,
                ("$m", m.Id), ("$st", Iso(schicht.Tag)), ("$s", BerichtRegeln.Kuerzel(schicht.Schicht)));
            if (Convert.ToInt32(WertIn(transaktion, "SELECT changes()"), CultureInfo.InvariantCulture) == 1)
            {
                Ausfuehren("""
                    INSERT INTO aufgaben (bewohner_id, text, fuer, schicht_tag, schicht, faellig_bis, erstellt, verfasser, bereich)
                    VALUES ($b, $t, 'schicht', $st, $s, $fb, $e, $v, '')
                    """, transaktion, ("$b", m.BewohnerId), ("$t", VitalRegeln.AuftragText(m)), ("$st", Iso(schicht.Tag)),
                    ("$s", BerichtRegeln.Kuerzel(schicht.Schicht)), ("$fb", Zeit(schicht.Ende)), ("$e", Zeit(schicht.Beginn)), ("$v", m.Kuerzel));
                neu++;
            }
            transaktion.Commit();
        }
        return neu;
    }
}
