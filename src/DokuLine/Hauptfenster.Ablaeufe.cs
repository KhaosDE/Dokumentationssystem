using DokuLine.Kern;

namespace DokuLine;

// Abläufe über mehrere Bereiche: Mehrplatz (Änderungen anderer Rechner), Zähler, neuer Bericht, Aufgaben aus
// Visiten, Einstellungen öffnen, Demo-Daten zurücksetzen.
public sealed partial class Hauptfenster
{
    // ------------------------------------------------------------------ Mehrplatz

    private long? Datenstand()
    {
        try { return speicher.Datenstand(); }
        catch (SpeicherFehler) { return null; }   // Netz kurz weg: beim nächsten Mal wieder versuchen
    }

    // „R1 · Datenbank lokal“ unten in der Seitenleiste; der volle Pfad als Hinweis.
    public void ArbeitsplatzAnzeigen(bool erreichbar = true)
    {
        var werte = Einstellungen.Laden();
        bool gemeinsam = !string.Equals(Path.GetDirectoryName(speicher.Pfad), Pfade.Datenordner, StringComparison.OrdinalIgnoreCase);
        string ort = gemeinsam ? "gemeinsamer Ordner" : "Datenbank lokal";
        bool neustart = !string.Equals(Path.GetFullPath(Einstellungen.DatenbankPfad(werte)), Path.GetFullPath(speicher.Pfad), StringComparison.OrdinalIgnoreCase);
        Arbeitsplatz.Text = $"{werte.Bereich} · {ort}" + (erreichbar ? "" : " – nicht erreichbar") + (neustart ? "\nneuer Ort nach Neustart" : "");
        Arbeitsplatz.ForeColor = erreichbar ? Stil.TextMittel : Stil.Beschluss.Text;
        hinweis.SetToolTip(Arbeitsplatz, $"Bereich dieses Arbeitsplatzes: {werte.Bereich}\nDatenbank: {speicher.Pfad}");
        InfoAktualisieren();
        leiste.PerformLayout();
    }

    // Hat ein anderer Arbeitsplatz etwas gespeichert? Dann Zähler und die gezeigte Ansicht neu lesen.
    // Fenster zum Bearbeiten bleiben unberührt; ihr Speichern prüft selbst (Konflikt-Hinweis).
    public bool AenderungenVonAnderen()
    {
        long? stand = Datenstand();
        ArbeitsplatzAnzeigen(stand is not null);
        if (stand is null || stand == datenstand || IsDisposed) return false;
        datenstand = stand;
        AufgabenZaehlen();
        NachrichtenZaehlen();
        string schluessel = Schluessel(AktiverBereich);
        if (modulSeiten.TryGetValue(schluessel, out var modulSeite))
        {
            modulSeite.Zeigen();
            Kopf.TexteSetzen(modulSeite.KopfText, schluessel);
            return true;
        }
        switch (schluessel)
        {
            case "Dokumentation": Bewohner.NeuLesen(); break;
            case "Übersicht": Uebersicht.Aktualisieren(); break;
            case "Tagesplan": Tagesplan.Aktualisieren(); break;
            case "Übergabe": Uebergabe.Aktualisieren(); break;
            case "Visiten": Visiten.Aktualisieren(); break;
            case "Auswertung": Auswertung.Aktualisieren(); break;
            case "Qualität": Qualitaet.Aktualisieren(filterBehalten: true); break;
            case "Archiv": Archiv.Aktualisieren(); break;
            case "Hilfeplangespräche": Hilfeplan.Aktualisieren(); break;
            case "Bestellung": Bestellung.Aktualisieren(); break;
            case "Medikamentenausgabe": Ausgabe.Aktualisieren(); break;
            case "Aufgaben": Aufgaben.Aktualisieren(); break;
            case "Nachrichten": Nachrichten.Aktualisieren(); break;
            case "Kalender": Kalender.Aktualisieren(); break;
            case "Therapie": Therapie.NeuLesen(); break;
        }
        return true;
    }

    private void StatusNeu()
    {
        if (IsDisposed) return;
        if (AktiverBereich == "Übersicht") Uebersicht.Aktualisieren();
        else if (AktiverBereich == "Dokumentation") Bewohner.NeuLesen();
    }

    // Zähler offener Aufgaben in der Seitenleiste (rot, wenn etwas überfällig ist).
    public void AufgabenZaehlen()
    {
        var jetzt = DateTime.Now;
        speicher.StandardaufgabenErzeugen(jetzt);   // zu Schichtbeginn die Standardaufgaben
        var offen = speicher.Aufgaben(jetzt, bereich: Arbeitsbereich.Gewaehlt);
        int ueberfaellig = offen.Count(a => AufgabenRegeln.Ueberfaellig(a, jetzt));
        Navigation.Single(n => n.Text == "Aufgaben").ZaehlerSetzen(offen.Count, ueberfaellig > 0,
            $"{offen.Count} offen" + (ueberfaellig > 0 ? $", {ueberfaellig} überfällig" : ""));
    }

