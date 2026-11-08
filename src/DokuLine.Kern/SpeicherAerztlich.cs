using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Ärztliche Informationen im Speicher (Bauplan Schritt 26f).
public sealed partial class Speicher
{
    private const string DemoAerztlich = "demo_aerztlich";

    public event Action? AerztlichGeaendert;

    // Aktuelle zuerst (Hauptdiagnosen vor Nebendiagnosen), darunter die beendeten.
    public List<Diagnose> Diagnosen(long bewohnerId)
    {
        var liste = new List<Diagnose>();
        using var befehl = Befehl("SELECT id, bewohner_id, icd, bezeichnung, seit, art, arzt, hinweis, erstellt, kuerzel, beendet, beendet_kuerzel " +
                                  "FROM diagnosen WHERE bewohner_id = $b", null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Diagnose
            {
                Id = leser.GetInt64(0), BewohnerId = leser.GetInt64(1), Icd = leser.GetString(2), Bezeichnung = leser.GetString(3),
                Seit = leser.IsDBNull(4) ? null : AusIso(leser.GetString(4)), Art = leser.GetString(5) == "haupt" ? DiagnoseArt.Haupt : DiagnoseArt.Neben,
                Arzt = leser.GetString(6), Hinweis = leser.GetString(7), Erstellt = AusZeit(leser.GetString(8)), Kuerzel = leser.GetString(9),
                Beendet = leser.IsDBNull(10) ? null : AusIso(leser.GetString(10)), BeendetKuerzel = leser.IsDBNull(11) ? null : leser.GetString(11),
            });
        return [.. liste.OrderBy(d => d.Aktuell ? 0 : 1).ThenBy(d => d.Art).ThenByDescending(d => d.Beendet).ThenBy(d => d.Id)];
    }

