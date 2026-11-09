using System.Globalization;
using System.Text;
using System.Xml;

namespace DokuLine.Kern;

// Kalender-Austausch über Dateien im Standardformat .ics (iCalendar). Termine
// lassen sich als Datei speichern (z. B. für Outlook) und .ics-Dateien einlesen (z. B. eine
// gespeicherte Einladung). Keine Verbindung zu Outlook, Exchange, Google oder einem Mailserver.
// Uhrzeiten schreibt das Programm als Ortszeit ohne Zeitzone; beim Lesen gelten Zeiten mit „Z“ oder
// TZID=UTC als UTC (werden umgerechnet), alle anderen als Ortszeit. Ein Termin, der sich nicht lesen
// lässt, wird übersprungen.
public sealed record IcsTermin
{
    public string Titel { get; init; } = "";
    public DateTime Beginn { get; init; }
    public DateTime Ende { get; init; }
    public bool Ganztags { get; init; }
    public string Ort { get; init; } = "";
    public string Beschreibung { get; init; } = "";
    public bool Wiederholung { get; init; }   // RRULE: übernommen wird nur der erste Termin

    // „Di 10.11., 09:00–10:00 Uhr“ bzw. „Di 10.11., ganztags“ – wie im Kalender.
    public string ZeitText => KalenderRegeln.ZeitText(new KalenderFassung
    {
        Beginn = Beginn, Ganztags = Ganztags,
        Ende = Ganztags ? (Ende.Date > Beginn.Date ? Ende.Date.AddDays(-1) : Beginn.Date) : Ende,   // ganztags: letzter Tag
    });
}

public static class Ics
{
    public const string Produkt = "-//DokuLine (Demo)//DE";
    public const string Vermerk = "Demo — ausgedachte Daten";

    // ----- Schreiben -----

    // kennung: zufällig je Datenbank (Speicher.IcsKennung) – sonst hätten Termine verschiedener Datenbanken dieselbe UID
    // und ein Kalenderprogramm überschriebe den einen mit dem anderen.
    public static string Schreiben(IEnumerable<KalenderTermin> termine, IReadOnlyDictionary<long, string> namen, DateTime jetzt, string kennung)
    {
        var zeilen = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0", $"PRODID:{Produkt}", "CALSCALE:GREGORIAN", "METHOD:PUBLISH" };
        foreach (var k in termine)
        {
            var f = k.Aktuell;
            zeilen.Add("BEGIN:VEVENT");
            zeilen.Add($"UID:termin-{k.Id}-{kennung}@dokuline.invalid");
            zeilen.Add($"DTSTAMP:{Format(jetzt.ToUniversalTime(), "yyyyMMdd'T'HHmmss'Z'")}");
            if (f.Ganztags)
            {
                zeilen.Add($"DTSTART;VALUE=DATE:{Format(f.Beginn, "yyyyMMdd")}");
                zeilen.Add($"DTEND;VALUE=DATE:{KalenderRegeln.LetzterTag(f).AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture)}");
            }
            else
            {
                zeilen.Add($"DTSTART:{Format(f.Beginn, "yyyyMMdd'T'HHmmss")}");
                zeilen.Add($"DTEND:{Format(f.Ende, "yyyyMMdd'T'HHmmss")}");
            }
            zeilen.Add($"SUMMARY:{Maskieren(f.Titel)}");
            if (f.Ort.Length > 0) zeilen.Add($"LOCATION:{Maskieren(f.Ort)}");
            var beschreibung = new List<string> { $"{KalenderRegeln.ArtText(f.Art)}{(f.Haus ? " (Haus)" : "")}" };
            if (f.Bewohner.Count > 0) beschreibung.Add("Bewohner: " + string.Join(", ", f.Bewohner.Select(b => namen.GetValueOrDefault(b, "?"))));
            if (f.Treffpunkt.Length > 0) beschreibung.Add($"Treffpunkt: {f.Treffpunkt}");
            if (f.Begleitung.Length > 0) beschreibung.Add($"Begleitung: {f.Begleitung}");
            if (f.Hinweis.Length > 0) beschreibung.Add(f.Hinweis);
            beschreibung.Add(Vermerk);
            zeilen.Add($"DESCRIPTION:{Maskieren(string.Join("\n", beschreibung))}");
            zeilen.Add("END:VEVENT");
        }
        zeilen.Add("END:VCALENDAR");
        return string.Concat(zeilen.SelectMany(Falten).Select(z => z + "\r\n"));
    }

