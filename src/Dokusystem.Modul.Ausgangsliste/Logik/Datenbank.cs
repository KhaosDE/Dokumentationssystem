using System.Globalization;
using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Modul.Ausgangsliste;

// Der Speicher des Moduls: eigene Tabellen (Vorsilbe „al_“) in der Datenbank des Kerns, die
// Bewohner liest das Modul aus dem Kern. Übernommen aus der Ausgangsliste (Datenbank.cs) und
// angepasst: keine eigene Datei, kein Mehrplatz-Schutz, Einstellungen in den Verwaltungsangaben
// des Kerns. Änderungen am Platz prüfen hier die Regeln und schreiben den Verlauf.
public sealed class Datenbank(Speicher speicher, SqliteTransaction? transaktion = null)
{
    public Speicher Speicher => speicher;
    public DateTime Jetzt() => speicher.Uhr();

    // Aufbau der Tabellen (Speicher.ModulAnmelden). Neue Schritte nur anhängen.
    public static readonly string[] Schritte =
    [
        """
        CREATE TABLE al_plaetze (
            bewohner_id     INTEGER PRIMARY KEY REFERENCES bewohner(id) ON DELETE CASCADE,
            ausgang         TEXT NOT NULL DEFAULT '',
            status          INTEGER NOT NULL DEFAULT 0 CHECK (status IN (0, 1, 2)),
            status_grund    TEXT NOT NULL DEFAULT '',
            forensik        INTEGER NOT NULL DEFAULT 0 CHECK (forensik IN (0, 1)),
            einverstaendnis TEXT
        );
        CREATE TABLE al_verlauf (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            zeitpunkt   TEXT NOT NULL,
            kuerzel     TEXT NOT NULL DEFAULT '',
            text        TEXT NOT NULL
        );
        CREATE TRIGGER al_verlauf_bleibt BEFORE UPDATE ON al_verlauf
            BEGIN SELECT RAISE(ABORT, 'Der Verlauf wird nicht überschrieben.'); END;
        CREATE TABLE al_kuechendienst (
            datum  TEXT PRIMARY KEY,
            zimmer TEXT NOT NULL,
            tausch TEXT NOT NULL DEFAULT ''
        );
        CREATE TABLE al_ausgaenge (
            id          INTEGER PRIMARY KEY,
            bewohner_id INTEGER NOT NULL REFERENCES bewohner(id) ON DELETE CASCADE,
            datum       TEXT NOT NULL,
            beginn      TEXT NOT NULL,
            ende        TEXT,
            art         TEXT NOT NULL CHECK (art IN ('allein', 'mit Bewohner')),
            hdz_beginn  TEXT NOT NULL DEFAULT '',
            hdz_ende    TEXT NOT NULL DEFAULT ''
        );
        """,
    ];

    // ------------------------------------------------------------------ Plätze

    // Alle Bewohner mit den Angaben des Moduls, nach Zimmer.
    public List<Platz> Laden()
    {
        var bewohner = transaktion is null ? speicher.AlleBewohner() : speicher.AlleBewohner(transaktion);
        var angaben = AlleAngaben();
        return bewohner.Select(b => Ausgangsliste.Platz.Aus(b, angaben.GetValueOrDefault(b.Id!.Value)))
            .OrderBy(p => int.TryParse(p.Zimmernummer, out int n) ? n : int.MaxValue)
            .ThenBy(p => p.Zimmernummer, StringComparer.CurrentCulture)
            .ThenBy(p => p.Bewohnername, StringComparer.CurrentCulture)
            .ToList();
    }

    public Platz? Platz(long bewohnerId) => Laden().FirstOrDefault(p => p.BewohnerId == bewohnerId);

    public PlatzAngaben Angaben(long bewohnerId) => AlleAngaben().GetValueOrDefault(bewohnerId) ?? new PlatzAngaben();

