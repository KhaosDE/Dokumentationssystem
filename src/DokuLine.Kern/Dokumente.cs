using System.Globalization;
using System.Text;

namespace DokuLine.Kern;

// Akte, Reiter „Dokumente“ (Bauplan 31): Jeder Bewohner hat eine Ablage in DokuLine. Dateien werden hineinkopiert (das
// Original bleibt), die Datenbank kennt Kategorie, Titel, Datum, „gültig bis“, Hinweis, wer, wann und die Prüfsumme.
// Nichts geht verloren: „Entfernen“ legt in den Papierkorb, endgültig löschen nur mit eigenem Recht; jedes Öffnen,
// Hinzufügen, Entfernen steht im Protokoll.
public sealed record Dokument
{
    public long Id { get; init; }
    public long BewohnerId { get; init; }
    public string Kategorie { get; init; } = "";
    public string Titel { get; init; } = "";
    public DateOnly Datum { get; init; }
    public DateOnly? GueltigBis { get; init; }
    public string Hinweis { get; init; } = "";
    public string Datei { get; init; } = "";              // Name in der Ablage des Bewohners
    public string Original { get; init; } = "";           // Dateiname beim Hinzufügen
    public long Groesse { get; init; }
    public string Pruefsumme { get; init; } = "";         // SHA-256, hexadezimal
    public DateTime Hinzugefuegt { get; init; }
    public string Kuerzel { get; init; } = "";
    public long? Vorgaenger { get; init; }                // „Ersetzen“: die vorige Fassung
    public bool Entfernt { get; init; }                   // im Papierkorb
    public bool Geloescht { get; init; }                  // Datei endgültig gelöscht
    public bool Ersetzt { get; init; }                    // es gibt eine neuere Fassung

    public bool Abgelaufen(DateOnly heute) => GueltigBis is DateOnly b && b < heute;
}

public sealed record DokumentEingabe
{
    public string Kategorie { get; init; } = "";
    public string Titel { get; init; } = "";
    public DateOnly Datum { get; init; }
    public DateOnly? GueltigBis { get; init; }
    public string Hinweis { get; init; } = "";
}

public enum DokumentAktion { Hinzugefuegt, Geoeffnet, Entfernt, Wiederhergestellt, Ersetzt, Geloescht }

public sealed record DokumentEreignis(long DokumentId, DokumentAktion Aktion, DateTime Zeit, string Kuerzel, string Grund, string Titel);

public static class DokumentRegeln
{
    public const string Ordner = "Dokumente";
    public const long Hoechstgroesse = 50L * 1024 * 1024;   // 50 MB je Datei
    public static readonly string[] Kategorienvorgabe =
    [
        "Aufnahme und Verträge", "Betreuung und Gericht", "Kostenträger und Bescheide", "Krankenkasse", "Ärztliches und Befunde",
        "Ausweise und Behörden", "Schriftverkehr", "Sonstiges",
    ];

    // Vorschlag nach dem Dateinamen (Stichwörter); sonst „Sonstiges“ (wenn es die Kategorie gibt).
    private static readonly (string[] Woerter, string Kategorie)[] Stichwoerter =
    [
        (["betreuerausweis", "betreuung", "beschluss", "gericht", "vorsorge"], "Betreuung und Gericht"),
        (["kostenzusage", "bescheid", "bewilligung", "kostentr", "eingliederung"], "Kostenträger und Bescheide"),
        (["krankenkasse", "versicherten", "befreiung", "kasse"], "Krankenkasse"),
        (["arztbrief", "befund", "labor", "entlass", "attest", "diagnose"], "Ärztliches und Befunde"),
        (["vertrag", "aufnahme", "heimvertrag", "wohnvertrag"], "Aufnahme und Verträge"),
        (["personalausweis", "ausweis", "reisepass", "pass", "behörde", "amt"], "Ausweise und Behörden"),
        (["brief", "schreiben", "antwort", "anfrage"], "Schriftverkehr"),
    ];

    public static string KategorieVorschlag(string dateiname, IReadOnlyList<string> kategorien)
    {
        string name = Path.GetFileNameWithoutExtension(dateiname).ToLower(CultureInfo.GetCultureInfo("de-DE"));
        foreach (var (woerter, kategorie) in Stichwoerter)
            if (woerter.Any(name.Contains) && kategorien.Contains(kategorie)) return kategorie;
        return kategorien.Contains("Sonstiges") ? "Sonstiges" : kategorien.FirstOrDefault() ?? "";
    }

