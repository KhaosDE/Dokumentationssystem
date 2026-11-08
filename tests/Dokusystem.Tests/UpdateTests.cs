using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dokusystem.Kern;
using Dokusystem.Update;
using A = Dokusystem.Update.Aktualisierung;

namespace Dokusystem.Tests;

// Tests für das Selbst-Update – ohne echtes Netz (GitHub wird nachgebaut).
[Collection("Datenordner")]
public sealed class UpdateTests : IDisposable
{
    private static readonly byte[] Inhalt = "neue Version"u8.ToArray();
    private static readonly string Summe = Convert.ToHexStringLower(SHA256.HashData(Inhalt));

    private readonly Datenordner daten = new();
    private readonly Hilfsordner ordner = new();
    private readonly string exe;
    private readonly List<string> gestartet = [];

    public UpdateTests()
    {
        exe = ordner.Datei(A.Datei, "alte Version"u8.ToArray());
    }

    public void Dispose()
    {
        ordner.Dispose();
        daten.Dispose();
    }

    private static JsonNode Release(string tag = "build-30", string? exeUrl = null, bool mitSumme = true)
    {
        var assets = new JsonArray
        {
            new JsonObject { ["name"] = A.Datei, ["size"] = Inhalt.Length, ["browser_download_url"] = exeUrl ?? A.DownloadPraefix + tag + "/" + A.Datei },
        };
        if (mitSumme)
            assets.Add(new JsonObject { ["name"] = A.Pruefsumme, ["size"] = 90, ["browser_download_url"] = A.DownloadPraefix + tag + "/" + A.Pruefsumme });
        return new JsonObject { ["tag_name"] = tag, ["assets"] = assets };
    }

    private static Action<string, string, Func<long, long, bool>?> Laden(byte[]? inhalt = null) => (url, ziel, fortschritt) =>
    {
        inhalt ??= Inhalt;
        if (url.EndsWith(".sha256", StringComparison.Ordinal))
        {
            File.WriteAllText(ziel, $"{Summe}  {A.Datei}\n");
            return;
        }
        File.WriteAllBytes(ziel, inhalt);
        if (fortschritt is not null && !fortschritt(inhalt.Length, inhalt.Length))
            throw new UpdateFehler("Vom Benutzer abgebrochen.");
    };

    private string Dateien() => string.Join(",", Directory.GetFiles(ordner.Ordner).Select(Path.GetFileName).Order());
    private static string LogText() => File.ReadAllText(Pfade.ImDatenordner(A.Log));

    // ----- Angebot -----

    [Fact]
    public void Nur_neuere_Builds()
    {
        Assert.Null(A.AngebotAusRelease(Release("build-30"), 30));
        Assert.Null(A.AngebotAusRelease(Release("build-29"), 30));
        Assert.Null(A.AngebotAusRelease(new JsonObject { ["tag_name"] = "v1.0" }, 30));
        var angebot = A.AngebotAusRelease(Release("build-31"), 30)!;
        Assert.Equal(31, angebot.Build);
        Assert.EndsWith("/build-31/Dokumentationssystem.exe", angebot.Url);
    }

    [Fact]
    public void Fremde_Adressen_und_fehlende_Pruefsumme()
    {
        Assert.Throws<UpdateFehler>(() => A.AngebotAusRelease(Release("build-31", exeUrl: "https://example.com/x.exe"), 30));
        Assert.Throws<UpdateFehler>(() => A.AngebotAusRelease(Release("build-31", mitSumme: false), 30));
        Assert.Throws<UpdateFehler>(() => A.Anfrage("https://example.com/"));
    }

    [Fact]
    public void Pruefsumme_lesen()
    {
        Assert.Equal(Summe, A.PruefsummeLesen($"{Summe.ToUpperInvariant()}  {A.Datei}\n"));
        Assert.Throws<UpdateFehler>(() => A.PruefsummeLesen("kaputt"));
    }

    // ----- Ablauf -----

    [Fact]
    public void Aktualisiert_tauscht_und_startet()
    {
        Assert.True(A.Aktualisieren(exe, 30, _ => Release("build-31"), Laden(), starten: gestartet.Add));
        Assert.Equal(Inhalt, File.ReadAllBytes(exe));
        Assert.Equal("alte Version", File.ReadAllText(exe + ".alt"));
        Assert.Equal([exe], gestartet);
        A.Aufraeumen(exe);
        Assert.Equal(A.Datei, Dateien());
        string log = LogText();
        Assert.Contains("Neue Version gefunden: Build 31", log);
        Assert.Contains("Prüfsumme stimmt", log);
        Assert.Contains("Build 30 → Build 31", log);
    }

