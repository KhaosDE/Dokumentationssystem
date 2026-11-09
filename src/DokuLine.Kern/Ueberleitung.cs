using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DokuLine.Kern;

// Überleitungsbogen (Bauplan 33): ein Blatt für Klinik, Notarzt oder Rettungsdienst, aus vorhandenen Angaben
// zusammengestellt. Fehlendes steht als „nicht erfasst“ da, nie als leeres Feld (Regel 5). Nur angehakte Abschnitte.
public sealed record UeberleitungEingabe
{
    public string Anlass { get; init; } = "";
    public DateTime Zeitpunkt { get; init; }
    public string Begleitung { get; init; } = "";
    public string Besonderheiten { get; init; } = "";
    public IReadOnlySet<string> Abschnitte { get; init; } = UeberleitungRegeln.AlleAbschnitte.ToHashSet();
}

public sealed record UeberleitungAbschnitt(string Titel, IReadOnlyList<string> Zeilen);

// Was das Programm über den Bewohner weiß (vom Speicher zusammengestellt).
public sealed record UeberleitungDaten(Bewohner Bewohner, EinrichtungWerte Einrichtung, IReadOnlyList<Diagnose> Diagnosen, string Notfallhinweise,
    IReadOnlyList<Tagesstand> Medikation, IReadOnlyDictionary<Angabe, AngabeFassung> Angaben, Func<long?, string> Person, DateOnly Heute)
{
    public IReadOnlyList<string> Vitalwerte { get; init; } = [];   // die letzten Werte je Art (Bauplan 36)
}

public sealed record EinrichtungWerte(string Name = "Wohnheim am Musterpark (ausgedacht)", string Anschrift = "Musterweg 1, 00000 Musterstadt (ausgedacht)",
    string Telefon = "0000 555 000");

public static class UeberleitungRegeln
{
    public const string Person = "Person", Allergien = "Allergien", Diagnosen = "Diagnosen und Notfallhinweise", Medikation = "Medikamentenplan",
        Betreuung = "Betreuung und Zuständige", Krankenkasse = "Krankenkasse", Aerzte = "Ärzte", Unterbringung = "Unterbringung",
        Schweigepflicht = "Schweigepflicht", Vitalwerte = "Vitalwerte", Besonderheiten = "Besonderheiten";
    public static readonly string[] AlleAbschnitte =
        [Person, Allergien, Diagnosen, Medikation, Betreuung, Krankenkasse, Aerzte, Unterbringung, Schweigepflicht, Vitalwerte, Besonderheiten];
    private const string Nicht = AngabenRegeln.NichtErfasst;
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static string Titel(Bewohner b) => $"Überleitungsbogen {b.Name}";

    // zusatz: ersetzt einzelne Abschnitte (z. B. in Tests).
    public static List<UeberleitungAbschnitt> Zusammenstellen(UeberleitungDaten d, UeberleitungEingabe e,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? zusatz = null)
    {
        var b = d.Bewohner;
        var liste = new List<UeberleitungAbschnitt>
        {
            new("Überleitung", [$"Anlass: {(e.Anlass.Trim().Length > 0 ? e.Anlass.Trim() : Nicht)}", $"Datum, Uhrzeit: {e.Zeitpunkt.ToString("dd.MM.yyyy HH:mm", De)}",
                $"Begleitung: {(e.Begleitung.Trim().Length > 0 ? e.Begleitung.Trim() : "keine")}",
                $"Von: {d.Einrichtung.Name}, Bereich {b.Wohnbereich}, {d.Einrichtung.Anschrift}, Tel. Dienstzimmer {d.Einrichtung.Telefon}"]),
        };
        void Abschnitt(string titel, IEnumerable<string> zeilen)
        {
            if (!e.Abschnitte.Contains(titel)) return;
            var z = zeilen.ToList();
            liste.Add(new(titel, z.Count > 0 ? z : [Nicht]));
        }
        Abschnitt(Person, [$"Name: {b.Name}", $"Geburtsdatum: {Datum.Text(b.Geburtsdatum)}", $"Zimmer: {b.Zimmer}"]);
        Abschnitt(Allergien, [BewohnerRegeln.AllergieText(b)]);
        Abschnitt(Diagnosen, [.. d.Diagnosen.Where(x => x.Aktuell).Select(x => x.Anzeige),
            d.Notfallhinweise.Trim().Length > 0 ? $"Hinweise für den Notfall: {d.Notfallhinweise.Trim()}" : "Hinweise für den Notfall: keine eingetragen"]);
        Abschnitt(Medikation, MedikationZeilen(d.Medikation, d.Heute));
        Abschnitt(Betreuung, BetreuungZeilen(d));
        Abschnitt(Krankenkasse, d.Angaben.GetValueOrDefault(Angabe.Krankenkasse) is AngabeFassung kf
            ? kf.Lesen<Krankenkasse>() is Krankenkasse k ? [$"{k.Kasse}, Versichertennummer {(k.Nummer.Length > 0 ? k.Nummer : Nicht)}", $"Pflegegrad: {AngabenRegeln.PflegegradText(k.Pflegegrad)}"]
                : [AngabenRegeln.KeineText(Angabe.Krankenkasse)]
            : [Nicht]);
        Abschnitt(Aerzte, [.. AngabenRegeln.Zeilen(Angabe.Aerzte, d.Angaben.GetValueOrDefault(Angabe.Aerzte), d.Person, d.Heute).Select(z => $"{z.Beschriftung}: {z.Wert}")]);
        Abschnitt(Unterbringung, [$"Beschluss: {BewohnerRegeln.BeschlussText(b)}"]);
        Abschnitt(Schweigepflicht, zusatz?.GetValueOrDefault(Schweigepflicht)
            ?? SchweigepflichtRegeln.FuerUeberleitung(d.Angaben.GetValueOrDefault(Angabe.Schweigepflicht), d.Person, d.Heute));
        Abschnitt(Vitalwerte, zusatz?.GetValueOrDefault(Vitalwerte) ?? (d.Vitalwerte.Count > 0 ? d.Vitalwerte : [Nicht]));
        Abschnitt(Besonderheiten, e.Besonderheiten.Trim().Length > 0 ? [e.Besonderheiten.Trim()] : ["keine eingetragen"]);
        return liste;
    }

