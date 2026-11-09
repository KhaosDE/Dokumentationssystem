using System.Diagnostics.CodeAnalysis;
using DokuLine.Kern;

namespace DokuLine;

// Arztauswahl statt Freitext (Verordnung, Anordnung, Visite, Termin) mit „Arzt anlegen …“ daneben. Ein Name,
// der nicht (mehr) im Verzeichnis steht – z. B. aus älteren Einträgen –, bleibt wählbar und ist so gekennzeichnet.
public sealed class ArztWahl : FlowLayoutPanel
{
    private readonly Speicher speicher;
    private readonly List<string> namen = [];

    public ComboBox Liste { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    public Button AnlegenKnopf { get; } = Stil.NebenKnopf("Arzt anlegen …");
    public event Action<Person?>? Gewechselt;

    private readonly long? bewohnerId;

    // bewohnerId: Die ihm zugeteilten Ärzte (Reiter „Ärzte“) stehen oben, alle übrigen darunter.
    public ArztWahl(Speicher speicher, long? bewohnerId = null)
    {
        this.speicher = speicher;
        this.bewohnerId = bewohnerId;
        AutoSize = true;
        WrapContents = false;
        Margin = new Padding(0);
        Liste.AccessibleName = "Arzt";
        Liste.Margin = AnlegenKnopf.Margin = new Padding(0, 0, Stil.AbstandKlein, 0);
        AnlegenKnopf.AutoSize = true;
        Stil.Sperren(AnlegenKnopf, Recht.Grund(speicher, Funktionen.Aerzte));
        AnlegenKnopf.Click += (_, _) => Anlegen();
        Liste.SelectedIndexChanged += (_, _) => Gewechselt?.Invoke(GewaehlterArzt);
        Controls.AddRange([Liste, AnlegenKnopf]);
        Fuellen();
    }

    private void Fuellen(string? waehlen = null)
    {
        string bisher = waehlen ?? Gewaehlt;
        namen.Clear();
        Liste.Items.Clear();
        var zugeteilt = bewohnerId is long id
            ? speicher.Angaben(id, DateOnly.FromDateTime(DateTime.Now)).GetValueOrDefault(Angabe.Aerzte)?.Lesen<AerzteAngabe>() : null;
        foreach (var a in AngabenRegeln.AerzteSortiert(speicher.Aerzte(nurAktive: true), zugeteilt))
        {
            namen.Add(a.Name);
            Liste.Items.Add(a.Anzeige);
        }
        if (bisher.Length > 0) Gewaehlt = bisher;
    }

    // Der gewählte Name (so wird er gespeichert); leer = keiner.
    public string Gewaehlt
    {
        get => Liste.SelectedIndex >= 0 ? namen[Liste.SelectedIndex] : "";
        set
        {
            string name = value.Trim();
            if (name.Length == 0) { Liste.SelectedIndex = -1; return; }
            int i = namen.FindIndex(n => n.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            if (i < 0)
            {
                namen.Add(name);
                Liste.Items.Add($"{name} (nicht im Verzeichnis)");
                i = namen.Count - 1;
            }
            Liste.SelectedIndex = i;
        }
    }

    // Wie ein Textfeld nutzbar: Text = gewählter Name.
    [AllowNull]
    public override string Text
    {
        get => Gewaehlt;
        set => Gewaehlt = value ?? "";
    }

    public Person? GewaehlterArzt => Gewaehlt.Length > 0 ? speicher.ArztNach(Gewaehlt) : null;

    public Person? Anlegen()
    {
        if (Recht.Grund(speicher, Funktionen.Aerzte) is not null) return null;
        using var fenster = new PersonFenster(speicher, PersonGruppe.Arzt, null);
        if (Dialog.Zeigen(fenster, FindForm()) != DialogResult.OK || fenster.Gespeichert is not Person neu) return null;
        Fuellen(neu.Name);
        return neu;
    }
}