    // Immer gregorianisch mit westlichen Ziffern, unabhängig von der Sprache des Rechners.
    private static string Format(DateTime zeit, string format) => zeit.ToString(format, CultureInfo.InvariantCulture);

    private static string Maskieren(string text) =>
        text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\n").Replace("\n", "\\n");

    // Zeilen höchstens 75 Bytes (UTF-8), Fortsetzung beginnt mit einem Leerzeichen (RFC 5545, 3.1).
    private static IEnumerable<string> Falten(string zeile)
    {
        var teil = new StringBuilder();
        int bytes = 0, grenze = 75;
        foreach (var r in zeile.EnumerateRunes())
        {
            int laenge = r.Utf8SequenceLength;
            if (bytes + laenge > grenze)
            {
                yield return teil.ToString();
                teil.Clear().Append(' ');
                bytes = 1;
                grenze = 75;
            }
            teil.Append(r.ToString());
            bytes += laenge;
        }
        yield return teil.ToString();
    }

    // ----- Lesen -----

    public static List<IcsTermin> Lesen(string text)
    {
        // Entfalten: Zeilen, die mit Leerzeichen oder Tab beginnen, gehören zur vorigen.
        var zeilen = new List<string>();
        foreach (var roh in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            if (roh.Length > 0 && (roh[0] == ' ' || roh[0] == '\t') && zeilen.Count > 0) zeilen[^1] += roh[1..];
            else if (roh.Length > 0) zeilen.Add(roh);
        }
        var termine = new List<IcsTermin>();
        Dictionary<string, (Dictionary<string, string> Parameter, string Wert)>? ereignis = null;
        int tiefe = 0;   // verschachtelte Blöcke im Ereignis (z. B. VALARM) überspringen
        foreach (var zeile in zeilen)
        {
            int doppelpunkt = WertBeginn(zeile);
            if (doppelpunkt < 0) continue;
            string kopf = zeile[..doppelpunkt], wert = zeile[(doppelpunkt + 1)..];
            var teile = kopf.Split(';');
            string name = teile[0].Trim().ToUpperInvariant();
            if (name == "BEGIN")
            {
                if (wert.Trim().Equals("VEVENT", StringComparison.OrdinalIgnoreCase) && ereignis is null) { ereignis = []; tiefe = 0; }
                else if (ereignis is not null) tiefe++;
                continue;
            }
            if (name == "END")
            {
                if (ereignis is not null && tiefe > 0) { tiefe--; continue; }
                if (ereignis is not null && wert.Trim().Equals("VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    try { if (AlsTermin(ereignis) is IcsTermin t) termine.Add(t); }
                    catch (Exception x) when (x is ArgumentException or FormatException or OverflowException) { }   // überspringen
                    ereignis = null;
                }
                continue;
            }
            if (ereignis is null || tiefe > 0 || ereignis.ContainsKey(name)) continue;
            var parameter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in teile.Skip(1))
            {
                int gleich = p.IndexOf('=');
                if (gleich > 0) parameter[p[..gleich].Trim()] = p[(gleich + 1)..].Trim().Trim('"');
            }
            ereignis[name] = (parameter, wert);
        }
        return termine;
    }

    // Der Doppelpunkt zwischen Name/Parametern und Wert (Doppelpunkte in Anführungszeichen zählen nicht).
    private static int WertBeginn(string zeile)
    {
        bool zitat = false;
        for (int i = 0; i < zeile.Length; i++)
        {
            if (zeile[i] == '"') zitat = !zitat;
            else if (zeile[i] == ':' && !zitat) return i;
        }
        return -1;
    }