    // Titel aus dem Dateinamen: „Betreuerausweis_2026.pdf“ → „Betreuerausweis 2026“.
    public static string TitelVorschlag(string dateiname) =>
        string.Join(' ', Path.GetFileNameWithoutExtension(dateiname).Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));

    // Name in der Ablage: eindeutige Kennung davor, nur ungefährliche Zeichen; die Endung bleibt (sie entscheidet, womit
    // Windows öffnet).
    public static string AblageName(string kennung, string original)
    {
        string endung = Path.GetExtension(original);
        string basis = new([.. Path.GetFileNameWithoutExtension(original).Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')]);
        if (basis.Length > 60) basis = basis[..60];
        endung = new([.. endung.Where(c => char.IsLetterOrDigit(c) || c == '.')]);
        return $"{kennung}-{(basis.Length > 0 ? basis : "dokument")}{(endung.Length is > 1 and <= 10 ? endung : "")}";
    }

    public static List<string> Pruefen(DokumentEingabe e, IReadOnlyCollection<string> kategorien)
    {
        var fehler = new List<string>();
        if (e.Titel.Trim().Length == 0) fehler.Add("Titel fehlt.");
        if (!kategorien.Contains(e.Kategorie)) fehler.Add("Bitte eine Kategorie wählen.");
        if (e.GueltigBis is DateOnly bis && bis < e.Datum) fehler.Add("„gültig bis“ liegt vor dem Datum des Dokuments.");
        return fehler;
    }

    public static string AktionText(DokumentAktion a) => a switch
    {
        DokumentAktion.Hinzugefuegt => "hinzugefügt",
        DokumentAktion.Geoeffnet => "geöffnet",
        DokumentAktion.Entfernt => "in den Papierkorb",
        DokumentAktion.Wiederhergestellt => "wiederhergestellt",
        DokumentAktion.Ersetzt => "ersetzt",
        _ => "endgültig gelöscht",
    };

    // Zu welchem Reiter der Akte gehört eine Kategorie („Dokument öffnen“ dort).
    public static string? KategorieFuer(Angabe art) => art switch
    {
        Angabe.Betreuung or Angabe.Zustaendigkeit => "Betreuung und Gericht",
        Angabe.Kostentraeger => "Kostenträger und Bescheide",
        Angabe.Krankenkasse => "Krankenkasse",
        Angabe.Aerzte => "Ärztliches und Befunde",
        _ => null,
    };
}

// Kleine PDF-Datei für die Demo, ohne Schriftdateien (Helvetica gehört zu den Standardschriften jedes PDF-Programms).
// Groß darauf „DEMO — ausgedachte Daten“ (Regel 1 und 7: keine echten Logos oder Namen).
public static class DemoPdf
{
    public static byte[] Erzeugen(string titel, IEnumerable<string> zeilen)
    {
        var latin = Encoding.Latin1;
        string Text(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace('—', '\u0097');
        var inhalt = new StringBuilder();
        inhalt.Append("BT /F1 34 Tf 0.75 0 0 rg 60 760 Td (").Append(Text("DEMO — ausgedachte Daten")).Append(") Tj ET\n");
        inhalt.Append("BT /F1 20 Tf 0 0 0 rg 60 700 Td (").Append(Text(titel)).Append(") Tj ET\n");
        int y = 660;
        foreach (var z in zeilen) { inhalt.Append("BT /F1 12 Tf 60 ").Append(y).Append(" Td (").Append(Text(z)).Append(") Tj ET\n"); y -= 20; }
        byte[] strom = latin.GetBytes(inhalt.ToString());
        var objekte = new List<byte[]>
        {
            latin.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            latin.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            latin.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>"),
            latin.GetBytes($"<< /Length {strom.Length} >>\nstream\n").Concat(strom).Concat(latin.GetBytes("\nendstream")).ToArray(),
            latin.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
        };
        using var ms = new MemoryStream();
        void Schreiben(string s) { var b = latin.GetBytes(s); ms.Write(b, 0, b.Length); }
        Schreiben("%PDF-1.4\n");
        var stellen = new List<long>();
        for (int i = 0; i < objekte.Count; i++)
        {
            stellen.Add(ms.Position);
            Schreiben($"{i + 1} 0 obj\n");
            ms.Write(objekte[i], 0, objekte[i].Length);
            Schreiben("\nendobj\n");
        }
        long xref = ms.Position;
        Schreiben($"xref\n0 {objekte.Count + 1}\n0000000000 65535 f \n");
        foreach (long s in stellen) Schreiben($"{s:D10} 00000 n \n");
        Schreiben($"trailer\n<< /Size {objekte.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return ms.ToArray();
    }
}