    // Stand heute: was gilt, auch Bedarf und Pausen.
    private static List<string> MedikationZeilen(IReadOnlyList<Tagesstand> staende, DateOnly heute)
    {
        var z = new List<string>();
        foreach (var s in staende.Where(s => s.Zustand is Zustand.Gilt or Zustand.Pausiert).OrderBy(s => s.Verordnung.Bedarf))
        {
            var v = s.Verordnung;
            string dosis = v.Bedarf ? $"bei Bedarf: {v.BedarfAnlass}, höchstens {(v.BedarfHoechstens.Length > 0 ? v.BedarfHoechstens : "keine Angabe")}"
                : s.Dosis.Text;
            z.Add($"{v.Medikament} {v.Staerke}: {dosis}" + (s.Zustand == Zustand.Pausiert ? $" ({s.Text})" : ""));
        }
        if (z.Count == 0) z.Add($"keine Medikamente (Stand {Datum.Text(heute)})");
        else z.Insert(0, $"Stand {Datum.Text(heute)}:");
        return z;
    }

    public static List<string> BetreuungZeilen(UeberleitungDaten d)
    {
        var z = new List<string>();
        var zust = d.Angaben.GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>();
        bool forensik = ZustaendigkeitRegeln.InForensik(zust);
        var bf = d.Angaben.GetValueOrDefault(Angabe.Betreuung);
        if (bf is null) z.Add($"Betreuer: {Nicht}");
        else if (bf.Lesen<Betreuung>() is Betreuung bt)
        {
            z.Add($"Betreuer: {d.Person(bt.PersonId)}" + (forensik ? $" – {ZustaendigkeitRegeln.BetreuerNichtZustaendig}" : ""));
            if (bt.Aufgaben.Count > 0) z.Add($"Aufgabenbereiche: {string.Join(", ", bt.Aufgaben)}");
        }
        else z.Add(AngabenRegeln.KeineText(Angabe.Betreuung));
        if (forensik) z.AddRange(zust!.ForensikPersonen.Select(p => $"Zuständig (Forensik {zust.Klinik}): {d.Person(p)}"));
        if (ZustaendigkeitRegeln.InFosa(zust, d.Heute)) z.AddRange(zust!.FosaPersonen.Select(p => $"Zuständig (FOSA {zust.Ambulanz}): {d.Person(p)}"));
        return z;
    }

    // Reiner Text (Bericht, Vorschau, Test).
    public static string Text(IReadOnlyList<UeberleitungAbschnitt> abschnitte) =>
        string.Join("\n\n", abschnitte.Select(a => a.Titel.ToUpper(De) + "\n" + string.Join("\n", a.Zeilen)));
}

