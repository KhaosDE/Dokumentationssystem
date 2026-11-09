using System.Text.Json;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;

namespace DokuLine.Kern;

// PDF-Bögen des Kostenträgers ausfüllen (PDFsharp, MIT). Die Vorlage teilt der Nutzer in den
// Einstellungen zu; sie liegt im Datenordner (vorlagen/heb-a.pdf …) und kommt nie ins Repository
// (Regel 7). Ausgefüllt wird immer eine Kopie (Ordner „heb“); die Vorlage bleibt unverändert.
// Welche Programmfelder in welches PDF-Feld kommen, steht in heb-zuordnung.json im Datenordner.
public static class HebPdf
{
    public const string VorlagenOrdner = "vorlagen";
    public const string AusgabeOrdner = "heb";
    public const string ZuordnungDatei = "heb-zuordnung.json";

    // PDFsharp braucht eine Schriftquelle (auch zum Lesen der Formularfelder). Unter Windows: die
    // Schriften des Systems. Anderswo (nur die Tests unter Linux): eine vorhandene Schrift.
    static HebPdf()
    {
        if (OperatingSystem.IsWindows()) GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        else if (GlobalFontSettings.FontResolver is null) GlobalFontSettings.FontResolver = new ErsatzSchriften();
    }

    private sealed class ErsatzSchriften : IFontResolver
    {
        private static readonly string[] Kandidaten =
        [
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        ];

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) => new("Ersatz");

