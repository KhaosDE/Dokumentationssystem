using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Kern;

// Wohnbereich-ToDo im Speicher. Aufgaben werden nie geändert: Weitergeben und „erledigt“
// kommen als Ereignisse dazu (die Datenbank verbietet UPDATE auf beiden Tabellen).
public sealed partial class Speicher
{
    // Nach jeder Änderung an Aufgaben (z. B. für den Zähler in der Seitenleiste).
    public event Action? AufgabenGeaendert;

    public Aufgabe AufgabeSpeichern(Aufgabe a)
    {
        if (a.BewohnerId is long b && Bewohner(b) is null) throw new Eingabefehler(["Diesen Bewohner gibt es nicht mehr."]);
        if (a.Id != 0) throw new InvalidOperationException("Aufgaben werden nicht überschrieben.");
        using var transaktion = db.BeginTransaction();
        long id = AufgabeEinfuegen(a, transaktion);
        transaktion.Commit();
        AufgabenGeaendert?.Invoke();
        return AufgabeLesen(id)!;
    }

    private long AufgabeEinfuegen(Aufgabe a, SqliteTransaction transaktion)
    {
        Ausfuehren("""
            INSERT INTO aufgaben (bewohner_id, bericht_id, text, fuer, schicht_tag, schicht, faellig_bis, erstellt, verfasser)
            VALUES ($b, $r, $t, $f, $st, $s, $fb, $e, $v)
            """, transaktion,
            ("$b", a.BewohnerId), ("$r", a.BerichtId), ("$t", a.Text.Trim()), ("$f", AufgabenRegeln.FuerKuerzel(a.Fuer)),
            ("$st", a.FuerSchicht is SchichtTermin s1 ? Iso(s1.Tag) : null),
            ("$s", a.FuerSchicht is SchichtTermin s2 ? BerichtRegeln.Kuerzel(s2.Schicht) : null),
            ("$fb", a.FaelligBis is DateTime f ? Zeit(f) : null), ("$e", Zeit(a.Erstellt)), ("$v", a.Verfasser));
        return Convert.ToInt64(WertIn(transaktion, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    public Aufgabe? AufgabeLesen(long id) => AufgabenLesen("WHERE id = $id", ("$id", id)).FirstOrDefault();

    // Offene Aufgaben in der Reihenfolge der Liste; mit „auchErledigte“ alle.
    public List<Aufgabe> Aufgaben(DateTime jetzt, bool auchErledigte = false) =>
        AufgabenRegeln.Sortiert(AufgabenLesen("").Where(a => auchErledigte || a.Offen), jetzt);

    public int OffeneAufgaben() => AufgabenLesen("").Count(a => a.Offen);

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
            SELECT id, bewohner_id, bericht_id, text, fuer, schicht_tag, schicht, faellig_bis, erstellt, verfasser
            FROM aufgaben {bedingung}
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
