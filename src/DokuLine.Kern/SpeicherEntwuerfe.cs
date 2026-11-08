using System.Text.Json;

namespace DokuLine.Kern;

// Entwürfe (Bauplan Schritt 26a3): Wer sich abmeldet, während ein Bericht angefangen ist, kann ihn als
// Entwurf behalten. Ein Entwurf gehört dem Kürzel, das ihn anfing; je Kürzel, Art und Bewohner einer.
public sealed record Entwurf(long Id, string Kuerzel, string Art, long BewohnerId, string Inhalt, DateTime Geaendert);

public static class EntwurfRegeln
{
    public const string Bericht = "bericht";

    public static string AlsJson(BerichtEingabe e) => JsonSerializer.Serialize(e);

    public static BerichtEingabe? BerichtLesen(string json)
    {
        try { return JsonSerializer.Deserialize<BerichtEingabe>(json); }
        catch (JsonException) { return null; }
    }
}

public sealed partial class Speicher
{
    public event Action? EntwuerfeGeaendert;

    public void EntwurfSpeichern(string kuerzel, string art, long bewohnerId, string inhalt, DateTime jetzt)
    {
        if (string.IsNullOrWhiteSpace(kuerzel)) throw new Eingabefehler([RechteRegeln.NichtAngemeldetText]);
        Ausfuehren("""
            INSERT INTO entwuerfe (kuerzel, art, bewohner_id, inhalt, geaendert) VALUES ($k, $a, $b, $i, $g)
            ON CONFLICT (kuerzel, art, bewohner_id) DO UPDATE SET inhalt = excluded.inhalt, geaendert = excluded.geaendert
            """, null, ("$k", kuerzel.Trim()), ("$a", art), ("$b", bewohnerId), ("$i", inhalt), ("$g", Zeit(jetzt)));
        EntwuerfeGeaendert?.Invoke();
    }

    // Die Entwürfe eines Kürzels, die ältesten zuerst.
    public List<Entwurf> Entwuerfe(string? kuerzel)
    {
        var liste = new List<Entwurf>();
        if (string.IsNullOrWhiteSpace(kuerzel)) return liste;
        using var befehl = Befehl("SELECT id, kuerzel, art, bewohner_id, inhalt, geaendert FROM entwuerfe WHERE kuerzel = $k ORDER BY geaendert, id",
            null, [("$k", kuerzel.Trim())]);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
            liste.Add(new Entwurf(leser.GetInt64(0), leser.GetString(1), leser.GetString(2), leser.GetInt64(3), leser.GetString(4), AusZeit(leser.GetString(5))));
        return liste;
    }

    public Entwurf? EntwurfFuer(string? kuerzel, string art, long bewohnerId) =>
        Entwuerfe(kuerzel).FirstOrDefault(e => e.Art == art && e.BewohnerId == bewohnerId);

    public void EntwurfLoeschen(long id)
    {
        Ausfuehren("DELETE FROM entwuerfe WHERE id = $id", null, ("$id", id));
        EntwuerfeGeaendert?.Invoke();
    }
}
