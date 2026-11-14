namespace DokuLine.Kern;

// Qualitätsmanagement (Bauplan 44): Prüfliste je Bewohner mit festen Prüfpunkten, Kennzahlen je Wohnbereich, Prüfvermerk
// und Hinweis an den Wohnbereich. Geprüft werden die Unterlagen der Bewohner, nie einzelne Mitarbeiter: nichts wird nach
// Kürzel ausgewertet (Liste der Freigaben, docs/betrieb.md).
public enum Pruefpunkt { Leitziel, Nahziele, NahzielMassnahme, ZielUeberpruefung, MassnahmenBestaetigt, Lebensbereiche, Reflexion, Heb, Hilfeplan, Sicherheit }

public sealed record PruefErgebnis(Pruefpunkt Punkt, bool Ok, string Text);

public sealed record Pruefvermerk(long Id, long BewohnerId, DateTime Zeit, string Kuerzel, string Bemerkung);

// Hinweis an den Wohnbereich: eine Aufgabe des Bereichs mit dem Mangel als Text; „erledigt“ kommt aus der Aufgabe.
public sealed record QmHinweis(long Id, long BewohnerId, long AufgabeId, string Text, DateTime Gesendet, string Kuerzel, DateTime? Erledigt);

// Was die Prüfung eines Bewohners braucht (aus dem Speicher gelesen).
public sealed record PruefDaten(Bewohner Bewohner, IReadOnlyList<Ziel> Ziele, ZielVerzicht? Verzicht, IReadOnlyList<Massnahme> Massnahmen,
    IReadOnlyList<MassnahmeBestaetigung> Bestaetigungen, IReadOnlyList<Bericht> Berichte, Reflexion LetzteReflexion, string? HebFaellig,
    IReadOnlyDictionary<Angabe, AngabeFassung> Angaben, HilfeplanStand? Hilfeplan = null);

public sealed record PruefZeile(Bewohner Bewohner, IReadOnlyList<PruefErgebnis> Ergebnisse, Pruefvermerk? Vermerk, QmHinweis? Hinweis)
{
    public bool Maengel => Ergebnisse.Any(e => !e.Ok);
    public List<string> MaengelTexte => [.. Ergebnisse.Where(e => !e.Ok).Select(e => $"{QualitaetRegeln.Name(e.Punkt)}: {e.Text}")];
}

public sealed record QmKennzahl(string Bereich, int Bewohner, int ZieleVollstaendig, int NahzieleMitMassnahme, int Nahziele, int UeberpruefungFaellig,
    int ReflexionFaellig, int HebFaellig);

public sealed record QmEinstellung(IReadOnlySet<Pruefpunkt> Aus, int TageUnbestaetigt, int Leitziele, int Nahziele, int KeinBerichtWochen);

public static class QualitaetRegeln
{
    public const string Position = "Qualitätsmanagement (QMB)";
    public const string BereichQm = "QM";
    public const int TageVorgabe = 14;
    public const string Ok = "✓";

    public static readonly Pruefpunkt[] Alle = Enum.GetValues<Pruefpunkt>();

    public static string Name(Pruefpunkt p) => p switch
    {
        Pruefpunkt.Leitziel => "Leitziel vorhanden",
        Pruefpunkt.Nahziele => "Nahziele vorhanden",
        Pruefpunkt.NahzielMassnahme => "Jedes Nahziel hat eine Maßnahme",
        Pruefpunkt.ZielUeberpruefung => "Überprüfung der Ziele nicht fällig",
        Pruefpunkt.MassnahmenBestaetigt => "Maßnahmen werden bestätigt",
        Pruefpunkt.Lebensbereiche => "Berichte je Lebensbereich",
        Pruefpunkt.Reflexion => "Reflexion aktuell",
        Pruefpunkt.Heb => "HEB aktuell",
        Pruefpunkt.Hilfeplan => "Hilfeplangespräch nicht überfällig",
        _ => "Sicherheitsangaben erfasst",
    };

    public static string Kurz(Pruefpunkt p) => p switch
    {
        Pruefpunkt.Leitziel => "Leitziel",
        Pruefpunkt.Nahziele => "Nahziele",
        Pruefpunkt.NahzielMassnahme => "Maßnahmen",
        Pruefpunkt.ZielUeberpruefung => "Überprüfung",
        Pruefpunkt.MassnahmenBestaetigt => "Bestätigt",
        Pruefpunkt.Lebensbereiche => "Lebensbereiche",
        Pruefpunkt.Reflexion => "Reflexion",
        Pruefpunkt.Heb => "HEB",
        Pruefpunkt.Hilfeplan => "Hilfeplan",
        _ => "Sicherheit",
    };

