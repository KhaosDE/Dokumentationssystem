using DokuLine.Kern;

namespace DokuLine;

// Bereich „Therapie“, Seite „Wochenpläne“: links die Bewohnerleiste (wie in der Dokumentation), rechts der
// Wochenplan des gewählten Bewohners. Freie Einträge nur lesen; Angebote ändert nur ihr Anbieter. Drucken.
public sealed class WochenplaeneSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Wochenpläne";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [];
    void IBereichSeite.Zeigen(bool filterBehalten) => Zeigen();

    private readonly Speicher speicher;
    private readonly KartenFlaeche links = new() { Dock = DockStyle.Left, Width = BewohnerBereich.LinksBreite, Padding = new Padding(0) };
    private readonly Panel rechts = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Stil.Seite };
    private readonly Label leer = new() { Text = "Bitte links einen Bewohner wählen.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };
    private List<Bewohner?> eintraege = [];
    private long? zuletzt;
    private bool fuellen;

    public TextBox Suche { get; } = new() { PlaceholderText = "Name suchen", Font = Stil.Standard, AccessibleName = "Bewohner suchen" };
    public CheckBox AlleKnopf { get; } = TherapieListe.AlleSchalter();
    public ListBox Namen { get; } = new RundeListe { AccessibleName = "Bewohner" };
    public WochenplanAnsicht Plan { get; }
    public List<Bewohner> Zeilen { get; private set; } = [];
    public Bewohner? Gewaehlt => Namen.SelectedIndex >= 0 && Namen.SelectedIndex < eintraege.Count ? eintraege[Namen.SelectedIndex] : null;
    public string KopfText { get; private set; } = "";
    public event Action? Geaendert;
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public WochenplaeneSeite(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        Padding = new Padding(Stil.AbstandGross, Stil.AbstandKlein, Stil.AbstandGross, 0);
        Plan = new WochenplanAnsicht(speicher) { NurLesen = true, Visible = false, Jetzt = () => Jetzt() };
        AlleKnopf.Visible = true;   // die Seite gibt es nur im Bereich Therapie
        Suche.TextChanged += (_, _) => Aktualisieren();
        AlleKnopf.CheckedChanged += (_, _) => Aktualisieren();
        Namen.SelectedIndexChanged += (_, _) => { if (!fuellen) PlanZeigen(); };
        links.Controls.AddRange([Suche, AlleKnopf, Namen]);
        links.Layout += (_, _) => LinksAnordnen();
        rechts.Controls.AddRange([Plan, leer]);
        rechts.Layout += (_, _) => RechtsAnordnen();
        Controls.Add(rechts);
        Controls.Add(new Panel { Dock = DockStyle.Left, Width = Stil.AbstandGross, BackColor = Stil.Seite });
        Controls.Add(links);
    }

    public void Zeigen() => Aktualisieren();

    public void Aktualisieren()
    {
        long? vorher = Gewaehlt?.Id ?? zuletzt;
        var imBereich = TherapieListe.Bewohner(speicher, AlleKnopf.Checked, DateOnly.FromDateTime(Jetzt()));
        string suche = Suche.Text.Trim();
        Zeilen = [.. imBereich.Where(b => suche.Length == 0 || $"{b.Nachname}, {b.Vorname}".Contains(suche, StringComparison.CurrentCultureIgnoreCase)
            || b.Name.Contains(suche, StringComparison.CurrentCultureIgnoreCase))];
        fuellen = true;
        try
        {
            eintraege = TherapieListe.Fuellen(Namen, Zeilen, speicher);
            int index = eintraege.FindIndex(b => b is not null && b.Id == vorher);
            Namen.SelectedIndex = index >= 0 ? index : eintraege.FindIndex(b => b is not null);
        }
        finally { fuellen = false; }
        string anzahl = imBereich.Count == 1 ? "1 Bewohner" : $"{imBereich.Count} Bewohner";
        KopfText = $"{(AlleKnopf.Checked ? "alle Bewohner des Hauses" : "mit Angebot der Therapie")} · " +
                   (Zeilen.Count == imBereich.Count ? anzahl : $"{Zeilen.Count} von {anzahl}");
        PlanZeigen();
    }

    private void PlanZeigen()
    {
        var b = Gewaehlt;
        if (b is not null)
        {
            zuletzt = b.Id;
            Plan.Zeigen(b);
        }
        Plan.Visible = b is not null;
        leer.Visible = b is null;
        rechts.PerformLayout();
        Geaendert?.Invoke();
    }

    private void LinksAnordnen()
    {
        int rand = Stil.Px(this, Stil.AbstandKlein), breite = links.ClientSize.Width - 2 * rand;
        Suche.SetBounds(rand, rand, breite, Suche.Height);
        AlleKnopf.Location = new Point(rand, Suche.Bottom + rand);
        int oben = AlleKnopf.Bottom + rand;
        Namen.SetBounds(rand, oben, breite, Math.Max(40, links.ClientSize.Height - oben - rand));
    }

    private void RechtsAnordnen()
    {
        int breite = Math.Max(200, rechts.ClientSize.Width);
        Plan.VerfuegbareHoehe = rechts.ClientSize.Height;
        Plan.Location = new Point(0, rechts.AutoScrollPosition.Y);
        Plan.Width = breite;
        leer.Location = new Point(0, Stil.Px(this, Stil.AbstandKlein));
    }
}
