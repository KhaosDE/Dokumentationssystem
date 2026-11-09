using DokuLine.Kern;

namespace DokuLine;

// „Bedarf gegeben …“ an einer Bedarfsverordnung: Uhrzeit (jetzt), Menge (Einzeldosis), Anlass (aus
// der Verordnung) und „in Absprache mit“ – Pflicht für Assistenzkräfte, nur Fachkräfte wählbar. Oben steht,
// was in den letzten 24 Stunden gegeben wurde (dasselbe Medikament) und was höchstens erlaubt ist. Würde die
// Gabe die Höchstmenge überschreiten: Warnung, Eintragen erst beim zweiten Klick. Die Gabe steht danach als
// Bericht in der Akte.
public sealed class BedarfFenster : Form
{
    private readonly Speicher speicher;
    private readonly Verordnung verordnung;
    private readonly List<Mitarbeiter> fachkraefte;
    private string? gewarntFuer;

    public Label Bisher { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), Font = Stil.Fett, UseMnemonic = false };
    public Label Hoechstens { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.TextMittel, UseMnemonic = false };
    public TextBox Uhrzeit { get; } = new() { Width = 80 };
    public TextBox Menge { get; } = new() { Width = 80 };
    public TextBox Anlass { get; } = new() { Width = 360 };
    public ComboBox Absprache { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360, AccessibleName = "In Absprache mit" };
    public Label AbspracheHinweis { get; } = Fenster.Hinweis("");
    public Label Wer { get; } = new() { AutoSize = true, Font = Stil.Fett, UseMnemonic = false, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) };
    public Label Warnung { get; } = new() { AutoSize = true, MaximumSize = new Size(520, 0), ForeColor = Stil.Achtung.Text, BackColor = Stil.Achtung.Flaeche, Padding = new Padding(Stil.AbstandKlein), Visible = false };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Button SpeichernKnopf { get; }
    public Bedarfsgabe? Gespeichert { get; private set; }
    public const string OhneAbsprache = "(ohne – ich bin Fachkraft)";

    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public BedarfFenster(Speicher speicher, Verordnung v, Bewohner bewohner)
    {
        this.speicher = speicher;
        verordnung = v;
        Fenster.Grundform(this, $"Bedarf gegeben – {bewohner.Name}");
        var ich = speicher.MitarbeiterLesen(Recht.Kuerzel);
        bool fachkraft = ich is not null && ich.Fachkraft;
        // Fachkräfte des Wohnbereichs zuerst, dann alle übrigen aktiven Fachkräfte; nicht man selbst.
        fachkraefte = [.. speicher.AlleMitarbeiter()
            .Where(m => m.Aktiv && m.Fachkraft && !m.Kuerzel.Equals(Recht.Kuerzel, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Bereiche.Contains(bewohner.Wohnbereich) ? 0 : 1).ThenBy(m => m.Nachname, StringComparer.CurrentCulture)];
        Absprache.Items.Add(fachkraft ? OhneAbsprache : "– bitte wählen –");
        foreach (var m in fachkraefte) Absprache.Items.Add($"{m.Kuerzel} – {m.Name} ({string.Join(", ", m.Bereiche)})");
        Absprache.SelectedIndex = 0;
        AbspracheHinweis.Text = fachkraft
            ? "Als Fachkraft ohne Absprache möglich; wer dabei war, kann trotzdem eingetragen werden."
            : "Pflicht für Assistenzkräfte: Bedarfsmedikation nur nach Absprache mit einer Fachkraft.";

        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile(null, new Label
        {
            Text = $"{v.Medikament}{(v.Staerke.Length > 0 ? $" · {v.Staerke}" : "")} – bei Bedarf: {v.BedarfAnlass} · Einzeldosis {BedarfRegeln.Menge(v.Dosis.Morgens, v.Einheit)}",
            AutoSize = true, MaximumSize = new Size(520, 0), Font = Stil.KarteName, UseMnemonic = false,
        });
        zeile(null, Bisher);
        zeile(null, Hoechstens);
        zeile("Uhrzeit", Uhrzeit);
        zeile($"Menge ({(v.Einheit.Length > 0 ? v.Einheit : "Stück")})", Menge);
        zeile("Anlass", Anlass);
        zeile("In Absprache mit", Absprache);
        zeile(null, AbspracheHinweis);
        zeile("Gegeben von", Wer);
        zeile(null, Warnung);
        zeile(null, Fehler);
        var knoepfe = Formular.Knoepfe(this, "Eintragen", () => Speichern());
        SpeichernKnopf = (Button)knoepfe.Controls[0];
        zeile(null, knoepfe);
        Controls.Add(raster);

        Uhrzeit.Text = Jetzt().ToString("HH:mm");
        Menge.Text = v.Dosis.Morgens;
        Anlass.Text = v.BedarfAnlass;
        Wer.Text = ich is null ? RechteRegeln.NichtAngemeldetText : $"{ich.Kuerzel} – {ich.Name} ({ich.Qualifikation})";
        Bisher.Text = BedarfRegeln.Letzte24Text(speicher.BedarfsgabenMittel(v), Jetzt(), v.Einheit);
        Hoechstens.Text = BedarfRegeln.HoechstensText(v);
        Stil.DpiSkalierungEinschalten(this);
    }

    // „AB“ aus der Auswahl (leer = ohne).
    public string AbspracheKuerzel => Absprache.SelectedIndex > 0 ? fachkraefte[Absprache.SelectedIndex - 1].Kuerzel : "";

    public bool AbspracheWaehlen(string kuerzel)
    {
        int i = fachkraefte.FindIndex(m => m.Kuerzel.Equals(kuerzel, StringComparison.OrdinalIgnoreCase));
        if (i < 0) return false;
        Absprache.SelectedIndex = i + 1;
        return true;
    }

    // Erster Klick bei erreichter Höchstmenge: Warnung; zweiter Klick (gleiche Uhrzeit): eintragen.
    public bool Speichern()
    {
        var e = new BedarfEingabe { VerordnungId = verordnung.Id, Uhrzeit = Uhrzeit.Text, Menge = Menge.Text, Anlass = Anlass.Text, Absprache = AbspracheKuerzel };
        try
        {
            var jetzt = Jetzt();
            if (BedarfRegeln.Zeitpunkt(Uhrzeit.Text, jetzt) is DateTime zeit)
            {
                string fuer = $"{Uhrzeit.Text}|{Menge.Text}";   // geänderte Uhrzeit oder Menge: neu warnen
                if (BedarfRegeln.Warnung(verordnung, speicher.BedarfsgabenMittel(verordnung), zeit, BedarfRegeln.MengeZahl(Menge.Text)) is string warnung && gewarntFuer != fuer)
                {
                    gewarntFuer = fuer;
                    Warnung.Text = warnung + " Trotzdem eintragen? Dann noch einmal „Trotzdem eintragen“.";
                    Warnung.Visible = true;
                    SpeichernKnopf.Text = "Trotzdem eintragen";
                    Fehler.Visible = false;
                    return false;
                }
            }
            Gespeichert = speicher.BedarfGeben(e, Recht.Kuerzel, jetzt);
        }
        catch (Eingabefehler f)
        {
            Fehler.Text = "Bitte prüfen:\n• " + string.Join("\n• ", f.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
