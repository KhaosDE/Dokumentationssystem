using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Monatsreflexion im Speicher: je Bewohner und Monat Fassungen (UPDATE verboten).
public sealed partial class Speicher
{
    private static string MonatSchluessel(DateOnly monat) => monat.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public Reflexion Reflexion(long bewohnerId, DateOnly monat)
    {
        monat = ReflexionRegeln.MonatVon(monat);
        return new Reflexion(bewohnerId, monat, ReflexionFassungen("WHERE bewohner_id = $b AND monat = $m", null, ("$b", bewohnerId), ("$m", MonatSchluessel(monat))));
    }

    // Alle Monate eines Bewohners mit Reflexion, ältester zuerst (für den HEB-Bogen).
    public List<Reflexion> Reflexionen(long bewohnerId) =>
        ReflexionFassungen("WHERE bewohner_id = $b", null, ("$b", bewohnerId))
            .GroupBy(f => f.Monat).OrderBy(g => g.Key)
            .Select(g => new Reflexion(bewohnerId, g.Key, g.ToList())).ToList();

    // Neue Fassung; ist nichts geändert (Texte und Status), entsteht keine.
    public Reflexion ReflexionSpeichern(ReflexionFassung f, int? erwartet = null)
    {
        if (Bewohner(f.BewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        var bisher = Reflexion(f.BewohnerId, f.Monat);
        if (bisher.Aktuell is ReflexionFassung alt && ReflexionRegeln.Gleich(alt, f)) return bisher;
        if (bisher.Aktuell is null && !f.Fertig && f.Texte.All(t => t.Length == 0)) return bisher;
        using var transaktion = db.BeginTransaction();
        FassungenPruefen(transaktion, "SELECT COUNT(*) FROM reflexionen WHERE bewohner_id = $b AND monat = $m", erwartet,
            () => Reflexion(f.BewohnerId, f.Monat).Aktuell is ReflexionFassung a ? FassungText(a.Kuerzel, a.Erstellt, ReflexionRegeln.AlsText(a)) : "",
            FassungText(f.Kuerzel, f.Erstellt, ReflexionRegeln.AlsText(f)), ("$b", f.BewohnerId), ("$m", MonatSchluessel(ReflexionRegeln.MonatVon(f.Monat))));
        ReflexionEinfuegen(f, transaktion);
        transaktion.Commit();
        return Reflexion(f.BewohnerId, f.Monat);
    }

    // Termine des Wochenplans in einem Monat (alle Wochen, die den Monat berühren).
    public List<Termin> TermineImMonat(long bewohnerId, DateOnly monat)
    {
        var von = ReflexionRegeln.MonatVon(monat);
        var bis = ReflexionRegeln.MonatBis(von);
        var liste = new List<Termin>();
        for (var montag = WochenplanRegeln.Montag(von); montag <= bis; montag = montag.AddDays(7))
            liste.AddRange(Woche(bewohnerId, montag).Where(t => t.Tag >= von && t.Tag <= bis));
        return liste;
    }

    private List<ReflexionFassung> ReflexionFassungen(string bedingung, SqliteTransaction? transaktion, params (string, object?)[] parameter)
    {
        var liste = new List<ReflexionFassung>();
        using var befehl = Befehl($"SELECT bewohner_id, monat, texte, fertig, erstellt, kuerzel FROM reflexionen {bedingung} ORDER BY id", transaktion, parameter);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new ReflexionFassung
            {
                BewohnerId = leser.GetInt64(0),
                Monat = DateOnly.ParseExact(leser.GetString(1) + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Texte = JsonSerializer.Deserialize<List<string>>(leser.GetString(2)) ?? [],
                Fertig = leser.GetInt64(3) == 1,
                Erstellt = AusZeit(leser.GetString(4)),
                Kuerzel = leser.GetString(5),
            });
        return liste;
    }

    private void ReflexionEinfuegen(ReflexionFassung f, SqliteTransaction transaktion) =>
        Ausfuehren("INSERT INTO reflexionen (bewohner_id, monat, texte, fertig, erstellt, kuerzel) VALUES ($b, $m, $t, $f, $e, $k)", transaktion,
            ("$b", f.BewohnerId), ("$m", MonatSchluessel(f.Monat)), ("$t", JsonSerializer.Serialize(f.Texte)), ("$f", f.Fertig ? 1 : 0),
            ("$e", Zeit(f.Erstellt)), ("$k", f.Kuerzel));

    // Demo: ausgedachte, kurze Reflexionen für den vergangenen Monat (einige fertig, eine Entwurf).
    private void DemoReflexionEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM reflexionen", transaktion);
        foreach (var f in Demodaten.Reflexionen(bewohner, Uhr()))
            ReflexionEinfuegen(f, transaktion);
        SetzeMeta(DemoReflexion, "1", transaktion);
    }
}
