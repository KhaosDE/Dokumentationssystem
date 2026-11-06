using System.Runtime.ExceptionServices;
using Dokusystem.Kern;

namespace Dokusystem.Oberflaeche.Tests;

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

// Leerer Datenordner mit Demo-Daten und offenem Speicher für einen Test.
public sealed class Umgebung : IDisposable
{
    private readonly string? vorher = Environment.GetEnvironmentVariable(Pfade.Umlenkung);
    public string Ordner { get; } = Directory.CreateTempSubdirectory("dokusystem-ui-").FullName;
    public Speicher Speicher { get; }

    public Umgebung()
    {
        Environment.SetEnvironmentVariable(Pfade.Umlenkung, Ordner);
        Speicher = new Speicher();
        Speicher.DemoBeimErstenStart();
    }

    public Bewohner Demo(string nachname) => Speicher.AlleBewohner().First(b => b.Nachname == nachname);

    public void Dispose()
    {
        Speicher.Dispose();
        Environment.SetEnvironmentVariable(Pfade.Umlenkung, vorher);
        try { Directory.Delete(Ordner, recursive: true); } catch (IOException) { }
    }
}