        public byte[]? GetFont(string faceName) => Kandidaten.Where(File.Exists).Select(File.ReadAllBytes).FirstOrDefault();
    }

    public static string VorlagePfad(Bogenart art) => Pfade.ImDatenordner(Path.Combine(VorlagenOrdner, $"heb-{art.ToString().ToLowerInvariant()}.pdf"));

    public static string? Vorlage(Bogenart art) => File.Exists(VorlagePfad(art)) ? VorlagePfad(art) : null;

    // Vorlage zuteilen: prüft, dass die Datei ein PDF mit Formularfeldern ist, und kopiert sie.
    public static List<string> VorlageZuteilen(Bogenart art, string quelle)
    {
        var felder = Felder(quelle);
        if (felder.Count == 0) throw new Eingabefehler(["Die PDF hat keine ausfüllbaren Formularfelder."]);
        Directory.CreateDirectory(Path.GetDirectoryName(VorlagePfad(art))!);
        File.Copy(quelle, VorlagePfad(art), overwrite: true);
        return felder;
    }

    public static void VorlageEntfernen(Bogenart art)
    {
        if (File.Exists(VorlagePfad(art))) File.Delete(VorlagePfad(art));
    }

    // Namen aller Formularfelder (auch verschachtelte), sortiert.
    public static List<string> Felder(string pdf)
    {
        try
        {
            using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
            var form = doc.AcroForm;
            if (form is null) return [];
            return form.Fields.DescendantNames.OrderBy(n => n, StringComparer.Ordinal).ToList();
        }
        catch (Exception e) when (e is PdfReaderException or InvalidOperationException or IOException or NotImplementedException)
        {
            throw new Eingabefehler([$"Die Datei lässt sich nicht als PDF lesen: {e.Message}"]);
        }
    }

    // Zuordnung je Bogenart: Programmfeld → PDF-Feld. Ohne Eintrag gilt: gleicher Name.
    public static Dictionary<string, string> Zuordnung(Bogenart art)
    {
        try
        {
            var alle = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Pfade.ImDatenordner(ZuordnungDatei)));
            return alle?.GetValueOrDefault(art.ToString()) ?? [];
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public static void ZuordnungSpeichern(Bogenart art, IReadOnlyDictionary<string, string> zuordnung)
    {
        Dictionary<string, Dictionary<string, string>> alle;
        try { alle = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Pfade.ImDatenordner(ZuordnungDatei))) ?? []; }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { alle = []; }
        alle[art.ToString()] = zuordnung.Where(x => x.Value.Length > 0).ToDictionary(x => x.Key, x => x.Value);
        Directory.CreateDirectory(Pfade.Datenordner);
        File.WriteAllText(Pfade.ImDatenordner(ZuordnungDatei), JsonSerializer.Serialize(alle, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Das PDF-Feld zu einem Programmfeld: aus der Zuordnung, sonst gleicher Name, wenn es ihn gibt.
    public static string? PdfFeld(string programmfeld, IReadOnlyDictionary<string, string> zuordnung, IReadOnlyCollection<string> pdfFelder) =>
        zuordnung.TryGetValue(programmfeld, out var z) && z.Length > 0 ? z : pdfFelder.Contains(programmfeld) ? programmfeld : null;

    // Die Werte aller Programmfelder eines Bogens (Kopf, Texte, Hilfebedarf); Kostenträger und Aktenzeichen aus dem
    // Reiter „Kostenträger“ der Akte (Bauplan 30).
    public static Dictionary<string, string> Werte(HebBogen h, Bewohner b, HebFassung f, Kostentraeger? kosten = null)
    {
        var w = new Dictionary<string, string>
        {
            ["name"] = b.Name, ["geburtsdatum"] = Datum.Text(b.Geburtsdatum), ["aufnahme"] = Datum.Text(b.Aufnahmedatum),
            ["beschlussende"] = BewohnerRegeln.BeschlussText(b), ["von"] = Datum.Text(h.Von), ["bis"] = Datum.Text(h.Bis),
            ["nummer"] = h.Nummer?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
            ["planung"] = HebRegeln.Planungsmonate(b, h.Erstellt)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
            ["erstellt"] = Datum.Text(h.Erstellt),
            ["kostentraeger"] = kosten?.Name ?? "", ["aktenzeichen"] = kosten?.Aktenzeichen ?? "",
        };
        foreach (var feld in HebRegeln.AlleTextfelder(h.Art)) w[feld.Schluessel] = f.Text(feld.Schluessel);
        for (int i = 1; i <= 5; i++) w[$"hilfe{i}"] = f.Hilfebedarf.GetValueOrDefault(i)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
        return w;
    }

    // Füllt eine Kopie der Vorlage und gibt den Pfad zurück. Programmfelder ohne PDF-Feld werden
    // als Liste zurückgemeldet (damit nichts unbemerkt fehlt).
    public static (string Pfad, List<string> OhneFeld) Ausfuellen(string vorlage, string ziel, IReadOnlyDictionary<string, string> werte,
        IReadOnlyDictionary<string, string> zuordnung)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        File.Copy(vorlage, ziel, overwrite: true);
        var ohne = new List<string>();
        using (var doc = PdfReader.Open(ziel, PdfDocumentOpenMode.Modify))
        {
            var form = doc.AcroForm ?? throw new Eingabefehler(["Die Vorlage hat keine Formularfelder."]);
            var namen = form.Fields.DescendantNames.ToHashSet(StringComparer.Ordinal);
            foreach (var (programmfeld, wert) in werte)
            {
                if (wert.Length == 0) continue;
                if (PdfFeld(programmfeld, zuordnung, namen) is not string name) { ohne.Add(programmfeld); continue; }
                if (form.Fields[name] is PdfTextField text) text.Text = wert;
                else ohne.Add(programmfeld);
            }
            // Der Betrachter baut die Ansicht der Felder selbst auf.
            form.Elements.SetBoolean("/NeedAppearances", true);
            doc.Save(ziel);
        }
        return (ziel, ohne);
    }

    public static string AusgabePfad(Bewohner b, HebBogen h) =>
        Pfade.ImDatenordner(Path.Combine(AusgabeOrdner,
            $"HEB-{h.Art}_{b.Nachname}_{b.Vorname}_{h.Bis:yyyy-MM-dd}.pdf".Replace(' ', '_')));
}
