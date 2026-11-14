namespace DokuLine.Kern;

// Startseite: was heute für jeden Bewohner gilt.
public sealed partial class Speicher
{
    // Nur der Name aus dem Personenverzeichnis (Schweigepflicht: „Mutter · Erika Ahornblatt“).
    public Func<long?, string> PersonenNamen()
    {
        var personen = Personen().ToDictionary(p => p.Id, p => p.Name);
        return id => id is long x && personen.TryGetValue(x, out var n) ? n : "?";
    }

    public List<HeuteFuer> Heute(DateTime jetzt)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        var w = Einstellungen.Laden();
        var heb = HebFristen(AlleBewohner(), heute).ToDictionary(s => s.Bewohner.Id!.Value);
        var montag = WochenplanRegeln.Montag(heute);
        var angaben = AlleAngaben(heute);
        var namen = PersonenNamen();
        return AlleBewohner().Select(b =>
        {
            long id = b.Id!.Value;
            var termine = Woche(id, montag).Where(t => t.Tag == heute && t.Teilnahme is not (Teilnahme.Abgesagt or Teilnahme.Ausgefallen)).ToList();
            var medikation = Verordnungen(id)
                .Select(v => (v, UebersichtRegeln.MedikationAm(v, heute)))
                .Where(x => x.Item2 is not null)
                .Select(x => (x.v, x.Item2!))
                .ToList();
            var a = angaben.GetValueOrDefault(id);
            var schweigepflicht = a?.GetValueOrDefault(Angabe.Schweigepflicht);
            // Ab der zweiten Stufe steht die HEB-Frist unter „Fristen“ (Kasten der Übersicht), vorher nur als Marke.
            var hebMarke = heb.TryGetValue(id, out var hs) ? HebFristRegeln.Marke(hs, heute, w.HebErinnernWochen, w.HebAchtungWochen) : null;
            bool hebFrist = hebMarke is not null && hs is not null && HebFristRegeln.Stufe(hs, heute, w.HebErinnernWochen, w.HebAchtungWochen) >= HebStufe.Achtung;
            return new HeuteFuer(b, termine, medikation, HeimfahrtJetzt(id, jetzt))
            {
                Heb = hebFrist ? null : hebMarke,
                Fristen = [.. a is null ? [] : AngabenRegeln.Fristen(a, heute), .. SchweigepflichtRegeln.Fristen(schweigepflicht, namen, heute),
                           .. hebFrist ? [hebMarke!] : Array.Empty<Marke>()],
                Zustaendig = a?.GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>() is Zustaendigkeit z
                    ? ZustaendigkeitRegeln.Marken(z, heute) : [],
                Auskunft = SchweigepflichtRegeln.Kurz(schweigepflicht, namen, heute),
            };
        }).ToList();
    }
}
