using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DokuLine.Kern;
using DokuLine.Update;
using A = DokuLine.Update.Aktualisierung;

namespace DokuLine.Tests;

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
        Assert.EndsWith("/build-31/DokuLine.exe", angebot.Url);
    }

    // Übergang beim Umbenennen (Schritt 26a1): beide Dateinamen und beide Adressen.
    private static JsonNode ReleaseMit(string tag, string praefix, params string[] namen) => new JsonObject
    {
        ["tag_name"] = tag,
        ["assets"] = new JsonArray([.. namen.SelectMany(n => new JsonNode[]
        {
            new JsonObject { ["name"] = n, ["size"] = Inhalt.Length, ["browser_download_url"] = praefix + tag + "/" + n },
            new JsonObject { ["name"] = n + ".sha256", ["size"] = 90, ["browser_download_url"] = praefix + tag + "/" + n + ".sha256" },
        })]),
    };

    [Fact]
    public void Beide_Dateinamen_und_beide_Adressen()
    {
        // Älteres Release (vor der Umbenennung): nur Dokumentationssystem.exe unter der alten Adresse.
        var alt = A.AngebotAusRelease(ReleaseMit("build-31", A.DownloadPraefixAlt, A.DateiAlt), 30)!;
        Assert.Equal(A.DownloadPraefixAlt + "build-31/Dokumentationssystem.exe", alt.Url);
        Assert.EndsWith("Dokumentationssystem.exe.sha256", alt.PruefsummeUrl);
        // Übergangs-Release: beide Dateien – DokuLine.exe wird genommen.
        var beide = A.AngebotAusRelease(ReleaseMit("build-32", A.DownloadPraefixAlt, A.DateiAlt, A.Datei), 30)!;
        Assert.EndsWith("/build-32/DokuLine.exe", beide.Url);
        // Nach der Umbenennung des Repositorys: neue Adresse.
        var neu = A.AngebotAusRelease(ReleaseMit("build-33", A.DownloadPraefix, A.Datei), 30)!;
        Assert.Equal("https://github.com/KhaosDE/DokuLine/releases/download/build-33/DokuLine.exe", neu.Url);
        // Fremde Adresse und fehlende Prüfsumme bleiben verboten.
        Assert.Throws<UpdateFehler>(() => A.AngebotAusRelease(ReleaseMit("build-34", "https://github.com/jemand/anderes/releases/download/", A.Datei), 30));
        var ohneSumme = ReleaseMit("build-35", A.DownloadPraefix, A.Datei);
        ((JsonArray)ohneSumme["assets"]!).RemoveAt(1);
        Assert.Throws<UpdateFehler>(() => A.AngebotAusRelease(ohneSumme, 30));
        A.Anfrage(A.ApiNeueste).Dispose();
        A.Anfrage(A.ApiNeuesteAlt).Dispose();
        A.Anfrage(A.DownloadPraefixAlt + "build-31/Dokumentationssystem.exe").Dispose();
        Assert.Throws<UpdateFehler>(() => A.Anfrage("https://api.github.com/repos/jemand/anderes/releases/latest"));
    }

    [Fact]
    public void Fragt_unter_dem_alten_Namen_wenn_der_neue_nicht_antwortet()
    {
        var gefragt = new List<string>();
        JsonNode? Holen(string url)
        {
            gefragt.Add(url);
            if (url == A.ApiNeueste) throw new HttpRequestException("404 (Not Found)");
            return ReleaseMit("build-31", A.DownloadPraefixAlt, A.DateiAlt, A.Datei);
        }
        Assert.True(A.Aktualisieren(exe, 30, Holen, Laden(), starten: gestartet.Add));
        Assert.Equal([A.ApiNeueste, A.ApiNeuesteAlt], gefragt);
        Assert.Contains("frage unter dem alten Namen", LogText());
        // Antwortet die neue Adresse, wird die alte nicht gefragt.
        gefragt.Clear();
        Assert.NotNull(A.NeuesteRelease(url => { gefragt.Add(url); return ReleaseMit("build-31", A.DownloadPraefix, A.Datei); }));
        Assert.Equal([A.ApiNeueste], gefragt);
    }

    [Fact]
    public void Alte_Programmdatei_behaelt_ihren_Namen()
    {
        string alteExe = ordner.Datei(A.DateiAlt, "alte Version"u8.ToArray());
        Assert.True(A.Aktualisieren(alteExe, 30, _ => ReleaseMit("build-31", A.DownloadPraefixAlt, A.DateiAlt, A.Datei), Laden(), starten: gestartet.Add));
        Assert.Equal(Inhalt, File.ReadAllBytes(alteExe));
        Assert.Equal([alteExe], gestartet);
        A.Aufraeumen(alteExe);
        Assert.Equal($"{A.Datei},{A.DateiAlt}", Dateien());
        Assert.Equal("alte Version", File.ReadAllText(exe));   // DokuLine.exe ist die Datei aus dem Konstruktor, unberührt
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
        var adressen = Quelltexte("DokuLine.Update")
            .SelectMany(d => Regex.Matches(File.ReadAllText(d), "https?://[^\\s\"')]+").Select(m => m.Value))
            .ToHashSet();
        string[] erlaubt =
        [
            "https://api.github.com/repos/KhaosDE/DokuLine/releases/latest",
            "https://github.com/KhaosDE/DokuLine/releases/download/",
            // bis zur Umbenennung des Repositorys und für ältere Installationen (Schritt 26a1)
            "https://api.github.com/repos/KhaosDE/Dokumentationssystem/releases/latest",
            "https://github.com/KhaosDE/Dokumentationssystem/releases/download/",
        ];
        Assert.NotEmpty(adressen);
        Assert.All(adressen, a => Assert.Contains(erlaubt, e => a.StartsWith(e, StringComparison.Ordinal)));
    }

    // Übergang (Schritt 26a1): Jedes Release enthält DokuLine.exe und für ältere Installationen
    // dieselbe Datei als Dokumentationssystem.exe, beide mit Prüfsumme.
    [Fact]
    public void Release_enthaelt_beide_Dateinamen()
    {
        string workflow = File.ReadAllText(Path.Combine(Repo.Wurzel, ".github", "workflows", "tests-und-exe.yml"));
        Assert.Contains("gh release create $tag publish\\DokuLine.exe publish\\DokuLine.exe.sha256", workflow);
        Assert.Contains("publish\\Dokumentationssystem.exe publish\\Dokumentationssystem.exe.sha256", workflow);
        Assert.Contains("Copy-Item publish\\DokuLine.exe publish\\Dokumentationssystem.exe", workflow);
        Assert.Contains("-FilePath \"publish\\DokuLine.exe\" -ArgumentList \"--selbsttest\"", workflow);
    }

    [Theory]
    [InlineData("DokuLine.Kern")]
    [InlineData("DokuLine")]
    [InlineData("DokuLine.Bausteine")]
    [InlineData("DokuLine.Modul.Ausgangsliste")]
    public void Programm_selbst_geht_nie_ins_Netz(string projekt)
    {
        var verboten = new Regex(@"\b(HttpClient|WebClient|WebRequest|Socket|TcpClient|System\.Net\.Http)");
        var adresse = new Regex("https?://[^\\s\"')]+");
        var funde = Quelltexte(projekt)
            .Where(d => !(projekt == "DokuLine.Kern" && Path.GetFileName(d) == AnzeigeDatei))   // eigene Prüfung unten
            .SelectMany(d => verboten.Matches(File.ReadAllText(d)).Select(m => $"{Path.GetFileName(d)}: {m.Value}")
                // Adressen nur zum Übergeben an den Browser, und nur in ExterneAdressen.cs (Regel 6).
                .Concat(Path.GetFileName(d) == "ExterneAdressen.cs" ? [] : adresse.Matches(File.ReadAllText(d)).Select(m => $"{Path.GetFileName(d)}: {m.Value}")))
            .ToList();
        Assert.Empty(funde);
    }

    // Ausnahme (Regel 6, Ergänzung 09.11.2026): die Anzeige der laufenden Therapien im LAN – nur in dieser Datei, nur
    // annehmend (kein Verbindungsaufbau nach außen), nur Anfragen aus dem eigenen Netz, nur die eigene Adresse.
    private const string AnzeigeDatei = "AnzeigeServer.cs";

    [Fact]
    public void Anzeige_nimmt_nur_an_und_nur_aus_dem_LAN()
    {
        string text = File.ReadAllText(Path.Combine(Repo.Wurzel, "src", "DokuLine.Kern", AnzeigeDatei));
        Assert.Contains("new TcpListener(", text);
        Assert.Contains("!ImLan(von.Address)", text);
        Assert.DoesNotMatch(new Regex(@"\b(HttpClient|WebClient|WebRequest|UdpClient|Dns\.|Connect\w*\(|new\s+TcpClient|new\s+Socket)"), text);
        var adressen = new Regex("https?://[^\\s\"]+").Matches(text).Select(m => m.Value).ToList();
        Assert.Single(adressen);
        Assert.StartsWith("http://{rechner}:{port}", adressen[0]);   // nur die eigene Adresse für den Fernseher
        // Kein anderes Projekt startet einen Server.
        var andere = new[] { "DokuLine.Kern", "DokuLine", "DokuLine.Bausteine", "DokuLine.Modul.Ausgangsliste" }.SelectMany(Quelltexte)
            .Where(d => Path.GetFileName(d) != AnzeigeDatei && Regex.IsMatch(File.ReadAllText(d), @"\b(TcpListener|HttpListener)\b"))
            .Select(Path.GetFileName);
        Assert.Empty(andere);
    }

    // Einzige eingebettete Webansicht: das Nachschlagen im Formular „Medikament“ (Regel 6, Schritt 21).
    [Fact]
    public void Webansicht_nur_zum_Nachschlagen()
    {
        // Verwendung der Bibliothek (nicht das Wort in Kommentaren oder Texten).
        var webansicht = new Regex(@"Microsoft\.Web\.WebView2|\bCoreWebView2\w*|\bnew\s+WebView2\b");
        var funde = new[] { "DokuLine.Kern", "DokuLine", "DokuLine.Bausteine", "DokuLine.Modul.Ausgangsliste", "DokuLine.Update" }
            .SelectMany(Quelltexte)
            .Where(d => webansicht.IsMatch(File.ReadAllText(d)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.Equal(["Nachschlageansicht.cs"], funde);
        string ansicht = File.ReadAllText(Path.Combine(Repo.Wurzel, "src", "DokuLine", "Nachschlageansicht.cs"));
        Assert.Contains("Nachschlagen.Zulaessig", ansicht);   // jede Navigation wird geprüft
        var projekte = Directory.EnumerateFiles(Path.Combine(Repo.Wurzel, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(d => File.ReadAllText(d).Contains("Microsoft.Web.WebView2"))
            .Select(Path.GetFileName);
        Assert.Equal(["DokuLine.csproj"], projekte);
    }

    [Fact]
    public void Browser_Adressen_nur_aus_der_Liste()
    {
        string datei = Path.Combine(Repo.Wurzel, "src", "DokuLine.Kern", "ExterneAdressen.cs");
        var gefunden = Regex.Matches(File.ReadAllText(datei), "https?://[^\\s\"')]+").Select(m => m.Value).ToList();
        Assert.Equal(ExterneAdressen.Alle.Order(), gefunden.Order());
        Assert.All(ExterneAdressen.Alle, a => Assert.StartsWith("https://", a));
    }
}
