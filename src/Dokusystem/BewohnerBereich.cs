using Dokusystem.Kern;

namespace Dokusystem;

// Bereich „Bewohner“: Übersicht als Kartenraster, Klick öffnet die Akte.
// Der Kopf (KopfBereich) zeigt je Ansicht passende Texte und Knöpfe; pro Ansicht genau eine
// Hauptaktion in Akzentfarbe.
public sealed class BewohnerBereich : Panel
{
    private readonly Speicher speicher;
    private readonly KopfBereich kopf;
    private readonly Button neuKnopf = Stil.HauptKnopf("Neuer Bewohner …");
    private readonly Button zurueckKnopf = Stil.NebenKnopf("← Alle Bewohner");
    private readonly Button bearbeitenKnopf = Stil.HauptKnopf("Bearbeiten …");

    public KartenRaster Raster { get; } = new() { Dock = DockStyle.Fill };
    public AkteAnsicht Akte { get; } = new() { Dock = DockStyle.Fill, Visible = false };
    // Eigenes Feld statt Akte.Visible: Visible meldet „unsichtbar“, solange das Fenster selbst
    // noch nicht angezeigt ist.
    public bool AkteSichtbar { get; private set; }

    // Zum Testen ersetzbar: öffnet das Formular als Dialog und gibt den gespeicherten Bewohner zurück.
    internal Func<BewohnerFormular, Bewohner?> FormularZeigen { get; set; } =
        f => f.ShowDialog() == DialogResult.OK ? f.Gespeichert : null;

    public BewohnerBereich(Speicher speicher, KopfBereich kopf)
    {
        this.speicher = speicher;
        this.kopf = kopf;
        BackColor = Stil.Seite;
        Controls.Add(Raster);
        Controls.Add(Akte);
        neuKnopf.Click += (_, _) => Formular(null);
        zurueckKnopf.Click += (_, _) => UebersichtZeigen();
        bearbeitenKnopf.Click += (_, _) =>
        {
            if (Akte.Bewohner?.Id is long id) Formular(speicher.Bewohner(id));
        };
    }

    // Kopf und Inhalt für die Übersicht.
    public void UebersichtZeigen()
    {
        var alle = speicher.AlleBewohner();
        Raster.KartenSetzen(alle.Select(b =>
        {
            var karte = new BewohnerKarte(b);
            karte.Gewaehlt += gewaehlt => AkteZeigen(gewaehlt.Id!.Value);
            return karte;
        }));
        Akte.Visible = false;
        Raster.Visible = true;
        AkteSichtbar = false;
        kopf.TexteSetzen(alle.Count == 1 ? "1 Bewohner" : $"{alle.Count} Bewohner", "Bewohner");
        kopf.KnoepfeSetzen(neuKnopf);
    }

    public void AkteZeigen(long id)
    {
        if (speicher.Bewohner(id) is not Bewohner b)
        {
            UebersichtZeigen();
            return;
        }
        Akte.Zeigen(b);
        Raster.Visible = false;
        Akte.Visible = true;
        AkteSichtbar = true;
        kopf.TexteSetzen("Bewohnerakte", b.Name);
        kopf.KnoepfeSetzen(zurueckKnopf, bearbeitenKnopf);
    }

    public BewohnerFormular FormularErstellen(Bewohner? b) => new(speicher, b);

    private void Formular(Bewohner? b)
    {
        using var formular = FormularErstellen(b);
        if (FormularZeigen(formular) is Bewohner gespeichert) AkteZeigen(gespeichert.Id!.Value);
    }
}

// Die Akte eines Bewohners: Kopfkarte (Bild, Name, Zimmer), Allergiehinweis gut sichtbar oben,
// darunter die Stammdaten.
public sealed class AkteAnsicht : Panel
{
    private const int MaxBreite = 880;
    private const int BildGroesse = 96;

    private readonly KartenFlaeche kopfKarte = new();
    private readonly BildFeld bild = new();
    private readonly Label name = new();
    private readonly MarkenFeld zimmer = new("Zimmer", Stil.Zimmer);
    private readonly Label abschnitt = new();
    private readonly KartenFlaeche stammdaten = new();
    private readonly TableLayoutPanel tabelle = new();

    public HinweisFeld Allergie { get; } = new();
    public Label Geburtsdatum { get; } = new();
    public Label Aufnahmedatum { get; } = new();
    public Label Beschlussende { get; } = new();
    public Bewohner? Bewohner { get; private set; }

