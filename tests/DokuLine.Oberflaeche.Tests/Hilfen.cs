using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Alle Oberflächentests nacheinander: Sie lenken den Datenordner (Umgebungsvariable) um.
[CollectionDefinition("Oberflaeche", DisableParallelization = true)]
public sealed class OberflaechenSammlung;

public static class Hilfe
{
    static Hilfe() => Application.EnableVisualStyles();

    // Kein Oberflächentest braucht annähernd so lange; wer länger läuft, wartet fast immer auf ein Dialogfenster.
    public static readonly TimeSpan Hoechstdauer = TimeSpan.FromMinutes(3);

    // Windows Forms erwartet einen STA-Thread; xUnit liefert keinen. Also: eigener Thread. Hängt er (offenes Dialogfenster,
    // das niemand schließt), schlägt der Test fehl, statt den ganzen Lauf aufzuhalten; als Hintergrund-Thread hält er das
    // Testprogramm am Ende nicht auf.
    // Hängt er doch, nennt die Meldung die offenen Fenster des Threads (Titel, Text) und schließt sie, damit der Thread endet
    // und nicht die folgenden Tests stört.
    public static void Sta(Action aktion)
    {
        Exception? fehler = null;
        uint kennung = 0;
        var thread = new Thread(() =>
        {
            kennung = ThreadKennung();
            try { aktion(); }
            catch (Exception e) { fehler = e; }
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(Hoechstdauer))
        {
            string offen = FensterSchliessen(kennung);
            thread.Join(TimeSpan.FromSeconds(10));
            throw new TimeoutException($"Der Test hängt seit {Hoechstdauer.TotalMinutes:0} Minuten – vermutlich wartet ein Dialogfenster auf eine Eingabe " +
                                       $"(im Test mit DialogErsatz beantworten). Offene Fenster: {offen}");
        }
        if (fehler is not null) ExceptionDispatchInfo.Capture(fehler).Throw();
    }

    private delegate bool FensterRueckruf(IntPtr fenster, IntPtr wert);

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    private static extern uint ThreadKennung();

    [DllImport("user32.dll", EntryPoint = "EnumThreadWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FensterDesThreads(uint thread, FensterRueckruf rueckruf, IntPtr wert);

    [DllImport("user32.dll", EntryPoint = "EnumChildWindows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnterFenster(IntPtr fenster, FensterRueckruf rueckruf, IntPtr wert);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int FensterText(IntPtr fenster, StringBuilder text, int laenge);

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int Klassenname(IntPtr fenster, StringBuilder name, int laenge);

    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Sichtbar(IntPtr fenster);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Senden(IntPtr fenster, uint nachricht, IntPtr w, IntPtr l);

    private static string Text(IntPtr fenster, bool klasse = false)
    {
        var s = new StringBuilder(512);
        _ = klasse ? Klassenname(fenster, s, s.Capacity) : FensterText(fenster, s, s.Capacity);
        return s.ToString();
    }

    private static string FensterSchliessen(uint kennung)
    {
        if (kennung == 0) return "unbekannt";
        var fenster = new List<IntPtr>();
        FensterDesThreads(kennung, (f, _) => { if (Sichtbar(f)) fenster.Add(f); return true; }, IntPtr.Zero);
        var texte = fenster.Select(f =>
        {
            var innen = new List<string>();
            UnterFenster(f, (k, _) => { if (Text(k, klasse: true) == "Static" && Text(k).Trim().Length > 0) innen.Add(Text(k).Trim()); return true; }, IntPtr.Zero);
            return $"„{Text(f)}“ ({Text(f, klasse: true)}){(innen.Count > 0 ? ": " + string.Join(" / ", innen) : "")}";
        }).ToList();
        const uint Schliessen = 0x0010;   // WM_CLOSE
        foreach (var f in fenster) Senden(f, Schliessen, IntPtr.Zero, IntPtr.Zero);
        return texte.Count == 0 ? "keine – vermutlich eine Endlosschleife" : string.Join("; ", texte);
    }

    // Fenster wirklich anzeigen: Windows Forms führt PerformClick nur bei sichtbaren Knöpfen aus.
    public static void Zeigen(Form fenster)
    {
        fenster.StartPosition = FormStartPosition.Manual;
        fenster.Location = new Point(0, 0);
        fenster.Show();
        Application.DoEvents();
    }

    // Ein ausgedachtes Testbild (Farbflächen, keine Person, kein echtes Logo).
    public static void Testbild(string pfad, int breite, int hoehe)
    {
        using var bild = new Bitmap(breite, hoehe);
        using (var g = Graphics.FromImage(bild))
        {
            g.Clear(Color.FromArgb(238, 241, 247));
            using var blau = new SolidBrush(Color.FromArgb(84, 128, 198));
            using var orange = new SolidBrush(Color.FromArgb(238, 166, 60));
            g.FillEllipse(orange, 4, 4, hoehe - 8, hoehe - 8);
            g.FillRectangle(blau, hoehe, hoehe / 3, breite - hoehe - 6, hoehe / 3);
        }
        bild.Save(pfad, System.Drawing.Imaging.ImageFormat.Png);
    }

    // Bildschirmfoto eines Fensters oder Steuerelements, nur wenn DOKUSYSTEM_BILDSCHIRMFOTOS gesetzt ist.
    public static void Foto(Control element, string name)
    {
        string? ordner = Environment.GetEnvironmentVariable("DOKUSYSTEM_BILDSCHIRMFOTOS");
        if (string.IsNullOrWhiteSpace(ordner)) return;
        Directory.CreateDirectory(ordner);
        if (element is Form fenster && !fenster.Visible) Zeigen(fenster);
        Application.DoEvents();
        using var bild = new Bitmap(element.Width, element.Height);
        element.DrawToBitmap(bild, new Rectangle(Point.Empty, element.Size));
        bild.Save(Path.Combine(ordner, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
    }
}

// Ersetzt Dialog.Zeigen für einen Test: Der Test füllt das Fenster aus, statt auf einen Menschen
// zu warten. Danach gilt wieder das Vorherige.
public sealed class DialogErsatz : IDisposable
{
    private readonly Func<Form, IWin32Window?, DialogResult> vorher = Dialog.Zeigen;
    public List<string> Titel { get; } = [];

    public DialogErsatz(Func<Form, DialogResult> aktion) =>
        Dialog.Zeigen = (fenster, _) => { Titel.Add(fenster.Text); return aktion(fenster); };

    public void Dispose() => Dialog.Zeigen = vorher;
}

// Leerer Datenordner mit Demo-Daten und offenem Speicher für einen Test.
public sealed class Umgebung : IDisposable
{
    private readonly string? vorher = Environment.GetEnvironmentVariable(Pfade.Umlenkung);
    public string Ordner { get; } = Directory.CreateTempSubdirectory("dokusystem-ui-").FullName;
    public Speicher Speicher { get; }

    public Umgebung()
    {
        Environment.SetEnvironmentVariable(Pfade.Umlenkung, Ordner);
        Arbeitsbereich.Gewaehlt = "";   // die Wahl der Übersicht gilt je Programmlauf; jeder Test beginnt ohne
        Speicher = new Speicher();
        Modulverwaltung.Anmelden(Speicher);
        Speicher.DemoBeimErstenStart();
        // Standardaufgaben (Schritt 25) hängen von der Uhrzeit ab: in den Tests aus, damit die Zahlen
        // gleich bleiben. Wer sie prüft, schaltet sie selbst ein.
        foreach (var v in Speicher.Vorlagen()) Speicher.VorlageAktiv(v.Id, false);
    }

    public Bewohner Demo(string nachname) => Speicher.AlleBewohner().First(b => b.Nachname == nachname);

    public void Dispose()
    {
        Speicher.Dispose();
        Environment.SetEnvironmentVariable(Pfade.Umlenkung, vorher);
        try { Directory.Delete(Ordner, recursive: true); } catch (IOException) { }
    }
}
