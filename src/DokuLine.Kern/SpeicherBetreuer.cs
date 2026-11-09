using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Betreuer je Bewohner: jede Änderung ist eine neue Fassung, nichts wird
// überschrieben; die neueste gilt.
public sealed partial class Speicher
{
    public event Action? BetreuerGeaendert;

    public Betreuer? Betreuer(long bewohnerId) => BetreuerFassungen(bewohnerId).LastOrDefault();

    public List<Betreuer> BetreuerFassungen(long bewohnerId) => BetreuerFassungenIn(null, bewohnerId);

    private List<Betreuer> BetreuerFassungenIn(SqliteTransaction? transaktion, long bewohnerId)
    {
        var liste = new List<Betreuer>();
        using var befehl = Befehl("SELECT name, stelle, email, telefon, geaendert, kuerzel, keine FROM betreuer WHERE bewohner_id = $b ORDER BY id", transaktion, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Betreuer
            {
                Name = leser.GetString(0), Stelle = leser.GetString(1), Email = leser.GetString(2), Telefon = leser.GetString(3),
                Geaendert = AusZeit(leser.GetString(4)), Kuerzel = leser.GetString(5), Keine = leser.GetInt64(6) != 0,
            });
        return liste;
    }

    public void BetreuerSpeichern(long bewohnerId, Betreuer b)
    {
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        BetreuerEinfuegen(bewohnerId, b, null);
        BetreuerGeaendert?.Invoke();
    }

    private void BetreuerEinfuegen(long bewohnerId, Betreuer b, SqliteTransaction? transaktion) =>
        Ausfuehren("INSERT INTO betreuer (bewohner_id, name, stelle, email, telefon, geaendert, kuerzel, keine) VALUES ($b, $n, $s, $e, $t, $g, $k, $x)", transaktion,
            ("$b", bewohnerId), ("$n", b.Name), ("$s", b.Stelle), ("$e", b.Email), ("$t", b.Telefon), ("$g", Zeit(b.Geaendert)), ("$k", b.Kuerzel), ("$x", b.Keine ? 1 : 0));

    private void DemoBetreuerEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM betreuer", transaktion);
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        foreach (var (nachname, b) in Demodaten.Betreuer(Uhr()))
            if (bewohner.FirstOrDefault(x => x.Nachname == nachname) is Bewohner x) BetreuerEinfuegen(x.Id!.Value, b, transaktion);
        SetzeMeta(DemoBetreuer, "1", transaktion);
    }
}
