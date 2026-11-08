using System.Diagnostics;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26h: 23 Bewohner je Wohnbereich. Bildschirmfotos aller Hauptansichten bei
// 1920 × 1080 und 1366 × 768 zur Sichtprüfung, dazu die Zeit für den Aufbau jeder Ansicht
// (109_zeiten.txt neben den Fotos). Richtwert: Übersicht unter einer Sekunde.
[Collection("Oberflaeche")]
public class UebersichtlichkeitTests
{
    private static readonly (string Bereich, string Datei)[] Ansichten =
    [
        ("Übersicht", "uebersicht"), ("Tagesplan", "tagesplan"), ("Bewohner", "bewohner"), ("Übergabe", "uebergabe"),
        ("Visiten", "visiten"), ("Aufgaben", "aufgaben"), ("Kalender", "kalender"), ("Therapie", "therapie"), ("Ausgangsliste", "ausgangsliste"),
    ];

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
            foreach (var (bereich, datei) in Ansichten)
            {
                var uhr = Stopwatch.StartNew();
                f.BereichWaehlen(bereich);
                Application.DoEvents();
                uhr.Stop();
                zeiten.Add($"{groesse}: {bereich} {uhr.ElapsedMilliseconds} ms");
                if (bereich == "Übersicht") Assert.True(uhr.ElapsedMilliseconds < 5000, $"Übersicht braucht {uhr.ElapsedMilliseconds} ms");   // grobe Grenze, Richtwert 1 s
                Hilfe.Foto(f, $"109_{groesse}_{datei}");
            }
        }
        string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
        if (!string.IsNullOrWhiteSpace(ordner))
        {
            Directory.CreateDirectory(ordner);
            File.WriteAllLines(Path.Combine(ordner, "109_zeiten.txt"), zeiten);
        }
    });
}
