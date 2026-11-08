namespace DokuLine.Oberflaeche.Tests;

// Bauplan Schritt 26n: Dasselbe Programmsymbol für Hauptfenster, Dialoge und alle übrigen Fenster;
// nicht im Logofeld der Kopfzeile (Regel 7).
[Collection("Oberflaeche")]
public class ProgrammsymbolOberflaechenTests
{
    [Fact]
    public void Symbol_in_allen_Fenstern_nicht_im_Logofeld() => Hilfe.Sta(() =>
    {
        using var u = new Umgebung();
        using var f = new Hauptfenster(u.Speicher);
        Hilfe.Zeigen(f);
        Assert.Same(Stil.Programmsymbol, f.Icon);
        Assert.True(Stil.SymbolFuerAlleFenster());
        using (var leer = new Form())
            Assert.Same(Stil.Programmsymbol, leer.Icon);   // Vorgabe für jedes Fenster ohne eigenes Symbol
        using (var dialog = new FahrzeugeFenster(u.Speicher))
            Assert.Same(Stil.Programmsymbol, dialog.Icon);
        using (var anmelden = new AnmeldenFenster(u.Speicher, ""))
            Assert.Same(Stil.Programmsymbol, anmelden.Icon);
        Assert.False(f.Kopf.HatLogo);                     // das Logofeld bleibt für die Einrichtung
        using (var gross = new Icon(Stil.Programmsymbol, 256, 256))
            Assert.Equal(256, gross.Width);
        using (var klein = new Icon(Stil.Programmsymbol, 16, 16))
            Assert.Equal(16, klein.Width);
        Hilfe.Foto(f, "117_programmsymbol");
    });
}
