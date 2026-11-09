using System.Drawing.Drawing2D;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Akte, Reiter „Vitalwerte“ (Bauplan 36): Verlaufslinie je Wert und Tabelle (neueste oben) für einen wählbaren Zeitraum.
// Hinweise: „außerhalb des angeordneten Bereichs“ in Gefahr-Farbe (nur nach ärztlicher Anordnung), sonst leise in
// Achtung-Farbe „über/unter dem allgemeinen Richtwert“. Doppelklick auf eine Zeile: Korrektur mit Grund.
public sealed class VitalwerteAnsicht : KartenFlaeche
{
    public static readonly (string Text, int Tage)[] Zeitraeume = [("30 Tage", 30), ("3 Monate", 92), ("12 Monate", 366), ("alle", 100000)];
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel kopf = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    public Button ErfassenKnopf { get; } = Stil.HauptKnopf("Vitalwerte erfassen …");
    public Button GrenzenKnopf { get; } = Stil.NebenKnopf("Grenzen und Messaufträge …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public ComboBox Zeitraum { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    public ComboBox VerlaufArt { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    public VerlaufBild Verlauf { get; } = new();
    public ListView Tabelle { get; } = new() { View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable, ShowItemToolTips = true };
    public Label Angeordnet { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label Auftraege { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label BmiText { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Bewohner? Bewohner { get; private set; }
    public List<Vitalwert> Gezeigt { get; private set; } = [];
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;
    internal Action<Func<System.Drawing.Printing.PrintDocument>, string> Vorschau { get; set; }
    public event Action? Geaendert;

    public VitalwerteAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        Vorschau = (bauen, titel) => Druckvorschau.Zeigen(FindForm()!, bauen, titel);
        Zeitraum.Items.AddRange([.. Zeitraeume.Select(z => z.Text)]);
        Zeitraum.SelectedIndex = 1;
        Zeitraum.AccessibleName = "Zeitraum";
        VerlaufArt.AccessibleName = "Verlauf für";
        foreach (var (titel, breite) in new[] { ("Zeit", 130), ("Wert", 220), ("Hinweis", 260), ("Bemerkung", 220), ("Kürzel", 60) }) Tabelle.Columns.Add(titel, breite);
        foreach (var k in new Control[] { ErfassenKnopf, GrenzenKnopf, DruckKnopf, Zeitraum, VerlaufArt }) k.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
        kopf.Controls.AddRange([ErfassenKnopf, GrenzenKnopf, DruckKnopf, Zeitraum, VerlaufArt]);
        ErfassenKnopf.Click += (_, _) => Erfassen();
        GrenzenKnopf.Click += (_, _) => GrenzenOeffnen();
        DruckKnopf.Click += (_, _) => Drucken();
        Zeitraum.SelectedIndexChanged += (_, _) => { if (Bewohner is not null) Zeigen(Bewohner); };
        VerlaufArt.SelectedIndexChanged += (_, _) => VerlaufZeigen();
        Tabelle.DoubleClick += (_, _) => { if (Tabelle.SelectedItems.Count == 1 && Tabelle.SelectedItems[0].Tag is Vitalwert w) Korrigieren(w); };
        Controls.AddRange([kopf, Verlauf, Tabelle, Angeordnet, Auftraege, BmiText]);
    }

    private DateTime Ab => Jetzt().AddDays(-Zeitraeume[Math.Max(0, Zeitraum.SelectedIndex)].Tage);

    public void Zeigen(Bewohner b)
    {
        Bewohner = b;
        long id = b.Id!.Value;
        var r = speicher.Richtwerte();
        var grenzen = speicher.Grenzen(id);
        Gezeigt = [.. speicher.Vitalwerte(id).Where(w => w.Zeit >= Ab)];
        var arten = speicher.Vitalarten().Concat(Gezeigt.Select(w => w.Art)).Distinct().ToList();
        string? vorher = VerlaufArt.SelectedItem as string;
        VerlaufArt.Items.Clear();
        VerlaufArt.Items.AddRange([.. arten]);
        VerlaufArt.SelectedIndex = Math.Max(0, arten.IndexOf(vorher ?? VitalRegeln.Blutdruck));
        Tabelle.BeginUpdate();
        Tabelle.Items.Clear();
        foreach (var w in Gezeigt)
        {
            var hinweis = VitalRegeln.Bewerten(w, grenzen.GetValueOrDefault(w.Art), r);
            string bemerkung = w.Grund.Length > 0 ? $"{w.Bemerkung} (korrigiert: {w.Grund})".Trim() : w.Bemerkung;
            var item = new ListViewItem([w.Zeit.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), $"{w.Art} {VitalRegeln.WertText(w, r)}",
                hinweis?.Text ?? "", bemerkung, w.Kuerzel])
            { Tag = w, UseItemStyleForSubItems = false };
            if (hinweis is not null)
            {
                item.SubItems[2].ForeColor = hinweis.Angeordnet ? Stil.Beschluss.Text : Stil.Achtung.Text;
                item.ToolTipText = hinweis.Angeordnet ? $"{VitalRegeln.Ausserhalb} (ärztlich angeordnet)" : VitalRegeln.RichtwertTooltip;
            }
            Tabelle.Items.Add(item);
        }
        Tabelle.EndUpdate();
        var arzt = AngabenAnsicht.PersonText(speicher);
        Angeordnet.Text = grenzen.Count == 0 ? "Angeordnete Bereiche: keine (es gelten nur die allgemeinen Richtwerte als Hinweis)."
            : "Angeordnete Bereiche: " + string.Join(" · ", grenzen.Values.Select(g => VitalRegeln.GrenzeText(g, arzt)));
        var auftraege = speicher.Messauftraege(id);
        Auftraege.Text = auftraege.Count == 0 ? "Messaufträge: keine."
            : "Messaufträge: " + string.Join(" · ", auftraege.Select(m => $"{m.Art} {VitalRegeln.RhythmusText(m)}, {BerichtRegeln.SchichtText(m.Schicht)}"));
        BmiText.Text = BmiZeile(id, r);
        Stil.Sperren(ErfassenKnopf, Recht.Grund(speicher, Funktionen.VitalwerteErfassen));
        Stil.Sperren(GrenzenKnopf, Recht.Grund(speicher, Funktionen.VitalGrenzen));
        VerlaufZeigen();
        PerformLayout();
    }

    // BMI aus dem letzten Gewicht und der Größe der Personenbeschreibung.
    private string BmiZeile(long id, VitalRichtwerte r)
    {
        var groesse = speicher.Angaben(id, DateOnly.FromDateTime(Jetzt())).GetValueOrDefault(Angabe.Personenbeschreibung)?.Lesen<Personenbeschreibung>()?.Groesse;
        var gewicht = speicher.Vitalwerte(id).FirstOrDefault(w => w.Art == VitalRegeln.Gewicht);
        if (gewicht is null) return "BMI: kein Gewicht erfasst.";
        if (groesse is null || VitalRegeln.BmiWert(gewicht.Wert, groesse) is not double bmi) return "BMI: Größe fehlt (Personenbeschreibung in den Stammdaten).";
        var hinweis = VitalRegeln.BmiHinweis(bmi, r);
        return $"BMI: {VitalRegeln.Zahl(bmi)} (aus {VitalRegeln.WertText(gewicht, r)} und {groesse})" + (hinweis is null ? "" : $" – {hinweis.Text}");
    }

    private void VerlaufZeigen()
    {
        if (Bewohner is null || VerlaufArt.SelectedItem is not string art) return;
        var r = speicher.Richtwerte();
        Verlauf.Setzen(art, [.. Gezeigt.Where(w => w.Art == art).OrderBy(w => w.Zeit).Select(w => (w.Zeit, VitalRegeln.Anzeige(art, w.Wert, r), w.Wert2))],
            speicher.Grenzen(Bewohner.Id!.Value).GetValueOrDefault(art), VitalRegeln.Einheit(art, r));
    }

    public bool Erfassen()
    {
        if (Bewohner is null || Stil.Sperrgrund(ErfassenKnopf) is not null) return false;
        using var f = new VitalwertFenster(speicher, Bewohner, null);
        if (Dialog.Zeigen(f, FindForm()) != DialogResult.OK) return false;
        Zeigen(Bewohner);
        Geaendert?.Invoke();
        return true;
    }

    public bool Korrigieren(Vitalwert w)
    {
        if (Bewohner is null || Stil.Sperrgrund(ErfassenKnopf) is not null) return false;
        using var f = new VitalwertFenster(speicher, Bewohner, w);
        if (Dialog.Zeigen(f, FindForm()) != DialogResult.OK) return false;
        Zeigen(Bewohner);
        Geaendert?.Invoke();
        return true;
    }

    public void GrenzenOeffnen()
    {
        if (Bewohner is null || Stil.Sperrgrund(GrenzenKnopf) is not null) return;
        using var f = new VitalGrenzenFenster(speicher, Bewohner);
        Dialog.Zeigen(f, FindForm());
        Zeigen(Bewohner);
        Geaendert?.Invoke();
    }

    // Für den Arztbesuch: je Wert die Messungen des Zeitraums.
    public List<UeberleitungAbschnitt> DruckInhalt()
    {
        if (Bewohner is null) return [];
        var r = speicher.Richtwerte();
        var grenzen = speicher.Grenzen(Bewohner.Id!.Value);
        var liste = new List<UeberleitungAbschnitt>
        {
            new("Bewohner", [$"{Bewohner.Name}, geboren {Datum.Text(Bewohner.Geburtsdatum)}", $"Zeitraum: {Zeitraum.Text} bis {Datum.Text(DateOnly.FromDateTime(Jetzt()))}",
                Angeordnet.Text, BmiText.Text]),
        };
        foreach (var g in Gezeigt.GroupBy(w => w.Art))
            liste.Add(new(g.Key, [.. g.Select(w => $"{w.Zeit.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}   {VitalRegeln.WertText(w, r)}" +
                (VitalRegeln.Bewerten(w, grenzen.GetValueOrDefault(w.Art), r) is VitalHinweis h ? $"   ({h.Text})" : "") +
                (w.Bemerkung.Length > 0 ? $"   {w.Bemerkung}" : "") + $"   {w.Kuerzel}")]));
        if (liste.Count == 1) liste.Add(new("Werte", ["Keine Werte im Zeitraum."]));
        liste.Add(new("Hinweis", [VitalRegeln.RichtwertVermerk]));
        return liste;
    }

    public bool Drucken()
    {
        if (Bewohner is null) return false;
        var inhalt = DruckInhalt();
        string titel = $"Vitalwerte {Bewohner.Name}";
        speicher.Zugriff(Recht.Kuerzel, Bewohner.Id, ZugriffRegeln.Gedruckt, "Vitalwerte");
        Vorschau(() => new UeberleitungDruck(titel, inhalt).DokumentBauen(), titel);
        return true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen), luecke = Stil.Px(this, Stil.AbstandKlein);
        kopf.MaximumSize = new Size(breite, 0);
        kopf.Bounds = new Rectangle(new Point(innen, innen), kopf.GetPreferredSize(new Size(breite, 0)));
        Verlauf.Bounds = new Rectangle(innen, kopf.Bottom + luecke, breite, Stil.Px(this, 200));
        int zeilen = Math.Clamp(Tabelle.Items.Count, 3, 14);
        Tabelle.Bounds = new Rectangle(innen, Verlauf.Bottom + luecke, breite, Stil.Px(this, 26) + zeilen * Stil.Px(this, 20));
        int y = Tabelle.Bottom + luecke;
        foreach (var l in new[] { Angeordnet, Auftraege, BmiText })
        {
            l.MaximumSize = new Size(breite, 0);
            l.Location = new Point(innen, y);
            y = l.Bottom + Stil.Px(this, Stil.AbstandMini);
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Verlaufslinie eines Werts; beim Blutdruck zwei Linien. Ein angeordneter Bereich erscheint als helles Band.
public sealed class VerlaufBild : Control
{
    private string art = "", einheit = "";
    private List<(DateTime Zeit, double Wert, double? Wert2)> punkte = [];
    private VitalGrenze? grenze;
    public int Punkte => punkte.Count;

    public VerlaufBild()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Stil.Flaeche;
        AccessibleRole = AccessibleRole.Graphic;
    }

    public void Setzen(string art, List<(DateTime, double, double?)> punkte, VitalGrenze? grenze, string einheit)
    {
        (this.art, this.punkte, this.grenze, this.einheit) = (art, punkte, grenze, einheit);
        AccessibleName = punkte.Count == 0 ? $"Verlauf {art}: keine Werte" : $"Verlauf {art}: {punkte.Count} Werte";
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var rahmen = new Pen(Stil.Trennlinie);
        g.DrawRectangle(rahmen, 0, 0, Width - 1, Height - 1);
        if (punkte.Count == 0)
        {
            TextRenderer.DrawText(g, $"{art}: keine Werte im Zeitraum", Stil.Standard, ClientRectangle, Stil.TextLeise, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        var werte = punkte.SelectMany(p => p.Wert2 is double w2 ? new[] { p.Wert, w2 } : [p.Wert]).ToList();
        if (grenze is { Gilt: true } gr) werte.AddRange(new[] { gr.Von, gr.Bis, gr.Von2, gr.Bis2 }.OfType<double>());
        double min = werte.Min(), max = werte.Max();
        if (max - min < 1) { min -= 1; max += 1; }
        double spanne = max - min;
        min -= spanne * 0.1; max += spanne * 0.1;
        int links = Stil.Px(this, 48), rechts = Stil.Px(this, 12), oben = Stil.Px(this, 22), unten = Stil.Px(this, 22);
        var flaeche = new Rectangle(links, oben, Math.Max(10, Width - links - rechts), Math.Max(10, Height - oben - unten));
        DateTime von = punkte[0].Zeit, bis = punkte[^1].Zeit;
        double sekunden = Math.Max(1, (bis - von).TotalSeconds);
        float X(DateTime t) => flaeche.Left + (float)((t - von).TotalSeconds / sekunden * flaeche.Width);
        float Y(double w) => flaeche.Bottom - (float)((w - min) / (max - min) * flaeche.Height);
        if (grenze is { Gilt: true } band)
            using (var pinsel = new SolidBrush(Color.FromArgb(40, Stil.Offen.Text)))
            {
                foreach (var (a, b) in new[] { (band.Von, band.Bis), (band.Von2, band.Bis2) })
                    if (a is not null || b is not null)
                    {
                        float y1 = Y(b ?? max), y2 = Y(a ?? min);
                        g.FillRectangle(pinsel, flaeche.Left, y1, flaeche.Width, Math.Max(1, y2 - y1));
                    }
            }
        TextRenderer.DrawText(g, $"{art} ({einheit})", Stil.Klein, new Point(Stil.Px(this, 6), Stil.Px(this, 4)), Stil.TextMittel);
        foreach (var w in new[] { max, (max + min) / 2, min })
            TextRenderer.DrawText(g, VitalRegeln.Zahl(Math.Round(w, 1)), Stil.Klein, new Point(Stil.Px(this, 4), (int)Y(w) - Stil.Px(this, 7)), Stil.TextLeise);
        TextRenderer.DrawText(g, von.ToString("dd.MM.", CultureInfo.InvariantCulture), Stil.Klein, new Point(flaeche.Left, flaeche.Bottom + Stil.Px(this, 3)), Stil.TextLeise);
        TextRenderer.DrawText(g, bis.ToString("dd.MM.", CultureInfo.InvariantCulture), Stil.Klein, new Point(flaeche.Right - Stil.Px(this, 36), flaeche.Bottom + Stil.Px(this, 3)), Stil.TextLeise);
        void Linie(Func<(DateTime Zeit, double Wert, double? Wert2), double?> wert, Color farbe)
        {
            var p = punkte.Where(x => wert(x) is not null).Select(x => new PointF(X(x.Zeit), Y(wert(x)!.Value))).ToArray();
            using var stift = new Pen(farbe, Stil.Px(this, 2));
            if (p.Length > 1) g.DrawLines(stift, p);
            using var punkt = new SolidBrush(farbe);
            float r = Stil.Px(this, 3);
            foreach (var q in p) g.FillEllipse(punkt, q.X - r, q.Y - r, 2 * r, 2 * r);
        }
        Linie(x => x.Wert, Stil.Akzent);
        if (punkte.Any(x => x.Wert2 is not null)) Linie(x => x.Wert2, Stil.TextMittel);
    }
}
