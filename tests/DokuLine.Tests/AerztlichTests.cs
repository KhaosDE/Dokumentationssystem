using DokuLine.Kern;

namespace DokuLine.Tests;

// Bauplan Schritt 26f: Ärztliche Informationen – Diagnosen (nie gelöscht, nur beendet), Art der Reaktion
// je Allergie, Hinweise für den Notfall, zwei neue Rechte.
[Collection("Datenordner")]
public sealed class AerztlichTests : IDisposable
{
    private static readonly DateTime Jetzt = new(2026, 11, 8, 10, 0, 0);
    private readonly Datenordner daten = new();
    private readonly Speicher s = new() { Uhr = () => Jetzt };

    public AerztlichTests() => s.DemoBeimErstenStart();

    public void Dispose()
    {
        s.Dispose();
        daten.Dispose();
    }

    private long Id(string nachname) => s.AlleBewohner().Single(b => b.Nachname == nachname).Id!.Value;

    [Fact]
    public void Demo_Diagnosen_und_Kurzform()
    {
        var ahorn = s.Diagnosen(Id("Ahorn"));
        Assert.Equal(["F20.0 Paranoide Schizophrenie", "E11.9 Diabetes mellitus Typ 2"], ahorn.Select(d => d.Anzeige));
        Assert.Equal(DiagnoseArt.Haupt, ahorn[0].Art);
        Assert.Equal("Dr. Linde (ausgedacht)", ahorn[0].Arzt);
        Assert.Equal("F20.0 Paranoide Schizophrenie · E11.9 Diabetes mellitus Typ 2", DiagnoseRegeln.Kurz(ahorn));
        var holunder = s.Diagnosen(Id("Holunder"));
        Assert.False(holunder[^1].Aktuell);   // beendete stehen unten und bleiben sichtbar
        Assert.DoesNotContain("Asthma", DiagnoseRegeln.Kurz(holunder));
        Assert.All(s.AlleBewohner(), b => Assert.NotEmpty(s.Diagnosen(b.Id!.Value)));
        Assert.Equal("Schwellung im Mund, Atemnot möglich", s.Reaktionen(Id("Ahorn"))["Haselnüsse"]);
        Assert.Contains("Notfallset", s.Notfallhinweise(Id("Ahorn")));
        Assert.Equal("", s.Notfallhinweise(Id("Birke")));
    }

    [Fact]
    public void Diagnose_eintragen_und_beenden()
    {
        long birke = Id("Birke");
        var fehler = Assert.Throws<Eingabefehler>(() => DiagnoseRegeln.AusEingabe(new DiagnoseEingabe { Icd = "Depression", Seit = "01.01.2030" }, birke, "EF", Jetzt)).Fehler;
        Assert.Contains("ICD-10: z. B. F20.0 – oder leer lassen.", fehler);
        Assert.Contains("Bezeichnung fehlt.", fehler);
        Assert.Contains("Seit: Das Datum liegt in der Zukunft.", fehler);
        var neu = s.DiagnoseSpeichern(DiagnoseRegeln.AusEingabe(new DiagnoseEingabe
        {
            Icd = "e66.0", Bezeichnung = "Adipositas", Seit = "01.03.2025", Art = DiagnoseArt.Neben, Arzt = "Dr. Ulme (ausgedacht)",
        }, birke, "EF", Jetzt));
        Assert.Equal("E66.0 Adipositas", neu.Anzeige);
        Assert.Equal(2, s.Diagnosen(birke).Count);
        Assert.Throws<Eingabefehler>(() => s.DiagnoseBeenden(neu.Id, new DateOnly(2025, 1, 1), "EF", Jetzt));   // vor „seit“
        s.DiagnoseBeenden(neu.Id, new DateOnly(2026, 11, 1), "EF", Jetzt);
        var beendet = s.Diagnosen(birke).Single(d => d.Id == neu.Id);
        Assert.Equal((new DateOnly(2026, 11, 1), "EF"), (beendet.Beendet!.Value, beendet.BeendetKuerzel));
        Assert.Throws<Eingabefehler>(() => s.DiagnoseBeenden(neu.Id, new DateOnly(2026, 11, 2), "EF", Jetzt));   // schon beendet
        // Die Datenbank lässt eine Diagnose nicht umschreiben.
        using var befehl = s.Verbindung.CreateCommand();
        befehl.CommandText = $"UPDATE diagnosen SET bezeichnung = 'anders' WHERE id = {neu.Id}";
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => befehl.ExecuteNonQuery());
    }

    [Fact]
    public void Allergien_mit_Reaktion_und_Notfallhinweis()
    {
        long birke = Id("Birke");
        s.AllergienSpeichern(birke, AllergieZustand.Eingetragen, "Penicillin, Latex", new Dictionary<string, string> { ["Penicillin"] = "Hautausschlag", ["Gras"] = "?" });
        var b = s.Bewohner(birke)!;
        Assert.Equal((AllergieZustand.Eingetragen, "Penicillin, Latex"), (b.Allergie, b.Allergien));
        Assert.Equal(new Dictionary<string, string> { ["Penicillin"] = "Hautausschlag" }, s.Reaktionen(birke));   // nur Stoffe aus der Liste
        Assert.Throws<Eingabefehler>(() => s.AllergienSpeichern(birke, AllergieZustand.Eingetragen, " ", new Dictionary<string, string>()));
        s.AllergienSpeichern(birke, AllergieZustand.NichtErfasst, "wird verworfen", new Dictionary<string, string>());
        Assert.Equal((AllergieZustand.NichtErfasst, ""), (s.Bewohner(birke)!.Allergie, s.Bewohner(birke)!.Allergien));
        Assert.Empty(s.Reaktionen(birke));
        s.NotfallhinweiseSpeichern(birke, "  Bei Krampfanfall Notruf 112. ", "EF", Jetzt);
        Assert.Equal("Bei Krampfanfall Notruf 112.", s.Notfallhinweise(birke));
        Assert.Throws<Eingabefehler>(() => s.NotfallhinweiseSpeichern(birke, "x", "", Jetzt));
    }

    [Fact]
    public void Rechte_ansehen_und_bearbeiten()
    {
        Assert.True(s.Darf("CD", Funktionen.AerztlicheAnsehen));
        Assert.True(s.Darf("CD", Funktionen.AerztlicheBearbeiten));        // Fachkraft
        Assert.Equal(RechteRegeln.NurFachkraftText, s.Grund("KL", Funktionen.AerztlicheBearbeiten));   // keine Fachkraft
        Assert.True(s.Darf("KL", Funktionen.AerztlicheAnsehen));
        Assert.Equal(RechteRegeln.FehltText, s.Grund("WZ", Funktionen.AerztlicheAnsehen));   // Verwaltung nicht
        Assert.True(s.Darf("RS", Funktionen.AerztlicheBearbeiten));        // Hausleitung, Fachkraft
        Assert.True(s.Darf("TST", Funktionen.AerztlicheBearbeiten));
    }
}
