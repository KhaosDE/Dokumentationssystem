namespace DokuLine.Kern;

// HEB-Fälligkeit (Bauplan 52): bis wann der nächste HEB-Bogen beim Kostenträger sein muss, mit Erinnerung in Stufen. Ereignisse
// werden nie geändert; Fälligkeit, Zustand und Stand ergeben sich aus ihnen. Drei Zustände (Regel 5): Datum eingetragen ·
// „kein HEB nötig“ (mit Grund) · nicht erfasst – dann gilt für die Erinnerung der Vorschlag, gezeigt in Achtung-Farbe.
public enum HebFristZustand { Eingetragen, KeinHeb, NichtErfasst }

public enum HebBogenStand { Offen, InArbeit, Fertig, Abgegeben }

public enum HebStufe { Keine, Hinweis, Achtung, Ueberfaellig }

public sealed record HebFristEreignis(long Id, long BewohnerId, string Art, DateOnly? Datum, string Grund, long? BogenId, long? AufgabeId,
    string Kuerzel, DateTime Erfasst);

public sealed record HebFristStand(Bewohner Bewohner, HebFristZustand Zustand, DateOnly? FaelligAm, string Grund, HebBogenStand Stand,
    HebBogen? Bogen, HebFristEreignis? LetzteAbgabe, long? AufgabeId);

public static class HebFristRegeln
{
    public const int ErinnernVorgabe = 8;
    public const int AchtungVorgabe = 4;
    public const int ErstberichtMonate = 3;
    public const string KalenderTitel = "HEB fällig";

    public static string ZustandText(HebFristZustand z) => z switch
    {
        HebFristZustand.Eingetragen => "eingetragen",
        HebFristZustand.KeinHeb => "kein HEB nötig",
        _ => "nicht erfasst",
    };

    public static string StandText(HebFristStand s) => s.Stand switch
    {
        HebBogenStand.InArbeit => "in Arbeit",
        HebBogenStand.Fertig => "fertig",
        HebBogenStand.Abgegeben when s.LetzteAbgabe is HebFristEreignis a => $"abgegeben am {Datum.Text(a.Datum)} ({a.Kuerzel})",
        _ => "offen",
    };

    // Vorschlag: Ende der jüngsten Bewilligung (nach einer Abgabe nur einer, die danach beginnt – der HEB wird vor dem Ende der
    // laufenden abgegeben), sonst der letzte fertige Bogen plus Frist, wenn er nach der Abgabe liegt; danach die Abgabe plus
    // Frist; ohne Bogen und Abgabe der Erstbericht drei Monate nach der Aufnahme.
    public static DateOnly Vorschlag(Bewohner b, Kostentraeger? k, IReadOnlyList<HebBogen> boegen, DateOnly? letzteAbgabe, int fristMonate)
    {
        var grenze = letzteAbgabe ?? DateOnly.MinValue;
        var bewilligungen = (k?.Bewilligungen ?? []).Where(x => letzteAbgabe is not DateOnly a || x.Von > a).ToList();
        if (bewilligungen.Count > 0 && bewilligungen.Max(x => x.Bis) is var bewilligung && bewilligung > grenze) return bewilligung;
        if (boegen.Where(x => x.Fertig).OrderBy(x => x.Bis).LastOrDefault() is HebBogen letzter && letzter.Bis.AddMonths(fristMonate) > grenze)
            return letzter.Bis.AddMonths(fristMonate);
        return letzteAbgabe is DateOnly a ? a.AddMonths(fristMonate) : b.Aufnahmedatum.AddMonths(ErstberichtMonate);
    }

    // Aus den Ereignissen seit der letzten Abgabe. Der Bogen dazu: der jüngste nicht abgegebene, der nach der letzten Abgabe
    // angelegt wurde – ohne Abgabe im Programm (Bögen von früher) höchstens eine halbe Frist vor der Fälligkeit.
    public static HebFristStand Stand(Bewohner b, IReadOnlyList<HebFristEreignis> ereignisse, IReadOnlyList<HebBogen> boegen, Kostentraeger? k,
        int fristMonate)
    {
        var sortiert = ereignisse.OrderBy(e => e.Erfasst).ThenBy(e => e.Id).ToList();
        var abgabe = sortiert.LastOrDefault(e => e.Art == "abgegeben");
        var danach = abgabe is null ? sortiert : sortiert.SkipWhile(e => e != abgabe).Skip(1).ToList();
        var setzung = danach.LastOrDefault(e => e.Art is "faellig" or "kein");
        var zustand = setzung?.Art switch { "faellig" => HebFristZustand.Eingetragen, "kein" => HebFristZustand.KeinHeb, _ => HebFristZustand.NichtErfasst };
        DateOnly? faellig = zustand switch
        {
            HebFristZustand.Eingetragen => setzung!.Datum,
            HebFristZustand.KeinHeb => null,
            _ => Vorschlag(b, k, boegen, abgabe?.Datum, fristMonate),
        };
        var abgegeben = sortiert.Where(e => e.Art == "abgegeben" && e.BogenId is long).Select(e => e.BogenId!.Value).ToHashSet();
        var ab = abgabe is not null ? DateOnly.FromDateTime(abgabe.Erfasst)
            : faellig?.AddMonths(-Math.Max(1, fristMonate / 2)) ?? DateOnly.MinValue;
        var bogen = boegen.Where(x => !abgegeben.Contains(x.Id) && x.Erstellt >= ab).OrderBy(x => x.Id).LastOrDefault();
        var stand = bogen is not null ? (bogen.Fertig ? HebBogenStand.Fertig : HebBogenStand.InArbeit)
            : abgabe is not null && setzung is null ? HebBogenStand.Abgegeben : HebBogenStand.Offen;
        var aufgabe = danach.LastOrDefault(e => e.Art == "aufgabe")?.AufgabeId;
        return new HebFristStand(b, zustand, faellig, setzung?.Grund ?? "", stand, bogen, abgabe, aufgabe);
    }

