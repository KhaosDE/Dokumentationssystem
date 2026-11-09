using DokuLine.Kern;

namespace DokuLine;

// Ein Kasten in „Heute wichtig“: Überschrift in Großbuchstaben mit Zähler, darunter die
// Einträge. Ohne Einträge steht „Nichts offen“ da; der Kasten verschwindet nicht.
public sealed class HeuteKasten : KartenFlaeche
{
    public KastenKopf Kopf { get; }
    public Label Leer { get; } = new() { Text = HeuteWichtigRegeln.NichtsOffen, AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    public List<Control> Zeilen { get; } = [];
    public List<Control> Oben { get; } = [];    // feste Teile über den Einträgen (z. B. Fortschritt)
    public List<Control> Unten { get; } = [];   // feste Teile darunter (z. B. „Alle Aufgaben …“)
    public string Titel { get; }
    public int Anzahl { get; private set; }
    public string KopfText => Kopf.Zaehler.Length > 0 ? $"{Titel} · {Kopf.Zaehler}" : Titel;
    public event Action? KopfGeklickt;

    public HeuteKasten(string titel)
    {
        Titel = titel;
        Kopf = new KastenKopf(titel);
        Kopf.Click += (_, _) => KopfGeklickt?.Invoke();
        Controls.Add(Kopf);
        Controls.Add(Leer);
        Padding = new Padding(Stil.AbstandMittel);
    }

    public void Setzen(int anzahl, IEnumerable<Control> zeilen, string leer = HeuteWichtigRegeln.NichtsOffen)
    {
        foreach (var c in Oben.Concat(Unten)) if (c.Parent != this) Controls.Add(c);
        foreach (var z in Zeilen) { Controls.Remove(z); z.Dispose(); }
        Zeilen.Clear();
        Zeilen.AddRange(zeilen);
        foreach (var z in Zeilen) Controls.Add(z);
        Anzahl = anzahl;
        Kopf.ZaehlerSetzen(anzahl.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Leer.Text = leer;
        Leer.Visible = Zeilen.Count == 0;
        AccessibleName = $"{Titel}: {anzahl}";
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandMittel), mini = Stil.Px(this, Stil.AbstandMini);
        int breite = Math.Max(100, Width - 2 * innen);
        Kopf.SetBounds(1, 1, Math.Max(10, Width - 2), Kopf.Hoehe);   // innerhalb des Rands, oben abgerundet wie der Kasten
        int y = Kopf.Bottom + Stil.Px(this, Stil.AbstandKlein);
        void Setzen(Control c)
        {
            switch (c)
            {
                case Label l when l.AutoSize: l.MaximumSize = new Size(breite, 0); l.Location = new Point(innen, y); break;
                case CheckBox k:
                    int hoch = TextRenderer.MeasureText(k.Text, k.Font, new Size(breite - Stil.Px(this, 22), 0), TextFormatFlags.WordBreak).Height + Stil.Px(this, 6);
                    k.SetBounds(innen, y, breite, Math.Max(Stil.Px(this, 24), hoch));
                    break;
                case HeuteEintrag e: e.Width = breite; e.Location = new Point(innen, y); e.PerformLayout(); break;
                default: c.SetBounds(innen, y, c is ProgressBar ? breite : c.Width, c.Height); break;
            }
            y = c.Bottom + mini;
        }
        foreach (var c in Oben) Setzen(c);
        if (Leer.Visible) Setzen(Leer);
        foreach (var z in Zeilen) Setzen(z);
        foreach (var c in Unten) Setzen(c);
        int hoehe = y - mini + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Ein Eintrag in einem Kasten: Text (umbrechend), darunter klein z. B. „seit 07.11. · AB“, rechts
// ein Status als Wort mit Farbe. Klick öffnet die Stelle, an die er gehört.
public sealed class HeuteEintrag : Panel
{
    public Label Text1 { get; } = new() { AutoSize = true, ForeColor = Stil.TextNormal, BackColor = Stil.Flaeche, UseMnemonic = false };
    public Label? Text2 { get; }
    public MarkenFeld? Status { get; }
    // Rand in der Farbe einer Schicht (Übergaben); ohne Rand null.
    public Color? Rahmen { get; init; }
    public event Action? Geklickt;

    public HeuteEintrag(string text, string? unten, (string Text, Stil.Marke Marke)? status)
    {
        BackColor = Stil.Flaeche;
        Cursor = Cursors.Hand;
        ResizeRedraw = true;
        Text1.Text = text;
        Controls.Add(Text1);
        if (unten is not null)
        {
            Text2 = new Label { Text = unten, AutoSize = true, Font = Stil.Klein, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche, UseMnemonic = false };
            Controls.Add(Text2);
        }
        if (status is { } s)
        {
            Status = new MarkenFeld(s.Text, s.Marke);
            Controls.Add(Status);
        }
        AccessibleRole = AccessibleRole.Link;
        AccessibleName = text + (unten is null ? "" : $", {unten}") + (status is { } st ? $", {st.Text}" : "");
        foreach (Control c in Controls) { c.Cursor = Cursors.Hand; c.Click += (_, _) => Klicken(); }
        Click += (_, _) => Klicken();
    }

    public void Klicken() => Geklickt?.Invoke();

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Rahmen is null ? 0 : Stil.Px(this, Stil.AbstandKlein), oben = Rahmen is null ? 0 : Stil.Px(this, Stil.AbstandMini);
        int statusBreite = Status is null ? 0 : Status.PreferredSize.Width + Stil.Px(this, Stil.AbstandKlein);
        Text1.MaximumSize = new Size(Math.Max(60, Width - statusBreite - 2 * innen), 0);
        Text1.Location = new Point(innen, oben);
        int y = Text1.Bottom;
        if (Text2 is not null) { Text2.Location = new Point(innen, y); y = Text2.Bottom; }
        if (Status is not null) Status.Bounds = new Rectangle(Width - innen - Status.PreferredSize.Width, oben, Status.PreferredSize.Width, Status.PreferredSize.Height);
        int hoehe = Math.Max(y, Status?.Bottom ?? 0) + Stil.Px(this, 2) + oben;
        if (Height != hoehe) Height = hoehe;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Rahmen is not Color farbe) return;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float stark = 1.5f * DeviceDpi / 96f;
        using var pfad = Stil.Abgerundet(new RectangleF(stark / 2, stark / 2, Width - stark - 1, Height - stark - 1), Stil.Px(this, Stil.RadiusKarte));
        using var stift = new Pen(farbe, stark);
        e.Graphics.DrawPath(stift, pfad);
    }
}
