using System.Runtime.InteropServices;

namespace DokuLine;

// Wie lange es keine Eingabe (Maus, Tastatur) gab – für die automatische Sperre im Betrieb (Bauplan 37). Auf dem
// Terminalserver gilt das je Sitzung. Zum Testen ersetzbar.
internal static class Leerlauf
{
    [StructLayout(LayoutKind.Sequential)]
    private struct EingabeInfo
    {
        public uint Groesse;
        public uint Zeit;
    }

    // DllImport statt LibraryImport: Das Projekt erlaubt keinen unsicheren Code.
    [DllImport("user32.dll", EntryPoint = "GetLastInputInfo")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LetzteEingabe(ref EingabeInfo info);

    public static Func<TimeSpan> Dauer { get; set; } = () =>
    {
        if (!OperatingSystem.IsWindows()) return TimeSpan.Zero;
        var info = new EingabeInfo { Groesse = (uint)Marshal.SizeOf<EingabeInfo>() };
        return LetzteEingabe(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.Zeit)) : TimeSpan.Zero;
    };
}
