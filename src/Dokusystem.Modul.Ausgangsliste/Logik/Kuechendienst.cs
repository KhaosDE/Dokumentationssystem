namespace Dokusystem.Modul.Ausgangsliste;

// Ein Tag im Küchendienstplan.
public class KuechendienstTag
{
    public DateTime Datum { get; set; }
    public string Zimmer { get; set; } = "";   // geplant, z. B. „105“ oder „109/110“
    public string Tausch { get; set; } = "";   // Vertretung für genau diesen Tag

    public string Wirksam => Tausch != "" ? Tausch : Zimmer;
}

// Untermodul „Küchendienst“ (übernommen aus der Ausgangsliste): reihum nach Zimmern, Zimmergruppen
// machen den Dienst gemeinsam. Wer abwesend ist oder erst seit weniger als 4 Tagen da ist
// (Karenz), wird übersprungen. Vergangene Tage und heute bleiben fest; geplant wird bis zum Ende
// des Folgemonats. Ein Tausch gilt für genau einen Tag.
// Unterschied zur Ausgangsliste: Die Zimmer kommen aus der Bewohnerliste des Kerns; die Regel
// für Einzelzimmer entfällt, weil es keine freien Plätze und keine Zimmerart gibt.
public static class Kuechendienst
{
    public const string Schluessel = "ausgangsliste.kuechendienst";
    public const int KarenzTage = 4;

    public const string SchluesselAnker = "kuechendienst_anker";
    public const string SchluesselReihenfolge = "kuechendienst_reihenfolge";   // „101,102,103“
    public const string SchluesselGruppen = "kuechendienst_gruppen";           // „109|110;111|112“

    // ----- Reihenfolge und Zimmergruppen -----

    // Gespeicherte Reihenfolge, ergänzt um neue Zimmer (aufsteigend) und ohne verschwundene.
    public static List<string> Reihenfolge(Datenbank db, IEnumerable<Platz> plaetze)
    {
        var vorhanden = Aufsteigend(plaetze.Select(p => p.Zimmernummer).Distinct());
        var gespeichert = db.EinstellungLesen(SchluesselReihenfolge, "")
            .Split(',').Select(z => z.Trim()).Where(z => z != "").ToList();
        var ergebnis = gespeichert.Where(vorhanden.Contains).Distinct().ToList();
        ergebnis.AddRange(vorhanden.Where(z => !ergebnis.Contains(z)));
        return ergebnis;
    }

    public static List<string> Aufsteigend(IEnumerable<string> zimmer) =>
        zimmer.OrderBy(z => int.TryParse(z, out int n) ? n : int.MaxValue)
              .ThenBy(z => z, StringComparer.CurrentCulture)
              .ToList();

    public static void ReihenfolgeSpeichern(Datenbank db, IEnumerable<string> reihenfolge) =>
        db.EinstellungSchreiben(SchluesselReihenfolge, string.Join(",", reihenfolge));

    public static List<string[]> Gruppen(Datenbank db) =>
        db.EinstellungLesen(SchluesselGruppen, "")
            .Split(';')
            .Select(g => g.Split('|').Select(z => z.Trim()).Where(z => z != "").ToArray())
            .Where(g => g.Length >= 2)
            .ToList();

    public static void GruppenSpeichern(Datenbank db, IEnumerable<string[]> gruppen) =>
        db.EinstellungSchreiben(SchluesselGruppen, string.Join(";", gruppen.Select(g => string.Join("|", g))));

    // Feste Reihenfolge der Einträge: Eine Gruppe steht dort, wo ihr erstes Zimmer steht; ihre
    // übrigen Zimmer fallen als eigene Einträge weg.
    public static List<string[]> FesteReihenfolge(List<string> basis, List<string[]> gruppen)
    {
        var eintraege = new List<string[]>();
        foreach (string zimmer in basis)
        {
            string[]? gruppe = gruppen.LastOrDefault(g => g[0] == zimmer);
            if (gruppe != null) eintraege.Add(gruppe);
            else if (!gruppen.Any(g => g.Contains(zimmer))) eintraege.Add([zimmer]);
        }
        return eintraege;
    }

    // ----- Wer kann Dienst machen? -----

    public static bool InKarenz(Platz platz, DateTime heute) =>
        platz.Aufnahmedatum is DateTime aufnahme && (heute.Date - aufnahme.Date).TotalDays < KarenzTage;

    public static bool ZimmerKannDienst(string zimmer, List<Platz> plaetze, DateTime heute) =>
        plaetze.Any(p => p.Zimmernummer == zimmer && p.Anwesend && !InKarenz(p, heute));

    private sealed record Verfuegbar(string[] Quelle, string Text);

