using DokuLine.Kern;

namespace DokuLine;

// Bereich „Tagesplan“: alle Termine von heute für die Bewohner des gewählten Bereichs. Oben „Jetzt und gleich“,
// darunter „Später heute“, unten blasser „Erledigt“ – oder „nach Bewohner“. Bei Therapien „gegangen“,
// „abgesagt“, „ausgefallen“ direkt in der Zeile; das ist derselbe Eintrag wie im Wochenplan, mit
// Kürzel und Uhrzeit. Die Tafel erneuert sich jede Minute und bei jeder Änderung (auch von anderen
// Rechnern über den Mehrplatz-Abgleich). „Vollbild (F11)“ zeigt nur die Tafel; Esc beendet.
public sealed class TagesplanSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Tagesplan";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, Dock = DockStyle.Top, BackColor = Stil.Seite };
    private readonly System.Windows.Forms.Timer takt = new() { Interval = 1000 };
    private string bereichText = "";
    private DateTime letzteMinute;

    public RadioButton NachUhrzeit { get; } = new() { Text = "nach Uhrzeit", AutoSize = true, Checked = true, BackColor = Stil.Seite };
    public RadioButton NachBewohner { get; } = new() { Text = "nach Bewohner", AutoSize = true, BackColor = Stil.Seite };
    public Button VollbildKnopf { get; } = Stil.NebenKnopf("Vollbild (F11)");
    public Label Fehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public AnzeigeTafel Tafel { get; } = new() { Dock = DockStyle.Fill };
    public TafelVollbild? Vollbild { get; private set; }
    public List<TagesplanZeile> Zeilen { get; private set; } = [];
    public List<TafelZeile> TafelZeilen { get; private set; } = [];
    public List<string> Gruppen => Tafel.Ueberschriften;
    public event Action? Geaendert;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public TagesplanSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        leiste.Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, Stil.AbstandKlein);
        foreach (var c in new Control[] { NachUhrzeit, NachBewohner }) c.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMittel, Stil.AbstandKlein);
        VollbildKnopf.AutoSize = true;
        VollbildKnopf.Margin = new Padding(Stil.AbstandGross, 0, Stil.AbstandMittel, 0);
        Fehler.Margin = new Padding(Stil.AbstandGross, Stil.AbstandMini, 0, Stil.AbstandKlein);
        leiste.Controls.AddRange([NachUhrzeit, NachBewohner, VollbildKnopf, Fehler]);
        NachUhrzeit.CheckedChanged += (_, _) => { if (NachUhrzeit.Checked) Aktualisieren(); };
        NachBewohner.CheckedChanged += (_, _) => { if (NachBewohner.Checked) Aktualisieren(); };
        VollbildKnopf.Click += (_, _) => VollbildZeigen();
        Controls.Add(Tafel);
        Controls.Add(leiste);
        // Jede Minute: Uhr, Status und Reihenfolge (nur wenn die Tafel zu sehen ist).
        takt.Tick += (_, _) => Takt();
        takt.Start();
    }

    private void Takt()
    {
        var jetzt = Jetzt();
        var minute = new DateTime(jetzt.Year, jetzt.Month, jetzt.Day, jetzt.Hour, jetzt.Minute, 0);
        if (minute == letzteMinute || !(Visible || Vollbild is not null)) return;
        Aktualisieren();
    }

    public string KopfText
    {
        get
        {
            var jetzt = Jetzt();
            int therapien = Zeilen.Count(z => z.Therapie);
            int bestaetigt = Zeilen.Count(z => z.Therapie && z.Status != Teilnahme.Offen);
            int offen = Zeilen.Count(z => TagesplanRegeln.Marke(z, jetzt) == TagesplanRegeln.NochOffen);
            return $"{Zeilen.Count} Termine heute · {bereichText} · {bestaetigt} von {therapien} Therapien bestätigt" +
                   (offen > 0 ? $" · {offen} noch offen" : "");
        }
    }

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        letzteMinute = new DateTime(jetzt.Year, jetzt.Month, jetzt.Day, jetzt.Hour, jetzt.Minute, 0);
        var heute = DateOnly.FromDateTime(jetzt);
        // Der in der Übersicht gewählte Bereich („Alle“ und Bereiche ohne Bewohner: alle).
        var alle = speicher.AlleBewohner();
        var gezeigt = BereichswahlRegeln.Bewohner(alle, Arbeitsbereich.Gewaehlt);
        bereichText = gezeigt.Count == alle.Count ? "alle Bereiche" : Arbeitsbereich.Gewaehlt;
        var zeilen = speicher.Tagesplan(heute, gezeigt);
        TafelZeilen = NachBewohner.Checked ? TafelRegeln.NachBewohner(zeilen, jetzt) : TafelRegeln.Tafel(zeilen, jetzt);
        Zeilen = TafelZeilen.Select(z => z.Zeile).ToList();
        string? grund = Recht.Grund(speicher, Funktionen.TeilnahmeBestaetigen);
        string titel = $"TAGESPLAN · {bereichText}";
        Tafel.Fuellen(titel, TafelZeilen, NachBewohner.Checked, jetzt, grund, (z, s) => Setzen(z, s));
        Vollbild?.Tafel.Fuellen(titel, TafelZeilen, NachBewohner.Checked, jetzt, grund, (z, s) => Setzen(z, s));
        Geaendert?.Invoke();
    }

    // „gegangen“, „abgesagt“ oder „ausgefallen“ – ein Eintrag mit Kürzel und Uhrzeit (wie im Wochenplan).
    public bool Setzen(TagesplanZeile z, Teilnahme status)
    {
        if (!z.Therapie || Recht.Grund(speicher, Funktionen.TeilnahmeBestaetigen) is not null) return false;
        try
        {
            speicher.TeilnahmeSetzen(z.Termin.ZuteilungId!.Value, z.Termin.Tag, status, Einstellungen.Laden().Kuerzel, Jetzt());
            Fehler.Visible = false;
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        Aktualisieren();
        return true;
    }

    // Nur die Tafel, bildschirmfüllend (Knopf oder F11); Esc beendet. Läuft weiter mit, bis es zu ist.
    public TafelVollbild VollbildZeigen()
    {
        if (Vollbild is not null) { Vollbild.Activate(); return Vollbild; }
        Vollbild = new TafelVollbild();
        if (FindForm() is Form f) Vollbild.Bounds = Screen.FromControl(f).Bounds;
        Vollbild.FormClosed += (_, _) => Vollbild = null;   // ohne Dialog geöffnet: Schließen gibt es selbst frei
        Vollbild.Show(FindForm());
        Aktualisieren();
        return Vollbild;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            takt.Dispose();
            if (Vollbild is { IsDisposed: false } v) v.Close();
        }
        base.Dispose(disposing);
    }
}
