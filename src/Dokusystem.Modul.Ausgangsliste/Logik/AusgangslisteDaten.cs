using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Modul.Ausgangsliste;

// Der Datenteil des Moduls für die Modulverwaltung: Tabellen und Demo-Daten.
public sealed partial class AusgangslisteDaten : IModulDaten
{
    public const string Kurzname = "ausgangsliste";
    public string Schluessel => Kurzname;
    public IReadOnlyList<string> Schritte => Datenbank.Schritte;

    // Ausgedachte Demo-Daten (daten/demo/ausgangsliste.json), Tage und Uhrzeiten relativ zu jetzt.
    public void DemoEinfuegen(Speicher speicher, SqliteTransaction transaktion)
    {
        var demo = Laden();
        var db = new Datenbank(speicher, transaktion);
        var jetzt = speicher.Uhr();
        var heute = DateOnly.FromDateTime(jetzt);
        db.Leeren();
        var bewohner = speicher.AlleBewohner(transaktion);
        long? Id(string nachname) => bewohner.FirstOrDefault(b => b.Nachname == nachname)?.Id;

        foreach (var p in demo.Plaetze)
        {
            if (Id(p.Nachname) is not long id) continue;
            var status = p.Status switch
            {
                "beschraenkung" => AnwesenheitsStatus.Ausgangsbeschraenkung,
                "abwesenheit" => AnwesenheitsStatus.Abwesenheit,
                _ => AnwesenheitsStatus.Keiner,
            };
            string grund = Einsetzen(p.Grund ?? "", heute);
            var einverstaendnis = p.EinverstaendnisVorTagen is int vor ? heute.AddDays(-vor) : (DateOnly?)null;
            db.AngabenSpeichern(id, new PlatzAngaben
            {
                Ausgang = p.Ausgang, Status = status, StatusGrund = grund, Forensik = p.Forensik, Einverstaendnis = einverstaendnis,
            });
            // Ein paar Einträge im Verlauf, damit er sich vorführen lässt.
            if (p.AusgangVorTagen is int tage)
                db.VerlaufSchreiben(id, $"Ausgang: {Verlauf.Ausgang(p.AusgangVorher ?? "")} → {Verlauf.Ausgang(p.Ausgang)}", "AB", jetzt.AddDays(-tage).Date.AddHours(10));
            if (einverstaendnis is DateOnly e)
                db.VerlaufSchreiben(id, $"Unterbringung: nicht erfasst → offen geführt (Datum {Datum.Text(e)})", "CD", e.ToDateTime(new TimeOnly(11, 30)));
            if (p.Forensik)
                db.VerlaufSchreiben(id, "Unterbringung: nicht erfasst → Forensik", "CD", jetzt.AddDays(-40).Date.AddHours(9));
            if (status != AnwesenheitsStatus.Keiner)
                db.VerlaufSchreiben(id, $"Status: {Verlauf.Status(status)} – {grund}", "EF", jetzt.AddDays(-(p.StatusVorTagen ?? 0)).Date.AddHours(16));
        }

        var plaetze = db.Laden();
        foreach (var a in demo.Ausgaenge)
        {
            var platz = plaetze.FirstOrDefault(x => x.Bewohnername.EndsWith(" " + a.Nachname, StringComparison.Ordinal));
            if (platz is null || platz.HatStatus || DigitalerAusgang.Anspruch(platz, a.Art) == 0) continue;
            var e = new AusgangEintrag
            {
                BewohnerId = platz.BewohnerId, Datum = jetzt.Date, Beginn = DigitalerAusgang.Vor(jetzt, a.VonVorStunden),
                Ende = a.BisVorStunden is double bis ? DigitalerAusgang.Vor(jetzt, bis) : null, Art = a.Art, HdzBeginn = a.Hdz,
                HdzEnde = a.BisVorStunden is null ? "" : a.HdzEnde ?? a.Hdz,
            };
            db.AusgangEinfuegen(e);
        }

        if (demo.Kuechendienst is { } k)
        {
            Kuechendienst.GruppenSpeichern(db, k.Gruppen.Select(g => g.ToArray()));
            Kuechendienst.Planen(db, plaetze, jetzt.Date.AddDays(-k.PlanAbVorTagen));
            Kuechendienst.Planen(db, plaetze, jetzt.Date);
            if (k.Tausch is { } t) db.KuechendienstTauschSetzen(jetzt.Date.AddDays(t.InTagen), t.Zimmer);
        }

        speicher.ModulSetzen(Kuechendienst.Schluessel, true, transaktion);
        speicher.ModulSetzen(DigitalerAusgang.Schluessel, true, transaktion);
    }

    // „{+5}“ → Datum in 5 Tagen, „{-3}“ → vor 3 Tagen (TT.MM.JJJJ).
    [GeneratedRegex(@"\{([+-]\d+)\}")]
    private static partial Regex Platzhalter();

    public static string Einsetzen(string text, DateOnly heute) =>
        Platzhalter().Replace(text, m => Datum.Text(heute.AddDays(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))));

    private static DemoDatei Laden()
    {
        using var strom = typeof(AusgangslisteDaten).Assembly.GetManifestResourceStream("Dokusystem.Modul.Ausgangsliste.Demo.ausgangsliste.json")
                          ?? throw new SpeicherFehler("Die Demo-Daten der Ausgangsliste fehlen im Programm.");
        return JsonSerializer.Deserialize<DemoDatei>(strom, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new SpeicherFehler("Die Demo-Daten der Ausgangsliste sind leer.");
    }

    private sealed record DemoDatei(List<DemoPlatz> Plaetze, List<DemoAusgang> Ausgaenge, DemoKuechendienst? Kuechendienst);

    private sealed record DemoPlatz(string Nachname, string Ausgang, string? Status, string? Grund, int? StatusVorTagen,
        bool Forensik, int? EinverstaendnisVorTagen, int? AusgangVorTagen, string? AusgangVorher);

    private sealed record DemoAusgang(string Nachname, string Art, double VonVorStunden, double? BisVorStunden, string Hdz, string? HdzEnde);

    private sealed record DemoKuechendienst(List<List<string>> Gruppen, int PlanAbVorTagen, DemoTausch? Tausch);

    private sealed record DemoTausch(int InTagen, string Zimmer);
}