// Angaben der Einrichtung für den Überleitungsbogen (Datenordner, nie im Repository – Regel 7). Vorgabe: ausgedacht.
public static class Einrichtung
{
    public const string Datei = "einrichtung.json";

    public static EinrichtungWerte Laden()
    {
        try
        {
            if (JsonNode.Parse(File.ReadAllText(Pfade.ImDatenordner(Datei))) is not JsonObject k) return new();
            string Wert(string name, string vorgabe) => k[name] is JsonValue v && v.TryGetValue(out string? s) && s.Trim().Length > 0 ? s.Trim() : vorgabe;
            var v = new EinrichtungWerte();
            return new(Wert("name", v.Name), Wert("anschrift", v.Anschrift), Wert("telefon", v.Telefon));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return new();
        }
    }

    public static void Speichern(EinrichtungWerte w)
    {
        string ziel = Pfade.ImDatenordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        var inhalt = new JsonObject { ["name"] = w.Name.Trim(), ["anschrift"] = w.Anschrift.Trim(), ["telefon"] = w.Telefon.Trim() };
        File.WriteAllText(ziel, inhalt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}

// Ein schlichtes PDF mit Überschriften und Text über mehrere Seiten (Helvetica, ohne Schriftdateien). Für Bögen wie den
// Überleitungsbogen; oben auf jeder Seite der Hinweis (z. B. „Demo — ausgedachte Daten“).
public static class TextPdf
{
    private const int Breite = 595, Hoehe = 842, Rand = 50, ZeichenJeZeile = 92;

    public static byte[] Erzeugen(string titel, IReadOnlyList<UeberleitungAbschnitt> abschnitte, string hinweis)
    {
        var seiten = new List<StringBuilder>();
        StringBuilder seite = null!;
        int y = 0;
        string T(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace('—', '\u0097').Replace('–', '\u0096')
            .Replace('„', '\u0084').Replace('“', '\u0093').Replace('·', '·');
        void NeueSeite()
        {
            seite = new StringBuilder();
            seiten.Add(seite);
            y = Hoehe - Rand;
            if (hinweis.Length > 0) { seite.Append($"BT /F2 10 Tf 0.75 0 0 rg {Rand} {y} Td ({T(hinweis)}) Tj ET\n"); y -= 22; }
        }
        void Zeile(string text, int groesse, bool fett)
        {
            if (y < Rand + groesse) NeueSeite();
            seite.Append($"BT /{(fett ? "F2" : "F1")} {groesse} Tf 0 0 0 rg {Rand} {y} Td ({T(text)}) Tj ET\n");
            y -= (int)(groesse * 1.4);
        }
        NeueSeite();
        Zeile(titel, 18, true);
        y -= 6;
        foreach (var a in abschnitte)
        {
            y -= 6;
            Zeile(a.Titel, 12, true);
            foreach (var z in a.Zeilen) foreach (var teil in Umbrechen(z)) Zeile(teil, 10, false);
        }
        return Bauen(seiten);
    }

    private static IEnumerable<string> Umbrechen(string text)
    {
        var worte = text.Split(' ');
        var zeile = new StringBuilder();
        foreach (var w in worte)
        {
            if (zeile.Length > 0 && zeile.Length + 1 + w.Length > ZeichenJeZeile) { yield return zeile.ToString(); zeile.Clear().Append("   "); }
            if (zeile.Length > 0 && zeile.ToString() != "   ") zeile.Append(' ');
            zeile.Append(w);
        }
        if (zeile.Length > 0) yield return zeile.ToString();
    }

    private static byte[] Bauen(List<StringBuilder> seiten)
    {
        var latin = Encoding.Latin1;
        var objekte = new List<byte[]>();
        int n = seiten.Count;
        // 1 Katalog, 2 Seitenbaum, 3/4 Schriften, dann je Seite: Seite und Inhalt.
        string kinder = string.Join(" ", Enumerable.Range(0, n).Select(i => $"{5 + 2 * i} 0 R"));
        objekte.Add(latin.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objekte.Add(latin.GetBytes($"<< /Type /Pages /Kids [{kinder}] /Count {n} >>"));
        objekte.Add(latin.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objekte.Add(latin.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
        for (int i = 0; i < n; i++)
        {
            byte[] strom = latin.GetBytes(seiten[i].ToString());
            objekte.Add(latin.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Breite} {Hoehe}] /Contents {6 + 2 * i} 0 R " +
                                       "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>"));
            objekte.Add(latin.GetBytes($"<< /Length {strom.Length} >>\nstream\n").Concat(strom).Concat(latin.GetBytes("\nendstream")).ToArray());
        }
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
