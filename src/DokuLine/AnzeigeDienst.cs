using DokuLine.Kern;

namespace DokuLine;

// Die Anzeige der laufenden Therapien für den Fernseher: Der Stand je Wohnbereich wird jede Minute und nach
// Änderungen der Therapie neu gelesen – hier auf dem Fenster-Thread, weil der Speicher nicht für mehrere Threads
// gebaut ist; der Server liest nur diesen Stand. An oder aus nach den Einstellungen dieses Rechners (Vorgabe: aus).
public sealed class AnzeigeDienst : IDisposable
{
    private readonly Speicher speicher;
    private readonly System.Windows.Forms.Timer uhr = new() { Interval = 60_000 };
    private AnzeigeServer? server;
    private AnzeigeWerte werte = new();

    public AnzeigeStand Stand { get; } = new();
    public string? Fehler { get; private set; }
    public bool Laeuft => server is not null;
    public int? Port => server?.Port;

    public static AnzeigeFarben Farben { get; } = new(Css(Stil.Seite), Css(Stil.Flaeche), Css(Stil.TextNormal), Css(Stil.TextLeise),
        Css(Stil.Akzent), Css(Stil.KarteRand));

    private static string Css(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public AnzeigeDienst(Speicher speicher)
    {
        this.speicher = speicher;
        uhr.Tick += (_, _) => StandSetzen();
        speicher.TherapieGeaendert += StandSetzen;
    }

    // Nach dem Start und nach jeder Änderung der Einstellungen.
    public void Anwenden()
    {
        werte = AnzeigeEinstellungen.Laden();
        server?.Dispose();
        server = null;
        Fehler = null;
        uhr.Stop();
        // Im Betrieb aus: Auf dem Terminalserver liefe je Sitzung ein Server, und die Einstellung liegt im gemeinsamen Ordner,
        // den alle Benutzer ändern dürfen (das Recht „Anzeige verwalten“ wäre umgangen). Offener Punkt in STAND.md.
        if (!werte.An || Betrieb.Aktiv) return;
        StandSetzen();
        var fest = werte;
        server = AnzeigeServer.Starten(fest.Port, ziel => AnzeigeRegeln.Antwort(ziel, Stand, fest, Farben, DateTime.Now, Stil.DemoHinweis), out var fehler);
        Fehler = fehler;
        if (server is not null) uhr.Start();
    }

    // Die Termine eines Bereichs heute (auch für das Vollbild auf einem zweiten Bildschirm).
    public static List<AnzeigeTermin> TagFuer(Speicher speicher, string bereich, bool vollerName, DateTime jetzt)
    {
        var bewohner = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value);
        var heute = LaufendeRegeln.Heute(speicher.Angebote(), bewohner, speicher.TeilnahmeEintraege(DateOnly.FromDateTime(jetzt)), jetzt, bereich);
        return AnzeigeRegeln.Tag(heute, vollerName);
    }

    public void StandSetzen()
    {
        if (!werte.An) return;
        var jetzt = DateTime.Now;
        try
        {
            var neu = speicher.Wohnbereiche().Select(w => w.Name).Distinct()
                .ToDictionary(b => b, b => TagFuer(speicher, b, werte.VollerName, jetzt));
            Stand.Setzen(DateOnly.FromDateTime(jetzt), neu);
        }
        catch (SpeicherFehler)
        {
            // Datenbank gerade nicht lesbar (z. B. gemeinsamer Ordner weg): Der alte Stand bleibt bis zur nächsten Minute.
        }
    }

    public void Dispose()
    {
        speicher.TherapieGeaendert -= StandSetzen;
        uhr.Dispose();
        server?.Dispose();
    }
}
