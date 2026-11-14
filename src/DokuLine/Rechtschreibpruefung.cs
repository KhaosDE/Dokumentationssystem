using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using DokuLine.Kern;

namespace DokuLine;

// Rechtschreibung im Textfeld (Bauplan 49): rote Wellenlinie unter unbekannten Wörtern, Rechtsklick mit bis zu fünf
// Vorschlägen, „Ignorieren“ (nur diese Sitzung) und „Zum Fachwörterbuch hinzufügen“ (mit Recht). Geprüft wird mit der
// Rechtschreibprüfung von Windows; eigene Wörter (Namen, Medikamente, Fachwörter) filtert der Kern heraus. Nichts davon wird
// in Wörterbuchdateien von Windows geschrieben. Fehlt das deutsche Sprachpaket, läuft alles ohne Prüfung weiter.
public static class Rechtschreibpruefung
{
    // Je Thread ein Prüfer: COM-Objekte gehören zu ihrem Apartment (im Programm gibt es nur den Fenster-Thread).
    [ThreadStatic] private static IRechtschreibung? pruefer;
    [ThreadStatic] private static bool versucht;
    private static HashSet<string> eigene = new(StringComparer.CurrentCultureIgnoreCase);
    private static DateTime eigeneStand = DateTime.MinValue;

    public static HashSet<string> Ignoriert { get; } = new(StringComparer.CurrentCultureIgnoreCase);

    // Zum Testen ersetzbar.
    public static Func<IRechtschreibung?> Erstellen { get; set; } = () => WindowsRechtschreibung.Erstellen("de-DE");

    public static IRechtschreibung? Pruefer
    {
        get
        {
            if (!versucht) { versucht = true; pruefer = Erstellen(); }
            return pruefer;
        }
    }

    public static void Zuruecksetzen() { versucht = false; pruefer = null; eigeneStand = DateTime.MinValue; }

    public static bool Verfuegbar => Pruefer is not null;

    // Die eigenen Wörter höchstens alle fünf Minuten neu (Namen und Medikamente ändern sich selten).
    public static HashSet<string> EigeneWoerter(Speicher speicher)
    {
        if (DateTime.Now - eigeneStand > TimeSpan.FromMinutes(5)) { eigene = speicher.EigeneWoerter(); eigeneStand = DateTime.Now; }
        return eigene;
    }

    public static void EigeneNeu() => eigeneStand = DateTime.MinValue;

    // Ein Fehler der Prüfung stört nie das Schreiben: dann eben ohne Wellenlinie.
    public static List<Schreibfehler> Pruefen(Speicher speicher, string text)
    {
        if (Pruefer is not IRechtschreibung p || text.Trim().Length == 0) return [];
        try { return RechtschreibRegeln.Filtern(text, p.Fehler(text), EigeneWoerter(speicher), Ignoriert); }
        catch (Exception e) when (e is COMException or InvalidComObjectException or InvalidCastException) { return []; }
    }

    public static IReadOnlyList<string> Vorschlaege(string wort)
    {
        try { return Pruefer?.Vorschlaege(wort) ?? []; }
        catch (Exception e) when (e is COMException or InvalidComObjectException or InvalidCastException) { return []; }
    }

    public static Unterstreichung Anbinden(TextBox feld, Speicher speicher, Label? hinweis = null) => new(feld, speicher, hinweis);
}

// Hängt sich an ein Textfeld: prüft kurz nach dem Tippen, zeichnet nach dem Zeichnen des Felds die Wellenlinien.
public sealed class Unterstreichung : NativeWindow
{
    private const int WmPaint = 0x000F;
    private readonly TextBox feld;
    private readonly Speicher speicher;
    private readonly Label? hinweis;
    private readonly System.Windows.Forms.Timer warten = new() { Interval = 400 };

    public List<Schreibfehler> Fehler { get; private set; } = [];

    public Unterstreichung(TextBox feld, Speicher speicher, Label? hinweis)
    {
        this.feld = feld;
        this.speicher = speicher;
        this.hinweis = hinweis;
        if (feld.IsHandleCreated) AssignHandle(feld.Handle);
        feld.HandleCreated += (_, _) => AssignHandle(feld.Handle);
        feld.HandleDestroyed += (_, _) => ReleaseHandle();
        feld.TextChanged += (_, _) => { warten.Stop(); warten.Start(); };
        feld.KeyPress += (_, e) => Autokorrektur(e);
        feld.MouseDown += (_, e) => { if (e.Button == MouseButtons.Right) MenueBauen(e.Location); };
        feld.Disposed += (_, _) => warten.Dispose();
        warten.Tick += (_, _) => { warten.Stop(); Pruefen(); };
    }

