using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Eine Aufgabe: Marken (überfällig/fällig bis, für wen, jetzt dran), Text, Bewohner und
// Herkunft, Weitergaben mit Notiz, Knöpfe „Erledigt“ und „Weitergeben …“.
public sealed class AufgabeKarte : KartenFlaeche
{
    private readonly FlowLayoutPanel marken = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Flaeche, Margin = new Padding(0) };
    private readonly Label text = new();
    private readonly Label herkunft = new();
    private readonly List<Label> verlauf = [];
    private readonly FlowLayoutPanel knoepfe = new() { AutoSize = true, WrapContents = false, BackColor = Stil.Flaeche, Margin = new Padding(0) };

    public Aufgabe Aufgabe { get; }
    public Button Erledigt { get; } = Stil.NebenKnopf("Erledigt");
    public Button Weitergeben { get; } = Stil.NebenKnopf("An die nächste Schicht weitergeben …");
    public Button SchliessenKnopf { get; } = Stil.NebenKnopf("Schließen mit Begründung …");
    public Button AktionKnopf { get; } = Stil.NebenKnopf("");   // Aufgabe aus einer Visite
    public IReadOnlyList<string> MarkenTexte { get; }
    public IEnumerable<string> VerlaufTexte => verlauf.Select(l => l.Text);

    public event Action<Aufgabe>? ErledigtGeklickt;
    public event Action<Aufgabe>? WeitergebenGeklickt;
    public event Action<Aufgabe>? SchliessenGeklickt;
    public event Action<Aufgabe>? AktionGeklickt;

    // aktion: Knopf für eine Aufgabe aus einer Visite (Text und ggf. Grund, warum gesperrt).
    public AufgabeKarte(Aufgabe a, string? bewohner, DateTime jetzt, string kuerzel, (string Text, string? Sperrgrund)? aktion = null)
    {
        Aufgabe = a;
        var liste = new List<(string, Stil.Marke)>();
        if (a.Offen)
        {
            if (a.FaelligBis is not null)
                liste.Add((AufgabenRegeln.FaelligText(a, jetzt), AufgabenRegeln.Ueberfaellig(a, jetzt) ? Stil.Beschluss : Stil.Zimmer));
            liste.Add((AufgabenRegeln.FuerText(a), Stil.Zimmer));
            if (AufgabenRegeln.AusSchichtOffen(a, jetzt) is string aus) liste.Insert(0, (aus, Stil.Achtung));
            else if (AufgabenRegeln.JetztDran(a, jetzt)) liste.Add(("jetzt dran", Stil.Unterwegs));
        }
        else if (a.Geschlossen)
        {
            liste.Add(($"geschlossen {BerichtRegeln.ZeitText(a.Erledigt!.Value)} · {a.ErledigtVon}: {a.ErledigtNotiz}", Stil.Zimmer));
        }
        else
        {
            liste.Add(($"erledigt {BerichtRegeln.ZeitText(a.Erledigt!.Value)} · {a.ErledigtVon}", Stil.Offen));
        }
        if (a.VorlageId is not null) liste.Add(("Standardaufgabe", Stil.Hinweis));
        MarkenTexte = liste.Select(m => m.Item1).ToList();
        foreach (var (t, m) in liste) marken.Controls.Add(new MarkenFeld(t, m) { Margin = new Padding(0, 0, Stil.AbstandMini, Stil.AbstandMini) });

        text.Text = a.Text;
        text.Font = Stil.KarteName;
        text.ForeColor = a.Offen ? Stil.TextNormal : Stil.TextLeise;
        herkunft.Text = (bewohner is null ? "Wohnbereich" : bewohner) +
                        $" · angelegt von {a.Verfasser}, {BerichtRegeln.ZeitText(a.Erstellt)}" +
                        (a.VisiteAnordnungId is not null || a.VisiteEintragId is not null ? " · aus einer Visite"
                            : a.BerichtId is not null ? " · aus einem Bericht" : "");
        herkunft.Font = Stil.Klein;
        herkunft.ForeColor = Stil.TextLeise;
        foreach (var w in a.Weitergaben)
            verlauf.Add(new Label
            {
                Text = $"weitergegeben von {w.Kuerzel}, {BerichtRegeln.ZeitText(w.Zeit)}" + (w.Notiz.Length > 0 ? $": {w.Notiz}" : ""),
                Font = Stil.Klein, ForeColor = Stil.Hinweis.Text,
            });
        foreach (var label in new[] { text, herkunft }.Concat(verlauf))
        {
            label.AutoSize = true;
            label.BackColor = Stil.Flaeche;
            label.UseMnemonic = false;
            Controls.Add(label);
        }

        Erledigt.Enabled = Weitergeben.Enabled = a.Offen && kuerzel.Length > 0;
        Erledigt.Visible = Weitergeben.Visible = a.Offen;
        SchliessenKnopf.Enabled = Erledigt.Enabled;
        SchliessenKnopf.Visible = a.Offen && a.VorlageId is not null;   // Standardaufgaben: begründet schließen
        SchliessenKnopf.Click += (_, _) => SchliessenGeklickt?.Invoke(a);
        Erledigt.Click += (_, _) => ErledigtGeklickt?.Invoke(a);
        Weitergeben.Click += (_, _) => WeitergebenGeklickt?.Invoke(a);
        AktionKnopf.Visible = a.Offen && aktion is not null;
        if (aktion is { } x)
        {
            AktionKnopf.Text = x.Text;
            AktionKnopf.AutoSize = true;
            Stil.Sperren(AktionKnopf, x.Sperrgrund);
        }
        AktionKnopf.Click += (_, _) => AktionGeklickt?.Invoke(a);
        foreach (var k in new[] { AktionKnopf, Erledigt, Weitergeben, SchliessenKnopf })
        {
            k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
            knoepfe.Controls.Add(k);
        }
        Controls.Add(marken);
        Controls.Add(knoepfe);
        AccessibleName = $"Aufgabe: {a.Text}, {string.Join(", ", MarkenTexte)}";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Überfällige Aufgaben: roter Balken links (zusätzlich zum Wort „überfällig“).
        if (MarkenTexte?.Count > 0 && MarkenTexte[0].StartsWith("überfällig"))
        {
            using var balken = new SolidBrush(Stil.Beschluss.Text);
            e.Graphics.FillRectangle(balken, Stil.Px(this, 2), Stil.Px(this, Stil.RadiusKarte), Stil.Px(this, 4), Height - 2 * Stil.Px(this, Stil.RadiusKarte));
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross), breite = Math.Max(100, Width - 2 * innen);
        int y = innen;
        marken.MaximumSize = new Size(breite, 0);
        marken.Bounds = new Rectangle(new Point(innen, y), marken.GetPreferredSize(new Size(breite, 0)));
        y = marken.Bottom + Stil.Px(this, Stil.AbstandMini);
        foreach (var label in new[] { text, herkunft }.Concat(verlauf))
        {
            label.MaximumSize = new Size(breite, 0);
            label.Location = new Point(innen, y);
            y = label.Bottom + Stil.Px(this, 2);
        }
        if (Aufgabe?.Offen == true)   // OnLayout läuft schon im Konstruktor der Basisklasse, vor dem Setzen
        {
            knoepfe.Bounds = new Rectangle(new Point(innen, y + Stil.Px(this, Stil.AbstandKlein)), knoepfe.GetPreferredSize(new Size(breite, 0)));
            y = knoepfe.Bottom;
        }
        int hoehe = y + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Kurzliste auf dem Startbildschirm (über den Bewohnerkarten): die ersten offenen Aufgaben,
// überfällige zuerst, und ein Knopf zu allen.
public sealed class AufgabenKurzliste : KartenFlaeche
{
    public const int Hoechstens = 3;
    private readonly Label kopf = new() { AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Flaeche };
    private readonly List<(MarkenFeld Marke, Label Text)> zeilen = [];
    public Button AlleKnopf { get; } = Stil.NebenKnopf("Alle Aufgaben …");
    public IEnumerable<string> ZeilenTexte => zeilen.Select(z => $"{z.Marke.Text} · {z.Text.Text}");
    public string KopfText => kopf.Text;

    public AufgabenKurzliste()
    {
        Controls.Add(kopf);
        Controls.Add(AlleKnopf);
        Padding = new Padding(Stil.AbstandMittel);
    }

    public void Setzen(IReadOnlyList<Aufgabe> offen, IReadOnlyDictionary<long, string> namen, DateTime jetzt)
    {
        foreach (var (m, t) in zeilen) { Controls.Remove(m); Controls.Remove(t); m.Dispose(); t.Dispose(); }
        zeilen.Clear();
        int ueberfaellig = offen.Count(a => AufgabenRegeln.Ueberfaellig(a, jetzt));
        kopf.Text = $"AUFGABEN · {offen.Count} OFFEN" + (ueberfaellig > 0 ? $" · {ueberfaellig} ÜBERFÄLLIG" : "");
        foreach (var a in offen.Take(Hoechstens))
        {
            var (markenText, marke) = AufgabenRegeln.Ueberfaellig(a, jetzt) ? ("überfällig", Stil.Beschluss)
                : AufgabenRegeln.JetztDran(a, jetzt) ? ("jetzt dran", Stil.Unterwegs)
                : (AufgabenRegeln.FuerText(a), Stil.Zimmer);
            var feld = new MarkenFeld(markenText, marke);
            var text = new Label
            {
                Text = a.Text + (a.BewohnerId is long id && namen.TryGetValue(id, out var n) ? $" ({n})" : ""),
                AutoSize = true, AutoEllipsis = true, BackColor = Stil.Flaeche, ForeColor = Stil.TextNormal, UseMnemonic = false,
            };
            zeilen.Add((feld, text));
            Controls.Add(feld);
            Controls.Add(text);
        }
        if (offen.Count > Hoechstens) AlleKnopf.Text = $"Alle {offen.Count} Aufgaben …";
        else AlleKnopf.Text = "Alle Aufgaben …";
        AccessibleName = $"{kopf.Text}: {string.Join("; ", ZeilenTexte)}";
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandMittel), x = innen, y = innen;
        AlleKnopf.Location = new Point(Width - innen - AlleKnopf.Width, y - Stil.Px(this, 2));
        kopf.Location = new Point(x, y + Stil.Px(this, Stil.AbstandMini));
        y = Math.Max(kopf.Bottom, AlleKnopf.Bottom - Stil.Px(this, Stil.AbstandKlein)) + Stil.Px(this, Stil.AbstandMini);
        int markenBreite = zeilen.Count == 0 ? 0 : zeilen.Max(z => z.Marke.PreferredSize.Width);
        foreach (var (m, t) in zeilen)
        {
            m.Size = new Size(markenBreite, m.PreferredSize.Height);
            m.Location = new Point(x, y);
            t.AutoSize = false;
            t.Bounds = new Rectangle(x + markenBreite + Stil.Px(this, Stil.AbstandKlein), y, Math.Max(50, Width - 2 * innen - markenBreite - Stil.Px(this, Stil.AbstandKlein)), m.Height);
            t.TextAlign = ContentAlignment.MiddleLeft;
            y += m.Height + Stil.Px(this, Stil.AbstandMini);
        }
        int hoehe = y + innen - Stil.Px(this, Stil.AbstandMini);
        if (Height != hoehe) Height = hoehe;
    }
}
