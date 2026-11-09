using System.Diagnostics;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26h: 23 Bewohner je Wohnbereich. Bildschirmfotos aller Hauptansichten bei
// 1920 × 1080 und 1366 × 768 zur Sichtprüfung, dazu die Zeit für den Aufbau jeder Ansicht
// (109_zeiten.txt neben den Fotos: erster und wiederholter Wechsel). Richtwert: Übersicht unter einer Sekunde.
[Collection("Oberflaeche")]
public class UebersichtlichkeitTests
{
    private static readonly (string Bereich, string Datei)[] Ansichten =
    [
        ("Übersicht", "uebersicht"), ("Tagesplan", "tagesplan"), ("Dokumentation", "dokumentation"), ("Übergabe", "uebergabe"),
        ("Visiten", "visiten"), ("Aufgaben", "aufgaben"), ("Kalender", "kalender"), ("Therapie", "therapie"), ("Ausgangsliste", "ausgangsliste"),
    ];

    private static long Wechsel(Hauptfenster f, string bereich)
    {
        var uhr = Stopwatch.StartNew();
        f.BereichWaehlen(bereich);
        Application.DoEvents();
        return uhr.ElapsedMilliseconds;
    }

    [Fact]
    public void Hauptansichten_mit_23_Bewohnern_je_Bereich() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "EF" });
        Assert.All(new[] { "R1", "R2", "R3" }, r => Assert.Equal(23, u.Speicher.AlleBewohner().Count(b => b.Wohnbereich == r)));
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        var zeiten = new List<string> { $"Bildschirm: {Screen.PrimaryScreen?.Bounds.Width}x{Screen.PrimaryScreen?.Bounds.Height}" };
        foreach (var (breite, hoehe) in new[] { (1920, 1080), (1366, 768) })
        {
            f.WindowState = FormWindowState.Normal;
            f.Bounds = new Rectangle(0, 0, breite, hoehe);
            Application.DoEvents();
            string groesse = $"{breite}x{hoehe}";
            zeiten.Add($"{groesse}: Fenster tatsächlich {f.Width}x{f.Height}");
            var erste = new Dictionary<string, long>();
            foreach (var (bereich, datei) in Ansichten)
            {
                erste[bereich] = Wechsel(f, bereich);
                if (bereich == "Übersicht") Assert.True(erste[bereich] < 5000, $"Übersicht braucht {erste[bereich]} ms");   // grobe Grenze, Richtwert 1 s
                Hilfe.Foto(f, $"109_{groesse}_{datei}");
            }
            // Zweiter Durchgang: der gewöhnliche Wechsel, wenn man schon einmal dort war (Ziel unter 150 ms).
            foreach (var (bereich, _) in Ansichten)
                zeiten.Add($"{groesse}: {bereich} {erste[bereich]} ms, wieder {Wechsel(f, bereich)} ms");
        }
        Messung.An = true;   // dieselbe Messung wie mit „--messen“: bis zur fertigen Anzeige, in messung.log
        try
        {
            f.BereichWaehlen("Aufgaben");
            f.BereichWaehlen("Übersicht");
            Assert.Equal("Übersicht", Messung.Werte[^1].Bereich);
            Assert.Contains("Übersicht", File.ReadAllText(Pfade.ImDatenordner(Messung.Datei)));
        }
        finally { Messung.An = false; }
        string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
        if (!string.IsNullOrWhiteSpace(ordner))
        {
            Directory.CreateDirectory(ordner);
            File.WriteAllLines(Path.Combine(ordner, "109_zeiten.txt"), zeiten);
        }
    });
}