    private Dictionary<long, PlatzAngaben> AlleAngaben()
    {
        var liste = new Dictionary<long, PlatzAngaben>();
        using var befehl = Befehl("SELECT bewohner_id, ausgang, status, status_grund, forensik, einverstaendnis FROM al_plaetze");
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste[leser.GetInt64(0)] = new PlatzAngaben
            {
                Ausgang = leser.GetString(1),
                Status = (AnwesenheitsStatus)leser.GetInt32(2),
                StatusGrund = leser.GetString(3),
                Forensik = leser.GetInt64(4) == 1,
                Einverstaendnis = leser.IsDBNull(5) ? null : DateOnly.ParseExact(leser.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            };
        return liste;
    }

    public void AngabenSpeichern(long bewohnerId, PlatzAngaben a) =>
        Ausfuehren("""
            INSERT INTO al_plaetze (bewohner_id, ausgang, status, status_grund, forensik, einverstaendnis)
            VALUES ($b, $a, $s, $g, $f, $e)
            ON CONFLICT(bewohner_id) DO UPDATE SET ausgang = excluded.ausgang, status = excluded.status,
                status_grund = excluded.status_grund, forensik = excluded.forensik, einverstaendnis = excluded.einverstaendnis
            """,
            ("$b", bewohnerId), ("$a", a.Ausgang), ("$s", (int)a.Status), ("$g", a.StatusGrund), ("$f", a.Forensik ? 1 : 0),
            ("$e", a.Einverstaendnis is DateOnly e ? e.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null));

    private Platz Pflicht(long bewohnerId) =>
        Platz(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);

    // ------------------------------------------------------------------ Änderungen mit Regeln und Verlauf

    // Ausgangszeiten (AusgangFenster): neuer Text der Ausgangsspalte.
    public Platz AusgangSetzen(long bewohnerId, string neu, string kuerzel)
    {
        var platz = Pflicht(bewohnerId);
        neu = AusgangText.Zusammenfuegen(AusgangText.Bausteine(neu));
        if (neu == platz.Ausgang) return platz;
        AngabenSpeichern(bewohnerId, Angaben(bewohnerId) with { Ausgang = neu });
        VerlaufSchreiben(bewohnerId, $"Ausgang: {Verlauf.Ausgang(platz.Ausgang)} → {Verlauf.Ausgang(neu)}", kuerzel);
        return Pflicht(bewohnerId);
    }

    // Anwesenheit (StatusFenster): Ausgangsbeschränkung, Abwesenheit oder aufheben (Keiner).
    public Platz StatusSetzen(long bewohnerId, AnwesenheitsStatus status, string grund, string kuerzel)
    {
        var platz = Pflicht(bewohnerId);
        grund = grund.Trim();
        var fehler = new List<string>();
        if (status == AnwesenheitsStatus.Ausgangsbeschraenkung && !platz.AusgangsbeschraenkungErlaubt)
            fehler.Add(platz.Unterbringung == Unterbringung.NichtErfasst ? Ausgangsliste.Platz.HinweisNichtErfasst : Ausgangsliste.Platz.HinweisNurMitBeschluss);
        if (status != AnwesenheitsStatus.Keiner && grund == "")
            fehler.Add("Bitte zuerst einen Grund auswählen oder eintragen.");
        if (status == AnwesenheitsStatus.Abwesenheit &&
            (grund.StartsWith("Ausgangsbeschränkung", StringComparison.OrdinalIgnoreCase) ||
             grund.StartsWith("Neuaufnahme", StringComparison.OrdinalIgnoreCase)))
            fehler.Add("Dieser Grund gehört zur Ausgangsbeschränkung, nicht zur Abwesenheit.");
        if (status != AnwesenheitsStatus.Keiner && platz.HatStatus)
            fehler.Add("Es läuft schon ein Status. Bitte erst aufheben.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);

        if (status == AnwesenheitsStatus.Keiner)
        {
            if (!platz.HatStatus) return platz;
            AngabenSpeichern(bewohnerId, Angaben(bewohnerId) with { Status = AnwesenheitsStatus.Keiner, StatusGrund = "" });
            VerlaufSchreiben(bewohnerId, $"Status aufgehoben (war: {Verlauf.Status(platz.Status)} – {platz.StatusGrund})", kuerzel);
        }
        else
        {
            AngabenSpeichern(bewohnerId, Angaben(bewohnerId) with { Status = status, StatusGrund = grund });
            VerlaufSchreiben(bewohnerId, $"Status: {Verlauf.Status(status)} – {grund}", kuerzel);
        }
        return Pflicht(bewohnerId);
    }

    // Unterbringung (UnterbringungFenster). Beschluss: datum = Ablaufdatum, steht danach in der
    // Akte. Offen geführt: datum = Unterschrift unter die Einverständniserklärung. Forensik: kein
    // Datum. Wechsel zu offen geführt hebt eine laufende Ausgangsbeschränkung auf (Rechtsregel).
    public Platz UnterbringungSetzen(long bewohnerId, Unterbringung neu, DateOnly? datum, string kuerzel)
    {
        var platz = Pflicht(bewohnerId);
        var b = speicher.Bewohner(bewohnerId)!;
        if (neu == Unterbringung.NichtErfasst) throw new Eingabefehler(["Bitte Beschluss, offen geführt oder Forensik wählen."]);
        if (neu is Unterbringung.Beschluss or Unterbringung.OffenGefuehrt && datum is null)
            throw new Eingabefehler([neu == Unterbringung.Beschluss ? "Beschlussende fehlt." : "Datum der Einverständniserklärung fehlt."]);

        var angaben = Angaben(bewohnerId);
        speicher.BewohnerSpeichern(neu == Unterbringung.Beschluss
            ? b with { Beschluss = BeschlussZustand.Ende, Beschlussende = datum }
            : b with { Beschluss = BeschlussZustand.KeinBeschluss, Beschlussende = null });
        angaben = angaben with
        {
            Forensik = neu == Unterbringung.Forensik,
            Einverstaendnis = neu == Unterbringung.OffenGefuehrt ? datum : null,
        };
        bool aufheben = neu == Unterbringung.OffenGefuehrt && platz.Status == AnwesenheitsStatus.Ausgangsbeschraenkung;
        if (aufheben) angaben = angaben with { Status = AnwesenheitsStatus.Keiner, StatusGrund = "" };
        AngabenSpeichern(bewohnerId, angaben);

        var danach = Pflicht(bewohnerId);
        VerlaufSchreiben(bewohnerId, $"Unterbringung: {Verlauf.UnterbringungMitDatum(platz)} → {Verlauf.UnterbringungMitDatum(danach)}", kuerzel);
        if (aufheben)
            VerlaufSchreiben(bewohnerId, $"Status aufgehoben (war: Ausgangsbeschränkung – {platz.StatusGrund}), weil offen geführt", kuerzel);
        return danach;
    }

    // ------------------------------------------------------------------ Verlauf

    public void VerlaufSchreiben(long bewohnerId, string text, string kuerzel, DateTime? zeitpunkt = null) =>
        Ausfuehren("INSERT INTO al_verlauf (bewohner_id, zeitpunkt, kuerzel, text) VALUES ($b, $z, $k, $t)",
            ("$b", bewohnerId), ("$z", Zeit(zeitpunkt ?? Jetzt())), ("$k", kuerzel.Trim()), ("$t", text));

    public List<VerlaufEintrag> VerlaufLaden(long bewohnerId)
    {
        var liste = new List<VerlaufEintrag>();
        using var befehl = Befehl("SELECT zeitpunkt, kuerzel, text FROM al_verlauf WHERE bewohner_id = $b ORDER BY zeitpunkt, id", ("$b", bewohnerId));
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(new VerlaufEintrag(AusZeit(leser.GetString(0)), leser.GetString(1), leser.GetString(2)));
        return liste;
    }

    // ------------------------------------------------------------------ Einstellungen des Moduls

    private static string Meta(string schluessel) => $"ausgangsliste:{schluessel}";

    public string EinstellungLesen(string schluessel, string vorgabe) =>
        Wert("SELECT wert FROM meta WHERE schluessel = $s", ("$s", Meta(schluessel))) as string ?? vorgabe;

    public void EinstellungSchreiben(string schluessel, string wert) =>
        Ausfuehren("INSERT INTO meta (schluessel, wert) VALUES ($s, $w) ON CONFLICT(schluessel) DO UPDATE SET wert = excluded.wert",
            ("$s", Meta(schluessel)), ("$w", wert));

    // ------------------------------------------------------------------ Küchendienst

    public List<KuechendienstTag> KuechendienstLaden()
    {
        var liste = new List<KuechendienstTag>();
        using var befehl = Befehl("SELECT datum, zimmer, tausch FROM al_kuechendienst ORDER BY datum");
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new KuechendienstTag { Datum = AusTag(leser.GetString(0)), Zimmer = leser.GetString(1), Tausch = leser.GetString(2) });
        return liste;
    }

    // Ersetzt alle Tage ab abDatum durch die neuen (vergangene Tage bleiben).
    public void KuechendienstAbErsetzen(DateTime abDatum, IEnumerable<KuechendienstTag> neueTage)
    {
        Ausfuehren("DELETE FROM al_kuechendienst WHERE datum >= $d", ("$d", Tag(abDatum)));
        foreach (var t in neueTage)
            Ausfuehren("INSERT INTO al_kuechendienst (datum, zimmer, tausch) VALUES ($d, $z, $t)",
                ("$d", Tag(t.Datum)), ("$z", t.Zimmer), ("$t", t.Tausch));
    }

    public void KuechendienstTauschSetzen(DateTime datum, string tausch) =>
        Ausfuehren("UPDATE al_kuechendienst SET tausch = $t WHERE datum = $d", ("$d", Tag(datum)), ("$t", tausch.Trim()));

    // ------------------------------------------------------------------ Digitaler Ausgang

    public List<AusgangEintrag> AusgaengeLaden(DateTime datum)
    {
        var liste = new List<AusgangEintrag>();
        using var befehl = Befehl("SELECT id, bewohner_id, datum, beginn, ende, art, hdz_beginn, hdz_ende FROM al_ausgaenge WHERE datum = $d ORDER BY beginn, id",
            ("$d", Tag(datum)));
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new AusgangEintrag
            {
                Id = leser.GetInt64(0), BewohnerId = leser.GetInt64(1), Datum = AusTag(leser.GetString(2)),
                Beginn = TimeSpan.ParseExact(leser.GetString(3), "hh\\:mm", CultureInfo.InvariantCulture),
                Ende = leser.IsDBNull(4) ? null : TimeSpan.ParseExact(leser.GetString(4), "hh\\:mm", CultureInfo.InvariantCulture),
                Art = leser.GetString(5), HdzBeginn = leser.GetString(6), HdzEnde = leser.GetString(7),
            });
        return liste;
    }

