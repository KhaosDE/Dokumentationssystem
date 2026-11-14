namespace DokuLine.Kern;

// Fachwörterbuch der Einrichtung und die Wörter, die DokuLine selbst kennt (Bauplan 49). Das Fachwörterbuch ist eine Liste
// für das ganze Haus, beim ersten Lesen mit der Grundliste gefüllt; ergänzen mit Recht „Fachwörter verwalten“.
public sealed partial class Speicher
{
    private const string FachwoerterVorbelegt = "fachwoerter_vorbelegt";

    public List<string> Fachwoerter()
    {
        if (Meta(FachwoerterVorbelegt) is null)
        {
            using var transaktion = db.BeginTransaction();
            if (WertIn(transaktion, "SELECT wert FROM meta WHERE schluessel = $s", ("$s", FachwoerterVorbelegt)) is null)
            {
                foreach (var w in RechtschreibRegeln.Grundliste)
                    Ausfuehren("INSERT OR IGNORE INTO fachwoerter (wort, mitgeliefert) VALUES ($w, 1)", transaktion, ("$w", w));
                SetzeMeta(FachwoerterVorbelegt, "1", transaktion);
            }
            transaktion.Commit();
        }
        var liste = new List<string>();
        using var befehl = Befehl("SELECT wort FROM fachwoerter ORDER BY wort COLLATE NOCASE", null, []);
        using var leser = befehl.ExecuteReader();
        while (leser.Read()) liste.Add(leser.GetString(0));
        return liste;
    }

    public void FachwortHinzufuegen(string wort, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.FachwoerterVerwalten) is string grund) throw new Eingabefehler([grund]);
        if (RechtschreibRegeln.WortPruefen(wort) is string fehler) throw new Eingabefehler([fehler]);
        if (Fachwoerter().Contains(wort.Trim(), StringComparer.CurrentCultureIgnoreCase)) throw new Eingabefehler([$"„{wort.Trim()}“ steht schon im Fachwörterbuch."]);
        Ausfuehren("INSERT INTO fachwoerter (wort, mitgeliefert) VALUES ($w, 0)", null, ("$w", wort.Trim()));
    }

    public void FachwortEntfernen(string wort, string kuerzel)
    {
        if (Grund(kuerzel, Funktionen.FachwoerterVerwalten) is string grund) throw new Eingabefehler([grund]);
        Ausfuehren("DELETE FROM fachwoerter WHERE wort = $w", null, ("$w", wort.Trim()));
    }

    // Was als richtig gilt: Fachwörter, Namen der Bewohner und der Verzeichnisse, Kürzel und Namen der Mitarbeiter,
    // Wirkstoffe und Handelsnamen, Therapieangebote und ihre Orte, Wohnbereiche.
    public HashSet<string> EigeneWoerter()
    {
        var quellen = new List<string>(Fachwoerter());
        quellen.AddRange(AlleBewohner().SelectMany(b => new[] { b.Vorname, b.Nachname }));
        quellen.AddRange(Personen().Select(p => p.Name));
        quellen.AddRange(AlleMitarbeiter().SelectMany(m => new[] { m.Kuerzel, m.Name }));
        quellen.AddRange(Medikamente().SelectMany(m => m.Aktuell.Handelsnamen.Append(m.Aktuell.Wirkstoff)));
        quellen.AddRange(Angebote().SelectMany(a => a.Fassungen.SelectMany(f => new[] { f.Name, f.Ort })));
        quellen.AddRange(Wohnbereiche().Select(w => w.Name));
        return RechtschreibRegeln.EigeneWoerter(quellen.Where(q => !string.IsNullOrWhiteSpace(q)));
    }

    // Autokorrektur je Mitarbeiter (Vorgabe aus).
    public bool AutokorrekturAn(string kuerzel) => Meta($"autokorrektur:{kuerzel.Trim().ToUpperInvariant()}") == "1";

    public void AutokorrekturSetzen(string kuerzel, bool an)
    {
        if (kuerzel.Trim().Length == 0) return;
        if (an) SetzeMeta($"autokorrektur:{kuerzel.Trim().ToUpperInvariant()}", "1", null);
        else Ausfuehren("DELETE FROM meta WHERE schluessel = $s", null, ("$s", $"autokorrektur:{kuerzel.Trim().ToUpperInvariant()}"));
    }
}
