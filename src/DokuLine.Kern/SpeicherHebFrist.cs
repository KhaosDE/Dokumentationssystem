using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// HEB-Fristen im Speicher (Bauplan 52): Ereignisse „faellig“, „kein“, „abgegeben“, „aufgabe“ (nie geändert). Setzen dürfen,
// die HEB-Bögen bearbeiten dürfen.
public sealed partial class Speicher
{
    private const string DemoHebFrist = "demo_heb_frist";

    public event Action? HebFristGeaendert;

    private List<HebFristEreignis> HebFristEreignisse(SqliteTransaction? transaktion = null)
    {
        var liste = new List<HebFristEreignis>();
        using var befehl = Befehl("SELECT id, bewohner_id, art, datum, grund, bogen_id, aufgabe_id, kuerzel, erfasst FROM heb_frist ORDER BY erfasst, id",
            transaktion, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new HebFristEreignis(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), leser.IsDBNull(3) ? null : AusIso(leser.GetString(3)),
                leser.GetString(4), leser.IsDBNull(5) ? null : leser.GetInt64(5), leser.IsDBNull(6) ? null : leser.GetInt64(6), leser.GetString(7),
                AusZeit(leser.GetString(8))));
        return liste;
    }

    public List<HebFristStand> HebFristen(IEnumerable<Bewohner> bewohner, DateOnly heute)
    {
        int frist = Einstellungen.Laden().HebFristMonate;
        var ereignisse = HebFristEreignisse().ToLookup(e => e.BewohnerId);
        var angaben = AlleAngaben(heute);
        return [.. bewohner.Select(b => HebFristRegeln.Stand(b, [.. ereignisse[b.Id!.Value]], HebBoegen(b.Id!.Value),
            angaben.GetValueOrDefault(b.Id!.Value)?.GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>(), frist))];
    }

    public HebFristStand HebFrist(long bewohnerId, DateOnly heute) =>
        HebFristen([Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."])], heute)[0];

    // Eingetragene Fälligkeiten im Zeitraum als Ganztagseinträge (Kalender, nur zum Zeigen).
    public List<KalenderTermin> HebFristKalender(DateOnly von, DateOnly bis) =>
        [.. HebFristen(AlleBewohner(), von).Select(HebFristRegeln.Kalendereintrag).OfType<KalenderTermin>()
            .Where(k => DateOnly.FromDateTime(k.Aktuell.Beginn) >= von && DateOnly.FromDateTime(k.Aktuell.Beginn) <= bis)];

    private void HebFristEinfuegen(long bewohnerId, string art, DateOnly? datum, string grund, long? bogenId, long? aufgabeId, string kuerzel, DateTime jetzt,
        SqliteTransaction? transaktion) =>
        Ausfuehren("INSERT INTO heb_frist (bewohner_id, art, datum, grund, bogen_id, aufgabe_id, kuerzel, erfasst) VALUES ($b, $a, $d, $g, $bo, $au, $k, $e)",
            transaktion, ("$b", bewohnerId), ("$a", art), ("$d", datum is DateOnly d ? Iso(d) : null), ("$g", grund.Trim()), ("$bo", bogenId), ("$au", aufgabeId),
            ("$k", kuerzel.Trim()), ("$e", Zeit(jetzt)));

    private void HebFristRecht(string kuerzel, long bewohnerId)
    {
        if (Grund(kuerzel, Funktionen.Heb) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (Archiviert(bewohnerId)) throw new Eingabefehler(["Dieser Bewohner ist entlassen."]);
    }

    // Von Hand: Datum mit Grund (z. B. „Schreiben des Bezirks vom …“).
    public void HebFaelligSetzen(long bewohnerId, DateOnly am, string grund, string kuerzel, DateTime jetzt)
    {
        HebFristRecht(kuerzel, bewohnerId);
        if (grund.Trim().Length == 0) throw new Eingabefehler(["Bitte den Grund angeben (z. B. „Schreiben des Bezirks vom …“)."]);
        HebFristEinfuegen(bewohnerId, "faellig", am, grund, null, null, kuerzel, jetzt, null);
        HebFristGeaendert?.Invoke();
    }

    public void HebKeinerNoetig(long bewohnerId, string grund, string kuerzel, DateTime jetzt)
    {
        HebFristRecht(kuerzel, bewohnerId);
        if (grund.Trim().Length == 0) throw new Eingabefehler(["Bitte den Grund angeben."]);
        HebFristEinfuegen(bewohnerId, "kein", null, grund, null, null, kuerzel, jetzt, null);
        HebFristGeaendert?.Invoke();
    }

    // Vorschlag für die nächste Fälligkeit nach einer Abgabe an diesem Tag.
    public DateOnly HebNaechsteVorschlag(long bewohnerId, DateOnly abgabe)
    {
        var b = Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var k = Angaben(bewohnerId, abgabe).GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>();
        return HebFristRegeln.Vorschlag(b, k, HebBoegen(bewohnerId), abgabe, Einstellungen.Laden().HebFristMonate);
    }

    // Abgegeben schließt die Fälligkeit; die nächste wird gleich mit gesetzt, wenn sie angegeben ist.
    public void HebAbgegeben(long bewohnerId, DateOnly am, DateOnly? naechste, string kuerzel, DateTime jetzt)
    {
        HebFristRecht(kuerzel, bewohnerId);
        var heute = DateOnly.FromDateTime(jetzt);
        if (am > heute) throw new Eingabefehler(["Das Datum der Abgabe liegt in der Zukunft."]);
        if (naechste is DateOnly n && n <= am) throw new Eingabefehler(["Die nächste Fälligkeit muss nach der Abgabe liegen."]);
        var stand = HebFrist(bewohnerId, heute);
        if (stand.Zustand == HebFristZustand.KeinHeb) throw new Eingabefehler(["Für diesen Bewohner ist „kein HEB nötig“ eingetragen."]);
        if (stand.Stand == HebBogenStand.InArbeit) throw new Eingabefehler(["Der Bogen ist noch ein Entwurf – bitte zuerst als fertig markieren."]);
        using var transaktion = db.BeginTransaction();
        HebFristEinfuegen(bewohnerId, "abgegeben", am, "", stand.Bogen?.Id, null, kuerzel, jetzt, transaktion);
        if (naechste is DateOnly bis)
            // Dieselbe Zeit wie die Abgabe: Die Reihenfolge ergibt die Nummer; eine Sekunde später überholte sie sonst eine Eingabe danach.
            HebFristEinfuegen(bewohnerId, "faellig", bis, $"nach Abgabe am {Datum.Text(am)}", null, null, kuerzel, jetzt, transaktion);
        transaktion.Commit();
        HebFristGeaendert?.Invoke();
    }

    // Erste Stufe: eine Aufgabe „wer Zeit hat“ des Bereichs (Verfasser AUTO), einmal je Abschnitt. Ist ein Bogen angelegt,
    // wird sie als erledigt markiert. Die Sperre in den Metadaten verhindert doppelte Aufgaben, wenn zwei Plätze zugleich prüfen.
    public int HebErinnerungen(DateTime jetzt)
    {
        var w = Einstellungen.Laden();
        var heute = DateOnly.FromDateTime(jetzt);
        var bezuege = Bezuege();
        int neu = 0;
        foreach (var s in HebFristen(AlleBewohner(), heute))
        {
            long id = s.Bewohner.Id!.Value;
            if (s.AufgabeId is long aufgabeId && s.Stand != HebBogenStand.Offen && AufgabeLesen(aufgabeId) is { Offen: true })
                AufgabeErledigt(aufgabeId, "AUTO", jetzt);
            if (!HebFristRegeln.AufgabeNoetig(s, heute, w.HebErinnernWochen, w.HebAchtungWochen) || s.FaelligAm is not DateOnly f) continue;
            string sperre = $"heb_erinnert:{id}:{Iso(f)}:{s.LetzteAbgabe?.Id ?? 0}";
            using var transaktion = db.BeginTransaction();
            Ausfuehren("INSERT OR IGNORE INTO meta (schluessel, wert) VALUES ($s, '1')", transaktion, ("$s", sperre));
            if (Convert.ToInt64(WertIn(transaktion, "SELECT changes()"), CultureInfo.InvariantCulture) == 0) continue;
            var bezug = bezuege.GetValueOrDefault(id)?.Bezugspflege?.Kuerzel;
            long neueAufgabe = AufgabeEinfuegenIn(new Aufgabe
            {
                BewohnerId = id, Bereich = s.Bewohner.Wohnbereich, Text = HebFristRegeln.AufgabeText(s.Bewohner, f, bezug), Fuer = AufgabeFuer.WerZeitHat,
                Erstellt = jetzt, Verfasser = "AUTO",
            }, transaktion);
            HebFristEinfuegen(id, "aufgabe", f, "", null, neueAufgabe, "AUTO", jetzt, transaktion);
            transaktion.Commit();
            neu++;
        }
        if (neu > 0) { AufgabenGeaendert?.Invoke(); HebFristGeaendert?.Invoke(); }
        return neu;
    }

    // Demo: frühere fertige Bögen gelten als abgegeben; je Bereich eine Frist überfällig, eine in den nächsten 4 Wochen, eine in
    // der ersten Stufe; einige „kein HEB nötig“, einige nicht erfasst, die übrigen über die nächsten Monate verteilt.
    private void DemoHebFristEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM heb_frist", transaktion);
        var jetzt = Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        var bewohner = LesenIn(transaktion, AktiveBewohnerSql);
        foreach (var bereich in bewohner.GroupBy(b => b.Wohnbereich))
        {
            var liste = bereich.ToList();
            liste.Sort(BewohnerRegeln.NachName);
            for (int j = 0; j < liste.Count; j++)
            {
                long id = liste[j].Id!.Value;
                var boegen = HebLesen("WHERE bewohner_id = $b", transaktion, ("$b", id));
                var letzte = jetzt.AddDays(-400);
                foreach (var x in boegen.Where(x => x.Fertig))
                {
                    var am = x.Bis.AddDays(3) <= heute ? x.Bis.AddDays(3) : heute;
                    letzte = am.ToDateTime(new TimeOnly(11, 0));
                    HebFristEinfuegen(id, "abgegeben", am, "", x.Id, null, "RS", letzte, transaktion);
                }
                var gesetzt = letzte.AddMinutes(1);
                if (j % 9 == 8) { HebFristEinfuegen(id, "kein", null, "Wechsel in eine andere Einrichtung geplant", null, null, "RS", gesetzt, transaktion); continue; }
                if (j % 7 == 6) continue;   // nicht erfasst: es gilt der Vorschlag
                var faellig = j switch { 0 => heute.AddDays(-10), 1 => heute.AddDays(20), 2 => heute.AddDays(45), _ => heute.AddDays(60 + j * 11) };
                if (j > 2 && boegen.Count > 0 && !boegen[^1].Fertig) faellig = heute.AddDays(25);   // ein Entwurf liegt vor: bald fällig
                HebFristEinfuegen(id, "faellig", faellig, "Bewilligung des Bezirks", null, null, "RS", gesetzt, transaktion);
            }
        }
        SetzeMeta(DemoHebFrist, "1", transaktion);
    }
}
