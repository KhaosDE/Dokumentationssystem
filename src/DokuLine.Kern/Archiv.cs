namespace DokuLine.Kern;

// Archiv für ehemalige Bewohner (Bauplan 46): Entlassen verschiebt die Akte ins Archiv (nicht mehr in Listen, Zählern,
// Belegung); dort bleibt sie lesbar bis zum Ende der Aufbewahrungsfrist. Die Frist hat keine Vorgabe – sie muss mit Träger
// und Datenschutz geklärt und eingetragen werden. Gelöscht wird nie von selbst, nur von zwei Personen (Vier-Augen) mit
// Löschprotokoll ohne Inhalte.
public enum EntlassGrund { Auszug, Verlegung, Verstorben, Sonstiges }

public sealed record ArchivEintrag(long Id, long BewohnerId, DateOnly Aufgenommen, DateOnly Entlassen, EntlassGrund Grund, string Bemerkung,
    string Wohnbereich, string Zimmer, string Kuerzel, DateTime Erfasst, DateOnly? WiederAufgenommen)
{
    public bool Aktiv => WiederAufgenommen is null;
}

public sealed record ArchivLoeschfreigabe(long BewohnerId, string Kuerzel, DateTime Zeit);

public sealed record ArchivZeile(Bewohner Bewohner, ArchivEintrag Eintrag, IReadOnlyList<ArchivEintrag> Aufenthalte, DateOnly? AufbewahrenBis,
    ArchivLoeschfreigabe? Freigabe)
{
    public bool Abgelaufen(DateOnly heute) => AufbewahrenBis is DateOnly bis && bis < heute;
}

// Ohne Inhalte und ohne Namen: Nummer, Zeitraum, wer, wann.
public sealed record Loeschprotokoll(long Id, long BewohnerNr, string Zeitraum, string Freigabe, string Geloescht, DateTime Zeit);

// Was beim Entlassen endet (zum Bestätigen).
public sealed record EntlassFolge(string Art, long Id, string Text);

public static class ArchivRegeln
{
    public const string FristFehlt = "Aufbewahrungsfrist nicht festgelegt – bitte in den Einstellungen eintragen (mit Träger und Datenschutz klären).";
    public const string FristOffen = "Frist nicht festgelegt";
    public const int FristHoechstens = 100;
    public const string EntlassenText = "Entlassung";

    public static readonly EntlassGrund[] Gruende = Enum.GetValues<EntlassGrund>();

    public static string GrundText(EntlassGrund g) => g switch
    {
        EntlassGrund.Auszug => "Auszug",
        EntlassGrund.Verlegung => "Verlegung",
        EntlassGrund.Verstorben => "verstorben",
        _ => "Sonstiges",
    };

    public static string Schluessel(EntlassGrund g) => g.ToString().ToLowerInvariant();

    public static EntlassGrund GrundAus(string text) => Enum.TryParse<EntlassGrund>(text, true, out var g) ? g : EntlassGrund.Sonstiges;

    // Frist in Jahren: leer = nicht festgelegt (null); sonst 1 bis 100.
    public static int? FristLesen(string text, List<string> fehler)
    {
        if (text.Trim().Length == 0) return null;
        if (int.TryParse(text.Trim(), out int jahre) && jahre is >= 1 and <= FristHoechstens) return jahre;
        fehler.Add($"Aufbewahrungsfrist: bitte eine ganze Zahl von 1 bis {FristHoechstens} Jahren oder leer lassen.");
        return null;
    }

    public static DateOnly? AufbewahrenBis(DateOnly entlassen, int? jahre) => jahre is int j ? entlassen.AddYears(j) : null;

    public static string AufbewahrenText(DateOnly? bis) => bis is DateOnly d ? Datum.Text(d) : FristOffen;

    public static List<string> EntlassenPruefen(Bewohner b, DateOnly datum, DateOnly heute)
    {
        var fehler = new List<string>();
        if (datum > heute) fehler.Add("Das Entlassdatum liegt in der Zukunft – bitte am Tag selbst oder danach entlassen.");
        if (datum < b.Aufnahmedatum) fehler.Add($"Das Entlassdatum liegt vor der Aufnahme ({Datum.Text(b.Aufnahmedatum)}).");
        return fehler;
    }

    public static string ZeitraumText(ArchivEintrag e) => $"{Datum.Text(e.Aufgenommen)} – {Datum.Text(e.Entlassen)}";

    // Suche in Name, Zimmer, Bereich und Grund.
    public static List<ArchivZeile> Suchen(IEnumerable<ArchivZeile> zeilen, string suche)
    {
        var s = suche.Trim();
        return [.. zeilen.Where(z => s.Length == 0 || z.Bewohner.Name.Contains(s, StringComparison.CurrentCultureIgnoreCase)
            || z.Eintrag.Zimmer.Contains(s, StringComparison.OrdinalIgnoreCase) || z.Eintrag.Wohnbereich.Contains(s, StringComparison.OrdinalIgnoreCase)
            || GrundText(z.Eintrag.Grund).Contains(s, StringComparison.CurrentCultureIgnoreCase))];
    }

