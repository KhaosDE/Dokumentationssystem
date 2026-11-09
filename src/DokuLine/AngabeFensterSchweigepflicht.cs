using DokuLine.Kern;

namespace DokuLine;

// Schweigepflicht (Bauplan 35): Liste der Einträge; ein Eintrag wird unten bearbeitet und mit „Hinzufügen“ bzw.
// „Übernehmen“ (für den gewählten) in die Liste gelegt. Gespeichert wird die ganze Liste als neue Fassung.
public sealed partial class AngabeFenster
{
    public List<Schweigepflichteintrag> Entbindungen { get; } = [];
    public ListBox EntbindungsListe { get; private set; } = null!;
    public ComboBox EntbindungPerson { get; private set; } = null!;
    public ComboBox EntbindungStelle { get; private set; } = null!;
    public TextBox EntbindungBezeichnung { get; private set; } = null!;
    public ComboBox EntbindungStand { get; private set; } = null!;
    public ComboBox EntbindungUmfang { get; private set; } = null!;
    public ComboBox EntbindungRichtung { get; private set; } = null!;
    public TextBox EntbindungErteilt { get; private set; } = null!;
    public TextBox EntbindungBis { get; private set; } = null!;
    public TextBox EntbindungWiderrufen { get; private set; } = null!;
    public ComboBox EntbindungForm { get; private set; } = null!;
    public ComboBox EntbindungDokument { get; private set; } = null!;

    private sealed record DokumentWahl(long? Id, string Text)
    {
        public override string ToString() => Text;
    }

