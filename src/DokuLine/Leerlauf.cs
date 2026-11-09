using System.Runtime.InteropServices;
using System.Text;

namespace DokuLine;

// Wie lange es keine Eingabe (Maus, Tastatur) gab – für die automatische Sperre im Betrieb (Bauplan 37). Auf dem
// Terminalserver gilt das je Sitzung. Zum Testen ersetzbar. Dazu: offene Windows-Dialoge (Rückfragen, Datei wählen)
// beim Sperren ausblenden – sie sind keine Formulare des Programms und lägen sonst über der Sperrfläche.
internal static class Leerlauf
{
    [StructLayout(LayoutKind.Sequential)]
    private struct EingabeInfo
    {
        public uint Groesse;
        public uint Zeit;
    }

    private delegate bool FensterRueckruf(IntPtr fenster, IntPtr wert);

    // DllImport statt LibraryImport: Das Projekt erlaubt keinen unsicheren Code.
    [DllImport("user32.dll", EntryPoint = "GetLastInputInfo")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LetzteEingabe(ref EingabeInfo info);

    [DllImport("user32.dll", EntryPoint = "EnumThreadWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FensterDesThreads(uint thread, FensterRueckruf rueckruf, IntPtr wert);

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    private static extern uint ThreadKennung();

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int Klassenname(IntPtr fenster, StringBuilder name, int laenge);

    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Sichtbar(IntPtr fenster);

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Zeigen(IntPtr fenster, int art);

    private const int Verbergen = 0, Wiederzeigen = 5;
    private const string DialogKlasse = "#32770";

    public static Func<TimeSpan> Dauer { get; set; } = () =>
    {
        if (!OperatingSystem.IsWindows()) return TimeSpan.Zero;
        var info = new EingabeInfo { Groesse = (uint)Marshal.SizeOf<EingabeInfo>() };
        return LetzteEingabe(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.Zeit)) : TimeSpan.Zero;
    };

    // Sichtbare Windows-Dialoge dieses Threads ausblenden; zurück kommen die Fenster zum späteren Wiederzeigen.
    public static List<IntPtr> DialogeVerbergen()
    {
        var liste = new List<IntPtr>();
        if (!OperatingSystem.IsWindows()) return liste;
        FensterDesThreads(ThreadKennung(), (fenster, _) =>
        {
            var name = new StringBuilder(64);
            if (Sichtbar(fenster) && Klassenname(fenster, name, name.Capacity) > 0 && name.ToString() == DialogKlasse) liste.Add(fenster);
            return true;
        }, IntPtr.Zero);
        foreach (var f in liste) Zeigen(f, Verbergen);
        return liste;
    }

    public static void DialogeZeigen(IEnumerable<IntPtr> fenster)
    {
        if (!OperatingSystem.IsWindows()) return;
        foreach (var f in fenster) Zeigen(f, Wiederzeigen);
    }
}
