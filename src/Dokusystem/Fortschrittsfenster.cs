using Dokusystem.Update;

namespace Dokusystem;

// Kleines Fenster mit Fortschrittsbalken, solange die neue Version geladen wird.
public sealed class Fortschrittsfenster : Form, IFortschritt
{
    private const int Schritte = 1000;
    private readonly Label text = new();
    private readonly ProgressBar balken = new();
    private bool abgebrochen;

    public Fortschrittsfenster()
    {
        Text = "Dokumentationssystem – Update";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Stil.Fensterhintergrund;
        Font = Stil.Standard;
        ClientSize = new Size(480, 136);

        text.AutoSize = false;
        text.Bounds = new Rectangle(Stil.AbstandGross, Stil.AbstandGross, 448, 24);
        text.ForeColor = Stil.TextNormal;
        balken.Bounds = new Rectangle(Stil.AbstandGross, 48, 448, 20);
        balken.Maximum = Schritte;
        var abbrechen = Stil.NebenKnopf("Abbrechen und alte Version starten");
        abbrechen.Location = new Point(Stil.AbstandGross, 84);
        abbrechen.Click += (_, _) => abgebrochen = true;
        Controls.AddRange([text, balken, abbrechen]);
        FormClosing += (_, e) => abgebrochen = true;
        Stil.DpiSkalierungEinschalten(this);
        Show();
        Application.DoEvents();
    }

    public bool Zeige(string meldung, double? anteil)
    {
        text.Text = meldung;
        if (anteil is double a)
        {
            balken.Style = ProgressBarStyle.Continuous;
            balken.Value = (int)(Math.Clamp(a, 0, 1) * Schritte);
        }
        else
        {
            balken.Style = ProgressBarStyle.Marquee;   // unbekannte Größe: laufender Balken
        }
        Application.DoEvents();
        return !abgebrochen;
    }

    public void Schliessen()
    {
        Hide();
        Dispose();
        Application.DoEvents();
    }
}
