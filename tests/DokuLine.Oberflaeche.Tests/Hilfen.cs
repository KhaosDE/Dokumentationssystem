using System.Runtime.ExceptionServices;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Alle Oberflächentests nacheinander: Sie lenken den Datenordner (Umgebungsvariable) um.
[CollectionDefinition("Oberflaeche", DisableParallelization = true)]
public sealed class OberflaechenSammlung;

public static class Hilfe
{
    static Hilfe() => Application.EnableVisualStyles();

    // Windows Forms erwartet einen STA-Thread; xUnit liefert keinen. Also: eigener Thread.
    public static void Sta(Action aktion)
    {
        Exception? fehler = null;
        var thread = new Thread(() =>
        {
            try { aktion(); }
            catch (Exception e) { fehler = e; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (fehler is not null) ExceptionDispatchInfo.Capture(fehler).Throw();
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