    public void Pruefen()
    {
        if (feld.IsDisposed) return;
        Fehler = Rechtschreibpruefung.Pruefen(speicher, feld.Text);
        if (hinweis is not null)
        {
            hinweis.Text = RechtschreibRegeln.HinweisText(Fehler.Count) ?? "";
            hinweis.Visible = Fehler.Count > 0;
        }
        feld.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WmPaint && Fehler.Count > 0 && !feld.IsDisposed) Zeichnen();
    }

    private void Zeichnen()
    {
        using var g = Graphics.FromHwnd(feld.Handle);
        using var stift = new Pen(Color.FromArgb(200, 30, 30), 1);
        int zeile = feld.Font.Height;
        foreach (var f in Fehler)
        {
            if (f.Start + f.Laenge > feld.TextLength) continue;
            var a = feld.GetPositionFromCharIndex(f.Start);
            var b = feld.GetPositionFromCharIndex(f.Start + f.Laenge - 1);
            int ende = b.Y == a.Y ? b.X + TextRenderer.MeasureText(feld.Text.Substring(f.Start + f.Laenge - 1, 1), feld.Font, Size.Empty, TextFormatFlags.NoPadding).Width
                : feld.ClientSize.Width - 2;
            int y = a.Y + zeile - 2;
            if (y > feld.ClientSize.Height) continue;
            var punkte = new List<Point>();
            for (int x = a.X, i = 0; x <= ende; x += 2, i++) punkte.Add(new Point(x, y + (i % 2 == 0 ? 0 : 2)));
            if (punkte.Count > 1) g.DrawLines(stift, [.. punkte]);
        }
    }

    // Rechtsklick auf ein unbekanntes Wort: Vorschläge, Ignorieren, Fachwörterbuch. Sonst das übliche Menü des Felds.
    private void MenueBauen(Point stelle)
    {
        int index = feld.GetCharIndexFromPosition(stelle);
        var f = Fehler.FirstOrDefault(x => index >= x.Start && index < x.Start + x.Laenge);
        feld.ContextMenuStrip?.Dispose();
        feld.ContextMenuStrip = null;
        if (f is null || Rechtschreibpruefung.Pruefer is not IRechtschreibung p) return;
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        foreach (var v in Rechtschreibpruefung.Vorschlaege(f.Wort).Take(RechtschreibRegeln.Vorschlaege))
            menue.Items.Add(v, null, (_, _) => Ersetzen(f, v)).Font = Stil.Fett;
        if (menue.Items.Count == 0) menue.Items.Add("keine Vorschläge").Enabled = false;
        menue.Items.Add(new ToolStripSeparator());
        menue.Items.Add("Ignorieren", null, (_, _) => { Rechtschreibpruefung.Ignoriert.Add(f.Wort); Pruefen(); });
        var fach = menue.Items.Add("Zum Fachwörterbuch hinzufügen", null, (_, _) =>
        {
            try { speicher.FachwortHinzufuegen(f.Wort, Recht.Kuerzel); Rechtschreibpruefung.EigeneNeu(); Pruefen(); }
            catch (Eingabefehler e) { MessageBox.Show(feld.FindForm(), string.Join("\n", e.Fehler), "Fachwörterbuch"); }
        });
        fach.Enabled = Recht.Grund(speicher, Funktionen.FachwoerterVerwalten) is null;
        feld.ContextMenuStrip = menue;
    }

    private void Ersetzen(Schreibfehler f, string neu)
    {
        if (f.Start + f.Laenge > feld.TextLength || feld.Text.Substring(f.Start, f.Laenge) != f.Wort) return;
        feld.Select(f.Start, f.Laenge);
        feld.Paste(neu);   // anders als SelectedText bleibt so Strg+Z möglich
    }

    // Wahlweise: nach einem Leerzeichen das Wort davor aus der Liste typischer Vertipper ersetzen.
    private void Autokorrektur(KeyPressEventArgs e)
    {
        if (e.KeyChar != ' ' || !speicher.AutokorrekturAn(Recht.Kuerzel)) return;
        int ende = feld.SelectionStart;
        int anfang = ende;
        while (anfang > 0 && char.IsLetter(feld.Text[anfang - 1])) anfang--;
        if (ende - anfang < 2 || RechtschreibRegeln.Korrektur(feld.Text[anfang..ende]) is not string richtig) return;
        feld.Select(anfang, ende - anfang);
        feld.Paste(richtig);
    }
}