    // Die Akte im Archiv, nur lesend: je Teil ein Text (auch zum Drucken). Zählt und stellt zusammen, formuliert nichts.
    public static readonly string[] AkteTeilNamen = ["Stammdaten", "Berichte", "Ziele und Maßnahmen", "Medikamentenplan", "Reflexion und HEB"];

    public static List<(string Titel, string Text)> AkteTeile(Bewohner b, IReadOnlyList<ArchivEintrag> aufenthalte, IEnumerable<Bericht> berichte,
        IReadOnlyCollection<Ziel> ziele, IEnumerable<Massnahme> massnahmen, IEnumerable<Verordnung> verordnungen, IEnumerable<Reflexion> reflexionen,
        IEnumerable<HebBogen> heb)
    {
        var n = Environment.NewLine;
        var stamm = new List<string>
        {
            $"{b.Name}, geboren {Datum.Text(b.Geburtsdatum)}", $"Zuletzt: {b.Wohnbereich}, Zimmer {b.Zimmer}", $"Allergien: {BewohnerRegeln.AllergieText(b)}", "",
            "Aufenthalte:",
        };
        stamm.AddRange(aufenthalte.Select(a => $"  {ZeitraumText(a)} · {a.Wohnbereich} · Zi. {a.Zimmer} · {GrundText(a.Grund)}" +
            (a.Bemerkung.Length > 0 ? $" ({a.Bemerkung})" : "") + (a.WiederAufgenommen is DateOnly w ? $" · wieder aufgenommen {Datum.Text(w)}" : "")));
        var liste = berichte.OrderBy(x => x.Zeitpunkt).ToList();
        string berichtText = liste.Count == 0 ? "Keine Berichte." : string.Join(n, liste.Select(x =>
            $"{x.Zeitpunkt:dd.MM.yyyy HH:mm} {x.Verfasser} [{x.Art}]: {x.Text.ReplaceLineEndings(" ")}" +
            string.Concat(x.Nachtraege.Select(t => $"{n}    Nachtrag {t.Erfasst:dd.MM.yyyy} {t.Verfasser}: {t.Text.ReplaceLineEndings(" ")}"))));
        var zielZeilen = ziele.OrderBy(z => z.Art).ThenBy(z => z.Id).Select(z =>
            $"{ZielRegeln.Text(z)} ({BerichtRegeln.BereichText(z.Aktuell.Lebensbereich)}, vereinbart {Datum.Text(z.Aktuell.VereinbartAm)}, {ZielRegeln.StandText(z.Aktuell.Stand)})").ToList();
        var massnahmeZeilen = massnahmen.Select(m => m.Fassungen[^1]).Select(f => $"Maßnahme: {f.Titel} ({BerichtRegeln.BereichText(f.Lebensbereich)}, ab {Datum.Text(f.GiltAb)})").ToList();
        var verordnet = verordnungen.Select(v => $"{v.Medikament} {v.Staerke} {(v.Bedarf ? "bei Bedarf" : v.Dosis.Text)} · ab {Datum.Text(v.Beginn)}").ToList();
        var refl = reflexionen.Where(r => r.Aktuell is not null).Select(r => $"Reflexion {ReflexionRegeln.MonatText(r.Monat)}: {ReflexionRegeln.StatusText(r)}")
            .Concat(heb.Select(h => $"{HebRegeln.ArtKurz(h.Art)} {HebRegeln.ZeitraumText(h)}: {(h.Fertig ? "fertig" : "Entwurf")}")).ToList();
        return
        [
            ("Stammdaten", string.Join(n, stamm)),
            ("Berichte", berichtText),
            ("Ziele und Maßnahmen", zielZeilen.Count + massnahmeZeilen.Count == 0 ? "Keine Ziele und Maßnahmen." : string.Join(n, zielZeilen.Concat(massnahmeZeilen))),
            ("Medikamentenplan", verordnet.Count == 0 ? "Keine Verordnungen." : string.Join(n, verordnet)),
            ("Reflexion und HEB", refl.Count == 0 ? "Keine Reflexionen und HEB-Bögen." : string.Join(n, refl)),
        ];
    }

    // Löschen zu zweit: wer freigibt, darf nicht selbst löschen; nur nach Ablauf der Frist.
    public static string? LoeschenPruefen(ArchivZeile z, DateOnly heute, string kuerzel, bool endgueltig)
    {
        if (z.AufbewahrenBis is null) return FristFehlt;
        if (!z.Abgelaufen(heute)) return $"Die Aufbewahrung läuft noch bis {Datum.Text(z.AufbewahrenBis.Value)}.";
        if (!endgueltig) return z.Freigabe is null ? null : $"Schon freigegeben von {z.Freigabe.Kuerzel}.";
        if (z.Freigabe is null) return "Zuerst muss eine zweite Person das Löschen freigeben.";
        if (z.Freigabe.Kuerzel.Equals(kuerzel.Trim(), StringComparison.OrdinalIgnoreCase)) return "Löschen nur zu zweit: Wer freigegeben hat, löscht nicht selbst.";
        return null;
    }
}