    private void SchweigepflichtBauen(Action<string, Control> feld, Schweigepflicht? s, DateOnly heute)
    {
        var namen = speicher.PersonenNamen();
        Entbindungen.AddRange(s?.Eintraege ?? []);
        EntbindungsListe = new ListBox { Width = 520, Height = 120, IntegralHeight = false };
        void Fuellen(int wahl = -1)
        {
            EntbindungsListe.Items.Clear();
            EntbindungsListe.Items.AddRange([.. Entbindungen.Select(e => $"{SchweigepflichtRegeln.Wer(e, namen)} – {SchweigepflichtRegeln.StandText(SchweigepflichtRegeln.Wirksam(e, heute))}" +
                                                                         (e.WiderrufenAm is DateOnly w ? $" (widerrufen am {Datum.Text(w)})" : ""))]);
            if (wahl >= 0 && wahl < EntbindungsListe.Items.Count) EntbindungsListe.SelectedIndex = wahl;
        }
        Fuellen();
        var personen = Enum.GetValues<PersonGruppe>().Where(g => g != PersonGruppe.Sonstige).SelectMany(g => speicher.Personen(g, nurAktive: true)).ToList();
        EntbindungPerson = Personenwahl(PersonGruppe.Angehoerige, null, "(keine Person – Stelle rechts)", personen);
        EntbindungPerson.Width = 300;
        EntbindungStelle = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 200 };
        EntbindungStelle.Items.AddRange([.. speicher.Listeneintraege(Kern.Liste.Kostentraeger).Where(e => e.Aktiv).Select(e => e.Name)]);
        EntbindungBezeichnung = new TextBox { Width = 160, PlaceholderText = "z. B. Mutter, Hausarzt" };
        EntbindungStand = Auswahl(["entbunden", "ausdrücklich keine Auskunft", AngabenRegeln.NichtErfasst], 0);
        EntbindungUmfang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 200 };
        EntbindungUmfang.Items.AddRange(SchweigepflichtRegeln.Umfaenge);
        EntbindungUmfang.Text = SchweigepflichtRegeln.UmfangAlle;
        EntbindungRichtung = Auswahl([.. Enum.GetValues<Auskunftsrichtung>().Select(SchweigepflichtRegeln.RichtungText)], (int)Auskunftsrichtung.Beides);
        EntbindungErteilt = DatumsFeld(heute);
        EntbindungBis = DatumsFeld(null);
        EntbindungBis.PlaceholderText = "unbefristet";
        EntbindungWiderrufen = DatumsFeld(null);
        EntbindungForm = Auswahl(["", .. SchweigepflichtRegeln.Formen], 1);
        EntbindungDokument = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        EntbindungDokument.Items.Add(new DokumentWahl(null, "(kein Dokument)"));
        foreach (var d in speicher.Dokumente(bewohner.Id!.Value)) EntbindungDokument.Items.Add(new DokumentWahl(d.Id, $"{d.Titel} ({d.Kategorie})"));
        EntbindungDokument.SelectedIndex = 0;

        EntbindungsListe.SelectedIndexChanged += (_, _) => { if (EntbindungsListe.SelectedIndex >= 0) Laden(Entbindungen[EntbindungsListe.SelectedIndex]); };
        feld("Einträge", EntbindungsListe);
        feld("Person", Formular.Reihe(EntbindungPerson, new Label { Text = "oder Stelle", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, EntbindungStelle));
        feld("Bezeichnung", EntbindungBezeichnung);
        feld("Zustand", EntbindungStand);
        feld("Umfang", Formular.Reihe(EntbindungUmfang, EntbindungRichtung));
        feld("Erteilt am, gültig bis", Formular.Reihe(EntbindungErteilt, EntbindungBis, new Label { Text = "widerrufen am", AutoSize = true, Margin = new Padding(0, Stil.AbstandKlein, 0, 0) }, EntbindungWiderrufen));
        feld("Form", EntbindungForm);
        feld("Dokument", EntbindungDokument);
        feld("", Formular.Reihe(
            Knopf("Hinzufügen", () => { if (EintragLesen() is Schweigepflichteintrag e) { Entbindungen.Add(e); Fuellen(Entbindungen.Count - 1); } }),
            Knopf("Übernehmen", () =>
            {
                int i = EntbindungsListe.SelectedIndex;
                if (i < 0) { Fehlermeldung("Bitte zuerst einen Eintrag wählen."); return; }
                if (EintragLesen() is Schweigepflichteintrag e) { Entbindungen[i] = e; Fuellen(i); }
            }),
            Knopf("Entfernen", () => { if (EntbindungsListe.SelectedIndex >= 0) { Entbindungen.RemoveAt(EntbindungsListe.SelectedIndex); Fuellen(); } })));
        lesen = () => new Schweigepflicht { Eintraege = [.. Entbindungen] };
    }

    private static ComboBox Auswahl(string[] texte, int gewaehlt)
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        box.Items.AddRange(texte);
        box.SelectedIndex = gewaehlt;
        return box;
    }

    private void Laden(Schweigepflichteintrag e)
    {
        int person = EntbindungPerson.Items.Cast<object>().ToList().FindIndex(w => w is Wahl x && x.Id == e.PersonId);
        EntbindungPerson.SelectedIndex = e.PersonId is null ? 0 : Math.Max(0, person);
        EntbindungStelle.Text = e.Stelle;
        EntbindungBezeichnung.Text = e.Bezeichnung;
        EntbindungStand.SelectedIndex = (int)e.Stand;
        EntbindungUmfang.Text = e.Umfang;
        EntbindungRichtung.SelectedIndex = (int)e.Richtung;
        EntbindungErteilt.Text = e.ErteiltAm is DateOnly a ? Datum.Text(a) : "";
        EntbindungBis.Text = e.GueltigBis is DateOnly b ? Datum.Text(b) : "";
        EntbindungWiderrufen.Text = e.WiderrufenAm is DateOnly w ? Datum.Text(w) : "";
        EntbindungForm.SelectedIndex = Math.Max(0, EntbindungForm.Items.IndexOf(e.Form));
        int dokument = EntbindungDokument.Items.Cast<DokumentWahl>().ToList().FindIndex(d => d.Id == e.DokumentId);
        EntbindungDokument.SelectedIndex = Math.Max(0, dokument);
    }

    // Der Eintrag aus den Feldern; bei Fehlern null (die Meldung steht im Fenster).
    private Schweigepflichteintrag? EintragLesen()
    {
        try
        {
            var e = new Schweigepflichteintrag
            {
                PersonId = Gewaehlt(EntbindungPerson), Stelle = Gewaehlt(EntbindungPerson) is null ? EntbindungStelle.Text.Trim() : "",
                Bezeichnung = EntbindungBezeichnung.Text.Trim(), Stand = (Entbindungsstand)EntbindungStand.SelectedIndex,
                Umfang = EntbindungUmfang.Text.Trim().Length > 0 ? EntbindungUmfang.Text.Trim() : SchweigepflichtRegeln.UmfangAlle,
                Richtung = (Auskunftsrichtung)EntbindungRichtung.SelectedIndex,
                ErteiltAm = DatumOderLeer(EntbindungErteilt, "Erteilt am"), GueltigBis = DatumOderLeer(EntbindungBis, "Gültig bis"),
                WiderrufenAm = DatumOderLeer(EntbindungWiderrufen, "Widerrufen am"), Form = EntbindungForm.Text,
                DokumentId = (EntbindungDokument.SelectedItem as DokumentWahl)?.Id,
            };
            if (SchweigepflichtRegeln.Pruefen(new Schweigepflicht { Eintraege = [e] }) is { Count: > 0 } fehler) throw new Eingabefehler(fehler);
            Fehler.Visible = false;
            return e;
        }
        catch (Eingabefehler x)
        {
            Fehlermeldung(string.Join(" ", x.Fehler));
            return null;
        }
    }
}
