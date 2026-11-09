using System.ComponentModel;
using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Reiter „Medikamentenplan“ in der Akte: „Was gilt am …“ (heute oder ein gewählter Tag) als
// Tabelle, berechnet aus Grundverordnung, Pausen und Stufenplänen; Bedarfsmedikation als eigene Tabelle
// darunter. Klick auf eine Zeile öffnet Einzelheiten und Verlauf; dort Pause, Stufenplan, Absetzen.
// Demo — nicht zum Stellen verwenden.
public sealed class MedikationAnsicht : Panel
{
    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly Label regelTitel = Titel("REGELMÄSSIG");
    private readonly Label bedarfTitel = Titel("BEI BEDARF");
    private Bewohner? bewohner;

    public TextBox TagFeld { get; } = new DatumFeld() { Width = 100, PlaceholderText = "TT.MM.JJJJ" };
    public Button HeuteKnopf { get; } = Stil.NebenKnopf("Heute");
    public Button NeuKnopf { get; } = Stil.NebenKnopf("Neue Verordnung …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Drucken …");
    public CheckBox AbgesetzteZeigen { get; } = new() { Text = "Abgesetzte zeigen", AutoSize = true, BackColor = Stil.Seite };
    public MarkenFeld Hinweis { get; } = new(MedikationRegeln.DruckHinweis, Stil.Achtung);
    public Label TagFehler { get; } = new() { AutoSize = true, ForeColor = Stil.Beschluss.Text, BackColor = Stil.Seite, Visible = false };
    public DataGridView Tabelle { get; } = RegelTabelleBauen();
    public DataGridView BedarfTabelle { get; } = BedarfTabelleBauen();

    // Auch für die Visitenansicht: dieselben Tabellen wie im Medikamentenplan.
    public static DataGridView RegelTabelleBauen() => PlanTabelle.Bauen(
        ("medikament", "Medikament", 30), ("morgens", "morgens", 7), ("mittags", "mittags", 7), ("abends", "abends", 7), ("nachts", "nachts", 7),
        ("arzt", "verordnet von", 13), ("seit", "seit", 9), ("status", "Status", 14), ("hinweis", "Hinweis", 16));

    public static DataGridView BedarfTabelleBauen() => PlanTabelle.Bauen(
        ("medikament", "Medikament", 30), ("anlass", "Anlass", 18), ("einzeldosis", "Einzeldosis", 9), ("hoechstens", "höchstens", 11),
        ("arzt", "verordnet von", 13), ("seit", "seit", 9), ("status", "Status", 14), ("hinweis", "Hinweis", 16));

