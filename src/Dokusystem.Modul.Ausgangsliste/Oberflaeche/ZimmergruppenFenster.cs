using Dokusystem.Kern;
using Microsoft.Data.Sqlite;

namespace Dokusystem.Modul.Ausgangsliste;

// Reihenfolge und Zimmergruppen des Küchendienstes pflegen (übernommen aus der Ausgangsliste).
// Eine Gruppe hat genau zwei verschiedene Zimmer, die den Dienst gemeinsam machen, und steht in
// der Reihenfolge an der Stelle ihres ersten Zimmers. Ein Zimmer kann nur in einer Gruppe sein.
// Ob ein Zimmer an einem Tag Dienst machen kann, prüft die Planung selbst.
// Gespeichert wird erst mit „Übernehmen“; „Abbrechen“ verwirft alles.
public sealed class ZimmergruppenFenster : Form
{
    private readonly Datenbank db;
    private readonly List<string> reihenfolge;
    private readonly List<string[]> gruppen;

    public ListBox ReihenfolgeListe { get; } = new();
    public ListBox GruppenListe { get; } = new();
    public ComboBox Zimmer1 { get; } = new();
    public ComboBox Zimmer2 { get; } = new();
    public Button HochKnopf { get; } = Stil.NebenKnopf("▲ Hoch");
    public Button RunterKnopf { get; } = Stil.NebenKnopf("▼ Runter");
    public Button HinzufuegenKnopf { get; } = Stil.NebenKnopf("Hinzufügen");
    public Button LoeschenKnopf { get; } = Stil.NebenKnopf("Löschen");
    public Button UebernehmenKnopf { get; } = Stil.HauptKnopf("Übernehmen");
    public Button AbbrechenKnopf { get; } = Stil.NebenKnopf("Abbrechen");
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(620, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    // Der bearbeitete Stand (wird erst mit Speichern geschrieben).
    public IReadOnlyList<string> Reihenfolge => reihenfolge;
    public IReadOnlyList<string[]> Gruppen => gruppen;

    public ZimmergruppenFenster(Datenbank db)
    {
        this.db = db;
        reihenfolge = Kuechendienst.Reihenfolge(db, db.Laden());
        gruppen = Kuechendienst.Gruppen(db);

        Text = $"Küchendienst – Reihenfolge und Zimmergruppen ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(680, 500);
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;

        var gitter = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(Stil.AbstandGross), ColumnCount = 2, RowCount = 6 };
        gitter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        gitter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        gitter.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Überschriften
        gitter.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Hinweise
        gitter.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Listen
        gitter.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Knöpfe unter den Listen
        gitter.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Fehler
        gitter.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Übernehmen / Abbrechen

        // ---- links: Reihenfolge ----
        gitter.Controls.Add(Ueberschrift("Reihenfolge"), 0, 0);
        gitter.Controls.Add(Hinweis("Zimmer auswählen und verschieben."), 0, 1);
        ListeGestalten(ReihenfolgeListe);
        ReihenfolgeListe.AccessibleName = "Reihenfolge";
        gitter.Controls.Add(ReihenfolgeListe, 0, 2);
        HochKnopf.Click += (_, _) => Verschieben(-1);
        RunterKnopf.Click += (_, _) => Verschieben(+1);
        gitter.Controls.Add(KnopfReihe(HochKnopf, RunterKnopf), 0, 3);

        // ---- rechts: Zimmergruppen ----
        gitter.Controls.Add(Ueberschrift("Zimmergruppen"), 1, 0);
        gitter.Controls.Add(Hinweis("Zwei Zimmer teilen sich einen Dienst. Die Gruppe steht in der Reihenfolge an der Stelle von Zimmer 1."), 1, 1);
        ListeGestalten(GruppenListe);
        GruppenListe.AccessibleName = "Zimmergruppen";
        gitter.Controls.Add(GruppenListe, 1, 2);
        foreach (var (auswahl, name) in new[] { (Zimmer1, "Zimmer 1"), (Zimmer2, "Zimmer 2") })
        {
            auswahl.DropDownStyle = ComboBoxStyle.DropDownList;
            auswahl.Width = 72;
            auswahl.AccessibleName = name;
            foreach (string zimmer in Kuechendienst.Aufsteigend(reihenfolge)) auswahl.Items.Add(zimmer);
        }
        HinzufuegenKnopf.Click += (_, _) => GruppeHinzufuegen();
        LoeschenKnopf.Click += (_, _) => GruppeLoeschen();
        gitter.Controls.Add(KnopfReihe(Zimmer1, Zimmer2, HinzufuegenKnopf, LoeschenKnopf), 1, 3);

        // ---- unten: Fehler, Übernehmen / Abbrechen ----
        Fehler.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        gitter.Controls.Add(Fehler, 0, 4);
        gitter.SetColumnSpan(Fehler, 2);

        AbbrechenKnopf.DialogResult = DialogResult.Cancel;
        UebernehmenKnopf.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0);
        UebernehmenKnopf.Click += (_, _) => Speichern();
        var knoepfe = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        knoepfe.Controls.AddRange([UebernehmenKnopf, AbbrechenKnopf]);
        gitter.Controls.Add(knoepfe, 0, 5);
        gitter.SetColumnSpan(knoepfe, 2);

