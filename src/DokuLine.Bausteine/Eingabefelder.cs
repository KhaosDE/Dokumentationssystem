using DokuLine.Kern;

namespace DokuLine;

// Datumsfeld (Bauplan Schritt 26d): setzt beim Tippen die Punkte selbst (08112026 → 08.11.2026) und
// ergänzt beim Verlassen das Jahrhundert (081126 → 08.11.2026). Für alle Datumsfelder des Programms.
public class DatumFeld : TextBox
{
    public DatumFeld()
    {
        Width = 100;
        PlaceholderText = "TT.MM.JJJJ";
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (char.IsAsciiDigit(e.KeyChar) && SelectionLength == 0 && SelectionStart == TextLength && Eingabeformat.DatumVorZiffer(Text) is string neu)
        {
            Text = neu;
            SelectionStart = TextLength;
        }
        base.OnKeyPress(e);
    }

    protected override void OnLeave(EventArgs e)
    {
        string fertig = Eingabeformat.DatumFertig(Text);
        if (fertig != Text) Text = fertig;
        base.OnLeave(e);
    }
}

// Uhrzeitfeld: 0930 → 09:30 (Doppelpunkt beim Tippen, Rest beim Verlassen).
public class UhrzeitFeld : TextBox
{
    public UhrzeitFeld()
    {
        Width = 70;
        PlaceholderText = "SS:MM";
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (char.IsAsciiDigit(e.KeyChar) && SelectionLength == 0 && SelectionStart == TextLength && Eingabeformat.UhrzeitVorZiffer(Text) is string neu)
        {
            Text = neu;
            SelectionStart = TextLength;
        }
        base.OnKeyPress(e);
    }

    protected override void OnLeave(EventArgs e)
    {
        string fertig = Eingabeformat.UhrzeitFertig(Text);
        if (fertig != Text) Text = fertig;
        base.OnLeave(e);
    }
}