    // Füllt beide Tabellen mit dem, was am Tag gilt (Abgesetztes nur auf Wunsch). Was sich in den letzten sieben Tagen
    // geändert hat, ist hervorgehoben und sagt im Status, was („neu seit 09.11.“, „Dosis geändert: … → …“).
    public static void TabellenFuellen(DataGridView regel, DataGridView bedarf, IEnumerable<Tagesstand> staende, bool abgesetzteZeigen)
    {
        var liste = staende.ToList();
        var heute = DateOnly.FromDateTime(DateTime.Now);
        bool Gezeigt(Tagesstand s) => s.Sichtbar || s.Zustand == Zustand.NochNicht || (abgesetzteZeigen && s.Zustand == Zustand.Abgesetzt);
        string Status(Tagesstand s) => MedikationsAenderungen.ZeilenText(s.Verordnung, heute) is string neu ? $"{neu} · {StatusText(s)}" : StatusText(s);
        PlanTabelle.Fuellen(regel, liste.Where(s => Gezeigt(s) && !s.Verordnung.Bedarf), s =>
        {
            var v = s.Verordnung;
            var d = s.Dosis.Werte.Select(w => Menge(w, v.Einheit)).ToArray();
            return [MedikamentText(v), d[0], d[1], d[2], d[3], v.Anordnung.Arzt, v.Beginn.ToDateTime(TimeOnly.MinValue), Status(s), v.Hinweis];
        });
        PlanTabelle.Fuellen(bedarf, liste.Where(s => Gezeigt(s) && s.Verordnung.Bedarf), s =>
        {
            var v = s.Verordnung;
            return [MedikamentText(v), v.BedarfAnlass, Menge(v.Dosis.Morgens, v.Einheit), v.BedarfHoechstens.Length > 0 ? v.BedarfHoechstens : "keine Angabe",
                    v.Anordnung.Arzt, v.Beginn.ToDateTime(TimeOnly.MinValue), Status(s), v.Hinweis];
        });
        foreach (var t in new[] { regel, bedarf })
            foreach (DataGridViewRow zeile in t.Rows)
            {
                if (zeile.Tag is not Tagesstand s || MedikationsAenderungen.ZeilenText(s.Verordnung, heute) is null) continue;
                zeile.DefaultCellStyle.BackColor = Stil.MedikationZeile;
                var status = zeile.Cells["status"].Style;
                status.BackColor = status.SelectionBackColor = Stil.MedikationGeaendert.Flaeche;
                status.ForeColor = status.SelectionForeColor = Stil.MedikationGeaendert.Text;
            }
    }
    public Label RegelLeer { get; } = Leer("Keine regelmäßige Medikation an diesem Tag.");
    public Label BedarfLeer { get; } = Leer("Keine Bedarfsmedikation.");

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public MedikationAnsicht(Speicher speicher)
    {
        this.speicher = speicher;
        BackColor = Stil.Seite;
        var beschriftung = new Label { Text = "Was gilt am", AutoSize = true, ForeColor = Stil.TextMittel, BackColor = Stil.Seite, Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0) };
        foreach (var c in new Control[] { beschriftung, TagFeld, HeuteKnopf, NeuKnopf, DruckKnopf, AbgesetzteZeigen, Hinweis, TagFehler })
        {
            if (c != beschriftung) c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        TagFeld.Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandKlein, Stil.AbstandKlein);
        AbgesetzteZeigen.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        Hinweis.Margin = new Padding(0, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        TagFeld.TextChanged += (_, _) => Aktualisieren();
        HeuteKnopf.Click += (_, _) => TagFeld.Text = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        NeuKnopf.Click += (_, _) => NeueVerordnung();
        DruckKnopf.Click += (_, _) => Drucken();
        AbgesetzteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Tabelle.AccessibleName = "Medikamentenplan, regelmäßig";
        BedarfTabelle.AccessibleName = "Medikamentenplan, bei Bedarf";
        foreach (var t in new[] { Tabelle, BedarfTabelle })
        {
            t.CellClick += (_, e) => { if (e.RowIndex >= 0) Oeffnen(PlanTabelle.Stand(t.Rows[e.RowIndex])); };
            t.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter || t.CurrentRow is null) return;
                e.SuppressKeyPress = true;
                Oeffnen(PlanTabelle.Stand(t.CurrentRow));
            };
        }
        Controls.AddRange([leiste, regelTitel, Tabelle, RegelLeer, bedarfTitel, BedarfTabelle, BedarfLeer]);
    }

    private static Label Titel(string text) => new() { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite };

    private static Label Leer(string text) => new() { Text = text, AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, Visible = false };

    public void Zeigen(Bewohner b)
    {
        bewohner = b;
        string heute = Datum.Text(DateOnly.FromDateTime(Jetzt()));
        if (TagFeld.Text != heute) TagFeld.Text = heute;   // löst Aktualisieren aus
        else Aktualisieren();
    }

    public DateOnly? Stichtag()
    {
        try { return Datum.Lesen(TagFeld.Text, "Tag"); }
        catch (FormatException) { return null; }
    }

    public List<Tagesstand> Staende(DateOnly tag) =>
        bewohner?.Id is long id ? speicher.Verordnungen(id).Select(v => MedikationRegeln.Stand(v, tag)).ToList() : [];

    // Zeilen der Tabelle „Regelmäßig“ in der gezeigten Reihenfolge (nach Sortierung).
    public List<Tagesstand> Eintraege => PlanTabelle.Staende(Tabelle);
    public List<Tagesstand> BedarfEintraege => PlanTabelle.Staende(BedarfTabelle);

    // „aktiv“, „pausiert bis …“, „Stufe 2 von 4 · nächste Stufe ab …“, „abgesetzt am …“, „beginnt am …“.
    public static string StatusText(Tagesstand s) =>
        (s.Zustand == Zustand.Gilt ? "aktiv" : s.Text) + (s.Naechstes is string n ? $" · {n}" : "");

    public void Aktualisieren()
    {
        if (bewohner is null) return;
        Stil.Sperren(NeuKnopf, Recht.Grund(speicher, Funktionen.MedikationBearbeiten));   // Rechte
        var tag = Stichtag();
        TagFehler.Visible = tag is null;
        TagFehler.Text = "Tag bitte als TT.MM.JJJJ eingeben.";
        var staende = tag is DateOnly t ? Staende(t) : [];
        TabellenFuellen(Tabelle, BedarfTabelle, staende, AbgesetzteZeigen.Checked);
        Tabelle.Visible = Tabelle.Rows.Count > 0;
        RegelLeer.Visible = tag is not null && Tabelle.Rows.Count == 0;
        BedarfTabelle.Visible = BedarfTabelle.Rows.Count > 0;
        BedarfLeer.Visible = tag is not null && BedarfTabelle.Rows.Count == 0;
        PerformLayout();
    }

    public static string MedikamentText(Verordnung v) => v.Medikament + (v.Staerke.Length > 0 ? $" · {v.Staerke}" : " · Stärke: keine Angabe");

    // Menge je Einnahme; die Einheit nur, wenn es nicht „Stück“ sind (z. B. „20 Tropfen“).
    public static string Menge(string wert, string einheit) =>
        einheit.Length == 0 || einheit == "Stück" || wert == "0" ? wert : $"{wert} {einheit}";

    // Klick auf eine Zeile: Einzelheiten und Verlauf. Wählt man dort eine Änderung, öffnet sie sich danach.
    public void Oeffnen(Tagesstand s)
    {
        string? grund = Recht.Grund(speicher, Funktionen.MedikationBearbeiten);
        // „Bedarf gegeben …“ an Bedarfsverordnungen (eigenes Recht; Assistenzkräfte mit Absprache).
        string? bedarfGrund = Recht.Grund(speicher, Funktionen.BedarfGeben);
        string? heute = s.Verordnung.Bedarf ? BedarfRegeln.Letzte24Kurz(speicher.BedarfsgabenMittel(s.Verordnung), DateTime.Now) : null;
        AenderungArt? wunsch;
        bool bedarf;
        using (var fenster = new VerordnungDetailFenster(s, grund, bedarfGrund, heute))
        {
            if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return;
            wunsch = fenster.Wunsch;
            bedarf = fenster.BedarfGewuenscht;
        }
        if (bedarf) BedarfGeben(s.Verordnung);
        else if (wunsch is AenderungArt art) Aendern(s.Verordnung, art);
    }

    // „Bedarf gegeben …“: Fenster; danach steht die Gabe als Bericht in der Akte. true = eingetragen.
    public bool BedarfGeben(Verordnung v)
    {
        if (bewohner is null || !v.Bedarf) return false;
        using var fenster = new BedarfFenster(speicher, v, bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK) return false;
        Aktualisieren();
        return true;
    }

    public void NeueVerordnung()
    {
        if (bewohner is null) return;
        using var fenster = new VerordnungFenster(speicher, bewohner);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Aendern(Verordnung v, AenderungArt art)
    {
        using var fenster = new AenderungFenster(speicher, v, art);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    // Aus einer Visiten-Aufgabe: das passende Formular, vorbelegt mit der Visite als
    // Anordnung (Arzt, Datum) und einem Hinweis auf die Visite. Neu verordnet (oder noch keine
    // Verordnung): „Neue Verordnung“; Dosis: Stufenplan; pausiert: Pause; abgesetzt: Absetzen.
    // true = gespeichert.
    public bool AusVisite(Visite visite, VisiteAnordnung anordnung, Medikament m)
    {
        if (bewohner?.Id is not long id) return false;
        string datum = Datum.Text(visite.Datum), hinweis = $"laut {VisitenRegeln.Kopf(visite)}";
        var heute = DateOnly.FromDateTime(Jetzt());
        var bisher = speicher.Verordnungen(id).Where(v => v.MedikamentId == m.Id)
            .Select(v => MedikationRegeln.Stand(v, heute)).Where(s => s.Zustand != Zustand.Abgesetzt)
            .OrderBy(s => s.Verordnung.Bedarf).Select(s => s.Verordnung).FirstOrDefault();
        DialogResult ergebnis;
        if (anordnung.Art == AnordnungArt.Neu || bisher is null)
        {
            using var fenster = new VerordnungFenster(speicher, bewohner);
            fenster.MedikamentWaehlen(m.Id);
            (fenster.Arzt.Text, fenster.AngeordnetAm.Text, fenster.Hinweis.Text) = (visite.Arzt, datum, hinweis);
            ergebnis = Dialog.Zeigen(fenster, FindForm());
        }
        else
        {
            var art = anordnung.Art switch { AnordnungArt.Dosis => AenderungArt.Stufenplan, AnordnungArt.Pause => AenderungArt.Pause, _ => AenderungArt.Absetzen };
            using var fenster = new AenderungFenster(speicher, bisher, art);
            (fenster.Arzt.Text, fenster.AngeordnetAm.Text, fenster.Hinweis.Text) = (visite.Arzt, datum, hinweis);
            ergebnis = Dialog.Zeigen(fenster, FindForm());
        }
        Aktualisieren();
        return ergebnis == DialogResult.OK;
    }

    public void Drucken()
    {
        if (bewohner is null || Stichtag() is not DateOnly tag) return;
        var druck = new MedikationDruck(bewohner, tag, Staende(tag));
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Medikamentenplan {bewohner.Name}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        leiste.MaximumSize = new Size(Width, 0);
        leiste.Bounds = new Rectangle(Point.Empty, leiste.GetPreferredSize(new Size(Math.Max(100, Width), 0)));   // AutoSize greift hier zu spät
        int y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var (titel, tabelle, leer) in new[] { (regelTitel, Tabelle, RegelLeer), (bedarfTitel, BedarfTabelle, BedarfLeer) })
        {
            titel.Location = new Point(Stil.Px(this, Stil.AbstandMini), y + Stil.Px(this, Stil.AbstandKlein));
            y = titel.Bottom + Stil.Px(this, Stil.AbstandKlein);
            if (tabelle.Visible)
            {
                tabelle.Width = Width;
                tabelle.Height = PlanTabelle.Hoehe(tabelle);
                tabelle.Location = new Point(0, y);
                y = tabelle.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
            if (leer.Visible)
            {
                leer.Location = new Point(Stil.Px(this, Stil.AbstandMini), y);
                y = leer.Bottom + Stil.Px(this, Stil.AbstandKlein);
            }
        }
        if (Height != y) Height = y;
    }
}
