using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Reiter „Ausgang“ in der Bewohnerakte: die Karte des Bewohners und darunter sein Verlauf.
public sealed class AusgangReiter : ModulReiter
{
    private readonly Datenbank db;
    private Bewohner? bewohner;

    public PlatzKarte? Karte { get; private set; }
    public TextBox VerlaufText { get; } = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Stil.Flaeche, BorderStyle = BorderStyle.FixedSingle };
    private readonly Label verlaufTitel = new() { Text = "VERLAUF", AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };

    public AusgangReiter(Speicher speicher)
    {
        db = new Datenbank(speicher);
        BackColor = Stil.Seite;
        Controls.Add(verlaufTitel);
        Controls.Add(VerlaufText);
    }

    public override string Titel => "Ausgang";

    public override void Zeigen(Bewohner b)
    {
        bewohner = b;
        var p = db.Platz(b.Id!.Value);
        if (Karte is not null) { Controls.Remove(Karte); Karte.Dispose(); Karte = null; }
        if (p is not null)
        {
            var jetzt = db.Jetzt();
            bool digital = db.Speicher.ModulAn(DigitalerAusgang.Schluessel);
            var stand = digital ? DigitalerAusgang.StandFuer(p, db.AusgaengeLaden(jetzt), jetzt.TimeOfDay) : null;
            bool kueche = db.Speicher.ModulAn(Kuechendienst.Schluessel) && p.Anwesend &&
                          Kuechendienst.ZimmerAm(db.KuechendienstLaden(), jetzt).Contains(p.Zimmernummer);
            Karte = new PlatzKarte(p, jetzt, stand, kueche, mitName: false);
            Aktionen.Verbinden(Karte, db, this, () => { Zeigen(bewohner!); Melden(); });
            Controls.Add(Karte);
        }
        var zeilen = Verlauf.Zeilen(db.VerlaufLaden(b.Id!.Value));
        VerlaufText.Text = zeilen.Count == 0 ? "Noch keine Einträge." : string.Join(Environment.NewLine, zeilen);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int breite = Math.Max(100, ClientSize.Width), y = 0;
        if (Karte is not null)
        {
            Karte.SetBounds(0, 0, breite, Karte.Height);
            Karte.PerformLayout();
            y = Karte.Bottom + Stil.Px(this, Stil.AbstandGross);
        }
        verlaufTitel.Location = new Point(Stil.Px(this, Stil.AbstandMini), y);
        y = verlaufTitel.Bottom + Stil.Px(this, Stil.AbstandMini);
        VerlaufText.SetBounds(0, y, breite, Stil.Px(this, 180));
        int hoehe = VerlaufText.Bottom + Stil.Px(this, Stil.AbstandKlein);
        if (Height != hoehe) Height = hoehe;
    }
}