    private static IcsTermin? AlsTermin(Dictionary<string, (Dictionary<string, string> Parameter, string Wert)> e)
    {
        if (!e.TryGetValue("DTSTART", out var start) || Zeitpunkt(start.Parameter, start.Wert) is not (DateTime beginn, bool ganztags)) return null;
        DateTime ende;
        if (e.TryGetValue("DTEND", out var schluss) && Zeitpunkt(schluss.Parameter, schluss.Wert) is (DateTime ee, _)) ende = ee;
        else if (e.TryGetValue("DURATION", out var dauer) && Dauer(dauer.Wert) is TimeSpan d) ende = beginn + d;
        else ende = ganztags ? beginn.AddDays(1) : beginn.AddHours(1);
        if (ende <= beginn) ende = ganztags ? beginn.AddDays(1) : beginn.AddHours(1);   // nie Länge 0
        string Text(string feld) => e.TryGetValue(feld, out var w) ? Entmaskieren(w.Wert).Trim() : "";
        string titel = Text("SUMMARY");
        return new IcsTermin
        {
            Titel = titel.Length > 0 ? titel : "(ohne Titel)", Beginn = beginn, Ende = ende, Ganztags = ganztags,
            Ort = Text("LOCATION"), Beschreibung = Text("DESCRIPTION"), Wiederholung = e.ContainsKey("RRULE"),
        };
    }

    private static (DateTime, bool)? Zeitpunkt(Dictionary<string, string> parameter, string wert)
    {
        wert = wert.Trim();
        bool nurDatum = parameter.TryGetValue("VALUE", out var art) && art.Equals("DATE", StringComparison.OrdinalIgnoreCase) || wert.Length == 8;
        if (nurDatum)
            return DateTime.TryParseExact(wert, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tag) ? (tag, true) : null;
        bool utc = wert.EndsWith('Z') ||
                   parameter.TryGetValue("TZID", out var zone) && zone.Trim() is "UTC" or "Etc/UTC" or "GMT" or "Etc/GMT" or "Z" or "Coordinated Universal Time";
        if (!DateTime.TryParseExact(wert.TrimEnd('Z'), ["yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var zeit))
            return null;
        return (utc ? DateTime.SpecifyKind(zeit, DateTimeKind.Utc).ToLocalTime() : zeit, false);
    }

    // „PT1H30M“, „P1D“; dazu „P2W“ (Wochen kennt XmlConvert nicht).
    private static TimeSpan? Dauer(string wert)
    {
        wert = wert.Trim().TrimStart('+');
        if (wert.Length > 2 && wert[0] == 'P' && wert[^1] == 'W' && int.TryParse(wert[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out int wochen))
            return TimeSpan.FromDays(7.0 * wochen);
        try { return XmlConvert.ToTimeSpan(wert); }
        catch (Exception x) when (x is FormatException or OverflowException) { return null; }
    }

    private static string Entmaskieren(string text)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                char n = text[++i];
                sb.Append(n is 'n' or 'N' ? '\n' : n);
            }
            else sb.Append(text[i]);
        }
        return sb.ToString();
    }

    // Für den Import: aus einem gelesenen Termin die Eingabe für den Kalender (Prüfung dort).
    public static KalenderEingabe AlsEingabe(IcsTermin t, KalenderArt art, bool haus, IReadOnlyList<long> bewohner, string kuerzel)
    {
        var letzterTag = t.Ganztags ? DateOnly.FromDateTime(t.Ende).AddDays(-1) : DateOnly.FromDateTime(t.Ende);
        if (letzterTag < DateOnly.FromDateTime(t.Beginn)) letzterTag = DateOnly.FromDateTime(t.Beginn);
        string hinweis = "aus einer .ics-Datei" + (t.Wiederholung ? " (Wiederholung: nur der erste Termin übernommen)" : "") +
                         (t.Beschreibung.Length > 0 ? $": {t.Beschreibung.Replace('\n', ' ')}" : "");
        return new KalenderEingabe
        {
            Art = art, Titel = t.Titel, BeginnDatum = Datum.Text(DateOnly.FromDateTime(t.Beginn)), BeginnUhrzeit = t.Ganztags ? "" : t.Beginn.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndeDatum = Datum.Text(letzterTag), EndeUhrzeit = t.Ganztags ? "" : t.Ende.ToString("HH:mm", CultureInfo.InvariantCulture),
            Ganztags = t.Ganztags, Ort = t.Ort, Hinweis = hinweis.Length > 400 ? hinweis[..400] + " …" : hinweis,
            Bewohner = art == KalenderArt.Wohnbereich ? [] : [.. bewohner], Haus = haus, Kuerzel = kuerzel,
        };
    }
}
