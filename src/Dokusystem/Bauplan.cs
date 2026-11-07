namespace Dokusystem;

// Welche Bereiche noch kommen und in welcher Version (Bauplan in IDEEN.md). Noch nicht gebaute
// Bereiche zeigen statt einer leeren Fläche einen freundlichen Hinweis.
public static class Bauplan
{
    public sealed record Vorhaben(string Version, string Beschreibung);

    public static readonly IReadOnlyDictionary<string, Vorhaben> Bereiche = new Dictionary<string, Vorhaben>
    {
        ["Übersicht"] = new("0.17.0", "Alle Bewohner auf einen Blick: was heute wichtig ist – Termine, Änderungen in der Medikation, Beschlüsse, die bald enden, fällige HEB-Bögen."),
        ["Kalender"] = new("0.13.0", "Termine des Wohnbereichs: Arzttermine, Heimfahrten, Tagesausflüge – als Monat, Woche oder Tag."),
        ["Therapie"] = new("0.11.0", "Die Therapieangebote mit Teilnehmern und freien Plätzen; zugeteilte Angebote erscheinen im Wochenplan."),
        ["Ausgangsliste"] = new("0.14.0", "Ausgänge, An- und Abwesenheit und Unterbringung – als Modul, das sich in den Einstellungen abschalten lässt."),
    };

    // Reiter der Bewohnerakte, in dieser Reihenfolge. Stammdaten gibt es schon.
    public static readonly string[] AktenReiter = ["Stammdaten", "Berichte", "Medikation", "Wochenplan", "Biografie", "Reflexion", "HEB"];

    public static readonly IReadOnlyDictionary<string, Vorhaben> Reiter = new Dictionary<string, Vorhaben>
    {
        ["Wochenplan"] = new("0.12.0", "Der Wochenplan mit Therapien und Terminen, auch zum Ausdrucken."),
        ["Biografie"] = new("0.15.0", "Lebensgeschichte in Abschnitten, mit Verlauf und Liste fehlender Angaben."),
        ["Reflexion"] = new("0.16.0", "Je Monat eine kurze Reflexion je Lebensbereich, mit den Berichten des Monats daneben."),
        ["HEB"] = new("0.18.0", "Der HEB-Bogen für einen Zeitraum, zusammengestellt aus Berichten, Wochenplan und Reflexionen."),
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
public sealed class PlatzhalterSeite : Panel
{
    private const int MaxBreite = 880;
    public PlatzhalterKarte Karte { get; } = new();

    public PlatzhalterSeite(Bauplan.Vorhaben vorhaben)
    {
        BackColor = Stil.Seite;
        Karte.Setzen(vorhaben.Version, vorhaben.Beschreibung);
        Controls.Add(Karte);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        Karte.Location = new Point(rand, rand);
        Karte.Width = Math.Min(ClientSize.Width - 2 * rand, Stil.Px(this, MaxBreite));
    }
}
