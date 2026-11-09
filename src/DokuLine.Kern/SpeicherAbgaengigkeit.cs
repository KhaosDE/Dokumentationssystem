using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Abgängigkeitsmeldungen speichern und lesen (Bauplan 34).
public sealed partial class Speicher
{
    public event Action? AbgaengigkeitGeaendert;

    public List<Abgaengigkeit> Abgaengigkeiten(long? bewohnerId = null)
    {
        var ereignisse = new Dictionary<long, List<(string Art, string Stelle, DateTime Zeit, string Kuerzel, string Hinweis)>>();
        using (var befehl = Befehl("SELECT abgaengigkeit_id, art, stelle, zeit, kuerzel, hinweis FROM abgaengigkeit_ereignisse ORDER BY id", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                if (!ereignisse.TryGetValue(leser.GetInt64(0), out var l)) ereignisse[leser.GetInt64(0)] = l = [];
                l.Add((leser.GetString(1), leser.GetString(2), AusZeit(leser.GetString(3)), leser.GetString(4), leser.GetString(5)));
            }
        var liste = new List<Abgaengigkeit>();
        using (var befehl = Befehl("SELECT id, bewohner_id, meldung, gemeldet, kuerzel FROM abgaengigkeiten WHERE $b IS NULL OR bewohner_id = $b ORDER BY id DESC",
                   null, [("$b", bewohnerId)]))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
            {
                var e = ereignisse.GetValueOrDefault(leser.GetInt64(0)) ?? [];
                var zurueck = e.LastOrDefault(x => x.Art == "zurueck");
                liste.Add(new Abgaengigkeit(leser.GetInt64(0), leser.GetInt64(1), JsonSerializer.Deserialize<AbgaengigkeitMeldung>(leser.GetString(2))!,
                    AusZeit(leser.GetString(3)), leser.GetString(4),
                    [.. e.Where(x => x.Art == "informiert").Select(x => new Benachrichtigung(x.Stelle, x.Zeit, x.Kuerzel, x.Hinweis))],
                    zurueck.Art is null ? null : zurueck.Zeit, zurueck.Hinweis ?? "",
                    [.. e.Where(x => x.Art == "entwarnt").Select(x => new Benachrichtigung(x.Stelle, x.Zeit, x.Kuerzel, x.Hinweis))]));
            }
        return liste;
    }

    public Abgaengigkeit? OffeneAbgaengigkeit(long bewohnerId) => Abgaengigkeiten(bewohnerId).FirstOrDefault(a => a.Offen);

    public Dictionary<long, Abgaengigkeit> OffeneAbgaengigkeiten() =>
        Abgaengigkeiten().Where(a => a.Offen).GroupBy(a => a.BewohnerId).ToDictionary(g => g.Key, g => g.First());

    public List<string> Meldestellen() => [.. Listeneintraege(Liste.Meldestellen).Where(e => e.Aktiv).Select(e => e.Name)];

    // Melden: die Meldung und ein wichtiger Bericht mit Übergabe (Berichtsart „Abgängigkeit“). Eine offene Meldung je Bewohner.
    public Abgaengigkeit AbgaengigkeitMelden(long bewohnerId, AbgaengigkeitMeldung m, string kuerzel, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        if (Grund(kuerzel, Funktionen.BerichteSchreiben) is string grund) throw new Eingabefehler([grund]);
        if (Bewohner(bewohnerId) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (OffeneAbgaengigkeit(bewohnerId) is not null) throw new Eingabefehler(["Für diesen Bewohner läuft schon eine Meldung."]);
        if (AbgaengigkeitRegeln.Pruefen(m, jetzt) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        long id;
        using (var transaktion = db.BeginTransaction())
        {
            if (WertIn(transaktion, """
                SELECT 1 FROM abgaengigkeiten a WHERE a.bewohner_id = $b
                    AND NOT EXISTS (SELECT 1 FROM abgaengigkeit_ereignisse e WHERE e.abgaengigkeit_id = a.id AND e.art = 'zurueck')
                """, ("$b", bewohnerId)) is not null)
                throw new Eingabefehler(["Für diesen Bewohner läuft schon eine Meldung."]);   // gleichzeitig von einem anderen Arbeitsplatz
            Ausfuehren("INSERT INTO abgaengigkeiten (bewohner_id, meldung, gemeldet, kuerzel) VALUES ($b, $m, $z, $k)", transaktion,
                ("$b", bewohnerId), ("$m", JsonSerializer.Serialize(m)), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()));
            id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            transaktion.Commit();
        }
        AbgaengigkeitBericht(bewohnerId, AbgaengigkeitRegeln.Berichttext(m), kuerzel, jetzt);
        AbgaengigkeitGeaendert?.Invoke();
        return Abgaengigkeiten(bewohnerId).Single(a => a.Id == id);
    }

    private void AbgaengigkeitBericht(long bewohnerId, string text, string kuerzel, DateTime jetzt)
    {
        var e = new BerichtEingabe
        {
            BewohnerId = bewohnerId, Datum = Datum.Text(DateOnly.FromDateTime(jetzt)), Uhrzeit = jetzt.ToString("HH:mm", CultureInfo.InvariantCulture),
            Verfasser = kuerzel, Allgemein = true, Text = text, Wichtig = true, Uebergabe = true, UebergabeDauer = UebergabeDauer.DreiTage,
            Art = Berichtsarten().Contains(AbgaengigkeitRegeln.Berichtsart) ? AbgaengigkeitRegeln.Berichtsart : DokuRegeln.ArtAlltag,
        };
        BerichtSpeichern(BerichtRegeln.AusEingabe(e, jetzt), BerichtRegeln.UebergabeAus(e));
    }

    private void AbgaengigkeitEreignis(long id, string art, string stelle, string kuerzel, string hinweis, DateTime jetzt, SqliteTransaction? transaktion = null)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        if (Grund(kuerzel, Funktionen.BerichteSchreiben) is string grund) throw new Eingabefehler([grund]);
        if (stelle.Trim().Length == 0 && art != "zurueck") throw new Eingabefehler(["Welche Stelle?"]);
        Ausfuehren("INSERT INTO abgaengigkeit_ereignisse (abgaengigkeit_id, art, stelle, zeit, kuerzel, hinweis) VALUES ($a, $r, $s, $z, $k, $h)", transaktion,
            ("$a", id), ("$r", art), ("$s", stelle), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()), ("$h", hinweis.Trim()));
        AbgaengigkeitGeaendert?.Invoke();
    }

