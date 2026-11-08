using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Übernommen aus der Ausgangsliste (Platz.cs), angepasst an DokuLine:
// Bewohner, Zimmer und Aufnahme gehören zum Kern (Bewohnerakte); das Modul ergänzt je Bewohner
// Ausgang, Anwesenheitsstatus und Unterbringung. Freie Plätze, Zimmerart und Mehrplatz-Schutz
// entfallen (eine Bewohnerliste, kein Doppelpflegen).

// Der Anwesenheitsstatus.
public enum AnwesenheitsStatus
{
    Keiner = 0,                // normal anwesend
    Ausgangsbeschraenkung = 1, // anwesend, aber kein freier Ausgang (auch Neuaufnahme)
    Abwesenheit = 2,           // nicht im Haus (Heimfahrt, Klinik …)
}

// Wie es um die Frist der Unterbringung steht (Beschluss-Ablauf bzw. Gültigkeit der
// Einverständniserklärung).
public enum FristStufe
{
    Keine,       // Forensik: keine Frist
    Fehlt,       // Datum nicht eingetragen oder Unterbringung nicht erfasst
    Gueltig,     // mehr als einen Monat Zeit
    Bald,        // läuft innerhalb eines Monats ab → Hinweis
    Abgelaufen,
}

// Der Unterbringungsstatus. Beschluss kommt aus der Bewohnerakte (Beschlussende); offen geführt
// und Forensik führt das Modul. NichtErfasst: In der Akte ist der Beschluss nicht erfasst – dann
// ist offen, ob eine Ausgangsbeschränkung zulässig wäre (Regel 5: leer ist nicht „nichts“).
public enum Unterbringung
{
    OffenGefuehrt = 0,
    Beschluss = 1,
    Forensik = 2,
    NichtErfasst = 3,
}

public class Platz
{
    public long BewohnerId { get; init; }
    public string Zimmernummer { get; init; } = "";
    public string Bewohnername { get; init; } = "";
    public DateTime? Aufnahmedatum { get; init; }

    // Die Ausgangs-Einträge als Text: Bausteine wie „2 Allein“ oder „1 MBW“, verbunden mit
    // „ / “ – die Zahl vorn sind die Stunden.
    public string Ausgang { get; set; } = "";

    public Unterbringung Unterbringung { get; set; } = Unterbringung.NichtErfasst;

    // „Offen geführt“ laut Bewohnerakte, zum Abgleich mit der Unterbringung des Moduls.
    public OffenGefuehrtZustand OffenGefuehrtLautAkte { get; init; }

    // Widerspruch zwischen Akte und Modul (nur als Hinweis, nichts wird automatisch geändert).
    public bool Widerspruch => (OffenGefuehrtLautAkte, Unterbringung) switch
    {
        (OffenGefuehrtZustand.Ja, Unterbringung.Beschluss or Unterbringung.Forensik) => true,
        (OffenGefuehrtZustand.Nein, Unterbringung.OffenGefuehrt) => true,
        _ => false,
    };

    // Datum zur Unterbringung:
    //   Beschluss     → Ablaufdatum des Beschlusses (aus der Akte)
    //   Offen geführt → Tag der Unterschrift unter die Einverständniserklärung (gilt 6 Monate)
    //   Forensik      → kein Datum
    public DateTime? UnterbringungDatum { get; set; }
    public AnwesenheitsStatus Status { get; set; } = AnwesenheitsStatus.Keiner;

    // Der Grund zum Status, z. B. „Heimfahrt bis 28.09.2026“.
    public string StatusGrund { get; set; } = "";

    // Rechtsregel: Eine Ausgangsbeschränkung ist nur bei Beschluss oder Forensik zulässig. Bei
    // offen geführten Bewohnern wäre sie Freiheitsentzug. Ist die Unterbringung nicht erfasst,
    // ist sie ebenfalls nicht zulässig, bis das geklärt ist.
    public bool AusgangsbeschraenkungErlaubt => Unterbringung is Unterbringung.Beschluss or Unterbringung.Forensik;
    public const string HinweisNurMitBeschluss =
        "Eine Ausgangsbeschränkung ist nur bei Beschluss oder Forensik zulässig. " +
        "Bei offen geführten Bewohnern wäre sie Freiheitsentzug.";
    public const string HinweisNichtErfasst =
        "Der Beschluss ist in der Bewohnerakte nicht erfasst. Bitte erst dort klären, ob ein Beschluss besteht.";

    // ----- Fristen der Unterbringung -----
    public const int EinverstaendnisMonate = 6;

    // Bis wann die Grundlage gilt (einschließlich dieses Tages).
    public DateTime? UnterbringungGueltigBis => Unterbringung switch
    {
        Unterbringung.Beschluss => UnterbringungDatum?.Date,
        Unterbringung.OffenGefuehrt => UnterbringungDatum?.Date.AddMonths(EinverstaendnisMonate),
        _ => null,
    };

