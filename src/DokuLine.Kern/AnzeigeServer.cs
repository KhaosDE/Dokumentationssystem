using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace DokuLine.Kern;

// Kleiner, nur lesender Webserver für die Anzeige im Wohnbereich (Regel 6: nur LAN). Er beantwortet nur „GET“,
// nur Anfragen aus dem eigenen Netz, höchstens acht gleichzeitig, jede in höchstens fünf Sekunden; alles andere
// wird abgewiesen oder die Verbindung geschlossen. Keine Verbindung nach außen.
public sealed class AnzeigeServer : IDisposable
{
    private const int KopfGrenze = 8192, Gleichzeitig = 8;
    private readonly TcpListener hoerer;
    private readonly CancellationTokenSource ende = new();
    private readonly SemaphoreSlim plaetze = new(Gleichzeitig);
    private readonly Func<string, (int Status, string Html)> seite;

    public int Port { get; }

    // Starten; ist der Port belegt oder gesperrt, kommt statt des Servers ein Text für die Oberfläche.
    public static AnzeigeServer? Starten(int port, Func<string, (int Status, string Html)> seite, out string? fehler)
    {
        fehler = null;
        try { return new AnzeigeServer(port, seite); }
        catch (SocketException e)
        {
            fehler = $"Die Anzeige ließ sich auf Port {port} nicht starten ({e.SocketErrorCode}). Ist der Port schon belegt?";
            return null;
        }
    }

    // Die IPv4-Adressen dieses Rechners im eigenen Netz (zum Abtippen am Fernseher); nur gelesen, nichts verbunden.
    public static List<string> EigeneAdressen() =>
        [.. NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(u => u.Address))
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork && ImLan(a))
            .Select(a => a.ToString()).Distinct()];

    // Die Adresse, die man am Fernseher öffnet (Rechnername oder Adresse im LAN).
    public static string Adresse(string rechner, int port, string bereich, string code) =>
        $"http://{rechner}:{port}{AnzeigeRegeln.Pfad}{Uri.EscapeDataString(bereich)}?code={code}";

    // Nur Adressen aus dem eigenen Netz (private Bereiche, Link-lokal, der Rechner selbst).
    public static bool ImLan(IPAddress a)
    {
        if (a.IsIPv4MappedToIPv6) a = a.MapToIPv4();
        if (IPAddress.IsLoopback(a)) return true;
        if (a.AddressFamily == AddressFamily.InterNetworkV6) return a.IsIPv6LinkLocal || a.IsIPv6UniqueLocal;
        if (a.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = a.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
    }

    // seite: Ziel der Anfrage („/anzeige/R3?code=…“) → Status und HTML. adresse: Vorgabe alle Netzwerkkarten.
    public AnzeigeServer(int port, Func<string, (int Status, string Html)> seite, IPAddress? adresse = null)
    {
        this.seite = seite;
        hoerer = new TcpListener(adresse ?? IPAddress.Any, port);
        hoerer.Start();
        Port = ((IPEndPoint)hoerer.LocalEndpoint).Port;
        _ = Task.Run(Annehmen);
    }

    private async Task Annehmen()
    {
        while (!ende.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await hoerer.AcceptTcpClientAsync(ende.Token); }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException) { return; }
            if (!plaetze.Wait(0)) { client.Dispose(); continue; }
            _ = Task.Run(async () =>
            {
                try { await Bedienen(client); }
                finally { plaetze.Release(); client.Dispose(); }
            });
        }
    }

    private async Task Bedienen(TcpClient client)
    {
        try
        {
            using var frist = CancellationTokenSource.CreateLinkedTokenSource(ende.Token);
            frist.CancelAfter(TimeSpan.FromSeconds(5));
            if (client.Client.RemoteEndPoint is not IPEndPoint von || !ImLan(von.Address)) return;
            var strom = client.GetStream();
            string? zeile = await ErsteZeile(strom, frist.Token);
            var teile = zeile?.Split(' ');
            (int status, string html) = teile is not { Length: 3 } || !teile[2].StartsWith("HTTP/1.", StringComparison.Ordinal)
                ? (400, "")
                : teile[0] != "GET" ? (405, "") : Seite(teile[1]);
            await Schreiben(strom, status, html, frist.Token);
        }
        catch (Exception e) when (e is IOException or OperationCanceledException or SocketException or ObjectDisposedException)
        {
            // Verbindung abgebrochen oder zu langsam: nichts zu tun
        }
    }

    private (int, string) Seite(string ziel)
    {
        try { return seite(ziel); }
        catch (Exception e) when (e is not OutOfMemoryException) { return (500, ""); }   // der Server läuft weiter
    }

    // Liest den Kopf der Anfrage bis zur Leerzeile (höchstens 8 KB) und gibt die erste Zeile zurück.
    private static async Task<string?> ErsteZeile(NetworkStream strom, CancellationToken abbruch)
    {
        var puffer = new byte[KopfGrenze];
        int gelesen = 0;
        while (gelesen < puffer.Length)
        {
            int n = await strom.ReadAsync(puffer.AsMemory(gelesen), abbruch);
            if (n == 0) return null;
            gelesen += n;
            string bisher = Encoding.ASCII.GetString(puffer, 0, gelesen);
            if (bisher.Contains("\r\n\r\n", StringComparison.Ordinal)) return bisher[..bisher.IndexOf("\r\n", StringComparison.Ordinal)];
        }
        return null;
    }

    private static async Task Schreiben(NetworkStream strom, int status, string html, CancellationToken abbruch)
    {
        var inhalt = Encoding.UTF8.GetBytes(html);
        string text = status switch { 200 => "OK", 403 => "Forbidden", 404 => "Not Found", 405 => "Method Not Allowed", 500 => "Internal Server Error", _ => "Bad Request" };
        string kopf = $"HTTP/1.1 {status} {text}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {inhalt.Length}\r\n" +
                      "Cache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\n" +
                      "Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'\r\n" +
                      (status == 405 ? "Allow: GET\r\n" : "") + "Connection: close\r\n\r\n";
        await strom.WriteAsync(Encoding.ASCII.GetBytes(kopf), abbruch);
        await strom.WriteAsync(inhalt, abbruch);
    }

    public void Dispose()
    {
        ende.Cancel();
        hoerer.Stop();
        ende.Dispose();
    }
}
