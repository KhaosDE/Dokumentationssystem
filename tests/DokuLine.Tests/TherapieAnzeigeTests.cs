using System.Net;
using System.Net.Sockets;
using System.Text;
using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan 29o: Therapie-Anzeige für den Fernseher – Inhalt je Wohnbereich, Namen, Zugangscode, nur LAN, nur lesend.
[Collection("Datenordner")]
public sealed class TherapieAnzeigeTests : IDisposable
{
    private static readonly DateTime Jetzt = DateTime.Today.AddHours(10).AddMinutes(15);
    private static readonly AnzeigeFarben Farben = new("#fff", "#fff", "#000", "#666", "#06c", "#ddd");
    private readonly Datenordner daten = new();
    private readonly Speicher s;

    public TherapieAnzeigeTests()
    {
        s = new Speicher { Uhr = () => Jetzt };
        s.DemoZuruecksetzen();
    }

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private List<AnzeigeTermin> Tag(string bereich, bool voll = false)
    {
        var bewohner = s.AlleBewohner().ToDictionary(b => b.Id!.Value);
        var heute = LaufendeRegeln.Heute(s.Angebote(), bewohner, s.TeilnahmeEintraege(DateOnly.FromDateTime(Jetzt)), Jetzt, bereich);
        return AnzeigeRegeln.Tag(heute, voll);
    }

    private static Bewohner B(string vorname, string nachname) =>
        new() { Vorname = vorname, Nachname = nachname, Zimmer = "1", Geburtsdatum = new(1980, 1, 1), Aufnahmedatum = new(2020, 1, 1) };

    [Fact]
    public void Nur_Teilnehmer_des_Bereichs_mit_kurzem_Namen()
    {
        var r1 = Tag("R1");
        Assert.NotEmpty(r1);
        var namenR1 = s.AlleBewohner().Where(b => b.Wohnbereich == "R1").Select(b => AnzeigeRegeln.Name(b, false)).ToHashSet();
        Assert.All(r1, t => Assert.All(t.Namen, n => Assert.Contains(n, namenR1)));
        Assert.All(r1, t => Assert.NotEmpty(t.Namen));   // ohne Teilnehmer aus dem Bereich keine Therapie
        Assert.Equal("Lia A.", AnzeigeRegeln.Name(B("Lia", "Ahorn"), false));
        Assert.Equal("Lia Ahorn", AnzeigeRegeln.Name(B("Lia", "Ahorn"), true));
        Assert.Contains(Tag("R1", voll: true).SelectMany(t => t.Namen), n => n.Contains(' ') && !n.EndsWith('.'));
        Assert.Empty(Tag("R9"));   // ein Bereich ohne Bewohner zeigt nichts
    }

    [Fact]
    public void Jetzt_und_Gleich_in_der_naechsten_Stunde()
    {
        var tag = new List<AnzeigeTermin>
        {
            new("Morgenrunde", new(9, 0), new(10, 0), "Raum 1", ["A B."]),
            new("Spaziergang", new(10, 0), new(11, 0), "Eingang", ["C D."]),
            new("Küchenteam", new(11, 0), new(12, 0), "Küche", ["E F."]),
            new("Sport", new(11, 30), new(12, 0), "Halle", ["G H."]),
        };
        var uhr = new TimeOnly(10, 15);
        Assert.Equal(["Spaziergang"], AnzeigeRegeln.Jetzt(tag, uhr).Select(t => t.Name));
        Assert.Equal(["Küchenteam"], AnzeigeRegeln.Gleich(tag, uhr).Select(t => t.Name));   // 11:30 ist mehr als 60 Minuten weg
        Assert.Equal(["Küchenteam", "Sport"], AnzeigeRegeln.Gleich(tag, new TimeOnly(10, 30)).Select(t => t.Name));
    }

    [Fact]
    public void Abgesagt_steht_nicht_da()
    {
        var bewohner = s.AlleBewohner().ToDictionary(b => b.Id!.Value);
        var tag = DateOnly.FromDateTime(Jetzt);
        var heute = LaufendeRegeln.Heute(s.Angebote(), bewohner, s.TeilnahmeEintraege(tag), Jetzt, "R1");
        var t = heute.First(x => x.Teilnehmer.Count > 0);
        var p = t.Teilnehmer[0];
        string name = AnzeigeRegeln.Name(p.Bewohner, false);
        var abgesagt = t with { Teilnehmer = [p with { Status = TafelStatus.Abgesagt }] };
        Assert.Empty(AnzeigeRegeln.Tag([abgesagt], false));
        Assert.Equal([name], AnzeigeRegeln.Tag([t with { Teilnehmer = [p] }], false)[0].Namen);
    }