    public AkteAnsicht()
    {
        AutoScroll = true;
        BackColor = Stil.Seite;
        DoubleBuffered = true;

        name.AutoSize = true;
        name.Font = Stil.Ueberschrift;
        name.ForeColor = Stil.TextNormal;
        name.BackColor = Stil.Flaeche;
        name.UseMnemonic = false;
        kopfKarte.Controls.AddRange([bild, name, zimmer]);

        abschnitt.Text = "STAMMDATEN";
        abschnitt.AutoSize = true;
        abschnitt.Font = Stil.Abschnitt;
        abschnitt.ForeColor = Stil.TextLeise;
        abschnitt.BackColor = Stil.Seite;

        tabelle.ColumnCount = 2;
        tabelle.AutoSize = true;
        tabelle.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        tabelle.BackColor = Stil.Flaeche;
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        foreach (var (beschriftung, wert) in new[] { ("Geburtsdatum", Geburtsdatum), ("Aufnahmedatum", Aufnahmedatum), ("Beschlussende", Beschlussende) })
        {
            tabelle.Controls.Add(new Label
            {
                Text = beschriftung, AutoSize = true, Font = Stil.Standard, ForeColor = Stil.TextMittel,
                Margin = new Padding(0, Stil.AbstandMini, Stil.AbstandGross * 2, Stil.AbstandMini),
            });
            wert.AutoSize = true;
            wert.Font = Stil.Fett;
            wert.ForeColor = Stil.TextNormal;
            wert.Margin = new Padding(0, Stil.AbstandMini, 0, Stil.AbstandMini);
            wert.UseMnemonic = false;
            tabelle.Controls.Add(wert);
        }
        stammdaten.Controls.Add(tabelle);

        Controls.AddRange([kopfKarte, Allergie, abschnitt, stammdaten]);
    }

    private int S(int wert) => Stil.Px(this, wert);

    public void Zeigen(Bewohner b)
    {
        Bewohner = b;
        bild.Setzen(Bilder.Pfad(b.Bild), BewohnerRegeln.Initialen(b), b.Nachname);
        name.Text = b.Name;
        zimmer.Text = $"Zimmer {b.Zimmer}";
        AccessibleName = $"Akte von {b.Name}";

        // Allergiehinweis: drei klar verschiedene Darstellungen (Regel 5), immer mit Wort.
        var (marke, gestrichelt) = b.Allergie switch
        {
            AllergieZustand.Eingetragen => (Stil.Beschluss, false),
            AllergieZustand.KeineBekannt => (Stil.Offen, false),
            _ => (Stil.Achtung, true),
        };
        Allergie.Setzen(BewohnerRegeln.AllergieText(b), marke, gestrichelt);

        Geburtsdatum.Text = Datum.Text(b.Geburtsdatum);
        Aufnahmedatum.Text = Datum.Text(b.Aufnahmedatum);
        Beschlussende.Text = BewohnerRegeln.BeschlussText(b);
        bool offen = b.Beschluss == BeschlussZustand.NichtErfasst;
        Beschlussende.ForeColor = offen ? Stil.Achtung.Text : Stil.TextNormal;
        Beschlussende.Font = offen ? Stil.FettKursiv : Stil.Fett;
        AutoScrollPosition = Point.Empty;
        PerformLayout();
        Invalidate(true);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int rand = S(Stil.AbstandGross);
        int breite = Math.Min(ClientSize.Width - 2 * rand, S(MaxBreite));
        int x = rand, y = rand + AutoScrollPosition.Y;
        int innen = S(Stil.AbstandGross);

        int bildSeite = S(BildGroesse);
        int kopfHoehe = bildSeite + 2 * innen;
        kopfKarte.Bounds = new Rectangle(x, y, breite, kopfHoehe);
        bild.Bounds = new Rectangle(innen, innen, bildSeite, bildSeite);
        int textX = innen + bildSeite + S(Stil.AbstandGross);
        int blockHoehe = name.PreferredHeight + S(Stil.AbstandKlein) + zimmer.Height;
        int textY = (kopfHoehe - blockHoehe) / 2;
        name.Location = new Point(textX, textY);
        zimmer.Location = new Point(textX, textY + name.PreferredHeight + S(Stil.AbstandKlein));
        y += kopfHoehe + S(Stil.AbstandMittel);

        Allergie.Bounds = new Rectangle(x, y, breite, S(52));
        y += Allergie.Height + S(Stil.AbstandGross + Stil.AbstandKlein);

        abschnitt.Location = new Point(x + S(Stil.AbstandMini), y);
        y += abschnitt.PreferredHeight + S(Stil.AbstandMini);

        var groesse = tabelle.GetPreferredSize(Size.Empty);
        stammdaten.Bounds = new Rectangle(x, y, breite, groesse.Height + 2 * innen);
        tabelle.Location = new Point(innen, innen);
        tabelle.Size = groesse;
        y += stammdaten.Height + rand;
        AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y);
    }
}
