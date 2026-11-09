using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using DokuLine.Kern;

namespace DokuLine;

// Termin anlegen oder ändern (neue Fassung). Je nach Art passende Felder.
public sealed class KalenderFenster : Form
{
    private readonly Speicher speicher;
    private readonly KalenderTermin? bisher;
    private readonly List<Bewohner> bewohner;

    public RadioButton[] Arten { get; } = Enum.GetValues<KalenderArt>()
        .Select(a => new RadioButton { Text = KalenderRegeln.ArtText(a) == "Termin" ? "Termin eines Bewohners" : KalenderRegeln.ArtText(a), AutoSize = true }).ToArray();
    public RadioButton BereichWahl { get; } = new() { Text = "Bereich", AutoSize = true, Checked = true };
    public RadioButton HausWahl { get; } = new() { Text = "Haus", AutoSize = true };
    public TextBox Titel { get; } = new() { Width = 320 };
    public TextBox BeginnDatum { get; } = new DatumFeld() { Width = 100 };
    public TextBox BeginnUhrzeit { get; } = new UhrzeitFeld() { Width = 60 };
    public TextBox EndeDatum { get; } = new DatumFeld() { Width = 100, PlaceholderText = "gleicher Tag" };
    public TextBox EndeUhrzeit { get; } = new UhrzeitFeld() { Width = 60 };
    public CheckBox Ganztags { get; } = new() { Text = "ganztags", AutoSize = true };
    public CheckedListBox BewohnerListe { get; } = new() { Width = 320, Height = 150, CheckOnClick = true, IntegralHeight = false };
    public TextBox Ort { get; } = new() { Width = 320 };
    public TextBox Treffpunkt { get; } = new() { Width = 320 };
    public PersonenWahl Begleitung { get; }   // Auswahl aus den Mitarbeitern
    public ArztWahl Arzt { get; }   // Termin beim Arzt (füllt Ort und Hinweis)
    public TextBox Hinweis { get; } = new() { Width = 420 };
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public KalenderTermin? Gespeichert { get; private set; }

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public KalenderFenster(Speicher speicher, KalenderTermin? bisher, DateOnly vorschlag, long? bewohnerId)
    {
        this.speicher = speicher;
        this.bisher = bisher;
        bewohner = speicher.AlleBewohner();
        Begleitung = new PersonenWahl(speicher) { AccessibleName = "Begleitung" };
        Arzt = new ArztWahl(speicher, bewohnerId);
        Arzt.Gewechselt += a =>   // Termin beim Arzt – Ort und Kontakt aus dem Verzeichnis
        {
            if (a is null) return;
            if (Ort.Text.Trim().Length == 0) Ort.Text = a.Praxis;
            string kontakt = PersonRegeln.Kontakt(a);
            if (!Hinweis.Text.Contains(a.Name, StringComparison.Ordinal)) Hinweis.Text = Hinweis.Text.Trim().Length == 0 ? kontakt : $"{kontakt} · {Hinweis.Text.Trim()}";
        };
        Text = (bisher is null ? "Neuer Termin" : "Termin ändern") + $" ({KopfBereich.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var raster = Formular.Raster(out var zeile);
        var arten = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(460, 0), Margin = new Padding(0) };
        arten.Controls.AddRange(Arten);
        zeile("Art", arten);
        // Kalender „Haus“ nur mit dem Recht „Kalender Haus bearbeiten“.
        zeile("Kalender", Formular.Reihe(BereichWahl, HausWahl));
        if (Recht.Grund(speicher, Funktionen.KalenderHaus) is string hausGrund)
        {
            HausWahl.Enabled = false;
            HausWahl.Text = $"Haus ({hausGrund.TrimEnd('.')})";
        }
        zeile("Titel / Ziel", Titel);
        zeile("Beginn", Formular.Reihe(BeginnDatum, BeginnUhrzeit, Ganztags));
        zeile("Ende", Formular.Reihe(EndeDatum, EndeUhrzeit));
        BewohnerListe.Items.AddRange(bewohner.Select(b => (object)$"{b.Nachname}, {b.Vorname} (Zi. {b.Zimmer})").ToArray());
        zeile("Bewohner", BewohnerListe);
        zeile("Arzt (freiwillig)", Arzt);
        zeile("Ort", Ort);
        zeile("Treffpunkt (Ausflug)", Treffpunkt);
        zeile("Begleitung (Ausflug)", Begleitung);
        zeile("Hinweis", Hinweis);
        zeile(null, new Label { Text = "Mitgegebene Medikation bei Heimfahrten bitte im Hinweis vermerken; sie wird nicht berechnet.", AutoSize = true, MaximumSize = new Size(560, 0), Font = Stil.Klein, ForeColor = Stil.TextLeise });
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Ganztags.CheckedChanged += (_, _) => BeginnUhrzeit.Enabled = EndeUhrzeit.Enabled = !Ganztags.Checked;
        foreach (var r in Arten) r.CheckedChanged += (_, _) => FelderAnpassen();

