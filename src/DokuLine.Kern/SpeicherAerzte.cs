using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Ärzteverzeichnis im Speicher (Schritt 26b): anlegen, ändern, inaktiv setzen; nie löschen.
public sealed partial class Speicher
{
    public event Action? AerzteGeaendert;

    public List<Arzt> Aerzte(bool nurAktive = false)
    {
        var liste = new List<Arzt>();
        using var befehl = Befehl("SELECT id, name, fachrichtung, praxis, telefon, email, aktiv FROM aerzte ORDER BY name COLLATE NOCASE", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Arzt
            {
                Id = leser.GetInt64(0), Name = leser.GetString(1), Fachrichtung = leser.GetString(2), Praxis = leser.GetString(3),
                Telefon = leser.GetString(4), Email = leser.GetString(5), Aktiv = leser.GetInt64(6) == 1,
            });
        return nurAktive ? liste.Where(a => a.Aktiv).ToList() : liste;
    }

    public Arzt? ArztNach(string name) =>
        Aerzte().FirstOrDefault(a => a.Name.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase));

    // Anlegen (Id 0) oder ändern. Namen sind eindeutig, damit die Auswahl überall eindeutig bleibt.
    public Arzt ArztSpeichern(Arzt a, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        if (Aerzte().Any(x => x.Id != a.Id && x.Name.Equals(a.Name, StringComparison.CurrentCultureIgnoreCase)))
            throw new Eingabefehler([$"„{a.Name}“ steht schon im Verzeichnis."]);
        long id = a.Id;
        if (id == 0)
        {
            using var transaktion = db.BeginTransaction();
            ArztEinfuegen(a, kuerzel, jetzt, transaktion);
            id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), System.Globalization.CultureInfo.InvariantCulture);
            transaktion.Commit();
        }
        else
            Ausfuehren("UPDATE aerzte SET name = $n, fachrichtung = $f, praxis = $p, telefon = $t, email = $e, geaendert = $g, kuerzel = $k WHERE id = $id", null,
                ("$n", a.Name), ("$f", a.Fachrichtung), ("$p", a.Praxis), ("$t", a.Telefon), ("$e", a.Email), ("$g", Zeit(jetzt)), ("$k", kuerzel), ("$id", id));
        AerzteGeaendert?.Invoke();
        return Aerzte().Single(x => x.Id == id);
    }

    public void ArztAktiv(long id, bool aktiv, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        Ausfuehren("UPDATE aerzte SET aktiv = $a, geaendert = $g, kuerzel = $k WHERE id = $id", null,
            ("$a", aktiv ? 1 : 0), ("$g", Zeit(jetzt)), ("$k", kuerzel), ("$id", id));
        AerzteGeaendert?.Invoke();
    }

    private void ArztEinfuegen(Arzt a, string kuerzel, DateTime jetzt, SqliteTransaction transaktion) =>
        Ausfuehren("INSERT INTO aerzte (name, fachrichtung, praxis, telefon, email, aktiv, geaendert, kuerzel) VALUES ($n, $f, $p, $t, $e, $a, $g, $k)", transaktion,
            ("$n", a.Name), ("$f", a.Fachrichtung), ("$p", a.Praxis), ("$t", a.Telefon), ("$e", a.Email), ("$a", a.Aktiv ? 1 : 0), ("$g", Zeit(jetzt)), ("$k", kuerzel));

    private void DemoAerzteEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM aerzte", transaktion);
        foreach (var a in Demodaten.Aerzte()) ArztEinfuegen(a, "EF", Uhr(), transaktion);
        SetzeMeta(DemoAerzte, "1", transaktion);
    }
}
