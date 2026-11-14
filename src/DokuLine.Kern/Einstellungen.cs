using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DokuLine.Kern;

// Einstellungen als lesbare Datei einstellungen.json im Datenordner (Schlüssel und Vorgaben: docs/technik.md).
// Eine kaputte oder fremde Datei führt nie zum Absturz; dann gelten die Vorgaben.
public sealed record EinstellungsWerte(bool AutoUpdate = true, string Kuerzel = "", string? Logo = null, int BeschlussFristTage = 90, int HebFristMonate = 12,
    string Bereich = Bereiche.Vorgabe, string? DatenbankOrdner = null, bool MenueEingeklappt = false, bool NachschlageAnsicht = true,
    int WartezeitMinuten = AbgaengigkeitRegeln.WartezeitVorgabe, int KeinBerichtWochen = MassnahmenRegeln.KeinBerichtWochenVorgabe,
    int ZieleLeitziele = ZielRegeln.LeitzieleVorgabe, int ZieleNahziele = ZielRegeln.NahzieleVorgabe,
    string QmAus = "", int QmTage = QualitaetRegeln.TageVorgabe);

public static partial class Einstellungen
{
    public const string Datei = "einstellungen.json";
    public const string LogoOrdner = "logo";
    public const int KuerzelLaenge = 6;

    [GeneratedRegex(@"^[\p{L}\d]*$")]
    private static partial Regex KuerzelMuster();

    // Viele Seiten fragen bei jedem Aufbau nach den Einstellungen: Die Datei wird nur neu gelesen, wenn sie (Pfad, Zeit,
    // Größe) sich geändert hat – auch wenn ein anderes Programm sie geschrieben hat.
    private static (string Pfad, DateTime Zeit, long Groesse, EinstellungsWerte Werte)? zuletzt;
    private static readonly Lock Sperre = new();

    public static EinstellungsWerte Laden()
    {
        string pfad = Pfade.ImBenutzerordner(Datei);
        var info = new FileInfo(pfad);
        var stempel = info.Exists ? (info.LastWriteTimeUtc, info.Length) : (DateTime.MinValue, -1L);
        lock (Sperre)
            if (zuletzt is var (p, z, g, w) && p == pfad && z == stempel.Item1 && g == stempel.Item2) return w;
        var werte = DateiLesen(pfad);
        lock (Sperre) zuletzt = (pfad, stempel.Item1, stempel.Item2, werte);
        return werte;
    }

    private static EinstellungsWerte DateiLesen(string pfad)
    {
        var werte = new EinstellungsWerte();
        try
        {
            var knoten = JsonNode.Parse(File.ReadAllText(pfad)) as JsonObject;
            if (knoten is null) return werte;
            if (knoten["auto_update"] is JsonValue a && a.TryGetValue(out bool an)) werte = werte with { AutoUpdate = an };
            if (knoten["kuerzel"] is JsonValue k && k.TryGetValue(out string? kuerzel) && KuerzelPruefen(kuerzel) is null)
                werte = werte with { Kuerzel = kuerzel.Trim() };
            if (knoten["logo"] is JsonValue l && l.TryGetValue(out string? logo) && !string.IsNullOrWhiteSpace(logo))
                werte = werte with { Logo = logo };
            if (knoten["beschluss_frist_tage"] is JsonValue f && f.TryGetValue(out int tage) && FristPruefen(tage) is null)
                werte = werte with { BeschlussFristTage = tage };
            if (knoten["heb_frist_monate"] is JsonValue h && h.TryGetValue(out int monate) && HebFristPruefen(monate) is null)
                werte = werte with { HebFristMonate = monate };
            if (knoten["bereich"] is JsonValue r && r.TryGetValue(out string? bereich) && Bereiche.Pruefen(bereich) is null)
                werte = werte with { Bereich = Bereiche.Bereinigen(bereich) };
            if (knoten["datenbank_ordner"] is JsonValue o && o.TryGetValue(out string? ordner) && !string.IsNullOrWhiteSpace(ordner))
                werte = werte with { DatenbankOrdner = ordner.Trim() };
            if (knoten["menue_eingeklappt"] is JsonValue m && m.TryGetValue(out bool eingeklappt))
                werte = werte with { MenueEingeklappt = eingeklappt };
            if (knoten["nachschlage_ansicht"] is JsonValue n && n.TryGetValue(out bool ansicht))
                werte = werte with { NachschlageAnsicht = ansicht };
            if (knoten["wartezeit_minuten"] is JsonValue wz && wz.TryGetValue(out int wartezeit) && AbgaengigkeitRegeln.WartezeitPruefen(wartezeit) is null)
                werte = werte with { WartezeitMinuten = wartezeit };
            if (knoten["kein_bericht_wochen"] is JsonValue kb && kb.TryGetValue(out int wochen) && KeinBerichtPruefen(wochen) is null)
                werte = werte with { KeinBerichtWochen = wochen };
            if (knoten["ziele_leitziele"] is JsonValue zl && zl.TryGetValue(out int leit) && leit is >= 0 and <= 3) werte = werte with { ZieleLeitziele = leit };
            if (knoten["ziele_nahziele"] is JsonValue zn && zn.TryGetValue(out int nah) && nah is >= 0 and <= 6) werte = werte with { ZieleNahziele = nah };
            if (knoten["qm_aus"] is JsonValue qa && qa.TryGetValue(out string? aus)) werte = werte with { QmAus = QualitaetRegeln.AusText(QualitaetRegeln.AusLesen(aus ?? "")) };
            if (knoten["qm_tage"] is JsonValue qt && qt.TryGetValue(out int qtage) && qtage is >= 1 and <= 90) werte = werte with { QmTage = qtage };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            // fehlt oder unlesbar: Vorgaben
        }
        return werte;
    }

