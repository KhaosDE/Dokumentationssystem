using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Archiv im Speicher (Bauplan 46). Entlassen und Wiederaufnahme sind eigene Einträge (nie geändert); archiviert ist, wessen
// letzter Aufenthalt entlassen und nicht wieder aufgenommen ist (Sicht `archiviert`). AlleBewohner() lässt sie weg – so
// verschwinden sie aus Listen, Zählern und der Belegung, ohne dass jede Stelle davon wissen muss.
public sealed partial class Speicher
{
    private const string DemoArchiv = "demo_archiv";
    private const string ArchivFristSchluessel = "archiv_frist_jahre";

    public event Action? ArchivGeaendert;

    // Aufbewahrungsfrist in Jahren für das ganze Haus; null = nicht festgelegt (keine Vorgabe, Bauplan 46).
    public int? ArchivFrist() => int.TryParse(Meta(ArchivFristSchluessel), NumberStyles.None, CultureInfo.InvariantCulture, out int j) ? j : null;

    public void ArchivFristSetzen(int? jahre, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.ArchivLoeschen) is string grund) throw new Eingabefehler([grund]);
        if (jahre is < 1 or > ArchivRegeln.FristHoechstens) throw new Eingabefehler([$"Bitte 1 bis {ArchivRegeln.FristHoechstens} Jahre."]);
        if (jahre is int j) SetzeMeta(ArchivFristSchluessel, j.ToString(CultureInfo.InvariantCulture), null);
        else Ausfuehren("DELETE FROM meta WHERE schluessel = $s", null, ("$s", ArchivFristSchluessel));
        ArchivGeaendert?.Invoke();
    }

    public List<ArchivEintrag> ArchivEintraege(long? bewohnerId = null) => ArchivEintraegeIn(null, bewohnerId);

    private List<ArchivEintrag> ArchivEintraegeIn(SqliteTransaction? transaktion, long? bewohnerId)
    {
        var liste = new List<ArchivEintrag>();
        using var befehl = Befehl("""
            SELECT a.id, a.bewohner_id, a.aufgenommen, a.entlassen, a.grund, a.bemerkung, a.wohnbereich, a.zimmer, a.kuerzel, a.erfasst, w.datum
            FROM archiv a LEFT JOIN archiv_wiederaufnahmen w ON w.archiv_id = a.id
            WHERE ($b IS NULL OR a.bewohner_id = $b) ORDER BY a.bewohner_id, a.entlassen, a.id
            """, transaktion, [("$b", bewohnerId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new ArchivEintrag(leser.GetInt64(0), leser.GetInt64(1), AusIso(leser.GetString(2)), AusIso(leser.GetString(3)),
                ArchivRegeln.GrundAus(leser.GetString(4)), leser.GetString(5), leser.GetString(6), leser.GetString(7), leser.GetString(8),
                AusZeit(leser.GetString(9)), leser.IsDBNull(10) ? null : AusIso(leser.GetString(10))));
        return liste;
    }

    public bool Archiviert(long bewohnerId) => ArchivEintraege(bewohnerId).LastOrDefault()?.Aktiv == true;

    // Die archivierten Bewohner mit ihrem letzten Aufenthalt, der Aufbewahrung und einer Löschfreigabe.
    public List<ArchivZeile> Archiv()
    {
        int? frist = ArchivFrist();
        var freigaben = new Dictionary<long, ArchivLoeschfreigabe>();
        using (var befehl = Befehl("SELECT bewohner_id, kuerzel, zeit FROM archiv_loeschfreigaben ORDER BY zeit", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read()) freigaben[leser.GetInt64(0)] = new ArchivLoeschfreigabe(leser.GetInt64(0), leser.GetString(1), AusZeit(leser.GetString(2)));
        var zeilen = new List<ArchivZeile>();
        foreach (var gruppe in ArchivEintraege().GroupBy(e => e.BewohnerId))
        {
            var letzter = gruppe.Last();
            if (!letzter.Aktiv || Bewohner(gruppe.Key) is not Bewohner b) continue;
            zeilen.Add(new ArchivZeile(b, letzter, [.. gruppe], ArchivRegeln.AufbewahrenBis(letzter.Entlassen, frist), freigaben.GetValueOrDefault(b.Id!.Value)));
        }
        return [.. zeilen.OrderByDescending(z => z.Eintrag.Entlassen).ThenBy(z => z.Bewohner.Nachname, StringComparer.CurrentCulture)];
    }

    // Was beim Entlassen endet: offene Aufgaben, laufende Maßnahmen, Teilnahmen an Therapien.
    public List<EntlassFolge> EntlassFolgen(long bewohnerId, DateOnly datum)
    {
        var folgen = new List<EntlassFolge>();
        foreach (var a in Aufgaben(datum.ToDateTime(TimeOnly.MaxValue)).Where(a => a.BewohnerId == bewohnerId && a.Offen))
            folgen.Add(new EntlassFolge("Aufgabe", a.Id, $"Aufgabe: {a.Text}"));
        foreach (var m in Massnahmen(bewohnerId))
            folgen.Add(new EntlassFolge("Maßnahme", m.Id, $"Maßnahme: {m.Aktuell(datum).Titel}"));
        var angebote = Angebote();
        foreach (var a in angebote)
            foreach (var z in a.Zuteilungen.Where(z => z.BewohnerId == bewohnerId && z.BeendetAb is null && (z.Bis is null || z.Bis >= datum)))
                folgen.Add(new EntlassFolge("Therapie", z.Id, $"Therapie: {a.Am(datum).Name}"));
        return folgen;
    }

    // Entlassen: Folgen beenden und den Aufenthalt ins Archiv – alles oder nichts.
    public ArchivEintrag Entlassen(long bewohnerId, DateOnly datum, EntlassGrund grund, string bemerkung, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.BewohnerBearbeiten) is string recht) throw new Eingabefehler([recht]);
        var b = Bewohner(bewohnerId) ?? throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (Archiviert(bewohnerId)) throw new Eingabefehler([$"{b.Name} ist schon im Archiv."]);
        if (ArchivRegeln.EntlassenPruefen(b, datum, DateOnly.FromDateTime(jetzt)) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
        var folgen = EntlassFolgen(bewohnerId, datum);
        var zuteilungen = Angebote().SelectMany(a => a.Zuteilungen).ToDictionary(z => z.Id);
        using var transaktion = db.BeginTransaction();
        foreach (var f in folgen)
            switch (f.Art)
            {
                case "Aufgabe":
                    Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel, notiz) VALUES ($a, 'erledigt', $z, $k, $n)", transaktion,
                        ("$a", f.Id), ("$z", Zeit(jetzt)), ("$k", kuerzel.Trim()), ("$n", ArchivRegeln.EntlassenText));
                    break;
                case "Maßnahme":
                    Ausfuehren("UPDATE massnahmen SET beendet_am = $a, beendet_grund = $g, beendet_von = $k WHERE id = $i AND beendet_am IS NULL", transaktion,
                        ("$a", Iso(datum.AddDays(1))), ("$g", ArchivRegeln.EntlassenText), ("$k", kuerzel.Trim()), ("$i", f.Id));
                    break;
                default:
                    // Beginnt die Zuteilung erst nach dem Entlasstag, endet sie, bevor sie beginnt (kein Tag bleibt übrig).
                    var ab = new[] { datum.AddDays(1), zuteilungen[f.Id].Ab }.Max();
                    Ausfuehren("INSERT INTO zuteilung_ende (zuteilung_id, ab, erstellt, kuerzel) VALUES ($z, $ab, $e, $k)", transaktion,
                        ("$z", f.Id), ("$ab", Iso(ab)), ("$e", Zeit(jetzt)), ("$k", kuerzel.Trim()));
                    break;
            }
        ArchivEinfuegen(b, datum, grund, bemerkung, kuerzel, jetzt, transaktion);
        transaktion.Commit();
        Zugriff(kuerzel, bewohnerId, ZugriffRegeln.Geaendert, ArchivRegeln.EntlassenText);
        ArchivGeaendert?.Invoke();
        AufgabenGeaendert?.Invoke();
        MassnahmenGeaendert?.Invoke();
        TherapieGeaendert?.Invoke();
        return ArchivEintraege(bewohnerId).Last();
    }

    private void ArchivEinfuegen(Bewohner b, DateOnly datum, EntlassGrund grund, string bemerkung, string kuerzel, DateTime jetzt, SqliteTransaction transaktion) =>
        Ausfuehren("""
            INSERT INTO archiv (bewohner_id, aufgenommen, entlassen, grund, bemerkung, wohnbereich, zimmer, kuerzel, erfasst)
            VALUES ($b, $a, $e, $g, $t, $w, $z, $k, $j)
            """, transaktion, ("$b", b.Id), ("$a", Iso(b.Aufnahmedatum)), ("$e", Iso(datum)), ("$g", ArchivRegeln.Schluessel(grund)), ("$t", bemerkung.Trim()),
            ("$w", b.Wohnbereich), ("$z", b.Zimmer), ("$k", kuerzel.Trim()), ("$j", Zeit(jetzt)));

    // Wieder aufnehmen: ein neuer Aufenthalt; der alte Verlauf bleibt. Das Bett muss frei sein.
    public Bewohner Wiederaufnehmen(long bewohnerId, DateOnly datum, string zimmer, string bett, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.BewohnerBearbeiten) is string recht) throw new Eingabefehler([recht]);
        var e = ArchivEintraege(bewohnerId).LastOrDefault(x => x.Aktiv) ?? throw new Eingabefehler(["Dieser Bewohner ist nicht im Archiv."]);
        if (datum < e.Entlassen) throw new Eingabefehler([$"Die Wiederaufnahme liegt vor der Entlassung ({Datum.Text(e.Entlassen)})."]);
        if (bett.Trim().Length == 0) throw new Eingabefehler(["Bitte ein freies Bett wählen."]);
        var b = BettPruefen(Bewohner(bewohnerId)! with { Aufnahmedatum = datum, Zimmer = zimmer.Trim(), Bett = bett.Trim() });
        using var transaktion = db.BeginTransaction();
        Ausfuehren("INSERT INTO archiv_wiederaufnahmen (archiv_id, datum, kuerzel, erfasst) VALUES ($a, $d, $k, $e)", transaktion,
            ("$a", e.Id), ("$d", Iso(datum)), ("$k", kuerzel.Trim()), ("$e", Zeit(jetzt)));
        Ausfuehren("DELETE FROM archiv_loeschfreigaben WHERE bewohner_id = $b", transaktion, ("$b", bewohnerId));
        Ausfuehren("UPDATE bewohner SET aufnahmedatum = $a, zimmer = $z, bett = $t, wohnbereich = $w WHERE id = $b", transaktion,
            ("$a", Iso(datum)), ("$z", b.Zimmer), ("$t", b.Bett), ("$w", b.Wohnbereich), ("$b", bewohnerId));
        transaktion.Commit();
        Zugriff(kuerzel, bewohnerId, ZugriffRegeln.Geaendert, "Wiederaufnahme");
        ArchivGeaendert?.Invoke();
        return Bewohner(bewohnerId)!;
    }

    public void LoeschenFreigeben(long bewohnerId, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.ArchivLoeschen) is string recht) throw new Eingabefehler([recht]);
        var z = Archiv().FirstOrDefault(x => x.Bewohner.Id == bewohnerId) ?? throw new Eingabefehler(["Dieser Bewohner ist nicht im Archiv."]);
        if (ArchivRegeln.LoeschenPruefen(z, DateOnly.FromDateTime(jetzt), kuerzel, endgueltig: false) is string fehler) throw new Eingabefehler([fehler]);
        Ausfuehren("INSERT INTO archiv_loeschfreigaben (bewohner_id, kuerzel, zeit) VALUES ($b, $k, $z)", null,
            ("$b", bewohnerId), ("$k", kuerzel.Trim()), ("$z", Zeit(jetzt)));
        ArchivGeaendert?.Invoke();
    }

    // Die zweite Person löscht: Akte, Berichte, Dokumente (auch die Dateien) und das Bild. Im Protokoll nur Nummer, Zeitraum, wer, wann.
    public Loeschprotokoll EndgueltigLoeschen(long bewohnerId, string kuerzel, DateTime jetzt)
    {
        if (Grund(kuerzel, Funktionen.ArchivLoeschen) is string recht) throw new Eingabefehler([recht]);
        var z = Archiv().FirstOrDefault(x => x.Bewohner.Id == bewohnerId) ?? throw new Eingabefehler(["Dieser Bewohner ist nicht im Archiv."]);
        if (ArchivRegeln.LoeschenPruefen(z, DateOnly.FromDateTime(jetzt), kuerzel, endgueltig: true) is string fehler) throw new Eingabefehler([fehler]);
        string zeitraum = $"{Datum.Text(z.Aufenthalte[0].Aufgenommen)} – {Datum.Text(z.Eintrag.Entlassen)}";
        long id;
        using (var transaktion = db.BeginTransaction())
        {
            Ausfuehren("INSERT INTO loeschprotokoll (bewohner_nr, zeitraum, freigabe, geloescht, zeit) VALUES ($b, $z, $f, $g, $t)", transaktion,
                ("$b", bewohnerId), ("$z", zeitraum), ("$f", z.Freigabe!.Kuerzel), ("$g", kuerzel.Trim()), ("$t", Zeit(jetzt)));
            id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            Ausfuehren("DELETE FROM bewohner WHERE id = $b", transaktion, ("$b", bewohnerId));   // alles Weitere folgt (CASCADE)
            transaktion.Commit();
        }
        string ablage = Ablage(bewohnerId);
        // Ist ein Dokument gerade geöffnet, bleibt die Datei liegen, statt nach dem Löschen in der Datenbank abzustürzen.
        try { if (Directory.Exists(ablage)) Directory.Delete(ablage, recursive: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        Bilder.Loeschen(z.Bewohner.Bild);
        ArchivGeaendert?.Invoke();
        return Loeschprotokolle().Single(p => p.Id == id);
    }

    public List<Loeschprotokoll> Loeschprotokolle()
    {
        var liste = new List<Loeschprotokoll>();
        using var befehl = Befehl("SELECT id, bewohner_nr, zeitraum, freigabe, geloescht, zeit FROM loeschprotokoll ORDER BY zeit", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Loeschprotokoll(leser.GetInt64(0), leser.GetInt64(1), leser.GetString(2), leser.GetString(3), leser.GetString(4), AusZeit(leser.GetString(5))));
        return liste;
    }

    // Demo: drei ausgedachte ehemalige Bewohner mit ein paar Berichten; einer ist so lange entlassen, dass die Demo-Frist
    // (10 Jahre, ausgedacht) abgelaufen ist.
    private void DemoArchivEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM archiv_loeschfreigaben", transaktion);   // das Löschprotokoll bleibt immer
        var heute = DateOnly.FromDateTime(Uhr());
        foreach (var (b, entlassen, grund, berichte) in Demodaten.Ehemalige(heute))
        {
            Ausfuehren($"INSERT INTO bewohner ({Spalten}) VALUES ({Platzhalter})", transaktion, Werte(b));
            var neu = b with { Id = Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture) };
            foreach (var (tage, text) in berichte)
                BerichtEinfuegen(new Bericht
                {
                    BewohnerId = neu.Id!.Value, Zeitpunkt = entlassen.AddDays(-tage).ToDateTime(new TimeOnly(10, 30)), Schicht = Schicht.Frueh, Verfasser = "EF",
                    Text = text, Bereiche = [1], Erfasst = entlassen.AddDays(-tage).ToDateTime(new TimeOnly(10, 35)),
                }, transaktion);
            ArchivEinfuegen(neu, entlassen, grund, "", "EF", entlassen.ToDateTime(new TimeOnly(14, 0)), transaktion);
        }
        SetzeMeta(ArchivFristSchluessel, "10", transaktion);   // ausgedacht, rechtlich nicht geprüft
        SetzeMeta(DemoArchiv, "1", transaktion);
    }
}
