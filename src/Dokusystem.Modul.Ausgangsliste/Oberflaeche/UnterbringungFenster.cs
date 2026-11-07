using Dokusystem.Kern;

namespace Dokusystem.Modul.Ausgangsliste;

// Unterbringungsstatus (übernommen aus der Ausgangsliste): Beschluss, offen geführt oder Forensik,
// danach das Datum (Beschluss: Ablaufdatum; offen geführt: Unterschrift unter die
// Einverständniserklärung). Der Beschluss steht danach auch in der Bewohnerakte.
public sealed class UnterbringungFenster : Form
{
    private readonly Datenbank db;
    private readonly Platz platz;

    public Button BeschlussKnopf { get; }
    public Button OffenKnopf { get; }
    public Button ForensikKnopf { get; }
    public Label Fehler { get; } = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.Beschluss.Text, BackColor = Stil.Beschluss.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };

    // Zum Testen ersetzbar: Ja/Nein-Rückfrage.
    public Func<string, DialogResult> Rueckfrage { get; set; }

    public UnterbringungFenster(Datenbank db, Platz platz)
    {
        this.db = db;
        this.platz = platz;
        Rueckfrage = frage => MessageBox.Show(this, frage, "Unterbringung", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        Text = $"Unterbringung – {platz.Bewohnername} ({Stil.DemoHinweis})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        BeschlussKnopf = WahlKnopf("Unterbringungsbeschluss", Unterbringung.Beschluss);
        OffenKnopf = WahlKnopf("Offen geführt", Unterbringung.OffenGefuehrt);
        ForensikKnopf = WahlKnopf("Forensik", Unterbringung.Forensik);

        var raster = Formular.Raster(out var zeile);
        zeile(null, new Label { Text = platz.Bewohnername, AutoSize = true, Font = Stil.KarteName, ForeColor = Stil.TextNormal });
        string jetzt = platz.Unterbringung == Unterbringung.NichtErfasst
            ? "Bisher nicht erfasst."
            : $"Bisher: {Verlauf.Unterbringung(platz.Unterbringung)}" +
              (platz.Unterbringung == Unterbringung.Forensik ? "" : $" · {platz.FristText(db.Jetzt())}");
        zeile(null, new Label { Text = jetzt, AutoSize = true, ForeColor = Stil.TextMittel });
        zeile(null, Formular.Reihe(BeschlussKnopf, OffenKnopf, ForensikKnopf));
        zeile(null, new Label
        {
            Text = "Beschluss: Ablaufdatum, steht danach auch in der Bewohnerakte. Offen geführt: Tag der Unterschrift " +
                   $"unter die Einverständniserklärung (gilt {Platz.EinverstaendnisMonate} Monate). Forensik: ohne Datum.",
            AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Stil.TextLeise,
        });
        zeile(null, Fehler);
        var abbrechen = Stil.NebenKnopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        abbrechen.Margin = new Padding(0, Stil.AbstandGross, 0, 0);
        CancelButton = abbrechen;
        zeile(null, abbrechen);
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private Button WahlKnopf(string text, Unterbringung wert)
    {
        bool aktiv = platz.Unterbringung == wert;
        var knopf = aktiv ? Stil.HauptKnopf($"✓ {text}") : Stil.NebenKnopf(text);
        knopf.Size = new Size(190, 56);
        knopf.Click += (_, _) => Waehlen(wert);
        return knopf;
    }

    // Wählt die Unterbringung; bei Beschluss und offen geführt folgt das Datum. true = gespeichert.
    public bool Waehlen(Unterbringung wert)
    {
        string name = wert switch
        {
            Unterbringung.Beschluss => "Unterbringungsbeschluss",
            Unterbringung.Forensik => "Forensik",
            _ => "Offen geführt",
        };
        if (platz.Unterbringung == wert && wert == Unterbringung.Forensik) return false;
        string frage = platz.Unterbringung == wert
            ? $"„{name}“ ist bereits gesetzt ({platz.FristText(db.Jetzt())}).\n\nDatum erneuern " +
              (wert == Unterbringung.Beschluss ? "(Beschluss verlängert)?" : "(Erklärung neu unterschrieben)?")
            : $"Unterbringung setzen: {name}?\n\nBewohner: {platz.Bewohnername}" +
              (wert == Unterbringung.OffenGefuehrt && platz.Status == AnwesenheitsStatus.Ausgangsbeschraenkung
                  ? $"\n\nDie laufende Ausgangsbeschränkung wird dabei AUFGEHOBEN:\n{platz.StatusGrund}\n\n{Platz.HinweisNurMitBeschluss}"
                  : "");
        if (Rueckfrage(frage) != DialogResult.Yes) return false;

        string kuerzel = Einstellungen.Laden().Kuerzel;
        try
        {
            if (wert == Unterbringung.Forensik)
                db.UnterbringungSetzen(platz.BewohnerId, wert, null, kuerzel);
            else
            {
                var heute = DateOnly.FromDateTime(db.Jetzt());
                var vorschlag = wert == Unterbringung.Beschluss
                    ? (platz.Unterbringung == wert && platz.UnterbringungDatum is DateTime d ? DateOnly.FromDateTime(d) : heute.AddMonths(6))
                    : heute;
                using var datum = new DatumFenster(name, wert == Unterbringung.Beschluss ? "Beschluss endet am" : "Unterschrieben am", vorschlag,
                    tag => db.UnterbringungSetzen(platz.BewohnerId, wert, tag, kuerzel));
                if (Dialog.Zeigen(datum, this) != DialogResult.OK) return false;
            }
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join("\n", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
