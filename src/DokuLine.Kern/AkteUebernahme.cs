using System.Globalization;
using System.Security.Cryptography;

namespace DokuLine.Kern;

// Vorhandene digitale Akte übernehmen (Bauplan 32): ein Hauptordner mit einem Unterordner je Bewohner, darin Unterordner
// je Kategorie. Das Programm schlägt die Zuordnung vor (Bewohner nach Name, Kategorie nach Ordnername), zeigt eine
// Vorschau und **kopiert** dann – der alte Ordner bleibt unverändert.
public sealed record UebernahmeDatei(string Pfad, string Kategorie, string Titel, DateOnly Datum, long Groesse);

public sealed record UebernahmeOrdner(string Pfad, string Name, long? BewohnerId, IReadOnlyList<UebernahmeDatei> Dateien);

public sealed record UebernahmeErgebnis(int Uebernommen, int Doppelt, IReadOnlyList<string> Fehler, IReadOnlyList<string> Uebersprungen);

public static class AkteUebernahme
{
    public const string Grund = "übernommen aus der digitalen Akte";
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private static string[] Teile(string text) =>
        text.ToLower(De).Split([' ', ',', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries);

    // Ein Ordner gehört zu dem Bewohner, dessen Nachname und Vorname beide im Ordnernamen stehen; sonst zu dem einzigen
    // mit diesem Nachnamen. Mehrdeutig oder unbekannt: keiner (zum Bestätigen in der Vorschau).
    public static long? Zuordnen(string ordnername, IReadOnlyList<Bewohner> bewohner)
    {
        var teile = Teile(ordnername);
        var voll = bewohner.Where(b => teile.Contains(b.Nachname.ToLower(De)) && teile.Contains(b.Vorname.ToLower(De))).ToList();
        if (voll.Count == 1) return voll[0].Id;
        if (voll.Count > 1) return null;
        var nachname = bewohner.Where(b => teile.Contains(b.Nachname.ToLower(De))).ToList();
        // Nur der Nachname im Ordnernamen (ohne weiteres Wort, das ein anderer Vorname sein könnte): „Ahorn, Berta“ gehört
        // nicht zu Anton Ahorn.
        return nachname.Count == 1 && teile.All(t => t == nachname[0].Nachname.ToLower(De) || t.Any(char.IsDigit)) ? nachname[0].Id : null;
    }

    // Gleicher Name wie eine Kategorie (ohne Groß/klein), sonst Vorschlag nach Stichwörtern.
    public static string KategorieFuerOrdner(string ordnername, IReadOnlyList<string> kategorien) =>
        kategorien.FirstOrDefault(k => k.Equals(ordnername.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? DokumentRegeln.KategorieVorschlag(ordnername, kategorien);

    public static List<UebernahmeOrdner> Planen(string hauptordner, IReadOnlyList<Bewohner> bewohner, IReadOnlyList<string> kategorien)
    {
        var plan = new List<UebernahmeOrdner>();
        foreach (var ordner in new DirectoryInfo(hauptordner).EnumerateDirectories().Where(Sichtbar).OrderBy(d => d.Name, StringComparer.CurrentCulture))
        {
            var dateien = new List<UebernahmeDatei>();
            // Dateien direkt im Ordner des Bewohners: Kategorie nach dem Dateinamen.
            foreach (var f in ordner.EnumerateFiles().Where(Uebernehmbar))
                dateien.Add(Datei(f, DokumentRegeln.KategorieVorschlag(f.Name, kategorien)));
            foreach (var unter in ordner.EnumerateDirectories().Where(Sichtbar))
            {
                string kategorie = KategorieFuerOrdner(unter.Name, kategorien);
                foreach (var f in unter.EnumerateFiles("*", SearchOption.AllDirectories).Where(Uebernehmbar)) dateien.Add(Datei(f, kategorie));
            }
            plan.Add(new UebernahmeOrdner(ordner.FullName, ordner.Name, Zuordnen(ordner.Name, bewohner), dateien));
        }
        return plan;
    }

    private static bool Uebernehmbar(FileInfo f) => Sichtbar(f) && DokumentRegeln.Erlaubt(f.Name);

    private static bool Sichtbar(FileSystemInfo f) => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System)
                                                      && !f.Name.StartsWith('.') && !f.Name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase);

    private static UebernahmeDatei Datei(FileInfo f, string kategorie) =>
        new(f.FullName, kategorie, DokumentRegeln.TitelVorschlag(f.Name), DateOnly.FromDateTime(f.LastWriteTime), f.Length);

    public static string Pruefsumme(string pfad)
    {
        using var strom = File.OpenRead(pfad);
        return Convert.ToHexString(SHA256.HashData(strom));
    }

    // Bericht nach dem Übernehmen.
    public static string Bericht(UebernahmeErgebnis e) =>
        $"Übernommen: {e.Uebernommen} · schon vorhanden (übersprungen): {e.Doppelt} · nicht zugeordnet: {e.Uebersprungen.Count} · Fehler: {e.Fehler.Count}" +
        string.Concat(e.Uebersprungen.Select(x => $"\n– nicht übernommen: {x}")) + string.Concat(e.Fehler.Select(x => $"\n– Fehler: {x}"));

    // Demo: ein ausgedachter Ordner zum Vorführen (drei Bewohner, ein Ordner ohne passenden Bewohner).
    public static string DemoOrdnerAnlegen(string ziel, IReadOnlyList<Bewohner> bewohner)
    {
        string wurzel = Path.Combine(ziel, "Digitale Akte (Demo)");
        if (Directory.Exists(wurzel)) Directory.Delete(wurzel, recursive: true);
        void Pdf(string pfad, string titel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
            File.WriteAllBytes(pfad, DemoPdf.Erzeugen(titel, ["Übernahme aus der digitalen Akte (ausgedacht)"]));
        }
        foreach (var (b, i) in bewohner.OrderBy(b => b.Nachname, StringComparer.CurrentCulture).Take(3).Select((b, i) => (b, i)))
        {
            string ordner = Path.Combine(wurzel, i == 1 ? $"{b.Nachname}_{b.Vorname}" : $"{b.Nachname}, {b.Vorname}");
            Pdf(Path.Combine(ordner, "Bescheide", "Kostenzusage 2025.pdf"), "Kostenzusage 2025");
            Pdf(Path.Combine(ordner, "Arztbriefe", "Arztbrief Neurologie.pdf"), "Arztbrief Neurologie");
            Pdf(Path.Combine(ordner, "Heimvertrag.pdf"), "Heimvertrag");
        }
        Pdf(Path.Combine(wurzel, "Muster, Max (ausgezogen)", "Brief.pdf"), "Brief");
        return wurzel;
    }
}
