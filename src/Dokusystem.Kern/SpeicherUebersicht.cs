namespace Dokusystem.Kern;

// Startseite: was heute für jeden Bewohner gilt.
public sealed partial class Speicher
{
    public List<HeuteFuer> Heute(DateTime jetzt, int hebFristMonate = 12)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        var montag = WochenplanRegeln.Montag(heute);
        return AlleBewohner().Select(b =>
        {
            long id = b.Id!.Value;
            var termine = Woche(id, montag).Where(t => t.Tag == heute && t.Teilnahme is not (Teilnahme.Abgesagt or Teilnahme.Ausgefallen)).ToList();
            var medikation = Verordnungen(id)
                .Select(v => (v, UebersichtRegeln.MedikationAm(v, heute)))
                .Where(x => x.Item2 is not null)
                .Select(x => (x.v, x.Item2!))
                .ToList();
            return new HeuteFuer(b, termine, medikation, HeimfahrtJetzt(id, jetzt))
            {
                HebFaellig = HebRegeln.Faellig(b, HebBoegen(id), heute, hebFristMonate),
            };
        }).ToList();
    }
}
