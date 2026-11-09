using DokuLine.Kern;
using DokuLine.Update;

namespace DokuLine;

// Einstiegspunkt der .exe.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Grundeinstellungen für die Oberfläche (Schriften, Skalierung) – vor dem ersten Fenster.
        ApplicationConfiguration.Initialize();
        Stil.SymbolFuerAlleFenster();   // Programmsymbol für alle Fenster

        if (args.Contains("--selbsttest")) return Selbsttest.Ausfuehren();
        Messung.An = args.Contains("--messen");

        // Betriebsmodus (Bauplan 37): betrieb.json im Programmordner. Fehlerhaft → nicht starten (nie still in die Demo).
        try { Betrieb.Setzen(Betrieb.Laden(AppContext.BaseDirectory)); }
        catch (SpeicherFehler e)
        {
            MessageBox.Show(e.Message, Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        if (Betrieb.Aktiv && args.Contains("--sicherung")) return Betriebsaufgaben.Sichern();
        if (Betrieb.Aktiv && args.Contains("--update-einspielen")) return Betriebsaufgaben.UpdateEinspielen();

        // Übergang: Daten aus %APPDATA%\Dokumentationssystem einmal nach %APPDATA%\DokuLine
        // kopieren – vor allem anderen, was in den Datenordner schreibt.
        try
        {
            if (!Betrieb.Aktiv && Pfade.AltenOrdnerUebernehmen() is string kopiert) Aktualisierung.Protokoll(kopiert);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Die Daten aus dem bisherigen Ordner „{Pfade.AlterDatenordner}“ konnten nicht übernommen werden:\n\n{e.Message}\n\n" +
                "Der bisherige Ordner ist unverändert. Bitte das Programm schließen und neu starten.", Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        // Zuerst das Selbst-Update: Ist eine neue Version geladen und gestartet, endet dieses Programm.
        // Im Betrieb nie: Updates spielt der Verwalter nachts ein („Update vorbereiten …“, --update-einspielen).
        if (!Betrieb.Aktiv && Aktualisierung.BeimStart(args, Programmversion.Build, () => new Fortschrittsfenster())) return 0;

        // Auffangnetz: Ein unerwarteter Fehler zeigt eine verständliche Meldung statt des
        // technischen Fehlerfensters von Windows.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => MessageBox.Show(
            "Ein unerwarteter Fehler ist aufgetreten:\n\n" + e.Exception.Message +
            "\n\nDie letzte Aktion wurde möglicherweise nicht ausgeführt.",
            Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);

        Speicher speicher;
        string? hinweis;
        try
        {
            // Mehrplatz: Datenbank am eingestellten Ort (lokal oder gemeinsamer Ordner).
            (speicher, hinweis) = Speicher.Oeffnen(Einstellungen.Laden());
            Modulverwaltung.Anmelden(speicher);
            if (Betrieb.Werte is BetriebWerte betrieb) speicher.ZugriffeAufraeumen(DateTime.Now, betrieb.ProtokollMonate);
            else speicher.DemoBeimErstenStart();
            foreach (string zeile in speicher.Startprotokoll) Aktualisierung.Protokoll(zeile);   // z. B. TST ergänzt
            string bereich = Einstellungen.Laden().Bereich;
            if (Kern.Bereiche.Umbenannt(bereich) != bereich) Einstellungen.Aendern(w => w with { Bereich = Kern.Bereiche.Umbenannt(bereich) });
        }
        catch (SpeicherFehler e)
        {
            MessageBox.Show(e.Message, Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        if (hinweis is not null)
            MessageBox.Show(hinweis, Programmversion.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _ = Nachschlageansicht.Laufzeit;
        BerichtRegeln.Zeiten = speicher.Schichtzeiten(Einstellungen.Laden().Bereich);   // Schichtzeiten des Bereichs // WebView2 vorhanden? Steht in den Einstellungen.
        // Erster Start im Betrieb: den Verwalter anlegen (mit Passwort).
        if (Betrieb.Aktiv && speicher.AlleMitarbeiter().Count == 0)
            using (var einrichten = new ErsteinrichtungFenster(speicher))
                if (einrichten.ShowDialog() != DialogResult.OK)
                {
                    speicher.Dispose();
                    return 0;
                }
        // Anmelden mit dem Kürzel (Demo ohne Passwort, Betrieb mit); „Beenden“ schließt das Programm.
        using (var anmelden = new AnmeldenFenster(speicher, Einstellungen.Laden().Kuerzel))
        {
            Stil.SymbolSetzen(anmelden);
            if (anmelden.ShowDialog() != DialogResult.OK || anmelden.Angemeldet is not Mitarbeiter angemeldet)
            {
                speicher.Dispose();
                return 0;
            }
            Einstellungen.Aendern(w => w with { Kuerzel = angemeldet.Kuerzel });
        }
        using (speicher)
        using (var fenster = new Hauptfenster(speicher))
        {
            fenster.BereichWaehlen(fenster.StartSeite);
            Application.Run(fenster);
        }
        return 0;
    }
}
