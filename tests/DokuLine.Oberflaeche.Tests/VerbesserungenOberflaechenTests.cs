using System.Runtime.InteropServices;
using DokuLine.Kern;

namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26d: Nachrichten „Gelesen“ (ins Archiv, auch mehrere), Datum und Uhrzeit mit
// automatischen Punkten, Kopf der Akte.
[Collection("Oberflaeche")]
public class VerbesserungenOberflaechenTests
{
#pragma warning disable SYSLIB1054   // nur im Test: ein Tastendruck an das Feld
    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr fenster, int nachricht, IntPtr w, IntPtr l);
#pragma warning restore SYSLIB1054

    // Zeichen für Zeichen tippen, wie über die Tastatur (WM_CHAR).
    private static void Tippen(TextBox feld, string text)
    {
        foreach (char c in text) SendMessage(feld.Handle, 0x0102, c, IntPtr.Zero);
    }

    [Fact]
    public void Datum_und_Uhrzeit_mit_Punkten() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        Einstellungen.Aendern(w => w with { Kuerzel = "CD" });
        using var formular = new BerichtFormular(u.Speicher, u.Demo("Ahorn"));
        Hilfe.Zeigen(formular);
        Assert.IsType<DatumFeld>(formular.Datum);
        Assert.IsType<UhrzeitFeld>(formular.Uhrzeit);
        formular.Datum.Text = "";
        Tippen(formular.Datum, "08112026");
        Assert.Equal("08.11.2026", formular.Datum.Text);
        formular.Datum.Text = "";
        Tippen(formular.Datum, "08.11.2026");   // eigene Punkte stören nicht
        Assert.Equal("08.11.2026", formular.Datum.Text);
        formular.Uhrzeit.Text = "";
        Tippen(formular.Uhrzeit, "0930");
        Assert.Equal("09:30", formular.Uhrzeit.Text);
        // 6 Ziffern (gestern, TTMMJJ): gespeichert wird 20xx.
        var gestern = DateTime.Today.AddDays(-1);
        formular.Datum.Text = "";
        Tippen(formular.Datum, gestern.ToString("ddMMyy", System.Globalization.CultureInfo.InvariantCulture));
        formular.Inhalt.Text = "Ruhiger Vormittag.";
        formular.Bereiche[0].Checked = true;
        Assert.True(formular.Speichern());
        Assert.Equal(gestern.AddHours(9).AddMinutes(30), formular.Gespeichert!.Zeitpunkt);
        // Auch die Felder anderer Fenster sind dieselben Bausteine.
        using var termin = new KalenderFenster(u.Speicher, null, DateOnly.FromDateTime(DateTime.Today), null);
        Assert.IsType<DatumFeld>(termin.BeginnDatum);
        Assert.IsType<UhrzeitFeld>(termin.BeginnUhrzeit);
    });

    [Fact]
    public void Nachrichten_gelesen_ins_Archiv() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.BenutzerWaehlen("AB");
        f.BereichWaehlen("Nachrichten");
        var seite = f.Nachrichten;
        int vorher = seite.Karten.Count, ungelesen = u.Speicher.UngeleseneNachrichten("AB");
        Assert.True(vorher >= 2);
        Assert.True(seite.MarkierteGelesenKnopf.Visible);
        Assert.False(seite.MarkierteGelesenKnopf.Enabled);   // noch nichts markiert
        Hilfe.Foto(f, "102_nachrichten_gelesen");

        var erste = seite.Karten[0].Verlauf;
        seite.Karten[0].GelesenKnopf.PerformClick();
        Assert.Equal(vorher - 1, seite.Karten.Count);
        Assert.DoesNotContain(seite.Karten, k => k.Verlauf.Id == erste.Id);
        Assert.True(u.Speicher.UngeleseneNachrichten("AB") <= ungelesen);

        // Mehrere auf einmal.
        foreach (var k in seite.Karten) k.Auswahl.Checked = true;
        Assert.StartsWith("Markierte als gelesen (", seite.MarkierteGelesenKnopf.Text);
        seite.MarkierteGelesenKnopf.PerformClick();
        Assert.Empty(seite.Karten);
        Assert.Equal(0, u.Speicher.UngeleseneNachrichten("AB"));

        // Im Archiv: „Wieder als ungelesen“ holt zurück.
        seite.OrdnerWaehlen(NachrichtenOrdner.Archiv);
        var karte = seite.Karten.Single(k => k.Verlauf.Id == erste.Id);
        Assert.False(seite.MarkierteGelesenKnopf.Visible);
        karte.UngelesenKnopf.PerformClick();
        seite.OrdnerWaehlen(NachrichtenOrdner.Posteingang);
        Assert.Contains(seite.Karten, k => k.Verlauf.Id == erste.Id && k.Verlauf.Ungelesen > 0);
    });

    [Fact]
    public void Kopf_der_Akte() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        f.Bewohner.AkteZeigen(u.Demo("Ahorn").Id!.Value);
        Assert.Equal("Bewohnerakte", f.Kopf.TitelText);
        Assert.DoesNotContain("Ahorn", f.Kopf.TitelText + f.Kopf.KleinText);
        Hilfe.Foto(f, "103_akte_kopf");
    });
}