    [Fact]
    public void Seite_mit_Code_ohne_Code_und_unbekannt()
    {
        var stand = new AnzeigeStand();
        var termin = new AnzeigeTermin("Holz<werkstatt>", new(10, 0), new(11, 0), "Keller & Hof", ["Lia A."]);
        stand.Setzen(DateOnly.FromDateTime(Jetzt), new Dictionary<string, List<AnzeigeTermin>> { ["R3"] = [termin] });
        var werte = new AnzeigeWerte { Codes = new Dictionary<string, string> { ["R3"] = "ABC234" } };
        var (status, html) = AnzeigeRegeln.Antwort("/anzeige/R3?code=abc234", stand, werte, Farben, Jetzt);
        Assert.Equal(200, status);
        Assert.Contains("Holz&lt;werkstatt&gt;", html);   // Text wird nie als HTML gelesen
        Assert.Contains("Keller &amp; Hof", html);
        Assert.Contains("Lia A.", html);
        Assert.Contains("10:15 Uhr", html);
        Assert.Contains("http-equiv=\"refresh\" content=\"60\"", html);
        Assert.DoesNotContain("<script", html);
        Assert.Equal(403, AnzeigeRegeln.Antwort("/anzeige/R3", stand, werte, Farben, Jetzt).Status);
        Assert.Equal(403, AnzeigeRegeln.Antwort("/anzeige/R3?code=ABC235", stand, werte, Farben, Jetzt).Status);
        Assert.Equal(404, AnzeigeRegeln.Antwort("/anzeige/R1?code=ABC234", stand, werte, Farben, Jetzt).Status);
        Assert.Equal(404, AnzeigeRegeln.Antwort("/", stand, werte, Farben, Jetzt).Status);
        // Am nächsten Tag zeigt ein alter Stand nichts.
        var morgen = AnzeigeRegeln.Antwort("/anzeige/R3?code=ABC234", stand, werte, Farben, Jetzt.AddDays(1));
        Assert.DoesNotContain("Holz", morgen.Html);
    }

    [Fact]
    public void Nur_Adressen_aus_dem_eigenen_Netz()
    {
        foreach (var a in new[] { "127.0.0.1", "10.1.2.3", "172.16.0.5", "172.31.255.1", "192.168.178.20", "169.254.1.1", "::1", "fe80::1", "fd00::5", "::ffff:192.168.1.2" })
            Assert.True(AnzeigeServer.ImLan(IPAddress.Parse(a)), a);
        foreach (var a in new[] { "8.8.8.8", "172.32.0.1", "172.15.0.1", "192.169.0.1", "2001:db8::1", "::ffff:8.8.8.8" })
            Assert.False(AnzeigeServer.ImLan(IPAddress.Parse(a)), a);
    }

    [Fact]
    public void Code_und_Einstellungen()
    {
        var codes = Enumerable.Range(0, 50).Select(_ => AnzeigeRegeln.CodeErzeugen()).ToList();
        Assert.All(codes, c => Assert.Matches("^[A-HJ-NP-Z2-9]{6}$", c));
        Assert.True(codes.Distinct().Count() > 45);
        Assert.False(AnzeigeEinstellungen.Laden().An);   // Vorgabe: aus
        Assert.Equal(AnzeigeRegeln.StandardPort, AnzeigeEinstellungen.Laden().Port);
        var werte = AnzeigeEinstellungen.MitCodes(new AnzeigeWerte { An = true, Port = 8090, VollerName = true }, ["R1", "R2"]);
        AnzeigeEinstellungen.Speichern(werte);
        var gelesen = AnzeigeEinstellungen.Laden();
        Assert.True(gelesen.An);
        Assert.Equal(8090, gelesen.Port);
        Assert.True(gelesen.VollerName);
        Assert.Equal(werte.Codes["R1"], gelesen.Codes["R1"]);
        Assert.Equal(werte.Codes["R1"], AnzeigeEinstellungen.MitCodes(gelesen, ["R1", "R3"]).Codes["R1"]);   // vorhandene bleiben
        Assert.Throws<Eingabefehler>(() => AnzeigeEinstellungen.Speichern(werte with { Port = 80 }));
        Assert.Equal("http://PC-1:8090/anzeige/R%201?code=ABC234", AnzeigeServer.Adresse("PC-1", 8090, "R 1", "ABC234"));
    }

    [Fact]
    public async Task Server_beantwortet_nur_GET_und_kennt_den_Code()
    {
        var stand = new AnzeigeStand();
        stand.Setzen(DateOnly.FromDateTime(DateTime.Now), new Dictionary<string, List<AnzeigeTermin>>
        {
            ["R2"] = [new("Spaziergang", new(0, 0), new(23, 59), "Eingang", ["Ute Z."])],
        });
        var werte = new AnzeigeWerte { Codes = new Dictionary<string, string> { ["R2"] = "QWERTZ" } };
        using var server = new AnzeigeServer(0, ziel => AnzeigeRegeln.Antwort(ziel, stand, werte, Farben, DateTime.Now), IPAddress.Loopback);
        using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };
        var gut = await http.GetAsync("/anzeige/R2?code=QWERTZ");
        Assert.Equal(HttpStatusCode.OK, gut.StatusCode);
        Assert.Contains("Ute Z.", await gut.Content.ReadAsStringAsync());
        Assert.Equal("no-store", gut.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/anzeige/R2?code=FALSCH")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/etc/passwd")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await http.PostAsync("/anzeige/R2?code=QWERTZ", new StringContent("x"))).StatusCode);

        // Unsinn ohne gültige erste Zeile und ein zu langer Kopf: 400.
        using (var roh = new TcpClient())
        {
            await roh.ConnectAsync(IPAddress.Loopback, server.Port);
            var strom = roh.GetStream();
            await strom.WriteAsync(Encoding.ASCII.GetBytes("HALLO\r\n\r\n"));
            var antwort = new byte[64];
            int n = await strom.ReadAsync(antwort);
            Assert.StartsWith("HTTP/1.1 400", Encoding.ASCII.GetString(antwort, 0, n));
        }
        using (var roh = new TcpClient())
        {
            await roh.ConnectAsync(IPAddress.Loopback, server.Port);
            var strom = roh.GetStream();
            await strom.WriteAsync(Encoding.ASCII.GetBytes("GET /" + new string('a', 9000)));
            var antwort = new byte[64];
            int n = await strom.ReadAsync(antwort);
            Assert.StartsWith("HTTP/1.1 400", Encoding.ASCII.GetString(antwort, 0, n));   // nach 8 KB ist Schluss
        }
    }
}
