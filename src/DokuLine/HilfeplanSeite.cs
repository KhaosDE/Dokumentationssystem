using DokuLine.Kern;

namespace DokuLine;

// Bereich „Hilfeplangespräche“ (Bauplan 47): alle Bewohner des Bereichs nach Fälligkeit mit „fällig in n Wochen“ bzw.
// „überfällig“. Termin vergeben (Kalender, Wochen- und Tagesplan; Warnung bei Überschneidung), einladen (E-Mail-Programm und
// .ics-Datei, ohne Gesundheitsangaben), Vorbereitung als Aufgaben, nach dem Gespräch „geführt“ mit Ergebnis als Bericht.
public sealed class HilfeplanSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Hilfeplangespräche";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [TerminKnopf, EinladenKnopf, VorbereitenKnopf, GefuehrtKnopf, FaelligKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    public Button TerminKnopf { get; } = Stil.HauptKnopf("Termin vergeben …");
    public Button EinladenKnopf { get; } = Stil.NebenKnopf("Einladen …");
    public Button VorbereitenKnopf { get; } = Stil.NebenKnopf("Vorbereitung …");
    public Button GefuehrtKnopf { get; } = Stil.NebenKnopf("Geführt …");
    public Button FaelligKnopf { get; } = Stil.NebenKnopf("Fälligkeit ändern …");
    public ListView Liste { get; } = new()
    {
        View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        AccessibleName = "Hilfeplangespräche", Font = Stil.Standard,
    };
    public List<HilfeplanStand> Zeilen { get; private set; } = [];
    public string KopfText { get; private set; } = "";
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    // Zum Testen ersetzbar: Fragen und das Speichern der .ics-Datei.
    internal Func<string, string, string?> Fragen { get; set; }
    internal Func<IReadOnlyList<string>, List<int>?> Waehlen { get; set; }
    internal Func<string, bool> Warnen { get; set; }
    internal Func<string, string?> IcsSpeichern { get; set; }
    public event Action? Geaendert;

    public HilfeplanSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Fragen = (titel, frage) => Eingabe.Fragen(FindForm(), titel, frage);
        Waehlen = texte => MehrfachWahl.Zeigen(FindForm(), "Hilfeplangespräch", "Bitte wählen:", texte);
        Warnen = text => MessageBox.Show(FindForm(), text + "\n\nTrotzdem eintragen?", "Überschneidung", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        IcsSpeichern = vorschlag => KalenderAustausch.SpeichernUnter(FindForm(), vorschlag);
        foreach (var (titel, breite) in new[] { ("Zimmer", 70), ("Name", 180), ("Letztes", 100), ("Fällig bis", 130), ("Stand", 130), ("Termin", 150), ("Hinweis", 230), ("Nächster HEB", 150) })
            Liste.Columns.Add(titel, breite);
        Liste.SelectedIndexChanged += (_, _) => KnoepfeSetzen();
        TerminKnopf.Click += (_, _) => TerminVergeben();
        EinladenKnopf.Click += (_, _) => Einladen();
        VorbereitenKnopf.Click += (_, _) => Vorbereiten();
        GefuehrtKnopf.Click += (_, _) => Gefuehrt();
        FaelligKnopf.Click += (_, _) => FaelligAendern();
        Controls.Add(Liste);
    }

    public HilfeplanStand? Gewaehlt => Liste.SelectedItems.Count == 1 ? Liste.SelectedItems[0].Tag as HilfeplanStand : null;

    public void Aktualisieren()
    {
        var heute = DateOnly.FromDateTime(Jetzt());
        var bewohner = BereichswahlRegeln.Bewohner(speicher.AlleBewohner(), Arbeitsbereich.Gewaehlt);
        Zeilen = [.. speicher.Hilfeplaene(bewohner, heute).OrderBy(z => z.FaelligBis ?? DateOnly.MaxValue).ThenBy(z => z.Bewohner.Zimmer.PadLeft(8), StringComparer.Ordinal)];
        var heb = speicher.HebFristen(bewohner, heute).ToDictionary(h => h.Bewohner.Id!.Value);
        Liste.BeginUpdate();
        Liste.Items.Clear();
        foreach (var z in Zeilen)
        {
            string? marke = HilfeplanRegeln.Marke(z, heute);
            var zeile = new ListViewItem([z.Bewohner.Zimmer, z.Bewohner.Name, Datum.Text(z.LetztesAm), z.FaelligBis is DateOnly f ? Datum.Text(f) + (z.Vorschlag ? " (Vorschlag)" : "") : "nicht erfasst",
                HilfeplanRegeln.StandText(z.Stand), z.Termin is KalenderTermin t ? $"{t.Aktuell.Beginn:dd.MM.yyyy HH:mm}" : "", marke ?? "",
                heb.TryGetValue(z.Bewohner.Id!.Value, out var h) ? HebFristenSeite.FaelligText(h) : ""])
            { Tag = z, UseItemStyleForSubItems = false };
            if (marke is not null) zeile.SubItems[6].ForeColor = Stil.Achtung.Text;
            if (z.FaelligBis is null) zeile.SubItems[3].ForeColor = Stil.Achtung.Text;
            Liste.Items.Add(zeile);
        }
        Liste.EndUpdate();
        int ueberfaellig = Zeilen.Count(z => HilfeplanRegeln.Marke(z, heute)?.EndsWith("überfällig", StringComparison.Ordinal) == true);
        KopfText = $"{Zeilen.Count} Bewohner · {ueberfaellig} überfällig · {Zeilen.Count(z => z.Stand == HilfeplanStandArt.TerminVergeben)} mit Termin";
        KnoepfeSetzen();
        Geaendert?.Invoke();
    }

    private void KnoepfeSetzen()
    {
        Recht.Anwenden(speicher, Funktionen.BewohnerBearbeiten, TerminKnopf, EinladenKnopf, VorbereitenKnopf, GefuehrtKnopf, FaelligKnopf);
        if (Recht.Grund(speicher, Funktionen.BewohnerBearbeiten) is not null) return;
        string? ohne = Gewaehlt is null ? "Bitte zuerst einen Bewohner wählen." : null;
        string? ohneTermin = ohne ?? (Gewaehlt!.Termin is null ? "Zuerst einen Termin vergeben." : null);
        foreach (var k in new[] { TerminKnopf, FaelligKnopf, GefuehrtKnopf }) Stil.Sperren(k, ohne);
        foreach (var k in new[] { EinladenKnopf, VorbereitenKnopf }) Stil.Sperren(k, ohneTermin);
    }

    private static DateTime? ZeitLesen(string? text)
    {
        if (text is null) return null;
        return DateTime.TryParseExact(text.Trim(), "dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t)
            ? t : throw new Eingabefehler(["Bitte als TT.MM.JJJJ SS:MM eingeben, z. B. 03.12.2026 10:00."]);
    }

    private bool Fehler(Eingabefehler e, string titel)
    {
        MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), titel, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    public bool TerminVergeben()
    {
        if (Gewaehlt is not HilfeplanStand z || Stil.Sperrgrund(TerminKnopf) is not null) return false;
        long id = z.Bewohner.Id!.Value;
        try
        {
            if (ZeitLesen(Fragen("Termin vergeben", "Wann? (TT.MM.JJJJ SS:MM)")) is not DateTime beginn) return false;
            if (Fragen("Termin vergeben", "Wo? (Ort)") is not string ort) return false;
            var vorschlag = speicher.HilfeplanTeilnehmer(id, DateOnly.FromDateTime(Jetzt()));
            if (Waehlen(vorschlag) is not { Count: > 0 } wahl) return false;
            var teilnehmer = wahl.Select(i => vorschlag[i]).ToList();
            if (Fragen("Termin vergeben", "Weitere Teilnehmer (freiwillig, mit Komma getrennt)") is string weitere)
                teilnehmer.AddRange(weitere.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            var ende = beginn.AddMinutes(HilfeplanRegeln.DauerVorgabe);
            if (speicher.HilfeplanUeberschneidungen(id, beginn, ende) is { Count: > 0 } ueber && !Warnen("Zur selben Zeit: " + string.Join("; ", ueber))) return false;
            speicher.HilfeplanTerminVergeben(id, beginn, HilfeplanRegeln.DauerVorgabe, ort, teilnehmer, Recht.Kuerzel, Jetzt());
        }
        catch (Eingabefehler e) { return Fehler(e, "Termin vergeben"); }
        Aktualisieren();
        return true;
    }

    // E-Mail-Programm mit Betreff und Text (ohne Gesundheitsangaben) und die .ics-Datei zum Anhängen.
    public bool Einladen()
    {
        if (Gewaehlt is not { Termin: KalenderTermin t } z || Stil.Sperrgrund(EinladenKnopf) is not null) return false;
        var namen = new Dictionary<long, string> { [z.Bewohner.Id!.Value] = z.Bewohner.Name };
        if (IcsSpeichern($"Hilfeplangespräch {z.Bewohner.Nachname} {t.Aktuell.Beginn:yyyy-MM-dd}.ics") is string pfad)
            KalenderAustausch.Schreiben(pfad, Ics.Schreiben([t], namen, Jetzt(), speicher.IcsKennung()));
        var empfaenger = new List<string>();
        if (speicher.Betreuer(z.Bewohner.Id!.Value) is { Keine: false } b) empfaenger.Add(b.Email);
        if (speicher.Angaben(z.Bewohner.Id!.Value, DateOnly.FromDateTime(Jetzt())).GetValueOrDefault(Angabe.Kostentraeger)?.Lesen<Kostentraeger>() is Kostentraeger k)
            empfaenger.Add(k.Email);
        Extern.Oeffnen(HilfeplanRegeln.Mailto(empfaenger, HilfeplanRegeln.Betreff(z.Bewohner, t.Aktuell), HilfeplanRegeln.Einladung(z.Bewohner, t.Aktuell)));
        return true;
    }

    public bool Vorbereiten()
    {
        if (Gewaehlt is not HilfeplanStand z || Stil.Sperrgrund(VorbereitenKnopf) is not null) return false;
        if (Waehlen(HilfeplanRegeln.Vorbereitung) is not { Count: > 0 } wahl) return false;
        try { speicher.HilfeplanVorbereiten(z.Bewohner.Id!.Value, wahl.Select(i => HilfeplanRegeln.Vorbereitung[i]), Recht.Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { return Fehler(e, "Vorbereitung"); }
        return true;
    }

    public bool Gefuehrt()
    {
        if (Gewaehlt is not HilfeplanStand z || Stil.Sperrgrund(GefuehrtKnopf) is not null) return false;
        try
        {
            var heute = DateOnly.FromDateTime(Jetzt());
            var vorgabe = z.Termin is KalenderTermin t ? DateOnly.FromDateTime(t.Aktuell.Beginn) : heute;
            if (Fragen("Geführt", $"Am (TT.MM.JJJJ, leer = {Datum.Text(vorgabe)})") is not string amText) return false;
            var am = amText.Trim().Length == 0 ? vorgabe : Datum.Lesen(amText, "Geführt am");
            if (Fragen("Geführt", "Ergebnis (kurz; erscheint als Bericht „Hilfeplangespräch“)") is not string ergebnis) return false;
            if (Fragen("Geführt", "Nächstes fällig bis (TT.MM.JJJJ, leer = später eintragen)") is not string naechstesText) return false;
            DateOnly? naechstes = naechstesText.Trim().Length == 0 ? null : Datum.Lesen(naechstesText, "Nächstes fällig bis");
            speicher.HilfeplanGefuehrt(z.Bewohner.Id!.Value, am, ergebnis, naechstes, Recht.Kuerzel, Jetzt());
        }
        catch (FormatException e) { return Fehler(new Eingabefehler([e.Message]), "Geführt"); }
        catch (Eingabefehler e) { return Fehler(e, "Geführt"); }
        Aktualisieren();
        return true;
    }

    public bool FaelligAendern()
    {
        if (Gewaehlt is not HilfeplanStand z || Stil.Sperrgrund(FaelligKnopf) is not null) return false;
        if (Fragen("Fälligkeit ändern", "Nächstes Hilfeplangespräch fällig bis (TT.MM.JJJJ)") is not string text) return false;
        try { speicher.HilfeplanFaelligSetzen(z.Bewohner.Id!.Value, Datum.Lesen(text, "Fällig bis"), Recht.Kuerzel, Jetzt()); }
        catch (FormatException e) { return Fehler(new Eingabefehler([e.Message]), "Fälligkeit ändern"); }
        catch (Eingabefehler e) { return Fehler(e, "Fälligkeit ändern"); }
        Aktualisieren();
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        Liste.SetBounds(rand, rand, Math.Max(100, ClientSize.Width - 2 * rand), Math.Max(Stil.Px(this, 120), ClientSize.Height - 2 * rand));
    }
}