    // Ein Monat vor Ablauf kommt der Hinweis.
    public FristStufe Frist(DateTime heute)
    {
        if (Unterbringung == Unterbringung.Forensik) return FristStufe.Keine;
        if (Unterbringung == Unterbringung.NichtErfasst) return FristStufe.Fehlt;
        if (UnterbringungGueltigBis is not DateTime bis) return FristStufe.Fehlt;
        if (heute.Date > bis) return FristStufe.Abgelaufen;
        if (heute.Date >= bis.AddMonths(-1)) return FristStufe.Bald;
        return FristStufe.Gueltig;
    }

    public bool FristBrauchtHinweis(DateTime heute) =>
        Frist(heute) is FristStufe.Fehlt or FristStufe.Bald or FristStufe.Abgelaufen;

    // „Beschluss endet in 12 Tagen“, „Einverständnis abgelaufen seit 3 Tagen“ …
    public string FristText(DateTime heute)
    {
        if (Unterbringung == Unterbringung.NichtErfasst) return "Unterbringung nicht erfasst";
        string was = Unterbringung == Unterbringung.Beschluss ? "Beschluss" : "Einverständnis";
        DateTime bis = UnterbringungGueltigBis ?? heute;
        int tage = (bis - heute.Date).Days;
        return Frist(heute) switch
        {
            FristStufe.Fehlt => Unterbringung == Unterbringung.Beschluss ? "Beschluss: Ablaufdatum fehlt" : "Einverständnis: Datum fehlt",
            FristStufe.Abgelaufen => tage == -1 ? $"{was} seit gestern abgelaufen" : $"{was} abgelaufen seit {-tage} Tagen",
            FristStufe.Bald => tage switch { 0 => $"{was} endet heute", 1 => $"{was} endet morgen", _ => $"{was} endet in {tage} Tagen" },
            FristStufe.Gueltig => $"{was} bis {bis:dd.MM.yyyy}",
            _ => "",
        };
    }

    // Altbestand oder Fehler: Beschränkung läuft, obwohl nicht zulässig.
    public bool BeschraenkungUnzulaessig =>
        Status == AnwesenheitsStatus.Ausgangsbeschraenkung && !AusgangsbeschraenkungErlaubt;
    public bool HatStatus => Status != AnwesenheitsStatus.Keiner;
    public bool Anwesend => Status != AnwesenheitsStatus.Abwesenheit;

    // Was in der Ausgangsspalte steht: bei aktivem Status der Grund, sonst die Einträge des
    // Tages. Beide Werte liegen getrennt nebeneinander – beim Aufheben des Status ist der alte
    // Stand deshalb automatisch wieder da.
    public string AusgangAnzeige => HatStatus ? StatusGrund : Ausgang;

    // Stundensumme aus den Ausgangs-Einträgen: Text an „ / “ trennen, je Baustein zählt das
    // erste Wort, wenn es eine Zahl ist.
    public int Stunden
    {
        get
        {
            int summe = 0;
            foreach (string teil in Ausgang.Split(" / "))
            {
                string[] woerter = teil.Trim().Split(' ');
                if (woerter.Length > 0 && int.TryParse(woerter[0], out int zahl)) summe += zahl;
            }
            return summe;
        }
    }

    // Bei aktivem Status bleibt die Stundenspalte leer.
    public string StundenText => !HatStatus && Stunden > 0 ? Stunden.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    // Der Platz eines Bewohners aus den Angaben der Akte und den Angaben des Moduls.
    public static Platz Aus(Bewohner b, PlatzAngaben? angaben)
    {
        angaben ??= new PlatzAngaben();
        var unterbringung = b.Beschluss switch
        {
            BeschlussZustand.Ende => Unterbringung.Beschluss,
            BeschlussZustand.KeinBeschluss => angaben.Forensik ? Unterbringung.Forensik : Unterbringung.OffenGefuehrt,
            _ => Unterbringung.NichtErfasst,
        };
        return new Platz
        {
            BewohnerId = b.Id ?? 0,
            Zimmernummer = b.Zimmer,
            Bewohnername = b.Name,
            Aufnahmedatum = b.Aufnahmedatum.ToDateTime(TimeOnly.MinValue),
            Ausgang = angaben.Ausgang,
            Status = angaben.Status,
            StatusGrund = angaben.StatusGrund,
            Unterbringung = unterbringung,
            OffenGefuehrtLautAkte = b.OffenGefuehrt,
            UnterbringungDatum = unterbringung switch
            {
                Unterbringung.Beschluss => b.Beschlussende?.ToDateTime(TimeOnly.MinValue),
                Unterbringung.OffenGefuehrt => angaben.Einverstaendnis?.ToDateTime(TimeOnly.MinValue),
                _ => null,
            },
        };
    }
}

// Was das Modul je Bewohner speichert (Tabelle al_plaetze).
public sealed record PlatzAngaben
{
    public string Ausgang { get; init; } = "";
    public AnwesenheitsStatus Status { get; init; }
    public string StatusGrund { get; init; } = "";
    public bool Forensik { get; init; }
    public DateOnly? Einverstaendnis { get; init; }
}