    [Fact]
    public void Aktuell_nichts_zu_tun()
    {
        Assert.False(A.Aktualisieren(exe, 31, _ => Release("build-31"), Laden(), starten: gestartet.Add));
        Assert.Equal("alte Version", File.ReadAllText(exe));
        Assert.Contains("Build 31 ist aktuell", LogText());
    }

    [Fact]
    public void Falsche_Pruefsumme_laesst_alte_Version_stehen()
    {
        Assert.Throws<UpdateFehler>(() =>
            A.Aktualisieren(exe, 30, _ => Release("build-31"), Laden("beschaedigt"u8.ToArray()), starten: gestartet.Add));
        Assert.Equal("alte Version", File.ReadAllText(exe));
        Assert.Empty(gestartet);
        Assert.Equal(A.Datei, Dateien());
    }

    private sealed class AbbrechendesFenster : IFortschritt
    {
        public bool Geschlossen { get; private set; }
        public bool Zeige(string text, double? anteil) => anteil == 0.0;   // erster Aufruf ja, beim Laden: abbrechen
        public void Schliessen() => Geschlossen = true;
    }

    [Fact]
    public void Abbrechen_im_Fenster_laesst_alte_Version_stehen()
    {
        var fenster = new AbbrechendesFenster();
        Assert.Throws<UpdateFehler>(() =>
            A.Aktualisieren(exe, 30, _ => Release("build-31"), Laden(), () => fenster, gestartet.Add));
        Assert.True(fenster.Geschlossen);
        Assert.Equal("alte Version", File.ReadAllText(exe));
        Assert.Empty(gestartet);
    }

    [Fact]
    public void Log_bleibt_kurz()
    {
        for (int i = 0; i < A.LogZeilen + 20; i++) A.Protokoll($"Zeile {i}");
        var zeilen = File.ReadAllLines(Pfade.ImDatenordner(A.Log));
        Assert.Equal(A.LogZeilen, zeilen.Length);
        Assert.EndsWith($"Zeile {A.LogZeilen + 19}", zeilen[^1]);
    }

    [Fact]
    public void Log_der_Python_Version_wird_fortgeschrieben()
    {
        File.WriteAllText(Pfade.ImDatenordner(A.Log), "05.11.2026 12:00:00  Build 4 ist aktuell.\n");
        A.Protokoll("weiter");
        var zeilen = File.ReadAllLines(Pfade.ImDatenordner(A.Log));
        Assert.Equal(2, zeilen.Length);
        Assert.Contains("Build 4", zeilen[0]);
    }

    // ----- Abschalten -----

    [Fact]
    public void Gruende_ohne_Update()
    {
        Assert.Equal("gestartet mit --kein-update", A.GrundOhneUpdate(["--kein-update"], 5, true));
        Assert.Equal("Selbsttest", A.GrundOhneUpdate(["--selbsttest"], 5, true));
        Assert.Equal("Entwicklerversion", A.GrundOhneUpdate([], 0, true));
        Assert.Equal("nicht als Windows-Programm gestartet", A.GrundOhneUpdate([], 5, false));
        // Aus dem Testlauf heraus (keine .exe, Build 0): nie ins Netz
        Assert.False(A.BeimStart([], 0, null));
    }

    [Fact]
    public void Einstellung_schaltet_ab()
    {
        Assert.Null(A.GrundOhneUpdate([], 5, alsWindowsProgramm: true));
        Einstellungen.Speichern(new EinstellungsWerte(AutoUpdate: false));
        Assert.Equal("in den Einstellungen abgeschaltet", A.GrundOhneUpdate([], 5, alsWindowsProgramm: true));
    }

    [Fact]
    public void Gerade_aktualisiert_fragt_nicht_gleich_wieder()
    {
        Environment.SetEnvironmentVariable(A.Frisch, "1");
        try { Assert.Equal("gerade aktualisiert", A.GrundOhneUpdate([], 5, true)); }
        finally { Environment.SetEnvironmentVariable(A.Frisch, null); }
    }

