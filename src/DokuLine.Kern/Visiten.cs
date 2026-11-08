namespace DokuLine.Kern;

// Visiten (Bauplan Schritt 26): was besprochen wurde, ob sich Anordnungen geändert haben und ob ein
// Facharzttermin nötig ist – je Bewohner. Beim Speichern entstehen sofort Aufgaben (je geänderter
// Anordnung eine, je Facharzttermin eine) und je Bewohner ein Bericht (Lebensbereich 5, mit
// Übergabe, wenn sich etwas ändert). Die Medikation selbst ändert das Programm nicht.

public enum AnordnungArt { Neu, Dosis, Pause, Absetzen }

public sealed record VisiteAnordnung(long Id, AnordnungArt Art, long? MedikamentId, string Medikament);

public sealed record VisiteEintrag
{
    public long Id { get; init; }
    public long BewohnerId { get; init; }
    public string Besprochen { get; init; } = "";
    public IReadOnlyList<VisiteAnordnung> Anordnungen { get; init; } = [];
    public string Facharzt { get; init; } = "";   // Fachrichtung; leer = kein Termin nötig
    public bool Dringend { get; init; }
}

public sealed record Visite
{
    public long Id { get; init; }
    public DateOnly Datum { get; init; }
    public string Art { get; init; } = "";   // seit Schritt 26c nicht mehr erfasst (ältere Visiten behalten sie)
    public string Arzt { get; init; } = "";
    public string Teilnehmer { get; init; } = "";
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
    public IReadOnlyList<VisiteEintrag> Eintraege { get; init; } = [];
}

public sealed class VisiteEintragEingabe
{
    public long BewohnerId { get; set; }
    public string Besprochen { get; set; } = "";
    public List<(AnordnungArt Art, long? MedikamentId, string Medikament)> Anordnungen { get; set; } = [];
    public string Facharzt { get; set; } = "";
    public bool FacharztNoetig { get; set; }   // Häkchen „Facharzttermin nötig“ (dann muss die Fachrichtung da sein)
    public bool Dringend { get; set; }
}

public sealed class VisiteEingabe
{
    public string Datum { get; set; } = "";
    public string Art { get; set; } = "";
    public string Arzt { get; set; } = "";
    public string Teilnehmer { get; set; } = "";
    public string Kuerzel { get; set; } = "";
    public List<VisiteEintragEingabe> Eintraege { get; set; } = [];
}

// Was die Aufgabe einer Visite tun soll.
public enum VisitenAktion { MedikamentAnlegen, MedikationAendern, FacharztTermin }

public static class VisitenRegeln
{

    public static string ArtText(AnordnungArt a) => a switch
    {
        AnordnungArt.Neu => "neu verordnet",
        AnordnungArt.Dosis => "Dosis geändert",
        AnordnungArt.Pause => "pausiert",
        _ => "abgesetzt",
    };

    public static string Kuerzel(AnordnungArt a) => a switch { AnordnungArt.Neu => "neu", AnordnungArt.Dosis => "dosis", AnordnungArt.Pause => "pause", _ => "absetzen" };
    public static AnordnungArt ArtAus(string s) => s switch { "neu" => AnordnungArt.Neu, "dosis" => AnordnungArt.Dosis, "pause" => AnordnungArt.Pause, _ => AnordnungArt.Absetzen };

    public static Visite AusEingabe(VisiteEingabe e, DateTime jetzt)
    {
        var fehler = new List<string>();
        DateOnly datum = default;
        try
        {
            datum = Kern.Datum.Lesen(e.Datum, "Datum");
            if (datum > DateOnly.FromDateTime(jetzt)) fehler.Add("Die Visite liegt in der Zukunft – bitte nach der Visite eintragen.");
        }
        catch (FormatException x) { fehler.Add(x.Message); }
        if (string.IsNullOrWhiteSpace(e.Arzt)) fehler.Add("Arzt fehlt (aus dem Ärzteverzeichnis wählen).");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        var eintraege = e.Eintraege.Where(x => x.Besprochen.Trim().Length > 0 || x.Anordnungen.Count > 0 || x.Facharzt.Trim().Length > 0 || x.FacharztNoetig).ToList();
        if (eintraege.Count == 0) fehler.Add("Bitte für mindestens einen Bewohner eintragen, was besprochen wurde.");
        foreach (var x in eintraege)
        {
            if (x.Besprochen.Trim().Length == 0) fehler.Add("Bei jedem Bewohner bitte eintragen, was besprochen wurde.");
            if (x.Anordnungen.Any(a => a.Medikament.Trim().Length == 0)) fehler.Add("Geänderte Anordnung: Medikament fehlt.");
            if (x.FacharztNoetig && x.Facharzt.Trim().Length == 0) fehler.Add("Facharzttermin nötig: Fachrichtung fehlt.");
        }
        if (fehler.Count > 0) throw new Eingabefehler(fehler.Distinct().ToList());
        return new Visite
        {
            Datum = datum, Art = e.Art.Trim(), Arzt = e.Arzt.Trim(), Teilnehmer = e.Teilnehmer.Trim(), Erstellt = jetzt, Kuerzel = e.Kuerzel.Trim(),
            Eintraege = eintraege.Select(x => new VisiteEintrag
            {
                BewohnerId = x.BewohnerId, Besprochen = x.Besprochen.Trim(),
                Anordnungen = x.Anordnungen.Select(a => new VisiteAnordnung(0, a.Art, a.MedikamentId, a.Medikament.Trim())).ToList(),
                Facharzt = x.Facharzt.Trim(), Dringend = x.Facharzt.Trim().Length > 0 && x.Dringend,
            }).ToList(),
        };
    }

    // „Visite 08.11.2026 (Dr. Linde (ausgedacht))“; ältere Visiten mit Art: „(Psychiater, Dr. Linde …)“.
    public static string Kopf(Visite v) => $"Visite {Kern.Datum.Text(v.Datum)} ({(v.Art.Length > 0 ? $"{v.Art}, " : "")}{v.Arzt})";

    public static string AufgabeText(Visite v, VisiteAnordnung a) =>
        a.MedikamentId is null
            ? $"Neues Medikament anlegen: {a.Medikament} ({ArtText(a.Art)}) – {Kopf(v)}"
            : $"Medikation ändern: {a.Medikament} ({ArtText(a.Art)}) – {Kopf(v)}";

    public static string FacharztText(Visite v, VisiteEintrag e) =>
        $"Termin bei {e.Facharzt} vereinbaren{(e.Dringend ? " (dringend)" : "")} – {Kopf(v)}";

    // Der Bericht je Bewohner (Quelle für Monatsreflexion und HEB-Bogen, Lebensbereich 5).
    public static string BerichtText(Visite v, VisiteEintrag e)
    {
        var teile = new List<string> { $"{Kopf(v)}{(v.Teilnehmer.Length > 0 ? $", dabei: {v.Teilnehmer}" : "")}: {e.Besprochen}" };
        if (e.Anordnungen.Count > 0)
            teile.Add("Anordnungen geändert: " + string.Join(", ", e.Anordnungen.Select(a => $"{a.Medikament} {ArtText(a.Art)}")) + ".");
        else teile.Add("Anordnungen nicht geändert.");
        if (e.Facharzt.Length > 0) teile.Add($"Facharzttermin nötig: {e.Facharzt}{(e.Dringend ? ", dringend" : "")}.");
        return string.Join(" ", teile);
    }
}
