using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DokuLine.Kern;

// Wohnbereich-ToDo im Speicher. Aufgaben werden nie geändert: Weitergeben und „erledigt“
// kommen als Ereignisse dazu (die Datenbank verbietet UPDATE auf beiden Tabellen).
public sealed partial class Speicher
{
    // Nach jeder Änderung an Aufgaben (z. B. für den Zähler in der Seitenleiste).
    public event Action? AufgabenGeaendert;

    public Aufgabe AufgabeSpeichern(Aufgabe a)
    {
        if (a.BewohnerId is long b && Bewohner(b) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (a.BewohnerId is null && a.Bereich.Trim().Length == 0)
            throw new Eingabefehler([AufgabenRegeln.BereichFehlt]);
        if (a.Id != 0) throw new InvalidOperationException("Aufgaben werden nicht überschrieben.");
        using var transaktion = db.BeginTransaction();
        long id = AufgabeEinfuegen(a, transaktion);
        transaktion.Commit();
        AufgabenGeaendert?.Invoke();
        return AufgabeLesen(id)!;
    }

    internal long AufgabeEinfuegenIn(Aufgabe a, SqliteTransaction transaktion) => AufgabeEinfuegen(a, transaktion);

    private long AufgabeEinfuegen(Aufgabe a, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO aufgaben (bewohner_id, bericht_id, text, fuer, schicht_tag, schicht, faellig_bis, erstellt, verfasser, vorlage_id,
                                  visite_anordnung_id, visite_eintrag_id, bereich)
            VALUES ($b, $r, $t, $f, $st, $s, $fb, $e, $v, $vo, $va, $ve, $be)
            """, transaktion, ("$be", a.BewohnerId is null ? a.Bereich.Trim() : ""),
            ("$b", a.BewohnerId), ("$r", a.BerichtId), ("$t", a.Text.Trim()), ("$f", AufgabenRegeln.FuerKuerzel(a.Fuer)),
            ("$st", a.FuerSchicht is SchichtTermin s1 ? Iso(s1.Tag) : null),
            ("$s", a.FuerSchicht is SchichtTermin s2 ? BerichtRegeln.Kuerzel(s2.Schicht) : null),
            ("$fb", a.FaelligBis is DateTime f ? Zeit(f) : null), ("$e", Zeit(a.Erstellt)), ("$v", a.Verfasser), ("$vo", a.VorlageId),
            ("$va", a.VisiteAnordnungId), ("$ve", a.VisiteEintragId));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    public Aufgabe? AufgabeLesen(long id) => AufgabenLesen("WHERE a.id = $id", ("$id", id)).FirstOrDefault();

    // Offene Aufgaben in der Reihenfolge der Liste; mit „auchErledigte“ alle. bereich: nur dieser (oder „Alle“).
    public List<Aufgabe> Aufgaben(DateTime jetzt, bool auchErledigte = false, string bereich = BereichswahlRegeln.Alle) =>
        AufgabenRegeln.Sortiert(
            AufgabenLesen("").Where(a => (auchErledigte || a.Offen) && BereichswahlRegeln.Passt(a.Bereich, bereich)), jetzt);

    public int OffeneAufgaben(string bereich = BereichswahlRegeln.Alle) =>
        AufgabenLesen("").Count(a => a.Offen && BereichswahlRegeln.Passt(a.Bereich, bereich));

    public void AufgabeWeitergeben(long id, string kuerzel, string notiz, DateTime zeit)
    {
        var a = AufgabeOffen(id, kuerzel);
        if (notiz.Trim().Length > 200) throw new Eingabefehler(["Die Notiz ist zu lang (höchstens 200 Zeichen)."]);
        Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel, notiz) VALUES ($a, 'weitergegeben', $z, $k, $n)", null,
            ("$a", a.Id), ("$z", Zeit(zeit)), ("$k", kuerzel.Trim()), ("$n", notiz.Trim()));
        AufgabenGeaendert?.Invoke();
    }

    public void AufgabeErledigt(long id, string kuerzel, DateTime zeit)
    {
        var a = AufgabeOffen(id, kuerzel);
        Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel) VALUES ($a, 'erledigt', $z, $k)", null,
            ("$a", a.Id), ("$z", Zeit(zeit)), ("$k", kuerzel.Trim()));
        AufgabenGeaendert?.Invoke();
    }

    // Begründet schließen: nicht erledigt, aber nicht mehr zu tun – mit Begründung.
    public void AufgabeSchliessen(long id, string kuerzel, string begruendung, DateTime zeit)
    {
        var a = AufgabeOffen(id, kuerzel);
        begruendung = begruendung.Trim().ReplaceLineEndings(" ");
        if (begruendung.Length == 0) throw new Eingabefehler(["Bitte kurz begründen, warum die Aufgabe nicht erledigt wurde."]);
        if (begruendung.Length > 200) throw new Eingabefehler(["Die Begründung ist zu lang (höchstens 200 Zeichen)."]);
        Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel, notiz) VALUES ($a, 'erledigt', $z, $k, $n)", null,
            ("$a", a.Id), ("$z", Zeit(zeit)), ("$k", kuerzel.Trim()), ("$n", begruendung));
        AufgabenGeaendert?.Invoke();
    }

    // ----- Standardaufgaben -----

    public List<AufgabeVorlage> Vorlagen()
    {
        var liste = new List<AufgabeVorlage>();
        using var befehl = Befehl("""
            SELECT v.id, v.text, v.schicht, v.tage, v.uhrzeit, v.bewohner_id, v.aktiv, v.erstellt, v.kuerzel, COALESCE(b.wohnbereich, v.bereich)
            FROM aufgabe_vorlagen v LEFT JOIN bewohner b ON b.id = v.bewohner_id ORDER BY v.id
            """, null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new AufgabeVorlage
            {
                Id = leser.GetInt64(0), Text = leser.GetString(1), Schicht = BerichtRegeln.SchichtAus(leser.GetString(2)),
                Tage = AufgabenRegeln.TageAus(leser.GetInt32(3)), Uhrzeit = leser.IsDBNull(4) ? null : TimeOnly.ParseExact(leser.GetString(4), "HH:mm", CultureInfo.InvariantCulture),
                BewohnerId = leser.IsDBNull(5) ? null : leser.GetInt64(5), Aktiv = leser.GetInt64(6) == 1,
                Erstellt = AusZeit(leser.GetString(7)), Kuerzel = leser.GetString(8), Bereich = leser.GetString(9),
            });
        return liste;
    }

    // Neue Vorlage (Id 0) oder ändern. Vorlagen sind Einstellungen: Ändern überschreibt, schon
    // entstandene Aufgaben bleiben, wie sie sind.
    public AufgabeVorlage VorlageSpeichern(AufgabeVorlage v) => VorlageSpeichern(v, null);

    private AufgabeVorlage VorlageSpeichern(AufgabeVorlage v, SqliteTransaction? transaktion)
    {
        if (v.BewohnerId is long b && Bewohner(b) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (v.BewohnerId is null && v.Bereich.Trim().Length == 0)
            throw new Eingabefehler([AufgabenRegeln.BereichFehlt]);
        (string, object?)[] werte =
        [
            ("$id", v.Id), ("$t", v.Text), ("$s", BerichtRegeln.Kuerzel(v.Schicht)), ("$ta", AufgabenRegeln.TageMaske(v.Tage)),
            ("$u", v.Uhrzeit is TimeOnly u ? u.ToString("HH:mm", CultureInfo.InvariantCulture) : null), ("$b", v.BewohnerId),
            ("$a", v.Aktiv ? 1 : 0), ("$e", Zeit(v.Erstellt)), ("$k", v.Kuerzel),
            ("$be", v.BewohnerId is null ? v.Bereich.Trim() : ""),
        ];
        long id = v.Id;
        if (id == 0)
        {
            Ausfuehren("""
                INSERT INTO aufgabe_vorlagen (text, schicht, tage, uhrzeit, bewohner_id, aktiv, erstellt, kuerzel, bereich)
                VALUES ($t, $s, $ta, $u, $b, $a, $e, $k, $be)
                """, transaktion, werte);
            id = Convert.ToInt64(transaktion is null ? Wert("SELECT last_insert_rowid()") : WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
        }
        else
            Ausfuehren("""
                UPDATE aufgabe_vorlagen SET text = $t, schicht = $s, tage = $ta, uhrzeit = $u, bewohner_id = $b, aktiv = $a, kuerzel = $k,
                    bereich = $be WHERE id = $id
                """, transaktion, werte);
        if (transaktion is null) AufgabenGeaendert?.Invoke();
        return v with { Id = id };
    }

    public void VorlageAktiv(long id, bool aktiv)
    {
        Ausfuehren("UPDATE aufgabe_vorlagen SET aktiv = $a WHERE id = $id", null, ("$a", aktiv ? 1 : 0), ("$id", id));
        AufgabenGeaendert?.Invoke();
    }

    // Zu Beginn jeder Schicht: aus den passenden Standardaufgaben die Aufgaben dieser Schicht – jede
    // nur einmal je Schicht (Datenbank-Regel). Läuft, wenn die Aufgaben gelesen werden.
    public int StandardaufgabenErzeugen(DateTime jetzt)
    {
        var schicht = SchichtTermin.Zu(jetzt);
        int neu = 0;
        foreach (var v in Vorlagen().Where(v => AufgabenRegeln.Gilt(v, schicht)))
        {
            Ausfuehren("""
                INSERT OR IGNORE INTO aufgaben (bewohner_id, text, fuer, schicht_tag, schicht, faellig_bis, erstellt, verfasser, vorlage_id, bereich)
                VALUES ($b, $t, 'schicht', $st, $s, $fb, $e, $v, $vo, $be)
                """, null, ("$be", v.BewohnerId is null ? v.Bereich : ""),
                ("$b", v.BewohnerId), ("$t", v.Text), ("$st", Iso(schicht.Tag)), ("$s", BerichtRegeln.Kuerzel(schicht.Schicht)),
                ("$fb", Zeit(AufgabenRegeln.FaelligIn(v, schicht))), ("$e", Zeit(schicht.Beginn)), ("$v", v.Kuerzel), ("$vo", v.Id));
            neu += Convert.ToInt32(Wert("SELECT changes()"), CultureInfo.InvariantCulture);
        }
        int messungen = MessauftraegeErzeugen(jetzt);   // zählen nicht als Standardaufgaben
        if (neu + messungen > 0) AufgabenGeaendert?.Invoke();
        return neu;
    }

    // ----- Schichtzeiten je Bereich -----

    public Schichtzeiten Schichtzeiten(string bereich)
    {
        using var befehl = Befehl("SELECT frueh, spaet, nacht FROM schichtzeiten WHERE bereich = $b", null, [("$b", bereich)]);
        using var leser = befehl.ExecuteReader();
        if (!leser.Read()) return Kern.Schichtzeiten.Vorgabe;
        static TimeOnly U(string s) => TimeOnly.ParseExact(s, "HH:mm", CultureInfo.InvariantCulture);
        return new Schichtzeiten(U(leser.GetString(0)), U(leser.GetString(1)), U(leser.GetString(2)));
    }

    public void SchichtzeitenSetzen(string bereich, Schichtzeiten z)
    {
        static string T(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);
        Ausfuehren("INSERT INTO schichtzeiten (bereich, frueh, spaet, nacht) VALUES ($b, $f, $s, $n) ON CONFLICT(bereich) DO UPDATE SET frueh = excluded.frueh, spaet = excluded.spaet, nacht = excluded.nacht",
            null, ("$b", bereich), ("$f", T(z.Frueh)), ("$s", T(z.Spaet)), ("$n", T(z.Nacht)));
    }

    // Demo: Standardaufgaben je Schicht, besonders für den Nachtdienst.
    private void DemoStandardaufgabenEinfuegen(SqliteTransaction transaktion)
    {
        Ausfuehren("DELETE FROM aufgabe_vorlagen WHERE kuerzel IN ('AB', 'CD', 'EF', 'KL')", transaktion);
        var wohnbereiche = LesenNamen(transaktion, "SELECT DISTINCT wohnbereich FROM bewohner ORDER BY wohnbereich");
        foreach (var v in Demodaten.Standardaufgaben(Uhr()))
            foreach (string bereich in wohnbereiche) VorlageSpeichern(v with { Bereich = bereich }, transaktion);
        SetzeMeta(DemoStandardaufgaben, "1", transaktion);
    }

    private List<string> LesenNamen(SqliteTransaction? transaktion, string sql)
    {
        var liste = new List<string>();
        using var befehl = Befehl(sql, transaktion, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(leser.GetString(0));
        return liste;
    }

    private Aufgabe AufgabeOffen(long id, string kuerzel)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler(["Kürzel fehlt (Einstellungen)."]);
        var a = AufgabeLesen(id) ?? throw new Eingabefehler(["Diese Aufgabe gibt es nicht mehr."]);
        if (!a.Offen) throw new Eingabefehler(["Diese Aufgabe ist schon erledigt."]);
        return a;
    }

    private List<Aufgabe> AufgabenLesen(string bedingung, params (string, object?)[] parameter)
    {
        var liste = new List<Aufgabe>();
        using (var befehl = Befehl($"""
            SELECT a.id, a.bewohner_id, a.bericht_id, a.text, a.fuer, a.schicht_tag, a.schicht, a.faellig_bis, a.erstellt, a.verfasser,
                   a.vorlage_id, a.visite_anordnung_id, a.visite_eintrag_id, COALESCE(b.wohnbereich, a.bereich)
            FROM aufgaben a LEFT JOIN bewohner b ON b.id = a.bewohner_id {bedingung}
            """, null, parameter))
        using (var leser = befehl.ExecuteReader())
        {
            while (leser.Read())
            {
                liste.Add(new Aufgabe
                {
                    Id = leser.GetInt64(0),
                    BewohnerId = leser.IsDBNull(1) ? null : leser.GetInt64(1),
                    BerichtId = leser.IsDBNull(2) ? null : leser.GetInt64(2),
                    Text = leser.GetString(3),
                    Fuer = AufgabenRegeln.FuerAus(leser.GetString(4)),
                    FuerSchicht = leser.IsDBNull(5) || leser.IsDBNull(6) ? null
                        : new SchichtTermin(AusIso(leser.GetString(5)), BerichtRegeln.SchichtAus(leser.GetString(6))),
                    FaelligBis = leser.IsDBNull(7) ? null : AusZeit(leser.GetString(7)),
                    Erstellt = AusZeit(leser.GetString(8)),
                    Verfasser = leser.GetString(9),
                    VorlageId = leser.IsDBNull(10) ? null : leser.GetInt64(10),
                    VisiteAnordnungId = leser.IsDBNull(11) ? null : leser.GetInt64(11),
                    VisiteEintragId = leser.IsDBNull(12) ? null : leser.GetInt64(12),
                    Bereich = leser.GetString(13),
                });
            }
        }
        if (liste.Count == 0) return liste;
        var ereignisse = new List<(long Id, string Art, DateTime Zeit, string Kuerzel, string Notiz)>();
        string ids = string.Join(",", liste.Select(a => a.Id));
        using (var befehl = Befehl($"SELECT aufgabe_id, art, zeit, kuerzel, notiz FROM aufgabe_ereignisse WHERE aufgabe_id IN ({ids}) ORDER BY zeit, id", null, []))
        using (var leser = befehl.ExecuteReader())
            while (leser.Read())
                ereignisse.Add((leser.GetInt64(0), leser.GetString(1), AusZeit(leser.GetString(2)), leser.GetString(3), leser.GetString(4)));
        return liste.Select(a =>
        {
            var eigene = ereignisse.Where(e => e.Id == a.Id).ToList();
            var erledigt = eigene.FirstOrDefault(e => e.Art == "erledigt");
            return a with
            {
                Weitergaben = eigene.Where(e => e.Art == "weitergegeben").Select(e => new Weitergabe(e.Zeit, e.Kuerzel, e.Notiz)).ToList(),
                Erledigt = erledigt.Art is null ? null : erledigt.Zeit,
                ErledigtVon = erledigt.Art is null ? null : erledigt.Kuerzel,
                ErledigtNotiz = erledigt.Art is null ? "" : erledigt.Notiz,
            };
        }).ToList();
    }

    // Demo: ausgedachte Aufgaben relativ zu jetzt (eine überfällig und weitergereicht, eine erledigt).
    private void DemoAufgabenEinfuegen(SqliteTransaction transaktion)
    {
        var bewohner = LesenIn(transaktion, $"SELECT id, {Spalten} FROM bewohner");
        Ausfuehren("DELETE FROM aufgaben WHERE bewohner_id IS NULL", transaktion);   // Aufgaben ohne Bewohner aus der Demo
        foreach (var d in Demodaten.Aufgaben(bewohner, Uhr()))
        {
            long id = AufgabeEinfuegen(d.Aufgabe, transaktion);
            foreach (var w in d.Aufgabe.Weitergaben)
                Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel, notiz) VALUES ($a, 'weitergegeben', $z, $k, $n)", transaktion,
                    ("$a", id), ("$z", Zeit(w.Zeit)), ("$k", w.Kuerzel), ("$n", w.Notiz));
            if (d.Aufgabe.Erledigt is DateTime erledigt)
                Ausfuehren("INSERT INTO aufgabe_ereignisse (aufgabe_id, art, zeit, kuerzel) VALUES ($a, 'erledigt', $z, $k)", transaktion,
                    ("$a", id), ("$z", Zeit(erledigt)), ("$k", d.Aufgabe.ErledigtVon ?? ""));
        }
        SetzeMeta(DemoAufgaben, "1", transaktion);
    }
}