    // Zähler ungelesener Nachrichten des angemeldeten Benutzers (Seitenleiste und Kuvert in der Kopfzeile).
    public void NachrichtenZaehlen()
    {
        var ich = speicher.Kollege(Recht.Kuerzel);
        int n = ich is null ? 0 : speicher.UngeleseneNachrichten(ich.Kuerzel);
        Navigation.Single(e => e.Text == "Nachrichten").ZaehlerSetzen(n > 0 ? n : null, false, $"{n} ungelesen");
        Kopf.NachrichtenSetzen(ich is null ? null : n);
    }

    // Den ältesten eigenen Entwurf fortsetzen (öffnet „Neuer Bericht“ mit dem Entwurf).
    public Bericht? EntwurfFortsetzen()
    {
        var entwurf = speicher.Entwuerfe(Recht.Kuerzel).FirstOrDefault(e => e.Art == EntwurfRegeln.Bericht);
        return entwurf is null ? null : NeuerBericht(entwurf.BewohnerId);
    }

    // Neuer Bericht für einen Bewohner (aus Akte oder Liste); danach seine Berichte in der Dokumentation.
    public Bericht? NeuerBericht(long bewohnerId)
    {
        if (speicher.Bewohner(bewohnerId) is not Kern.Bewohner b) return null;
        using var formular = new BerichtFormular(speicher, b);
        if (Dialog.Zeigen(formular, this) != DialogResult.OK || formular.Gespeichert is not Bericht bericht) return null;
        BereichWaehlen("Dokumentation");
        Bewohner.DokumentationZeigen(bericht.BewohnerId);
        if (formular.Folgeschritte.Count > 0) Bewohner.FolgeschritteAusfuehren(bericht, [.. formular.Folgeschritte]);
        return bericht;
    }

    // Aufgabe aus einer Visite ausführen. Facharzttermin: Datum, Uhrzeit, Ort (Kalender,
    // auf Wunsch Fahrt). Medikation: fehlt das Medikament in der Datenbank, zuerst „Neues Medikament“
    // (Name vorbelegt); dann die Akte beim Reiter „Medikamentenplan“ und dort das passende Formular.
    // Ist es gespeichert, ist die Aufgabe erledigt. true = erledigt.
    public bool VisitenAufgabe(Aufgabe a)
    {
        if (speicher.VisitenAufgabe(a) is not { } info) return false;
        if (VisitenAblauf.Sperrgrund(speicher, info.Aktion) is string grund)
        {
            MessageBox.Show(this, grund, "Visiten", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        bool erledigt;
        if (info.Aktion == VisitenAktion.FacharztTermin)
        {
            using var fenster = new FacharztTerminFenster(speicher, a, info.Eintrag);
            erledigt = Dialog.Zeigen(fenster, this) == DialogResult.OK;
        }
        else
        {
            var anordnung = info.Anordnung!;
            var medikament = speicher.VisitenMedikament(anordnung);
            if (medikament is null)
            {
                using var anlegen = new MedikamentFenster(speicher, null);
                anlegen.Wirkstoff.Text = anordnung.Medikament;
                if (Dialog.Zeigen(anlegen, this) != DialogResult.OK || anlegen.Gespeichert is not Medikament neu) return false;
                medikament = neu;
            }
            BereichWaehlen("Dokumentation");
            Bewohner.AkteZeigen(info.Eintrag.BewohnerId);
            Bewohner.Akte.Reiter.Waehlen("Medikamentenplan");
            erledigt = Bewohner.Akte.Medikation.AusVisite(info.Visite, anordnung, medikament);
            if (erledigt)
            {
                try { speicher.AufgabeErledigt(a.Id, Recht.Kuerzel, DateTime.Now); }
                catch (Eingabefehler) { erledigt = false; }   // schon erledigt (anderer Arbeitsplatz)
            }
        }
        AufgabenZaehlen();
        if (AktiverBereich == "Visiten") Visiten.Aktualisieren();
        return erledigt;
    }

    // Einstellungen als Seite, auf Wunsch gleich bei einer Gruppe (z. B. „Wohnbereich“ für das Logo).
    public bool EinstellungenOeffnen(string? gruppe = null)
    {
        BereichWaehlen("Einstellungen");
        if (gruppe is not null) Einstellungsseite.GruppeWaehlen(gruppe);
        return true;
    }

    private void UebersichtNeu()
    {
        if (AktiverBereich == "Übersicht" && !IsDisposed) Uebersicht.Aktualisieren();
    }

    // Alle Bewohner und Bilder weg, Demo-Daten neu laden – mit Rückfrage.
    public bool DemoZuruecksetzen(bool nachfragen = true)
    {
        bool gemeinsam = !string.Equals(Path.GetDirectoryName(speicher.Pfad), Pfade.Datenordner, StringComparison.OrdinalIgnoreCase);
        if (nachfragen && MessageBox.Show(this,
                "Alle Änderungen und hinterlegten Bilder werden gelöscht und die ausgedachten Demo-Daten neu geladen." +
                (gemeinsam ? "\n\nDie Datenbank liegt im gemeinsamen Ordner: Das gilt für alle Arbeitsplätze." : "") + "\n\nFortfahren?",
                "Demo-Daten zurücksetzen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            != DialogResult.Yes)
            return false;
        speicher.DemoZuruecksetzen();
        BereichWaehlen("Dokumentation");
        return true;
    }
}