// Die Rechtschreibprüfung von Windows (ISpellChecker, ab Windows 8). Ohne deutsches Sprachpaket: null.
public sealed class WindowsRechtschreibung : IRechtschreibung
{
    private readonly ISpellChecker pruefer;

    private WindowsRechtschreibung(ISpellChecker pruefer) => this.pruefer = pruefer;

    public static WindowsRechtschreibung? Erstellen(string sprache)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 2)) return null;
        try
        {
            var fabrik = (ISpellCheckerFactory)new SpellCheckerFactoryKlasse();
            if (fabrik.IsSupported(sprache, out int ja) != 0 || ja == 0) return null;
            return fabrik.CreateSpellChecker(sprache, out var p) == 0 && p is not null ? new WindowsRechtschreibung(p) : null;
        }
        catch (Exception e) when (e is COMException or InvalidCastException or NotSupportedException) { return null; }
    }

    public IReadOnlyList<(int Start, int Laenge)> Fehler(string text)
    {
        var liste = new List<(int, int)>();
        if (pruefer.Check(text, out var fehler) != 0 || fehler is null) return liste;
        while (fehler.Next(out var f) == 0 && f is not null)
        {
            if (f.GetStartIndex(out uint start) == 0 && f.GetLength(out uint laenge) == 0) liste.Add(((int)start, (int)laenge));
            Marshal.ReleaseComObject(f);
        }
        Marshal.ReleaseComObject(fehler);
        return liste;
    }

    public IReadOnlyList<string> Vorschlaege(string wort)
    {
        var liste = new List<string>();
        if (pruefer.Suggest(wort, out var aufzaehlung) != 0 || aufzaehlung is null) return liste;
        var eins = new string[1];
        while (liste.Count < RechtschreibRegeln.Vorschlaege && aufzaehlung.Next(1, eins, IntPtr.Zero) == 0) liste.Add(eins[0]);
        Marshal.ReleaseComObject(aufzaehlung);
        return liste;
    }

    public void Ignorieren(string wort) => pruefer.Ignore(wort);

    // Schnittstellen aus spellcheck.h (Windows SDK); Reihenfolge der Methoden wie dort, nur die benutzten am Anfang.
    [ComImport, Guid("7ab36653-1796-484b-bdfa-e74f1db7c1dc")]
    private class SpellCheckerFactoryKlasse;

    [ComImport, Guid("8e018a9d-2415-4677-bf08-794ea61f94bb"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellCheckerFactory
    {
        [PreserveSig] int GetSupportedLanguages(out IEnumString val);
        [PreserveSig] int IsSupported([MarshalAs(UnmanagedType.LPWStr)] string sprache, out int val);
        [PreserveSig] int CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string sprache, out ISpellChecker val);
    }

    [ComImport, Guid("b6fd0b71-e2bc-4653-8d05-f197e412770b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellChecker
    {
        [PreserveSig] int GetLanguageTag(out IntPtr val);
        [PreserveSig] int Check([MarshalAs(UnmanagedType.LPWStr)] string text, out IEnumSpellingError val);
        [PreserveSig] int Suggest([MarshalAs(UnmanagedType.LPWStr)] string wort, out IEnumString val);
        [PreserveSig] int Add([MarshalAs(UnmanagedType.LPWStr)] string wort);
        [PreserveSig] int Ignore([MarshalAs(UnmanagedType.LPWStr)] string wort);
    }

    [ComImport, Guid("803e3bd4-2828-4410-8290-418d1d73c762"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumSpellingError
    {
        [PreserveSig] int Next(out ISpellingError val);
    }

    [ComImport, Guid("b7c82d61-fbe8-4b47-9b27-6c0d2e0de0a3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellingError
    {
        [PreserveSig] int GetStartIndex(out uint val);
        [PreserveSig] int GetLength(out uint val);
    }
}