    public static HebStufe Stufe(HebFristStand s, DateOnly heute, int erinnernWochen, int achtungWochen)
    {
        if (s.Zustand == HebFristZustand.KeinHeb || s.FaelligAm is not DateOnly f) return HebStufe.Keine;
        int tage = f.DayNumber - heute.DayNumber;
        return tage < 0 ? HebStufe.Ueberfaellig : tage <= achtungWochen * 7 ? HebStufe.Achtung : tage <= erinnernWochen * 7 ? HebStufe.Hinweis : HebStufe.Keine;
    }

    public static string FaelligText(DateOnly f, DateOnly heute)
    {
        int tage = f.DayNumber - heute.DayNumber;
        if (tage < 0) return $"HEB überfällig seit {Datum.Text(f)}";
        if (tage == 0) return "HEB fällig heute";
        if (tage < 7) return $"HEB fällig am {Datum.Text(f)}";
        int wochen = (tage + 6) / 7;
        return $"HEB fällig in {wochen} {(wochen == 1 ? "Woche" : "Wochen")}";
    }

    // Eine Marke, Farbe nie allein (immer mit Wort): Hinweis 8 Wochen vorher, Achtung 4 Wochen vorher, Gefahr überfällig.
    // Nicht erfasst: mindestens Achtung; ohne Stufe „HEB-Frist nicht erfasst“.
    public static Marke? Marke(HebFristStand s, DateOnly heute, int erinnernWochen, int achtungWochen)
    {
        if (s.Zustand == HebFristZustand.KeinHeb || s.FaelligAm is not DateOnly f) return null;
        var stufe = Stufe(s, heute, erinnernWochen, achtungWochen);
        bool vorschlag = s.Zustand == HebFristZustand.NichtErfasst;
        if (stufe == HebStufe.Keine) return vorschlag ? new Marke(MarkenArt.NichtErfasst, "HEB-Frist nicht erfasst") : null;
        var art = stufe switch
        {
            HebStufe.Ueberfaellig => MarkenArt.Ueberfaellig,
            HebStufe.Hinweis when !vorschlag => MarkenArt.Wichtig,
            _ => MarkenArt.NichtErfasst,
        };
        return new Marke(art, FaelligText(f, heute) + (vorschlag ? " (Vorschlag)" : ""));
    }

    // Kurzzeile für die Stammdaten, den Reiter „Kostenträger“ und das Hilfeplangespräch.
    public static string KurzText(HebFristStand s) => s.Zustand switch
    {
        HebFristZustand.KeinHeb => $"kein HEB nötig ({s.Grund})",
        HebFristZustand.NichtErfasst => $"nicht erfasst (Vorschlag {Datum.Text(s.FaelligAm)}) · {StandText(s)}",
        _ => $"fällig am {Datum.Text(s.FaelligAm)} · {StandText(s)}",
    };

    // Aufgabe der ersten Stufe: einmal je Abschnitt, solange kein Bogen angelegt ist.
    public static bool AufgabeNoetig(HebFristStand s, DateOnly heute, int erinnernWochen, int achtungWochen) =>
        s.AufgabeId is null && (s.Stand is HebBogenStand.Offen or HebBogenStand.Abgegeben) && Stufe(s, heute, erinnernWochen, achtungWochen) != HebStufe.Keine;

    public static string AufgabeText(Bewohner b, DateOnly faellig, string? bezugspflege) =>
        $"HEB-Bogen für {b.Name} beginnen (fällig {Datum.Text(faellig)})" + (bezugspflege is null ? "" : $" – Bezugspflege {bezugspflege}");

    // Ganztagseintrag im Bereichskalender, nur Name und „HEB fällig“; nur bei eingetragenem Datum.
    public static KalenderTermin? Kalendereintrag(HebFristStand s) =>
        s is { Zustand: HebFristZustand.Eingetragen, FaelligAm: DateOnly f }
            ? new KalenderTermin
            {
                Id = -s.Bewohner.Id!.Value,
                Fassungen = [new KalenderFassung
                {
                    Art = KalenderArt.Bewohner, Titel = KalenderTitel, Beginn = f.ToDateTime(TimeOnly.MinValue), Ende = f.ToDateTime(TimeOnly.MinValue),
                    Ganztags = true, Bewohner = [s.Bewohner.Id!.Value], Hinweis = "aus der HEB-Frist",
                }],
            }
            : null;

    // Prüfseite des QMB: „HEB aktuell“ ist nicht gegeben, wenn überfällig oder nicht erfasst.
    public static string? QmText(HebFristStand s, DateOnly heute) => s.Zustand switch
    {
        HebFristZustand.KeinHeb => null,
        HebFristZustand.NichtErfasst => "HEB-Frist nicht erfasst",
        _ => s.FaelligAm is DateOnly f && f < heute ? FaelligText(f, heute) : null,
    };
}
