using System.Globalization;

namespace DokuLine.Kern;

// Schlüsselwörter im Speicher (Bauplan 48): eine Liste für das ganze Haus, beim ersten Lesen mit der Werkseinstellung gefüllt.
// Mitgelieferte Wörter werden nur abgeschaltet, nie gelöscht. Verweise vom Bericht auf das, was daraus entstand.
public sealed partial class Speicher
{
    private const string WoerterVorbelegt = "schluesselwoerter_vorbelegt";

    public List<Schluesselwort> Schluesselwoerter()
    {
        if (Meta(WoerterVorbelegt) is null)
        {
            using var transaktion = db.BeginTransaction();
            if (WertIn(transaktion, "SELECT wert FROM meta WHERE schluessel = $s", ("$s", WoerterVorbelegt)) is null)
            {
                foreach (var (wort, folge, art) in SchluesselwortRegeln.Mitgeliefert)
                    Ausfuehren("INSERT INTO schluesselwoerter (wort, folgeschritt, art, aktiv, mitgeliefert) VALUES ($w, $f, $a, 1, 1)", transaktion,
                        ("$w", wort), ("$f", folge.ToString()), ("$a", art.ToString()));
                SetzeMeta(WoerterVorbelegt, "1", transaktion);
            }
            transaktion.Commit();
        }
        var liste = new List<Schluesselwort>();
        using var befehl = Befehl("SELECT id, wort, folgeschritt, art, aktiv, mitgeliefert FROM schluesselwoerter ORDER BY id", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            if (Enum.TryParse<Folgeschritt>(leser.GetString(2), out var f) && Enum.TryParse<Treffer>(leser.GetString(3), out var t))
                liste.Add(new Schluesselwort(leser.GetInt64(0), leser.GetString(1), f, t, leser.GetInt64(4) != 0, leser.GetInt64(5) != 0));
        return liste;
    }

    private void WoerterRecht(string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.SchluesselwoerterVerwalten) is string grund) throw new Eingabefehler([grund]);
    }

    public Schluesselwort WortHinzufuegen(string wort, Folgeschritt folge, Treffer art, string kuerzel)
    {
        WoerterRecht(kuerzel);
        if (SchluesselwortRegeln.Pruefen(wort) is string fehler) throw new Eingabefehler([fehler]);
        if (Schluesselwoerter().Any(w => w.Folgeschritt == folge && w.Wort.Equals(wort.Trim(), StringComparison.CurrentCultureIgnoreCase)))
            throw new Eingabefehler(["Dieses Wort gibt es für diesen Schritt schon."]);
        Ausfuehren("INSERT INTO schluesselwoerter (wort, folgeschritt, art, aktiv, mitgeliefert) VALUES ($w, $f, $a, 1, 0)", null,
            ("$w", wort.Trim()), ("$f", folge.ToString()), ("$a", art.ToString()));
        return Schluesselwoerter()[^1];
    }

    public void WortAendern(long id, bool aktiv, Folgeschritt folge, Treffer art, string kuerzel)
    {
        WoerterRecht(kuerzel);
        Ausfuehren("UPDATE schluesselwoerter SET aktiv = $a, folgeschritt = $f, art = $t WHERE id = $i", null,
            ("$a", aktiv ? 1 : 0), ("$f", folge.ToString()), ("$t", art.ToString()), ("$i", id));
    }

    // Eigene Wörter weg; mitgelieferte nur aus.
    public void WortEntfernen(long id, string kuerzel)
    {
        WoerterRecht(kuerzel);
        Ausfuehren("DELETE FROM schluesselwoerter WHERE id = $i AND mitgeliefert = 0", null, ("$i", id));
        Ausfuehren("UPDATE schluesselwoerter SET aktiv = 0 WHERE id = $i AND mitgeliefert = 1", null, ("$i", id));
    }

    // Werkseinstellung je Schritt: die mitgelieferten Wörter wie geliefert, eigene bleiben.
    public void Werkseinstellung(Folgeschritt folge, string kuerzel)
    {
        WoerterRecht(kuerzel);
        using var transaktion = db.BeginTransaction();
        Ausfuehren("DELETE FROM schluesselwoerter WHERE mitgeliefert = 1 AND folgeschritt = $f", transaktion, ("$f", folge.ToString()));
        foreach (var (wort, f, art) in SchluesselwortRegeln.Mitgeliefert.Where(m => m.Folgeschritt == folge))
            Ausfuehren("INSERT INTO schluesselwoerter (wort, folgeschritt, art, aktiv, mitgeliefert) VALUES ($w, $f, $a, 1, 1)", transaktion,
                ("$w", wort), ("$f", f.ToString()), ("$a", art.ToString()));
        transaktion.Commit();
    }

    // Je Mitarbeiter: „keine Vorschläge anzeigen“.
    public bool VorschlaegeAus(string kuerzel) => Meta($"keine_vorschlaege:{kuerzel.Trim().ToUpperInvariant()}") == "1";

    public void VorschlaegeAusSetzen(string kuerzel, bool aus)
    {
        if (kuerzel.Trim().Length == 0) return;
        if (aus) SetzeMeta($"keine_vorschlaege:{kuerzel.Trim().ToUpperInvariant()}", "1", null);
        else Ausfuehren("DELETE FROM meta WHERE schluessel = $s", null, ("$s", $"keine_vorschlaege:{kuerzel.Trim().ToUpperInvariant()}"));
    }

    public void VerweisEintragen(long berichtId, string text)
    {
        if (BerichtLesen(berichtId) is null) throw new Eingabefehler(["Diesen Bericht gibt es nicht mehr."]);
        Ausfuehren("INSERT INTO bericht_verweise (bericht_id, text, erfasst) VALUES ($b, $t, $e)", null,
            ("$b", berichtId), ("$t", text.Trim()), ("$e", Zeit(Uhr())));
    }

    public List<string> Verweise(long berichtId)
    {
        var liste = new List<string>();
        using var befehl = Befehl("SELECT text FROM bericht_verweise WHERE bericht_id = $b ORDER BY id", null, [("$b", berichtId)]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(leser.GetString(0));
        return liste;
    }
}
