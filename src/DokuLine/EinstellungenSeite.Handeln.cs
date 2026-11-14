using DokuLine.Kern;

namespace DokuLine;

// Was die Einstellungen übernehmen: Bereich, Schichtzeiten, Module, Ort der Datenbank, Vorlagen, Farbe, Logo.
public sealed partial class EinstellungenSeite
{
    // ------------------------------------------------------------------ Übernehmen

    public bool BereichUebernehmen()
    {
        if (laedt) return false;
        string neu = Kern.Bereiche.Bereinigen(Bereich.Text);
        if (neu == Einstellungen.Laden().Bereich) return true;
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        if (Kern.Bereiche.Pruefen(Bereich.Text) is string fehler) return FehlerZeigen(fehler);
        Einstellungen.Aendern(w => w with { Bereich = neu });
        var zeiten = speicher.Schichtzeiten(neu);
        BerichtRegeln.Zeiten = zeiten;
        laedt = true;
        (Frueh.Text, Spaet.Text, Nacht.Text) = (zeiten.Frueh.ToString("HH:mm"), zeiten.Spaet.ToString("HH:mm"), zeiten.Nacht.ToString("HH:mm"));
        laedt = false;
        Fehler.Visible = fehlerZeigen = false;
        BereichGeaendert?.Invoke();
        return true;
    }

    // Aufbewahrungsfrist des Archivs (Bauplan 46): leer = nicht festgelegt; gilt für das ganze Haus.
    public bool ArchivFristUebernehmen()
    {
        if (!Darf(Funktionen.ArchivLoeschen)) return false;
        var fehler = new List<string>();
        int? jahre = ArchivRegeln.FristLesen(ArchivFrist.Text, fehler);
        try
        {
            if (fehler.Count > 0) throw new Eingabefehler(fehler);
            speicher.ArchivFristSetzen(jahre, Recht.Kuerzel);
        }
        catch (Eingabefehler e)
        {
            ArchivFristMeldung.Text = string.Join(" ", e.Fehler);
            return false;
        }
        ArchivFristMeldung.Text = jahre is null ? "nicht festgelegt" : "übernommen";
        return true;
    }

    // Medikamentenbestellung (Bauplan 51): Bestand je Bereich, Tage, Apotheke und ihre E-Mail-Freigabe.
    public bool BestellungUebernehmen()
    {
        if (!Darf(Funktionen.EinstellungenBereich)) return false;
        try
        {
            speicher.BestellEinstellungen(Einstellungen.Laden().Bereich, BestandFuehren.Checked, (int)BestellTage.Value, Apotheke.Text, ApothekeEmail.Text, Recht.Kuerzel);
            if (Apotheke.Text.Trim().Length > 0) speicher.EmailFreigeben(Apotheke.Text, ApothekeFrei.Checked, Recht.Kuerzel);
        }
        catch (Eingabefehler e)
        {
            BestellungMeldung.Text = string.Join(" ", e.Fehler);
            BestellungMeldung.ForeColor = Stil.Beschluss.Text;
            return false;
        }
        BestellungMeldung.Text = "übernommen";
        BestellungMeldung.ForeColor = Stil.TextMittel;
        return true;
    }

    public bool SchichtzeitenUebernehmen()
    {
        if (laedt) return false;
        // Unverändert: nichts zu tun – auch ohne Recht kein Fehler beim Verlassen des Felds.
        var bisher = speicher.Schichtzeiten(Einstellungen.Laden().Bereich);
        if ((Frueh.Text.Trim(), Spaet.Text.Trim(), Nacht.Text.Trim()) == (bisher.Frueh.ToString("HH:mm"), bisher.Spaet.ToString("HH:mm"), bisher.Nacht.ToString("HH:mm")))
        {
            Fehler.Visible = fehlerZeigen = false;
            return true;
        }
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        Kern.Schichtzeiten zeiten;
        try { zeiten = Kern.Schichtzeiten.AusEingabe(Frueh.Text, Spaet.Text, Nacht.Text); }
        catch (Eingabefehler f) { return FehlerZeigen(string.Join(" ", f.Fehler)); }
        speicher.SchichtzeitenSetzen(Einstellungen.Laden().Bereich, zeiten);
        BerichtRegeln.Zeiten = zeiten;
        Fehler.Visible = fehlerZeigen = false;
        SchichtzeitenGeaendert?.Invoke();
        return true;
    }

