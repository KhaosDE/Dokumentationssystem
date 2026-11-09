using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Fahrzeuge und Fahrten im Speicher. Fahrten und „losgefahren“/„zurück“
// werden nie überschrieben (Datenbank-Regel).
public sealed partial class Speicher
{
    public event Action? FahrtenGeaendert;

    // ----- Fahrzeuge (Liste in den Einstellungen) -----

    public List<Listeneintrag> Fahrzeuge()
    {
        var liste = new List<Listeneintrag>();
        using var befehl = Befehl("SELECT name, aktiv, (SELECT COUNT(*) FROM fahrten f WHERE f.fahrzeug = z.name COLLATE NOCASE) FROM fahrzeuge z ORDER BY nr, name", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(new Listeneintrag(leser.GetString(0), leser.GetInt64(1) == 1, leser.GetInt32(2)));
        return liste;
    }

    public List<string> AktiveFahrzeuge() => Fahrzeuge().Where(f => f.Aktiv).Select(f => f.Name).ToList();

    public void FahrzeugAnlegen(string name)
    {
        name = name.Trim();
        if (name.Length == 0) throw new Eingabefehler(["Bitte einen Namen eingeben (z. B. „VW Bus“)."]);
        if (name.Length > 30) throw new Eingabefehler(["Name: höchstens 30 Zeichen."]);
        if (Fahrzeuge().Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new Eingabefehler([$"„{name}“ gibt es schon."]);
        Ausfuehren("INSERT INTO fahrzeuge (name, nr) VALUES ($n, (SELECT COALESCE(MAX(nr), 0) + 1 FROM fahrzeuge))", null, ("$n", name));
        FahrtenGeaendert?.Invoke();
    }

    // Stilllegen statt löschen: Frühere Fahrten behalten ihr Fahrzeug.
    public void FahrzeugAktiv(string name, bool aktiv)
    {
        Ausfuehren("UPDATE fahrzeuge SET aktiv = $a WHERE name = $n COLLATE NOCASE", null, ("$a", aktiv ? 1 : 0), ("$n", name));
        FahrtenGeaendert?.Invoke();
    }

    // ----- Fahrten -----

    public List<Fahrt> Fahrten(DateOnly? von = null, DateOnly? bis = null) =>
        FahrtenLesen(null).Where(f => (von is not DateOnly v || DateOnly.FromDateTime(f.Bis) >= v) && (bis is not DateOnly b || DateOnly.FromDateTime(f.Von) <= b))
                          .OrderBy(f => f.Von).ToList();

    public List<Fahrt> FahrtUeberschneidungen(Fahrt neu) => FahrtRegeln.Ueberschneidungen(neu, FahrtenLesen(null));

    public Fahrt FahrtAnlegen(Fahrt f)
    {
        foreach (var b in f.Bewohner)
            if (Bewohner(b) is null) throw new Eingabefehler(["Einen der Bewohner gibt es nicht mehr."]);
        using var transaktion = db.BeginTransaction();
        long id = FahrtEinfuegen(f, transaktion);
        transaktion.Commit();
        FahrtenGeaendert?.Invoke();
        return FahrtenLesen(null).Single(x => x.Id == id);
    }

    private long FahrtEinfuegen(Fahrt f, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO fahrten (fahrzeug, von, bis, zweck, fahrer, bewohner, ziel, erstellt, kuerzel)
            VALUES ($fz, $v, $b, $z, $fa, $bw, $zi, $e, $k)
            """, transaktion,
            ("$fz", f.Fahrzeug), ("$v", Zeit(f.Von)), ("$b", Zeit(f.Bis)), ("$z", FahrtRegeln.ZweckKuerzel(f.Zweck)), ("$fa", f.Fahrer),
            ("$bw", JsonSerializer.Serialize(f.Bewohner)), ("$zi", f.Ziel), ("$e", Zeit(f.Erstellt)), ("$k", f.Kuerzel));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // „losgefahren“ (mit Uhrzeit und Kürzel). Ein Fahrzeug kann nur einmal gleichzeitig unterwegs sein.
    public Fahrt Losgefahren(long fahrtId, string kuerzel, DateTime jetzt)
    {
        var alle = FahrtenLesen(null);
        var f = alle.FirstOrDefault(x => x.Id == fahrtId) ?? throw new Eingabefehler(["Diese Fahrt gibt es nicht mehr."]);
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (f.Losgefahren is not null) fehler.Add($"Schon losgefahren um {f.Losgefahren:HH\\:mm}.");
        if (alle.FirstOrDefault(x => x.Id != f.Id && x.Unterwegs && x.Fahrzeug.Equals(f.Fahrzeug, StringComparison.OrdinalIgnoreCase)) is Fahrt andere)
            fehler.Add($"Der {f.Fahrzeug} ist noch unterwegs ({FahrtRegeln.Zeile(andere)}) – erst „zurück“ eintragen.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        EreignisEinfuegen(fahrtId, "los", kuerzel, jetzt);
        return FahrtenLesen(null).Single(x => x.Id == fahrtId);
    }

    public Fahrt Zurueck(long fahrtId, string kuerzel, DateTime jetzt)
    {
        var f = FahrtenLesen(null).FirstOrDefault(x => x.Id == fahrtId) ?? throw new Eingabefehler(["Diese Fahrt gibt es nicht mehr."]);
        var fehler = new List<string>();
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (f.Losgefahren is null) fehler.Add("Diese Fahrt hat noch nicht begonnen.");
        else if (f.Zurueck is not null) fehler.Add($"Schon zurück um {f.Zurueck:HH\\:mm}.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        EreignisEinfuegen(fahrtId, "zurueck", kuerzel, jetzt);
        return FahrtenLesen(null).Single(x => x.Id == fahrtId);
    }

    private void EreignisEinfuegen(long fahrtId, string art, string kuerzel, DateTime jetzt)
    {
        Ausfuehren("INSERT INTO fahrt_ereignisse (fahrt_id, art, zeit, kuerzel) VALUES ($f, $a, $z, $k)", null,
            ("$f", fahrtId), ("$a", art), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()));
        FahrtenGeaendert?.Invoke();
    }

    // Je aktivem Fahrzeug: „vor Ort“ oder „unterwegs seit …“.
    public List<FahrzeugStand> FahrzeugStaende(DateTime jetzt)
    {
        var fahrten = FahrtenLesen(null);
        return AktiveFahrzeuge().Select(f => FahrtRegeln.Stand(f, fahrten, jetzt)).ToList();
    }

    // Bewohner, die gerade mitfahren (Fahrt losgefahren, noch nicht zurück): „abwesend: …“.
    public Dictionary<long, Marke> Mitfahrende(DateTime jetzt)
    {
        var ergebnis = new Dictionary<long, Marke>();
        foreach (var f in FahrtenLesen(null).Where(f => f.Unterwegs))
            foreach (var b in f.Bewohner)
                ergebnis[b] = new Marke(MarkenArt.Abwesend, FahrtRegeln.AbwesendText(f));
        return ergebnis;
    }

    private List<Fahrt> FahrtenLesen(SqliteTransaction? transaktion)
    {
        var ereignisse = new Dictionary<(long, string), (DateTime Zeit, string Kuerzel)>();
        using (var befehl = Befehl("SELECT fahrt_id, art, zeit, kuerzel FROM fahrt_ereignisse", transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read()) ereignisse[(leser.GetInt64(0), leser.GetString(1))] = (AusZeit(leser.GetString(2)), leser.GetString(3));
        var liste = new List<Fahrt>();
        using (var befehl = Befehl("SELECT id, fahrzeug, von, bis, zweck, fahrer, bewohner, ziel, erstellt, kuerzel FROM fahrten ORDER BY von, id", transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                long id = leser.GetInt64(0);
                bool los = ereignisse.TryGetValue((id, "los"), out var l), zurueck = ereignisse.TryGetValue((id, "zurueck"), out var z);
                liste.Add(new Fahrt
                {
                    Id = id, Fahrzeug = leser.GetString(1), Von = AusZeit(leser.GetString(2)), Bis = AusZeit(leser.GetString(3)),
                    Zweck = FahrtRegeln.ZweckAus(leser.GetString(4)), Fahrer = leser.GetString(5),
                    Bewohner = JsonSerializer.Deserialize<List<long>>(leser.GetString(6)) ?? [], Ziel = leser.GetString(7),
                    Erstellt = AusZeit(leser.GetString(8)), Kuerzel = leser.GetString(9),
                    Losgefahren = los ? l.Zeit : null, LosVon = los ? l.Kuerzel : null,
                    Zurueck = zurueck ? z.Zeit : null, ZurueckVon = zurueck ? z.Kuerzel : null,
                });
            }
        return liste;
    }

    // Demo: „VW Bus“ und „Caddy“, eine Fahrt gerade unterwegs, eine gestern erledigt, eine geplant.
    private void DemoFahrtenEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM fahrten; DELETE FROM fahrzeuge;", transaktion);
        var demo = Demodaten.Fahrten(bewohner, Uhr());
        for (int i = 0; i < demo.Fahrzeuge.Count; i++)
            Ausfuehren("INSERT INTO fahrzeuge (name, nr) VALUES ($n, $i)", transaktion, ("$n", demo.Fahrzeuge[i]), ("$i", i + 1));
        foreach (var (f, los, zurueck) in demo.Fahrten)
        {
            long id = FahrtEinfuegen(f, transaktion);
            if (los is DateTime l)
                Ausfuehren("INSERT INTO fahrt_ereignisse (fahrt_id, art, zeit, kuerzel) VALUES ($f, 'los', $z, $k)", transaktion, ("$f", id), ("$z", Zeit(l)), ("$k", f.Fahrer));
            if (zurueck is DateTime z)
                Ausfuehren("INSERT INTO fahrt_ereignisse (fahrt_id, art, zeit, kuerzel) VALUES ($f, 'zurueck', $z, $k)", transaktion, ("$f", id), ("$z", Zeit(z)), ("$k", f.Fahrer));
        }
        SetzeMeta(DemoFahrten, "1", transaktion);
    }
}
