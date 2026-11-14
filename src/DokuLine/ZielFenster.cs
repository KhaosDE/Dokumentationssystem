using DokuLine.Kern;

namespace DokuLine;

// „Neues Ziel …“ und „Ziel ändern …“ (Bauplan 43): Wortlaut in den Worten des Bewohners, Art, Lebensbereich, vereinbart
// am, mit wem, überprüfen bis, Stand mit kurzem Satz. Ändern speichert eine neue Fassung; die frühere bleibt im Verlauf.
public sealed class ZielFenster : Form
{
    private readonly Speicher speicher;
    private readonly long bewohnerId;
    private readonly Ziel? alt;
    private bool vonHand, setzt;   // „überprüfen bis“ von Hand geändert: den Vorschlag nicht mehr nachführen

    public ComboBox Art { get; } = Auswahl(160, "Art", "Leitziel", "Nahziel");
    public TextBox Wortlaut { get; } = new() { Width = 420, Height = 60, Multiline = true, AccessibleName = "Ziel in den Worten des Bewohners" };
    public ComboBox Lebensbereich { get; } = Auswahl(420, "Lebensbereich", [.. Enumerable.Range(1, 5).Select(n => $"{n} {BerichtRegeln.Lebensbereiche[n - 1]}")]);
    public TextBox VereinbartAm { get; } = new DatumFeld { Width = 100, AccessibleName = "vereinbart am" };
    public TextBox MitWem { get; } = new() { Width = 160, AccessibleName = "mit wem vereinbart" };
    public TextBox UeberpruefenBis { get; } = new DatumFeld { Width = 100, AccessibleName = "überprüfen bis" };
    public ComboBox Stand { get; } = Auswahl(140, "Stand", "aktiv", "erreicht", "geändert", "beendet");
    public TextBox StandSatz { get; } = new() { Width = 420, AccessibleName = "Stand in einem Satz", PlaceholderText = "kurz: warum erreicht, geändert oder beendet" };
    public Label Fehler { get; } = Fenster.Fehlerfeld();
    public Ziel? Gespeichert { get; private set; }
    internal Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    public ZielFenster(Speicher speicher, Bewohner bewohner, Ziel? alt = null, ZielArt art = ZielArt.Nahziel)
    {
        this.speicher = speicher;
        bewohnerId = bewohner.Id!.Value;
        this.alt = alt;
        Fenster.Grundform(this, alt is null ? $"Neues Ziel: {bewohner.Name}" : $"Ziel ändern: {bewohner.Name}");
        Rechtschreibpruefung.Anbinden(Wortlaut, speicher);
        var heute = DateOnly.FromDateTime(Jetzt());
        var f = alt?.Aktuell;
        Art.SelectedIndex = (int)(alt?.Art ?? art);
        Art.Enabled = alt is null;
        Wortlaut.Text = f?.Wortlaut ?? "";
        Lebensbereich.SelectedIndex = f is null ? -1 : f.Lebensbereich - 1;
        VereinbartAm.Text = Datum.Text(f?.VereinbartAm ?? heute);
        MitWem.Text = f?.MitWem ?? Recht.Kuerzel;
        UeberpruefenBis.Text = Datum.Text(f?.UeberpruefenBis ?? ZielRegeln.UeberpruefenVorschlag(alt?.Art ?? art, heute));
        Stand.Enabled = StandSatz.Enabled = alt is not null;
        vonHand = alt is not null;
        Art.SelectedIndexChanged += (_, _) => VorschlagSetzen();
        VereinbartAm.Leave += (_, _) => VorschlagSetzen();
        UeberpruefenBis.TextChanged += (_, _) => { if (!setzt) vonHand = true; };

        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        zeile("Art", Art);
        zeile("Ziel (Worte des Bewohners)", Wortlaut);
        zeile("Lebensbereich", Lebensbereich);
        zeile("Vereinbart am, mit", Formular.Reihe(VereinbartAm, MitWem));
        zeile("Überprüfen bis", UeberpruefenBis);
        zeile("Stand", Formular.Reihe(Stand, StandSatz));
        zeile(null, Fenster.Hinweis(alt is null
            ? $"Vorschlag für „überprüfen bis“: Nahziel {ZielRegeln.NahzielMonate}, Leitziel {ZielRegeln.LeitzielMonate} Monate nach der Vereinbarung."
            : "Die bisherige Fassung bleibt im Verlauf sichtbar. „Erreicht“, „geändert“ oder „beendet“ schließt das Ziel ab."));
        zeile(null, Fehler);
        zeile(null, Formular.Knoepfe(this, "Speichern", () => Speichern()));
        Controls.Add(raster);
        Stil.DpiSkalierungEinschalten(this);
    }

    private static ComboBox Auswahl(int breite, string name, params string[] eintraege)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = breite, AccessibleName = name };
        box.Items.AddRange(eintraege);
        if (eintraege.Length > 0) box.SelectedIndex = 0;
        return box;
    }

    private void VorschlagSetzen()
    {
        if (vonHand) return;
        try
        {
            var vereinbart = Datum.Lesen(VereinbartAm.Text, "Vereinbart am");
            setzt = true;
            UeberpruefenBis.Text = Datum.Text(ZielRegeln.UeberpruefenVorschlag((ZielArt)Math.Max(0, Art.SelectedIndex), vereinbart));
        }
        catch (FormatException) { }
        finally { setzt = false; }
    }

    public ZielFassung Lesen()
    {
        DateOnly vereinbart, bis;
        try
        {
            vereinbart = Datum.Lesen(VereinbartAm.Text, "Vereinbart am");
            bis = Datum.Lesen(UeberpruefenBis.Text, "Überprüfen bis");
        }
        catch (FormatException e) { throw new Eingabefehler([e.Message]); }
        return new ZielFassung
        {
            Wortlaut = Wortlaut.Text.Trim(), Lebensbereich = Lebensbereich.SelectedIndex + 1, VereinbartAm = vereinbart, MitWem = MitWem.Text.Trim(),
            UeberpruefenBis = bis, Stand = (ZielStand)Math.Max(0, Stand.SelectedIndex), StandSatz = StandSatz.Text.Trim(),
        };
    }

    public bool Speichern()
    {
        try
        {
            var f = Lesen();
            if (alt is null) Gespeichert = speicher.ZielAnlegen(bewohnerId, (ZielArt)Math.Max(0, Art.SelectedIndex), f, Recht.Kuerzel, Jetzt());
            else speicher.ZielAendern(alt.Id, f, Recht.Kuerzel, Jetzt());
        }
        catch (Eingabefehler e)
        {
            Fehler.Text = string.Join(" ", e.Fehler);
            Fehler.Visible = true;
            return false;
        }
        DialogResult = DialogResult.OK;
        Close();
        return true;
    }
}