        if (bisher is not null)
        {
            var f = bisher.Aktuell;
            Arten[(int)f.Art].Checked = true;
            (f.Haus ? HausWahl : BereichWahl).Checked = true;
            Titel.Text = f.Titel;
            BeginnDatum.Text = Datum.Text(DateOnly.FromDateTime(f.Beginn));
            EndeDatum.Text = Datum.Text(KalenderRegeln.LetzterTag(f));
            Ganztags.Checked = f.Ganztags;
            if (!f.Ganztags)
            {
                BeginnUhrzeit.Text = f.Beginn.ToString("HH:mm");
                EndeUhrzeit.Text = f.Ende.ToString("HH:mm");
                EndeDatum.Text = Datum.Text(DateOnly.FromDateTime(f.Ende));
            }
            for (int i = 0; i < bewohner.Count; i++) BewohnerListe.SetItemChecked(i, f.Bewohner.Contains(bewohner[i].Id!.Value));
            Ort.Text = f.Ort;
            Treffpunkt.Text = f.Treffpunkt;
            Begleitung.Setzen(PersonenWahl.Lesen(f.Begleitung));
            Hinweis.Text = f.Hinweis;
        }
        else
        {
            Arten[0].Checked = true;
            BeginnDatum.Text = Datum.Text(vorschlag);
            BeginnUhrzeit.Text = "10:00";
            EndeUhrzeit.Text = "11:00";
            if (bewohnerId is long id && bewohner.FindIndex(b => b.Id == id) is int i and >= 0) BewohnerListe.SetItemChecked(i, true);
        }
        FelderAnpassen();
        Stil.DpiSkalierungEinschalten(this);
    }

    public KalenderArt Art => (KalenderArt)Math.Max(0, Array.FindIndex(Arten, r => r.Checked));

    private void FelderAnpassen()
    {
        bool ausflug = Art == KalenderArt.Ausflug;
        Treffpunkt.Enabled = Begleitung.Enabled = ausflug;
        BewohnerListe.Enabled = Art != KalenderArt.Wohnbereich;
    }

    public void BewohnerAnkreuzen(long id) => BewohnerListe.SetItemChecked(bewohner.FindIndex(b => b.Id == id), true);

    private bool ohnePruefung;   // Mehrplatz: nach „Meine Fassung speichern“ im Konflikt-Hinweis

    public bool Speichern()
    {
        try
        {
            var f = KalenderRegeln.AusEingabe(new KalenderEingabe
            {
                Art = Art, Titel = Titel.Text, BeginnDatum = BeginnDatum.Text, BeginnUhrzeit = BeginnUhrzeit.Text,
                EndeDatum = EndeDatum.Text, EndeUhrzeit = EndeUhrzeit.Text, Ganztags = Ganztags.Checked, Ort = Ort.Text, Hinweis = Hinweis.Text,
                Bewohner = Art == KalenderArt.Wohnbereich ? [] : BewohnerListe.CheckedIndices.Cast<int>().Select(i => bewohner[i].Id!.Value).ToList(),
                Treffpunkt = Art == KalenderArt.Ausflug ? Treffpunkt.Text : "", Begleitung = Art == KalenderArt.Ausflug ? Begleitung.AlsText : "",
                Haus = HausWahl.Checked, Kuerzel = Recht.Kuerzel,
            }, Jetzt());
            Gespeichert = bisher is null ? speicher.KalenderAnlegen(f) : speicher.KalenderAendern(bisher.Id, f, ohnePruefung ? null : bisher.Fassungen.Count);
            // Häuser (Bauplan 37): ein Haus-Termin gilt für das Haus des gewählten Bereichs (bei „Alle“ für alle Häuser);
            // ein Termin, der kein Haus-Termin mehr ist, verliert sein Haus.
            if (!f.Haus) speicher.KalenderHausSetzen(Gespeichert.Id, null);
            else if (speicher.HausVonBereich().TryGetValue(Arbeitsbereich.Gewaehlt, out var haus)) speicher.KalenderHausSetzen(Gespeichert.Id, haus);
        }
        catch (Konflikt k)
        {
            switch (KonfliktFenster.Fragen(this, k))
            {
                case KonfliktWahl.MeineSpeichern:
                    ohnePruefung = true;
                    return Speichern();
                case KonfliktWahl.AndereUebernehmen:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return false;
                default:
                    return false;
            }
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