    private static List<Verfuegbar> VerfuegbareEintraege(List<string[]> fest, List<Platz> plaetze, DateTime heute)
    {
        var liste = new List<Verfuegbar>();
        foreach (var eintrag in fest)
        {
            var koennen = eintrag.Where(z => ZimmerKannDienst(z, plaetze, heute)).Take(2).ToList();
            if (koennen.Count > 0) liste.Add(new Verfuegbar(eintrag, string.Join("/", koennen)));
        }
        return liste;
    }

    // Wo es nach dem Anker weitergeht: der Eintrag nach dem Anker, sonst der nächste verfügbare.
    private static int StartIndex(string anker, List<Verfuegbar> verfuegbar, List<string[]> fest)
    {
        if (anker == "" || verfuegbar.Count == 0) return 0;
        string ankerZimmer = anker.Split('/')[0].Trim();
        int k = verfuegbar.FindIndex(v => v.Quelle.Contains(ankerZimmer));
        if (k >= 0) return (k + 1) % verfuegbar.Count;
        int p = fest.FindIndex(e => e.Contains(ankerZimmer));
        if (p < 0) return 0;
        for (int versatz = 1; versatz <= fest.Count; versatz++)
        {
            var kandidat = fest[(p + versatz) % fest.Count];
            int treffer = verfuegbar.FindIndex(v => v.Quelle == kandidat);
            if (treffer >= 0) return treffer;
        }
        return 0;
    }

    // ----- Planen -----

    // Plant ab heute (steht heute schon fest: ab morgen) bis zum Ende des Folgemonats neu.
    // Vergangene Tage bleiben; Tausche in der Zukunft bleiben erhalten.
    public static void Planen(Datenbank db, List<Platz> plaetze, DateTime heute)
    {
        heute = heute.Date;
        if (plaetze.Count == 0) return;
        var tage = db.KuechendienstLaden();
        var fest = FesteReihenfolge(Reihenfolge(db, plaetze), Gruppen(db));
        var verfuegbar = VerfuegbareEintraege(fest, plaetze, heute);
        if (verfuegbar.Count == 0) return;

        bool heuteSteht = tage.Any(t => t.Datum == heute);
        DateTime ab = heuteSteht ? heute.AddDays(1) : heute;
        var letzterFester = tage.LastOrDefault(t => t.Datum < ab);
        string anker = letzterFester?.Zimmer ?? db.EinstellungLesen(SchluesselAnker, "");
        var tausche = tage.Where(t => t.Datum >= ab && t.Tausch != "").ToDictionary(t => t.Datum, t => t.Tausch);
        var bis = new DateTime(heute.Year, heute.Month, 1).AddMonths(2).AddDays(-1);
        int index = StartIndex(anker, verfuegbar, fest);

        var neu = new List<KuechendienstTag>();
        for (var datum = ab; datum <= bis; datum = datum.AddDays(1))
        {
            neu.Add(new KuechendienstTag { Datum = datum, Zimmer = verfuegbar[index].Text, Tausch = tausche.GetValueOrDefault(datum, "") });
            index = (index + 1) % verfuegbar.Count;
        }
        db.KuechendienstAbErsetzen(ab, neu);
        if (neu.Count > 0) db.EinstellungSchreiben(SchluesselAnker, neu[^1].Zimmer);
    }

    // ----- Tausch -----

    public static bool TauschMoeglich(DateTime datum, DateTime heute) => datum.Date > heute.Date;

    public static List<string> TauschKandidaten(List<KuechendienstTag> plan, KuechendienstTag tag, DateTime heute) =>
        plan.Where(t => t.Datum >= heute.Date)
            .Select(t => t.Zimmer)
            .Where(z => z != "" && z != tag.Zimmer)
            .Distinct()
            .ToList();

    // ----- Anzeige -----

    public static string NamenFuer(string zimmerText, List<Platz> plaetze) =>
        string.Join(" + ", zimmerText.Split('/')
            .Select(z => z.Trim())
            .SelectMany(z => plaetze.Where(p => p.Zimmernummer == z && p.Anwesend))
            .Select(p => p.Bewohnername));

    public static HashSet<string> ZimmerAm(List<KuechendienstTag> plan, DateTime datum)
    {
        var tag = plan.FirstOrDefault(t => t.Datum == datum.Date);
        if (tag == null) return [];
        return tag.Wirksam.Split('/').Select(z => z.Trim()).Where(z => z != "").ToHashSet();
    }

    public const string KeinDienst = "Kein Küchendienst";

    public static string AnzeigeFuer(List<KuechendienstTag> plan, DateTime datum, List<Platz> plaetze)
    {
        var tag = plan.FirstOrDefault(t => t.Datum == datum.Date);
        if (tag == null) return KeinDienst;
        string namen = NamenFuer(tag.Wirksam, plaetze);
        return namen == "" ? KeinDienst : namen;
    }
}
