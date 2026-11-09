using System.Drawing.Printing;
using DokuLine.Kern;

namespace DokuLine;

// Bereich „Aufgaben“ (Wohnbereich-ToDo): Aufgaben bleiben stehen, bis sie erledigt sind.
// Überfällige stehen oben, rot und mit dem Wort „überfällig“. Erledigte lassen sich einblenden;
// gelöscht wird nichts.
public sealed class AufgabenSeite : Panel, IBereichSeite
{
    string IBereichSeite.Titel => "Aufgaben";
    IReadOnlyList<Button> IBereichSeite.Knoepfe => [NeuKnopf];
    void IBereichSeite.Zeigen(bool filterBehalten) => Aktualisieren();

    private readonly Speicher speicher;
    private readonly FlowLayoutPanel leiste = new() { AutoSize = true, WrapContents = true, BackColor = Stil.Seite };
    private readonly List<Control> inhalt = [];

    public Button NeuKnopf { get; } = Stil.HauptKnopf("Neue Aufgabe …");
    public Button DruckKnopf { get; } = Stil.NebenKnopf("Aufgaben der Schicht drucken …");
    public CheckBox ErledigteZeigen { get; } = new() { Text = "Erledigte zeigen", AutoSize = true, BackColor = Stil.Seite };
    public Label KuerzelHinweis { get; } = new() { AutoSize = true, Font = Stil.Klein, ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandMini) };
    public List<AufgabeKarte> Karten { get; } = [];
    public List<string> Abschnitte { get; } = [];
    public int Offen { get; private set; }
    public int Ueberfaellig { get; private set; }
    public event Action? Geaendert;
    // Aufgabe aus einer Visite ausführen (Medikamentenplan, Medikament anlegen, Termin) – das Hauptfenster.
    public event Action<Aufgabe>? VisitenAufgabeGewuenscht;

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public AufgabenSeite(Speicher speicher)
    {
        this.speicher = speicher;
        AutoScroll = true;
        BackColor = Stil.Seite;
        // Standardaufgaben verwalten: Einstellungen → „Aufgaben“.
        foreach (var c in new Control[] { DruckKnopf, ErledigteZeigen, KuerzelHinweis })
        {
            c.Margin = new Padding(0, 0, Stil.AbstandKlein, Stil.AbstandKlein);
            leiste.Controls.Add(c);
        }
        ErledigteZeigen.Margin = new Padding(Stil.AbstandKlein, Stil.AbstandKlein, Stil.AbstandKlein, 0);
        KuerzelHinweis.Margin = new Padding(0, Stil.AbstandKlein, 0, 0);
        KuerzelHinweis.Text = "Zum Eintragen, Erledigen und Weitergeben bitte zuerst oben rechts anmelden.";
        NeuKnopf.Click += (_, _) => NeueAufgabe();
        DruckKnopf.Click += (_, _) => Drucken();
        ErledigteZeigen.CheckedChanged += (_, _) => Aktualisieren();
        Controls.Add(leiste);
    }

    private static string Kuerzel => Einstellungen.Laden().Kuerzel;

    public string KopfText => (Offen == 1 ? "1 offene Aufgabe" : $"{Offen} offene Aufgaben") + (Ueberfaellig > 0 ? $" · {Ueberfaellig} überfällig" : "");

    public void Aktualisieren()
    {
        var jetzt = Jetzt();
        string kuerzel = Kuerzel;
        speicher.StandardaufgabenErzeugen(jetzt);   // Standardaufgaben der laufenden Schicht
        var alle = speicher.Aufgaben(jetzt, auchErledigte: true, Arbeitsbereich.Gewaehlt);
        var offen = alle.Where(a => a.Offen).ToList();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        Offen = offen.Count;
        Ueberfaellig = offen.Count(a => AufgabenRegeln.Ueberfaellig(a, jetzt));
        KuerzelHinweis.Visible = kuerzel.Length == 0;
        Recht.NurAngemeldet(NeuKnopf);   // ohne Anmeldung nur ansehen

        SuspendLayout();
        foreach (var c in inhalt) { Controls.Remove(c); c.Dispose(); }
        inhalt.Clear();
        Karten.Clear();
        Abschnitte.Clear();

        void Abschnitt(string text)
        {
            Abschnitte.Add(text);
            inhalt.Add(new Label { Text = text, AutoSize = true, Font = Stil.Abschnitt, ForeColor = Stil.TextLeise, BackColor = Stil.Seite, UseMnemonic = false });
        }
        void Karte(Aufgabe a)
        {
            (string, string?)? aktion = null;
            if (a.Offen && (a.VisiteAnordnungId is not null || a.VisiteEintragId is not null) && speicher.VisitenAufgabe(a) is { } info)
                aktion = (VisitenAblauf.KnopfText(info.Aktion), VisitenAblauf.Sperrgrund(speicher, info.Aktion));
            var karte = new AufgabeKarte(a, a.BewohnerId is long id ? namen.GetValueOrDefault(id) : null, jetzt, kuerzel, aktion);
            // Facharzttermin: „Erledigt“ fragt nach Datum, Uhrzeit und Ort.
            karte.ErledigtGeklickt += x =>
            {
                if (x.VisiteEintragId is not null) VisitenAufgabeGewuenscht?.Invoke(x);
                else Ausfuehren(() => speicher.AufgabeErledigt(x.Id, Kuerzel, Jetzt()));
            };
            karte.AktionGeklickt += x => VisitenAufgabeGewuenscht?.Invoke(x);
            karte.WeitergebenGeklickt += Weitergeben;
            karte.SchliessenGeklickt += Schliessen;
            Karten.Add(karte);
            inhalt.Add(karte);
        }

        // Gruppiert nach Schicht, oben die laufende (Liegengebliebenes davor).
        foreach (var (titel, aufgaben) in AufgabenRegeln.Gruppen(offen, jetzt))
        {
            Abschnitt($"{titel} ({aufgaben.Count})");
            aufgaben.ForEach(Karte);
            if (aufgaben.Count == 0)
                inhalt.Add(new Label { Text = "Für diese Schicht ist nichts offen.", AutoSize = true, ForeColor = Stil.TextLeise, BackColor = Stil.Seite });
        }
        if (ErledigteZeigen.Checked)
        {
            var erledigt = alle.Where(a => !a.Offen).OrderByDescending(a => a.Erledigt).ToList();
            Abschnitt($"ERLEDIGT ({erledigt.Count})");
            erledigt.ForEach(Karte);
        }
        foreach (var c in inhalt) Controls.Add(c);
        ResumeLayout(false);
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Geaendert?.Invoke();
    }

    private void Ausfuehren(Action aktion)
    {
        try { aktion(); }
        catch (Eingabefehler e) { MessageBox.Show(FindForm(), string.Join("\n", e.Fehler), "Aufgaben", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        Aktualisieren();
    }

    public void NeueAufgabe()
    {
        using var fenster = new AufgabeFenster(speicher, null, null);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    // Begründet schließen: nicht erledigt, aber erledigt im Sinne von „nicht mehr zu tun“.
    public void Schliessen(Aufgabe a)
    {
        using var fenster = new SchliessenFenster(speicher, a);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Weitergeben(Aufgabe a)
    {
        using var fenster = new WeitergebenFenster(speicher, a);
        if (Dialog.Zeigen(fenster, FindForm()) == DialogResult.OK) Aktualisieren();
    }

    public void Drucken()
    {
        var jetzt = Jetzt();
        var namen = speicher.AlleBewohner().ToDictionary(b => b.Id!.Value, b => $"{b.Name}, Zi. {b.Zimmer}");
        // Aufgabenliste der Schicht: was jetzt dran ist (auch Liegengebliebenes) und „wer Zeit hat“.
        var schicht = SchichtTermin.Zu(jetzt);
        var liste = speicher.Aufgaben(jetzt, bereich: Arbeitsbereich.Gewaehlt)
            .Where(a => AufgabenRegeln.JetztDran(a, jetzt) || AufgabenRegeln.Zielschicht(a) is null).ToList();
        var druck = new AufgabenDruck(liste, namen, jetzt, $"Aufgaben – {schicht.Text}");
        Druckvorschau.Zeigen(FindForm(), druck.DokumentBauen, $"Aufgaben – {schicht.Text}");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = Stil.Px(this, Stil.AbstandGross);
        int breite = Math.Max(100, ClientSize.Width - 2 * rand);
        int y = rand + AutoScrollPosition.Y;
        leiste.MaximumSize = new Size(breite, 0);
        leiste.Location = new Point(rand, y);
        y = leiste.Bottom + Stil.Px(this, Stil.AbstandKlein);
        foreach (var c in inhalt)
        {
            if (c is AufgabeKarte karte)
            {
                karte.SetBounds(rand, y, breite, karte.Height);
                karte.PerformLayout();
                y += karte.Height + Stil.Px(this, Stil.AbstandKlein);
            }
            else
            {
                y += Stil.Px(this, Stil.AbstandMittel);
                c.Location = new Point(rand + Stil.Px(this, Stil.AbstandMini), y);
                y += c.Height + Stil.Px(this, Stil.AbstandMini);
            }
        }
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + rand);
    }
}
