using DokuLine.Kern;

namespace DokuLine;

// Die Karten der Einstellungen je Gruppe: Name, ein Satz Erklärung, das Bedienelement und das nötige Recht.
public sealed partial class EinstellungenSeite
{
    // ------------------------------------------------------------------ Karten

    private EinstellungKarte Karte(string gruppe, string name, string erklaerung, Control bedienung, string? recht = null, bool darunter = false)
    {
        var k = new EinstellungKarte(gruppe, name, erklaerung, bedienung, darunter) { Funktion = recht };
        Karten.Add(k);
        return k;
    }

    private static FlowLayoutPanel Reihe(params Control[] teile)
    {
        var r = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0), BackColor = Stil.Flaeche };
        foreach (var t in teile)
        {
            t.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandMini);
            if (t is Button b) b.AutoSize = true;
            r.Controls.Add(t);
        }
        return r;
    }

    private void KartenBauen()
    {
        // Persönlich
        AbmeldenKnopf.Click += (_, _) => AbmeldenGewuenscht?.Invoke();
        Karte("Persönlich", "Angemeldet als", (Betrieb.Aktiv ? "Anmelden mit Kürzel und Passwort." : "Anmelden mit dem Kürzel, ohne Passwort (Demo).") +
            " Das Kürzel steht bei Berichten, Übergabe und Aufgaben. Benutzer wechseln: abmelden (F12) und mit dem anderen Kürzel anmelden.",
            Reihe(Angemeldet, AbmeldenKnopf));
        FarbeKnopf.Click += (_, _) =>
        {
            if (Recht.Kuerzel is { Length: > 0 } k && FarbeWaehlen(MeineFarbe.BackColor) is Color c)
            {
                speicher.FarbeSetzen(k, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
                FarbeZeigen();
            }
        };
        FarbeVorgabeKnopf.Click += (_, _) => { if (Recht.Kuerzel is { Length: > 0 } k) { speicher.FarbeSetzen(k, null); FarbeZeigen(); } };
        Karte("Persönlich", "Meine Farbe", "Für Bezugspflege (Streifen auf der Karte) und Einzeltrainer (Punkt). Gilt auf allen Rechnern.",
            Reihe(MeineFarbe, FarbeKnopf, FarbeVorgabeKnopf));
        MenueEingeklappt.CheckedChanged += (_, _) => { if (!laedt) MenueGewuenscht?.Invoke(MenueEingeklappt.Checked); };
        Karte("Persönlich", "Seitenmenü", "Eingeklappt zeigt die Seitenleiste nur Symbole; der Name steht beim Darüberfahren.", MenueEingeklappt);

        // Wohnbereich (Recht „Einstellungen des Bereichs“)
        Bereich.Items.AddRange([.. Kern.Bereiche.Vorschlaege]);
        Bereich.Leave += (_, _) => BereichUebernehmen();
        Bereich.SelectedIndexChanged += (_, _) => BereichUebernehmen();
        Bereich.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BereichUebernehmen(); } };
        Karte("Wohnbereich", "Bereich dieses Rechners", "Ausgedachte Kennung des Arbeitsplatzes. Die Übersicht zeigt zuerst die Bewohner dieses Bereichs; Statusmeldungen tragen ihn mit.",
            Bereich, Funktionen.EinstellungenBereich);
        foreach (var feld in new[] { Frueh, Spaet, Nacht })
        {
            feld.Leave += (_, _) => SchichtzeitenUebernehmen();
            feld.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SchichtzeitenUebernehmen(); } };
        }
        Frueh.AccessibleName = "Beginn Frühdienst";
        Spaet.AccessibleName = "Beginn Spätdienst";
        Nacht.AccessibleName = "Beginn Nachtdienst";
        Karte("Wohnbereich", "Schichtzeiten", "Beginn von Früh, Spät und Nacht für diesen Bereich (alle Rechner). Danach richten sich Schicht der Berichte, Übergabe und Aufgaben.",
            Reihe(Frueh, Spaet, Nacht), Funktionen.EinstellungenBereich);
        BeschlussFrist.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { BeschlussFristTage = (int)BeschlussFrist.Value }); };
        Karte("Wohnbereich", "Beschluss endet bald", "So viele Tage vorher zeigt die Übersicht „Beschluss endet in …“.", BeschlussFrist, Funktionen.EinstellungenBereich);
        HebFrist.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { HebFristMonate = (int)HebFrist.Value }); };
        Karte("Wohnbereich", "HEB fällig", "So viele Monate nach dem letzten Bogen gilt der HEB-Bogen als fällig.", HebFrist, Funktionen.EinstellungenBereich);
        Wartezeit.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { WartezeitMinuten = (int)Wartezeit.Value }); };
        Karte("Wohnbereich", "Abgängigkeit: Wartezeit", "So viele Minuten nach der Rückkehrzeit fragt die Ausgangsliste „Abgängigkeit melden?“.", Wartezeit, Funktionen.EinstellungenBereich);
        MeldestellenKnopf.Click += (_, _) => FensterZeigen(new ListenFenster(speicher, [Liste.Meldestellen]));
        Karte("Wohnbereich", "Abgängigkeit: wer wird informiert", "Die Liste zum Abhaken bei einer Abgängigkeitsmeldung; Telefonnummern gehören mit in den Namen, z. B. „Rufbereitschaft (0000 000000)“.",
            MeldestellenKnopf, Funktionen.EinstellungenBereich);
        KeinBericht.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { KeinBerichtWochen = (int)KeinBericht.Value }); };
        Karte("Wohnbereich", "Lebensbereiche: kein Bericht seit", "Nach so vielen Wochen ohne Bericht in einem Lebensbereich zeigt die Akte (Reiter „Ziele und Maßnahmen“) „seit … Wochen kein Bericht“.",
            KeinBericht, Funktionen.EinstellungenBereich);
        ZieleLeit.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { ZieleLeitziele = (int)ZieleLeit.Value }); };
        ZieleNah.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { ZieleNahziele = (int)ZieleNah.Value }); };
        Karte("Wohnbereich", "Ziele: wie viele je Bewohner", "Weniger aktive Ziele zeigen die Marke „Ziel fehlt“. Vorgabe: 1 Leitziel und 2 Nahziele.",
            Reihe(new Label { Text = "Leitziele", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) }, ZieleLeit,
                new Label { Text = "Nahziele", AutoSize = true, Margin = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandKlein, 0) }, ZieleNah),
            Funktionen.EinstellungenBereich);
        Pruefpunkte.Items.AddRange([.. QualitaetRegeln.Alle.Select(QualitaetRegeln.Name)]);
        // ItemCheck meldet den neuen Zustand erst danach: die Liste nach dem Wechsel lesen.
        Pruefpunkte.ItemCheck += (_, e) =>
        {
            if (laedt || !Darf(Funktionen.Qualitaet)) return;
            var aus = QualitaetRegeln.Alle.Where((_, i) => i == e.Index ? e.NewValue != CheckState.Checked : !Pruefpunkte.GetItemChecked(i));
            Einstellungen.Aendern(w => w with { QmAus = QualitaetRegeln.AusText(aus) });
        };
        QmTage.ValueChanged += (_, _) => { if (!laedt && Darf(Funktionen.Qualitaet)) Einstellungen.Aendern(w => w with { QmTage = (int)QmTage.Value }); };
        Karte("Wohnbereich", "Qualität: Prüfpunkte", "Welche Punkte die Prüfliste der Seite „Qualität“ zeigt. „Maßnahmen werden bestätigt“: keine fällige Maßnahme so viele Tage ohne Bestätigung.",
            Reihe(Pruefpunkte, new Label { Text = "Tage", AutoSize = true, Margin = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandKlein, 0) }, QmTage),
            Funktionen.Qualitaet);
        ArchivFristKnopf.Click += (_, _) => ArchivFristUebernehmen();
        Karte("Wohnbereich", "Archiv: Aufbewahrungsfrist", "Jahre ab Entlassung, bis eine archivierte Akte gelöscht werden darf. Keine Vorgabe: Die Frist muss mit Träger und Datenschutz geklärt werden. Leer = nicht festgelegt.",
            Reihe(ArchivFrist, ArchivFristKnopf, ArchivFristMeldung), Funktionen.ArchivLoeschen);
        MassnahmenListenKnopf.Click += (_, _) => FensterZeigen(new ListenFenster(speicher, [Liste.Massnahmenarten, Liste.Massnahmenvorlagen]));
        Karte("Wohnbereich", "Maßnahmen: Arten und Vorlagen", "Arten (z. B. Anleitung, Begleitung) und Vorlagen für häufige Maßnahmen, damit nicht jeder anders benennt.",
            MassnahmenListenKnopf, Funktionen.MassnahmenAnlegen);
        RichtwerteKnopf.Click += (_, _) => FensterZeigen(new RichtwerteFenster(speicher));
        Karte("Wohnbereich", "Vitalwerte: allgemeine Richtwerte", $"Hinweis „über/unter dem allgemeinen Richtwert“, wenn keine ärztliche Anordnung gilt. {VitalRegeln.RichtwertVermerk}",
            RichtwerteKnopf, Funktionen.VitalGrenzen);
        logoVorschau.Paint += (_, e) => LogoZeichnen(e.Graphics);
        LogoKnopf.Click += (_, _) => LogoWaehlen();
        LogoWeg.Click += (_, _) => LogoEntfernen();
        Karte("Wohnbereich", "Bild im Kopf (Logo)", "Erscheint oben rechts. Die Datei wird in den Datenordner kopiert und nie mitgeliefert.",
            Reihe(logoVorschau, LogoKnopf, LogoWeg), Funktionen.EinstellungenBereich);
        // Wohnbereiche R1–R3 (offen/beschützend), Zimmer und Betten; Rechte prüft das Fenster.
        ZimmerKnopf.Click += (_, _) => { using var f = new ZimmerFenster(speicher); Dialog.Zeigen(f, FindForm()); };
        Karte("Wohnbereich", "Zimmer und Betten", "Wohnbereiche (offen oder beschützend) mit ihren Zimmern: Einzelzimmer oder „Bett Fenster“ und „Bett Tür“. Die Aufnahme zeigt nur freie Betten.",
            ZimmerKnopf);

        EinrichtungKnopf.Click += (_, _) => { using var f = new EinrichtungFenster(speicher); Dialog.Zeigen(f, FindForm()); };
        Karte("Wohnbereich", "Angaben der Einrichtung", "Name, Anschrift und Telefon des Dienstzimmers für den Überleitungsbogen. Nur im Datenordner, nie im Programm.",
            EinrichtungKnopf, Funktionen.EinstellungenBereich);

        // Anzeige für den Fernseher
        AnzeigeKnopf.Click += (_, _) => { using var f = new AnzeigeFenster(speicher, Anzeige); Dialog.Zeigen(f, FindForm()); Geaendert?.Invoke(); };
        if (!Betrieb.Aktiv)
            Karte("Wohnbereich", "Anzeige für den Fernseher", "Laufende Therapien je Wohnbereich als nur lesende Seite im lokalen Netz, mit Zugangscode. " +
                "Standardmäßig aus; gilt für diesen Rechner.", AnzeigeKnopf, Funktionen.AnzeigeVerwalten);
        VollbildBereich.Items.AddRange([.. speicher.Wohnbereiche().Select(w => (object)w.Name)]);
        if (VollbildBereich.Items.Count > 0) VollbildBereich.SelectedIndex = 0;
        VollbildKnopf.Click += (_, _) => VollbildZeigen();
        Karte("Wohnbereich", "Anzeige im Vollbild", "Dieselbe Anzeige auf einem zweiten Bildschirm (z. B. Fernseher über HDMI). Esc oder Doppelklick schließt.",
            Reihe(VollbildBereich, VollbildKnopf));

        // Module (Recht „Module ein- und ausschalten“): ein Modul je Karte, Untermodule darin (eine Ebene).
        foreach (var m in Modulverwaltung.Alle)
        {
            var spalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
            var haken = new CheckBox { Text = "eingeschaltet", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche };
            Module[m.Daten.Schluessel] = haken;
            spalte.Controls.Add(haken);
            string schluessel = m.Daten.Schluessel;
            haken.CheckedChanged += (_, _) => ModulSetzen(schluessel, haken.Checked);
            foreach (var u in m.Untermodule)
            {
                var unter = new CheckBox { Text = $"{u.Name} – {u.Beschreibung}", AutoSize = true, MaximumSize = new Size(420, 0), BackColor = Stil.Flaeche, Margin = new Padding(Stil.AbstandGross, 0, 0, 0) };
                Module[u.Schluessel] = unter;
                string us = u.Schluessel;
                unter.CheckedChanged += (_, _) => ModulSetzen(us, unter.Checked);
                spalte.Controls.Add(unter);
            }
            Karte("Module", m.Name, $"{m.Beschreibung} Ausgeschaltet verschwindet es aus Seitenleiste, Akte und Karten; die Daten bleiben.",
                spalte, Funktionen.Module, darunter: true);
        }

        // Aufgaben, Fahrzeuge (eigene Fenster mit „Speichern“)
        VorlagenKnopf.Click += (_, _) => { FensterZeigen(new VorlagenFenster(speicher)); Geaendert?.Invoke(); };
        Karte("Aufgaben", "Standardaufgaben", "Aufgaben, die zu Beginn jeder Schicht von selbst entstehen (z. B. Rundgänge im Nachtdienst). Ohne Recht nur ansehen.", VorlagenKnopf);
        FahrzeugeKnopf.Click += (_, _) => { using var f = new FahrzeugeFenster(speicher); Dialog.Zeigen(f, FindForm()); Geaendert?.Invoke(); };
        Karte("Fahrzeuge", "Fahrzeugliste", "Firmenwagen für Fahrten (Arzt, Ausflug, Sporttherapie); hinzufügen, stilllegen, wieder aktivieren.", FahrzeugeKnopf, Funktionen.Fahrzeuge);

        // Vorlagen: HEB-PDFs je Bogenart
        foreach (var art in Enum.GetValues<Bogenart>())
        {
            var status = new Label { AutoSize = true, MaximumSize = new Size(360, 0), BackColor = Stil.Flaeche, Margin = new Padding(0, 0, 0, Stil.AbstandMini) };
            Vorlagen[art] = status;
            var zuteilen = Stil.NebenKnopf("Zuteilen …");
            var entfernen = Stil.NebenKnopf("Entfernen");
            var zuordnung = Stil.NebenKnopf("Zuordnung …");
            zuteilen.Click += (_, _) => { if (PdfWaehlen() is string pfad) VorlageZuteilen(art, pfad); };
            entfernen.Click += (_, _) => { HebPdf.VorlageEntfernen(art); VorlageAnzeigen(art); };
            zuordnung.Click += (_, _) => { using var f = new HebZuordnungFenster(art); Dialog.Zeigen(f, FindForm()); };
            var spalte = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
            spalte.Controls.AddRange([status, Reihe(zuteilen, entfernen, zuordnung)]);
            Karte("Vorlagen", $"HEB-Bogen {HebRegeln.ArtKurz(art)}", "PDF mit Formularfeldern des Kostenträgers. Sie wird in den Datenordner kopiert und nie mitgeliefert; ausgefüllt wird immer eine Kopie.",
                spalte, Funktionen.Heb);
        }

        // Medikamente
        MedikamenteKnopf.Click += (_, _) => { using var f = new MedikamenteFenster(speicher); Dialog.Zeigen(f, FindForm()); };
        Karte("Medikamente", "Medikamentendatenbank", "Liste, aus der beim Verordnen gewählt wird. Anlegen dürfen Fachkräfte (sie prüfen mit Quelle und Datum), verwalten der Psychologische Fachdienst.",
            MedikamenteKnopf);
        NachschlageAnsicht.CheckedChanged += (_, _) => { if (!laedt) Einstellungen.Aendern(w => w with { NachschlageAnsicht = NachschlageAnsicht.Checked }); };
        Karte("Medikamente", "Nachschlage-Ansicht (BfArM)", "Eingebettete Suche des BfArM über der Maske „Medikament“ – die einzige Webansicht im Programm, nur feste Adressen (" +
            string.Join(", ", ExterneAdressen.Nachschlagebereiche) + "). Das BfArM erhält dabei nur, was jede Webseite erhält: den Suchbegriff (nur den " +
            "Medikamentennamen, den man selbst einfügt), die IP-Adresse der Einrichtung und die Uhrzeit – nie Angaben zu Bewohnern. Anfragen an andere " +
            "Adressen werden blockiert; Cookies und Verlauf gelten nur bis zum Schließen. Aus: nur „Im Browser öffnen“.", NachschlageAnsicht);

        // Verwaltung
        MitarbeiterKnopf.Click += (_, _) => FensterZeigen(new MitarbeiterFenster(speicher));
        ListenKnopf.Click += (_, _) => FensterZeigen(new ListenFenster(speicher));
        RechteKnopf.Click += (_, _) => FensterZeigen(new RechteFenster(speicher));
        Karte("Verwaltung", "Mitarbeiter", "Mitarbeiter mit Bereichen, Positionen und Qualifikation (Assistenzkraft oder Fachkraft). Die Bereichsleitung verwaltet ihren Bereich.", MitarbeiterKnopf);
        Karte("Verwaltung", "Bereiche, Positionen und Berichtsarten",
            "Listen zum Zuordnen; stilllegen statt löschen, solange etwas zugeordnet ist.", ListenKnopf);
        AkteListenKnopf.Click += (_, _) => FensterZeigen(new ListenFenster(speicher, ListenFenster.Akte));
        Karte("Verwaltung", "Listen für die Akte", "Auswahl in den Reitern Krankenkasse, Kostenträger, Betreuung, Dokumente und Vitalwerte (Demo: ausgedachte Namen).",
            AkteListenKnopf);
        UebernahmeKnopf.Click += (_, _) => { using var f = new AkteUebernahmeFenster(speicher); Dialog.Zeigen(f, FindForm()); Geaendert?.Invoke(); };
        Karte("Verwaltung", "Digitale Akte übernehmen", "Vorhandene Ordner (ein Unterordner je Bewohner) in die Ablage „Dokumente“ kopieren – mit Vorschau; " +
            "der alte Ordner bleibt unverändert.", UebernahmeKnopf, Funktionen.DokumenteHinzufuegen);
        Karte("Verwaltung", "Rechte", "Tabelle: welche Position was darf, je Funktion „nur Fachkräfte“. Ohne Recht nur ansehen.", RechteKnopf);

        // System (folgenreich: fragt vorher)
        OrdnerKnopf.Click += (_, _) => { if (OrdnerWaehlen() is string ordner) OrdnerSetzen(ordner); };
        LokalKnopf.Click += (_, _) => OrdnerSetzen(null);
        var ort = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = Stil.Flaeche };
        ort.Controls.AddRange([DatenbankOrt, Reihe(OrdnerKnopf, LokalKnopf)]);
        DatenbankOrt.BackColor = Stil.Flaeche;
        if (!Betrieb.Aktiv)
            Karte("System", "Ort der Datenbank", "Mehrere Rechner mit demselben Ordner arbeiten gemeinsam. Gilt nach dem Neustart; ist der Ordner leer, entstehen dort die Demo-Daten. Auch dort nur ausgedachte Daten.",
                ort, Funktionen.EinstellungenBereich, darunter: true);
        SicherungKnopf.Click += (_, _) => FensterZeigen(new SicherungFenster(speicher));
        Karte("System", "Sicherung", "Jetzt sichern, Stände prüfen, zurücksichern. Im Betrieb jede Nacht automatisch (30 tägliche, 12 monatliche Stände).",
            SicherungKnopf, Funktionen.BetriebVerwalten);
        AutoUpdate.CheckedChanged += (_, _) => { if (!laedt && Darf(Funktionen.EinstellungenBereich)) Einstellungen.Aendern(w => w with { AutoUpdate = AutoUpdate.Checked }); };
        if (!Betrieb.Aktiv)
            Karte("System", "Update", "Beim Start nach einer neuen Version suchen (nur die Releases dieses Programms). Abschaltbar auch mit „--kein-update“.",
                AutoUpdate, Funktionen.EinstellungenBereich);
        else
        {
            UpdateKnopf.Click += (_, _) => FensterZeigen(new UpdateFenster(speicher));
            Karte("System", "Update vorbereiten", "Im Betrieb kein Selbst-Update: herunterladen oder vom USB-Stick, Prüfsumme prüfen, nachts einspielen.",
                UpdateKnopf, Funktionen.BetriebVerwalten);
        }
        // Betrieb (Bauplan 37): Passwörter, Zugriffsprotokoll, Häuser.
        PasswortKnopf.Click += (_, _) => FensterZeigen(new PasswortAendernFenster(speicher, Recht.Kuerzel));
        if (Betrieb.Aktiv) Karte("Persönlich", "Passwort ändern", "Das eigene Passwort (mindestens 10 Zeichen). Gespeichert wird nur ein Hash.", PasswortKnopf);
        PasswoerterKnopf.Click += (_, _) => FensterZeigen(new PasswoerterFenster(speicher));
        if (Betrieb.Aktiv)
            Karte("Verwaltung", "Passwörter der Mitarbeiter", "Einmalpasswort vergeben – auch zum Entsperren nach 5 Fehlversuchen.", PasswoerterKnopf, Funktionen.MitarbeiterBereich);
        ProtokollKnopf.Click += (_, _) => FensterZeigen(new ZugriffsprotokollFenster(speicher));
        Karte("Verwaltung", "Zugriffsprotokoll", "Wer wann welche Akte geöffnet, gedruckt oder geändert hat. Nur mit eigenem Recht.", ProtokollKnopf, Funktionen.Zugriffsprotokoll);
        HaeuserKnopf.Click += (_, _) => FensterZeigen(new HaeuserFenster(speicher));
        Karte("Verwaltung", "Häuser", "Bereiche gehören zu einem Haus; die Bereichswahl zeigt das eigene Haus, andere nur mit Recht.", HaeuserKnopf, Funktionen.BetriebVerwalten);
        DemoKnopf.Click += (_, _) => DemoGewuenscht?.Invoke();
        var achtung = new Label
        {
            Text = "Achtung: löscht alle Änderungen und hinterlegten Bilder und lädt die ausgedachten Demo-Daten neu (fragt vorher).",
            AutoSize = true, MaximumSize = new Size(360, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandMini),
        };
        if (!Betrieb.Aktiv)
            Karte("System", "Demo-Daten zurücksetzen", "Für die Vorführung: alles auf den Anfang.", Reihe(DemoKnopf, achtung), Funktionen.DemoZuruecksetzen, darunter: true);

        // Über
        Karte("Über", "Programm", "Bis Version 0.29.1 hieß das Programm „Dokumentationssystem“.",
            new Label { Text = $"{Programmversion.Name} – {Programmversion.Leittext}", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche });
        Karte("Über", "Version", "Neue Versionen kommen über das Selbst-Update.", new Label { Text = Programmversion.Text, AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche });
        Karte("Über", "Tastenkürzel", "Gelten im ganzen Programm.",
            new Label { Text = "F12 = Abmelden · F11 = Tagesplan im Vollbild (Esc beendet)", AutoSize = true, Font = Stil.Fett, BackColor = Stil.Flaeche });
        Karte("Über", "Demo", "Kein Echtbetrieb: Echte Daten wären Gesundheitsdaten (Art. 9 DSGVO) und bräuchten Freigaben, die es nicht gibt.",
            new MarkenFeld(Stil.DemoHinweis, Stil.Achtung));
        Karte("Über", "Bausteine und Lizenzen", "Freie Bibliotheken, die das Programm benutzt.",
            new Label
            {
                Text = "Microsoft.Data.Sqlite (MIT) · PDFsharp 6.2.4 (MIT) · System.Security.Cryptography.Pkcs (MIT) · WebView2-Steuerelement von Microsoft (BSD-artige Lizenz) · .NET 10 (MIT)",
                AutoSize = true, MaximumSize = new Size(360, 0), BackColor = Stil.Flaeche,
            }, darunter: true);
        WebAnsichtStand.BackColor = Stil.Flaeche;
        Karte("Über", "Nachschlage-Ansicht", "Ob Microsoft Edge WebView2 auf diesem Rechner da ist (Hinweis für die IT).", WebAnsichtStand);
    }
}
