using DokuLine.Kern;

namespace DokuLine.Modul.Ausgangsliste;

// Oben im Tagesplan (Bauplan 45): „Küchendienst heute: … (Zi. …)“ aus dem Küchendienstplan des Moduls, getauschte Dienste
// mit Hinweis, „Küchendienst unbesetzt“ in Achtung-Farbe, wenn niemand vom Dienst da ist (Ausgangsliste). Bestätigen wie
// andere Termine: gemacht · nicht gemacht · vertreten durch …, mit Kürzel und Uhrzeit. Untermodul aus: nichts.
public sealed class KuechendienstTagesplan : TagesplanTeil
{
    private readonly Speicher speicher;
    private DateTime tag;

    public Label Zeile { get; } = new() { AutoSize = true, Font = Stil.Fett, ForeColor = Stil.TextNormal, BackColor = Stil.Seite, UseMnemonic = false };
    public MarkenFeld Marke { get; } = new(Kuechendienst.Unbesetzt, Stil.Achtung) { Visible = false };
    public Label Stand { get; } = new() { AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, UseMnemonic = false };
    public Button GemachtKnopf { get; } = Stil.KleinerKnopf("gemacht");
    public Button NichtGemachtKnopf { get; } = Stil.KleinerKnopf("nicht gemacht");
    public Button VertretenKnopf { get; } = Stil.KleinerKnopf("vertreten durch …");
    public KuechendienstHeute? Heute { get; private set; }
    // Zum Testen ersetzbar: wer vertreten hat.
    public Func<string?> VertretungFragen { get; set; }

    public KuechendienstTagesplan(Speicher speicher)
    {
        this.speicher = speicher;
        VertretungFragen = () => Eingabe.Fragen(FindForm(), "Küchendienst vertreten", "Wer hat den Küchendienst gemacht? (Name oder Zimmer)");
        foreach (var c in new Control[] { Zeile, Marke, Stand }) c.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandMittel, 0);
        foreach (var k in new[] { GemachtKnopf, NichtGemachtKnopf, VertretenKnopf }) k.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        GemachtKnopf.Click += (_, _) => Setzen(KuechendienstStatus.Gemacht);
        NichtGemachtKnopf.Click += (_, _) => Setzen(KuechendienstStatus.NichtGemacht);
        VertretenKnopf.Click += (_, _) => Setzen(KuechendienstStatus.Vertreten);
        Controls.AddRange([Zeile, Marke, Stand, GemachtKnopf, NichtGemachtKnopf, VertretenKnopf]);
        AccessibleName = "Küchendienst heute";
    }

    public override void Zeigen(DateTime jetzt)
    {
        tag = jetzt.Date;
        Visible = speicher.ModulAn(Kuechendienst.Schluessel);
        if (!Visible) return;
        var db = new Datenbank(speicher);
        Heute = Kuechendienst.Heute(db.KuechendienstLaden(), tag, db.Laden());
        Zeile.Text = Kuechendienst.ZeileText(Heute);
        Marke.Visible = Heute?.Unbesetzt == true;
        Stand.Text = Heute is null ? "" : Kuechendienst.ErledigtText(db.KuechendienstErledigtLaden(tag));
        string? grund = Heute is null ? "Kein Küchendienst geplant." : speicher.Grund(Sitzung.Kuerzel, Funktionen.TeilnahmeBestaetigen);
        foreach (var k in new[] { GemachtKnopf, NichtGemachtKnopf, VertretenKnopf })
        {
            k.Visible = Heute is not null;
            Stil.Sperren(k, grund);
        }
    }

    public bool Setzen(KuechendienstStatus status, string? vertretung = null)
    {
        if (Stil.Sperrgrund(GemachtKnopf) is not null) return false;
        if (status == KuechendienstStatus.Vertreten && (vertretung ??= VertretungFragen()) is null) return false;
        var jetzt = speicher.Uhr();
        if (Kuechendienst.BestaetigenPruefen(Heute, tag, jetzt, status, vertretung ?? "") is string fehler)
        {
            Stand.Text = fehler;
            return false;
        }
        new Datenbank(speicher).KuechendienstErledigtSetzen(tag, status, vertretung ?? "", Sitzung.Kuerzel, jetzt);
        Zeigen(jetzt);
        Melden();
        return true;
    }
}
