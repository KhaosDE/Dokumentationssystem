namespace DokuLine.Modul.Ausgangsliste;

// Die Textbausteine-Logik der Ausgangsspalte (übernommen aus der Ausgangsliste, bereinigt).
//
// Das Format ist das des Originals: Bausteine, verbunden mit " / ".
//   "2 Allein / 1 MBW / (Befindlichkeit abfragen)"
//   - "2 Allein"  = Stunden-Baustein (Zahl + Art)
//   - "(…)"       = Info-Baustein
//   - feste Texte = z. B. "wie offen geführt" oder die Personal-Begleitung
public static class AusgangText
{
    public const string TextWieOffenGefuehrt = "wie offen geführt";
    public const string TextPersonalBegleitung = "Ausgang nur mit Personal und in den geschl. Garten";

    // Text in einzelne Bausteine zerlegen (leere verschwinden).
    public static List<string> Bausteine(string text) =>
        text.Split(" / ")
            .Select(baustein => baustein.Trim())
            .Where(baustein => baustein != "")
            .ToList();

    public static string Zusammenfuegen(IEnumerable<string> bausteine) =>
        string.Join(" / ", bausteine);

    public static bool IstInfo(string baustein) =>
        baustein.StartsWith("(") && baustein.EndsWith(")");

    // "Allein +1": vorhandenen Marker-Baustein hochzählen, sonst "1 Allein"
    // anhängen. woerter[^1] heißt: das letzte Wort ("^1" = eins vom Ende).
    public static string MarkerErhoehen(string text, string marker)
    {
        var bausteine = Bausteine(text);

        for (int i = 0; i < bausteine.Count; i++)
        {
            string[] woerter = bausteine[i].Split(' ');
            if (woerter.Length >= 2
                && woerter[^1].Equals(marker, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(woerter[0], out int zahl))
            {
                bausteine[i] = $"{zahl + 1} {marker}";
                return Zusammenfuegen(bausteine);
            }
        }

        bausteine.Add($"1 {marker}");
        return Zusammenfuegen(bausteine);
    }

    // "Allein −1": herunterzählen; bei 0 fliegt der Baustein ganz raus.
    public static string MarkerVerringern(string text, string marker)
    {
        var bausteine = Bausteine(text);

        for (int i = 0; i < bausteine.Count; i++)
        {
            string[] woerter = bausteine[i].Split(' ');
            if (woerter.Length >= 2
                && woerter[^1].Equals(marker, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(woerter[0], out int zahl))
            {
                if (zahl - 1 >= 1)
                {
                    bausteine[i] = $"{zahl - 1} {marker}";
                }
                else
                {
                    bausteine.RemoveAt(i);
                }
                break;
            }
        }

        return Zusammenfuegen(bausteine);
    }

    // Festen Text an-/abschalten (Personal-Begleitung).
    public static string TextUmschalten(string text, string baustein)
    {
        var bausteine = Bausteine(text);
        int gefunden = bausteine.FindIndex(b =>
            b.Equals(baustein, StringComparison.OrdinalIgnoreCase));

        if (gefunden >= 0)
        {
            bausteine.RemoveAt(gefunden);
        }
        else
        {
            bausteine.Add(baustein);
        }

        return Zusammenfuegen(bausteine);
    }

    // Info als "(Text)" anhängen; gleicher Text ersetzt sich selbst.
    // Leerer Text entfernt alle Infos, die Stunden bleiben (wie im Original).
    public static string InfoSetzen(string text, string info)
    {
        var bausteine = Bausteine(text);

        if (info.Trim() == "")
        {
            return Zusammenfuegen(bausteine.Where(b => !IstInfo(b)));
        }

        // " / " trennt die Bausteine — in der Info würde es sie zerlegen.
        string neu = $"({info.Trim().Replace(" / ", ", ")})";
        bausteine.RemoveAll(b => b.Equals(neu, StringComparison.OrdinalIgnoreCase));
        bausteine.Add(neu);
        return Zusammenfuegen(bausteine);
    }
}
