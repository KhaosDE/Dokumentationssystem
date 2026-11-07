using System.Globalization;
using System.Text.RegularExpressions;

namespace Dokusystem.Kern;

// Medikation je Bewohner. Die Grundverordnung bleibt immer erhalten; Pausen, Stufenpläne
// (ansetzen, umstellen, absetzen in Etappen) und Absetzen liegen als eigene Einträge darüber.
// „Was gilt heute“ wird daraus berechnet. Ausdrücklich nicht: Dosisvorschläge, Wechselwirkungen,
// Stellen und Ausgabe, Bestand. Demo — nicht zum Stellen verwenden.

// Dosis je Einnahmezeit als Text, wie auf einem Medikamentenplan („1“, „½“, „0,5“, „2“).
public sealed record Dosis(string Morgens, string Mittags, string Abends, string Nachts)
{
    public static readonly Dosis Null = new("0", "0", "0", "0");

    public IEnumerable<string> Werte => [Morgens, Mittags, Abends, Nachts];
    public bool IstNull => Werte.All(w => MedikationRegeln.Menge(w) == 0m);
    public string Text => string.Join("–", Werte);
}

// Wer hat es angeordnet (ausgedacht in der Demo) und wann.
public sealed record Anordnung(string Arzt, DateOnly Am);

public enum AenderungArt { Pause, PauseEnde, Stufenplan, Absetzen }

public sealed record Stufe(DateOnly Ab, Dosis Dosis);

public sealed record VerordnungsAenderung
{
    public long Id { get; init; }
    public AenderungArt Art { get; init; }
    public DateOnly Von { get; init; }          // Pause, Pause-Ende, Absetzen: Datum; Stufenplan: erste Stufe
    public DateOnly? Bis { get; init; }         // Pause: bis (einschließlich); null = bis auf Weiteres
    public IReadOnlyList<Stufe> Stufen { get; init; } = [];
    public Anordnung Anordnung { get; init; } = new("", default);
    public string Hinweis { get; init; } = "";
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
}

public sealed record Verordnung
{
    public long Id { get; init; }
    public long BewohnerId { get; init; }
    public long MedikamentId { get; init; }
    public string Medikament { get; init; } = "";   // Anzeige aus der Datenbank (Wirkstoff, Form)
    public string Staerke { get; init; } = "";
    public string Einheit { get; init; } = "Stück";
    public Dosis Dosis { get; init; } = Dosis.Null;
    public bool Bedarf { get; init; }
    public string BedarfAnlass { get; init; } = "";
    public string BedarfHoechstens { get; init; } = "";   // z. B. „2 in 24 Stunden“
    public DateOnly Beginn { get; init; }
    public Anordnung Anordnung { get; init; } = new("", default);
    public string Hinweis { get; init; } = "";
    public DateTime Erstellt { get; init; }
    public string Kuerzel { get; init; } = "";
    public IReadOnlyList<VerordnungsAenderung> Aenderungen { get; init; } = [];   // nach Erstellung
}

public enum Zustand { NochNicht, Gilt, Pausiert, Stufenplan, Abgesetzt }

// Was an einem Tag für eine Verordnung gilt.
public sealed record Tagesstand(Verordnung Verordnung, Zustand Zustand, Dosis Dosis, string Text, string? Naechstes)
{
    public bool Sichtbar => Zustand is not (Zustand.Abgesetzt or Zustand.NochNicht);
}

