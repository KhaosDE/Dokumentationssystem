using DokuLine.Kern;
using Microsoft.Data.Sqlite;

namespace DokuLine.Tests;

// Sicherheitsnetz für Updates (nach dem Startabbruch in 0.33.0, behoben in 0.33.1):
// Beim ersten Start nach einem Update holt das Programm einmalige Schritte nach (neue Rechte vorbelegen,
// neue Demo-Daten laden …). Jeder dieser Schritte merkt sich in der Tabelle „meta“, dass er gelaufen ist.
// Dieser Test entfernt in einer Datenbank, die der Nutzer verändert hat, jeden Merker einzeln und alle
// zusammen – so, als käme die Datenbank aus einer älteren Version – und startet neu. Der Start darf nie
// abbrechen. Die Merker werden aus der Datenbank gelesen: Ein künftiger Schritt ist automatisch mitgeprüft.
// Gegenstück für die Oberfläche: UpdateSicherheitOberflaechenTests.
[Collection("Datenordner")]
public sealed class UpdateSicherheitTests : IDisposable
{
    private readonly Datenordner daten = new();
    private readonly string vorlage;

    public UpdateSicherheitTests()
    {
        using var s = new Speicher();
        vorlage = s.Pfad;
        s.DemoBeimErstenStart();
        NutzerStand.Veraendern(s);
    }

    public void Dispose() => daten.Dispose();

    private List<(string Schluessel, string Wert)> Merker()
    {
        using var db = new SqliteConnection($"Data Source={vorlage};Pooling=False");
        db.Open();
        return NutzerStand.Merker(db);
    }

    // Kopie der Vorlage ohne die genannten Merker, dann starten wie das Programm. null = alles gut.
    private string? Starten(string name, IEnumerable<string> entfernen, Action<Speicher>? danach = null)
    {
        string pfad = Path.Combine(daten.Ordner, $"start-{Guid.NewGuid():N}.db");
        File.Copy(vorlage, pfad);
        using (var db = new SqliteConnection($"Data Source={pfad};Pooling=False"))
        {
            db.Open();
            NutzerStand.MerkerEntfernen(db, entfernen);
        }
        try
        {
            using (var s = new Speicher(pfad))
            {
                s.DemoBeimErstenStart();
                danach?.Invoke(s);
            }
            using var zweiter = new Speicher(pfad);   // zweiter Start: nichts mehr nachzuholen
            Assert.False(zweiter.DemoBeimErstenStart(), "Beim zweiten Start wurde wieder etwas nachgeholt.");
            return null;
        }
        catch (Exception e)
        {
            return $"{name}: {e.GetType().Name}: {e.Message}";
        }
    }

    [Fact]
    public void Vorlage_ist_wirklich_veraendert()
    {
        using (var s = new Speicher(vorlage))
        {
            Assert.DoesNotContain(s.Positionsliste(), p => p.Name == "Mitarbeiter Wohnbereich");
            Assert.DoesNotContain(s.Bereichsliste(), b => b.Name == "R1");
            Assert.False(s.MitarbeiterLesen("AB")!.Aktiv);
        }
        var merker = Merker();
        Assert.Contains(merker, m => m.Schluessel == "rechte_vorbelegt_" + Funktionen.BedarfGeben);
        Assert.True(merker.Count >= 30, $"Nur {merker.Count} Merker – fehlt die Demo?");
    }

    [Fact]
    public void Jeder_einmalige_Schritt_einzeln_nachgeholt_startet()
    {
        var merker = Merker();
        var fehler = merker.Select(m => Starten(m.Schluessel, [m.Schluessel], s =>
            {
                // Ein Schritt, der gelaufen ist, setzt seinen Merker wieder (sonst liefe er bei jedem Start).
                if (m.Wert == "1" && !m.Schluessel.StartsWith("modul_", StringComparison.Ordinal))
                    Assert.True(s.Meta(m.Schluessel) is not null, "Merker nach dem Start nicht wieder gesetzt.");
            }))
            .OfType<string>().ToList();
        Assert.True(fehler.Count == 0, $"{fehler.Count} von {merker.Count} Schritten brechen den Start ab:\n" + string.Join("\n", fehler));
    }

    [Fact]
    public void Alle_einmaligen_Schritte_zusammen_nachgeholt_startet()
    {
        var entfernen = Merker().Select(m => m.Schluessel).Where(k => !NutzerStand.NeuLaden.Contains(k)).ToList();
        Assert.Null(Starten("alle zusammen", entfernen));
    }
}
