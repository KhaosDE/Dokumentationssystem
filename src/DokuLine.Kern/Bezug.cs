using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Bezugspflege und Einzeltrainer (Bauplan Schritt 20). Jeder Bewohner hat eine Bezugspflege und
// optional einen Einzeltrainer (DBT, Suchtgruppe, erweiterbar). Wechsel sind eigene Einträge mit
// Datum und Kürzel – nichts wird überschrieben, der Verlauf bleibt. Kürzel leer = keine/keiner.

public sealed record Zuordnung(long Id, long BewohnerId, string Kuerzel, string Art, DateOnly Ab, DateTime Erstellt, string Von);

public sealed record Bezug(Zuordnung? Bezugspflege, Zuordnung? Einzeltrainer);

public static class BezugRegeln
{
    public const int HinweisAb = 5;   // ab so vielen Bezugsbewohnern ein Hinweis, keine Grenze
    public static readonly string[] ArtenVorgabe = ["DBT", "Suchtgruppe"];

    // Feste, gut unterscheidbare Farben als Vorgabe, solange jemand keine eigene gewählt hat.
    private static readonly string[] Palette =
        ["#2F6FB7", "#2E8B57", "#C2410C", "#7C3AED", "#B91C1C", "#0F766E", "#A16207", "#BE185D", "#4D7C0F", "#1D4ED8", "#9333EA", "#0E7490"];

    public static string Vorgabefarbe(string kuerzel)
    {
        int summe = (kuerzel ?? "").ToUpperInvariant().Aggregate(0, (s, c) => s * 31 + c);
        return Palette[Math.Abs(summe % Palette.Length)];
    }

