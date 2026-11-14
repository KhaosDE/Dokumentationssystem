using DokuLine.Kern;

namespace DokuLine;

// Einstellungen → „Fachwörter …“ (Bauplan 49): das gemeinsame Fachwörterbuch der Einrichtung. Ansehen alle; ergänzen und
// entfernen mit Recht „Fachwörter verwalten“.
public sealed class FachwoerterFenster : Form
{
    private readonly Speicher speicher;

    public ListBox Liste { get; } = new() { Width = 320, Height = 260, AccessibleName = "Fachwörter" };
    public TextBox Wort { get; } = new() { Width = 220, AccessibleName = "Neues Fachwort" };
    public Button HinzuKnopf { get; } = Stil.NebenKnopf("Hinzufügen");
    public Button EntfernenKnopf { get; } = Stil.NebenKnopf("Entfernen");
    public Label Fehler { get; } = Fenster.Fehlerfeld();

    public FachwoerterFenster(Speicher speicher)
    {
        this.speicher = speicher;
        Fenster.Grundform(this, "Fachwörter");
        HinzuKnopf.Click += (_, _) => Hinzufuegen();
        EntfernenKnopf.Click += (_, _) => Entfernen();
        var raster = Formular.Raster(out var zeile);
        zeile(null, Fenster.Hinweis("Diese Wörter gelten bei der Rechtschreibprüfung als richtig – für alle. Namen der Bewohner, Mitarbeiter und Verzeichnisse, " +
            "Medikamente und Therapieangebote kennt das Programm ohnehin."));
        zeile(null, Liste);
        zeile(null, Formular.Reihe(Wort, HinzuKnopf, EntfernenKnopf));
        zeile(null, Fehler);
        Controls.Add(raster);
        Recht.Anwenden(speicher, Funktionen.FachwoerterVerwalten, HinzuKnopf, EntfernenKnopf);
        Laden();
        Stil.DpiSkalierungEinschalten(this);
    }

    private void Laden()
    {
        Liste.BeginUpdate();
        Liste.Items.Clear();
        Liste.Items.AddRange([.. speicher.Fachwoerter()]);
        Liste.EndUpdate();
    }

    private bool Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e) { Fehler.Text = string.Join(" ", e.Fehler); Fehler.Visible = true; return false; }
        Fehler.Visible = false;
        Rechtschreibpruefung.EigeneNeu();
        Laden();
        return true;
    }

    public bool Hinzufuegen()
    {
        if (Stil.Sperrgrund(HinzuKnopf) is not null) return false;
        if (!Ausfuehren(() => speicher.FachwortHinzufuegen(Wort.Text, Recht.Kuerzel))) return false;
        Wort.Text = "";
        return true;
    }

    public bool Entfernen() =>
        Stil.Sperrgrund(EntfernenKnopf) is null && Liste.SelectedItem is string w && Ausfuehren(() => speicher.FachwortEntfernen(w, Recht.Kuerzel));
}
