using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Biografie im Speicher: je Bewohner und Abschnitt Fassungen (UPDATE verboten).
public sealed partial class Speicher
{
    public List<BiografieAbschnitt> Biografie(long bewohnerId)
    {
        var fassungen = new Dictionary<int, List<BiografieFassung>>();
        using (var befehl = Befehl("SELECT abschnitt, text, angegeben, erstellt, kuerzel FROM biografie WHERE bewohner_id = $b ORDER BY id", null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!fassungen.TryGetValue(leser.GetInt32(0), out var l)) fassungen[leser.GetInt32(0)] = l = [];
                l.Add(new BiografieFassung
                {
                    Abschnitt = leser.GetInt32(0), Text = leser.GetString(1), Angegeben = leser.GetInt64(2) == 1,
                    Erstellt = AusZeit(leser.GetString(3)), Kuerzel = leser.GetString(4),
                });
            }
        return BiografieRegeln.Abschnitte.Select((titel, i) => new BiografieAbschnitt(i, titel, fassungen.GetValueOrDefault(i) ?? [])).ToList();
    }

    // Neue Fassung eines Abschnitts; ist nichts geändert, entsteht keine. erwartet: so viele
    // Fassungen kannte der Aufrufer – sind es inzwischen mehr, gibt es einen Konflikt.
    public BiografieAbschnitt BiografieSpeichern(long bewohnerId, BiografieFassung f, int? erwartet = null)
    {
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var bisher = Biografie(bewohnerId)[f.Abschnitt];
        if (bisher.Aktuell is BiografieFassung alt && alt.Text == f.Text && alt.Angegeben == f.Angegeben) return bisher;
        if (bisher.Aktuell is null && f.Text.Length == 0) return bisher;
        using var transaktion = db.BeginTransaction();
        FassungenPruefen(transaktion, "SELECT COUNT(*) FROM biografie WHERE bewohner_id = $b AND abschnitt = $a", erwartet,
            () => Biografie(bewohnerId)[f.Abschnitt].Aktuell is BiografieFassung a ? FassungText(a.Kuerzel, a.Erstellt, a.Text) : "",
            FassungText(f.Kuerzel, f.Erstellt, f.Text), ("$b", bewohnerId), ("$a", f.Abschnitt));
        BiografieEinfuegen(bewohnerId, f, transaktion);
        transaktion.Commit();
        return Biografie(bewohnerId)[f.Abschnitt];
    }

    private void BiografieEinfuegen(long bewohnerId, BiografieFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("INSERT INTO biografie (bewohner_id, abschnitt, text, angegeben, erstellt, kuerzel) VALUES ($b, $a, $t, $g, $e, $k)", transaktion,
            ("$b", bewohnerId), ("$a", f.Abschnitt), ("$t", f.Text), ("$g", f.Angegeben ? 1 : 0), ("$e", Zeit(f.Erstellt)), ("$k", f.Kuerzel));

    // Demo: ausgedachte, kurze und wertneutrale Biografien; einige Abschnitte bleiben leer.
    private void DemoBiografieEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM biografie", transaktion);
        foreach (var (bewohnerId, f) in Demodaten.Biografien(bewohner, Uhr()))
            BiografieEinfuegen(bewohnerId, f, transaktion);
        SetzeMeta(DemoBiografie, "1", transaktion);
    }
}