    private void ModulSetzen(string schluessel, bool an)
    {
        ModulHakenAbgleichen();
        if (laedt || speicher.ModulAn(schluessel) == an) return;
        if (Recht.Grund(speicher, Funktionen.Module) is string grund) { FehlerZeigen(grund); return; }
        speicher.ModulSetzen(schluessel, an);   // Hauptfenster baut Seitenleiste und Akte neu (ModuleGeaendert)
    }

    // Ein Fenster der Einstellungen (Mitarbeiter, Rechte …): danach Werte und Sperren neu – die Rechte des Angemeldeten
    // können sich geändert haben.
    // Nicht modal: Das Vollbild bleibt auf dem zweiten Bildschirm offen, während man weiterarbeitet.
    public AnzeigeVollbild? VollbildZeigen()
    {
        if (VollbildBereich.SelectedItem is not string bereich || FindForm() is not Form programm) return null;
        var fenster = new AnzeigeVollbild(speicher, bereich, AnzeigeEinstellungen.Laden().VollerName, AnzeigeVollbild.Bildschirm(programm));
        fenster.FormClosed += (_, _) => fenster.Dispose();
        fenster.Show(programm);
        return fenster;
    }

    private void FensterZeigen(Form fenster)
    {
        using (fenster) Dialog.Zeigen(fenster, FindForm());
        Zeigen();
    }

    // Rechte auch im Handler prüfen: Die Sperre der Karte allein verhindert nicht jeden Weg.
    private bool Darf(string funktion)
    {
        if (Recht.Grund(speicher, funktion) is not string grund) return true;
        FehlerZeigen(grund);
        return false;
    }

    // Untermodule nur, wenn das Modul an ist.
    private void ModulHakenAbgleichen()
    {
        foreach (var m in Modulverwaltung.Alle)
            foreach (var u in m.Untermodule)
                Module[u.Schluessel].Enabled = Module[m.Daten.Schluessel].Checked && Module[m.Daten.Schluessel].Enabled;
    }

    // null = lokal. Ein gewählter Ordner muss erreichbar und beschreibbar sein; vorher fragen.
    public bool OrdnerSetzen(string? ordner)
    {
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        if (ordner is not null && Speicher.OrdnerPruefen(ordner) is string fehler) return FehlerZeigen(fehler);
        if (ordner == Einstellungen.Laden().DatenbankOrdner) return true;
        if (!Rueckfrage((ordner is null ? "Wieder die lokale Datenbank dieses Rechners nehmen?" : $"Die Datenbank im gemeinsamen Ordner „{ordner}“ nehmen?") +
                        "\n\nDas gilt nach dem Neustart des Programms."))
            return false;
        Einstellungen.Aendern(w => w with { DatenbankOrdner = ordner });
        Fehler.Visible = fehlerZeigen = false;
        OrtAnzeigen(ordner);
        BereichGeaendert?.Invoke();   // Hinweis „neuer Ort nach Neustart“ in der Seitenleiste
        return true;
    }

    private void OrtAnzeigen(string? ordner)
    {
        DatenbankOrt.Text = ordner is null ? "lokal im Datenordner dieses Rechners" : $"gemeinsamer Ordner: {ordner}";
        LokalKnopf.Enabled = ordner is not null;
    }