        Controls.Add(gitter);
        AcceptButton = UebernehmenKnopf;
        CancelButton = AbbrechenKnopf;

        ListenFuellen();
        Stil.DpiSkalierungEinschalten(this);
    }

    // ----- Bauhilfen -----

    private static Label Ueberschrift(string text) =>
        new() { Text = text, AutoSize = true, Font = Stil.Fett, Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandMini) };

    private static Label Hinweis(string text) =>
        new() { Text = text, Dock = DockStyle.Fill, Height = 40, ForeColor = Stil.TextLeise, Margin = new Padding(0, 0, Stil.AbstandGross, Stil.AbstandKlein) };

    private static void ListeGestalten(ListBox liste)
    {
        liste.Dock = DockStyle.Fill;
        liste.BackColor = Stil.Flaeche;
        liste.ForeColor = Stil.TextNormal;
        liste.BorderStyle = BorderStyle.FixedSingle;
        liste.IntegralHeight = false; // genau so hoch wie der Platz, nicht auf ganze Zeilen gerundet
        liste.Margin = new Padding(0, 0, Stil.AbstandGross, 0);
    }

    private static FlowLayoutPanel KnopfReihe(params Control[] teile)
    {
        var reihe = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
        foreach (var teil in teile) teil.Margin = new Padding(0, teil is ComboBox ? Stil.AbstandKlein - 2 : 0, Stil.AbstandKlein, 0);
        reihe.Controls.AddRange(teile);
        return reihe;
    }

    // ----- Anzeige -----

    private string[]? GruppeVon(string zimmer) => gruppen.FirstOrDefault(g => g.Contains(zimmer));

    private static string GruppenText(string[] gruppe) => string.Join(" / ", gruppe);

    // Beide Listen neu aufbauen; die Auswahl links bleibt erhalten.
    private void ListenFuellen(int auswahlLinks = -1)
    {
        ReihenfolgeListe.Items.Clear();
        foreach (string zimmer in reihenfolge)
        {
            // Gehört das Zimmer zu einer Gruppe, sieht man so, wo die Gruppe im Ablauf landet.
            string[]? gruppe = GruppeVon(zimmer);
            ReihenfolgeListe.Items.Add(gruppe is null ? $"Zimmer {zimmer}" : $"Zimmer {zimmer}   (Gruppe {GruppenText(gruppe)})");
        }
        if (auswahlLinks >= 0 && auswahlLinks < reihenfolge.Count) ReihenfolgeListe.SelectedIndex = auswahlLinks;

        GruppenListe.Items.Clear();
        foreach (var gruppe in gruppen) GruppenListe.Items.Add("Zimmer " + GruppenText(gruppe));
    }

    // ----- Aktionen -----

    // Verschiebt das gewählte Zimmer um eine Stelle (-1 hoch, +1 runter).
    public bool Verschieben(int richtung)
    {
        FehlerZeigen("");
        int alt = ReihenfolgeListe.SelectedIndex;
        if (alt < 0) return FehlerZeigen("Bitte zuerst links ein Zimmer auswählen.");
        int neu = alt + richtung;
        if (neu < 0 || neu >= reihenfolge.Count) return false; // schon ganz oben oder unten

        (reihenfolge[alt], reihenfolge[neu]) = (reihenfolge[neu], reihenfolge[alt]);
        ListenFuellen(neu);
        return true;
    }

    public bool GruppeHinzufuegen()
    {
        FehlerZeigen("");
        if (Zimmer1.SelectedIndex < 0 || Zimmer2.SelectedIndex < 0) return FehlerZeigen("Bitte beide Zimmer auswählen.");
        string zimmer1 = (string)Zimmer1.SelectedItem!;
        string zimmer2 = (string)Zimmer2.SelectedItem!;
        if (zimmer1 == zimmer2) return FehlerZeigen("Die beiden Zimmer müssen unterschiedlich sein.");
        foreach (string zimmer in new[] { zimmer1, zimmer2 })
            if (GruppeVon(zimmer) is string[] vorhanden)
                return FehlerZeigen($"Zimmer {zimmer} gehört schon zur Gruppe {GruppenText(vorhanden)}.");

        gruppen.Add([zimmer1, zimmer2]);
        ListenFuellen(ReihenfolgeListe.SelectedIndex);
        return true;
    }

    public bool GruppeLoeschen()
    {
        FehlerZeigen("");
        if (GruppenListe.SelectedIndex < 0) return FehlerZeigen("Bitte zuerst rechts eine Gruppe auswählen.");
        gruppen.RemoveAt(GruppenListe.SelectedIndex);
        ListenFuellen(ReihenfolgeListe.SelectedIndex);
        return true;
    }

    // Schreibt Reihenfolge und Gruppen. Neu geplant wird danach vom Küchendienst-Fenster.
    public bool Speichern()
    {
        FehlerZeigen("");
        try
        {
            Kuechendienst.ReihenfolgeSpeichern(db, reihenfolge);
            Kuechendienst.GruppenSpeichern(db, gruppen);
        }
        catch (Exception e) when (e is SqliteException or SpeicherFehler)
        {
            return FehlerZeigen("Die Einstellungen konnten nicht gespeichert werden: " + e.Message);
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }

    private bool FehlerZeigen(string text)
    {
        Fehler.Text = text;
        Fehler.Visible = text != "";
        return false;
    }
}