    public Diagnose DiagnoseSpeichern(Diagnose d)
    {
        if (Bewohner(d.BewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        DiagnoseEinfuegen(d, null);
        AerztlichGeaendert?.Invoke();
        return Diagnosen(d.BewohnerId).Last(x => x.Bezeichnung == d.Bezeichnung && x.Aktuell);
    }

    private void DiagnoseEinfuegen(Diagnose d, SqliteTransaction? transaktion) =>
        Ausfuehren("INSERT INTO diagnosen (bewohner_id, icd, bezeichnung, seit, art, arzt, hinweis, erstellt, kuerzel, beendet, beendet_kuerzel) " +
                   "VALUES ($b, $i, $t, $s, $a, $r, $h, $e, $k, $be, $bk)",
            transaktion, ("$b", d.BewohnerId), ("$i", d.Icd), ("$t", d.Bezeichnung), ("$s", d.Seit is DateOnly s ? Iso(s) : null),
            ("$a", d.Art == DiagnoseArt.Haupt ? "haupt" : "neben"), ("$r", d.Arzt), ("$h", d.Hinweis), ("$e", Zeit(d.Erstellt)), ("$k", d.Kuerzel),
            ("$be", d.Beendet is DateOnly be ? Iso(be) : null), ("$bk", d.BeendetKuerzel));

    // „Beendet am …“: Die Diagnose bleibt mit Verlauf sichtbar, gilt aber nicht mehr.
    public void DiagnoseBeenden(long id, DateOnly am, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add(RechteRegeln.NichtAngemeldetText);
        var d = WertIn(null, "SELECT seit FROM diagnosen WHERE id = $id AND beendet IS NULL", ("$id", id));
        if (d is null) fehler.Add("Diese Diagnose ist schon beendet oder gibt es nicht.");
        else if (d is string seit && am < AusIso(seit)) fehler.Add("Beendet am: liegt vor „seit“.");
        if (am > DateOnly.FromDateTime(jetzt)) fehler.Add("Beendet am: Das Datum liegt in der Zukunft.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        Ausfuehren("UPDATE diagnosen SET beendet = $b, beendet_kuerzel = $k WHERE id = $id", null, ("$b", Iso(am)), ("$k", kuerzel), ("$id", id));
        AerztlichGeaendert?.Invoke();
    }

    // Stoff → Art der Reaktion.
    public Dictionary<string, string> Reaktionen(long bewohnerId)
    {
        var d = new Dictionary<string, string>();
        using var befehl = Befehl("SELECT stoff, reaktion FROM allergie_reaktionen WHERE bewohner_id = $b", null, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) d[leser.GetString(0)] = leser.GetString(1);
        return d;
    }

    // Allergien wie bisher mit drei Zuständen (Regel 5), dazu je Stoff die Art der Reaktion (freiwillig).
    public void AllergienSpeichern(long bewohnerId, AllergieZustand zustand, string allergien, IReadOnlyDictionary<string, string> reaktionen)
    {
        allergien = zustand == AllergieZustand.Eingetragen ? (allergien ?? "").Trim() : "";
        if (Bewohner(bewohnerId) is not Bewohner b) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var fehler = BewohnerRegeln.Pruefen(b with { Allergie = zustand, Allergien = allergien })
            .Where(f => f.StartsWith("Allergien", StringComparison.Ordinal)).ToList();
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        var stoffe = DiagnoseRegeln.Stoffe(allergien);
        using (var transaktion = db.BeginTransaction())
        {
            Ausfuehren("UPDATE bewohner SET allergie = $a, allergien = $t WHERE id = $id", transaktion,
                ("$a", BewohnerRegeln.Kuerzel(zustand)), ("$t", allergien), ("$id", bewohnerId));
            Ausfuehren("DELETE FROM allergie_reaktionen WHERE bewohner_id = $b", transaktion, ("$b", bewohnerId));
            foreach (var (stoff, reaktion) in reaktionen)
                if (stoffe.Contains(stoff) && !string.IsNullOrWhiteSpace(reaktion))
                    Ausfuehren("INSERT INTO allergie_reaktionen (bewohner_id, stoff, reaktion) VALUES ($b, $s, $r)", transaktion,
                        ("$b", bewohnerId), ("$s", stoff), ("$r", reaktion.Trim()));
            transaktion.Commit();
        }
        AerztlichGeaendert?.Invoke();
    }

    public string Notfallhinweise(long bewohnerId) =>
        Convert.ToString(WertIn(null, "SELECT text FROM notfallhinweise WHERE bewohner_id = $b", ("$b", bewohnerId)), CultureInfo.InvariantCulture) ?? "";

    public void NotfallhinweiseSpeichern(long bewohnerId, string text, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        Ausfuehren("INSERT INTO notfallhinweise (bewohner_id, text, geaendert, kuerzel) VALUES ($b, $t, $g, $k) " +
                   "ON CONFLICT (bewohner_id) DO UPDATE SET text = excluded.text, geaendert = excluded.geaendert, kuerzel = excluded.kuerzel",
            null, ("$b", bewohnerId), ("$t", (text ?? "").Trim()), ("$g", Zeit(jetzt)), ("$k", kuerzel));
        AerztlichGeaendert?.Invoke();
    }

    // Demo: ausgedachte Diagnosen, Reaktionen und Notfallhinweise (daten/demo/aerztlich.json).
    private void DemoAerztlichEinfuegen(SqliteTransaction transaktion)
    {
        var demo = Demodaten.Aerztlich(Uhr());
        var bewohner = AlleBewohner(transaktion).ToDictionary(b => b.Nachname, b => b.Id!.Value);
        Ausfuehren("DELETE FROM diagnosen; DELETE FROM allergie_reaktionen; DELETE FROM notfallhinweise;", transaktion);
        foreach (var (nachname, d) in demo.Diagnosen)
            if (bewohner.TryGetValue(nachname, out long id)) DiagnoseEinfuegen(d with { BewohnerId = id }, transaktion);
        foreach (var (nachname, stoff, reaktion) in demo.Reaktionen)
            if (bewohner.TryGetValue(nachname, out long id))
                Ausfuehren("INSERT OR IGNORE INTO allergie_reaktionen (bewohner_id, stoff, reaktion) VALUES ($b, $s, $r)", transaktion, ("$b", id), ("$s", stoff), ("$r", reaktion));
        foreach (var (nachname, text) in demo.Notfall)
            if (bewohner.TryGetValue(nachname, out long id))
                Ausfuehren("INSERT OR REPLACE INTO notfallhinweise (bewohner_id, text, geaendert, kuerzel) VALUES ($b, $t, $g, 'EF')", transaktion,
                    ("$b", id), ("$t", text), ("$g", Zeit(Uhr())));
        SetzeMeta(DemoAerztlich, "1", transaktion);
    }
}
