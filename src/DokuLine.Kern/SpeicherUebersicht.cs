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

    public List<HeuteFuer> Heute(DateTime jetzt, int hebFristMonate = 12)
    {
        var heute = DateOnly.FromDateTime(jetzt);
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
            return new HeuteFuer(b, termine, medikation, HeimfahrtJetzt(id, jetzt))
            {
                HebFaellig = HebRegeln.Faellig(b, HebBoegen(id), heute, hebFristMonate),
                Fristen = [.. a is null ? [] : AngabenRegeln.Fristen(a, heute), .. SchweigepflichtRegeln.Fristen(schweigepflicht, namen, heute)],
                Zustaendig = a?.GetValueOrDefault(Angabe.Zustaendigkeit)?.Lesen<Zustaendigkeit>() is Zustaendigkeit z
                    ? ZustaendigkeitRegeln.Marken(z, heute) : [],
                Auskunft = SchweigepflichtRegeln.Kurz(schweigepflicht, namen, heute),
            };
        }).ToList();
    }
}