    public static void Speichern(EinstellungsWerte werte)
    {
        if (KuerzelPruefen(werte.Kuerzel) is string fehler) throw new Eingabefehler([fehler]);
        if (FristPruefen(werte.BeschlussFristTage) is string fristFehler) throw new Eingabefehler([fristFehler]);
        if (HebFristPruefen(werte.HebFristMonate) is string hebFehler) throw new Eingabefehler([hebFehler]);
        if (Bereiche.Pruefen(werte.Bereich) is string bereichFehler) throw new Eingabefehler([bereichFehler]);
        if (AbgaengigkeitRegeln.WartezeitPruefen(werte.WartezeitMinuten) is string wartezeitFehler) throw new Eingabefehler([wartezeitFehler]);
        if (KeinBerichtPruefen(werte.KeinBerichtWochen) is string keinBerichtFehler) throw new Eingabefehler([keinBerichtFehler]);
        string ziel = Pfade.ImBenutzerordner(Datei);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
        string zwischen = ziel + ".neu";
        var inhalt = new JsonObject
        {
            ["auto_update"] = werte.AutoUpdate,
            ["kuerzel"] = werte.Kuerzel.Trim(),
            ["logo"] = werte.Logo,
            ["beschluss_frist_tage"] = werte.BeschlussFristTage,
            ["heb_frist_monate"] = werte.HebFristMonate,
            ["bereich"] = Bereiche.Bereinigen(werte.Bereich),
            ["datenbank_ordner"] = string.IsNullOrWhiteSpace(werte.DatenbankOrdner) ? null : werte.DatenbankOrdner.Trim(),
            ["menue_eingeklappt"] = werte.MenueEingeklappt,
            ["nachschlage_ansicht"] = werte.NachschlageAnsicht,
            ["wartezeit_minuten"] = werte.WartezeitMinuten,
            ["kein_bericht_wochen"] = werte.KeinBerichtWochen,
            ["ziele_leitziele"] = Math.Clamp(werte.ZieleLeitziele, 0, 3),
            ["ziele_nahziele"] = Math.Clamp(werte.ZieleNahziele, 0, 6),
            ["qm_aus"] = QualitaetRegeln.AusText(QualitaetRegeln.AusLesen(werte.QmAus)),
            ["qm_tage"] = Math.Clamp(werte.QmTage, 1, 90),
        };
        File.WriteAllText(zwischen, inhalt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.Move(zwischen, ziel, overwrite: true);   // erst ganz schreiben, dann tauschen
        lock (Sperre) zuletzt = null;
    }

    // Lebensbereiche: „seit n Wochen kein Bericht“ (Bauplan 39).
    public static string? KeinBerichtPruefen(int wochen) => wochen is >= 1 and <= 26 ? null : "„Kein Bericht seit“: 1 bis 26 Wochen.";

    // Kürzel: leer oder bis zu 6 Buchstaben/Ziffern. Rückgabe: Fehlertext oder null.
    public static string? KuerzelPruefen(string? kuerzel)
    {
        string k = (kuerzel ?? "").Trim();
        if (k.Length > KuerzelLaenge) return $"Kürzel: höchstens {KuerzelLaenge} Zeichen.";
        if (!KuerzelMuster().IsMatch(k)) return "Kürzel: nur Buchstaben und Ziffern.";
        return null;
    }

    public static string? FristPruefen(int tage) =>
        tage is >= 1 and <= 365 ? null : "Frist „Beschluss endet bald“: 1 bis 365 Tage.";

    public static string? HebFristPruefen(int monate) =>
        monate is >= 1 and <= 60 ? null : "Frist „HEB fällig“: 1 bis 60 Monate.";

    // Ändert einen Wert und speichert sofort.
    public static EinstellungsWerte Aendern(Func<EinstellungsWerte, EinstellungsWerte> aenderung)
    {
        var neu = aenderung(Laden());
        Speichern(neu);
        return neu;
    }

    // Eigenes Bild für den Kopf hinterlegen (Kopie im Datenordner, nie im Repository, Regel 7).
    // Ein vorher hinterlegtes Bild wird gelöscht.
    public static EinstellungsWerte LogoSetzen(string? quelle)
    {
        var alt = Laden();
        string? neu = quelle is null ? null : Bilder.Uebernehmen(quelle, LogoOrdner);
        var werte = Aendern(w => w with { Logo = neu });
        if (alt.Logo is not null && alt.Logo != neu) Bilder.Loeschen(alt.Logo, LogoOrdner);
        return werte;
    }

    public static string? LogoPfad() => Bilder.Pfad(Laden().Logo, LogoOrdner);

    // Wo die Datenbank liegt: im gemeinsamen Ordner (Mehrplatz) oder lokal im Datenordner.
    public static string DatenbankPfad(EinstellungsWerte werte) =>
        string.IsNullOrWhiteSpace(werte.DatenbankOrdner) ? Pfade.ImDatenordner(Speicher.Datei) : Path.Combine(werte.DatenbankOrdner, Speicher.Datei);

    public static bool Gemeinsam(EinstellungsWerte werte) => !string.IsNullOrWhiteSpace(werte.DatenbankOrdner);
}