    public static string? FarbePruefen(string? farbe) =>
        farbe is null || (farbe.Length == 7 && farbe[0] == '#' && int.TryParse(farbe[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            ? null : "Farbe bitte als #RRGGBB.";

    // „DBT · AB“
    public static string TrainerText(Zuordnung z) => $"{z.Art} · {z.Kuerzel}";

    public static string Hinweis(string kuerzel, int anzahl) =>
        anzahl >= HinweisAb ? $"Hinweis: {kuerzel} ist dann Bezugspflege für {anzahl} Bewohner." : "";

    public static List<string> Verlauf(IEnumerable<Zuordnung> bezugspflege, IEnumerable<Zuordnung> trainer, IReadOnlyDictionary<string, string> namen)
    {
        string Wer(string k) => k.Length == 0 ? "keine" : namen.TryGetValue(k, out var n) ? $"{n} ({k})" : k;
        return bezugspflege.Select(z => (z, $"Bezugspflege: {Wer(z.Kuerzel)}"))
            .Concat(trainer.Select(z => (z, z.Kuerzel.Length == 0 ? "Einzeltrainer: keiner" : $"Einzeltrainer {z.Art}: {Wer(z.Kuerzel)}")))
            .OrderByDescending(x => x.z.Erstellt).ThenByDescending(x => x.z.Id)
            .Select(x => $"ab {Datum.Text(x.z.Ab)} · {x.Item2} · eingetragen {x.z.Erstellt:dd.MM.yyyy} von {x.z.Von}")
            .ToList();
    }
}

public sealed partial class Speicher
{
    private const string DemoBezug = "demo_bezug";

    public event Action? BezugGeaendert;

    public Bezug Bezug(long bewohnerId) => Bezuege().GetValueOrDefault(bewohnerId) ?? new Bezug(null, null);

    // Die geltenden Zuordnungen aller Bewohner (die neueste je Art; leeres Kürzel = keine).
    public Dictionary<long, Bezug> Bezuege()
    {
        var bp = Neueste("bezugspflege");
        var et = Neueste("einzeltrainer");
        return bp.Keys.Union(et.Keys).ToDictionary(id => id, id => new Bezug(
            bp.GetValueOrDefault(id) is { Kuerzel.Length: > 0 } b ? b : null,
            et.GetValueOrDefault(id) is { Kuerzel.Length: > 0 } t ? t : null));
    }

    private Dictionary<long, Zuordnung> Neueste(string tabelle) =>
        ZuordnungenLesen(tabelle, $"WHERE id IN (SELECT MAX(id) FROM {tabelle} GROUP BY bewohner_id)").ToDictionary(z => z.BewohnerId);

    public List<Zuordnung> Bezugspflegen(long bewohnerId) => ZuordnungenLesen("bezugspflege", "WHERE bewohner_id = $b", ("$b", bewohnerId));
    public List<Zuordnung> Einzeltrainer(long bewohnerId) => ZuordnungenLesen("einzeltrainer", "WHERE bewohner_id = $b", ("$b", bewohnerId));

    private List<Zuordnung> ZuordnungenLesen(string tabelle, string bedingung, params (string, object?)[] parameter)
    {
        string art = tabelle == "einzeltrainer" ? "art" : "''";
        var liste = new List<Zuordnung>();
        using var befehl = Befehl($"SELECT id, bewohner_id, kuerzel, {art}, ab, erstellt, von FROM {tabelle} {bedingung} ORDER BY id", null, parameter);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Zuordnung(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), leser.GetString(3),
                AusIso(leser.GetString(4)), AusZeit(leser.GetString(5)), leser.GetString(6)));
        return liste;
    }

    // Wie viele Bewohner jemand als Bezugspflege hat.
    public int Bezugsbewohner(string kuerzel) =>
        Bezuege().Values.Count(b => b.Bezugspflege?.Kuerzel.Equals(kuerzel, StringComparison.OrdinalIgnoreCase) == true);

    // Neue Bezugspflege (Kürzel leer = keine). Ist sie unverändert, entsteht kein Eintrag.
    public void BezugspflegeSetzen(long bewohnerId, string kuerzel, DateOnly ab, string von) =>
        ZuordnungSetzen("bezugspflege", bewohnerId, kuerzel, "", ab, von);

    public void EinzeltrainerSetzen(long bewohnerId, string kuerzel, string art, DateOnly ab, string von) =>
        ZuordnungSetzen("einzeltrainer", bewohnerId, kuerzel, kuerzel.Trim().Length == 0 ? "" : art, ab, von);

    private void ZuordnungSetzen(string tabelle, long bewohnerId, string kuerzel, string art, DateOnly ab, string von)
    {
        kuerzel = kuerzel.Trim();
        art = art.Trim();
        var fehler = new List<string>();
        if (Bewohner(bewohnerId) is null) fehler.Add("Diesen Bewohner gibt es nicht mehr.");
        if (string.IsNullOrWhiteSpace(von)) fehler.Add("Kürzel fehlt (oben rechts anmelden).");
        var person = kuerzel.Length > 0 ? MitarbeiterLesen(kuerzel) : null;
        if (kuerzel.Length > 0 && person is null) fehler.Add($"Das Kürzel „{kuerzel}“ ist nicht bekannt.");
        else if (person is { Aktiv: false }) fehler.Add($"{person.Name} ist ausgeschieden.");
        if (tabelle == "einzeltrainer" && kuerzel.Length > 0 && art.Length == 0) fehler.Add("Art des Einzeltrainings fehlt.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        var bisher = ZuordnungenLesen(tabelle, "WHERE bewohner_id = $b", ("$b", bewohnerId)).LastOrDefault();
        if (bisher is not null && bisher.Kuerzel.Equals(kuerzel, StringComparison.OrdinalIgnoreCase) && bisher.Art == art) return;
        if (bisher is null && kuerzel.Length == 0) return;
        if (person is not null) kuerzel = person.Kuerzel;
        using (var transaktion = db.BeginTransaction())
        {
            ZuordnungEinfuegen(tabelle, bewohnerId, kuerzel, art, ab, Uhr(), von.Trim(), transaktion);
            if (tabelle == "einzeltrainer" && art.Length > 0)
                Ausfuehren("INSERT OR IGNORE INTO einzeltrainer_arten (name, nr) VALUES ($n, (SELECT COALESCE(MAX(nr), 0) + 1 FROM einzeltrainer_arten))", transaktion, ("$n", art));
            transaktion.Commit();
        }
        BezugGeaendert?.Invoke();
    }

    private void ZuordnungEinfuegen(string tabelle, long bewohnerId, string kuerzel, string art, DateOnly ab, DateTime erstellt, string von, SqliteTransaction transaktion)
    {
        if (tabelle == "einzeltrainer")
            Ausfuehren("INSERT INTO einzeltrainer (bewohner_id, kuerzel, art, ab, erstellt, von) VALUES ($b, $k, $a, $ab, $e, $v)", transaktion,
                ("$b", bewohnerId), ("$k", kuerzel), ("$a", art), ("$ab", Iso(ab)), ("$e", Zeit(erstellt)), ("$v", von));
        else
            Ausfuehren("INSERT INTO bezugspflege (bewohner_id, kuerzel, ab, erstellt, von) VALUES ($b, $k, $ab, $e, $v)", transaktion,
                ("$b", bewohnerId), ("$k", kuerzel), ("$ab", Iso(ab)), ("$e", Zeit(erstellt)), ("$v", von));
    }

    public List<string> EinzeltrainerArten()
    {
        var liste = new List<string>();
        using var befehl = Befehl("SELECT name FROM einzeltrainer_arten ORDER BY nr, name", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(leser.GetString(0));
        return liste;
    }

    // ----- Farbe je Mitarbeiter (gehört zur Person, nicht zum Rechner) -----

    public string Farbe(string kuerzel) =>
        Wert("SELECT farbe FROM kollegen WHERE kuerzel = $k COLLATE NOCASE", ("$k", kuerzel)) as string ?? BezugRegeln.Vorgabefarbe(kuerzel);

    public void FarbeSetzen(string kuerzel, string? farbe)
    {
        if (BezugRegeln.FarbePruefen(farbe) is string f) throw new Eingabefehler([f]);
        if (MitarbeiterLesen(kuerzel) is null) throw new Eingabefehler(["Bitte zuerst anmelden."]);
        Ausfuehren("UPDATE kollegen SET farbe = $f WHERE kuerzel = $k COLLATE NOCASE", null, ("$f", farbe?.ToUpperInvariant()), ("$k", kuerzel));
        BezugGeaendert?.Invoke();
    }

    // Demo: Bezugspflege und Einzeltrainer aus daten/demo/bezug.json, Datum relativ zu heute.
    private void DemoBezugEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM bezugspflege; DELETE FROM einzeltrainer; DELETE FROM einzeltrainer_arten;", transaktion);
        for (int i = 0; i < BezugRegeln.ArtenVorgabe.Length; i++)
            Ausfuehren("INSERT INTO einzeltrainer_arten (name, nr) VALUES ($n, $i)", transaktion, ("$n", BezugRegeln.ArtenVorgabe[i]), ("$i", i + 1));
        var jetzt = Uhr();
        foreach (var d in Demodaten.Bezuege(bewohner, DateOnly.FromDateTime(jetzt)))
            ZuordnungEinfuegen(d.Trainer ? "einzeltrainer" : "bezugspflege", d.BewohnerId, d.Kuerzel, d.Art, d.Ab,
                d.Ab.ToDateTime(new TimeOnly(9, 0)) > jetzt ? jetzt : d.Ab.ToDateTime(new TimeOnly(9, 0)), d.Von, transaktion);
        SetzeMeta(DemoBezug, "1", transaktion);
    }
}
