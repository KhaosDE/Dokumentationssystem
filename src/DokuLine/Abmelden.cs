using DokuLine.Kern;

namespace DokuLine;

// Abmelden (Bauplan Schritt 26a3): Knopf „Abmelden (F12)“ oben rechts, Taste F12 aus jedem Fenster,
// Symbol in der Seitenleiste und in den Einstellungen. Danach liegt eine Sperrfläche über dem ganzen
// Hauptfenster (keine Bewohnerdaten sichtbar) und das Anmeldefenster darüber.

// Ein Fenster mit Eingaben, die beim Abmelden als Entwurf bleiben können.
public interface IEntwurfFenster
{
    bool HatEingaben { get; }
    bool AlsEntwurfSpeichern();
}

public static class Abmeldung
{
    public const string Taste = "F12";
    public const string KnopfText = "Abmelden (F12)";

    // Während die offenen Fenster beim Abmelden geschlossen werden: keine eigenen Rückfragen mehr.
    public static bool SchliesstFenster { get; internal set; }
}

// Rückfrage, wenn beim Abmelden noch Fenster offen sind: „Als Entwurf speichern / Verwerfen / Abbrechen“.
public sealed class AbmeldenFrage : Form
{
    public Button EntwurfKnopf { get; } = Stil.HauptKnopf("Als Entwurf speichern");
    public Button VerwerfenKnopf { get; } = Stil.NebenKnopf("Verwerfen");
    public Button AbbrechenKnopf { get; } = Stil.NebenKnopf("Abbrechen");
    public Label Frage { get; } = new() { AutoSize = true, MaximumSize = new Size(480, 0), UseMnemonic = false };
    public bool MitEntwurf { get; }

    public AbmeldenFrage(IReadOnlyList<string> fenster, bool mitEntwurf)
    {
        MitEntwurf = mitEntwurf;
        Fenster.Grundform(this, "Abmelden");
        var raster = Formular.Raster(out var zeile);
        zeile(null, new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandKlein) });
        Frage.Text = "Noch offen: " + string.Join(", ", fenster.Select(f => $"„{f}“")) + ".\n\n" +
                     (mitEntwurf
                         ? "Angefangene Eingaben als Entwurf speichern? Er gehört Ihrem Kürzel und lässt sich nach dem nächsten Anmelden fortsetzen. " +
                           "Andere Eingaben gehen verloren."
                         : "Ungespeicherte Eingaben darin gehen verloren.");
        zeile(null, Frage);
        EntwurfKnopf.DialogResult = DialogResult.Yes;
        VerwerfenKnopf.DialogResult = DialogResult.No;
        AbbrechenKnopf.DialogResult = DialogResult.Cancel;
        EntwurfKnopf.Visible = mitEntwurf;
        var knoepfe = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, Stil.AbstandGross, 0, 0) };
        foreach (var b in new[] { EntwurfKnopf, VerwerfenKnopf, AbbrechenKnopf }) { b.AutoSize = true; b.Margin = new Padding(Stil.AbstandKlein, 0, 0, 0); }
        knoepfe.Controls.AddRange([EntwurfKnopf, VerwerfenKnopf, AbbrechenKnopf]);
        zeile(null, knoepfe);
        Controls.Add(raster);
        AcceptButton = mitEntwurf ? EntwurfKnopf : VerwerfenKnopf;
        CancelButton = AbbrechenKnopf;
        Stil.DpiSkalierungEinschalten(this);
    }
}

// F12 meldet aus jedem Fenster des Programms ab (auch aus offenen Dialogen).
internal sealed class F12Taste(Hauptfenster fenster) : IMessageFilter
{
    private const int WmKeyDown = 0x0100;

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WmKeyDown || (Keys)(int)(long)m.WParam != Keys.F12) return false;
        if (fenster.IsDisposed || !fenster.IsHandleCreated || fenster.Gesperrt || !Recht.Angemeldet) return false;
        if (Application.OpenForms.OfType<AnmeldenFenster>().Any() || Application.OpenForms.OfType<AbmeldenFrage>().Any()) return true;
        fenster.BeginInvoke(() => fenster.Abmelden());
        return true;
    }
}

// Liegt nach dem Abmelden über dem ganzen Hauptfenster: Name, Demo-Hinweis, „Anmelden …“.
public sealed class Sperrflaeche : Panel
{
    public Button AnmeldenKnopf { get; } = Stil.HauptKnopf("Anmelden …");

    public Sperrflaeche()
    {
        BackColor = Stil.Fensterhintergrund;
        Visible = false;
        var stapel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Stil.Fensterhintergrund };
        stapel.Controls.Add(new Label { Text = Programmversion.Name, AutoSize = true, Font = Stil.Ueberschrift, ForeColor = Stil.TextNormal });
        stapel.Controls.Add(new Label { Text = Programmversion.Leittext, AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, 0, 0, Stil.AbstandGross) });
        stapel.Controls.Add(new MarkenFeld(Stil.DemoHinweis, Stil.Achtung) { Margin = new Padding(0, 0, 0, Stil.AbstandGross) });
        stapel.Controls.Add(new Label { Text = "Abgemeldet. Bis sich jemand anmeldet, sind keine Daten zu sehen.", AutoSize = true, ForeColor = Stil.TextMittel, Margin = new Padding(0, 0, 0, Stil.AbstandGross) });
        AnmeldenKnopf.AutoSize = true;
        stapel.Controls.Add(AnmeldenKnopf);
        Controls.Add(stapel);
        Layout += (_, _) => stapel.Location = new Point(Math.Max(0, (ClientSize.Width - stapel.Width) / 2), Math.Max(0, (ClientSize.Height - stapel.Height) / 3));
    }
}
