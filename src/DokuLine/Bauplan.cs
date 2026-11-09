namespace DokuLine;

// Welche Bereiche noch kommen und in welcher Version (Bauplan in IDEEN.md). Noch nicht gebaute
// Bereiche zeigen statt einer leeren Fläche einen freundlichen Hinweis.
public static class Bauplan
{
    public sealed record Vorhaben(string Version, string Beschreibung);

    public static readonly IReadOnlyDictionary<string, Vorhaben> Bereiche = new Dictionary<string, Vorhaben>
    {
    };

    // Reiter der Bewohnerakte, in dieser Reihenfolge. Stammdaten gibt es schon.
    public static readonly string[] AktenReiter =
        ["Stammdaten", "Betreuung", "Angehörige", "Ärzte", "Krankenkasse", "Kostenträger", "Schweigepflicht", "Dokumente", "Medikamentenplan", "Vitalwerte", "Wochenplan", "Biografie",
         "Reflexion", "HEB"];

    public static readonly IReadOnlyDictionary<string, Vorhaben> Reiter = new Dictionary<string, Vorhaben>
    {
    };
}

// Eine Fläche mit dem Hinweis „Kommt in Version …“ und einer kurzen Beschreibung.
public sealed class PlatzhalterKarte : KartenFlaeche
{
    public Label Titel { get; } = new();
    public Label Beschreibung { get; } = new();

    public PlatzhalterKarte()
    {
        foreach (var label in new[] { Titel, Beschreibung })
        {
            label.AutoSize = true;
            label.BackColor = Stil.Flaeche;
            label.UseMnemonic = false;
            Controls.Add(label);
        }
        Titel.Font = Stil.KarteName;
        Titel.ForeColor = Stil.TextNormal;
        Beschreibung.Font = Stil.Standard;
        Beschreibung.ForeColor = Stil.TextMittel;
        Height = 120;
    }

    public void Setzen(string version, string beschreibung)
    {
        Titel.Text = $"Kommt in Version {version}";
        Beschreibung.Text = beschreibung;
        AccessibleName = $"{Titel.Text}. {beschreibung}";
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int innen = Stil.Px(this, Stil.AbstandGross + Stil.AbstandKlein);
        Titel.Location = new Point(innen, innen);
        Beschreibung.MaximumSize = new Size(Math.Max(100, Width - 2 * innen), 0);
        Beschreibung.Location = new Point(innen, Titel.Bottom + Stil.Px(this, Stil.AbstandKlein));
        int hoehe = Beschreibung.Bottom + innen;
        if (Height != hoehe) Height = hoehe;
    }
}

// Eine ganze Seite für einen noch nicht gebauten Bereich.
public sealed class PlatzhalterSeite : Panel, IBereichSeite
{
    private readonly Bauplan.Vorhaben vorhaben;
    public string Titel { get; set; } = "";
    public string KopfText => $"Kommt in Version {vorhaben.Version}";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [];
    void IBereichSeite.Zeigen(bool filterBehalten) { }

    public PlatzhalterKarte Karte { get; } = new();

    public PlatzhalterSeite(Bauplan.Vorhaben vorhaben)
    {
        this.vorhaben = vorhaben;
        BackColor = Stil.Seite;
        Karte.Setzen(vorhaben.Version, vorhaben.Beschreibung);
        Controls.Add(Karte);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        Karte.Location = new Point(rand, rand);
        Karte.Width = Math.Max(100, ClientSize.Width - 2 * rand);
    }
}