    // Ausgeschaltete Prüfpunkte in den Einstellungen: Namen mit Komma getrennt; Unbekanntes wird übergangen.
    public static HashSet<Pruefpunkt> AusLesen(string text) =>
        [.. text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => Enum.TryParse<Pruefpunkt>(t, true, out var p) ? (Pruefpunkt?)p : null).OfType<Pruefpunkt>()];

    public static string AusText(IEnumerable<Pruefpunkt> aus) => string.Join(",", aus.Distinct().Order());

    public static QmEinstellung Einstellung(EinstellungsWerte w) =>
        new(AusLesen(w.QmAus), Math.Clamp(w.QmTage, 1, 90), w.ZieleLeitziele, w.ZieleNahziele, w.KeinBerichtWochen);

    public static List<PruefErgebnis> Pruefen(PruefDaten d, DateOnly heute, QmEinstellung e)
    {
        var liste = new List<PruefErgebnis>();
        void Punkt(Pruefpunkt p, string? mangel, string ok = Ok) { if (!e.Aus.Contains(p)) liste.Add(new(p, mangel is null, mangel ?? ok)); }

        var zustand = ZielRegeln.Zustand(d.Ziele, d.Verzicht);
        var aktive = ZielRegeln.Aktive(d.Ziele);
        bool keine = zustand == ZieleZustand.Keine;
        string okZiel = keine ? "kein Ziel benannt" : Ok;
        int leit = aktive.Count(z => z.Art == ZielArt.Leitziel), nah = aktive.Count(z => z.Art == ZielArt.Nahziel);
        Punkt(Pruefpunkt.Leitziel, keine ? null : zustand == ZieleZustand.NichtErfasst ? "Ziele nicht erfasst" : leit < e.Leitziele ? "fehlt" : null, okZiel);
        Punkt(Pruefpunkt.Nahziele, keine ? null : nah < e.Nahziele ? $"{nah} von {e.Nahziele}" : null, okZiel);
        var laufend = d.Massnahmen.Where(m => m.Laeuft(heute)).ToList();
        var mitMassnahme = laufend.Select(m => m.Am(heute)?.ZielId).OfType<long>().ToHashSet();
        int ohne = aktive.Count(z => z.Art == ZielArt.Nahziel && !mitMassnahme.Contains(z.Id));
        Punkt(Pruefpunkt.NahzielMassnahme, ohne == 0 ? null : ohne == 1 ? "1 Nahziel ohne Maßnahme" : $"{ohne} Nahziele ohne Maßnahme");
        var faellig = aktive.Where(z => z.Aktuell.UeberpruefenBis <= heute).Select(z => z.Aktuell.UeberpruefenBis).ToList();
        Punkt(Pruefpunkt.ZielUeberpruefung, faellig.Count == 0 ? null : $"fällig seit {Datum.Text(faellig.Min())}");

        // Unbestätigt: in den letzten n Tagen fällig gewesen (ohne heute), aber in dieser Zeit nie bestätigt.
        var von = heute.AddDays(-e.TageUnbestaetigt);
        int unbestaetigt = laufend.Count(m => m.Am(heute) is MassnahmeFassung f && f.Rhythmus != MassnahmeRhythmus.BeiBedarf && f.GiltAb <= von
            && Enumerable.Range(1, e.TageUnbestaetigt).Any(i => MassnahmenRegeln.AmTag(m, f, heute.AddDays(-i)))
            && !d.Bestaetigungen.Any(b => b.MassnahmeId == m.Id && b.Tag >= von));
        Punkt(Pruefpunkt.MassnahmenBestaetigt, unbestaetigt == 0 ? null
            : $"{unbestaetigt} {(unbestaetigt == 1 ? "Maßnahme" : "Maßnahmen")} seit {e.TageUnbestaetigt} Tagen nicht bestätigt");

        var ohneBericht = MassnahmenRegeln.Lebensbereiche(d.Berichte, heute.ToDateTime(new TimeOnly(12, 0)), e.KeinBerichtWochen)
            .Where(l => l.Marke is not null).Select(l => l.Nr).ToList();
        Punkt(Pruefpunkt.Lebensbereiche, ohneBericht.Count == 0 ? null : $"seit {e.KeinBerichtWochen} Wochen kein Bericht: LB {string.Join(", ", ohneBericht)}");

        var monat = ReflexionRegeln.Vorschlag(heute);
        bool spaeterAufgenommen = d.Bewohner.Aufnahmedatum > ReflexionRegeln.MonatBis(monat);
        Punkt(Pruefpunkt.Reflexion, spaeterAufgenommen || d.LetzteReflexion.Fertig ? null
            : $"{ReflexionRegeln.MonatText(monat)}: {ReflexionRegeln.StatusText(d.LetzteReflexion)}");
        Punkt(Pruefpunkt.Heb, d.HebFaellig);
        Punkt(Pruefpunkt.Hilfeplan, d.Hilfeplan is { FaelligBis: DateOnly hpg } hp && !HilfeplanRegeln.TerminSteht(hp, heute) && hpg < heute
            ? $"überfällig seit {Datum.Text(hpg)}" : null);

        var fehlt = new List<string>();
        if (d.Bewohner.Allergie == AllergieZustand.NichtErfasst) fehlt.Add("Allergien");
        if (!d.Angaben.ContainsKey(Angabe.Aerzte)) fehlt.Add("Ärzte");
        if (!d.Angaben.ContainsKey(Angabe.Schweigepflicht)) fehlt.Add("Schweigepflicht");
        Punkt(Pruefpunkt.Sicherheit, fehlt.Count == 0 ? null : $"nicht erfasst: {string.Join(", ", fehlt)}");
        return liste;
    }

    // Kennzahlen je Wohnbereich – unabhängig davon, welche Prüfpunkte ausgeschaltet sind.
    public static List<QmKennzahl> Kennzahlen(IEnumerable<PruefDaten> daten, DateOnly heute, QmEinstellung e)
    {
        var alle = e with { Aus = new HashSet<Pruefpunkt>() };
        return [.. daten.GroupBy(d => d.Bewohner.Wohnbereich).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g =>
        {
            int vollstaendig = 0, mitMassnahme = 0, nahziele = 0, ueberpruefung = 0, reflexion = 0, heb = 0;
            foreach (var d in g)
            {
                var r = Pruefen(d, heute, alle).ToDictionary(x => x.Punkt, x => x.Ok);
                if (r[Pruefpunkt.Leitziel] && r[Pruefpunkt.Nahziele]) vollstaendig++;
                if (!r[Pruefpunkt.ZielUeberpruefung]) ueberpruefung++;
                if (!r[Pruefpunkt.Reflexion]) reflexion++;
                if (!r[Pruefpunkt.Heb]) heb++;
                var aktive = ZielRegeln.Aktive(d.Ziele).Where(z => z.Art == ZielArt.Nahziel).ToList();
                var mit = d.Massnahmen.Where(m => m.Laeuft(heute)).Select(m => m.Am(heute)?.ZielId).OfType<long>().ToHashSet();
                nahziele += aktive.Count;
                mitMassnahme += aktive.Count(z => mit.Contains(z.Id));
            }
            return new QmKennzahl(g.Key, g.Count(), vollstaendig, mitMassnahme, nahziele, ueberpruefung, reflexion, heb);
        })];
    }

    public static string KennzahlText(QmKennzahl k) =>
        $"{k.Bereich}: Ziele vollständig {k.ZieleVollstaendig} von {k.Bewohner} · Nahziele mit Maßnahme {k.NahzieleMitMassnahme} von {k.Nahziele} · " +
        $"Überprüfung fällig {k.UeberpruefungFaellig} · Reflexion fällig {k.ReflexionFaellig} · HEB fällig {k.HebFaellig}";

    // Text der Aufgabe an den Wohnbereich.
    public static string HinweisText(Bewohner b, IEnumerable<string> maengel) =>
        $"Qualität: Unterlagen von {b.Name} (Zi. {b.Zimmer}) bitte ergänzen – {string.Join("; ", maengel)}";

    public static string VermerkText(Pruefvermerk? v) => v is null ? "noch nicht geprüft" : $"geprüft am {Datum.Text(DateOnly.FromDateTime(v.Zeit))} von {v.Kuerzel}";

    public static string HinweisStand(QmHinweis? h) => h is null ? "" :
        $"Hinweis gesendet am {Datum.Text(DateOnly.FromDateTime(h.Gesendet))}" + (h.Erledigt is DateTime e ? $", erledigt am {Datum.Text(DateOnly.FromDateTime(e))}" : ", offen");

    // Nachweis „Ziele und Maßnahmen“ (für die Heimaufsicht): je Ziel eine Zeile mit seinen Maßnahmen und der letzten Überprüfung.
    public static readonly string[] NachweisSpalten = ["Zimmer", "Bewohner", "Ziel", "Lebensbereich", "Vereinbart", "Überprüfen bis", "Zuletzt überprüft", "Maßnahmen"];

    public static List<string[]> Nachweis(IEnumerable<PruefDaten> daten, DateOnly heute)
    {
        var zeilen = new List<string[]>();
        foreach (var d in daten)
        {
            var aktive = ZielRegeln.Aktive(d.Ziele);
            if (aktive.Count == 0)
            {
                string zustand = ZielRegeln.Zustand(d.Ziele, d.Verzicht) == ZieleZustand.Keine ? ZielRegeln.Leiste(d.Ziele, d.Verzicht) : "Ziele: nicht erfasst";
                zeilen.Add([d.Bewohner.Zimmer, d.Bewohner.Name, zustand, "", "", "", "", ""]);
                continue;
            }
            var massnahmen = d.Massnahmen.Where(m => m.Laeuft(heute)).ToLookup(m => m.Am(heute)?.ZielId);
            foreach (var z in aktive)
            {
                var f = z.Aktuell;
                zeilen.Add([d.Bewohner.Zimmer, d.Bewohner.Name, ZielRegeln.Text(z), BerichtRegeln.BereichText(f.Lebensbereich), Datum.Text(f.VereinbartAm),
                    Datum.Text(f.UeberpruefenBis), Datum.Text(DateOnly.FromDateTime(f.Erfasst)),
                    massnahmen[z.Id].Any() ? string.Join("; ", massnahmen[z.Id].Select(m => m.Am(heute)!.Titel)) : "keine Maßnahme"]);
            }
        }
        return zeilen;
    }
}
