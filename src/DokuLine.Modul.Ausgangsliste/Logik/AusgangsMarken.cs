using System.Text.RegularExpressions;
using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Marken des Moduls. Auf den Bewohnerkarten des Kerns nur, was Aufmerksamkeit braucht (Farbe
// sparsam); auf den Karten des Moduls zusätzlich Zimmer, Unterbringung und Küchendienst.
// „Beschluss nicht erfasst“ zeigt der Kern schon selbst.
public static partial class AusgangsMarken
{
    // Auf den schmalen Bewohnerkarten Datum ohne Jahr: „bis 09.11.“
    [GeneratedRegex(@"(\d{2}\.\d{2}\.)\d{4}")]
    private static partial Regex Jahr();

    public static List<Marke> Fuer(Platz p, DateTime jetzt, DigitalStand? digital, bool alle = false, bool kueche = false)
    {
        var marken = new List<Marke>();
        if (alle) marken.Add(new Marke(MarkenArt.Ausgang, $"Zimmer {p.Zimmernummer}"));
        switch (p.Status)
        {
            case AnwesenheitsStatus.Abwesenheit:
                marken.Add(new Marke(MarkenArt.Abwesend, "abwesend: " + (alle ? p.StatusGrund : Jahr().Replace(KartenText.GrundOhneTitel(p), "$1"))));
                break;
            case AnwesenheitsStatus.Ausgangsbeschraenkung when p.BeschraenkungUnzulaessig:
                marken.Add(new Marke(MarkenArt.NichtErfasst, "Ausgangsbeschränkung nicht zulässig – bitte klären"));
                break;
            case AnwesenheitsStatus.Ausgangsbeschraenkung:
                string rest = KartenText.GrundOhneTitel(p);
                marken.Add(new Marke(MarkenArt.Ausgang, "Ausgangsbeschränkung" + (alle && rest.Length > 0 && rest.Length <= 30 ? $" {rest}" : "")));
                break;
        }
        if (p.HatStatus)
        {
            string frist = KartenText.Frist(p.StatusGrund, jetzt, out bool abgelaufen);
            if (abgelaufen) marken.Add(new Marke(MarkenArt.NichtErfasst, $"{Verlauf.Status(p.Status)}: {frist}"));
        }
        if (digital?.Laufend is AusgangEintrag e)
            marken.Add(digital.Ueberfaellig && digital.ZurueckBis is TimeSpan bis
                ? new Marke(MarkenArt.NichtErfasst, $"überfällig seit {DigitalerAusgang.Uhrzeit(bis)}")
                : new Marke(MarkenArt.Ausgang, $"unterwegs seit {DigitalerAusgang.Uhrzeit(e.Beginn)} ({e.Art})"));
        if (alle && p.Unterbringung != Unterbringung.NichtErfasst)
            marken.Add(new Marke(MarkenArt.Ausgang, Verlauf.Unterbringung(p.Unterbringung)));
        if (p.Unterbringung != Unterbringung.NichtErfasst && p.FristBrauchtHinweis(jetzt))
            marken.Add(new Marke(MarkenArt.NichtErfasst, p.FristText(jetzt)));
        if (p.Widerspruch)
            marken.Add(new Marke(MarkenArt.NichtErfasst, alle
                ? $"Widerspruch: Akte „{(p.OffenGefuehrtLautAkte == OffenGefuehrtZustand.Ja ? "offen geführt" : "nicht offen geführt")}“, " +
                  $"Ausgangsliste „{Verlauf.Unterbringung(p.Unterbringung)}“ – bitte klären"
                : "Unterbringung widersprüchlich – bitte klären"));
        if (alle && p.Unterbringung == Unterbringung.NichtErfasst)
            marken.Add(new Marke(MarkenArt.NichtErfasst, "Unterbringung nicht erfasst"));
        if (kueche) marken.Add(new Marke(MarkenArt.Ausgang, "Küchendienst heute"));
        return marken;
    }
}