    public long AusgangEinfuegen(AusgangEintrag e)
    {
        Ausfuehren("INSERT INTO al_ausgaenge (bewohner_id, datum, beginn, ende, art, hdz_beginn, hdz_ende) VALUES ($b, $d, $v, $e, $a, $h1, $h2)",
            ("$b", e.BewohnerId), ("$d", Tag(e.Datum)), ("$v", DigitalerAusgang.Uhrzeit(e.Beginn)),
            ("$e", e.Ende is TimeSpan ende ? DigitalerAusgang.Uhrzeit(ende) : null), ("$a", e.Art), ("$h1", e.HdzBeginn), ("$h2", e.HdzEnde));
        return Convert.ToInt64(Wert("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    // Ausgang beginnen (Regeln in DigitalerAusgang.BeginnenNichtMoeglich), mit Verlauf.
    public AusgangEintrag AusgangBeginnen(long bewohnerId, string art, string handzeichen)
    {
        var platz = Pflicht(bewohnerId);
        var jetzt = Jetzt();
        var stand = DigitalerAusgang.StandFuer(platz, AusgaengeLaden(jetzt), jetzt.TimeOfDay);
        var fehler = new List<string>();
        if (!DigitalerAusgang.Arten.Contains(art)) fehler.Add("Unbekannte Art des Ausgangs.");
        else if (DigitalerAusgang.BeginnenNichtMoeglich(platz, stand, art) is string grund) fehler.Add(grund);
        if (string.IsNullOrWhiteSpace(handzeichen)) fehler.Add("Handzeichen fehlt.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        var e = new AusgangEintrag
        {
            BewohnerId = bewohnerId, Datum = jetzt.Date, Beginn = new TimeSpan(jetzt.Hour, jetzt.Minute, 0), Art = art,
            HdzBeginn = handzeichen.Trim().ToUpperInvariant(),
        };
        e.Id = AusgangEinfuegen(e);
        VerlaufSchreiben(bewohnerId, $"Ausgang begonnen: {e.Zeitraum}", e.HdzBeginn);
        return e;
    }

    public void AusgangBeenden(long id, string handzeichen)
    {
        var jetzt = Jetzt();
        var e = AusgaengeLaden(jetzt).FirstOrDefault(x => x.Id == id && x.Unterwegs)
                ?? throw new Eingabefehler(["Dieser Ausgang ist schon beendet."]);
        if (string.IsNullOrWhiteSpace(handzeichen)) throw new Eingabefehler(["Handzeichen fehlt."]);
        var ende = new TimeSpan(jetzt.Hour, jetzt.Minute, 0);
        if (ende < e.Beginn) ende = e.Beginn;
        e.Ende = ende;
        e.HdzEnde = handzeichen.Trim().ToUpperInvariant();
        Ausfuehren("UPDATE al_ausgaenge SET ende = $e, hdz_ende = $h WHERE id = $id",
            ("$e", DigitalerAusgang.Uhrzeit(ende)), ("$h", e.HdzEnde), ("$id", id));
        VerlaufSchreiben(e.BewohnerId, $"Ausgang beendet: {e.Zeitraum} (Hz. {e.Handzeichen})", e.HdzEnde);
    }

    // Aufbewahrung: Ältere digitale Ausgänge werden gelöscht.
    public void AusgaengeLoeschenVor(DateTime stichtag) =>
        Ausfuehren("DELETE FROM al_ausgaenge WHERE datum < $d", ("$d", Tag(stichtag)));

    // ------------------------------------------------------------------ Demo-Daten

    public void Leeren()
    {
        Ausfuehren("DELETE FROM al_plaetze");
        Ausfuehren("DELETE FROM al_verlauf");
        Ausfuehren("DELETE FROM al_kuechendienst");
        Ausfuehren("DELETE FROM al_ausgaenge");
        Ausfuehren("DELETE FROM meta WHERE schluessel LIKE 'ausgangsliste:%'");
    }

    // ------------------------------------------------------------------ Hilfen

    private static string Zeit(DateTime t) => t.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    private static DateTime AusZeit(string s) => DateTime.ParseExact(s, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    private static string Tag(DateTime t) => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateTime AusTag(string s) => DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private SqliteCommand Befehl(string sql, params (string, object?)[] parameter)
    {
        var befehl = speicher.Verbindung.CreateCommand();
        befehl.CommandText = sql;
        befehl.Transaction = transaktion;
        foreach (var (name, wert) in parameter) befehl.Parameters.AddWithValue(name, wert ?? DBNull.Value);
        return befehl;
    }

    private void Ausfuehren(string sql, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, parameter);
        befehl.ExecuteNonQuery();
    }

    private object? Wert(string sql, params (string, object?)[] parameter)
    {
        using var befehl = Befehl(sql, parameter);
        return befehl.ExecuteScalar();
    }
}
