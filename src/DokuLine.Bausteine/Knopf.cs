namespace DokuLine;

// Ein Knopf, der ohne Berechtigung gesperrt werden kann: sichtbar und grau, nicht versteckt, damit
// die Demo zeigt, was es gibt (Bauplan Schritt 19). Ein Klick löst dann nichts aus, sondern nennt
// den Grund als Hinweis neben dem Knopf. Windows zeigt Hinweise an ausgeschalteten Knöpfen nicht an –
// deshalb bleibt der Knopf eingeschaltet und sieht nur gesperrt aus.
public sealed class Knopf : Button
{
    private static readonly ToolTip Hinweis = new();
    private string? sperrgrund;
    private (Color Vorne, Color Hinten, Color Rand, Cursor Zeiger)? frei;

    public string? Sperrgrund
    {
        get => sperrgrund;
        set
        {
            if (sperrgrund == value) return;
            if (value is not null && frei is null) frei = (ForeColor, BackColor, FlatAppearance.BorderColor, Cursor);
            sperrgrund = value;
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
