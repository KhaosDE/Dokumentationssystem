using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using Dokusystem.Kern;

namespace Dokusystem;

// Reiter „Wochenplan“ in der Akte: Montag bis Sonntag als Spalten, Uhrzeiten als Zeilen. Freie
// Einträge der Regelwoche und die zugeteilten Therapien (farblich abgesetzt); Blättern nach
// Kalenderwochen. Klick auf einen Termin: bei Therapien die Teilnahme, bei freien Einträgen
// „Ändern ab …“ oder „Entfernen ab …“. Ausdruck groß und nur mit Name und Plan.
public sealed class WochenplanAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private Bewohner? bewohner;

    public DateOnly Montag { get; private set; }
    public Label WocheText { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite };
    public Button ZurueckKnopf { get; } = Stil.NebenKnopf("◀");
    public Button VorKnopf { get; } = Stil.NebenKnopf("▶");
    public Button DieseWocheKnopf { get; } = Stil.NebenKnopf("Diese Woche");
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neuer Eintrag …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public Button VerlaufKnopf { get; } = Stil.NebenKnopf("Verlauf …");
    public WochenRaster Raster { get; } = new();

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public WochenplanAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        foreach (var c in new Control[] { ZurueckKnopf, WocheText, VorKnopf, DieseWocheKnopf, NeuKnopf, DruckKnopf, VerlaufKnopf })
        {
            c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        ZurueckKnopf.Width = VorKnopf.Width = 44;
        WocheText.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein + 2, Stil.AbstandKlein, 0);
        ZurueckKnopf.AccessibleName = "Vorige Woche";
        VorKnopf.AccessibleName = "Nächste Woche";
        ZurueckKnopf.Click += (_, _) => WocheZeigen(Montag.AddDays(-7));
        VorKnopf.Click += (_, _) => WocheZeigen(Montag.AddDays(7));
        DieseWocheKnopf.Click += (_, _) => WocheZeigen(WochenplanRegeln.Montag(DateOnly.FromDateTime(Jetzt())));
        NeuKnopf.Click += (_, _) => Bearbeiten(null);
        DruckKnopf.Click += (_, _) => Drucken();
        VerlaufKnopf.Click += (_, _) => VerlaufZeigen();
        Raster.TerminGeklickt += TerminMenue;
        Controls.Add(leiste);
        Controls.Add(Raster);
    }

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        WocheZeigen(WochenplanRegeln.Montag(DateOnly.FromDateTime(Jetzt())));
    }

    public void WocheZeigen(DateOnly montag)
    {
        Montag = montag;
        Aktualisieren();
    }

    public List<Termin> Termine => bewohner?.Id is long id ? speicher.Woche(id, Montag) : [];

    public void Aktualisieren()
    {
        if (bewohner is null) return;
        WocheText.Text = WochenplanRegeln.WochenText(Montag);
        Raster.Setzen(Montag, Termine, DateOnly.FromDateTime(Jetzt()));
        PerformLayout();
    }

    private static string Kuerzel => Einstellungen.Laden().Kuerzel;

    // Kontextmenü beim Klick auf einen Termin.
    private void TerminMenue(Termin t, Point wo)
    {
        var menue = new ContextMenuStrip { Font = Stil.Standard, ShowImageMargin = false };
        if (t.Quelle == TerminQuelle.Therapie)
        {
            foreach (var s in new[] { Teilnahme.Teilgenommen, Teilnahme.Abgesagt, Teilnahme.Ausgefallen, Teilnahme.Offen })
            {
                var e = new ToolStripMenuItem(s == Teilnahme.Offen ? "zurück auf „offen“" : WochenplanRegeln.TeilnahmeText(s)) { Checked = t.Teilnahme == s };
                e.Click += (_, _) => TeilnahmeSetzen(t, s);
                menue.Items.Add(e);
            }
        }
        else if (t.Quelle == TerminQuelle.Plan)
        {
            var aendern = new ToolStripMenuItem("Ändern ab …");
            aendern.Click += (_, _) => Bearbeiten(t);
            var entfernen = new ToolStripMenuItem("Entfernen ab …");
            entfernen.Click += (_, _) => Entfernen(t);
            menue.Items.AddRange([aendern, entfernen]);
        }
        if (menue.Items.Count == 0) { menue.Dispose(); return; }
        menue.Closed += (_, _) => BeginInvoke(menue.Dispose);
        menue.Show(Raster, wo);
    }

    public void TeilnahmeSetzen(Termin t, Teilnahme status)
    {
        try { speicher.TeilnahmeSetzen(t.ZuteilungId!.Value, t.Tag, status, Kuerzel, Jetzt()); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Teilnahme", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    // termin == null: neuer Eintrag; sonst „ändern ab …“ (der bisherige endet, der neue beginnt).
    public void Bearbeiten(Termin? termin)
    {
        if (bewohner is null) return;
        var alt = termin?.PlanEintragId is long id ? speicher.PlanEintraege(bewohner.Id!.Value).FirstOrDefault(e => e.Id == id) : null;
        using var fenster = new PlanEintragFenster(speicher, bewohner, alt, termin?.Tag ?? Montag);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Entfernen(Termin termin)
    {
        if (termin.PlanEintragId is not long id) return;
        using var fenster = new DatumFenster($"„{termin.Titel}“ entfernen", "nicht mehr ab", termin.Tag, ab =>
            speicher.PlanEintragEntfernen(id, ab, Kuerzel, Jetzt()));
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void VerlaufZeigen()
    {
        if (bewohner is null) return;
        using var fenster = new TextFenster($"Verlauf Wochenplan: {bewohner.Name}",
            string.Join(Environment.NewLine, WochenplanRegeln.Verlauf(speicher.PlanEintraege(bewohner.Id!.Value))));
        Dialog.Zeigen(fenster, FindForm());
    }

    public void Drucken()
    {
        if (bewohner is null) return;
        var druck = new WochenplanDruck(bewohner, Montag, Termine);
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Wochenplan {bewohner.Name}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        leiste.MaximumSize = new Size(Width, 0);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(Math.Max(100, Width), 0)));
        Raster.SetBounds(0, leiste.Bottom + Stil.Px(this, Stil.AbstandKlein), Width, Raster.BenoetigteHoehe());
        int hoehe = Raster.Bottom;
        if (Height != hoehe) Height = hoehe;
    }
}

// Das Wochenraster: gezeichnet, Spalten Mo–So, Zeilen je Stunde. Therapien blaugrün, freie
// Einträge blaugrau, einzelne Termine (Kalender) violett. Teilnahme als Wort im Kästchen.
public sealed class WochenRaster : Control
{
    private const int KopfHoehe = 42, StundeHoehe = 44, ZeitBreite = 52, GanztagsZeile = 40;
    private DateOnly montag;
    private DateOnly heute;
    private int vonStunde = 7, bisStunde = 21;
    private readonly List<(Rectangle Feld, Termin Termin)> kaestchen = [];

    public IReadOnlyList<Termin> Termine { get; private set; } = [];
    public event Action<Termin, Point>? TerminGeklickt;

    public WochenRaster()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Stil.Flaeche;
        Font = Stil.Klein;
        AccessibleRole = AccessibleRole.Table;
    }

    private int S(int w) => Stil.Px(this, w);

    public void Setzen(DateOnly montag, IReadOnlyList<Termin> termine, DateOnly heute)
    {
        this.montag = montag;
        this.heute = heute;
        Termine = termine;
        var mitZeit = termine.Where(t => !t.Ganztags).ToList();
        vonStunde = Math.Min(7, mitZeit.Count == 0 ? 7 : mitZeit.Min(t => t.Von.Hour));
        bisStunde = Math.Max(21, mitZeit.Count == 0 ? 21 : mitZeit.Max(t => t.Bis.Minute > 0 ? t.Bis.Hour + 1 : t.Bis.Hour));
        AccessibleName = "Wochenplan: " + (termine.Count == 0 ? "keine Termine" :
            string.Join("; ", termine.Select(t => $"{t.Tag.ToString("ddd", CultureInfo.GetCultureInfo("de-DE"))} {t.ZeitText} {t.Titel}" +
                                                  (t.Quelle == TerminQuelle.Therapie ? $" ({WochenplanRegeln.TeilnahmeText(t.Teilnahme)})" : ""))));
        Invalidate();
    }

    public int BenoetigteHoehe() => S(KopfHoehe) + GanztagsHoehe() + (bisStunde - vonStunde) * S(StundeHoehe) + 1;

    // Ganztägige Termine stehen oben untereinander, je Termin eine Zeile mit Platz für zwei Textzeilen.
    private int GanztagsZeilen() => Termine.Where(t => t.Ganztags).GroupBy(t => t.Tag).Select(g => g.Count()).DefaultIfEmpty(0).Max();
    private int GanztagsHoehe() => GanztagsZeilen() == 0 ? 0 : GanztagsZeilen() * S(GanztagsZeile) + S(4);

    // Spur je Termin: gleichzeitige Termine eines Tages stehen nebeneinander, ganztägige untereinander.
    private (int Nr, int Anzahl)[] Spuren()
    {
        var spuren = new (int Nr, int Anzahl)[Termine.Count];
        foreach (var tag in Termine.Select((t, i) => (t, i)).GroupBy(x => x.t.Tag))
        {
            int ganz = 0;
            foreach (var (t, i) in tag.Where(x => x.t.Ganztags)) spuren[i] = (ganz++, 1);
            var enden = new List<TimeOnly>();
            var mitZeit = tag.Where(x => !x.t.Ganztags).OrderBy(x => x.t.Von).ToList();
            foreach (var (t, i) in mitZeit)
            {
                int nr = enden.FindIndex(e => e <= t.Von);
                if (nr < 0) { nr = enden.Count; enden.Add(t.Bis); } else enden[nr] = t.Bis;
                spuren[i] = (nr, 0);
            }
            foreach (var (_, i) in mitZeit) spuren[i] = (spuren[i].Nr, enden.Count);
        }
        return spuren;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        foreach (var (feld, termin) in kaestchen)
            if (feld.Contains(e.Location)) { TerminGeklickt?.Invoke(termin, e.Location); return; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Stil.Seite);
        kaestchen.Clear();
        var flaeche = new Rectangle(0, 0, Width - 1, Height - 1);
        Stil.FlaecheZeichnen(g, flaeche, Stil.RadiusKarte * Stil.Skala(this), Stil.Flaeche, Stil.KarteRand);
        int zeit = S(ZeitBreite), kopf = S(KopfHoehe), ganztags = GanztagsHoehe();
        var spuren = Spuren();
        float spalte = (Width - zeit - S(8)) / 7f;
        using var linie = new Pen(Stil.Trennlinie);
        var de = CultureInfo.GetCultureInfo("de-DE");
        for (int i = 0; i < 7; i++)
        {
            var tag = montag.AddDays(i);
            float x = zeit + i * spalte;
            var kopfFeld = new Rectangle((int)x, S(4), (int)spalte, kopf - S(8));
            if (tag == heute)
                using (var heuteFarbe = new SolidBrush(Stil.AkzentHell)) g.FillRectangle(heuteFarbe, x, S(1), spalte, Height - S(2));
            TextRenderer.DrawText(g, $"{tag.ToString("dddd", de)}\n{tag.ToString("dd.MM.", de)}", tag == heute ? Stil.Fett : Stil.Standard, kopfFeld,
                tag == heute ? Stil.Akzent : Stil.TextNormal, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            g.DrawLine(linie, x, kopf, x, Height - 1);
        }
        for (int h = vonStunde; h <= bisStunde; h++)
        {
            int y = kopf + ganztags + (h - vonStunde) * S(StundeHoehe);
            g.DrawLine(linie, zeit, y, Width - S(4), y);
            if (h < bisStunde)
                TextRenderer.DrawText(g, $"{h:00}:00", Stil.Klein, new Rectangle(S(4), y + S(2), zeit - S(8), S(18)), Stil.TextLeise, TextFormatFlags.Right);
        }
        for (int k = 0; k < Termine.Count; k++)
        {
            var t = Termine[k];
            int i = t.Tag.DayNumber - montag.DayNumber;
            if (i is < 0 or > 6) continue;
            float x = zeit + i * spalte + S(3);
            Rectangle feld;
            if (t.Ganztags) feld = new Rectangle((int)x, kopf + S(2) + spuren[k].Nr * S(GanztagsZeile), (int)spalte - S(6), S(GanztagsZeile) - S(4));
            else
            {
                float breite = (spalte - S(6)) / Math.Max(1, spuren[k].Anzahl);
                float y1 = kopf + ganztags + (float)((t.Von.ToTimeSpan().TotalHours - vonStunde) * S(StundeHoehe));
                float y2 = kopf + ganztags + (float)((t.Bis.ToTimeSpan().TotalHours - vonStunde) * S(StundeHoehe));
                feld = new Rectangle((int)(x + spuren[k].Nr * breite), (int)y1 + S(1), (int)breite - (spuren[k].Anzahl > 1 ? S(2) : 0),
                    Math.Max(S(20), (int)(y2 - y1) - S(2)));
            }
            var marke = t.Quelle switch { TerminQuelle.Therapie => Stil.Unterwegs, TerminQuelle.Kalender => Stil.Hinweis, _ => Stil.Zimmer };
            bool weg = t.Teilnahme is Teilnahme.Abgesagt or Teilnahme.Ausgefallen;
            Stil.FlaecheZeichnen(g, feld, Stil.RadiusKnopf * Stil.Skala(this), marke.Flaeche, marke.Rand ?? Stil.Mischen(marke.Flaeche, marke.Text, 0.25));
            // Titel, Uhrzeit, dann die Teilnahme (falls eingetragen) oder der Ort – passt auch in ein Stunden-Kästchen.
            string dritte = t.Quelle == TerminQuelle.Therapie && t.Teilnahme != Teilnahme.Offen ? WochenplanRegeln.TeilnahmeText(t.Teilnahme) : t.Ort;
            // „\r\n“: Mit TextBoxControl gilt nur dieser Zeilenumbruch.
            string text = t.Titel + (t.Ganztags ? "" : $"\r\n{t.ZeitText}") + (dritte.Length > 0 ? $"\r\n{dritte}" : "");
            // TextBoxControl: keine halb abgeschnittene letzte Zeile.
            TextRenderer.DrawText(g, text, Stil.Klein, Rectangle.Inflate(feld, -S(4), -S(2)), weg ? Stil.TextLeise : marke.Text,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl);
            kaestchen.Add((feld, t));
        }
    }
}

// Freier Eintrag der Regelwoche anlegen oder „ab Datum“ ändern. Warnung bei Überschneidung.
public sealed class PlanEintragFenster : Form
{
    private readonly Speicher speicher;
    private readonly Bewohner bewohner;
    private readonly PlanEintrag? alt;
    private string? gewarnt;

    public TextBox Titel { get; } = new() { Width = 300 };
    public ComboBox Wochentag { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    public TextBox Von { get; } = new() { Width = 70, PlaceholderText = "SS:MM" };
    public TextBox Bis { get; } = new() { Width = 70, PlaceholderText = "SS:MM" };
    public TextBox Ort { get; } = new() { Width = 260 };
    public TextBox Hinweis { get; } = new() { Width = 360 };
    public TextBox Ab { get; } = new() { Width = 100 };
    public Label Warnung { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public PlanEintragFenster(Speicher speicher, Bewohner bewohner, PlanEintrag? alt, DateOnly vorschlag)
    {
        this.speicher = speicher;
        this.bewohner = bewohner;
        this.alt = alt;
        Text = (alt is null ? "Neuer Eintrag im Wochenplan" : "Eintrag ändern") + $" – {bewohner.Name} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        zeile("Was", Titel);
        Wochentag.Items.AddRange(TherapieRegeln.Woche.Select(t => (object)TherapieRegeln.TagName(t)).ToArray());
        zeile("Jede Woche am", Formular.Reihe(Wochentag, Von, new Label { Text = "bis", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, Bis));
        zeile("Ort", Ort);
        zeile("Hinweis", Hinweis);
        var heute = DateOnly.FromDateTime(Jetzt());
        Ab.Text = Datum.Text(vorschlag > heute ? vorschlag : heute);
        zeile(alt is null ? "Gilt ab" : "Änderung gilt ab", Ab);
        if (alt is not null)
        {
            Titel.Text = alt.Titel;
            Wochentag.SelectedIndex = Array.IndexOf(TherapieRegeln.Woche, alt.Termin.Tag);
            Von.Text = alt.Termin.Von.ToString("HH:mm");
            Bis.Text = alt.Termin.Bis.ToString("HH:mm");
            Ort.Text = alt.Ort;
            Hinweis.Text = alt.Hinweis;
        }
        else Wochentag.SelectedIndex = Array.IndexOf(TherapieRegeln.Woche, vorschlag.DayOfWeek);
        zeile(null, new Label { Text = "Gilt jede Woche gleich. Frühere Fassungen bleiben im Verlauf.", AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise });
        zeile(null, Warnung);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        Fehler.Visible = false;
        try
        {
            var neu = WochenplanRegeln.AusEingabe(new PlanEingabe
            {
                Tag = Wochentag.SelectedIndex >= 0 ? TherapieRegeln.Woche[Wochentag.SelectedIndex] : null, Von = Von.Text, Bis = Bis.Text,
                Titel = Titel.Text, Ort = Ort.Text, Hinweis = Hinweis.Text, Ab = Ab.Text, Kuerzel = Einstellungen.Laden().Kuerzel,
            }, bewohner.Id!.Value, Jetzt()) with { Ersetzt = alt?.Id };
            var konflikte = speicher.PlanUeberschneidungen(neu);
            string auswahl = $"{neu.Termin}|{neu.Ab}";
            if (konflikte.Count > 0 && gewarnt != auswahl)
            {
                gewarnt = auswahl;
                Warnung.Text = "Überschneidung mit: " + string.Join("; ", konflikte) + ". Noch einmal „Speichern“ speichert trotzdem.";
                Warnung.Visible = true;
                return false;
            }
            if (alt is null) speicher.PlanEintragAnlegen(neu);
            else speicher.PlanEintragAendern(alt.Id, neu);
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Kleines Fenster mit einem Datum (z. B. „nicht mehr ab“).
public sealed class DatumFenster : Form
{
    private readonly Action<DateOnly> speichern;
    public TextBox Datumsfeld { get; } = new() { Width = 100 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    public DatumFenster(string titel, string beschriftung, DateOnly vorschlag, Action<DateOnly> speichern)
    {
        this.speichern = speichern;
        Text = $"{titel} ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        Datumsfeld.Text = Datum.Text(vorschlag);
        zeile(beschriftung, Datumsfeld);
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    public bool Speichern()
    {
        try
        {
            DateOnly d;
            try { d = Datum.Lesen(Datumsfeld.Text, "Datum"); }
            catch (FormatException x) { throw new Eingabefehler([x.Message]); }
            speichern(d);
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join("\n", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}

// Ausdruck: A4 quer, große Schrift, nur Name und Plan (keine Allergien, Medikation oder andere
// Angaben aus der Akte). Je Tag eine Spalte mit den Terminen untereinander.
public sealed class WochenplanDruck(Bewohner bewohner, DateOnly montag, IReadOnlyList<Termin> termine)
{
    public PrintDocument DokumentBauen()
    {
        var dokument = new PrintDocument { DocumentName = $"Wochenplan {bewohner.Name}" };
        dokument.DefaultPageSettings.Landscape = true;
        dokument.PrintPage += (_, e) => { SeiteZeichnen(e.Graphics!); e.HasMorePages = false; };
        return dokument;
    }

    public void SeiteZeichnen(Graphics g)
    {
        g.PageUnit = GraphicsUnit.Millimeter;
        const float links = 12f, oben = 12f, breite = 273f, hoehe = 186f;
        var de = CultureInfo.GetCultureInfo("de-DE");
        using var titel = new Font("Segoe UI Semibold", 20F);
        using var tagSchrift = new Font("Segoe UI Semibold", 13F);
        using var zeitSchrift = new Font("Segoe UI Semibold", 10.5F);
        using var textSchrift = new Font("Segoe UI", 11F);
        using var klein = new Font("Segoe UI", 8F);
        using var linie = new Pen(Color.Gray, 0.3f);
        g.DrawString($"Wochenplan – {bewohner.Name}", titel, Brushes.Black, links, oben);
        g.DrawString(WochenplanRegeln.WochenText(montag), textSchrift, Brushes.Black, links, oben + 10f);
        float y0 = oben + 20f, spalte = breite / 7f;
        for (int i = 0; i < 7; i++)
        {
            var tag = montag.AddDays(i);
            float x = links + i * spalte;
            g.DrawRectangle(linie, x, y0, spalte, hoehe - y0 + oben - 6f);
            g.DrawString(tag.ToString("dddd", de), tagSchrift, Brushes.Black, x + 1.5f, y0 + 1.5f);
            g.DrawString(tag.ToString("dd.MM.", de), textSchrift, Brushes.DimGray, x + 1.5f, y0 + 8f);
            float y = y0 + 16f;
            foreach (var t in termine.Where(t => t.Tag == tag))
            {
                g.DrawString(t.ZeitText, zeitSchrift, Brushes.Black, x + 1.5f, y);
                y += 5.5f;
                string text = t.Titel + (t.Ort.Length > 0 ? $"\n{t.Ort}" : "");
                var groesse = g.MeasureString(text, textSchrift, (int)(spalte - 3f));
                g.DrawString(text, textSchrift, Brushes.Black, new RectangleF(x + 1.5f, y, spalte - 3f, groesse.Height));
                y += groesse.Height + 3f;
            }
        }
        g.DrawString(KopfBereich.DemoHinweis, klein, Brushes.DimGray, links, oben + hoehe - 4f);
    }
}
