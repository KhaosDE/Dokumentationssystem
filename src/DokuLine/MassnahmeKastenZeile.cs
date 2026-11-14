using DokuLine.Kern;

namespace DokuLine;

// Eine Maßnahme im Kasten „Laufender Dienst“ der Übersicht (Bauplan 40): „08:30 · Ahorn, Anton · Tagesplan besprechen“,
// überfällige mit Marke; darunter die drei Knöpfe. Bestätigte stehen blass mit Ergebnis, Kürzel und Uhrzeit, ohne Knöpfe.
public sealed class MassnahmeKastenZeile : Panel
{
    public MassnahmeTermin Termin { get; }
    public Label Text1 { get; } = new() { AutoSize = true, BackColor = Stil.Flaeche, UseMnemonic = false };
    public MarkenFeld? Ueberfaellig { get; }
    public Button? DurchgefuehrtKnopf { get; }
    public Button? AbgelehntKnopf { get; }
    public Button? NichtMoeglichKnopf { get; }
    public event Action<MassnahmeTermin, MassnahmeErgebnis, bool>? Gewaehlt;   // Ergebnis; true = mit Fenster (Grund)

    public MassnahmeKastenZeile(MassnahmeTermin termin, string bewohner, DateTime jetzt, string? sperre)
    {
        Termin = termin;
        BackColor = Stil.Flaeche;
        string wann = termin.Fassung.Uhrzeit is TimeOnly u ? $"{u:HH\\:mm}" : "im Dienst";
        if (termin.Bestaetigung is MassnahmeBestaetigung b)
        {
            Text1.Text = $"✓ {b.Zeit:HH:mm} {b.Kuerzel} · {bewohner} · {termin.Fassung.Titel} ({MassnahmenRegeln.ErgebnisText(b.Ergebnis)})";
            Text1.ForeColor = Stil.TextLeise;
            Controls.Add(Text1);
            return;
        }
        Text1.Text = $"{wann} · {bewohner} · {termin.Fassung.Titel}";
        Text1.ForeColor = Stil.TextNormal;
        Controls.Add(Text1);
        if (termin.Uhrzeit is DateTime zeit && zeit < jetzt)
        {
            Ueberfaellig = new MarkenFeld("überfällig", Stil.Achtung);
            Controls.Add(Ueberfaellig);
        }
        DurchgefuehrtKnopf = Knopf("durchgeführt", MassnahmeErgebnis.Durchgefuehrt, false, sperre);
        AbgelehntKnopf = Knopf("abgelehnt …", MassnahmeErgebnis.Abgelehnt, true, sperre);
        NichtMoeglichKnopf = Knopf("nicht möglich …", MassnahmeErgebnis.NichtMoeglich, true, sperre);
        AccessibleName = Text1.Text;
    }

    private Button Knopf(string text, MassnahmeErgebnis ergebnis, bool fenster, string? sperre)
    {
        var k = Stil.NebenKnopf(text);
        k.Font = Stil.Klein;
        k.AccessibleName = $"{text.TrimEnd(' ', '…')}: {Termin.Fassung.Titel}";
        Stil.Sperren(k, sperre);
        k.Click += (_, _) => Gewaehlt?.Invoke(Termin, ergebnis, fenster);
        Controls.Add(k);
        return k;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int mini = Stil.Px(this, Stil.AbstandMini);
        int marke = Ueberfaellig is null ? 0 : Ueberfaellig.PreferredSize.Width + mini;
        Text1.MaximumSize = new Size(Math.Max(50, Width - marke), 0);
        Text1.Location = Point.Empty;
        if (Ueberfaellig is not null) Ueberfaellig.Location = new Point(Width - Ueberfaellig.PreferredSize.Width, 0);
        int y = Math.Max(Text1.Bottom, Ueberfaellig?.Bottom ?? 0) + mini, x = 0;
        foreach (var k in new[] { DurchgefuehrtKnopf, AbgelehntKnopf, NichtMoeglichKnopf }.OfType<Button>())
        {
            k.Size = k.GetPreferredSize(Size.Empty);
            if (x > 0 && x + k.Width > Width) { x = 0; y += k.Height + mini; }
            k.Location = new Point(x, y);
            x += k.Width + mini;
        }
        int hoehe = Controls.OfType<Button>().Select(k => k.Bottom).DefaultIfEmpty(Text1.Bottom).Max() + mini;
        if (Height != hoehe) Height = hoehe;
    }
}
