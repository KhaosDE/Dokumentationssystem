using DokuLine.Kern;

namespace DokuLine;

// Die Rückfragen zur HEB-Frist (Bauplan 52), gemeinsam für den Reiter „HEB“ der Akte und die Seite „HEB-Fristen“.
public static class HebFristAblauf
{
    private static bool Fehler(IWin32Window? fenster, Eingabefehler e, string titel)
    {
        MessageBox.Show(fenster, string.Join("\n", e.Fehler), titel, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    public static bool FaelligAendern(Speicher speicher, HebFristStand s, Func<string, string, string?> fragen, DateTime jetzt, IWin32Window? fenster)
    {
        if (fragen("Fälligkeit ändern", $"Nächster HEB fällig am (TT.MM.JJJJ, leer = {Datum.Text(s.FaelligAm)})") is not string text) return false;
        try
        {
            var am = text.Trim().Length == 0 && s.FaelligAm is DateOnly f ? f : Datum.Lesen(text, "Fällig am");
            if (fragen("Fälligkeit ändern", "Grund (z. B. „Schreiben des Bezirks vom …“)") is not string grund) return false;
            speicher.HebFaelligSetzen(s.Bewohner.Id!.Value, am, grund, Recht.Kuerzel, jetzt);
        }
        catch (FormatException e) { return Fehler(fenster, new Eingabefehler([e.Message]), "Fälligkeit ändern"); }
        catch (Eingabefehler e) { return Fehler(fenster, e, "Fälligkeit ändern"); }
        return true;
    }

    public static bool KeinHeb(Speicher speicher, HebFristStand s, Func<string, string, string?> fragen, DateTime jetzt, IWin32Window? fenster)
    {
        if (fragen("Kein HEB nötig", "Grund (z. B. „Auszug geplant zum …“)") is not string grund) return false;
        try { speicher.HebKeinerNoetig(s.Bewohner.Id!.Value, grund, Recht.Kuerzel, jetzt); }
        catch (Eingabefehler e) { return Fehler(fenster, e, "Kein HEB nötig"); }
        return true;
    }

    // Abgegeben am …; die nächste Fälligkeit wird vorgeschlagen (Bewilligung bzw. letzter Bogen plus Frist), „-“ = später eintragen.
    public static bool Abgegeben(Speicher speicher, HebFristStand s, Func<string, string, string?> fragen, DateTime jetzt, IWin32Window? fenster)
    {
        var heute = DateOnly.FromDateTime(jetzt);
        try
        {
            if (fragen("Abgegeben", $"Beim Kostenträger abgegeben am (TT.MM.JJJJ, leer = {Datum.Text(heute)})") is not string amText) return false;
            var am = amText.Trim().Length == 0 ? heute : Datum.Lesen(amText, "Abgegeben am");
            var vorschlag = speicher.HebNaechsteVorschlag(s.Bewohner.Id!.Value, am);
            if (fragen("Abgegeben", $"Nächster HEB fällig am (TT.MM.JJJJ, leer = {Datum.Text(vorschlag)}, „-“ = später eintragen)") is not string naechstesText)
                return false;
            DateOnly? naechste = naechstesText.Trim() == "-" ? null : naechstesText.Trim().Length == 0 ? vorschlag : Datum.Lesen(naechstesText, "Nächster HEB fällig am");
            speicher.HebAbgegeben(s.Bewohner.Id!.Value, am, naechste, Recht.Kuerzel, jetzt);
        }
        catch (FormatException e) { return Fehler(fenster, new Eingabefehler([e.Message]), "Abgegeben"); }
        catch (Eingabefehler e) { return Fehler(fenster, e, "Abgegeben"); }
        return true;
    }

    // Sperrgrund für „Abgegeben …“ (null = möglich).
    public static string? AbgegebenGesperrt(HebFristStand s) =>
        s.Zustand == HebFristZustand.KeinHeb ? "Für diesen Bewohner ist „kein HEB nötig“ eingetragen."
        : s.Stand == HebBogenStand.InArbeit ? "Der Bogen ist noch ein Entwurf." : null;
}