    public bool VorlageZuteilen(Bogenart art, string pfad)
    {
        if (Recht.Grund(speicher, Funktionen.Heb) is string grund) return FehlerZeigen(grund);
        try { HebPdf.VorlageZuteilen(art, pfad); }
        catch (Exception e) when (e is Eingabefehler or IOException or UnauthorizedAccessException)
        {
            return FehlerZeigen(e is Eingabefehler f ? string.Join(" ", f.Fehler) : $"Zuteilen ging nicht: {e.Message}");
        }
        Fehler.Visible = fehlerZeigen = false;
        VorlageAnzeigen(art);
        return true;
    }

    private void VorlageAnzeigen(Bogenart art)
    {
        string? pfad = HebPdf.Vorlage(art);
        try
        {
            Vorlagen[art].Text = pfad is null ? "keine Vorlage – der Bogen lässt sich trotzdem schreiben und drucken"
                : $"zugeteilt ({HebPdf.Felder(pfad).Count} Formularfelder)";
        }
        catch (Eingabefehler) { Vorlagen[art].Text = "zugeteilt, aber nicht lesbar – bitte neu zuteilen"; }
    }

    private void FarbeZeigen() =>
        MeineFarbe.BackColor = Recht.Kuerzel is { Length: > 0 } k && speicher.MitarbeiterLesen(k) is not null
            ? ColorTranslator.FromHtml(speicher.Farbe(k)) : Stil.Fensterhintergrund;

    public bool HatLogo => logoBild is not null;

    public bool LogoSetzen(string pfad)
    {
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) return FehlerZeigen(grund);
        using (var probe = Bewohnerbild.Laden(pfad))
            if (probe is null || !Bilder.EndungErlaubt(pfad))
                return FehlerZeigen($"„{Path.GetFileName(pfad)}“ ist kein lesbares Bild ({string.Join(", ", Bilder.Endungen)}).");
        try { Einstellungen.LogoSetzen(pfad); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return FehlerZeigen($"Übernehmen ging nicht: {e.Message}"); }
        Fehler.Visible = fehlerZeigen = false;
        LogoLaden(Einstellungen.LogoPfad());
        LogoGeaendert?.Invoke();
        return true;
    }

    public void LogoEntfernen()
    {
        if (Recht.Grund(speicher, Funktionen.EinstellungenBereich) is string grund) { FehlerZeigen(grund); return; }
        Einstellungen.LogoSetzen(null);
        LogoLaden(null);
        LogoGeaendert?.Invoke();
    }

    private void LogoWaehlen()
    {
        string muster = string.Join(";", Bilder.Endungen.Select(e => "*" + e));
        using var dialog = new OpenFileDialog { Title = "Bild für den Kopf wählen", Filter = $"Bilder ({muster})|{muster}" };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LogoSetzen(dialog.FileName);
    }

    private void LogoLaden(string? pfad)
    {
        logoBild?.Dispose();
        logoBild = Bewohnerbild.Laden(pfad);
        LogoWeg.Enabled = logoBild is not null;
        logoVorschau.Invalidate();
    }

    private void LogoZeichnen(Graphics g)
    {
        g.Clear(Stil.Flaeche);
        var rahmen = new Rectangle(0, 0, logoVorschau.Width - 1, logoVorschau.Height - 1);
        using (var stift = new Pen(Stil.KarteRand)) g.DrawRectangle(stift, rahmen);
        if (logoBild is null)
        {
            TextRenderer.DrawText(g, "kein Bild", Stil.Klein, rahmen, Stil.TextLeise, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(logoBild, KopfBereich.Einpassen(logoBild.Size, Rectangle.Inflate(rahmen, -4, -4)));
    }

    // Fehler oben in der Gruppe, in Gefahrenfarbe mit Text (Farbe nie allein). Immer false.
    private bool fehlerZeigen;

    private bool FehlerZeigen(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = fehlerZeigen = true;
        rechts.PerformLayout();
        return false;
    }
}
