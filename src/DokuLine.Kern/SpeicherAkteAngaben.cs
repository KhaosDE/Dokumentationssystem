using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Akte, Reiter Betreuung, Angehörige, Ärzte, Krankenkasse, Kostenträger: Fassungen lesen und anfügen (nie ändern).
public sealed partial class Speicher
{
    private const string DemoAkte = "demo_akte_angaben", DemoSchweigepflicht = "demo_schweigepflicht";
    public event Action? AngabenGeaendert;

    public List<AngabeFassung> Fassungen(long bewohnerId, Angabe art) => FassungenIn(null, bewohnerId, art);

    private List<AngabeFassung> FassungenIn(SqliteTransaction? transaktion, long? bewohnerId, Angabe? art)
    {
        var liste = new List<AngabeFassung>();
        using var befehl = Befehl("""
            SELECT id, bewohner_id, art, zustand, inhalt, ab, geaendert, kuerzel FROM akte_angaben
            WHERE ($b IS NULL OR bewohner_id = $b) AND ($a IS NULL OR art = $a) ORDER BY ab, id
            """, transaktion, [("$b", bewohnerId), ("$a", art is Angabe x ? AngabenRegeln.Schluessel(x) : null)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new AngabeFassung(leser.GetInt64(0), leser.GetInt64(1), Enum.GetValues<Angabe>().First(a => AngabenRegeln.Schluessel(a) == leser.GetString(2)),
                leser.GetString(3) == "keine" ? AngabeZustand.Keine : AngabeZustand.Eingetragen, leser.GetString(4),
                DateOnly.ParseExact(leser.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture), AusZeit(leser.GetString(6)), leser.GetString(7)));
        return liste;
    }

    // Die geltenden Fassungen eines Bewohners je Reiter (fehlt einer: nicht erfasst).
    public Dictionary<Angabe, AngabeFassung> Angaben(long bewohnerId, DateOnly heute) =>
        FassungenIn(null, bewohnerId, null).GroupBy(f => f.Art)
            .Select(g => AngabenRegeln.Aktuell(g, heute)).OfType<AngabeFassung>().ToDictionary(f => f.Art);

    // Für die Übersicht: die geltenden Fassungen aller Bewohner auf einmal.
    public Dictionary<long, Dictionary<Angabe, AngabeFassung>> AlleAngaben(DateOnly heute)
    {
        return FassungenIn(null, null, null).GroupBy(f => f.BewohnerId).ToDictionary(g => g.Key,
            g => g.GroupBy(f => f.Art).Select(x => AngabenRegeln.Aktuell(x, heute)).OfType<AngabeFassung>().ToDictionary(f => f.Art));
    }

    // Eine neue Fassung (inhalt null: „keine“). Recht „Bewohner anlegen und bearbeiten“. Bei der Betreuung bekommt
    // auch die bisherige Betreuer-Angabe eine Fassung (E-Mail an Betreuer, ältere Auswertungen).
    public AngabeFassung AngabeSpeichern(long bewohnerId, Angabe art, object? inhalt, DateOnly ab, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        if (Grund(kuerzel, Funktionen.BewohnerBearbeiten) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (inhalt is not null && AngabenRegeln.Pruefen(art, inhalt) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        long id;
        var betreuer = (inhalt as Betreuung)?.PersonId is long pid ? PersonNach(pid) : null;
        using (var transaktion = db.BeginTransaction())
        {
            id = AngabeEinfuegen(bewohnerId, art, inhalt, ab, kuerzel.Trim(), jetzt, transaktion);
            if (art == Angabe.Betreuung) BetreuerAngleichen(bewohnerId, inhalt is null, betreuer, kuerzel.Trim(), jetzt, transaktion);
            transaktion.Commit();
        }
        AngabenGeaendert?.Invoke();
        if (art == Angabe.Betreuung) BetreuerGeaendertMelden();
        Zugriff(kuerzel, bewohnerId, ZugriffRegeln.Geaendert, $"Akte: {AngabenRegeln.Name(art)}");
        return FassungenIn(null, bewohnerId, art).Single(f => f.Id == id);
    }

    private long AngabeEinfuegen(long bewohnerId, Angabe art, object? inhalt, DateOnly ab, string kuerzel, DateTime jetzt, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO akte_angaben (bewohner_id, art, zustand, inhalt, ab, geaendert, kuerzel) VALUES ($b, $a, $z, $i, $ab, $g, $k)
            """, transaktion, ("$b", bewohnerId), ("$a", AngabenRegeln.Schluessel(art)), ("$z", inhalt is null ? "keine" : "eingetragen"),
            ("$i", inhalt is null ? "" : System.Text.Json.JsonSerializer.Serialize(inhalt, inhalt.GetType())),
            ("$ab", ab.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), ("$g", Zeit(jetzt)), ("$k", kuerzel));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    private void BetreuerAngleichen(long bewohnerId, bool keine, Person? p, string kuerzel, DateTime jetzt, SqliteTransaction transaktion)
    {
        if (keine) { BetreuerEinfuegen(bewohnerId, new Betreuer { Keine = true, Geaendert = jetzt, Kuerzel = kuerzel }, transaktion); return; }
        if (p is null) return;
        BetreuerEinfuegen(bewohnerId, new Betreuer { Name = p.Name, Stelle = p.Praxis, Email = p.Email, Telefon = p.Telefon, Geaendert = jetzt, Kuerzel = kuerzel },
            transaktion);
    }

    private void BetreuerGeaendertMelden() => BetreuerGeaendert?.Invoke();

    // Demo: alle fünf Reiter für alle Bewohner, gemischt mit „keine“ und „nicht erfasst“, Fristen relativ zu heute.
    private void DemoAkteAngabenEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM akte_angaben", transaktion);
        foreach (var name in Demodaten.Krankenkassen)
            Ausfuehren("INSERT OR IGNORE INTO krankenkassen (name, nr) VALUES ($n, (SELECT COALESCE(MAX(nr), 0) + 1 FROM krankenkassen))", transaktion, ("$n", name));
        foreach (var name in Demodaten.Kostentraeger)
            Ausfuehren("INSERT OR IGNORE INTO kostentraeger (name, nr) VALUES ($n, (SELECT COALESCE(MAX(nr), 0) + 1 FROM kostentraeger))", transaktion, ("$n", name));
        var jetzt = Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        var (bewohner, angaben) = DemoAkteDaten(transaktion, heute);
        var ab = heute.AddDays(-30);
        foreach (var (art, bewohnerId, inhalt) in angaben)
            AngabeEinfuegen(bewohnerId, art, inhalt, ab, "EF", jetzt.AddDays(-30), transaktion);
        foreach (var (bewohnerId, beschreibung, tage) in Demodaten.Personenbeschreibungen(bewohner))
            AngabeEinfuegen(bewohnerId, Angabe.Personenbeschreibung, beschreibung, heute.AddDays(-tage), "EF", jetzt.AddDays(-tage), transaktion);
        SetzeMeta(DemoAkte, "1", transaktion);
    }

    // Die Demo-Angaben, wie sie aus Personen, Zuordnungen und Betreuern entstehen (immer gleich, daher auch zum Nachholen).
    private (List<Bewohner> Bewohner, List<(Angabe Art, long BewohnerId, object? Inhalt)> Angaben) DemoAkteDaten(SqliteTransaction transaktion, DateOnly heute)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        bewohner.Sort(BewohnerRegeln.NachName);
        var personen = new List<Person>();
        using (var befehl = Befehl("SELECT id, gruppe, name, fachrichtung FROM aerzte WHERE aktiv = 1 ORDER BY id", transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                personen.Add(new Person { Id = leser.GetInt64(0), Gruppe = PersonRegeln.Gruppen.First(g => PersonRegeln.Schluessel(g) == leser.GetString(1)), Name = leser.GetString(2), Fachrichtung = leser.GetString(3) });
        var zuordnungen = new List<(long Person, long Bewohner, string Rolle)>();
        using (var befehl = Befehl("SELECT person_id, bewohner_id, rolle FROM person_zuordnungen ORDER BY id", transaktion, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read()) zuordnungen.Add((leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2)));
        var betreuer = new Dictionary<long, Betreuer>();
        foreach (var b in bewohner)
            if (BetreuerFassungenIn(transaktion, b.Id!.Value).LastOrDefault() is Betreuer x) betreuer[b.Id!.Value] = x;
        return (bewohner, [.. Demodaten.AkteAngaben(bewohner, personen, zuordnungen, betreuer, heute)]);
    }

    // Demo Schweigepflicht (Bauplan 35): eigener Schlüssel, damit vorhandene Datenbanken sie nach dem Update bekommen.
    private void DemoSchweigepflichtEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM akte_angaben WHERE art = 'schweigepflicht'", transaktion);
        var jetzt = Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        var (bewohner, angaben) = DemoAkteDaten(transaktion, heute);
        foreach (var (bewohnerId, inhalt) in Demodaten.Schweigepflichten(bewohner, angaben, heute))
            AngabeEinfuegen(bewohnerId, Angabe.Schweigepflicht, inhalt, heute.AddDays(-30), "EF", jetzt.AddDays(-30), transaktion);
        SetzeMeta(DemoSchweigepflicht, "1", transaktion);
    }
}