    // „Wer wurde informiert“: je Stelle mit Uhrzeit und Kürzel; bei der Polizei z. B. Name und Vorgangsnummer als Hinweis.
    public void AbgaengigkeitInformiert(long id, string stelle, string hinweis, string kuerzel, DateTime jetzt) =>
        AbgaengigkeitEreignis(id, "informiert", stelle, kuerzel, hinweis, jetzt);

    public void AbgaengigkeitEntwarnt(long id, string stelle, string kuerzel, DateTime jetzt) =>
        AbgaengigkeitEreignis(id, "entwarnt", stelle, kuerzel, "", jetzt);

    // „Zurück“: schließt die Meldung und ergänzt den Bericht.
    public void AbgaengigkeitZurueck(long id, DateTime zurueck, string zustand, string kuerzel, DateTime jetzt)
    {
        var a = Abgaengigkeiten().FirstOrDefault(x => x.Id == id) ?? throw new Eingabefehler(["Diese Meldung gibt es nicht mehr."]);
        if (!a.Offen) throw new Eingabefehler(["Die Meldung ist schon geschlossen."]);
        if (zurueck < a.Meldung.VermisstSeit) throw new Eingabefehler(["„Zurück“ liegt vor „vermisst seit“."]);
        AbgaengigkeitEreignis(id, "zurueck", "", kuerzel, zustand, zurueck);
        AbgaengigkeitBericht(a.BewohnerId, AbgaengigkeitRegeln.Zuruecktext(zurueck, zustand), kuerzel, jetzt);
    }
}