public sealed class VerordnungEingabe
{
    public long BewohnerId { get; set; }
    public long MedikamentId { get; set; }
    public string Staerke { get; set; } = "";
    public string Einheit { get; set; } = "Stück";
    public string Morgens { get; set; } = "0";
    public string Mittags { get; set; } = "0";
    public string Abends { get; set; } = "0";
    public string Nachts { get; set; } = "0";
    public bool Bedarf { get; set; }
    public string BedarfAnlass { get; set; } = "";
    public string BedarfHoechstens { get; set; } = "";
    public string Beginn { get; set; } = "";
    public string Arzt { get; set; } = "";
    public string AngeordnetAm { get; set; } = "";
    public string Hinweis { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

public static partial class MedikationRegeln
{
    public const string DruckHinweis = "Demo — nicht zum Stellen verwenden";
    public static readonly string[] Zeiten = ["morgens", "mittags", "abends", "nachts"];

    [GeneratedRegex(@"^\s*(\d+([,.]\d+)?|½|¼|¾|\d+\s*½)\s*$")]
    private static partial Regex MengeMuster();

    // „1“, „0,5“, „½“, „1 ½“ → Zahl; ungültig → null.
    public static decimal? Menge(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return 0m;
        if (!MengeMuster().IsMatch(text)) return null;
        decimal ganz = 0m;
        if (text.EndsWith('½') || text.EndsWith('¼') || text.EndsWith('¾'))
        {
            decimal bruch = text[^1] switch { '½' => 0.5m, '¼' => 0.25m, _ => 0.75m };
            string vorne = text[..^1].Trim();
            if (vorne.Length > 0) ganz = decimal.Parse(vorne, CultureInfo.InvariantCulture);
            return ganz + bruch;
        }
        return decimal.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    public static Dosis DosisLesen(string morgens, string mittags, string abends, string nachts, List<string> fehler, string wo = "")
    {
        string[] werte = [morgens, mittags, abends, nachts];
        for (int i = 0; i < 4; i++)
            if (Menge(werte[i]) is null) fehler.Add($"{wo}Menge {Zeiten[i]}: bitte als Zahl, z. B. 1, 0,5 oder ½.");
        return new Dosis(Sauber(morgens), Sauber(mittags), Sauber(abends), Sauber(nachts));
    }

    private static string Sauber(string s) => string.IsNullOrWhiteSpace(s) ? "0" : s.Trim();

    private static DateOnly? DatumOderFehler(string text, string feld, List<string> fehler)
    {
        try { return Datum.Lesen(text, feld); }
        catch (FormatException e) { fehler.Add(e.Message); return null; }
    }

    private static Anordnung AnordnungLesen(string arzt, string am, List<string> fehler)
    {
        if (string.IsNullOrWhiteSpace(arzt)) fehler.Add("Anordnung: Wer hat es angeordnet? (Arzt)");
        var datum = DatumOderFehler(am, "Angeordnet am", fehler);
        return new Anordnung(arzt.Trim(), datum ?? default);
    }

    public static Verordnung AusEingabe(VerordnungEingabe e, string medikament, DateTime jetzt)
    {
        var fehler = new List<string>();
        if (e.MedikamentId == 0) fehler.Add("Bitte ein Medikament aus der Liste wählen.");
        if (string.IsNullOrWhiteSpace(e.Kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        var dosis = DosisLesen(e.Morgens, e.Mittags, e.Abends, e.Nachts, fehler);
        if (!e.Bedarf && fehler.Count == 0 && dosis.IstNull) fehler.Add("Dosis: Mindestens eine Einnahmezeit braucht eine Menge.");
        if (e.Bedarf && string.IsNullOrWhiteSpace(e.BedarfAnlass)) fehler.Add("Bedarf: Bei welchem Anlass?");
        if (e.Bedarf && Menge(e.Morgens) is null or 0m) fehler.Add("Bedarf: Einzeldosis fehlt.");
        var beginn = DatumOderFehler(e.Beginn, "Beginn", fehler);
        var anordnung = AnordnungLesen(e.Arzt, e.AngeordnetAm, fehler);
        if (anordnung.Am != default && anordnung.Am > DateOnly.FromDateTime(jetzt)) fehler.Add("Angeordnet am: Das Datum liegt in der Zukunft.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new Verordnung
        {
            BewohnerId = e.BewohnerId,
            MedikamentId = e.MedikamentId,
            Medikament = medikament,
            Staerke = e.Staerke.Trim(),
            Einheit = string.IsNullOrWhiteSpace(e.Einheit) ? "Stück" : e.Einheit.Trim(),
            Dosis = e.Bedarf ? new Dosis(Sauber(e.Morgens), "0", "0", "0") : dosis,
            Bedarf = e.Bedarf,
            BedarfAnlass = e.BedarfAnlass.Trim(),
            BedarfHoechstens = e.BedarfHoechstens.Trim(),
            Beginn = beginn!.Value,
            Anordnung = anordnung,
            Hinweis = e.Hinweis.Trim(),
            Erstellt = jetzt,
            Kuerzel = e.Kuerzel.Trim(),
        };
    }

    // ----- Änderungen: Pause, Pause beenden, Stufenplan, Absetzen -----

    public static VerordnungsAenderung Pause(string von, string bis, string arzt, string am, string hinweis, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        var v = DatumOderFehler(von, "Pause von", fehler);
        DateOnly? b = string.IsNullOrWhiteSpace(bis) ? null : DatumOderFehler(bis, "Pause bis", fehler);
        if (v is DateOnly vv && b is DateOnly bb && bb < vv) fehler.Add("Pause: „bis“ liegt vor „von“.");
        return Aenderung(AenderungArt.Pause, v, b, [], arzt, am, hinweis, kuerzel, jetzt, fehler);
    }

    public static VerordnungsAenderung PauseEnde(string am, string arzt, string angeordnet, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        var v = DatumOderFehler(am, "Pause endet am", fehler);
        return Aenderung(AenderungArt.PauseEnde, v, null, [], arzt, angeordnet, "", kuerzel, jetzt, fehler);
    }

    public static VerordnungsAenderung Absetzen(string ab, string arzt, string am, string hinweis, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        var v = DatumOderFehler(ab, "Abgesetzt ab", fehler);
        return Aenderung(AenderungArt.Absetzen, v, null, [], arzt, am, hinweis, kuerzel, jetzt, fehler);
    }

    // Stufen: (ab, morgens, mittags, abends, nachts). Die letzte Stufe gilt danach weiter –
    // als neue Grundverordnung, oder, wenn sie 0 ist, als „abgesetzt“.
    public static VerordnungsAenderung Stufenplan(IReadOnlyList<(string Ab, string M, string Mi, string A, string N)> stufen,
        string arzt, string am, string hinweis, string kuerzel, DateTime jetzt)
    {
        var fehler = new List<string>();
        var liste = new List<Stufe>();
        if (stufen.Count == 0) fehler.Add("Stufenplan: mindestens eine Stufe.");
        for (int i = 0; i < stufen.Count; i++)
        {
            var s = stufen[i];
            var ab = DatumOderFehler(s.Ab, $"Stufe {i + 1} ab", fehler);
            var d = DosisLesen(s.M, s.Mi, s.A, s.N, fehler, $"Stufe {i + 1}: ");
            if (ab is DateOnly a) liste.Add(new Stufe(a, d));
        }
        for (int i = 1; i < liste.Count; i++)
            if (liste[i].Ab <= liste[i - 1].Ab) fehler.Add($"Stufe {i + 1} muss nach Stufe {i} beginnen.");
        return Aenderung(AenderungArt.Stufenplan, liste.Count > 0 ? liste[0].Ab : null, null, liste, arzt, am, hinweis, kuerzel, jetzt, fehler);
    }

    private static VerordnungsAenderung Aenderung(AenderungArt art, DateOnly? von, DateOnly? bis, List<Stufe> stufen,
        string arzt, string am, string hinweis, string kuerzel, DateTime jetzt, List<string> fehler)
    {
        var anordnung = AnordnungLesen(arzt, am, fehler);
        if (string.IsNullOrWhiteSpace(kuerzel)) fehler.Add("Kürzel fehlt (oben rechts „Angemeldet als …“).");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
        return new VerordnungsAenderung
        {
            Art = art, Von = von!.Value, Bis = bis, Stufen = stufen, Anordnung = anordnung,
            Hinweis = hinweis.Trim(), Erstellt = jetzt, Kuerzel = kuerzel.Trim(),
        };
    }

    // Prüft eine Änderung gegen die Verordnung (z. B. nichts vor dem Beginn, nichts nach dem Absetzen).
    public static void Pruefen(Verordnung v, VerordnungsAenderung a)
    {
        var fehler = new List<string>();
        if (a.Von < v.Beginn) fehler.Add($"Das Datum liegt vor dem Beginn der Verordnung ({Datum.Text(v.Beginn)}).");
        if (Abgesetzt(v) is DateOnly weg && a.Von >= weg) fehler.Add($"Die Verordnung ist ab {Datum.Text(weg)} abgesetzt.");
        if (v.Bedarf && a.Art == AenderungArt.Stufenplan) fehler.Add("Für Bedarfsmedikation gibt es keinen Stufenplan.");
        if (a.Art == AenderungArt.PauseEnde && !v.Aenderungen.Any(x => x.Art == AenderungArt.Pause && x.Von <= a.Von && (x.Bis is null || x.Bis >= a.Von)))
            fehler.Add("An diesem Tag läuft keine Pause.");
        if (fehler.Count > 0) throw new Eingabefehler(fehler);
    }

    // Ab wann ist die Verordnung abgesetzt (direkt oder Stufenplan auf 0)? null = nicht abgesetzt.
    public static DateOnly? Abgesetzt(Verordnung v)
    {
        DateOnly? weg = v.Aenderungen.Where(a => a.Art == AenderungArt.Absetzen).Select(a => (DateOnly?)a.Von).Min();
        foreach (var plan in v.Aenderungen.Where(a => a.Art == AenderungArt.Stufenplan))
            if (plan.Stufen.Count > 0 && plan.Stufen[^1].Dosis.IstNull && (weg is null || plan.Stufen[^1].Ab < weg))
                weg = plan.Stufen[^1].Ab;
        return weg;
    }

    // Pause an einem Tag: die zuletzt angelegte, die den Tag abdeckt; ein „Pause endet“ dazwischen beendet sie.
    private static VerordnungsAenderung? PauseAm(Verordnung v, DateOnly tag)
    {
        foreach (var p in v.Aenderungen.Where(a => a.Art == AenderungArt.Pause).Reverse())
        {
            if (p.Von > tag || (p.Bis is DateOnly b && b < tag)) continue;
            bool beendet = v.Aenderungen.Any(e => e.Art == AenderungArt.PauseEnde && e.Erstellt > p.Erstellt && e.Von <= tag && e.Von >= p.Von);
            if (!beendet) return p;
        }
        return null;
    }

    // Was gilt an einem Tag?
    public static Tagesstand Stand(Verordnung v, DateOnly tag)
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        string Kurz(DateOnly d) => d.ToString("ddd dd.MM.", de);
        if (tag < v.Beginn) return new(v, Zustand.NochNicht, v.Dosis, $"beginnt am {Kurz(v.Beginn)}", null);
        if (Abgesetzt(v) is DateOnly weg && tag >= weg) return new(v, Zustand.Abgesetzt, Dosis.Null, $"abgesetzt am {Kurz(weg)}", null);

        // Grundverordnung, ggf. ersetzt durch die letzte Stufe eines abgeschlossenen Stufenplans.
        var dosis = v.Dosis;
        string text = v.Bedarf ? "bei Bedarf" : "Grundverordnung";
        string? naechstes = null;
        var plaene = v.Aenderungen.Where(a => a.Art == AenderungArt.Stufenplan && a.Stufen.Count > 0 && a.Stufen[0].Ab <= tag).ToList();
        if (plaene.Count > 0)
        {
            var plan = plaene[^1];   // der zuletzt angelegte, der schon begonnen hat
            int nr = plan.Stufen.Count(s => s.Ab <= tag);
            dosis = plan.Stufen[nr - 1].Dosis;
            if (nr < plan.Stufen.Count)
            {
                text = $"Stufe {nr} von {plan.Stufen.Count}";
                naechstes = $"nächste Stufe ab {Kurz(plan.Stufen[nr].Ab)}: {plan.Stufen[nr].Dosis.Text}";
            }
            else text = $"neue Grundverordnung seit {Kurz(plan.Stufen[^1].Ab)}";
        }
        if (PauseAm(v, tag) is VerordnungsAenderung pause)
            return new(v, Zustand.Pausiert, dosis, pause.Bis is DateOnly b ? $"pausiert bis {Kurz(b)}" : "pausiert bis auf Weiteres", null);
        var zustand = text.StartsWith("Stufe ") ? Zustand.Stufenplan : Zustand.Gilt;
        return new(v, zustand, dosis, text, naechstes);
    }

    // Verlauf in Worten (älteste zuerst): Grundverordnung, dann jede Änderung mit Datum und Anordnung.
    public static List<string> Verlauf(Verordnung v)
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        string A(Anordnung a) => $"angeordnet von {a.Arzt} am {Datum.Text(a.Am)}";
        var liste = new List<string>
        {
            $"{Datum.Text(v.Beginn)}: {(v.Bedarf ? $"Bedarf ({v.BedarfAnlass}), Einzeldosis {v.Dosis.Morgens}" : $"Grundverordnung {v.Dosis.Text}")} · {A(v.Anordnung)} · eingetragen von {v.Kuerzel}",
        };
        foreach (var a in v.Aenderungen)
        {
            string was = a.Art switch
            {
                AenderungArt.Pause => a.Bis is DateOnly b ? $"Pause bis {Datum.Text(b)}" : "Pause bis auf Weiteres",
                AenderungArt.PauseEnde => "Pause beendet",
                AenderungArt.Absetzen => "abgesetzt",
                _ => "Stufenplan: " + string.Join("; ", a.Stufen.Select(s => $"ab {s.Ab.ToString("dd.MM.", de)} {s.Dosis.Text}")) +
                     (a.Stufen[^1].Dosis.IstNull ? " (absetzen)" : " (letzte Stufe wird neue Grundverordnung)"),
            };
            liste.Add($"{Datum.Text(a.Von)}: {was} · {A(a.Anordnung)} · eingetragen von {a.Kuerzel}" + (a.Hinweis.Length > 0 ? $" · {a.Hinweis}" : ""));
        }
        return liste;
    }

    public static string AenderungKuerzel(AenderungArt a) => a switch
    {
        AenderungArt.Pause => "pause",
        AenderungArt.PauseEnde => "pause_ende",
        AenderungArt.Stufenplan => "stufenplan",
        _ => "absetzen",
    };

    public static AenderungArt AenderungAus(string s) => s switch
    {
        "pause" => AenderungArt.Pause,
        "pause_ende" => AenderungArt.PauseEnde,
        "stufenplan" => AenderungArt.Stufenplan,
        _ => AenderungArt.Absetzen,
    };
}
