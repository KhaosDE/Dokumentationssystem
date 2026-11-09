using DokuLine.Kern;

namespace DokuLine;

// Ein Knopf mit Sperrgrund: Fehlt das Recht, ist er ausgeblendet; geht er gerade nicht (z. B. ohne Anmeldung),
// bleibt er grau und nennt beim Klick den Grund. Windows zeigt Hinweise an ausgeschalteten Knöpfen nicht an –
// deshalb bleibt er eingeschaltet und sieht nur gesperrt aus.
public sealed class Knopf : Button
{
    private static readonly ToolTip Hinweis = new();
    private string? sperrgrund;
    private (Color Vorne, Color Hinten, Color Rand, Cursor Zeiger)? frei;

    // Hauptaktion (farbig, Stil.HauptKnopf) – bleibt es auch gesperrt, wenn die Farbe grau ist.
    public bool Hauptaktion { get; set; }

    public bool OhneRecht => RechteRegeln.FehltRecht(sperrgrund);
    private bool sichtbarGewuenscht = true;   // was der Aufrufer will; ohne Recht bleibt der Knopf trotzdem weg

    protected override void SetVisibleCore(bool value)
    {
        sichtbarGewuenscht = value;
        base.SetVisibleCore(value && !OhneRecht);
    }

    public string? Sperrgrund
    {
        get => sperrgrund;
        set
        {
            if (sperrgrund == value) return;
            if (value is not null && frei is null) frei = (ForeColor, BackColor, FlatAppearance.BorderColor, Cursor);
            bool vorher = OhneRecht;
            sperrgrund = value;
            if (vorher != OhneRecht) base.SetVisibleCore(sichtbarGewuenscht && !OhneRecht);
            if (value is not null)
            {
                ForeColor = Stil.TextLeise;
                BackColor = Stil.Fensterhintergrund;
                FlatAppearance.BorderColor = Stil.Trennlinie;
                Cursor = Cursors.No;
            }
            else if (frei is { } f)
            {
                (ForeColor, BackColor, FlatAppearance.BorderColor, Cursor) = f;
                frei = null;
            }
            AccessibleDescription = value;
            Hinweis.SetToolTip(this, value);
        }
    }

    // Der Hinweis ist für alle Knöpfe derselbe und merkt sich jeden Knopf; ohne Abmelden blieben gelöschte Knöpfe
    // (z. B. bei jedem Neuaufbau der Therapie-Tafel) im Speicher.
    protected override void Dispose(bool disposing)
    {
        if (disposing) Hinweis.SetToolTip(this, null);
        base.Dispose(disposing);
    }

    protected override void OnClick(EventArgs e)
    {
        if (sperrgrund is string grund)
        {
            if (IsHandleCreated) Hinweis.Show(grund, this, 0, Height, 3000);
            return;
        }
        base.OnClick(e);
    }
}
