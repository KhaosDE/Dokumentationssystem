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
        // 0.33.3: Dialoge (fester Rahmen) brauchen ein eigenes Symbol-Objekt; das Vorgabesymbol zeigen sie nicht.
        using (var dialog = new FahrzeugeFenster(u.Speicher))
        {
            Assert.Equal(FormBorderStyle.FixedDialog, dialog.FormBorderStyle);
            Assert.Same(Stil.Dialogsymbol, dialog.Icon);
            Assert.NotSame(Stil.Programmsymbol, dialog.Icon);
        }
        using (var anmelden = new AnmeldenFenster(u.Speicher, ""))
        {
            Stil.SymbolSetzen(anmelden);   // wie vor dem Anzeigen (Dialog.Zeigen, Programmstart)
            Assert.Same(Stil.Dialogsymbol, anmelden.Icon);
        }
        Assert.Equal(Stil.Programmsymbol.Size, Stil.Dialogsymbol.Size);
        Assert.False(f.Kopf.HatLogo);                     // das Logofeld bleibt für die Einrichtung
        // Fenster und Taskleiste nehmen die klassischen Bilder bis 64 px; das 256-px-Bild (PNG) liest der
        // Explorer für große Symbole – dass es in der Datei steckt, prüft ProgrammsymbolTests (Kern).
        using (var gross = new Icon(Stil.Programmsymbol, 48, 48))
            Assert.Equal(48, gross.Width);
        using (var klein = new Icon(Stil.Programmsymbol, 16, 16))
            Assert.Equal(16, klein.Width);
        Hilfe.Foto(f, "117_programmsymbol");
    });
}