    [Fact]
    public void Aufraeumen_entfernt_die_alte_Programmdatei_auch_der_Python_Version()
    {
        File.WriteAllText(exe + ".alt", "Python-Programmdatei");
        File.WriteAllText(exe + ".neu", "halb geladen");
        A.Aufraeumen(exe);
        Assert.Equal(A.Datei, Dateien());
    }
}

// Nur das Update-Projekt darf ins Netz, und nur zu den Releases dieses Repositorys.
public class NetzNurImUpdateTests
{
    private static IEnumerable<string> Quelltexte(string projekt) =>
        Directory.EnumerateFiles(Path.Combine(Repo.Wurzel, "src", projekt), "*.cs", SearchOption.AllDirectories)
                 .Where(d => !d.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                             !d.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void Update_spricht_nur_mit_den_Releases_dieses_Repositorys()
    {
        var adressen = Quelltexte("Dokusystem.Update")
            .SelectMany(d => Regex.Matches(File.ReadAllText(d), "https?://[^\\s\"')]+").Select(m => m.Value))
            .ToHashSet();
        string[] erlaubt =
        [
            "https://api.github.com/repos/KhaosDE/Dokumentationssystem/releases/latest",
            "https://github.com/KhaosDE/Dokumentationssystem/releases/download/",
        ];
        Assert.NotEmpty(adressen);
        Assert.All(adressen, a => Assert.Contains(erlaubt, e => a.StartsWith(e, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("Dokusystem.Kern")]
    [InlineData("Dokusystem")]
    [InlineData("Dokusystem.Bausteine")]
    [InlineData("Dokusystem.Modul.Ausgangsliste")]
    public void Programm_selbst_geht_nie_ins_Netz(string projekt)
    {
        var verboten = new Regex(@"\b(HttpClient|WebClient|WebRequest|Socket|TcpClient|System\.Net\.Http)");
        var adresse = new Regex("https?://[^\\s\"')]+");
        var funde = Quelltexte(projekt)
            .SelectMany(d => verboten.Matches(File.ReadAllText(d)).Select(m => $"{Path.GetFileName(d)}: {m.Value}")
                // Adressen nur zum Übergeben an den Browser, und nur in ExterneAdressen.cs (Regel 6).
                .Concat(Path.GetFileName(d) == "ExterneAdressen.cs" ? [] : adresse.Matches(File.ReadAllText(d)).Select(m => $"{Path.GetFileName(d)}: {m.Value}")))
            .ToList();
        Assert.Empty(funde);
    }

    // Einzige eingebettete Webansicht: das Nachschlagen im Formular „Medikament“ (Regel 6, Schritt 21).
    [Fact]
    public void Webansicht_nur_zum_Nachschlagen()
    {
        // Verwendung der Bibliothek (nicht das Wort in Kommentaren oder Texten).
        var webansicht = new Regex(@"Microsoft\.Web\.WebView2|\bCoreWebView2\w*|\bnew\s+WebView2\b");
        var funde = new[] { "Dokusystem.Kern", "Dokusystem", "Dokusystem.Bausteine", "Dokusystem.Modul.Ausgangsliste", "Dokusystem.Update" }
            .SelectMany(Quelltexte)
            .Where(d => webansicht.IsMatch(File.ReadAllText(d)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Equal(["Nachschlageansicht.cs"], funde);
        string ansicht = File.ReadAllText(Path.Combine(Repo.Wurzel, "src", "Dokusystem", "Nachschlageansicht.cs"));
        Assert.Contains("Nachschlagen.Zulaessig", ansicht);   // jede Navigation wird geprüft
        var projekte = Directory.EnumerateFiles(Path.Combine(Repo.Wurzel, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(d => File.ReadAllText(d).Contains("Microsoft.Web.WebView2"))
            .Select(Path.GetFileName);
        Assert.Equal(["Dokusystem.csproj"], projekte);
    }

    [Fact]
    public void Browser_Adressen_nur_aus_der_Liste()
    {
        string datei = Path.Combine(Repo.Wurzel, "src", "Dokusystem.Kern", "ExterneAdressen.cs");
        var gefunden = Regex.Matches(File.ReadAllText(datei), "https?://[^\\s\"')]+").Select(m => m.Value).ToList();
        Assert.Equal(ExterneAdressen.Alle.Order(), gefunden.Order());
        Assert.All(ExterneAdressen.Alle, a => Assert.StartsWith("https://", a));
    }
}
